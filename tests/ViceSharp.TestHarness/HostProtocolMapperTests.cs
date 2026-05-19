namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// Direct unit tests for <see cref="HostProtocolMapper"/>, the static
/// translation layer that converts <see cref="EmulatorRuntimeSession"/>
/// values plus <see cref="MachineState"/> snapshots into the
/// transport-neutral protocol DTOs that every service host returns to
/// callers (gRPC, REPL, MCP). The mapper has five public surfaces:
/// <c>ToStatusDto</c>, <c>ToMachineStateDto</c>, <c>ToInputStateDto</c>,
/// <c>ToSettingsDto</c>, and <c>MissingSessionStatus</c>. These tests
/// cover each surface end-to-end against a minimal-host runtime session
/// (no ROMs required) plus a stub profiled descriptor for the model-id
/// path, asserting that every CPU register, every session setting, the
/// missing-session NotFound status, and the input snapshot shape are
/// preserved byte-for-byte through the mapping boundary.
/// </summary>
public sealed class HostProtocolMapperTests
{
    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 ProtocolMapper).
    /// Use case: A monitor host reads a machine snapshot and asks the
    /// mapper to translate it to the over-the-wire DTO so a debugger UI
    /// can render register values.
    /// Acceptance: Every field on the input <see cref="MachineState"/>
    /// (A, X, Y, S, P, PC, Cycle) appears verbatim on the resulting
    /// <see cref="MachineStateDto"/>; the mapper performs no scaling,
    /// reordering, or sign extension.
    /// </summary>
    [Fact]
    public void ToMachineStateDto_PreservesCpuRegisterValues()
    {
        var state = new MachineState
        {
            A = 0x12,
            X = 0x34,
            Y = 0x56,
            S = 0x78,
            P = 0x9A,
            PC = 0xBCDE,
            Cycle = 1_234_567L
        };

        var dto = HostProtocolMapper.ToMachineStateDto(state);

        Assert.Equal(0x12, dto.A);
        Assert.Equal(0x34, dto.X);
        Assert.Equal(0x56, dto.Y);
        Assert.Equal(0x78, dto.S);
        Assert.Equal(0x9A, dto.P);
        Assert.Equal((ushort)0xBCDE, dto.Pc);
        Assert.Equal(1_234_567L, dto.Cycle);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 ProtocolMapper).
    /// Use case: A fresh minimal-host session is mapped to the status
    /// DTO immediately after creation (the canonical CreateSession +
    /// GetStatus flow).
    /// Acceptance: The DTO carries the session id verbatim, surfaces
    /// the architecture name and master clock from the descriptor, and
    /// reports the run-state and cycle counter consistent with a stopped
    /// machine that has not yet stepped.
    /// </summary>
    [Fact]
    public void ToStatusDto_IncludesSessionIdRunStateAndCycle()
    {
        var session = CreateMinimalSession("status-session");

        var dto = HostProtocolMapper.ToStatusDto(session);

        Assert.Equal("status-session", dto.SessionId);
        Assert.Equal(session.Architecture.MachineName, dto.Architecture);
        Assert.Equal(session.RunState, dto.RunState);
        Assert.Equal(session.Machine.GetState().Cycle, dto.Cycle);
        Assert.Equal(session.Architecture.MasterClockHz, dto.NominalClockHz);
        Assert.NotNull(dto.MachineState);
        Assert.Equal(session.Machine.GetState().PC, dto.Pc);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 ProtocolMapper).
    /// Use case: A session is built with a non-default run state,
    /// limiter rate, and power state (e.g. paused mid-debug). The status
    /// DTO must reflect those mutable fields, not the constructor
    /// defaults.
    /// Acceptance: <see cref="EmulatorStatusDto.RunState"/>,
    /// <see cref="EmulatorStatusDto.LimiterRatePercent"/>, and
    /// <see cref="EmulatorStatusDto.PowerState"/> match the mutated
    /// session.
    /// </summary>
    [Fact]
    public void ToStatusDto_ReflectsMutableSessionFields()
    {
        var session = CreateMinimalSession("mutable-session");
        session.RunState = EmulatorRunState.Paused;
        session.LimiterRatePercent = 42.5;
        session.PowerState = "Off";

        var dto = HostProtocolMapper.ToStatusDto(session);

        Assert.Equal(EmulatorRunState.Paused, dto.RunState);
        Assert.Equal(42.5, dto.LimiterRatePercent);
        Assert.Equal("Off", dto.PowerState);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 ProtocolMapper).
    /// Use case: The descriptor implements
    /// <see cref="IProfiledArchitectureDescriptor"/>, so the mapper must
    /// surface the profile id (e.g. "c64") on the status DTO's
    /// <see cref="EmulatorStatusDto.ModelId"/> field.
    /// Acceptance: ModelId equals the profile's stable Id; the host name
    /// still comes from the descriptor's MachineName.
    /// </summary>
    [Fact]
    public void ToStatusDto_PopulatesModelIdFromProfiledDescriptor()
    {
        var profiled = new StubProfiledDescriptor(C64MachineProfiles.Default);
        var session = CreateSessionWithArchitecture("profiled-session", profiled);

        var dto = HostProtocolMapper.ToStatusDto(session);

        Assert.Equal(C64MachineProfiles.Default.Id, dto.ModelId);
        Assert.Equal(profiled.MachineName, dto.Architecture);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 ProtocolMapper).
    /// Use case: The descriptor is non-profiled (the default minimal
    /// host arch).
    /// Acceptance: The mapper does not invent a model id; ModelId is the
    /// empty string so callers can detect "no profile" via simple
    /// equality on string.Empty.
    /// </summary>
    [Fact]
    public void ToStatusDto_ModelIdIsEmptyForNonProfiledDescriptor()
    {
        var session = CreateMinimalSession("plain-session");

        var dto = HostProtocolMapper.ToStatusDto(session);

        Assert.Equal(string.Empty, dto.ModelId);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 ProtocolMapper).
    /// Use case: No host keyboard automation is attached (the default
    /// state for any freshly created session).
    /// Acceptance: HostAutomationDescription is an empty string, the
    /// HostAutomationActive flag is false, and LastHostAutomationError
    /// is empty. These are the contract callers rely on to render an
    /// "idle" automation chip in UI without null-checking the DTO.
    /// </summary>
    [Fact]
    public void ToStatusDto_HostAutomationFieldsDefaultWhenNoAutomationAttached()
    {
        var session = CreateMinimalSession("no-automation");

        var dto = HostProtocolMapper.ToStatusDto(session);

        Assert.Equal(string.Empty, dto.HostAutomationDescription);
        Assert.False(dto.HostAutomationActive);
        Assert.Equal(string.Empty, dto.LastHostAutomationError);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 ProtocolMapper).
    /// Use case: A caller asks the mapper to produce a NotFound RPC
    /// status for a session id that is missing from the registry.
    /// Acceptance: The status code is NotFound and the message contains
    /// the session id verbatim, so clients can render a precise error
    /// without parsing.
    /// </summary>
    [Fact]
    public void MissingSessionStatus_HasNotFoundCodeAndContainsSessionId()
    {
        var status = HostProtocolMapper.MissingSessionStatus("ghost-session");

        Assert.Equal(RpcStatusCode.NotFound, status.Code);
        Assert.Contains("ghost-session", status.Message);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 ProtocolMapper).
    /// Use case: MissingSessionStatus is called with an empty session
    /// id (degenerate caller / typo path).
    /// Acceptance: The mapper still returns NotFound and produces a
    /// stable non-null message (never throws on edge inputs).
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingSessionStatus_HandlesEmptyOrWhitespaceSessionId(string sessionId)
    {
        var status = HostProtocolMapper.MissingSessionStatus(sessionId);

        Assert.Equal(RpcStatusCode.NotFound, status.Code);
        Assert.False(string.IsNullOrEmpty(status.Message));
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 ProtocolMapper).
    /// Use case: A freshly created session has neither key nor joystick
    /// state, but a default keyboard map id. The InputServiceHost flows
    /// this DTO to clients on every poll.
    /// Acceptance: The input DTO carries an empty keys list, an empty
    /// joysticks list, and a null selected keyboard map (the session is
    /// constructed without a resolved KeyboardMapDto).
    /// </summary>
    [Fact]
    public void ToInputStateDto_EmptySessionYieldsEmptyCollections()
    {
        var session = CreateMinimalSession("empty-input");

        var dto = HostProtocolMapper.ToInputStateDto(session);

        Assert.Empty(dto.Keys);
        Assert.Empty(dto.Joysticks);
        Assert.Null(dto.SelectedKeyboardMap);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 ProtocolMapper).
    /// Use case: The session holds a heterogeneous mix of keys and
    /// joystick ports.
    /// Acceptance: The mapper preserves every key state, sorts keys
    /// case-insensitively (so a UI's diff renderer sees stable ordering
    /// regardless of insertion order), and surfaces every joystick port
    /// ordered by InputPort enum value.
    /// </summary>
    [Fact]
    public void ToInputStateDto_SortsKeysCaseInsensitivelyAndJoysticksByPort()
    {
        var session = CreateMinimalSession("sorted-input");
        session.KeyStates["Space"] = new KeyStateDto("Space", true, true);
        session.KeyStates["a"] = new KeyStateDto("a", true, true);
        session.KeyStates["B"] = new KeyStateDto("B", false, false);
        session.JoystickStates[InputPort.Joystick2] = new JoystickStateDto(0x01, true, true);
        session.JoystickStates[InputPort.Joystick1] = new JoystickStateDto(0x02, false, true);

        var dto = HostProtocolMapper.ToInputStateDto(session);

        Assert.Equal(["a", "B", "Space"], dto.Keys.Select(k => k.Key));
        Assert.Equal(
            [InputPort.Joystick1, InputPort.Joystick2],
            dto.Joysticks.Select(j => j.Port));
        Assert.Equal(0x02, dto.Joysticks[0].State.DirectionMask);
        Assert.Equal(0x01, dto.Joysticks[1].State.DirectionMask);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 ProtocolMapper).
    /// Use case: ToSettingsDto is called on a session whose architecture
    /// is non-profiled (minimal host arch).
    /// Acceptance: ProfileId falls back to
    /// <see cref="MinimalHostArchitectureDescriptor.ArchitectureId"/>
    /// ("minimal"), the limiter rate and enabled flag round-trip, and
    /// the display/input/audio/resources settings are the exact same
    /// references the session carries (no clone, no defaults
    /// substitution).
    /// </summary>
    [Fact]
    public void ToSettingsDto_NonProfiledDescriptorFallsBackToMinimalProfileId()
    {
        var session = CreateMinimalSession("settings-session");
        session.LimiterRatePercent = 75;
        session.LimiterEnabled = false;

        var dto = HostProtocolMapper.ToSettingsDto(session);

        Assert.Equal(MinimalHostArchitectureDescriptor.ArchitectureId, dto.ProfileId);
        Assert.Equal(75, dto.Limiter.RatePercent);
        Assert.False(dto.Limiter.IsEnabled);
        Assert.Same(session.DisplaySettings, dto.Display);
        Assert.Same(session.InputSettings, dto.Input);
        Assert.Same(session.AudioSettings, dto.Audio);
        Assert.Same(session.ResourceSettings, dto.Resources);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 ProtocolMapper).
    /// Use case: ToSettingsDto is called on a session whose architecture
    /// is a profiled descriptor.
    /// Acceptance: The settings DTO surfaces the profile id from the
    /// descriptor rather than the minimal fallback, so settings hosts
    /// can round-trip the active profile across UpdateSettings calls.
    /// </summary>
    [Fact]
    public void ToSettingsDto_ProfiledDescriptorSurfacesProfileId()
    {
        var profiled = new StubProfiledDescriptor(C64MachineProfiles.Default);
        var session = CreateSessionWithArchitecture("profiled-settings", profiled);

        var dto = HostProtocolMapper.ToSettingsDto(session);

        Assert.Equal(C64MachineProfiles.Default.Id, dto.ProfileId);
    }

    private static EmulatorRuntimeSession CreateMinimalSession(string sessionId)
    {
        var factory = new DefaultEmulatorRuntimeFactory(
            new ArchitectureBuilder(),
            [MinimalHostArchitectureDescriptor.Instance],
            MinimalHostArchitectureDescriptor.ArchitectureId);

        var session = factory.Create(new CreateEmulatorSessionRequest(MinimalHostArchitectureDescriptor.ArchitectureId));
        return new EmulatorRuntimeSession(sessionId, session.Architecture, session.Machine);
    }

    private static EmulatorRuntimeSession CreateSessionWithArchitecture(
        string sessionId,
        IArchitectureDescriptor descriptor)
    {
        var factory = new DefaultEmulatorRuntimeFactory(
            new ArchitectureBuilder(),
            [MinimalHostArchitectureDescriptor.Instance],
            MinimalHostArchitectureDescriptor.ArchitectureId);
        var seed = factory.Create(new CreateEmulatorSessionRequest(MinimalHostArchitectureDescriptor.ArchitectureId));
        return new EmulatorRuntimeSession(sessionId, descriptor, seed.Machine);
    }

    private sealed class StubProfiledDescriptor : IProfiledArchitectureDescriptor
    {
        public StubProfiledDescriptor(IMachineProfile profile)
        {
            MachineProfile = profile;
        }

        public IMachineProfile MachineProfile { get; }

        public string MachineName => "Stub Profiled Machine";

        public long MasterClockHz => MachineProfile.NominalClockHz;

        public VideoStandard VideoStandard => MachineProfile.VideoStandard;

        public IReadOnlyList<DeviceDescriptor> Devices => Array.Empty<DeviceDescriptor>();

        public IRomSet? RequiredRoms => null;
    }
}
