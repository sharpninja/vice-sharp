namespace ViceSharp.TestHarness;

using NSubstitute;
using ViceSharp.Abstractions;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// TEST-CPUTICK-001 (AC3) / FR-CPUTICK-001. The machine enumerates a per-CPU roster - one
/// entry per CPU with its label, own ExecutedCycles, and clock - so the status surface can
/// list the host and each peripheral CPU distinctly (and the C128's two CPUs later). A plain
/// single-CPU machine reports one entry; a coordinator rig reports host + each drive.
/// </summary>
public sealed class CpuRosterTests
{
    /// <summary>
    /// FR: FR-CPUTICK-001, TR: TR-CPU-TICK-001, TEST-CPUTICK-001 (AC3).
    /// Use case: a single-CPU machine (a bare C64) still presents a one-entry roster so the
    ///   status surface has a uniform per-CPU list to render.
    /// Acceptance: CpuInfos has exactly one entry carrying the CPU's ExecutedCycles and clock.
    /// </summary>
    [Fact]
    public void SingleCpuMachine_RostersOneEntry()
    {
        IMachine host = new TestRosterMachine("Commodore 64 PAL", executed: 1234, clockHz: 985248);

        var roster = host.CpuInfos;

        Assert.Single(roster);
        Assert.Equal(1234, roster[0].ExecutedCycles);
        Assert.Equal(985248, roster[0].ClockHz);
    }

    /// <summary>
    /// FR: FR-CPUTICK-001, TR: TR-CPU-TICK-001, TEST-CPUTICK-001 (AC3).
    /// Use case: a coordinator rig (C64 host + a 1541 drive) must list the host and each
    ///   peripheral CPU distinctly so each appears separately on the status surface.
    /// Acceptance: the rig's CpuInfos lists the host first then each peripheral system's CPU,
    ///   each with its own ExecutedCycles and clock, as distinct entries.
    /// </summary>
    [Fact]
    public void CoordinatorRig_RostersHostThenEachPeripheralDistinctly()
    {
        var host = new TestRosterMachine("Commodore 64 PAL", executed: 5000, clockHz: 985248);
        var drive = new TestRosterMachine("C1541", executed: 700, clockHz: 1000000);

        var coordinator = new SystemCoordinator();
        coordinator.AttachSystem(host);
        coordinator.AttachSystem(drive);
        var rig = new CoordinatorMachine(host, coordinator, hostCyclesPerFrame: 19656);

        var roster = rig.CpuInfos;

        Assert.Equal(2, roster.Count);
        Assert.Equal("Commodore 64 PAL", roster[0].Label);
        Assert.Equal(5000, roster[0].ExecutedCycles);
        Assert.Equal("C1541", roster[1].Label);
        Assert.Equal(700, roster[1].ExecutedCycles);
        Assert.Equal(1000000, roster[1].ClockHz);
    }

    // Minimal real IMachine so the default IMachine.CpuInfos implementation runs (a mock
    // would bypass the default member). Only the members the roster reads are wired.
    private sealed class TestRosterMachine : IMachine
    {
        private readonly ICpu _cpu;
        private readonly IClock _clock;
        private readonly IArchitectureDescriptor _arch;

        public TestRosterMachine(string name, long executed, long clockHz)
        {
            var cpu = Substitute.For<ICpu>();
            cpu.ExecutedCycles.Returns(executed);
            _cpu = cpu;

            var clock = Substitute.For<IClock>();
            clock.FrequencyHz.Returns(clockHz);
            _clock = clock;

            var arch = Substitute.For<IArchitectureDescriptor>();
            arch.MachineName.Returns(name);
            _arch = arch;
        }

        public IBus Bus => null!;
        public IClock Clock => _clock;
        public IDeviceRegistry Devices => null!;
        public IArchitectureDescriptor Architecture => _arch;
        public ICpu? PrimaryCpu => _cpu;
        public void RunFrame() { }
        public void StepInstruction() { }
        public MachineState GetState() => default;
        public void Reset() { }
    }
}
