using ViceSharp.Abstractions;
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
            session.LimiterRatePercent,
            session.MeasuredFramesPerSecond,
            session.FrameCount,
            nominalClockHz,
            session.EffectiveClockHz,
            effectiveClockPercent,
            state.PC,
            modelId,
            session.HostKeyboardAutomation?.Description ?? string.Empty,
            session.HostKeyboardAutomation?.IsActive == true,
            session.LastHostAutomationError ?? string.Empty);
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
            new LimiterSettingsDto(session.LimiterRatePercent, session.LimiterEnabled),
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
