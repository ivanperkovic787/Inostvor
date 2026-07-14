using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;

namespace Inostvor.App.Controls;

/// <summary>Argumenti crtanja: Skia platno i njegove dimenzije u FIZIČKIM pikselima.</summary>
public sealed class SkiaPaintEventArgs(SKCanvas canvas, int width, int height) : EventArgs
{
    public SKCanvas Canvas { get; } = canvas;

    public int Width { get; } = width;

    public int Height { get; } = height;
}

/// <summary>
/// SkiaSharp platno za WinUI 3 — VLASTITA implementacija.
///
/// ZAŠTO NE SkiaSharp.Views.WinUI: taj paket je dio UNO ekosustava i povlači
/// uno.winui kao tranzitivnu ovisnost. Uno-ov targets file interferira s Windows
/// App SDK buildom (_FindInvalidWinAppSDKUnoPlatformReference), a
/// SkiaSharp.Views.WinUI.Native.Projection se ne može razriješiti — XAML kompajler
/// pada. Vrijedi i za 2.88.x i za 3.119.x.
///
/// Ova implementacija koristi SAMO Microsoft.UI.Xaml + čisti SkiaSharp: Skia crta u
/// SKBitmap, čiji se pikseli kopiraju u WriteableBitmap koji prikazuje Image kontrol.
/// Za CAD viewport (recrtava se samo na promjenu stanja, ne 60 fps) to je potpuno
/// dovoljno — nema per-frame renderiranja.
/// </summary>
public sealed partial class SkiaCanvas : UserControl, IDisposable
{
    private readonly Image _image = new()
    {
        Stretch = Microsoft.UI.Xaml.Media.Stretch.Fill,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Stretch,
    };

    private WriteableBitmap? _bitmap;
    private SKBitmap? _surface;
    private int _pixelWidth;
    private int _pixelHeight;
    private bool _disposed;

    public SkiaCanvas()
    {
        Content = _image;
        SizeChanged += (_, _) => Invalidate();
        Loaded += (_, _) => Invalidate();
    }

    /// <summary>Crtanje: pretplatnik crta na e.Canvas.</summary>
    public event EventHandler<SkiaPaintEventArgs>? PaintSurface;

    /// <summary>Omjer fizičkih piksela i DIP-ova (DPI-svjesno mapiranje pointera).</summary>
    public double DpiScale { get; private set; } = 1.0;

    /// <summary>Zatraži ponovno crtanje.</summary>
    public void Invalidate()
    {
        if (_disposed || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        var scale = XamlRoot?.RasterizationScale ?? 1.0;
        DpiScale = scale <= 0 ? 1.0 : scale;

        var width = Math.Max(1, (int)Math.Round(ActualWidth * DpiScale));
        var height = Math.Max(1, (int)Math.Round(ActualHeight * DpiScale));

        if (_bitmap is null || width != _pixelWidth || height != _pixelHeight)
        {
            _pixelWidth = width;
            _pixelHeight = height;

            _surface?.Dispose();
            _surface = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));

            _bitmap = new WriteableBitmap(width, height);
            _image.Source = _bitmap;
        }

        using (var canvas = new SKCanvas(_surface!))
        {
            PaintSurface?.Invoke(this, new SkiaPaintEventArgs(canvas, width, height));
            canvas.Flush();
        }

        // Skia (BGRA8888 premul) → WriteableBitmap (isti raspored) — izravna kopija.
        var pixels = _surface!.GetPixelSpan();
        using (var stream = _bitmap!.PixelBuffer.AsStream())
        {
            stream.Write(pixels);
        }

        _bitmap.Invalidate();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _surface?.Dispose();
        _surface = null;
    }
}
