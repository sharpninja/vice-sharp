using System.Buffers.Binary;
using System.Reflection;
using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;
using ReSidEnv = ViceSharp.Chips.Audio.Sid6581.ReSidEnvelope;

namespace ViceSharp.TestHarness;

/// <summary>
/// PLAN-VICEPARITY-001 S1: DIVERGENT (red-now) remediation tests for the
/// envelope and clock-dispatch acceptance criteria of
/// artifacts/vice-parity-requirements/requirements.yaml. Scope: the three
/// DIVERGENT FR-SID-ENV criteria (AC-07 reset-preserve, AC-08 power-up 0xaa,
/// AC-50 model_dac output) and the clock-dispatch DIVERGENT FR-SID-CLOCK
/// criteria (AC-01, AC-02, AC-03, AC-06, AC-07, AC-09). FR-SID-OSC3ENV3
/// contributes nothing: its ENV3-side criteria (AC-09/10/11) are all FAITHFUL
/// and already locked; its DIVERGENT criteria are all OSC3-side (other
/// slice). FR-SID-CLOCK AC-05 and AC-08 are the batched clock(delta_t)
/// oscillator sub-stepping and 8580 SAMPLE_FAST write-pipeline prologue of
/// the sampling machinery; per the slice plan they belong to the
/// resampling/8580 slices (AC-05's red component is owned by
/// FR-SID-WAVE-SYNC AC-01/AC-04) and are intentionally absent here.
///
/// The spec is reSID (native/vice/vice/src/resid: envelope.h, envelope.cc,
/// dac.cc, sid.h, sid.cc), reached bit-exactly through the single-cycle
/// vice_sid_exact_* oracle where an oracle observable exists. All assertions
/// are exact equality; no tolerances.
/// </summary>
[Collection("NativeVice")]
public sealed class SidEnvDivergentParityTests
{
    private const ushort FreqLoV3 = 0x0E;
    private const ushort ControlV3 = 0x12;
    private const ushort AttackDecayV3 = 0x13;
    private const ushort SustainReleaseV3 = 0x14;
    private const ushort Env3 = 0x1C;

    /// <summary>MOS 6581 write-to-bus TTL (reSID sid.cc:119, model 6581 branch).</summary>
    private const int DataBusTtl6581 = 0x1D00;

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

    /// <summary>Post-clock envelope counter mirror of a voice via the public CaptureState layout.</summary>
    private static byte EnvelopeLevel(Sid6581 sid, int voice)
    {
        Span<byte> state = stackalloc byte[sid.StateSize];
        sid.CaptureState(state);
        return state[0x20 + voice * 6 + 4];
    }

    /// <summary>All three envelope counter mirrors via the public CaptureState layout.</summary>
    private static byte[] EnvelopeLevels(Sid6581 sid)
    {
        Span<byte> state = stackalloc byte[sid.StateSize];
        sid.CaptureState(state);
        return [state[0x20 + 4], state[0x20 + 6 + 4], state[0x20 + 12 + 4]];
    }

