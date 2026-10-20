using System.Globalization;
using static ViceSharp.NativeClockProbe.Tests.BusObserverContractTests;

namespace ViceSharp.NativeClockProbe.Tests;

/// <summary>
/// Runs the mock-proven bus observer contract against the compiled external wrapper linked with the
/// VICE-shaped double through the real linker's --wrap of vic_cycle, mem_initialize_memory and
/// mem_mmu_translate. Extra compiler arguments select the double's shape (for example the 32-row
/// x64sc layout). No VICE sources, objects, build targets or emulator binaries are used.
/// </summary>
internal sealed class CompiledBusProbe : IBusObserverProbe
{
    private static readonly string[] Kinds = ["init", "layout", "repatch", "access", "translate", "missing", "foreign", "cycle"];
    private readonly CompiledCFixture fixture;

    internal CompiledBusProbe(CompiledCFixture fixture) => this.fixture = fixture;

    internal static CompiledCFixture Build(string name, params string[] extraArguments) => new(name,
        ["bus-driver.c", "native-bus-double.c", "clock-observer.c", "bus-observer.c"],
        [.. extraArguments, "-Wl,--wrap=vic_cycle", "-Wl,--wrap=mem_initialize_memory", "-Wl,--wrap=mem_mmu_translate"]);

    public TraceResult Run(BusStep[] steps)
    {
        var arguments = steps.Select(step => step.Op switch
        {
            "init" or "cycle" or "alias" => step.Op,
            "clock" or "read" or "dread" or "translate" or "rewrite" or "toggle" or "bank" =>
                step.Op + ":" + step.A.ToString(CultureInfo.InvariantCulture),
            "write" or "dwrite" or "rewriterow" =>
                step.Op + ":" + step.A.ToString(CultureInfo.InvariantCulture) + ":" + step.B.ToString(CultureInfo.InvariantCulture),
            _ => throw new ArgumentOutOfRangeException(nameof(steps), step.Op)
        }).ToArray();
        var result = fixture.Run(arguments);
        var events = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line =>
            {
                var text = line.TrimEnd('\r');
                var comma = text.IndexOf(',');
                if (comma <= 0 || !Kinds.Contains(text[..comma]))
                    throw new InvalidDataException("Unexpected C fixture output: " + line);
                return new BusEvent(text[..comma], text[(comma + 1)..]);
            }).ToArray();
        if (!string.IsNullOrWhiteSpace(result.Error))
            throw new InvalidDataException("Unexpected C fixture stderr: " + result.Error);
        return new TraceResult(result.ExitCode, events);
    }
}
