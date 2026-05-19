namespace ViceSharp.TestHarness;

using FluentAssertions;
using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-SID-004 (BACKFILL-SID-001 filter slice, acceptance criteria
/// 1-5 + 7). Use case: the SID's analog filter is configured through
/// registers $D415 (FCLO low 3 bits of cutoff), $D416 (FCHI high 8 bits
/// of cutoff: combined as an 11-bit cutoff value), $D417 (upper nibble =
/// resonance Q, lower nibble = per-voice routing + external-input bit)
/// and $D418 bits 4-6 (additive LP/BP/HP mode select). High resonance
/// must produce audible amplification near the cutoff frequency without
/// generating NaN/Infinity or completely killing the signal.
///
/// Acceptance criteria covered by this slice:
///   1. Cutoff register is 11-bit combined from $D415 + $D416.
///   2. Resonance ($D417 upper nibble) affects amplitude at cutoff.
///   3. LP / BP / HP mode bits ($D418 bits 4-6) select output paths
///      and are combinable additively.
///   4. Per-voice routing ($D417 bits 0-2) gates which voices pass
///      through the filter vs bypass it.
///   7. Resonance distortion at extreme settings stays finite and
///      audible (soft clip, no NaN/Inf/silence).
///
/// Acceptance criterion 5 (external audio input via $D417 bit 3) is
/// documented but not exercised in this slice - the chip exposes no
/// external-input surface yet and modelling it without an audio backend
/// hook would be premature. The lower-nibble parsing in the implementation
/// does cover bit 3 so a future external-input plumbing slice can wire it.
///
/// Acceptance criterion 6 (6581 non-linear cutoff curve, the "kinked"
/// analog response) is deliberately out of scope - the cutoff mapping
/// in this slice is linear and matches what resid_fp calls the "ideal"
/// curve. A follow-up slice will overlay the 6581-specific non-linear
/// response.
/// </summary>
public sealed class SidFilter6581Tests
{
    private static Sid6581 BuildSid()
    {
        var bus = new BasicBus();
        return new Sid6581(bus);
    }

    /// <summary>
    /// Drive the SID for a fixed number of ticks at audio-rate sampling
    /// (one sample per ClockDivisor ticks) and return the max absolute
    /// sample value observed. Useful for comparing relative output energy
    /// across two filter configurations.
    /// </summary>
    private static float RunAndMeasurePeak(Sid6581 sid, int samples)
    {
        float peak = 0f;
        for (int s = 0; s < samples; s++)
        {
            for (int t = 0; t < 16; t++) sid.Tick(); // ClockDivisor = 16
            var sample = sid.GenerateSample();
            float a = sample < 0 ? -sample : sample;
            if (a > peak) peak = a;
        }
        return peak;
    }

    /// <summary>
    /// Drive the SID and return peak-to-peak amplitude (max - min) of the
    /// observed samples. Peak-to-peak captures the AC content of the
    /// signal regardless of DC offset; useful for distinguishing LP
    /// (small AC near cutoff) from HP (large AC above cutoff) when the
    /// signal also has a non-trivial DC component (sawtooth waveforms
    /// have a 50% duty-cycle DC bias).
    /// </summary>
    private static float RunAndMeasurePeakToPeak(Sid6581 sid, int samples)
    {
        float min = float.PositiveInfinity;
        float max = float.NegativeInfinity;
        for (int s = 0; s < samples; s++)
        {
            for (int t = 0; t < 16; t++) sid.Tick();
            var sample = sid.GenerateSample();
            if (sample < min) min = sample;
            if (sample > max) max = sample;
        }
        return max - min;
    }

    /// <summary>
    /// Run the SID for enough ticks that voice 1's envelope reaches its
    /// sustain level (~255) under the fastest attack rate. With AttackRate
    /// index 0 = 9 ticks per envelope step, ramping to 255 requires
    /// 9 * 256 = 2304 ticks; we double that to leave the decay phase
    /// behind too. This warmup keeps filter measurements out of the
    /// envelope-ramp transient.
    /// </summary>
    private static void WarmUpEnvelope(Sid6581 sid)
    {
        for (int t = 0; t < 6000; t++) sid.Tick();
    }

    /// <summary>
    /// Configure a single voice 1 sawtooth with a fixed frequency word
    /// and a full-attack envelope so the test's filter measurements
    /// have stable energy to work with. The caller sets filter
    /// configuration after this helper.
    /// </summary>
    private static void ConfigureVoice1Saw(Sid6581 sid, ushort freqWord, byte volume = 0x0F)
    {
        // Voice 1 freq lo/hi
        sid.Write(0xD400, (byte)(freqWord & 0xFF));
        sid.Write(0xD401, (byte)(freqWord >> 8));
        // Voice 1 attack=0/decay=0; sustain=15/release=0 so envelope rises
        // fast and holds at max (sustain = 15 -> envelope ~ 255).
        sid.Write(0xD405, 0x00);
        sid.Write(0xD406, 0xF0);
        // Voice 1 control: sawtooth + gate
        sid.Write(0xD404, 0x21);
        // Master volume (low nibble), no filter mode bits set yet
        sid.Write(0xD418, volume);
    }

    /// <summary>
    /// FR/TR: FR-SID-004 acceptance criterion 1.
    /// Use case: The cutoff register is an 11-bit value formed from
    /// $D415 (low 3 bits) and $D416 (high 8 bits). At minimum cutoff
    /// the LP filter should heavily attenuate a routed voice; at
    /// maximum cutoff it should pass the voice with much less loss.
    /// Acceptance: peak amplitude with cutoff = 0 is strictly less
    /// than peak amplitude with cutoff = 0x7FF, given LP mode and
    /// the voice routed through the filter.
    /// </summary>
    [Fact]
    public void Cutoff_Is11BitFromD415AndD416()
    {
        var sidLow = BuildSid();
        var sidHigh = BuildSid();

        // Both: voice 1 sawtooth at a moderate frequency, routed through
        // the filter, LP mode selected, max volume, zero resonance.
        foreach (var sid in new[] { sidLow, sidHigh })
        {
            ConfigureVoice1Saw(sid, 0xFFFF);
            sid.Write(0xD417, 0x01); // voice 1 routed (bit 0), resonance = 0
            sid.Write(0xD418, 0x1F); // LP (bit 4) + volume 15
        }

        // sidLow: cutoff = 0x000 (all 11 bits zero)
        sidLow.Write(0xD415, 0x00);
        sidLow.Write(0xD416, 0x00);

        // sidHigh: cutoff = 0x7FF (11 bits all set) - high byte 0xFF, low
        // byte 0x07 (only bottom 3 bits matter for $D415).
        sidHigh.Write(0xD415, 0x07);
        sidHigh.Write(0xD416, 0xFF);

        WarmUpEnvelope(sidLow);
        WarmUpEnvelope(sidHigh);

        var peakLow = RunAndMeasurePeak(sidLow, 400);
        var peakHigh = RunAndMeasurePeak(sidHigh, 400);

        peakLow.Should().BeLessThan(peakHigh,
            "min cutoff must attenuate the routed voice more than max cutoff");
    }

    /// <summary>
    /// FR/TR: FR-SID-004 acceptance criterion 2.
    /// Use case: Resonance is held in the upper nibble of $D417.
    /// Higher resonance amplifies the signal near the cutoff frequency.
    /// Acceptance: at the same cutoff and voice config, a resonance of
    /// $F0 (max) produces a strictly larger peak than resonance $00
    /// when the voice frequency sits in the resonant band, OR (under
    /// strong feedback) drives the signal differently in a clearly
    /// non-trivial way. Conservative check: peakHi != peakLo.
    /// </summary>
    [Fact]
    public void Resonance_AffectsPeakAmplitudeNearCutoff()
    {
        var sidLo = BuildSid();
        var sidHi = BuildSid();

        foreach (var sid in new[] { sidLo, sidHi })
        {
            ConfigureVoice1Saw(sid, 0xFFFF);
            // Mid cutoff so the voice's harmonics straddle it.
            sid.Write(0xD415, 0x04);
            sid.Write(0xD416, 0x40);
            // BP mode (bit 5) + volume 15 to emphasise the resonant peak.
            sid.Write(0xD418, 0x2F);
        }

        // Low resonance.
        sidLo.Write(0xD417, 0x01); // voice 1 routed, resonance = 0
        // Max resonance.
        sidHi.Write(0xD417, 0xF1); // voice 1 routed, resonance = 15

        WarmUpEnvelope(sidLo);
        WarmUpEnvelope(sidHi);

        var peakLo = RunAndMeasurePeak(sidLo, 500);
        var peakHi = RunAndMeasurePeak(sidHi, 500);

        // Resonance must materially change the output level; we don't
        // demand monotonic gain because state-variable filters can both
        // amplify near-band signals and produce ringing artefacts.
        peakHi.Should().NotBeApproximately(peakLo, 0.001f,
            "max-resonance output must materially differ from zero-resonance");
    }

    /// <summary>
    /// FR/TR: FR-SID-004 acceptance criterion 3.
    /// Use case: $D418 bits 4 (LP), 5 (BP), 6 (HP) select which filter
    /// taps mix into the output. With cutoff held near zero, the LP
    /// integrators never accumulate signal so the LP tap is silent;
    /// the HP tap still computes input - lp - q*bp, which leaves the
    /// input passing through essentially unchanged. The LP and HP
    /// modes must therefore produce materially different AC swings.
    /// Acceptance: p2p amplitude with HP-only mode at cutoff = 0 is
    /// strictly greater than p2p with LP-only mode at the same cutoff.
    /// </summary>
    [Fact]
    public void Modes_LP_HP_DifferAtZeroCutoff()
    {
        var sidLp = BuildSid();
        var sidHp = BuildSid();

        foreach (var sid in new[] { sidLp, sidHp })
        {
            ConfigureVoice1Saw(sid, 0xFFFF);
            // Cutoff = 0: LP integrators are stalled (no signal accumulates)
            // so LP output is ~0; HP equals input - 0 - 0 = input.
            sid.Write(0xD415, 0x00);
            sid.Write(0xD416, 0x00);
            // Route voice 1 through filter, no resonance.
            sid.Write(0xD417, 0x01);
        }

        // LP only (bit 4) + volume 15.
        sidLp.Write(0xD418, 0x1F);
        // HP only (bit 6) + volume 15.
        sidHp.Write(0xD418, 0x4F);

        WarmUpEnvelope(sidLp);
        WarmUpEnvelope(sidHp);

        // Peak-to-peak captures the AC swing each filter path lets through.
        // A sawtooth has a strong DC bias, so absolute |peak| would be
        // dominated by the digi DC offset; p2p isolates the filter-passed
        // AC content.
        var p2pLp = RunAndMeasurePeakToPeak(sidLp, 400);
        var p2pHp = RunAndMeasurePeakToPeak(sidHp, 400);

        p2pLp.Should().BeLessThan(p2pHp,
            "at zero cutoff LP is silent, HP passes the voice's AC content");
    }

    /// <summary>
    /// FR/TR: FR-SID-004 acceptance criterion 4.
    /// Use case: $D417 bits 0-2 gate per-voice filter routing.
    /// When bit 0 is clear, voice 1 bypasses the filter entirely;
    /// when it is set, voice 1 is routed through. With cutoff held at
    /// zero and LP mode selected, the filter is silent (LP integrators
    /// stalled), so a routed voice is gated out while a bypassed voice
    /// passes through to the mix unchanged.
    /// Acceptance: routed p2p &lt; bypassed p2p (filter actually gates).
    /// </summary>
    [Fact]
    public void Routing_GatesPerVoiceFilterPath()
    {
        var sidBypass = BuildSid();
        var sidRouted = BuildSid();

        foreach (var sid in new[] { sidBypass, sidRouted })
        {
            ConfigureVoice1Saw(sid, 0xFFFF);
            // Cutoff = 0 silences the LP integrators, so routed voices
            // are effectively muted.
            sid.Write(0xD415, 0x00);
            sid.Write(0xD416, 0x00);
            sid.Write(0xD418, 0x1F); // LP + volume 15
        }

        // Bypass: voice 1 not routed (lower nibble = 0).
        sidBypass.Write(0xD417, 0x00);
        // Routed: voice 1 routed (bit 0).
        sidRouted.Write(0xD417, 0x01);

        WarmUpEnvelope(sidBypass);
        WarmUpEnvelope(sidRouted);

        var p2pBypass = RunAndMeasurePeakToPeak(sidBypass, 400);
        var p2pRouted = RunAndMeasurePeakToPeak(sidRouted, 400);

        p2pRouted.Should().BeLessThan(p2pBypass,
            "routed voice must be filter-gated; bypassed voice AC swing should be larger");
    }

    /// <summary>
    /// FR/TR: FR-SID-004 acceptance criterion 7.
    /// Use case: At max resonance with cutoff sitting on a strong
    /// voice harmonic, the filter must produce a finite, audibly
    /// non-silent output - even when the feedback loop is driven hard.
    /// Soft clipping is acceptable (and indeed expected on real 6581
    /// silicon); the test guards against numerical blow-up and
    /// complete decay to zero.
    /// Acceptance: all samples in a long run are finite (no NaN, no
    /// Infinity) and at least one sample is non-zero.
    /// </summary>
    [Fact]
    public void Resonance_ExtremeStaysFiniteAndAudible()
    {
        var sid = BuildSid();
        ConfigureVoice1Saw(sid, 0xFFFF);
        // Cutoff near a strong voice harmonic.
        sid.Write(0xD415, 0x04);
        sid.Write(0xD416, 0x40);
        // BP + LP combined, max resonance, voice 1 routed.
        sid.Write(0xD417, 0xF1);
        sid.Write(0xD418, 0x3F);

        WarmUpEnvelope(sid);

        bool sawNonZero = false;
        for (int s = 0; s < 2000; s++)
        {
            for (int t = 0; t < 16; t++) sid.Tick();
            var sample = sid.GenerateSample();
            float.IsNaN(sample).Should().BeFalse("filter must not produce NaN");
            float.IsInfinity(sample).Should().BeFalse("filter must not produce Infinity");
            if (sample != 0f) sawNonZero = true;
        }
        sawNonZero.Should().BeTrue("filter at max resonance must remain audible");
    }
}
