using NSubstitute;
using Xunit.Sdk;

namespace ViceSharp.NativeClockProbe.Tests;

/// <summary>
/// Mock-first assertions for the external bus observer of PLAN-OBSERVER-001 (Phase C step 3, option B).
/// FR-VICE-001 AC4, TR-VICE-ORACLE-001 AC3/AC5, TEST-VICE-001 AC4.
/// The observer patches the contents of the machine's memory table rows with trampolines that record
/// each access (kind, address, data byte, clock), withholds the bank-base fast path by returning limit 0
/// from the wrapped translate, re-patches every row only after the real table rebuild returns, re-points
/// the dummy pointers at its own rows so dummy accesses are distinguishable, and in the per-cycle hook
/// rescans the known rows: an unpatched entry or an aliased dummy pointer is a fail-closed event, never a gap.
/// Rules fixed here, implemented later by tools/immutable-vice/bus-observer.c against a VICE-shaped double.
/// </summary>
public sealed class BusObserverContractTests
{
    /// <summary>One scripted action of the VICE-shaped double: init, clock, read, write, dread, dwrite, cycle, translate, rewrite, alias, toggle.</summary>
    public sealed record BusStep(string Op, ulong A = 0, ulong B = 0);

    /// <summary>One observer or double output line: kind plus positional detail.</summary>
    public sealed record BusEvent(string Kind, string Detail);

    public sealed record TraceResult(int ExitCode, BusEvent[] Events);
    public sealed record Fixture(BusStep[] Steps, BusEvent[] Expected);

    /// <summary>Same seam and assertions for mocks and the later compiled C fixture.</summary>
    public interface IBusObserverProbe
    {
        TraceResult Run(BusStep[] steps);
    }

    // Double's stock read returns (address and 0xff) xor 0xa5; the observer never invents data.
    private static readonly BusEvent InitOk = new("init", "0");
    private static readonly BusEvent InitPatch = new("repatch", "514,1");

