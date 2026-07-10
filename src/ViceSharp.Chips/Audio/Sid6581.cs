using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Audio;

public partial class Sid6581 : IClockedDevice, IAddressSpace, IAudioChip
{
    public DeviceId Id => new DeviceId(0x0004);
    public string Name => "MOS 6581 SID";
    // BUG-SIDAUDIO-001: the SID phase accumulator and ADSR rate counter advance once per
    // Tick, and the ADSR rate tables are resid rate_counter_period values in phi2-cycle
    // units, so the SID must tick once per phi2 master cycle (divisor 1). Ticking every 16
    // cycles made pitch and envelopes 16x too slow. (The audio sample rate self-corrects via
    // ConfigureAudioClock either way; only pitch/envelope timing were wrong.)
    public uint ClockDivisor => 1;
    public ClockPhase Phase => ClockPhase.Phi2;

    public int SamplingRate => 44100;
    public int ChannelCount => 1;
    public byte MasterVolume { get => _volume; set => _volume = value; }

    /// <summary>
    /// Pending-playback depth of the attached audio backend (0 when none).
    /// </summary>
    public int QueuedSampleCount => _audioBackend?.QueuedSampleCount ?? 0;

    /// <summary>Free playback-buffer space exposed by the attached audio backend.</summary>
    public int AvailableSampleCount => _audioBackend?.AvailableSampleCount ?? int.MaxValue;

    /// <summary>SID batches samples in the same 256-sample fragment size VICE flushes.</summary>
    public int AudioFragmentSampleCount => _sampleBuffer.Length;

    /// <summary>
    /// True once a backend is attached AND <see cref="ConfigureAudioClock"/> has run
    /// (so the SID is streaming samples and can be the emulation timing source).
    /// </summary>
    public bool IsAudioTimingSource => _audioBackend is not null && _audioTicksPerSample > 0.0;

    // Waveform types
    public enum Waveform { Triangle = 0x04, Sawtooth = 0x08, Pulse = 0x40, Noise = 0x80, None = 0x00 }
    
    // Voice modulation modes
    public enum VoiceModulation { None, Sync, Ring }
    
    // Filter types
    public enum FilterType { None, LowPass, BandPass, HighPass, Notch }
    
    // ADSR envelope states
    public enum EnvelopeState { Idle, Attack, Decay, Sustain, Release }

    // FR-SID-009 noise LFSR. The hardware LFSR is 23 bits wide (mask
    // 0x7FFFFF) with feedback taps at bits 17 and 22 (XOR), clocked when
    // bit 19 of each voice's 24-bit phase accumulator transitions low->high
    // with a 2-cycle pipeline delay (wave.h:164-170). The reset seed is
    // 0x7ffffe (wave.cc:323). Output is 12 bits packed from SR bits
    // 20,18,14,11,9,5,2,0 into waveform bits 11-4 (wave.h:354-367).
    // PLAN-VICEPARITY-001 S8 (FR-SID-VOICE): 20-bit voice output range is
    // (wave12dac - wave_zero) * envelope_dac, no >>8 shift (voice.h:99-103).
    // The product spans [-2048*255, 2047*255] = ~[-521984, 521985].
    // GenerateSample divides by this scale to normalize to [-1,1] float.
    // 524288 = 2048 * 256 (was 2048 when >>8 was applied; removing >>8 adds factor 256).
    private const float VoiceOutputScale = 524288.0f;
    // Bit 19 of the 24-bit accumulator (the implementation stores the
    // 24-bit value in a uint; the upper byte is the phase output).
    private const uint NoiseClockBit = 1u << 19;
    /// <summary>
    /// reSID 6581 floating-DAC fade-start TTL: cycles from waveform
    /// deselect to the first bit-fade (wave.cc:43).
    /// FR-SID-OSC3ENV3 AC-07 [PLAN-VICEPARITY-001 S3].
    /// </summary>
    private const int FloatingOutputTtlStart6581 = 182000;
    /// <summary>
    /// reSID 6581 per-bit-fade TTL: cycles between successive bit-fades
    /// while the latch is nonzero (wave.cc:44).
    /// FR-SID-OSC3ENV3 AC-07 [PLAN-VICEPARITY-001 S3].
    /// </summary>
    private const int FloatingOutputTtlBit6581 = 1500;
    /// <summary>
    /// reSID 6581 shift-register full-reset countdown (wave.cc:35):
    /// cycles from test-rising to the first shiftreg_bitfade call.
    /// FR-SID-WAVE-TESTBIT AC-04 [PLAN-VICEPARITY-001 S4/S5].
    /// </summary>
    private const uint ShiftRegisterResetStart6581 = 35000;
    /// <summary>
    /// reSID 6581 per-bit-fade countdown (wave.cc:36): cycles between
    /// successive OR-fill steps while the shift register is not all-ones.
    /// FR-SID-WAVE-TESTBIT AC-04 [PLAN-VICEPARITY-001 S4/S5].
    /// </summary>
    private const uint ShiftRegisterResetBit6581 = 1000;

    /// <summary>
    /// The waveform DAC zero point in the 12-bit domain (voice.cc:93-97).
    /// reSID subtracts wave_zero from the 12-bit R-2R waveform DAC output
    /// before multiplying by the envelope DAC, producing a signed 20-bit
    /// voice output centered around zero. MOS 6581: 0x380; MOS 8580: 0x9e0.
    /// FR-SID-VOICE AC-02, AC-03 [PLAN-VICEPARITY-001 S8].
    /// </summary>
    protected virtual int WaveZeroLevel => 0x380;


    /// <summary>
    /// Reads the current audio sample from the per-cycle-evolved chain state.
    /// PLAN-VICEPARITY-001 S1 (FR-SID-CLOCK AC-02/AC-06): the voice waveform
    /// outputs, the filter stage and the external-filter stage are computed
    /// once per phi2 cycle inside Tick() in reSID SID::clock() order
    /// (sid.h:200-244, sid.cc:822-831), so sampling is a pure read of the
    /// committed external-filter output with the 4-bit master volume applied.
    /// Register writes between cycles no longer retroactively change the
    /// current sample; they take effect when the next cycle's chain runs,
    /// exactly like hardware.
    /// </summary>
    public float GenerateSample()
    {
        if (UsesReSidFilter)
        {
            // reSID path: gain[vol] table already embeds the volume scaling;
            // _cycleExtFilterOutput is the external-filter output SID::output()
            // (extfilt, ±32768 range). reSID amplifies it by the per-model
            // scaleFactor and integer-clips to 16 bits (amplify(), sid.cc:54-57,
            // applied at emission sid.cc:886-888) before the host consumes it.
            // The managed host contract is float [-1, 1], so the amplify/clip
            // happens on the integer sample and the result is scaled by 1/2^15
            // (lossless). The 6581 is 1.5x louder than the raw output (scale 3),
            // matching VICE. PLAN-VICEPARITY-001 S12 (FR-SID-OUTPUT AC-01/05/06).
            return AmplifyToPcm16(_cycleExtFilterOutput) / 32768.0f;
        }

        // FR-SID-010 digi playback: the 4-bit master-volume DAC ($D418 bits 0-3)
        // contributes a small DC offset proportional to volume even when no
        // voices are gated. This is the rail that makes the famous Galway/
        // Daglish 4-bit PCM technique audible on real SID hardware: rapid
        // $D418 writes alone produce hearable PCM through the DAC nonlinearity.
        // The chosen DC magnitude (DigiDcOffset) is modest enough to leave
        // normal voice output dominant while still letting per-write volume
        // changes register as audible amplitude.
        float volumeFraction = _volume / 15.0f;
        float voiceMix = _cycleExtFilterOutput * volumeFraction / VoiceOutputScale;
        float digiDcOffset = volumeFraction * DigiDcOffset;
        return Math.Clamp(voiceMix + digiDcOffset, -1.0f, 1.0f);
    }

    /// <summary>
    /// Per-cycle waveform-output pass (reSID sid.h:220-223, sid.cc:822-825):
    /// first runs each voice's reSID set_waveform_output equivalent (the
    /// 12-bit selected-waveform pass that latches the osc3 readback state
    /// and pushes the pulse level pipeline, FR-SID-OSC3ENV3
    /// [PLAN-VICEPARITY-001 S2]), then computes each voice's selected 8-bit
    /// waveform sample from the post-synchronize oscillator state, applies
    /// the envelope DAC, and commits the three voice outputs that feed the
    /// filter stage this cycle. (Routing the audio path through the same
    /// 12-bit output is the FR-SID-WAVE-SAWTRI / FR-SID-WAVE-DACRES
    /// remediation, not this slice.)
    /// </summary>
    private void ComputeWaveformOutputs()
    {
        SetWaveformOutput(0);
        SetWaveformOutput(1);
        SetWaveformOutput(2);
        _cycleVoiceOutput0 = ComputeVoiceOutput(0);
        _cycleVoiceOutput1 = ComputeVoiceOutput(1);
        _cycleVoiceOutput2 = ComputeVoiceOutput(2);
    }

    /// <summary>
    /// True on the MOS 8580 die variants. Selects the 8580 branches of the
    /// waveform-output pass: the tri/saw OSC3 pipeline delay (reSID
    /// wave.h:475-482) and the 8580 noise+pulse combination transform
    /// (wave.h:453-456,469-473). The 6581 base returns false.
    /// </summary>
    protected virtual bool IsMos8580Wave => false;

