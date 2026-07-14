using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
        ViewModel.Simulation.RedrawRequested += (_, _) => Canvas.Invalidate();

        // Ticker reprodukcije: aktivan ISKLJUČIVO dok simulacija igra (bez praznog hoda).
        _simulationTicker = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _simulationTicker.Tick += (_, _) => ViewModel.Simulation.Advance(0.033);
        ViewModel.Simulation.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SimulationViewModel.IsPlaying))
            {
                if (ViewModel.Simulation.IsPlaying)
                {
                    _simulationTicker.Start();
                }
                else
                {
                    _simulationTicker.Stop();
                }
            }
        };

        // Auto Save svakih 60 s (samo ako ima uvezene geometrije; tiho na pogrešku).
        _autoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
        _autoSaveTimer.Tick += async (_, _) =>
        {
            try
            {
                await ViewModel.AutoSaveAsync();
            }
            catch (IOException)
            {
                // Autosave nikad ne smije srušiti rad korisnika.
            }
        };
        _autoSaveTimer.Start();

        Activated += OnFirstActivation;

        Closed += (_, _) =>
        {
            _simulationTicker.Stop();
            _autoSaveTimer.Stop();
            ViewModel.Project.MarkCleanExit(); // uredan izlaz → nema ponude oporavka
            _renderer.Dispose();
        };
    }

    private readonly DispatcherTimer _autoSaveTimer;
    private bool _recoveryChecked;

    /// <summary>Nakon pada prošle sesije ponudi oporavak zadnjeg autosavea.</summary>
    private async void OnFirstActivation(object sender, WindowActivatedEventArgs e)
    {
        if (_recoveryChecked)
        {
            return;
        }

        _recoveryChecked = true;
        Activated -= OnFirstActivation;

        if (!ViewModel.Project.RecoveryAvailable)
        {
            return;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = Content.XamlRoot,
            Title = "Oporavak rada",
            Content = "Prethodna sesija nije uredno završena. Želiš li otvoriti automatski spremljeni projekt?",
            PrimaryButtonText = "Oporavi",
            CloseButtonText = "Odbaci",
            DefaultButton = ContentDialogButton.Primary,
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await ViewModel.OpenProjectAsync(ViewModel.Project.AutoSavePath);
        }
        else
        {
            ViewModel.Project.ClearAutoSave();
        }
    }

    private readonly DispatcherTimer _simulationTicker;

    private void OnSpeedChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.Simulation.SpeedMultiplier = SpeedBox.SelectedIndex switch
        {
            0 => 0.5, 1 => 1.0, 2 => 2.0, 3 => 5.0, _ => 10.0,
        };
    }

    /// <summary>
    /// x:Bind funkcija: bool → Visibility. WinUI x:Bind ne radi implicitnu konverziju
    /// (klasični Binding radi), pa je ovo najčišći put bez IValueConvertera.
    /// </summary>
    public static Visibility BoolToVisibility(bool value)
        => value ? Visibility.Visible : Visibility.Collapsed;

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
        _renderer.Draw(
            e.Surface.Canvas, viewport.Camera, viewport.Scene,
            viewport.HighlightedContourId, viewport.IssueMarker,
            ViewModel.LastToolpath, ViewModel.Simulation.CurrentState);
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
