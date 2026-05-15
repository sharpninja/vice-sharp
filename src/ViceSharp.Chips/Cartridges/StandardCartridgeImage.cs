using System.Buffers.Binary;
using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Cartridges;

/// <summary>
/// Deterministic standard C64 cartridge ROM image.
/// </summary>
public sealed class StandardCartridgeImage : IAddressSpace
{
    public const int RomBankSize = 0x2000;
    public const int Rom16KSize = RomBankSize * 2;
    public const int GameSystemRomSize = RomBankSize * 64;
    public const int CrtHeaderSize = 0x40;
    public const int CrtChipHeaderSize = 0x10;
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
        return HasCrtSignature(image)
            ? FromCrtBytes(image)
            : FromRawBytes(image);
    }

    public byte[] ToArray() => _image.ToArray();

    private static StandardCartridgeImage FromRawBytes(ReadOnlySpan<byte> image)
    {
        var size = image.Length switch
        {
            RomBankSize => StandardCartridgeSize.Rom8K,
            Rom16KSize => StandardCartridgeSize.Rom16K,
            _ => throw new ArgumentException("Standard cartridge images must be exactly 8K or 16K.", nameof(image)),
        };

        return new StandardCartridgeImage(image.ToArray(), size);
    }

    private static StandardCartridgeImage FromCrtBytes(ReadOnlySpan<byte> image)
    {
        if (image.Length < CrtHeaderSize)
            throw new ArgumentException("CRT cartridge images must include a complete 64-byte header.", nameof(image));

        var headerLength = ReadInt32(image[0x10..0x14]);
        if (headerLength < CrtHeaderSize || headerLength > image.Length)
            throw new ArgumentException("CRT cartridge header length is invalid.", nameof(image));

        var cartridgeType = BinaryPrimitives.ReadUInt16BigEndian(image[0x16..0x18]);
        if (cartridgeType != 0)
            throw new ArgumentException("Only standard generic C64 CRT cartridge type 0 is supported.", nameof(image));

        var rom = new byte[Rom16KSize];
        var hasLow = false;
        var hasHigh = false;
        var offset = headerLength;
        while (offset < image.Length)
        {
            if (image.Length - offset < CrtChipHeaderSize)
                throw new ArgumentException("CRT cartridge CHIP packet is truncated.", nameof(image));

            var chip = image[offset..];
            if (!chip[..4].SequenceEqual("CHIP"u8))
                throw new ArgumentException("CRT cartridge contains a packet without a CHIP signature.", nameof(image));

            var packetLength = ReadInt32(chip[0x04..0x08]);
            if (packetLength < CrtChipHeaderSize || offset + packetLength > image.Length)
                throw new ArgumentException("CRT cartridge CHIP packet length is invalid.", nameof(image));

            var chipType = BinaryPrimitives.ReadUInt16BigEndian(chip[0x08..0x0A]);
            var bank = BinaryPrimitives.ReadUInt16BigEndian(chip[0x0A..0x0C]);
            var loadAddress = BinaryPrimitives.ReadUInt16BigEndian(chip[0x0C..0x0E]);
            var romSize = BinaryPrimitives.ReadUInt16BigEndian(chip[0x0E..0x10]);
            if (chipType != 0 || bank != 0)
                throw new ArgumentException("Only bank 0 ROM CHIP packets are supported for standard CRT cartridges.", nameof(image));

            if (packetLength < CrtChipHeaderSize + romSize)
                throw new ArgumentException("CRT cartridge CHIP packet payload is truncated.", nameof(image));

            var chipData = chip.Slice(CrtChipHeaderSize, romSize);
            if (loadAddress == RomLowStart && romSize == RomBankSize)
            {
                chipData.CopyTo(rom);
                hasLow = true;
            }
            else if (loadAddress == RomLowStart && romSize == Rom16KSize)
            {
                chipData.CopyTo(rom);
                hasLow = true;
                hasHigh = true;
            }
            else if (loadAddress == RomHighStart && romSize == RomBankSize)
            {
                chipData.CopyTo(rom.AsSpan(RomBankSize));
                hasHigh = true;
            }
            else
            {
                throw new ArgumentException("Standard CRT cartridges must contain 8K ROM data at $8000 and optional 8K ROM data at $A000.", nameof(image));
            }

            offset += packetLength;
        }

        if (!hasLow)
            throw new ArgumentException("Standard CRT cartridges must contain ROML data at $8000.", nameof(image));

        return hasHigh
            ? FromRawBytes(rom)
            : FromRawBytes(rom.AsSpan(0, RomBankSize));
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

    private static bool HasCrtSignature(ReadOnlySpan<byte> image)
        => image.Length >= 16 && image[..16].SequenceEqual("C64 CARTRIDGE   "u8);

    private static int ReadInt32(ReadOnlySpan<byte> value)
    {
        var result = BinaryPrimitives.ReadUInt32BigEndian(value);
        return result > int.MaxValue
            ? throw new ArgumentException("CRT cartridge length field is too large.")
            : (int)result;
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
