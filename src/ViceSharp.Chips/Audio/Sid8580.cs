using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Audio;

/// <summary>
/// MOS 8580 SID emulator - the C64DTV/CR variant with different filter characteristics.
/// Based on VICE sid8580.c logic.
/// </summary>
public sealed class Sid8580 : IClockedDevice, IAddressSpace, IAudioChip
{
    public DeviceId Id => new DeviceId(0x0004);
    public string Name => "MOS 8580 SID";
    public uint ClockDivisor => 16;
    public ClockPhase Phase => ClockPhase.Phi2;

    public int SamplingRate => 44100;
    public int ChannelCount => 1;
    public byte MasterVolume { get => _volume; set => _volume = value; }

    // Waveform types (same as 6581)
    private const byte TRIANGLE = 0x04;
    private const byte SAWTOOTH = 0x08;
    private const byte PULSE = 0x40;
    private const byte NOISE = 0x80;

    private uint _noiseLfsr = 0x7FFFFFFF;

    // 8580 filter - different DC characteristics
    private int _dcOffset;

    public float GenerateSample()
    {
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
            
            // VICE-style sync
            if (sync && i > 0 && prevOutput > 0)
            {
                voice.WaveformAccumulator = 0;
                phase = 0;
            }
            
            if ((waveform & TRIANGLE) != 0)
            {
                uint tri;
                if (ringMod && i > 0)
                {
                    int triVal = (int)(phase < 128 ? (phase << 1) : ((255 - phase) << 1));
                    tri = (uint)((triVal - 128 + prevOutput) & 0xFF);
                }
                else
                {
                    tri = phase < 128 ? (phase << 1) : ((255 - phase) << 1);
                }
                sample = (int)tri;
            }
            else if ((waveform & SAWTOOTH) != 0)
            {
                sample = (int)(phase << 1);
            }
            else if ((waveform & PULSE) != 0)
            {
                uint pulseWidth = (uint)voice.PulseWidth << 16;
                sample = phase < (pulseWidth >> 24) ? 255 : 0;
            }
            else if ((waveform & NOISE) != 0)
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

        int filteredOutput = Apply8580Filter(voiceOutputs0, voiceOutputs1, voiceOutputs2);

        return (filteredOutput / 3) * (_volume / 15.0f) / 255.0f;
    }

    /// <summary>
    /// 8580 filter - simpler DC characteristics than 6581
    /// </summary>
    private int Apply8580Filter(int voice0, int voice1, int voice2)
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

        // 8580 has different cutoff scaling
        double cutoff = _filterCutoff / 2047.0 * 0.85;
        cutoff = Math.Clamp(cutoff, 0.0, 1.0);
        
        double resonance = _filterResonance / 15.0 * 0.9;

