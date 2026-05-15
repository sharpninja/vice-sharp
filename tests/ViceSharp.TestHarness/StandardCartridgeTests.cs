namespace ViceSharp.TestHarness;

using ViceSharp.Chips.Cartridges;
using Xunit;

public sealed class StandardCartridgeTests
{
    [Fact]
    public void FromBytes_Accepts8KImage_AndMapsRomLowOnly()
    {
        var image = CreateImage(StandardCartridgeImage.RomBankSize);
        image[0] = 0x11;
        image[0x1FFF] = 0x22;

        var cartridge = StandardCartridgeImage.FromBytes(image);

        Assert.Equal(StandardCartridgeSize.Rom8K, cartridge.Size);
        Assert.True(cartridge.ExromLineAsserted);
        Assert.False(cartridge.GameLineAsserted);
        Assert.True(cartridge.HandlesAddress(0x8000));
        Assert.True(cartridge.HandlesAddress(0x9FFF));
        Assert.False(cartridge.HandlesAddress(0xA000));
        Assert.Equal(0x11, cartridge.Read(0x8000));
        Assert.Equal(0x22, cartridge.Read(0x9FFF));
        Assert.Equal(0xFF, cartridge.Read(0xA000));
    }

    [Fact]
    public void FromBytes_Accepts16KImage_AndMapsRomLowAndRomHigh()
    {
        var image = CreateImage(StandardCartridgeImage.RomBankSize * 2);
        image[0] = 0x33;
        image[0x1FFF] = 0x44;
        image[0x2000] = 0x55;
        image[0x3FFF] = 0x66;

        var cartridge = StandardCartridgeImage.FromBytes(image);

        Assert.Equal(StandardCartridgeSize.Rom16K, cartridge.Size);
        Assert.True(cartridge.ExromLineAsserted);
        Assert.True(cartridge.GameLineAsserted);
        Assert.True(cartridge.HandlesAddress(0x8000));
        Assert.True(cartridge.HandlesAddress(0x9FFF));
        Assert.True(cartridge.HandlesAddress(0xA000));
        Assert.True(cartridge.HandlesAddress(0xBFFF));
        Assert.False(cartridge.HandlesAddress(0xC000));
        Assert.Equal(0x33, cartridge.Read(0x8000));
        Assert.Equal(0x44, cartridge.Peek(0x9FFF));
        Assert.Equal(0x55, cartridge.Read(0xA000));
        Assert.Equal(0x66, cartridge.Peek(0xBFFF));
    }

    [Fact]
    public void FromBytes_RejectsNonStandardLength()
    {
        var image = CreateImage(StandardCartridgeImage.RomBankSize - 1);

        var ex = Assert.Throws<ArgumentException>(() => StandardCartridgeImage.FromBytes(image));

        Assert.Contains("8K or 16K", ex.Message);
    }

    [Fact]
    public void FromBytes_AcceptsGeneric8KCrt_AndNormalizesToRawRom()
    {
        var rom = CreateImage(StandardCartridgeImage.RomBankSize);
        rom[0] = 0x99;
        rom[^1] = 0xAA;
        var image = CreateGenericCrt(rom, includeHigh: false);

        var cartridge = StandardCartridgeImage.FromBytes(image);

        Assert.Equal(StandardCartridgeSize.Rom8K, cartridge.Size);
        Assert.Equal(StandardCartridgeImage.RomBankSize, cartridge.ImageSizeBytes);
        Assert.Equal(0x99, cartridge.Read(0x8000));
        Assert.Equal(0xAA, cartridge.Read(0x9FFF));
        Assert.Equal(rom, cartridge.ToArray());
    }

