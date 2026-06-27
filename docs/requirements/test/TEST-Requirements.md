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
- **Evidence:** `SidCombinedWaveformTests`, `SidPcmEquivalencyTests`, `validation-output/runtime/sid-goal-20260626-2351/vicesharp-pieces-release-space-final.mp4`

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
