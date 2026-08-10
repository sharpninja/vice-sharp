namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Architectures.Vic20;
using ViceSharp.Core.Vic20;
using ViceSharp.Protocol;
using Xunit;
using CoreLayout = ViceSharp.Core.Vic20.Vic20MemoryLayout;

/// <summary>
/// Prove create path with Vic20MemorySpec installs matching RAM regions
/// (real factory + ArchitectureBuilder, not mocked map).
/// </summary>
public sealed class Vic20MemorySettingsRestartTests
{
    [Fact]
    public void CreateSession_WithMemorySpec60a0_InstallsBlk3AndBlk5Only()
    {
        var descriptor = new Vic20Descriptor("vic20").WithRamBlocks(Vic20RamBlocks.Blk3 | Vic20RamBlocks.Blk5);
        var machine = MachineTestFactory.CreateVic20Machine(descriptor);
        var ram = machine.Devices.GetAll<Vic20SystemRam>().Single();

        Assert.Equal(Vic20RamBlocks.Blk3 | Vic20RamBlocks.Blk5, ram.RamBlocks);
        Assert.True(ram.HandlesAddress(0x6000));
        Assert.True(ram.HandlesAddress(0xA000));
        Assert.False(ram.HandlesAddress(0x2000));
        Assert.False(ram.HandlesAddress(0x0400));

        // Writable installed regions
        machine.Bus.Write(0x6000, 0x11);
        machine.Bus.Write(0xA000, 0x22);
        Assert.Equal(0x11, machine.Bus.Read(0x6000));
        Assert.Equal(0x22, machine.Bus.Read(0xA000));
    }

    [Fact]
    public void CreateSession_WithMemorySpecAll_InstallsAllBlocks()
    {
        var descriptor = new Vic20Descriptor("vic20").WithRamBlocks(Vic20RamBlocks.All);
        var machine = MachineTestFactory.CreateVic20Machine(descriptor);
        var ram = machine.Devices.GetAll<Vic20SystemRam>().Single();
        Assert.Equal(Vic20RamBlocks.All, ram.RamBlocks);
        Assert.True(ram.HandlesAddress(0x0400));
        Assert.True(ram.HandlesAddress(0x2000));
        Assert.True(ram.HandlesAddress(0x4000));
        Assert.True(ram.HandlesAddress(0x6000));
        Assert.True(ram.HandlesAddress(0xA000));
    }

    [Fact]
    public void MemorySpec_FormatParse_RoundTrip_ForSettingsPipeline()
    {
        var spec = CoreLayout.FormatMemorySpec(Vic20RamBlocks.Blk3 | Vic20RamBlocks.Blk5);
        Assert.Equal("60,a0", spec);
        Assert.True(CoreLayout.TryParseMemorySpec(spec, out var blocks));
        Assert.Equal(Vic20RamBlocks.Blk3 | Vic20RamBlocks.Blk5, blocks);

        // UI helper agrees with core codec for this custom map
        var ui = new Vic20MemoryUiState(spec);
        Assert.Equal("60,a0", ui.MemorySpec);
        Assert.True(ui.Blk3);
        Assert.True(ui.Blk5);
        Assert.False(ui.Blk1);
    }

    [Fact]
    public void Vic20Machine_RegistersExpansionCartPort_AndFileSystemIec()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        Assert.NotNull(machine.Devices.GetAll<IVic20ExpansionCartPort>().FirstOrDefault());
        Assert.NotNull(machine.Devices.GetAll<ViceSharp.Core.Iec.FileSystemIecDevice>().FirstOrDefault());
    }
}
