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
- FR-UI-002
- FR-CFG-008
