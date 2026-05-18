namespace ViceSharp.TestHarness;

using ViceSharp.Chips.Cpu;
using ViceSharp.Core;
using Xunit;

public sealed class CpuReadModifyWriteRegressionTests
{
    /// <summary>
    /// FR: FR-CPU-001, TR: TR-CYCLE-001.
    /// Use case: <c>INC $zp</c> is a read-modify-write opcode; the operand
    /// address must be fetched once and not re-fetched after the modify
    /// step (a historical regression here double-incremented PC).
    /// Acceptance: After executing <c>E6 10</c> at $8000, the zero-page byte
    /// at $0010 increments by one ($7F -> $80) and PC advances to $8002.
    /// </summary>
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

    /// <summary>
    /// FR: FR-CPU-001, TR: TR-CYCLE-001.
    /// Use case: A non-taken branch (e.g. <c>BNE</c> with Z set) must still
    /// consume its operand byte so PC advances past both opcode and offset
    /// without executing the would-be-skipped instruction.
    /// Acceptance: After running <c>D0 02</c> at $8000 with Z=1, PC ends at
    /// $8002 and the otherwise-skipped <c>INX</c> at $8002 is not executed
    /// (X remains $00).
    /// </summary>
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
