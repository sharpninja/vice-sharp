using System;
using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// PLAN-VICEPARITY-001 S13: DIVERGENT parity tests for the fixed-point sampling
/// engine ACs of FR-SID-OUTPUT (AC-08..AC-13): the SAMPLE_FAST/INTERPOLATE/
/// RESAMPLE decimation, the 16.16 cycles-per-sample cadence, the resample
/// constants, and the Kaiser-windowed sinc FIR table
/// (artifacts/vice-parity-requirements/requirements.yaml, finding 20).
///
/// The managed sampling engine (Sid6581.Sampling.cs) is a statement-for-statement
/// port of reSID's set_sampling_parameters + clock_fast/interpolate/resample
/// (sid.cc:542-1038). The bit-exact ACs drain the native buffered oracle
/// (vice_sid_exact_clock_buffered, TR-SID-ORACLE-002 - first consumption) in
/// 4096-cycle chunks and compare the emitted short stream element-for-element
/// against the managed ClockBuffered. The structural ACs assert the derived
/// cadence/constants/FIR table, sealed end-to-end by the lockstep.
/// </summary>
[Collection("NativeVice")]
public sealed class SidResampleDivergentParityTests
{
    private const double ClockPal = 985248.0;
    private const double SampleRate = 44100.0;

    private static Sid6581 MakeSid6581() => new(new BasicBus()) { BaseAddress = 0xD400 };
    private static Sid8580 MakeSid8580() => new(new BasicBus()) { BaseAddress = 0xD400 };

    // A gated saw + LP-filtered program so the resampled stream is non-trivial.
    private static readonly (ushort reg, byte val)[] SawLpProgram =
    {
        (0x15, 0x00), (0x16, 0x40), (0x17, 0x51), (0x18, 0x1F),
        (0x00, 0x00), (0x01, 0x28), (0x05, 0x00), (0x06, 0xF0), (0x04, 0x21),
        (0x07, 0x00), (0x08, 0x40), (0x0B, 0x41),
    };

    /// <summary>
    /// FR: FR-SID-OUTPUT AC-08 (DIVERGENT, finding 20). TR-SID-RESAMPLE-001.
    /// Use case: SAMPLE_FAST fixed-point next-sample selection (sid.cc:868-892)
    ///   replaces the managed double accumulator: next_sample_offset =
    ///   sample_offset + cycles_per_sample + half; delta_t_sample = it &gt;&gt; 16.
    /// Acceptance: the managed ClockFast reproduces the fixed-point sample cadence
    ///   bit-exact vs the buffered oracle: identical sample count AND identical
    ///   unconsumed remainder every 4096-cycle chunk for &gt;= 30000 samples.
    ///   NB the per-sample VALUES are NOT locked here: reSID clock_fast advances
    ///   the chain via the batched SID::clock(delta_t) (sid.cc:745-832), which
    ///   holds the voice outputs constant over the window and sub-steps the
    ///   filter (dt=3) / external filter (dt=8), while the managed fast path
    ///   clocks cycle-exact. That batched sub-stepping is the deferred
    ///   TEST-SID-FILTER-CLOCK-03/04 (PLAN-VICEPARITY-001 out-of-scope). The
    ///   high-fidelity value lockstep is sealed by OUTPUT-09 (interpolate) and
    ///   OUTPUT-10 (resample), both cycle-exact in reSID.
    /// viceCite: sid.cc:868-892.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-OUTPUT-08", ParityTag.Divergent, pending: false)]
    public void SampleFast_FixedPointCadence_MatchesOracle()
        => AssertFastCadenceMatchesOracle("c64", MakeSid6581, SawLpProgram, 30000);

    /// <summary>
    /// FR: FR-SID-OUTPUT AC-09 (DIVERGENT, finding 20). TR-SID-RESAMPLE-001.
    /// Use case: SAMPLE_INTERPOLATE linear interpolation (sid.cc:904-938); managed
    ///   previously had no interpolate path.
    /// Acceptance: the managed ClockBuffered short stream matches the buffered
    ///   oracle at SAMPLE_INTERPOLATE, element-for-element, for &gt;= 30000 samples.
    /// viceCite: sid.cc:904-938.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-OUTPUT-09", ParityTag.Divergent, pending: false)]
    public void SampleInterpolate_BufferedLockstep()
        => AssertBufferedOutputLockstep("c64", MakeSid6581, SidSamplingMethod.Interpolate, SawLpProgram, 30000);

