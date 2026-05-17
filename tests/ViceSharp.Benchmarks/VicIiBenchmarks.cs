using BenchmarkDotNet.Attributes;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;

namespace ViceSharp.Benchmarks;

/// <summary>
/// Per-frame benchmark for the MOS 6569 VIC-II video chip.
/// Drives one full PAL frame worth of Tick() calls in isolation. The
/// CPU is not present so the chip stays in its idle/border render path.
/// </summary>
[MemoryDiagnoser]
public class VicIiBenchmarks
{
    private Mos6569 _vic = null!;
    private BasicBus _bus = null!;
    private InterruptLine _irq = null!;

    [Params(BenchmarkMachineFactory.PalFrameCycles)]
    public int Cycles { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _bus = new BasicBus();

        // Provide a backing RAM so any matrix/colour fetches see deterministic data.
        var ram = new byte[0x10000];
        Array.Fill(ram, (byte)0x20); // spaces by default
        _bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, ram));

        _irq = new InterruptLine(ViceSharp.Abstractions.InterruptType.Irq);
        _vic = new Mos6569(_bus, _irq);
        _bus.RegisterDevice(_vic);
        _vic.Reset();
    }

    [Benchmark(Description = "Mos6569.Tick() one PAL frame")]
    public void TickFrame()
    {
        var vic = _vic;
        var cycles = Cycles;
        for (var i = 0; i < cycles; i++)
        {
            vic.Tick();
        }
    }
}
