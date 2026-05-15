namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.Cpu;
using ViceSharp.Core;
using Xunit;

public sealed class CiaTimerInterruptTests
{
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

    [Fact]
    public void TodWritesTargetClockByDefaultAndAlarmWhenCrbAlarmBitIsSet()
    {
        var bus = new BasicBus();
        var irq = new InterruptLine(InterruptType.Irq);
        var cia = new Mos6526(bus, irq);

        cia.Write(0xDC08, 0x09);
        cia.Write(0xDC09, 0x59);
        cia.Write(0xDC0A, 0x59);
        cia.Write(0xDC0B, 0x23);

        cia.Write(0xDC0F, 0x80);
        cia.Write(0xDC08, 0x00);
        cia.Write(0xDC09, 0x00);
        cia.Write(0xDC0A, 0x00);
        cia.Write(0xDC0B, 0x00);
        cia.Write(0xDC0D, 0x84);

        cia.ClockTod();

        Assert.Equal(0x00, cia.Read(0xDC08));
        Assert.Equal(0x00, cia.Read(0xDC09));
        Assert.Equal(0x00, cia.Read(0xDC0A));
        Assert.Equal(0x00, cia.Read(0xDC0B));
        Assert.True(irq.IsAsserted);
        Assert.Equal(0x84, cia.Read(0xDC0D));
    }

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
