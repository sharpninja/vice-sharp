using System;
using System.Collections.Generic;
using System.Linq;
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

    private IReadOnlyList<string> _perCpuRates = Array.Empty<string>();

    /// <summary>
    /// One formatted entry per CPU in the rig (host + each peripheral, e.g. "C1541 100%"), so
    /// the status bar lists each CPU's own speed distinctly. Empty for a single-CPU machine,
    /// where the headline <see cref="Clock"/> already shows the only CPU's rate.
    /// </summary>
    public IReadOnlyList<string> PerCpuRates
    {
        get => _perCpuRates;
        private set
        {
            if (SetProperty(ref _perCpuRates, value))
                OnPropertyChanged(nameof(HasPerCpuRates));
        }
    }

    /// <summary>True when there is more than one CPU to list, so the view shows the CPUs row.</summary>
    public bool HasPerCpuRates => _perCpuRates.Count > 0;

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

        // Only list per-CPU rows when there is more than one CPU (a true-drive rig or the C128's
        // two CPUs); a bare C64's single CPU is already the headline Clock reading.
        PerCpuRates = status.PerCpuRates.Count > 1
            ? status.PerCpuRates.Select(r => $"{ShortCpuLabel(r.Label)} {r.EffectiveClockPercent:0}%").ToArray()
            : Array.Empty<string>();

        StatusText =
            $"Power {Power} | Run {Run} | Limiter {Limiter} | " +
            $"FPS {Fps} | Clock {Clock} | " +
            $"Cycle {Cycle} | PC {Pc} | IEC {Iec}" +
            (PerCpuRates.Count > 0 ? $" | CPUs {string.Join(", ", PerCpuRates)}" : string.Empty);
    }

    // Compact a CPU's machine-name label for the status bar: "Commodore 64 PAL" -> "C64",
    // "Commodore 128" -> "C128", "C1541" -> "1541". Falls back to the label unchanged.
    private static string ShortCpuLabel(string label)
    {
        if (string.IsNullOrEmpty(label))
            return "CPU";
        if (label.StartsWith("Commodore 128", StringComparison.OrdinalIgnoreCase))
            return "C128";
        if (label.StartsWith("Commodore 64", StringComparison.OrdinalIgnoreCase))
            return "C64";
        if (label.Length > 1 && (label[0] is 'C' or 'c') && char.IsDigit(label[1]))
            return label[1..];
        return label;
    }
}
