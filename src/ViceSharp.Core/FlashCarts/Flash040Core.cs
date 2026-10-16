namespace ViceSharp.Core.FlashCarts;

/// <summary>
/// Managed port of VICE <c>flash040core.c</c> for AM29F040B
/// (<c>FLASH040_TYPE_B</c>), the type FE3 uses
/// (<c>finalexpansion.c</c> / <c>flash040core_init(..., FLASH040_TYPE_B, ...)</c>).
/// </summary>
/// <remarks>
/// Command FSM, byte program (AND with existing), autoselect IDs, chip/sector erase
/// data effects and erase alarm cycle counts match VICE TYPE_B
/// (timeout=50, sector=1_000_000, chip=8_000_000). Call <see cref="AdvanceCycles"/>
/// from the system clock (or tests) so erase completes after the VICE cycle budget.
/// </remarks>
public sealed class Flash040Core
{
    /// <summary>AM29F040 / 29F040B manufacturer ID.</summary>
    public const byte ManufacturerId = 0x01;

    /// <summary>AM29F040B device ID.</summary>
    public const byte DeviceId = 0xA4;

    public const int Size = 0x80000;
    public const int SectorSize = 0x10000;
    public const int SectorCount = Size / SectorSize;

    /// <summary>VICE TYPE_B <c>erase_sector_timeout_cycles</c>.</summary>
    public const long EraseSectorTimeoutCycles = 50;

    /// <summary>VICE TYPE_B <c>erase_sector_cycles</c>.</summary>
    public const long EraseSectorCycles = 1_000_000;

    /// <summary>VICE TYPE_B <c>erase_chip_cycles</c>.</summary>
    public const long EraseChipCycles = 8_000_000;

    // TYPE_B magic (flash040core.c flash_types[FLASH040_TYPE_B])
    private const int Magic1Addr = 0x555;
    private const int Magic2Addr = 0x2AA;
    private const int Magic1Mask = 0x7FF;
    private const int Magic2Mask = 0x7FF;
    private const int SectorMask = 0x70000;
    private const int SectorShift = 16;

    public enum State
    {
        Read = 0,
        Magic1,
        Magic2,
        Autoselect,
        ByteProgram,
        ByteProgramError,
        EraseMagic1,
        EraseMagic2,
        EraseSelect,
        ChipErase,
        SectorErase,
        SectorEraseTimeout,
        SectorEraseSuspend,
    }

    private readonly byte[] _data;
    private State _state = State.Read;
    private State _baseState = State.Read;
    private byte _programByte;
    private readonly byte[] _eraseMask = new byte[8];
    private byte _lastRead;
    private bool _dirty;
    private long _eraseCyclesRemaining;

