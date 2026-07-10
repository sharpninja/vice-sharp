// Managed port of reSID's batched SID::clock(cycle_count delta_t) engine
// (native/vice/vice/src/resid/sid.cc:745-832 and the batched clock methods of
// envelope.h / wave.h / filter8580new.h / extfilt.h). This is reSID's
// non-cycle-accurate SAMPLE_FAST clocking: voice outputs are computed once over
// the whole delta_t window, oscillators sub-step only to sync-source MSB
// toggles, the filter sub-steps at dt=3 and the external filter at dt=8. It is
// reachable ONLY from ClockFast (Sid6581.Sampling.cs); the per-cycle Tick()
// chain is untouched. PLAN-VICEPARITY-001 (batched clock slice).
namespace ViceSharp.Chips.Audio;

public partial class Sid6581
{
    /// <summary>
    /// reSID SID::clock(cycle_count delta_t) (sid.cc:745-832): advance the whole
    /// chain by delta_t cycles in batched (approximate) form. Used only by the
    /// SAMPLE_FAST buffered path.
    /// </summary>
    internal void ClockBatched(int deltaT)
    {
        // Pipelined write on the MOS8580 (sid.cc:749-756): flush by stepping one
        // cycle recursively, commit, then continue with the remaining window.
        if (_writePipeline != 0 && deltaT > 0)
        {
            _writePipeline = 0;
            ClockBatched(1);
            CommitWrite(_writeAddress, _busValue);
            deltaT -= 1;
        }

        if (deltaT <= 0)
            return;

        // Batched bus aging (sid.cc:762-767).
        _busValueTtl -= deltaT;
        if (_busValueTtl <= 0)
        {
            _busValue = 0;
            _busValueTtl = 0;
        }

        // Batched envelopes (sid.cc:769-772).
        _voices[0].Env.ClockBatched(deltaT);
        _voices[1].Env.ClockBatched(deltaT);
        _voices[2].Env.ClockBatched(deltaT);

        // Clock and synchronize oscillators, sub-stepping to sync-source MSB
        // toggles so hard sync operates correctly (sid.cc:776-820).
        int deltaTOsc = deltaT;
        while (deltaTOsc != 0)
        {
            int deltaTMin = deltaTOsc;

            for (int i = 0; i < 3; i++)
            {
                ref Voice wave = ref _voices[i];
                // Only clock on the MSB of an oscillator that is a sync source
                // (its dest has sync set) and has freq != 0 (sid.cc:786-790).
                // sync_dest of voice i is voice (i+1)%3.
                if (!(((_voices[(i + 1) % 3].Control & 0x02) != 0) && wave.Frequency != 0))
                    continue;

                uint accumulator = wave.WaveformAccumulator & 0x00FFFFFFu;
                // Clock on MSB off if MSB is on, else clock on MSB on (sid.cc:796-797).
                uint deltaAccumulator =
                    ((accumulator & 0x800000u) != 0 ? 0x1000000u : 0x800000u) - accumulator;

                int deltaTNext = (int)(deltaAccumulator / wave.Frequency);
                if (deltaAccumulator % wave.Frequency != 0)
                    ++deltaTNext;

                if (deltaTNext < deltaTMin)
                    deltaTMin = deltaTNext;
            }

            ClockWaveBatched(ref _voices[0], deltaTMin, ref _msbRising0);
            ClockWaveBatched(ref _voices[1], deltaTMin, ref _msbRising1);
            ClockWaveBatched(ref _voices[2], deltaTMin, ref _msbRising2);
            SynchronizeOscillators();

            deltaTOsc -= deltaTMin;
        }

        // Calculate waveform output once (sid.cc:822-825).
        SetWaveformOutputBatched(0, deltaT);
        SetWaveformOutputBatched(1, deltaT);
        SetWaveformOutputBatched(2, deltaT);

        _cycleVoiceOutput0 = ComputeVoiceOutput(0);
        _cycleVoiceOutput1 = ComputeVoiceOutput(1);
        _cycleVoiceOutput2 = ComputeVoiceOutput(2);

        // Clock the filter (sid.cc:828) then the external filter (sid.cc:831).
        ClockResidFilterBatched(deltaT, _cycleVoiceOutput0, _cycleVoiceOutput1, _cycleVoiceOutput2);
        short filterRaw = ComputeResidFilterOutput6581();
        _cycleFilterOutput = filterRaw;
        ClockResidExtFilterBatched(deltaT, filterRaw);
        _cycleExtFilterOutput = ResidExtFilterOutput6581();
    }

