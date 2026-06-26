using ViceSharp.Abstractions;
using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;

namespace ViceSharp.Host.Services;

internal static class HostProtocolMapper
{
    public static EmulatorStatusDto ToStatusDto(EmulatorRuntimeSession session)
    {
        session.UpdatePerformanceCounters();
        var state = session.Machine.GetState();
        var nominalClockHz = session.Architecture.MasterClockHz;
        var effectiveClockPercent = nominalClockHz <= 0
            ? 0
            : session.EffectiveClockHz / nominalClockHz * 100.0;

        var modelId = session.Architecture is IProfiledArchitectureDescriptor profiled
            ? profiled.MachineProfile.Id
            : string.Empty;

        return new EmulatorStatusDto(
            session.SessionId,
            session.Architecture.MachineName,
            session.RunState,
            state.Cycle,
            ToMachineStateDto(state),
            session.PowerState,
            session.LimiterEnabled ? session.LimiterRatePercent : 0.0,
            session.MeasuredFramesPerSecond,
            session.FrameCount,
            nominalClockHz,
            session.EffectiveClockHz,
            effectiveClockPercent,
            state.PC,
            modelId,
            session.HostKeyboardAutomation?.Description ?? string.Empty,
            session.HostKeyboardAutomation?.IsActive == true,
            session.LastHostAutomationError ?? string.Empty,
            session.IecBusActivity?.IsActive == true,
            session.IecBusActivity?.TransitionCount ?? 0,
            session.IecBusActivity?.ActivityState ?? "Idle")
        {
            PerCpuRates = ToPerCpuRateDtos(session.PerCpuRates),
            IecBusLines = ToIecBusLineDtos(session),
        };
    }

    private static IReadOnlyList<IecBusLineDto> ToIecBusLineDtos(EmulatorRuntimeSession session)
    {
        // The monitor panel is only meaningful for a true-drive rig - a real second CPU sharing
        // the IEC bus (a CoordinatorMachine with a live bus). A single-system C64 has the drive
        // endpoint baked into its always-on bus even with only a virtual/trap drive, so endpoint
        // count alone can't tell them apart; key off the rig type so the panel hides otherwise.
        if (session.IecBusActivity is null || session.Machine is not CoordinatorMachine { IecBus: not null })
            return Array.Empty<IecBusLineDto>();

        // Snapshot under the session lock so we do not race the emulation worker mutating the bus.
        BusSnapshot snapshot;
        lock (session.SyncRoot)
            snapshot = session.IecBusActivity.Snapshot();

        return BuildIecBusLines(snapshot);
    }

    /// <summary>
    /// Maps a bus snapshot to the monitor's line DTOs, but only when a peripheral actually shares
    /// the bus (host + >=1 device). A single-system C64 has just the host endpoint, so there is no
    /// inter-system IEC traffic to show - returns empty so the panel hides rather than showing
    /// idle ghost lines.
    /// </summary>
    internal static IReadOnlyList<IecBusLineDto> BuildIecBusLines(BusSnapshot snapshot)
    {
        if (snapshot.Lines.Count == 0 || snapshot.Endpoints.Count < 2)
            return Array.Empty<IecBusLineDto>();

        var lines = new IecBusLineDto[snapshot.Lines.Count];
        for (var i = 0; i < snapshot.Lines.Count; i++)
        {
            var line = snapshot.Lines[i];
            lines[i] = new IecBusLineDto(line.Signal, line.IsHigh, string.Join(", ", line.Pullers));
        }

        return lines;
    }

    private static IReadOnlyList<PerCpuRateDto> ToPerCpuRateDtos(IReadOnlyList<CpuRateReading> readings)
    {
        if (readings.Count == 0)
            return Array.Empty<PerCpuRateDto>();

        var dtos = new PerCpuRateDto[readings.Count];
        for (var i = 0; i < readings.Count; i++)
            dtos[i] = new PerCpuRateDto(readings[i].Label, readings[i].EffectiveClockHz, readings[i].EffectiveClockPercent);
        return dtos;
    }

    public static MachineStateDto ToMachineStateDto(MachineState state)
    {
        return new MachineStateDto(state.A, state.X, state.Y, state.S, state.P, state.PC, state.Cycle);
    }

    public static InputStateDto ToInputStateDto(EmulatorRuntimeSession session)
    {
        var keys = session.KeyStates.Values
            .OrderBy(key => key.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var joysticks = session.JoystickStates
            .OrderBy(pair => pair.Key)
            .Select(pair => new JoystickPortStateDto(pair.Key, pair.Value))
            .ToArray();

        return new InputStateDto(keys, joysticks, session.SelectedKeyboardMap);
    }

    public static SessionSettingsDto ToSettingsDto(EmulatorRuntimeSession session)
    {
        var profileId = session.Architecture is IProfiledArchitectureDescriptor profiled
            ? profiled.MachineProfile.Id
            : MinimalHostArchitectureDescriptor.ArchitectureId;

        return new SessionSettingsDto(
            profileId,
            new LimiterSettingsDto(session.LimiterRatePercent, session.LimiterEnabled, session.PacingStrategy),
            session.DisplaySettings,
            session.InputSettings,
            session.AudioSettings,
            session.ResourceSettings);
    }

    public static RpcStatus MissingSessionStatus(string sessionId)
    {
        return RpcStatus.NotFound($"Emulator session '{sessionId}' was not found.");
    }
}
