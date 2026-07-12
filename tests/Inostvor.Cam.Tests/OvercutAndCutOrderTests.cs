using Inostvor.Cam.Generation;
using Inostvor.Core.Model.Toolpath;
using Inostvor.Kernel.Primitives;
using Shouldly;
using Xunit;
using static Inostvor.Cam.Tests.CamTestHelpers;

namespace Inostvor.Cam.Tests;

public sealed class OvercutAndCutOrderTests
{
    [Fact]
    public void Overcut_DodajeTocnuDuljinu_PreskaceLeadove()
    {
        var moves = new List<CutMove>
        {
            new(L(-4, -4, 0, 0), MoveKind.LeadIn, 2500),
            new(L(0, 0, 10, 0), MoveKind.Cut, 2500),
            new(L(10, 0, 10, 10), MoveKind.Cut, 2500),
        };

        var result = new OvercutService().Apply(moves, overcutLength: 6.0, feedRate: 2500);

        var overcuts = result.Where(m => m.Kind == MoveKind.Overcut).ToList();
        overcuts.Sum(m => m.Length).ShouldBe(6.0, 1e-9);
        overcuts[0].Geometry.StartPoint.ShouldBe(new Point2(0, 0)); // počinje od PRVOG Cut poteza
        overcuts[0].Geometry.EndPoint.ShouldBe(new Point2(6, 0));   // prefiks, ne cijeli segment
    }

    [Fact]
    public void Overcut_DuljiOdPrvogSegmenta_NastavljaNaSljedeci()
    {
        var moves = new List<CutMove>
        {
            new(L(0, 0, 10, 0), MoveKind.Cut, 2500),
            new(L(10, 0, 10, 10), MoveKind.Cut, 2500),
        };

        var result = new OvercutService().Apply(moves, 13.0, 2500);

        var overcuts = result.Where(m => m.Kind == MoveKind.Overcut).ToList();
        overcuts.Count.ShouldBe(2);
        overcuts.Sum(m => m.Length).ShouldBe(13.0, 1e-9);
        overcuts[1].Geometry.EndPoint.ShouldBe(new Point2(10, 3));
    }

    [Fact]
    public void Overcut_Nula_UlazNepromijenjen()
    {
        var moves = new List<CutMove> { new(L(0, 0, 10, 0), MoveKind.Cut, 2500) };
        new OvercutService().Apply(moves, 0.0, 2500).Count.ShouldBe(1);
    }

    [Fact]
    public void Overcut_NaLuku_PrefiksLuka()
    {
        var circle = FullCircle(0, 0, 10); // opseg 20π
        var moves = new List<CutMove> { new(circle, MoveKind.Cut, 2500) };

        var result = new OvercutService().Apply(moves, 5.0, 2500);

        var overcut = result.Single(m => m.Kind == MoveKind.Overcut);
        var arc = overcut.Geometry.ShouldBeOfType<ArcSeg>();
        arc.Length.ShouldBe(5.0, 1e-9);
        arc.StartPoint.AlmostEquals(circle.StartPoint, 1e-9).ShouldBeTrue();
    }

    [Fact]
    public void CutOrder_RupePrijeVanjske_DijeloviPoPoziciji()
    {
        // Dva dijela: B je NIŽE (MinY manji) pa ide prvi; svaki ima rupu.
        var contours = Contours(
            ("0", SquareLines(0, 100, 50)),          // dio A outer (gore)  → id 0
            ("0", [FullCircle(25, 125, 5)]),         // rupa A              → id 1
            ("0", SquareLines(0, 0, 50)),            // dio B outer (dolje) → id 2
            ("0", [FullCircle(25, 25, 5)]));         // rupa B              → id 3

        var sequences = contours
            .Select(c => new CutSequence(c.Id, c.Polyline.StartPoint, []))
            .ToList();

        var ordered = new DefaultCutOrderStrategy().Order(sequences, contours);

        ordered.Select(s => s.SourceContourId).ShouldBe([3, 2, 1, 0]); // B: rupa, outer; A: rupa, outer
    }

    [Fact]
    public void CutOrder_Deterministican()
    {
        var contours = Contours(
            ("0", SquareLines(0, 0, 50)), ("0", [FullCircle(25, 25, 5)]),
            ("0", SquareLines(100, 0, 50)), ("0", [FullCircle(125, 25, 5)]));
        var sequences = contours.Select(c => new CutSequence(c.Id, c.Polyline.StartPoint, [])).ToList();
        var strategy = new DefaultCutOrderStrategy();

        var reference = strategy.Order(sequences, contours).Select(s => s.SourceContourId).ToList();
        for (var i = 0; i < 5; i++)
        {
            strategy.Order(sequences, contours).Select(s => s.SourceContourId).ShouldBe(reference);
        }
    }
}
