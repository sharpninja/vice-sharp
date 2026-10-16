# Grok completion acceptance-criteria matrix

Status: requirements phase preparation. No test, slice, or goal completion is claimed.

Authority: [approved plan](../../plans/PLAN-GROK-COMPLETION-20260906.md); effective MCP workspace layer `layer-1`; [Sol inventory](../../../validation-output/codex-grok-completion-20260906/sol-slice0-inventory.md).

Test additions are planned until source and retained execution receipts exist. Final discovery counts determine execution scope.

## Slice 1: Bounded and isolated native oracle lifecycle

Planned executable scope:

- Vic20SoundLockstep, Vic20NativeAudioExportTests and Vic20NativeLockstepTests
- NativeLifecycleTests (planned child faults and repeated sequence)

Required evidence: Exact PCM counts/bytes; repeated same-process PAL/NTSC; child failure/deadline/poison; pinned patch/build/loaded-DLL hashes.

### FR-NATIVE-LIFECYCLE-001

- **FR-NATIVE-LIFECYCLE-001/ac-01**: Native VIC-20 sound-oracle reset preserves the live mixer timing and clocks. Evidence: pending in executable scope above.
- **FR-NATIVE-LIFECYCLE-001/ac-02**: Create and step finish or fail within 5000 ms; stop finishes or fails within 2000 ms. Evidence: pending in executable scope above.
- **FR-NATIVE-LIFECYCLE-001/ac-03**: Dispose is idempotent; a live worker never observes freed state. Failed stop poisons shared native-library state and all later operations fail immediately. Evidence: pending in executable scope above.
- **FR-NATIVE-LIFECYCLE-001/ac-04**: Reset, destroy, snapshot read and snapshot write apply the same shutdown-failure policy. Evidence: pending in executable scope above.
- **FR-NATIVE-LIFECYCLE-001/ac-05**: Normal audio, dual-VIA, reset, snapshot and dispose work repeatedly in one process for PAL and NTSC. Evidence: pending in executable scope above.

### TR-NATIVE-LIFECYCLE-001

- **TR-NATIVE-LIFECYCLE-001/ac-01**: Oracle sound state is separate from live mixer state; no reset memset clears playback timing. Evidence: pending in executable scope above.
- **TR-NATIVE-LIFECYCLE-001/ac-02**: Use absolute monotonic deadlines for create/step 5000 ms and stop 2000 ms; remove infinite waits and propagate native failure to managed callers. Evidence: pending in executable scope above.
- **TR-NATIVE-LIFECYCLE-001/ac-03**: Poison belongs to the shared loaded native library, not only one wrapper instance; retain memory if its worker is still alive. Evidence: pending in executable scope above.
- **TR-NATIVE-LIFECYCLE-001/ac-04**: Capture all required modified native source in the supported patch against the pinned clean native gitlink; retain patch/build and loaded-DLL SHA256 receipts. Evidence: pending in executable scope above.
- **TR-NATIVE-LIFECYCLE-001/ac-05**: Audio comparisons require identical sample counts and complete PCM sequences; truncated Math.Min comparisons cannot establish parity. Evidence: pending in executable scope above.

### TEST-NATIVE-LIFECYCLE-001

- **TEST-NATIVE-LIFECYCLE-001/ac-01**: Add red regressions proving oracle reset does not zero live playback timing, then exact-length PCM equality for PAL and NTSC. Evidence: pending in executable scope above.
- **TEST-NATIVE-LIFECYCLE-001/ac-02**: Run AudioThenDualViaResetSnapshotDispose repeatedly in one process for PAL and NTSC, including independent and post-sequence snapshot tests. Evidence: pending in executable scope above.
- **TEST-NATIVE-LIFECYCLE-001/ac-03**: Run create/step timeout, stop timeout, poison-after-failure and repeat-dispose fault cases in owned child processes; assert bounded wall time and immediate later failure. Evidence: pending in executable scope above.
- **TEST-NATIVE-LIFECYCLE-001/ac-04**: Require native prerequisites, selected workload flags and actual cycle budgets; missing DLLs/ROMs or disabled required workloads fail instead of returning early. Restore environment variables. Evidence: pending in executable scope above.
- **TEST-NATIVE-LIFECYCLE-001/ac-05**: Build the supported patch from the pinned clean native source and verify the hash of the DLL loaded by the passing native tests. Evidence: pending in executable scope above.


## Slice 4: Truthful transactional Settings state

Planned executable scope:

