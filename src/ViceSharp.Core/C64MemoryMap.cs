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
internal sealed class C64MemoryMap : IMemory, IKeyboardMatrix, IMachineKeyboardInput, IKeyboardInputMapSelection, IMachineJoystickInput, ICartridgePort
{
    private const int CartridgeBankSize = 0x2000;
    private const int GameSystemCartridgeSize = CartridgeBankSize * 64;
    private const ushort CartridgeRomLowStart = 0x8000;
    private const ushort CartridgeRomLowEnd = 0x9FFF;
    private const ushort CartridgeStandardRomHighStart = 0xA000;
    private const ushort CartridgeStandardRomHighEnd = 0xBFFF;
    private const ushort CartridgeUltimaxRomHighStart = 0xE000;
    private const ushort CartridgeIo1Start = 0xDE00;
    private const ushort CartridgeIo1End = 0xDEFF;

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
    private readonly Mos6526? _cia2;
    private readonly Mos906114 _pla;
    private readonly C64KeyboardMatrix _keyboard;
    private readonly Dictionary<byte, int> _keyboardKeyPressCounts = new();
    private readonly C64JoystickPort _joystickPort1;
    private readonly C64JoystickPort _joystickPort2;
    private readonly bool _keyboardEnabled;
    private readonly byte _cia2PortAInputMask;
    private readonly bool _cia2Connected;
    private readonly CartridgeMappingMode _defaultCartridgeMappingMode;
    private IKeyboardInputMap _keyboardMap;
    private byte[]? _cartridgeImage;
    private CartridgeMappingMode? _attachedCartridgeMappingMode;
    private int _gameSystemCartridgeBank;
    private int _vicPhi1Bank;
    private bool _loadingRoms;
    private IBusEndpoint? _cartPortPinSource;

    public C64MemoryMap(
        Mos6569 vic,
        Sid6581 sid,
        Mos6526 cia1,
        Mos6526? cia2,
        Mos906114 pla,
        bool keyboardEnabled = true,
        IKeyboardInputMap? keyboardMap = null,
        byte cia2PortAInputMask = 0x7F,
        bool cia2Connected = true,
        CartridgeMappingMode defaultCartridgeMappingMode = CartridgeMappingMode.Auto)
    {
        _vic = vic;
        _sid = sid;
        _cia1 = cia1;
        _cia2 = cia2Connected
            ? cia2 ?? throw new ArgumentNullException(nameof(cia2))
            : null;
        _pla = pla;
        _keyboardEnabled = keyboardEnabled;
        _cia2PortAInputMask = cia2PortAInputMask;
        _cia2Connected = cia2Connected;
        _defaultCartridgeMappingMode = defaultCartridgeMappingMode;
        _keyboard = new C64KeyboardMatrix();
        _keyboardMap = keyboardMap ?? C64HostKeyboardMapper.DefaultFallbackMap;
        _joystickPort1 = new C64JoystickPort();
        _joystickPort2 = new C64JoystickPort();

        _cia1.PortAInput = ReadCia1PortA;
        _cia1.PortBInput = ReadCia1PortB;
        _cia1.PortAOutputChanged = value => _keyboard.SetRowMask(value);
        _cia1.PortBOutputChanged = value => _keyboard.SetColumnMask(value);
        if (_cia2Connected)
        {
            _cia2!.PortAInput = ReadCia2PortA;
            _cia2.PortAExternalInputMask = 0xC0;
            _cia2.PortAOutputChanged = value =>
            {
                var bank = 3 - (value & 0x03);
                _vic.VicBank = bank;
                _vicPhi1Bank = bank;
            };
        }

        _vic.VideoMemoryReader = ReadVideoMemory;
        _vic.Phi1MemoryReader = ReadVicPhi1OpenBus;

        Reset();
    }

    public DeviceId Id => new DeviceId(0x0101);
    public string Name => "C64 Memory Map";

    public Span<byte> Span => _ram;

