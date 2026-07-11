using PlasmaCAM.Core.Model.Validation;

namespace PlasmaCAM.Sdk.Validation;

/// <summary>
/// Jedno pravilo validacije geometrije. Ugrađena pravila (M3) i buduća plugin
/// pravila implementiraju ISTI kontrakt (Baseline v1.1, §4.5). Pravilo NE
/// jamči redoslijed vlastitih nalaza — ValidationReport ih stabilno sortira.
/// </summary>
public interface IValidationRule
{
    /// <summary>Stabilan kod pravila (prefiks kodova nalaza), npr. "OPEN_CONTOUR".</summary>
    string Code { get; }

    IEnumerable<ValidationIssue> Evaluate(ValidationContext context);
}
