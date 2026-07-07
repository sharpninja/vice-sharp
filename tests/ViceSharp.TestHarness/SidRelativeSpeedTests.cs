namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR: FR-Audio-SID, TR: TR-AUDIO-SPEED-001. VICE ties sound to the limiter's
/// relative speed: sound_set_relative_speed (vice sound.c:1799-1817) tracks
/// vsync's speed percent and re-inits the SID, whose sample step becomes
/// clkstep = speed_percent/100 * cycles_per_sec / sample_rate (sound.c:1067,
/// engine rate = sample_rate*100/speed_percent, sound.c:1063-1064). The fixed
/// 44100 Hz device then drains those samples at wall rate, so audio
/// back-pressure paces emulation to the REQUESTED rate (pitch shifts with
/// speed, exactly like VICE fast-forward). The managed SID must scale its
/// tick:sample cadence the same way.
/// </summary>
public sealed class SidRelativeSpeedTests
{
    private sealed class CountingBackend : IAudioBackend
    {
        public long SubmittedSamples;

        public int QueuedSampleCount => 0;

        public int AvailableSampleCount => int.MaxValue;

        public void SubmitSamples(ReadOnlySpan<float> samples) => SubmittedSamples += samples.Length;

        public void Pause()
        {
        }

        public void Resume()
        {
        }

        public void Stop()
        {
        }
    }

    private static long SamplesPerEmulatedSecond(double relativeSpeedPercent)
    {
        var backend = new CountingBackend();
        var sid = new Sid6581(new BasicBus(), backend);
        sid.ConfigureAudioClock(985_248.0);
        sid.SetRelativeSpeed(relativeSpeedPercent);

        for (var i = 0; i < 985_248; i++)
            sid.Tick();

        return backend.SubmittedSamples;
    }

    /// <summary>
    /// FR: FR-Audio-SID, TR: TR-AUDIO-SPEED-001, TEST: TEST-AUDIO-SPEED-01.
    /// Use case: one emulated second at 100 percent produces the device rate
    /// in samples; at 200 percent half of it (device drains the same 44100
    /// per wall second, so emulation runs twice as fast); at 50 percent
    /// double. Acceptance: sample counts for one emulated PAL second land
    /// within one flush batch (256) of 44100, 22050, and 88200.
    /// </summary>
    [Fact]
    public void Sample_Cadence_Scales_With_Relative_Speed()
    {
        var at100 = SamplesPerEmulatedSecond(100);
        Assert.InRange(at100, 44100 - 256, 44100 + 1);

        var at200 = SamplesPerEmulatedSecond(200);
        Assert.InRange(at200, 22050 - 256, 22050 + 1);

        var at50 = SamplesPerEmulatedSecond(50);
        Assert.InRange(at50, 88200 - 256, 88200 + 1);
    }

    /// <summary>
    /// FR: FR-Audio-SID, TR: TR-AUDIO-SPEED-001, TEST: TEST-AUDIO-SPEED-02.
    /// Use case: sound_set_relative_speed treats non-positive/invalid values
    /// as no-ops host-side; the chip clamps to a sane band and reconfiguring
    /// the audio clock preserves the selected speed (VICE re-inits the SID
    /// with the current speed_percent, sound.c:1063).
    /// Acceptance: 0 and negative leave the previous factor; the factor
    /// survives ConfigureAudioClock.
    /// </summary>
    [Fact]
    public void Relative_Speed_Ignores_Garbage_And_Survives_Reconfigure()
    {
        var backend = new CountingBackend();
        var sid = new Sid6581(new BasicBus(), backend);
        sid.ConfigureAudioClock(985_248.0);

        sid.SetRelativeSpeed(200);
        sid.SetRelativeSpeed(0);
        sid.SetRelativeSpeed(-50);
        sid.ConfigureAudioClock(985_248.0);

        for (var i = 0; i < 985_248; i++)
            sid.Tick();

        Assert.InRange(backend.SubmittedSamples, 22050 - 256, 22050 + 1);
    }
}
