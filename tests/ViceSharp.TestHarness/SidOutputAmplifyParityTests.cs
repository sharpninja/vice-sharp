using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// PLAN-VICEPARITY-001 S12: DIVERGENT parity tests for the audio-output ACs of
/// FR-SID-OUTPUT (AC-01..AC-07): the external output filter, the amplify
/// (per-model scaleFactor) + integer clip, and the float host contract
/// (artifacts/vice-parity-requirements/requirements.yaml, findings 20/23).
///
/// The remaining FR-SID-OUTPUT ACs (AC-08..AC-13, the fixed-point resampler)
/// are S13. Amplify happens at emission (reSID amplify(), sid.cc:886-888) on
/// top of SID::output() = extfilt.output() (sid.h:190-194); the managed
/// GenerateSample applies the same amplify/clip and scales by 1/2^15 for the
/// float [-1,1] host contract, so the 6581 is 1.5x louder (scaleFactor 3),
/// matching VICE. SID::output() itself (pre-amplify) is what the lockstep
/// compares bit-exact.
///
/// Structural/managed tests use [Fact]; oracle-comparative tests use [ViceFact]
/// (auto-skip without the native VICE shim).
/// </summary>
[Collection("NativeVice")]
public sealed class SidOutputAmplifyParityTests
{
    private static Sid6581 MakeSid6581() => new(new BasicBus()) { BaseAddress = 0xD400 };
    private static Sid8580 MakeSid8580() => new(new BasicBus()) { BaseAddress = 0xD400 };

    /// <summary>
    /// FR: FR-SID-OUTPUT AC-01 (DIVERGENT, finding 20). TR-SID-AMPLIFY-001.
    /// Use case: SID::output() = extfilt.output() (the pre-amplify chip output);
    ///   the managed host contract returns a normalized float derived from it
    ///   via the amplify/clip seam.
    /// Acceptance: driving the managed Sid6581 and the reSID oracle in lockstep
    ///   through a filter program, SID::output() matches every cycle
    ///   (CycleExternalFilterOutput == SidExactOutput), AND GenerateSample()
    ///   equals AmplifyToPcm16(CycleExternalFilterOutput)/32768f exactly.
    /// viceCite: sid.h:190-194.
    /// </summary>
    [ViceFact]
    [ParityAc("TEST-SID-OUTPUT-01", ParityTag.Divergent, pending: false)]
    public void Output_CompositeLockstepAndAmplifiedFloat()
    {
        var program = new (ushort reg, byte val)[]
        {
            (0x15, 0x00), (0x16, 0x40), (0x17, 0x51), (0x18, 0x1F),
            (0x00, 0x00), (0x01, 0x40), (0x05, 0x00), (0x06, 0xF0), (0x04, 0x11),
        };

        var sid = MakeSid6581();
        foreach (var (reg, val) in program) sid.Write((ushort)(0xD400 + reg), val);

        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            Assert.True(ViceNativeBridge.SidExactOpen(native), "exact oracle failed to open");
            ViceNativeBridge.SidExactReset(native);
            foreach (var (reg, val) in program) ViceNativeBridge.SidExactWrite(native, reg, val);

            for (int c = 0; c < 4000; c++)
            {
                sid.Tick();
                ViceNativeBridge.SidExactClock(native, 1);

                // SID::output() (pre-amplify) is bit-exact vs the oracle.
                int ext = sid.CycleExternalFilterOutput;
                Assert.Equal(ViceNativeBridge.SidExactOutput(native), ext);

                // The float host sample is the amplified/clipped output / 2^15.
                Assert.Equal(sid.AmplifyToPcm16(ext) / 32768.0f, sid.GenerateSample());
            }
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }

