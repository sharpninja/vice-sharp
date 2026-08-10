using ViceSharp.Abstractions;

namespace ViceSharp.Core.Vic20;

/// <summary>
/// VIC-20 Final Expansion 3 (VICE finalexpansion.c v3.2) MVP.
/// 512K SRAM + 512K flash; REGA $9C02 / REGB $9C03; RAM modes for BLK0/1/2/3/5.
/// </summary>
/// <remarks>
/// Where diverged: full flash040 program path deferred; START/FLASH menu boot
/// needs user flash image. Realign: REGA/REGB bit fields and RAM1/SUPER_RAM/ROM_RAM
/// maps match VICE tables for host RAM Manager presets.
/// </remarks>
public sealed class FinalExpansion3Cartridge : IAddressSpace
{
    public const int FlashSize = 0x80000;
    public const int SramSize = 0x80000;

    public const byte RegABankMask = 0x0F;
    public const byte RegAModeMask = 0xE0;
    public const byte ModeStart = 0x00;
    public const byte ModeFlash = 0x20;
    public const byte ModeSuperRom = 0x40;
    public const byte ModeRomRam = 0x60;
    public const byte ModeRam1 = 0x80;
    public const byte ModeSuperRam = 0xA0;
    public const byte ModeRam2 = 0xC0;

    public const byte RegABlk0Ro = 0x01;
    public const byte RegABlk1Sel = 0x02;
    public const byte RegABlk2Sel = 0x04;
    public const byte RegABlk3Sel = 0x08;
    public const byte RegABlk5Sel = 0x10;

    public const byte RegBBlk0Off = 0x01;
    public const byte RegBBlk1Off = 0x02;
    public const byte RegBBlk2Off = 0x04;
    public const byte RegBBlk3Off = 0x08;
    public const byte RegBBlk5Off = 0x10;

    private readonly byte[] _flash;
    private readonly byte[] _sram;
    private byte _registerA;
    private byte _registerB;
    private bool _flashDirty;

    public FinalExpansion3Cartridge(ReadOnlySpan<byte> flashImage)
    {
        _flash = new byte[FlashSize];
        _sram = new byte[SramSize];
        var copy = Math.Min(flashImage.Length, FlashSize);
        if (copy > 0)
            flashImage[..copy].CopyTo(_flash);
        Id = new DeviceId(0x0C10);
        PowerUp();
    }

    public DeviceId Id { get; }
    public string Name => "Final Expansion 3";
    public byte RegisterA => _registerA;
    public byte RegisterB => _registerB;
    public bool FlashDirty => _flashDirty;
    public bool WriteBack { get; set; }

    public void PowerUp()
    {
        // VICE powerup: START mode, bank 0, registers enabled.
        _registerA = ModeStart;
        _registerB = 0;
    }

    public void Reset() => PowerUp();

    public void ApplyConfigPreset(string presetId)
    {
        switch (presetId.Trim().ToLowerInvariant())
        {
            case "start":
            case "menu":
                _registerA = ModeStart;
                _registerB = 0;
                break;
            case "flash":
                // VICE FE3 FLASH mode (REGA mode bits): bankable flash window for programming/menu.
                _registerA = ModeFlash;
                _registerB = 0;
                break;
            case "super-rom":
            case "superrom":
                _registerA = ModeSuperRom;
                _registerB = 0;
                break;
            case "rom-ram":
            case "romram":
                _registerA = ModeRomRam;
                _registerB = 0;
                break;
            case "ram2":
                _registerA = ModeRam2;
                _registerB = 0;
                break;
            case "none":
            case "unexpanded":
                _registerA = ModeRam1;
                _registerB = (byte)(RegBBlk0Off | RegBBlk1Off | RegBBlk2Off | RegBBlk3Off | RegBBlk5Off);
                break;
            case "3k":
                _registerA = (byte)(ModeRam1 | RegABlk0Ro);
                _registerB = (byte)(RegBBlk1Off | RegBBlk2Off | RegBBlk3Off | RegBBlk5Off);
                break;
            case "8k":
                _registerA = (byte)(ModeRam1 | RegABlk1Sel);
                _registerB = (byte)(RegBBlk0Off | RegBBlk2Off | RegBBlk3Off | RegBBlk5Off);
                break;
            case "16k":
                _registerA = (byte)(ModeRam1 | RegABlk1Sel | RegABlk2Sel);
                _registerB = (byte)(RegBBlk0Off | RegBBlk3Off | RegBBlk5Off);
                break;
            case "24k":
                _registerA = (byte)(ModeRam1 | RegABlk1Sel | RegABlk2Sel | RegABlk3Sel);
                _registerB = (byte)(RegBBlk0Off | RegBBlk5Off);
                break;
            case "full":
            case "all":
            case "32k":
                _registerA = (byte)(ModeRam1 | RegABlk0Ro | RegABlk1Sel | RegABlk2Sel | RegABlk3Sel | RegABlk5Sel);
                _registerB = 0;
                break;
            case "super-ram":
            case "superram":
                _registerA = (byte)(ModeSuperRam | RegABlk1Sel | RegABlk2Sel | RegABlk3Sel | RegABlk5Sel);
                _registerB = 0;
                break;
            default:
                throw new ArgumentException($"Unknown FE3 preset '{presetId}'.", nameof(presetId));
        }
    }

    public bool HandlesAddress(ushort address)
    {
        if (address is >= 0x9C00 and <= 0x9FFF)
            return true;
        return IsMappedWindow(address);
    }

