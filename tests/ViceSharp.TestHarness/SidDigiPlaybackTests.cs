namespace ViceSharp.TestHarness;

using FluentAssertions;
using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-SID-010 (BACKFILL-SID-001 digi playback slice).
/// Use case: $D418 bits 0-3 act as a 4-bit DAC; rapid writes produce
/// audible PCM ("digi") even with no voices gated. The DAC value
/// multiplies the mixed voice output and adds a DC offset, so changes
/// to $D418 are audible on real C64 hardware even with no oscillator
/// activity. This is the Galway/Daglish 4-bit sample technique
/// commonly used in C64 music for digitised drums and speech.
/// </summary>
public sealed class SidDigiPlaybackTests
{
    private static Sid6581 BuildSid()
    {
        var bus = new BasicBus();
        return new Sid6581(bus) { BaseAddress = 0xD400 };
    }

    /// <summary>
    /// FR/TR: FR-SID-010
    /// Use case: With volume bits 0-3 zero, the mixed output must be
    /// silent regardless of any other state. This guards the bottom
    /// rail of the DAC - $D418 = 0 means absolute silence even when
    /// the new DC-offset behaviour is active.
    /// Acceptance: $D418=0x00 + no voices gated yields exact 0.0f sample.
    /// </summary>
    [Fact]
    public void Volume0_ProducesSilence()
    {
        var sid = BuildSid();
        sid.Write(0xD418, 0x00); // volume = 0, no filter, no V3OFF
        for (int i = 0; i < 4; i++) sid.Tick();

        var sample = sid.GenerateSample();
        sample.Should().Be(0.0f);
    }

    /// <summary>
    /// FR/TR: FR-SID-010
    /// Use case: With volume bits at max ($0F) and no voices gated,
    /// the master-volume DAC contributes an audible DC offset. This is
    /// the foundation of digi playback - the volume register alone
    /// produces audible level even with zero oscillator activity.
    /// Acceptance: $D418=0x0F + no voices gated yields a strictly
    /// positive sample within the valid -1..1 range.
    /// </summary>
    [Fact]
    public void Volume15_NoVoicesGated_ProducesDcOffset()
    {
        var sid = BuildSid();
        sid.Write(0xD418, 0x0F); // volume = 15, no filter, no V3OFF
        for (int i = 0; i < 4; i++) sid.Tick();

        var sample = sid.GenerateSample();
        sample.Should().BeGreaterThan(0.0f, "max-volume DAC contributes DC offset (digi rail)");
        sample.Should().BeLessThanOrEqualTo(1.0f, "must stay in [-1,1] range");
    }

    /// <summary>
    /// FR/TR: FR-SID-010
    /// Use case: Sweeping $D418 bits 0-3 from 0..15 with no voices
    /// gated should produce a monotonically non-decreasing sequence
    /// of samples - that is the staircase shape that makes digi
    /// playback work. Real C64 programs write the next 4-bit sample
    /// value every rasterline (~15.7kHz) to play PCM through the
    /// volume DAC.
    /// Acceptance: Captured samples at volumes 0..15 are
    /// monotonically non-decreasing and span the DAC range.
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

        // Monotonically non-decreasing across the DAC range.
        for (int i = 1; i < samples.Length; i++)
        {
            samples[i].Should().BeGreaterThanOrEqualTo(
                samples[i - 1],
                $"sample at volume {i} must be >= sample at volume {i - 1} (DAC staircase)");
        }

        // The full sweep must actually move - volume 15 strictly above volume 0.
        samples[15].Should().BeGreaterThan(samples[0], "DAC must span a non-zero range");
    }

    /// <summary>
    /// FR/TR: FR-SID-010
    /// Use case: Only $D418 bits 0-3 are the volume DAC. Bits 4-6 are
    /// filter-mode selects and bit 7 is the voice-3 disconnect flag.
    /// When no voices are gated, only the low nibble drives the DAC
    /// output; the high nibble must not change the digi level.
    /// Acceptance: $D418=0x80 (V3OFF=1, vol=0) yields silence;
    /// $D418=0x8F (V3OFF=1, vol=15) equals $D418=0x0F (V3OFF=0, vol=15).
    /// </summary>
    [Fact]
    public void HighNibble_DoesNotAffectDigiLevel()
    {
        var sid = BuildSid();

        sid.Write(0xD418, 0x80); // V3OFF=1, volume=0
        for (int i = 0; i < 4; i++) sid.Tick();
        sid.GenerateSample().Should().Be(0.0f);

        sid.Write(0xD418, 0x8F); // V3OFF=1, volume=15
        for (int i = 0; i < 4; i++) sid.Tick();
        var v3OffMaxVol = sid.GenerateSample();

        sid.Write(0xD418, 0x0F); // V3OFF=0, volume=15
        for (int i = 0; i < 4; i++) sid.Tick();
        var v3OnMaxVol = sid.GenerateSample();

        v3OffMaxVol.Should().Be(v3OnMaxVol, "V3OFF only matters if voice 3 is outputting");
        v3OffMaxVol.Should().BeGreaterThan(0.0f);
    }

    /// <summary>
    /// FR/TR: FR-SID-010
    /// Use case: Real digi playback alternates the volume register
    /// between low and high values at audio rate, producing a square
    /// wave that the listener perceives as PCM. The captured sample
    /// stream must show this alternation - silence rail interleaved
    /// with the high-volume DC offset.
    /// Acceptance: Alternating $D418 between 0x00 and 0x0F yields a
    /// non-zero peak-to-peak amplitude in the captured sample stream.
    /// </summary>
    [Fact]
    public void RapidAlternatingWrites_ProduceAlternatingSamples()
    {
        var sid = BuildSid();
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

        // Even-index samples are the low rail ($D418=0); odd-index are the high rail.
        float maxLow = float.MinValue, minHigh = float.MaxValue;
        for (int i = 0; i < samples.Length; i += 2) maxLow = MathF.Max(maxLow, samples[i]);
        for (int i = 1; i < samples.Length; i += 2) minHigh = MathF.Min(minHigh, samples[i]);

        maxLow.Should().Be(0.0f, "every low-rail sample must be exactly silent");
        minHigh.Should().BeGreaterThan(0.0f, "every high-rail sample must carry the digi DC offset");
        (minHigh - maxLow).Should().BeGreaterThan(0.0f, "AC component must be non-zero (audible digi)");
    }
}
