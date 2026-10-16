# TR-Zero-Allocation: Zero Allocation Technical Requirement

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Quality Area   | Performance / GC Pressure      |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-04-13                     |

---

## TR-ALLOC-001: Zero Managed Allocations Per Emulation Cycle on Hot Path

**ID:** TR-ALLOC-001
**Title:** Zero Managed Allocations Per Emulation Cycle on Hot Path
**Priority:** P0 -- Critical
**Category:** Performance

### Description

The emulation hot path (the per-cycle execution of CPU, VIC-II, SID, and CIA ticks) shall produce zero managed heap allocations during steady-state operation. This eliminates GC pauses that would cause frame drops and audio glitches. All per-cycle data structures shall use value types (structs), stack allocation, or pre-allocated buffers.

### Rationale

A single C64 frame at PAL timing requires approximately 19,656 CPU cycles (312 lines x 63 cycles). At 50fps, that is approximately 985,000 ticks per second. Even a 24-byte allocation per tick would generate approximately 23MB/s of GC pressure, causing Gen0 collections every few milliseconds and introducing unpredictable latency.

### Technical Specification

1. **Struct-Based Value Types:** Core data structures (CpuState, VicState, SidVoiceState, CiaState) are `readonly struct` or `ref struct` types.
2. **PayloadArena Bump Allocator:** Variable-size data within a frame (sprite line buffers, audio sample batches) uses a `PayloadArena<T>` bump allocator that resets per frame.
3. **Span/Memory Usage:** All byte-buffer operations use `Span<byte>` or `Memory<byte>` backed by pre-allocated arrays or native memory.
4. **No Boxing:** Interface dispatch on value types uses generic constraints (`where T : struct, IFoo`) to avoid boxing. Critical paths use concrete types directly.
5. **No String Concatenation:** Logging on the hot path uses structured logging with pre-allocated message templates, not string interpolation.
6. **No LINQ on Hot Path:** LINQ extension methods allocate enumerator objects; hot-path iteration uses `for`/`foreach` over arrays or spans.
7. **No Closures on Hot Path:** Lambda captures that close over local variables allocate a display class; hot-path callbacks use static lambdas or direct method references.

### Acceptance Criteria

1. A benchmark running 10 million emulation cycles reports zero `GC.GetAllocatedBytesForCurrentThread()` delta on the hot-path thread.
2. `dotnet-counters` monitoring during a 60-second emulation session shows zero Gen0 collections attributable to the emulation thread.
3. The `[NoAlloc]` custom attribute (checked by a Roslyn analyzer) is applied to all hot-path methods, and the analyzer reports zero violations.
4. The PayloadArena reset occurs exactly once per frame and completes in under 100ns.
5. All event/message payloads in the pub/sub system are value types (per TR-PUBSUB-001).
6. No `new` keyword appears in hot-path code paths except for stack-allocated `Span<T>` or `stackalloc`.

### Verification Method

- Allocation-tracking benchmark in the performance test suite.
- Roslyn analyzer that flags heap allocations in methods annotated with `[NoAlloc]`.
- `dotnet-trace` GC event analysis during integration test runs.
- Code review checklist item for all hot-path PRs.

### Related TRs

- TR-AOT-001 (Struct types are inherently AoT-friendly)
- TR-PUBSUB-001 (Zero allocations in the messaging system)
- TR-SIMD-001 (SIMD operations on stack-allocated vectors)

### Design Decisions

- CPU state is a `readonly record struct` passed by `ref` or `in` to avoid copies.
- The frame buffer is a pre-allocated `byte[]` (or native memory) reused every frame.
- Audio sample buffers are pre-allocated ring buffers sized for the maximum samples-per-frame.
- Event payloads use a tagged union (`readonly struct EventPayload`) with an inline fixed buffer for small data.
