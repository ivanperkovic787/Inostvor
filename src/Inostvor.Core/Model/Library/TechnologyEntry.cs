using Inostvor.Core.Model.Toolpath;

namespace Inostvor.Core.Model.Library;

/// <summary>
/// Jedna tehnologija u biblioteci (npr. "Steel 2 mm"): materijal + parametri
/// rezanja, primjenjiva na više projekata. Extra vreća prima parametre koje
/// eksplicitna polja ne pokrivaju (proizvođačke tablice i sl.).
/// </summary>
public sealed record TechnologyEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Name { get; init; }

    public string Material { get; init; } = "";

    /// <summary>Debljina materijala. [mm]</summary>
    public double ThicknessMm { get; init; }

    /// <summary>Plin (Air, O2, N2…).</summary>
    public string Gas { get; init; } = "Air";

    public double Amperage { get; init; }

    /// <summary>Parametri rezanja (kerf, feed, pierce delay, visine…).</summary>
    public TechnologySettings Settings { get; init; } = TechnologySettings.Default;

    public IReadOnlyDictionary<string, string> Extra { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);
}
