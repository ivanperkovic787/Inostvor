using Inostvor.Kernel.Primitives;

namespace Inostvor.Kernel.Intersections;

/// <summary>Presjek ravnog segmenta i kružnog luka.</summary>
public static class LineArcIntersection
{
    /// <summary>
    /// Upisuje do 2 presječne točke u <paramref name="results"/> (kapacitet mora biti ≥ 2)
    /// i vraća njihov broj. Tangencijalni dodir daje jednu točku (snapanu na kružnicu).
    /// </summary>
    public static int Intersect(LineSeg line, ArcSeg arc, Span<Point2> results)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentNullException.ThrowIfNull(arc);
        if (results.Length < 2)
        {
            throw new ArgumentException("Buffer mora imati kapacitet za barem 2 točke.", nameof(results));
        }

        var d = line.EndPoint - line.StartPoint;
        var f = line.StartPoint - arc.Center;

        var a = d.Dot(d);
        var b = 2.0 * f.Dot(d);
        var c = f.Dot(f) - (arc.Radius * arc.Radius);
        var disc = (b * b) - (4.0 * a * c);

        var tTol = Tolerance.Geometric / line.Length;
        var count = 0;

        if (disc > 0.0)
        {
            var sq = Math.Sqrt(disc);
            Span<double> ts = [(-b - sq) / (2.0 * a), (-b + sq) / (2.0 * a)];
            foreach (var t in ts)
            {
                if (t < -tTol || t > 1.0 + tTol)
                {
                    continue;
                }

                var p = line.PointAt(Math.Clamp(t, 0.0, 1.0));
                if (!arc.ContainsAngle((p - arc.Center).Angle))
                {
                    continue;
                }

                if (count == 1 && results[0].AlmostEquals(p))
                {
                    continue; // gotovo tangencijalno — dedupliciraj
                }

                results[count++] = p;
            }

            return count;
        }

        // disc <= 0: možda tangenta unutar tolerancije. Najbliža točka pravca centru:
        var tClosest = -b / (2.0 * a);
        if (tClosest < -tTol || tClosest > 1.0 + tTol)
        {
            return 0;
        }

        var closest = line.PointAt(Math.Clamp(tClosest, 0.0, 1.0));
        var toCenter = closest - arc.Center;
        var dist = toCenter.Length;
        if (!Tolerance.AreEqual(dist, arc.Radius))
        {
            return 0;
        }

        // Snap na kružnicu (dist > 0 garantirano jer je radius >= Tolerance.Geometric i |dist - r| <= Geometric).
        var snapped = arc.Center + (toCenter * (arc.Radius / dist));
        if (!arc.ContainsAngle((snapped - arc.Center).Angle))
        {
            return 0;
        }

        results[0] = snapped;
        return 1;
    }
}
