using PlasmaCAM.Core.Model.Geometry;
using PlasmaCAM.Core.Model.Import;
using PlasmaCAM.Core.Model.Validation;

namespace PlasmaCAM.Core.Abstractions;

/// <summary>Rezultat cijelog geometrijskog cjevovoda: konture + spojevi + validacija.</summary>
public sealed record GeometryPipelineResult(
    IReadOnlyList<Contour> Contours,
    IReadOnlyList<ContourJoin> Joins,
    ValidationReport Report);

/// <summary>Cjevovod: import entiteti → detekcija kontura → klasifikacija → validacija.</summary>
public interface IGeometryPipeline
{
    GeometryPipelineResult Process(IReadOnlyList<ImportedEntity> entities, ContourBuildSettings settings);
}
