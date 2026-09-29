# Approved Grok completion technical requirements

## TR-AIREVIEW-ROUTING-001: Auditable Astra xhigh AI review routing

**ID:** TR-AIREVIEW-ROUTING-001
**Priority:** high
**Status:** pending; no completion claim.

### Description

Add ReasoningEffort through aiUnit strategy configuration, resolver, inline specifications, CLI invocation and result metadata.
Support AIUNIT_REASONING_EFFORT as the process override and emit Codex -c model_reasoning_effort with the resolved value.
Remove forced Grok attributes and prompt instructions. Use one selected agent per theory so no extra paid aggregator call is implied.
Test deterministic routing without paid calls, then publish through aiUnit supported Nuke packaging and its Azure pipeline using pool Default and existing stable-version release rules.
Consume the verified published aiUnit version in ViceSharp; retain package provenance and exact model/effort receipts for both paid theories.

### Acceptance Criteria

- **ac-01**: Add ReasoningEffort through aiUnit strategy configuration, resolver, inline specifications, CLI invocation and result metadata.
- **ac-02**: Support AIUNIT_REASONING_EFFORT as the process override and emit Codex -c model_reasoning_effort with the resolved value.
- **ac-03**: Remove forced Grok attributes and prompt instructions. Use one selected agent per theory so no extra paid aggregator call is implied.
- **ac-04**: Test deterministic routing without paid calls, then publish through aiUnit supported Nuke packaging and its Azure pipeline using pool Default and existing stable-version release rules.
- **ac-05**: Consume the verified published aiUnit version in ViceSharp; retain package provenance and exact model/effort receipts for both paid theories.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---

## TR-BASELINE-CORRECTNESS-001: Approved baseline regression repairs

**ID:** TR-BASELINE-CORRECTNESS-001
**Priority:** high
**Status:** pending; no completion claim.

### Description

BasicBus unregister and unmapped-write tests preserve the intentional open-bus latch values 0x55 and 0xBB. Native-shim users follow the NativeVice collection convention.
Repair cycle-2078 snapshot branch-timing divergence using a minimal pinned-VICE-referenced instruction sequence for taken, untaken and page-crossing branches on C64 and VIC-20.
Keep the 5000-cycle snapshot fixture unchanged and require standalone and post-native-lifecycle success. Preserve passing performance/prefetch behavior.
Trace acceptance coverage before removing obsolete prefetch quarantine. Replace obsolete renderer assertions with equivalent cycle-aware tests before retirement. Move intentionally failing manual diagnostics to an explicit diagnostic tool, never hide actual product failures.

### Acceptance Criteria

- **ac-01**: BasicBus unregister and unmapped-write tests preserve the intentional open-bus latch values 0x55 and 0xBB. Native-shim users follow the NativeVice collection convention.
- **ac-02**: Repair cycle-2078 snapshot branch-timing divergence using a minimal pinned-VICE-referenced instruction sequence for taken, untaken and page-crossing branches on C64 and VIC-20.
- **ac-03**: Keep the 5000-cycle snapshot fixture unchanged and require standalone and post-native-lifecycle success. Preserve passing performance/prefetch behavior.
- **ac-04**: Trace acceptance coverage before removing obsolete prefetch quarantine. Replace obsolete renderer assertions with equivalent cycle-aware tests before retirement. Move intentionally failing manual diagnostics to an explicit diagnostic tool, never hide actual product failures.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---

## TR-COMPLETION-MANIFEST-001: Exhaustive approved-work completion evidence

**ID:** TR-COMPLETION-MANIFEST-001
**Priority:** high
**Status:** pending; no completion claim.

### Description

Add an exhaustive manifest covering every solution and relevant out-of-solution test project, every category excluded by ordinary Nuke Test and every required native/remote/AI workload.
Implement Nuke CompletionTest as the supported entry point; gates retain exact commands, configuration, filters, counts, TRX, stdout/stderr, diagnostic logs and relevant hang dumps.
Final validation includes Release solution build, managed/native/determinism/parity/CLI/integration/AI scopes, changed-AC traceability and git diff --check.
Fresh live passes first prove RemoteControl default-off failure, then enabled capabilities/actions and every applicable Settings control. Match debug-attach metadata to the owned PID and refresh session/tree IDs after restart.
Live representative profiles are c64, c64c, ntsc and VIC-20 PAL/NTSC; automate all 14 C64 profiles, retain READY/geometry/clock/pacing/Warp evidence and verify exactly 28159 BASIC bytes free for pinned-ROM VIC-20 all-RAM.
Prove actual cartridge image/bank/preset/writeback and uIEC I/O. Stop only validation processes started by this work.

### Acceptance Criteria

