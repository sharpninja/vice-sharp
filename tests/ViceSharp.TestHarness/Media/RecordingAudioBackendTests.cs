namespace ViceSharp.TestHarness.Media;

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Media;
using Xunit;

/// <summary>
/// FR/TR: FR-MED-003 (audio backend tee to WAV recorder).
/// Use case: The emulator routes SID audio through an IAudioBackend
/// pipeline. RecordingAudioBackend tees that float-PCM stream into a
/// WavAudioRecorder (after clamp + int16 conversion) while optionally
/// forwarding the original float samples to a downstream backend for
/// real playback.
/// Acceptance: Each SubmitSamples call writes int16 samples to the
/// recorder with [-1, 1] clamping (boundary mapping 1.0 -> 32767,
/// -1.0 -> -32767, 0.0 -> 0) and forwards an unmodified float span
/// to the downstream backend; null downstream is permitted.
/// </summary>
public sealed class RecordingAudioBackendTests
{
    private sealed class CapturingBackend : IAudioBackend
    {
        public List<float> Received { get; } = new();
        public int QueuedSampleCount => Received.Count;
        public void SubmitSamples(ReadOnlySpan<float> samples)
        {
            foreach (var s in samples) Received.Add(s);
        }
        public void Pause() { }
        public void Resume() { }
        public void Stop() => Received.Clear();
    }

    private static short[] ReadDataChunkSamples(byte[] wav)
    {
        // Data starts at offset 44 (canonical RIFF header) per WavAudioRecorder.
        int dataBytes = wav[40] | (wav[41] << 8) | (wav[42] << 16) | (wav[43] << 24);
        int sampleCount = dataBytes / 2;
        var samples = new short[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            int offset = 44 + (i * 2);
            samples[i] = (short)(wav[offset] | (wav[offset + 1] << 8));
        }
        return samples;
    }

    /// <summary>
    /// FR/TR: FR-MED-003
    /// Use case: A float audio stream is submitted to the tee; the recorder
    /// must receive the same number of int16 samples as floats submitted.
    /// Acceptance: After submitting 100 floats and stopping the recorder,
    /// the WAV data chunk contains exactly 100 short samples (200 bytes).
    /// </summary>
    [Fact]
    public void SubmitSamples_WritesIntoWavAudioRecorder()
    {
        using var ms = new MemoryStream();
        var recorder = new WavAudioRecorder(ms, sampleRate: 44100, channels: 1);
        var tee = new RecordingAudioBackend(recorder);

        var floats = new float[100];
        for (int i = 0; i < floats.Length; i++) floats[i] = 0.5f;

        tee.SubmitSamples(floats);
        recorder.Stop();

        var bytes = ms.ToArray();
        bytes.Length.Should().Be(44 + 200, "100 samples * 2 bytes + 44-byte header");
        var shorts = ReadDataChunkSamples(bytes);
        shorts.Length.Should().Be(100);
        // 0.5 * 32767 = 16383 (truncated short cast).
        shorts[0].Should().Be((short)(0.5f * 32767f));
    }

    /// <summary>
    /// FR/TR: FR-MED-003
    /// Use case: Float samples outside [-1, 1] must be clamped so the int16
    /// conversion does not wrap around or sign-flip.
    /// Acceptance: 2.0f clamps to 32767, -2.0f clamps to -32767 (not -32768).
    /// </summary>
    [Fact]
    public void SubmitSamples_ClampsFloatsToUnitRange()
    {
        using var ms = new MemoryStream();
        var recorder = new WavAudioRecorder(ms, sampleRate: 44100, channels: 1);
        var tee = new RecordingAudioBackend(recorder);

        tee.SubmitSamples(new float[] { 2.0f, -2.0f, 5.0f, -5.0f });
        recorder.Stop();

        var shorts = ReadDataChunkSamples(ms.ToArray());
        shorts.Length.Should().Be(4);
        shorts[0].Should().Be((short)32767);
        shorts[1].Should().Be((short)-32767);
        shorts[2].Should().Be((short)32767);
        shorts[3].Should().Be((short)-32767);
    }

    /// <summary>
    /// FR/TR: FR-MED-003
    /// Use case: Boundary checks for the float-to-int16 mapping at the
    /// canonical reference values (-1.0, 0.0, 1.0).
    /// Acceptance: 1.0 -> 32767, -1.0 -> -32767, 0.0 -> 0.
    /// </summary>
    [Fact]
    public void SubmitSamples_BoundaryMappingIsCorrect()
    {
        using var ms = new MemoryStream();
        var recorder = new WavAudioRecorder(ms, sampleRate: 44100, channels: 1);
        var tee = new RecordingAudioBackend(recorder);

        tee.SubmitSamples(new float[] { -1.0f, 0.0f, 1.0f });
        recorder.Stop();

        var shorts = ReadDataChunkSamples(ms.ToArray());
        shorts.Should().Equal(new short[] { -32767, 0, 32767 });
    }

    /// <summary>
    /// FR/TR: FR-MED-003
    /// Use case: When a downstream IAudioBackend is supplied, the tee must
    /// forward the original (unclamped, unmodified) float samples for
    /// playback while still writing the clamped int16 stream to the recorder.
    /// Acceptance: Downstream capturing backend receives the exact float[]
    /// passed in, in order.
    /// </summary>
    [Fact]
    public void Downstream_ReceivesIdenticalFloatStream()
    {
        using var ms = new MemoryStream();
        var recorder = new WavAudioRecorder(ms, sampleRate: 44100, channels: 1);
        var downstream = new CapturingBackend();
        var tee = new RecordingAudioBackend(recorder, downstream);

        var floats = new float[] { -0.25f, 0.0f, 0.5f, 1.0f, -1.0f };
        tee.SubmitSamples(floats);

        downstream.Received.Should().Equal(floats);
    }

    /// <summary>
    /// FR/TR: FR-MED-003
    /// Use case: Downstream backend is optional; the tee should function as
    /// a recorder-only sink when constructed with a null downstream.
    /// Acceptance: SubmitSamples does not throw and the recorder captures
    /// all the data; an explicit null assertion of QueuedSampleCount is 0
    /// (no downstream queue exists).
    /// </summary>
    [Fact]
    public void NullDownstream_StillRecords()
    {
        using var ms = new MemoryStream();
        var recorder = new WavAudioRecorder(ms, sampleRate: 44100, channels: 1);
        var tee = new RecordingAudioBackend(recorder, downstream: null);

        Action act = () => tee.SubmitSamples(new float[] { 0.1f, 0.2f, 0.3f });
        act.Should().NotThrow();
        recorder.Stop();

        var shorts = ReadDataChunkSamples(ms.ToArray());
        shorts.Length.Should().Be(3);
        tee.QueuedSampleCount.Should().Be(0);
    }
}
