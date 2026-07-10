using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Audio;

/// <summary>
/// MOS 8580 SID emulator. Shares the reSID two-integrator op-amp filter
/// with the 6581 (one code path branched on the model) but binds the 8580
/// model tables and the 8580 integrator (solve_integrate_8580). Wave/envelope
/// DACs and combined-waveform ROMs use the 8580 die rows.
/// PLAN-VICEPARITY-001 S11 (FR-SID-FILTER-8580).
/// </summary>
public partial class Sid8580 : Sid6581
{
    public Sid8580(IBus bus) : base(bus) { }

    public Sid8580(IBus bus, IAudioBackend? audioBackend) : base(bus, audioBackend) { }

    // ----------------------------------------------------------------
    // reSID filter model selection (PLAN-VICEPARITY-001 S11)
    // ----------------------------------------------------------------

    /// <inheritdoc />
    private protected override ResidFilterModel FilterModel => Model8580.Value;

    /// <inheritdoc />
    protected override bool IsMos8580Filter => true;

    /// <inheritdoc />
    private protected override void SetW0() => SetW0_8580();

    /// <summary>
    /// 8580 output amplify scaleFactor = 5 (sid.cc:121). S12 wires it into the
    /// amplify/clip seam; declared here for FR-SID-8580 AC-02.
    /// </summary>
    protected override int OutputScaleFactor => 5;

    /// <summary>
    /// 8580 data-bus fade TTL = 0xa2000 cycles (reSID sid.cc:119,
    /// databus_ttl = sid_model == MOS8580 ? 0xa2000 : 0x1d00).
    /// PLAN-VICEPARITY-001 S11.
    /// </summary>
    protected override int DataBusTtl => 0xA2000;

    // ----------------------------------------------------------------
    // 8580 die DAC rows (already built in the base; retarget the envelope)
    // ----------------------------------------------------------------

    /// <inheritdoc />
    /// PLAN-VICEPARITY-001 S11 (FR-SID-8580 / FR-SID-WAVE-DACRES): route the
    /// 8580 envelope through model_dac row 1 (2R/R = 2.00, terminated,
    /// envelope.cc:167-168) instead of the prior identity table.
    protected override ReadOnlySpan<ushort> EnvelopeDacTable => EnvelopeDac8580;

    /// <summary>
    /// 8580 waveform DAC: 2R/R = 2.00, terminated (dac.cc:167-168).
    /// FR-SID-WAVE-DACRES / PLAN-VICEPARITY-001 S7.
    /// </summary>
    protected override ReadOnlySpan<ushort> WaveDacTable => WaveDac8580Static;

    // ----------------------------------------------------------------
    // 8580 waveform behavior (unchanged from S7/S8)
    // ----------------------------------------------------------------

    /// <inheritdoc />
    /// PLAN-VICEPARITY-001 S8 (FR-SID-VOICE AC-03): MOS 8580 wave_zero in
    /// 12-bit domain = 0x9e0 (voice.cc:97).
    protected override int WaveZeroLevel => 0x9E0;

    /// <summary>
    /// FR-SID-OSC3ENV3 AC-05 [PLAN-VICEPARITY-001 S2]: the 8580 die serves
    /// the tri/saw component of OSC3 one cycle late through tri_saw_pipeline
    /// (wave.h:475-482) and uses the 8580 noise+pulse combination transform.
    /// </summary>
    protected override bool IsMos8580Wave => true;

    /// <summary>
    /// 8580 combined waveform ROM tables from wave8580_*.h (reSID).
    /// FR-SID-WAVE-COMBINED / PLAN-VICEPARITY-001 S7.
    /// </summary>
    protected override int WaveTable12(int waveform, int ix)
    {
        int tri = (((ix & 0x800) != 0 ? ~ix : ix) & 0x7FF) << 1;   // wave.cc:96
        int saw = ix & 0xFFF;                                      // wave.cc:97
        return (waveform & 0x7) switch
        {
            0 => 0xFFF,
            1 => tri,
            2 => saw,
            3 => SidWaveTables.Wave8580_ST[ix],
            4 => 0xFFF,
            5 => SidWaveTables.Wave8580_PT[ix],
            6 => SidWaveTables.Wave8580_PS[ix],
            _ => SidWaveTables.Wave8580_PST[ix],
        };
    }
}
