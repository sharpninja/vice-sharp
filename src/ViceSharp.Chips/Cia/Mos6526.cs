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
        public int LoadDelay;
        public int CountDelay;
    }

    // I/O Ports
    private byte _portA;
    private byte _portB;
    private byte _portADir;
    private byte _portBDir;

    public Func<byte>? PortAInput { get; set; }
    public Func<byte>? PortBInput { get; set; }
    public byte PortAExternalInputMask { get; set; }
    public Action<byte>? PortAOutputChanged { get; set; }
    public Action<byte>? PortBOutputChanged { get; set; }
    
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
    private bool _irqAsserted;
    
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
        _todTenths = IncrementBcd(_todTenths, 0x09);
        if (_todTenths == 0x00)
        {
            _todSeconds = IncrementBcd(_todSeconds, 0x59);
            if (_todSeconds == 0x00)
            {
                _todMinutes = IncrementBcd(_todMinutes, 0x59);
                if (_todMinutes == 0x00)
                {
                    _todHours = IncrementBcd(_todHours, 0x23);
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
        var timerAUnderflowed = TickTimer(ref _timerA);
        if (timerAUnderflowed)
            UnderflowTimerA();
        
        if (_timerB.Running && TimerBCountsPhi2())
        {
            if (TickTimer(ref _timerB))
                UnderflowTimerB();
        }
        else if (_timerB.Running && timerAUnderflowed && TimerBCountsTimerAUnderflow())
        {
            if (TickTimer(ref _timerB))
                UnderflowTimerB();
        }
    }

    public void Reset()
    {
        Array.Clear(_registers);
        _timerA = CreateResetTimer();
        _timerB = CreateResetTimer();
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
        _irqAsserted = false;
        _irqLine.Release(this);
    }

    public byte Peek(ushort address) => Read(address);

    public byte Read(ushort address)
    {
        int register = (address - BaseAddress) & 0x0F;
        return register switch
        {
            0x00 => ReadPortValue(_portA, _portADir, PortAInput, PortAExternalInputMask),
            0x01 => ReadPortValue(_portB, _portBDir, PortBInput),
            0x02 => _portADir,
            0x03 => _portBDir,
            0x04 => (byte)_timerA.Counter,
            0x05 => (byte)(_timerA.Counter >> 8),
            0x06 => (byte)_timerB.Counter,
            0x07 => (byte)(_timerB.Counter >> 8),
            0x08 => _todTenths,
            0x09 => _todSeconds,
            0x0A => _todMinutes,
            0x0B => _todHours,
            0x0D => ReadInterruptControlRegister(),
            _ => _registers[register]
        };
    }

    public void Write(ushort address, byte value)
    {
        int register = (address - BaseAddress) & 0x0F;
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
                _timerA.Control = (byte)(value & 0xEF);
                _registers[register] = _timerA.Control;
                _timerA.Running = (value & 0x01) != 0;
                ScheduleControlPipeline(ref _timerA, value);
                break;
                
            // Timer B
            case 0x06: _timerB.Latch = (ushort)((_timerB.Latch & 0xFF00) | value); break;
            case 0x07:
                _timerB.Latch = (ushort)((_timerB.Latch & 0x00FF) | (value << 8));
                if ((_timerB.Control & 0x01) == 0)
                    _timerB.Counter = _timerB.Latch;
                break;
            case 0x0F:
                _timerB.Control = (byte)(value & 0xEF);
                _registers[register] = _timerB.Control;
                _timerB.Running = (value & 0x01) != 0;
                ScheduleControlPipeline(ref _timerB, value);
                break;
            case 0x0B: WriteTodOrAlarm(ref _todHours, ref _todAlarmHours, value); break;
            case 0x0A: WriteTodOrAlarm(ref _todMinutes, ref _todAlarmMinutes, value); break;
            case 0x09: WriteTodOrAlarm(ref _todSeconds, ref _todAlarmSeconds, value); break;
            case 0x08: WriteTodOrAlarm(ref _todTenths, ref _todAlarmTenths, value); break;
                
            // Ports
            case 0x00:
                _portA = value;
                PortAOutputChanged?.Invoke(value);
                break;
            case 0x02: _portADir = value; break;
            case 0x01:
                _portB = value;
                PortBOutputChanged?.Invoke(value);
                break;
            case 0x03: _portBDir = value; break;
            
            // Interrupt mask
            case 0x0D:
                if ((value & 0x80) != 0)
                    _interruptMask |= (byte)(value & 0x7F);
                else
                    _interruptMask &= (byte)(~value & 0x7F);
                RefreshIrqLine();
                break;
        }
    }

    public bool HandlesAddress(ushort address) => address >= BaseAddress && address < BaseAddress + 0x0100;

    private static byte ReadPortValue(byte outputLatch, byte dataDirection, Func<byte>? inputReader, byte externalInputMask = 0)
    {
        byte input = inputReader?.Invoke() ?? (byte)0xFF;
        var value = (byte)((outputLatch & dataDirection) | (input & ~dataDirection));
        return (byte)((value & ~externalInputMask) | (input & externalInputMask));
    }

    private static TimerState CreateResetTimer() => new()
    {
        Latch = 0xFFFF,
        Counter = 0xFFFF
    };

    private static void ScheduleControlPipeline(ref TimerState timer, byte control)
    {
        var forceLoad = (control & 0x10) != 0;
        if (forceLoad)
            timer.LoadDelay = 2;

        if (timer.Running)
            timer.CountDelay = forceLoad ? 3 : 2;
        else
            timer.CountDelay = 0;
    }

    private static bool TickTimer(ref TimerState timer)
    {
        if (timer.LoadDelay > 0)
        {
            timer.LoadDelay--;
            if (timer.LoadDelay == 0)
                timer.Counter = timer.Latch;
        }

        if (!timer.Running)
            return false;

        if (timer.CountDelay > 0)
        {
            timer.CountDelay--;
            return false;
        }

        if (timer.Counter != 0)
            timer.Counter--;

        return timer.Counter == 0;
    }

    private void UnderflowTimerA()
    {
        _timerA.Counter = _timerA.Latch;
        if ((_timerA.Control & 0x08) != 0)
        {
            _timerA.Running = false;
            _timerA.CountDelay = 0;
        }
        else
        {
            _timerA.CountDelay = 1;
        }

        SetInterruptFlag(0x01);
    }

    private void UnderflowTimerB()
    {
        _timerB.Counter = _timerB.Latch;
        if ((_timerB.Control & 0x08) != 0)
        {
            _timerB.Running = false;
            _timerB.CountDelay = 0;
        }
        else
        {
            _timerB.CountDelay = 1;
        }

        SetInterruptFlag(0x02);
    }

    private bool TimerBCountsPhi2() => (_timerB.Control & 0x60) == 0x00;

    private bool TimerBCountsTimerAUnderflow() => (_timerB.Control & 0x40) != 0;

    private void WriteTodOrAlarm(ref byte clockRegister, ref byte alarmRegister, byte value)
    {
        if ((_timerB.Control & 0x80) != 0)
        {
            alarmRegister = value;
        }
        else
        {
            clockRegister = value;
        }
    }

    private static byte IncrementBcd(byte value, byte max)
    {
        if (value >= max)
            return 0x00;

        var low = (value + 1) & 0x0F;
        var high = value & 0xF0;

        if (low <= 0x09)
            return (byte)(high | low);

        return (byte)(((high + 0x10) & 0xF0) | 0x00);
    }

    private void SetInterruptFlag(byte flag)
    {
        _interruptFlags |= flag;
        RefreshIrqLine();
    }

    private byte ReadInterruptControlRegister()
    {
        var result = (byte)(_interruptFlags | (((_interruptFlags & _interruptMask) != 0) ? 0x80 : 0x00));
        _interruptFlags = 0;
        RefreshIrqLine();
        return result;
    }

    private void RefreshIrqLine()
    {
        var shouldAssert = (_interruptFlags & _interruptMask) != 0;
        if (shouldAssert == _irqAsserted)
            return;

        _irqAsserted = shouldAssert;
        if (shouldAssert)
            _irqLine.Assert(this);
        else
            _irqLine.Release(this);
    }
}
