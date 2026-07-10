# ViceSharp Public API Reference

This document describes the public interfaces defined in `ViceSharp.Abstractions` (50 interface files as of v1.0.2). The interfaces are explicit and trim-aware: no reflection on the hot path, no base classes, and all members are explicitly defined for source-generator discovery. Current application packaging is self-contained managed code with ReadyToRun, not native ahead-of-time publishing.

This reference covers the foundational contracts. Chip-level and machine-level contracts (`ICpu`, `IVideoChip`, `IAudioChip`, `ICiaChip`, `IMemory`, the input/storage port interfaces, `IStatefulDevice`, `ISystemCoordinator`, `IInterSystemBus`, the lockstep oracle `IViceNative`, and others) are documented by their XML doc comments in `src/ViceSharp.Abstractions`.

---

## Core Interfaces

These interfaces define the fundamental emulation primitives: bus access, clock-driven execution, and interrupt signaling.

### `IBus`

**Assembly:** `ViceSharp.Abstractions`

Provides a flat address space with read/write access, representing the system data bus. All memory-mapped I/O, RAM, and ROM access flows through this interface. Address decoding is delegated to the registered `IAddressSpace` devices.

```csharp
public interface IBus
{
    /// <summary>Reads a byte from the specified 16-bit address.</summary>
    byte Read(ushort address);

    /// <summary>Writes a byte to the specified 16-bit address.</summary>
    void Write(ushort address, byte value);

    /// <summary>Reads a byte without side effects (monitor/debugger inspection).</summary>
    byte Peek(ushort address);

    /// <summary>Registers an address space handler on the bus.</summary>
    void RegisterDevice(IAddressSpace device);

    /// <summary>Removes an address space handler from the bus.</summary>
    void UnregisterDevice(IAddressSpace device);
}
```

### `IClockedDevice`

**Assembly:** `ViceSharp.Abstractions`

Represents any device that receives ticks from the system clock. The clock subsystem calls `Tick()` at the device's configured rate, which may differ from the master clock frequency via a divisor.

```csharp
public interface IClockedDevice : IDevice
{
    /// <summary>Advances the device by one clock cycle.</summary>
    void Tick();

    /// <summary>Clock divisor relative to master clock. 1 = same as master.</summary>
    uint ClockDivisor { get; }

    /// <summary>The clock phase this device operates on (phi1 or phi2).</summary>
    ClockPhase Phase { get; }
}
```

### `IInterruptLine`

**Assembly:** `ViceSharp.Abstractions`

Models a physical interrupt line (IRQ or NMI). Multiple sources can assert the same line; the line remains active as long as any source holds it asserted (active-low, open-drain behavior matching real hardware).

```csharp
public interface IInterruptLine
{
    /// <summary>Assert (pull low) the interrupt line from the given source.</summary>
    void Assert(IInterruptSource source);

    /// <summary>Deassert (release) the interrupt line from the given source.</summary>
    void Release(IInterruptSource source);

    /// <summary>True when at least one source is asserting the line.</summary>
    bool IsAsserted { get; }

    /// <summary>The type of interrupt this line carries.</summary>
    InterruptType Type { get; }
}
```

### `IInterruptSource`

**Assembly:** `ViceSharp.Abstractions`

Implemented by any device capable of raising interrupts. Provides identification for the source so the interrupt line can track which sources are currently asserting.

```csharp
public interface IInterruptSource : IDevice
{
    /// <summary>Unique identifier for this interrupt source.</summary>
    DeviceId SourceId { get; }

    /// <summary>The interrupt line(s) this source is connected to.</summary>
    IReadOnlyList<IInterruptLine> ConnectedLines { get; }
}
```

### `IAddressSpace`

**Assembly:** `ViceSharp.Abstractions`

Implemented by devices that occupy a region of the address bus. The bus routes reads and writes to the correct `IAddressSpace` implementor; each implementor answers `HandlesAddress` for the current banking configuration.

