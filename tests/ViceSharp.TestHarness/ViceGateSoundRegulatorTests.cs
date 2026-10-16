namespace ViceSharp.TestHarness;

using System;
using System.Buffers.Binary;
using System.IO;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Audio;
using ViceSharp.Chips.Vic;
using ViceSharp.Core;
using ViceSharp.Core.Media;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// FR-SNDREG-001 / TR-SNDREG-GATE-001 / TEST-SNDREG-001.
/// The VICE pacing strategy's regulator 1 (sound-buffer back-pressure, faithful to
/// VICE sound.c sound_flush): when the audio device is the timing source the emulation
/// worker advances to the next sync chunk and the audio backend blocks only when a completed
/// sound fragment is written to a full device queue. Precedence is warp (skip all) -> sound
/// (when audio is the timing source) -> vsync (otherwise).
/// </summary>
public sealed class ViceGateSoundRegulatorTests
{
    private const double PalMasterClockHz = 985_248.0;

    // ---- EvaluateSound: the pure regulator-1 decision ----

    /// <summary>FR-SNDREG-001 / TR-SNDREG-GATE-001. Use case: with no audio chip, sound is not the timing source.
    /// Acceptance: EvaluateSound(null) == NotTimingSource.</summary>
    [Fact]
    public void EvaluateSound_NullChip_ReturnsNotTimingSource()
        => Assert.Equal(
            ViceEmulationGate.SoundAction.NotTimingSource,
            ViceEmulationGate.EvaluateSound(null));

    /// <summary>FR-SNDREG-001 / TR-SNDREG-GATE-001. Use case: a chip that is not emitting (no backend / clock unconfigured) is
    /// not the timing source. Acceptance: inactive chip => NotTimingSource.</summary>
    [Fact]
    public void EvaluateSound_InactiveChip_ReturnsNotTimingSource()
    {
        var chip = new FakeAudioChip { IsAudioTimingSource = false, QueuedSampleCount = 0 };
        Assert.Equal(
            ViceEmulationGate.SoundAction.NotTimingSource,
            ViceEmulationGate.EvaluateSound(chip));
    }

    /// <summary>FR-SNDREG-001 / TR-SNDREG-GATE-001. Use case: an active audio chip is
    /// the timing source. Acceptance: timing source => Advance.</summary>
    [Fact]
    public void EvaluateSound_ActiveChipWithFragmentSpace_ReturnsAdvance()
    {
        var chip = new FakeAudioChip { IsAudioTimingSource = true, AvailableSamples = 256, AudioFragmentSampleCount = 256 };
        Assert.Equal(
            ViceEmulationGate.SoundAction.Advance,
            ViceEmulationGate.EvaluateSound(chip));
    }

    /// <summary>FR-SNDREG-001 / TR-SNDREG-GATE-001. Use case: VICE produces sound before
    /// checking device space. Acceptance: pre-flush device space below fragment size still => Advance.</summary>
    [Fact]
    public void EvaluateSound_ActiveChipWithoutFragmentSpace_ReturnsAdvance()
    {
        var chip = new FakeAudioChip { IsAudioTimingSource = true, AvailableSamples = 255, AudioFragmentSampleCount = 256 };
        Assert.Equal(
            ViceEmulationGate.SoundAction.Advance,
            ViceEmulationGate.EvaluateSound(chip));
    }

    /// <summary>FR-SNDREG-001 / TR-SNDREG-GATE-001. Use case: VICE must not block before
    /// a completed fragment is produced. Acceptance: no pre-flush device space still => Advance.</summary>
    [Fact]
    public void EvaluateSound_ActiveChipWithNoSpace_ReturnsAdvance()
    {
        var chip = new FakeAudioChip { IsAudioTimingSource = true, AvailableSamples = 0, AudioFragmentSampleCount = 256 };
        Assert.Equal(
            ViceEmulationGate.SoundAction.Advance,
            ViceEmulationGate.EvaluateSound(chip));
    }

    // ---- Gate.Tick: regulator selection + back-pressure effect ----

    /// <summary>FR-SNDREG-001 / TR-SNDREG-GATE-001 / TEST-SNDREG-001.
    /// Use case: when the SID is the audio timing source and the device currently reports no
    ///   free space, the gate must still advance to the sound-flush boundary instead of
    ///   pre-blocking before a fragment exists.
    /// Acceptance: Tick selects Sound and advances cycles.</summary>
    [Fact]
    public void Tick_AudioTimingSourceWithoutFragmentSpace_AdvancesToSoundFlushBoundary()
    {
        var chip = new FakeAudioChip
        {
            IsAudioTimingSource = true,
            AudioFragmentSampleCount = 256,
            AvailableSamples = 0,
        };
        var (registry, session) = BuildSession(chip, limiterEnabled: true);
        using var gate = new ViceEmulationGate();
        gate.Start();

        var before = session.Machine.Clock.TotalCycles;
        var ran = gate.Tick(registry, Advance);
        gate.Stop();

        Assert.True(ran, "the gate reported no running session");
        Assert.True(session.Machine.Clock.TotalCycles > before, "sound regulator pre-blocked before the audio flush boundary");
        Assert.Equal(ViceEmulationGate.PacingRegulator.Sound, gate.LastRegulator);
    }

