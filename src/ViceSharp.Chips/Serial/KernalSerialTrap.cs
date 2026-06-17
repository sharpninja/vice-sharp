using ViceSharp.Abstractions;
using ViceSharp.Chips.Cpu;

namespace ViceSharp.Chips.Serial;

/// <summary>
/// VICE-faithful KERNAL serial-bus traps for the C64 (the "virtual device
/// traps" used when True Drive is OFF). It is a direct port of
/// native/vice/vice/src/serial/serial-trap.c plus the C64 trap table in
/// src/c64/c64.c: the five low-level KERNAL serial primitives (LISTEN, talk
/// secondary, send byte, receive byte, "serial ready") are intercepted at their
/// ROM entry points and serviced by a host-side <see cref="VirtualDriveServer"/>
/// instead of bit-banging the IEC lines. This makes LOAD"$",8 / LOAD"*",8,1 /
/// LOAD"NAME",8 work without the real serial protocol, exactly as VICE does.
///
/// The trap is installed unconditionally on the simulated C64 CPU but, like
/// VICE's <c>device_uses_serial_traps()</c>, declines (leaves the instruction
/// to execute unchanged) unless the addressed device 8-11 has a virtual disk
/// inserted. With no disk - including every native cycle-parity rig and the
/// true-drive host C64 (whose disk lives in the emulated 1541, not the simulated
/// drive) - behaviour is byte-for-byte identical to an untrapped CPU.
/// </summary>
public sealed class KernalSerialTrap
{
    // Zero-page locations (C64/VIC20/C128 KERNAL).
    private const ushort Bsour = 0x95;       // buffered character for the serial bus
    private const ushort StRegister = 0x90;  // KERNAL status byte (ST)
    private const ushort TmpIn = 0xA4;        // serial input temp (C64: serial_trap_init(0xa4))
    private const ushort ResumeAddress = 0xEDAB; // common KERNAL serial routine tail

    // Command nibbles on the bus under attention.
    private const byte Listen = 0x20;
    private const byte Talk = 0x40;
    private const byte Secondary = 0x60;
    private const byte Close = 0xE0;
    private const byte Open = 0xF0;
    private const byte Unlisten = 0x3F;
    private const byte Untalk = 0x5F;
    private const byte DeviceMask = 0x0F;

    // 6502 processor-status flag bits.
    private const byte FlagCarry = 0x01;
    private const byte FlagZero = 0x02;
    private const byte FlagInterrupt = 0x04;
    private const byte FlagNegative = 0x80;

    private readonly Mos6502 _cpu;
    private readonly IBus _bus;
    private readonly VirtualDriveServer _server;
    private readonly TrapEntry[] _traps;
    private readonly ushort _minAddress;
    private readonly ushort _maxAddress;

    private byte _trapDevice;
    private byte _trapSecondary;
    private int _activeDevice = -1;

    private readonly record struct TrapEntry(ushort Address, byte Check0, byte Check1, byte Check2, Func<bool> Handler);

    public KernalSerialTrap(Mos6502 cpu, IBus bus, VirtualDriveServer server)
    {
        ArgumentNullException.ThrowIfNull(cpu);
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(server);

        _cpu = cpu;
        _bus = bus;
        _server = server;

        // c64_serial_traps[] from native/vice/vice/src/c64/c64.c. Check bytes are
        // the original opcodes at each address in the 901227-03 KERNAL.
        _traps = new[]
        {
            new TrapEntry(0xED24, 0x20, 0x97, 0xEE, Attention),     // SerialListen
            new TrapEntry(0xED37, 0x20, 0x8E, 0xEE, Attention),     // SerialSaListen
            new TrapEntry(0xED41, 0x20, 0x97, 0xEE, Send),          // SerialSendByte
            new TrapEntry(0xEE14, 0xA9, 0x00, 0x85, Receive),       // SerialReceiveByte
            new TrapEntry(0xEEA9, 0xAD, 0x00, 0xDD, Ready),         // SerialReady
        };

        _minAddress = ushort.MaxValue;
        _maxAddress = ushort.MinValue;
        foreach (var trap in _traps)
        {
            if (trap.Address < _minAddress) _minAddress = trap.Address;
            if (trap.Address > _maxAddress) _maxAddress = trap.Address;
        }
    }

    /// <summary>Reset all trap and virtual-drive state (called on machine reset).</summary>
    public void Reset()
    {
        _trapDevice = 0;
        _trapSecondary = 0;
        _activeDevice = -1;
        _server.Reset();
    }

