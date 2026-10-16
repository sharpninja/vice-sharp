# TR-Input-VKM: VICE Keymap Translation Technical Requirement

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Quality Area   | Input Translation              |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-05-13                     |

---

## TR-INPUT-VKM-001: VICE VKM Parser and Selected Map Resolver

**ID:** TR-INPUT-VKM-001
**Title:** VICE VKM Parser and Selected Map Resolver
**Priority:** P0 -- Critical
**Category:** Input

### Description

ViceSharp machine keyboard input shall resolve normalized host key events through a selected machine-specific VICE keymap before updating keyboard matrix state.

### Technical Specification

1. Keyboard map parsing is host-owned and session-scoped.
2. C64 support parses VICE VKM comments, `!CLEAR`, `!INCLUDE`, `!UNDEF`, modifier directives, row/column entries, and shift flags.
3. Custom uploaded keymaps are validated before becoming active.
4. The machine keyboard translator is abstracted so other machine profiles can provide different matrix/key handling.
5. Real-time key state changes update CIA keyboard matrix lines through machine input abstractions, not UI runtime references.

### Acceptance Criteria

1. Built-in and custom VKM maps produce deterministic key-to-matrix mappings.
2. Invalid maps return diagnostics without replacing the selected map.
3. Host input service tests prove selected map entries affect C64 keyboard matrix state.
4. The parser and resolver are usable without Avalonia dependencies.

### Verification Method

- VKM parser unit tests with VICE C64 maps.
- Input integration tests against C64 keyboard matrix/CIA behavior.
- gRPC input service tests for selected-map behavior.

### Architecture Sources

- `docs/Architecture.md`: host/UI boundary, input boundary, and library-first assembly rules.
- `src/ViceSharp.Abstractions`: keyboard map and machine keyboard abstractions.
- `src/ViceSharp.Core/Input`: C64 VKM parser and keyboard matrix implementation (moved from ViceSharp.Chips during ARCH-CHIPGLUE-001).
- `src/ViceSharp.Host`: host input service.

### Related FRs

- FR-INP-001
- FR-INP-006
- FR-HOST-004