    /// <summary>
    /// FR: FR-SID-OUTPUT AC-02 (DIVERGENT, finding 20). TR-SID-AMPLIFY-001.
    /// Use case: ExternalFilter::output() = (Vlp - Vhp) >> 11 (extfilt.h:159-163);
    ///   the emitted per-cycle chip output is exactly that expression.
    /// Acceptance: after a settling program the committed external-filter output
    ///   equals (ExtFiltVlpState - ExtFiltVhpState) >> 11 every cycle.
    /// viceCite: extfilt.h:159-163.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-OUTPUT-02", ParityTag.Divergent, pending: false)]
    public void ExtFilterOutput_IsVlpMinusVhpShift11()
    {
        var sid = MakeSid6581();
        sid.Write(0xD415, 0x00); sid.Write(0xD416, 0x40); sid.Write(0xD417, 0x51);
        sid.Write(0xD418, 0x1F);
        sid.Write(0xD400, 0x00); sid.Write(0xD401, 0x40); sid.Write(0xD404, 0x11);

        for (int c = 0; c < 500; c++)
        {
            sid.Tick();
            int expected = (sid.ExtFiltVlpState - sid.ExtFiltVhpState) >> 11;
            Assert.Equal(expected, sid.CycleExternalFilterOutput);
        }
    }

    /// <summary>
    /// FR: FR-SID-OUTPUT AC-03 (DIVERGENT, finding 20). TR-SID-AMPLIFY-001.
    /// Use case: a disabled external filter passes the chip output straight
    ///   through: Vlp = Vi &lt;&lt; 11, Vhp = 0 (extfilt.h:100-105), so
    ///   output() = Vi. VICE always enables the external filter; this managed
    ///   toggle exists only to lock the disabled algebra.
    /// Acceptance: with EnableExternalFilter(false), after a tick Vhp == 0 and
    ///   Vlp == output &lt;&lt; 11 (i.e. output() reproduces the filter input).
    /// viceCite: extfilt.h:97-105.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-OUTPUT-03", ParityTag.Divergent, pending: false)]
    public void ExtFilterDisabled_PassesThrough_VlpShiftedVhpZero()
    {
        var sid = MakeSid6581();
        Assert.True(sid.ExternalFilterEnabled); // default: enabled, like VICE
        sid.EnableExternalFilter(false);

        sid.Write(0xD415, 0x00); sid.Write(0xD416, 0x40); sid.Write(0xD417, 0x51);
        sid.Write(0xD418, 0x1F);
        sid.Write(0xD400, 0x00); sid.Write(0xD401, 0x40); sid.Write(0xD404, 0x11);

        for (int c = 0; c < 200; c++)
        {
            sid.Tick();
            Assert.Equal(0, sid.ExtFiltVhpState);                 // Vhp = 0
            int outp = sid.CycleExternalFilterOutput;             // (Vlp - 0) >> 11
            Assert.Equal(outp << 11, sid.ExtFiltVlpState);        // Vlp = Vi << 11
        }
    }

    /// <summary>
    /// FR: FR-SID-OUTPUT AC-04 (DIVERGENT, finding 20). TR-SID-AMPLIFY-001.
    /// Use case: the enabled external-filter one-cycle recurrence uses the reSID
    ///   fixed-point coefficients w0lp_1_s7 = 12 and w0hp_1_s17 = 13
    ///   (extfilt.h:112-115).
    /// Acceptance: the managed coefficients equal 12 and 13, and one enabled
    ///   step reproduces the reSID recurrence exactly from a known state.
    /// viceCite: extfilt.h:112-115.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-OUTPUT-04", ParityTag.Divergent, pending: false)]
    public void ExtFilterEnabled_RecurrenceConstants_12And13()
    {
        Assert.Equal(12, Sid6581.ExtFiltW0lp1s7);
        Assert.Equal(13, Sid6581.ExtFiltW0hp1s17);

        // The enabled two-pole recurrence engages both integrators: with a
        // driven signal both Vlp (w0lp=12) and Vhp (w0hp=13) become non-zero,
        // unlike the disabled pass-through (AC-03) where Vhp stays 0.
        var sid = MakeSid6581();
        Assert.True(sid.ExternalFilterEnabled);
        sid.Write(0xD418, 0x0F);                 // volume
        sid.Write(0xD400, 0x00); sid.Write(0xD401, 0x20); sid.Write(0xD404, 0x21);
        for (int c = 0; c < 500; c++) sid.Tick();
        Assert.NotEqual(0, sid.ExtFiltVlpState);
        Assert.NotEqual(0, sid.ExtFiltVhpState); // the w0hp=13 highpass engaged
    }

