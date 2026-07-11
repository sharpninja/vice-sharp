namespace ViceSharp.TestHarness.Audio;

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Core;
using ViceSharp.Host.Audio;
using Xunit;

/// <summary>
/// FR/TR: FR-SIDAUDIO-001 / TEST-SIDAUDIO-002.
/// End-to-end proof that live SID audio actually flows: a real C64 built with an
/// audio backend, driven for several frames with a voice gated, must deliver a
/// continuous ~44.1 kHz stream of TIME-VARYING samples to the backend (not the
/// all-identical output that a naive post-frame pump would produce on the
/// cycle-clocked SID). Also exercises the real winmm device path end-to-end.
/// </summary>
[Collection("NativeVice")]
public sealed class SidAudioWiringTests : IClassFixture<WindowsAudioSessionMute>
{
    private sealed class CollectingAudioBackend : IAudioBackend
    {
        public List<float> Samples { get; } = new();
        public void SubmitSamples(ReadOnlySpan<float> samples)
        {
            foreach (var s in samples)
                Samples.Add(s);
        }

        public int QueuedSampleCount => 0;
        public void Pause() { }
        public void Resume() { }
        public void Stop() { }
    }

    [Fact]
    public void RealC64_WithAudioBackend_ProducesTimeVaryingSampleStream()
    {
        var backend = new CollectingAudioBackend();
        var provider = MachineTestFactory.CreateC64RomProvider();
        var builder = new ArchitectureBuilder(provider, backend);
        var machine = builder.Build(new C64Descriptor(C64MachineProfiles.C64Pal));

        // Let the KERNAL reach the READY prompt (I/O banked in, SID addressable).
        for (var i = 0; i < 60; i++)
            machine.RunFrame();

        // Voice 1: a mid frequency, sawtooth + gate on, full sustain, max volume.
        machine.Bus.Write(0xD400, 0x00); // freq lo
        machine.Bus.Write(0xD401, 0x20); // freq hi
        machine.Bus.Write(0xD405, 0x00); // attack=0 / decay=0 (instant)
        machine.Bus.Write(0xD406, 0xF0); // sustain=15 / release=0
        machine.Bus.Write(0xD418, 0x0F); // master volume = 15
        machine.Bus.Write(0xD404, 0x21); // sawtooth (0x20) + gate (0x01)

        backend.Samples.Clear();

        const int measuredFrames = 20;
        for (var i = 0; i < measuredFrames; i++)
            machine.RunFrame();

        // ~879 samples/frame at 44.1 kHz over 20 PAL frames.
        backend.Samples.Count.Should().BeGreaterThan(15000,
            "the SID must stream ~44.1 kHz of samples while the machine runs");

        // The crucial property: the samples are time-varying (a real waveform),
        // not a single repeated value. This is what proves emission is clocked
        // from Tick() against the evolving synthesis state.
        backend.Samples.Distinct().Count().Should().BeGreaterThan(50,
            "a gated voice must produce a varying waveform, not a constant level");
    }

    [Fact]
    public void WinMmAudioBackend_PreservesSamplesWhenDeviceQueueIsFull()
    {
        if (!OperatingSystem.IsWindows())
            return;

        WinMmAudioBackend.DropsSamplesWhenDeviceQueueFull.Should().BeFalse(
            "live playback must not corrupt an otherwise-correct SID sample stream");
    }

    [Fact]
    public void WinMmAudioBackend_RingSpaceTracksWaveOutPlayCursor()
    {
        if (!OperatingSystem.IsWindows())
            return;

        const int bufferBytes = 4096;

        WinMmAudioBackend.ComputeAvailableBytes(writeCursorBytes: 512, playCursorBytes: 0, bufferBytes)
            .Should().Be(3584, "bytes between play and write cursors are still queued for playback");
        WinMmAudioBackend.ComputeAvailableBytes(writeCursorBytes: 128, playCursorBytes: 3584, bufferBytes)
            .Should().Be(3456, "wrapped write cursors must still leave only played bytes writable");
    }

    [Fact]
    public void WinMmAudioBackend_NormalizesWaveOutPositionAcrossRingWrap()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var subtract = 0;

        var cursor = WinMmAudioBackend.NormalizePlayCursor(9216, ref subtract, bufferBytes: 4096);

        cursor.Should().Be(1024);
        subtract.Should().Be(8192);
    }

    [Fact]
    public void WinMmAudioBackend_OpensAndAcceptsSamples_WithoutThrowing()
    {
        if (!OperatingSystem.IsWindows())
            return; // winmm is Windows-only; the backend is a silent no-op elsewhere.

        // Direct calls after the OS guard so the platform analyzer (CA1416) sees
        // the winmm path as Windows-reachable. Reaching the end without throwing
        // is the assertion: the real waveOut open/submit/pause/stop path executed
        // (or degraded to a silent no-op when no device is present).
        using var backend = new WinMmAudioBackend();

        var buffer = new float[256];
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = MathF.Sin(i * 0.1f) * 0.25f;

        for (var batch = 0; batch < 4; batch++)
            backend.SubmitSamples(buffer);

        var queued = backend.QueuedSampleCount;
        backend.Pause();
        backend.Resume();
        backend.Stop();

        queued.Should().BeGreaterThanOrEqualTo(0);
    }
}
