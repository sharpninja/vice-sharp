using System.Globalization;
using NSubstitute;
using Xunit.Sdk;

namespace ViceSharp.NativeClockProbe.Tests;

/// <summary>
/// Mock-first assertions for the x64sc per-cycle seam. The x64sc CPU core's CLK_INC (c64cpusc.c 47 to 51)
/// increments maincpu_clk and then ORs the int returned by vicii_cycle() into maincpu_ba_low_flags, so the
/// wrapper must forward the real call unchanged, observe the clock after it, publish the returned BA flags for
/// the adapter (the core has not ORed them in yet when the hook runs), and return them untouched.
/// FR-VICE-001 AC2/AC4, TR-VICE-ORACLE-001 AC2/AC3/AC5, TEST-VICE-001 AC2/AC4. PLAN-OBSERVER-001 S2b prerequisite.
/// </summary>
public sealed class ClockHookX64scContractTests
{
    public sealed record CycleInput(ulong Before, ulong AfterReal, int BaFlags);
    public sealed record TraceEvent(string Kind, ulong Clock, int Ba);
    public sealed record TraceResult(int ExitCode, TraceEvent[] Events);
    public sealed record Fixture(CycleInput[] Inputs, TraceEvent[] Expected);

    public interface IX64scClockProbe
    {
        TraceResult Run(CycleInput[] inputs);
    }

    public static Fixture GetFixture(string name) => name switch
    {
        "empty" => new([], []),
        "zero" => new([new(0, 0, 0)], [new("real", 0, 0), new("observe", 0, 0), new("end", 0, 0)]),
        "ba-low" => new([new(7, 11, 1)], [new("real", 7, 1), new("observe", 11, 1), new("end", 11, 1)]),
        "ba-mixed" => new([new(3, 4, 0), new(4, 5, 2), new(5, 6, 3)],
            [new("real", 3, 0), new("observe", 4, 0), new("end", 4, 0),
             new("real", 4, 2), new("observe", 5, 2), new("end", 5, 2),
             new("real", 5, 3), new("observe", 6, 3), new("end", 6, 3)]),
        "bounds" => new([new(ulong.MaxValue, 0, int.MaxValue)],
            [new("real", ulong.MaxValue, int.MaxValue), new("observe", 0, int.MaxValue), new("end", 0, int.MaxValue)]),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    internal static void AssertProbe(IX64scClockProbe probe, string name)
    {
        var fixture = GetFixture(name);
        var input = fixture.Inputs.ToArray();
        var actual = probe.Run(input);
        Assert.NotNull(actual);
        Assert.Equal(0, actual.ExitCode);
        Assert.Equal(fixture.Expected, actual.Events);
        Assert.Equal(fixture.Inputs, input);
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("zero")]
    [InlineData("ba-low")]
    [InlineData("ba-mixed")]
    [InlineData("bounds")]
    public void Correct_mock_passes_and_every_wrong_mock_is_rejected(string name)
    {
        var fixture = GetFixture(name);
        var correct = Substitute.For<IX64scClockProbe>();
        correct.Run(Arg.Any<CycleInput[]>()).Returns(new TraceResult(0, fixture.Expected.ToArray()));
        AssertProbe(correct, name);

        void Reject(TraceResult result)
        {
            var wrong = Substitute.For<IX64scClockProbe>();
            wrong.Run(Arg.Any<CycleInput[]>()).Returns(result);
            Assert.ThrowsAny<XunitException>(() => AssertProbe(wrong, name));
        }

        Reject(new(1, fixture.Expected));
        Reject(new(0, null!));
        Reject(null!);
        Reject(new(0, [.. fixture.Expected, new("end", 0, 0)]));
        for (var i = 0; i < fixture.Expected.Length; i++)
        {
            var wrongClock = fixture.Expected.ToArray();
            wrongClock[i] = wrongClock[i] with { Clock = wrongClock[i].Clock ^ 1UL };
            Reject(new(0, wrongClock));
            var wrongBa = fixture.Expected.ToArray();
            wrongBa[i] = wrongBa[i] with { Ba = wrongBa[i].Ba ^ 1 };
            Reject(new(0, wrongBa));
            var wrongKind = fixture.Expected.ToArray();
            wrongKind[i] = wrongKind[i] with { Kind = "wrong-phase" };
            Reject(new(0, wrongKind));
            Reject(new(0, fixture.Expected.Where((_, index) => index != i).ToArray()));
            if (i + 1 < fixture.Expected.Length)
            {
                var reordered = fixture.Expected.ToArray();
                (reordered[i], reordered[i + 1]) = (reordered[i + 1], reordered[i]);
                Reject(new(0, reordered));
            }
        }
    }
}

/// <summary>The identical assertions against the compiled x64sc hook through the real linker's --wrap=vicii_cycle.</summary>
public sealed class ClockHookX64scImplementationTests
{
    private static readonly CompiledCFixture Fixture = new("clock-probe-x64sc",
        ["x64sc-clock-driver.c", "native-vicii-double.c", "clock-observer-x64sc.c"], "-Wl,--wrap=vicii_cycle");

    [Theory]
    [InlineData("empty")]
    [InlineData("zero")]
    [InlineData("ba-low")]
    [InlineData("ba-mixed")]
    [InlineData("bounds")]
    public void Compiled_wrapper_satisfies_identical_mock_proven_contract(string name) =>
        ClockHookX64scContractTests.AssertProbe(new CompiledProbe(), name);

    private sealed class CompiledProbe : ClockHookX64scContractTests.IX64scClockProbe
    {
        public ClockHookX64scContractTests.TraceResult Run(ClockHookX64scContractTests.CycleInput[] inputs)
        {
            var arguments = inputs.SelectMany(input => new[]
            {
                input.Before.ToString(CultureInfo.InvariantCulture),
                input.AfterReal.ToString(CultureInfo.InvariantCulture),
                input.BaFlags.ToString(CultureInfo.InvariantCulture)
            }).ToArray();
            var result = Fixture.Run(arguments);
            var events = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line =>
                {
                    var fields = line.TrimEnd('\r').Split(',');
                    if (fields.Length != 3 || fields[0] is not ("real" or "observe" or "end"))
                        throw new InvalidDataException("Unexpected C fixture output: " + line);
                    return new ClockHookX64scContractTests.TraceEvent(fields[0],
                        ulong.Parse(fields[1], CultureInfo.InvariantCulture), int.Parse(fields[2], CultureInfo.InvariantCulture));
                }).ToArray();
            if (!string.IsNullOrWhiteSpace(result.Error))
                throw new InvalidDataException("Unexpected C fixture stderr: " + result.Error);
            return new ClockHookX64scContractTests.TraceResult(result.ExitCode, events);
        }
    }
}
