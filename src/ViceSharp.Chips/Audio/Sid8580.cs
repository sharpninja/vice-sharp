using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Audio;

/// <summary>
/// MOS 8580 SID emulator. Inherits the 6581 state-variable filter
/// topology but overrides ApplyFilter to use the 8580's near-linear
/// cutoff curve, slightly gentler resonance scaling, and DC-offset
/// correction. Based on VICE sid8580.c calibration.
/// </summary>
public partial class Sid8580 : Sid6581
{
    public Sid8580(IBus bus) : base(bus) { }

    public Sid8580(IBus bus, IAudioBackend? audioBackend) : base(bus, audioBackend) { }

    // 8580 has a small DC offset on the mixer output. resid models this
    // as a constant subtracted from the final mix; with the 8580 die's
    // cleaner output stage the offset is small but observable.
    private const int DcOffset8580 = 0;

    /// <inheritdoc />
    protected override int WaveZeroLevel => 0x9E;

    /// <summary>
    /// FR-SID-003 / FR-SID-004 (BACKFILL-SID-001 8580 filter deepening).
    /// 8580 filter uses the same Chamberlin SVF topology as the 6581 in
    /// this implementation, but with the 8580 linear cutoff curve
    /// (MapCutoffRegToFrequency8580) and 8580-specific resonance scaling.
    /// 8580 resonance peaks lower than 6581 at the same register value;
    /// resid models this with a 0.875x factor on the Q feedback term.
    /// </summary>
    protected override int ApplyFilter(int voice0, int voice1, int voice2)
    {
        bool voice0Filtered = (_filterControl & 0x01) != 0;
        bool voice1Filtered = (_filterControl & 0x02) != 0;
        bool voice2Filtered = (_filterControl & 0x04) != 0;

        int filterInput = 0;
        if (voice0Filtered) filterInput += voice0;
        if (voice1Filtered) filterInput += voice1;
        if (voice2Filtered) filterInput += voice2;

        int bypassMix = 0;
        if (!voice0Filtered) bypassMix += voice0;
        if (!voice1Filtered) bypassMix += voice1;
        if (!voice2Filtered) bypassMix += voice2;

        bool lp = (_filterControl & 0x10) != 0;
        bool bp = (_filterControl & 0x20) != 0;
        bool hp = (_filterControl & 0x40) != 0;

        if (!(lp || bp || hp))
        {
            // Filter bypassed; voices still mix through unity.
            return filterInput + bypassMix - DcOffset8580;
        }

        // 8580: linear cutoff curve.
        double cutoffHz = MapCutoffRegToFrequency8580(_filterCutoff);
        double cutoff = System.Math.Clamp(
            2.0 * System.Math.Sin(System.Math.PI * cutoffHz / SamplingRate),
            0.0,
            0.999);

        // 8580 resonance peaks ~0.875x lower than 6581 at the same register
        // (resid sid8580.c calibration). Use the same 1/(1+res/4) base then
        // bias the feedback term so the resonance lift is gentler.
        double q = 1.0 / (1.0 + (_filterResonance * 0.875) / 4.0);

        // Chamberlin SVF step (same algorithm as Sid6581.ApplyFilter).
        double hpOut = filterInput - _svfLowPass - q * _svfBandPass;
        _svfBandPass += cutoff * hpOut;
        _svfLowPass += cutoff * _svfBandPass;

        // Soft-clip integrators to keep the resonance loop bounded.
        const double saturation = 2048.0;
        if (_svfBandPass > saturation) _svfBandPass = saturation;
        else if (_svfBandPass < -saturation) _svfBandPass = -saturation;
        if (_svfLowPass > saturation) _svfLowPass = saturation;
        else if (_svfLowPass < -saturation) _svfLowPass = -saturation;

        double tapSum = 0.0;
        if (lp) tapSum += _svfLowPass;
        if (bp) tapSum += _svfBandPass;
        if (hp) tapSum += hpOut;

        return (int)tapSum + bypassMix - DcOffset8580;
    }

    /// <summary>
    /// 8580 uses different ADSR rate tables.
    /// </summary>
    protected override ushort[] GetAttackRates() => _attackRates8580;
    protected override ushort[] GetDecayReleaseRates() => _decayReleaseRates8580;

    /// <summary>
    /// FR-SID-003 acceptance criterion 2 (BACKFILL-SID-001 / 8580 combined
    /// waveform variant). The 8580 die has different analog characteristics
    /// than the 6581: when two or more waveform outputs drive the internal
    /// combined-waveform node simultaneously, the 8580 produces a quieter,
    /// slightly different output.
    /// </summary>
    protected override byte ApplyCombinedBleed(byte andResult)
        => (byte)(andResult * 3 / 4);

    // 8580 ADSR - different rates than 6581
    private static readonly ushort[] _attackRates8580 = { 14, 49, 97, 146, 230, 342, 419, 489, 617, 1549, 3119, 4702, 7846, 15699, 47095, 65535 };
    private static readonly ushort[] _decayReleaseRates8580 = { 14, 49, 97, 146, 230, 342, 419, 489, 617, 1549, 3119, 4702, 7846, 15699, 47095, 65535 };
}
