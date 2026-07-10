using System;
using System.Collections.Generic;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// PLAN-VICEPARITY-001 (live-audio wiring slice): the live emission path now
/// drives the reSID fixed-point resampler (SAMPLE_RESAMPLE, matching VICE
/// x64sc) one cycle per Tick, replacing the double-accumulator nearest-sample
/// picker. These tests prove the push (live Tick tail) and pull (ClockBuffered)
/// paths emit the identical short stream, that the emission gates and warp
/// behave, and that the hot path stays zero-allocation.
/// </summary>
public sealed class SidLiveSamplingTests
{
    private const double PalClock = 985248.0;

    private sealed class CollectingBackend : IAudioBackend
    {
        public readonly List<float> Samples = new();
        public void SubmitSamples(ReadOnlySpan<float> samples)
        {
            foreach (var s in samples) Samples.Add(s);
        }
        public int QueuedSampleCount => 0;
        public int AvailableSampleCount => int.MaxValue;
        public void Pause() { }
        public void Resume() { }
        public void Stop() { }
    }

    private sealed class CountingBackend : IAudioBackend
    {
        public long Count;
        public void SubmitSamples(ReadOnlySpan<float> samples) => Count += samples.Length;
        public int QueuedSampleCount => 0;
        public int AvailableSampleCount => int.MaxValue;
        public void Pause() { }
        public void Resume() { }
        public void Stop() { }
    }

    private static readonly (ushort reg, byte val)[] Program =
    {
        (0x15, 0x00), (0x16, 0x40), (0x17, 0x51), (0x18, 0x1F),
        (0x00, 0x00), (0x01, 0x40), (0x05, 0x00), (0x06, 0xF0), (0x04, 0x21),
        (0x07, 0x00), (0x08, 0x28), (0x0B, 0x41),
    };

    private static void Program6581(Sid6581 sid)
    {
        foreach (var (reg, val) in Program) sid.Write((ushort)(0xD400 + reg), val);
    }

    /// <summary>
    /// FR: FR-SID-OUTPUT (live wiring), TR-SID-RESAMPLE-001.
    /// Use case: the live per-Tick resampler tail (push) must emit exactly the
    /// same samples as the buffered ClockBuffered path (pull) at SAMPLE_RESAMPLE.
    /// Acceptance: for the same program and cycle count, the live float stream
    /// (via a backend) equals the buffered short stream, element for element,
    /// after rescaling (float == short / 2^15).
    /// </summary>
    [Fact]
    public void LiveResampleTail_EquivalentToBufferedResample()
    {
        const int cycles = 60000;

        // Pull path: ClockBuffered at SAMPLE_RESAMPLE.
        var pull = new Sid6581(new BasicBus()) { BaseAddress = 0xD400 };
        Assert.True(pull.SetSamplingParameters(PalClock, SidSamplingMethod.Resample, 44100.0));
        Program6581(pull);
        var pullSamples = new List<short>();
        var buf = new short[512];
        int remaining = cycles;
        while (remaining > 0)
        {
            int chunk = Math.Min(4096, remaining);
            int c = chunk;
            int got = pull.ClockBuffered(ref c, buf);
            for (int i = 0; i < got; i++) pullSamples.Add(buf[i]);
            remaining -= (chunk - c);
        }

        // Push path: live tail via Tick().
        var backend = new CollectingBackend();
        var push = new Sid6581(new BasicBus(), backend) { BaseAddress = 0xD400 };
        push.ConfigureAudioClock(PalClock);
        Program6581(push);
        for (int i = 0; i < cycles; i++) push.Tick();
        push.FlushAudioBuffer();

        int n = Math.Min(pullSamples.Count, backend.Samples.Count);
        Assert.True(n > 2000, $"expected a substantial sample stream, got {n}");
        // Sample counts match (identical fixed-point cadence).
        Assert.Equal(pullSamples.Count, backend.Samples.Count);
        for (int i = 0; i < n; i++)
        {
            Assert.Equal(pullSamples[i] / 32768.0f, backend.Samples[i]);
        }
    }

    /// <summary>
    /// FR: FR-SID-OUTPUT (live wiring), TR-SID-RESAMPLE-001.
    /// Use case: emission is gated - no backend, or an unconfigured audio clock,
    /// must emit nothing and never allocate/touch the audio path.
    /// Acceptance: a SID with no backend, and a SID with a backend but no
    /// ConfigureAudioClock, both emit zero samples across many ticks;
    /// IsAudioTimingSource is false until armed and true after.
    /// </summary>
    [Fact]
    public void LiveEmission_GatedByBackendAndConfigure()
    {
        var noBackend = new Sid6581(new BasicBus()) { BaseAddress = 0xD400 };
        Program6581(noBackend);
        for (int i = 0; i < 50000; i++) noBackend.Tick(); // must not throw
        Assert.False(noBackend.IsAudioTimingSource);

        var backend = new CountingBackend();
        var unconfigured = new Sid6581(new BasicBus(), backend) { BaseAddress = 0xD400 };
        Assert.False(unconfigured.IsAudioTimingSource);
        Program6581(unconfigured);
        for (int i = 0; i < 50000; i++) unconfigured.Tick();
        Assert.Equal(0, backend.Count);

        unconfigured.ConfigureAudioClock(PalClock);
        Assert.True(unconfigured.IsAudioTimingSource);
        for (int i = 0; i < 50000; i++) unconfigured.Tick();
        Assert.True(backend.Count > 2000);

        // masterClockHz <= 0 disarms.
        unconfigured.ConfigureAudioClock(0.0);
        Assert.False(unconfigured.IsAudioTimingSource);
    }

