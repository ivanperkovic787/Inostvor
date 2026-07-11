using Inostvor.Core.Model.Geometry;
using Inostvor.Core.Model.Validation;
using Inostvor.Kernel.Primitives;
using Inostvor.Kernel.Spatial;

namespace Inostvor.Rendering.Scene;

/// <summary>
/// Nepromjenjiva scena za prikaz — izgrađena iz REZULTATA jezgre
/// (GeometryPipelineResult). Renderer NE sadrži CAM logiku: scena je isti
/// geometrijski model (Contour/Polyline2/ISegment) koji koristi CAM, plus
/// prostorni indeks za culling. Nova scena = novi objekt (invalidacija cachea).
/// </summary>
public sealed class RenderScene
{
    private readonly AabbTree<SceneSegment> _spatialIndex = new();

    public RenderScene(IReadOnlyList<Contour> contours, IReadOnlyList<ValidationIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(contours);
        ArgumentNullException.ThrowIfNull(issues);

        Contours = contours;
        Issues = issues;

        var segments = new List<SceneSegment>();
        var bounds = default(Aabb?);
        foreach (var contour in contours)
        {
            for (var i = 0; i < contour.Polyline.Count; i++)
            {
                var segment = new SceneSegment(contour, i, contour.Polyline[i]);
                segments.Add(segment);
                _spatialIndex.Insert(segment.Segment.Bounds, segment);
                bounds = bounds is null ? segment.Segment.Bounds : bounds.Value.Union(segment.Segment.Bounds);
            }
        }

        Segments = segments;
        Bounds = bounds ?? new Aabb(0, 0, 1, 1);
    }

    public IReadOnlyList<Contour> Contours { get; }

    public IReadOnlyList<ValidationIssue> Issues { get; }

    public IReadOnlyList<SceneSegment> Segments { get; }

    /// <summary>Granice cijele scene (za Zoom Extents).</summary>
    public Aabb Bounds { get; }

    public int SegmentCount => Segments.Count;

    /// <summary>Viewport culling: samo segmenti čiji AABB siječe vidljivo područje.</summary>
    public void QueryVisible(Aabb visibleWorld, ICollection<SceneSegment> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        _spatialIndex.Query(visibleWorld, results);
    }

    public static RenderScene Empty { get; } = new([], []);
}

/// <summary>Jedan segment scene s referencom na konturu (za selekciju/isticanje).</summary>
public sealed record SceneSegment(Contour Contour, int SegmentIndex, ISegment Segment);
