namespace ViceSharp.Library.Tests;

using System.Text.Json;
using ViceSharp.Build.Completion;
using Xunit;

public sealed class CompletionTestManifestTests
{
    private const string InvocationId = "completion-20260907-001";
    private const string ProductHash = "sha256-product-final";
    private const string X64Hash = "sha256-vice-x64-final";
    private static readonly string XvicHash = ReadPinnedXvicDllHash();

    private const string HeadsProject = "tests/ViceSharp.Heads.Tests/ViceSharp.Heads.Tests.csproj";
    private const string LibraryProject = "tests/ViceSharp.Library.Tests/ViceSharp.Library.Tests.csproj";
    private const string HarnessProject = "tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj";
    private const string IntegrationProject = "tests/ViceSharp.Library.IntegrationTests/ViceSharp.Library.IntegrationTests.csproj";
    private const string AiProject = "tests/ViceSharp.AiReview.Tests/ViceSharp.AiReview.Tests.csproj";
    private const string CliProject = "tests/ViceSharp.RemoteControlCli.Tests/ViceSharp.RemoteControlCli.Tests.csproj";
    private const string BenchmarksProject = "tests/ViceSharp.Benchmarks/ViceSharp.Benchmarks.csproj";

    private const string ManagedPartition = "test-harness-managed";
    private const string PaidAiPartition = "ai-paid";
    private const string CliPartition = "remote-control-cli";
    private const string SnapshotCase =
        "ViceSharp.TestHarness.SnapshotRunLogParityTests.SnapshotRun_MatchesViceRunLog(case-1)";

    private static readonly string[] ManualMethods =
    [
        "ViceSharp.TestHarness.CaptureRunLogUtilityTests.CaptureBaselineRunLog_FromEnvironment",
        "ViceSharp.TestHarness.LiveHandlerTraceTests.Trace_LiveIrqHandler_FromUserSnapshot",
        "ViceSharp.TestHarness.LiveDeployedAppSettingsBisectTests.DeployedApp_SettingsBisect_ReportsPhaseSpeeds",
        "ViceSharp.TestHarness.LiveLimiterBandProbeTests.DeployedApp_LimiterBand_ReportsAchievedSpeeds",
        "ViceSharp.TestHarness.Vic20.BranchCycleCountTests.TakenBne_CycleTrace",
        "ViceSharp.TestHarness.Vic20.BranchCycleCountTests.NotTakenBne_CycleTrace",
    ];

    [Fact]
    public void Validate_FailsWhenDiscoveredTestProjectIsMissingFromManifest()
    {
        var fixture = CompleteFixture();
        var manifest = fixture.Manifest with
        {
            Projects = fixture.Manifest.Projects
                .Where(project => project.ProjectPath != CliProject)
                .ToArray(),
        };

        AssertIssue("project_missing", manifest, fixture.Run);
    }

    [Fact]
    public void Validate_FailsWhenDiscoveredTestIdentityMatchesNoPartition()
    {
        var fixture = CompleteFixture();
        var manifest = fixture.Manifest with
        {
            Partitions = Replace(
                fixture.Manifest.Partitions,
                partition => partition.Id == CliPartition,
                partition => partition with
                {
                    Selector = partition.Selector with { RequiredCategory = "NeverSelected" },
                }),
        };

        AssertIssue("test_unassigned", manifest, fixture.Run);
    }

    [Fact]
    public void Validate_FailsWhenDiscoveredTestIdentityMatchesMultiplePartitions()
    {
        var fixture = CompleteFixture();
        var duplicate = new CompletionPartitionExpectation(
            "heads-duplicate",
            HeadsProject,
            new CompletionTestSelector(string.Empty));

        var manifest = fixture.Manifest with
        {
            Partitions = [.. fixture.Manifest.Partitions, duplicate],
        };

        AssertIssue("test_multiply_assigned", manifest, fixture.Run);
    }

    [Fact]
    public void Validate_FailsWhenExclusionUsesFileOrClassWildcard()
    {
        var fixture = CompleteFixture();
        var wildcard = new CompletionMethodDisposition(
            "ViceSharp.TestHarness.LiveHandlerTraceTests.*",
            CompletionDispositionKind.IntentionalManual,
            IsExactMethod: false,
            "Invalid class-wide exclusion.");

        var manifest = fixture.Manifest with
        {
            MethodDispositions = [.. fixture.Manifest.MethodDispositions, wildcard],
        };

        AssertIssue("disposition_not_exact", manifest, fixture.Run);
    }

    [Fact]
    public void Validate_FailsWhenCliExpectedCountIsFrozen()
    {
        var fixture = CompleteFixture();
        var manifest = fixture.Manifest with
        {
            Partitions = Replace(
                fixture.Manifest.Partitions,
                partition => partition.Id == CliPartition,
                partition => partition with { FixedExpectedCount = 6 }),
        };

        AssertIssue("fixed_count_for_discovered_partition", manifest, fixture.Run);
    }

    [Fact]
    public void Validate_AcceptsNewCliCaseThroughDiscoveryBasedPartition()
    {
        var fixture = CompleteFixture();
        var discovery = fixture.Run.Discoveries.Single(item => item.ProjectPath == CliProject);
        var addedCase = TestCase(
            "ViceSharp.RemoteControlCli.Tests.GrpcRemoteControlClientTests.GetCapabilities_UsesGrpc",
            CliProject);
        var updatedDiscovery = discovery with { Cases = [.. discovery.Cases, addedCase] };
        var execution = fixture.Run.TestPartitions.Single(item => item.PartitionId == CliPartition);
        var updatedExecution = WithExecutedCases(
            execution,
            [.. execution.ExecutedCaseIdentities, addedCase.Identity]);

        var run = fixture.Run with
        {
            Discoveries = Replace(
                fixture.Run.Discoveries,
                item => item.ProjectPath == CliProject,
                _ => updatedDiscovery),
            TestPartitions = Replace(
                fixture.Run.TestPartitions,
                item => item.PartitionId == CliPartition,
                _ => updatedExecution),
        };

        AssertValid(fixture.Manifest, run);
    }

