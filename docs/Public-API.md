# ViceSharp Public API Reference

This document describes the 33+ public interfaces defined in `ViceSharp.Abstractions`. All interfaces are designed for NativeAOT compatibility: no reflection on the hot path, no base classes, and all members are explicitly defined for source-generator discovery.

---

## Core Interfaces

These interfaces define the fundamental emulation primitives: bus access, clock-driven execution, and interrupt signaling.

### `IBus`

**Assembly:** `ViceSharp.Abstractions`

Provides a flat address space with read/write access, representing the system data bus. All memory-mapped I/O, RAM, and ROM access flows through this interface.

```csharp
/// <summary>
/// Represents the system data bus providing unified address-space access.
/// All memory reads and writes are dispatched through the bus to the
/// appropriate IAddressSpace handler based on the current banking configuration.
/// </summary>
public interface IBus
{
    /// <summary>Reads a byte from the specified 16-bit address.</summary>
    byte Read(ushort address);

    /// <summary>Writes a byte to the specified 16-bit address.</summary>
    void Write(ushort address, byte value);

    /// <summary>Reads a contiguous block of memory into the target span.</summary>
    void ReadBlock(ushort startAddress, Span<byte> target);

    /// <summary>Writes a contiguous block of memory from the source span.</summary>
    void WriteBlock(ushort startAddress, ReadOnlySpan<byte> source);

    /// <summary>Registers an address space handler for a given address range.</summary>
    void MapAddressSpace(IAddressSpace space, ushort start, ushort end);

    /// <summary>Removes an address space mapping, reverting to the underlying default.</summary>
    void UnmapAddressSpace(ushort start, ushort end);

    /// <summary>Gets the current banking configuration identifier.</summary>
    int BankConfiguration { get; }
}
```

### `IClockedDevice`

**Assembly:** `ViceSharp.Abstractions`

Represents any device that receives ticks from the system clock. The clock subsystem calls `Tick()` at the device's configured rate, which may differ from the master clock frequency via a divisor.

```csharp
/// <summary>
/// A device driven by the system clock. Receives periodic tick calls
/// at a rate determined by the master clock frequency and the device's
/// clock divisor.
/// </summary>
public interface IClockedDevice : IDevice
{
    /// <summary>Advances the device by one clock cycle.</summary>
    void Tick();

    /// <summary>Clock divisor relative to master clock. 1 = same as master.</summary>
    int ClockDivisor { get; }

    /// <summary>The clock phase this device operates on (phi1 or phi2).</summary>
    ClockPhase Phase { get; }
}
```

### `IInterruptLine`

**Assembly:** `ViceSharp.Abstractions`

Models a physical interrupt line (IRQ or NMI). Multiple sources can assert the same line; the line remains active as long as any source holds it asserted (active-low, open-drain behavior matching real hardware).

```csharp
/// <summary>
/// Represents a physical interrupt line (IRQ or NMI) with open-drain
/// semantics. The line is active-low: it is asserted when any source
/// pulls it low, and deasserted only when all sources release.
/// </summary>
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
/// <summary>
/// Identifies a device that can raise interrupts. Used by IInterruptLine
/// to track assertion state per source.
/// </summary>
public interface IInterruptSource
{
    /// <summary>Unique identifier for this interrupt source.</summary>
    DeviceId SourceId { get; }

    /// <summary>The interrupt line(s) this source is connected to.</summary>
    IReadOnlyList<IInterruptLine> ConnectedLines { get; }
}
```

### `IAddressSpace`

**Assembly:** `ViceSharp.Abstractions`

Implemented by devices that occupy a region of the address bus. The bus routes reads and writes to the correct `IAddressSpace` implementor based on the current memory map and banking state.

```csharp
/// <summary>
/// A device that maps onto a contiguous region of the system address bus.
/// The bus routes memory operations to the appropriate IAddressSpace
/// based on the architecture's banking configuration.
/// </summary>
public interface IAddressSpace
{
    /// <summary>Reads a byte at the given offset within this address space.</summary>
    byte Read(ushort offset);

    /// <summary>Writes a byte at the given offset within this address space.</summary>
    void Write(ushort offset, byte value);

    /// <summary>Base address of this space in the system memory map.</summary>
    ushort BaseAddress { get; }

    /// <summary>Size of this address space in bytes.</summary>
    ushort Size { get; }

    /// <summary>True if this space is read-only (e.g., ROM).</summary>
    bool IsReadOnly { get; }
}
```

### `IClock`

