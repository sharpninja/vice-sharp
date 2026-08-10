using ViceSharp.Abstractions;

namespace ViceSharp.Core.Vic20;

/// <summary>
/// VIC-20 Mega-Cart (VICE megacart.c) MVP: banked ROM + 8K NVRAM window.
/// </summary>
public sealed class MegaCartCartridge : IAddressSpace
{
    public const int LowRomSize = 0x100000;  // 1MB low
    public const int NvramSize = 0x2000;

    private readonly byte[] _rom;
    private readonly byte[] _nvram;
    private byte _bankLow = 0x7F;
    private byte _bankHigh = 0x7F;
    private bool _oe = true;
    private bool _nvramDirty;
    private bool _nvramEnabled;

    public MegaCartCartridge(ReadOnlySpan<byte> romImage, ReadOnlySpan<byte> nvram = default)
    {
        _rom = new byte[Math.Max(LowRomSize, romImage.Length)];
        if (!romImage.IsEmpty)
            romImage[..Math.Min(romImage.Length, _rom.Length)].CopyTo(_rom);
        _nvram = new byte[NvramSize];
        if (!nvram.IsEmpty)
            nvram[..Math.Min(nvram.Length, NvramSize)].CopyTo(_nvram);
        Id = new DeviceId(0x0C12);
        PowerUp();
    }

    public DeviceId Id { get; }
    public string Name => "Mega-Cart";
    public bool NvramDirty => _nvramDirty;
    public bool WriteBack { get; set; }
    public byte BankLow => _bankLow;
    public byte BankHigh => _bankHigh;

    public void PowerUp()
    {
        _bankLow = 0x7F;
        _bankHigh = 0x7F;
        _oe = true;
        _nvramEnabled = false;
    }

    public void Reset() => PowerUp();

    public void ApplyConfigPreset(string presetId)
    {
        switch (presetId.Trim().ToLowerInvariant())
        {
            case "default":
            case "start":
                PowerUp();
                break;
            case "bank0":
                _oe = true;
                _bankLow = 0x00;
                _bankHigh = 0x00;
                break;
            case "nvram":
                _nvramEnabled = true;
                break;
            default:
                throw new ArgumentException($"Unknown Mega-Cart preset '{presetId}'.", nameof(presetId));
        }
    }

    public bool HandlesAddress(ushort address)
        => address is (>= 0x9C00 and <= 0x9FFF)
            or (>= 0x9800 and <= 0x9BFF)
            or (>= 0x2000 and <= 0x7FFF)
            or (>= 0xA000 and <= 0xBFFF)
            or (>= 0x0400 and <= 0x0FFF);

    public byte Read(ushort address)
    {
        if (address is >= 0x9C00 and <= 0x9FFF)
            return ReadIo3(address);
        if (address is >= 0x9800 and <= 0x9BFF)
            return _bankLow; // IO2 mirrors bank low for tests
        if (_nvramEnabled && address is >= 0x0400 and <= 0x0FFF)
            return _nvram[address & 0x0FFF];
        if (TryMapRom(address, out var offset))
            return _rom[offset];
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

        if (address is >= 0x9800 and <= 0x9BFF)
        {
            _bankLow = value;
            return;
        }

        if (_nvramEnabled && address is >= 0x0400 and <= 0x0FFF)
        {
            _nvram[address & 0x0FFF] = value;
            _nvramDirty = true;
        }
    }

    public byte[] GetRomImage() => _rom.ToArray();
    public byte[] GetNvram() => _nvram.ToArray();
    public void ClearNvramDirty() => _nvramDirty = false;

    private byte ReadIo3(ushort address)
    {
        return (address & 1) == 0 ? _bankLow : _bankHigh;
    }

    private void WriteIo3(ushort address, byte value)
    {
        if ((address & 1) == 0)
            _bankLow = value;
        else
            _bankHigh = value;
        _oe = true;
        // bit7 of bank = RAM enable style (VICE ram_low_en)
        _nvramEnabled = (_bankLow & 0x80) != 0;
    }

    private bool TryMapRom(ushort address, out int offset)
    {
        var bank = _oe ? (_bankLow & 0x7F) : 0x7F;
        int local;
        if (address is >= 0x2000 and <= 0x3FFF)
            local = address - 0x2000;
        else if (address is >= 0x4000 and <= 0x5FFF)
            local = address - 0x4000 + 0x2000;
        else if (address is >= 0x6000 and <= 0x7FFF)
            local = address - 0x6000 + 0x4000;
        else if (address is >= 0xA000 and <= 0xBFFF)
            local = address - 0xA000 + 0x6000;
        else
        {
            offset = 0;
            return false;
        }

        offset = (bank * 0x2000) + (local & 0x1FFF);
        return offset >= 0 && offset < _rom.Length;
    }
}
