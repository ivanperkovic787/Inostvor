using Inostvor.Kernel.Primitives;

namespace Inostvor.Kernel.Intersections;

/// <summary>Polimorfni presjek dvaju segmenata bilo kojeg tipa (dispatcher).</summary>
public static class SegmentIntersection
{
    /// <summary>
    /// Upisuje do 2 presječne točke u <paramref name="results"/> (kapacitet ≥ 2) i vraća broj.
    /// Kolinearno preklapanje linija i koincidentni lukovi vraćaju 0 (vidi pojedinačne intersektore).
    /// </summary>
    public static int Intersect(ISegment first, ISegment second, Span<Point2> results)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        switch (first, second)
        {
            case (LineSeg la, LineSeg lb):
                if (LineLineIntersection.TryIntersect(la, lb, out var p))
                {
                    results[0] = p;
                    return 1;
                }

                return 0;

            case (LineSeg line, ArcSeg arc):
                return LineArcIntersection.Intersect(line, arc, results);

            case (ArcSeg arc, LineSeg line):
                return LineArcIntersection.Intersect(line, arc, results);

            case (ArcSeg aa, ArcSeg ab):
                return ArcArcIntersection.Intersect(aa, ab, results);

            default:
                throw new NotSupportedException(
                    FormattableString.Invariant($"Nepodržana kombinacija segmenata: {first.GetType().Name} × {second.GetType().Name}."));
        }
    }
}
