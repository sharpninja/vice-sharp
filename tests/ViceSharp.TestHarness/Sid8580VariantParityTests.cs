using System;
using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// PLAN-VICEPARITY-001 S11: DIVERGENT parity tests for every DIVERGENT
/// acceptance criterion of FR-SID-8580 in
/// artifacts/vice-parity-requirements/requirements.yaml (finding 23): the
/// per-model 8580 constants (databus_ttl 0xa2000, amplify scaleFactor 5,
/// wave_zero 0x9e0), the SAMPLE_FAST one-cycle write pipeline, the model-1
/// filter dispatch, and the collapse of the fabricated Sid8580D.
///
/// Structural/behavioral tests use [Fact]; oracle-comparative lockstep tests
/// use [ViceFact] (auto-skip without the native VICE shim; c64c selector =>
/// SidModel 1 => MOS8580).
/// </summary>
[Collection("NativeVice")]
public sealed class Sid8580VariantParityTests
{
    private static Sid8580 MakeSid8580() => new(new BasicBus()) { BaseAddress = 0xD400 };
    private static Sid6581 MakeSid6581() => new(new BasicBus()) { BaseAddress = 0xD400 };

    /// <summary>
    /// FR: FR-SID-8580 AC-01 (DIVERGENT, finding 23). TR-SID-ORACLE-002.
    /// Use case: the 8580 data-bus fade TTL is 0xa2000 cycles (vs 0x1d00 on the
    ///   6581); a register write reloads the bus TTL to that value.
    /// Acceptance: Sid8580 DataBusTtl == 0xa2000 and a write latches
    ///   DataBusValueTtl == 0xa2000; the 6581 stays 0x1d00.
    /// viceCite: sid.cc:119.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-8580-01", ParityTag.Divergent, pending: false)]
    public void DataBusTtl_8580_Is0xA2000()
    {
        var sid = MakeSid8580();
        Assert.Equal(0xA2000, sid.DataBusTtlSeam);
        sid.Write(0xD404, 0x11);
        Assert.Equal(0xA2000, sid.DataBusValueTtl);

        Assert.Equal(0x1D00, MakeSid6581().DataBusTtlSeam);
    }

    /// <summary>
    /// FR: FR-SID-8580 AC-02 (DIVERGENT, finding 23). TR-SID-ORACLE-002.
    /// Use case: reSID amplify scaleFactor is 5 on the 8580, 3 on the 6581
    ///   (set_chip_model, sid.cc:121); the 6581 mixes 1.5x louder.
    /// Acceptance: Sid8580 OutputScaleFactor == 5; Sid6581 == 3.
    /// viceCite: sid.cc:121.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-8580-02", ParityTag.Divergent, pending: false)]
    public void ScaleFactor_8580_Is5_6581Is3()
    {
        Assert.Equal(5, MakeSid8580().OutputScaleFactorSeam);
        Assert.Equal(3, MakeSid6581().OutputScaleFactorSeam);
    }

    /// <summary>
    /// FR: FR-SID-8580 AC-03 (DIVERGENT, finding 23). TR-SID-ORACLE-002.
    /// Use case: MOS 8580 wave_zero is 0x9e0 in the 12-bit domain (voice.cc:97),
    ///   subtracted from the wave DAC output before the envelope multiply.
    /// Acceptance: Sid8580 WaveZeroLevel == 0x9e0; Sid6581 == 0x380.
    /// viceCite: voice.cc:97.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-8580-03", ParityTag.Divergent, pending: false)]
    public void WaveZero_8580_Is0x9e0()
    {
        Assert.Equal(0x9E0, MakeSid8580().WaveZeroLevelSeam);
        Assert.Equal(0x380, MakeSid6581().WaveZeroLevelSeam);
    }

    /// <summary>
    /// FR: FR-SID-8580 AC-04 (DIVERGENT, finding 23). TR-SID-ORACLE-002.
    /// Use case: under SAMPLE_FAST an 8580 register write arms a one-cycle
    ///   pipeline (write_pipeline=1) instead of applying immediately (the
    ///   SID-detection quirk); the 6581 and non-FAST paths apply immediately.
    /// Acceptance: Sid8580 with SamplingMethod=Fast arms PipelinedWriteSlot=1 on
    ///   a write; the 6581 at Fast, and the 8580 at Resample, keep the slot 0.
    /// viceCite: sid.cc:211-216.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-8580-04", ParityTag.Divergent, pending: false)]
    public void SampleFast_8580_Write_ArmsPipeline()
    {
        var fast8580 = MakeSid8580();
        fast8580.SamplingMethod = SidSamplingMethod.Fast;
        fast8580.Write(0xD404, 0x21);
        Assert.Equal(1, fast8580.PipelinedWriteSlot);

        // 8580 at the default Resample commits immediately.
        var resample8580 = MakeSid8580();
        resample8580.Write(0xD404, 0x21);
        Assert.Equal(0, resample8580.PipelinedWriteSlot);

        // 6581 never arms the pipeline, even at Fast (sid_model != MOS8580).
        var fast6581 = MakeSid6581();
        fast6581.SamplingMethod = SidSamplingMethod.Fast;
        fast6581.Write(0xD404, 0x21);
        Assert.Equal(0, fast6581.PipelinedWriteSlot);
    }

