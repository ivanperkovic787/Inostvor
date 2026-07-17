namespace Inostvor.Core.Abstractions;

/// <summary>
/// Hash sadržaja datoteke (ulaz u ključ cachea, ADR-006). Seam prema datotečnom
/// sustavu — ViewModeli ne smiju direktno čitati disk (testabilnost, isti razlog
/// kao IFilePickerService/IFileSaveService).
/// </summary>
public interface IFileHashService
{
    /// <summary>SHA-256 sadržaja datoteke (hex, mala slova).</summary>
    string HashFile(string path);
}
