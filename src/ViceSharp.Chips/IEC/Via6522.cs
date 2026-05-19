using ViceSharp.Abstractions;

namespace ViceSharp.Chips.IEC;

/// <summary>
/// MOS 6522 Versatile Interface Adapter (VIA). Bus-addressable, clocked,
/// IRQ-emitting. Supports the 16-register layout (Port A/B, DDR A/B, two
/// 16-bit timers, shift register, ACR, PCR, IFR, IER).
///
/// Used by the 1541 floppy drive: VIA1 ($1800-$1BFF) handles IEC bus (PB)
/// + LED; VIA2 ($1C00-$1FFF) handles head/motor/byte-ready. The drive's
/// 6502 reads/writes via this address space; timer underflows and CB1/CA1
/// transitions assert the drive IRQ line through IInterruptLine.
///
/// Implementation scope: register R/W with mirroring, one-shot + continuous
/// Timer 1 + one-shot Timer 2 decrement, IFR/IER + IRQ assertion, port
/// input/output callbacks. Shift register modes, CB1/CB2 handshake, and
/// latched-input quirks land in later slices.
/// </summary>
public sealed class Via6522 : IClockedDevice, IAddressSpace, IInterruptSource
{
    private const ushort DefaultBaseAddress = 0x1800;
    private const ushort DefaultMirrorSize = 0x0400;

    private const byte IfrTimer1 = 0x40;
    private const byte IfrTimer2 = 0x20;
    private const byte IfrSr = 0x04;
    private const byte IfrAny = 0x80;

    private const byte AcrT1ContinuousMask = 0x40;
    private const byte AcrShiftModeMask = 0x1C; // bits 4..2 select shift mode

    private readonly IBus _bus;
    private readonly IInterruptLine _irqLine;

    private byte _portAOutput;
    private byte _portBOutput;
    private byte _ddra;
    private byte _ddrb;

    private ushort _t1Counter;
    private ushort _t1Latch;
    private ushort _t2Counter;
    private bool _t1Running;
    private bool _t2Running;

    private byte _sr;
    private byte _acr;
    private byte _pcr;
    private byte _ifr;
    private byte _ier;
    private bool _irqAsserted;

    // Shift register state. _srShiftCount tracks bits shifted in the current
    // 8-bit transfer; the SR IFR latches when it reaches 8. _srShifting is
    // armed by SR writes (out modes) and by entering an in mode; it auto-
    // disarms on IFR latch and re-arms on the next SR write / mode entry.
    private int _srShiftCount;
    private bool _srShifting;

    public Via6522(IBus bus, IInterruptLine irqLine)
    {
        _bus = bus;
        _irqLine = irqLine;
        Id = new DeviceId(0x1200);
        SourceId = Id;
        Name = "MOS 6522 VIA";
    }

    /// <inheritdoc />
    public DeviceId Id { get; init; }

    /// <inheritdoc />
    public string Name { get; init; }

    /// <inheritdoc />
    public DeviceId SourceId { get; init; }

    /// <summary>Base address; defaults to $1800 (1541 VIA1).</summary>
    public ushort BaseAddress { get; init; } = DefaultBaseAddress;

    /// <summary>Mirrored window size; defaults to $0400 (1541 VIA mirror region).</summary>
    public ushort Size { get; init; } = DefaultMirrorSize;

    /// <inheritdoc />
    public uint ClockDivisor => 1;

    /// <inheritdoc />
    public ClockPhase Phase => ClockPhase.Phi2;

    /// <inheritdoc />
    public IReadOnlyList<IInterruptLine> ConnectedLines => new[] { _irqLine };

    /// <summary>Port A external input function (e.g. IEC DATA in).</summary>
    public Func<byte>? PortAInput { get; set; }

    /// <summary>Port B external input function (e.g. IEC ATN/CLK in).</summary>
    public Func<byte>? PortBInput { get; set; }

