using System.Buffers.Binary;
using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// PLAN-VICEPARITY-001 S3: DIVERGENT (red-now) remediation tests for the
/// waveform-core acceptance criteria of
/// artifacts/vice-parity-requirements/requirements.yaml:
/// FR-SID-WAVE-ACC AC-02 (24-bit stored accumulator), AC-05 (power-on seed
/// 0x555555) and AC-06 (reset preserves the accumulator); FR-SID-OSC3ENV3
/// AC-07 (waveform-0 OSC3 reads the fading floating-DAC latch); every
/// DIVERGENT criterion of FR-SID-WAVE-SAWTRI (AC-01..AC-04) and
/// FR-SID-WAVE-PULSE (AC-01/AC-02/AC-03/AC-04/AC-06). The first four were
/// stopped in S2 because their fixes conflicted with the FAITHFUL locks
/// TEST-SID-VOICE-07, TEST-SID-VOICE-09 and TEST-SID-CLOCK-11; those locks
/// were re-rigged (seed/mask/reset-robust, same subjects bit-exact) at the
/// start of this slice, unblocking the criteria.
///
/// The spec is reSID (native/vice/vice/src/resid: wave.h, wave.cc), reached
/// bit-exactly through the single-cycle vice_sid_exact_* oracle where an
/// oracle observable exists (accumulator export, OSC3 reads). The
/// FR-SID-WAVE-SAWTRI / FR-SID-WAVE-PULSE criteria pin the AUDIO output
/// path (the per-cycle voice outputs fed to the filter chain through the
/// committed 12-bit waveform_output), for which no oracle observable exists
/// at the managed seam; they are asserted closed-form against the cited
/// wave.h/wave.cc mechanisms through the internal CycleVoiceOutputs seam,
/// exactly like the S1 FR-SID-CLOCK dispatch tests. All assertions are
/// exact equality; no tolerances.
///
/// Sample-domain scope notes: the sampled-die combined-waveform ROM content
/// (wave6581_*.h) is FR-SID-WAVE-COMBINED (AC-05/AC-17); the nonlinear
/// 12-bit waveform DAC (model_dac) is FR-SID-WAVE-DACRES; the 20-bit
/// no-shift voice multiply and the waveform-0 floating-DAC audio bias are
/// FR-SID-VOICE (AC-01/AC-04). Those criteria stay red-now and are NOT
/// remediated here; the mirrored voice-output arithmetic below therefore
/// keeps the managed (wave12 - wave_zero) * envelopeDac &gt;&gt; 8 shape.
/// </summary>
[Collection("NativeVice")]
public sealed class SidWaveCoreDivergentParityTests
{
    private const ushort FreqLoV3 = 0x0E;
    private const ushort FreqHiV3 = 0x0F;
    private const ushort ControlV3 = 0x12;
    private const ushort Osc3 = 0x1B;

    /// <summary>reSID 6581 floating-DAC fade-start TTL (wave.cc:43).</summary>
    private const int FloatingOutputTtlStart6581 = 182000;

    /// <summary>reSID 6581 floating-DAC per-bit fade TTL (wave.cc:44).</summary>
    private const int FloatingOutputTtlBit6581 = 1500;

    private static Sid6581 BuildSid()
    {
        var bus = new BasicBus();
        return new Sid6581(bus) { BaseAddress = 0xD400 };
    }

    /// <summary>Write voice register <paramref name="register"/> (0=FREQ_LO ... 6=SR) of <paramref name="voice"/>.</summary>
    private static void WriteVoice(Sid6581 sid, int voice, int register, byte value) =>
        sid.Write((ushort)(0xD400 + voice * 7 + register), value);

    private static void TickN(Sid6581 sid, int cycles)
    {
        for (var i = 0; i < cycles; i++)
        {
            sid.Tick();
        }
    }

    /// <summary>Clock the native oracle and the managed chip the same number of single cycles.</summary>
    private static void ClockBoth(IntPtr native, Sid6581 sid, int cycles)
    {
        ViceNativeBridge.SidExactClock(native, cycles);
        TickN(sid, cycles);
    }

