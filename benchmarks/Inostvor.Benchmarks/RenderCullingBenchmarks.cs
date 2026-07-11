using BenchmarkDotNet.Attributes;
using Inostvor.Core.Model.Geometry;
using Inostvor.Kernel.Primitives;
using Inostvor.Rendering.Scene;

namespace Inostvor.Benchmarks;

/// <summary>
/// Renderer je projektiran za stotine tisuća segmenata: mjeri se trošak jednog
/// framea culling upita (AabbTree) nad 120 000 segmenata pri raznim zoomovima.
/// </summary>
[MemoryDiagnoser]
public class RenderCullingBenchmarks
{
    private RenderScene _scene = null!;
    private readonly List<SceneSegment> _buffer = new(65536);

    [GlobalSetup]
    public void Setup()
    {
        // 30 000 kvadrata × 4 segmenta = 120 000 segmenata na mreži ~175 × 175.
        var contours = new List<Contour>();
        const int cols = 175;
        for (var p = 0; p < 30_000; p++)
        {
            var x = (p % cols) * 20.0;
            var y = (p / cols) * 20.0;
            var polyline = new Polyline2(
            [
                new LineSeg(new Point2(x, y), new Point2(x + 15, y)),
                new LineSeg(new Point2(x + 15, y), new Point2(x + 15, y + 15)),
                new LineSeg(new Point2(x + 15, y + 15), new Point2(x, y + 15)),
                new LineSeg(new Point2(x, y + 15), new Point2(x, y)),
            ]);
            contours.Add(new Contour(p, polyline, "0", ContourKind.Outer, closedByTolerance: false));
        }

        _scene = new RenderScene(contours, []);
    }

    [Benchmark]
    public int CullFullView()
    {
        _buffer.Clear();
        _scene.QueryVisible(_scene.Bounds, _buffer);
        return _buffer.Count;
    }

    [Benchmark]
    public int CullZoomedTo1Percent()
    {
        _buffer.Clear();
        _scene.QueryVisible(new Aabb(1000, 1000, 1350, 1350), _buffer);
        return _buffer.Count;
    }
}
