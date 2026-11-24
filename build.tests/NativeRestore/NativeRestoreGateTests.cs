using ViceSharp.Build.Testing;
using Xunit;

namespace ViceSharp.Build.Tests.NativeRestore;

/// <summary>
/// PLAN-NATIVERESTORE-001 D2: the shared ordinary selector excludes ShimLegacy and VsfSlice1Real while keeping the
/// contract category selected (R6, N08, N20), and the TRX classification behind NativeSnapshotCompatibilityTest
/// reports BLOCKED as a third outcome, never counts skipped as passing, and treats zero selected cases as
/// nonpassing (N14).
/// </summary>
public sealed class NativeRestoreGateTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "vicesharp-trx-" + Guid.NewGuid().ToString("N"));

    public NativeRestoreGateTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    /// <summary>
    /// Use case: publish fixtures only after measuring both lab hosts.
    /// Acceptance: PLAN-NATIVERESTORE-001 D5b/R17 requires two named hosts regardless of the receipts supplied.
    /// 2026-11-23: check the default and reject overrides that omit either host or repeat just one.
    /// </summary>
    [Fact]
    public void FixturePublication_RequiresBothLabHosts()
    {
        Assert.Equal(new[] { "LAB-OMARCHY", "PAYTON-LEGION2" },
            global::Build.ResolveFixtureRequiredHosts(global::Build.DefaultFixtureRequiredHosts));
        Assert.Equal(new[] { "PAYTON-LEGION2", "lab-omarchy" },
            global::Build.ResolveFixtureRequiredHosts(" PAYTON-LEGION2 , lab-omarchy,LAb-OMARCHY "));
        Assert.Throws<ArgumentException>(() => global::Build.ResolveFixtureRequiredHosts("LAB-OMARCHY,lab-omarchy"));
        Assert.Throws<ArgumentException>(() => global::Build.ResolveFixtureRequiredHosts("PAYTON-LEGION2"));
    }

    [Fact]
    public void OrdinaryTestFilter_ExcludesShimLegacyAndVsfSlice1Real_AndKeepsEveryEarlierExclusion()
    {
        var terms = global::Build.OrdinaryTestFilter.Split('&');
        Assert.Contains("Category!=ShimLegacy", terms);
        Assert.Contains("Category!=VsfSlice1Real", terms);
        Assert.Contains("Category!=Determinism", terms);
        Assert.Contains("Category!=AiReview", terms);
        Assert.Contains("Category!=ParityPending", terms);
        Assert.Contains("Category!=ParityLegacy", terms);
        Assert.Contains("Category!=Integration", terms);
        Assert.Contains("Category!=Diagnostic", terms);
        Assert.DoesNotContain(terms, t => t.Contains("VsfSlice1Contract", StringComparison.Ordinal));
        Assert.Equal(terms.Length, terms.Distinct().Count());
    }

    [Theory]
    [InlineData(true, false, "Host!=Linux", "Host!=Windows")]
    [InlineData(false, true, "Host!=Windows", "Host!=Linux")]
    public void OrdinaryTestFilter_SelectsOnlyTheCurrentHost(bool windows, bool linux, string required, string excluded)
    {
        var terms = global::Build.OrdinaryTestFilterFor(windows, linux).Split('&');
        Assert.Contains(required, terms);
        Assert.DoesNotContain(excluded, terms);
    }

    private string WriteTrx(string name, params (string Method, string Outcome, string? Message)[] cases)
    {
        var tests = string.Join("\n", cases.Select((c, i) =>
            $"<UnitTest name=\"{c.Method}\" id=\"{i + 1:D8}-0000-0000-0000-000000000000\"><TestMethod className=\"Ns.{name}\" name=\"{c.Method}\" /></UnitTest>"));
        var results = string.Join("\n", cases.Select((c, i) =>
            $"<UnitTestResult testId=\"{i + 1:D8}-0000-0000-0000-000000000000\" testName=\"{c.Method}\" outcome=\"{c.Outcome}\">"
            + (c.Message is null ? "" : $"<Output><ErrorInfo><Message>{c.Message}</Message></ErrorInfo></Output>") + "</UnitTestResult>"));
        var xml = $"<?xml version=\"1.0\" encoding=\"utf-8\"?><TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Results>{results}</Results><TestDefinitions>{tests}</TestDefinitions></TestRun>";
        var path = Path.Combine(_root, name + ".trx");
        File.WriteAllText(path, xml);
        return path;
    }

    [Fact]
    public void TrxOutcomes_ReportBlockedAsAThirdOutcome_AndNeverCountSkippedAsPassing()
    {
        var trx = WriteTrx("Vsf",
            ("Resume_Pal", "Failed", "BLOCKED: BLOCKER-R12-XVIC-CHECKPOINT trigger 100002 write clock 100007"),
            ("Resume_Ntsc", "Failed", "Assert.Equal() Failure"),
            ("Legacy_V12", "Passed", null),
            ("Later", "NotExecuted", null),
            ("Skipped_One", "NotExecuted", "skipped"));
        var summary = TrxOutcomes.Summarize([trx]);
        Assert.Equal(5, summary.Total);
        Assert.Equal(1, summary.Passed);
        Assert.Equal(1, summary.Failed);
        Assert.Equal(1, summary.Blocked);
        Assert.Equal("Ns.Vsf.Resume_Pal", Assert.Single(summary.BlockedCases).FullName);
        Assert.Contains("BLOCKER-R12-XVIC-CHECKPOINT", Assert.Single(summary.BlockedCases).Message);
        var (passing, reason) = TrxOutcomes.Evaluate(summary);
        Assert.False(passing);
        Assert.Contains("1 BLOCKED: Ns.Vsf.Resume_Pal", reason);
        Assert.Contains("1 failed: Ns.Vsf.Resume_Ntsc", reason);
        Assert.Contains("not executed", reason);
    }

    [Fact]
    public void TrxOutcomes_AllPassed_IsPassing_AndZeroSelected_IsNot()
    {
        var all = TrxOutcomes.Summarize([WriteTrx("A", ("One", "Passed", null), ("Two", "Passed", null))]);
        var (passing, reason) = TrxOutcomes.Evaluate(all);
        Assert.True(passing, reason);
        Assert.Equal("2 of 2 passed", reason);

        var none = TrxOutcomes.Summarize([WriteTrx("Empty")]);
        var (nonePassing, noneReason) = TrxOutcomes.Evaluate(none);
        Assert.False(nonePassing);
        Assert.Contains("no case selected", noneReason);

        var skipped = TrxOutcomes.Summarize([WriteTrx("S", ("One", "Passed", null), ("Two", "NotExecuted", "skip reason"))]);
        Assert.False(TrxOutcomes.Evaluate(skipped).Passing);
    }
}
