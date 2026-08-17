namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Core.Vic20;
using Xunit;

/// <summary>
/// FR-VIC20-CART-001 / AC-CT-06 / TEST-VIC20-CT-06: Ultimem register + bank windows.
/// Use case: control registers at $9C00+ select banked BLK windows (VICE ultimem.c).
/// Acceptance: power-up defaults, register write/read, BLK5 map offset stable.
/// </summary>
public sealed class UltimemParityTests
{
    [Fact]
    public void PowerUp_RegistersMatchViceDefaults_AndBlk5Readable()
    {
        var image = new byte[UltimemCartridge.DefaultImageSize];
        image[0] = 0xC3;
        image[1] = 0xA5;
        var cart = new UltimemCartridge(image);

        var registers = Enumerable.Range(0, 16)
            .Select(index => cart.Read((ushort)(0x9C00 + index)))
            .ToArray();
        Assert.Equal(
            new byte[] { 6, 0, 64, 0x11, 1, 0, 2, 0, 3, 0, 4, 0, 5, 0, 0, 0 },
            registers);
        Assert.True(cart.HandlesAddress(0xA000));
        Assert.Equal(0xC3, cart.Read(0xA000));
        Assert.Equal(0xA5, cart.Read(0xA001));
    }

    [Fact]
    public void PowerUp_UsesVice512KHardwareIdentifier()
    {
        var cart = new UltimemCartridge(new byte[512 * 1024]);

        Assert.Equal(0x12, cart.Read(0x9C03));
    }

    [Fact]
    public void RegisterWrite_RoundTripsLowNibbleRegisters()
    {
        var cart = new UltimemCartridge(new byte[UltimemCartridge.DefaultImageSize]);
        cart.Write(0x9C02, 0xAB);
        Assert.Equal(0xAB, cart.Read(0x9C02));
    }

    [Fact]
    public void Preset_Blk5Rom_DoesNotThrow()
    {
        var cart = new UltimemCartridge(new byte[UltimemCartridge.DefaultImageSize]);
        cart.ApplyConfigPreset("blk5-rom");
        Assert.Equal(0x03, cart.Read(0x9C02));
    }
}
