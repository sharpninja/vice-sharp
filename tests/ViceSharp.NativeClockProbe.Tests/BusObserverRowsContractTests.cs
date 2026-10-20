using NSubstitute;
using Xunit.Sdk;
using static ViceSharp.NativeClockProbe.Tests.BusObserverContractTests;

namespace ViceSharp.NativeClockProbe.Tests;

/// <summary>
/// Mock-first assertions for PLAN-OBSERVER-001 slice S1b: the x64sc table layout, where the 32
/// configuration rows are static arrays of 0x101 entries (c64memsc.c 110 to 111), the current row is
/// mem_read_tab[mem_config] (169 to 184), mem_get_current_bank_config() is global (222), the watch
/// functions delegate to the current row (150 to 162) and a banking change re-points all four pointers
/// intra-object. The observer reaches every row from the current row pointer by stride 0x101, proves
/// the layout once through the global mem_read_tab_set (fail closed: "layout,0" and only the live row
/// is patched), selects the saved original by the current configuration, and rescans the current row
/// each cycle. FR-VICE-001 AC4, TR-VICE-ORACLE-001 AC3/AC5, TEST-VICE-001 AC4.
/// </summary>
public sealed class BusObserverRowsContractTests
{
    private const int Rows = 32;
    private static readonly BusEvent InitOk = new("init", "0");
    private static readonly BusEvent Layout = new("layout", "32");
    private static readonly BusEvent InitPatch = new("repatch", (Rows * 514) + ",1");

    /// <summary>The single-row fixtures hold under the 32-row layout with the layout event and the larger patch count.</summary>
    private static BusEvent[] Adapt(BusEvent[] single) =>
        single.SelectMany(e => e == new BusEvent("repatch", "514,1") ? new[] { Layout, InitPatch } : [e]).ToArray();

    public static Fixture GetFixture(string name) => name switch
    {
        "bank-switch" => new(
            [new("init"), new("bank", 7), new("read", 0x1234), new("cycle"), new("read", 0x1234), new("cycle")],
            [InitOk, Layout, InitPatch,
             new("access", "missing,4660,145,0"), new("repatch", "0,1"), new("cycle", "0"),
             new("access", "read,4660,145,0"), new("cycle", "0")]),
        "rewrite-noncurrent-row" => new(
            [new("init"), new("rewriterow", 3, 0x12), new("bank", 3), new("read", 0x1234), new("cycle"), new("read", 0x1234), new("cycle")],
            [InitOk, Layout, InitPatch,
             new("missing", "read,18"), new("repatch", "1,1"), new("cycle", "0"),
             new("access", "read,4660,145,0"), new("cycle", "0")]),
        "bank-then-rebuild" => new(
            [new("init"), new("bank", 5), new("cycle"), new("init"), new("read", 0x0100), new("cycle")],
            [InitOk, Layout, InitPatch,
             new("repatch", "0,1"), new("cycle", "0"),
             InitOk, Layout, InitPatch,
             new("access", "read,256,165,0"), new("cycle", "0")]),
        _ => new(BusObserverContractTests.GetFixture(name).Steps, Adapt(BusObserverContractTests.GetFixture(name).Expected))
    };

    public static readonly string[] FixtureNames =
    [
        .. BusObserverContractTests.FixtureNames, "bank-switch", "rewrite-noncurrent-row", "bank-then-rebuild"
    ];

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
    public void Fixture_names_cover_the_single_row_rules_plus_the_row_rules()
    {
        Assert.Equal(17, FixtureNames.Length);
        Assert.Equal(FixtureNames.Length, FixtureNames.Distinct().Count());
        foreach (var name in FixtureNames) Assert.NotNull(GetFixture(name));
        var kinds = FixtureNames.SelectMany(n => GetFixture(n).Expected).Select(e => e.Kind).Distinct().Order().ToArray();
        Assert.Equal(["access", "cycle", "foreign", "init", "layout", "missing", "repatch", "translate", "watch"], kinds);
        Assert.DoesNotContain(new BusEvent("repatch", "514,1"), FixtureNames.SelectMany(n => GetFixture(n).Expected));
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
    [InlineData("watch-flag1-after-rebuild")]
    [InlineData("foreign-row")]
    [InlineData("translate-keep")]
    [InlineData("bank-by-store")]
    [InlineData("bank-switch")]
    [InlineData("rewrite-noncurrent-row")]
    [InlineData("bank-then-rebuild")]
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
        Reject(new(0, [.. fixture.Expected, new("layout", "0")]));
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
    }

    [Fact]
    public void Lenient_variants_of_the_row_rules_are_rejected()
    {
        void RejectVariant(string name, Func<BusEvent[], BusEvent[]> variant)
        {
            var lenient = Substitute.For<IBusObserverProbe>();
            lenient.Run(Arg.Any<BusStep[]>()).Returns(new TraceResult(0, variant(GetFixture(name).Expected.ToArray())));
            Assert.ThrowsAny<XunitException>(() => AssertProbe(lenient, name));
        }

        // A failed layout self-test must not be reported as success.
        RejectVariant("init-only", e => e.Select(x => x.Kind == "layout" ? x with { Detail = "0" } : x).ToArray());
        // Patching only the live row (514 entries) is not the 32-row contract.
        RejectVariant("init-only", e => e.Select(x => x.Kind == "repatch" ? x with { Detail = "514,1" } : x).ToArray());
        // After a banking change the aliased dummy pointers must not yield a plain read.
        RejectVariant("bank-switch", e => e.Select(x => x.Detail == "missing,4660,145,0" ? x with { Detail = "read,4660,145,0" } : x).ToArray());
        // A row rewritten while not current must surface as missing once it is current.
        RejectVariant("rewrite-noncurrent-row", e => e.Where(x => x.Kind != "missing").ToArray());
        // A banking change to a patched row is not foreign.
        RejectVariant("bank-switch", e => [.. e.Take(3), new("foreign", "read"), .. e.Skip(3)]);
    }
}
