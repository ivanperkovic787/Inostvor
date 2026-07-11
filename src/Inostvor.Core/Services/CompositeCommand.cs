using Inostvor.Core.Abstractions;

namespace Inostvor.Core.Services;

/// <summary>
/// Grupira više naredbi u jednu Undo/Redo jedinicu (npr. "Generate toolpaths").
/// Ako neka naredba u Execute() baci iznimku, već izvršene naredbe se automatski
/// poništavaju obrnutim redoslijedom (rollback), pa dokument nikad ne ostaje u pola izmjene.
/// </summary>
public sealed class CompositeCommand : IUndoableCommand
{
    private readonly IReadOnlyList<IUndoableCommand> _commands;

    public CompositeCommand(string description, IReadOnlyList<IUndoableCommand> commands)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(commands);
        if (commands.Count == 0)
        {
            throw new ArgumentException("CompositeCommand mora sadržavati barem jednu naredbu.", nameof(commands));
        }

        Description = description;
        _commands = commands;
    }

    public string Description { get; }

    public void Execute()
    {
        var executed = 0;
        try
        {
            for (; executed < _commands.Count; executed++)
            {
                _commands[executed].Execute();
            }
        }
        catch
        {
            // Rollback već izvršenih, obrnutim redoslijedom.
            for (var i = executed - 1; i >= 0; i--)
            {
                _commands[i].Undo();
            }

            throw;
        }
    }

    public void Undo()
    {
        for (var i = _commands.Count - 1; i >= 0; i--)
        {
            _commands[i].Undo();
        }
    }
}