```csharp
public interface IAddressSpace : IDevice
{
    /// <summary>Reads a byte at the given system address.</summary>
    byte Read(ushort address);

    /// <summary>Writes a byte at the given system address.</summary>
    void Write(ushort address, byte value);

    /// <summary>Reads a byte without side effects (monitor/debugger inspection).</summary>
    byte Peek(ushort address);

    /// <summary>True if this space currently decodes the given address.</summary>
    bool HandlesAddress(ushort address);
}
```

### `IClock`

**Assembly:** `ViceSharp.Abstractions`

The master system clock. Drives all cycle-accurate timing. Manages registration of clocked devices and distributes ticks according to each device's divisor and phase configuration.

```csharp
public interface IClock
{
    /// <summary>Advances the clock by one master cycle, ticking all due devices.</summary>
    void Step();

    /// <summary>Advances the clock by the specified number of master cycles.</summary>
    void Step(long cycles);

    /// <summary>Total master cycles elapsed since reset.</summary>
    long TotalCycles { get; }

    /// <summary>Master clock frequency in Hz.</summary>
    long FrequencyHz { get; }

    /// <summary>Registers a device to receive clock ticks.</summary>
    void Register(IClockedDevice device);

    /// <summary>Unregisters a device from clock tick distribution.</summary>
    void Unregister(IClockedDevice device);

    /// <summary>Resets the cycle counter.</summary>
    void Reset();
}
```

---

## System Interfaces

These interfaces define the machine abstraction: how devices are organized into a running system.

### `ISystem`

**Assembly:** `ViceSharp.Abstractions`

Top-level container for a running emulator instance. Holds the machine and manages lifecycle (start, stop, reset).

```csharp
public interface ISystem
{
    /// <summary>The currently loaded machine.</summary>
    IMachine Machine { get; }

    /// <summary>Starts the emulation loop.</summary>
    void Start();

    /// <summary>Stops the emulation loop, preserving state.</summary>
    void Stop();

    /// <summary>Performs a hardware reset (equivalent to power-cycle).</summary>
    void Reset();

    /// <summary>True if the emulation loop is currently running.</summary>
    bool IsRunning { get; }
}
```

### `IMachine`

**Assembly:** `ViceSharp.Abstractions`

Represents a specific hardware configuration (e.g., C64 PAL). Assembled from an `IArchitectureDescriptor` by the `IArchitectureBuilder`. Owns the bus, clock, and device registry, and exposes optional pub/sub and CPU accessors with default implementations.

```csharp
public interface IMachine
{
    /// <summary>The system bus for this machine.</summary>
    IBus Bus { get; }

    /// <summary>The master clock for this machine.</summary>
    IClock Clock { get; }

    /// <summary>Registry of all devices in this machine.</summary>
    IDeviceRegistry Devices { get; }

    /// <summary>The architecture descriptor this machine was built from.</summary>
    IArchitectureDescriptor Architecture { get; }

    /// <summary>Executes one frame (all cycles for one video frame).</summary>
    void RunFrame();

    /// <summary>Executes a single CPU instruction (variable cycle count).</summary>
    void StepInstruction();

    /// <summary>Captures the machine state for lockstep comparison.</summary>
    MachineState GetState();

    /// <summary>Resets the machine.</summary>
    void Reset();

    /// <summary>The machine's pub/sub hub, or null when not wired.</summary>
    IPubSub? PubSub => null;

    /// <summary>The primary (host system) CPU, or null when not exposed.</summary>
    ICpu? PrimaryCpu => null;

    /// <summary>Per-CPU cycle counts for the status surface (host CPU plus attached drive CPUs).</summary>
    IReadOnlyList<CpuInfo> CpuInfos { get; }
}
```

### `IDevice`

**Assembly:** `ViceSharp.Abstractions`

Base interface for all emulated hardware components. Every chip, memory bank, and peripheral implements `IDevice`.

```csharp
public interface IDevice
{
    /// <summary>Unique identifier for this device instance.</summary>
    DeviceId Id { get; }

    /// <summary>Human-readable device name (e.g., "MOS 6510 CPU").</summary>
    string Name { get; }

    /// <summary>Resets the device (equivalent to hardware reset line).</summary>
    void Reset();
}
```

### `IPeripheral`

