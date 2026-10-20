using System.Globalization;
using static ViceSharp.NativeClockProbe.Tests.BusObserverContractTests;

namespace ViceSharp.NativeClockProbe.Tests;

/// <summary>
/// The identical mock-proven assertions execute the external C bus observer through the real
/// linker's --wrap of vic_cycle, mem_initialize_memory and mem_mmu_translate against a VICE-shaped
/// double (global table pointers, static rows, intra-object toggle). The double is not VICE.
/// No VICE sources, objects, build targets or emulator binaries are used here.
/// </summary>
public sealed class BusObserverImplementationTests
{
    [Theory]
    [InlineData("empty")]
    [InlineData("init-only")]
    [InlineData("read")]
    [InlineData("fetch")]
    [InlineData("write-and-dummy")]
    [InlineData("translate")]
    [InlineData("rebuild-at-runtime")]
    [InlineData("rewrite-outside-init")]
    [InlineData("dummy-alias")]
    [InlineData("watch-toggle")]
    public void Compiled_observer_satisfies_identical_mock_proven_contract(string name) =>
        AssertProbe(new CompiledProbe(), name);

    private sealed class CompiledProbe : IBusObserverProbe
    {
        private static readonly CompiledCFixture Fixture = new("bus-probe",
            ["bus-driver.c", "native-bus-double.c", "clock-observer.c", "bus-observer.c"],
            "-Wl,--wrap=vic_cycle", "-Wl,--wrap=mem_initialize_memory", "-Wl,--wrap=mem_mmu_translate");

        private static readonly string[] Kinds = ["init", "repatch", "access", "translate", "missing", "foreign", "cycle"];

        public TraceResult Run(BusStep[] steps)
        {
            var arguments = steps.Select(step => step.Op switch
            {
                "init" or "cycle" or "alias" => step.Op,
                "clock" or "read" or "dread" or "translate" or "rewrite" or "toggle" =>
                    step.Op + ":" + step.A.ToString(CultureInfo.InvariantCulture),
                "write" or "dwrite" =>
                    step.Op + ":" + step.A.ToString(CultureInfo.InvariantCulture) + ":" + step.B.ToString(CultureInfo.InvariantCulture),
                _ => throw new ArgumentOutOfRangeException(nameof(steps), step.Op)
            }).ToArray();
            var result = Fixture.Run(arguments);
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
}