**Assembly:** `ViceSharp.Abstractions`

The master system clock. Drives all cycle-accurate timing. Manages registration of clocked devices and distributes ticks according to each device's divisor and phase configuration.

```csharp
/// <summary>
/// Master system clock that drives cycle-accurate emulation.
/// Distributes ticks to all registered IClockedDevice instances
/// respecting divisors and clock phases.
/// </summary>
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
}
```

---

## System Interfaces

These interfaces define the machine abstraction: how devices are organized into a running system.

### `ISystem`

**Assembly:** `ViceSharp.Abstractions`

Top-level container for a running emulator instance. Holds the machine, manages lifecycle (start, stop, reset), and provides access to system-wide services.

```csharp
/// <summary>
/// Top-level container for a running emulator instance. Manages machine
/// lifecycle and provides access to system-wide services like ROM loading,
/// media capture, and the monitor/debugger.
/// </summary>
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

    /// <summary>Access to the system's ROM provider.</summary>
    IRomProvider RomProvider { get; }

    /// <summary>Access to the monitor/debugger.</summary>
    IMonitor Monitor { get; }
}
```

### `IMachine`

**Assembly:** `ViceSharp.Abstractions`

Represents a specific hardware configuration (e.g., C64 PAL, VIC-20 NTSC). Assembled from an `IArchitectureDescriptor` by the `IArchitectureBuilder`. Owns the bus, clock, and device registry.

```csharp
/// <summary>
/// A fully-assembled emulated machine. Created by IArchitectureBuilder
/// from an IArchitectureDescriptor. Owns the bus, clock, and all
/// registered devices.
/// </summary>
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
}
```

### `IDevice`

**Assembly:** `ViceSharp.Abstractions`

Base interface for all emulated hardware components. Every chip, memory bank, and peripheral implements `IDevice`. Provides identification and lifecycle management.

```csharp
/// <summary>
/// Base interface for all emulated hardware components. Provides
/// identification, initialization, and reset capabilities.
/// </summary>
public interface IDevice
{
    /// <summary>Unique identifier for this device instance.</summary>
    DeviceId Id { get; }

    /// <summary>Human-readable device name (e.g., "MOS 6510 CPU").</summary>
    string Name { get; }

    /// <summary>Initializes the device to its power-on state.</summary>
    void Initialize();

    /// <summary>Resets the device (equivalent to hardware reset line).</summary>
    void Reset();
}
```

### `IPeripheral`

**Assembly:** `ViceSharp.Abstractions`

Represents an external peripheral device such as a disk drive, datasette, or cartridge. Peripherals can be connected and disconnected at runtime and interact with the machine through defined ports.

```csharp
/// <summary>
/// An external peripheral that can be attached/detached at runtime.
/// Examples: 1541 disk drive, datasette, cartridges, REU.
/// </summary>
public interface IPeripheral : IDevice
{
    /// <summary>Connects the peripheral to the machine.</summary>
    void Attach(IMachine machine);

    /// <summary>Disconnects the peripheral from the machine.</summary>
    void Detach();

    /// <summary>True if the peripheral is currently attached.</summary>
    bool IsAttached { get; }

    /// <summary>The type of port this peripheral connects to.</summary>
    PeripheralPort Port { get; }
}
```

### `IDeviceRegistry`

**Assembly:** `ViceSharp.Abstractions`

Registry of all devices within a machine. Populated by the architecture builder during machine construction. Supports lookup by ID, type, and role.

```csharp
/// <summary>
/// Registry of all devices in a machine. Supports lookup by DeviceId,
/// interface type, or device role. Populated during machine construction.
/// </summary>
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

### `IArchitecture`

**Assembly:** `ViceSharp.Abstractions`

Represents a fully-wired machine architecture at runtime. Provides access to the memory map, interrupt routing, and clock configuration as assembled from a descriptor.

```csharp
/// <summary>
/// A fully-wired runtime architecture. Provides the assembled memory map,
/// interrupt routing tables, and clock configuration.
/// </summary>
public interface IArchitecture
{
    /// <summary>The descriptor this architecture was built from.</summary>
    IArchitectureDescriptor Descriptor { get; }

    /// <summary>The memory map with all address space assignments.</summary>
    IReadOnlyList<AddressMapping> MemoryMap { get; }

    /// <summary>All interrupt line connections.</summary>
    IReadOnlyList<InterruptRoute> InterruptRoutes { get; }

