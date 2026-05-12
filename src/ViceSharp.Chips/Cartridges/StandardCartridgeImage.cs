using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Cartridges;

/// <summary>
/// Deterministic raw standard C64 cartridge ROM image.
/// </summary>
public sealed class StandardCartridgeImage : IAddressSpace
{
    public const int RomBankSize = 0x2000;
    public const ushort RomLowStart = 0x8000;
    public const ushort RomLowEnd = 0x9FFF;
    public const ushort RomHighStart = 0xA000;
    public const ushort RomHighEnd = 0xBFFF;

    private readonly byte[] _image;

    private StandardCartridgeImage(byte[] image, StandardCartridgeSize size)
    {
        _image = image;
        Size = size;
    }

    public DeviceId Id => new DeviceId(0x00020001);
    public string Name => $"Standard {ImageSizeBytes / 1024}K Cartridge";
    public StandardCartridgeSize Size { get; }
    public int ImageSizeBytes => _image.Length;
    public bool ExromLineAsserted => true;
    public bool GameLineAsserted => Size == StandardCartridgeSize.Rom16K;

    public static StandardCartridgeImage FromBytes(ReadOnlySpan<byte> image)
    {
        var size = image.Length switch
        {
            RomBankSize => StandardCartridgeSize.Rom8K,
            RomBankSize * 2 => StandardCartridgeSize.Rom16K,
            _ => throw new ArgumentException("Standard cartridge images must be exactly 8K or 16K.", nameof(image)),
        };

        return new StandardCartridgeImage(image.ToArray(), size);
    }

    public byte Read(ushort address)
    {
        if (!TryGetImageOffset(address, out var offset))
            return 0xFF;

        return _image[offset];
    }

    public byte Peek(ushort address) => Read(address);

    public void Write(ushort address, byte value)
    {
        // Standard cartridge ROM is read-only; writes are intentionally ignored.
    }

    public bool HandlesAddress(ushort address) => TryGetImageOffset(address, out _);

    public void Reset()
    {
    }

    private bool TryGetImageOffset(ushort address, out int offset)
    {
        if (address is >= RomLowStart and <= RomLowEnd)
        {
            offset = address - RomLowStart;
            return true;
        }

        if (Size == StandardCartridgeSize.Rom16K && address is >= RomHighStart and <= RomHighEnd)
        {
            offset = RomBankSize + address - RomHighStart;
            return true;
        }

        offset = 0;
        return false;
    }
}
