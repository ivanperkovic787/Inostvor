using PlasmaCAM.Core.Model.Validation;

namespace PlasmaCAM.Core.Abstractions;

/// <summary>Port validacije: izvršava sva registrirana pravila i vraća deterministički izvještaj.</summary>
public interface IToolpathValidator
{
    ValidationReport Validate(ValidationContext context);
}