    /// <summary>
    /// Per-voice mirror of reSID WaveformGenerator::set_waveform_output()
    /// (wave.h:458-519) for the OSC3 readback path, FR-SID-OSC3ENV3
    /// AC-01..AC-06 [PLAN-VICEPARITY-001 S2]. With a waveform selected it
    /// computes the 12-bit selected output wave[ix] &amp; (no_pulse |
    /// pulse_output) &amp; no_noise_or_noise_output (wave.h:462-467), applies
    /// the die-specific noise+pulse transform (wave.h:469-473), and latches
    /// osc3: directly on the 6581 (wave.h:485), through the one-cycle
    /// tri_saw_pipeline on the 8580 (wave.h:475-482). The waveform-0
    /// floating-DAC fade (wave.h:499-504) is FR-SID-OSC3ENV3 AC-07, stopped
    /// this slice (TEST-SID-CLOCK-11 locks the legacy phase readback), so
    /// waveform 0 leaves the latch untouched. The tail always pushes the
    /// next pulse level into the pipeline (one-cycle compare delay,
    /// wave.h:506-518). The 6581 combined-saw accumulator writeback and the
    /// combined-waveform shift-register writeback (wave.h:488-497) belong to
    /// FR-SID-WAVE-COMBINED / FR-SID-WAVE-NOISE and are intentionally
    /// absent. Called once per voice per cycle from the chain and at
    /// control-write time (wave.cc:261-264). Allocation-free.
    /// </summary>
    private void SetWaveformOutput(int i)
    {
        ref Voice voice = ref _voices[i];
        int waveform = (voice.Control >> 4) & 0x0F;
        // Bits 24-31 of the stored accumulator are masked off defensively:
        // reSID keeps the register 24-bit (wave.h:155); the managed stored
        // width is FR-SID-WAVE-ACC AC-02 (stopped this slice).
        uint accumulator = voice.WaveformAccumulator & 0x00FFFFFFu;

        if (waveform != 0)
        {
            // wave.h:465 with wave.cc:214: the ring-mod MSB substitution mask
            // is armed only when ring mod is on and sawtooth is off.
            uint syncSourceAccumulator = _voices[(i + 2) % 3].WaveformAccumulator & 0x00FFFFFFu;
            uint ringMsbMask = (voice.Control & 0x24) == 0x04 ? 0x00800000u : 0u;
            int ix = (int)((accumulator ^ (~syncSourceAccumulator & ringMsbMask)) >> 12);

            int wave12 = WaveTable12(waveform, ix);
            int noPulse = (waveform & 0x4) != 0 ? 0x000 : 0xFFF;                       // wave.cc:221
            int noNoiseOrNoiseOutput = (waveform & 0x8) != 0                            // wave.cc:219-220
                ? NoiseOutput12(voice.ShiftRegister)                                    // AC-08 [PLAN-VICEPARITY-001 S6]
                : 0xFFF;

            int waveformOutput = wave12 & (noPulse | voice.PulseLevel) & noNoiseOrNoiseOutput;

            if ((waveform & 0xC) == 0xC)
            {
                waveformOutput = IsMos8580Wave
                    ? NoisePulse8580(waveformOutput)
                    : NoisePulse6581(waveformOutput);
            }

            // wave.h:494-497: combined waveforms write back into shift register.
            // Condition: noise is combined with another waveform (waveform > 0x8),
            // test bit is not held, and shift_pipeline is not in step 1.
            // AC-11/AC-16/AC-17/AC-18 [PLAN-VICEPARITY-001 S6].
            if (waveform > 0x8 && (voice.Control & 0x08) == 0 && voice.ShiftPipeline != 1)
            {
                WriteShiftRegister(ref voice, waveformOutput);
            }

            // wave.h:488-492 (6581 only): when the sawtooth is combined with
            // another waveform the accumulator MSB (bit 23) may be driven low
            // when waveform_output bit 11 is 0. The (waveform & 0xd) guard
            // restricts this to sawtooth-in-combination (triangle/pulse/noise
            // also selected); for pure sawtooth reSID skips it because it is a
            // mathematical identity. For combined waveforms with a ROM output
            // below 0x800 it resets bit 23 (FR-SID-WAVE-COMBINED AC-15 /
            // PLAN-VICEPARITY-001 S7).
            if (!IsMos8580Wave && (waveform & 0x2) != 0 && (waveform & 0xd) != 0)
            {
                voice.WaveformAccumulator &= (uint)(waveformOutput << 12) | 0x7FFFFFu;
            }

            // wave.h:588-592: the value fed to Voice::output() (and thus the
            // filter) is ALWAYS the current waveform_output, DAC-mapped. Osc3 is
            // a separate readback latch that on the 8580 tri/saw path lags one
            // cycle (readOSC, wave.cc:293-295). PLAN-VICEPARITY-001 S11.
            voice.WaveformOutput = (ushort)waveformOutput;

            if ((waveform & 0x3) != 0 && IsMos8580Wave)
            {
                // wave.h:475-482: 8580 tri/saw OSC3 readback is delayed half a
                // cycle, appearing as a one-cycle OSC3 delay through the pipeline.
                voice.Osc3 = (ushort)(voice.TriSawPipeline & (noPulse | voice.PulseLevel) & noNoiseOrNoiseOutput);
                voice.TriSawPipeline = (ushort)wave12;
            }
            else
            {
                voice.Osc3 = (ushort)waveformOutput;                                    // wave.h:485
            }
        }
        else
        {
            // FR-SID-OSC3ENV3 AC-07 [PLAN-VICEPARITY-001 S3]: with waveform 0
            // selected, set_waveform_output ages the floating-DAC TTL each
            // cycle and calls wave_bitfade() when it expires (wave.h:499-503).
            // The Osc3 latch retains the last selected output and decays
            // bit-by-bit to zero; a zero latch never re-arms (wave.cc:278-279).
            if (voice.FloatingOutputTtl != 0 && --voice.FloatingOutputTtl == 0)
            {
                WaveBitFade(ref voice);
            }
        }

        // wave.h:506-518: the pulse compare result is delayed one cycle; push
        // the next level from this cycle's post-clock 24-bit phase.
        voice.PulseLevel = (accumulator >> 12) >= voice.PulseWidth ? (ushort)0xFFF : (ushort)0x000;
    }

    /// <summary>
    /// reSID wave_bitfade() (wave.cc:274-280): right-fold the floating
    /// waveform_output one bit, mirror it to the osc3 readback latch, and
    /// re-arm the per-bit TTL if the result is nonzero. Called by the
    /// waveform-0 path in <see cref="SetWaveformOutput"/> when the floating-DAC
    /// TTL expires. FR-SID-OSC3ENV3 AC-07 [PLAN-VICEPARITY-001 S3].
    /// </summary>
    private static void WaveBitFade(ref Voice voice)
    {
        int output = voice.WaveformOutput & (voice.WaveformOutput >> 1);
        voice.WaveformOutput = (ushort)output; // wave.cc:276
        voice.Osc3 = (ushort)output;           // wave.cc:277 (osc3 = waveform_output)
        if (output != 0)
        {
            voice.FloatingOutputTtl = FloatingOutputTtlBit6581;
        }
    }

    /// <summary>
    /// The reSID waveform table row for the OSC3 path, selected by
    /// waveform &amp; 0x7 (wave.cc:211). Rows 0 and 4 are the all-ones
    /// noise/pulse mask rows, row 1 the branch-free triangle (upper 11 bits
    /// MSB-folded and left-shifted, bit 0 grounded) and row 2 the sawtooth
    /// identity, exactly as built by the reSID class initialiser
    /// (wave.cc:87-101). The combined rows 3/5/6/7 are measured die ROM tables
    /// (wave6581__ST.h / wave6581_P_T.h / wave6581_PS_.h / wave6581_PST.h)
    /// ported verbatim in <see cref="SidWaveTables"/> (FR-SID-WAVE-COMBINED,
    /// PLAN-VICEPARITY-001 S7). Overridden in <see cref="Sid8580"/> to use
    /// the 8580 ROM tables.
    /// </summary>
    protected virtual int WaveTable12(int waveform, int ix)
    {
        int tri = (((ix & 0x800) != 0 ? ~ix : ix) & 0x7FF) << 1;   // wave.cc:96
        int saw = ix & 0xFFF;                                      // wave.cc:97
        return (waveform & 0x7) switch
        {
            0 => 0xFFF,                             // wave.cc:95 (noise-only selections mask through)
            1 => tri,
            2 => saw,
            3 => SidWaveTables.Wave6581_ST[ix],     // wave6581__ST.h (PLAN-VICEPARITY-001 S7)
            4 => 0xFFF,                             // wave.cc:98 (pulse rail comes from the pulse level mask)
            5 => SidWaveTables.Wave6581_PT[ix],     // wave6581_P_T.h
            6 => SidWaveTables.Wave6581_PS[ix],     // wave6581_PS_.h
            _ => SidWaveTables.Wave6581_PST[ix],    // wave6581_PST.h
        };
    }

    /// <summary>
    /// reSID 12-bit noise output packing (set_noise_output, wave.h:354-367):
    /// shift-register bits 20, 18, 14, 11, 9, 5, 2, 0 drive waveform bits 11
    /// down to 4; the low 4 waveform bits are grounded. FR-SID-OSC3ENV3
    /// AC-04: OSC3 with noise selected reads exactly this value shifted
    /// right 4.
    /// </summary>
    private static int NoiseOutput12(uint lfsr) =>
        (int)(((lfsr & 0x100000) >> 9) |
              ((lfsr & 0x040000) >> 8) |
              ((lfsr & 0x004000) >> 5) |
              ((lfsr & 0x000800) >> 3) |
              ((lfsr & 0x000200) >> 2) |
              ((lfsr & 0x000020) << 1) |
              ((lfsr & 0x000004) << 3) |
              ((lfsr & 0x000001) << 4));

    /// <summary>
    /// Immediate pulse-level push performed by the PW register writes (reSID
    /// writePW_LO / writePW_HI, wave.cc:158-170): the new 12-bit compare
    /// result enters the pulse level pipeline without waiting for the next
    /// cycle's waveform-output pass. FR-SID-OSC3ENV3 AC-03
    /// [PLAN-VICEPARITY-001 S2].
    /// </summary>
    private void PushPulseLevel(int i)
    {
        ref Voice voice = ref _voices[i];
        voice.PulseLevel = ((voice.WaveformAccumulator & 0x00FFFFFFu) >> 12) >= voice.PulseWidth
            ? (ushort)0xFFF
            : (ushort)0x000;
    }

    /// <summary>MOS 6581 noise+pulse combination transform (reSID wave.h:448-451).</summary>
    private static int NoisePulse6581(int noise) =>
        noise < 0xF00 ? 0x000 : noise & (noise << 1) & (noise << 2);

    /// <summary>MOS 8580 noise+pulse combination transform (reSID wave.h:453-456).</summary>
    private static int NoisePulse8580(int noise) =>
        noise < 0xFC0 ? noise & (noise << 1) : 0xFC0;

    /// <summary>
    /// Selected waveform sample of voice <paramref name="i"/> multiplied by
    /// the envelope DAC output, routed through the 12-bit
    /// <see cref="Voice.Osc3"/> latch set by <see cref="SetWaveformOutput"/>
    /// each cycle. FR-SID-WAVE-SAWTRI / FR-SID-WAVE-PULSE / FR-SID-WAVE-DACRES
    /// AC-01 [PLAN-VICEPARITY-001 S3]: reSID routes audio through the 12-bit
    /// waveform_output latch (wave.h:104,485); with no waveform selected the
    /// voice is silent (the waveform-0 floating-DAC audio bias is FR-SID-VOICE
    /// AC-04, a later slice). FR-SID-ENV AC-50: the envelope is applied through
    /// the nonlinear model_dac row (envelope.h:377-383).
    /// </summary>
    private int ComputeVoiceOutput(int i)
    {
        ref Voice voice = ref _voices[i];
        // reSID never special-cases "no waveform"; Voice::output() is always
        // (model_dac[waveform_output] - wave_zero) * envelope.output()
        // (voice.h:99-103). With no waveform selected, waveform_output is the
        // floating-DAC latch that fades to 0 via floating_output_ttl
        // (wave.h:501,546-551), so the voice still contributes a decaying DC bias
        // to the mix. PLAN-VICEPARITY-001 S9 (FR-SID-VOICE AC-04): route the
        // floating latch through the shared formula below rather than forcing 0.
        // PLAN-VICEPARITY-001 S8 (FR-SID-VOICE AC-01, AC-02): reSID voice
        // output = (wave.output() - wave_zero) * envelope.output() with NO >>8
        // (voice.h:99-103, voice.cc:105-113). WaveZeroLevel is the 12-bit
        // constant (0x380 for 6581, 0x9e0 for 8580). WaveDacTable maps the
        // 12-bit oscillator output through the R-2R nonlinearity (S7).
        // Uses WaveformOutput (the current DAC index, wave.h:588-592), NOT Osc3:
        // on the 8580 tri/saw path Osc3 lags one cycle for OSC3 readback only.
        // PLAN-VICEPARITY-001 S11 (FR-SID-FILTER-8580).
        return (WaveDacTable[voice.WaveformOutput] - WaveZeroLevel) * EnvelopeDacTable[voice.Env.EnvelopeCounter];
    }

