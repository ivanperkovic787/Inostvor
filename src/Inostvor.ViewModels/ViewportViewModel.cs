using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Inostvor.Core.Model.Validation;
using Inostvor.Kernel.Primitives;
using Inostvor.Rendering.Scene;
using Inostvor.Rendering.Viewport;

namespace Inostvor.ViewModels;

/// <summary>
/// Stanje viewporta: kamera + scena + isticanje. View (canvas) se pretplaćuje na
/// <see cref="RedrawRequested"/> i recrtava SAMO kad se stanje promijeni
/// (change-driven invalidacija — nema recrtavanja u praznom hodu).
/// </summary>
public sealed partial class ViewportViewModel : ObservableObject
{
    /// <summary>Ciljana širina prikaza pri skoku na nalaz bez konture. [mm]</summary>
    private const double IssueZoomWindow = 40.0;

    public Camera2D Camera { get; } = new();

    public RenderScene Scene { get; private set; } = RenderScene.Empty;

    [ObservableProperty]
    private int _highlightedContourId = -1;

    [ObservableProperty]
    private Point2? _issueMarker;

    public event EventHandler? RedrawRequested;

    public void SetScene(RenderScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        Scene = scene;
        HighlightedContourId = -1;
        IssueMarker = null;
        Camera.ZoomExtents(scene.Bounds);
        RequestRedraw();
    }

    public void SetViewportSize(double width, double height)
    {
        Camera.SetViewportSize(width, height);
        RequestRedraw();
    }

    public void ZoomAt(Point2 screenPoint, double factor)
    {
        Camera.ZoomAt(screenPoint, factor);
        RequestRedraw();
    }

    public void Pan(double deltaXPixels, double deltaYPixels)
    {
        Camera.PanScreen(deltaXPixels, deltaYPixels);
        RequestRedraw();
    }

    [RelayCommand]
    public void ZoomExtents()
    {
        Camera.ZoomExtents(Scene.Bounds);
        RequestRedraw();
    }

    /// <summary>Zoom Selected: uklopi istaknutu konturu.</summary>
    [RelayCommand]
    public void ZoomSelected()
    {
        var contour = Scene.Contours.FirstOrDefault(c => c.Id == HighlightedContourId);
        if (contour is null)
        {
            return;
        }

        Camera.ZoomExtents(contour.Bounds.Inflate(Math.Max(contour.Bounds.Width, contour.Bounds.Height) * 0.15));
        RequestRedraw();
    }

    /// <summary>
    /// Klik na nalaz validacije: centriraj, zumiraj i označi konturu/segment.
    /// Kontura postoji → uklopi njezine granice; inače fiksni prozor oko lokacije.
    /// </summary>
    public void ZoomToIssue(ValidationIssue issue)
    {
        ArgumentNullException.ThrowIfNull(issue);

        HighlightedContourId = issue.ContourId;
        IssueMarker = issue.Location;

        var contour = Scene.Contours.FirstOrDefault(c => c.Id == issue.ContourId);
        if (contour is not null)
        {
            var padding = Math.Max(Math.Max(contour.Bounds.Width, contour.Bounds.Height) * 0.2, 5.0);
            Camera.ZoomExtents(contour.Bounds.Inflate(padding));
        }
        else if (issue.Location is { } location)
        {
            var scale = Math.Max(Camera.ViewportWidth, 1.0) / IssueZoomWindow;
            Camera.CenterOn(location, scale);
        }

        RequestRedraw();
    }

    private void RequestRedraw() => RedrawRequested?.Invoke(this, EventArgs.Empty);
}
