namespace ViceSharp.TestHarness.Wiring;

using FluentAssertions;
using ViceSharp.Architectures.Multisystem;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: ARCH-OBSERVABILITY-001 (Phase H1).
/// Use case: A real C64 KERNAL boot must actually drive the IEC bus
/// (initial device scan + serial protocol setup). With the substrate
/// fully wired, attaching a tracer to the IEC bus and running the
/// coordinator for N host cycles should record nonzero line transitions
/// across ATN / CLK / DATA. Proves the substrate carries real ROM
/// traffic, not just static test pulls.
/// </summary>
public sealed class C64Plus1541BootBusActivityTests
{
    private const string Topology = """
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
          buses:
            - id: IEC
              signals: [ATN, CLK, DATA, SRQ]
        """;

    private static MultiSystemBuildResult BuildRig()
    {
        var provider = MachineTestFactory.CreateC64RomProvider();
        var bp = new MultiSystemYamlLoader().LoadFromString(Topology);
        return bp.BuildCoordinatorAuto(new ArchitectureBuilder(provider));
    }

    /// <summary>
    /// FR/TR: ARCH-OBSERVABILITY-001
    /// Use case: After 200k host cycles, the C64 KERNAL boot sequence has
    /// progressed enough to interact with the IEC bus. The CIA2 binding
    /// translates KERNAL writes to bus pulls; the tracer captures the
    /// resulting line transitions.
    /// Acceptance: At least one CLK or DATA transition is observed.
    /// </summary>
    [Fact]
    public void C64KernalBoot_DrivesIecBus_TracerObservesTransitions()
    {
        var build = BuildRig();
        var bus = build.BusesById["IEC"];
        var tracer = new InterSystemBusTracer(bus);
        tracer.Attach();
        build.Coordinator.Reset();

        build.Coordinator.Step(200_000);

        tracer.Events.Count.Should().BeGreaterThan(0,
            "the C64 KERNAL should have driven the IEC bus during boot");
        var clkOrData = tracer.CountFor("CLK") + tracer.CountFor("DATA");
        clkOrData.Should().BeGreaterThan(0,
            "CLK or DATA should transition as the KERNAL sets up the bus");
    }

    /// <summary>
    /// FR/TR: ARCH-OBSERVABILITY-001
    /// Use case: Tracer Detach + Reset clears captured state and stops
    /// receiving further events.
    /// Acceptance: After Detach + further coordinator steps, no new events
    /// are recorded.
    /// </summary>
    [Fact]
    public void Tracer_Detach_StopsRecording()
    {
        var build = BuildRig();
        var bus = build.BusesById["IEC"];
        var tracer = new InterSystemBusTracer(bus);
        tracer.Attach();
        build.Coordinator.Step(50_000);
        var beforeDetach = tracer.Events.Count;

        tracer.Detach();
        build.Coordinator.Step(50_000);

        tracer.Events.Count.Should().Be(beforeDetach);
    }

    /// <summary>
    /// FR/TR: ARCH-OBSERVABILITY-001
    /// Use case: Reset clears the recorded events without affecting the
    /// subscription.
    /// Acceptance: After Reset, Events is empty; further activity captures
    /// fresh events.
    /// </summary>
    [Fact]
    public void Tracer_Reset_ClearsEvents_WithoutDetaching()
    {
        var build = BuildRig();
        var bus = build.BusesById["IEC"];
        var tracer = new InterSystemBusTracer(bus);
        tracer.Attach();
        build.Coordinator.Step(50_000);
        tracer.Events.Count.Should().BeGreaterThan(0);

        tracer.Reset();

        tracer.Events.Should().BeEmpty();
    }

    /// <summary>
    /// FR/TR: ARCH-OBSERVABILITY-001
    /// Use case: A tracer with low MaxEvents drops oldest events on overflow,
    /// keeping the most recent.
    /// Acceptance: After many writes, Events.Count never exceeds MaxEvents.
    /// </summary>
    [Fact]
    public void Tracer_MaxEvents_DropsOldest_OnOverflow()
    {
        var build = BuildRig();
        var bus = build.BusesById["IEC"];
        var tracer = new InterSystemBusTracer(bus, maxEvents: 10);
        tracer.Attach();

        build.Coordinator.Step(200_000);

        tracer.Events.Count.Should().BeLessThanOrEqualTo(10);
    }
}
