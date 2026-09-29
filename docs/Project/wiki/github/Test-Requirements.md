# TEST-Requirements: ViceSharp Test Requirements

## Document Information

| Field | Value |
|-------|-------|
| Project | ViceSharp |
| Version | 0.1.0-draft |
| Last Updated | 2026-06-12 |
| Status | Draft |

## Purpose

These test requirements define the verification conditions used to validate Functional Requirements ported from classic VICE documentation and Technical Requirements derived from the Vice-Sharp architecture.

---

## TEST-CPU-001: CPU Execution Reference Tests

**ID:** TEST-CPU-001
**Title:** CPU Execution Reference Tests
**Priority:** P1 -- Important

### Condition

CPU instruction, timing, interrupt, and port behavior is verified with unit tests and VICE/Lorenz-style reference comparisons.

### Traceability

- **Related FR Area(s):** FR-CPU

---

## TEST-MEM-001: Memory and Banking Tests

**ID:** TEST-MEM-001
**Title:** Memory and Banking Tests
**Priority:** P1 -- Important

### Condition

Address decoding, RAM-under-ROM, Ultimax, VIC bank selection, color RAM, and stack/zero-page behavior are verified with unit and integration tests.

### Traceability

- **Related FR Area(s):** FR-MEM

---

## TEST-VIC-001: VIC-II Video Reference Tests

**ID:** TEST-VIC-001
**Title:** VIC-II Video Reference Tests
**Priority:** P1 -- Important

### Condition

Raster timing, display modes, sprites, collisions, badlines, borders, FLI/AFLI, banking, and DMA timing are verified with deterministic frame or trace comparisons. The gate includes closed-border sprite masking, open-border sprite visibility, sprite priority over background/foreground pixels, per-model sprite DMA access timing, and VIC-II matrix/idle fetch behavior including prefetch `$ff` fill and ECM idle graphics addresses.

### Traceability

- **Related FR Area(s):** FR-VIC
- **Canonical FR IDs:** FR-VIC-001, FR-VIC-002, FR-VIC-003, FR-VIC-004, FR-VIC-005, FR-VIC-006, FR-VIC-007, FR-VIC-008, FR-VIC-009, FR-VIC-010

---

## TEST-SID-001: SID Audio Behavior Tests

**ID:** TEST-SID-001
**Title:** SID Audio Behavior Tests
**Priority:** P1 -- Important

### Condition

Oscillator, waveform, filter, ADSR, modulation, sync, noise, digi, external input, and multi-SID behavior are verified with deterministic audio/register tests.

### Traceability

- **Related FR Area(s):** FR-SID

---

## TEST-SID-002: VICE-Compatible SID Pacing Runtime Validation

**ID:** TEST-SID-002
**Title:** VICE-Compatible SID Pacing Runtime Validation
**Priority:** P0 -- Critical

### Condition

SID waveform centering, PCM equivalency, and Pieces of Light runtime capture validation prove that live audio back-pressure paces the emulator like VICE before and after runtime segment transitions.

### Traceability

- **Related FR Area(s):** FR-SID
- **Canonical FR IDs:** FR-SID-014
- **Canonical TR IDs:** TR-SID-EDGE-004
- **Evidence:** `SidPcmEquivalencyTests`, `SidDeterminismTests`, `SidFilter6581DivergentParityTests`, `SidFilter8580DivergentParityTests`, `validation-output/runtime/sid-goal-20260626-2351/vicesharp-pieces-release-space-final.mp4` (the legacy Chamberlin-SVF suites `SidFilter6581Tests`/`Sid6581NonLinearCutoffTests`/`Sid8580FilterTests` were retired with the Chamberlin stack, PLAN-SIDCHAMBERLIN-001; `SidCombinedWaveformTests` retired in commit 3cfcdf7)

---

## TEST-CIA-001: CIA and Keyboard Matrix Tests

**ID:** TEST-CIA-001
**Title:** CIA and Keyboard Matrix Tests
**Priority:** P1 -- Important

### Condition

CIA timers, TOD, keyboard matrix, joystick interaction, serial shift register, IRQ, and NMI behavior are verified with unit and integration tests.

### Traceability

- **Related FR Area(s):** FR-CIA

---

## TEST-VIA-001: VIA Integration Tests

**ID:** TEST-VIA-001
**Title:** VIA Integration Tests
**Priority:** P1 -- Important

