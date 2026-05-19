namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.Cpu;
using Xunit;

/// <summary>
/// FR/TR: FR-CPU (BACKFILL-CPU 6510 BRK instruction + B-flag).
/// Use case: the 6510 must execute opcode $00 (BRK) as a software
/// interrupt: it consumes a signature byte (so the stacked return
/// address is PC+2 from the BRK opcode location), pushes PCH/PCL/P
/// in that order, sets the B flag (bit 4) in the pushed status byte
/// to distinguish a software interrupt from a hardware IRQ/NMI,
/// sets the live I flag, and dispatches through the $FFFE/$FFFF
/// vector. RTI from the handler pops three bytes and resumes at the
/// stacked PC. These tests parallel the NMI fixture (Mos6510NmiTests)
/// and backfill BRK-specific coverage that the broad lockstep
/// validator does not isolate by name.
/// Acceptance: each test below covers one externally observable
/// guarantee of the 6502/6510 BRK instruction.
/// </summary>
public sealed class Mos6510BrkTests
{
    /// <summary>
    /// FR/TR: FR-CPU (BACKFILL-CPU 6510 BRK vector dispatch).
    /// Use case: BRK must transfer control through the IRQ/BRK vector
    /// at $FFFE/$FFFF (little-endian), regardless of the I flag state.
    /// Acceptance: after a BRK at $C000 with $FFFE/F = $1234, PC equals
    /// $1234 at the next instruction boundary.
    /// </summary>
    [Fact]
    public void Brk_DispatchesThroughFffeFfffVector()
    {
        // Arrange
        var bus = new MockBus();
        var cpu = new Mos6502(bus);
        cpu.S = 0xFF;
        cpu.PC = 0xC000;
        cpu.P = 0x20; // unused bit only - I clear
        bus.SetMemory(0xC000, 0x00); // BRK opcode
        bus.SetMemory(0xC001, 0x00); // BRK signature byte
        bus.SetMemory(0xFFFE, 0x34);
        bus.SetMemory(0xFFFF, 0x12);

        // Act - step until BRK completes (7 cycles)
        StepUntilInstructionBoundary(cpu, maxTicks: 16);

        // Assert
        Assert.Equal(0x1234, cpu.PC);
    }

    /// <summary>
    /// FR/TR: FR-CPU (BACKFILL-CPU 6510 BRK stacked return address).
    /// Use case: BRK is documented as a 1-byte opcode but the CPU
    /// internally consumes the following byte as a signature, so the
    /// stacked return PC must be the address two bytes past the BRK
    /// opcode (i.e. BRK at $C000 stacks $C002, not $C001 and not $C000).
    /// Acceptance: with S=0xFF and BRK at $C000, after dispatch the
    /// bytes at $01FF/$01FE hold $C0/$02 (PCH/PCL) respectively and
    /// S equals 0xFC (three pushes).
    /// </summary>
    [Fact]
    public void Brk_PushesReturnAddressOfBrkPlusTwo()
    {
        // Arrange
        var bus = new MockBus();
        var cpu = new Mos6502(bus);
        cpu.S = 0xFF;
        cpu.PC = 0xC000;
        cpu.P = 0x20;
        bus.SetMemory(0xC000, 0x00); // BRK
        bus.SetMemory(0xC001, 0x00); // signature byte (ignored)
        bus.SetMemory(0xFFFE, 0x00);
        bus.SetMemory(0xFFFF, 0x08); // handler at $0800

        // Act
        StepUntilInstructionBoundary(cpu, maxTicks: 16);

        // Assert
        Assert.Equal(0xC0, bus.Read(0x01FF)); // PCH = high byte of BRK+2
        Assert.Equal(0x02, bus.Read(0x01FE)); // PCL = low byte of BRK+2
        Assert.Equal(0xFC, cpu.S);            // three bytes pushed
    }

    /// <summary>
    /// FR/TR: FR-CPU (BACKFILL-CPU 6510 BRK B-flag-set semantic).
    /// Use case: the pushed status byte must have the B flag (bit 4,
    /// $10) set to 1 so a shared IRQ/BRK handler can distinguish a
    /// software BRK from a hardware IRQ (the IRQ path pushes B=0).
    /// This is the inverse of the NMI/IRQ test which asserts B=0.
    /// Acceptance: after BRK dispatch, bit 4 of the byte at $01FD is 1.
    /// </summary>
    [Fact]
    public void Brk_SetsBFlagInPushedStatus()
    {
        // Arrange - start with B clear in the live register, prove that
        // the pushed copy still has B=1 (B exists only in the pushed
        // copy on the 6502).
        var bus = new MockBus();
        var cpu = new Mos6502(bus);
        cpu.S = 0xFF;
        cpu.PC = 0xC000;
        cpu.P = 0x20; // unused only - B clear in live register
        bus.SetMemory(0xC000, 0x00);
        bus.SetMemory(0xC001, 0x00);
        bus.SetMemory(0xFFFE, 0x00);
        bus.SetMemory(0xFFFF, 0x08);

        // Act
        StepUntilInstructionBoundary(cpu, maxTicks: 16);

        // Assert
        var pushedStatus = bus.Read(0x01FD);
        Assert.Equal(0x10, pushedStatus & 0x10); // B flag MUST be 1 in pushed byte
    }