    /// <summary>FR-SNDREG-001 / TR-SNDREG-GATE-001. Use case: with no audio device the gate falls through to the vsync
    /// regulator. Acceptance: Tick advances a chunk and LastRegulator == Vsync.</summary>
    [Fact]
    public void Tick_NoAudioChip_SelectsVsync()
    {
        var (registry, session) = BuildSession(chip: null, limiterEnabled: true);
        using var gate = new ViceEmulationGate();
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
        var chip = new FakeAudioChip { IsAudioTimingSource = true, AvailableSamples = 0 };
        var (registry, session) = BuildSession(chip, limiterEnabled: false);
        using var gate = new ViceEmulationGate();
        gate.Start();

        var before = session.Machine.Clock.TotalCycles;
        var ran = gate.Tick(registry, Advance);
        gate.Stop();

        Assert.True(ran, "the gate reported no running session");
        Assert.True(session.Machine.Clock.TotalCycles - before > 0, "warp did not advance the clock");
        Assert.Equal(ViceEmulationGate.PacingRegulator.Warp, gate.LastRegulator);
    }

    // ---- SID-side wiring: IsAudioTimingSource + backend state ----

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

    /// <summary>
    /// FR-SNDREG-001 / FR-MED-003 / TEST-SNDREG-001. Losing a playback device through
    /// either recording wrapper must select vsync pacing without disabling recording
    /// or modifying samples forwarded to the backend. No real audio device is opened.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Tick_RecordingWrapperDeviceLost_SelectsVsyncAndKeepsRecording(bool fixedRecorder)
    {
        using var stream = new MemoryStream();
        using var recorder = new WavAudioRecorder(stream);
        var backend = new SwitchableAudioBackend();
        var wrapper = CreateRecordingWrapper(fixedRecorder, recorder, backend);
        var sid = new Sid6581(new BasicBus(), wrapper);
        sid.ConfigureAudioClock(PalMasterClockHz);
        var (registry, session) = BuildSession(sid, limiterEnabled: true);
        using var gate = new ViceEmulationGate();
        gate.Start();

        Assert.True(gate.Tick(registry, Advance));
        Assert.Equal(ViceEmulationGate.PacingRegulator.Sound, gate.LastRegulator);

        backend.IsAvailable = false;
        var before = session.Machine.Clock.TotalCycles;
        Assert.True(gate.Tick(registry, Advance));
        Assert.True(session.Machine.Clock.TotalCycles > before);
        Assert.Equal(ViceEmulationGate.PacingRegulator.Vsync, gate.LastRegulator);
        Assert.False(sid.IsAudioTimingSource);

        wrapper.SubmitSamples(new float[] { 0.25f, -0.5f });
        Assert.Equal(new float[] { 0.25f, -0.5f }, backend.Received);
        for (var cycle = 0; cycle < 6000; cycle++) sid.Tick();
        sid.FlushAudioBuffer();
        recorder.Stop();
        Assert.True(backend.Received.Count > 2, "device availability must not disable SID sample generation");
        Assert.Equal(backend.Received.Count * 2,
            BinaryPrimitives.ReadInt32LittleEndian(stream.ToArray().AsSpan(40, 4)));
        Assert.Equal((short)(0.25f * 32767f),
            BinaryPrimitives.ReadInt16LittleEndian(stream.ToArray().AsSpan(WavAudioRecorder.HeaderSize, 2)));
    }

    /// <summary>
    /// FR-SNDREG-001 / FR-MED-003 / TEST-SNDREG-001. A recorder-only wrapper captures
    /// samples but cannot supply device pacing, so an armed SID must use vsync.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Tick_RecordingWrapperWithoutDevice_SelectsVsyncAndStillRecords(bool fixedRecorder)
    {
        using var stream = new MemoryStream();
        using var recorder = new WavAudioRecorder(stream);
        var wrapper = CreateRecordingWrapper(fixedRecorder, recorder, downstream: null);
        var sid = new Sid6581(new BasicBus(), wrapper);
        sid.ConfigureAudioClock(PalMasterClockHz);
        var (registry, _) = BuildSession(sid, limiterEnabled: true);
        using var gate = new ViceEmulationGate();
        gate.Start();

        Assert.True(gate.Tick(registry, Advance));
        Assert.Equal(ViceEmulationGate.PacingRegulator.Vsync, gate.LastRegulator);
        Assert.False(sid.IsAudioTimingSource);
        wrapper.SubmitSamples(new float[] { -0.5f });
        for (var cycle = 0; cycle < 6000; cycle++) sid.Tick();
        sid.FlushAudioBuffer();
        recorder.Stop();
        Assert.True(BinaryPrimitives.ReadInt32LittleEndian(stream.ToArray().AsSpan(40, 4)) > 2,
            "recorder-only output must retain the SID sample stream");
        Assert.Equal((short)(-0.5f * 32767f),
            BinaryPrimitives.ReadInt16LittleEndian(stream.ToArray().AsSpan(WavAudioRecorder.HeaderSize, 2)));
    }