    /// <summary>Literal expectations precede the wrapper implementation.</summary>
    public static Fixture GetFixture(string name) => name switch
    {
        "empty" => new([], []),
        "init-only" => new([new("init")], [InitOk, InitPatch]),
        "read" => new(
            [new("init"), new("clock", 5), new("read", 0x1234), new("cycle")],
            [InitOk, InitPatch, new("access", "read,4660,145,5"), new("cycle", "5")]),
        "fetch" => new(
            [new("init"), new("clock", 10), new("read", 0x0800), new("cycle"),
             new("clock", 11), new("read", 0x0801), new("cycle"),
             new("clock", 12), new("read", 0x0802), new("cycle")],
            [InitOk, InitPatch,
             new("access", "read,2048,165,10"), new("cycle", "10"),
             new("access", "read,2049,164,11"), new("cycle", "11"),
             new("access", "read,2050,167,12"), new("cycle", "12")]),
        "write-and-dummy" => new(
            [new("init"), new("write", 0xd020, 7), new("cycle"),
             new("dread", 0x0100), new("cycle"),
             new("dwrite", 0x0101, 66), new("cycle")],
            [InitOk, InitPatch,
             new("access", "write,53280,7,0"), new("cycle", "0"),
             new("access", "dummy-read,256,165,0"), new("cycle", "0"),
             new("access", "dummy-write,257,66,0"), new("cycle", "0")]),
        "translate" => new(
            [new("init"), new("translate", 0x1234)],
            [InitOk, InitPatch, new("translate", "0,0")]),
        "rebuild-at-runtime" => new(
            [new("init"), new("read", 0x0100), new("cycle"), new("init"), new("read", 0x0100), new("cycle")],
            [InitOk, InitPatch, new("access", "read,256,165,0"), new("cycle", "0"),
             InitOk, InitPatch, new("access", "read,256,165,0"), new("cycle", "0")]),
        "rewrite-outside-init" => new(
            [new("init"), new("rewrite", 0x12), new("read", 0x1234), new("cycle"), new("read", 0x1234), new("cycle")],
            [InitOk, InitPatch,
             new("missing", "read,18"), new("repatch", "1,0"), new("cycle", "0"),
             new("access", "read,4660,145,0"), new("cycle", "0")]),
        "dummy-alias" => new(
            [new("init"), new("alias"), new("read", 0x0100), new("cycle"), new("read", 0x0100), new("cycle")],
            [InitOk, InitPatch,
             new("access", "missing,256,165,0"), new("repatch", "0,1"), new("cycle", "0"),
             new("access", "read,256,165,0"), new("cycle", "0")]),
        "watch-toggle" => new(
            [new("init"), new("toggle", 1), new("read", 0x0100), new("cycle"),
             new("toggle", 0), new("read", 0x0100), new("cycle"), new("read", 0x0100), new("cycle")],
            [InitOk, InitPatch,
             new("access", "missing,256,165,0"), new("foreign", "read"), new("foreign", "write"), new("repatch", "0,1"), new("cycle", "0"),
             new("access", "missing,256,165,0"), new("repatch", "0,1"), new("cycle", "0"),
             new("access", "read,256,165,0"), new("cycle", "0")]),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    public static readonly string[] FixtureNames =
    [
        "empty", "init-only", "read", "fetch", "write-and-dummy", "translate",
        "rebuild-at-runtime", "rewrite-outside-init", "dummy-alias", "watch-toggle"
    ];

    /// <summary>Exact event order proves capture, fail-closed rescan, re-patch timing and preserved input.</summary>
    internal static void AssertProbe(IBusObserverProbe probe, string name)
    {
        var fixture = GetFixture(name);
        var input = fixture.Steps.ToArray();
        var actual = probe.Run(input);
        Assert.NotNull(actual);
        Assert.Equal(0, actual.ExitCode);
        Assert.Equal(fixture.Expected, actual.Events);
        Assert.Equal(fixture.Steps, input);
    }

    [Fact]
    public void Fixture_names_cover_every_rule_once()
    {
        Assert.Equal(FixtureNames.Length, FixtureNames.Distinct().Count());
        foreach (var name in FixtureNames) Assert.NotNull(GetFixture(name));
        Assert.Throws<ArgumentOutOfRangeException>(() => GetFixture("no-such-fixture"));
        var kinds = FixtureNames.SelectMany(n => GetFixture(n).Expected).Select(e => e.Kind).Distinct().Order().ToArray();
        Assert.Equal(["access", "cycle", "foreign", "init", "missing", "repatch", "translate"], kinds);
    }

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
    public void Correct_mock_passes_and_every_wrong_mock_is_rejected(string name)
    {
        var fixture = GetFixture(name);
        var correct = Substitute.For<IBusObserverProbe>();
        correct.Run(Arg.Any<BusStep[]>()).Returns(new TraceResult(0, fixture.Expected.ToArray()));
        AssertProbe(correct, name);
        correct.Received(1).Run(Arg.Any<BusStep[]>());

        void Reject(TraceResult result)
        {
            var wrong = Substitute.For<IBusObserverProbe>();
            wrong.Run(Arg.Any<BusStep[]>()).Returns(result);
            Assert.ThrowsAny<XunitException>(() => AssertProbe(wrong, name));
        }

        Reject(new(1, fixture.Expected));
        Reject(new(0, null!));
        Reject(null!);
        Reject(new(0, [.. fixture.Expected, new("access", "read,0,0,0")]));
        for (var i = 0; i < fixture.Expected.Length; i++)
        {
            var wrongKind = fixture.Expected.ToArray();
            wrongKind[i] = wrongKind[i] with { Kind = "wrong-kind" };
            Reject(new(0, wrongKind));
            var wrongDetail = fixture.Expected.ToArray();
            wrongDetail[i] = wrongDetail[i] with { Detail = wrongDetail[i].Detail + ",1" };
            Reject(new(0, wrongDetail));
            Reject(new(0, fixture.Expected.Where((_, index) => index != i).ToArray()));
            Reject(new(0, [.. fixture.Expected.Take(i), fixture.Expected[i], .. fixture.Expected.Skip(i)]));
            if (i + 1 < fixture.Expected.Length && fixture.Expected[i] != fixture.Expected[i + 1])
            {
                var reordered = fixture.Expected.ToArray();
                (reordered[i], reordered[i + 1]) = (reordered[i + 1], reordered[i]);
                Reject(new(0, reordered));
            }
        }

        if (fixture.Steps.Length != 0)
        {
            var mutating = Substitute.For<IBusObserverProbe>();
            mutating.Run(Arg.Any<BusStep[]>()).Returns(call =>
            {
                var input = call.Arg<BusStep[]>();
                input[0] = input[0] with { A = input[0].A ^ 1UL };
                return new TraceResult(0, fixture.Expected.ToArray());
            });
            Assert.ThrowsAny<XunitException>(() => AssertProbe(mutating, name));
        }
    }

    /// <summary>The fail-closed rules are literal in the fixtures: a lenient variant of each must be rejected.</summary>
    [Fact]
    public void Lenient_variants_of_the_fail_closed_rules_are_rejected()
    {
        void RejectVariant(string name, Func<BusEvent[], BusEvent[]> variant)
        {
            var lenient = Substitute.For<IBusObserverProbe>();
            lenient.Run(Arg.Any<BusStep[]>()).Returns(new TraceResult(0, variant(GetFixture(name).Expected.ToArray())));
            Assert.ThrowsAny<XunitException>(() => AssertProbe(lenient, name));
        }

        // Translate must withhold the fast path: a nonzero limit or a non-null base is wrong.
        RejectVariant("translate", e => e.Select(x => x.Kind == "translate" ? x with { Detail = "8189,1" } : x).ToArray());
        RejectVariant("translate", e => e.Select(x => x.Kind == "translate" ? x with { Detail = "0,1" } : x).ToArray());
        // An untraced access must surface as missing, not be silently dropped.
        RejectVariant("rewrite-outside-init", e => e.Where(x => x.Kind != "missing").ToArray());
        // An aliased dummy pointer must not be reported as a plain read.
        RejectVariant("dummy-alias", e => e.Select(x => x.Detail == "missing,256,165,0" ? x with { Detail = "read,256,165,0" } : x).ToArray());
        // Trampolines present during the real rebuild would be reported by the double as a nonzero init count.
        RejectVariant("rebuild-at-runtime", e => e.Select((x, i) => i == 4 ? x with { Detail = "514" } : x).ToArray());
        // A foreign live row must be reported every cycle it is live.
        RejectVariant("watch-toggle", e => e.Where(x => x.Kind != "foreign").ToArray());
    }
}
