using Inostvor.Core.Model.Toolpath;
using Inostvor.Kernel.Primitives;

namespace Inostvor.Post.Tests;

/// <summary>
/// Ručno konstruiran, potpuno deterministički ToolpathProgram za golden testove —
/// namjerno BEZ CAM cjevovoda (Clipper/fitting) da golden ovisi isključivo o emitteru.
/// </summary>
internal static class GoldenTestProgram
{
    public static ToolpathProgram Build()
    {
        const double feed = 3000.0;
        var tech = TechnologySettings.Default with { FeedRate = feed, RapidRate = 6000.0, PierceTime = 0.6 };

        var seq1 = new CutSequence(7, new Point2(10, 15),
        [
            new CutMove(new LineSeg(new Point2(10, 15), new Point2(10, 20)), MoveKind.LeadIn, feed),
            new CutMove(new LineSeg(new Point2(10, 20), new Point2(60, 20)), MoveKind.Cut, feed),
            new CutMove(new LineSeg(new Point2(60, 20), new Point2(60, 70)), MoveKind.Cut, feed),
            new CutMove(new LineSeg(new Point2(60, 70), new Point2(65, 70)), MoveKind.LeadOut, feed),
        ]);

        // Polukrug CCW: centar (100,10), r 10, od (100,0) do (100,20) preko (110,10).
        var seq2 = new CutSequence(9, new Point2(100, 0),
        [
            new CutMove(new ArcSeg(new Point2(100, 10), 10, -Math.PI / 2.0, Math.PI), MoveKind.Cut, feed),
        ]);

        var rapids = new List<RapidMove>
        {
            new(new Point2(0, 0), new Point2(10, 15)),
            new(new Point2(65, 70), new Point2(100, 0)),
        };

        var cutLength = seq1.CutLength + seq2.CutLength;
        var rapidLength = rapids.Sum(r => r.Length);
        var stats = new ToolpathStatistics(
            cutLength, rapidLength,
            cutLength / feed * 60.0,
            rapidLength / 6000.0 * 60.0,
            2 * 0.6, 2);

        return new ToolpathProgram([seq1, seq2], rapids, tech, stats);
    }
}
