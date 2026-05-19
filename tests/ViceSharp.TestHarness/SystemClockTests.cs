namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// Direct unit tests for <see cref="SystemClock"/>, the master clock
/// primitive that distributes per-cycle <see cref="IClockedDevice.Tick"/>
/// calls to all registered devices, honors per-device clock divisors
/// and phase splits (PHI1 / PHI2), tracks total cycles, and routes
/// CPU cycle steal requests from bus owners such as the VIC-II BA
/// line. These tests use small in-test fake devices that record their
/// tick history so the clock contract can be exercised without
/// pulling in CPU, VIC, CIA, or SID dependencies. They cover register
/// / unregister, single and multi-step advancement, divisor masking,
/// phase scheduling, reset semantics, the frequency property, and
/// the cycle-steal hand-off path.
/// </summary>
public sealed class SystemClockTests
{
    private sealed class RecordingDevice : IClockedDevice
    {
        private readonly List<long> _ticks = new();
        public RecordingDevice(uint divisor = 1, ClockPhase phase = ClockPhase.Phi1, uint id = 1)
        {
            ClockDivisor = divisor;
            Phase = phase;
            Id = new DeviceId(id);
            Name = $"Recording#{id}";
        }

        public IReadOnlyList<long> Ticks => _ticks;
        public int ResetCount { get; private set; }

        public DeviceId Id { get; }
        public string Name { get; }
        public uint ClockDivisor { get; }
        public ClockPhase Phase { get; }

        public void Tick() => _ticks.Add(_ticks.Count);
        public void Reset()
        {
            _ticks.Clear();
            ResetCount++;
        }
    }

    private sealed class PhaseRecorder : IClockedDevice
    {
        private readonly List<ClockPhase> _events;
        public PhaseRecorder(List<ClockPhase> sink, ClockPhase phase, uint id)
        {
            _events = sink;
            Phase = phase;
            Id = new DeviceId(id);
            Name = $"Phase#{id}";
        }

        public DeviceId Id { get; }
        public string Name { get; }
        public uint ClockDivisor => 1;
        public ClockPhase Phase { get; }

        public void Tick() => _events.Add(Phase);
        public void Reset() { }
    }

    private sealed class StealerDevice : IClockedDevice, ICpuCycleStealer
    {
        public DeviceId Id { get; } = new(99);
        public string Name => "Stealer";
        public uint ClockDivisor => 1;
        public ClockPhase Phase => ClockPhase.Phi1;
        public bool IsCpuCycleStolen { get; set; }
        public bool IsCpuCycleStealMandatory { get; set; }
        public int Ticks { get; private set; }

        public void Tick() => Ticks++;
        public void Reset() { Ticks = 0; IsCpuCycleStolen = false; IsCpuCycleStealMandatory = false; }
    }

    /// <summary>
    /// FR/TR: TR-Cycle-Accuracy (SystemClock primitive).
    /// Use case: A bare clock with no registered devices is stepped
    /// once - the smallest possible scenario for verifying that
    /// <see cref="SystemClock.Step()"/> deterministically advances
    /// the master cycle counter even when there are no tick consumers.
    /// Acceptance: <see cref="IClock.TotalCycles"/> increments by 1
    /// per call to <c>Step()</c>.
    /// </summary>
    [Fact]
    public void Step_NoDevices_AdvancesTotalCyclesByOne()
    {
        var clock = new SystemClock();
        Assert.Equal(0, clock.TotalCycles);

        clock.Step();

        Assert.Equal(1, clock.TotalCycles);
    }

    /// <summary>
    /// FR/TR: TR-Cycle-Accuracy (SystemClock primitive).
    /// Use case: The bulk advancement overload <c>Step(long)</c> is
    /// the path the runtime uses to crank the system for a frame's
    /// worth of cycles in one call. We need it to invoke <c>Step()</c>
    /// exactly N times so the counter and any device tick counts stay
    /// frame-accurate.
    /// Acceptance: After <c>Step(N)</c> on an empty clock,
    /// <see cref="IClock.TotalCycles"/> equals N.
    /// </summary>
    [Fact]
    public void StepBulk_AdvancesTotalCyclesByExactCount()
    {
        var clock = new SystemClock();

        clock.Step(1000);

        Assert.Equal(1000, clock.TotalCycles);
    }

