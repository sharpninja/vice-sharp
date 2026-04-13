# TR-Cycle-Accuracy: Cycle Accuracy Technical Requirement

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Quality Area   | Accuracy / Fidelity            |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-04-13                     |

---

## TR-CYCLE-001: Sub-Cycle Bus-Phase Accuracy

**ID:** TR-CYCLE-001
**Title:** Sub-Cycle Bus-Phase Accuracy Matching VICE x64sc Behavior
**Priority:** P0 -- Critical
**Category:** Accuracy

### Description

The emulation core shall operate at sub-cycle granularity, modeling the two phases of the system clock (PHI1 and PHI2) and the bus ownership arbitration between the CPU and VIC-II. This level of accuracy is required to correctly emulate cycle-exact tricks used by C64 demos and games. The reference implementation for correctness is VICE x64sc (the cycle-exact VICE variant).

### Rationale

Many C64 programs rely on exact cycle timing for visual effects (raster bars, FLI, sprite multiplexing, open borders) and audio effects (digi playback). A cycle-accurate emulator that does not model bus phases correctly will fail these programs. VICE x64sc is the accepted gold standard for cycle-accurate C64 emulation.

### Technical Specification

1. **Clock Model:** The system clock is divided into PHI1 (VIC-II access phase) and PHI2 (CPU access phase). Each phase is one half-cycle.
2. **Bus Arbitration:** During PHI1, the VIC-II has bus access (for c-access, g-access, p-access, s-access). During PHI2, the CPU has bus access (unless the VIC-II has asserted BA to steal cycles).
3. **BA/AEC Signals:** The VIC-II asserts BA (Bus Available = low) 3 cycles before it needs to steal PHI2 cycles, giving the CPU time to complete its current access. AEC (Address Enable Control) gates the CPU's address bus.
4. **CPU Pipeline:** The CPU's internal pipeline (fetch, decode, execute sub-phases) is modeled per-cycle, not per-instruction.
5. **Tick Granularity:** The `IClockedDevice.Tick()` method is invoked once per half-cycle (PHI1 or PHI2), not once per full cycle.

### Acceptance Criteria

1. All devices (CPU, VIC-II, CIA, SID) are ticked at half-cycle (bus phase) granularity.
2. The VIC-II's DMA steal pattern (badlines, sprites) matches VICE x64sc cycle-by-cycle.
3. CIA timer countdown occurs at the correct bus phase (PHI2 falling edge).
4. CPU memory accesses occur only during PHI2 when AEC is asserted.
5. The Lorenz test suite passes at 100% (same pass rate as VICE x64sc).
6. The VICE cycle-exact test programs (e.g., those in the VICE test suite repository) produce identical output.
7. Raster effects that depend on sub-cycle timing (FLI, AGSP, sprite stretching) work correctly.

### Verification Method

- Automated test suite comparing ViceSharp output to VICE x64sc reference captures.
- Lorenz test suite full execution with pass/fail comparison.
- Visual comparison of known demo effects (Crest, Booze Design, Oxyron productions).

### Related FRs

- FR-CPU-002 (Cycle-accurate execution timing)
- FR-VIC-006 (Badline handling and DMA stealing)
- FR-VIC-010 (Sprite multiplexing DMA timing)

### Design Decisions

- Half-cycle ticking doubles the tick rate compared to full-cycle emulators but is essential for bus-phase accuracy.
- The clock driver is a single-threaded loop that alternates PHI1/PHI2 ticks to all devices in deterministic order.
- The tick order within a phase is: VIC-II, then CIA1, then CIA2, then SID, then CPU (matching the hardware's analog timing).
