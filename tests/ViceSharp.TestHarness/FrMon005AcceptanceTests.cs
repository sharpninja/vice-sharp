namespace ViceSharp.TestHarness;

using ViceSharp.Architectures.C1541;
using ViceSharp.Core;
using ViceSharp.Monitor;
using Xunit;

/// <summary>
/// FR-MON-005 acceptance tests for monitor bank views. Iteration 2.
/// The monitor command set does not yet have a bank selector.
/// </summary>
public sealed class FrMon005AcceptanceTests
{
    /// <summary>
    /// FR: FR-MON-005.
    /// Use case: The CPU view shows memory the way the CPU sees it, including
    /// the BASIC ROM overlay.
    /// Acceptance: A RAM poke under $A000 does not appear in the CPU memory
    /// dump. The dump is not an unknown-command error.
    /// </summary>
    [Fact]
    public void CpuView_ShowsTheCpuMapping()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var basic = machine.Bus.Read(0xA000);
        var ram = Assert.IsAssignableFrom<ViceSharp.Abstractions.IMemory>(
            machine.Devices.GetByRole(ViceSharp.Abstractions.DeviceRole.SystemRam));
        ram.Span[0xA000] = (byte)(basic ^ 0xFF);

        var dump = new Monitor(machine).ExecuteCommand("m a000");

        Assert.DoesNotContain("Unknown", dump, StringComparison.Ordinal);
        // The 128-byte BASIC window contains (basic XOR $FF) later in the
        // dump. The CPU-visible byte is the first data byte on the line.
        Assert.StartsWith($"A000  {basic:X2} ", dump, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// FR: FR-MON-005.
    /// Use case: The RAM view ignores ROM overlays.
    /// Acceptance: "bank ram" then a dump of $A000 shows the poked RAM byte.
    /// </summary>
    [Fact]
    public void RamView_IgnoresRomOverlays()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var monitor = new Monitor(machine);
        var ram = Assert.IsAssignableFrom<ViceSharp.Abstractions.IMemory>(
            machine.Devices.GetByRole(ViceSharp.Abstractions.DeviceRole.SystemRam));
        ram.Span[0xA000] = 0x42;

        var selected = monitor.ExecuteCommand("bank ram");
        var dump = monitor.ExecuteCommand("m a000");

        Assert.DoesNotContain("Unknown", selected, StringComparison.Ordinal);
        Assert.Contains("42", dump, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// FR: FR-MON-005.
    /// Use case: The ROM view shows BASIC, character ROM, and KERNAL at
    /// their mapped addresses.
    /// Acceptance: "bank rom" shows the KERNAL byte at $E000.
    /// </summary>
    [Fact]
    public void RomView_ShowsBasicCharAndKernal()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var images = Assert.IsAssignableFrom<ViceSharp.Abstractions.IFixedRomImage>(
            machine.Devices.GetByRole(ViceSharp.Abstractions.DeviceRole.SystemRam));
        Assert.True(images.TryPeekRom(0xE000, out var kernal));
        var ram = Assert.IsAssignableFrom<ViceSharp.Abstractions.IMemory>(
            machine.Devices.GetByRole(ViceSharp.Abstractions.DeviceRole.SystemRam));
        ram.Span[0xE000] = (byte)(kernal ^ 0xFF);
        machine.Bus.Write(0x0001, 0x35);
        Assert.Equal((byte)(kernal ^ 0xFF), machine.Bus.Read(0xE000));
        var monitor = new Monitor(machine);

        var selected = monitor.ExecuteCommand("bank rom");
        var dump = monitor.ExecuteCommand("m e000");

        Assert.DoesNotContain("Unknown", selected, StringComparison.Ordinal);
        Assert.Contains("E000  " + kernal.ToString("X2"), dump, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// FR: FR-MON-005.
    /// Use case: The I/O view shows $D000-$DFFF as I/O even when CHAREN
    /// maps character ROM there for the CPU.
    /// Acceptance: After CHAREN is cleared, "bank io" still dumps the I/O
    /// byte rather than the character ROM byte.
    /// </summary>
    [Fact]
    public void IoView_ShowsIoWhenCharacterRomIsMappedForTheCpu()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        machine.Bus.Write(0xD000, 0x12);
        machine.Bus.Write(0x0001, 0x33);
        var monitor = new Monitor(machine);

        var selected = monitor.ExecuteCommand("bank io");
        var dump = monitor.ExecuteCommand("m d000");

        Assert.DoesNotContain("Unknown", selected, StringComparison.Ordinal);
        Assert.Contains("12", dump, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// FR: FR-MON-005.
    /// Use case: The drive view shows the attached drive address space.
    /// Acceptance: "bank drive" is a known command and dumps drive memory.
    /// </summary>
    [Fact]
    public void DriveView_ShowsDriveMemory()
    {
        var machine = new ArchitectureBuilder(MachineTestFactory.CreateC64RomProvider()).Build(new C1541Descriptor());
        machine.Bus.Write(0x0123, 0x42);
        var monitor = new Monitor(machine);

        var selected = monitor.ExecuteCommand("bank drive");
        var dump = monitor.ExecuteCommand("m 0123");

        Assert.DoesNotContain("Unknown", selected, StringComparison.Ordinal);
        Assert.Contains("0123  42", dump, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// FR: FR-MON-005.
    /// Use case: Choosing a bank view does not change PLA banking.
    /// Acceptance: "bank ram" leaves the CPU read of $A000 on BASIC.
    /// </summary>
    [Fact]
    public void BankSelection_DoesNotChangePlaBanking()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var basic = machine.Bus.Read(0xA000);
        var monitor = new Monitor(machine);

        var selected = monitor.ExecuteCommand("bank ram");

        Assert.DoesNotContain("Unknown", selected, StringComparison.Ordinal);
        Assert.Equal(basic, machine.Bus.Read(0xA000));
    }

    /// <summary>
    /// FR: FR-MON-005.
    /// Use case: Editing in the RAM view writes RAM even while ROM is banked
    /// in for the CPU.
    /// Acceptance: "fill ram a000 a000 5a" changes the RAM byte and not the
    /// CPU-visible BASIC byte.
    /// </summary>
    [Fact]
    public void EditingRamView_WritesRamUnderRom()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var basic = machine.Bus.Read(0xA000);
        var monitor = new Monitor(machine);

        var edited = monitor.ExecuteCommand("fill ram a000 a000 5a");
        var ram = Assert.IsAssignableFrom<ViceSharp.Abstractions.IMemory>(
            machine.Devices.GetByRole(ViceSharp.Abstractions.DeviceRole.SystemRam));

        Assert.DoesNotContain("Unknown", edited, StringComparison.Ordinal);
        Assert.Equal(0x5A, ram.Span[0xA000]);
        Assert.Equal(basic, machine.Bus.Read(0xA000));
    }
}
