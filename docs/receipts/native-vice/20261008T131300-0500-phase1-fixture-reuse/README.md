# F-10 shared fixture extraction receipts

Local validation completed through 2026-10-08T13:33:57-05:00. This is test-only remediation within the unaccepted Phase 1 mock gate, not production implementation, emulator parity, requirement satisfaction or hostile acceptance.

## Result and limits

- All 87 oracle cases now consume shared rows/builders and supplied-provider assertions. Each of ten boundary cases additionally rejects the wrong observed resolved path. `BoundaryRequest` does not supply that path.
- Exact before/after TRX names match. All 85 ordered theory rows retain their original arguments. Seventeen source-token comparisons preserve moved input values, expected results, decision assertions, transcript assertions and lifecycle controls. `fixture-extraction-proof.json` records both sides and the limited syntactic transformations used for comparison.
- Full native-safe Release suites: 87 oracle, 199 VSF compatibility, 50 clock probe, total 336 passed. Each exited 0 with failed/notExecuted/aborted/error/timeout 0. These counts are distinct evidence classes, not 336 real-emulator acceptance tests.
- Native before/after state matched for each build/test. Existing native dirt was preserved; it is not a clean oracle. No build or generation ran inside `native/vice`.
- Real adapter binding and actual filesystem link/traversal resolution remain mandatory. Mock success does not establish either. No phase, requirement, TODO or whole-plan done state changed.

## Sources and preservation

`preimage-copy-proof.json` verifies retained `.before` files. `fixture-extraction-proof.json` verifies `.after` source copies, original/current TRX hashes and exact extraction comparisons. `scenario-ac-ledger.json` retains all 87 mock cases and prior AC mappings; `realGateSatisfied` and `hvAccepted` remain false. Its preceding 87-case ledger and the original 82-case ledger were not overwritten. `effective-requirements.json` is the pre-edit live requirement readback.

`compile-log-copy-proof.json` verifies both `clock-gate-compile.log` and `clock-probe-compile.log` against their retained originals. These are the current run's compiler receipts, not replacements for missing historical evidence.

## Exact build and test evidence

The native-bracketed edit checks used the supported Codex `lib/code-verify.ps1` hook on the nearest standalone project. Result/build/native-before/native-after receipts are in sibling `../20261008T082100-0500-phase2-feasibility/` under these prefixes:

- `phase1-f10-assertions-edit-01`
- `phase1-f10-fixtures-edit-01`
- `phase1-f10-shared-assertions-edit-02`
- `phase1-f10-test-bindings-edit-01`
- `phase1-f10-test-bindings-edit-02`

The complete test commands, exit codes and counters are in each `result.json`; each directory also has the named `.trx`, `.log` and `.diag.log`. Each run is `dotnet test <project> -c Release` with no filter, TRX logger, unique results directory, VSTest diagnostics and `--blame-hang --blame-hang-timeout 2m`:

- [Oracle result](../20261008T082100-0500-phase2-feasibility/phase1-f10-oracle-01/result.json), [TRX](../20261008T082100-0500-phase2-feasibility/phase1-f10-oracle-01/phase1-f10-oracle-01.trx), [output](../20261008T082100-0500-phase2-feasibility/phase1-f10-oracle-01/phase1-f10-oracle-01.log).
- [VSF result](../20261008T082100-0500-phase2-feasibility/phase1-f10-vsf-01/result.json), [TRX](../20261008T082100-0500-phase2-feasibility/phase1-f10-vsf-01/phase1-f10-vsf-01.trx), [output](../20261008T082100-0500-phase2-feasibility/phase1-f10-vsf-01/phase1-f10-vsf-01.log).
- [Clock result](../20261008T082100-0500-phase2-feasibility/phase1-f10-clock-01/result.json), [TRX](../20261008T082100-0500-phase2-feasibility/phase1-f10-clock-01/phase1-f10-clock-01.trx), [output](../20261008T082100-0500-phase2-feasibility/phase1-f10-clock-01/phase1-f10-clock-01.log).

The first multi-hunk test-binding patch was rejected without changing the source because its expected context used the wrong negative-skipped error string. Corrected patches compiled. The first static comparison command failed because its helper was not script-scoped; the corrected comparison completed. These are retained process failures, not failed test gates hidden as passes.

## Independent review remains open

The prior Opus 5.5 max review ended on quota, with an interrupted DISAGREE draft at 96 accuracy / 94 completeness, five FAIL and one UNKNOWN. No finalized verdict was accepted. This refactor and its docs are new evidence changes to disclose and pin for that same review. No retry or reviewer substitution is implied by these local results. Full details remain in `../../hv/20261008-noon-catchup-opus.resume-02.terminal-summary.md` and the canonical `HANDOFF.md`.

MCP parent session: `Codex-20261007T135626Z-plugin-session`; turn: `req-20261008T181223Z-prompt-3ff5`. Primary completion readback and `remediation-result.json` are the final receipt references for this slice, not an independent HV verdict.
