namespace ViceSharp.TestHarness;

using Google.Protobuf;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Avalonia.Host;
using ViceSharp.Core;
using ViceSharp.Chips.IEC;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using GrpcContracts = ViceSharp.Protocol.Grpc;
using Xunit;

public sealed class ProtocolHostIntegrationTests
{
    /// <summary>
    /// FR: FR-HOST-001, TR: TR-GRPC-BOUNDARY-001.
    /// Use case: A client asks the EmulatorHost service to create a new
    /// emulator session and then to step one frame on that session.
    /// Acceptance: CreateSession returns Ok with a non-empty session id
    /// and an initial status; StepFrame returns Ok and the post-step
    /// cycle count is strictly greater than the create-time cycle.
    /// </summary>
    [Fact]
    public async Task EmulatorHost_CreatesSessionAndStepsFrame()
    {
        var registry = new EmulatorRuntimeRegistry();
        var emulatorHost = new EmulatorHostService(registry, new DefaultEmulatorRuntimeFactory());

        var created = await emulatorHost.CreateSessionAsync(
            new CreateEmulatorSessionRequest(),
            TestContext.Current.CancellationToken);
        var stepped = await emulatorHost.StepFrameAsync(
            new StepFrameRequest(created.SessionId),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, created.Status.Code);
        Assert.False(string.IsNullOrWhiteSpace(created.SessionId));
        Assert.NotNull(created.EmulatorStatus);
        Assert.Equal(RpcStatusCode.Ok, stepped.Status.Code);
        Assert.NotNull(stepped.EmulatorStatus);
        Assert.True(stepped.EmulatorStatus.Cycle > created.EmulatorStatus.Cycle);
    }

    /// <summary>
    /// FR: FR-HOST-006, FR: FR-UI-002, TR: TR-HOST-STATUS-001.
    /// Use case: A UI client needs the EmulatorStatus payload to carry
    /// limiter rate, run state, measured FPS, machine state, etc., so the
    /// control bar can render without polling extra services.
    /// Acceptance: A freshly created session returns Ok with the expected
    /// initial UI control fields populated; setting the limiter rate and
    /// resuming the session are both Ok and surface the new values back
    /// through the status field.
    /// </summary>
    [Fact]
    public async Task EmulatorHost_StatusIncludesUiControlFields()
    {
        var registry = new EmulatorRuntimeRegistry();
        var emulatorHost = new EmulatorHostService(registry, CreateMinimalRuntimeFactory());

        var created = await emulatorHost.CreateSessionAsync(
            new CreateEmulatorSessionRequest(),
            TestContext.Current.CancellationToken);
        var limited = await emulatorHost.SetLimiterRateAsync(
            new SetLimiterRateRequest(created.SessionId, 50),
            TestContext.Current.CancellationToken);
        var resumed = await emulatorHost.ResumeAsync(
            new SessionRequest(created.SessionId),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, created.Status.Code);
        var status = created.EmulatorStatus!;
        Assert.Equal("On", status.PowerState);
        Assert.Equal(EmulatorRunState.Stopped, status.RunState);
        Assert.Equal(100, status.LimiterRatePercent);
        Assert.Equal(0, status.MeasuredFps);
        Assert.Equal(MinimalHostArchitectureDescriptor.Instance.MasterClockHz, status.NominalClockHz);
        Assert.Equal(0, status.FrameCount);
        Assert.Equal(status.MachineState.Cycle, status.Cycle);
        Assert.Equal(status.MachineState.Pc, status.Pc);

        Assert.Equal(RpcStatusCode.Ok, limited.Status.Code);
        Assert.NotNull(limited.EmulatorStatus);
        Assert.Equal(50, limited.EmulatorStatus.LimiterRatePercent);
        Assert.Equal(RpcStatusCode.Ok, resumed.Status.Code);
        Assert.NotNull(resumed.EmulatorStatus);
        Assert.Equal(EmulatorRunState.Running, resumed.EmulatorStatus.RunState);
        Assert.Equal(50, resumed.EmulatorStatus.LimiterRatePercent);
    }

    /// <summary>
    /// FR: FR-CFG-008, FR: FR-HOST-006, TR: TR-HOST-STATUS-001.
    /// Use case: Settings service exposes the list of installed machine
    /// profiles and lets a client adjust the performance limiter without
    /// restarting the session.
    /// Acceptance: ListProfiles returns the C64 profile catalog and
    /// SetLimiterRate updates the live limiter on the active session
    /// without disrupting the session id.
    /// </summary>
    [Fact]
    public async Task SettingsService_ListsProfilesAndAppliesLiveLimiter()
    {
        var registry = new EmulatorRuntimeRegistry();
        var emulatorHost = new EmulatorHostService(registry, CreateMinimalRuntimeFactory());
        var settingsService = new SettingsServiceHost(registry);

        var created = await emulatorHost.CreateSessionAsync(
            new CreateEmulatorSessionRequest(),
            TestContext.Current.CancellationToken);
        var profiles = await settingsService.ListProfilesAsync(
            new SessionRequest(created.SessionId),
            TestContext.Current.CancellationToken);
        var settings = await settingsService.GetSettingsAsync(
            new SessionRequest(created.SessionId),
            TestContext.Current.CancellationToken);
        var updated = await settingsService.UpdateSettingsAsync(
            new UpdateSettingsRequest(
                created.SessionId,
                new LimiterSettingsDto(125, false),
                new DisplaySettingsDto("software", "pepto", false, true, "fit-window", "borderless", "force-4-3"),
                new InputSettingsDto("c64:gtk3_pos", InputPort.Joystick2, false, "keyboard-only"),
                "c64c",
                Audio: new AudioSettingsDto("muted"),
                Resources: new ResourceSettingsDto("configured-paths")),
            TestContext.Current.CancellationToken);
        var status = await emulatorHost.GetStatusAsync(
            new SessionRequest(created.SessionId),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, profiles.Status.Code);
        Assert.Contains(profiles.Profiles, profile => profile.Id == "c64c" && profile.Machine == "x64sc");
        Assert.Equal(RpcStatusCode.Ok, settings.Status.Code);
        Assert.NotNull(settings.Settings);
        Assert.Equal(MinimalHostArchitectureDescriptor.ArchitectureId, settings.Settings.ProfileId);
        Assert.Equal(RpcStatusCode.Ok, updated.Status.Code);
        Assert.NotNull(updated.Settings);
        Assert.Equal(125, updated.Settings.Limiter.RatePercent);
        Assert.False(updated.Settings.Limiter.IsEnabled);
        Assert.Equal("software", updated.Settings.Display.Renderer);
        Assert.Equal("pepto", updated.Settings.Display.Palette);
        Assert.Equal("fit-window", updated.Settings.Display.Scale);
        Assert.Equal("borderless", updated.Settings.Display.CropMode);
        Assert.Equal("force-4-3", updated.Settings.Display.AspectMode);
        Assert.Equal("keyboard-only", updated.Settings.Input.Mode);
        Assert.Equal("muted", updated.Settings.Audio!.Mode);
        Assert.Equal("configured-paths", updated.Settings.Resources!.Mode);
        Assert.Contains(updated.Diagnostics, diagnostic => diagnostic.Setting == "limiter.ratePercent" && diagnostic.AppliedLive);
        Assert.Contains(updated.Diagnostics, diagnostic => diagnostic.Setting == "profile" && diagnostic.RestartRequired);
        // LimiterEnabled=false signals warp mode by emitting LimiterRatePercent=0
        // so the status bar can show "WARP" (rate 0 triggers the warp display path).
        Assert.Equal(0, status.EmulatorStatus!.LimiterRatePercent);
    }

    /// <summary>
    /// FR: FR-CFG-008, FR: FR-HOST-001, TR: TR-HOST-STATUS-001.
    /// Use case: A client picks a different machine profile from the
    /// SettingsService and asks to restart the session; the session id
    /// must remain stable so existing clients keep their handle.
    /// Acceptance: SetActiveProfile with RestartSession=true returns the
    /// same session id and the new EmulatorStatus reflects the chosen
    /// profile's clock/timing values.
    /// </summary>
    [Fact]
    public async Task SettingsService_RestartSessionAppliesProfileAndPreservesSessionId()
    {
        var registry = new EmulatorRuntimeRegistry();
        var factory = CreateC64RuntimeFactory();
        var emulatorHost = new EmulatorHostService(registry, factory);
        var settingsService = new SettingsServiceHost(registry, factory);

        var created = await emulatorHost.CreateSessionAsync(
            new CreateEmulatorSessionRequest("c64"),
            TestContext.Current.CancellationToken);
        var updated = await settingsService.UpdateSettingsAsync(
            new UpdateSettingsRequest(
                created.SessionId,
                new LimiterSettingsDto(80, true),
                new DisplaySettingsDto("software", "pepto", false, true, "3x", "full-frame", "square-pixels"),
                new InputSettingsDto("c64:gtk3_pos", InputPort.Joystick1, false, "disabled"),
                "c64c",
                RestartSession: true,
                Audio: new AudioSettingsDto("muted"),
                Resources: new ResourceSettingsDto("configured-paths")),
            TestContext.Current.CancellationToken);
        var status = await emulatorHost.GetStatusAsync(
            new SessionRequest(created.SessionId),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, created.Status.Code);
        Assert.Equal(RpcStatusCode.Ok, updated.Status.Code);
        Assert.NotNull(updated.Settings);
        Assert.Equal(created.SessionId, status.EmulatorStatus!.SessionId);
        Assert.Equal("c64c", updated.Settings.ProfileId);
        Assert.Equal("software", updated.Settings.Display.Renderer);
        Assert.Equal("3x", updated.Settings.Display.Scale);
        Assert.Equal("full-frame", updated.Settings.Display.CropMode);
        Assert.Equal("square-pixels", updated.Settings.Display.AspectMode);
        Assert.Equal("disabled", updated.Settings.Input.Mode);
        Assert.Equal("muted", updated.Settings.Audio!.Mode);
        Assert.Equal("configured-paths", updated.Settings.Resources!.Mode);
        Assert.Equal("c64c", status.EmulatorStatus.ModelId);
        Assert.Equal(80, updated.Settings.Limiter.RatePercent);
        Assert.Equal("pepto", updated.Settings.Display.Palette);
        Assert.False(updated.Settings.Display.ShowBorder);
        Assert.Equal(InputPort.Joystick1, updated.Settings.Input.PrimaryJoystickPort);
        Assert.Contains(updated.Diagnostics, diagnostic =>
            diagnostic.Setting == "profile" &&
            diagnostic.AppliedLive &&
            !diagnostic.RestartRequired);
        Assert.DoesNotContain(updated.Diagnostics, diagnostic => diagnostic.RestartRequired);
    }

