using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Inostvor.App.Controls;

/// <summary>Smjer razvlačenja razdjelnika.</summary>
public enum SplitterOrientation
{
    /// <summary>Okomit razdjelnik — mijenja ŠIRINU stupca lijevo od sebe.</summary>
    Vertical = 0,

    /// <summary>Vodoravan razdjelnik — mijenja VISINU retka iznad sebe.</summary>
    Horizontal = 1,
}

/// <summary>
/// Razdjelnik panela (drag-to-resize) — VLASTITA implementacija.
///
/// ZAŠTO NE CommunityToolkit.WinUI.Controls.Sizers: taj paket povlači Uno.WinUI kao
/// tranzitivnu ovisnost, čiji targets ruše XAML kompajler uz Windows App SDK.
/// Vučenje cijelog Uno Platforma zbog razdjelnika panela je loša trgovina.
///
/// Radi izravno na Grid.ColumnDefinitions / RowDefinitions roditeljskog Grida:
/// mijenja veličinu susjednog stupca (lijevo) odnosno retka (iznad), poštujući
/// MinWidth/MinHeight tog elementa.
/// </summary>
public sealed partial class PanelSplitter : Control
{
    private bool _isDragging;
    private double _dragStart;
    private double _initialSize;

    public PanelSplitter()
    {
        DefaultStyleKey = typeof(PanelSplitter);
        Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);

        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerCaptureLost += (_, _) => _isDragging = false;
    }

    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register(
            nameof(Orientation),
            typeof(SplitterOrientation),
            typeof(PanelSplitter),
            new PropertyMetadata(SplitterOrientation.Vertical));

    public SplitterOrientation Orientation
    {
        get => (SplitterOrientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
        => ProtectedCursor = InputSystemCursor.Create(Orientation == SplitterOrientation.Vertical
            ? InputSystemCursorShape.SizeWestEast
            : InputSystemCursorShape.SizeNorthSouth);

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging)
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        }
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (Parent is not Grid grid)
        {
            return;
        }

        var position = e.GetCurrentPoint(grid).Position;

        if (Orientation == SplitterOrientation.Vertical)
        {
            var index = Grid.GetColumn(this) - 1;
            if (index < 0 || index >= grid.ColumnDefinitions.Count)
            {
                return;
            }

            _dragStart = position.X;
            _initialSize = grid.ColumnDefinitions[index].ActualWidth;
        }
        else
        {
            var index = Grid.GetRow(this) - 1;
            if (index < 0 || index >= grid.RowDefinitions.Count)
            {
                return;
            }

            _dragStart = position.Y;
            _initialSize = grid.RowDefinitions[index].ActualHeight;
        }

        _isDragging = true;
        CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging || Parent is not Grid grid)
        {
            return;
        }

        var position = e.GetCurrentPoint(grid).Position;

        if (Orientation == SplitterOrientation.Vertical)
        {
            var index = Grid.GetColumn(this) - 1;
            if (index < 0 || index >= grid.ColumnDefinitions.Count)
            {
                return;
            }

            var definition = grid.ColumnDefinitions[index];
            var target = _initialSize + (position.X - _dragStart);
            var minimum = double.IsNaN(definition.MinWidth) ? 0 : definition.MinWidth;
            definition.Width = new GridLength(Math.Max(target, minimum), GridUnitType.Pixel);
        }
        else
        {
            var index = Grid.GetRow(this) - 1;
            if (index < 0 || index >= grid.RowDefinitions.Count)
            {
                return;
            }

            var definition = grid.RowDefinitions[index];
            var target = _initialSize + (position.Y - _dragStart);
            var minimum = double.IsNaN(definition.MinHeight) ? 0 : definition.MinHeight;
            definition.Height = new GridLength(Math.Max(target, minimum), GridUnitType.Pixel);
        }

        e.Handled = true;
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }

        _isDragging = false;
        ReleasePointerCapture(e.Pointer);
        e.Handled = true;
    }
}