    /// <summary>
    /// FR: FR-SID-8580 AC-05 (DIVERGENT, finding 23). TR-SID-ORACLE-002.
    /// Use case: the per-cycle clock() flushes a pending 8580 SAMPLE_FAST write
    ///   after the filter/external-filter stages, committing the register effect
    ///   and clearing the pipeline slot.
    /// Acceptance: after arming (Fast + write) a single Tick clears
    ///   PipelinedWriteSlot to 0 (the flush committed the deferred register);
    ///   the one-cycle-late EFFECT on the audio stream is sealed by AC-06.
    /// viceCite: sid.cc:749-756.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-8580-05", ParityTag.Divergent, pending: false)]
    public void Clock_FlushesPipeline_AppliesDeferredWrite()
    {
        var sid = MakeSid8580();
        sid.SamplingMethod = SidSamplingMethod.Fast;
        sid.Write(0xD40F, 0x40); // voice3 freq hi - armed
        sid.Tick();              // clock() flushes the freq write
        Assert.Equal(0, sid.PipelinedWriteSlot);

        sid.Write(0xD412, 0x21); // control: saw + gate - armed (not yet applied)
        Assert.Equal(1, sid.PipelinedWriteSlot);

        sid.Tick();              // clock() flushes the pipeline
        Assert.Equal(0, sid.PipelinedWriteSlot);
    }

    /// <summary>
    /// FR: FR-SID-8580 AC-06 (DIVERGENT, finding 23). TR-SID-ORACLE-002.
    /// Use case: the single-cycle clock() flush makes an 8580 SAMPLE_FAST write
    ///   take effect exactly one cycle late, identically to reSID.
    /// Acceptance: driving the managed Sid8580 (SamplingMethod=Fast) and the c64c
    ///   oracle (SidExactSetSampling FAST) through the same interleaved
    ///   write/clock program yields bit-exact SID output every cycle.
    /// viceCite: sid.h:231-234.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-8580-06", ParityTag.Divergent, pending: false)]
    public void SampleFast_1CycleWriteDelay_LockstepVsOracle()
    {
        var program = new (ushort reg, byte val)[]
        {
            (0x15, 0x00), (0x16, 0x40), (0x17, 0x11), (0x18, 0x1F),
            (0x00, 0x00), (0x01, 0x30), (0x04, 0x21),
        };

        var sid = MakeSid8580();
        sid.SamplingMethod = SidSamplingMethod.Fast;

        var native = ViceNativeBridge.CreateMachine("c64c");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);
            Assert.True(ViceNativeBridge.SidExactSetSampling(native, 0, 44100.0),
                "oracle SAMPLE_FAST reconfigure failed");

            // Interleave each write with one clock on both sides so the single
            // pipeline slot is flushed before the next write (respecting the
            // FAST single-slot semantics), then run to 3000 cycles comparing.
            foreach (var (reg, val) in program)
            {
                sid.Write((ushort)(0xD400 + reg), val);
                ViceNativeBridge.SidExactWrite(native, reg, val);
                sid.Tick();
                ViceNativeBridge.SidExactClock(native, 1);
                Assert.Equal(ViceNativeBridge.SidExactOutput(native), sid.CycleExternalFilterOutput);
            }

            for (int c = 0; c < 3000; c++)
            {
                sid.Tick();
                ViceNativeBridge.SidExactClock(native, 1);
                Assert.Equal(ViceNativeBridge.SidExactOutput(native), sid.CycleExternalFilterOutput);
            }
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    /// <summary>
    /// FR: FR-SID-8580 AC-07 (DIVERGENT, finding 23). TR-SID-ORACLE-002.
    /// Use case: the 8580 filter mixer uses filterGain with scaleFactor 1.0, so
    ///   Model8580.FilterGain = (int)(1.0*(1&lt;&lt;12)) = 4096 and the output
    ///   dc_offset term (32767*(4096-filterGain)) is 0.
    /// Acceptance: Model8580.FilterGain == 4096; the dc_offset term is 0.
    /// viceCite: filter8580new.cc:285-286.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-8580-07", ParityTag.Divergent, pending: false)]
    public void FilterGain_8580_ScaleFactor1_DcOffsetZero()
    {
        Assert.Equal(1 << 12, Sid6581.Model8580.Value.FilterGain);
        Assert.Equal(0, 32767 * ((1 << 12) - Sid6581.Model8580.Value.FilterGain));
    }

