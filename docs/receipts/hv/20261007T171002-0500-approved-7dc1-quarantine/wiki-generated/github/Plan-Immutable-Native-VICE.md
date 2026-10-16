# PLAN-VICE-IMMUTABLE-001: Restore the Immutable Native VICE Boundary

Status as of 2026-10-07T17:11:10-05:00: operator-approved BDPv4 remediation plan, Phase 0 still awaiting acceptance. Latest local Opus 5.5 max review: DISAGREE, accuracy 94, completeness 85, PASS 22, FAIL 4, UNKNOWN 0, receipt prefix `docs/receipts/hv/20261007T141100-0500-phi2-ac-remediation-opus`. Its full verdict and separate queue correction are persisted and independently read back from primary MCP. The findings concern missing durable tracking, missing/unknown CPU access kinds and empty/gapped observations, safe pending-record reconciliation, and focused-filter exit wording. Tracking and test-plan wording have been revised. Three safe records were replayed and read back; the operator then explicitly approved preserving/quarantining stale `7dc1`, with matching read-only copies and its completed primary turn unchanged. Active pending count is now zero, with seven retained quarantined records. Proof: `docs/receipts/hv/20261007T171002-0500-approved-7dc1-quarantine/verification.json`. Independent acceptance remains open. Next review is local Grok 4.7 xhigh. No product implementation, cleanup, commit, or push is authorized by this status.

Durable execution tracking: MCP `PLAN-VICEORACLE-001`, priority high, all tasks open. It names the integrity FR/TR/TEST IDs, the complete native-safe mock-contract project, the deferred comparator XMLDoc correction, and real gate `REAL-C64-PHI2-AEC-001`. No mock result satisfies a real emulator requirement.

Recovery evidence: `docs/receipts/native-vice/20261007T092700-0500-phase0-primary-recovery/`. The supported Codex failsafe drain replayed 4 records with 0 failures and 0 quarantined records. `TR-VSFLOCKSTEP-RESUME-001` now has the exact approved external-adapter description and both corrected structured ACs in primary MCP, still pending and unsatisfied. The supported wiki export refreshed the GitHub projection. The traceability command ran with exit 0 but reported 86 unreferenced canonical IDs and 1,011 noncanonical references; that diagnostic exit is not a clean traceability or implementation gate. The preserved native delta remains 12 tracked modifications and 2,527 untracked paths, with matching preservation hashes and reverse-apply check exit 0. Native VICE has not been restored or modified by this recovery.

Classification: remediation of a violation of the original VICE-as-reference requirement. This plan does not introduce a new emulator behavior requirement. `FR-VICE-001`, `TR-VICE-ORACLE-001`, and `TEST-VICE-001` make the existing oracle-integrity boundary executable and auditable.

## Objective

Reviewer rotation, operator directive 2026-10-07: alternate local Grok `grok-4.7` at `xhigh` and local Claude Code `claude-opus-5-5` at `max`, including remediation reviews. The resumed Opus Phase 0 review has terminated; the next slot is Grok, then Opus. Follow the current handoff and whole-goal plan for rotation and local launch receipts. Older Grok-only wording does not prohibit the designated Opus slot. No fallback model, cloud execution, reviewer subagent, or artificial turn/time cap is permitted. Keep all review thresholds and receipt requirements unchanged.

Make the pinned native VICE revision the immutable behavioral target for every ViceSharp parity claim. Move all observation, stepping, snapshot, bus-event, and lifecycle integration outside `native/vice`. Managed ViceSharp must move toward VICE; VICE must never be changed to move toward ViceSharp.

## Existing requirements and integrity guardrails

- Original behavioral authority: `TR-CYCLE-001`, whose first committed form names VICE x64sc as the reference implementation and accepted gold standard.
- Existing product behavior: `FR-SNP-005`, `TR-LOCKSTEP-VSF-001`, `TEST-SNP-002`, and the recovered VSF lockstep requirements. `TR-VSFLOCKSTEP-RESUME-001` remains active, but its implementation boundary is corrected: legacy v1.2/v1.3 `MAINCPU` compatibility is an external adapter around unmodified VICE, never a source patch.
- Executable oracle-integrity guardrails: `FR-VICE-001` AC1-AC4, `TR-VICE-ORACLE-001` AC1-AC5, and `TEST-VICE-001` AC1-AC4.
- Existing lockstep requirements remain active, including `TR-CYCLE-001` AC4.

