namespace Inostvor.Kernel.Primitives;

/// <summary>
/// 2D vektor. Sintetizirana jednakost je EGZAKTNA — za geometrijske usporedbe
/// usporediti komponente kroz <see cref="Tolerance"/>.
/// </summary>
public readonly record struct Vector2(double X, double Y)
{
    public static Vector2 Zero => new(0.0, 0.0);

    public static Vector2 UnitX => new(1.0, 0.0);

    public static Vector2 UnitY => new(0.0, 1.0);

    public double Length => Math.Sqrt(LengthSquared);

    public double LengthSquared => (X * X) + (Y * Y);

    /// <summary>Kut vektora u odnosu na +X os, raspon (-π, π] (atan2 konvencija).</summary>
    public double Angle => Math.Atan2(Y, X);

    public double Dot(Vector2 other) => (X * other.X) + (Y * other.Y);

    /// <summary>Skalarni 2D "cross" (z-komponenta 3D vektorskog produkta). Pozitivan ako je <paramref name="other"/> lijevo od ovog vektora.</summary>
    public double Cross(Vector2 other) => (X * other.Y) - (Y * other.X);

    /// <summary>Lijeva okomica (rotacija za +90°): (X, Y) → (-Y, X).</summary>
    public Vector2 Perpendicular() => new(-Y, X);

    /// <summary>
    /// Jedinični vektor istog smjera. Baca <see cref="InvalidOperationException"/> ako je
    /// duljina manja od <see cref="Tolerance.Geometric"/> (smjer nedefiniran).
    /// </summary>
    public Vector2 Normalized()
    {
        var len = Length;
        if (len < Tolerance.Geometric)
        {
            throw new InvalidOperationException("Vektor je (gotovo) nul-vektor — smjer nije definiran.");
        }

        return new Vector2(X / len, Y / len);
    }

    /// <summary>Vektor rotiran za zadani kut (pozitivno = CCW).</summary>
    public Vector2 Rotated(double angle)
    {
        var c = Math.Cos(angle);
        var s = Math.Sin(angle);
        return new Vector2((X * c) - (Y * s), (X * s) + (Y * c));
    }

    /// <summary>Jedinični vektor pod zadanim kutom u odnosu na +X os.</summary>
    public static Vector2 FromAngle(double angle) => new(Math.Cos(angle), Math.Sin(angle));

    public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);

    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);

    public static Vector2 operator -(Vector2 v) => new(-v.X, -v.Y);

    public static Vector2 operator *(Vector2 v, double scalar) => new(v.X * scalar, v.Y * scalar);

    public static Vector2 operator *(double scalar, Vector2 v) => v * scalar;

    public static Vector2 operator /(Vector2 v, double scalar) => new(v.X / scalar, v.Y / scalar);

    public override string ToString() => FormattableString.Invariant($"<{X:0.######}, {Y:0.######}>");
}
