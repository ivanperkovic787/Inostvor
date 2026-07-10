using BenchmarkDotNet.Attributes;
using PlasmaCAM.Kernel.Intersections;
using PlasmaCAM.Kernel.Primitives;

namespace PlasmaCAM.Benchmarks;

/// <summary>Mikro-benchmarki pojedinačnih presjeka — jezgra CAM validacije i trimanja.</summary>
[MemoryDiagnoser]
public class IntersectionBenchmarks
{
    private LineSeg _lineA = null!;
    private LineSeg _lineB = null!;
    private ArcSeg _arcA = null!;
    private ArcSeg _arcB = null!;

    [GlobalSetup]
    public void Setup()
    {
        _lineA = new LineSeg(new Point2(0, 0), new Point2(10, 10));
        _lineB = new LineSeg(new Point2(0, 10), new Point2(10, 0));
        _arcA = new ArcSeg(new Point2(0, 0), 5.0, 0.0, Math.Tau);
        _arcB = new ArcSeg(new Point2(6, 0), 5.0, 0.0, Math.Tau);
    }

    [Benchmark]
    public bool LineLine() => LineLineIntersection.TryIntersect(_lineA, _lineB, out _);

    [Benchmark]
    public int LineArc()
    {
        Span<Point2> buf = stackalloc Point2[2];
        return LineArcIntersection.Intersect(_lineA, _arcA, buf);
    }

    [Benchmark]
    public int ArcArc()
    {
        Span<Point2> buf = stackalloc Point2[2];
        return ArcArcIntersection.Intersect(_arcA, _arcB, buf);
    }
}
