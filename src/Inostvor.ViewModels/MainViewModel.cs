using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Inostvor.Core.Abstractions;
using Inostvor.Core.Model.Geometry;
using Inostvor.Core.Model.Import;
using Inostvor.Core.Model.Toolpath;
using Inostvor.Core.Model.Validation;
using Inostvor.Rendering.Scene;

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
        ILogger<MainViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(undoService);
        ArgumentNullException.ThrowIfNull(importer);
        ArgumentNullException.ThrowIfNull(filePicker);
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(toolpathGenerator);
        ArgumentNullException.ThrowIfNull(logger);

        _undoService = undoService;
        _importer = importer;
        _filePicker = filePicker;
        _pipeline = pipeline;
        _toolpathGenerator = toolpathGenerator;
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

        // Import je CPU/IO posao — ne blokira UI thread.
        var result = await Task.Run(() => _importer.Import(path)).ConfigureAwait(true);

        if (!result.Success)
        {
            _logger.LogError("DXF import neuspješan ({Importer}): {Error}", _importer.Name, result.Error);
            StatusText = "Import neuspješan — detalji u konzoli";
            return;
        }

        LastImport = result;
        _logger.LogInformation(
            "DXF učitan: {Entities} entiteta, {Segments} segmenata, layeri: [{Layers}], jedinice: {Units} (×{Scale}), upozorenja: {WarningCount}.",
            result.Entities.Count,
            result.TotalSegmentCount,
            string.Join(", ", result.Layers),
            result.SourceUnits,
            result.UnitScaleToMm,
            result.Warnings.Count);

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
        _logger.LogInformation(
            "Konture: {Total} ({Outer} vanjskih, {Holes} rupa, {Open} otvorenih); automatskih spojeva: {Joins}. Validacija: {Errors} grešaka, {Warnings} upozorenja, {Infos} informacija.",
            pipeline.Contours.Count, outer, holes, open, pipeline.Joins.Count,
            pipeline.Report.ErrorCount, pipeline.Report.WarningCount, pipeline.Report.InfoCount);

        foreach (var issue in pipeline.Report.Issues.Take(30))
        {
            var level = issue.Severity switch
            {
                ValidationSeverity.Error => LogLevel.Error,
                ValidationSeverity.Warning => LogLevel.Warning,
                _ => LogLevel.Information,
            };
            _logger.Log(level, "[{Code}] {Message}", issue.Code, issue.Message);
        }

        if (pipeline.Report.Issues.Count > 30)
        {
            _logger.LogInformation("… i još {More} nalaza validacije.", pipeline.Report.Issues.Count - 30);
        }

        StatusText = FormattableString.Invariant(
            $"Učitano: {Path.GetFileName(path)} — {pipeline.Contours.Count} kontura ({outer} vanjskih, {holes} rupa, {open} otvorenih)"
            + (pipeline.Report.HasErrors ? FormattableString.Invariant($", {pipeline.Report.ErrorCount} GREŠAKA") : string.Empty));

        // M5: putanja se generira samo kad validacija nema grešaka.
        if (pipeline.Report.HasErrors)
        {
            LastToolpath = null;
            _logger.LogWarning("Putanja NIJE generirana — validacija ima {Errors} grešaka.", pipeline.Report.ErrorCount);
            return;
        }

        var toolpath = await Task.Run(() => _toolpathGenerator.Generate(pipeline.Contours, TechnologySettings.Default)).ConfigureAwait(true);
        LastToolpath = toolpath;
        _logger.LogInformation(
            "Putanja: {Sequences} sekvenci (pierce), rez {CutLength:0.#} mm ({CutTime:0.#} s), brzi hodovi {RapidLength:0.#} mm ({RapidTime:0.#} s), probijanja {PierceTime:0.#} s — ukupno {Total:0.#} s.",
            toolpath.Sequences.Count,
            toolpath.Statistics.CutLength, toolpath.Statistics.CutTimeSeconds,
            toolpath.Statistics.RapidLength, toolpath.Statistics.RapidTimeSeconds,
            toolpath.Statistics.PierceTimeSeconds, toolpath.Statistics.TotalTimeSeconds);
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
