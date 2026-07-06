using System;
using System.Runtime.CompilerServices;

namespace ViceSharp.Chips.Audio;

/// <summary>
/// reSID 6581 filter model: field-for-field port of
/// filter8580new.cc:244-625 and filter8580new.h:684-1875.
/// Replaces the Chamberlin state-variable filter with reSID's
/// two-integrator op-amp EKV/VCR/capacitor model.
/// PLAN-VICEPARITY-001 S9:
///   FR-SID-FILTER-6581, FR-SID-CUTOFFDAC, FR-SID-FILTER-CLOCK,
///   FR-SID-MIXVOL AC-09..12.
/// </summary>
public partial class Sid6581
{
    // ----------------------------------------------------------------
    // Summer and mixer table layout constants
    // template summer_offset / mixer_offset from filter8580new.h:497-525
    // summer_offset<i> = summer_offset<i-1> + ((2+i-1)<<16)
    // ----------------------------------------------------------------
    internal const int SummerOffset0    = 0;
    internal const int SummerOffset1    = SummerOffset0 + (2 << 16);    // 131072
    internal const int SummerOffset2    = SummerOffset1 + (3 << 16);    // 327680
    internal const int SummerOffset3    = SummerOffset2 + (4 << 16);    // 589824
    internal const int SummerOffset4    = SummerOffset3 + (5 << 16);    // 917504
    internal const int SummerTableSize  = SummerOffset4 + (6 << 16);    // 1310720

    // mixer_offset<0>=0, mixer_offset<1>=1,
    // mixer_offset<i>=mixer_offset<i-1>+(i-1)<<16  for i>=2
    internal const int MixerOffset0   = 0;
    internal const int MixerOffset1   = 1;
    internal const int MixerOffset2   = MixerOffset1 + (1 << 16);       // 65537
    internal const int MixerOffset3   = MixerOffset2 + (2 << 16);       // 196609
    internal const int MixerOffset4   = MixerOffset3 + (3 << 16);       // 393217
    internal const int MixerOffset5   = MixerOffset4 + (4 << 16);       // 655361
    internal const int MixerOffset6   = MixerOffset5 + (5 << 16);       // 983041
    internal const int MixerOffset7   = MixerOffset6 + (6 << 16);       // 1376257
    internal const int MixerTableSize = MixerOffset7 + (7 << 16);       // 1835009

    // External filter coefficients (extfilt.cc:41-42, at 1MHz).
    // w0lp_1_s7  = int(1e-6/(1e-6+1e4*1e-9)*(1<<7)+0.5)  = 12
    // w0hp_1_s17 = int(1e-6/(1e-6+1e3*1e-5)*(1<<17)+0.5) = 13
    internal const int ExtFiltW0lp1s7  = 12;
    internal const int ExtFiltW0hp1s17 = 13;

    // ----------------------------------------------------------------
    // Static model tables (built once at class-load time)
    // ----------------------------------------------------------------

    /// <summary>
    /// Exposes the 6581 model tables for test assertions.
    /// PLAN-VICEPARITY-001 S9 internal seam.
    /// </summary>
    internal static readonly ResidFilter6581Model Model6581 = BuildResidModel6581();

    // ----------------------------------------------------------------
    // Per-instance filter state (reSID Filter / ExternalFilter fields)
    // filter8580new.h:580-609
    // ----------------------------------------------------------------

    // Filter integrator outputs
    private int _vhp;
    private int _vbp;
    private int _vlp;
    // Integrator internal state (x = opamp input, vc = capacitor charge)
    private int _vbpX, _vbpVc;
    private int _vlpX, _vlpVc;
    // Vddt_Vw_2 = (kVddt-Vw)^2/2, set by SetW0_6581 on fc register write
    private int _vddt_Vw_2;
    // Prescaled voice inputs (set by ClockResidFilter6581 each cycle)
    private int _rv1, _rv2, _rv3;
    // External filter state (extfilt.h:72-75)
    private int _extFiltVlp;
    private int _extFiltVhp;


    // ----------------------------------------------------------------
    // reSID dispatch flag
    // ----------------------------------------------------------------

    /// <summary>
    /// True for the MOS 6581 path: reSID two-integrator op-amp filter.
    /// Sid8580 overrides to false to keep the Chamberlin SVF path.
    /// PLAN-VICEPARITY-001 S9 (FR-SID-FILTER-6581 / FR-SID-FILTER-CLOCK).
    /// </summary>
    protected virtual bool UsesReSidFilter => true;

    // ----------------------------------------------------------------
    // reSID Filter::clock() for the 6581 path
    // filter8580new.h:684-779
    // ----------------------------------------------------------------

