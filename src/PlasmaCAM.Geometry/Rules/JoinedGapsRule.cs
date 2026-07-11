using PlasmaCAM.Core.Model.Validation;
using PlasmaCAM.Sdk.Validation;

namespace PlasmaCAM.Geometry.Rules;

/// <summary>
/// Prijavljuje SVAKI automatski premošteni razmak iz detekcije kontura — korisnik
/// mora vidjeti točno što je spojeno, gdje i koliki je razmak bio.
/// </summary>
public sealed class JoinedGapsRule : IValidationRule
{
    public string Code => "AUTO_JOINED_GAP";

    public IEnumerable<ValidationIssue> Evaluate(ValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var join in context.Joins)
        {
            var closing = join.IsClosingJoin ? " (spoj koji je zatvorio konturu)" : string.Empty;
            yield return new ValidationIssue(
                ValidationSeverity.Info,
                Code,
                FormattableString.Invariant(
                    $"Automatski premošten razmak od {join.GapSize:0.###} mm u konturi #{join.ContourId} na ({join.Location.X:0.###}, {join.Location.Y:0.###}){closing}."),
                join.ContourId,
                join.Location);
        }
    }
}
