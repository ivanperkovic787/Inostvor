using System.Text.Json;
using Inostvor.Core.Model.Machines;
using Inostvor.Core.Model.Toolpath;

namespace Inostvor.Core.Model.Project;

/// <summary>
/// Jedan DXF izvor u projektu, sa stabilnim Id-om i hashom sadržaja.
/// </summary>
/// <param name="Id">Stabilan identitet izvora (ADR-006).</param>
/// <param name="FileName">Ime datoteke unutar kontejnera (dxf/…).</param>
/// <param name="SourcePath">Putanja na disku (pri spremanju: odakle kopirati; pri učitavanju: gdje je raspakirano).</param>
/// <param name="Sha256">SHA-256 sadržaja DXF-a — ulaz u ključ valjanosti cachea.</param>
public sealed record ProjectDxfSource(Guid Id, string FileName, string SourcePath, string Sha256 = "")
{
    public static ProjectDxfSource Create(string fileName, string sourcePath, string sha256 = "")
        => new(Guid.NewGuid(), fileName, sourcePath, sha256);
}

/// <summary>
/// OPCIONALNI cache izvedenih podataka (ADR-006). NIJE izvor istine — služi samo
/// brzom otvaranju velikih projekata.
///
/// <see cref="InputHash"/> pokriva SVE ulaze deterministički generiranog rezultata:
/// hasheve svih DXF izvora + tehnologiju + verziju cjevovoda. Ako se ijedan ulaz
/// promijeni (ili se promijeni algoritam → nova PipelineVersion), hash se ne
/// poklapa i cache se ODBACUJE te regenerira. Nikad se ne koristi "možda valjan" cache.
/// </summary>
/// <param name="InputHash">Hash svih ulaza koji determiniraju rezultat.</param>
/// <param name="PipelineVersion">Verzija CAM cjevovoda koja je proizvela cache.</param>
/// <param name="Program">Spremljeni ToolpathProgram.</param>
public sealed record ToolpathCache(string InputHash, int PipelineVersion, ToolpathProgram Program);

/// <summary>
/// PROJEKT — jedina istina korisnikova rada (ADR-005). Sadrži IZVORE i ODLUKE;
/// izvedeni podaci (konture, validacija, putanja) mogu postojati kao OPCIONALNI
/// cache uz provjeru hasha (ADR-006), ali izvor istine su uvijek DXF + odluke.
///
/// <see cref="Extensions"/> je forward-compatibility kanal: budući moduli
/// (nesting, tabovi, višestruki limovi, ostaci, baza materijala, optimizacija)
/// dodaju vlastite sekcije; stariji Inostvor NEPOZNATE sekcije čuva netaknute
/// pri round-tripu — otvaranje starim programom nikad ne gubi podatke novih.
/// </summary>
public sealed record ProjectDocument
{
    /// <summary>Stabilan identitet projekta (ADR-006) — preživljava preimenovanje datoteke.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Name { get; init; }

    public IReadOnlyList<ProjectDxfSource> DxfSources { get; init; } = [];

    /// <summary>Aktivna tehnologija projekta (ugrađena kopija — projekt je samodostatan).</summary>
    public TechnologySettings Technology { get; init; } = TechnologySettings.Default;

    /// <summary>Id tehnologije iz biblioteke iz koje je kopirana (Empty ako je ručno postavljena).</summary>
    public Guid TechnologyId { get; init; }

    /// <summary>UGRAĐENA kopija profila stroja (prenosivost: projekt se otvara i bez tog profila u biblioteci).</summary>
    public MachineProfile Machine { get; init; } = new()
    {
        Name = "Nepoznat stroj",
        PostProcessorId = "inostvor.post.mach3",
    };

    /// <summary>Checkpoint simulacije (pozicija na vremenskoj crti, brzina); null = od početka.</summary>
    public double? SimulationTimeSeconds { get; init; }

    public double SimulationSpeed { get; init; } = 1.0;

    /// <summary>Sekcije budućih modula — čuvaju se i kad ih ova verzija ne razumije.</summary>
    public IReadOnlyDictionary<string, JsonElement> Extensions { get; init; } =
        new Dictionary<string, JsonElement>(StringComparer.Ordinal);
}

/// <summary>Rezultat učitavanja: dokument, verzija formata i (ako je valjan) cache.</summary>
/// <param name="Cache">Valjan cache ili null — pozivatelj regenerira ako je null.</param>
public sealed record LoadedProject(ProjectDocument Document, int FormatVersion, ToolpathCache? Cache);
