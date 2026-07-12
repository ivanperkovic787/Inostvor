using BenchmarkDotNet.Attributes;
using Inostvor.Cam.Fitting;
using Inostvor.Cam.Generation;
using Inostvor.Cam.Leads;
using Inostvor.Cam.Offset;
using Inostvor.Core.Model.Geometry;
using Inostvor.Core.Model.Import;
using Inostvor.Core.Model.Toolpath;
using Inostvor.Geometry.Contours;
using Inostvor.Kernel.Primitives;

namespace Inostvor.Benchmarks;

/// <summary>Cijeli CAM cjevovod (kerf → fit → leadovi → redoslijed) na mreži dijelova s rupama.</summary>
[MemoryDiagnoser]
public class ToolpathBenchmarks
{
    private IReadOnlyList<Contour> _contours = null!;
    private ToolpathGenerator _generator = null!;
    private readonly TechnologySettings _tech = TechnologySettings.Default with { KerfWidth = 1.6, OvercutLength = 2.0 };

    [Params(50, 200)]
    public int PartCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var entities = new List<ImportedEntity>();
        var cols = (int)Math.Ceiling(Math.Sqrt(PartCount));
        for (var p = 0; p < PartCount; p++)
        {
            var x = (p % cols) * 70.0;
            var y = (p / cols) * 70.0;
            entities.Add(new ImportedEntity(
            [
                new LineSeg(new Point2(x, y), new Point2(x + 50, y)),
                new LineSeg(new Point2(x + 50, y), new Point2(x + 50, y + 50)),
                new LineSeg(new Point2(x + 50, y + 50), new Point2(x, y + 50)),
                new LineSeg(new Point2(x, y + 50), new Point2(x, y)),
            ], "0", "TEST", null));
            entities.Add(new ImportedEntity([new ArcSeg(new Point2(x + 25, y + 25), 10, 0, Math.Tau)], "0", "TEST", null));
        }

        _contours = new ContourClassifier().Classify(
            new ContourBuilder().Build(entities, ContourBuildSettings.Default).Contours);

        _generator = new ToolpathGenerator(
            new KerfOffsetService(),
            new ArcFitter(),
            new LeadGeneratorService([new LineLeadStrategy(), new ArcLeadStrategy()]),
            new OvercutService(),
            new CutOrderStrategyProvider([new DefaultCutOrderStrategy(), new NearestNeighborCutOrderStrategy()]));
    }

    [Benchmark]
    public int GenerateFullProgram() => _generator.Generate(_contours, _tech).Sequences.Count;
}
