using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;
using Inostvor.Core.Abstractions;
using Inostvor.Core.Model.Project;

namespace Inostvor.Data.Project;

/// <summary>
/// .ino format (ADR-005, ADR-006): ZIP kontejner s
///   manifest.json  — {"formatVersion": N}
///   project.json   — ProjectDocument (enumi kao stringovi, uvlačeno)
///   dxf/&lt;ime&gt;    — ORIGINALNI bajtovi uvezenih DXF-ova
///   cache/toolpath.json — OPCIONALNI izvedeni podaci (nije izvor istine)
/// Odabran je ZIP+JSON jer je čitljiv standardnim alatima i za 10+ godina;
/// derivirani podaci se ne spremaju (regeneriraju se deterministički iz izvora).
/// Nepoznate sekcije u Extensions čuvaju se netaknute (forward compatibility).
/// Učitavanje starijih verzija ide kroz lanac IProjectMigration koraka.
/// </summary>
public sealed class ProjectStore : IProjectStore
{
    private readonly IReadOnlyList<IProjectMigration> _migrations;

    public ProjectStore(IEnumerable<IProjectMigration> migrations)
    {
        ArgumentNullException.ThrowIfNull(migrations);
        _migrations = migrations.OrderBy(m => m.FromVersion).ToList();
    }

    public int CurrentFormatVersion => 1;

    public async Task SaveAsync(ProjectDocument document, string path, ToolpathCache? cache = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // Atomarno: piši u temp pa zamijeni (pad usred spremanja ne uništava stari projekt).
        var tempPath = path + ".tmp";
        await using (var stream = File.Create(tempPath))
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
        {
            var manifest = zip.CreateEntry("manifest.json");
            await using (var writer = new StreamWriter(manifest.Open()))
            {
                await writer.WriteAsync(FormattableString.Invariant(
                    $"{{\n  \"formatVersion\": {CurrentFormatVersion}\n}}\n"));
            }

            // DXF izvori: kopiraj originalne bajtove; u dokumentu putanja postaje ime u kontejneru.
            var containedSources = new List<ProjectDxfSource>();
            foreach (var source in document.DxfSources)
            {
                var entryName = "dxf/" + source.FileName;
                var entry = zip.CreateEntry(entryName);
                await using (var target = entry.Open())
                await using (var origin = File.OpenRead(source.SourcePath))
                {
                    await origin.CopyToAsync(target);
                }

                containedSources.Add(source with { SourcePath = entryName });
            }

            var projectEntry = zip.CreateEntry("project.json");
            await using (var writer = new StreamWriter(projectEntry.Open()))
            {
                var toWrite = document with { DxfSources = containedSources };
                await writer.WriteAsync(JsonSerializer.Serialize(toWrite, ProjectJson.Options));
            }

            // Opcionalni cache — spremljen odvojeno od istine; brisanje datoteke
            // iz kontejnera ne mijenja projekt, samo usporava sljedeće otvaranje.
            if (cache is not null)
            {
                var cacheEntry = zip.CreateEntry("cache/toolpath.json");
                await using var cacheWriter = new StreamWriter(cacheEntry.Open());
                await cacheWriter.WriteAsync(JsonSerializer.Serialize(cache, ProjectJson.Options));
            }
        }

        File.Move(tempPath, path, overwrite: true);
    }