    /// <summary>
    /// reSID Filter::clock(v1, v2, v3) for the 6581 path.
    /// Prescales three voice outputs, routes them through the summer
    /// switch, and advances Vlp/Vbp/Vhp via solve_integrate_6581.
    /// filter8580new.h:684-779.
    /// PLAN-VICEPARITY-001 S9 (FR-SID-FILTER-6581 AC-23..26).
    /// </summary>
    private void ClockResidFilter6581(int rawVoice0, int rawVoice1, int rawVoice2)
    {
        var m = Model6581;

        // Voice pre-scaling: v = ((raw * voice_scale_s14 + rnd.getNoise()) >> 18)
        // + voice_DC (filter8580new.h:701-703). reSID adds a per-draw dither from
        // its Randomnoise buffer, filled at construction with rand() % (1<<19).
        // That dither is NON-DETERMINISTIC (it varies per SID instance and run,
        // since VICE never seeds rand()), so it cannot be bit-reproduced and it
        // violates the ViceSharp determinism invariant. ViceSharp therefore omits
        // it (deterministic filter), and the parity oracle zeroes reSID's dither
        // buffer to match (native filter8580new.h Randomnoise ctor). The
        // deterministic filter model below is bit-exact against that oracle.
        // PLAN-VICEPARITY-001 S9 (FR-SID-FILTER-6581).
        _rv1 = ((rawVoice0 * m.VoiceScaleS14) >> 18) + m.VoiceDC;
        _rv2 = ((rawVoice1 * m.VoiceScaleS14) >> 18) + m.VoiceDC;
        _rv3 = ((rawVoice2 * m.VoiceScaleS14) >> 18) + m.VoiceDC;

        // Sum routing: 'sum' = filt & voice_mask (filter8580new.cc:772)
        // filter8580new.h:696-761: 16-case switch over lower nibble
        int sumMask = _filterControl & 0x0F & _voiceMask;
        int Vi;
        int offset;

        switch (sumMask)
        {
            case 0x0: Vi = 0;                   offset = SummerOffset0; break;
            case 0x1: Vi = _rv1;                offset = SummerOffset1; break;
            case 0x2: Vi = _rv2;                offset = SummerOffset1; break;
            case 0x3: Vi = _rv2 + _rv1;         offset = SummerOffset2; break;
            case 0x4: Vi = _rv3;                offset = SummerOffset1; break;
            case 0x5: Vi = _rv3 + _rv1;         offset = SummerOffset2; break;
            case 0x6: Vi = _rv3 + _rv2;         offset = SummerOffset2; break;
            case 0x7: Vi = _rv3 + _rv2 + _rv1; offset = SummerOffset3; break;
            // Cases 0x8-0xF include EXT-IN (ve=0 in managed C64)
            case 0x8: Vi = 0;                   offset = SummerOffset1; break;
            case 0x9: Vi = _rv1;                offset = SummerOffset2; break;
            case 0xA: Vi = _rv2;                offset = SummerOffset2; break;
            case 0xB: Vi = _rv2 + _rv1;         offset = SummerOffset3; break;
            case 0xC: Vi = _rv3;                offset = SummerOffset2; break;
            case 0xD: Vi = _rv3 + _rv1;         offset = SummerOffset3; break;
            case 0xE: Vi = _rv3 + _rv2;         offset = SummerOffset3; break;
            default:  Vi = _rv3 + _rv2 + _rv1; offset = SummerOffset4; break;
        }

        // 6581 two-integrator filter (filter8580new.h:764-778)
        _vlp = SolveIntegrate6581(1, _vbp, ref _vlpX, ref _vlpVc, m);
        _vbp = SolveIntegrate6581(1, _vhp, ref _vbpX, ref _vbpVc, m);

        // reSID summer index: offset + resonance[res][Vbp] + Vlp + Vi, using RAW
        // Vbp/Vlp (filter8580new.h:776). Vbp is asserted in [0, 1<<16) so the
        // resonance array index is clamped only for memory safety; Vlp is added
        // raw (clamping it here diverges from reSID on the double-integrated tap).
        int vbpC = _vbp;
        if (vbpC < 0) vbpC = 0; else if (vbpC > 65535) vbpC = 65535;

        int resonanceOut = m.Resonance[_filterResonance][vbpC];
        int sumIdx = offset + resonanceOut + _vlp + Vi;
        if (sumIdx < 0) sumIdx = 0;
        else if (sumIdx >= SummerTableSize) sumIdx = SummerTableSize - 1;
        _vhp = m.Summer[sumIdx];
    }

    // ----------------------------------------------------------------
    // reSID Filter::output() for the 6581 path
    // filter8580new.h:939-1499
    // ----------------------------------------------------------------

