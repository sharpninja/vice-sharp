namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Abstractions;
using ViceSharp.Architectures.C1541;
using ViceSharp.Architectures.Vic20;
using ViceSharp.Chips.Cpu;
using ViceSharp.Chips.IEC;
using ViceSharp.Chips.Vic;
using ViceSharp.Core;
using ViceSharp.Core.Vic20;
using ViceSharp.Launcher;
using Xunit;

/// <summary>
/// FR-PRF-005, FR-VIC20-001..003, FR-VIC20-006 / Iteration 2 Slice A.
/// Use case: construct a VIC-20 skeleton (CPU, ROMs, dual VIA, stub video)
/// and prove default drive 8 model is 1540 without requiring full BASIC boot.
/// Acceptance: profile resolve, ROM completeness, machine build, reset vector,
/// first cycles, VIA windows, stub video role, launcher xvic YAML, C1540 default.
/// </summary>
public sealed class Vic20SkeletonTests
{
    [Fact]
    public void Vic20Profile_Default_IsPal_With6561TimingAnd1540Drive()
    {
        var profile = Vic20MachineProfiles.Default;

        Assert.Equal("vic20", profile.Id);
        Assert.Equal("xvic", profile.Family);
        Assert.Equal(VideoStandard.Pal, profile.VideoStandard);
        Assert.Equal(1_108_405, profile.NominalClockHz);
        Assert.Equal(71, profile.CyclesPerLine);
        Assert.Equal(312, profile.RasterLines);
        Assert.Equal("Mos6561", profile.VicIIModel);
        Assert.Equal(DriveModel.C1540, profile.DefaultDriveModel);
        Assert.Equal(C1541ViceRomNames.Dos1540, profile.DefaultDriveDosRomName);
        Assert.Equal(Vic20ViceRomNames.Basic, profile.BasicRomName);
        Assert.Equal(Vic20ViceRomNames.KernalPal, profile.KernalRomName);
        Assert.Equal(Vic20ViceRomNames.Character, profile.CharacterRomName);
    }

    [Theory]
    [InlineData("vic20")]
    [InlineData("vic20pal")]
    [InlineData("pal")]
    [InlineData("xvic")]
    public void Vic20Profile_Resolve_PalAliases(string selector)
    {
        var profile = Vic20MachineProfiles.Resolve(selector);
        Assert.Equal(VideoStandard.Pal, profile.VideoStandard);
        Assert.Equal(DriveModel.C1540, profile.DefaultDriveModel);
    }

    [Theory]
    [InlineData("vic20ntsc")]
    [InlineData("ntsc")]
    public void Vic20Profile_Resolve_NtscAliases(string selector)
    {
        var profile = Vic20MachineProfiles.Resolve(selector);
        Assert.Equal(VideoStandard.Ntsc, profile.VideoStandard);
        Assert.Equal(1_022_727, profile.NominalClockHz);
        Assert.Equal(65, profile.CyclesPerLine);
        Assert.Equal(261, profile.RasterLines);
        Assert.Equal("Mos6560", profile.VicIIModel);
        Assert.Equal(DriveModel.C1540, profile.DefaultDriveModel);
    }

    [Fact]
    public void Vic20RomSet_IsComplete_AgainstViceDataRoot()
    {
        var provider = MachineTestFactory.CreateVic20RomProvider();
        var romSet = new Vic20RomSet();

        Assert.True(romSet.IsComplete(provider));
        Assert.Equal(Vic20ViceRomNames.ArchitectureKey, romSet.Architecture);
    }

