# Phase C step 1 on PAYTON-LEGION2: observation record contract, mock first

Goal GOAL-LOCKSTEP-REMEDIATION-001, task "Step 8", Phase C step 1 (OMAR-VICE# brief 2026-10-19T19:52; scope HANDOFF.lockstep-plan.md section 4 Phase C step 1). D3: no review; this receipt carries the self-assessment. Mocks only: no validator, decoder, wrapper or C code was written, and nothing under native/vice or artifacts/immutable-vice was touched.

Head when the suite ran: f743157d694e6cdfe94cc29a7a4ea49e06e131ff on develop. dotnet 10.0.402.

## What was added

- `tests/ViceSharp.OracleContract.Tests/ObservationRecordContractTests.cs` and `OBSERVATION-RECORD-SLICE.md`: the ten-field record `ObservationWire` (half-cycle index, PHI1/PHI2, AEC, BA, access kind read/write/idle/dummy, address, data, interrupt mask, stolen-cycle flag, clock), all fields nullable so a missing field is representable, and the validator contract on the shared `Decision` (`Missing:<Field>`, `Malformed:<Field>`, half-cycle).
- `tests/ViceSharp.NativeClockProbe.Tests/ObservationLineContractTests.cs` and `OBSERVATION-LINE-SLICE.md`: the `VICE_OBS,1,...` stdout line the step 3 wrappers will emit (12 positional decimal columns, one LF), and the decoder contract (`Missing`, `Malformed`, `Framing:Prefix|Version|ColumnCount|Terminator`).

Value domains taken from VICE, read only: the interrupt field is VICE's `enum cpu_int` pending mask (`vice/src/interrupt.h`: IK_NMI 1 to IK_IRQPEND 64, so 0 to 0x7F); the clock is VICE `CLOCK` (`vice/src/types.h`: `uint64_t`).

## Mock gate

- `ObservationRecordContractTests`: 54 passed, 0 failed, 0 skipped. 10 valid records, 10 missing-field controls, 13 malformed-value controls, 10 lenient-validator controls, 10 wrong-field-name controls, 1 coverage guard. Every case also rejects a flipped acceptance, a wrong reason and a wrong half-cycle (`ProveDecision`).
- `ObservationLineContractTests`: 55 passed, 0 failed, 0 skipped. 9 valid lines (13 result corruptions rejected each), 10 missing-field controls, 24 malformed-value controls covering all ten fields, 10 framing controls (4 corruptions rejected each), 2 structure guards.

## Full applicable suite (unfiltered, Release)

Run by `docs/receipts/lockstep/20261019T194456-phase-b-windows/Invoke-PhaseBNativeSafe.ps1 -Suffix phase-c-step1-windows`; TRX, console logs, run.log and summary.json are in this folder.

- ViceSharp.OracleContract.Tests: 143 total, 143 passed, 0 failed, 0 skipped (89 before this step plus 54).
- ViceSharp.VsfCompatibility.Tests: 199 total, 199 passed, 0 failed, 0 skipped.
- ViceSharp.NativeClockProbe.Tests: 124 total, 124 passed, 0 failed, 0 skipped (69 plus 55).
- native/vice inventory: 2,539 entries before and after, SHA256 C7848A24867CD60C both times: unchanged.

## Self-assessment

- Accuracy 98. Every count above is from the TRX and run.log in this folder; the VICE value domains are quoted from the files named.
- Completeness 94. Below 98, so not a pass. Gaps:
  1. Data is required on every record, idle and dummy included (fail closed). Whether the wrappers can observe a data-bus value on idle half-cycles is unknown until step 3; if they cannot, that is a named blocker, not a reason to relax this field.
  2. "Interrupt stage" is represented by VICE's pending mask. If the comparison needs the exact cycle at which VICE samples or takes the interrupt, an additional field will be required; to be decided against the step 3 probe evidence, not inferred now.
  3. The contract is not yet bound to any real binding (steps 2 to 4).
  4. x64sc versus xvic field availability is unproven (step 3).

## Not done here

PLAN-VICEORACLE-001 task 3 stays open until step 12.
