using Inostvor.Core.Model.Import;
using Inostvor.Kernel.Primitives;
using Shouldly;
using Xunit;

namespace Inostvor.Import.NetDxf.Tests;

public sealed class NetDxfImporterInsertTests
{
    private static ImportResult Import(string file)
        => new NetDxfImporter().Import(TestDataLocator.Get("Nested", file));

    [Fact]
    public void Insert_Layer0_NasljedujeLayerInserta()
    {
        var r = Import("insert_layer0_inherit.dxf");

        r.Success.ShouldBeTrue(r.Error);
        r.Entities.Count.ShouldBe(2); // krug + linija iz bloka
        r.Entities.All(e => e.Layer == "PARTS").ShouldBeTrue();
        r.Entities.All(e => e.SourceType.StartsWith("INSERT/", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public void Insert_Skala2Rotacija90_TransformacijeTocne()
    {
        var r = Import("insert_scale2_rot90.dxf");

        // Blok: krug c=(10,0) r=5; linija (0,0)→(20,0). Insert: pos (100,50), skala 2, rot 90.
        // p' = (100,50) + R90·S2·p
        var circle = r.Entities.Single(e => e.SourceType == "INSERT/CIRCLE")
            .Segments.ShouldHaveSingleItem().ShouldBeOfType<ArcSeg>();
        circle.Center.AlmostEquals(new Point2(100, 70), 1e-6).ShouldBeTrue();
        circle.Radius.ShouldBe(10.0, 1e-6);
        circle.IsCcw.ShouldBeTrue(); // det > 0, smjer očuvan

        var line = r.Entities.Single(e => e.SourceType == "INSERT/LINE").Segments.ShouldHaveSingleItem();
        line.StartPoint.AlmostEquals(new Point2(100, 50), 1e-6).ShouldBeTrue();
        line.EndPoint.AlmostEquals(new Point2(100, 90), 1e-6).ShouldBeTrue();
    }

    [Fact]
    public void Insert_Zrcaljen_LukPostajeCw()
    {
        var r = Import("insert_mirrored.dxf");

        // Blok: CCW luk c=(0,0) r=10, 0°→90°. Insert: pos (50,0), xscale = -1.
        var arc = r.Entities.ShouldHaveSingleItem().Segments.ShouldHaveSingleItem().ShouldBeOfType<ArcSeg>();
        arc.IsCcw.ShouldBeFalse();
        arc.Center.AlmostEquals(new Point2(50, 0), 1e-6).ShouldBeTrue();
        arc.Radius.ShouldBe(10.0, 1e-6);
        arc.StartPoint.AlmostEquals(new Point2(40, 0), 1e-6).ShouldBeTrue();
        arc.EndPoint.AlmostEquals(new Point2(50, 10), 1e-6).ShouldBeTrue();
    }

    [Fact]
    public void Insert_NeuniformnaSkala_TesselliraUzUpozorenje()
    {
        var r = Import("insert_nonuniform_scale.dxf");

        r.Warnings.ShouldContain(w => w.Code == ImportWarningCodes.NonUniformScale);
        var e = r.Entities.ShouldHaveSingleItem();
        e.Segments.Count.ShouldBeGreaterThan(16);
        e.Segments.All(s => s is LineSeg).ShouldBeTrue();

        // Krug r=10 pod skalom (2,1) → elipsa polu-osi 20 i 10.
        var bounds = Aabb.FromPoints(e.Segments.SelectMany(s => new[] { s.StartPoint, s.EndPoint }));
        bounds.MinX.ShouldBe(-20.0, 0.05);
        bounds.MaxX.ShouldBe(20.0, 0.05);
        bounds.MinY.ShouldBe(-10.0, 0.05);
        bounds.MaxY.ShouldBe(10.0, 0.05);
    }

    [Fact]
    public void UgnjezdeniBlokovi_TriRazine_KompozicijaTransformacija()
    {
        var r = Import("blocks_3_levels.dxf");

        // INNER: krug r=2 u (0,0). MID: INNER na (±10,0). OUTER: MID na (0,20) rot 90.
        // MSP: OUTER na (100,100) skala 2.
        // Očekivani centri: (100,160) i (100,120); polumjer 2·2 = 4.
        r.Success.ShouldBeTrue(r.Error);
        var circles = r.Entities.Select(e => e.Segments.ShouldHaveSingleItem().ShouldBeOfType<ArcSeg>()).ToList();
        circles.Count.ShouldBe(2);
        circles.All(c => Math.Abs(c.Radius - 4.0) < 1e-6).ShouldBeTrue();

        var centers = circles.Select(c => c.Center).OrderBy(p => p.Y).ToList();
        centers[0].AlmostEquals(new Point2(100, 120), 1e-6).ShouldBeTrue();
        centers[1].AlmostEquals(new Point2(100, 160), 1e-6).ShouldBeTrue();
    }
}