    public Flash040Core(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length < Size)
            throw new ArgumentException($"Flash image must be at least {Size} bytes.", nameof(data));
        _data = data;
    }

    public State FlashState => _state;
    public bool Dirty => _dirty;
    public long EraseCyclesRemaining => _eraseCyclesRemaining;
    public void ClearDirty() => _dirty = false;

    public void Reset()
    {
        _state = State.Read;
        _baseState = State.Read;
        _programByte = 0;
        _eraseCyclesRemaining = 0;
        Array.Clear(_eraseMask);
    }

    /// <summary>
    /// Advance VICE-equivalent maincpu clocks for erase_alarm timing.
    /// No-op when not in an erase-busy state.
    /// </summary>
    public void AdvanceCycles(long cycles)
    {
        if (cycles <= 0 || _eraseCyclesRemaining <= 0)
            return;

        if (cycles >= _eraseCyclesRemaining)
        {
            var spent = _eraseCyclesRemaining;
            _eraseCyclesRemaining = 0;
            OnEraseAlarm();
            // If another erase segment was armed, consume remaining of this tick.
            var leftover = cycles - spent;
            if (leftover > 0 && _eraseCyclesRemaining > 0)
                AdvanceCycles(leftover);
        }
        else
        {
            _eraseCyclesRemaining -= cycles;
        }
    }

    public void Store(uint addr, byte value)
    {
        addr &= Size - 1;
        switch (_state)
        {
            case State.Read:
                if (IsMagic1(addr) && value == 0xAA)
                    _state = State.Magic1;
                break;

            case State.Magic1:
                _state = IsMagic2(addr) && value == 0x55 ? State.Magic2 : _baseState;
                break;

            case State.Magic2:
                if (IsMagic1(addr))
                {
                    switch (value)
                    {
                        case 0x90:
                            _state = State.Autoselect;
                            _baseState = State.Autoselect;
                            break;
                        case 0xF0:
                            _state = State.Read;
                            _baseState = State.Read;
                            break;
                        case 0xA0:
                            _state = State.ByteProgram;
                            break;
                        case 0x80:
                            _state = State.EraseMagic1;
                            break;
                        default:
                            _state = _baseState;
                            break;
                    }
                }
                else
                {
                    _state = _baseState;
                }
                break;

            case State.ByteProgram:
                if (ProgramByte(addr, value))
                    _state = _baseState;
                else
                    _state = State.ByteProgramError;
                break;

            case State.EraseMagic1:
                _state = IsMagic1(addr) && value == 0xAA ? State.EraseMagic2 : _baseState;
                break;

            case State.EraseMagic2:
                _state = IsMagic2(addr) && value == 0x55 ? State.EraseSelect : _baseState;
                break;

            case State.EraseSelect:
                if (IsMagic1(addr) && value == 0x10)
                {
                    _state = State.ChipErase;
                    _programByte = 0;
                    _eraseCyclesRemaining = EraseChipCycles;
                }
                else if (value == 0x30)
                {
                    AddSectorToEraseMask(addr);
                    _state = State.SectorEraseTimeout;
                    _programByte = 0;
                    _eraseCyclesRemaining = EraseSectorTimeoutCycles;
                }
                else
                {
                    _state = _baseState;
                }
                break;

            case State.SectorEraseTimeout:
                // VICE: only another 0x30 extends the multi-sector mask.
                // Any other write cancels the pending erase and unsets its alarm.
                if (value == 0x30)
                    AddSectorToEraseMask(addr);
                else
                    CancelEraseToRead();
                break;

            case State.SectorErase:
                // VICE flash040core.c suspends the active sector alarm on 0xB0.
                if (value == 0xB0)
                {
                    _state = State.SectorEraseSuspend;
                    _eraseCyclesRemaining = 0;
                }
                break;

            case State.SectorEraseSuspend:
                // VICE resumes with a fresh full sector erase cycle budget.
                if (value == 0x30)
                {
                    _state = State.SectorErase;
                    _eraseCyclesRemaining = EraseSectorCycles;
                }
                break;

            case State.ChipErase:
                // VICE ignores writes while chip erase is active.
                break;

            case State.ByteProgramError:
            case State.Autoselect:
                if (IsMagic1(addr) && value == 0xAA)
                    _state = State.Magic1;
                if (value == 0xF0)
                {
                    _state = State.Read;
                    _baseState = State.Read;
                }
                break;
        }
    }

    public byte Read(uint addr)
    {
        addr &= Size - 1;
        byte value;
        switch (_state)
        {
            case State.Autoselect:
                if ((addr & 0xFF) == 0)
                    value = ManufacturerId;
                else if ((addr & 0xFF) == 1)
                    value = DeviceId;
                else if ((addr & 0xFF) == 2)
                    value = 0;
                else
                    value = _data[addr];
                break;

            case State.ByteProgramError:
                // DQ7 inverse of program byte; DQ5 timeout; simplified DQ6.
                value = (byte)(((_programByte ^ 0x80) & 0x80) | (1 << 5));
                break;

            case State.ChipErase:
            case State.SectorErase:
            case State.SectorEraseTimeout:
            case State.SectorEraseSuspend:
                // VICE flash_read_status: toggle bit + DQ3 sector timer when not in timeout.
                value = _programByte;
                _programByte ^= 0x40;
                if (_state != State.SectorEraseTimeout)
                    value = (byte)(value | 0x08);
                break;

            default:
                value = _data[addr];
                break;
        }

        _lastRead = value;
        return value;
    }

    public byte Peek(uint addr) => _data[addr & (Size - 1)];

    private void OnEraseAlarm()
    {
        switch (_state)
        {
            case State.SectorEraseTimeout:
                _state = State.SectorErase;
                _eraseCyclesRemaining = EraseSectorCycles;
                break;

            case State.SectorErase:
                EraseOneMarkedSector();
                if (AnyEraseMask())
                    _eraseCyclesRemaining = EraseSectorCycles;
                else
                    _state = _baseState;
                break;

            case State.ChipErase:
                EraseChip();
                _state = _baseState;
                break;
        }
    }

    private void CancelEraseToRead()
    {
        _state = State.Read;
        _baseState = State.Read;
        _eraseCyclesRemaining = 0;
        Array.Clear(_eraseMask);
    }

    private static bool IsMagic1(uint addr) => (addr & Magic1Mask) == Magic1Addr;
    private static bool IsMagic2(uint addr) => (addr & Magic2Mask) == Magic2Addr;

    private bool ProgramByte(uint addr, byte value)
    {
        var old = _data[addr];
        var next = (byte)(old & value);
        _programByte = value;
        _data[addr] = next;
        _dirty = true;
        return next == value;
    }

    private void EraseChip()
    {
        Array.Fill(_data, (byte)0xFF, 0, Size);
        _dirty = true;
        _programByte = 0;
    }

    private void AddSectorToEraseMask(uint addr)
    {
        var sector = (int)((addr & SectorMask) >> SectorShift);
        if ((uint)sector >= SectorCount)
            return;
        _eraseMask[sector >> 3] |= (byte)(1 << (sector & 7));
    }

    private bool AnyEraseMask()
    {
        for (var i = 0; i < _eraseMask.Length; i++)
        {
            if (_eraseMask[i] != 0)
                return true;
        }

        return false;
    }

    private void EraseOneMarkedSector()
    {
        for (var i = 0; i < SectorCount; i++)
        {
            var m = (byte)(1 << (i & 7));
            if ((_eraseMask[i >> 3] & m) == 0)
                continue;
            Array.Fill(_data, (byte)0xFF, i * SectorSize, SectorSize);
            _dirty = true;
            _eraseMask[i >> 3] &= (byte)~m;
            _programByte = 0;
            return;
        }
    }
}
