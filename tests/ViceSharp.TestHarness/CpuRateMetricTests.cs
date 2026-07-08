namespace ViceSharp.TestHarness;

using System;
using NSubstitute;
using ViceSharp.Abstractions;
using ViceSharp.Host.Runtime;
using Xunit;

/// <summary>
/// TEST-CPUTICK-001 (AC2) / FR-CPUTICK-001 / TR-CPU-TICK-001. The store's AC2
/// defines the PER-CPU speed: delta-executed over delta-wall over the CPU's
/// own target clock (the C64 primary legitimately reads 95-100 percent while
/// BA-stalled). That reading lives in PerCpuRates; the HEADLINE
/// EffectiveClockHz remains emulated machine time (see PerCpuRateTests for
/// the multi-CPU display contract), so a perfectly paced session reads 100
/// percent regardless of BA stalls.
/// </summary>
public sealed class CpuRateMetricTests
{
    /// <summary>
    /// FR: FR-CPUTICK-001, TR: TR-CPU-TICK-001, TEST-CPUTICK-001 (AC2).
    /// Use case: the per-CPU speed reading must reflect how fast the primary CPU actually
    ///   executed (its own ExecutedCycles delta over its own clock), independent of the
    ///   system clock - while the headline stays machine time so pacing reads 100 percent.
    /// Acceptance: PerCpuRates reports the primary CPU's ExecutedCycles delta over the
    ///   elapsed wall time against its own clock, and the headline EffectiveClockHz
    ///   measures the machine clock.
    /// </summary>
    [Fact]
    public void PerCpuRate_MeasuresPrimaryCpuExecutedCycles_HeadlineStaysMachineTime()
    {
        long executed = 0;
        long machineCycle = 0;
        var cpu = Substitute.For<ICpu>();
        cpu.ExecutedCycles.Returns(_ => executed);

        var machine = Substitute.For<IMachine>();
        machine.PrimaryCpu.Returns(cpu);
        machine.Architecture.Returns(Substitute.For<IArchitectureDescriptor>());
        machine.Clock.Returns(Substitute.For<IClock>());
        machine.Clock.FrequencyHz.Returns(985_248);
        machine.GetState().Returns(_ => new MachineState { Cycle = machineCycle });
        machine.CpuInfos.Returns(_ => new[] { new CpuInfo("Commodore 64", executed, 985_248) });

        var architecture = Substitute.For<IArchitectureDescriptor>();
        var session = new EmulatorRuntimeSession("rate-test", architecture, machine);

        var t0 = DateTimeOffset.UnixEpoch;
        session.ResetPerformanceCounters(t0);                  // anchor both counters at 0
        // The system clock ran a full emulated second while the BA-stalled
        // primary executed only 500k of those cycles.
        machineCycle = 985_248;
        executed = 500_000;
        session.UpdatePerformanceCounters(t0.AddSeconds(1.0)); // ...over one wall second

        Assert.Equal(985_248d, session.EffectiveClockHz);

        var primary = Assert.Single(session.PerCpuRates);
        Assert.Equal(500_000d, primary.EffectiveClockHz);
        Assert.Equal(500_000d / 985_248d * 100.0, primary.EffectiveClockPercent, precision: 3);
    }
}
