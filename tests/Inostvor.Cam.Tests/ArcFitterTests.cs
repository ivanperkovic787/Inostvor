using Inostvor.Cam.Fitting;
using Inostvor.Kernel.Primitives;
using Shouldly;
using Xunit;
using static Inostvor.Cam.Tests.CamTestHelpers;

namespace Inostvor.Cam.Tests;

public sealed class ArcFitterTests
{
    private static readonly ArcFitter Fitter = new();

    private static List<Point2> SampleArc(Point2 center, double r, double a0, double sweep, int n)
        => Enumerable.Range(0, n)
            .Select(i => a0 + (sweep * i / (n - 1)))
            .Select(a => new Point2(center.X + (r * Math.Cos(a)), center.Y + (r * Math.Sin(a))))
            .ToList();

    [Fact]
    public void TockeSaKruznice_JedanLuk_TocanCentarIPolumjer()
    {
        var points = SampleArc(new Point2(10, 20), 15, 0.3, 1.8, 60);

        var segments = Fitter.Fit(points, closed: false, tolerance: 0.01);

        var arc = segments.ShouldHaveSingleItem().ShouldBeOfType<ArcSeg>();
        arc.Center.X.ShouldBe(10, 1e-6);
        arc.Center.Y.ShouldBe(20, 1e-6);
        arc.Radius.ShouldBe(15, 1e-6);
        // Krajevi luka sidre se na ulazne točke; usporedba s tolerancijom jer se luk
        // rekonstruira iz centra i kuta (bit-identičnost nije realna ni potrebna).
        arc.StartPoint.AlmostEquals(points[0], 1e-9).ShouldBeTrue();
        arc.EndPoint.AlmostEquals(points[^1], 1e-9).ShouldBeTrue();
    }

    [Fact]
    public void KolinearneTocke_JednaLinija()
    {
        var points = Enumerable.Range(0, 20).Select(i => new Point2(i * 5.0, 0)).ToList();

        var segments = Fitter.Fit(points, closed: false, tolerance: 0.01);

        var line = segments.ShouldHaveSingleItem().ShouldBeOfType<LineSeg>();
        line.StartPoint.ShouldBe(new Point2(0, 0));
        line.EndPoint.ShouldBe(new Point2(95, 0));
    }

    [Fact]
    public void KvadratniPrsten_CetiriLinije_BezLaznihLukova()
    {
        var points = new List<Point2>();
        for (var i = 0; i <= 10; i++) points.Add(new Point2(i, 0));
        for (var i = 1; i <= 10; i++) points.Add(new Point2(10, i));
        for (var i = 9; i >= 0; i--) points.Add(new Point2(i, 10));
        for (var i = 9; i >= 1; i--) points.Add(new Point2(0, i));

        var segments = Fitter.Fit(points, closed: true, tolerance: 0.01);

        segments.Count.ShouldBe(4);
        segments.ShouldAllBe(s => s is LineSeg);
    }

    [Fact]
    public void CikCakIzvanTolerancije_OstajuLinije_NikadNetocanLuk()
    {
        // Cik-cak amplitude 0.5 mm — nijedan luk ne može zadovoljiti tol 0.01.
        var points = Enumerable.Range(0, 30)
            .Select(i => new Point2(i * 2.0, i % 2 == 0 ? 0.0 : 0.5))
            .ToList();

        var segments = Fitter.Fit(points, closed: false, tolerance: 0.01);

        segments.ShouldAllBe(s => s is LineSeg);
        // Garancija: svaka ulazna točka na izlaznoj putanji.
        foreach (var p in points)
        {
            MinDistanceToPath(p, segments).ShouldBeLessThanOrEqualTo(0.01);
        }
    }

    [Fact]
    public void GarancijaTocnosti_SlucajniLukovi_SveTockeUnutarTolerancije()
    {
        var rng = new Random(42);
        const double tolerance = 0.02;

        for (var trial = 0; trial < 20; trial++)
        {
            var center = new Point2(rng.NextDouble() * 100, rng.NextDouble() * 100);
            var r = 2 + (rng.NextDouble() * 80);
            var a0 = rng.NextDouble() * Math.Tau;
            var sweep = 0.3 + (rng.NextDouble() * 4.0);
            var points = SampleArc(center, r, a0, sweep, 30 + rng.Next(100));

            var segments = Fitter.Fit(points, closed: false, tolerance);

            foreach (var p in points)
            {
                MinDistanceToPath(p, segments).ShouldBeLessThanOrEqualTo(tolerance + 1e-9);
            }
        }
    }

    [Fact]
    public void SlotPrsten_MjesavinaLukovaILinija()
    {
        // Slot: donja linija, desni polukrug, gornja linija, lijevi polukrug.
        var points = new List<Point2>();
        for (var i = 0; i <= 30; i++) points.Add(new Point2(i * 2.0, 0));
        points.AddRange(SampleArc(new Point2(60, 10), 10, -Math.PI / 2, Math.PI, 40).Skip(1));
        for (var i = 29; i >= 0; i--) points.Add(new Point2(i * 2.0, 20));
        points.AddRange(SampleArc(new Point2(0, 10), 10, Math.PI / 2, Math.PI, 40).Skip(1).SkipLast(1));

        var segments = Fitter.Fit(points, closed: true, tolerance: 0.02);

        segments.OfType<ArcSeg>().Count().ShouldBeGreaterThanOrEqualTo(2);
        segments.OfType<LineSeg>().Count().ShouldBeGreaterThanOrEqualTo(2);
        foreach (var p in points)
        {
            MinDistanceToPath(p, segments).ShouldBeLessThanOrEqualTo(0.02 + 1e-9);
        }
    }

    [Fact]
    public void Determinizam_IstiUlaz_IdenticanIzlaz()
    {
        var points = SampleArc(new Point2(0, 0), 25, 0, Math.PI * 1.5, 120);

        var a = Fitter.Fit(points, false, 0.01);
        var b = Fitter.Fit(points, false, 0.01);

        a.Count.ShouldBe(b.Count);
        for (var i = 0; i < a.Count; i++)
        {
            a[i].StartPoint.ShouldBe(b[i].StartPoint);
            a[i].EndPoint.ShouldBe(b[i].EndPoint);
            a[i].GetType().ShouldBe(b[i].GetType());
        }
    }
}
