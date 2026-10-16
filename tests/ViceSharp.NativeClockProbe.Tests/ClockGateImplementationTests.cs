using System.Globalization;
using static ViceSharp.NativeClockProbe.Tests.ClockGateContractTests;

namespace ViceSharp.NativeClockProbe.Tests;

/// <summary>Same mock-proven gate inputs and assertions, bound to the real compiled C gate.</summary>
public sealed class ClockGateImplementationTests
{
    [Theory]
    [MemberData(nameof(Cases), MemberType = typeof(ClockGateContractTests))]
    public void Compiled_gate_satisfies_identical_mock_proven_contract(string name) =>
        AssertGate(new CompiledGate(), name);

    private sealed class CompiledGate : IGateProbe
    {
        private static readonly CompiledCFixture Fixture = new("clock-gate", ["gate-driver.c", "clock-gate.c"]);

        public GateResult Run(GateInput input)
        {
            string[] arguments = [input.Clock.ToString(CultureInfo.InvariantCulture),
                input.Publish.ToString(CultureInfo.InvariantCulture), input.Flush.ToString(CultureInfo.InvariantCulture),
                input.Receive.ToString(CultureInfo.InvariantCulture), input.Missing.ToString(CultureInfo.InvariantCulture)];
            var result = Fixture.Run(arguments);
            if (result.ExitCode != 0 || !string.IsNullOrWhiteSpace(result.Error))
                throw new InvalidDataException($"C gate fixture failed: {result.ExitCode}: {result.Error}");
            var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.TrimEnd('\r')).ToArray();
            if (lines.Length == 0 || !lines[^1].StartsWith("status,", StringComparison.Ordinal))
                throw new InvalidDataException("Missing terminal gate status.");
            var status = int.Parse(lines[^1][7..], CultureInfo.InvariantCulture);
            var events = lines[..^1].Select(line =>
            {
                var fields = line.Split(',');
                if (fields.Length != 2 || fields[0] is not ("publish" or "flush" or "receive"))
                    throw new InvalidDataException("Unexpected gate fixture output: " + line);
                return new GateEvent(fields[0], ulong.Parse(fields[1], CultureInfo.InvariantCulture));
            }).ToArray();
            return new GateResult(status, events);
        }
    }
}
