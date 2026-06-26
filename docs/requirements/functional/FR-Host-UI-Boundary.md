# FR-Host-UI-Boundary: Host and UI Boundary Functional Requirements

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Subsystem      | Host / UI Boundary             |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-05-13 |

---

## Scope

These requirements define the observable behavior at the boundary between the emulator host and UI clients. The host owns emulator state, device services, persistence, and local render-source composition. Host UI control, media, session, input, state, capture, and diagnostic operations consume the host through the gRPC boundary defined by TR-GRPC-BOUNDARY-001.

The in-process Avalonia renderer is a narrow local rendering exception. A host-owned render surface may bind directly to a local emulator/frame source to avoid routing frame presentation through gRPC. ViewModels still consume gRPC-backed client abstractions only, and external or remote UIs use gRPC video service/stream APIs when direct in-process rendering is unavailable.

---

## FR-HOST-001: Host Process and Machine Session Control

**ID:** FR-HOST-001
**Title:** Host-Owned Emulator Session Lifecycle
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The emulator host shall own machine session creation, lifecycle transitions, and process-level fault reporting. UI clients request state changes through the host boundary instead of instantiating emulator core objects directly.

### Acceptance Criteria

1. A host process can create and destroy a C64 machine session without referencing any UI framework.
2. The host exposes start, pause, resume, reset, and stop commands for an active session.
3. Lifecycle state is reported as idle, running, paused, faulted, or stopped.
4. Lifecycle commands are serialized per session so transitions cannot race.
5. Each command response includes the session id, command sequence, resulting state, and structured error details when applicable.
6. Runtime counters for frame number, cycle count, speed percentage, and elapsed host time are queryable by the UI.

### Source References

- `native/vice/vice/doc/vice.texi`: invoking emulators, basic operation, settings/resources, reset, autostart, monitor, media, and diagnostics as observable host-control behavior.

### Traceability

- **Interfaces:** `IEmulator`, `IMachine`, `IArchitectureDescriptor`, `HostControlService`
- **Technical Requirement:** TR-GRPC-BOUNDARY-001
- **Test Suite:** `HostSessionLifecycleTests`, `GrpcHostControlTests`

---

## FR-HOST-002: Media Attachment Protocol

**ID:** FR-HOST-002
**Title:** Host-Mediated Disk, Tape, and Cartridge Attachment
**Priority:** P0 -- Critical
**Iteration:** 2

### Description

The host shall expose runtime attachment and ejection of disk, tape, and cartridge media through boundary commands. UI clients provide user intent and file references or byte payloads; the host validates formats and applies the changes at emulator-safe boundaries.

### Acceptance Criteria

1. Disk images can be mounted and ejected through the host boundary for the configured drive device.
2. TAP images can be mounted and ejected through the host boundary for the datasette device.
3. Standard cartridge images can be inserted and removed through the host boundary.
4. Media format validation failures are reported without mutating the current device state.
5. Attachment commands return stable media status including device id, media kind, display name, write-protection state, and current error state.
6. Attach and eject operations are applied at command boundaries that do not tear device state mid-cycle.

### Source References

- `native/vice/vice/doc/vice.texi`: invoking emulators, basic operation, settings/resources, reset, autostart, monitor, media, and diagnostics as observable host-control behavior.

### Traceability

- **Interfaces:** `IDiskDrive`, `ITapeUnit`, `ICartridgePort`, `HostMediaService`
- **Related FRs:** FR-DRV-001, FR-TAP-002, FR-CRT-001
- **Technical Requirement:** TR-GRPC-BOUNDARY-001
- **Test Suite:** `HostMediaAttachmentTests`, `GrpcMediaCommandTests`

---

## FR-HOST-003: Remote Video Frame Streaming Protocol

**ID:** FR-HOST-003
**Title:** Host-Streamed Video Frames for Remote UI Clients
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The host shall stream committed video frames to external or remote UI clients without allowing UI rendering to block the emulation thread. In-process Avalonia rendering may instead use a host-owned direct frame-source binding as described by FR-UI-001 and TR-GRPC-BOUNDARY-001.

### Acceptance Criteria

