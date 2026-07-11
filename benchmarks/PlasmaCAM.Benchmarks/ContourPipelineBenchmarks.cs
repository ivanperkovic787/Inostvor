using BenchmarkDotNet.Attributes;
using PlasmaCAM.Core.Model.Geometry;
using PlasmaCAM.Core.Model.Import;
using PlasmaCAM.Geometry.Contours;
using PlasmaCAM.Kernel.Primitives;

namespace PlasmaCAM.Benchmarks;

/// <summary>
/// Detekcija + klasifikacija kontura na realističnom rasporedu: mreža pravokutnika
/// s rupama, segmenti izmiješani determinističkim seedom (najgori slučaj za lančanje).
/// </summary>
[MemoryDiagnoser]
public class ContourPipelineBenchmarks
{
    private IReadOnlyList<ImportedEntity> _entities = null!;
    private readonly ContourBuilder _builder = new();
    private readonly ContourClassifier _classifier = new();

    [Params(100, 500)]
    public int PartCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var segments = new List<ISegment>();
        var cols = (int)Math.Ceiling(Math.Sqrt(PartCount));
        for (var p = 0; p < PartCount; p++)
        {
            var x = (p % cols) * 60.0;
            var y = (p / cols) * 60.0;
            segments.Add(new LineSeg(new Point2(x, y), new Point2(x + 40, y)));
            segments.Add(new LineSeg(new Point2(x + 40, y), new Point2(x + 40, y + 40)));
            segments.Add(new LineSeg(new Point2(x + 40, y + 40), new Point2(x, y + 40)));
            segments.Add(new LineSeg(new Point2(x, y + 40), new Point2(x, y)));
            segments.Add(new ArcSeg(new Point2(x + 20, y + 20), 8.0, 0.0, Math.Tau));
        }

        // Deterministički shuffle (Fisher-Yates, fiksni seed) — najgori ulazni redoslijed.
        var rng = new Random(42);
        for (var i = segments.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (segments[i], segments[j]) = (segments[j], segments[i]);
        }

        _entities = [new ImportedEntity(segments, "0", "TEST", null)];
    }

    [Benchmark]
    public int BuildOnly() => _builder.Build(_entities, ContourBuildSettings.Default).Contours.Count;

    [Benchmark]
    public int BuildAndClassify()
    {
        var build = _builder.Build(_entities, ContourBuildSettings.Default);
        return _classifier.Classify(build.Contours).Count(c => c.Kind == ContourKind.Hole);
    }
}