    public byte Read(ushort address)
    {
        if (address is >= 0x9C00 and <= 0x9FFF)
            return ReadIo3(address);
        if (TryMap(address, isWrite: false, out var offset, out var useFlash))
            return useFlash ? _flash[offset] : _sram[offset];
        return 0xFF;
    }

    public byte Peek(ushort address) => Read(address);

    public void Write(ushort address, byte value)
    {
        if (address is >= 0x9C00 and <= 0x9FFF)
        {
            WriteIo3(address, value);
            return;
        }

        if (!TryMap(address, isWrite: true, out var offset, out var useFlash))
            return;

        if (useFlash)
        {
            _flash[offset] = value;
            _flashDirty = true;
        }
        else
        {
            _sram[offset] = value;
        }
    }

    public byte[] GetFlashImage() => _flash.ToArray();

    public void ClearFlashDirty() => _flashDirty = false;

    private byte ReadIo3(ushort address)
    {
        return (address & 0x03) switch
        {
            0x02 => _registerA,
            0x03 => _registerB,
            _ => 0xFF,
        };
    }

    private void WriteIo3(ushort address, byte value)
    {
        switch (address & 0x03)
        {
            case 0x02:
                _registerA = value;
                break;
            case 0x03:
                _registerB = value;
                break;
        }
    }

    private bool IsMappedWindow(ushort address)
    {
        return address switch
        {
            >= 0x0400 and <= 0x0FFF => IsBlockEnabled(0),
            >= 0x2000 and <= 0x3FFF => IsBlockEnabled(1),
            >= 0x4000 and <= 0x5FFF => IsBlockEnabled(2),
            >= 0x6000 and <= 0x7FFF => IsBlockEnabled(3),
            >= 0xA000 and <= 0xBFFF => IsBlockEnabled(5),
            _ => false,
        };
    }

    private bool IsBlockEnabled(int block)
    {
        var mode = (byte)(_registerA & RegAModeMask);
        if (mode is ModeStart or ModeFlash or ModeSuperRom)
        {
            // START/FLASH/SUPER_ROM: BLK5 typically ROM; expose BLK5 for menu/rom reads.
            return block == 5 || mode == ModeSuperRom;
        }

        var offMask = block switch
        {
            0 => RegBBlk0Off,
            1 => RegBBlk1Off,
            2 => RegBBlk2Off,
            3 => RegBBlk3Off,
            5 => RegBBlk5Off,
            _ => (byte)0,
        };
        if ((_registerB & offMask) != 0)
            return false;

        // RAM1/RAM2/ROM_RAM/SUPER_RAM: use select bits (BLK0 uses RO bit as present).
        var selMask = block switch
        {
            0 => RegABlk0Ro,
            1 => RegABlk1Sel,
            2 => RegABlk2Sel,
            3 => RegABlk3Sel,
            5 => RegABlk5Sel,
            _ => (byte)0,
        };
        // If no select bits set at all in RAM1, treat as none selected.
        if (mode is ModeRam1 or ModeRam2 or ModeRomRam)
            return (_registerA & selMask) != 0 || (block == 0 && (_registerA & RegABlk0Ro) != 0 && selMask == RegABlk0Ro);

        if (mode == ModeSuperRam)
            return true;

        return false;
    }

    private bool TryMap(ushort address, bool isWrite, out int offset, out bool useFlash)
    {
        offset = 0;
        useFlash = false;
        if (!IsMappedWindow(address))
            return false;

        var mode = (byte)(_registerA & RegAModeMask);
        var bank = _registerA & RegABankMask;
        useFlash = mode is ModeStart or ModeFlash or ModeSuperRom or ModeRomRam;

        // VICE RAM map (simplified): BLK windows into 8K slices; SUPER_RAM banks N*64K.
        int windowBase = address switch
        {
            >= 0x0400 and <= 0x0FFF => 0x0400,
            >= 0x2000 and <= 0x3FFF => 0x0000,
            >= 0x4000 and <= 0x5FFF => 0x2000,
            >= 0x6000 and <= 0x7FFF => 0x4000,
            >= 0xA000 and <= 0xBFFF => 0x6000,
            _ => 0,
        };

        var local = address switch
        {
            >= 0x0400 and <= 0x0FFF => address - 0x0400,
            >= 0x2000 and <= 0x3FFF => address - 0x2000,
            >= 0x4000 and <= 0x5FFF => address - 0x4000,
            >= 0x6000 and <= 0x7FFF => address - 0x6000,
            >= 0xA000 and <= 0xBFFF => address - 0xA000,
            _ => 0,
        };

        if (mode == ModeSuperRam)
        {
            offset = (bank * 0x10000) + windowBase + local;
            useFlash = false;
        }
        else if (mode is ModeRam1 or ModeRam2)
        {
            offset = windowBase + local;
            if (address is >= 0x0400 and <= 0x0FFF)
                offset = 0x0400 + local;
            useFlash = false;
        }
        else
        {
            // ROM modes: banked flash
            offset = (bank * 0x8000) + windowBase + local;
            useFlash = true;
        }

        var size = useFlash ? FlashSize : SramSize;
        if (offset < 0 || offset >= size)
            return false;

        // BLK0 RO in RAM1: ignore writes
        if (isWrite && useFlash == false && address is >= 0x0400 and <= 0x0FFF
            && (_registerA & RegABlk0Ro) != 0 && mode is ModeRam1 or ModeRomRam)
        {
            // still allow if not RO-only intent; FE3 RegA bit0 = BLK0 RO in some modes
            // For simplicity allow writes unless ModeRomRam on BLK0.
            if (mode == ModeRomRam)
                return false;
        }

        return true;
    }
}
