namespace ViceSharp.Core;

/// <summary>
/// System memory bus
/// </summary>
public sealed class SystemBus
{
    private readonly byte[] _ram = new byte[65536];

    /// <summary>
    /// Read byte from memory
    /// </summary>
    public byte Read(ushort address)
    {
        return _ram[address];
    }

    /// <summary>
    /// Write byte to memory
    /// </summary>
    public void Write(ushort address, byte value)
    {
        _ram[address] = value;
    }

    /// <summary>
    /// Read 16 bit word from memory
    /// </summary>
    public ushort ReadWord(ushort address)
    {
        return (ushort)(_ram[address] | (_ram[address + 1] << 8));
    }

    /// <summary>
    /// Reset bus
    /// </summary>
    public void Reset()
    {
        Array.Clear(_ram, 0, _ram.Length);
    }
}