        if (lp || bp || hp)
        {
            // Simplified 8580 filter (no state variable filter)
            // Uses cascaded single-pole filters for HP/LP
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

    private readonly IBus _bus;
    private byte[] _registers = new byte[0x20];
    private Voice[] _voices = new Voice[3];
    private int _filterCutoff;
    private byte _filterResonance;
    private byte _filterControl;
    private byte _volume;

    private struct Voice
    {
        public ushort Frequency;
        public ushort PulseWidth;
        public byte Control;
        public byte AttackDecay;
        public byte SustainRelease;
        public uint WaveformAccumulator;
        public byte Envelope;
        public byte EnvelopeState;
        public bool Gate;
    }

    public Sid8580(IBus bus)
    {
        _bus = bus;
    }

    private const byte ENV_IDLE = 0, ENV_ATTACK = 1, ENV_DECAY = 2, ENV_SUSTAIN = 3, ENV_RELEASE = 4;

    // 8580 ADSR - different rates than 6581
    private static readonly uint[] AttackRates8580 = { 14, 49, 97, 146, 230, 342, 419, 489, 617, 1549, 3119, 4702, 7846, 15699, 47095, 0xFFFFFF };
    private static readonly uint[] DecayReleaseRates8580 = { 14, 49, 97, 146, 230, 342, 419, 489, 617, 1549, 3119, 4702, 7846, 15699, 47095, 0xFFFFFF };

    public void Tick()
    {
        for (int i = 0; i < 3; i++)
        {
            ref Voice voice = ref _voices[i];
            voice.WaveformAccumulator += voice.Frequency;
            ProcessEnvelope(ref voice);
        }
    }

    private void ProcessEnvelope(ref Voice voice)
    {
        if (voice.Gate && voice.EnvelopeState == ENV_IDLE)
        {
            voice.EnvelopeState = ENV_ATTACK;
            voice.Envelope = 0;
        }

        byte targetLevel = (byte)((voice.SustainRelease >> 4) * 17);
        int attackRate = (int)AttackRates8580[voice.AttackDecay >> 4];
        int decayRate = (int)DecayReleaseRates8580[voice.AttackDecay & 0x0F];
        int releaseRate = (int)DecayReleaseRates8580[voice.SustainRelease & 0x0F];

        switch (voice.EnvelopeState)
        {
            case ENV_ATTACK:
                if (voice.Envelope < 255)
                {
                    voice.Envelope += (byte)Math.Min(255, (256 * 256) / attackRate);
                    if (voice.Envelope >= 255) { voice.Envelope = 255; voice.EnvelopeState = ENV_DECAY; }
                }
                break;
            case ENV_DECAY:
                if (voice.Envelope > targetLevel)
                {
                    voice.Envelope -= (byte)Math.Min(voice.Envelope, (256 * 256) / decayRate);
                    if (voice.Envelope <= targetLevel) { voice.Envelope = targetLevel; voice.EnvelopeState = ENV_SUSTAIN; }
                }
                break;
            case ENV_SUSTAIN: break;
            case ENV_RELEASE:
                if (voice.Envelope > 0)
                {
                    voice.Envelope -= (byte)Math.Min(voice.Envelope, (256 * 256) / releaseRate);
                    if (voice.Envelope <= 0) { voice.Envelope = 0; voice.EnvelopeState = ENV_IDLE; }
                }
                break;
        }

        if (!voice.Gate && voice.EnvelopeState != ENV_IDLE && voice.EnvelopeState != ENV_RELEASE)
            voice.EnvelopeState = ENV_RELEASE;
    }

    public void Reset()
    {
        Array.Clear(_registers);
        Array.Clear(_voices);
        _filterCutoff = 0; _filterResonance = 0; _filterControl = 0; _volume = 0;
        _dcOffset = 0;
    }

    public byte Read(ushort address)
    {
        int register = address & 0x1F;
        switch (register)
        {
            case 0x19: return (byte)(_voices[2].WaveformAccumulator >> 24);
            case 0x1A: return (byte)((_voices[2].WaveformAccumulator >> 16) & 0xFF);
            case 0x1B: return _voices[2].Envelope;
            default: return _registers[register];
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
                case 0: _voices[voiceIndex].Frequency = (ushort)((_voices[voiceIndex].Frequency & 0xFF00) | value); break;
                case 1: _voices[voiceIndex].Frequency = (ushort)((_voices[voiceIndex].Frequency & 0x00FF) | (value << 8)); break;
                case 2: _voices[voiceIndex].PulseWidth = (ushort)((_voices[voiceIndex].PulseWidth & 0xFF00) | value); break;
                case 3: _voices[voiceIndex].PulseWidth = (ushort)((_voices[voiceIndex].PulseWidth & 0x00FF) | ((value & 0x0F) << 8)); break;
                case 4: _voices[voiceIndex].Control = value; _voices[voiceIndex].Gate = (value & 0x01) != 0; break;
                case 5: _voices[voiceIndex].AttackDecay = value; break;
                case 6: _voices[voiceIndex].SustainRelease = value; break;
            }
        }
        switch (register)
        {
            case 0x15: _filterCutoff = (_filterCutoff & 0xFF00) | value; break;
            case 0x16: _filterCutoff = (_filterCutoff & 0x00FF) | ((value & 0x0F) << 8); _filterResonance = (byte)(value >> 4); break;
            case 0x17: _filterControl = value; break;
            case 0x18: _volume = (byte)(value & 0x0F); break;
        }
    }

    public byte Peek(ushort address) => Read(address);

    public bool HandlesAddress(ushort address) => address >= 0xD400 && address <= 0xD7FF;
}
