using Inostvor.Kernel.Primitives;
using Inostvor.Rendering.Scene;
using Shouldly;
using Xunit;

namespace Inostvor.Rendering.Tests;

public sealed class DisplayTessellationTests
{
    private static ArcSeg Arc() => new(new Point2(0, 0), 50, 0, Math.PI);

    [Fact]
    public void WorldChordTolerance_PadaSPovecanjemZooma()
    {
        DisplayTessellation.WorldChordTolerance(1.0).ShouldBe(0.5, 1e-12);
        DisplayTessellation.WorldChordTolerance(10.0).ShouldBe(0.05, 1e-12);
        DisplayTessellation.WorldChordTolerance(1e-9).ShouldBe(5.0);   // clamp gore
        DisplayTessellation.WorldChordTolerance(1e9).ShouldBe(1e-4);  // clamp dolje
    }

    [Fact]
    public void ToleranceBucket_StabilanUnutarDvostrukogRaspona()
    {
        DisplayTessellation.ToleranceBucket(0.5).ShouldBe(DisplayTessellation.ToleranceBucket(0.9));
        DisplayTessellation.ToleranceBucket(0.4).ShouldBeLessThan(DisplayTessellation.ToleranceBucket(0.9));
    }

    [Fact]
    public void GetArcPoints_IstiBucket_IstaInstanca()
    {
        var cache = new DisplayTessellation();
        var a = cache.GetArcPoints(1, Arc(), 1.0);
        var b = cache.GetArcPoints(1, Arc(), 1.05); // ista razina zooma → isti bucket

        ReferenceEquals(a, b).ShouldBeTrue();
        cache.CachedEntryCount.ShouldBe(1);
    }

    [Fact]
    public void GetArcPoints_ViseZoom_ViseTocaka()
    {
        var cache = new DisplayTessellation();
        var coarse = cache.GetArcPoints(1, Arc(), 0.5);
        var fine = cache.GetArcPoints(1, Arc(), 64.0);

        fine.Count.ShouldBeGreaterThan(coarse.Count);
        cache.CachedEntryCount.ShouldBe(2);
    }

    [Fact]
    public void IsSubPixel_SitniSegmentPriOdzumiranju()
    {
        var tiny = new LineSeg(new Point2(0, 0), new Point2(0.5, 0));
        DisplayTessellation.IsSubPixel(tiny, scale: 0.1).ShouldBeTrue();    // 0.05 px
        DisplayTessellation.IsSubPixel(tiny, scale: 100.0).ShouldBeFalse(); // 50 px
    }
}
