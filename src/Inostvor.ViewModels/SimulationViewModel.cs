using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Inostvor.Cam.Simulation;
using Inostvor.Core.Model.Toolpath;

namespace Inostvor.ViewModels;

/// <summary>
/// Transport simulacije za UI: Play/Pause/Stop/Seek/brzina. Drži SimulationSession
/// (čisti model iz Cam) — nikakva geometrijska ni vremenska logika ne živi ovdje.
/// </summary>
public sealed partial class SimulationViewModel : ObservableObject
{
    private SimulationSession? _session;
    private bool _suppressSeek;

    [ObservableProperty]
    private bool _hasProgram;


    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private double _currentTime;

    [ObservableProperty]
    private double _totalDuration;

    [ObservableProperty]
    private double _speedMultiplier = 1.0;

    [ObservableProperty]
    private string _statusText = "";

    public SimulationState? CurrentState => _session?.Current;

    public event EventHandler? RedrawRequested;

    public void SetProgram(ToolpathProgram? program)
    {
        _session = program is null || program.Sequences.Count == 0 ? null : new SimulationSession(program);
        HasProgram = _session is not null;
        IsPlaying = false;
        TotalDuration = _session?.TotalDuration ?? 0.0;
        SetTimeInternal(0.0);
        UpdateStatus();
        RedrawRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Poziva view ticker; realno vrijeme se skalira brzinom u sesiji.</summary>
    public void Advance(double realSeconds)
    {
        if (_session is null || !_session.IsPlaying)
        {
            return;
        }

        _session.SpeedMultiplier = SpeedMultiplier;
        _session.Advance(realSeconds);
        SetTimeInternal(_session.CurrentTime);
        if (_session.IsCompleted)
        {
            IsPlaying = false;
        }

        UpdateStatus();
        RedrawRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void TogglePlay()
    {
        if (_session is null)
        {
            return;
        }

        if (_session.IsPlaying)
        {
            _session.Pause();
        }
        else
        {
            if (_session.IsCompleted)
            {
                _session.Seek(0);
                SetTimeInternal(0);
            }

            _session.Play();
        }

        IsPlaying = _session.IsPlaying;
        UpdateStatus();
    }

    [RelayCommand]
    private void Stop()
    {
        if (_session is null)
        {
            return;
        }

        _session.Stop();
        IsPlaying = false;
        SetTimeInternal(0.0);
        UpdateStatus();
        RedrawRequested?.Invoke(this, EventArgs.Empty);
    }

    partial void OnCurrentTimeChanged(double value)
    {
        if (_suppressSeek || _session is null)
        {
            return;
        }

        _session.Seek(value); // korisnički seek (slider)
        UpdateStatus();
        RedrawRequested?.Invoke(this, EventArgs.Empty);
    }

    private void SetTimeInternal(double time)
    {
        _suppressSeek = true;
        CurrentTime = time;
        _suppressSeek = false;
    }

    private void UpdateStatus()
    {
        if (_session is null)
        {
            StatusText = "";
            return;
        }

        var s = _session.Current;
        var phase = s.Phase switch
        {
            MachinePhase.Rapid => "brzi hod",
            MachinePhase.Piercing => "probijanje",
            MachinePhase.Cutting => "rezanje",
            MachinePhase.Completed => "završeno",
            _ => "spremno",
        };
        StatusText = FormattableString.Invariant(
            $"{s.Time:0.0}/{_session.TotalDuration:0.0} s · {phase} · rez {s.Elapsed.Cut:0.0} s · rapid {s.Elapsed.Rapid:0.0} s · pierce {s.Elapsed.Pierce:0.0} s");
    }
}
