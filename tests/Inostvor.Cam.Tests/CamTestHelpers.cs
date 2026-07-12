using Inostvor.Core.Model.Geometry;
using Inostvor.Core.Model.Import;
using Inostvor.Geometry.Contours;
using Inostvor.Kernel.Primitives;

namespace Inostvor.Cam.Tests;

internal static class CamTestHelpers
{
    private static readonly ContourBuilder Builder = new();
    private static readonly ContourClassifier Classifier = new();

    public static LineSeg L(double x1, double y1, double x2, double y2)
        => new(new Point2(x1, y1), new Point2(x2, y2));

    public static ISegment[] SquareLines(double x, double y, double size) =>
    [
        L(x, y, x + size, y),
        L(x + size, y, x + size, y + size),
        L(x + size, y + size, x, y + size),
        L(x, y + size, x, y),
    ];

    public static ArcSeg FullCircle(double cx, double cy, double r, bool ccw = true)
        => new(new Point2(cx, cy), r, 0.0, ccw ? Math.Tau : -Math.Tau);

    /// <summary>Konture kroz STVARNI M3 pipeline (builder + classifier) — CAM testira nad istim modelom.</summary>
    public static IReadOnlyList<Contour> Contours(params (string Layer, ISegment[] Segments)[] entities)
    {
        var imported = entities.Select(e => new ImportedEntity(e.Segments, e.Layer, "TEST", null)).ToList();
        return Classifier.Classify(Builder.Build(imported, ContourBuildSettings.Default).Contours);
    }

    public static double MinDistanceToPath(Point2 point, IReadOnlyList<ISegment> path)
        => path.Min(s => point.DistanceTo(s.ClosestPoint(point)));
}