    /// <summary>
    /// reSID Filter::output() for the 6581 path.
    /// Active filter taps are summed then scaled by filterGain + dc_offset.
    /// Each individual tap AND each direct voice count as 1 input for the
    /// mixer_offset (matching the Perl-generated 128-case switch).
    /// filter8580new.h:939-1499.
    /// PLAN-VICEPARITY-001 S9 (FR-SID-FILTER-6581 AC-28..29).
    /// </summary>
    private short ComputeResidFilterOutput6581()
    {
        var m = Model6581;
        int filterGain = m.FilterGain;
        // dc_offset = 32767 * (4096 - filterGain) (filter8580new.h:977)
        int dcOffset = 32767 * ((1 << 12) - filterGain);
        int vol = _volume;

        // Compute mix from filter8580new.cc:768-776 with voice_mask=0xF7
        // bits 0-3: direct voice mask
        // bits 4-6: filter tap mask (bit4=Vlp, bit5=Vbp, bit6=Vhp)
        byte filt     = (byte)(_filterControl & 0x0F);
        byte mode     = (byte)(_filterControl & 0xF0);
        byte v3offBit = (byte)((mode & 0x80) >> 5); // 0x04 when V3OFF
        byte directMask = (byte)((~(filt | v3offBit)) & 0x0F & _voiceMask);
        // tap selects: mode bits 4-6 = LP/BP/HP (matching reSID mix bits 4-6)
        byte tapBits = (byte)((mode >> 4) & 0x07);

        int Vi = 0;
        int nDirect = 0;
        int nTaps = 0;

        // Direct voice contributions (each = 1 mixer input)
        if ((directMask & 0x01) != 0) { Vi += _rv1; nDirect++; }
        if ((directMask & 0x02) != 0) { Vi += _rv2; nDirect++; }
        if ((directMask & 0x04) != 0) { Vi += _rv3; nDirect++; }
        // EXT-IN bit3 is excluded by voice_mask 0xF7

        // Filter tap contributions: each tap = 1 mixer input,
        // but ALL active taps are summed BEFORE applying gain.
        // (See Perl script: @sumFilt = list of active taps;
        //  offset = mixer_offset<@sumVoice + @sumFilt>)
        if (tapBits != 0)
        {
            int tapSum = 0;
            if ((tapBits & 0x01) != 0) { tapSum += _vlp; nTaps++; }
            if ((tapBits & 0x02) != 0) { tapSum += _vbp; nTaps++; }
            if ((tapBits & 0x04) != 0) { tapSum += _vhp; nTaps++; }
            Vi += (tapSum * filterGain + dcOffset) >> 12;
        }

        int mixOffset = GetMixerOffset(nDirect + nTaps);
        int idx1 = mixOffset + Vi;
        if (idx1 < 0) idx1 = 0;
        else if (idx1 >= MixerTableSize) idx1 = MixerTableSize - 1;
        int idx2 = m.Mixer[idx1];
        return (short)(m.Gain[vol][idx2] - (1 << 15));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetMixerOffset(int n) => n switch
    {
        0 => MixerOffset0,
        1 => MixerOffset1,
        2 => MixerOffset2,
        3 => MixerOffset3,
        4 => MixerOffset4,
        5 => MixerOffset5,
        6 => MixerOffset6,
        7 => MixerOffset7,
        _ => MixerOffset0,
    };

    // ----------------------------------------------------------------
    // External filter (extfilt.h:96-116, extfilt.h:159-163)
    // ----------------------------------------------------------------

    /// <summary>
    /// reSID ExternalFilter::clock(Vi) single-cycle step.
    /// extfilt.h:107-115.
    /// PLAN-VICEPARITY-001 S9 (FR-SID-FILTER-CLOCK AC-02).
    /// </summary>
    private void ClockResidExtFilter6581(short vi)
    {
        int dVlp = ExtFiltW0lp1s7 * unchecked((int)(((uint)vi << 11) - (uint)_extFiltVlp)) >> 7;
        int dVhp = ExtFiltW0hp1s17 * (_extFiltVlp - _extFiltVhp) >> 17;
        _extFiltVlp += dVlp;
        _extFiltVhp += dVhp;
    }

    /// <summary>
    /// reSID ExternalFilter::output(). Returns (Vlp - Vhp) >> 11.
    /// extfilt.h:159-163.
    /// PLAN-VICEPARITY-001 S9 (FR-SID-FILTER-CLOCK AC-02).
    /// </summary>
    private int ResidExtFilterOutput6581() => (_extFiltVlp - _extFiltVhp) >> 11;

    // ----------------------------------------------------------------
    // Register-write hooks
    // ----------------------------------------------------------------

    /// <summary>
    /// reSID Filter::set_w0() 6581 path (filter8580new.cc:751-758):
    ///   Vw = Vw_bias + f0_dac[fc]; Vw_bias = 0 for 6581.
    ///   Vddt_Vw_2 = unsigned(kVddt-Vw) * unsigned(kVddt-Vw) >> 1
    /// Called on $D415/$D416 writes when UsesReSidFilter is true.
    /// PLAN-VICEPARITY-001 S9 (FR-SID-FILTER-6581 AC-20).
    /// </summary>
    // VICE default 6581 filter bias: RESID_6581_FILTER_BIAS_DEFAULT = 500 (mV)
    // (native src/sid/sid.h). VICE resid.cc always applies it via
    // SID::adjust_filter_bias(bias_mV/1000), which sets
    // Vw_bias = int((bias_mV/1000) * model_filter[0].vo_N16)
    // (filter8580new.cc:654-657). The managed chip must apply the same bias or
    // every cutoff (and thus the whole VCR integrator rate) diverges from the
    // oracle. PLAN-VICEPARITY-001 S9 (FR-SID-FILTER-6581 AC-20 / FR-SID-CUTOFFDAC).
    private const double FilterBiasVolts6581 = 500.0 / 1000.0;

    internal void SetW0_6581()
    {
        var m = Model6581;
        int vwBias = (int)(FilterBiasVolts6581 * m.VoN16);
        int fc = _filterCutoff & 0x7FF; // 11-bit cutoff register value
        int Vw = vwBias + m.F0Dac[fc];  // reSID: Vw = Vw_bias + f0_dac[fc]
        uint diff = (uint)(m.KVddt - Vw);
        _vddt_Vw_2 = (int)((diff * diff) >> 1);
    }

    /// <summary>
    /// reSID Filter::set_sum_mix() (filter8580new.cc:768-776).
    /// Sum/mix masks are recomputed inline each cycle from _filterControl
    /// and _voiceMask; this hook is a no-op but marks the call site.
    /// PLAN-VICEPARITY-001 S9 (FR-SID-FILTER-6581 AC-21).
    /// </summary>
    internal void SetSumMix_6581() { }

    /// <summary>
    /// Reset reSID filter state (filter8580new.cc:706-716, extfilt.cc:58-63).
    /// Zeroes all integrator state and re-applies set_w0 for fc=0.
    /// Called by Reset() when UsesReSidFilter is true.
    /// PLAN-VICEPARITY-001 S9 (FR-SID-FILTER-6581 AC-22).
    /// </summary>
    internal void ResetFilter6581()
    {
        _vhp = 0; _vbp = 0; _vlp = 0;
        _vbpX = 0; _vbpVc = 0;
        _vlpX = 0; _vlpVc = 0;
        _rv1 = 0; _rv2 = 0; _rv3 = 0;
        _extFiltVlp = 0;
        _extFiltVhp = 0;
        SetW0_6581(); // apply f0_dac[0]
    }

    // ----------------------------------------------------------------
    // Internal seams for test access
    // ----------------------------------------------------------------
    internal int FilterVhp => _vhp;
    internal int FilterVbp => _vbp;
    internal int FilterVlp => _vlp;
    internal int FilterVddt_Vw_2 => _vddt_Vw_2;
    internal int ExtFiltVlpState => _extFiltVlp;
    internal int ExtFiltVhpState => _extFiltVhp;
    internal int ResidPrescaledV1 => _rv1;
    internal int ResidPrescaledV2 => _rv2;
    internal int ResidPrescaledV3 => _rv3;

    // ----------------------------------------------------------------
    // solve_integrate_6581 (filter8580new.h:1827-1875)
    // ----------------------------------------------------------------

    /// <summary>
    /// reSID solve_integrate_6581: one EKV/VCR integrator step.
    /// filter8580new.h:1827-1875.
    /// PLAN-VICEPARITY-001 S9 (FR-SID-FILTER-6581 AC-26).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int SolveIntegrate6581(int dt, int vi, ref int vx, ref int vc,
        ResidFilter6581Model m)
    {
        int kVddt = m.KVddt;

        // Snake voltages (triode mode): filter8580new.h:1837-1839
        uint Vgst  = (uint)(kVddt - vx);
        uint Vgdt  = (uint)(kVddt - vi);
        uint Vgdt2 = Vgdt * Vgdt;

        // Snake current (filter8580new.h:1842)
        // n_snake * (int(Vgst*Vgst - Vgdt2) >> 15)
        int nISnake = m.NSnake * ((int)(Vgst * Vgst - Vgdt2) >> 15);

        // VCR gate voltage (filter8580new.h:1846)
        // kVg = vcr_kVg[(Vddt_Vw_2 + (Vgdt_2>>1)) >> 16]
        uint vcrIdx = ((uint)_vddt_Vw_2 + (Vgdt2 >> 1)) >> 16;
        if (vcrIdx > 65535u) vcrIdx = 65535u;
        int kVg = m.VcrKVg[(int)vcrIdx];

        // VCR voltages for EKV lookup (filter8580new.h:1849-1850)
        int Vgs = kVg - vx + (1 << 15);
        int Vgd = kVg - vi + (1 << 15);
        if (Vgs < 0) Vgs = 0; else if (Vgs > 65535) Vgs = 65535;
        if (Vgd < 0) Vgd = 0; else if (Vgd > 65535) Vgd = 65535;

        // VCR EKV current (filter8580new.h:1853)
        // int(unsigned(vcr_n_Ids_term[Vgs] - vcr_n_Ids_term[Vgd]) << 15)
        int nIVcr = (int)(unchecked((uint)(m.VcrNIdsTerm[Vgs] - m.VcrNIdsTerm[Vgd])) << 15);

        // Capacitor charge update (filter8580new.h:1856)
        vc -= (nISnake + nIVcr) * dt;

        // vx = g(vc) via opamp_rev (filter8580new.h:1869-1871)
        int opampIdx = (vc >> 15) + (1 << 15);
        if (opampIdx < 0) opampIdx = 0;
        else if (opampIdx > 65535) opampIdx = 65535;
        vx = m.OpampRev[opampIdx];

        // Return vo = vx + (vc >> 14) (filter8580new.h:1874)
        return vx + (vc >> 14);
    }

