namespace ViceSharp.TestHarness.Wiring;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: ARCH-CARTEXT-002 (Phase C2).
/// Use case: A pin-driven cart extension on the cart-port InterSystemBus
/// dynamically asserts GAME/EXROM to switch the host C64's memory map
/// mode at runtime. Static StandardCartridgeImage attachment remains the
/// cheap path; the pin source overrides when set.
/// </summary>
public sealed class C64MemoryMapPinDrivenTests
{
    private static byte[] FixedPattern16K()
    {
        var bytes = new byte[0x4000];
        for (int i = 0; i < bytes.Length; i++) bytes[i] = (byte)(i & 0xFF);
        return bytes;
    }

    private static (IMachine machine, ICartridgePort cart) BuildC64WithCart16K()
    {
        var provider = MachineTestFactory.CreateC64RomProvider();
        var machine = new ArchitectureBuilder(provider).Build(new C64Descriptor());
        var cart = (ICartridgePort)machine.Devices.GetByRole(DeviceRole.CartridgePort)!;
        cart.AttachCartridge(FixedPattern16K(), CartridgeMappingMode.Standard16K);
        machine.Reset();
        return (machine, cart);
    }

    // Helper retained for clarity; tests interact via ICartridgePort.

    /// <summary>
    /// FR/TR: ARCH-CARTEXT-002
    /// Use case: Without a pin source, behavior is unchanged - the attached
    /// mapping mode determines cart visibility.
    /// Acceptance: After attaching a 16K cart in Standard16K mode, $8000
    /// and $A000 read from the cart image (after RAM-disable PLA banking).
    /// </summary>
    [Fact]
    public void WithoutPinSource_UsesStaticMappingMode()
    {
        var (machine, cart) = BuildC64WithCart16K();

        // PLA banking: write $01 = $37 -> LORAM + HIRAM + CHAREN; RAM at $8000
        // overrides cart. To see cart, we use a banking config where ROML/ROMH
        // are visible. With Standard16K + GAME=0 EXROM=0 (default attach) the
        // cart is visible at $8000-$BFFF when LORAM+HIRAM+GAME=0+EXROM=0.
        machine.Bus.Write(0x0001, 0x37); // default LORAM+HIRAM+CHAREN

        var rom8000 = machine.Bus.Read(0x8000);
        var rom9000 = machine.Bus.Read(0x9000);
        rom8000.Should().Be(0x00);
        rom9000.Should().Be(0x00);
    }

    /// <summary>
    /// FR/TR: ARCH-CARTEXT-002
    /// Use case: With a cart-port bus + endpoint set as pin source, the
    /// memory map derives the mapping mode from GAME/EXROM live state.
    /// Asserting GAME=0 + EXROM=1 selects Ultimax; cart visible at $E000.
    /// Acceptance: Pin source set + extension pulls EXROM (and not GAME) =>
    /// Ultimax mapping; Read($E000) returns cart byte.
    /// </summary>
    [Fact]
    public void PinSource_GameLow_ExromHigh_SelectsUltimax()
    {
        var (machine, cart) = BuildC64WithCart16K();
        var bus = CartPortInterSystemBus.Create();
        var hostEp = bus.AttachEndpoint("c64");
        var extEp = bus.AttachEndpoint("ext");
        cart.SetCartPortPinSource(hostEp);

        // Drive GAME=0, EXROM=1 -> Ultimax. (low: true on GAME, low: false on EXROM)
        extEp.Pull(CartPortInterSystemBus.Game, low: true);
        extEp.Pull(CartPortInterSystemBus.ExRom, low: false);

        machine.Bus.Write(0x0001, 0x37);
        // Ultimax maps ROMH at $E000-$FFFF + ROML at $8000-$9FFF; in 16K image
        // the high bank (offset $2000-$3FFF) is at $E000.
        var rom_e000 = machine.Bus.Read(0xE000);
        rom_e000.Should().Be(0x00);
    }

    /// <summary>
    /// FR/TR: ARCH-CARTEXT-002
    /// Use case: Releasing GAME + EXROM (both high) hides the cart through
    /// the pin path even though an image is still attached.
    /// Acceptance: Pin source set + both lines high => ResolveActiveMapping
    /// returns null; reads at $8000 fall back to RAM / banking.
    /// </summary>
    [Fact]
    public void PinSource_BothHigh_HidesCart()
    {
        var (machine, cart) = BuildC64WithCart16K();
        var bus = CartPortInterSystemBus.Create();
        var hostEp = bus.AttachEndpoint("c64");
        cart.SetCartPortPinSource(hostEp);

        // No extension pulls anything - both lines float high.
        machine.Bus.Write(0x0001, 0x37);
        machine.Bus.Write(0x8000, 0x5A); // write into RAM at $8000
        machine.Bus.Read(0x8000).Should().Be(0x5A,
            "with both pins high cart is hidden; $8000 reads from RAM");
    }

    /// <summary>
    /// FR/TR: ARCH-CARTEXT-002
    /// Use case: SetCartPortPinSource(null) reverts to static mapping mode.
    /// Acceptance: After set + clear, behavior matches initial attach mode.
    /// </summary>
    [Fact]
    public void SetCartPortPinSource_Null_RevertsToStatic()
    {
        var (machine, cart) = BuildC64WithCart16K();
        var bus = CartPortInterSystemBus.Create();
        var hostEp = bus.AttachEndpoint("c64");
        cart.SetCartPortPinSource(hostEp);

        cart.SetCartPortPinSource(null);
        machine.Bus.Write(0x0001, 0x37);

        // Static mapping is Standard16K -> cart visible at $8000.
        machine.Bus.Read(0x8000).Should().Be(0x00);
    }

    /// <summary>
    /// FR/TR: ARCH-CARTEXT-002
    /// Use case: With no cart image attached, the pin source path returns
    /// null regardless of pin state.
    /// Acceptance: SetCartPortPinSource + pull GAME low + no cart => $8000
    /// reads RAM, not cart.
    /// </summary>
    [Fact]
    public void PinSource_NoImageAttached_FallsThroughToRam()
    {
        var provider = MachineTestFactory.CreateC64RomProvider();
        var machine = new ArchitectureBuilder(provider).Build(new C64Descriptor());
        machine.Reset();
        var cart = (ICartridgePort)machine.Devices.GetByRole(DeviceRole.CartridgePort)!;
        var bus = CartPortInterSystemBus.Create();
        var hostEp = bus.AttachEndpoint("c64");
        var extEp = bus.AttachEndpoint("ext");
        cart.SetCartPortPinSource(hostEp);

        extEp.Pull(CartPortInterSystemBus.Game, low: true);
        machine.Bus.Write(0x0001, 0x37);
        machine.Bus.Write(0x8000, 0x3C);

        machine.Bus.Read(0x8000).Should().Be(0x3C);
    }
}
