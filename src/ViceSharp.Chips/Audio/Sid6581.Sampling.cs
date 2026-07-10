// SPDX-License-Identifier: GPL-2.0-or-later
// Derivative of VICE reSID (native/vice/vice/src/resid/sid.cc, sid.h).
// Managed statement-for-statement port of the reSID sampling front-end:
// I0, SID::set_sampling_parameters, SID::clock dispatch, clock_fast,
// clock_interpolate, and clock_resample. PLAN-VICEPARITY-001 S13.
// Managed transcription produced under adversarial multi-agent verification
// (fixed-point/rounding/indexing/signed-shift lenses).
using System;
using System.Diagnostics;

namespace ViceSharp.Chips.Audio;

/// <summary>
/// reSID sampling front-end (I0 Bessel kernel, sampling-parameter setup with
/// FIR-table construction, and the fast / interpolate / resample clocking loops)
/// for the managed <see cref="Sid6581"/> chip. Ported from
/// native/vice/vice/src/resid/sid.cc and sid.h.
/// </summary>
public partial class Sid6581
{
    // ---- reSID resampling constants (sid.h :140-158) ------------------------
    private const int FirN = 125;              // FIR_N
    private const int FirRes = 285;            // FIR_RES
    private const int FirShift = 15;           // FIR_SHIFT
    private const int RingSize = 1 << 14;      // RINGSIZE
    private const int RingMask = RingSize - 1; // RINGMASK
    private const int FixpShift = 16;          // FIXP_SHIFT (16.16 fixed point)
    private const int FixpMask = 0xFFFF;       // FIXP_MASK

    // ---- reSID sampling state (sid.h :161-177) ------------------------------
    // reSID cycle_count is a 32-bit signed int (siddefs.h:70). cycles_per_sample
    // (~1.46M for 985248/44100) fits in int, so shifts/masks match the native
    // build exactly. These stay int, never long.
    private double _clockFrequency;        // clock_frequency
    private int _cyclesPerSample;          // cycles_per_sample
    private int _sampleOffset;             // sample_offset
    private int _sampleIndex;              // sample_index
    private short _samplePrev;             // sample_prev
    private short _sampleNow;              // sample_now
    private int _firN;                     // fir_N
    private int _firRes;                   // fir_RES
    private double _firBeta;               // fir_beta
    private double _firFCyclesPerSample;   // fir_f_cycles_per_sample
    private double _firFilterScale;        // fir_filter_scale
    private short[]? _ring;                // "sample" ring, length RingSize*2
    private short[]? _fir;                 // FIR tables, length _firN*_firRes

    // ---- Parity-test seams (PLAN-VICEPARITY-001 S13) ------------------------
    internal const int FirNConst = FirN;
    internal const int FirResConst = FirRes;
    internal const int FirShiftConst = FirShift;
    internal const int RingSizeConst = RingSize;
    internal const int FixpShiftConst = FixpShift;
    internal int CyclesPerSampleSeam => _cyclesPerSample;
    internal int FirLengthSeam => _firN;
    internal int FirResolutionSeam => _firRes;
    internal short[] FirTableSeam => _fir!;

    /// <summary>
    /// Computes the 0th order modified Bessel function of the first kind.
    /// Port of reSID SID::I0 (native/vice/vice/src/resid/sid.cc :542-560).
    /// </summary>
    private static double I0(double x)
    {
        // Max error acceptable in I0.
        const double I0e = 1e-6;

        double sum, u, halfx, temp;
        int n;

        sum = u = n = 1;
        halfx = x / 2.0;

        do
        {
            temp = halfx / n++;
            u *= temp * temp;
            sum += u;
        }
        while (u >= I0e * sum);

        return sum;
    }

