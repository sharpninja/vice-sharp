using System.Globalization;
using NSubstitute;
using Xunit.Sdk;

namespace ViceSharp.NativeClockProbe.Tests;

/// <summary>
/// PLAN-OBSERVER-001 S4b: the VICE_OBS emitter (tools/immutable-vice/obs-emitter.c), the pure part of the
/// composition. Per CPU cycle it writes two step 1 lines (OBSERVATION-LINE-SLICE.md), PHI1 then PHI2, with the
/// half-cycle index 2n and 2n+1 for cycle index n since the emitter started, from one input record the machine
/// glue fills from VICE globals. Rules fixed here: PHI1 is idle with address 0 (the CPU is off the bus; contract
/// amendment A1) and the PHI1 data VICE exposes; AEC in PHI1 is low where the VIC owns PHI1 (x64sc) and high on
/// the VIC-20 (D15); AEC in PHI2 is derived from BA where the machine rule says so: low once BA has been low for
/// three consecutive cycles including this one (Bauer, MOS 6567/6569 video controller, 3.14.3; VICE SC's
/// three-cycle write window, mainc64cpu.c 117 to 154; amendment A2); a cycle without a CPU access is idle with
/// an empty data column, which the decoder fails closed as Missing:Data, never a guessed byte.
/// FR-VICE-001 AC4, TR-VICE-ORACLE-001 AC3/AC5, TEST-VICE-001 AC4.
/// </summary>
public sealed class ObsEmitterContractTests
{
    /// <summary>One cycle as the glue hands it to the emitter. Kind null means no CPU access; Phi1Data null means not exposed.</summary>
    public sealed record CycleInput(ulong Clock, string? Kind, ushort Address, byte Data, byte Mask, bool BaHigh, bool Stolen, byte? Phi1Data);
    /// <summary>Machine rules: whether the VIC owns PHI1 (AEC low there) and whether PHI2 AEC follows BA.</summary>
    public sealed record Rules(bool AecPhi1Low, bool AecFromBa);
    public sealed record TraceResult(int ExitCode, string[] Lines);
    public sealed record Fixture(Rules Rules, CycleInput[] Inputs, string[] Expected);

    public interface IObsEmitterProbe
    {
        TraceResult Run(Rules rules, CycleInput[] inputs);
    }

    public static readonly Rules X64sc = new(true, true);
    public static readonly Rules Xvic = new(false, false);

    private static string Line(long half, string phase, int aec, int ba, string access, int address, string data, int mask, int stolen, ulong clock) =>
        $"VICE_OBS,1,{half},{phase},{aec},{ba},{access},{address},{data},{mask},{stolen},{clock}";