    /// <summary>Fires after Port A output bits change (LED, head step lines).</summary>
    public Action<byte>? PortAOutputChanged { get; set; }

    /// <summary>Fires after Port B output bits change (IEC bus pulls, motor).</summary>
    public Action<byte>? PortBOutputChanged { get; set; }

    /// <inheritdoc />
    public void Reset()
    {
        _portAOutput = 0;
        _portBOutput = 0;
        _ddra = 0;
        _ddrb = 0;
        _t1Counter = 0xFFFF;
        _t1Latch = 0xFFFF;
        _t2Counter = 0xFFFF;
        _t1Running = false;
        _t2Running = false;
        _sr = 0;
        _acr = 0;
        _pcr = 0;
        _ifr = 0;
        _ier = 0;
        _srShiftCount = 0;
        _srShifting = false;
        if (_irqAsserted)
        {
            _irqLine.Release(this);
            _irqAsserted = false;
        }
    }

    /// <inheritdoc />
    public bool HandlesAddress(ushort address)
        => address >= BaseAddress && address < (ushort)(BaseAddress + Size);

    /// <inheritdoc />
    public byte Read(ushort address)
    {
        var reg = (byte)((address - BaseAddress) & 0x0F);
        switch (reg)
        {
            case 0x00: // ORB / IRB
                {
                    var input = PortBInput?.Invoke() ?? 0xFF;
                    return (byte)((_portBOutput & _ddrb) | (input & (byte)~_ddrb));
                }
            case 0x01: // ORA / IRA - clears CA1/CA2 in IFR
                _ifr &= unchecked((byte)~0x03);
                RefreshIrq();
                {
                    var input = PortAInput?.Invoke() ?? 0xFF;
                    return (byte)((_portAOutput & _ddra) | (input & (byte)~_ddra));
                }
            case 0x02: return _ddrb;
            case 0x03: return _ddra;
            case 0x04: // T1C-L: read low + clear T1 flag
                _ifr &= unchecked((byte)~IfrTimer1);
                RefreshIrq();
                return (byte)(_t1Counter & 0xFF);
            case 0x05: return (byte)(_t1Counter >> 8);
            case 0x06: return (byte)(_t1Latch & 0xFF);
            case 0x07: return (byte)(_t1Latch >> 8);
            case 0x08: // T2C-L: read low + clear T2 flag
                _ifr &= unchecked((byte)~IfrTimer2);
                RefreshIrq();
                return (byte)(_t2Counter & 0xFF);
            case 0x09: return (byte)(_t2Counter >> 8);
            case 0x0A:
                // Reading SR clears the SR IFR flag and rearms an 8-bit shift
                // transfer for the active mode (common 6522 idiom for chained
                // shift-in operations).
                _ifr &= unchecked((byte)~IfrSr);
                RefreshIrq();
                _srShiftCount = 0;
                _srShifting = GetShiftMode() != 0;
                return _sr;
            case 0x0B: return _acr;
            case 0x0C: return _pcr;
            case 0x0D: return _ifr;
            case 0x0E: return (byte)(_ier | 0x80);
            case 0x0F: // ORA no-handshake
                {
                    var input = PortAInput?.Invoke() ?? 0xFF;
                    return (byte)((_portAOutput & _ddra) | (input & (byte)~_ddra));
                }
            default: return 0;
        }
    }

