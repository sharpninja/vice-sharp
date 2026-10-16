Perform an independent hostile plan review of ViceSharp `PLAN-VICE-IMMUTABLE-001`. This is a project-requirement remediation gate. The operator approved the plan on 2026-10-04, but execution beyond non-destructive preparation may begin only if this review returns AGREE with both scores at least 98, zero FAIL, and zero UNKNOWN.

Mandatory execution constraints:

1. Execute the complete local Grok add-profile skill before review work. Read `C:\Users\kingd\.grok\skills\add-profile\SKILL.md` in full, then use PowerShell.MCP to read `add-profile.md` first and every included markdown file under `C:\Users\kingd\.claude\profile` in full, excluding only `add-profile.grok.md`. Dynamically enumerate the files. No Python, Node, raw pwsh, or alternate shell.
2. Work locally on PAYTON-LEGION2 in `F:\GitHub\vice-sharp`. No web, cloud execution, hosted agents, remote runners, repository remotes, or subagents.
3. Do not use `--max-turns`, a turn cap, a timeout, or another artificial work limit.
4. Review read-only. Do not edit source, tests, requirements, plans, TODO state, HANDOFF, git state, or receipts. Edit, Write, WebSearch, and WebFetch are disallowed by the parent launch.
5. Use PowerShell.MCP for every command.
6. Use the supported mcpserver-grok-plugin or exposed MCP tools, never raw REST. Create a dedicated GrokCode MCP session and turn and persist the complete review result. If primary MCP persistence is unavailable, use the plugin's supported v4 failsafe flow. Do not handwrite or directly edit failsafe YAML. Verify failsafe queue state through `workflow.failsafe.status` or the Grok plugin equivalent, record every exact failsafe path and SHA-256, and prove that a queued `client.SessionLog.SubmitAsync` payload contains the complete terminal review result. Report `PersistenceDisposition: failsafe-pending` and `PrimaryReadback: false`. The operator explicitly authorized failsafe use for this hostile review on 2026-10-04. A verified, non-quarantined failsafe write may satisfy this plan-review persistence gate, but it does not count as primary MCP readback and must be drained and read back after service recovery before any final goal, TODO, requirement, or whole-plan done-state change.

Authoritative evidence:

- Remediation plan: `F:\GitHub\vice-sharp\docs\plans\PLAN-VICE-IMMUTABLE-001.md`.
- Full active plan amended by the remediation plan: `C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\01a0ef2b-2331-70d1-9c65-0ef12b15d09a\plan.md`.
- Established behavioral authority: `F:\GitHub\vice-sharp\docs\requirements\technical\TR-Cycle-Accuracy.md`, especially `TR-CYCLE-001` lines 14-50. Confirm from commit `d1f7175b70e0833ef9414faa98607130aa614da4` that the first committed form names VICE x64sc as the reference implementation and accepted gold standard.
- Existing native-VSF behavior: `docs/requirements/functional/FR-Snapshot.md` (`FR-SNP-005`), `docs/requirements/technical/TR-Native-Vice-Snapshot.md` (`TR-LOCKSTEP-VSF-001`), and `docs/requirements/test/TEST-Requirements.md` (`TEST-SNP-002`).
- Integrity guardrails: `docs/requirements/functional/FR-Vice-Reference.md`, `docs/requirements/technical/TR-Vice-Oracle.md`, `docs/requirements/test/TEST-Vice-Oracle.md`, and `docs/requirements/traceability/VICE-Immutable-Oracle-Matrix-2026-10-03.md`.
- Operator correction: native VICE is the immutable target. Nothing under `native/vice` may be edited, patched, generated into, or behaviorally changed. External wrappers outside the submodule are allowed only when they preserve VICE semantics. Missing observability is a blocker, never authorization to change VICE or infer an event.
- Classification correction: this is remediation of divergence from the original requirement, not a new emulator behavior requirement. `FR-VICE-001` is an executable integrity guardrail and AC backfill, not a replacement for or conflict with the legacy requirements.
- Observed starting state recorded by the plan: gitlink and checkout at `cc67418cba58e71d8e70fd353e9de47199ad2075`, 12 modified tracked native source files, 2,527 untracked entries at the 2026-10-04 preflight, and untracked `vice/src/vice-shim-runtime.h`. The earlier 1,148 count was the 2026-10-03 amendment-time snapshot. Independently verify current state without modifying it.
- The prior Phase 3 baseline PIDs `116360`, `81564`, `88124`, and `49176` are no longer alive, and `TestResults/handoff-completion/phase3-baseline/phase3-baseline.trx` does not exist. The run is not a pass and cannot provide acceptance evidence.
- Parent MCP plugin attempts failed locally because `PAYTON-LEGION2:7147` refused the connection. The supported Codex v4 failsafe status then reported `pendingCount: 4` and `quarantineCount: 0` under `F:\GitHub\vice-sharp\.mcpServer\failsafe\Codex\workspaces\RjpcR2l0SHViXHZpY2Utc2hhcnA\pending`. The two records for the operator's explicit failsafe direction are `20261004T140638Z-session_submit-3f2d.yaml` with SHA-256 `E7B6331D8E4191B07700014CCC553AF1ED5600E61072B236B6488DA4224F88C7` and `20261004T140647Z-session_submit-249c.yaml` with SHA-256 `7F8C7EBEB18FD0C486867F753DA194412C251FB2AEC3FE1F6A8CE21E8C1F4D03`. Do not claim primary persistence or live readback while service is unavailable.

