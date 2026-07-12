namespace Inostvor.Core.Model.Toolpath;

/// <summary>Proces rezanja. IR i CAM jezgra su procesno neutralni — enum služi
/// profilima tehnologije i budućim procesno specifičnim strategijama, nikad granama u jezgri.</summary>
public enum CutProcess
{
    Plasma = 0,
    Laser = 1,
    OxyFuel = 2,
    WaterJet = 3,
    Router = 4,
    Mill = 5,
}

/// <summary>Stil leada — otvoren skup; V1 implementira Line i Arc, arhitektura nosi sve.</summary>
public enum LeadStyle
{
    None = 0,
    Line = 1,
    Arc = 2,
    Tangential = 3,
    Loop = 4,
    Corner = 5,
    PierceOnScrap = 6,
}

/// <summary>
/// Tehnologija rezanja — procesno neutralni parametri koje IR i CAM operacije
/// razumiju. Parametri specifični za stroj/kontroler (THC, visine, M-kodovi)
/// NE pripadaju ovdje nego postprocesoru (M7, ADR-004).
/// </summary>
public sealed record TechnologySettings
{
    public CutProcess Process { get; init; } = CutProcess.Plasma;

    /// <summary>Širina reza; offset je KerfWidth/2. [mm]</summary>
    public double KerfWidth { get; init; } = 1.5;

    /// <summary>Posmak rezanja. [mm/min]</summary>
    public double FeedRate { get; init; } = 2500.0;

    /// <summary>Brzina brzih pomaka (za procjenu vremena). [mm/min]</summary>
    public double RapidRate { get; init; } = 8000.0;

    /// <summary>Vrijeme probijanja po piercu. [s]</summary>
    public double PierceTime { get; init; } = 0.6;

    public LeadStyle LeadInStyle { get; init; } = LeadStyle.Arc;

    public LeadStyle LeadOutStyle { get; init; } = LeadStyle.Line;

    /// <summary>Duljina/polumjer leada. [mm]</summary>
    public double LeadInLength { get; init; } = 4.0;

    public double LeadOutLength { get; init; } = 2.0;

    /// <summary>Produljenje reza preko točke zatvaranja (0 = isključeno). [mm]</summary>
    public double OvercutLength { get; init; }

    /// <summary>Arc fitting nakon kerf offseta (isključivo opcionalan; točnost apsolutni prioritet).</summary>
    public bool EnableArcFitting { get; init; } = true;

    /// <summary>Maksimalno dopušteno odstupanje arc-fita od ulaznih točaka. [mm]</summary>
    public double ArcFittingTolerance { get; init; } = 0.01;

    /// <summary>Tolerancija tessellacije prije Clipper offseta. [mm]</summary>
    public double OffsetTessellationTolerance { get; init; } = 0.01;

    /// <summary>Id strategije redoslijeda rezanja (vidi ICutOrderStrategy).</summary>
    public string CutOrderStrategyId { get; init; } = "bottom-to-top";

    public static TechnologySettings Default { get; } = new();
}
