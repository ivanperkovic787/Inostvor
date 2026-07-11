namespace PlasmaCAM.Core.Model.Geometry;

/// <summary>Rezultat detekcije kontura: lanci + potpuna evidencija automatskih spojeva.</summary>
public sealed record ContourBuildResult(
    IReadOnlyList<Contour> Contours,
    IReadOnlyList<ContourJoin> Joins);
