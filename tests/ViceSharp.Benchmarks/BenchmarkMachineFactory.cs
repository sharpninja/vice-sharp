using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Core;

namespace ViceSharp.Benchmarks;

/// <summary>
/// Shared helpers for constructing the standalone chip instances and a full
/// C64 machine used by the benchmark suite. These helpers intentionally avoid
/// requiring on-disk ROMs so each benchmark stays deterministic and offline.
/// </summary>
internal static class BenchmarkMachineFactory
{
    /// <summary>
    /// Default workload size for per-cycle benchmarks. Tuned to land in the
    /// few-millisecond range under release-mode JIT so BenchmarkDotNet can
    /// produce stable measurements without long warmup.
    /// </summary>
    public const int DefaultCycleBudget = 100_000;

    /// <summary>
    /// PAL frame cycle count used by VIC-II benchmarks (312 lines x 63 cycles).
    /// </summary>
    public const int PalFrameCycles = 312 * 63;

    /// <summary>
    /// Build a freshly-reset Commodore64 instance with the ROM regions left
    /// as RAM. Useful for hot path measurements where the boot ROM content
    /// is not interesting.
    /// </summary>
    public static IMachine CreateRomlessC64()
    {
        var bus = new BasicBus();
        var clock = new SystemClock(985_248);
        var irq = new InterruptLine(InterruptType.Irq);
        var nmi = new InterruptLine(InterruptType.Nmi);
        var machine = new Commodore64(bus, clock, irq, nmi);
        machine.Reset();
        return machine;
    }
}