    /// <summary>Raw stored 32-bit accumulator of a voice via the public CaptureState layout.</summary>
    private static uint RawAccumulator(Sid6581 sid, int voice)
    {
        Span<byte> state = stackalloc byte[sid.StateSize];
        sid.CaptureState(state);
        return BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(0x20 + voice * 6, 4));
    }

    /// <summary>
    /// Tick the chip until the envelope counter mirror of <paramref name="voice"/>
    /// reaches <paramref name="target"/> (the reSID envelope only moves in
    /// single steps, so every level on the trajectory is hit exactly).
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
    /// Mirror of the exact GenerateSample arithmetic for a single active voice
    /// whose 8-bit waveform sample is <paramref name="waveformSample"/> and
    /// whose envelope DAC level is <paramref name="envelopeDacLevel"/>, at
    /// master volume 15 with filter mode bits clear:
    /// envelopeAdjusted = ((sample - 0x380) * dacLevel) arithmetic-shifted
    /// right 8, then envelopeAdjusted / 2048f + 0.05f, clamped to [-1, 1].
    /// wave_zero is 0x380 for the 6581 die (voice.cc:93); the 8-bit form
    /// WaveZeroLevel=0x38 is multiplied by 0x10 in ComputeVoiceOutput to reach
    /// the 12-bit domain [PLAN-VICEPARITY-001 S3].
    /// Same operation sequence as Sid6581.GenerateSample, so float equality
    /// is exact.
    /// </summary>
    private static float ExpectedSample(int waveformSample, int envelopeDacLevel)
    {
        var envelopeAdjusted = ((waveformSample - 0x380) * envelopeDacLevel) >> 8;
        const float VolumeFraction = 15 / 15.0f;
        var voiceMix = envelopeAdjusted * VolumeFraction / 2048.0f;
        const float DigiDcOffset = VolumeFraction * 0.05f;
        return Math.Clamp(voiceMix + DigiDcOffset, -1.0f, 1.0f);
    }

    /// <summary>
    /// Independent test-side reference of reSID's 8-bit MOS 6581 envelope DAC,
    /// ported statement-for-statement from resid/dac.cc build_dac_table
    /// (dac.cc:76-137) with the EnvelopeGenerator constructor parameters
    /// bits=8, 2R/R=2.20, term=false (envelope.cc:164-166) and 6581 MOSFET
    /// leakage 0.0075 (dac.cc:46). The R-2R ladder voltage of each bit is
    /// computed by repeated parallel substitution plus one source
    /// transformation, output voltages superposition per set bit (unset bits
    /// contribute the leakage fraction), and each entry is scaled by
    /// (2^8 - 1) and rounded via + 0.5 truncation, exactly as dac.cc does.
    /// Derivation anchors: dac[0x00]=2, dac[0x06]=9, dac[0x1A]=30,
    /// dac[0x40]=65, dac[0x5D]=97, dac[0xAA]=168, dac[0xFF]=255.
    /// </summary>
    private static ushort[] BuildMos6581EnvelopeDacReference()
    {
        const int Bits = 8;
        const double TwoRDivR = 2.20;
        const double Leakage = 0.0075; // MOSFET_LEAKAGE_6581, dac.cc:46

        var vbit = new double[Bits];
        for (var setBit = 0; setBit < Bits; setBit++)
        {
            int bit;
            var vn = 1.0;
            const double R = 1.0;
            const double TwoR = TwoRDivR * R;
            var rn = double.PositiveInfinity; // term=false: missing termination resistor

            for (bit = 0; bit < setBit; bit++)
            {
                rn = double.IsPositiveInfinity(rn) ? R + TwoR : R + TwoR * rn / (TwoR + rn);
            }

            if (double.IsPositiveInfinity(rn))
            {
                rn = TwoR;
            }
            else
            {
                rn = TwoR * rn / (TwoR + rn);
                vn = vn * rn / TwoR;
            }

            for (++bit; bit < Bits; bit++)
            {
                rn += R;
                var current = vn / rn;
                rn = TwoR * rn / (TwoR + rn);
                vn = rn * current;
            }

            vbit[setBit] = vn;
        }

        var dac = new ushort[1 << Bits];
        for (var i = 0; i < (1 << Bits); i++)
        {
            var x = i;
            var vo = 0.0;
            for (var j = 0; j < Bits; j++)
            {
                vo += ((x & 0x1) != 0 ? 1.0 : Leakage) * vbit[j];
                x >>= 1;
            }

            dac[i] = (ushort)(((1 << Bits) - 1) * vo + 0.5);
        }

        return dac;
    }

    /// <summary>
    /// FR: FR-SID-ENV AC-07 (DIVERGENT, finding 03), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-ENV-07.
    /// Use case: reSID's EnvelopeGenerator::reset() explicitly leaves the
    /// envelope counter alone ("counter is not changed on reset",
    /// envelope.cc:189); the managed port forces it to zero, so any music
    /// player relying on the post-reset release-from-current-level behaviour
    /// diverges from real hardware.
    /// Acceptance: with attack 0 / sustain 0 / release 0 and gate on, the
    /// envelope is walked to exactly 0x40 (every level is hit because the
    /// counter moves in single steps), then reset. The native oracle
    /// (reSID::SID::reset()) reports envelope_counter[2] == 0x40 after reset;
    /// the managed ReSidEnvelope struct and the managed chip-level Reset()
    /// must both preserve exactly 0x40, and one post-reset clock must make
    /// ENV3 read exactly 0x40 on both sides. Bit-exact, no tolerance.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-ENV-07", ParityTag.Divergent, pending: false)]
    public void EnvelopeReset_PreservesEnvelopeCounterExactlyAsReSid()
    {
        // Native oracle: walk voice 3 to counter 0x40, then reset.
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);
            ViceNativeBridge.SidExactWrite(native, AttackDecayV3, 0x00);
            ViceNativeBridge.SidExactWrite(native, SustainReleaseV3, 0x00);
            ViceNativeBridge.SidExactWrite(native, ControlV3, 0x01);

            var cycles = 0;
            while (ViceNativeBridge.SidExactGetState(native).GetEnvelopeCounters()[2] != 0x40 && cycles < 6000)
            {
                ViceNativeBridge.SidExactClock(native, 1);
                cycles++;
            }

            Assert.True(cycles < 6000, "oracle rig sanity: envelope never reached 0x40");

            ViceNativeBridge.SidExactReset(native);
            byte oracleAfterReset = ViceNativeBridge.SidExactGetState(native).GetEnvelopeCounters()[2];
            Assert.Equal((byte)0x40, oracleAfterReset);

            // Managed struct level (the managedCite line lives in ReSidEnvelope.Reset()).
            var env = default(ReSidEnv);
            env.Reset();
            env.WriteAttackDecay(0x00);
            env.WriteSustainRelease(0x00);
            env.WriteControl(0x01);
            var structCycles = 0;
            while (env.EnvelopeCounter != 0x40 && structCycles < 6000)
            {
                env.Clock();
                structCycles++;
            }

            Assert.True(structCycles < 6000, "struct rig sanity: envelope never reached 0x40");
            env.Reset();
            Assert.Equal(oracleAfterReset, env.EnvelopeCounter);

            // Managed chip level: Sid6581.Reset() must not clobber the counter either.
            var sid = BuildSid();
            sid.Write(0xD413, 0x00);
            sid.Write(0xD414, 0x00);
            sid.Write(0xD412, 0x01);
            TickUntilEnvelope(sid, voice: 2, target: 0x40, maxCycles: 6000);
            sid.Reset();
            Assert.Equal(oracleAfterReset, EnvelopeLevel(sid, 2));

            // One post-reset clock relatches ENV3 from the preserved counter on both sides.
            ViceNativeBridge.SidExactClock(native, 1);
            sid.Tick();
            Assert.Equal(ViceNativeBridge.SidExactRead(native, Env3), sid.Read(0xD41C));
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    /// <summary>
    /// FR: FR-SID-ENV AC-08 (DIVERGENT, finding 03), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-ENV-08.
    /// Use case: on power-up the envelope counter's odd bits are high
    /// (envelope_counter = 0xaa, envelope.cc:176) and reset() preserves that
    /// seed, so a fresh chip's idle envelopes release from 0xaa toward zero;
    /// the managed chip seeds 0, which even wraps to 0xff through the release
    /// decrement instead of decaying like hardware.
    /// Acceptance: a freshly opened oracle reports envelope_counter ==
    /// (0xaa, 0xaa, 0xaa) with no writes and no clocks, and the fresh managed
    /// chip's captured counters equal that exactly. Clocking both sides one
    /// cycle at a time for 600 cycles with no register writes produces
    /// bit-identical ENV3 streams (the idle release from 0xaa first steps at
    /// cycle 12 and then every 9 cycles: rate_counter_period[0] == 8 plus the
    /// one-cycle reset delay), and the exported/captured voice-3 counters are
    /// equal at cycles 1 and 600 (0x68 at 600).
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-ENV-08", ParityTag.Divergent, pending: false)]
    public void PowerUp_SeedsEnvelopeCounter0xAAAndReleasesExactlyAsReSid()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);

            var sid = BuildSid();

            var oracleCounters = ViceNativeBridge.SidExactGetState(native).GetEnvelopeCounters();
            Assert.Equal(new byte[] { 0xAA, 0xAA, 0xAA }, oracleCounters);
            Assert.Equal(oracleCounters, EnvelopeLevels(sid));

            for (var cycle = 1; cycle <= 600; cycle++)
            {
                ViceNativeBridge.SidExactClock(native, 1);
                sid.Tick();
                Assert.Equal(ViceNativeBridge.SidExactRead(native, Env3), sid.Read(0xD41C));
                if (cycle is 1 or 600)
                {
                    Assert.Equal(
                        ViceNativeBridge.SidExactGetState(native).GetEnvelopeCounters()[2],
                        EnvelopeLevel(sid, 2));
                }
            }

            Assert.Equal((byte)0x68, EnvelopeLevel(sid, 2)); // idle release from 0xaa after 600 cycles
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    /// <summary>
    /// FR: FR-SID-ENV AC-50 (DIVERGENT, finding 03), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-ENV-50.
    /// Use case: reSID's envelope output is model_dac[sid_model]
    /// [envelope_counter] (envelope.h:377-383), the nonlinear 8-bit R-2R DAC
    /// with the 6581's missing termination resistor; the managed voice path
    /// multiplies by the raw envelope counter, which is only correct at the
    /// handful of levels where the 6581 DAC happens to be linear.
    /// Acceptance: with voice 3 held at waveform sample 0 (TEST bit pins the
    /// accumulator, sawtooth selected), master volume 15 and no filter mode
    /// bits, the envelope is parked at the 0xFF plateau (attack 0, sustain 15)
    /// and then walked down through release 0; at counter levels 0xFF, 0xAA,
    /// 0x5D, 0x40 and 0x1A the sample equals exactly the value computed from
    /// the dac.cc-derived 6581 table (255, 168, 97, 65, 30): envelopeAdjusted
    /// (0x000 - 0x380) * dac[level] arithmetic-shifted right 8 gives -893,
    /// -588, -340, -228, -105 [PLAN-VICEPARITY-001 S3 rebase: 12-bit Osc3 path;
    /// the old 8-bit values were -56, -37, -22, -15, -7], while the linear
    /// counter gives divergent values at the non-plateau levels, so every
    /// non-plateau level is a strict divergence witness. Exact float equality
    /// via the mirrored GenerateSample arithmetic.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-ENV-50", ParityTag.Divergent, pending: false)]
    public void EnvelopeOutput_MapsThroughMos6581ModelDacTable()
    {
        var dac = BuildMos6581EnvelopeDacReference();
        Assert.Equal(255, dac[0xFF]);
        Assert.Equal(168, dac[0xAA]);
        Assert.Equal(97, dac[0x5D]);
        Assert.Equal(65, dac[0x40]);
        Assert.Equal(30, dac[0x1A]);

        var sid = BuildSid();
        sid.Write(0xD418, 0x0F);        // master volume 15, filter mode bits clear
        sid.Write(0xD413, 0x00);        // voice 3 attack 0 / decay 0
        sid.Write(0xD414, 0xF0);        // voice 3 sustain 15 / release 0
        sid.Write(0xD412, 0x29);        // voice 3 TEST | sawtooth | gate: waveform sample pinned to 0

        TickUntilEnvelope(sid, voice: 2, target: 0xFF, maxCycles: 6000);
        Assert.Equal(ExpectedSample(0x00, dac[0xFF]), sid.GenerateSample());

        sid.Write(0xD412, 0x28);        // gate off: release 0 walks the counter down

        foreach (var level in new byte[] { 0xAA, 0x5D, 0x40, 0x1A })
        {
            TickUntilEnvelope(sid, voice: 2, target: level, maxCycles: 6000);
            Assert.Equal(ExpectedSample(0x00, dac[level]), sid.GenerateSample());
        }
    }

    /// <summary>
    /// FR: FR-SID-CLOCK AC-01 (DIVERGENT, finding 13), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-CLOCK-01.
    /// Use case: reSID's single-cycle SID::clock() runs the full chain
    /// envelopes, oscillators, synchronize, waveform outputs, filter,
    /// external filter, pipelined-write slot, bus aging (sid.h:200-244); the
    /// managed Tick omitted the filter/extfilt/pipeline/bus tail entirely, so
    /// nothing aged the data bus and no pipelined-write slot existed.
    /// Acceptance: after writing 0x5A to register $00 on both sides, the
    /// oracle bus is observed through side-effect-free reads of the
    /// write-only register $00 (only $19-$1C latch the bus, sid.cc:176-197;
    /// the state export cannot be used because reSID read_state() itself
    /// latches the bus via read($19-$1C), sid.cc:380-388). The oracle read
    /// returns 0x5A up to and including cumulative cycle 0x1CFF and exactly
    /// 0x00 from cycle 0x1D00 (the chain tail's if (!--bus_value_ttl) fires,
    /// sid.h:236-239, pinning the 6581 TTL 0x1D00 of sid.cc:119). The managed
    /// chain must match: (DataBusValue, DataBusValueTtl) == (0x5A, 0x1D00)
    /// after the write, (0x5A, 0x1CFF) after one cycle, (0x5A, 1) at 0x1CFF,
    /// (0, 0) at 0x1D00, and (0, -5) five cycles later (the decrement
    /// continues past zero exactly as in sid.h). The pipelined-write slot
    /// reads 0 on both sides (6581 writes commit immediately; the slot is
    /// still checked every cycle per sid.h:231-234).
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-CLOCK-01", ParityTag.Divergent, pending: false)]
    public void PerCycleClockChain_RunsPipelinedWriteAndBusAgingStages()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);

            var sid = BuildSid();

            ViceNativeBridge.SidExactWrite(native, 0x00, 0x5A);
            sid.Write(0xD400, 0x5A);
            AssertBusStateMatches(native, sid, busRegister: 0x00, expectedValue: 0x5A, expectedManagedTtl: DataBusTtl6581);

            ViceNativeBridge.SidExactClock(native, 1);
            TickN(sid, 1);
            AssertBusStateMatches(native, sid, busRegister: 0x00, expectedValue: 0x5A, expectedManagedTtl: DataBusTtl6581 - 1);

            ViceNativeBridge.SidExactClock(native, DataBusTtl6581 - 2);
            TickN(sid, DataBusTtl6581 - 2);
            AssertBusStateMatches(native, sid, busRegister: 0x00, expectedValue: 0x5A, expectedManagedTtl: 1);

            ViceNativeBridge.SidExactClock(native, 1);
            TickN(sid, 1);
            AssertBusStateMatches(native, sid, busRegister: 0x00, expectedValue: 0x00, expectedManagedTtl: 0);

            ViceNativeBridge.SidExactClock(native, 5);
            TickN(sid, 5);
            AssertBusStateMatches(native, sid, busRegister: 0x00, expectedValue: 0x00, expectedManagedTtl: -5);

            // Safe only after the last bus assertion: the state export latches
            // the bus as a side effect (reSID read_state, sid.cc:380-388).
            Assert.Equal(0, ViceNativeBridge.SidExactGetState(native).WritePipeline);
            Assert.Equal(0, sid.PipelinedWriteSlot);
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    /// <summary>
    /// Asserts the reSID bus state at a checkpoint: the oracle's
    /// side-effect-free read of the write-only <paramref name="busRegister"/>
    /// returns <paramref name="expectedValue"/> (reads of $00-$18 return
    /// bus_value without latching, sid.cc:176-197), and the managed chain
    /// seams report exactly that value with <paramref name="expectedManagedTtl"/>
    /// remaining (the ttl trajectory is pinned by the oracle's observed fade
    /// boundary at 0x1D00 elapsed cycles).
    /// </summary>
    private static void AssertBusStateMatches(IntPtr native, Sid6581 sid, ushort busRegister, byte expectedValue, int expectedManagedTtl)
    {
        Assert.Equal(expectedValue, ViceNativeBridge.SidExactRead(native, busRegister));
        Assert.Equal(expectedValue, sid.DataBusValue);
        Assert.Equal(expectedManagedTtl, sid.DataBusValueTtl);
    }

    /// <summary>
    /// FR: FR-SID-CLOCK AC-02 (DIVERGENT, finding 13), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-CLOCK-02.
    /// Use case: within each cycle the filter is fed the three voice outputs
    /// computed after set_waveform_output, i.e. from this cycle's post-sync
    /// oscillator state (sid.h:220-226); the managed chip computed voice
    /// outputs lazily inside GenerateSample instead, so no per-cycle values
    /// were ever fed to a filter stage.
    /// Acceptance: voice 0 (sawtooth, SYNC, gate, envelope parked at 0xFF)
    /// runs at freq $0100 against sync source voice 2 at freq $8000 from
    /// zeroed accumulators. At cycle 511 the per-cycle voice outputs fed to
    /// the filter stage are exactly (-862, 0, 0): sawtooth ix = acc>>12 =
    /// 511*0x100>>12 = 0x1FF00>>12 = 0x1F = 31, so ((31 - 0x380) * 255)>>8.
    /// At cycle 512 the source MSB edge resets the destination accumulator and
    /// the fed value is exactly (-893, 0, 0): post-sync ix=0x000 gives
    /// ((0x000 - 0x380) * 255) >> 8 = -893, not the stale pre-sync -861.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-CLOCK-02", ParityTag.Divergent, pending: false)]
    public void FilterStage_IsFedVoiceOutputsComputedAfterSynchronize()
    {
        var sid = BuildSid();
        // Pin all accumulators to zero via the CTRL test bit (FAITHFUL
        // TEST-SID-WAVE-TESTBIT-01) so the rig's phase closed forms do not
        // depend on the power-on accumulator seed (FR-SID-WAVE-ACC AC-05).
        // [S3 relock: rig previously relied on the legacy zero power-on.]
        for (var v = 0; v < 3; v++)
        {
            WriteVoice(sid, v, 4, 0x08);
        }

        sid.Tick();
        for (var v = 0; v < 3; v++)
        {
            WriteVoice(sid, v, 4, 0x00);
        }

        sid.Write(0xD418, 0x0F);        // master volume 15, no filter mode bits
        WriteVoice(sid, 0, 5, 0x00);    // attack 0 / decay 0
        WriteVoice(sid, 0, 6, 0xF0);    // sustain 15 / release 0
        WriteVoice(sid, 0, 4, 0x23);    // sawtooth | sync | gate, freq still 0

        TickUntilEnvelope(sid, voice: 0, target: 0xFF, maxCycles: 6000);
        Assert.Equal(0u, RawAccumulator(sid, 0) & 0xFFFFFFu);

        WriteVoice(sid, 0, 1, 0x01);    // destination freq $0100
        WriteVoice(sid, 2, 1, 0x80);    // sync source freq $8000: MSB falls at cycle 512

        TickN(sid, 511);
        Assert.Equal((-862, 0, 0), sid.CycleVoiceOutputs); // ix=31=(511*$100)>>12: ((31 - 0x380) * 255) >> 8

        sid.Tick();                     // source MSB edge: destination resynced to phase 0
        Assert.Equal((-893, 0, 0), sid.CycleVoiceOutputs); // post-sync ix=0: ((0 - 0x380) * 255) >> 8
    }

    /// <summary>
    /// FR: FR-SID-CLOCK AC-03 (DIVERGENT, finding 21), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-CLOCK-03.
    /// Use case: reSID's batched clock(delta_t) ages the bus first
    /// (bus_value_ttl -= delta_t, sid.cc:762-767); the per-cycle managed
    /// equivalent must age the bus exactly once per clocked cycle so that N
    /// elapsed cycles reduce the ttl by exactly N, which is what the exact
    /// single-cycle oracle accumulates too. The managed chip had no bus
    /// aging at all.
    /// Acceptance: after writing 0xA5 to register $02 on both sides, the
    /// oracle's side-effect-free read of write-only register $02 returns
    /// 0xA5 at 1, 2, 250 and 0x1000 cumulative cycles while the managed ttl
    /// reads exactly 0x1D00 minus the elapsed cycles; a mid-fade rewrite of
    /// 0x3C to register $07 reloads both sides to (0x3C, 0x1D00); after a
    /// further 0x1D00 cycles both read 0x00 with managed ttl 0 (the fade
    /// boundary observed on the oracle pins the trajectory), then managed
    /// ttl -5 after five more cycles. Bit-exact equality at every checkpoint.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-CLOCK-03", ParityTag.Divergent, pending: false)]
    public void BusValue_AgesByExactlyTheElapsedClockedCycles()
    {
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);

            var sid = BuildSid();

            ViceNativeBridge.SidExactWrite(native, 0x02, 0xA5);
            sid.Write(0xD402, 0xA5);

            var elapsed = 0;
            foreach (var checkpoint in new[] { 1, 2, 250, 0x1000 })
            {
                ViceNativeBridge.SidExactClock(native, checkpoint - elapsed);
                TickN(sid, checkpoint - elapsed);
                elapsed = checkpoint;
                AssertBusStateMatches(native, sid, busRegister: 0x02, expectedValue: 0xA5, expectedManagedTtl: DataBusTtl6581 - elapsed);
            }

            // Mid-fade rewrite reloads value and ttl on both sides.
            ViceNativeBridge.SidExactWrite(native, 0x07, 0x3C);
            sid.Write(0xD407, 0x3C);
            AssertBusStateMatches(native, sid, busRegister: 0x07, expectedValue: 0x3C, expectedManagedTtl: DataBusTtl6581);

            ViceNativeBridge.SidExactClock(native, DataBusTtl6581);
            TickN(sid, DataBusTtl6581);
            AssertBusStateMatches(native, sid, busRegister: 0x07, expectedValue: 0x00, expectedManagedTtl: 0);

            ViceNativeBridge.SidExactClock(native, 5);
            TickN(sid, 5);
            AssertBusStateMatches(native, sid, busRegister: 0x07, expectedValue: 0x00, expectedManagedTtl: -5);
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    /// <summary>
    /// FR: FR-SID-CLOCK AC-06 (DIVERGENT, finding 13), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-CLOCK-06.
    /// Use case: reSID computes each voice's waveform output exactly once per
    /// cycle, after the oscillator/synchronize passes (sid.cc:822-825,
    /// sid.h:220-223); the managed chip computed it lazily at GenerateSample
    /// time, so a register write between cycles retroactively changed the
    /// "current" output without any clock elapsing, which real hardware
    /// cannot do.
    /// Acceptance: voice 0 pulse (PW $800, envelope parked at 0xFF, volume
    /// 15) runs 100 cycles at freq $8000 to 12-bit phase 0x320 (acc=0x320000,
    /// acc>>12=0x320=800). reSID pulse is HIGH when (acc>>12) >= pw (wave.h:518);
    /// 800 < 2048 so pulse is LOW = 0x000. GenerateSample reads exactly the
    /// committed low-rail sample (mirrored arithmetic with dac[0xFF] = 255),
    /// twice in a row (reading commits nothing). Rewriting PW_HI to 0 without
    /// ticking must leave the sample bit-identical (lazy impl would flip); after
    /// exactly one further Tick the PW=0 takes effect and any phase >= 0 is HIGH
    /// = 0xFFF, so the sample equals the exact high-rail value.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-CLOCK-06", ParityTag.Divergent, pending: false)]
    public void WaveformOutput_IsCommittedOncePerCycleNotLazilyAtSampleTime()
    {
        var sid = BuildSid();
        // Pin the accumulator to zero via the CTRL test bit (seed-robust rig,
        // FAITHFUL TEST-SID-WAVE-TESTBIT-01; the power-on seed is the
        // DIVERGENT FR-SID-WAVE-ACC AC-05). [S3 relock: rig previously
        // relied on the legacy zero power-on.]
        WriteVoice(sid, 0, 4, 0x08);
        sid.Tick();

        sid.Write(0xD418, 0x0F);        // master volume 15, no filter mode bits
        WriteVoice(sid, 0, 2, 0x00);    // PW lo
        WriteVoice(sid, 0, 3, 0x08);    // PW hi: threshold phase 0x80
        WriteVoice(sid, 0, 5, 0x00);    // attack 0 / decay 0
        WriteVoice(sid, 0, 6, 0xF0);    // sustain 15 / release 0
        WriteVoice(sid, 0, 4, 0x41);    // pulse | gate, freq still 0

        TickUntilEnvelope(sid, voice: 0, target: 0xFF, maxCycles: 6000);
        WriteVoice(sid, 0, 1, 0x80);    // freq $8000
        TickN(sid, 100);                // phase 50: below threshold 0x80, high rail

        var lowRail = ExpectedSample(0x000, 255);
        Assert.Equal(lowRail, sid.GenerateSample());
        Assert.Equal(lowRail, sid.GenerateSample()); // sampling is a pure read of committed state

        WriteVoice(sid, 0, 3, 0x00);    // PW=0: comparator would flip HIGH, but no cycle elapsed
        Assert.Equal(lowRail, sid.GenerateSample());

        sid.Tick();                     // the write takes effect in the next per-cycle chain run
        Assert.Equal(ExpectedSample(0xFFF, 255), sid.GenerateSample());
    }

    /// <summary>
    /// FR: FR-SID-CLOCK AC-07 (DIVERGENT, finding 13), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-CLOCK-07.
    /// Use case: every cycle clocks the filter with the voice outputs and
    /// then the external filter with filter.output() (sid.cc:827-831,
    /// sid.h:225-229); the managed chip had no per-cycle filter/extfilt
    /// dispatch at all. The numeric filter and external-filter models belong
    /// to FR-SID-FILTER-6581/FR-SID-EXTFILT; this criterion pins the
    /// dispatch: both stages run every cycle and the external filter consumes
    /// exactly this cycle's filter output.
    /// Acceptance: voice 3 (TEST | sawtooth | gate) holds waveform sample 0
    /// (12-bit OSC3 = 0x000 via set_waveform_output); with filter mode bits
    /// clear the filter stage is the exact unity sum of the per-cycle voice
    /// outputs. At the 0xFF envelope plateau the committed filter output and
    /// the external-filter output both read exactly -893 (((0x000 - 0x380) *
    /// 255) >> 8); after gate-off walks the envelope to 0xAA they both read
    /// exactly -588 ((0x000 - 0x380) * dac[0xAA]=168 >> 8), proving both
    /// stages re-clock from fresh voice outputs every cycle.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-CLOCK-07", ParityTag.Divergent, pending: false)]
    public void FilterAndExternalFilterStages_ClockEveryCycleFromChainOutputs()
    {
        var sid = BuildSid();
        sid.Write(0xD413, 0x00);        // attack 0 / decay 0
        sid.Write(0xD414, 0xF0);        // sustain 15 / release 0
        sid.Write(0xD412, 0x29);        // TEST | sawtooth | gate: waveform sample pinned to 0

        TickUntilEnvelope(sid, voice: 2, target: 0xFF, maxCycles: 6000);
        Assert.Equal(-893, sid.CycleFilterOutput);           // (0x000 - 0x380) * 255 >> 8
        Assert.Equal(-893, sid.CycleExternalFilterOutput);   // extfilt consumes this cycle's filter output

        sid.Write(0xD412, 0x28);        // gate off: release 0 walks the counter down
        TickUntilEnvelope(sid, voice: 2, target: 0xAA, maxCycles: 6000);
        Assert.Equal(-588, sid.CycleFilterOutput);           // (0x000 - 0x380) * dac[0xAA]=168 >> 8
        Assert.Equal(-588, sid.CycleExternalFilterOutput);
    }

    /// <summary>
    /// FR: FR-SID-CLOCK AC-09 (DIVERGENT, finding 13), TR: TR-SID-ORACLE-001,
    /// TEST: TEST-SID-CLOCK-09.
    /// Use case: reSID dispatches grouped passes: all three envelopes, then
    /// all three oscillators, then all three synchronizations (sid.h:205-218);
    /// the managed Tick interleaved the envelope clock into the per-voice
    /// oscillator loop. FR-SID-CLOCK AC-11 (FAITHFUL) proves the orders are
    /// behaviourally equivalent within a cycle, so this criterion's substance
    /// is the dispatch structure itself; it is asserted at source level, the
    /// same way the harness already asserts source conventions.
    /// Acceptance: Sid6581.Tick() invokes the grouped stage methods
    /// ClockEnvelopes, ClockOscillators, SynchronizeOscillators in exactly
    /// that order; the ClockEnvelopes body clocks envelopes without touching
    /// any accumulator, and the ClockOscillators body advances accumulators
    /// without clocking any envelope (no interleaving).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-CLOCK-09", ParityTag.Divergent, pending: false)]
    public void TickDispatch_RunsGroupedEnvelopeOscillatorSyncPasses()
    {
        var source = File.ReadAllText(ResolveSid6581SourcePath());

        var tickBody = ExtractMethodBody(source, "public void Tick()");
        var envelopeCall = tickBody.IndexOf("ClockEnvelopes();", StringComparison.Ordinal);
        var oscillatorCall = tickBody.IndexOf("ClockOscillators();", StringComparison.Ordinal);
        var synchronizeCall = tickBody.IndexOf("SynchronizeOscillators();", StringComparison.Ordinal);

        Assert.True(envelopeCall >= 0, "Tick() has no grouped envelope pass (ClockEnvelopes)");
        Assert.True(oscillatorCall >= 0, "Tick() has no grouped oscillator pass (ClockOscillators)");
        Assert.True(synchronizeCall >= 0, "Tick() has no grouped synchronize pass (SynchronizeOscillators)");
        Assert.True(envelopeCall < oscillatorCall, "envelope pass must precede the oscillator pass (sid.h:205-212)");
        Assert.True(oscillatorCall < synchronizeCall, "oscillator pass must precede the synchronize pass (sid.h:210-218)");

        var envelopePass = ExtractMethodBody(source, "private void ClockEnvelopes()");
        Assert.Contains("ProcessEnvelope(", envelopePass, StringComparison.Ordinal);
        Assert.DoesNotContain("WaveformAccumulator", envelopePass, StringComparison.Ordinal);

        var oscillatorPass = ExtractMethodBody(source, "private void ClockOscillators()");
        Assert.Contains("WaveformAccumulator =", oscillatorPass, StringComparison.Ordinal); // masked assignment: = (acc + freq) & 0xFFFFFF
        Assert.DoesNotContain("ProcessEnvelope(", oscillatorPass, StringComparison.Ordinal);
        Assert.DoesNotContain("Env.Clock(", oscillatorPass, StringComparison.Ordinal);
    }

    /// <summary>Resolves src/ViceSharp.Chips/Audio/Sid6581.cs from the recorded test-source directory.</summary>
    private static string ResolveSid6581SourcePath()
    {
        var metadata = typeof(SidEnvDivergentParityTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "TestHarnessSourceDirectory");
        Assert.False(string.IsNullOrWhiteSpace(metadata?.Value), "TestHarnessSourceDirectory assembly metadata missing");

        var path = Path.GetFullPath(Path.Combine(
            metadata!.Value!, "..", "..", "src", "ViceSharp.Chips", "Audio", "Sid6581.cs"));
        Assert.True(File.Exists(path), $"managed SID source not found: {path}");
        return path;
    }

    /// <summary>
    /// Extracts the brace-balanced body of the method whose signature line
    /// contains <paramref name="signature"/>. Fails the test with a clear
    /// message when the method does not exist (the red-phase shape).
    /// </summary>
    private static string ExtractMethodBody(string source, string signature)
    {
        var at = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"method '{signature}' not found in Sid6581.cs");

        var open = source.IndexOf('{', at);
        Assert.True(open >= 0, $"method '{signature}' has no body");

        var depth = 0;
        for (var i = open; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}' && --depth == 0)
            {
                return source.Substring(open, i - open + 1);
            }
        }

        Assert.Fail($"method '{signature}' body is not brace-balanced");
        return string.Empty;
    }
}
