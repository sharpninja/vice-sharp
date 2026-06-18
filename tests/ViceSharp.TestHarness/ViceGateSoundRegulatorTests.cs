namespace ViceSharp.TestHarness;

using System;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// FR-SNDREG-001 / TR-SNDREG-GATE-001 / TEST-SNDREG-001.
/// The VICE pacing strategy's regulator 1 (sound-buffer back-pressure, faithful to
/// VICE sound.c sound_flush): when the audio device is the timing source the emulation
/// worker paces to the device draining its sample buffer - it blocks (advances nothing)
/// while the buffer is at/over the high-water mark and resumes as it drains. Precedence
/// is warp (skip all) -> sound (when audio is the timing source) -> vsync (otherwise).
/// </summary>
public sealed class ViceGateSoundRegulatorTests
{
    private const double PalMasterClockHz = 985_248.0;

    // ---- EvaluateSound: the pure regulator-1 decision ----

    /// <summary>FR-SNDREG-001 / TR-SNDREG-GATE-001. Use case: with no audio chip, sound is not the timing source.
    /// Acceptance: EvaluateSound(null, ...) == NotTimingSource.</summary>
    [Fact]
    public void EvaluateSound_NullChip_ReturnsNotTimingSource()
        => Assert.Equal(
            ViceEmulationGate.SoundAction.NotTimingSource,
            ViceEmulationGate.EvaluateSound(null, 2048));

    /// <summary>FR-SNDREG-001 / TR-SNDREG-GATE-001. Use case: a chip that is not emitting (no backend / clock unconfigured) is
    /// not the timing source. Acceptance: inactive chip => NotTimingSource.</summary>
    [Fact]
    public void EvaluateSound_InactiveChip_ReturnsNotTimingSource()
    {
        var chip = new FakeAudioChip { IsAudioTimingSource = false, QueuedSampleCount = 0 };
        Assert.Equal(
            ViceEmulationGate.SoundAction.NotTimingSource,
            ViceEmulationGate.EvaluateSound(chip, 2048));
    }

    /// <summary>FR-SNDREG-001 / TR-SNDREG-GATE-001. Use case: an active chip whose device buffer has room must keep running.
    /// Acceptance: queued below high-water => Advance.</summary>
    [Fact]
    public void EvaluateSound_ActiveChipBelowHighWater_ReturnsAdvance()
    {
        var chip = new FakeAudioChip { IsAudioTimingSource = true, QueuedSampleCount = 2047 };
        Assert.Equal(
            ViceEmulationGate.SoundAction.Advance,
            ViceEmulationGate.EvaluateSound(chip, 2048));
    }

    /// <summary>FR-SNDREG-001 / TR-SNDREG-GATE-001. Use case: a full device buffer back-pressures the CPU. Acceptance: queued
    /// exactly at the high-water mark => BackPressure (boundary is inclusive).</summary>
    [Fact]
    public void EvaluateSound_ActiveChipAtHighWater_ReturnsBackPressure()
    {
        var chip = new FakeAudioChip { IsAudioTimingSource = true, QueuedSampleCount = 2048 };
        Assert.Equal(
            ViceEmulationGate.SoundAction.BackPressure,
            ViceEmulationGate.EvaluateSound(chip, 2048));
    }

    /// <summary>FR-SNDREG-001 / TR-SNDREG-GATE-001. Use case: an over-full device buffer back-pressures the CPU.
    /// Acceptance: queued above the high-water mark => BackPressure.</summary>
    [Fact]
    public void EvaluateSound_ActiveChipAboveHighWater_ReturnsBackPressure()
    {
        var chip = new FakeAudioChip { IsAudioTimingSource = true, QueuedSampleCount = 9000 };
        Assert.Equal(
            ViceEmulationGate.SoundAction.BackPressure,
            ViceEmulationGate.EvaluateSound(chip, 2048));
    }

    // ---- Gate.Tick: regulator selection + back-pressure effect ----

    /// <summary>FR-SNDREG-001 / TR-SNDREG-GATE-001. Use case: audio is the timing source and the buffer has room - the gate
    /// runs the sound regulator and advances the clock. Acceptance: Tick advances the
    /// master clock and LastRegulator == Sound.</summary>
    [Fact]
    public void Tick_SoundActiveBelowHighWater_AdvancesAndSelectsSound()
    {
        var chip = new FakeAudioChip { IsAudioTimingSource = true, QueuedSampleCount = 0 };
        var (registry, session) = BuildSession(chip, limiterEnabled: true);
        using var gate = new ViceEmulationGate { BackPressurePause = static () => { } };
        gate.Start();

        var before = session.Machine.Clock.TotalCycles;
        var ran = gate.Tick(registry, Advance);
        gate.Stop();

        Assert.True(ran, "the gate reported no running session");
        Assert.True(session.Machine.Clock.TotalCycles > before, "sound regulator did not advance the clock");
        Assert.Equal(ViceEmulationGate.PacingRegulator.Sound, gate.LastRegulator);
    }

