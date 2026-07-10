namespace PlasmaCAM.Kernel.Primitives;

/// <summary>
/// Ravni segment. Nepromjenjiv; duljina se validira u konstruktoru —
/// degenerirani (nul-duljinski) segmenti se odbijaju odmah, na izvoru.
/// </summary>
public sealed class LineSeg : ISegment
{
    public LineSeg(Point2 start, Point2 end)
    {
        Length = start.DistanceTo(end);
        if (Length < Tolerance.Geometric)
        {
            throw new ArgumentException(
                FormattableString.Invariant($"Degeneriran segment: start {start} i end {end} se poklapaju unutar tolerancije."));
        }

        StartPoint = start;
        EndPoint = end;
    }

    public Point2 StartPoint { get; }

    public Point2 EndPoint { get; }

    public double Length { get; }

    /// <summary>Jedinični vektor smjera od starta prema endu.</summary>
    public Vector2 Direction => (EndPoint - StartPoint) / Length;

    public Aabb Bounds => Aabb.FromCorners(StartPoint, EndPoint);

    public Point2 PointAt(double t) => Point2.Lerp(StartPoint, EndPoint, t);

    public Point2 ClosestPoint(Point2 point)
    {
        var d = EndPoint - StartPoint;
        var t = (point - StartPoint).Dot(d) / d.LengthSquared;
        return PointAt(Math.Clamp(t, 0.0, 1.0));
    }

    public double DistanceTo(Point2 point) => point.DistanceTo(ClosestPoint(point));

    public ISegment Reversed() => new LineSeg(EndPoint, StartPoint);

    public override string ToString()
        => FormattableString.Invariant($"Line {StartPoint} -> {EndPoint}");
}