    // ----------------------------------------------------------------
    // BuildResidModel6581: static table construction
    // Ports filter8580new.cc:244-625 for model m=0 (6581)
    // ----------------------------------------------------------------

    /// <summary>
    /// Build the complete reSID 6581 filter model tables.
    /// Called once at class-load time (~200-500ms computation).
    /// filter8580new.cc:244-625, spline.h, dac.cc.
    /// PLAN-VICEPARITY-001 S9.
    /// </summary>
    private static ResidFilter6581Model BuildResidModel6581()
    {
        // -----------------------------------------------------------
        // MOS 6581 model_filter_init[0] (filter8580new.cc:179-202)
        // -----------------------------------------------------------
        const double voice_voltage_range = 1.5;
        const double voice_DC_voltage    = 5.075;
        const double Vdd          = 12.18;
        const double Vth          = 1.31;
        const double Ut           = 26.0e-3;
        const double k            = 1.0;
        const double uCox         = 20e-6;
        const double WL_vcr       = 9.0;
        const double WL_snake     = 1.0 / 115.0;
        const double dac_zero     = 6.65;
        const double dac_scale    = 2.63;
        const double dac_2R_div_R = 2.20;
        const int    dac_bits     = 11;

        // opamp_voltage_6581: 35-point measured curve (filter8580new.cc:40-76)
        // First entry (0.81,10.31) and last entry (10.31,0.81) are repeated
        // to anchor the spline at AK and BK (see spline.h notes).
        // Each row = [Vin, Vout] of the inverting op-amp
        var opVin  = new double[]
        {  0.81, 0.81, 2.40, 2.60, 2.70, 2.80, 2.90, 3.00, 3.10, 3.20,
           3.30, 3.50, 3.70, 4.00, 4.40, 4.54, 4.60, 4.80, 4.90, 4.95,
           5.00, 5.05, 5.10, 5.20, 5.40, 5.60, 5.80, 6.00, 6.40, 7.00,
           7.50, 8.50,10.00,10.31,10.31 };
        var opVout = new double[]
        { 10.31,10.31,10.31,10.30,10.29,10.26,10.17,10.04, 9.83, 9.58,
           9.32, 8.69, 8.00, 6.89, 5.21, 4.54, 4.19, 3.00, 2.30, 2.03,
           1.88, 1.77, 1.69, 1.58, 1.44, 1.33, 1.26, 1.21, 1.12, 1.02,
           0.97, 0.89, 0.81, 0.81, 0.81 };
        int opSize = opVin.Length; // 35

        // -----------------------------------------------------------
        // Normalization constants (filter8580new.cc:269-299)
        // -----------------------------------------------------------
        double vmin   = opVin[0];   // 0.81
        double vmax   = Math.Max(opVout[0], k * (Vdd - Vth)); // max(10.31, 10.87)
        double denorm = vmax - vmin;
        double norm   = 1.0 / denorm;

        double N16 = norm * ((1u << 16) - 1);
        double N30 = norm * ((1u << 30) - 1);
        double N31 = norm * ((1u << 31) - 1);
        double vo_N16 = N16;

        int filterGain    = (int)(0.93 * (1 << 12));          // 3809
        double N14        = norm * (1u << 14);
        int voiceScaleS14 = (int)(N14 * voice_voltage_range);
        int voiceDC       = (int)(N16 * (voice_DC_voltage - vmin));
        int kVddt_int     = (int)(N16 * (k * (Vdd - Vth) - vmin) + 0.5);

        // n_snake (filter8580new.cc:473-474)
        double C = 470e-12;
        double tmp_n_param = denorm * (1 << 13) * ((uCox / 2.0) * 1e-6 / C);
        int nSnake = (int)(WL_snake * tmp_n_param + 0.5);

        // -----------------------------------------------------------
        // Build scaled opamp points (filter8580new.cc:310-337)
        // x = N16*(Vout-Vin)/2 + 32768;  y = N31*(Vin-vmin)
        // reversed order (last C++ row -> first C# entry)
        // -----------------------------------------------------------
        var scX = new double[opSize];
        var scY = new double[opSize];
        for (int i = 0; i < opSize; i++)
        {
            int ri = opSize - 1 - i;
            scX[ri] = N16 * (opVout[i] - opVin[i]) / 2.0 + (1 << 15);
            scY[ri] = N31 * (opVin[i] - vmin);
        }
        if (scX[opSize - 1] > 65535.0)
            scX[opSize - 1] = scX[opSize - 2] = 65535.0;

        // Spline interpolation -> voltages[0..65535]
        var voltages = new uint[1 << 16];
        SplineInterpolate(scX, scY, opSize, voltages, 1.0);

        // -----------------------------------------------------------
        // Build opamp table (filter8580new.cc:341-370)
        // vx = f >> 15;  dvx = (f - fp) >> 4
        // -----------------------------------------------------------
        int ak = (int)(scX[0] + 0.5);
        int bk = (int)(scX[opSize - 1] + 0.5);
        var opampVx  = new int[1 << 16];
        var opampDvx = new int[1 << 16];

        uint fv = voltages[ak];
        for (int j = ak; j < bk; j++)
        {
            uint fp = fv;
            fv = voltages[j];
            int df = (int)(fv - fp);
            opampVx[j]  = (int)(fv > (0xFFFFu << 15) ? 0xFFFFu : fv >> 15);
            opampDvx[j] = df >> (15 - 11);
        }
        opampDvx[ak] = opampDvx[ak + 1]; // fix first entry

        // opamp_rev (filter8580new.cc:436-438)
        var opampRev = new ushort[1 << 16];
        for (int i = 0; i < (1 << 16); i++)
            opampRev[i] = (ushort)opampVx[i];

        int vcMax = (int)(N30 * (opVout[0] - opVin[0]));
        int vcMin = (int)(N30 * (opVout[opSize - 1] - opVin[opSize - 1]));

        // -----------------------------------------------------------
        // Summer table (filter8580new.cc:382-392)
        // 5 configs with 2-6 input resistors
        // -----------------------------------------------------------
        var summer = new ushort[SummerTableSize];
        {
            int sumOff = 0;
            for (int kk = 0; kk < 5; kk++)
            {
                int idiv  = 2 + kk;
                double nId = (double)idiv;
                int size  = idiv << 16;
                int x = ak;
                for (int vi = 0; vi < size; vi++)
                    summer[sumOff + vi] = (ushort)SolveGainD(opampVx, opampDvx, nId, vi / idiv, ref x, kVddt_int, ak, bk);
                sumOff += size;
            }
        }

        // -----------------------------------------------------------
        // Mixer table (filter8580new.cc:400-418)
        // 8 configs; divider=6.0 for 6581
        // -----------------------------------------------------------
        var mixer = new ushort[MixerTableSize];
        {
            const double divider = 6.0;
            int mixOff = 0;
            int size = 1; // mixer_offset<0> size = 1
            for (int l = 0; l < 8; l++)
            {
                int idiv  = l;
                double nId = (double)(idiv << 3) / divider;
                if (idiv == 0) idiv = 1; // avoid div-by-zero; nId=0 gives correct result
                int x = ak;
                for (int vi = 0; vi < size; vi++)
                    mixer[mixOff + vi] = (ushort)SolveGainD(opampVx, opampDvx, nId, vi / idiv, ref x, kVddt_int, ak, bk);
                mixOff += size;
                size = (l + 1) << 16;
            }
        }

        // -----------------------------------------------------------
        // Gain table [16][65536] divider=12 (filter8580new.cc:425-432)
        // -----------------------------------------------------------
        var gain = new ushort[16][];
        for (int n8 = 0; n8 < 16; n8++)
        {
            gain[n8] = new ushort[1 << 16];
            double n = (double)n8 / 12.0;
            int x = ak;
            for (int vi = 0; vi < (1 << 16); vi++)
                gain[n8][vi] = (ushort)SolveGainD(opampVx, opampDvx, n, vi, ref x, kVddt_int, ak, bk);
        }

        // -----------------------------------------------------------
        // Resonance table [16][65536] (filter8580new.cc:462-468)
        // n = (~n8 & 0xf) / 8.0
        // -----------------------------------------------------------
        var resonance = new ushort[16][];
        for (int n8 = 0; n8 < 16; n8++)
        {
            resonance[n8] = new ushort[1 << 16];
            double n = (double)((~n8) & 0xF) / 8.0;
            int x = ak;
            for (int vi = 0; vi < (1 << 16); vi++)
                resonance[n8][vi] = (ushort)SolveGainD(opampVx, opampDvx, n, vi, ref x, kVddt_int, ak, bk);
        }

        // -----------------------------------------------------------
        // f0_dac[2048] (filter8580new.cc:477-480)
        // -----------------------------------------------------------
        var f0dacRaw = BuildEnvelopeDacTable(dac_bits, dac_2R_div_R, term: false);
        var f0dac = new ushort[1 << dac_bits];
        for (int n = 0; n < (1 << dac_bits); n++)
        {
            double sc = N16 * (dac_zero + f0dacRaw[n] * dac_scale / (1 << dac_bits) - vmin) + 0.5;
            f0dac[n] = (ushort)(sc < 0 ? 0 : sc > 65535 ? 65535 : (ushort)sc);
        }

        // -----------------------------------------------------------
        // vcr_kVg[65536] (filter8580new.cc:487-499)
        // kVddt_raw = N16*k*(Vdd-Vth);  vmin_N16 = vmin*N16
        // -----------------------------------------------------------
        var vcrKVg = new ushort[1 << 16];
        {
            double kVddt_raw = N16 * k * (Vdd - Vth);
            double vmin_N16  = vmin * N16;
            for (int i = 0; i < (1 << 16); i++)
            {
                double Vg  = kVddt_raw - Math.Sqrt((double)i * (1 << 16));
                double val = k * Vg - vmin_N16 + 0.5;
                vcrKVg[i] = (ushort)(val < 0 ? 0 : val > 65535 ? 65535 : (ushort)val);
            }
        }

        // -----------------------------------------------------------
        // vcr_n_Ids_term[65536] EKV (filter8580new.cc:509-523)
        // -----------------------------------------------------------
        var vcrNIdsTerm = new ushort[1 << 16];
        {
            double kVt  = k * Vth;
            double Is   = ((2.0 * uCox * Ut * Ut) / k) * WL_vcr;
            double N15  = N16 / 2.0;
            double n_Is = N15 * 1e-6 / C * Is;
            for (int i = 0; i < (1 << 16); i++)
            {
                int    kVg_Vx   = i - (1 << 15);
                double logTerm  = Log1p(Math.Exp((kVg_Vx / N16 - kVt) / (2.0 * Ut)));
                double val      = n_Is * logTerm * logTerm;
                vcrNIdsTerm[i]  = (ushort)(val > 65535 ? 65535 : (ushort)val);
            }
        }

        return new ResidFilter6581Model
        {
            FilterGain    = filterGain,
            VoiceScaleS14 = voiceScaleS14,
            VoiceDC       = voiceDC,
            KVddt         = kVddt_int,
            AK            = ak,
            BK            = bk,
            VcMin         = vcMin,
            VcMax         = vcMax,
            NSnake        = nSnake,
            VoN16         = vo_N16,
            OpampRev      = opampRev,
            Summer        = summer,
            Mixer         = mixer,
            Gain          = gain,
            Resonance     = resonance,
            F0Dac         = f0dac,
            VcrKVg        = vcrKVg,
            VcrNIdsTerm   = vcrNIdsTerm,
        };
    }

