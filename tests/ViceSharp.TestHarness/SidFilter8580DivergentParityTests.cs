using System;
using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// PLAN-VICEPARITY-001 S11: DIVERGENT parity tests for every DIVERGENT
/// acceptance criterion of FR-SID-FILTER-8580 in
/// artifacts/vice-parity-requirements/requirements.yaml (finding 17).
///
/// This slice replaces the fabricated Chamberlin/near-linear 8580 filter in
/// Sid8580 with a field-for-field port of reSID's MOS8580 op-amp/DAC model
/// (filter8580new.cc:203-224 + :524-621 for m==1, filter8580new.h:1913-1937).
/// The bit-exact lockstep ACs (10/11/12) drive the managed Sid8580 and the
/// reSID single-cycle oracle (c64c selector => SidModel 1 => MOS8580) from
/// reset through the same register program and compare every phi2 cycle. The
/// structural ACs assert the built model tables (opamp_rev/summer/mixer/gain/
/// resonance/f0_dac) and derived scalars, sealed downstream by the lockstep.
///
/// Structural tests use [Fact]; oracle-comparative tests use [ViceFact]
/// (auto-skip without the native VICE shim).
/// </summary>
[Collection("NativeVice")]
public sealed class SidFilter8580DivergentParityTests
{
    // Convenience accessor for the 8580 static model tables.
    private static Sid6581.ResidFilterModel M8580 => Sid6581.Model8580.Value;
    private static Sid6581.ResidFilterModel M6581 => Sid6581.Model6581;

    private static Sid8580 MakeSid8580()
        => new(new BasicBus()) { BaseAddress = 0xD400 };

    // -----------------------------------------------------------------------
    // FR-SID-FILTER-8580 (DIVERGENT ACs)
    // -----------------------------------------------------------------------

    /// <summary>
    /// FR: FR-SID-FILTER-8580 AC-01 (DIVERGENT, finding 17). TR-SID-ORACLE-002.
    /// Use case: opamp_voltage_8580 22-point transfer curve defines the 8580
    /// op-amp model consumed throughout the 8580 filter table build.
    /// Acceptance: the 8580 model build yields a valid spline range: AK in
    ///   [1, 65534], BK > AK; and (distinct from the 6581) vmin=1.30 so the
    ///   curve is a different op-amp than Model6581 (AK values differ).
    /// viceCite: filter8580new.cc:80-104.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-FILTER-8580-01", ParityTag.Divergent, pending: false)]
    public void OpampVoltage8580_22Points_ValidSplineRange()
    {
        Assert.InRange(M8580.AK, 1, 65534);
        Assert.True(M8580.BK > M8580.AK);
        Assert.InRange(M8580.BK, M8580.AK + 1, 65535);
        // Distinct op-amp curve from the 6581 (different vmin/denorm).
        Assert.NotEqual(M6581.AK, M8580.AK);
    }

    /// <summary>
    /// FR: FR-SID-FILTER-8580 AC-02 (DIVERGENT, finding 17). TR-SID-ORACLE-002.
    /// Use case: model_filter_init[1] scalars (range 0.24, DC 4.7975, C 22e-9,
    ///   Vdd 9.09, Vth 0.80, uCox 100e-6) determine every derived 8580 constant.
    /// Acceptance: voiceScaleS14 and voiceDC and kVddt match the closed-form
    ///   derivation from those scalars: voiceScaleS14=(int)(N14*0.24)=516,
    ///   voiceDC=(int)(N16*(4.7975-1.30))=30119, kVddt=(int)(N16*(9.09-0.80-1.30)+0.5).
    /// viceCite: filter8580new.cc:203-224,291-297.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-FILTER-8580-02", ParityTag.Divergent, pending: false)]
    public void ModelFilterInit1_Scalars_VoiceScaleDcKVddt()
    {
        const double vmin = 1.30;
        const double vmax = 9.09 - 0.80;                 // max(8.91, k*(Vdd-Vth)=8.29) = 8.91
        double denorm = Math.Max(8.91, vmax) - vmin;     // 8.91 - 1.30 = 7.61
        double norm = 1.0 / denorm;
        double n16 = norm * ((1u << 16) - 1);
        double n14 = norm * (1u << 14);

        Assert.Equal((int)(n14 * 0.24), M8580.VoiceScaleS14);          // 516
        Assert.Equal((int)(n16 * (4.7975 - vmin)), M8580.VoiceDC);     // 30119
        Assert.Equal((int)(n16 * (1.0 * (9.09 - 0.80) - vmin) + 0.5), M8580.KVddt); // 60196
    }

