using ViceSharp.Abstractions;
using ViceSharp.Chips.Audio;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.Input;
using ViceSharp.Chips.Pla;
using ViceSharp.Chips.VicIi;

namespace ViceSharp.Core;

/// <summary>
/// CPU-visible C64 memory map with PLA-controlled ROM and I/O banking.
/// </summary>
internal sealed class C64MemoryMap : IAddressSpace
{
    private const ushort BasicStart = 0xA000;
    private const ushort KernalStart = 0xE000;
    private const ushort CharStart = 0xD000;
    private const ushort IoStart = 0xD000;
    private const ushort IoEnd = 0xDFFF;
    private const ushort ColorRamStart = 0xD800;
    private const ushort ColorRamEnd = 0xDBFF;

    private readonly byte[] _ram = new byte[0x10000];
    private readonly byte[] _colorRam = new byte[0x0400];
    private readonly byte[] _basicRom = new byte[0x2000];
    private readonly byte[] _kernalRom = new byte[0x2000];
    private readonly byte[] _charRom = new byte[0x1000];

    private readonly Mos6569 _vic;
    private readonly Sid6581 _sid;
    private readonly Mos6526 _cia1;
    private readonly Mos6526 _cia2;
    private readonly Mos906114 _pla;
    private readonly C64KeyboardMatrix _keyboard;
    private readonly C64JoystickPort _joystickPort2;

    public C64MemoryMap(
        Mos6569 vic,
        Sid6581 sid,
        Mos6526 cia1,
        Mos6526 cia2,
        Mos906114 pla)
    {
        _vic = vic;
        _sid = sid;
        _cia1 = cia1;
        _cia2 = cia2;
        _pla = pla;
        _keyboard = new C64KeyboardMatrix();
        _joystickPort2 = new C64JoystickPort();

        _cia1.PortAInput = ReadCia1PortA;
        _cia1.PortBInput = ReadCia1PortB;
        _cia1.PortAOutputChanged = value => _keyboard.SetRowMask(value);
        _cia1.PortBOutputChanged = value => _keyboard.SetColumnMask(value);
        _cia2.PortAOutputChanged = value => _vic.VicBank = 3 - (value & 0x03);

        _vic.VideoMemoryReader = ReadVideoMemory;

        Reset();
    }

    public DeviceId Id => new DeviceId(0x0101);
    public string Name => "C64 Memory Map";

    public void Reset()
    {
        Array.Clear(_ram);
        Array.Fill(_ram, (byte)0xFF, 0x0800, 0x10000 - 0x0800);
        Array.Fill(_ram, (byte)0x20, 0x0400, 0x0400);
        Array.Fill(_colorRam, (byte)0x0E);

        _ram[0xFFFC] = 0xE2;
        _ram[0xFFFD] = 0xFC;
        _ram[0x0000] = 0x2F;
        _ram[0x0001] = 0x37;

        _keyboard.Reset();
        _joystickPort2.Reset();
        _vic.VicBank = 3;
    }

    public void LoadBasicRom(ReadOnlySpan<byte> data)
    {
        if (data.Length != _basicRom.Length)
            throw new ArgumentException("Invalid BASIC ROM size.", nameof(data));

        data.CopyTo(_basicRom);
    }

    public void LoadKernalRom(ReadOnlySpan<byte> data)
    {
        if (data.Length != _kernalRom.Length)
            throw new ArgumentException("Invalid KERNAL ROM size.", nameof(data));

        data.CopyTo(_kernalRom);
    }

    public void LoadCharacterRom(ReadOnlySpan<byte> data)
    {
        if (data.Length != _charRom.Length)
            throw new ArgumentException("Invalid character ROM size.", nameof(data));

        data.CopyTo(_charRom);
    }

    public byte Read(ushort address)
    {
        if (address == 0x0001)
            return _pla.ControlRegister;

        if (address is >= BasicStart and < 0xC000 && _pla.BasicRomVisible)
            return _basicRom[address - BasicStart];

        if (address is >= KernalStart && _pla.KernalRomVisible)
            return _kernalRom[address - KernalStart];

        if (address is >= IoStart and <= IoEnd)
        {
            if (IsIoVisible)
                return ReadIo(address, peek: false);

            if (IsCpuCharRomVisible)
                return _charRom[address - CharStart];
        }

        return _ram[address];
    }

    public byte Peek(ushort address)
    {
        if (address == 0x0001)
            return _pla.ControlRegister;

        if (address is >= BasicStart and < 0xC000 && _pla.BasicRomVisible)
            return _basicRom[address - BasicStart];

        if (address is >= KernalStart && _pla.KernalRomVisible)
            return _kernalRom[address - KernalStart];

        if (address is >= IoStart and <= IoEnd)
        {
            if (IsIoVisible)
                return ReadIo(address, peek: true);

            if (IsCpuCharRomVisible)
                return _charRom[address - CharStart];
        }

        return _ram[address];
    }

    public void Write(ushort address, byte value)
    {
        if (address == 0x0001)
        {
            _ram[address] = value;
            _pla.Write(address, value);
            return;
        }

        if (address is >= IoStart and <= IoEnd && IsIoVisible)
        {
            WriteIo(address, value);
            return;
        }

        if (address is >= ColorRamStart and <= ColorRamEnd)
        {
            _colorRam[address - ColorRamStart] = (byte)(value & 0x0F);
            return;
        }

        _ram[address] = value;
    }

    public bool HandlesAddress(ushort address) => true;

    public byte ReadVideoMemory(ushort address)
    {
        if (address is >= ColorRamStart and <= ColorRamEnd)
            return _colorRam[address - ColorRamStart];

        if (address is >= CharStart and <= IoEnd)
            return _charRom[address - CharStart];

        return _ram[address];
    }

    private bool IsIoVisible => _pla.Charen && (_pla.Loram || _pla.Hiram);

    private bool IsCpuCharRomVisible => !_pla.Charen && (_pla.Loram || _pla.Hiram);

    private byte ReadCia1PortA()
    {
        return (byte)(_keyboard.ReadRowState() & _joystickPort2.ReadPortState());
    }

    private byte ReadCia1PortB()
    {
        return _keyboard.ReadColumnState();
    }

    private byte ReadIo(ushort address, bool peek)
    {
        if (address < 0xD400)
            return peek ? _vic.Peek(address) : _vic.Read(address);

        if (address < 0xD800)
            return peek ? _sid.Peek(address) : _sid.Read(address);

        if (address is >= ColorRamStart and <= ColorRamEnd)
            return _colorRam[address - ColorRamStart];

        if (address < 0xDD00)
            return peek ? _cia1.Peek(address) : _cia1.Read(address);

        if (address < 0xDE00)
            return peek ? _cia2.Peek(address) : _cia2.Read(address);

        return _ram[address];
    }

    private void WriteIo(ushort address, byte value)
    {
        if (address < 0xD400)
        {
            _vic.Write(address, value);
            return;
        }

        if (address < 0xD800)
        {
            _sid.Write(address, value);
            return;
        }

        if (address is >= ColorRamStart and <= ColorRamEnd)
        {
            _colorRam[address - ColorRamStart] = (byte)(value & 0x0F);
            return;
        }

        if (address < 0xDD00)
        {
            _cia1.Write(address, value);
            return;
        }

        if (address < 0xDE00)
        {
            _cia2.Write(address, value);
            return;
        }

        _ram[address] = value;
    }
}