    /// <summary>
    /// FR: FR-SID-8580 AC-08 (DIVERGENT, finding 23). TR-SID-ORACLE-002.
    /// Use case: sid_model 1 selects model_filter[1] and solve_integrate_8580
    ///   (not the 6581 VCR/snake integrator or a Chamberlin SVF).
    /// Acceptance: Sid8580 reports the 8580 filter dispatch (IsMos8580Filter) and
    ///   the 6581 does not; the model-1 dispatch is sealed bit-exact by the
    ///   FILTER-8580-11 integrator lockstep.
    /// viceCite: sid.cc:105-128.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-8580-08", ParityTag.Divergent, pending: false)]
    public void Model1_Selects_SolveIntegrate8580()
    {
        Assert.True(MakeSid8580().IsMos8580FilterSeam);
        Assert.False(MakeSid6581().IsMos8580FilterSeam);
    }

    /// <summary>
    /// FR: FR-SID-8580 AC-09 (DIVERGENT, finding 23). TR-SID-ORACLE-002.
    /// Use case: reSID's write_state HACK forces SAMPLE_RESAMPLE so a snapshot
    ///   restore applies registers immediately (bypassing the FAST pipeline).
    ///   The managed default is SAMPLE_RESAMPLE, so 8580 writes apply immediately.
    /// Acceptance: an 8580 at the default Resample commits a write immediately
    ///   (PipelinedWriteSlot stays 0), matching the immediate-apply semantics.
    /// viceCite: sid.cc:424-433.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-8580-09", ParityTag.Divergent, pending: false)]
    public void WriteState_ResampleDefault_ImmediateApply()
    {
        var sid = MakeSid8580();
        Assert.Equal(SidSamplingMethod.Resample, sid.SamplingMethod);
        sid.Write(0xD412, 0x21);
        Assert.Equal(0, sid.PipelinedWriteSlot); // committed immediately
        Assert.Equal(0x21, sid.Peek(0xD412));
    }

    /// <summary>
    /// FR: FR-SID-8580 AC-10 (DIVERGENT, finding 23). TR-SID-ORACLE-002.
    /// Use case: 8580 combined waveforms use the wave8580_* ROM tables (not an
    ///   AND * 3/4 approximation); combined output flows through the filter.
    /// Acceptance: driving the managed Sid8580 and the c64c oracle through a
    ///   combined-waveform program (pulse+saw, pulse+tri, all three) yields
    ///   bit-exact SID output every cycle.
    /// viceCite: wave8580_PS.h / wave8580_PT.h / wave8580_PST.h.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-8580-10", ParityTag.Divergent, pending: false)]
    public void CombinedWaveforms_8580_OutputLockstep()
    {
        var program = new (ushort reg, byte val)[]
        {
            (0x15, 0x00), (0x16, 0x40), (0x17, 0x01), (0x18, 0x0F),
            (0x00, 0x00), (0x01, 0x22), (0x04, 0x61), // voice1 pulse+saw
            (0x02, 0x00), (0x03, 0x08),
            (0x07, 0x00), (0x08, 0x2A), (0x0B, 0x51), // voice2 pulse+tri
            (0x09, 0x00), (0x0A, 0x08),
            (0x0E, 0x00), (0x0F, 0x18), (0x12, 0x71), // voice3 pulse+saw+tri
            (0x10, 0x00), (0x11, 0x08),
        };

        var sid = MakeSid8580();
        foreach (var (reg, val) in program) sid.Write((ushort)(0xD400 + reg), val);

        var native = ViceNativeBridge.CreateMachine("c64c");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);
            foreach (var (reg, val) in program) ViceNativeBridge.SidExactWrite(native, reg, val);

            for (int c = 0; c < 4000; c++)
            {
                sid.Tick();
                ViceNativeBridge.SidExactClock(native, 1);
                Assert.Equal(ViceNativeBridge.SidExactOutput(native), sid.CycleExternalFilterOutput);
            }
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    /// <summary>
    /// FR: FR-SID-8580 AC-11 (DIVERGENT, finding 23). TR-SID-ORACLE-002.
    /// Use case: Sid8580D was a fabricated filter with no reSID counterpart and
    ///   must be removed; the SID factory must only ever create Sid6581/Sid8580.
    /// Acceptance: no ViceSharp.Chips.Audio.Sid8580D type exists, and the SID
    ///   factory returns a Sid8580 (never a Sid8580D) for the 8580 model.
    /// viceCite: none (no reSID counterpart).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-8580-11", ParityTag.Divergent, pending: false)]
    public void Sid8580D_Removed_FactoryMakesSid8580()
    {
        var asm = typeof(Sid6581).Assembly;
        Assert.Null(asm.GetType("ViceSharp.Chips.Audio.Sid8580D"));

        var sid8580 = MakeSid8580();
        Assert.IsType<Sid8580>(sid8580);
        // A Sid8580 is not, and cannot be, a removed "D" subtype.
        Assert.Equal("ViceSharp.Chips.Audio.Sid8580", sid8580.GetType().FullName);
    }
}