    /// <summary>
    /// FR: FR-SID-FILTER-8580 AC-03 (DIVERGENT, finding 17). TR-SID-ORACLE-002.
    /// Use case: 8580 filterGain = 1.0*(1&lt;&lt;12); mixer divider 5; gain divider 16
    ///   (vs 6581 0.93*4096, divider 6, divider 12).
    /// Acceptance: Model8580.FilterGain == 4096 (exactly 1&lt;&lt;12, so dc_offset=0);
    ///   and it differs from the 6581 gain (3809), evidencing the distinct build.
    /// viceCite: filter8580new.cc:285-286,400,425.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-FILTER-8580-03", ParityTag.Divergent, pending: false)]
    public void FilterGain8580_Is4096_DcOffsetZero()
    {
        Assert.Equal(1 << 12, M8580.FilterGain); // 4096
        Assert.Equal(3809, M6581.FilterGain);    // 6581 differs
        // dc_offset = 32767 * (4096 - filterGain) = 0 for the 8580.
        Assert.Equal(0, 32767 * ((1 << 12) - M8580.FilterGain));
    }

    /// <summary>
    /// FR: FR-SID-FILTER-8580 AC-04 (DIVERGENT, finding 17). TR-SID-ORACLE-002.
    /// Use case: resGain[16] parallel-resistor ratios drive the 8580 resonance
    ///   (replacing the managed q=1/(1+res*0.875/4) invention).
    /// Acceptance: the resonance table is built from the 16 resGain ratios: it
    ///   has 16 rows of 65536, all valid ushorts, and the rows differ across n8
    ///   (distinct resonance per register value, not a single curve).
    /// viceCite: filter8580new.cc:135-153,592-597.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-FILTER-8580-04", ParityTag.Divergent, pending: false)]
    public void ResGain16_Ratios_DriveResonanceTable()
    {
        Assert.Equal(16, M8580.Resonance.Length);
        for (int n8 = 0; n8 < 16; n8++)
            Assert.Equal(1 << 16, M8580.Resonance[n8].Length);
        // resGain is strictly decreasing across the 16 ratios (1.4/1.0 down to
        // (1.4|R3)/2.8), so distinct rows produce distinct mid-scale gains.
        Assert.NotEqual(M8580.Resonance[0][40000], M8580.Resonance[15][40000]);
    }

    /// <summary>
    /// FR: FR-SID-FILTER-8580 AC-05 (DIVERGENT, finding 17). TR-SID-ORACLE-002.
    /// Use case: resonance[16][65536] (8580) is built via solve_gain_d(resGain[n8])
    ///   over the shared op-amp, not the 6581 (~n8&amp;0xf)/8 divisor scheme.
    /// Acceptance: the resonance table has the full 16x65536 shape; the 16 rows
    ///   are built from the 16 distinct resGain ratios (so rows 0 and 15 differ
    ///   at mid scale), evidencing solve_gain_d(resGain[n8]) rather than a single
    ///   shared curve.
    /// viceCite: filter8580new.cc:592-597.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-FILTER-8580-05", ParityTag.Divergent, pending: false)]
    public void Resonance8580_BuiltViaSolveGainD_ResGain()
    {
        // 16 distinct resGain ratios produce 16 distinct rows.
        Assert.NotEqual(M8580.Resonance[0][45000], M8580.Resonance[15][45000]);
        // All rows are the full op-amp domain.
        Assert.All(M8580.Resonance, row => Assert.Equal(1 << 16, row.Length));
    }

