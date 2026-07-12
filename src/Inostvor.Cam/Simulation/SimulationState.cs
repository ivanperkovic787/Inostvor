using Inostvor.Kernel.Primitives;

namespace Inostvor.Cam.Simulation;

/// <summary>Faza u kojoj se stroj (planirano ili stvarno) nalazi.</summary>
public enum MachinePhase
{
    Idle = 0,
    Rapid = 1,
    Piercing = 2,
    Cutting = 3,
    Completed = 4,
}

/// <summary>Razdvojena vremena — temelj za analizu posla.</summary>
/// <param name="Cut">Proteklo vrijeme rezanja. [s]</param>
/// <param name="Rapid">Proteklo vrijeme brzih pomaka. [s]</param>
/// <param name="Pierce">Proteklo vrijeme probijanja. [s]</param>
public sealed record TimeBreakdown(double Cut, double Rapid, double Pierce)
{
    public double Total => Cut + Rapid + Pierce;
}

/// <summary>
/// ČISTI MODEL STANJA — bez ikakve ovisnosti o rendereru, SkiaSharpu ili WinUI-ju.
/// Isti oblik stanja opisuje planiranu simulaciju (SimulationEngine) i, u budućnosti,
/// STVARNI stroj (Live Monitor / Digital Twin): pozicija, gori li luk, faza,
/// aktivni potez. Renderer ovo stanje isključivo prikazuje.
/// </summary>
/// <param name="Time">Vrijeme od početka. [s]</param>
/// <param name="Position">Trenutna pozicija torcha. [mm]</param>
/// <param name="TorchOn">Gori li luk (pierce ili rezanje).</param>
/// <param name="Phase">Faza stroja.</param>
/// <param name="SequenceIndex">Indeks aktivne CutSequence; -1 izvan sekvence.</param>
/// <param name="MoveIndex">Indeks aktivnog CutMove unutar sekvence; -1 ako nije rezanje.</param>
/// <param name="MoveProgress">Napredak aktivnog poteza t ∈ [0,1].</param>
/// <param name="Elapsed">Razdvojena protekla vremena.</param>
public sealed record SimulationState(
    double Time,
    Point2 Position,
    bool TorchOn,
    MachinePhase Phase,
    int SequenceIndex,
    int MoveIndex,
    double MoveProgress,
    TimeBreakdown Elapsed);

/// <summary>
/// Izvor stanja stroja — apstrakcija preko koje UI promatra i PLANIRANU simulaciju
/// i (budući) STVARNI stroj. Live Monitor / Digital Twin = nova implementacija
/// ovog sučelja hranjena telemetrijom (pozicija, stanje luka, M-kodovi) — bez
/// ijedne promjene u rendereru ili ViewModelima.
/// </summary>
public interface IMachineStateSource
{
    SimulationState Current { get; }

    event EventHandler? StateChanged;
}
