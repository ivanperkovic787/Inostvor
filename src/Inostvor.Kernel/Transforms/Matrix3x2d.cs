using Inostvor.Kernel.Primitives;

namespace Inostvor.Kernel.Transforms;

/// <summary>
/// 2D afina transformacija u double preciznosti. Konvencija identična System.Numerics.Matrix3x2
/// (row-vector): x' = x·M11 + y·M21 + M31; y' = x·M12 + y·M22 + M32.
/// Kompozicija a * b znači "primijeni a, ZATIM b".
/// </summary>
public readonly record struct Matrix3x2d(double M11, double M12, double M21, double M22, double M31, double M32)
{
    public static Matrix3x2d Identity => new(1, 0, 0, 1, 0, 0);

    public double Determinant => (M11 * M22) - (M12 * M21);

    public Point2 TransformPoint(Point2 p)
        => new((p.X * M11) + (p.Y * M21) + M31, (p.X * M12) + (p.Y * M22) + M32);

    /// <summary>Transformira vektor (smjer) — translacijski dio se ignorira.</summary>
    public Vector2 TransformVector(Vector2 v)
        => new((v.X * M11) + (v.Y * M21), (v.X * M12) + (v.Y * M22));

    public static Matrix3x2d CreateTranslation(double dx, double dy) => new(1, 0, 0, 1, dx, dy);

    public static Matrix3x2d CreateTranslation(Vector2 offset) => CreateTranslation(offset.X, offset.Y);

    /// <summary>Rotacija oko ishodišta; pozitivan kut je CCW.</summary>
    public static Matrix3x2d CreateRotation(double angle)
    {
        var c = Math.Cos(angle);
        var s = Math.Sin(angle);
        return new Matrix3x2d(c, s, -s, c, 0, 0);
    }

    /// <summary>Rotacija oko zadane točke.</summary>
    public static Matrix3x2d CreateRotation(double angle, Point2 center)
        => CreateTranslation(-center.X, -center.Y) * CreateRotation(angle) * CreateTranslation(center.X, center.Y);

    public static Matrix3x2d CreateScale(double sx, double sy) => new(sx, 0, 0, sy, 0, 0);

    /// <summary>Skaliranje oko zadane točke. Negativan faktor = zrcaljenje po pripadnoj osi.</summary>
    public static Matrix3x2d CreateScale(double sx, double sy, Point2 center)
        => CreateTranslation(-center.X, -center.Y) * CreateScale(sx, sy) * CreateTranslation(center.X, center.Y);

    /// <summary>Kompozicija: rezultat primjenjuje <paramref name="a"/> pa zatim <paramref name="b"/>.</summary>
    public static Matrix3x2d operator *(Matrix3x2d a, Matrix3x2d b) => new(
        (a.M11 * b.M11) + (a.M12 * b.M21),
        (a.M11 * b.M12) + (a.M12 * b.M22),
        (a.M21 * b.M11) + (a.M22 * b.M21),
        (a.M21 * b.M12) + (a.M22 * b.M22),
        (a.M31 * b.M11) + (a.M32 * b.M21) + b.M31,
        (a.M31 * b.M12) + (a.M32 * b.M22) + b.M32);

    /// <summary>
    /// Pokušaj inverzije. Vraća false (i Identity u <paramref name="inverse"/>) ako je
    /// matrica singularna (|det| ispod numeričkog praga).
    /// </summary>
    public bool TryInvert(out Matrix3x2d inverse)
    {
        var det = Determinant;
        if (Math.Abs(det) < 1e-12)
        {
            inverse = Identity;
            return false;
        }

        var inv = 1.0 / det;
        inverse = new Matrix3x2d(
            M22 * inv,
            -M12 * inv,
            -M21 * inv,
            M11 * inv,
            ((M21 * M32) - (M22 * M31)) * inv,
            ((M12 * M31) - (M11 * M32)) * inv);
        return true;
    }
}
