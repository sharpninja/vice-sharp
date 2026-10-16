using Receipt = ViceSharp.OracleContract.Tests.ImmutableViceReceiptContractTests.Receipt;
using NativeStream = ViceSharp.OracleContract.Tests.Vic20NativeBusEventTests.Stream;
using BusSample = ViceSharp.OracleContract.Tests.Vic20HalfCycleComparatorContractTests.BusSample;
using Registers = ViceSharp.OracleContract.Tests.Vic20HalfCycleComparatorContractTests.Registers;
using Mismatch = ViceSharp.OracleContract.Tests.Vic20HalfCycleComparatorContractTests.Mismatch;
using Phase = ViceSharp.OracleContract.Tests.CpuPhi2AecContractTests.Phase;
using Access = ViceSharp.OracleContract.Tests.CpuPhi2AecContractTests.Access;
using Observation = ViceSharp.OracleContract.Tests.CpuPhi2AecContractTests.Observation;
using Sequence = ViceSharp.OracleContract.Tests.CpuPhi2AecContractTests.Sequence;

namespace ViceSharp.OracleContract.Tests;

/// <summary>
/// Independent Phase 1 fixture data shared by mock tests and future production bindings.
/// No acceptance, comparison, path resolution or timing algorithm is implemented here.
/// </summary>
public static class OracleContractFixtures
{
    /// <summary>FR-VICE-001 AC1/AC3; exact original identity expectations.</summary>
    public static TheoryData<string, bool, string?> IdentityCases => new()
    {
        { "clean", true, null },
        { "wrong-commit", false, "CommitMismatch" },
        { "missing-commit", false, "CommitMissing" },
        { "tracked-delta", false, "DirtyBefore" },
        { "post-inventory", false, "InventoryChanged" },
        { "dirty-after", false, "DirtyAfter" },
        { "patched", false, "PatchedOracle" },
        { "different-source", false, "SourceMismatch" }
    };

    /// <summary>Fixed input mutations from the original identity fixtures.</summary>
    public static OracleState IdentityInput(string scenario) => scenario switch
    {
        "clean" => Fixtures.Clean,
        "wrong-commit" => Fixtures.Clean with { Commit = new string('b', 40) },
        "missing-commit" => Fixtures.Clean with { Commit = null },
        "tracked-delta" => Fixtures.Clean with { CleanBefore = false },
        "post-inventory" => Fixtures.Clean with { AfterInventory = new string('b', 64) },
        "dirty-after" => Fixtures.Clean with { CleanAfter = false },
        "patched" => Fixtures.Clean with { Patched = true },
        "different-source" => Fixtures.Clean with { Source = "managed-derived-fork" },
        _ => throw new ArgumentOutOfRangeException(nameof(scenario))
    };

    /// <summary>TR-VICE-ORACLE-001 AC1: original preflight/postflight outcomes.</summary>
    public static TheoryData<bool, bool> LifecycleCases => new()
    {
        { false, false }, { true, true }, { true, false }
    };

    /// <summary>F-07 single-fault controls require both postflight outcomes.</summary>
    public static TheoryData<bool> PostflightCases => new() { false, true };

    /// <summary>
    /// FR-VICE-001 AC2. Third column is expected observed output, not resolver input.
    /// Real bindings must establish actual filesystem fixtures and observe resolution.
    /// </summary>
    public static TheoryData<string, string, string, bool, bool, string?> BoundaryCases => new()
    {
        { "read", "F:/wrapper/input.vsf", "F:/wrapper/input.vsf", true, true, null },
        { "write", "F:/wrapper/output.vsf", "F:/wrapper/output.vsf", true, true, null },
        { "write", "F:/native/vice/out", "F:/native/vice/out", true, false, "ImmutableWrite" },
        { "patch", "F:/native/vice/cpu.c", "F:/native/vice/cpu.c", true, false, "ImmutablePatch" },
        { "generate", "F:/native/vice/generated.c", "F:/native/vice/generated.c", true, false, "ImmutableGeneration" },
        { "write", "F:/wrapper/../native/vice/out", "F:/native/vice/out", true, false, "ImmutableWrite" },
        { "write", "F:/wrapper/link/out", "F:/native/vice/out", true, false, "ImmutableWrite" },
        { "write", "f:/NATIVE/Vice/out", "F:/native/vice/out", true, false, "ImmutableWrite" },
        { "write", "F:/native/vice-copy/out", "F:/native/vice-copy/out", true, true, null },
        { "write", "F:/wrapper/out", "F:/wrapper/out", false, false, "InventoryChanged" }
    };