### Condition

VIA timer, shift register, port handshake, VIC-20, and drive integration behavior are verified with unit and machine integration tests.

### Traceability

- **Related FR Area(s):** FR-VIA

---

## TEST-DRV-001: Drive and IEC Tests

**ID:** TEST-DRV-001
**Title:** Drive and IEC Tests
**Priority:** P1 -- Important

### Condition

Drive CPU/timing, image formats, GCR, IEC bus protocol, fast loader, and host media attachment behavior are verified with smoke, protocol, and reference tests.

### Traceability

- **Related FR Area(s):** FR-DRV

---

## TEST-TAP-001: Tape and Datasette Tests

**ID:** TEST-TAP-001
**Title:** Tape and Datasette Tests
**Priority:** P1 -- Important

### Condition

Datasette motor, TAP parsing, pulse timing, write behavior, and turbo loader compatibility are verified with unit and timing tests.

### Traceability

- **Related FR Area(s):** FR-TAP

---

## TEST-CRT-001: Cartridge Mapping Tests

**ID:** TEST-CRT-001
**Title:** Cartridge Mapping Tests
**Priority:** P1 -- Important

### Condition

Standard, Ocean, EasyFlash, Action Replay, Retro Replay, and Final Cartridge mapping behavior is verified with cartridge image and banking tests.

### Traceability

- **Related FR Area(s):** FR-CRT

---

## TEST-INPUT-001: Input and VKM Tests

**ID:** TEST-INPUT-001
**Title:** Input and VKM Tests
**Priority:** P1 -- Important

### Condition

Keyboard, joystick, mouse, lightpen, paddle, and VICE VKM behavior is verified with parser, matrix, protocol, and machine integration tests.

### Traceability

- **Related FR Area(s):** FR-INP

---

## TEST-MED-001: Media Capture Tests

**ID:** TEST-MED-001
**Title:** Media Capture Tests
**Priority:** P1 -- Important

### Condition

Screenshot, video, audio, synchronized capture, and format selection behavior are verified through capture metadata and round-trip output tests.

### Traceability

- **Related FR Area(s):** FR-MED

---

## TEST-MON-001: Monitor Tests

**ID:** TEST-MON-001
**Title:** Monitor Tests
**Priority:** P1 -- Important

### Condition

Disassembly, memory display, breakpoints, register operations, bank selection, watch expressions, and monitor RPC behavior are verified through monitor engine tests.

### Traceability

- **Related FR Area(s):** FR-MON

---

## TEST-SNP-001: Snapshot and Replay Tests

**ID:** TEST-SNP-001
**Title:** Snapshot and Replay Tests
**Priority:** P1 -- Important

### Condition

Save/load, deterministic replay, and state diff behavior are verified through round-trip and byte/trace comparison tests.

### Traceability

- **Related FR Area(s):** FR-SNP

---

## TEST-PRF-001: Machine Profile Tests

**ID:** TEST-PRF-001
**Title:** Machine Profile Tests
**Priority:** P1 -- Important

### Condition

C64, C64C, SX-64, C128, VIC-20, PET, Plus/4, and C16 profiles are verified for required devices, ROMs, clocks, and address maps.

### Traceability

- **Related FR Area(s):** FR-PRF

---

## TEST-ARCH-CHIPGLUE-001: Shared Chip Glue Boundary Tests

**ID:** TEST-ARCH-CHIPGLUE-001
**Title:** Shared Chip Glue Boundary Tests
**Priority:** P0 -- Critical

### Condition

Source-boundary, focused integration, and lockstep/checkpoint tests verify that reusable chip implementations stay free of machine-specific board and device glue. Machine-specific wiring for C64, C1541, IEC, datasette, cartridge, input, and media helper behavior is owned by Core machine/device definitions or host services.

### Acceptance Criteria

1. `ChipGlueBoundaryTests` prove moved and retired helper locations: duplicate/fake chip stubs are absent from `src/ViceSharp.Chips`, Core-owned device adapters exist, and shared chip files do not contain guarded C64/C1541 board policy.
2. Focused tests cover the moved or guarded behavior for C64 memory map, processor port, CIA, VIA, SID, VIC-II/video, IEC/drive, standard cartridge mapping, C64 input/VKM, datasette/TAP, and media capture.
3. The `ARCH-CHIPGLUE-001` audit document inventories every remaining `src/ViceSharp.Chips` type and maps each acceptance criterion to direct evidence.
4. The x64sc lockstep/checkpoint gate passes after the remediation with `0` failed and `0` skipped tests.

