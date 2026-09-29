# Slice 0 completion-test inventory

Recorded: 2026-09-06 21:52:00 -05:00
Agent: GPT-5.6 Sol High
Mode: bounded inventory and design only

## Approved contract

The governing plan is `docs/plans/PLAN-GROK-COMPLETION-20260906.md`. This receipt covers only the pre-implementation inventory for Slice 0. No product source, build source, test source, dependency, native source, MCP requirement, secret, or external service was changed or invoked.

## Git state

- Root branch: `main...origin/main`; live divergence was `0 0`.
- Root porcelain inventory: 399 entries, comprising 37 tracked changes and 362 untracked entries.
- Native submodule: detached HEAD with 1,160 porcelain entries, comprising 12 tracked changes and 1,148 untracked entries.
- The existing working tree includes unrelated work in package, handoff, requirements, Avalonia, Host, Protocol, TestHarness, CLI, and validation-output paths.
- All existing changes are preserved. Completion-test work must stay within the owned paths listed below and must never clean or reset the root or native worktree.

## Proposed Slice 0 ownership

- `build/Build.cs`
- `build/CompletionTestManifest.cs`
- `build/completion-test-manifest.json`
- `tests/ViceSharp.Library.Tests/CompletionTestManifestTests.cs`
- `tests/ViceSharp.Library.Tests/ViceSharp.Library.Tests.csproj`, only if needed to compile-link `../../build/CompletionTestManifest.cs` into the test assembly
- `validation-output/codex-grok-completion-20260906/sol-slice0-inventory.md`

The test project should link the pure validator source rather than reference the Nuke build project, avoiding a build-project dependency cycle.

## Project and suite inventory

`ViceSharp.slnx` contains 30 projects: 24 production/tool projects and 6 entries under the solution's tests folder. There are 34 non-native `.csproj` files in the repository.

Required test projects:

- `tests/ViceSharp.AiReview.Tests/ViceSharp.AiReview.Tests.csproj`: 3 contract Facts in `ReviewLogTests`; 2 paid theories in `AiReviewTests`.
- `tests/ViceSharp.Heads.Tests/ViceSharp.Heads.Tests.csproj`: 2 Facts and 1 Theory; Category `Heads`.
- `tests/ViceSharp.Library.IntegrationTests/ViceSharp.Library.IntegrationTests.csproj`: exactly 7 Facts; Category `Integration`.
- `tests/ViceSharp.Library.Tests/ViceSharp.Library.Tests.csproj`: 75 Facts and 5 Theories by static declaration count.
- `tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj`: 2,385 Facts and 108 Theories by static declaration count.
- `tests/ViceSharp.RemoteControlCli.Tests/ViceSharp.RemoteControlCli.Tests.csproj`: currently 6 Facts; currently untracked and absent from the solution. Completion uses final discovery and does not freeze this count because Slice 6 adds gRPC coverage.

Intentional non-test project:

- `tests/ViceSharp.Benchmarks/ViceSharp.Benchmarks.csproj`: `IsTestProject=false`; keep as diagnostics/benchmarks, outside completion test counting.

Current out-of-solution tool:

- `tools/ViceSharp.RemoteControlCli/ViceSharp.RemoteControlCli.csproj`: currently untracked and absent from the solution. Slice 6 owns its dependency/API correction and solution membership.

## Existing Nuke coverage and exclusions

`build/Build.cs` currently defines:

- `Test` and `CiTest` with `Category!=Determinism&Category!=AiReview&Category!=ParityPending&Category!=ParityLegacy&Category!=Integration`.
- `DeterminismTest` with `Category=Determinism`.
- `ParityTest` with `Category=Parity`; because pending cases also receive `Category=Parity`, this currently includes pending parity.
- `ValidateXbox`, which runs `Category=Xbox` and publishes the workload-free `ViceSharpXboxUwp=false` win-x64 Native-AOT fallback head.

The existing `Test` target excludes Determinism, AiReview, ParityPending, ParityLegacy, and Integration. It does not exclude active Parity or Xbox, so a partitioned completion run must explicitly exclude those categories from the managed partition to prevent duplicate execution.