## Observed starting state

- The superproject records `native/vice` at `cc67418cba58e71d8e70fd353e9de47199ad2075`.
- The checkout is at the same commit but contains 12 modified tracked source files. The untracked inventory grew from 1,148 entries at the 2026-10-03 amendment to 2,527 entries at the 2026-10-04 preflight, so Phase 0 must capture a fresh inventory immediately before preservation and restoration.
- The 12 tracked modifications cover hosted CPU hooks, drive residue changes, reSID probes, and VIC-20 sound-oracle changes.
- `vice/src/vice-shim-runtime.h` is an untracked injected source header.
- These changes predate the current correction. They are still incompatible with the immutable-target requirement.
- Historical 2026-10-04 outage: the plugin read and `workflow.requirements.updateTr` were refused; the canonical correction was local-only and requirement mutation was not a supported v4 failsafe record type. On 2026-10-07 the supported update and primary readback succeeded, including both structured ACs, and `workflow.requirements.generateDocument` regenerated the wiki. The independent Phase 0 recovery review remains required; no test or product phase is claimed started by this recovery.
- The Phase 3 baseline was launched against the existing patched oracle environment. Its four recorded process IDs are no longer alive and no `phase3-baseline.trx` exists. It is not a passing run and remains quarantined from immutable-target acceptance.

## Scope boundary

Allowed locations include wrapper and managed code outside `native/vice`, such as `native/vice-shim*.c`, wrapper build definitions, `src/`, `tests/`, and requirement or receipt artifacts.

Forbidden actions:

- Editing, patching, generating source into, or changing semantics under `native/vice`.
- Treating a binary built from the dirty target as parity evidence.
- Replacing a real bus event with `PeekBus`, PC inference, a register-only comparison, or fabricated data.
- Destructive restoration or cleanup before the operator approves this plan and the existing source delta has a durable preservation receipt.
- Commit or push without a separate operator order.

## Phase 0: Quarantine and preserve the existing delta

1. Record the gitlink SHA, checked-out SHA, tracked diff, untracked source inventory, wrapper binaries, and hashes.
2. Save the meaningful existing source delta as a read-only receipt outside `native/vice`. Separate generated build output from human-authored source changes.
3. Mark every test result produced from the patched native build as diagnostic only, not immutable-target acceptance evidence.
4. Do not delete or restore anything in this phase.
5. Persist the exact pending `TR-VSFLOCKSTEP-RESUME-001` correction and outage evidence in the Codex session turn through supported v4 session-log failsafe. This preserves the decision but does not claim the requirement database was mutated.
6. After service recovery, drain the session-log failsafe, update the TR through `workflow.requirements.updateTr`, prove primary readback, regenerate requirement projections through `workflow.requirements.generateDocument`, and rerun traceability before the mock-validation phase.

Current preservation receipt: `docs/receipts/native-vice/20261004T090000-0500-preexisting-delta/`. Its tracked patch passes reverse-apply validation against the dirty checkout, and its copied `vice-shim-runtime.h` hash matches the source file.

Exit gate:

- Preservation receipt is complete and independently readable.
- If MCP persistence genuinely fails, preserve the complete session/HV result through the operator-authorized supported failsafe, with exact pending paths, SHA-256 values, and `PrimaryReadback: false` disclosed. No unaccounted corrupt or quarantined record may be presented as successfully saved or replayed.
- Before Phase 1 begins, `TR-VSFLOCKSTEP-RESUME-001` is updated in the primary requirement store, read back with both corrected ACs pending, and generated projections no longer mandate the patch. Reconcile pending session/HV records across every workspace agent queue, not only Codex: valid records must be safely drained and read back; corrupt cross-session records must not be replayed into historical turns. On 2026-10-07 the operator explicitly authorized preserving and quarantining five such hook-generated records. Their retained bytes, matching backup/quarantine hashes, reason files, zero replay count, and unchanged primary sessions are recorded in `docs/receipts/hv/20261007T125315-0500-failsafe-quarantine/verification.json`. This is an approved quarantine disposition, not a claim that those records were replayed. Historical quarantine artifacts remain evidence, not active pending writes.
- Subsequent queue reconciliation: `978b`, `7058`, and `40a8` were safely replayed with primary readbacks under `docs/receipts/hv/20261007T163600-0500-phase0-opus-remediation/`. Additional stale snapshot `7dc1` received a separate operator approval and supported quarantine, not replay; `docs/receipts/hv/20261007T171002-0500-approved-7dc1-quarantine/verification.json` proves its bytes preserved, target turn still completed and deeply equal, zero active pending records, and seven total retained quarantine records (the five historical cancellations, older triage `2b86`, and `7dc1`). Do not discard or replay these retained records.
- Hostile review returns AGREE with accuracy and completeness at least 98 and no FAIL or UNKNOWN.