    [Fact]
    public void Validate_FailsWhenAiContractPartitionOmitsFutureNonAiReviewTest()
    {
        var fixture = CompleteFixture();
        var manifest = fixture.Manifest with
        {
            Partitions = Replace(
                fixture.Manifest.Partitions,
                partition => partition.Id == "ai-contract",
                partition => partition with
                {
                    Selector = partition.Selector with
                    {
                        IncludedIdentityPrefix = "ViceSharp.AiReview.Tests.ReviewLogTests",
                    },
                }),
        };

        AssertIssue("ai_contract_incomplete", manifest, fixture.Run);
    }

    [Fact]
    public void Validate_FailsWhenPaidAiIdentityAlsoMatchesContractPartition()
    {
        var fixture = CompleteFixture();
        var manifest = fixture.Manifest with
        {
            Partitions = Replace(
                fixture.Manifest.Partitions,
                partition => partition.Id == "ai-contract",
                partition => partition with
                {
                    Selector = partition.Selector with { ExcludedCategories = [] },
                }),
        };

        AssertIssue("test_multiply_assigned", manifest, fixture.Run);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("empty")]
    [InlineData("invalid")]
    public void Validate_FailsWhenRequiredTestArtifactIsMissing(string state)
    {
        var fixture = CompleteFixture();
        var execution = fixture.Run.TestPartitions.First();
        var trx = state switch
        {
            "missing" => execution.Trx with { Exists = false },
            "empty" => execution.Trx with { Length = 0 },
            _ => execution.Trx with { IsValid = false },
        };

        var run = fixture.Run with
        {
            TestPartitions = Replace(
                fixture.Run.TestPartitions,
                item => item.PartitionId == execution.PartitionId,
                item => item with { Trx = trx }),
        };

        AssertIssue("test_artifact_invalid", fixture.Manifest, run);
    }

