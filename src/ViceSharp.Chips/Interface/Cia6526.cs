using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Interface;

public sealed partial class Cia6526 : IClockedDevice, IAddressSpace, ICiaChip, IInterruptSource
{
    public DeviceId Id => new DeviceId(0x0003);
    public string Name => "MOS 6526 CIA";
    public uint ClockDivisor => 1;
    public ClockPhase Phase => ClockPhase.Phi2;

    public byte PortA { get => _portA; set => _portA = value; }
    public byte PortB { get => _portB; set => _portB = value; }
    public byte DdrA { get => _ddrA; set => _ddrA = value; }
    public byte DdrB { get => _ddrB; set => _ddrB = value; }
    public ushort TimerA { get => _timerA; set => _timerA = value; }
    public ushort TimerB { get => _timerB; set => _timerB = value; }

    public DeviceId SourceId => Id;
    public IReadOnlyList<IInterruptLine> ConnectedLines => new[] { _irqLine };

    private readonly IBus _bus;
    private readonly IInterruptLine _irqLine;

    // CIA Registers
    private byte[] _registers = new byte[0x10];

    // Timer state
    private ushort _timerA;
    private ushort _timerALatch;
    private ushort _timerB;
    private ushort _timerBLatch;
    private bool _timerARunning;
    private bool _timerBRunning;

    // I/O Ports
    private byte _portA;
    private byte _portB;
    private byte _ddrA;
    private byte _ddrB;

    // Interrupt state
    private byte _interruptMask;
    private byte _interruptFlag;

    public Cia6526(IBus bus, IInterruptLine irqLine)
    {
        _bus = bus;
        _irqLine = irqLine;
    }

    public void Tick()
    {
        // Timer A decrement
        if (_timerARunning)
        {
            _timerA--;
            if (_timerA == 0)
            {
                _timerA = _timerALatch;
                _interruptFlag |= 0x01;
                
                if ((_interruptMask & 0x01) != 0)
                {
                    _irqLine.Assert(this);
                }
            }
        }

        // Timer B decrement
        if (_timerBRunning)
        {
            _timerB--;
            if (_timerB == 0)
            {
                _timerB = _timerBLatch;
                _interruptFlag |= 0x02;
                
                if ((_interruptMask & 0x02) != 0)
                {
                    _irqLine.Assert(this);
                }
            }
        }

        // Update registers
        _registers[0x04] = (byte)_timerA;
        _registers[0x05] = (byte)(_timerA >> 8);
        _registers[0x06] = (byte)_timerB;
        _registers[0x07] = (byte)(_timerB >> 8);
    }

    public void Reset()
    {
        Array.Clear(_registers);
        _timerA = 0xFFFF;
        _timerALatch = 0xFFFF;
        _timerB = 0xFFFF;
        _timerBLatch = 0xFFFF;
        _timerARunning = false;
        _timerBRunning = false;
        _interruptMask = 0;
        _interruptFlag = 0;
        _portA = 0;
        _portB = 0;
        _ddrA = 0;
        _ddrB = 0;
    }

    public byte Read(ushort address)
    {
        int register = address & 0x0F;

        if (register == 0x0D)
        {
            // Interrupt flag register - read clears IRQ
            byte value = _interruptFlag;
            _interruptFlag = 0;
            _irqLine.Release(this);
            return value;
        }

        return _registers[register];
    }

    public void Write(ushort address, byte value)
    {
        int register = address & 0x0F;
        _registers[register] = value;

        switch (register)
        {
            case 0x00:
                // Port A
                _portA = (byte)((_portA & ~_ddrA) | (value & _ddrA));
                break;
            case 0x01:
                // Port B
                _portB = (byte)((_portB & ~_ddrB) | (value & _ddrB));
                break;
            case 0x02:
                // DDR A
                _ddrA = value;
                break;
            case 0x03:
                // DDR B
                _ddrB = value;
                break;
            case 0x04:
                // Timer A low
                _timerALatch = (ushort)((_timerALatch & 0xFF00) | value);
                break;
            case 0x05:
                // Timer A high
                _timerALatch = (ushort)((_timerALatch & 0x00FF) | (value << 8));
                _timerA = _timerALatch;
                break;
            case 0x06:
                // Timer B low
                _timerBLatch = (ushort)((_timerBLatch & 0xFF00) | value);
                break;
            case 0x07:
                // Timer B high
                _timerBLatch = (ushort)((_timerBLatch & 0x00FF) | (value << 8));
                _timerB = _timerBLatch;
                break;
            case 0x0E:
                // Interrupt mask
                if ((value & 0x80) != 0)
                    _interruptMask |= (byte)(value & 0x7F);
                else
                    _interruptMask &= (byte)~(value & 0x7F);
                break;
            case 0x0F:
                // Control register B
                _timerBRunning = (value & 0x01) != 0;
                break;
        }
    }

    public byte Peek(ushort address)
    {
        return Read(address);
    }

    public bool HandlesAddress(ushort address)
    {
        return address >= 0xDC00 && address <= 0xDCFF;
    }
}