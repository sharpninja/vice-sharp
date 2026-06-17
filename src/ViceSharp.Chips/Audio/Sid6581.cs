using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Audio;

public partial class Sid6581 : IClockedDevice, IAddressSpace, IAudioChip
{
    public DeviceId Id => new DeviceId(0x0004);
    public string Name => "MOS 6581 SID";
    public uint ClockDivisor => 16;
    public ClockPhase Phase => ClockPhase.Phi2;

    public int SamplingRate => 44100;
    public int ChannelCount => 1;
    public byte MasterVolume { get => _volume; set => _volume = value; }

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
    // bit 19 of each voice's 24-bit phase accumulator transitions low->high.
    // Output is derived from bits 0, 2, 5, 9, 11, 14, 18, 20 (one tap per
    // 8-bit output bit). The reset state is all-ones (0x7FFFFF) and the
    // test bit in CTRL (bit 3) reseeds the LFSR to that state.
    private const uint NoiseLfsrMask = 0x007F_FFFF;
    private const uint NoiseLfsrInitial = NoiseLfsrMask;
    // Bit 19 of the 24-bit accumulator (the implementation stores the
    // 24-bit value in a uint; the upper byte is the phase output).
    private const uint NoiseClockBit = 1u << 19;
    private uint _noiseLfsr = NoiseLfsrInitial;

    /// <summary>
    /// FR-SID-009 ac.1: advance the 23-bit noise LFSR one step. The
    /// feedback polynomial taps bits 22 and 17 (XOR), the result is
    /// shifted into bit 0, and the LFSR is masked to 23 bits. If the
    /// LFSR ever lands on zero it is reseeded to the all-ones state
    /// (the LFSR cannot escape zero with the standard polynomial).
    /// </summary>
    private void ClockNoiseLfsr()
    {
        var feedback = ((_noiseLfsr >> 22) ^ (_noiseLfsr >> 17)) & 0x01;
        _noiseLfsr = ((_noiseLfsr << 1) | feedback) & NoiseLfsrMask;
        if (_noiseLfsr == 0)
            _noiseLfsr = NoiseLfsrInitial;
    }

    /// <summary>
    /// FR-SID-009 ac.3: pack the 8-bit noise output from the 23-bit LFSR
    /// using the canonical hardware tap map (bits 0, 2, 5, 9, 11, 14, 18,
    /// 20 contribute one bit each to the 8-bit output). Matches the
    /// SidOscillator and resid noise output exactly.
    /// </summary>
    private static byte NoiseOutput(uint lfsr) =>
        (byte)(
            (((lfsr >> 20) & 0x01) << 7) |
            (((lfsr >> 18) & 0x01) << 6) |
            (((lfsr >> 14) & 0x01) << 5) |
            (((lfsr >> 11) & 0x01) << 4) |
            (((lfsr >> 9) & 0x01) << 3) |
            (((lfsr >> 5) & 0x01) << 2) |
            (((lfsr >> 2) & 0x01) << 1) |
            ((lfsr >> 0) & 0x01));