- AttachPanelViewModelTests, GrpcHostServiceAdaptersTests, WarpModeTests
- SettingsTransactionTests (planned state/failure/concurrency cases)

Required evidence: Active/Accepted/Draft matrix; failure injection/rollback; protobuf compatibility; merge races; persistence; actual cart/uIEC effects.

### FR-SETTINGS-TXN-001

- **FR-SETTINGS-TXN-001/ac-01**: Expose Active runtime settings, Accepted last accepted target including staged restart fields, and local unsaved Draft independently. Evidence: pending in executable scope above.
- **FR-SETTINGS-TXN-001/ac-02**: Dirty compares normalized Draft to Accepted. Restart-required compares restart-relevant Draft to Active. Evidence: pending in executable scope above.
- **FR-SETTINGS-TXN-001/ac-03**: Plain Apply applies live fields, accepts the complete target, clears dirty and retains a pending restart. Revert restores Accepted and does not cancel an earlier accepted restart. Evidence: pending in executable scope above.
- **FR-SETTINGS-TXN-001/ac-04**: Apply + Restart publishes a prepared replacement and then makes Active and Accepted equal. Any failure preserves prior runtime, accepted state and unsaved draft. Evidence: pending in executable scope above.
- **FR-SETTINGS-TXN-001/ac-05**: Validate profile/family, RAM, limiter, resources, cartridge image/preset and IEC root/unit/reserved-device conflicts before any externally visible mutation. Evidence: pending in executable scope above.
- **FR-SETTINGS-TXN-001/ac-06**: Derive active cartridge kind from the actual device. A non-none kind needs a compatible image; none detaches. Preserve image, banks, preset, writeback and uIEC across applicable restart. Evidence: pending in executable scope above.
- **FR-SETTINGS-TXN-001/ac-07**: Apply Warp through the existing limiter in the same settings transaction. Existing standalone Warp control remains compatible. Evidence: pending in executable scope above.
- **FR-SETTINGS-TXN-001/ac-08**: Serialize refresh and Apply, reject stale responses, preserve edited draft fields and refresh untouched fields. Same-field conflicts keep local draft until Apply or Revert. Evidence: pending in executable scope above.
- **FR-SETTINGS-TXN-001/ac-09**: Older hosts lacking State remain readable and clearly disable state-aware Apply/Restart with an upgrade explanation. Evidence: pending in executable scope above.
- **FR-SETTINGS-TXN-001/ac-10**: Snapshots and INI persistence include RAM, cartridge and uIEC fields. Defaults are no RAM expansion, no cartridge, preset start, writeback false, uIEC disabled with empty root and unit 9. Save-on-exit preferences remain outside host Apply/Revert. Evidence: pending in executable scope above.

### TR-SETTINGS-TXN-001

- **TR-SETTINGS-TXN-001/ac-01**: Add SettingsStateDto(Active, Accepted) and optional State on settings responses; preserve existing DTO fields and protobuf tags, using GetSettingsResponse.state=3 and UpdateSettingsResponse.state=4. Evidence: pending in executable scope above.
- **TR-SETTINGS-TXN-001/ac-02**: Prepare a fully configured replacement with media restored before publication; candidate cleanup produces no writeback or success notifications. Evidence: pending in executable scope above.
- **TR-SETTINGS-TXN-001/ac-03**: Commit under existing synchronization, defer publication until success, and roll back runtime/session/pump state on failure. Do not swallow attachment exceptions. Evidence: pending in executable scope above.
- **TR-SETTINGS-TXN-001/ac-04**: UpdateSettings honors Limiter.IsEnabled=false atomically and the view model makes no second Warp RPC for Apply. Evidence: pending in executable scope above.
- **TR-SETTINGS-TXN-001/ac-05**: A single synchronization path handles initial state, refresh, Apply response, Revert and persistence; perform field-wise three-way merge against Accepted. Evidence: pending in executable scope above.
- **TR-SETTINGS-TXN-001/ac-06**: Preserve existing field and standalone-control compatibility. Detect absent State explicitly rather than fabricating Active/Accepted truth. Evidence: pending in executable scope above.

### TEST-SETTINGS-TXN-001

