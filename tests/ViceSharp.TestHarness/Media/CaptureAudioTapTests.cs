namespace ViceSharp.TestHarness.Media;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using ViceSharp.Abstractions;
using ViceSharp.Core.Media;
using Xunit;

/// <summary>
/// FR-MED-003 / TR-MEDIA-SOUND-001: runtime-swappable sound-recording tap.
/// The tap is installed once in the SID -> output path and lets the host
/// attach/detach a WAV recorder at runtime (StartCapture/StopCapture) without
/// rebuilding the machine. While idle it is a transparent pass-through; while a
/// recorder is attached it tees a clamped int16 copy of every float batch into
/// the recorder and still forwards the untouched floats to live playback.
/// </summary>
public sealed class CaptureAudioTapTests
{
    private sealed class CapturingBackend : IAudioBackend
    {
        public List<float> Received { get; } = new();
        public int QueuedSampleCount => 0;
        public void SubmitSamples(ReadOnlySpan<float> samples)
        {
            foreach (var s in samples) Received.Add(s);
        }
        public void Pause() { }
        public void Resume() { }
        public void Stop() { }
    }

    /// <summary>
    /// Acceptance: with no recorder attached, SubmitSamples forwards the exact
    /// float buffer to the downstream and IsRecording stays false.
    /// </summary>
    [Fact]
    public void SubmitSamples_NoRecorder_ForwardsToDownstreamOnly()
    {
        var downstream = new CapturingBackend();
        var tap = new CaptureAudioTap(downstream);

        tap.SubmitSamples(new[] { 0.25f, -0.5f, 0.75f });

        Assert.False(tap.IsRecording);
        Assert.Equal(new[] { 0.25f, -0.5f, 0.75f }, downstream.Received);
    }

    /// <summary>
    /// Acceptance: while a recorder is attached, one int16 sample is written per
    /// input float (clamped symmetrically to [-1, 1] then scaled by 32767) and
    /// the original floats still reach the downstream so live playback continues.
    /// </summary>
    [Fact]
    public void SubmitSamples_WithRecorder_WritesClampedInt16AndForwardsFloats()
    {
        var downstream = new CapturingBackend();
        var tap = new CaptureAudioTap(downstream);
        using var stream = new MemoryStream();
        var recorder = new WavAudioRecorder(stream, sampleRate: 44100, channels: 1);

        tap.AttachRecorder(recorder);
        Assert.True(tap.IsRecording);

        var input = new[] { -2.0f, 1.0f, 0.0f, -1.0f };
        tap.SubmitSamples(input);

        var detached = tap.DetachRecorder();
        Assert.Same(recorder, detached);
        Assert.False(tap.IsRecording);
        recorder.Stop();

        // Live playback path is undisturbed.
        Assert.Equal(input, downstream.Received);

        // The WAV data chunk holds one clamped int16 per input float.
        var bytes = stream.ToArray();
        int dataBytes = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(40, 4));
        Assert.Equal(input.Length * 2, dataBytes);

        var samples = new short[input.Length];
        for (int i = 0; i < samples.Length; i++)
            samples[i] = BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(WavAudioRecorder.HeaderSize + i * 2, 2));

        Assert.Equal(new short[] { -32767, 32767, 0, -32767 }, samples);
    }

    /// <summary>
    /// Acceptance: DetachRecorder returns the attached recorder once, then null.
    /// </summary>
    [Fact]
    public void DetachRecorder_ReturnsRecorderThenNull()
    {
        var tap = new CaptureAudioTap(downstream: null);
        using var stream = new MemoryStream();
        var recorder = new WavAudioRecorder(stream);

        tap.AttachRecorder(recorder);

        Assert.Same(recorder, tap.DetachRecorder());
        Assert.Null(tap.DetachRecorder());
    }
}
