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
/// Redoslijed rezanja — OTVOREN SKUP strategija (bottom-to-top, nearest-neighbor,
/// left-to-right, grid, minimal-rapids, custom-user-order…). Nove strategije se
/// registriraju u DI-ju i biraju po Id-u kroz TechnologySettings — BEZ izmjene
/// ToolpathGeneratora. Nepromjenjivo pravilo svake strategije: rupe dijela prije
/// njegove vanjske konture.
/// </summary>
public interface ICutOrderStrategy
{
    /// <summary>Stabilan identifikator strategije (npr. "bottom-to-top", "nearest-neighbor").</summary>
    string Id { get; }

    IReadOnlyList<CutSequence> Order(IReadOnlyList<CutSequence> sequences, IReadOnlyList<Contour> contours);
}

/// <summary>Rezolucija strategije redoslijeda po Id-u; nepoznat Id → default (konzervativno).</summary>
public interface ICutOrderStrategyProvider
{
    ICutOrderStrategy Resolve(string strategyId);
}

/// <summary>Generator cijelog programa: konture + tehnologija → neutralni IR.</summary>
public interface IToolpathGenerator
{
    ToolpathProgram Generate(IReadOnlyList<Contour> contours, TechnologySettings technology);
}
