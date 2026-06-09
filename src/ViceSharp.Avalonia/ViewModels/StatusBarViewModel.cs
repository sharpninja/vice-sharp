using ViceSharp.Protocol;

namespace ViceSharp.Avalonia.ViewModels;

public sealed class StatusBarViewModel : ObservableObject
{
    private string _statusText = "Power ? | Run ? | Limiter 100% | FPS 0.0 | Clock 0.000 MHz | Cycle 0 | PC 0000 | IEC Idle";

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public void ApplyStatus(EmulatorStatusDto? status, RpcStatus rpcStatus)
    {
        if (!rpcStatus.IsSuccess)
        {
            StatusText = rpcStatus.Message;
            return;
        }

        if (status is null)
            return;

        var clockMhz = status.EffectiveClockHz / 1_000_000.0;
        var limiterText = status.LimiterRatePercent > 0 && status.LimiterRatePercent < 1000
            ? $"Limiter {status.LimiterRatePercent:0}%"
            : "WARP";

        StatusText =
            $"Power {status.PowerState} | Run {status.RunState} | {limiterText} | " +
            $"FPS {status.MeasuredFramesPerSecond:0.0} | Clock {clockMhz:0.000} MHz ({status.EffectiveClockPercent:0}%) | " +
            $"Cycle {status.Cycle} | PC {status.MachineState.Pc:X4} | IEC {status.IecBusActivityState}";
    }
}
