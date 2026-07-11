using Inostvor.Core.Abstractions;
using Inostvor.Core.Model.Validation;
using Inostvor.Sdk.Validation;

namespace Inostvor.Geometry.Validation;

/// <summary>
/// Orkestrator validacije: izvršava pravila i agregira nalaze. Determinizam NE ovisi
/// o redoslijedu registracije pravila — ValidationReport stabilno sortira sve nalaze.
/// </summary>
public sealed class ToolpathValidator : IToolpathValidator
{
    private readonly IReadOnlyList<IValidationRule> _rules;

    public ToolpathValidator(IEnumerable<IValidationRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _rules = rules.ToList();
    }

    public ValidationReport Validate(ValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var issues = new List<ValidationIssue>();
        foreach (var rule in _rules)
        {
            issues.AddRange(rule.Evaluate(context));
        }

        return new ValidationReport(issues);
    }
}
