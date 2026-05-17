using BenchmarkDotNet.Attributes;
using ViceSharp.Abstractions;

namespace ViceSharp.Benchmarks;

/// <summary>
/// End-to-end benchmark that drives the full Commodore64 wiring (CPU + VIC
/// + CIA1 + CIA2 + SID) via the master <see cref="IClock"/>. The machine is
/// built without ROMs so the workload is deterministic and offline; the
/// emulated cycles-per-second number this produces is the headline figure
/// for the harness.
/// </summary>
[MemoryDiagnoser]
public class FullSystemBenchmark
{
    private IMachine _machine = null!;

    [Params(BenchmarkMachineFactory.PalFrameCycles)]
    public int Cycles { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _machine = BenchmarkMachineFactory.CreateRomlessC64();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _machine.Reset();
    }

    [Benchmark(Description = "Romless C64 one PAL frame via IClock.Step")]
    public void OneFrame()
    {
        _machine.Clock.Step(Cycles);
    }
}
