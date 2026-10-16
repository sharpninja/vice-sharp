# FR-Snapshot: Snapshot and Replay Functional Requirements

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Subsystem      | Snapshot / State Management    |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-10-03 |

---

## FR-SNP-001: Save Machine State

**ID:** FR-SNP-001
**Title:** Save Complete Machine State to Snapshot
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The emulator shall capture the complete machine state at any point during execution and serialize it to a snapshot file. The snapshot includes CPU registers, all RAM, all I/O chip registers and internal state, VIC-II raster state, SID oscillator/envelope state, CIA timer state, and peripheral state.

### Acceptance Criteria

1. CPU state: all registers (A, X, Y, SP, PC, P), interrupt pending flags, and pipeline phase.
2. Memory: complete 64KB RAM contents, Color RAM (1KB nybbles), and any expansion RAM.
3. VIC-II: all 47 registers, raster position, sprite state (DMA, counters, data buffers), display state, badline state.
4. SID: all registers, oscillator phase accumulators, envelope state (phase, counter, rate), filter state, noise LFSR state.
5. CIA1 and CIA2: all registers, timer counters and latches, TOD clock state, shift register state, interrupt flags.
6. 6510 I/O port: DDR value, port value, capacitive charge state.
7. Peripherals: disk drive state (if true drive emulation), tape position, cartridge banking state.
8. Snapshot files use a versioned binary format with integrity checksums.
9. The `ISnapshotManager.Save()` method returns a snapshot handle or writes to a specified path.

### Source References

- `native/vice/vice/doc/vice.texi`: snapshot, history, recording, and state persistence behavior exposed by emulator commands.

### Traceability

- **Interfaces:** `ISnapshotManager`
- **Boundary:** FR-HOST-005 exposes snapshot save commands and artifact metadata through the host service.
- **Test Suite:** `SnapshotSaveTests`, `SnapshotCompletenessTests`, `SnapshotFormatTests`

---

## FR-SNP-002: Load Machine State

**ID:** FR-SNP-002
**Title:** Load Machine State from Snapshot
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The emulator shall restore a complete machine state from a previously saved snapshot file. After loading, execution continues from exactly the point at which the snapshot was taken, with all devices in their saved state.

### Acceptance Criteria

1. Loading a snapshot restores the exact CPU state (registers, flags, pipeline phase).
2. All 64KB of RAM plus Color RAM are restored byte-for-byte.
3. VIC-II state is restored including mid-raster position -- the first frame after load completes the line from the saved raster position.
4. SID state is restored including oscillator phase -- audio output continues seamlessly.
5. CIA state is restored including running timer values -- interrupt timing is preserved.
6. Peripheral state (drives, tape, cartridges) is restored.
7. Snapshot format version mismatches are detected and reported.
8. Invalid or corrupt snapshots are rejected with a descriptive error.
9. Loading a snapshot does not leak memory (previous state is fully released).
10. The `ISnapshotManager.Load()` method accepts a snapshot handle or file path.

### Source References

- `native/vice/vice/doc/vice.texi`: snapshot, history, recording, and state persistence behavior exposed by emulator commands.

### Traceability

- **Interfaces:** `ISnapshotManager`
- **Boundary:** FR-HOST-005 exposes snapshot load commands and validation errors through the host service.
- **Test Suite:** `SnapshotLoadTests`, `SnapshotRoundtripTests`, `SnapshotVersionTests`, `CorruptSnapshotTests`

---

## FR-SNP-003: Deterministic Replay

**ID:** FR-SNP-003
**Title:** Deterministic Input Replay
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The emulator shall support recording input events (keyboard, joystick, timing) from a starting snapshot and replaying them to reproduce the exact same execution. This enables tool-assisted speedruns (TAS), automated testing, and bug reproduction.

### Acceptance Criteria

1. Recording mode captures all external input events with their precise frame and cycle timestamps.
2. Playback mode loads a base snapshot and replays the recorded input events.
3. Playback produces bit-exact results: the same memory contents, same VIC-II output, same audio output as the original recording at every point.
4. The replay format stores: base snapshot reference, input event stream (event type, frame number, cycle within frame, value).
5. Replay can be paused, single-stepped, and fast-forwarded.
6. The `IReplayEngine` interface provides record/playback/seek operations.
7. Replay files are compact (only input events are stored, not full state per frame).
8. Seeking to an arbitrary frame is supported via periodic auto-snapshots during recording.

