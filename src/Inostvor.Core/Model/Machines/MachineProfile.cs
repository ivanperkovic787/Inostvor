using Inostvor.Core.Model.Toolpath;

namespace Inostvor.Core.Model.Machines;

/// <summary>
/// KONKRETAN STROJ — odvojen pojam od postprocesora: postprocesor opisuje
/// DIJALEKT kontrolera, profil opisuje stroj koji tim dijalektom govori.
/// Jedan postprocesor → mnogo profila (Mach3 Plasma, Mach3 Router, Mach3 Mill…).
/// </summary>
public sealed record MachineProfile
{
    public required string Name { get; init; }

    /// <summary>Id postprocesora čijim dijalektom stroj govori (npr. "inostvor.post.mach3").</summary>
    public required string PostProcessorId { get; init; }

    public CutProcess Process { get; init; } = CutProcess.Plasma;

    /// <summary>Zadana tehnologija stroja (kerf, posmaci, leadovi…).</summary>
    public TechnologySettings DefaultTechnology { get; init; } = TechnologySettings.Default;

    /// <summary>Radna površina. [mm]</summary>
    public double TableWidth { get; init; } = 1500;

    public double TableHeight { get; init; } = 3000;

    /// <summary>Sigurna visina Z za brze pomake. [mm]</summary>
    public double SafeZ { get; init; } = 30.0;

    /// <summary>Visina probijanja. [mm]</summary>
    public double PierceHeight { get; init; } = 3.8;

    /// <summary>Visina rezanja. [mm]</summary>
    public double CutHeight { get; init; } = 1.5;

    /// <summary>
    /// M-makro za touch-off/probe prije probijanja (npr. "M101"); prazno = bez probe
    /// linije (kontroleri s vlastitim THC/IHS ciklusom).
    /// </summary>
    public string ProbeMacro { get; init; } = "";

    /// <summary>Otvorena vreća strojno specifičnih parametara za sekvence u kodu.</summary>
    public IReadOnlyDictionary<string, string> Extra { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);
}