    /// <summary>Raw stored 32-bit accumulator of a voice via the public CaptureState layout.</summary>
    private static uint RawAccumulator(Sid6581 sid, int voice)
    {
        Span<byte> state = stackalloc byte[sid.StateSize];
        sid.CaptureState(state);
        return BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(0x20 + voice * 6, 4));
    }

    /// <summary>Post-clock envelope counter mirror of a voice via the public CaptureState layout.</summary>
    private static byte EnvelopeLevel(Sid6581 sid, int voice)
    {
        Span<byte> state = stackalloc byte[sid.StateSize];
        sid.CaptureState(state);
        return state[0x20 + voice * 6 + 4];
    }

    /// <summary>
    /// Tick the chip until the envelope counter mirror of <paramref name="voice"/>
    /// reaches <paramref name="target"/> (the reSID envelope moves in single
    /// steps, so every level on the trajectory is hit exactly).
    /// </summary>
    private static void TickUntilEnvelope(Sid6581 sid, int voice, byte target, int maxCycles)
    {
        var t = 0;
        while (EnvelopeLevel(sid, voice) != target && t < maxCycles)
        {
            sid.Tick();
            t++;
        }

        Assert.True(t < maxCycles, $"rig sanity: envelope never reached 0x{target:X2} within {maxCycles} cycles");
    }

    /// <summary>
    /// Pin all three managed accumulators to zero through the CTRL test bit
    /// (FAITHFUL mechanism, TEST-SID-WAVE-TESTBIT-01) so no rig depends on
    /// the power-on accumulator seed under either the legacy zero seed or
    /// the reSID 0x555555 seed (FR-SID-WAVE-ACC AC-05, remediated here).
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
        }
    }

    /// <summary>
    /// Pin the voice-3 accumulator to zero on both sides through the CTRL
    /// test bit: one held test cycle zeroes the oracle accumulator at write
    /// time (wave.cc:231) and the managed accumulator inside Tick, then the
    /// control is released to zero on both sides.
    /// </summary>
    private static void ZeroVoice3AccumulatorBothSides(IntPtr native, Sid6581 sid)
    {
        ViceNativeBridge.SidExactWrite(native, ControlV3, 0x08);
        sid.Write(0xD412, 0x08);
        ClockBoth(native, sid, 1);
        ViceNativeBridge.SidExactWrite(native, ControlV3, 0x00);
        sid.Write(0xD412, 0x00);
    }

    /// <summary>
    /// Gated voice-0 audio rig: all accumulators pinned to zero via the test
    /// bit, master volume 15 with the filter mode bits clear, attack 0 /
    /// sustain 15 so the envelope parks at the 0xFF plateau (envelope DAC
    /// exactly 255 there, FR-SID-ENV AC-50), the supplied CTRL and 12-bit
    /// pulse width, frequency still zero. The caller starts the sweep by
    /// writing FREQ after this returns, so the phase at cycle k is exactly
    /// k * freq modulo 2^24.
    /// </summary>
    private static Sid6581 BuildGatedVoice0Rig(byte control, ushort pulseWidth)
    {
        var sid = BuildSid();
        ZeroAllAccumulatorsViaTestBit(sid);
        sid.Write(0xD418, 0x0F); // master volume 15, filter mode bits clear
        WriteVoice(sid, 0, 2, (byte)(pulseWidth & 0xFF));
        WriteVoice(sid, 0, 3, (byte)(pulseWidth >> 8));
        WriteVoice(sid, 0, 5, 0x00); // attack 0 / decay 0
        WriteVoice(sid, 0, 6, 0xF0); // sustain 15 / release 0
        WriteVoice(sid, 0, 4, control);
        TickUntilEnvelope(sid, voice: 0, target: 0xFF, maxCycles: 6000);
        return sid;
    }

    /// <summary>
    /// Mirror of the audio-path voice output for a 12-bit waveform value at
    /// the parked 0xFF envelope plateau: (wave12 - wave_zero 0x380) *
    /// envelope DAC 255, arithmetic-shifted right 8. The 12-bit wave_zero is
    /// the reSID 6581 voice DC level (voice.cc:93; the no-shift 20-bit
    /// multiply itself is FR-SID-VOICE AC-01, another slice). After
    /// PLAN-VICEPARITY-001 S7 the voice output routes through the 12-bit
    /// waveform DAC (model_dac, wave.h:587-593), so this helper uses the
    /// same BuildEnvelopeDacTable call to stay bit-exact.
    /// </summary>
    private static readonly ushort[] _waveDac6581 = Sid6581.BuildEnvelopeDacTable(12, 2.20, term: false);
    // SANCTIONED REBASE (PLAN-VICEPARITY-001 S8): removed >> 8 to match the
    // 20-bit reSID formula (voice.h:99-103). Doc-comment specific values
    // (e.g. VoiceOut(0x000)=-893, VoiceOut(0xFFF)=3186) were the S7 formula;
    // new values are 256x larger (e.g. VoiceOut(0x000)=-220575).
    private static int VoiceOut(int wave12) => (_waveDac6581[wave12] - 0x380) * 255;

    /// <summary>reSID triangle table row at accumulator <paramref name="acc24"/>: ((acc ^ -!!msb) &gt;&gt; 11) &amp; 0xffe (wave.cc:96).</summary>
    private static int Tri12(uint acc24)
    {
        var ix = (int)((acc24 >> 12) & 0xFFF);
        return (((ix & 0x800) != 0 ? ~ix : ix) & 0x7FF) << 1;
    }

    /// <summary>reSID sawtooth table row at accumulator <paramref name="acc24"/>: acc &gt;&gt; 12 (wave.cc:97).</summary>
    private static int Saw12(uint acc24) => (int)((acc24 >> 12) & 0xFFF);

    // ------------------------------------------------------------------
    // FR-SID-WAVE-ACC: 24-bit phase accumulator (the S2-stopped criteria)
    // ------------------------------------------------------------------

    /// <summary>
    /// FR: FR-SID-WAVE-ACC AC-02 (DIVERGENT, finding new), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-WAVE-ACC-02.
    /// Use case: reSID stores the accumulator masked to 24 bits every cycle
    /// (accumulator_next = (accumulator + freq) &amp; 0xffffff, wave.h:155);
    /// the managed chip stored the unmasked 32-bit running sum, so any state
    /// capture, snapshot or downstream consumer of the stored value diverges
    /// after the first 24-bit wrap.
    /// Acceptance: voice 3 on both sides from a test-bit-zeroed accumulator
    /// at FREQ $FFFF; for 300 single cycles (the 24-bit wrap lands at cycle
    /// 257: 257 * 0xFFFF = 0x0100FEFF unmasked) the managed STORED
    /// accumulator (raw CaptureState uint) equals the oracle's exported
    /// 24-bit accumulator bit-exactly on every cycle, and at cycle 300 both
    /// equal exactly (300 * 0xFFFF) mod 2^24 = 0x2BFED4, never the unmasked
    /// 0x012BFED4.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-WAVE-ACC-02", ParityTag.Divergent, pending: false)]
    public void Accumulator_StoredValueIsMaskedTo24Bits()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);

            var sid = BuildSid();
            ZeroVoice3AccumulatorBothSides(native, sid);

            ViceNativeBridge.SidExactWrite(native, FreqLoV3, 0xFF);
            sid.Write(0xD40E, 0xFF);
            ViceNativeBridge.SidExactWrite(native, FreqHiV3, 0xFF); // FREQ $FFFF
            sid.Write(0xD40F, 0xFF);

            for (var cycle = 1; cycle <= 300; cycle++)
            {
                ClockBoth(native, sid, 1);
                Assert.Equal(
                    ViceNativeBridge.SidExactGetState(native).GetAccumulators()[2],
                    RawAccumulator(sid, 2));
            }

            Assert.Equal(0x2BFED4u, RawAccumulator(sid, 2)); // (300 * 0xFFFF) & 0xFFFFFF
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    /// <summary>
    /// FR: FR-SID-WAVE-ACC AC-05 (DIVERGENT, finding new), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-WAVE-ACC-05.
    /// Use case: on power-up the accumulator's even bits are high
    /// (accumulator = 0x555555, wave.cc:117) and reset() preserves that seed
    /// (wave.cc:301-303), so a fresh chip's oscillators start mid-ramp; the
    /// managed chip powered up at zero, so every from-power-on phase
    /// trajectory ran 0x555555 early.
    /// Acceptance: a freshly opened (and reset) oracle exports accumulators
    /// exactly (0x555555, 0x555555, 0x555555) with no writes and no clocks,
    /// and the fresh managed chip's three raw stored accumulators equal that
    /// exactly. Witness through the register surface with zero cycles
    /// elapsed: selecting sawtooth on voice 3 (CTRL $20) refreshes the
    /// waveform output at write time (wave.cc:261-264), so OSC3 immediately
    /// reads exactly 0x55 (sawtooth of 0x555555, top 8 of acc &gt;&gt; 12)
    /// on both sides.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-WAVE-ACC-05", ParityTag.Divergent, pending: false)]
    public void Accumulator_PowersUpWithEvenBitsHigh0x555555()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);

            Assert.Equal(
                new uint[] { 0x555555, 0x555555, 0x555555 },
                ViceNativeBridge.SidExactGetState(native).GetAccumulators());

            var sid = BuildSid();
            Assert.Equal(0x555555u, RawAccumulator(sid, 0));
            Assert.Equal(0x555555u, RawAccumulator(sid, 1));
            Assert.Equal(0x555555u, RawAccumulator(sid, 2));

            // Register-surface witness with zero cycles elapsed.
            ViceNativeBridge.SidExactWrite(native, ControlV3, 0x20);
            sid.Write(0xD412, 0x20);
            Assert.Equal((byte)0x55, ViceNativeBridge.SidExactRead(native, Osc3));
            Assert.Equal((byte)0x55, sid.Read(0xD41B));
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    /// <summary>
    /// FR: FR-SID-WAVE-ACC AC-06 (DIVERGENT, finding new), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-WAVE-ACC-06.
    /// Use case: reSID WaveformGenerator::reset() deliberately leaves the
    /// accumulator alone ("accumulator is not changed on reset",
    /// wave.cc:301-303) while clearing freq, pw, control state and the
    /// output latches; the managed Reset() cleared the accumulator to zero,
    /// so post-reset phase differs from hardware until the next test-bit
    /// write.
    /// Acceptance: voice 3 on both sides from a test-bit-zeroed accumulator
    /// runs 100 cycles at FREQ $1234 to exactly 0x71C50. After a reset on
    /// both sides (reSID SID::reset() via the oracle, Sid6581.Reset()
    /// managed) both accumulators still read exactly 0x71C50. A further 50
    /// cycles change neither side (reset cleared FREQ to 0, wave.cc:304),
    /// proving the accumulator was preserved rather than re-derived.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-WAVE-ACC-06", ParityTag.Divergent, pending: false)]
    public void Reset_PreservesAccumulatorExactly()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);

            var sid = BuildSid();
            ZeroVoice3AccumulatorBothSides(native, sid);

            ViceNativeBridge.SidExactWrite(native, FreqLoV3, 0x34);
            sid.Write(0xD40E, 0x34);
            ViceNativeBridge.SidExactWrite(native, FreqHiV3, 0x12); // FREQ $1234
            sid.Write(0xD40F, 0x12);

            ClockBoth(native, sid, 100);
            Assert.Equal(0x71C50u, ViceNativeBridge.SidExactGetState(native).GetAccumulators()[2]);
            Assert.Equal(0x71C50u, RawAccumulator(sid, 2));

            ViceNativeBridge.SidExactReset(native);
            sid.Reset();

            Assert.Equal(0x71C50u, ViceNativeBridge.SidExactGetState(native).GetAccumulators()[2]);
            Assert.Equal(0x71C50u, RawAccumulator(sid, 2));

            // Reset cleared FREQ (wave.cc:304), so the preserved value holds.
            ClockBoth(native, sid, 50);
            Assert.Equal(0x71C50u, ViceNativeBridge.SidExactGetState(native).GetAccumulators()[2]);
            Assert.Equal(0x71C50u, RawAccumulator(sid, 2));
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    // ------------------------------------------------------------------
    // FR-SID-OSC3ENV3 AC-07: waveform-0 floating-DAC fade (S2-stopped)
    // ------------------------------------------------------------------

    /// <summary>
    /// FR: FR-SID-OSC3ENV3 AC-07 (DIVERGENT, finding 10), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-OSC3ENV3-07.
    /// Use case: with waveform 0 selected the DAC input floats: osc3 keeps
    /// the last selected waveform output and set_waveform_output only ages
    /// the fade counter (wave.h:499-504). On the MOS 6581 the first fade
    /// lands FLOATING_OUTPUT_TTL_START_6581 = 182000 cycles after
    /// deselection and each subsequent bit-fade every
    /// FLOATING_OUTPUT_TTL_BIT_6581 = 1500 cycles (wave.cc:43-44), each fade
    /// computing waveform_output &amp;= waveform_output &gt;&gt; 1
    /// (wave_bitfade, wave.cc:274-280) until the latch reaches zero and
    /// stays there. The managed chip read the live phase ramp for waveform
    /// 0 instead of the fading latch.
    /// Acceptance: voice 3 on both sides from a test-bit-zeroed accumulator
    /// runs sawtooth (CTRL $20) at FREQ $1000 for 4095 cycles so the latch
    /// holds exactly 0xFFF (OSC3 0xFF), then deselects every waveform (CTRL
    /// $00). The managed $D41B equals the oracle's read($1B) exactly at
    /// every checkpoint after deselection: +1000 and +181999 still 0xFF
    /// (latch held; the live phase kept moving, which is the managed red),
    /// +182000 exactly 0x7F (first bit-fade 0xFFF -&gt; 0x7FF), +183500
    /// exactly 0x3F, +185000 exactly 0x1F (per-bit fades every 1500),
    /// +198500 exactly 0x00 (the twelfth fade drains the latch) and +199500
    /// still 0x00 (a zero latch never re-arms the fade timer).
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-OSC3ENV3-07", ParityTag.Divergent, pending: false)]
    public void Osc3_Waveform0ReadsFloatingDacFadeOfLastSelectedOutput()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);

            var sid = BuildSid();
            ZeroVoice3AccumulatorBothSides(native, sid);

            ViceNativeBridge.SidExactWrite(native, FreqLoV3, 0x00);
            sid.Write(0xD40E, 0x00);
            ViceNativeBridge.SidExactWrite(native, FreqHiV3, 0x10); // FREQ $1000
            sid.Write(0xD40F, 0x10);
            ViceNativeBridge.SidExactWrite(native, ControlV3, 0x20); // sawtooth
            sid.Write(0xD412, 0x20);

            ClockBoth(native, sid, 0xFFF); // accumulator 0xFFF000: latch 0xFFF
            Assert.Equal((byte)0xFF, ViceNativeBridge.SidExactRead(native, Osc3));
            Assert.Equal((byte)0xFF, sid.Read(0xD41B));

            ViceNativeBridge.SidExactWrite(native, ControlV3, 0x00); // float the DAC input
            sid.Write(0xD412, 0x00);

            void AssertAfter(int cycles, byte expected)
            {
                ClockBoth(native, sid, cycles);
                var oracle = ViceNativeBridge.SidExactRead(native, Osc3);
                Assert.Equal(expected, oracle);
                Assert.Equal(oracle, sid.Read(0xD41B));
            }

            AssertAfter(1000, 0xFF);                                    // latch held, phase moved on
            AssertAfter(FloatingOutputTtlStart6581 - 1000 - 1, 0xFF);   // +181999: one cycle before the fade
            AssertAfter(1, 0x7F);                                       // +182000: 0xFFF -> 0x7FF
            AssertAfter(FloatingOutputTtlBit6581, 0x3F);                // +183500: 0x7FF -> 0x3FF
            AssertAfter(FloatingOutputTtlBit6581, 0x1F);                // +185000: 0x3FF -> 0x1FF
            AssertAfter(9 * FloatingOutputTtlBit6581, 0x00);            // +198500: latch fully drained
            AssertAfter(1000, 0x00);                                    // zero latch never re-arms
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    // ------------------------------------------------------------------
    // FR-SID-WAVE-SAWTRI: sawtooth and triangle on the audio path
    // ------------------------------------------------------------------

    /// <summary>
    /// FR: FR-SID-WAVE-SAWTRI AC-01 (DIVERGENT, finding 11), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-WAVE-SAWTRI-01.
    /// Use case: the sawtooth output is the upper 12 bits of the accumulator
    /// (wave.cc:97) and the voice fed to the filter chain carries that full
    /// 12-bit resolution; the managed audio path used only the upper 8 bits
    /// (phase byte), discarding accumulator bits 12-15 from the output.
    /// Acceptance: voice 0 sawtooth+gate, envelope parked at the 0xFF
    /// plateau, accumulator test-bit-zeroed, then FREQ $0011; on every one
    /// of 2000 single cycles the per-cycle voice output fed to the filter
    /// equals exactly the mirrored (Saw12(k * 0x11) - 0x380) * 255 &gt;&gt;
    /// 8. The whole window sits below one phase-byte step (2000 * 0x11 =
    /// 0x8500 &lt; 0x10000), so the 8-bit path would be constant while the
    /// 12-bit output first moves at cycle 241 (0x1001), reads exactly
    /// VoiceOut(1) = -892 there and exactly VoiceOut(8) = -885 at cycle
    /// 2000 (saw12 of 0x8500).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-SAWTRI-01", ParityTag.Divergent, pending: false)]
    public void AudioSawtooth_CarriesFullTwelveBitAccumulatorResolution()
    {
        var sid = BuildGatedVoice0Rig(control: 0x21, pulseWidth: 0x000);
        WriteVoice(sid, 0, 0, 0x11); // FREQ $0011

        for (var cycle = 1; cycle <= 2000; cycle++)
        {
            sid.Tick();
            var expected = VoiceOut(Saw12((uint)(cycle * 0x11)));
            Assert.Equal(expected, sid.CycleVoiceOutputs.Voice0);
        }

        Assert.Equal(VoiceOut(8), sid.CycleVoiceOutputs.Voice0); // saw12(0x8500) = 8
    }

    /// <summary>
    /// FR: FR-SID-WAVE-SAWTRI AC-02 (DIVERGENT, finding 11), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-WAVE-SAWTRI-02.
    /// Use case: the triangle folds when accumulator bit 23 rises: the MSB
    /// inverts the lower 11 index bits ((acc ^ -!!msb) in wave.cc:96), so
    /// the fold pivot is the full 24-bit accumulator MSB acting on an
    /// 11-bit fold field; the managed audio path folded an 8-bit phase at
    /// phase bit 7, losing the 11-bit fold resolution.
    /// Acceptance: voice 0 triangle+gate, envelope parked, accumulator
    /// zeroed, FREQ $4000; on every one of 1024 cycles the voice output
    /// equals exactly the mirrored Tri12(k * 0x4000) closed form. Around
    /// the fold the exact sequence is VoiceOut(0xFF8) = 3179 at cycle 511
    /// (rising, acc 0x7FC000), VoiceOut(0xFFE) = 3185 at cycle 512 (acc
    /// 0x800000: bit 23 rose, inverted index), VoiceOut(0xFF6) = 3177 at
    /// cycle 513 (falling): a peak resolvable only with the 11-bit fold.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-SAWTRI-02", ParityTag.Divergent, pending: false)]
    public void AudioTriangle_FoldsOnAccumulatorBit23WithElevenBitField()
    {
        var sid = BuildGatedVoice0Rig(control: 0x11, pulseWidth: 0x000);
        WriteVoice(sid, 0, 1, 0x40); // FREQ $4000

        for (var cycle = 1; cycle <= 1024; cycle++)
        {
            sid.Tick();
            var expected = VoiceOut(Tri12((uint)(cycle * 0x4000) & 0xFFFFFF));
            Assert.Equal(expected, sid.CycleVoiceOutputs.Voice0);
            switch (cycle)
            {
                case 511:
                    Assert.Equal(VoiceOut(0xFF8), sid.CycleVoiceOutputs.Voice0);
                    break;
                case 512:
                    Assert.Equal(VoiceOut(0xFFE), sid.CycleVoiceOutputs.Voice0);
                    break;
                case 513:
                    Assert.Equal(VoiceOut(0xFF6), sid.CycleVoiceOutputs.Voice0);
                    break;
            }
        }
    }

    /// <summary>
    /// FR: FR-SID-WAVE-SAWTRI AC-03 (DIVERGENT, finding 11), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-WAVE-SAWTRI-03.
    /// Use case: the triangle output is 12-bit with bit 0 grounded (the
    /// &amp; 0xffe in wave.cc:96: DAC bit 0 = 0, DAC bit n = accumulator
    /// bit n - 1 per wave.h:399-400) and the accumulator MSB itself is
    /// discarded after selecting the fold direction; the managed audio path
    /// produced an 8-bit doubled phase instead.
    /// Acceptance: voice 0 triangle+gate, envelope parked, accumulator
    /// zeroed, FREQ $1800; on every one of 1400 cycles the voice output
    /// equals exactly the mirrored Tri12(k * 0x1800) closed form. At cycle
    /// 1 (accumulator 0x001800, index 1) the output is exactly VoiceOut(2)
    /// = -891: the un-grounded value 3 (acc &gt;&gt; 11) is a strictly
    /// different mirrored output (-890), witnessing the grounded bit 0. At
    /// cycle 1365 (acc 0x7FF800) the output is exactly VoiceOut(0xFFE) and
    /// at cycle 1366 (acc 0x801000, MSB set) exactly VoiceOut(0xFFC): the
    /// MSB selected the fold but never entered the magnitude.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-SAWTRI-03", ParityTag.Divergent, pending: false)]
    public void AudioTriangle_GroundsBitZeroAndDiscardsMsb()
    {
        Assert.NotEqual(VoiceOut(3), VoiceOut(2)); // rig sanity: the grounded bit is observable

        var sid = BuildGatedVoice0Rig(control: 0x11, pulseWidth: 0x000);
        WriteVoice(sid, 0, 0, 0x00);
        WriteVoice(sid, 0, 1, 0x18); // FREQ $1800

        for (var cycle = 1; cycle <= 1400; cycle++)
        {
            sid.Tick();
            var expected = VoiceOut(Tri12((uint)(cycle * 0x1800) & 0xFFFFFF));
            Assert.Equal(expected, sid.CycleVoiceOutputs.Voice0);
            switch (cycle)
            {
                case 1:
                    Assert.Equal(VoiceOut(2), sid.CycleVoiceOutputs.Voice0);
                    break;
                case 1365:
                    Assert.Equal(VoiceOut(0xFFE), sid.CycleVoiceOutputs.Voice0);
                    break;
                case 1366:
                    Assert.Equal(VoiceOut(0xFFC), sid.CycleVoiceOutputs.Voice0);
                    break;
            }
        }
    }

    /// <summary>
    /// FR: FR-SID-WAVE-SAWTRI AC-04 (DIVERGENT, finding 04), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-WAVE-SAWTRI-04.
    /// Use case: the selected output is a single waveform-table lookup
    /// wave[ix] with ix = (acc ^ ring substitution) &gt;&gt; 12, masked by
    /// the pulse/noise rails (wave.h:462-467); the managed audio path
    /// instead AND-combined independently generated 8-bit waveforms. With
    /// triangle+sawtooth selected (waveform 3, ring off so ix = acc &gt;&gt;
    /// 12) the table row is the 12-bit tri &amp; saw combination (the
    /// managed interim analytic row; the sampled-die __ST ROM content is
    /// FR-SID-WAVE-COMBINED AC-05/AC-17, another slice).
    /// Acceptance: voice 0 tri+saw+gate (CTRL $31), envelope parked,
    /// accumulator zeroed, FREQ $4000; on every one of 1024 cycles the
    /// voice output equals exactly the mirrored single-index lookup
    /// VoiceOut(Tri12(acc) &amp; Saw12(acc)) with acc = k * 0x4000. At
    /// cycle 600 (acc 0x960000: tri 0xD3E, saw 0x960) the output is
    /// exactly VoiceOut(0x920) = 1434, a value unreachable by the old
    /// 8-bit AND (0xD2 &amp; 0x96 = 0x92 scaled to 89).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-SAWTRI-04", ParityTag.Divergent, pending: false)]
    public void AudioSelectedOutput_IsSingleTableLookupNotEightBitAndCombine()
    {
        var sid = BuildGatedVoice0Rig(control: 0x31, pulseWidth: 0x000);
        WriteVoice(sid, 0, 1, 0x40); // FREQ $4000

        // After PLAN-VICEPARITY-001 S7: combined waveforms use the measured ROM table
        // (SidWaveTables.Wave6581_ST) and the 6581 saw accumulator writeback modifies
        // the accumulator when waveform_output bit 11 = 0 (wave.h:488-492).
        // Track the simulated accumulator to compute the expected output per cycle.
        uint simAcc = 0;
        for (var cycle = 1; cycle <= 1024; cycle++)
        {
            simAcc = (simAcc + 0x4000u) & 0x00FFFFFFu;
            int ix = (int)(simAcc >> 12);
            int wave12 = SidWaveTables.Wave6581_ST[ix];
            // Saw accumulator writeback (6581, saw bit set): clear bit 23 when bit 11 = 0.
            simAcc &= (uint)(wave12 << 12) | 0x7FFFFFu;
            var expected = VoiceOut(wave12);
            sid.Tick();
            Assert.Equal(expected, sid.CycleVoiceOutputs.Voice0);
        }
    }

    // ------------------------------------------------------------------
    // FR-SID-WAVE-PULSE: pulse comparator on the audio path
    // ------------------------------------------------------------------

    /// <summary>
    /// FR: FR-SID-WAVE-PULSE AC-01 (DIVERGENT, finding 05), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-WAVE-PULSE-01.
    /// Use case: the pulse rail is HIGH (0xfff) exactly when (accumulator
    /// &gt;&gt; 12) &gt;= pw (wave.h:243,518): the ramp starts LOW and goes
    /// HIGH once the phase reaches the pulse width; the managed audio path
    /// inverted the comparator (HIGH while phase &lt; pw), producing a
    /// mirrored duty cycle.
    /// Acceptance: voice 0 pulse+gate, PW $800, envelope parked,
    /// accumulator zeroed, FREQ $8000 (512-cycle period). Away from the
    /// compare and wrap boundaries (owned by AC-06) the voice output is
    /// exactly the LOW rail VoiceOut(0x000) = -893 for cycles 2..255,
    /// exactly the HIGH rail VoiceOut(0xFFF) = 3186 for cycles 258..511,
    /// and exactly LOW again for cycles 514..767: HIGH on the SECOND half
    /// of the ramp, the reSID polarity.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-PULSE-01", ParityTag.Divergent, pending: false)]
    public void AudioPulse_IsHighWhenPhaseReachesPulseWidth()
    {
        var sid = BuildGatedVoice0Rig(control: 0x41, pulseWidth: 0x800);
        WriteVoice(sid, 0, 1, 0x80); // FREQ $8000

        for (var cycle = 1; cycle <= 767; cycle++)
        {
            sid.Tick();
            if (cycle is >= 2 and <= 255)
            {
                Assert.Equal(VoiceOut(0x000), sid.CycleVoiceOutputs.Voice0);
            }
            else if (cycle is >= 258 and <= 511)
            {
                Assert.Equal(VoiceOut(0xFFF), sid.CycleVoiceOutputs.Voice0);
            }
            else if (cycle >= 514)
            {
                Assert.Equal(VoiceOut(0x000), sid.CycleVoiceOutputs.Voice0);
            }
        }
    }

    /// <summary>
    /// FR: FR-SID-WAVE-PULSE AC-02 (DIVERGENT, finding 05), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-WAVE-PULSE-02.
    /// Use case: the comparator is a full 12-bit compare of (accumulator
    /// &gt;&gt; 12) against the 12-bit pw (wave.h:243); the managed audio
    /// path compared at 8-bit resolution (phase byte vs pw &gt;&gt; 4), so
    /// pulse widths differing only in their low nibble were
    /// indistinguishable.
    /// Acceptance: two rigs identical except pw $800 vs $801 (same upper
    /// byte), envelope parked, accumulators zeroed, FREQ $1000 so the
    /// consumed compare phase at cycle k is exactly k - 1. At cycle 0x801
    /// (compare phase 0x800) the $800 rig reads exactly the HIGH rail
    /// VoiceOut(0xFFF) while the $801 rig reads exactly the LOW rail
    /// VoiceOut(0x000): a one-LSB pulse-width difference the 8-bit compare
    /// cannot see. At cycle 0x803 (compare phase 0x802) both read exactly
    /// the HIGH rail.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-PULSE-02", ParityTag.Divergent, pending: false)]
    public void AudioPulse_ComparesAtTwelveBitResolution()
    {
        var wide = BuildGatedVoice0Rig(control: 0x41, pulseWidth: 0x800);
        var narrow = BuildGatedVoice0Rig(control: 0x41, pulseWidth: 0x801);
        WriteVoice(wide, 0, 1, 0x10);   // FREQ $1000
        WriteVoice(narrow, 0, 1, 0x10);

        TickN(wide, 0x801);
        TickN(narrow, 0x801);
        Assert.Equal(VoiceOut(0xFFF), wide.CycleVoiceOutputs.Voice0);   // 0x800 >= 0x800
        Assert.Equal(VoiceOut(0x000), narrow.CycleVoiceOutputs.Voice0); // 0x800 <  0x801
        Assert.NotEqual(wide.CycleVoiceOutputs.Voice0, narrow.CycleVoiceOutputs.Voice0);

        TickN(wide, 2);
        TickN(narrow, 2);
        Assert.Equal(VoiceOut(0xFFF), wide.CycleVoiceOutputs.Voice0);
        Assert.Equal(VoiceOut(0xFFF), narrow.CycleVoiceOutputs.Voice0);
    }

    /// <summary>
    /// FR: FR-SID-WAVE-PULSE AC-03 (DIVERGENT, finding 05), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-WAVE-PULSE-03.
    /// Use case: with pw == 0 the compare (accumulator &gt;&gt; 12) &gt;= 0
    /// is always true, so the pulse rail is constantly HIGH (wave.h:243);
    /// the managed inverted comparator (phase &lt; 0) produced a constant
    /// LOW instead: the exact opposite rail.
    /// Acceptance: voice 0 pulse+gate with pw exactly 0, envelope parked,
    /// accumulator zeroed, FREQ $8000; on every one of 600 cycles
    /// (spanning a full ramp including the 24-bit wrap at cycle 512) the
    /// voice output is exactly the HIGH rail VoiceOut(0xFFF) = 3186.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-PULSE-03", ParityTag.Divergent, pending: false)]
    public void AudioPulse_PulseWidthZeroIsConstantHigh()
    {
        var sid = BuildGatedVoice0Rig(control: 0x41, pulseWidth: 0x000);
        WriteVoice(sid, 0, 1, 0x80); // FREQ $8000

        for (var cycle = 1; cycle <= 600; cycle++)
        {
            sid.Tick();
            Assert.Equal(VoiceOut(0xFFF), sid.CycleVoiceOutputs.Voice0);
        }
    }

    /// <summary>
    /// FR: FR-SID-WAVE-PULSE AC-04 (DIVERGENT, finding 09), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-WAVE-PULSE-04.
    /// Use case: while the CTRL test bit is held the pulse output is forced
    /// to 0xfff regardless of the pulse width ("The test bit sets pulse
    /// high", wave.h:151 in clock(), wave.h:194 in clock(delta_t)); the
    /// managed audio path ignored the forcing and ran the plain comparator.
    /// Acceptance: voice 0 with TEST|pulse|gate (CTRL $49) and pw $001 (a
    /// width whose comparator result at the test-pinned zero accumulator is
    /// LOW: 0 &gt;= 1 is false), envelope parked at the 0xFF plateau; on
    /// every one of 50 held-test cycles the voice output is exactly the
    /// HIGH rail VoiceOut(0xFFF) = 3186. After releasing test (CTRL $41)
    /// the next cycle reads exactly the LOW rail VoiceOut(0x000) = -893:
    /// without the forcing the comparator takes back over.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-PULSE-04", ParityTag.Divergent, pending: false)]
    public void AudioPulse_TestBitForcesPulseOutputHigh()
    {
        var sid = BuildGatedVoice0Rig(control: 0x49, pulseWidth: 0x001);

        for (var cycle = 1; cycle <= 50; cycle++)
        {
            sid.Tick();
            Assert.Equal(VoiceOut(0xFFF), sid.CycleVoiceOutputs.Voice0);
        }

        sid.Write(0xD404, 0x41); // release test, keep pulse | gate
        sid.Tick();
        Assert.Equal(VoiceOut(0x000), sid.CycleVoiceOutputs.Voice0);
    }

    /// <summary>
    /// FR: FR-SID-WAVE-PULSE AC-06 (DIVERGENT, finding new), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-WAVE-PULSE-06.
    /// Use case: the result of the pulse width compare is delayed one cycle
    /// through the pulse level pipeline: set_waveform_output consumes the
    /// previous cycle's level and pushes the compare of the current
    /// accumulator at its tail (wave.h:516-518); the managed audio path
    /// compared and consumed in the same cycle.
    /// Acceptance: voice 0 pulse+gate, PW $800, envelope parked,
    /// accumulator zeroed, FREQ $8000; the compare first becomes true at
    /// cycle 256 (accumulator 0x800000). The voice output is exactly the
    /// LOW rail VoiceOut(0x000) at cycles 255 AND 256 (the compare-true
    /// cycle still serves the previous level) and exactly the HIGH rail
    /// VoiceOut(0xFFF) at cycle 257: the rail lands exactly one cycle after
    /// the compare.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-PULSE-06", ParityTag.Divergent, pending: false)]
    public void AudioPulse_CompareResultIsDelayedOneCycle()
    {
        var sid = BuildGatedVoice0Rig(control: 0x41, pulseWidth: 0x800);
        WriteVoice(sid, 0, 1, 0x80); // FREQ $8000

        TickN(sid, 255);
        Assert.Equal(VoiceOut(0x000), sid.CycleVoiceOutputs.Voice0); // phase 0x7F8 < 0x800

        sid.Tick(); // cycle 256: accumulator 0x800000, compare true THIS cycle
        Assert.Equal(VoiceOut(0x000), sid.CycleVoiceOutputs.Voice0); // pipeline still serves LOW

        sid.Tick(); // cycle 257: the pipelined HIGH level lands
        Assert.Equal(VoiceOut(0xFFF), sid.CycleVoiceOutputs.Voice0);
    }
}
