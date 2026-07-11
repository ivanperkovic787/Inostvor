using Inostvor.Core.Abstractions;

namespace Inostvor.Core.Services;

/// <summary>
/// Standardna implementacija <see cref="IUndoService"/> s ograničenom dubinom povijesti.
/// Undo povijest je LinkedList (a ne Stack) kako bi se kod prekoračenja kapaciteta
/// mogla ukloniti NAJSTARIJA naredba s dna.
/// </summary>
public sealed class UndoRedoService : IUndoService
{
    private readonly LinkedList<IUndoableCommand> _undoHistory = new();
    private readonly Stack<IUndoableCommand> _redoHistory = new();
    private readonly int _capacity;

    public UndoRedoService(int capacity = 100)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _capacity = capacity;
    }

    public bool CanUndo => _undoHistory.Count > 0;

    public bool CanRedo => _redoHistory.Count > 0;

    public string? UndoDescription => _undoHistory.Last?.Value.Description;

    public string? RedoDescription => _redoHistory.TryPeek(out var cmd) ? cmd.Description : null;

    public event EventHandler? StateChanged;

    public void Do(IUndoableCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Baci li Execute iznimku, povijest ostaje netaknuta — neuspjela naredba se ne pamti.
        command.Execute();

        _undoHistory.AddLast(command);
        if (_undoHistory.Count > _capacity)
        {
            _undoHistory.RemoveFirst();
        }

        _redoHistory.Clear();
        OnStateChanged();
    }

    public void Undo()
    {
        if (!CanUndo)
        {
            throw new InvalidOperationException("Undo povijest je prazna.");
        }

        var command = _undoHistory.Last!.Value;
        _undoHistory.RemoveLast();
        command.Undo();
        _redoHistory.Push(command);
        OnStateChanged();
    }

    public void Redo()
    {
        if (!CanRedo)
        {
            throw new InvalidOperationException("Redo povijest je prazna.");
        }

        var command = _redoHistory.Pop();
        command.Execute();
        _undoHistory.AddLast(command);
        OnStateChanged();
    }

    public void Clear()
    {
        _undoHistory.Clear();
        _redoHistory.Clear();
        OnStateChanged();
    }

    private void OnStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
}