## Phase 1: Mock-validated green integrity tests

Operator correction, 2026-10-07: "RED tests are not acceptable. BDPv4 requires mocking data to prove tests work to green before starting code." Validate AC-covering unit contracts against valid mocked data first. Deliberately incorrect mock data must prove the assertions detect defects; negative tests assert the expected rejection and keep the suite green. No expected-red production run or red-gate review is required or acceptable. Only after this mock-validation gate passes may production implementation begin. Then the full applicable suite for the current increment and all prior work must pass with zero failed and zero skipped tests before advancing. This is correction of the existing BDPv4 process, not a new product requirement.

### Approved test scope and execution boundary

On 2026-10-07 the operator explicitly approved: "Yes, mocks first; retain all real gates." The pre-implementation gate is the complete AC-mapped mock-contract suite in `tests/ViceSharp.OracleContract.Tests/ViceSharp.OracleContract.Tests.csproj`. This is a new test-only project, not a production implementation or a substitute oracle. It inherits the repository test properties and central package versions and references only the existing test SDK, xUnit, and NSubstitute packages. It must not reference TestHarness, production projects, `Vice.Native.proj`, wrapper build targets, or a native binary. Do not add a target or import that invokes `BuildViceNative`. Do not use `DesignTimeBuild`, a nonexistent native-project path, disabled assertions, filters, or skipped tests to evade a build or acceptance gate.

All tests in that project run at each Phase 1 gate, including any earlier mock-contract cases added during this phase. Valid mocks must satisfy the contract assertions. Deliberately incorrect mocks must be rejected by those same assertions, with negative tests asserting that rejection so the executed suite remains green. Test-local DTOs, dependency interfaces, and assertion helpers are permitted; production preflight, wrapper, and comparator implementations are not. Later production adapters must exercise these same contracts against real behavior, not replace their expected values.

The prior real-emulator acceptance tests remain mandatory and unchanged. Historical red results in the handoff are not passing evidence and are not waived, skipped, retired, or silently removed. This mock gate does not run them, claim they pass, or establish emulator behavior. Subsequent implementation gates retain their full current-plus-prior real suites with zero failures/skips/aborts. Existing failures must be remediated before the applicable real gate advances; a focused oracle result cannot stand in for that gate. The original whole-plan requirement-recovery and acceptance remediation work remains open.

### AC-to-contract coverage

All ACs below are covered at the mock-contract level in Phase 1. None is marked satisfied from mock evidence. Native-build and execution fulfillment remains mandatory in Phases 2-4.

