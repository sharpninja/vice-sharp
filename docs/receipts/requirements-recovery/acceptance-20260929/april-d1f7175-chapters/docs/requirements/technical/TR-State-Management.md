# TR-State-Management: State Management Technical Requirement

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Quality Area   | Reliability / Consistency      |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-04-13                     |

---

## TR-STATE-001: Mutation Queue, ACID State Transactions, Configurable State Window

**ID:** TR-STATE-001
**Title:** Mutation Queue with ACID State Transactions and Configurable State Window
**Priority:** P0 -- Critical
**Category:** Reliability / Architecture

### Description

All mutations to emulated machine state shall be performed through a mutation queue that provides ACID-like transaction semantics. State changes within a single emulation step (one clock cycle or one instruction) are grouped into a transaction that is either fully applied or fully rolled back. A configurable state window maintains a ring buffer of recent states to support rewind, debugging, and snapshot operations.

### Rationale

ACID transactions ensure that a snapshot taken at any point captures a consistent state (not a half-updated state where the CPU has executed but the VIC-II has not yet ticked). The mutation queue also enables: state diffing for snapshot comparison, undo/rewind for debugging, and event sourcing for replay.

### Technical Specification

1. **Mutation Queue:**
   - All state-modifying operations enqueue a `StateMutation` record (target device, field offset, old value, new value).
   - Mutations are value types (structs) to avoid allocation (per TR-ALLOC-001).
   - The queue is drained and applied atomically at the end of each tick.

2. **ACID Properties:**
   - **Atomicity:** All mutations within a transaction (one tick) are applied together or not at all.
   - **Consistency:** After each transaction, all device states are internally consistent and cross-device invariants hold.
   - **Isolation:** The UI thread (reading state for display) sees only committed state, never in-progress mutations.
   - **Durability:** Committed state can be persisted to a snapshot at any transaction boundary.

3. **State Window:**
   - A configurable ring buffer holds the last N committed states (default: 60 frames worth, approximately 1.2 seconds at PAL rate).
   - Each entry in the ring buffer is a delta (diff from the previous state) to minimize memory usage.
   - Full state can be reconstructed by applying deltas forward from the nearest keyframe.
   - Keyframes (full state snapshots) are inserted at configurable intervals (default: every 30 frames).

4. **Transaction Boundary:**
   - For instruction-level accuracy: one transaction per instruction.
   - For cycle-level accuracy: one transaction per half-cycle (per TR-CYCLE-001).
   - The granularity is configurable via `IStateManager.TransactionGranularity`.

### Acceptance Criteria

1. A snapshot taken at any transaction boundary produces a state from which emulation can resume and produce deterministic output (per TR-DET-001).
2. Rolling back a transaction restores the exact previous state (verified by state hash comparison).
3. The state window supports rewind of at least 60 frames (1.2 seconds) without performance degradation.
4. Reconstructing full state from deltas produces a byte-identical result to the original full state.
5. The mutation queue processes at least 1 million mutations per second without exceeding the per-frame time budget.
6. UI reads of emulation state never observe a partially-committed transaction.
7. The state window memory footprint is bounded: delta entries average under 256 bytes per frame; keyframe entries are under 128KB.

### Verification Method

- Snapshot round-trip tests: save at arbitrary points, load, compare state hashes.
- Rewind tests: advance N frames, rewind N frames, advance again, verify identical state.
- Concurrency tests: UI thread reads state while emulation thread commits transactions, verify no torn reads.
- Memory benchmarks for state window under sustained operation.

### Related FRs

- FR-SNP-001 / FR-SNP-002 (Snapshot save/load relies on consistent state)
- FR-SNP-003 (Replay relies on deterministic state transitions)
- FR-SNP-004 (Snapshot diffing uses the mutation log)

### Related TRs

- TR-ALLOC-001 (Mutations are value types, no allocation)
- TR-DET-001 (Transaction ordering ensures determinism)
- TR-PUBSUB-001 (State change events flow through the pub/sub system)

### Design Decisions

- The mutation queue is a pre-allocated ring buffer of `StateMutation` structs, not a `List<T>`.
- Delta compression uses XOR encoding: the delta is the XOR of the old and new state bytes, yielding zero bytes for unchanged data which compress well.
- The UI accesses state through a "published snapshot" -- a copy-on-write reference that is updated atomically at frame boundaries.