Xbox/UWP is outside the new product feature scope, but existing Xbox tests and public head behavior remain supported. `ValidateXbox` is the existing repository target that proves both the Category Xbox contract and publishability of the workload-free fallback head. Completion should invoke that existing target behavior. It should not require the UWP workload, packaging, device deployment, or `PublishXbox`.

## Category and skip inventory

- `ParityAcAttribute` always emits Category `Parity`; it also emits Category `ParityPending` when `pending: true`.
- There are 466 authored `[ParityAc]` declarations, pinned by `ParityCoverageManifestTests`.
- One current pending parity declaration exists: `tests/ViceSharp.TestHarness/VicCycleDivergentParityTests.cs:357`.
- Two legacy parity classes exist: `tests/ViceSharp.TestHarness/RasterBarRendererTests.cs:27` and `tests/ViceSharp.TestHarness/VideoRendererTests.cs:12`.
- Determinism is represented by `tests/ViceSharp.TestHarness/SidDeterminismTests.cs:26`.
- Xbox has 220 source trait occurrences by bounded static search.
- TestHarness has 167 `[ViceFact]` and 46 `[ViceTheory]` uses. Both custom attributes set `Skip` when `ViceNative.IsAvailable` is false, so native preflight and a zero-skips TRX rule are required.
- TestHarness contains 30 files with skip tokens, 79 `Assert.Skip*` call tokens, and 4 `Skip=` tokens. Some are comments/test strings; conditional product tests must either meet their prerequisites or make completion fail.

Files reviewed for method-level classification; filenames alone do not determine disposition:

- `CaptureRunLogUtilityTests.cs`
- `ClockThroughputBenchmarkTests.cs`
- `DemoWorkloadSpeedDiagTests.cs`
- `LiveDeployedAppSettingsBisectTests.cs`
- `LiveDeployedAppSpeedProbeTests.cs`
- `LiveHandlerTraceTests.cs`
- `LiveLimiterBandProbeTests.cs`
- `NativeResidueDiagTests.cs`
- `SidEngineClockingProbeTests.cs`
- `SnapshotResumeSpikeTests.cs`
- `SnapshotRunLogParityTests.cs`
- `WinMmAudioThroughputDiagTests.cs`

The manifest must never make an exclusion sufficient for completion. Dispositions apply to discovered test methods, not filenames. A `required` or `unresolved` disposition fails preflight. Only the six intentional manual/throwing methods named below are planned non-product exclusions. Pending and legacy parity remain blocking until fixed, converted to active coverage, or removed under an approved requirement disposition. The runner compares discovered test identities with partition selectors and rejects zero matches, multiple matches, unlisted exclusions, and whole-file exclusions.

## Native and artifact prerequisites

- `tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj:68` invokes `native/Vice.Native.proj` before build.
- `native/Vice.Native.proj` produces `native/vice_x64.dll` and invokes `native/build-vice-shim.ps1` on Windows.
- The native build script requires `C:\msys64\usr\bin\bash.exe` and explicitly sets `MSYSTEM=MINGW64`.
- `SkipIfNoBuildArtifactAttribute` skips when an expected executable is absent.
- `VICESHARP_XMLDOCS_ENFORCE=1` is the opt-in XML documentation enforcement switch found in the tests.

Completion must validate native output presence/loadability and all required build artifacts before test execution. A missing prerequisite must fail before tests can convert it into skips. All required partitions then enforce zero skipped tests.

Environment variable names discovered by bounded source inspection:

- `VICE_DATA_PATH`
- `VICESHARP_AUDIO`
- `VICESHARP_CAPTURE_CYCLES`
- `VICESHARP_CAPTURE_OUT`
- `VICESHARP_CAPTURE_SECONDS`
- `VICESHARP_CAPTURE_VSF`
- `VICESHARP_CLOCK_BENCH`
- `VICESHARP_CSDB_BRIDGE_URL`
- `VICESHARP_GRPC_REFLECTION`
- `VICESHARP_LIVE_PROBE`
- `VICESHARP_LIVE_PROBE_MIN`
- `VICESHARP_LOCKSTEP_10S`
- `VICESHARP_LOCKSTEP_2S`
- `VICESHARP_LOCKSTEP_DUMP`
- `VICESHARP_LOCKSTEP_DUMP_FILE`
- `VICESHARP_LOCKSTEP_VIDEO`
- `VICESHARP_RESIDUE_STEPS`
- `VICESHARP_ROM_PATH`
- `VICESHARP_ROMM_INTEGRATION`
- `VICESHARP_ROMM_TOKEN`
- `VICESHARP_ROMM_URL`
- `VICESHARP_ROMM_USER_ID`
- `VICESHARP_XMLDOCS_ENFORCE`

This inventory records names only. It does not read or print values.

## Proposed completion manifest groups

Each required manifest entry is selected once per CompletionTest invocation. A later corrected rerun is a new invocation:

1. Release build of `ViceSharp.slnx`.
2. Entire Heads test project.
3. Entire Library unit-test project.
4. TestHarness managed partition using the exact filter recorded below. It excludes separately executed categories and six individually named intentional manual/throwing methods; actual performance, live, snapshot, and native tests remain required.
5. AI review contract partition using `Category!=AiReview`, covering all current and future non-paid tests in the project, including routing tests.
6. Entire final RemoteControl CLI test project with no test filter. The current six cases are an inventory baseline; the completion expectation is discovery-based after Slice 6 adds gRPC tests.
7. Determinism partition, `Category=Determinism`.
8. Active parity partition, `Category=Parity&Category!=ParityPending&Category!=ParityLegacy`.
9. Existing Xbox validation: Category Xbox tests plus workload-free fallback Native-AOT publish.
10. RomM/CSDb integration, expected exactly 7.
11. Paid AI review filtered to `AiReviewTests`, expected exactly 2 theories in one invocation.
12. Requirement traceability audit.
13. The two required live-app evidence records from the approved plan.

The seven RomM/CSDb cases are heartbeat, LAN discovery, C64 platform resolution, C64 browse, first launchable detail/download, collections round-trip, and CSDb bridge search. They currently depend on the opt-in integration flag plus RomM URL, token, user ID, and CSDb bridge URL. Final completion requires seven passes and zero skips.

The paid AI theories intentionally do not fail solely on review text. Completion must therefore validate the expected retained review artifacts and hostile verdict independently of the xUnit exit code. Both reviews use Astra xhigh; Sol High is coding only.

## Runner acceptance rules

For every test process:

- Exit code must be zero.
- Required TRX, stdout/stderr capture, and VSTest diagnostic log must exist and be nonempty.
- The successful TRX case-identity multiset must exactly equal the partition's complete final discovered expected multiset. Missing, extra, or duplicate cases fail even when aggregate counters are green.
- Failed, skipped, aborted, error, timeout, not-executed, and inconclusive counters must all be zero.
- Long-running test commands use retained `--blame-hang` evidence; a stopped or hung command cannot pass.
- Required native/build preflight failures block execution rather than allowing skip-based success.
- Missing coverage artifacts, missing live evidence, or an unlisted project/category/exclusion block completion.
- Within one CompletionTest invocation, each partition and discovered test identity is selected once. A corrected rerun after a failure or code change is allowed and produces a new invocation receipt.
- The completion run writes a durable per-partition result record and an aggregate verdict; the aggregate succeeds only if every manifest entry is satisfied.

The named planned failing regression tests for the pure validator are recorded below.

## Canonical requirement reconciliation

Use the root-approved IDs exactly:

- `FR-COMPLETION-001`, `TR-COMPLETION-MANIFEST-001`, `TEST-COMPLETION-001`
- `FR-SETTINGS-TXN-001`, `TR-SETTINGS-TXN-001`, `TEST-SETTINGS-TXN-001`
- `FR-NATIVE-LIFECYCLE-001`, `TR-NATIVE-LIFECYCLE-001`, `TEST-NATIVE-LIFECYCLE-001`
- `FR-REMOTECTRL-CLI-001`, `TR-REMOTECTRL-CLI-001`, `TEST-REMOTECTRL-CLI-001`
- `FR-AIREVIEW-ROUTING-001`, `TR-AIREVIEW-ROUTING-001`, `TEST-AIREVIEW-ROUTING-001`
- `TR-BASELINE-CORRECTNESS-001`, `TEST-BASELINE-CORRECTNESS-001`
- `TR-UI-COREBOUNDARY-001`, `TEST-UI-COREBOUNDARY-001`
- `TR-REMOTE-INTEGRATION-001`, `TEST-REMOTE-INTEGRATION-001`

Existing canonical IDs confirmed present include `FR-WARP-001`, `TR-WARP-STATUS-001`, `TEST-UISET-001`, `TEST-UISET-002`, `TEST-VIC20-FLASH-001`, `TEST-VIC20-SOUND-001`, `TEST-ROMM-SEC-001`, and `TR-UI-SHELL-001`.

The following plan IDs were absent from the bounded canonical requirements search and need restoration/export rather than renaming:

- `FR-MACHINEPICKER-001`
- `FR-UISETVIS-001`
- `TR-UIAXAML-PICKER-001`
- `TEST-UISET-003`
- `TEST-REMOTECTRL-001`

Canonical files for the supplemental records are the appropriate existing functional documents under `docs/requirements/functional/`, `docs/requirements/technical/TR-Build-System.md`, `TR-GRPC-Boundary.md`, `TR-UI-Shell.md`, `TR-VIC20.md`, and `docs/requirements/test/TEST-Requirements.md`. MCP is authoritative; root owns requirement persistence and generated exports.

## Bounded commands used

- `git status --short --branch`
- `git rev-list --left-right --count HEAD...origin/main`
- `git -c safe.directory=F:/GitHub/vice-sharp/native/vice -C native/vice status --short --branch`
- `rg --files -g '*.csproj' -g '!native/**'`
- `dotnet sln ViceSharp.slnx list`
- `rg -n` over the named build, test-project, attribute, and requirement files for `Category`, `Trait`, `Fact`, `Theory`, `Skip`, `ParityAc`, and the explicit requirement IDs.
- `Show-TextFiles` for the bounded project, build, plan, attribute, and requirement files needed to interpret those results.

No build, test, native build, paid AI call, remote integration call, service call, commit, or push was performed during this inventory.
## Requirements-phase manifest corrections

Recorded: 2026-09-06 22:08:00 -05:00

### Exact discovery and partition selectors

Before executing tests, CompletionTest performs and retains per-project test discovery. Each final discovered identity must match exactly one executable partition or one individually named intentional non-product disposition. Zero matches, multiple matches, a newly discovered project, or an unlisted category fails manifest validation.

