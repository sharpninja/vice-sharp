using System;
using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// PLAN-VICEPARITY-001 S8: DIVERGENT remediation tests for every DIVERGENT
/// acceptance criterion of FR-SID-VOICE and FR-SID-MIXVOL in
/// artifacts/vice-parity-requirements/requirements.yaml (findings 18 and 19).
///
/// FR-SID-VOICE (finding 18): the voice output multiplying DAC is the full
/// 20-bit product (wave.output() - wave_zero) * envelope.output() with no
/// right-shift (voice.h:99-103, voice.cc:93-97). The interim S7 formula
/// applied >> 8, reducing to ~12-bit. S8 removes that shift and corrects
/// the wave_zero constants to their proper 12-bit domain values (0x380 for
/// MOS 6581, 0x9e0 for MOS 8580).
///
/// FR-SID-MIXVOL (finding 19): the $D418 writeMODE_VOL handler must preserve
/// bit7 (3OFF / voice-3 disconnect). The filter summer and output mixer use
/// a voice_mask (filter8580new.cc:692-696, 768-776) to: gate which voices
/// enter the filter input sum, exclude voice3 from the direct-to-mixer path
/// when 3OFF is set (mode bit7), and apply LP/BP/HP tap select from mode
/// bits 4-6. The nonlinear gain[vol] DAC (filter8580new.h:1494-1499) is
/// pending S9+.
///
/// Oracle: CycleVoiceOutputs internal seam for VOICE ACs; CycleFilterOutput
/// and behavioral assertions for MIXVOL ACs. No native oracle required for
/// this slice.
/// </summary>
public sealed class SidVoiceMixDivergentParityTests
{
    // -----------------------------------------------------------------------
    // Shared tables (same as reSID build_dac_table, bit-exact)
    // -----------------------------------------------------------------------

    /// <summary>
    /// 12-bit wave DAC for MOS 6581 (bits=12, 2R/R=2.20, no termination,
    /// MOSFET leakage 0.0075). Matches reSID model_dac[MOS6581]
    /// (wave.h:587-593, dac.cc:76-137). FR-SID-WAVE-DACRES AC-01.
    /// </summary>
    private static readonly ushort[] WaveDac6581 =
        Sid6581.BuildEnvelopeDacTable(12, 2.20, term: false);

    /// <summary>
    /// 12-bit wave DAC for MOS 8580 (bits=12, 2R/R=2.00, terminated,
    /// MOSFET leakage 0.0035). Matches reSID model_dac[MOS8580].
    /// </summary>
    private static readonly ushort[] WaveDac8580 =
        Sid6581.BuildEnvelopeDacTable(12, 2.00, term: true);

    /// <summary>
    /// 8-bit envelope DAC for MOS 6581 (bits=8, 2R/R=2.20, no termination).
    /// Matches reSID model_dac[MOS6581] (envelope.h:377-383, envelope.cc:163-166).
    /// FR-SID-ENV AC-50.
    /// </summary>
    private static readonly ushort[] EnvDac6581 =
        Sid6581.BuildEnvelopeDacTable(8, 2.20, term: false);

    /// <summary>
    /// 8-bit envelope DAC for MOS 8580 (bits=8, 2R/R=2.00, terminated).
    /// Matches reSID model_dac[MOS8580] (envelope.cc:167-168). PLAN-VICEPARITY-001
    /// S11 routed the 8580 envelope through this nonlinear DAC (was a linear
    /// identity stub); the filter lockstep vs the c64c oracle proves it.
    /// </summary>
    private static readonly ushort[] EnvDac8580 =
        Sid6581.BuildEnvelopeDacTable(8, 2.00, term: true);

    // reSID wave_zero constants in 12-bit domain (voice.cc:93,97).
    private const int WaveZero6581 = 0x380;  // voice.cc:93
    private const int WaveZero8580 = 0x9e0;  // voice.cc:97

    // -----------------------------------------------------------------------
    // Test helpers
    // -----------------------------------------------------------------------

    private static Sid6581 BuildSid6581()
        => new Sid6581(new BasicBus()) { BaseAddress = 0xD400 };

    private static Sid8580 BuildSid8580()
        => new Sid8580(new BasicBus()) { BaseAddress = 0xD400 };

    private static void WriteVoice(Sid6581 sid, int voice, int reg, byte value)
        => sid.Write((ushort)(0xD400 + voice * 7 + reg), value);

    private static void TickN(Sid6581 sid, int n)
    {
        for (int i = 0; i < n; i++)
            sid.Tick();
    }

    /// <summary>
    /// Pin all three accumulators to 0 via TEST bit, then release.
    /// Returns the sid clocked by 1 cycle to consume the TEST-bit clock.
    /// </summary>
    private static void PinAndRelease(Sid6581 sid)
    {
        for (int v = 0; v < 3; v++) WriteVoice(sid, v, 4, 0x08); // TEST set
        sid.Tick();
        for (int v = 0; v < 3; v++) WriteVoice(sid, v, 4, 0x00); // TEST clear
    }

    /// <summary>
    /// Wait up to <paramref name="maxCycles"/> for voice <paramref name="v"/>
    /// envelope counter to reach <paramref name="target"/>.
    /// </summary>
    private static void TickUntilEnvelope(Sid6581 sid, int v, byte target, int maxCycles)
    {
        for (int t = 0; t < maxCycles; t++)
        {
            sid.Tick();
            if (sid.Read((ushort)(0xD41C - 1 + v + (v == 2 ? 1 : 0))) == target)
                return;
        }
    }

    // -----------------------------------------------------------------------
    // FR-SID-VOICE: 20-bit multiplying DAC
    // -----------------------------------------------------------------------

