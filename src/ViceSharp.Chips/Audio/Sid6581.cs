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
            byte waveform = (byte)(voice.Control & 0x78);
            bool sync = (voice.Control & 0x02) != 0;
            bool ringMod = (voice.Control & 0x04) != 0;
            
            int sample = 0;
            uint phase = voice.WaveformAccumulator >> 24;
            uint pulseWidth = (uint)voice.PulseWidth << 16;
            
            // VICE-style sync: check if previous voice wraps (24-bit accumulator)
            uint prevPhase = prevOutput > 0 ? 0xFFFFFFu : 0;
            if (sync && i > 0 && prevPhase == 0)
            {
                voice.WaveformAccumulator = 0;
                phase = 0;
            }
            
            // Use waveform enum for cleaner code
            Waveform wf = (Waveform)(waveform & (byte)Waveform.Noise);
            bool hasTriangle = (waveform & (byte)Waveform.Triangle) != 0;
            bool hasSawtooth = (waveform & (byte)Waveform.Sawtooth) != 0;
            bool hasPulse = (waveform & (byte)Waveform.Pulse) != 0;
            
            if (hasTriangle)
            {
                uint tri;
                if (ringMod && i > 0)
                {
                    // Ring modulation: triangle becomes difference waveform
                    int modulation = prevOutput;
                    int triVal = (int)(phase < 128 ? (phase << 1) : ((255 - phase) << 1));
                    tri = (uint)((triVal - 128 + modulation) & 0xFF);
                }
                else
                {
                    tri = phase < 128 ? (phase << 1) : ((255 - phase) << 1);
                }
                sample = (int)tri;
            }
            else if (hasSawtooth)
            {
                sample = (int)(phase << 1);
            }
            else if (hasPulse)
            {
                sample = phase < (pulseWidth >> 24) ? 255 : 0;
            }
            else if (wf == Waveform.Noise)
            {
                if ((_noiseLfsr & 1) != 0) sample = 255;
            }
            else
            {
                sample = (int)(voice.Envelope);
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
    }

    public Sid6581(IBus bus)
    {
        _bus = bus;
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
        for (int i = 0; i < 3; i++)
        {
            ref Voice voice = ref _voices[i];
            
            // Waveform generation
            voice.WaveformAccumulator += voice.Frequency;

            // ADSR envelope generation
            ProcessEnvelope(ref voice);
        }
    }

    private void ProcessEnvelope(ref Voice voice)
    {
        // Gate on: start attack or resume
        if (voice.Gate && voice.State == EnvelopeState.Idle)
        {
            voice.State = EnvelopeState.Attack;
            voice.Envelope = 0;
        }

        byte targetLevel = (byte)((voice.SustainRelease >> 4) * 17); // Sustain level (0-15 -> 0-255)
        int attackRate = GetAttackRates()[voice.AttackDecay >> 4];
        int decayRate = GetDecayReleaseRates()[voice.AttackDecay & 0x0F];
        int releaseRate = GetDecayReleaseRates()[voice.SustainRelease & 0x0F];

        switch (voice.State)
        {
            case EnvelopeState.Attack:
                if (voice.Envelope < 255)
                {
                    voice.Envelope += (byte)Math.Min(255, (256 * 256) / attackRate);
                    if (voice.Envelope >= 255)
                    {
                        voice.Envelope = 255;
                        voice.State = EnvelopeState.Decay;
                    }
                }
                break;

            case EnvelopeState.Decay:
                if (voice.Envelope > targetLevel)
                {
                    voice.Envelope -= (byte)Math.Min(voice.Envelope, (256 * 256) / decayRate);
                    if (voice.Envelope <= targetLevel)
                    {
                        voice.Envelope = targetLevel;
                        voice.State = EnvelopeState.Sustain;
                    }
                }
                break;

            case EnvelopeState.Sustain:
                // Sustain holds level until gate off
                break;

            case EnvelopeState.Release:
                if (voice.Envelope > 0)
                {
                    voice.Envelope -= (byte)Math.Min(voice.Envelope, (256 * 256) / releaseRate);
                    if (voice.Envelope <= 0)
                    {
                        voice.Envelope = 0;
                        voice.State = EnvelopeState.Idle;
                    }
                }
                break;
        }

        // Gate off: start release
        if (!voice.Gate && voice.State != EnvelopeState.Idle && voice.State != EnvelopeState.Release)
        {
            voice.State = EnvelopeState.Release;
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
            // Voice oscillator read (for voice 3 or combined)
            case 0x19: // Voice 3 frequency low / OSC3
                return (byte)(_voices[2].WaveformAccumulator >> 24);
            case 0x1A: // Voice 3 frequency high
                return (byte)((_voices[2].WaveformAccumulator >> 16) & 0xFF);
            case 0x1B: // Voice 3 envelope
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