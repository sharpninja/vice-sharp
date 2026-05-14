# TR-System-Core: Definable System Core Technical Requirement

## Document Information

| Field | Value |
|-------|-------|
| Quality Area | Architecture / Machine Definition |
| Version | 0.1.0-draft |
| Last Updated | 2026-05-13 |

---

## TR-SYSTEM-CORE-001: Definable System Core for Machine-Specific Bus Behavior

**ID:** TR-SYSTEM-CORE-001
**Title:** Definable System Core for Machine-Specific Bus Behavior
**Priority:** P0 -- Critical
**Category:** Architecture

### Description

ViceSharp machine variants shall express board-level behavior through a definable system core that defines chip interconnect rules, bus arbitration, address decoding, programmable logic behavior, interrupt routing, and model-specific peripheral wiring. Individual chips remain reusable components; machine profiles select a system core specification, and `ArchitectureBuilder` is the glue that instantiates the chips and connects them to the selected system core.

The x64sc C64-family variants are the first acceptance testbed for this requirement because they share a CPU/VIC-II/SID/CIA lineage while differing in PAL/NTSC/PAL-N timing, VIC-II revision, SID revision, CIA revision, PLA/board behavior, datasette/IEC/user-port availability, keyboard availability, ROM selection, C64GS behavior, PET64/SX-64 policy, Japanese resources, and MAX/Ultimax wiring.

### Technical Specification

1. A machine profile identifies both chip selections and a system-core specification.
2. `ArchitectureBuilder` selects the system core from the profile, instantiates chips, and wires chips, buses, clocks, interrupts, memory, ROM, and peripherals into the running machine.
3. The system core defines address decoding and bus routing policy, including PLA-like programmable logic and model-specific board logic.
4. The system core defines interrupt routing, including IRQ and edge-triggered NMI sources from CIA/VIC/RESTORE and other board lines.
5. The system core defines bus ownership and contention policy, including CPU/VIC phase ordering, BA/AEC behavior, DMA cycle stealing, and open-bus/floating-bus sources.
6. Chip implementations expose pins/registers/signals; they do not hard-code whole-machine policy that belongs to board wiring.
7. x64sc variants shall be represented by system-core definitions rather than scattered model-condition branches.
8. gRPC host/session APIs expose selected profile identity and observable behavior, but the emulator host owns the assembled machine.

### Acceptance Criteria

1. The default C64 PAL profile is built by `ArchitectureBuilder` with a selected C64 system core and explicit CPU, VIC-II, SID, CIA1, CIA2, PLA/address decode, RAM, ROM, color RAM, keyboard, joystick, IEC, datasette, and cartridge connections.
2. x64sc variants can override timing, chip revision, ROM/resource identity, board availability flags, and bus/interconnect policy through profile/system-core data.
3. A test can prove that two variants with the same chips but different board policy produce different bus-visible behavior without changing chip code.
4. Core timing tests validate interrupt routing, raster/badline timing, VIC/CPU bus ordering, and selected memory windows through the system core.
5. Final lockstep validation can select every x64sc profile and compare CPU, memory windows, CIA/VIC/SID registers, IRQ/NMI, and raster checkpoints against native x64sc.

### Verification Method

- Unit tests for system-core address decoding and signal routing.
- Integration tests that instantiate each x64sc profile and assert selected system-core policy.
- Native x64sc lockstep tests that compare bus-visible behavior across all required variants.
- Boundary tests that keep UI/client code outside system-core internals.

### Related TRs

- TR-CYCLE-001 (Sub-cycle and bus-phase accuracy)
- TR-DET-001 (Deterministic replay)
- TR-LIB-001 (Library-first reusable core)
- TR-GRPC-BOUNDARY-001 (Host-owned emulator/session boundary)

### Design Decisions

- The system core is not a UI or host concern; it is part of the emulator runtime model.
- `ArchitectureBuilder` remains the machine assembly boundary between system-core policy and concrete chip instances.
- The system core is the right home for PLA/address-decoder logic, machine-specific bus policy, and chip interconnects.
- x64sc model diversity is the first proving ground for the definable computer architecture.
