using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Cpu;

public sealed class Mos6502 : IClockedDevice, IAddressSpace
{
    public DeviceId Id => new DeviceId(0x0001);
    public string Name => "MOS 6502 CPU";
    public uint ClockDivisor => 1;

    // Registers
    public byte A;
    public byte X;
    public byte Y;
    public byte S;
    public ushort PC;
    public byte P;

    private readonly IBus _bus;

    public Mos6502(IBus bus)
    {
        _bus = bus;
    }

    public void Tick()
    {
        // Execution cycle will be implemented here
    }

    public void Reset()
    {
        A = 0;
        X = 0;
        Y = 0;
        S = 0xFD;
        P = 0x24;
        PC = _bus.Read(0xFFFC);
        PC |= (ushort)(_bus.Read(0xFFFD) << 8);
    }

    public byte Read(ushort address) => _bus.Read(address);
    public void Write(ushort address, byte value) => _bus.Write(address, value);
    public byte Peek(ushort address) => _bus.Peek(address);
    public bool HandlesAddress(ushort address) => false;
}