using System.Buffers.Binary;
using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// PLAN-VICEPARITY-001 S4/S5 (combined): DIVERGENT remediation tests for the
/// acceptance criteria of FR-SID-WAVE-SYNC (AC-01, AC-04), FR-SID-WAVE-RING
/// (AC-01, AC-02, AC-03) and FR-SID-WAVE-TESTBIT (AC-03, AC-04, AC-05,
/// AC-06, AC-08) in artifacts/vice-parity-requirements/requirements.yaml.
///
/// Hard sync (FR-SID-WAVE-SYNC): reSID fires on the RISING MSB edge
/// (msb_rising, wave.h:160) with a same-cycle sync-source special case
/// (wave.h:255-264). The managed chip used a FALLING edge and lacked the
/// suppression case.
///
/// Ring modulation (FR-SID-WAVE-RING): reSID substitutes only the fold-MSB
/// of the triangle index via XOR with the source accumulator's bit 23
/// (wave.h:465, wave.cc:214). The gate is ring_mod=1 AND NOT(sawtooth);
/// the trigger fires when the source MSB is LOW (ring_msb_mask XOR is
/// non-zero). The managed implementation was already correct in S3.
///
/// Test bit (FR-SID-WAVE-TESTBIT): the CTRL test bit is a state machine
/// (wave.h:144-172, wave.cc:225-268): on rising edge, accumulator=0,
/// shift_pipeline=0, shift_register_reset=35000 (6581), pulse_output=0xfff;
/// while held, shift_register_reset counts down and calls shiftreg_bitfade
/// at zero; on falling edge, a single shift-register clock fires with
/// bit0 = NOT(bit17). Shift registers are per-voice, not shared.
///
/// Spec: native/vice/vice/src/resid/wave.h and wave.cc. Oracle: the
/// single-cycle reSID oracle via ViceNativeBridge.SidExact* (TR-SID-ORACLE-001).
/// </summary>
[Collection("NativeVice")]
public sealed class SidSyncRingTestbitDivergentParityTests
{
    // Register offsets within each voice block (0x00 per voice 1, 0x07 per voice 2, 0x0E per voice 3).
    // Oracle addresses are relative to chip base (0x00 = voice 1 freq lo, 0x0B = voice 2 control, etc.)
    private const ushort V0FreqHi  = 0x01;  // oracle voice 1 freq hi
    private const ushort V1FreqLo  = 0x07;  // oracle voice 2 freq lo
    private const ushort V1FreqHi  = 0x08;  // oracle voice 2 freq hi
    private const ushort V1Ctrl    = 0x0B;  // oracle voice 2 control
    private const ushort V2FreqHi  = 0x0F;  // oracle voice 3 freq hi
    private const ushort V2Ctrl    = 0x12;  // oracle voice 3 control (same as SidWaveCoreDivergentParityTests.ControlV3)

    private static Sid6581 BuildSid()
    {
        var bus = new BasicBus();
        return new Sid6581(bus) { BaseAddress = 0xD400 };
    }

    private static void TickN(Sid6581 sid, int cycles)
    {
        for (var i = 0; i < cycles; i++) sid.Tick();
    }

    private static void ClockBoth(IntPtr native, Sid6581 sid, int cycles)
    {
        ViceNativeBridge.SidExactClock(native, cycles);
        TickN(sid, cycles);
    }

