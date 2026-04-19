using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Audio;

/// <summary>
/// MOS 8580 SID emulator - inherits from 6581, overrides filter differences.
/// Based on VICE sid8580.c logic.
/// </summary>
public sealed class Sid8580 : Sid6581
{
    public Sid8580(IBus bus) : base(bus) { }

    // 8580 DC offset for filter
    private int _dcOffset = 0;

    /// <summary>
    /// 8580 filter - different DC characteristics and simpler than 6581
    /// </summary>
    protected override int ApplyFilter(int voice0, int voice1, int voice2)
    {
        bool voice0Filtered = (_filterControl & 0x01) != 0;
        bool voice1Filtered = (_filterControl & 0x02) != 0;
        bool voice2Filtered = (_filterControl & 0x04) != 0;

        int input = 0;
        if (!voice0Filtered) input += voice0;
        if (!voice1Filtered) input += voice1;
        if (!voice2Filtered) input += voice2;

        bool lp = (_filterControl & 0x10) != 0;
        bool bp = (_filterControl & 0x20) != 0;
        bool hp = (_filterControl & 0x40) != 0;

        // 8580 has different cutoff scaling (0.85x)
        double cutoff = _filterCutoff / 2047.0 * 0.85;
        cutoff = Math.Clamp(cutoff, 0.0, 1.0);
        
        double resonance = _filterResonance / 15.0 * 0.9;

        if (lp || bp || hp)
        {
            // 8580 uses cascaded single-pole filters instead of state variable
            int output = input;
            
            if (lp) output = (int)(output * (1.0 - cutoff * 0.8));
            if (hp) output = (int)(output * cutoff * 0.3);
            
            int filtered = 0;
            if (voice0Filtered) filtered += voice0;
            if (voice1Filtered) filtered += voice1;
            if (voice2Filtered) filtered += voice2;
            
            // 8580 DC offset correction
            return output + filtered - _dcOffset;
        }
        else
        {
            return voice0 + voice1 + voice2 - _dcOffset;
        }
    }

    /// <summary>
    /// 8580 uses different ADSR rate tables
    /// </summary>
    protected override ushort[] GetAttackRates() => _attackRates8580;
    protected override ushort[] GetDecayReleaseRates() => _decayReleaseRates8580;

    // 8580 ADSR - different rates than 6581
    private static readonly ushort[] _attackRates8580 = { 14, 49, 97, 146, 230, 342, 419, 489, 617, 1549, 3119, 4702, 7846, 15699, 47095, 65535 };
    private static readonly ushort[] _decayReleaseRates8580 = { 14, 49, 97, 146, 230, 342, 419, 489, 617, 1549, 3119, 4702, 7846, 15699, 47095, 65535 };
}
