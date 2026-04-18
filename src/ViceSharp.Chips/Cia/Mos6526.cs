using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Cia;

/// <summary>
/// MOS 6526 Complex Interface Adapter implementation.
/// </summary>
public sealed class Mos6526 : ICiaChip, IAddressSpace
{
    public DeviceId Id => new DeviceId(0x0002);
    public DeviceId SourceId => Id;
    public string Name => "MOS 6526 CIA";
    public uint ClockDivisor => 1;
    public ClockPhase Phase => ClockPhase.Phi2;

    public ushort BaseAddress { get; init; } = 0xDC00;
    public ushort Size => 16;
    public bool IsReadOnly => false;

    // Registers
    public byte PortA { get; set; }
    public byte PortB { get; set; }
    public byte DdrA { get; set; }
    public byte DdrB { get; set; }
    public ushort TimerA { get; set; }
    public ushort TimerB { get; set; }

    public byte TimerALatch;
    public byte TimerBLatch;
    public byte TimerAHiLatch;
    public byte TimerBHiLatch;
    public byte ControlA;
    public byte ControlB;
    public byte InterruptEnable;
    public byte InterruptFlag;

    private byte _tod10ths;
    private byte _todSeconds;
    private byte _todMinutes;
    private byte _todHours;

    private readonly IBus _bus;
    private readonly IInterruptLine _irqLine;

    public IReadOnlyList<IInterruptLine> ConnectedLines => new[] { _irqLine };

    public Mos6526(IBus bus, IInterruptLine irqLine)
    {
        _bus = bus;
        _irqLine = irqLine;
    }

    /// <inheritdoc />
    public void Tick()
    {
        // Timer A
        if ((ControlA & 0x01) != 0)
        {
            TimerA--;
            if (TimerA == 0)
            {
                if ((ControlA & 0x08) != 0)
                {
                    TimerA = (ushort)((TimerAHiLatch << 8) | TimerALatch);
                }
                else
                {
                    ControlA &= 0xFE;
                }

                if ((InterruptEnable & 0x01) != 0)
                {
                    InterruptFlag |= 0x01;
                    _irqLine.Assert(this);
                }
            }
        }

        // Timer B
        if ((ControlB & 0x01) != 0)
        {
            TimerB--;
            if (TimerB == 0)
            {
                if ((ControlB & 0x08) != 0)
                {
                    TimerB = (ushort)((TimerBHiLatch << 8) | TimerBLatch);
                }
                else
                {
                    ControlB &= 0xFE;
                }

                if ((InterruptEnable & 0x02) != 0)
                {
                    InterruptFlag |= 0x02;
                    _irqLine.Assert(this);
                }
            }
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
        PortA = 0;
        PortB = 0;
        DdrA = 0;
        DdrB = 0;
        TimerA = 0xFFFF;
        TimerB = 0xFFFF;
        TimerALatch = 0xFF;
        TimerBLatch = 0xFF;
        TimerAHiLatch = 0xFF;
        TimerBHiLatch = 0xFF;
        ControlA = 0;
        ControlB = 0;
        InterruptEnable = 0;
        InterruptFlag = 0;
    }

    /// <inheritdoc />
    public byte Peek(ushort offset) => Read(offset);

    /// <inheritdoc />
    public byte Read(ushort offset)
    {
        return offset switch
        {
            0x00 => (byte)((PortA & DdrA) | (PortA & ~DdrA)),
            0x01 => (byte)((PortB & DdrB) | (PortB & ~DdrB)),
            0x02 => DdrA,
            0x03 => DdrB,
            0x04 => (byte)TimerA,
            0x05 => (byte)(TimerA >> 8),
            0x06 => (byte)TimerB,
            0x07 => (byte)(TimerB >> 8),
            0x08 => _tod10ths,
            0x09 => _todSeconds,
            0x0A => _todMinutes,
            0x0B => _todHours,
            0x0C => 0, // Serial data
            0x0D => InterruptFlag,
            0x0E => ControlA,
            0x0F => ControlB,
            _ => 0xFF
        };
    }

    /// <inheritdoc />
    public void Write(ushort offset, byte value)
    {
        switch (offset)
        {
            case 0x00: PortA = value; break;
            case 0x01: PortB = value; break;
            case 0x02: DdrA = value; break;
            case 0x03: DdrB = value; break;
            case 0x04:
                TimerALatch = value;
                if ((ControlA & 0x01) == 0) TimerA = (ushort)((TimerA & 0xFF00) | value);
                break;
            case 0x05:
                TimerAHiLatch = value;
                TimerA = (ushort)((value << 8) | TimerALatch);
                ControlA |= 0x01;
                break;
            case 0x06:
                TimerBLatch = value;
                if ((ControlB & 0x01) == 0) TimerB = (ushort)((TimerB & 0xFF00) | value);
                break;
            case 0x07:
                TimerBHiLatch = value;
                TimerB = (ushort)((value << 8) | TimerBLatch);
                ControlB |= 0x01;
                break;
            case 0x08: _tod10ths = value; break;
            case 0x09: _todSeconds = value; break;
            case 0x0A: _todMinutes = value; break;
            case 0x0B: _todHours = value; break;
            case 0x0D: InterruptEnable = value; break;
            case 0x0E: ControlA = value; break;
            case 0x0F: ControlB = value; break;
        }
    }

    /// <inheritdoc />
    public bool HandlesAddress(ushort address)
    {
        return address >= BaseAddress && address < BaseAddress + Size;
    }
}