namespace Inostvor.Core.Model.Validation;

/// <summary>
/// Rezultat validacije. DETERMINISTIČAN: za isti ulaz redoslijed nalaza je uvijek
/// identičan — nalazi su stabilno sortirani po (Severity, Code, ContourId, X, Y, Message),
/// neovisno o redoslijedu registracije pravila.
/// </summary>
public sealed class ValidationReport
{
    public ValidationReport(IEnumerable<ValidationIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        Issues = issues
            .OrderBy(i => (int)i.Severity)
            .ThenBy(i => i.Code, StringComparer.Ordinal)
            .ThenBy(i => i.ContourId)
            .ThenBy(i => i.Location?.X ?? double.NegativeInfinity)
            .ThenBy(i => i.Location?.Y ?? double.NegativeInfinity)
            .ThenBy(i => i.Message, StringComparer.Ordinal)
            .ToList();
    }

    public IReadOnlyList<ValidationIssue> Issues { get; }

    public bool HasErrors => Issues.Any(i => i.Severity == ValidationSeverity.Error);

    public int ErrorCount => Issues.Count(i => i.Severity == ValidationSeverity.Error);

    public int WarningCount => Issues.Count(i => i.Severity == ValidationSeverity.Warning);

    public int InfoCount => Issues.Count(i => i.Severity == ValidationSeverity.Info);
}
