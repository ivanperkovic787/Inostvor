using PlasmaCAM.Kernel.Primitives;
using PlasmaCAM.Kernel.Transforms;
using Shouldly;
using Xunit;

namespace PlasmaCAM.Kernel.Tests;

public sealed class SegmentTransformTests
{
    private const double Eps = 1e-9;

    [Fact]
    public void IsConformal_RotacijaITranslacijaISkala_Da_NeuniformnaSkala_Ne()
    {
        SegmentTransform.IsConformal(Matrix3x2d.Identity, out var s1).ShouldBeTrue();
        s1.ShouldBe(1.0, Eps);

        var m = Matrix3x2d.CreateRotation(0.7) * Matrix3x2d.CreateScale(2.5, 2.5) * Matrix3x2d.CreateTranslation(10, -3);
        SegmentTransform.IsConformal(m, out var s2).ShouldBeTrue();
        s2.ShouldBe(2.5, 1e-9);

        SegmentTransform.IsConformal(Matrix3x2d.CreateScale(2, 1), out _).ShouldBeFalse();

        // Zrcaljenje JE konformno (čuva oblik, obrće orijentaciju).
        SegmentTransform.IsConformal(Matrix3x2d.CreateScale(-1, 1), out var s3).ShouldBeTrue();
        s3.ShouldBe(1.0, Eps);
    }

    [Fact]
    public void Linija_TransformiraKrajeve()
    {
        var line = new LineSeg(new Point2(0, 0), new Point2(10, 0));
        var m = Matrix3x2d.CreateRotation(Math.PI / 2.0) * Matrix3x2d.CreateTranslation(5, 5);

        var result = SegmentTransform.Transform(line, m, 0.01, out var tess);

        tess.ShouldBeFalse();
        result.Count.ShouldBe(1);
        result[0].StartPoint.AlmostEquals(new Point2(5, 5), Eps).ShouldBeTrue();
        result[0].EndPoint.AlmostEquals(new Point2(5, 15), Eps).ShouldBeTrue();
    }

    [Fact]
    public void Luk_KonformnaMatrica_OstajeLuk_SkaliranPolumjer()
    {
        var arc = new ArcSeg(new Point2(10, 0), 5.0, 0.0, Math.PI / 2.0);
        var m = Matrix3x2d.CreateScale(2, 2) * Matrix3x2d.CreateRotation(Math.PI / 2.0) * Matrix3x2d.CreateTranslation(100, 50);

        var result = SegmentTransform.Transform(arc, m, 0.01, out var tess);

        tess.ShouldBeFalse();
        result.Count.ShouldBe(1);
        var t = result[0].ShouldBeOfType<ArcSeg>();
        t.Radius.ShouldBe(10.0, Eps);
        t.Center.AlmostEquals(new Point2(100, 70), Eps).ShouldBeTrue(); // (10,0)·2 → rot90 → (0,20) → +(100,50)
        t.IsCcw.ShouldBeTrue();
        t.StartPoint.AlmostEquals(m.TransformPoint(arc.StartPoint), Eps).ShouldBeTrue();
        t.EndPoint.AlmostEquals(m.TransformPoint(arc.EndPoint), Eps).ShouldBeTrue();
    }

    [Fact]
    public void Luk_Zrcaljenje_ObrceSmjer()
    {
        var arc = new ArcSeg(Point2.Origin, 10.0, 0.0, Math.PI / 2.0); // CCW
        var m = Matrix3x2d.CreateScale(-1, 1) * Matrix3x2d.CreateTranslation(50, 0);

        var result = SegmentTransform.Transform(arc, m, 0.01, out _);

        var t = result[0].ShouldBeOfType<ArcSeg>();
        t.IsCcw.ShouldBeFalse();
        t.Center.AlmostEquals(new Point2(50, 0), Eps).ShouldBeTrue();
        t.StartPoint.AlmostEquals(new Point2(40, 0), Eps).ShouldBeTrue();
        t.EndPoint.AlmostEquals(new Point2(50, 10), Eps).ShouldBeTrue();
        t.Radius.ShouldBe(10.0, Eps);
    }

    [Fact]
    public void PuniKrug_KonformnaTransformacija_OstajePuniKrug()
    {
        var circle = new ArcSeg(new Point2(3, 4), 2.0, 0.0, Math.Tau);
        var m = Matrix3x2d.CreateScale(3, 3) * Matrix3x2d.CreateTranslation(-1, 1);

        var result = SegmentTransform.Transform(circle, m, 0.01, out _);

        var t = result[0].ShouldBeOfType<ArcSeg>();
        t.IsFullCircle.ShouldBeTrue();
        t.Radius.ShouldBe(6.0, Eps);
        t.Center.AlmostEquals(new Point2(8, 13), Eps).ShouldBeTrue();
    }

    [Fact]
    public void Luk_NeuniformnaSkala_Tessellira_KrajeviIzOblikaElipse()
    {
        var circle = new ArcSeg(Point2.Origin, 10.0, 0.0, Math.Tau);
        var m = Matrix3x2d.CreateScale(2, 1);

        var result = SegmentTransform.Transform(circle, m, 0.01, out var tess);

        tess.ShouldBeTrue();
        result.Count.ShouldBeGreaterThan(16);
        result.All(s => s is LineSeg).ShouldBeTrue();

        // Svaka točka mora ležati na elipsi (x/20)² + (y/10)² = 1.
        foreach (var seg in result)
        {
            var p = seg.StartPoint;
            var e = ((p.X * p.X) / 400.0) + ((p.Y * p.Y) / 100.0);
            e.ShouldBe(1.0, 0.01);
        }
    }

    [Fact]
    public void Luk_KolabiranSkalomNula_VracaPrazno()
    {
        var arc = new ArcSeg(Point2.Origin, 5.0, 0.0, Math.PI);
        var collapse = Matrix3x2d.CreateScale(1e-9, 1e-9);

        var result = SegmentTransform.Transform(arc, collapse, 0.01, out _);

        result.ShouldBeEmpty();
    }
}
