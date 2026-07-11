namespace Inostvor.Core.Abstractions;

/// <summary>
/// Jedinica izmjene dokumenta (Command pattern). SVE mutacije CutJob-a i ostalog
/// stanja dokumenta idu isključivo kroz implementacije ovog sučelja — nikad direktno.
/// Time je Undo/Redo garantiran dizajnom, a ne naknadnom disciplinom.
/// </summary>
public interface IUndoableCommand
{
    /// <summary>Kratki opis izmjene, prikazuje se u UI-ju (npr. "Promjena kerf širine").</summary>
    string Description { get; }

    /// <summary>Izvršava izmjenu. Ako baci iznimku, implementacija ne smije ostaviti djelomično stanje.</summary>
    void Execute();

    /// <summary>Vraća izmjenu. Poziva se isključivo nakon uspješnog <see cref="Execute"/>.</summary>
    void Undo();
}
