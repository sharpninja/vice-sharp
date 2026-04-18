namespace ViceSharp.Chips;

/// <summary>
/// MOS 6581 Sound Interface Device (SID)
/// Direct logic port from VICE sid.h
/// </summary>
public sealed class Mos6581
{
    #region Register Offsets

    public const byte VOICE1_FREQ_LO = 0x00;
    public const byte VOICE1_FREQ_HI = 0x01;
    public const byte VOICE1_PW_LO = 0x02;
    public const byte VOICE1_PW_HI = 0x03;
    public const byte VOICE1_CONTROL = 0x04;
    public const byte VOICE1_ATTACK_DECAY = 0x05;
    public const byte VOICE1_SUSTAIN_RELEASE = 0x06;

    public const byte VOICE2_FREQ_LO = 0x07;
    public const byte VOICE2_FREQ_HI = 0x08;
    public const byte VOICE2_PW_LO = 0x09;
    public const byte VOICE2_PW_HI = 0x0A;
    public const byte VOICE2_CONTROL = 0x0B;
    public const byte VOICE2_ATTACK_DECAY = 0x0C;
    public const byte VOICE2_SUSTAIN_RELEASE = 0x0D;

    public const byte VOICE3_FREQ_LO = 0x0E;
    public const byte VOICE3_FREQ_HI = 0x0F;
    public const byte VOICE3_PW_LO = 0x10;
    public const byte VOICE3_PW_HI = 0x11;
    public const byte VOICE3_CONTROL = 0x12;
    public const byte VOICE3_ATTACK_DECAY = 0x13;
    public const byte VOICE3_SUSTAIN_RELEASE = 0x14;

    public const byte FILTER_CUTOFF_LO = 0x15;
    public const byte FILTER_CUTOFF_HI = 0x16;
    public const byte FILTER_CONTROL = 0x17;
    public const byte VOLUME = 0x18;

    public const byte POTX = 0x19;
    public const byte POTY = 0x1A;
    public const byte VOICE3_OSC = 0x1B;
    public const byte VOICE3_ENV = 0x1C;

    #endregion

    #region Control Register Bits

    [Flags]
    public enum VoiceControl : byte
    {
        Gate = 0x01,
        Sync = 0x02,
        RingMod = 0x04,
        Test = 0x08,
        Triangle = 0x10,
        Sawtooth = 0x20,
        Pulse = 0x40,
        Noise = 0x80
    }

    [Flags]
    public enum FilterControl : byte
    {
        LowPass = 0x10,
        BandPass = 0x20,
        HighPass = 0x40,
        Filter3Off = 0x80
    }

    #endregion

    /// <summary>SID registers</summary>
    public readonly byte[] Registers = new byte[32];

    /// <summary>Voice frequency registers</summary>
    public readonly ushort[] Frequency = new ushort[3];

    /// <summary>Voice pulse width registers</summary>
    public readonly ushort[] PulseWidth = new ushort[3];

    /// <summary>Voice ADSR registers</summary>
    public readonly byte[] AttackDecay = new byte[3];
    public readonly byte[] SustainRelease = new byte[3];

    /// <summary>Filter cutoff frequency</summary>
    public ushort FilterCutoff;

    /// <summary>Master volume</summary>
    public byte Volume => (byte)(Registers[VOLUME] & 0x0F);

    /// <summary>
    /// Reset SID to power on state
    /// </summary>
    public void Reset()
    {
        Array.Clear(Registers, 0, Registers.Length);

        Array.Clear(Frequency, 0, Frequency.Length);
        Array.Clear(PulseWidth, 0, PulseWidth.Length);
        Array.Clear(AttackDecay, 0, AttackDecay.Length);
        Array.Clear(SustainRelease, 0, SustainRelease.Length);

        FilterCutoff = 0;
    }

    /// <summary>
    /// Read SID register
    /// </summary>
    public byte Read(ushort address)
    {
        byte offset = (byte)(address & 0x1F);

        if (offset > 0x1C)
            return 0;

        return Registers[offset];
    }

    /// <summary>
    /// Write SID register
    /// </summary>
    public void Write(ushort address, byte value)
    {
        byte offset = (byte)(address & 0x1F);

        if (offset > 0x18)
            return;

        Registers[offset] = value;

        switch (offset)
        {
            case VOICE1_FREQ_LO: Frequency[0] = (ushort)((Frequency[0] & 0xFF00) | value); break;
            case VOICE1_FREQ_HI: Frequency[0] = (ushort)((Frequency[0] & 0x00FF) | (value << 8)); break;
            case VOICE1_PW_LO: PulseWidth[0] = (ushort)((PulseWidth[0] & 0x0F00) | value); break;
            case VOICE1_PW_HI: PulseWidth[0] = (ushort)((PulseWidth[0] & 0x00FF) | ((value & 0x0F) << 8)); break;
            case VOICE1_ATTACK_DECAY: AttackDecay[0] = value; break;
            case VOICE1_SUSTAIN_RELEASE: SustainRelease[0] = value; break;

            case VOICE2_FREQ_LO: Frequency[1] = (ushort)((Frequency[1] & 0xFF00) | value); break;
            case VOICE2_FREQ_HI: Frequency[1] = (ushort)((Frequency[1] & 0x00FF) | (value << 8)); break;
            case VOICE2_PW_LO: PulseWidth[1] = (ushort)((PulseWidth[1] & 0x0F00) | value); break;
            case VOICE2_PW_HI: PulseWidth[1] = (ushort)((PulseWidth[1] & 0x00FF) | ((value & 0x0F) << 8)); break;
            case VOICE2_ATTACK_DECAY: AttackDecay[1] = value; break;
            case VOICE2_SUSTAIN_RELEASE: SustainRelease[1] = value; break;

            case VOICE3_FREQ_LO: Frequency[2] = (ushort)((Frequency[2] & 0xFF00) | value); break;
            case VOICE3_FREQ_HI: Frequency[2] = (ushort)((Frequency[2] & 0x00FF) | (value << 8)); break;
            case VOICE3_PW_LO: PulseWidth[2] = (ushort)((PulseWidth[2] & 0x0F00) | value); break;
            case VOICE3_PW_HI: PulseWidth[2] = (ushort)((PulseWidth[2] & 0x00FF) | ((value & 0x0F) << 8)); break;
            case VOICE3_ATTACK_DECAY: AttackDecay[2] = value; break;
            case VOICE3_SUSTAIN_RELEASE: SustainRelease[2] = value; break;

            case FILTER_CUTOFF_LO: FilterCutoff = (ushort)((FilterCutoff & 0xFF00) | value); break;
            case FILTER_CUTOFF_HI: FilterCutoff = (ushort)((FilterCutoff & 0x00FF) | ((value & 0x07) << 8)); break;
        }
    }

    /// <summary>
    /// Execute single clock cycle
    /// </summary>
    public void Step()
    {
        // TODO: Waveform generation, envelope generators, filter
    }
}