1. Video frames are streamed with frame number, cycle stamp, video standard, dimensions, pixel format, and skipped-frame metadata.
2. Video backpressure uses a latest-complete-frame policy; stale frames may be dropped without changing emulation state.
3. A newly connected remote UI client can request the latest committed frame before subscribing to the live stream.
4. Disconnecting a remote UI client does not pause, reset, or mutate the emulator session.
5. Frame payloads use a documented pixel format so non-Avalonia clients can render them.

### Source References

- `native/vice/vice/doc/vice.texi`: invoking emulators, basic operation, settings/resources, reset, autostart, monitor, media, and diagnostics as observable host-control behavior.

### Traceability

- **Interfaces:** `IFrameSink`, `HostOutputService`, `VideoService`
- **Technical Requirement:** TR-GRPC-BOUNDARY-001
- **Test Suite:** `HostOutputStreamTests`, `GrpcFrameStreamTests`

---

## FR-HOST-004: Input and Machine Control Protocol

**ID:** FR-HOST-004
**Title:** Host-Normalized Keyboard, Joystick, and Machine Control
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The host shall accept normalized input events and machine control commands from UI clients and inject or apply them at deterministic boundaries.

### Acceptance Criteria

1. Keyboard key-down, key-up, RESTORE, and SHIFT LOCK events can be submitted through the host boundary.
2. Joystick port 1 and port 2 direction/fire state can be submitted through the host boundary.
3. Start, pause, resume, reset, and shutdown controls are submitted through the host boundary.
4. Input events preserve client order through a monotonically increasing sequence number.
5. Unknown mappings, invalid port numbers, and stale sequence numbers are rejected with structured errors.
6. The same normalized input event stream can be recorded for deterministic replay.

### Source References

- `native/vice/vice/doc/vice.texi`: invoking emulators, basic operation, settings/resources, reset, autostart, monitor, media, and diagnostics as observable host-control behavior.

### Traceability

- **Interfaces:** `IKeyboardMatrix`, `IJoystickPort`, `IInputSource`, `HostInputService`, `HostControlService`
- **Related FRs:** FR-INP-001, FR-INP-002
- **Technical Requirement:** TR-GRPC-BOUNDARY-001
- **Test Suite:** `HostInputInjectionTests`, `GrpcInputOrderingTests`, `GrpcHostControlTests`

---

## FR-HOST-005: State, Capture, and Diagnostics Commands

**ID:** FR-HOST-005
**Title:** Host-Owned Snapshot, Screenshot, and Diagnostic Operations
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The host shall expose state save/load, screenshot capture, and basic diagnostic commands without granting UI clients direct access to emulator internals.

### Acceptance Criteria

1. Snapshot save requests produce a versioned artifact or artifact handle with checksum metadata.
2. Snapshot load requests validate the artifact before replacing the active machine state.
3. Screenshot requests capture the latest committed frame with dimensions, pixel format, and palette metadata.
4. State and capture commands coordinate with the emulation loop so snapshots and screenshots are not torn.
5. Long-running state operations report progress and can be cancelled before commit.
6. Diagnostic status includes active machine profile, lifecycle state, attached media, recent structured errors, and current frame/cycle counters.

### Source References

- `native/vice/vice/doc/vice.texi`: invoking emulators, basic operation, settings/resources, reset, autostart, monitor, media, and diagnostics as observable host-control behavior.

### Traceability

- **Interfaces:** `ISnapshotManager`, `IMediaCapture`, `IMonitor`, `HostStateService`
- **Related FRs:** FR-SNP-001, FR-SNP-002, FR-MED-001
- **Technical Requirement:** TR-GRPC-BOUNDARY-001
- **Test Suite:** `HostStateCommandTests`, `GrpcSnapshotTests`, `GrpcScreenshotTests`

---

## FR-UI-001: Dockable Host UI Control Client

**ID:** FR-UI-001
**Title:** Dockable Host UI Control Client
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The UI control layer shall operate as a dockable thin client of the emulator host. It sends commands and input events, presents host status, and performs media/session/state operations through generated gRPC clients or narrow gRPC-backed client abstractions without referencing or mutating core emulator objects directly.

For local desktop presentation, the in-process Avalonia renderer may bind a host-owned render surface directly to a local emulator/frame source. That direct renderer boundary is limited to frame presentation and is not available to ViewModels. External or remote UIs display streamed output through the gRPC video service/stream APIs.

### Acceptance Criteria