**Assembly:** `ViceSharp.Abstractions`

Represents an external peripheral device such as a disk drive, datasette, or cartridge. Peripherals can be connected and disconnected at runtime.

```csharp
public interface IPeripheral : IDevice
{
    /// <summary>Connects the peripheral to the machine.</summary>
    void Attach();

    /// <summary>Disconnects the peripheral from the machine.</summary>
    void Detach();

    /// <summary>True if the peripheral is currently attached.</summary>
    bool IsAttached { get; }
}
```

### `IDeviceRegistry`

**Assembly:** `ViceSharp.Abstractions`

Registry of all devices within a machine. Populated by the architecture builder during machine construction. Supports lookup by ID, type, and role.

```csharp
public interface IDeviceRegistry
{
    /// <summary>Returns the device with the given ID, or null if not found.</summary>
    IDevice? GetById(DeviceId id);

    /// <summary>Returns all devices implementing the specified interface.</summary>
    IReadOnlyList<T> GetAll<T>() where T : IDevice;

    /// <summary>Returns all registered devices.</summary>
    IReadOnlyList<IDevice> All { get; }

    /// <summary>Returns the single device with the given role, or null.</summary>
    IDevice? GetByRole(DeviceRole role);

    /// <summary>Total number of registered devices.</summary>
    int Count { get; }
}
```

---

## Architecture Interfaces

These interfaces define how machines are described, constructed, and validated.

### `IArchitectureDescriptor`

**Assembly:** `ViceSharp.Abstractions`

Declarative description of a machine architecture. Lists the devices, master clock, video standard, and ROM set needed to construct a running machine. Descriptors are immutable data.

```csharp
public interface IArchitectureDescriptor
{
    /// <summary>Machine name (e.g., "Commodore 64 PAL").</summary>
    string MachineName { get; }

    /// <summary>Master clock frequency in Hz.</summary>
    long MasterClockHz { get; }

    /// <summary>Video standard (PAL or NTSC).</summary>
    VideoStandard VideoStandard { get; }

    /// <summary>Devices required by this architecture.</summary>
    IReadOnlyList<DeviceDescriptor> Devices { get; }

    /// <summary>Required ROM set for this architecture, or null when none.</summary>
    IRomSet? RequiredRoms { get; }
}
```

### `IArchitectureBuilder`

**Assembly:** `ViceSharp.Abstractions`

Constructs a running `IMachine` from an `IArchitectureDescriptor`. Performs the assembly sequence: select the profile's system core, instantiate chips, map address spaces, connect interrupts, configure clocks, and validate. The builder is the glue between system-core policy and concrete chip instances.

```csharp
public interface IArchitectureBuilder
{
    /// <summary>Builds a machine from the given architecture descriptor.</summary>
    IMachine Build(IArchitectureDescriptor descriptor);
}
```

### `IArchitectureValidator`

**Assembly:** `ViceSharp.Abstractions`

Validates an architecture descriptor for correctness before construction. Catches configuration errors such as overlapping address ranges and missing required devices.

```csharp
public interface IArchitectureValidator
{
    /// <summary>Runs all validation rules against the descriptor.</summary>
    bool Validate(IArchitectureDescriptor descriptor);
}
```

---

## Services Interfaces

These interfaces define external services consumed by the emulation engine.

### `IRomProvider`

**Assembly:** `ViceSharp.Abstractions`

Loads ROM images for a given architecture. The concrete loader (`ViceSharp.RomFetch`) validates ROM integrity against pinned MD5/SHA1 descriptor hashes before returning data.

```csharp
public interface IRomProvider
{
    /// <summary>Loads a ROM file for the specified architecture.</summary>
    ReadOnlyMemory<byte> LoadRom(string romName, string architecture);

    /// <summary>Checks if a ROM is available.</summary>
    bool IsAvailable(string romName, string architecture);
}
```

### `IRomSet`

**Assembly:** `ViceSharp.Abstractions`

Describes the complete set of ROMs required by a specific architecture (e.g., KERNAL, BASIC, CHARGEN for C64).

