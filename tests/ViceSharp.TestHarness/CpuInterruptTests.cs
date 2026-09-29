namespace ViceSharp.TestHarness;

using Xunit;
using ViceSharp.Chips.Cpu;
using ViceSharp.Abstractions;
using ViceSharp.Core;

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

    /// <summary>
    /// FR: FR-CPU-003, TR: TR-CYCLE-001.
    /// Use case: Wolf64 sample 2060428. VICE CLI sets OPINFO_ENABLES_IRQ when
    ///   I was set, then <c>interrupt_check_irq_delay</c> refuses to dispatch
    ///   (sets IK_IRQPEND) so the following instruction runs. Managed armed
    ///   IRQ on the CLI last CLK and pushed (nS=$FF mS=$FE).
    /// Acceptance: With IRQ asserted and I set, CLI last CLK leaves S unchanged;
    ///   the next NOP completes with S still unchanged; the first stack push
    ///   happens only after that NOP.
    /// </summary>
    [Fact]
    public void Cli_WithIrqPending_DoesNotPushUntilAfterNextInstruction()
    {
        var bus = new MockBus();
        bus.SetMemory(0x8000, 0xEA);
        bus.SetMemory(0x8001, 0xEA);
        bus.SetMemory(0x8002, 0xEA);
        bus.SetMemory(0x8003, 0x58); // CLI after IRQ delay has elapsed
        bus.SetMemory(0x8004, 0xEA); // NOP that must complete before IRQ
        bus.SetMemory(0x8005, 0xEA);
        bus.SetMemory(0xFFFE, 0x00);
        bus.SetMemory(0xFFFF, 0x09);

        var irq = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus)
        {
            PC = 0x8000,
            S = 0xFF,
            P = 0x24
        };
        var clock = new SystemClock(985_248, cpu, irq);
        clock.Register(cpu);
        irq.Assert(new StubIrqSource());

        for (var i = 0; i < 32 && !(cpu.DebugOpcode == 0x58 && cpu.DebugCycle == 0); i++)
            clock.Step();

        Assert.Equal((byte)0x58, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal(0x00, cpu.P & 0x04);
        Assert.Equal((byte)0xFF, cpu.S);

        clock.Step();
        Assert.Equal((byte)0xFF, cpu.S);

        for (var i = 0; i < 8 && !(cpu.DebugOpcode == 0xEA && cpu.DebugCycle == 0); i++)
            clock.Step();

        Assert.Equal((byte)0xEA, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0xFF, cpu.S);

        clock.Step();
        Assert.Equal((byte)0xFF, cpu.S);

        clock.Step();
        Assert.Equal((byte)0xFE, cpu.S);
    }

    /// <summary>
    /// FR: FR-CPU-003, TR: TR-CYCLE-001.
    /// Use case: Wolf64 sample 2093250. VICE RTS() LOADs then CLK_INC, then
    ///   JUMP with no extra CLK; DO_INTERRUPT is paired with FETCH of the
    ///   return insn. Managed <c>_delayNextFetch</c> after RTS is a host tick
    ///   at the return PC with cycle 0, so IRQ dispatched and pushed (nPC=$EA0C
    ///   nA=$20 nS=$EE mPC=$EA0A mA=$0E mS=$ED).
    /// Acceptance: RTS returning to LDA #$20 with IRQ already pending does not
    ///   decrement S on the jump-visible tick; LDA completes with A=$20 first.
    /// </summary>
    [Fact]
    public void Rts_DelayNextFetchTick_DoesNotTakeIrqBeforeReturnFetch()
    {
        var bus = new MockBus();
        bus.SetMemory(0x8000, 0x60); // RTS
        bus.SetMemory(0x8003, 0xA9); // LDA #$20
        bus.SetMemory(0x8004, 0x20);
        bus.SetMemory(0x01FE, 0x02);
        bus.SetMemory(0x01FF, 0x80);
        bus.SetMemory(0xFFFE, 0x00);
        bus.SetMemory(0xFFFF, 0x09);

        var irq = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus)
        {
            PC = 0x8000,
            S = 0xFD,
            P = 0x20
        };
        var clock = new SystemClock(985_248, cpu, irq);
        clock.Register(cpu);

        for (var i = 0; i < 16 && cpu.PC != 0x8003; i++)
            clock.Step();

        Assert.Equal((ushort)0x8003, cpu.PC);
        // Assert on the RTS JUMP tick (no CLK). VICE DO_INTERRUPT is the next
        // FETCH; irq_clk+2 has not elapsed yet, so LDA #$20 still runs
        // (Wolf64 2093250). IRQ already pending for a long time is 2109682.
        irq.Assert(new StubIrqSource());

        for (var i = 0; i < 24 && !(cpu.DebugOpcode == 0xA9 && cpu.DebugCycle == 0 && cpu.A == 0x20); i++)
            clock.Step();

        Assert.Equal((byte)0xA9, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x20, cpu.A);
        Assert.True(cpu.S >= 0xFD, $"IRQ pushed before LDA #$20; S=${cpu.S:X2}");
    }

    /// <summary>
    /// FR: FR-CPU-003, TR: TR-CYCLE-001.
    /// Use case: Wolf64 sample 2093251. VICE LDA # GET_IMM CLK_INC then INC_PC
    ///   with no extra CLK; the next host tick is DO_INTERRUPT plus FETCH of
    ///   STA ($D1),Y at $EA0C. Managed stayed on opcode $A9 cycle 0 for one
    ///   extra dwell, then took IRQ (nPC=$EA0E nS=$EE mS=$ED).
    /// Acceptance: After RTS into LDA #$20 last CLK, the next Step fetches
    ///   the following NOP; S is unchanged.
    /// </summary>
    [Fact]
    public void RtsThenLdaImm_NextTick_FetchesFollowingOpcode()
    {
        var bus = new MockBus();
        bus.SetMemory(0x8000, 0x60); // RTS
        bus.SetMemory(0x8003, 0xA9); // LDA #$20
        bus.SetMemory(0x8004, 0x20);
        bus.SetMemory(0x8005, 0xEA); // NOP (stand-in for STA (zp),Y)
        bus.SetMemory(0x01FE, 0x02);
        bus.SetMemory(0x01FF, 0x80);

        var irq = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus)
        {
            PC = 0x8000,
            S = 0xFD,
            P = 0x20
        };
        var clock = new SystemClock(985_248, cpu, irq);
        clock.Register(cpu);

        for (var i = 0; i < 24 && !(cpu.DebugOpcode == 0xA9 && cpu.DebugCycle == 0 && cpu.A == 0x20); i++)
            clock.Step();

        Assert.Equal((byte)0xA9, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x20, cpu.A);
        var stack = cpu.S;

        clock.Step();

        Assert.Equal((byte)0xEA, cpu.DebugOpcode);
        Assert.Equal(stack, cpu.S);
    }

    /// <summary>
    /// FR: FR-CPU-003, TR: TR-CYCLE-001.
    /// Use case: Wolf64 sample 2093252. VICE C64 <c>interrupt_check_irq_delay</c>
    ///   needs <c>irq_delay_cycles &gt;= INTERRUPT_DELAY</c> (2). IRQ that
    ///   arrives on LDA # last CLK increments delay once; DO_INTERRUPT of the
    ///   next insn still sees delay=1 and FETCHes. Managed extra dwell let
    ///   wall-clock irq_clk+2 elapse at a fake boundary and pushed.
    /// Acceptance: IRQ asserted on LDA #$20 last CLK does not decrement S on
    ///   the next Step; that Step fetches the following NOP.
    /// </summary>
    [Fact]
    public void LdaImm_IrqOnLastClk_DoesNotDispatchBeforeNextFetch()
    {
        var bus = new MockBus();
        bus.SetMemory(0x8000, 0xA9); // LDA #$20
        bus.SetMemory(0x8001, 0x20);
        bus.SetMemory(0x8002, 0xEA); // NOP
        bus.SetMemory(0xFFFE, 0x00);
        bus.SetMemory(0xFFFF, 0x09);

        var irq = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus)
        {
            PC = 0x8000,
            S = 0xFF,
            P = 0x20
        };
        var clock = new SystemClock(985_248, cpu, irq);
        clock.Register(cpu);

        for (var i = 0; i < 8 && !(cpu.DebugOpcode == 0xA9 && cpu.DebugCycle == 0 && cpu.A == 0x20); i++)
            clock.Step();

        Assert.Equal((byte)0xA9, cpu.DebugOpcode);
        Assert.Equal(0, cpu.DebugCycle);
        Assert.Equal((byte)0x20, cpu.A);
        Assert.Equal((byte)0xFF, cpu.S);

        irq.Assert(new StubIrqSource());
        clock.Step();

        Assert.Equal((byte)0xEA, cpu.DebugOpcode);
        Assert.Equal((byte)0xFF, cpu.S);
    }

    /// <summary>
    /// FR: FR-CPU-003, TR: TR-CYCLE-001 / TR-LOCKSTEP-VSF-001.
    /// Use case: Wolf64 sample 2208208. VICE <c>interrupt_set_irq</c> updates
    ///   <c>irq_clk</c> only when <c>nirq</c> goes 0 to 1. A same-cycle Release
    ///   then Assert (CIA ack then RAISE0) must relatch. Polling
    ///   <see cref="IInterruptLine.IsAsserted"/> at end of the host tick misses
    ///   that edge, so a stale assert cycle makes the pre-FETCH sample treat
    ///   delay as elapsed (mS=$F2) while x64sc <c>nIrqClk=2208210</c> still
    ///   runs STA zp.
    /// Acceptance: IRQ asserted and delay elapsed, then the source Releases
    ///   and Asserts in one Phi2 tick; the next instruction boundary does not
    ///   push (fresh irq_clk). STA/NOP after that still executes with S
    ///   unchanged for at least two steps.
    /// </summary>
    [Fact]
    public void IrqLine_ReleaseThenAssertSameCycle_RelatchesIrqClk_DoesNotDispatchImmediately()
    {
        var bus = new MockBus();
        bus.SetMemory(0xFFFC, 0x00);
        bus.SetMemory(0xFFFD, 0x80);
        bus.SetMemory(0xFFFE, 0x00);
        bus.SetMemory(0xFFFF, 0x09);
        bus.SetMemory(0x8000, 0xEA); // NOP
        bus.SetMemory(0x8001, 0x4C); // JMP $8000
        bus.SetMemory(0x8002, 0x00);
        bus.SetMemory(0x8003, 0x80);

        var irq = new InterruptLine(InterruptType.Irq);
        var cpu = new Mos6502(bus);
        var reedge = new SameCycleReEdgeDevice(irq);
        var clock = new SystemClock(985_248, cpu, irq);
        clock.Register(reedge);
        clock.Register(cpu);
        cpu.Reset();
        cpu.P = 0x20;
        cpu.S = 0xF3;
        cpu.PC = 0x8000;
        irq.Assert(reedge);

        for (var i = 0; i < 8; i++)
            clock.Step();
        Assert.True(cpu.S < 0xF3, "IRQ must first become delay-elapsed so a stale latch would dispatch.");

        for (var i = 0; i < 16 && cpu.DebugInterruptSequenceRemaining > 0; i++)
            clock.Step();
        Assert.Equal(0, cpu.DebugInterruptSequenceRemaining);

        cpu.S = 0xF3;
        cpu.P = 0x20;
        cpu.PC = 0x8000;
        reedge.ReEdgeNextTick = true;
        clock.Step();

        var sAfterEdge = cpu.S;
        clock.Step();
        clock.Step();

        Assert.Equal((byte)0xF3, sAfterEdge);
        Assert.Equal((byte)0xF3, cpu.S);
    }

    private sealed class SameCycleReEdgeDevice : IClockedDevice, IInterruptSource
    {
        private readonly IInterruptLine _irq;
        public SameCycleReEdgeDevice(IInterruptLine irq) => _irq = irq;
        public bool ReEdgeNextTick { get; set; }
        public DeviceId Id { get; } = new(0x00C1);
        public DeviceId SourceId => Id;
        public string Name => "re-edge-irq";
        public uint ClockDivisor => 1;
        public ClockPhase Phase => ClockPhase.Phi2;
        public IReadOnlyList<IInterruptLine> ConnectedLines { get; } = [];
        public void Reset() { }
        public void Tick()
        {
            if (!ReEdgeNextTick)
                return;
            ReEdgeNextTick = false;
            _irq.Release(this);
            _irq.Assert(this);
        }
    }

    private sealed class StubIrqSource : IInterruptSource
    {
        public DeviceId Id { get; } = new(0x00FE);
        public DeviceId SourceId => Id;
        public string Name => "stub-irq";
        public IReadOnlyList<IInterruptLine> ConnectedLines { get; } = [];
        public void Reset() { }
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
