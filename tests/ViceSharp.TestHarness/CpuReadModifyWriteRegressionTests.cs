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
    /// consume its operand and land at the fall-through address. A
    /// taken branch adds the signed offset to that same address.
    /// Acceptance: BNE at $8000 with Z=1 ends at $8002. With Z=0, +2
    /// branches to $8004 and -4 branches to $7FFE. None executes the
    /// fall-through INX during the branch instruction.
    /// 2026-11-23, FR-CPU-001 ac-01 and D6/R18: use VICE BRANCH's
    /// instruction start plus two as the displacement base.
    /// </summary>
    [Theory]
    [InlineData(true, 0x02, 0x8002)]
    [InlineData(false, 0x02, 0x8004)]
    [InlineData(false, 0xFC, 0x7FFE)]
    public void Branch_ConsumesTheOffsetByteAndUsesSignedDisplacement(bool zeroFlag, byte offset, ushort expectedPc)
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xD0; // BNE
        memory[0x8001] = offset;
        memory[0x8002] = 0xE8; // INX, executed only on a separate instruction
        memory[0x8003] = 0xEA; // NOP

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));

        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.Flags = zeroFlag ? (byte)0x26 : (byte)0x24;

        cpu.ExecuteInstruction();

        Assert.Equal(expectedPc, cpu.PC);
        Assert.Equal((byte)0x00, cpu.X);
    }

}
