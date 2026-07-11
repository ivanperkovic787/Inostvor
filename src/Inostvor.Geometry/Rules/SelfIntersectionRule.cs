using Inostvor.Core.Model.Validation;
using Inostvor.Kernel.Intersections;
using Inostvor.Sdk.Validation;

namespace Inostvor.Geometry.Rules;

/// <summary>
/// Samopresijecanje konture je greška: kerf offset (M5) nad samopresijecajućom
/// konturom daje nepredvidivu geometriju reza.
/// </summary>
public sealed class SelfIntersectionRule : IValidationRule
{
    public string Code => "SELF_INTERSECTION";

    public IEnumerable<ValidationIssue> Evaluate(ValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var contour in context.Contours)
        {
            foreach (var hit in PolylineSelfIntersection.Find(contour.Polyline))
            {
                yield return new ValidationIssue(
                    ValidationSeverity.Error,
                    Code,
                    FormattableString.Invariant(
                        $"Kontura #{contour.Id} (layer '{contour.Layer}') siječe samu sebe na ({hit.Point.X:0.###}, {hit.Point.Y:0.###}) — segmenti {hit.SegmentA} i {hit.SegmentB}."),
                    contour.Id,
                    hit.Point);
            }
        }
    }
}
