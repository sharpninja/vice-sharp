using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Cia;

/// <summary>
/// MOS 6526 CIA (Complex Interface Adapter) chip.
/// Timer and I/O port emulation with VICE-compatible behavior.
/// </summary>
public sealed class Mos6526 : IClockedDevice, IAddressSpace, IInterruptSource
{
    public DeviceId Id => new DeviceId(0x0005);
    public DeviceId SourceId => Id;
    public string Name => "MOS 6526 CIA";
    public uint ClockDivisor => 1;
    public ClockPhase Phase => ClockPhase.Phi2;
    public ushort BaseAddress { get; init; } = 0xDC00;
    public ushort Size => 64;
    
    // Timer modes
    public enum TimerMode { OneShot, Continuous, Cascade }
    
    // Timer output modes
    public enum TimerOutput { Toggle, Pulse, Continuous }
    
    // Port modes
    public enum PortMode { Input, Output }
    
    // TOD alarm modes
    public enum TodMode { Hours12, Hours24, Alarm12, Alarm24 }
    
    // Interrupt sources
    public enum IrqSource { None, UnderflowA, UnderflowB, Alarm, Serial, Flag }

    // Timer A state
    private TimerState _timerA = new();
    private TimerState _timerB = new();
    
    private struct TimerState
    {
        public ushort Latch;
        public ushort Counter;
        public byte Control;
        public bool Running;
        public int Divider;
    }

    // I/O Ports
    private byte _portA;
    private byte _portB;
    private byte _portADir;
    private byte _portBDir;
    
    // TOD (Time of Day) clock - VICE-style
    private byte _todTenths;
    private byte _todSeconds;
    private byte _todMinutes;
    private byte _todHours;
    private byte _todAlarmTenths;
    private byte _todAlarmSeconds;
    private byte _todAlarmMinutes;
    private byte _todAlarmHours;
    
    // Interrupt state
    private byte _interruptMask;
    private byte _interruptFlags;
    
    private readonly IBus _bus;
    private readonly IInterruptLine _irqLine;
    public IReadOnlyList<IInterruptLine> ConnectedLines => new[] { _irqLine };
    
    // Registers
    private byte[] _registers = new byte[0x10];

    public Mos6526(IBus bus, IInterruptLine irqLine)
    {
        _bus = bus;
        _irqLine = irqLine;
    }

    /// <summary>
    /// TOD is clocked by Timer B output when in 24-hour mode (VICE-style)
    /// </summary>
    public void ClockTod()
    {
        _todTenths = (byte)((_todTenths + 1) % 10);
        if (_todTenths == 0)
        {
            _todSeconds = (byte)((_todSeconds + 1) % 60);
            if (_todSeconds == 0)
            {
                _todMinutes = (byte)((_todMinutes + 1) % 60);
                if (_todMinutes == 0)
                {
                    _todHours = (byte)((_todHours + 1) % 24);
                }
            }
        }
        
        // Check alarm match
        if (_todTenths == _todAlarmTenths && 
            _todSeconds == _todAlarmSeconds && 
            _todMinutes == _todAlarmMinutes && 
            _todHours == _todAlarmHours)
        {
            _interruptFlags |= 0x04;
            if ((_interruptMask & 0x04) != 0)
                _irqLine.Assert(this);
        }
    }

    public void Tick()
    {
        // Timer A
        if (_timerA.Running)
        {
            _timerA.Divider--;
            if (_timerA.Divider <= 0)
            {
                _timerA.Divider = _timerA.Latch > 0 ? _timerA.Latch : 0x10000;
                _timerA.Counter--;
                if (_timerA.Counter == 0xFFFF)
                {
                    _timerA.Counter = _timerA.Latch;
                    _interruptFlags |= 0x01;
                    if ((_interruptMask & 0x01) != 0)
                        _irqLine.Assert(this);
                }
            }
        }
        
        // Timer B
        if (_timerB.Running)
        {
            _timerB.Divider--;
            if (_timerB.Divider <= 0)
            {
                _timerB.Divider = _timerB.Latch > 0 ? _timerB.Latch : 0x10000;
                _timerB.Counter--;
                if (_timerB.Counter == 0xFFFF)
                {
                    _timerB.Counter = _timerB.Latch;
                    _interruptFlags |= 0x02;
                    if ((_interruptMask & 0x02) != 0)
                        _irqLine.Assert(this);
                }
            }
        }
    }

    public void Reset()
    {
        Array.Clear(_registers);
        _timerA = new();
        _timerB = new();
        _portA = 0;
        _portB = 0;
        _portADir = 0;
        _portBDir = 0;
        _todTenths = 0;
        _todSeconds = 0;
        _todMinutes = 0;
        _todHours = 0;
        _interruptMask = 0;
        _interruptFlags = 0;
    }

    public byte Peek(ushort address) => Read(address);

    public byte Read(ushort address)
    {
        int register = address & 0x0F;
        return register switch
        {
            0x04 => (byte)_timerA.Counter,
            0x05 => (byte)(_timerA.Counter >> 8),
            0x06 => (byte)_timerB.Counter,
            0x07 => (byte)(_timerB.Counter >> 8),
            0x08 => _portA,
            0x09 => _portB,
            0x0D => _interruptFlags,
            _ => _registers[register]
        };
    }

    public void Write(ushort address, byte value)
    {
        int register = address & 0x0F;
        _registers[register] = value;
        
        switch (register)
        {
            // Timer A
            case 0x04: _timerA.Latch = (ushort)((_timerA.Latch & 0xFF00) | value); break;
            case 0x05: 
                _timerA.Latch = (ushort)((_timerA.Latch & 0x00FF) | (value << 8));
                if ((_timerA.Control & 0x01) == 0)
                    _timerA.Counter = _timerA.Latch;
                break;
            case 0x0E:
                _timerA.Control = value;
                _timerA.Running = (value & 0x01) != 0;
                if ((value & 0x10) != 0)
                    _timerA.Counter = _timerA.Latch;
                _timerA.Divider = _timerA.Latch > 0 ? _timerA.Latch : 0x10000;
                break;
                
            // Timer B
            case 0x06: _timerB.Latch = (ushort)((_timerB.Latch & 0xFF00) | value); break;
            case 0x07:
                _timerB.Latch = (ushort)((_timerB.Latch & 0x00FF) | (value << 8));
                if ((_timerB.Control & 0x01) == 0)
                    _timerB.Counter = _timerB.Latch;
                break;
            case 0x0F:
                _timerB.Control = value;
                _timerB.Running = (value & 0x01) != 0;
                if ((value & 0x10) != 0)
                    _timerB.Counter = _timerB.Latch;
                _timerB.Divider = _timerB.Latch > 0 ? _timerB.Latch : 0x10000;
                break;
            case 0x0B: _todAlarmHours = value; break;
            case 0x0A: _todAlarmMinutes = value; break;
            case 0x09: _todAlarmSeconds = value; break;
            case 0x08: _todAlarmTenths = value; break;
                
            // Ports
            case 0x00: _portA = value; break;
            case 0x02: _portADir = value; break;
            case 0x01: _portB = value; break;
            case 0x03: _portBDir = value; break;
            
            // Interrupt mask
            case 0x0D:
                if ((value & 0x80) != 0)
                    _interruptMask |= (byte)(value & 0x7F);
                else
                    _interruptMask &= (byte)(~value & 0x7F);
                _interruptFlags &= (byte)(~value & 0x7F);
                break;
        }
    }

    public bool HandlesAddress(ushort address) => address >= 0xDC00 && address < 0xDE00;
}
