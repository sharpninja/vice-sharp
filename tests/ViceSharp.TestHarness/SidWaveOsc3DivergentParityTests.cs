using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// PLAN-VICEPARITY-001 S2: DIVERGENT (red-now) remediation tests for the
/// OSC3-side acceptance criteria of FR-SID-OSC3ENV3 in
/// artifacts/vice-parity-requirements/requirements.yaml (AC-01 selected
/// waveform readback, AC-02 waveform dependence, AC-03 pulse rails, AC-04
/// noise readback, AC-05 8580 tri/saw pipeline delay, AC-06 tri_saw_pipeline
/// power-up seed 0x555).
///
/// Intentionally NOT authored in this slice (stopped, FAITHFUL-lock
/// conflicts; see the S2 slice report):
/// FR-SID-WAVE-ACC AC-02 (24-bit mask) and AC-05 (power-up seed 0x555555)
/// conflict with TEST-SID-VOICE-07, which pins the raw unmasked from-zero
/// accumulator capture (0x012BFED4 after 300 cycles at freq $FFFF);
/// FR-SID-WAVE-ACC AC-06 (reset preserves the accumulator) conflicts with
/// TEST-SID-VOICE-09, which pins post-Reset captured accumulators (0, 0, 0)
/// and a reset-equals-fresh OSC3 replay; FR-SID-OSC3ENV3 AC-07 (waveform 0
/// reads the fading floating-DAC osc3 latch) conflicts with
/// TEST-SID-CLOCK-11, which pins the phase-ramp readback 0x32 with no
/// waveform selected. Per the slice contract FAITHFUL locks are never
/// weakened, so those four ACs stay red-now in the artifact and the
/// waveform-0 branch of Read($D41B) keeps the legacy phase readback.
///
/// The spec is reSID (native/vice/vice/src/resid: wave.h, wave.cc, sid.cc),
/// reached bit-exactly through the single-cycle vice_sid_exact_* oracle
/// (MOS 6581 engine) where an oracle observable exists; the 8580-only
/// criteria (AC-05/AC-06) assert the wave.h/wave.cc mechanism closed-form
/// against the managed Sid8580, because the shim oracle instantiates the
/// C64 default 6581 model. All assertions are exact equality; no tolerances.
/// </summary>
[Collection("NativeVice")]
public sealed class SidWaveOsc3DivergentParityTests
{
    private const ushort FreqLoV3 = 0x0E;
    private const ushort FreqHiV3 = 0x0F;
    private const ushort PwLoV3 = 0x10;
    private const ushort PwHiV3 = 0x11;
    private const ushort ControlV3 = 0x12;
    private const ushort Osc3 = 0x1B;

    private static Sid6581 BuildSid()
    {
        var bus = new BasicBus();
        return new Sid6581(bus) { BaseAddress = 0xD400 };
    }

    private static Sid8580 BuildSid8580()
    {
        var bus = new BasicBus();
        return new Sid8580(bus) { BaseAddress = 0xD400 };
    }

    /// <summary>
    /// Pin the voice-3 accumulator to zero on both sides through the CTRL
    /// test bit (FAITHFUL mechanism, TEST-SID-WAVE-TESTBIT-01): one held
    /// test cycle zeroes the oracle accumulator at write time (wave.cc:231)
    /// and the managed accumulator inside Tick. Keeps every rig independent
    /// of the DIVERGENT power-on accumulator seed (FR-SID-WAVE-ACC AC-05,
    /// stopped this slice).
    /// </summary>
    private static void ZeroVoice3AccumulatorBothSides(IntPtr native, Sid6581 sid)
    {
        ViceNativeBridge.SidExactWrite(native, ControlV3, 0x08);
        sid.Write(0xD412, 0x08);
        ViceNativeBridge.SidExactClock(native, 1);
        sid.Tick();
    }

    /// <summary>
    /// The 23-bit noise shift register of voice 3 (index 2) via the public
    /// VoiceShiftRegister seam. S6 (PLAN-VICEPARITY-001) replaced the shared
    /// _noiseLfsr with per-voice ShiftRegister; this helper reads voice 3
    /// (index 2), which is the voice under test in Osc3_NoiseReadsNoiseOutputTopBits.
    /// </summary>
    private static uint NoiseLfsr(Sid6581 sid) => sid.VoiceShiftRegister(2);

