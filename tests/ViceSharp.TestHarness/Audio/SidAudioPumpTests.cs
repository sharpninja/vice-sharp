namespace ViceSharp.TestHarness.Audio;

using System;
using System.Collections.Generic;
using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using ViceSharp.Host.Audio;
using Xunit;

/// <summary>
/// FR/TR: FR-SIDAUDIO-001 / TEST-SIDAUDIO-001.
/// Use case: live SID audio. The cycle-clocked SID must emit output samples at
/// 44.1 kHz while it is ticked (downconverting from its tick rate via a
/// fractional accumulator) ONLY when an audio backend is attached and the audio
/// clock is configured. With no backend, or before the clock is configured, the
/// SID must touch nothing on the audio path - the property that keeps native
/// cycle parity intact.
/// </summary>
public sealed class SidAudioPumpTests
{
    private sealed class CountingAudioBackend : IAudioBackend
    {
        public int TotalSamples { get; private set; }
        public List<float> Samples { get; } = new();

        public void SubmitSamples(ReadOnlySpan<float> samples)
        {
            TotalSamples += samples.Length;
            foreach (var s in samples)
                Samples.Add(s);
        }

        public int QueuedSampleCount => 0;
        public void Pause() { }
        public void Resume() { }
        public void Stop() { }
    }

    // BUG-SIDAUDIO-001: SID Tick() is invoked once per master cycle (ClockDivisor = 1),
    // so a PAL frame (19656 master cycles) is 19656 ticks.
    private const int PalTicksPerFrame = 19656;
    private const double PalMasterClockHz = 985248.0;

    /// <summary>
    /// FR-SIDAUDIO-001 / TR-SIDAUDIO-CLOCK-001 / TEST-SIDAUDIO-001.
    /// Use case: with a backend and a configured audio clock, ticking the SID for one PAL
    ///   frame's worth of master cycles emits roughly one frame of 44.1 kHz samples.
    /// Acceptance: 19656 ticks (ClockDivisor 1) yield 872..886 samples.
    /// </summary>
    [Fact]
    public void Tick_WithBackendAndClock_EmitsRoughlyOneFrameOfSamples()
    {
        var backend = new CountingAudioBackend();
        var sid = new Sid6581(new BasicBus(), backend);
        sid.ConfigureAudioClock(PalMasterClockHz);

        for (var i = 0; i < PalTicksPerFrame; i++)
            sid.Tick();
        sid.FlushAudioBuffer();

        // 19656 ticks / ((985248/1)/44100 ~= 22.34 ticks-per-sample) ~= 880 samples.
        backend.TotalSamples.Should().BeInRange(872, 886);
    }

    /// <summary>
    /// FR-SIDAUDIO-001 / TR-SIDAUDIO-CLOCK-001 / TEST-SIDAUDIO-001.
    /// Use case: audio emission must be gated on an explicitly configured audio clock so a
    ///   parity (silent) run touches nothing on the audio path.
    /// Acceptance: without ConfigureAudioClock, ticking emits zero samples.
    /// </summary>
    [Fact]
    public void Tick_WithBackendButNoClockConfigured_EmitsNothing()
    {
        var backend = new CountingAudioBackend();
        var sid = new Sid6581(new BasicBus(), backend);
        // Deliberately do NOT call ConfigureAudioClock.

        for (var i = 0; i < PalTicksPerFrame; i++)
            sid.Tick();
        sid.FlushAudioBuffer();

        backend.TotalSamples.Should().Be(0, "audio emission must be gated on an explicitly configured clock");
    }

    /// <summary>
    /// FR-SIDAUDIO-001 / TR-SIDAUDIO-CLOCK-001 / TEST-SIDAUDIO-001.
    /// Use case: with no audio backend, the SID's per-tick sample path must be inert (no
    ///   allocation, no throw) even when the audio clock is configured.
    /// Acceptance: ticking + flushing with a null backend does not throw.
    /// </summary>
    [Fact]
    public void Tick_WithNoBackend_DoesNotThrow_AndEmitsNothing()
    {
        var sid = new Sid6581(new BasicBus());
        sid.ConfigureAudioClock(PalMasterClockHz);

        var act = () =>
        {
            for (var i = 0; i < PalTicksPerFrame; i++)
                sid.Tick();
            sid.FlushAudioBuffer();
        };

        act.Should().NotThrow();
    }

    /// <summary>
    /// FR-SIDAUDIO-001 / TR-SIDAUDIO-CLOCK-001 / TEST-SIDAUDIO-001.
    /// Use case: a gated sawtooth voice must produce a continuous, time-varying sample
    ///   stream (proving emission is clocked against the evolving synthesis state).
    /// Acceptance: 400000 ticks yield more than 15000 samples with more than 50 distinct values.
    /// </summary>
    [Fact]
    public void Sid_WithGatedVoice_ProducesTimeVaryingSamples()
    {
        var backend = new CountingAudioBackend();
        var sid = new Sid6581(new BasicBus(), backend) { BaseAddress = 0xD400 };
        sid.ConfigureAudioClock(PalMasterClockHz);

        // Voice 1: mid frequency, sawtooth + gate, instant attack, full sustain, max volume.
        sid.Write(0xD400, 0x00); // freq lo
        sid.Write(0xD401, 0x20); // freq hi
        sid.Write(0xD405, 0x00); // attack/decay
        sid.Write(0xD406, 0xF0); // sustain=15 / release=0
        sid.Write(0xD418, 0x0F); // master volume = 15
        sid.Write(0xD404, 0x21); // sawtooth (0x20) + gate (0x01)

        // ~0.41 s of emulated time: 400000 master-cycle ticks (ClockDivisor = 1) at
        // ~22.34 ticks/sample yields ~17900 samples.
        for (var i = 0; i < 400_000; i++)
            sid.Tick();
        sid.FlushAudioBuffer();

        backend.TotalSamples.Should().BeGreaterThan(15000);
        backend.Samples.Distinct().Count().Should().BeGreaterThan(50,
            "a gated sawtooth voice must yield a varying waveform");
    }

    /// <summary>
    /// FR-SIDAUDIO-001 / TR-SIDAUDIO-CLOCK-001 / TEST-SIDAUDIO-001.
    /// Use case: float samples are converted to little-endian PCM16 with clamping for the
    ///   audio output path.
    /// Acceptance: 0/1/-1 map to 0x0000/0x7FFF/0x8001 and out-of-range values clamp to
    ///   0x7FFF / 0x8000, all little-endian.
    /// </summary>
    [Fact]
    public void ConvertToPcm16_ClampsAndEncodesLittleEndian()
    {
        Span<byte> dst = stackalloc byte[10];
        var written = AudioSampleConverter.ConvertToPcm16(
            new[] { 0f, 1f, -1f, 2f, -2f }, dst);

        written.Should().Be(5);
        // 0.0 -> 0
        dst[0].Should().Be(0x00); dst[1].Should().Be(0x00);
        // 1.0 -> 32767 (0x7FFF)
        dst[2].Should().Be(0xFF); dst[3].Should().Be(0x7F);
        // -1.0 -> -32767 (0x8001)
        dst[4].Should().Be(0x01); dst[5].Should().Be(0x80);
        // 2.0 -> clamped to 32767 (0x7FFF)
        dst[6].Should().Be(0xFF); dst[7].Should().Be(0x7F);
        // -2.0 -> clamped to -32768 (0x8000)
        dst[8].Should().Be(0x00); dst[9].Should().Be(0x80);
    }
}
