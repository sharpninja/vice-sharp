using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Sid;

/// <summary>
/// MOS 6581 Sound Interface Device implementation.
/// </summary>
public sealed class Mos6581 : IAudioChip, IAddressSpace
{
    public DeviceId Id => new DeviceId(0x0004);
    public string Name => "MOS 6581 SID";
    public uint ClockDivisor => 16;
    public ClockPhase Phase => ClockPhase.Phi2;

    public ushort BaseAddress { get; init; } = 0xD400;
    public ushort Size => 32;
    public bool IsReadOnly => false;

    // SID registers
    private readonly byte[] _registers = new byte[32];

    private readonly IBus _bus;
    private readonly IAudioBackend _audioBackend;

    public Mos6581(IBus bus, IAudioBackend audioBackend)
    {
        _bus = bus;
        _audioBackend = audioBackend;
    }

    /// <inheritdoc />
    public void Tick()
    {
        // Audio generation runs every 16 CPU cycles
    }

    /// <inheritdoc />
    public void Initialize()
    {
        Reset();
    }

    /// <inheritdoc />
    public void Reset()
    {
        Array.Clear(_registers, 0, _registers.Length);
    }

    /// <inheritdoc />
    public byte Peek(ushort offset)
    {
        return Read(offset);
    }

    /// <inheritdoc />
    public byte Read(ushort offset)
    {
        if (offset >= Size) return 0xFF;
        return _registers[offset];
    }

    /// <inheritdoc />
    public void Write(ushort offset, byte value)
    {
        if (offset >= Size) return;
        _registers[offset] = value;
    }

    /// <inheritdoc />
    public bool HandlesAddress(ushort address)
    {
        return address >= BaseAddress && address < BaseAddress + Size;
    }

    /// <inheritdoc />
    public void FillAudioBuffer(Span<short> buffer)
    {
        // SID audio generation - 3 voices at 44100 Hz
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = (short)(GenerateSample() * 32767);
        }
    }

    public int ChannelCount => 3;
    public byte MasterVolume
    {
        get => (byte)(_registers[0x18] & 0x0F);
        set => _registers[0x18] = (byte)((_registers[0x18] & 0xF0) | (value & 0x0F));
    }

    /// <inheritdoc />
    public int SampleRate => 44100;
    
    // VICE-style: Voice state for SID synthesis
    private readonly VoiceState[] _voices = new VoiceState[3];
    
    private struct VoiceState
    {
        public ushort Frequency;
        public ushort PulseWidth;
        public byte Waveform;
        public byte Control;
        public byte AttackDecay;
        public byte SustainRelease;
        public ushort Oscillator;
        public ushort Envelope;
        public bool Gate;
    }
    
    /// <inheritdoc />
    public float GenerateSample()
    {
        float output = 0f;
        int volume = _registers[0x18] & 0x0F;
        bool muteLeft = (_registers[0x18] & 0x20) != 0;
        bool muteRight = (_registers[0x18] & 0x40) != 0;
        
        for (int v = 0; v < 3; v++)
        {
            ref VoiceState voice = ref _voices[v];
            
            // Update oscillator
            voice.Oscillator += voice.Frequency;
            
            // Waveform generation (simplified)
            float voiceOut = 0f;
            switch (voice.Waveform & 0xF0)
            {
                case 0x10: // Triangle
                    voiceOut = (voice.Oscillator >> 8) / 255f;
                    break;
                case 0x20: // Sawtooth
                    voiceOut = (voice.Oscillator >> 8) / 255f;
                    break;
                case 0x40: // Square
                    voiceOut = (voice.Oscillator & 0x8000) != 0 ? 1f : 0f;
                    break;
                case 0x80: // Noise
                    voiceOut = ((voice.Oscillator >> 8) & 0x01) != 0 ? 1f : 0f;
                    break;
            }
            
            output += voiceOut;
        }
        
        return (output / 3f) * (volume / 15f);
    }
    
    private void UpdateVoice(int voiceNum, ushort offset, byte value)
    {
        ref VoiceState v = ref _voices[voiceNum];
        switch (offset)
        {
            case 0: v.Frequency = (ushort)((v.Frequency & 0xFF00) | value); break;
            case 1: v.Frequency = (ushort)((v.Frequency & 0x00FF) | (value << 8)); break;
            case 2: v.PulseWidth = (ushort)((v.PulseWidth & 0xFF00) | value); break;
            case 3: v.PulseWidth = (ushort)((v.PulseWidth & 0x00FF) | (value << 8)); break;
            case 4: v.Control = value; v.Gate = (value & 0x01) != 0; break;
            case 5: v.AttackDecay = value; break;
            case 6: v.SustainRelease = value; break;
        }
    }
}
