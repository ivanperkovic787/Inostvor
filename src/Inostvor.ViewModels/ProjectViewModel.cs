using CommunityToolkit.Mvvm.ComponentModel;
using Inostvor.Core.Abstractions;
using Inostvor.Core.Model.Machines;
using Inostvor.Core.Model.Project;
using Inostvor.Core.Model.Toolpath;

namespace Inostvor.ViewModels;

/// <summary>
/// Stanje otvorenog projekta: put do datoteke, "prljavost", sastavljanje
/// ProjectDocumenta i autosave. Projekt je JEDINA istina korisnikova rada
/// (ADR-005) — SQLite drži samo biblioteke i postavke.
/// </summary>
public sealed partial class ProjectViewModel : ObservableObject
{
    private readonly IProjectStore _store;
    private readonly IAutoSaveService _autoSave;
    private readonly IFileHashService _fileHash;

    public ProjectViewModel(IProjectStore store, IAutoSaveService autoSave, IFileHashService fileHash)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(autoSave);
        ArgumentNullException.ThrowIfNull(fileHash);
        _store = store;
        _autoSave = autoSave;
        _fileHash = fileHash;
    }

    [ObservableProperty]
    private string _name = "Bez naziva";

    [ObservableProperty]
    private string? _filePath;

    [ObservableProperty]
    private bool _isDirty;

    /// <summary>DXF izvori uvezeni u projekt (putanje na disku).</summary>
    public List<ProjectDxfSource> DxfSources { get; } = [];

    /// <summary>Nepoznate sekcije budućih modula — čuvaju se pri spremanju.</summary>
    public IReadOnlyDictionary<string, System.Text.Json.JsonElement> Extensions { get; private set; } =
        new Dictionary<string, System.Text.Json.JsonElement>(StringComparer.Ordinal);

    public bool RecoveryAvailable => _autoSave.RecoveryAvailable;

    public string AutoSavePath => _autoSave.AutoSavePath;

    /// <summary>Stabilan Id projekta (ADR-006) — zadržava se kroz spremanja i preimenovanja.</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    public ProjectDocument BuildDocument(TechnologySettings technology, MachineProfile machine, double? simulationTime, double simulationSpeed)
        => new()
        {
            Id = Id,
            Name = Name,
            DxfSources = DxfSources.ToList(),
            Technology = technology,
            Machine = machine,
            SimulationTimeSeconds = simulationTime,
            SimulationSpeed = simulationSpeed,
            Extensions = Extensions,
        };

    public void ApplyLoaded(LoadedProject loaded, string? path)
    {
        ArgumentNullException.ThrowIfNull(loaded);
        Id = loaded.Document.Id;
        Name = loaded.Document.Name;
        FilePath = path;
        DxfSources.Clear();
        DxfSources.AddRange(loaded.Document.DxfSources);
        Extensions = loaded.Document.Extensions;
        IsDirty = false;
    }

    /// <summary>Dodaje DXF izvor i odmah računa hash sadržaja (ulaz u ključ cachea).</summary>
    public void AddDxf(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        DxfSources.Add(ProjectDxfSource.Create(
            Path.GetFileName(path), path, _fileHash.HashFile(path)));
        IsDirty = true;
    }

    public Task SaveAsync(ProjectDocument document, string path, ToolpathCache? cache)
        => SaveInternalAsync(document, path, cache, isAutoSave: false);

    public Task AutoSaveAsync(ProjectDocument document, ToolpathCache? cache)
        => SaveInternalAsync(document, _autoSave.AutoSavePath, cache, isAutoSave: true);

    public Task<LoadedProject> LoadAsync(string path) => _store.LoadAsync(path);

    public void MarkCleanExit() => _autoSave.MarkCleanExit();

    public void ClearAutoSave() => _autoSave.ClearAutoSave();

    private async Task SaveInternalAsync(ProjectDocument document, string path, ToolpathCache? cache, bool isAutoSave)
    {
        await _store.SaveAsync(document, path, cache).ConfigureAwait(true);
        if (isAutoSave)
        {
            return;
        }

        FilePath = path;
        Name = document.Name;
        IsDirty = false;
    }
}
