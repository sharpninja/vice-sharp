# FR-CPU-Emulation: CPU Emulation Functional Requirements

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Subsystem      | CPU                            |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-04-13                     |

---

## FR-CPU-001: Full 6502/6510 Instruction Set

**ID:** FR-CPU-001
**Title:** Full 6502/6510 Instruction Set Including Undocumented Opcodes
**Priority:** P0 -- Critical
**Iteration:** 0

### Description

The CPU emulation shall implement the complete MOS 6502/6510 instruction set, including all 256 opcode entries. This encompasses the 151 official opcodes as well as all undocumented/illegal opcodes (LAX, SAX, DCP, ISC, SLO, RLA, SRE, RRA, ANC, ALR, ARR, SBX, LAS, SHA, SHX, SHY, TAS, NOP variants, JAM/KIL). Behavior of undocumented opcodes shall match the known behavior documented by the VICE team and confirmed by hardware testing.

### Acceptance Criteria

1. All 151 official 6502 opcodes execute with correct results for all addressing modes.
2. All undocumented opcodes produce results matching the Lorenz test suite (C64 Tester) pass criteria.
3. The BCD flag affects ADC/SBC results correctly when the decimal flag is set (6502 mode) and is a no-op on the 6510 in CMOS variants.
4. All addressing modes are implemented: Immediate, Zero Page, Zero Page X/Y, Absolute, Absolute X/Y, Indirect, (Indirect,X), (Indirect),Y, Relative, Implied, Accumulator.
5. JAM/KIL opcodes halt the CPU and raise a diagnostic event on `ICpu.Jammed`.

### Traceability

- **Interfaces:** `ICpu`, `IInstructionDecoder`
- **Test Suite:** `Cpu6502Tests`, `IllegalOpcodeTests`, `LorenzTestSuiteRunner`

---

## FR-CPU-002: Cycle-Accurate Execution Timing

**ID:** FR-CPU-002
**Title:** Cycle-Accurate Execution Timing
**Priority:** P0 -- Critical
**Iteration:** 0

### Description

Each instruction shall consume the exact number of clock cycles as the real MOS 6510, including the extra cycle for page-boundary crossings on indexed addressing modes, the extra cycle for taken branches, and the additional cycle for page-crossing branches. The CPU shall expose its internal pipeline state (fetch, decode, execute sub-cycles) to allow other devices to observe and react at sub-instruction granularity.

### Acceptance Criteria

1. Every opcode consumes exactly the documented cycle count (per the "MOS 6510 Unintended Opcodes" reference and 64doc.txt).
2. Page-boundary crossing adds exactly one cycle for absolute indexed reads (LDA abs,X / LDA abs,Y / LDA (zp),Y).
3. Page-boundary crossing on write instructions (STA abs,X) always takes the extra cycle regardless of whether a page boundary is actually crossed.
4. Taken branches add one cycle; taken branches crossing a page boundary add two cycles.
5. The `IClockedDevice.Tick()` method is invoked once per clock phase, and CPU sub-cycle state is observable via `ICpu.Phase`.
6. The CIA/VIC-II timing interleave passes the VICE timing test suite.

### Traceability

- **Interfaces:** `ICpu`, `IClockedDevice`
- **Test Suite:** `CycleTimingTests`, `ViceTimingComparisonTests`

---

## FR-CPU-003: Interrupt Handling (IRQ, NMI, BRK)

**ID:** FR-CPU-003
**Title:** Interrupt Handling with Correct Timing
**Priority:** P0 -- Critical
**Iteration:** 0

### Description

The CPU shall correctly handle hardware interrupts (IRQ, NMI) and the software BRK instruction. IRQ is level-triggered and masked by the I flag. NMI is edge-triggered on the falling edge. The interrupt sequence shall take exactly 7 cycles. Interrupt hijacking (where an NMI arrives during an IRQ sequence or vice versa) shall be handled correctly per real hardware behavior.

### Acceptance Criteria

1. IRQ is sampled during the penultimate cycle of each instruction; when asserted and the I flag is clear, the IRQ sequence begins after the current instruction completes.
2. NMI is edge-detected (falling edge); once detected, it is serviced after the current instruction regardless of the I flag.
3. The BRK instruction pushes PC+2 (not PC+1) and sets the B flag in the pushed status register.
4. Interrupt hijacking: if NMI occurs during the vector-fetch cycles of an IRQ, the NMI vector ($FFFA) is fetched instead of the IRQ vector ($FFFE).
5. The interrupt acknowledge sequence takes exactly 7 cycles.
6. The `IInterruptController` interface allows external devices to assert/deassert IRQ and NMI lines independently.
7. Passes the Lorenz CIA interrupt timing tests.

### Traceability

- **Interfaces:** `ICpu`, `IInterruptController`
- **Test Suite:** `InterruptTimingTests`, `NmiEdgeDetectionTests`, `InterruptHijackTests`

---

## FR-CPU-004: 6510 I/O Port ($0000/$0001)

**ID:** FR-CPU-004
**Title:** 6510 I/O Port at Addresses $0000/$0001
**Priority:** P0 -- Critical
**Iteration:** 0

### Description

The MOS 6510 processor's built-in I/O port at addresses $0000 (data direction register) and $0001 (data port) shall be emulated. This port controls the PLA memory banking configuration (LORAM, HIRAM, CHAREN bits), the cassette motor, and the cassette sense line. The data port shall exhibit the correct behavior with pull-up/pull-down resistors and the capacitive discharge timing that affects reads of output pins.

### Acceptance Criteria

1. Address $0000 is the Data Direction Register (DDR); bits set to 1 configure the corresponding $0001 bit as output.
2. Address $0001 bits 0-2 (LORAM, HIRAM, CHAREN) control PLA banking and are reflected immediately in the address space configuration.
3. Bit 3 controls the cassette motor (active low).
4. Bit 4 reads the cassette switch sense (active low).
5. Bit 5 is unused on C64 but reads as expected based on DDR/pull-up behavior.
6. When a bit transitions from output to input, the capacitive discharge timing (approximately 350 microseconds to decay) is modeled.
7. The `IMemoryMappedDevice` interface exposes the port to the address decoder.

### Traceability

- **Interfaces:** `ICpu`, `IMemoryMappedDevice`
- **Test Suite:** `IoPortTests`, `BankingConfigTests`, `CapacitiveDischargeTests`

---

## FR-CPU-005: 8502 2MHz Mode Support (C128)

**ID:** FR-CPU-005
**Title:** 8502 2MHz Mode Support for C128
**Priority:** P2 -- Enhancement
**Iteration:** 3

### Description

For C128 machine profile support, the CPU shall implement the MOS 8502 processor's ability to switch between 1MHz and 2MHz operation. In 2MHz mode, the VIC-II cannot access the bus, so the display shows a static pattern. The clock speed is controlled by bit 0 of the VIC-II register at $D030.

### Acceptance Criteria

1. Writing bit 0 of $D030 switches the CPU clock between 1MHz (0) and 2MHz (1).
2. In 2MHz mode, the CPU executes instructions at double speed (halved cycle duration).
3. In 2MHz mode, VIC-II DMA does not occur and the screen displays the characteristic "static" pattern.
4. I/O chip access (CIAs, SID, VIC-II registers) forces the clock back to 1MHz for the duration of the access.
5. The `IClockController` interface exposes the current clock rate and allows mode transitions.
6. CIA timers and SID continue to operate at their expected rates relative to the system clock.

### Traceability

- **Interfaces:** `ICpu`, `IClockController`
- **Test Suite:** `C128ClockModeTests`, `DualSpeedTimingTests`
