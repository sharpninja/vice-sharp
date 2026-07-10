using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// PLAN-VICEPARITY-001 (batched clock slice): DIVERGENT parity tests for the
/// external output filter (FR-SID-EXTFILT AC-01..07) and the batched-clock
/// per-cycle acceptance criteria of FR-SID-CLOCK (AC-05 oscillator MSB-toggle
/// sub-stepping, AC-08 write-pipeline prologue). The dt-sub-stepping ACs are
/// sealed by SAMPLE_FAST buffered lockstep vs the native reSID oracle, which is
/// only bit-exact if the managed batched engine (Sid6581.BatchedClock.cs)
/// reproduces reSID's SID::clock(delta_t) exactly. The remaining ACs are
/// structural locks on the (already reSID-faithful) external-filter recurrence.
/// </summary>
[Collection("NativeVice")]
public sealed class SidBatchedClockParityTests
{
    private static Sid6581 MakeSid6581() => new(new BasicBus()) { BaseAddress = 0xD400 };
    private static Sid8580 MakeSid8580() => new(new BasicBus()) { BaseAddress = 0xD400 };

    private static readonly (ushort reg, byte val)[] SawLp =
    {
        (0x15, 0x00), (0x16, 0x40), (0x17, 0x51), (0x18, 0x1F),
        (0x00, 0x00), (0x01, 0x40), (0x05, 0x00), (0x06, 0xF0), (0x04, 0x21),
    };

    // -----------------------------------------------------------------------
    // FR-SID-EXTFILT
    // -----------------------------------------------------------------------

    /// <summary>
    /// FR: FR-SID-EXTFILT AC-01 (DIVERGENT, finding 15). TR-SID-AMPLIFY-001.
    /// Use case: the external filter's fixed-point recurrence coefficients are
    ///   w0lp_1_s7 = 12 and w0hp_1_s17 = 13 (extfilt.cc:41-42).
    /// Acceptance: the managed coefficients equal 12 and 13.
    /// viceCite: extfilt.cc:41-42.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-EXTFILT-01", ParityTag.Divergent, pending: false)]
    public void ExtFilterCoefficients_12And13()
    {
        Assert.Equal(12, Sid6581.ExtFiltW0lp1s7);
        Assert.Equal(13, Sid6581.ExtFiltW0hp1s17);
    }

    /// <summary>
    /// FR: FR-SID-EXTFILT AC-02 (DIVERGENT, finding 15). TR-SID-AMPLIFY-001.
    /// Use case: reset() zeroes the external-filter integrators (extfilt.cc:58-63).
    /// Acceptance: after ticks that drive the ext filter, a Reset() zeroes both
    ///   Vlp and Vhp.
    /// viceCite: extfilt.cc:58-63.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-EXTFILT-02", ParityTag.Divergent, pending: false)]
    public void ExtFilterReset_ZeroesVlpVhp()
    {
        var sid = MakeSid6581();
        foreach (var (reg, val) in SawLp) sid.Write((ushort)(0xD400 + reg), val);
        for (int c = 0; c < 500; c++) sid.Tick();
        Assert.True(sid.ExtFiltVlpState != 0 || sid.ExtFiltVhpState != 0);
        sid.Reset();
        Assert.Equal(0, sid.ExtFiltVlpState);
        Assert.Equal(0, sid.ExtFiltVhpState);
    }

    /// <summary>
    /// FR: FR-SID-EXTFILT AC-03 (DIVERGENT, finding 15). TR-SID-AMPLIFY-001.
    /// Use case: the single-cycle recurrence dVlp = w0lp_1_s7*((Vi&lt;&lt;11)-Vlp)&gt;&gt;7,
    ///   dVhp = w0hp_1_s17*(Vlp-Vhp)&gt;&gt;17 (extfilt.h:96-116) engages both integrators.
    /// Acceptance: after per-cycle ticks with a driven filter output, both Vlp
    ///   (w0lp=12 low-pass) and Vhp (w0hp=13 high-pass) become non-zero.
    /// viceCite: extfilt.h:96-116.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-EXTFILT-03", ParityTag.Divergent, pending: false)]
    public void ExtFilterClock1_RecurrenceEngagesBothIntegrators()
    {
        var sid = MakeSid6581();
        Assert.True(sid.ExternalFilterEnabled);
        foreach (var (reg, val) in SawLp) sid.Write((ushort)(0xD400 + reg), val);
        for (int c = 0; c < 500; c++) sid.Tick();
        Assert.NotEqual(0, sid.ExtFiltVlpState);
        Assert.NotEqual(0, sid.ExtFiltVhpState);
    }

