using PlasmaCAM.Kernel.Primitives;

namespace PlasmaCAM.Kernel;

/// <summary>Zajedničke matematičke funkcije koje ne pripadaju nijednom primitivu.</summary>
public static class MathUtil
{
    /// <summary>Linearna interpolacija: a za t=0, b za t=1. t nije ograničen na [0,1].</summary>
    public static double Lerp(double a, double b, double t) => a + (b - a) * t;

    /// <summary>Normalizira kut u raspon [0, 2π).</summary>
    public static double NormalizeAngle(double angle)
    {
        var r = angle % Math.Tau;
        if (r < 0)
        {
            r += Math.Tau;
        }

        // Floating-point zaštita: -1e-18 % Tau može vratiti točno Tau nakon korekcije.
        return r >= Math.Tau ? 0.0 : r;
    }

    /// <summary>Normalizira kut u raspon (-π, π].</summary>
    public static double NormalizeAngleSigned(double angle)
    {
        var r = NormalizeAngle(angle);
        return r > Math.PI ? r - Math.Tau : r;
    }

    public static double DegToRad(double degrees) => degrees * (Math.PI / 180.0);

    public static double RadToDeg(double radians) => radians * (180.0 / Math.PI);

    /// <summary>
    /// Predznačena površina poligona (shoelace formula). Pozitivna za CCW orijentaciju,
    /// negativna za CW. Poligon je definiran vrhovima; zadnji se implicitno spaja s prvim.
    /// </summary>
    public static double SignedArea(IReadOnlyList<Point2> vertices)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        if (vertices.Count < 3)
        {
            return 0.0;
        }

        var sum = 0.0;
        for (var i = 0; i < vertices.Count; i++)
        {
            var a = vertices[i];
            var b = vertices[(i + 1) % vertices.Count];
            sum += (a.X * b.Y) - (b.X * a.Y);
        }

        return sum / 2.0;
    }

    /// <summary>Je li poligon orijentiran suprotno kazaljci na satu (CCW).</summary>
    public static bool IsCcw(IReadOnlyList<Point2> vertices) => SignedArea(vertices) > 0.0;
}
