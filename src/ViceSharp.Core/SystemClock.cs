using ViceSharp.Abstractions;
using ViceSharp.Chips.Cpu;

namespace ViceSharp.Core;

public sealed class SystemClock : IClock
{
    // PERF-CLOCK-001: pre-sorted dispatch arrays eliminate per-cycle Phase/ClockDivisor
    // virtual property reads and long modulo from the hot TickPhase path.
    // FR-VIC-006, TR-CYCLE-001: phi1/phi2 split mirrors VICE vicii-cycle.c dispatch order.
    private struct SlowDeviceEntry
    {
        public IClockedDevice Device;
        public uint Divisor;
        public uint Counter;
    }

    private readonly List<IClockedDevice> _devices = new();
    private readonly List<ICpuCycleStealer> _cycleStealers = new();
    private IClockedDevice[] _phi1FastDevices = [];
    private IClockedDevice[] _phi2FastDevices = [];
    private SlowDeviceEntry[] _phi1SlowDevices = [];
    private SlowDeviceEntry[] _phi2SlowDevices = [];

    private readonly Mos6502? _cpu;
    private readonly IInterruptLine? _irqLine;
    private readonly IInterruptLine? _nmiLine;
    private long _cycle;
    private bool _nmiWasAsserted;
    private bool _nmiPending;
    /// <summary>
    /// VICE <c>INTERRUPT_DELAY</c> (interrupt.h): x64sc
    /// <c>interrupt_check_irq_delay</c> (mainc64cpu.c) dispatches when
    /// <c>irq_delay_cycles &gt;= INTERRUPT_DELAY</c> (+1 when last opcode
    /// DELAYS_INTERRUPT). <c>interrupt_delay</c> increments that counter on
    /// CLK_INC while <c>irq_clk &lt;= maincpu_clk</c>.
    /// </summary>
    private const int InterruptDelayCycles = 2;
    /// <summary>Cycle when IRQ line last rose; <see cref="long.MaxValue"/> when clear.</summary>
    private long _irqAssertCycle = long.MaxValue;
    /// <summary>
    /// VICE <c>irq_delay_cycles</c>: incremented on CPU <c>CLK_INC</c> while
    /// <c>irq_clk &lt;= maincpu_clk</c>. Not incremented on extra JUMP/RTS
    /// host ticks (no VICE CLK) or on the latch tick itself (Wolf64 2109675).
    /// </summary>
    private int _irqDelayCycles;

    /// <summary>
    /// Host cycle of the last nirq 0 to 1 (VICE <c>irq_clk</c>).
    /// <see cref="long.MaxValue"/> when the IRQ line is clear.
    /// </summary>
    public long DebugIrqAssertCycle => _irqAssertCycle;

    /// <summary>VICE <c>irq_delay_cycles</c> (CLK_INC count since nirq 0 to 1).</summary>
    public int DebugIrqDelayCycles => _irqDelayCycles;

    /// <summary>
    /// Last IRQ sample taken while the CPU was at cycle 0 (FETCH or last CLK).
    /// Post-tick samples after FETCH (cycle &gt; 0) do not overwrite this.
    /// </summary>
    public string DebugFetchIrqNote { get; private set; } = "";

    public long TotalCycles => _cycle;
    public long FrequencyHz { get; }

    /// <summary>
    /// Creates a new SystemClock with default C64 PAL frequency (985248 Hz).
    /// </summary>
    public SystemClock() : this(985248)
    {
    }

    /// <summary>
    /// Creates a new SystemClock with the specified frequency.
    /// </summary>
    public SystemClock(long frequencyHz)
    {
        FrequencyHz = frequencyHz;
    }

    /// <summary>
    /// Creates a new SystemClock with CPU and IRQ line for interrupt handling.
    /// </summary>
    public SystemClock(long frequencyHz, Mos6502 cpu, IInterruptLine irqLine)
        : this(frequencyHz, cpu, irqLine, null)
    {
    }