    /// <summary>
    /// FR/TR: TR-Cycle-Accuracy (SystemClock primitive).
    /// Use case: The default parameterless constructor must produce
    /// a PAL C64 master clock at 985,248 Hz so that systems built via
    /// the default ctor (test harnesses, ad-hoc machines) match real
    /// hardware timing without explicit configuration. The frequency
    /// is exposed via <see cref="IClock.FrequencyHz"/>.
    /// Acceptance: A default-constructed SystemClock reports
    /// 985248 Hz; a frequency-arg ctor reports the requested value.
    /// </summary>
    [Fact]
    public void Frequency_DefaultIsPalC64_CustomIsHonored()
    {
        var defaultClock = new SystemClock();
        var customClock = new SystemClock(1_022_727);

        Assert.Equal(985_248, defaultClock.FrequencyHz);
        Assert.Equal(1_022_727, customClock.FrequencyHz);
    }

    /// <summary>
    /// FR/TR: TR-Cycle-Accuracy (SystemClock primitive).
    /// Use case: A single full-speed Phi1 device (the simplest CPU-
    /// like consumer) is registered and the clock is stepped several
    /// times - the most common runtime scenario where each master
    /// cycle drives exactly one device tick.
    /// Acceptance: After N steps the device's tick log has exactly N
    /// entries, matching <see cref="IClock.TotalCycles"/>.
    /// </summary>
    [Fact]
    public void Register_FullSpeedDevice_TicksOncePerStep()
    {
        var clock = new SystemClock();
        var device = new RecordingDevice(divisor: 1, phase: ClockPhase.Phi1, id: 1);
        clock.Register(device);

        clock.Step(5);

        Assert.Equal(5, device.Ticks.Count);
        Assert.Equal(5, clock.TotalCycles);
    }

    /// <summary>
    /// FR/TR: TR-Cycle-Accuracy (SystemClock primitive).
    /// Use case: Multiple devices are registered on the same phase -
    /// representative of a real C64 where CPU + VIC + CIA1 + CIA2 all
    /// live on the clock - and we need every one of them to receive
    /// each scheduled tick without one device starving another.
    /// Acceptance: Each registered device's tick log advances in
    /// lockstep with the master cycle counter.
    /// </summary>
    [Fact]
    public void MultipleDevices_AllReceiveTicksPerStep()
    {
        var clock = new SystemClock();
        var a = new RecordingDevice(divisor: 1, phase: ClockPhase.Phi1, id: 1);
        var b = new RecordingDevice(divisor: 1, phase: ClockPhase.Phi1, id: 2);
        var c = new RecordingDevice(divisor: 1, phase: ClockPhase.Phi1, id: 3);
        clock.Register(a);
        clock.Register(b);
        clock.Register(c);

        clock.Step(4);

        Assert.Equal(4, a.Ticks.Count);
        Assert.Equal(4, b.Ticks.Count);
        Assert.Equal(4, c.Ticks.Count);
    }

    /// <summary>
    /// FR/TR: TR-Cycle-Accuracy (SystemClock primitive).
    /// Use case: A slower device declares a divisor > 1, modelling
    /// the SID at half rate or audio downsamplers running every N
    /// master cycles. The clock divides ticks by modulo of the
    /// current cycle number against the divisor.
    /// Acceptance: With divisor=2 the device is ticked every other
    /// cycle, so 10 master cycles produce 5 ticks; with divisor=5,
    /// 10 master cycles produce 2 ticks.
    /// </summary>
    [Fact]
    public void ClockDivisor_GreaterThanOne_TicksAtReducedRate()
    {
        var clock = new SystemClock();
        var half = new RecordingDevice(divisor: 2, phase: ClockPhase.Phi1, id: 1);
        var fifth = new RecordingDevice(divisor: 5, phase: ClockPhase.Phi1, id: 2);
        clock.Register(half);
        clock.Register(fifth);

        clock.Step(10);

        Assert.Equal(5, half.Ticks.Count);
        Assert.Equal(2, fifth.Ticks.Count);
    }

