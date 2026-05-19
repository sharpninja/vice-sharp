namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.Cpu;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-CPU (BACKFILL-CPU 6510 NMI vector + handling).
/// Use case: the 6510 (which the C64 ships) must take any NMI edge
/// (high-to-low transition on the NMI pin) and dispatch through the
/// $FFFA/$FFFB vector regardless of the I flag, with a stack frame
/// that distinguishes NMI/IRQ from BRK via the B flag bit. The fixture
/// drives the bare CPU and a real <see cref="SystemClock"/> wired with
/// an NMI line to cover both the in-CPU vector dispatch and the
/// edge-trigger latch that lives in the clock.
/// Acceptance: each test below covers one externally observable
/// guarantee from the 6510 datasheet; together they backfill the
/// dedicated coverage that the broad lockstep validator does not
/// isolate by name.
/// </summary>
public sealed class Mos6510NmiTests
{
    /// <summary>
    /// FR/TR: FR-CPU (BACKFILL-CPU 6510 NMI dispatch).
    /// Use case: when the CPU services an NMI it must load PC from
    /// the $FFFA/$FFFB vector regardless of the I flag.
    /// Acceptance: after <see cref="Mos6502.Nmi"/>, PC equals the
    /// 16-bit value assembled little-endian from $FFFA/$FFFB.
    /// </summary>
    [Fact]
    public void Nmi_DispatchesThroughFffaFffbVector()
    {
        // Arrange
        var bus = new MockBus();
        var cpu = new Mos6502(bus);
        cpu.S = 0xFF;
        cpu.PC = 0x0400;
        cpu.P = 0x24; // I flag set on purpose - NMI must still fire
        bus.SetMemory(0xFFFA, 0x34);
        bus.SetMemory(0xFFFB, 0x12);

        // Act
        cpu.Nmi();

        // Assert
        Assert.Equal(0x1234, cpu.PC);
    }

    /// <summary>
    /// FR/TR: FR-CPU (BACKFILL-CPU 6510 NMI stack frame layout).
    /// Use case: NMI must push exactly three bytes onto the stack
    /// in 6502 order: PC high, PC low, processor status. The stack
    /// pointer must decrement by three.
    /// Acceptance: with S=0xFF and PC=0x0400, after <see cref="Mos6502.Nmi"/>
    /// the bytes at $01FF/$01FE/$01FD hold PCH/PCL/P respectively and
    /// the resulting S equals 0xFC.
    /// </summary>
    [Fact]
    public void Nmi_PushesThreeByteStackFrameInPushOrder()
    {
        // Arrange
        var bus = new MockBus();
        var cpu = new Mos6502(bus);
        cpu.S = 0xFF;
        cpu.PC = 0x0400;
        cpu.P = 0x20; // Just unused bit set - simple to verify post-mask below
        bus.SetMemory(0xFFFA, 0x00);
        bus.SetMemory(0xFFFB, 0x08);

        // Act
        cpu.Nmi();

        // Assert
        Assert.Equal(0x04, bus.Read(0x01FF)); // PCH pushed first
        Assert.Equal(0x00, bus.Read(0x01FE)); // PCL pushed second
        Assert.Equal(0x20, bus.Read(0x01FD)); // P pushed third (B=0)
        Assert.Equal(0xFC, cpu.S);            // S decremented three times
    }

    /// <summary>
    /// FR/TR: FR-CPU (BACKFILL-CPU 6510 NMI B-flag-clear semantic).
    /// Use case: software inside the NMI handler inspects the pushed
    /// processor status to distinguish a hardware interrupt (NMI/IRQ)
    /// from a BRK instruction. The B flag (bit 4, $10) must be 0 in
    /// the pushed byte for NMI, even when B was 1 in the live P
    /// register at the time of dispatch.
    /// Acceptance: with cpu.P pre-set including B=1, after
    /// <see cref="Mos6502.Nmi"/> the byte at $01FD has bit 4 cleared.
    /// </summary>
    [Fact]
    public void Nmi_ClearsBFlagInPushedStatus()
    {
        // Arrange
        var bus = new MockBus();
        var cpu = new Mos6502(bus);
        cpu.S = 0xFF;
        cpu.PC = 0x0400;
        cpu.P = 0x30; // B + unused both set going in
        bus.SetMemory(0xFFFA, 0x00);
        bus.SetMemory(0xFFFB, 0x08);

        // Act
        cpu.Nmi();

        // Assert
        var pushedStatus = bus.Read(0x01FD);
        Assert.Equal(0x00, pushedStatus & 0x10); // B flag MUST be 0 in pushed byte
        Assert.Equal(0x20, pushedStatus & 0x20); // unused bit preserved
    }

