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

    private uint _noiseLfsr = 0x7FFFFFFF;

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
            uint phase = voice.WaveformAccumulator >> 24;
            uint pulseWidth = (uint)voice.PulseWidth << 16;
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
            // their 8-bit outputs (the iconic distorted/attenuated C64
            // combined-waveform sound). With zero waveform bits selected
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
                pulValue = phase < (pulseWidth >> 24) ? 0xFF : 0x00;
            }
            if (hasNoise)
            {
                noiseValue = (_noiseLfsr & 1) != 0 ? 0xFF : 0x00;
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
                sample = combined;
            }

            prevOutput = sample;
            int envelopeAdjusted = (sample * voice.Envelope) >> 8;

            if (i == 0) voiceOutputs0 = envelopeAdjusted;
            else if (i == 1) voiceOutputs1 = envelopeAdjusted;
            else voiceOutputs2 = envelopeAdjusted;
        }

        // Apply filter - classic VICE-style multi-mode filter
        int filteredOutput = ApplyFilter(voiceOutputs0, voiceOutputs1, voiceOutputs2);

        return (filteredOutput / 3) * (_volume / 15.0f) / 255.0f;
    }

    // Filter state for VICE-style resonant filter
    private double _filterV0, _filterV1, _filterV2, _filterV3;
    
    /// <summary>
    /// Virtual filter method for SID variant overrides
    /// </summary>
    protected virtual int ApplyFilter(int voice0, int voice1, int voice2)
    {
        // Check which voices are routed through filter
        bool voice0Filtered = (_filterControl & 0x01) != 0;
        bool voice1Filtered = (_filterControl & 0x02) != 0;
        bool voice2Filtered = (_filterControl & 0x04) != 0;
        bool voice3Filtered = (_filterControl & 0x08) != 0; // External input

        int input = 0;
        if (!voice0Filtered) input += voice0;
        if (!voice1Filtered) input += voice1;
        if (!voice2Filtered) input += voice2;
        // Voice3/external = 0 for now

        // Get filter type settings
        bool lp = (_filterControl & 0x10) != 0;
        bool bp = (_filterControl & 0x20) != 0;
        bool hp = (_filterControl & 0x40) != 0;

        // Cutoff frequency (10-bit)
        double cutoff = _filterCutoff / 2047.0;
        
        // Resonance (0-15, VICE-style)
        double resonance = _filterResonance / 15.0;

        // VICE-style state variable filter
        if (lp || bp || hp)
        {
            // Cutoff must be in valid range
            cutoff = Math.Clamp(cutoff, 0.0, 1.0);
            
            // Update filter
            _filterV0 += cutoff * _filterV1;
            _filterV3 = input - _filterV0 - resonance * _filterV1;
            _filterV1 += cutoff * _filterV3;
            _filterV2 += cutoff * _filterV0;
            
            int output = 0;
            
            if (lp) output += (int)_filterV2;
            if (bp) output += (int)_filterV1;
            if (hp) output += (int)_filterV3;
            
            // Mix filtered voices
            int filtered = 0;
            if (voice0Filtered) filtered += voice0;
            if (voice1Filtered) filtered += voice1;
            if (voice2Filtered) filtered += voice2;
            
            return output + filtered;
        }
        else
        {
            // No filter - mix all voices
            return voice0 + voice1 + voice2;
        }
    }

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

    public Sid6581(IBus bus) : this(bus, audioBackend: null) { }

    public Sid6581(IBus bus, IAudioBackend? audioBackend)
    {
        _bus = bus;
        _audioBackend = audioBackend;
    }

    /// <summary>
    /// Generate a sample + push it to the configured audio backend (if any).
    /// Buffers samples internally and flushes in batches of 256 to amortise
    /// backend overhead. Call once per audio-sample tick (host responsibility).
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

        for (int i = 0; i < 3; i++)
        {
            ref Voice voice = ref _voices[i];

            voice.WaveformAccumulator += voice.Frequency;

            ProcessEnvelope(ref voice);
        }

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

        // Global registers
        switch (register)
        {
            case 0x15:
                _filterCutoff = (_filterCutoff & 0xFF00) | value;
                break;
            case 0x16:
                _filterCutoff = (_filterCutoff & 0x00FF) | ((value & 0x0F) << 8);
                _filterResonance = (byte)(value >> 4);
                break;
            case 0x17:
                _filterControl = value;
                break;
            case 0x18:
                _volume = (byte)(value & 0x0F);
                break;
        }
    }

    public byte Peek(ushort address)
    {
        return Read(address);
    }

    public bool HandlesAddress(ushort address)
    {
        return address >= 0xD400 && address <= 0xD7FF;
    }
}
