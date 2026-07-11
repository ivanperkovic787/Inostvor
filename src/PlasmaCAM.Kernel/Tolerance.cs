namespace PlasmaCAM.Kernel;

/// <summary>
/// JEDINO mjesto s epsilon konstantama u cijelom sustavu (ADR-001).
/// Zabranjeno je definiranje lokalnih epsilona ili usporedba doubleova s == po kodu.
/// Sve vrijednosti su u milimetrima odnosno radijanima.
/// </summary>
public static class Tolerance
{
    /// <summary>Geometrijska tolerancija: dvije koordinate/udaljenosti unutar ove vrijednosti smatraju se jednakima. [mm]</summary>
    public const double Geometric = 1e-6;

    /// <summary>Kutna tolerancija za usporedbe kutova i testove paralelnosti. [rad]</summary>
    public const double Angular = 1e-9;

    /// <summary>Relativna tolerancija za usporedbe skaliranih veličina (npr. konformnost matrica). Bezdimenzionalna.</summary>
    public const double Relative = 1e-9;

    /// <summary>
    /// Zadana tolerancija spajanja kontura (gap između krajeva segmenata koji se još
    /// smatraju povezanima). Podesiva po dokumentu; ovo je samo default. [mm]
    /// </summary>
    public const double DefaultContourJoin = 0.05;

    /// <summary>Jesu li dvije skalarne vrijednosti jednake unutar tolerancije.</summary>
    public static bool AreEqual(double a, double b, double tolerance = Geometric)
        => Math.Abs(a - b) <= tolerance;

    /// <summary>Je li vrijednost nula unutar tolerancije.</summary>
    public static bool IsZero(double value, double tolerance = Geometric)
        => Math.Abs(value) <= tolerance;
}
