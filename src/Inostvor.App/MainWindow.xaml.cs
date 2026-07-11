using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Inostvor.Kernel.Primitives;
using Inostvor.Rendering.Skia;
using Inostvor.ViewModels;
using SkiaSharp.Views.Windows;

namespace Inostvor.App;

public sealed partial class MainWindow : Window
{
    private readonly SceneRenderer _renderer = new();
    private bool _isPanning;
    private Windows.Foundation.Point _lastPointerPosition;
    private double _dpiScale = 1.0; // Skia crta u fizičkim pikselima, pointer eventi su u DIP-ovima

    public MainWindow()
    {
        ViewModel = App.Services.GetRequiredService<MainViewModel>();
        Console = App.Services.GetRequiredService<ConsoleViewModel>();

        InitializeComponent();

        Title = "Inostvor";
        SystemBackdrop = new MicaBackdrop();

        // Change-driven invalidacija: canvas se recrtava SAMO kad viewport to zatraži.
        ViewModel.Viewport.RedrawRequested += (_, _) => Canvas.Invalidate();
        Closed += (_, _) => _renderer.Dispose();
    }

    public MainViewModel ViewModel { get; }

    public ConsoleViewModel Console { get; }

    private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        if (Canvas.ActualWidth > 0)
        {
            _dpiScale = e.Info.Width / Canvas.ActualWidth;
        }

        var viewport = ViewModel.Viewport;
        viewport.Camera.SetViewportSize(e.Info.Width, e.Info.Height);
        _renderer.Draw(e.Surface.Canvas, viewport.Camera, viewport.Scene, viewport.HighlightedContourId, viewport.IssueMarker);
    }

    private void OnCanvasSizeChanged(object sender, SizeChangedEventArgs e) => Canvas.Invalidate();

    private void OnCanvasWheel(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(Canvas);
        var factor = point.Properties.MouseWheelDelta > 0 ? 1.2 : 1.0 / 1.2;
        ViewModel.Viewport.ZoomAt(ToPixels(point.Position), factor);
        e.Handled = true;
    }

    private void OnCanvasPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(Canvas);
        if (point.Properties.IsMiddleButtonPressed || point.Properties.IsRightButtonPressed)
        {
            _isPanning = true;
            _lastPointerPosition = point.Position;
            Canvas.CapturePointer(e.Pointer);
            e.Handled = true;
        }
    }

    private void OnCanvasPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isPanning)
        {
            return;
        }

        var position = e.GetCurrentPoint(Canvas).Position;
        ViewModel.Viewport.Pan(
            (position.X - _lastPointerPosition.X) * _dpiScale,
            (position.Y - _lastPointerPosition.Y) * _dpiScale);
        _lastPointerPosition = position;
        e.Handled = true;
    }

    private void OnCanvasPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            Canvas.ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }
    }

    private Point2 ToPixels(Windows.Foundation.Point dip) => new(dip.X * _dpiScale, dip.Y * _dpiScale);
}
