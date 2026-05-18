namespace ViceSharp.TestHarness.Wiring;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.Multisystem;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: ARCH-TOPOLOGY-002 (Phase H3).
/// Use case: A topology with multiple bus types (IEC for a drive +
/// UserPort for a peer link) running concurrently under one coordinator.
/// The auto-bind walks each system and wires CIA2 to BOTH buses when
/// matching endpoints exist. Both buses carry traffic in parallel.
/// </summary>
public sealed class MultiBusTopologyTests
{
    private const string MultiBusYaml = """
        schemaVersion: 1
        coordinator:
          host:
            id: c64-host
            kind: C64
            busAttachments:
              - busId: IEC
                endpointName: c64-iec
              - busId: UserPort
                endpointName: c64-user
          peripherals:
            - id: drive-8
              kind: C1541
              deviceNumber: 8
              busAttachments:
                - busId: IEC
                  endpointName: drive-8
            - id: peer-c64
              kind: C64
              busAttachments:
                - busId: UserPort
                  endpointName: peer
          buses:
            - id: IEC
              signals: [ATN, CLK, DATA, SRQ]
            - id: UserPort
              signals: [PB0, PB1, PB2, PB3, PB4, PB5, PB6, PB7, PA2, PC2, FLAG2, CNT1, CNT2, SP1, SP2, ATN, RESET]
        """;

    private static MultiSystemBuildResult BuildRig()
    {
        var provider = MachineTestFactory.CreateC64RomProvider();
        var bp = new MultiSystemYamlLoader().LoadFromString(MultiBusYaml);
        return bp.BuildCoordinatorAuto(new ArchitectureBuilder(provider));
    }

    /// <summary>
    /// FR/TR: ARCH-TOPOLOGY-002
    /// Use case: Three systems built, two buses registered, four endpoint
    /// attachments recorded by the loader + coordinator.
    /// Acceptance: SystemsById has c64-host + drive-8 + peer-c64;
    /// BusesById has IEC + UserPort; endpoint count = 4.
    /// </summary>
    [Fact]
    public void Build_ProducesThreeSystems_TwoBuses_FourEndpoints()
    {
        var build = BuildRig();

        build.SystemsById.Keys.Should().BeEquivalentTo(new[] { "c64-host", "drive-8", "peer-c64" });
        build.BusesById.Keys.Should().BeEquivalentTo(new[] { "IEC", "UserPort" });
        build.Endpoints.Should().HaveCount(4);
        build.Endpoints.Should().ContainKeys(
            ("c64-host", "IEC"),
            ("c64-host", "UserPort"),
            ("drive-8", "IEC"),
            ("peer-c64", "UserPort"));
    }

    /// <summary>
    /// FR/TR: ARCH-TOPOLOGY-002
    /// Use case: Auto-bind wires host CIA2 to BOTH the IEC + UserPort
    /// endpoints. Host CIA2 PA writes drive IEC; host CIA2 PB writes drive
    /// UserPort. Verified via direct register writes.
    /// Acceptance: Host PA=$08 -> drive VIA1 PB7 = 1; host PB=$5A -> peer
    /// CIA2 read of PB returns $5A.
    /// </summary>
    [Fact]
    public void HostCia2_AutoBound_ToBothIecAndUserPort()
    {
        var build = BuildRig();
        var hostCia2 = (Mos6526)build.SystemsById["c64-host"].Devices.GetByRole(DeviceRole.Cia2)!;
        var driveVia1 = build.SystemsById["drive-8"].Devices.GetAll<Via6522>()
            .OrderBy(v => v.BaseAddress).First();
        var peerCia2 = (Mos6526)build.SystemsById["peer-c64"].Devices.GetByRole(DeviceRole.Cia2)!;
        peerCia2.Write(0xDD03, 0x00); // peer DDRB all inputs

        hostCia2.Write(0xDD00, 0x08); // assert ATN
        hostCia2.Write(0xDD03, 0xFF); // host DDRB all outputs
        hostCia2.Write(0xDD01, 0x5A);

        (driveVia1.Read(0x1800) & 0x80).Should().Be(0x80, "drive sees ATN on IEC");
        peerCia2.Read(0xDD01).Should().Be(0x5A, "peer reads host PB output via UserPort");
    }

