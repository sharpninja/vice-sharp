using ViceSharp.Abstractions;

namespace ViceSharp.Core.Vic20;

/// <summary>
/// VIC-20 Ultimem (VICE ultimem.c) MVP: register file at $9C00+, banked BLK windows.
/// </summary>
public sealed class UltimemCartridge : IAddressSpace
{
    public const int MaxImageSize = 0x1000000; // up to 16MB in VICE; we accept up to 1MB for MVP
    public const int DefaultImageSize = 0x100000; // 1MB

    private readonly byte[] _image;
    private readonly byte[] _registers = new byte[16];
    private bool _imageDirty;

    public UltimemCartridge(ReadOnlySpan<byte> image)
    {
        var size = Math.Max(DefaultImageSize, image.Length);
        if (size > MaxImageSize)
            throw new ArgumentException("Ultimem image too large.", nameof(image));
        _image = new byte[size];
        if (!image.IsEmpty)
            image[..Math.Min(image.Length, size)].CopyTo(_image);
        Id = new DeviceId(0x0C11);
        PowerUp();
    }

    public DeviceId Id { get; }
    public string Name => "Ultimem";
    public bool ImageDirty => _imageDirty;
    public bool WriteBack { get; set; }

    public void PowerUp()
    {
        Array.Fill(_registers, (byte)0xFF);
        // Enabled BLK5 ROM-ish default bank 0
        _registers[0] = 0x00;
        _registers[1] = 0x00;
        _registers[2] = 0x00; // cfg
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
            case "blk5-rom":
                PowerUp();
                _registers[2] = 0x03; // BLK5 ROM enabled (2 bits * block)
                break;
            case "ram-all":
                PowerUp();
                _registers[2] = 0xFF;
                break;
            default:
                throw new ArgumentException($"Unknown Ultimem preset '{presetId}'.", nameof(presetId));
        }
    }

    public bool HandlesAddress(ushort address)
        => address is (>= 0x9C00 and <= 0x9FFF)
            or (>= 0x2000 and <= 0x3FFF)
            or (>= 0x4000 and <= 0x7FFF)
            or (>= 0xA000 and <= 0xBFFF)
            or (>= 0x0400 and <= 0x0FFF);

    public byte Read(ushort address)
    {
        if (address is >= 0x9C00 and <= 0x9FFF)
            return _registers[address & 0x0F];
        if (TryMap(address, out var offset))
            return _image[offset];
        return 0xFF;
    }

    public byte Peek(ushort address) => Read(address);

    public void Write(ushort address, byte value)
    {
        if (address is >= 0x9C00 and <= 0x9FFF)
        {
            _registers[address & 0x0F] = value;
            return;
        }

        if (TryMap(address, out var offset))
        {
            _image[offset] = value;
            _imageDirty = true;
        }
    }

    public byte[] GetImage() => _image.ToArray();
    public void ClearDirty() => _imageDirty = false;

    private bool TryMap(ushort address, out int offset)
    {
        // Bank select: registers 6+ pair per block (simplified from VICE CART_BLK_ADDR).
        int bankLo;
        int local;
        if (address is >= 0x0400 and <= 0x0FFF)
        {
            bankLo = _registers[6];
            local = address - 0x0400;
        }
        else if (address is >= 0x2000 and <= 0x3FFF)
        {
            bankLo = _registers[8];
            local = address - 0x2000;
        }
        else if (address is >= 0x4000 and <= 0x5FFF)
        {
            bankLo = _registers[10];
            local = address - 0x4000;
        }
        else if (address is >= 0x6000 and <= 0x7FFF)
        {
            bankLo = _registers[12];
            local = address - 0x6000;
        }
        else if (address is >= 0xA000 and <= 0xBFFF)
        {
            bankLo = _registers[14];
            local = address - 0xA000;
        }
        else
        {
            offset = 0;
            return false;
        }

        offset = ((bankLo & 0x7F) * 0x2000) + local;
        return offset >= 0 && offset < _image.Length;
    }
}