- **TEST-SETTINGS-TXN-001/ac-01**: Add host regression cases for stage-restart Apply, retained restart after Revert, successful Apply+Restart and failed Apply/Restart with unchanged runtime/Accepted/Draft. Evidence: pending in executable scope above.
- **TEST-SETTINGS-TXN-001/ac-02**: Inject invalid family/profile, RAM, pacing/resource, image/preset and IEC root/unit/conflict inputs; assert no partial mutation or transient success notification. Evidence: pending in executable scope above.
- **TEST-SETTINGS-TXN-001/ac-03**: Inject replacement preparation and attachment failures; assert rollback and candidate cleanup without writeback. Evidence: pending in executable scope above.
- **TEST-SETTINGS-TXN-001/ac-04**: Verify Warp changes in the single UpdateSettings call, status rate 0, runtime uncapped behavior and preserved standalone Warp RPC compatibility. Evidence: pending in executable scope above.
- **TEST-SETTINGS-TXN-001/ac-05**: Race refresh against Apply, deliver out-of-order responses, edit one or multiple fields, and verify stale rejection and three-way field merge. Evidence: pending in executable scope above.
- **TEST-SETTINGS-TXN-001/ac-06**: Round-trip every RAM/cart/uIEC snapshot and INI field, legacy defaults and independent save-on-exit preferences. Evidence: pending in executable scope above.
- **TEST-SETTINGS-TXN-001/ac-07**: Test old responses with absent State: readable display, explicit upgrade message and disabled state-aware Apply/Restart. Evidence: pending in executable scope above.
- **TEST-SETTINGS-TXN-001/ac-08**: Verify actual cartridge type, detach, image/bank/preset/writeback and uIEC behavior after relevant family and profile restart. Evidence: pending in executable scope above.


## Slice 6: Portable RemoteControl gRPC command line

Planned executable scope:

- RemoteControlCommandFactoryTests
- RemoteControlGrpcContractTests (planned real in-process server cases)

Required evidence: Standalone restore/build; final discovered CLI cases; auth/default-off/permission/mutation/deadlines/tree/PNG/exit contracts.

### FR-REMOTECTRL-CLI-001

- **FR-REMOTECTRL-CLI-001/ac-01**: CLI restores and builds from a standalone ViceSharp checkout without a sibling repository. Evidence: pending in executable scope above.
- **FR-REMOTECTRL-CLI-001/ac-02**: Support grpc transport and its environment default; reject unsupported transports explicitly. Evidence: pending in executable scope above.
- **FR-REMOTECTRL-CLI-001/ac-03**: Provide machine-readable capabilities and tree results suitable for retained validation evidence. Evidence: pending in executable scope above.
- **FR-REMOTECTRL-CLI-001/ac-04**: Resolve exact automation IDs first; reject ambiguous matches, malformed trees and ancestor-hidden controls. Evidence: pending in executable scope above.
- **FR-REMOTECTRL-CLI-001/ac-05**: Authentication, disabled control, denied actions, failed mutations, cancellation, timeout and invalid frames produce accurate failures and exit codes. Evidence: pending in executable scope above.

### TR-REMOTECTRL-CLI-001

- **TR-REMOTECTRL-CLI-001/ac-01**: Use published SharpNinja Avalonia RemoteControl Protocol 0.7.4 matching the embedded Server 0.7.4, its generated gRPC client and the existing Grpc.Net.Client dependency. Evidence: pending in executable scope above.
- **TR-REMOTECTRL-CLI-001/ac-02**: Own and dispose the channel; propagate bearer metadata and cancellation with bounded calls. Evidence: pending in executable scope above.
- **TR-REMOTECTRL-CLI-001/ac-03**: Add CLI and test projects to ViceSharp.slnx and the exhaustive CompletionTest manifest. Evidence: pending in executable scope above.
- **TR-REMOTECTRL-CLI-001/ac-04**: Calculate effective visibility through ancestors and validate malformed references/cycles rather than assuming a matching node is actionable. Evidence: pending in executable scope above.
- **TR-REMOTECTRL-CLI-001/ac-05**: Validate frame data before writing PNG output and retain machine-readable response/error contracts. Evidence: pending in executable scope above.

### TEST-REMOTECTRL-CLI-001

- **TEST-REMOTECTRL-CLI-001/ac-01**: Restore/build/test an isolated checkout with no sibling paths and verify published package resolution. Evidence: pending in executable scope above.
- **TEST-REMOTECTRL-CLI-001/ac-02**: Use a real in-process gRPC server to verify bearer authentication, default-disabled and action permissions. Evidence: pending in executable scope above.
- **TEST-REMOTECTRL-CLI-001/ac-03**: Exercise exact-ID precedence, ambiguous matches, ancestor-hidden nodes and malformed trees. Evidence: pending in executable scope above.
- **TEST-REMOTECTRL-CLI-001/ac-04**: Exercise failed mutations, cancellation/deadlines, unsupported transport, malformed frame/PNG handling and CLI exit codes. Evidence: pending in executable scope above.
- **TEST-REMOTECTRL-CLI-001/ac-05**: Verify capability/tree JSON and successful enabled control actions against generated gRPC contracts. Evidence: pending in executable scope above.


