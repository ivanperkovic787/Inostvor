using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;
using Inostvor.Core.Abstractions;
using Inostvor.Core.Model.Project;

namespace Inostvor.Data.Project;

/// <summary>
/// .ino format (ADR-005): ZIP kontejner s
///   manifest.json  — {"formatVersion": N}
///   project.json   — ProjectDocument (enumi kao stringovi, uvlačeno)
///   dxf/&lt;ime&gt;    — ORIGINALNI bajtovi uvezenih DXF-ova
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

    public async Task SaveAsync(ProjectDocument document, string path)
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
        var extractDir = Path.Combine(Path.GetTempPath(), "Inostvor",
            "project_" + Path.GetFileNameWithoutExtension(path) + "_" + Environment.TickCount64);
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

        return new LoadedProject(document with { DxfSources = extracted }, version);
    }
}
