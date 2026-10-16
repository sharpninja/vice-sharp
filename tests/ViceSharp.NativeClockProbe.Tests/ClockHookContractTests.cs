using NSubstitute;
using Xunit.Sdk;

namespace ViceSharp.NativeClockProbe.Tests;

/// <summary>
/// Mock-first assertions for the external vic_cycle forwarding seam, not VICE parity.
/// FR-VICE-001 AC2/AC4, TR-VICE-ORACLE-001 AC2/AC3/AC5, TEST-VICE-001 AC2/AC4.
/// The real-call double may change clock state; the observer must see that state.
/// </summary>
public sealed class ClockHookContractTests
{
    public sealed record CycleInput(ulong Before, ulong AfterReal);
    public sealed record TraceEvent(string Kind, ulong Clock);
    public sealed record TraceResult(int ExitCode, TraceEvent[] Events);
    public sealed record Fixture(CycleInput[] Inputs, TraceEvent[] Expected);

    /// <summary>Same seam and assertions for mocks and the later compiled C fixture.</summary>
    public interface IClockProbe
    {
        TraceResult Run(CycleInput[] inputs);
    }

    /// <summary>Literal expectations precede the wrapper implementation.</summary>
    public static Fixture GetFixture(string name) => name switch
    {
        "empty" => new([], []),
        "zero" => new([new(0, 0)], [new("real", 0), new("observe", 0), new("end", 0)]),
        "native-change" => new([new(7, 11)], [new("real", 7), new("observe", 11), new("end", 11)]),
        "bounds" => new([new(0, ulong.MaxValue), new(ulong.MaxValue, 0), new(ulong.MaxValue, ulong.MaxValue)],
            [new("real", 0), new("observe", ulong.MaxValue), new("end", ulong.MaxValue),
             new("real", ulong.MaxValue), new("observe", 0), new("end", 0),
             new("real", ulong.MaxValue), new("observe", ulong.MaxValue), new("end", ulong.MaxValue)]),
        "nonmonotonic" => new([new(9, 3), new(2, 5)],
            [new("real", 9), new("observe", 3), new("end", 3),
             new("real", 2), new("observe", 5), new("end", 5)]),
        "repeated" => new([new(4, 4), new(4, 4)],
            [new("real", 4), new("observe", 4), new("end", 4),
             new("real", 4), new("observe", 4), new("end", 4)]),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    /// <summary>Exact event order proves forwarding, observation ordering and preserved state.</summary>
    internal static void AssertProbe(IClockProbe probe, string name)
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
    [InlineData("native-change")]
    [InlineData("bounds")]
    [InlineData("nonmonotonic")]
    [InlineData("repeated")]
    public void Correct_mock_passes_and_every_wrong_mock_is_rejected(string name)
    {
        var fixture = GetFixture(name);
        var correct = Substitute.For<IClockProbe>();
        correct.Run(Arg.Any<CycleInput[]>()).Returns(new TraceResult(0, fixture.Expected.ToArray()));
        AssertProbe(correct, name);
        correct.Received(1).Run(Arg.Any<CycleInput[]>());

        void Reject(TraceResult result)
        {
            var wrong = Substitute.For<IClockProbe>();
            wrong.Run(Arg.Any<CycleInput[]>()).Returns(result);
            Assert.ThrowsAny<XunitException>(() => AssertProbe(wrong, name));
        }

        Reject(new(1, fixture.Expected));
        Reject(new(0, null!));
        Reject(null!);
        Reject(new(0, [.. fixture.Expected, new("unexpected", 123)]));
        for (var i = 0; i < fixture.Expected.Length; i++)
        {
            var wrongClock = fixture.Expected.ToArray();
            wrongClock[i] = wrongClock[i] with { Clock = wrongClock[i].Clock ^ 1UL };
            Reject(new(0, wrongClock));
            var wrongKind = fixture.Expected.ToArray();
            wrongKind[i] = wrongKind[i] with { Kind = "wrong-phase" };
            Reject(new(0, wrongKind));
            Reject(new(0, fixture.Expected.Where((_, index) => index != i).ToArray()));
            Reject(new(0, [.. fixture.Expected.Take(i), fixture.Expected[i], .. fixture.Expected.Skip(i)]));
            if (i + 1 < fixture.Expected.Length)
            {
                var reordered = fixture.Expected.ToArray();
                (reordered[i], reordered[i + 1]) = (reordered[i + 1], reordered[i]);
                Reject(new(0, reordered));
            }
        }

        if (fixture.Inputs.Length != 0)
        {
            var mutating = Substitute.For<IClockProbe>();
            mutating.Run(Arg.Any<CycleInput[]>()).Returns(call =>
            {
                var input = call.Arg<CycleInput[]>();
                input[0] = input[0] with { Before = input[0].Before ^ 1UL };
                return new TraceResult(0, fixture.Expected.ToArray());
            });
            Assert.ThrowsAny<XunitException>(() => AssertProbe(mutating, name));
        }
    }
}