    public float GenerateSample()
    {
        // Generate raw voice outputs with sync/ring modulation
        int voiceOutputs0 = 0, voiceOutputs1 = 0, voiceOutputs2 = 0;
        int prevOutput = 0;

        for (int i = 0; i < 3; i++)
        {
            ref Voice voice = ref _voices[i];
            // Waveform-select bits live in CTRL bits 4-7 (Triangle, Sawtooth,
            // Pulse, Noise). Mask with 0xF0 to isolate them from sync/ring/
            // test/gate bits.
            byte waveformBits = (byte)(voice.Control & 0xF0);
            bool sync = (voice.Control & 0x02) != 0;
            bool ringMod = (voice.Control & 0x04) != 0;

            int sample = 0;
            // The phase accumulator is 24-bit (sync uses bit 23, noise bit 19),
            // so the 8-bit waveform index is bits 16-23. Reading bits 24-31
            // (>> 24) capped the oscillator at ~15 Hz - every note was sub-audible
            // DC, which is why no sound was produced. (OSC3 readback at Read()
            // keeps its existing extraction; it is parity-gated separately.)
            uint phase = (voice.WaveformAccumulator >> 16) & 0xFF;
            // Hard sync is handled cycle-accurately in Tick() via MSB-edge
            // detection; GenerateSample only reads the post-sync state.
            _ = sync;

            bool hasTriangle = (waveformBits & 0x10) != 0;
            bool hasSawtooth = (waveformBits & 0x20) != 0;
            bool hasPulse = (waveformBits & 0x40) != 0;
            bool hasNoise = (waveformBits & 0x80) != 0;

            // FR-SID-003 (combined waveforms): the SID's hardware ANDs the
            // outputs of every selected waveform together. With a single
            // waveform selected this collapses to that waveform's value;
            // with two or more selected the result is the bitwise AND of
            // their 8-bit outputs (the distorted/attenuated combined-waveform
            // sound associated with SID silicon). With zero waveform bits selected
            // the voice is silent.
            int triValue = 0, sawValue = 0, pulValue = 0, noiseValue = 0;

            if (hasTriangle)
            {
                uint tri = phase < 128 ? (phase << 1) : ((255 - phase) << 1);
                if (ringMod)
                {
                    // Ring modulation: triangle output's "shape" is inverted
                    // when the sync-source voice's MSB is high. The sync
                    // source is voice ((i + 2) % 3) (cyclic backward).
                    var modulatorMsb = (_voices[(i + 2) % 3].WaveformAccumulator & 0x800000u) != 0;
                    if (modulatorMsb)
                        tri ^= 0xFF;
                }
                triValue = (int)(tri & 0xFF);
            }
            if (hasSawtooth)
            {
                sawValue = (int)(phase & 0xFF);
            }
            if (hasPulse)
            {
                // 12-bit pulse width compared at the 8-bit phase resolution
                // (top 8 bits of PW), so a 50% duty register (~$800) gives a
                // 50% duty wave.
                pulValue = phase < (uint)(voice.PulseWidth >> 4) ? 0xFF : 0x00;
            }
            if (hasNoise)
            {
                // FR-SID-009 ac.3: noise output is derived from specific
                // taps of the 23-bit LFSR (bits 0, 2, 5, 9, 11, 14, 18, 20),
                // packed into an 8-bit value. Matches the hardware tap map
                // documented in resid/sid8580.c.
                noiseValue = NoiseOutput(_noiseLfsr);
            }

            int selectedCount = (hasTriangle ? 1 : 0) + (hasSawtooth ? 1 : 0)
                               + (hasPulse ? 1 : 0) + (hasNoise ? 1 : 0);

            if (selectedCount == 0)
            {
                // No waveform selected: silence (hardware floats to 0).
                sample = 0;
            }
            else if (selectedCount == 1)
            {
                // Single waveform - preserve the existing per-waveform value
                // verbatim so single-waveform behaviour is unchanged.
                sample = hasTriangle ? triValue
                       : hasSawtooth ? sawValue
                       : hasPulse    ? pulValue
                       :               noiseValue;
            }
            else
            {
                // Two or more waveforms selected: AND the 8-bit outputs of
                // every selected waveform. Unselected waveforms contribute
                // 0xFF (the AND identity) so they do not affect the result.
                int combined = 0xFF;
                if (hasTriangle) combined &= triValue;
                if (hasSawtooth) combined &= sawValue;
                if (hasPulse)    combined &= pulValue;
                if (hasNoise)    combined &= noiseValue;
                // FR-SID-003 acceptance criterion 2: the 8580 die has
                // different combined-waveform analog bleed than the 6581.
                // ApplyCombinedBleed is a no-op on 6581 and applies a
                // ~0.75 attenuation scalar on the 8580 variant.
                sample = ApplyCombinedBleed((byte)combined);
            }

            prevOutput = sample;
            int envelopeAdjusted = (sample * voice.Envelope) >> 8;

            if (i == 0) voiceOutputs0 = envelopeAdjusted;
            else if (i == 1) voiceOutputs1 = envelopeAdjusted;
            else voiceOutputs2 = envelopeAdjusted;
        }

        // Apply filter - classic VICE-style multi-mode filter
        int filteredOutput = ApplyFilter(voiceOutputs0, voiceOutputs1, voiceOutputs2);

        // FR-SID-010 digi playback: the 4-bit master-volume DAC ($D418 bits 0-3)
        // contributes a small DC offset proportional to volume even when no
        // voices are gated. This is the rail that makes the famous Galway/
        // Daglish 4-bit PCM technique audible on real SID hardware: rapid
        // $D418 writes alone produce hearable PCM through the DAC nonlinearity.
        // The chosen DC magnitude (DigiDcOffset) is modest enough to leave
        // normal voice output dominant while still letting per-write volume
        // changes register as audible amplitude.
        float volumeFraction = _volume / 15.0f;
        float voiceMix = (filteredOutput / 3) * volumeFraction / 255.0f;
        float digiDcOffset = volumeFraction * DigiDcOffset;
        return voiceMix + digiDcOffset;
    }

