namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.Cpu;
using ViceSharp.Core;
using Xunit;

public sealed class CiaTimerInterruptTests
{
    /// <summary>
    /// FR: FR-CIA-001, FR: FR-CIA-007, TR: TR-CYCLE-001.
    /// Use case: Configure CIA Timer A as a one-shot, enable underflow IRQ
    /// in the ICR mask, and verify the IRQ line is asserted on underflow
    /// and cleared when the ICR is read.
    /// Acceptance: IRQ remains low until the timer underflows, then
    /// reading $DC0D returns the underflow flag plus the IR bit ($81) and
    /// the IRQ line drops back to deasserted.
    /// </summary>
    [Fact]
    public void TimerA_AssertsIrqWhenEnabledAndClearsOnIcrRead()
    {
        var bus = new BasicBus();
        var irq = new InterruptLine(InterruptType.Irq);
        var cia = new Mos6526(bus, irq);

        cia.Write(0xDC04, 0x02);
        cia.Write(0xDC05, 0x00);
        cia.Write(0xDC0D, 0x81);
        cia.Write(0xDC0E, 0x11);

        for (var cycle = 0; cycle < 4; cycle++)
            cia.Tick();

        Assert.False(irq.IsAsserted);

        cia.Tick();

        Assert.True(irq.IsAsserted);
        Assert.Equal(0x81, cia.Read(0xDC0D));
        Assert.False(irq.IsAsserted);
    }

    /// <summary>
    /// FR: FR-CIA-001, TR: TR-CYCLE-001.
    /// Use case: Without enabling the Timer A IRQ mask in the ICR, the
    /// timer underflow flag must still latch but no IRQ line should
    /// assert.
    /// Acceptance: Reading $DC0D returns the underflow bit ($01) without
    /// the IR (master) bit set, and the IRQ line remains deasserted
    /// throughout.
    /// </summary>
    [Fact]
    public void TimerA_UnmaskedUnderflowSetsFlagWithoutAssertingIrq()
    {
        var bus = new BasicBus();
        var irq = new InterruptLine(InterruptType.Irq);
        var cia = new Mos6526(bus, irq);

        cia.Write(0xDC04, 0x01);
        cia.Write(0xDC05, 0x00);
        cia.Write(0xDC0E, 0x11);

        for (var cycle = 0; cycle < 3; cycle++)
            cia.Tick();

        Assert.False(irq.IsAsserted);
        Assert.Equal(0x00, cia.Read(0xDC0D));

        cia.Tick();

        Assert.False(irq.IsAsserted);
        Assert.Equal(0x01, cia.Read(0xDC0D));
        Assert.False(irq.IsAsserted);
    }

    /// <summary>
    /// FR: FR-CIA-001, TR: TR-CYCLE-001.
    /// Use case: Configure Timer B to count Timer A underflows (CRB
    /// INMODE=10) so the two timers cascade as a 32-bit counter; verify
    /// Timer B does not advance on phi2 alone but does count Timer A
    /// underflow events.
    /// Acceptance: Timer B remains idle across 16 phi2 cycles without
    /// Timer A running, then asserts an IRQ on the cycle that Timer A
    /// underflows and the IRQ line drops to ICR value $83.
    /// </summary>
    [Fact]
    public void TimerB_CanCountTimerAUnderflowsInsteadOfPhi2()
    {
        var bus = new BasicBus();
        var irq = new InterruptLine(InterruptType.Irq);
        var cia = new Mos6526(bus, irq);

        cia.Write(0xDC04, 0x01);
        cia.Write(0xDC05, 0x00);
        cia.Write(0xDC06, 0x01);
        cia.Write(0xDC07, 0x00);
        cia.Write(0xDC0D, 0x82);
        cia.Write(0xDC0F, 0x51);

        for (var cycle = 0; cycle < 16; cycle++)
            cia.Tick();

        Assert.False(irq.IsAsserted);
        Assert.Equal(0x00, cia.Read(0xDC0D));

        cia.Write(0xDC0E, 0x11);

        for (var cycle = 0; cycle < 9; cycle++)
            cia.Tick();

        Assert.False(irq.IsAsserted);

        cia.Tick();

        Assert.True(irq.IsAsserted);
        Assert.Equal(0x83, cia.Read(0xDC0D));
    }