    // ----------------------------------------------------------------
    // SolveGainD: Newton-Raphson + Dekker bisection
    // filter8580new.h:1632-1704
    // ----------------------------------------------------------------

    /// <summary>
    /// reSID solve_gain_d: Newton-Raphson + Dekker bisection.
    /// The ref x is a warm-start that persists across table iterations.
    /// filter8580new.h:1632-1704.
    /// PLAN-VICEPARITY-001 S9 (FR-SID-FILTER-6581 AC-16).
    /// </summary>
    private static int SolveGainD(
        int[] opVx, int[] opDvx,
        double n, int vi, ref int x, int b, int ak, int bk)
    {
        double a    = n + 1.0;
        double b_vi = b > vi ? (double)(b - vi) : 0.0;
        double c    = n * (b_vi * b_vi);
        int lak = ak, lbk = bk;

        for (;;)
        {
            int xk  = x;
            int vx  = opVx[x];
            int dvx = opDvx[x];

            int vo = vx + (x << 1) - (1 << 16);
            if (vo > (1 << 16) - 1) vo = (1 << 16) - 1;
            else if (vo < 0) vo = 0;

            double b_vx = b > vx ? (double)(b - vx) : 0.0;
            double b_vo = b > vo ? (double)(b - vo) : 0.0;

            double f  = a * (b_vx * b_vx) - c - (b_vo * b_vo);
            double df = 2.0 * (b_vo - a * b_vx) * (double)dvx;

            if (df != 0.0)
                x -= (int)((double)(1 << 11) * f / df);

            if (x == xk)
                return vo;

            if (f < 0.0) lak = xk;
            else         lbk = xk;

            if (x <= lak || x >= lbk)
            {
                x = (lak + lbk) >> 1;
                if (x == lak)
                    return vo;
            }
        }
    }

