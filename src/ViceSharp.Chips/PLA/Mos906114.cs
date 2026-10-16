using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Pla;

/// <summary>
/// MOS 906114 Programmable Logic Array banking model.
/// </summary>
public sealed partial class Mos906114 : IAddressSpace, IClockedDevice
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
    /// Processor-port DDR latch. VICE stores the whole written byte.
    /// Bits 6 and 7 are the floating pins.
    /// </summary>
    public byte DataDirection { get; private set; }

    /// <summary>
    /// VICE <c>C64_CPU6510_DATA_PORT_FALL_OFF_CYCLES</c>. Bits 6 and 7
    /// keep the driven level for this many cycles after they become inputs.
    /// The random jitter in c64mem.c is omitted so replay stays deterministic.
    /// </summary>
    public const int DataPortFalloffCycles = 350000;

    private byte _capBit6;
    private byte _capBit7;
    private int _cap6Remaining;
    private int _cap7Remaining;

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
        if (_cap6Remaining > 0 && --_cap6Remaining == 0)
            _capBit6 = 0;
        if (_cap7Remaining > 0 && --_cap7Remaining == 0)
            _capBit7 = 0;
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
        _capBit6 = 0;
        _capBit7 = 0;
        _cap6Remaining = 0;
        _cap7Remaining = 0;
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
        var value = (byte)(output | input);
        if ((DataDirection & 0x40) == 0)
            value = (byte)((value & 0xBF) | _capBit6);
        if ((DataDirection & 0x80) == 0)
            value = (byte)((value & 0x7F) | _capBit7);
        return value;
    }

    /// <summary>
    /// VICE <c>pport.data_set_bit6/7</c> and the falloff clocks. Not part
    /// of the serialized machine snapshot. Replay injects this after the
    /// DDR and data latches are written.
    /// </summary>
    internal readonly struct PortChargeReplay
    {
        public byte Bit6 { get; init; }
        public byte Bit7 { get; init; }
        public int Remaining6 { get; init; }
        public int Remaining7 { get; init; }
    }

    /// <summary>Copies the charged input bits. VICE c64memsnapshot saves them.</summary>
    internal PortChargeReplay CapturePortCharge() => new()
    {
        Bit6 = _capBit6,
        Bit7 = _capBit7,
        Remaining6 = _cap6Remaining,
        Remaining7 = _cap7Remaining
    };

    /// <summary>Writes charged input bits back after the DDR and data latches.</summary>
    internal void InjectPortCharge(PortChargeReplay charge)
    {
        _capBit6 = charge.Bit6;
        _capBit7 = charge.Bit7;
        _cap6Remaining = charge.Remaining6;
        _cap7Remaining = charge.Remaining7;
    }

    /// <summary>
    /// Update the data-direction register. VICE stores the whole byte.
    /// A 1-to-0 edge on bit 6 or 7 starts the discharge countdown.
    /// </summary>
    public void WriteDataDirection(byte value)
    {
        var previous = DataDirection;
        DataDirection = value;
        if ((previous & 0x40) != 0 && (value & 0x40) == 0)
        {
            _capBit6 = (byte)(DataRegister & 0x40);
            _cap6Remaining = DataPortFalloffCycles;
        }

        if ((previous & 0x80) != 0 && (value & 0x80) == 0)
        {
            _capBit7 = (byte)(DataRegister & 0x80);
            _cap7Remaining = DataPortFalloffCycles;
        }
    }

    /// <summary>
    /// Update the data latch. Bits 0-2 propagate to the PLA banking
    /// state (LORAM/HIRAM/CHAREN); other bits are stored for future
    /// board integration.
    /// </summary>
    public void WriteDataPort(byte value)
    {
        DataRegister = value;
        if ((DataDirection & 0x40) != 0)
        {
            _capBit6 = (byte)(value & 0x40);
            _cap6Remaining = DataPortFalloffCycles;
        }

        if ((DataDirection & 0x80) != 0)
        {
            _capBit7 = (byte)(value & 0x80);
            _cap7Remaining = DataPortFalloffCycles;
        }
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
