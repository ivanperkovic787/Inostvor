using Inostvor.Core.Model.Toolpath;

namespace Inostvor.Cam.Simulation;

/// <summary>Checkpoint sesije — dovoljan za spremanje i nastavak BEZ ponovnog izračuna programa.</summary>
/// <param name="TimeSeconds">Pozicija na vremenskoj crti. [s]</param>
/// <param name="SpeedMultiplier">Brzina reprodukcije.</param>
public sealed record SimulationCheckpoint(double TimeSeconds, double SpeedMultiplier);

/// <summary>
/// Reprodukcija planirane simulacije: Play/Pause/Seek/Advance nad čistim
/// SimulationEngineom. Implementira IMachineStateSource — isti kanal kroz koji
/// će budući Live Monitor slati STVARNO stanje stroja.
/// </summary>
public sealed class SimulationSession : IMachineStateSource
{
    private readonly SimulationEngine _engine;
    private SimulationState _current;

    public SimulationSession(ToolpathProgram program)
    {
        ArgumentNullException.ThrowIfNull(program);
        _engine = new SimulationEngine(program);
        _current = _engine.StateAt(0);
    }

    public double TotalDuration => _engine.TotalDuration;

    public TimeBreakdown FinalBreakdown => _engine.FinalBreakdown;

    public double CurrentTime { get; private set; }

    public double SpeedMultiplier { get; set; } = 1.0;

    public bool IsPlaying { get; private set; }

    public bool IsCompleted => CurrentTime >= TotalDuration;

    public SimulationState Current => _current;

    public event EventHandler? StateChanged;

    public void Play() => IsPlaying = !IsCompleted && true;

    public void Pause() => IsPlaying = false;

    public void Stop()
    {
        IsPlaying = false;
        Seek(0);
    }

    /// <summary>Pomak realnog vremena (npr. iz UI tickera); skaliran brzinom reprodukcije.</summary>
    public void Advance(double realSeconds)
    {
        if (!IsPlaying)
        {
            return;
        }

        Seek(CurrentTime + (realSeconds * SpeedMultiplier));
        if (IsCompleted)
        {
            IsPlaying = false;
        }
    }

    public void Seek(double timeSeconds)
    {
        CurrentTime = Math.Clamp(timeSeconds, 0.0, TotalDuration);
        _current = _engine.StateAt(CurrentTime);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public SimulationCheckpoint Save() => new(CurrentTime, SpeedMultiplier);

    /// <summary>Nastavak iz checkpointa — bez ponovnog izračuna ToolpathPrograma.</summary>
    public void Restore(SimulationCheckpoint checkpoint)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        SpeedMultiplier = checkpoint.SpeedMultiplier;
        Seek(checkpoint.TimeSeconds);
    }
}
