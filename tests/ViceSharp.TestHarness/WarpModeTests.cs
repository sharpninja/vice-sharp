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
    ///   MainWindow.ApplyStatus() shows WARP when LimiterRatePercent is 0.
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
    /// FR: FR-WARP-001, TR: TR-WARP-STATUS-001.
    /// Use case: The 1000% limiter target is still a limiter target, not VICE warp.
    /// Acceptance: A limiter-enabled session at 1000% does not publish/report Warp mode.
    /// </summary>
    [Fact]
    public void IsWarpMode_WhenLimiterEnabledAtMaximumRate_IsFalse()
    {
        var session = CreateMinimalSession();
        session.LimiterEnabled = true;
        session.LimiterRatePercent = 1000;

        Assert.False(session.IsWarpMode);
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
    /// FR: FR-WARP-001, TR: TR-WARP-STATUS-001.
    /// Use case: emulator-side Warp status is observable as a pub/sub event whenever the
    /// limiter state changes.
    /// Acceptance: disabling the limiter publishes an event that reports Warp active,
    /// the raw limiter state, and the current machine cycle.
    /// </summary>
    [Fact]
    public void SetLimiter_WhenLimiterDisabled_PublishesWarpModeEvent()
    {
        var session = CreatePubSubSession();
        Assert.NotNull(session.Machine.PubSub);
        var pubSub = session.Machine.PubSub;
        WarpModeEvent observed = default;
        var received = false;
        var handle = pubSub.Subscribe<WarpModeEvent>(WarpModeEvent.Topic, e =>
        {
            observed = e;
            received = true;
        });

        try
        {
            session.Machine.Clock.Step(42);
            session.SetLimiter(100, enabled: false);
        }
        finally
        {
            pubSub.Unsubscribe(handle);
        }

        Assert.True(received);
        Assert.True(observed.IsWarpMode);
        Assert.False(observed.LimiterEnabled);
        Assert.Equal(100, observed.LimiterRatePercent);
        Assert.Equal(42, observed.Cycle);
    }

    /// <summary>
    /// FR: FR-WARP-001, TR: TR-WARP-STATUS-001.
    /// Use case: starting or resuming the emulator establishes Warp status even when the
    /// limiter did not change during the command.
    /// Acceptance: StartAsync publishes the current Warp/limiter state after the session
    /// enters Running.
    /// </summary>
    [Fact]
    public async Task StartAsync_PublishesEstablishedWarpModeEvent()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreatePubSubSession();
        registry.Add(session);
        var host = new EmulatorHostService(registry, new DefaultEmulatorRuntimeFactory());
        Assert.NotNull(session.Machine.PubSub);
        var pubSub = session.Machine.PubSub;
        WarpModeEvent observed = default;
        var received = false;
        var handle = pubSub.Subscribe<WarpModeEvent>(WarpModeEvent.Topic, e =>
        {
            observed = e;
            received = true;
        });

        try
        {
            var response = await host.StartAsync(
                new SessionRequest(session.SessionId),
                TestContext.Current.CancellationToken);

            Assert.True(response.Status.IsSuccess);
        }
        finally
        {
            pubSub.Unsubscribe(handle);
        }

        Assert.True(received);
        Assert.False(observed.IsWarpMode);
        Assert.True(observed.LimiterEnabled);
        Assert.Equal(100, observed.LimiterRatePercent);
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

    /// <summary>
    /// FR: FR-PACESEL-001 / BUG-THROTTLE-001, TR: TR-CYCLE-PACE-001.
    /// Use case: VICE pacing uses the live sound device as the primary timing
    ///   source when a SID backend is active, matching VICE sound.c back-pressure
    ///   semantics instead of falling through to vsync.
    /// Acceptance: with a live audio timing source that still has queue room,
    ///   ViceEmulationGate.Tick advances one chunk through the Sound regulator.
    /// </summary>
    [Fact]
    public void ViceGate_Tick_WithAudioTimingSource_UsesSoundRegulator()
    {
        var audio = new TestAudioChip { IsTimingSource = true, QueuedSamples = 0 };
        var machine = new AudioTimingTestMachine(audio);
        var session = new EmulatorRuntimeSession(
            "audio-session",
            MinimalHostArchitectureDescriptor.Instance,
            machine)
        {
            RunState = EmulatorRunState.Running,
            LimiterEnabled = true
        };
        var registry = new EmulatorRuntimeRegistry();
        registry.Add(session);

        using var gate = new ViceEmulationGate();
        gate.Start();

        var advanceCalls = 0;
        long requestedCycles = 0;
        var ran = gate.Tick(registry, (target, cycles) =>
        {
            advanceCalls++;
            requestedCycles = cycles;
            target.Machine.Clock.Step(cycles);
            return cycles;
        });
        gate.Stop();

        Assert.True(ran);
        Assert.Equal(ViceEmulationGate.PacingRegulator.Sound, gate.LastRegulator);
        Assert.Equal(1, advanceCalls);
        Assert.True(requestedCycles > 0);
    }

    private static EmulatorRuntimeSession CreateMinimalSession()
    {
        var factory = new DefaultEmulatorRuntimeFactory(
            new ArchitectureBuilder(),
            [MinimalHostArchitectureDescriptor.Instance],
            MinimalHostArchitectureDescriptor.ArchitectureId);
        return factory.Create(new CreateEmulatorSessionRequest());
    }

    private static EmulatorRuntimeSession CreatePubSubSession() => new(
        "warp-pubsub",
        MinimalHostArchitectureDescriptor.Instance,
        new PubSubTestMachine());

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

    private sealed class AudioTimingTestMachine(IAudioChip audioChip) : IMachine
    {
        private readonly TestDeviceRegistry _devices = new(audioChip);

        public IBus Bus { get; } = new BasicBus();

        public IClock Clock { get; } = new SystemClock(1_000_000);

        public IDeviceRegistry Devices => _devices;

        public IArchitectureDescriptor Architecture => MinimalHostArchitectureDescriptor.Instance;

        public void RunFrame() => Clock.Step(20_000);

        public void StepInstruction() => Clock.Step();

        public MachineState GetState() => new() { Cycle = Clock.TotalCycles };

        public void Reset() => Clock.Reset();
    }

    private sealed class PubSubTestMachine : IMachine
    {
        public IBus Bus { get; } = new BasicBus();

        public IClock Clock { get; } = new SystemClock(1_000_000);

        public IDeviceRegistry Devices { get; } = new EmptyDeviceRegistry();

        public IArchitectureDescriptor Architecture => MinimalHostArchitectureDescriptor.Instance;

        public IPubSub PubSub { get; } = new LockFreePubSub();

        public void RunFrame() => Clock.Step(20_000);

        public void StepInstruction() => Clock.Step();

        public MachineState GetState() => new() { Cycle = Clock.TotalCycles };

        public void Reset() => Clock.Reset();
    }

    private sealed class EmptyDeviceRegistry : IDeviceRegistry
    {
        public IReadOnlyList<IDevice> All => [];

        public int Count => 0;

        public IDevice? GetById(DeviceId id) => null;

        public IReadOnlyList<T> GetAll<T>()
            where T : IDevice
            => [];

        public IDevice? GetByRole(DeviceRole role) => null;
    }

    private sealed class TestDeviceRegistry(IAudioChip audioChip) : IDeviceRegistry
    {
        private readonly IDevice[] _devices = [audioChip];

        public IReadOnlyList<IDevice> All => _devices;

        public int Count => _devices.Length;

        public IDevice? GetById(DeviceId id) => _devices.FirstOrDefault(device => device.Id == id);

        public IReadOnlyList<T> GetAll<T>()
            where T : IDevice
            => _devices.OfType<T>().ToArray();

        public IDevice? GetByRole(DeviceRole role) => role == DeviceRole.AudioChip ? audioChip : null;
    }

    private sealed class TestAudioChip : IAudioChip
    {
        public DeviceId Id => new(0xA001);

        public string Name => "Test Audio";

        public byte MasterVolume { get; set; }

        public int ChannelCount => 1;

        public int QueuedSamples { get; set; }

        public bool IsTimingSource { get; set; }

        public int QueuedSampleCount => QueuedSamples;

        public bool IsAudioTimingSource => IsTimingSource;

        public uint ClockDivisor => 1;

        public ClockPhase Phase => ClockPhase.Phi2;

        public float GenerateSample() => 0;

        public void Tick()
        {
        }

        public void Reset()
        {
        }
    }
}
