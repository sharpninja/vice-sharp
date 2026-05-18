namespace ViceSharp.TestHarness.UserPort;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: ARCH-USERPORT-001 (Phase D1b).
/// Use case: Two C64 machines connected user-port-to-user-port (the classic
/// "two-C64 RS232 link" scenario) - each at its own clock under a shared
/// SystemCoordinator, exchanging signal state on PB lines + handshake.
///
/// Full RS232 bit-banging tied to CIA2 SP/CNT lines is Phase D1c; this
/// slice proves the substrate carries the byte-level link cleanly.
/// </summary>
public sealed class TwoC64UserPortLinkTests
{
    private static (SystemCoordinator coord, IMachine a, IMachine b,
        InterSystemBus userPort, IBusEndpoint epA, IBusEndpoint epB) BuildLinkRig()
    {
        var provider = MachineTestFactory.CreateC64RomProvider();
        var a = new ArchitectureBuilder(provider).Build(new C64Descriptor());
        var b = new ArchitectureBuilder(provider).Build(new C64Descriptor());
        var coord = new SystemCoordinator();
        coord.AttachSystem(a);
        coord.AttachSystem(b);
        var bus = UserPortInterSystemBus.Create();
        coord.AttachBus(bus);
        var epA = bus.AttachEndpoint("c64-a");
        var epB = bus.AttachEndpoint("c64-b");
        return (coord, a, b, bus, epA, epB);
    }

    /// <summary>
    /// FR/TR: ARCH-USERPORT-001
    /// Use case: Two C64s under one coordinator advance at the same PAL clock
    /// rate; user port bus is registered and ready for endpoint traffic.
    /// Acceptance: After Step(50_000) both machines have 50_000 cycles.
    /// </summary>
    [Fact]
    public void TwoC64s_OnUserPortBus_AdvanceLockstep_PalClock()
    {
        var rig = BuildLinkRig();

        rig.coord.Step(50_000);

        rig.a.Clock.TotalCycles.Should().Be(50_000);
        rig.b.Clock.TotalCycles.Should().Be(50_000);
    }

    /// <summary>
    /// FR/TR: ARCH-USERPORT-001
    /// Use case: Machine A writes a byte to PB via its user-port endpoint;
    /// Machine B reads the same byte from its endpoint.
    /// Acceptance: After WritePortB($A5) on A, ReadPortB on B returns $A5.
    /// </summary>
    [Fact]
    public void MachineA_WritesPortB_MachineB_Reads_SameByte()
    {
        var rig = BuildLinkRig();

        UserPortInterSystemBus.WritePortB(rig.epA, 0xA5);

        UserPortInterSystemBus.ReadPortB(rig.epB).Should().Be(0xA5);
    }

    /// <summary>
    /// FR/TR: ARCH-USERPORT-001
    /// Use case: Bidirectional handshake - A pulls FLAG2 to signal "byte
    /// ready" to B; B reads FLAG2 low.
    /// Acceptance: After A pulls FLAG2, B reads it low.
    /// </summary>
    [Fact]
    public void Handshake_Flag2_PropagatesBetweenC64s()
    {
        var rig = BuildLinkRig();

        rig.epA.Pull(UserPortInterSystemBus.Flag2, low: true);

        rig.epB.ReadLine(UserPortInterSystemBus.Flag2).Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: ARCH-USERPORT-001
    /// Use case: Wired-OR on PB lines - both sides simultaneously driving
    /// different bits low produces the AND of their high bits.
    /// Acceptance: A writes $0F, B writes $F0; both read $00.
    /// </summary>
    [Fact]
    public void Bidirectional_PortB_WiredOr_Both_ReadCombinedResult()
    {
        var rig = BuildLinkRig();

        UserPortInterSystemBus.WritePortB(rig.epA, 0x0F);
        UserPortInterSystemBus.WritePortB(rig.epB, 0xF0);

        UserPortInterSystemBus.ReadPortB(rig.epA).Should().Be(0x00);
        UserPortInterSystemBus.ReadPortB(rig.epB).Should().Be(0x00);
    }

    /// <summary>
    /// FR/TR: ARCH-USERPORT-001
    /// Use case: Long coordinator run with both C64 ROMs executing - no
    /// crash, both PCs advance, user-port bus stays operable.
    /// Acceptance: After 100k steps, both PCs differ from reset values
    /// and a fresh byte write/read still round-trips.
    /// </summary>
    [Fact]
    public void LongRun_BothC64sExecuting_UserPortStillOperable()
    {
        var rig = BuildLinkRig();
        rig.a.Reset();
        rig.b.Reset();
        var resetPcA = rig.a.GetState().PC;
        var resetPcB = rig.b.GetState().PC;

        rig.coord.Step(100_000);

        rig.a.GetState().PC.Should().NotBe(resetPcA);
        rig.b.GetState().PC.Should().NotBe(resetPcB);
        UserPortInterSystemBus.WritePortB(rig.epA, 0x42);
        UserPortInterSystemBus.ReadPortB(rig.epB).Should().Be(0x42);
    }
}
