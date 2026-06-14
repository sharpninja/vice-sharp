using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Pla;

/// <summary>
/// MOS 906114 Programmable Logic Array banking model.
/// </summary>
public sealed class Mos906114 : IAddressSpace, IClockedDevice
{
    public DeviceId Id => new DeviceId(0x0007);
    public DeviceId SourceId => Id;
    public string Name => "MOS 906114 PLA";
    public uint ClockDivisor => 1;
    public ClockPhase Phase => ClockPhase.Phi1;

    public ushort BaseAddress { get; init; }
    public ushort Size => 2;
    public bool IsReadOnly => false;

    /// <summary>
    /// Processor-port DDR latch. Bits 0-5 select pin direction
    /// (1 = output, 0 = input); bits 6-7 are unused on the 6510 and
    /// always read zero.
    /// </summary>
    public byte DataDirection { get; private set; }

    /// <summary>
    /// Processor-port data latch. Reads through
    /// <see cref="ReadProcessorPort"/> mix this latch with the DDR mask;
    /// the banking selector exposes the latch directly via
    /// <see cref="Loram"/>, <see cref="Hiram"/> and <see cref="Charen"/>.
    /// </summary>
    public byte DataRegister { get; private set; }

    /// <summary>
    /// Backwards-compatible alias for <see cref="DataRegister"/>. Existing
    /// debugger / lockstep paths read the raw latch via this name.
    /// </summary>
    public byte ControlRegister => DataRegister;

    /// <summary>
    /// External pull-up state applied to input bits when the processor
    /// port is read. The simple model used here returns 0 for every
    /// unconnected input bit; board integration owns any external line
    /// state or NMOS capacitor decay model.
    /// </summary>
    public byte InputPullUp { get; set; } = 0x00;

    public bool Loram => (DataRegister & 0x01) != 0;
    public bool Hiram => (DataRegister & 0x02) != 0;
    public bool Charen => (DataRegister & 0x04) != 0;

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
        DataDirection = 0x00;
        DataRegister = 0x00;
    }

    /// <inheritdoc />
    public byte Peek(ushort offset) => Read(offset);

    /// <inheritdoc />
    public byte Read(ushort address)
    {
        var register = (ushort)(address - BaseAddress);
        return (register & 0x0001) == 0
            ? DataDirection
            : ReadProcessorPort();
    }

    /// <summary>
    /// Compute the value visible on the processor data port. Output bits
    /// (DDR=1) return the latched data; input bits (DDR=0) return the
    /// external pull-up state.
    /// </summary>
    public byte ReadProcessorPort()
    {
        var output = (byte)(DataRegister & DataDirection);
        var input = (byte)(InputPullUp & (byte)~DataDirection);
        return (byte)((output | input) & 0x3F);
    }

    /// <summary>
    /// Update the data-direction register. Bits 6-7 are masked off
    /// since they are not implemented on the chip.
    /// </summary>
    public void WriteDataDirection(byte value)
    {
        DataDirection = (byte)(value & 0x3F);
    }

    /// <summary>
    /// Update the data latch. Bits 0-2 propagate to the PLA banking
    /// state (LORAM/HIRAM/CHAREN); other bits are stored for future
    /// board integration.
    /// </summary>
    public void WriteDataPort(byte value)
    {
        DataRegister = (byte)(value & 0x3F);
    }

    /// <inheritdoc />
    public void Write(ushort address, byte value)
    {
        var register = (ushort)(address - BaseAddress);
        if ((register & 0x0001) == 0)
        {
            WriteDataDirection(value);
        }
        else
        {
            WriteDataPort(value);
        }

        // Memory banking combinations (data latch bits 0-2 -> ROM map):
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
        return address >= BaseAddress && address < BaseAddress + Size;
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