    /// <summary>
    /// Creates a new SystemClock with CPU, IRQ line, and NMI line for interrupt handling.
    /// </summary>
    public SystemClock(long frequencyHz, Mos6502 cpu, IInterruptLine irqLine, IInterruptLine? nmiLine)
    {
        FrequencyHz = frequencyHz;
        _cpu = cpu;
        _irqLine = irqLine;
        _nmiLine = nmiLine;
        // Soft-deferred imm commits on the same host tick as the next FETCH;
        // VICE DO_INTERRUPT runs before that FETCH. CPU invokes this after the
        // soft commit and before reading the next opcode (NTSC c=520829).
        // Phi2 order is VIA then CPU (IRQ line updated); timer CPU reads use
        // Via6522 pre-Tick bus-visible counters (VICE LOAD before CLK_INC).
        cpu.TrySampleInterruptBeforeFetch = TryDispatchInterrupts;
    }

    public void Step()
    {
        _cycle++;

        TickPhase(ClockPhase.Phi1, skipCpu: false);
        // PERF-CLOCK-002: read IsCpuCycleStolen and IsCpuCycleStealMandatory into locals
        // once to eliminate double property reads from the hot path.
        var cpuCycleStolen = IsCpuCycleStolen(out var cpuCycleStealConditional, out var cpuCycleStealMandatory);
        var cpuSkipped = TickPhase(
            ClockPhase.Phi2,
            skipCpu: cpuCycleStolen,
            conditionalCpuSkip: cpuCycleStealConditional,
            mandatoryCpuSkip: cpuCycleStealMandatory);

        UpdateNmiEdgeLatch();
        UpdateIrqAssertClock();

        // VICE interrupt_delay increments on CLK_INC, including stolen
        // clocks (mainc64cpu.c steal_cycles / CLK still advances). Extra
        // JUMP/RTS host ticks are not CLK_INC (ConsumedViceClockThisTick).
        var viceClock = cpuSkipped
            || (_cpu is not null && _cpu.ConsumedViceClockThisTick);
        if (_irqLine is not null
            && _irqLine.IsAsserted
            && _irqAssertCycle != long.MaxValue)
        {
            if (viceClock && _irqAssertCycle < _cycle)
            {
                var clocks = 1;
                if (!cpuSkipped && _cpu is not null && _cpu.ViceClocksThisTick > 1)
                    clocks = _cpu.ViceClocksThisTick;
                _irqDelayCycles += clocks;
            }
            else if (!cpuSkipped
                && _cpu is not null
                && !_cpu.ConsumedViceClockThisTick
                && _irqAssertCycle == _cycle - 1)
            {
                // Previous tick was the IRQ latch on a real STORE CLK
                // (VICE irq_clk <= clk increments on that CLK). Managed
                // skipped the latch tick (assert == cycle). Count it on
                // the following apply host tick, which is not a CLK_INC
                // of its own (Wolf64 2865090 STA abs write at 2865084).
                // Latch on the apply tick itself (2175366) has
                // assert == cycle, so this does not fire.
                _irqDelayCycles++;
            }
        }

        if (cpuSkipped)
            return;

        // Check for pending interrupts after all devices have ticked.
        // Soft-deferred imm may also sample mid-CPU-tick via TrySampleInterruptBeforeFetch.
        TryDispatchInterrupts();
    }

    private void TryDispatchInterrupts()
    {
        if (_cpu is null)
            return;

        // Pre-FETCH samples run inside the CPU tick, after CIA Phi2 but before
        // the post-Phi2 UpdateIrqAssertClock. Latch the rising edge here so a
        // fresh irq_clk is not treated as already elapsed (Wolf64 2191785:
        // native STA zp nPC=$E5D1 nIrqClk=2191788 vs managed IRQ push).
        UpdateIrqAssertClock();

        // VICE interrupt_check_irq_delay: fire when cpu_clk >= irq_clk +
        // INTERRUPT_DELAY (2), plus one when last opcode DELAYS_INTERRUPT.
        if (_nmiPending && _cpu.IsInstructionBoundary)
        {
            _nmiPending = false;
            _cpu.Nmi();
            return;
        }

        if (_irqLine != null
            && _irqLine.IsAsserted
            && _cpu.IsInstructionBoundary
            && IsIrqDelayElapsed())
        {
            // VICE interrupt_check_irq_delay: OPINFO_ENABLES_IRQ (CLI) must
            // not dispatch; it sets IK_IRQPEND so the next instruction runs.
            // OPCODE_DELAYS_INTERRUPT is not skip-all: IsIrqDelayElapsed already
            // adds +1 to the threshold (need 3 instead of 2). Skipping here
            // blocked the KERNAL CIA IRQ after taken BEQ (Wolf64 2208208).
            if (_cpu.LastOpcodeEnablesIrq)
            {
                NoteFetchIrqSample("enables");
                return;
            }
            _cpu.Irq();
            NoteFetchIrqSample("irq");
            return;
        }

        NoteFetchIrqSample("skip");
    }