1. `ImmutableViceOraclePreflightTests`: accept a clean target at the exact pinned commit; reject the wrong commit, a tracked delta, a changed post-run inventory, and evidence from a patched or differently sourced oracle. Map `FR-VICE-001` AC1/AC3, `TR-VICE-ORACLE-001` AC1, and `TEST-VICE-001` AC1. Assert ordering explicitly: rejected preflight prevents any parity-runner invocation; the accepted preflight is recorded before the runner is invoked. A post-run inventory change invalidates the receipt even when preflight passed. Include incorrect-mock controls that wrongly accept each invalid case or invoke the runner after rejected preflight, proving the acceptance assertions reject them.
2. `ImmutableViceWrapperBoundaryTests`: permit external wrapper inputs/outputs and an unchanged target; reject write, patch, generated-source, path-traversal, or resolved-link destinations under the immutable source root, including changes discovered only after execution. Map `FR-VICE-001` AC2, `TR-VICE-ORACLE-001` AC2, and `TEST-VICE-001` AC2. Real build non-mutation proof belongs to Phase 2; the mock cannot establish it.
3. `ImmutableViceReceiptContractTests`: require exact native commit identity, clean-target before/after evidence, wrapper hash/version provenance, exact command and claimed scope, and nonnegative result counts with failed/skipped/aborted all zero. Reject missing or inconsistent provenance, patched-oracle claims, absent commands/scope, failure counts, and skip/abort counts. Map `FR-VICE-001` AC1/AC3, `TR-VICE-ORACLE-001` AC4, and `TEST-VICE-001` AC3. This closes the previously unnamed receipt/provenance coverage gap; actual receipts remain a later real-execution requirement.
4. `Vic20NativeBusEventTests`: valid mock data describes the required native-observation contract; reject absent capability, absent observation, an incomplete or discontinuous event stream at the first missing half-cycle, and `PeekBus`, PC-derived, fabricated, or patched-target provenance. Cover each required observation category explicitly: CPU bus events, timing, snapshots, and lifecycle. Each category has valid immutable-target provenance and negative controls for absent capability, absent observation, and inferred/fabricated/patched provenance. Map `FR-VICE-001` AC4, `TR-VICE-ORACLE-001` AC3/AC5, and `TEST-VICE-001` AC4. Mock event data is explicitly synthetic test input, never an actual immutable-xvic acceptance trace.
5. `Vic20HalfCycleComparatorContractTests`: preserve the existing `Vic20HalfCycleComparatorTests` scenario with equal registers, bus addresses `$1000` and `$1001`, and exact half-cycle 37. Validate assertions against mock comparison responses and deliberately wrong responses (missing mismatch, wrong half-cycle, wrong field, or wrong values). This is partial comparator-contract coverage for `FR-SNP-005` AC3 and `TEST-SNP-002` AC4, not fulfillment of their restored-checkpoint, 100000-cycle, complete-state requirements. Those real gates remain mandatory. The existing fixture's XMLDoc also names `TR-CYCLE-001` AC4, but that association is incorrect: a bus-address mismatch does not validate PHI2/AEC gating. Do not count that annotation as AC4 evidence. Keep the existing real fixture and its single `LockstepValidator` comparator ownership; do not implement a second comparison algorithm in the contract project. Correct the stale source annotation when the harness can be safely verified without the forbidden native-mutating build.
6. `CpuPhi2AecContractTests`: cover the actual `TR-CYCLE-001` AC4, "CPU memory accesses occur only during PHI2 when AEC is asserted." Use mocked per-half-cycle observations with explicit PHI1/PHI2 phase, AEC level, and nullable CPU access kind (`None`/`Read`/`Write`). Missing access kind is null, never defaulted to `None`; an explicit unknown or out-of-domain value is rejected. Accept no access during PHI1 at either AEC level, no access during PHI2 with AEC low, and read/write or idle during PHI2 with AEC high. Idle is allowed even when access is permitted. Reject read and write observations during PHI1 at either AEC level, read/write during PHI2 with AEC low, missing/unknown phase, missing AEC, and missing/unknown access kind. Require an explicit nonempty expected half-cycle interval and an observation for each position in order. Reject an empty sequence at its expected first half-cycle and leading, interior, or trailing gaps at the exact first missing half-cycle; duplicate or out-of-order observations cannot fill a missing position. Missing observations also map `FR-VICE-001` AC4 and `TEST-VICE-001` AC4. For every invalid observation or sequence, include a deliberately wrong accepting mock and prove the same contract assertion rejects it, including a wrong first-missing-half-cycle result. For valid observations, including idle, include wrong-rejection controls. This is test-local mock coverage, not a production timing algorithm or real C64 bus evidence. Real PHI2/AEC behavior and all seven `TR-CYCLE-001` ACs remain mandatory at their later implementation/acceptance gates.

Deferred real gate `REAL-C64-PHI2-AEC-001` is tracked in MCP `PLAN-VICEORACLE-001`. After Phase 2 wrapper feasibility and native-safe harness integration, actual immutable x64sc CPU bus observations must prove `TR-CYCLE-001` AC4 against real managed execution, with exact half-cycle, phase, AEC and access-kind evidence and fail-closed missing-observation checks. Retain raw native/managed traces, workload and oracle hashes, commands, zero-failure/skip/abort full-suite receipts, and independent HV. The contract project cannot establish this gate. Other `TR-CYCLE-001` ACs retain their own real acceptance evidence obligations.

