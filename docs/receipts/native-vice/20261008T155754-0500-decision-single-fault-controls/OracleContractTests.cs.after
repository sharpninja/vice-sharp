using NSubstitute;
using static ViceSharp.OracleContract.Tests.ContractAssertions;

namespace ViceSharp.OracleContract.Tests;

/// <summary>FR-VICE-001 AC1/AC3; TR-VICE-ORACLE-001 AC1; TEST-VICE-001 AC1.</summary>
public sealed class ImmutableViceOraclePreflightTests
{
    /// <summary>Known inputs have independently declared expected decisions and wrong-result controls.</summary>
    [Theory]
    [MemberData(nameof(OracleContractFixtures.IdentityCases), MemberType = typeof(OracleContractFixtures))]
    public void IdentityAndInventory(string scenario, bool accepted, string? error)
    {
        ProveDecision(OracleContractFixtures.IdentityInput(scenario), new Decision(accepted, error));
    }

    /// <summary>Observed adapter call transcript; used instead of implementing an orchestrator.</summary>
    public interface IRunAudit
    {
        /// <summary>Record the preflight decision before any runner call.</summary>
        void Preflight(bool accepted);
        /// <summary>Record the parity-runner invocation.</summary>
        void Run();
        /// <summary>Record whether post-run inventory still matches.</summary>
        void Postflight(bool unchanged);
        /// <summary>Record final receipt acceptance.</summary>
        void Receipt(bool accepted);
    }

    /// <summary>Rejected preflight cannot run; changed postflight invalidates an otherwise accepted run.</summary>
    [Theory]
    [MemberData(nameof(OracleContractFixtures.LifecycleCases), MemberType = typeof(OracleContractFixtures))]
    public void OrderedLifecycle(bool preflight, bool postflight)
    {
        var audit = Substitute.For<IRunAudit>();
        audit.Preflight(preflight);
        if (preflight) { audit.Run(); audit.Postflight(postflight); }
        audit.Receipt(preflight && postflight);
        AssertTranscript(audit, preflight, postflight);
        // Mutation: an additional runner call is forbidden, including after rejection.
        audit.Run();
        Rejects(() => AssertTranscript(audit, preflight, postflight));

        audit.ClearReceivedCalls();
        if (preflight) audit.Run(); // Mutation: run before acceptance is recorded.
        audit.Preflight(preflight);
        if (preflight) audit.Postflight(postflight);
        audit.Receipt(!(preflight && postflight));
        Rejects(() => AssertTranscript(audit, preflight, postflight));
    }

    /// <summary>The ordering assertion rejects early runner even if final receipt is otherwise correct.</summary>
    [Fact]
    public void RunnerBeforeRecordedAcceptanceIsRejectedIndependently()
    {
        var audit = Substitute.For<IRunAudit>();
        audit.Run(); audit.Preflight(true); audit.Postflight(true); audit.Receipt(true);
        Rejects(() => AssertTranscript(audit, true, true));
    }

    /// <summary>F-07: correct ordering cannot hide an inverted final receipt after either postflight result.</summary>
    [Theory]
    [MemberData(nameof(OracleContractFixtures.PostflightCases), MemberType = typeof(OracleContractFixtures))]
    public void WrongReceiptAfterPostflightIsRejectedIndependently(bool unchanged)
    {
        var audit = Substitute.For<IRunAudit>();
        audit.Preflight(true); audit.Run(); audit.Postflight(unchanged); audit.Receipt(unchanged);
        AssertTranscript(audit, true, unchanged);

        audit.ClearReceivedCalls();
        audit.Preflight(true); audit.Run(); audit.Postflight(unchanged); audit.Receipt(!unchanged);
        Rejects(() => AssertTranscript(audit, true, unchanged));
    }

    /// <summary>F-07: an independently corrupted postflight argument is rejected even when the receipt is correct.</summary>
    [Theory]
    [MemberData(nameof(OracleContractFixtures.PostflightCases), MemberType = typeof(OracleContractFixtures))]
    public void WrongPostflightArgumentIsRejectedIndependently(bool unchanged)
    {
        var audit = Substitute.For<IRunAudit>();
        audit.Preflight(true); audit.Run(); audit.Postflight(unchanged); audit.Receipt(unchanged);
        AssertTranscript(audit, true, unchanged);

        audit.ClearReceivedCalls();
        audit.Preflight(true); audit.Run(); audit.Postflight(!unchanged); audit.Receipt(unchanged);
        Rejects(() => AssertTranscript(audit, true, unchanged));
    }
}

/// <summary>FR-VICE-001 AC2; TR-VICE-ORACLE-001 AC2; TEST-VICE-001 AC2.</summary>
public sealed class ImmutableViceWrapperBoundaryTests
{
    /// <summary>Reject forbidden destinations even when spelling or a link hides the native root.</summary>
    [Theory]
    [MemberData(nameof(OracleContractFixtures.BoundaryCases), MemberType = typeof(OracleContractFixtures))]
    public void DestinationAndAfterState(string operation, string requested, string resolved,
        bool unchanged, bool accepted, string? error) =>
        ProveBoundary(new BoundaryRequest(operation, requested, unchanged),
            new BoundaryObservation(resolved, new Decision(accepted, error)));
}

