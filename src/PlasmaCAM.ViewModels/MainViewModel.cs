using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlasmaCAM.Core.Abstractions;

namespace PlasmaCAM.ViewModels;

/// <summary>
/// Shell ViewModel: status traka + Undo/Redo komande. Funkcionalne komande
/// (OpenDxf, Generate, ExportGCode) dolaze s modulima M2–M7.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly IUndoService _undoService;

    [ObservableProperty]
    private string _statusText = "Spremno";

    public MainViewModel(IUndoService undoService)
    {
        ArgumentNullException.ThrowIfNull(undoService);

        _undoService = undoService;
        _undoService.StateChanged += (_, _) =>
        {
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        };
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
