using PlasmaCAM.Kernel.Intersections;
using PlasmaCAM.Kernel.Primitives;
using Shouldly;
using Xunit;

namespace PlasmaCAM.Kernel.Tests;

public sealed class LineArcIntersectionTests
{
    private const double Eps = 1e-9;

    private static ArcSeg FullCircle(double r = 1.0) => new(Point2.Origin, r, 0.0, Math.Tau);

    [Fact]
    public void Sekanta_DvijeTocke()
    {
        var line = new LineSeg(new Point2(-2, 0), new Point2(2, 0));
        Span<Point2> buf = stackalloc Point2[2];

        var n = LineArcIntersection.Intersect(line, FullCircle(), buf);

        n.ShouldBe(2);
        buf[0].AlmostEquals(new Point2(-1, 0), Eps).ShouldBeTrue(); // t rastuće: prvo x = -1
        buf[1].AlmostEquals(new Point2(1, 0), Eps).ShouldBeTrue();
    }

    [Fact]
    public void Tangenta_JednaTocka()
    {
        var line = new LineSeg(new Point2(-2, 1), new Point2(2, 1));
        Span<Point2> buf = stackalloc Point2[2];

        var n = LineArcIntersection.Intersect(line, FullCircle(), buf);

        n.ShouldBe(1);
        buf[0].AlmostEquals(new Point2(0, 1), Eps).ShouldBeTrue();
    }

    [Fact]
    public void Promasaj_NulaTocaka()
    {
        var line = new LineSeg(new Point2(-2, 2), new Point2(2, 2));
        Span<Point2> buf = stackalloc Point2[2];

        LineArcIntersection.Intersect(line, FullCircle(), buf).ShouldBe(0);
    }

    [Fact]
    public void SweepFilter_SamoTockeNaLuku()
    {
        var upperHalf = new ArcSeg(Point2.Origin, 1.0, 0.0, Math.PI);
        var line = new LineSeg(new Point2(0, -2), new Point2(0, 2));
        Span<Point2> buf = stackalloc Point2[2];

        var n = LineArcIntersection.Intersect(line, upperHalf, buf);

        n.ShouldBe(1);
        buf[0].AlmostEquals(new Point2(0, 1), Eps).ShouldBeTrue(); // (0,-1) je izvan sweepa
    }

    [Fact]
    public void SegmentZavrsavaNaKruznici_JedanPresjek()
    {
        var line = new LineSeg(new Point2(0, 0), new Point2(1, 0)); // end točno na kružnici
        Span<Point2> buf = stackalloc Point2[2];

        var n = LineArcIntersection.Intersect(line, FullCircle(), buf);

        n.ShouldBe(1);
        buf[0].AlmostEquals(new Point2(1, 0), Eps).ShouldBeTrue();
    }

    [Fact]
    public void SegmentUnutarKruznice_BezPresjeka()
    {
        var line = new LineSeg(new Point2(-0.5, 0), new Point2(0.5, 0));
        Span<Point2> buf = stackalloc Point2[2];

        LineArcIntersection.Intersect(line, FullCircle(), buf).ShouldBe(0);
    }

    [Fact]
    public void PremaliBuffer_Baca()
    {
        var line = new LineSeg(new Point2(-2, 0), new Point2(2, 0));
        var arc = FullCircle();

        Should.Throw<ArgumentException>(() =>
        {
            Span<Point2> tooSmall = stackalloc Point2[1];
            LineArcIntersection.Intersect(line, arc, tooSmall);
        });
    }
}