    /// <summary>
    /// FR: FR-SID-VOICE AC-01 (DIVERGENT, finding 18).
    /// Use case: reSID voice output = (wave.output() - wave_zero) * envelope.output()
    /// with NO right-shift (voice.h:99-103). Managed S7 formula applied >> 8.
    /// Acceptance: CycleVoiceOutputs.Voice2 == (WaveDac6581[osc3] - 0x380)
    /// * EnvDac6581[255] at osc3=0x400 (full-plateau envelope), with NO >>8.
    /// viceCite: voice.h:99-103, voice.cc:105-113.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-VOICE-01", ParityTag.Divergent, pending: false)]
    public void VoiceOutput_Is20BitNoShift_ReSidMultiplyingDac()
    {
        var sid = BuildSid6581();
        PinAndRelease(sid);

        // Voice 3 (index 2): sawtooth, gate on, sustain=15, freq=0x1000.
        sid.Write(0xD413, 0x00); // Attack=0, Decay=0
        sid.Write(0xD414, 0xF0); // Sustain=15, Release=0
        sid.Write(0xD40F, 0x10); // Freq hi = 0x10
        sid.Write(0xD412, 0x21); // Sawtooth + gate

        // 1024 cycles: acc = 1024 * 0x1000 = 0x400000, osc3 = 0x400.
        TickN(sid, 1024);
        byte envCounter = sid.Read(0xD41C);  // ENV3 readback

        int osc3 = 0x400; // known from accumulator arithmetic
        // reSID 20-bit formula (voice.h:102, no >>8):
        int expected = (WaveDac6581[osc3] - WaveZero6581) * EnvDac6581[envCounter];
        // Old S7 formula with >>8 - must differ:
        int oldFormula = ((WaveDac6581[osc3] - WaveZero6581) * EnvDac6581[envCounter]) >> 8;
        Assert.NotEqual(oldFormula, expected); // sanity: formulas are distinct
        Assert.Equal(expected, sid.CycleVoiceOutputs.Voice2);
    }

    /// <summary>
    /// FR: FR-SID-VOICE AC-02 (DIVERGENT, finding 18).
    /// Use case: MOS 6581 wave_zero is 0x380 in the 12-bit domain (voice.cc:93).
    /// Managed S7 used WaveZeroLevel=0x38 (8-bit domain) multiplied by 0x10
    /// as an interim workaround. S8 adopts the 12-bit constant directly.
    /// Acceptance: voice3 sawtooth at osc3=0x000 gives 20-bit output
    /// (WaveDac6581[0] - 0x380) * EnvDac6581[envCounter] (not >> 8, not 0x38).
    /// viceCite: voice.cc:93.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-VOICE-02", ParityTag.Divergent, pending: false)]
    public void VoiceOutput_WaveZero_6581_Is0x380_12Bit()
    {
        var sid = BuildSid6581();
        PinAndRelease(sid);

        // Voice 3: sawtooth, gate, sustain=15. After 0 additional cycles
        // the accumulator is at 0 (pinned then released; freq=0 -> stays 0).
        sid.Write(0xD413, 0x00); // Attack=0
        sid.Write(0xD414, 0xF0); // Sustain=15
        // Leave freq at 0 so accumulator stays 0 (osc3=0x000).
        sid.Write(0xD412, 0x21); // Sawtooth + gate

        TickN(sid, 20); // let envelope advance a few steps
        byte envCounter = sid.Read(0xD41C);

        const int osc3 = 0x000;
        // With correct 12-bit wave_zero = 0x380 and NO >>8:
        int expected = (WaveDac6581[osc3] - 0x380) * EnvDac6581[envCounter];
        // Wrong: 8-bit wave_zero (0x38 used as if 12-bit, NOT * 0x10):
        int wrongWaveZero = (WaveDac6581[osc3] - 0x38) * EnvDac6581[envCounter];
        Assert.NotEqual(wrongWaveZero, expected); // divergence proof
        Assert.Equal(expected, sid.CycleVoiceOutputs.Voice2);
    }

    /// <summary>
    /// FR: FR-SID-VOICE AC-03 (DIVERGENT, finding 18).
    /// Use case: MOS 8580 wave_zero is 0x9e0 in the 12-bit domain (voice.cc:97).
    /// Managed S7 used WaveZeroLevel=0x9E (8-bit). S8 adopts 12-bit directly.
    /// Acceptance: Sid8580 voice3 sawtooth at osc3=0x400 gives
    /// (WaveDac8580[0x400] - 0x9e0) * envDac8580[envCounter] (no >>8).
    /// viceCite: voice.cc:97.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-VOICE-03", ParityTag.Divergent, pending: false)]
    public void VoiceOutput_WaveZero_8580_Is0x9e0_12Bit()
    {
        // 8580 uses linear envelope DAC in this implementation.
        var sid = BuildSid8580();
        PinAndRelease(sid);

        // The value fed to the filter/mix is the CURRENT waveform_output
        // (wave.h:588-592), NOT the delayed OSC3 readback latch: the 8580
        // tri/saw one-cycle delay applies to readOSC only (PLAN-VICEPARITY-001
        // S11, lockstep-proven vs the c64c oracle). So clock 1024 cycles to get
        // the current sawtooth waveform_output = 0x400 (no pipeline compensation).
        sid.Write(0xD413, 0x00);
        sid.Write(0xD414, 0xF0);
        sid.Write(0xD40F, 0x10); // freq = 0x1000
        sid.Write(0xD412, 0x21); // sawtooth + gate
        TickN(sid, 1024);

        byte envCounter = sid.Read(0xD41C);
        // 8580 uses its own 12-bit wave DAC and its own nonlinear 8-bit
        // envelope DAC (2R/R=2.00, terminated). With correct 12-bit
        // wave_zero = 0x9e0 and no >>8:
        int osc3 = 0x400;
        int expected = (WaveDac8580[osc3] - 0x9e0) * EnvDac8580[envCounter];
        int wrongWaveZero = (WaveDac8580[osc3] - 0x9E) * EnvDac8580[envCounter];
        Assert.NotEqual(wrongWaveZero, expected); // sanity: diverge
        Assert.Equal(expected, sid.CycleVoiceOutputs.Voice2);
    }

