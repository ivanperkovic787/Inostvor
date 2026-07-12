using Inostvor.Cam.Fitting;
using Inostvor.Cam.Generation;
using Inostvor.Cam.Leads;
using Inostvor.Cam.Offset;
using Inostvor.Core.Model.Toolpath;
using Inostvor.Kernel.Primitives;
using Shouldly;
using Xunit;
using static Inostvor.Cam.Tests.CamTestHelpers;

namespace Inostvor.Cam.Tests;

public sealed class ToolpathGeneratorTests
{
    private static ToolpathGenerator Generator() => new(
        new KerfOffsetService(),
        new ArcFitter(),
        new LeadGeneratorService([new LineLeadStrategy(), new ArcLeadStrategy()]),
        new OvercutService(),
        new CutOrderStrategyProvider([new DefaultCutOrderStrategy(), new NearestNeighborCutOrderStrategy()]));

    [Fact]
    public void PlocaSRupom_DvijeSekvence_RupaPrva_TocneStrane()
    {
        var contours = Contours(("0", SquareLines(0, 0, 100)), ("0", [FullCircle(50, 50, 10)]));
        var tech = TechnologySettings.Default with { KerfWidth = 2.0, OvercutLength = 0.0 };

        var program = Generator().Generate(contours, tech);

        program.Sequences.Count.ShouldBe(2);
        program.Rapids.Count.ShouldBe(2);
        program.Rapids[0].From.ShouldBe(new Point2(0, 0)); // ishodište stroja

        var holeSeq = program.Sequences[0];
        var outerSeq = program.Sequences[1];
        holeSeq.SourceContourId.ShouldBe(contours[1].Id);  // rupa PRIJE vanjske

        // Rupa: rezni potezi na r ≈ 9 od centra; Outer: putanja izvan kvadrata.
        var center = new Point2(50, 50);
        foreach (var m in holeSeq.Moves.Where(m => m.Kind == MoveKind.Cut))
        {
            center.DistanceTo(m.Geometry.PointAt(0.5)).ShouldBe(9.0, 0.05);
        }

        outerSeq.Moves.Where(m => m.Kind == MoveKind.Cut)
            .SelectMany(m => new[] { m.Geometry.StartPoint, m.Geometry.EndPoint })
            .ShouldContain(p => p.X < -0.5 || p.X > 100.5 || p.Y < -0.5 || p.Y > 100.5);
    }

    [Fact]
    public void Sekvenca_SadrziLeadInCutLeadOut_PierceNaPocetkuLeada()
    {
        var contours = Contours(("0", [FullCircle(50, 50, 10)]));
        var program = Generator().Generate(contours, TechnologySettings.Default with { KerfWidth = 2.0 });

        var seq = program.Sequences.ShouldHaveSingleItem();
        seq.Moves[0].Kind.ShouldBe(MoveKind.LeadIn);
        seq.Moves[^1].Kind.ShouldBe(MoveKind.LeadOut);
        seq.Moves.ShouldContain(m => m.Kind == MoveKind.Cut);

        seq.PiercePoint.ShouldBe(seq.Moves[0].Geometry.StartPoint);
        // Lead-in završava točno na početku prvog reznog poteza (kontinuitet).
        var firstCut = seq.Moves.First(m => m.Kind == MoveKind.Cut);
        seq.Moves[0].Geometry.EndPoint.AlmostEquals(firstCut.Geometry.StartPoint, 1e-6).ShouldBeTrue();
    }

    [Fact]
    public void ArcFittingIskljucen_SviReznipoteziLinije()
    {
        var contours = Contours(("0", [FullCircle(50, 50, 10)]));
        var tech = TechnologySettings.Default with { EnableArcFitting = false, LeadInStyle = LeadStyle.None, LeadOutStyle = LeadStyle.None };

        var program = Generator().Generate(contours, tech);

        program.Sequences.Single().Moves
            .Where(m => m.Kind == MoveKind.Cut)
            .ShouldAllBe(m => m.Geometry is LineSeg);
    }

