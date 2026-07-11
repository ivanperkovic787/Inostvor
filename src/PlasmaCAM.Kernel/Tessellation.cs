using PlasmaCAM.Kernel.Primitives;

namespace PlasmaCAM.Kernel;

/// <summary>Tessellacija krivulja u tetive s kontroliranim maksimalnim odstupanjem (sagitta).</summary>
public static class Tessellation
{
    /// <summary>Sigurnosna gornja granica broja tetiva po luku (patološki mali tolerance ne smije eksplodirati memoriju).</summary>
    public const int MaxChordsPerArc = 4096;

    /// <summary>
    /// Točke po luku takve da odstupanje tetive od luka (sagitta) ne prelazi
    /// <paramref name="chordTolerance"/>. Prva i zadnja točka su egzaktni krajevi luka.
    /// </summary>
    public static IReadOnlyList<Point2> TessellateArc(ArcSeg arc, double chordTolerance)
    {
        ArgumentNullException.ThrowIfNull(arc);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(chordTolerance, 0.0);

        // Sagitta s = r·(1 - cos(Δ/2))  →  Δ = 2·acos(1 - s/r).
        var ratio = Math.Min(chordTolerance / arc.Radius, 1.0);
        var maxStep = 2.0 * Math.Acos(1.0 - ratio);

        var chords = (int)Math.Ceiling(Math.Abs(arc.SweepAngle) / maxStep);
        chords = Math.Clamp(chords, 1, MaxChordsPerArc);

        var points = new Point2[chords + 1];
        for (var i = 0; i <= chords; i++)
        {
            points[i] = arc.PointAt((double)i / chords);
        }

        return points;
    }
}
