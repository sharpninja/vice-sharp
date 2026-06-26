namespace ViceSharp.TestHarness;

using System;
using NSubstitute;
using ViceSharp.Abstractions;
using ViceSharp.Host.Runtime;
using Xunit;

/// <summary>
/// TEST-CPUTICK-001 (AC2) / FR-CPUTICK-001 / TR-CPU-TICK-001. The headline emulation rate is
/// the PRIMARY CPU's own executed-cycle rate, not the system/bus clock - so a multi-CPU rig
/// (a coordinator's host plus each drive, or the C128's 8502 + Z80) measures the right CPU
/// and is not conflated by peripheral cycles.
/// </summary>
public sealed class CpuRateMetricTests
{
    /// <summary>
    /// FR: FR-CPUTICK-001, TR: TR-CPU-TICK-001, TEST-CPUTICK-001 (AC2).
    /// Use case: the status speed reading must reflect how fast the primary CPU is actually
    ///   executing, independently of the system clock (which on a coordinator rig also
    ///   advances peripheral cycles), so the displayed rate is not miscalculated.
    /// Acceptance: EffectiveClockHz equals the primary CPU's ExecutedCycles delta over the
    ///   elapsed wall time, and ignores the system clock's Cycle.
    /// </summary>
    [Fact]
    public void EffectiveClockHz_MeasuresPrimaryCpuExecutedCycles_NotSystemClock()
    {
        long executed = 0;
        var cpu = Substitute.For<ICpu>();
        cpu.ExecutedCycles.Returns(_ => executed);

        var machine = Substitute.For<IMachine>();
        machine.PrimaryCpu.Returns(cpu);
        // The system clock is deliberately far ahead; the per-CPU metric must IGNORE it.
        machine.GetState().Returns(new MachineState { Cycle = 9_000_000 });

        var architecture = Substitute.For<IArchitectureDescriptor>();
        var session = new EmulatorRuntimeSession("rate-test", architecture, machine);

        var t0 = DateTimeOffset.UnixEpoch;
        session.ResetPerformanceCounters(t0);                  // anchor at ExecutedCycles = 0
        executed = 500_000;                                    // CPU executed 500k cycles...
        session.UpdatePerformanceCounters(t0.AddSeconds(1.0)); // ...over one wall second

        Assert.Equal(500_000d, session.EffectiveClockHz);
    }
}
