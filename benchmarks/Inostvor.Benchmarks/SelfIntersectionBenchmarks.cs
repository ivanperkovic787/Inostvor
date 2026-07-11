using BenchmarkDotNet.Attributes;
using Inostvor.Kernel.Intersections;
using Inostvor.Kernel.Primitives;

namespace Inostvor.Benchmarks;

/// <summary>
/// Sweep-and-prune detekcija samopresjeka na nasumičnom hodu (deterministički seed).
/// Čuva prag performansi za SelfIntersectionRule validatora (M3).
/// </summary>
[MemoryDiagnoser]
public class SelfIntersectionBenchmarks
{
    private Polyline2 _polyline = null!;

    [Params(100, 1000, 5000)]
    public int SegmentCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(1234);
        var segments = new List<ISegment>(SegmentCount);
        var current = new Point2(0, 0);

        for (var i = 0; i < SegmentCount; i++)
        {
            var step = new Vector2((rng.NextDouble() * 4.0) - 2.0, (rng.NextDouble() * 4.0) - 2.0);
            if (step.Length < 0.2)
            {
                step = new Vector2(0.5, 0.5);
            }

            var next = current + step;
            segments.Add(new LineSeg(current, next));
            current = next;
        }

        _polyline = new Polyline2(segments, joinTolerance: 1e-9);
    }

    [Benchmark]
    public int Find() => PolylineSelfIntersection.Find(_polyline).Count;
}