    public static Fixture GetFixture(string name) => name switch
    {
        "empty" => new(X64sc, [], []),
        "single-read" => new(X64sc,
            [new(20, "read", 65532, 226, 0, true, false, 17)],
            [Line(0, "PHI1", 0, 1, "idle", 0, "17", 0, 0, 20), Line(1, "PHI2", 1, 1, "read", 65532, "226", 0, 0, 20)]),
        "write-then-dummy" => new(X64sc,
            [new(20, "read", 65532, 226, 0, true, false, 17), new(21, "write", 53280, 7, 2, true, false, 18), new(22, "dummy", 256, 165, 2, true, false, 19)],
            [Line(0, "PHI1", 0, 1, "idle", 0, "17", 0, 0, 20), Line(1, "PHI2", 1, 1, "read", 65532, "226", 0, 0, 20),
             Line(2, "PHI1", 0, 1, "idle", 0, "18", 2, 0, 21), Line(3, "PHI2", 1, 1, "write", 53280, "7", 2, 0, 21),
             Line(4, "PHI1", 0, 1, "idle", 0, "19", 2, 0, 22), Line(5, "PHI2", 1, 1, "dummy", 256, "165", 2, 0, 22)]),
        // BA low for four cycles: PHI2 AEC stays high for the first two, goes low from the third (three-cycle rule).
        "ba-low-four" => new(X64sc,
            [new(30, "read", 4096, 1, 0, false, false, 0), new(31, "write", 4097, 2, 0, false, false, 0),
             new(32, "read", 4098, 3, 0, false, false, 0), new(33, "read", 4099, 4, 0, false, false, 0), new(34, "read", 4100, 5, 0, true, false, 0)],
            [Line(0, "PHI1", 0, 0, "idle", 0, "0", 0, 0, 30), Line(1, "PHI2", 1, 0, "read", 4096, "1", 0, 0, 30),
             Line(2, "PHI1", 0, 0, "idle", 0, "0", 0, 0, 31), Line(3, "PHI2", 1, 0, "write", 4097, "2", 0, 0, 31),
             Line(4, "PHI1", 0, 0, "idle", 0, "0", 0, 0, 32), Line(5, "PHI2", 0, 0, "read", 4098, "3", 0, 0, 32),
             Line(6, "PHI1", 0, 0, "idle", 0, "0", 0, 0, 33), Line(7, "PHI2", 0, 0, "read", 4099, "4", 0, 0, 33),
             Line(8, "PHI1", 0, 1, "idle", 0, "0", 0, 0, 34), Line(9, "PHI2", 1, 1, "read", 4100, "5", 0, 0, 34)]),
        // A stolen cycle has no CPU access: PHI2 idle, address 0, empty data (fails closed downstream), stolen 1.
        "stolen-idle" => new(X64sc,
            [new(40, "read", 4096, 1, 0, false, false, 9), new(41, null, 0, 0, 0, false, true, 10), new(42, null, 0, 0, 0, false, true, 11), new(43, "read", 4097, 2, 0, true, false, 12)],
            [Line(0, "PHI1", 0, 0, "idle", 0, "9", 0, 0, 40), Line(1, "PHI2", 1, 0, "read", 4096, "1", 0, 0, 40),
             Line(2, "PHI1", 0, 0, "idle", 0, "10", 0, 1, 41), Line(3, "PHI2", 1, 0, "idle", 0, "", 0, 1, 41),
             Line(4, "PHI1", 0, 0, "idle", 0, "11", 0, 1, 42), Line(5, "PHI2", 0, 0, "idle", 0, "", 0, 1, 42),
             Line(6, "PHI1", 0, 1, "idle", 0, "12", 0, 0, 43), Line(7, "PHI2", 1, 1, "read", 4097, "2", 0, 0, 43)]),
        // Interrupt mask and maximum values pass through unchanged; unknown PHI1 data is an empty column.
        "mask-and-bounds" => new(X64sc,
            [new(ulong.MaxValue, "write", 65535, 255, 127, true, false, null)],
            [Line(0, "PHI1", 0, 1, "idle", 0, "", 127, 0, ulong.MaxValue), Line(1, "PHI2", 1, 1, "write", 65535, "255", 127, 0, ulong.MaxValue)]),
        // VIC-20 rules (D15): ba 0, aec 1 on both half-cycles, stolen 0, whatever BA the glue passes.
        "xvic-rules" => new(Xvic,
            [new(6, "read", 65532, 34, 0, false, false, 201), new(7, "read", 65533, 253, 0, false, false, 202)],
            [Line(0, "PHI1", 1, 0, "idle", 0, "201", 0, 0, 6), Line(1, "PHI2", 1, 0, "read", 65532, "34", 0, 0, 6),
             Line(2, "PHI1", 1, 0, "idle", 0, "202", 0, 0, 7), Line(3, "PHI2", 1, 0, "read", 65533, "253", 0, 0, 7)]),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    public static readonly string[] FixtureNames = ["empty", "single-read", "write-then-dummy", "ba-low-four", "stolen-idle", "mask-and-bounds", "xvic-rules"];

    internal static void AssertProbe(IObsEmitterProbe probe, string name)
    {
        var fixture = GetFixture(name);
        var actual = probe.Run(fixture.Rules, fixture.Inputs.ToArray());
        Assert.NotNull(actual);
        Assert.Equal(0, actual.ExitCode);
        Assert.Equal(fixture.Expected, actual.Lines);
    }

    [Fact]
    public void Every_expected_line_has_twelve_columns_and_the_step_1_prefix()
    {
        foreach (var name in FixtureNames)
            foreach (var line in GetFixture(name).Expected)
            {
                Assert.StartsWith("VICE_OBS,1,", line, StringComparison.Ordinal);
                Assert.Equal(12, line.Split(',').Length);
                Assert.DoesNotContain('\n', line);
            }
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("single-read")]
    [InlineData("write-then-dummy")]
    [InlineData("ba-low-four")]
    [InlineData("stolen-idle")]
    [InlineData("mask-and-bounds")]
    [InlineData("xvic-rules")]
    public void Correct_mock_passes_and_every_wrong_mock_is_rejected(string name)
    {
        var fixture = GetFixture(name);
        var correct = Substitute.For<IObsEmitterProbe>();
        correct.Run(Arg.Any<Rules>(), Arg.Any<CycleInput[]>()).Returns(new TraceResult(0, fixture.Expected.ToArray()));
        AssertProbe(correct, name);

        void Reject(TraceResult result)
        {
            var wrong = Substitute.For<IObsEmitterProbe>();
            wrong.Run(Arg.Any<Rules>(), Arg.Any<CycleInput[]>()).Returns(result);
            Assert.ThrowsAny<XunitException>(() => AssertProbe(wrong, name));
        }

        Reject(new(1, fixture.Expected));
        Reject(new(0, null!));
        Reject(null!);
        Reject(new(0, [.. fixture.Expected, "VICE_OBS,1,99,PHI1,0,1,idle,0,0,0,0,0"]));
        for (var i = 0; i < fixture.Expected.Length; i++)
        {
            var columns = fixture.Expected[i].Split(',');
            for (var c = 2; c < columns.Length; c++)
            {
                var corrupted = columns.ToArray();
                corrupted[c] = corrupted[c] == "" ? "7" : (corrupted[c] == "PHI1" ? "PHI2" : corrupted[c] == "PHI2" ? "PHI1" : corrupted[c] + "1");
                var lines = fixture.Expected.ToArray();
                lines[i] = string.Join(',', corrupted);
                Reject(new(0, lines));
            }
            Reject(new(0, fixture.Expected.Where((_, index) => index != i).ToArray()));
            if (i + 1 < fixture.Expected.Length)
            {
                var reordered = fixture.Expected.ToArray();
                (reordered[i], reordered[i + 1]) = (reordered[i + 1], reordered[i]);
                Reject(new(0, reordered));
            }
        }
    }

    [Fact]
    public void Lenient_variants_of_the_rules_are_rejected()
    {
        void RejectVariant(string name, Func<string[], string[]> variant)
        {
            var lenient = Substitute.For<IObsEmitterProbe>();
            lenient.Run(Arg.Any<Rules>(), Arg.Any<CycleInput[]>()).Returns(new TraceResult(0, variant(GetFixture(name).Expected.ToArray())));
            Assert.ThrowsAny<XunitException>(() => AssertProbe(lenient, name));
        }
        // A guessed data byte on a stolen idle half-cycle is wrong: the column must stay empty.
        RejectVariant("stolen-idle", e => e.Select(l => l.Replace(",idle,0,,0,1,", ",idle,0,0,0,1,")).ToArray());
        // AEC must not go low before the third BA-low cycle, and must go low from it.
        RejectVariant("ba-low-four", e => e.Select((l, i) => i == 3 ? l.Replace("PHI2,1,0", "PHI2,0,0") : l).ToArray());
        RejectVariant("ba-low-four", e => e.Select((l, i) => i == 5 ? l.Replace("PHI2,0,0", "PHI2,1,0") : l).ToArray());
        // The VIC-20 rules must not borrow the C64 AEC behavior.
        RejectVariant("xvic-rules", e => e.Select(l => l.Replace("PHI1,1,0", "PHI1,0,0")).ToArray());
        // Half-cycle indices must be consecutive from zero.
        RejectVariant("write-then-dummy", e => e.Select(l => l.Replace("VICE_OBS,1,4,", "VICE_OBS,1,5,")).ToArray());
    }
}

/// <summary>The identical assertions against the compiled emitter through a scripted driver.</summary>
public sealed class ObsEmitterImplementationTests
{
    private static readonly CompiledCFixture Fixture = new("obs-emitter", ["obs-emitter-driver.c", "obs-emitter.c"]);