### Source References

- `native/vice/vice/doc/vice.texi`: snapshot, history, recording, and state persistence behavior exposed by emulator commands.

### Traceability

- **Interfaces:** `ISnapshotManager`, `IReplayEngine`
- **Test Suite:** `ReplayDeterminismTests`, `ReplaySeekTests`, `ReplayInputRecordTests`

---

## FR-SNP-004: Snapshot Comparison / Diff

**ID:** FR-SNP-004
**Title:** Snapshot Comparison and State Diffing
**Priority:** P2 -- Enhancement
**Iteration:** 3

### Description

The emulator shall support comparing two snapshots and reporting the differences in machine state. This is useful for debugging, understanding program behavior, and verifying determinism.

### Acceptance Criteria

1. Two snapshots can be compared via `ISnapshotManager.Compare(snapshot1, snapshot2)`.
2. The diff result reports all changed CPU registers and flags.
3. The diff result reports all changed memory addresses with old and new values.
4. The diff result reports all changed I/O chip register values (VIC-II, SID, CIA).
5. A summary mode reports the count of changes per subsystem (e.g., "RAM: 42 bytes changed, VIC-II: 3 registers changed").
6. A detailed mode reports every individual change.
7. Unchanged state is not included in the diff output (only deltas).
8. The diff can be serialized to a human-readable format for inspection.

### Source References

- `native/vice/vice/doc/vice.texi`: snapshot, history, recording, and state persistence behavior exposed by emulator commands.

### Traceability

- **Interfaces:** `ISnapshotManager`
- **Test Suite:** `SnapshotComparisonTests`, `DiffFormattingTests`, `IdenticalSnapshotTests`

---

## FR-SNP-005: Native VICE Snapshot Interoperability

**ID:** FR-SNP-005
**Title:** Load and Save Native VICE Snapshot Files
**Priority:** P0 -- Critical
**Iteration:** 2
**Status:** Pending

### Description

For supported x64sc C64 and xvic VIC-20 systems, native VICE `.vsf` shall be the canonical ViceSharp snapshot format. All normal supported public and Avalonia desktop capture, save, restore, file, quick-slot, and named-slot paths shall read or write native VICE snapshots through the existing managed state-transfer bridge. The proprietary ViceSharp runtime and replay payloads are not peer snapshot formats and may remain only behind an explicit legacy migration boundary.

### Acceptance Criteria

1. ViceSharp loads a native VICE `.vsf` produced by x64sc or xvic without translating it through a proprietary ViceSharp snapshot format.
2. The restored CPU, RAM, video, CIA/VIA, SID/VIC audio, timing, cartridge, tape, and drive state represented by the supported modules matches the corresponding VICE checkpoint.
3. Starting from the same native `.vsf`, ViceSharp and the matching VICE executable remain in lockstep for at least 100,000 cycles with zero CPU, bus, video, timer, interrupt, or mapped-memory divergence.
4. ViceSharp writes a native VICE `.vsf` that x64sc or xvic loads and resumes with matching represented state.
5. Bidirectional VICE-to-ViceSharp-to-VICE and ViceSharp-to-VICE-to-ViceSharp round trips preserve all supported modules and deterministic continuation.
6. A wrong machine identity, unsupported container or mandatory-module version, manifest-invalid mandatory module set, truncated header or module framing, invalid or oversized length, trailing bytes, or byte corruption that violates envelope invariants or makes native VICE reject the snapshot is rejected before visible machine-state mutation and returns a descriptive error. VSF has no general payload checksum, so arbitrary payload bit flips that pass both structural validation and native VICE are not falsely claimed detectable.
7. The native VICE shim and the required x64sc and xvic fixtures are mandatory test prerequisites. Their absence is a failing gate and is never reported as a skipped pass.
8. Every normal supported public and Avalonia `win-x64` desktop snapshot entry point, including `CaptureSnapshot`, `.vsf` file save/load, quick save/load, and ten named slots, uses native VICE `.vsf` as the canonical default without requiring an opt-in format selector. A host without the packaged native runtime returns an explicit unavailable result and never falls back to a proprietary format; UWP/Xbox implementation remains out of this requirement's active implementation scope without retiring its historical records.
9. Existing `vice-sharp.machine-snapshot.v2` and `vice-sharp.runtime-snapshot.v1` payloads are accepted only by an explicit `ImportLegacySnapshot` operation and RPC that restores them once. No normal snapshot capture or save emits them, and replay payloads remain outside the snapshot API.

