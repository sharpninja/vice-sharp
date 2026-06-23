namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.IEC;
using Xunit;

/// <summary>
/// TEST-SYSINDEP-001 (AC2) / FR-SYSINDEP-001 / TR-SYS-SCHED-001. The async wired-OR IEC bus is
/// the ONLY coupling between independently-clocked systems: when one endpoint pulls a line, the
/// resolved level is visible to every other endpoint at that instant, and the LineChanged edge
/// fires synchronously - so a system that reacts to the edge (the drive's VIA CA1 latch) sees
/// the transition before it would next read the line. No cycle-lockstep is required for one
/// system to observe another's bus activity.
/// </summary>
public sealed class InterSystemBusEdgeOrderingTests
{
    /// <summary>
    /// FR: FR-SYSINDEP-001, TR: TR-SYS-SCHED-001, TEST-SYSINDEP-001 (AC2).
    /// Use case: the C64 pulls ATN low to signal the drive; the drive's endpoint must observe
    ///   ATN low immediately (the wired-OR bus is not quantized to either CPU's clock).
    /// Acceptance: after one endpoint pulls a line low, every other endpoint reads it low at
    ///   once, and releasing it returns every endpoint to high (wired-OR resolution).
    /// </summary>
    [Fact]
    public void Pull_IsObservedByOtherEndpoint_Immediately()
    {
        var bus = IecInterSystemBus.Create();
        var c64 = bus.AttachEndpoint("c64");
        var drive = bus.AttachEndpoint("drive-8");

        Assert.True(drive.ReadLine(IecInterSystemBus.Atn));   // idle: released => high

        c64.Pull(IecInterSystemBus.Atn, low: true);
        Assert.False(drive.ReadLine(IecInterSystemBus.Atn));  // observed low at once, no stepping
        Assert.False(bus.ReadLine(IecInterSystemBus.Atn));

        c64.Pull(IecInterSystemBus.Atn, low: false);
        Assert.True(drive.ReadLine(IecInterSystemBus.Atn));   // released => wired-OR back to high
    }

    /// <summary>
    /// FR: FR-SYSINDEP-001, TR: TR-SYS-SCHED-001, TEST-SYSINDEP-001 (AC2).
    /// Use case: the drive latches the ATN falling edge on its VIA CA1 via the bus LineChanged
    ///   event; the event must fire at the moment of the pull, before any later read.
    /// Acceptance: pulling a line low raises LineChanged once with NewState=false for that
    ///   signal, synchronously with the pull (so the edge is delivered before the next read).
    /// </summary>
    [Fact]
    public void LineChanged_FiresSynchronouslyOnPull_BeforeAnyRead()
    {
        var bus = IecInterSystemBus.Create();
        var c64 = bus.AttachEndpoint("c64");
        var drive = bus.AttachEndpoint("drive-8");

        var edges = 0;
        var lastSignal = "";
        var lastState = true;
        var observedLowAtEdge = true; // capture the read AS SEEN by the reacting system at the edge
        bus.LineChanged += (_, e) =>
        {
            edges++;
            lastSignal = e.Signal;
            lastState = e.NewState;
            observedLowAtEdge = drive.ReadLine(IecInterSystemBus.Atn);
        };

        c64.Pull(IecInterSystemBus.Atn, low: true);

        Assert.Equal(1, edges);                       // exactly one edge for the transition
        Assert.Equal(IecInterSystemBus.Atn, lastSignal);
        Assert.False(lastState);                      // edge reports the new (low) state
        Assert.False(observedLowAtEdge);              // the reacting system already reads it low
    }

    /// <summary>
    /// FR: FR-SYSINDEP-001, TR: TR-SYS-SCHED-001, TEST-SYSINDEP-001 (AC2).
    /// Use case: multiple devices may hold a line low at once (C64 + drive both asserting);
    ///   the line stays low until the LAST puller releases - the open-collector behaviour the
    ///   real IEC bus has, independent of any device's clock.
    /// Acceptance: with two endpoints pulling, releasing one keeps the line low; releasing the
    ///   second returns it high, and an edge fires only on the resolved-state change.
    /// </summary>
    [Fact]
    public void WiredOr_LineStaysLowUntilLastPullerReleases()
    {
        var bus = IecInterSystemBus.Create();
        var c64 = bus.AttachEndpoint("c64");
        var drive = bus.AttachEndpoint("drive-8");

        var edges = 0;
        bus.LineChanged += (_, _) => edges++;

        c64.Pull(IecInterSystemBus.Data, low: true);   // first puller: high -> low (1 edge)
        drive.Pull(IecInterSystemBus.Data, low: true); // second puller: still low (no edge)
        Assert.False(bus.ReadLine(IecInterSystemBus.Data));
        Assert.Equal(1, edges);

        c64.Pull(IecInterSystemBus.Data, low: false);  // one releases: still held low by drive
        Assert.False(bus.ReadLine(IecInterSystemBus.Data));
        Assert.Equal(1, edges);

        drive.Pull(IecInterSystemBus.Data, low: false); // last releases: low -> high (1 edge)
        Assert.True(bus.ReadLine(IecInterSystemBus.Data));
        Assert.Equal(2, edges);
    }
}
