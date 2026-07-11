namespace PlasmaCAM.Core.Abstractions;

/// <summary>Apstrakcija sistemskog dijaloga za odabir datoteke (testabilnost ViewModela).</summary>
public interface IFilePickerService
{
    /// <summary>Otvara dijalog; vraća punu putanju ili null ako je korisnik odustao.</summary>
    /// <param name="extensions">Ekstenzije s točkom, npr. [".dxf"].</param>
    Task<string?> PickOpenFileAsync(IReadOnlyList<string> extensions);
}
