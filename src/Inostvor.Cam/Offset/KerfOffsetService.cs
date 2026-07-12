using Inostvor.Core.Abstractions;
using Inostvor.Core.Model.Geometry;
using Inostvor.Kernel;
using Inostvor.Kernel.Primitives;

namespace Inostvor.Cam.Offset;

/// <summary>
/// Kerf offset kroz Clipper2 (µm, ADR-001): tessellacija konture → inflate →
/// stabilna normalizacija rezultata.
///
/// Strana offseta iz vrste konture: Outer (CCW) → +kerf/2 (van), Hole → −kerf/2
/// prema materijalu (putanja UNUTAR rupe). Smjer obilaska rezultata vraća se na
/// konvenciju klasifikatora (Outer CCW, Hole CW).
///
/// DETERMINIZAM (eksplicitni zahtjev): Clipper je determinističan za isti ulaz,
/// ali redoslijed prstenova i početna točka NISU ugovorna obveza biblioteke.
/// Zato se svaki prsten rotira tako da počinje leksikografski najmanjom točkom
/// (X pa Y), a prstenovi se sortiraju po (MinX, MinY, broj točaka) — isti ulaz
/// daje bajt-identičan izlaz neovisno o verziji Clippera.
///
/// Otvorene konture NE dobivaju kerf kompenzaciju u V1 (režu se po središnjici) —
/// vraća se tessellirana središnjica; odluka dokumentirana u M5 izvještaju.
/// </summary>
public sealed class KerfOffsetService : IKerfOffsetService
{
    public IReadOnlyList<IReadOnlyList<Point2>> Offset(Contour contour, double kerfWidth, double tessellationTolerance)
    {
        ArgumentNullException.ThrowIfNull(contour);
        ArgumentOutOfRangeException.ThrowIfNegative(kerfWidth);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(tessellationTolerance, 0.0);

        var points = Tessellate(contour.Polyline, tessellationTolerance);

        if (contour.Kind == ContourKind.Open)
        {
            return [points]; // središnjica, bez kerfa (V1)
        }

        // Zatvoreni prsten: bez duplicirane zadnje točke.
        if (points.Count > 1 && points[0].AlmostEquals(points[^1], tessellationTolerance))
        {
            points.RemoveAt(points.Count - 1);
        }

        var isHole = contour.Kind == ContourKind.Hole;

        // Clipper ulaz uvijek CCW; klasifikator drži rupe CW → obrni za Clipper.
        if (isHole)
        {
            points.Reverse();
        }

        var delta = isHole ? -kerfWidth / 2.0 : kerfWidth / 2.0;

        List<List<Point2>> rings;
        if (kerfWidth <= Tolerance.Geometric)
        {
            rings = [points]; // kerf 0: putanja = geometrija (bez Clippera)
        }
        else
        {
            rings = ClipperAdapter.InflateClosed(points, delta);
        }

        // Smjer obilaska natrag na konvenciju rezanja (Hole → CW).
        if (isHole)
        {
            foreach (var ring in rings)
            {
                ring.Reverse();
            }
        }

        return Normalize(rings);
    }

    private static List<Point2> Tessellate(Polyline2 polyline, double tolerance)
    {
        var points = new List<Point2>();
        for (var i = 0; i < polyline.Count; i++)
        {
            switch (polyline[i])
            {
                case ArcSeg arc:
                {
                    var arcPoints = Tessellation.TessellateArc(arc, tolerance);
                    for (var k = 0; k < arcPoints.Count - 1; k++)
                    {
                        points.Add(arcPoints[k]);
                    }

                    break;
                }

                default:
                    points.Add(polyline[i].StartPoint);
                    break;
            }
        }

        points.Add(polyline[^1].EndPoint);
        return points;
    }

    /// <summary>Stabilna normalizacija: rotacija prstena na leksikografski minimum + sortiranje prstenova.</summary>
    private static List<IReadOnlyList<Point2>> Normalize(List<List<Point2>> rings)
    {
        var normalized = new List<IReadOnlyList<Point2>>(rings.Count);
        foreach (var ring in rings)
        {
            var minIndex = 0;
            for (var i = 1; i < ring.Count; i++)
            {
                if (ring[i].X < ring[minIndex].X
                    || (ring[i].X == ring[minIndex].X && ring[i].Y < ring[minIndex].Y))
                {
                    minIndex = i;
                }
            }

            var rotated = new Point2[ring.Count];
            for (var i = 0; i < ring.Count; i++)
            {
                rotated[i] = ring[(minIndex + i) % ring.Count];
            }

            normalized.Add(rotated);
        }

        normalized.Sort((a, b) =>
        {
            var byX = a[0].X.CompareTo(b[0].X);
            if (byX != 0)
            {
                return byX;
            }

            var byY = a[0].Y.CompareTo(b[0].Y);
            return byY != 0 ? byY : a.Count.CompareTo(b.Count);
        });

        return normalized;
    }
}
