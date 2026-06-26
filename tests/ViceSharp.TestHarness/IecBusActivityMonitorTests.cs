namespace ViceSharp.TestHarness;

using System;
using ViceSharp.Chips.IEC;
using ViceSharp.Host.Runtime;
using Xunit;

/// <summary>
/// TEST-IECMON-001 / FR-IECMON-001 / FR-IECSPY-001. The IEC activity monitor is the status
/// surface's live-traffic source: it counts every line transition on the async bus and reports
/// Active while any line is held low or a transition happened within the active-hold window,
/// Idle otherwise - so the status bar / monitor panel can show whether the IEC bus is busy.
/// </summary>
public sealed class IecBusActivityMonitorTests
{
    private sealed class ManualTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UnixEpoch;

        public override DateTimeOffset GetUtcNow() => Now;
    }

    /// <summary>
    /// FR: FR-IECMON-001, TR: TR-SYS-SCHED-001, TEST-IECMON-001.
    /// Use case: with nothing on the bus, the status surface must read Idle, no false activity.
    /// Acceptance: a fresh monitor reports zero transitions, null last-activity, and Idle.
    /// </summary>
    [Fact]
    public void FreshMonitor_IsIdle()
    {
        var bus = IecInterSystemBus.Create();
        bus.AttachEndpoint("c64");
        var monitor = new IecBusActivityMonitor(bus, timeProvider: new ManualTimeProvider());

        Assert.Equal(0, monitor.TransitionCount);
        Assert.Null(monitor.LastActivityUtc);
        Assert.False(monitor.IsActive);
        Assert.Equal("Idle", monitor.ActivityState);
    }

    /// <summary>
    /// FR: FR-IECMON-001, TR: TR-SYS-SCHED-001, TEST-IECMON-001.
    /// Use case: the monitor must tally bus edges (a transfer is many edges) and read Active
    ///   while a line is held low - the visible sign of an in-progress IEC transaction.
    /// Acceptance: each pull/release raises the transition count and stamps last-activity; while
    ///   a line is held low the monitor is Active regardless of the hold window.
    /// </summary>
    [Fact]
    public void Transitions_AreCounted_AndLowLineIsAlwaysActive()
    {
        var time = new ManualTimeProvider();
        var bus = IecInterSystemBus.Create();
        var c64 = bus.AttachEndpoint("c64");
        var monitor = new IecBusActivityMonitor(bus, activeHold: TimeSpan.FromMilliseconds(500), timeProvider: time);

        c64.Pull(IecInterSystemBus.Atn, low: true); // edge 1, line now low

        Assert.Equal(1, monitor.TransitionCount);
        Assert.Equal(time.Now, monitor.LastActivityUtc);
        Assert.True(monitor.IsActive);

        time.Now = time.Now.AddSeconds(10); // far past the hold window...
        Assert.True(monitor.IsActive);       // ...but a held-low line keeps it Active

        c64.Pull(IecInterSystemBus.Atn, low: false); // edge 2, line released
        Assert.Equal(2, monitor.TransitionCount);
    }

    /// <summary>
    /// FR: FR-IECMON-001, TR: TR-SYS-SCHED-001, TEST-IECMON-001.
    /// Use case: after a transfer ends (all lines released) the surface should still read Active
    ///   briefly so a flicker of traffic is visible, then settle to Idle.
    /// Acceptance: with no line held low, the monitor is Active within ActiveHold of the last
    ///   transition and Idle once the hold elapses.
    /// </summary>
    [Fact]
    public void GoesIdle_AfterActiveHoldElapses_WithNoLineLow()
    {
        var time = new ManualTimeProvider();
        var bus = IecInterSystemBus.Create();
        var c64 = bus.AttachEndpoint("c64");
        var monitor = new IecBusActivityMonitor(bus, activeHold: TimeSpan.FromMilliseconds(500), timeProvider: time);

        c64.Pull(IecInterSystemBus.Clk, low: true);
        c64.Pull(IecInterSystemBus.Clk, low: false); // brief blip, no line left low

        time.Now = time.Now.AddMilliseconds(400); // within the 500 ms hold
        Assert.True(monitor.IsActive);

        time.Now = time.Now.AddMilliseconds(200); // now 600 ms since the blip
        Assert.False(monitor.IsActive);
        Assert.Equal("Idle", monitor.ActivityState);
    }
}
