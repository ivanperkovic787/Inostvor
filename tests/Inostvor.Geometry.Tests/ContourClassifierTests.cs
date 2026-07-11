using Inostvor.Core.Model.Geometry;
using Inostvor.Geometry.Contours;
using Inostvor.Kernel.Primitives;
using Shouldly;
using Xunit;
using static Inostvor.Geometry.Tests.TestGeometry;

namespace Inostvor.Geometry.Tests;

public sealed class ContourClassifierTests
{
    private static readonly ContourBuilder Builder = new();
    private static readonly ContourClassifier Classifier = new();

    private static IReadOnlyList<Contour> BuildAndClassify(params Core.Model.Import.ImportedEntity[] entities)
        => Classifier.Classify(Builder.Build(entities, ContourBuildSettings.Default).Contours);

    [Fact]
    public void SamKvadrat_Outer_CcwOrijentacija()
    {
        var contours = BuildAndClassify(Entity("0", SquareLines(0, 0, 10)));

        var c = contours.ShouldHaveSingleItem();
        c.Kind.ShouldBe(ContourKind.Outer);
        ContourClassifier.SignedArea(c.Polyline).ShouldBe(100.0, 1e-9);
    }

    [Fact]
    public void CwUlaz_NormaliziranUCcwOuter()
    {
        // Kvadrat unesen CW (obrnuti segmenti obrnutim redoslijedom).
        var s = SquareLines(0, 0, 10);
        var cw = new ISegment[] { s[3].Reversed(), s[2].Reversed(), s[1].Reversed(), s[0].Reversed() };

        var contours = BuildAndClassify(Entity("0", cw));

        var c = contours.ShouldHaveSingleItem();
        c.Kind.ShouldBe(ContourKind.Outer);
        ContourClassifier.SignedArea(c.Polyline).ShouldBePositive(); // normalizirano u CCW
    }

    [Fact]
    public void PlocaSRupom_KrugPostajeHole_CwOrijentacija()
    {
        var contours = BuildAndClassify(
            Entity("0", SquareLines(0, 0, 100)),
            Entity("0", FullCircle(50, 50, 10)));

        contours.Count.ShouldBe(2);
        contours[0].Kind.ShouldBe(ContourKind.Outer);

        var hole = contours[1];
        hole.Kind.ShouldBe(ContourKind.Hole);
        ContourClassifier.SignedArea(hole.Polyline).ShouldBeNegative(); // Hole → CW
    }

    [Fact]
    public void TriRazineUgnjezdenja_OuterHoleOuter()
    {
        var contours = BuildAndClassify(
            Entity("0", SquareLines(0, 0, 100)),
            Entity("0", SquareLines(20, 20, 60)),
            Entity("0", SquareLines(40, 40, 20)));

        contours[0].Kind.ShouldBe(ContourKind.Outer);
        contours[1].Kind.ShouldBe(ContourKind.Hole);  // rupa u ploči
        contours[2].Kind.ShouldBe(ContourKind.Outer); // otok unutar rupe
    }

    [Fact]
    public void SignedArea_PuniKrug_EgzaktnoPiR2()
    {
        var ccw = new Polyline2([FullCircle(0, 0, 5)]);
        ContourClassifier.SignedArea(ccw).ShouldBe(Math.PI * 25.0, 1e-9);

        var cw = new Polyline2([FullCircle(0, 0, 5, ccw: false)]);
        ContourClassifier.SignedArea(cw).ShouldBe(-Math.PI * 25.0, 1e-9);
    }

    [Fact]
    public void SignedArea_SlotSLukovima_EgzaktnaFormula()
    {
        // Pravokutnik 60×20 + dva polukruga r=10: A = 1200 + 100π.
        var slot = new Polyline2(
        [
            new LineSeg(new Point2(0, 0), new Point2(60, 0)),
            ArcSeg.FromStartEndCenter(new Point2(60, 0), new Point2(60, 20), new Point2(60, 10), isCcw: true),
            new LineSeg(new Point2(60, 20), new Point2(0, 20)),
            ArcSeg.FromStartEndCenter(new Point2(0, 20), new Point2(0, 0), new Point2(0, 10), isCcw: true),
        ]);

        ContourClassifier.SignedArea(slot).ShouldBe(1200.0 + (100.0 * Math.PI), 1e-9);
    }

    [Fact]
    public void OtvorenaKontura_ProlaziKaoOpen_BezKlasifikacije()
    {
        var contours = BuildAndClassify(
            Entity("0", SquareLines(0, 0, 100)),
            Entity("0", L(200, 0, 250, 0)));

        contours[1].Kind.ShouldBe(ContourKind.Open);
    }

    [Fact]
    public void RupaVecUnesenaCw_OstajeCw()
    {
        var contours = BuildAndClassify(
            Entity("0", SquareLines(0, 0, 100)),
            Entity("0", FullCircle(50, 50, 10, ccw: false)));

        var hole = contours[1];
        hole.Kind.ShouldBe(ContourKind.Hole);
        ContourClassifier.SignedArea(hole.Polyline).ShouldBeNegative();
    }

    [Fact]
    public void ViseDijelovaNaPlocice_SvakiOuterSaSvojomRupom()
    {
        var contours = BuildAndClassify(
            Entity("0", SquareLines(0, 0, 50)),
            Entity("0", FullCircle(25, 25, 5)),
            Entity("0", SquareLines(100, 0, 50)),
            Entity("0", FullCircle(125, 25, 5)));

        contours[0].Kind.ShouldBe(ContourKind.Outer);
        contours[1].Kind.ShouldBe(ContourKind.Hole);
        contours[2].Kind.ShouldBe(ContourKind.Outer);
        contours[3].Kind.ShouldBe(ContourKind.Hole);
    }

    [Fact]
    public void Determinizam_KlasifikacijaPonovljiva()
    {
        var build = Builder.Build(
        [
            Entity("0", SquareLines(0, 0, 100)),
            Entity("0", SquareLines(20, 20, 60)),
            Entity("0", FullCircle(50, 50, 10)),
        ], ContourBuildSettings.Default).Contours;

        var reference = Classifier.Classify(build);
        for (var run = 0; run < 5; run++)
        {
            var result = Classifier.Classify(build);
            for (var i = 0; i < result.Count; i++)
            {
                result[i].Id.ShouldBe(reference[i].Id);
                result[i].Kind.ShouldBe(reference[i].Kind);
                result[i].Polyline.StartPoint.ShouldBe(reference[i].Polyline.StartPoint);
            }
        }
    }
}
