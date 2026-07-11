using Inostvor.Kernel.Primitives;

namespace Inostvor.Core.Model.Validation;

public enum ValidationSeverity
{
    /// <summary>Blokira generiranje putanje.</summary>
    Error = 0,

    /// <summary>Rezanje moguće, ali rezultat vjerojatno nije namjeravan.</summary>
    Warning = 1,

    /// <summary>Informacija (npr. automatski premošten razmak).</summary>
    Info = 2,
}

/// <summary>Jedan nalaz validacije. Nepromjenjiv, sa stabilnim kodom za testove.</summary>
/// <param name="Severity">Težina nalaza.</param>
/// <param name="Code">Stabilan kod pravila, npr. "OPEN_CONTOUR".</param>
/// <param name="Message">Poruka korisniku (hrvatski, s konkretnim brojkama).</param>
/// <param name="ContourId">Kontura na koju se nalaz odnosi; -1 ako nije vezan uz konturu.</param>
/// <param name="Location">Reprezentativna točka nalaza (za zoom-to u M4); null ako nema smisla.</param>
public sealed record ValidationIssue(
    ValidationSeverity Severity,
    string Code,
    string Message,
    int ContourId = -1,
    Point2? Location = null);
