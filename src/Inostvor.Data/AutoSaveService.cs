using Inostvor.Core.Abstractions;

namespace Inostvor.Data;

/// <summary>
/// Auto Save + oporavak. Mehanizam: sentinel datoteka "session.lock" postoji dok
/// aplikacija radi i briše se pri urednom izlasku. Ako pri pokretanju sentinel
/// POSTOJI, a autosave datoteka postoji → prošla sesija je pala i nudi se oporavak.
///
/// Autosave je OBIČAN .ino projekt (isti ProjectStore) — oporavljena datoteka je
/// potpuno valjan projekt, ne poseban format.
/// </summary>
public sealed class AutoSaveService : IAutoSaveService
{
    private readonly string _sessionLockPath;

    public AutoSaveService(string appDataDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appDataDirectory);
        Directory.CreateDirectory(appDataDirectory);
        AutoSavePath = Path.Combine(appDataDirectory, "autosave.ino");
        _sessionLockPath = Path.Combine(appDataDirectory, "session.lock");

        // Stanje se očitava JEDNOM pri konstrukciji — prije nego MarkSessionStart prepiše sentinel.
        RecoveryAvailable = File.Exists(_sessionLockPath) && File.Exists(AutoSavePath);
    }

    public string AutoSavePath { get; }

    public bool RecoveryAvailable { get; }

    public void MarkSessionStart()
        => File.WriteAllText(_sessionLockPath, DateTime.UtcNow.ToString("O"));

    public void MarkCleanExit()
    {
        if (File.Exists(_sessionLockPath))
        {
            File.Delete(_sessionLockPath);
        }
    }

    public void ClearAutoSave()
    {
        if (File.Exists(AutoSavePath))
        {
            File.Delete(AutoSavePath);
        }
    }
}