    /// <summary>
    /// Sets the SID sampling parameters. Statement-for-statement port of reSID
    /// SID::set_sampling_parameters (native/vice/vice/src/resid/sid.cc :585-719).
    /// Returns false on the same constraint failures as reSID, preallocates and
    /// zeros the sample ring, and builds the FIR tables for the resample methods
    /// (zero per-sample allocation invariant). SAMPLE_RESAMPLE_FASTMEM is not
    /// ported (throws <see cref="NotSupportedException"/>).
    /// </summary>
    public bool SetSamplingParameters(
        double clockFreq,
        SidSamplingMethod method,
        double sampleFreq,
        double passFreq = -1.0,
        double filterScale = 0.97)
    {
        // SAMPLE_RESAMPLE_FASTMEM is intentionally not ported (no AC requires it).
        if (method == SidSamplingMethod.ResampleFastMem)
        {
            throw new NotSupportedException(
                "SidSamplingMethod.ResampleFastMem (reSID SAMPLE_RESAMPLE_FASTMEM) is not supported.");
        }

        // Check resampling constraints.
        if (method == SidSamplingMethod.Resample)
        {
            // Check whether the sample ring buffer would overfill.
            if ((int)((double)FirN * clockFreq / sampleFreq) >= RingSize)
            {
                return false;
            }

            // The default passband limit is 0.9*sample_freq/2 for sample
            // frequencies below ~ 44.1kHz, and 20kHz for higher sample frequencies.
            if (passFreq < 0)
            {
                passFreq = 20000;
                if (2 * passFreq / sampleFreq >= 0.9)
                {
                    passFreq = 0.9 * sampleFreq / 2;
                }
            }
            // Check whether the FIR table would overfill.
            else if (passFreq > 0.9 * sampleFreq / 2)
            {
                return false;
            }

            // The filter scaling is only included to avoid clipping, so keep it sane.
            if (filterScale < 0.9 || filterScale > 1.0)
            {
                return false;
            }
        }

        _clockFrequency = clockFreq;
        SamplingMethod = method;

        // Hazard 2: keep the +0.5 then truncating cast; 1 << FixpShift stays int.
        _cyclesPerSample =
            (int)(clockFreq / sampleFreq * (1 << FixpShift) + 0.5);

        _sampleOffset = 0;
        _samplePrev = 0;
        _sampleNow = 0;

        // FIR initialization is only necessary for resampling.
        if (method != SidSamplingMethod.Resample)
        {
            _ring = null;
            _fir = null;
            return true;
        }

        // Allocate sample buffer.
        if (_ring is null)
        {
            _ring = new short[RingSize * 2];
        }
        // Clear sample buffer.
        for (int j = 0; j < RingSize * 2; j++)
        {
            _ring[j] = 0;
        }
        _sampleIndex = 0;

        const double pi = 3.1415926535897932385;

        // 16 bits -> -96dB stopband attenuation.
        double A = -20 * Math.Log10(1.0 / (1 << 16));
        // A fraction of the bandwidth is allocated to the transition band.
        double dw = (1 - 2 * passFreq / sampleFreq) * pi * 2;
        // The cutoff frequency is midway through the transition band (nyquist).
        double wc = pi;

        // For calculation of beta and N see the kaiserord reference in the
        // MATLAB Signal Processing Toolbox.
        double beta = 0.1102 * (A - 8.7);
        double I0beta = I0(beta);

        // The filter order will maximally be 124 with the current constraints.
        // The filter order equals the number of zero crossings (even number).
        int N = (int)((A - 7.95) / (2.285 * dw) + 0.5);
        N += N & 1;

        double f_samples_per_cycle = sampleFreq / clockFreq;
        double f_cycles_per_sample = clockFreq / sampleFreq;

        // The filter length equals the filter order + 1 (odd number).
        int fir_N_new = (int)(N * f_cycles_per_sample) + 1;
        fir_N_new |= 1;

        // Check whether the sample ring buffer would overflow.
        Debug.Assert(fir_N_new < RingSize);

        // Clamp the filter table resolution to 2^n, making the fixed point
        // sample_offset a whole multiple of the filter table resolution.
        int res = FirRes; // SAMPLE_RESAMPLE only (FASTMEM excluded above).
        // reSID uses log(2.0f) here, which binds to the float overload (logf),
        // computing ln2 at single precision. Reproduce that with MathF.Log(2f)
        // so ceil() boundary crossings match the native build bit-for-bit.
        int nRes = (int)Math.Ceiling(
            Math.Log(res / f_cycles_per_sample) / MathF.Log(2f));
        int fir_RES_new = 1 << nRes;

        // Determine if we can reuse the earlier cached FIR table (reSID cache
        // reuse early-return; exact double == comparisons preserved).
        if (_fir != null
            && fir_RES_new == _firRes
            && fir_N_new == _firN
            && beta == _firBeta
            && f_cycles_per_sample == _firFCyclesPerSample
            && _firFilterScale == filterScale)
        {
            return true;
        }
        _firRes = fir_RES_new;
        _firN = fir_N_new;
        _firBeta = beta;
        _firFCyclesPerSample = f_cycles_per_sample;
        _firFilterScale = filterScale;

        // Allocate memory for FIR tables.
        _fir = new short[_firN * _firRes];

        // Calculate fir_RES FIR tables for linear interpolation.
        for (int i = 0; i < _firRes; i++)
        {
            int fir_offset = i * _firN + _firN / 2;
            double j_offset = (double)i / _firRes;
            // Calculate FIR table: the sinc function weighted by the Kaiser window.
            for (int j = -_firN / 2; j <= _firN / 2; j++)
            {
                double jx = j - j_offset;
                double wt = wc * jx / f_cycles_per_sample;
                double temp = jx / (_firN / 2);
                double Kaiser =
                    Math.Abs(temp) <= 1 ? I0(beta * Math.Sqrt(1 - temp * temp)) / I0beta : 0;
                double sincwt =
                    Math.Abs(wt) >= 1e-6 ? Math.Sin(wt) / wt : 1;
                double val =
                    (1 << FirShift) * filterScale * f_samples_per_cycle * wc / pi * sincwt * Kaiser;
                // Hazard 1: C round() is round-half-away-from-zero, NOT banker's.
                _fir[fir_offset + j] = (short)Math.Round(val, MidpointRounding.AwayFromZero);
            }
        }

        return true;
    }