    /// <summary>
    /// FR: TR-AUDIO-WARP-001, TR-SID-RESAMPLE-001.
    /// Use case: SetRelativeSpeed scales the live decimation ratio only (pitch
    /// shift, like VICE fast-forward); the synthesis state is untouched.
    /// Acceptance: running at 200% speed emits fewer samples than 100% for the
    /// same cycle count, and the captured synthesis state after equal cycles is
    /// byte-identical to a 100% run (warp changes cadence, not synthesis).
    /// </summary>
    [Fact]
    public void Warp_ChangesCadenceNotSynthesis()
    {
        const int cycles = 100000;

        var baseline = new Sid6581(new BasicBus(), new CountingBackend()) { BaseAddress = 0xD400 };
        var b0 = (CountingBackend)GetBackend(baseline);
        baseline.ConfigureAudioClock(PalClock);
        Program6581(baseline);
        for (int i = 0; i < cycles; i++) baseline.Tick();

        var fast = new Sid6581(new BasicBus(), new CountingBackend()) { BaseAddress = 0xD400 };
        var b1 = (CountingBackend)GetBackend(fast);
        fast.ConfigureAudioClock(PalClock);
        fast.SetRelativeSpeed(200.0);
        Program6581(fast);
        for (int i = 0; i < cycles; i++) fast.Tick();

        Assert.True(b1.Count < b0.Count, "200% speed decimates more cycles per sample (fewer samples)");

        // Synthesis state is identical (warp only changed the emission cadence).
        var s0 = new byte[baseline.StateSize];
        var s1 = new byte[fast.StateSize];
        baseline.CaptureState(s0);
        fast.CaptureState(s1);
        Assert.Equal(s0, s1);
    }

    /// <summary>
    /// FR: TR-SID-RESAMPLE-001 (zero-allocation invariant).
    /// Use case: the live resampler tail must not allocate on the per-Tick hot
    /// path (all buffers preallocated at ConfigureAudioClock time).
    /// Acceptance: after warmup, ticking one second of audio (985248 cycles)
    /// allocates zero managed bytes on the emulation thread.
    /// </summary>
    [Fact]
    public void LiveResampleTail_IsZeroAllocationOnHotPath()
    {
        var sid = new Sid6581(new BasicBus(), new CountingBackend()) { BaseAddress = 0xD400 };
        sid.ConfigureAudioClock(PalClock);
        Program6581(sid);
        for (int i = 0; i < 50000; i++) sid.Tick(); // warmup + JIT

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 985248; i++) sid.Tick();
        long delta = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, delta);
    }

    /// <summary>
    /// FR: TR-AUDIO-WARP-001, TR-SID-RESAMPLE-001.
    /// Use case: extreme slow-motion warp (emulation speed below ~4.5%) drives
    /// the resampler cadence below one cycle per output sample (deltaTSample==0);
    /// the live tail must keep emitting (matching ClockResample's zero-cycle
    /// window path) instead of underflowing its countdown and going permanently
    /// silent.
    /// Acceptance: at 2% speed the live tail emits a continuous stream across the
    /// whole run - the sample count keeps growing in the second half, not just an
    /// initial burst that then stops.
    /// </summary>
    [Fact]
    public void ExtremeSlowWarp_KeepsEmitting_NoUnderflowStall()
    {
        var backend = new CollectingBackend();
        var sid = new Sid6581(new BasicBus(), backend) { BaseAddress = 0xD400 };
        sid.ConfigureAudioClock(PalClock);
        sid.SetRelativeSpeed(2.0); // ~2% -> cps < 1<<16 -> zero-cycle windows
        Program6581(sid);

        for (int i = 0; i < 5000; i++) sid.Tick();
        int firstHalf = backend.Samples.Count;
        for (int i = 0; i < 5000; i++) sid.Tick();
        int total = backend.Samples.Count;

        // Without the zero-cycle-window fix the tail underflows on the very first
        // window and stops (firstHalf ~= 0, total == firstHalf). With it, both
        // halves stream continuously.
        Assert.True(firstHalf > 1000, $"first half should stream, got {firstHalf}");
        Assert.True(total - firstHalf > 1000,
            $"second half should keep streaming (no stall), got {total - firstHalf}");
    }

    private static IAudioBackend GetBackend(Sid6581 sid)
    {
        var f = typeof(Sid6581).GetField("_audioBackend",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (IAudioBackend)f!.GetValue(sid)!;
    }
}
