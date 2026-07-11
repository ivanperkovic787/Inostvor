using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PlasmaCAM.Core.Abstractions;
using PlasmaCAM.Core.Model.Import;

namespace PlasmaCAM.ViewModels;

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
    private readonly ILogger<MainViewModel> _logger;

    [ObservableProperty]
    private string _statusText = "Spremno";

    [ObservableProperty]
    private ImportResult? _lastImport;

    public MainViewModel(
        IUndoService undoService,
        IDxfImporter importer,
        IFilePickerService filePicker,
        ILogger<MainViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(undoService);
        ArgumentNullException.ThrowIfNull(importer);
        ArgumentNullException.ThrowIfNull(filePicker);
        ArgumentNullException.ThrowIfNull(logger);

        _undoService = undoService;
        _importer = importer;
        _filePicker = filePicker;
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

        StatusText = FormattableString.Invariant(
            $"Učitano: {Path.GetFileName(path)} — {result.Entities.Count} entiteta, {result.TotalSegmentCount} segmenata");
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
