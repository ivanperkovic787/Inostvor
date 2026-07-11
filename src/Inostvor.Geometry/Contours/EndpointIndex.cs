using Inostvor.Kernel.Primitives;

namespace Inostvor.Geometry.Contours;

/// <summary>Kandidat za nastavak lanca: razmak, indeks segmenta i koji je kraj pogođen.</summary>
internal readonly record struct EndpointCandidate(double Gap, int SegmentIndex, bool AtStart);

/// <summary>
/// Prostorni indeks krajnjih točaka (uniform grid, ćelija = tolerancija spajanja).
/// Točka unutar tolerancije od upita nužno leži u 3×3 susjedstvu ćelije upita.
/// DETERMINIZAM: ćelije se obilaze fiksnim redoslijedom, a najbolji kandidat bira
/// se strogim uređajem (Gap, SegmentIndex, AtStart) — enumeracija Dictionaryja
/// nikad ne utječe na rezultat.
/// </summary>
internal sealed class EndpointIndex
{
    private readonly double _cellSize;
    private readonly Dictionary<(long X, long Y), List<(int Seg, bool AtStart, Point2 Point)>> _cells = [];

    public EndpointIndex(double cellSize)
    {
        _cellSize = Math.Max(cellSize, 1e-6);
    }

    public void Add(Point2 point, int segmentIndex, bool atStart)
    {
        var key = Quantize(point);
        if (!_cells.TryGetValue(key, out var list))
        {
            list = [];
            _cells[key] = list;
        }

        list.Add((segmentIndex, atStart, point));
    }

    /// <summary>Najbliži NEISKORIŠTENI kraj unutar tolerancije; false ako ne postoji.</summary>
    public bool TryFindNearest(Point2 query, double tolerance, bool[] used, out EndpointCandidate best)
    {
        best = new EndpointCandidate(double.PositiveInfinity, -1, false);
        var (qx, qy) = Quantize(query);

        for (var dx = -1L; dx <= 1L; dx++)
        {
            for (var dy = -1L; dy <= 1L; dy++)
            {
                if (!_cells.TryGetValue((qx + dx, qy + dy), out var list))
                {
                    continue;
                }

                foreach (var (seg, atStart, point) in list)
                {
                    if (used[seg])
                    {
                        continue;
                    }

                    var gap = query.DistanceTo(point);
                    if (gap > tolerance)
                    {
                        continue;
                    }

                    // Strogi deterministički uređaj: manji razmak, pa manji indeks, pa Start prije Enda.
                    var isBetter = gap < best.Gap
                        || (gap == best.Gap && seg < best.SegmentIndex)
                        || (gap == best.Gap && seg == best.SegmentIndex && atStart && !best.AtStart);
                    if (isBetter)
                    {
                        best = new EndpointCandidate(gap, seg, atStart);
                    }
                }
            }
        }

        return best.SegmentIndex >= 0;
    }

    private (long X, long Y) Quantize(Point2 p)
        => ((long)Math.Floor(p.X / _cellSize), (long)Math.Floor(p.Y / _cellSize));
}
