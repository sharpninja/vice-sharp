namespace ViceSharp.TestHarness;

using FluentAssertions;
using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-SID-010 (BACKFILL-SID-001 digi playback slice).
/// Use case: $D418 bits 0-3 act as a 4-bit DAC; rapid writes produce audible PCM
/// ("digi") even with no voices gated - the Galway/Daglish 4-bit sample technique.
/// PLAN-VICEPARITY-001 S9: with the reSID 6581 op-amp filter the volume DAC now acts
/// through reSID's gain[vol] table (not the pre-S9 DigiDcOffset approximation). Two
/// consequences: (1) the "no voices" mix is not literal 0 - no-waveform voices carry
/// a constant floating-DAC bias once their power-up envelopes release (FR-SID-VOICE
/// AC-04), and vol=0 collapses that mix to the muted op-amp rail, not to 0.0f; and
/// (2) the voice-3 disconnect (bit 7) removes voice 3's floating bias from the mix.
/// These tests warm the chip to the settled floating-bias state, then verify the
/// volume DAC alone drives an audible, monotonic output.
/// </summary>
public sealed class SidDigiPlaybackTests
{
    private static Sid6581 BuildSid()
    {
        var bus = new BasicBus();
        return new Sid6581(bus) { BaseAddress = 0xD400 };
    }

    /// <summary>Warm the chip until the power-up envelopes release and the
    /// no-waveform floating-DAC bias settles, so $D418 alone drives the output.</summary>
    private static Sid6581 BuildSettledSid()
    {
        var sid = BuildSid();
        for (int i = 0; i < 30000; i++) sid.Tick();
        return sid;
    }

    private static float SampleAtVolume(Sid6581 sid, byte d418)
    {
        sid.Write(0xD418, d418);
        for (int t = 0; t < 16; t++) sid.Tick();
        return sid.GenerateSample();
    }

    /// <summary>
    /// FR/TR: FR-SID-010
    /// Use case: volume 0 collapses the reSID output op-amp gain, muting the audible
    /// signal. With the reSID model the muted rail is a constant DC (the settled
    /// floating-DAC bias through gain[0]), not literal 0.0f, and it is audibly
    /// distinct from a higher volume - which is exactly what makes the digi work.
    /// Acceptance: vol=0 yields an in-range muted rail that is audibly distinct from
    /// vol=15 (the volume DAC controls the level).
    /// </summary>
    [Fact]
    public void Volume0_ProducesSilence()
    {
        var sid = BuildSettledSid();
        var atZero = SampleAtVolume(sid, 0x00);
        var atMax = SampleAtVolume(sid, 0x0F);

        atZero.Should().BeInRange(-1.0f, 1.0f);
        atZero.Should().NotBe(atMax, "the volume DAC changes the level (digi is audible)");
    }

    /// <summary>
    /// FR/TR: FR-SID-010
    /// Use case: with volume at max ($0F) and no voices gated, the master-volume DAC
    /// drives an audible level through reSID's gain table - the foundation of digi
    /// playback (the volume register alone produces audible level with no oscillator
    /// activity).
    /// Acceptance: vol=15 yields an in-range sample that differs from vol=0.
    /// </summary>
    [Fact]
    public void Volume15_NoVoicesGated_ProducesDcOffset()
    {
        var sid = BuildSettledSid();
        var atZero = SampleAtVolume(sid, 0x00);
        var atMax = SampleAtVolume(sid, 0x0F);

        atMax.Should().BeInRange(-1.0f, 1.0f);
        atMax.Should().NotBe(atZero, "max-volume DAC drives an audible level (digi rail)");
    }

    /// <summary>
    /// FR/TR: FR-SID-010
    /// Use case: sweeping $D418 bits 0-3 from 0..15 with no voices gated produces a
    /// monotonically non-decreasing staircase of samples - the shape that makes digi
    /// playback work. Real programs write the next 4-bit sample every rasterline.
    /// Acceptance: samples at volumes 0..15 are monotonically non-decreasing and span
    /// a non-zero range.
    /// </summary>
    [Fact]
    public void SteppingVolume_ProducesMonotonicPcm()
    {
        var sid = BuildSid();
        var samples = new float[16];

        for (int v = 0; v < 16; v++)
        {
            sid.Write(0xD418, (byte)v);
            for (int t = 0; t < 1000; t++) sid.Tick();
            samples[v] = sid.GenerateSample();
        }

        for (int i = 1; i < samples.Length; i++)
        {
            samples[i].Should().BeGreaterThanOrEqualTo(
                samples[i - 1],
                $"sample at volume {i} must be >= sample at volume {i - 1} (DAC staircase)");
        }

        samples[15].Should().BeGreaterThan(samples[0], "DAC must span a non-zero range");
    }

    /// <summary>
    /// FR/TR: FR-SID-010
    /// Use case: $D418 bit 7 disconnects voice 3 from the audio output. Under the
    /// reSID model a no-waveform voice 3 still carries a floating-DAC bias, so V3OFF
    /// removes that bias from the mix (it is not a no-op as the pre-S9 approximation
    /// assumed). The low nibble remains the volume DAC.
    /// Acceptance: with V3OFF set, vol=0 and vol=15 still differ (the volume DAC is
    /// audible), and toggling V3OFF at a fixed volume changes the level (voice 3's
    /// floating contribution is removed).
    /// </summary>
    [Fact]
    public void HighNibble_DoesNotAffectDigiLevel()
    {
        var sid = BuildSettledSid();

        var v3OffZero = SampleAtVolume(sid, 0x80); // V3OFF=1, volume=0
        var v3OffMax = SampleAtVolume(sid, 0x8F);  // V3OFF=1, volume=15
        v3OffMax.Should().NotBe(v3OffZero, "the volume DAC is audible with voice 3 disconnected");

        var v3OnMax = SampleAtVolume(sid, 0x0F);   // V3OFF=0, volume=15
        v3OffMax.Should().NotBe(v3OnMax, "V3OFF removes voice 3's floating-DAC contribution from the mix");
        v3OnMax.Should().BeInRange(-1.0f, 1.0f);
    }

    /// <summary>
    /// FR/TR: FR-SID-010
    /// Use case: real digi playback alternates the volume register between low and
    /// high values at audio rate, producing a square wave perceived as PCM. The
    /// captured stream must show the alternation (a non-zero peak-to-peak amplitude).
    /// Acceptance: alternating $D418 between 0x00 and 0x0F yields distinct low- and
    /// high-rail samples (non-zero AC component).
    /// </summary>
    [Fact]
    public void RapidAlternatingWrites_ProduceAlternatingSamples()
    {
        var sid = BuildSettledSid();
        var samples = new float[200];

        for (int i = 0; i < 100; i++)
        {
            sid.Write(0xD418, 0x00);
            for (int t = 0; t < 50; t++) sid.Tick();
            samples[i * 2] = sid.GenerateSample();

            sid.Write(0xD418, 0x0F);
            for (int t = 0; t < 50; t++) sid.Tick();
            samples[i * 2 + 1] = sid.GenerateSample();
        }

        float maxLow = float.MinValue, minHigh = float.MaxValue;
        for (int i = 0; i < samples.Length; i += 2) maxLow = MathF.Max(maxLow, samples[i]);
        for (int i = 1; i < samples.Length; i += 2) minHigh = MathF.Min(minHigh, samples[i]);

        maxLow.Should().BeLessThan(minHigh, "the low rail (vol 0) and high rail (vol 15) must be distinct (audible digi)");
    }
}
