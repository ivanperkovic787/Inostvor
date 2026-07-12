namespace Inostvor.Core.Abstractions;

/// <summary>Spremanje tekstualne datoteke kroz sistemski dijalog.</summary>
public interface IFileSaveService
{
    /// <summary>Vraća putanju spremljene datoteke ili null ako je korisnik odustao.</summary>
    Task<string?> SaveTextAsync(string suggestedFileName, string extension, string content);
}