    /// <summary>
    /// FR: FR-SID-FILTER-8580 AC-06 (DIVERGENT, finding 17). TR-SID-ORACLE-002.
    /// Use case: the shared opamp_rev/summer/mixer/gain tables are rebuilt for
    ///   m==1 with the 8580 params (not reused from the 6581).
    /// Acceptance: OpampRev (65536), Summer, Mixer and Gain (16x65536) are all
    ///   present and sized; and the 8580 opamp_rev differs from the 6581's
    ///   (distinct op-amp), proving an independent m==1 build.
    /// viceCite: filter8580new.cc:382-438.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-FILTER-8580-06", ParityTag.Divergent, pending: false)]
    public void SharedTables_BuiltForModel1_WithParams()
    {
        Assert.Equal(1 << 16, M8580.OpampRev.Length);
        Assert.Equal(16, M8580.Gain.Length);
        Assert.All(M8580.Gain, row => Assert.Equal(1 << 16, row.Length));
        Assert.NotEmpty(M8580.Summer);
        Assert.NotEmpty(M8580.Mixer);
        // Distinct op-amp reverse table vs the 6581 (mid-scale sample differs).
        Assert.NotEqual(M6581.OpampRev[40000], M8580.OpampRev[40000]);
    }

    /// <summary>
    /// FR: FR-SID-FILTER-8580 AC-07 (DIVERGENT, finding 17). TR-SID-ORACLE-002.
    /// Use case: n_param = (int)(tmp_n_param[1]*32+0.5), tmp_n_param[1] =
    ///   denorm*(1&lt;&lt;13)*((uCox/2)*1e-6/C) with the 8580 scalars.
    /// Acceptance: Model8580.NParam == the closed-form value (4534); 0 on the 6581.
    /// viceCite: filter8580new.cc:299,600.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-FILTER-8580-07", ParityTag.Divergent, pending: false)]
    public void NParam_ClosedForm_4534()
    {
        double denorm = 8.91 - 1.30;                  // 7.61
        double tmpNParam = denorm * (1 << 13) * ((100e-6 / 2.0) * 1e-6 / 22e-9);
        Assert.Equal((int)(tmpNParam * 32 + 0.5), M8580.NParam); // 4534
        Assert.Equal(0, M6581.NParam);                            // 6581 unused
    }

    /// <summary>
    /// FR: FR-SID-FILTER-8580 AC-08 (DIVERGENT, finding 17). TR-SID-ORACLE-002.
    /// Use case: nVgt = (int)(N16*(Vgt-vmin)+0.5), Vgt = Vref*1.6 - Vth,
    ///   Vref=4.7975, Vth=0.80, vmin=1.30 (the DAC gate default overdrive).
    /// Acceptance: Model8580.NVgtDefault == the closed-form value (48019); 0 on 6581.
    /// viceCite: filter8580new.cc:602-603.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-FILTER-8580-08", ParityTag.Divergent, pending: false)]
    public void NVgtDefault_ClosedForm_48019()
    {
        const double vmin = 1.30;
        double denorm = 8.91 - vmin;
        double n16 = (1.0 / denorm) * ((1u << 16) - 1);
        double vgt = (4.7975 * 1.6) - 0.80;
        Assert.Equal((int)(n16 * (vgt - vmin) + 0.5), M8580.NVgtDefault); // 48019
        Assert.Equal(0, M6581.NVgtDefault);                               // 6581 unused
    }

    /// <summary>
    /// FR: FR-SID-FILTER-8580 AC-09 (DIVERGENT, finding 17). TR-SID-ORACLE-002.
    /// Use case: the 8580 f0_dac is the parallel-NMOS W/L ladder with dacWL=806
    ///   (no N16/dac_zero/dac_scale normalization), f0_dac[0]=dacWL&gt;&gt;8.
    /// Acceptance: Model8580.F0Dac matches an independent in-test recomputation
    ///   of the dacWL=806 ladder for all 2048 entries.
    /// viceCite: filter8580new.cc:605-620.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-FILTER-8580-09", ParityTag.Divergent, pending: false)]
    public void F0Dac8580_DacWL806_Ladder_Exhaustive()
    {
        const uint dacWL = 806;
        Assert.Equal(1 << 11, M8580.F0Dac.Length);
        Assert.Equal((ushort)(dacWL >> 8), M8580.F0Dac[0]); // 3
        for (int n = 1; n < (1 << 11); n++)
        {
            uint wl = 0;
            for (int i = 0; i < 11; i++)
            {
                int bitmask = 1 << i;
                if ((n & bitmask) != 0)
                    wl += dacWL * (uint)(bitmask << 1);
            }
            Assert.Equal((ushort)(wl >> 8), M8580.F0Dac[n]);
        }
    }

