# Approved Grok completion functional requirements

## FR-AIREVIEW-ROUTING-001: Auditable Astra xhigh AI review routing

**ID:** FR-AIREVIEW-ROUTING-001
**Priority:** high
**Status:** pending; no completion claim.

### Description

Both required ViceSharp AI review theories run GPT-6 Astra with xhigh reasoning effort and retain independently verifiable completed review artifacts.
An xUnit NeverFails pass is not review success; malformed, error, incomplete, misrouted or missing artifacts fail the completion gate.
Critical and high findings are resolved and affected reviews rerun before completion.

### Acceptance Criteria

- **ac-01**: Both required ViceSharp AI review theories run GPT-6 Astra with xhigh reasoning effort and retain independently verifiable completed review artifacts.
- **ac-02**: An xUnit NeverFails pass is not review success; malformed, error, incomplete, misrouted or missing artifacts fail the completion gate.
- **ac-03**: Critical and high findings are resolved and affected reviews rerun before completion.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---

## FR-COMPLETION-001: Exhaustive approved-work completion evidence

**ID:** FR-COMPLETION-001
**Priority:** high
**Status:** pending; no completion claim.

### Description

Complete every approved slice, including baseline repairs, Settings/Warp/video, portable CLI, native lifecycle, external integration and both Astra AI reviews.
All required tests finish with zero failures, skips, aborts or silent prerequisite returns before completion.
Run two fresh live application validation passes against final binaries and prove runtime effects independently of UI values.
Preserve unrelated dirty files and probes. Finalize tracked requirements, plan, handoff and receipts before explicit-manifest commit and non-force push; verify local and remote SHAs match.
Independent Astra xhigh agreement is required after requirements, red tests, green implementation and final combined evidence.

### Acceptance Criteria

- **ac-01**: Complete every approved slice, including baseline repairs, Settings/Warp/video, portable CLI, native lifecycle, external integration and both Astra AI reviews.
- **ac-02**: All required tests finish with zero failures, skips, aborts or silent prerequisite returns before completion.
- **ac-03**: Run two fresh live application validation passes against final binaries and prove runtime effects independently of UI values.
- **ac-04**: Preserve unrelated dirty files and probes. Finalize tracked requirements, plan, handoff and receipts before explicit-manifest commit and non-force push; verify local and remote SHAs match.
- **ac-05**: Independent Astra xhigh agreement is required after requirements, red tests, green implementation and final combined evidence.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---

## FR-MACHINEPICKER-001: Settings Computer and Model dropdowns

**ID:** FR-MACHINEPICKER-001
**Priority:** high
**Status:** pending; no completion claim.

### Description

Avalonia Settings splits machine selection into two ComboBoxes matching Xbox SettingsPage. Computer is the family (Commodore 64 / VIC-20). Model is the variant list for that family from the host profile catalog (C64 PAL/NTSC/C64C/... or VIC-20 PAL/NTSC). Changing Computer publishes that family's Models and selects the family default profile. Changing Model sets the host profile id and flags Apply+Restart.

### Acceptance Criteria

- **ac-two-combos**: Settings exposes Computer and Model ComboBoxes with AutomationIds Settings.Computer and Settings.MachineVariant.
- **ac-models-filter**: Selecting Commodore 64 lists only x64sc models; selecting VIC-20 lists only xvic models. Minimal host is excluded.
- **ac-restart**: Changing Computer or Model flags restart and Apply+Restart switches the live machine family/variant.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---

## FR-NATIVE-LIFECYCLE-001: Bounded and isolated native oracle lifecycle

**ID:** FR-NATIVE-LIFECYCLE-001
**Priority:** high
**Status:** pending; no completion claim.

### Description

Native VIC-20 sound-oracle reset preserves the live mixer timing and clocks.
Create and step finish or fail within 5000 ms; stop finishes or fails within 2000 ms.
Dispose is idempotent; a live worker never observes freed state. Failed stop poisons shared native-library state and all later operations fail immediately.
Reset, destroy, snapshot read and snapshot write apply the same shutdown-failure policy.
Normal audio, dual-VIA, reset, snapshot and dispose work repeatedly in one process for PAL and NTSC.

### Acceptance Criteria

- **ac-01**: Native VIC-20 sound-oracle reset preserves the live mixer timing and clocks.
- **ac-02**: Create and step finish or fail within 5000 ms; stop finishes or fails within 2000 ms.
- **ac-03**: Dispose is idempotent; a live worker never observes freed state. Failed stop poisons shared native-library state and all later operations fail immediately.
- **ac-04**: Reset, destroy, snapshot read and snapshot write apply the same shutdown-failure policy.
- **ac-05**: Normal audio, dual-VIA, reset, snapshot and dispose work repeatedly in one process for PAL and NTSC.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---

## FR-REMOTECTRL-001: Live Avalonia visual-tree inspection over gRPC

**ID:** FR-REMOTECTRL-001
**Priority:** medium
**Status:** pending; no completion claim.

### Description

ViceSharp.Avalonia can expose its live Avalonia visual tree for remote inspection and (optionally) interaction over gRPC via the SharpNinja.Avalonia.RemoteControl embeddable server, to support UI development/validation. The server is disabled by default and only starts when explicitly enabled via environment switches, and then only with a bearer token on a loopback transport (interaction and live frames remain deny-by-default opt-ins).

### Acceptance Criteria

