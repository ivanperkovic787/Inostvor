namespace Inostvor.Core.Abstractions;

/// <summary>
/// Apstrakcija UI dispatchera — ViewModeli njome prebacuju rad na UI thread
/// bez ovisnosti o WinUI tipovima (testabilnost).
/// </summary>
public interface IDispatcherService
{
    bool HasThreadAccess { get; }

    /// <summary>Izvršava akciju na UI threadu; ako smo već na njemu, izvršava odmah (sinkrono).</summary>
    void Enqueue(Action action);
}