## Slice 7: Auditable Astra xhigh AI review routing

Planned executable scope:

- aiUnit scoped strategy/resolver/Codex routing cases (exact cases assigned in Slice 7)
- ReviewLogTests, AiReviewRoutingTests (planned deterministic routing), both AiReviewTests theories

Required evidence: Supported published dependency build/test/release/package provenance; exact effort/argument precedence; both Astra/xhigh completed artifacts and durable logs.

### FR-AIREVIEW-ROUTING-001

- **FR-AIREVIEW-ROUTING-001/ac-01**: Both required ViceSharp AI review theories run GPT-6 Astra with xhigh reasoning effort and retain independently verifiable completed review artifacts. Evidence: pending in executable scope above.
- **FR-AIREVIEW-ROUTING-001/ac-02**: An xUnit NeverFails pass is not review success; malformed, error, incomplete, misrouted or missing artifacts fail the completion gate. Evidence: pending in executable scope above.
- **FR-AIREVIEW-ROUTING-001/ac-03**: Critical and high findings are resolved and affected reviews rerun before completion. Evidence: pending in executable scope above.

### TR-AIREVIEW-ROUTING-001

- **TR-AIREVIEW-ROUTING-001/ac-01**: Add ReasoningEffort through aiUnit strategy configuration, resolver, inline specifications, CLI invocation and result metadata. Evidence: pending in executable scope above.
- **TR-AIREVIEW-ROUTING-001/ac-02**: Support AIUNIT_REASONING_EFFORT as the process override and emit Codex -c model_reasoning_effort with the resolved value. Evidence: pending in executable scope above.
- **TR-AIREVIEW-ROUTING-001/ac-03**: Remove forced Grok attributes and prompt instructions. Use one selected agent per theory so no extra paid aggregator call is implied. Evidence: pending in executable scope above.
- **TR-AIREVIEW-ROUTING-001/ac-04**: Test deterministic routing without paid calls, then publish through aiUnit supported Nuke packaging and its Azure pipeline using pool Default and existing stable-version release rules. Evidence: pending in executable scope above.
- **TR-AIREVIEW-ROUTING-001/ac-05**: Consume the verified published aiUnit version in ViceSharp; retain package provenance and exact model/effort receipts for both paid theories. Evidence: pending in executable scope above.

### TEST-AIREVIEW-ROUTING-001

- **TEST-AIREVIEW-ROUTING-001/ac-01**: Add aiUnit deterministic tests for strategy/default/inline/process precedence and exact Codex model/effort argument routing. Evidence: pending in executable scope above.
- **TEST-AIREVIEW-ROUTING-001/ac-02**: Verify both ViceSharp theory definitions select Astra/xhigh and no Grok override remains in their execution configuration or prompts. Evidence: pending in executable scope above.
- **TEST-AIREVIEW-ROUTING-001/ac-03**: Validate artifact schema, non-error completed status, requested and actual model/effort metadata and durable logs independently of xUnit status. Evidence: pending in executable scope above.
- **TEST-AIREVIEW-ROUTING-001/ac-04**: Run both paid theories with Astra/xhigh after publication/consumption verification; retain artifact paths and resolve all critical/high findings with rerun evidence. Evidence: pending in executable scope above.


## Slice 0 and 8/final: Exhaustive approved-work completion evidence

Planned executable scope:

- tests/ViceSharp.Library.Tests/CompletionTestManifestTests.cs (planned validator regression matrix)
- All six test projects in Sol inventory with final test-case discovery
- Two fresh final-binary live application passes

Required evidence: Exhaustive discovered-case partitioning; all required command/test/native/remote/AI/live receipts; changed-AC audit; final hostile agreement; explicit-manifest commit and matching remote SHA.

### FR-COMPLETION-001

