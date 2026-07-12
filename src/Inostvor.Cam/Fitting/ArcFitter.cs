using Inostvor.Core.Abstractions;
using Inostvor.Kernel;
using Inostvor.Kernel.Primitives;

namespace Inostvor.Cam.Fitting;

/// <summary>
/// Pohlepni arc fitting s APSOLUTNOM garancijom točnosti: luk se emitira samo
/// ako SVE obuhvaćene ulazne točke leže unutar tolerancije od luka I kutovi su
/// strogo monotoni duž smjera obilaska. U protivnom ostaju linije. Kolinearni
/// nizovi komprimiraju se u jednu liniju (isti kriterij tolerancije).
///
/// Na svakoj poziciji računaju se maksimalni linijski i maksimalni lučni niz;
/// bira se dulji (tie → linija, jer je jednostavnija). Deterministički.
/// </summary>
public sealed class ArcFitter : IArcFitter
{
    /// <summary>Polumjer iznad ovoga tretira se kao pravac (numerička stabilnost G2/G3 centra).</summary>
    private const double MaxArcRadius = 1e5;

    private const int MinArcPoints = 4;

    public IReadOnlyList<ISegment> Fit(IReadOnlyList<Point2> points, bool closed, double tolerance)
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(tolerance, 0.0);

        // Zatvoreni prsten: radi na nizu p0..pn-1,p0 (šav se ne fita preko — konzervativno).
        var pts = new List<Point2>(points);
        if (closed && pts.Count > 1 && !pts[0].AlmostEquals(pts[^1]))
        {
            pts.Add(pts[0]);
        }

        var segments = new List<ISegment>();
        var i = 0;
        while (i < pts.Count - 1)
        {
            var lineEnd = ExtendLine(pts, i, tolerance);
            var arcEnd = ExtendArc(pts, i, tolerance, out var arc);

            if (arc is not null && arcEnd > lineEnd)
            {
                segments.Add(arc);
                i = arcEnd;
            }
            else
            {
                if (pts[i].DistanceTo(pts[lineEnd]) > Tolerance.Geometric)
                {
                    segments.Add(new LineSeg(pts[i], pts[lineEnd]));
                }

                i = lineEnd;
            }
        }

        return segments;
    }

    /// <summary>Najdalji j takav da su SVE točke i..j unutar tolerancije od tetive p[i]→p[j].</summary>
    private static int ExtendLine(List<Point2> pts, int i, double tolerance)
    {
        var j = i + 1;
        while (j + 1 < pts.Count)
        {
            var candidate = j + 1;
            if (pts[i].DistanceTo(pts[candidate]) <= Tolerance.Geometric)
            {
                break; // degenerirana tetiva
            }

            var chord = new LineSeg(pts[i], pts[candidate]);
            var allWithin = true;
            for (var k = i + 1; k < candidate; k++)
            {
                if (chord.DistanceTo(pts[k]) > tolerance)
                {
                    allWithin = false;
                    break;
                }
            }

            if (!allWithin)
            {
                break;
            }

            j = candidate;
        }

        return j;
    }

    /// <summary>
    /// Najdalji j za koji postoji VERIFICIRAN luk kroz p[i]..p[j]; vraća i sam luk.
    /// Verifikacija: |dist(centar, p[k]) − r| ≤ tol za sve k, monotoni kutovi, r ≤ MaxArcRadius.
    /// </summary>
    private static int ExtendArc(List<Point2> pts, int i, double tolerance, out ArcSeg? best)
    {
        best = null;
        var bestEnd = i + 1;

        var j = i + MinArcPoints - 1;
        while (j < pts.Count)
        {
            var arc = TryBuildVerifiedArc(pts, i, j, tolerance);
            if (arc is null)
            {
                break; // pohlepno: prvo proširenje koje ne prolazi zaustavlja rast
            }

            best = arc;
            bestEnd = j;
            j++;
        }

        return bestEnd;
    }

    private static ArcSeg? TryBuildVerifiedArc(List<Point2> pts, int i, int j, double tolerance)
    {
        // Kružnica kroz krajeve i točku najudaljeniju od tetive (stabilan izbor).
        var chord = pts[i].DistanceTo(pts[j]) > Tolerance.Geometric ? new LineSeg(pts[i], pts[j]) : null;
        if (chord is null)
        {
            return null;
        }

        var farIndex = i + 1;
        var farDistance = -1.0;
        for (var k = i + 1; k < j; k++)
        {
            var d = chord.DistanceTo(pts[k]);
            if (d > farDistance)
            {
                farDistance = d;
                farIndex = k;
            }
        }

        if (farDistance <= tolerance / 2.0)
        {
            return null; // praktički ravno — linija je ispravniji izbor
        }

        if (!TryCircumcenter(pts[i], pts[farIndex], pts[j], out var center))
        {
            return null;
        }

        var radius = center.DistanceTo(pts[i]);
        if (radius > MaxArcRadius || radius <= Tolerance.Geometric)
        {
            return null;
        }

        // Smjer: putovanje i → far → j.
        var a0 = (pts[i] - center).Angle;
        var aFar = MathUtil.NormalizeAngle((pts[farIndex] - center).Angle - a0);
        var aEnd = MathUtil.NormalizeAngle((pts[j] - center).Angle - a0);
        var isCcw = aFar < aEnd;

        // Verifikacija SVIH točaka: radijalno odstupanje + stroga monotonost kuta.
        var previousOffset = 0.0;
        for (var k = i; k <= j; k++)
        {
            if (Math.Abs(center.DistanceTo(pts[k]) - radius) > tolerance)
            {
                return null;
            }

            if (k == i)
            {
                continue;
            }

            var offset = MathUtil.NormalizeAngle((pts[k] - center).Angle - a0);
            var forward = isCcw ? offset : MathUtil.NormalizeAngle(-offset);
            var previousForward = isCcw ? previousOffset : MathUtil.NormalizeAngle(-previousOffset);
            if (k > i + 1 && forward <= previousForward)
            {
                return null; // kut se vraća — nije jednostavan luk
            }

            previousOffset = offset;
        }

        var sweep = isCcw ? aEnd : aEnd - Math.Tau;
        if (Math.Abs(sweep) >= Math.Tau - (Tolerance.Angular * 10.0))
        {
            return null; // puni krug preko fitanja nije dopušten (šav ostaje)
        }

        return new ArcSeg(center, radius, a0, sweep);
    }

    private static bool TryCircumcenter(Point2 a, Point2 b, Point2 c, out Point2 center)
    {
        var d = 2.0 * ((a.X * (b.Y - c.Y)) + (b.X * (c.Y - a.Y)) + (c.X * (a.Y - b.Y)));
        if (Math.Abs(d) < 1e-12)
        {
            center = default;
            return false;
        }

        var aa = (a.X * a.X) + (a.Y * a.Y);
        var bb = (b.X * b.X) + (b.Y * b.Y);
        var cc = (c.X * c.X) + (c.Y * c.Y);
        center = new Point2(
            ((aa * (b.Y - c.Y)) + (bb * (c.Y - a.Y)) + (cc * (a.Y - b.Y))) / d,
            ((aa * (c.X - b.X)) + (bb * (a.X - c.X)) + (cc * (b.X - a.X))) / d);
        return true;
    }
}
