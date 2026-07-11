using PlasmaCAM.Core.Model.Validation;
using PlasmaCAM.Kernel;
using PlasmaCAM.Kernel.Primitives;
using PlasmaCAM.Sdk.Validation;

namespace PlasmaCAM.Geometry.Rules;

/// <summary>
/// Duplicirana geometrija (isti segment dva puta, i u suprotnom smjeru) — čest CAD
/// artefakt (copy-paste, explode). Plazma bi rezala isto mjesto dvaput.
/// Detekcija: kvantizirani ključ (10× geometrijska tolerancija) + egzaktna potvrda.
/// Kvantizacija na granici ćelije može propustiti NE-egzaktni duplikat — prihvatljivo,
/// CAD duplikati su u praksi egzaktne kopije (dokumentirana aproksimacija).
/// </summary>
public sealed class DuplicateGeometryRule : IValidationRule
{
    private const double Quantum = Tolerance.Geometric * 10.0;

    public string Code => "DUPLICATE_GEOMETRY";

    public IEnumerable<ValidationIssue> Evaluate(ValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Deterministički popis: konture po Id (ulazni redoslijed), segmenti po indeksu.
        var all = new List<(string Key, int ContourId, int SegmentIndex, ISegment Segment)>();
        foreach (var contour in context.Contours)
        {
            for (var i = 0; i < contour.Polyline.Count; i++)
            {
                var segment = contour.Polyline[i];
                all.Add((BuildKey(segment), contour.Id, i, segment));
            }
        }

        all.Sort((a, b) =>
        {
            var byKey = string.CompareOrdinal(a.Key, b.Key);
            if (byKey != 0)
            {
                return byKey;
            }

            var byContour = a.ContourId.CompareTo(b.ContourId);
            return byContour != 0 ? byContour : a.SegmentIndex.CompareTo(b.SegmentIndex);
        });

        for (var i = 1; i < all.Count; i++)
        {
            if (all[i].Key != all[i - 1].Key || !AreGeometricallyEqual(all[i - 1].Segment, all[i].Segment))
            {
                continue;
            }

            var first = all[i - 1];
            var duplicate = all[i];
            yield return new ValidationIssue(
                ValidationSeverity.Warning,
                Code,
                FormattableString.Invariant(
                    $"Duplicirana geometrija: segment {duplicate.SegmentIndex} konture #{duplicate.ContourId} preklapa segment {first.SegmentIndex} konture #{first.ContourId}."),
                duplicate.ContourId,
                duplicate.Segment.StartPoint.MidPointTo(duplicate.Segment.EndPoint));
        }
    }

    /// <summary>Ključ neovisan o smjeru: krajevi leksikografski sortirani, kvantizirani.</summary>
    private static string BuildKey(ISegment segment)
    {
        var a = segment.StartPoint;
        var b = segment.EndPoint;
        if (a.X > b.X || (a.X == b.X && a.Y > b.Y))
        {
            (a, b) = (b, a);
        }

        return segment switch
        {
            ArcSeg arc => FormattableString.Invariant(
                $"A:{Q(a.X)}:{Q(a.Y)}:{Q(b.X)}:{Q(b.Y)}:{Q(arc.Center.X)}:{Q(arc.Center.Y)}:{Q(arc.Radius)}"),
            _ => FormattableString.Invariant($"L:{Q(a.X)}:{Q(a.Y)}:{Q(b.X)}:{Q(b.Y)}"),
        };

        static long Q(double v) => (long)Math.Round(v / Quantum);
    }

    private static bool AreGeometricallyEqual(ISegment x, ISegment y)
    {
        var sameDirection = x.StartPoint.AlmostEquals(y.StartPoint) && x.EndPoint.AlmostEquals(y.EndPoint);
        var reversed = x.StartPoint.AlmostEquals(y.EndPoint) && x.EndPoint.AlmostEquals(y.StartPoint);
        if (!sameDirection && !reversed)
        {
            return false;
        }

        return (x, y) switch
        {
            (ArcSeg ax, ArcSeg ay) => ax.Center.AlmostEquals(ay.Center)
                && Tolerance.AreEqual(ax.Radius, ay.Radius)
                && Tolerance.AreEqual(Math.Abs(ax.SweepAngle), Math.Abs(ay.SweepAngle), Tolerance.Angular + (Tolerance.Geometric / ax.Radius)),
            (LineSeg, LineSeg) => true,
            _ => false,
        };
    }
}
