namespace ViceSharp.TestHarness;

using System;
using System.Collections.Generic;
using NSubstitute;
using ViceSharp.Abstractions;
using ViceSharp.Host.Runtime;
using Xunit;

/// <summary>
/// TEST-CPUTICK-001 (AC3 display) / FR-CPUTICK-001. The session measures a rate for EVERY CPU in
/// the machine's roster - host and each peripheral - on its OWN clock, so the status surface can
/// list each CPU's speed distinctly (and the C128's two CPUs later), not just the headline
/// primary. Each reading is that CPU's executed-cycle delta over wall time and its percent of
/// its own target clock.
/// </summary>
public sealed class PerCpuRateTests
{
    /// <summary>
    /// FR: FR-CPUTICK-001, TR: TR-CPU-TICK-001, TEST-CPUTICK-001 (AC3 display).
    /// Use case: a host + drive rig must report each CPU's speed separately on the status bar so
    ///   the user sees the C64 and the 1541 running at their own rates.
    /// Acceptance: after one wall second, PerCpuRates lists the host then the drive, each with
    ///   its own executed-cycle Hz and percent-of-its-own-clock.
    /// </summary>
    [Fact]
    public void PerCpuRates_MeasureEachCpuOnItsOwnClock()
    {
        var machine = new FakeMultiCpuMachine();
        var architecture = Substitute.For<IArchitectureDescriptor>();
        var session = new EmulatorRuntimeSession("percpu-test", architecture, machine);

        var t0 = DateTimeOffset.UnixEpoch;
        session.ResetPerformanceCounters(t0);          // anchor both CPUs at 0

        machine.HostExecuted = 500_000;                // host ran 500k...
        machine.DriveExecuted = 400_000;               // ...drive ran 400k over one wall second
        session.UpdatePerformanceCounters(t0.AddSeconds(1.0));

        Assert.Equal(2, session.PerCpuRates.Count);

        var host = session.PerCpuRates[0];
        Assert.Equal("Commodore 64", host.Label);
        Assert.Equal(500_000d, host.EffectiveClockHz);
        Assert.Equal(500_000d / 985_248d * 100.0, host.EffectiveClockPercent, precision: 3);

        var drive = session.PerCpuRates[1];
        Assert.Equal("C1541", drive.Label);
        Assert.Equal(400_000d, drive.EffectiveClockHz);
        Assert.Equal(40.0, drive.EffectiveClockPercent, precision: 3);
    }

    private sealed class FakeMultiCpuMachine : IMachine
    {
        private readonly ICpu _primary;

        public FakeMultiCpuMachine()
        {
            var cpu = Substitute.For<ICpu>();
            cpu.ExecutedCycles.Returns(_ => HostExecuted);
            _primary = cpu;
        }

        public long HostExecuted { get; set; }
        public long DriveExecuted { get; set; }

        // Explicit roster (host + drive) the session measures per-CPU rates from.
        public IReadOnlyList<CpuInfo> CpuInfos => new[]
        {
            new CpuInfo("Commodore 64", HostExecuted, 985_248),
            new CpuInfo("C1541", DriveExecuted, 1_000_000),
        };

        public ICpu? PrimaryCpu => _primary;
        public IBus Bus => null!;
        public IClock Clock => null!;
        public IDeviceRegistry Devices => null!;
        public IArchitectureDescriptor Architecture => null!;
        public void RunFrame() { }
        public void StepInstruction() { }
        public MachineState GetState() => default;
        public void Reset() { }
    }
}