    /// <summary>
    /// FR: FR-SID-FILTER-8580 AC-10 (DIVERGENT, finding 17). TR-SID-ORACLE-002.
    /// Use case: set_w0 8580 computes n_dac=(n_param*f0_dac[fc])&gt;&gt;11 and the
    ///   filter tracks the cutoff DAC (replacing managed 2*sin(pi*f/Fs)).
    /// Acceptance: the managed Sid8580 filter output matches the c64c oracle
    ///   filter probe[8] bit-exact every phi2 cycle across a cutoff-sweep + LP
    ///   program (4000 cycles).
    /// viceCite: filter8580new.cc:760-764.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-FILTER-8580-10", ParityTag.Divergent, pending: false)]
    public void SetW0_8580_CutoffSweep_FilterProbeLockstep()
    {
        AssertFilter8580LockstepVsOracle(
            new (ushort, byte)[]
            {
                (0x15, 0x00), (0x16, 0x40), (0x17, 0xF1), (0x18, 0x1F),
                (0x00, 0x00), (0x01, 0x20), (0x04, 0x21),
                (0x07, 0x00), (0x08, 0x28), (0x0B, 0x41),
                (0x0E, 0x00), (0x0F, 0x30), (0x12, 0x11),
            },
            cycles: 4000,
            compareFilterOutput: true);
    }

    /// <summary>
    /// FR: FR-SID-FILTER-8580 AC-11 (DIVERGENT, finding 17). TR-SID-ORACLE-002.
    /// Use case: solve_integrate_8580 (nVgt/n_dac DAC-gate integrator with the
    ///   triode/saturation Vgdt clamp) replaces the 6581 VCR/snake integrator.
    /// Acceptance: the managed Sid8580 filter output matches the c64c oracle
    ///   filter probe[8] bit-exact across a low-pass program (4000 cycles) whose
    ///   integrator trajectory exercises the DAC-gate math.
    /// viceCite: filter8580new.h:1913-1937.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-FILTER-8580-11", ParityTag.Divergent, pending: false)]
    public void SolveIntegrate8580_LpProgram_FilterProbeLockstep()
    {
        AssertFilter8580LockstepVsOracle(
            new (ushort, byte)[]
            {
                (0x15, 0x05), (0x16, 0x30), (0x17, 0x51), (0x18, 0x1F),
                (0x00, 0x00), (0x01, 0x40), (0x05, 0x00), (0x06, 0xF0), (0x04, 0x11),
            },
            cycles: 4000,
            compareFilterOutput: true);
    }

    /// <summary>
    /// FR: FR-SID-FILTER-8580 AC-12 (DIVERGENT, finding 17). TR-SID-ORACLE-002.
    /// Use case: the 8580 per-cycle clock order runs solve_integrate_8580 then
    ///   the summer/external filter; the final external-filter output must match.
    /// Acceptance: the managed Sid8580 external-filter output matches the c64c
    ///   oracle SID output bit-exact across a BP/HP-routed program (4000 cycles).
    /// viceCite: filter8580new.h:769-778.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-FILTER-8580-12", ParityTag.Divergent, pending: false)]
    public void PerCycleOrder_8580_ExtFilterOutputLockstep()
    {
        AssertFilter8580LockstepVsOracle(
            new (ushort, byte)[]
            {
                (0x15, 0x02), (0x16, 0x60), (0x17, 0x71), (0x18, 0x5F), // BP+HP mode
                (0x00, 0x00), (0x01, 0x30), (0x04, 0x41),               // pulse
                (0x07, 0x00), (0x08, 0x50), (0x0B, 0x21),               // saw
            },
            cycles: 4000,
            compareFilterOutput: false);
    }

