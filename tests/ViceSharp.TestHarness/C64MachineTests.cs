namespace ViceSharp.TestHarness;

using ViceSharp.Architectures.C64;
using ViceSharp.Architectures.EmptyMachine;
using ViceSharp.Abstractions;
using ViceSharp.Core;
using System.Text;
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
    public void C64Descriptor_WithBadRomChecksums_ThrowsClearError()
    {
        var badRomProvider = new CorruptLengthAndHashRomProvider();
        var ex = Assert.Throws<InvalidOperationException>(() => new ArchitectureBuilder(badRomProvider).Build(new C64Descriptor()));

        Assert.Contains("ROM set is invalid or missing expected checksum entries", ex.Message);
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
    public void C64Ram_UsesVicePowerOnPattern()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var ram = Assert.IsAssignableFrom<IMemory>(machine.Devices.GetByRole(DeviceRole.SystemRam));

        Assert.Equal(0x00, machine.Bus.Read(0x0400));
        Assert.Equal(0x00, machine.Bus.Read(0x0401));
        Assert.Equal(0xFF, machine.Bus.Read(0x0402));
        Assert.Equal(0xFF, machine.Bus.Read(0x0403));
        Assert.Equal(0xFF, machine.Bus.Read(0xC000));
        Assert.Equal(0x00, ram.Span[0xFFFC]);
        Assert.Equal(0x00, ram.Span[0xFFFD]);
    }

    [Fact]
    public void Cia2PortA_ReadsIdleSerialInputWithBit7Low()
    {
        var machine = MachineTestFactory.CreateC64Machine();

        machine.Bus.Write(0xDD00, 0x07);
        machine.Bus.Write(0xDD02, 0x3F);

        Assert.Equal(0x47, machine.Bus.Read(0xDD00));
    }

    [Fact]
    public void C64GsCia2PortA_ReadsDisconnectedSerialInputWithBits6And7Low()
    {
        var machine = MachineTestFactory.CreateC64Machine("c64gs");

        machine.Bus.Write(0xDD00, 0x07);
        machine.Bus.Write(0xDD02, 0x3F);

        Assert.Equal(0x07, machine.Bus.Read(0xDD00));
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

    private sealed class CorruptLengthAndHashRomProvider : IRomProvider
    {
        private readonly Dictionary<string, byte[]> _romData = new()
        {
            ["basic"] = CreateBytes(8192),
            [C64ViceRomNames.Basic] = CreateBytes(8192),
            ["kernal"] = CreateBytes(8192),
            [C64ViceRomNames.KernalRev3] = CreateBytes(8192),
            ["characters"] = CreateBytes(4096),
            [C64ViceRomNames.Character] = CreateBytes(4096),
        };

        private static byte[] CreateBytes(int count) => Encoding.UTF8.GetBytes(new string('A', count));

        public ReadOnlyMemory<byte> LoadRom(string romName, string architecture)
            => _romData[romName];

        public bool IsAvailable(string romName, string architecture) => true;
    }
}