    /// <summary>
    /// Per-cycle filter chain (reSID SID::clock(), sid.h:225-229 and
    /// sid.cc:827-831): the filter stage consumes this cycle's three voice
    /// outputs, then the external-filter stage consumes the filter output.
    /// The filter transfer function is still the managed SVF (its reSID
    /// replacement is the FR-SID-FILTER-6581 / FR-SID-FILTER-CLOCK
    /// remediation) and the external-filter stage is a unity placeholder
    /// until FR-SID-EXTFILT lands; FR-SID-CLOCK AC-07 pins the dispatch,
    /// not the analog models.
    /// </summary>
    /// <summary>
    /// Per-cycle filter chain dispatch.
    /// For the 6581 path (UsesReSidFilter=true): runs reSID
    /// Filter::clock() + Filter::output() + ExternalFilter::clock/output.
    /// For the 8580 path: keeps the Chamberlin SVF path.
    /// PLAN-VICEPARITY-001 S9 (FR-SID-FILTER-CLOCK AC-01..05).
    /// </summary>
    private void ClockFilterChain()
    {
        if (UsesReSidFilter)
        {
            ClockResidFilter6581(_cycleVoiceOutput0, _cycleVoiceOutput1, _cycleVoiceOutput2);
            short filterRaw = ComputeResidFilterOutput6581();
            _cycleFilterOutput = filterRaw;
            ClockResidExtFilter6581(filterRaw);
            _cycleExtFilterOutput = ResidExtFilterOutput6581();
        }
        else
        {
            _cycleFilterOutput = ApplyFilter(_cycleVoiceOutput0, _cycleVoiceOutput1, _cycleVoiceOutput2);
            _cycleExtFilterOutput = ClockExternalFilter(_cycleFilterOutput);
        }
    }

    /// <summary>
    /// External-filter stage placeholder for the non-reSID (8580 SVF) path.
    /// Unity gain. The reSID path uses ClockResidExtFilter6581 / ResidExtFilterOutput6581.
    /// </summary>
    private static int ClockExternalFilter(int filterOutput) => filterOutput;

    // FR-SID-ENV AC-50 [PLAN-VICEPARITY-001 S1]: reSID envelope DAC tables,
    // built once exactly like the EnvelopeGenerator constructor
    // (envelope.cc:163-171): MOS 6581 with 2R/R = 2.20 and the missing
    // termination resistor, MOS 8580 with 2R/R = 2.00 and correct
    // termination.
    private static readonly ushort[] EnvelopeDac6581 = BuildEnvelopeDacTable(8, 2.20, term: false);
    private protected static readonly ushort[] EnvelopeDac8580 = BuildEnvelopeDacTable(8, 2.00, term: true);

    /// <summary>
    /// The die-specific envelope DAC row (reSID model_dac[sid_model],
    /// envelope.h:377-383). The 6581 base implementation returns the
    /// 2R/R = 2.20 no-termination table; Sid8580 overrides.
    /// </summary>
    protected virtual ReadOnlySpan<ushort> EnvelopeDacTable => EnvelopeDac6581;

    /// <summary>
    /// The 12-bit waveform DAC table (reSID model_dac[sid_model][waveform_output],
    /// wave.h:587-593). 4096 entries; built with the same R-2R algorithm as the
    /// envelope DAC but over 12 bits. For MOS 6581: 2R/R = 2.20, no termination.
    /// Overridden in <see cref="Sid8580"/> for 2R/R = 2.00 with termination.
    /// FR-SID-WAVE-DACRES AC-01 / PLAN-VICEPARITY-001 S7.
    /// </summary>
    private static readonly ushort[] WaveDac6581 = BuildEnvelopeDacTable(12, 2.20, term: false);
    private protected static readonly ushort[] WaveDac8580Static = BuildEnvelopeDacTable(12, 2.00, term: true);
    protected virtual ReadOnlySpan<ushort> WaveDacTable => WaveDac6581;

    /// <summary>
    /// Verbatim port of reSID build_dac_table (dac.cc:76-137): computes the
    /// output table of an R-2R ladder DAC with the given 2R/R ratio, an
    /// optional termination resistor (missing on all MOS 6581 DACs), and
    /// die-specific MOSFET leakage through zero bits (6581 0.0075, 8580
    /// 0.0035, dac.cc:46-47). Per-bit voltages come from repeated parallel
    /// substitution plus a source transformation, output voltages
    /// superposition per set bit, and each entry scales to 2^bits - 1 with
    /// + 0.5 truncation rounding. Same double arithmetic as the C++
    /// original, so every entry is bit-exact.
    /// </summary>
    internal static ushort[] BuildEnvelopeDacTable(int bits, double twoRDivR, bool term)
    {
        const double MosfetLeakage6581 = 0.0075;
        const double MosfetLeakage8580 = 0.0035;
        double leakage = term ? MosfetLeakage8580 : MosfetLeakage6581;

        Span<double> vbit = stackalloc double[12];
        for (int setBit = 0; setBit < bits; setBit++)
        {
            int bit;
            double vn = 1.0;          // Normalized bit voltage.
            const double R = 1.0;     // Normalized R.
            double twoR = twoRDivR * R;
            double rn = term ? twoR : double.PositiveInfinity;

            // DAC "tail" resistance by repeated parallel substitution.
            for (bit = 0; bit < setBit; bit++)
            {
                rn = double.IsPositiveInfinity(rn)
                    ? R + twoR
                    : R + twoR * rn / (twoR + rn); // R + 2R || Rn
            }

            // Source transformation for the bit voltage.
            if (double.IsPositiveInfinity(rn))
            {
                rn = twoR;
            }
            else
            {
                rn = twoR * rn / (twoR + rn); // 2R || Rn
                vn = vn * rn / twoR;
            }

            // Output voltage by repeated source transformation from the tail.
            for (++bit; bit < bits; bit++)
            {
                rn += R;
                double current = vn / rn;
                rn = twoR * rn / (twoR + rn); // 2R || Rn
                vn = rn * current;
            }

            vbit[setBit] = vn;
        }

        var dac = new ushort[1 << bits];
        for (int i = 0; i < (1 << bits); i++)
        {
            int x = i;
            double vo = 0.0;
            for (int j = 0; j < bits; j++)
            {
                vo += ((x & 0x1) != 0 ? 1.0 : leakage) * vbit[j];
                x >>= 1;
            }

            // Scale maximum output to 2^bits - 1.
            dac[i] = (ushort)(((1 << bits) - 1) * vo + 0.5);
        }

        return dac;
    }

    /// <summary>
    /// Per-step amplitude of the $D418 DAC DC offset used for digi playback
    /// (FR-SID-010). Scales linearly with the 4-bit master-volume nibble;
    /// at volume 15 the offset is DigiDcOffset, at volume 0 it is exactly
    /// zero (silent rail preserved). Chosen small enough that normal voice
    /// output dominates the mix.
    /// </summary>
    private const float DigiDcOffset = 0.05f;

    // FR-SID-004 (BACKFILL-SID-001 filter slice): state-variable filter
    // state. The Chamberlin SVF needs two persistent integrators (LP and
    // BP); the HP tap is computed each step from the current LP, BP, the
    // filter input and the resonance Q feedback, so it does not need its
    // own state field.
    // Chamberlin SVF integrator state. protected so Sid8580 can reuse the
    // same state-variable filter topology with its own cutoff curve.
    protected double _svfLowPass;
    protected double _svfBandPass;

    /// <summary>
    /// FR-SID-004 (BACKFILL-SID-001 filter slice). State-variable filter
    /// with per-voice routing, LP/BP/HP additive mode select, resonance
    /// Q feedback, and soft-clipping for criterion 7 (resonance distortion).
    /// The 11-bit cutoff register from $D415/$D416 is mapped through the
    /// 6581 non-linear ("kinked") curve (criterion 6) to a frequency in
    /// Hz, then converted to a Chamberlin SVF coefficient via
    /// 2*sin(PI*f/Fs). Sid8580 and Sid8580D supply their own ApplyFilter
    /// override - this implementation is 6581-only.
    /// </summary>
    protected virtual int ApplyFilter(int voice0, int voice1, int voice2)
    {
        // PLAN-VICEPARITY-001 S8 (FR-SID-MIXVOL AC-03..08, AC-11):
        // reSID Filter::set_sum_mix() (filter8580new.cc:768-776) computes:
        //   filt  = _filterControl & 0x0F  (per-voice routing from $D417)
        //   mode  = _filterControl & 0xF0  (mode bits from $D418, inc. bit7=3OFF)
        //   sum   = (enabled ? filt : 0x00) & voice_mask
        //   mix   = (enabled ? (mode & 0x70) | (~(filt | (mode&0x80)>>5) & 0x0F) : 0x0F) & voice_mask
        // sum   = filter input voices; mix lower nibble = direct-to-mixer voices.
        // enabled = always true in this managed implementation (no power-down).
        byte filt = (byte)(_filterControl & 0x0F);
        byte mode = (byte)(_filterControl & 0xF0);
        byte vm = _voiceMask;  // default 0x07 (filter8580new.cc:633)

        // sum: voices routed to filter input (filter8580new.cc:772).
        // FR-SID-MIXVOL AC-03: only voice_mask-enabled voices can enter filter.
        byte sum = (byte)(filt & vm);

        // mix lower nibble: direct-to-mixer voices (filter8580new.cc:773-775).
        // 3OFF: (mode & 0x80) >> 5 = 0x04 when bit7 set -> removes voice3 bit.
        // FR-SID-MIXVOL AC-04, AC-05: 3OFF removes voice3 from direct path.
        byte v3offBit = (byte)((mode & 0x80) >> 5);          // 0x04 if 3OFF, 0x00 otherwise
        byte directMask = (byte)((~(filt | v3offBit)) & 0x0F & vm);

        // Build filter input sum from sum-masked voices.
        int filterInput = 0;
        if ((sum & 0x01) != 0) filterInput += voice0;
        if ((sum & 0x02) != 0) filterInput += voice1;
        if ((sum & 0x04) != 0) filterInput += voice2;

        // Build direct-to-mixer (bypass) sum from directMask voices.
        int bypassMix = 0;
        if ((directMask & 0x01) != 0) bypassMix += voice0;
        if ((directMask & 0x02) != 0) bypassMix += voice1;
        if ((directMask & 0x04) != 0) bypassMix += voice2;

        // FR-SID-004 ac.3 / FR-SID-MIXVOL AC-06: LP/BP/HP tap select from
        // mode bits 4-6 ($D418 upper nibble bits 4-6). mix upper nibble =
        // (mode & 0x70) in reSID. When no taps are set, filter is inactive
        // (filter input voices do NOT reach the output; they go into the SVF
        // but with no active tap there is no output path for them).
        // FR-SID-MIXVOL AC-07: no-taps means filtered voices are NOT in output.
        bool lp = (mode & 0x10) != 0;
        bool bp = (mode & 0x20) != 0;
        bool hp = (mode & 0x40) != 0;

        if (!(lp || bp || hp))
        {
            // No mode taps: filter input voices are swallowed (no tap output).
            // Only bypass (direct) voices reach the mixer.
            return bypassMix;
        }

        // FR-SID-004 ac.1 + ac.6: 11-bit cutoff register mapped to Hz
        // through the 6581 non-linear ("kinked") curve, then converted to
        // the Chamberlin SVF coefficient via 2*sin(PI*f/Fs). Note that
        // reg=0 maps to ~200Hz (not true 0), so the LP integrator no
        // longer fully stalls at register zero - it accumulates a small
        // amount of input per sample, matching real 6581 silicon which
        // never has a zero cutoff. We clamp to 0.999 to keep the
        // Chamberlin SVF stable up to coefficient ~1.4; anything beyond
        // pushes the integrators into runaway.
        double cutoffHz = MapCutoffRegToFrequency(_filterCutoff);
        double cutoff = Math.Clamp(2.0 * Math.Sin(Math.PI * cutoffHz / SamplingRate), 0.0, 0.999);

        // FR-SID-004 ac.2: resonance Q feedback from $D417 upper nibble
        // (0..15). Q = 1.0 / (1.0 + res/4) gives a gentle resonance lift
        // at res = 0 and substantial near-self-oscillation at res = 15.
        double q = 1.0 / (1.0 + _filterResonance / 4.0);

        // Chamberlin state-variable filter step. Computing HP from the
        // current LP and BP makes the filter a one-pole-per-tap design
        // with the resonance feedback closing the loop.
        double hpOut = filterInput - _svfLowPass - q * _svfBandPass;
        _svfBandPass += cutoff * hpOut;
        _svfLowPass += cutoff * _svfBandPass;

        // FR-SID-004 ac.7: soft-clip the SVF taps so extreme resonance
        // can't drive the integrators past +/- input range. Real 6581
        // silicon saturates rather than blowing up; tanh-style soft clip
        // captures that. We clip _svfBandPass and _svfLowPass so the
        // resonance feedback path stays bounded.
        if (_svfBandPass > FilterSaturation) _svfBandPass = FilterSaturation;
        else if (_svfBandPass < -FilterSaturation) _svfBandPass = -FilterSaturation;
        if (_svfLowPass > FilterSaturation) _svfLowPass = FilterSaturation;
        else if (_svfLowPass < -FilterSaturation) _svfLowPass = -FilterSaturation;

        double tapSum = 0.0;
        if (lp) tapSum += _svfLowPass;
        if (bp) tapSum += _svfBandPass;
        if (hp) tapSum += hpOut;

        // Bypass voices add post-filter (they were never gated through it).
        double mixed = tapSum + bypassMix;
        return (int)mixed;
    }

