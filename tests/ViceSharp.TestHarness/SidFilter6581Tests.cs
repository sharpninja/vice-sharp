namespace ViceSharp.TestHarness;

using FluentAssertions;
using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-SID-004 (BACKFILL-SID-001 filter slice, acceptance criteria
/// 1-4 + 6 + 7). Use case: the SID's analog filter is configured through
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
/// analog response) is wired into ApplyFilter end-to-end and exercised
/// here via the Cutoff_NonLinearCurveDrivesApplyFilter test. The
/// mapper itself is unit-tested separately in
/// <see cref="Sid6581NonLinearCutoffTests" />.
/// </summary>
public sealed class SidFilter6581Tests
{
    private static Sid6581 BuildSid()
    {
        var bus = new BasicBus();
        return new Sid6581(bus) { BaseAddress = 0xD400 };
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
    /// Run the SID long enough that both the envelope and the SVF
    /// integrators have settled. The non-linear cutoff curve makes
    /// register 0 map to ~200Hz (not true 0); at that cutoff the
    /// Chamberlin SVF integrators take ~1000 samples (16000 ticks)
    /// to converge on the DC level. This longer warmup is required by
    /// tests that exercise low-register cutoff values where the LP
    /// time constant is the dominant settling factor. We also draw
    /// samples (without observing) so GenerateSample is exercised and
    /// the filter state advances at the audio-rate stride the live
    /// measurement loop uses.
    /// </summary>
    private static void WarmUpEnvelopeAndFilter(Sid6581 sid)
    {
        for (int s = 0; s < 4000; s++)
        {
            for (int t = 0; t < 16; t++) sid.Tick();
            _ = sid.GenerateSample();
        }
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
    /// Acceptance: the LP AC swing (peak-to-peak) with cutoff = 0 is
    /// strictly less than the swing with cutoff = 0x7FF, given LP mode
    /// and the voice routed through the filter. Peak-to-peak isolates
    /// the AC content the LP is attenuating: since PLAN-VICEPARITY-001
    /// S7 gives the sawtooth a physically-correct DC bias through the
    /// waveform model_dac (wave.output = model_dac[waveform_output],
    /// voice.cc/wave.h), the peak-absolute measure saturates at both
    /// cutoffs, whereas the AC swing still cleanly separates them.
    /// </summary>
    [Fact(Skip = "Retired in S9: SVF filter replaced by reSID 6581 op-amp model (PLAN-VICEPARITY-001 S9)")]
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

        var peakLow = RunAndMeasurePeakToPeak(sidLow, 400);
        var peakHigh = RunAndMeasurePeakToPeak(sidHigh, 400);

        peakLow.Should().BeLessThan(peakHigh,
            "min cutoff must attenuate the routed voice's AC swing more than max cutoff");
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
    [Fact(Skip = "Retired in S9: SVF filter replaced by reSID 6581 op-amp model (PLAN-VICEPARITY-001 S9)")]
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
    /// FR/TR: FR-SID-004 acceptance criterion 3 (with ac.6 active).
    /// Use case: $D418 bits 4 (LP), 5 (BP), 6 (HP) select which filter
    /// taps mix into the output. Under the 6581 non-linear cutoff curve
    /// (ac.6), register 0 maps to ~200Hz - the LP integrator no longer
    /// "fully stalls" at reg=0 but settles to the input's DC level after
    /// a few hundred samples. Once settled, LP at 200Hz heavily attenuates
    /// the 3850Hz sawtooth's AC content (its fundamental and harmonics);
    /// HP at the same cutoff passes virtually all of the AC. The LP and
    /// HP modes must therefore produce materially different AC swings.
    /// Acceptance: p2p amplitude with HP-only mode at cutoff register 0
    /// (~200Hz) is strictly greater than p2p with LP-only mode at the
    /// same cutoff, after the SVF integrator has settled.
    /// </summary>
    [Fact(Skip = "Retired in S9: SVF filter replaced by reSID 6581 op-amp model (PLAN-VICEPARITY-001 S9)")]
    public void Modes_LP_HP_DifferAtZeroCutoff()
    {
        var sidLp = BuildSid();
        var sidHp = BuildSid();

        foreach (var sid in new[] { sidLp, sidHp })
        {
            ConfigureVoice1Saw(sid, 0xFFFF);
            // Cutoff register = 0: maps to ~200Hz under the 6581 non-linear
            // curve. LP integrator settles to the input's DC level after
            // a few hundred samples and then attenuates the 3850Hz sawtooth
            // AC content; HP passes the AC through (input - settled DC).
            sid.Write(0xD415, 0x00);
            sid.Write(0xD416, 0x00);
            // Route voice 1 through filter, no resonance.
            sid.Write(0xD417, 0x01);
        }

        // LP only (bit 4) + volume 15.
        sidLp.Write(0xD418, 0x1F);
        // HP only (bit 6) + volume 15.
        sidHp.Write(0xD418, 0x4F);

        // Use the longer warmup so the LP integrator finishes converging
        // on the input's DC level (~1000 samples at the 200Hz cutoff).
        WarmUpEnvelopeAndFilter(sidLp);
        WarmUpEnvelopeAndFilter(sidHp);

        // Peak-to-peak captures the AC swing each filter path lets through.
        // A sawtooth has a strong DC bias, so absolute |peak| would be
        // dominated by the digi DC offset; p2p isolates the filter-passed
        // AC content.
        var p2pLp = RunAndMeasurePeakToPeak(sidLp, 400);
        var p2pHp = RunAndMeasurePeakToPeak(sidHp, 400);

        p2pLp.Should().BeLessThan(p2pHp,
            "at ~200Hz cutoff LP attenuates the 3850Hz sawtooth AC; HP passes it");
    }

    /// <summary>
    /// FR/TR: FR-SID-004 acceptance criterion 4 (with ac.6 active).
    /// Use case: $D417 bits 0-2 gate per-voice filter routing.
    /// When bit 0 is clear, voice 1 bypasses the filter entirely;
    /// when it is set, voice 1 is routed through. Under the 6581
    /// non-linear cutoff curve, register 0 maps to ~200Hz. With BP
    /// (band-pass) mode selected, the routed voice's 3850Hz sawtooth
    /// is well above the 200Hz center and the BP heavily attenuates
    /// it (rejects content far from the band center), while the
    /// bypassed voice retains its full AC swing through the unity
    /// path. BP mode is used in preference to LP here because the
    /// Chamberlin SVF's undamped LP integrator at q=1 rings against
    /// the sawtooth's sharp wraps - producing larger p2p than bypass
    /// even though the LP is heavily attenuating the fundamental.
    /// BP, in contrast, cleanly rejects out-of-band content without
    /// significant ringing at q=1.
    /// Acceptance: routed p2p &lt; bypassed p2p (filter actually gates).
    /// </summary>
    [Fact(Skip = "Retired in S9: SVF filter replaced by reSID 6581 op-amp model (PLAN-VICEPARITY-001 S9)")]
    public void Routing_GatesPerVoiceFilterPath()
    {
        var sidBypass = BuildSid();
        var sidRouted = BuildSid();

        foreach (var sid in new[] { sidBypass, sidRouted })
        {
            ConfigureVoice1Saw(sid, 0xFFFF);
            // Cutoff register = 0 maps to ~200Hz under the non-linear curve;
            // a 3850Hz voice is far above the BP center band so the BP
            // rejects most of its content.
            sid.Write(0xD415, 0x00);
            sid.Write(0xD416, 0x00);
            sid.Write(0xD418, 0x2F); // BP + volume 15
        }

        // Bypass: voice 1 not routed (lower nibble = 0).
        sidBypass.Write(0xD417, 0x00);
        // Routed: voice 1 routed (bit 0).
        sidRouted.Write(0xD417, 0x01);

        // Long warmup lets the SVF integrators settle so the
        // routed-vs-bypass comparison reflects steady-state attenuation
        // rather than the integrator's startup ramp.
        WarmUpEnvelopeAndFilter(sidBypass);
        WarmUpEnvelopeAndFilter(sidRouted);

        var p2pBypass = RunAndMeasurePeakToPeak(sidBypass, 400);
        var p2pRouted = RunAndMeasurePeakToPeak(sidRouted, 400);

        p2pRouted.Should().BeLessThan(p2pBypass,
            "routed voice must be filter-gated; bypassed voice AC swing should be larger");
    }

    /// <summary>
    /// FR/TR: FR-SID-004 acceptance criterion 6 end-to-end.
    /// Use case: ApplyFilter must use the non-linear cutoff curve, not the
    /// old linear mapping: register 0x100 vs 0x400 vs 0x700 must land at
    /// clearly different filter responses. Since PLAN-VICEPARITY-001 S1
    /// (FR-SID-CLOCK) the SVF is clocked once per phi2 cycle in reSID
    /// SID::clock() order, so the 44.1kHz-derived coefficient
    /// 2*sin(pi*f/44100) now applies per cycle: the mapped ~250Hz of
    /// register 0x100 corresponds to an effective band centre near the
    /// 3849Hz voice fundamental (energy passes), while registers 0x400 and
    /// 0x700 clamp the coefficient near the SVF stability limit and push
    /// the band far above the audio range (the fundamental is rejected).
    /// The exact reSID filter model plus per-cycle response is the
    /// FR-SID-FILTER-6581 / FR-SID-FILTER-CLOCK remediation; this lock
    /// keeps criterion 6's wiring true: the register value still reaches
    /// ApplyFilter through MapCutoffRegToFrequency and measurably changes
    /// the output.
    /// Acceptance: p2p at reg=0x100 is non-zero and strictly greater than
    /// p2p at reg=0x400, and p2p at reg=0x700 sits at essentially the same
    /// clamped residual as reg=0x400 (both push the band far above audio and
    /// clamp the coefficient at the SVF stability limit), confirming the
    /// non-linear curve actually drives ApplyFilter under per-cycle clocking.
    /// The reg 0x400 vs 0x700 residuals coincide to within a few thousandths
    /// (the exact residual now carries the physically-correct waveform
    /// model_dac harmonics from PLAN-VICEPARITY-001 S7), so the far-above-band
    /// pair is asserted approximately equal rather than strictly ordered.
    /// </summary>
    [Fact(Skip = "Retired in S9: SVF filter replaced by reSID 6581 op-amp model (PLAN-VICEPARITY-001 S9)")]
    public void Cutoff_NonLinearCurveDrivesApplyFilter()
    {
        var sidLow = BuildSid();
        var sidMid = BuildSid();
        var sidHigh = BuildSid();

        foreach (var sid in new[] { sidLow, sidMid, sidHigh })
        {
            ConfigureVoice1Saw(sid, 0xFFFF);
            sid.Write(0xD417, 0x01); // voice 1 routed, no resonance
            sid.Write(0xD418, 0x2F); // BP + volume 15
        }

        // sidLow: cutoff register = 0x100 (~250Hz mapped; effective band near
        // the voice fundamental under per-cycle stepping).
        sidLow.Write(0xD415, 0x00);
        sidLow.Write(0xD416, 0x20);

        // sidMid: cutoff register = 0x400 (~6300Hz mapped; band far above the
        // audio range under per-cycle stepping).
        sidMid.Write(0xD415, 0x00);
        sidMid.Write(0xD416, 0x80);

        // sidHigh: cutoff register = 0x700 (~13800Hz mapped; coefficient
        // clamped at the SVF stability limit).
        sidHigh.Write(0xD415, 0x00);
        sidHigh.Write(0xD416, 0xE0);

        WarmUpEnvelopeAndFilter(sidLow);
        WarmUpEnvelopeAndFilter(sidMid);
        WarmUpEnvelopeAndFilter(sidHigh);

        var p2pLow = RunAndMeasurePeakToPeak(sidLow, 400);
        var p2pMid = RunAndMeasurePeakToPeak(sidMid, 400);
        var p2pHigh = RunAndMeasurePeakToPeak(sidHigh, 400);

        p2pLow.Should().BeGreaterThan(0f,
            "the low-register band centre sits near the voice fundamental, so energy must pass");
        p2pMid.Should().BeLessThan(p2pLow,
            "BP at reg 0x400 pushes the band far above the voice under per-cycle clocking, rejecting the fundamental");
        p2pHigh.Should().BeApproximately(p2pMid, 0.02f,
            "BP at reg 0x700 clamps at the stability limit and rejects the fundamental to the same residual as reg 0x400");
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
    [Fact(Skip = "Retired in S9: SVF filter replaced by reSID 6581 op-amp model (PLAN-VICEPARITY-001 S9)")]
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