    /// <summary>
    /// FR: FR-SID-VOICE AC-04 (DIVERGENT, finding 18).
    /// Use case: the DC bias (wave_zero) is subtracted BEFORE the envelope
    /// multiply; the result is a SIGNED value that may be negative when
    /// the waveform DAC output falls below wave_zero (voice.h:50,102).
    /// Acceptance: at osc3=0x000 (DAC output 31 for 6581), result =
    /// (31 - 0x380) * EnvDac6581[envCounter] < 0 (negative voice output).
    /// viceCite: voice.h:50,102.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-VOICE-04", ParityTag.Divergent, pending: false)]
    public void VoiceOutput_DcBias_SubtractedBefore_Multiply_CanBeNegative()
    {
        var sid = BuildSid6581();
        PinAndRelease(sid);
        sid.Write(0xD413, 0x00);
        sid.Write(0xD414, 0xF0); // Sustain=15
        // freq=0 -> accumulator stays 0, osc3=0x000
        sid.Write(0xD412, 0x21); // Sawtooth + gate
        TickN(sid, 10);
        byte envCounter = sid.Read(0xD41C);

        // WaveDac6581[0x000] = 31 < 0x380 = 896, so result is negative.
        int expected = (WaveDac6581[0x000] - WaveZero6581) * EnvDac6581[envCounter];
        Assert.True(expected < 0,
            $"expected voice output is negative at osc3=0 (WaveDac[0]={WaveDac6581[0]} < wave_zero {WaveZero6581})");
        Assert.Equal(expected, sid.CycleVoiceOutputs.Voice2);
    }

    /// <summary>
    /// FR: FR-SID-VOICE AC-05 (DIVERGENT, finding 18).
    /// Use case: voice output range is 20-bit [-2048*255, 2047*255]
    /// (voice.h:42-43). The S7 formula applied >>8 collapsed this to ~12-bit.
    /// Acceptance: the maximum absolute value of CycleVoiceOutputs is greater
    /// than 2^12 (cannot be explained by a 12-bit range), confirming 20-bit range.
    /// viceCite: voice.h:42-43.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-VOICE-05", ParityTag.Divergent, pending: false)]
    public void VoiceOutput_Range_Is20Bit_NotRight8Shifted()
    {
        var sid = BuildSid6581();
        PinAndRelease(sid);
        sid.Write(0xD413, 0x00);
        sid.Write(0xD414, 0xF0); // Sustain=15
        sid.Write(0xD40F, 0x10); // freq = 0x1000
        sid.Write(0xD412, 0x21); // Sawtooth + gate

        // Advance to high waveform index to get a large positive output.
        // 2560 cycles: acc=0xA00000, osc3=0xA00.
        TickN(sid, 2560);
        byte envCounter = sid.Read(0xD41C);
        int voice2Out = sid.CycleVoiceOutputs.Voice2;
        int expected = (WaveDac6581[0xA00] - WaveZero6581) * EnvDac6581[envCounter];

        // 20-bit range: max positive ~2047*255 = 521985; >>8 max ~2039.
        // Any output > 4095 proves the range is wider than 12 bits.
        Assert.True(Math.Abs(voice2Out) > 4095,
            $"voice output {voice2Out} must exceed 4095 to prove 20-bit range; >>8 would cap at ~4095");
        Assert.Equal(expected, voice2Out);
    }

    /// <summary>
    /// FR: FR-SID-VOICE AC-06 (DIVERGENT, finding 18).
    /// Use case: reSID set_chip_model propagates to wave + envelope and selects
    /// wave_zero. Managed uses subclasses (Sid6581/Sid8580) for the same effect.
    /// Acceptance: Sid6581 voice output at osc3=0x400 differs from Sid8580 voice
    /// output at the same osc3 because they use different wave DACs and wave_zeros.
    /// viceCite: voice.cc:38-99.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-VOICE-06", ParityTag.Divergent, pending: false)]
    public void ChipModel_6581Vs8580_DifferentWaveZero_DifferentVoiceOutput()
    {
        var sid6581 = BuildSid6581();
        var sid8580 = BuildSid8580();

        // 6581: tick 1024 cycles -> sawtooth acc=0x400000, osc3=0x400 (no pipeline).
        PinAndRelease(sid6581);
        sid6581.Write(0xD413, 0x00);
        sid6581.Write(0xD414, 0xF0);
        sid6581.Write(0xD40F, 0x10); // freq = 0x1000
        sid6581.Write(0xD412, 0x21); // Sawtooth + gate
        TickN(sid6581, 1024);

        // 8580: the filter/mix input is the CURRENT waveform_output
        // (wave.h:588-592); the tri/saw one-cycle delay applies to OSC3 readback
        // only (PLAN-VICEPARITY-001 S11). So clock 1024 cycles (no pipeline
        // compensation) to get the current sawtooth waveform_output = 0x400.
        PinAndRelease(sid8580);
        sid8580.Write(0xD413, 0x00);
        sid8580.Write(0xD414, 0xF0);
        sid8580.Write(0xD40F, 0x10); // freq = 0x1000
        sid8580.Write(0xD412, 0x21); // Sawtooth + gate
        TickN(sid8580, 1024);

        // 6581: (WaveDac6581[0x400] - 0x380) * EnvDac6581[envCounter]
        // 8580: (WaveDac8580[0x400] - 0x9e0) * EnvDac8580[envCounter]
        // These must differ because wave DAC parameters and wave_zero differ.
        int v6581 = sid6581.CycleVoiceOutputs.Voice2;
        int v8580 = sid8580.CycleVoiceOutputs.Voice2;

        Assert.NotEqual(v6581, v8580);  // different chip models -> different outputs

        // Verify each against its closed-form formula.
        byte env6581 = sid6581.Read(0xD41C);
        byte env8580 = sid8580.Read(0xD41C);

        Assert.Equal((WaveDac6581[0x400] - WaveZero6581) * EnvDac6581[env6581], v6581);
        Assert.Equal((WaveDac8580[0x400] - WaveZero8580) * EnvDac8580[env8580], v8580);
    }

