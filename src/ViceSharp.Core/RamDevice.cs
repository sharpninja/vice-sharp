using ViceSharp.Abstractions;

namespace ViceSharp.Core;

/// <summary>
/// Read/Write RAM device occupying a fixed address range
/// </summary>
public sealed class RamDevice : IAddressSpace
{
    private readonly ushort _startAddress;
    private readonly ushort _endAddress;
    private readonly byte[] _memory;

    public RamDevice(ushort startAddress, ushort endAddress, byte[] memory)
    {
        _startAddress = startAddress;
        _endAddress = endAddress;
        _memory = memory;
    }

    public byte Read(ushort address) => _memory[address - _startAddress];
    public void Write(ushort address, byte value) => _memory[address - _startAddress] = value;
    public byte Peek(ushort address) => _memory[address - _startAddress];
    public bool HandlesAddress(ushort address) => address >= _startAddress && address <= _endAddress;

    public DeviceId Id => new DeviceId(0x00010000);
    public string Name => "System RAM";
    public void Reset() { }
}