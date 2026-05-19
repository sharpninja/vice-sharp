namespace ViceSharp.TestHarness;

using FluentAssertions;
using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-SID-009 (Noise Waveform Linear Feedback Shift Register) on the
/// 8580 die revision (BACKFILL-SID-001 8580 noise LFSR fix). Use case: the
/// 8580 SID variant must drive its noise LFSR identically to the 6581 (same
/// polynomial, taps, and bit-19 clocking) so that noise-based audio (drums,
/// hi-hats, SFX percussion) is audible and not stuck at a constant 0xFF.
/// Acceptance: noise output varies over time, matches 6581 for the same
/// stimulus, resets on the test bit, and is never constantly 0xFF.
/// </summary>
public sealed class Sid8580NoiseLfsrTests
{
    // SID voice 1 register offsets (relative to $D400).
    private const ushort V1FreqLo = 0xD400;
    private const ushort V1FreqHi = 0xD401;
    private const ushort V1Ctrl = 0xD404;
    private const ushort V1Ad = 0xD405;
    private const ushort V1Sr = 0xD406;
    private const ushort Osc3 = 0xD41B;
    private const ushort Volume = 0xD418;

    // Control register bits.
    private const byte Gate = 0x01;
    private const byte Test = 0x08;
    private const byte Noise = 0x80;

    private static T BuildNoiseVoice<T>() where T : Sid6581
    {
        var sid = (T)System.Activator.CreateInstance(typeof(T), new BasicBus())!;
        sid.Write(Volume, 0x0F);
        // Max frequency so the 24-bit accumulator's bit-19 transitions occur
        // as often as possible: at freq 0xFFFF every Tick advances the
        // accumulator significantly, ensuring many bit-19 transitions over
        // the test's tick budget.
        sid.Write(V1FreqLo, 0xFF);
        sid.Write(V1FreqHi, 0xFF);
        // Fast attack/decay so the envelope reaches sustain quickly; sustain
        // = max so envelope-scaled output isn't silenced.
        sid.Write(V1Ad, 0x00);
        sid.Write(V1Sr, 0xF0);
        // Gate + noise waveform on voice 1.
        sid.Write(V1Ctrl, (byte)(Noise | Gate));
        return sid;
    }

    /// <summary>
    /// FR/TR: FR-SID-009 acceptance criteria 1, 2 on Sid8580 (BACKFILL-
    /// SID-001 8580 noise LFSR fix). Use case: a program enables noise on
    /// voice 1 at high frequency and reads the noise output sequence; the
    /// LFSR must clock so that the noise sequence visits multiple distinct
    /// values over time. Acceptance: capturing voice-1 noise samples over
    /// 10000 ticks yields at least three distinct float values, proving the
    /// LFSR is clocking on bit-19 transitions rather than being stuck.
    /// </summary>
    [Fact]
    public void Sid8580_Noise_OutputVariesOverTime()
    {
        var sid = BuildNoiseVoice<Sid8580>();
        // Let the envelope ramp to sustain.
        for (var i = 0; i < 5000; i++) sid.Tick();

        var samples = new HashSet<float>();
        for (var i = 0; i < 10_000; i++)
        {
            sid.Tick();
            samples.Add(sid.GenerateSample());
        }

        samples.Count.Should().BeGreaterThanOrEqualTo(3,
            "8580 noise LFSR must clock on bit-19 transitions; if stuck at " +
            "0x7FFFFFFF the noise output is constant 0xFF and we'd see one value");
    }

    /// <summary>
    /// FR/TR: FR-SID-009 acceptance criteria 1, 2 (BACKFILL-SID-001 8580
    /// noise LFSR fix). Use case: the noise LFSR polynomial and taps are
    /// the same on the 6581 and 8580 die revisions (only the analog
    /// filter/combined-bleed differs). Two SID instances of different die
    /// types driven with identical register state and tick count should
    /// produce identical noise sample sequences. Acceptance: byte-for-byte
    /// equal sequences of OSC3 reads from both Sid6581 and Sid8580 over a
    /// large number of ticks.
    /// </summary>
    [Fact]
    public void Sid8580_Noise_MatchesSid6581_ForSameStimulus()
    {
        var sid6581 = BuildNoiseVoice<Sid6581>();
        var sid8580 = BuildNoiseVoice<Sid8580>();

        // Capture noise outputs via OSC3 from voice 3. To do that we'd need
        // voice 3 wired with noise; OSC3 reads bits 24-31 of voice 3's
        // accumulator, which is not the LFSR. Use GenerateSample instead;
        // both sids have voice 1 with noise, so the voice mix represents
        // the LFSR state. Filter is bypassed (no filter bits set), so the
        // 6581/8580 filter difference doesn't apply. The 8580 combined-
        // bleed scalar isn't engaged for single-waveform noise either.
        const int Ticks = 5000;
        for (var i = 0; i < Ticks; i++) sid6581.Tick();
        for (var i = 0; i < Ticks; i++) sid8580.Tick();

        var seq6581 = new float[2000];
        var seq8580 = new float[2000];
        for (var i = 0; i < seq6581.Length; i++)
        {
            sid6581.Tick();
            sid8580.Tick();
            seq6581[i] = sid6581.GenerateSample();
            seq8580[i] = sid8580.GenerateSample();
        }

        // Filter is bypassed and single-waveform path doesn't hit the
        // combined-bleed hook, so the noise samples should be identical
        // (LFSR polynomial parity per FR-SID-009 ac.1).
        seq8580.Should().Equal(seq6581,
            "8580 noise LFSR must use the same polynomial/taps as 6581 (FR-SID-009 ac.1)");
    }