    public IKeyboardInputMap KeyboardMap => _keyboardMap;

    public CartridgeMappingMode DefaultMappingMode => _defaultCartridgeMappingMode;

    public CartridgeMappingMode? AttachedMappingMode => _attachedCartridgeMappingMode;

    public bool IsCartridgeAttached => _cartridgeImage is not null;

    public void Reset()
    {
        InitializeRam();
        Array.Fill(_colorRam, (byte)0x0E);

        _keyboard.Reset();
        _keyboardKeyPressCounts.Clear();
        _joystickPort1.Reset();
        _joystickPort2.Reset();
        _vic.VicBank = 3;
        _vicPhi1Bank = 0;
        if (_attachedCartridgeMappingMode == CartridgeMappingMode.GameSystem)
            _gameSystemCartridgeBank = 0;
    }

    private void InitializeRam()
    {
        for (var address = 0; address < _ram.Length; address++)
        {
            var value = (((address + 2) / 4) & 1) != 0 ? 0xFF : 0x00;
            if (((address / 0x4000) & 1) != 0)
            {
                value ^= 0xFF;
            }

            _ram[address] = (byte)value;
        }
    }

    public void BeginRomLoad()
    {
        _loadingRoms = true;
    }

    public void EndRomLoad()
    {
        _loadingRoms = false;
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
        if (address <= 0x0001)
            return _pla.Read(address);

        if (TryReadCartridge(address, out var cartridgeValue))
            return cartridgeValue;

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
        if (address <= 0x0001)
            return _pla.Peek(address);

        if (TryReadCartridge(address, out var cartridgeValue))
            return cartridgeValue;

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
        if (_loadingRoms)
        {
            if (address is >= BasicStart and < 0xC000)
            {
                _basicRom[address - BasicStart] = value;
                return;
            }

            if (address is >= KernalStart and <= 0xFFFF)
            {
                _kernalRom[address - KernalStart] = value;
                return;
            }

            if (address is >= CharStart and <= IoEnd)
            {
                _charRom[address - CharStart] = value;
                return;
            }
        }

        if (address <= 0x0001)
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

    public void AttachCartridge(ReadOnlyMemory<byte> image, CartridgeMappingMode mappingMode)
    {
        var resolvedMappingMode = ResolveMappingMode(image.Length, mappingMode);
        ValidateCartridgeImageLength(image.Length, resolvedMappingMode);

        _cartridgeImage = image.ToArray();
        _attachedCartridgeMappingMode = resolvedMappingMode;
        _gameSystemCartridgeBank = 0;
    }

    public void EjectCartridge()
    {
        _cartridgeImage = null;
        _attachedCartridgeMappingMode = null;
        _gameSystemCartridgeBank = 0;
    }

    /// <summary>
    /// Bind a cart-port InterSystemBus endpoint. When set + a cartridge image
    /// is attached, the active mapping mode is derived from the live GAME +
    /// EXROM pin state on the bus instead of the static
    /// AttachedMappingMode. Pass null to revert to static mapping.
    ///
    /// Pin -> mode table (matches the real C64 PLA decode):
    ///   GAME=1 EXROM=1  no cart (image hidden until pins assert)
    ///   GAME=1 EXROM=0  Standard 8K
    ///   GAME=0 EXROM=0  Standard 16K
    ///   GAME=0 EXROM=1  Ultimax
    /// </summary>
    public void SetCartPortPinSource(IBusEndpoint? endpoint)
    {
        _cartPortPinSource = endpoint;
    }

    private CartridgeMappingMode? ResolveActiveMappingMode()
    {
        if (_cartridgeImage is null) return null;
        if (_cartPortPinSource is null) return _attachedCartridgeMappingMode;

        var game = _cartPortPinSource.ReadLine(CartPortInterSystemBus.Game);
        var exrom = _cartPortPinSource.ReadLine(CartPortInterSystemBus.ExRom);
        return (game, exrom) switch
        {
            (true, true) => null,
            (true, false) => CartridgeMappingMode.Standard8K,
            (false, false) => CartridgeMappingMode.Standard16K,
            (false, true) => CartridgeMappingMode.Ultimax,
        };
    }

    public void SetKey(byte keyCode, bool pressed)
    {
        if (!_keyboardEnabled)
            return;

        _keyboard.SetKey(keyCode, pressed);
    }

    public bool SetKeyState(string key, bool pressed)
    {
        if (!_keyboardEnabled)
            return false;

        if (!_keyboardMap.TryResolve(key, out var keyCodes))
            return false;

        foreach (var keyCode in keyCodes)
            ApplyMatrixKeyState(keyCode, pressed);

        return true;
    }

    public bool SetJoystickState(int controlPort, byte directionMask, bool fireButton)
    {
        var port = controlPort switch
        {
            1 => _joystickPort1,
            2 => _joystickPort2,
            _ => null
        };

        if (port is null)
            return false;

        var pressed = (C64JoystickPort.JoystickButtons)(directionMask & 0x0F);
        if (fireButton)
            pressed |= C64JoystickPort.JoystickButtons.Fire;

        port.State = pressed;
        return true;
    }

    public void SelectKeyboardMap(IKeyboardInputMap keyboardMap)
    {
        ArgumentNullException.ThrowIfNull(keyboardMap);

        _keyboardMap = keyboardMap;
        _keyboardKeyPressCounts.Clear();
        _keyboard.ClearKeys();
    }

    public bool IsRestorePressed => _keyboard.IsRestorePressed;

    public bool IsStopPressed => _keyboard.IsStopPressed;

    public bool IsShiftCbmPressed => _keyboard.IsShiftCbmPressed;

    public bool KeyboardEnabled => _keyboardEnabled;

    public byte ReadVideoMemory(ushort address)
    {
        if (address is >= ColorRamStart and <= ColorRamEnd)
            return _colorRam[address - ColorRamStart];

        var vicAddress = (ushort)(address & 0x3FFF);
        if (TryReadUltimaxVideoMemory(vicAddress, out var cartridgeValue))
            return cartridgeValue;

        if (vicAddress is >= 0x1000 and < 0x2000 && (_vic.VicBank is 0 or 2))
            return _charRom[vicAddress - 0x1000];

        return _ram[_vic.TranslateVicAddress(vicAddress)];
    }

    private bool IsIoVisible => _pla.Charen && (_pla.Loram || _pla.Hiram);

    private bool IsCpuCharRomVisible => !_pla.Charen && (_pla.Loram || _pla.Hiram);

    private byte ReadCia1PortA()
    {
        return (byte)(_keyboard.ReadRowState() & _joystickPort2.ReadPortState());
    }

    private byte ReadCia1PortB()
    {
        return (byte)(_keyboard.ReadColumnState() & _joystickPort1.ReadPortState());
    }

    private byte ReadCia2PortA()
    {
        return _cia2PortAInputMask;
    }

    private byte ReadIo(ushort address, bool peek)
    {
        if (!peek && TryAccessGameSystemBankRegister(address, out var value))
            return value;

        if (address < 0xD400)
            return peek ? _vic.Peek(address) : _vic.Read(address);

        if (address < 0xD800)
            return peek ? _sid.Peek(address) : _sid.Read(address);

        if (address is >= ColorRamStart and <= ColorRamEnd)
            return _colorRam[address - ColorRamStart];

        if (address < 0xDD00)
            return peek ? _cia1.Peek(address) : _cia1.Read(address);

        if (address < 0xDE00)
            return _cia2 is null ? _vic.LastReadPhi1 : peek ? _cia2.Peek(address) : _cia2.Read(address);

        return _ram[address];
    }

    private byte ReadVicPhi1OpenBus(byte cycle)
    {
        return _vic.CyclesPerLine switch
        {
            Mos6569.PalCyclesPerLine => ReadVicPhi1Pal(cycle),
            Mos6569.NtscOldCyclesPerLine => ReadVicPhi1NtscOld(cycle),
            Mos6569.NtscCyclesPerLine => ReadVicPhi1Ntsc(cycle),
            _ => ReadVicGfxDataOrIdle(cycle)
        };
    }

    private byte ReadVicPhi1Pal(byte cycle) => cycle switch
    {
        0 => ReadVicSpritePointer(3),
        1 => ReadVicIdleGap(),
        2 => ReadVicSpritePointer(4),
        3 => ReadVicIdleGap(),
        4 => ReadVicSpritePointer(5),
        5 => ReadVicIdleGap(),
        6 => ReadVicSpritePointer(6),
        7 => ReadVicIdleGap(),
        8 => ReadVicSpritePointer(7),
        9 => ReadVicIdleGap(),
        >= 10 and <= 14 => ReadVicRefreshCounter(),
        55 or 56 => ReadVicIdleGap(),
        57 => ReadVicSpritePointer(0),
        58 => ReadVicIdleGap(),
        59 => ReadVicSpritePointer(1),
        60 => ReadVicIdleGap(),
        61 => ReadVicSpritePointer(2),
        62 => ReadVicIdleGap(),
        _ => ReadVicGfxDataOrIdle((byte)(cycle - 15))
    };

    private byte ReadVicPhi1NtscOld(byte cycle) => cycle switch
    {
        0 => ReadVicSpritePointer(3),
        1 => ReadVicIdleGap(),
        2 => ReadVicSpritePointer(4),
        3 => ReadVicIdleGap(),
        4 => ReadVicSpritePointer(5),
        5 => ReadVicIdleGap(),
        6 => ReadVicSpritePointer(6),
        7 => ReadVicIdleGap(),
        8 => ReadVicSpritePointer(7),
        9 => ReadVicIdleGap(),
        >= 10 and <= 14 => ReadVicRefreshCounter(),
        55 or 56 or 57 => ReadVicIdleGap(),
        58 => ReadVicSpritePointer(0),
        59 => ReadVicIdleGap(),
        60 => ReadVicSpritePointer(1),
        61 => ReadVicIdleGap(),
        62 => ReadVicSpritePointer(2),
        63 => ReadVicIdleGap(),
        _ => ReadVicGfxDataOrIdle((byte)(cycle - 15))
    };

    private byte ReadVicPhi1Ntsc(byte cycle) => cycle switch
    {
        0 => ReadVicIdleGap(),
        1 => ReadVicSpritePointer(4),
        2 => ReadVicIdleGap(),
        3 => ReadVicSpritePointer(5),
        4 => ReadVicIdleGap(),
        5 => ReadVicSpritePointer(6),
        6 => ReadVicIdleGap(),
        7 => ReadVicSpritePointer(7),
        8 => ReadVicIdleGap(),
        9 => ReadVicIdleGap(),
        >= 10 and <= 14 => ReadVicRefreshCounter(),
        55 or 56 or 57 => ReadVicIdleGap(),
        58 => ReadVicSpritePointer(0),
        59 => ReadVicIdleGap(),
        60 => ReadVicSpritePointer(1),
        61 => ReadVicIdleGap(),
        62 => ReadVicSpritePointer(2),
        63 => ReadVicIdleGap(),
        64 => ReadVicSpritePointer(3),
        _ => ReadVicGfxDataOrIdle((byte)(cycle - 15))
    };

    private byte ReadVicSpritePointer(byte sprite)
    {
        var pointerBase = (ushort)((_vic.Peek(0xD018) & 0xF0) << 6);
        return ReadVicPhi1Ram((ushort)(pointerBase + 0x03F8 + sprite));
    }

    private byte ReadVicRefreshCounter()
    {
        var offset = _vic.ConsumeRefreshCounter();
        return ReadVicPhi1Ram((ushort)(0x3F00 + offset));
    }

    private byte ReadVicGfxDataOrIdle(byte _) =>
        _vic.IsGraphicsIdle ? ReadVicIdleGap() : ReadVicPhi1Ram(_vic.ConsumeGraphicsFetchAddress());

    private byte ReadVicIdleGap() => ReadVicPhi1Ram(0x3FFF);

    private byte ReadVicPhi1Ram(ushort vicAddress)
    {
        var normalizedAddress = (ushort)(vicAddress & 0x3FFF);
        var effectiveAddress = TranslateVicPhi1Address(normalizedAddress);

        if (TryReadUltimaxPhi1Memory(effectiveAddress, out var cartridgeValue))
            return cartridgeValue;

        if (normalizedAddress is >= 0x1000 and < 0x2000 && (_vicPhi1Bank is 0 or 2))
            return _charRom[effectiveAddress & 0x0FFF];

        return _ram[effectiveAddress];
    }

    private ushort TranslateVicPhi1Address(ushort vicAddress)
    {
        return (ushort)((_vicPhi1Bank << 14) | (vicAddress & 0x3FFF));
    }

    private void WriteIo(ushort address, byte value)
    {
        if (TryStoreGameSystemBankRegister(address))
            return;

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
            if (_cia2 is not null)
                _cia2.Write(address, value);

            return;
        }

        _ram[address] = value;
    }