```csharp
public interface IRomSet
{
    /// <summary>Architecture this ROM set belongs to (e.g., "c64").</summary>
    string Architecture { get; }

    /// <summary>Checks if all required ROMs are present and valid.</summary>
    bool IsComplete(IRomProvider provider);
}
```

### `IAudioBackend`

**Assembly:** `ViceSharp.Abstractions`

Platform-specific audio output. Receives PCM samples from the emulation engine and delivers them to the host audio system. Backends are constructed pre-configured (e.g., the desktop `WinMmAudioBackend` via `AudioBackendFactory` in `ViceSharp.Host`); there is no separate initialize step.

```csharp
public interface IAudioBackend
{
    /// <summary>Submits a buffer of PCM samples for playback.</summary>
    void SubmitSamples(ReadOnlySpan<float> samples);

    /// <summary>Number of samples currently queued for playback.</summary>
    int QueuedSampleCount { get; }

    /// <summary>
    /// Number of additional samples the playback device can accept without blocking.
    /// Backends that do not expose finite device space report a large value.
    /// </summary>
    int AvailableSampleCount => int.MaxValue;

    /// <summary>Pauses audio playback without discarding buffered data.</summary>
    void Pause();

    /// <summary>Resumes audio playback.</summary>
    void Resume();

    /// <summary>Stops playback and discards all buffered samples.</summary>
    void Stop();
}
```

### `IFrameSink`

**Assembly:** `ViceSharp.Abstractions`

Receives completed video frames from the emulation engine. The video chip writes into a back buffer; when a frame is complete, it is swapped to the front buffer and delivered to the sink as raw pixel data.

```csharp
public interface IFrameSink
{
    /// <summary>Called when a complete frame is ready for display.</summary>
    void PresentFrame(ReadOnlySpan<byte> pixelData);

    /// <summary>True if the sink is ready to accept a new frame.</summary>
    bool IsReady { get; }

    /// <summary>Total frames presented since last reset.</summary>
    long FrameCount { get; }
}
```

### `IInputSource`

**Assembly:** `ViceSharp.Abstractions`

Abstracts a host input device. Host shells poll the source each frame; concrete keyboard/joystick translation flows through `IMachineKeyboardInput`, `IMachineJoystickInput`, `IKeyboardMatrix`, and `IKeyboardInputMap`.

```csharp
public interface IInputSource
{
    /// <summary>Polls for new input events since the last call.</summary>
    void Poll();

    /// <summary>True if this input source is currently connected/active.</summary>
    bool IsConnected { get; }
}
```

---

## Media Interfaces

Media export is built from two small abstractions plus concrete recorders in
`ViceSharp.Core.Media` / `ViceSharp.Core.Capture`, all driven by the host's gRPC
`CaptureService`. Capture operates on the committed frame buffer and the SID audio
tap, never blocking the emulation thread's lock-free UI read path.

### `IAudioRecorder`

**Assembly:** `ViceSharp.Abstractions`

A persistent audio sink that captures the emulator's PCM stream to a file. Fed
int16 samples by `CaptureAudioTap` (which clamps and scales the SID float output).
Implemented by `WavAudioRecorder` (RIFF/WAVE 16-bit PCM) and by
`FfmpegVideoRecorder` (its audio track).

```csharp
/// <summary>
/// Persistent audio sink: writes a container header on construction, appends
/// signed-16 PCM on WriteSamples, and finalises on Stop. Dispose == Stop.
/// </summary>
public interface IAudioRecorder : IDisposable
{
    int SampleRate { get; }   // e.g. 44100
    int Channels { get; }     // 1 = mono, 2 = stereo

    /// <summary>Append signed 16-bit PCM (stereo interleaved L,R,L,R...).</summary>
    void WriteSamples(ReadOnlySpan<short> samples);

    /// <summary>Finalise the recording (patch header sizes, flush).</summary>
    void Stop();
}
```

### `IVideoCaptureSink`

**Assembly:** `ViceSharp.Core.Media`

A continuous video sink the emulation worker tees each committed BGRA frame into.
Implemented by `FrameSequenceCapture` (numbered BMPs) and `FfmpegVideoRecorder`
(muxed container), so the runtime session drives either through one surface.