    /// <summary>
    /// FR: FR-SID-OUTPUT AC-10 (DIVERGENT, finding 20). TR-SID-RESAMPLE-001.
    /// Use case: SAMPLE_RESAMPLE Kaiser-windowed-sinc FIR resampling
    ///   (sid.cc:977-1038), the theoretically-correct decimation; managed had none.
    /// Acceptance: the managed ClockBuffered short stream matches the buffered
    ///   oracle at SAMPLE_RESAMPLE for &gt;= 44100 samples (crossing the FIR ring
    ///   buffer many times) on BOTH the 6581 (c64) and the 8580 (c64c) - the
    ///   latter also seals the S11 scaleFactor 5 and S12 amplify end-to-end.
    /// viceCite: sid.cc:977-1038.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-OUTPUT-10", ParityTag.Divergent, pending: false)]
    public void SampleResample_KaiserFir_BufferedLockstep_BothModels()
    {
        AssertBufferedOutputLockstep("c64", MakeSid6581, SidSamplingMethod.Resample, SawLpProgram, 44100);
        AssertBufferedOutputLockstep("c64c", MakeSid8580, SidSamplingMethod.Resample, SawLpProgram, 44100);
    }

    /// <summary>
    /// FR: FR-SID-OUTPUT AC-11 (DIVERGENT, finding 20). TR-SID-RESAMPLE-001.
    /// Use case: cycles_per_sample = round(clock/sample * (1&lt;&lt;16)) as a 16.16
    ///   fixed-point value (sid.cc:619-620); managed previously used a double ratio.
    /// Acceptance: after SetSamplingParameters the managed cycles_per_sample equals
    ///   (int)(ClockPal/SampleRate*(1&lt;&lt;16)+0.5) exactly.
    /// viceCite: sid.cc:619-620.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-OUTPUT-11", ParityTag.Divergent, pending: false)]
    public void CyclesPerSample_Is16Point16FixedPoint()
    {
        var sid = MakeSid6581();
        Assert.True(sid.SetSamplingParameters(ClockPal, SidSamplingMethod.Resample, SampleRate));
        int expected = (int)(ClockPal / SampleRate * (1 << 16) + 0.5);
        Assert.Equal(expected, sid.CyclesPerSampleSeam);
    }

    /// <summary>
    /// FR: FR-SID-OUTPUT AC-12 (DIVERGENT, finding 20). TR-SID-RESAMPLE-001.
    /// Use case: the resample constants FIR_N=125, FIR_RES=285, FIR_SHIFT=15,
    ///   RINGSIZE=16384, FIXP_SHIFT=16 (sid.h:144-158).
    /// Acceptance: the managed constants match, and after SetSamplingParameters the
    ///   derived FIR length is odd and the ring is RINGSIZE*2.
    /// viceCite: sid.h:144-158.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-OUTPUT-12", ParityTag.Divergent, pending: false)]
    public void ResampleConstants_MatchReSid()
    {
        Assert.Equal(125, Sid6581.FirNConst);
        Assert.Equal(285, Sid6581.FirResConst);
        Assert.Equal(15, Sid6581.FirShiftConst);
        Assert.Equal(1 << 14, Sid6581.RingSizeConst);
        Assert.Equal(16, Sid6581.FixpShiftConst);

        var sid = MakeSid6581();
        Assert.True(sid.SetSamplingParameters(ClockPal, SidSamplingMethod.Resample, SampleRate));
        Assert.Equal(1, sid.FirLengthSeam & 1); // fir_N is odd
        Assert.True(sid.FirLengthSeam < (1 << 14));
    }

    /// <summary>
    /// FR: FR-SID-OUTPUT AC-13 (DIVERGENT, finding 20). TR-SID-RESAMPLE-001.
    /// Use case: the FIR table is a sinc weighted by a Kaiser window, the only
    ///   legitimate floating-point stage, rounded to short (sid.cc:646-717).
    /// Acceptance: the managed FIR table matches an independent in-test
    ///   recomputation of the sinc*Kaiser formula (round-half-away-from-zero) for
    ///   every entry.
    /// viceCite: sid.cc:646-717.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-OUTPUT-13", ParityTag.Divergent, pending: false)]
    public void FirTable_MatchesIndependentSincKaiserRecompute()
    {
        var sid = MakeSid6581();
        Assert.True(sid.SetSamplingParameters(ClockPal, SidSamplingMethod.Resample, SampleRate));

        int firN = sid.FirLengthSeam;
        int firRes = sid.FirResolutionSeam;
        var fir = sid.FirTableSeam;
        Assert.Equal(firN * firRes, fir.Length);

        // Independent recomputation of the Kaiser-windowed sinc (sid.cc:646-717).
        const double pi = 3.1415926535897932385;
        double a = -20 * Math.Log10(1.0 / (1 << 16));
        double beta = 0.1102 * (a - 8.7);
        double i0beta = I0(beta);
        double wc = pi;
        double fSamplesPerCycle = SampleRate / ClockPal;
        double fCyclesPerSample = ClockPal / SampleRate;
        const double filterScale = 0.97;

        for (int i = 0; i < firRes; i++)
        {
            int firOffset = i * firN + firN / 2;
            double jOffset = (double)i / firRes;
            for (int j = -firN / 2; j <= firN / 2; j++)
            {
                double jx = j - jOffset;
                double wt = wc * jx / fCyclesPerSample;
                double temp = jx / (firN / 2);
                double kaiser = Math.Abs(temp) <= 1 ? I0(beta * Math.Sqrt(1 - temp * temp)) / i0beta : 0;
                double sincwt = Math.Abs(wt) >= 1e-6 ? Math.Sin(wt) / wt : 1;
                double val = (1 << 15) * filterScale * fSamplesPerCycle * wc / pi * sincwt * kaiser;
                short expected = (short)Math.Round(val, MidpointRounding.AwayFromZero);
                Assert.Equal(expected, fir[firOffset + j]);
            }
        }
    }