### Traceability

- **Related TR Area(s):** TR-SYSTEM-CORE-001
- **Related TEST Area(s):** TEST-X64SC-LOCKSTEP-001, TEST-CIA-001, TEST-VIA-001, TEST-DRV-001, TEST-TAP-001, TEST-CRT-001, TEST-INPUT-001, TEST-MED-001
- **Related TODO:** ARCH-CHIPGLUE-001

---

## TEST-PERF-RUNFRAME-001: C64 PAL RunFrame Performance Tests

**ID:** TEST-PERF-RUNFRAME-001
**Title:** C64 PAL RunFrame Performance Tests
**Priority:** P0 -- Critical

### Condition

The benchmark harness builds a real-ROM C64 PAL machine through `ArchitectureBuilder`, measures `IMachine.RunFrame()` after warmup over the required 600-frame window, reports median and p95 frame time, and proves the measured hot path allocates zero bytes on the current thread.

### Acceptance Criteria

1. `C64PalRunFrameBenchmark` builds Commodore 64 PAL through `ArchitectureBuilder` with the real ROM provider.
2. `RunFramePerfProbe` reports median `<= 18 ms`, p95 `<= 22 ms`, and `0` allocated bytes for the 60 warmup / 600 measured frame workflow.
3. Focused BasicBus/C64MemoryMap/VideoRenderer/VideoSurface/SID tests pass with `0` failed and `0` skipped.
4. Lockstep and checkpoint gates pass with `0` failed and `0` skipped.
5. BenchmarkDotNet completes `C64PalRunFrameBenchmark` and reports no managed allocation.

### Traceability

- **Related FR Area(s):** FR-PERF-RUNFRAME-001
- **Related TR Area(s):** TR-CORE-CYCLE-001, TR-CORE-DET-001, TR-PERF-ALLOC-001

---

## TEST-GRPC-001: gRPC Boundary Tests

**ID:** TEST-GRPC-001
**Title:** gRPC Boundary Tests
**Priority:** P1 -- Important

### Condition

Protocol, status, control, input, monitor, media, snapshot, capture, and boundary enforcement paths are verified through generated clients and host integration tests.

### Traceability

- **Related FR Area(s):** FR-HOST, FR-UI

---

## TEST-X64SC-LOCKSTEP-001: Native x64sc Variant Lockstep Tests

**ID:** TEST-X64SC-LOCKSTEP-001
**Title:** Native x64sc Variant Lockstep Tests
**Priority:** P0 -- Critical

### Condition

Every required x64sc model profile is validated against native x64sc for deterministic startup, BASIC prompt, keyboard input, disk attach/autostart, cartridge boot, and reset scenarios. Validation compares CPU registers, flags, cycle count, selected memory windows, CIA/VIC/SID observable register state, IRQ/NMI state, and frame/raster checkpoints. Missing ROMs, unavailable native x64sc binaries, unsupported variants, skipped variants, or stubbed checks fail the gate.

### Traceability

- **Related FR Area(s):** FR-CPU, FR-MEM, FR-VIC, FR-SID, FR-CIA, FR-DRV, FR-TAP, FR-CRT, FR-INP, FR-PRF, FR-CFG, FR-HOST

---

## TEST-HOST-001: Host Service Tests

**ID:** TEST-HOST-001
**Title:** Host Service Tests
**Priority:** P1 -- Important

### Condition

Host lifecycle, status, media, state, capture, diagnostics, and session ownership behavior are verified with in-process host service tests.

### Traceability

- **Related FR Area(s):** FR-HOST, FR-CFG

---

## TEST-HOST-DIAG-001: Diagnostics Host Attach Tests

**ID:** TEST-HOST-DIAG-001
**Title:** Diagnostics Host Attach Tests
**Priority:** P0 -- Critical

### Condition

Diagnostics protocol descriptors, session enumeration, host info, current UI session resolution, performance snapshots, streaming snapshots, attach file lifecycle, and development-scoped reflection are verified with red-first unit and in-process gRPC tests.

### Traceability

