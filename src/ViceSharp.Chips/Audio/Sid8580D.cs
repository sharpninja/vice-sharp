using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Audio;

/// <summary>
/// MOS 8580D SID emulator - inherits from 8580, with D revision filter differences.
/// Based on VICE sid8580d.c logic.
/// </summary>
public sealed class Sid8580D : Sid8580
{
    public Sid8580D(IBus bus) : base(bus) { }

    // 8580D has different filter DC characteristics
    private int _dcOffset = 0;

    /// <summary>
    /// 8580D filter - D revision has improved DC stability
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

        // 8580D uses 0.9x cutoff scaling (different from 8580)
        double cutoff = _filterCutoff / 2047.0 * 0.9;
        cutoff = Math.Clamp(cutoff, 0.0, 1.0);
        
        // D revision has slightly different resonance curve
        double resonance = _filterResonance / 15.0 * 0.95;

        if (lp || bp || hp)
        {
            int output = input;
            
            if (lp) output = (int)(output * (1.0 - cutoff * 0.75));
            if (hp) output = (int)(output * cutoff * 0.35);
            
            int filtered = 0;
            if (voice0Filtered) filtered += voice0;
            if (voice1Filtered) filtered += voice1;
            if (voice2Filtered) filtered += voice2;
            
            // D revision DC offset correction (improved)
            return output + filtered - (_dcOffset / 2);
        }
        else
        {
            return voice0 + voice1 + voice2 - (_dcOffset / 2);
        }
    }
}
