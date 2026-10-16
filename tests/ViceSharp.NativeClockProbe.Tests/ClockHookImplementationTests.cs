using System.Globalization;
using static ViceSharp.NativeClockProbe.Tests.ClockHookContractTests;

namespace ViceSharp.NativeClockProbe.Tests;

/// <summary>
/// The identical mock-proven assertions execute the external C wrapper through
/// the real linker's --wrap=vic_cycle. The native-call double is not VICE.
/// No VICE sources, objects, build targets or emulator binaries are used here.
/// </summary>
public sealed class ClockHookImplementationTests
{
    [Theory]
    [InlineData("empty")]
    [InlineData("zero")]
    [InlineData("native-change")]
    [InlineData("bounds")]
    [InlineData("nonmonotonic")]
    [InlineData("repeated")]
    public void Compiled_wrapper_satisfies_identical_mock_proven_contract(string name) =>
        AssertProbe(new CompiledProbe(), name);

    private sealed class CompiledProbe : IClockProbe
    {
        private static readonly CompiledCFixture Fixture = new("clock-probe",
            ["clock-driver.c", "native-cycle-double.c", "clock-observer.c"], "-Wl,--wrap=vic_cycle");

        public TraceResult Run(CycleInput[] inputs)
        {
            var arguments = inputs.SelectMany(input => new[]
            {
                input.Before.ToString(CultureInfo.InvariantCulture),
                input.AfterReal.ToString(CultureInfo.InvariantCulture)
            }).ToArray();
            var result = Fixture.Run(arguments);
            var events = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line =>
                {
                    var fields = line.TrimEnd('\r').Split(',');
                    if (fields.Length != 2 || fields[0] is not ("real" or "observe" or "end"))
                        throw new InvalidDataException("Unexpected C fixture output: " + line);
                    return new TraceEvent(fields[0], ulong.Parse(fields[1], CultureInfo.InvariantCulture));
                }).ToArray();
            if (!string.IsNullOrWhiteSpace(result.Error))
                throw new InvalidDataException("Unexpected C fixture stderr: " + result.Error);
            return new TraceResult(result.ExitCode, events);
        }

    }
}
