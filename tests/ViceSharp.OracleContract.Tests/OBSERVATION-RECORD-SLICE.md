# Observation record contract (Phase C step 1)

Scope: the record an external wrapper reports for each half-cycle of the immutable VICE oracle, before any comparison. Mock-first; no validator, wrapper or C code exists yet. Serves FR-VICE-001 AC4, TR-VICE-ORACLE-001 AC3 and AC5, TEST-VICE-001 AC4.

## Fields (`ObservationWire`, all nullable)

1. `HalfCycle`: zero-based half-cycle index since the observed reset; malformed when negative.
2. `Phase`: `PHI1` or `PHI2`, exact case.
3. `Aec`: AEC line level.
4. `Ba`: BA line level.
5. `Access`: `read`, `write`, `idle` or `dummy`, exact case.
6. `Address`: 0 to 0xFFFF.
7. `Data`: 0 to 0xFF, required for every access kind. Missing data fails closed; it is never inferred, not even for idle half-cycles.
8. `InterruptMask`: VICE `enum cpu_int` pending mask from `vice/src/interrupt.h` (IK_NMI 1, IK_IRQ 2, IK_RESET 4, IK_TRAP 8, IK_MONITOR 16, IK_DMA 32, IK_IRQPEND 64), so 0 to 0x7F. The plan's "interrupt stage" is recorded in VICE's own representation instead of invented stage names.
9. `StolenCycle`: the CPU is held off the bus in this half-cycle.
10. `Clock`: VICE `CLOCK`, `uint64_t` in `vice/src/types.h`; the whole range is valid, so it can only be missing.

## Per-machine applicability and amendments (PLAN-OBSERVER-001 S4b, 2026-10-20)

- D15 (Payton, 2026-10-20T09:07, "Not applicable: constant 0"): the VIC-20 has no BA line and its CPU core carries no BA state (`vic20cpu.o` lacks `maincpu_ba_low_flags`; VICE `vic20cpu.c` 43 `CLK_INC` calls `vic_cycle` with no bus arbitration), so xvic records carry `Ba` 0, `Aec` 1 and `StolenCycle` 0 as constants under this table; x64sc records carry the real values (`Ba` from the flags `vicii_cycle` returns, `StolenCycle` from the wrapped `vicii_steal_cycles`).
- A1 (proposed, pending Payton): on PHI1 the CPU is off the bus, so `Access` is `idle` and `Address` is 0 under this table; `Data` is the PHI1 bus value VICE exposes (`vicii_read_phi1()` on x64sc, `vic.vbuf` on xvic). A PHI2 record for a cycle without a CPU access (a stolen cycle, or VICE's own non-cycle reset reads) is `idle` with an empty `Data` column, which fails closed as `Missing:Data` and is counted by the trace validator, never filled in.
- A2 (proposed, pending Payton): x64sc `Aec` is derived, not read: low on every PHI1 (the VIC-II owns PHI1) and low on PHI2 once BA has been low for three consecutive cycles including the current one (Bauer, "The MOS 6567/6569 video controller", section 3.14.3; VICE SC embeds the same three-cycle write window in `mainc64cpu.c` 117 to 154). VICE SC exposes no AEC state of its own.

## Decisions

A validator returns the shared `Decision`: accepted at the record's half-cycle, or rejected with `Missing:<Field>` or `Malformed:<Field>` and the record's half-cycle (null when `HalfCycle` itself is missing).

## Mock gate (`ObservationRecordContractTests`)

10 valid records, 10 missing-field controls, 13 malformed-value controls, 10 lenient-validator controls (accepting a missing field must fail), 10 wrong-field-name controls, and a coverage guard. `ProveDecision` additionally rejects a flipped acceptance, a wrong reason and a wrong half-cycle for every case. Real binding comes in Phase C steps 2 to 4 with the same fixtures and assertions.