```csharp
public interface IVideoCaptureSink : IDisposable
{
    int FrameCount { get; }

    /// <summary>Persist one BGRA8888 frame (length == width*height*4).</summary>
    void CaptureFrame(ReadOnlySpan<byte> bgra, int width, int height);
}
```

### Concrete recorders (`ViceSharp.Core.Media` / `ViceSharp.Core.Capture`)

| Type | Role |
|------|------|
| `FrameCapture` | One-shot screenshot encode (`CaptureBgraAsync`, png/bmp). |
| `FrameSequenceCapture` | Numbered 24-bit BMP sequence; `FrameSequenceMode.AllFrames` or `UniqueFrames` (skips consecutive byte-identical frames). |
| `WavAudioRecorder` | RIFF/WAVE 16-bit PCM recorder. |
| `CaptureAudioTap` | `IAudioBackend` installed in the SID -> output path with a runtime-swappable `IAudioRecorder` slot; transparent pass-through when idle. |
| `FfmpegVideoRecorder` | Muxed video+audio via an external `ffmpeg` process. Implements both `IVideoCaptureSink` and `IAudioRecorder`; streams raw BGRA + s16le over two loopback TCP sockets (mirrors VICE `ffmpegexedrv`). |
| `FfmpegLocator` / `FfmpegVideoFormats` | Locate `ffmpeg` (PATH / `VICESHARP_FFMPEG`); the mp4 / mkv / avi container table. |

### gRPC `CaptureService` (control surface)

**Assembly:** `ViceSharp.Protocol` (contracts), `ViceSharp.Host` (`CaptureServiceHost`)

The client-facing API (`ICaptureService` / `IHostProtocolClient`):

```csharp
// Discover what this host can encode (screenshot/audio formats + ffmpeg video formats).
GetCaptureCapabilitiesAsync(SessionRequest) -> { ScreenshotFormats, AudioFormats, VideoFormats }

// One-shot screenshot (format: "png" | "bmp").
CaptureFrameAsync(CaptureFrameRequest{ SessionId, FilePath, Format })

// Start/stop a recording. Kind = Screenshot | Video | Audio.
//   Video  Format: "bmpseq" (Options { "frames": "all" | "unique" }), or "mp4"/"mkv"/"avi" (ffmpeg)
//   Audio  Format: "wav"
StartCaptureAsync(StartCaptureRequest{ SessionId, Kind, TargetPath, Format, Options })
StopCaptureAsync(StopCaptureRequest{ SessionId, CaptureId })

// Enumerate active captures for a session.
ListCapturesAsync(SessionRequest) -> CaptureSessionDto[]
```

`GetCaptureCapabilities` advertises the ffmpeg containers (each `CaptureVideoFormatDto.RequiresFfmpeg = true`) only when an ffmpeg binary is located; muxed video and WAV sound additionally require a live audio device.

---

## Monitor Interfaces

### `IMonitor`

**Assembly:** `ViceSharp.Abstractions`

The monitor/debugger contract. Provides command execution, register access, and side-effect-free disassembly. The full monitor engine (command parsing, breakpoints, tick history) lives in `ViceSharp.Monitor` and `ViceSharp.Host`, exposed remotely through the gRPC `MonitorService`.

```csharp
public interface IMonitor
{
    /// <summary>Executes a monitor command and returns the result.</summary>
    string ExecuteCommand(string command);

    /// <summary>Gets the current CPU register state.</summary>
    RegisterSnapshot GetRegisters();

    /// <summary>Disassembles instructions without mutating machine state.</summary>
    IReadOnlyList<DisassemblyEntry> Disassemble(ushort address, int count);

    /// <summary>True if execution is currently paused at a breakpoint.</summary>
    bool IsPaused { get; }
}
```

---

## State Interfaces

These interfaces manage emulation state: snapshots, mutation tracking, and inter-device messaging.

### `ISnapshot`

**Assembly:** `ViceSharp.Abstractions`

Captures the complete machine state in a serializable form. Snapshots are byte-comparable for determinism testing.