    /// <summary>Clock configuration including divisors and phases.</summary>
    ClockConfiguration ClockConfig { get; }
}
```

### `IArchitectureDescriptor`

**Assembly:** `ViceSharp.Abstractions`

Declarative description of a machine architecture. Lists the devices, memory map, clock speeds, and interrupt wiring needed to construct a running machine. Descriptors are immutable data.

```csharp
/// <summary>
/// Immutable declarative description of a machine architecture.
/// Lists devices, memory map, clock configuration, and interrupt wiring.
/// Used by IArchitectureBuilder to construct a running IMachine.
/// </summary>
public interface IArchitectureDescriptor
{
    /// <summary>Machine name (e.g., "Commodore 64 PAL").</summary>
    string MachineName { get; }

    /// <summary>Devices required by this architecture.</summary>
    IReadOnlyList<DeviceDescriptor> Devices { get; }

    /// <summary>Address space mappings.</summary>
    IReadOnlyList<AddressMapEntry> AddressMap { get; }

    /// <summary>Master clock frequency in Hz.</summary>
    long MasterClockHz { get; }

    /// <summary>Video standard (PAL or NTSC).</summary>
    VideoStandard VideoStandard { get; }

    /// <summary>Required ROM set for this architecture.</summary>
    IRomSet RequiredRoms { get; }
}
```

### `IArchitectureBuilder`

**Assembly:** `ViceSharp.Abstractions`

Constructs a running `IMachine` from an `IArchitectureDescriptor`. Performs the wiring sequence: instantiate devices, map address spaces, connect interrupts, configure clocks, and validate.

```csharp
/// <summary>
/// Constructs a running IMachine from an IArchitectureDescriptor.
/// Instantiates devices, wires address spaces, connects interrupts,
/// configures clocks, and runs validation.
/// </summary>
public interface IArchitectureBuilder
{
    /// <summary>Builds a machine from the given architecture descriptor.</summary>
    IMachine Build(IArchitectureDescriptor descriptor);

    /// <summary>Builds a machine with custom service overrides.</summary>
    IMachine Build(IArchitectureDescriptor descriptor, Action<IServiceOverrides> configure);

    /// <summary>Validates a descriptor without constructing a machine.</summary>
    ValidationResult Validate(IArchitectureDescriptor descriptor);
}
```

### `IArchitectureValidator`

**Assembly:** `ViceSharp.Abstractions`

Validates an architecture descriptor for correctness before construction. Catches configuration errors such as overlapping address ranges, missing required devices, and invalid clock divisors.

```csharp
/// <summary>
/// Validates an IArchitectureDescriptor for correctness. Catches errors
/// like overlapping address ranges, missing required devices, invalid
/// clock divisors, and disconnected interrupt sources.
/// </summary>
public interface IArchitectureValidator
{
    /// <summary>Runs all validation rules against the descriptor.</summary>
    ValidationResult Validate(IArchitectureDescriptor descriptor);

    /// <summary>Checks for overlapping address space mappings.</summary>
    ValidationResult ValidateAddressMap(IReadOnlyList<AddressMapEntry> map);

    /// <summary>Checks that all required device roles are present.</summary>
    ValidationResult ValidateDeviceRoles(IReadOnlyList<DeviceDescriptor> devices);
}
```

---

## Services Interfaces

These interfaces define external services consumed by the emulation engine.

### `IRomProvider`

**Assembly:** `ViceSharp.Abstractions`

Loads and caches ROM images from the filesystem or other sources. Validates ROM integrity via checksums before use.

```csharp
/// <summary>
/// Loads and caches ROM images. Validates integrity via CRC32/SHA256
/// checksums before returning ROM data.
/// </summary>
public interface IRomProvider
{
    /// <summary>Loads a ROM by name for the specified architecture.</summary>
    ReadOnlyMemory<byte> LoadRom(string romName, string architecture);

    /// <summary>Checks whether a ROM is available and valid.</summary>
    bool IsAvailable(string romName, string architecture);

    /// <summary>Returns all available ROMs for the given architecture.</summary>
    IReadOnlyList<RomInfo> GetAvailableRoms(string architecture);

    /// <summary>Validates a ROM's checksum against known-good values.</summary>
    RomValidationResult Validate(string romName, string architecture);