    /// <summary>
    /// FR: FR-SID-EXTFILT AC-04 (DIVERGENT, finding 15). TR-SID-ORACLE-002.
    /// Use case: ExternalFilter::clock(delta_t) sub-steps at delta_t_flt=8 with
    ///   the shifted coefficients over the SAMPLE_FAST window (extfilt.h:121-153);
    ///   the batched clock engine implements it (ClockResidExtFilterBatched).
    /// Acceptance: the managed Sid6581 final output matches the c64 oracle
    ///   bit-exact at SAMPLE_FAST for &gt;= 8000 samples - only possible if the
    ///   external filter sub-steps at dt=8 exactly like reSID.
    /// viceCite: extfilt.h:121-153.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-EXTFILT-04", ParityTag.Divergent, pending: false)]
    public void ExtFilterClockDeltaT_DeltaTFlt8_BatchedLockstep()
        => AssertFastBatchedLockstep("c64", MakeSid6581, SawLp, 8000);

    /// <summary>
    /// FR: FR-SID-EXTFILT AC-05 (DIVERGENT, finding 15). TR-SID-AMPLIFY-001.
    /// Use case: a disabled external filter passes through (Vlp=Vi&lt;&lt;11, Vhp=0,
    ///   extfilt.h:100-105); VICE always enables it, so this is a managed toggle.
    /// Acceptance: with EnableExternalFilter(false), after a tick Vhp==0 and
    ///   Vlp == output &lt;&lt; 11.
    /// viceCite: extfilt.h:100-105.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-EXTFILT-05", ParityTag.Divergent, pending: false)]
    public void ExtFilterDisabled_PassesThrough()
    {
        var sid = MakeSid6581();
        sid.EnableExternalFilter(false);
        foreach (var (reg, val) in SawLp) sid.Write((ushort)(0xD400 + reg), val);
        for (int c = 0; c < 200; c++) sid.Tick();
        Assert.Equal(0, sid.ExtFiltVhpState);
        Assert.Equal(sid.CycleExternalFilterOutput << 11, sid.ExtFiltVlpState);
    }

    /// <summary>
    /// FR: FR-SID-EXTFILT AC-06 (DIVERGENT, finding 15). TR-SID-AMPLIFY-001.
    /// Use case: ExternalFilter::output() = (Vlp - Vhp) &gt;&gt; 11 (extfilt.h:159-163).
    /// Acceptance: the committed external-filter output equals
    ///   (ExtFiltVlpState - ExtFiltVhpState) &gt;&gt; 11 every cycle.
    /// viceCite: extfilt.h:159-163.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-EXTFILT-06", ParityTag.Divergent, pending: false)]
    public void ExtFilterOutput_IsVlpMinusVhpShift11()
    {
        var sid = MakeSid6581();
        foreach (var (reg, val) in SawLp) sid.Write((ushort)(0xD400 + reg), val);
        for (int c = 0; c < 500; c++)
        {
            sid.Tick();
            Assert.Equal((sid.ExtFiltVlpState - sid.ExtFiltVhpState) >> 11, sid.CycleExternalFilterOutput);
        }
    }

    /// <summary>
    /// FR: FR-SID-EXTFILT AC-07 (DIVERGENT, finding 15). TR-SID-ORACLE-002.
    /// Use case: the per-cycle chain clocks the external filter every phi2 cycle
    ///   from the filter output and the emitted sample is the external-filter
    ///   output (sid.cc:828-831).
    /// Acceptance: the managed single-cycle external-filter output matches the c64
    ///   oracle SID output bit-exact every cycle across a filter program (proving
    ///   the per-cycle extfilt.clock(filter.output()) wiring).
    /// viceCite: sid.cc:828-831.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-EXTFILT-07", ParityTag.Divergent, pending: false)]
    public void ExtFilterWiring_PerCycleOutputLockstep()
    {
        var sid = MakeSid6581();
        foreach (var (reg, val) in SawLp) sid.Write((ushort)(0xD400 + reg), val);
        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);
            foreach (var (reg, val) in SawLp) ViceNativeBridge.SidExactWrite(native, reg, val);
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

    // -----------------------------------------------------------------------
    // FR-SID-CLOCK (batched)
    // -----------------------------------------------------------------------

