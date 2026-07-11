using PlasmaCAM.Core.Model.Import;

namespace PlasmaCAM.Core.Abstractions;

/// <summary>
/// Port za import CAD geometrije. Implementacije su zamjenjive (NetDxfImporter danas,
/// AcadSharpImporter ili drugi sutra) bez ijedne izmjene u ostatku sustava —
/// jedini zajednički jezik je parser-neutralni <see cref="ImportResult"/>.
/// Implementacije NE bacaju iznimke za probleme s datotekom: vraćaju Fail rezultat.
/// </summary>
public interface IDxfImporter
{
    /// <summary>Opisno ime implementacije (za log i dijagnostiku).</summary>
    string Name { get; }

    ImportResult Import(string filePath);
}