    /// <summary>
    /// reSID WaveformGenerator::clock(cycle_count delta_t) (wave.h:178-245):
    /// bulk accumulator advance, MSB-rising detection and batched noise shifts.
    /// The 8580 tri/saw and pulse one-cycle delays are NOT modeled in batched
    /// mode (wave.h:269-270,293-294). Leaves msbRising untouched on the test path.
    /// </summary>
    private static void ClockWaveBatched(ref Voice voice, int deltaT, ref bool msbRising)
    {
        if ((voice.Control & 0x08) != 0)
        {
            // Test bit held (wave.h:180-195): count down the shift-register reset,
            // hold the pulse output high. msb_rising is left at its previous value.
            if (voice.ShiftRegisterReset != 0)
            {
                voice.ShiftRegisterReset = (uint)((int)voice.ShiftRegisterReset - deltaT);
                if ((int)voice.ShiftRegisterReset <= 0)
                {
                    voice.ShiftRegister = 0x7FFFFF;
                    voice.ShiftRegisterReset = 0;
                }
            }
            voice.PulseLevel = 0xFFF;
            return;
        }

        // Bulk accumulator advance (wave.h:197-204).
        uint accumulator = voice.WaveformAccumulator & 0x00FFFFFFu;
        uint deltaAccumulator = (uint)(deltaT * voice.Frequency);
        uint accumulatorNext = (accumulator + deltaAccumulator) & 0x00FFFFFFu;
        uint accumulatorBitsSet = ~accumulator & accumulatorNext;
        voice.WaveformAccumulator = accumulatorNext;

        msbRising = (accumulatorBitsSet & 0x800000u) != 0;

        // Shift the noise register once per accumulator bit-19 rising edge
        // (wave.h:209-239). NB: the two-cycle pipeline is only modeled single-cycle.
        uint shiftPeriod = 0x100000;
        while (deltaAccumulator != 0)
        {
            if (deltaAccumulator < shiftPeriod)
            {
                shiftPeriod = deltaAccumulator;
                if (shiftPeriod <= 0x080000)
                {
                    // Check for flip from 0 to 1 (wave.h:218-224).
                    if (((accumulatorNext - shiftPeriod) & 0x080000) != 0 || (accumulatorNext & 0x080000) == 0)
                        break;
                }
                else
                {
                    // Flip from 0 (to 1 or via 1 to 0) or from 1 via 0 to 1 (wave.h:225-231).
                    if (((accumulatorNext - shiftPeriod) & 0x080000) != 0 && (accumulatorNext & 0x080000) == 0)
                        break;
                }
            }

            ClockVoiceShiftRegister(ref voice);
            deltaAccumulator -= shiftPeriod;
        }

        // Pulse compare, applied directly in batched mode (wave.h:243).
        voice.PulseLevel = (accumulatorNext >> 12) >= voice.PulseWidth ? (ushort)0xFFF : (ushort)0x000;
    }

    /// <summary>
    /// reSID WaveformGenerator::set_waveform_output(cycle_count delta_t)
    /// (wave.h:522-555): same waveform_output as the single-cycle path, but osc3
    /// = waveform_output directly (the 8580 tri/saw delay is not modeled), the
    /// combined-waveform shift-register write drops the shift_pipeline guard, and
    /// the floating-DAC TTL ages in bulk.
    /// </summary>
    private void SetWaveformOutputBatched(int i, int deltaT)
    {
        ref Voice voice = ref _voices[i];
        int waveform = (voice.Control >> 4) & 0x0F;
        uint accumulator = voice.WaveformAccumulator & 0x00FFFFFFu;

        if (waveform != 0)
        {
            uint syncSourceAccumulator = _voices[(i + 2) % 3].WaveformAccumulator & 0x00FFFFFFu;
            uint ringMsbMask = (voice.Control & 0x24) == 0x04 ? 0x00800000u : 0u;
            int ix = (int)((accumulator ^ (~syncSourceAccumulator & ringMsbMask)) >> 12);

            int wave12 = WaveTable12(waveform, ix);
            int noPulse = (waveform & 0x4) != 0 ? 0x000 : 0xFFF;
            int noNoiseOrNoiseOutput = (waveform & 0x8) != 0
                ? NoiseOutput12(voice.ShiftRegister)
                : 0xFFF;

            int waveformOutput = wave12 & (noPulse | voice.PulseLevel) & noNoiseOrNoiseOutput;

            if ((waveform & 0xC) == 0xC)
            {
                waveformOutput = IsMos8580Wave
                    ? NoisePulse8580(waveformOutput)
                    : NoisePulse6581(waveformOutput);
            }

            // Combined waveforms write the shift register (wave.h:538-543). NB the
            // batched path drops the shift_pipeline!=1 guard: skipped cycles miss
            // writes, which is the documented approximation.
            if (waveform > 0x8 && (voice.Control & 0x08) == 0)
            {
                WriteShiftRegister(ref voice, waveformOutput);
            }

            if (!IsMos8580Wave && (waveform & 0x2) != 0 && (waveform & 0xd) != 0)
            {
                voice.WaveformAccumulator &= (uint)(waveformOutput << 12) | 0x7FFFFFu;
            }

            // osc3 = waveform_output directly (wave.h:532): no 8580 tri/saw delay.
            voice.WaveformOutput = (ushort)waveformOutput;
            voice.Osc3 = (ushort)waveformOutput;
        }
        else
        {
            // Age the floating D/A output in bulk (wave.h:546-553).
            if (voice.FloatingOutputTtl != 0)
            {
                voice.FloatingOutputTtl -= deltaT;
                if (voice.FloatingOutputTtl <= 0)
                {
                    voice.FloatingOutputTtl = 0;
                    voice.WaveformOutput = 0;
                    voice.Osc3 = 0;
                }
            }
        }
    }