    /// <summary>
    /// FR: FR-CIA-002, FR: FR-CIA-TOD, TR: TR-CYCLE-001.
    /// Use case: With CRB ALARM=0, writes to $DC08-$DC0B target the TOD
    /// clock latch; with CRB ALARM=1 (bit 7) those writes update the alarm
    /// latch instead. On TOD == alarm the CIA must raise the ALARM IRQ.
    /// Real 6526 HOUR is 12-hour BCD (1..12) with AM/PM in bit 7; the
    /// 12 PM -> 1 AM rollover toggles bit 7.
    /// Acceptance: After loading the clock with $12:59:59:09 PM and the
    /// alarm with $01:00:00:00 AM, a single ClockTod() ticks the clock to
    /// match the alarm and asserts the ICR ALARM bit ($84).
    /// </summary>
    [Fact]
    public void TodWritesTargetClockByDefaultAndAlarmWhenCrbAlarmBitIsSet()
    {
        var bus = new BasicBus();
        var irq = new InterruptLine(InterruptType.Irq);
        var cia = new Mos6526(bus, irq);

        // CRB.7 = 0 by default -> writes target live TOD clock.
        // Load CLOCK = 12:59:59.09 PM (HOUR = 0x92).
        cia.Write(0xDC08, 0x09);
        cia.Write(0xDC09, 0x59);
        cia.Write(0xDC0A, 0x59);
        cia.Write(0xDC0B, 0x92);

        // CRB.7 = 1 -> writes target ALARM. Load ALARM = 01:00:00.00 AM.
        cia.Write(0xDC0F, 0x80);
        cia.Write(0xDC08, 0x00);
        cia.Write(0xDC09, 0x00);
        cia.Write(0xDC0A, 0x00);
        cia.Write(0xDC0B, 0x01);
        cia.Write(0xDC0D, 0x84);

        cia.ClockTod();

        Assert.Equal(0x00, cia.Read(0xDC08));
        Assert.Equal(0x00, cia.Read(0xDC09));
        Assert.Equal(0x00, cia.Read(0xDC0A));
        Assert.Equal(0x01, cia.Read(0xDC0B));
        Assert.True(irq.IsAsserted);
        Assert.Equal(0x84, cia.Read(0xDC0D));
    }

    /// <summary>
    /// FR: FR-CIA-006, TR: TR-CYCLE-001.
    /// Use case: A CIA2 timer underflow on the C64 routes to the CPU's
    /// NMI line (not IRQ); the SystemClock must dispatch the NMI on the
    /// underflow cycle and the CPU must execute the standard NMI sequence.
    /// Acceptance: After 4 cycles of clock stepping, the CPU's PC equals
    /// the NMI vector target ($0800), I is set, and PCL/PCH are on stack.
    /// </summary>
    [Fact]
    public void SystemClock_DispatchesCia2TimerUnderflowAsNmi()
    {
        var bus = new MockBus();
        var irq = new InterruptLine(InterruptType.Irq);
        var nmi = new InterruptLine(InterruptType.Nmi);
        var cpu = new Mos6502(bus)
        {
            PC = 0x0400,
            S = 0xFF
        };
        var cia2 = new Mos6526(bus, nmi) { BaseAddress = 0xDD00 };
        var clock = new SystemClock(985_248, cpu, irq, nmi);
        clock.Register(cia2);

        bus.SetMemory(0xFFFA, 0x00);
        bus.SetMemory(0xFFFB, 0x08);
        cia2.Write(0xDD04, 0x01);
        cia2.Write(0xDD05, 0x00);
        cia2.Write(0xDD0D, 0x81);
        cia2.Write(0xDD0E, 0x11);

        clock.Step(3);
        Assert.Equal(0x0400, cpu.PC);

        clock.Step();

        Assert.Equal(0x0800, cpu.PC);
        Assert.Equal(0x04, cpu.P & 0x04);
        Assert.Equal(0x00, bus.Read(0x01FE));
        Assert.Equal(0x04, bus.Read(0x01FF));
    }

    /// <summary>
    /// FR: FR-CIA-006, TR: TR-CYCLE-001.
    /// Use case: NMI is edge-triggered: while the CIA2 NMI line stays
    /// asserted (because the ICR was not read to clear it), the CPU must
    /// not re-enter the NMI sequence on later cycles.
    /// Acceptance: After 4 cycles the CPU executes NMI once; running an
    /// additional 4 cycles must leave the stack pointer unchanged and PC
    /// still inside the handler ($0800).
    /// </summary>
    [Fact]
    public void SystemClock_DoesNotRetriggerNmiWhileLineRemainsAsserted()
    {
        var bus = new MockBus();
        var irq = new InterruptLine(InterruptType.Irq);
        var nmi = new InterruptLine(InterruptType.Nmi);
        var cpu = new Mos6502(bus)
        {
            PC = 0x0400,
            S = 0xFF
        };
        var cia2 = new Mos6526(bus, nmi) { BaseAddress = 0xDD00 };
        var clock = new SystemClock(985_248, cpu, irq, nmi);
        clock.Register(cia2);

        bus.SetMemory(0xFFFA, 0x00);
        bus.SetMemory(0xFFFB, 0x08);
        cia2.Write(0xDD04, 0x01);
        cia2.Write(0xDD05, 0x00);
        cia2.Write(0xDD0D, 0x81);
        cia2.Write(0xDD0E, 0x11);

        clock.Step(4);
        var stackAfterFirstNmi = cpu.S;

        clock.Step(4);

        Assert.True(nmi.IsAsserted);
        Assert.Equal(0x0800, cpu.PC);
        Assert.Equal(stackAfterFirstNmi, cpu.S);
    }

    private sealed class MockBus : IBus
    {
        private readonly byte[] _memory = new byte[65536];

        public void SetMemory(int address, byte value) => _memory[address & 0xFFFF] = value;

        public byte Read(ushort address) => _memory[address];

        public void Write(ushort address, byte value) => _memory[address] = value;

        public byte Peek(ushort address) => _memory[address];

        public void RegisterDevice(IAddressSpace device)
        {
        }

        public void UnregisterDevice(IAddressSpace device)
        {
        }
    }
}