    // ----------------------------------------------------------------
    // Spline interpolation (port of spline.h)
    // ----------------------------------------------------------------

    /// <summary>
    /// Catmull-Rom cubic spline through the opamp voltage points.
    /// Port of spline.h interpolate().
    /// PLAN-VICEPARITY-001 S9 (FR-SID-FILTER-6581 AC-07).
    /// </summary>
    private static void SplineInterpolate(
        double[] scX, double[] scY, int nPoints,
        uint[] voltages, double res)
    {
        for (int i = 0; i + 3 < nPoints; i++)
        {
            double x0 = scX[i],     y0 = scY[i];
            double x1 = scX[i + 1], y1 = scY[i + 1];
            double x2 = scX[i + 2], y2 = scY[i + 2];
            double x3 = scX[i + 3], y3 = scY[i + 3];

            if (x1 == x2) continue;

            double k1, k2;
            if (x0 == x1 && x2 == x3)
            {
                k1 = k2 = (y2 - y1) / (x2 - x1);
            }
            else if (x0 == x1)
            {
                k2 = (y3 - y1) / (x3 - x1);
                k1 = (3.0 * (y2 - y1) / (x2 - x1) - k2) / 2.0;
            }
            else if (x2 == x3)
            {
                k1 = (y2 - y0) / (x2 - x0);
                k2 = (3.0 * (y2 - y1) / (x2 - x1) - k1) / 2.0;
            }
            else
            {
                k1 = (y2 - y0) / (x2 - x0);
                k2 = (y3 - y1) / (x3 - x1);
            }

            SplineForwardDifference(x1, y1, x2, y2, k1, k2, voltages, res);
        }
    }