    private bool TryAccessGameSystemBankRegister(ushort address, out byte value)
    {
        value = 0;

        if (_attachedCartridgeMappingMode != CartridgeMappingMode.GameSystem ||
            address is < CartridgeIo1Start or > CartridgeIo1End)
        {
            return false;
        }

        _gameSystemCartridgeBank = address & 0x3F;
        return true;
    }

    private bool TryStoreGameSystemBankRegister(ushort address)
    {
        if (_attachedCartridgeMappingMode != CartridgeMappingMode.GameSystem ||
            address is < CartridgeIo1Start or > CartridgeIo1End)
        {
            return false;
        }

        _gameSystemCartridgeBank = address & 0x3F;
        return true;
    }

    private void ApplyMatrixKeyState(byte keyCode, bool pressed)
    {
        _keyboardKeyPressCounts.TryGetValue(keyCode, out var count);
        count = pressed ? count + 1 : Math.Max(0, count - 1);

        if (count == 0)
            _keyboardKeyPressCounts.Remove(keyCode);
        else
            _keyboardKeyPressCounts[keyCode] = count;

        _keyboard.SetKey(keyCode, count > 0);
    }

    private bool TryReadCartridge(ushort address, out byte value)
    {
        value = 0;

        if (_cartridgeImage is null || ResolveActiveMappingMode() is not { } mappingMode)
            return false;

        if (!TryGetCartridgeOffset(address, mappingMode, _cartridgeImage.Length, out var offset))
            return false;

        value = _cartridgeImage[offset];
        return true;
    }

