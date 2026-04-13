# Pub/Sub and Message Pool

## Overview

ViceSharp's inter-device communication uses a lock-free publish/subscribe system optimized for zero allocations on the hot path. Devices communicate events (interrupts, DMA requests, bus contention) through this system rather than direct method calls, enabling decoupled architecture and auditable event flow.

## IMessagePool

The message pool pre-allocates a fixed number of message slots at startup. During emulation, messages are borrowed from and returned to the pool without allocation.

### Capacity

Default pool size: 4096 messages. Sized for worst-case frame activity:
- ~312 scanlines per frame (PAL)
- Multiple events per scanline (VIC-II badline, sprite DMA, CIA timer, etc.)
- Typical frame: 500-1500 messages

### Lifecycle

```
Pool.Rent() → MessageHandle → publish → subscribers process → handle.Release() → Pool.Return()
```

The pool uses a lock-free stack (Interlocked.CompareExchange) for rent/return. Exhaustion is a fatal error in debug builds and silently drops in release (with a performance counter increment).

### Frame-Boundary Reset

At the end of each emulated frame, any unreturned messages are forcibly reclaimed. This prevents slow leaks from buggy subscribers. A warning counter tracks forced reclamations.

## PayloadArena

Variable-size message payloads (e.g., a block of memory bytes for DMA, a set of register values) are allocated from a bump allocator that resets each frame.

### Design

```
Frame start → arena.Reset()
During frame → arena.Allocate(size) returns Span<byte>
Frame end → arena.Reset() (all allocations invalidated)
```

The arena is a single contiguous byte array (default: 64KB). Allocation is a single pointer increment — the fastest possible allocator.

### Constraints

- Allocations are valid only within the current frame
- No individual deallocation (arena is all-or-nothing)
- If the arena fills, it doubles (up to a configured maximum), then warns
- Payloads must be consumed before frame boundary

## MessageHandle

A reference-counted handle into the message pool. Handles are value types (structs) to avoid allocation.

### Reference Counting

```csharp
public readonly struct MessageHandle : IDisposable
{
    // Pool slot index (immutable)
    // Reference count (interlocked increment/decrement)
    // Topic (interned string, set at publish time)
    // PayloadOffset + PayloadLength (into arena)
}
```

When ref count reaches zero, the message slot is returned to the pool. Subscribers increment the ref count when they receive a handle and decrement when done processing.

### Zero-Copy Delivery

Subscribers receive the same `MessageHandle` — no copying. The payload `Span<byte>` points directly into the arena. This makes delivery O(1) regardless of payload size.

## IPubSub

### Topic-Based Routing

Topics are interned strings for fast comparison:
- `"irq"` — IRQ line asserted/deasserted
- `"nmi"` — NMI line asserted/deasserted
- `"ba"` — BA line (VIC-II signals bus available)
- `"aec"` — AEC line (address enable control)
- `"dma"` — DMA transfer initiated
- `"clock"` — clock phase notification
- `"state"` — state mutation notification

### Subscription Management

```
IPubSub.Subscribe(topic, handler)    → SubscriptionHandle
IPubSub.Unsubscribe(handle)
IPubSub.Publish(topic, payload)      → MessageHandle
```

Subscriptions are stored in a flat array per topic (cache-friendly iteration). Adding/removing subscriptions is rare (device init/teardown only) and takes a lock. Publishing and delivering are lock-free.

### Delivery Order

Within a single `Publish()` call, subscribers are notified in registration order. This is deterministic — same device init order produces same delivery order.

## Performance Targets

| Operation | Target | Measured |
|-----------|--------|----------|
| Pool.Rent() | <20ns | — |
| Pool.Return() | <20ns | — |
| Arena.Allocate() | <10ns | — |
| IPubSub.Publish() | <50ns | — |
| Per-subscriber delivery | <100ns | — |
| Total per-frame overhead | <50us | — |
| Allocations per frame | 0 | — |

Measured column populated during Iteration 1 benchmarking.

## Integration with Mutation Queue

The pub/sub system and mutation queue serve complementary roles:

- **Pub/sub:** transient, intra-frame, device-to-device signals (interrupts, bus events)
- **Mutation queue:** persistent, auditable state changes (register writes, memory modifications)

A device might publish an `"irq"` message via pub/sub AND enqueue a mutation recording the interrupt source and cycle. The pub/sub message drives real-time behavior; the mutation enables replay and debugging.