    /// <summary>Base search path for ROM files.</summary>
    string RomBasePath { get; }
}
```

### `IRomSet`

**Assembly:** `ViceSharp.Abstractions`

Describes the complete set of ROMs required by a specific architecture (e.g., KERNAL, BASIC, CHARGEN for C64).

```csharp
/// <summary>
/// Describes the complete set of ROMs required by a specific architecture.
/// Each entry specifies the ROM name, expected size, and known-good checksums.
/// </summary>
public interface IRomSet
{
    /// <summary>Architecture this ROM set belongs to (e.g., "c64").</summary>
    string Architecture { get; }

    /// <summary>Individual ROM file requirements.</summary>
    IReadOnlyList<RomRequirement> Requirements { get; }

    /// <summary>Checks if all required ROMs are present and valid.</summary>
    bool IsComplete(IRomProvider provider);
}
```

### `IAudioBackend`

**Assembly:** `ViceSharp.Abstractions`

Platform-specific audio output. Receives sample data from the emulation engine and delivers it to the host audio system. Manages buffering, sample rate conversion, and latency.

```csharp
/// <summary>
/// Platform-specific audio output backend. Receives PCM samples from the
/// emulation engine's ring buffer and delivers them to the host audio system.
/// </summary>
public interface IAudioBackend
{
    /// <summary>Initializes the audio backend with the given parameters.</summary>
    void Initialize(AudioParameters parameters);

    /// <summary>Submits a buffer of PCM samples for playback.</summary>
    void SubmitSamples(ReadOnlySpan<float> samples);

    /// <summary>Number of samples currently queued for playback.</summary>
    int QueuedSampleCount { get; }

    /// <summary>Target latency in milliseconds.</summary>
    int TargetLatencyMs { get; set; }