    public async Task<LoadedProject> LoadAsync(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using var zip = ZipFile.OpenRead(path);

        var manifestEntry = zip.GetEntry("manifest.json")
            ?? throw new InvalidDataException("Nije Inostvor projekt: nedostaje manifest.json.");
        int version;
        using (var reader = new StreamReader(manifestEntry.Open()))
        {
            var manifest = JsonNode.Parse(await reader.ReadToEndAsync())
                ?? throw new InvalidDataException("Neispravan manifest.json.");
            version = manifest["formatVersion"]?.GetValue<int>()
                ?? throw new InvalidDataException("manifest.json nema formatVersion.");
        }

        if (version > CurrentFormatVersion)
        {
            throw new InvalidDataException(FormattableString.Invariant(
                $"Projekt je formata v{version}, a ova verzija Inostvora čita do v{CurrentFormatVersion}. Ažuriraj Inostvor."));
        }

        var projectEntry = zip.GetEntry("project.json")
            ?? throw new InvalidDataException("Nije Inostvor projekt: nedostaje project.json.");
        JsonNode projectNode;
        using (var reader = new StreamReader(projectEntry.Open()))
        {
            projectNode = JsonNode.Parse(await reader.ReadToEndAsync())
                ?? throw new InvalidDataException("Neispravan project.json.");
        }

        // Lanac migracija: v → v+1 → … → Current. Kompatibilnost se nikad ne razbija.
        while (version < CurrentFormatVersion)
        {
            var step = _migrations.FirstOrDefault(m => m.FromVersion == version)
                ?? throw new InvalidDataException(FormattableString.Invariant(
                    $"Nedostaje migracija formata v{version} → v{version + 1}."));
            projectNode = step.Migrate(projectNode);
            version++;
        }

        var document = projectNode.Deserialize<ProjectDocument>(ProjectJson.Options)
            ?? throw new InvalidDataException("project.json se ne može pročitati.");

        // Raspakiraj DXF izvore u temp — pipeline ih uvozi kao obične datoteke.
        //
        // JEDINSTVENOST: Environment.TickCount64 ima rezoluciju ~15 ms, pa dva otvaranja
        // projekta u istom trenutku dobiju ISTI direktorij i drugo puca s "file in use".
        // GUID to isključuje.
        var sessionRoot = Path.Combine(Path.GetTempPath(), "Inostvor", "projects");
        CleanupStaleExtractions(sessionRoot);

        var extractDir = Path.Combine(sessionRoot,
            Path.GetFileNameWithoutExtension(path) + "_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(extractDir);

        var extracted = new List<ProjectDxfSource>();
        foreach (var source in document.DxfSources)
        {
            var entry = zip.GetEntry(source.SourcePath)
                ?? throw new InvalidDataException($"U projektu nedostaje '{source.SourcePath}'.");
            var target = Path.Combine(extractDir, source.FileName);
            entry.ExtractToFile(target, overwrite: true);
            extracted.Add(source with { SourcePath = target });
        }

        var finalDocument = document with { DxfSources = extracted };

        return new LoadedProject(finalDocument, version, ReadValidCache(zip, finalDocument));
    }

    /// <summary>
    /// Briše raspakirane DXF-ove iz PRIJAŠNJIH sesija (starije od 24 h). Bez ovoga bi
    /// svako otvaranje projekta trajno ostavljalo direktorij u %Temp%. Greške se tiho
    /// progutaju — čišćenje smeća nikad ne smije spriječiti otvaranje projekta.
    /// </summary>
    private static void CleanupStaleExtractions(string sessionRoot)
    {
        try
        {
            if (!Directory.Exists(sessionRoot))
            {
                Directory.CreateDirectory(sessionRoot);
                return;
            }

            var cutoff = DateTime.UtcNow.AddHours(-24);
            foreach (var dir in Directory.GetDirectories(sessionRoot))
            {
                try
                {
                    if (Directory.GetLastWriteTimeUtc(dir) < cutoff)
                    {
                        Directory.Delete(dir, recursive: true);
                    }
                }
                catch (IOException)
                {
                    // Direktorij je u upotrebi (druga instanca Inostvora) — preskoči.
                }
                catch (UnauthorizedAccessException)
                {
                    // Nema prava — preskoči.
                }
            }
        }
        catch (IOException)
        {
            // Čišćenje je best-effort.
        }
        catch (UnauthorizedAccessException)
        {
            // Čišćenje je best-effort.
        }
    }

    /// <summary>
    /// Cache se prihvaća SAMO ako se podudaraju (a) verzija cjevovoda i (b) hash svih
    /// ulaza izračunat iz TRENUTNOG stanja projekta. Svaka nepodudarnost, neispravan
    /// JSON ili nedostajuća datoteka → null (tiha regeneracija). Cache nikad ne smije
    /// uzrokovati grešku otvaranja projekta.
    /// </summary>
    private static ToolpathCache? ReadValidCache(ZipArchive zip, ProjectDocument document)
    {
        var entry = zip.GetEntry("cache/toolpath.json");
        if (entry is null)
        {
            return null;
        }

        try
        {
            using var reader = new StreamReader(entry.Open());
            var cache = JsonSerializer.Deserialize<ToolpathCache>(reader.ReadToEnd(), ProjectJson.Options);
            if (cache is null || cache.PipelineVersion != CacheKey.PipelineVersion)
            {
                return null;
            }

            var expected = CacheKey.ComputeInputHash(document.DxfSources, document.Technology);
            return string.Equals(cache.InputHash, expected, StringComparison.Ordinal) ? cache : null;
        }
        catch (JsonException)
        {
            return null; // oštećen cache = nema cachea
        }
    }
}
