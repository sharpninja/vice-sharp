namespace ViceSharp.TestHarness.CartPort;

using FluentAssertions;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: ARCH-CARTEXT-001 (Phase C1).
/// Use case: A cart-port pin bus is constructed via the factory and behaves
/// like an InterSystemBus with the canonical signals (GAME, EXROM, IRQ,
/// NMI, DMA, RESET). Active extensions (SuperCPU, smart REU) attach an
/// endpoint to drive pins.
/// </summary>
public sealed class CartPortInterSystemBusTests
{
    /// <summary>
    /// FR/TR: ARCH-CARTEXT-001
    /// Use case: Factory returns a bus named "CartPort" carrying the six
    /// canonical pin signals.
    /// Acceptance: bus.Name = "CartPort"; bus.Signals contains GAME, EXROM,
    /// IRQ, NMI, DMA, RESET.
    /// </summary>
    [Fact]
    public void Factory_ProducesBusWithCanonicalCartPortSignals()
    {
        var bus = CartPortInterSystemBus.Create();

        bus.Name.Should().Be("CartPort");
        bus.Signals.Should().BeEquivalentTo(new[]
        {
            CartPortInterSystemBus.Game,
            CartPortInterSystemBus.ExRom,
            CartPortInterSystemBus.Irq,
            CartPortInterSystemBus.Nmi,
            CartPortInterSystemBus.Dma,
            CartPortInterSystemBus.Reset,
        });
    }

    /// <summary>
    /// FR/TR: ARCH-CARTEXT-001
    /// Use case: All pins start in their idle (high) state, matching the
    /// physical open-collector behavior when no cartridge is pulling.
    /// Acceptance: ReadLine returns true for every signal on a fresh bus.
    /// </summary>
    [Fact]
    public void FreshBus_AllPinsIdleHigh()
    {
        var bus = CartPortInterSystemBus.Create();

        foreach (var signal in CartPortInterSystemBus.Signals)
            bus.ReadLine(signal).Should().BeTrue($"signal {signal} should idle high");
    }

    /// <summary>
    /// FR/TR: ARCH-CARTEXT-001
    /// Use case: An extension pulls GAME low; the host endpoint observes
    /// the low state. Models the banking-mode selector behavior used by
    /// 16K and Ultimax cart configurations.
    /// Acceptance: After extension.Pull("GAME", true), host.ReadLine("GAME")
    /// returns false.
    /// </summary>
    [Fact]
    public void ExtensionPullsGame_HostObservesLow()
    {
        var bus = CartPortInterSystemBus.Create();
        var host = bus.AttachEndpoint("c64-cart-port");
        var ext = bus.AttachEndpoint("supercpu");

        ext.Pull(CartPortInterSystemBus.Game, low: true);

        host.ReadLine(CartPortInterSystemBus.Game).Should().BeFalse();
        bus.ReadLine(CartPortInterSystemBus.Game).Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: ARCH-CARTEXT-001
    /// Use case: Extension asserts IRQ; the host C64 endpoint reads the
    /// pin low and routes it into the CPU IRQ line. The pin-state read is
    /// the only thing the bus does here; the C64-side routing into a
    /// real IInterruptLine is host-side wiring (Phase C2).
    /// Acceptance: Extension pulls IRQ; host reads IRQ low.
    /// </summary>
    [Fact]
    public void ExtensionPullsIrq_HostObservesLow()
    {
        var bus = CartPortInterSystemBus.Create();
        var host = bus.AttachEndpoint("c64-cart-port");
        var ext = bus.AttachEndpoint("ext");

        ext.Pull(CartPortInterSystemBus.Irq, low: true);

        host.ReadLine(CartPortInterSystemBus.Irq).Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: ARCH-CARTEXT-001
    /// Use case: GAME / EXROM low combinations drive the host's banking
    /// mode. The bus carries the pin state - host code reads both pins
    /// and derives the mode (this test asserts the bus carries the state;
    /// derivation lives in C64MemoryMap in Phase C2).
    /// Acceptance: Pulling both GAME and EXROM low yields {GAME:low, EXROM:low}.
    /// </summary>
    [Fact]
    public void GameAndExRom_PulledByExtension_BothReadLow()
    {
        var bus = CartPortInterSystemBus.Create();
        var host = bus.AttachEndpoint("c64-cart-port");
        var ext = bus.AttachEndpoint("ext");

        ext.Pull(CartPortInterSystemBus.Game, low: true);
        ext.Pull(CartPortInterSystemBus.ExRom, low: true);

        host.ReadLine(CartPortInterSystemBus.Game).Should().BeFalse();
        host.ReadLine(CartPortInterSystemBus.ExRom).Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: ARCH-CARTEXT-001
    /// Use case: An extension that no longer wants to assert a pin releases
    /// it; line goes high once no endpoint pulls.
    /// Acceptance: Extension pulls + releases NMI; line returns high.
    /// </summary>
    [Fact]
    public void Extension_ReleasesNmi_LineReturnsHigh()
    {
        var bus = CartPortInterSystemBus.Create();
        var ext = bus.AttachEndpoint("ext");
        ext.Pull(CartPortInterSystemBus.Nmi, low: true);
        bus.ReadLine(CartPortInterSystemBus.Nmi).Should().BeFalse();

        ext.Pull(CartPortInterSystemBus.Nmi, low: false);

        bus.ReadLine(CartPortInterSystemBus.Nmi).Should().BeTrue();
    }
}