    /// <inheritdoc />
    public void Write(ushort address, byte value)
    {
        var reg = (byte)((address - BaseAddress) & 0x0F);
        switch (reg)
        {
            case 0x00:
                _portBOutput = value;
                PortBOutputChanged?.Invoke((byte)(_portBOutput & _ddrb));
                break;
            case 0x01:
                _ifr &= unchecked((byte)~0x03);
                RefreshIrq();
                _portAOutput = value;
                PortAOutputChanged?.Invoke((byte)(_portAOutput & _ddra));
                break;
            case 0x02:
                _ddrb = value;
                PortBOutputChanged?.Invoke((byte)(_portBOutput & _ddrb));
                break;
            case 0x03:
                _ddra = value;
                PortAOutputChanged?.Invoke((byte)(_portAOutput & _ddra));
                break;
            case 0x04:
                _t1Latch = (ushort)((_t1Latch & 0xFF00) | value);
                break;
            case 0x05:
                _t1Latch = (ushort)((value << 8) | (_t1Latch & 0x00FF));
                _t1Counter = _t1Latch;
                _t1Running = true;
                _ifr &= unchecked((byte)~IfrTimer1);
                RefreshIrq();
                break;
            case 0x06:
                _t1Latch = (ushort)((_t1Latch & 0xFF00) | value);
                break;
            case 0x07:
                _t1Latch = (ushort)((value << 8) | (_t1Latch & 0x00FF));
                _ifr &= unchecked((byte)~IfrTimer1);
                RefreshIrq();
                break;
            case 0x08:
                _t2Counter = (ushort)((_t2Counter & 0xFF00) | value);
                break;
            case 0x09:
                _t2Counter = (ushort)((value << 8) | (_t2Counter & 0x00FF));
                _t2Running = true;
                _ifr &= unchecked((byte)~IfrTimer2);
                RefreshIrq();
                break;
            case 0x0A:
                _sr = value;
                // Arm a fresh 8-bit shift transfer. Active phi2 in/out modes
                // will start shifting on the next Tick; inert modes (000) just
                // hold the byte.
                _srShiftCount = 0;
                _srShifting = GetShiftMode() != 0;
                _ifr &= unchecked((byte)~IfrSr);
                RefreshIrq();
                break;
            case 0x0B:
                {
                    var prevMode = GetShiftMode();
                    _acr = value;
                    var newMode = GetShiftMode();
                    if (newMode != prevMode)
                    {
                        // Mode change rearms the shift state machine for the
                        // new direction (or disables it for mode 000).
                        _srShiftCount = 0;
                        _srShifting = newMode != 0;
                    }
                }
                break;
            case 0x0C: _pcr = value; break;
            case 0x0D:
                if ((value & IfrAny) == 0)
                {
                    _ifr &= (byte)~(value & 0x7F);
                    RefreshIrq();
                }
                break;
            case 0x0E:
                if ((value & IfrAny) != 0)
                    _ier |= (byte)(value & 0x7F);
                else
                    _ier &= (byte)~(value & 0x7F);
                RefreshIrq();
                break;
            case 0x0F:
                _portAOutput = value;
                PortAOutputChanged?.Invoke((byte)(_portAOutput & _ddra));
                break;
        }
    }

    /// <inheritdoc />
    public byte Peek(ushort address)
    {
        var reg = (byte)((address - BaseAddress) & 0x0F);
        return reg switch
        {
            0x00 => (byte)((_portBOutput & _ddrb) | ((PortBInput?.Invoke() ?? 0xFF) & (byte)~_ddrb)),
            0x01 => (byte)((_portAOutput & _ddra) | ((PortAInput?.Invoke() ?? 0xFF) & (byte)~_ddra)),
            0x02 => _ddrb,
            0x03 => _ddra,
            0x04 => (byte)(_t1Counter & 0xFF),
            0x05 => (byte)(_t1Counter >> 8),
            0x06 => (byte)(_t1Latch & 0xFF),
            0x07 => (byte)(_t1Latch >> 8),
            0x08 => (byte)(_t2Counter & 0xFF),
            0x09 => (byte)(_t2Counter >> 8),
            0x0A => _sr,
            0x0B => _acr,
            0x0C => _pcr,
            0x0D => _ifr,
            0x0E => (byte)(_ier | 0x80),
            0x0F => (byte)((_portAOutput & _ddra) | ((PortAInput?.Invoke() ?? 0xFF) & (byte)~_ddra)),
            _ => 0,
        };
    }

