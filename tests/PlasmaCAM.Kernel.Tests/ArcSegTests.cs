using PlasmaCAM.Kernel.Primitives;
using Shouldly;
using Xunit;

namespace PlasmaCAM.Kernel.Tests;

public sealed class ArcSegTests
{
    private const double Eps = 1e-9;

    private static void ShouldBePoint(Point2 actual, double x, double y, double eps = Eps)
    {
        actual.X.ShouldBe(x, eps);
        actual.Y.ShouldBe(y, eps);
    }

    [Fact]
    public void Konstruktor_DegeneriraniLukovi_Bacaju()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new ArcSeg(Point2.Origin, 1e-8, 0, Math.PI));   // premali polumjer
        Should.Throw<ArgumentOutOfRangeException>(() => new ArcSeg(Point2.Origin, 1.0, 0, 1e-9));        // premala lučna duljina
        Should.Throw<ArgumentOutOfRangeException>(() => new ArcSeg(Point2.Origin, 1.0, 0, 3 * Math.PI)); // sweep > 2π
    }

    [Fact]
    public void CetvrtinaLukaCcw_OsnovnaSvojstva()
    {
        var arc = new ArcSeg(Point2.Origin, 1.0, 0.0, Math.PI / 2.0);

        ShouldBePoint(arc.StartPoint, 1, 0);
        ShouldBePoint(arc.EndPoint, 0, 1);
        arc.Length.ShouldBe(Math.PI / 2.0, Eps);
        arc.IsCcw.ShouldBeTrue();
        arc.IsFullCircle.ShouldBeFalse();
        ShouldBePoint(arc.PointAt(0.5), Math.Sqrt(2) / 2, Math.Sqrt(2) / 2);
    }

    [Fact]
    public void CwLuk_SweepNegativan()
    {
        var arc = new ArcSeg(Point2.Origin, 1.0, 0.0, -Math.PI / 2.0);

        ShouldBePoint(arc.StartPoint, 1, 0);
        ShouldBePoint(arc.EndPoint, 0, -1);
        arc.IsCcw.ShouldBeFalse();
        arc.ContainsAngle(-Math.PI / 4.0).ShouldBeTrue();  // 315°, na luku
        arc.ContainsAngle(Math.PI / 2.0).ShouldBeFalse();  // 90°, izvan
    }

    [Fact]
    public void ContainsAngle_RubneVrijednostiIToleranicija()
    {
        var arc = new ArcSeg(Point2.Origin, 1.0, 0.0, Math.PI / 2.0);

        arc.ContainsAngle(0.0).ShouldBeTrue();
        arc.ContainsAngle(Math.PI / 2.0).ShouldBeTrue();
        arc.ContainsAngle(Math.PI / 4.0).ShouldBeTrue();
        arc.ContainsAngle(Math.PI / 2.0 + 1e-8).ShouldBeTrue();  // unutar kutne tolerancije
        arc.ContainsAngle(Math.PI).ShouldBeFalse();
        arc.ContainsAngle(-1e-8).ShouldBeTrue();                 // wrap ispred starta, unutar tolerancije
    }

    [Fact]
    public void Bounds_GornjaPolukruznica()
    {
        var arc = new ArcSeg(Point2.Origin, 1.0, 0.0, Math.PI);
        var b = arc.Bounds;
        b.MinX.ShouldBe(-1.0, Eps);
        b.MaxX.ShouldBe(1.0, Eps);
        b.MinY.ShouldBe(0.0, Eps);
        b.MaxY.ShouldBe(1.0, Eps); // ekstrem na 90° uključen
    }

    [Fact]
    public void Bounds_PuniKrug()
    {
        var arc = new ArcSeg(new Point2(2, 3), 1.5, 0.7, Math.Tau);
        arc.IsFullCircle.ShouldBeTrue();
        var b = arc.Bounds;
        b.MinX.ShouldBe(0.5, Eps);
        b.MaxX.ShouldBe(3.5, Eps);
        b.MinY.ShouldBe(1.5, Eps);
        b.MaxY.ShouldBe(4.5, Eps);
    }

    [Fact]
    public void Reversed_ZamjenjujeKrajeveICuvaGeometriju()
    {
        var arc = new ArcSeg(Point2.Origin, 1.0, 0.0, Math.PI / 2.0);
        var rev = (ArcSeg)arc.Reversed();

        ShouldBePoint(rev.StartPoint, 0, 1);
        ShouldBePoint(rev.EndPoint, 1, 0);
        rev.IsCcw.ShouldBeFalse();
        rev.Length.ShouldBe(arc.Length, Eps);
        ShouldBePoint(rev.PointAt(0.5), Math.Sqrt(2) / 2, Math.Sqrt(2) / 2);
    }

    [Fact]
    public void ClosestPoint_NaLukuIzvanLukaIUCentru()
    {
        var full = new ArcSeg(Point2.Origin, 1.0, 0.0, Math.Tau);
        ShouldBePoint(full.ClosestPoint(new Point2(2, 0)), 1, 0);
        ShouldBePoint(full.ClosestPoint(new Point2(0, -3)), 0, -1);

        var upper = new ArcSeg(Point2.Origin, 1.0, 0.0, Math.PI);
        ShouldBePoint(upper.ClosestPoint(new Point2(0, 2)), 0, 1);      // radijalno na luku
        var nearEnd = upper.ClosestPoint(new Point2(-0.5, -2));          // ispod, bliže endu (-1,0)
        ShouldBePoint(nearEnd, -1, 0);

        ShouldBePoint(full.ClosestPoint(Point2.Origin), 1, 0);           // centar → deterministički start
    }

    [Fact]
    public void FromStartEndCenter_CcwICw()
    {
        var ccw = ArcSeg.FromStartEndCenter(new Point2(1, 0), new Point2(0, 1), Point2.Origin, isCcw: true);
        ccw.SweepAngle.ShouldBe(Math.PI / 2.0, Eps);

        var cw = ArcSeg.FromStartEndCenter(new Point2(1, 0), new Point2(0, 1), Point2.Origin, isCcw: false);
        cw.SweepAngle.ShouldBe(-3.0 * Math.PI / 2.0, Eps);
    }

    [Fact]
    public void FromStartEndCenter_StartJednakEnd_PuniKrug()
    {
        var circle = ArcSeg.FromStartEndCenter(new Point2(1, 0), new Point2(1, 0), Point2.Origin, isCcw: true);
        circle.IsFullCircle.ShouldBeTrue();
        circle.SweepAngle.ShouldBe(Math.Tau, Eps);
    }

    [Fact]
    public void FromStartEndCenter_RazlicitiPolumjeri_Baca()
    {
        Should.Throw<ArgumentException>(() =>
            ArcSeg.FromStartEndCenter(new Point2(1, 0), new Point2(0, 1.2), Point2.Origin, isCcw: true));
    }

    // ============ FromBulge — konvencija predznaka je klasičan izvor bugova ============
    // AutoCAD: pozitivan bulge = CCW od starta prema endu. Za tetivu (0,0)→(1,0) to znači
    // luk ISPOD tetive (CCW gibanje ima centar s lijeve strane smjera putovanja).

    [Fact]
    public void FromBulge_Bulge1_PolukrugIspodTetive()
    {
        var arc = ArcSeg.FromBulge(new Point2(0, 0), new Point2(1, 0), 1.0);

        ShouldBePoint(arc.Center, 0.5, 0.0);
        arc.Radius.ShouldBe(0.5, Eps);
        arc.IsCcw.ShouldBeTrue();
        ShouldBePoint(arc.PointAt(0.5), 0.5, -0.5); // sredina luka ISPOD tetive
        ShouldBePoint(arc.StartPoint, 0, 0);
        ShouldBePoint(arc.EndPoint, 1, 0);
    }

    [Fact]
    public void FromBulge_BulgeMinus1_PolukrugIznadTetive()
    {
        var arc = ArcSeg.FromBulge(new Point2(0, 0), new Point2(1, 0), -1.0);

        ShouldBePoint(arc.Center, 0.5, 0.0);
        arc.IsCcw.ShouldBeFalse();
        ShouldBePoint(arc.PointAt(0.5), 0.5, 0.5); // sredina luka IZNAD tetive
    }

    [Fact]
    public void FromBulge_CetvrtinskiLuk_SagittaIPolumjer()
    {
        var bulge = Math.Tan(Math.PI / 8.0); // θ = π/2
        var arc = ArcSeg.FromBulge(new Point2(0, 0), new Point2(1, 0), bulge);

        arc.Radius.ShouldBe(Math.Sqrt(2) / 2.0, Eps);
        ShouldBePoint(arc.Center, 0.5, 0.5);
        arc.SweepAngle.ShouldBe(Math.PI / 2.0, 1e-9);

        // Sagitta: udaljenost sredine luka od sredine tetive = |bulge| · (tetiva/2).
        var sagittaPoint = arc.PointAt(0.5);
        ShouldBePoint(sagittaPoint, 0.5, -(Math.Sqrt(2) / 2.0 - 0.5));
    }

    [Fact]
    public void FromBulge_VelikiLuk_BulgeVeciOd1()
    {
        var bulge = 2.0; // θ = 4·atan(2) ≈ 253.7° — veliki (major) luk
        var arc = ArcSeg.FromBulge(new Point2(0, 0), new Point2(1, 0), bulge);

        Math.Abs(arc.SweepAngle).ShouldBeGreaterThan(Math.PI);
        ShouldBePoint(arc.StartPoint, 0, 0, 1e-9);
        ShouldBePoint(arc.EndPoint, 1, 0, 1e-9);

        // Sagitta = 2 · 0.5 = 1.0 ispod tetive.
        ShouldBePoint(arc.PointAt(0.5), 0.5, -1.0);
    }

    [Fact]
    public void FromBulge_NulaIPoklopljeneTocke_Bacaju()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ArcSeg.FromBulge(new Point2(0, 0), new Point2(1, 0), 0.0));
        Should.Throw<ArgumentException>(() => ArcSeg.FromBulge(new Point2(1, 1), new Point2(1, 1), 1.0));
    }

    [Fact]
    public void FromBulge_RoundTrip_KrajnjeTockeSePoklapaju()
    {
        // Nasumični, ali deterministički parametri — krajevi rekonstruiranog luka
        // moraju pogoditi ulazne točke unutar geometrijske tolerancije.
        double[] bulges = [0.1, -0.35, 0.9, -1.0, 1.7, -2.5];
        var start = new Point2(3.2, -1.7);
        var end = new Point2(-0.8, 2.4);

        foreach (var b in bulges)
        {
            var arc = ArcSeg.FromBulge(start, end, b);
            arc.StartPoint.AlmostEquals(start, 1e-9).ShouldBeTrue();
            arc.EndPoint.AlmostEquals(end, 1e-9).ShouldBeTrue();
            arc.IsCcw.ShouldBe(b > 0);
        }
    }
}
