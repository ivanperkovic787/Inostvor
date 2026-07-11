using PlasmaCAM.Core.Model.Geometry;
using PlasmaCAM.Geometry.Contours;
using PlasmaCAM.Kernel.Primitives;
using Shouldly;
using Xunit;
using static PlasmaCAM.Geometry.Tests.TestGeometry;

namespace PlasmaCAM.Geometry.Tests;

public sealed class ContourBuilderTests
{
    private static readonly ContourBuilder Builder = new();
    private static readonly ContourBuildSettings Default = ContourBuildSettings.Default;

    [Fact]
    public void Kvadrat_EgzaktniSpojevi_JednaZatvorenaKontura_BezJoinova()
    {
        var result = Builder.Build([Entity("0", SquareLines(0, 0, 10))], Default);

        var c = result.Contours.ShouldHaveSingleItem();
        c.Kind.ShouldBe(ContourKind.Unclassified);
        c.IsClosed.ShouldBeTrue();
        c.ClosedByTolerance.ShouldBeFalse();
        c.SegmentCount.ShouldBe(4);
        result.Joins.ShouldBeEmpty();
    }

    [Fact]
    public void Kvadrat_IzmjesaniIObrnutiSegmenti_IstaZatvorenaKontura()
    {
        var s = SquareLines(0, 0, 10);
        // Namjerno izmiješano i dva segmenta obrnuta — builder mora sam orijentirati.
        var shuffled = new ISegment[] { s[2].Reversed(), s[0], s[3], s[1].Reversed() };

        var result = Builder.Build([Entity("0", shuffled)], Default);

        var c = result.Contours.ShouldHaveSingleItem();
        c.IsClosed.ShouldBeTrue();
        c.SegmentCount.ShouldBe(4);
        result.Joins.ShouldBeEmpty();

        // Lanac je stvarno povezan: kraj svakog segmenta = početak sljedećeg.
        for (var i = 1; i < c.Polyline.Count; i++)
        {
            c.Polyline[i - 1].EndPoint.AlmostEquals(c.Polyline[i].StartPoint).ShouldBeTrue();
        }
    }

    [Fact]
    public void MaliRazmak_UnutarTolerancije_SpojenIEvidentiran()
    {
        // Kao small_gap_002.dxf: razmak 0.02 mm između L0 i L1 (tolerancija 0.05).
        var result = Builder.Build(
        [
            Entity("0",
                L(0, 0, 100, 0),
                L(100, 0.02, 100, 50),
                L(100, 50, 0, 50),
                L(0, 50, 0, 0)),
        ], Default);

        var c = result.Contours.ShouldHaveSingleItem();
        c.IsClosed.ShouldBeTrue();

        var join = result.Joins.ShouldHaveSingleItem();
        join.GapSize.ShouldBe(0.02, 1e-9);
        join.IsClosingJoin.ShouldBeFalse();
        join.Location.AlmostEquals(new Point2(100, 0.01), 1e-9).ShouldBeTrue();
        join.ContourId.ShouldBe(c.Id);
    }

    [Fact]
    public void VelikiRazmak_IzvanTolerancije_OstajeOtvorenaKontura()
    {
        // Kao large_gap_05.dxf: razmak 0.5 mm > 0.05 — lanac se NE zatvara.
        var result = Builder.Build(
        [
            Entity("0",
                L(0, 0, 100, 0),
                L(100, 0.5, 100, 50),
                L(100, 50, 0, 50),
                L(0, 50, 0, 0)),
        ], Default);

        var c = result.Contours.ShouldHaveSingleItem();
        c.Kind.ShouldBe(ContourKind.Open);
        c.SegmentCount.ShouldBe(4); // ostali spojevi su egzaktni pa je lanac jedan
        result.Joins.ShouldBeEmpty();
    }

    [Fact]
    public void ZatvaranjeTolerancijom_ClosedByTolerance_IClosingJoin()
    {
        // Šav konture ima razmak 0.03 mm (početak prvog i kraj zadnjeg segmenta).
        var result = Builder.Build(
        [
            Entity("0",
                L(0.03, 0, 100, 0),
                L(100, 0, 100, 50),
                L(100, 50, 0, 50),
                L(0, 50, 0, 0)),
        ], Default);

        var c = result.Contours.ShouldHaveSingleItem();
        c.IsClosed.ShouldBeTrue();
        c.ClosedByTolerance.ShouldBeTrue();

        var join = result.Joins.ShouldHaveSingleItem();
        join.IsClosingJoin.ShouldBeTrue();
        join.GapSize.ShouldBe(0.03, 1e-9);
    }