1. The UI control layer can connect to an emulator host, create or attach to a session, and reflect lifecycle state through gRPC-backed abstractions.
2. A media attach panel can dock on the left or right side of the main window and expose disk, tape, cartridge, readonly, status, validation, and recent-media affordances.
3. External or remote UIs display streamed video frames and route audio samples from host stream APIs to the platform audio backend.
4. The UI maps keyboard and joystick input to normalized host input events.
5. The UI invokes media attach/eject, snapshot save/load, and screenshot commands through host services.
6. The UI surfaces host errors and command validation failures without crashing.
7. Reconnecting to an existing remote session restores lifecycle state, current media status, and the latest committed frame.
8. UI ViewModels depend on abstraction-level host client facades; generated gRPC clients are isolated behind adapters.
9. The in-process Avalonia render surface may consume a local frame source directly only from the host/composition layer; ViewModels must not reference runtime internals, concrete emulator devices, or frame-source implementations.

### Source References

- `native/vice/vice/doc/vice.texi`: emulation window, menus, file selector, disk/tape images, reset, settings/resources, monitor, and help behavior as user-facing control requirements.

### Traceability

- **Interfaces:** `UiHostClient`, `HostControlService`, `HostOutputService`, `HostInputService`, `AvaloniaRenderSurface`
- **Related TRs:** TR-MVVM-001, TR-GRPC-BOUNDARY-001
- **Test Suite:** `RemoteUiClientTests`, `UiReconnectTests`, `UiHostBoundaryTests`

---

## FR-HOST-006: Host Runtime Status and Control Telemetry

**ID:** FR-HOST-006
**Title:** Host Runtime Status and Control Telemetry
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The host shall expose runtime telemetry and machine-control state needed by emulator shells without requiring UI clients to read emulator internals directly.

### Acceptance Criteria

1. Host status reports power state, run state, limiter target, measured frames per second, frame count, cycle count, program counter, nominal clock, effective clock Hz, and effective clock percent.
2. Effective clock speed is measured from emulated cycles per real second and remains distinct from the requested limiter target.
3. Pause, resume, step one cycle, step one frame, cold reset, and warm reset commands are exposed through the host boundary.
4. Rewind controls return explicit unsupported status until backing host history support exists.
5. Reset-plus-drive-8 autorun returns `Ok` for supported drive 8 D64/PRG launches and explicit failed-precondition status when backing host or media prerequisites are missing.
6. Telemetry responses are safe for polling by UI clients and do not mutate emulator state.

### Source References

- `native/vice/vice/doc/vice.texi`: performance settings, reset behavior, monitor settings, and emulator status/control behavior exposed through user-facing commands.

### Traceability

- **Interfaces:** `HostControlService`, `HostStatusService`, `IMachine`, `ICpu`, `IClockedDevice`
- **Technical Requirements:** TR-GRPC-BOUNDARY-001, TR-HOST-STATUS-001
- **Test Suite:** `HostStatusTests`, `GrpcHostControlTests`, `StatusBarViewModelTests`

---

## FR-HOST-DIAG-001: Self-Describing Host Diagnostics Attach

**ID:** FR-HOST-DIAG-001
**Title:** Self-Describing Host Diagnostics Attach
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The host shall expose a read-only diagnostics surface that allows humans and external tools to discover the running host, active UI session, session list, and current performance state without scanning ports, reading source code, using UI automation, or creating throwaway probe sessions.

### Acceptance Criteria

1. A local diagnostics client can discover host process metadata, endpoint, protocol package, and version.
2. A local diagnostics client can list live emulator sessions without mutating the registry or creating a new session.
3. A local diagnostics client can identify the session currently bound to the UI.
4. A local diagnostics client can request a performance snapshot with an explicit session id or omit the session id to use the current UI session.
5. The desktop app publishes deterministic debug attach metadata under `%LOCALAPPDATA%\ViceSharp\debug-attach.json` and removes it best-effort on clean shutdown.
6. The desktop UI exposes a Copy Debug Attach Info command containing endpoint, active session, app version, and current status.
7. The diagnostics surface remains read-only and loopback/development scoped.

### Source References

- `docs/Architecture.md`: host/UI boundary and observability sections.
- `native/vice/vice/doc/vice.texi`: monitor/debug status behavior and host diagnostics as user-facing observability.

### Traceability

