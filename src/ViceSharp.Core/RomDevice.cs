using ViceSharp.Abstractions;

namespace ViceSharp.Core;

/// <summary>
/// Read Only ROM device occupying a fixed address range
/// </summary>
public sealed class RomDevice : IAddressSpace
{
    private readonly ushort _startAddress;
    private readonly ushort _endAddress;
    private readonly byte[] _memory;

    public RomDevice(ushort startAddress, ushort endAddress, byte[] memory)
    {
        _startAddress = startAddress;
        _endAddress = endAddress;
        _memory = memory;
    }

    public byte Read(ushort address) => _memory[address - _startAddress];
    public void Write(ushort address, byte value) { /* Writes ignored for ROM */ }
    public byte Peek(ushort address) => _memory[address - _startAddress];
    public bool HandlesAddress(ushort address) => address >= _startAddress && address <= _endAddress;

    public DeviceId Id => new DeviceId(0x00010001);
    public string Name => "System ROM";
    public void Reset() { }
}