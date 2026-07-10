using System.Collections;

namespace PlasmaCAM.Kernel.Primitives;

/// <summary>
/// Uređeni niz POVEZANIH segmenata: početak svakog segmenta poklapa se s krajem
/// prethodnog unutar tolerancije spajanja (validira se u konstruktoru).
/// Zatvorenost se određuje istom tolerancijom. Nepromjenjiva.
/// </summary>
public sealed class Polyline2 : IReadOnlyList<ISegment>
{
    private readonly ISegment[] _segments;

    public Polyline2(IReadOnlyList<ISegment> segments, double joinTolerance = Tolerance.DefaultContourJoin)
    {
        ArgumentNullException.ThrowIfNull(segments);
        if (segments.Count == 0)
        {
            throw new ArgumentException("Polyline mora sadržavati barem jedan segment.", nameof(segments));
        }

        for (var i = 1; i < segments.Count; i++)
        {
            if (!segments[i].StartPoint.AlmostEquals(segments[i - 1].EndPoint, joinTolerance))
            {
                throw new ArgumentException(
                    FormattableString.Invariant(
                        $"Segment {i} nije povezan s prethodnim: gap {segments[i].StartPoint.DistanceTo(segments[i - 1].EndPoint):0.######} mm > tolerancija {joinTolerance} mm."),
                    nameof(segments));
            }
        }

        _segments = [.. segments];
        JoinTolerance = joinTolerance;
        IsClosed = _segments[^1].EndPoint.AlmostEquals(_segments[0].StartPoint, joinTolerance);

        var length = 0.0;
        var bounds = _segments[0].Bounds;
        foreach (var s in _segments)
        {
            length += s.Length;
            bounds = bounds.Union(s.Bounds);
        }

        Length = length;
        Bounds = bounds;
    }

    public int Count => _segments.Length;

    public ISegment this[int index] => _segments[index];

    public double JoinTolerance { get; }

    public bool IsClosed { get; }

    public double Length { get; }

    public Aabb Bounds { get; }

    public Point2 StartPoint => _segments[0].StartPoint;

    public Point2 EndPoint => _segments[^1].EndPoint;

    /// <summary>Vrhovi: početne točke svih segmenata + krajnja točka zadnjeg.</summary>
    public IEnumerable<Point2> Vertices
    {
        get
        {
            foreach (var s in _segments)
            {
                yield return s.StartPoint;
            }

            yield return _segments[^1].EndPoint;
        }
    }

    /// <summary>Polyline suprotnog smjera: segmenti obrnuti i preokrenuti.</summary>
    public Polyline2 Reversed()
    {
        var reversed = new ISegment[_segments.Length];
        for (var i = 0; i < _segments.Length; i++)
        {
            reversed[_segments.Length - 1 - i] = _segments[i].Reversed();
        }

        return new Polyline2(reversed, JoinTolerance);
    }

    public IEnumerator<ISegment> GetEnumerator() => ((IEnumerable<ISegment>)_segments).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
