namespace ViceSharp.TestHarness.Wiring;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.Multisystem;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: ARCH-TOPOLOGY-001 (Phase H2).
/// Use case: A multi-system topology with two 1541 drives (devices 8 + 9)
/// + a C64 host. Each drive runs on its own 1MHz clock; both attach to
/// the same IEC bus. The auto-bind picks the right device number per
/// drive so jumper bits PB5/PB6 distinguish them. Proves the substrate
/// scales beyond a single peripheral.
/// </summary>
public sealed class TwoDriveTopologyTests
{
    private const string TwoDriveTopology = """
        schemaVersion: 1
        coordinator:
          host:
            id: c64-host
            kind: C64
            busAttachments:
              - busId: IEC
                endpointName: c64
          peripherals:
            - id: drive-8
              kind: C1541
              deviceNumber: 8
              busAttachments:
                - busId: IEC
                  endpointName: drive-8
            - id: drive-9
              kind: C1541
              deviceNumber: 9
              busAttachments:
                - busId: IEC
                  endpointName: drive-9
          buses:
            - id: IEC
              signals: [ATN, CLK, DATA, SRQ]
        """;

    private static MultiSystemBuildResult BuildRig()
    {
        var provider = MachineTestFactory.CreateC64RomProvider();
        var bp = new MultiSystemYamlLoader().LoadFromString(TwoDriveTopology);
        return bp.BuildCoordinatorAuto(new ArchitectureBuilder(provider));
    }

    /// <summary>
    /// FR/TR: ARCH-TOPOLOGY-001
    /// Use case: After build, the coordinator owns three systems: C64,
    /// drive-8, drive-9 - each with its own clock at the expected rate.
    /// Acceptance: SystemsById contains all three; clocks match expected
    /// frequencies.
    /// </summary>
    [Fact]
    public void Build_ProducesThreeSystems_WithExpectedClocks()
    {
        var build = BuildRig();

        build.SystemsById.Keys.Should().BeEquivalentTo(new[] { "c64-host", "drive-8", "drive-9" });
        build.SystemsById["c64-host"].Clock.FrequencyHz.Should().Be(985_248);
        build.SystemsById["drive-8"].Clock.FrequencyHz.Should().Be(1_000_000);
        build.SystemsById["drive-9"].Clock.FrequencyHz.Should().Be(1_000_000);
    }

    /// <summary>
    /// FR/TR: ARCH-TOPOLOGY-001
    /// Use case: Each drive's VIA1 PB5..PB6 reflects its declared device
    /// number jumper.
    /// Acceptance: drive-8 PB & $60 = $00; drive-9 PB & $60 = $20.
    /// </summary>
    [Fact]
    public void EachDrive_VIA1_HasDeviceNumberJumpers()
    {
        var build = BuildRig();

        var drive8Via1 = build.SystemsById["drive-8"].Devices.GetAll<Via6522>()
            .OrderBy(v => v.BaseAddress).First();
        var drive9Via1 = build.SystemsById["drive-9"].Devices.GetAll<Via6522>()
            .OrderBy(v => v.BaseAddress).First();
        drive8Via1.Write(0x1802, 0x1A);
        drive9Via1.Write(0x1802, 0x1A);

        (drive8Via1.Read(0x1800) & 0x60).Should().Be(0x00);
        (drive9Via1.Read(0x1800) & 0x60).Should().Be(0x20);
    }

    /// <summary>
    /// FR/TR: ARCH-TOPOLOGY-001
    /// Use case: Both drives plus the host run their respective ROMs under
    /// a single coordinator; PCs advance from reset within the cycle budget.
    /// Acceptance: After Step(200_000) all three machines have PCs
    /// different from their reset values.
    /// </summary>
    [Fact]
    public void Coordinator_RunsAllThreeMachines_PcsAdvance()
    {
        var build = BuildRig();
        foreach (var m in build.SystemsById.Values) m.Reset();
        var resetPcs = build.SystemsById.ToDictionary(kv => kv.Key, kv => kv.Value.GetState().PC);

        build.Coordinator.Step(200_000);

        foreach (var (id, m) in build.SystemsById)
            m.GetState().PC.Should().NotBe(resetPcs[id], $"{id} should have advanced past its reset vector");
    }

    /// <summary>
    /// FR/TR: ARCH-TOPOLOGY-001
    /// Use case: The IEC bus is shared by all three endpoints. Either drive
    /// can pull DATA and the host endpoint observes the wired-OR result.
    /// Acceptance: drive-9 pulls DATA -> host endpoint reads DATA low even
    /// though drive-8 is releasing.
    /// </summary>
    [Fact]
    public void IecBus_WiredOr_Across_AllThreeEndpoints()
    {
        var build = BuildRig();
        var hostEp = build.Endpoints[("c64-host", "IEC")];
        var d8Ep = build.Endpoints[("drive-8", "IEC")];
        var d9Ep = build.Endpoints[("drive-9", "IEC")];

        d9Ep.Pull(IecInterSystemBus.Data, low: true);

        hostEp.ReadLine(IecInterSystemBus.Data).Should().BeFalse();
        d8Ep.ReadLine(IecInterSystemBus.Data).Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: ARCH-TOPOLOGY-001
    /// Use case: The drive clocks advance proportionally to the host clock
    /// under the coordinator even with two peripherals.
    /// Acceptance: After Step(985_248) all three machines have advanced by
    /// their expected cycle counts within 1-cycle drift.
    /// </summary>
    [Fact]
    public void Coordinator_AdvancesAllClocks_AtTheirOwnRates()
    {
        var build = BuildRig();

        build.Coordinator.Step(985_248);

        build.SystemsById["c64-host"].Clock.TotalCycles.Should().Be(985_248);
        var expectedDrive = 985_248L * 1_000_000 / 985_248;
        System.Math.Abs(build.SystemsById["drive-8"].Clock.TotalCycles - expectedDrive)
            .Should().BeLessThanOrEqualTo(1);
        System.Math.Abs(build.SystemsById["drive-9"].Clock.TotalCycles - expectedDrive)
            .Should().BeLessThanOrEqualTo(1);
    }

    /// <summary>
    /// FR/TR: ARCH-TOPOLOGY-001
    /// Use case: A tracer attached to the shared IEC bus records traffic
    /// driven by the host CIA2 during boot. Both drives are passive
    /// listeners (with their VIA1 binding); they don't drive ATN.
    /// Acceptance: After 200k host cycles, tracer captured > 0 events.
    /// </summary>
    [Fact]
    public void TwoDriveBoot_TracerObserves_IecTraffic()
    {
        var build = BuildRig();
        var tracer = new InterSystemBusTracer(build.BusesById["IEC"]);
        tracer.Attach();

        build.Coordinator.Step(200_000);

        tracer.Events.Count.Should().BeGreaterThan(0);
    }
}
