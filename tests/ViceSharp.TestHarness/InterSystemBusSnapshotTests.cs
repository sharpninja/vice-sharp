namespace ViceSharp.TestHarness;

using System.Linq;
using FluentAssertions;
using ViceSharp.Chips.IEC;
using Xunit;

/// <summary>
/// FR/TR: RUNTIME-IECSPY-001 / TR-IECBUS-OBSERV-001.
/// Use case: An operator (or the UI / a diagnostic) must be able to spy on the
/// IEC bus at any instant and see the level of every line plus which attached
/// device is pulling it - i.e. who is "talking". Snapshotting is pure
/// observation and must never perturb the resolved bus state.
/// </summary>
public sealed class InterSystemBusSnapshotTests
{
    /// <summary>
    /// FR: RUNTIME-IECSPY-001, TR: TR-IECBUS-OBSERV-001.
    /// Use case: Spying an idle IEC bus must show every line high with no
    /// talkers so the diagnostic shows a quiescent bus.
    /// Acceptance: On an idle IEC bus every line reads high, no line has
    /// pullers, and no endpoint is talking; all attached endpoints are listed.
    /// </summary>
    [Fact]
    public void Snapshot_IdleBus_AllLinesHigh_NoTalkers()
    {
        var bus = IecInterSystemBus.Create();
        bus.AttachEndpoint("c64");
        bus.AttachEndpoint("drive-8");

        var snapshot = bus.Snapshot();

        snapshot.BusName.Should().Be(IecInterSystemBus.BusName);
        snapshot.Endpoints.Should().BeEquivalentTo("c64", "drive-8");
        snapshot.Lines.Should().OnlyContain(line => line.IsHigh && line.Pullers.Count == 0);
        snapshot.TalkingEndpoints.Should().BeEmpty();
    }

    /// <summary>
    /// FR: RUNTIME-IECSPY-001, TR: TR-IECBUS-OBSERV-001.
    /// Use case: When a drive pulls DATA low the spy must identify the asserted
    /// line and which endpoint is talking.
    /// Acceptance: When the drive pulls DATA low, the snapshot reports DATA
    /// asserted with the drive as its sole puller, other lines high, and the
    /// drive listed as the talking endpoint.
    /// </summary>
    [Fact]
    public void Snapshot_DrivePullsData_ReportsLineLowAndTalker()
    {
        var bus = IecInterSystemBus.Create();
        bus.AttachEndpoint("c64");
        var drive = bus.AttachEndpoint("drive-8");

        drive.Pull(IecInterSystemBus.Data, low: true);
        var snapshot = bus.Snapshot();

        var data = snapshot.Lines.Single(l => l.Signal == IecInterSystemBus.Data);
        data.IsHigh.Should().BeFalse();
        data.IsAsserted.Should().BeTrue();
        data.Pullers.Should().Equal("drive-8");

        snapshot.Lines.Where(l => l.Signal != IecInterSystemBus.Data)
            .Should().OnlyContain(l => l.IsHigh);
        snapshot.TalkingEndpoints.Should().Equal("drive-8");
    }

    /// <summary>
    /// FR: RUNTIME-IECSPY-001, TR: TR-IECBUS-OBSERV-001.
    /// Use case: Wired-AND contention (two endpoints pulling one line) must be
    /// fully attributed by the spy.
    /// Acceptance: Two endpoints pulling the same line (wired-AND) both appear
    /// in that line's puller list and as talking endpoints.
    /// </summary>
    [Fact]
    public void Snapshot_MultiplePullers_ListsAll()
    {
        var bus = IecInterSystemBus.Create();
        var c64 = bus.AttachEndpoint("c64");
        var drive = bus.AttachEndpoint("drive-8");

        c64.Pull(IecInterSystemBus.Clk, low: true);
        drive.Pull(IecInterSystemBus.Clk, low: true);
        var snapshot = bus.Snapshot();

        var clk = snapshot.Lines.Single(l => l.Signal == IecInterSystemBus.Clk);
        clk.IsHigh.Should().BeFalse();
        clk.Pullers.Should().BeEquivalentTo("c64", "drive-8");
        snapshot.TalkingEndpoints.Should().BeEquivalentTo("c64", "drive-8");
    }

    /// <summary>
    /// FR: RUNTIME-IECSPY-001, TR: TR-IECBUS-OBSERV-001.
    /// Use case: After an endpoint releases a line the spy must show the line
    /// recovered to high with no talkers.
    /// Acceptance: Releasing a pull restores the line to high with no pullers.
    /// </summary>
    [Fact]
    public void Snapshot_AfterRelease_LineHighAgain()
    {
        var bus = IecInterSystemBus.Create();
        var drive = bus.AttachEndpoint("drive-8");

        drive.Pull(IecInterSystemBus.Atn, low: true);
        drive.Pull(IecInterSystemBus.Atn, low: false);
        var snapshot = bus.Snapshot();

        snapshot.Lines.Single(l => l.Signal == IecInterSystemBus.Atn).IsHigh.Should().BeTrue();
        snapshot.TalkingEndpoints.Should().BeEmpty();
    }

    /// <summary>
    /// FR: RUNTIME-IECSPY-001, TR: TR-IECBUS-OBSERV-001.
    /// Use case: Snapshotting is pure observation and must never perturb the
    /// resolved bus state, even across repeated reads.
    /// Acceptance: Taking a snapshot does not change resolved bus state.
    /// </summary>
    [Fact]
    public void Snapshot_DoesNotMutateBus()
    {
        var bus = IecInterSystemBus.Create();
        var drive = bus.AttachEndpoint("drive-8");
        drive.Pull(IecInterSystemBus.Data, low: true);

        _ = bus.Snapshot();
        _ = bus.Snapshot();

        bus.ReadLine(IecInterSystemBus.Data).Should().BeFalse();
        bus.ReadLine(IecInterSystemBus.Clk).Should().BeTrue();
    }
}
