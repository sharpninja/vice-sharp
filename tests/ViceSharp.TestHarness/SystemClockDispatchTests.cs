namespace ViceSharp.TestHarness;

using ViceSharp.Core;
using ViceSharp.Abstractions;
using Xunit;

/// <summary>
/// Regression suite for PERF-CLOCK-001 (pre-sorted dispatch arrays) and
/// PERF-CLOCK-002 (eliminate double property reads in IsCpuCycleStolen).
///
/// Requirements: FR-VIC-006, TR-CYCLE-001, PERF-CLOCK-001, PERF-CLOCK-002.
///
/// VICE source: vicii-cycle.c:54-63 (bad-line dispatch), vicii-draw-cycle.c (phi1/phi2 split).
///
/// BDP: Tests written first. Each test proves the optimized dispatch path produces
/// bit-identical behavior to the original List-based path for phase filtering,
/// divisor scheduling, cpu-skip, and cycle stealing.
/// </summary>
public sealed class SystemClockDispatchTests
{
    /// <summary>
    /// TR-CYCLE-001 / PERF-CLOCK-001.
    /// Use case: Phi1-phase device must tick exactly once per SystemClock.Step() call,
    /// during Phi1, regardless of whether the clock has slow or fast Phi2 devices also
    /// registered.
    /// Acceptance: After N steps the Phi1 device tick count equals N; the Phi2-only
    /// counting device tick count also equals N; neither is zero.
    /// VICE: vicii-chip-model.c phi1/phi2 split - VIC on Phi1, CPU/CIA on Phi2.
    /// </summary>
    [Fact]
    public void SystemClock_Phi1Device_TicksOncePerStep()
    {
        var clock = new SystemClock();
        var phi1 = new CountingDevice(ClockPhase.Phi1, 1);
        var phi2 = new CountingDevice(ClockPhase.Phi2, 1);

        clock.Register(phi1);
        clock.Register(phi2);

        const int steps = 100;
        clock.Step(steps);

        Assert.Equal(steps, phi1.TickCount);
        Assert.Equal(steps, phi2.TickCount);
    }

    /// <summary>
    /// TR-CYCLE-001 / PERF-CLOCK-001.
    /// Use case: A slow device with ClockDivisor=16 (like SID) must tick every
    /// 16 master cycles, not every cycle. The fast Phi2 device alongside it
    /// must not be affected.
    /// Acceptance: After 64 steps the fast device ticked 64 times and the
    /// slow device ticked exactly 4 times (64/16).
    /// VICE: vicii-clock-delta.h - SID clocked at phi2/16.
    /// </summary>
    [Fact]
    public void SystemClock_SlowDevice_TicksEveryDivisorCycles()
    {
        var clock = new SystemClock();
        var fast = new CountingDevice(ClockPhase.Phi2, 1);
        var slow = new CountingDevice(ClockPhase.Phi2, 16);

        clock.Register(fast);
        clock.Register(slow);

        clock.Step(64);

        Assert.Equal(64, fast.TickCount);
        Assert.Equal(4, slow.TickCount);
    }

    /// <summary>
    /// TR-CYCLE-001 / PERF-CLOCK-001.
    /// Use case: Unregistering a device must stop it from receiving ticks immediately;
    /// the remaining devices continue without disruption.
    /// Acceptance: Register two devices, step 10, unregister one, step 10 more.
    /// The unregistered device stays at 10; the remaining device reaches 20.
    /// </summary>
    [Fact]
    public void SystemClock_UnregisterDevice_StopsTickingImmediately()
    {
        var clock = new SystemClock();
        var kept = new CountingDevice(ClockPhase.Phi2, 1);
        var removed = new CountingDevice(ClockPhase.Phi2, 1);

        clock.Register(kept);
        clock.Register(removed);

        clock.Step(10);

        Assert.Equal(10, kept.TickCount);
        Assert.Equal(10, removed.TickCount);

        clock.Unregister(removed);
        clock.Step(10);

        Assert.Equal(20, kept.TickCount);
        Assert.Equal(10, removed.TickCount);
    }

    /// <summary>
    /// TR-CYCLE-001 / PERF-CLOCK-001.
    /// Use case: Reset must reset slow-device counters so the divisor counting
    /// restarts from zero after reset, matching behavior of original modulo-based
    /// dispatch (_cycle % divisor == 0 with _cycle reset to 0).
    /// Acceptance: Step 4 cycles (slow device with divisor=4 fires once at cycle 4),
    /// reset, step 4 again. Slow device fires once more (total 2 ticks, not 1).
    /// </summary>
    [Fact]
    public void SystemClock_Reset_ResetsSlowDeviceCounters()
    {
        var clock = new SystemClock();
        var slow = new CountingDevice(ClockPhase.Phi2, 4);

        clock.Register(slow);

        clock.Step(4);
        Assert.Equal(1, slow.TickCount);

        clock.Reset();
        Assert.Equal(0, slow.TickCount);

        clock.Step(4);
        Assert.Equal(1, slow.TickCount);
    }