    [Fact]
    public void Validate_FailsWhenTrxHasZeroExecutedTests()
    {
        var fixture = CompleteFixture();
        var execution = fixture.Run.TestPartitions.First();
        var run = fixture.Run with
        {
            TestPartitions = Replace(
                fixture.Run.TestPartitions,
                item => item.PartitionId == execution.PartitionId,
                item => item with
                {
                    Counters = new CompletionTestCounters(0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                    ExecutedCaseIdentities = [],
                }),
        };

        AssertIssue("test_zero_executed", fixture.Manifest, run);
    }

    [Fact]
    public void Validate_FailsWhenTrxContainsSkippedTest()
    {
        var fixture = CompleteFixture();
        var run = WithCounters(
            fixture.Run,
            ManagedPartition,
            counters => counters with
            {
                Passed = counters.Passed - 1,
                Skipped = 1,
            });

        AssertIssue("test_skipped", fixture.Manifest, run);
    }

    [Fact]
    public void Validate_FailsWhenTrxContainsFailedTest()
    {
        var fixture = CompleteFixture();
        var run = WithCounters(
            fixture.Run,
            ManagedPartition,
            counters => counters with
            {
                Passed = counters.Passed - 1,
                Failed = 1,
            });

        AssertIssue("test_failed", fixture.Manifest, run);
    }

    [Theory]
    [InlineData("aborted")]
    [InlineData("error")]
    [InlineData("timeout")]
    [InlineData("not-executed")]
    [InlineData("inconclusive")]
    public void Validate_FailsWhenTrxContainsAbortedOrIncompleteTest(string counter)
    {
        var fixture = CompleteFixture();
        var run = WithCounters(
            fixture.Run,
            ManagedPartition,
            counters => counter switch
            {
                "aborted" => counters with { Passed = counters.Passed - 1, Aborted = 1 },
                "error" => counters with { Passed = counters.Passed - 1, Error = 1 },
                "timeout" => counters with { Passed = counters.Passed - 1, Timeout = 1 },
                "not-executed" => counters with { Passed = counters.Passed - 1, NotExecuted = 1 },
                _ => counters with { Passed = counters.Passed - 1, Inconclusive = 1 },
            });

        AssertIssue("test_incomplete", fixture.Manifest, run);
    }

    [Fact]
    public void Validate_FailsWhenPaidPartitionIsSelectedTwiceInOneInvocation()
    {
        var fixture = CompleteFixture();
        var paid = fixture.Run.TestPartitions.Single(item => item.PartitionId == PaidAiPartition);
        var run = fixture.Run with
        {
            TestPartitions = [.. fixture.Run.TestPartitions, paid],
        };

        AssertIssue("partition_duplicate", fixture.Manifest, run);
    }

    [Fact]
    public void Validate_AllowsPaidPartitionRerunInNewInvocation()
    {
        var fixture = CompleteFixture();
        var run = fixture.Run with
        {
            PriorPartitionExecutions =
            [
                new CompletionPriorPartitionExecution("completion-previous", PaidAiPartition),
            ],
        };

        AssertValid(fixture.Manifest, run);
    }

    [Fact]
    public void Validate_FailsWhenDispositionIsUnresolved()
    {
        var fixture = CompleteFixture();
        var unresolved = new CompletionMethodDisposition(
            "ViceSharp.TestHarness.VicCycleDivergentParityTests.PendingFetch",
            CompletionDispositionKind.Unresolved,
            IsExactMethod: true,
            "Pending parity has not been remediated.");
        var manifest = fixture.Manifest with
        {
            MethodDispositions = [.. fixture.Manifest.MethodDispositions, unresolved],
        };

        AssertIssue("disposition_unresolved", manifest, fixture.Run);
    }

    [Theory]
    [InlineData("ParityPending")]
    [InlineData("ParityLegacy")]
    public void Validate_FailsWhenPendingOrLegacyParityIsExcludedAsComplete(string category)
    {
        var fixture = CompleteFixture();
        var identity = $"ViceSharp.TestHarness.{category}Tests.Unresolved";
        var discovery = fixture.Run.Discoveries.Single(item => item.ProjectPath == HarnessProject);
        var unresolvedCase = TestCase(identity, HarnessProject, category);
        var disposition = new CompletionMethodDisposition(
            identity,
            CompletionDispositionKind.IntentionalManual,
            IsExactMethod: true,
            "Invalid attempt to waive parity.");

        var run = fixture.Run with
        {
            Discoveries = Replace(
                fixture.Run.Discoveries,
                item => item.ProjectPath == HarnessProject,
                item => item with { Cases = [.. item.Cases, unresolvedCase] }),
        };
        var manifest = fixture.Manifest with
        {
            MethodDispositions = [.. fixture.Manifest.MethodDispositions, disposition],
        };

        AssertIssue("parity_unresolved", manifest, run);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("empty")]
    public void Validate_FailsWhenViceX64ArtifactIsMissingOrEmpty(string state)
    {
        var fixture = CompleteFixture();
        var run = WithFinalFile(
            fixture.Run,
            "native/vice_x64.dll",
            file => state == "missing"
                ? file with { Exists = false }
                : file with { Length = 0 });

        AssertIssue("native_x64_invalid", fixture.Manifest, run);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("empty")]
    public void Validate_FailsWhenViceXvicArtifactIsMissingOrEmpty(string state)
    {
        var fixture = CompleteFixture();
        var run = WithFinalFile(
            fixture.Run,
            "native/vice_xvic.dll",
            file => state == "missing"
                ? file with { Exists = false }
                : file with { Length = 0 });

        AssertIssue("native_xvic_invalid", fixture.Manifest, run);
    }

    [Theory]
    [InlineData("native/vice_x64.dll")]
    [InlineData("native/vice_xvic.dll")]
    public void Validate_FailsWhenEitherNativeOracleIsNotLoadable(string path)
    {
        var fixture = CompleteFixture();
        var run = WithFinalFile(fixture.Run, path, file => file with { IsLoadable = false });

        AssertIssue("native_not_loadable", fixture.Manifest, run);
    }

    [Theory]
    [InlineData("release-build")]
    [InlineData("xbox-publish")]
    public void Validate_FailsWhenRequiredBuildOrPublishArtifactIsMissing(string commandId)
    {
        var fixture = CompleteFixture();
        var run = fixture.Run with
        {
            Commands = Replace(
                fixture.Run.Commands,
                command => command.Id == commandId,
                command => command with
                {
                    OutputArtifact = command.OutputArtifact! with { Exists = false },
                }),
        };

        AssertIssue("command_artifact_missing", fixture.Manifest, run);
    }

    [Fact]
    public void Validate_FailsWhenCommandEvidenceUsesTestCountersInsteadOfCommandContract()
    {
        var fixture = CompleteFixture();
        var run = fixture.Run with
        {
            Commands = Replace(
                fixture.Run.Commands,
                command => command.Id == "release-build",
                command => command with
                {
                    TestCounters = new CompletionTestCounters(1, 1, 1, 0, 0, 0, 0, 0, 0, 0),
                }),
        };

        AssertIssue("command_evidence_type", fixture.Manifest, run);
    }

    [Fact]
    public void Validate_FailsWhenLivePassLacksTypedRuntimeEvidence()
    {
        var fixture = CompleteFixture();
        var live = fixture.Run.LiveRuntimePasses.First();
        var run = fixture.Run with
        {
            LiveRuntimePasses = Replace(
                fixture.Run.LiveRuntimePasses,
                item => item.Id == live.Id,
                item => item with { HasRuntimeObservedEffects = false }),
        };

        AssertIssue("live_evidence_incomplete", fixture.Manifest, run);
    }

    [Theory]
    [InlineData("model")]
    [InlineData("effort")]
    [InlineData("status")]
    [InlineData("artifact")]
    [InlineData("verdict")]
    public void Validate_FailsWhenAiReviewArtifactLacksAstraXhighMetadata(string defect)
    {
        var fixture = CompleteFixture();
        var review = fixture.Run.AiReviews.First();
        var changed = defect switch
        {
            "model" => review with { Model = "Sol" },
            "effort" => review with { Effort = "high" },
            "status" => review with { Status = "in_progress" },
            "artifact" => review with { Artifact = review.Artifact with { Exists = false } },
            _ => review with { Verdict = "UNKNOWN" },
        };
        var run = fixture.Run with
        {
            AiReviews = Replace(
                fixture.Run.AiReviews,
                item => item.Id == review.Id,
                _ => changed),
        };

        AssertIssue("ai_evidence_invalid", fixture.Manifest, run);
    }

    [Fact]
    public void Validate_FailsWhenSnapshotRunLogTheoryHasNoDiscoveredData()
    {
        var fixture = CompleteFixture();
        var discovery = fixture.Run.Discoveries.Single(item => item.ProjectPath == HarnessProject);
        var execution = fixture.Run.TestPartitions.Single(item => item.PartitionId == ManagedPartition);
        var run = fixture.Run with
        {
            Discoveries = Replace(
                fixture.Run.Discoveries,
                item => item.ProjectPath == HarnessProject,
                item => item with
                {
                    Cases = item.Cases.Where(test => test.Identity != SnapshotCase).ToArray(),
                }),
            TestPartitions = Replace(
                fixture.Run.TestPartitions,
                item => item.PartitionId == ManagedPartition,
                item => WithExecutedCases(
                    item,
                    item.ExecutedCaseIdentities.Where(identity => identity != SnapshotCase).ToArray())),
        };

        AssertIssue("required_case_missing", fixture.Manifest, run);
    }

    [Fact]
    public void Validate_FailsWhenTrxOmitsDiscoveredExpectedCase()
    {
        var fixture = CompleteFixture();
        var execution = fixture.Run.TestPartitions.Single(item => item.PartitionId == ManagedPartition);
        var run = fixture.Run with
        {
            TestPartitions = Replace(
                fixture.Run.TestPartitions,
                item => item.PartitionId == ManagedPartition,
                item => WithExecutedCases(item, item.ExecutedCaseIdentities.Skip(1).ToArray())),
        };

        AssertIssue("trx_identity_mismatch", fixture.Manifest, run);
    }

    [Fact]
    public void Validate_FailsWhenTrxContainsUnexpectedCase()
    {
        var fixture = CompleteFixture();
        var execution = fixture.Run.TestPartitions.Single(item => item.PartitionId == ManagedPartition);
        var run = fixture.Run with
        {
            TestPartitions = Replace(
                fixture.Run.TestPartitions,
                item => item.PartitionId == ManagedPartition,
                item => WithExecutedCases(
                    item,
                    [.. item.ExecutedCaseIdentities, "ViceSharp.TestHarness.UnexpectedTests.Extra"])),
        };

        AssertIssue("trx_identity_mismatch", fixture.Manifest, run);
    }

    [Fact]
    public void Validate_FailsWhenTrxContainsExcessDuplicateCase()
    {
        var fixture = CompleteFixture();
        var execution = fixture.Run.TestPartitions.Single(item => item.PartitionId == ManagedPartition);
        var duplicate = execution.ExecutedCaseIdentities.First();
        var run = fixture.Run with
        {
            TestPartitions = Replace(
                fixture.Run.TestPartitions,
                item => item.PartitionId == ManagedPartition,
                item => WithExecutedCases(item, [.. item.ExecutedCaseIdentities, duplicate])),
        };

        AssertIssue("trx_identity_mismatch", fixture.Manifest, run);
    }

    [Fact]
    public void Validate_FailsWhenTrxCaseMultiplicityDiffersFromDiscovery()
    {
        var fixture = CompleteFixture();
        var discovery = fixture.Run.Discoveries.Single(item => item.ProjectPath == HeadsProject);
        var duplicate = discovery.Cases.Single();
        var run = fixture.Run with
        {
            Discoveries = Replace(
                fixture.Run.Discoveries,
                item => item.ProjectPath == HeadsProject,
                item => item with { Cases = [.. item.Cases, duplicate] }),
        };

        AssertIssue("trx_identity_multiplicity", fixture.Manifest, run);
    }

    [Fact]
    public void Validate_FailsWhenEvidenceInvocationIdDiffersFromCurrentInvocation()
    {
        var fixture = CompleteFixture();
        var execution = fixture.Run.TestPartitions.First();
        var run = fixture.Run with
        {
            TestPartitions = Replace(
                fixture.Run.TestPartitions,
                item => item.PartitionId == execution.PartitionId,
                item => item with { InvocationId = "different-invocation" }),
        };

        AssertIssue("invocation_mismatch", fixture.Manifest, run);
    }

    [Fact]
    public void Validate_FailsWhenDiscoveryAndTrxUseDifferentTestAssemblyHashes()
    {
        var fixture = CompleteFixture();
        var execution = fixture.Run.TestPartitions.First();
        var run = fixture.Run with
        {
            TestPartitions = Replace(
                fixture.Run.TestPartitions,
                item => item.PartitionId == execution.PartitionId,
                item => item with { TestAssemblyHash = "sha256-stale-test-assembly" }),
        };

        AssertIssue("test_assembly_provenance", fixture.Manifest, run);
    }

    [Fact]
    public void Validate_FailsWhenEvidenceUsesNonFinalProductBinaryHash()
    {
        var fixture = CompleteFixture();
        var execution = fixture.Run.TestPartitions.First();
        var run = fixture.Run with
        {
            TestPartitions = Replace(
                fixture.Run.TestPartitions,
                item => item.PartitionId == execution.PartitionId,
                item => item with { ProductBinaryHash = "sha256-stale-product" }),
        };

        AssertIssue("product_binary_provenance", fixture.Manifest, run);
    }

    /// <summary>
    /// FR-SNP-005 AC7 / TEST-SNP-002 AC6 / Phase 3 Slice 3b.
    /// Acceptance: completion evidence loads the pinned vice_xvic native-library
    /// hash from the checked-in oracle manifest and uses it on every evidence surface.
    /// </summary>
    [Fact]
    public void CompleteFixture_UsesPinnedXvicHashFromOracleManifest()
    {
        var fixture = CompleteFixture();
        var expected = ReadPinnedXvicDllHash();

        Assert.Equal(expected, fixture.Run.FinalArtifacts.NativeLibraryHashes["vice_xvic"]);
        Assert.All(
            fixture.Run.Discoveries,
            discovery => Assert.Equal(expected, discovery.NativeLibraryHashes["vice_xvic"]));
        Assert.All(
            fixture.Run.TestPartitions,
            partition => Assert.Equal(expected, partition.NativeLibraryHashes["vice_xvic"]));
        Assert.Contains(
            fixture.Run.FinalArtifacts.Files,
            file => file.Path == "native/vice_xvic.dll" && file.Hash == expected);
    }

    [Fact]
    public void Validate_FailsWhenEvidenceUsesNonFinalNativeLibraryHash()
    {
        var fixture = CompleteFixture();
        var execution = fixture.Run.TestPartitions.Single(item => item.PartitionId == ManagedPartition);
        var staleHashes = new Dictionary<string, string>(execution.NativeLibraryHashes)
        {
            ["vice_xvic"] = "sha256-stale-xvic",
        };
        var run = fixture.Run with
        {
            TestPartitions = Replace(
                fixture.Run.TestPartitions,
                item => item.PartitionId == ManagedPartition,
                item => item with { NativeLibraryHashes = staleHashes }),
        };

        AssertIssue("native_binary_provenance", fixture.Manifest, run);
    }

    [Fact]
    public void Validate_FailsWhenAlwaysFailingDiagnosticMethodIsInRequiredPartition()
    {
        var fixture = CompleteFixture();
        var identity =
            "ViceSharp.TestHarness.LiveDeployedAppSettingsBisectTests.DeployedApp_SettingsBisect_ReportsPhaseSpeeds";
        var managed = fixture.Manifest.Partitions.Single(item => item.Id == ManagedPartition);
        var selector = managed.Selector with
        {
            ExcludedIdentities = managed.Selector.ExcludedIdentities!
                .Where(item => item != identity)
                .ToArray(),
        };
        var manifest = fixture.Manifest with
        {
            Partitions = Replace(
                fixture.Manifest.Partitions,
                item => item.Id == ManagedPartition,
                item => item with { Selector = selector }),
            MethodDispositions = fixture.Manifest.MethodDispositions
                .Where(item => item.TestIdentity != identity)
                .ToArray(),
        };
        var execution = fixture.Run.TestPartitions.Single(item => item.PartitionId == ManagedPartition);
        var run = fixture.Run with
        {
            TestPartitions = Replace(
                fixture.Run.TestPartitions,
                item => item.PartitionId == ManagedPartition,
                item => WithExecutedCases(item, [.. item.ExecutedCaseIdentities, identity])),
        };

        AssertIssue("always_failing_diagnostic_selected", manifest, run);
    }

    [Fact]
    public void Validate_FailsWhenManualDispositionDoesNotNameAnExactMethod()
    {
        var fixture = CompleteFixture();
        var disposition = fixture.Manifest.MethodDispositions.First();
        var manifest = fixture.Manifest with
        {
            MethodDispositions = Replace(
                fixture.Manifest.MethodDispositions,
                item => item.TestIdentity == disposition.TestIdentity,
                item => item with
                {
                    TestIdentity = "ViceSharp.TestHarness.CaptureRunLogUtilityTests.*",
                    IsExactMethod = false,
                }),
        };

        AssertIssue("disposition_not_exact", manifest, fixture.Run);
    }

    [Fact]
    public void Validate_AcceptsCompleteManifestAndEvidence()
    {
        var fixture = CompleteFixture();

        AssertValid(fixture.Manifest, fixture.Run);
    }

    [Theory]
    [InlineData("discovery", "discovery_missing")]
    [InlineData("partition", "partition_missing")]
    [InlineData("command", "command_missing")]
    [InlineData("live", "live_evidence_missing")]
    [InlineData("ai", "ai_evidence_missing")]
    public void Validate_FailsWhenRequiredEvidenceIsOmitted(string kind, string expectedCode)
    {
        var fixture = CompleteFixture();
        var run = kind switch
        {
            "discovery" => fixture.Run with { Discoveries = fixture.Run.Discoveries.Skip(1).ToArray() },
            "partition" => fixture.Run with { TestPartitions = fixture.Run.TestPartitions.Skip(1).ToArray() },
            "command" => fixture.Run with { Commands = fixture.Run.Commands.Skip(1).ToArray() },
            "live" => fixture.Run with { LiveRuntimePasses = fixture.Run.LiveRuntimePasses.Skip(1).ToArray() },
            _ => fixture.Run with { AiReviews = fixture.Run.AiReviews.Skip(1).ToArray() },
        };

        AssertIssue(expectedCode, fixture.Manifest, run);
    }

    [Theory]
    [InlineData("invocation", "invocation_mismatch")]
    [InlineData("assembly", "test_assembly_provenance")]
    [InlineData("product", "product_binary_provenance")]
    [InlineData("native", "native_binary_provenance")]
    public void Validate_FailsWhenDiscoveryProvenanceIsStale(string kind, string expectedCode)
    {
        var fixture = CompleteFixture();
        var discovery = fixture.Run.Discoveries.First();
        var changed = kind switch
        {
            "invocation" => discovery with { InvocationId = "old-run" },
            "assembly" => discovery with { TestAssemblyHash = "old-assembly" },
            "product" => discovery with { ProductBinaryHash = "old-product" },
            _ => discovery with { NativeLibraryHashes = new Dictionary<string, string>() },
        };
        var run = fixture.Run with { Discoveries = [changed, .. fixture.Run.Discoveries.Skip(1)] };

        AssertIssue(expectedCode, fixture.Manifest, run);
    }

    [Theory]
    [InlineData("exit", "command_failed")]
    [InlineData("stdout", "command_artifact_missing")]
    [InlineData("stderr", "command_artifact_missing")]
    [InlineData("output", "command_artifact_missing")]
    public void Validate_FailsWhenCommandDidNotProduceSuccessfulEvidence(string kind, string expectedCode)
    {
        var fixture = CompleteFixture();
        var command = fixture.Run.Commands.First();
        var changed = kind switch
        {
            "exit" => command with { ExitCode = 1 },
            "stdout" => command with { StandardOutput = command.StandardOutput with { Exists = false } },
            "stderr" => command with { StandardError = command.StandardError with { IsValid = false } },
            _ => command with { OutputArtifact = null },
        };
        var run = fixture.Run with { Commands = [changed, .. fixture.Run.Commands.Skip(1)] };

        AssertIssue(expectedCode, fixture.Manifest, run);
    }

    [Theory]
    [InlineData("passed")]
    [InlineData("total")]
    [InlineData("negative")]
    public void Validate_FailsWhenCountersDisagreeWithExecutedCases(string kind)
    {
        var fixture = CompleteFixture();
        var run = WithCounters(fixture.Run, ManagedPartition, counters => kind switch
        {
            "passed" => counters with { Passed = counters.Passed - 1 },
            "total" => counters with { Total = counters.Total + 1 },
            _ => counters with { Failed = -1 },
        });

        AssertIssue("test_counter_mismatch", fixture.Manifest, run);
    }

    [Fact]
    public void Validate_FailsWhenPaidPartitionAlreadyRanInCurrentInvocation()
    {
        var fixture = CompleteFixture();
        var run = fixture.Run with
        {
            PriorPartitionExecutions = [new CompletionPriorPartitionExecution(InvocationId, PaidAiPartition)],
        };

        AssertIssue("partition_duplicate", fixture.Manifest, run);
    }

    [Theory]
    [InlineData(false, "test_process_failed")]
    [InlineData(true, "test_artifact_invalid")]
    public void Validate_FailsWhenTestProcessFailedOrStderrWasNotCaptured(bool missingLog, string expectedCode)
    {
        var fixture = CompleteFixture();
        var execution = fixture.Run.TestPartitions.First();
        var changed = missingLog
            ? execution with { StandardError = execution.StandardError with { Exists = false } }
            : execution with { ExitCode = 1 };
        var run = fixture.Run with { TestPartitions = [changed, .. fixture.Run.TestPartitions.Skip(1)] };

        AssertIssue(expectedCode, fixture.Manifest, run);
    }

    [Fact]
    public void Validate_AcceptsMatchingRepeatedCaseIdentitiesAsMultiset()
    {
        var fixture = CompleteFixture();
        var discovery = fixture.Run.Discoveries.First();
        var execution = fixture.Run.TestPartitions.First();
        var run = fixture.Run with
        {
            Discoveries = [discovery with { Cases = [.. discovery.Cases, discovery.Cases.Single()] }, .. fixture.Run.Discoveries.Skip(1)],
            TestPartitions = [WithExecutedCases(execution, [.. execution.ExecutedCaseIdentities, execution.ExecutedCaseIdentities.Single()]), .. fixture.Run.TestPartitions.Skip(1)],
        };

        AssertValid(fixture.Manifest, run);
    }

    [Theory]
    [InlineData("release-build", "native/vice_x64.dll")]
    [InlineData("xbox-publish", "artifacts/release/ViceSharp.dll")]
    public void Validate_FailsWhenCommandCitesAnotherCommandsArtifact(string commandId, string wrongPath)
    {
        var fixture = CompleteFixture();
        var wrongArtifact = fixture.Run.FinalArtifacts.Files.Single(file => file.Path == wrongPath);
        var run = fixture.Run with
        {
            Commands = Replace(fixture.Run.Commands, command => command.Id == commandId,
                command => command with { OutputArtifact = wrongArtifact }),
        };

        AssertIssue("command_artifact_mismatch", fixture.Manifest, run);
    }

    [Fact]
    public void Validate_FailsWhenRequiredMethodIsReplacedBySimilarlyNamedMethod()
    {
        var fixture = CompleteFixture();
        string lookalike = SnapshotCase.Replace("(case-1)", "Extra(case-1)", StringComparison.Ordinal);
        var run = fixture.Run with
        {
            Discoveries = Replace(fixture.Run.Discoveries, discovery => discovery.ProjectPath == HarnessProject,
                discovery => discovery with
                {
                    Cases = Replace(discovery.Cases, test => test.Identity == SnapshotCase,
                        test => test with { Identity = lookalike }),
                }),
            TestPartitions = Replace(fixture.Run.TestPartitions, execution => execution.PartitionId == ManagedPartition,
                execution => WithExecutedCases(execution,
                    execution.ExecutedCaseIdentities.Select(identity => identity == SnapshotCase ? lookalike : identity).ToArray())),
        };

        AssertIssue("required_case_missing", fixture.Manifest, run);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Validate_FailsWhenDistinctPartitionsReuseTrxEvidence(bool copiedToDifferentPath)
    {
        var fixture = CompleteFixture();
        var first = fixture.Run.TestPartitions[0];
        var second = fixture.Run.TestPartitions[1];
        var run = fixture.Run with
        {
            TestPartitions = Replace(fixture.Run.TestPartitions, execution => execution.PartitionId == second.PartitionId,
                execution => execution with
                {
                    Trx = copiedToDifferentPath
                        ? execution.Trx with { Hash = first.Trx.Hash }
                        : first.Trx,
                }),
        };

        AssertIssue("test_artifact_reused", fixture.Manifest, run);
    }

    private static void AssertIssue(
        string expectedCode,
        CompletionManifest manifest,
        CompletionRunEvidence run)
    {
        var result = CompletionTestManifestValidator.Validate(manifest, run);

        Assert.Contains(result.Issues, issue => issue.Code == expectedCode);
    }

    private static void AssertValid(
        CompletionManifest manifest,
        CompletionRunEvidence run)
    {
        var result = CompletionTestManifestValidator.Validate(manifest, run);

        Assert.True(
            result.IsValid,
            string.Join(Environment.NewLine, result.Issues.Select(issue => $"{issue.Code}: {issue.Message}")));
    }

    private static CompletionFixture CompleteFixture()
    {
        var projects = new CompletionProjectExpectation[]
        {
            new(HeadsProject, CompletionProjectKind.Test),
            new(LibraryProject, CompletionProjectKind.Test),
            new(HarnessProject, CompletionProjectKind.Test),
            new(IntegrationProject, CompletionProjectKind.Test),
            new(AiProject, CompletionProjectKind.Test),
            new(CliProject, CompletionProjectKind.Test),
            new(BenchmarksProject, CompletionProjectKind.NonTest),
        };

        var managedSelector = new CompletionTestSelector(
            "Category!=Determinism&Category!=Parity&Category!=ParityPending&Category!=ParityLegacy&Category!=Xbox"
            + string.Concat(ManualMethods.Select(identity => $"&FullyQualifiedName!~{identity}")),
            ExcludedCategories: ["Determinism", "Parity", "ParityPending", "ParityLegacy", "Xbox"],
            ExcludedIdentities: ManualMethods);

        var partitions = new CompletionPartitionExpectation[]
        {
            new("heads", HeadsProject, new CompletionTestSelector(string.Empty)),
            new("library", LibraryProject, new CompletionTestSelector(string.Empty)),
            new(ManagedPartition, HarnessProject, managedSelector),
            new(
                "test-harness-determinism",
                HarnessProject,
                new CompletionTestSelector("Category=Determinism", RequiredCategory: "Determinism")),
            new(
                "test-harness-parity",
                HarnessProject,
                new CompletionTestSelector(
                    "Category=Parity&Category!=ParityPending&Category!=ParityLegacy",
                    RequiredCategory: "Parity",
                    ExcludedCategories: ["ParityPending", "ParityLegacy"])),
            new(
                "test-harness-xbox",
                HarnessProject,
                new CompletionTestSelector("Category=Xbox", RequiredCategory: "Xbox")),
            new("romm-csdb-integration", IntegrationProject, new CompletionTestSelector(string.Empty)),
            new(
                "ai-contract",
                AiProject,
                new CompletionTestSelector(
                    "Category!=AiReview",
                    ExcludedCategories: ["AiReview"])),
            new(
                PaidAiPartition,
                AiProject,
                new CompletionTestSelector("Category=AiReview", RequiredCategory: "AiReview"),
                IsPaid: true),
            new(CliPartition, CliProject, new CompletionTestSelector(string.Empty)),
        };

        var dispositions = ManualMethods
            .Select(identity => new CompletionMethodDisposition(
                identity,
                CompletionDispositionKind.IntentionalManual,
                IsExactMethod: true,
                "Verified operator/manual or unconditional-report diagnostic."))
            .ToArray();

        var manifest = new CompletionManifest(
            projects,
            partitions,
            dispositions,
            [new CompletionCaseRequirement(
                "ViceSharp.TestHarness.SnapshotRunLogParityTests.SnapshotRun_MatchesViceRunLog",
                MinimumDiscoveredCases: 1)],
            [
                new CompletionArtifactRequirement("native-x64", "native/vice_x64.dll", true, true),
                new CompletionArtifactRequirement("native-xvic", "native/vice_xvic.dll", true, true),
                new CompletionArtifactRequirement("release-product", "artifacts/release/ViceSharp.dll", true, false),
                new CompletionArtifactRequirement("xbox-publish", "artifacts/xbox/ViceSharp.Xbox.exe", true, false),
            ],
            [
                new CompletionEvidenceRequirement("release-build", CompletionEvidenceKind.Command),
                new CompletionEvidenceRequirement("native-build", CompletionEvidenceKind.Command),
                new CompletionEvidenceRequirement("xbox-publish", CompletionEvidenceKind.Command),
                new CompletionEvidenceRequirement("traceability", CompletionEvidenceKind.Command),
                new CompletionEvidenceRequirement("diff-check", CompletionEvidenceKind.Command),
                new CompletionEvidenceRequirement("live-disabled", CompletionEvidenceKind.LiveRuntime),
                new CompletionEvidenceRequirement("live-enabled", CompletionEvidenceKind.LiveRuntime),
                new CompletionEvidenceRequirement("ai-code-review", CompletionEvidenceKind.AiReview),
                new CompletionEvidenceRequirement("ai-project-review", CompletionEvidenceKind.AiReview),
            ]);

        CompletionDiscoveredTestCase[] cases =
        [
            TestCase("ViceSharp.Heads.Tests.HeadContractTests.Builds", HeadsProject),
            TestCase("ViceSharp.Library.Tests.LibraryContractTests.Passes", LibraryProject),
            TestCase("ViceSharp.TestHarness.ManagedTests.RequiredBehavior", HarnessProject),
            TestCase(
                "ViceSharp.TestHarness.SidDeterminismTests.Deterministic",
                HarnessProject,
                "Determinism"),
            TestCase("ViceSharp.TestHarness.ParityTests.Active", HarnessProject, "Parity"),
            TestCase("ViceSharp.TestHarness.Xbox.XboxTests.Required", HarnessProject, "Xbox"),
            TestCase(SnapshotCase, HarnessProject),
            TestCase(
                "ViceSharp.TestHarness.LiveDeployedAppSpeedProbeTests.DeployedApp_Demo_Sustains_RealTime",
                HarnessProject),
            .. ManualMethods.Select(identity => TestCase(identity, HarnessProject)),
            .. Enumerable.Range(1, 7).Select(index => TestCase(
                $"ViceSharp.Library.IntegrationTests.RommLiveE2ETests.Case{index}",
                IntegrationProject,
                "Integration")),
            TestCase(
                "ViceSharp.AiReview.Tests.ReviewLogTests.Write_CreatesTimestampedMarkdown",
                AiProject),
            TestCase(
                "ViceSharp.AiReview.Tests.ProviderRoutingTests.AstraXhigh_IsSelected",
                AiProject),
            TestCase(
                "ViceSharp.AiReview.Tests.AiReviewTests.AiCodeReview",
                AiProject,
                "AiReview"),
            TestCase(
                "ViceSharp.AiReview.Tests.AiReviewTests.AiProjectReview",
                AiProject,
                "AiReview"),
            TestCase(
                "ViceSharp.RemoteControlCli.Tests.RemoteControlCommandFactoryTests.Create_Caps",
                CliProject),
            TestCase(
                "ViceSharp.RemoteControlCli.Tests.GrpcRemoteControlClientTests.GetCapabilities_UsesGrpc",
                CliProject),
        ];

        var testAssemblyHashes = projects
            .Where(project => project.Kind == CompletionProjectKind.Test)
            .ToDictionary(
                project => project.ProjectPath,
                project => $"sha256::{project.ProjectPath}",
                StringComparer.Ordinal);

        var nativeHashes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["vice_x64"] = X64Hash,
            ["vice_xvic"] = XvicHash,
        };

        var discoveries = projects
            .Where(project => project.Kind == CompletionProjectKind.Test)
            .Select(project => new CompletionDiscoveryEvidence(
                project.ProjectPath,
                InvocationId,
                testAssemblyHashes[project.ProjectPath],
                ProductHash,
                nativeHashes,
                cases.Where(test => test.ProjectPath == project.ProjectPath).ToArray()))
            .ToArray();

        var testEvidence = partitions
            .Select(partition =>
            {
                var selected = cases
                    .Where(test => test.ProjectPath == partition.ProjectPath)
                    .Where(test => Matches(partition.Selector, test))
                    .Select(test => test.Identity)
                    .ToArray();

                return SuccessfulTestEvidence(
                    partition.Id,
                    testAssemblyHashes[partition.ProjectPath],
                    nativeHashes,
                    selected);
            })
            .ToArray();

        var finalFiles = new CompletionFileEvidence[]
        {
            File("native/vice_x64.dll", X64Hash, loadable: true),
            File("native/vice_xvic.dll", XvicHash, loadable: true),
            File("artifacts/release/ViceSharp.dll", ProductHash),
            File("artifacts/xbox/ViceSharp.Xbox.exe", "sha256-xbox-final"),
        };

        var finalArtifacts = new CompletionFinalArtifacts(
            ProductHash,
            testAssemblyHashes,
            nativeHashes,
            finalFiles);

        var commands = new CompletionCommandEvidence[]
        {
            Command("release-build", finalFiles[2]),
            Command("native-build", finalFiles[0]),
            Command("xbox-publish", finalFiles[3]),
            Command("traceability", null),
            Command("diff-check", null),
        };

        var livePasses = new CompletionLiveRuntimeEvidence[]
        {
            Live("live-disabled"),
            Live("live-enabled"),
        };

        var aiReviews = new CompletionAiReviewEvidence[]
        {
            AiReview("ai-code-review"),
            AiReview("ai-project-review"),
        };

        var run = new CompletionRunEvidence(
            InvocationId,
            finalArtifacts,
            discoveries,
            testEvidence,
            commands,
            livePasses,
            aiReviews,
            []);

        return new CompletionFixture(manifest, run);
    }

    private static CompletionTestPartitionEvidence SuccessfulTestEvidence(
        string partitionId,
        string testAssemblyHash,
        IReadOnlyDictionary<string, string> nativeHashes,
        IReadOnlyList<string> cases)
    {
        var count = cases.Count;
        return new CompletionTestPartitionEvidence(
            partitionId,
            InvocationId,
            testAssemblyHash,
            ProductHash,
            nativeHashes,
            File($"TestResults/{partitionId}.trx"),
            File($"TestResults/{partitionId}.stdout.log"),
            File($"TestResults/{partitionId}.diag.log"),
            new CompletionTestCounters(count, count, count, 0, 0, 0, 0, 0, 0, 0),
            cases,
            ExitCode: 0,
            File($"TestResults/{partitionId}.stderr.log"));
    }

    private static CompletionTestPartitionEvidence WithExecutedCases(
        CompletionTestPartitionEvidence evidence,
        IReadOnlyList<string> cases)
    {
        var count = cases.Count;
        return evidence with
        {
            Counters = evidence.Counters with
            {
                Total = count,
                Executed = count,
                Passed = count,
            },
            ExecutedCaseIdentities = cases,
        };
    }

    private static CompletionRunEvidence WithCounters(
        CompletionRunEvidence run,
        string partitionId,
        Func<CompletionTestCounters, CompletionTestCounters> change)
    {
        return run with
        {
            TestPartitions = Replace(
                run.TestPartitions,
                item => item.PartitionId == partitionId,
                item => item with { Counters = change(item.Counters) }),
        };
    }

    private static CompletionRunEvidence WithFinalFile(
        CompletionRunEvidence run,
        string path,
        Func<CompletionFileEvidence, CompletionFileEvidence> change)
    {
        return run with
        {
            FinalArtifacts = run.FinalArtifacts with
            {
                Files = Replace(
                    run.FinalArtifacts.Files,
                    file => file.Path == path,
                    change),
            },
        };
    }

    private static CompletionDiscoveredTestCase TestCase(
        string identity,
        string project,
        params string[] categories) =>
        new(identity, project, categories);

    private static CompletionFileEvidence File(
        string path,
        string? hash = null,
        bool loadable = true) =>
        new(path, Exists: true, Length: 128, IsValid: true, IsLoadable: loadable, hash ?? $"sha256::{path}");

    private static CompletionCommandEvidence Command(
        string id,
        CompletionFileEvidence? outputArtifact) =>
        new(
            id,
            InvocationId,
            ExitCode: 0,
            File($"validation-output/{id}.stdout.log"),
            File($"validation-output/{id}.stderr.log"),
            outputArtifact,
            TestCounters: null);

    private static CompletionLiveRuntimeEvidence Live(string id) =>
        new(
            id,
            InvocationId,
            ProductHash,
            HasProcessIdentity: true,
            HasDebugAttachSession: true,
            HasRemoteControlCapabilities: true,
            HasDraftAcceptedActiveState: true,
            HasRuntimeObservedEffects: true,
            HasReadyGeometryClockPacingWarp: true,
            HasScreenshotsAndHashes: true);

    private static CompletionAiReviewEvidence AiReview(string id) =>
        new(
            id,
            InvocationId,
            Model: "Astra",
            Effort: "xhigh",
            Status: "completed",
            File($"validation-output/{id}.md"),
            Verdict: "AGREE");

    private static string ReadPinnedXvicDllHash()
    {
        var path = RepoPaths.File(
            Path.Combine("docs", "requirements", "oracles", "vic20-xvic-manifest.json"));
        using var document = JsonDocument.Parse(System.IO.File.ReadAllText(path));
        return document.RootElement.GetProperty("viceXvicDllSha256").GetString()
            ?? throw new InvalidOperationException(
                $"Pinned xvic oracle manifest does not define viceXvicDllSha256: '{path}'.");
    }

    private static bool Matches(
        CompletionTestSelector selector,
        CompletionDiscoveredTestCase test)
    {
        if (selector.RequiredCategory is not null
            && !test.Categories.Contains(selector.RequiredCategory, StringComparer.Ordinal))
        {
            return false;
        }

        if (selector.ExcludedCategories is not null
            && selector.ExcludedCategories.Any(
                category => test.Categories.Contains(category, StringComparer.Ordinal)))
        {
            return false;
        }

        if (selector.ExcludedIdentities is not null
            && selector.ExcludedIdentities.Contains(test.Identity, StringComparer.Ordinal))
        {
            return false;
        }

        return selector.IncludedIdentityPrefix is null
            || test.Identity.StartsWith(selector.IncludedIdentityPrefix, StringComparison.Ordinal);
    }

    private static T[] Replace<T>(
        IReadOnlyList<T> source,
        Func<T, bool> predicate,
        Func<T, T> replacement) =>
        source.Select(item => predicate(item) ? replacement(item) : item).ToArray();

    private sealed record CompletionFixture(
        CompletionManifest Manifest,
        CompletionRunEvidence Run);
}
