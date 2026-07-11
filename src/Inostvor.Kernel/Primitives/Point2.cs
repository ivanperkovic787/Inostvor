namespace Inostvor.Kernel.Primitives;

/// <summary>
/// 2D točka u milimetrima (ADR-001: double u domeni).
/// Sintetizirana jednakost (==, Equals) je EGZAKTNA bitovna usporedba — za geometrijske
/// usporedbe koristiti <see cref="AlmostEquals"/>.
/// </summary>
public readonly record struct Point2(double X, double Y)
{
    public static Point2 Origin => new(0.0, 0.0);

    public double DistanceTo(Point2 other) => Math.Sqrt(DistanceSquaredTo(other));

    public double DistanceSquaredTo(Point2 other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return (dx * dx) + (dy * dy);
    }

    /// <summary>Geometrijska jednakost: euklidska udaljenost unutar tolerancije.</summary>
    public bool AlmostEquals(Point2 other, double tolerance = Tolerance.Geometric)
        => DistanceTo(other) <= tolerance;

    public Point2 MidPointTo(Point2 other) => new((X + other.X) / 2.0, (Y + other.Y) / 2.0);

    public static Point2 Lerp(Point2 a, Point2 b, double t)
        => new(MathUtil.Lerp(a.X, b.X, t), MathUtil.Lerp(a.Y, b.Y, t));

    public static Point2 operator +(Point2 p, Vector2 v) => new(p.X + v.X, p.Y + v.Y);

    public static Point2 operator -(Point2 p, Vector2 v) => new(p.X - v.X, p.Y - v.Y);

    /// <summary>Razlika dviju točaka je vektor od <paramref name="b"/> prema <paramref name="a"/>.</summary>
    public static Vector2 operator -(Point2 a, Point2 b) => new(a.X - b.X, a.Y - b.Y);

    public override string ToString() => FormattableString.Invariant($"({X:0.######}, {Y:0.######})");
}