### Source References

- `native/vice/vice/src/snapshot.c` and `snapshot.h`: VICE snapshot container, module framing, typed module reads and writes, machine identity, and version handling.
- `native/vice/vice/src/c64/c64-snapshot.c`: x64sc C64 snapshot composition and restore order.
- `native/vice/vice/src/vic20/vic20-snapshot.c`: xvic VIC-20 snapshot composition and restore order.
- Native VICE source is pinned by the `native/vice` submodule at commit `cc67418cba58e71d8e70fd353e9de47199ad2075` for this compatibility contract.

### Traceability

- **Technical Requirement:** TR-LOCKSTEP-VSF-001
- **Test Requirement:** TEST-SNP-002
- **Recovered Legacy Coverage:** FR-VSFLOCKSTEP-001, TR-VSFLOCKSTEP-RESUME-001, TEST-VSFLOCKSTEP-001
- **Public Boundary:** `ISnapshotService`, `SnapshotDto`, desktop file commands, quick slots, named slots, and the canonical `vice.native.vsf` format identifier.
- **Plan:** `docs/plans/PLAN-VSFLOCKSTEP-001.md`
- **Gate 0 local evidence:** `docs/receipts/hv/20261003T100623-0500-vsf-gate0-local-evidence.md`

---

## FR-VSFLOCKSTEP-001: Resume Externally-Staged VICE Snapshots in the Native Oracle

**ID:** FR-VSFLOCKSTEP-001
**Title:** Resume externally-staged VICE `.vsf` snapshots in the native oracle
**Priority:** P1 -- High
**Status:** Pending
**Scope Layer:** `layer-1`

### Description

The native x64sc oracle shall read and resume the supplied `.vsf` snapshot staged by a standalone x64sc, including an older VICE snapshot whose module versions differ from the pinned submodule, so lockstep can start from the user-supplied PAL/C64C, reSID, true-drive state.

### Acceptance Criteria

1. The committed fixture `tests/ViceSharp.TestHarness/Fixtures/Vsf/ready-c64sc-truedrive.vsf`, SHA-256 `5C284F5CF0A1BC7E01A2E4117A74158B7A9D60485F5626448F4B7655C0E23C19`, walks exactly to EOF as 24 framed modules (`MAINCPU`, `C64MEM`, `C64CART`, `CIA1`, `CIA2`, `SID`, `SIDEXTENDED`, `VDRIVEIMAGE8`, `DRIVE8`, `DRIVE9`, `DRIVE10`, `DRIVE11`, `FSDRIVE`, `VIC-II`, `GLUE`, `C64MEMHACKS`, `TAPEPORT`, `DATASETTE`, `KEYBOARD`, `JOYPORT0`, `JOYSTICK0`, `JOYPORT1`, `JOYSTICK1`, `USERPORT`) and loads through the pinned x64sc shim with `rc=0` and `snapshot_last_error=0` with no skipped prerequisite.
2. The resumed `MAINCPU` registers A, X, Y, SP, and PC equal those encoded in the snapshot `MAINCPU` module.
3. The current non-skipped reSID, SID parity, shim round-trip, and x64sc lockstep gates complete with zero failures after the external snapshot resume. Historical pass or skip counts are not completion evidence.

### Traceability

- **Superseded By:** None. This recovered requirement remains active.
- **Expanded By:** FR-SNP-005
- **Technical Requirements:** TR-VSFLOCKSTEP-RESUME-001, TR-LOCKSTEP-VSF-001
- **Test Requirements:** TEST-VSFLOCKSTEP-001, TEST-SNP-002
