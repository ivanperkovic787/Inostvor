// =====================================================================================
// JEDINA datoteka u solutionu koja dodiruje Clipper2 API (analogno netDxf izolaciji).
// ADR-001: Clipper radi u Int64 MIKROMETRIMA (skala 1000 od mm) — eliminira
// probleme plutajućeg zareza u boolean/offset operacijama.
//
// Pretpostavljena API površina (Clipper2 1.5.x, provjeriti na prvom Windows buildu):
//   Clipper2Lib.Point64(long x, long y), Path64 : List<Point64>, Paths64 : List<Path64>
//   Clipper.InflatePaths(Paths64, double delta, JoinType, EndType) → Paths64
//   Clipper.Area(Path64) → double (CCW pozitivna)
//   JoinType.Round, EndType.Polygon
// =====================================================================================
using Clipper2Lib;
using Inostvor.Kernel.Primitives;

namespace Inostvor.Cam.Offset;

internal static class ClipperAdapter
{
    /// <summary>µm po mm (ADR-001).</summary>
    public const double Scale = 1000.0;

    /// <summary>
    /// Offset zatvorenog prstena za signed deltu (mm; + širi, − skuplja CCW prsten).
    /// Ulaz mora biti CCW. Izlaz: mm točke, SVAKI prsten CCW, nenormaliziran.
    /// </summary>
    public static List<List<Point2>> InflateClosed(IReadOnlyList<Point2> ccwRing, double deltaMm)
    {
        var path = new Path64(ccwRing.Count);
        foreach (var p in ccwRing)
        {
            path.Add(new Point64((long)Math.Round(p.X * Scale), (long)Math.Round(p.Y * Scale)));
        }

        var solution = Clipper.InflatePaths([path], deltaMm * Scale, JoinType.Round, EndType.Polygon);

        var result = new List<List<Point2>>(solution.Count);
        foreach (var solved in solution)
        {
            var ring = new List<Point2>(solved.Count);
            foreach (var p in solved)
            {
                ring.Add(new Point2(p.X / Scale, p.Y / Scale));
            }

            // Clipper vanjske prstenove vraća CCW (pozitivna površina); rupe u rješenju CW.
            if (Clipper.Area(solved) < 0)
            {
                ring.Reverse();
            }

            result.Add(ring);
        }

        return result;
    }
}
