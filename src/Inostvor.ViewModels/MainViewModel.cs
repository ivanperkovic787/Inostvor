using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Inostvor.Core.Abstractions;
using Inostvor.Core.Model.Geometry;
using Inostvor.Core.Model.Import;
using Inostvor.Core.Model.Toolpath;
using Inostvor.Core.Model.Machines;
using Inostvor.Core.Model.Project;
using Inostvor.Core.Model.Validation;
using Inostvor.Post;
using Inostvor.Rendering.Scene;
using Inostvor.Sdk.Post;

namespace Inostvor.ViewModels;

/// <summary>
/// Shell ViewModel: status traka, Undo/Redo i otvaranje DXF datoteka (M2).
/// Rezultat importa drži se u <see cref="LastImport"/> — Canvas (M4) i
/// Project Explorer (M3) vezat će se na njega.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly IUndoService _undoService;
    private readonly IDxfImporter _importer;
    private readonly IFilePickerService _filePicker;
    private readonly IGeometryPipeline _pipeline;
    private readonly IToolpathGenerator _toolpathGenerator;
    private readonly IPostProcessorCatalog _postCatalog;
    private readonly IFileSaveService _fileSave;
    private readonly ProjectViewModel _project;
    private readonly ILogger<MainViewModel> _logger;

    [ObservableProperty]
    private string _statusText = "Spremno";

    [ObservableProperty]
    private ImportResult? _lastImport;

    [ObservableProperty]
    private GeometryPipelineResult? _lastPipeline;

    [ObservableProperty]
    private ToolpathProgram? _lastToolpath;

    [ObservableProperty]
    private IssueDisplay? _selectedIssue;

    [ObservableProperty]
    private IReadOnlyList<IssueDisplay> _issues = [];

    public ViewportViewModel Viewport { get; } = new();

    public SimulationViewModel Simulation { get; } = new();

    partial void OnLastToolpathChanged(ToolpathProgram? value)
    {
        Simulation.SetProgram(value);
        ExportGCodeCommand.NotifyCanExecuteChanged();
    }

    public ProjectViewModel Project => _project;

    /// <summary>Aktivni stroj projekta; mijenja se odabirom profila iz biblioteke.</summary>
    [ObservableProperty]
    private MachineProfile _activeMachine = BuiltInMachineProfiles.Ec300Plasma;

    /// <summary>Aktivna tehnologija projekta (iz biblioteke ili profila).</summary>
    [ObservableProperty]
    private TechnologySettings _activeTechnology = TechnologySettings.Default;

    private ProjectDocument CurrentDocument() => _project.BuildDocument(
        ActiveTechnology, ActiveMachine, Simulation.CurrentTime, Simulation.SpeedMultiplier);

    /// <summary>Cache putanje za spremanje (ADR-006) — null ako putanje nema.</summary>
    private ToolpathCache? CurrentCache()
    {
        if (LastToolpath is null || LastToolpath.Sequences.Count == 0)
        {
            return null;
        }

        return new ToolpathCache(
            CacheKey.ComputeInputHash(_project.DxfSources, ActiveTechnology),
            CacheKey.PipelineVersion,
            LastToolpath);
    }

    [RelayCommand]
    private async Task SaveProjectAsync()
    {
        var path = _project.FilePath
            ?? await _fileSave.PickSavePathAsync(_project.Name, ".ino").ConfigureAwait(true);
        if (path is null)
        {
            return;
        }

        await _project.SaveAsync(CurrentDocument(), path, CurrentCache()).ConfigureAwait(true);
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Projekt spremljen: {Path}", path);
        }
        StatusText = FormattableString.Invariant($"Projekt spremljen: {Path.GetFileName(path)}");
    }

    /// <summary>Otvara .ino projekt i regenerira derivirane podatke iz spremljenih DXF izvora.</summary>
    public async Task OpenProjectAsync(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var loaded = await _project.LoadAsync(path).ConfigureAwait(true);
        _project.ApplyLoaded(loaded, path == _project.AutoSavePath ? null : path);
        ActiveMachine = loaded.Document.Machine;
        ActiveTechnology = loaded.Document.Technology;

        // Geometrija se uvijek regenerira (potrebna je za prikaz i selekciju);
        // PUTANJA se uzima iz cachea ako je hash valjan, inače se regenerira (ADR-006).
        var useCache = loaded.Cache is not null;
        foreach (var source in loaded.Document.DxfSources)
        {
            _ = await ProcessDxfAsync(source.SourcePath, skipToolpath: useCache).ConfigureAwait(true);
        }

        if (loaded.Cache is not null)
        {
            LastToolpath = loaded.Cache.Program;
            _logger.LogInformation("Putanja učitana iz cachea (hash valjan) — bez ponovnog izračuna.");
        }

        if (loaded.Document.SimulationTimeSeconds is { } time)
        {
            Simulation.SpeedMultiplier = loaded.Document.SimulationSpeed;
            Simulation.CurrentTime = time;
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Projekt otvoren: {Name} (format v{Version}, {Sources} DXF izvora).",
                loaded.Document.Name, loaded.FormatVersion, loaded.Document.DxfSources.Count);
        }
        StatusText = FormattableString.Invariant($"Projekt otvoren: {loaded.Document.Name}");
    }

    /// <summary>
    /// Neuspio oporavak autosavea: logira uzrok i vraća UI u prazno stanje. NE baca —
    /// pozivatelj je async void event handler kojeg bi iznimka srušila.
    /// </summary>
    public void ReportRecoveryFailure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        _logger.LogError(exception, "Oporavak automatski spremljenog projekta nije uspio — autosave se odbacuje.");
        StatusText = "Oporavak nije uspio — automatski spremljeni projekt je odbačen.";
    }

    /// <summary>Autosave; poziva se periodički iz UI-ja dok postoji uvezena geometrija.</summary>
    public async Task AutoSaveAsync()
    {
        if (_project.DxfSources.Count == 0)
        {
            return;
        }

        await _project.AutoSaveAsync(CurrentDocument(), CurrentCache()).ConfigureAwait(true);
    }

    private bool CanExportGCode() => LastToolpath is not null && LastToolpath.Sequences.Count > 0;

    [RelayCommand(CanExecute = nameof(CanExportGCode))]
    private async Task ExportGCodeAsync()
    {
        var plugin = _postCatalog.Find(ActiveMachine.PostProcessorId);
        if (plugin is null)
        {
            _logger.LogError("Postprocesor '{Id}' nije registriran.", ActiveMachine.PostProcessorId);
            return;
        }

        var post = plugin.Create(plugin.DefaultDialect, ActiveMachine);
        var result = post.Generate(LastToolpath!);

        foreach (var warning in result.Warnings)
        {
            _logger.LogWarning("[POST] {Warning}", warning);
        }

        var path = await _fileSave.SaveTextAsync("program", result.FileExtension, result.GCode).ConfigureAwait(true);
        if (path is null)
        {
            return; // korisnik odustao
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "G-kod izvezen: {Path} ({Post} / {Machine}, {Lines} redaka).",
                path, plugin.DisplayName, ActiveMachine.Name, result.GCode.Count(c => c == '\n'));
        }
        StatusText = FormattableString.Invariant($"G-kod spremljen: {Path.GetFileName(path)}");
    }

    partial void OnSelectedIssueChanged(IssueDisplay? value)
    {
        if (value is not null)
        {
            Viewport.ZoomToIssue(value.Issue);
        }
    }

    partial void OnLastPipelineChanged(GeometryPipelineResult? value)
    {
        Issues = value is null ? [] : value.Report.Issues.Select(i => new IssueDisplay(i)).ToList();
        SelectedIssue = null;
        Viewport.SetScene(value is null
            ? RenderScene.Empty
            : new RenderScene(value.Contours, value.Report.Issues));
    }

    public MainViewModel(
        IUndoService undoService,
        IDxfImporter importer,
        IFilePickerService filePicker,
        IGeometryPipeline pipeline,
        IToolpathGenerator toolpathGenerator,
        IPostProcessorCatalog postCatalog,
        IFileSaveService fileSave,
        ProjectViewModel project,
        ILogger<MainViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(undoService);
        ArgumentNullException.ThrowIfNull(importer);
        ArgumentNullException.ThrowIfNull(filePicker);
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(toolpathGenerator);
        ArgumentNullException.ThrowIfNull(postCatalog);
        ArgumentNullException.ThrowIfNull(fileSave);
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(logger);

        _undoService = undoService;
        _importer = importer;
        _filePicker = filePicker;
        _pipeline = pipeline;
        _toolpathGenerator = toolpathGenerator;
        _postCatalog = postCatalog;
        _fileSave = fileSave;
        _project = project;
        _logger = logger;

        _undoService.StateChanged += (_, _) =>
        {
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        };
    }

    [RelayCommand]
    private async Task OpenDxfAsync()
    {
        var path = await _filePicker.PickOpenFileAsync([".dxf"]).ConfigureAwait(true);
        if (path is null)
        {
            return; // korisnik odustao — nije događaj vrijedan loga
        }

        StatusText = FormattableString.Invariant($"Učitavanje: {Path.GetFileName(path)}…");

        // Izvor se u projekt dodaje TEK NAKON uspješnog importa — neuspjeh je ranije
        // ostavljao fantomski DXF izvor u projektu (i markirao projekt dirty), koji bi
        // se potom i spremio u .ino datoteku.
        if (await ProcessDxfAsync(path).ConfigureAwait(true))
        {
            _project.AddDxf(path);
        }
    }

    /// <summary>
    /// Uvoz + geometrijski cjevovod (+ putanja, osim ako je preuzeta iz cachea).
    /// Vraća false ako je import neuspješan (cjevovod se tada ne pokreće).
    /// </summary>
    private async Task<bool> ProcessDxfAsync(string path, bool skipToolpath = false)
    {
        // Import je CPU/IO posao — ne blokira UI thread.
        var result = await Task.Run(() => _importer.Import(path)).ConfigureAwait(true);

        if (!result.Success)
        {
            _logger.LogError("DXF import neuspješan ({Importer}): {Error}", _importer.Name, result.Error);
            StatusText = "Import neuspješan — detalji u konzoli";
            return false;
        }

        LastImport = result;
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "DXF učitan: {Entities} entiteta, {Segments} segmenata, layeri: [{Layers}], jedinice: {Units} (×{Scale}), upozorenja: {WarningCount}.",
                result.Entities.Count,
                result.TotalSegmentCount,
                string.Join(", ", result.Layers),
                result.SourceUnits,
                result.UnitScaleToMm,
                result.Warnings.Count);
        }

        foreach (var w in result.Warnings.Take(20))
        {
            _logger.LogWarning("[{Code}] {Message}", w.Code, w.Message);
        }

        if (result.Warnings.Count > 20)
        {
            _logger.LogWarning("… i još {More} upozorenja (puni popis u log datoteci).", result.Warnings.Count - 20);
        }

        // M3: detekcija kontura + klasifikacija + validacija.
        var pipeline = await Task.Run(() => _pipeline.Process(result.Entities, ContourBuildSettings.Default)).ConfigureAwait(true);
        LastPipeline = pipeline;

        var outer = pipeline.Contours.Count(c => c.Kind == ContourKind.Outer);
        var holes = pipeline.Contours.Count(c => c.Kind == ContourKind.Hole);
        var open = pipeline.Contours.Count(c => c.Kind == ContourKind.Open);
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Konture: {Total} ({Outer} vanjskih, {Holes} rupa, {Open} otvorenih); automatskih spojeva: {Joins}. Validacija: {Errors} grešaka, {Warnings} upozorenja, {Infos} informacija.",
                pipeline.Contours.Count, outer, holes, open, pipeline.Joins.Count,
                pipeline.Report.ErrorCount, pipeline.Report.WarningCount, pipeline.Report.InfoCount);
        }

        foreach (var issue in pipeline.Report.Issues.Take(30))
        {
            var level = issue.Severity switch
            {
                ValidationSeverity.Error => LogLevel.Error,
                ValidationSeverity.Warning => LogLevel.Warning,
                _ => LogLevel.Information,
            };
            if (_logger.IsEnabled(level))
            {
                _logger.Log(level, "[{Code}] {Message}", issue.Code, issue.Message);
            }
        }

        if (pipeline.Report.Issues.Count > 30)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("… i još {More} nalaza validacije.", pipeline.Report.Issues.Count - 30);
            }
        }

        var errorSuffix = pipeline.Report.HasErrors
            ? FormattableString.Invariant($", {pipeline.Report.ErrorCount} GREŠAKA")
            : string.Empty;
        StatusText = FormattableString.Invariant(
            $"Učitano: {Path.GetFileName(path)} — {pipeline.Contours.Count} kontura ({outer} vanjskih, {holes} rupa, {open} otvorenih){errorSuffix}");

        if (skipToolpath)
        {
            return true; // putanja dolazi iz valjanog cachea
        }

        // M5: putanja se generira samo kad validacija nema grešaka.
        if (pipeline.Report.HasErrors)
        {
            LastToolpath = null;
            _logger.LogWarning("Putanja NIJE generirana — validacija ima {Errors} grešaka.", pipeline.Report.ErrorCount);
            return true; // import je USPIO — izvor pripada projektu, samo putanje nema
        }

        var toolpath = await Task.Run(() => _toolpathGenerator.Generate(pipeline.Contours, ActiveTechnology)).ConfigureAwait(true);
        LastToolpath = toolpath;
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Putanja: {Sequences} sekvenci (pierce), rez {CutLength:0.#} mm ({CutTime:0.#} s), brzi hodovi {RapidLength:0.#} mm ({RapidTime:0.#} s), probijanja {PierceTime:0.#} s — ukupno {Total:0.#} s.",
                toolpath.Sequences.Count,
                toolpath.Statistics.CutLength, toolpath.Statistics.CutTimeSeconds,
                toolpath.Statistics.RapidLength, toolpath.Statistics.RapidTimeSeconds,
                toolpath.Statistics.PierceTimeSeconds, toolpath.Statistics.TotalTimeSeconds);
        }

        return true;
    }

    private bool CanUndo() => _undoService.CanUndo;

    private bool CanRedo() => _undoService.CanRedo;

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        _undoService.Undo();
        StatusText = "Poništeno";
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        _undoService.Redo();
        StatusText = "Ponovljeno";
    }
}
