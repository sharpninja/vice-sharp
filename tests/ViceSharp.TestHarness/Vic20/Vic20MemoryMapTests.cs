namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Architectures.Vic20;
using CoreVic20 = ViceSharp.Core.Vic20;
using Xunit;

/// <summary>
/// FR-VIC20-002. Memory map and expansion regions.
/// Open bus matches VICE: unconnected BLK reads return last CPU bus data
/// (<c>vic20_cpu_last_data</c> / <c>read_unconnected_c_bus</c>); writes update
/// last data without a sticky store (<c>store_dummy_c_bus</c>).
/// </summary>
public sealed class Vic20MemoryMapTests
{
    [Theory]
    [InlineData(Vic20Expansion.Unexpanded, 0x0000, true)]
    [InlineData(Vic20Expansion.Unexpanded, 0x1000, true)]
    [InlineData(Vic20Expansion.Unexpanded, 0x0400, false)]
    [InlineData(Vic20Expansion.Unexpanded, 0x2000, false)]
    [InlineData(Vic20Expansion.Exp3K, 0x0400, true)]
    [InlineData(Vic20Expansion.Exp3K, 0x2000, false)]
    [InlineData(Vic20Expansion.Exp8K, 0x2000, true)]
    [InlineData(Vic20Expansion.Exp8K, 0x3FFF, true)]
    [InlineData(Vic20Expansion.Exp8K, 0x4000, false)]
    [InlineData(Vic20Expansion.Exp16K, 0x4000, true)]
    [InlineData(Vic20Expansion.Exp24K, 0x6000, true)]
    [InlineData(Vic20Expansion.Exp32K, 0xA000, true)]
    [InlineData(Vic20Expansion.Exp32K, 0x0400, true)]
    public void Expansion_InstallsExpectedRam(Vic20Expansion expansion, int address, bool installed)
    {
        Assert.Equal(installed, Architectures.Vic20.Vic20MemoryLayout.IsInstalledRam(expansion, (ushort)address));
    }

    [Fact]
    public void UnexpandedMachine_OpenBusExpansion_WriteDoesNotStick()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        var ram = machine.Devices.GetAll<CoreVic20.Vic20SystemRam>().Single();
        Assert.Equal(CoreVic20.Vic20ExpansionKind.Unexpanded, ram.Expansion);

        // Base RAM still works.
        machine.Bus.Write(0x1000, 0xAB);
        Assert.Equal(0xAB, machine.Bus.Read(0x1000));

        // Unexpanded BLK1 is open bus: write updates last-data but does not stick
        // as RAM (KERNAL expansion probe). BASIC/KERNAL ROM reads do NOT refresh
        // last-data (VICE); a subsequent RAM/I/O access does.
        machine.Bus.Write(0x2000, 0x5A);
        Assert.Equal(0x5A, machine.Bus.Read(0x2000)); // last data after open write
        _ = machine.Bus.Read(0xC000); // ROM must not clear the open-bus last byte
        Assert.Equal(0x5A, machine.Bus.Read(0x2000));
        machine.Bus.Write(0x1000, 0x11); // installed RAM write refreshes last-data
        Assert.Equal(0x11, machine.Bus.Read(0x2000));
        Assert.NotEqual(0x5A, machine.Bus.Read(0x2000));

        machine.Bus.Write(0x0400, 0x42);
        machine.Bus.Write(0x1001, 0x33);
        Assert.Equal(0x33, machine.Bus.Read(0x0400));

        machine.Bus.Write(0xA000, 0x99);
        machine.Bus.Write(0x1002, 0x44);
        Assert.Equal(0x44, machine.Bus.Read(0xA000));
    }

    [Fact]
    public void UnexpandedMachine_OpenBus_IgnoresRomFetches_MatchesViceLastData()
    {
        // VICE: BASIC/KERNAL reads do not update vic20_cpu_last_data; only RAM/I/O
        // / dummy stores do. Open BLK5 after a stack/RAM write must echo that
        // write, not a subsequent ROM operand fetch (kernal CMP $A003,X path).
        var machine = MachineTestFactory.CreateVic20Machine();
        machine.Bus.Write(0x01FD, 0x29); // stack-style write latches $29
        _ = machine.Bus.Read(0xFD46); // kernal ROM ADH $A0 must NOT replace last-data
        Assert.Equal(0x29, machine.Bus.Read(0xA008));
        // $CD - $29 => N=1,C=1 (native xvic fingerprint at cycle 25).
        var diff = (byte)(0xCD - machine.Bus.Read(0xA008));
        Assert.Equal(0xA4, diff);
        Assert.True((diff & 0x80) != 0);
    }

    [Fact]
    public void UnexpandedMachine_Reset_DoesNotApplyC64InitPattern()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        machine.Bus.Write(0x1000, 0x11);
        machine.Reset();

        // After reset, installed main RAM is cleared — not C64 screen-space $20 fill.
        Assert.Equal(0x00, machine.Bus.Read(0x1000));
        // Open 3K window is not sticky RAM: write latches, then installed RAM overwrites last-data.
        machine.Bus.Write(0x0400, 0x11);
        Assert.Equal(0x11, machine.Bus.Read(0x0400));
        machine.Bus.Write(0x1000, 0x22);
        Assert.Equal(0x22, machine.Bus.Read(0x0400));
        // ROMs still mapped via RomDevice
        Assert.Equal(
            MachineTestFactory.LoadVic20Rom("basic-901486-01.bin").Span[0],
            machine.Bus.Read(0xC000));
    }

    [Fact]
    public void Exp8KMachine_Blk1IsWritable_Blk2OpenBusNonSticky()
    {
        var descriptor = new Vic20Descriptor().WithExpansion(Vic20Expansion.Exp8K);
        var machine = MachineTestFactory.CreateVic20Machine(descriptor);

        machine.Bus.Write(0x2000, 0x5A);
        machine.Bus.Write(0x3FFF, 0xA5);
        Assert.Equal(0x5A, machine.Bus.Read(0x2000));
        Assert.Equal(0xA5, machine.Bus.Read(0x3FFF));

        machine.Bus.Write(0x4000, 0x77);
        Assert.Equal(0x77, machine.Bus.Read(0x4000));
        machine.Bus.Write(0x2000, 0x5A); // installed BLK1 write refreshes last-data
        Assert.Equal(0x5A, machine.Bus.Read(0x4000));
        Assert.NotEqual(0x77, machine.Bus.Read(0x4000));
    }

    [Fact]
    public void IoWindow_ViaAndVicStillMapped()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        machine.Bus.Write(0x900F, 0x1B);
        Assert.Equal(0x1B, machine.Bus.Read(0x900F));
        Assert.Equal(0x00, machine.Bus.Read(0x9122)); // DDRB default
    }
}