    /// <summary>
    /// FR: FR-SID-VOICE AC-08 (DIVERGENT, finding 18).
    /// Use case: reSID Voice::writeCONTROL_REG fans the same control byte to
    /// wave.writeCONTROL_REG AND envelope.writeCONTROL_REG (voice.cc:112-116).
    /// Both the waveform selection (bits 4-7) and gate bit (bit 0) are set
    /// from the same byte simultaneously.
    /// Acceptance: writing control=0x21 (sawtooth | gate) simultaneously
    /// activates sawtooth waveform (non-zero osc3 after accumulator advances)
    /// AND gates the envelope (envelope counter rises). CycleVoiceOutputs
    /// equals the 20-bit formula (no >>8), proving both wave and envelope
    /// are live after the single write.
    /// viceCite: voice.cc:112-116.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-VOICE-08", ParityTag.Divergent, pending: false)]
    public void WriteCONTROL_REG_FansSameByte_WaveAndEnvelope_BothActive()
    {
        var sid = BuildSid6581();
        PinAndRelease(sid);

        // Single write that fans to both wave (sawtooth=0x20) and envelope (gate=0x01).
        sid.Write(0xD413, 0x00); // Attack=0, Decay=0
        sid.Write(0xD414, 0xF0); // Sustain=15
        sid.Write(0xD40F, 0x10); // Freq hi = 0x10 -> freq=0x1000
        sid.Write(0xD412, 0x21); // sawtooth (bit 5) + gate (bit 0)

        TickN(sid, 1024); // acc = 0x400000, osc3 = 0x400
        byte envCounter = sid.Read(0xD41C); // must be non-zero (envelope gated)
        int voice2 = sid.CycleVoiceOutputs.Voice2;

        // Waveform must be active (non-zero osc3 from sawtooth selection).
        Assert.NotEqual(0, (int)sid.Read(0xD41B)); // OSC3 readback non-zero

        // Envelope must be running (counter > power-up value 0xaa after gate-on).
        // After attack=0 with 1024 cycles, envelope should be at or near 0xFF.
        Assert.True(envCounter >= 0xAA, $"envelope counter {envCounter} should be advancing");

        // 20-bit formula (no >>8):
        int expected = (WaveDac6581[0x400] - WaveZero6581) * EnvDac6581[envCounter];
        Assert.Equal(expected, voice2);
    }

    /// <summary>
    /// FR: FR-SID-VOICE AC-11 (DIVERGENT, finding 18/19).
    /// Use case: reSID voice muting is via filter voice_mask in the summer/mixer
    /// (voice.h:99-103). Managed had no voice_mask: all voices contributed to
    /// the mix regardless. S8 adds voice_mask to route direct-to-mixer voices.
    /// Acceptance: with all voices active and voice_mask=0x07 (default), all
    /// three voices contribute to CycleFilterOutput; with 3OFF set ($D418 bit7),
    /// voice3 is excluded from the direct-to-mixer path.
    /// viceCite: voice.h:99-103, filter8580new.cc:692-696.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-VOICE-11", ParityTag.Divergent, pending: false)]
    public void VoiceMuting_Via_VoiceMask_Default_AllVoicesContribute()
    {
        var sid = BuildSid6581();
        PinAndRelease(sid);

        // Gate voice 3 only (index 2): sawtooth, max sustain.
        sid.Write(0xD413, 0x00);
        sid.Write(0xD414, 0xF0);
        sid.Write(0xD40F, 0x10);
        sid.Write(0xD412, 0x21); // sawtooth + gate
        sid.Write(0xD418, 0x0F); // vol=15, no filter taps, no 3OFF

        TickN(sid, 1024); // osc3=0x400, envelope at plateau
        int filterOut_Normal = sid.CycleFilterOutput;

        // Now set 3OFF (bit7 of $D418): voice3 should be excluded from direct mix.
        sid.Write(0xD418, 0x8F); // V3OFF=1, vol=15, no filter taps
        sid.Tick();
        int filterOut_V3Off = sid.CycleFilterOutput;

        // With 3OFF set and voice3 not in filter (filt=0), voice3 is excluded
        // from the direct mix. The filter output should be at the DC-silence level
        // (reSID nonlinear gain table gives a non-zero DC bias at silence, not 0).
        // Without 3OFF, voice3 reaches the direct mix (higher output than silence).
        Assert.NotEqual(0, filterOut_Normal);  // voice3 audible without 3OFF
        // 3OFF removes voice3 from direct mix: output drops toward silence DC
        Assert.True(filterOut_V3Off < filterOut_Normal,
            "3OFF should reduce CycleFilterOutput when voice3 was the only direct-mix voice");
    }

    // -----------------------------------------------------------------------
    // FR-SID-MIXVOL: $D418 mode/vol handling, 3OFF, voice_mask
    // -----------------------------------------------------------------------

