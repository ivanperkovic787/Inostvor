using Inostvor.Core.Model.Import;
using Inostvor.Kernel.Primitives;
using Shouldly;
using Xunit;

namespace Inostvor.Import.NetDxf.Tests;

public sealed class NetDxfImporterBasicTests
{
    private static ImportResult Import(string category, string file)
        => new NetDxfImporter().Import(TestDataLocator.Get(category, file));

    [Theory]
    [InlineData("rectangle_lines_R2000.dxf")]
    [InlineData("rectangle_lines_R2004.dxf")]
    [InlineData("rectangle_lines_R2010.dxf")]
    [InlineData("rectangle_lines_R2013.dxf")]
    [InlineData("rectangle_lines_R2018.dxf")]
    public void PravokutnikIzLinija_SveDxfVerzije(string file)
    {
        var r = Import("Simple", file);

        r.Success.ShouldBeTrue(r.Error);
        r.Entities.Count.ShouldBe(4);
        r.TotalSegmentCount.ShouldBe(4);
        r.Entities.All(e => e.SourceType == "LINE" && e.Layer == "0").ShouldBeTrue();

        var bounds = Aabb.FromPoints(r.Entities.SelectMany(e => e.Segments).SelectMany(s => new[] { s.StartPoint, s.EndPoint }));
        bounds.MinX.ShouldBe(0.0, 1e-6);
        bounds.MaxX.ShouldBe(100.0, 1e-6);
        bounds.MaxY.ShouldBe(50.0, 1e-6);
    }

    [Fact]
    public void Krug_PostajePuniArcSeg()
    {
        var r = Import("Simple", "circle.dxf");

        r.Success.ShouldBeTrue(r.Error);
        r.Entities.Count.ShouldBe(1);
        var arc = r.Entities[0].Segments.ShouldHaveSingleItem().ShouldBeOfType<ArcSeg>();
        arc.IsFullCircle.ShouldBeTrue();
        arc.Center.AlmostEquals(new Point2(50, 30), 1e-6).ShouldBeTrue();
        arc.Radius.ShouldBe(25.0, 1e-6);
        arc.IsCcw.ShouldBeTrue();
    }

    [Fact]
    public void Luk_CcwSaStupnjevima()
    {
        var r = Import("Simple", "arc_ccw.dxf");

        var arc = r.Entities.ShouldHaveSingleItem().Segments.ShouldHaveSingleItem().ShouldBeOfType<ArcSeg>();
        arc.Radius.ShouldBe(40.0, 1e-6);
        arc.IsCcw.ShouldBeTrue();
        arc.SweepAngle.ShouldBe(Math.PI * 120.0 / 180.0, 1e-9);
        arc.StartPoint.AlmostEquals(new Point2(40 * Math.Cos(Math.PI / 6), 40 * Math.Sin(Math.PI / 6)), 1e-6).ShouldBeTrue();
    }

    [Fact]
    public void ZatvorenaLwPolyline_LanacSegmenataSeZatvara()
    {
        var r = Import("Simple", "rect_lwpolyline_closed.dxf");

        var e = r.Entities.ShouldHaveSingleItem();
        e.SourceType.ShouldBe("LWPOLYLINE");
        e.Segments.Count.ShouldBe(4);
        e.Segments[^1].EndPoint.AlmostEquals(e.Segments[0].StartPoint, 1e-6).ShouldBeTrue();
    }

    [Fact]
    public void SlotSBulgeovima_DvijeLinijeDvaPolukruga()
    {
        var r = Import("Simple", "slot_lwpolyline_bulge.dxf");

        var segs = r.Entities.ShouldHaveSingleItem().Segments;
        segs.Count.ShouldBe(4);
        segs.OfType<LineSeg>().Count().ShouldBe(2);

        var arcs = segs.OfType<ArcSeg>().ToList();
        arcs.Count.ShouldBe(2);
        arcs.All(a => Math.Abs(a.Radius - 10.0) < 1e-6).ShouldBeTrue();      // bulge=1, tetiva 20 → r=10
        arcs.All(a => Math.Abs(Math.Abs(a.SweepAngle) - Math.PI) < 1e-9).ShouldBeTrue(); // polukrugovi

        segs[^1].EndPoint.AlmostEquals(segs[0].StartPoint, 1e-6).ShouldBeTrue();
    }

    [Fact]
    public void Layeri_SeBiljezePoEntitetu()
    {
        var r = Import("Holes", "plate_two_holes.dxf");

        r.Entities.Count.ShouldBe(3);
        r.Layers.ShouldBe(["0", "HOLES"]);
        r.Entities.Count(e => e.Layer == "HOLES").ShouldBe(2);
    }

    [Fact]
    public void OtvorenaPolyline_KrajeviRazliciti()
    {
        var r = Import("OpenContours", "open_polyline.dxf");

        var e = r.Entities.ShouldHaveSingleItem();
        e.Segments.Count.ShouldBe(3); // 4 vrha, NEzatvorena → 3 segmenta
        e.Segments[^1].EndPoint.AlmostEquals(e.Segments[0].StartPoint, 0.05).ShouldBeFalse();
    }

    [Fact]
    public void Jedinice_InciSkaliraniUMilimetre()
    {
        var r = Import("Simple", "units_inches_line4in.dxf");

        r.SourceUnits.ShouldBe("Inches");
        r.UnitScaleToMm.ShouldBe(25.4, 1e-9);
        var line = r.Entities.ShouldHaveSingleItem().Segments.ShouldHaveSingleItem();
        line.Length.ShouldBe(101.6, 1e-6);
    }

    [Fact]
    public void Jedinice_Unitless_PretpostavljeniMm_UzUpozorenje()
    {
        var r = Import("Simple", "units_unitless.dxf");

        r.UnitScaleToMm.ShouldBe(1.0);
        r.Warnings.ShouldContain(w => w.Code == ImportWarningCodes.UnitlessAssumedMm);
        r.Entities.ShouldHaveSingleItem().Segments.ShouldHaveSingleItem().Length.ShouldBe(10.0, 1e-6);
    }
}
