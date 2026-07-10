namespace PlasmaCAM.Kernel.Primitives;

/// <summary>
/// Axis-aligned bounding box. Uvijek validan (Min ≤ Max) — konstruktor to garantira.
/// Nema "prazne" sentinel vrijednosti: gdje AABB može izostati, koristiti <c>Aabb?</c>.
/// </summary>
public readonly record struct Aabb
{
    public Aabb(double minX, double minY, double maxX, double maxY)
    {
        if (minX > maxX || minY > maxY)
        {
            throw new ArgumentException(
                FormattableString.Invariant($"Neispravan AABB: min ({minX}, {minY}) > max ({maxX}, {maxY})."));
        }

        MinX = minX;
        MinY = minY;
        MaxX = maxX;
        MaxY = maxY;
    }

    public double MinX { get; }

    public double MinY { get; }

    public double MaxX { get; }

    public double MaxY { get; }

    public double Width => MaxX - MinX;

    public double Height => MaxY - MinY;

    public Point2 Center => new((MinX + MaxX) / 2.0, (MinY + MaxY) / 2.0);

    /// <summary>Opseg (2·(w+h)) — metrika cijene za BVH heuristiku (bolja od površine za degenerirane kutije).</summary>
    public double Perimeter => 2.0 * (Width + Height);

    public bool Contains(Point2 point, double tolerance = 0.0)
        => point.X >= MinX - tolerance && point.X <= MaxX + tolerance
        && point.Y >= MinY - tolerance && point.Y <= MaxY + tolerance;

    public bool Contains(Aabb other)
        => other.MinX >= MinX && other.MaxX <= MaxX && other.MinY >= MinY && other.MaxY <= MaxY;

    /// <summary>Presijecanje uključuje i dodirivanje rubova (zatvoreni intervali).</summary>
    public bool Intersects(Aabb other, double tolerance = 0.0)
        => other.MinX <= MaxX + tolerance && other.MaxX >= MinX - tolerance
        && other.MinY <= MaxY + tolerance && other.MaxY >= MinY - tolerance;

    public Aabb Union(Aabb other) => new(
        Math.Min(MinX, other.MinX), Math.Min(MinY, other.MinY),
        Math.Max(MaxX, other.MaxX), Math.Max(MaxY, other.MaxY));

    public static Aabb Union(Aabb a, Aabb b) => a.Union(b);

    /// <summary>Proširuje (ili sužava za negativan iznos) kutiju za margin sa svih strana.</summary>
    public Aabb Inflate(double margin) => new(MinX - margin, MinY - margin, MaxX + margin, MaxY + margin);

    /// <summary>AABB iz dvije točke u bilo kojem poretku.</summary>
    public static Aabb FromCorners(Point2 a, Point2 b) => new(
        Math.Min(a.X, b.X), Math.Min(a.Y, b.Y),
        Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));

    /// <summary>AABB koji obuhvaća sve točke. Baca <see cref="ArgumentException"/> za prazan skup.</summary>
    public static Aabb FromPoints(IEnumerable<Point2> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        var any = false;
        double minX = 0, minY = 0, maxX = 0, maxY = 0;
        foreach (var p in points)
        {
            if (!any)
            {
                minX = maxX = p.X;
                minY = maxY = p.Y;
                any = true;
                continue;
            }

            if (p.X < minX) { minX = p.X; }
            if (p.X > maxX) { maxX = p.X; }
            if (p.Y < minY) { minY = p.Y; }
            if (p.Y > maxY) { maxY = p.Y; }
        }

        if (!any)
        {
            throw new ArgumentException("AABB se ne može izgraditi iz praznog skupa točaka.", nameof(points));
        }

        return new Aabb(minX, minY, maxX, maxY);
    }
}
