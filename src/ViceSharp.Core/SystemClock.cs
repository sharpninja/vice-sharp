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

        if (cpuSkipped)
            return;

        // Check for pending interrupts after all devices have ticked.
        // This allows CIA/VIC to assert interrupt lines during their Tick().
        if (_cpu != null && _nmiPending && _cpu.IsInstructionBoundary)
        {
            _nmiPending = false;
            _cpu.Nmi();
        }
        else if (_cpu != null && _irqLine != null && _irqLine.IsAsserted && _cpu.IsInstructionBoundary)
        {
            _cpu.Irq();
        }
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
