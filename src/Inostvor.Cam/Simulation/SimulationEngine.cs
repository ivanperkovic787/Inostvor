using Inostvor.Core.Model.Toolpath;
using Inostvor.Kernel.Primitives;

namespace Inostvor.Cam.Simulation;

/// <summary>
/// Simulacija kao ČISTA FUNKCIJA vremena: iz ToolpathPrograma se JEDNOM izgradi
/// vremenska crta (kumulativna vremena po koraku), a <see cref="StateAt"/> vraća
/// stanje za bilo koje t — bez ikakvog internog mutabilnog stanja.
///
/// Posljedice dizajna (eksplicitni zahtjevi):
/// - PAUZA/NASTAVAK: stanje sesije je JEDAN broj (vrijeme). SimulationCheckpoint
///   se sprema i nastavlja bez ponovnog izračuna programa (timeline je
///   deterministička projekcija postojećeg IR-a).
/// - SIMULACIJA = ISTI IR koji koriste postprocesori; nula parsiranja G-koda.
/// - Nema ovisnosti o UI-ju: izlaz je SimulationState (čisti model).
/// </summary>
public sealed class SimulationEngine
{
    private enum StepKind { Rapid = 0, Pierce = 1, Move = 2 }

    private readonly record struct Step(
        StepKind Kind, double StartTime, double Duration,
        int SequenceIndex, int MoveIndex,
        Point2 From, Point2 To, CutMove? Move,
        TimeBreakdown ElapsedAtStart);

    private readonly List<Step> _steps = [];
    private readonly Point2 _origin = new(0, 0);

    public SimulationEngine(ToolpathProgram program)
    {
        ArgumentNullException.ThrowIfNull(program);
        Program = program;

        var time = 0.0;
        var cut = 0.0;
        var rapid = 0.0;
        var pierce = 0.0;
        var rapidRate = program.Technology.RapidRate;

        for (var si = 0; si < program.Sequences.Count; si++)
        {
            var sequence = program.Sequences[si];
            var rapidMove = program.Rapids[si];

            var rapidDuration = rapidRate > 0 ? rapidMove.Length / rapidRate * 60.0 : 0.0;
            _steps.Add(new Step(StepKind.Rapid, time, rapidDuration, si, -1,
                rapidMove.From, rapidMove.To, null, new TimeBreakdown(cut, rapid, pierce)));
            time += rapidDuration;
            rapid += rapidDuration;

            var pierceDuration = program.Technology.PierceTime;
            _steps.Add(new Step(StepKind.Pierce, time, pierceDuration, si, -1,
                sequence.PiercePoint, sequence.PiercePoint, null, new TimeBreakdown(cut, rapid, pierce)));
            time += pierceDuration;
            pierce += pierceDuration;

            for (var mi = 0; mi < sequence.Moves.Count; mi++)
            {
                var move = sequence.Moves[mi];
                _steps.Add(new Step(StepKind.Move, time, move.Duration, si, mi,
                    move.Geometry.StartPoint, move.Geometry.EndPoint, move, new TimeBreakdown(cut, rapid, pierce)));
                time += move.Duration;
                cut += move.Duration;
            }
        }

        TotalDuration = time;
        FinalBreakdown = new TimeBreakdown(cut, rapid, pierce);
    }

    public ToolpathProgram Program { get; }

    /// <summary>Ukupno trajanje. [s]</summary>
    public double TotalDuration { get; }

    public TimeBreakdown FinalBreakdown { get; }

    /// <summary>Stanje u trenutku t (clampano na [0, TotalDuration]). Čista funkcija — determinističko.</summary>
    public SimulationState StateAt(double timeSeconds)
    {
        if (_steps.Count == 0)
        {
            return new SimulationState(0, _origin, false, MachinePhase.Completed, -1, -1, 0, new TimeBreakdown(0, 0, 0));
        }

        var t = Math.Clamp(timeSeconds, 0.0, TotalDuration);
        if (t >= TotalDuration)
        {
            var last = _steps[^1];
            return new SimulationState(TotalDuration, last.To, false, MachinePhase.Completed,
                -1, -1, 1.0, FinalBreakdown);
        }

        // Binarno traženje koraka koji sadrži t.
        var lo = 0;
        var hi = _steps.Count - 1;
        while (lo < hi)
        {
            var mid = (lo + hi + 1) / 2;
            if (_steps[mid].StartTime <= t)
            {
                lo = mid;
            }
            else
            {
                hi = mid - 1;
            }
        }

        var step = _steps[lo];
        var progress = step.Duration > 0 ? Math.Clamp((t - step.StartTime) / step.Duration, 0.0, 1.0) : 1.0;
        var inStep = t - step.StartTime;

        var elapsed = step.Kind switch
        {
            StepKind.Rapid => step.ElapsedAtStart with { Rapid = step.ElapsedAtStart.Rapid + inStep },
            StepKind.Pierce => step.ElapsedAtStart with { Pierce = step.ElapsedAtStart.Pierce + inStep },
            _ => step.ElapsedAtStart with { Cut = step.ElapsedAtStart.Cut + inStep },
        };

        return step.Kind switch
        {
            StepKind.Rapid => new SimulationState(
                t, Point2.Lerp(step.From, step.To, progress), false, MachinePhase.Rapid,
                step.SequenceIndex, -1, progress, elapsed),
            StepKind.Pierce => new SimulationState(
                t, step.From, true, MachinePhase.Piercing,
                step.SequenceIndex, -1, progress, elapsed),
            _ => new SimulationState(
                t, step.Move!.Geometry.PointAt(progress), true, MachinePhase.Cutting,
                step.SequenceIndex, step.MoveIndex, progress, elapsed),
        };
    }
}