- **Related FR Area(s):** FR-HOST-DIAG-001
- **Related TR Area(s):** TR-HOST-DIAG-001, TR-HOST-DIAG-002, TR-HOST-DIAG-003, TR-HOST-DIAG-004

---

## TEST-UI-DIAG-001: UI Debug Attach Info Tests

**ID:** TEST-UI-DIAG-001
**Title:** UI Debug Attach Info Tests
**Priority:** P1 -- Important

### Condition

The Avalonia shell exposes a Copy Debug Attach Info command and formats endpoint, current session id, app version, attach JSON, and latest status for clipboard use.

### Traceability

- **Related FR Area(s):** FR-HOST-DIAG-001, FR-UI
- **Related TR Area(s):** TR-HOST-DIAG-002, TR-HOST-DIAG-003

---

## TEST-UI-001: Avalonia Shell ViewModel Tests

**ID:** TEST-UI-001
**Title:** Avalonia Shell ViewModel Tests
**Priority:** P1 -- Important

### Condition

Sidebar, status bar, attach panel, settings, keyboard map selection, monitor dock/pop-out, focus, and startup behavior are verified with fake host clients.

### Traceability

- **Related FR Area(s):** FR-UI

---

## TEST-CFG-001: Configuration and Resource Tests

**ID:** TEST-CFG-001
**Title:** Configuration and Resource Tests
**Priority:** P1 -- Important

### Condition

Resource files, ROM/romset selection, palettes, hotkeys, autostart, peripherals, RAM init, debug resources, and limiter settings are verified with configuration and host service tests.

### Traceability

- **Related FR Area(s):** FR-CFG

---

## TEST-CLI-LAUNCHER-001: CLI Launcher and Testbench Smoke Tests

**ID:** TEST-CLI-LAUNCHER-001
**Title:** CLI Launcher and Testbench Smoke Tests
**Priority:** P1 -- Important

### Condition

VICE-style launcher parser, topology, debugcart polarity, bounded `-limitcycles`, PRG autostart dispatch, help text, and process smoke behavior are verified with parser, stub entrypoint, and real console process tests.

### Traceability

- **Related FR Area(s):** FR-CFG, FR-HOST

---

## TEST-UISET-001: VIC-20 RAM settings acceptance

**ID:** TEST-UISET-001
**Priority:** medium
**Status:** pending; no completion claim.

### Description

VIC-20 RAM settings acceptance verifies all selected BLK regions in fresh host/runtime state after Apply+Restart and exactly 28159 BASIC bytes free with the pinned ROM.

### Acceptance Criteria

- **ac-all-spec**: All+Apply+Restart keeps BLK checks and Vic20MemorySpec all.
- **ac-bytes-free**: After pinned-ROM VIC-20 All RAM plus Apply+Restart, the READY BASIC boot reports exactly 28159 BYTES FREE; verify actual runtime memory as well as checked BLK controls.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---

## TEST-UISET-002: VIC-20 Settings RemoteControl matrix

**ID:** TEST-UISET-002
**Priority:** medium
**Status:** pending; no completion claim.

### Description

Two fresh final-binary RemoteControl passes exercise every applicable VIC-20 Settings control. Verify effective visibility, Active/Accepted/Draft semantics, single-transaction Warp above150 percent, PAL/NTSC runtime geometry/clocks, Revert, cartridge persistence and actual uIEC I/O.

### Acceptance Criteria

- **ac-inventory**: In each of two fresh final-binary live passes, drive every applicable VIC-20 Settings control and verify effective visibility, fresh host Active/Accepted state and actual runtime effects, including image/bank/preset/writeback and uIEC I/O.
- **ac-warp**: Warp via the single Settings transaction sets LIMITER WARP, LimiterRatePercent 0 and measured EffectiveClockPercent greater than 150 in WarpModeTests and live RemoteControl receipts.
- **ac-palntsc**: PAL vs NTSC Apply+Restart yields CLOCK ~1.108 vs ~1.023 MHz and ContentHeight 284 vs 234 with READY.
- **ac-revert**: Revert is exercised while dirty and restores Accepted values without cancelling an earlier accepted pending restart; save-on-exit preferences remain independent.
- **ac-state-and-runtime**: Apply, Apply+Restart, validation failure, speed cycling, every RAM BLK, persistence preferences and builder open/close follow TEST-SETTINGS-TXN-001 and TEST-COMPLETION-001, with host/runtime oracles after refreshing owned-process debug attach and session IDs.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---

