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
/// Serializable selector metadata for a test partition. The runner retains <see cref="Filter"/>
/// as its VSTest argument; this validator selects cases using the normalized category/identity
/// fields and reconciles them with complete parsed TRX identities, without interpreting VSTest syntax.
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
/// A required discovered test method or data case. A method identity includes only that exact
/// method and its parenthesized theory cases, never another method with a similar name.
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
    IReadOnlyList<string> ExecutedCaseIdentities,
    int ExitCode,
    CompletionFileEvidence StandardError);

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
/// FR-COMPLETION-001 / TR-COMPLETION-MANIFEST-001: reconciles final discovery with
/// retained test, command, runtime and review evidence. This pure validator does not run
/// commands or infer success from missing evidence. Collectors remain responsible for truthful
/// discovery, file hashes, parsed TRX identities and typed runtime/review records; validation of
/// these normalized records does not replace the pending Nuke collection/execution pipeline.
/// </summary>
internal static class CompletionTestManifestValidator
{
    internal static CompletionValidationResult Validate(
        CompletionManifest manifest,
        CompletionRunEvidence run)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(run);

        return new Validator(manifest, run).Validate();
    }

    private sealed class Validator(CompletionManifest manifest, CompletionRunEvidence run)
    {
        private readonly List<CompletionValidationIssue> _issues = [];

        // These exact diagnostics intentionally fail to print reports; they are not product gates.
        // See the approved method dispositions in sol-slice0-inventory.md (2026-09-06).
        private static readonly string[] ReportDiagnostics =
        [
            "ViceSharp.TestHarness.LiveDeployedAppSettingsBisectTests.DeployedApp_SettingsBisect_ReportsPhaseSpeeds",
            "ViceSharp.TestHarness.LiveLimiterBandProbeTests.DeployedApp_LimiterBand_ReportsAchievedSpeeds",
            "ViceSharp.TestHarness.Vic20.BranchCycleCountTests.TakenBne_CycleTrace",
            "ViceSharp.TestHarness.Vic20.BranchCycleCountTests.NotTakenBne_CycleTrace",
        ];

        internal CompletionValidationResult Validate()
        {
            if (manifest.Projects.Count == 0 || manifest.Partitions.Count == 0)
                Issue("manifest_empty", "Completion requires projects and executable partitions.");
            CheckUnique(manifest.Projects.Select(project => project.ProjectPath), "project_duplicate");
            CheckUnique(manifest.Partitions.Select(partition => partition.Id), "partition_duplicate");
            CheckUnique(manifest.EvidenceRequirements.Select(requirement => requirement.Id), "evidence_duplicate");
            ValidateDiscovery();
            ValidatePartitions();
            ValidateArtifacts();
            ValidateOtherEvidence();
            return new CompletionValidationResult(_issues);
        }

        private void ValidateDiscovery()
        {
            foreach (var project in manifest.Projects.Where(project => project.Kind == CompletionProjectKind.Test))
            {
                int count = run.Discoveries.Count(discovery => discovery.ProjectPath == project.ProjectPath);
                if (count != 1)
                    Issue(count == 0 ? "discovery_missing" : "discovery_duplicate", project.ProjectPath);
                if (!manifest.Partitions.Any(partition => partition.ProjectPath == project.ProjectPath))
                    Issue("partition_missing", project.ProjectPath);
            }

            foreach (var disposition in manifest.MethodDispositions)
            {
                if (!disposition.IsExactMethod || !IsExactIdentity(disposition.TestIdentity))
                    Issue("disposition_not_exact", disposition.TestIdentity);
                if (disposition.Kind != CompletionDispositionKind.IntentionalManual
                    || string.IsNullOrWhiteSpace(disposition.Reason))
                    Issue("disposition_unresolved", disposition.TestIdentity);
            }

            foreach (var discovery in run.Discoveries)
            {
                if (!manifest.Projects.Any(project => project.ProjectPath == discovery.ProjectPath
                        && project.Kind == CompletionProjectKind.Test))
                    Issue("project_missing", discovery.ProjectPath);
                CheckProvenance(discovery.ProjectPath, discovery.InvocationId, discovery.TestAssemblyHash,
                    discovery.ProductBinaryHash, discovery.NativeLibraryHashes);

                foreach (var test in discovery.Cases)
                {
                    if (test.ProjectPath != discovery.ProjectPath)
                        Issue("discovery_project_mismatch", test.Identity);
                    var selected = manifest.Partitions.Where(partition => partition.ProjectPath == discovery.ProjectPath
                        && Matches(partition.Selector, test)).ToArray();
                    int dispositions = manifest.MethodDispositions.Count(disposition => disposition.IsExactMethod
                        && disposition.Kind == CompletionDispositionKind.IntentionalManual
                        && MethodMatches(disposition.TestIdentity, test.Identity));
                    if (selected.Length + dispositions == 0)
                        Issue("test_unassigned", test.Identity);
                    else if (selected.Length + dispositions != 1)
                        Issue("test_multiply_assigned", test.Identity);
                    if (test.Categories.Any(category => category is "ParityPending" or "ParityLegacy"))
                        Issue("parity_unresolved", test.Identity);
                    if (selected.Length > 0 && ReportDiagnostics.Any(method => MethodMatches(method, test.Identity)))
                        Issue("always_failing_diagnostic_selected", test.Identity);

                    // The AI contract partition must include future non-paid cases, not a frozen class list.
                    if (manifest.Partitions.Any(partition => partition.ProjectPath == discovery.ProjectPath && partition.IsPaid)
                        && !test.Categories.Contains("AiReview", StringComparer.Ordinal)
                        && selected.Count(partition => !partition.IsPaid) != 1)
                        Issue("ai_contract_incomplete", test.Identity);
                }
            }

            foreach (var requirement in manifest.CaseRequirements)
            {
                int count = run.Discoveries.SelectMany(discovery => discovery.Cases)
                    .Count(test => MethodMatches(requirement.IdentityPrefix, test.Identity));
                if (requirement.MinimumDiscoveredCases < 1 || count < requirement.MinimumDiscoveredCases)
                    Issue("required_case_missing", requirement.IdentityPrefix);
            }
        }

        private void ValidatePartitions()
        {
            // Every execution owns a complete TRX. Reusing its path or copying its content cannot
            // independently prove a different partition's claimed parsed case identities.
            CheckUnique(run.TestPartitions.Select(execution => execution.Trx.Path), "test_artifact_reused");
            CheckUnique(run.TestPartitions.Select(execution => execution.Trx.Hash), "test_artifact_reused");
            foreach (var partition in manifest.Partitions)
            {
                if (!manifest.Projects.Any(project => project.ProjectPath == partition.ProjectPath
                        && project.Kind == CompletionProjectKind.Test))
                    Issue("project_missing", partition.ProjectPath);
                if (partition.FixedExpectedCount is not null)
                    Issue("fixed_count_for_discovered_partition", partition.Id);
                if (partition.Selector.ExcludedIdentities?.Any(identity => !IsExactIdentity(identity)) == true)
                    Issue("disposition_not_exact", partition.Id);
                var executions = run.TestPartitions.Where(execution => execution.PartitionId == partition.Id).ToArray();
                if (executions.Length == 0)
                    Issue("partition_missing", partition.Id);
                if (executions.Length + run.PriorPartitionExecutions.Count(prior => prior.InvocationId == run.InvocationId
                        && prior.PartitionId == partition.Id) > 1)
                    Issue("partition_duplicate", partition.Id);

                var expected = run.Discoveries.Where(discovery => discovery.ProjectPath == partition.ProjectPath)
                    .SelectMany(discovery => discovery.Cases).Where(test => Matches(partition.Selector, test))
                    .Select(test => test.Identity).ToArray();
                foreach (var execution in executions)
                {
                    CheckProvenance(partition.ProjectPath, execution.InvocationId, execution.TestAssemblyHash,
                        execution.ProductBinaryHash, execution.NativeLibraryHashes);
                    if (!ValidFile(execution.Trx) || !ValidFile(execution.StandardOutput)
                        || !ValidFile(execution.DiagnosticLog) || !ValidFile(execution.StandardError, nonEmpty: false))
                        Issue("test_artifact_invalid", partition.Id);
                    if (execution.ExitCode != 0)
                        Issue("test_process_failed", partition.Id);
                    ValidateCounters(partition.Id, execution);

                    var expectedCounts = Counts(expected);
                    var actualCounts = Counts(execution.ExecutedCaseIdentities);
                    if (!EqualCounts(expectedCounts, actualCounts))
                    {
                        Issue("trx_identity_mismatch", partition.Id);
                        if (expectedCounts.Any(pair => actualCounts.TryGetValue(pair.Key, out int count) && pair.Value != count))
                            Issue("trx_identity_multiplicity", partition.Id);
                    }
                }
            }
            foreach (var execution in run.TestPartitions)
                if (!manifest.Partitions.Any(partition => partition.Id == execution.PartitionId))
                    Issue("partition_unexpected", execution.PartitionId);
        }

        private void ValidateCounters(string partitionId, CompletionTestPartitionEvidence execution)
        {
            var counters = execution.Counters;
            if (counters.Executed == 0)
                Issue("test_zero_executed", partitionId);
            if (counters.Failed > 0)
                Issue("test_failed", partitionId);
            if (counters.Skipped > 0)
                Issue("test_skipped", partitionId);
            if (counters.Aborted > 0 || counters.Error > 0 || counters.Timeout > 0
                || counters.NotExecuted > 0 || counters.Inconclusive > 0)
                Issue("test_incomplete", partitionId);
            if (counters.Total != execution.ExecutedCaseIdentities.Count
                || counters.Executed != counters.Total || counters.Passed != counters.Executed
                || counters.Total < 0 || counters.Failed < 0 || counters.Skipped < 0 || counters.Aborted < 0
                || counters.Error < 0 || counters.Timeout < 0 || counters.NotExecuted < 0 || counters.Inconclusive < 0)
                Issue("test_counter_mismatch", partitionId);
        }

        private void ValidateArtifacts()
        {
            foreach (var requirement in manifest.ArtifactRequirements)
            {
                var files = run.FinalArtifacts.Files.Where(file => file.Path == requirement.Path).ToArray();
                if (files.Length != 1 || !ValidFile(files[0], requirement.MustBeNonEmpty))
                    Issue(requirement.Id switch
                    {
                        "native-x64" => "native_x64_invalid",
                        "native-xvic" => "native_xvic_invalid",
                        _ => "artifact_invalid",
                    }, requirement.Path);
                if (requirement.MustBeLoadable && (files.Length != 1 || !files[0].IsLoadable))
                    Issue("native_not_loadable", requirement.Path);
                if (files.Length != 1)
                    continue;
                string? nativeKey = requirement.Id switch
                {
                    "native-x64" => "vice_x64",
                    "native-xvic" => "vice_xvic",
                    _ => null,
                };
                if (nativeKey is not null && (!run.FinalArtifacts.NativeLibraryHashes.TryGetValue(nativeKey, out string? hash)
                        || !SameHash(hash, files[0].Hash)))
                    Issue("native_binary_provenance", requirement.Path);
                if (requirement.Id == "release-product" && !SameHash(files[0].Hash, run.FinalArtifacts.ProductBinaryHash))
                    Issue("product_binary_provenance", requirement.Path);
            }
        }

        private void ValidateOtherEvidence()
        {
            foreach (var requirement in manifest.EvidenceRequirements)
            {
                int count = requirement.Kind switch
                {
                    CompletionEvidenceKind.Command => run.Commands.Count(item => item.Id == requirement.Id),
                    CompletionEvidenceKind.LiveRuntime => run.LiveRuntimePasses.Count(item => item.Id == requirement.Id),
                    CompletionEvidenceKind.AiReview => run.AiReviews.Count(item => item.Id == requirement.Id),
                    _ => 0,
                };
                if (count == 0)
                    Issue(requirement.Kind switch
                    {
                        CompletionEvidenceKind.Command => "command_missing",
                        CompletionEvidenceKind.LiveRuntime => "live_evidence_missing",
                        _ => "ai_evidence_missing",
                    }, requirement.Id);
                if (count > 1)
                    Issue("evidence_duplicate", requirement.Id);
            }

            foreach (var command in run.Commands)
            {
                CheckEvidenceIdentity(command.Id, command.InvocationId, CompletionEvidenceKind.Command);
                if (command.ExitCode != 0)
                    Issue("command_failed", command.Id);
                if (command.TestCounters is not null)
                    Issue("command_evidence_type", command.Id);
                if (!ValidFile(command.StandardOutput, nonEmpty: false) || !ValidFile(command.StandardError, nonEmpty: false))
                    Issue("command_artifact_missing", command.Id);
                bool requiresOutput = command.Id is "release-build" or "native-build" or "xbox-publish";
                if ((requiresOutput && command.OutputArtifact is null)
                    || (command.OutputArtifact is not null && !ValidFile(command.OutputArtifact)))
                    Issue("command_artifact_missing", command.Id);
                string? expectedArtifactId = command.Id switch
                {
                    "release-build" => "release-product",
                    "native-build" => "native-x64",
                    "xbox-publish" => "xbox-publish",
                    _ => null,
                };
                if (expectedArtifactId is not null)
                {
                    var expected = manifest.ArtifactRequirements.FirstOrDefault(artifact => artifact.Id == expectedArtifactId);
                    if (expected is null || command.OutputArtifact?.Path != expected.Path)
                        Issue("command_artifact_mismatch", command.Id);
                }
                if (command.OutputArtifact is { } output && !run.FinalArtifacts.Files.Any(file =>
                        file.Path == output.Path && SameHash(file.Hash, output.Hash) && file.Length == output.Length))
                    Issue("command_artifact_provenance", command.Id);
            }

            foreach (var live in run.LiveRuntimePasses)
            {
                CheckEvidenceIdentity(live.Id, live.InvocationId, CompletionEvidenceKind.LiveRuntime);
                if (!SameHash(live.ProductBinaryHash, run.FinalArtifacts.ProductBinaryHash))
                    Issue("product_binary_provenance", live.Id);
                if (!live.HasProcessIdentity || !live.HasDebugAttachSession || !live.HasRemoteControlCapabilities
                    || !live.HasDraftAcceptedActiveState || !live.HasRuntimeObservedEffects
                    || !live.HasReadyGeometryClockPacingWarp || !live.HasScreenshotsAndHashes)
                    Issue("live_evidence_incomplete", live.Id);
            }

            foreach (var review in run.AiReviews)
            {
                CheckEvidenceIdentity(review.Id, review.InvocationId, CompletionEvidenceKind.AiReview);
                if (!string.Equals(review.Model, "Astra", StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(review.Effort, "xhigh", StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(review.Status, "completed", StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(review.Verdict, "AGREE", StringComparison.OrdinalIgnoreCase)
                    || !ValidFile(review.Artifact))
                    Issue("ai_evidence_invalid", review.Id);
            }
        }

        private void CheckProvenance(string project, string invocation, string assemblyHash,
            string productHash, IReadOnlyDictionary<string, string> nativeHashes)
        {
            CheckInvocation(invocation, project);
            if (!run.FinalArtifacts.TestAssemblyHashes.TryGetValue(project, out string? finalHash)
                || !SameHash(assemblyHash, finalHash))
                Issue("test_assembly_provenance", project);
            if (!SameHash(productHash, run.FinalArtifacts.ProductBinaryHash))
                Issue("product_binary_provenance", project);
            if (nativeHashes.Count != run.FinalArtifacts.NativeLibraryHashes.Count
                || run.FinalArtifacts.NativeLibraryHashes.Any(pair =>
                    !nativeHashes.TryGetValue(pair.Key, out string? hash) || !SameHash(hash, pair.Value)))
                Issue("native_binary_provenance", project);
        }

        private void CheckEvidenceIdentity(string id, string invocation, CompletionEvidenceKind kind)
        {
            CheckInvocation(invocation, id);
            if (!manifest.EvidenceRequirements.Any(requirement => requirement.Id == id && requirement.Kind == kind))
                Issue("evidence_unexpected", id);
        }

        private void CheckInvocation(string invocation, string subject)
        {
            if (string.IsNullOrWhiteSpace(run.InvocationId) || invocation != run.InvocationId)
                Issue("invocation_mismatch", subject);
        }

        private void CheckUnique(IEnumerable<string> ids, string code)
        {
            foreach (var group in ids.GroupBy(id => id, StringComparer.Ordinal))
                if (string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1)
                    Issue(code, group.Key);
        }

        private void Issue(string code, string subject) => _issues.Add(new CompletionValidationIssue(code, subject));

        private static bool ValidFile(CompletionFileEvidence file, bool nonEmpty = true) =>
            file.Exists && file.IsValid && file.Length >= (nonEmpty ? 1 : 0)
            && !string.IsNullOrWhiteSpace(file.Path) && !string.IsNullOrWhiteSpace(file.Hash);

        private static bool SameHash(string left, string right) =>
            !string.IsNullOrWhiteSpace(left) && string.Equals(left, right, StringComparison.Ordinal);

        private static bool IsExactIdentity(string identity) =>
            !string.IsNullOrWhiteSpace(identity) && identity.Contains('.') && identity.IndexOfAny(['*', '?', '/', '\\']) < 0;

        private static bool MethodMatches(string method, string identity) =>
            identity == method || identity.StartsWith(method + "(", StringComparison.Ordinal);

        private static bool Matches(CompletionTestSelector selector, CompletionDiscoveredTestCase test) =>
            (selector.RequiredCategory is null || test.Categories.Contains(selector.RequiredCategory, StringComparer.Ordinal))
            && (selector.ExcludedCategories is null || !selector.ExcludedCategories.Any(category =>
                test.Categories.Contains(category, StringComparer.Ordinal)))
            && (selector.ExcludedIdentities is null || !selector.ExcludedIdentities.Any(method => MethodMatches(method, test.Identity)))
            && (selector.IncludedIdentityPrefix is null || test.Identity.StartsWith(selector.IncludedIdentityPrefix, StringComparison.Ordinal));

        private static Dictionary<string, int> Counts(IEnumerable<string> identities) =>
            identities.GroupBy(identity => identity, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        private static bool EqualCounts(IReadOnlyDictionary<string, int> expected, IReadOnlyDictionary<string, int> actual) =>
            expected.Count == actual.Count && expected.All(pair => actual.TryGetValue(pair.Key, out int count) && pair.Value == count);
    }
}