    [Theory]
    [InlineData("empty")]
    [InlineData("single-read")]
    [InlineData("write-then-dummy")]
    [InlineData("ba-low-four")]
    [InlineData("stolen-idle")]
    [InlineData("mask-and-bounds")]
    [InlineData("xvic-rules")]
    public void Compiled_emitter_satisfies_identical_mock_proven_contract(string name) =>
        ObsEmitterContractTests.AssertProbe(new CompiledProbe(), name);

    private sealed class CompiledProbe : ObsEmitterContractTests.IObsEmitterProbe
    {
        public ObsEmitterContractTests.TraceResult Run(ObsEmitterContractTests.Rules rules, ObsEmitterContractTests.CycleInput[] inputs)
        {
            var ic = CultureInfo.InvariantCulture;
            string[] arguments = [rules.AecPhi1Low ? "1" : "0", rules.AecFromBa ? "1" : "0",
                .. inputs.Select(i => string.Join(':', i.Clock.ToString(ic), i.Kind ?? "-", i.Address.ToString(ic), i.Data.ToString(ic),
                    i.Mask.ToString(ic), i.BaHigh ? "1" : "0", i.Stolen ? "1" : "0", i.Phi1Data?.ToString(ic) ?? "-"))];
            var result = Fixture.Run(arguments);
            if (!string.IsNullOrWhiteSpace(result.Error))
                throw new InvalidDataException("Unexpected C fixture stderr: " + result.Error);
            var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimEnd('\r')).ToArray();
            return new ObsEmitterContractTests.TraceResult(result.ExitCode, lines);
        }
    }
}
