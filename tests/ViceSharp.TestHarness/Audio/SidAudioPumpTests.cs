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

    // SID Tick() is invoked once per ClockDivisor (16) master cycles, so a PAL
    // frame (19656 master cycles) is 19656 / 16 = 1228.5 ~= 1228 ticks.
    private const int PalTicksPerFrame = 1228;
    private const double PalMasterClockHz = 985248.0;

    [Fact]
    public void Tick_WithBackendAndClock_EmitsRoughlyOneFrameOfSamples()
    {
        var backend = new CountingAudioBackend();
        var sid = new Sid6581(new BasicBus(), backend);
        sid.ConfigureAudioClock(PalMasterClockHz);

        for (var i = 0; i < PalTicksPerFrame; i++)
            sid.Tick();
        sid.FlushAudioBuffer();

        // 1228 ticks / ((985248/16)/44100 ~= 1.396 ticks-per-sample) ~= 879 samples.
        backend.TotalSamples.Should().BeInRange(872, 886);
    }

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

        for (var i = 0; i < 25000; i++)
            sid.Tick();
        sid.FlushAudioBuffer();

        backend.TotalSamples.Should().BeGreaterThan(15000);
        backend.Samples.Distinct().Count().Should().BeGreaterThan(50,
            "a gated sawtooth voice must yield a varying waveform");
    }

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