- **FR-COMPLETION-001/ac-01**: Complete every approved slice, including baseline repairs, Settings/Warp/video, portable CLI, native lifecycle, external integration and both Astra AI reviews. Evidence: pending in executable scope above.
- **FR-COMPLETION-001/ac-02**: All required tests finish with zero failures, skips, aborts or silent prerequisite returns before completion. Evidence: pending in executable scope above.
- **FR-COMPLETION-001/ac-03**: Run two fresh live application validation passes against final binaries and prove runtime effects independently of UI values. Evidence: pending in executable scope above.
- **FR-COMPLETION-001/ac-04**: Preserve unrelated dirty files and probes. Finalize tracked requirements, plan, handoff and receipts before explicit-manifest commit and non-force push; verify local and remote SHAs match. Evidence: pending in executable scope above.
- **FR-COMPLETION-001/ac-05**: Independent Astra xhigh agreement is required after requirements, red tests, green implementation and final combined evidence. Evidence: pending in executable scope above.

### TR-COMPLETION-MANIFEST-001

- **TR-COMPLETION-MANIFEST-001/ac-01**: Add an exhaustive manifest covering every solution and relevant out-of-solution test project, every category excluded by ordinary Nuke Test and every required native/remote/AI workload. Evidence: pending in executable scope above.
- **TR-COMPLETION-MANIFEST-001/ac-02**: Implement Nuke CompletionTest as the supported entry point; gates retain exact commands, configuration, filters, counts, TRX, stdout/stderr, diagnostic logs and relevant hang dumps. Evidence: pending in executable scope above.
- **TR-COMPLETION-MANIFEST-001/ac-03**: Final validation includes Release solution build, managed/native/determinism/parity/CLI/integration/AI scopes, changed-AC traceability and git diff --check. Evidence: pending in executable scope above.
- **TR-COMPLETION-MANIFEST-001/ac-04**: Fresh live passes first prove RemoteControl default-off failure, then enabled capabilities/actions and every applicable Settings control. Match debug-attach metadata to the owned PID and refresh session/tree IDs after restart. Evidence: pending in executable scope above.
- **TR-COMPLETION-MANIFEST-001/ac-05**: Live representative profiles are c64, c64c, ntsc and VIC-20 PAL/NTSC; automate all 14 C64 profiles, retain READY/geometry/clock/pacing/Warp evidence and verify exactly 28159 BASIC bytes free for pinned-ROM VIC-20 all-RAM. Evidence: pending in executable scope above.
- **TR-COMPLETION-MANIFEST-001/ac-06**: Prove actual cartridge image/bank/preset/writeback and uIEC I/O. Stop only validation processes started by this work. Evidence: pending in executable scope above.

### TEST-COMPLETION-001

- **TEST-COMPLETION-001/ac-01**: Completion manifest coverage verification detects an omitted project, category, required workload or prerequisite instead of accepting a narrower run. Evidence: pending in executable scope above.
- **TEST-COMPLETION-001/ac-02**: Final Release build and every required manifest gate completes with zero failed/skipped/aborted tests and retained evidence. Evidence: pending in executable scope above.
- **TEST-COMPLETION-001/ac-03**: Both fresh live passes cover Apply/Revert/Restart, validation, speed, individual RAM blocks, persistence preferences, builder open/close, effective visibility and independent host/runtime state. Evidence: pending in executable scope above.
- **TEST-COMPLETION-001/ac-04**: Audit every changed AC against named implementation and executable evidence; the informational traceability script exit code alone cannot prove completion. Evidence: pending in executable scope above.
- **TEST-COMPLETION-001/ac-05**: Require final Astra agreement over combined diff, requirement evidence, owned-file manifest and receipts before commit/push and local/origin SHA proof. Evidence: pending in executable scope above.
- **TEST-COMPLETION-001/ac-06**: Both fresh live passes and WarpModeTests satisfy existing TR-WARP-STATUS-001: LimiterRatePercent 0 and EffectiveClockPercent greater than 150, with actual runtime pacing evidence. Evidence: pending in executable scope above.


## Slice 3: Avalonia flash-cart builder abstraction boundary

Planned executable scope:

- AvaloniaBoundaryTests
- FlashCartBuilderAbstractionTests (planned controlled-I/O/notification cases)

Required evidence: C# and AXAML boundary red/green; profile/bank/import/build/save/error and notifications.

### TR-UI-COREBOUNDARY-001

- **TR-UI-COREBOUNDARY-001/ac-01**: Own flash-cart builder interfaces in Abstractions and supply a Host composition factory/private adapter; Avalonia C# and AXAML do not depend on Core flash-cart types. Evidence: pending in executable scope above.
- **TR-UI-COREBOUNDARY-001/ac-02**: Preserve existing Core/Xbox public APIs and observable collection/property-change behavior. Evidence: pending in executable scope above.