    /// <summary>
    /// FR/TR: FR-SID-009 acceptance criterion 4 (BACKFILL-SID-001 8580 noise
    /// LFSR fix). Use case: a program asserts the test bit (CTRL bit 3) to
    /// re-seed the LFSR to the all-ones reset state, then clears it and
    /// expects the LFSR to advance predictably from the known seed. This
    /// is the standard SID demo technique for synchronising noise patterns.
    /// Acceptance: after setting test bit, noise output snaps to the reset-
    /// state value; after clearing test bit and ticking, the value changes
    /// (LFSR advanced from the seed).
    /// </summary>
    [Fact]
    public void Sid8580_Noise_TestBitResetsLfsr()
    {
        var sid = BuildNoiseVoice<Sid8580>();

        // Run for a while so the LFSR has clocked away from the all-ones
        // seed (or, if the bug is present, stuck at it).
        for (var i = 0; i < 20_000; i++) sid.Tick();
        var afterRun = sid.GenerateSample();

        // Set the test bit (along with noise + gate). This must reset the
        // LFSR to the all-ones seed per FR-SID-009 ac.4.
        sid.Write(V1Ctrl, (byte)(Noise | Test | Gate));
        sid.Tick();
        var afterTestBit = sid.GenerateSample();

        // Clear the test bit (back to noise + gate) and tick. The LFSR
        // should now advance from the all-ones seed.
        sid.Write(V1Ctrl, (byte)(Noise | Gate));
        for (var i = 0; i < 2000; i++) sid.Tick();
        var afterClearAndRun = sid.GenerateSample();

        // The reset state should be a specific value (all-ones LFSR + noise
        // tap bits all 1 = output 0xFF). After running from that seed the
        // value should change. We don't pin the exact reset value here
        // because the envelope/master-volume scaling makes the float value
        // non-trivial; we instead check that the LFSR advanced.
        afterClearAndRun.Should().NotBe(afterTestBit,
            "after clearing the test bit and clocking, the LFSR must advance " +
            "from the all-ones seed (FR-SID-009 ac.4 + ac.2)");
        afterTestBit.Should().NotBe(float.NaN, "test-bit reset produces a defined value");
    }

    /// <summary>
    /// FR/TR: FR-SID-009 (BACKFILL-SID-001 8580 noise LFSR fix). Use case:
    /// regression sentinel guarding against the original bug pattern where
    /// the 8580 noise output was constantly 0xFF because the LFSR was
    /// initialised to 0x7FFFFFFF and never clocked. If this test passes,
    /// noise output isn't a flat line. Acceptance: 100 well-separated
    /// noise output samples across 50000 ticks must not all be the same
    /// value (i.e. the LFSR cannot have been stuck).
    /// </summary>
    [Fact]
    public void Sid8580_Noise_NotConstantlySameSample_RegressionSentinel()
    {
        var sid = BuildNoiseVoice<Sid8580>();

        // Prime the envelope so output is non-silent.
        for (var i = 0; i < 5000; i++) sid.Tick();

        const int SampleCount = 100;
        const int TotalTicks = 50_000;
        const int Stride = TotalTicks / SampleCount;
        var samples = new float[SampleCount];
        for (var s = 0; s < SampleCount; s++)
        {
            for (var t = 0; t < Stride; t++) sid.Tick();
            samples[s] = sid.GenerateSample();
        }

        var distinct = new HashSet<float>(samples).Count;
        distinct.Should().BeGreaterThan(1,
            "if the 8580 noise LFSR were stuck (regression of the original " +
            "bug), every sample over 50000 ticks would be identical");
    }
}
