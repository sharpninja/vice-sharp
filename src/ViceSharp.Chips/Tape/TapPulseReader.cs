namespace ViceSharp.Chips.Tape;

public sealed class TapPulseReader
{
    private readonly byte[] _pulseData;
    private readonly byte _version;
    private int _offset;

    internal TapPulseReader(byte[] pulseData, byte version)
    {
        _pulseData = pulseData;
        _version = version;
    }

    public bool TryReadNextPulse(out int cycles)
    {
        cycles = 0;

        if (_offset >= _pulseData.Length)
        {
            return false;
        }

        var value = _pulseData[_offset++];
        if (value != 0)
        {
            cycles = value * 8;
            return true;
        }

        if (_offset + 3 > _pulseData.Length)
        {
            return false;
        }

        cycles = _pulseData[_offset]
            | (_pulseData[_offset + 1] << 8)
            | (_pulseData[_offset + 2] << 16);
        _offset += 3;
        return true;
    }
}
