namespace ViceSharp.TestHarness;

using ViceSharp.Avalonia.ViewModels;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// TEST-IECMON-001 / FR-IECMON-001 / FR-IECSPY-001. The IEC monitor panel renders each live IEC
/// line's resolved state and pullers plus the activity summary from the host status, and stays
/// hidden when the session has no IEC bus (a bare C64 with no drive).
/// </summary>
public sealed class IecMonitorViewModelTests
{
    private static EmulatorStatusDto BaseStatus() => new(
        "session",
        "Commodore 64",
        EmulatorRunState.Running,
        0,
        new MachineStateDto(0, 0, 0, 0, 0, 0, 0));

    /// <summary>
    /// FR: FR-IECMON-001, TR: TR-SYS-SCHED-001, TEST-IECMON-001.
    /// Use case: a single-system C64 has no IEC bus; the monitor panel must hide itself rather
    ///   than show empty/garbage lines.
    /// Acceptance: a status with no IecBusLines leaves HasBus false and Lines empty.
    /// </summary>
    [Fact]
    public void NoIecBus_HidesPanel()
    {
        var vm = new IecMonitorViewModel();

        vm.ApplyStatus(BaseStatus(), RpcStatus.Ok());

        Assert.False(vm.HasBus);
        Assert.Empty(vm.Lines);
    }

    /// <summary>
    /// FR: FR-IECMON-001, FR: FR-IECSPY-001, TR: TR-SYS-SCHED-001, TEST-IECMON-001.
    /// Use case: during a true-drive transaction the panel must show which lines are low and who
    ///   is pulling them, plus that the bus is Active, so the user can watch the IEC protocol.
    /// Acceptance: a status carrying IEC lines surfaces HasBus true, one formatted entry per line
    ///   (with pullers when low), and the activity summary + transition count.
    /// </summary>
    [Fact]
    public void WithIecBus_ListsLinesPullersAndActivity()
    {
        var vm = new IecMonitorViewModel();
        var status = BaseStatus() with
        {
            IecBusActive = true,
            IecBusActivityState = "Active",
            IecBusTransitionCount = 42,
            IecBusLines = new[]
            {
                new IecBusLineDto("ATN", IsHigh: false, Pullers: "c64"),
                new IecBusLineDto("CLK", IsHigh: true, Pullers: ""),
                new IecBusLineDto("DATA", IsHigh: false, Pullers: "drive-8"),
            },
        };

        vm.ApplyStatus(status, RpcStatus.Ok());

        Assert.True(vm.HasBus);
        Assert.Equal("Active", vm.Activity);
        Assert.Equal(42, vm.Transitions);
        Assert.Equal(3, vm.Lines.Count);
        Assert.Equal("ATN: low (c64)", vm.Lines[0]);
        Assert.Equal("CLK: high", vm.Lines[1]);
        Assert.Equal("DATA: low (drive-8)", vm.Lines[2]);
    }
}