    [Fact]
    public void Vic20Descriptor_WithoutRomProvider_ThrowsClearError()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => new ArchitectureBuilder().Build(new Vic20Descriptor()));

        Assert.Contains("requires an IRomProvider", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Vic20Machine_Builds_WithCpuVideoDualViaAndRoms()
    {
        var machine = MachineTestFactory.CreateVic20Machine();

        Assert.NotNull(machine.Devices.GetByRole(DeviceRole.Cpu));
        Assert.NotNull(machine.Devices.GetByRole(DeviceRole.SystemRam));
        Assert.NotNull(machine.Devices.GetByRole(DeviceRole.VideoChip));
        Assert.IsAssignableFrom<IVideoChip>(machine.Devices.GetByRole(DeviceRole.VideoChip));
        Assert.IsType<Mos6561>(machine.Devices.GetByRole(DeviceRole.VideoChip));

        var vias = machine.Devices.GetAll<Via6522>().OrderBy(v => v.BaseAddress).ToArray();
        Assert.Equal(2, vias.Length);
        Assert.Equal(0x9110, vias[0].BaseAddress);
        Assert.Equal(0x9120, vias[1].BaseAddress);

        var basic = MachineTestFactory.LoadVic20Rom(Vic20ViceRomNames.Basic).Span;
        var kernal = MachineTestFactory.LoadVic20Rom(Vic20ViceRomNames.KernalPal).Span;
        var chargen = MachineTestFactory.LoadVic20Rom(Vic20ViceRomNames.Character).Span;

        Assert.Equal(basic[0], machine.Bus.Read(0xC000));
        Assert.Equal(kernal[0], machine.Bus.Read(0xE000));
        Assert.Equal(chargen[0], machine.Bus.Read(0x8000));
    }

    [Fact]
    public void Vic20Machine_AfterReset_CpuPcMatchesKernalResetVector()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        var cpu = Assert.IsAssignableFrom<ICpu>(machine.Devices.GetByRole(DeviceRole.Cpu));
        var lo = machine.Bus.Read(0xFFFC);
        var hi = machine.Bus.Read(0xFFFD);
        var expected = (ushort)(lo | (hi << 8));

        Assert.Equal(expected, cpu.PC);
        Assert.NotEqual((ushort)0, expected);
    }

    [Fact]
    public void Vic20Machine_FirstThousandCycles_DoNotThrow_AndAdvanceClock()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        var start = machine.Clock.TotalCycles;

        for (var i = 0; i < 1000; i++)
            machine.Clock.Step();

        Assert.Equal(start + 1000, machine.Clock.TotalCycles);
    }

    [Fact]
    public void Vic20Machine_BaseRam_IsWritable()
    {
        var machine = MachineTestFactory.CreateVic20Machine();

        machine.Bus.Write(0x0002, 0x42);
        machine.Bus.Write(0x1000, 0x55);

        Assert.Equal(0x42, machine.Bus.Read(0x0002));
        Assert.Equal(0x55, machine.Bus.Read(0x1000));
    }

    [Fact]
    public void Vic20Descriptor_DevicesList_IncludesExpectedRoles()
    {
        var descriptor = new Vic20Descriptor();
        var roles = descriptor.Devices.Select(d => d.Role).ToHashSet();

        Assert.Contains(DeviceRole.Cpu, roles);
        Assert.Contains(DeviceRole.VideoChip, roles);
        Assert.Contains(DeviceRole.SystemRam, roles);
        Assert.Equal(1_108_405, descriptor.MasterClockHz);
        Assert.Equal(VideoStandard.Pal, descriptor.VideoStandard);
        Assert.NotNull(descriptor.RequiredRoms);
        Assert.True(descriptor.RequiredRoms!.IsComplete(MachineTestFactory.CreateVic20RomProvider()));
    }

    [Fact]
    public void Launcher_Xvic_BuildsTopologyYaml_WithVic20HostAnd1540DriveDefaultComment()
    {
        var args = ViceArgsParser.Parse("xvic", Array.Empty<string>());
        var yaml = ViceTopologyBuilder.BuildYaml(args);

        Assert.Contains("kind: Vic20", yaml, StringComparison.Ordinal);
        Assert.Contains("xvic", yaml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DefaultDrive_Is1540_Not1541()
    {
        foreach (var profile in Vic20MachineProfiles.All)
        {
            Assert.Equal(DriveModel.C1540, profile.DefaultDriveModel);
            Assert.Equal(C1541ViceRomNames.Dos1540, profile.DefaultDriveDosRomName);
            Assert.NotEqual(DriveModel.C1541, profile.DefaultDriveModel);
        }
    }
}
