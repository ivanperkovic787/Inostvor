using PlasmaCAM.Core.Model.Geometry;
using PlasmaCAM.Core.Model.Import;

namespace PlasmaCAM.Core.Abstractions;

/// <summary>Port detekcije kontura: uvezeni entiteti → lanci segmenata + evidencija spojeva.</summary>
public interface IContourBuilder
{
    /// <summary>
    /// Lanča segmente u konture, PO LAYERU (segmenti različitih layera se ne miješaju).
    /// Deterministički: isti ulaz daje identičan izlaz, uključujući Id-jeve i redoslijed.
    /// </summary>
    ContourBuildResult Build(IReadOnlyList<ImportedEntity> entities, ContourBuildSettings settings);
}