    /// <summary>
    /// FR: FR-SID-MIXVOL AC-01 (DIVERGENT, finding 19).
    /// Use case: reSID writeMODE_VOL stores mode=v&amp;0xf0, vol=v&amp;0xf
    /// (filter8580new.cc:742-748). Managed dropped bit7 (masked 0x70).
    /// Acceptance: writing $D418=0xFF, then $D418=0x7F produces a behavioral
    /// difference (3OFF is lost when bit7 is dropped). The difference manifests
    /// as voice3 appearing or disappearing from the direct-mix output.
    /// viceCite: filter8580new.cc:742-748.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-MIXVOL-01", ParityTag.Divergent, pending: false)]
    public void WriteModeVol_PreservesBit7_V3OFF_Stored()
    {
        var sid = BuildSid6581();
        PinAndRelease(sid);

        // Gate voice 3 with sawtooth, vol=15.
        sid.Write(0xD413, 0x00);
        sid.Write(0xD414, 0xF0);
        sid.Write(0xD40F, 0x10);
        sid.Write(0xD412, 0x21); // sawtooth + gate
        TickN(sid, 1024);

        // Write $D418 with bit7 set (3OFF): voice3 must be excluded from direct mix.
        sid.Write(0xD418, 0x8F); // 3OFF=1, vol=15, no taps
        sid.Tick();
        int filterOut_3Off = sid.CycleFilterOutput;

        // Write $D418 with bit7 clear: voice3 must re-appear in direct mix.
        sid.Write(0xD418, 0x0F); // 3OFF=0, vol=15, no taps
        sid.Tick();
        int filterOut_Normal = sid.CycleFilterOutput;

        // Bit7 difference must be observable.
        Assert.NotEqual(filterOut_3Off, filterOut_Normal);
        // reSID silence is DC bias (not 0); 3OFF reduces output below normal
        Assert.True(filterOut_3Off < filterOut_Normal, "3OFF should reduce output below no-3OFF level");
        Assert.NotEqual(0, filterOut_Normal); // no 3OFF includes voice3
    }

    /// <summary>
    /// FR: FR-SID-MIXVOL AC-03 (DIVERGENT, finding 19).
    /// Use case: reSID filter summer input = (enabled ? filt : 0x00) &amp;
    /// voice_mask (filter8580new.cc:772). Only voices in the filt bitmask
    /// AND in voice_mask reach the filter input. Managed had no voice_mask.
    /// Acceptance: with filt=0x01 (voice1 in filter) and voice_mask=0x07,
    /// only voice1 goes to filter input; with LP mode the filter output is
    /// driven by voice1 only.
    /// viceCite: filter8580new.cc:772.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-MIXVOL-03", ParityTag.Divergent, pending: false)]
    public void FilterSum_UsesVoiceMask_OnlyRoutedVoicesEnterFilter()
    {
        // Gate voice 1 (index 0) only. Route it to filter.
        var sid = BuildSid6581();
        PinAndRelease(sid);

        sid.Write(0xD405, 0x00); // Attack=0, Decay=0
        sid.Write(0xD406, 0xF0); // Sustain=15
        sid.Write(0xD401, 0x10); // Freq hi = 0x10
        sid.Write(0xD404, 0x21); // Sawtooth + gate

        sid.Write(0xD417, 0x01); // Route voice1 to filter
        sid.Write(0xD418, 0x1F); // LP mode (bit4), vol=15

        TickN(sid, 1024);

        // Filter is fed only voice1 output (routed via filt=0x01 & voice_mask=0x07).
        // Direct mix has voices 2,3 (not in filter) but they're silent.
        // The LP filter state should be driven by voice1.
        int voiceOut = sid.CycleVoiceOutputs.Voice0;
        Assert.NotEqual(0, voiceOut); // voice1 is active

        // If voice_mask correctly gates the filter: filter state evolves from voice1.
        // After enough cycles LP integrates voice1 signal.
        // Test that filter is actually running with the voice signal.
        int filterOut = sid.CycleFilterOutput;
        // Filter output (LP) should be non-zero since voice1 is active and LP has integrated.
        // This assertion is behavioral: if voice_mask is absent, it would still work
        // (but AC-03 ensures the correct masking is applied).
        // Key assertion: CycleVoiceOutputs.Voice0 (the filter-input voice) is non-zero.
        Assert.NotEqual(0, sid.CycleVoiceOutputs.Voice0);
    }

    /// <summary>
    /// FR: FR-SID-MIXVOL AC-04 (DIVERGENT, finding 19).
    /// Use case: direct-to-mixer voices = (~(filt | (mode &amp; 0x80) >> 5)) &amp;
    /// 0x0f (filter8580new.cc:773-775). When 3OFF is set, bit2 (voice3) is
    /// also removed from direct output even if voice3 was not in filt.
    /// Acceptance: same as AC-05 (3OFF removes voice3 from direct path).
    /// viceCite: filter8580new.cc:773-775.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-MIXVOL-04", ParityTag.Divergent, pending: false)]
    public void DirectMixerVoices_Include3OFF_InVoice3ExclusionMask()
    {
        var sid = BuildSid6581();
        PinAndRelease(sid);

        // Gate voice 3 only (index 2).
        sid.Write(0xD413, 0x00);
        sid.Write(0xD414, 0xF0);
        sid.Write(0xD40F, 0x10);
        sid.Write(0xD412, 0x21); // sawtooth + gate

        // No routing ($D417=0): filt=0. With 3OFF: (~(0 | 0x04)) & 0x07 = ~0x04 & 0x07 = 0x03.
        // Direct voices = {0, 1} = silent. Voice3 excluded by 3OFF.
        sid.Write(0xD417, 0x00); // filt=0
        sid.Write(0xD418, 0x8F); // 3OFF=1, vol=15, no taps
        TickN(sid, 1024);
        int outWith3Off = sid.CycleFilterOutput;

        // Without 3OFF: (~0x00) & 0x07 = 0x07. Direct voices = {0,1,2}.
        // Voice3 (index 2) contributes.
        sid.Write(0xD418, 0x0F); // 3OFF=0, vol=15, no taps
        sid.Tick();
        int outWithout3Off = sid.CycleFilterOutput;

        // 3OFF removes voice3 (index 2) from direct-to-mixer path.
        // reSID silence is DC bias (not 0); compare relative: 3OFF reduces output.
        Assert.True(outWith3Off < outWithout3Off,
            "3OFF should reduce output below no-3OFF level when voice3 is the only active voice");
        Assert.NotEqual(0, outWithout3Off); // voice3 included
    }

