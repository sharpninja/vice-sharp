# ViceSharp Architecture

## Design Philosophy

ViceSharp is a **library-first** emulator: the emulation engine is a set of composable .NET libraries. UI shells (console, Avalonia, future MAUI/WebAssembly) are thin consumers.

### Core Principles

1. **POCO model** — All emulator state lives in plain C# structs and records. No base classes, no ORM, no serialization attributes on hot-path types.
2. **Zero-allocation hot path** — The per-cycle emulation loop allocates zero managed objects. All transient data uses stack allocation, spans, or arena-pooled buffers.
3. **Deterministic** — Given identical initial state and input sequence, execution is bit-exact reproducible. This enables snapshot comparison, replay, and regression testing.
4. **NativeAOT compatible** — No reflection on the hot path. Source generators replace runtime discovery. All emulation assemblies pass trim analysis cleanly.
5. **MVVM** — ViewModels reference only `ViceSharp.Abstractions`. Views contain zero logic. The emulation engine has no UI dependencies.

## Assembly Structure

```
ViceSharp.Abstractions     33+ interfaces, value types, attributes
    |
    +-- ViceSharp.Core         Bus, clock, mutation queue, pub/sub, snapshots
    |       |
    |       +-- ViceSharp.Chips          CPU, VIC-II, SID, CIA, VIA, PLA stubs
    |       |
    |       +-- ViceSharp.Architectures  Machine definitions (C64, VIC-20, etc.)
    |
    +-- ViceSharp.SourceGen    Roslyn source generator (device registration)
    |
    +-- ViceSharp.Monitor      Debugger/monitor engine
    |
    +-- ViceSharp.Hosting      Generic host integration, service registration
    |
    +-- ViceSharp.Console      NativeAOT reference shell
    |
    +-- ViceSharp.Avalonia     Desktop UI (Avalonia 12.x)
    |
    +-- ViceSharp.RomFetch     ROM download/validation tool
```

## Device Model

Every hardware component is an `IDevice` with a unique `DeviceId`. Devices are registered with the bus and optionally subscribe to the system clock.

```
IDevice
    IClockedDevice          — receives clock ticks
    IAddressSpace           — maps address ranges on the bus
    IInterruptSource        — can raise IRQ/NMI
    IPeripheral             — external device (drive, datasette)
```

Devices are wired together by an `IArchitecture` which describes:
- Which devices exist
- Address space mappings (including bank-switched overlays)
- Clock divisors and phase relationships
- Interrupt routing

## Bus and Address Decoding

The `IBus` provides a flat 64KB address space. Address decoding is performed by the architecture's PLA/banking logic, which routes reads and writes to the correct `IAddressSpace` implementor.

For the C64:
- RAM underlays the entire 64KB
- ROM (BASIC, KERNAL, CHARGEN) overlays configurable regions
- I/O area ($D000-$DFFF) maps to VIC-II, SID, CIA1, CIA2, color RAM
- PLA control lines (from CPU port at $0000/$0001) select the active configuration

## Mutation Queue

All state changes flow through an `IMutationQueue`. Each mutation is a small struct describing: source device, target address/field, old value, new value, cycle timestamp.

Benefits:
- **Auditing** — full history of every state change
- **Undo** — reverse mutations for debugging
- **Determinism** — replay mutations to reproduce exact state
- **Networking** — serialize mutation stream for netplay

The queue is double-buffered: the emulation thread writes to the active buffer while consumers (UI, debugger, recorder) read the committed buffer.

## Pub/Sub and Message Pool

High-frequency inter-device communication (interrupt signals, DMA requests, bus contention notifications) uses a lock-free pub/sub system.

- `IMessagePool` — pre-allocates message slots to avoid allocation
- `PayloadArena` — bump allocator for variable-size payloads within a frame
- `MessageHandle` — reference-counted handle into the pool
- `IPubSub` — topic-based publish/subscribe with zero-copy delivery

The pool and arena reset at frame boundaries, making per-frame allocation effectively free.

## State and Snapshots

The `ISnapshot` interface captures the complete machine state as a flat byte array. Snapshots are:
- **Serializable** — save/load to disk
- **Comparable** — byte-exact comparison for determinism testing
- **Diffable** — compute delta between two snapshots for incremental save

The `StateWindow` concept allows configuring how much history is retained:
- Snapshot interval (e.g., every N frames)
- Maximum history depth
- Memory budget

## Clock and Timing

The system clock drives all cycle-accurate behavior. Each `IClockedDevice` receives ticks at its configured rate (which may differ from the master clock via divisors).

For the C64:
- Master clock: ~985,248 Hz (PAL) or ~1,022,727 Hz (NTSC)
- CPU: master / 1 (same as master)
- VIC-II: master / 1 (interleaved with CPU via bus phases)
- SID: master / 1 (but updates at its own internal rate)
- CIA timers: count CPU cycles or external events

## Architecture Registration

Architectures are defined as `IArchitectureDescriptor` implementations. The `IArchitectureBuilder` constructs a running `IMachine` from a descriptor:

1. Instantiate devices listed in the descriptor
2. Wire address spaces to the bus per the descriptor's memory map
3. Connect interrupt lines
4. Set clock divisors
5. Validate (no overlapping address ranges, required devices present)

The `IArchitectureValidator` catches configuration errors at build time, not runtime.

## Media Capture

ViceSharp supports capturing output in multiple formats:

- `IScreenshotCapture` — single-frame PNG/BMP capture from `IFrameSink`
- `IVideoRecorder` — continuous frame recording to MP4 via FFmpeg (AoT-compatible via P/Invoke)
- `IAudioRecorder` — audio stream recording to WAV/FLAC
- `IMediaCaptureSession` — coordinated A/V recording with synchronized timestamps

All capture interfaces operate on the committed frame buffer, never blocking the emulation thread.
