namespace ViceSharp.TestHarness.FlashCart;

using ViceSharp.Core.Vic20;
using Xunit;

/// <summary>
/// FE3 features in scope: all REGA mode presets map without invent.
/// </summary>
public sealed class Fe3PresetTests
{
    [Theory]
    [InlineData("start", FinalExpansion3Cartridge.ModeStart)]
    [InlineData("flash", FinalExpansion3Cartridge.ModeFlash)]
    [InlineData("super-rom", FinalExpansion3Cartridge.ModeSuperRom)]
    [InlineData("rom-ram", FinalExpansion3Cartridge.ModeRomRam)]
    [InlineData("ram2", FinalExpansion3Cartridge.ModeRam2)]
    [InlineData("super-ram", FinalExpansion3Cartridge.ModeSuperRam)]
    public void ApplyConfigPreset_SetsModeBits(string preset, byte modeBits)
    {
        var cart = new FinalExpansion3Cartridge(ReadOnlySpan<byte>.Empty);
        cart.ApplyConfigPreset(preset);
        Assert.Equal(modeBits, (byte)(cart.RegisterA & FinalExpansion3Cartridge.RegAModeMask));
    }
}
