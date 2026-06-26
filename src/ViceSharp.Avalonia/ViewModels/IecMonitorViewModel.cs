using System;
using System.Collections.Generic;
using System.Linq;
using ViceSharp.Protocol;

namespace ViceSharp.Avalonia.ViewModels;

/// <summary>
/// Backs the real-time IEC bus monitor panel (FR-IECMON-001 / FR-IECSPY-001). From the polled
/// host status it renders each IEC line's live resolved level and which endpoints are pulling it
/// low, plus the bus activity summary (Active/Idle + transition count). Hidden when the session
/// has no IEC bus (a single-system C64 with no drive attached).
/// </summary>
public sealed class IecMonitorViewModel : ObservableObject
{
    private IReadOnlyList<string> _lines = Array.Empty<string>();
    private string _activity = "Idle";
    private long _transitions;
    private bool _hasBus;

    /// <summary>One formatted entry per IEC line, e.g. "ATN: low (c64)" or "DATA: high".</summary>
    public IReadOnlyList<string> Lines { get => _lines; private set => SetProperty(ref _lines, value); }

    /// <summary>Bus activity summary: "Active" while traffic is in flight, else "Idle".</summary>
    public string Activity { get => _activity; private set => SetProperty(ref _activity, value); }

    /// <summary>Running total of IEC line transitions since the session started.</summary>
    public long Transitions { get => _transitions; private set => SetProperty(ref _transitions, value); }

    /// <summary>True when the session has a true-drive IEC bus, so the panel is shown.</summary>
    public bool HasBus { get => _hasBus; private set => SetProperty(ref _hasBus, value); }

    public void ApplyStatus(EmulatorStatusDto? status, RpcStatus rpcStatus)
    {
        if (!rpcStatus.IsSuccess || status is null)
            return;

        Activity = status.IecBusActivityState;
        Transitions = status.IecBusTransitionCount;
        HasBus = status.IecBusLines.Count > 0;
        Lines = status.IecBusLines.Count == 0
            ? Array.Empty<string>()
            : status.IecBusLines.Select(FormatLine).ToArray();
    }

    private static string FormatLine(IecBusLineDto line)
    {
        var level = line.IsHigh ? "high" : "low";
        return string.IsNullOrEmpty(line.Pullers)
            ? $"{line.Signal}: {level}"
            : $"{line.Signal}: {level} ({line.Pullers})";
    }
}
