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

    /// <summary>
    /// Reset the pulse cursor to the start of the pulse data, equivalent to
    /// rewinding the tape to position zero.
    /// </summary>
    public void Rewind()
    {
        _offset = 0;
    }

    /// <summary>Byte cursor in the pulse data. VICE stores the tape image position.</summary>
    public int Offset => _offset;

    /// <summary>Puts the cursor back. Does not re-read the pulses in front of it.</summary>
    public void RestoreOffset(int offset)
    {
        if (offset < 0)
            offset = 0;
        if (offset > _pulseData.Length)
            offset = _pulseData.Length;
        _offset = offset;
    }

    /// <summary>
    /// Try to position the cursor at the given pulse index by iterating
    /// pulses from the start. Returns false if the index is negative or
    /// exceeds the pulse count; on failure the cursor is left unchanged.
    /// </summary>
    public bool TrySeekToPulse(int pulseIndex)
    {
        if (pulseIndex < 0)
        {
            return false;
        }

        var saved = _offset;
        _offset = 0;

        for (var i = 0; i < pulseIndex; i++)
        {
            if (!TryReadNextPulse(out _))
            {
                _offset = saved;
                return false;
            }
        }

        return true;
    }
}
