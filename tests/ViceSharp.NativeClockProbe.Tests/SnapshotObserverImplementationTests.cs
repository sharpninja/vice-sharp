using System.Globalization;
using static ViceSharp.NativeClockProbe.Tests.SnapshotObserverContractTests;

namespace ViceSharp.NativeClockProbe.Tests;

/// <summary>
/// The identical mock-proven assertions execute the external C unit tools/immutable-vice/snapshot-observer.c
/// through the real linker's --wrap=maincpu_snapshot_read_module and --wrap=machine_read_snapshot. The doubles are
/// not VICE; no VICE sources, objects or binaries are used here. PLAN-NATIVERESTORE-001 D1 (R11 B, N02, N04).
/// </summary>
public sealed class SnapshotObserverImplementationTests
{
    [Theory]
    [InlineData("approved")]
    [InlineData("zero-flag-set")]
    [InlineData("all-flags")]
    [InlineData("wide")]
    [InlineData("cpu-read-fails")]
    [InlineData("later-module-fails")]
    public void Compiled_unit_satisfies_identical_mock_proven_contract(string name) =>
        AssertProbe(new CompiledProbe(), name);

    private sealed class CompiledProbe : ISnapshotObserverProbe
    {
        private static readonly CompiledCFixture Fixture = new("snapshot-observer-probe",
            ["snapshot-observer-driver.c", "snapshot-observer-double.c", "snapshot-observer.c"],
            "-Wl,--wrap=maincpu_snapshot_read_module", "-Wl,--wrap=machine_read_snapshot", "-I" + Path.Combine(AppContext.BaseDirectory, "Fixtures"));

        public TraceResult Run(Scenario s)
        {
            var r = s.Registers;
            string[] arguments =
            [
                s.ReadResult.ToString(CultureInfo.InvariantCulture), r.Clock.ToString(CultureInfo.InvariantCulture),
                r.A.ToString(CultureInfo.InvariantCulture), r.X.ToString(CultureInfo.InvariantCulture), r.Y.ToString(CultureInfo.InvariantCulture),
                r.Sp.ToString(CultureInfo.InvariantCulture), r.Pc.ToString(CultureInfo.InvariantCulture), r.P.ToString(CultureInfo.InvariantCulture),
                r.N.ToString(CultureInfo.InvariantCulture), r.Z.ToString(CultureInfo.InvariantCulture),
                s.LoadResult.ToString(CultureInfo.InvariantCulture), s.Drives
            ];
            var result = Fixture.Run(arguments);
            if (!string.IsNullOrWhiteSpace(result.Error))
                throw new InvalidDataException("Unexpected C fixture stderr: " + result.Error);
            var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimEnd('\r')).ToArray();
            return new TraceResult(result.ExitCode, lines);
        }
    }
}