### TEST-UI-COREBOUNDARY-001

- **TEST-UI-COREBOUNDARY-001/ac-01**: Extend boundary scanner coverage to relevant real C# and AXAML sources and demonstrate the existing direct dependency as a red case. Evidence: pending in executable scope above.
- **TEST-UI-COREBOUNDARY-001/ac-02**: Exercise profile selection, bank updates, import, build, save and failure reporting through the abstraction with controlled file I/O and collection notifications. Evidence: pending in executable scope above.


## Slice 2: Approved baseline regression repairs

Planned executable scope:

- BasicBusAndSimpleRamTests, NativeCollectionConventionTests, SnapshotResumeCpuStateTests, BranchCycleCountTests
- Unchanged 5000-cycle snapshot and required performance/prefetch cases

Required evidence: Red branch traces against pinned VICE; green both-family branch matrix; snapshot alone/post-lifecycle; individual diagnostic/legacy dispositions.

### TR-BASELINE-CORRECTNESS-001

- **TR-BASELINE-CORRECTNESS-001/ac-01**: BasicBus unregister and unmapped-write tests preserve the intentional open-bus latch values 0x55 and 0xBB. Native-shim users follow the NativeVice collection convention. Evidence: pending in executable scope above.
- **TR-BASELINE-CORRECTNESS-001/ac-02**: Repair cycle-2078 snapshot branch-timing divergence using a minimal pinned-VICE-referenced instruction sequence for taken, untaken and page-crossing branches on C64 and VIC-20. Evidence: pending in executable scope above.
- **TR-BASELINE-CORRECTNESS-001/ac-03**: Keep the 5000-cycle snapshot fixture unchanged and require standalone and post-native-lifecycle success. Preserve passing performance/prefetch behavior. Evidence: pending in executable scope above.
- **TR-BASELINE-CORRECTNESS-001/ac-04**: Trace acceptance coverage before removing obsolete prefetch quarantine. Replace obsolete renderer assertions with equivalent cycle-aware tests before retirement. Move intentionally failing manual diagnostics to an explicit diagnostic tool, never hide actual product failures. Evidence: pending in executable scope above.

### TEST-BASELINE-CORRECTNESS-001

- **TEST-BASELINE-CORRECTNESS-001/ac-01**: Retain existing BasicBus tests with correct latched-bus expectations and a passing native-collection convention test. Evidence: pending in executable scope above.
- **TEST-BASELINE-CORRECTNESS-001/ac-02**: Add minimal branch-sequence red regressions for both machine families and compare cycle/register/PC results to pinned VICE. Evidence: pending in executable scope above.
- **TEST-BASELINE-CORRECTNESS-001/ac-03**: Run unchanged 5000-cycle snapshot fixture alone and after audio/dual-VIA/reset/snapshot/dispose; require passing historical performance and both investigated prefetch tests. Evidence: pending in executable scope above.
- **TEST-BASELINE-CORRECTNESS-001/ac-04**: Retain traceability from every retired renderer/quarantine assertion to equivalent cycle-aware acceptance tests. Account for manual diagnostic relocation explicitly in the completion manifest. Evidence: pending in executable scope above.


## Slice 7: Existing RomM and CSDb integration gate

Planned executable scope:

- ViceSharp.Library.IntegrationTests full project: all seven integration cases

Required evidence: Existing remote authenticated preflight and C64 fixture; 7 passed,0 failed/skipped; collections cleanup and both bridge auth callers.

### TR-REMOTE-INTEGRATION-001

- **TR-REMOTE-INTEGRATION-001/ac-01**: Use existing RomM http://192.168.0.148:8080/ and CSDb bridge http://192.168.0.148:8090/; do not substitute or start the stopped local stack. Evidence: pending in executable scope above.
- **TR-REMOTE-INTEGRATION-001/ac-02**: Provision the existing per-user bridge connection flow with a dedicated vicesharp-e2e identity; keep service administrative credentials on the service. Evidence: pending in executable scope above.
- **TR-REMOTE-INTEGRATION-001/ac-03**: Fixture-owned HttpClient supports optional VICESHARP_CSDB_BRIDGE_API_KEY via X-Api-Key for both connection and search requests. Evidence: pending in executable scope above.
- **TR-REMOTE-INTEGRATION-001/ac-04**: Authenticated preflight requires access and at least one downloadable launchable C64 D64 or CRT seed before running seven integration tests. Evidence: pending in executable scope above.