    private bool TryReadUltimaxVideoMemory(ushort vicAddress, out byte value)
    {
        value = 0;

        if (_cartridgeImage is null || ResolveActiveMappingMode() != CartridgeMappingMode.Ultimax)
            return false;

        if (vicAddress <= 0x0FFF)
            return false;

        var offset = vicAddress >= CartridgeBankSize
            ? _cartridgeImage.Length == CartridgeBankSize
                ? vicAddress - CartridgeBankSize
                : CartridgeBankSize + vicAddress - CartridgeBankSize
            : vicAddress - 0x1000;

        if (offset < 0 || offset >= _cartridgeImage.Length)
            return false;

        value = _cartridgeImage[offset];
        return true;
    }

    private bool TryReadUltimaxPhi1Memory(ushort effectiveAddress, out byte value)
    {
        value = 0;

        if (_cartridgeImage is null ||
            ResolveActiveMappingMode() != CartridgeMappingMode.Ultimax ||
            (effectiveAddress & 0x3FFF) < 0x3000)
        {
            return false;
        }

        var offset = _cartridgeImage.Length == CartridgeBankSize
            ? 0x1000 + (effectiveAddress & 0x0FFF)
            : 0x3000 + (effectiveAddress & 0x0FFF);

        if (offset < 0 || offset >= _cartridgeImage.Length)
            return false;

        value = _cartridgeImage[offset];
        return true;
    }

