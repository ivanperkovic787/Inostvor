namespace PlasmaCAM.Core.Abstractions;

/// <summary>
/// Središnji Undo/Redo servis. Jedina ispravna ulazna točka za izvršavanje
/// <see cref="IUndoableCommand"/> naredbi je <see cref="Do"/>.
/// </summary>
public interface IUndoService
{
    bool CanUndo { get; }

    bool CanRedo { get; }

    /// <summary>Opis naredbe koja bi se poništila (za UI tooltip "Undo: ..."), ili null.</summary>
    string? UndoDescription { get; }

    /// <summary>Opis naredbe koja bi se ponovila (za UI tooltip "Redo: ..."), ili null.</summary>
    string? RedoDescription { get; }

    /// <summary>Izvršava naredbu i stavlja je na undo stack. Briše redo povijest.</summary>
    void Do(IUndoableCommand command);

    /// <summary>Poništava zadnju naredbu. Baca <see cref="InvalidOperationException"/> ako je <see cref="CanUndo"/> false.</summary>
    void Undo();

    /// <summary>Ponovno izvršava zadnju poništenu naredbu. Baca <see cref="InvalidOperationException"/> ako je <see cref="CanRedo"/> false.</summary>
    void Redo();

    /// <summary>Briše cijelu povijest (npr. kod otvaranja novog dokumenta).</summary>
    void Clear();

    /// <summary>Okida se nakon svake promjene stanja (Do/Undo/Redo/Clear) — UI osvježava gumbe.</summary>
    event EventHandler? StateChanged;
}
