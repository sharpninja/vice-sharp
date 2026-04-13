# StateWindow

The StateWindow controls how much emulation history ViceSharp retains in memory. It governs the frequency of automatic snapshots, the depth of the snapshot ring buffer, and the total memory budget allocated to state retention.

## Configuration Surface

The `StateWindowConfig` value type holds all configuration parameters:

| Parameter | Type | Description |
|-----------|------|-------------|
| `SnapshotIntervalFrames` | `int` | Number of frames between automatic snapshots. Lower values give finer rewind granularity at higher memory cost. |
| `HistoryDepth` | `int` | Maximum number of snapshots retained in the ring buffer. When full, the oldest snapshot is evicted. |
| `MemoryBudgetBytes` | `long` | Hard cap on total memory used by the snapshot ring buffer. If the budget would be exceeded, the oldest snapshots are evicted regardless of `HistoryDepth`. |
| `EnableDeltaCompression` | `bool` | When true, only the diff between consecutive snapshots is stored, dramatically reducing per-slot memory. |
| `MutationRetentionFrames` | `int` | How many frames of raw mutation history to retain alongside snapshots. Mutations provide finer-grained undo than snapshots alone. |

Configuration is set on the `ISnapshotStore` and can be changed at runtime. Changing the configuration does not discard existing snapshots; it applies to subsequent captures.

## Presets

ViceSharp provides four built-in presets covering common use cases. Presets are starting points; all parameters can be individually overridden after applying a preset.

### Minimal

Optimized for low-memory environments or when state history is not needed.

| Parameter | Value |
|-----------|-------|
| SnapshotIntervalFrames | 300 (every ~6 seconds at 50 fps PAL) |
| HistoryDepth | 3 |
| MemoryBudgetBytes | 1 MB |
| EnableDeltaCompression | true |
| MutationRetentionFrames | 0 |

Use case: Embedded targets, CI/CD test runs, headless operation where only save/load is needed.

### Standard

Balanced defaults for typical desktop use.

| Parameter | Value |
|-----------|-------|
| SnapshotIntervalFrames | 50 (every ~1 second at 50 fps PAL) |
| HistoryDepth | 60 |
| MemoryBudgetBytes | 32 MB |
| EnableDeltaCompression | true |
| MutationRetentionFrames | 50 |

Use case: Normal gameplay with rewind support. Provides approximately 60 seconds of rewind history.

### Debug

High-frequency snapshots for development and debugging workflows.

| Parameter | Value |
|-----------|-------|
| SnapshotIntervalFrames | 1 (every frame) |
| HistoryDepth | 600 |
| MemoryBudgetBytes | 256 MB |
| EnableDeltaCompression | true |
| MutationRetentionFrames | 600 |

Use case: Development, debugging, determinism testing. Provides per-frame rewind for approximately 12 seconds (PAL). Full mutation history enables instruction-level undo.

### Replay

Extended history for recording and replay sessions.

| Parameter | Value |
|-----------|-------|
| SnapshotIntervalFrames | 150 (every ~3 seconds at 50 fps PAL) |
| HistoryDepth | 1200 |
| MemoryBudgetBytes | 128 MB |
| EnableDeltaCompression | true |
| MutationRetentionFrames | 150 |

Use case: Long recording sessions, TAS (tool-assisted speedrun) workflows, replay analysis. Provides approximately 60 minutes of rewind at coarser granularity.

## Memory Math

The memory consumed by the StateWindow is determined by the snapshot size, history depth, and whether delta compression is enabled.

### Full Snapshot Sizing

A full snapshot captures the complete machine state. Sizes vary by architecture:

| Architecture | Full Snapshot Size | Primary Components |
|-------------|-------------------|-------------------|
| C64 | ~72 KB | 64 KB RAM + 4 KB color RAM + CPU regs + VIC-II state + SID state + CIA x2 state + PLA state |
| VIC-20 (unexpanded) | ~12 KB | 5 KB RAM + CPU regs + VIC state + VIA x2 state |
| VIC-20 (full expansion) | ~40 KB | 32 KB RAM + CPU regs + VIC state + VIA x2 state |
| C128 | ~200 KB | 128 KB RAM + VIC-IIe + VDC + MMU + CPU regs + Z80 regs |
| PET 8032 | ~40 KB | 32 KB RAM + CRTC + PIA + VIA + CPU regs |