    /// <inheritdoc />
    public void Tick()
    {
        if (_t1Running)
        {
            if (_t1Counter == 0)
            {
                _ifr |= IfrTimer1;
                RefreshIrq();
                if ((_acr & AcrT1ContinuousMask) != 0)
                    _t1Counter = _t1Latch;
                else
                    _t1Running = false;
            }
            else
            {
                _t1Counter--;
            }
        }

        if (_t2Running)
        {
            if (_t2Counter == 0)
            {
                _ifr |= IfrTimer2;
                RefreshIrq();
                _t2Running = false;
            }
            else
            {
                _t2Counter--;
            }
        }

        TickShiftRegister();
    }

    /// <summary>
    /// Returns the active ACR shift-register mode (ACR bits 4..2 packed into
    /// values 0..7).
    /// </summary>
    private int GetShiftMode() => (_acr & AcrShiftModeMask) >> 2;

    /// <summary>
    /// Advances the shift register one phi2 cycle. Implemented modes:
    /// - 010 (shift in under phi2): clocks one bit from CB2 (defaults to 0
    ///   while CB2 plumbing is pending) into the LSB of SR.
    /// - 110 (shift out under phi2): rotates SR left, emitting MSB to CB2
    ///   (sink TBD); the bit also feeds back into the LSB so the byte
    ///   survives 8 shifts (matches NMOS 6522 wrap-around behaviour).
    /// After 8 shifts the SR IFR bit latches and shifting halts until SR is
    /// re-armed via $0A write/read or an ACR mode change.
    ///
    /// Modes 001, 011, 100, 101, 111 (T2- and CB1-clocked) are stubs: they
    /// require T2 underflow events and CB1 pin plumbing that are not yet
    /// implemented. They currently behave as if the shift clock never ticks,
    /// matching a powered-on chip with no external CB1 stimulus.
    /// </summary>
    private void TickShiftRegister()
    {
        if (!_srShifting)
        {
            return;
        }

        var mode = GetShiftMode();
        switch (mode)
        {
            case 0b010: // shift in under phi2
                {
                    // CB2 not yet plumbed - sample 0 as a safe default. Real
                    // CB2 input lands when the IEC fast-serial pins are wired.
                    var incoming = 0;
                    _sr = (byte)(((_sr << 1) | (incoming & 0x01)) & 0xFF);
                    AdvanceShiftCount();
                    break;
                }
            case 0b110: // shift out under phi2
                {
                    // Emit MSB to CB2 (sink TBD) and rotate it back into the
                    // LSB so the source byte survives an 8-bit transfer.
                    var msb = (byte)((_sr >> 7) & 0x01);
                    _sr = (byte)(((_sr << 1) | msb) & 0xFF);
                    AdvanceShiftCount();
                    break;
                }
            default:
                // Modes 001 / 011 / 100 / 101 / 111: T2- and CB1-clocked
                // transfers. Stubbed pending T2 underflow event hookup and
                // CB1 pin plumbing - see class summary for scope.
                break;
        }
    }

    private void AdvanceShiftCount()
    {
        _srShiftCount++;
        if (_srShiftCount >= 8)
        {
            _ifr |= IfrSr;
            _srShifting = false;
            _srShiftCount = 0;
            RefreshIrq();
        }
    }

    private void RefreshIrq()
    {
        var pending = (byte)(_ifr & _ier & 0x7F);
        var shouldAssert = pending != 0;
        if (shouldAssert)
            _ifr |= IfrAny;
        else
            _ifr &= unchecked((byte)~IfrAny);

        if (shouldAssert && !_irqAsserted)
        {
            _irqLine.Assert(this);
            _irqAsserted = true;
        }
        else if (!shouldAssert && _irqAsserted)
        {
            _irqLine.Release(this);
            _irqAsserted = false;
        }
    }
}
