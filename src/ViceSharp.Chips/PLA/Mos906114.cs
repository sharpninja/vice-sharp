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
    /// VICE-style: Memory configuration from control register
    /// </summary>
    public MemoryConfig GetMemoryConfig()
    {
        bool loram = Loram;
        bool hiram = Hiram;
        bool charen = Charen;
        
        return (loram, hiram, charen) switch
        {
            (false, false, false) => MemoryConfig.RamRamRam,
            (false, false, true) => MemoryConfig.RamRamChar,
            (false, true, false) => MemoryConfig.RamKernalRam,
            (false, true, true) => MemoryConfig.RamKernalChar,
            (true, false, false) => MemoryConfig.RamRamRam,
            (true, false, true) => MemoryConfig.BasicRamChar,
            (true, true, false) => MemoryConfig.RamKernalRam,
            (true, true, true) => MemoryConfig.BasicKernalChar,
        };
    }
    
    /// <summary>
    /// Check if VIC-II has access to character ROM
    /// </summary>
    public bool VicHasCharacterRom => Loram && Charen;
    
    /// <summary>
    /// Check if BASIC ROM is visible
    /// </summary>
    public bool BasicRomVisible => Loram && Hiram;
    
    /// <summary>
    /// Check if KERNAL ROM is visible
    /// </summary>
    public bool KernalRomVisible => Hiram;
    
    /// <summary>
    /// Memory banking configuration (VICE-style)
    /// </summary>
    public enum MemoryConfig
    {
        RamRamRam,
        RamRamChar,
        RamKernalRam,
        RamKernalChar,
        BasicRamChar,
        RamKernalRam2,
        BasicKernalChar
    }
}