    /// <summary>
    /// Managed mirror of reSID <c>SID::clock(cycle_count& delta_t, short* buf, int n, int interleave)</c>
    /// (native/vice/vice/src/resid/sid.cc :849-862) with <c>n = buf.Length</c> and
    /// <c>interleave = 1</c>. Dispatches on <see cref="SamplingMethod"/>. Returns the number
    /// of samples written; <paramref name="deltaT"/> is written back with the unconsumed
    /// cycle remainder (reSID's <c>cycle_count&</c> out-parameter).
    /// </summary>
    public int ClockBuffered(ref int deltaT, short[] buf)
    {
        switch (SamplingMethod)
        {
            default:
            case SidSamplingMethod.Fast:
                return ClockFast(ref deltaT, buf);
            case SidSamplingMethod.Interpolate:
                return ClockInterpolate(ref deltaT, buf);
            case SidSamplingMethod.Resample:
            case SidSamplingMethod.ResampleFastMem:
                return ClockResample(ref deltaT, buf);
        }
    }

    /// <summary>
    /// SID clocking with audio sampling - delta clocking picking the nearest sample.
    /// Port of reSID SID::clock_fast (native/vice/vice/src/resid/sid.cc :868-892).
    /// </summary>
    private int ClockFast(ref int deltaT, short[] buf)
    {
        int n = buf.Length;
        int s;

        for (s = 0; s < n; s++)
        {
            int nextSampleOffset = _sampleOffset + _cyclesPerSample + (1 << (FixpShift - 1));
            int deltaTSample = nextSampleOffset >> FixpShift;

            if (deltaTSample > deltaT)
            {
                deltaTSample = deltaT;
            }

            // reSID clock_fast advances via the batched SID::clock(delta_t)
            // (sid.cc:880), not N single cycles: voice outputs held over the
            // window, oscillators/filter/extfilt sub-stepped. This is the
            // SAMPLE_FAST approximation and is bit-exact vs the buffered oracle.
            ClockBatched(deltaTSample);

            if ((deltaT -= deltaTSample) == 0)
            {
                _sampleOffset -= deltaTSample << FixpShift;
                break;
            }

            _sampleOffset = (nextSampleOffset & FixpMask) - (1 << (FixpShift - 1));
            buf[s] = AmplifyToPcm16(CycleExternalFilterOutput);
        }

        return s;
    }