    /// <summary>
    /// FR-SID-004 acceptance criterion 7 saturation rail. The state-
    /// variable filter's integrators saturate at this absolute value so
    /// extreme resonance cannot generate NaN/Infinity or drive the
    /// feedback loop past audible levels. Chosen to allow comfortable
    /// signal headroom (8-bit voice * envelope ~ 255 max) while
    /// preventing runaway feedback.
    /// </summary>
    // PLAN-VICEPARITY-001 S8 (FR-SID-VOICE): voice outputs are now 20-bit
    // (no >>8), scaling by 256x. FilterSaturation scales proportionally:
    // 2048 (old 12-bit range) * 256 = 524288. Prevents SVF runaway at high
    // resonance while allowing the full 20-bit signal range to pass.
    private const double FilterSaturation = 524288.0;

    private readonly IBus _bus;

    // PLAN-VICEPARITY-001 S1 (FR-SID-CLOCK): per-cycle clock-chain state.
    // reSID SID::clock() (sid.h:200-244) runs envelopes, oscillators,
    // synchronize, waveform outputs, filter, external filter, the pipelined
    // write slot and bus aging every phi2 cycle; these fields hold the
    // committed chain outputs plus the bus/pipeline tail state. reSID SID
    // constructor seeds bus_value = 0, bus_value_ttl = 0, write_pipeline = 0
    // (sid.cc:80-82); the 6581 write-to-bus TTL is 0x1d00 (sid.cc:119).
    private const int DataBusTtl6581 = 0x1D00;

    /// <summary>
    /// Per-model data-bus fade TTL in cycles (reSID sid.cc:119). The 6581 uses
    /// 0x1d00; the 8580 (0xa2000) overrides this in the S11 slice.
    /// </summary>
    protected virtual int DataBusTtl => DataBusTtl6581;

    /// <summary>
    /// reSID output amplify scaleFactor (sid.cc:105-121, set_chip_model):
    /// 3 for the 6581, 5 for the 8580 - so the 6581 mixes 1.5x louder. reSID
    /// applies it in amplify() at the sample-emission stage (sid.cc:886-888);
    /// S12 wires it into the managed amplify/clip seam. Declared here (not yet
    /// consumed on the per-cycle path) so FR-SID-8580 AC-02 and FR-SID-OUTPUT
    /// AC-07 can lock the per-model constant. Distinct from the filter mixer
    /// divider (filter8580new.cc:400: 6/5), which is baked into BuildResidModel*.
    /// PLAN-VICEPARITY-001 S11.
    /// </summary>
    protected virtual int OutputScaleFactor => 3;

    /// <summary>
    /// reSID clip(int) (sid.cc:42-52): saturating cast to a signed 16-bit
    /// sample. Guards against overflow when the amplified output exceeds the
    /// 16-bit range. PLAN-VICEPARITY-001 S12 (FR-SID-OUTPUT AC-06).
    /// </summary>
    internal static short ClipPcm16(int input)
    {
        if (input > 32767) return 32767;
        if (input < -32768) return -32768;
        return (short)input;
    }

    /// <summary>
    /// reSID amplify(input, scaleFactor) (sid.cc:54-57):
    /// clip((scaleFactor * input) / 2). C# integer division truncates toward
    /// zero exactly like C++. PLAN-VICEPARITY-001 S12 (FR-SID-OUTPUT AC-05).
    /// </summary>
    internal short AmplifyToPcm16(int input) => ClipPcm16((OutputScaleFactor * input) / 2);

    private byte _busValue;
    private int _busValueTtl;
    private byte _writePipeline;
    // reSID SID::write_address (sid.cc:207): the register offset held by the
    // one-cycle write pipeline, committed by ConsumePipelinedWrite. Only used
    // by the 8580 SAMPLE_FAST path. PLAN-VICEPARITY-001 S11.
    private byte _writeAddress;
    private int _cycleVoiceOutput0;
    private int _cycleVoiceOutput1;
    private int _cycleVoiceOutput2;
    private int _cycleFilterOutput;
    private int _cycleExtFilterOutput;

    /// <summary>Shared data-bus value (reSID SID::bus_value). Parity-test seam.</summary>
    internal byte DataBusValue => _busValue;

    /// <summary>Data-bus fade TTL in cycles (reSID SID::bus_value_ttl). Parity-test seam.</summary>
    internal int DataBusValueTtl => _busValueTtl;

    /// <summary>Pipelined-write slot (reSID SID::write_pipeline). Armed only on the 8580 SAMPLE_FAST path. Parity-test seam.</summary>
    internal byte PipelinedWriteSlot => _writePipeline;

    /// <summary>reSID amplify scaleFactor (sid.cc:86/121): 3 (6581) / 5 (8580). Parity-test seam.</summary>
    internal int OutputScaleFactorSeam => OutputScaleFactor;

    /// <summary>reSID wave_zero (voice.cc:93/97): 0x380 (6581) / 0x9e0 (8580). Parity-test seam.</summary>
    internal int WaveZeroLevelSeam => WaveZeroLevel;

    /// <summary>reSID databus_ttl (sid.cc:119): 0x1d00 (6581) / 0xa2000 (8580). Parity-test seam.</summary>
    internal int DataBusTtlSeam => DataBusTtl;

    /// <summary>True when the active filter model is the 8580 (Model8580 + solve_integrate_8580). Parity-test seam.</summary>
    internal bool IsMos8580FilterSeam => IsMos8580Filter;

    /// <summary>
    /// reSID sampling method (sid.h). Default SAMPLE_RESAMPLE, matching x64sc
    /// (sid-resources.c:439-441), so the 8580 one-cycle write pipeline stays
    /// inert in normal operation. The 8580 arms the pipeline only under
    /// <see cref="SidSamplingMethod.Fast"/>. PLAN-VICEPARITY-001 S11.
    /// </summary>
    public SidSamplingMethod SamplingMethod { get; set; } = SidSamplingMethod.Resample;

    /// <summary>The three per-cycle voice outputs fed to the filter stage this cycle. Parity-test seam.</summary>
    internal (int Voice0, int Voice1, int Voice2) CycleVoiceOutputs =>
        (_cycleVoiceOutput0, _cycleVoiceOutput1, _cycleVoiceOutput2);

    /// <summary>The filter stage output committed by the current cycle's chain run. Parity-test seam.</summary>
    internal int CycleFilterOutput => _cycleFilterOutput;

    /// <summary>The external-filter stage output committed this cycle (fed from the filter output). Parity-test seam.</summary>
    internal int CycleExternalFilterOutput => _cycleExtFilterOutput;

    /// <summary>
    /// Per-voice 23-bit noise shift register (reSID wave.h shift_register).
    /// FR-SID-WAVE-TESTBIT AC-08 [PLAN-VICEPARITY-001 S4/S5]. Parity-test seam.
    /// </summary>
    internal uint VoiceShiftRegister(int i) => _voices[i].ShiftRegister;

    /// <summary>
    /// Per-voice shift-register-reset countdown (reSID wave.h shift_register_reset):
    /// cycles remaining to the next shiftreg_bitfade call while test is held.
    /// FR-SID-WAVE-TESTBIT AC-04 [PLAN-VICEPARITY-001 S4/S5]. Parity-test seam.
    /// </summary>
    internal uint VoiceShiftRegisterReset(int i) => _voices[i].ShiftRegisterReset;

    /// <summary>
    /// Per-voice noise-clock pipeline (reSID wave.h shift_pipeline): armed to 2
    /// on bit-19 rise, decremented each cycle, shift register clocks at 0.
    /// FR-SID-WAVE-TESTBIT AC-05 [PLAN-VICEPARITY-001 S4/S5]. Parity-test seam.
    /// </summary>
    internal uint VoiceShiftPipeline(int i) => _voices[i].ShiftPipeline;

    // SID Registers
    private byte[] _registers = new byte[0x20];

    // Voice state
    private Voice[] _voices = new Voice[3];

    // Filter state
    protected int _filterCutoff;
    protected byte _filterResonance;
    protected byte _filterControl;
    protected byte _volume;
    /// <summary>
    /// Voice routing mask (reSID Filter::voice_mask, filter8580new.cc:692-696).
    /// Lower nibble enables which of the four possible filter inputs (voices 0-2
    /// + EXT-IN) participate in the summer and direct-to-mixer paths. Default
    /// 0x07 enables all three internal voices. The upper nibble is always 0xF0
    /// so mode bits always pass through set_sum_mix regardless of voice_mask.
    /// Set once at construction; not reset on chip reset.
    /// FR-SID-MIXVOL AC-08 [PLAN-VICEPARITY-001 S8].
    /// </summary>
    protected byte _voiceMask = 0x07;

