namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Core.Vic20;
using Xunit;

/// <summary>
/// FE3 / Ultimem / Mega-Cart attach and map smoke tests driving real cart types.
/// </summary>
public sealed class Vic20ExpansionCartTests
{
    [Fact]
    public void Fe3_Ram1_FullPreset_MapsBlk1()
    {
        var flash = new byte[FinalExpansion3Cartridge.FlashSize];
        var cart = new FinalExpansion3Cartridge(flash);
        cart.ApplyConfigPreset("full");
        Assert.Equal(FinalExpansion3Cartridge.ModeRam1 | FinalExpansion3Cartridge.RegABlk0Ro
            | FinalExpansion3Cartridge.RegABlk1Sel | FinalExpansion3Cartridge.RegABlk2Sel
            | FinalExpansion3Cartridge.RegABlk3Sel | FinalExpansion3Cartridge.RegABlk5Sel,
            cart.RegisterA & 0xFF);

        cart.Write(0x2000, 0x5A);
        Assert.Equal(0x5A, cart.Read(0x2000));
        cart.Write(0xA000, 0xA5);
        Assert.Equal(0xA5, cart.Read(0xA000));
    }

    [Fact]
    public void Fe3_Io3_RegARegB_RoundTrip()
    {
        var cart = new FinalExpansion3Cartridge(new byte[FinalExpansion3Cartridge.FlashSize]);
        cart.Write(0x9C02, FinalExpansion3Cartridge.ModeSuperRam | 0x03);
        cart.Write(0x9C03, FinalExpansion3Cartridge.RegBBlk0Off);
        Assert.Equal((byte)(FinalExpansion3Cartridge.ModeSuperRam | 0x03), cart.Read(0x9C02));
        Assert.Equal(FinalExpansion3Cartridge.RegBBlk0Off, cart.Read(0x9C03));
        // Mirror
        Assert.Equal(cart.Read(0x9C02), cart.Read(0x9C06));
    }

    [Fact]
    public void Fe3_WriteBack_FlushReturnsDirtyFlash()
    {
        var image0 = new byte[FinalExpansion3Cartridge.FlashSize];
        Array.Fill(image0, (byte)0xFF);
        var cart = new FinalExpansion3Cartridge(image0);
        cart.WriteBack = true;
        // MODE_FLASH + AM29F040B program sequence (not a raw poke).
        cart.Write(0x9C02, FinalExpansion3Cartridge.ModeFlash);
        cart.Write(0xA555, 0xAA);
        cart.Write(0xA2AA, 0x55);
        cart.Write(0xA555, 0xA0);
        cart.Write(0xA010, 0x42);
        Assert.True(cart.FlashDirty);
        var image = cart.GetFlashImage();
        Assert.Equal(FinalExpansion3Cartridge.FlashSize, image.Length);
        Assert.Equal(0x42, image[0x6010]);
    }

    [Fact]
    public void Ultimem_BankRegister_MapsBlk5()
    {
        var image = new byte[0x20000];
        for (var i = 0; i < image.Length; i++)
            image[i] = (byte)(i & 0xFF);
        var cart = new UltimemCartridge(image);
        cart.Write(0x9C0E, 0x01); // bank lo for BLK5
        var v = cart.Read(0xA000);
        // bank 1 * 0x2000 + 0
        Assert.Equal(image[0x2000], v);
    }

    [Fact]
    public void MegaCart_BankLow_SelectsRomWindow()
    {
        var rom = new byte[0x10000];
        rom[0x0000] = 0x11;
        rom[0x2000] = 0x22;
        var cart = new MegaCartCartridge(rom);
        cart.Write(0x9C00, 0x00);
        Assert.Equal(0x11, cart.Read(0x2000));
        cart.Write(0x9C00, 0x01);
        Assert.Equal(0x22, cart.Read(0x2000));
    }

    [Fact]
    public void MegaCart_Nvram_WriteAndFlush()
    {
        var cart = new MegaCartCartridge(new byte[0x10000]);
        cart.ApplyConfigPreset("nvram");
        // Enable nvram via bank bit7
        cart.Write(0x9C00, 0x80);
        cart.Write(0x0500, 0x77);
        Assert.Equal(0x77, cart.Read(0x0500));
        Assert.True(cart.NvramDirty);
        var nv = cart.GetNvram();
        Assert.Equal(0x77, nv[0x0500]);
    }

    [Fact]
    public void ExpansionCartPort_AttachFe3_OnBus()
    {
        var bus = new ViceSharp.Core.BasicBus();
        var port = new Vic20ExpansionCartPort(bus);
        var flash = new byte[FinalExpansion3Cartridge.FlashSize];
        flash[0] = 0xC3;
        port.AttachFinalExpansion3(flash);
        Assert.Equal(Vic20ExpansionCartKind.FinalExpansion3, port.AttachedKind);
        port.ApplyConfigPreset("8k");
        Assert.True(port.TryWriteMapped(0x2000, 0xAB));
        Assert.True(port.TryReadMapped(0x2000, out var v));
        Assert.Equal(0xAB, v);
        port.Eject();
        Assert.Equal(Vic20ExpansionCartKind.None, port.AttachedKind);
    }
}
