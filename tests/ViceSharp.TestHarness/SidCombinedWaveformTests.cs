namespace ViceSharp.TestHarness;

using FluentAssertions;
using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-SID-003 (BACKFILL-SID-001 Slice 3).
/// Use case: SID waveform selection bits (CTRL bits 4-7: Triangle, Sawtooth,
/// Pulse, Noise) are not a multiplexer; selecting two or more bits causes
/// the SID to bitwise-AND the selected waveform outputs together, producing
/// the iconic distorted/attenuated combined waveforms used by countless C64
/// musicians (e.g. pulse+saw leads). With no waveform bit set the voice
/// output decays to silence.
/// </summary>
public sealed class SidCombinedWaveformTests
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

    private static Sid6581 BuildPrimedSid()
    {
        var sid = new Sid6581(new BasicBus());
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
        // Run enough ticks for the envelope to reach sustain (0xFF).
        // Attack rate index 0 jumps to 0xFF in a single Tick().
        for (int i = 0; i < 8; i++) sid.Tick();
    }

    private static float SampleAtPhase(byte ctrl, int extraTicks)
    {
        var sid = BuildPrimedSid();
        sid.Write(V1Ctrl, (byte)(ctrl | Gate));
        PrimeEnvelope(sid);
        for (int i = 0; i < extraTicks; i++) sid.Tick();
        return sid.GenerateSample();
    }

    // Sweep length large enough that a 32-bit accumulator at frequency
    // 0xFFFF visits every top-byte phase value: 65,536 ticks produces an
    // accumulator value of 0xFFFF0000, which gives every phase byte
    // 0x00..0xFF along the way.
    private const int FullPhaseSweepTicks = 65_536;

    /// <summary>
    /// FR/TR: FR-SID-003
    /// Use case: A voice with only the triangle waveform bit set produces a
    /// non-silent sample once the envelope has ramped to sustain. This is
    /// the single-waveform baseline that combined-waveform behaviour must
    /// not regress.
    /// Acceptance: After priming the envelope, GenerateSample on a triangle
    /// voice returns a non-zero finite sample at the accumulator phase that
    /// is known to produce non-zero triangle output.
    /// </summary>
    [Fact]
    public void SingleTriangle_ProducesNonZeroOutput()
    {
        var sid = BuildPrimedSid();
        sid.Write(V1Ctrl, (byte)(Triangle | Gate));
        PrimeEnvelope(sid);

        // Sweep the accumulator across all top-byte phase values; triangle
        // produces a non-zero sample whenever phase is not 0 or 255.
        bool sawNonZero = false;
        for (int i = 0; i < FullPhaseSweepTicks; i++)
        {
            sid.Tick();
            var s = sid.GenerateSample();
            float.IsNaN(s).Should().BeFalse();
            if (System.Math.Abs(s) > 0.0001f) { sawNonZero = true; break; }
        }
        sawNonZero.Should().BeTrue("a single triangle waveform must emit a non-zero sample within one full phase sweep");
    }

    /// <summary>
    /// FR/TR: FR-SID-003
    /// Use case: A voice with triangle and sawtooth bits set must emit a
    /// sample that is neither the standalone-triangle sample nor the
    /// standalone-sawtooth sample at the same accumulator phase, because
    /// the SID hardware ANDs the two waveform outputs together.
    /// Acceptance: At a phase whose triangle and sawtooth outputs share a
    /// limited set of bits (top-byte phase 0x33: triangle = 0x66, sawtooth
    /// = 0x33, AND = 0x22) the three values differ by a wide-enough margin
    /// to survive the SID's downstream integer scaling.
    /// </summary>
    [Fact]
    public void TriangleSawtooth_Combined_DiffersFromEachComponent()
    {
        // 13062 extra ticks plus the 8 prime ticks puts the accumulator at
        // 13070 * 0xFFFF = 0x33199B72, giving top-byte phase = 0x33.
        // Triangle output at phase 0x33 = 0x33 << 1 = 0x66 (102);
        // sawtooth output = 0x33 (51); AND of the two = 0x22 (34). The
        // three values differ by 17 and 68 respectively, plenty of slack
        // for the /3 integer mixer and float conversion downstream.
        const int Ticks = 13062;

        float triOnly = SampleAtPhase(Triangle, Ticks);
        float sawOnly = SampleAtPhase(Sawtooth, Ticks);
        float combined = SampleAtPhase((byte)(Triangle | Sawtooth), Ticks);

        // None should be NaN.
        float.IsNaN(triOnly).Should().BeFalse();
        float.IsNaN(sawOnly).Should().BeFalse();
        float.IsNaN(combined).Should().BeFalse();

        // All three samples are non-zero at this phase (control).
        System.Math.Abs(triOnly).Should().BeGreaterThan(0.0f,
            "control case: triangle alone must be non-zero at phase 0x33");
        System.Math.Abs(sawOnly).Should().BeGreaterThan(0.0f,
            "control case: sawtooth alone must be non-zero at phase 0x33");
        System.Math.Abs(combined).Should().BeGreaterThan(0.0f,
            "AND of triangle (0x66) and sawtooth (0x33) is 0x22, which is non-zero");

        // The combined output must not equal either source.
        System.Math.Abs(combined - triOnly).Should().BeGreaterThan(1e-5f,
            "combined Tri+Saw must AND-mask the triangle output, so it cannot equal triangle-only");
        System.Math.Abs(combined - sawOnly).Should().BeGreaterThan(1e-5f,
            "combined Tri+Saw must AND-mask the sawtooth output, so it cannot equal sawtooth-only");
    }

    /// <summary>
    /// FR/TR: FR-SID-003
    /// Use case: Pulse + Sawtooth is the canonical "fat lead" combined
    /// waveform on C64 (Commando, Last Ninja). When the pulse phase is
    /// high the AND with sawtooth passes the sawtooth value; when the
    /// pulse phase is low the AND zeroes everything. The combined output
    /// is therefore strictly less-than-or-equal to the sawtooth-only
    /// output sample-by-sample (and zero whenever pulse is low).
    /// Acceptance: At a phase where pulse is low the combined sample
    /// equals the $D418-DAC DC baseline (no voice contribution), even
    /// though the sawtooth-only sample at that phase has a clearly
    /// larger voice contribution above that baseline. FR-SID-010
    /// volume-DAC offset is the floor here, not absolute zero.
    /// </summary>
    [Fact]
    public void PulseSawtooth_Combined_IsAndMasked()
    {
        // Pulse width = 0x0800 makes pulse HIGH when phase byte < 0x08.
        // At 16384 ticks (phase = 0x3F) pulse is LOW: the AND of pulse=0x00
        // and sawtooth=0x3F yields 0x00, so the combined voice output is
        // silent. The mixed sample equals the digi DC baseline (volume DAC).
        // Sawtooth alone at the same phase outputs 0x3F (clearly non-zero
        // above baseline).
        const int Ticks = 16384;

        float baseline = SampleAtPhase(0x00, Ticks); // no waveform bits = DAC-only baseline
        float combined = SampleAtPhase((byte)(Pulse | Sawtooth), Ticks);
        float sawOnly = SampleAtPhase(Sawtooth, Ticks);

        float.IsNaN(combined).Should().BeFalse();
        float.IsNaN(sawOnly).Should().BeFalse();

        // Pulse is LOW at this phase, so AND yields 0 voice contribution:
        // the mixed sample equals the volume-DAC baseline.
        System.Math.Abs(combined - baseline).Should().BeLessThan(1e-5f,
            "Pulse+Sawtooth at a low-pulse phase ANDs voice to zero (sample == DAC baseline)");
        // Sawtooth alone at that phase has a clearly non-zero voice contribution above baseline.
        System.Math.Abs(sawOnly - baseline).Should().BeGreaterThan(0.01f,
            "control case: sawtooth alone at this phase must be clearly above DAC baseline");
    }

    /// <summary>
    /// FR/TR: FR-SID-003
    /// Use case: Selecting all four waveform bits simultaneously is rare in
    /// real music but is a hard test of the AND-combine rule: most phases
    /// produce 0 (pulse low forces zero, or one of the other waveforms has
    /// a zero bit in common), and even when non-zero the result is
    /// heavily attenuated compared to any single waveform.
    /// Acceptance: Across a full accumulator sweep the maximum absolute
    /// sample from all-four-combined is strictly less than the maximum
    /// absolute sample from triangle-only over the same sweep.
    /// </summary>
    [Fact]
    public void AllFourWaveforms_Combined_IsHeavilyAttenuated()
    {
        float MaxAbsOverSweep(byte ctrl)
        {
            var sid = BuildPrimedSid();
            sid.Write(V1Ctrl, (byte)(ctrl | Gate));
            PrimeEnvelope(sid);
            float maxAbs = 0;
            for (int i = 0; i < FullPhaseSweepTicks; i++)
            {
                sid.Tick();
                var s = System.Math.Abs(sid.GenerateSample());
                if (s > maxAbs) maxAbs = s;
            }
            return maxAbs;
        }

        float triMax = MaxAbsOverSweep(Triangle);
        float allFourMax = MaxAbsOverSweep((byte)(Triangle | Sawtooth | Pulse | Noise));

        triMax.Should().BeGreaterThan(0.0f, "triangle baseline must produce some output");
        allFourMax.Should().BeLessThan(triMax,
            "AND-combining all four waveforms must attenuate the maximum amplitude below the triangle-only baseline");
    }

    /// <summary>
    /// FR/TR: FR-SID-003
    /// Use case: A voice with the gate bit set but no waveform bits chosen
    /// has no waveform output to drive the audio stage. Real SID hardware
    /// outputs a value that decays to zero (the floating waveform value
    /// quickly settles to 0); the emulator must not synthesize spurious
    /// envelope-only samples when no waveform is selected. The mixed
    /// output may still carry the $D418 volume-DAC DC offset (FR-SID-010);
    /// the assertion is that the voice contribution above that baseline
    /// is silent.
    /// Acceptance: With CTRL = gate only (no waveform bits), after the
    /// envelope is primed and the accumulator has run, every sample is
    /// within float epsilon of the no-waveform DAC baseline.
    /// </summary>
    [Fact]
    public void NoWaveformSelected_OutputIsSilent()
    {
        var sid = BuildPrimedSid();
        sid.Write(V1Ctrl, Gate); // gate only, no waveform bits
        PrimeEnvelope(sid);

        // Capture the digi DC baseline using a fresh SID with the same volume.
        var baselineSid = new Sid6581(new BasicBus());
        baselineSid.Write(Volume, 0x0F);
        for (int i = 0; i < 8; i++) baselineSid.Tick();
        float baseline = baselineSid.GenerateSample();

        float maxAbsAboveBaseline = 0f;
        for (int i = 0; i < FullPhaseSweepTicks; i++)
        {
            sid.Tick();
            var s = System.Math.Abs(sid.GenerateSample() - baseline);
            if (s > maxAbsAboveBaseline) maxAbsAboveBaseline = s;
        }

        maxAbsAboveBaseline.Should().BeLessThan(1e-5f,
            "no waveform bits selected must produce no voice contribution above the DAC baseline");
    }
}