    private struct Voice
    {
        public ushort Frequency;
        public ushort PulseWidth;
        public byte Control;
        public byte AttackDecay;
        public byte SustainRelease;
        public uint WaveformAccumulator;
        public uint PulseAccumulator;
        // FR-SID-OSC3ENV3 [PLAN-VICEPARITY-001 S2]: reSID WaveformGenerator
        // readback state. Osc3 is the 12-bit osc3 latch (wave.h:104) written
        // by the per-cycle SetWaveformOutput pass and by control writes;
        // PulseLevel is the one-cycle-delayed 12-bit pulse level pipeline
        // (wave.h:97,516-518), 0x000 or 0xFFF; TriSawPipeline is the 8580
        // tri/saw OSC3 pipeline (wave.h:102-103), power-up seeded 0x555
        // (wave.cc:119) and deliberately untouched by reset (wave.cc:301-332).
        public ushort Osc3;
        // reSID waveform_output (wave.h:113): the CURRENT 12-bit DAC index fed
        // to Voice::output() -> the filter (wave.h:588-592). Distinct from Osc3,
        // which on the 8580 tri/saw path is the one-cycle-delayed OSC3-readback
        // latch (readOSC, wave.cc:293-295). Identical to Osc3 on the 6581 and on
        // the 8580 pulse/noise path. PLAN-VICEPARITY-001 S11 (FR-SID-FILTER-8580).
        public ushort WaveformOutput;
        public ushort PulseLevel;
        public ushort TriSawPipeline;
        /// <summary>
        /// reSID floating_output_ttl: countdown (cycles remaining) until the
        /// next bit-fade; zero means the fade is not armed. Armed to
        /// FloatingOutputTtlStart6581 when a non-zero waveform is deselected
        /// (wave.cc:265-268); re-armed to FloatingOutputTtlBit6581 after each
        /// fade while the latch is nonzero (wave.cc:278-279). FR-SID-OSC3ENV3
        /// AC-07 [PLAN-VICEPARITY-001 S3].
        /// </summary>
        public int FloatingOutputTtl;
        /// <summary>
        /// Per-voice 23-bit noise LFSR (reSID shift_register, wave.h).
        /// Initialized to 0x7FFFFE by reset() (wave.cc:307).
        /// FR-SID-WAVE-TESTBIT AC-08 [PLAN-VICEPARITY-001 S4/S5].
        /// </summary>
        public uint ShiftRegister;
        /// <summary>
        /// Countdown cycles to the next shiftreg_bitfade call while test is held.
        /// Armed to ShiftRegisterResetStart6581 (35000) on test-rising
        /// (writeCONTROL_REG wave.cc:234). FR-SID-WAVE-TESTBIT AC-04.
        /// </summary>
        public uint ShiftRegisterReset;
        /// <summary>
        /// Noise-clock pipeline: set to 2 on bit-19 rising edge, decremented
        /// each cycle, shift register clocks when it hits 0 (reSID shift_pipeline,
        /// wave.h:164-170). Flushed to 0 on test-rising (wave.cc:233).
        /// FR-SID-WAVE-TESTBIT AC-05 [PLAN-VICEPARITY-001 S4/S5].
        /// </summary>
        public uint ShiftPipeline;
        public byte Envelope;          // display/readback mirror of Env.EnvelopeCounter
        public EnvelopeState State;     // display mirror of Env.State
        public bool Gate;
        public bool Reset;
        // Verbatim reSID EnvelopeGenerator (batched clock path). The managed
        // SID is reSID-modelled, so the ADSR rate counter, exponential decay,
        // and the ADSR delay bug come straight from reSID's envelope.cc/.h.
        public ReSidEnvelope Env;
    }

    /// <summary>
    /// Verbatim port of reSID's EnvelopeGenerator (envelope.cc / envelope.h),
    /// batched clock(delta_t) path - the path the native lockstep oracle uses
    /// via SAMPLE_FAST clock_fast. Ported field-for-field so managed ENV3 is
    /// cycle-exact with native reSID (SidEngineParityTests). The 15-bit rate
    /// counter is never reset on ATK/DCY/SUS/REL writes (the famous ADSR delay
    /// bug, FR-SID-006); the exponential counter divides the decay/release clock
    /// at envelope levels 255/93/54/26/14/6 to approximate an exponential.
    /// </summary>
    internal struct ReSidEnvelope
    {
        public enum EnvStateE : byte { Attack, DecaySustain, Release, Freezed }

        // rate_counter_period[16] - reSID values (the actual comparison values).
        private static readonly int[] RatePeriods =
            { 8, 31, 62, 94, 148, 219, 266, 312, 391, 976, 1953, 3125, 3906, 11719, 19531, 31250 };
        // sustain_level[16] - both nibbles of the 4-bit sustain value.
        private static readonly byte[] SustainLevels =
            { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff };

        public int RateCounter;
        public int RatePeriod;
        public byte ExponentialCounter;
        public byte ExponentialCounterPeriod;
        public byte EnvelopeCounter;
        public byte Env3;   // ENV3 ($1C) readback: envelope_counter sampled at clock start
        public bool HoldZero;
        public bool ResetRateCounter;
        public byte Attack, Decay, Sustain, Release;
        public byte Gate;
        public EnvStateE State;
        public EnvStateE NextState;
        public int StatePipeline;
        public int EnvelopePipeline;
        public int ExponentialPipeline;

        /// <summary>
        /// reSID EnvelopeGenerator constructor state (envelope.cc:159-182):
        /// the counter's odd bits are high on power-up (envelope_counter =
        /// 0xaa, envelope.cc:176), next_state parks at RELEASE
        /// (envelope.cc:179), then reset() runs and deliberately preserves
        /// the counter. FR-SID-ENV AC-08 [PLAN-VICEPARITY-001 S1].
        /// </summary>
        public void PowerUp()
        {
            EnvelopeCounter = 0xaa;
            NextState = EnvStateE.Release;
            Reset();
        }

        public void Reset()
        {
            // envelope_counter (and the env3 latch) are deliberately NOT
            // touched: "counter is not changed on reset" (envelope.cc:189).
            // FR-SID-ENV AC-07 [PLAN-VICEPARITY-001 S1].
            EnvelopePipeline = 0;
            ExponentialPipeline = 0;
            StatePipeline = 0;
            Attack = Decay = Sustain = Release = 0;
            Gate = 0;
            RateCounter = 0;
            ExponentialCounter = 0;
            ExponentialCounterPeriod = 1;
            ResetRateCounter = false;
            State = EnvStateE.Release;
            NextState = EnvStateE.Release;
            RatePeriod = RatePeriods[Release];
            HoldZero = false;
        }

        public void WriteControl(byte control)
        {
            byte gateNext = (byte)(control & 0x01);
            if (Gate == gateNext)
                return;

            NextState = gateNext != 0 ? EnvStateE.Attack : EnvStateE.Release;
            if (NextState == EnvStateE.Attack)
            {
                // The decay register is "accidentally" activated during the first
                // cycle of the attack phase.
                State = EnvStateE.DecaySustain;
                RatePeriod = RatePeriods[Decay];
                StatePipeline = 2;
                if (ResetRateCounter || ExponentialPipeline == 2)
                    EnvelopePipeline = (ExponentialCounterPeriod == 1 || ExponentialPipeline == 2) ? 2 : 4;
                else if (ExponentialPipeline == 1)
                    StatePipeline = 3;
            }
            else
            {
                StatePipeline = EnvelopePipeline > 0 ? 3 : 2;
            }
            Gate = gateNext;
        }

        public void WriteAttackDecay(byte attackDecay)
        {
            Attack = (byte)((attackDecay >> 4) & 0x0f);
            Decay = (byte)(attackDecay & 0x0f);
            if (State == EnvStateE.Attack)
                RatePeriod = RatePeriods[Attack];
            else if (State == EnvStateE.DecaySustain)
                RatePeriod = RatePeriods[Decay];
        }

        public void WriteSustainRelease(byte sustainRelease)
        {
            Sustain = (byte)((sustainRelease >> 4) & 0x0f);
            Release = (byte)(sustainRelease & 0x0f);
            if (State == EnvStateE.Release)
                RatePeriod = RatePeriods[Release];
        }

        private void SetExponentialCounter()
        {
            switch (EnvelopeCounter)
            {
                case 0xff: ExponentialCounterPeriod = 1; break;
                case 0x5d: ExponentialCounterPeriod = 2; break;
                case 0x36: ExponentialCounterPeriod = 4; break;
                case 0x1a: ExponentialCounterPeriod = 8; break;
                case 0x0e: ExponentialCounterPeriod = 16; break;
                case 0x06: ExponentialCounterPeriod = 30; break;
                case 0x00:
                    ExponentialCounterPeriod = 1;
                    // When the envelope counter reaches zero it freezes there.
                    HoldZero = true;
                    break;
            }
        }

        /// <summary>
        /// reSID EnvelopeGenerator::clock() - single cycle. This is the path
        /// SAMPLE_RESAMPLE / SAMPLE_INTERPOLATE use (the shim oracle defaults to
        /// RESAMPLING), and it carries the envelope/state pipelines and the +1
        /// rate-counter reset delay that the batched clock drops - which is why
        /// the effective attack period is longer than rate_counter_period[attack].
        /// </summary>
        public void Clock()
        {
            // ENV3 is sampled at the first phase of the clock.
            Env3 = EnvelopeCounter;

            if (StatePipeline != 0)
                StateChange();

            // If the exponential counter period != 1, the envelope decrement is
            // delayed one cycle. Only modeled for single-cycle clocking.
            if (EnvelopePipeline != 0 && --EnvelopePipeline == 0)
            {
                if (!HoldZero)
                {
                    if (State == EnvStateE.Attack)
                    {
                        EnvelopeCounter = (byte)((EnvelopeCounter + 1) & 0xff);
                        if (EnvelopeCounter == 0xff)
                        {
                            State = EnvStateE.DecaySustain;
                            RatePeriod = RatePeriods[Decay];
                        }
                    }
                    else if (State == EnvStateE.DecaySustain || State == EnvStateE.Release)
                    {
                        EnvelopeCounter = (byte)((EnvelopeCounter - 1) & 0xff);
                    }
                    SetExponentialCounter();
                }
            }

            if (ExponentialPipeline != 0 && --ExponentialPipeline == 0)
            {
                ExponentialCounter = 0;
                if ((State == EnvStateE.DecaySustain && EnvelopeCounter != SustainLevels[Sustain])
                    || State == EnvStateE.Release)
                {
                    EnvelopePipeline = 1;
                }
            }
            else if (ResetRateCounter)
            {
                RateCounter = 0;
                ResetRateCounter = false;

                if (State == EnvStateE.Attack)
                {
                    // The first attack step also resets the exponential counter.
                    ExponentialCounter = 0;
                    EnvelopePipeline = 2;
                }
                else
                {
                    if (!HoldZero && ++ExponentialCounter == ExponentialCounterPeriod)
                        ExponentialPipeline = ExponentialCounterPeriod != 1 ? 2 : 1;
                }
            }

            // ADSR delay bug: when the comparison value is below the current
            // counter, the counter wraps at 0x8000 before the envelope steps.
            if (RateCounter != RatePeriod)
            {
                RateCounter++;
                if ((RateCounter & 0x8000) != 0)
                    RateCounter = (RateCounter + 1) & 0x7fff;
            }
            else
            {
                ResetRateCounter = true;
            }
        }

        private void StateChange()
        {
            StatePipeline--;
            switch (NextState)
            {
                case EnvStateE.Attack:
                    if (StatePipeline == 0)
                    {
                        State = EnvStateE.Attack;
                        RatePeriod = RatePeriods[Attack];
                        HoldZero = false;
                    }
                    break;
                case EnvStateE.DecaySustain:
                    break;
                case EnvStateE.Release:
                    if ((State == EnvStateE.Attack && StatePipeline == 0)
                        || (State == EnvStateE.DecaySustain && StatePipeline == 1))
                    {
                        State = EnvStateE.Release;
                        RatePeriod = RatePeriods[Release];
                    }
                    break;
                case EnvStateE.Freezed:
                    break;
            }
        }
    }

    private readonly IAudioBackend? _audioBackend;
    private readonly float[] _sampleBuffer = new float[256];
    private int _sampleBufferLen;

    // Audio sample-rate downconversion. The SID is clocked (Tick) at
    // masterClockHz / ClockDivisor; output runs at SamplingRate (44.1 kHz).
    // _audioTicksPerSample is how many Tick()s elapse per emitted sample
    // (e.g. PAL: (985248/1)/44100 ~= 22.34). A fractional accumulator emits
    // one sample whenever it crosses that threshold, sampling the evolving
    // synthesis state. Zero (the default) disables emission entirely - so a
    // SID built without an audio backend behaves exactly as before and never
    // touches the audio path (preserving native cycle parity).
    private double _audioTicksPerSample;
    private double _audioSampleAccumulator;
    private double _audioMasterClockHz;
    private double _audioRelativeSpeedPercent = 100.0;

