using Inostvor.Core.Model.Geometry;
using Inostvor.Core.Model.Validation;
using Inostvor.Sdk.Validation;

namespace Inostvor.Geometry.Rules;

/// <summary>
/// Prijavljuje otvorene konture — s razmakom krajeva i uputom, tako da korisnik
/// jasno vidi ŠTO je ostalo otvoreno i ZAŠTO (razmak veći od tolerancije spajanja).
/// </summary>
public sealed class OpenContourRule : IValidationRule
{
    public string Code => "OPEN_CONTOUR";

    public IEnumerable<ValidationIssue> Evaluate(ValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var contour in context.Contours)
        {
            if (contour.Kind != ContourKind.Open)
            {
                continue;
            }

            var start = contour.Polyline.StartPoint;
            var end = contour.Polyline.EndPoint;
            var gap = start.DistanceTo(end);

            var hint = gap <= context.Settings.JoinTolerance * 10.0
                ? FormattableString.Invariant(
                    $" Razmak je blizu tolerancije spajanja ({context.Settings.JoinTolerance:0.###} mm) — provjeri geometriju ili povećaj toleranciju.")
                : string.Empty;

            yield return new ValidationIssue(
                ValidationSeverity.Warning,
                Code,
                FormattableString.Invariant(
                    $"Otvorena kontura #{contour.Id} (layer '{contour.Layer}', {contour.SegmentCount} segmenata): krajevi udaljeni {gap:0.###} mm.{hint}"),
                contour.Id,
                start.MidPointTo(end));
        }
    }
}
