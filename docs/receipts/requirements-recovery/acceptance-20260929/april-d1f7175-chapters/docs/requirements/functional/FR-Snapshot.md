# FR-Snapshot: Snapshot and Replay Functional Requirements

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Subsystem      | Snapshot / State Management    |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-04-13                     |

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

### Traceability

- **Interfaces:** `ISnapshotManager`
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

### Traceability

- **Interfaces:** `ISnapshotManager`
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

### Traceability

- **Interfaces:** `ISnapshotManager`
- **Test Suite:** `SnapshotComparisonTests`, `DiffFormattingTests`, `IdenticalSnapshotTests`
