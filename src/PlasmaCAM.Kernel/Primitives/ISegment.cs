namespace PlasmaCAM.Kernel.Primitives;

/// <summary>
/// Zajednički kontrakt geometrijskih segmenata (linija, luk). Lukovi su first-class
/// primitivi kroz cijeli pipeline — tesselliraju se samo tamo gdje je nužno (Clipper2 granica).
/// Implementacije su nepromjenjive (immutable).
/// </summary>
public interface ISegment
{
    Point2 StartPoint { get; }

    Point2 EndPoint { get; }

    double Length { get; }

    Aabb Bounds { get; }

    /// <summary>Točka na segmentu za parametar t ∈ [0,1] po duljini luka (0 = start, 1 = end).</summary>
    Point2 PointAt(double t);

    /// <summary>Najbliža točka NA segmentu (uključujući krajeve) zadanoj točki.</summary>
    Point2 ClosestPoint(Point2 point);

    /// <summary>Segment suprotnog smjera (start i end zamijenjeni, geometrija identična).</summary>
    ISegment Reversed();
}
