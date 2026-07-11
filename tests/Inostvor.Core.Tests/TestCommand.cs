using Inostvor.Core.Abstractions;

namespace Inostvor.Core.Tests;

/// <summary>Testna naredba koja bilježi izvršavanja u zajednički dnevnik.</summary>
internal sealed class TestCommand : IUndoableCommand
{
    private readonly string _name;
    private readonly List<string> _journal;
    private readonly Action? _onExecute;

    public TestCommand(string name, List<string> journal, Action? onExecute = null)
    {
        _name = name;
        _journal = journal;
        _onExecute = onExecute;
    }

    public string Description => _name;

    public void Execute()
    {
        _onExecute?.Invoke();
        _journal.Add($"{_name}:do");
    }

    public void Undo() => _journal.Add($"{_name}:undo");
}