    /// <summary>
    /// Forward-difference cubic polynomial step.
    /// Port of spline.h interpolate_forward_difference().
    /// </summary>
    private static void SplineForwardDifference(
        double x1, double y1, double x2, double y2,
        double k1, double k2, uint[] voltages, double res)
    {
        double dx = x2 - x1, dy = y2 - y1;
        double a = ((k1 + k2) - 2.0 * dy / dx) / (dx * dx);
        double b = ((k2 - k1) / dx - 3.0 * (x1 + x2) * a) / 2.0;
        double c = k1 - (3.0 * x1 * a + 2.0 * b) * x1;
        double d = y1 - ((x1 * a + b) * x1 + c) * x1;

        double yy  = ((a * x1 + b) * x1 + c) * x1 + d;
        double dy2 = (3.0 * a * (x1 + res) + 2.0 * b) * x1 * res
                   + ((a * res + b) * res + c) * res;
        double d2y = (6.0 * a * (x1 + res) + 2.0 * b) * res * res;
        double d3y = 6.0 * a * res * res * res;

        for (double xf = x1; xf <= x2; xf += res)
        {
            int ix = (int)xf;
            if ((uint)ix < (uint)voltages.Length)
                voltages[ix] = yy < 0.0 ? 0u : (uint)(yy + 0.5);
            yy += dy2; dy2 += d2y; d2y += d3y;
        }
    }