    /// <summary>FR-SNDREG-001 / TR-SNDREG-GATE-001. Use case: audio is the timing source and the device buffer is full - the
    /// worker must block rather than overrun the buffer. Acceptance: Tick advances ZERO
    /// master cycles (back-pressure) and LastRegulator == Sound.</summary>
    [Fact]
    public void Tick_SoundActiveAboveHighWater_BackPressuresWithoutAdvancing()
    {
        var chip = new FakeAudioChip
        {
            IsAudioTimingSource = true,
            QueuedSampleCount = ViceEmulationGate.HighWaterSamples + 5000,
        };
        var (registry, session) = BuildSession(chip, limiterEnabled: true);
        using var gate = new ViceEmulationGate { BackPressurePause = static () => { } };
        gate.Start();

        var before = session.Machine.Clock.TotalCycles;
        var ran = gate.Tick(registry, Advance);
        gate.Stop();

        Assert.True(ran, "the gate reported no running session");
        Assert.Equal(before, session.Machine.Clock.TotalCycles); // blocked: nothing advanced
        Assert.Equal(ViceEmulationGate.PacingRegulator.Sound, gate.LastRegulator);
    }

    /// <summary>FR-SNDREG-001 / TR-SNDREG-GATE-001. Use case: with no audio device the gate falls through to the vsync
    /// regulator. Acceptance: Tick advances a chunk and LastRegulator == Vsync.</summary>
    [Fact]
    public void Tick_NoAudioChip_SelectsVsync()
    {
        var (registry, session) = BuildSession(chip: null, limiterEnabled: true);
        using var gate = new ViceEmulationGate { BackPressurePause = static () => { } };
        gate.Start();

        var before = session.Machine.Clock.TotalCycles;
        var ran = gate.Tick(registry, Advance);
        gate.Stop();

        Assert.True(ran, "the gate reported no running session");
        Assert.True(session.Machine.Clock.TotalCycles > before, "vsync regulator did not advance the clock");
        Assert.Equal(ViceEmulationGate.PacingRegulator.Vsync, gate.LastRegulator);
    }

    /// <summary>FR-SNDREG-001 / TR-SNDREG-GATE-001. Use case: warp (limiter off) skips BOTH regulators even when audio is the
    /// timing source and its buffer is full - warp has highest precedence. Acceptance: Tick
    /// advances a burst and LastRegulator == Warp.</summary>
    [Fact]
    public void Tick_WarpIgnoresSoundBackPressure_SelectsWarp()
    {
        var chip = new FakeAudioChip { IsAudioTimingSource = true, QueuedSampleCount = 999_999 };
        var (registry, session) = BuildSession(chip, limiterEnabled: false);
        using var gate = new ViceEmulationGate { BackPressurePause = static () => { } };
        gate.Start();

        var before = session.Machine.Clock.TotalCycles;
        var ran = gate.Tick(registry, Advance);
        gate.Stop();

        Assert.True(ran, "the gate reported no running session");
        Assert.True(session.Machine.Clock.TotalCycles - before > 0, "warp did not advance the clock");
        Assert.Equal(ViceEmulationGate.PacingRegulator.Warp, gate.LastRegulator);
    }

    // ---- SID-side wiring: IsAudioTimingSource + QueuedSampleCount ----

    /// <summary>FR-SNDREG-001 / TR-SNDREG-GATE-001. Use case: a SID with no backend is silent and not a timing source.
    /// Acceptance: IsAudioTimingSource false, QueuedSampleCount 0.</summary>
    [Fact]
    public void Sid_NoBackend_IsNotTimingSource_AndZeroQueued()
    {
        var sid = new Sid6581(new BasicBus());
        Assert.False(sid.IsAudioTimingSource);
        Assert.Equal(0, sid.QueuedSampleCount);
    }

    /// <summary>FR-SNDREG-001 / TR-SNDREG-GATE-001. Use case: a SID with a backend but before ConfigureAudioClock is not yet a
    /// timing source (it emits nothing). Acceptance: IsAudioTimingSource false.</summary>
    [Fact]
    public void Sid_BackendButClockNotConfigured_IsNotTimingSource()
    {
        var sid = new Sid6581(new BasicBus(), new FakeAudioBackend());
        Assert.False(sid.IsAudioTimingSource);
    }