    [Fact]
    public void ArcFittingUkljucen_KrugPostajeLukovi()
    {
        var contours = Contours(("0", [FullCircle(50, 50, 10)]));
        var program = Generator().Generate(contours, TechnologySettings.Default with { KerfWidth = 2.0 });

        program.Sequences.Single().Moves
            .Where(m => m.Kind == MoveKind.Cut)
            .ShouldAllBe(m => m.Geometry is ArcSeg); // kompresija natrag u G2/G3 geometriju
    }

    [Fact]
    public void OtvorenaKontura_SredisnjicaBezLeadova()
    {
        var contours = Contours(("0", [L(0, 0, 100, 0)]));
        var program = Generator().Generate(contours, TechnologySettings.Default);

        var seq = program.Sequences.ShouldHaveSingleItem();
        seq.Moves.ShouldAllBe(m => m.Kind == MoveKind.Cut);
        seq.CutLength.ShouldBe(100.0, 1e-6);
        seq.PiercePoint.ShouldBe(new Point2(0, 0));
    }

    [Fact]
    public void Statistika_EgzaktnaMatematika()
    {
        var contours = Contours(("0", [L(0, 0, 100, 0)]));
        var tech = TechnologySettings.Default with { FeedRate = 6000, RapidRate = 6000, PierceTime = 0.5 };

        var stats = Generator().Generate(contours, tech).Statistics;

        stats.CutLength.ShouldBe(100.0, 1e-6);
        stats.CutTimeSeconds.ShouldBe(1.0, 1e-6);   // 100 mm @ 6000 mm/min = 1 s
        stats.RapidLength.ShouldBe(0.0, 1e-9);      // pierce je u ishodištu (0,0)
        stats.PierceCount.ShouldBe(1);
        stats.PierceTimeSeconds.ShouldBe(0.5, 1e-9);
        stats.TotalTimeSeconds.ShouldBe(1.5, 1e-6);
    }

    [Fact]
    public void Overcut_UkljucenUProgram()
    {
        var contours = Contours(("0", SquareLines(0, 0, 50)));
        var tech = TechnologySettings.Default with { OvercutLength = 3.0, LeadInStyle = LeadStyle.None, LeadOutStyle = LeadStyle.None };

        var program = Generator().Generate(contours, tech);

        program.Sequences.Single().Moves
            .Where(m => m.Kind == MoveKind.Overcut)
            .Sum(m => m.Length).ShouldBe(3.0, 1e-6);
    }

    [Fact]
    public void Determinizam_TriPokretanja_IdenticanProgram()
    {
        var contours = Contours(
            ("0", SquareLines(0, 0, 100)),
            ("0", [FullCircle(30, 30, 8)]),
            ("0", [FullCircle(70, 70, 8)]));
        var tech = TechnologySettings.Default with { KerfWidth = 1.6, OvercutLength = 2.0 };
        var generator = Generator();

        static List<string> Fingerprint(ToolpathProgram p) =>
            p.Sequences.SelectMany(s => s.Moves)
                .Select(m => FormattableString.Invariant(
                    $"{m.Kind}|{m.Geometry.GetType().Name}|{m.Geometry.StartPoint}|{m.Geometry.EndPoint}|{m.Length:0.#########}"))
                .ToList();

        var reference = Fingerprint(generator.Generate(contours, tech));
        reference.ShouldNotBeEmpty();

        for (var i = 0; i < 3; i++)
        {
            Fingerprint(generator.Generate(contours, tech)).ShouldBe(reference);
        }
    }

    [Fact]
    public void IR_NeSadrziNistaKontrolerSpecifino()
    {
        // Strukturna provjera neutralnosti: IR tipovi nemaju string reprezentacije G-koda.
        var contours = Contours(("0", [FullCircle(50, 50, 10)]));
        var program = Generator().Generate(contours, TechnologySettings.Default);

        foreach (var move in program.Sequences.SelectMany(s => s.Moves))
        {
            (move.Geometry is LineSeg || move.Geometry is ArcSeg).ShouldBeTrue();
            move.FeedRate.ShouldBeGreaterThan(0);
        }
    }
}