    /// <summary>
    /// FR/TR: TR-Cycle-Accuracy (SystemClock primitive).
    /// Use case: The 6502 bus operates as two half-cycles - PHI1
    /// is the VIC fetch window, PHI2 is the CPU access window.
    /// Devices declare which phase they run on, and the clock must
    /// dispatch them in PHI1-then-PHI2 order each master cycle so
    /// the simulated bus owner ordering matches real hardware.
    /// Acceptance: A PHI1 device and a PHI2 device registered on
    /// the same clock are both ticked once per step, and the PHI1
    /// device's tick is observed before the PHI2 device's tick.
    /// </summary>
    [Fact]
    public void Phases_Phi1IsTickedBeforePhi2()
    {
        var clock = new SystemClock();
        var order = new List<ClockPhase>();
        var phi1 = new PhaseRecorder(order, ClockPhase.Phi1, id: 1);
        var phi2 = new PhaseRecorder(order, ClockPhase.Phi2, id: 2);
        clock.Register(phi1);
        clock.Register(phi2);

        clock.Step(3);

        Assert.Equal(6, order.Count);
        for (var i = 0; i < order.Count; i += 2)
        {
            Assert.Equal(ClockPhase.Phi1, order[i]);
            Assert.Equal(ClockPhase.Phi2, order[i + 1]);
        }
    }

    /// <summary>
    /// FR/TR: TR-Cycle-Accuracy (SystemClock primitive).
    /// Use case: A device is unregistered mid-run (representative of
    /// hot-swappable hardware such as cartridges or detached drives).
    /// After unregister the clock must no longer call its
    /// <see cref="IClockedDevice.Tick"/> method.
    /// Acceptance: Ticks accumulated before unregister are preserved
    /// on the device's log; no further ticks are appended after
    /// unregister even as the master clock advances.
    /// </summary>
    [Fact]
    public void Unregister_StopsFurtherTicks()
    {
        var clock = new SystemClock();
        var device = new RecordingDevice(divisor: 1, phase: ClockPhase.Phi1, id: 1);
        clock.Register(device);

        clock.Step(3);
        var ticksBefore = device.Ticks.Count;

        clock.Unregister(device);
        clock.Step(5);

        Assert.Equal(3, ticksBefore);
        Assert.Equal(3, device.Ticks.Count);
        Assert.Equal(8, clock.TotalCycles);
    }

    /// <summary>
    /// FR/TR: TR-Cycle-Accuracy (SystemClock primitive).
    /// Use case: A full system Reset (warm boot or test fixture
    /// teardown) clears the master cycle counter and propagates
    /// <see cref="IDevice.Reset"/> to every registered device. This
    /// is critical for deterministic test setup and for the user-
    /// facing Reset button in the host shell.
    /// Acceptance: After Reset, TotalCycles is zero and each
    /// registered device's Reset method has been invoked exactly
    /// once per Reset call.
    /// </summary>
    [Fact]
    public void Reset_ZeroesTotalCyclesAndCallsDeviceReset()
    {
        var clock = new SystemClock();
        var a = new RecordingDevice(divisor: 1, phase: ClockPhase.Phi1, id: 1);
        var b = new RecordingDevice(divisor: 1, phase: ClockPhase.Phi2, id: 2);
        clock.Register(a);
        clock.Register(b);
        clock.Step(7);

        Assert.Equal(7, clock.TotalCycles);
        Assert.Equal(7, a.Ticks.Count);

        clock.Reset();

        Assert.Equal(0, clock.TotalCycles);
        Assert.Equal(1, a.ResetCount);
        Assert.Equal(1, b.ResetCount);
        Assert.Empty(a.Ticks);
        Assert.Empty(b.Ticks);
    }

    /// <summary>
    /// FR/TR: TR-Cycle-Accuracy (SystemClock primitive).
    /// Use case: A bus owner (VIC-II BA line, REU DMA, etc.)
    /// implements <see cref="ICpuCycleStealer"/> and is registered
    /// with the clock. The clock collects cycle-steal state at the
    /// PHI1/PHI2 boundary so the CPU can be skipped on the matching
    /// PHI2. Even when no CPU is wired, the stealer itself remains
    /// a clocked device and must still tick.
    /// Acceptance: A non-CPU clock with a registered stealer ticks
    /// the stealer on every master cycle regardless of the steal
    /// flags, since CPU skip only suppresses CPU-classified
    /// participants - never the stealer itself.
    /// </summary>
    [Fact]
    public void CycleStealer_RegisteredDeviceTicksUnaffectedByStealFlags()
    {
        var clock = new SystemClock();
        var stealer = new StealerDevice();
        clock.Register(stealer);

        clock.Step(2);
        stealer.IsCpuCycleStolen = true;
        clock.Step(3);
        stealer.IsCpuCycleStealMandatory = true;
        clock.Step(2);

        Assert.Equal(7, stealer.Ticks);
        Assert.Equal(7, clock.TotalCycles);
    }
}
