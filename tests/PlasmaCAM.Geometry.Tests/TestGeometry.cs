using PlasmaCAM.Core.Model.Import;
using PlasmaCAM.Kernel.Primitives;

namespace PlasmaCAM.Geometry.Tests;

/// <summary>Pomoćnici za građenje ulaza testova.</summary>
internal static class TestGeometry
{
    public static ImportedEntity Entity(string layer, params ISegment[] segments)
        => new(segments, layer, "TEST", null);

    public static LineSeg L(double x1, double y1, double x2, double y2)
        => new(new Point2(x1, y1), new Point2(x2, y2));

    /// <summary>Kvadrat iz 4 linije, CCW, donji-lijevi kut u (x, y).</summary>
    public static LineSeg[] SquareLines(double x, double y, double size) =>
    [
        L(x, y, x + size, y),
        L(x + size, y, x + size, y + size),
        L(x + size, y + size, x, y + size),
        L(x, y + size, x, y),
    ];

    public static ArcSeg FullCircle(double cx, double cy, double r, bool ccw = true)
        => new(new Point2(cx, cy), r, 0.0, ccw ? Math.Tau : -Math.Tau);
}