## TEST-VIC20-FLASH-001: VIC-20 flash timing and persistence tests

**ID:** TEST-VIC20-FLASH-001
**Title:** VIC-20 flash timing and expansion write-back regression suite
**Priority:** P1 -- Important

### Condition

`Flash040EraseLatencyTests` verifies the VICE TYPE_B sector timeout, sector/chip cycle budgets, busy status, cancel, suspend, and resume behavior. `Vic20ExpansionCartTests`, `UltimemParityTests`, `MegaCartParityTests`, and `MediaServiceHostVic20ExpansionTests` verify mapping, dirty-state separation, and atomic detach persistence.

### Traceability

- **Related FR:** FR-VIC20-005
- **Related TR:** TR-VIC20-FLASH-001

---

## TEST-VIC20-SOUND-001: VIC-I sound determinism and native comparison

**ID:** TEST-VIC20-SOUND-001
**Title:** Deterministic VIC-I PCM and live backend regression suite
**Priority:** P1 -- Important

### Condition

`Vic20SoundLockstep` verifies deterministic managed silence/tone/multi-register output, no silent register-store allocation after warmup, and focused byte-identical silence/tone batches against native xvic. `Vic20AudioWiringTests` verifies machine-clock advancement and 256-sample delivery to the configured audio backend.

### Traceability

- **Related FR:** FR-VIC20-SOUND-001
- **Related TR:** TR-VIC20-SOUND-001

---

## TEST-ROMM-SEC-001: RomM credential, origin, and download safety

**ID:** TEST-ROMM-SEC-001
**Title:** RomM trusted-boundary regression suite
**Priority:** P0 -- Critical

### Condition

`RomMCoverImageSourceTests` verifies anonymous public covers and rejection of absolute authenticated-path inputs. `RomMGatewayDownloadTests` verifies filename containment and that truncated content never publishes a partial cache entry. `FileRomMConnectionStoreTests` verifies current-user DPAPI round-trip and immediate migration of legacy plaintext tokens.

### Traceability

- **Related FRs:** FR-ROMM-CONN-001, FR-ROMM-COVER-001, FR-ROMM-LAUNCH-001
- **Related TR:** TR-ROMM-SEC-001


## TEST-AIREVIEW-ROUTING-001: Auditable Astra xhigh AI review routing

**ID:** TEST-AIREVIEW-ROUTING-001
**Priority:** high
**Status:** pending; no completion claim.

### Description

Add aiUnit deterministic tests for strategy/default/inline/process precedence and exact Codex model/effort argument routing.
Verify both ViceSharp theory definitions select Astra/xhigh and no Grok override remains in their execution configuration or prompts.
Validate artifact schema, non-error completed status, requested and actual model/effort metadata and durable logs independently of xUnit status.
Run both paid theories with Astra/xhigh after publication/consumption verification; retain artifact paths and resolve all critical/high findings with rerun evidence.

### Acceptance Criteria

- **ac-01**: Add aiUnit deterministic tests for strategy/default/inline/process precedence and exact Codex model/effort argument routing.
- **ac-02**: Verify both ViceSharp theory definitions select Astra/xhigh and no Grok override remains in their execution configuration or prompts.
- **ac-03**: Validate artifact schema, non-error completed status, requested and actual model/effort metadata and durable logs independently of xUnit status.
- **ac-04**: Run both paid theories with Astra/xhigh after publication/consumption verification; retain artifact paths and resolve all critical/high findings with rerun evidence.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---

## TEST-BASELINE-CORRECTNESS-001: Approved baseline regression repairs

**ID:** TEST-BASELINE-CORRECTNESS-001
**Priority:** high
**Status:** pending; no completion claim.

### Description

Retain existing BasicBus tests with correct latched-bus expectations and a passing native-collection convention test.
Add minimal branch-sequence red regressions for both machine families and compare cycle/register/PC results to pinned VICE.
Run unchanged 5000-cycle snapshot fixture alone and after audio/dual-VIA/reset/snapshot/dispose; require passing historical performance and both investigated prefetch tests.
Retain traceability from every retired renderer/quarantine assertion to equivalent cycle-aware acceptance tests. Account for manual diagnostic relocation explicitly in the completion manifest.

### Acceptance Criteria

