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

## Decisions

A validator returns the shared `Decision`: accepted at the record's half-cycle, or rejected with `Missing:<Field>` or `Malformed:<Field>` and the record's half-cycle (null when `HalfCycle` itself is missing).

## Mock gate (`ObservationRecordContractTests`)

10 valid records, 10 missing-field controls, 13 malformed-value controls, 10 lenient-validator controls (accepting a missing field must fail), 10 wrong-field-name controls, and a coverage guard. `ProveDecision` additionally rejects a flipped acceptance, a wrong reason and a wrong half-cycle for every case. Real binding comes in Phase C steps 2 to 4 with the same fixtures and assertions.
