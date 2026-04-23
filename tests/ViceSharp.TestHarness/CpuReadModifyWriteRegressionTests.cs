namespace ViceSharp.TestHarness;

using ViceSharp.Chips.Cpu;
using ViceSharp.Core;
using Xunit;

public sealed class CpuReadModifyWriteRegressionTests
{
    [Fact]
    public void IncZeroPage_UsesTheFetchedOperandAddressOnce()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xE6; // INC $10
        memory[0x8001] = 0x10;
        memory[0x8002] = 0xEA; // NOP
        memory[0x0010] = 0x7F;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));

        var cpu = new Mos6502(bus);
        cpu.Reset();

        cpu.ExecuteInstruction();

        Assert.Equal((byte)0x80, memory[0x0010]);
        Assert.Equal((ushort)0x8002, cpu.PC);
        Assert.Equal((byte)0xEA, memory[0x8002]);
    }

    [Fact]
    public void BranchNotTaken_StillConsumesTheOffsetByte()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xD0; // BNE +2
        memory[0x8001] = 0x02;
        memory[0x8002] = 0xE8; // INX, must be skipped when branch is not taken
        memory[0x8003] = 0xEA; // NOP

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));

        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.Flags = 0x26; // Zero flag set so BNE is not taken

        cpu.ExecuteInstruction();

        Assert.Equal((ushort)0x8002, cpu.PC);
        Assert.Equal((byte)0x00, cpu.X);
    }
}
