namespace ViceSharp.TestHarness;

using Xunit;
using ViceSharp.Chips.Cpu;
using ViceSharp.Abstractions;

/// <summary>
/// Unit tests for CPU interrupt handling - no native VICE required
/// </summary>
public sealed class CpuInterruptTests
{
    /// <summary>
    /// FR: FR-CPU-001, TR: TR-CYCLE-001.
    /// Use case: A fresh 6502 with reset vector bytes at $FFFC/$FFFD must
    /// load PC from the vector, clear the stack pointer to $00 and seed P
    /// with the documented post-reset flag bits.
    /// Acceptance: After <c>Reset()</c>, PC equals the vector word ($1234),
    /// S equals $00 and P equals $26 (interrupt-disable plus unused bit).
    /// </summary>
    [Fact]
    public void Reset_InitializesRegisters()
    {
        // Arrange
        var bus = new MockBus();
        var cpu = new Mos6502(bus);
        
        // Setup reset vector
        bus.SetMemory(0xFFFC, 0x34);
        bus.SetMemory(0xFFFD, 0x12);

        // Act
        cpu.Reset();

        // Assert
        Assert.Equal(0x1234, cpu.PC);
        Assert.Equal(0x00, cpu.S);
        Assert.Equal(0x26, cpu.P);
    }

    /// <summary>
    /// FR: FR-CPU-003, TR: TR-CYCLE-001.
    /// Use case: With the I (interrupt-disable) flag set, asserting an IRQ
    /// must not divert execution: PC stays put and no stack push occurs.
    /// Acceptance: After <c>Irq()</c> on a CPU with <c>P=0x04</c>, PC equals
    /// its pre-call value (no jump performed).
    /// </summary>
    [Fact]
    public void Irq_WhenInterruptDisabled_DoesNotJump()
    {
        // Arrange
        var bus = new MockBus();
        var cpu = new Mos6502(bus);
        cpu.P = 0x04; // I flag set = interrupts disabled
        
        var originalPC = cpu.PC;

        // Act
        cpu.Irq();

        // Assert - PC should not change when I flag is set
        Assert.Equal(originalPC, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-003, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: With the I flag clear, asserting an IRQ must dispatch through
    /// VICE's 7-cycle x64sc sequence (6510dtvcore.c DO_INTERRUPT + DO_IRQBRK:
    /// two dummy fetches, PCH/PCL/P pushes with B clear, I set with the $FFFE
    /// read, $FFFF read). <c>Irq()</c> arms the sequence at the boundary tick
    /// (which under the core's one-cycle-lag convention coincides with the
    /// native sequence's first dummy cycle) and each Tick() consumes one of
    /// the remaining 6 cycles, so BA steals can interleave exactly as on the
    /// single-cycle core.
    /// Acceptance: after <c>Irq()</c> plus 6 ticks, PC equals the vector
    /// target ($0800) on the following fetch tick, I is set, the stack holds
    /// PCH/PCL at $01FF/$01FE and the pushed status at $01FD with B (bit 4)
    /// clear; the stack pushes land on sequence ticks 2-4 (S still $FF after
    /// the dummy tick, $FC after the 4th tick) and I becomes visible with the
    /// $FFFE read on tick 5, matching the measured native per-cycle export.
    /// </summary>
    [Fact]
    public void Irq_WhenInterruptEnabled_RunsStagedSequenceAndJumps()
    {
        // Arrange
        var bus = new MockBus();
        var cpu = new Mos6502(bus);
        cpu.P = 0x20; // I flag clear = interrupts enabled
        cpu.S = 0xFF;
        cpu.PC = 0x0400;

        // Setup IRQ vector
        bus.SetMemory(0xFFFE, 0x00);
        bus.SetMemory(0xFFFF, 0x08);

        // Act: arm at the boundary, then consume the staged dispatch sequence.
        cpu.Irq();

        cpu.Tick(); // dummy fetch (native C2)
        Assert.Equal(0xFF, cpu.S); // no push yet

        cpu.Tick(); // push PCH (native C3)
        cpu.Tick(); // push PCL (native C4)
        cpu.Tick(); // push P with B clear (native C5)
        Assert.Equal(0xFC, cpu.S);
        Assert.Equal(0x00, cpu.P & 0x04); // I not yet visible before the vector read

        cpu.Tick(); // set I + read $FFFE (native C6)
        Assert.Equal(0x04, cpu.P & 0x04);
        Assert.Equal(0x0400, cpu.PC); // interrupted PC stays visible

        cpu.Tick(); // read $FFFF, latch new PC (native C7)
        Assert.Equal(0x0400, cpu.PC); // JUMP not exported until the fetch cycle

        cpu.Tick(); // handler C1: JUMP visible (delayed-fetch tick restores the lag)

        // Assert
        Assert.Equal(0x0800, cpu.PC);
        Assert.Equal(0x04, cpu.P & 0x04); // I flag should be set
        Assert.Equal(0x04, bus.Read(0x01FF)); // PCH = 0x04
        Assert.Equal(0x00, bus.Read(0x01FE)); // PCL = 0x00
        Assert.Equal(0x20, bus.Read(0x01FD)); // pushed P with B (0x10) clear
    }

    /// <summary>
    /// FR: FR-CPU-003, TR: TR-CYCLE-001.
    /// Use case: An NMI must always push the return PC and jump through the
    /// $FFFA/$FFFB vector regardless of the I flag, then set I to prevent
    /// IRQ recursion inside the handler.
    /// Acceptance: After <c>Nmi()</c>, PC equals the vector target ($0800),
    /// I is set, and the stack contains PCL/PCH at $01FE/$01FF.
    /// </summary>
    [Fact]
    public void Nmi_PushesStateAndJumps()
    {
        // Arrange
        var bus = new MockBus();
        var cpu = new Mos6502(bus);
        cpu.S = 0xFF;
        cpu.PC = 0x0400;
        
        // Setup NMI vector
        bus.SetMemory(0xFFFA, 0x00);
        bus.SetMemory(0xFFFB, 0x08);

        // Act
        cpu.Nmi();

        // Assert
        Assert.Equal(0x0800, cpu.PC);
        Assert.Equal(0x04, cpu.P & 0x04); // I flag should be set
        
        // Stack after PushWord(0x0400):
        // S = 0xFF initially, Push(PCH=0x04) writes to 0x01FF, S becomes 0xFE
        // Push(PCL=0x00) writes to 0x01FE, S becomes 0xFD
        Assert.Equal(0x00, bus.Read(0x01FE)); // PCL = 0x00
        Assert.Equal(0x04, bus.Read(0x01FF)); // PCH = 0x04
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