Attack surfaces:

A. Original-requirement classification.

1. Verify that VICE was already the reference implementation in the oldest requirement artifacts.
2. Verify that the remediation plan does not represent immutable VICE as newly introduced product behavior.
3. Verify the guardrail IDs add executable cleanliness, provenance, and fail-closed AC without retiring or weakening legacy requirements.

B. Immutable target boundary.

1. Search both plans for any remaining instruction to edit, patch, generate into, fix, or behaviorally alter `native/vice`.
2. Confirm all proposed wrappers, adapters, launchers, build glue, and interposition live outside `native/vice` and preserve target semantics.
3. Confirm patched-native binaries and the in-flight patched-oracle baseline are quarantined from acceptance.
4. Confirm missing bus, timing, snapshot, interrupt, or stolen-cycle observations stop as named blockers and cannot be replaced by `PeekBus`, PC inference, register-only comparison, or fabricated data.

C. BDPv4 and phase safety.

1. Requirements and AC exist before red tests.
2. Phase 0 preserves the current native delta without deletion or restoration.
3. Phase 1 writes red integrity tests without modifying the real submodule.
4. Phase 2 proves wrapper-only feasibility before restoration or lockstep implementation.
5. Phase 3 classifies every tracked modification and injected source file, then restores only the exact inventoried native paths after durable preservation.
6. Every phase has an executable gate, failed 0, skipped 0 where green is required, and an uncapped local hostile review at phase exit.
7. The plan does not authorize commit, push, UWP/Xbox implementation, or unrelated scope.

D. Restoration safety and completeness.

1. Identify any data-loss risk in preserving, restoring, or relocating the 12 tracked changes, injected header, and generated output.
2. Require exact hashes, paths, commit identity, and before/after clean-target proof.
3. Confirm that broad `git reset --hard`, broad cleanup, or unreviewed deletion is forbidden.
4. Confirm that wrapper feasibility is demonstrated before discarding native instrumentation that may contain knowledge needed for the external wrapper.

E. Whole-plan coherence.

1. Verify the amendment truly supersedes the previously wrong `fix the native reference` and `shim export` directions.
2. Verify xvic lockstep, native VSF, settings visibility, requirements recovery, handoff replacement, and final exit conditions remain ordered and testable.
3. Verify every result derived from the dirty oracle is diagnostic only.
4. Identify any contradiction, missing decision, unverifiable gate, or path that could let managed ViceSharp redefine VICE behavior.

F. Workspace compliance.

Verify local-only execution, PowerShell.MCP/no Python, no subagents, source-pinned claims, durable request/response JSONL, a complete reviewer MCP turn stored primarily or through the supported v4 failsafe, no commit/push, and no native mutation by the review.

Default each assertion to FAIL until independently proven. Show every FAIL and UNKNOWN. `OverallVerdict=AGREE` requires zero FAIL, zero UNKNOWN, `AccuracyScore >= 98`, `CompletenessScore >= 98`, and proof that the complete reviewer result was either read back from MCP or durably queued in a valid, non-quarantined v4 failsafe payload under the operator's explicit exception above.

The reviewer MCP turn payload must contain the entire terminal result: full prose, every finding, all attack-surface results, PASS/FAIL/UNKNOWN counts, numeric scores, OverallVerdict, exact final JSON, and the absolute request/response/verdict paths. Query it back when primary MCP is available. When failsafe is required, prove the complete payload is present in the queued record and report the pending and quarantine counts, paths, hashes, and deferred drain/readback obligation.

Return a complete verdict and end with exactly one JSON object:

`{"OverallVerdict":"AGREE|DISAGREE","AccuracyScore":0,"CompletenessScore":0,"PassCount":0,"FailCount":0,"UnknownCount":0,"FailList":[],"SessionId":"...","RequestId":"...","PersistenceDisposition":"primary|failsafe-pending","PrimaryReadback":false,"FailsafePaths":[]}`