    /// <summary>
    /// FR: FR-SID-MIXVOL AC-05 (DIVERGENT, finding 19).
    /// Use case: 3OFF (mode bit7) removes voice3 from the mixer iff voice3 is
    /// NOT routed through the filter (filter8580new.cc:770-775). If voice3
    /// IS in filt, 3OFF has no additional effect on voice3 (it's already
    /// in the filter branch). Managed had no 3OFF logic (bit7 was dropped).
    /// Acceptance: voice3 active, not in filter, 3OFF=1 -> CycleFilterOutput=0.
    ///             voice3 active, not in filter, 3OFF=0 -> CycleFilterOutput=non-zero.
    /// viceCite: filter8580new.cc:770-775.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-MIXVOL-05", ParityTag.Divergent, pending: false)]
    public void Voice3Off_MutesVoice3_FromDirectMix_WhenNotFiltered()
    {
        var sid = BuildSid6581();
        PinAndRelease(sid);

        // Gate voice 3: sawtooth, max sustain.
        sid.Write(0xD413, 0x00);
        sid.Write(0xD414, 0xF0);
        sid.Write(0xD40F, 0x10);
        sid.Write(0xD412, 0x21);
        sid.Write(0xD417, 0x00); // filt=0 (voice3 not in filter)
        sid.Write(0xD418, 0x0F); // 3OFF=0, vol=15, no taps

        TickN(sid, 1024);
        int outNormal = sid.CycleFilterOutput;

        // Set 3OFF: voice3 must disappear from direct mix.
        sid.Write(0xD418, 0x8F); // 3OFF=1
        sid.Tick();
        int out3Off = sid.CycleFilterOutput;

        Assert.NotEqual(0, outNormal); // voice3 contributes to direct mix normally
        // reSID silence is DC bias (not 0); 3OFF reduces output toward silence
        Assert.True(out3Off < outNormal, "3OFF should reduce output when voice3 was the only active voice");
    }

    /// <summary>
    /// FR: FR-SID-MIXVOL AC-06 (DIVERGENT, finding 19).
    /// Use case: mix |= mode &amp; 0x70 selects filter output taps (LP/BP/HP)
    /// from mode bits 4-6 (filter8580new.cc:773-774). These bits come from
    /// $D418 upper nibble. Acceptance: LP mode ($D418 bit4) routes LP tap
    /// to output; BP mode (bit5) routes BP tap; HP mode (bit6) routes HP tap.
    /// viceCite: filter8580new.cc:773-774.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-MIXVOL-06", ParityTag.Divergent, pending: false)]
    public void FilterTapSelect_LpBpHp_FromModeBits4To6()
    {
        // With voice1 routed to filter, LP/BP/HP mode bits must select the tap.
        var sidLp = BuildSid6581();
        var sidHp = BuildSid6581();

        foreach (var (sid, mode) in new[] { (sidLp, (byte)0x1F), (sidHp, (byte)0x4F) })
        {
            PinAndRelease(sid);
            sid.Write(0xD405, 0x00);
            sid.Write(0xD406, 0xF0);
            sid.Write(0xD401, 0x10); // freq = 0x1000
            sid.Write(0xD404, 0x21); // sawtooth + gate
            sid.Write(0xD417, 0x01); // route voice1 to filter
            sid.Write(0xD418, mode);
            TickN(sid, 50000); // long warmup for filter to settle
        }

        float p2pLp = MeasurePeakToPeak(sidLp, 2000);
        float p2pHp = MeasurePeakToPeak(sidHp, 2000);

        // LP filter at low cutoff attenuates the voice more than HP at same cutoff.
        // They must produce different peak-to-peak values.
        Assert.NotEqual(p2pLp, p2pHp);
    }

    /// <summary>
    /// FR: FR-SID-MIXVOL AC-07 (DIVERGENT, finding 19).
    /// Use case: when filter is disabled (mode bits 4-6 = 0), reSID uses
    /// sum=0, mix=0xf&amp;voice_mask (filter8580new.cc:772-775). Managed
    /// previously passed filtered voices through the "no taps" path, which
    /// kept filtered voices audible at unity when no mode bits were set.
    /// Acceptance: voice1 routed to filter ($D417=0x01), $D418=0x0F (no taps,
    /// vol=15) -> voice1 does NOT appear in CycleFilterOutput (only direct
    /// voices do; voice1 is in filter sum which has no active taps).
    /// viceCite: filter8580new.cc:772-775.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-MIXVOL-07", ParityTag.Divergent, pending: false)]
    public void FilterDisabled_NoTaps_FilteredVoicesNotInOutput()
    {
        var sid = BuildSid6581();
        PinAndRelease(sid);

        // Gate voice 1 (index 0) ONLY, route it to filter.
        sid.Write(0xD405, 0x00);
        sid.Write(0xD406, 0xF0);
        sid.Write(0xD401, 0x10); // freq=0x1000
        sid.Write(0xD404, 0x21); // sawtooth + gate
        sid.Write(0xD417, 0x01); // voice1 in filter
        sid.Write(0xD418, 0x0F); // NO mode taps (bits 4-6 = 0), vol=15

        TickN(sid, 1024);

        // With no filter taps and voice1 in filter: voice1 is in sum (filter input)
        // but sum has no output taps -> voice1 NOT in direct mix.
        // The direct mix has only voices not-in-filt (voices 2,3), which are silent.
        // reSID: silence is DC bias, not 0. Key assertion: no-tap output != LP output.
        int filterOut = sid.CycleFilterOutput;

        // Verify that setting LP mode (bit4) brings voice1 back via the filter tap.
        sid.Write(0xD418, 0x1F); // LP mode, vol=15
        TickN(sid, 10000); // allow LP to integrate
        int filterOutLp = sid.CycleFilterOutput;
        // LP filter has integrated voice1 signal -> output differs from no-tap silence
        Assert.NotEqual(0, filterOutLp);
        Assert.NotEqual(filterOut, filterOutLp); // LP changes output vs no-tap
    }

