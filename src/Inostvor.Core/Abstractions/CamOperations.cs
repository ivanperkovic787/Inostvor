using Inostvor.Core.Model.Geometry;
using Inostvor.Core.Model.Toolpath;
using Inostvor.Kernel.Primitives;

namespace Inostvor.Core.Abstractions;

/// <summary>
/// Kerf offset: kontura → offsetirane putanje (poligonalne točke; lukove vraća
/// ArcFitting). Strana offseta slijedi iz vrste konture (Outer van, Hole unutra).
/// DETERMINIZAM je dio kontrakta: isti ulaz ⇒ identičan izlaz (redoslijed putanja,
/// početna točka i orijentacija svake putanje stabilno su normalizirani).
/// </summary>
public interface IKerfOffsetService
{
    IReadOnlyList<IReadOnlyList<Point2>> Offset(Contour contour, double kerfWidth, double tessellationTolerance);
}

/// <summary>
/// Arc fitting: poligonalne točke → segmenti (lukovi + linije). ISKLJUČIVO
/// opcionalan i konzervativan: luk se prihvaća samo ako SVE ulazne točke leže
/// unutar tolerancije; inače ostaju linije. Nikad netočan G2/G3 radi kompresije.
/// </summary>
public interface IArcFitter
{
    IReadOnlyList<ISegment> Fit(IReadOnlyList<Point2> points, bool closed, double tolerance);
}

/// <summary>Overcut: produljenje zatvorenog reza preko točke zatvaranja.</summary>
public interface IOvercutService
{
    IReadOnlyList<CutMove> Apply(IReadOnlyList<CutMove> moves, double overcutLength, double feedRate);
}

/// <summary>
/// Redoslijed rezanja. V1: deterministički default (rupe prije vanjske konture
/// istog dijela, dijelovi po poziciji); napredne strategije (M6) kroz isti kontrakt.
/// </summary>
public interface ICutOrderStrategy
{
    IReadOnlyList<CutSequence> Order(IReadOnlyList<CutSequence> sequences, IReadOnlyList<Contour> contours);
}

/// <summary>Generator cijelog programa: konture + tehnologija → neutralni IR.</summary>
public interface IToolpathGenerator
{
    ToolpathProgram Generate(IReadOnlyList<Contour> contours, TechnologySettings technology);
}
