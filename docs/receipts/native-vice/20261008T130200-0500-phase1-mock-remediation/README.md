# Phase 1 mock remediation, F-07 through F-09

Evidence measured through 2026-10-08T13:05:32-05:00. This is remediation within the existing unaccepted mock phase, not advancement to a production gate. The full original ViceSharp goal and every real-emulator gate remain open.

## Scope and evidence

- F-07: four new cases test false/true postflight results. Each first accepts the correct mock transcript, then independently corrupts only the final receipt or only the postflight argument while preserving correct call order and all other arguments. The same `AssertTranscript` must reject that single fault.
- F-08: an explicit `zero-tests` receipt has Passed/Failed/Skipped/Aborted all zero and expects `NoTestsExecuted`. Existing `ProveDecision` controls reject a wrongly accepting mock, wrong reason and wrong half-cycle. No production receipt validator exists or is claimed here.
- F-09: a new current scenario ledger maps actual scenario coverage. The historical reviewed ledger is retained unchanged. Of 34 PHI2/AEC cases, 16 missing/unknown/gap scenarios map the missing-observation ACs; the other 18 map only TR-CYCLE-001 AC4. All original 82 test names are present, plus five new cases.

Applicable current requirements were read from the supported effective-layer route before editing: FR-VICE-001 AC1/AC3, TR-VICE-ORACLE-001 AC1/AC4, TEST-VICE-001 AC1/AC3, and the approved Phase 1 coverage definitions. No requirement text or satisfaction state was changed. `effective-requirements-before.json` preserves the live pending records. The ledger's PHI2/AEC associations retain TR-CYCLE-001 AC4 and the approved missing-observation mapping.

`preimage-copy-proof.json` proves four pre-edit files were copied with matching SHA256 and originals retained. The `.before` files preserve those exact bytes. `native-before-edits.json` records the unchanged existing native baseline. No file under native/vice was edited, generated, reset or cleaned.

## Full validation commands and results

Each command used PowerShell.MCP from the repository root, Release, no filter, with TRX, diagnostics and two-minute blame-hang detection. The retained result JSON includes the complete command and absolute output paths. Every build/test has native before/after inventory receipts in the sibling Phase 2 evidence directory.

- [Oracle full-run receipt](../20261008T082100-0500-phase2-feasibility/phase1-review-f07-f08-oracle-01/result.json): `dotnet test tests/ViceSharp.OracleContract.Tests/ViceSharp.OracleContract.Tests.csproj -c Release`, 87/87 passed, exit 0, timestamp 13:03:29-05:00.
- [VSF full-run receipt](../20261008T082100-0500-phase2-feasibility/phase1-review-f07-f08-vsf-01/result.json): `dotnet test tests/ViceSharp.VsfCompatibility.Tests/ViceSharp.VsfCompatibility.Tests.csproj -c Release`, 199/199 passed, exit 0, timestamp 13:03:38-05:00.
- [Clock full-run receipt](../20261008T082100-0500-phase2-feasibility/phase1-review-f07-f08-clock-01/result.json): `dotnet test tests/ViceSharp.NativeClockProbe.Tests/ViceSharp.NativeClockProbe.Tests.csproj -c Release`, 50/50 passed, exit 0, timestamp 13:03:47-05:00.
- [Post-edit build receipt](../20261008T082100-0500-phase2-feasibility/phase1-review-f07-f08-edit-01-result.json): supported code verification succeeded, native unchanged. Captured hook output is `f07-f08-code-verify.output.log` here.

Total 336 passed, zero failed, notExecuted or aborted. The result JSON also records error/timeout counts. The full real-emulator suite is neither run nor waived by these native-safe projects. A test count is not a parity or complete-behavior claim.

## Traceability and preservation

`scenario-ac-ledger.json` is the new 87-case mapping, with current TRX path/hash and source hash. Original ledger SHA256 remains `B74F9AE910D09F7E72C832860539BD9609BC097EBD3895E5F1A4BA416B9F0556`. No historical test run or earlier incorrect mapping is rewritten. Every row remains `MockContractOnly`, `realGateSatisfied: false`.

Primary work turn: `Codex-20261007T135626Z-plugin-session` / `req-20261008T175952Z-prompt-f7fb`. Decisions are recorded both as decision dialogs and design_decision actions. This packet must be included in the next Opus max continuation with the prior full backlog; it changes previously pinned test/document inputs after the interrupted review.

## Remaining work and acceptance boundary

F-10 remains open: reusable fixture rows/provider binding and genuine filesystem resolution must precede real adapter binding. The five new controls do not resolve it. Other draft findings and unknowns remain as indexed in the interrupted review terminal summary. Opus must independently re-review the remediation; do not mark findings, Phase 1, Phase 2, requirements, TODOs or the whole goal accepted from these local tests.

After compaction reload the complete current add-profile skill and all Global MCP bodies. Preserve local execution, immutable native VICE, mocks-first sequencing, sortable local timestamps, full receipts and the user's failsafe authorization. No commit or push was performed.