    /// <summary>Raw stored 24-bit accumulator of a voice via public CaptureState layout.</summary>
    private static uint RawAccumulator(Sid6581 sid, int voice)
    {
        Span<byte> state = stackalloc byte[sid.StateSize];
        sid.CaptureState(state);
        return BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(0x20 + voice * 6, 4));
    }

    /// <summary>Post-clock envelope counter of a voice via public CaptureState.</summary>
    private static byte EnvelopeLevel(Sid6581 sid, int voice)
    {
        Span<byte> state = stackalloc byte[sid.StateSize];
        sid.CaptureState(state);
        return state[0x20 + voice * 6 + 4];
    }

    private static void TickUntilEnvelope(Sid6581 sid, int voice, byte target, int maxCycles)
    {
        var t = 0;
        while (EnvelopeLevel(sid, voice) != target && t < maxCycles)
        {
            sid.Tick();
            t++;
        }
        Assert.True(t < maxCycles, $"rig: envelope never reached 0x{target:X2} in {maxCycles} cycles");
    }

    /// <summary>Zero all managed accumulators via test bit (both write and tick).</summary>
    private static void ZeroAllAccumulatorsViaTestBit(Sid6581 sid)
    {
        for (var v = 0; v < 3; v++) sid.Write((ushort)(0xD404 + v * 7), 0x08);
        sid.Tick();
        for (var v = 0; v < 3; v++) sid.Write((ushort)(0xD404 + v * 7), 0x00);
    }

    /// <summary>Zero all accumulators on both oracle and managed sides via test bit.</summary>
    private static void ZeroAllBothSides(IntPtr native, Sid6581 sid)
    {
        // Write test bit to all three voices on both sides.
        for (ushort r = 0; r < 3; r++)
        {
            ViceNativeBridge.SidExactWrite(native, (ushort)(0x04 + r * 7), 0x08);
            sid.Write((ushort)(0xD404 + r * 7), 0x08);
        }
        ClockBoth(native, sid, 1);
        // Release test bit on all voices.
        for (ushort r = 0; r < 3; r++)
        {
            ViceNativeBridge.SidExactWrite(native, (ushort)(0x04 + r * 7), 0x00);
            sid.Write((ushort)(0xD404 + r * 7), 0x00);
        }
    }

    /// <summary>
    /// Voice output for 12-bit waveform at the 0xFF envelope plateau (exactly
    /// as SidWaveCoreDivergentParityTests.VoiceOut).
    /// </summary>
    private static readonly ushort[] _waveDac6581 = Sid6581.BuildEnvelopeDacTable(12, 2.20, term: false);
    // SANCTIONED REBASE (PLAN-VICEPARITY-001 S8): removed >> 8.
    // 20-bit formula: (waveDac[wave12] - wave_zero) * envDac (voice.h:99-103).
    private static int VoiceOut(int wave12) => (_waveDac6581[wave12] - 0x380) * 255;

    /// <summary>reSID triangle table row at acc24 (wave.cc:96).</summary>
    private static int Tri12(uint acc24)
    {
        var ix = (int)((acc24 >> 12) & 0xFFF);
        return (((ix & 0x800) != 0 ? ~ix : ix) & 0x7FF) << 1;
    }

    /// <summary>
    /// reSID ring-modulated triangle: ix = (acc ^ (~srcAcc and ring_msb_mask)) >> 12
    /// where ring_msb_mask = 0x800000 when ring_mod=1 and sawtooth=0.
    /// </summary>
    private static int RingTri12(uint acc24, uint srcAcc24)
    {
        const uint RingMsbMask = 0x800000u;
        int ix = (int)((acc24 ^ (~srcAcc24 & RingMsbMask)) >> 12);
        return (((ix & 0x800) != 0 ? ~ix : ix) & 0x7FF) << 1;
    }

    // -------------------------------------------------------------------------
    // FR-SID-WAVE-SYNC: hard sync fires on rising MSB edge + special case
    // -------------------------------------------------------------------------

    /// <summary>
    /// FR: FR-SID-WAVE-SYNC AC-01 (DIVERGENT, finding 07), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-WAVE-SYNC-01.
    /// Use case: reSID fires hard sync when the SOURCE voice's bit 23 transitions
    /// 0->1 (msb_rising, wave.h:160). The managed chip used the 1->0 (falling)
    /// edge, so the dest accumulator was reset one half-period late and missed
    /// the first MSB-rise entirely.
    /// Acceptance: voice 1 (index 0) at freq 0x8000 drives sync into voice 2
    /// (index 1, SYNC bit set, freq 0x1000). With accumulators zeroed on both
    /// sides, at cycle 256 voice 1's bit 23 transitions 0->1 (accumulator reaches
    /// 0x800000). reSID zeroes voice 2 on that cycle; the managed accumulator[1]
    /// must equal the oracle's exactly. Assert both oracle and managed equal 0
    /// after 256 cycles (sync fired on the rising edge this cycle).
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-WAVE-SYNC-01", ParityTag.Divergent, pending: false)]
    public void HardSync_FiresOnRisingEdgeOfSourceMsb()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);

            var sid = BuildSid();
            ZeroAllBothSides(native, sid);

            // Voice 0 (SID voice 1): source, freq 0x8000 (MSB rises at cycle 256).
            ViceNativeBridge.SidExactWrite(native, 0x00, 0x00);
            ViceNativeBridge.SidExactWrite(native, V0FreqHi, 0x80);
            sid.Write(0xD400, 0x00);
            sid.Write(0xD401, 0x80);

            // Voice 1 (SID voice 2): dest, SYNC bit, freq 0x1000.
            ViceNativeBridge.SidExactWrite(native, V1FreqLo, 0x00);
            ViceNativeBridge.SidExactWrite(native, V1FreqHi, 0x10);
            ViceNativeBridge.SidExactWrite(native, V1Ctrl, 0x02);  // SYNC
            sid.Write(0xD407, 0x00);
            sid.Write(0xD408, 0x10);
            sid.Write(0xD40B, 0x02);  // SYNC

            // Clock 256 cycles: at cycle 256 voice 0 MSB rises (0x8000*256 = 0x800000).
            // Oracle resets voice 1 on this cycle; managed must match.
            ClockBoth(native, sid, 256);

            var oracleAccs = ViceNativeBridge.SidExactGetState(native).GetAccumulators();
            // Oracle voice 1 was reset by the rising-edge sync.
            Assert.Equal(0u, oracleAccs[1]);
            // Managed must equal oracle exactly.
            Assert.Equal(oracleAccs[1], RawAccumulator(sid, 1));
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    /// <summary>
    /// FR: FR-SID-WAVE-SYNC AC-04 (DIVERGENT, finding new), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-WAVE-SYNC-04.
    /// Use case: reSID wave.h:255-264 special case: voice[i] suppresses its reset
    /// of voice[(i+1)%3] when voice[i] itself has SYNC set AND its own source
    /// (voice[(i+2)%3]) has msb_rising this same cycle. This prevents the sync
    /// chain from cascading incorrectly when two sources rise simultaneously.
    /// Acceptance: voice 0 (freq 0x8000, SYNC set) and voice 2 (freq 0x8000, no
    /// SYNC) both have MSBs rising at cycle 256. Voice 1 (SYNC set) is voice 0's
    /// sync dest; voice 0 ALSO has SYNC set while voice 2's MSB rises, so the
    /// special case fires and voice 1 is NOT reset. Voice 0 itself IS reset by
    /// voice 2 (voice 2 has no SYNC so the special case does not suppress that
    /// direction). Both oracle accumulators[0] and accumulators[1] must equal
    /// managed accumulators 0 and 1 exactly after 256 cycles.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-WAVE-SYNC-04", ParityTag.Divergent, pending: false)]
    public void HardSync_SameCycleSyncSourceSpecialCaseSuppressesDestSync()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);

            var sid = BuildSid();
            ZeroAllBothSides(native, sid);

            // Voice 0 (SID voice 1): SYNC set, freq 0x8000 (MSB rises at cycle 256).
            ViceNativeBridge.SidExactWrite(native, 0x00, 0x00);
            ViceNativeBridge.SidExactWrite(native, V0FreqHi, 0x80);
            ViceNativeBridge.SidExactWrite(native, 0x04, 0x02);  // SYNC
            sid.Write(0xD400, 0x00);
            sid.Write(0xD401, 0x80);
            sid.Write(0xD404, 0x02);  // SYNC

            // Voice 1 (SID voice 2): SYNC set, freq 0x0800 (MSB NOT rising at cycle 256).
            ViceNativeBridge.SidExactWrite(native, V1FreqLo, 0x00);
            ViceNativeBridge.SidExactWrite(native, V1FreqHi, 0x08);
            ViceNativeBridge.SidExactWrite(native, V1Ctrl, 0x02);  // SYNC
            sid.Write(0xD407, 0x00);
            sid.Write(0xD408, 0x08);
            sid.Write(0xD40B, 0x02);  // SYNC

            // Voice 2 (SID voice 3): no SYNC, freq 0x8000 (MSB rises at cycle 256 simultaneously with voice 0).
            ViceNativeBridge.SidExactWrite(native, 0x0E, 0x00);
            ViceNativeBridge.SidExactWrite(native, V2FreqHi, 0x80);
            ViceNativeBridge.SidExactWrite(native, V2Ctrl, 0x00);  // no SYNC
            sid.Write(0xD40E, 0x00);
            sid.Write(0xD40F, 0x80);
            sid.Write(0xD412, 0x00);  // no SYNC

            // At cycle 256: msbRising0=true, msbRising2=true, msbRising1=false.
            // voice[0] fires on voice[1]: suppressed because voice0.sync=true AND msbRising2=true.
            // voice[2] fires on voice[0]: NOT suppressed (voice2.sync=false).
            // Result: voice[0] acc=0, voice[1] acc=256*0x0800=0x080000 (not reset).
            ClockBoth(native, sid, 256);

            var oracleAccs = ViceNativeBridge.SidExactGetState(native).GetAccumulators();
            // Voice 0 was reset by voice 2 (no special case suppression there).
            Assert.Equal(0u, oracleAccs[0]);
            // Voice 1 was NOT reset (sync suppressed by special case).
            Assert.Equal(256u * 0x0800u, oracleAccs[1]);
            // Managed must match oracle on both.
            Assert.Equal(oracleAccs[0], RawAccumulator(sid, 0));
            Assert.Equal(oracleAccs[1], RawAccumulator(sid, 1));
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    // -------------------------------------------------------------------------
    // FR-SID-WAVE-RING: ring modulation XOR and gate logic (already correct in S3)
    // -------------------------------------------------------------------------

    /// <summary>
    /// FR: FR-SID-WAVE-RING AC-01 (DIVERGENT, finding new), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-WAVE-RING-01.
    /// Use case: ring modulation substitutes only the fold-direction MSB of
    /// the triangle index (ix = (acc ^ (~srcAcc and ring_msb_mask)) >> 12,
    /// wave.h:465), not the whole output. The managed implementation already
    /// matches reSID in S3.
    /// Acceptance: voice 0 triangle+ring+gate (CTRL 0x15), voice 2 freq 0 (src
    /// MSB = 0 throughout, ring active). After 1 cycle at freq 0x2000 the
    /// accumulator is 0x2000; managed voice output equals VoiceOut(RingTri12(
    /// 0x2000, 0)) and differs from VoiceOut(Tri12(0x2000)) (fold direction
    /// inverted), proving the XOR flips only the fold-direction bit.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-RING-01", ParityTag.Divergent, pending: false)]
    public void RingMod_SubstitutesFoldMsbViaXorNotWholeTriangle()
    {
        var sid = BuildSid();
        ZeroAllAccumulatorsViaTestBit(sid);
        sid.Write(0xD418, 0x0F);   // master vol 15
        sid.Write(0xD405, 0x00);   // A=0 D=0
        sid.Write(0xD406, 0xF0);   // S=F R=0
        sid.Write(0xD404, 0x15);   // gate | tri | ring (0x01 | 0x10 | 0x04)
        TickUntilEnvelope(sid, 0, 0xFF, 6000);

        // Voice 2 freq 0: stays at acc 0, src MSB = 0, ring active (XOR flips bit 23).
        // Voice 0 freq 0x2000: after 1 cycle acc = 0x2000.
        sid.Write(0xD400, 0x00);
        sid.Write(0xD401, 0x20);   // freq 0x2000
        sid.Tick();

        int ringOut = sid.CycleVoiceOutputs.Voice0;
        // With ring active (src MSB=0), the fold bit of ix = 0x802 (bit 11 set),
        // so tri = (~0x802 and 0x7FF) << 1 = 0x7FD << 1 = 0xFFA.
        int expectedRing = RingTri12(0x2000u, 0u);
        Assert.Equal(VoiceOut(expectedRing), ringOut);
        // Without ring the fold bit is NOT set (ix=2, tri=4); the outputs differ.
        Assert.NotEqual(VoiceOut(Tri12(0x2000u)), ringOut);
    }

    /// <summary>
    /// FR: FR-SID-WAVE-RING AC-02 (DIVERGENT, finding new), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-WAVE-RING-02.
    /// Use case: ring triggers when the source MSB is LOW (ring_msb_mask = 0x800000;
    /// ~srcAcc and ring_msb_mask is non-zero only when srcAcc bit 23 = 0, wave.h:465).
    /// When source MSB is HIGH the XOR has no effect. Managed implementation already
    /// matches in S3.
    /// Acceptance: two rigs, voice 0 tri+ring+gate, voice 2 freq 0 (src MSB = 0)
    /// vs voice 2 at acc 0x800000 (src MSB = 1). After 1 cycle at freq 0x2000:
    /// rig A (src MSB=0, ring active) output = VoiceOut(RingTri12(0x2000, 0));
    /// rig B (src MSB=1, ring inactive) output = VoiceOut(Tri12(0x2000)), equal
    /// to the no-ring triangle. The two outputs differ, proving ring triggers on
    /// LOW src MSB only.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-RING-02", ParityTag.Divergent, pending: false)]
    public void RingMod_TriggersWhenSourceMsbIsLowNotHigh()
    {
        // Rig A: src (voice 2) acc = 0 (MSB = 0, ring active).
        var sidA = BuildSid();
        ZeroAllAccumulatorsViaTestBit(sidA);
        sidA.Write(0xD418, 0x0F);
        sidA.Write(0xD405, 0x00); sidA.Write(0xD406, 0xF0);
        sidA.Write(0xD404, 0x15);  // gate | tri | ring
        TickUntilEnvelope(sidA, 0, 0xFF, 6000);
        sidA.Write(0xD401, 0x20);  // freq 0x2000
        sidA.Tick();

        // Rig B: src (voice 2) acc = 0x800000 (MSB = 1, ring inactive).
        // To set voice 2 acc to 0x800000: freq 0x8000, clock 256 cycles.
        var sidB = BuildSid();
        ZeroAllAccumulatorsViaTestBit(sidB);
        sidB.Write(0xD40F, 0x80);  // voice 3 freq hi = 0x80 (freq 0x8000)
        TickN(sidB, 256);          // voice 2 acc = 256*0x8000 = 0x800000 (MSB=1)
        // Now set voice 0 up with ring mod.
        sidB.Write(0xD40F, 0x00);  // freeze voice 2 freq at 0 to keep acc stable
        sidB.Write(0xD418, 0x0F);
        sidB.Write(0xD405, 0x00); sidB.Write(0xD406, 0xF0);
        sidB.Write(0xD404, 0x15);  // gate | tri | ring
        TickUntilEnvelope(sidB, 0, 0xFF, 6000);
        sidB.Write(0xD401, 0x20);  // voice 0 freq 0x2000
        sidB.Tick();

        int outA = sidA.CycleVoiceOutputs.Voice0;  // ring active: XOR flips fold
        int outB = sidB.CycleVoiceOutputs.Voice0;  // ring inactive: normal triangle

        // Ring active (src MSB=0): matches RingTri12.
        Assert.Equal(VoiceOut(RingTri12(0x2000u, 0u)), outA);
        // Ring inactive (src MSB=1): matches plain Tri12.
        Assert.Equal(VoiceOut(Tri12(0x2000u)), outB);
        // The two outputs must differ (ring has observable effect).
        Assert.NotEqual(outA, outB);
    }

    /// <summary>
    /// FR: FR-SID-WAVE-RING AC-03 (DIVERGENT, finding new), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-WAVE-RING-03.
    /// Use case: ring_msb_mask = ((~ctrl >> 5) and (ctrl >> 2) and 1) << 23
    /// (wave.cc:214): armed only when ring_mod=1 AND NOT(sawtooth). With sawtooth
    /// also selected the mask is zero and ring has no effect. Managed already
    /// matches in S3 via (ctrl and 0x24) == 0x04.
    /// Acceptance: voice 0 tri+sawtooth+ring+gate (CTRL 0x35 = saw|tri|ring|gate)
    /// at freq 0x2000; after 1 cycle the sawtooth deselects ring (mask=0), so
    /// output = VoiceOut(Tri12(0x2000) and Saw12(0x2000)) = the analytic AND
    /// row, identical to no-ring. With only tri+ring+gate (CTRL 0x15) the ring is
    /// active and output differs.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-RING-03", ParityTag.Divergent, pending: false)]
    public void RingMod_ActiveOnlyWhenSawtoothDeselected()
    {
        // Rig A: tri+ring+gate (sawtooth off, ring active).
        var sidA = BuildSid();
        ZeroAllAccumulatorsViaTestBit(sidA);
        sidA.Write(0xD418, 0x0F);
        sidA.Write(0xD405, 0x00); sidA.Write(0xD406, 0xF0);
        sidA.Write(0xD404, 0x15);  // gate | tri | ring
        TickUntilEnvelope(sidA, 0, 0xFF, 6000);
        sidA.Write(0xD401, 0x20);
        sidA.Tick();

        // Rig B: tri+saw+ring+gate (sawtooth ON, ring mask = 0, ring inactive).
        var sidB = BuildSid();
        ZeroAllAccumulatorsViaTestBit(sidB);
        sidB.Write(0xD418, 0x0F);
        sidB.Write(0xD405, 0x00); sidB.Write(0xD406, 0xF0);
        sidB.Write(0xD404, 0x35);  // gate | tri | saw | ring
        TickUntilEnvelope(sidB, 0, 0xFF, 6000);
        sidB.Write(0xD401, 0x20);
        sidB.Tick();

        int outA = sidA.CycleVoiceOutputs.Voice0;  // ring active
        int outB = sidB.CycleVoiceOutputs.Voice0;  // sawtooth suppresses ring

        // Rig A: ring active, output uses XOR-flipped index.
        Assert.Equal(VoiceOut(RingTri12(0x2000u, 0u)), outA);
        // Rig B: ring suppressed by sawtooth, output is analytic tri&saw.
        int saw12 = (int)(0x2000u >> 12) & 0xFFF; // Saw12(0x2000)
        Assert.Equal(VoiceOut(Tri12(0x2000u) & saw12), outB);
        // The two outputs must differ.
        Assert.NotEqual(outA, outB);
    }

    // -------------------------------------------------------------------------
    // FR-SID-WAVE-TESTBIT: test-bit state machine (AC-03 already correct in S3)
    // -------------------------------------------------------------------------

    /// <summary>
    /// FR: FR-SID-WAVE-TESTBIT AC-03 (DIVERGENT, finding new), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-WAVE-TESTBIT-03.
    /// Use case: while the CTRL test bit is held, pulse_output is forced to 0xfff
    /// every clock cycle (wave.h:151), overriding the accumulator-based comparator.
    /// The managed chip already implements this in S3.
    /// Acceptance: voice 0 test|pulse|gate (CTRL 0x49) with PW 0x001 (comparator
    /// would be LOW since acc=0 < pw=1); on every one of 20 held-test cycles the
    /// voice output is exactly VoiceOut(0xFFF). After releasing test (CTRL 0x41)
    /// the next cycle reads the LOW rail VoiceOut(0x000).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-WAVE-TESTBIT-03", ParityTag.Divergent, pending: false)]
    public void TestBit_ForcesPulseLevelHighEachCycle()
    {
        var sid = BuildSid();
        ZeroAllAccumulatorsViaTestBit(sid);
        sid.Write(0xD418, 0x0F);
        sid.Write(0xD402, 0x01);  // PW_LO = 1
        sid.Write(0xD403, 0x00);  // PW_HI = 0 (PW = 1)
        sid.Write(0xD405, 0x00); sid.Write(0xD406, 0xF0);
        sid.Write(0xD404, 0x49);  // test | pulse | gate
        TickUntilEnvelope(sid, 0, 0xFF, 6000);

        // Held test: acc stays 0, comparator 0 >= 1 is false (would be LOW),
        // but test forces pulse_output = 0xFFF -> HIGH rail.
        for (int cycle = 1; cycle <= 20; cycle++)
        {
            sid.Tick();
            Assert.Equal(VoiceOut(0xFFF), sid.CycleVoiceOutputs.Voice0);
        }

        // Release test: next cycle comparator takes over (acc=0, pw=1 -> LOW).
        sid.Write(0xD404, 0x41);  // pulse | gate (no test)
        sid.Tick();
        Assert.Equal(VoiceOut(0x000), sid.CycleVoiceOutputs.Voice0);
    }

    /// <summary>
    /// FR: FR-SID-WAVE-TESTBIT AC-04 (DIVERGENT, finding new), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-WAVE-TESTBIT-04.
    /// Use case: on the test-bit rising edge, writeCONTROL_REG sets
    /// shift_register_reset = SHIFT_REGISTER_RESET_START_6581 = 35000 (wave.cc:234).
    /// This starts the slow countdown to shiftreg_bitfade. The managed chip did
    /// not track per-voice shift_register_reset.
    /// Acceptance: oracle and managed start from reset state. Write test bit to
    /// voice 0 (CTRL 0x08) on both sides. Immediately after the write (no clock):
    /// oracle ShiftRegisterReset[0] = 35000, managed VoiceShiftRegisterReset(0) = 35000.
    /// One clock later, oracle ShiftRegisterReset[0] = 34999 (one decrement),
    /// managed VoiceShiftRegisterReset(0) = 34999.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-WAVE-TESTBIT-04", ParityTag.Divergent, pending: false)]
    public void TestBitRising_ArmsSlowShiftRegisterResetOf35000Cycles()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);
            var sid = BuildSid();

            // Write test bit to voice 0 (SID voice 1 control = oracle reg 0x04).
            ViceNativeBridge.SidExactWrite(native, 0x04, 0x08);
            sid.Write(0xD404, 0x08);

            // Immediately after write: shift_register_reset = 35000.
            var srr0 = ViceNativeBridge.SidExactGetState(native).GetShiftRegisterResets();
            Assert.Equal(35000u, srr0[0]);
            Assert.Equal(35000u, sid.VoiceShiftRegisterReset(0));

            // After one clock: decremented to 34999.
            ClockBoth(native, sid, 1);
            var srr1 = ViceNativeBridge.SidExactGetState(native).GetShiftRegisterResets();
            Assert.Equal(34999u, srr1[0]);
            Assert.Equal(34999u, sid.VoiceShiftRegisterReset(0));
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    /// <summary>
    /// FR: FR-SID-WAVE-TESTBIT AC-05 (DIVERGENT, finding new), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-WAVE-TESTBIT-05.
    /// Use case: on the test-bit rising edge, writeCONTROL_REG flushes
    /// shift_pipeline = 0 (wave.cc:233), discarding any pending noise-clock
    /// pipeline state. The managed chip did not track per-voice shift_pipeline.
    /// Acceptance: voice 0 advances at freq 0x100000 / 0x04 = 0x40000 for 2 cycles
    /// so bit-19 rises on cycle 1 (acc = 0x040000: bit 19 set, pipeline armed to 2
    /// in reSID clock()). After the test bit is written on both sides, oracle
    /// ShiftPipeline[0] = 0 and managed VoiceShiftPipeline(0) = 0 (pipeline
    /// flushed by writeCONTROL_REG, wave.cc:233).
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-WAVE-TESTBIT-05", ParityTag.Divergent, pending: false)]
    public void TestBitRising_FlushesShiftPipelineToZero()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);
            var sid = BuildSid();
            ZeroAllBothSides(native, sid);

            // Voice 0 freq 0x8000: bit 19 (0x080000) rises when acc >= 0x080000.
            // At freq 0x8000: cycle 16 gives acc=0x080000 (bit 19 first rise, pipeline=2).
            ViceNativeBridge.SidExactWrite(native, 0x01, 0x80);
            sid.Write(0xD401, 0x80);  // freq 0x8000

            // Clock 16 cycles: bit 19 rises at cycle 16, pipeline is armed to 2.
            ClockBoth(native, sid, 16);

            // Verify oracle pipeline is 2 after bit-19 rise.
            var spPre = ViceNativeBridge.SidExactGetState(native).GetShiftPipelines();
            Assert.Equal(2u, spPre[0]);
            Assert.Equal(2u, sid.VoiceShiftPipeline(0));

            // Write test bit: should flush pipeline to 0 on both sides.
            ViceNativeBridge.SidExactWrite(native, 0x04, 0x08);
            sid.Write(0xD404, 0x08);

            var spPost = ViceNativeBridge.SidExactGetState(native).GetShiftPipelines();
            Assert.Equal(0u, spPost[0]);
            Assert.Equal(0u, sid.VoiceShiftPipeline(0));
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    /// <summary>
    /// FR: FR-SID-WAVE-TESTBIT AC-06 (DIVERGENT, finding new), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-WAVE-TESTBIT-06.
    /// Use case: on the test-bit falling edge, writeCONTROL_REG clocks the shift
    /// register exactly once with bit0 = NOT(bit17) (~shift_register >> 17 and 1,
    /// wave.cc:255). This creates the first LFSR step after the held-0 period.
    /// Acceptance: voice 0 with test bit held for 1 cycle (then released). The
    /// oracle and managed shift registers before and after the release are both
    /// captured. The shift register changes by exactly one clock step matching
    /// the formula: bit0 = (~sr >> 17) and 1, new_sr = ((sr << 1) | bit0) and 0x7FFFFF.
    /// Oracle and managed must agree exactly on the post-release value.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-WAVE-TESTBIT-06", ParityTag.Divergent, pending: false)]
    public void TestBitFalling_ClocksShiftRegisterOnceWithInvertedBit17()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);
            var sid = BuildSid();

            // Write test bit to voice 0.
            ViceNativeBridge.SidExactWrite(native, 0x04, 0x08);
            sid.Write(0xD404, 0x08);
            ClockBoth(native, sid, 1);

            // Capture shift register BEFORE release.
            uint oracleSrBefore = ViceNativeBridge.SidExactGetState(native).GetShiftRegisters()[0];
            uint managedSrBefore = sid.VoiceShiftRegister(0);

            // Release test bit (falling edge): single clock fires.
            ViceNativeBridge.SidExactWrite(native, 0x04, 0x00);
            sid.Write(0xD404, 0x00);

            // Capture shift register AFTER release.
            uint oracleSrAfter = ViceNativeBridge.SidExactGetState(native).GetShiftRegisters()[0];
            uint managedSrAfter = sid.VoiceShiftRegister(0);

            // Both pre-release values must agree.
            Assert.Equal(oracleSrBefore, managedSrBefore);

            // Apply the expected single-clock formula to verify oracle.
            uint bit0 = (~oracleSrBefore >> 17) & 1u;
            uint expectedSr = ((oracleSrBefore << 1) | bit0) & 0x7FFFFFu;
            Assert.Equal(expectedSr, oracleSrAfter);

            // Managed must match oracle exactly.
            Assert.Equal(oracleSrAfter, managedSrAfter);
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    /// <summary>
    /// FR: FR-SID-WAVE-TESTBIT AC-08 (DIVERGENT, finding new), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-WAVE-TESTBIT-08.
    /// Use case: reSID maintains a separate shift_register per WaveformGenerator
    /// instance (one per voice). The managed chip used a single shared _noiseLfsr,
    /// so all voices' test-bit state machines shared one counter, making it
    /// impossible for two voices to independently track their shift_register_reset
    /// countdowns.
    /// Acceptance: write test bit to voice 0 only; clock 10 cycles; write test bit
    /// to voice 1 as well; clock 5 more cycles. Oracle ShiftRegisterReset[0]
    /// must equal 35000-10-5 = 34985 and ShiftRegisterReset[1] must equal
    /// 35000-5 = 34995 (voice 0 has been counting down 15 cycles, voice 1 only 5).
    /// Managed VoiceShiftRegisterReset(0) and (1) must match the oracle values
    /// exactly.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-WAVE-TESTBIT-08", ParityTag.Divergent, pending: false)]
    public void TestBit_ShiftRegisterIsPerVoiceNotShared()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);
            var sid = BuildSid();

            // Write test bit to voice 0 only.
            ViceNativeBridge.SidExactWrite(native, 0x04, 0x08);
            sid.Write(0xD404, 0x08);

            // Clock 10 cycles: voice 0 reset counter decremented 10 times (= 34990).
            ClockBoth(native, sid, 10);

            // Now write test bit to voice 1 as well.
            ViceNativeBridge.SidExactWrite(native, V1Ctrl, 0x08);
            sid.Write(0xD40B, 0x08);

            // Clock 5 more cycles: voice 0 now at 34985, voice 1 at 34995.
            ClockBoth(native, sid, 5);

            var srrFinal = ViceNativeBridge.SidExactGetState(native).GetShiftRegisterResets();
            Assert.Equal(34985u, srrFinal[0]);
            Assert.Equal(34995u, srrFinal[1]);
            Assert.Equal(34985u, sid.VoiceShiftRegisterReset(0));
            Assert.Equal(34995u, sid.VoiceShiftRegisterReset(1));
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }
}