- **ac-01**: Retain existing BasicBus tests with correct latched-bus expectations and a passing native-collection convention test.
- **ac-02**: Add minimal branch-sequence red regressions for both machine families and compare cycle/register/PC results to pinned VICE.
- **ac-03**: Run unchanged 5000-cycle snapshot fixture alone and after audio/dual-VIA/reset/snapshot/dispose; require passing historical performance and both investigated prefetch tests.
- **ac-04**: Retain traceability from every retired renderer/quarantine assertion to equivalent cycle-aware acceptance tests. Account for manual diagnostic relocation explicitly in the completion manifest.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---

## TEST-COMPLETION-001: Exhaustive approved-work completion evidence

**ID:** TEST-COMPLETION-001
**Priority:** high
**Status:** pending; no completion claim.

### Description

Completion manifest coverage verification detects an omitted project, category, required workload or prerequisite instead of accepting a narrower run.
Final Release build and every required manifest gate completes with zero failed/skipped/aborted tests and retained evidence.
Both fresh live passes cover Apply/Revert/Restart, validation, speed, individual RAM blocks, persistence preferences, builder open/close, effective visibility and independent host/runtime state.
Audit every changed AC against named implementation and executable evidence; the informational traceability script exit code alone cannot prove completion.
Require final Astra agreement over combined diff, requirement evidence, owned-file manifest and receipts before commit/push and local/origin SHA proof.

### Acceptance Criteria

- **ac-01**: Completion manifest coverage verification detects an omitted project, category, required workload or prerequisite instead of accepting a narrower run.
- **ac-02**: Final Release build and every required manifest gate completes with zero failed/skipped/aborted tests and retained evidence.
- **ac-03**: Both fresh live passes cover Apply/Revert/Restart, validation, speed, individual RAM blocks, persistence preferences, builder open/close, effective visibility and independent host/runtime state.
- **ac-04**: Audit every changed AC against named implementation and executable evidence; the informational traceability script exit code alone cannot prove completion.
- **ac-05**: Require final Astra agreement over combined diff, requirement evidence, owned-file manifest and receipts before commit/push and local/origin SHA proof.
- **ac-06**: Both fresh live passes and WarpModeTests satisfy existing TR-WARP-STATUS-001: LimiterRatePercent 0 and EffectiveClockPercent greater than 150, with actual runtime pacing evidence.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---

## TEST-NATIVE-LIFECYCLE-001: Bounded and isolated native oracle lifecycle

**ID:** TEST-NATIVE-LIFECYCLE-001
**Priority:** high
**Status:** pending; no completion claim.

### Description

Add red regressions proving oracle reset does not zero live playback timing, then exact-length PCM equality for PAL and NTSC.
Run AudioThenDualViaResetSnapshotDispose repeatedly in one process for PAL and NTSC, including independent and post-sequence snapshot tests.
Run create/step timeout, stop timeout, poison-after-failure and repeat-dispose fault cases in owned child processes; assert bounded wall time and immediate later failure.
Require native prerequisites, selected workload flags and actual cycle budgets; missing DLLs/ROMs or disabled required workloads fail instead of returning early. Restore environment variables.
Build the supported patch from the pinned clean native source and verify the hash of the DLL loaded by the passing native tests.

### Acceptance Criteria

- **ac-01**: Add red regressions proving oracle reset does not zero live playback timing, then exact-length PCM equality for PAL and NTSC.
- **ac-02**: Run AudioThenDualViaResetSnapshotDispose repeatedly in one process for PAL and NTSC, including independent and post-sequence snapshot tests.
- **ac-03**: Run create/step timeout, stop timeout, poison-after-failure and repeat-dispose fault cases in owned child processes; assert bounded wall time and immediate later failure.
- **ac-04**: Require native prerequisites, selected workload flags and actual cycle budgets; missing DLLs/ROMs or disabled required workloads fail instead of returning early. Restore environment variables.
- **ac-05**: Build the supported patch from the pinned clean native source and verify the hash of the DLL loaded by the passing native tests.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---

## TEST-REMOTECTRL-001: RemoteControl server gating + root provider

**ID:** TEST-REMOTECTRL-001
**Priority:** medium
**Status:** pending; no completion claim.

### Description

Tests prove the RemoteControl integration is off by default (no host started when VICESHARP_REMOTECONTROL_ENABLE is unset) and fails closed when enabled without a token; and that, when enabled with a token, the configured IRemoteControlRootProvider returns the live MainWindow. App-launch gate: connect the RemoteControl client tool and confirm the visual tree is readable.

