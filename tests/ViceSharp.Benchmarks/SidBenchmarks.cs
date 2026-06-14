using BenchmarkDotNet.Attributes;
using ViceSharp.Chips.Audio;
using ViceSharp.Core;

namespace ViceSharp.Benchmarks;

/// <summary>
/// Per-cycle benchmark for the MOS 6581 SID audio chip.
/// Programs voice 1 with a sawtooth waveform plus a short ADSR envelope
/// and ticks through a deterministic workload to exercise the oscillator
/// and envelope generator hot paths.
/// </summary>
[MemoryDiagnoser]
public class SidBenchmarks
{
    private Sid6581 _sid = null!;
    private BasicBus _bus = null!;

    [Params(BenchmarkMachineFactory.DefaultCycleBudget)]
    public int Cycles { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _bus = new BasicBus();
        _sid = new Sid6581(_bus) { BaseAddress = 0xD400 };
        _bus.RegisterDevice(_sid);
        _sid.Reset();

        // Voice 1: frequency $1000, sawtooth waveform, gate on
        _sid.Write(0xD400, 0x00);
        _sid.Write(0xD401, 0x10);
        _sid.Write(0xD405, 0x09); // attack=0, decay=9
        _sid.Write(0xD406, 0xF0); // sustain=15, release=0
        _sid.Write(0xD404, 0x21); // sawtooth + gate

        // Master volume to 15 so the mixer path is active.
        _sid.Write(0xD418, 0x0F);
    }

    [Benchmark(Description = "Sid6581.Tick() x N")]
    public void TickLoop()
    {
        var sid = _sid;
        var cycles = Cycles;
        for (var i = 0; i < cycles; i++)
        {
            sid.Tick();
        }
    }
}
