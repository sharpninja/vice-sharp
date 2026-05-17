namespace ViceSharp.Chips;

/// <summary>
/// SID Voice Oscillator
/// </summary>
public struct SidOscillator
{
    private const uint AccumulatorMask = 0x00FF_FFFF;
    private const uint NoiseLfsrMask = 0x007F_FFFF;
    private const uint NoiseInitialLfsr = NoiseLfsrMask;
    private const uint NoiseClockBit = 1u << 19;

    private uint _noiseLfsr;

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
        if ((Waveform & 0x08) != 0)
        {
            _noiseLfsr = NoiseInitialLfsr;
            Accumulator = 0;
            return;
        }

        var previousAccumulator = Accumulator;
        Accumulator = (Accumulator + Frequency) & AccumulatorMask;

        if ((Waveform & 0x80) != 0 &&
            (previousAccumulator & NoiseClockBit) == 0 &&
            (Accumulator & NoiseClockBit) != 0)
        {
            ClockNoiseLfsr();
        }
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
            EnsureNoiseLfsrInitialized();
            return NoiseOutput();
        }

        return 0;
    }

    private void ClockNoiseLfsr()
    {
        EnsureNoiseLfsrInitialized();

        var feedback = ((_noiseLfsr >> 22) ^ (_noiseLfsr >> 17)) & 0x01;
        _noiseLfsr = ((_noiseLfsr << 1) | feedback) & NoiseLfsrMask;
        if (_noiseLfsr == 0)
            _noiseLfsr = NoiseInitialLfsr;
    }

    private void EnsureNoiseLfsrInitialized()
    {
        if (_noiseLfsr == 0)
            _noiseLfsr = NoiseInitialLfsr;
    }

    private readonly byte NoiseOutput()
    {
        return (byte)(
            (((_noiseLfsr >> 20) & 0x01) << 7) |
            (((_noiseLfsr >> 18) & 0x01) << 6) |
            (((_noiseLfsr >> 14) & 0x01) << 5) |
            (((_noiseLfsr >> 11) & 0x01) << 4) |
            (((_noiseLfsr >> 9) & 0x01) << 3) |
            (((_noiseLfsr >> 5) & 0x01) << 2) |
            (((_noiseLfsr >> 2) & 0x01) << 1) |
            ((_noiseLfsr >> 0) & 0x01));
    }
}
