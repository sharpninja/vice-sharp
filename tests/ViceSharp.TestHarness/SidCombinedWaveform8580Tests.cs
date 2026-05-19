namespace ViceSharp.TestHarness;

using FluentAssertions;
using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-SID-003 acceptance criterion 2 (BACKFILL-SID-001 / 8580 combined
/// waveform variant). Use case: The 8580 SID's analog die has different
/// combined-waveform bleed than the 6581; for the same register stimulus,
/// the 8580 produces a quieter, slightly different combined output. This
/// slice models the difference as a fixed attenuation scalar applied to
/// the AND-combine result of two or more selected waveforms. Single
/// waveform output remains identical across both die revisions because
/// the analog bleed only manifests when multiple waveform outputs are
/// driving the internal node simultaneously.
/// </summary>
public sealed class SidCombinedWaveform8580Tests
{
    // SID voice 1 register offsets (relative to $D400).
    private const ushort V1FreqLo = 0xD400;
    private const ushort V1FreqHi = 0xD401;
    private const ushort V1PwLo = 0xD402;
    private const ushort V1PwHi = 0xD403;
    private const ushort V1Ctrl = 0xD404;
    private const ushort V1Ad = 0xD405;
    private const ushort V1Sr = 0xD406;
    private const ushort Volume = 0xD418;

    // Control register bits (SID hardware layout).
    private const byte Gate = 0x01;
    private const byte Triangle = 0x10;
    private const byte Sawtooth = 0x20;
    private const byte Pulse = 0x40;
    private const byte Noise = 0x80;

    private static T BuildPrimedSid<T>() where T : Sid6581
    {
        var sid = (T)System.Activator.CreateInstance(typeof(T), new BasicBus())!;
        // Master volume max so envelope-scaled output is observable.
        sid.Write(Volume, 0x0F);
        // Voice 1 frequency = max so each Tick advances the accumulator by
        // 0xFFFF; after ~256 Ticks the top-byte phase has swept the full
        // range and the per-waveform outputs visit non-trivial values.
        sid.Write(V1FreqLo, 0xFF);
        sid.Write(V1FreqHi, 0xFF);
        // Pulse width = 0x0800 means pulse is HIGH when the top-byte phase
        // is below 0x08 (small slice). Phases >= 0x08 give pulse-low (0x00),
        // which lets the AND-mask tests observe the pulse-low case clearly.
        sid.Write(V1PwLo, 0x00);
        sid.Write(V1PwHi, 0x08);
        // Attack=0, Decay=0 (fastest); Sustain=15 so envelope sits at 0xFF.
        sid.Write(V1Ad, 0x00);
        sid.Write(V1Sr, 0xF0);
        return sid;
    }

    private static void PrimeEnvelope(Sid6581 sid)
    {
        // Run enough ticks for the envelope to reach sustain (0xFF) on both
        // SID models. 6581 attack rate index 0 = 9 cycles/step; 8580 attack
        // rate index 0 = 14 cycles/step. 14 * 255 = 3570 cycles for the
        // 8580 to fully ramp; 5000 ticks gives safe margin for both.
        for (int i = 0; i < 5000; i++) sid.Tick();
    }

    private static float SampleAtPhase<T>(byte ctrl, int extraTicks) where T : Sid6581
    {
        var sid = BuildPrimedSid<T>();
        sid.Write(V1Ctrl, (byte)(ctrl | Gate));
        PrimeEnvelope(sid);
        for (int i = 0; i < extraTicks; i++) sid.Tick();
        return sid.GenerateSample();
    }

    private static float MaxAbsOverSweep<T>(byte ctrl, int ticks) where T : Sid6581
    {
        var sid = BuildPrimedSid<T>();
        sid.Write(V1Ctrl, (byte)(ctrl | Gate));
        PrimeEnvelope(sid);
        float maxAbs = 0f;
        for (int i = 0; i < ticks; i++)
        {
            sid.Tick();
            var s = System.Math.Abs(sid.GenerateSample());
            if (s > maxAbs) maxAbs = s;
        }
        return maxAbs;
    }

    // Sweep length large enough that a 32-bit accumulator at frequency
    // 0xFFFF visits every top-byte phase value.
    private const int FullPhaseSweepTicks = 65_536;

    /// <summary>
    /// FR/TR: FR-SID-003 acceptance criterion 2 (BACKFILL-SID-001 / 8580 variant).
    /// Use case: Single-waveform output on 8580 must match 6581 - the 8580
    /// combined-bleed attenuation only kicks in when two or more waveform
    /// bits are set (the analog bleed comes from multiple waveform outputs
    /// driving the same node). A single waveform doesn't go through the
    /// AND-combine path, so it sees no attenuation.
    /// Acceptance: For voice 1 triangle only, the maximum absolute sample
    /// over a full phase sweep on 8580 equals (within filter tolerance) the
    /// max on 6581.
    /// </summary>
    [Fact]
    public void SingleWaveform_8580_MatchesSid6581()
    {
        // Single triangle waveform - no combined-bleed path engaged.
        // Both implementations should produce the same output since
        // the only difference is in the multi-waveform AND path.
        // However, the 8580 has a different filter implementation, but
        // since the filter is bypassed (no filter bits set), the only
        // difference would come from the combined-bleed path - which is
        // not engaged for a single waveform.
        float max6581 = MaxAbsOverSweep<Sid6581>(Triangle, FullPhaseSweepTicks);
        float max8580 = MaxAbsOverSweep<Sid8580>(Triangle, FullPhaseSweepTicks);

        max6581.Should().BeGreaterThan(0.0f, "triangle baseline must produce output on 6581");
        max8580.Should().BeGreaterThan(0.0f, "triangle baseline must produce output on 8580");
        // Single waveform path: outputs must be equal (no combined-bleed).
        max8580.Should().BeApproximately(max6581, 1e-5f,
            "single-waveform output is identical across 6581 and 8580 (no combined-bleed path)");
    }

    /// <summary>
    /// FR/TR: FR-SID-003 acceptance criterion 2 (BACKFILL-SID-001 / 8580 variant).
    /// Use case: Combined triangle + sawtooth on the 8580 must be quieter
    /// than the same combination on the 6581 because the 8580 die's analog
    /// bleed attenuates the combined-waveform internal node before it
    /// reaches the mixer.
    /// Acceptance: Maximum absolute sample over a full phase sweep for
    /// the combined triangle+sawtooth waveform is strictly less on the
    /// 8580 than on the 6581 (attenuated), and strictly greater than
    /// half of the 6581's max (attenuated, not silenced).
    /// </summary>
    [Fact]
    public void CombinedTriangleSawtooth_8580_IsQuieterThan6581()
    {
        float max6581 = MaxAbsOverSweep<Sid6581>((byte)(Triangle | Sawtooth), FullPhaseSweepTicks);
        float max8580 = MaxAbsOverSweep<Sid8580>((byte)(Triangle | Sawtooth), FullPhaseSweepTicks);

        max6581.Should().BeGreaterThan(0.0f,
            "control: 6581 combined Tri+Saw must have non-zero peak amplitude");
        max8580.Should().BeGreaterThan(0.0f,
            "8580 attenuation must not silence the combined output entirely");
        max8580.Should().BeLessThan(max6581,
            "8580 die-revision bleed attenuates combined-waveform output below 6581 level");
        max8580.Should().BeGreaterThan(max6581 * 0.5f,
            "8580 attenuation is modest (~0.75 scalar), not a full halving");
    }

    /// <summary>
    /// FR/TR: FR-SID-003 acceptance criterion 2 (BACKFILL-SID-001 / 8580 variant).
    /// Use case: The canonical "fat lead" pulse + sawtooth combined
    /// waveform on the 8580 must show the same attenuation pattern as
    /// triangle + sawtooth: quieter than 6581 but not silenced. Tested
    /// across the full phase sweep to find peak amplitude.
    /// Acceptance: Maximum absolute voice contribution (relative to DAC
    /// baseline) on 8580 over a full sweep is strictly less than the same
    /// max on 6581, and strictly greater than half of it.
    /// </summary>
    [Fact]
    public void CombinedPulseSawtooth_8580_IsAttenuated()
    {
        float max6581 = MaxAbsOverSweep<Sid6581>((byte)(Pulse | Sawtooth), FullPhaseSweepTicks);
        float max8580 = MaxAbsOverSweep<Sid8580>((byte)(Pulse | Sawtooth), FullPhaseSweepTicks);

        max6581.Should().BeGreaterThan(0.0f,
            "control: 6581 pulse+sawtooth must have non-zero peak amplitude");
        max8580.Should().BeGreaterThan(0.0f,
            "8580 pulse+sawtooth must not be silenced");
        max8580.Should().BeLessThan(max6581,
            "8580 die-revision bleed attenuates combined pulse+sawtooth peak below 6581");
        max8580.Should().BeGreaterThan(max6581 * 0.5f,
            "8580 attenuation is modest (~0.75), not a full halving");
    }

    /// <summary>
    /// FR/TR: FR-SID-003 acceptance criterion 2 (BACKFILL-SID-001 / 8580 variant).
    /// Use case: All four waveforms (Triangle + Sawtooth + Pulse + Noise)
    /// produce a heavily attenuated AND-combined output. On the 8580, the
    /// combined-bleed path further attenuates this. To escape integer
    /// quantization in the downstream /3 mixer, this test uses a wider
    /// pulse window (PW=0xF00) so that the AND-combined byte values reach
    /// magnitudes where the 3/4 attenuation is observable in the float mix.
    /// Acceptance: Sum of absolute voice contribution (relative to DAC
    /// baseline) over the full sweep is non-zero on both models, and
    /// 8580's sum is strictly less than 6581's.
    /// </summary>
    [Fact]
    public void AllFourWaveforms_8580_AttenuatedBelow6581()
    {
        float SumAbsVoiceOverSweep<T>(byte ctrl) where T : Sid6581
        {
            var sid = BuildPrimedSid<T>();
            // Wider pulse window: pulse HIGH for phases 0..0x0F (PW=0xF00).
            // This exposes more non-zero AND-combined bytes than the
            // default PW=0x800.
            sid.Write(V1PwLo, 0x00);
            sid.Write(V1PwHi, 0x0F);
            // Baseline (no waveform) reference.
            var baselineSid = (T)System.Activator.CreateInstance(typeof(T), new BasicBus())!;
            baselineSid.Write(Volume, 0x0F);
            for (int i = 0; i < 5000; i++) baselineSid.Tick();
            float baseline = baselineSid.GenerateSample();
            sid.Write(V1Ctrl, (byte)(ctrl | Gate));
            PrimeEnvelope(sid);
            double sumAbs = 0.0;
            for (int i = 0; i < FullPhaseSweepTicks; i++)
            {
                sid.Tick();
                sumAbs += System.Math.Abs(sid.GenerateSample() - baseline);
            }
            return (float)sumAbs;
        }

        byte ctrl = (byte)(Triangle | Sawtooth | Pulse | Noise);
        float sum6581 = SumAbsVoiceOverSweep<Sid6581>(ctrl);
        float sum8580 = SumAbsVoiceOverSweep<Sid8580>(ctrl);

        sum6581.Should().BeGreaterThan(0.0f,
            "control: 6581 all-four AND must produce non-zero cumulative output");
        sum8580.Should().BeGreaterThan(0.0f,
            "8580 all-four AND must not be silenced by the bleed scalar");
        sum8580.Should().BeLessThan(sum6581,
            "8580 die-revision bleed attenuates cumulative all-four-combined output below 6581");
    }

    /// <summary>
    /// FR/TR: FR-SID-003 acceptance criterion 2 (BACKFILL-SID-001 / 8580 variant).
    /// Use case: When no waveform bits are selected (gate-only) there is
    /// no combined-waveform path engaged. Both 6581 and 8580 produce the
    /// same DC offset (the $D418 volume-DAC baseline from FR-SID-010).
    /// The 8580 attenuation only modifies the combined-AND result; it
    /// does not change the DAC baseline.
    /// Acceptance: With CTRL = gate only, the steady-state sample on
    /// 8580 equals the steady-state sample on 6581 within float epsilon.
    /// </summary>
    [Fact]
    public void NoWaveformSelected_8580_SameBaselineAs6581()
    {
        const int Ticks = 1000;
        float baseline6581 = SampleAtPhase<Sid6581>(0x00, Ticks);
        float baseline8580 = SampleAtPhase<Sid8580>(0x00, Ticks);

        baseline8580.Should().BeApproximately(baseline6581, 1e-5f,
            "no-waveform DC baseline must match across 6581 and 8580 (DAC offset only, no combined path)");
    }
}
