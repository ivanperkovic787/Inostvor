using PlasmaCAM.Core.Model.Geometry;

namespace PlasmaCAM.Core.Model.Validation;

/// <summary>Sve što pravilo validacije smije vidjeti — konture i evidencija spojeva.</summary>
/// <param name="Contours">Klasificirane konture (deterministični Id-jevi).</param>
/// <param name="Joins">Automatski premošteni razmaci iz detekcije kontura.</param>
/// <param name="Settings">Postavke s kojima su konture građene.</param>
public sealed record ValidationContext(
    IReadOnlyList<Contour> Contours,
    IReadOnlyList<ContourJoin> Joins,
    ContourBuildSettings Settings);
