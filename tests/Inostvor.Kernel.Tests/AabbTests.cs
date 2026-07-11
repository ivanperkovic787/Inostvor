using Inostvor.Kernel.Primitives;
using Shouldly;
using Xunit;

namespace Inostvor.Kernel.Tests;

public sealed class AabbTests
{
    [Fact]
    public void Konstruktor_MinVeciOdMax_Baca()
    {
        Should.Throw<ArgumentException>(() => new Aabb(1, 0, 0, 1));
        Should.Throw<ArgumentException>(() => new Aabb(0, 1, 1, 0));
    }

    [Fact]
    public void FromCorners_NormaliziraPoredak()
    {
        var b = Aabb.FromCorners(new Point2(3, 4), new Point2(1, 2));
        b.ShouldBe(new Aabb(1, 2, 3, 4));
    }

    [Fact]
    public void FromPoints_ObuhvacaSve_IBacaZaPrazno()
    {
        var b = Aabb.FromPoints([new Point2(1, 5), new Point2(-2, 3), new Point2(4, -1)]);
        b.ShouldBe(new Aabb(-2, -1, 4, 5));

        Should.Throw<ArgumentException>(() => Aabb.FromPoints([]));
    }

    [Fact]
    public void Dimenzije_ICentar()
    {
        var b = new Aabb(0, 0, 4, 2);
        b.Width.ShouldBe(4.0);
        b.Height.ShouldBe(2.0);
        b.Center.ShouldBe(new Point2(2, 1));
        b.Perimeter.ShouldBe(12.0);
    }

    [Fact]
    public void Intersects_PreklapanjeDodirIRazdvojenost()
    {
        var a = new Aabb(0, 0, 2, 2);
        a.Intersects(new Aabb(1, 1, 3, 3)).ShouldBeTrue();
        a.Intersects(new Aabb(2, 0, 4, 2)).ShouldBeTrue();  // dodir ruba je presjek (zatvoreni intervali)
        a.Intersects(new Aabb(3, 3, 4, 4)).ShouldBeFalse();
        a.Intersects(new Aabb(2.5, 0, 4, 2), tolerance: 1.0).ShouldBeTrue(); // tolerancija širi dohvat
    }

    [Fact]
    public void Contains_TockaIKutija()
    {
        var b = new Aabb(0, 0, 2, 2);
        b.Contains(new Point2(1, 1)).ShouldBeTrue();
        b.Contains(new Point2(2, 2)).ShouldBeTrue();  // rub uključen
        b.Contains(new Point2(2.1, 1)).ShouldBeFalse();
        b.Contains(new Point2(2.1, 1), tolerance: 0.2).ShouldBeTrue();

        b.Contains(new Aabb(0.5, 0.5, 1.5, 1.5)).ShouldBeTrue();
        b.Contains(new Aabb(0.5, 0.5, 2.5, 1.5)).ShouldBeFalse();
    }

    [Fact]
    public void Union_IInflate()
    {
        var u = Aabb.Union(new Aabb(0, 0, 1, 1), new Aabb(2, -1, 3, 0.5));
        u.ShouldBe(new Aabb(0, -1, 3, 1));

        new Aabb(0, 0, 2, 2).Inflate(0.5).ShouldBe(new Aabb(-0.5, -0.5, 2.5, 2.5));
        Should.Throw<ArgumentException>(() => new Aabb(0, 0, 1, 1).Inflate(-0.6)); // kolaps u nevaljan
    }
}
