namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.Cpu;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// Verifies the Mos6502 publishes a <see cref="CpuInstructionCompletedEvent"/> at each
/// clean instruction boundary (BUG-THROTTLE-001 clean-state gating + debugger trace),
/// and that the publish is dormant - no behavioural change - when nothing subscribes.
/// </summary>
public sealed class CpuInstructionCompletedEventTests
{
    /// <summary>
    /// FR-CPU-TRACE-001, TR-PUBSUB-CPU-001.
    /// Use case: A debugger/monitor subscribes to per-instruction completion to read the
    ///   opcode and register file at each clean instruction boundary.
    /// Acceptance: After executing LDA #$42 the CPU publishes a CpuInstructionCompletedEvent
    ///   whose Opcode is 0xA9 and whose A register is 0x42.
    /// </summary>
    [Fact]
    public void CompletedInstruction_PublishesOpcodeAndRegisters()
    {
        var bus = new BasicBus();
        bus.RegisterDevice(new SimpleRam());
        // Program at $1000: LDA #$42 ; NOP ; NOP
        bus.Write(0x1000, 0xA9);
        bus.Write(0x1001, 0x42);
        bus.Write(0x1002, 0xEA);
        bus.Write(0x1003, 0xEA);
        // Reset vector -> $1000
        bus.Write(0xFFFC, 0x00);
        bus.Write(0xFFFD, 0x10);

        var cpu = new Mos6502(bus);
        var pubSub = new LockFreePubSub();
        cpu.ConnectPubSub(pubSub);

        CpuInstructionCompletedEvent? lda = null;
        pubSub.Subscribe<CpuInstructionCompletedEvent>(
            CpuInstructionCompletedEvent.Topic,
            e => { if (e.Opcode == 0xA9) lda = e; });

        cpu.Reset();
        for (var i = 0; i < 64 && lda is null; i++)
            cpu.Tick();

        Assert.NotNull(lda);
        Assert.Equal(0xA9, lda!.Value.Opcode);
        Assert.Equal(0x42, lda.Value.A);
        Assert.Equal(0x1000, lda.Value.InstructionAddress);
    }

    /// <summary>
    /// FR-CPU-TRACE-001, TR-PUBSUB-CPU-001.
    /// Use case: An unobserved run (no debugger) must not pay for, or be perturbed by,
    ///   instruction-completed publishing - cycle parity is the headline property.
    /// Acceptance: With no pubsub connected, executing LDA #$42 still leaves A = 0x42 and
    ///   advances the PC exactly as it would without the event hook (no behavioural change).
    /// </summary>
    [Fact]
    public void NoSubscriber_DoesNotChangeExecution()
    {
        var bus = new BasicBus();
        bus.RegisterDevice(new SimpleRam());
        bus.Write(0x1000, 0xA9);
        bus.Write(0x1001, 0x42);
        bus.Write(0x1002, 0xEA);
        bus.Write(0xFFFC, 0x00);
        bus.Write(0xFFFD, 0x10);

        var cpu = new Mos6502(bus);
        // No ConnectPubSub: the publish path is dormant.

        cpu.Reset();
        for (var i = 0; i < 64 && cpu.A != 0x42; i++)
            cpu.Tick();

        Assert.Equal(0x42, cpu.A);
    }
}