    private CartridgeMappingMode ResolveMappingMode(int imageLength, CartridgeMappingMode requestedMappingMode)
    {
        if (requestedMappingMode != CartridgeMappingMode.Auto)
            return requestedMappingMode;

        if (_defaultCartridgeMappingMode == CartridgeMappingMode.Ultimax)
            return CartridgeMappingMode.Ultimax;

        if (_defaultCartridgeMappingMode == CartridgeMappingMode.GameSystem && imageLength == GameSystemCartridgeSize)
            return CartridgeMappingMode.GameSystem;

        return imageLength switch
        {
            CartridgeBankSize => CartridgeMappingMode.Standard8K,
            CartridgeBankSize * 2 => CartridgeMappingMode.Standard16K,
            _ => CartridgeMappingMode.Auto
        };
    }

    private static void ValidateCartridgeImageLength(int imageLength, CartridgeMappingMode mappingMode)
    {
        var valid = mappingMode switch
        {
            CartridgeMappingMode.Standard8K => imageLength == CartridgeBankSize,
            CartridgeMappingMode.Standard16K => imageLength == CartridgeBankSize * 2,
            CartridgeMappingMode.Ultimax => imageLength is CartridgeBankSize or CartridgeBankSize * 2,
            CartridgeMappingMode.GameSystem => imageLength == GameSystemCartridgeSize,
            _ => false
        };

        if (!valid)
            throw new ArgumentException("Raw C64 cartridge images must be 8K or 16K and match the selected mapping mode.");
    }

