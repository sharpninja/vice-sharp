# TR-UI-Shell: Avalonia Emulator Control Shell Technical Requirement

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Quality Area   | UI Architecture                |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-05-13                     |

---

## TR-UI-SHELL-001: Avalonia Emulator Control Shell

**ID:** TR-UI-SHELL-001
**Title:** Avalonia Emulator Control Shell
**Priority:** P1 -- Important
**Category:** UI Architecture

### Description

The Avalonia shell shall present emulator controls through ViewModels and host-client abstractions while preserving the host-owned boundary for emulator state and local rendering.

### Technical Specification

1. ViewModels depend on abstractions or host client facades, not concrete emulator runtime devices.
2. The shell provides status bar, collapsible sidebar, Peripherals/Settings/Monitor tabs, and monitor dock/pop-out composition.
3. Local rendering may use a host-owned direct frame source only from the composition/render-surface layer.
4. Keyboard focus returns to the emulator display after normal controls unless monitor/text entry explicitly takes focus.
5. UI tests can fake host clients without starting the emulator runtime.

### Acceptance Criteria

1. Boundary tests fail if Avalonia ViewModels reference `ViceSharp.Core`, `ViceSharp.Chips`, or concrete architecture/device types.
2. ViewModel tests cover sidebar collapse, tab switching, attach state, settings state, VKM selection, status bar state, and monitor pop-out state.
3. UI startup succeeds while disconnected or while the in-process host is starting.

### Verification Method

- Avalonia ViewModel tests using fake host clients.
- Assembly/reference boundary tests.
- Local startup smoke test.

### Architecture Sources

- `docs/Architecture.md`: MVVM and host/UI boundary sections.
- `docs/requirements/technical/TR-MVVM.md`: strict ViewModel separation.
- `docs/requirements/technical/TR-GRPC-Boundary.md`: UI control boundary and local renderer exception.
- `src/ViceSharp.Avalonia`: shell, ViewModels, and render surface.

### Related FRs

- FR-UI-001
- FR-UI-002
- FR-UI-003
- FR-UI-004