- **ac-01**: Add an exhaustive manifest covering every solution and relevant out-of-solution test project, every category excluded by ordinary Nuke Test and every required native/remote/AI workload.
- **ac-02**: Implement Nuke CompletionTest as the supported entry point; gates retain exact commands, configuration, filters, counts, TRX, stdout/stderr, diagnostic logs and relevant hang dumps.
- **ac-03**: Final validation includes Release solution build, managed/native/determinism/parity/CLI/integration/AI scopes, changed-AC traceability and git diff --check.
- **ac-04**: Fresh live passes first prove RemoteControl default-off failure, then enabled capabilities/actions and every applicable Settings control. Match debug-attach metadata to the owned PID and refresh session/tree IDs after restart.
- **ac-05**: Live representative profiles are c64, c64c, ntsc and VIC-20 PAL/NTSC; automate all 14 C64 profiles, retain READY/geometry/clock/pacing/Warp evidence and verify exactly 28159 BASIC bytes free for pinned-ROM VIC-20 all-RAM.
- **ac-06**: Prove actual cartridge image/bank/preset/writeback and uIEC I/O. Stop only validation processes started by this work.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---

## TR-NATIVE-LIFECYCLE-001: Bounded and isolated native oracle lifecycle

**ID:** TR-NATIVE-LIFECYCLE-001
**Priority:** high
**Status:** pending; no completion claim.

### Description

Oracle sound state is separate from live mixer state; no reset memset clears playback timing.
Use absolute monotonic deadlines for create/step 5000 ms and stop 2000 ms; remove infinite waits and propagate native failure to managed callers.
Poison belongs to the shared loaded native library, not only one wrapper instance; retain memory if its worker is still alive.
Capture all required modified native source in the supported patch against the pinned clean native gitlink; retain patch/build and loaded-DLL SHA256 receipts.
Audio comparisons require identical sample counts and complete PCM sequences; truncated Math.Min comparisons cannot establish parity.

### Acceptance Criteria

- **ac-01**: Oracle sound state is separate from live mixer state; no reset memset clears playback timing.
- **ac-02**: Use absolute monotonic deadlines for create/step 5000 ms and stop 2000 ms; remove infinite waits and propagate native failure to managed callers.
- **ac-03**: Poison belongs to the shared loaded native library, not only one wrapper instance; retain memory if its worker is still alive.
- **ac-04**: Capture all required modified native source in the supported patch against the pinned clean native gitlink; retain patch/build and loaded-DLL SHA256 receipts.
- **ac-05**: Audio comparisons require identical sample counts and complete PCM sequences; truncated Math.Min comparisons cannot establish parity.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---

## TR-REMOTECTRL-CLI-001: Portable RemoteControl gRPC command line

**ID:** TR-REMOTECTRL-CLI-001
**Priority:** high
**Status:** pending; no completion claim.

### Description

Use published SharpNinja Avalonia RemoteControl Protocol 0.7.4 matching the embedded Server 0.7.4, its generated gRPC client and the existing Grpc.Net.Client dependency.
Own and dispose the channel; propagate bearer metadata and cancellation with bounded calls.
Add CLI and test projects to ViceSharp.slnx and the exhaustive CompletionTest manifest.
Calculate effective visibility through ancestors and validate malformed references/cycles rather than assuming a matching node is actionable.
Validate frame data before writing PNG output and retain machine-readable response/error contracts.

### Acceptance Criteria

- **ac-01**: Use published SharpNinja Avalonia RemoteControl Protocol 0.7.4 matching the embedded Server 0.7.4, its generated gRPC client and the existing Grpc.Net.Client dependency.
- **ac-02**: Own and dispose the channel; propagate bearer metadata and cancellation with bounded calls.
- **ac-03**: Add CLI and test projects to ViceSharp.slnx and the exhaustive CompletionTest manifest.
- **ac-04**: Calculate effective visibility through ancestors and validate malformed references/cycles rather than assuming a matching node is actionable.
- **ac-05**: Validate frame data before writing PNG output and retain machine-readable response/error contracts.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---

## TR-REMOTECTRL-SERVER-001: Embeddable RemoteControl 0.7.4 integration

**ID:** TR-REMOTECTRL-SERVER-001
**Priority:** medium
**Status:** pending; no completion claim.

### Description

Reference SharpNinja.Avalonia.RemoteControl.Server 0.7.4, with the CLI using matching published Protocol 0.7.4; restore/build is reproducible without sibling source paths through the supported package feeds.
Use AddAvaloniaRemoteControl plus a DI IRemoteControlRootProvider for the live desktop MainWindow, and bind the host lifecycle to the classic-desktop lifetime.
Preserve loopback/bearer/default-disabled behavior, deny-by-default action/frame gates and compatible central Avalonia/Grpc dependency constraints.

### Acceptance Criteria

- **ac-01**: Reference SharpNinja.Avalonia.RemoteControl.Server 0.7.4, with the CLI using matching published Protocol 0.7.4; restore/build is reproducible without sibling source paths through the supported package feeds.
- **ac-02**: Use AddAvaloniaRemoteControl plus a DI IRemoteControlRootProvider for the live desktop MainWindow, and bind the host lifecycle to the classic-desktop lifetime.
- **ac-03**: Preserve loopback/bearer/default-disabled behavior, deny-by-default action/frame gates and compatible central Avalonia/Grpc dependency constraints.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---

