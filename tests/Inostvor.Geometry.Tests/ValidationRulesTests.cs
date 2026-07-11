using Inostvor.Core.Model.Geometry;
using Inostvor.Core.Model.Validation;
using Inostvor.Geometry.Contours;
using Inostvor.Geometry.Rules;
using Inostvor.Geometry.Validation;
using Inostvor.Kernel.Primitives;
using Inostvor.Sdk.Validation;
using Shouldly;
using Xunit;
using static Inostvor.Geometry.Tests.TestGeometry;

namespace Inostvor.Geometry.Tests;

public sealed class ValidationRulesTests
{
    private static readonly ContourBuilder Builder = new();
    private static readonly ContourClassifier Classifier = new();

    private static ValidationContext Context(params Core.Model.Import.ImportedEntity[] entities)
    {
        var settings = ContourBuildSettings.Default;
        var build = Builder.Build(entities, settings);
        return new ValidationContext(Classifier.Classify(build.Contours), build.Joins, settings);
    }

    [Fact]
    public void OpenContour_PrijavljujeRazmakILokaciju()
    {
        var ctx = Context(Entity("0",
            L(0, 0, 100, 0), L(100, 0.5, 100, 50), L(100, 50, 0, 50), L(0, 50, 0, 0)));

        var issues = new OpenContourRule().Evaluate(ctx).ToList();

        var issue = issues.ShouldHaveSingleItem();
        issue.Severity.ShouldBe(ValidationSeverity.Warning);
        issue.Code.ShouldBe("OPEN_CONTOUR");
        issue.Message.ShouldContain("0.5 mm");
        issue.Message.ShouldContain("tolerancije spajanja"); // razmak ≤ 10× tol → uputa korisniku
        issue.ContourId.ShouldBe(0);
        issue.Location.ShouldNotBeNull();
    }

    [Fact]
    public void OpenContour_DalekiKrajevi_BezUpute()
    {
        var ctx = Context(Entity("0", L(0, 0, 100, 0)));

        var issue = new OpenContourRule().Evaluate(ctx).Single();

        issue.Message.ShouldContain("100 mm");
        issue.Message.ShouldNotContain("povećaj toleranciju");
    }

    [Fact]
    public void JoinedGaps_SvakiSpojDobivaInfoNalaz()
    {
        var ctx = Context(Entity("0",
            L(0, 0, 100, 0), L(100, 0.02, 100, 50), L(100, 50, 0, 50), L(0, 50, 0.03, 0)));

        var issues = new JoinedGapsRule().Evaluate(ctx).ToList();

        issues.Count.ShouldBe(2); // unutarnji spoj 0.02 + zatvarajući 0.03
        issues.All(i => i.Severity == ValidationSeverity.Info && i.Code == "AUTO_JOINED_GAP").ShouldBeTrue();
        issues.ShouldContain(i => i.Message.Contains("0.02 mm"));
        issues.ShouldContain(i => i.Message.Contains("0.03 mm") && i.Message.Contains("zatvorio konturu"));
    }

    [Fact]
    public void SelfIntersection_MasnaKravata_Error()
    {
        var ctx = Context(Entity("0",
            L(0, 0, 2, 2), L(2, 2, 2, 0), L(2, 0, 0, 2)));

        var issue = new SelfIntersectionRule().Evaluate(ctx).Single();

        issue.Severity.ShouldBe(ValidationSeverity.Error);
        issue.Code.ShouldBe("SELF_INTERSECTION");
        issue.Location.ShouldNotBeNull();
        issue.Location.Value.AlmostEquals(new Point2(1, 1), 1e-9).ShouldBeTrue();
    }

    [Fact]
    public void SelfIntersection_CistiKvadrat_BezNalaza()
    {
        var ctx = Context(Entity("0", SquareLines(0, 0, 10)));
        new SelfIntersectionRule().Evaluate(ctx).ShouldBeEmpty();
    }

    [Fact]
    public void Duplicate_IdenticnaLinijaDvaput_Warning()
    {
        var ctx = Context(Entity("0", L(0, 0, 10, 0), L(0, 0, 10, 0)));

        var issue = new DuplicateGeometryRule().Evaluate(ctx).Single();

        issue.Severity.ShouldBe(ValidationSeverity.Warning);
        issue.Code.ShouldBe("DUPLICATE_GEOMETRY");
    }

    [Fact]
    public void Duplicate_ObrnutaKopija_TakoderDetektirana()
    {
        var ctx = Context(Entity("0", L(0, 0, 10, 0), L(10, 0, 0, 0)));
        new DuplicateGeometryRule().Evaluate(ctx).Single().Code.ShouldBe("DUPLICATE_GEOMETRY");
    }

    [Fact]
    public void Duplicate_DvaIdenticnaKruga_Detektirano()
    {
        var ctx = Context(
            Entity("0", FullCircle(5, 5, 3)),
            Entity("0", FullCircle(5, 5, 3)));

        new DuplicateGeometryRule().Evaluate(ctx).ShouldNotBeEmpty();
    }

    [Fact]
    public void Duplicate_RazlicitaGeometrija_BezNalaza()
    {
        var ctx = Context(Entity("0", SquareLines(0, 0, 10)), Entity("0", FullCircle(50, 50, 5)));
        new DuplicateGeometryRule().Evaluate(ctx).ShouldBeEmpty();
    }

    [Fact]
    public void ZeroLength_SegmentIspodPraga_Warning()
    {
        var ctx = Context(Entity("0", L(0, 0, 0.005, 0), L(0.005, 0, 10, 0)));

        var issue = new ZeroLengthSegmentRule().Evaluate(ctx).Single();

        issue.Code.ShouldBe("ZERO_LENGTH_SEGMENT");
        issue.Message.ShouldContain("0.005");
    }

    [Fact]
    public void ZeroLength_NormalnaGeometrija_BezNalaza()
    {
        var ctx = Context(Entity("0", SquareLines(0, 0, 10)));
        new ZeroLengthSegmentRule().Evaluate(ctx).ShouldBeEmpty();
    }

    [Fact]
    public void Validator_AgregiraSvaPravila()
    {
        var validator = new ToolpathValidator(
        [
            new OpenContourRule(),
            new JoinedGapsRule(),
            new SelfIntersectionRule(),
            new DuplicateGeometryRule(),
            new ZeroLengthSegmentRule(),
        ]);

        // Ulaz koji okida više pravila: otvorena kontura + spojeni razmak + rupa.
        var ctx = Context(
            Entity("0", SquareLines(0, 0, 100)),
            Entity("0", L(200, 0, 250, 0)),
            Entity("0", L(300, 0, 400, 0), L(400, 0.02, 400, 50), L(400, 50, 300, 50), L(300, 50, 300, 0)));

        var report = validator.Validate(ctx);

        report.Issues.ShouldContain(i => i.Code == "OPEN_CONTOUR");
        report.Issues.ShouldContain(i => i.Code == "AUTO_JOINED_GAP");
        report.HasErrors.ShouldBeFalse();
        report.WarningCount.ShouldBe(1);
        report.InfoCount.ShouldBe(1);
    }
}