- **ac-01**: RemoteControl is disabled when VICESHARP_REMOTECONTROL_ENABLE is unset; enabled startup without a bearer token fails closed.
- **ac-02**: Explicitly enabled, authenticated loopback gRPC exposes the current live MainWindow visual tree.
- **ac-03**: Interaction and live-frame access remain denied unless their separate opt-ins permit them; authentication and action failures are explicit.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---

## FR-REMOTECTRL-CLI-001: Portable RemoteControl gRPC command line

**ID:** FR-REMOTECTRL-CLI-001
**Priority:** high
**Status:** pending; no completion claim.

### Description

CLI restores and builds from a standalone ViceSharp checkout without a sibling repository.
Support grpc transport and its environment default; reject unsupported transports explicitly.
Provide machine-readable capabilities and tree results suitable for retained validation evidence.
Resolve exact automation IDs first; reject ambiguous matches, malformed trees and ancestor-hidden controls.
Authentication, disabled control, denied actions, failed mutations, cancellation, timeout and invalid frames produce accurate failures and exit codes.

### Acceptance Criteria

- **ac-01**: CLI restores and builds from a standalone ViceSharp checkout without a sibling repository.
- **ac-02**: Support grpc transport and its environment default; reject unsupported transports explicitly.
- **ac-03**: Provide machine-readable capabilities and tree results suitable for retained validation evidence.
- **ac-04**: Resolve exact automation IDs first; reject ambiguous matches, malformed trees and ancestor-hidden controls.
- **ac-05**: Authentication, disabled control, denied actions, failed mutations, cancellation, timeout and invalid frames produce accurate failures and exit codes.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---

## FR-SETTINGS-TXN-001: Truthful transactional Settings state

**ID:** FR-SETTINGS-TXN-001
**Priority:** high
**Status:** pending; no completion claim.

### Description

Expose Active runtime settings, Accepted last accepted target including staged restart fields, and local unsaved Draft independently.
Dirty compares normalized Draft to Accepted. Restart-required compares restart-relevant Draft to Active.
Plain Apply applies live fields, accepts the complete target, clears dirty and retains a pending restart. Revert restores Accepted and does not cancel an earlier accepted restart.
Apply + Restart publishes a prepared replacement and then makes Active and Accepted equal. Any failure preserves prior runtime, accepted state and unsaved draft.
Validate profile/family, RAM, limiter, resources, cartridge image/preset and IEC root/unit/reserved-device conflicts before any externally visible mutation.
Derive active cartridge kind from the actual device. A non-none kind needs a compatible image; none detaches. Preserve image, banks, preset, writeback and uIEC across applicable restart.
Apply Warp through the existing limiter in the same settings transaction. Existing standalone Warp control remains compatible.
Serialize refresh and Apply, reject stale responses, preserve edited draft fields and refresh untouched fields. Same-field conflicts keep local draft until Apply or Revert.
Older hosts lacking State remain readable and clearly disable state-aware Apply/Restart with an upgrade explanation.
Snapshots and INI persistence include RAM, cartridge and uIEC fields. Defaults are no RAM expansion, no cartridge, preset start, writeback false, uIEC disabled with empty root and unit 9. Save-on-exit preferences remain outside host Apply/Revert.

### Acceptance Criteria

- **ac-01**: Expose Active runtime settings, Accepted last accepted target including staged restart fields, and local unsaved Draft independently.
- **ac-02**: Dirty compares normalized Draft to Accepted. Restart-required compares restart-relevant Draft to Active.
- **ac-03**: Plain Apply applies live fields, accepts the complete target, clears dirty and retains a pending restart. Revert restores Accepted and does not cancel an earlier accepted restart.
- **ac-04**: Apply + Restart publishes a prepared replacement and then makes Active and Accepted equal. Any failure preserves prior runtime, accepted state and unsaved draft.
- **ac-05**: Validate profile/family, RAM, limiter, resources, cartridge image/preset and IEC root/unit/reserved-device conflicts before any externally visible mutation.
- **ac-06**: Derive active cartridge kind from the actual device. A non-none kind needs a compatible image; none detaches. Preserve image, banks, preset, writeback and uIEC across applicable restart.
- **ac-07**: Apply Warp through the existing limiter in the same settings transaction. Existing standalone Warp control remains compatible.
- **ac-08**: Serialize refresh and Apply, reject stale responses, preserve edited draft fields and refresh untouched fields. Same-field conflicts keep local draft until Apply or Revert.
- **ac-09**: Older hosts lacking State remain readable and clearly disable state-aware Apply/Restart with an upgrade explanation.
- **ac-10**: Snapshots and INI persistence include RAM, cartridge and uIEC fields. Defaults are no RAM expansion, no cartridge, preset start, writeback false, uIEC disabled with empty root and unit 9. Save-on-exit preferences remain outside host Apply/Revert.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---

## FR-UISETVIS-001: Family-specific Settings visibility

**ID:** FR-UISETVIS-001
**Priority:** high
**Status:** pending; no completion claim.

### Description

VIC-20-only Settings (memory preset, BLK0/1/2/3/5, expansion kind/preset/write-back, flash builder) are not effectively visible when a C64 family model is selected. When a VIC-20 family model is selected, those controls are visible and any C64-only Settings (none in the current Avalonia surface besides family-filtered Models) stay hidden. Shared limiter/display/audio/input/uIEC/persistence remain visible for both families.

### Acceptance Criteria

- **ac-c64-hides-vic20**: On C64 Computer, Settings.Vic20MemoryPreset, Vic20Blk0-5, ExpansionCartKind/Preset/WriteBack have IsEffectivelyVisible false.
- **ac-vic20-shows-vic20**: On VIC-20 Computer, those same controls have IsEffectivelyVisible true.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---