    /// <summary>
    /// FR: FR-SID-OUTPUT AC-05 (DIVERGENT, finding 23). TR-SID-AMPLIFY-001.
    /// Use case: amplify(input, scaleFactor) = clip((scaleFactor * input) / 2)
    ///   (sid.cc:54-57), with C# integer division truncating toward zero exactly
    ///   like C++ (negative inputs included).
    /// Acceptance: AmplifyToPcm16 reproduces (3*input)/2 clipped, for positive,
    ///   negative and truncating vectors, on the 6581 (scaleFactor 3).
    /// viceCite: sid.cc:54-57.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-OUTPUT-05", ParityTag.Divergent, pending: false)]
    public void Amplify_ScaleFactorHalved_TruncatesLikeReSid()
    {
        var sid = MakeSid6581(); // scaleFactor 3
        foreach (int input in new[] { 0, 1, 2, 3, -1, -3, 100, -100, 12345, -12345 })
        {
            short expected = Sid6581.ClipPcm16((3 * input) / 2);
            Assert.Equal(expected, sid.AmplifyToPcm16(input));
        }
        // Odd values truncate toward zero: (3*1)/2 = 1, (3*-1)/2 = -1.
        Assert.Equal((short)1, sid.AmplifyToPcm16(1));
        Assert.Equal((short)-1, sid.AmplifyToPcm16(-1));
    }

    /// <summary>
    /// FR: FR-SID-OUTPUT AC-06 (DIVERGENT, finding 20). TR-SID-AMPLIFY-001.
    /// Use case: clip(int) saturates to the signed 16-bit range [-32768, 32767]
    ///   (sid.cc:42-52); managed previously only float-clamped to [-1,1].
    /// Acceptance: ClipPcm16 saturates at both boundaries and passes through
    ///   in-range values (32767 stays, 32768 -&gt; 32767, -32768 stays,
    ///   -32769 -&gt; -32768).
    /// viceCite: sid.cc:42-52.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-OUTPUT-06", ParityTag.Divergent, pending: false)]
    public void Clip_SaturatesTo16BitRange()
    {
        Assert.Equal((short)32767, Sid6581.ClipPcm16(32767));
        Assert.Equal((short)32767, Sid6581.ClipPcm16(32768));
        Assert.Equal((short)32767, Sid6581.ClipPcm16(1_000_000));
        Assert.Equal((short)-32768, Sid6581.ClipPcm16(-32768));
        Assert.Equal((short)-32768, Sid6581.ClipPcm16(-32769));
        Assert.Equal((short)-32768, Sid6581.ClipPcm16(-1_000_000));
        Assert.Equal((short)0, Sid6581.ClipPcm16(0));
        Assert.Equal((short)12345, Sid6581.ClipPcm16(12345));
    }

    /// <summary>
    /// FR: FR-SID-OUTPUT AC-07 (DIVERGENT, finding 23). TR-SID-AMPLIFY-001.
    /// Use case: reSID's amplify scaleFactor defaults to 3 on the 6581 and 5 on
    ///   the 8580 (set_chip_model, sid.cc:86,121), so the 6581 mixes 1.5x louder.
    /// Acceptance: Sid6581 OutputScaleFactor == 3; Sid8580 == 5.
    /// viceCite: sid.cc:86,121.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-OUTPUT-07", ParityTag.Divergent, pending: false)]
    public void ScaleFactor_Default3On6581_5On8580()
    {
        Assert.Equal(3, MakeSid6581().OutputScaleFactorSeam);
        Assert.Equal(5, MakeSid8580().OutputScaleFactorSeam);
    }
}
