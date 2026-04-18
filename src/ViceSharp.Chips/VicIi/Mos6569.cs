using ViceSharp.Abstractions;

namespace ViceSharp.Chips.VicIi;

/// <summary>
/// MOS 6569 VIC-II Video Interface Controller implementation.
/// </summary>
public sealed class Mos6569 : IVideoChip, IAddressSpace, IInterruptSource
{
    public DeviceId Id => new DeviceId(0x0003);
    public DeviceId SourceId => Id;
    public string Name => "MOS 6569 VIC-II";
    public uint ClockDivisor => 1;
    public ClockPhase Phase => ClockPhase.Phi1;

    public ushort BaseAddress { get; init; } = 0xD000;
    public ushort Size => 64;
    public bool IsReadOnly => false;

    // VIC-II registers
    private readonly byte[] _registers = new byte[64];

    public ushort CurrentRasterLine { get; private set; }
    public int CyclesPerLine => 63;
    public int VisibleLines => 200;
    public int TotalLines => 312;
    public bool IsVBlank => CurrentRasterLine >= VisibleLines;

    public byte RasterX;
    public uint CycleCounter;

    private readonly IBus _bus;
    private readonly IInterruptLine _irqLine;

    public IReadOnlyList<IInterruptLine> ConnectedLines => new[] { _irqLine };

    public Mos6569(IBus bus, IInterruptLine irqLine)
    {
        _bus = bus;
        _irqLine = irqLine;
    }

    /// <inheritdoc />
    public void Tick()
    {
        CycleCounter++;
        RasterX++;

        if (RasterX >= CyclesPerLine)
        {
            RasterX = 0;
            CurrentRasterLine++;

            if (CurrentRasterLine >= TotalLines)
            {
                CurrentRasterLine = 0;
            }

            // Raster interrupt compare
            ushort rasterIrq = (ushort)(((_registers[0x11] & 0x80) << 1) | _registers[0x12]);
            if (CurrentRasterLine == rasterIrq && (_registers[0x1A] & 0x01) != 0)
            {
                _registers[0x19] |= 0x01;
                _irqLine.Assert(this);
            }
        }

        // Update raster register
        _registers[0x12] = (byte)CurrentRasterLine;
        if ((CurrentRasterLine & 0x100) != 0)
        {
            _registers[0x11] |= 0x80;
        }
        else
        {
            _registers[0x11] &= 0x7F;
        }
    }

    /// <inheritdoc />
    public void Initialize()
    {
        Reset();
    }

    /// <inheritdoc />
    public void Reset()
    {
        Array.Clear(_registers, 0, _registers.Length);
        CurrentRasterLine = 0;
        RasterX = 0;
        CycleCounter = 0;
    }

    /// <inheritdoc />
    public byte Peek(ushort offset)
    {
        return Read(offset);
    }

    /// <inheritdoc />
    public byte Read(ushort offset)
    {
        if (offset >= Size) return 0xFF;

        if (offset == 0x19)
        {
            byte value = _registers[0x19];
            _registers[0x19] &= 0x7F;
            return value;
        }

        return _registers[offset];
    }

    /// <inheritdoc />
    public void Write(ushort offset, byte value)
    {
        if (offset >= Size) return;

        if (offset == 0x19)
        {
            _registers[offset] &= (byte)~value;
            return;
        }

        _registers[offset] = value;
    }

    /// <inheritdoc />
    public bool HandlesAddress(ushort address)
    {
        return address >= BaseAddress && address < BaseAddress + Size;
    }
}