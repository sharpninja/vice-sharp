namespace ViceSharp.TestHarness;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-SID-013 (BACKFILL-SID-001 Slice 5 - audio backend wiring).
/// Use case: SID synthesizes samples per host tick; when an IAudioBackend
/// is wired into the SID, GenerateSampleAndOutput buffers samples and
/// submits batches to the backend for playback. Without a backend, the
/// SID still works (samples are dropped to /dev/null).
/// </summary>
public sealed class SidAudioBackendTests
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

    /// <summary>
    /// FR/TR: FR-SID-013
    /// Use case: Without an audio backend, GenerateSampleAndOutput runs
    /// without throwing and the SID continues to synthesize.
    /// Acceptance: 1000 calls do not throw.
    /// </summary>
    [Fact]
    public void NoBackend_GenerateAndOutput_DoesNotThrow()
    {
        var sid = new Sid6581(new BasicBus());
        var act = () =>
        {
            for (int i = 0; i < 1000; i++) sid.GenerateSampleAndOutput();
        };
        act.Should().NotThrow();
    }

    /// <summary>
    /// FR/TR: FR-SID-013
    /// Use case: With a backend wired, samples are batched and submitted
    /// once the internal buffer fills (256 samples per batch).
    /// Acceptance: After 256 calls the backend has received exactly 256
    /// samples; after 257 it still has 256 (partial batch held).
    /// </summary>
    [Fact]
    public void BackendWired_BatchesSamples_FlushesAt256()
    {
        var backend = new CapturingBackend();
        var sid = new Sid6581(new BasicBus(), backend);

        for (int i = 0; i < 256; i++) sid.GenerateSampleAndOutput();
        backend.Received.Count.Should().Be(256);

        sid.GenerateSampleAndOutput();
        backend.Received.Count.Should().Be(256, "partial batch waits for flush");
    }

    /// <summary>
    /// FR/TR: FR-SID-013
    /// Use case: FlushAudioBuffer drains any pending partial batch to the
    /// backend.
    /// Acceptance: After 10 calls + FlushAudioBuffer, backend has 10 samples.
    /// </summary>
    [Fact]
    public void Flush_DrainsPartialBuffer()
    {
        var backend = new CapturingBackend();
        var sid = new Sid6581(new BasicBus(), backend);

        for (int i = 0; i < 10; i++) sid.GenerateSampleAndOutput();
        sid.FlushAudioBuffer();

        backend.Received.Count.Should().Be(10);
    }

    /// <summary>
    /// FR/TR: FR-SID-013
    /// Use case: Generated samples are within the canonical -1.0..1.0 range
    /// after volume scaling.
    /// Acceptance: All samples from a long run are in [-1, 1].
    /// </summary>
    [Fact]
    public void GeneratedSamples_StayInValidRange()
    {
        var backend = new CapturingBackend();
        var sid = new Sid6581(new BasicBus(), backend);
        // Set volume + voice with envelope to produce output.
        sid.Write(0xD418, 0x0F); // volume = 15
        sid.Write(0xD400, 0x10); // V1 freq lo
        sid.Write(0xD401, 0x20); // V1 freq hi
        sid.Write(0xD404, 0x11); // V1 triangle + gate

        for (int i = 0; i < 512; i++)
        {
            sid.Tick();
            sid.GenerateSampleAndOutput();
        }
        sid.FlushAudioBuffer();

        backend.Received.Should().NotBeEmpty();
        backend.Received.Should().OnlyContain(s => s >= -1.0f && s <= 1.0f);
    }
}