    /// <summary>
    /// FR-SNDREG-001 / TEST-SNDREG-001. Backends implementing the original interface
    /// remain timing sources through both recording wrappers without an availability override.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Sid_RecordingWrapperWithLegacyBackend_RemainsTimingSource(bool fixedRecorder)
    {
        using var stream = new MemoryStream();
        using var recorder = new WavAudioRecorder(stream);
        var wrapper = CreateRecordingWrapper(fixedRecorder, recorder, new FakeAudioBackend());
        var sid = new Sid6581(new BasicBus(), wrapper);
        sid.ConfigureAudioClock(PalMasterClockHz);

        Assert.True(sid.IsAudioTimingSource);
        Assert.Equal(ViceEmulationGate.SoundAction.Advance, ViceEmulationGate.EvaluateSound(sid));
        recorder.Stop();
    }

    /// <summary>
    /// FR-SNDREG-001 / FR-VIC20-SOUND-001 / TEST-SNDREG-001. VIC-I shares the
    /// playback backend and must fall back to vsync when it fails, while leaving
    /// its generated sample stream available to the recording tap.
    /// </summary>
    [Fact]
    public void Tick_Vic20PlaybackDeviceLost_SelectsVsyncAndKeepsSubmittingSamples()
    {
        var backend = new SwitchableAudioBackend();
        var vic = new Mos6561(audioBackend: new CaptureAudioTap(backend));
        var (registry, session) = BuildSession(vic, limiterEnabled: true);
        using var gate = new ViceEmulationGate();
        gate.Start();

        Assert.True(gate.Tick(registry, Advance));
        Assert.Equal(ViceEmulationGate.PacingRegulator.Sound, gate.LastRegulator);

        backend.IsAvailable = false;
        var before = session.Machine.Clock.TotalCycles;
        Assert.True(gate.Tick(registry, Advance));
        Assert.True(session.Machine.Clock.TotalCycles > before);
        Assert.Equal(ViceEmulationGate.PacingRegulator.Vsync, gate.LastRegulator);
        Assert.False(vic.IsAudioTimingSource);

        for (var cycle = 0; cycle < 7000; cycle++) vic.Tick();
        vic.FlushAudioBuffer();
        Assert.NotEmpty(backend.Received);
    }

    /// <summary>FR-SNDREG-001 / TR-SNDREG-GATE-001. Use case: the SID still surfaces the
    /// backend's queued samples for diagnostics. Acceptance: QueuedSampleCount mirrors the backend.</summary>
    [Fact]
    public void Sid_QueuedSampleCount_ReflectsBackend()
    {
        var backend = new FakeAudioBackend { QueuedSampleCount = 1234 };
        var sid = new Sid6581(new BasicBus(), backend);
        Assert.Equal(1234, sid.QueuedSampleCount);
    }

    /// <summary>FR-SNDREG-001 / TR-SNDREG-GATE-001. Use case: the SID surfaces backend
    /// device space so diagnostics can see the VICE sound_flush write-side timing state.
    /// Acceptance: AvailableSampleCount mirrors the backend.</summary>
    [Fact]
    public void Sid_AvailableSampleCount_ReflectsBackend()
    {
        var backend = new FakeAudioBackend { AvailableSampleCount = 512 };
        var sid = new Sid6581(new BasicBus(), backend);
        Assert.Equal(512, sid.AvailableSampleCount);
    }

    // ---- helpers ----

    private static IAudioBackend CreateRecordingWrapper(
        bool fixedRecorder, WavAudioRecorder recorder, IAudioBackend? downstream)
    {
        if (fixedRecorder)
            return new RecordingAudioBackend(recorder, downstream);

        var tap = new CaptureAudioTap(downstream);
        tap.AttachRecorder(recorder);
        return tap;
    }

    private sealed class SwitchableAudioBackend : IAudioBackend
    {
        public bool IsAvailable { get; set; } = true;
        public List<float> Received { get; } = new();
        public int QueuedSampleCount => 0;
        public void SubmitSamples(ReadOnlySpan<float> samples)
        {
            foreach (var sample in samples) Received.Add(sample);
        }
        public void Pause() { }
        public void Resume() { }
        public void Stop() { }
    }

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
        public int AvailableSamples { get; set; } = int.MaxValue;
        public Func<int>? AvailableSampleCountProvider { get; set; }
        public int AvailableSampleCount => AvailableSampleCountProvider?.Invoke() ?? AvailableSamples;
        public int AudioFragmentSampleCount { get; set; } = 256;
        public bool IsAudioTimingSource { get; set; }
        public float GenerateSample() => 0f;
        public void Tick() { }
        public void Reset() { }
    }

    private sealed class FakeAudioBackend : IAudioBackend
    {
        public int QueuedSampleCount { get; set; }
        public int AvailableSampleCount { get; set; } = int.MaxValue;
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