    /// <summary>
    /// FR/TR: FR-CPU (BACKFILL-CPU 6510 NMI / RTI round trip).
    /// Use case: a minimal NMI handler that is just RTI must restore
    /// the CPU to the exact state it was in pre-NMI: original PC, S
    /// back to its pre-NMI value, and the original P value (modulo
    /// the documented B-flag handling on the way out via RTI's pull).
    /// Acceptance: after dispatching NMI and then executing one RTI
    /// instruction (opcode 0x40), PC equals the pre-NMI PC and S
    /// equals the pre-NMI S.
    /// </summary>
    [Fact]
    public void Nmi_RtiReturnsCpuToPreNmiState()
    {
        // Arrange
        var bus = new MockBus();
        var cpu = new Mos6502(bus);
        cpu.S = 0xFF;
        const ushort originalPc = 0x0400;
        cpu.PC = originalPc;
        cpu.P = 0x20;
        bus.SetMemory(0xFFFA, 0x00);
        bus.SetMemory(0xFFFB, 0x08);
        bus.SetMemory(0x0800, 0x40); // RTI as the entire NMI handler

        // Act - dispatch NMI then step until RTI completes
        cpu.Nmi();
        Assert.Equal(0x0800, cpu.PC);   // sanity: handler entered
        Assert.Equal(0xFC, cpu.S);      // sanity: three bytes pushed

        // RTI is a 6-cycle instruction. Tick until back at an
        // instruction boundary with PC restored.
        StepUntilPcReaches(cpu, originalPc, maxTicks: 64);

        // Assert
        Assert.Equal(originalPc, cpu.PC);
        Assert.Equal(0xFF, cpu.S);
    }

    /// <summary>
    /// FR/TR: FR-CPU (BACKFILL-CPU 6510 NMI edge-trigger latch).
    /// Use case: the 6510 NMI input is edge-triggered. Holding the
    /// line asserted (low) after a previous NMI was taken must NOT
    /// cause a second dispatch. Only a release-then-reassert sequence
    /// produces a new edge that fires a second NMI. The latch lives
    /// in <see cref="SystemClock.UpdateNmiEdgeLatch"/>.
    /// Acceptance: with the NMI line held continuously asserted across
    /// many clock steps, the CPU enters the handler exactly once.
    /// After release + re-assert, the CPU enters the handler a second
    /// time.
    /// </summary>
    [Fact]
    public void Nmi_IsEdgeTriggered_HoldingLineDoesNotRefire()
    {
        // Arrange - NOP loop at $0400, RTI-only NMI handler at $0800
        // Bus must also expose a valid reset vector for cpu.Reset().
        var bus = new MockBus();
        var cpu = new Mos6502(bus);
        var nmiLine = new InterruptLine(InterruptType.Nmi);
        var irqLine = new InterruptLine(InterruptType.Irq);
        var clock = new SystemClock(985248, cpu, irqLine, nmiLine);
        clock.Register(cpu);

        // NOP at $0400, JMP $0400 right after - a tight 5-cycle loop
        bus.SetMemory(0x0400, 0xEA);     // NOP
        bus.SetMemory(0x0401, 0x4C);     // JMP $0400
        bus.SetMemory(0x0402, 0x00);
        bus.SetMemory(0x0403, 0x04);

        // Reset vector points at the NOP loop
        bus.SetMemory(0xFFFC, 0x00);
        bus.SetMemory(0xFFFD, 0x04);
        // NMI vector points at handler
        bus.SetMemory(0xFFFA, 0x00);
        bus.SetMemory(0xFFFB, 0x08);
        // Handler: RTI
        bus.SetMemory(0x0800, 0x40);

        cpu.Reset();
        var source = new DummyNmiSource();

        // Burn a few cycles so we are inside the NOP loop at an
        // instruction boundary before any NMI is asserted.
        for (var i = 0; i < 8; i++)
            clock.Step();

        // Act - assert NMI line and hold it low
        nmiLine.Assert(source);

        // Step enough cycles for: edge latch to fire, then NMI dispatch,
        // then handler RTI to complete, then several more loop iterations.
        var enteredHandlerCount = 0;
        for (var i = 0; i < 64; i++)
        {
            clock.Step();
            // Detect the moment we land at handler entry $0800 on an
            // instruction boundary.
            if (cpu.PC == 0x0800 && cpu.IsInstructionBoundary)
            {
                enteredHandlerCount++;
                // Keep line asserted - do NOT release
            }
        }

        // Assert - handler entered exactly once despite line still held
        Assert.Equal(1, enteredHandlerCount);
        Assert.True(nmiLine.IsAsserted); // still held
        // CPU should be back in the NOP loop somewhere in $0400..$0403
        Assert.InRange(cpu.PC, (ushort)0x0400, (ushort)0x0403);

        // Now release and re-assert: that should produce a fresh edge
        nmiLine.Release(source);
        for (var i = 0; i < 4; i++)
            clock.Step();
        nmiLine.Assert(source);

        for (var i = 0; i < 64; i++)
        {
            clock.Step();
            if (cpu.PC == 0x0800 && cpu.IsInstructionBoundary)
            {
                enteredHandlerCount++;
            }
        }

        Assert.Equal(2, enteredHandlerCount);
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
                return;
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

    private sealed class DummyNmiSource : IInterruptSource
    {
        public DeviceId Id => new DeviceId(0xDEAD_BEEF);
        public DeviceId SourceId => Id;
        public string Name => "Test NMI source";
        public IReadOnlyList<IInterruptLine> ConnectedLines => Array.Empty<IInterruptLine>();
        public void Reset() { }
    }
}
