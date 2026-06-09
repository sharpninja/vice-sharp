using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Core;
using ViceSharp.RomFetch;

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

    /// <summary>
    /// Build a real-ROM C64 PAL machine through the production ArchitectureBuilder path.
    /// </summary>
    public static IMachine CreateC64Pal()
    {
        var descriptor = new C64Descriptor(C64MachineProfiles.C64Pal);
        var machine = new ArchitectureBuilder(CreateC64RomProvider()).Build(descriptor);
        if (!string.Equals(machine.Architecture.MachineName, C64MachineProfiles.C64Pal.DisplayName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Expected {C64MachineProfiles.C64Pal.DisplayName}, got {machine.Architecture.MachineName}.");
        }

        return machine;
    }

    private static RomProvider CreateC64RomProvider()
    {
        var dataRoots = ViceDataPathResolver.FindDataRoots();
        foreach (var dataRoot in dataRoots)
        {
            var provider = new RomProvider(
                dataRoot,
                dataRoots.Where(path => !string.Equals(path, dataRoot, StringComparison.OrdinalIgnoreCase)));
            if (new C64RomSet().IsComplete(provider))
                return provider;
        }

        throw new DirectoryNotFoundException(
            "Could not locate a VICE data root with complete C64 ROM resources. Set VICESHARP_ROM_PATH or VICE_DATA_PATH to a VICE data root, or put x64sc.exe on PATH.");
    }
}