    /// <summary>FR-SNDREG-001 / TR-SNDREG-GATE-001. Use case: a SID with a backend and a configured audio clock IS the timing
    /// source. Acceptance: IsAudioTimingSource true.</summary>
    [Fact]
    public void Sid_BackendConfigured_IsTimingSource()
    {
        var sid = new Sid6581(new BasicBus(), new FakeAudioBackend());
        sid.ConfigureAudioClock(PalMasterClockHz);
        Assert.True(sid.IsAudioTimingSource);
    }

    /// <summary>FR-SNDREG-001 / TR-SNDREG-GATE-001. Use case: the SID surfaces the backend's queue depth so the gate can
    /// back-pressure on it. Acceptance: QueuedSampleCount mirrors the backend.</summary>
    [Fact]
    public void Sid_QueuedSampleCount_ReflectsBackend()
    {
        var backend = new FakeAudioBackend { QueuedSampleCount = 1234 };
        var sid = new Sid6581(new BasicBus(), backend);
        Assert.Equal(1234, sid.QueuedSampleCount);
    }

    // ---- helpers ----

    private static long Advance(EmulatorRuntimeSession session, long cycles)
    {
        lock (session.SyncRoot)
        {
            var start = session.Machine.Clock.TotalCycles;
            session.Machine.Clock.Step(cycles);
            return session.Machine.Clock.TotalCycles - start;
        }
    }

    private static (EmulatorRuntimeRegistry Registry, EmulatorRuntimeSession Session) BuildSession(
        IAudioChip? chip, bool limiterEnabled)
    {
        var clock = new SystemClock((long)PalMasterClockHz);
        var arch = MinimalHostArchitectureDescriptor.Instance;
        var machine = new FakeMachine(clock, new FakeDeviceRegistry(chip), arch);
        var session = new EmulatorRuntimeSession("sound-regulator-test", arch, machine)
        {
            RunState = EmulatorRunState.Running,
            LimiterEnabled = limiterEnabled,
            LimiterRatePercent = 100,
        };
        var registry = new EmulatorRuntimeRegistry();
        registry.Add(session);
        return (registry, session);
    }

    private sealed class FakeAudioChip : IAudioChip
    {
        public DeviceId Id => new(0x9001);
        public string Name => "FakeAudioChip";
        public uint ClockDivisor => 1;
        public ClockPhase Phase => ClockPhase.Phi2;
        public byte MasterVolume { get; set; }
        public int ChannelCount => 1;
        public int QueuedSampleCount { get; set; }
        public bool IsAudioTimingSource { get; set; }
        public float GenerateSample() => 0f;
        public void Tick() { }
        public void Reset() { }
    }

    private sealed class FakeAudioBackend : IAudioBackend
    {
        public int QueuedSampleCount { get; set; }
        public void SubmitSamples(ReadOnlySpan<float> samples) { }
        public void Pause() { }
        public void Resume() { }
        public void Stop() { }
    }

    private sealed class FakeDeviceRegistry : IDeviceRegistry
    {
        private readonly IAudioChip? _audio;

        public FakeDeviceRegistry(IAudioChip? audio) => _audio = audio;

        public IDevice? GetByRole(DeviceRole role)
            => role == DeviceRole.AudioChip ? _audio : null;

        public IDevice? GetById(DeviceId id) => null;
        public IReadOnlyList<T> GetAll<T>() where T : IDevice => Array.Empty<T>();
        public IReadOnlyList<IDevice> All => _audio is null ? Array.Empty<IDevice>() : new IDevice[] { _audio };
        public int Count => _audio is null ? 0 : 1;
    }

    private sealed class FakeMachine : IMachine
    {
        private readonly IClock _clock;
        private readonly IDeviceRegistry _devices;
        private readonly IArchitectureDescriptor _architecture;

        public FakeMachine(IClock clock, IDeviceRegistry devices, IArchitectureDescriptor architecture)
        {
            _clock = clock;
            _devices = devices;
            _architecture = architecture;
        }

        public IClock Clock => _clock;
        public IDeviceRegistry Devices => _devices;
        public IArchitectureDescriptor Architecture => _architecture;
        public IBus Bus => throw new NotSupportedException();
        public void RunFrame() => throw new NotSupportedException();
        public void StepInstruction() => throw new NotSupportedException();
        public void Reset() => throw new NotSupportedException();
        public MachineState GetState() => new() { Cycle = _clock.TotalCycles };
    }
}
