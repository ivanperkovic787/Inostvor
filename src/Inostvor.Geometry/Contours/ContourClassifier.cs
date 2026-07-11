using Inostvor.Core.Abstractions;
using Inostvor.Core.Model.Geometry;
using Inostvor.Kernel;
using Inostvor.Kernel.Primitives;

namespace Inostvor.Geometry.Contours;

/// <summary>
/// Klasifikacija zatvorenih kontura ugnježđivanjem (parna dubina = Outer, neparna = Hole)
/// i normalizacija orijentacije: Outer → CCW, Hole → CW (konvencija za kerf offset u M5).
///
/// Površina se računa EGZAKTNO (shoelace po vrhovima + korekcije kružnih odsječaka
/// ½r²(θ − sin θ) za lukove). Sadržavanje (točka-u-poligonu) koristi tesselliranu
/// aproksimaciju lukova (tolerancija 0.01 mm) — dovoljno za odnos ugnježđivanja,
/// dokumentirana aproksimacija. Izlazni redoslijed = ulazni (po Id), deterministički.
/// </summary>
public sealed class ContourClassifier : IContourClassifier
{
    private const double ContainmentTessellationTolerance = 0.01;

    public IReadOnlyList<Contour> Classify(IReadOnlyList<Contour> contours)
    {
        ArgumentNullException.ThrowIfNull(contours);

        var finalById = new Dictionary<int, Contour>();
        var closedInfos = new List<ClosedInfo>();

        foreach (var contour in contours)
        {
            if (contour.Kind == ContourKind.Open || !contour.IsClosed)
            {
                finalById[contour.Id] = contour.Kind == ContourKind.Open ? contour : contour.WithKind(ContourKind.Open);
                continue;
            }

            closedInfos.Add(new ClosedInfo(
                contour,
                SignedArea(contour.Polyline),
                TessellateToPolygon(contour.Polyline),
                contour.Polyline[0].PointAt(0.5)));
        }

        // Ugnježđivanje: od najveće prema najmanjoj (roditelj je uvijek obrađen prije djeteta).
        var byAreaDesc = closedInfos
            .OrderByDescending(i => Math.Abs(i.Area))
            .ThenBy(i => i.Contour.Id)
            .ToList();

        var depths = new Dictionary<int, int>();
        for (var i = 0; i < byAreaDesc.Count; i++)
        {
            var current = byAreaDesc[i];
            var depth = 0;
            var parentArea = double.PositiveInfinity;

            for (var j = 0; j < i; j++)
            {
                var candidate = byAreaDesc[j];
                if (Math.Abs(candidate.Area) < Math.Abs(current.Area))
                {
                    continue; // roditelj mora biti veći
                }

                if (PointInPolygon(current.RepresentativePoint, candidate.Polygon)
                    && Math.Abs(candidate.Area) < parentArea)
                {
                    parentArea = Math.Abs(candidate.Area);
                    depth = depths[candidate.Contour.Id] + 1;
                }
            }

            depths[current.Contour.Id] = depth;

            var kind = depth % 2 == 0 ? ContourKind.Outer : ContourKind.Hole;
            var classified = current.Contour.WithKind(kind);

            // Normalizacija orijentacije: Outer CCW (površina > 0), Hole CW (površina < 0).
            var isCcw = current.Area > 0.0;
            var wantCcw = kind == ContourKind.Outer;
            if (isCcw != wantCcw)
            {
                classified = classified.Reversed();
            }

            finalById[classified.Id] = classified;
        }

        return contours.Select(c => finalById[c.Id]).ToList();
    }

    /// <summary>
    /// Egzaktna predznačena površina zatvorene konture: shoelace po početnim točkama
    /// segmenata + kružni odsječci ½r²(θ − sin θ) po predznačenom sweepu lukova.
    /// Pozitivna = CCW. Za konture zatvorene tolerancijom pogreška je reda gap².
    /// </summary>
    public static double SignedArea(Polyline2 polyline)
    {
        ArgumentNullException.ThrowIfNull(polyline);

        var vertices = new Point2[polyline.Count];
        for (var i = 0; i < polyline.Count; i++)
        {
            vertices[i] = polyline[i].StartPoint;
        }

        var area = MathUtil.SignedArea(vertices);

        for (var i = 0; i < polyline.Count; i++)
        {
            if (polyline[i] is ArcSeg arc)
            {
                var sweep = arc.SweepAngle;
                area += 0.5 * arc.Radius * arc.Radius * (sweep - Math.Sin(sweep));
            }
        }

        return area;
    }

    private static List<Point2> TessellateToPolygon(Polyline2 polyline)
    {
        var points = new List<Point2>();
        for (var i = 0; i < polyline.Count; i++)
        {
            switch (polyline[i])
            {
                case ArcSeg arc:
                {
                    var arcPoints = Tessellation.TessellateArc(arc, ContainmentTessellationTolerance);
                    for (var k = 0; k < arcPoints.Count - 1; k++)
                    {
                        points.Add(arcPoints[k]); // zadnja = start sljedećeg segmenta
                    }

                    break;
                }

                default:
                    points.Add(polyline[i].StartPoint);
                    break;
            }
        }

        return points;
    }

    /// <summary>Ray-casting (even-odd). Točke NA rubu su nedefinirane — pozivatelj koristi reprezentativne točke koje ne leže na rubu kandidata.</summary>
    private static bool PointInPolygon(Point2 p, List<Point2> polygon)
    {
        var inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            var a = polygon[i];
            var b = polygon[j];
            if ((a.Y > p.Y) != (b.Y > p.Y)
                && p.X < ((b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y)) + a.X)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private sealed record ClosedInfo(Contour Contour, double Area, List<Point2> Polygon, Point2 RepresentativePoint);
}
