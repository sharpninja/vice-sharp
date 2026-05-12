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
}