```csharp
public interface ISnapshot
{
    /// <summary>Cycle number when this snapshot was taken.</summary>
    ulong Cycle { get; }

    /// <summary>Serialize snapshot to a byte span.</summary>
    void Serialize(Span<byte> destination);

    /// <summary>Deserialize snapshot from a byte span.</summary>
    void Deserialize(ReadOnlySpan<byte> source);

    /// <summary>Get required buffer size for serialization.</summary>
    int GetSerializedSize();
}
```

### `ISnapshotStore`

**Assembly:** `ViceSharp.Abstractions`

Captures and restores machine state. Persistence to disk and any history retention are host concerns layered above this contract (see `SnapshotService` in the gRPC surface).

```csharp
public interface ISnapshotStore
{
    /// <summary>Captures a snapshot of the current machine state.</summary>
    ISnapshot Capture(IMachine machine);

    /// <summary>Restores machine state from a snapshot.</summary>
    void Restore(IMachine machine, ISnapshot snapshot);
}
```

### `IMutationQueue`

**Assembly:** `ViceSharp.Abstractions`

Records all state changes as small mutation structs. Double-buffered: the emulation thread writes to the active buffer while consumers read the committed buffer.

```csharp
public interface IMutationQueue
{
    /// <summary>Record a state mutation.</summary>
    void Enqueue(DeviceId source, ushort address, byte oldValue, byte newValue, ulong cycle);

    /// <summary>Commit current buffer and swap to next buffer.</summary>
    void Commit();

    /// <summary>Reset the queue to empty state.</summary>
    void Clear();
}
```

### `MutationEntry`

**Assembly:** `ViceSharp.Abstractions`

A single state change record. Captures the source device, target address, old and new values, and the cycle timestamp for replay and auditing.

```csharp
public readonly struct MutationEntry
{
    public DeviceId Source { get; }
    public ushort Address { get; }
    public byte OldValue { get; }
    public byte NewValue { get; }
    public ulong Cycle { get; }
}
```

### `IPubSub`

**Assembly:** `ViceSharp.Abstractions`

Lock-free topic-based publish/subscribe for inter-device communication. Designed for zero-allocation hot-path operation. Alongside the generic typed surface shown below, `IPubSub` offers packed-payload overloads (`Publish(Topic, MessageKind, PubSubPayload)`, `Subscribe(Topic, Action<PubSubMessage>)`) and raw-span compatibility overloads keyed by `TopicId`.

```csharp
public interface IPubSub
{
    /// <summary>Publishes an unmanaged payload to all subscribers of the given topic.</summary>
    void Publish<T>(Topic topic, T payload) where T : unmanaged;

    /// <summary>Subscribes a strongly typed handler to the given topic.</summary>
    SubscriptionHandle Subscribe<T>(Topic topic, Action<T> handler) where T : unmanaged;

    /// <summary>Removes a subscription by its opaque handle.</summary>
    void Unsubscribe(SubscriptionHandle handle);

    /// <summary>Delivers pending messages. Synchronous implementations may complete this as a no-op.</summary>
    void Flush();

    /// <summary>Resets frame-scoped message state.</summary>
    void FrameReset();

    /// <summary>Number of active subscriptions.</summary>
    int SubscriptionCount { get; }
}
```

### `IMessagePool`

**Assembly:** `ViceSharp.Abstractions`

Pre-allocates a fixed number of message slots to avoid per-message heap allocation. Slots are rented for the duration of a message's lifetime and returned via reference counting or frame reset.

```csharp
public interface IMessagePool
{
    /// <summary>Rents a message slot from the pool.</summary>
    MessageHandle Rent();

    /// <summary>Returns a message slot to the pool.</summary>
    void Return(MessageHandle handle);

    /// <summary>Total number of slots in the pool.</summary>
    int Capacity { get; }

    /// <summary>Number of slots currently rented.</summary>
    int ActiveCount { get; }

    /// <summary>Returns all slots to the pool.</summary>
    void Reset();
}
```

---

## Value Types

The following value types are defined in `ViceSharp.Abstractions`. All are structs or records for stack allocation (except the `ValidationReport` and `BusSnapshot` diagnostic classes).

