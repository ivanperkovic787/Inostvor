namespace Inostvor.Core.Abstractions;

/// <summary>Spremanje datoteka kroz sistemski dijalog.</summary>
public interface IFileSaveService
{
    /// <summary>Zapisuje tekst u odabranu datoteku; null ako je korisnik odustao.</summary>
    Task<string?> SaveTextAsync(string suggestedFileName, string extension, string content);

    /// <summary>Samo odabir putanje (sadržaj zapisuje pozivatelj — npr. binarni .ino kontejner).</summary>
    Task<string?> PickSavePathAsync(string suggestedFileName, string extension);
}