    /// <summary>Actual measured latency in milliseconds.</summary>
    int ActualLatencyMs { get; }

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

Receives completed video frames from the emulation engine. The video chip writes into a back buffer; when a frame is complete, it is swapped to the front buffer and delivered to the sink.

```csharp
/// <summary>
/// Receives completed video frames from the emulation engine.
/// Frames are delivered as raw pixel data in a platform-neutral format.
/// The sink owns presentation timing (vsync, frame skip).
/// </summary>
public interface IFrameSink
{
    /// <summary>Called when a complete frame is ready for display.</summary>
    void PresentFrame(ReadOnlySpan<byte> pixelData, FrameMetadata metadata);

    /// <summary>Current display resolution (may change on mode switch).</summary>
    Resolution DisplayResolution { get; }

    /// <summary>Preferred pixel format for frame data.</summary>
    PixelFormat PreferredFormat { get; }

    /// <summary>True if the sink is ready to accept a new frame.</summary>
    bool IsReady { get; }

    /// <summary>Total frames presented since last reset.</summary>
    long FrameCount { get; }
}
```

### `IInputSource`

**Assembly:** `ViceSharp.Abstractions`

Abstracts host input devices (keyboard, gamepad, mouse) into emulated input events. Polls or receives events from the host and translates them to Commodore-compatible input.

```csharp
/// <summary>
/// Abstracts host input devices into emulated input events.
/// Translates keyboard, gamepad, and mouse input from the host platform
/// into Commodore-compatible input events.
/// </summary>
public interface IInputSource
{
    /// <summary>Polls for new input events since the last call.</summary>
    void Poll();

    /// <summary>Returns all pending input events.</summary>
    IReadOnlyList<IInputEvent> GetPendingEvents();

    /// <summary>The type of input device this source represents.</summary>
    InputDeviceType DeviceType { get; }

    /// <summary>True if this input source is currently connected/active.</summary>
    bool IsConnected { get; }
}
```

### `IInputEvent`

**Assembly:** `ViceSharp.Abstractions`

A single input event produced by an `IInputSource`. Carries the event type, key/button code, and timestamp for accurate input timing.

```csharp
/// <summary>
/// A single input event carrying type, code, and timestamp.
/// Used for keyboard presses, joystick directions, mouse movements,
/// and other host input translated to emulated device signals.
/// </summary>
public interface IInputEvent
{
    /// <summary>Type of input event (key down, key up, axis, button).</summary>
    InputEventType EventType { get; }

    /// <summary>Key or button code.</summary>
    int Code { get; }

    /// <summary>Analog value for axis events, 0/1 for digital.</summary>
    float Value { get; }

    /// <summary>Timestamp in master clock cycles when this event was received.</summary>
    long Timestamp { get; }
}
```

---

## Media Interfaces

These interfaces support capturing emulator output for screenshots, video, and audio recording.

### `IScreenshotCapture`

**Assembly:** `ViceSharp.Abstractions`

Captures a single video frame as a still image (PNG or BMP). Operates on the committed frame buffer, never blocking the emulation thread.

```csharp
/// <summary>
/// Captures a single frame as a still image from the current IFrameSink.
/// Operates on the committed (front) frame buffer.
/// </summary>
public interface IScreenshotCapture
{
    /// <summary>Captures the current frame to the specified file path.</summary>
    Task CaptureAsync(string filePath, ImageFormat format);

    /// <summary>Captures the current frame to a byte buffer.</summary>
    Task<ReadOnlyMemory<byte>> CaptureToMemoryAsync(ImageFormat format);
}
```

### `IVideoRecorder`

**Assembly:** `ViceSharp.Abstractions`

Records a continuous stream of video frames to a container format (MP4 via FFmpeg). Designed for NativeAOT compatibility using P/Invoke rather than managed wrappers.

```csharp
/// <summary>
/// Records continuous video output to a container format.
/// Uses FFmpeg via P/Invoke for NativeAOT compatibility.
/// </summary>
public interface IVideoRecorder
{
    /// <summary>Begins recording to the specified output path.</summary>
    void Start(string outputPath, VideoRecordingOptions options);

    /// <summary>Submits a frame for encoding.</summary>
    void SubmitFrame(ReadOnlySpan<byte> pixelData, FrameMetadata metadata);

    /// <summary>Stops recording and finalizes the output file.</summary>
    Task StopAsync();

    /// <summary>True if currently recording.</summary>
    bool IsRecording { get; }

    /// <summary>Total frames recorded in the current session.</summary>
    long RecordedFrameCount { get; }
}
```

### `IAudioRecorder`

**Assembly:** `ViceSharp.Abstractions`

Records the audio output stream to WAV or FLAC format. Taps the same sample stream that feeds `IAudioBackend`.

```csharp
/// <summary>
/// Records the audio output stream to WAV or FLAC.
/// Taps the sample stream feeding IAudioBackend without affecting playback.
/// </summary>
public interface IAudioRecorder
{
    /// <summary>Begins recording to the specified output path.</summary>
    void Start(string outputPath, AudioRecordingOptions options);

    /// <summary>Submits a buffer of samples for recording.</summary>
    void SubmitSamples(ReadOnlySpan<float> samples);

    /// <summary>Stops recording and finalizes the output file.</summary>
    Task StopAsync();

    /// <summary>True if currently recording.</summary>
    bool IsRecording { get; }

    /// <summary>Duration of recorded audio in milliseconds.</summary>
    long RecordedDurationMs { get; }
}
```

### `IMediaCaptureSession`

**Assembly:** `ViceSharp.Abstractions`

Coordinates synchronized audio and video recording. Ensures A/V streams start and stop together with aligned timestamps.

```csharp
/// <summary>
/// Coordinates synchronized audio and video recording.
/// Ensures both streams use aligned timestamps for correct A/V sync.
/// </summary>
public interface IMediaCaptureSession
{
    /// <summary>Begins a coordinated A/V recording session.</summary>
    void Start(string basePath, MediaCaptureOptions options);

    /// <summary>Stops all active recordings and finalizes output files.</summary>
    Task StopAsync();

    /// <summary>Takes a screenshot without interrupting an active recording.</summary>
    Task CaptureScreenshotAsync(string filePath, ImageFormat format);

    /// <summary>Access to the underlying video recorder.</summary>
    IVideoRecorder VideoRecorder { get; }

    /// <summary>Access to the underlying audio recorder.</summary>
    IAudioRecorder AudioRecorder { get; }

    /// <summary>True if any recording is currently active.</summary>
    bool IsActive { get; }
}
```

---

## Monitor Interfaces

These interfaces define the debugger/monitor subsystem for interactive debugging and inspection.

### `IMonitor`

**Assembly:** `ViceSharp.Abstractions`

The monitor/debugger engine. Provides breakpoints, memory inspection, disassembly, register access, and single-step execution.

```csharp
/// <summary>
/// Debugger/monitor engine providing breakpoints, disassembly,
/// memory inspection, register manipulation, and execution control.
/// </summary>
public interface IMonitor
{
    /// <summary>Executes a monitor command and returns the result.</summary>
    MonitorResult Execute(IMonitorCommand command);

    /// <summary>Sets a breakpoint at the specified address.</summary>
    BreakpointHandle SetBreakpoint(ushort address, BreakpointType type);

    /// <summary>Removes a previously set breakpoint.</summary>
    void RemoveBreakpoint(BreakpointHandle handle);

    /// <summary>Disassembles instructions starting at the given address.</summary>
    IReadOnlyList<DisassemblyLine> Disassemble(ushort address, int count);

    /// <summary>Gets the current CPU register state.</summary>
    RegisterSnapshot GetRegisters();

    /// <summary>True if execution is currently paused at a breakpoint.</summary>
    bool IsPaused { get; }

    /// <summary>Event stream for monitor events (breakpoint hit, step complete).</summary>
    IMonitorEventStream Events { get; }
}
```

### `IMonitorCommand`

**Assembly:** `ViceSharp.Abstractions`

Represents a parsed monitor command ready for execution. Commands are parsed from text input by a command parser and executed by the `IMonitor`.

```csharp
/// <summary>
/// A parsed monitor command ready for execution by IMonitor.
/// Carries the command verb, target, and any arguments.
/// </summary>
public interface IMonitorCommand
{
    /// <summary>Command verb (e.g., "break", "disassemble", "mem").</summary>
    string Verb { get; }

    /// <summary>Command arguments as parsed tokens.</summary>
    IReadOnlyList<string> Arguments { get; }

    /// <summary>Raw text representation of the command.</summary>
    string RawText { get; }
}
```

### `IMonitorTransport`

**Assembly:** `ViceSharp.Abstractions`

Transport layer for monitor I/O. Supports multiple frontends: interactive console, TCP socket (for remote debugging), and programmatic in-process access.

```csharp
/// <summary>
/// Transport layer for monitor I/O. Supports console, TCP socket,
/// and in-process programmatic access.
/// </summary>
public interface IMonitorTransport
{
    /// <summary>Reads the next command from the transport.</summary>
    Task<IMonitorCommand> ReadCommandAsync(CancellationToken ct);

    /// <summary>Writes a response back through the transport.</summary>
    Task WriteResponseAsync(MonitorResult result, CancellationToken ct);

    /// <summary>True if the transport connection is active.</summary>
    bool IsConnected { get; }

    /// <summary>Transport type identifier (console, tcp, in-process).</summary>
    string TransportType { get; }
}
```

### `IMonitorEventStream`

**Assembly:** `ViceSharp.Abstractions`

Async event stream for monitor notifications. Consumers subscribe to events such as breakpoint hits, single-step completions, and register changes.

```csharp
/// <summary>
/// Async event stream for monitor notifications including breakpoint hits,
/// step completions, and state changes.
/// </summary>
public interface IMonitorEventStream
{
    /// <summary>Subscribes to all monitor events.</summary>
    IDisposable Subscribe(Action<MonitorEvent> handler);

    /// <summary>Asynchronously yields monitor events as they occur.</summary>
    IAsyncEnumerable<MonitorEvent> ReadAllAsync(CancellationToken ct);

    /// <summary>Total events emitted since the stream was created.</summary>
    long EventCount { get; }
}
```

---

## State Interfaces

These interfaces manage emulation state: snapshots, mutation tracking, and inter-device messaging.

### `ISnapshot`

**Assembly:** `ViceSharp.Abstractions`

Captures the complete machine state as a flat byte array. Snapshots are serializable, comparable (byte-exact for determinism testing), and diffable for incremental saves.

```csharp
/// <summary>
/// Complete machine state captured as a flat byte array.
/// Serializable, byte-comparable, and diffable for incremental saves.
/// </summary>
public interface ISnapshot
{
    /// <summary>The raw state data.</summary>
    ReadOnlyMemory<byte> Data { get; }

    /// <summary>Architecture identifier this snapshot was taken from.</summary>
    string Architecture { get; }

    /// <summary>Master clock cycle at the time of capture.</summary>
    long CycleStamp { get; }

    /// <summary>Frame number at the time of capture.</summary>
    long FrameNumber { get; }

    /// <summary>Computes a byte-level diff against another snapshot.</summary>
    SnapshotDiff DiffAgainst(ISnapshot other);

    /// <summary>Size of the snapshot data in bytes.</summary>
    int SizeBytes { get; }
}
```

### `ISnapshotStore`

**Assembly:** `ViceSharp.Abstractions`

Manages snapshot persistence: saving to disk, loading, and maintaining a history window of recent snapshots in memory.

```csharp
/// <summary>
/// Manages snapshot persistence and the in-memory history window.
/// Supports save/load to disk and ring-buffer retention of recent snapshots.
/// </summary>
public interface ISnapshotStore
{
    /// <summary>Captures a snapshot of the current machine state.</summary>
    ISnapshot Capture(IMachine machine);

    /// <summary>Restores machine state from a snapshot.</summary>
    void Restore(IMachine machine, ISnapshot snapshot);

    /// <summary>Saves a snapshot to the specified file path.</summary>
    Task SaveAsync(ISnapshot snapshot, string filePath);

    /// <summary>Loads a snapshot from the specified file path.</summary>
    Task<ISnapshot> LoadAsync(string filePath);

    /// <summary>Recent snapshots held in the history window.</summary>
    IReadOnlyList<ISnapshot> History { get; }

    /// <summary>Current StateWindow configuration.</summary>
    StateWindowConfig WindowConfig { get; set; }
}
```

### `IMutationQueue`

**Assembly:** `ViceSharp.Abstractions`

Records all state changes as small mutation structs. Double-buffered: the emulation thread writes to the active buffer while consumers read the committed buffer.

```csharp
/// <summary>
/// Records all state changes as mutation structs. Double-buffered so
/// the emulation thread writes to the active buffer while consumers
/// (UI, debugger, recorder) read the committed buffer.
/// </summary>
public interface IMutationQueue
{
    /// <summary>Enqueues a mutation (called from the emulation thread).</summary>
    void Enqueue(IMutation mutation);

    /// <summary>Swaps active and committed buffers at frame boundary.</summary>
    void Commit();

    /// <summary>Returns all mutations in the committed buffer.</summary>
    ReadOnlySpan<IMutation> GetCommitted();

    /// <summary>Number of mutations in the active (uncommitted) buffer.</summary>
    int PendingCount { get; }

    /// <summary>Number of mutations in the committed buffer.</summary>
    int CommittedCount { get; }

    /// <summary>Clears the committed buffer after consumers have processed it.</summary>
    void AcknowledgeCommitted();
}
```

### `IMutation`

**Assembly:** `ViceSharp.Abstractions`

A single state change record. Captures the source device, target location, old and new values, and the cycle timestamp for replay and auditing.

```csharp
/// <summary>
/// A single state change record for auditing, undo, and replay.
/// Captures source, target, old/new values, and cycle timestamp.
/// </summary>
public interface IMutation
{
    /// <summary>Device that caused this mutation.</summary>
    DeviceId Source { get; }

    /// <summary>Target address or field identifier.</summary>
    ushort TargetAddress { get; }

    /// <summary>Value before the mutation.</summary>
    byte OldValue { get; }

    /// <summary>Value after the mutation.</summary>
    byte NewValue { get; }

    /// <summary>Master clock cycle when this mutation occurred.</summary>
    long CycleStamp { get; }

    /// <summary>Category of mutation (memory write, register change, I/O).</summary>
    MutationType Type { get; }
}
```

### `IPubSub`

**Assembly:** `ViceSharp.Abstractions`

Lock-free topic-based publish/subscribe for inter-device communication. Designed for zero-allocation hot-path operation using pre-allocated message pools and arena-scoped payloads.

```csharp
/// <summary>
/// Lock-free topic-based publish/subscribe for inter-device communication.
/// Zero-allocation on the hot path using IMessagePool and PayloadArena.
/// </summary>
public interface IPubSub
{
    /// <summary>Publishes a message to the given topic.</summary>
    void Publish<T>(Topic topic, T payload) where T : unmanaged;

    /// <summary>Subscribes a handler to the given topic.</summary>
    SubscriptionHandle Subscribe<T>(Topic topic, Action<T> handler) where T : unmanaged;

    /// <summary>Removes a subscription.</summary>
    void Unsubscribe(SubscriptionHandle handle);

    /// <summary>Delivers all pending messages to their subscribers.</summary>
    void Flush();

    /// <summary>Resets the message pool and payload arena (call at frame boundary).</summary>
    void FrameReset();

    /// <summary>Number of active subscriptions.</summary>
    int SubscriptionCount { get; }
}
```

### `IMessagePool`

**Assembly:** `ViceSharp.Abstractions`

Pre-allocates a fixed number of message slots to avoid per-message heap allocation. Slots are rented for the duration of a message's lifetime and returned automatically when the reference count drops to zero or at frame reset.

```csharp
/// <summary>
/// Pre-allocated pool of message slots for zero-allocation pub/sub.
/// Slots are rented and returned via reference counting or frame reset.
/// </summary>
public interface IMessagePool
{
    /// <summary>Rents a message slot from the pool.</summary>
    MessageHandle Rent();

    /// <summary>Returns a message slot to the pool.</summary>
    void Return(MessageHandle handle);

    /// <summary>Total capacity of the pool.</summary>
    int Capacity { get; }

    /// <summary>Number of slots currently in use.</summary>
    int ActiveCount { get; }

    /// <summary>Resets the pool, returning all slots (called at frame boundary).</summary>
    void Reset();
}
```

---

## Value Types

The following value types are used throughout the interface surface. All are structs or records for stack allocation.

| Type | Assembly | Description |
|------|----------|-------------|
| `DeviceId` | `ViceSharp.Abstractions` | 32-bit unique device identifier |
| `Address` | `ViceSharp.Abstractions` | 16-bit address with bank qualifier |
| `ClockCycle` | `ViceSharp.Abstractions` | 64-bit master cycle counter |
| `MessageHandle` | `ViceSharp.Abstractions` | Reference-counted handle into `IMessagePool` |
| `Topic` | `ViceSharp.Abstractions` | Pub/sub topic identifier (interned string key) |
| `SubscriptionHandle` | `ViceSharp.Abstractions` | Opaque handle for unsubscribing from a topic |
| `BreakpointHandle` | `ViceSharp.Abstractions` | Opaque handle for removing a breakpoint |
| `FrameMetadata` | `ViceSharp.Abstractions` | Frame number, cycle stamp, and timing info |
| `Resolution` | `ViceSharp.Abstractions` | Width/height pair for display resolution |
| `AudioParameters` | `ViceSharp.Abstractions` | Sample rate, channels, buffer size |
| `StateWindowConfig` | `ViceSharp.Abstractions` | Snapshot interval, history depth, memory budget |
| `SnapshotDiff` | `ViceSharp.Abstractions` | Delta between two snapshots |
| `ValidationResult` | `ViceSharp.Abstractions` | Success/failure with diagnostic messages |
| `RegisterSnapshot` | `ViceSharp.Abstractions` | CPU register state at a point in time |
| `DisassemblyLine` | `ViceSharp.Abstractions` | Single disassembled instruction |
| `RomInfo` | `ViceSharp.Abstractions` | ROM metadata (name, size, checksum) |
| `RomRequirement` | `ViceSharp.Abstractions` | Expected ROM with known-good checksums |
| `RomValidationResult` | `ViceSharp.Abstractions` | ROM integrity check result |
| `AddressMapping` | `ViceSharp.Abstractions` | Runtime address-to-device mapping |
| `AddressMapEntry` | `ViceSharp.Abstractions` | Descriptor-level address range entry |
| `InterruptRoute` | `ViceSharp.Abstractions` | Source-to-line interrupt connection |
| `ClockConfiguration` | `ViceSharp.Abstractions` | Assembled clock tree with divisors |
| `DeviceDescriptor` | `ViceSharp.Abstractions` | Device type and role within an architecture |

---

## Enumerations

| Enum | Assembly | Values |
|------|----------|--------|
| `ClockPhase` | `ViceSharp.Abstractions` | `Phi1`, `Phi2` |
| `InterruptType` | `ViceSharp.Abstractions` | `IRQ`, `NMI`, `Reset` |
| `PeripheralPort` | `ViceSharp.Abstractions` | `CartridgePort`, `UserPort`, `SerialBus`, `CassettePort`, `JoystickPort1`, `JoystickPort2` |
| `DeviceRole` | `ViceSharp.Abstractions` | `CPU`, `VideoChip`, `AudioChip`, `Timer`, `Memory`, `PLA`, `IOController` |
| `VideoStandard` | `ViceSharp.Abstractions` | `PAL`, `NTSC` |
| `PixelFormat` | `ViceSharp.Abstractions` | `RGBA8888`, `BGRA8888`, `RGB565` |
| `InputDeviceType` | `ViceSharp.Abstractions` | `Keyboard`, `Joystick`, `Mouse`, `Lightpen`, `Paddle` |
| `InputEventType` | `ViceSharp.Abstractions` | `KeyDown`, `KeyUp`, `ButtonDown`, `ButtonUp`, `AxisMoved`, `MouseMoved` |
| `BreakpointType` | `ViceSharp.Abstractions` | `Execution`, `Read`, `Write`, `ReadWrite` |
| `MutationType` | `ViceSharp.Abstractions` | `MemoryWrite`, `RegisterChange`, `IOWrite`, `InterruptChange`, `BankSwitch` |
| `ImageFormat` | `ViceSharp.Abstractions` | `PNG`, `BMP` |
