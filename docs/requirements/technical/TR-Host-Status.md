# TR-Host-Status: Host Runtime Telemetry Technical Requirement

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Quality Area   | Runtime Telemetry              |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-05-13                     |

---

## TR-HOST-STATUS-001: Measured Emulator Runtime Telemetry

**ID:** TR-HOST-STATUS-001
**Title:** Measured Emulator Runtime Telemetry
**Priority:** P0 -- Critical
**Category:** Observability

### Description

ViceSharp host status shall distinguish requested throttle settings from measured emulation output and measured emulated clock speed.

### Technical Specification

1. The host computes effective clock speed as rolling emulated cycles per real second.
2. Effective clock percent is effective clock speed divided by the active machine profile nominal clock.
3. Requested limiter rate is reported separately from measured FPS and effective clock speed.
4. Cycle, frame, PC, power state, and run state are sampled from host-owned session state.
5. Status polling must not mutate emulator state.

### Acceptance Criteria

1. Status responses include nominal clock Hz, effective clock Hz, effective clock percent, limiter rate percent, measured FPS, frame count, cycle, PC, power state, and run state.
2. Tests can verify limiter target remains stable while effective clock and FPS vary with execution.
3. Paused sessions report stable cycle/frame counters and paused run state.

### Verification Method

- Host status unit tests.
- gRPC status contract tests.
- UI status bar ViewModel tests with fake host status clients.

### Architecture Sources

- `docs/Architecture.md`: host/UI boundary and clock/timing sections.
- `src/ViceSharp.Host`: host-owned session/status services.
- `src/ViceSharp.Protocol`: generated status contract types.

### Related FRs

- FR-HOST-006
- FR-HOST-DIAG-001
- FR-UI-002
- FR-CFG-008

---

## TR-HOST-DIAG-001: Read-Only Diagnostics gRPC Contract

**ID:** TR-HOST-DIAG-001
**Title:** Read-Only Diagnostics gRPC Contract
**Priority:** P0 -- Critical
**Category:** Observability

### Description

ViceSharp shall expose a generated gRPC `DiagnosticsService` for local diagnostics tools. The service is read-only and must not create, remove, start, stop, reset, or otherwise mutate emulator sessions.

### Technical Specification

1. `GetHostInfo` returns process id, endpoint, app version, build SHA if available, host start time, and protocol package/version.
2. `ListSessions` returns live session identifiers and summary status from the runtime registry.
3. `GetCurrentSession` returns the UI-bound session tracked by the host diagnostics state.
4. `GetPerformanceSnapshot` accepts an optional session id; an empty session id resolves to the UI-bound current session.
5. `WatchPerformance` streams snapshots at a requested interval until cancellation.

### Verification Method

- Protocol descriptor tests.
- Diagnostics service unit tests.
- In-process gRPC integration tests.

## TR-HOST-DIAG-002: Current UI Session Tracking Bridge

**ID:** TR-HOST-DIAG-002
**Title:** Current UI Session Tracking Bridge
**Priority:** P0 -- Critical
**Category:** Observability

### Description

The desktop UI and host diagnostics state shall keep the current UI-bound session id synchronized so external diagnostics tools can attach to the same session that the visible app is using.

### Technical Specification

1. Session id changes in the host client are observable by the UI shell.
2. The UI updates host diagnostics state and debug attach metadata when the client session id changes.
3. The tracker records last status and frame update timestamps without blocking render or status polling.

### Verification Method

- Host client session-change unit tests.
- UI debug attach info provider tests.

## TR-HOST-DIAG-003: Debug Attach File Lifecycle

**ID:** TR-HOST-DIAG-003
**Title:** Debug Attach File Lifecycle
**Priority:** P0 -- Critical
**Category:** Observability

### Description

The desktop app shall publish deterministic local attach metadata at `%LOCALAPPDATA%\ViceSharp\debug-attach.json` for the running in-process host.

### Technical Specification

1. The attach file contains `schemaVersion`, `processId`, `endpoint`, `currentSessionId`, `protocolPackage`, `appVersion`, `startedAtUtc`, `updatedAtUtc`, and `authMode`.
2. The file is written atomically on host start and rewritten when the current UI session changes.
3. `authMode` is `none` until a future secured local-host contract exists.
4. The file is deleted best-effort on clean shutdown.

### Verification Method

- Attach file publisher unit tests with a temporary path.
- In-process host lifecycle tests.

## TR-HOST-DIAG-004: Development-Scoped gRPC Reflection

**ID:** TR-HOST-DIAG-004
**Title:** Development-Scoped gRPC Reflection
**Priority:** P1 -- Important
**Category:** Observability

### Description

ViceSharp shall support gRPC reflection for local development diagnostics without enabling reflection unconditionally in release/runtime use.

### Technical Specification

1. Reflection is disabled by default.
2. Reflection is enabled when `VICESHARP_GRPC_REFLECTION=1` or the host environment is Development.
3. Reflection exposes the diagnostics service through the same loopback-only in-process host endpoint.

### Verification Method

- In-process host reflection tests.
- Manual `grpcurl list` smoke validation.