    public Sid6581(IBus bus) : this(bus, audioBackend: null) { }

    public Sid6581(IBus bus, IAudioBackend? audioBackend)
    {
        _bus = bus;
        _audioBackend = audioBackend;
        // FR-SID-ENV AC-08 [PLAN-VICEPARITY-001 S1]: power-up seeds each
        // envelope counter to 0xaa (envelope.cc:176) via the reSID
        // constructor sequence; the display mirrors reflect the seed so the
        // capture surface is correct before the first Tick.
        for (var v = 0; v < _voices.Length; v++)
        {
            _voices[v].Env.PowerUp();
            _voices[v].Envelope = _voices[v].Env.EnvelopeCounter;
            _voices[v].State = EnvelopeState.Release;
            // FR-SID-OSC3ENV3 AC-06 [PLAN-VICEPARITY-001 S2]: the 8580
            // tri/saw OSC3 pipeline powers up with the even bits high
            // (tri_saw_pipeline = 0x555, wave.cc:119; seeded on every die,
            // consumed only by the 8580 OSC3 path). The pulse level pipeline
            // powers up high via the constructor's reset() (wave.cc:319).
            _voices[v].TriSawPipeline = 0x555;
            _voices[v].PulseLevel = 0xFFF;
            // FR-SID-WAVE-ACC AC-05 [PLAN-VICEPARITY-001 S3]: reSID powers
            // up the accumulator with even bits high (accumulator = 0x555555,
            // wave.cc:117). Shared by both 6581 and 8580.
            _voices[v].WaveformAccumulator = 0x555555;
            // FR-SID-WAVE-TESTBIT AC-08 [PLAN-VICEPARITY-001 S4/S5]: per-voice
            // shift register initialized by the constructor's implicit reset():
            // shift_register = 0x7ffffe (wave.cc:307), shift_register_reset = 0,
            // shift_pipeline not explicitly set by constructor (starts at 0).
            _voices[v].ShiftRegister = 0x7FFFFEu;
            _voices[v].ShiftRegisterReset = 0;
            _voices[v].ShiftPipeline = 0;
        }

        // reSID seeds the 8580 DAC gate overdrive (nVgt) at model-build /
        // init time (filter8580new.cc:603) and its Filter::reset() never clears
        // it. Mirror that here: seed _nVgt at construction so the 8580
        // integrator is valid before the first Reset() (the lockstep oracle
        // likewise has nVgt set at init, surviving SidExactReset). For the 6581
        // NVgtDefault is 0 (the 6581 integrator does not use it), so this is a
        // no-op there. PLAN-VICEPARITY-001 S11 (FR-SID-FILTER-8580).
        _nVgt = FilterModel.NVgtDefault;
    }

    /// <summary>
    /// Configure live-audio emission for the given system master clock (Hz).
    /// Enables per-Tick sample generation at <see cref="SamplingRate"/> so the
    /// SID streams to its audio backend during emulation. No effect unless an
    /// audio backend was supplied at construction.
    /// </summary>
    public void ConfigureAudioClock(double masterClockHz)
    {
        if (masterClockHz <= 0.0)
        {
            _audioMasterClockHz = 0.0;
            _audioTicksPerSample = 0.0;
            return;
        }

        _audioMasterClockHz = masterClockHz;
        RecomputeAudioCadence();
        _audioSampleAccumulator = 0.0;
    }

    /// <summary>
    /// Set the relative emulation speed for live audio (percent), mirroring
    /// VICE sound_set_relative_speed (sound.c:1799-1817): the sample step
    /// scales to clkstep = speed/100 * cycles_per_sec / sample_rate
    /// (sound.c:1067), so the fixed-rate device drains one emulated second
    /// of audio in 100/speed wall seconds and back-pressure paces emulation
    /// to the requested rate (pitch shifts with speed, like VICE
    /// fast-forward). Non-positive values are ignored; values above 200 are
    /// clamped (past double speed the host suspends live output instead,
    /// TR-AUDIO-WARP-001). Synthesis state is untouched - only the emission
    /// cadence changes - so parity and determinism are unaffected.
    /// </summary>
    public void SetRelativeSpeed(double speedPercent)
    {
        if (speedPercent <= 0.0)
            return;

        _audioRelativeSpeedPercent = Math.Min(speedPercent, 200.0);
        if (_audioMasterClockHz > 0.0)
            RecomputeAudioCadence();
    }

    private void RecomputeAudioCadence()
    {
        var sidTickHz = _audioMasterClockHz / ClockDivisor;
        _audioTicksPerSample = _audioRelativeSpeedPercent / 100.0 * sidTickHz / SamplingRate;
    }

    /// <summary>
    /// Generate a sample + push it to the configured audio backend (if any).
    /// Buffers samples internally and flushes in batches of 256 to amortise
        /// backend overhead. Call once per audio-sample tick.
    /// </summary>
    public void GenerateSampleAndOutput()
    {
        var sample = GenerateSample();
        if (_audioBackend is null) return;

        _sampleBuffer[_sampleBufferLen++] = sample;
        if (_sampleBufferLen >= _sampleBuffer.Length)
        {
            _audioBackend.SubmitSamples(_sampleBuffer.AsSpan(0, _sampleBufferLen));
            _sampleBufferLen = 0;
        }
    }

    /// <summary>Flush any partial sample buffer to the audio backend.</summary>
    public void FlushAudioBuffer()
    {
        if (_audioBackend is null || _sampleBufferLen == 0) return;
        _audioBackend.SubmitSamples(_sampleBuffer.AsSpan(0, _sampleBufferLen));
        _sampleBufferLen = 0;
    }

    public void Tick()
    {
        // PLAN-VICEPARITY-001 S1 (FR-SID-CLOCK AC-01/AC-09): reSID's
        // single-cycle SID::clock() chain (sid.h:200-244), in exact stage
        // order: 1. amplitude modulators, 2. oscillators, 3. synchronize,
        // 4. waveform outputs, 5. filter, 6. external filter, 7. pipelined
        // write slot, 8. bus aging.
        ClockEnvelopes();
        ClockOscillators();
        SynchronizeOscillators();
        ComputeWaveformOutputs();
        ClockFilterChain();
        ConsumePipelinedWrite();
        AgeBusValue();

        // Live-audio emission: once a backend is attached and the audio clock is
        // configured, sample the committed chain output at 44.1 kHz via a
        // fractional tick:sample accumulator. Inert (no allocation, no call)
        // when audio is not configured, so parity rigs are unaffected.
        if (_audioBackend is not null && _audioTicksPerSample > 0.0)
        {
            _audioSampleAccumulator += 1.0;
            if (_audioSampleAccumulator >= _audioTicksPerSample)
            {
                _audioSampleAccumulator -= _audioTicksPerSample;
                GenerateSampleAndOutput();
            }
        }
    }

    /// <summary>
    /// Grouped envelope pass (reSID sid.h:205-208): clock all three
    /// amplitude modulators before any oscillator advances. The passes are
    /// order-independent within a cycle (FR-SID-CLOCK AC-11), so this
    /// mirrors the reSID dispatch structure exactly.
    /// </summary>
    private void ClockEnvelopes()
    {
        for (int i = 0; i < 3; i++)
        {
            ProcessEnvelope(ref _voices[i]);
        }
    }

    // Per-voice msb_rising flags (reSID wave.h:160): true when bit 23 transitions
    // 0->1 this cycle. Set by ClockOscillators, consumed by SynchronizeOscillators.
    // FR-SID-WAVE-SYNC AC-01 [PLAN-VICEPARITY-001 S4/S5].
    private bool _msbRising0;
    private bool _msbRising1;
    private bool _msbRising2;

    /// <summary>
    /// Grouped oscillator pass (reSID sid.h:210-213 + wave.h:142-172): capture
    /// the pre-advance MSBs for msb_rising detection, advance every phase
    /// accumulator, manage the per-voice noise-clock pipeline, handle the
    /// test-bit-held path (accumulator held 0, slow shift-register-reset counter,
    /// pulse forced high), and clock the shared noise LFSR on bit-19 rising edges.
    /// FR-SID-WAVE-SYNC AC-01 [PLAN-VICEPARITY-001 S4/S5]: msb_rising replaces
    /// the old falling-edge _prevMsb fields.
    /// FR-SID-WAVE-TESTBIT AC-04/AC-05 [PLAN-VICEPARITY-001 S4/S5]: per-voice
    /// shift_register_reset countdown and shift_pipeline management.
    /// </summary>
    private void ClockOscillators()
    {
        // Capture pre-advance MSBs for the rising-edge (msb_rising) computation.
        bool prevMsb0 = (_voices[0].WaveformAccumulator & 0x800000u) != 0;
        bool prevMsb1 = (_voices[1].WaveformAccumulator & 0x800000u) != 0;
        bool prevMsb2 = (_voices[2].WaveformAccumulator & 0x800000u) != 0;

        for (int i = 0; i < 3; i++)
        {
            ref Voice voice = ref _voices[i];

            if ((voice.Control & 0x08) != 0)
            {
                // Test bit held (wave.h:144-152, FR-SID-WAVE-TESTBIT AC-04/AC-05):
                // accumulator stays 0, shift_register_reset counts down, pulse forced high.
                voice.WaveformAccumulator = 0;
                if (voice.ShiftRegisterReset != 0 && --voice.ShiftRegisterReset == 0)
                {
                    ShiftRegBitFade(ref voice);
                }
                voice.PulseLevel = 0xFFF;
                continue;
            }

            // Normal accumulator advance (wave.h:155-157).
            // FR-SID-WAVE-ACC AC-02 [PLAN-VICEPARITY-001 S3]: mask to 24 bits each cycle.
            uint prevAcc = voice.WaveformAccumulator;
            voice.WaveformAccumulator = (prevAcc + voice.Frequency) & 0xFFFFFF;
            uint bitsSet = ~prevAcc & voice.WaveformAccumulator;

            // Per-voice noise-clock pipeline (wave.h:164-170, FR-SID-WAVE-TESTBIT AC-05):
            // arm to 2 on bit-19 rising edge, decrement each cycle, clock LFSR at 0.
            if ((bitsSet & 0x080000) != 0)
            {
                voice.ShiftPipeline = 2;
            }
            else if (voice.ShiftPipeline != 0 && --voice.ShiftPipeline == 0)
            {
                ClockVoiceShiftRegister(ref voice);
            }
        }

        // Compute msb_rising for each voice (wave.h:159-160): 0->1 transition of bit 23.
        // FR-SID-WAVE-SYNC AC-01 [PLAN-VICEPARITY-001 S4/S5].
        _msbRising0 = !prevMsb0 && (_voices[0].WaveformAccumulator & 0x800000u) != 0;
        _msbRising1 = !prevMsb1 && (_voices[1].WaveformAccumulator & 0x800000u) != 0;
        _msbRising2 = !prevMsb2 && (_voices[2].WaveformAccumulator & 0x800000u) != 0;
    }

    /// <summary>
    /// reSID clock_shift_register (wave.h:321-329): advance the per-voice 23-bit
    /// noise LFSR one step with feedback from bits 22 and 17 (XOR), masked to
    /// 23 bits. Clocked by the bit-19 rising edge (2-cycle pipeline via
    /// ShiftPipeline). The audio noise output is derived from this register via
    /// NoiseOutput12 in SetWaveformOutput. FR-SID-WAVE-NOISE AC-05/AC-06/AC-07
    /// [PLAN-VICEPARITY-001 S6].
    /// </summary>
    private static void ClockVoiceShiftRegister(ref Voice voice)
    {
        uint bit0 = ((voice.ShiftRegister >> 22) ^ (voice.ShiftRegister >> 17)) & 1u;
        voice.ShiftRegister = ((voice.ShiftRegister << 1) | bit0) & 0x7FFFFFu;
    }

