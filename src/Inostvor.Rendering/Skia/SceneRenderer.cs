using Inostvor.Cam.Simulation;
using Inostvor.Core.Model.Geometry;
using Inostvor.Core.Model.Toolpath;
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

    private static readonly SKColor RapidColor = new(0x6A, 0x6A, 0x6A);
    private static readonly SKColor CutPathColor = new(0xE8, 0x4D, 0x4D);      // crvena — rez
    private static readonly SKColor LeadColor = new(0x98, 0xC3, 0x79);          // zelena — leadovi
    private static readonly SKColor TorchOnColor = new(0xFF, 0xA5, 0x00);
    private static readonly SKColor TorchOffColor = new(0x9C, 0x9C, 0x9C);

    private readonly SKPaint _rapidStroke = new()
    {
        IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.0f, Color = RapidColor,
        PathEffect = SKPathEffect.CreateDash([6f, 4f], 0f),
    };

    private readonly SKPaint _toolpathStroke = new() { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.6f };
    private readonly SKPaint _torchFill = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    private readonly SKPaint _torchRing = new() { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2.0f };

    private DisplayTessellation _display = new();
    private RenderScene? _displayOwner;
    private readonly List<SceneSegment> _visibleBuffer = new(4096);

    /// <summary>Statistika zadnjeg framea (dijagnostika/benchmark).</summary>
    public int LastVisibleCount { get; private set; }

    public int LastDrawnCount { get; private set; }

    public void Draw(
        SKCanvas canvas,
        Camera2D camera,
        RenderScene scene,
        int highlightedContourId,
        Point2? issueMarker,
        ToolpathProgram? toolpath = null,
        SimulationState? simulation = null)
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

        if (toolpath is not null)
        {
            DrawToolpath(canvas, camera, toolpath);
        }

        if (simulation is not null)
        {
            DrawTorch(canvas, camera, simulation);
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

    /// <summary>Overlay putanje iz NEUTRALNOG IR-a: rapids crtkano, leadovi zeleno, rez crveno.</summary>
    private void DrawToolpath(SKCanvas canvas, Camera2D camera, ToolpathProgram toolpath)
    {
        foreach (var rapidMove in toolpath.Rapids)
        {
            var a = camera.WorldToScreen(rapidMove.From);
            var b = camera.WorldToScreen(rapidMove.To);
            canvas.DrawLine((float)a.X, (float)a.Y, (float)b.X, (float)b.Y, _rapidStroke);
        }

        foreach (var sequence in toolpath.Sequences)
        {
            foreach (var move in sequence.Moves)
            {
                _toolpathStroke.Color = move.Kind == MoveKind.Cut || move.Kind == MoveKind.Overcut
                    ? CutPathColor
                    : LeadColor;
                DrawGeometry(canvas, camera, move.Geometry, _toolpathStroke);
            }
        }
    }

    private void DrawTorch(SKCanvas canvas, Camera2D camera, SimulationState state)
    {
        var p = camera.WorldToScreen(state.Position);
        _torchFill.Color = state.TorchOn ? TorchOnColor : TorchOffColor;
        _torchRing.Color = _torchFill.Color;
        canvas.DrawCircle((float)p.X, (float)p.Y, 4f, _torchFill);
        canvas.DrawCircle((float)p.X, (float)p.Y, 8f, _torchRing);
    }

    private static void DrawGeometry(SKCanvas canvas, Camera2D camera, ISegment geometry, SKPaint paint)
    {
        switch (geometry)
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
                // Toolpath overlay nije u display cacheu (mijenja se s tehnologijom, ne sa scenom) —
                // izravna tessellacija po zoomu; broj lukova putanje je malen u odnosu na scenu.
                var points = Kernel.Tessellation.TessellateArc(arc, DisplayTessellation.WorldChordTolerance(camera.Scale));
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
        _rapidStroke.Dispose();
        _toolpathStroke.Dispose();
        _torchFill.Dispose();
        _torchRing.Dispose();
    }
}
