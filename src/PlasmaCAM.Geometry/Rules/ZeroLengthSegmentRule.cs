using PlasmaCAM.Core.Model.Validation;
using PlasmaCAM.Sdk.Validation;

namespace PlasmaCAM.Geometry.Rules;

/// <summary>
/// Segmenti kraći od praktičnog minimuma za rezanje. Kernel odbija segmente ispod
/// geometrijske tolerancije (1e-6 mm) već u konstruktoru; ovo pravilo hvata one
/// koji su geometrijski validni, ali praktično nerezljivi (obrana u dubinu —
/// konture u budućnosti neće dolaziti samo iz importa).
/// </summary>
public sealed class ZeroLengthSegmentRule : IValidationRule
{
    private readonly double _minimumLength;

    /// <param name="minimumLength">Prag ispod kojeg se segment prijavljuje. [mm]</param>
    public ZeroLengthSegmentRule(double minimumLength = 0.01)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(minimumLength, 0.0);
        _minimumLength = minimumLength;
    }

    public string Code => "ZERO_LENGTH_SEGMENT";

    public IEnumerable<ValidationIssue> Evaluate(ValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var contour in context.Contours)
        {
            for (var i = 0; i < contour.Polyline.Count; i++)
            {
                var segment = contour.Polyline[i];
                if (segment.Length >= _minimumLength)
                {
                    continue;
                }

                yield return new ValidationIssue(
                    ValidationSeverity.Warning,
                    Code,
                    FormattableString.Invariant(
                        $"Segment {i} konture #{contour.Id} dug je samo {segment.Length:0.####} mm (prag {_minimumLength:0.###} mm) — vjerojatno artefakt crteža."),
                    contour.Id,
                    segment.StartPoint.MidPointTo(segment.EndPoint));
            }
        }
    }
}