    [Fact]
    public void DvaOdvojenaKvadrata_DvijeKonture_StabilniIdjevi()
    {
        var result = Builder.Build(
        [
            Entity("0", SquareLines(0, 0, 10)),
            Entity("0", SquareLines(100, 100, 20)),
        ], Default);

        result.Contours.Count.ShouldBe(2);
        result.Contours[0].Id.ShouldBe(0);
        result.Contours[1].Id.ShouldBe(1);
        result.Contours.All(c => c.IsClosed).ShouldBeTrue();
    }

    [Fact]
    public void PuniKrug_JedanSegment_Zatvoren()
    {
        var result = Builder.Build([Entity("0", FullCircle(50, 50, 10))], Default);

        var c = result.Contours.ShouldHaveSingleItem();
        c.IsClosed.ShouldBeTrue();
        c.ClosedByTolerance.ShouldBeFalse();
        c.SegmentCount.ShouldBe(1);
    }

    [Fact]
    public void SamaLinija_NeZatvaraSeNaSebe()
    {
        // Linija kraća od tolerancije spajanja (0.04 mm) — krajevi su si "blizu",
        // ali linija NIKAD nije zatvorena kontura.
        var result = Builder.Build([Entity("0", L(0, 0, 0.04, 0))], Default);

        result.Contours.ShouldHaveSingleItem().Kind.ShouldBe(ContourKind.Open);
    }

    [Fact]
    public void Layeri_SeNeMijesaju()
    {
        // Krajevi se poklapaju, ali su segmenti na različitim layerima.
        var result = Builder.Build(
        [
            Entity("A", L(0, 0, 10, 0)),
            Entity("B", L(10, 0, 10, 10)),
        ], Default);

        result.Contours.Count.ShouldBe(2);
        result.Contours.All(c => c.Kind == ContourKind.Open).ShouldBeTrue();
        result.Contours[0].Layer.ShouldBe("A");
        result.Contours[1].Layer.ShouldBe("B");
    }

    [Fact]
    public void SirenjeUnatrag_SeedUSrediniLanca()
    {
        // Seed (prvi u ulazu) je SREDNJI segment — lanac se mora proširiti u oba smjera.
        var result = Builder.Build(
        [
            Entity("0",
                L(5, 0, 10, 0),   // seed (sredina)
                L(0, 0, 5, 0),    // prije seeda
                L(10, 0, 10, 5)), // poslije seeda
        ], Default);

        var c = result.Contours.ShouldHaveSingleItem();
        c.SegmentCount.ShouldBe(3);
        c.Polyline.StartPoint.AlmostEquals(new Point2(0, 0)).ShouldBeTrue();
        c.Polyline.EndPoint.AlmostEquals(new Point2(10, 5)).ShouldBeTrue();
    }

    [Fact]
    public void MjesovitiSlot_LinijeILukovi_Zatvoren()
    {
        var result = Builder.Build(
        [
            Entity("0",
                L(0, 0, 60, 0),
                ArcSeg.FromStartEndCenter(new Point2(60, 0), new Point2(60, 20), new Point2(60, 10), isCcw: true),
                L(60, 20, 0, 20),
                ArcSeg.FromStartEndCenter(new Point2(0, 20), new Point2(0, 0), new Point2(0, 10), isCcw: true)),
        ], Default);

        var c = result.Contours.ShouldHaveSingleItem();
        c.IsClosed.ShouldBeTrue();
        c.SegmentCount.ShouldBe(4);
    }

    [Fact]
    public void Determinizam_PetUzastopnihPokretanja_IdenticanRezultat()
    {
        var entities = new[]
        {
            Entity("0", SquareLines(0, 0, 10)),
            Entity("0", L(30, 0, 40, 0), L(40, 0.02, 40, 10), L(40, 10, 30, 10)),
            Entity("HOLES", FullCircle(5, 5, 2)),
        };

        var reference = Builder.Build(entities, Default);
        for (var run = 0; run < 5; run++)
        {
            var result = Builder.Build(entities, Default);

            result.Contours.Count.ShouldBe(reference.Contours.Count);
            for (var i = 0; i < result.Contours.Count; i++)
            {
                result.Contours[i].Id.ShouldBe(reference.Contours[i].Id);
                result.Contours[i].Kind.ShouldBe(reference.Contours[i].Kind);
                result.Contours[i].Layer.ShouldBe(reference.Contours[i].Layer);
                result.Contours[i].SegmentCount.ShouldBe(reference.Contours[i].SegmentCount);
                result.Contours[i].Polyline.StartPoint.ShouldBe(reference.Contours[i].Polyline.StartPoint);
            }

            result.Joins.Count.ShouldBe(reference.Joins.Count);
            for (var i = 0; i < result.Joins.Count; i++)
            {
                result.Joins[i].ShouldBe(reference.Joins[i]);
            }
        }
    }
}