/// <summary>FR-VICE-001 AC1/AC3; TR-VICE-ORACLE-001 AC4; TEST-VICE-001 AC3.</summary>
public sealed class ImmutableViceReceiptContractTests
{
    /// <summary>Test-local acceptance receipt fields; no live or native claims.</summary>
    public sealed record Receipt(OracleState Oracle, string? WrapperHash, string? WrapperVersion,
        string? Command, string? Scope, int Passed, int Failed, int Skipped, int Aborted);

    /// <summary>Every invalid receipt gets a wrong accepting result rejected by the same assertion.</summary>
    [Theory]
    [MemberData(nameof(OracleContractFixtures.ReceiptCases), MemberType = typeof(OracleContractFixtures))]
    public void RequiredProvenanceAndCounts(string scenario, bool accepted, string? error)
    {
        ProveDecision(OracleContractFixtures.ReceiptInput(scenario), new Decision(accepted, error));
    }
}

/// <summary>FR-VICE-001 AC4; TR-VICE-ORACLE-001 AC3/AC5; TEST-VICE-001 AC4.</summary>
public sealed class Vic20NativeBusEventTests
{
    /// <summary>Synthetic category capability and per-half-cycle native observation contract.</summary>
    public sealed record Stream(string Category, bool Capability, string Provenance,
        long ExpectedFirst, long ExpectedLast, long[] ObservedHalfCycles);

    /// <summary>All four categories independently exercise every provenance and observation defect.</summary>
    [Theory]
    [MemberData(nameof(OracleContractFixtures.ObservationCategories), MemberType = typeof(OracleContractFixtures))]
    public void EveryCategoryRequiresActualCompleteObservations(string category)
    {
        foreach (var fixture in OracleContractFixtures.ObservationCases(category))
            ProveDecision(fixture.Input, fixture.Expected);
    }
}

/// <summary>Partial FR-SNP-005 AC3 / TEST-SNP-002 AC4. Not TR-CYCLE-001 AC4 or real VSF proof.</summary>
public sealed class Vic20HalfCycleComparatorContractTests
{
    /// <summary>Equal CPU registers do not hide an actual bus-address mismatch.</summary>
    public sealed record Registers(ushort Pc, byte A, byte X, byte Y, byte Sp, byte Flags);
    /// <summary>Test-local observation, preserving the original half-cycle37 fixture.</summary>
    public sealed record BusSample(long HalfCycle, Registers Cpu, ushort Address);
    /// <summary>Only the result DTO is mocked; no comparison algorithm is implemented here.</summary>
    public sealed record Mismatch(long HalfCycle, string Field, ushort Expected, ushort Actual);
    /// <summary>Future adapter seam to the single production LockstepValidator.</summary>
    public interface IComparator { Mismatch? Compare(BusSample native, BusSample managed); }

    /// <summary>Correct result passes; each single-field corruption is independently detected.</summary>
    [Fact]
    public void EqualRegistersDifferentAddressAtHalfCycle37()
    {
        var (native, managed, correct) = OracleContractFixtures.ComparatorCase;
        Assert.Equal(native.Cpu, managed.Cpu);
        var comparator = Substitute.For<IComparator>();
        comparator.Compare(native, managed).Returns(correct);
        AssertComparator(comparator, native, managed, correct);
        foreach (var wrong in new Mismatch?[]
        {
            null, correct with { HalfCycle = 38 }, correct with { Field = "Pc" },
            correct with { Expected = 0x1001 }, correct with { Actual = 0x1000 }
        })
        {
            comparator.Compare(native, managed).Returns(wrong);
            Rejects(() => AssertComparator(comparator, native, managed, correct));
        }
    }
}

/// <summary>TR-CYCLE-001 AC4; missing observations also FR-VICE-001 AC4 / TEST-VICE-001 AC4.</summary>
public sealed class CpuPhi2AecContractTests
{
    /// <summary>Explicit phase. Undefined numeric values are distinct from valid phases.</summary>
    public enum Phase { Phi1 = 1, Phi2 = 2 }
    /// <summary>Explicit idle is valid; null is unavailable evidence, not idle.</summary>
    public enum Access { None = 0, Read = 1, Write = 2 }
    /// <summary>Synthetic timing evidence, not an actual x64sc or managed trace.</summary>
    public sealed record Observation(long HalfCycle, Phase? Phase, bool? Aec, Access? Access);
    /// <summary>Caller must specify the entire nonempty expected interval.</summary>
    public sealed record Sequence(long? First, long? Last, Observation[] Observations);

    /// <summary>All legal idle/access combinations and all forbidden accesses are explicit cases.</summary>
    [Theory]
    [MemberData(nameof(OracleContractFixtures.PhaseCases), MemberType = typeof(OracleContractFixtures))]
    public void PhaseAecAccessTruthTable(int? phase, bool? aec, int? access, bool accepted, string? error)
    {
        ProveDecision(OracleContractFixtures.PhaseInput(phase, aec, access),
            new Decision(accepted, error, accepted ? null : 37));
    }

    /// <summary>Gap positions are explicitly stated, never derived by a second timing algorithm.</summary>
    [Theory]
    [MemberData(nameof(OracleContractFixtures.IntervalCases), MemberType = typeof(OracleContractFixtures))]
    public void IntervalMustBeCompleteAndOrdered(string scenario, bool accepted, string? error, long? firstError)
    {
        ProveDecision(OracleContractFixtures.IntervalInput(scenario), new Decision(accepted, error, firstError));
    }
}
