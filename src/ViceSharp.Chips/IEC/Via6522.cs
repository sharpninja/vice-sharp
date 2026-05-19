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
    private const byte IfrCb1 = 0x10;
    private const byte IfrCb2 = 0x08;
    private const byte IfrSr = 0x04;
    private const byte IfrCa1 = 0x02;
    private const byte IfrCa2 = 0x01;
    private const byte IfrAny = 0x80;

    private const byte PcrCa1ActiveEdgeMask = 0x01; // PCR bit 0: 1 = rising, 0 = falling
    private const byte PcrCa2ModeMask = 0x0E;       // PCR bits 3..1 select CA2 mode
    private const byte PcrCb1ActiveEdgeMask = 0x10; // PCR bit 4: 1 = rising, 0 = falling
    private const byte PcrCb2ModeMask = 0xE0;       // PCR bits 7..5 select CB2 mode

    // CA2 mode encodings (PCR bits 3..1 packed into 0..7). Match the CB2
    // encoding bit-for-bit; only the four landed modes are named here so
    // unused constants do not raise warnings.
    private const int Ca2ModeHandshakeOut = 0b100;
    private const int Ca2ModePulseOut = 0b101;
    private const int Ca2ModeManualLow = 0b110;
    private const int Ca2ModeManualHigh = 0b111;

    // CB2 mode encodings (PCR bits 7..5 packed into 0..7)
    private const int Cb2ModeInputNegEdge = 0b000;
    private const int Cb2ModeInputNegIndependent = 0b001;
    private const int Cb2ModeInputPosEdge = 0b010;
    private const int Cb2ModeInputPosIndependent = 0b011;
    private const int Cb2ModeHandshakeOut = 0b100;
    private const int Cb2ModePulseOut = 0b101;
    private const int Cb2ModeManualLow = 0b110;
    private const int Cb2ModeManualHigh = 0b111;

    private const byte AcrT1ContinuousMask = 0x40;
    private const byte AcrT1Pb7Mask = 0x80; // ACR bit 7: route T1 onto PB7
    private const byte AcrT2PulseCountMask = 0x20; // ACR bit 5: 0 = phi2 countdown, 1 = count PB6 negative edges
    private const byte AcrShiftModeMask = 0x1C; // bits 4..2 select shift mode
    private const byte Pb7Mask = 0x80;

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

    // Timer 1 PB7 output state. When ACR bit 7 = 1 the T1 underflow event is
    // routed onto PB7: ACR bit 6 = 1 (continuous) toggles _pb7TimerToggle on
    // every underflow (free-run square wave); ACR bit 6 = 0 (one-shot) drives
    // _pb7TimerToggle low on T1 start and high on the first underflow. The
    // bit overrides ORB bit 7 in port-B reads while DDRB bit 7 = 1.
    private bool _pb7TimerToggle;

    // CB2 pin state. PCR bits 7..5 select the output mode (manual low/high,
    // handshake, pulse) or one of four input edge-detect modes that latch
    // IFR bit 3 via TriggerCb2. Defaults to high so manual-high and unused
    // configurations idle at the inactive level.
    private bool _cb2State = true;

    // CA2 pin state. PCR bits 3..1 select the output mode (manual low/high,
    // handshake, pulse) or one of four input edge-detect modes that latch
    // IFR bit 0 via TriggerCa2. Defaults to high so manual-high and unused
    // configurations idle at the inactive level (symmetric with CB2).
    private bool _ca2State = true;

    // Remaining phi2 cycles before CA2/CB2 pulse-output (PCR mode 101) lines
    // return to the idle high level. The mode drives the corresponding pin
    // low for exactly one cycle on its trigger event (PA read for CA2, ORB
    // write for CB2). A non-zero counter is decremented in Tick(); when it
    // hits zero the line is restored to true.
    private int _ca2PulseRemaining;
    private int _cb2PulseRemaining;

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
        _pb7TimerToggle = false;
        _cb2State = true;
        _ca2State = true;
        _ca2PulseRemaining = 0;
        _cb2PulseRemaining = 0;
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
                    return ComposePortBRead(input);
                }
            case 0x01: // ORA / IRA - clears CA1/CA2 in IFR
                _ifr &= unchecked((byte)~0x03);
                RefreshIrq();
                // Handshake output mode (PCR bits 3..1 = 100): an ORA / IRA
                // read drives CA2 low; the line stays low until the next CA1
                // active edge restores it. Symmetric with CB2's ORB-write
                // handshake.
                if (GetCa2Mode() == Ca2ModeHandshakeOut)
                    _ca2State = false;
                // Pulse output mode (PCR bits 3..1 = 101): an ORA / IRA read
                // drives CA2 low for exactly one phi2 cycle. The pulse
                // counter is decremented in Tick(); when it hits zero the
                // line is automatically restored high.
                else if (GetCa2Mode() == Ca2ModePulseOut)
                {
                    _ca2State = false;
                    _ca2PulseRemaining = 1;
                }
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
                // Handshake output mode (PCR bits 7..5 = 100): an ORB write
                // drives CB2 low; the line stays low until the next CB1
                // active edge restores it. This is the canonical 6522 write-
                // handshake protocol used by the IEC fast-serial drivers.
                if (GetCb2Mode() == Cb2ModeHandshakeOut)
                    _cb2State = false;
                // Pulse output mode (PCR bits 7..5 = 101): an ORB write
                // drives CB2 low for exactly one phi2 cycle. The pulse
                // counter is decremented in Tick(); when it hits zero the
                // line is automatically restored high.
                else if (GetCb2Mode() == Cb2ModePulseOut)
                {
                    _cb2State = false;
                    _cb2PulseRemaining = 1;
                }
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
                // PB7 timer mode init: ACR bit 7 = 1 + bit 6 = 0 (one-shot)
                // drives PB7 low at T1 start; the bit returns high on the
                // first underflow. Continuous mode leaves the toggle state
                // alone (it free-runs from prior underflows).
                if ((_acr & AcrT1Pb7Mask) != 0 && (_acr & AcrT1ContinuousMask) == 0)
                    _pb7TimerToggle = false;
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
            case 0x0C:
                _pcr = value;
                // Apply CB2 output modes immediately on PCR change. Manual
                // low/high latch the pin to a static level; handshake and
                // pulse output modes idle the pin high until the next ORB
                // write (or external CB1 stimulus) drives the handshake.
                // Input modes leave _cb2State untouched so any prior level
                // persists until a TriggerCb2 edge arrives.
                switch (GetCb2Mode())
                {
                    case Cb2ModeManualLow:
                        _cb2State = false;
                        break;
                    case Cb2ModeManualHigh:
                        _cb2State = true;
                        break;
                    case Cb2ModeHandshakeOut:
                    case Cb2ModePulseOut:
                        _cb2State = true; // idle high until ORB write triggers the handshake
                        break;
                }
                // Apply CA2 output modes on the same PCR write (bits 3..1).
                // Mirrors the CB2 handling: manual low/high latch directly,
                // handshake / pulse idle high until an ORA read drives the
                // handshake, and input modes leave _ca2State alone.
                switch (GetCa2Mode())
                {
                    case Ca2ModeManualLow:
                        _ca2State = false;
                        break;
                    case Ca2ModeManualHigh:
                        _ca2State = true;
                        break;
                    case Ca2ModeHandshakeOut:
                    case Ca2ModePulseOut:
                        _ca2State = true; // idle high until ORA read triggers the handshake
                        break;
                }
                break;
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
            0x00 => ComposePortBRead(PortBInput?.Invoke() ?? 0xFF),
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
        // CA2/CB2 pulse-output mode 101: each non-zero counter represents one
        // remaining phi2 cycle that the corresponding line must stay low. The
        // trigger (ORA read for CA2, ORB write for CB2) sets the counter to 1
        // and the state to false; this Tick restores the line to high when
        // the counter reaches zero. Pulse mode without a trigger leaves the
        // counter at zero, so the line idles high - matching the 6522 spec.
        if (_ca2PulseRemaining > 0)
        {
            _ca2PulseRemaining--;
            if (_ca2PulseRemaining == 0)
                _ca2State = true;
        }
        if (_cb2PulseRemaining > 0)
        {
            _cb2PulseRemaining--;
            if (_cb2PulseRemaining == 0)
                _cb2State = true;
        }

        if (_t1Running)
        {
            if (_t1Counter == 0)
            {
                _ifr |= IfrTimer1;
                RefreshIrq();
                // T1 underflow with PB7 routing enabled: continuous mode
                // (ACR bit 6 = 1) inverts PB7 on every underflow; one-shot
                // mode (ACR bit 6 = 0) drives PB7 high on the first (and
                // only) underflow.
                if ((_acr & AcrT1Pb7Mask) != 0)
                {
                    if ((_acr & AcrT1ContinuousMask) != 0)
                        _pb7TimerToggle = !_pb7TimerToggle;
                    else
                        _pb7TimerToggle = true;
                }
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

        // T2 phi2 countdown is gated by ACR bit 5: 0 = phi2 ticks decrement T2,
        // 1 = pulse-count mode counting negative edges on PB6 (driven by
        // TriggerPb6). In pulse-count mode this Tick path is bypassed entirely
        // so phi2 cannot advance the counter. T2 is one-shot: after underflow
        // latches IFR bit 5 the running flag drops, matching the spec that T2
        // does not auto-reload.
        if (_t2Running && (_acr & AcrT2PulseCountMask) == 0)
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
    /// Simulates an edge transition on the PB6 input pin. In T2 pulse-count
    /// mode (ACR bit 5 = 1) each negative (high-to-low) transition decrements
    /// the T2 counter; underflow latches IFR bit 5 and (gated by IER bit 5)
    /// asserts the IRQ output. Positive transitions and calls made while
    /// ACR bit 5 = 0 (phi2 countdown mode) are ignored: the 6522 spec gates
    /// PB6 counting exclusively on falling edges in pulse-count mode.
    /// </summary>
    /// <param name="rising">True for a low-to-high transition; false for high-to-low.</param>
    public void TriggerPb6(bool rising)
    {
        // Only negative edges count, and only while pulse-count mode is armed.
        if (rising || (_acr & AcrT2PulseCountMask) == 0)
            return;
        if (_t2Counter == 0)
        {
            _t2Counter = 0xFFFF;
            _ifr |= IfrTimer2;
            RefreshIrq();
        }
        else
        {
            _t2Counter--;
        }
    }

    /// <summary>
    /// Simulates an edge transition on the CA1 input pin. The configured
    /// active edge (PCR bit 0: 1 = rising, 0 = falling) gates whether the
    /// transition latches IFR bit 1; IER bit 1 then gates the IRQ output.
    /// When CA2 is in handshake output mode (PCR bits 3..1 = 100) an active
    /// CA1 edge also restores CA2 to high, completing the read handshake.
    /// Used by the 1541 to deliver BYTE-READY from the disk controller.
    /// </summary>
    /// <param name="rising">True for a low-to-high transition; false for high-to-low.</param>
    public void TriggerCa1(bool rising)
    {
        var activeEdgeIsRising = (_pcr & PcrCa1ActiveEdgeMask) != 0;
        if (rising == activeEdgeIsRising)
        {
            _ifr |= IfrCa1;
            RefreshIrq();
            if (GetCa2Mode() == Ca2ModeHandshakeOut)
                _ca2State = true;
        }
    }

    /// <summary>
    /// Simulates an edge transition on the CB1 input pin. The configured
    /// active edge (PCR bit 4: 1 = rising, 0 = falling) gates whether the
    /// transition latches IFR bit 4; IER bit 4 then gates the IRQ output.
    /// When CB2 is in handshake output mode (PCR bits 7..5 = 100) an active
    /// CB1 edge also restores CB2 to high, completing the write handshake.
    /// </summary>
    /// <param name="rising">True for a low-to-high transition; false for high-to-low.</param>
    public void TriggerCb1(bool rising)
    {
        var activeEdgeIsRising = (_pcr & PcrCb1ActiveEdgeMask) != 0;
        if (rising == activeEdgeIsRising)
        {
            _ifr |= IfrCb1;
            RefreshIrq();
            if (GetCb2Mode() == Cb2ModeHandshakeOut)
                _cb2State = true;
        }
    }

    /// <summary>
    /// Simulates an edge transition on the CB2 input pin. Only valid for CB2
    /// input modes (PCR bits 7..5 in 000..011). The configured edge direction
    /// (PCR bit 5: 1 = rising, 0 = falling) gates whether the transition
    /// latches IFR bit 3; IER bit 3 then gates the IRQ output. Output modes
    /// ignore the call (the pin is driven by the VIA in those cases).
    /// </summary>
    /// <param name="rising">True for a low-to-high transition; false for high-to-low.</param>
    public void TriggerCb2(bool rising)
    {
        var mode = GetCb2Mode();
        // Output modes (100..111) ignore externally-driven edges.
        if (mode >= Cb2ModeHandshakeOut)
            return;
        // Edge direction for input modes is encoded in PCR bit 6 (mode bit 1):
        // 0 = negative/falling active (modes 000/001), 1 = positive/rising
        // active (modes 010/011). Mode bit 0 (PCR bit 5) selects
        // active-vs-independent latching, which we treat uniformly here.
        var activeEdgeIsRising = (mode & 0b010) != 0;
        if (rising == activeEdgeIsRising)
        {
            _ifr |= IfrCb2;
            RefreshIrq();
        }
    }

    /// <summary>
    /// Current CB2 pin level. In output modes this reflects the VIA-driven
    /// state (manual low/high, handshake idle/asserted, pulse). In input
    /// modes it is unaffected by TriggerCb2 (which only latches IFR) and
    /// retains whatever level was last applied by a prior output mode.
    /// </summary>
    public bool Cb2State => _cb2State;

    /// <summary>
    /// Simulates an edge transition on the CA2 input pin. Only valid for CA2
    /// input modes (PCR bits 3..1 in 000..011). The configured edge direction
    /// (PCR bit 2: 1 = rising, 0 = falling) gates whether the transition
    /// latches IFR bit 0; IER bit 0 then gates the IRQ output. Output modes
    /// ignore the call (the pin is driven by the VIA in those cases).
    /// </summary>
    /// <param name="rising">True for a low-to-high transition; false for high-to-low.</param>
    public void TriggerCa2(bool rising)
    {
        var mode = GetCa2Mode();
        // Output modes (100..111) ignore externally-driven edges.
        if (mode >= Ca2ModeHandshakeOut)
            return;
        // Edge direction for input modes is encoded in PCR bit 2 (mode bit 1):
        // 0 = negative/falling active (modes 000/001), 1 = positive/rising
        // active (modes 010/011). Mode bit 0 (PCR bit 1) selects
        // active-vs-independent latching, which we treat uniformly here.
        var activeEdgeIsRising = (mode & 0b010) != 0;
        if (rising == activeEdgeIsRising)
        {
            _ifr |= IfrCa2;
            RefreshIrq();
        }
    }

    /// <summary>
    /// Current CA2 pin level. In output modes this reflects the VIA-driven
    /// state (manual low/high, handshake idle/asserted, pulse). In input
    /// modes it is unaffected by TriggerCa2 (which only latches IFR) and
    /// retains whatever level was last applied by a prior output mode.
    /// </summary>
    public bool Ca2State => _ca2State;

    /// <summary>
    /// Returns the active CB2 mode from PCR bits 7..5, packed as values 0..7.
    /// </summary>
    private int GetCb2Mode() => (_pcr & PcrCb2ModeMask) >> 5;

    /// <summary>
    /// Returns the active CA2 mode from PCR bits 3..1, packed as values 0..7.
    /// </summary>
    private int GetCa2Mode() => (_pcr & PcrCa2ModeMask) >> 1;

    /// <summary>
    /// Returns the active ACR shift-register mode (ACR bits 4..2 packed into
    /// values 0..7).
    /// </summary>
    private int GetShiftMode() => (_acr & AcrShiftModeMask) >> 2;

    /// <summary>
    /// Composes the port-B read value from output latch, DDR, and external
    /// input pins. When ACR bit 7 = 1 routes T1 onto PB7 and DDRB bit 7 = 1
    /// selects PB7 as an output, the timer-driven _pb7TimerToggle replaces
    /// the ORB bit 7 contribution; other bits follow the standard rule
    /// (output bits from ORB, input bits from the external input function).
    /// </summary>
    private byte ComposePortBRead(byte input)
    {
        var outputBits = (byte)(_portBOutput & _ddrb);
        var inputBits = (byte)(input & (byte)~_ddrb);
        var combined = (byte)(outputBits | inputBits);
        if ((_acr & AcrT1Pb7Mask) != 0 && (_ddrb & Pb7Mask) != 0)
        {
            combined = (byte)(combined & unchecked((byte)~Pb7Mask));
            if (_pb7TimerToggle)
                combined |= Pb7Mask;
        }
        return combined;
    }

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
