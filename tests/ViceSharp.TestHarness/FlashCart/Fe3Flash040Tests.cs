namespace ViceSharp.TestHarness.FlashCart;

using ViceSharp.Core.Vic20;
using Xunit;

/// <summary>
/// FR: FE3 MODE_FLASH program path, TR: FinalExpansion3Cartridge flash040 wiring,
/// TEST: CPU-visible unlock + program through BLK5.
/// Use case: host/tools program FE3 flash via mapped BLK5 in FLASH mode.
/// Acceptance: without unlock write is ignored; with AA/55/A0 sequence BLK5 store programs.
/// VICE: finalexpansion.c MODE_FLASH -> flash040core_store; TYPE_B.
/// </summary>
public sealed class Fe3Flash040Tests
{
    [Fact]
    public void ModeFlash_Blk5_DirectWrite_DoesNotChangeFlash()
    {
        var image = new byte[FinalExpansion3Cartridge.FlashSize];
        Array.Fill(image, (byte)0xFF);
        var cart = new FinalExpansion3Cartridge(image);
        cart.ApplyConfigPreset("flash");

        cart.Write(0xA000, 0x00);
        Assert.Equal(0xFF, cart.Read(0xA000));
        Assert.False(cart.FlashDirty);
    }

    [Fact]
    public void ModeFlash_Blk5_ProgramSequence_ProgramsByte()
    {
        var image = new byte[FinalExpansion3Cartridge.FlashSize];
        Array.Fill(image, (byte)0xFF);
        var cart = new FinalExpansion3Cartridge(image);
        cart.ApplyConfigPreset("flash");

        // Magic via BLK5: $A555 / $A2AA map to faddr with low 11 bits 0x555 / 0x2AA.
        cart.Write(0xA555, 0xAA);
        cart.Write(0xA2AA, 0x55);
        cart.Write(0xA555, 0xA0);
        cart.Write(0xA010, 0x42);

        Assert.Equal(0x42, cart.Read(0xA010));
        Assert.True(cart.FlashDirty);
        Assert.Equal(0x42, cart.GetFlashImage()[0x6010]); // BLK5 base 0x6000 + 0x10
    }

    [Fact]
    public void ModeFlash_ChipErase_FillsFlashFf()
    {
        var image = new byte[FinalExpansion3Cartridge.FlashSize];
        Array.Fill(image, (byte)0x77);
        var cart = new FinalExpansion3Cartridge(image);
        cart.ApplyConfigPreset("flash");

        cart.Write(0xA555, 0xAA);
        cart.Write(0xA2AA, 0x55);
        cart.Write(0xA555, 0x80);
        cart.Write(0xA555, 0xAA);
        cart.Write(0xA2AA, 0x55);
        cart.Write(0xA555, 0x10);

        // TYPE_B chip erase: 8_000_000 maincpu cycles (Flash040Core.EraseChipCycles).
        cart.AdvanceFlashCycles(ViceSharp.Core.FlashCarts.Flash040Core.EraseChipCycles);

        Assert.Equal(0xFF, cart.Read(0xA000));
        Assert.Equal(0xFF, cart.GetFlashImage()[0]);
        Assert.True(cart.FlashDirty);
    }

    [Fact]
    public void ModeFlash_Autoselect_ManufacturerDevice()
    {
        var cart = new FinalExpansion3Cartridge(new byte[FinalExpansion3Cartridge.FlashSize]);
        cart.ApplyConfigPreset("flash");

        cart.Write(0xA555, 0xAA);
        cart.Write(0xA2AA, 0x55);
        cart.Write(0xA555, 0x90);

        // faddr for $A000 bank0 = 0x6000; autoselect uses addr & 0xFF
        Assert.Equal(0x01, cart.Read(0xA000)); // manufacturer at low byte 0
        Assert.Equal(0xA4, cart.Read(0xA001)); // device at low byte 1
    }
}
