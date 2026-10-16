# Proposed legacy 1541 acceptance correction

Prepared by Codex from the source audit dated 2026-10-08T15:29:01-05:00. This is an unapplied requirements proposal, not an implementation approval or accepted HV result.

## Decision requested

Approve or reject the exact five-record before/after delta in `proposed-field-delta.json`. It preserves the long-standing requirement that managed behavior match immutable native VICE. It corrects narrower helper-specific claims that do not establish that behavior. These are newly source-derived criteria, not a claim of verbatim recovery of lost legacy AC.

The delta changes only each record's title, behavioral text (`body` for FR/TR or `condition` for TEST), status, and structured acceptance criteria. It leaves IDs, priority, scope, workspace identity, mappings, and all historical evidence unchanged. No FR is retired.

- `FR-ARCH-TRUEDRIVE-1541-002`: fixed IEC/motor helper wording becomes native-observable IEC, motor and true-drive sector behavior. Five unsatisfied AC.
- `TR-DRV-EDGE-001`: the claimed 300,000-cycle ramp becomes the native VIA2/motor/rotation contract, with distinct media visibility and byte-ready behavior. Four unsatisfied AC.
- `TR-IEC-EDGE-001`: the claimed 985-call response becomes native machine-specific C64 and VIC-20 line resolution and synchronization. Four unsatisfied AC.
- `TEST-DRV-MOTOR-001`: historical five-test wording becomes mock-first and then production-bound motor/rotation/sector acceptance. Five unsatisfied AC.
- `TEST-IEC-TIMING-001`: historical five-test wording becomes mock-first and then production-bound IEC trace acceptance. Five unsatisfied AC.

All five statuses change from `closed` to `pending`; all 23 criteria have `isSatisfied: false`. No existing passing count is converted into acceptance of these criteria.

## Why this correction is necessary

The preceding source audit identifies four problems: tests instantiate simplified helpers instead of the actual true-drive rig; the fixed startup ramp is not the inspected native motor path; the current TEST wording says 300,000 calls while its test uses 300,001; and direct D64 image reads are not proof of DOS/CPU/VIA true-drive reads.

The source audit, including seven native Git blob pins, nine managed/test hashes, numbered excerpts and full MCP proof, is retained in `../20261008T152823-0500-legacy-drive-source-audit/`. The native source commit is `cc67418cba58e71d8e70fd353e9de47199ad2075`.

The proposal keeps simulated image access separate from true-drive behavior, consistent with existing `FR-DRVTRUE-001` and `FR-IECLOAD-001`. It does not change their records, the default True Drive setting, or the product implementation.

## Scope of approval and gates

Approval of this packet authorizes only the exact requirements correction. Before applying, re-read all five records and the mapping through the supported MCP interface. If the approved preimage differs, stop rather than overwrite newer content. Use the supported mixed-kind atomic update with the documented API field translation, then read back every proposed field and verify that unrelated fields and mappings did not change. Retain originals and run requirement traceability before requesting independent HV.

This packet is not a decision-complete adapter implementation plan. Exact native observation adapters and clock/phase extraction remain behind the current unaccepted oracle/HV gate. Do not implement those adapters under a requirements-only approval.

Subsequent implementation must follow BDPv4:

1. Map every approved AC to explicit test fixtures, assertion functions and single-fault negative controls. `proposed-ac-coverage.json` names planned groups, not tests already written or passed.
2. Establish correct-mock success and expected wrong-mock rejection in the approved native-safe scope. Keep the complete required scope green, with no unexplained failures, skips or missing cases, before production edits.
3. Obtain the required independent acceptance, then bind the same fixtures, expected observations, assertions and interactions to the real implementations. Changing the provider must not weaken the oracle.
4. Run all mandatory real-emulator, native VSF and cumulative gates. Mock-only results do not prove runtime parity. Require independent HV AGREE, both scores at least 98, and zero FAIL/UNKNOWN before acceptance.

Explicitly classify existing helper and buffered-image tests. Preserve their historical source and receipts; document any superseded expectation before a later approved correction. Do not silently delete, skip, or rewrite history to remove the contradictions.

## Evidence and current limits

- `proposed-field-delta.json`: exact old/new fields for the five records.
- `proposed-ac-coverage.json`: proposed FR AC to TR/TEST coverage and future test groups.
- `effective-requirements.before.json`, `five-records.before.json`, `mapping.before.json`: supported MCP preimages.
- `verification.json`: structural checks, current-state comparisons, input integrity and diff-check result.
- `session-turn.completed-primary-readback.json`: created only after actual primary MCP readback.

No canonical requirement, production/test source, native source, TODO, commit or push is changed by preparing this packet. No tests or builds run. The earlier tape correction remains a separate pending approval; this draft does not alter it. Opus review is still outstanding, so accuracy/completeness scores and an acceptance verdict are unavailable.
