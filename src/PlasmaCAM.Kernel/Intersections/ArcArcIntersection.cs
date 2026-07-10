using PlasmaCAM.Kernel.Primitives;

namespace PlasmaCAM.Kernel.Intersections;

/// <summary>Presjek dvaju kružnih lukova.</summary>
public static class ArcArcIntersection
{
    /// <summary>
    /// Upisuje do 2 presječne točke u <paramref name="results"/> (kapacitet ≥ 2) i vraća broj.
    /// OGRANIČENJE (dokumentirano): koincidentne kružnice (isti centar i polumjer) imaju
    /// beskonačno presjeka i vraćaju 0 — preklapanje lukova rješava viši sloj (validator, M3+).
    /// </summary>
    public static int Intersect(ArcSeg a, ArcSeg b, Span<Point2> results)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (results.Length < 2)
        {
            throw new ArgumentException("Buffer mora imati kapacitet za barem 2 točke.", nameof(results));
        }

        var c12 = b.Center - a.Center;
        var d = c12.Length;

        // Koincidentne kružnice → 0 (vidi XML doc).
        if (d < Tolerance.Geometric && Tolerance.AreEqual(a.Radius, b.Radius))
        {
            return 0;
        }

        // Koncentrične s različitim polumjerima → nema presjeka.
        if (d < Tolerance.Geometric)
        {
            return 0;
        }

        // Predaleko ili jedna unutar druge (s tolerancijom za tangencijalne dodire).
        if (d > a.Radius + b.Radius + Tolerance.Geometric || d < Math.Abs(a.Radius - b.Radius) - Tolerance.Geometric)
        {
            return 0;
        }

        var along = ((d * d) + (a.Radius * a.Radius) - (b.Radius * b.Radius)) / (2.0 * d);
        var h2 = (a.Radius * a.Radius) - (along * along);
        var h = h2 > 0.0 ? Math.Sqrt(h2) : 0.0; // clamp: tangencijalni slučaj unutar tolerancije

        var u = c12 / d;
        var mid = a.Center + (u * along);

        Span<Point2> candidates = stackalloc Point2[2];
        int candidateCount;
        if (h <= Tolerance.Geometric)
        {
            candidates[0] = mid;
            candidateCount = 1;
        }
        else
        {
            var offset = u.Perpendicular() * h;
            candidates[0] = mid + offset;
            candidates[1] = mid - offset;
            candidateCount = 2;
        }

        var count = 0;
        for (var i = 0; i < candidateCount; i++)
        {
            var p = candidates[i];
            if (!a.ContainsAngle((p - a.Center).Angle) || !b.ContainsAngle((p - b.Center).Angle))
            {
                continue;
            }

            if (count == 1 && results[0].AlmostEquals(p))
            {
                continue;
            }

            results[count++] = p;
        }

        return count;
    }
}