    private void NoteFetchIrqSample(string result)
    {
        if (_cpu is null || _cpu.DebugCycle != 0)
            return;
        var need = InterruptDelayCycles;
        if (_cpu.LastOpcodeDelaysInterrupt)
            need++;
        DebugFetchIrqNote =
            $"{result} bound={_cpu.IsInstructionBoundary} delay={_irqDelayCycles}/{need} line={_irqLine?.IsAsserted} delays={_cpu.LastOpcodeDelaysInterrupt} en={_cpu.LastOpcodeEnablesIrq} seq={_cpu.DebugInterruptSequenceRemaining} supp={_cpu.DebugSuppressBootstrapBoundary}";
    }

    private bool UpdateIrqAssertClock()
    {
        if (_irqLine is null)
            return false;

        // VICE interrupt_set_irq: irq_clk latches only on nirq 0 to 1.
        // Polling IsAsserted misses a same-cycle Release then Assert
        // (Wolf64 2208208 nIrqClk=2208210 vs stale elapsed pre-FETCH IRQ).
        if (_irqLine is InterruptLine line && line.ConsumeRisingEdge())
        {
            _irqAssertCycle = _cycle;
            _irqDelayCycles = 0;
            return true;
        }

        if (_irqLine.IsAsserted)
        {
            if (_irqAssertCycle == long.MaxValue)
            {
                _irqAssertCycle = _cycle;
                return true;
            }
        }
        else
        {
            _irqAssertCycle = long.MaxValue;
        }

        return false;
    }

    private bool IsIrqDelayElapsed()
    {
        if (_irqAssertCycle == long.MaxValue || _cpu is null)
            return false;

        // VICE interrupt_check_irq_delay: irq_delay_cycles >= INTERRUPT_DELAY
        // (2), plus one when last opcode DELAYS_INTERRUPT. Do not use
        // wall-clock _cycle: extra JUMP host ticks are not CLK_INC
        // (Wolf64 2175366). Do not increment every Step (regressed 2109675).
        var need = InterruptDelayCycles;
        if (_cpu.LastOpcodeDelaysInterrupt)
            need++;
        return _irqDelayCycles >= need;
    }

    // PERF-CLOCK-001: iterate pre-sorted phase arrays directly instead of the
    // full _devices list with per-entry Phase/ClockDivisor checks.
    private bool TickPhase(
        ClockPhase phase,
        bool skipCpu,
        bool conditionalCpuSkip = false,
        bool mandatoryCpuSkip = false)
    {
        var cpuSkipped = false;
        var fastDevices = phase == ClockPhase.Phi1 ? _phi1FastDevices : _phi2FastDevices;
        var slowDevices = phase == ClockPhase.Phi1 ? _phi1SlowDevices : _phi2SlowDevices;

        foreach (var device in fastDevices)
        {
            if (skipCpu && CanSkipCpu(device, conditionalCpuSkip, mandatoryCpuSkip))
            {
                if (device is ICpu)
                    cpuSkipped = true;
                if (device is ICpuCycleStealTarget stealTarget
                    && (stealTarget.NotifyOnStolenCycle
                        || (mandatoryCpuSkip && stealTarget.CanForceStealCurrentCycle)))
                    stealTarget.OnStolenCycle();
                continue;
            }
            device.Tick();
        }

        for (int i = 0; i < slowDevices.Length; i++)
        {
            ref var entry = ref slowDevices[i];
            entry.Counter++;
            if (entry.Counter < entry.Divisor)
                continue;
            entry.Counter = 0;

            if (skipCpu && CanSkipCpu(entry.Device, conditionalCpuSkip, mandatoryCpuSkip))
            {
                if (entry.Device is ICpu)
                    cpuSkipped = true;
                if (entry.Device is ICpuCycleStealTarget stealTarget
                    && (stealTarget.NotifyOnStolenCycle
                        || (mandatoryCpuSkip && stealTarget.CanForceStealCurrentCycle)))
                    stealTarget.OnStolenCycle();
                continue;
            }
            entry.Device.Tick();
        }

        return cpuSkipped;
    }