    /// <summary>
    /// Per-step amplitude of the $D418 DAC DC offset used for digi playback
    /// (FR-SID-010). Scales linearly with the 4-bit master-volume nibble;
    /// at volume 15 the offset is DigiDcOffset, at volume 0 it is exactly
    /// zero (silent rail preserved). Chosen small enough that normal voice
    /// output dominates the mix.
    /// </summary>
    private const float DigiDcOffset = 0.05f;

    /// <summary>
    /// FR-SID-003 acceptance criterion 2 (BACKFILL-SID-001 / 8580 variant).
    /// Hook applied to the AND-combine result when two or more waveform
    /// bits are selected. The 6581 base implementation is a no-op (the
    /// AND-combined value passes through unchanged); the 8580 override
    /// applies the ~0.75 attenuation scalar that models the 8580 die's
    /// reduced combined-waveform analog bleed. Single-waveform output is
    /// never routed through this hook, so single-waveform behaviour is
    /// identical across both die revisions.
    /// </summary>
    protected virtual byte ApplyCombinedBleed(byte andResult) => andResult;

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
        // FR-SID-004 ac.4: per-voice routing. $D417 bits 0-2 gate which
        // voice mixes into the filter input; the remaining voices bypass.
        bool voice0Filtered = (_filterControl & 0x01) != 0;
        bool voice1Filtered = (_filterControl & 0x02) != 0;
        bool voice2Filtered = (_filterControl & 0x04) != 0;
        // FR-SID-004 ac.5: external audio input via $D417 bit 3. The chip
        // has no IAudioBackend-style input surface yet, so the routing bit
        // is parsed but the external sample is 0 (silent). A follow-up
        // slice will wire a real external-input mixer.
        bool extInRouted = (_filterControl & 0x08) != 0;
        _ = extInRouted;

        int filterInput = 0;
        if (voice0Filtered) filterInput += voice0;
        if (voice1Filtered) filterInput += voice1;
        if (voice2Filtered) filterInput += voice2;

        int bypassMix = 0;
        if (!voice0Filtered) bypassMix += voice0;
        if (!voice1Filtered) bypassMix += voice1;
        if (!voice2Filtered) bypassMix += voice2;

        // FR-SID-004 ac.3: LP/BP/HP mode select via $D418 bits 4-6. The
        // taps are additive: if all three are set, the output sums all
        // three filter responses. If none are set, the filter is bypassed
        // and the input passes through unchanged (the standard "filter off"
        // configuration).
        bool lp = (_filterControl & 0x10) != 0;
        bool bp = (_filterControl & 0x20) != 0;
        bool hp = (_filterControl & 0x40) != 0;