    /// <summary>
    /// FR: FR-SID-CLOCK AC-05 (DIVERGENT, finding 13). TR-SID-ORACLE-002.
    /// Use case: the batched clock sub-steps the oscillators to the nearest
    ///   sync-source MSB toggle so hard sync operates correctly (sid.cc:776-820);
    ///   the batched engine implements it.
    /// Acceptance: the managed Sid6581 output matches the c64 oracle bit-exact at
    ///   SAMPLE_FAST across a hard-sync + ring-mod program - only possible if the
    ///   oscillators sub-step to the MSB toggles exactly like reSID.
    /// viceCite: sid.cc:776-820.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-CLOCK-05", ParityTag.Divergent, pending: false)]
    public void OscMsbToggleSubStepping_BatchedLockstep()
        => AssertFastBatchedLockstep("c64", MakeSid6581, new (ushort, byte)[]
        {
            (0x15, 0x00), (0x16, 0x40), (0x17, 0x11), (0x18, 0x1F),
            (0x00, 0x00), (0x01, 0x11), (0x05, 0x00), (0x06, 0xF0), (0x04, 0x21), // v1 saw sync src
            (0x07, 0x00), (0x08, 0x1F), (0x0C, 0x00), (0x0D, 0xF0), (0x0B, 0x13), // v2 tri+sync
        }, 8000);

    /// <summary>
    /// FR: FR-SID-CLOCK AC-08 (DIVERGENT, finding 23). TR-SID-ORACLE-002.
    /// Use case: the batched clock's write-pipeline prologue commits an armed
    ///   8580 SAMPLE_FAST write by stepping one cycle then flushing before
    ///   consuming the rest of the window (sid.cc:749-756).
    /// Acceptance: driving the managed Sid8580 (SAMPLE_FAST) and the c64c oracle
    ///   through an interleaved write/clock program - so a write arms the pipeline
    ///   mid-stream and the next batched clock flushes it via the prologue -
    ///   yields bit-exact output.
    /// viceCite: sid.cc:749-756.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-CLOCK-08", ParityTag.Divergent, pending: false)]
    public void WritePipelinePrologue_BatchedLockstep()
    {
        var program = new (ushort reg, byte val)[]
        {
            (0x15, 0x00), (0x16, 0x40), (0x17, 0x11), (0x18, 0x1F),
            (0x00, 0x00), (0x01, 0x30), (0x04, 0x21),
        };
        var sid = MakeSid8580();
        Assert.True(sid.SetSamplingParameters(985248.0, SidSamplingMethod.Fast, 44100.0));
        var native = ViceNativeBridge.CreateMachine("c64c");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);
            Assert.True(ViceNativeBridge.SidExactSetSampling(native, 0, 44100.0), "oracle SAMPLE_FAST failed");

            var mbuf = new short[512];
            var obuf = new short[512];
            void DrainAndCompare()
            {
                int mc = 4096, oc = 4096;
                int mGot = sid.ClockBuffered(ref mc, mbuf);
                int oGot = ViceNativeBridge.SidExactClockBuffered(native, ref oc, obuf);
                Assert.Equal(oGot, mGot);
                for (int i = 0; i < mGot; i++)
                    Assert.Equal(obuf[i], mbuf[i]);
            }

            // Interleave register writes with buffered drains: at SAMPLE_FAST each
            // 8580 write arms the pipeline, flushed by the next batched clock's
            // prologue.
            foreach (var (reg, val) in program)
            {
                sid.Write((ushort)(0xD400 + reg), val);
                ViceNativeBridge.SidExactWrite(native, reg, val);
                DrainAndCompare();
            }
            for (int c = 0; c < 40; c++) DrainAndCompare();
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    // Shared SAMPLE_FAST batched lockstep helper.
    private static void AssertFastBatchedLockstep(
        string oracleMachine, System.Func<Sid6581> makeSid,
        (ushort reg, byte val)[] program, int minSamples)
    {
        var sid = makeSid();
        Assert.True(sid.SetSamplingParameters(985248.0, SidSamplingMethod.Fast, 44100.0));
        foreach (var (reg, val) in program) sid.Write((ushort)(0xD400 + reg), val);

        var native = ViceNativeBridge.CreateMachine(oracleMachine);
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);
            Assert.True(ViceNativeBridge.SidExactSetSampling(native, 0, 44100.0), "oracle SAMPLE_FAST failed");
            foreach (var (reg, val) in program) ViceNativeBridge.SidExactWrite(native, reg, val);

            var mbuf = new short[512];
            var obuf = new short[512];
            int produced = 0;
            while (produced < minSamples)
            {
                int mc = 4096, oc = 4096;
                int mGot = sid.ClockBuffered(ref mc, mbuf);
                int oGot = ViceNativeBridge.SidExactClockBuffered(native, ref oc, obuf);
                Assert.Equal(oGot, mGot);
                Assert.Equal(oc, mc);
                for (int i = 0; i < mGot; i++)
                {
                    if (mbuf[i] != obuf[i])
                        Assert.Fail($"FAST sample {produced + i}: managed {mbuf[i]} != oracle {obuf[i]}");
                }
                produced += mGot;
            }
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }
}
