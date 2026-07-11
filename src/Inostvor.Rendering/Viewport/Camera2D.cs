using Inostvor.Kernel.Primitives;

namespace Inostvor.Rendering.Viewport;

/// <summary>
/// 2D kamera CAD kvalitete: world (mm, +Y gore) ↔ screen (px, +Y dolje).
/// Čista matematika bez ovisnosti o UI-ju ili SkiaSharpu — potpuno testabilna.
///
/// Stabilnost pri velikim povećanjima: transformacija je Center+Scale (ne
/// akumulirana matrica), pa nema drifta množenja matrica; skala je ograničena
/// na [MinScale, MaxScale] unutar kojih double zadržava sub-pikselnu preciznost.
/// </summary>
public sealed class Camera2D
{
    public const double MinScale = 1e-4;  // 0.1 mm stane u 10 km — dovoljno
    public const double MaxScale = 1e6;   // 1 nm po pikselu — daleko iznad potrebe

    /// <summary>Točka svijeta u središtu viewporta. [mm]</summary>
    public Point2 Center { get; private set; } = new(0, 0);

    /// <summary>Skala: piksela po milimetru.</summary>
    public double Scale { get; private set; } = 1.0;

    public double ViewportWidth { get; private set; } = 1.0;

    public double ViewportHeight { get; private set; } = 1.0;

    /// <summary>Raste pri svakoj promjeni — view po tome zna da treba recrtati (change-driven invalidacija).</summary>
    public long Version { get; private set; }

    public void SetViewportSize(double width, double height)
    {
        ViewportWidth = Math.Max(width, 1.0);
        ViewportHeight = Math.Max(height, 1.0);
        Version++;
    }

    public Point2 WorldToScreen(Point2 world) => new(
        ((world.X - Center.X) * Scale) + (ViewportWidth / 2.0),
        (ViewportHeight / 2.0) - ((world.Y - Center.Y) * Scale));

    public Point2 ScreenToWorld(Point2 screen) => new(
        ((screen.X - (ViewportWidth / 2.0)) / Scale) + Center.X,
        (((ViewportHeight / 2.0) - screen.Y) / Scale) + Center.Y);

    /// <summary>Vidljivi pravokutnik svijeta (za culling).</summary>
    public Aabb VisibleWorldBounds()
    {
        var halfW = ViewportWidth / (2.0 * Scale);
        var halfH = ViewportHeight / (2.0 * Scale);
        return new Aabb(Center.X - halfW, Center.Y - halfH, Center.X + halfW, Center.Y + halfH);
    }

    /// <summary>
    /// Zoom To Cursor: točka svijeta pod kursorom OSTAJE pod kursorom.
    /// Izvod: nakon promjene skale, Center se postavlja tako da
    /// WorldToScreen(worldUnderCursor) == screenPoint.
    /// </summary>
    public void ZoomAt(Point2 screenPoint, double factor)
    {
        var worldUnderCursor = ScreenToWorld(screenPoint);
        Scale = Math.Clamp(Scale * factor, MinScale, MaxScale);
        Center = new Point2(
            worldUnderCursor.X - ((screenPoint.X - (ViewportWidth / 2.0)) / Scale),
            worldUnderCursor.Y - (((ViewportHeight / 2.0) - screenPoint.Y) / Scale));
        Version++;
    }

    /// <summary>Zoom Extents: uklopi granice u viewport s marginom (udio, default 5%).</summary>
    public void ZoomExtents(Aabb bounds, double margin = 0.05)
    {
        var width = Math.Max(bounds.Width, 1e-9);
        var height = Math.Max(bounds.Height, 1e-9);
        var scaleX = ViewportWidth / (width * (1.0 + (2.0 * margin)));
        var scaleY = ViewportHeight / (height * (1.0 + (2.0 * margin)));
        Scale = Math.Clamp(Math.Min(scaleX, scaleY), MinScale, MaxScale);
        Center = bounds.Center;
        Version++;
    }

    /// <summary>Pan u pikselima (drag): pomak sadržaja prati pokazivač 1:1, bez trzanja.</summary>
    public void PanScreen(double deltaXPixels, double deltaYPixels)
    {
        Center = new Point2(
            Center.X - (deltaXPixels / Scale),
            Center.Y + (deltaYPixels / Scale));
        Version++;
    }

    /// <summary>Centriraj na točku svijeta uz zadanu skalu (za zoom-to-issue).</summary>
    public void CenterOn(Point2 world, double scale)
    {
        Center = world;
        Scale = Math.Clamp(scale, MinScale, MaxScale);
        Version++;
    }
}