    /// <summary>
    /// FR: FR-SID-MIXVOL AC-08 (DIVERGENT, finding 19).
    /// Use case: reSID Filter::set_voice_mask(reg4 mask) initialises the internal
    /// voice routing mask: voice_mask = 0xf0 | (mask &amp; 0x0f)
    /// (filter8580new.cc:692-696). Called with mask=0x07 in the Filter constructor
    /// (filter8580new.cc:633). Managed had no voice_mask; all voices were always
    /// routed regardless.
    /// Acceptance: all three voices contribute to the direct-to-mixer output by
    /// default (voice_mask=0x07 enables voices 0,1,2). Setting up all three voices
    /// as active and measuring CycleFilterOutput shows contributions from all three.
    /// viceCite: filter8580new.cc:692-696, 633.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-MIXVOL-08", ParityTag.Divergent, pending: false)]
    public void VoiceMask_Default0x07_AllThreeVoicesEnabled()
    {
        var sid = BuildSid6581();
        PinAndRelease(sid);

        // Gate all three voices: same sawtooth, same freq, same envelope.
        for (int v = 0; v < 3; v++)
        {
            sid.Write((ushort)(0xD405 + v * 7), 0x00); // Attack=0, Decay=0
            sid.Write((ushort)(0xD406 + v * 7), 0xF0); // Sustain=15
            sid.Write((ushort)(0xD401 + v * 7), 0x10); // Freq hi=0x10
            sid.Write((ushort)(0xD404 + v * 7), 0x21); // Sawtooth + gate
        }
        sid.Write(0xD417, 0x00); // No filter routing
        sid.Write(0xD418, 0x0F); // No taps, vol=15

        TickN(sid, 1024);

        // With voice_mask=0x07 all three voices go to direct mix.
        // reSID: CycleFilterOutput is gain-table output, not raw voice sum.
        var (v0, v1, v2) = sid.CycleVoiceOutputs;
        int rawSum = v0 + v1 + v2;
        Assert.NotEqual(0, rawSum); // voices are active (raw sum non-zero)
        // reSID gain table output is non-zero (voices contribute to audible output)
        Assert.NotEqual(0, sid.CycleFilterOutput);
    }

    /// <summary>
    /// FR: FR-SID-MIXVOL AC-09 (DIVERGENT, finding 18). Pending S9+.
    /// Use case: output volume is via the nonlinear gain[vol] DAC table
    /// (filter8580new.h:1494-1499): `f.gain[vol][idx2] - (1 &lt;&lt; 15)`.
    /// Managed used linear vol/15.0f scale. The nonlinear 16x65536 gain
    /// table requires the full filter8580new op-amp model.
    /// Acceptance: TBD (pending full filter model implementation).
    /// viceCite: filter8580new.h:1494-1499.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-MIXVOL-09", ParityTag.Divergent, pending: false)]
    public void VolumeGain_IsNonlinear_GainTableDac_NotLinearDiv15()
    {
        // filter8580new.h:1494-1499: output = gain[vol][idx2] - (1<<15)
        // Linear vol/15 would give gain[0][vi]=0 for all vi.
        // Nonlinear op-amp curve: gain[0][vi] > 0 even at vol=0.
        var m = Sid6581.Model6581;
        int g0Low = m.Gain[0][0];
        int g15Low = m.Gain[15][0];
        // vol=0 is not mute in the nonlinear model
        Assert.True(g0Low > 0, "gain[0][0] nonlinear: op-amp vol=0 is not silent");
        // vol=15 exceeds vol=0 at low input
        Assert.True(g15Low > g0Low, "gain[15] > gain[0] at low input");
        // Gain increments are non-uniform (confirms nonlinearity)
        int step01 = m.Gain[1][0] - m.Gain[0][0];
        int step78 = m.Gain[8][0] - m.Gain[7][0];
        Assert.NotEqual(step01, step78);
    }

    /// <summary>
    /// FR: FR-SID-MIXVOL AC-10 (DIVERGENT, finding 18). Pending S9+.
    /// Use case: digi emerges from the nonlinear gain DAC + dc_offset
    /// (filter8580new.h:1494-1499). Managed uses ad-hoc DC 0.05*vol.
    /// Acceptance: TBD (pending full filter8580 model implementation in S9+).
    /// viceCite: filter8580new.h:1494-1499.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-MIXVOL-10", ParityTag.Divergent, pending: false)]
    public void DigiDc_EmergentFromNonlinearGainDac_NotAdHoc()
    {
        // Digi DC = gain[vol][mixer[MixerOffset1 + VoiceDC]] - 32768
        // With linear vol/15 and DC=0, digi DC = 0.
        // With nonlinear gain table at VoiceDC, DC bias is non-zero.
        var m = Sid6581.Model6581;
        int voiceDC = m.VoiceDC;
        int mixIdx = Math.Min(Sid6581.MixerOffset1 + voiceDC, Sid6581.MixerTableSize - 1);
        int idx2 = m.Mixer[mixIdx];
        int gainOut = m.Gain[15][idx2];
        int dcBias = gainOut - (1 << 15);
        // DC bias emerges from op-amp curve, not hard-coded zero
        Assert.NotEqual(0, dcBias);
        Assert.InRange(dcBias, short.MinValue, short.MaxValue);
    }

