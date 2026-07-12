using Inostvor.Cam.Generation;
using Inostvor.Core.Model.Toolpath;
using Inostvor.Kernel.Primitives;
using Shouldly;
using Xunit;
using static Inostvor.Cam.Tests.CamTestHelpers;

namespace Inostvor.Cam.Tests;

public sealed class CutOrderStrategyTests
{
    [Fact]
    public void NearestNeighbor_BiraNajbliziDio_RupePrijeOutera()
    {
        // Dio A blizu ishodišta, dio B daleko; NN mora prvi uzeti A.
        var contours = Contours(
            ("0", SquareLines(500, 500, 50)), ("0", [FullCircle(525, 525, 5)]),   // B daleko (id 0, 1)
            ("0", SquareLines(10, 10, 50)), ("0", [FullCircle(35, 35, 5)]));      // A blizu  (id 2, 3)

        var sequences = contours.Select(c => new CutSequence(c.Id, c.Polyline.StartPoint, [])).ToList();

        var ordered = new NearestNeighborCutOrderStrategy().Order(sequences, contours);

        ordered.Select(s => s.SourceContourId).ShouldBe([3, 2, 1, 0]); // A: rupa, outer → B: rupa, outer
    }

    [Fact]
    public void NearestNeighbor_Deterministican()
    {
        var contours = Contours(
            ("0", SquareLines(0, 0, 50)), ("0", [FullCircle(25, 25, 5)]),
            ("0", SquareLines(200, 0, 50)), ("0", [FullCircle(225, 25, 5)]),
            ("0", SquareLines(100, 100, 50)));
        var sequences = contours.Select(c => new CutSequence(c.Id, c.Polyline.StartPoint, [])).ToList();
        var strategy = new NearestNeighborCutOrderStrategy();

        var reference = strategy.Order(sequences, contours).Select(s => s.SourceContourId).ToList();
        for (var i = 0; i < 5; i++)
        {
            strategy.Order(sequences, contours).Select(s => s.SourceContourId).ShouldBe(reference);
        }
    }

    [Fact]
    public void Provider_RezolucijaPoIdu_NepoznatId_Fallback()
    {
        var provider = new CutOrderStrategyProvider(
            [new DefaultCutOrderStrategy(), new NearestNeighborCutOrderStrategy()]);

        provider.Resolve("nearest-neighbor").Id.ShouldBe("nearest-neighbor");
        provider.Resolve("bottom-to-top").Id.ShouldBe("bottom-to-top");
        provider.Resolve("ne-postoji").Id.ShouldBe("bottom-to-top"); // konzervativni fallback
    }

    [Fact]
    public void TehnologijaBiraStrategiju_BezIzmjeneGeneratora()
    {
        // Isti generator, različit CutOrderStrategyId u tehnologiji → različit redoslijed.
        var contours = Contours(
            ("0", SquareLines(0, 200, 50)),     // gore, ali NAJBLIŽE po NN? ne — (0,200) vs (150,0)
            ("0", SquareLines(150, 0, 50)));    // dolje

        var generator = new Cam.Generation.ToolpathGenerator(
            new Cam.Offset.KerfOffsetService(), new Cam.Fitting.ArcFitter(),
            new Cam.Leads.LeadGeneratorService([new Cam.Leads.LineLeadStrategy(), new Cam.Leads.ArcLeadStrategy()]),
            new OvercutService(),
            new CutOrderStrategyProvider([new DefaultCutOrderStrategy(), new NearestNeighborCutOrderStrategy()]));

        var bottomToTop = generator.Generate(contours, TechnologySettings.Default with { CutOrderStrategyId = "bottom-to-top" });
        var nearest = generator.Generate(contours, TechnologySettings.Default with { CutOrderStrategyId = "nearest-neighbor" });

        // bottom-to-top: dio na y=0 (id 1) prvi; nearest od (0,0): offsetirana putanja
        // dijela (0,200) počinje na ~(−0.8, 200), dijela (150,0) na ~(149.2, −0.8) — bliži je (150,0)?
        // dist((0,0),(−0.8,200)) ≈ 200; dist((0,0),(149.2,−0.8)) ≈ 149 → oba biraju id 1 prvi…
        // zato provjeravamo da su strategije IZABRANE (fingerprint identičan default vs default):
        bottomToTop.Sequences[0].SourceContourId.ShouldBe(contours[1].Id);
        nearest.Sequences[0].SourceContourId.ShouldBe(contours[1].Id);

        // …i da stvarno različit raspored daje različit NN izbor:
        var contours2 = Contours(
            ("0", SquareLines(0, 30, 20)),      // vrlo blizu ishodišta, ali viši MinY
            ("0", SquareLines(400, 0, 20)));    // MinY = 0 (bottom-to-top ga bira prvog), daleko za NN

        var b2 = generator.Generate(contours2, TechnologySettings.Default with { CutOrderStrategyId = "bottom-to-top" });
        var n2 = generator.Generate(contours2, TechnologySettings.Default with { CutOrderStrategyId = "nearest-neighbor" });

        b2.Sequences[0].SourceContourId.ShouldBe(contours2[1].Id); // najniži dio
        n2.Sequences[0].SourceContourId.ShouldBe(contours2[0].Id); // najbliži dio
    }
}
