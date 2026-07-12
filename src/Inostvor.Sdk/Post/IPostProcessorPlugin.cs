using Inostvor.Core.Model.Machines;
using Inostvor.Core.Model.Toolpath;

namespace Inostvor.Sdk.Post;

/// <summary>Rezultat postprocesiranja: tekst G-koda + metapodaci.</summary>
public sealed record PostResult(string GCode, string FileExtension, IReadOnlyList<string> Warnings);

/// <summary>
/// Postprocesor: FORMATIRA gotov ToolpathProgram u G-kod. STROGA GRANICA
/// ODGOVORNOSTI (zahtjev vlasnika projekta): smije formatirati, emitirati
/// naredbe, dodati zaglavlje/završetak i kontrolerske sekvence — NIKAD ne
/// smije mijenjati redoslijed rezanja, leadove, kerf ili geometriju. Program
/// je 100% gotov prije ulaska ovamo.
/// </summary>
public interface IPostProcessor
{
    PostResult Generate(ToolpathProgram program);
}

/// <summary>
/// Plugin postprocesora (ADR-004): Mach3, EC300 i SVI budući (Mach4, LinuxCNC,
/// GRBL, EdingCNC, Fanuc, Haas, Siemens, ESS, Masso, PlanetCNC…) implementiraju
/// ISTI kontrakt, bez privilegija. Dodavanje kontrolera = nova registracija,
/// nula izmjena jezgre.
/// </summary>
public interface IPostProcessorPlugin
{
    /// <summary>Stabilan id, npr. "inostvor.post.mach3".</summary>
    string Id { get; }

    string DisplayName { get; }

    /// <summary>Zadani dijalekt — polazna točka koju profil/korisnik (i budući editor) smije prilagoditi.</summary>
    GCodeDialect DefaultDialect { get; }

    /// <summary>Stvori postprocesor za KONKRETAN stroj (dijalekt + profil su odvojeni pojmovi).</summary>
    IPostProcessor Create(GCodeDialect dialect, MachineProfile profile);
}

/// <summary>Katalog registriranih postprocesora (DI); rezolucija po id-u.</summary>
public interface IPostProcessorCatalog
{
    IReadOnlyList<IPostProcessorPlugin> Plugins { get; }

    IPostProcessorPlugin? Find(string id);
}
