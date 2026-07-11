using Inostvor.Core.Model.Validation;

namespace Inostvor.Core.Abstractions;

/// <summary>Port validacije: izvršava sva registrirana pravila i vraća deterministički izvještaj.</summary>
public interface IToolpathValidator
{
    ValidationReport Validate(ValidationContext context);
}
