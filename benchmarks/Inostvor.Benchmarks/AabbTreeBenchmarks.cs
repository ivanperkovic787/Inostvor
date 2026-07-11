using BenchmarkDotNet.Attributes;
using Inostvor.Kernel.Primitives;
using Inostvor.Kernel.Spatial;

namespace Inostvor.Benchmarks;

/// <summary>
/// AabbTree (bez rebalansirajućih rotacija — odluka M1) protiv linearnog skena.
/// Ako QueryTree ikad padne ispod ~5× prednosti nad QueryLinear na 10k kutija,
/// vrijeme je za uvođenje rotacija.
/// </summary>
[MemoryDiagnoser]
public class AabbTreeBenchmarks
{
    private List<(Aabb Box, int Id)> _boxes = null!;
    private List<Aabb> _queries = null!;
    private AabbTree<int> _tree = null!;

    [Params(1000, 10000)]
    public int BoxCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        _boxes = new List<(Aabb, int)>(BoxCount);
        for (var i = 0; i < BoxCount; i++)
        {
            var x = rng.NextDouble() * 1000.0;
            var y = rng.NextDouble() * 1000.0;
            _boxes.Add((new Aabb(x, y, x + (rng.NextDouble() * 20.0), y + (rng.NextDouble() * 20.0)), i));
        }

        _queries = new List<Aabb>(100);
        for (var q = 0; q < 100; q++)
        {
            var x = rng.NextDouble() * 1000.0;
            var y = rng.NextDouble() * 1000.0;
            _queries.Add(new Aabb(x, y, x + 50.0, y + 50.0));
        }

        _tree = new AabbTree<int>();
        foreach (var (box, id) in _boxes)
        {
            _tree.Insert(box, id);
        }
    }

    [Benchmark]
    public int Build()
    {
        var tree = new AabbTree<int>();
        foreach (var (box, id) in _boxes)
        {
            tree.Insert(box, id);
        }

        return tree.Count;
    }

    [Benchmark]
    public int QueryTree()
    {
        var results = new List<int>();
        foreach (var q in _queries)
        {
            _tree.Query(q, results);
        }

        return results.Count;
    }

    [Benchmark(Baseline = true)]
    public int QueryLinear()
    {
        var results = new List<int>();
        foreach (var q in _queries)
        {
            foreach (var (box, id) in _boxes)
            {
                if (box.Intersects(q))
                {
                    results.Add(id);
                }
            }
        }

        return results.Count;
    }
}
