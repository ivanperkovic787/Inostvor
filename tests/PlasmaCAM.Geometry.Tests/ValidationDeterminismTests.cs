using PlasmaCAM.Core.Model.Geometry;
using PlasmaCAM.Core.Model.Validation;
using PlasmaCAM.Geometry.Contours;
using PlasmaCAM.Geometry.Rules;
using PlasmaCAM.Geometry.Validation;
using PlasmaCAM.Sdk.Validation;
using Shouldly;
using Xunit;
using static PlasmaCAM.Geometry.Tests.TestGeometry;

namespace PlasmaCAM.Geometry.Tests;

/// <summary>
/// Determinizam izvještaja — eksplicitni zahtjev: za isti ulaz redoslijed nalaza
/// mora biti IDENTIČAN u svakom pokretanju, neovisno o redoslijedu pravila.
/// </summary>
public sealed class ValidationDeterminismTests
{
    private static readonly ContourBuilder Builder = new();
    private static readonly ContourClassifier Classifier = new();

    private static IValidationRule[] AllRules() =>
    [
        new OpenContourRule(),
        new JoinedGapsRule(),
        new SelfIntersectionRule(),
        new DuplicateGeometryRule(),
        new ZeroLengthSegmentRule(),
    ];

    private static Core.Model.Import.ImportedEntity[] RichInput() =>
    [
        Entity("0", SquareLines(0, 0, 100)),                                   // čist Outer
        Entity("0", FullCircle(50, 50, 10)),                                   // rupa
        Entity("0", L(200, 0, 300, 0), L(300, 0.02, 300, 50)),                 // spoj + otvorena
        Entity("0", L(400, 0, 402, 2), L(402, 2, 402, 0), L(402, 0, 400, 2)),  // samopresjek
        Entity("0", L(500, 0, 510, 0), L(510, 0, 500, 0)),                     // duplikat (obrnut)
        Entity("HOLES", L(600, 0, 600.005, 0), L(600.005, 0, 620, 0)),         // sitni segment
    ];

    private static ValidationContext BuildContext()
    {
        var settings = ContourBuildSettings.Default;
        var build = Builder.Build(RichInput(), settings);
        return new ValidationContext(Classifier.Classify(build.Contours), build.Joins, settings);
    }

    private static List<string> Fingerprint(ValidationReport report)
        => report.Issues
            .Select(i => FormattableString.Invariant($"{i.Severity}|{i.Code}|{i.ContourId}|{i.Location?.X:0.######}|{i.Location?.Y:0.######}|{i.Message}"))
            .ToList();

    [Fact]
    public void IstiUlaz_PetPokretanja_IdenticanRedoslijedNalaza()
    {
        var reference = Fingerprint(new ToolpathValidator(AllRules()).Validate(BuildContext()));
        reference.ShouldNotBeEmpty();

        for (var run = 0; run < 5; run++)
        {
            var fingerprint = Fingerprint(new ToolpathValidator(AllRules()).Validate(BuildContext()));
            fingerprint.ShouldBe(reference);
        }
    }

    [Fact]
    public void ObrnutRedoslijedPravila_Identican_Izvjestaj()
    {
        var forward = Fingerprint(new ToolpathValidator(AllRules()).Validate(BuildContext()));
        var backward = Fingerprint(new ToolpathValidator(AllRules().Reverse().ToArray()).Validate(BuildContext()));

        backward.ShouldBe(forward);
    }

    [Fact]
    public void Sortiranje_ErrorPrijeWarningaPrijeInfa()
    {
        var report = new ToolpathValidator(AllRules()).Validate(BuildContext());

        var severities = report.Issues.Select(i => (int)i.Severity).ToList();
        severities.ShouldBe(severities.OrderBy(s => s).ToList());
        report.HasErrors.ShouldBeTrue(); // samopresjek u ulazu
    }

    [Fact]
    public void Report_StabilnoSortiranje_UnutarIstogKodaPoKonturiPaLokaciji()
    {
        // Dvije otvorene konture — nalazi istog koda moraju biti po ContourId.
        var settings = ContourBuildSettings.Default;
        var build = Builder.Build(
        [
            Entity("0", L(0, 0, 10, 0)),
            Entity("0", L(50, 0, 60, 0)),
        ], settings);
        var ctx = new ValidationContext(Classifier.Classify(build.Contours), build.Joins, settings);

        var report = new ToolpathValidator([new OpenContourRule()]).Validate(ctx);

        report.Issues.Count.ShouldBe(2);
        report.Issues[0].ContourId.ShouldBe(0);
        report.Issues[1].ContourId.ShouldBe(1);
    }
}
