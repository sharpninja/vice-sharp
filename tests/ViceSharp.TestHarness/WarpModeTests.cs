namespace ViceSharp.TestHarness;

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
    /// FR: FR-WARP-001 / BUG-THROTTLE-001, TR: TR-CYCLE-PACE-001.
    /// Use case: Timing is driven by the emulated CPU clock, not the video frame
    ///   rate. With the limiter on, one pump tick advances the master clock by a
    ///   single sub-frame cycle slice (so the worker can pace at ~1 kHz against
    ///   real time), never a whole video frame.
    /// Acceptance: A single PumpSession call advances the clock by a positive
    ///   number of master cycles that is a small fraction of a second (well under
    ///   one video frame's worth), and TotalCycles moves by exactly that amount.
    /// </summary>
    [Fact]
    public async Task PumpSession_WhenLimiterEnabled_AdvancesCpuClockBySubFrameSlice()
    {
        var (registry, sessionId) = await CreateRunningSessionAsync();
        var pump = new EmulationPumpService(registry);

        registry.TryGet(sessionId, out var session);
        session!.LimiterEnabled = true;

        var clock = session.Machine.Clock;
        var before = clock.TotalCycles;
        var advanced = pump.PumpSession(session);

        Assert.True(advanced > 0, "pump advanced no cycles");
        Assert.Equal(advanced, clock.TotalCycles - before);
        // A slice is ~1 ms of emulated time: far less than one frame (~19656 cycles
        // / ~20 ms). Bound it under 100 ms of cycles to prove sub-frame pacing.
        Assert.True(advanced <= clock.FrequencyHz / 10,
            $"Expected a sub-frame cycle slice but advanced {advanced} cycles.");
    }

    /// <summary>
    /// FR: FR-WARP-001 / BUG-THROTTLE-001, TR: TR-CYCLE-PACE-001.
    /// Use case: Warp (limiter off) runs the CPU clock flat out, advancing a larger
    ///   cycle burst per tick than the paced slice so effective speed exceeds 100%.
    /// Acceptance: A PumpSession tick with LimiterEnabled = false advances strictly
    ///   more master cycles than the same session advances with the limiter on.
    /// </summary>
    [Fact]
    public async Task PumpSession_WhenLimiterDisabled_AdvancesLargerCycleBurst()
    {
        var (registry, sessionId) = await CreateRunningSessionAsync();
        var pump = new EmulationPumpService(registry);

        registry.TryGet(sessionId, out var session);

        session!.LimiterEnabled = true;
        var limitedCycles = pump.PumpSession(session);

        session.LimiterEnabled = false;
        var warpCycles = pump.PumpSession(session);

        Assert.True(warpCycles > limitedCycles,
            $"Expected warp burst ({warpCycles}) to exceed the paced slice ({limitedCycles}).");
    }

    /// <summary>
    /// FR: FR-WARP-001 / BUG-THROTTLE-001, TR: TR-CYCLE-PACE-001.
    /// Use case: The emulation throttle is a selectable strategy (gate); the active one
    ///   is reported so callers/diagnostics know which is in effect.
    /// Acceptance: A pump built with the VICE gate reports GateName "VICE"; one built with
    ///   the Semaphore gate reports "Semaphore".
    /// </summary>
    [Fact]
    public void EmulationPump_GateName_ReflectsInjectedStrategy()
    {
        var registry = new EmulatorRuntimeRegistry();
        using var vice = new EmulationPumpService(registry, new ViceEmulationGate());
        using var semaphore = new EmulationPumpService(registry, new SemaphoreEmulationGate());

        Assert.Equal("VICE", vice.GateName);
        Assert.Equal("Semaphore", semaphore.GateName);
    }

    /// <summary>
    /// FR: FR-WARP-001 / BUG-THROTTLE-001, TR: TR-CYCLE-PACE-001.
    /// Use case: The VICE gate advances a Running limiter-on session toward real time on
    ///   each tick (its first tick primes the vsync anchor and advances a chunk without
    ///   sleeping), and reports its strategy name.
    /// Acceptance: ViceEmulationGate.Tick advances the master clock and returns true for a
    ///   Running session; Name is "VICE".
    /// </summary>
    [Fact]
    public async Task ViceGate_Tick_AdvancesRunningSession()
    {
        var (registry, sessionId) = await CreateRunningSessionAsync();
        registry.TryGet(sessionId, out var session);
        session!.LimiterEnabled = true;

        using var gate = new ViceEmulationGate();
        gate.Start();

        var clock = session.Machine.Clock;
        var before = clock.TotalCycles;
        var ran = gate.Tick(registry, Advance);
        gate.Stop();

        Assert.True(ran, "the gate reported no running session");
        Assert.True(clock.TotalCycles > before, "the gate did not advance the clock");
        Assert.Equal("VICE", gate.Name);

        static long Advance(EmulatorRuntimeSession target, long cycles)
        {
            lock (target.SyncRoot)
            {
                var start = target.Machine.Clock.TotalCycles;
                target.Machine.Clock.Step(cycles);
                return target.Machine.Clock.TotalCycles - start;
            }
        }
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
}
