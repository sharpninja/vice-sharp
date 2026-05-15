namespace ViceSharp.TestHarness;

using ViceSharp.Chips.Cpu;
using ViceSharp.Core;
using Xunit;

public sealed class CpuVisiblePcTimingTests
{
    [Fact]
    public void Jsr_FinalCycle_ExposesTargetAfterHighOperandFetch()
    {
        var cpu = CreateJsrCpu();

        for (var i = 0; i < 7; i++)
            cpu.Tick();

        Assert.Equal((ushort)0x9000, cpu.PC);
    }

    [Fact]
    public void Jsr_FinalOperandRead_CanBeHeldByExternalBusOwner()
    {
        var cpu = CreateJsrCpu();

        for (var i = 0; i < 6; i++)
            cpu.Tick();

        Assert.Equal(1, cpu.DebugCycle);
        Assert.True(cpu.CanStealCurrentCycle);
    }

    private static Mos6502 CreateJsrCpu()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0x20; // JSR $9000
        memory[0x8001] = 0x00;
        memory[0x8002] = 0x90;
        memory[0x9000] = 0xEA; // NOP

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));

        var cpu = new Mos6502(bus);
        cpu.Reset();
        return cpu;
    }
}
