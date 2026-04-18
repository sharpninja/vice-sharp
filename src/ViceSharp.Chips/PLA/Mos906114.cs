using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Pla;

/// <summary>
/// MOS 906114 Programmable Logic Array - C64 Memory Banking Controller.
/// </summary>
public sealed class Mos906114 : IAddressSpace, IClockedDevice
{
    public DeviceId Id => new DeviceId(0x0007);
    public DeviceId SourceId => Id;
    public string Name => "MOS 906114 PLA";
    public uint ClockDivisor => 1;
    public ClockPhase Phase => ClockPhase.Phi1;

    public ushort BaseAddress { get; init; } = 0x0001;
    public ushort Size => 1;
    public bool IsReadOnly => false;

    public byte ControlRegister { get; private set; }

    public bool Loram => (ControlRegister & 0x01) != 0;
    public bool Hiram => (ControlRegister & 0x02) != 0;
    public bool Charen => (ControlRegister & 0x04) != 0;

    private readonly IBus _bus;

    public Mos906114(IBus bus)
    {
        _bus = bus;
    }

    /// <inheritdoc />
    public void Tick()
    {
        // PLA banking logic runs synchronously with Phi1
    }

    /// <inheritdoc />
    public void Initialize()
    {
        Reset();
    }

    /// <inheritdoc />
    public void Reset()
    {
        ControlRegister = 0x37; // Default state on power up
    }

    /// <inheritdoc />
    public byte Peek(ushort offset) => Read(offset);

    /// <inheritdoc />
    public byte Read(ushort offset)
    {
        return ControlRegister;
    }

    /// <inheritdoc />
    public void Write(ushort offset, byte value)
    {
        ControlRegister = value;

        // Memory banking combinations:
        //
        // 000: RAM     RAM     RAM
        // 001: RAM     RAM     CHAR
        // 010: RAM     KERNAL  RAM
        // 011: RAM     KERNAL  CHAR
        // 100: RAM     RAM     RAM
        // 101: BASIC   RAM     CHAR
        // 110: RAM     KERNAL  RAM
        // 111: BASIC   KERNAL  CHAR
    }

    /// <inheritdoc />
    public bool HandlesAddress(ushort address)
    {
        return address == 0x0001;
    }

    /// <summary>
    /// Resolve mapped device for given address
    /// </summary>
    public DeviceId MapAddress(ushort address)
    {
        return new DeviceId(0);
    }
}