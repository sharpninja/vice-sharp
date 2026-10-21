using System.Globalization;
using static ViceSharp.NativeClockProbe.Tests.SnapshotCheckpointContractTests;

namespace ViceSharp.NativeClockProbe.Tests;

/// <summary>
/// The identical mock-proven assertions execute the external C unit tools/immutable-vice/snapshot-checkpoint.c
/// against doubles of the pinned trap registration and snapshot writer. The doubles are not VICE; no VICE sources,
/// objects or binaries are used here. PLAN-NATIVERESTORE-001 D1 (R12, N15, N21, N22, N28).
/// </summary>
public sealed class SnapshotCheckpointImplementationTests
{
    [Theory]
    [InlineData("unset")]
    [InlineData("exact-boundary")]
    [InlineData("delayed-by-interrupt")]
    [InlineData("write-fails-negative")]
    [InlineData("write-fails-positive")]
    [InlineData("once-only")]
    [InlineData("trigger-not-reached")]
    [InlineData("literal-100000")]
    [InlineData("malformed")]
    [InlineData("missing-path")]
    public void Compiled_unit_satisfies_identical_mock_proven_contract(string name) =>
        AssertProbe(new CompiledProbe(), name);

    private sealed class CompiledProbe : ISnapshotCheckpointProbe
    {
        private static readonly CompiledCFixture Fixture = new("snapshot-checkpoint-probe",
            ["snapshot-checkpoint-driver.c", "snapshot-checkpoint-double.c", "snapshot-checkpoint.c"]);

        public TraceResult Run(Scenario s)
        {
            string[] arguments =
            [
                s.Spec ?? "-", s.Start.ToString(CultureInfo.InvariantCulture), s.End.ToString(CultureInfo.InvariantCulture),
                s.BoundaryClock.ToString(CultureInfo.InvariantCulture), s.WriteResult.ToString(CultureInfo.InvariantCulture)
            ];
            var result = Fixture.Run(arguments);
            if (!string.IsNullOrWhiteSpace(result.Error))
                throw new InvalidDataException("Unexpected C fixture stderr: " + result.Error);
            var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimEnd('\r')).ToArray();
            return new TraceResult(result.ExitCode, lines);
        }
    }
}
