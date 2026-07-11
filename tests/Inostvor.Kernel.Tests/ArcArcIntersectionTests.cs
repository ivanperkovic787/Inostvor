using Inostvor.Kernel.Intersections;
using Inostvor.Kernel.Primitives;
using Shouldly;
using Xunit;

namespace Inostvor.Kernel.Tests;

public sealed class ArcArcIntersectionTests
{
    private const double Eps = 1e-9;

    private static ArcSeg Circle(double cx, double cy, double r) => new(new Point2(cx, cy), r, 0.0, Math.Tau);

    [Fact]
    public void DvaPresjeka_KlasicniSlucaj()
    {
        Span<Point2> buf = stackalloc Point2[2];
        var n = ArcArcIntersection.Intersect(Circle(0, 0, 1), Circle(1, 0, 1), buf);

        n.ShouldBe(2);
        var expectedY = Math.Sqrt(3) / 2.0;
        // Redoslijed: +okomica pa -okomica.
        buf[0].AlmostEquals(new Point2(0.5, expectedY), Eps).ShouldBeTrue();
        buf[1].AlmostEquals(new Point2(0.5, -expectedY), Eps).ShouldBeTrue();
    }

    [Fact]
    public void VanjskaTangenta_JednaTocka()
    {
        Span<Point2> buf = stackalloc Point2[2];
        var n = ArcArcIntersection.Intersect(Circle(0, 0, 1), Circle(2, 0, 1), buf);

        n.ShouldBe(1);
        buf[0].AlmostEquals(new Point2(1, 0), Eps).ShouldBeTrue();
    }

    [Fact]
    public void UnutarnjaTangenta_JednaTocka()
    {
        Span<Point2> buf = stackalloc Point2[2];
        var n = ArcArcIntersection.Intersect(Circle(0, 0, 2), Circle(1, 0, 1), buf);

        n.ShouldBe(1);
        buf[0].AlmostEquals(new Point2(2, 0), Eps).ShouldBeTrue();
    }

    [Fact]
    public void Razdvojene_IJednaUnutarDruge_NulaTocaka()
    {
        Span<Point2> buf = stackalloc Point2[2];
        ArcArcIntersection.Intersect(Circle(0, 0, 1), Circle(4, 0, 1), buf).ShouldBe(0);   // predaleko
        ArcArcIntersection.Intersect(Circle(0, 0, 2), Circle(0.3, 0, 1), buf).ShouldBe(0); // unutra, bez dodira
    }

    [Fact]
    public void Koncentricne_IKoincidentne_NulaTocaka()
    {
        Span<Point2> buf = stackalloc Point2[2];
        ArcArcIntersection.Intersect(Circle(0, 0, 1), Circle(0, 0, 2), buf).ShouldBe(0); // koncentrične
        ArcArcIntersection.Intersect(Circle(0, 0, 1), Circle(0, 0, 1), buf).ShouldBe(0); // koincidentne (dokumentirano)
    }

    [Fact]
    public void SweepFilter_SamoTockeNaObaLuka()
    {
        var upperHalfLeft = new ArcSeg(Point2.Origin, 1.0, 0.0, Math.PI); // gornja polovica lijeve kružnice
        Span<Point2> buf = stackalloc Point2[2];

        var n = ArcArcIntersection.Intersect(upperHalfLeft, Circle(1, 0, 1), buf);

        n.ShouldBe(1);
        buf[0].AlmostEquals(new Point2(0.5, Math.Sqrt(3) / 2.0), Eps).ShouldBeTrue();
    }
}