- Heads: project `tests/ViceSharp.Heads.Tests/ViceSharp.Heads.Tests.csproj`; no filter.
- Library: project `tests/ViceSharp.Library.Tests/ViceSharp.Library.Tests.csproj`; no filter.
- TestHarness managed: project `tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj`; filter `Category!=Determinism&Category!=Parity&Category!=ParityPending&Category!=ParityLegacy&Category!=Xbox&FullyQualifiedName!~ViceSharp.TestHarness.CaptureRunLogUtilityTests.CaptureBaselineRunLog_FromEnvironment&FullyQualifiedName!~ViceSharp.TestHarness.LiveHandlerTraceTests.Trace_LiveIrqHandler_FromUserSnapshot&FullyQualifiedName!~ViceSharp.TestHarness.LiveDeployedAppSettingsBisectTests.DeployedApp_SettingsBisect_ReportsPhaseSpeeds&FullyQualifiedName!~ViceSharp.TestHarness.LiveLimiterBandProbeTests.DeployedApp_LimiterBand_ReportsAchievedSpeeds&FullyQualifiedName!~ViceSharp.TestHarness.Vic20.BranchCycleCountTests.TakenBne_CycleTrace&FullyQualifiedName!~ViceSharp.TestHarness.Vic20.BranchCycleCountTests.NotTakenBne_CycleTrace`.
- TestHarness determinism: same project; filter `Category=Determinism`.
- TestHarness active parity: same project; filter `Category=Parity&Category!=ParityPending&Category!=ParityLegacy`.
- TestHarness Xbox: same project; filter `Category=Xbox`, followed by the workload-free `ViceSharpXboxUwp=false`, `PublishAot=true`, `win-x64` publish command.
- RomM/CSDb integration: project `tests/ViceSharp.Library.IntegrationTests/ViceSharp.Library.IntegrationTests.csproj`; no filter; final expectation remains exactly seven discovered and seven passed cases.
- AI contract/routing: project `tests/ViceSharp.AiReview.Tests/ViceSharp.AiReview.Tests.csproj`; filter `Category!=AiReview`. This selects every current and future non-paid test, including future routing tests.
- AI paid review: same project; filter `Category=AiReview`. The final manifest expects the two approved theories and separately validates both Astra/xhigh artifacts.
- RemoteControl CLI: project `tests/ViceSharp.RemoteControlCli.Tests/ViceSharp.RemoteControlCli.Tests.csproj`; no filter and no frozen count. The runner records and executes every final discovered case after Slice 6.
- Benchmarks: `tests/ViceSharp.Benchmarks/ViceSharp.Benchmarks.csproj` is reconciled as `IsTestProject=false`, not a test partition.

The ordinary managed partition excludes Category Parity entirely because active parity has its own partition. Category ParityPending and Category ParityLegacy remain explicit blocking dispositions until later approved slices resolve them. The six method exclusions below are the only planned intentional non-product test dispositions. All other discovered TestHarness identities remain required.

### Method-level dispositions

Intentional operator/manual methods:

- `ViceSharp.TestHarness.CaptureRunLogUtilityTests.CaptureBaselineRunLog_FromEnvironment`: operator baseline-generation utility; it is idle by design without `VICESHARP_CAPTURE_*` input.
- `ViceSharp.TestHarness.LiveHandlerTraceTests.Trace_LiveIrqHandler_FromUserSnapshot`: exploratory output-only trace tied to a hard-coded user-staged snapshot.
- `ViceSharp.TestHarness.LiveDeployedAppSettingsBisectTests.DeployedApp_SettingsBisect_ReportsPhaseSpeeds`: opt-in XMLDoc-declared diagnostic that unconditionally calls `Assert.Fail` with its phase report.
- `ViceSharp.TestHarness.LiveLimiterBandProbeTests.DeployedApp_LimiterBand_ReportsAchievedSpeeds`: opt-in XMLDoc-declared diagnostic that unconditionally calls `Assert.Fail` with its measured limiter-band report.
- `ViceSharp.TestHarness.Vic20.BranchCycleCountTests.TakenBne_CycleTrace`: `Explicit=true` trace probe that always throws `XunitException` to print its trace.
- `ViceSharp.TestHarness.Vic20.BranchCycleCountTests.NotTakenBne_CycleTrace`: `Explicit=true` trace probe that always throws `XunitException` to print its trace.

These dispositions are method identities. The manifest rejects a file/class wildcard disposition.

Required performance, live, snapshot, and native identities include all discovered tests in:

- `ClockThroughputBenchmarkTests`: `MeasureGuiFramePumpThroughput`, `MeasureC64ChipsetFrameCost`, `MeasurePumpLoopPacedClock`, `MeasurePumpLoopPacedClockUnderPullLoad`, and `MeasureVicePacedClock`.
- `DemoWorkloadSpeedDiagTests`: `Demo_SilentWarp_Measures_Core_Headroom` and `Demo_UserConfig_LiveAudio_ViceGate_Sustains_RealTime`.
- `LiveDeployedAppSpeedProbeTests.DeployedApp_Demo_Sustains_RealTime`.
- `NativeResidueDiagTests`: all four `FreshMachine_*` tests.
- `SidEngineClockingProbeTests.Probe_NativeReSidInternalStateAdvancesOnStep`.
- `SnapshotResumeSpikeTests`: all three discovered tests.
- `SnapshotRunLogParityTests`: all six discovered tests, including a nonempty data set for `SnapshotRun_MatchesViceRunLog`.
- `WinMmAudioThroughputDiagTests.WinMm_Backend_Accepts_Samples_At_Device_Rate`.