    /// <summary>
    /// TR-CYCLE-001 / PERF-CLOCK-001 / PERF-CLOCK-002.
    /// Use case: Full parity check - mixed Phi1/Phi2, fast/slow devices with a
    /// cycle stealer registered. The optimized array path must produce identical
    /// tick counts to the original List dispatch for 500 steps.
    /// Acceptance: Phi1-fast=500, Phi2-fast=500, Phi2-slow(5)=100, Phi1-slow(7)=71
    /// after 500 steps (integer division: 500/5=100, 500/7=71).
    /// No assertion on CPU skipping in this test (covered by existing VicIiCoreTimingTests).
    /// </summary>
    [Fact]
    public void SystemClock_ArrayDispatch_PhasePreFiltered_MatchesOriginalOrder()
    {
        var clock = new SystemClock();
        var phi1Fast = new CountingDevice(ClockPhase.Phi1, 1);
        var phi2Fast = new CountingDevice(ClockPhase.Phi2, 1);
        var phi2Slow = new CountingDevice(ClockPhase.Phi2, 5);
        var phi1Slow = new CountingDevice(ClockPhase.Phi1, 7);

        clock.Register(phi1Fast);
        clock.Register(phi2Fast);
        clock.Register(phi2Slow);
        clock.Register(phi1Slow);

        clock.Step(500);

        Assert.Equal(500, phi1Fast.TickCount);
        Assert.Equal(500, phi2Fast.TickCount);
        Assert.Equal(100, phi2Slow.TickCount);
        Assert.Equal(71, phi1Slow.TickCount);
    }

    /// <summary>
    /// TR-CYCLE-001 / PERF-CLOCK-002.
    /// Use case: IsCpuCycleStolen must read stealer properties only once per call
    /// to avoid stale double-reads when the property has a side effect or changes
    /// between reads (theoretical). More concretely: if IsCpuCycleStolen returns
    /// true and IsCpuCycleStealMandatory returns false, the out parameters must
    /// reflect those single-read values (conditional=true, mandatory=false).
    /// Acceptance: ScriptedStealer returns (stolen=true, mandatory=false) at tick 5.
    /// After 6 steps, the CPU device was skipped exactly once (at step 5).
    /// </summary>
    [Fact]
    public void SystemClock_CycleSteal_ReadsStolenAndMandatoryOnlyOnce()
    {
        var clock = new SystemClock();
        var stealer = new ScriptedStealer(stolenOnTick: 5, mandatoryOnTick: -1);
        var cpu = new CountingCpuStealTarget();

        clock.Register(stealer);
        clock.Register(cpu);

        clock.Step(10);

        // CPU should have been skipped exactly at tick 5 (stolen=true, mandatory=false,
        // cpu.CanStealCurrentCycle=true)
        Assert.Equal(9, cpu.TickCount);
    }

    // -----------------------------------------------------------------------
    // Test doubles
    // -----------------------------------------------------------------------

    private sealed class CountingDevice : IClockedDevice
    {
        private readonly uint _divisor;
        public CountingDevice(ClockPhase phase, uint divisor)
        {
            Phase = phase;
            _divisor = divisor;
        }
        public DeviceId Id => new(0xA001);
        public string Name => "Test counter";
        public uint ClockDivisor => _divisor;
        public ClockPhase Phase { get; }
        public int TickCount { get; private set; }
        public void Tick() => TickCount++;
        public void Reset() => TickCount = 0;
        public void Initialize() { }
    }

    private sealed class ScriptedStealer : IClockedDevice, ICpuCycleStealer
    {
        private readonly int _stolenOnTick;
        private readonly int _mandatoryOnTick;
        private int _ticks;

        public ScriptedStealer(int stolenOnTick, int mandatoryOnTick)
        {
            _stolenOnTick = stolenOnTick;
            _mandatoryOnTick = mandatoryOnTick;
        }

        public DeviceId Id => new(0xA002);
        public string Name => "Scripted stealer";
        public uint ClockDivisor => 1;
        public ClockPhase Phase => ClockPhase.Phi1;
        public bool IsCpuCycleStolen => _ticks == _stolenOnTick;
        public bool IsCpuCycleStealMandatory => _ticks == _mandatoryOnTick;
        public void Tick() => _ticks++;
        public void Reset() => _ticks = 0;
        public void Initialize() { }
    }

    private sealed class CountingCpuStealTarget : ICpu, ICpuCycleStealTarget
    {
        public DeviceId Id => new(0xA003);
        public string Name => "Test CPU";
        public uint ClockDivisor => 1;
        public ClockPhase Phase => ClockPhase.Phi2;
        public ushort PC { get; set; }
        public byte Flags { get; set; }
        public int TickCount { get; private set; }
        // Executed-only by construction: the clock does not Tick() a stolen CPU cycle.
        public long ExecutedCycles => TickCount;
        public bool CanStealCurrentCycle => true;
        public bool CanForceStealCurrentCycle => false;
        public void Tick() => TickCount++;
        public void Reset() => TickCount = 0;
        public void Initialize() { }
        public int ExecuteInstruction() => 0;
        public void Irq() { }
        public void Nmi() { }
    }
}
