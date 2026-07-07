using ViceSharp.Abstractions;
using ViceSharp.Chips.Audio;
using ViceSharp.Chips.Cia;
using ViceSharp.Core.Input;
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
    private const ushort VicScreenControl2 = 0xD016;
    private const ushort ColorRamStart = 0xD800;
    private const ushort ColorRamEnd = 0xDBFF;

    // PERF-MEM-001: precomputed 16-entry (one per 4KB page) read dispatch table.
    // Rebuilt whenever PLA banking state changes (CPU writes $0001) or cartridge
    // is attached/ejected. Eliminates 5 if-branches + PLA property reads (~3 bit-ANDs)
    // from the hot Read() path (~80,000 calls per PAL frame).
    private enum PageKind : byte
    {
        Ram = 0,
        Pla0 = 1,     // page 0: $0000-$0001 -> PLA, $0002-$0FFF -> RAM
        BasicRom = 2,  // $A000-$BFFF when BasicRomVisible && no cart override
        KernalRom = 3, // $E000-$FFFF when KernalRomVisible && no cart override
        Io = 4,        // $D000-$DFFF when CHAREN && (LORAM||HIRAM)
        CharRom = 5,   // $D000-$DFFF when !CHAREN && (LORAM||HIRAM)
        CartOrRam = 6, // page may be overridden by attached cartridge: use slow path
    }

    private readonly PageKind[] _readPageTable = new PageKind[16];

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
    private readonly object _inputSync = new();
    private readonly Dictionary<byte, int> _keyboardKeyPressCounts = new();
    private readonly C64JoystickPort _joystickPort1;
    private readonly C64JoystickPort _joystickPort2;
    private readonly bool _keyboardEnabled;
    private readonly byte _cia2PortAInputMask;
    private readonly bool _cia2Connected;
    private readonly CartridgeMappingMode _defaultCartridgeMappingMode;
    private readonly Func<ushort>? _cpuPcReader;
    private IKeyboardInputMap _keyboardMap;
    private byte[]? _cartridgeImage;
    private CartridgeMappingMode? _attachedCartridgeMappingMode;
    private int _gameSystemCartridgeBank;

    // Magic Desk (CRT type 19) state. _magicDeskCartridgeBank selects the
    // active 8K ROML bank (bits 0-6 of the latest $DE00 write).
    // _magicDeskDisabled mirrors bit 7 of the latest $DE00 write: when
    // high the cartridge releases its ROML mapping (EXROM goes high) and
    // $8000-$9FFF reads fall through to RAM/BASIC.
    private int _magicDeskCartridgeBank;
    private bool _magicDeskDisabled;

    // Ocean (CRT type 5) state. Same ROML-only bank selection as Magic
    // Desk but without a disable bit; the register stores bits 0-5 of
    // the latest $DE00 write as the active bank index.
    private int _oceanCartridgeBank;

    // Final Cartridge III (CRT type 60) state. 16K banks (ROML+ROMH) and a
    // hide bit. Bank register lives at $DFFF: bits 0-1 select bank, bit 7
    // hides the cartridge so $8000-$BFFF reads fall through to RAM/BASIC.
    private int _fc3CartridgeBank;
    private bool _fc3Hidden;
    private const ushort Fc3BankRegisterAddress = 0xDFFF;

    // Action Replay V4/V5 (CRT type 1) state. 32K image, 4 banks of 8K
    // (ROML only). Bank reg at $DE00: bits 0-3 = bank mod 4, bit 7 = hide.
    private int _arCartridgeBank;
    private bool _arHidden;

    // EasyFlash (CRT type 32) state. Up to 1024K image, banked 8K ROML.
    // Bank reg at $DE00 (bits 0-5 mod bank-count). Flash writes deferred.
    private int _easyFlashCartridgeBank;

    // Super Snapshot V5 (CRT type 4) state. 64K image = 4 banks of 16K
    // (ROML+ROMH). Bank reg at $DE00: bits 2-4 = bank, bit 1 = hide.
    private int _ss5CartridgeBank;
    private bool _ss5Hidden;

    // RR-Net (CRT type 25) state. 64K image = 4 banks of 16K (ROML+ROMH).
    // Bank reg at $DE00: bits 0-3 = bank mod 4, bit 7 = hide. The $DE00-
    // $DE0F Ethernet register window is stubbed.
    private int _rrNetCartridgeBank;
    private bool _rrNetHidden;
    // PLAN-VICEPARITY-001 audit M6 RESOLUTION: audit claimed VICE boots both
    // banks at base 0 (vicii.c:354-355), but the lockstep oracle REFUTES
    // changing this seed: with _vicBank = 0 the CPU stream diverges from
    // native x64sc within 200 cycles (LockstepValidationTests), because bank 0
    // enables the managed char-ROM window at VIC $1000-$1FFF while VICE's
    // pre-CIA2 init leaves chargen matching unconfigured and fetches raw RAM.
    // The 3/0 seed is observationally equivalent to native at boot (the C64
    // RAM init pattern repeats every $4000, so base $C000 and base $0000
    // fetch identical bytes) and the KERNAL programs the real bank via $DD00
    // (both fields then track it symmetrically, :163-164) before the display
    // is enabled. Verified by First10000/100000CyclesMatch plus the bit-exact
    // boot-frame oracle test.
    private int _vicBank = 3;
    private int _vicPhi1Bank;
    private byte _lastCpuBusValue = 0xFF;
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
        CartridgeMappingMode defaultCartridgeMappingMode = CartridgeMappingMode.Auto,
        Func<ushort>? cpuPcReader = null)
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
        _cpuPcReader = cpuPcReader;
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
                _vicBank = bank;
                _vicPhi1Bank = bank;
            };
        }

        _vic.VideoMemoryReader = ReadVideoMemory;
        _vic.Phi1MemoryReader = ReadVicPhi1OpenBus;
        // PLAN-VICEPARITY-001 FR-VIC-FETCH AC-09: char-ROM address predicate for
        // the 6569 RAM-to-char-ROM g-fetch latch (VICE is_char_rom,
        // viciisc/vicii-fetch.c:184-188): the $1000-$1FFF window resolves to
        // character ROM in VIC banks 0 and 2.
        _vic.CharRomAddressProbe = IsVicPhi1CharRomAddress;

        Reset();
        // Note: RebuildReadPageTable() is called inside Reset(). The board
        // processor-port state is applied there, so the table is rebuilt with
        // the C64 power-up PLA inputs during machine.Reset().
    }

    public DeviceId Id => new DeviceId(0x0101);
    public string Name => "C64 Memory Map";

    public Span<byte> Span => _ram;

    public IKeyboardInputMap KeyboardMap => _keyboardMap;

    public CartridgeMappingMode DefaultMappingMode => _defaultCartridgeMappingMode;

    public CartridgeMappingMode? AttachedMappingMode => _attachedCartridgeMappingMode;

    public bool ShouldDeferCpuAbsoluteStore(ushort address) => address == VicScreenControl2;

    public bool ShouldDelayCpuFetchAfterWrite(ushort address) => address == VicScreenControl2;

    public bool IsCartridgeAttached => _cartridgeImage is not null;

    public void Reset()
    {
        InitializeRam();
        Array.Fill(_colorRam, (byte)0x0E);

        _keyboard.Reset();
        _keyboardKeyPressCounts.Clear();
        _joystickPort1.Reset();
        _joystickPort2.Reset();
        _pla.WriteDataDirection(0x2F);
        _pla.WriteDataPort(0x37);
        // audit M6 RESOLUTION: keep the 3/0 boot seed; see the field comment.
        // Changing it to 0/0 breaks CPU lockstep vs native x64sc.
        _vicBank = 3;
        _vicPhi1Bank = 0;
        if (_attachedCartridgeMappingMode == CartridgeMappingMode.GameSystem)
            _gameSystemCartridgeBank = 0;
        if (_attachedCartridgeMappingMode == CartridgeMappingMode.MagicDesk)
        {
            _magicDeskCartridgeBank = 0;
            _magicDeskDisabled = false;
        }
        if (_attachedCartridgeMappingMode == CartridgeMappingMode.Ocean)
            _oceanCartridgeBank = 0;
        if (_attachedCartridgeMappingMode == CartridgeMappingMode.FinalCartridgeIII)
        {
            _fc3CartridgeBank = 0;
            _fc3Hidden = false;
        }
        if (_attachedCartridgeMappingMode == CartridgeMappingMode.ActionReplay)
        {
            _arCartridgeBank = 0;
            _arHidden = false;
        }
        if (_attachedCartridgeMappingMode == CartridgeMappingMode.EasyFlash)
            _easyFlashCartridgeBank = 0;
        if (_attachedCartridgeMappingMode == CartridgeMappingMode.SuperSnapshotV5)
        {
            _ss5CartridgeBank = 0;
            _ss5Hidden = false;
        }
        if (_attachedCartridgeMappingMode == CartridgeMappingMode.RRNet)
        {
            _rrNetCartridgeBank = 0;
            _rrNetHidden = false;
        }

        // PERF-MEM-001: Rebuild page table after applying the board-owned
        // processor-port reset state so PLA banking starts in the C64 power-up map.
        RebuildReadPageTable();
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
        // PERF-MEM-001: precomputed page table eliminates 5 if-branches + PLA property
        // reads from the hot path. Table is rebuilt on PLA state change or cart attach/eject.
        return _readPageTable[address >> 12] switch
        {
            PageKind.Ram => LatchCpuBusValue(_ram[address]),
            PageKind.Pla0 => address <= 0x0001
                ? LatchCpuBusValue(_pla.Read(address))
                : LatchCpuBusValue(_ram[address]),
            PageKind.BasicRom => LatchCpuBusValue(_basicRom[address - BasicStart]),
            PageKind.KernalRom => LatchCpuBusValue(_kernalRom[address - KernalStart]),
            PageKind.Io => LatchCpuBusValue(ReadIo(address, peek: false)),
            PageKind.CharRom => LatchCpuBusValue(_charRom[address - CharStart]),
            _ /* CartOrRam */ => ReadWithCartridgeCheck(address),
        };
    }

    // PERF-MEM-001: slow path used only when a cartridge is attached and the address
    // falls in a page that may be overridden by the cart image. Identical to the
    // original read logic for cart-affected pages.
    private byte ReadWithCartridgeCheck(ushort address)
    {
        if (TryReadCartridge(address, out var cartridgeValue))
            return LatchCpuBusValue(cartridgeValue);

        if (address is >= BasicStart and < 0xC000 && _pla.BasicRomVisible)
            return LatchCpuBusValue(_basicRom[address - BasicStart]);

        if (address >= KernalStart && _pla.KernalRomVisible)
            return LatchCpuBusValue(_kernalRom[address - KernalStart]);

        if (address is >= IoStart and <= IoEnd)
        {
            if (IsIoVisible)
                return LatchCpuBusValue(ReadIo(address, peek: false));

            if (IsCpuCharRomVisible)
                return LatchCpuBusValue(_charRom[address - CharStart]);
        }

        return LatchCpuBusValue(_ram[address]);
    }

    // PERF-MEM-001: Rebuild the 16-entry page table from current PLA + cart state.
    // Called on constructor, PLA writes ($0000/$0001), and cart attach/eject.
    // Cost is tiny (~20 array writes) and happens rarely (only when banking changes).
    private void RebuildReadPageTable()
    {
        // Default all pages to RAM.
        for (int i = 0; i < 16; i++)
            _readPageTable[i] = PageKind.Ram;

        // Page 0 ($0000-$0FFF): $0000-$0001 are PLA processor port.
        _readPageTable[0x0] = PageKind.Pla0;

        bool hasCart = _cartridgeImage is not null;

        if (hasCart)
        {
            // Cartridge may intercept $8000-$9FFF, $A000-$BFFF, and/or $E000-$FFFF.
            // Mark affected pages as CartOrRam (slow path) rather than pre-routing
            // to ROM, because cart takes priority over ROM.
            _readPageTable[0x8] = PageKind.CartOrRam;
            _readPageTable[0x9] = PageKind.CartOrRam;
            _readPageTable[0xA] = PageKind.CartOrRam;
            _readPageTable[0xB] = PageKind.CartOrRam;
            _readPageTable[0xE] = PageKind.CartOrRam;
            _readPageTable[0xF] = PageKind.CartOrRam;
        }
        else
        {
            // No cartridge: pure PLA-based mapping.
            if (_pla.BasicRomVisible)
            {
                _readPageTable[0xA] = PageKind.BasicRom;
                _readPageTable[0xB] = PageKind.BasicRom;
            }

            if (_pla.KernalRomVisible)
            {
                _readPageTable[0xE] = PageKind.KernalRom;
                _readPageTable[0xF] = PageKind.KernalRom;
            }
        }

        // $D000-$DFFF: I/O, CharROM, or RAM depending on CHAREN + LORAM/HIRAM.
        if (IsIoVisible)
            _readPageTable[0xD] = PageKind.Io;
        else if (IsCpuCharRomVisible)
            _readPageTable[0xD] = PageKind.CharRom;
        // else: already set to Ram
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
        _lastCpuBusValue = value;

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
            RebuildReadPageTable(); // PERF-MEM-001: PLA banking bits may have changed
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
        _magicDeskCartridgeBank = 0;
        _magicDeskDisabled = false;
        _oceanCartridgeBank = 0;
        _fc3CartridgeBank = 0;
        _fc3Hidden = false;
        _arCartridgeBank = 0;
        _arHidden = false;
        _easyFlashCartridgeBank = 0;
        _ss5CartridgeBank = 0;
        _ss5Hidden = false;
        _rrNetCartridgeBank = 0;
        _rrNetHidden = false;
        RebuildReadPageTable(); // PERF-MEM-001: cart pages now active
    }

    public void EjectCartridge()
    {
        _cartridgeImage = null;
        _attachedCartridgeMappingMode = null;
        _gameSystemCartridgeBank = 0;
        _magicDeskCartridgeBank = 0;
        _magicDeskDisabled = false;
        _oceanCartridgeBank = 0;
        _fc3CartridgeBank = 0;
        _fc3Hidden = false;
        _arCartridgeBank = 0;
        _arHidden = false;
        _easyFlashCartridgeBank = 0;
        _ss5CartridgeBank = 0;
        _ss5Hidden = false;
        _rrNetCartridgeBank = 0;
        _rrNetHidden = false;
        RebuildReadPageTable(); // PERF-MEM-001: cart pages now inactive
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
        RebuildReadPageTable(); // PERF-MEM-001: GAME/EXROM pin state may affect cart mapping
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

        lock (_inputSync)
            _keyboard.SetKey(keyCode, pressed);
    }

    public bool SetKeyState(string key, bool pressed)
    {
        if (!_keyboardEnabled)
            return false;

        lock (_inputSync)
        {
            if (!_keyboardMap.TryResolve(key, out var keyCodes))
                return false;

            foreach (var keyCode in keyCodes)
                ApplyMatrixKeyState(keyCode, pressed);
        }

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

        lock (_inputSync)
            port.State = pressed;
        return true;
    }

    public void SelectKeyboardMap(IKeyboardInputMap keyboardMap)
    {
        ArgumentNullException.ThrowIfNull(keyboardMap);

        lock (_inputSync)
        {
            _keyboardMap = keyboardMap;
            _keyboardKeyPressCounts.Clear();
            _keyboard.ClearKeys();
        }
    }

    public bool IsRestorePressed
    {
        get
        {
            lock (_inputSync)
                return _keyboard.IsRestorePressed;
        }
    }

    public bool IsStopPressed
    {
        get
        {
            lock (_inputSync)
                return _keyboard.IsStopPressed;
        }
    }

    public bool IsShiftCbmPressed
    {
        get
        {
            lock (_inputSync)
                return _keyboard.IsShiftCbmPressed;
        }
    }

    public bool KeyboardEnabled => _keyboardEnabled;

    public byte ReadVideoMemory(ushort address)
    {
        if (address is >= ColorRamStart and <= ColorRamEnd)
            return _colorRam[address - ColorRamStart];

        var vicAddress = (ushort)(address & 0x3FFF);
        if (TryReadUltimaxVideoMemory(vicAddress, out var cartridgeValue))
            return cartridgeValue;

        if (vicAddress is >= 0x1000 and < 0x2000 && (_vicBank is 0 or 2))
            return _charRom[vicAddress - 0x1000];

        return _ram[TranslateVicAddress(vicAddress)];
    }

    private bool IsIoVisible => _pla.Charen && (_pla.Loram || _pla.Hiram);

    private bool IsCpuCharRomVisible => !_pla.Charen && (_pla.Loram || _pla.Hiram);

    private byte ReadCia1PortA()
    {
        lock (_inputSync)
            return (byte)(_keyboard.ReadRowState() & _joystickPort2.ReadPortState());
    }

    private byte ReadCia1PortB()
    {
        lock (_inputSync)
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
        1 => ReadVicSpriteData1(3),
        2 => ReadVicSpritePointer(4),
        3 => ReadVicSpriteData1(4),
        4 => ReadVicSpritePointer(5),
        5 => ReadVicSpriteData1(5),
        6 => ReadVicSpritePointer(6),
        7 => ReadVicSpriteData1(6),
        8 => ReadVicSpritePointer(7),
        9 => ReadVicSpriteData1(7),
        >= 10 and <= 13 => ReadVicRefreshCounter(),
        14 => ReadVicRefreshAndFirstMatrix(),
        55 or 56 => ReadVicIdleGap(),
        57 => ReadVicSpritePointer(0),
        58 => ReadVicSpriteData1(0),
        59 => ReadVicSpritePointer(1),
        60 => ReadVicSpriteData1(1),
        61 => ReadVicSpritePointer(2),
        62 => ReadVicSpriteData1(2),
        _ => ReadVicGfxDataOrIdle((byte)(cycle - 15))
    };

    private byte ReadVicPhi1NtscOld(byte cycle) => cycle switch
    {
        0 => ReadVicSpritePointer(3),
        1 => ReadVicSpriteData1(3),
        2 => ReadVicSpritePointer(4),
        3 => ReadVicSpriteData1(4),
        4 => ReadVicSpritePointer(5),
        5 => ReadVicSpriteData1(5),
        6 => ReadVicSpritePointer(6),
        7 => ReadVicSpriteData1(6),
        8 => ReadVicSpritePointer(7),
        9 => ReadVicSpriteData1(7),
        >= 10 and <= 13 => ReadVicRefreshCounter(),
        14 => ReadVicRefreshAndFirstMatrix(),
        55 or 56 or 57 => ReadVicIdleGap(),
        58 => ReadVicSpritePointer(0),
        59 => ReadVicSpriteData1(0),
        60 => ReadVicSpritePointer(1),
        61 => ReadVicSpriteData1(1),
        62 => ReadVicSpritePointer(2),
        63 => ReadVicSpriteData1(2),
        _ => ReadVicGfxDataOrIdle((byte)(cycle - 15))
    };

    private byte ReadVicPhi1Ntsc(byte cycle) => cycle switch
    {
        0 => ReadVicSpriteData1(3),
        1 => ReadVicSpritePointer(4),
        2 => ReadVicSpriteData1(4),
        3 => ReadVicSpritePointer(5),
        4 => ReadVicSpriteData1(5),
        5 => ReadVicSpritePointer(6),
        6 => ReadVicSpriteData1(6),
        7 => ReadVicSpritePointer(7),
        8 => ReadVicSpriteData1(7),
        9 => ReadVicIdleGap(),
        >= 10 and <= 13 => ReadVicRefreshCounter(),
        14 => ReadVicRefreshAndFirstMatrix(),
        55 or 56 or 57 => ReadVicIdleGap(),
        58 => ReadVicSpritePointer(0),
        59 => ReadVicSpriteData1(0),
        60 => ReadVicSpritePointer(1),
        61 => ReadVicSpriteData1(1),
        62 => ReadVicSpritePointer(2),
        63 => ReadVicSpriteData1(2),
        64 => ReadVicSpritePointer(3),
        _ => ReadVicGfxDataOrIdle((byte)(cycle - 15))
    };

    private byte ReadVicSpritePointer(byte sprite)
    {
        var pointerBase = (ushort)((_vic.Peek(0xD018) & 0xF0) << 6);
        var pointer = ReadVicPhi1Ram((ushort)(pointerBase + 0x03F8 + sprite));
        // PLAN-VICEPARITY-001 FR-VIC-FETCH AC-14: the p-access stores the pointer
        // (VICE vicii_fetch_sprite_pointer, viciisc/vicii-fetch.c:275-280) and the
        // Phi2 of the same cycle performs the first s-access (SprDma0,
        // vicii-chip-model.c cycle table; vicii-fetch.c:110-131).
        _vic.LatchSpritePointer(sprite, pointer);
        FetchSpriteDataPhi2(sprite, sAccessIndex: 0);
        return pointer;
    }

    // PLAN-VICEPARITY-001 FR-VIC-FETCH AC-14: second sprite s-access (SprDma1) on
    // the Phi1 following the pointer fetch: (pointer << 6) + mc with mc++ when DMA
    // is active, or the $3FFF idle fetch without an mc advance when it is not
    // (VICE vicii_fetch_sprite_dma_1, viciisc/vicii-fetch.c:282-299). audit M7:
    // the fetched byte merges into the middle data lane UNCONDITIONALLY
    // (:295-296). This Phi1 access is NOT gated by the BA prefetch window
    // (audit M9: vicii_fetch_sprite_dma_1 fetches regardless of
    // prefetch_cycles). The Phi2 of the same cycle performs the third s-access
    // (SprDma2, viciisc/vicii-fetch.c:133-154).
    private byte ReadVicSpriteData1(byte sprite)
    {
        byte data;
        if (_vic.IsSpriteDmaActive(sprite))
        {
            data = ReadVicPhi1Ram(_vic.GetSpriteDataFetchAddress(sprite));
            _vic.LatchSpriteData(sprite, sAccessIndex: 1, data);
        }
        else
        {
            data = ReadVicIdleGap();
            _vic.MergeSpriteData(sprite, sAccessIndex: 1, data);
        }

        FetchSpriteDataPhi2(sprite, sAccessIndex: 2);
        return data;
    }

    // PLAN-VICEPARITY-001 FR-VIC-FETCH AC-14 + audit M7/M9/L2: Phi2 sprite
    // s-access (SprDma0 on the pointer cycle, SprDma2 on the dma1 cycle),
    // mirroring VICE sprite_dma_cycle_0/_2 (viciisc/vicii-fetch.c:110-154).
    // The byte defaults to the Phi2 bus latch (last_bus_phi2, :112/:135); with
    // DMA active the real fetch replaces it only when the BA prefetch counter
    // has settled (!prefetch_cycles, :115/:138) while mc always advances; the
    // merge into the 24-bit data latch happens UNCONDITIONALLY (:129-130,
    // :152-153). The CPU is halted by the sprite BA window during real
    // fetches, so performing them within the same dispatch preserves bus
    // ordering.
    private void FetchSpriteDataPhi2(byte sprite, int sAccessIndex)
    {
        byte value = _vic.LastBusPhi2;
        if (_vic.IsSpriteDmaActive(sprite))
        {
            if (!_vic.IsPrefetchActive)
                value = ReadVicPhi1Ram(_vic.GetSpriteDataFetchAddress(sprite));
            _vic.LatchSpriteData(sprite, sAccessIndex, value);
        }
        else
        {
            _vic.MergeSpriteData(sprite, sAccessIndex, value);
        }
    }

    private byte ReadVicRefreshCounter()
    {
        var offset = _vic.ConsumeRefreshCounter();
        return ReadVicPhi1Ram((ushort)(0x3F00 + offset));
    }

    // PLAN-VICEPARITY-001 FR-VIC-FETCH AC-03: VICE cycle 15 (managed RasterX 14)
    // performs the last refresh fetch on Phi1 and the FIRST c-access on Phi2
    // (vicii-chip-model.c:139-141: Phi1(15) Refresh, Phi2(15) FetchC), one slot
    // ahead of the first g-access at cycle 16.
    private byte ReadVicRefreshAndFirstMatrix()
    {
        var data = ReadVicRefreshCounter();
        FetchVideoMatrixPhi2(cAccessOrdinal: 0);
        return data;
    }

    // PLAN-VICEPARITY-001 FR-VIC-FETCH AC-03: Phi1 g-access for the slot, then the
    // Phi2 c-access of the SAME cycle, which fills the vbuf/cbuf slot consumed by
    // the NEXT cycle's g-access (VICE cycle_phi1_fetch + vicii_fetch_matrix;
    // vicii-cycle.c:130-142,594-602). fetchSlot is the g ordinal (cycle - 15); the
    // trailing c ordinal is fetchSlot + 1 and the last c-access runs at ordinal 39
    // (VICE Phi2(54)), so the final g cycle (ordinal 39) has no trailing c-access.
    private byte ReadVicGfxDataOrIdle(byte fetchSlot)
    {
        byte data = _vic.IsGraphicsIdle
            ? ReadVicPhi1Ram(_vic.IdleGraphicsFetchAddress)
            : ReadVicPhi1Ram(_vic.ConsumeGraphicsFetchAddress());

        if (fetchSlot < 39)
            FetchVideoMatrixPhi2((byte)(fetchSlot + 1));

        return data;
    }

    // PLAN-VICEPARITY-001 FR-VIC-FETCH AC-03: the Phi2 matrix fetch, gated on the
    // per-cycle bad-line latch exactly like VICE (vicii-cycle.c:594-602
    // "if (vicii.bad_line && cycle_may_fetch_c(...)) vicii_fetch_matrix()").
    private void FetchVideoMatrixPhi2(byte cAccessOrdinal)
    {
        if (!_vic.IsMatrixFetchSlot(cAccessOrdinal))
            return;

        var slot = _vic.CurrentVideoMatrixSlot;
        if (_vic.IsPrefetchActive)
        {
            _vic.LatchVideoMatrixPrefetch(slot, ReadCpuProgramRamByte());
        }
        else
        {
            var vc = _vic.CurrentVideoMatrixCounter;
            var matrixByte = ReadVicPhi1Ram(_vic.CurrentVideoMatrixFetchAddress);
            _vic.LatchVideoMatrixFetch(slot, matrixByte, _colorRam[vc]);
        }
    }

    // PLAN-VICEPARITY-001 FR-VIC-FETCH AC-09: VICE is_char_rom equivalent
    // (viciisc/vicii-fetch.c:184-188) against the VIC Phi1 banking: the
    // $1000-$1FFF window resolves to character ROM in banks 0 and 2 (mirrors
    // ReadVicPhi1Ram's char-ROM dispatch).
    private bool IsVicPhi1CharRomAddress(ushort vicAddress)
    {
        var normalizedAddress = (ushort)(vicAddress & 0x3FFF);
        return normalizedAddress is >= 0x1000 and < 0x2000 && (_vicPhi1Bank is 0 or 2);
    }

    private byte ReadVicIdleGap() => ReadVicPhi1Ram(0x3FFF);

    private byte ReadCpuProgramRamByte()
    {
        if (_cpuPcReader is null)
            return _lastCpuBusValue;

        return _ram[_cpuPcReader()];
    }

    private byte LatchCpuBusValue(byte value)
    {
        _lastCpuBusValue = value;
        return value;
    }

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

    public ushort TranslateVicAddressForDiagnostics(ushort vicAddress) => TranslateVicAddress(vicAddress);

    public int VicBankForDiagnostics => _vicBank;

    private ushort TranslateVicAddress(ushort vicAddress)
    {
        return (ushort)((_vicBank << 14) | (vicAddress & 0x3FFF));
    }

    private void WriteIo(ushort address, byte value)
    {
        if (TryStoreGameSystemBankRegister(address))
            return;

        if (TryStoreMagicDeskBankRegister(address, value))
            return;

        if (TryStoreOceanBankRegister(address, value))
            return;

        if (TryStoreFc3BankRegister(address, value))
            return;

        if (TryStoreActionReplayBankRegister(address, value))
            return;

        if (TryStoreEasyFlashBankRegister(address, value))
            return;

        if (TryStoreSuperSnapshotBankRegister(address, value))
            return;

        if (TryStoreRRNetBankRegister(address, value))
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
            // PLAN-VICEPARITY-001 audit L5: the VIC light-pen input follows
            // CIA1 port B bit 4 (c64cia1.c cia1_internal_lightpen_check,
            // called from the PA and PB store paths :163/:176): after any
            // port or DDR store, the merged port-B line level (output
            // register, direction and the keyboard/joystick pulls, which the
            // CIA port read already combines) drives vicii_set_light_pen;
            // a low PB4 asserts the pen.
            if ((address & 0x0F) <= 0x03)
            {
                _vic.SetLightPen((_cia1.Peek(0xDC01) & 0x10) == 0);
            }

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

    /// <summary>
    /// Magic Desk (CRT type 19) bank-register store. Any write to $DE00-$DEFF
    /// loads the bank selector: data bits 0-6 = bank index modulo the image
    /// size, data bit 7 = disable (release ROML). Reads from $DE00 return 0,
    /// matching VICE c64cart/magicdesk.c behaviour. Returns true when the
    /// store is consumed by the mapper (so the caller does not pass the
    /// value through to the I/O fallback).
    /// </summary>
    private bool TryStoreMagicDeskBankRegister(ushort address, byte value)
    {
        if (_attachedCartridgeMappingMode != CartridgeMappingMode.MagicDesk ||
            address is < CartridgeIo1Start or > CartridgeIo1End)
        {
            return false;
        }

        var prevDisabled = _magicDeskDisabled;
        var prevBank = _magicDeskCartridgeBank;
        _magicDeskDisabled = (value & 0x80) != 0;
        var bankCount = _cartridgeImage is null
            ? 1
            : System.Math.Max(1, _cartridgeImage.Length / CartridgeBankSize);
        _magicDeskCartridgeBank = (value & 0x7F) % bankCount;
        if (prevDisabled != _magicDeskDisabled || prevBank != _magicDeskCartridgeBank)
            RebuildReadPageTable();
        return true;
    }

    /// <summary>
    /// Ocean (CRT type 5) bank-register store. Writes to $DE00-$DEFF select
    /// the active 8K ROML bank. The data byte's bits 0-5 are mod bank-count;
    /// no disable bit (ROML is always mapped while attached). Source: VICE
    /// src/c64/cart/ocean.c.
    /// </summary>
    private bool TryStoreOceanBankRegister(ushort address, byte value)
    {
        if (_attachedCartridgeMappingMode != CartridgeMappingMode.Ocean ||
            address is < CartridgeIo1Start or > CartridgeIo1End)
        {
            return false;
        }

        var prev = _oceanCartridgeBank;
        var bankCount = _cartridgeImage is null
            ? 1
            : System.Math.Max(1, _cartridgeImage.Length / CartridgeBankSize);
        _oceanCartridgeBank = (value & 0x3F) % bankCount;
        if (prev != _oceanCartridgeBank)
            RebuildReadPageTable();
        return true;
    }

    /// <summary>
    /// Final Cartridge III (CRT type 60) bank-register store at $DFFF.
    /// Data bits 0-1 = bank index (mod 4), bit 7 = hide (release ROML+ROMH).
    /// Bits 2-6 carry LED/NMI/freeze control and are accepted-but-ignored in
    /// this slice (left for follow-up FC-III deepening). Source: VICE
    /// src/c64/cart/finalIII.c.
    /// </summary>
    private bool TryStoreFc3BankRegister(ushort address, byte value)
    {
        if (_attachedCartridgeMappingMode != CartridgeMappingMode.FinalCartridgeIII ||
            address != Fc3BankRegisterAddress)
        {
            return false;
        }

        var prevHidden = _fc3Hidden;
        var prevBank = _fc3CartridgeBank;
        _fc3Hidden = (value & 0x80) != 0;
        _fc3CartridgeBank = value & 0x03;
        if (prevHidden != _fc3Hidden || prevBank != _fc3CartridgeBank)
            RebuildReadPageTable();
        return true;
    }

    /// <summary>
    /// Action Replay V4/V5 (CRT type 1) $DE00 store. Bits 0-3 select bank
    /// (mod 4); bit 7 hides the cartridge (release ROML). Bits 4-6 = LED /
    /// EXROM / GAME (deferred). Source: VICE src/c64/cart/actionreplay.c.
    /// </summary>
    private bool TryStoreActionReplayBankRegister(ushort address, byte value)
    {
        if (_attachedCartridgeMappingMode != CartridgeMappingMode.ActionReplay ||
            address is < CartridgeIo1Start or > CartridgeIo1End)
        {
            return false;
        }

        var prevHidden = _arHidden;
        var prevBank = _arCartridgeBank;
        _arHidden = (value & 0x80) != 0;
        _arCartridgeBank = value & 0x03;
        if (prevHidden != _arHidden || prevBank != _arCartridgeBank)
            RebuildReadPageTable();
        return true;
    }

    /// <summary>
    /// EasyFlash (CRT type 32) $DE00 bank-select. Bits 0-5 = bank mod
    /// bank-count. The $DE02 control register and flash writes are
    /// deferred to a follow-up EasyFlash slice.
    /// </summary>
    private bool TryStoreEasyFlashBankRegister(ushort address, byte value)
    {
        if (_attachedCartridgeMappingMode != CartridgeMappingMode.EasyFlash ||
            address != CartridgeIo1Start)
        {
            return false;
        }

        var prev = _easyFlashCartridgeBank;
        var bankCount = _cartridgeImage is null
            ? 1
            : System.Math.Max(1, _cartridgeImage.Length / CartridgeBankSize);
        _easyFlashCartridgeBank = (value & 0x3F) % bankCount;
        if (prev != _easyFlashCartridgeBank)
            RebuildReadPageTable();
        return true;
    }

    /// <summary>
    /// Super Snapshot V5 (CRT type 4) $DE00 store. Bits 2-4 select bank
    /// (mod 4); bit 1 hides the cartridge. Source: VICE src/c64/cart/
    /// supersnapshot.c. Freeze button deferred.
    /// </summary>
    private bool TryStoreSuperSnapshotBankRegister(ushort address, byte value)
    {
        if (_attachedCartridgeMappingMode != CartridgeMappingMode.SuperSnapshotV5 ||
            address is < CartridgeIo1Start or > CartridgeIo1End)
        {
            return false;
        }

        var prevHidden = _ss5Hidden;
        var prevBank = _ss5CartridgeBank;
        _ss5Hidden = (value & 0x02) != 0;
        _ss5CartridgeBank = (value >> 2) & 0x03;
        if (prevHidden != _ss5Hidden || prevBank != _ss5CartridgeBank)
            RebuildReadPageTable();
        return true;
    }

    /// <summary>
    /// RR-Net (CRT type 25) $DE00 store. Bits 0-3 select 16K bank (mod 4),
    /// bit 7 hides. The Ethernet I/O register window at $DE00-$DE0F is
    /// stubbed (writes here still update the bank register but Ethernet
    /// behaviour is not modelled). Source: VICE src/c64/cart/retroreplay.c.
    /// </summary>
    private bool TryStoreRRNetBankRegister(ushort address, byte value)
    {
        if (_attachedCartridgeMappingMode != CartridgeMappingMode.RRNet ||
            address is < CartridgeIo1Start or > CartridgeIo1End)
        {
            return false;
        }

        var prevHidden = _rrNetHidden;
        var prevBank = _rrNetCartridgeBank;
        _rrNetHidden = (value & 0x80) != 0;
        _rrNetCartridgeBank = value & 0x03;
        if (prevHidden != _rrNetHidden || prevBank != _rrNetCartridgeBank)
            RebuildReadPageTable();
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
            // Magic Desk images come in 32K (4 banks), 64K (8), or 128K (16);
            // VICE also accepts 256K and 512K for derivative carts. Each bank
            // is exactly 8K and the total must be a non-zero multiple.
            CartridgeMappingMode.MagicDesk =>
                imageLength > 0 &&
                imageLength % CartridgeBankSize == 0 &&
                imageLength / CartridgeBankSize is >= 4 and <= 64,
            // Ocean accepts the same bank-multiple shapes (4..64 banks of 8K).
            CartridgeMappingMode.Ocean =>
                imageLength > 0 &&
                imageLength % CartridgeBankSize == 0 &&
                imageLength / CartridgeBankSize is >= 4 and <= 64,
            // FC-III ships as a fixed 64K image (4 banks of 16K).
            CartridgeMappingMode.FinalCartridgeIII =>
                imageLength == CartridgeBankSize * 8,  // 4 banks * 16K = 64K
            // Action Replay ships as a fixed 32K image (4 banks of 8K).
            CartridgeMappingMode.ActionReplay =>
                imageLength == CartridgeBankSize * 4,
            // EasyFlash up to 1024K (128 banks of 8K); require at least 64K
            // (8 banks) since smaller images are unusual for EasyFlash.
            CartridgeMappingMode.EasyFlash =>
                imageLength > 0 &&
                imageLength % CartridgeBankSize == 0 &&
                imageLength / CartridgeBankSize is >= 8 and <= 128,
            // Super Snapshot V5 = fixed 64K (4 banks of 16K).
            CartridgeMappingMode.SuperSnapshotV5 =>
                imageLength == CartridgeBankSize * 8,
            // RR-Net base ships as 64K (4 banks of 16K).
            CartridgeMappingMode.RRNet =>
                imageLength == CartridgeBankSize * 8,
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

            offset = mappingMode switch
            {
                CartridgeMappingMode.GameSystem
                    => (_gameSystemCartridgeBank * CartridgeBankSize) + address - CartridgeRomLowStart,
                CartridgeMappingMode.MagicDesk
                    => (_magicDeskCartridgeBank * CartridgeBankSize) + address - CartridgeRomLowStart,
                CartridgeMappingMode.Ocean
                    => (_oceanCartridgeBank * CartridgeBankSize) + address - CartridgeRomLowStart,
                // FC-III: 16K banks (ROML+ROMH); ROML lives at offset (bank * 16K).
                CartridgeMappingMode.FinalCartridgeIII
                    => (_fc3CartridgeBank * CartridgeBankSize * 2) + address - CartridgeRomLowStart,
                // Action Replay: 8K ROML banks.
                CartridgeMappingMode.ActionReplay
                    => (_arCartridgeBank * CartridgeBankSize) + address - CartridgeRomLowStart,
                // EasyFlash: 8K ROML banks.
                CartridgeMappingMode.EasyFlash
                    => (_easyFlashCartridgeBank * CartridgeBankSize) + address - CartridgeRomLowStart,
                // Super Snapshot V5: 16K banks (ROML+ROMH); ROML at (bank * 16K).
                CartridgeMappingMode.SuperSnapshotV5
                    => (_ss5CartridgeBank * CartridgeBankSize * 2) + address - CartridgeRomLowStart,
                // RR-Net: 16K banks (ROML+ROMH); ROML at (bank * 16K).
                CartridgeMappingMode.RRNet
                    => (_rrNetCartridgeBank * CartridgeBankSize * 2) + address - CartridgeRomLowStart,
                _ => address - CartridgeRomLowStart,
            };
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

        if (mappingMode == CartridgeMappingMode.FinalCartridgeIII &&
            address is >= CartridgeStandardRomHighStart and <= CartridgeStandardRomHighEnd)
        {
            if (!IsRomHighVisible(mappingMode))
                return false;

            // FC-III ROMH offset = (bank * 16K) + 8K + (address - $A000).
            offset = (_fc3CartridgeBank * CartridgeBankSize * 2)
                   + CartridgeBankSize
                   + address - CartridgeStandardRomHighStart;
            return true;
        }

        if (mappingMode == CartridgeMappingMode.SuperSnapshotV5 &&
            address is >= CartridgeStandardRomHighStart and <= CartridgeStandardRomHighEnd)
        {
            if (!IsRomHighVisible(mappingMode))
                return false;

            offset = (_ss5CartridgeBank * CartridgeBankSize * 2)
                   + CartridgeBankSize
                   + address - CartridgeStandardRomHighStart;
            return true;
        }

        if (mappingMode == CartridgeMappingMode.RRNet &&
            address is >= CartridgeStandardRomHighStart and <= CartridgeStandardRomHighEnd)
        {
            if (!IsRomHighVisible(mappingMode))
                return false;

            offset = (_rrNetCartridgeBank * CartridgeBankSize * 2)
                   + CartridgeBankSize
                   + address - CartridgeStandardRomHighStart;
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
            CartridgeMappingMode.MagicDesk
                => _pla.Loram && _pla.Hiram && !_magicDeskDisabled,
            CartridgeMappingMode.Ocean
                => _pla.Loram && _pla.Hiram,
            CartridgeMappingMode.FinalCartridgeIII
                => _pla.Loram && _pla.Hiram && !_fc3Hidden,
            CartridgeMappingMode.ActionReplay
                => _pla.Loram && _pla.Hiram && !_arHidden,
            CartridgeMappingMode.EasyFlash
                => _pla.Loram && _pla.Hiram,
            CartridgeMappingMode.SuperSnapshotV5
                => _pla.Loram && _pla.Hiram && !_ss5Hidden,
            CartridgeMappingMode.RRNet
                => _pla.Loram && _pla.Hiram && !_rrNetHidden,
            _ => false
        };
    }

    private bool IsRomHighVisible(CartridgeMappingMode mappingMode)
    {
        return mappingMode switch
        {
            CartridgeMappingMode.Ultimax => true,
            CartridgeMappingMode.Standard16K => _pla.Hiram,
            CartridgeMappingMode.FinalCartridgeIII => _pla.Hiram && !_fc3Hidden,
            CartridgeMappingMode.SuperSnapshotV5 => _pla.Hiram && !_ss5Hidden,
            CartridgeMappingMode.RRNet => _pla.Hiram && !_rrNetHidden,
            _ => false
        };
    }
}