    [Fact]
    public void FromBytes_AcceptsGeneric16KCrt_AndMapsRomLowAndHigh()
    {
        var rom = CreateImage(StandardCartridgeImage.RomBankSize * 2);
        rom[0] = 0x10;
        rom[StandardCartridgeImage.RomBankSize] = 0x20;
        rom[^1] = 0x30;
        var image = CreateGenericCrt(rom, includeHigh: true);

        var cartridge = StandardCartridgeImage.FromBytes(image);

        Assert.Equal(StandardCartridgeSize.Rom16K, cartridge.Size);
        Assert.Equal(rom, cartridge.ToArray());
        Assert.Equal(0x10, cartridge.Read(0x8000));
        Assert.Equal(0x20, cartridge.Read(0xA000));
        Assert.Equal(0x30, cartridge.Read(0xBFFF));
    }

    [Fact]
    public void Write_DoesNotMutateRomContents()
    {
        var image = CreateImage(StandardCartridgeImage.RomBankSize);
        image[0] = 0x77;
        var cartridge = StandardCartridgeImage.FromBytes(image);

        cartridge.Write(0x8000, 0x88);

        Assert.Equal(0x77, cartridge.Read(0x8000));
    }

    private static byte[] CreateImage(int length)
    {
        var image = new byte[length];
        for (var i = 0; i < image.Length; i++)
        {
            image[i] = (byte)(i & 0xFF);
        }

        return image;
    }

    public static byte[] CreateGenericCrt(byte[] rom, bool includeHigh)
    {
        var packetCount = includeHigh ? 2 : 1;
        var image = new byte[StandardCartridgeImage.CrtHeaderSize + packetCount * (StandardCartridgeImage.CrtChipHeaderSize + StandardCartridgeImage.RomBankSize)];
        "C64 CARTRIDGE   "u8.CopyTo(image);
        WriteInt32(image.AsSpan(0x10, 4), StandardCartridgeImage.CrtHeaderSize);
        WriteUInt16(image.AsSpan(0x14, 2), 0x0100);
        WriteUInt16(image.AsSpan(0x16, 2), 0);
        image[0x18] = 1;
        image[0x19] = includeHigh ? (byte)1 : (byte)0;
        "VICE-SHARP TEST"u8.CopyTo(image.AsSpan(0x20));

        WriteChipPacket(
            image.AsSpan(StandardCartridgeImage.CrtHeaderSize),
            0,
            StandardCartridgeImage.RomLowStart,
            rom.AsSpan(0, StandardCartridgeImage.RomBankSize));

        if (includeHigh)
        {
            WriteChipPacket(
                image.AsSpan(StandardCartridgeImage.CrtHeaderSize + StandardCartridgeImage.CrtChipHeaderSize + StandardCartridgeImage.RomBankSize),
                0,
                StandardCartridgeImage.RomHighStart,
                rom.AsSpan(StandardCartridgeImage.RomBankSize, StandardCartridgeImage.RomBankSize));
        }

        return image;
    }

    private static void WriteChipPacket(Span<byte> destination, ushort bank, ushort loadAddress, ReadOnlySpan<byte> rom)
    {
        "CHIP"u8.CopyTo(destination);
        WriteInt32(destination[0x04..0x08], StandardCartridgeImage.CrtChipHeaderSize + rom.Length);
        WriteUInt16(destination[0x08..0x0A], 0);
        WriteUInt16(destination[0x0A..0x0C], bank);
        WriteUInt16(destination[0x0C..0x0E], loadAddress);
        WriteUInt16(destination[0x0E..0x10], (ushort)rom.Length);
        rom.CopyTo(destination[StandardCartridgeImage.CrtChipHeaderSize..]);
    }

    private static void WriteInt32(Span<byte> destination, int value)
    {
        destination[0] = (byte)(value >> 24);
        destination[1] = (byte)(value >> 16);
        destination[2] = (byte)(value >> 8);
        destination[3] = (byte)value;
    }

    private static void WriteUInt16(Span<byte> destination, ushort value)
    {
        destination[0] = (byte)(value >> 8);
        destination[1] = (byte)value;
    }
}