    /// <summary>
    /// FR: FR-SID-FILTER-8580 AC-13 (DIVERGENT, finding 17). TR-SID-ORACLE-002.
    /// Use case: adjust_filter_bias(dac_bias) recomputes nVgt as
    ///   Vg=Vref*(dac_bias*6/100+1.6), Vgt=Vg-Vth, nVgt=(int)(vo_N16*(Vgt-vmin)+0.5).
    /// Acceptance: on a fresh Sid8580 (default bias 0) nVgt == NVgtDefault; a
    ///   +0.5V bias raises nVgt and a -0.5V bias lowers it, each matching the
    ///   closed-form value.
    /// viceCite: filter8580new.cc:654-669.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-FILTER-8580-13", ParityTag.Divergent, pending: false)]
    public void AdjustFilterBias8580_RecomputesNVgt()
    {
        var sid = MakeSid8580();
        Assert.Equal(M8580.NVgtDefault, sid.FilterNVgt); // default bias 0

        int Expected(double biasVolts)
        {
            double vg = 4.7975 * (biasVolts * 6.0 / 100.0 + 1.6);
            double vgt = vg - 0.80;
            return (int)(M8580.VoN16 * (vgt - 1.30) + 0.5);
        }

        sid.AdjustFilterBias8580(0.5);
        Assert.Equal(Expected(0.5), sid.FilterNVgt);
        Assert.True(sid.FilterNVgt > M8580.NVgtDefault);

        sid.AdjustFilterBias8580(-0.5);
        Assert.Equal(Expected(-0.5), sid.FilterNVgt);
        Assert.True(sid.FilterNVgt < M8580.NVgtDefault);

        sid.AdjustFilterBias8580(0.0);
        Assert.Equal(M8580.NVgtDefault, sid.FilterNVgt);
    }

    /// <summary>
    /// FR: FR-SID-FILTER-8580 AC-14 (DIVERGENT, finding 17). TR-SID-ORACLE-002.
    /// Use case: reSID has no separate 8580D filter; the fabricated Sid8580D must
    ///   be removed so the "D" revision reduces to the single 8580 model.
    /// Acceptance: no ViceSharp.Chips.Audio.Sid8580D type exists in the assembly.
    /// viceCite: none (no reSID counterpart).
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-FILTER-8580-14", ParityTag.Divergent, pending: false)]
    public void No8580DFilter_TypeRemoved()
    {
        Type? t = typeof(Sid6581).Assembly.GetType("ViceSharp.Chips.Audio.Sid8580D");
        Assert.Null(t);
    }

    // -----------------------------------------------------------------------
    // Lockstep helper: managed Sid8580 vs reSID c64c oracle (SidModel 1 = 8580)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Drives the managed Sid8580 and the reSID single-cycle oracle (c64c
    /// selector, SidModel 1 = MOS8580) from reset through the same register
    /// program and asserts the per-cycle filter output matches every phi2 cycle.
    /// compareFilterOutput selects Filter::output() (oracle probe[8]) vs the
    /// final SID::output() (oracle SidExactOutput). The oracle's filter dither is
    /// zeroed in the shim so the deterministic model is comparable.
    /// </summary>
    private static void AssertFilter8580LockstepVsOracle(
        (ushort reg, byte val)[] program, int cycles, bool compareFilterOutput)
    {
        var sid = MakeSid8580();
        foreach (var (reg, val) in program) sid.Write((ushort)(0xD400 + reg), val);

        var native = ViceNativeBridge.CreateMachine("c64c");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);
            foreach (var (reg, val) in program) ViceNativeBridge.SidExactWrite(native, reg, val);

            for (int c = 0; c < cycles; c++)
            {
                sid.Tick();
                ViceNativeBridge.SidExactClock(native, 1);
                if (compareFilterOutput)
                {
                    int oracleFilter = ViceNativeBridge.SidExactGetState(native).GetFilterProbe()[8];
                    Assert.Equal(oracleFilter, sid.CycleFilterOutput);
                }
                else
                {
                    Assert.Equal(ViceNativeBridge.SidExactOutput(native), sid.CycleExternalFilterOutput);
                }
            }
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }
}
