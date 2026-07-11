using Inostvor.Kernel.Primitives;
using Shouldly;
using Xunit;

namespace Inostvor.Kernel.Tests;

public sealed class LineSegTests
{
    private const double Eps = 1e-12;

    [Fact]
    public void Konstruktor_DegeneriranSegment_Baca()
    {
        Should.Throw<ArgumentException>(() => new LineSeg(new Point2(1, 1), new Point2(1, 1)));
        Should.Throw<ArgumentException>(() => new LineSeg(new Point2(0, 0), new Point2(5e-7, 0)));
    }

    [Fact]
    public void Length_IDirection()
    {
        var s = new LineSeg(new Point2(1, 1), new Point2(4, 5));
        s.Length.ShouldBe(5.0, Eps);
        s.Direction.X.ShouldBe(0.6, Eps);
        s.Direction.Y.ShouldBe(0.8, Eps);
    }

    [Fact]
    public void PointAt_Interpolacija()
    {
        var s = new LineSeg(new Point2(0, 0), new Point2(10, 0));
        s.PointAt(0.0).ShouldBe(new Point2(0, 0));
        s.PointAt(1.0).ShouldBe(new Point2(10, 0));
        s.PointAt(0.3).X.ShouldBe(3.0, Eps);
    }

    [Fact]
    public void ClosestPoint_PrijeNaIPoslijeSegmenta()
    {
        var s = new LineSeg(new Point2(0, 0), new Point2(10, 0));

        s.ClosestPoint(new Point2(-5, 3)).ShouldBe(new Point2(0, 0));   // prije starta → start
        s.ClosestPoint(new Point2(15, -2)).ShouldBe(new Point2(10, 0)); // poslije enda → end

        var mid = s.ClosestPoint(new Point2(4, 7));                      // projekcija unutar segmenta
        mid.X.ShouldBe(4.0, Eps);
        mid.Y.ShouldBe(0.0, Eps);

        s.DistanceTo(new Point2(4, 7)).ShouldBe(7.0, Eps);
    }

    [Fact]
    public void Bounds_IReversed()
    {
        var s = new LineSeg(new Point2(3, 4), new Point2(1, 2));
        s.Bounds.ShouldBe(new Aabb(1, 2, 3, 4));

        var r = s.Reversed();
        r.StartPoint.ShouldBe(new Point2(1, 2));
        r.EndPoint.ShouldBe(new Point2(3, 4));
        r.Length.ShouldBe(s.Length, Eps);
    }
}
