using Inostvor.Kernel.Primitives;

namespace Inostvor.Kernel.Transforms;

/// <summary>
/// Transformacija segmenata afinom matricom. Lukovi ostaju lukovi za KONFORMNE matrice
/// (uniformna skala + rotacija + translacija ± zrcaljenje); za neuniformnu skalu luk
/// prelazi u elipsu koju eksplicitno tesselliramo (uz signal pozivatelju).
/// </summary>
public static class SegmentTransform
{
    /// <summary>
    /// Je li matrica konformna (čuva oblik: kutovi i omjeri duljina).
    /// <paramref name="scale"/> vraća apsolutni uniformni faktor skale.
    /// </summary>
    public static bool IsConformal(in Matrix3x2d m, out double scale)
    {
        var basisX = new Vector2(m.M11, m.M12);
        var basisY = new Vector2(m.M21, m.M22);
        var lenX = basisX.Length;
        var lenY = basisY.Length;
        scale = (lenX + lenY) / 2.0;

        if (lenX < Tolerance.Geometric || lenY < Tolerance.Geometric)
        {
            return false; // degenerirana (kolaps u pravac/točku)
        }

        var maxLen = Math.Max(lenX, lenY);
        return Math.Abs(lenX - lenY) <= Tolerance.Relative * maxLen
            && Math.Abs(basisX.Dot(basisY)) <= Tolerance.Relative * lenX * lenY;
    }

    /// <summary>
    /// Transformira segment. Za lukove pod nekonformnom matricom vraća tessellirane
    /// linijske segmente (<paramref name="tessellated"/> = true). Vraća PRAZNU listu
    /// ako je rezultat degeneriran (npr. luk kolabiran skalom ~0) — pozivatelj odlučuje
    /// o upozorenju.
    /// </summary>
    public static IReadOnlyList<ISegment> Transform(ISegment segment, in Matrix3x2d m, double tessellationTolerance, out bool tessellated)
    {
        ArgumentNullException.ThrowIfNull(segment);
        tessellated = false;

        switch (segment)
        {
            case LineSeg line:
            {
                var p1 = m.TransformPoint(line.StartPoint);
                var p2 = m.TransformPoint(line.EndPoint);
                return p1.DistanceTo(p2) < Tolerance.Geometric ? [] : [new LineSeg(p1, p2)];
            }

            case ArcSeg arc when IsConformal(m, out var scale):
            {
                if (arc.Radius * scale < Tolerance.Geometric)
                {
                    return [];
                }

                var ccw = arc.IsCcw ^ (m.Determinant < 0.0); // zrcaljenje obrće smjer
                var center = m.TransformPoint(arc.Center);

                if (arc.IsFullCircle)
                {
                    var onCircle = m.TransformPoint(arc.StartPoint);
                    return [ArcSeg.FromStartEndCenter(onCircle, onCircle, center, ccw)];
                }

                var start = m.TransformPoint(arc.StartPoint);
                var end = m.TransformPoint(arc.EndPoint);
                return [ArcSeg.FromStartEndCenter(start, end, center, ccw)];
            }

            case ArcSeg arc:
            {
                tessellated = true;
                var points = Tessellation.TessellateArc(arc, tessellationTolerance);
                var result = new List<ISegment>(points.Count - 1);
                var previous = m.TransformPoint(points[0]);
                for (var i = 1; i < points.Count; i++)
                {
                    var current = m.TransformPoint(points[i]);
                    if (previous.DistanceTo(current) >= Tolerance.Geometric)
                    {
                        result.Add(new LineSeg(previous, current));
                        previous = current;
                    }
                }

                return result;
            }

            default:
                throw new NotSupportedException(
                    FormattableString.Invariant($"Nepodržan tip segmenta za transformaciju: {segment.GetType().Name}."));
        }
    }
}
