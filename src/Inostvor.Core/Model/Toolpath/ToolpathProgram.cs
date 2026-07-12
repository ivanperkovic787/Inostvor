using Inostvor.Kernel.Primitives;

namespace Inostvor.Core.Model.Toolpath;

/// <summary>Uloga poteza u sekvenci — simulacija i postprocesori razlikuju faze bez parsiranja.</summary>
public enum MoveKind
{
    LeadIn = 0,
    Cut = 1,
    LeadOut = 2,
    Overcut = 3,
}

/// <summary>
/// Jedan potez rezanja: geometrija (linija ili luk — first-class), uloga i posmak.
/// POTPUNO NEUTRALAN: nema G-koda, nema dijalekta, nema kontrolera. Postprocesor
/// (M7) je JEDINO mjesto gdje iz ovoga nastaje G-kod; simulacija (M6) koristi
/// isti model (Length + FeedRate ⇒ trajanje, Geometry.PointAt(t) ⇒ aktivna točka).
/// </summary>
/// <param name="Geometry">Linija ili luk u world koordinatama. [mm]</param>
/// <param name="Kind">Uloga poteza.</param>
/// <param name="FeedRate">Posmak. [mm/min]</param>
public sealed record CutMove(ISegment Geometry, MoveKind Kind, double FeedRate)
{
    public double Length => Geometry.Length;

    /// <summary>Trajanje poteza. [s]</summary>
    public double Duration => FeedRate > 0 ? Length / FeedRate * 60.0 : 0.0;
}

/// <summary>Brzi pomak (torch ugašen) — eksplicitno u programu radi simulacije.</summary>
public sealed record RapidMove(Point2 From, Point2 To)
{
    public double Length => From.DistanceTo(To);
}

/// <summary>
/// Jedna sekvenca rezanja = jedan pierce: brzi dolazak, probijanje na PiercePoint,
/// pa neprekinuti niz poteza (lead-in → cut → overcut → lead-out).
/// </summary>
public sealed record CutSequence(
    int SourceContourId,
    Point2 PiercePoint,
    IReadOnlyList<CutMove> Moves)
{
    public Point2 EndPoint => Moves.Count > 0 ? Moves[^1].Geometry.EndPoint : PiercePoint;

    public double CutLength => Moves.Sum(m => m.Length);
}

/// <summary>Sažeta statistika programa — izračunata jednom, bez parsiranja G-koda.</summary>
public sealed record ToolpathStatistics(
    double CutLength,
    double RapidLength,
    double CutTimeSeconds,
    double RapidTimeSeconds,
    double PierceTimeSeconds,
    int PierceCount)
{
    public double TotalTimeSeconds => CutTimeSeconds + RapidTimeSeconds + PierceTimeSeconds;
}

/// <summary>
/// Cijeli program: naizmjenično Rapids[i] → Sequences[i]. Rapids.Count == Sequences.Count
/// (prvi rapid kreće iz ishodišta stroja). Sve što treba simulaciji i postprocesoru.
/// </summary>
public sealed record ToolpathProgram(
    IReadOnlyList<CutSequence> Sequences,
    IReadOnlyList<RapidMove> Rapids,
    TechnologySettings Technology,
    ToolpathStatistics Statistics)
{
    public static ToolpathProgram Empty { get; } = new(
        [], [], TechnologySettings.Default, new ToolpathStatistics(0, 0, 0, 0, 0, 0));
}
