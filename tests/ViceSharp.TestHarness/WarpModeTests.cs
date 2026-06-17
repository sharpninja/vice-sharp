namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

public sealed class WarpModeTests
{
    /// <summary>
    /// FR: FR-WARP-001, TR: TR-WARP-STATUS-001.
    /// Use case: The status bar renders "Limiter N%" when the speed limiter is on.
    /// Acceptance: ToStatusDto emits the actual LimiterRatePercent value when
    ///   LimiterEnabled = true, preserving the existing behaviour unchanged.
    /// </summary>
    [Fact]
    public void ToStatusDto_WhenLimiterEnabled_EmitsActualRatePercent()
    {
        var session = CreateMinimalSession();
        session.LimiterEnabled = true;
        session.LimiterRatePercent = 75;

        var dto = HostProtocolMapper.ToStatusDto(session);

        Assert.Equal(75, dto.LimiterRatePercent);
    }

    /// <summary>
    /// FR: FR-WARP-001, TR: TR-WARP-STATUS-001.
    /// Use case: The status bar must display "WARP" when warp mode is active.
    ///   MainWindow.ApplyStatus() shows WARP when LimiterRatePercent is 0 or >= 1000.
    /// Acceptance: ToStatusDto emits LimiterRatePercent = 0 when LimiterEnabled = false,
    ///   regardless of the stored rate, so the status bar can detect the warp signal.
    /// </summary>
    [Fact]
    public void ToStatusDto_WhenLimiterDisabled_EmitsZeroForWarpSignal()
    {
        var session = CreateMinimalSession();
        session.LimiterEnabled = false;
        session.LimiterRatePercent = 100;

        var dto = HostProtocolMapper.ToStatusDto(session);

        Assert.Equal(0, dto.LimiterRatePercent);
    }

    /// <summary>
    /// FR: FR-WARP-001 / BUG-THROTTLE-001, TR: TR-WARP-FRAMES-001.
    /// Use case: Normal (non-warp) operation: the emulation pump advances exactly
    ///   one frame per tick. (Frame advancement moved off GetFrameAsync to the
    ///   host worker per docs/Decoupling.md; GetFrameAsync is now a pure pull.)
    /// Acceptance: A single PumpSession call with LimiterEnabled = true sets
    ///   FrameCount to 1.
    /// </summary>
    [Fact]
    public async Task PumpSession_WhenLimiterEnabled_RunsExactlyOneFrame()
    {
        var (registry, sessionId) = await CreateRunningSessionAsync();
        var pump = new EmulationPumpService(registry);

        registry.TryGet(sessionId, out var session);
        session!.LimiterEnabled = true;

        pump.PumpSession(session);

        Assert.Equal(1, session.FrameCount);
    }

    /// <summary>
    /// FR: FR-WARP-001 / BUG-THROTTLE-001, TR: TR-WARP-FRAMES-001.
    /// Use case: Warp mode must run as many emulation frames as possible within
    ///   each tick so effective emulation speed exceeds 100%.
    /// Acceptance: A single PumpSession call with LimiterEnabled = false advances
    ///   more than one frame (the warp burst).
    /// Uses WarpFastMachine (no-op RunFrame) to remove Debug-build timing sensitivity.
    /// </summary>
    [Fact]
    public void PumpSession_WhenLimiterDisabled_RunsMultipleFrames()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = new EmulatorRuntimeSession(
            "warp-fast-test",
            MinimalHostArchitectureDescriptor.Instance,
            new WarpFastMachine());
        session.RunState = EmulatorRunState.Running;
        session.LimiterEnabled = false;
        registry.Add(session);

        var pump = new EmulationPumpService(registry);
        pump.PumpSession(session);

        Assert.True(session.FrameCount > 1,
            $"Expected multiple frames in warp mode but got {session.FrameCount}.");
    }

    private static EmulatorRuntimeSession CreateMinimalSession()
    {
        var factory = new DefaultEmulatorRuntimeFactory(
            new ArchitectureBuilder(),
            [MinimalHostArchitectureDescriptor.Instance],
            MinimalHostArchitectureDescriptor.ArchitectureId);
        return factory.Create(new CreateEmulatorSessionRequest());
    }

    private static async Task<(EmulatorRuntimeRegistry Registry, string SessionId)> CreateRunningSessionAsync()
    {
        var registry = new EmulatorRuntimeRegistry();
        var factory = new DefaultEmulatorRuntimeFactory(
            new ArchitectureBuilder(),
            [MinimalHostArchitectureDescriptor.Instance],
            MinimalHostArchitectureDescriptor.ArchitectureId);
        var emulatorHost = new EmulatorHostService(registry, factory);

        var created = await emulatorHost.CreateSessionAsync(
            new CreateEmulatorSessionRequest(),
            TestContext.Current.CancellationToken);
        await emulatorHost.ResumeAsync(
            new SessionRequest(created.SessionId),
            TestContext.Current.CancellationToken);

        return (registry, created.SessionId);
    }

    // No-op machine: RunFrame() returns instantly so the 20ms warp budget
    // runs thousands of iterations regardless of Debug/Release build timing.
    private sealed class WarpFastMachine : IMachine
    {
        public IBus Bus => throw new NotSupportedException();
        public IClock Clock => throw new NotSupportedException();
        public IDeviceRegistry Devices { get; } = new WarpFastDeviceRegistry();
        public IArchitectureDescriptor Architecture => MinimalHostArchitectureDescriptor.Instance;
        public void RunFrame() { }
        public void StepInstruction() { }
        public MachineState GetState() => new();
        public void Reset() { }
    }

    private sealed class WarpFastDeviceRegistry : IDeviceRegistry
    {
        public IDevice? GetById(DeviceId id) => null;
        public IDevice? GetByRole(DeviceRole role) => null;
        public IReadOnlyList<T> GetAll<T>() where T : IDevice => [];
        public IReadOnlyList<IDevice> All => [];
        public int Count => 0;
    }
}
