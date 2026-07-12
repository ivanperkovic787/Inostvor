using System.Text.Json;
using Inostvor.Core.Model.Machines;
using Inostvor.Core.Model.Toolpath;

namespace Inostvor.Core.Model.Project;

/// <summary>Jedan DXF izvor u projektu.</summary>
/// <param name="FileName">Ime datoteke unutar kontejnera (dxf/…).</param>
/// <param name="SourcePath">Apsolutna putanja na disku (pri spremanju: odakle kopirati; pri učitavanju: gdje je raspakirano).</param>
public sealed record ProjectDxfSource(string FileName, string SourcePath);

/// <summary>
/// PROJEKT — jedina istina korisnikova rada (ADR-005). Sadrži IZVORE i ODLUKE;
/// derivirani podaci (konture, validacija, putanja) NE spremaju se nego se
/// deterministički regeneriraju iz izvora (bajt-deterministički cjevovod M3–M7).
///
/// <see cref="Extensions"/> je forward-compatibility kanal: budući moduli
/// (nesting, tabovi, višestruki limovi, ostaci, baza materijala, optimizacija)
/// dodaju vlastite sekcije; stariji Inostvor NEPOZNATE sekcije čuva netaknute
/// pri round-tripu — otvaranje starim programom nikad ne gubi podatke novih.
/// </summary>
public sealed record ProjectDocument
{
    public required string Name { get; init; }

    public IReadOnlyList<ProjectDxfSource> DxfSources { get; init; } = [];

    /// <summary>Aktivna tehnologija projekta.</summary>
    public TechnologySettings Technology { get; init; } = TechnologySettings.Default;

    /// <summary>UGRAĐENA kopija profila stroja (prenosivost: projekt se otvara i na računalu bez tog profila).</summary>
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

/// <summary>Rezultat učitavanja projekta: dokument + gdje su DXF-ovi raspakirani.</summary>
public sealed record LoadedProject(ProjectDocument Document, int FormatVersion);