Their prerequisite switches, fixtures, live process, audio backend, ffmpeg, and native oracles must be supplied. A skip or a theory with no discovered data remains a completion failure.

### Native preflight

Completion requires both C64 and VIC-20 native oracles:

- `native/vice_x64.dll` must exist, be nonempty, load through the product resolver, and create/reset a C64 instance.
- `native/vice_xvic.dll` must exist, be nonempty, load through the product resolver, and create/reset a VIC-20 instance.
- Required sibling runtime libraries must be present and resolvable.
- TestHarness output must resolve the same built binaries used by the completion run.

The current `native/Vice.Native.proj` declares only `vice_x64.dll` as an output and the Windows wrapper invokes only `build-vice-shim.sh`. The repository also contains `native/build-vice-shim-xvic.sh`. Until the approved native slice makes the dual-oracle build first-class, this remains a blocking manifest prerequisite. CompletionTest must use the supported native scripts and explicit MSYS2 Bash path; it must not treat the current VIC-20 skip-by-return tests as loadability evidence.

The planned executable loadability check is `NativeOracleAvailabilityTests.BothC64AndVic20Oracles_ArePresentLoadableAndCallable`, added in the approved native-lifecycle test slice. It asserts product-level availability and a minimal create/reset/state operation for each oracle.

### Evidence contracts

Test-process evidence requires exit code zero, nonempty TRX, captured stdout/stderr, VSTest diagnostic log, blame-hang evidence when configured, zero failed/skipped/aborted/error/timeout/not-executed/inconclusive results, and exact equality between the TRX case-identity multiset and the complete final discovered expected multiset. Evidence must carry the current CompletionTest invocation ID and hashes of the final test assembly and applicable product/native binaries.

Command evidence applies to solution build, native build, Xbox Native-AOT publish, requirement traceability, and `git diff --check`. Each command record requires the exact command/arguments, start/end timestamps, exit code zero, captured stdout/stderr, and declared output-artifact checks. TRX counters do not apply to these commands.

Typed runtime evidence applies to each of the two fresh live application passes. Each record must identify the launched binary/configuration, process and refreshed debug-attach/session identity, RemoteControl enabled/disabled state and capabilities, automation IDs/actions, before/after Draft/Accepted/Active settings values, restart boundary, runtime-observed effects, selected machine/profile, READY state, geometry/clock/pacing/Warp data, screenshot/artifact paths and hashes, and final pass/fail assertions. A screenshot or UI value without matching runtime state is insufficient.

AI artifact evidence records model `Astra`, effort `xhigh`, review kind, completed status, durable artifact/log path and hash, and accepted hostile verdict. The xUnit `NeverFails` result alone cannot satisfy this evidence.

Each partition is selected at most once within a single CompletionTest invocation. A later rerun after a correction is allowed and receives a new invocation ID and evidence directory.

### Planned failing validator tests

The first implementation step creates these failing tests in `CompletionTestManifestTests`:

- `Validate_FailsWhenDiscoveredTestProjectIsMissingFromManifest`
- `Validate_FailsWhenDiscoveredTestIdentityMatchesNoPartition`
- `Validate_FailsWhenDiscoveredTestIdentityMatchesMultiplePartitions`
- `Validate_FailsWhenExclusionUsesFileOrClassWildcard`
- `Validate_FailsWhenCliExpectedCountIsFrozen`
- `Validate_AcceptsNewCliCaseThroughDiscoveryBasedPartition`
- `Validate_FailsWhenAiContractPartitionOmitsFutureNonAiReviewTest`
- `Validate_FailsWhenPaidAiIdentityAlsoMatchesContractPartition`
- `Validate_FailsWhenRequiredTestArtifactIsMissing`
- `Validate_FailsWhenTrxHasZeroExecutedTests`
- `Validate_FailsWhenTrxContainsSkippedTest`
- `Validate_FailsWhenTrxContainsFailedTest`
- `Validate_FailsWhenTrxContainsAbortedOrIncompleteTest`
- `Validate_FailsWhenPaidPartitionIsSelectedTwiceInOneInvocation`
- `Validate_AllowsPaidPartitionRerunInNewInvocation`
- `Validate_FailsWhenDispositionIsUnresolved`
- `Validate_FailsWhenPendingOrLegacyParityIsExcludedAsComplete`
- `Validate_FailsWhenViceX64ArtifactIsMissingOrEmpty`
- `Validate_FailsWhenViceXvicArtifactIsMissingOrEmpty`
- `Validate_FailsWhenEitherNativeOracleIsNotLoadable`
- `Validate_FailsWhenRequiredBuildOrPublishArtifactIsMissing`
- `Validate_FailsWhenCommandEvidenceUsesTestCountersInsteadOfCommandContract`
- `Validate_FailsWhenLivePassLacksTypedRuntimeEvidence`
- `Validate_FailsWhenAiReviewArtifactLacksAstraXhighMetadata`
- `Validate_FailsWhenSnapshotRunLogTheoryHasNoDiscoveredData`

This correction preserves the approved completion plan. It changes only the proposed Slice 0 manifest design in response to requirements-phase review.

## Final requirements-phase corrections

Recorded: 2026-09-06 22:12:05 -05:00

Live source verification confirmed two additional intentional always-failing diagnostics:

- `LiveDeployedAppSettingsBisectTests.DeployedApp_SettingsBisect_ReportsPhaseSpeeds` states in XMLDocs and its opt-in message that it always fails with a report, then unconditionally calls `Assert.Fail` at source line 90.
- `LiveLimiterBandProbeTests.DeployedApp_LimiterBand_ReportsAchievedSpeeds` states in XMLDocs that it always fails with its report, then unconditionally calls `Assert.Fail` at source line 73.

They join the four previously identified method-level manual dispositions, producing six exact-method dispositions. The actual live runtime/performance coverage remains required, including `LiveDeployedAppSpeedProbeTests.DeployedApp_Demo_Sustains_RealTime`, both `DemoWorkloadSpeedDiagTests` methods, and all other required methods listed above.

Successful partition reconciliation is exact:

- Discovery runs after the final build and records the final test-assembly hash plus applicable product/native binary hashes.
- Each partition records the same CompletionTest invocation ID and the same final-binary provenance.
- The expected value is the complete normalized discovered test-case identity multiset selected by that partition's filter.
- The actual value is the complete normalized successful TRX test-case identity multiset.
- Expected and actual multisets must be equal. A missing case, unexpected case, excess duplicate occurrence, or different multiplicity fails the partition even if all aggregate counters are green.
- Discovery and execution from different invocation IDs, test assembly hashes, product binary hashes, or native-library hashes fail.
- A rerun after a correction performs new discovery and uses a new invocation ID; it cannot reuse a prior invocation's TRX or artifacts.

Additional planned failing validator tests:

- `Validate_FailsWhenTrxOmitsDiscoveredExpectedCase`
- `Validate_FailsWhenTrxContainsUnexpectedCase`
- `Validate_FailsWhenTrxContainsExcessDuplicateCase`
- `Validate_FailsWhenTrxCaseMultiplicityDiffersFromDiscovery`
- `Validate_FailsWhenEvidenceInvocationIdDiffersFromCurrentInvocation`
- `Validate_FailsWhenDiscoveryAndTrxUseDifferentTestAssemblyHashes`
- `Validate_FailsWhenEvidenceUsesNonFinalProductBinaryHash`
- `Validate_FailsWhenEvidenceUsesNonFinalNativeLibraryHash`
- `Validate_FailsWhenAlwaysFailingDiagnosticMethodIsInRequiredPartition`
- `Validate_FailsWhenManualDispositionDoesNotNameAnExactMethod`

These corrections remain within the approved exhaustive-test and manual-diagnostic scope. They do not amend the approved completion plan.