    /// <summary>TR-VICE-ORACLE-001 AC4 / TEST-VICE-001 AC3, including F-08 zero-test rejection.</summary>
    public static TheoryData<string, bool, string?> ReceiptCases => new()
    {
        { "valid", true, null },
        { "missing-commit", false, "CommitMissing" },
        { "wrong-commit", false, "CommitMismatch" },
        { "dirty-before", false, "DirtyBefore" },
        { "dirty-after", false, "DirtyAfter" },
        { "changed-inventory", false, "InventoryChanged" },
        { "missing-inventory", false, "InventoryMissing" },
        { "patched", false, "PatchedOracle" },
        { "other-source", false, "SourceMismatch" },
        { "missing-hash", false, "WrapperHashMissing" },
        { "malformed-hash", false, "WrapperHashInvalid" },
        { "missing-version", false, "WrapperVersionMissing" },
        { "missing-command", false, "CommandMissing" },
        { "missing-scope", false, "ScopeMissing" },
        { "zero-tests", false, "NoTestsExecuted" },
        { "negative-passed", false, "NegativeCount" },
        { "negative-failed", false, "NegativeCount" },
        { "negative-skipped", false, "NegativeCount" },
        { "negative-aborted", false, "NegativeCount" },
        { "failure", false, "FailedTests" },
        { "skip", false, "SkippedTests" },
        { "abort", false, "AbortedTests" }
    };

