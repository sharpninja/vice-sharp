using NSubstitute;
using Xunit.Sdk;
using static ViceSharp.OracleContract.Tests.ImmutableViceOraclePreflightTests;
using static ViceSharp.OracleContract.Tests.Vic20HalfCycleComparatorContractTests;

namespace ViceSharp.OracleContract.Tests;

/// <summary>Test-only response contract. Null is not an inferred observation.</summary>
public sealed record Decision(bool Accepted, string? Error = null, long? HalfCycle = null);

/// <summary>Substitution seam for future real adapters; no production implementation exists here.</summary>
public interface IContractProbe<in T>
{
    /// <summary>Returns the adapter's reported acceptance or exact failure.</summary>
    Decision Evaluate(T input);
}

/// <summary>Unresolved request. A real provider must compute the destination through its filesystem boundary.</summary>
public sealed record BoundaryRequest(string Operation, string RequestedPath, bool InventoryUnchanged);

/// <summary>The resolved destination is observed output, never trusted caller input.</summary>
public sealed record BoundaryObservation(string ResolvedPath, Decision Decision);

/// <summary>Shared mock/real-adapter seam for destination resolution and immutable-target decisions.</summary>
public interface IBoundaryProbe
{
    /// <summary>Resolve the requested destination and return the observed path and decision.</summary>
    BoundaryObservation Evaluate(BoundaryRequest input);
}

/// <summary>Assertions shared by correct mocks, wrong mocks, and future real adapters.</summary>
public static class ContractAssertions
{
    /// <summary>Requires all decision fields, including the exact first failing position.</summary>
    public static void DecisionIs(Decision expected, Decision actual)
    {
        Assert.Equal(expected.Accepted, actual.Accepted);
        Assert.Equal(expected.Error, actual.Error);
        Assert.Equal(expected.HalfCycle, actual.HalfCycle);
    }

    /// <summary>Exercise the supplied provider; expected results are independent fixture data.</summary>
    public static void AssertProbe<T>(IContractProbe<T> probe, T input, Decision expected) =>
        DecisionIs(expected, probe.Evaluate(input));

    /// <summary>Shared transcript assertions for a mocked audit dependency of either mock or real orchestration.</summary>
    public static void AssertTranscript(IRunAudit audit, bool preflight, bool postflight)
    {
        var calls = audit.ReceivedCalls().ToArray();
        var expectedNames = preflight
            ? new[] { "Preflight", "Run", "Postflight", "Receipt" }
            : new[] { "Preflight", "Receipt" };
        Assert.Equal(expectedNames, calls.Select(c => c.GetMethodInfo().Name));
        Assert.Equal(preflight, Assert.IsType<bool>(calls[0].GetArguments()[0]));
        if (preflight)
            Assert.Equal(postflight, Assert.IsType<bool>(calls[2].GetArguments()[0]));
        Assert.Equal(preflight && postflight, Assert.IsType<bool>(calls[^1].GetArguments()[0]));
    }

    /// <summary>Exercise the supplied comparator against independent fixture expectations without a second algorithm.</summary>
    public static void AssertComparator(IComparator comparator, BusSample native, BusSample managed, Mismatch expected)
    {
        var actual = comparator.Compare(native, managed);
        Assert.NotNull(actual);
        Assert.Equal(expected.HalfCycle, actual.HalfCycle);
        Assert.Equal(expected.Field, actual.Field);
        Assert.Equal(expected.Expected, actual.Expected);
        Assert.Equal(expected.Actual, actual.Actual);
    }

    /// <summary>Observe the supplied resolver without passing its expected resolved path as input.</summary>
    public static void AssertBoundary(IBoundaryProbe probe, BoundaryRequest input, BoundaryObservation expected)
    {
        var actual = probe.Evaluate(input);
        Assert.NotNull(actual);
        Assert.Equal(expected.ResolvedPath, actual.ResolvedPath);
        DecisionIs(expected.Decision, actual.Decision);
    }

    /// <summary>Correct boundary output and each independent path/decision corruption use the same assertion.</summary>
    public static void ProveBoundary(BoundaryRequest input, BoundaryObservation expected)
    {
        var probe = Substitute.For<IBoundaryProbe>();
        probe.Evaluate(input).Returns(expected);
        AssertBoundary(probe, input, expected);
        probe.Received(1).Evaluate(input);
        foreach (var wrong in new[]
        {
            expected with { ResolvedPath = expected.ResolvedPath + ".wrong" },
            expected with { Decision = expected.Decision with { Accepted = !expected.Decision.Accepted } },
            expected with { Decision = expected.Decision with { Error = "WrongReason" } },
            expected with { Decision = expected.Decision with { HalfCycle = (expected.Decision.HalfCycle ?? 0) + 1 } }
        })
        {
            probe.Evaluate(input).Returns(wrong);
            Rejects(() => AssertBoundary(probe, input, expected));
        }
    }

    /// <summary>
    /// Exercises a fixture's independent expected decision. Negative controls are passing
    /// tests of these same assertions, not intentionally failing production tests.
    /// </summary>
    public static void ProveDecision<T>(T input, Decision expected)
    {
        var probe = Substitute.For<IContractProbe<T>>();
        probe.Evaluate(input).Returns(expected);
        AssertProbe(probe, input, expected);
        probe.Received(1).Evaluate(input);

        // Change only acceptance so a reason or cycle mismatch cannot mask a
        // missing acceptance assertion. This covers rejection and legal idle.
        probe.Evaluate(input).Returns(expected with { Accepted = !expected.Accepted });
        Rejects(() => AssertProbe(probe, input, expected));

        // Catch assertions that accidentally ignore the reason or exact cycle.
        probe.Evaluate(input).Returns(expected with { Error = "WrongReason" });
        Rejects(() => AssertProbe(probe, input, expected));
        probe.Evaluate(input).Returns(expected with { HalfCycle = (expected.HalfCycle ?? 0) + 1 });
        Rejects(() => AssertProbe(probe, input, expected));
    }

    /// <summary>Only assertion failures count as rejection, not arbitrary application errors.</summary>
    public static void Rejects(Action assertion) => Assert.ThrowsAny<XunitException>(assertion);
}

/// <summary>Synthetic oracle identity and inventory; never a real native acceptance receipt.</summary>
public sealed record OracleState(string? Commit, bool CleanBefore, bool CleanAfter,
    string BeforeInventory, string AfterInventory, string Source, bool Patched);

/// <summary>Fixture constants are expectations, not live environment discovery.</summary>
public static class Fixtures
{
    /// <summary>Pinned upstream reference from the approved immutable plan.</summary>
    public const string Commit = "cc67418cba58e71d8e70fd353e9de47199ad2075";
    /// <summary>Synthetic hash-shaped fixture value, not a claimed file hash.</summary>
    public static readonly string Hash = new('a', 64);
    /// <summary>Valid synthetic immutable-target state.</summary>
    public static OracleState Clean => new(Commit, true, true, Hash, Hash, "upstream-vice", false);
}
