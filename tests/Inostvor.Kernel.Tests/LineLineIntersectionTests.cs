using Inostvor.Kernel.Intersections;
using Inostvor.Kernel.Primitives;
using Shouldly;
using Xunit;

namespace Inostvor.Kernel.Tests;

public sealed class LineLineIntersectionTests
{
    private const double Eps = 1e-9;

    [Fact]
    public void Krizanje_VracaTocku()
    {
        var a = new LineSeg(new Point2(0, 0), new Point2(2, 2));
        var b = new LineSeg(new Point2(0, 2), new Point2(2, 0));

        LineLineIntersection.TryIntersect(a, b, out var p).ShouldBeTrue();
        p.X.ShouldBe(1.0, Eps);
        p.Y.ShouldBe(1.0, Eps);
    }

    [Fact]
    public void PravciSeSijeku_AliSegmentiNe_VracaFalse()
    {
        var a = new LineSeg(new Point2(0, 0), new Point2(1, 1));
        var b = new LineSeg(new Point2(3, 0), new Point2(4, -1)); // presjek pravaca izvan oba segmenta

        LineLineIntersection.TryIntersect(a, b, out _).ShouldBeFalse();
    }

    [Fact]
    public void DodirNaKraju_JestPresjek()
    {
        var a = new LineSeg(new Point2(0, 0), new Point2(1, 0));
        var b = new LineSeg(new Point2(1, 0), new Point2(1, 1));

        LineLineIntersection.TryIntersect(a, b, out var p).ShouldBeTrue();
        p.AlmostEquals(new Point2(1, 0)).ShouldBeTrue();
    }

    [Fact]
    public void Paralelni_VracaFalse()
    {
        var a = new LineSeg(new Point2(0, 0), new Point2(1, 0));
        var b = new LineSeg(new Point2(0, 1), new Point2(1, 1));

        LineLineIntersection.TryIntersect(a, b, out _).ShouldBeFalse();
        LineLineIntersection.AreCollinearOverlapping(a, b).ShouldBeFalse(); // paralelni, ne kolinearni
    }

    [Fact]
    public void KolinearnoPreklapanje_DetektiraSeZasebno()
    {
        var a = new LineSeg(new Point2(0, 0), new Point2(2, 0));
        var overlapping = new LineSeg(new Point2(1, 0), new Point2(3, 0));
        var touching = new LineSeg(new Point2(2, 0), new Point2(3, 0));
        var disjoint = new LineSeg(new Point2(3, 0), new Point2(4, 0));

        LineLineIntersection.TryIntersect(a, overlapping, out _).ShouldBeFalse(); // beskonačno presjeka → false
        LineLineIntersection.AreCollinearOverlapping(a, overlapping).ShouldBeTrue();
        LineLineIntersection.AreCollinearOverlapping(a, touching).ShouldBeTrue();  // dodir kraja
        LineLineIntersection.AreCollinearOverlapping(a, disjoint).ShouldBeFalse();
    }

    [Fact]
    public void SkoroDodir_UnutarTolerancije_JestPresjek()
    {
        // b završava 0.5 µm ispod pravca a — unutar geometrijske tolerancije (1 µm).
        var a = new LineSeg(new Point2(0, 0), new Point2(10, 0));
        var b = new LineSeg(new Point2(5, -3), new Point2(5, -5e-7));

        LineLineIntersection.TryIntersect(a, b, out var p).ShouldBeTrue();
        p.X.ShouldBe(5.0, 1e-6);
    }
}