### TEST-REMOTE-INTEGRATION-001

- **TEST-REMOTE-INTEGRATION-001/ac-01**: Run all seven integration tests with zero failures and zero skips, including server-side collection create and cleanup. Evidence: pending in executable scope above.
- **TEST-REMOTE-INTEGRATION-001/ac-02**: Verify optional bridge authentication is sent consistently by the caller-owned client for connection and search. Evidence: pending in executable scope above.
- **TEST-REMOTE-INTEGRATION-001/ac-03**: Verify authenticated discovery/download/launch of the required C64 seed against the existing remote endpoints. Missing access, disabled provisioning or absent seed blocks completion. Evidence: pending in executable scope above.

## Existing Settings, Warp and RemoteControl requirements

### FR-MACHINEPICKER-001

- **FR-MACHINEPICKER-001/ac-two-combos**: Settings exposes Computer and Model ComboBoxes with AutomationIds Settings.Computer and Settings.MachineVariant. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.
- **FR-MACHINEPICKER-001/ac-models-filter**: Selecting Commodore 64 lists only x64sc models; selecting VIC-20 lists only xvic models. Minimal host is excluded. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.
- **FR-MACHINEPICKER-001/ac-restart**: Changing Computer or Model flags restart and Apply+Restart switches the live machine family/variant. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.

### FR-REMOTECTRL-001

- **FR-REMOTECTRL-001/ac-01**: RemoteControl is disabled when VICESHARP_REMOTECONTROL_ENABLE is unset; enabled startup without a bearer token fails closed. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.
- **FR-REMOTECTRL-001/ac-02**: Explicitly enabled, authenticated loopback gRPC exposes the current live MainWindow visual tree. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.
- **FR-REMOTECTRL-001/ac-03**: Interaction and live-frame access remain denied unless their separate opt-ins permit them; authentication and action failures are explicit. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.

### FR-UISETVIS-001

- **FR-UISETVIS-001/ac-c64-hides-vic20**: On C64 Computer, Settings.Vic20MemoryPreset, Vic20Blk0-5, ExpansionCartKind/Preset/WriteBack have IsEffectivelyVisible false. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.
- **FR-UISETVIS-001/ac-vic20-shows-vic20**: On VIC-20 Computer, those same controls have IsEffectivelyVisible true. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.

### FR-WARP-001

- **FR-WARP-001/ac-status-warp**: Warp via Settings or RemoteControl sets status LIMITER to WARP. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.
- **FR-WARP-001/ac-clock-above-100**: After warp is applied, status CLOCK percent is well above 100. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.

### TR-REMOTECTRL-SERVER-001

- **TR-REMOTECTRL-SERVER-001/ac-01**: Reference SharpNinja.Avalonia.RemoteControl.Server 0.7.4, with the CLI using matching published Protocol 0.7.4; restore/build is reproducible without sibling source paths through the supported package feeds. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.
- **TR-REMOTECTRL-SERVER-001/ac-02**: Use AddAvaloniaRemoteControl plus a DI IRemoteControlRootProvider for the live desktop MainWindow, and bind the host lifecycle to the classic-desktop lifetime. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.
- **TR-REMOTECTRL-SERVER-001/ac-03**: Preserve loopback/bearer/default-disabled behavior, deny-by-default action/frame gates and compatible central Avalonia/Grpc dependency constraints. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.

### TR-UIAXAML-PICKER-001

- **TR-UIAXAML-PICKER-001/ac-vm**: ViewModel tests cover Computers list, Models filter, family switch default profile, PAL/NTSC keeps Models instance. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.

### TR-WARP-STATUS-001

- **TR-WARP-STATUS-001/ac-dto-zero**: ToStatusDto emits LimiterRatePercent 0 when LimiterEnabled is false. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.
- **TR-WARP-STATUS-001/ac-clock-sample**: GetStatus after warp reports EffectiveClockPercent greater than 150 in WarpModeTests and live RC dumps. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.

### TEST-REMOTECTRL-001

