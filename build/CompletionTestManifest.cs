namespace ViceSharp.Build.Completion;

/// <summary>
/// Describes whether a discovered project contributes executable tests.
/// </summary>
internal enum CompletionProjectKind
{
    Test,
    NonTest,
}

/// <summary>
/// Describes why a discovered test identity is not executed by a completion partition.
/// </summary>
internal enum CompletionDispositionKind
{
    IntentionalManual,
    Unresolved,
}

/// <summary>
/// Describes the evidence contract for a non-test completion step.
/// </summary>
internal enum CompletionEvidenceKind
{
    Command,
    LiveRuntime,
    AiReview,
}

/// <summary>
/// A repository project that must be reconciled by the completion manifest.
/// </summary>
internal sealed record CompletionProjectExpectation(
    string ProjectPath,
    CompletionProjectKind Kind);

/// <summary>
/// Serializable selector metadata for a test partition.
/// </summary>
internal sealed record CompletionTestSelector(
    string Filter,
    string? RequiredCategory = null,
    IReadOnlyList<string>? ExcludedCategories = null,
    IReadOnlyList<string>? ExcludedIdentities = null,
    string? IncludedIdentityPrefix = null);

/// <summary>
/// One executable partition of a test project.
/// </summary>
internal sealed record CompletionPartitionExpectation(
    string Id,
    string ProjectPath,
    CompletionTestSelector Selector,
    bool IsPaid = false,
    int? FixedExpectedCount = null);

/// <summary>
/// An exact test-method disposition outside executable product coverage.
/// </summary>
internal sealed record CompletionMethodDisposition(
    string TestIdentity,
    CompletionDispositionKind Kind,
    bool IsExactMethod,
    string Reason);

/// <summary>
/// A required discovered test method or data case.
/// </summary>
internal sealed record CompletionCaseRequirement(
    string IdentityPrefix,
    int MinimumDiscoveredCases);

/// <summary>
/// A required file or binary used by completion validation.
/// </summary>
internal sealed record CompletionArtifactRequirement(
    string Id,
    string Path,
    bool MustBeNonEmpty,
    bool MustBeLoadable);

/// <summary>
/// A required non-test evidence entry.
/// </summary>
internal sealed record CompletionEvidenceRequirement(
    string Id,
    CompletionEvidenceKind Kind);

/// <summary>
/// The machine-readable completion contract.
/// </summary>
internal sealed record CompletionManifest(
    IReadOnlyList<CompletionProjectExpectation> Projects,
    IReadOnlyList<CompletionPartitionExpectation> Partitions,
    IReadOnlyList<CompletionMethodDisposition> MethodDispositions,
    IReadOnlyList<CompletionCaseRequirement> CaseRequirements,
    IReadOnlyList<CompletionArtifactRequirement> ArtifactRequirements,
    IReadOnlyList<CompletionEvidenceRequirement> EvidenceRequirements);

/// <summary>
/// One discovered test case with its project and category metadata.
/// </summary>
internal sealed record CompletionDiscoveredTestCase(
    string Identity,
    string ProjectPath,
    IReadOnlyList<string> Categories);

/// <summary>
/// Test discovery evidence tied to a final test assembly and product binaries.
/// </summary>
internal sealed record CompletionDiscoveryEvidence(
    string ProjectPath,
    string InvocationId,
    string TestAssemblyHash,
    string ProductBinaryHash,
    IReadOnlyDictionary<string, string> NativeLibraryHashes,
    IReadOnlyList<CompletionDiscoveredTestCase> Cases);

/// <summary>
/// File evidence retained by a completion run.
/// </summary>
internal sealed record CompletionFileEvidence(
    string Path,
    bool Exists,
    long Length,
    bool IsValid,
    bool IsLoadable,
    string Hash);

/// <summary>
/// Normalized TRX counters.
/// </summary>
internal sealed record CompletionTestCounters(
    int Total,
    int Executed,
    int Passed,
    int Failed,
    int Skipped,
    int Aborted,
    int Error,
    int Timeout,
    int NotExecuted,
    int Inconclusive);