    /// <summary>
    /// FR/TR: ARCH-TOPOLOGY-002
    /// Use case: IEC bus carries KERNAL-driven traffic during boot. UserPort
    /// bus carries synthetic traffic when the host CIA2 writes to PB - the
    /// C64 KERNAL itself does not touch CIA2 PB during stock boot, so the
    /// test exercises it explicitly.
    /// Acceptance: After 200k cycles, IEC tracer > 0. After a synthetic
    /// host write to $DD01, UserPort tracer also > 0.
    /// </summary>
    [Fact]
    public void BothBuses_CarryTraffic_KernalDrivesIec_SyntheticDrivesUserPort()
    {
        var build = BuildRig();
        var iecTracer = new InterSystemBusTracer(build.BusesById["IEC"]);
        var upTracer = new InterSystemBusTracer(build.BusesById["UserPort"]);
        iecTracer.Attach();
        upTracer.Attach();

        build.Coordinator.Step(200_000);
        iecTracer.Events.Count.Should().BeGreaterThan(0, "C64 KERNAL drives IEC during boot");

        // Synthetic UserPort traffic via host CIA2 PB write.
        var hostCia2 = (Mos6526)build.SystemsById["c64-host"].Devices.GetByRole(DeviceRole.Cia2)!;
        hostCia2.Write(0xDD03, 0xFF);
        hostCia2.Write(0xDD01, 0x5A);

        upTracer.Events.Count.Should().BeGreaterThan(0, "host CIA2 PB write must drive UserPort");
    }

    /// <summary>
    /// FR/TR: ARCH-TOPOLOGY-002
    /// Use case: Peer C64 advances at PAL clock alongside the host + drive.
    /// Three independent clocks share the coordinator without drift.
    /// Acceptance: After 985_248 host cycles, host + peer C64 both at exactly
    /// 985_248 cycles; drive within 1 of 1_000_000.
    /// </summary>
    [Fact]
    public void AllThreeMachines_AdvanceAtIndependentClockRates()
    {
        var build = BuildRig();

        build.Coordinator.Step(985_248);

        build.SystemsById["c64-host"].Clock.TotalCycles.Should().Be(985_248);
        build.SystemsById["peer-c64"].Clock.TotalCycles.Should().Be(985_248);
        var drift = System.Math.Abs(build.SystemsById["drive-8"].Clock.TotalCycles - 1_000_000);
        drift.Should().BeLessThanOrEqualTo(1);
    }

    /// <summary>
    /// FR/TR: ARCH-TOPOLOGY-002
    /// Use case: Each bus is isolated - pulling DATA on IEC does not affect
    /// PB0 on UserPort, and vice versa.
    /// Acceptance: drive pulls IEC DATA low; UserPort PB0 still high.
    /// peer pulls UserPort PB0 low; IEC DATA reset to high (not affected).
    /// </summary>
    [Fact]
    public void Buses_AreIsolated_NoCrosstalkOnSimilarSignals()
    {
        var build = BuildRig();
        var iec = build.BusesById["IEC"];
        var up = build.BusesById["UserPort"];
        var driveEp = build.Endpoints[("drive-8", "IEC")];
        var peerEp = build.Endpoints[("peer-c64", "UserPort")];

        driveEp.Pull(IecInterSystemBus.Data, low: true);
        up.ReadLine(UserPortInterSystemBus.Pb0).Should().BeTrue("UserPort PB0 untouched by IEC traffic");

        peerEp.Pull(UserPortInterSystemBus.Pb0, low: true);
        iec.ReadLine(IecInterSystemBus.Data).Should().BeFalse("IEC DATA still pulled by drive endpoint");
    }
}