| Type | Defined in | Description |
|------|------------|-------------|
| `DeviceId` | `IDevice.cs` | Strongly typed device identifier wrapping a `uint` |
| `Topic` | `IPubSub.cs` | Pub/sub topic: FNV-1a `uint` key plus optional interned name |
| `TopicId` | `IPubSub.cs` | Compatibility numeric topic identifier (implicit conversions to/from `Topic`) |
| `SubscriptionHandle` | `IPubSub.cs` | Opaque unsubscribe handle (slot index, generation, topic) |
| `MessageHandle` | `IPubSub.cs` | Reference-counted handle into `IMessagePool` (owner, slot, generation, topic, kind, payload length, sequence) |
| `PubSubPayload` | `IPubSub.cs` | Fixed 64-byte inline payload union |
| `PubSubMessage` | `IPubSub.cs` | Discriminated message: topic, `MessageKind`, packed payload |
| `MutationEntry` | `IMutationQueue.cs` | Single state-change record (source, address, old/new value, cycle) |
| `RegisterSnapshot` | `IMonitor.cs` | CPU register state (A, X, Y, S, P, PC) |
| `DisassemblyEntry` | `IMonitor.cs` | Single disassembled instruction (address, bytes, text, length, next address) |
| `DeviceDescriptor` | `IArchitectureDescriptor.cs` | Device name, id, role, base address, and size within an architecture |
| `CpuInfo` / `CpuRateReading` | `IMachine.cs` | Per-CPU cycle counts and effective-rate readings for the status surface |
| `ChipStateField` | `IStatefulDevice.cs` | One named field of a chip's staged state |
| `StateDiff` | `StateDiff.cs` | Managed-vs-native state comparison result (cycle, expected/actual `MachineState`) |
| `ValidationReport` | `ValidationReport.cs` | Architecture validation results (class) |
| `MachineState`, `NativeCpuPipelineState`, `NativeVicState`, `NativeCiaState` | `IViceNative.cs` | Lockstep oracle state structs shared with the native VICE shim |
| `MemoryWriteEvent`, `RasterLineEvent`, `WarpModeEvent`, `CpuInstructionCompletedEvent`, `CpuControlTransferEvent` | event records | Pub/sub event payloads |
| `BusSnapshot`, `BusLineSnapshot`, `BusEdgeEventArgs` | `IInterSystemBus.cs` | Inter-system bus diagnostics |

---

## Enumerations

| Enum | Defined in | Values |
|------|------------|--------|
| `ClockPhase` | `IClock.cs` | `Phi1`, `Phi2` |
| `InterruptType` | `IInterruptLine.cs` | `Irq`, `Nmi`, `Reset` |
| `DeviceRole` | `IDeviceRegistry.cs` | `SystemCore`, `Cpu`, `VideoChip`, `AudioChip`, `Cia1`, `Cia2`, `Pla`, `SystemRam`, `KernalRom`, `BasicRom`, `ChargenRom`, `CartridgePort`, `DriveCpu`, `DriveVia`, `DriveRam`, `DriveRom`, `DriveDisk` |
| `VideoStandard` | `IArchitectureDescriptor.cs` | `Pal`, `Ntsc` |
| `MessageKind` | `IPubSub.cs` | `Unknown`, `Raw`, `Typed`, `Irq`, `Nmi`, `BusAvailable`, `AddressEnableControl`, `Dma`, `Clock`, `State` |
| `CpuFlags` | `ICpu.cs` | `Carry`, `Zero`, `InterruptDisable`, `Decimal`, `Break`, `Unused`, `Overflow`, `Negative` (flags) |
| `Fidelity` | `Fidelity.cs` | `Buffered` (lightweight in-host emulation), `TrueDevice` (full standalone-machine emulation) |
| `CartridgeMappingMode` | `ICartridgePort.cs` | `Auto`, `Standard8K`, `Standard16K`, `Ultimax`, `GameSystem`, `MagicDesk`, `Ocean`, `FinalCartridgeIII`, `ActionReplay`, `EasyFlash`, `SuperSnapshotV5`, `RRNet` |