    // log1p precise workaround (matching reSID filter8580new.h)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double Log1p(double x)
        => Math.Log(1.0 + x) - (((1.0 + x) - 1.0) - x) / (1.0 + x);

    // ----------------------------------------------------------------
    // ResidFilter6581Model: table container
    // ----------------------------------------------------------------

    /// <summary>
    /// Static 6581 filter model tables.
    /// Internal for parity-test assertions.
    /// PLAN-VICEPARITY-001 S9.
    /// </summary>
    internal sealed class ResidFilter6581Model
    {
        public int FilterGain    { get; init; }  // (int)(0.93*4096) = 3809
        public int VoiceScaleS14 { get; init; }  // ~2442
        public int VoiceDC       { get; init; }  // ~27788
        public int KVddt         { get; init; }
        public int AK            { get; init; }
        public int BK            { get; init; }
        public int VcMin         { get; init; }
        public int VcMax         { get; init; }
        public int NSnake        { get; init; }  // ~15
        public double VoN16      { get; init; }
        public required ushort[]   OpampRev    { get; init; }
        public required ushort[]   Summer      { get; init; }
        public required ushort[]   Mixer       { get; init; }
        public required ushort[][] Gain        { get; init; }
        public required ushort[][] Resonance   { get; init; }
        public required ushort[]   F0Dac       { get; init; }
        public required ushort[]   VcrKVg      { get; init; }
        public required ushort[]   VcrNIdsTerm { get; init; }
    }
}
