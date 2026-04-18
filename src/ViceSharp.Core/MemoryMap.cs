namespace ViceSharp.Core;

/// <summary>
/// Memory map address decode
/// </summary>
public sealed class MemoryMap
{
    public delegate byte ReadHandler(ushort address);
    public delegate void WriteHandler(ushort address, byte value);

    private readonly ReadHandler[] _readHandlers = new ReadHandler[256];
    private readonly WriteHandler[] _writeHandlers = new WriteHandler[256];

    public MemoryMap()
    {
        // Default to open bus
        for (int i = 0; i < 256; i++)
        {
            _readHandlers[i] = _ => 0xFF;
            _writeHandlers[i] = (_, _) => { };
        }
    }

    /// <summary>
    /// Map address range to handlers
    /// </summary>
    public void Map(ushort start, ushort end, ReadHandler read, WriteHandler write)
    {
        for (int page = start >> 8; page <= end >> 8; page++)
        {
            _readHandlers[page] = read;
            _writeHandlers[page] = write;
        }
    }

    /// <summary>
    /// Read byte from memory map
    /// </summary>
    public byte Read(ushort address)
    {
        return _readHandlers[address >> 8](address);
    }

    /// <summary>
    /// Write byte to memory map
    /// </summary>
    public void Write(ushort address, byte value)
    {
        _writeHandlers[address >> 8](address, value);
    }
}