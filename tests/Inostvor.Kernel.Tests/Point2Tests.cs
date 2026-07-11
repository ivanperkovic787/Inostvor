using Inostvor.Kernel.Primitives;
using Shouldly;
using Xunit;

namespace Inostvor.Kernel.Tests;

public sealed class Point2Tests
{
    private const double Eps = 1e-12;

    [Fact]
    public void DistanceTo_Pitagora()
    {
        new Point2(0, 0).DistanceTo(new Point2(3, 4)).ShouldBe(5.0, Eps);
        new Point2(1, 1).DistanceSquaredTo(new Point2(4, 5)).ShouldBe(25.0, Eps);
    }

    [Fact]
    public void AlmostEquals_UnutarIIzvanTolerancije()
    {
        var p = new Point2(1, 1);
        p.AlmostEquals(new Point2(1 + 5e-7, 1)).ShouldBeTrue();
        p.AlmostEquals(new Point2(1 + 5e-6, 1)).ShouldBeFalse();
        p.AlmostEquals(new Point2(1.04, 1), 0.05).ShouldBeTrue();
    }

    [Fact]
    public void Operatori_TockaVektor()
    {
        var p = new Point2(1, 2) + new Vector2(3, 4);
        p.ShouldBe(new Point2(4, 6));

        var q = new Point2(4, 6) - new Vector2(1, 1);
        q.ShouldBe(new Point2(3, 5));

        var v = new Point2(5, 5) - new Point2(2, 3);
        v.ShouldBe(new Vector2(3, 2));
    }

    [Fact]
    public void Lerp_IMidPoint()
    {
        Point2.Lerp(new Point2(0, 0), new Point2(10, 20), 0.25).ShouldBe(new Point2(2.5, 5.0));
        new Point2(0, 0).MidPointTo(new Point2(4, 6)).ShouldBe(new Point2(2, 3));
    }
}
