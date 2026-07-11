using Inostvor.Core.Model.Geometry;
using Inostvor.Core.Model.Import;
using Inostvor.Geometry.Contours;
using Inostvor.Kernel.Primitives;
using Inostvor.Rendering.Scene;
using Shouldly;
using Xunit;

namespace Inostvor.Rendering.Tests;

public sealed class RenderSceneTests
{
    private static RenderScene Scene()
    {
        // Ista geometrija kao CAM: konture iz stvarnog ContourBuildera/Classifiera.
        var builder = new ContourBuilder();
        var classifier = new ContourClassifier();
        var entities = new[]
        {
            new ImportedEntity(
            [
                new LineSeg(new Point2(0, 0), new Point2(100, 0)),
                new LineSeg(new Point2(100, 0), new Point2(100, 50)),
                new LineSeg(new Point2(100, 50), new Point2(0, 50)),
                new LineSeg(new Point2(0, 50), new Point2(0, 0)),
            ], "0", "TEST", null),
            new ImportedEntity([new ArcSeg(new Point2(500, 500), 10, 0, Math.Tau)], "0", "TEST", null),
        };
        var contours = classifier.Classify(builder.Build(entities, ContourBuildSettings.Default).Contours);
        return new RenderScene(contours, []);
    }

    [Fact]
    public void Bounds_ObuhvacajuSvuGeometriju()
    {
        var scene = Scene();
        scene.Bounds.Contains(new Point2(0, 0), 1e-9).ShouldBeTrue();
        scene.Bounds.Contains(new Point2(510, 510), 1e-9).ShouldBeTrue();
        scene.SegmentCount.ShouldBe(5);
    }

    [Fact]
    public void QueryVisible_VracaSamoSegmenteUProzoru()
    {
        var scene = Scene();
        var results = new List<SceneSegment>();

        scene.QueryVisible(new Aabb(-10, -10, 120, 60), results);

        results.Count.ShouldBe(4); // samo pravokutnik; krug je na (500, 500)
        results.All(s => s.Contour.Layer == "0").ShouldBeTrue();
    }

    [Fact]
    public void QueryVisible_PrazanProzor_NistaNeVraca()
    {
        var scene = Scene();
        var results = new List<SceneSegment>();

        scene.QueryVisible(new Aabb(2000, 2000, 2100, 2100), results);

        results.ShouldBeEmpty();
    }

    [Fact]
    public void PraznaScena_ValjaneGranice()
    {
        RenderScene.Empty.SegmentCount.ShouldBe(0);
        RenderScene.Empty.Bounds.Width.ShouldBeGreaterThan(0);
    }
}