- **TEST-REMOTECTRL-001/ac-01**: Test unset enable switch and enabled-without-token startup; both expose no usable control host. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.
- **TEST-REMOTECTRL-001/ac-02**: Launch the owned app with explicit valid enable/token settings; read capabilities and the live MainWindow tree over gRPC. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.
- **TEST-REMOTECTRL-001/ac-03**: Verify missing/wrong bearer token rejection, loopback policy and separate denied/enabled action/frame permissions. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.
- **TEST-REMOTECTRL-001/ac-04**: Retain the two fresh final-binary live passes and real gRPC CLI contract evidence required by TEST-REMOTECTRL-CLI-001 and TEST-COMPLETION-001. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.

### TEST-UISET-001

- **TEST-UISET-001/ac-all-spec**: All+Apply+Restart keeps BLK checks and Vic20MemorySpec all. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.
- **TEST-UISET-001/ac-bytes-free**: After pinned-ROM VIC-20 All RAM plus Apply+Restart, the READY BASIC boot reports exactly 28159 BYTES FREE; verify actual runtime memory as well as checked BLK controls. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.

### TEST-UISET-002

- **TEST-UISET-002/ac-inventory**: In each of two fresh final-binary live passes, drive every applicable VIC-20 Settings control and verify effective visibility, fresh host Active/Accepted state and actual runtime effects, including image/bank/preset/writeback and uIEC I/O. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.
- **TEST-UISET-002/ac-warp**: Warp via the single Settings transaction sets LIMITER WARP, LimiterRatePercent 0 and measured EffectiveClockPercent greater than 150 in WarpModeTests and live RemoteControl receipts. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.
- **TEST-UISET-002/ac-palntsc**: PAL vs NTSC Apply+Restart yields CLOCK ~1.108 vs ~1.023 MHz and ContentHeight 284 vs 234 with READY. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.
- **TEST-UISET-002/ac-revert**: Revert is exercised while dirty and restores Accepted values without cancelling an earlier accepted pending restart; save-on-exit preferences remain independent. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.
- **TEST-UISET-002/ac-state-and-runtime**: Apply, Apply+Restart, validation failure, speed cycling, every RAM BLK, persistence preferences and builder open/close follow TEST-SETTINGS-TXN-001 and TEST-COMPLETION-001, with host/runtime oracles after refreshing owned-process debug attach and session IDs. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.

### TEST-UISET-003

- **TEST-UISET-003/ac-c64-inventory**: Shared Settings inventory mutates and sticks for C64 models c64, c64c, and ntsc. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.
- **TEST-UISET-003/ac-c64-palntsc-warp**: C64 PAL versus NTSC boots READY with clocks approximately 0.985 versus 1.023 MHz; Warp status and live runtime satisfy TR-WARP-STATUS-001, including EffectiveClockPercent greater than 150. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.
- **TEST-UISET-003/ac-visibility**: VIC-20-only AutomationIds are not effectively visible on C64 Computer and are visible on VIC-20 Computer. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.
- **TEST-UISET-003/ac-catalog-14**: Automatically cover all 14 available C64 host catalog profiles and live c64/c64c/ntsc representatives. Cover case-insensitive IDs, unavailable/out-of-family/null/reentry rejection, stable Models within family and disabled Apply with a visible explanation for unusable catalog. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.
- **TEST-UISET-003/ac-draft-preservation**: Exercise shared synchronization for initial load, refresh, Apply, Revert and persisted state, preserving VIC-20 draft RAM/cart fields across family switches without attaching VIC-20 hardware to a C64 runtime. Evidence: pending Settings/Warp/RemoteControl tests and both fresh live passes.

## Phase gates and ownership

- Requirements/AC agreement precedes completion-validator tests and implementation. Compiling scaffolding is allowed where necessary; genuine red behavior tests precede implementation.
- Independent reviews cover workspace rules, requirements and the approved plan at each phase. Requirements-phase agreement is not completed Slice 0.
- Sol owns proposed completion runner/test files in its inventory. Root owns plan, handoff, requirements, matrix and MCP records. All other dirty files stay unowned until a later approved slice assigns them.
- Test evidence requires nonempty TRX and final discovered cases/counters; build/publish/audit evidence uses command exit/output receipts. Live and paid-review entries require independent typed artifact checks.
- No duplicate paid/remote partition selection within one completion invocation; affected reruns after a fix remain mandatory.
- Native prerequisites include x64 and xvic libraries, pinned ROMs, required workload flags and loadability. Native/performance/snapshot tests remain required; intentional diagnostic methods require individual disposition.
- Operational logging degradation was submitted as triage-report-af4731a38fcd4acaa3300f8ce37893d7. Queued/local log state is not proof of server persistence. Infrastructure repair is outside this plan.