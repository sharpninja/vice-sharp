namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.Cpu;
using ViceSharp.Core;
using Xunit;

public sealed class CpuVisiblePcTimingTests
{
    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: <c>JSR abs</c> spans six bus cycles; the externally
    /// visible PC must reflect the jump target after the high operand byte
    /// is fetched on cycle 7 (post-reset cycle count).
    /// Acceptance: After 7 ticks following reset, PC equals the JSR target
    /// $9000 (high-byte fetch latched the destination address).
    /// </summary>
    [Fact]
    public void Jsr_FinalCycle_ExposesTargetAfterHighOperandFetch()
    {
        var cpu = CreateJsrCpu();

        for (var i = 0; i < 7; i++)
            cpu.Tick();

        Assert.Equal((ushort)0x9000, cpu.PC);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: Lockstep diagnostics need to subscribe to CPU subroutine
    /// calls so a KERNAL IEC close call can mark the stable post-load window.
    /// Acceptance: Completing <c>JSR $9000</c> publishes a typed control
    /// transfer event with the source PC, target PC, opcode, and return PC.
    /// </summary>
    [Fact]
    public void Jsr_FinalCycle_PublishesControlTransferEvent()
    {
        var cpu = CreateJsrCpu();
        var pubSub = new LockFreePubSub();
        CpuControlTransferEvent? observed = null;
        var subscription = pubSub.Subscribe<CpuControlTransferEvent>(
            CpuControlTransferEvent.Topic,
            transfer => observed = transfer);

        try
        {
            cpu.ConnectPubSub(pubSub);

            for (var i = 0; i < 7; i++)
                cpu.Tick();
        }
        finally
        {
            pubSub.Unsubscribe(subscription);
        }

        Assert.True(observed.HasValue);
        Assert.Equal((ushort)0x8000, observed.Value.Source);
        Assert.Equal((ushort)0x9000, observed.Value.Target);
        Assert.Equal((ushort)0x8003, observed.Value.ReturnPc);
        Assert.Equal((byte)0x20, observed.Value.Opcode);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: The final operand read of <c>JSR abs</c> must remain a
    /// stealable bus cycle so VIC-II DMA can suspend the CPU at exactly
    /// that point without corrupting the instruction.
    /// Acceptance: After 6 ticks, the CPU reports DebugCycle == 1 and
    /// <see cref="Mos6502.CanStealCurrentCycle"/> is true.
    /// </summary>
    [Fact]
    public void Jsr_FinalOperandRead_CanBeHeldByExternalBusOwner()
    {
        var cpu = CreateJsrCpu();

        for (var i = 0; i < 6; i++)
            cpu.Tick();

        Assert.Equal(1, cpu.DebugCycle);
        Assert.True(cpu.CanStealCurrentCycle);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: Hosted VICE exposes the first <c>JSR()</c> return-address
    /// push at the third post-reset checkpoint for the reset ROM
    /// <c>JSR $FCE2</c> sequence.
    /// Acceptance: The managed CPU decrements the stack pointer on that same
    /// checkpoint, then performs the low-byte push on the next checkpoint and
    /// reaches the target on the final high-byte operand load.
    /// </summary>
    [Fact]
    public void Jsr_ReturnAddressPushTimingMatchesHostedVice()
    {
        var cpu = CreateJsrCpu();
        cpu.S = 0xF9;

        for (var i = 0; i < 4; i++)
            cpu.Tick();

        Assert.Equal((byte)0xF8, cpu.S);
        Assert.Equal(3, cpu.DebugCycle);

        cpu.Tick();

        Assert.Equal((byte)0xF7, cpu.S);
        Assert.Equal(2, cpu.DebugCycle);

        cpu.Tick();

        Assert.Equal((byte)0xF7, cpu.S);
        Assert.Equal(1, cpu.DebugCycle);

        cpu.Tick();

        Assert.Equal((ushort)0x9000, cpu.PC);
        Assert.Equal(0, cpu.DebugCycle);
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: VICE's x64sc <c>RTS()</c> performs its stack operations across
    /// consecutive <c>CLK_INC()</c> boundaries.
    /// Acceptance: At the point the managed CPU exposes <c>DebugCycle == 2</c>,
    /// native VICE has completed the low-byte pull and advanced the stack
    /// pointer once.
    /// </summary>
    [Fact]
    public void Rts_DebugCycleTwo_HasCompletedLowBytePull()
    {
        var memory = new byte[0x10000];
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0x60; // RTS
        memory[0x01F8] = 0x34;
        memory[0x01F9] = 0x12;

        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));

        var cpu = new Mos6502(bus);
        cpu.Reset();
        cpu.S = 0xF7;

        for (var i = 0; i < 5; i++)
            cpu.Tick();

        Assert.Equal((byte)0xF8, cpu.S);
        Assert.Equal(2, cpu.DebugCycle);

        cpu.Tick();

        Assert.Equal((byte)0xF9, cpu.S);
        Assert.Equal(1, cpu.DebugCycle);
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
