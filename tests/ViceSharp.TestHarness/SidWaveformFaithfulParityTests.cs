using System.Buffers.Binary;
using System.Reflection;
using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// PLAN-VICEPARITY-001 P0-8: FAITHFUL (green-now) regression locks for the SID
/// waveform-path acceptance criteria of
/// artifacts/vice-parity-requirements/requirements.yaml: FR-SID-WAVE-ACC,
/// FR-SID-WAVE-PULSE, FR-SID-WAVE-SYNC, FR-SID-WAVE-RING, FR-SID-WAVE-TESTBIT,
/// FR-SID-WAVE-NOISE and FR-SID-OSC3ENV3. FR-SID-WAVE-SAWTRI,
/// FR-SID-WAVE-COMBINED and FR-SID-WAVE-DACRES carry no FAITHFUL criteria
/// (every AC there is DIVERGENT), so they contribute no lock in this file.
/// Use case: pin the managed <see cref="Sid6581"/> behaviour that already
/// matches reSID (native/vice/vice/src/resid: wave.h, wave.cc, sid.h, sid.cc,
/// envelope.cc) so DIVERGENT-criterion remediation cannot silently regress it.
/// Acceptance: one plain managed-only [Fact] per FAITHFUL AC, exact
/// Assert.Equal with no tolerance. Rigs observe chip state through the public
/// CaptureState surface (per-voice accumulator + envelope mirror), the internal
/// <see cref="Sid6581.ReSidEnvelope"/> struct (InternalsVisibleTo), and - only
/// where the state is private (_noiseLfsr, Voice.PulseWidth) - reflection.
/// Expectations are written relative to the observed initial state, or the
/// accumulators are first pinned to zero through the test bit (itself FAITHFUL
/// per TEST-SID-WAVE-TESTBIT-01), so no lock depends on the power-on seed
/// criteria that are tracked as DIVERGENT (FR-SID-WAVE-ACC AC-05 and
/// FR-SID-WAVE-NOISE AC-10).
/// </summary>
public sealed class SidWaveformFaithfulParityTests
{
    private const uint Acc24Mask = 0x00FF_FFFF;
    private const uint NoiseLfsrMask = 0x007F_FFFF;

    /// <summary>Cycles that park an attack-0 / sustain-15 envelope at 0xFF (full ramp needs ~2,300).</summary>
    private const int EnvelopeWarmupCycles = 5000;

    private static Sid6581 BuildSid()
    {
        var bus = new BasicBus();
        return new Sid6581(bus) { BaseAddress = 0xD400 };
    }

    /// <summary>Write voice register <paramref name="register"/> (0=FREQ_LO, 1=FREQ_HI, 2=PW_LO, 3=PW_HI, 4=CTRL, 5=AD, 6=SR).</summary>
    private static void WriteVoice(Sid6581 sid, int voice, int register, byte value) =>
        sid.Write((ushort)(0xD400 + voice * 7 + register), value);

    private static void TickN(Sid6581 sid, int cycles)
    {
        for (var i = 0; i < cycles; i++)
        {
            sid.Tick();
        }
    }