    /// <summary>
    /// CPU instruction-boundary hook. Invoked with the address about to be
    /// fetched. Returns true when a serial routine was serviced (the handler has
    /// already set PC to the KERNAL resume point and the trapped instruction must
    /// be skipped), false to let the CPU execute the original instruction.
    /// </summary>
    public bool TryHandle(ushort pc)
    {
        if (pc < _minAddress || pc > _maxAddress)
            return false;

        for (var i = 0; i < _traps.Length; i++)
        {
            ref readonly var trap = ref _traps[i];
            if (trap.Address != pc)
                continue;

            // VICE check[] guard: only fire if the KERNAL ROM is banked in and
            // matches the expected opcode bytes. Peek avoids any I/O side effects.
            if (_bus.Peek(pc) != trap.Check0
                || _bus.Peek((ushort)(pc + 1)) != trap.Check1
                || _bus.Peek((ushort)(pc + 2)) != trap.Check2)
            {
                return false;
            }

            if (!trap.Handler())
                return false;

            _cpu.PC = ResumeAddress;
            return true;
        }

        return false;
    }

    // serial-trap.c device_uses_serial_traps(): only disk devices 8-11 with a
    // disk inserted are served (printers are not emulated here).
    private bool DeviceUsesTraps(int device)
    {
        if (device < 8 || device > 11)
            return false;
        return _server.HasDisk(device);
    }

    // serial_trap_attention()
    private bool Attention()
    {
        var iecdata = _bus.Read(Bsour);

        if (iecdata is Unlisten or Untalk)
        {
            // transfer ends
        }
        else if ((iecdata & 0xF0) == Open || (iecdata & 0xF0) == Close)
        {
            // open/close for the current ActiveDevice
        }
        else if ((iecdata & 0xF0) == Listen || (iecdata & 0xF0) == Talk)
        {
            _activeDevice = iecdata & DeviceMask;
        }

        if (!DeviceUsesTraps(_activeDevice))
        {
            if (iecdata is Unlisten or Untalk)
                _activeDevice = 0;
            return false;
        }

        if (iecdata == Unlisten)
        {
            SetStatus(_server.Unlisten(_trapDevice, _trapSecondary));
            _activeDevice = 0;
        }
        else if (iecdata == Untalk)
        {
            _server.Untalk(_trapDevice, _trapSecondary);
            _activeDevice = 0;
        }
        else
        {
            switch (iecdata & 0xF0)
            {
                case Listen:
                case Talk:
                    _trapDevice = iecdata;
                    _trapSecondary = 0;
                    break;
                case Secondary:
                    SendListenTalkSecondary(iecdata);
                    break;
                case Close:
                    _trapSecondary = iecdata;
                    SetStatus(_server.Close(_trapDevice, _trapSecondary));
                    break;
                case Open:
                    _trapSecondary = iecdata;
                    _server.Open(_trapDevice, _trapSecondary);
                    break;
            }
        }

        if (!_server.HasDisk(_trapDevice))
            SetStatus(0x80);

        ClearCarry();
        ClearInterrupt();
        return true;
    }

    // send_listen_talk_secondary()
    private void SendListenTalkSecondary(byte b)
    {
        _trapSecondary = b;
        switch (_trapDevice & 0xF0)
        {
            case Listen:
            case Talk:
                SetStatus(_server.ListenTalk(_trapDevice, _trapSecondary));
                break;
        }
    }

    // serial_trap_send()
    private bool Send()
    {
        if (!DeviceUsesTraps(_activeDevice))
            return false;

        if (_trapSecondary == 0)
            SendListenTalkSecondary(Secondary + 0);

        var iecdata = _bus.Read(Bsour);
        SetStatus(_server.Write(_trapDevice, _trapSecondary, iecdata));

        ClearCarry();
        ClearInterrupt();
        return true;
    }

    // serial_trap_receive()
    private bool Receive()
    {
        if (!DeviceUsesTraps(_activeDevice))
            return false;

        if (_trapSecondary == 0)
            SendListenTalkSecondary(Secondary + 0);

        var (data, status) = _server.Read(_trapDevice, _trapSecondary);
        SetStatus(status);

        _bus.Write(TmpIn, data);

        // Set registers exactly as the KERNAL ACPTR routine would.
        _cpu.A = data;
        SetSign((data & 0x80) != 0);
        SetZero(data == 0);
        ClearCarry();
        ClearInterrupt();
        return true;
    }

    // serial_trap_ready()
    private bool Ready()
    {
        if (!DeviceUsesTraps(_activeDevice))
            return false;

        _cpu.A = 1;
        SetSign(false);
        SetZero(false);
        ClearInterrupt();
        return true;
    }

    private void SetStatus(byte status)
        => _bus.Write(StRegister, (byte)(_bus.Read(StRegister) | status));

    private void ClearCarry() => _cpu.P &= unchecked((byte)~FlagCarry);

    private void ClearInterrupt() => _cpu.P &= unchecked((byte)~FlagInterrupt);

    private void SetSign(bool on)
        => _cpu.P = on ? (byte)(_cpu.P | FlagNegative) : (byte)(_cpu.P & unchecked((byte)~FlagNegative));

    private void SetZero(bool on)
        => _cpu.P = on ? (byte)(_cpu.P | FlagZero) : (byte)(_cpu.P & unchecked((byte)~FlagZero));
}