### Acceptance Criteria

- **ac-01**: Test unset enable switch and enabled-without-token startup; both expose no usable control host.
- **ac-02**: Launch the owned app with explicit valid enable/token settings; read capabilities and the live MainWindow tree over gRPC.
- **ac-03**: Verify missing/wrong bearer token rejection, loopback policy and separate denied/enabled action/frame permissions.
- **ac-04**: Retain the two fresh final-binary live passes and real gRPC CLI contract evidence required by TEST-REMOTECTRL-CLI-001 and TEST-COMPLETION-001.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---

## TEST-REMOTECTRL-CLI-001: Portable RemoteControl gRPC command line

**ID:** TEST-REMOTECTRL-CLI-001
**Priority:** high
**Status:** pending; no completion claim.

### Description

Restore/build/test an isolated checkout with no sibling paths and verify published package resolution.
Use a real in-process gRPC server to verify bearer authentication, default-disabled and action permissions.
Exercise exact-ID precedence, ambiguous matches, ancestor-hidden nodes and malformed trees.
Exercise failed mutations, cancellation/deadlines, unsupported transport, malformed frame/PNG handling and CLI exit codes.
Verify capability/tree JSON and successful enabled control actions against generated gRPC contracts.

### Acceptance Criteria

- **ac-01**: Restore/build/test an isolated checkout with no sibling paths and verify published package resolution.
- **ac-02**: Use a real in-process gRPC server to verify bearer authentication, default-disabled and action permissions.
- **ac-03**: Exercise exact-ID precedence, ambiguous matches, ancestor-hidden nodes and malformed trees.
- **ac-04**: Exercise failed mutations, cancellation/deadlines, unsupported transport, malformed frame/PNG handling and CLI exit codes.
- **ac-05**: Verify capability/tree JSON and successful enabled control actions against generated gRPC contracts.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---

## TEST-REMOTE-INTEGRATION-001: Existing RomM and CSDb integration gate

**ID:** TEST-REMOTE-INTEGRATION-001
**Priority:** high
**Status:** pending; no completion claim.

### Description

Run all seven integration tests with zero failures and zero skips, including server-side collection create and cleanup.
Verify optional bridge authentication is sent consistently by the caller-owned client for connection and search.
Verify authenticated discovery/download/launch of the required C64 seed against the existing remote endpoints. Missing access, disabled provisioning or absent seed blocks completion.

### Acceptance Criteria

- **ac-01**: Run all seven integration tests with zero failures and zero skips, including server-side collection create and cleanup.
- **ac-02**: Verify optional bridge authentication is sent consistently by the caller-owned client for connection and search.
- **ac-03**: Verify authenticated discovery/download/launch of the required C64 seed against the existing remote endpoints. Missing access, disabled provisioning or absent seed blocks completion.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---

## TEST-SETTINGS-TXN-001: Truthful transactional Settings state

**ID:** TEST-SETTINGS-TXN-001
**Priority:** high
**Status:** pending; no completion claim.

### Description

Add host regression cases for stage-restart Apply, retained restart after Revert, successful Apply+Restart and failed Apply/Restart with unchanged runtime/Accepted/Draft.
Inject invalid family/profile, RAM, pacing/resource, image/preset and IEC root/unit/conflict inputs; assert no partial mutation or transient success notification.
Inject replacement preparation and attachment failures; assert rollback and candidate cleanup without writeback.
Verify Warp changes in the single UpdateSettings call, status rate 0, runtime uncapped behavior and preserved standalone Warp RPC compatibility.
Race refresh against Apply, deliver out-of-order responses, edit one or multiple fields, and verify stale rejection and three-way field merge.
Round-trip every RAM/cart/uIEC snapshot and INI field, legacy defaults and independent save-on-exit preferences.
Test old responses with absent State: readable display, explicit upgrade message and disabled state-aware Apply/Restart.
Verify actual cartridge type, detach, image/bank/preset/writeback and uIEC behavior after relevant family and profile restart.

### Acceptance Criteria

