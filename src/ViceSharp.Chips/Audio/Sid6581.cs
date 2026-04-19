using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Audio;

public sealed partial class Sid6581 : IClockedDevice, IAddressSpace, IAudioChip
{
    public DeviceId Id => new DeviceId(0x0004);
    public string Name => "MOS 6581 SID";
    public uint ClockDivisor => 16;
    public ClockPhase Phase => ClockPhase.Phi2;

    public int SamplingRate => 44100;
    public int ChannelCount => 1;
    public byte MasterVolume { get => _volume; set => _volume = value; }

    // Waveform types
    private const byte TRIANGLE = 0x04;
    private const byte SAWTOOTH = 0x08;
    private const byte PULSE = 0x40;
    private const byte NOISE = 0x80;

    private uint _noiseLfsr = 0x7FFFFFFF;

    public float GenerateSample()
    {
        int output = 0;

        for (int i = 0; i < 3; i++)
        {
            ref Voice voice = ref _voices[i];
            byte waveform = (byte)(voice.Control & 0x78); // Check waveform bits
            
            int sample = 0;
            uint phase = voice.WaveformAccumulator >> 24;
            uint pulseWidth = (uint)voice.PulseWidth << 16;
            
            if ((waveform & TRIANGLE) != 0)
            {
                // Triangle wave - symmetric
                uint tri = phase < 128 ? (phase << 1) : ((255 - phase) << 1);
                sample = (int)tri;
            }
            else if ((waveform & SAWTOOTH) != 0)
            {
                // Sawtooth - ramp up
                sample = (int)(phase << 1);
            }
            else if ((waveform & PULSE) != 0)
            {
                // Pulse wave - duty cycle
                sample = phase < (pulseWidth >> 24) ? 255 : 0;
            }
            else if ((waveform & NOISE) != 0)
            {
                // Noise - LFSR
                if ((_noiseLfsr & 1) != 0) sample = 255;
            }
            else
            {
                // Test mode or no waveform
                sample = (int)(voice.Envelope);
            }
            
            // Apply envelope
            output += (sample * voice.Envelope) >> 8;
        }

        // Apply filter (simplified)
        return (output / 3) * (_volume / 15.0f) / 255.0f;
    }

    private readonly IBus _bus;

    // SID Registers
    private byte[] _registers = new byte[0x20];

    // Voice state
    private Voice[] _voices = new Voice[3];

    // Filter state
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
        public uint PulseAccumulator;
        public byte Envelope;
        public byte EnvelopeState; // 0=Idle, 1=Attack, 2=Decay, 3=Sustain, 4=Release
        public bool Gate;
        public bool Reset;
    }

    public Sid6581(IBus bus)
    {
        _bus = bus;
    }

    public void Tick()
    {
        for (int i = 0; i < 3; i++)
        {
            ref Voice voice = ref _voices[i];
            
            // Waveform generation
            voice.WaveformAccumulator += voice.Frequency;

            // Envelope generation
            if (voice.Gate)
            {
                // Attack phase
                if (voice.Envelope < 0xFF)
                {
                    voice.Envelope += (byte)(voice.AttackDecay >> 4);
                }
            }
            else
            {
                // Release phase
                voice.Envelope -= (byte)(voice.SustainRelease & 0x0F);
            }
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
        return _registers[register];
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