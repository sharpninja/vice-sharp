# ViceSharp Implementation Plan

## Current State (2026-04-19)

**Completion:** ~75%

**Strengths:**
- Chip implementations (opcodes/raster/timers/synthesis)
- Core primitives (lock-free, zero-alloc)
- Build clean (0 errors, 2 warnings)

**Gaps:**
- No full C64Machine wiring
- No ROM load-to-boot path confirmed
- No VICE trace logger
- No lockstep validation run

## Priorities

### Priority 1: C64Machine Full Wiring
**File:** `src/ViceSharp.Core/ArchitectureBuilder.cs`
**Task:** Wire all components with interrupt routing
- Add IBus field with `new BasicBus()`
- Add `_irqLine = new InterruptLine(InterruptType.Irq)`
- Add `_nmiLine = new InterruptLine(InterruptType.Nmi)`
- Register all chips: CPU, VIC, CIA1, CIA2, SID, PLA
- Register RAM at 0x0000-0xFFFF
- Register color RAM at 0xD800-0xDBFF
- Register ROMs: BASIC (0xA000), KERNAL (0xE000), CHAR (0xD000)
- Register clock devices
- **Acceptance:** BasicBootTest passes

### Priority 2: ROM Load-to-Boot Path
**File:** `src/ViceSharp.RomFetch/C64RomLoader.cs`
**Task:** Wire ROM loader into ArchitectureBuilder
- Add `_romLoader.LoadAllRoms()` call
- Test with VICE ROM files
- Verify `$FCE2` reset vector
- **Acceptance:** CPU jumps to KERNAL reset after reset()

### Priority 3: Trace Comparison
**File:** `tests/ViceSharp.TestHarness/TraceComparisonValidator.cs`
**Task:** Create VICE trace generator
- Add `GenerateTrace(int cycles)` method
- Output format: `[F:000 L:001 C:01] PC:A000 A:00 X:00 Y:00 S:FD P:24`
- **Acceptance:** Trace file generated for 1000 cycles

### Priority 4: Lockstep Validation
**File:** `tests/ViceSharp.TestHarness/LockstepTestRunner.cs`
**Task:** Run comparison test
- Execute 10,000 cycle comparison
- **Acceptance:** >95% cycle match

## Task Checklist

- [ ] ArchitectureBuilder - wire all chips + RAM + ROM
- [ ] C64RomLoader integration test
- [ ] Reset vector verification test
- [ ] Trace file generator
- [ ] Lockstep test runner
- [ ] First boot validation (10 frame test)

## Time Estimate

- Priority 1-2: 1-2 hours
- Priority 3-4: 1-2 hours
- **Total:** 2-4 hours development