    /// <summary>Receipt input construction contains only fixed fixture mutations, no acceptance rules.</summary>
    public static Receipt ReceiptInput(string scenario)
    {
        var valid = new Receipt(Fixtures.Clean, Fixtures.Hash, "fixture-v1", "fixture-wrapper --system vic20",
            "synthetic contract example only", 10, 0, 0, 0);
        return scenario switch
        {
            "valid" => valid,
            "missing-commit" => valid with { Oracle = valid.Oracle with { Commit = null } },
            "wrong-commit" => valid with { Oracle = valid.Oracle with { Commit = new string('b', 40) } },
            "dirty-before" => valid with { Oracle = valid.Oracle with { CleanBefore = false } },
            "dirty-after" => valid with { Oracle = valid.Oracle with { CleanAfter = false } },
            "changed-inventory" => valid with { Oracle = valid.Oracle with { AfterInventory = new string('b', 64) } },
            "missing-inventory" => valid with { Oracle = valid.Oracle with { BeforeInventory = "" } },
            "patched" => valid with { Oracle = valid.Oracle with { Patched = true } },
            "other-source" => valid with { Oracle = valid.Oracle with { Source = "fork" } },
            "missing-hash" => valid with { WrapperHash = null },
            "malformed-hash" => valid with { WrapperHash = "not-sha256" },
            "missing-version" => valid with { WrapperVersion = null },
            "missing-command" => valid with { Command = null },
            "missing-scope" => valid with { Scope = null },
            "zero-tests" => valid with { Passed = 0 },
            "negative-passed" => valid with { Passed = -1 },
            "negative-failed" => valid with { Failed = -1 },
            "negative-skipped" => valid with { Skipped = -1 },
            "negative-aborted" => valid with { Aborted = -1 },
            "failure" => valid with { Failed = 1 },
            "skip" => valid with { Skipped = 1 },
            "abort" => valid with { Aborted = 1 },
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
    }

    /// <summary>All four required observation categories remain independent rows.</summary>
    public static TheoryData<string> ObservationCategories => new() { "cpu-bus", "timing", "snapshots", "lifecycle" };

    /// <summary>FR-VICE-001 AC4: original fixed continuity/provenance inputs and independent expected failures.</summary>
    public static IEnumerable<(NativeStream Input, Decision Expected)> ObservationCases(string category)
    {
        var valid = new NativeStream(category, true, "immutable-native-observation", 36, 38, [36, 37, 38]);
        yield return (valid, new Decision(true));
        yield return (valid with { Capability = false }, new Decision(false, "CapabilityUnavailable", 36));
        yield return (valid with { ObservedHalfCycles = [] }, new Decision(false, "ObservationMissing", 36));
        yield return (valid with { ObservedHalfCycles = [37, 38] }, new Decision(false, "ObservationMissing", 36));
        yield return (valid with { ObservedHalfCycles = [36, 38] }, new Decision(false, "ObservationMissing", 37));
        yield return (valid with { ObservedHalfCycles = [36, 37] }, new Decision(false, "ObservationMissing", 38));
        yield return (valid with { ObservedHalfCycles = [36, 36, 38] }, new Decision(false, "ObservationMissing", 37));
        yield return (valid with { ObservedHalfCycles = [36, 38, 37] }, new Decision(false, "ObservationMissing", 37));
        foreach (var provenance in new[] { "PeekBus", "PC-derived", "fabricated", "patched-target" })
            yield return (valid with { Provenance = provenance }, new Decision(false, "ProvenanceRejected", 36));
    }

    /// <summary>Partial FR-SNP-005 AC3 / TEST-SNP-002 AC4: unchanged half-cycle 37 bus mismatch fixture.</summary>
    public static (BusSample Native, BusSample Managed, Mismatch Expected) ComparatorCase
    {
        get
        {
            var registers = new Registers(0x2000, 0x42, 1, 2, 0xff, 0x24);
            return (new BusSample(37, registers, 0x1000), new BusSample(37, registers, 0x1001),
                new Mismatch(37, "BusAddress", 0x1000, 0x1001));
        }
    }

    /// <summary>TR-CYCLE-001 AC4: unchanged phase/AEC/access truth table and missing-data cases.</summary>
    public static TheoryData<int?, bool?, int?, bool, string?> PhaseCases => new()
    {
        { 1, false, 0, true, null },
        { 1, true, 0, true, null },
        { 2, false, 0, true, null },
        { 2, true, 0, true, null },
        { 2, true, 1, true, null },
        { 2, true, 2, true, null },
        { 1, false, 1, false, "AccessForbidden" },
        { 1, false, 2, false, "AccessForbidden" },
        { 1, true, 1, false, "AccessForbidden" },
        { 1, true, 2, false, "AccessForbidden" },
        { 2, false, 1, false, "AccessForbidden" },
        { 2, false, 2, false, "AccessForbidden" },
        { null, true, 1, false, "PhaseMissing" },
        { 0, true, 1, false, "PhaseUnknown" },
        { 99, true, 1, false, "PhaseUnknown" },
        { 2, null, 1, false, "AecMissing" },
        { 2, true, null, false, "AccessMissing" },
        { 2, true, -1, false, "AccessUnknown" },
        { 2, true, 99, false, "AccessUnknown" },
        { 1, null, 0, false, "AecMissing" },
        { null, false, 0, false, "PhaseMissing" },
        { 1, false, null, false, "AccessMissing" }
    };

    /// <summary>Original synthetic observation at exact half-cycle 37.</summary>
    public static Sequence PhaseInput(int? phase, bool? aec, int? access) =>
        new(37, 37, [new Observation(37, (Phase?)phase, aec, (Access?)access)]);

    /// <summary>Fixed interval expectations, not a gap-detection algorithm.</summary>
    public static TheoryData<string, bool, string?, long?> IntervalCases => new()
    {
        { "complete", true, null, null },
        { "empty", false, "ObservationMissing", 36L },
        { "leading", false, "ObservationMissing", 36L },
        { "interior", false, "ObservationMissing", 37L },
        { "trailing", false, "ObservationMissing", 38L },
        { "duplicate-gap", false, "ObservationMissing", 37L },
        { "out-of-order", false, "ObservationMissing", 37L },
        { "duplicate-complete", false, "SequenceOrder", 37L },
        { "extra", false, "UnexpectedObservation", 39L },
        { "missing-first", false, "IntervalMissing", null },
        { "missing-last", false, "IntervalMissing", null },
        { "reversed", false, "IntervalInvalid", null }
    };

    /// <summary>Original fixed cycle lists and interval parameters, reusable without duplicating expected values.</summary>
    public static Sequence IntervalInput(string scenario)
    {
        long[] cycles = scenario switch
        {
            "empty" => [], "leading" => [37, 38], "interior" => [36, 38], "trailing" => [36, 37],
            "duplicate-gap" => [36, 36, 38], "out-of-order" => [36, 38, 37],
            "duplicate-complete" => [36, 36, 37, 38], "extra" => [36, 37, 38, 39],
            "complete" or "missing-first" or "missing-last" or "reversed" => [36, 37, 38],
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        return new Sequence(scenario == "missing-first" ? null : scenario == "reversed" ? 38 : 36,
            scenario == "missing-last" ? null : scenario == "reversed" ? 36 : 38,
            cycles.Select(c => new Observation(c, Phase.Phi2, true, Access.None)).ToArray());
    }
}
