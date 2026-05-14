# Testing Requirements (MCP Server)

- TEST-CFG-001: ## TEST-CFG-001: Configuration and Resource Tests

**ID:** TEST-CFG-001
**Title:** Configuration and Resource Tests
**Priority:** P1 -- Important

### Condition

Resource files, ROM/romset selection, palettes, hotkeys, autostart, peripherals, RAM init, debug resources, and limiter settings are verified with configuration and host service tests.

### Traceability

- **Related FR Area(s):** FR-CFG
- TEST-CIA-001: ## TEST-CIA-001: CIA and Keyboard Matrix Tests

**ID:** TEST-CIA-001
**Title:** CIA and Keyboard Matrix Tests
**Priority:** P1 -- Important

### Condition

CIA timers, TOD, keyboard matrix, joystick interaction, serial shift register, IRQ, and NMI behavior are verified with unit and integration tests.

### Traceability

- **Related FR Area(s):** FR-CIA

---
- TEST-CPU-001: ## TEST-CPU-001: CPU Execution Reference Tests

**ID:** TEST-CPU-001
**Title:** CPU Execution Reference Tests
**Priority:** P1 -- Important

### Condition

CPU instruction, timing, interrupt, and port behavior is verified with unit tests and VICE/Lorenz-style reference comparisons.

### Traceability

- **Related FR Area(s):** FR-CPU

---
- TEST-CRT-001: ## TEST-CRT-001: Cartridge Mapping Tests

**ID:** TEST-CRT-001
**Title:** Cartridge Mapping Tests
**Priority:** P1 -- Important

### Condition

Standard, Ocean, EasyFlash, Action Replay, Retro Replay, and Final Cartridge mapping behavior is verified with cartridge image and banking tests.

### Traceability

- **Related FR Area(s):** FR-CRT

---
- TEST-DRV-001: ## TEST-DRV-001: Drive and IEC Tests

**ID:** TEST-DRV-001
**Title:** Drive and IEC Tests
**Priority:** P1 -- Important

### Condition

Drive CPU/timing, image formats, GCR, IEC bus protocol, fast loader, and host media attachment behavior are verified with smoke, protocol, and reference tests.

### Traceability

- **Related FR Area(s):** FR-DRV

---
- TEST-GRPC-001: ## TEST-GRPC-001: gRPC Boundary Tests

**ID:** TEST-GRPC-001
**Title:** gRPC Boundary Tests
**Priority:** P1 -- Important

### Condition

Protocol, status, control, input, monitor, media, snapshot, capture, and boundary enforcement paths are verified through generated clients and host integration tests.

### Traceability

- **Related FR Area(s):** FR-HOST, FR-UI

---
- TEST-HOST-001: ## TEST-HOST-001: Host Service Tests

**ID:** TEST-HOST-001
**Title:** Host Service Tests
**Priority:** P1 -- Important

### Condition

Host lifecycle, status, media, state, capture, diagnostics, and session ownership behavior are verified with in-process host service tests.

### Traceability

- **Related FR Area(s):** FR-HOST, FR-CFG

---
- TEST-INPUT-001: ## TEST-INPUT-001: Input and VKM Tests

**ID:** TEST-INPUT-001
**Title:** Input and VKM Tests
**Priority:** P1 -- Important

### Condition

Keyboard, joystick, mouse, lightpen, paddle, and VICE VKM behavior is verified with parser, matrix, protocol, and machine integration tests.

### Traceability

- **Related FR Area(s):** FR-INP

---
- TEST-MED-001: ## TEST-MED-001: Media Capture Tests

**ID:** TEST-MED-001
**Title:** Media Capture Tests
**Priority:** P1 -- Important

### Condition

Screenshot, video, audio, synchronized capture, and format selection behavior are verified through capture metadata and round-trip output tests.

### Traceability

- **Related FR Area(s):** FR-MED

---
- TEST-MEM-001: ## TEST-MEM-001: Memory and Banking Tests

**ID:** TEST-MEM-001
**Title:** Memory and Banking Tests
**Priority:** P1 -- Important

### Condition

Address decoding, RAM-under-ROM, Ultimax, VIC bank selection, color RAM, and stack/zero-page behavior are verified with unit and integration tests.

### Traceability

- **Related FR Area(s):** FR-MEM

---
- TEST-MON-001: ## TEST-MON-001: Monitor Tests

**ID:** TEST-MON-001
**Title:** Monitor Tests
**Priority:** P1 -- Important

### Condition

Disassembly, memory display, breakpoints, register operations, bank selection, watch expressions, and monitor RPC behavior are verified through monitor engine tests.

### Traceability

- **Related FR Area(s):** FR-MON

---
- TEST-PRF-001: ## TEST-PRF-001: Machine Profile Tests

**ID:** TEST-PRF-001
**Title:** Machine Profile Tests
**Priority:** P1 -- Important

### Condition

C64, C64C, SX-64, C128, VIC-20, PET, Plus/4, and C16 profiles are verified for required devices, ROMs, clocks, and address maps.

### Traceability

- **Related FR Area(s):** FR-PRF

---
- TEST-SID-001: ## TEST-SID-001: SID Audio Behavior Tests

**ID:** TEST-SID-001
**Title:** SID Audio Behavior Tests
**Priority:** P1 -- Important

### Condition

Oscillator, waveform, filter, ADSR, modulation, sync, noise, digi, external input, and multi-SID behavior are verified with deterministic audio/register tests.

### Traceability

- **Related FR Area(s):** FR-SID

---
- TEST-SNP-001: ## TEST-SNP-001: Snapshot and Replay Tests

**ID:** TEST-SNP-001
**Title:** Snapshot and Replay Tests
**Priority:** P1 -- Important

### Condition

Save/load, deterministic replay, and state diff behavior are verified through round-trip and byte/trace comparison tests.

### Traceability

- **Related FR Area(s):** FR-SNP

---
- TEST-TAP-001: ## TEST-TAP-001: Tape and Datasette Tests

**ID:** TEST-TAP-001
**Title:** Tape and Datasette Tests
**Priority:** P1 -- Important

### Condition

Datasette motor, TAP parsing, pulse timing, write behavior, and turbo loader compatibility are verified with unit and timing tests.

### Traceability

- **Related FR Area(s):** FR-TAP

---
- TEST-UI-001: ## TEST-UI-001: Avalonia Shell ViewModel Tests

**ID:** TEST-UI-001
**Title:** Avalonia Shell ViewModel Tests
**Priority:** P1 -- Important

### Condition

Sidebar, status bar, attach panel, settings, keyboard map selection, monitor dock/pop-out, focus, and startup behavior are verified with fake host clients.

### Traceability

- **Related FR Area(s):** FR-UI

---
- TEST-VIA-001: ## TEST-VIA-001: VIA Integration Tests

**ID:** TEST-VIA-001
**Title:** VIA Integration Tests
**Priority:** P1 -- Important

### Condition

VIA timer, shift register, port handshake, VIC-20, and drive integration behavior are verified with unit and machine integration tests.

### Traceability

- **Related FR Area(s):** FR-VIA

---
- TEST-VIC-001: ## TEST-VIC-001: VIC-II Video Reference Tests

**ID:** TEST-VIC-001
**Title:** VIC-II Video Reference Tests
**Priority:** P1 -- Important

### Condition

Raster timing, display modes, sprites, collisions, badlines, borders, FLI/AFLI, banking, and DMA timing are verified with deterministic frame or trace comparisons.

### Traceability

- **Related FR Area(s):** FR-VIC

---
