using BenchmarkDotNet.Attributes;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Cia;
using ViceSharp.Core;

namespace ViceSharp.Benchmarks;

/// <summary>
/// Per-cycle benchmark for the MOS 6526 CIA chip.
/// Configures Timer A in continuous mode so the hot path traverses both
/// the countdown and underflow handling once per cycle window.
/// </summary>
[MemoryDiagnoser]
public class CiaBenchmarks
{
    private Mos6526 _cia = null!;
    private BasicBus _bus = null!;
    private InterruptLine _irq = null!;

    [Params(BenchmarkMachineFactory.DefaultCycleBudget)]
    public int Cycles { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _bus = new BasicBus();
        _irq = new InterruptLine(InterruptType.Irq);
        _cia = new Mos6526(_bus, _irq) { BaseAddress = 0xDC00 };
        _bus.RegisterDevice(_cia);
        _cia.Reset();

        // Timer A latch = $00FF (short, so underflow fires often), continuous mode.
        _cia.Write(0xDC04, 0xFF);
        _cia.Write(0xDC05, 0x00);
        _cia.Write(0xDC0D, 0x81); // unmask Timer A IRQ
        _cia.Write(0xDC0E, 0x11); // start | load
    }

    [Benchmark(Description = "Mos6526.Tick() x N")]
    public void TickLoop()
    {
        var cia = _cia;
        var cycles = Cycles;
        for (var i = 0; i < cycles; i++)
        {
            cia.Tick();
        }
    }
}
