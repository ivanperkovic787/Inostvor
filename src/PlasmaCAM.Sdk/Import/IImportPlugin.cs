using PlasmaCAM.Core.Abstractions;

namespace PlasmaCAM.Sdk.Import;

/// <summary>
/// Plugin točka za importere geometrije. Ugrađeni NetDxfImporter registrira se kroz
/// OVAJ kontrakt, identično kao budući vanjski plugini (Baseline v1.1, §4.5) —
/// plugin API je time testiran vlastitim kodom od prvog dana.
/// </summary>
public interface IImportPlugin : IPlugin
{
    /// <summary>Podržane ekstenzije s točkom, malim slovima, npr. [".dxf"].</summary>
    IReadOnlyList<string> FileExtensions { get; }

    IDxfImporter CreateImporter();
}
