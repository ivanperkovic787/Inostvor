using Inostvor.Core.Model.Geometry;
using Inostvor.Core.Model.Import;
using Inostvor.Core.Model.Validation;
using Inostvor.Geometry.Contours;
using Inostvor.Kernel.Primitives;
using Inostvor.Rendering.Scene;
using Inostvor.ViewModels;
using Shouldly;
using Xunit;

namespace Inostvor.ViewModels.Tests;

public sealed class ViewportViewModelTests
{
    private static RenderScene SceneWithSquare()
    {
        var builder = new ContourBuilder();
        var entities = new[]
        {
            new ImportedEntity(
            [
                new LineSeg(new Point2(0, 0), new Point2(100, 0)),
                new LineSeg(new Point2(100, 0), new Point2(100, 100)),
                new LineSeg(new Point2(100, 100), new Point2(0, 100)),
                new LineSeg(new Point2(0, 100), new Point2(0, 0)),
            ], "0", "TEST", null),
        };
        var contours = new ContourClassifier().Classify(builder.Build(entities, ContourBuildSettings.Default).Contours);
        return new RenderScene(contours, []);
    }

    [Fact]
    public void SetScene_ZoomExtentsIRedraw()
    {
        var vm = new ViewportViewModel();
        vm.SetViewportSize(800, 600);
        var redraws = 0;
        vm.RedrawRequested += (_, _) => redraws++;

        vm.SetScene(SceneWithSquare());

        vm.Camera.Center.X.ShouldBe(50, 1e-9);
        vm.Camera.Center.Y.ShouldBe(50, 1e-9);
        redraws.ShouldBe(1);
    }

    [Fact]
    public void ZoomToIssue_SKonturom_CentriraIOznaci()
    {
        var vm = new ViewportViewModel();
        vm.SetViewportSize(800, 600);
        vm.SetScene(SceneWithSquare());
        var contourId = vm.Scene.Contours[0].Id;

        vm.ZoomToIssue(new ValidationIssue(
            ValidationSeverity.Error, "SELF_INTERSECTION", "test", contourId, new Point2(50, 50)));

        vm.HighlightedContourId.ShouldBe(contourId);
        vm.IssueMarker.ShouldBe(new Point2(50, 50));
        vm.Camera.Center.X.ShouldBe(50, 1e-9); // centriran na konturu
    }

    [Fact]
    public void ZoomToIssue_BezKonture_CentriraNaLokaciju()
    {
        var vm = new ViewportViewModel();
        vm.SetViewportSize(800, 600);
        vm.SetScene(SceneWithSquare());

        vm.ZoomToIssue(new ValidationIssue(
            ValidationSeverity.Info, "AUTO_JOINED_GAP", "test", ContourId: -1, new Point2(300, 400)));

        vm.HighlightedContourId.ShouldBe(-1);
        vm.Camera.Center.ShouldBe(new Point2(300, 400));
    }
}
