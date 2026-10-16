# Legacy tape acceptance correction proposal

Prepared 2026-10-08T15:11:05-05:00. **Not approved, not applied, not independently accepted.** This is a requirements correction proposal within the existing full-goal recovery work, not a new product implementation gate.

## Decision requested

Approve the exact five-record delta in [proposed-field-delta.json](proposed-field-delta.json): replace the behavior/condition text, add 23 unsatisfied acceptance criteria, change the existing `closed` statuses to `pending`, and remove historical test-count claims from the two TEST titles for:

- `FR-RUNTIME-TAPE-002`: six criteria.
- `TR-TAP-EDGE-001`: four criteria.
- `TR-TAPE-EDGE-001`: four criteria.
- `TEST-TAPE-RAMP-001`: four criteria.
- `TEST-TAPE-SENSE-001`: five criteria.

Preserve IDs, FR/TR titles, priorities, scope, mappings, and historical evidence. The TEST titles become "Datasette motor startup and delayed-stop parity" and "Datasette transport, sense wiring and recording parity". Nothing is retired or deleted. Approval of this delta would not approve product code, satisfy an AC, replace the full-goal plan, waive native VSF or real-emulator gates, or authorize changes to `native/vice`. The other three legacy emulator FRs and the MCP placeholder remain unresolved and unchanged.

These are **new corrective criteria derived from pinned source**, not verbatim recovered historical criteria. The JSON contains every old and proposed value. The initial draft, before removing the TEST-title count claims, is retained unchanged as `proposed-field-delta.initial.json` with SHA-256 `5004608206F617FEED8A45A7DB92C44AE6EBB48AEEF85580F0BD298280371650`. Use `proposed-field-delta.json`, not that initial draft, for the requested approval; its current hash is retained in `verification.json`.

## Evidence that changes the next action

The four pre-rename legacy snapshots already contain zero structured AC. All four current titles, bodies and statuses match those snapshots. The September 28 recovery ledger explicitly labels all four proposed AC sets `inferred-from-surviving-fr-body`. These inspected artifacts do not establish whether additional historical criteria exist elsewhere. They do establish that copying the staged proposals is not a verified restoration of historical AC. See [rename-preservation-proof.json](rename-preservation-proof.json) and [historical-inferred-proposals.json](historical-inferred-proposals.json).

For the tape requirement specifically:

1. The stored expression `!PlayPressed || !RecordPressed` disagrees with the managed `!(PlayPressed || RecordPressed)` in both single-button cases. [The four-row truth table](sense-expression-truth-table.json) proves the Boolean disagreement, not native runtime parity.
2. VICE routes C64 tape sense to CPU port `$0001` bit 4, subject to direction masking. CIA1 FLAG receives pulse events. VIC-20 uses VIA1 port A bit 6 for sense and VIA2 CA1 for read pulses. The managed `C64MemoryMap.ReadVisibleProcessorPort` already uses CPU port bit 4 correctly; do not misreport that method as the wrong-port defect. The FR/TR and `DatasetteSenseLineTests` retain the wrong CIA1-port description.
3. Native RECORD with no image is ignored. A read-only image does not accept RECORD. The existing managed test `TryWritePulse_StorePulse_WhenRecordPressed` creates no image yet expects recording success. This is an inspected expectation conflict, not a newly executed failure.
4. Native motor-off schedules a delayed stop. Re-enabling before the stop executes cancels it without a new startup ramp. `MotorOff_ThenOn_ResetsRamp` instead demands a new ramp after one off tick. Copying that expectation into AC would perpetuate the divergence.
5. The native writer consumes clocked write-line transitions and has gap encoding, remainder, first-write and write-failure behavior. A managed buffer-count increment alone cannot establish native recording parity.

All six native files examined match their exact Git blobs at `cc67418cba58e71d8e70fd353e9de47199ad2075`; see [native-source-pins.json](native-source-pins.json). Source inspection is not a substitute for native execution. A pulse-duration getter is not itself a machine-visible pulse event; the real comparison must include the owning machine's clocked binding.

## Gated execution after approval

1. Re-read all five effective records and compare the exact old content, status and AC arrays. Stop on drift. Use the supported requirements batch workflow for the approved fields, preserve every unrelated field, and prove all five records by primary readback. Do not reapply the old recovery bundle or modify its provenance receipts.
2. Refresh the canonical projections through the supported requirements export path and run the traceability check. Keep every AC unsatisfied and all five statuses pending. Retain the original assertions and receipts as historical evidence, with explicit documentation of the expectations being superseded.
3. Submit the complete correction, source evidence and planned test cases to the required local Opus max review. The existing review and phase gates remain in force. This proposal has no independent scores or acceptance verdict.
4. Before product implementation, prepare the native-safe test seam and exact shared fixtures for the named cases in the TEST deltas. Correct mocks and single-fault negative controls must all execute within a green suite. No red production suite, test-count-only gate or copied production expectation is acceptable.
5. Only after the applicable accepted mock gate and native-safe harness integration, bind the same fixtures and assertions to managed and immutable native providers. Validate machine-visible event order, not merely internal getters. Run all required current-plus-prior suites, including retained native VSF and real parity gates, then obtain independent HV before any satisfaction or done-state change.

This requirements proposal deliberately does not claim to be an implementation-ready native tape adapter design. That design depends on the still-unaccepted immutable-wrapper and native-safe harness work. No source/build/test execution, requirement mutation, or phase advancement was performed here.

## Receipt boundaries

`live-fr-readback.json` retains the four direct FR responses. Wrapper-returned timestamps are retained as returned but are not used as historical mutation proof. `source-excerpts.json` and `managed-input-pins.json` retain the inspected code/test evidence. The old recovery ledger, old provenance file and prior review packet are not rewritten. New evidence must be included and pinned in the next actual HV request.
