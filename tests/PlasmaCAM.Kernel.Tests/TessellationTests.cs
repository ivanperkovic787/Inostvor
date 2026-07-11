using PlasmaCAM.Kernel.Primitives;
using Shouldly;
using Xunit;

namespace PlasmaCAM.Kernel.Tests;

public sealed class TessellationTests
{
    [Fact]
    public void TessellateArc_KrajeviEgzaktni_SagittaUnutarTolerancije()
    {
        var arc = new ArcSeg(Point2.Origin, 50.0, 0.3, 2.1);
        const double tol = 0.01;

        var pts = Tessellation.TessellateArc(arc, tol);

        pts[0].AlmostEquals(arc.StartPoint, 1e-9).ShouldBeTrue();
        pts[^1].AlmostEquals(arc.EndPoint, 1e-9).ShouldBeTrue();

        // Sagitta svake tetive: udaljenost polovišta tetive od kružnice ≤ tol.
        for (var i = 1; i < pts.Count; i++)
        {
            var mid = pts[i - 1].MidPointTo(pts[i]);
            var deviation = Math.Abs(mid.DistanceTo(arc.Center) - arc.Radius);
            deviation.ShouldBeLessThanOrEqualTo(tol + 1e-9);
        }
    }

    [Fact]
    public void TessellateArc_GrubljaTolerancija_ManjeTetiva()
    {
        var arc = new ArcSeg(Point2.Origin, 50.0, 0.0, Math.PI);

        var fine = Tessellation.TessellateArc(arc, 0.001);
        var coarse = Tessellation.TessellateArc(arc, 0.5);

        coarse.Count.ShouldBeLessThan(fine.Count);
        coarse.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void TessellateArc_NevaljanaTolerancija_Baca()
    {
        var arc = new ArcSeg(Point2.Origin, 1.0, 0.0, 1.0);
        Should.Throw<ArgumentOutOfRangeException>(() => Tessellation.TessellateArc(arc, 0.0));
    }

    [Fact]
    public void TessellateArc_PatoloskiFinaTolerancija_PostujeGornjuGranicu()
    {
        var arc = new ArcSeg(Point2.Origin, 1000.0, 0.0, Math.Tau);
        var pts = Tessellation.TessellateArc(arc, 1e-12);
        pts.Count.ShouldBeLessThanOrEqualTo(Tessellation.MaxChordsPerArc + 1);
    }
}
