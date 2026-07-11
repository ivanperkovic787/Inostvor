using PlasmaCAM.Kernel;

namespace PlasmaCAM.Core.Model.Geometry;

/// <summary>Postavke detekcije kontura.</summary>
public sealed record ContourBuildSettings
{
    /// <summary>
    /// Maksimalni razmak krajnjih točaka koji se automatski premošćuje. [mm]
    /// Svako premoštenje se bilježi u <see cref="ContourJoin"/>.
    /// </summary>
    public double JoinTolerance { get; init; } = Tolerance.DefaultContourJoin;

    public static ContourBuildSettings Default { get; } = new();
}
