namespace ViceSharp.Core.FlashCarts;

/// <summary>
/// Managed port of VICE <c>flash040core.c</c> for AM29F040B
/// (<c>FLASH040_TYPE_B</c>), the type FE3 uses
/// (<c>finalexpansion.c</c> / <c>flash040core_init(..., FLASH040_TYPE_B, ...)</c>).
/// </summary>
/// <remarks>
/// Command FSM, byte program (AND with existing), autoselect IDs, chip/sector erase
/// data effects match VICE. Erase <em>latency</em> is instant (no maincpu alarm
/// cycles): Partial vs multi-second VICE erase_alarm; status returns to READ
/// immediately after erase command completes.
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

    public Flash040Core(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length < Size)
            throw new ArgumentException($"Flash image must be at least {Size} bytes.", nameof(data));
        _data = data;
    }

    public State FlashState => _state;
    public bool Dirty => _dirty;
    public void ClearDirty() => _dirty = false;

    public void Reset()
    {
        _state = State.Read;
        _baseState = State.Read;
        _programByte = 0;
        Array.Clear(_eraseMask);
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
                    // Instant chip erase (Partial: no multi-second alarm).
                    EraseChip();
                    _state = _baseState;
                }
                else if (value == 0x30)
                {
                    AddSectorToEraseMask(addr);
                    // Instant sector erase for all marked sectors.
                    FlushSectorErase();
                    _state = _baseState;
                }
                else
                {
                    _state = _baseState;
                }
                break;

            case State.SectorEraseTimeout:
            case State.SectorErase:
            case State.SectorEraseSuspend:
            case State.ChipErase:
                // Instant-path residual; treat further writes as reset to base.
                if (value == 0xF0)
                {
                    _state = State.Read;
                    _baseState = State.Read;
                    Array.Clear(_eraseMask);
                }
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
                value = _programByte;
                _programByte ^= 0x40;
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

    private void FlushSectorErase()
    {
        for (var i = 0; i < SectorCount; i++)
        {
            var m = (byte)(1 << (i & 7));
            if ((_eraseMask[i >> 3] & m) == 0)
                continue;
            Array.Fill(_data, (byte)0xFF, i * SectorSize, SectorSize);
            _dirty = true;
            _eraseMask[i >> 3] &= (byte)~m;
        }
        _programByte = 0;
    }
}
