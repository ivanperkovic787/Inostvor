using Inostvor.Kernel.Primitives;
using Inostvor.Rendering.Viewport;
using Shouldly;
using Xunit;

namespace Inostvor.Rendering.Tests;

public sealed class Camera2DTests
{
    private static Camera2D Camera(double w = 800, double h = 600)
    {
        var c = new Camera2D();
        c.SetViewportSize(w, h);
        return c;
    }

    [Fact]
    public void WorldScreen_RoundTrip()
    {
        var camera = Camera();
        camera.ZoomAt(new Point2(200, 100), 3.7);
        camera.PanScreen(55, -23);

        var world = new Point2(12.345, -67.89);
        var back = camera.ScreenToWorld(camera.WorldToScreen(world));

        back.X.ShouldBe(world.X, 1e-9);
        back.Y.ShouldBe(world.Y, 1e-9);
    }

    [Theory]
    [InlineData(100, 100, 2.0)]
    [InlineData(0, 0, 1.5)]
    [InlineData(799, 599, 0.5)]
    [InlineData(400, 300, 10.0)]
    public void ZoomAt_TockaPodKursoromOstajeFiksna(double sx, double sy, double factor)
    {
        var camera = Camera();
        var screenPoint = new Point2(sx, sy);
        var worldBefore = camera.ScreenToWorld(screenPoint);

        camera.ZoomAt(screenPoint, factor);

        var worldAfter = camera.ScreenToWorld(screenPoint);
        worldAfter.X.ShouldBe(worldBefore.X, 1e-9);
        worldAfter.Y.ShouldBe(worldBefore.Y, 1e-9);
    }

    [Fact]
    public void ZoomAt_UzastopnoStotinuPuta_BezDrifta()
    {
        // Stabilnost pri velikim povećanjima: kumulativni zoom prema istoj točki.
        var camera = Camera();
        var screenPoint = new Point2(631, 87);
        var anchor = camera.ScreenToWorld(screenPoint);

        for (var i = 0; i < 100; i++)
        {
            camera.ZoomAt(screenPoint, 1.1);
        }

        var after = camera.ScreenToWorld(screenPoint);
        after.X.ShouldBe(anchor.X, 1e-6);
        after.Y.ShouldBe(anchor.Y, 1e-6);
    }

    [Fact]
    public void ZoomExtents_GraniceStanuUViewport_SCentrom()
    {
        var camera = Camera(800, 600);
        var bounds = new Aabb(10, 20, 110, 60); // 100 × 40 mm

        camera.ZoomExtents(bounds);

        camera.Center.ShouldBe(bounds.Center);
        foreach (var corner in new[] { new Point2(10, 20), new Point2(110, 20), new Point2(110, 60), new Point2(10, 60) })
        {
            var s = camera.WorldToScreen(corner);
            s.X.ShouldBeInRange(0, 800);
            s.Y.ShouldBeInRange(0, 600);
        }
    }

    [Fact]
    public void ZoomExtents_DegeneriraneGranice_NePada()
    {
        var camera = Camera();
        camera.ZoomExtents(new Aabb(5, 5, 5, 5)); // točka
        camera.Scale.ShouldBe(Camera2D.MaxScale);  // clampano, bez NaN/Inf
        double.IsFinite(camera.Center.X).ShouldBeTrue();
    }

    [Fact]
    public void PanScreen_SadrzajPratiPokazivac_JedanNapramJedan()
    {
        var camera = Camera();
        camera.ZoomAt(new Point2(400, 300), 4.0);
        var world = new Point2(3, 7);
        var before = camera.WorldToScreen(world);

        camera.PanScreen(37, -12);

        var after = camera.WorldToScreen(world);
        (after.X - before.X).ShouldBe(37, 1e-9);
        (after.Y - before.Y).ShouldBe(-12, 1e-9);
    }

    [Fact]
    public void Scale_Clampana()
    {
        var camera = Camera();
        camera.ZoomAt(new Point2(0, 0), 1e12);
        camera.Scale.ShouldBe(Camera2D.MaxScale);
        camera.ZoomAt(new Point2(0, 0), 1e-24);
        camera.Scale.ShouldBe(Camera2D.MinScale);
    }

    [Fact]
    public void VisibleWorldBounds_OdgovaraViewportu()
    {
        var camera = Camera(800, 600);
        camera.CenterOn(new Point2(50, 50), 2.0); // 2 px/mm → 400 × 300 mm vidljivo

        var visible = camera.VisibleWorldBounds();

        visible.Width.ShouldBe(400, 1e-9);
        visible.Height.ShouldBe(300, 1e-9);
        visible.Center.ShouldBe(new Point2(50, 50));
    }

    [Fact]
    public void Version_RastePriSvakojPromjeni()
    {
        var camera = Camera();
        var v = camera.Version;
        camera.ZoomAt(new Point2(1, 1), 2);
        camera.PanScreen(1, 1);
        camera.ZoomExtents(new Aabb(0, 0, 10, 10));
        camera.Version.ShouldBe(v + 3);
    }
}
