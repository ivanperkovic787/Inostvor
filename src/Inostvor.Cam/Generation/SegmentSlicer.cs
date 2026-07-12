using Inostvor.Kernel.Primitives;

namespace Inostvor.Cam.Generation;

/// <summary>Rezanje prefiksa segmenta po duljini (za overcut).</summary>
internal static class SegmentSlicer
{
    /// <summary>Prvih <paramref name="length"/> mm segmenta; cijeli segment ako je kraći.</summary>
    public static ISegment TakePrefix(ISegment segment, double length)
    {
        if (length >= segment.Length)
        {
            return segment;
        }

        var t = length / segment.Length;
        return segment switch
        {
            ArcSeg arc => new ArcSeg(arc.Center, arc.Radius, arc.StartAngle, arc.SweepAngle * t),
            _ => new LineSeg(segment.StartPoint, segment.PointAt(t)),
        };
    }
}
