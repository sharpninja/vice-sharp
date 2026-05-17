using BenchmarkDotNet.Attributes;
using ViceSharp.Chips.Cpu;
using ViceSharp.Core;

namespace ViceSharp.Benchmarks;

/// <summary>
/// Per-cycle benchmark for the MOS 6502 / 6510 CPU core.
/// Runs a deterministic NOP-only workload through Tick() to measure
/// the JIT-native cost of the inner loop.
/// </summary>
[MemoryDiagnoser]
public class CpuBenchmarks
{
    private Mos6502 _cpu = null!;
    private byte[] _ram = null!;
    private BasicBus _bus = null!;

    [Params(BenchmarkMachineFactory.DefaultCycleBudget)]
    public int Cycles { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _bus = new BasicBus();
        _ram = new byte[0x10000];

        // Fill RAM with $EA (NOP) so the CPU loops through a stable workload
        // and avoids hitting the open bus pattern returned by an empty BasicBus.
        Array.Fill(_ram, (byte)0xEA);

        // Reset vector points at $0000 so we trivially loop NOPs.
        _ram[0xFFFC] = 0x00;
        _ram[0xFFFD] = 0x00;

        _bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, _ram));

        _cpu = new Mos6502(_bus);
        _bus.RegisterDevice(_cpu);
        _cpu.Reset();
    }

    [Benchmark(Description = "Mos6502.Tick() x N")]
    public void TickLoop()
    {
        var cpu = _cpu;
        var cycles = Cycles;
        for (var i = 0; i < cycles; i++)
        {
            cpu.Tick();
        }
    }
}