/// <summary>
/// Evidence for one test partition.
/// </summary>
internal sealed record CompletionTestPartitionEvidence(
    string PartitionId,
    string InvocationId,
    string TestAssemblyHash,
    string ProductBinaryHash,
    IReadOnlyDictionary<string, string> NativeLibraryHashes,
    CompletionFileEvidence Trx,
    CompletionFileEvidence StandardOutput,
    CompletionFileEvidence DiagnosticLog,
    CompletionTestCounters Counters,
    IReadOnlyList<string> ExecutedCaseIdentities);

/// <summary>
/// Evidence for a build, publish, or audit command.
/// </summary>
internal sealed record CompletionCommandEvidence(
    string Id,
    string InvocationId,
    int ExitCode,
    CompletionFileEvidence StandardOutput,
    CompletionFileEvidence StandardError,
    CompletionFileEvidence? OutputArtifact,
    CompletionTestCounters? TestCounters);

/// <summary>
/// Typed evidence from one launched application validation pass.
/// </summary>
internal sealed record CompletionLiveRuntimeEvidence(
    string Id,
    string InvocationId,
    string ProductBinaryHash,
    bool HasProcessIdentity,
    bool HasDebugAttachSession,
    bool HasRemoteControlCapabilities,
    bool HasDraftAcceptedActiveState,
    bool HasRuntimeObservedEffects,
    bool HasReadyGeometryClockPacingWarp,
    bool HasScreenshotsAndHashes);

/// <summary>
/// Durable evidence for one hostile AI review.
/// </summary>
internal sealed record CompletionAiReviewEvidence(
    string Id,
    string InvocationId,
    string Model,
    string Effort,
    string Status,
    CompletionFileEvidence Artifact,
    string Verdict);

/// <summary>
/// Final artifact provenance for the completion invocation.
/// </summary>
internal sealed record CompletionFinalArtifacts(
    string ProductBinaryHash,
    IReadOnlyDictionary<string, string> TestAssemblyHashes,
    IReadOnlyDictionary<string, string> NativeLibraryHashes,
    IReadOnlyList<CompletionFileEvidence> Files);

/// <summary>
/// One partition execution retained from an earlier completion invocation.
/// </summary>
internal sealed record CompletionPriorPartitionExecution(
    string InvocationId,
    string PartitionId);

/// <summary>
/// All evidence submitted for one CompletionTest invocation.
/// </summary>
internal sealed record CompletionRunEvidence(
    string InvocationId,
    CompletionFinalArtifacts FinalArtifacts,
    IReadOnlyList<CompletionDiscoveryEvidence> Discoveries,
    IReadOnlyList<CompletionTestPartitionEvidence> TestPartitions,
    IReadOnlyList<CompletionCommandEvidence> Commands,
    IReadOnlyList<CompletionLiveRuntimeEvidence> LiveRuntimePasses,
    IReadOnlyList<CompletionAiReviewEvidence> AiReviews,
    IReadOnlyList<CompletionPriorPartitionExecution> PriorPartitionExecutions);

/// <summary>
/// One completion-manifest defect.
/// </summary>
internal sealed record CompletionValidationIssue(string Code, string Message);

/// <summary>
/// Result of validating a completion manifest and its retained evidence.
/// </summary>
internal sealed record CompletionValidationResult(IReadOnlyList<CompletionValidationIssue> Issues)
{
    internal bool IsValid => Issues.Count == 0;
}

/// <summary>
/// Compilation contract for the Slice 0 red tests. Validation behavior is intentionally
/// absent until the hostile red-test review accepts the requested scenarios.
/// </summary>
internal static class CompletionTestManifestValidator
{
    internal static CompletionValidationResult Validate(
        CompletionManifest manifest,
        CompletionRunEvidence run)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(run);

        return new CompletionValidationResult([]);
    }
}
