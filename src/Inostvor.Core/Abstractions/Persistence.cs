using Inostvor.Core.Model.Library;
using Inostvor.Core.Model.Machines;
using Inostvor.Core.Model.Project;

namespace Inostvor.Core.Abstractions;

/// <summary>Spremište projekta — .ino kontejner na disku (ADR-005: projekt je DATOTEKA).</summary>
public interface IProjectStore
{
    /// <summary>Trenutna verzija formata koju pisač proizvodi.</summary>
    int CurrentFormatVersion { get; }

    Task SaveAsync(ProjectDocument document, string path);

    /// <summary>Učitava projekt; stariji formati se migriraju kroz lanac migracija.</summary>
    Task<LoadedProject> LoadAsync(string path);
}

/// <summary>Biblioteka profila strojeva (SQLite, aplikacijska razina).</summary>
public interface IMachineProfileRepository
{
    IReadOnlyList<MachineProfile> GetAll();

    void Save(MachineProfile profile);

    void Delete(string name);
}

/// <summary>Biblioteka tehnologija (SQLite, aplikacijska razina).</summary>
public interface ITechnologyRepository
{
    IReadOnlyList<TechnologyEntry> GetAll();

    void Save(TechnologyEntry entry);

    void Delete(Guid id);
}

/// <summary>Key-value postavke aplikacije (SQLite).</summary>
public interface ISettingsRepository
{
    string? Get(string key);

    void Set(string key, string value);
}

/// <summary>Prijenos postavki među računalima: profili + tehnologije + postavke u jednu JSON datoteku.</summary>
public interface ISettingsPortService
{
    string ExportToJson();

    /// <summary>Uvozi bundle; postojeći zapisi istog identiteta se zamjenjuju. Vraća sažetak.</summary>
    string ImportFromJson(string json);
}

/// <summary>Automatsko spremanje + oporavak nakon pada.</summary>
public interface IAutoSaveService
{
    string AutoSavePath { get; }

    /// <summary>True ako prošla sesija NIJE čisto završila i autosave postoji.</summary>
    bool RecoveryAvailable { get; }

    void MarkSessionStart();

    void MarkCleanExit();

    void ClearAutoSave();
}