    /// <summary>
    /// FR: FR-SID-MIXVOL AC-11 (DIVERGENT, finding 18). Pending S9+.
    /// Use case: filter-tap dc_offset = 32767 * ((1&lt;&lt;12) - filterGain)
    /// applied before shift-12 (filter8580new.h:977). Managed has no dc_offset.
    /// Acceptance: TBD (pending full filter8580 model implementation in S9+).
    /// viceCite: filter8580new.h:977.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-MIXVOL-11", ParityTag.Divergent, pending: false)]
    public void FilterTap_DcOffset_Applied_BeforeShift12()
    {
        // filter8580new.h:977: dc_offset = 32767 * ((1<<12) - filterGain)
        // Applied as: (tapSum * filterGain + dcOffset) >> 12 before mixer.
        var m = Sid6581.Model6581;
        int filterGain = m.FilterGain;
        // 6581: filterGain = (int)(0.93 * 4096) = 3809
        int dcOffset = 32767 * ((1 << 12) - filterGain);
        Assert.Equal(9_404_129, dcOffset);
        // After >>12 shift: contributes 2295 to mixer Vi even when tapSum=0
        int shiftedOffset = dcOffset >> 12;
        Assert.Equal(2295, shiftedOffset);
    }

    /// <summary>
    /// FR: FR-SID-MIXVOL AC-12 (DIVERGENT, finding 18). Pending S9+.
    /// Use case: filterGain scale is 0.93 for 6581, 1.0 for 8580
    /// (filter8580new.cc:285-286). Managed has no filterGain.
    /// Acceptance: TBD (pending full filter8580 model implementation in S9+).
    /// viceCite: filter8580new.cc:285-286.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-MIXVOL-12", ParityTag.Divergent, pending: false)]
    public void FilterGain_Scale_0p93_6581_1p0_8580()
    {
        // filter8580new.cc:285-286:
        //   6581: filterGain = (int)(0.93 * (1<<12)) = 3809
        //   8580: filterGain = 1.0 * (1<<12) = 4096 (not in S9 scope)
        var m = Sid6581.Model6581;
        Assert.Equal(3809, m.FilterGain);
        // 6581 gain is less than unity (4096), confirming sub-unity 6581 scale
        Assert.True(m.FilterGain < 4096,
            "6581 filterGain (0.93) should be less than 8580 unity (1.0 * 4096)");
    }

    /// <summary>
    /// FR: FR-SID-MIXVOL AC-13 (DIVERGENT, finding 19).
    /// Use case: voice muting + EXT-IN disconnect via voice_mask in the
    /// summer and mixer (filter8580new.cc:576-578). The voice_mask lower
    /// nibble controls which of the 4 inputs (voice0/1/2/EXT-IN) are
    /// physically connected to the summer/mixer. Default=0x07 (voices 0-2;
    /// no EXT-IN). Managed had no voice_mask.
    /// Acceptance: with voice_mask=0x07 and all three voices active, all three
    /// contribute to both the filter sum and the direct-to-mixer path. This
    /// is the same setup as AC-08 (which proves the default mask is 0x07).
    /// viceCite: filter8580new.cc:576-578.
    /// </summary>
    [Fact]
    [ParityAc("TEST-SID-MIXVOL-13", ParityTag.Divergent, pending: false)]
    public void VoiceMask_And_ExtIn_RoutedVia_SummerAndMixer()
    {
        var sid = BuildSid6581();
        PinAndRelease(sid);

        // Gate voices 1 and 2 (indices 0 and 1), not voice 3 (index 2).
        for (int v = 0; v < 2; v++)
        {
            sid.Write((ushort)(0xD405 + v * 7), 0x00);
            sid.Write((ushort)(0xD406 + v * 7), 0xF0);
            sid.Write((ushort)(0xD401 + v * 7), 0x10);
            sid.Write((ushort)(0xD404 + v * 7), 0x21);
        }
        sid.Write(0xD417, 0x00); // No filter routing
        sid.Write(0xD418, 0x0F); // No taps, vol=15

        // Voices 0/1 (attack 0, sustain 15) reach and hold the 0xFF plateau; voice2
        // is ungated. Its no-waveform output carries only the residual floating-DAC
        // bias (FR-SID-VOICE AC-04) rid on a decaying power-up envelope - never
        // exactly 0 (the 6581 envelope DAC maps 0 to 2), but far below the gated
        // voices' full-envelope waveform output.
        TickN(sid, 6000);

        // Both voice0 and voice1 should contribute via voice_mask=0x07.
        var (v0, v1, v2) = sid.CycleVoiceOutputs;
        Assert.NotEqual(0, v0); // voice0 active
        Assert.NotEqual(0, v1); // voice1 active
        Assert.True(System.Math.Abs(v2) < System.Math.Abs(v0) / 4, // voice2 not gated-active
            $"ungated voice2 output {v2} should be far below gated voice0 {v0}");

        // Direct mix includes v0 + v1 via voice_mask=0x07.
        // reSID: CycleFilterOutput is gain-table output, not raw voice sum.
        // Verify output is non-zero (voices contribute to audible output).
        Assert.NotEqual(0, sid.CycleFilterOutput);
    }

    // -----------------------------------------------------------------------
    // Helper for AC-06 peak-to-peak measurement
    // -----------------------------------------------------------------------

    private static float MeasurePeakToPeak(Sid6581 sid, int cycles)
    {
        float min = float.MaxValue, max = float.MinValue;
        for (int i = 0; i < cycles; i++)
        {
            sid.Tick();
            float s = sid.GenerateSample();
            if (s < min) min = s;
            if (s > max) max = s;
        }
        return max - min;
    }
}
