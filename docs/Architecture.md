# ViceSharp Architecture

## Design Philosophy

ViceSharp is a **library-first** emulator: the emulation engine is a set of composable .NET libraries. UI shells (console, Avalonia, future MAUI/WebAssembly) are thin consumers.

### Core Principles

1. **POCO model** — All emulator state lives in plain C# structs and records. No base classes, no ORM, no serialization attributes on hot-path types.
2. **Zero-allocation hot path** — The per-cycle emulation loop allocates zero managed objects. All transient data uses stack allocation, spans, or arena-pooled buffers.
3. **Deterministic** — Given identical initial state and input sequence, execution is bit-exact reproducible. This enables snapshot comparison, replay, and regression testing.
4. **NativeAOT compatible** — No reflection on the hot path. Source generators replace runtime discovery. All emulation assemblies pass trim analysis cleanly.
5. **MVVM** — ViewModels reference only `ViceSharp.Abstractions`. Views contain zero logic. The emulation engine has no UI dependencies.
6. **Host/UI boundary** — UI control, media, session, input, snapshot, capture, and diagnostic operations communicate with `ViceSharp.Hosting` through versioned gRPC services or narrow gRPC-backed client abstractions. The host owns emulator sessions, devices, media, snapshots, diagnostics, and local render-source composition.

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
    +-- ViceSharp.Hosting      Generic host integration, service registration, gRPC host boundary
    |
    +-- ViceSharp.Protocol     gRPC/protobuf contracts and generated client/server types
    |
    +-- ViceSharp.Console      NativeAOT reference shell
    |
    +-- ViceSharp.Avalonia     Desktop UI (Avalonia 12.x)
    |
    +-- ViceSharp.RomFetch     ROM download/validation tool
```

## Host/UI gRPC Boundary

`ViceSharp.Hosting` is the composition boundary for UI-facing emulator sessions. It creates machines from architecture descriptors, owns media and state services, and exposes control, remote output, input, media, snapshot, capture, and diagnostic operations through TR-GRPC-BOUNDARY-001.

UI control clients consume the host contract:
- Lifecycle commands use `HostControlService`.
- Remote video/audio and host events stream through `HostOutputService`.
- Keyboard and joystick events are normalized through `HostInputService`.
- Disk, tape, and cartridge attach/eject operations use `HostMediaService`.
- Snapshot, screenshot, and diagnostics commands use `HostStateService`.

The in-process Avalonia renderer is a narrow host-owned exception for frame presentation: it may bind directly to a local emulator/frame source so local rendering does not have to route frame buffers through gRPC. That binding belongs in the host/composition or render-surface layer, not in ViewModels, and it does not allow UI code to mutate emulator devices.

External or remote UIs use the gRPC video service/stream APIs where direct in-process rendering is unavailable. The UI control layer does not hold direct references to live core devices. Generated gRPC clients are adapted behind ViewModel-facing abstractions so TR-MVVM-001 remains enforceable.

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

Architectures are defined as `IArchitectureDescriptor` implementations. The `IArchitectureBuilder` is the assembly boundary between the selected system core and concrete chip instances. It constructs a running `IMachine` from a descriptor:

1. Select the machine profile's system-core definition
2. Instantiate devices listed in the descriptor
3. Wire chips, address spaces, buses, interrupt lines, clocks, ROM, and peripherals according to that system-core policy
4. Register the system core and chips in the machine device registry
5. Validate (no overlapping address ranges, required devices present)

The `IArchitectureValidator` catches configuration errors at build time, not runtime.

## Media Capture

ViceSharp exports emulator output through one gRPC `CaptureService` surface
(`GetCaptureCapabilities`, `CaptureFrame`, `StartCapture`, `StopCapture`,
`ListCaptures`). The host implementation (`CaptureServiceHost`) routes each request
to a concrete recorder in `ViceSharp.Core.Media`:

| Facility | Format(s) | Recorder | Tee point |
|----------|-----------|----------|-----------|
| Screenshot (one-shot) | `png`, `bmp` | `FrameCapture.CaptureBgraAsync` | reads the committed frame buffer |
| Video (frame sequence) | `bmpseq` (numbered 24-bit BMPs) | `FrameSequenceCapture` (`AllFrames` / `UniqueFrames`) | `EmulatorRuntimeSession.CommitFrame` |
| Video (muxed, with sound) | `mp4`, `mkv`, `avi` | `FfmpegVideoRecorder` (external ffmpeg) | `CommitFrame` (video) + `CaptureAudioTap` (audio) |
| Sound | `wav` (16-bit PCM) | `WavAudioRecorder` | `CaptureAudioTap` in the SID -> output path |

Key design points:

- **Two tee points, one worker thread.** Completed frames are teed from
  `CommitFrame` and SID samples from `CaptureAudioTap`, both on the emulation
  worker. A capture-only lock (`_captureSync`) keeps this off the lock-free UI
  frame-read path, and a recorder fault can never propagate onto the worker.
- **`IVideoCaptureSink`** unifies the BMP-sequence and ffmpeg recorders so the
  session drives either through one surface. The ffmpeg recorder also implements
  `IAudioRecorder`, so a single object receives both video frames and audio.
- **External ffmpeg, not libav.** `FfmpegVideoRecorder` mirrors VICE's
  `ffmpegexedrv`: it opens two loopback TCP servers, launches `ffmpeg` as a client
  of both, and streams raw BGRA video + s16le PCM for muxing into the chosen
  container. `FfmpegLocator` finds the binary (PATH or `VICESHARP_FFMPEG`), and the
  muxed formats are advertised by `GetCaptureCapabilities` only when ffmpeg is
  present. Launch happens off the session lock.
- **Parity-preserving audio tap.** `CaptureAudioTap` is installed in the SID audio
  path only when a real audio device exists, so headless and test hosts keep the
  SID silent (no timing perturbation) and simply advertise no sound/muxed-video
  capture.

The capture client surface is exposed to the Avalonia UI through
`IHostProtocolClient` (Snapshot menu: Save screenshot, Record sound, Record video).