    /// <summary>
    /// reSID Filter::clock(cycle_count delta_t, v1, v2, v3) (filter8580new.h:
    /// 800-927): prescale the voices and select the summer input once, then
    /// sub-step the two integrators at dt=3 with the held voice input.
    /// </summary>
    private void ClockResidFilterBatched(int deltaT, int rawVoice0, int rawVoice1, int rawVoice2)
    {
        var m = FilterModel;

        _rv1 = ((rawVoice0 * m.VoiceScaleS14) >> 18) + m.VoiceDC;
        _rv2 = ((rawVoice1 * m.VoiceScaleS14) >> 18) + m.VoiceDC;
        _rv3 = ((rawVoice2 * m.VoiceScaleS14) >> 18) + m.VoiceDC;

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
            case 0x8: Vi = 0;                   offset = SummerOffset1; break;
            case 0x9: Vi = _rv1;                offset = SummerOffset2; break;
            case 0xA: Vi = _rv2;                offset = SummerOffset2; break;
            case 0xB: Vi = _rv2 + _rv1;         offset = SummerOffset3; break;
            case 0xC: Vi = _rv3;                offset = SummerOffset2; break;
            case 0xD: Vi = _rv3 + _rv1;         offset = SummerOffset3; break;
            case 0xE: Vi = _rv3 + _rv2;         offset = SummerOffset3; break;
            default:  Vi = _rv3 + _rv2 + _rv1; offset = SummerOffset4; break;
        }

        // dt=3 fixpoint sub-step loop (filter8580new.h:888-927).
        int deltaTFlt = 3;
        while (deltaT != 0)
        {
            if (deltaT < deltaTFlt)
                deltaTFlt = deltaT;

            if (IsMos8580Filter)
            {
                _vlp = SolveIntegrate8580(deltaTFlt, _vbp, ref _vlpX, ref _vlpVc, m);
                _vbp = SolveIntegrate8580(deltaTFlt, _vhp, ref _vbpX, ref _vbpVc, m);
            }
            else
            {
                _vlp = SolveIntegrate6581(deltaTFlt, _vbp, ref _vlpX, ref _vlpVc, m);
                _vbp = SolveIntegrate6581(deltaTFlt, _vhp, ref _vbpX, ref _vbpVc, m);
            }

            int vbpC = _vbp;
            if (vbpC < 0) vbpC = 0; else if (vbpC > 65535) vbpC = 65535;
            int sumIdx = offset + m.Resonance[_filterResonance][vbpC] + _vlp + Vi;
            if (sumIdx < 0) sumIdx = 0;
            else if (sumIdx >= SummerTableSize) sumIdx = SummerTableSize - 1;
            _vhp = m.Summer[sumIdx];

            deltaT -= deltaTFlt;
        }
    }

    /// <summary>
    /// reSID ExternalFilter::clock(cycle_count delta_t, short Vi) (extfilt.h:
    /// 121-153): disabled pass-through, else the dt=8 sub-step recurrence.
    /// </summary>
    private void ClockResidExtFilterBatched(int deltaT, short vi)
    {
        if (!_extFilterEnabled)
        {
            _extFiltVlp = vi << 11;
            _extFiltVhp = 0;
            return;
        }

        int deltaTFlt = 8;
        while (deltaT != 0)
        {
            if (deltaT < deltaTFlt)
                deltaTFlt = deltaT;

            int dVlp = (ExtFiltW0lp1s7 * deltaTFlt >> 3) * ((vi << 11) - _extFiltVlp) >> 4;
            int dVhp = (ExtFiltW0hp1s17 * deltaTFlt >> 3) * (_extFiltVlp - _extFiltVhp) >> 14;
            _extFiltVlp += dVlp;
            _extFiltVhp += dVhp;

            deltaT -= deltaTFlt;
        }
    }
}
