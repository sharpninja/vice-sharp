namespace ViceSharp.TestHarness;

using ViceSharp.Monitor;
using Xunit;

/// <summary>
/// FR-MON-006 acceptance tests for watch expressions. Iteration 2.
/// The monitor has no watch command yet.
/// </summary>
public sealed class FrMon006AcceptanceTests
{
    /// <summary>
    /// FR: FR-MON-006.
    /// Use case: A watch can name a memory address or a range.
    /// Acceptance: "watch [$d020]" and "watch [$0400:$0427]" are accepted
    /// and listed back.
    /// </summary>
    [Fact]
    public void Watch_AcceptsAddressAndRange()
    {
        var monitor = new Monitor(MachineTestFactory.CreateC64Machine());

        var single = monitor.ExecuteCommand("watch [$d020]");
        var range = monitor.ExecuteCommand("watch [$0400:$0427]");
        var list = monitor.ExecuteCommand("watch");

        Assert.DoesNotContain("Unknown", single, StringComparison.Ordinal);
        Assert.DoesNotContain("Unknown", range, StringComparison.Ordinal);
        Assert.Contains("D020", list, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("0400", list, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// FR: FR-MON-006.
    /// Use case: A watch can name A, X, Y, SP, or PC.
    /// Acceptance: "watch A" is accepted and the list shows A.
    /// </summary>
    [Fact]
    public void Watch_AcceptsRegisters()
    {
        var monitor = new Monitor(MachineTestFactory.CreateC64Machine());

        var added = monitor.ExecuteCommand("watch A");
        var list = monitor.ExecuteCommand("watch");

        Assert.DoesNotContain("Unknown", added, StringComparison.Ordinal);
        Assert.Contains("A", list, StringComparison.Ordinal);
    }

    /// <summary>
    /// FR: FR-MON-006.
    /// Use case: A watch can add a byte to the following byte times 256.
    /// Acceptance: "watch [$fb] + [$fc] * 256" is accepted.
    /// </summary>
    [Fact]
    public void Watch_AcceptsPointerArithmetic()
    {
        var monitor = new Monitor(MachineTestFactory.CreateC64Machine());

        var added = monitor.ExecuteCommand("watch [$fb] + [$fc] * 256");

        Assert.DoesNotContain("Unknown", added, StringComparison.Ordinal);
        Assert.Contains("FC", added, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// FR: FR-MON-006.
    /// Use case: Watch values can be shown as hex, decimal, binary, or
    /// ASCII/PETSCII.
    /// Acceptance: "watch [$d020] decimal" reports a decimal value.
    /// </summary>
    [Fact]
    public void Watch_FormatsHexDecimalBinaryAndPetscii()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        machine.Bus.Write(0xD020, 0x0E);
        var monitor = new Monitor(machine);

        var shown = monitor.ExecuteCommand("watch [$d020] decimal");

        Assert.DoesNotContain("Unknown", shown, StringComparison.Ordinal);
        Assert.Contains("14", shown, StringComparison.Ordinal);
    }

    /// <summary>
    /// FR: FR-MON-006.
    /// Use case: An array watch shows a contiguous range.
    /// Acceptance: A four-byte watch shows four values.
    /// </summary>
    [Fact]
    public void ArrayWatch_ShowsAContiguousRange()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        machine.Bus.Write(0xC000, 0x01);
        machine.Bus.Write(0xC001, 0x02);
        machine.Bus.Write(0xC002, 0x03);
        machine.Bus.Write(0xC003, 0x04);
        var monitor = new Monitor(machine);

        var shown = monitor.ExecuteCommand("watch [$c000:$c003]");

        Assert.DoesNotContain("Unknown", shown, StringComparison.Ordinal);
        Assert.Contains("01", shown, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("04", shown, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// FR: FR-MON-006.
    /// Use case: Watches can be added, removed, and reordered.
    /// Acceptance: Removing the first watch leaves the second one first.
    /// </summary>
    [Fact]
    public void Watches_CanBeAddedRemovedAndReordered()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        machine.Bus.Write(0xC000, 0x11);
        var monitor = new Monitor(machine);
        monitor.ExecuteCommand("watch [$c000]");
        machine.Bus.Write(0xC000, 0x22);
        var refreshed = monitor.ExecuteCommand("watch");
        Assert.Contains("22", refreshed, StringComparison.OrdinalIgnoreCase);

        monitor.ExecuteCommand("watch A");
        monitor.ExecuteCommand("watch X");
        var reordered = monitor.ExecuteCommand("watch reorder 3 1");
        Assert.StartsWith("1 X", reordered, StringComparison.Ordinal);

        var removed = monitor.ExecuteCommand("unwatch 1");
        var list = monitor.ExecuteCommand("watch");

        Assert.DoesNotContain("Unknown", removed, StringComparison.Ordinal);
        Assert.Contains("A", list, StringComparison.Ordinal);
        Assert.DoesNotContain("X", list, StringComparison.Ordinal);
    }

    /// <summary>
    /// FR: FR-MON-006.
    /// Use case: A watch of SID OSC3 at $D41B is marked disruptive.
    /// Acceptance: The watch listing says the read can disturb the chip.
    /// </summary>
    [Fact]
    public void Osc3Watch_IsMarkedDisruptive()
    {
        var monitor = new Monitor(MachineTestFactory.CreateC64Machine());

        var shown = monitor.ExecuteCommand("watch [$d41b]");

        Assert.DoesNotContain("Unknown", shown, StringComparison.Ordinal);
        Assert.Contains("disrupt", shown, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// FR: FR-MON-006.
    /// Use case: The watch list survives closing and reopening the monitor
    /// in the same session.
    /// Acceptance: A second monitor object on the same machine still lists
    /// the watch.
    /// </summary>
    [Fact]
    public void WatchList_SurvivesMonitorReopen()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        new Monitor(machine).ExecuteCommand("watch A");

        var reopened = new Monitor(machine).ExecuteCommand("watch");

        Assert.DoesNotContain("Unknown", reopened, StringComparison.Ordinal);
        Assert.Contains("A", reopened, StringComparison.Ordinal);
    }

}
