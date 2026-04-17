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

    public float GenerateSample()
    {
        int output = 0;

        for (int i = 0; i < 3; i++)
        {
            int phase = (int)(_voices[i].WaveformAccumulator >> 24);
            output += ((_voices[i].Envelope * phase) >> 8);
        }

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
        public byte Envelope;
        public bool Gate;
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