Status correction receipt: `docs/receipts/hv/20261007T131928-0500-phase0-mock-scope-grok.cycle-status-remediation.json`. Supported primary update/readback changed only `TR-CYCLE-001` status from `closed` to `pending`; its description and all seven structured ACs are unchanged, all unsatisfied. No original or legacy requirement is retired. Steering-hash replay evidence now includes four full reconstructed, read-only files under `docs/receipts/hv/20261007T131928-0500-phase0-mock-scope-grok-b7-reconstruction/`, each matching its previously recorded SHA-256. These were reconstructed after review, not claimed as contemporaneous backups; the original verdicts remain unchanged.

`TEST-VICE-001` is a testing requirement: the effective API supplies its nonempty `condition` and four structured ACs, not a functional/technical `body` field. Preserve that content. Absence of a `body` property is not evidence that the testing requirement is empty.

### Exact mock gate command and immutable-target evidence

Before and after every mock build/test, capture read-only native state to retained evidence outside `native/vice`: gitlink SHA, checkout HEAD, tracked binary-diff SHA-256, sorted untracked-path-list SHA-256, file-content inventory hash, and newest source-tree file modification time (exclude only Git's own metadata). Use `git ls-tree HEAD native/vice`, `git -C native/vice rev-parse HEAD`, `git -C native/vice diff --binary`, `git -C native/vice ls-files --others --exclude-standard`, and PowerShell file/hash inspection. Serialize the evidence from native objects. The before/after identities and hashes must match; any change blocks the gate. The preexisting dirty inventory is preserved diagnostic evidence, never acceptance proof of a clean oracle.

Run through PowerShell.MCP from the workspace root, with no test filter:

```pwsh
dotnet test .\tests\ViceSharp.OracleContract.Tests\ViceSharp.OracleContract.Tests.csproj -c Release --logger "trx;LogFileName=phase1-oracle-contracts.trx" --results-directory .\TestResults\vice-immutable-phase1 --diag .\TestResults\vice-immutable-phase1\vstest.diag.log --blame-hang --blame-hang-timeout 2m
git diff --check
```

Capture stdout/stderr, exit codes, TRX, diagnostics, the scenario-to-AC ledger, and immutable inventory comparison. A timeout or aborted test is not green. The native inventory checks bracket automatic edit-verification builds as well as the explicit test command. The new project must be checked for native-free imports/references before any build; normal TestHarness, solution, or Nuke builds remain prohibited at this stage because their current `BuildViceNative` path edits/generates within `native/vice`.

Exit gate:

- Every test in the complete mock-contract project compiles and passes against valid mocks, and its negative controls prove rejection of incorrect mocks. Failed, skipped, and aborted counts are all zero.
- Native before/after evidence is unchanged, and every in-scope AC has the named positive and discriminating-negative contract coverage above. Focused-filter results do not advance the gate.
- No real-emulator test has been waived or declared passing from these mocks. Full real gates remain mandatory after implementation, including remediation of prior failures before their applicable gate advances.
- Hostile mock-validation-gate AGREE at 98/98 or higher with zero FAIL and UNKNOWN is required before any production implementation. Add the contract project to the normal solution/test invocation only after this gate and a native-safe build path exist, so later full regressions include it without triggering the forbidden legacy native build.

## Phase 2: Wrapper-only feasibility proof

Implement the smallest external proof that immutable xvic can be controlled and observed without source changes. The proof may not use the dirty working checkout as its oracle. Materialize the pinned commit into a separate local clean source tree outside `native/vice`, record its tree identity and complete preflight status, keep generated/build output outside that source tree, and verify the source tree remains clean after the proof. Evaluate wrapper mechanisms in this order:

1. Existing public VICE monitor, trace, history, snapshot, and exported interfaces.
2. External process control around the stock executable.
3. Link-time symbol wrapping or function-table interposition performed entirely by wrapper build inputs outside both `native/vice` and the clean materialized source tree, with byte-identical target sources and unchanged target semantics.

The same boundary applies to recovered snapshot compatibility. A dedicated external adapter must prove that validated v1.2 `MAINCPU` input gains only the absent v1.3 and v1.4 fields, v1.3 gains only the absent v1.4 field, v1.4 passes through byte-for-byte, all following bytes and module ordering remain unchanged, and malformed or unsupported layouts fail closed before immutable VICE reads the temporary copy. `native/patches/vice-shim-runtime.patch` is preservation evidence only and may not participate in a build.

The proof must obtain the real executed CPU bus address, data, read/write direction, dummy/real classification, clock position, interrupt stage, and stolen-cycle state needed by Slice 3c. It must also support deterministic cycle-boundary control.

If the pinned VICE source cannot be materialized and built without writing generated files into either source tree, or immutable VICE cannot expose a required observation through the allowed wrapper mechanisms, stop with a named blocker. Do not patch VICE, generate into it, or weaken the comparator.

Exit gate:

- The full applicable current-plus-prior suite, including wrapper and legacy v1.2/v1.3/v1.4 snapshot-compatibility proofs, has failed 0, skipped 0, and aborted 0. Focused filters are diagnostic only.
- The separate pinned source materialization has zero tracked or untracked changes before and after the proof.
- The dirty `native/vice` checkout is byte-for-byte unchanged by the proof.
- Hostile green-gate AGREE at 98/98 or higher with zero FAIL and UNKNOWN.

## Phase 3: Migrate or reject the existing native modifications

For each of the 12 tracked source changes and the injected runtime header, classify it as:

- Observation/control logic to rehome in an external wrapper.
- Legacy snapshot normalization to rehome in the external compatibility adapter, with exact v1.2/v1.3/v1.4 fixture coverage.
- Behavior correction that belongs in managed ViceSharp.
- Target modification that must be rejected from the oracle.

After the preservation receipt and wrapper feasibility proof are accepted, restore the `native/vice` tracked checkout to the pinned commit and remove only paths from the preserved untracked inventory after classifying them as generated output or injected source. Future build output must stay outside the submodule. Broad reset or cleanup commands remain forbidden. This restoration is destructive and begins only under the operator's 2026-10-04 approval and after the hostile gates above agree.

Exit gate:

- Recorded and checked-out gitlink SHAs match.
- `git -C native/vice diff --exit-code` succeeds.
- `git -C native/vice status --porcelain=v1 -uall` returns no entries.
- No generated output, injected source, or wrapper input under `native/vice` participates in the oracle build.
- Wrapper and managed replacements pass the full applicable current-plus-prior suite with failed 0, skipped 0, and aborted 0. Focused tests do not advance this gate.
- Hostile green-gate AGREE at 98/98 or higher with zero FAIL and UNKNOWN.

## Phase 4: Resume xvic lockstep Slice 3c

Extend only the existing `LockstepValidator`. Compare managed and immutable xvic on every required half-cycle for:

- A, X, Y, S, P, and PC.
- Opcode and micro-op phase.
- Real bus address, data, and read/write direction.
- Dummy cycle versus real access.
- IRQ and NMI pipeline stage.
- Stolen-cycle state.

All native inputs come from the accepted external wrapper. Managed behavior changes to match VICE. The native target remains untouched.

Exit gate:

- The full applicable current-plus-prior suite, including comparator fixtures, native wrapper tests, and `REAL-C64-PHI2-AEC-001`, passes with failed 0, skipped 0, and aborted 0. Focused filters are diagnostic only.
- C64 idle pins remain green when shared CPU or clock code changes.
- Clean-target proof is attached to every native-backed receipt.
- Hostile green-gate AGREE at 98/98 or higher with zero FAIL and UNKNOWN.

## Phase 5: Resume the remaining approved plan

Continue the original Phase 3 repair pairs and Phase 4 settings work only after Phase 4 above is accepted. Every native-backed phase repeats clean-target preflight and provenance capture.

## Whole-plan exit additions

The handoff completion plan cannot finish until:

- `FR-VICE-001`, `TR-VICE-ORACLE-001`, and `TEST-VICE-001` have executable evidence for every AC.
- Native VICE acceptance evidence comes from the pinned unmodified target.
- All required wrapper capabilities exist outside `native/vice` or the plan stops at an explicit immutable-target blocker.
- Primary MCP readback proves the corrected `TR-VSFLOCKSTEP-RESUME-001`; no requirement projection or active plan instructs an executor to apply the legacy patch.
- Every executable phase exit has retained full applicable current-plus-prior suite evidence with zero failures, skips, and aborts. A missing capability or other named blocker leaves the goal incomplete; recording it is not an alternative completion condition.
- The final hostile review agrees at accuracy and completeness at least 98 with zero FAIL and UNKNOWN.