- **ac-01**: Add host regression cases for stage-restart Apply, retained restart after Revert, successful Apply+Restart and failed Apply/Restart with unchanged runtime/Accepted/Draft.
- **ac-02**: Inject invalid family/profile, RAM, pacing/resource, image/preset and IEC root/unit/conflict inputs; assert no partial mutation or transient success notification.
- **ac-03**: Inject replacement preparation and attachment failures; assert rollback and candidate cleanup without writeback.
- **ac-04**: Verify Warp changes in the single UpdateSettings call, status rate 0, runtime uncapped behavior and preserved standalone Warp RPC compatibility.
- **ac-05**: Race refresh against Apply, deliver out-of-order responses, edit one or multiple fields, and verify stale rejection and three-way field merge.
- **ac-06**: Round-trip every RAM/cart/uIEC snapshot and INI field, legacy defaults and independent save-on-exit preferences.
- **ac-07**: Test old responses with absent State: readable display, explicit upgrade message and disabled state-aware Apply/Restart.
- **ac-08**: Verify actual cartridge type, detach, image/bank/preset/writeback and uIEC behavior after relevant family and profile restart.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---

## TEST-UI-COREBOUNDARY-001: Avalonia flash-cart builder abstraction boundary

**ID:** TEST-UI-COREBOUNDARY-001
**Priority:** high
**Status:** pending; no completion claim.

### Description

Extend boundary scanner coverage to relevant real C# and AXAML sources and demonstrate the existing direct dependency as a red case.
Exercise profile selection, bank updates, import, build, save and failure reporting through the abstraction with controlled file I/O and collection notifications.

### Acceptance Criteria

- **ac-01**: Extend boundary scanner coverage to relevant real C# and AXAML sources and demonstrate the existing direct dependency as a red case.
- **ac-02**: Exercise profile selection, bank updates, import, build, save and failure reporting through the abstraction with controlled file I/O and collection notifications.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---

## TEST-UISET-003: C64 Settings RC plus family visibility

**ID:** TEST-UISET-003
**Priority:** high
**Status:** pending; no completion claim.

### Description

Headless and live RemoteControl tests for C64 variants (at least c64, c64c, ntsc from host catalog) mutate shared Settings controls, Apply+Restart PAL vs NTSC CLOCK ~0.985 vs ~1.023 MHz with READY, Warp LIMITER WARP and CLOCK percent well above 100, and assert VIC-20-only AutomationIds are not effectively visible on C64 and are visible on VIC-20.

### Acceptance Criteria

- **ac-c64-inventory**: Shared Settings inventory mutates and sticks for C64 models c64, c64c, and ntsc.
- **ac-c64-palntsc-warp**: C64 PAL versus NTSC boots READY with clocks approximately 0.985 versus 1.023 MHz; Warp status and live runtime satisfy TR-WARP-STATUS-001, including EffectiveClockPercent greater than 150.
- **ac-visibility**: VIC-20-only AutomationIds are not effectively visible on C64 Computer and are visible on VIC-20 Computer.
- **ac-catalog-14**: Automatically cover all 14 available C64 host catalog profiles and live c64/c64c/ntsc representatives. Cover case-insensitive IDs, unavailable/out-of-family/null/reentry rejection, stable Models within family and disabled Apply with a visible explanation for unusable catalog.
- **ac-draft-preservation**: Exercise shared synchronization for initial load, refresh, Apply, Revert and persisted state, preserving VIC-20 draft RAM/cart fields across family switches without attaching VIC-20 hardware to a C64 runtime.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.

---

## TEST-UIDROP-002: PRG drag-drop load and BASIC RUN tests

**ID:** TEST-UIDROP-002
**Title:** PRG drag-drop load and BASIC RUN tests
**Priority:** P1 -- Important

### Condition

Verify PRG drop acceptance and host RAM load: `ShellViewModel` routes `*.prg` to `LoadProgramAsync` without attach/reset; `IsDropStartSupported` is true for `.prg` and existing media and false for unsupported types; `PrgMemoryLoader` writes at the load address, updates BASIC pointers only when load equals TXTTAB, and reports ran; `EmulatorHostService.LoadProgramAsync` loads payload into the session, sets Ran, and starts BASIC RUN automation only for BASIC-start PRGs; invalid PRGs return InvalidArgument; `GrpcEmulatorHostService` maps LoadProgram request/response fields.

### Traceability

- **Related FR Area(s):** FR-UIDROP-002, FR-CFG-005
- **Canonical FR IDs:** FR-UIDROP-002
- **Technical Requirements:** TR-HOST-PRG-001