    /// <summary>
    /// SID clocking with audio sampling - cycle based with linear sample interpolation.
    /// Port of reSID SID::clock_interpolate (native/vice/vice/src/resid/sid.cc :904-938).
    /// </summary>
    private int ClockInterpolate(ref int deltaT, short[] buf)
    {
        int n = buf.Length;
        int s;

        for (s = 0; s < n; s++)
        {
            int nextSampleOffset = _sampleOffset + _cyclesPerSample;
            int deltaTSample = nextSampleOffset >> FixpShift;

            if (deltaTSample > deltaT)
            {
                deltaTSample = deltaT;
            }

            for (int i = deltaTSample; i > 0; i--)
            {
                Tick();
                if (i <= 2)
                {
                    _samplePrev = _sampleNow;
                    _sampleNow = ClipPcm16(CycleExternalFilterOutput);
                }
            }

            if ((deltaT -= deltaTSample) == 0)
            {
                _sampleOffset -= deltaTSample << FixpShift;
                break;
            }

            _sampleOffset = nextSampleOffset & FixpMask;

            buf[s] = AmplifyToPcm16(
                _samplePrev + ((_sampleOffset * (_sampleNow - _samplePrev)) >> FixpShift));
        }

        return s;
    }

    /// <summary>
    /// SID clocking with audio sampling - cycle based with audio resampling
    /// (Smith/Gosset flexible sampling-rate conversion). Port of reSID
    /// SID::clock_resample (native/vice/vice/src/resid/sid.cc :977-1038).
    /// interleave is fixed at 1 by the <see cref="ClockBuffered"/> contract; n = buf.Length.
    /// </summary>
    private int ClockResample(ref int deltaT, short[] buf)
    {
        int n = buf.Length;
        short[] ring = _ring!;
        short[] fir = _fir!;
        int s;

        for (s = 0; s < n; s++)
        {
            int nextSampleOffset = _sampleOffset + _cyclesPerSample;
            int deltaTSample = nextSampleOffset >> FixpShift;

            if (deltaTSample > deltaT)
            {
                deltaTSample = deltaT;
            }

            for (int i = 0; i < deltaTSample; i++)
            {
                Tick();
                short pcm = ClipPcm16(CycleExternalFilterOutput);
                ring[_sampleIndex] = pcm;
                ring[_sampleIndex + RingSize] = pcm;
                _sampleIndex = (_sampleIndex + 1) & RingMask;
            }

            if ((deltaT -= deltaTSample) == 0)
            {
                _sampleOffset -= deltaTSample << FixpShift;
                break;
            }

            _sampleOffset = nextSampleOffset & FixpMask;

            int firOffset = _sampleOffset * _firRes >> FixpShift;
            int firOffsetRmd = _sampleOffset * _firRes & FixpMask;
            int firStart = firOffset * _firN;                       // base index into _fir
            int sampleStart = _sampleIndex - _firN - 1 + RingSize;  // base index into _ring

            // Convolution with filter impulse response.
            int v1 = 0;
            for (int j = 0; j < _firN; j++)
            {
                v1 += ring[sampleStart + j] * fir[firStart + j];
            }

            // Use next FIR table, wrap around to first FIR table using next sample.
            if (++firOffset == _firRes)
            {
                firOffset = 0;
                ++sampleStart;
            }
            firStart = firOffset * _firN;

            // Convolution with filter impulse response.
            int v2 = 0;
            for (int k = 0; k < _firN; k++)
            {
                v2 += ring[sampleStart + k] * fir[firStart + k];
            }

            // Linear interpolation.
            // fir_offset_rmd is equal for all samples, it can thus be factorized out:
            // sum(v1 + rmd*(v2 - v1)) = sum(v1) + rmd*(sum(v2) - sum(v1))
            int v = v1 + (int)((uint)firOffsetRmd * (uint)(v2 - v1) >> FixpShift);

            v >>= FirShift;

            buf[s] = AmplifyToPcm16(v);
        }

        return s;
    }
}

