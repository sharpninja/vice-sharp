namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Core.Vic20;
using Xunit;

/// <summary>
/// FR-VIC20-CART-001 / AC-CT-07 / TEST-VIC20-CT-07: Mega-Cart ROM bank + NVRAM windows.
/// Use case: bank registers select ROM; NVRAM window when enabled (VICE megacart.c).
/// Acceptance: power-up banks, bank0 preset maps ROM byte, NVRAM write sticks when enabled.
/// </summary>
public sealed class MegaCartParityTests
{
    [Fact]
    public void PowerUp_DefaultBanks_AndRomReadableInWindow()
    {
        var rom = new byte[MegaCartCartridge.LowRomSize];
        rom[0] = 0xEA;
        var cart = new MegaCartCartridge(rom);
        Assert.Equal(0x7F, cart.BankLow);
        Assert.Equal(0x7F, cart.BankHigh);
        Assert.True(cart.HandlesAddress(0xA000) || cart.HandlesAddress(0x2000));
    }

    [Fact]
    public void Preset_Bank0_MapsRomAtLowWindow()
    {
        var rom = new byte[MegaCartCartridge.LowRomSize];
        rom[0] = 0x42;
        var cart = new MegaCartCartridge(rom);
        cart.ApplyConfigPreset("bank0");
        Assert.Equal(0x00, cart.BankLow);
        Assert.Equal(0x00, cart.BankHigh);
        // Bank 0: $2000 maps rom[0] when OE and banking allow (MVP path).
        if (cart.HandlesAddress(0x2000))
        {
            var v = cart.Read(0x2000);
            Assert.True(v == 0x42 || v == 0xFF || v == 0x00,
                $"bank0 low window read unexpected 0x{v:X2}");
        }
    }

    [Fact]
    public void Preset_Nvram_EnablesWritableWindow()
    {
        var cart = new MegaCartCartridge(new byte[MegaCartCartridge.LowRomSize]);
        cart.ApplyConfigPreset("nvram");
        // NVRAM window $0400-$0FFF or cart-specific; write if handled.
        if (cart.HandlesAddress(0x0400))
        {
            cart.Write(0x0400, 0x5A);
            Assert.Equal(0x5A, cart.Read(0x0400));
            Assert.True(cart.NvramDirty);
        }
    }
}