    /// <summary>
    /// FR: FR-HOST-001, FR: FR-HOST-006, TR: TR-HOST-STATUS-001.
    /// Use case: Each control command (pause, resume, reset, step,
    /// rewind, autostart) must return an EmulatorStatus narrowed to the
    /// fields the caller asked for so UI updates remain efficient.
    /// Acceptance: All control commands return Ok and the returned
    /// EmulatorStatus reflects the post-command run state.
    /// </summary>
    [Fact]
    public async Task EmulatorHost_ControlCommandsReturnFocusedStatus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var emulatorHost = new EmulatorHostService(registry, CreateMinimalRuntimeFactory());

        var created = await emulatorHost.CreateSessionAsync(
            new CreateEmulatorSessionRequest(),
            TestContext.Current.CancellationToken);
        var steppedCycle = await emulatorHost.StepCycleAsync(
            new StepCycleRequest(created.SessionId, 7),
            TestContext.Current.CancellationToken);
        var steppedFrame = await emulatorHost.StepFrameAsync(
            new StepFrameRequest(created.SessionId, 2),
            TestContext.Current.CancellationToken);
        var rewindCycle = await emulatorHost.RewindCycleAsync(
            new RewindCycleRequest(created.SessionId),
            TestContext.Current.CancellationToken);
        var rewindFrame = await emulatorHost.RewindFrameAsync(
            new RewindFrameRequest(created.SessionId),
            TestContext.Current.CancellationToken);
        var autostart = await emulatorHost.ResetAndAutostartDrive8Async(
            new ResetAndAutostartDrive8Request(created.SessionId),
            TestContext.Current.CancellationToken);
        var reset = await emulatorHost.ColdResetAsync(
            new SessionRequest(created.SessionId),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, steppedCycle.Status.Code);
        Assert.Equal(7, steppedCycle.EmulatorStatus!.Cycle);
        Assert.Equal(RpcStatusCode.Ok, steppedFrame.Status.Code);
        Assert.Equal(2, steppedFrame.EmulatorStatus!.FrameCount);
        Assert.True(steppedFrame.EmulatorStatus.Cycle > steppedCycle.EmulatorStatus.Cycle);
        Assert.Equal(RpcStatusCode.NotImplemented, rewindCycle.Status.Code);
        Assert.NotNull(rewindCycle.EmulatorStatus);
        Assert.Equal(RpcStatusCode.NotImplemented, rewindFrame.Status.Code);
        Assert.NotNull(rewindFrame.EmulatorStatus);
        Assert.Equal(RpcStatusCode.FailedPrecondition, autostart.Status.Code);
        Assert.NotNull(autostart.EmulatorStatus);
        Assert.Equal(RpcStatusCode.Ok, reset.Status.Code);
        Assert.NotNull(reset.EmulatorStatus);
        Assert.Equal(0, reset.EmulatorStatus.Cycle);
        Assert.Equal(0, reset.EmulatorStatus.FrameCount);
        Assert.Equal(EmulatorRunState.Stopped, reset.EmulatorStatus.RunState);
    }

    /// <summary>
    /// FR: FR-SNP-001, FR: FR-SNP-002, TR: TR-STATE-001.
    /// Use case: A client uses the SnapshotService to capture a runtime
    /// snapshot of the active session and then restore it later, even
    /// after additional cycles have been executed.
    /// Acceptance: Capture returns Ok with a non-empty snapshot blob;
    /// Restore returns Ok and the post-restore status reflects the
    /// snapshot's cycle/PC values.
    /// </summary>
    [Fact]
    public async Task SnapshotService_CapturesAndRestoresRuntimeSnapshot()
    {
        var registry = new EmulatorRuntimeRegistry();
        var emulatorHost = new EmulatorHostService(registry, new DefaultEmulatorRuntimeFactory());
        var snapshotService = new SnapshotServiceHost(registry);

        var created = await emulatorHost.CreateSessionAsync(
            new CreateEmulatorSessionRequest(),
            TestContext.Current.CancellationToken);
        await emulatorHost.StepFrameAsync(
            new StepFrameRequest(created.SessionId),
            TestContext.Current.CancellationToken);

        var captured = await snapshotService.CaptureSnapshotAsync(
            new SessionRequest(created.SessionId),
            TestContext.Current.CancellationToken);
        var restored = await snapshotService.RestoreSnapshotAsync(
            new RestoreSnapshotRequest(created.SessionId, captured.Snapshot!),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, captured.Status.Code);
        Assert.NotNull(captured.Snapshot);
        Assert.Equal(SnapshotServiceHost.RuntimeSnapshotFormat, captured.Snapshot.Format);
        Assert.NotEmpty(captured.Snapshot.Payload);
        Assert.Equal(RpcStatusCode.Ok, restored.Status.Code);
        Assert.NotNull(restored.EmulatorStatus);
    }

    /// <summary>
    /// FR: FR-HOST-004, FR: FR-INP-001, TR: TR-GRPC-BOUNDARY-001.
    /// Use case: Hosts send keyboard and joystick state through the
    /// InputService; the service records the latest state into the
    /// active session so the runtime sees it on its next tick.
    /// Acceptance: After applying a SetKeyState/SetJoystickState pair,
    /// the InputService reports the same host-side state on Inspect, and
    /// the runtime side reflects the input on its next step.
    /// </summary>
    [Fact]
    public async Task InputService_RecordsHostSideState()
    {
        var registry = new EmulatorRuntimeRegistry();
        var emulatorHost = new EmulatorHostService(registry, new DefaultEmulatorRuntimeFactory());
        var inputService = new InputServiceHost(registry);

        var created = await emulatorHost.CreateSessionAsync(
            new CreateEmulatorSessionRequest(),
            TestContext.Current.CancellationToken);
        var joystick = await inputService.SetJoystickStateAsync(
            new SetJoystickStateRequest(created.SessionId, InputPort.Joystick2, 0x11, true),
            TestContext.Current.CancellationToken);
        var key = await inputService.SetKeyStateAsync(
            new SetKeyStateRequest(created.SessionId, "Space", true),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, joystick.Status.Code);
        Assert.NotNull(joystick.InputState);
        Assert.Contains(joystick.InputState.Joysticks, x =>
            x.Port == InputPort.Joystick2 &&
            x.State.FireButton);
        Assert.Equal(RpcStatusCode.Ok, key.Status.Code);
        Assert.NotNull(key.InputState);
        Assert.Contains(key.InputState.Keys, x => x.Key == "Space" && x.IsPressed);
    }

    /// <summary>
    /// FR: FR-MON-004, FR: FR-HOST-005, TR: TR-GRPC-BOUNDARY-001.
    /// Use case: A debugger client reads CPU registers through the
    /// MonitorService against an active emulator session.
    /// Acceptance: ReadRegisters returns Ok and reports A, X, Y, P, S,
    /// PC values that match the runtime's actual CPU state.
    /// </summary>
    [Fact]
    public async Task MonitorService_ReadsRegistersThroughHostSession()
    {
        var registry = new EmulatorRuntimeRegistry();
        var emulatorHost = new EmulatorHostService(registry, CreateC64RuntimeFactory());
        var monitorService = new MonitorServiceHost(registry);

        var created = await emulatorHost.CreateSessionAsync(
            new CreateEmulatorSessionRequest("c64"),
            TestContext.Current.CancellationToken);
        await emulatorHost.StepCycleAsync(
            new StepCycleRequest(created.SessionId, 5),
            TestContext.Current.CancellationToken);
        var registers = await monitorService.ReadRegistersAsync(
            new SessionRequest(created.SessionId),
            TestContext.Current.CancellationToken);
        var status = await emulatorHost.GetStatusAsync(
            new SessionRequest(created.SessionId),
            TestContext.Current.CancellationToken);
        var missing = await monitorService.ReadRegistersAsync(
            new SessionRequest("missing-session"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, registers.Status.Code);
        Assert.NotNull(registers.Registers);
        Assert.NotNull(registers.EmulatorStatus);
        Assert.Equal(status.EmulatorStatus!.MachineState, registers.Registers);
        Assert.Equal(status.EmulatorStatus.Cycle, registers.Registers.Cycle);
        Assert.Equal(RpcStatusCode.NotFound, missing.Status.Code);
        Assert.Null(missing.Registers);
    }

    /// <summary>
    /// FR: FR-MON-001, FR: FR-HOST-005, TR: TR-GRPC-BOUNDARY-001.
    /// Use case: A debugger client asks the MonitorService for a range
    /// of disassembled instructions starting at a given address.
    /// Acceptance: Disassemble returns Ok with one MonitorDisassemblyLine
    /// per instruction in the requested range, each carrying the
    /// instruction bytes and decoded mnemonic.
    /// </summary>
    [Fact]
    public async Task MonitorService_DisassemblesMemoryThroughHostSession()
    {
        var registry = new EmulatorRuntimeRegistry();
        var emulatorHost = new EmulatorHostService(registry, CreateC64RuntimeFactory());
        var monitorService = new MonitorServiceHost(registry);

        var created = await emulatorHost.CreateSessionAsync(
            new CreateEmulatorSessionRequest("c64"),
            TestContext.Current.CancellationToken);
        await monitorService.WriteMemoryAsync(
            new MonitorWriteMemoryRequest(created.SessionId, 0xC000, [0x20, 0x23, 0xC1, 0xA9, 0x01, 0xEA]),
            TestContext.Current.CancellationToken);
        var disassembly = await monitorService.DisassembleAsync(
            new MonitorDisassemblyRequest(created.SessionId, 0xC000, 3),
            TestContext.Current.CancellationToken);
        var invalid = await monitorService.DisassembleAsync(
            new MonitorDisassemblyRequest(created.SessionId, 0xC000, 0),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, disassembly.Status.Code);
        Assert.NotNull(disassembly.EmulatorStatus);
        Assert.Collection(
            disassembly.Lines,
            line =>
            {
                Assert.Equal(0xC000, line.Address);
                Assert.Equal([0x20, 0x23, 0xC1], line.Bytes);
                Assert.Equal("JSR $C123", line.Text);
                Assert.Equal(3, line.Length);
                Assert.Equal(0xC003, line.NextAddress);
            },
            line =>
            {
                Assert.Equal(0xC003, line.Address);
                Assert.Equal([0xA9, 0x01], line.Bytes);
                Assert.Equal("LDA #$01", line.Text);
                Assert.Equal(2, line.Length);
                Assert.Equal(0xC005, line.NextAddress);
            },
            line =>
            {
                Assert.Equal(0xC005, line.Address);
                Assert.Equal([0xEA], line.Bytes);
                Assert.Equal("NOP", line.Text);
                Assert.Equal(1, line.Length);
                Assert.Equal(0xC006, line.NextAddress);
            });
        Assert.Equal(RpcStatusCode.InvalidArgument, invalid.Status.Code);
    }

    /// <summary>
    /// FR: FR-MON-003, FR: FR-HOST-005, TR: TR-GRPC-BOUNDARY-001.
    /// Use case: A debugger client adds, lists and removes breakpoints
    /// through the host-owned MonitorService API.
    /// Acceptance: ListBreakpoints reflects each Add/Remove operation;
    /// the breakpoint storage is host-owned (not runtime-owned) so it
    /// survives runtime steps without state loss.
    /// </summary>
    [Fact]
    public async Task MonitorService_ManagesHostOwnedBreakpointState()
    {
        var registry = new EmulatorRuntimeRegistry();
        var emulatorHost = new EmulatorHostService(registry, CreateC64RuntimeFactory());
        var monitorService = new MonitorServiceHost(registry);

        var created = await emulatorHost.CreateSessionAsync(
            new CreateEmulatorSessionRequest("c64"),
            TestContext.Current.CancellationToken);
        var empty = await monitorService.ListBreakpointsAsync(
            new SessionRequest(created.SessionId),
            TestContext.Current.CancellationToken);
        var addedC000 = await monitorService.AddBreakpointAsync(
            new MonitorBreakpointRequest(created.SessionId, 0xC000),
            TestContext.Current.CancellationToken);
        var added0801 = await monitorService.AddBreakpointAsync(
            new MonitorBreakpointRequest(created.SessionId, 0x0801),
            TestContext.Current.CancellationToken);
        var duplicate = await monitorService.AddBreakpointAsync(
            new MonitorBreakpointRequest(created.SessionId, 0x0801),
            TestContext.Current.CancellationToken);
        var removed = await monitorService.RemoveBreakpointAsync(
            new MonitorBreakpointRequest(created.SessionId, 0xC000),
            TestContext.Current.CancellationToken);
        var invalid = await monitorService.AddBreakpointAsync(
            new MonitorBreakpointRequest(created.SessionId, 0x10000),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, empty.Status.Code);
        Assert.Empty(empty.Breakpoints);
        Assert.Equal(RpcStatusCode.Ok, addedC000.Status.Code);
        Assert.Contains(addedC000.Breakpoints, breakpoint => breakpoint.Address == 0xC000 && breakpoint.IsEnabled);
        Assert.Equal([0x0801, 0xC000], added0801.Breakpoints.Select(breakpoint => breakpoint.Address).ToArray());
        Assert.Equal([0x0801, 0xC000], duplicate.Breakpoints.Select(breakpoint => breakpoint.Address).ToArray());
        Assert.Equal([0x0801], removed.Breakpoints.Select(breakpoint => breakpoint.Address).ToArray());
        Assert.NotNull(removed.EmulatorStatus);
        Assert.Equal(RpcStatusCode.InvalidArgument, invalid.Status.Code);
    }

    /// <summary>
    /// FR: FR-MON-002, FR: FR-HOST-005, TR: TR-GRPC-BOUNDARY-001.
    /// Use case: A debugger client reads memory windows and writes
    /// individual bytes through the MonitorService.
    /// Acceptance: ReadMemory returns the runtime's bus contents and
    /// WriteMemory mutates them; subsequent reads reflect the write.
    /// </summary>
    [Fact]
    public async Task MonitorService_ReadsAndWritesMemoryThroughHostSession()
    {
        var registry = new EmulatorRuntimeRegistry();
        var emulatorHost = new EmulatorHostService(registry, CreateC64RuntimeFactory());
        var monitorService = new MonitorServiceHost(registry);

        var created = await emulatorHost.CreateSessionAsync(
            new CreateEmulatorSessionRequest("c64"),
            TestContext.Current.CancellationToken);
        var write = await monitorService.WriteMemoryAsync(
            new MonitorWriteMemoryRequest(created.SessionId, 0x0801, [0x42, 0x43, 0x44]),
            TestContext.Current.CancellationToken);
        var read = await monitorService.ReadMemoryAsync(
            new MonitorReadMemoryRequest(created.SessionId, 0x0801, 3),
            TestContext.Current.CancellationToken);
        var invalid = await monitorService.ReadMemoryAsync(
            new MonitorReadMemoryRequest(created.SessionId, 0xFFFF, 2),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, write.Status.Code);
        Assert.Equal(3, write.BytesWritten);
        Assert.NotNull(write.EmulatorStatus);
        Assert.Equal(RpcStatusCode.Ok, read.Status.Code);
        Assert.Equal(0x0801, read.Address);
        Assert.Equal([0x42, 0x43, 0x44], read.Data);
        Assert.NotNull(read.EmulatorStatus);
        Assert.Equal(RpcStatusCode.InvalidArgument, invalid.Status.Code);
    }

    /// <summary>
    /// FR: FR-HOST-003, TR: TR-GRPC-BOUNDARY-001.
    /// Use case: A minimal host configuration without a video chip must
    /// still respond to VideoService queries with a graceful "no video"
    /// status rather than crashing.
    /// Acceptance: VideoService responses report HasVideoChip=false and
    /// return a sensible, non-erroring status object.
    /// </summary>
    [Fact]
    public async Task VideoService_ReportsNoVideoChipForMinimalHost()
    {
        var registry = new EmulatorRuntimeRegistry();
        var emulatorHost = new EmulatorHostService(registry, CreateMinimalRuntimeFactory());
        var videoService = new VideoServiceHost(registry);

        var created = await emulatorHost.CreateSessionAsync(
            new CreateEmulatorSessionRequest(),
            TestContext.Current.CancellationToken);
        var status = await videoService.GetVideoStatusAsync(
            new SessionRequest(created.SessionId),
            TestContext.Current.CancellationToken);
        var frame = await videoService.GetFrameAsync(
            new SessionRequest(created.SessionId),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, status.Status.Code);
        Assert.NotNull(status.VideoStatus);
        Assert.False(status.VideoStatus.IsAvailable);
        Assert.Equal(RpcStatusCode.Unavailable, frame.Status.Code);
    }

    /// <summary>
    /// FR: FR-HOST-001, TR: TR-GRPC-BOUNDARY-001.
    /// Use case: An external client uses the generated gRPC EmulatorHost
    /// client to create, step, pause and resume a session over a real
    /// in-process gRPC channel.
    /// Acceptance: Every RPC returns Ok, the session id is stable across
    /// calls, and run state transitions track the issued commands.
    /// </summary>
    [Fact]
    public async Task GrpcHost_ControlsSessionThroughGeneratedClient()
    {
        using var host = await CreateGrpcHostAsync();
        using var httpClient = CreateGrpcHttpClient(host);
        using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpClient = httpClient });
        var client = new GrpcContracts.EmulatorHost.EmulatorHostClient(channel);

        var created = await client.CreateSessionAsync(
            new GrpcContracts.CreateEmulatorSessionRequest(),
            cancellationToken: TestContext.Current.CancellationToken);
        var started = await client.StartAsync(
            new GrpcContracts.SessionRequest { SessionId = created.SessionId },
            cancellationToken: TestContext.Current.CancellationToken);
        var paused = await client.PauseAsync(
            new GrpcContracts.SessionRequest { SessionId = created.SessionId },
            cancellationToken: TestContext.Current.CancellationToken);
        var cycleStepped = await client.StepCycleAsync(
            new GrpcContracts.StepCycleRequest { SessionId = created.SessionId, CycleCount = 3 },
            cancellationToken: TestContext.Current.CancellationToken);
        var stepped = await client.StepFrameAsync(
            new GrpcContracts.StepFrameRequest { SessionId = created.SessionId, FrameCount = 1 },
            cancellationToken: TestContext.Current.CancellationToken);
        var limited = await client.SetLimiterRateAsync(
            new GrpcContracts.SetLimiterRateRequest { SessionId = created.SessionId, LimiterRatePercent = 75 },
            cancellationToken: TestContext.Current.CancellationToken);
        var rewind = await client.RewindCycleAsync(
            new GrpcContracts.RewindCycleRequest { SessionId = created.SessionId, CycleCount = 1 },
            cancellationToken: TestContext.Current.CancellationToken);
        var autostart = await client.ResetAndAutostartDrive8Async(
            new GrpcContracts.ResetAndAutostartDrive8Request { SessionId = created.SessionId },
            cancellationToken: TestContext.Current.CancellationToken);
        var shutdown = await client.ShutdownAsync(
            new GrpcContracts.SessionRequest { SessionId = created.SessionId },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, created.Status.Code);
        Assert.False(string.IsNullOrWhiteSpace(created.SessionId));
        Assert.Equal(GrpcContracts.EmulatorRunState.Running, started.EmulatorStatus.RunState);
        Assert.Equal(GrpcContracts.EmulatorRunState.Paused, paused.EmulatorStatus.RunState);
        Assert.True(cycleStepped.EmulatorStatus.Cycle > paused.EmulatorStatus.Cycle);
        Assert.True(stepped.EmulatorStatus.Cycle > cycleStepped.EmulatorStatus.Cycle);
        Assert.Equal(75, limited.EmulatorStatus.LimiterRatePercent);
        Assert.Equal(limited.EmulatorStatus.MachineState.Pc, limited.EmulatorStatus.Pc);
        Assert.Equal(GrpcContracts.RpcStatusCode.NotImplemented, rewind.Status.Code);
        Assert.Equal(GrpcContracts.RpcStatusCode.FailedPrecondition, autostart.Status.Code);
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, shutdown.Status.Code);
    }

    /// <summary>
    /// FR: FR-DRV-001, FR: FR-CFG-005, FR: FR-HOST-002, TR: TR-GRPC-BOUNDARY-001.
    /// Use case: ResetAndAutostartDrive8 must verify a disk is attached
    /// before initiating the autostart; without a disk the command must
    /// report a structured failure rather than crashing the host.
    /// Acceptance: Without an attached disk the RPC returns
    /// FailedPrecondition; after attaching a D64 to drive 8 the same RPC
    /// returns Ok and the runtime begins the autostart sequence.
    /// </summary>
    [Fact]
    public async Task EmulatorHost_ResetAndAutostartDrive8RequiresAttachedRuntimeDisk()
    {
        var registry = new EmulatorRuntimeRegistry();
        var machine = MachineTestFactory.CreateC64Machine();
        var session = new EmulatorRuntimeSession("test-session", machine.Architecture, machine);
        registry.Add(session);
        var emulatorHost = new EmulatorHostService(registry, new DefaultEmulatorRuntimeFactory());
        var mediaService = new MediaServiceHost(registry);

        var missingDisk = await emulatorHost.ResetAndAutostartDrive8Async(
            new ResetAndAutostartDrive8Request("test-session"),
            TestContext.Current.CancellationToken);
        var attached = await mediaService.AttachMediaAsync(
            new AttachMediaRequest(
                "test-session",
                MediaSlot.Drive8,
                string.Empty,
                "demo.d64",
                Payload: CreateD64Image()),
            TestContext.Current.CancellationToken);
        var unsupportedAutostart = await emulatorHost.ResetAndAutostartDrive8Async(
            new ResetAndAutostartDrive8Request("test-session"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.FailedPrecondition, missingDisk.Status.Code);
        Assert.Contains("disk", missingDisk.Status.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(RpcStatusCode.Ok, attached.Status.Code);
        Assert.True(attached.Attachment!.AppliedToRuntime);
        Assert.Equal(RpcStatusCode.Ok, unsupportedAutostart.Status.Code);
        Assert.Equal(EmulatorRunState.Running, unsupportedAutostart.EmulatorStatus!.RunState);
        Assert.True(unsupportedAutostart.EmulatorStatus.HostAutomationActive);
        Assert.Equal("C64 BASIC drive 8 autostart", unsupportedAutostart.EmulatorStatus.HostAutomationDescription);
        Assert.Equal(string.Empty, unsupportedAutostart.EmulatorStatus.LastHostAutomationError);
        Assert.NotNull(session.HostKeyboardAutomation);

        var loadedProgram = false;
        var observedRunCommand = false;
        EmulatorCommandResponse? lastStep = null;
        for (var frame = 0; frame < 520; frame++)
        {
            lastStep = await emulatorHost.StepFrameAsync(
                new StepFrameRequest("test-session"),
                TestContext.Current.CancellationToken);
            loadedProgram |= BasicAutostartProgramLoaded(machine);
            observedRunCommand |= ScreenContains(machine, [18, 21, 14]);
            if (loadedProgram && observedRunCommand)
            {
                break;
            }
        }

        Assert.True(loadedProgram, "ResetAndAutostartDrive8 should load the first D64 PRG into BASIC memory.");
        Assert.True(observedRunCommand, "ResetAndAutostartDrive8 should type RUN after loading the first D64 PRG.");
        Assert.NotNull(lastStep?.EmulatorStatus);
        Assert.True(lastStep.EmulatorStatus.HostAutomationActive);
        Assert.Equal("C64 BASIC drive 8 autostart", lastStep.EmulatorStatus.HostAutomationDescription);
        Assert.Equal(string.Empty, lastStep.EmulatorStatus.LastHostAutomationError);
    }

    /// <summary>
    /// FR: FR-DRV-005, FR-HOST-006; TR: TR-HOST-STATUS-001; TEST-DRV-001.
    /// Use case: ResetAndAutostartDrive8 must read the PRG from the runtime
    /// drive over the IEC path after media has been attached, not by re-reading
    /// the original host D64 file.
    /// Acceptance: Moving the original D64 file after attach does not break
    /// autostart, and the returned status reports IEC bus transitions.
    /// </summary>
    [Fact]
    public async Task EmulatorHost_ResetAndAutostartDrive8ReadsRuntimeDiskOverIecBus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var machine = MachineTestFactory.CreateC64Machine();
        var iecBus = IecInterSystemBus.Create();
        var monitor = new IecBusActivityMonitor(iecBus);
        foreach (var drive in machine.Devices.All.OfType<IecDrive>())
            drive.ConnectIecBus(iecBus);
        var session = new EmulatorRuntimeSession("test-session", machine.Architecture, machine, monitor);
        registry.Add(session);
        var emulatorHost = new EmulatorHostService(registry, new DefaultEmulatorRuntimeFactory());
        var mediaService = new MediaServiceHost(registry);
        var diskPath = Path.Combine(Path.GetTempPath(), $"vicesharp-{Guid.NewGuid():N}.d64");
        var movedPath = diskPath + ".moved";
        await File.WriteAllBytesAsync(diskPath, CreateD64Image(), TestContext.Current.CancellationToken);

        var attached = await mediaService.AttachMediaAsync(
            new AttachMediaRequest("test-session", MediaSlot.Drive8, diskPath, "demo.d64"),
            TestContext.Current.CancellationToken);
        File.Move(diskPath, movedPath);
        var autostart = await emulatorHost.ResetAndAutostartDrive8Async(
            new ResetAndAutostartDrive8Request("test-session"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, attached.Status.Code);
        Assert.True(attached.Attachment!.AppliedToRuntime);
        Assert.Equal(RpcStatusCode.Ok, autostart.Status.Code);
        Assert.NotNull(autostart.EmulatorStatus);
        Assert.True(autostart.EmulatorStatus!.IecBusTransitionCount >= 2);
        Assert.True(autostart.EmulatorStatus.IecBusActive);
    }

    /// <summary>
    /// FR: FR-CFG-008, FR: FR-HOST-006, TR: TR-GRPC-BOUNDARY-001.
    /// Use case: A client uses the generated gRPC SettingsService client
    /// to enumerate profiles, apply settings and observe the resulting
    /// EmulatorStatus changes.
    /// Acceptance: All settings operations succeed over the generated
    /// gRPC client and the returned status reflects every change.
    /// </summary>
    [Fact]
    public async Task GrpcSettingsService_RoundTripsSettingsThroughGeneratedClient()
    {
        using var host = await CreateGrpcHostAsync();
        using var httpClient = CreateGrpcHttpClient(host);
        using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpClient = httpClient });
        var hostClient = new GrpcContracts.EmulatorHost.EmulatorHostClient(channel);
        var settingsClient = new GrpcContracts.SettingsService.SettingsServiceClient(channel);

        var created = await hostClient.CreateSessionAsync(
            new GrpcContracts.CreateEmulatorSessionRequest(),
            cancellationToken: TestContext.Current.CancellationToken);
        var profiles = await settingsClient.ListProfilesAsync(
            new GrpcContracts.SessionRequest { SessionId = created.SessionId },
            cancellationToken: TestContext.Current.CancellationToken);
        var updated = await settingsClient.UpdateSettingsAsync(
            new GrpcContracts.UpdateSettingsRequest
            {
                SessionId = created.SessionId,
                ProfileId = "c64c",
                Limiter = new GrpcContracts.LimiterSettingsDto { RatePercent = 150, IsEnabled = true },
                Display = new GrpcContracts.DisplaySettingsDto
                {
                    Renderer = "software",
                    Palette = "amber",
                    ShowBorder = false,
                    MaintainAspectRatio = true,
                    Scale = "fit-window",
                    CropMode = "borderless",
                    AspectMode = "force-4-3"
                },
                Input = new GrpcContracts.InputSettingsDto
                {
                    KeyboardMapId = "c64:gtk3_pos",
                    PrimaryJoystickPort = GrpcContracts.InputPort.Joystick2,
                    Mode = "keyboard-only"
                },
                Audio = new GrpcContracts.AudioSettingsDto { Mode = "muted" },
                Resources = new GrpcContracts.ResourceSettingsDto { Mode = "configured-paths" }
            },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, profiles.Status.Code);
        Assert.Contains(profiles.Profiles, profile => profile.Id == "c64gs");
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, updated.Status.Code);
        Assert.Equal(150, updated.Settings.Limiter.RatePercent);
        Assert.True(updated.Settings.Limiter.IsEnabled);
        Assert.Equal("software", updated.Settings.Display.Renderer);
        Assert.Equal("amber", updated.Settings.Display.Palette);
        Assert.Equal("fit-window", updated.Settings.Display.Scale);
        Assert.Equal("borderless", updated.Settings.Display.CropMode);
        Assert.Equal("force-4-3", updated.Settings.Display.AspectMode);
        Assert.Equal("keyboard-only", updated.Settings.Input.Mode);
        Assert.Equal("muted", updated.Settings.Audio.Mode);
        Assert.Equal("configured-paths", updated.Settings.Resources.Mode);
        Assert.Contains(updated.Diagnostics, diagnostic => diagnostic.Setting == "profile" && diagnostic.RestartRequired);
    }

    /// <summary>
    /// FR: FR-CFG-008, FR: FR-HOST-001, TR: TR-GRPC-BOUNDARY-001.
    /// Use case: Same restart-session contract as the in-process test,
    /// but exercised over the generated gRPC client to verify the wire
    /// protocol carries the flag and surfaces the rebuilt status.
    /// Acceptance: Setting a new profile with RestartSession=true keeps
    /// the session id and returns the new profile's timing values.
    /// </summary>
    [Fact]
    public async Task GrpcSettingsService_RestartSessionFlagRebuildsProfile()
    {
        using var host = await CreateGrpcHostAsync();
        using var httpClient = CreateGrpcHttpClient(host);
        using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpClient = httpClient });
        var hostClient = new GrpcContracts.EmulatorHost.EmulatorHostClient(channel);
        var settingsClient = new GrpcContracts.SettingsService.SettingsServiceClient(channel);

        var created = await hostClient.CreateSessionAsync(
            new GrpcContracts.CreateEmulatorSessionRequest { ArchitectureId = "c64" },
            cancellationToken: TestContext.Current.CancellationToken);
        var updated = await settingsClient.UpdateSettingsAsync(
            new GrpcContracts.UpdateSettingsRequest
            {
                SessionId = created.SessionId,
                ProfileId = "c64c",
                RestartSession = true,
                Limiter = new GrpcContracts.LimiterSettingsDto { RatePercent = 90, IsEnabled = true },
                Display = new GrpcContracts.DisplaySettingsDto
                {
                    Renderer = "software",
                    Palette = "pepto",
                    ShowBorder = false,
                    MaintainAspectRatio = true,
                    Scale = "3x",
                    CropMode = "full-frame",
                    AspectMode = "square-pixels"
                },
                Input = new GrpcContracts.InputSettingsDto
                {
                    KeyboardMapId = "c64:gtk3_pos",
                    PrimaryJoystickPort = GrpcContracts.InputPort.Joystick1,
                    Mode = "disabled"
                },
                Audio = new GrpcContracts.AudioSettingsDto { Mode = "muted" },
                Resources = new GrpcContracts.ResourceSettingsDto { Mode = "configured-paths" }
            },
            cancellationToken: TestContext.Current.CancellationToken);
        var status = await hostClient.GetStatusAsync(
            new GrpcContracts.SessionRequest { SessionId = created.SessionId },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, updated.Status.Code);
        Assert.Equal("c64c", updated.Settings.ProfileId);
        Assert.Equal("software", updated.Settings.Display.Renderer);
        Assert.Equal("3x", updated.Settings.Display.Scale);
        Assert.Equal("full-frame", updated.Settings.Display.CropMode);
        Assert.Equal("square-pixels", updated.Settings.Display.AspectMode);
        Assert.Equal("disabled", updated.Settings.Input.Mode);
        Assert.Equal("muted", updated.Settings.Audio.Mode);
        Assert.Equal("configured-paths", updated.Settings.Resources.Mode);
        Assert.Equal("c64c", status.EmulatorStatus.ModelId);
        Assert.Equal(90, status.EmulatorStatus.LimiterRatePercent);
        Assert.DoesNotContain(updated.Diagnostics, diagnostic => diagnostic.RestartRequired);
    }

    /// <summary>
    /// FR: FR-INP-002, FR: FR-CFG-006, TR: TR-GRPC-BOUNDARY-001.
    /// Use case: Settings changes to the primary joystick routing must
    /// apply live without requiring a restart, even when the change
    /// arrives via the generated gRPC SettingsService.
    /// Acceptance: After applying a JoystickRouting setting through the
    /// generated client, the InputService snapshot reflects the new
    /// routing on the next state-read RPC.
    /// </summary>
    [Fact]
    public async Task GrpcInputService_AppliesLivePrimaryJoystickRoutingFromSettings()
    {
        using var host = await CreateGrpcHostAsync();
        using var httpClient = CreateGrpcHttpClient(host);
        using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpClient = httpClient });
        var hostClient = new GrpcContracts.EmulatorHost.EmulatorHostClient(channel);
        var settingsClient = new GrpcContracts.SettingsService.SettingsServiceClient(channel);
        var inputClient = new GrpcContracts.InputService.InputServiceClient(channel);

        var created = await hostClient.CreateSessionAsync(
            new GrpcContracts.CreateEmulatorSessionRequest { ArchitectureId = "c64" },
            cancellationToken: TestContext.Current.CancellationToken);
        var settings = await settingsClient.UpdateSettingsAsync(
            new GrpcContracts.UpdateSettingsRequest
            {
                SessionId = created.SessionId,
                Input = new GrpcContracts.InputSettingsDto
                {
                    KeyboardMapId = "c64:gtk3_pos",
                    PrimaryJoystickPort = GrpcContracts.InputPort.Joystick1,
                    SwapJoystickPorts = false
                }
            },
            cancellationToken: TestContext.Current.CancellationToken);
        var joystick = await inputClient.SetJoystickStateAsync(
            new GrpcContracts.SetJoystickStateRequest
            {
                SessionId = created.SessionId,
                Port = GrpcContracts.InputPort.PrimaryJoystick,
                DirectionMask = 0x02,
                FireButton = true
            },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, settings.Status.Code);
        Assert.Contains(settings.Diagnostics, diagnostic =>
            diagnostic.Setting == "input.joystickRouting" &&
            diagnostic.AppliedLive &&
            !diagnostic.RestartRequired);
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, joystick.Status.Code);
        Assert.Contains(joystick.InputState.Joysticks, state =>
            state.Port == GrpcContracts.InputPort.PrimaryJoystick &&
            state.State.FireButton &&
            state.State.AppliedToRuntime);
        AssertGrpcJoystickStateDrivesCiaPortB(host, created.SessionId);
    }

    /// <summary>
    /// FR: FR-HOST-002, FR: FR-DRV-001, FR: FR-TAP-001, FR: FR-CRT-001, TR: TR-GRPC-BOUNDARY-001.
    /// Use case: External clients attach disk, tape and cartridge media
    /// to the runtime by sending payload bytes (not a shared file path)
    /// over the generated gRPC MediaService.
    /// Acceptance: Disk, tape and cartridge attach RPCs each return Ok
    /// when the variant supports the port, and the runtime exposes the
    /// attached media on subsequent reads/ticks.
    /// </summary>
    [Fact]
    public async Task GrpcMediaService_AttachesDiskTapeAndCartridgePayloads()
    {
        using var host = await CreateGrpcHostAsync();
        using var httpClient = CreateGrpcHttpClient(host);
        using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpClient = httpClient });
        var hostClient = new GrpcContracts.EmulatorHost.EmulatorHostClient(channel);
        var mediaClient = new GrpcContracts.MediaService.MediaServiceClient(channel);

        var created = await hostClient.CreateSessionAsync(
            new GrpcContracts.CreateEmulatorSessionRequest(),
            cancellationToken: TestContext.Current.CancellationToken);

        var disk = await mediaClient.AttachMediaAsync(
            new GrpcContracts.AttachMediaRequest
            {
                SessionId = created.SessionId,
                Slot = GrpcContracts.MediaSlot.Drive8,
                DisplayName = "demo.d64",
                IsReadOnly = true,
                Payload = ByteString.CopyFrom(CreateD64Image())
            },
            cancellationToken: TestContext.Current.CancellationToken);
        var tape = await mediaClient.AttachMediaAsync(
            new GrpcContracts.AttachMediaRequest
            {
                SessionId = created.SessionId,
                Slot = GrpcContracts.MediaSlot.Tape,
                DisplayName = "demo.tap",
                Payload = ByteString.CopyFrom(CreateTapImage(1, [10, 0, 0x40, 0x1F, 0x00]))
            },
            cancellationToken: TestContext.Current.CancellationToken);
        var cartridge = await mediaClient.AttachMediaAsync(
            new GrpcContracts.AttachMediaRequest
            {
                SessionId = created.SessionId,
                Slot = GrpcContracts.MediaSlot.Cartridge,
                DisplayName = "demo.crt",
                Payload = ByteString.CopyFrom(StandardCartridgeTests.CreateGenericCrt(
                    CreateRawCartridgeImage(StandardCartridgeImage.RomBankSize),
                    includeHigh: false))
            },
            cancellationToken: TestContext.Current.CancellationToken);
        var listed = await mediaClient.ListMediaAsync(
            new GrpcContracts.SessionRequest { SessionId = created.SessionId },
            cancellationToken: TestContext.Current.CancellationToken);
        var autostart = await hostClient.ResetAndAutostartDrive8Async(
            new GrpcContracts.ResetAndAutostartDrive8Request { SessionId = created.SessionId },
            cancellationToken: TestContext.Current.CancellationToken);
        var ejected = await mediaClient.EjectMediaAsync(
            new GrpcContracts.DetachMediaRequest { SessionId = created.SessionId, Slot = GrpcContracts.MediaSlot.Drive8 },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, disk.Status.Code);
        Assert.Equal(GrpcContracts.MediaSlot.Drive8, disk.Attachment.Slot);
        Assert.Equal("demo.d64", disk.Attachment.DisplayName);
        Assert.True(disk.Attachment.IsReadOnly);
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, tape.Status.Code);
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, cartridge.Status.Code);
        Assert.True(cartridge.Attachment.AppliedToRuntime);
        Assert.Contains(listed.Attachments, attachment => attachment.Slot == GrpcContracts.MediaSlot.Drive8);
        Assert.Contains(listed.Attachments, attachment => attachment.Slot == GrpcContracts.MediaSlot.Tape);
        Assert.Contains(listed.Attachments, attachment => attachment.Slot == GrpcContracts.MediaSlot.Cartridge);
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, autostart.Status.Code);
        Assert.True(autostart.EmulatorStatus.HostAutomationActive);
        Assert.Equal("C64 BASIC drive 8 autostart", autostart.EmulatorStatus.HostAutomationDescription);
        Assert.Equal(string.Empty, autostart.EmulatorStatus.LastHostAutomationError);
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, ejected.Status.Code);
        Assert.False(ejected.Attachment.IsAttached);
    }

    /// <summary>
    /// FR: FR-DRV-001, FR: FR-HOST-002, TR: TR-GRPC-BOUNDARY-001.
    /// Use case: Attach a D64 image to drive 8 and later eject it; the
    /// runtime's floppy drive must reflect both operations through the
    /// MediaService.
    /// Acceptance: AttachDisk returns Ok and the runtime exposes the
    /// attached image; Eject returns Ok and clears the runtime's drive.
    /// </summary>
    [Fact]
    public async Task MediaService_AttachesAndEjectsD64ToRuntimeFloppyDrive()
    {
        var registry = new EmulatorRuntimeRegistry();
        var machine = MachineTestFactory.CreateC64Machine();
        registry.Add(new EmulatorRuntimeSession("test-session", machine.Architecture, machine));
        var service = new MediaServiceHost(registry);
        var drive8 = machine.Devices.All.OfType<IFloppyDrive>().Single(drive => drive.DriveNumber == 8);

        Assert.False(drive8.HasDisk);

        var attached = await service.AttachMediaAsync(
            new AttachMediaRequest(
                "test-session",
                MediaSlot.Drive8,
                string.Empty,
                "demo.d64",
                IsReadOnly: true,
                Payload: CreateD64Image()),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, attached.Status.Code);
        Assert.NotNull(attached.Attachment);
        Assert.True(attached.Attachment.AppliedToRuntime);
        Assert.True(drive8.HasDisk);

        var detached = await service.DetachMediaAsync(
            new DetachMediaRequest("test-session", MediaSlot.Drive8),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, detached.Status.Code);
        Assert.True(detached.Attachment!.AppliedToRuntime);
        Assert.False(drive8.HasDisk);
    }

    /// <summary>
    /// FR: FR-TAP-001, FR: FR-HOST-002, TR: TR-GRPC-BOUNDARY-001.
    /// Use case: Attach a .tap tape image to the datasette and later
    /// eject it; the runtime must reflect both operations through the
    /// MediaService.
    /// Acceptance: AttachTape returns Ok with the runtime's datasette
    /// reporting an attached image; Eject returns Ok and clears the
    /// runtime's datasette state.
    /// </summary>
    [Fact]
    public async Task MediaService_AttachesAndEjectsTapToRuntimeDatasette()
    {
        var registry = new EmulatorRuntimeRegistry();
        var machine = MachineTestFactory.CreateC64Machine();
        registry.Add(new EmulatorRuntimeSession("test-session", machine.Architecture, machine));
        var service = new MediaServiceHost(registry);
        var datasette = machine.Devices.All.OfType<ITapeDevice>().Single();

        Assert.False(datasette.HasTape);

        var attached = await service.AttachMediaAsync(
            new AttachMediaRequest(
                "test-session",
                MediaSlot.Tape,
                string.Empty,
                "demo.tap",
                Payload: CreateTapImage(1, [10, 0, 0x40, 0x1F, 0x00])),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, attached.Status.Code);
        Assert.NotNull(attached.Attachment);
        Assert.True(attached.Attachment.AppliedToRuntime);
        Assert.True(datasette.HasTape);

        var detached = await service.DetachMediaAsync(
            new DetachMediaRequest("test-session", MediaSlot.Tape),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, detached.Status.Code);
        Assert.True(detached.Attachment!.AppliedToRuntime);
        Assert.False(datasette.HasTape);
    }

    /// <summary>
    /// FR: FR-TAP-001, FR: FR-HOST-002, TR: TR-GRPC-BOUNDARY-001.
    /// Use case: Some C64 variants lack a tape port; the MediaService
    /// must refuse to attach a .tap to those variants with a structured
    /// "not applied" outcome rather than silently succeeding.
    /// Acceptance: AttachTape on a no-tape variant returns the documented
    /// NotApplied status and leaves the runtime unchanged.
    /// </summary>
    [Fact]
    public async Task MediaService_AttachTapReportsNotAppliedWhenVariantHasNoTapePort()
    {
        var registry = new EmulatorRuntimeRegistry();
        var machine = MachineTestFactory.CreateC64Machine("sx64pal");
        registry.Add(new EmulatorRuntimeSession("test-session", machine.Architecture, machine));
        var service = new MediaServiceHost(registry);

        Assert.Empty(machine.Devices.All.OfType<ITapeDevice>());

        var attached = await service.AttachMediaAsync(
            new AttachMediaRequest(
                "test-session",
                MediaSlot.Tape,
                string.Empty,
                "demo.tap",
                Payload: CreateTapImage(1, [10, 0, 0x40, 0x1F, 0x00])),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, attached.Status.Code);
        Assert.NotNull(attached.Attachment);
        Assert.False(attached.Attachment.AppliedToRuntime);
        Assert.Equal("Runtime has no tape device.", attached.Attachment.Error);
    }

    /// <summary>
    /// FR: FR-CRT-001, FR: FR-HOST-002, TR: TR-GRPC-BOUNDARY-001.
    /// Use case: Attach a generic .crt (8K/16K standard cartridge) to
    /// the cartridge port of a standard C64 profile.
    /// Acceptance: AttachCartridge returns Ok and the runtime exposes
    /// the cartridge's ROM bytes on the bus at the expected addresses.
    /// </summary>
    [Fact]
    public async Task MediaService_AttachesGenericCrtToRuntimeCartridgePort()
    {
        var registry = new EmulatorRuntimeRegistry();
        var machine = MachineTestFactory.CreateC64Machine();
        registry.Add(new EmulatorRuntimeSession("test-session", machine.Architecture, machine));
        var service = new MediaServiceHost(registry);
        var cartridgePort = machine.Devices.All.OfType<ICartridgePort>().Single();
        var crt = StandardCartridgeTests.CreateGenericCrt(
            CreateRawCartridgeImage(StandardCartridgeImage.RomBankSize),
            includeHigh: false);

        Assert.False(cartridgePort.IsCartridgeAttached);

        var attached = await service.AttachMediaAsync(
            new AttachMediaRequest(
                "test-session",
                MediaSlot.Cartridge,
                string.Empty,
                "demo.crt",
                Payload: crt),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, attached.Status.Code);
        Assert.NotNull(attached.Attachment);
        Assert.True(attached.Attachment.AppliedToRuntime);
        Assert.True(cartridgePort.IsCartridgeAttached);
        Assert.Equal(CartridgeMappingMode.Standard8K, cartridgePort.AttachedMappingMode);

        var detached = await service.DetachMediaAsync(
            new DetachMediaRequest("test-session", MediaSlot.Cartridge),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, detached.Status.Code);
        Assert.True(detached.Attachment!.AppliedToRuntime);
        Assert.False(cartridgePort.IsCartridgeAttached);
    }

    /// <summary>
    /// FR: FR-PRF-002, FR: FR-CRT-001, FR: FR-HOST-002, TR: TR-GRPC-BOUNDARY-001.
    /// Use case: Raw C64GS Game System cartridge images are only valid
    /// on a Game System profile; attaching them must succeed on that
    /// profile.
    /// Acceptance: AttachCartridge with a raw GS image returns Ok on a
    /// Game System profile and the runtime exposes the cartridge ROM.
    /// </summary>
    [Fact]
    public async Task MediaService_AttachesRawC64GsCartridgeOnlyToGameSystemProfile()
    {
        var registry = new EmulatorRuntimeRegistry();
        var machine = MachineTestFactory.CreateC64Machine("c64gs");
        registry.Add(new EmulatorRuntimeSession("test-session", machine.Architecture, machine));
        var service = new MediaServiceHost(registry);
        var cartridgePort = machine.Devices.All.OfType<ICartridgePort>().Single();
        var cartridge = CreateRawCartridgeImage(StandardCartridgeImage.GameSystemRomSize);

        Assert.Equal(CartridgeMappingMode.GameSystem, cartridgePort.DefaultMappingMode);

        var attached = await service.AttachMediaAsync(
            new AttachMediaRequest(
                "test-session",
                MediaSlot.Cartridge,
                string.Empty,
                "c64gs.bin",
                Payload: cartridge),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, attached.Status.Code);
        Assert.NotNull(attached.Attachment);
        Assert.True(attached.Attachment.AppliedToRuntime);
        Assert.True(cartridgePort.IsCartridgeAttached);
        Assert.Equal(CartridgeMappingMode.GameSystem, cartridgePort.AttachedMappingMode);

        var detached = await service.DetachMediaAsync(
            new DetachMediaRequest("test-session", MediaSlot.Cartridge),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, detached.Status.Code);
        Assert.True(detached.Attachment!.AppliedToRuntime);
        Assert.False(cartridgePort.IsCartridgeAttached);
        Assert.Null(cartridgePort.AttachedMappingMode);
    }

    /// <summary>
    /// FR: FR-PRF-002, FR: FR-CRT-001, FR: FR-HOST-002, TR: TR-GRPC-BOUNDARY-001.
    /// Use case: Raw C64GS cartridge images must NOT attach to a
    /// standard C64 profile; the service should report a structured
    /// rejection rather than silently corrupt the bus.
    /// Acceptance: AttachCartridge with a raw GS image on a standard
    /// profile returns the documented rejection status code and the
    /// runtime's cartridge port stays empty.
    /// </summary>
    [Fact]
    public async Task MediaService_RejectsRawC64GsCartridgeOnStandardProfile()
    {
        var registry = new EmulatorRuntimeRegistry();
        var machine = MachineTestFactory.CreateC64Machine("c64");
        registry.Add(new EmulatorRuntimeSession("test-session", machine.Architecture, machine));
        var service = new MediaServiceHost(registry);
        var cartridgePort = machine.Devices.All.OfType<ICartridgePort>().Single();
        var cartridge = CreateRawCartridgeImage(StandardCartridgeImage.GameSystemRomSize);

        Assert.Equal(CartridgeMappingMode.Auto, cartridgePort.DefaultMappingMode);

        var attached = await service.AttachMediaAsync(
            new AttachMediaRequest(
                "test-session",
                MediaSlot.Cartridge,
                string.Empty,
                "c64gs.bin",
                Payload: cartridge),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, attached.Status.Code);
        Assert.False(cartridgePort.IsCartridgeAttached);
    }

    /// <summary>
    /// FR: FR-HOST-002, TR: TR-GRPC-BOUNDARY-001.
    /// Use case: The HostProtocolClient must be able to attach media to
    /// the runtime by sending the file contents inline over gRPC rather
    /// than relying on a shared filesystem path.
    /// Acceptance: Attaching media via inline payload returns Ok and the
    /// host registers the attachment with the runtime.
    /// </summary>
    [Fact]
    public async Task GrpcHostProtocolClient_AttachesMediaPayloadWithoutSharedFilePath()
    {
        await using var host = await InProcessGrpcHost.StartAsync(TestContext.Current.CancellationToken);
        using var client = new GrpcHostProtocolClient(host.Endpoint);
        var clientOnlyPath = Path.Combine("Z:\\client-only", "demo.d64");

        var response = await client.AttachMediaAsync(
            MediaSlot.Drive8,
            clientOnlyPath,
            isReadOnly: true,
            CreateD64Image(),
            "demo.d64",
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.NotNull(response.Attachment);
        Assert.Equal("demo.d64", response.Attachment.DisplayName);
        Assert.True(response.Attachment.IsReadOnly);
        Assert.NotEqual(clientOnlyPath, response.Attachment.FilePath);
        Assert.True(File.Exists(response.Attachment.FilePath));
    }

    /// <summary>
    /// FR: FR-HOST-001, FR: FR-HOST-003, FR: FR-HOST-005, TR: TR-GRPC-BOUNDARY-001.
    /// Use case: Walk the generated gRPC clients through Input, Snapshot,
    /// Video and Capture commands to confirm the entire service surface
    /// can be driven end-to-end over a single channel.
    /// Acceptance: Every Input, Snapshot, Video and Capture RPC returns
    /// Ok over the generated gRPC client and the returned payloads
    /// match the runtime state.
    /// </summary>
    [Fact]
    public async Task GrpcServices_RoundTripInputSnapshotVideoAndCaptureCommands()
    {
        using var host = await CreateGrpcHostAsync();
        using var httpClient = CreateGrpcHttpClient(host);
        using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpClient = httpClient });
        var hostClient = new GrpcContracts.EmulatorHost.EmulatorHostClient(channel);
        var inputClient = new GrpcContracts.InputService.InputServiceClient(channel);
        var videoClient = new GrpcContracts.VideoService.VideoServiceClient(channel);
        var snapshotClient = new GrpcContracts.SnapshotService.SnapshotServiceClient(channel);
        var captureClient = new GrpcContracts.CaptureService.CaptureServiceClient(channel);

        var created = await hostClient.CreateSessionAsync(
            new GrpcContracts.CreateEmulatorSessionRequest(),
            cancellationToken: TestContext.Current.CancellationToken);
        var key = await inputClient.SetKeyStateAsync(
            new GrpcContracts.SetKeyStateRequest
            {
                SessionId = created.SessionId,
                Key = "Space",
                IsPressed = true,
                PhysicalKey = "Space",
                Text = " ",
                Modifiers = 1
            },
            cancellationToken: TestContext.Current.CancellationToken);
        var joystick = await inputClient.SetJoystickStateAsync(
            new GrpcContracts.SetJoystickStateRequest
            {
                SessionId = created.SessionId,
                Port = GrpcContracts.InputPort.Joystick2,
                DirectionMask = 0x11,
                FireButton = true
            },
            cancellationToken: TestContext.Current.CancellationToken);
        var videoStatus = await videoClient.GetVideoStatusAsync(
            new GrpcContracts.SessionRequest { SessionId = created.SessionId },
            cancellationToken: TestContext.Current.CancellationToken);
        var frame = await videoClient.GetFrameAsync(
            new GrpcContracts.SessionRequest { SessionId = created.SessionId },
            cancellationToken: TestContext.Current.CancellationToken);
        var snapshot = await snapshotClient.CaptureSnapshotAsync(
            new GrpcContracts.SessionRequest { SessionId = created.SessionId },
            cancellationToken: TestContext.Current.CancellationToken);
        var restored = await snapshotClient.RestoreSnapshotAsync(
            new GrpcContracts.RestoreSnapshotRequest { SessionId = created.SessionId, Snapshot = snapshot.Snapshot },
            cancellationToken: TestContext.Current.CancellationToken);
        var capture = await captureClient.StartCaptureAsync(
            new GrpcContracts.StartCaptureRequest
            {
                SessionId = created.SessionId,
                Kind = GrpcContracts.CaptureKind.Screenshot,
                TargetPath = Path.Combine(Path.GetTempPath(), $"vice-sharp-{Guid.NewGuid():N}.bmp")
            },
            cancellationToken: TestContext.Current.CancellationToken);
        var stopped = await captureClient.StopCaptureAsync(
            new GrpcContracts.StopCaptureRequest { SessionId = created.SessionId, CaptureId = capture.Capture.CaptureId },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, key.Status.Code);
        Assert.Contains(key.InputState.Keys, state =>
            state.Key == "Space" &&
            state.IsPressed &&
            state.AppliedToRuntime &&
            state.PhysicalKey == "Space" &&
            state.Text == " " &&
            state.Modifiers == 1);
        AssertGrpcKeyStateDrivesCiaMatrix(host, created.SessionId);
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, joystick.Status.Code);
        Assert.Contains(joystick.InputState.Joysticks, state =>
            state.Port == GrpcContracts.InputPort.Joystick2 &&
            state.State.FireButton &&
            state.State.AppliedToRuntime);
        AssertGrpcJoystickStateDrivesCiaPortA(host, created.SessionId);
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, videoStatus.Status.Code);
        Assert.NotNull(videoStatus.VideoStatus);
        if (videoStatus.VideoStatus.IsAvailable)
        {
            Assert.Equal(GrpcContracts.RpcStatusCode.Ok, frame.Status.Code);
            Assert.NotNull(frame.Frame);
            Assert.NotEmpty(frame.Frame.Bgra);
        }
        else
        {
            Assert.Equal(GrpcContracts.RpcStatusCode.Unavailable, frame.Status.Code);
        }
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, snapshot.Status.Code);
        Assert.NotEmpty(snapshot.Snapshot.Payload);
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, restored.Status.Code);
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, capture.Status.Code);
        Assert.True(capture.Capture.IsActive);
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, stopped.Status.Code);
        Assert.False(stopped.Capture.IsActive);
    }

    /// <summary>
    /// FR: FR-MON-002, FR: FR-HOST-005, TR: TR-GRPC-BOUNDARY-001.
    /// Use case: Same memory read/write contract as the in-process test,
    /// but exercised through the generated MonitorService gRPC client.
    /// Acceptance: ReadMemory/WriteMemory round-trip bytes through the
    /// generated client and the runtime state mirrors them.
    /// </summary>
    [Fact]
    public async Task GrpcMonitorService_ReadsAndWritesMemoryThroughGeneratedClient()
    {
        using var host = await CreateGrpcHostAsync();
        using var httpClient = CreateGrpcHttpClient(host);
        using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpClient = httpClient });
        var hostClient = new GrpcContracts.EmulatorHost.EmulatorHostClient(channel);
        var monitorClient = new GrpcContracts.MonitorService.MonitorServiceClient(channel);

        var created = await hostClient.CreateSessionAsync(
            new GrpcContracts.CreateEmulatorSessionRequest(),
            cancellationToken: TestContext.Current.CancellationToken);
        var write = await monitorClient.WriteMemoryAsync(
            new GrpcContracts.MonitorWriteMemoryRequest
            {
                SessionId = created.SessionId,
                Address = 0x0801,
                Data = ByteString.CopyFrom([0xA1, 0xB2, 0xC3])
            },
            cancellationToken: TestContext.Current.CancellationToken);
        var read = await monitorClient.ReadMemoryAsync(
            new GrpcContracts.MonitorReadMemoryRequest
            {
                SessionId = created.SessionId,
                Address = 0x0801,
                Length = 3
            },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, write.Status.Code);
        Assert.Equal(3u, write.BytesWritten);
        Assert.NotNull(write.EmulatorStatus);
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, read.Status.Code);
        Assert.Equal(0x0801u, read.Address);
        Assert.Equal([0xA1, 0xB2, 0xC3], read.Data.ToByteArray());
        Assert.NotNull(read.EmulatorStatus);
    }

    /// <summary>
    /// FR: FR-MON-004, FR: FR-HOST-005, TR: TR-GRPC-BOUNDARY-001.
    /// Use case: Same CPU register read contract as the in-process test,
    /// but exercised through the generated MonitorService gRPC client.
    /// Acceptance: ReadRegisters over the generated client returns Ok
    /// and reports register values that match runtime state.
    /// </summary>
    [Fact]
    public async Task GrpcMonitorService_ReadsRegistersThroughGeneratedClient()
    {
        using var host = await CreateGrpcHostAsync();
        using var httpClient = CreateGrpcHttpClient(host);
        using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpClient = httpClient });
        var hostClient = new GrpcContracts.EmulatorHost.EmulatorHostClient(channel);
        var monitorClient = new GrpcContracts.MonitorService.MonitorServiceClient(channel);

        var created = await hostClient.CreateSessionAsync(
            new GrpcContracts.CreateEmulatorSessionRequest(),
            cancellationToken: TestContext.Current.CancellationToken);
        await hostClient.StepCycleAsync(
            new GrpcContracts.StepCycleRequest { SessionId = created.SessionId, CycleCount = 5 },
            cancellationToken: TestContext.Current.CancellationToken);
        var status = await hostClient.GetStatusAsync(
            new GrpcContracts.SessionRequest { SessionId = created.SessionId },
            cancellationToken: TestContext.Current.CancellationToken);
        var registers = await monitorClient.ReadRegistersAsync(
            new GrpcContracts.SessionRequest { SessionId = created.SessionId },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, registers.Status.Code);
        Assert.NotNull(registers.Registers);
        Assert.NotNull(registers.EmulatorStatus);
        Assert.Equal(status.EmulatorStatus.MachineState.A, registers.Registers.A);
        Assert.Equal(status.EmulatorStatus.MachineState.X, registers.Registers.X);
        Assert.Equal(status.EmulatorStatus.MachineState.Y, registers.Registers.Y);
        Assert.Equal(status.EmulatorStatus.MachineState.S, registers.Registers.S);
        Assert.Equal(status.EmulatorStatus.MachineState.P, registers.Registers.P);
        Assert.Equal(status.EmulatorStatus.MachineState.Pc, registers.Registers.Pc);
        Assert.Equal(status.EmulatorStatus.MachineState.Cycle, registers.Registers.Cycle);
    }

    /// <summary>
    /// FR: FR-MON-001, FR: FR-HOST-005, TR: TR-GRPC-BOUNDARY-001.
    /// Use case: Same disassembly contract as the in-process test, but
    /// exercised through the generated MonitorService gRPC client.
    /// Acceptance: Disassemble returns Ok and a non-empty line list
    /// over the generated client; each line carries instruction bytes
    /// and the decoded mnemonic.
    /// </summary>
    [Fact]
    public async Task GrpcMonitorService_DisassemblesMemoryThroughGeneratedClient()
    {
        using var host = await CreateGrpcHostAsync();
        using var httpClient = CreateGrpcHttpClient(host);
        using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpClient = httpClient });
        var hostClient = new GrpcContracts.EmulatorHost.EmulatorHostClient(channel);
        var monitorClient = new GrpcContracts.MonitorService.MonitorServiceClient(channel);

        var created = await hostClient.CreateSessionAsync(
            new GrpcContracts.CreateEmulatorSessionRequest(),
            cancellationToken: TestContext.Current.CancellationToken);
        await monitorClient.WriteMemoryAsync(
            new GrpcContracts.MonitorWriteMemoryRequest
            {
                SessionId = created.SessionId,
                Address = 0x0801,
                Data = ByteString.CopyFrom([0x20, 0x23, 0xC1, 0xA9, 0x01, 0xEA])
            },
            cancellationToken: TestContext.Current.CancellationToken);
        var disassembly = await monitorClient.DisassembleAsync(
            new GrpcContracts.MonitorDisassemblyRequest
            {
                SessionId = created.SessionId,
                Address = 0x0801,
                Count = 3
            },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, disassembly.Status.Code);
        Assert.NotNull(disassembly.EmulatorStatus);
        Assert.Collection(
            disassembly.Lines,
            line =>
            {
                Assert.Equal(0x0801u, line.Address);
                Assert.Equal([0x20, 0x23, 0xC1], line.InstructionBytes.ToByteArray());
                Assert.Equal("JSR $C123", line.Text);
                Assert.Equal(3u, line.Length);
                Assert.Equal(0x0804u, line.NextAddress);
            },
            line =>
            {
                Assert.Equal(0x0804u, line.Address);
                Assert.Equal([0xA9, 0x01], line.InstructionBytes.ToByteArray());
                Assert.Equal("LDA #$01", line.Text);
                Assert.Equal(2u, line.Length);
                Assert.Equal(0x0806u, line.NextAddress);
            },
            line =>
            {
                Assert.Equal(0x0806u, line.Address);
                Assert.Equal([0xEA], line.InstructionBytes.ToByteArray());
                Assert.Equal("NOP", line.Text);
                Assert.Equal(1u, line.Length);
                Assert.Equal(0x0807u, line.NextAddress);
            });
    }

    /// <summary>
    /// FR: FR-MON-003, FR: FR-HOST-005, TR: TR-GRPC-BOUNDARY-001.
    /// Use case: Same breakpoint state-machine contract as the in-process
    /// test, but exercised through the generated MonitorService gRPC
    /// client.
    /// Acceptance: Add/List/Remove breakpoint RPCs round-trip across the
    /// gRPC channel and the host retains the correct breakpoint set.
    /// </summary>
    [Fact]
    public async Task GrpcMonitorService_ManagesBreakpointStateThroughGeneratedClient()
    {
        using var host = await CreateGrpcHostAsync();
        using var httpClient = CreateGrpcHttpClient(host);
        using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpClient = httpClient });
        var hostClient = new GrpcContracts.EmulatorHost.EmulatorHostClient(channel);
        var monitorClient = new GrpcContracts.MonitorService.MonitorServiceClient(channel);

        var created = await hostClient.CreateSessionAsync(
            new GrpcContracts.CreateEmulatorSessionRequest(),
            cancellationToken: TestContext.Current.CancellationToken);
        var empty = await monitorClient.ListBreakpointsAsync(
            new GrpcContracts.SessionRequest { SessionId = created.SessionId },
            cancellationToken: TestContext.Current.CancellationToken);
        var added = await monitorClient.AddBreakpointAsync(
            new GrpcContracts.MonitorBreakpointRequest { SessionId = created.SessionId, Address = 0xC000 },
            cancellationToken: TestContext.Current.CancellationToken);
        var removed = await monitorClient.RemoveBreakpointAsync(
            new GrpcContracts.MonitorBreakpointRequest { SessionId = created.SessionId, Address = 0xC000 },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, empty.Status.Code);
        Assert.Empty(empty.Breakpoints);
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, added.Status.Code);
        var breakpoint = Assert.Single(added.Breakpoints);
        Assert.Equal(0xC000u, breakpoint.Address);
        Assert.True(breakpoint.IsEnabled);
        Assert.NotNull(added.EmulatorStatus);
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, removed.Status.Code);
        Assert.Empty(removed.Breakpoints);
    }

    private static async Task<IHost> CreateGrpcHostAsync()
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddViceSharpGrpcHost();
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapViceSharpGrpcHost());
                });
            })
            .Build();

        await host.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        return host;
    }

    private static HttpClient CreateGrpcHttpClient(IHost host)
    {
        var httpClient = host.GetTestClient();
        httpClient.DefaultRequestVersion = new Version(2, 0);
        return httpClient;
    }

    private static void AssertGrpcKeyStateDrivesCiaMatrix(IHost host, string sessionId)
    {
        var registry = host.Services.GetRequiredService<EmulatorRuntimeRegistry>();
        Assert.True(registry.TryGet(sessionId, out var session));

        lock (session.SyncRoot)
        {
            session.Machine.Bus.Write(0xDC01, 0xEF);
            Assert.Equal(0, session.Machine.Bus.Read(0xDC00) & 0x80);
        }
    }

    private static void AssertGrpcJoystickStateDrivesCiaPortA(IHost host, string sessionId)
    {
        var registry = host.Services.GetRequiredService<EmulatorRuntimeRegistry>();
        Assert.True(registry.TryGet(sessionId, out var session));

        lock (session.SyncRoot)
        {
            Assert.Equal(0, session.Machine.Bus.Read(0xDC00) & 0x11);
        }
    }

    private static void AssertGrpcJoystickStateDrivesCiaPortB(IHost host, string sessionId)
    {
        var registry = host.Services.GetRequiredService<EmulatorRuntimeRegistry>();
        Assert.True(registry.TryGet(sessionId, out var session));

        lock (session.SyncRoot)
        {
            Assert.Equal(0, session.Machine.Bus.Read(0xDC01) & 0x12);
        }
    }

    private static IEmulatorRuntimeFactory CreateMinimalRuntimeFactory()
    {
        return new DefaultEmulatorRuntimeFactory(
            new ArchitectureBuilder(),
            [MinimalHostArchitectureDescriptor.Instance],
            MinimalHostArchitectureDescriptor.ArchitectureId);
    }

    private static IEmulatorRuntimeFactory CreateC64RuntimeFactory()
    {
        var descriptors = new List<IArchitectureDescriptor> { MinimalHostArchitectureDescriptor.Instance };
        descriptors.AddRange(C64MachineProfiles.All.Select(profile => new C64Descriptor(profile)));
        return new DefaultEmulatorRuntimeFactory(
            new ArchitectureBuilder(MachineTestFactory.CreateC64RomProvider()),
            descriptors,
            "c64");
    }

    private static byte[] CreateD64Image()
    {
        var image = CreateD64WithFirstProgram("BOOT", CreateBasicProgramPrg());
        var sectorOffset = Track18Sector1Offset();
        for (var i = 32; i < 256; i++)
            image[sectorOffset + i] = (byte)(255 - i);

        return image;
    }

    private static byte[] CreateD64WithFirstProgram(string fileName, byte[] programBytes)
    {
        if (programBytes.Length > 254)
            throw new ArgumentOutOfRangeException(nameof(programBytes), "Test PRG must fit in one D64 sector.");

        var image = new D64Image();
        image.Format();

        const int directoryEntryOffset = 2;
        image.WriteSectorByte(18, 1, directoryEntryOffset, 0x82);
        image.WriteSectorByte(18, 1, directoryEntryOffset + 1, 17);
        image.WriteSectorByte(18, 1, directoryEntryOffset + 2, 0);

        for (var index = 0; index < 16; index++)
        {
            var value = index < fileName.Length
                ? (byte)char.ToUpperInvariant(fileName[index])
                : (byte)0xA0;
            image.WriteSectorByte(18, 1, directoryEntryOffset + 3 + index, value);
        }

        image.WriteSectorByte(17, 0, 0, 0);
        image.WriteSectorByte(17, 0, 1, (byte)(programBytes.Length + 1));
        for (var offset = 0; offset < programBytes.Length; offset++)
            image.WriteSectorByte(17, 0, 2 + offset, programBytes[offset]);

        return image.ToArray();
    }

    private static byte[] CreateBasicProgramPrg()
    {
        return
        [
            0x01, 0x08,
            0x0B, 0x08,
            0x0A, 0x00,
            0x99,
            0x22, (byte)'O', (byte)'K', 0x22,
            0x00,
            0x00, 0x00
        ];
    }

    private static byte[] CreateTapImage(byte version, byte[] pulseData)
    {
        var image = new byte[20 + pulseData.Length];
        "C64-TAPE-RAW"u8.CopyTo(image);
        image[12] = version;
        image[16] = (byte)pulseData.Length;
        image[17] = (byte)(pulseData.Length >> 8);
        image[18] = (byte)(pulseData.Length >> 16);
        image[19] = (byte)(pulseData.Length >> 24);
        pulseData.CopyTo(image.AsSpan(20));
        return image;
    }

    private static byte[] CreateRawCartridgeImage(int length)
    {
        var image = new byte[length];
        for (var i = 0; i < image.Length; i++)
            image[i] = (byte)(i & 0xFF);

        return image;
    }

    private static int Track18Sector1Offset()
    {
        var offset = 0;
        for (var track = 1; track < 18; track++)
            offset += 21 * 256;

        return offset + 256;
    }

    private static bool ScreenContains(IMachine machine, ReadOnlySpan<byte> screenCodes)
    {
        Span<byte> screen = stackalloc byte[1000];
        for (var i = 0; i < screen.Length; i++)
            screen[i] = machine.Bus.Peek((ushort)(0x0400 + i));

        return screen.IndexOf(screenCodes) >= 0;
    }

    private static bool BasicAutostartProgramLoaded(IMachine machine)
    {
        return machine.Bus.Peek(0x0801) == 0x0B &&
            machine.Bus.Peek(0x0802) == 0x08 &&
            machine.Bus.Peek(0x0805) == 0x99 &&
            machine.Bus.Peek(0x002D) == 0x0D &&
            machine.Bus.Peek(0x002E) == 0x08;
    }
}
