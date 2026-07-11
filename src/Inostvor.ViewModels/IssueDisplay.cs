using Inostvor.Core.Model.Validation;

namespace Inostvor.ViewModels;

/// <summary>Prikazni omotač nalaza validacije za listu u UI-ju.</summary>
public sealed record IssueDisplay(ValidationIssue Issue)
{
    public string Header => Issue.ContourId >= 0
        ? FormattableString.Invariant($"{SeverityLabel} · {Issue.Code} · kontura #{Issue.ContourId}")
        : FormattableString.Invariant($"{SeverityLabel} · {Issue.Code}");

    public string Message => Issue.Message;

    private string SeverityLabel => Issue.Severity switch
    {
        ValidationSeverity.Error => "GREŠKA",
        ValidationSeverity.Warning => "UPOZORENJE",
        _ => "INFO",
    };
}