        if (!(lp || bp || hp))
        {
            // No mode bits selected: filter is effectively bypassed and
            // routed voices still reach the mix (matches real hardware
            // where the filter just becomes a unity-gain wire).
            return filterInput + bypassMix;
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
    private const double FilterSaturation = 2048.0;

    private readonly IBus _bus;

    // SID Registers
    private byte[] _registers = new byte[0x20];

    // Voice state
    private Voice[] _voices = new Voice[3];

    // Filter state
    protected int _filterCutoff;
    protected byte _filterResonance;
    protected byte _filterControl;
    protected byte _volume;

    private struct Voice
    {
        public ushort Frequency;
        public ushort PulseWidth;
        public byte Control;
        public byte AttackDecay;
        public byte SustainRelease;
        public uint WaveformAccumulator;
        public uint PulseAccumulator;
        public byte Envelope;
        public EnvelopeState State;
        public bool Gate;
        public bool Reset;
        // 15-bit envelope-rate prescaler counter (0..32767). Ticks once per
        // SID cycle. Reaching the per-stage threshold steps the envelope and
        // resets the counter to 0. Critically, this counter is NEVER reset
        // when the ATK/DCY/SUS/REL registers are written - that omission is
        // the source of the famous SID ADSR bug (FR-SID-006): a write that
        // lowers the threshold below the current counter forces the counter
        // to wrap all 15 bits before the next step fires (~32k cycles stall).
        public ushort EnvelopeRateCounter;
    }

    private readonly IAudioBackend? _audioBackend;
    private readonly float[] _sampleBuffer = new float[256];
    private int _sampleBufferLen;

    // Audio sample-rate downconversion. The SID is clocked (Tick) at
    // masterClockHz / ClockDivisor; output runs at SamplingRate (44.1 kHz).
    // _audioTicksPerSample is how many Tick()s elapse per emitted sample
    // (e.g. PAL: (985248/16)/44100 ~= 1.396). A fractional accumulator emits
    // one sample whenever it crosses that threshold, sampling the evolving
    // synthesis state. Zero (the default) disables emission entirely - so a
    // SID built without an audio backend behaves exactly as before and never
    // touches the audio path (preserving native cycle parity).
    private double _audioTicksPerSample;
    private double _audioSampleAccumulator;

    public Sid6581(IBus bus) : this(bus, audioBackend: null) { }

    public Sid6581(IBus bus, IAudioBackend? audioBackend)
    {
        _bus = bus;
        _audioBackend = audioBackend;
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
            _audioTicksPerSample = 0.0;
            return;
        }

        var sidTickHz = masterClockHz / ClockDivisor;
        _audioTicksPerSample = sidTickHz / SamplingRate;
        _audioSampleAccumulator = 0.0;
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

    // ADSR rate tables (cycles per level) - virtual for override
    protected static readonly ushort[] AttackRates = { 9, 32, 63, 95, 149, 220, 267, 313, 392, 976, 1953, 2946, 4910, 9818, 29454, 65535 };
    protected static readonly ushort[] DecayReleaseRates = { 9, 32, 63, 95, 149, 220, 267, 313, 392, 976, 1953, 2946, 4910, 9818, 29454, 65535 };
    
    /// <summary>
    /// Virtual method to get attack rates (override in subclass)
    /// </summary>
    protected virtual ushort[] GetAttackRates() => AttackRates;
    
    /// <summary>
    /// Virtual method to get decay/release rates (override in subclass)
    /// </summary>
    protected virtual ushort[] GetDecayReleaseRates() => DecayReleaseRates;

    public void Tick()
    {
        // Capture previous-cycle MSBs to detect 1->0 transitions for hard sync.
        // Each voice's sync source is voice ((i + 2) % 3) (cyclic backward).
        var prevMsb0 = (_voices[0].WaveformAccumulator & 0x800000u) != 0;
        var prevMsb1 = (_voices[1].WaveformAccumulator & 0x800000u) != 0;
        var prevMsb2 = (_voices[2].WaveformAccumulator & 0x800000u) != 0;

        // FR-SID-009 ac.2: the noise LFSR clocks on bit-19 low->high
        // transitions of the phase accumulator. Capture each voice's
        // bit-19 state before the accumulator advances so we can detect
        // the edge precisely. Real hardware has a per-voice LFSR; this
        // implementation shares one LFSR across voices (good enough for
        // mono SFX), and clocks once per edge observed on any noise-
        // selected voice this cycle.
        var prevBit19_0 = (_voices[0].WaveformAccumulator & NoiseClockBit) != 0;
        var prevBit19_1 = (_voices[1].WaveformAccumulator & NoiseClockBit) != 0;
        var prevBit19_2 = (_voices[2].WaveformAccumulator & NoiseClockBit) != 0;

        for (int i = 0; i < 3; i++)
        {
            ref Voice voice = ref _voices[i];

            // FR-SID-009 ac.4: the CTRL test bit (bit 3) forces the LFSR
            // back to the all-ones seed and pins the phase accumulator
            // at zero (matches the SidOscillator and resid behaviour).
            if ((voice.Control & 0x08) != 0)
            {
                _noiseLfsr = NoiseLfsrInitial;
                voice.WaveformAccumulator = 0;
                ProcessEnvelope(ref voice);
                continue;
            }

            voice.WaveformAccumulator += voice.Frequency;

            ProcessEnvelope(ref voice);
        }

        // FR-SID-009 ac.2: clock the LFSR once for each noise-selected
        // voice that just transitioned bit 19 low->high. Most noise
        // patches use a single voice so this is typically a single clock
        // per cycle.
        bool hasNoise0 = (_voices[0].Control & 0x80) != 0;
        bool hasNoise1 = (_voices[1].Control & 0x80) != 0;
        bool hasNoise2 = (_voices[2].Control & 0x80) != 0;
        var newBit19_0 = (_voices[0].WaveformAccumulator & NoiseClockBit) != 0;
        var newBit19_1 = (_voices[1].WaveformAccumulator & NoiseClockBit) != 0;
        var newBit19_2 = (_voices[2].WaveformAccumulator & NoiseClockBit) != 0;
        if (hasNoise0 && !prevBit19_0 && newBit19_0) ClockNoiseLfsr();
        if (hasNoise1 && !prevBit19_1 && newBit19_1) ClockNoiseLfsr();
        if (hasNoise2 && !prevBit19_2 && newBit19_2) ClockNoiseLfsr();

        // Detect MSB 1->0 transitions on each voice and apply hard sync to
        // the dependent voice when its SYNC control bit is set.
        var newMsb0 = (_voices[0].WaveformAccumulator & 0x800000u) != 0;
        var newMsb1 = (_voices[1].WaveformAccumulator & 0x800000u) != 0;
        var newMsb2 = (_voices[2].WaveformAccumulator & 0x800000u) != 0;

        // Voice 0 syncs from voice 2 (downward edge of voice 2 MSB resets voice 0).
        if ((_voices[0].Control & 0x02) != 0 && prevMsb2 && !newMsb2)
            _voices[0].WaveformAccumulator = 0;

        // Voice 1 syncs from voice 0.
        if ((_voices[1].Control & 0x02) != 0 && prevMsb0 && !newMsb0)
            _voices[1].WaveformAccumulator = 0;

        // Voice 2 syncs from voice 1.
        if ((_voices[2].Control & 0x02) != 0 && prevMsb1 && !newMsb1)
            _voices[2].WaveformAccumulator = 0;

        // Live-audio emission: once a backend is attached and the audio clock is
        // configured, sample the (now-advanced) synthesis state at 44.1 kHz via
        // a fractional tick:sample accumulator. Inert (no allocation, no call)
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

    private void ProcessEnvelope(ref Voice voice)
    {
        // Gate transitions select the state. Note: register writes to ATK/DCY/
        // SUS/REL elsewhere in Write() do NOT touch voice.EnvelopeRateCounter
        // - that omission is exactly the SID ADSR bug (FR-SID-006).
        if (voice.Gate && voice.State == EnvelopeState.Idle)
        {
            voice.State = EnvelopeState.Attack;
            voice.Envelope = 0;
            // Counter is left alone here too: matches hardware where the
            // prescaler is a free-running 15-bit counter.
        }
        if (!voice.Gate && voice.State != EnvelopeState.Idle && voice.State != EnvelopeState.Release)
        {
            voice.State = EnvelopeState.Release;
        }

        if (voice.State == EnvelopeState.Idle)
            return;

        // Select threshold for the current stage.
        ushort threshold = voice.State switch
        {
            EnvelopeState.Attack => GetAttackRates()[voice.AttackDecay >> 4],
            EnvelopeState.Decay => GetDecayReleaseRates()[voice.AttackDecay & 0x0F],
            EnvelopeState.Sustain => GetDecayReleaseRates()[voice.AttackDecay & 0x0F],
            EnvelopeState.Release => GetDecayReleaseRates()[voice.SustainRelease & 0x0F],
            _ => (ushort)0
        };

        // Tick the 15-bit prescaler. It wraps at 32768 (0..32767). This counter
        // is NOT reset on rate-register writes; that is the ADSR bug.
        voice.EnvelopeRateCounter = (ushort)((voice.EnvelopeRateCounter + 1) & 0x7FFF);

        if (voice.EnvelopeRateCounter != threshold)
            return;

        // Threshold reached: reset prescaler and step the envelope.
        voice.EnvelopeRateCounter = 0;

        byte sustainLevel = (byte)((voice.SustainRelease >> 4) * 17);

        switch (voice.State)
        {
            case EnvelopeState.Attack:
                if (voice.Envelope < 255)
                {
                    voice.Envelope++;
                }
                if (voice.Envelope == 255)
                {
                    voice.State = EnvelopeState.Decay;
                }
                break;

            case EnvelopeState.Decay:
                if (voice.Envelope > sustainLevel)
                {
                    voice.Envelope--;
                }
                if (voice.Envelope <= sustainLevel)
                {
                    voice.State = EnvelopeState.Sustain;
                }
                break;

            case EnvelopeState.Sustain:
                // Track sustain-level changes downward; the level register can
                // be lowered at any time.
                if (voice.Envelope > sustainLevel)
                {
                    voice.Envelope--;
                }
                break;

            case EnvelopeState.Release:
                if (voice.Envelope > 0)
                {
                    voice.Envelope--;
                }
                if (voice.Envelope == 0)
                {
                    voice.State = EnvelopeState.Idle;
                }
                break;
        }
    }

    public void Reset()
    {
        Array.Clear(_registers);
        Array.Clear(_voices);
        _filterCutoff = 0;
        _filterResonance = 0;
        _filterControl = 0;
        _volume = 0;
        _svfLowPass = 0.0;
        _svfBandPass = 0.0;
    }

    public byte Read(ushort address)
    {
        int register = address & 0x1F;
        
        // VICE-style: Read back current values (not just registers)
        switch (register)
        {
            case 0x1B: // OSC3: upper 8 bits of voice 3 oscillator
                return (byte)(_voices[2].WaveformAccumulator >> 24);
            case 0x1C: // ENV3: voice 3 envelope output
                return _voices[2].Envelope;
            default:
                return _registers[register];
        }
    }

    public void Write(ushort address, byte value)
    {
        int register = address & 0x1F;
        _registers[register] = value;

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
                    break;
                case 3:
                    _voices[voiceIndex].PulseWidth = (ushort)((_voices[voiceIndex].PulseWidth & 0x00FF) | ((value & 0x0F) << 8));
                    break;
                case 4:
                    _voices[voiceIndex].Control = value;
                    _voices[voiceIndex].Gate = (value & 0x01) != 0;
                    break;
                case 5:
                    _voices[voiceIndex].AttackDecay = value;
                    break;
                case 6:
                    _voices[voiceIndex].SustainRelease = value;
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
        // (mode) into a single _filterControl byte using the same layout
        // ApplyFilter already reads (bits 0-3 = routing, bits 4-6 = mode).
        // That keeps Sid8580 and Sid8580D filter overrides binary-compatible
        // with this slice (they read the same composite field).
        switch (register)
        {
            case 0x15:
                _filterCutoff = (_filterCutoff & 0x07F8) | (value & 0x07);
                break;
            case 0x16:
                _filterCutoff = (_filterCutoff & 0x0007) | ((value & 0xFF) << 3);
                break;
            case 0x17:
                _filterResonance = (byte)((value >> 4) & 0x0F);
                _filterControl = (byte)((_filterControl & 0xF0) | (value & 0x0F));
                break;
            case 0x18:
                _volume = (byte)(value & 0x0F);
                _filterControl = (byte)((_filterControl & 0x0F) | (value & 0x70));
                break;
        }
    }

    public byte Peek(ushort address)
    {
        return Read(address);
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