    // reSID I0 (sid.cc:542-560), reproduced for the independent FIR recompute.
    private static double I0(double x)
    {
        const double i0e = 1e-6;
        double sum = 1, u = 1, halfx = x / 2.0;
        int n = 1;
        double temp;
        do
        {
            temp = halfx / n++;
            u *= temp * temp;
            sum += u;
        } while (u >= i0e * sum);
        return sum;
    }

    // -----------------------------------------------------------------------
    // SAMPLE_FAST cadence lockstep: the fixed-point next-sample selection
    // (sample count + unconsumed remainder) is bit-exact vs the oracle even
    // though the per-sample values diverge (batched vs cycle-exact clock).
    // -----------------------------------------------------------------------
    private static void AssertFastCadenceMatchesOracle(
        string oracleMachine, Func<Sid6581> makeSid,
        (ushort reg, byte val)[] program, int minSamples)
    {
        var sid = makeSid();
        Assert.True(sid.SetSamplingParameters(ClockPal, SidSamplingMethod.Fast, SampleRate));
        foreach (var (reg, val) in program) sid.Write((ushort)(0xD400 + reg), val);

        var native = ViceNativeBridge.CreateMachine(oracleMachine);
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);
            Assert.True(ViceNativeBridge.SidExactSetSampling(native, (int)SidSamplingMethod.Fast, SampleRate));
            foreach (var (reg, val) in program) ViceNativeBridge.SidExactWrite(native, reg, val);

            var mbuf = new short[512];
            var obuf = new short[512];
            int produced = 0;
            while (produced < minSamples)
            {
                int mc = 4096, oc = 4096;
                int mGot = sid.ClockBuffered(ref mc, mbuf);
                int oGot = ViceNativeBridge.SidExactClockBuffered(native, ref oc, obuf);
                // The fixed-point cadence (count + remainder) is purely arithmetic
                // (cycles_per_sample + half-cycle bias), independent of the audio
                // values, so it is bit-exact vs the oracle every chunk.
                Assert.Equal(oGot, mGot);
                Assert.Equal(oc, mc);
                produced += mGot;
            }
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    // -----------------------------------------------------------------------
    // Buffered-output lockstep: managed ClockBuffered vs the native buffered
    // oracle (vice_sid_exact_clock_buffered), 4096-cycle chunks, element-exact.
    // -----------------------------------------------------------------------
    private static void AssertBufferedOutputLockstep(
        string oracleMachine, Func<Sid6581> makeSid, SidSamplingMethod method,
        (ushort reg, byte val)[] program, int minSamples)
    {
        var sid = makeSid();
        Assert.True(sid.SetSamplingParameters(ClockPal, method, SampleRate),
            $"managed SetSamplingParameters({method}) failed");
        foreach (var (reg, val) in program) sid.Write((ushort)(0xD400 + reg), val);

        var native = ViceNativeBridge.CreateMachine(oracleMachine);
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);
            Assert.True(ViceNativeBridge.SidExactSetSampling(native, (int)method, SampleRate),
                $"oracle SidExactSetSampling({method}) failed");
            foreach (var (reg, val) in program) ViceNativeBridge.SidExactWrite(native, reg, val);

            var mbuf = new short[512];
            var obuf = new short[512];
            int produced = 0;
            while (produced < minSamples)
            {
                int mc = 4096, oc = 4096;
                int mGot = sid.ClockBuffered(ref mc, mbuf);
                int oGot = ViceNativeBridge.SidExactClockBuffered(native, ref oc, obuf);

                Assert.Equal(oGot, mGot);   // same sample count per chunk
                Assert.Equal(oc, mc);       // same unconsumed remainder
                for (int i = 0; i < mGot; i++)
                {
                    if (mbuf[i] != obuf[i])
                        Assert.Fail($"{method} sample {produced + i}: managed {mbuf[i]} != oracle {obuf[i]}");
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
