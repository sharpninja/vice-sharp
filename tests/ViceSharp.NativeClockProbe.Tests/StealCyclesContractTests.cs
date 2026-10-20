using System.Globalization;
using NSubstitute;
using Xunit.Sdk;

namespace ViceSharp.NativeClockProbe.Tests;

/// <summary>
/// PLAN-OBSERVER-001 S4a: VIC-II stolen cycles made visible. vicii_steal_cycles (viciisc/vicii-cycle.c 628 to 636)
/// loops "maincpu_clk++; ba_low = vicii_cycle();" inside its own object, so neither the per-cycle hook nor the gate
/// sees stolen cycles. The wrapper intercepts the cross-object call from the CPU core (mainc64cpu.c 118) and
/// executes the same three statements itself, calling vicii_cycle cross-object so every stolen cycle reaches the
/// hook with the stolen flag set; the flag is clear again when control returns to the core.
/// FR-VICE-001 AC2/AC4, TR-VICE-ORACLE-001 AC2/AC3/AC5, TEST-VICE-001 AC2/AC4.
/// </summary>
public sealed class StealCyclesContractTests
{
    /// <summary>Clock before the steal and the BA values the VIC double returns per inner cycle (the last must be 0, which ends the loop).</summary>
    public sealed record StealInput(ulong StartClock, int[] BaSequence);
    public sealed record TraceEvent(string Kind, ulong Clock, int Ba, int Stolen);
    public sealed record TraceResult(int ExitCode, TraceEvent[] Events);
    public sealed record Fixture(StealInput Input, TraceEvent[] Expected);

    public interface IStealProbe
    {
        TraceResult Run(StealInput input);
    }

    private static TraceEvent[] Expect(ulong start, params int[] ba)
    {
        var events = new List<TraceEvent>();
        for (var i = 0; i < ba.Length; i++)
        {
            var clock = start + (ulong)i + 1;
            events.Add(new("real", clock, ba[i], 0));
            events.Add(new("observe", clock, ba[i], 1));
        }
        events.Add(new("end", start + (ulong)ba.Length, 0, 0));
        return [.. events];
    }

    public static Fixture GetFixture(string name) => name switch
    {
        "one" => new(new(10, [0]), Expect(10, 0)),
        "three" => new(new(100, [1, 1, 0]), Expect(100, 1, 1, 0)),
        "forty" => new(new(5000, [.. Enumerable.Repeat(1, 39), 0]), Expect(5000, [.. Enumerable.Repeat(1, 39), 0])),
        "flags-mixed" => new(new(7, [3, 1, 2, 0]), Expect(7, 3, 1, 2, 0)),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    internal static void AssertProbe(IStealProbe probe, string name)
    {
        var fixture = GetFixture(name);
        var actual = probe.Run(fixture.Input);
        Assert.NotNull(actual);
        Assert.Equal(0, actual.ExitCode);
        Assert.Equal(fixture.Expected, actual.Events);
    }

    [Theory]
    [InlineData("one")]
    [InlineData("three")]
    [InlineData("forty")]
    [InlineData("flags-mixed")]
    public void Correct_mock_passes_and_every_wrong_mock_is_rejected(string name)
    {
        var fixture = GetFixture(name);
        var correct = Substitute.For<IStealProbe>();
        correct.Run(Arg.Any<StealInput>()).Returns(new TraceResult(0, fixture.Expected.ToArray()));
        AssertProbe(correct, name);

        void Reject(TraceResult result)
        {
            var wrong = Substitute.For<IStealProbe>();
            wrong.Run(Arg.Any<StealInput>()).Returns(result);
            Assert.ThrowsAny<XunitException>(() => AssertProbe(wrong, name));
        }

        Reject(new(1, fixture.Expected));
        Reject(new(0, null!));
        Reject(null!);
        // The real loop hidden from the hook: only "real" lines and the end, no "observe" per stolen cycle.
        Reject(new(0, fixture.Expected.Where(e => e.Kind != "observe").ToArray()));
        // Stolen flag not set, or still set after the steal returns.
        Reject(new(0, fixture.Expected.Select(e => e with { Stolen = 0 }).ToArray()));
        Reject(new(0, fixture.Expected.Select(e => e.Kind == "end" ? e with { Stolen = 1 } : e).ToArray()));
        for (var i = 0; i < fixture.Expected.Length; i++)
        {
            var wrongClock = fixture.Expected.ToArray();
            wrongClock[i] = wrongClock[i] with { Clock = wrongClock[i].Clock ^ 1UL };
            Reject(new(0, wrongClock));
            var wrongBa = fixture.Expected.ToArray();
            wrongBa[i] = wrongBa[i] with { Ba = wrongBa[i].Ba ^ 1 };
            Reject(new(0, wrongBa));
            Reject(new(0, fixture.Expected.Where((_, index) => index != i).ToArray()));
        }
    }
}

/// <summary>The identical assertions against the compiled x64sc hook with --wrap=vicii_steal_cycles.</summary>
public sealed class StealCyclesImplementationTests
{
    private static readonly CompiledCFixture Fixture = new("steal-probe-x64sc",
        ["x64sc-steal-driver.c", "native-vicii-steal-double.c", "clock-observer-x64sc.c"],
        "-Wl,--wrap=vicii_cycle", "-Wl,--wrap=vicii_steal_cycles");

    [Theory]
    [InlineData("one")]
    [InlineData("three")]
    [InlineData("forty")]
    [InlineData("flags-mixed")]
    public void Compiled_wrapper_satisfies_identical_mock_proven_contract(string name) =>
        StealCyclesContractTests.AssertProbe(new CompiledProbe(), name);

    private sealed class CompiledProbe : StealCyclesContractTests.IStealProbe
    {
        public StealCyclesContractTests.TraceResult Run(StealCyclesContractTests.StealInput input)
        {
            string[] arguments = [input.StartClock.ToString(CultureInfo.InvariantCulture),
                .. input.BaSequence.Select(b => b.ToString(CultureInfo.InvariantCulture))];
            var result = Fixture.Run(arguments);
            var events = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line =>
                {
                    var f = line.TrimEnd('\r').Split(',');
                    if (f.Length != 4 || f[0] is not ("real" or "observe" or "end"))
                        throw new InvalidDataException("Unexpected C fixture output: " + line);
                    return new StealCyclesContractTests.TraceEvent(f[0], ulong.Parse(f[1], CultureInfo.InvariantCulture),
                        int.Parse(f[2], CultureInfo.InvariantCulture), int.Parse(f[3], CultureInfo.InvariantCulture));
                }).ToArray();
            if (!string.IsNullOrWhiteSpace(result.Error))
                throw new InvalidDataException("Unexpected C fixture stderr: " + result.Error);
            return new StealCyclesContractTests.TraceResult(result.ExitCode, events);
        }
    }
}
