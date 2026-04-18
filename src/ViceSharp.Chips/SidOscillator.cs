namespace ViceSharp.Chips;

/// <summary>
/// SID Voice Oscillator
/// </summary>
public struct SidOscillator
{
    /// <summary>24 bit phase accumulator</summary>
    public uint Accumulator;

    /// <summary>16 bit frequency</summary>
    public ushort Frequency;

    /// <summary>Waveform control</summary>
    public byte Waveform;

    /// <summary>Pulse width 12 bit</summary>
    public ushort PulseWidth;

    /// <summary>
    /// Step oscillator one cycle
    /// </summary>
    public void Step()
    {
        Accumulator += Frequency;
    }

    /// <summary>
    /// Get current output sample
    /// </summary>
    public byte Output()
    {
        // Triangle
        if ((Waveform & 0x10) != 0)
        {
            uint t = Accumulator >> 16;
            if ((t & 0x80) != 0)
                t = ~t;
            return (byte)(t ^ ((Waveform & 0x80) != 0 ? 0xFF : 0));
        }

        // Sawtooth
        if ((Waveform & 0x20) != 0)
        {
            return (byte)((Accumulator >> 16) ^ ((Waveform & 0x80) != 0 ? 0xFF : 0));
        }

        // Pulse
        if ((Waveform & 0x40) != 0)
        {
            return ((Accumulator >> 16) < PulseWidth) ? (byte)0xFF : (byte)0;
        }

        // Noise
        if ((Waveform & 0x80) != 0)
        {
            // TODO: LFSR noise generation
            return 0;
        }

        return 0;
    }
}