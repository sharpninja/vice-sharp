using ViceSharp.Abstractions;
using ViceSharp.Chips.Cpu;

namespace ViceSharp.Core;

public sealed class SystemClock : IClock
{
    private readonly List<IClockedDevice> _devices = new();
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

        foreach (var device in _devices)
        {
            if (_cycle % device.ClockDivisor == 0)
            {
                device.Tick();
            }
        }
        
        UpdateNmiEdgeLatch();

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

    public void Step(long cycles)
    {
        for (long i = 0; i < cycles; i++)
        {
            Step();
        }
    }

    public void Register(IClockedDevice device)
    {
        _devices.Add(device);
    }

    public void Unregister(IClockedDevice device)
    {
        _devices.Remove(device);
    }

    public void Reset()
    {
        _cycle = 0;
        foreach (var device in _devices)
        {
            device.Reset();
        }
        if (_irqLine is InterruptLine irq)
            irq.Clear();
        if (_nmiLine is InterruptLine nmi)
            nmi.Clear();
        _nmiWasAsserted = false;
        _nmiPending = false;
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