    /// <summary>
    /// Independent reference of reSID's 12-bit noise output packing
    /// (set_noise_output, wave.h:354-367): shift-register bits 20, 18, 14,
    /// 11, 9, 5, 2, 0 land on waveform bits 11 down to 4; the low 4 bits are
    /// grounded. readOSC() therefore reports exactly these eight taps.
    /// </summary>
    private static int NoiseOutput12(uint shiftRegister) =>
        (int)(((shiftRegister & 0x100000) >> 9) |
              ((shiftRegister & 0x040000) >> 8) |
              ((shiftRegister & 0x004000) >> 5) |
              ((shiftRegister & 0x000800) >> 3) |
              ((shiftRegister & 0x000200) >> 2) |
              ((shiftRegister & 0x000020) << 1) |
              ((shiftRegister & 0x000004) << 3) |
              ((shiftRegister & 0x000001) << 4));

    /// <summary>
    /// FR: FR-SID-OSC3ENV3 AC-01 (DIVERGENT, finding 10), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-OSC3ENV3-01.
    /// Use case: reSID readOSC() returns osc3 &gt;&gt; 4 (wave.cc:293-296),
    /// the top 8 bits of the SELECTED waveform output latched by
    /// set_waveform_output (wave.h:485); the managed chip returns raw
    /// accumulator bits 16-23 (Sid6581.cs Read case 0x1B), so any waveform
    /// whose output is not the sawtooth identity reads back wrong. Triangle
    /// doubles the slope (upper 11 bits left-shifted, wave.cc:96) and folds
    /// at the accumulator MSB, so its OSC3 differs from the phase byte on
    /// almost every cycle.
    /// Acceptance: voice 3, accumulator pinned to zero via the test bit on
    /// both sides, triangle selected (CTRL $10) at FREQ $4000; for 1200
    /// single cycles the managed $D41B read equals the oracle's read($1B)
    /// bit-exactly on every cycle. Rig sanity pins the closed form at cycle
    /// 300 (rising half: 0x96 = doubled phase 0x4B) and cycle 600 (folded
    /// half after the MSB at cycle 512: 0xD3), matching wave.cc:96 exactly.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-OSC3ENV3-01", ParityTag.Divergent, pending: false)]
    public void Osc3_ReturnsSelectedTriangleWaveformOutputTopBits()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);

            var sid = BuildSid();

            ViceNativeBridge.SidExactWrite(native, FreqLoV3, 0x00);
            sid.Write(0xD40E, 0x00);
            ViceNativeBridge.SidExactWrite(native, FreqHiV3, 0x40); // FREQ $4000
            sid.Write(0xD40F, 0x40);
            ZeroVoice3AccumulatorBothSides(native, sid);
            ViceNativeBridge.SidExactWrite(native, ControlV3, 0x10); // triangle, test released
            sid.Write(0xD412, 0x10);

            for (var cycle = 1; cycle <= 1200; cycle++)
            {
                ViceNativeBridge.SidExactClock(native, 1);
                sid.Tick();
                var oracle = ViceNativeBridge.SidExactRead(native, Osc3);
                if (cycle == 300)
                {
                    Assert.Equal((byte)0x96, oracle); // rising half: ((0x4B0 & 0x7FF) << 1) >> 4
                }

                if (cycle == 600)
                {
                    Assert.Equal((byte)0xD3, oracle); // folded half: ((~0x960 & 0x7FF) << 1) >> 4
                }

                Assert.Equal(oracle, sid.Read(0xD41B));
            }
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    /// <summary>
    /// FR: FR-SID-OSC3ENV3 AC-02 (DIVERGENT, finding 10), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-OSC3ENV3-02.
    /// Use case: osc3 is the waveform output itself (wave.h:485), so two
    /// programs with the identical phase trajectory but different waveform
    /// selections read different OSC3 values; the managed phase readback
    /// collapses every waveform to the same accumulator byte.
    /// Acceptance: two runs from a zeroed voice-3 accumulator at FREQ $4000,
    /// one with sawtooth (CTRL $20) and one with triangle (CTRL $10), each
    /// match the oracle's read($1B) bit-exactly on all 700 single cycles. At
    /// cycle 600 (identical accumulator 0x960000 in both runs) sawtooth reads
    /// exactly 0x96 (accumulator &gt;&gt; 16, wave.cc:97) while triangle
    /// reads exactly 0xD3 (MSB-folded doubled slope, wave.cc:96): the same
    /// phase, two selection-dependent readbacks.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-OSC3ENV3-02", ParityTag.Divergent, pending: false)]
    public void Osc3_IsWaveformDependentNotPhaseDependent()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");

            byte RunLeg(byte control, Sid6581 sid)
            {
                ViceNativeBridge.SidExactReset(native);
                ViceNativeBridge.SidExactWrite(native, FreqLoV3, 0x00);
                sid.Write(0xD40E, 0x00);
                ViceNativeBridge.SidExactWrite(native, FreqHiV3, 0x40); // FREQ $4000
                sid.Write(0xD40F, 0x40);
                ZeroVoice3AccumulatorBothSides(native, sid);
                ViceNativeBridge.SidExactWrite(native, ControlV3, control);
                sid.Write(0xD412, control);

                byte atCycle600 = 0;
                for (var cycle = 1; cycle <= 700; cycle++)
                {
                    ViceNativeBridge.SidExactClock(native, 1);
                    sid.Tick();
                    var oracle = ViceNativeBridge.SidExactRead(native, Osc3);
                    Assert.Equal(oracle, sid.Read(0xD41B));
                    if (cycle == 600)
                    {
                        atCycle600 = oracle;
                    }
                }

                return atCycle600;
            }

            var sawtoothAt600 = RunLeg(0x20, BuildSid());
            var triangleAt600 = RunLeg(0x10, BuildSid());

            Assert.Equal((byte)0x96, sawtoothAt600); // accumulator 0x960000 >> 16
            Assert.Equal((byte)0xD3, triangleAt600); // folded triangle of the same phase
            Assert.NotEqual(sawtoothAt600, triangleAt600);
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    /// <summary>
    /// FR: FR-SID-OSC3ENV3 AC-03 (DIVERGENT, finding 10), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-OSC3ENV3-03.
    /// Use case: with pulse alone selected, osc3 is the pulse rail itself
    /// (wave[4] is the all-ones mask row, so waveform_output collapses to
    /// pulse_output; wave.h:467,485), i.e. OSC3 reads only 0x00 or 0xFF with
    /// the compare result delayed one cycle through the pulse level pipeline
    /// (wave.h:516-518); the managed chip reads the accumulator ramp instead.
    /// Acceptance: voice 3 from a zeroed accumulator, PW $800, pulse (CTRL
    /// $40) at FREQ $4000; for 1100 single cycles the managed $D41B equals
    /// the oracle's read($1B) bit-exactly on every cycle, every observed
    /// value is exactly 0x00 or 0xFF, and the pipeline boundary is exact:
    /// cycle 512 (the first cycle with accumulator &gt;&gt; 12 == pw) still
    /// reads 0x00 and cycle 513 reads 0xFF.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-OSC3ENV3-03", ParityTag.Divergent, pending: false)]
    public void Osc3_PulseOnlyReadsRailsNotARamp()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);

            var sid = BuildSid();

            ViceNativeBridge.SidExactWrite(native, FreqLoV3, 0x00);
            sid.Write(0xD40E, 0x00);
            ViceNativeBridge.SidExactWrite(native, FreqHiV3, 0x40); // FREQ $4000
            sid.Write(0xD40F, 0x40);
            ViceNativeBridge.SidExactWrite(native, PwLoV3, 0x00);
            sid.Write(0xD410, 0x00);
            ViceNativeBridge.SidExactWrite(native, PwHiV3, 0x08); // PW $800
            sid.Write(0xD411, 0x08);
            ZeroVoice3AccumulatorBothSides(native, sid);
            ViceNativeBridge.SidExactWrite(native, ControlV3, 0x40); // pulse, test released
            sid.Write(0xD412, 0x40);

            for (var cycle = 1; cycle <= 1100; cycle++)
            {
                ViceNativeBridge.SidExactClock(native, 1);
                sid.Tick();
                var oracle = ViceNativeBridge.SidExactRead(native, Osc3);
                Assert.True(oracle is 0x00 or 0xFF, $"cycle {cycle}: oracle pulse OSC3 read 0x{oracle:X2}, not a rail");
                if (cycle == 512)
                {
                    Assert.Equal((byte)0x00, oracle); // compare true this cycle, output delayed one cycle
                }

                if (cycle == 513)
                {
                    Assert.Equal((byte)0xFF, oracle); // pipelined rail lands exactly one cycle later
                }

                Assert.Equal(oracle, sid.Read(0xD41B));
            }
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    /// <summary>
    /// FR: FR-SID-OSC3ENV3 AC-04 (DIVERGENT, finding 10), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-OSC3ENV3-04.
    /// Use case: with noise selected, osc3 is noise_output (wave.h:467,485
    /// with no_noise == 0 and the wave[0] all-ones row), so readOSC() returns
    /// noise_output &gt;&gt; 4: the eight shift-register taps 20, 18, 14, 11,
    /// 9, 5, 2, 0 (wave.h:354-367). The managed chip reads the accumulator
    /// ramp instead. Each side is asserted against its own shift register
    /// because the power-on register seeds legitimately differ (managed
    /// 0x7FFFFF vs reSID reset 0x7FFFFE) and that seed is the separate
    /// DIVERGENT FR-SID-WAVE-NOISE AC-10, outside this slice.
    /// Acceptance: voice 3 noise (CTRL $80) at FREQ $FFFF; on every one of
    /// 600 single cycles the oracle's read($1B) equals exactly the tap
    /// packing of its exported voice-3 shift register shifted right 4, the
    /// managed $D41B read equals exactly the identical tap packing of the
    /// managed noise register, and the oracle register demonstrably shifted
    /// during the window.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-OSC3ENV3-04", ParityTag.Divergent, pending: false)]
    public void Osc3_NoiseReadsNoiseOutputTopBits()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);

            var sid = BuildSid();

            ViceNativeBridge.SidExactWrite(native, FreqLoV3, 0xFF);
            sid.Write(0xD40E, 0xFF);
            ViceNativeBridge.SidExactWrite(native, FreqHiV3, 0xFF); // FREQ $FFFF
            sid.Write(0xD40F, 0xFF);
            ViceNativeBridge.SidExactWrite(native, ControlV3, 0x80); // noise
            sid.Write(0xD412, 0x80);

            var initialOracleRegister = ViceNativeBridge.SidExactGetState(native).GetShiftRegisters()[2];
            var oracleRegisterChanged = false;
            for (var cycle = 1; cycle <= 600; cycle++)
            {
                ViceNativeBridge.SidExactClock(native, 1);
                sid.Tick();

                var oracleRegister = ViceNativeBridge.SidExactGetState(native).GetShiftRegisters()[2];
                oracleRegisterChanged |= oracleRegister != initialOracleRegister;
                Assert.Equal(
                    (byte)(NoiseOutput12(oracleRegister) >> 4),
                    ViceNativeBridge.SidExactRead(native, Osc3));

                Assert.Equal(
                    (byte)(NoiseOutput12(NoiseLfsr(sid)) >> 4),
                    sid.Read(0xD41B));
            }

            Assert.True(oracleRegisterChanged, "rig sanity: the oracle noise register never shifted in 600 cycles");
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    /// <summary>
    /// FR: FR-SID-OSC3ENV3 AC-05 (DIVERGENT, finding 10), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-OSC3ENV3-05.
    /// Use case: on the MOS 8580 the tri/saw component of OSC3 is served one
    /// cycle late through tri_saw_pipeline (wave.h:475-482: osc3 gets the
    /// pipelined previous wave[ix], then the pipeline latches the current
    /// one), while the 6581 latches waveform_output directly (wave.h:485).
    /// The shim oracle instantiates the C64 default 6581, so the 8580 branch
    /// is asserted closed-form against a lockstep managed 6581.
    /// Acceptance: managed Sid8580 and Sid6581 run the identical voice-3
    /// program (accumulator zeroed via the test bit, sawtooth CTRL $20,
    /// FREQ $FFFF). On every one of 600 cycles the 6581 reads exactly
    /// (cycle - 1) AND 0xFF (sawtooth = accumulator top byte) and the 8580
    /// reads exactly the 6581's PREVIOUS cycle value (0x00 on cycle 1: the
    /// control write latched wave[ix] of the zeroed accumulator into the
    /// pipeline). At cycle 2 the sides differ (0x01 vs 0x00), witnessing the
    /// one-cycle delay.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-OSC3ENV3-05", ParityTag.Divergent, pending: false)]
    public void Osc3_8580TriSawReadbackIsDelayedOneCycleThroughPipeline()
    {
        var sid8580 = BuildSid8580();
        var sid6581 = BuildSid();

        foreach (Sid6581 sid in new Sid6581[] { sid8580, sid6581 })
        {
            sid.Write(0xD412, 0x08); // test bit: pin the accumulator to zero (seed-robust rig)
            sid.Tick();
            sid.Write(0xD412, 0x20); // sawtooth, test released
            sid.Write(0xD40E, 0xFF);
            sid.Write(0xD40F, 0xFF); // FREQ $FFFF
        }

        byte previous6581 = 0x00;
        for (var cycle = 1; cycle <= 600; cycle++)
        {
            sid8580.Tick();
            sid6581.Tick();

            var current6581 = sid6581.Read(0xD41B);
            Assert.Equal((byte)((cycle - 1) & 0xFF), current6581); // sawtooth closed form from zero
            Assert.Equal(previous6581, sid8580.Read(0xD41B));      // 8580 serves the previous cycle
            if (cycle == 2)
            {
                Assert.Equal((byte)0x01, current6581);
                Assert.Equal((byte)0x00, sid8580.Read(0xD41B)); // delay witness: sides differ
            }

            previous6581 = current6581;
        }
    }

    /// <summary>
    /// FR: FR-SID-OSC3ENV3 AC-06 (DIVERGENT, finding new), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-OSC3ENV3-06.
    /// Use case: tri_saw_pipeline powers up at 0x555 (wave.cc:119, the
    /// even-bits-high die state) and reset() does not touch it
    /// (wave.cc:301-332), so the very first 8580 tri/saw OSC3 read after a
    /// waveform is selected exposes the seed: writeCONTROL_REG runs
    /// set_waveform_output immediately (wave.cc:261-264), which serves the
    /// seeded pipeline into osc3 and latches wave[ix] behind it.
    /// Acceptance: on a fresh managed Sid8580 whose voice-3 accumulator was
    /// pinned to zero via the test bit (so the witness is independent of the
    /// stopped FR-SID-WAVE-ACC AC-05 accumulator seed), writing CTRL $20
    /// makes $D41B read exactly 0x55 (0x555 &gt;&gt; 4) with no cycle
    /// elapsed; after exactly one Tick it reads exactly 0x00 (the pipeline
    /// now holds wave[0] of the zeroed accumulator). A fresh Sid6581 given
    /// the identical program reads exactly 0x00 immediately: the seed is
    /// visible only through the 8580 pipeline path.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-OSC3ENV3-06", ParityTag.Divergent, pending: false)]
    public void Osc3_8580TriSawPipelinePowersUpAt0x555()
    {
        var sid8580 = BuildSid8580();
        sid8580.Write(0xD412, 0x08); // test bit: pin the accumulator to zero
        sid8580.Tick();
        sid8580.Write(0xD412, 0x20); // sawtooth: write-time refresh serves the pipeline seed

        Assert.Equal((byte)0x55, sid8580.Read(0xD41B)); // 0x555 >> 4, no cycle elapsed

        sid8580.Tick();
        Assert.Equal((byte)0x00, sid8580.Read(0xD41B)); // seed consumed; pipeline now wave[0] = 0

        var sid6581 = BuildSid();
        sid6581.Write(0xD412, 0x08);
        sid6581.Tick();
        sid6581.Write(0xD412, 0x20);
        Assert.Equal((byte)0x00, sid6581.Read(0xD41B)); // 6581 has no tri/saw OSC3 pipeline
    }
}