    private static bool CanSkipCpu(IClockedDevice device, bool conditionalCpuSkip, bool mandatoryCpuSkip)
    {
        return device is ICpu &&
            (device is ICpuCycleStealTarget target
                ? (conditionalCpuSkip && target.CanStealCurrentCycle) ||
                    (mandatoryCpuSkip && target.CanForceStealCurrentCycle)
                : conditionalCpuSkip || mandatoryCpuSkip);
    }

    // PERF-CLOCK-002: read each property once into locals before the compound
    // assignments to eliminate the double property reads in the original path.
    private bool IsCpuCycleStolen(out bool conditional, out bool mandatory)
    {
        conditional = false;
        mandatory = false;
        foreach (var stealer in _cycleStealers)
        {
            bool stolen = stealer.IsCpuCycleStolen;
            bool mand = stealer.IsCpuCycleStealMandatory;
            if (stolen || mand)
            {
                conditional |= stolen;
                mandatory |= mand;
                return true;
            }
        }

        return false;
    }

    public void Step(long cycles)
    {
        for (long i = 0; i < cycles; i++)
            Step();
    }

    public void Register(IClockedDevice device)
    {
        _devices.Add(device);
        if (device is ICpuCycleStealer cycleStealer)
            _cycleStealers.Add(cycleStealer);
        RebuildDispatchArrays();
    }

    public void Unregister(IClockedDevice device)
    {
        _devices.Remove(device);
        if (device is ICpuCycleStealer cycleStealer)
            _cycleStealers.Remove(cycleStealer);
        RebuildDispatchArrays();
    }

    public void Reset()
    {
        _cycle = 0;
        foreach (var device in _devices)
            device.Reset();

        // Reset slow-device counters so divisor counting restarts from zero,
        // matching original behavior where _cycle % divisor used _cycle=0.
        for (int i = 0; i < _phi1SlowDevices.Length; i++)
            _phi1SlowDevices[i].Counter = 0;
        for (int i = 0; i < _phi2SlowDevices.Length; i++)
            _phi2SlowDevices[i].Counter = 0;

        if (_irqLine is InterruptLine irq)
            irq.Clear();
        if (_nmiLine is InterruptLine nmi)
            nmi.Clear();
        _nmiWasAsserted = false;
        _nmiPending = false;
        _irqAssertCycle = long.MaxValue;
        _irqDelayCycles = 0;
    }

    // PERF-CLOCK-001: rebuild dispatch arrays whenever the device set changes.
    // Called only on Register/Unregister (not on the hot Step() path).
    private void RebuildDispatchArrays()
    {
        var phi1Fast = new List<IClockedDevice>();
        var phi2Fast = new List<IClockedDevice>();
        var phi1Slow = new List<SlowDeviceEntry>();
        var phi2Slow = new List<SlowDeviceEntry>();

        foreach (var device in _devices)
        {
            bool isPhi1 = device.Phase == ClockPhase.Phi1;
            if (device.ClockDivisor == 1)
            {
                (isPhi1 ? phi1Fast : phi2Fast).Add(device);
            }
            else
            {
                var entry = new SlowDeviceEntry { Device = device, Divisor = device.ClockDivisor, Counter = 0 };
                (isPhi1 ? phi1Slow : phi2Slow).Add(entry);
            }
        }

        _phi1FastDevices = phi1Fast.ToArray();
        _phi2FastDevices = phi2Fast.ToArray();
        _phi1SlowDevices = phi1Slow.ToArray();
        _phi2SlowDevices = phi2Slow.ToArray();
    }

    private void UpdateNmiEdgeLatch()
    {
        if (_nmiLine is null)
            return;

        var isAsserted = _nmiLine.IsAsserted;
        if (isAsserted && !_nmiWasAsserted)
            _nmiPending = true;

        _nmiWasAsserted = isAsserted;
    }
}
