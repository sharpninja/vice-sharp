namespace ViceSharp.TestHarness;

using System.Linq;
using FluentAssertions;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-IECMON-001 / TR-IECTRACE-001 / TEST-IECTRACE-001.
/// Use case: The IEC scope records a cycle-stamped trace of the bus - an entry
/// per line edge plus an entry at each emulator step boundary - into a bounded
/// ring, without perturbing the bus. The trace is the data the timing diagram
/// and protocol decoder consume.
/// </summary>
public sealed class IecBusTraceRecorderTests
{
    private sealed class FakeClock
    {
        public long Cycle;
        public long Read() => Cycle;
    }

    /// <summary>
    /// FR: FR-IECMON-001, TR: TR-IECTRACE-001.
    /// Use case: Opening the scope must seed the trace with the current bus
    /// state so the timing diagram has a starting point.
    /// Acceptance: Starting the recorder captures a baseline step sample of the
    /// current bus state at the current cycle.
    /// </summary>
    [Fact]
    public void Start_CapturesBaselineStepSample()
    {
        var bus = IecInterSystemBus.Create();
        bus.AttachEndpoint("c64");
        var clock = new FakeClock { Cycle = 42 };
        using var rec = new IecBusTraceRecorder(bus, clock.Read);

        rec.Start();

        var samples = rec.GetSamples();
        samples.Should().HaveCount(1);
        samples[0].Kind.Should().Be(IecBusTraceRecorder.StepKind);
        samples[0].Cycle.Should().Be(42);
        samples[0].Bus.Lines.Should().OnlyContain(l => l.IsHigh);
    }

    /// <summary>
    /// FR: FR-IECMON-001, TR: TR-IECTRACE-001.
    /// Use case: Every IEC line transition must be captured as a cycle-stamped
    /// edge so the protocol decoder can reconstruct traffic.
    /// Acceptance: Each line edge appends a cycle-stamped edge sample naming the
    /// changed signal and snapshotting the resulting bus state.
    /// </summary>
    [Fact]
    public void Edge_AppendsCycleStampedSampleWithSignalAndState()
    {
        var bus = IecInterSystemBus.Create();
        var drive = bus.AttachEndpoint("drive-8");
        var clock = new FakeClock { Cycle = 100 };
        using var rec = new IecBusTraceRecorder(bus, clock.Read);
        rec.Start();

        clock.Cycle = 105;
        drive.Pull(IecInterSystemBus.Data, low: true);

        var edge = rec.GetSamples().Single(s => s.Kind == IecBusTraceRecorder.EdgeKind);
        edge.Cycle.Should().Be(105);
        edge.Signal.Should().Be(IecInterSystemBus.Data);
        edge.Bus.Lines.Single(l => l.Signal == IecInterSystemBus.Data).IsAsserted.Should().BeTrue();
        edge.Bus.TalkingEndpoints.Should().Equal("drive-8");
    }

    /// <summary>
    /// FR: FR-IECMON-001, TR: TR-IECTRACE-001.
    /// Use case: Step boundaries must appear in the trace even when no line
    /// changed so the diagram has a time axis when the bus is quiet.
    /// Acceptance: MarkStep records a step sample even when no line changed.
    /// </summary>
    [Fact]
    public void MarkStep_RecordsSampleWithoutEdge()
    {
        var bus = IecInterSystemBus.Create();
        bus.AttachEndpoint("c64");
        var clock = new FakeClock { Cycle = 1 };
        using var rec = new IecBusTraceRecorder(bus, clock.Read);
        rec.Start();

        clock.Cycle = 2;
        rec.MarkStep();

        rec.GetSamples().Count(s => s.Kind == IecBusTraceRecorder.StepKind).Should().Be(2);
        rec.GetSamples().Last().Cycle.Should().Be(2);
    }

    /// <summary>
    /// FR: FR-IECMON-001, TR: TR-IECTRACE-001.
    /// Use case: The trace ring is bounded; beyond capacity the oldest samples
    /// must be evicted so memory stays bounded during long runs.
    /// Acceptance: The ring evicts oldest samples beyond capacity, preserving
    /// chronological order of the survivors.
    /// </summary>
    [Fact]
    public void Ring_EvictsOldestBeyondCapacity()
    {
        var bus = IecInterSystemBus.Create();
        var drive = bus.AttachEndpoint("drive-8");
        var clock = new FakeClock();
        using var rec = new IecBusTraceRecorder(bus, clock.Read, capacity: 4);
        rec.Start(); // 1 baseline sample

        for (var i = 1; i <= 10; i++)
        {
            clock.Cycle = i;
            drive.Pull(IecInterSystemBus.Clk, low: (i % 2) == 1);
        }

        var samples = rec.GetSamples();
        samples.Should().HaveCount(4);
        samples.Select(s => s.Cycle).Should().BeInAscendingOrder();
        samples.Last().Cycle.Should().Be(10);
    }

    /// <summary>
    /// FR: FR-IECMON-001, TR: TR-IECTRACE-001.
    /// Use case: Closing the scope must stop recording so the trace stays
    /// inactive (and cost-free) when the monitor is not open.
    /// Acceptance: After Stop, further edges are not recorded.
    /// </summary>
    [Fact]
    public void Stop_HaltsRecording()
    {
        var bus = IecInterSystemBus.Create();
        var drive = bus.AttachEndpoint("drive-8");
        var clock = new FakeClock();
        using var rec = new IecBusTraceRecorder(bus, clock.Read);
        rec.Start();
        var countAfterStart = rec.Count;

        rec.Stop();
        drive.Pull(IecInterSystemBus.Atn, low: true);

        rec.Count.Should().Be(countAfterStart);
    }

    /// <summary>
    /// FR: FR-IECMON-001, TR: TR-IECTRACE-001.
    /// Use case: The scope fetches only new samples since the last poll cycle so
    /// the UI can incrementally append to the timing diagram.
    /// Acceptance: Since(cycle) returns only samples at or after the given cycle.
    /// </summary>
    [Fact]
    public void Since_FiltersByCycle()
    {
        var bus = IecInterSystemBus.Create();
        var drive = bus.AttachEndpoint("drive-8");
        var clock = new FakeClock { Cycle = 10 };
        using var rec = new IecBusTraceRecorder(bus, clock.Read);
        rec.Start();
        clock.Cycle = 20; drive.Pull(IecInterSystemBus.Data, low: true);
        clock.Cycle = 30; drive.Pull(IecInterSystemBus.Data, low: false);

        rec.Since(20).Select(s => s.Cycle).Should().Equal(20, 30);
    }
}