    /// <summary>
    /// reSID shiftreg_bitfade (wave.cc:282-291): OR-fill the SR upward by one
    /// step (`SR |= 1; SR |= SR &lt;&lt; 1`), with NO 23-bit masking (wave.cc:284-285
    /// uses raw `unsigned int` arithmetic). Starting from 0x7ffffe the result is
    /// 0xffffff (24-bit all-ones, NOT 0x7fffff). Since 0xffffff != 0x7fffff the
    /// re-arm condition fires again (wave.cc:289-290), keeping SRR alive.
    /// FR-SID-WAVE-TESTBIT AC-04 [PLAN-VICEPARITY-001 S4/S5].
    /// FR-SID-WAVE-NOISE AC-14 [PLAN-VICEPARITY-001 S6]: removed incorrect mask.
    /// </summary>
    private static void ShiftRegBitFade(ref Voice voice)
    {
        voice.ShiftRegister |= 1u;
        voice.ShiftRegister |= voice.ShiftRegister << 1;   // no 23-bit mask (wave.cc:285)
        if (voice.ShiftRegister != 0x7FFFFFu)
        {
            voice.ShiftRegisterReset = ShiftRegisterResetBit6581;
        }
    }

    /// <summary>
    /// reSID write_shift_register (wave.h:331-351): when a combined waveform
    /// (noise + any other) is selected, the current waveform output is AND'd
    /// back into the shift register tap positions. A bit once zeroed in the
    /// output stays zero in the SR forever, gradually corrupting (decaying to 0)
    /// the register. Also updates the 12-bit noise_output mask in reSID; in
    /// managed code the noise output is re-derived from SR each cycle so no
    /// separate mask field is needed.
    /// FR-SID-WAVE-NOISE AC-11/AC-16/AC-17/AC-18 [PLAN-VICEPARITY-001 S6].
    /// </summary>
    private static void WriteShiftRegister(ref Voice voice, int waveformOutput)
    {
        // wave.h:339-348: AND the waveform output bits back into SR tap positions.
        // Tap-clear mask: ~(bit20|bit18|bit14|bit11|bit9|bit5|bit2|bit0) = 0xFFEBB5DAu
        voice.ShiftRegister &=
            0xFFEBB5DAu                                                                   // clear all 8 tap positions
            | ((uint)(waveformOutput & 0x800) << 9)   // waveform bit 11 -> SR bit 20
            | ((uint)(waveformOutput & 0x400) << 8)   // waveform bit 10 -> SR bit 18
            | ((uint)(waveformOutput & 0x200) << 5)   // waveform bit  9 -> SR bit 14
            | ((uint)(waveformOutput & 0x100) << 3)   // waveform bit  8 -> SR bit 11
            | ((uint)(waveformOutput & 0x080) << 2)   // waveform bit  7 -> SR bit  9
            | ((uint)(waveformOutput & 0x040) >> 1)   // waveform bit  6 -> SR bit  5
            | ((uint)(waveformOutput & 0x020) >> 3)   // waveform bit  5 -> SR bit  2
            | ((uint)(waveformOutput & 0x010) >> 4);  // waveform bit  4 -> SR bit  0
    }

    /// <summary>
    /// Grouped synchronize pass (reSID sid.h:215-218 + wave.h:255-264): all
    /// oscillators have clocked so every sync decision sees this cycle's parallel
    /// post-advance state. Fires on the RISING MSB edge (msb_rising set by
    /// ClockOscillators) with the same-cycle sync-source special case:
    /// voice[i] does NOT reset voice[(i+1)%3] if voice[i] itself has SYNC set
    /// AND its own source voice[(i+2)%3] also has its MSB rising this cycle
    /// (reSID wave.h:255-264: "!(sync and sync_source->msb_rising)").
    /// FR-SID-WAVE-SYNC AC-01/AC-04 [PLAN-VICEPARITY-001 S4/S5].
    /// </summary>
    private void SynchronizeOscillators()
    {
        bool sync0 = (_voices[0].Control & 0x02) != 0;
        bool sync1 = (_voices[1].Control & 0x02) != 0;
        bool sync2 = (_voices[2].Control & 0x02) != 0;

        // voice[0] fires on voice[1]: suppress if voice[0].sync AND voice[2].msb_rising.
        if (_msbRising0 && sync1 && !(sync0 && _msbRising2))
            _voices[1].WaveformAccumulator = 0;

        // voice[1] fires on voice[2]: suppress if voice[1].sync AND voice[0].msb_rising.
        if (_msbRising1 && sync2 && !(sync1 && _msbRising0))
            _voices[2].WaveformAccumulator = 0;

        // voice[2] fires on voice[0]: suppress if voice[2].sync AND voice[1].msb_rising.
        if (_msbRising2 && sync0 && !(sync2 && _msbRising1))
            _voices[0].WaveformAccumulator = 0;
    }

    /// <summary>
    /// Pipelined-write slot of the per-cycle chain (reSID sid.h:236-237): after
    /// the filter and external filter clock, a pending 8580 SAMPLE_FAST write is
    /// committed via <see cref="CommitWrite"/> using the latched write_address /
    /// bus_value, then the slot clears (reSID's write() ends write_pipeline=0).
    /// The 6581 (and the 8580 outside SAMPLE_FAST) never arm the slot, so this
    /// is a no-op there; the chain still checks it every cycle, as reSID does.
    /// PLAN-VICEPARITY-001 S11 (FR-SID-8580 AC-05/AC-06).
    /// </summary>
    private void ConsumePipelinedWrite()
    {
        if (_writePipeline != 0)
        {
            _writePipeline = 0;
            CommitWrite(_writeAddress, _busValue);
        }
    }

    /// <summary>
    /// Bus aging, the final chain stage (reSID sid.h:236-239):
    /// if (!--bus_value_ttl) bus_value = 0. The decrement deliberately
    /// continues past zero exactly like reSID's cycle_count arithmetic; any
    /// write reloads the ttl (sid.cc:207-209).
    /// </summary>
    private void AgeBusValue()
    {
        if (--_busValueTtl == 0)
        {
            _busValue = 0;
        }
    }

    private void ProcessEnvelope(ref Voice voice)
    {
        // Advance the reSID envelope one master cycle (batched clock with
        // delta_t = 1, matching the native oracle's SAMPLE_FAST clock_fast at
        // 1:1). Gate transitions and rate changes are applied by Env.Write*
        // from Write(); here we only clock and mirror the result for output,
        // readback, and the debugger snapshot.
        voice.Env.Clock();
        voice.Envelope = voice.Env.EnvelopeCounter;
        voice.State = voice.Env.State switch
        {
            ReSidEnvelope.EnvStateE.Attack => EnvelopeState.Attack,
            ReSidEnvelope.EnvStateE.DecaySustain => EnvelopeState.Decay,
            ReSidEnvelope.EnvStateE.Release => EnvelopeState.Release,
            _ => EnvelopeState.Idle,
        };
    }

    public void Reset()
    {
        Array.Clear(_registers);
        // reSID SID::reset() fans out to voice.reset() = wave.reset() +
        // envelope.reset() (voice.cc:121-125). envelope.reset() preserves
        // the envelope counter and the env3 latch (envelope.cc:189;
        // FR-SID-ENV AC-07), so the voices are reset field-by-field instead
        // of being zeroed wholesale.
        for (var v = 0; v < _voices.Length; v++)
        {
            ref var voice = ref _voices[v];
            voice.Frequency = 0;
            voice.PulseWidth = 0;
            voice.Control = 0;
            voice.AttackDecay = 0;
            voice.SustainRelease = 0;
            // FR-SID-WAVE-ACC AC-06 [PLAN-VICEPARITY-001 S3]: reSID
            // wave.reset() deliberately leaves the accumulator alone
            // ("accumulator is not changed on reset", wave.cc:301-303).
            voice.PulseAccumulator = 0;
            // FR-SID-OSC3ENV3 [PLAN-VICEPARITY-001 S2]: reSID wave.reset()
            // clears the osc3 latch (osc3 = 0, wave.cc:330) and parks the
            // pulse level high (pulse_output = 0xfff, wave.cc:319), while
            // tri_saw_pipeline is deliberately NOT touched by reset
            // (wave.cc:301-332 never assigns it), so it is preserved here.
            // wave.cc:551 reset(): osc3 = waveform_output = 0.
            voice.Osc3 = 0;
            voice.WaveformOutput = 0;
            voice.PulseLevel = 0xFFF;
            // FR-SID-OSC3ENV3 AC-07 [PLAN-VICEPARITY-001 S3]: reset also
            // clears the floating-DAC TTL (floating_output_ttl = 0,
            // wave.cc:331).
            voice.FloatingOutputTtl = 0;
            // FR-SID-WAVE-TESTBIT AC-08 [PLAN-VICEPARITY-001 S4/S5]: reset()
            // sets shift_register = 0x7ffffe (wave.cc:307) and clears the
            // reset counter and pipeline.
            voice.ShiftRegister = 0x7FFFFEu;
            voice.ShiftRegisterReset = 0;
            voice.ShiftPipeline = 0;
            voice.Gate = false;
            voice.Reset = false;
            voice.Env.Reset();
            voice.Envelope = voice.Env.EnvelopeCounter;
            voice.State = EnvelopeState.Release;
        }
        _filterCutoff = 0;
        _filterResonance = 0;
        _filterControl = 0;
        _volume = 0;
        _svfLowPass = 0.0;
        _svfBandPass = 0.0;
        if (UsesReSidFilter)
            ResetFilter6581();
        // reSID SID::reset() clears the data bus (sid.cc:142-143); the
        // per-cycle chain outputs restart from silence.
        _busValue = 0;
        _busValueTtl = 0;
        _writePipeline = 0;
        _cycleVoiceOutput0 = 0;
        _cycleVoiceOutput1 = 0;
        _cycleVoiceOutput2 = 0;
        _cycleFilterOutput = 0;
        _cycleExtFilterOutput = 0;
        _msbRising0 = false;
        _msbRising1 = false;
        _msbRising2 = false;
    }

    public byte Read(ushort address)
    {
        int register = address & 0x1F;

        // reSID SID::read (sid.cc:176-197) [PLAN-VICEPARITY-001 S10,
        // FR-SID-POT / FR-SID-DATABUS]: $19/$1A latch the POT stub (0xff,
        // pot.cc:25-29), $1B/$1C latch the OSC3/ENV3 readback, and every read
        // returns the shared data bus. The register file is never read back;
        // unreadable and write-only registers surface the last value driven onto
        // the fading bus (bus_value), which ages to zero DataBusTtl cycles later.
        switch (register)
        {
            case 0x19: // POTX
            case 0x1A: // POTY
                _busValue = 0xFF;
                _busValueTtl = DataBusTtl;
                break;
            case 0x1B: // OSC3: voice-3 selected-waveform readback (osc3 >> 4,
                       // wave.cc:293-296).
                _busValue = (byte)(_voices[2].Osc3 >> 4);
                _busValueTtl = DataBusTtl;
                break;
            case 0x1C: // ENV3: voice-3 envelope readback (counter sampled at the
                       // first phase of the cycle).
                _busValue = _voices[2].Env.Env3;
                _busValueTtl = DataBusTtl;
                break;
            default:
                break;
        }

        return _busValue;
    }

