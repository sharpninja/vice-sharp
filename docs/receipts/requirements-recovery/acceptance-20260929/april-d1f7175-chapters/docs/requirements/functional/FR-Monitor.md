# FR-Monitor: Machine Monitor Functional Requirements

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Subsystem      | Machine Monitor / Debugger     |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-04-13                     |

---

## FR-MON-001: Disassembly View

**ID:** FR-MON-001
**Title:** Real-Time Disassembly View
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The monitor shall provide a disassembly view that decodes 6502/6510 machine code into human-readable assembly mnemonics. The disassembler shall support all official and undocumented opcodes, display correct addressing mode syntax, and handle code that overlaps data regions.

### Acceptance Criteria

1. All 256 opcodes (including undocumented) are disassembled with correct mnemonics and operands.
2. All addressing modes display with standard syntax (e.g., `LDA #$FF`, `STA $D020`, `JMP ($FFFE)`, `LDA ($FB),Y`).
3. The disassembly view can be positioned at any address and scrolls forward/backward.
4. Backward disassembly uses heuristic alignment (scanning back N bytes and disassembling forward to find the correct instruction boundary at the target address).
5. Undocumented opcodes display with their commonly used mnemonics (LAX, SAX, DCP, ISC, etc.) and are visually distinguished from official opcodes.
6. The `IDisassembler` interface accepts an address and returns structured disassembly data (address, bytes, mnemonic, operand, size).
7. Labels and symbols can be optionally applied to addresses.

### Traceability

- **Interfaces:** `IMonitor`, `IDisassembler`
- **Test Suite:** `DisassemblerTests`, `AllOpcodeDisassemblyTests`, `BackwardDisassemblyTests`

---

## FR-MON-002: Memory Hex/ASCII Display

**ID:** FR-MON-002
**Title:** Memory Hex and ASCII Display
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The monitor shall display memory contents in a combined hex/ASCII view, showing 16 bytes per row with the address, hexadecimal byte values, and the printable ASCII representation of each byte.

### Acceptance Criteria

1. Each row displays: a 4-digit hex address, 16 hex byte values (space-separated, with a wider gap at the 8-byte midpoint), and the 16-character ASCII representation.
2. Non-printable characters ($00-$1F, $80-$FF) display as a dot (`.`) in the ASCII column.
3. The PETSCII character set is optionally available for the text column.
4. The display can be scrolled to any address ($0000-$FFFF).
5. Memory contents reflect the current banking configuration (the same address can show different values depending on which bank is selected).
6. Modified bytes (since last refresh) can be visually highlighted.
7. The display updates in real-time when the emulation is paused and memory is edited.

### Traceability

- **Interfaces:** `IMonitor`
- **Test Suite:** `MemoryDisplayTests`, `PetsciiDisplayTests`, `BankViewTests`

---

## FR-MON-003: Breakpoint Management

**ID:** FR-MON-003
**Title:** Breakpoint Management
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The monitor shall support execution breakpoints, read/write watchpoints, and conditional breakpoints. When a breakpoint is hit, the emulation pauses and the monitor displays the current CPU state.

### Acceptance Criteria

1. Execution breakpoints: pause emulation when the PC reaches the specified address.
2. Read watchpoints: pause emulation when a specified address is read.
3. Write watchpoints: pause emulation when a specified address is written (optionally filtering by value).
4. Conditional breakpoints: expression-based conditions (e.g., `A == $FF`, `X > $10`, `MEM[$D020] == $01`).
5. Breakpoints can be enabled, disabled, and removed individually.
6. A maximum of at least 64 simultaneous breakpoints is supported without performance degradation.
7. Temporary breakpoints (auto-removed after first hit) are supported for step-over and run-to-cursor.
8. The `IBreakpointManager` interface provides CRUD operations and hit-count tracking for each breakpoint.

### Traceability

- **Interfaces:** `IMonitor`, `IBreakpointManager`
- **Test Suite:** `BreakpointTests`, `WatchpointTests`, `ConditionalBreakpointTests`, `BreakpointPerformanceTests`

---

## FR-MON-004: Register Inspection and Manipulation

**ID:** FR-MON-004
**Title:** CPU Register Inspection and Manipulation
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The monitor shall display and allow modification of all CPU registers (A, X, Y, SP, PC) and the processor status flags (N, V, -, B, D, I, Z, C). Register changes take effect on the next instruction execution.

### Acceptance Criteria

1. All registers are displayed: A (accumulator), X, Y (index registers), SP (stack pointer), PC (program counter).
2. All status flags are individually displayed and modifiable: N (negative), V (overflow), B (break), D (decimal), I (interrupt disable), Z (zero), C (carry).
3. Registers can be set to arbitrary values via the monitor interface.
4. Setting the PC to a new address causes execution to continue from that address.
5. Setting SP adjusts the effective stack pointer (addresses $0100+SP).
6. Register display includes both hexadecimal and decimal/binary representations.
7. The `ICpu` interface exposes `GetRegisters()` and `SetRegisters()` for programmatic access.

### Traceability

- **Interfaces:** `IMonitor`, `ICpu`
- **Test Suite:** `RegisterInspectionTests`, `RegisterModificationTests`, `FlagManipulationTests`

---

## FR-MON-005: Memory Bank View Selection

**ID:** FR-MON-005
**Title:** Memory Bank View Selection
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The monitor shall allow the user to select which memory "bank" or view is displayed: CPU view (as the CPU sees it with current PLA configuration), RAM view (raw RAM regardless of ROM overlay), ROM view (character/BASIC/KERNAL ROM), I/O view, and individual bank configurations.

### Acceptance Criteria

1. CPU view: shows memory as the CPU currently sees it (respecting PLA banking).
2. RAM view: shows raw RAM contents at all addresses, ignoring ROM overlays.
3. ROM view: shows the ROM contents at their mapped addresses ($A000 BASIC, $D000 CHARROM, $E000 KERNAL).
4. I/O view: shows the I/O register space at $D000-$DFFF.
5. Drive view: shows memory from the attached disk drive's address space.
6. Bank selection is independent of the actual PLA banking state.
7. Editing memory in a specific bank view writes to that specific target (e.g., editing RAM view writes to RAM even if ROM is currently banked in).

### Traceability

- **Interfaces:** `IMonitor`, `IAddressSpace`
- **Test Suite:** `BankViewSelectionTests`, `BankViewEditTests`, `DriveMemoryViewTests`

---

## FR-MON-006: Watch Expressions

**ID:** FR-MON-006
**Title:** Watch Expressions
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The monitor shall support watch expressions that continuously evaluate and display values from memory, registers, or computed expressions. Watch values update whenever the emulation state changes (single-step, breakpoint hit, or periodic refresh).

### Acceptance Criteria

1. Watch expressions can reference memory addresses: `[$D020]`, `[$0400:$0427]` (range).
2. Watch expressions can reference registers: `A`, `X`, `Y`, `SP`, `PC`.
3. Watch expressions support arithmetic: `[$FB] + [$FC] * 256` (constructs a 16-bit pointer).
4. Watch values are displayed in configurable formats: hex, decimal, binary, ASCII/PETSCII.
5. Array watches display a contiguous range of bytes.
6. Watches can be added, removed, and reordered.
7. A watch that reads a side-effect register (like SID OSC3 at $D41B) is marked as potentially disruptive.
8. The watch list persists across monitor open/close within a session.

### Traceability

- **Interfaces:** `IMonitor`
- **Test Suite:** `WatchExpressionTests`, `WatchArithmeticTests`, `WatchFormatTests`