    private bool TryGetCartridgeOffset(
        ushort address,
        CartridgeMappingMode mappingMode,
        int imageLength,
        out int offset)
    {
        offset = 0;

        if (address is >= CartridgeRomLowStart and <= CartridgeRomLowEnd)
        {
            if (!IsRomLowVisible(mappingMode))
                return false;

            if (mappingMode == CartridgeMappingMode.Ultimax && imageLength == CartridgeBankSize)
                return false;

            offset = mappingMode == CartridgeMappingMode.GameSystem
                ? (_gameSystemCartridgeBank * CartridgeBankSize) + address - CartridgeRomLowStart
                : address - CartridgeRomLowStart;
            return true;
        }

        if (mappingMode == CartridgeMappingMode.Standard16K &&
            address is >= CartridgeStandardRomHighStart and <= CartridgeStandardRomHighEnd)
        {
            if (!IsRomHighVisible(mappingMode))
                return false;

            offset = CartridgeBankSize + address - CartridgeStandardRomHighStart;
            return true;
        }

        if (mappingMode == CartridgeMappingMode.Ultimax && address is >= CartridgeUltimaxRomHighStart)
        {
            offset = imageLength == CartridgeBankSize
                ? address - CartridgeUltimaxRomHighStart
                : CartridgeBankSize + address - CartridgeUltimaxRomHighStart;
            return true;
        }

        return false;
    }

    private bool IsRomLowVisible(CartridgeMappingMode mappingMode)
    {
        return mappingMode switch
        {
            CartridgeMappingMode.Ultimax => true,
            CartridgeMappingMode.Standard8K or CartridgeMappingMode.Standard16K or CartridgeMappingMode.GameSystem
                => _pla.Loram && _pla.Hiram,
            _ => false
        };
    }

    private bool IsRomHighVisible(CartridgeMappingMode mappingMode)
    {
        return mappingMode switch
        {
            CartridgeMappingMode.Ultimax => true,
            CartridgeMappingMode.Standard16K => _pla.Hiram,
            _ => false
        };
    }
}
