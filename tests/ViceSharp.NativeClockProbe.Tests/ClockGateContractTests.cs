using NSubstitute;
using Xunit.Sdk;

namespace ViceSharp.NativeClockProbe.Tests;

/// <summary>
/// Mock-first fail-closed external cycle permission contract. See GATE-SLICE.md.
/// FR-VICE-001 AC2/AC4; TR-VICE-ORACLE-001 AC2/AC3/AC5; TEST-VICE-001 AC2/AC4.
/// Correct and deliberately wrong providers exercise identical result/order assertions.
/// </summary>
public sealed class ClockGateContractTests
{
    public sealed record GateInput(ulong Clock, int Publish, int Flush, int Receive, int Missing = 0);
    public sealed record GateEvent(string Kind, ulong Value);
    public sealed record GateResult(int Status, GateEvent[] Events);
    public sealed record GateFixture(GateInput Input, int Status, GateEvent[] Events);
    public interface IGateProbe { GateResult Run(GateInput input); }

    public static IEnumerable<object[]> Cases => new[]
    {
        "zero", "maximum", "clock", "publish-negative", "publish-positive", "flush-negative",
        "flush-positive", "eof", "input-error", "quit", "uppercase", "newline", "nul",
        "out-of-range", "other-negative", "null-io", "missing-publish", "missing-flush", "missing-read"
    }.Select(name => new object[] { name });

    /// <summary>Expected values are literal contract fixtures, not implementation-derived.</summary>
    public static GateFixture GetFixture(string name) => name switch
    {
        "zero" => new(new(0, 0, 0, 99), 0, [new("publish", 0), new("flush", 0), new("receive", 0)]),
        "maximum" => new(new(ulong.MaxValue, 0, 0, 99), 0,
            [new("publish", ulong.MaxValue), new("flush", 0), new("receive", 0)]),
        "clock" => new(new(123456789, 0, 0, 99), 0,
            [new("publish", 123456789), new("flush", 0), new("receive", 0)]),
        "publish-negative" => new(new(7, -1, 0, 99), 64, [new("publish", 7)]),
        "publish-positive" => new(new(7, 1, 0, 99), 64, [new("publish", 7)]),
        "flush-negative" => new(new(7, 0, -1, 99), 65, [new("publish", 7), new("flush", 0)]),
        "flush-positive" => new(new(7, 0, 1, 99), 65, [new("publish", 7), new("flush", 0)]),
        "eof" => new(new(7, 0, 0, -1), 66, [new("publish", 7), new("flush", 0), new("receive", 0)]),
        "input-error" => new(new(7, 0, 0, -2), 70, [new("publish", 7), new("flush", 0), new("receive", 0)]),
        "quit" => new(new(7, 0, 0, 113), 67, [new("publish", 7), new("flush", 0), new("receive", 0)]),
        "uppercase" => new(new(7, 0, 0, 67), 68, [new("publish", 7), new("flush", 0), new("receive", 0)]),
        "newline" => new(new(7, 0, 0, 10), 68, [new("publish", 7), new("flush", 0), new("receive", 0)]),
        "nul" => new(new(7, 0, 0, 0), 68, [new("publish", 7), new("flush", 0), new("receive", 0)]),
        "out-of-range" => new(new(7, 0, 0, 256), 68, [new("publish", 7), new("flush", 0), new("receive", 0)]),
        "other-negative" => new(new(7, 0, 0, -3), 68, [new("publish", 7), new("flush", 0), new("receive", 0)]),
        "null-io" => new(new(7, 0, 0, 99, 1), 69, []),
        "missing-publish" => new(new(7, 0, 0, 99, 2), 69, []),
        "missing-flush" => new(new(7, 0, 0, 99, 3), 69, []),
        "missing-read" => new(new(7, 0, 0, 99, 4), 69, []),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    internal static void AssertGate(IGateProbe probe, string name)
    {
        var fixture = GetFixture(name);
        var actual = probe.Run(fixture.Input);
        Assert.NotNull(actual);
        Assert.Equal(fixture.Status, actual.Status);
        Assert.Equal(fixture.Events, actual.Events);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Correct_mock_passes_and_every_wrong_gate_result_is_rejected(string name)
    {
        var fixture = GetFixture(name);
        var correct = Substitute.For<IGateProbe>();
        correct.Run(fixture.Input).Returns(new GateResult(fixture.Status, fixture.Events));
        AssertGate(correct, name);
        correct.Received(1).Run(fixture.Input);

        void Reject(GateResult result)
        {
            var wrong = Substitute.For<IGateProbe>();
            wrong.Run(fixture.Input).Returns(result);
            Assert.ThrowsAny<XunitException>(() => AssertGate(wrong, name));
        }

        Reject(null!);
        Reject(new(fixture.Status ^ 1, fixture.Events));
        Reject(new(fixture.Status, null!));
        Reject(new(fixture.Status, [.. fixture.Events, new("unexpected", 0)]));
        for (var i = 0; i < fixture.Events.Length; i++)
        {
            var changed = fixture.Events.ToArray();
            changed[i] = changed[i] with { Value = changed[i].Value ^ 1UL };
            Reject(new(fixture.Status, changed));
            changed = fixture.Events.ToArray();
            changed[i] = changed[i] with { Kind = "wrong-phase" };
            Reject(new(fixture.Status, changed));
            Reject(new(fixture.Status, fixture.Events.Where((_, index) => index != i).ToArray()));
            Reject(new(fixture.Status, [.. fixture.Events.Take(i), fixture.Events[i], .. fixture.Events.Skip(i)]));
            if (i + 1 < fixture.Events.Length)
            {
                changed = fixture.Events.ToArray();
                (changed[i], changed[i + 1]) = (changed[i + 1], changed[i]);
                Reject(new(fixture.Status, changed));
            }
        }
    }
}
