namespace PlasmaCAM.Core.Model.Import;

/// <summary>
/// Rezultat importa (Result pattern — bez exception-driven flowa prema pozivatelju).
/// Sva geometrija je već skalirana u MILIMETRE i transformirana u world koordinate.
/// </summary>
public sealed class ImportResult
{
    private ImportResult(
        bool success,
        string? error,
        IReadOnlyList<ImportedEntity> entities,
        IReadOnlyList<ImportWarning> warnings,
        string sourceUnits,
        double unitScaleToMm,
        IReadOnlyList<string> layers)
    {
        Success = success;
        Error = error;
        Entities = entities;
        Warnings = warnings;
        SourceUnits = sourceUnits;
        UnitScaleToMm = unitScaleToMm;
        Layers = layers;
    }

    public bool Success { get; }

    /// <summary>Opis greške kad <see cref="Success"/> = false; inače null.</summary>
    public string? Error { get; }

    public IReadOnlyList<ImportedEntity> Entities { get; }

    public IReadOnlyList<ImportWarning> Warnings { get; }

    /// <summary>Naziv izvornih jedinica iz datoteke (npr. "Millimeters", "Inches", "Unitless").</summary>
    public string SourceUnits { get; }

    /// <summary>Faktor kojim su izvorne koordinate pomnožene da postanu milimetri.</summary>
    public double UnitScaleToMm { get; }

    /// <summary>Svi layeri koji se pojavljuju među uvezenim entitetima, sortirani.</summary>
    public IReadOnlyList<string> Layers { get; }

    public int TotalSegmentCount
    {
        get
        {
            var n = 0;
            foreach (var e in Entities)
            {
                n += e.Segments.Count;
            }

            return n;
        }
    }

    public static ImportResult Ok(
        IReadOnlyList<ImportedEntity> entities,
        IReadOnlyList<ImportWarning> warnings,
        string sourceUnits,
        double unitScaleToMm,
        IReadOnlyList<string> layers)
        => new(true, null, entities, warnings, sourceUnits, unitScaleToMm, layers);

    public static ImportResult Fail(string error)
        => new(false, error, [], [], "Unknown", 1.0, []);
}