- **Interfaces:** `DiagnosticsService`, `HostDiagnosticsState`, `DebugAttachFilePublisher`, `DebugAttachInfoProvider`
- **Related FRs:** FR-HOST-005, FR-HOST-006, FR-UI-001, FR-UI-002
- **Technical Requirements:** TR-HOST-DIAG-001, TR-HOST-DIAG-002, TR-HOST-DIAG-003, TR-HOST-DIAG-004
- **Test Requirements:** TEST-HOST-DIAG-001, TEST-UI-DIAG-001

---

## FR-UI-002: Emulator Status and Machine Control Bar

**ID:** FR-UI-002
**Title:** Emulator Status and Machine Control Bar
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The desktop UI shall provide a bottom status and control bar that surfaces host telemetry and machine controls while keeping keyboard/rendering focus stable.

### Acceptance Criteria

1. The status bar displays power state, run state, limiter target, measured FPS, cycle, PC, and effective clock speed.
2. The status bar exposes pause, resume, step cycle, step frame, rewind cycle, rewind frame, cold reset, warm reset, and reset-plus-drive-8 autorun controls.
3. Unsupported controls remain visible but report disabled/unsupported state from the host.
4. Using status controls does not stop emulator rendering or steal keyboard focus unless a text field explicitly takes focus.

### Source References

- `native/vice/vice/doc/vice.texi`: emulation window, reset, performance settings, monitor settings, and command-line control behavior.

### Traceability

- **Interfaces:** `UiHostClient`, `HostControlService`, `HostStatusService`
- **Technical Requirements:** TR-HOST-STATUS-001, TR-UI-SHELL-001, TR-GRPC-BOUNDARY-001
- **Test Suite:** `StatusBarViewModelTests`, `GrpcHostControlTests`, `AvaloniaShellTests`

---

## FR-UI-003: Collapsible Tabbed Emulator Sidebar

**ID:** FR-UI-003
**Title:** Collapsible Tabbed Emulator Sidebar
**Priority:** P1 -- Important
**Iteration:** 1

### Description

The desktop UI shall provide a collapsible sidebar with Peripherals, Settings, and Monitor tabs so routine emulator controls remain available without crowding the display surface.

### Acceptance Criteria

1. A hamburger control collapses and expands the sidebar without stopping emulator input or rendering.
2. The Peripherals tab contains disk, tape, cartridge, recent media, readonly, and keyboard map controls.
3. The Settings tab contains limiter target, display scale/crop, and host/session settings controls.
4. The Monitor tab embeds the reusable monitor control.
5. Tab state remains synchronized with host status and survives sidebar collapse/expand.

### Source References

- `native/vice/vice/doc/vice.texi`: menus, file selector, disk/tape images, settings/resources, keyboard settings, control port settings, and monitor settings.

### Traceability

- **Interfaces:** `UiHostClient`, `HostMediaService`, `HostInputService`, `HostControlService`
- **Technical Requirements:** TR-UI-SHELL-001, TR-GRPC-BOUNDARY-001
- **Test Suite:** `SidebarViewModelTests`, `AttachPanelViewModelTests`, `SettingsPanelViewModelTests`

---

## FR-UI-004: Docked and Pop-Out Monitor Control

**ID:** FR-UI-004
**Title:** Docked and Pop-Out Monitor Control
**Priority:** P1 -- Important
**Iteration:** 1

### Description

The UI shall provide a reusable machine monitor control that can be docked in the sidebar or popped into a separate window while sharing the same host monitor session.

### Acceptance Criteria

1. The monitor control can execute commands, display output, and request register, memory, disassembly, breakpoint, and stepping operations through the host boundary.
2. The monitor can dock inside the sidebar or pop out to a separate window without creating a second emulator session.
3. Docked and popped monitor state stays synchronized.
4. The monitor intentionally takes keyboard focus only while the user interacts with its command input.

### Source References

- `native/vice/vice/doc/vice.texi`: monitor settings, debug settings, memory/register/disassembly-oriented monitor behavior, and machine-control commands.

### Traceability

- **Interfaces:** `IMonitor`, `HostMonitorService`, `UiHostClient`
- **Technical Requirements:** TR-UI-SHELL-001, TR-GRPC-BOUNDARY-001
- **Test Suite:** `MonitorControlViewModelTests`, `GrpcMonitorServiceTests`, `MonitorPopOutTests`
