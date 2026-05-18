namespace ViceSharp.TestHarness.CartPort;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.EmptyMachine;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: ARCH-CARTEXT-001 (Phase C1).
/// Use case: A placeholder "active cart" extension (stub for SuperCPU, smart
/// REU, future accelerator carts) attaches via AttachCartExtension and ticks
/// in lockstep with the host clock (phi2-lock). Pin signals propagate via
/// the cart-port InterSystemBus.
///
/// Not a real SuperCPU - the W65C816 emulation is its own slice. This is the
/// substrate proof: any IMachine can be a cart-port extension, and the
/// coordinator + bus carry the load.
/// </summary>
public sealed class SuperCpuPlaceholderTests
{
    private static (SystemCoordinator coord, IMachine host, IMachine ext,
        InterSystemBus cartBus, IBusEndpoint hostEp, IBusEndpoint extEp) BuildRig()
    {
        var host = new ArchitectureBuilder().Build(new EmptyMachineDescriptor());
        var ext = new ArchitectureBuilder().Build(new EmptyMachineDescriptor());
        var coord = new SystemCoordinator();
        coord.AttachSystem(host);
        coord.AttachCartExtension(ext, host);
        var cartBus = CartPortInterSystemBus.Create();
        coord.AttachBus(cartBus);
        var hostEp = cartBus.AttachEndpoint("c64-cart-port");
        var extEp = cartBus.AttachEndpoint("supercpu-placeholder");
        return (coord, host, ext, cartBus, hostEp, extEp);
    }

    /// <summary>
    /// FR/TR: ARCH-CARTEXT-001
    /// Use case: A cart extension ticks once per host cycle regardless of
    /// its own clock's declared FrequencyHz - it shares phi2 with the host.
    /// Acceptance: After coordinator.Step(1000), both host clock and
    /// extension clock have stepped 1000 cycles.
    /// </summary>
    [Fact]
    public void Extension_TicksOncePerHostCycle_Phi2Locked()
    {
        var rig = BuildRig();

        rig.coord.Step(1000);

        rig.host.Clock.TotalCycles.Should().Be(1000);
        rig.ext.Clock.TotalCycles.Should().Be(1000);
        rig.coord.TotalHostCycles.Should().Be(1000);
    }

    /// <summary>
    /// FR/TR: ARCH-CARTEXT-001
    /// Use case: Extension asserts an IRQ via its cart-port endpoint; the
    /// host endpoint observes the pulled-low pin. Demonstrates the
    /// cart-port pin path that will let Phase C2 route IRQ into the host
    /// CPU's IInterruptLine.
    /// Acceptance: ext.Pull("IRQ", true) -> host.ReadLine("IRQ") = false.
    /// </summary>
    [Fact]
    public void Extension_AssertsIrq_HostObservesPulledLow()
    {
        var rig = BuildRig();

        rig.extEp.Pull(CartPortInterSystemBus.Irq, low: true);

        rig.hostEp.ReadLine(CartPortInterSystemBus.Irq).Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: ARCH-CARTEXT-001
    /// Use case: Extension drives GAME + EXROM low to select Ultimax-style
    /// banking; both pins propagate as low on the bus.
    /// Acceptance: After dual pulls, both pin reads are false.
    /// </summary>
    [Fact]
    public void Extension_AssertsGameAndExRom_HostObservesUltimaxBanking()
    {
        var rig = BuildRig();

        rig.extEp.Pull(CartPortInterSystemBus.Game, low: true);
        rig.extEp.Pull(CartPortInterSystemBus.ExRom, low: true);

        rig.hostEp.ReadLine(CartPortInterSystemBus.Game).Should().BeFalse();
        rig.hostEp.ReadLine(CartPortInterSystemBus.ExRom).Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: ARCH-CARTEXT-001
    /// Use case: A long coordinator run with both machines executing keeps
    /// the phi2 invariant: extension cycles always equal host cycles.
    /// Acceptance: After 100k coordinator steps, host = ext = 100k.
    /// </summary>
    [Fact]
    public void Extension_LongRun_StaysInLockstep_WithHost()
    {
        var rig = BuildRig();

        rig.coord.Step(100_000);

        rig.host.Clock.TotalCycles.Should().Be(100_000);
        rig.ext.Clock.TotalCycles.Should().Be(100_000);
    }

    /// <summary>
    /// FR/TR: ARCH-CARTEXT-001
    /// Use case: Coordinator.Reset propagates to both host and extension.
    /// Acceptance: After Step(500) + Reset, both clocks at 0.
    /// </summary>
    [Fact]
    public void Coordinator_Reset_ResetsHostAndExtension()
    {
        var rig = BuildRig();
        rig.coord.Step(500);

        rig.coord.Reset();

        rig.host.Clock.TotalCycles.Should().Be(0);
        rig.ext.Clock.TotalCycles.Should().Be(0);
        rig.coord.TotalHostCycles.Should().Be(0);
    }
}