    public void Write(ushort address, byte value)
    {
        int register = address & 0x1F;
        _registers[register] = value;

        // reSID SID::write (sid.cc:205-219): every write drives the shared
        // data bus; the value fades to zero databus_ttl cycles later (0x1d00
        // on the 6581, sid.cc:119). The register offset is latched into the
        // write pipeline slot; on the 8580 under SAMPLE_FAST the register
        // EFFECT is deferred one cycle (SID-detection quirk), otherwise it is
        // committed immediately. PLAN-VICEPARITY-001 S11 (FR-SID-8580 AC-04).
        _writeAddress = (byte)register;
        _busValue = value;
        _busValueTtl = DataBusTtl;

        if (SamplingMethod == SidSamplingMethod.Fast && IsMos8580Wave)
        {
            // Fake one-cycle pipeline delay on the MOS8580 (sid.cc:211-216).
            _writePipeline = 1;
        }
        else
        {
            CommitWrite(register, value);
        }
    }

    /// <summary>
    /// Apply a register's side effects (reSID SID::write(), sid.cc:226+). Called
    /// immediately by <see cref="Write"/> on the 6581 and on the 8580 outside
    /// SAMPLE_FAST, or one cycle later by <see cref="ConsumePipelinedWrite"/>
    /// when the 8580 SAMPLE_FAST pipeline flushes. PLAN-VICEPARITY-001 S11.
    /// </summary>
    private void CommitWrite(int register, byte value)
    {
        int voiceIndex = register / 7;

        if (voiceIndex < 3)
        {
            switch (register % 7)
            {
                case 0:
                    _voices[voiceIndex].Frequency = (ushort)((_voices[voiceIndex].Frequency & 0xFF00) | value);
                    break;
                case 1:
                    _voices[voiceIndex].Frequency = (ushort)((_voices[voiceIndex].Frequency & 0x00FF) | (value << 8));
                    break;
                case 2:
                    _voices[voiceIndex].PulseWidth = (ushort)((_voices[voiceIndex].PulseWidth & 0xFF00) | value);
                    // FR-SID-OSC3ENV3 AC-03 [PLAN-VICEPARITY-001 S2]: a pulse
                    // width write pushes the next pulse level immediately
                    // (reSID writePW_LO, wave.cc:158-163).
                    PushPulseLevel(voiceIndex);
                    break;
                case 3:
                    _voices[voiceIndex].PulseWidth = (ushort)((_voices[voiceIndex].PulseWidth & 0x00FF) | ((value & 0x0F) << 8));
                    // reSID writePW_HI pushes the next pulse level immediately
                    // (wave.cc:165-170).
                    PushPulseLevel(voiceIndex);
                    break;
                case 4:
                    {
                        byte prevCtrl = _voices[voiceIndex].Control;
                        var prevWaveformBits = prevCtrl & 0xF0;
                        bool prevTest = (prevCtrl & 0x08) != 0;
                        bool newTest = (value & 0x08) != 0;

                        _voices[voiceIndex].Control = value;
                        _voices[voiceIndex].Gate = (value & 0x01) != 0;
                        // reSID gate/state transition (attack on gate-on, release on gate-off).
                        _voices[voiceIndex].Env.WriteControl(value);

                        if (!prevTest && newTest)
                        {
                            // Test bit rising (wave.cc:229-241, FR-SID-WAVE-TESTBIT AC-04/AC-05):
                            // accumulator = 0, shift_pipeline = 0,
                            // shift_register_reset = SHIFT_REGISTER_RESET_START_6581, pulse_output = 0xfff.
                            _voices[voiceIndex].WaveformAccumulator = 0;
                            _voices[voiceIndex].ShiftPipeline = 0;
                            _voices[voiceIndex].ShiftRegisterReset = ShiftRegisterResetStart6581;
                            _voices[voiceIndex].PulseLevel = 0xFFF;
                        }
                        else if (prevTest && !newTest)
                        {
                            // Test bit falling (wave.cc:242-259, FR-SID-WAVE-TESTBIT AC-06):
                            // single clock of the shift register with bit0 = NOT(bit17).
                            // Comment in wave.cc: "bit0 = (bit22 | test) ^ bit17 = 1 ^ bit17 = ~bit17"
                            ref Voice vt = ref _voices[voiceIndex];
                            uint bit0 = (~vt.ShiftRegister >> 17) & 1u;
                            vt.ShiftRegister = ((vt.ShiftRegister << 1) | bit0) & 0x7FFFFFu;
                        }

                        // FR-SID-OSC3ENV3 AC-01/AC-06 [PLAN-VICEPARITY-001 S2]:
                        // a control write with a waveform selected refreshes the
                        // waveform output and the osc3 latch immediately (reSID
                        // writeCONTROL_REG, wave.cc:261-264).
                        if ((value & 0xF0) != 0)
                        {
                            SetWaveformOutput(voiceIndex);
                        }
                        else if (prevWaveformBits != 0)
                        {
                            // FR-SID-OSC3ENV3 AC-07 [PLAN-VICEPARITY-001 S3]:
                            // deselecting all waveforms while a waveform was
                            // previously selected arms the floating-DAC fade TTL
                            // (reSID writeCONTROL_REG, wave.cc:265-268).
                            _voices[voiceIndex].FloatingOutputTtl = FloatingOutputTtlStart6581;
                        }
                    }
                    break;
                case 5:
                    _voices[voiceIndex].AttackDecay = value;
                    _voices[voiceIndex].Env.WriteAttackDecay(value);
                    break;
                case 6:
                    _voices[voiceIndex].SustainRelease = value;
                    _voices[voiceIndex].Env.WriteSustainRelease(value);
                    break;
            }
        }

        // Global registers. FR-SID-004 acceptance criteria 1-5 (BACKFILL-
        // SID-001 filter slice): the SID filter is configured by four
        // distinct registers and one composite control byte. Parsing must
        // match real-hardware semantics so that test programs writing the
        // canonical $D415-$D418 layout get the expected behaviour.
        //
        //   $D415 FCLO  bits 0-2 = low 3 bits of 11-bit cutoff (others ignored)
        //   $D416 FCHI  bits 0-7 = high 8 bits of 11-bit cutoff
        //   $D417 RES_FILT
        //                  bits 0-3 = filter routing (V1,V2,V3,EXT in)
        //                  bits 4-7 = resonance (0..15)
        //   $D418 MODE_VOL
        //                  bits 0-3 = master volume (4-bit DAC, FR-SID-010)
        //                  bits 4-6 = filter mode (LP, BP, HP - combinable)
        //                  bit  7   = voice 3 off (V3OFF / disable)
        //
        // We pack the lower nibble of $D417 (routing) and bits 4-6 of $D418
        // (mode) into a single _filterControl byte using the same layout the
        // reSID filter model reads (bits 0-3 = routing, bits 4-6 = mode), shared
        // unchanged by the 6581 and 8580 filter models.
        switch (register)
        {
            case 0x15:
                _filterCutoff = (_filterCutoff & 0x07F8) | (value & 0x07);
                if (UsesReSidFilter) SetW0();
                break;
            case 0x16:
                _filterCutoff = (_filterCutoff & 0x0007) | ((value & 0xFF) << 3);
                if (UsesReSidFilter) SetW0();
                break;
            case 0x17:
                _filterResonance = (byte)((value >> 4) & 0x0F);
                _filterControl = (byte)((_filterControl & 0xF0) | (value & 0x0F));
                if (UsesReSidFilter) SetSumMix_6581();
                break;
            case 0x18:
                _volume = (byte)(value & 0x0F);
                // PLAN-VICEPARITY-001 S8 (FR-SID-MIXVOL AC-01): preserve bit7
                // (3OFF / V3OFF) in mode store. reSID writeMODE_VOL stores
                // mode = v & 0xf0, vol = v & 0xf (filter8580new.cc:742-748).
                // Previously masked 0x70 (dropped bit7). Must mask 0xF0.
                _filterControl = (byte)((_filterControl & 0x0F) | (value & 0xF0));
                if (UsesReSidFilter) SetSumMix_6581();
                break;
        }
    }

    public byte Peek(ushort address)
    {
        // Side-effect-free debug view [PLAN-VICEPARITY-001 S10]: unlike Read, it
        // never mutates the data bus. Returns the register file for the readable/
        // write-only storage, the POT stub for $19/$1A, and the live OSC3/ENV3
        // latches for $1B/$1C. Snapshot round-trip probes rely on this to inspect
        // write-only registers without perturbing the shared bus.
        int register = address & 0x1F;
        return register switch
        {
            0x19 or 0x1A => 0xFF,
            0x1B => (byte)(_voices[2].Osc3 >> 4),
            0x1C => _voices[2].Env.Env3,
            _ => _registers[register],
        };
    }

    /// <summary>
    /// FR-SID-012 (BACKFILL-SID-001 dual-SID slice, acceptance criteria 1,
    /// 6). The base address of this SID chip's 32-byte register window.
    /// Board-specific placement and any mirror window belong to the
    /// memory-map dispatcher, not to the chip itself: each chip claims
    /// only its native 32-byte window so multiple instances can coexist
    /// at board-selected 32-byte boundaries without ambiguity.
    /// </summary>
    public ushort BaseAddress { get; init; }

    /// <summary>
    /// FR-SID-012 (BACKFILL-SID-001 dual-SID slice, acceptance criterion 6).
    /// The chip claims only its native 32-byte register window
    /// (<c>BaseAddress</c> .. <c>BaseAddress + 0x1F</c>). The owning
    /// memory-map dispatcher is responsible for filling any surrounding
    /// mirror window.
    /// </summary>
    public bool HandlesAddress(ushort address)
    {
        return address >= BaseAddress && address < (BaseAddress + 0x20);
    }

    /// <summary>
    /// FR-SID-004 ac.6: 3-segment piecewise approximation of the 6581's
    /// non-linear ("kinked") cutoff frequency curve. Real 6581 silicon
    /// shows a flat low region (~200-300Hz across reg 0-0x200), a steep
    /// middle (300-12300Hz across 0x200-0x600), and a flatter high
    /// region (12300-15000Hz across 0x600-0x7FF). Public for tests; the
    /// filter pipeline can adopt this via a future follow-up wiring slice.
    /// Source: resid documentation + Bob Yannes interview.
    /// </summary>
    public static float MapCutoffRegToFrequency(int reg11)
    {
        if (reg11 < 0) reg11 = 0;
        if (reg11 > 0x7FF) reg11 = 0x7FF;

        if (reg11 < 0x200)
        {
            return 200f + (reg11 / 512f) * 100f;
        }

        if (reg11 < 0x600)
        {
            return 300f + ((reg11 - 0x200) / 1024f) * 12000f;
        }

        return 12300f + ((reg11 - 0x600) / 511f) * 2700f;
    }

    /// <summary>
    /// FR-SID-003 / FR-SID-004 (8580 filter deepening). 8580 cutoff curve is
    /// essentially linear from ~30Hz at register 0 to ~12,500Hz at register
    /// 0x7FF, in contrast with the 6581's kinked three-segment curve. Source:
    /// resid sid8580.c filter calibration tables + Bob Yannes interview. The
    /// curve is monotone, continuous, and roughly 12kHz wide.
    /// </summary>
    public static float MapCutoffRegToFrequency8580(int reg11)
    {
        if (reg11 < 0) reg11 = 0;
        if (reg11 > 0x7FF) reg11 = 0x7FF;
        const float minHz = 30f;
        const float maxHz = 12500f;
        return minHz + (reg11 / 2047f) * (maxHz - minHz);
    }
}