    /// <summary>
    /// FR/TR: FR-CPU (BACKFILL-CPU 6510 BRK sets live I flag).
    /// Use case: after BRK dispatch the live processor status must
    /// have the I flag set so that the handler runs with IRQs masked
    /// until it executes CLI or RTI restores the previous P.
    /// Acceptance: with the I flag clear pre-BRK, after BRK dispatch
    /// the live <see cref="Mos6502.P"/> register has bit 2 set.
    /// </summary>
    [Fact]
    public void Brk_SetsInterruptDisableFlagInLiveStatus()
    {
        // Arrange
        var bus = new MockBus();
        var cpu = new Mos6502(bus);
        cpu.S = 0xFF;
        cpu.PC = 0xC000;
        cpu.P = 0x20; // I clear pre-BRK
        bus.SetMemory(0xC000, 0x00);
        bus.SetMemory(0xC001, 0x00);
        bus.SetMemory(0xFFFE, 0x00);
        bus.SetMemory(0xFFFF, 0x08);

        // Act
        StepUntilInstructionBoundary(cpu, maxTicks: 16);

        // Assert
        Assert.Equal(0x04, cpu.P & 0x04); // I flag set after BRK
    }

    /// <summary>
    /// FR/TR: FR-CPU (BACKFILL-CPU 6510 BRK / RTI round trip).
    /// Use case: a minimal BRK handler that is just RTI must pop the
    /// three pushed bytes (P, PCL, PCH) and restore PC to the post-BRK
    /// location (BRK+2). The stack pointer must return to its pre-BRK
    /// value. RTI ignores the B flag in the popped status.
    /// Acceptance: after BRK at $C000 and one RTI, PC equals $C002 and
    /// S equals 0xFF (pre-BRK value).
    /// </summary>
    [Fact]
    public void Brk_RtiReturnsToInstructionAfterSignatureByte()
    {
        // Arrange
        var bus = new MockBus();
        var cpu = new Mos6502(bus);
        cpu.S = 0xFF;
        cpu.PC = 0xC000;
        cpu.P = 0x20;
        bus.SetMemory(0xC000, 0x00); // BRK
        bus.SetMemory(0xC001, 0x00); // signature byte
        bus.SetMemory(0xC002, 0xEA); // NOP target of return (proves we landed here)
        bus.SetMemory(0xFFFE, 0x00);
        bus.SetMemory(0xFFFF, 0x08);
        bus.SetMemory(0x0800, 0x40); // RTI as entire handler

        // Act - step BRK to completion
        StepUntilInstructionBoundary(cpu, maxTicks: 16);
        Assert.Equal(0x0800, cpu.PC); // sanity: handler entered
        Assert.Equal(0xFC, cpu.S);    // sanity: three bytes pushed

        // Step through RTI (6 cycles) until back at $C002 boundary
        StepUntilPcReaches(cpu, 0xC002, maxTicks: 32);

        // Assert
        Assert.Equal(0xC002, cpu.PC);
        Assert.Equal(0xFF, cpu.S);
    }

    /// <summary>
    /// Step the CPU until it reaches an instruction boundary after the
    /// in-progress instruction completes, or fail on tick exhaustion.
    /// </summary>
    private static void StepUntilInstructionBoundary(Mos6502 cpu, int maxTicks)
    {
        for (var i = 0; i < maxTicks; i++)
        {
            cpu.Tick();
            if (cpu.IsInstructionBoundary && i > 0)
            {
                return;
            }
        }

        Assert.Fail($"CPU did not reach instruction boundary within {maxTicks} ticks (PC=${cpu.PC:X4}).");
    }

    /// <summary>
    /// Tick the CPU directly until <paramref name="targetPc"/> is reached at an
    /// instruction boundary, or fail the test on tick exhaustion.
    /// </summary>
    private static void StepUntilPcReaches(Mos6502 cpu, ushort targetPc, int maxTicks)
    {
        for (var i = 0; i < maxTicks; i++)
        {
            cpu.Tick();
            if (cpu.PC == targetPc && cpu.IsInstructionBoundary)
            {
                return;
            }
        }

        Assert.Fail($"CPU did not reach PC=${targetPc:X4} within {maxTicks} ticks (PC=${cpu.PC:X4}).");
    }

    private sealed class MockBus : IBus
    {
        private readonly byte[] _memory = new byte[65536];

        public void SetMemory(int address, byte value) => _memory[address & 0xFFFF] = value;

        public byte Read(ushort address) => _memory[address];

        public void Write(ushort address, byte value) => _memory[address] = value;

        public byte Peek(ushort address) => _memory[address];

        public void RegisterDevice(IAddressSpace device) { }

        public void UnregisterDevice(IAddressSpace device) { }
    }
}