    /// <summary>
    /// Raw stored 32-bit phase accumulator of a voice, via the public
    /// FR-TICKHIST-CHIP-SID CaptureState layout (0x20 register bytes, then
    /// per voice: accumulator uint32 LE, envelope byte, adsr byte).
    /// </summary>
    private static uint RawAccumulator(Sid6581 sid, int voice)
    {
        Span<byte> state = stackalloc byte[sid.StateSize];
        sid.CaptureState(state);
        return BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(0x20 + voice * 6, 4));
    }

    private static uint Accumulator24(Sid6581 sid, int voice) => RawAccumulator(sid, voice) & Acc24Mask;

    /// <summary>Post-clock envelope counter mirror of a voice (CaptureState layout, see <see cref="RawAccumulator"/>).</summary>
    private static byte EnvelopeLevel(Sid6581 sid, int voice)
    {
        Span<byte> state = stackalloc byte[sid.StateSize];
        sid.CaptureState(state);
        return state[0x20 + voice * 6 + 4];
    }

    /// <summary>
    /// The 23-bit noise shift register. It is a private field with no public
    /// or internal read surface, so the lock reads it via reflection
    /// (test-only; the emulation hot path is untouched).
    /// </summary>
    private static uint NoiseLfsr(Sid6581 sid)
    {
        var field = typeof(Sid6581).GetField("_noiseLfsr", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (uint)field.GetValue(sid)!;
    }

    /// <summary>
    /// The composed 12-bit pulse-width latch of a voice. Voice state is a
    /// private nested struct with no readback surface (PW_LO/PW_HI reads echo
    /// the raw register file), so the composition lock reads it via reflection.
    /// </summary>
    private static ushort PulseWidthOf(Sid6581 sid, int voice)
    {
        var voicesField = typeof(Sid6581).GetField("_voices", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var voices = (Array)voicesField.GetValue(sid)!;
        var boxedVoice = voices.GetValue(voice)!;
        var pulseWidthField = boxedVoice.GetType().GetField("PulseWidth")!;
        return (ushort)pulseWidthField.GetValue(boxedVoice)!;
    }

    /// <summary>
    /// Pin all three accumulators to zero through the CTRL test bit (FAITHFUL
    /// per TEST-SID-WAVE-TESTBIT-01: while test is set the accumulator is held
    /// at zero, in reSID wave.cc:229-231 and managed Sid6581.Tick alike), then
    /// clear the controls. Keeps position-sensitive rigs independent of the
    /// power-on accumulator seed (DIVERGENT FR-SID-WAVE-ACC AC-05).
    /// </summary>
    private static void ZeroAllAccumulatorsViaTestBit(Sid6581 sid)
    {
        for (var voice = 0; voice < 3; voice++)
        {
            WriteVoice(sid, voice, 4, 0x08);
        }

        sid.Tick();
        for (var voice = 0; voice < 3; voice++)
        {
            WriteVoice(sid, voice, 4, 0x00);
            Assert.Equal(0u, RawAccumulator(sid, voice));
        }
    }

    /// <summary>
    /// Mirror of the exact GenerateSample arithmetic for a single voice at
    /// full envelope (0xFF), master volume 15, filter mode bits clear:
    /// envelopeAdjusted = ((sample - 0x380) * 0xFF) arithmetic-shifted right 8,
    /// then envelopeAdjusted * 1.0f / 2048.0f + 0.05f, clamped to [-1, 1].
    /// wave_zero is 0x380 for the 6581 die (voice.cc:93); the 8-bit form
    /// WaveZeroLevel=0x38 is multiplied by 0x10 in ComputeVoiceOutput to reach
    /// the 12-bit domain [PLAN-VICEPARITY-001 S3].
    /// Same operation sequence as the per-cycle chain feeding
    /// Sid6581.GenerateSample, so float equality is exact. The FR-SID-ENV
    /// AC-50 envelope DAC is identity at the 0xFF plateau (the 6581
    /// model_dac[0xFF] is exactly 255), so the mirrored multiplicand is
    /// unchanged by that remediation.
    /// </summary>
    private static float ExpectedSampleAtFullEnvelope(int waveformSample)
    {
        var envelopeAdjusted = ((waveformSample - 0x380) * 0xFF) >> 8;
        const float volumeFraction = 15 / 15.0f;
        var voiceMix = envelopeAdjusted * volumeFraction / 2048.0f;
        const float digiDcOffset = volumeFraction * 0.05f;
        return Math.Clamp(voiceMix + digiDcOffset, -1.0f, 1.0f);
    }

    /// <summary>One reSID noise shift step: bit0 = (bit22 XOR bit17), register = ((register shl 1) or bit0) AND 0x7fffff (wave.h:321-329).</summary>
    private static uint NextLfsr(uint lfsr)
    {
        var feedback = ((lfsr >> 22) ^ (lfsr >> 17)) & 0x1u;
        return ((lfsr << 1) | feedback) & NoiseLfsrMask;
    }

    /// <summary>
    /// Drive a noise-selected voice at FREQ 0xFFFF for <paramref name="cycles"/>
    /// cycles and collect every (before, after) noise-register transition that
    /// occurs on an accumulator bit-19 rising edge; asserts the register is
    /// untouched on every other cycle. The accumulator is modelled from its
    /// observed initial value with the same raw uint arithmetic the chip uses.
    /// </summary>
    private static List<(uint Before, uint After)> CollectNoiseShifts(int cycles)
    {
        var sid = BuildSid();
        WriteVoice(sid, 0, 0, 0xFF);
        WriteVoice(sid, 0, 1, 0xFF); // FREQ 0xFFFF
        WriteVoice(sid, 0, 4, 0x80); // noise selected

        var accumulator = RawAccumulator(sid, 0);
        var last = NoiseLfsr(sid);
        var shifts = new List<(uint Before, uint After)>();
        for (var cycle = 1; cycle <= cycles; cycle++)
        {
            var accumulatorPrevious = accumulator;
            accumulator += 0xFFFFu;
            sid.Tick();
            var current = NoiseLfsr(sid);
            if (((~accumulatorPrevious & accumulator) & 0x0008_0000u) != 0)
            {
                shifts.Add((last, current));
            }
            else
            {
                Assert.Equal(last, current);
            }

            last = current;
        }

        return shifts;
    }

    /// <summary>
    /// Hard-sync rig: accumulators pinned to zero, sync source voice 2 at FREQ
    /// 0x8000 (its MSB rises at cycle 256 and falls at cycle 512), destination
    /// voice 0 at FREQ 0x0100 with the supplied CTRL byte.
    /// </summary>
    private static Sid6581 BuildHardSyncRig(byte destinationControl)
    {
        var sid = BuildSid();
        ZeroAllAccumulatorsViaTestBit(sid);
        WriteVoice(sid, 2, 1, 0x80);               // source: FREQ 0x8000
        WriteVoice(sid, 0, 1, 0x01);               // destination: FREQ 0x0100
        WriteVoice(sid, 0, 4, destinationControl);
        return sid;
    }

    /// <summary>
    /// Gated single-voice sample rig for voice 0: accumulator pinned to zero,
    /// master volume 15, attack 0 / sustain 15 so the envelope parks at 0xFF,
    /// the supplied CTRL and PW_HI, then FREQ 0x8000 is started only after the
    /// envelope warmup so the phase at cycle k is exactly (k / 2) AND 0xFF.
    /// </summary>
    private static Sid6581 BuildGatedVoice0SampleRig(byte control, byte pulseWidthHi)
    {
        var sid = BuildSid();
        ZeroAllAccumulatorsViaTestBit(sid);
        sid.Write(0xD418, 0x0F); // master volume 15, filter mode bits clear
        WriteVoice(sid, 0, 2, 0x00);
        WriteVoice(sid, 0, 3, pulseWidthHi);
        WriteVoice(sid, 0, 5, 0x00); // attack 0 / decay 0
        WriteVoice(sid, 0, 6, 0xF0); // sustain 15 / release 0
        WriteVoice(sid, 0, 4, control);
        TickN(sid, EnvelopeWarmupCycles);
        Assert.Equal((byte)0xFF, EnvelopeLevel(sid, 0));
        WriteVoice(sid, 0, 1, 0x80); // FREQ 0x8000
        return sid;
    }

    // ------------------------------------------------------------------
    // FR-SID-WAVE-ACC: 24-bit phase accumulator
    // ------------------------------------------------------------------

    /// <summary>
    /// FR-SID-WAVE-ACC AC-01 (FAITHFUL, TEST-SID-WAVE-ACC-01).
    /// Use case: on every non-test cycle the oscillator advances its phase
    /// accumulator by FREQ modulo 2^24, exactly like reSID's accumulator_next
    /// computation (wave.h:155,157); managed cite Sid6581.cs:752.
    /// Acceptance: at FREQ 0xFFFF, for 300 consecutive cycles (which includes
    /// a 24-bit wrap at cycle 257) the low 24 bits after each Tick equal the
    /// previous low 24 bits plus FREQ, masked to 24 bits - exact every cycle.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-ACC-01", ParityTag.Faithful)]
    public void AccumulatorAddsFrequencyEachNonTestCycleModulo24Bits()
    {
        var sid = BuildSid();
        WriteVoice(sid, 0, 0, 0xFF);
        WriteVoice(sid, 0, 1, 0xFF); // FREQ 0xFFFF, CTRL 0x00 (non-test cycles)

        var previous = Accumulator24(sid, 0);
        for (var cycle = 1; cycle <= 300; cycle++)
        {
            sid.Tick();
            var current = Accumulator24(sid, 0);
            // reSID wave.h:155: accumulator_next = (accumulator + freq) & 0xffffff.
            Assert.Equal((previous + 0xFFFFu) & Acc24Mask, current);
            previous = current;
        }
    }

    /// <summary>
    /// FR-SID-WAVE-ACC AC-03 (FAITHFUL, TEST-SID-WAVE-ACC-03).
    /// Use case: FREQ is a 16-bit addend composed from FREQ_LO / FREQ_HI
    /// (reSID wave.cc:148-156) and applied to the accumulator on every cycle;
    /// managed cite Sid6581.cs:870-873,752.
    /// Acceptance: writing LO alone advances by 0x00CD per cycle, adding HI
    /// 0xAB advances by 0xABCD per cycle, rewriting LO to 0x00 advances by
    /// 0xAB00 per cycle - each post-Tick accumulator matches exactly.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-ACC-03", ParityTag.Faithful)]
    public void FrequencyRegisterPairFormsSixteenBitPerCycleAddend()
    {
        var sid = BuildSid();
        var expected = Accumulator24(sid, 0);

        WriteVoice(sid, 0, 0, 0xCD); // FREQ_LO only: addend 0x00CD
        sid.Tick();
        expected = (expected + 0x00CDu) & Acc24Mask;
        Assert.Equal(expected, Accumulator24(sid, 0));

        WriteVoice(sid, 0, 1, 0xAB); // FREQ_HI: addend now 0xABCD
        for (var cycle = 0; cycle < 3; cycle++)
        {
            sid.Tick();
            expected = (expected + 0xABCDu) & Acc24Mask;
            Assert.Equal(expected, Accumulator24(sid, 0));
        }

        WriteVoice(sid, 0, 0, 0x00); // rewrite FREQ_LO: addend now 0xAB00
        for (var cycle = 0; cycle < 2; cycle++)
        {
            sid.Tick();
            expected = (expected + 0xAB00u) & Acc24Mask;
            Assert.Equal(expected, Accumulator24(sid, 0));
        }
    }

    /// <summary>
    /// FR-SID-WAVE-ACC AC-04 (FAITHFUL, TEST-SID-WAVE-ACC-04).
    /// Use case: reSID derives edge events from the bit-transition set
    /// (NOT previous-accumulator AND next-accumulator, wave.h:156) - bit 19
    /// rising clocks the noise register (wave.h:164), the MSB feeds sync
    /// (wave.h:160). Managed captures pre/post accumulator state per cycle
    /// (Sid6581.cs:722-724,733-735,764-769). The MSB/sync half is locked by
    /// the TEST-SID-WAVE-SYNC tests; the edge-polarity divergence there is
    /// TEST-SID-WAVE-SYNC-01, not this lock.
    /// Acceptance: at FREQ 0xFFFF (irregular bit-19 stride) a co-model that
    /// applies one shift exactly when the transition set has bit 19 predicts
    /// both the accumulator and the noise register bit-for-bit for 700 cycles.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-ACC-04", ParityTag.Faithful)]
    public void AccumulatorBitTransitionSetDrivesNoiseShiftClocking()
    {
        var sid = BuildSid();
        WriteVoice(sid, 0, 0, 0xFF);
        WriteVoice(sid, 0, 1, 0xFF); // FREQ 0xFFFF
        WriteVoice(sid, 0, 4, 0x80); // noise selected so the shift clock is observable

        var accumulator = RawAccumulator(sid, 0);
        var expectedLfsr = NoiseLfsr(sid);
        for (var cycle = 1; cycle <= 700; cycle++)
        {
            var accumulatorPrevious = accumulator;
            accumulator += 0xFFFFu;
            sid.Tick();

            // wave.h:156: accumulator_bits_set = ~accumulator & accumulator_next;
            // wave.h:164: bit 19 in the set clocks the shift register.
            var bitsSet = ~accumulatorPrevious & accumulator;
            if ((bitsSet & 0x0008_0000u) != 0)
            {
                expectedLfsr = NextLfsr(expectedLfsr);
            }

            Assert.Equal(accumulator & Acc24Mask, Accumulator24(sid, 0));
            Assert.Equal(expectedLfsr, NoiseLfsr(sid));
        }
    }

    // ------------------------------------------------------------------
    // FR-SID-WAVE-PULSE: pulse comparator
    // ------------------------------------------------------------------

    /// <summary>
    /// FR-SID-WAVE-PULSE AC-05 (FAITHFUL, TEST-SID-WAVE-PULSE-05).
    /// Use case: the 12-bit pulse width is PW_LO plus the low nibble of PW_HI
    /// shifted up 8 (reSID wave.cc:160,167: each write preserves the other
    /// half and masks HI to 4 bits); managed cite Sid6581.cs:876,879.
    /// Acceptance: a write sequence exercising both orders and a garbage HI
    /// upper nibble composes exactly 0x0300, 0x03FF, 0x0AFF, 0x0ACD, 0x0FCD
    /// in the voice pulse-width latch.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-PULSE-05", ParityTag.Faithful)]
    public void PulseWidthRegisterComposesTwelveBitsFromLoAndMaskedHiNibble()
    {
        var sid = BuildSid();

        WriteVoice(sid, 0, 3, 0x03); // PW_HI first: pw = 0x300
        Assert.Equal((ushort)0x0300, PulseWidthOf(sid, 0));

        WriteVoice(sid, 0, 2, 0xFF); // PW_LO preserves HI: pw = 0x3FF
        Assert.Equal((ushort)0x03FF, PulseWidthOf(sid, 0));

        WriteVoice(sid, 0, 3, 0xFA); // HI upper nibble discarded: pw = 0xAFF
        Assert.Equal((ushort)0x0AFF, PulseWidthOf(sid, 0));

        WriteVoice(sid, 0, 2, 0xCD); // LO overwrite preserves HI: pw = 0xACD
        Assert.Equal((ushort)0x0ACD, PulseWidthOf(sid, 0));

        WriteVoice(sid, 0, 3, 0x0F); // HI overwrite preserves LO: pw = 0xFCD
        Assert.Equal((ushort)0x0FCD, PulseWidthOf(sid, 0));
    }

    /// <summary>
    /// FR-SID-WAVE-PULSE AC-07 (FAITHFUL, TEST-SID-WAVE-PULSE-07).
    /// Use case: the pulse comparator influences the voice output only while
    /// the pulse waveform is selected (reSID wave.cc:221 no_pulse gating);
    /// managed cite Sid6581.cs:164-170,205.
    /// Acceptance: with triangle-only selected, rigs differing solely in pulse
    /// width (0x800 vs 0x400) produce identical exact samples at 12-bit phases
    /// 0x320 (100 cycles) and 0x960 (300 cycles); with pulse selected the same
    /// width pair splits the output at 12-bit phase 0x640 (200 cycles) into the
    /// exact low-rail (0x000, below PW 0x800) and high-rail (0xFFF, at or above
    /// PW 0x400) samples. reSID pulse is HIGH when (acc>>12)>=pw (wave.h:518).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-PULSE-07", ParityTag.Faithful)]
    public void PulseWidthAffectsOutputOnlyWhenPulseWaveformSelected()
    {
        // Triangle | gate: pulse width must be inert.
        var triangleWide = BuildGatedVoice0SampleRig(0x11, 0x08);   // pw 0x800
        var triangleNarrow = BuildGatedVoice0SampleRig(0x11, 0x04); // pw 0x400
        TickN(triangleWide, 100);
        TickN(triangleNarrow, 100); // 12-bit phase 0x320 (800): tri12 = 0x640
        Assert.Equal(ExpectedSampleAtFullEnvelope(0x640), triangleWide.GenerateSample());
        Assert.Equal(ExpectedSampleAtFullEnvelope(0x640), triangleNarrow.GenerateSample());
        TickN(triangleWide, 200);
        TickN(triangleNarrow, 200); // 12-bit phase 0x960 (2400): tri12 = 0xD3E
        Assert.Equal(ExpectedSampleAtFullEnvelope(0xD3E), triangleWide.GenerateSample());
        Assert.Equal(ExpectedSampleAtFullEnvelope(0xD3E), triangleNarrow.GenerateSample());

        // Pulse | gate: the identical pulse-width pair now swings the output.
        var pulseWide = BuildGatedVoice0SampleRig(0x41, 0x08);   // PW 0x800; 12-bit phase 0x640 < 0x800: LOW
        var pulseNarrow = BuildGatedVoice0SampleRig(0x41, 0x04); // PW 0x400; 12-bit phase 0x640 >= 0x400: HIGH
        TickN(pulseWide, 200);
        TickN(pulseNarrow, 200); // 12-bit phase 0x640 (200 cycles at FREQ $8000)
        Assert.Equal(ExpectedSampleAtFullEnvelope(0x000), pulseWide.GenerateSample());   // 0x640 < PW 0x800: LOW
        Assert.Equal(ExpectedSampleAtFullEnvelope(0xFFF), pulseNarrow.GenerateSample()); // 0x640 >= PW 0x400: HIGH
    }

    // ------------------------------------------------------------------
    // FR-SID-WAVE-SYNC: hard sync
    // ------------------------------------------------------------------

    /// <summary>
    /// FR-SID-WAVE-SYNC AC-02 (FAITHFUL, TEST-SID-WAVE-SYNC-02).
    /// Use case: a sync event resets the destination only when the destination
    /// CTRL SYNC bit 0x02 is set (reSID wave.h:261 gates on sync_dest sync);
    /// managed cite Sid6581.cs:778,782,786.
    /// Acceptance: identical rigs (source voice 2 FREQ 0x8000, destination
    /// voice 0 FREQ 0x0100, 600 cycles spanning the source MSB edge at cycle
    /// 512) end at exactly 88 * 0x100 with the bit set and exactly 600 * 0x100
    /// (an unbroken ramp) with the bit clear.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-SYNC-02", ParityTag.Faithful)]
    public void HardSyncAppliesOnlyWhenDestinationSyncControlBitSet()
    {
        var synced = BuildHardSyncRig(0x02);
        var unsynced = BuildHardSyncRig(0x00);

        TickN(synced, 600);
        TickN(unsynced, 600);

        // Current managed edge (source MSB 1 to 0) lands at cycle 512; the
        // rising-vs-falling polarity itself is DIVERGENT TEST-SID-WAVE-SYNC-01.
        Assert.Equal(88u * 0x100u, Accumulator24(synced, 0));
        Assert.Equal(600u * 0x100u, Accumulator24(unsynced, 0));
    }

    /// <summary>
    /// FR-SID-WAVE-SYNC AC-03 (FAITHFUL, TEST-SID-WAVE-SYNC-03).
    /// Use case: on a sync event the destination accumulator is set to exactly
    /// zero (reSID wave.h:262 sync_dest accumulator = 0); managed cite
    /// Sid6581.cs:779,783,787.
    /// Acceptance: the destination ramps untouched through cycle 511
    /// (511 * 0x100), reads exactly 0 after the sync cycle 512 (the reset wins
    /// over that cycle's FREQ add), and resumes from zero with exactly 0x100
    /// on cycle 513.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-SYNC-03", ParityTag.Faithful)]
    public void HardSyncEventSetsDestinationAccumulatorToZero()
    {
        var sid = BuildHardSyncRig(0x02);

        TickN(sid, 511);
        Assert.Equal(511u * 0x100u, Accumulator24(sid, 0));

        sid.Tick(); // source MSB edge cycle
        Assert.Equal(0u, Accumulator24(sid, 0));

        sid.Tick();
        Assert.Equal(0x100u, Accumulator24(sid, 0));
    }

    /// <summary>
    /// FR-SID-WAVE-SYNC AC-05 (FAITHFUL, TEST-SID-WAVE-SYNC-05).
    /// Use case: the hard-sync sources are chained voice 0 from voice 2,
    /// voice 1 from voice 0, voice 2 from voice 1 (reSID sid.cc:74-76
    /// set_sync_source wiring); managed cite Sid6581.cs:721,777-787.
    /// Acceptance: for each source voice s, after 600 cycles the chained
    /// destination (s+1 mod 3) is reset at the source edge (exactly
    /// 88 * 0x100) while the remaining voice - whose own source produced no
    /// MSB edge - ramps to exactly 600 * 0x100 even though its SYNC bit is
    /// also set.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-SYNC-05", ParityTag.Faithful)]
    public void HardSyncChainIsVoiceZeroFromTwoOneFromZeroTwoFromOne()
    {
        foreach (var source in new[] { 2, 0, 1 })
        {
            var chainedDestination = (source + 1) % 3; // its sync source is `source`
            var unrelated = (source + 2) % 3;          // its sync source is `chainedDestination` (no edge)

            var sid = BuildSid();
            ZeroAllAccumulatorsViaTestBit(sid);
            WriteVoice(sid, source, 1, 0x80);              // source FREQ 0x8000: MSB falls at cycle 512
            WriteVoice(sid, chainedDestination, 1, 0x01);  // FREQ 0x0100
            WriteVoice(sid, chainedDestination, 4, 0x02);
            WriteVoice(sid, unrelated, 1, 0x01);           // FREQ 0x0100
            WriteVoice(sid, unrelated, 4, 0x02);

            TickN(sid, 600);

            Assert.Equal(88u * 0x100u, Accumulator24(sid, chainedDestination));
            Assert.Equal(600u * 0x100u, Accumulator24(sid, unrelated));
        }
    }

    /// <summary>
    /// FR-SID-WAVE-SYNC AC-06 (FAITHFUL, TEST-SID-WAVE-SYNC-06).
    /// Use case: all oscillators are clocked first and only then synchronized,
    /// because they operate in parallel (reSID sid.h:200-223); managed
    /// latches every post-clock MSB before applying any sync reset
    /// (Sid6581.cs:722-724,737-755,771-787).
    /// Acceptance: with voice 0 (FREQ 0x7000) producing its MSB edge at cycle
    /// 586 while voice 1 (FREQ 0x4000, SYNC set) holds its MSB high across
    /// that cycle, voice 1 is reset to exactly 0 at cycle 586 but voice 2
    /// (FREQ 0x0010, SYNC set) keeps its unbroken ramp (586 * 0x10 then
    /// 700 * 0x10): a serial implementation reading voice 1 post-reset would
    /// have synced voice 2.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-SYNC-06", ParityTag.Faithful)]
    public void HardSyncEvaluatesAllVoicesInParallelAfterClocking()
    {
        var sid = BuildSid();
        ZeroAllAccumulatorsViaTestBit(sid);
        WriteVoice(sid, 0, 1, 0x70); // FREQ 0x7000: MSB set cycles 293..585, falls at 586
        WriteVoice(sid, 1, 1, 0x40); // FREQ 0x4000: MSB set from cycle 512 (still high at 586)
        WriteVoice(sid, 1, 4, 0x02); // voice 1 syncs from voice 0
        WriteVoice(sid, 2, 0, 0x10); // FREQ 0x0010
        WriteVoice(sid, 2, 4, 0x02); // voice 2 syncs from voice 1

        TickN(sid, 585);
        Assert.Equal((585u * 0x4000u) & Acc24Mask, Accumulator24(sid, 1)); // 0x924000: not yet reset

        sid.Tick(); // cycle 586: voice 0 edge resets voice 1; voice 1's latched MSB stayed high
        Assert.Equal(0u, Accumulator24(sid, 1));
        Assert.Equal(586u * 0x10u, Accumulator24(sid, 2)); // parallel semantics: voice 2 untouched

        TickN(sid, 114); // to cycle 700
        Assert.Equal(114u * 0x4000u, Accumulator24(sid, 1));
        Assert.Equal(700u * 0x10u, Accumulator24(sid, 2));
    }

    // ------------------------------------------------------------------
    // FR-SID-WAVE-RING: ring modulation
    // ------------------------------------------------------------------

    /// <summary>
    /// FR-SID-WAVE-RING AC-04 (FAITHFUL, TEST-SID-WAVE-RING-04).
    /// Use case: the ring-modulation source of voice i is voice (i+2) mod 3,
    /// the same backward chain as hard sync (reSID wave.h:465 uses the
    /// sync-source accumulator); managed cite Sid6581.cs:154.
    /// Acceptance: for each destination voice with triangle+ring at phase 0
    /// and a parked full envelope, the initial output is the ring-INVERTED
    /// triangle value 0xFFE (source acc=0, source MSB=LOW -> reSID inverts
    /// the destination triangle: ix=0x800 -> tri12=0xFFE). Parking the
    /// NON-source voice's MSB high leaves the inverted sample unchanged.
    /// Parking the (i+2) mod 3 source's MSB high (MSB=1 = no inversion)
    /// gives the normal triangle at phase 0 (0x000 = low-rail).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-RING-04", ParityTag.Faithful)]
    public void RingModulationSourceIsVoicePlusTwoModThree()
    {
        var lowSample = ExpectedSampleAtFullEnvelope(0x000);  // normal triangle at 12-bit phase 0: tri12=0x000
        var highSample = ExpectedSampleAtFullEnvelope(0xFFE); // ring-inverted: source MSB=0 -> ix=0x800 -> tri12=0xFFE

        for (var destination = 0; destination < 3; destination++)
        {
            var source = (destination + 2) % 3;
            var nonSource = (destination + 1) % 3;

            var sid = BuildSid();
            ZeroAllAccumulatorsViaTestBit(sid);
            sid.Write(0xD418, 0x0F);                 // master volume 15
            WriteVoice(sid, destination, 5, 0x00);   // attack 0 / decay 0
            WriteVoice(sid, destination, 6, 0xF0);   // sustain 15 / release 0
            WriteVoice(sid, destination, 4, 0x15);   // triangle | ring | gate
            TickN(sid, EnvelopeWarmupCycles);        // all FREQ 0: phases frozen while envelope parks
            Assert.Equal((byte)0xFF, EnvelopeLevel(sid, destination));

            Assert.Equal(highSample, sid.GenerateSample()); // source acc=0 MSB=LOW: ring inverts -> 0xFFE

            ParkVoiceMsbHigh(sid, nonSource);
            Assert.Equal(0x800000u, RawAccumulator(sid, nonSource));
            Assert.Equal(highSample, sid.GenerateSample()); // non-source MSB ignored; ring source unchanged

            ParkVoiceMsbHigh(sid, source);
            Assert.Equal(0x800000u, RawAccumulator(sid, source));
            Assert.Equal(lowSample, sid.GenerateSample()); // source MSB=HIGH: no ring inversion -> 0x000
        }
    }

    /// <summary>Advance a frozen voice to accumulator 0x800000 (MSB high) and freeze it again.</summary>
    private static void ParkVoiceMsbHigh(Sid6581 sid, int voice)
    {
        WriteVoice(sid, voice, 1, 0x80); // FREQ 0x8000
        TickN(sid, 256);                 // 256 * 0x8000 = 0x800000
        WriteVoice(sid, voice, 1, 0x00); // freeze
    }

    // ------------------------------------------------------------------
    // FR-SID-WAVE-TESTBIT: test bit
    // ------------------------------------------------------------------

    /// <summary>
    /// FR-SID-WAVE-TESTBIT AC-01 (FAITHFUL, TEST-SID-WAVE-TESTBIT-01).
    /// Use case: while the CTRL test bit is set the phase accumulator is held
    /// at zero (reSID wave.cc:231 resets it on test-rising and the test branch
    /// never advances it); managed cite Sid6581.cs:747.
    /// Acceptance: a voice running at FREQ 0x1234 (verified advancing for 4
    /// cycles) reads a raw accumulator of exactly 0 on the first cycle after
    /// the test bit is set and on every one of the following 8 cycles.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-TESTBIT-01", ParityTag.Faithful)]
    public void TestBitHoldsAccumulatorAtZero()
    {
        var sid = BuildSid();
        WriteVoice(sid, 0, 0, 0x34);
        WriteVoice(sid, 0, 1, 0x12); // FREQ 0x1234

        var initial = Accumulator24(sid, 0);
        TickN(sid, 4);
        Assert.Equal((initial + 4u * 0x1234u) & Acc24Mask, Accumulator24(sid, 0)); // precondition: running

        WriteVoice(sid, 0, 4, 0x08); // test bit set
        for (var cycle = 0; cycle < 9; cycle++)
        {
            sid.Tick();
            Assert.Equal(0u, RawAccumulator(sid, 0));
        }
    }

    /// <summary>
    /// FR-SID-WAVE-TESTBIT AC-02 (FAITHFUL, TEST-SID-WAVE-TESTBIT-02).
    /// Use case: while the test bit is held, FREQ never advances the
    /// accumulator (reSID wave.h:144-152 skips the whole accumulator path);
    /// managed cite Sid6581.cs:744-750.
    /// Acceptance: with FREQ 0xFFFF and test held for 50 cycles the raw
    /// accumulator reads exactly 0 on every cycle; after clearing test it
    /// resumes with exactly 0xFFFF then 2 * 0xFFFF on the next two cycles.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-TESTBIT-02", ParityTag.Faithful)]
    public void TestBitBlocksFrequencyAccumulationWhileHeld()
    {
        var sid = BuildSid();
        WriteVoice(sid, 0, 0, 0xFF);
        WriteVoice(sid, 0, 1, 0xFF); // FREQ 0xFFFF
        WriteVoice(sid, 0, 4, 0x08); // test held from the start

        for (var cycle = 0; cycle < 50; cycle++)
        {
            sid.Tick();
            Assert.Equal(0u, RawAccumulator(sid, 0));
        }

        WriteVoice(sid, 0, 4, 0x00); // release the test bit
        sid.Tick();
        Assert.Equal(0xFFFFu, RawAccumulator(sid, 0));
        sid.Tick();
        Assert.Equal(2u * 0xFFFFu, RawAccumulator(sid, 0));
    }

    /// <summary>
    /// FR-SID-WAVE-TESTBIT AC-07 (FAITHFUL, TEST-SID-WAVE-TESTBIT-07).
    /// Use case: the test bit is CTRL bit 3, mask 0x08 (reSID wave.cc:206);
    /// managed cite Sid6581.cs:744.
    /// Acceptance: with FREQ 0x1234 for 16 cycles, every other single CTRL bit
    /// (0x01, 0x02, 0x04, 0x10, 0x20, 0x40, 0x80) and the everything-but-test
    /// byte 0xF7 leave the accumulator ramp exact, while 0x08 and the
    /// test-amid-waveforms byte 0xF8 pin it to exactly 0.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-TESTBIT-07", ParityTag.Faithful)]
    public void TestBitIsControlRegisterBitThree()
    {
        foreach (var control in new byte[] { 0x01, 0x02, 0x04, 0x10, 0x20, 0x40, 0x80, 0xF7 })
        {
            var sid = BuildSid();
            WriteVoice(sid, 0, 0, 0x34);
            WriteVoice(sid, 0, 1, 0x12); // FREQ 0x1234
            WriteVoice(sid, 0, 4, control);
            var initial = Accumulator24(sid, 0);
            TickN(sid, 16);
            Assert.Equal((initial + 16u * 0x1234u) & Acc24Mask, Accumulator24(sid, 0));
        }

        foreach (var control in new byte[] { 0x08, 0xF8 })
        {
            var sid = BuildSid();
            WriteVoice(sid, 0, 0, 0x34);
            WriteVoice(sid, 0, 1, 0x12);
            WriteVoice(sid, 0, 4, control);
            TickN(sid, 16);
            Assert.Equal(0u, RawAccumulator(sid, 0));
        }
    }

    // ------------------------------------------------------------------
    // FR-SID-WAVE-NOISE: 23-bit noise LFSR
    // ------------------------------------------------------------------

    /// <summary>
    /// FR-SID-WAVE-NOISE AC-01 (FAITHFUL, TEST-SID-WAVE-NOISE-01).
    /// Use case: the noise shift register is 23 bits wide and is masked with
    /// 0x7fffff after every shift (reSID wave.h:325); managed cite
    /// Sid6581.cs:56,83.
    /// Acceptance: 700 cycles at FREQ 0xFFFF yield exactly 44 shifts (bit 19
    /// enters odd 0x80000-blocks 1,3,...,87; block b is entered at cycle
    /// ceil(b * 0x80000 / 0xFFFF)); every post-shift value has bits 23-31
    /// exactly 0, including shifts whose pre-state had bit 22 set (where an
    /// unmasked left shift would have carried into bit 23).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-NOISE-01", ParityTag.Faithful)]
    public void NoiseShiftRegisterStaysWithinTwentyThreeBits()
    {
        var shifts = CollectNoiseShifts(700);

        Assert.Equal(44, shifts.Count);
        foreach (var (_, after) in shifts)
        {
            Assert.Equal(0u, after >> 23);
        }

        // The mask is provably exercised: at least one shift started with bit 22 set.
        Assert.Contains(shifts, shift => (shift.Before & 0x0040_0000u) != 0);
    }

    /// <summary>
    /// FR-SID-WAVE-NOISE AC-02 (FAITHFUL, TEST-SID-WAVE-NOISE-02).
    /// Use case: the LFSR feedback into bit 0 is bit 22 XOR bit 17 of the
    /// pre-shift register (reSID wave.h:323-324); managed cite Sid6581.cs:82.
    /// Acceptance: for every one of the 44 observed shifts across 700 cycles,
    /// bit 0 of the post-shift register equals exactly the XOR of pre-shift
    /// bits 22 and 17, and both feedback polarities (0 and 1) occur in the
    /// window.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-NOISE-02", ParityTag.Faithful)]
    public void NoiseFeedbackBitIsXorOfBits22And17()
    {
        var shifts = CollectNoiseShifts(700);
        Assert.Equal(44, shifts.Count);

        var seenFeedback = new HashSet<uint>();
        foreach (var (before, after) in shifts)
        {
            var expectedFeedback = ((before >> 22) ^ (before >> 17)) & 0x1u;
            Assert.Equal(expectedFeedback, after & 0x1u);
            seenFeedback.Add(expectedFeedback);
        }

        Assert.Contains(0u, seenFeedback);
        Assert.Contains(1u, seenFeedback);
    }

    /// <summary>
    /// FR-SID-WAVE-NOISE AC-03 (FAITHFUL, TEST-SID-WAVE-NOISE-03).
    /// Use case: each noise clock performs one left shift, ORs the feedback
    /// bit into bit 0, and masks to 23 bits (reSID wave.h:325); managed cite
    /// Sid6581.cs:83. (The managed zero-reseed fallback, DIVERGENT
    /// TEST-SID-WAVE-NOISE-11, is unreachable here: the zero state cannot be
    /// entered from a nonzero register with this polynomial.)
    /// Acceptance: for every one of the 44 observed shifts across 700 cycles
    /// the post-shift register equals exactly ((before shifted left 1) OR
    /// feedback) masked to 23 bits.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-NOISE-03", ParityTag.Faithful)]
    public void NoiseShiftIsLeftShiftOrFeedbackMaskedTo23Bits()
    {
        var shifts = CollectNoiseShifts(700);
        Assert.Equal(44, shifts.Count);

        foreach (var (before, after) in shifts)
        {
            var feedback = ((before >> 22) ^ (before >> 17)) & 0x1u;
            Assert.Equal(((before << 1) | feedback) & NoiseLfsrMask, after);
        }
    }

    /// <summary>
    /// FR-SID-WAVE-NOISE AC-04 (FAITHFUL, TEST-SID-WAVE-NOISE-04).
    /// Use case: the noise register clocks exactly on accumulator bit-19
    /// low-to-high transitions - not on the high level and not on the
    /// high-to-low edge (reSID wave.h:156,164); managed cite
    /// Sid6581.cs:61,733-735,764-766.
    /// Acceptance: with the accumulator pinned to zero and FREQ 0x8000, bit 19
    /// rises exactly at cycles 16, 48, 80, 112, 144, 176 within 200 cycles;
    /// a co-model shifted only at those cycles predicts the register exactly
    /// on every one of the 200 cycles (so the 15 high-level cycles after each
    /// rise and the falling edges at 32, 64, ... produce no shift).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-NOISE-04", ParityTag.Faithful)]
    public void NoiseLfsrClocksOnAccumulatorBit19RisingEdgeOnly()
    {
        var sid = BuildSid();
        WriteVoice(sid, 0, 4, 0x08); // pin the accumulator to zero (FAITHFUL test-bit mechanism)
        sid.Tick();
        Assert.Equal(0u, RawAccumulator(sid, 0));
        WriteVoice(sid, 0, 1, 0x80); // FREQ 0x8000: bit 19 of cycle*0x8000 is (cycle shifted right 4) AND 1
        WriteVoice(sid, 0, 4, 0x80); // noise selected

        var expected = NoiseLfsr(sid);
        var shiftCycles = new List<int>();
        for (var cycle = 1; cycle <= 200; cycle++)
        {
            sid.Tick();
            if (cycle % 32 == 16)
            {
                expected = NextLfsr(expected);
                shiftCycles.Add(cycle);
            }

            Assert.Equal(expected, NoiseLfsr(sid));
        }

        Assert.Equal(new[] { 16, 48, 80, 112, 144, 176 }, shiftCycles);
    }

    // ------------------------------------------------------------------
    // FR-SID-OSC3ENV3: OSC3/ENV3 readback
    // ------------------------------------------------------------------

    /// <summary>
    /// FR-SID-OSC3ENV3 AC-08 (FAITHFUL, TEST-SID-OSC3ENV3-08).
    /// Use case: register $1B (OSC3) reads voice 3 (index 2), not voice 1 or 2
    /// (reSID sid.cc routes read($1B) to voice[2]); managed cite
    /// Sid6581.cs:849. Sawtooth is selected so the expected byte is identical
    /// under the current managed readback (accumulator bits 16-23) and the
    /// reSID semantics (sawtooth waveform output shifted right 4), keeping
    /// this lock inert to the DIVERGENT TEST-SID-OSC3ENV3-01 remediation.
    /// Acceptance: with accumulators pinned to zero and FREQ 0x8000 / 0xC000 /
    /// 0x4000 on voices 1/2/3, after 8 cycles the voices sit at exactly
    /// 0x040000 / 0x060000 / 0x020000 and $1B reads exactly 0x02 (voice 3),
    /// not 0x04 or 0x06.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-OSC3ENV3-08", ParityTag.Faithful)]
    public void Osc3ReadsVoiceThreeOscillator()
    {
        var sid = BuildSid();
        ZeroAllAccumulatorsViaTestBit(sid);
        WriteVoice(sid, 0, 1, 0x80); // FREQ 0x8000
        WriteVoice(sid, 1, 1, 0xC0); // FREQ 0xC000
        WriteVoice(sid, 2, 1, 0x40); // FREQ 0x4000
        for (var voice = 0; voice < 3; voice++)
        {
            WriteVoice(sid, voice, 4, 0x20); // sawtooth
        }

        TickN(sid, 8);

        Assert.Equal(0x040000u, RawAccumulator(sid, 0));
        Assert.Equal(0x060000u, RawAccumulator(sid, 1));
        Assert.Equal(0x020000u, RawAccumulator(sid, 2));
        Assert.Equal((byte)0x02, sid.Read(0xD41B));
    }

    /// <summary>
    /// FR-SID-OSC3ENV3 AC-09 (FAITHFUL, TEST-SID-OSC3ENV3-09).
    /// Use case: register $1C returns readENV(), i.e. the env3 latch of the
    /// envelope generator (reSID envelope.cc:273-275); managed cite
    /// Sid6581.cs:850-852.
    /// Acceptance: a standalone <see cref="Sid6581.ReSidEnvelope"/> reference
    /// driven with the identical AD/SR/gate writes and one Clock per Tick
    /// predicts $1C exactly on every one of 2,600 cycles (attack ramp from
    /// the 0xaa power-up seed plus sustain entry at attack 0 / sustain 15).
    /// The reference is power-up-seeded (PowerUp: counter 0xaa,
    /// envelope.cc:176) to match the chip's reSID power-up state
    /// (FR-SID-ENV AC-08).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-OSC3ENV3-09", ParityTag.Faithful)]
    public void Env3ReadbackReturnsLatchedEnv3Value()
    {
        var sid = BuildSid();
        sid.Write(0xD413, 0x00); // voice 3 attack 0 / decay 0
        sid.Write(0xD414, 0xF0); // voice 3 sustain 15 / release 0
        sid.Write(0xD412, 0x01); // voice 3 gate on

        var reference = new Sid6581.ReSidEnvelope();
        reference.PowerUp();
        reference.WriteAttackDecay(0x00);
        reference.WriteSustainRelease(0xF0);
        reference.WriteControl(0x01);

        for (var cycle = 1; cycle <= 2600; cycle++)
        {
            reference.Clock();
            sid.Tick();
            Assert.Equal(reference.Env3, sid.Read(0xD41C));
        }
    }

    /// <summary>
    /// FR-SID-OSC3ENV3 AC-10 (FAITHFUL, TEST-SID-OSC3ENV3-10).
    /// Use case: env3 is latched at the first phase of the envelope clock, so
    /// a cycle that steps the counter still reads back the pre-step value and
    /// the new value only appears one cycle later (reSID envelope
    /// single-cycle clock; managed cite Sid6581.cs:536).
    /// Acceptance: across 2,600 cycles covering the attack-0 ramp up and the
    /// sustain-0 decay down, $1C after every Tick equals exactly the post-Tick
    /// envelope counter of the previous cycle - through increments,
    /// decrements, and idle stretches alike - and the counter demonstrably
    /// moved (final value not 0).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-OSC3ENV3-10", ParityTag.Faithful)]
    public void Env3IsLatchedAtFirstPhaseOfEnvelopeClock()
    {
        var sid = BuildSid();
        sid.Write(0xD413, 0x00); // attack 0 / decay 0
        sid.Write(0xD414, 0x00); // sustain 0 / release 0: decays after the peak
        sid.Write(0xD412, 0x01); // gate on

        var previous = EnvelopeLevel(sid, 2);
        for (var cycle = 1; cycle <= 2600; cycle++)
        {
            sid.Tick();
            Assert.Equal(previous, sid.Read(0xD41C));
            previous = EnvelopeLevel(sid, 2);
        }

        Assert.NotEqual((byte)0x00, previous); // rig sanity: the counter ramped and is mid-decay
    }

    /// <summary>
    /// FR-SID-OSC3ENV3 AC-11 (FAITHFUL, TEST-SID-OSC3ENV3-11).
    /// Use case: register $1C reads voice 3 (index 2), not another voice
    /// (reSID sid.cc routes read($1C) to voice[2]); managed cite
    /// Sid6581.cs:852.
    /// Acceptance: with voice 1 gated at attack 0 (fast) and voice 3 gated at
    /// attack 2 (slow), $1C tracks the voice-3 reference exactly on every one
    /// of 400 cycles, the voice-1 envelope mirror tracks the voice-1 reference
    /// exactly, and the two references disagree at the end of the window (so
    /// the readback demonstrably selected voice 3). Both references are
    /// power-up-seeded (PowerUp: counter 0xaa, envelope.cc:176) to match the
    /// chip's reSID power-up state (FR-SID-ENV AC-08).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-OSC3ENV3-11", ParityTag.Faithful)]
    public void Env3ReadsVoiceThreeEnvelope()
    {
        var sid = BuildSid();
        sid.Write(0xD405, 0x00); // voice 1 attack 0 / decay 0
        sid.Write(0xD406, 0xF0); // voice 1 sustain 15 / release 0
        sid.Write(0xD404, 0x01); // voice 1 gate on
        sid.Write(0xD413, 0x20); // voice 3 attack 2 / decay 0
        sid.Write(0xD414, 0xF0); // voice 3 sustain 15 / release 0
        sid.Write(0xD412, 0x01); // voice 3 gate on

        var referenceVoice0 = new Sid6581.ReSidEnvelope();
        referenceVoice0.PowerUp();
        referenceVoice0.WriteAttackDecay(0x00);
        referenceVoice0.WriteSustainRelease(0xF0);
        referenceVoice0.WriteControl(0x01);

        var referenceVoice2 = new Sid6581.ReSidEnvelope();
        referenceVoice2.PowerUp();
        referenceVoice2.WriteAttackDecay(0x20);
        referenceVoice2.WriteSustainRelease(0xF0);
        referenceVoice2.WriteControl(0x01);

        for (var cycle = 1; cycle <= 400; cycle++)
        {
            referenceVoice0.Clock();
            referenceVoice2.Clock();
            sid.Tick();
            Assert.Equal(referenceVoice2.Env3, sid.Read(0xD41C));
        }

        Assert.Equal(referenceVoice0.EnvelopeCounter, EnvelopeLevel(sid, 0));
        // Rig sanity: the two envelopes diverged, so $1C demonstrably read voice 3.
        Assert.NotEqual(referenceVoice0.EnvelopeCounter, referenceVoice2.Env3);
    }
}
