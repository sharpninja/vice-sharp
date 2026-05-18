namespace ViceSharp.TestHarness.Multisystem;

using ViceSharp.Abstractions;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// Contract tests for InterSystemBus: wired-OR open-collector semantics,
/// edge events, detach recompute.
///
/// FR/TR: ARCH-MULTISYSTEM-001 (inter-system bus protocol primitive).
/// Use case: IEC bus connecting C64 host + 1541 drive. Either side can
/// assert (pull low) ATN/CLK/DATA; the line stays low if any endpoint pulls
/// low (wired-OR). Edge events drive protocol-level handshakes.
/// </summary>
public sealed class BusBridgingTests
{
    private static readonly string[] IecSignals = { "ATN", "CLK", "DATA" };

    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001
    /// Use case: A line with no endpoint pulls reads high.
    /// Acceptance: Newly constructed bus reports all signals high.
    /// </summary>
    [Fact]
    public void NewBus_AllSignalsRead_High()
    {
        var bus = new InterSystemBus("IEC", IecSignals);

        Assert.True(bus.ReadLine("ATN"));
        Assert.True(bus.ReadLine("CLK"));
        Assert.True(bus.ReadLine("DATA"));
    }

    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001
    /// Use case: Single endpoint pulls DATA low.
    /// Acceptance: Bus reports DATA low; the second endpoint also sees DATA
    /// low (cross-endpoint visibility).
    /// </summary>
    [Fact]
    public void OneEndpointPullsLow_OtherEndpointSeesLow()
    {
        var bus = new InterSystemBus("IEC", IecSignals);
        var a = bus.AttachEndpoint("C64");
        var b = bus.AttachEndpoint("1541");

        a.Pull("DATA", low: true);

        Assert.False(bus.ReadLine("DATA"));
        Assert.False(b.ReadLine("DATA"));
        Assert.True(bus.ReadLine("CLK"));
    }

    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001
    /// Use case: Two endpoints pull the same line; one releases.
    /// Acceptance: Line stays low until the last puller releases (wired-OR).
    /// </summary>
    [Fact]
    public void TwoEndpointsPullLow_LineStaysLow_UntilLastReleases()
    {
        var bus = new InterSystemBus("IEC", IecSignals);
        var a = bus.AttachEndpoint("C64");
        var b = bus.AttachEndpoint("1541");

        a.Pull("DATA", low: true);
        b.Pull("DATA", low: true);
        Assert.False(bus.ReadLine("DATA"));

        a.Pull("DATA", low: false);
        Assert.False(bus.ReadLine("DATA"));

        b.Pull("DATA", low: false);
        Assert.True(bus.ReadLine("DATA"));
    }

    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001
    /// Use case: LineChanged fires on resolved-state transitions, drives
    /// protocol-level state machines.
    /// Acceptance: A single endpoint pulling and releasing DATA fires
    /// exactly two LineChanged events (high-to-low, low-to-high) on DATA.
    /// </summary>
    [Fact]
    public void LineChanged_FiresOnce_PerResolvedTransition()
    {
        var bus = new InterSystemBus("IEC", IecSignals);
        var a = bus.AttachEndpoint("C64");
        var transitions = new List<BusEdgeEventArgs>();
        bus.LineChanged += (_, e) => transitions.Add(e);

        a.Pull("DATA", low: true);
        a.Pull("DATA", low: false);

        Assert.Equal(2, transitions.Count);
        Assert.Equal("DATA", transitions[0].Signal);
        Assert.False(transitions[0].NewState);
        Assert.Equal("DATA", transitions[1].Signal);
        Assert.True(transitions[1].NewState);
    }

    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001
    /// Use case: Redundant Pull calls must not fire spurious LineChanged
    /// events.
    /// Acceptance: Pulling low while already pulled-low fires no extra event.
    /// </summary>
    [Fact]
    public void RedundantPull_DoesNotFire_ExtraEvents()
    {
        var bus = new InterSystemBus("IEC", IecSignals);
        var a = bus.AttachEndpoint("C64");
        var transitions = new List<BusEdgeEventArgs>();
        bus.LineChanged += (_, e) => transitions.Add(e);

        a.Pull("DATA", low: true);
        a.Pull("DATA", low: true);
        a.Pull("DATA", low: true);

        Assert.Single(transitions);
    }

    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001
    /// Use case: Detaching an endpoint while it holds lines low must
    /// release those lines.
    /// Acceptance: After detach, line returns to high (no other pullers).
    /// </summary>
    [Fact]
    public void DetachingPullingEndpoint_ReleasesItsLines()
    {
        var bus = new InterSystemBus("IEC", IecSignals);
        var a = bus.AttachEndpoint("C64");
        a.Pull("DATA", low: true);

        bus.DetachEndpoint(a);

        Assert.True(bus.ReadLine("DATA"));
    }

    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001
    /// Use case: Unknown signal names indicate caller bug; bus must reject
    /// them rather than silently create new lines.
    /// Acceptance: ReadLine + Pull with unknown signal throw ArgumentException.
    /// </summary>
    [Fact]
    public void UnknownSignal_Throws()
    {
        var bus = new InterSystemBus("IEC", IecSignals);
        var a = bus.AttachEndpoint("C64");

        Assert.Throws<ArgumentException>(() => bus.ReadLine("SRQ"));
        Assert.Throws<ArgumentException>(() => a.Pull("SRQ", true));
    }
}
