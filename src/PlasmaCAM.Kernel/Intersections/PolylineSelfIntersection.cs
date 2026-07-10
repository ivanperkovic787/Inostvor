using PlasmaCAM.Kernel.Primitives;

namespace PlasmaCAM.Kernel.Intersections;

/// <summary>Jedan pronađeni samopresjek: indeksi segmenata (A &lt; B) i točka presjeka.</summary>
public readonly record struct SelfIntersectionHit(int SegmentA, int SegmentB, Point2 Point);

/// <summary>
/// Detekcija samopresijecanja polyline-a algoritmom sweep-and-prune po X osi:
/// segmenti sortirani po MinX, aktivna lista obrezuje se po MaxX, kandidati se
/// filtriraju Y-preklapanjem AABB-ova pa tek onda egzaktno presijecaju.
/// Složenost O(n log n + k·m) — za CAM geometrije praktički O(n log n), bez
/// slučajeva degeneracije koji muče punu Bentley–Ottmann implementaciju.
/// </summary>
public static class PolylineSelfIntersection
{
    /// <summary>
    /// Vraća sve samopresjeke. SUSJEDNI segmenti (dijele vrh, uključujući zatvarajući par
    /// zatvorene polyline) se preskaču — njihov zajednički vrh nije samopresjek.
    /// </summary>
    public static IReadOnlyList<SelfIntersectionHit> Find(Polyline2 polyline)
    {
        ArgumentNullException.ThrowIfNull(polyline);

        var n = polyline.Count;
        var hits = new List<SelfIntersectionHit>();
        if (n < 2)
        {
            return hits;
        }

        var bounds = new Aabb[n];
        for (var i = 0; i < n; i++)
        {
            bounds[i] = polyline[i].Bounds;
        }

        var order = new int[n];
        for (var i = 0; i < n; i++)
        {
            order[i] = i;
        }

        Array.Sort(order, (x, y) => bounds[x].MinX.CompareTo(bounds[y].MinX));

        var active = new List<int>();
        Span<Point2> buffer = stackalloc Point2[2];

        foreach (var i in order)
        {
            // Izbaci segmente koji su po X osi u potpunosti lijevo od trenutnog.
            for (var k = active.Count - 1; k >= 0; k--)
            {
                if (bounds[active[k]].MaxX < bounds[i].MinX - Tolerance.Geometric)
                {
                    active.RemoveAt(k);
                }
            }

            foreach (var j in active)
            {
                if (!bounds[i].Intersects(bounds[j], Tolerance.Geometric))
                {
                    continue;
                }

                if (AreAdjacent(i, j, n, polyline.IsClosed))
                {
                    continue;
                }

                var count = SegmentIntersection.Intersect(polyline[i], polyline[j], buffer);
                for (var k = 0; k < count; k++)
                {
                    hits.Add(new SelfIntersectionHit(Math.Min(i, j), Math.Max(i, j), buffer[k]));
                }
            }

            active.Add(i);
        }

        return hits;
    }

    private static bool AreAdjacent(int i, int j, int count, bool isClosed)
    {
        if (Math.Abs(i - j) == 1)
        {
            return true;
        }

        return isClosed && ((i == 0 && j == count - 1) || (j == 0 && i == count - 1));
    }
}
