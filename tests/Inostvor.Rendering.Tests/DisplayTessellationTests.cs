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
        // Skale 1.2 i 1.8 daju tolerancije 0.417 i 0.278 — obje u bucketu -2
        // (granice bucketa su na POTENCIJAMA BROJA 2: 0.25, 0.5, 1.0…).
        var cache = new DisplayTessellation();
        var a = cache.GetArcPoints(1, Arc(), 1.2);
        var b = cache.GetArcPoints(1, Arc(), 1.8);

        ReferenceEquals(a, b).ShouldBeTrue(); // cache pogodak — bez regeneracije pri panu/finom zoomu
        cache.CachedEntryCount.ShouldBe(1);
    }

    [Fact]
    public void GetArcPoints_PrelazakGraniceBucketa_NovaTessellacija()
    {
        // Skala 1.0 → tolerancija TOČNO 0.5 = 2^-1 (granica bucketa -1);
        // skala 1.05 → 0.476 → bucket -2. Prijelaz granice MORA dati novu tessellaciju,
        // inače bi prikaz pri zoomiranju ostao grublji od ciljanih 0.5 px.
        var cache = new DisplayTessellation();
        cache.GetArcPoints(1, Arc(), 1.0);
        cache.GetArcPoints(1, Arc(), 1.05);

        cache.CachedEntryCount.ShouldBe(2);
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
