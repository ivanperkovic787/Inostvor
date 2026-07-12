using Inostvor.Core.Abstractions;
using Inostvor.Core.Model.Toolpath;
using Inostvor.Kernel;

namespace Inostvor.Cam.Generation;

/// <summary>
/// Overcut: produljenje reza preko točke zatvaranja ponavljanjem početka putanje
/// do zadane duljine — eliminira "bradavicu" na spoju kod plazme. 0 = isključeno.
/// </summary>
public sealed class OvercutService : IOvercutService
{
    public IReadOnlyList<CutMove> Apply(IReadOnlyList<CutMove> moves, double overcutLength, double feedRate)
    {
        ArgumentNullException.ThrowIfNull(moves);
        ArgumentOutOfRangeException.ThrowIfNegative(overcutLength);

        if (overcutLength <= Tolerance.Geometric || moves.Count == 0)
        {
            return moves;
        }

        var result = new List<CutMove>(moves);
        var remaining = overcutLength;

        foreach (var move in moves)
        {
            if (move.Kind != MoveKind.Cut)
            {
                continue; // overcut ponavlja isključivo rezne poteze
            }

            var slice = SegmentSlicer.TakePrefix(move.Geometry, remaining);
            result.Add(new CutMove(slice, MoveKind.Overcut, feedRate));
            remaining -= slice.Length;
            if (remaining <= Tolerance.Geometric)
            {
                break;
            }
        }

        return result;
    }
}