### Memory Calculation

Without delta compression:

```
Total memory = HistoryDepth * FullSnapshotSize
```

Example (Standard preset, C64):
```
60 snapshots * 72 KB = 4,320 KB (~4.2 MB)
```

With delta compression, consecutive snapshots typically share 95-99% of their data. The delta stores only changed bytes plus a small header:

```
Delta size ~= FullSnapshotSize * (1 - similarity ratio) + header overhead
Typical delta ~= 72 KB * 0.03 + 64 bytes = ~2.2 KB per delta
```

Example (Standard preset, C64, delta compression):
```
1 full snapshot (base) + 59 deltas
= 72 KB + (59 * 2.2 KB) = 72 KB + 130 KB = ~202 KB
```

Delta compression reduces memory consumption by approximately 20x for typical workloads.

### Budget Enforcement

The `MemoryBudgetBytes` parameter acts as a hard ceiling. When a new snapshot would exceed the budget:

1. The oldest snapshot(s) in the ring buffer are evicted until space is available.
2. If delta compression is enabled and the base snapshot is evicted, the next delta is promoted to a full snapshot (decompressed) to maintain the chain.
3. If a single full snapshot exceeds the budget, the capture is rejected and a diagnostic warning is emitted.

This ensures the StateWindow never exceeds its configured memory allocation, even if snapshot sizes vary due to architecture differences or expansion hardware.

### Mutation Retention Overhead

Each mutation record is approximately 16 bytes:

```
DeviceId (4) + TargetAddress (2) + OldValue (1) + NewValue (1) + CycleStamp (8) = 16 bytes
```

A typical C64 frame generates 5,000-15,000 mutations. At the high end:

```
15,000 mutations/frame * 16 bytes = 240 KB per frame of mutation history
50 frames * 240 KB = 12 MB for MutationRetentionFrames=50
```

Mutation retention is accounted for within the same `MemoryBudgetBytes` allocation as snapshots.

## Interaction with Mutation Queue

The StateWindow and `IMutationQueue` work together to provide both coarse (snapshot) and fine (mutation) history:

1. **Snapshot capture:** At every `SnapshotIntervalFrames` boundary, `ISnapshotStore.Capture()` is called. This reads the machine's current state into a new `ISnapshot` and inserts it into the ring buffer.

2. **Mutation buffering:** Between snapshots, every state change flows through `IMutationQueue.Enqueue()`. The mutation queue is double-buffered: the emulation thread writes to the active buffer, and `Commit()` at each frame boundary swaps the buffers.

3. **Rewind via snapshots:** To rewind to a specific point, the nearest preceding snapshot is restored, and then any mutations between that snapshot and the target time are replayed forward.

4. **Rewind via mutations:** For sub-snapshot granularity (when `MutationRetentionFrames > 0`), individual mutations can be reversed in LIFO order to step backward one state change at a time.

5. **Frame boundary lifecycle:**
   - `IMutationQueue.Commit()` swaps mutation buffers
   - If this frame is a snapshot boundary, `ISnapshotStore.Capture()` takes a snapshot
   - `IPubSub.FrameReset()` resets the message pool and payload arena
   - Consumers process committed mutations (UI update, debugger, recorder)
   - `IMutationQueue.AcknowledgeCommitted()` marks committed buffer as consumed

## Determinism Guarantees

The StateWindow is designed to support deterministic replay:

- Snapshots are byte-exact: two independent executions from the same initial state with the same inputs produce identical snapshots.
- `ISnapshot.DiffAgainst()` returns an empty diff for deterministic runs, enabling automated regression testing.
- Delta compression preserves determinism: decompressing a delta chain produces a byte-identical result to a full snapshot taken at the same point.
- Mutation replay is order-preserving: replaying the same mutation sequence from the same snapshot always produces the same end state.
