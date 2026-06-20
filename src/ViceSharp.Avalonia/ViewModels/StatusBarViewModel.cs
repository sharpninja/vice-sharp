using ViceSharp.Protocol;

namespace ViceSharp.Avalonia.ViewModels;

/// <summary>
/// Backs the bottom status bar. Each runtime field is exposed as its own bare
/// value so the view can lay them out as cells in a two-row grid (the labels
/// live in the view). <see cref="StatusText"/> keeps the legacy single-line
/// composition for the grid tooltip and any text consumers. On a failed status
/// poll the message is routed to <see cref="ErrorText"/> so the grid can
/// collapse to a single error line.
/// </summary>
public sealed class StatusBarViewModel : ObservableObject
{
    private string _power = "?";
    private string _run = "?";
    private string _limiter = "100%";
    private string _fps = "0.0";
    private string _clock = "0.000 MHz";
    private string _cycle = "0";
    private string _pc = "0000";
    private string _iec = "Idle";
    private string _errorText = string.Empty;
    private string _statusText = "Power ? | Run ? | Limiter 100% | FPS 0.0 | Clock 0.000 MHz | Cycle 0 | PC 0000 | IEC Idle";

    public string Power { get => _power; private set => SetProperty(ref _power, value); }
    public string Run { get => _run; private set => SetProperty(ref _run, value); }
    public string Limiter { get => _limiter; private set => SetProperty(ref _limiter, value); }
    public string Fps { get => _fps; private set => SetProperty(ref _fps, value); }
    public string Clock { get => _clock; private set => SetProperty(ref _clock, value); }
    public string Cycle { get => _cycle; private set => SetProperty(ref _cycle, value); }
    public string Pc { get => _pc; private set => SetProperty(ref _pc, value); }
    public string Iec { get => _iec; private set => SetProperty(ref _iec, value); }

    /// <summary>Non-empty when the last status poll failed; the view shows this in place of the cells.</summary>
    public string ErrorText
    {
        get => _errorText;
        private set
        {
            if (SetProperty(ref _errorText, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    /// <summary>True while a poll error is being shown, so the cell grid collapses to the error line.</summary>
    public bool HasError => _errorText.Length > 0;

    /// <summary>Legacy single-line composition (labels + values), used as the grid tooltip.</summary>
    public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }

    public void ApplyStatus(EmulatorStatusDto? status, RpcStatus rpcStatus)
    {
        if (!rpcStatus.IsSuccess)
        {
            ErrorText = rpcStatus.Message;
            return;
        }

        if (status is null)
            return;

        ErrorText = string.Empty;

        var clockMhz = status.EffectiveClockHz / 1_000_000.0;

        Power = $"{status.PowerState}";
        Run = $"{status.RunState}";
        Limiter = status.LimiterRatePercent > 0 && status.LimiterRatePercent < 1000
            ? $"{status.LimiterRatePercent:0}%"
            : "WARP";
        Fps = $"{status.MeasuredFramesPerSecond:0.0}";
        Clock = $"{clockMhz:0.000} MHz ({status.EffectiveClockPercent:0}%)";
        Cycle = $"{status.Cycle}";
        Pc = $"{status.MachineState.Pc:X4}";
        Iec = $"{status.IecBusActivityState}";

        StatusText =
            $"Power {Power} | Run {Run} | Limiter {Limiter} | " +
            $"FPS {Fps} | Clock {Clock} | " +
            $"Cycle {Cycle} | PC {Pc} | IEC {Iec}";
    }
}
