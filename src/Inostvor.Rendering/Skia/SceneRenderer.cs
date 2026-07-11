using Inostvor.Core.Model.Geometry;
using Inostvor.Kernel.Primitives;
using Inostvor.Rendering.Scene;
using Inostvor.Rendering.Viewport;
using SkiaSharp;

namespace Inostvor.Rendering.Skia;

/// <summary>
/// Crta scenu na SKCanvas. NEMA CAM logike — isključivo prikaz stanja iz jezgre.
/// Redoslijed rada po frameu: culling (AabbTree) → sub-pixel LOD → crtanje
/// (lukovi iz display cachea). Renderer je stateless osim display cachea koji
/// se veže uz scenu i mijenja s njom.
/// </summary>
public sealed class SceneRenderer : IDisposable
{
    private static readonly SKColor BackgroundColor = new(0x1E, 0x1E, 0x1E);
    private static readonly SKColor OuterColor = new(0x4E, 0xC9, 0xB0);       // teal — vanjske
    private static readonly SKColor HoleColor = new(0x56, 0x9C, 0xD6);        // plava — rupe
    private static readonly SKColor OpenColor = new(0xD1, 0x9A, 0x66);        // narančasta — otvorene
    private static readonly SKColor UnclassifiedColor = new(0x80, 0x80, 0x80);
    private static readonly SKColor HighlightColor = new(0xFF, 0xD7, 0x00);   // žuta — selekcija
    private static readonly SKColor ErrorMarkerColor = new(0xF4, 0x47, 0x47);

    private readonly SKPaint _stroke = new() { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.2f };
    private readonly SKPaint _highlightStroke = new() { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2.8f, Color = HighlightColor };
    private readonly SKPaint _markerStroke = new() { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.6f, Color = ErrorMarkerColor };

    private DisplayTessellation _display = new();
    private RenderScene? _displayOwner;
    private readonly List<SceneSegment> _visibleBuffer = new(4096);

    /// <summary>Statistika zadnjeg framea (dijagnostika/benchmark).</summary>
    public int LastVisibleCount { get; private set; }

    public int LastDrawnCount { get; private set; }

    public void Draw(SKCanvas canvas, Camera2D camera, RenderScene scene, int highlightedContourId, Point2? issueMarker)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(scene);

        // Display cache prati scenu — nova scena invalidira cache (bez ručnog čišćenja).
        if (!ReferenceEquals(_displayOwner, scene))
        {
            _display = new DisplayTessellation();
            _displayOwner = scene;
        }

        canvas.Clear(BackgroundColor);

        // Culling: samo segmenti u vidljivom svijetu (+mala margina za debljinu linije).
        _visibleBuffer.Clear();
        scene.QueryVisible(camera.VisibleWorldBounds().Inflate(2.0 / camera.Scale), _visibleBuffer);
        LastVisibleCount = _visibleBuffer.Count;

        var drawn = 0;
        foreach (var item in _visibleBuffer)
        {
            if (DisplayTessellation.IsSubPixel(item.Segment, camera.Scale))
            {
                continue; // LOD: ispod praga piksela
            }

            var highlighted = item.Contour.Id == highlightedContourId;
            var paint = highlighted ? _highlightStroke : _stroke;
            if (!highlighted)
            {
                _stroke.Color = ColorFor(item.Contour.Kind);
            }

            DrawSegment(canvas, camera, item, paint);
            drawn++;
        }

        // Drugi prolaz: istaknuta kontura preko svega (čitljivost pri gustoj sceni).
        if (highlightedContourId >= 0)
        {
            foreach (var item in _visibleBuffer)
            {
                if (item.Contour.Id == highlightedContourId && !DisplayTessellation.IsSubPixel(item.Segment, camera.Scale))
                {
                    DrawSegment(canvas, camera, item, _highlightStroke);
                }
            }
        }

        if (issueMarker is { } marker)
        {
            var p = camera.WorldToScreen(marker);
            canvas.DrawCircle((float)p.X, (float)p.Y, 9f, _markerStroke);
            canvas.DrawLine((float)p.X - 14f, (float)p.Y, (float)p.X + 14f, (float)p.Y, _markerStroke);
            canvas.DrawLine((float)p.X, (float)p.Y - 14f, (float)p.X, (float)p.Y + 14f, _markerStroke);
        }

        LastDrawnCount = drawn;
    }

    private void DrawSegment(SKCanvas canvas, Camera2D camera, SceneSegment item, SKPaint paint)
    {
        switch (item.Segment)
        {
            case LineSeg line:
            {
                var a = camera.WorldToScreen(line.StartPoint);
                var b = camera.WorldToScreen(line.EndPoint);
                canvas.DrawLine((float)a.X, (float)a.Y, (float)b.X, (float)b.Y, paint);
                break;
            }

            case ArcSeg arc:
            {
                // Globalno jedinstven id segmenta za cache: (kontura << 16) | indeks.
                var segmentId = (item.Contour.Id << 16) | (item.SegmentIndex & 0xFFFF);
                var points = _display.GetArcPoints(segmentId, arc, camera.Scale);
                var previous = camera.WorldToScreen(points[0]);
                for (var i = 1; i < points.Count; i++)
                {
                    var current = camera.WorldToScreen(points[i]);
                    canvas.DrawLine((float)previous.X, (float)previous.Y, (float)current.X, (float)current.Y, paint);
                    previous = current;
                }

                break;
            }
        }
    }

    private static SKColor ColorFor(ContourKind kind) => kind switch
    {
        ContourKind.Outer => OuterColor,
        ContourKind.Hole => HoleColor,
        ContourKind.Open => OpenColor,
        _ => UnclassifiedColor,
    };

    public void Dispose()
    {
        _stroke.Dispose();
        _highlightStroke.Dispose();
        _markerStroke.Dispose();
    }
}
