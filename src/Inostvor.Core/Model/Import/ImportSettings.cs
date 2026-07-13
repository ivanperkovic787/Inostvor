namespace Inostvor.Core.Model.Import;

/// <summary>Postavke importa — vrijede za SVE importere (dio parser-neutralnog kontrakta).</summary>
public sealed record ImportSettings
{
    /// <summary>Maksimalno odstupanje tetive pri tessellaciji splineova/elipsa i lukova pod neuniformnom skalom. [mm]</summary>
    public double TessellationTolerance { get; init; } = 0.01;

    /// <summary>Maksimalna dubina ugniježđenih INSERT-a (zaštita od patoloških datoteka).</summary>
    public int MaxInsertDepth { get; init; } = 32;

    /// <summary>Gornja granica broja upozorenja (datoteka s 50 000 degeneriranih entiteta ne smije zatrpati memoriju).</summary>
    public int MaxWarnings { get; init; } = 200;

    /// <summary>
    /// Tvrdi vremenski limit uvoza JEDNE datoteke. [s]
    /// Oštećena DXF datoteka (npr. odrezana, bez EOF markera) može natjerati parser
    /// u beskonačno čekanje — aplikacija se u tom slučaju NE SMIJE zamrznuti.
    /// 0 = bez ograničenja (samo za testove/benchmarke).
    /// </summary>
    public int TimeoutSeconds { get; init; } = 30;

    public static ImportSettings Default { get; } = new();
}
