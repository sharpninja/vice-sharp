namespace ViceSharp.TestHarness;

using ViceSharp.Architectures.C64;
using ViceSharp.Architectures.EmptyMachine;
using ViceSharp.Core;
using Xunit;

public sealed class C64MachineTests
{
    [Fact]
    public void EmptyMachineDescriptor_BuildsWithoutRomProvider()
    {
        var machine = new ArchitectureBuilder().Build(new EmptyMachineDescriptor());

        Assert.NotNull(machine.Devices.GetByRole(Abstractions.DeviceRole.Cpu));
        Assert.NotNull(machine.Devices.GetByRole(Abstractions.DeviceRole.SystemRam));
        Assert.Null(machine.Devices.GetByRole(Abstractions.DeviceRole.VideoChip));
    }

    [Fact]
    public void C64Descriptor_WithoutRomProvider_ThrowsClearError()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => new ArchitectureBuilder().Build(new C64Descriptor()));

        Assert.Contains("requires an IRomProvider", ex.Message);
    }

    [Fact]
    public void BasicRom_IsVisibleByDefault_AndBankedRamAppears_WhenLoramDisabled()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var basicRomFirstByte = MachineTestFactory.LoadC64Rom("basic").Span[0];

        machine.Bus.Write(0xA000, 0x42);

        Assert.Equal(basicRomFirstByte, machine.Bus.Read(0xA000));

        machine.Bus.Write(0x0001, 0x36);

        Assert.Equal(0x42, machine.Bus.Read(0xA000));
    }

    [Fact]
    public void CharacterRom_CanBeMappedOverIoSpace_WhenCharenCleared()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var charRomFirstByte = MachineTestFactory.LoadC64Rom("characters").Span[0];

        Assert.NotEqual(0x12, charRomFirstByte);

        machine.Bus.Write(0xD000, 0x12);
        Assert.Equal(0x12, machine.Bus.Peek(0xD000));

        machine.Bus.Write(0x0001, 0x33);
        Assert.Equal(charRomFirstByte, machine.Bus.Read(0xD000));

        machine.Bus.Write(0x0001, 0x37);
        Assert.Equal(0x12, machine.Bus.Peek(0xD000));
    }

    [Fact]
    public void ColorRam_WritesAreMaskedToLowNibble()
    {
        var machine = MachineTestFactory.CreateC64Machine();

        machine.Bus.Write(0xD800, 0xFF);

        Assert.Equal(0x0F, machine.Bus.Read(0xD800));
    }

    [Fact]
    public void StepInstruction_AdvancesPastSingleMasterCycle()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var before = machine.GetState();

        machine.StepInstruction();

        var after = machine.GetState();
        Assert.True(after.Cycle - before.Cycle > 1, $"Expected >1 cycle, got {after.Cycle - before.Cycle}");
        Assert.NotEqual(before.PC, after.PC);
    }
}