## TR-REMOTE-INTEGRATION-001: Existing RomM and CSDb integration gate

**ID:** TR-REMOTE-INTEGRATION-001
**Priority:** high
**Status:** pending; no completion claim.

### Description

Use existing RomM http://192.168.0.148:8080/ and CSDb bridge http://192.168.0.148:8090/; do not substitute or start the stopped local stack.
Provision the existing per-user bridge connection flow with a dedicated vicesharp-e2e identity; keep service administrative credentials on the service.
Fixture-owned HttpClient supports optional VICESHARP_CSDB_BRIDGE_API_KEY via X-Api-Key for both connection and search requests.
Authenticated preflight requires access and at least one downloadable launchable C64 D64 or CRT seed before running seven integration tests.

### Acceptance Criteria

- **ac-01**: Use existing RomM http://192.168.0.148:8080/ and CSDb bridge http://192.168.0.148:8090/; do not substitute or start the stopped local stack.
- **ac-02**: Provision the existing per-user bridge connection flow with a dedicated vicesharp-e2e identity; keep service administrative credentials on the service.
- **ac-03**: Fixture-owned HttpClient supports optional VICESHARP_CSDB_BRIDGE_API_KEY via X-Api-Key for both connection and search requests.
- **ac-04**: Authenticated preflight requires access and at least one downloadable launchable C64 D64 or CRT seed before running seven integration tests.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---

## TR-SETTINGS-TXN-001: Truthful transactional Settings state

**ID:** TR-SETTINGS-TXN-001
**Priority:** high
**Status:** pending; no completion claim.

### Description

Add SettingsStateDto(Active, Accepted) and optional State on settings responses; preserve existing DTO fields and protobuf tags, using GetSettingsResponse.state=3 and UpdateSettingsResponse.state=4.
Prepare a fully configured replacement with media restored before publication; candidate cleanup produces no writeback or success notifications.
Commit under existing synchronization, defer publication until success, and roll back runtime/session/pump state on failure. Do not swallow attachment exceptions.
UpdateSettings honors Limiter.IsEnabled=false atomically and the view model makes no second Warp RPC for Apply.
A single synchronization path handles initial state, refresh, Apply response, Revert and persistence; perform field-wise three-way merge against Accepted.
Preserve existing field and standalone-control compatibility. Detect absent State explicitly rather than fabricating Active/Accepted truth.

### Acceptance Criteria

- **ac-01**: Add SettingsStateDto(Active, Accepted) and optional State on settings responses; preserve existing DTO fields and protobuf tags, using GetSettingsResponse.state=3 and UpdateSettingsResponse.state=4.
- **ac-02**: Prepare a fully configured replacement with media restored before publication; candidate cleanup produces no writeback or success notifications.
- **ac-03**: Commit under existing synchronization, defer publication until success, and roll back runtime/session/pump state on failure. Do not swallow attachment exceptions.
- **ac-04**: UpdateSettings honors Limiter.IsEnabled=false atomically and the view model makes no second Warp RPC for Apply.
- **ac-05**: A single synchronization path handles initial state, refresh, Apply response, Revert and persistence; perform field-wise three-way merge against Accepted.
- **ac-06**: Preserve existing field and standalone-control compatibility. Detect absent State explicitly rather than fabricating Active/Accepted truth.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---

## TR-UIAXAML-PICKER-001: Avalonia Computer/Model picker binds like Xbox

**ID:** TR-UIAXAML-PICKER-001
**Priority:** high
**Status:** pending; no completion claim.

### Description

AttachPanelViewModel mirrors XboxSettingsViewModel Computers/Models/SelectedComputer/SelectedModel using host SettingsProfileDto.Machine as family key (x64sc, xvic). SettingsView.axaml replaces the single Settings.MachineProfile ComboBox with Settings.Computer and Settings.MachineVariant. IsVic20Selected remains family xvic.

### Acceptance Criteria

- **ac-vm**: ViewModel tests cover Computers list, Models filter, family switch default profile, PAL/NTSC keeps Models instance.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---

## TR-UI-COREBOUNDARY-001: Avalonia flash-cart builder abstraction boundary

**ID:** TR-UI-COREBOUNDARY-001
**Priority:** high
**Status:** pending; no completion claim.

### Description

Own flash-cart builder interfaces in Abstractions and supply a Host composition factory/private adapter; Avalonia C# and AXAML do not depend on Core flash-cart types.
Preserve existing Core/Xbox public APIs and observable collection/property-change behavior.

### Acceptance Criteria

- **ac-01**: Own flash-cart builder interfaces in Abstractions and supply a Host composition factory/private adapter; Avalonia C# and AXAML do not depend on Core flash-cart types.
- **ac-02**: Preserve existing Core/Xbox public APIs and observable collection/property-change behavior.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---
