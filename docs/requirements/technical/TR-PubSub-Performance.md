# TR-PubSub-Performance: Pub/Sub Performance Technical Requirement

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Quality Area   | Performance / Messaging        |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-04-13                     |

---

## TR-PUBSUB-001: High-Performance Zero-Allocation Pub/Sub

**ID:** TR-PUBSUB-001
**Title:** <50ns Publish, <100ns Deliver, 0 Allocations Per Frame
**Priority:** P0 -- Critical
**Category:** Performance

### Description

The internal event/message bus used for inter-device communication (e.g., VIC-II asserting IRQ on the CPU, CIA timer underflow signaling the SID) shall achieve sub-100ns latency with zero managed heap allocations. This pub/sub system is the backbone of the emulation's device interconnect and fires hundreds of thousands of times per second.

### Rationale

At approximately 985,000 CPU cycles per second (PAL), even a 1-microsecond overhead per event would consume the entire frame budget. The pub/sub system must be faster than a virtual method call and must not produce GC pressure.

### Technical Specification

1. **Message Types:**
   - All message payloads are `readonly record struct` types with a maximum inline size of 64 bytes.
   - Messages use a discriminated union pattern (`MessageKind` enum + fixed-size payload) to avoid boxing.

2. **Publish Path:**
   - Publishing a message writes the payload to a pre-allocated slot in a lock-free ring buffer.
   - The publisher does not allocate, box, or copy to the managed heap.
   - Target latency: <50ns per publish operation.

3. **Delivery Path:**
   - Subscribers are registered as direct delegate references (not interface dispatch).
   - Delivery iterates a pre-allocated subscriber array (not a `List<T>` or `Dictionary<K,V>`).
   - Target latency: <100ns per deliver operation (including subscriber callback invocation).

4. **Per-Frame Budget:**
   - Total pub/sub overhead shall not exceed 5% of the per-frame time budget (PAL frame = 20ms, budget = 1ms).
   - Estimated message count per frame: approximately 2,000 (IRQ/NMI signals, DMA events, register writes).

5. **Zero Allocations:**
   - No allocations during publish, deliver, subscribe, or unsubscribe operations during steady-state emulation.
   - Subscriber registration may allocate during setup (before emulation starts) but not during runtime.

### Acceptance Criteria

1. Microbenchmark: publish 1 million messages in a tight loop; median per-publish latency is <50ns (measured via `Stopwatch` or hardware counters).
2. Microbenchmark: deliver 1 million messages to 3 subscribers; median per-deliver latency is <100ns.
3. Frame benchmark: a full PAL frame emulation (19,656 cycles) reports zero allocations from pub/sub (measured via `GC.GetAllocatedBytesForCurrentThread()`).
4. The pub/sub ring buffer has a fixed capacity (configurable, default 8,192 slots) and does not resize during emulation.
5. Subscriber registration and unregistration are O(1) operations using slot-based indexing.
6. Message ordering is preserved per-publisher (FIFO within a single source).

### Verification Method

- BenchmarkDotNet microbenchmarks with `[MemoryDiagnoser]` and `[HardwareCounters]`.
- Integration benchmark running 1,000 frames with allocation tracking.
- Stress test with maximum message volume (simulating worst-case sprite + badline + IRQ activity).

### Related TRs

- TR-ALLOC-001 (Zero allocation constraint applies to pub/sub)
- TR-CYCLE-001 (Sub-cycle events require fast pub/sub)
- TR-STATE-001 (State mutations may publish change events)
- TR-DET-001 (Message delivery order must be deterministic)

### Design Decisions

- The pub/sub system is not a general-purpose event bus; it is a specialized emulation interconnect.
- Message delivery is synchronous (inline) -- there is no async dispatch or thread marshaling within the emulation loop.
- Subscribers are sorted by priority at registration time; delivery order is fixed during emulation.
- The ring buffer uses `Unsafe.As<TFrom, TTo>()` for zero-copy payload reinterpretation between message types.
