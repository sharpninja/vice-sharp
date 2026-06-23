namespace ViceSharp.TestHarness;

using ViceSharp.Chips.Cpu;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// TEST-CPUTICK-001 / FR-CPUTICK-001 / TR-CPU-TICK-001. Each CPU keeps its own
/// independent executed-cycle counter - the foundation for per-CPU speed metrics and
/// for machines with more than one CPU (the C128's 8502 + Z80, and a host CPU plus each
/// drive's CPU). A CPU only executes on the cycles it is clocked; VIC badline/sprite
/// cycle-steals halt the 6510, and the system clock simply does not Tick() the CPU on
/// those cycles, so a per-Tick counter is executed-only by construction.
/// </summary>
public sealed class CpuTickCounterTests
{
    private static Mos6502 NewNopCpu()
    {
        var bus = new BasicBus();
        bus.RegisterDevice(new SimpleRam());
        // NOP sled from $1000; reset vector -> $1000.
        for (ushort a = 0x1000; a < 0x1040; a++)
            bus.Write(a, 0xEA);
        bus.Write(0xFFFC, 0x00);
        bus.Write(0xFFFD, 0x10);
        var cpu = new Mos6502(bus);
        cpu.Reset();
        return cpu;
    }

    /// <summary>
    /// FR: FR-CPUTICK-001, TR: TR-CPU-TICK-001, TEST-CPUTICK-001 (AC1/AC4).
    /// Use case: a CPU must count the cycles it actually executes so its real-time rate
    ///   can be measured independently of any shared system clock or other CPU.
    /// Acceptance: ExecutedCycles starts at zero after reset, increments once per Tick(),
    ///   and a subsequent Reset() returns it to zero.
    /// </summary>
    [Fact]
    public void ExecutedCycles_IncrementsPerTick_AndResetZeroes()
    {
        var cpu = NewNopCpu();

        Assert.Equal(0, cpu.ExecutedCycles);

        for (var i = 0; i < 10; i++)
            cpu.Tick();
        Assert.Equal(10, cpu.ExecutedCycles);

        cpu.Reset();
        Assert.Equal(0, cpu.ExecutedCycles);
    }

    /// <summary>
    /// FR: FR-CPUTICK-001, TR: TR-CPU-TICK-001, TEST-CPUTICK-001 (AC1).
    /// Use case: two independent CPUs (e.g. host 6510 and a drive 6502) must each keep
    ///   their own counter so neither conflates with the other.
    /// Acceptance: ticking one CPU advances only its own ExecutedCycles; the second CPU's
    ///   counter is unaffected.
    /// </summary>
    [Fact]
    public void ExecutedCycles_AreIndependentPerCpuInstance()
    {
        var cpuA = NewNopCpu();
        var cpuB = NewNopCpu();

        for (var i = 0; i < 7; i++)
            cpuA.Tick();

        Assert.Equal(7, cpuA.ExecutedCycles);
        Assert.Equal(0, cpuB.ExecutedCycles);
    }
}
