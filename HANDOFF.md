# ViceSharp Handoff

Generated local: 2026-09-28 19:27:44

This file completely replaces the previous handoff at the operator's explicit request.

## Current State

- Work is stopped at the plan-before-implementation boundary.
- No ViceSharp product code was changed during the requirements recovery work described here.
- No recovery plan has been approved for implementation.
- No commit, push, pull request, or deployment was performed.
- Do not resume product implementation until the decision-complete BDPv4 plan is written, independently accepted, presented to the operator, and explicitly approved.
- Do not clean, reset, revert, or overwrite unrelated dirty files. The dirty tree contains operator and prior-agent work.

Repository state at handoff creation:

- Workspace: `F:\GitHub\vice-sharp`
- Branch: `main`
- HEAD: `687b03a75c1971bdb4bcbe07f2b5ea2d94f47b0d`
- Origin: `https://github.com/sharpninja/vice-sharp.git`
- Tracked dirty entries before replacing this file: 72
- Untracked entries before replacing this file: 402
- `HANDOFF.md` was already modified and was explicitly deleted before this replacement was created.

## Mandatory Startup and Trust Rules

1. Read `AGENTS-README-FIRST.yaml` before any other repository action.
2. Read `AGENTS.md` and this handoff.
3. If context has compacted, execute the complete `add-profile` skill and read every eligible global profile file before resuming.
4. Use the supported Codex MCP plugin or native MCP tools for session log, TODO, requirements, triage, and traceability operations.
5. Use PowerShell.MCP for commands. Do not use Python.
6. Use sortable local response timestamps in exact `yyyy-MM-dd HH:mm:ss` format, without UTC or timezone suffixes.
7. Use UTC only inside structured audit fields and IDs where the schema requires it.
8. Preserve request and response JSONL for every hostile review and store the complete hostile result in the reviewer's MCP session turn.
9. Accuracy and completeness must both be at least 98 for hostile acceptance.
10. Never mark a requirement, TODO, goal, or plan step done while a required failure, skip, unknown, or incomplete hostile receipt remains.

## Operator Contract for ViceSharp and VICE

The recovered foundational relationship is:

- Observable emulator behavior is a functional requirement.
- VICE behavior is the accepted behavioral oracle, including VICE-compatible quirks and bugs unless an operator-approved requirement explicitly says otherwise.
- Internal architecture and implementation mechanism are technical requirements.
- Executable differential proof is a test requirement.
- Every FR, including legacy FRs, must have acceptance criteria appropriate to validating that FR.
- A passing narrow checkpoint cannot replace a broad behavioral contract.
- Legacy UWP and Xbox requirements are retained. They are not retired. UWP and Xbox implementation is outside the current implementation scope.
- ViceSharp may use an idiomatic managed architecture, but observable timing, state transitions, bus effects, device behavior, and supported media behavior must match the relevant VICE machine.

The immediate product goals remain:

1. Recover the authoritative requirements without narrowing or losing legacy behavior.
2. Complete and prove fail-closed lockstep parity with `xvic` for VIC-20.
3. Hide and show VIC-20 settings according to the selected machine without losing draft values.

## How the Requirements Diverged

Historical evidence established the following:

- The original April 2026 requirements treated VICE x64sc and hardware-backed evidence as the correctness reference.
- Commit `e73a820` on 2026-05-27 regenerated the MCP/wiki export and collapsed 219 FR headings, 40 TR headings, and 259 TEST lines to 12 FR headings, 9 TR headings, and 1 TEST line.
- Broad `FR-VIC-001` and `TR-CYCLE-001` content was replaced by placeholders.
- Commit `f608d02` on 2026-05-30 added narrow PAL 19,656-cycle checkpoint tests under `BACKFILL-VIDEO-001`.
- Commit `d1cbf30` on 2026-05-31 populated the reused legacy IDs from that narrow wording.
- No persisted rationale was found for reusing and narrowing the foundational IDs.
- This proves requirements and traceability loss. It does not alone prove which individual runtime defects were caused by that loss.

## Requirements Recovery State

The recovery preview under `docs/receipts/requirements-recovery/20260928T195211Z` contains:

- 513 typed records
- 232 FRs
- 53 legacy FRs
- 1,026 FR acceptance criteria
- 442 proposed forward operations with 442 reverse-order inverses

No live ViceSharp requirement mutation was applied from that preview.

The preview hostile gate was `DISAGREE`, accuracy 99, completeness 99, with 15 PASS, 9 FAIL, and 0 UNKNOWN. The nine unresolved preview defects were:

1. Atomicity is not the only blocker. Four TR bodies were erased, two placeholders survived, and four records lost acceptance criteria.
2. Several inferred FR criteria contain implementation mechanisms and require owner approval.
3. Thirteen records use noncanonical IDs or the wrong type prefix.
4. Four TR bodies are empty and two placeholder TRs remain.
5. Non-allowlisted narrowing and acceptance-criteria loss remain.
6. Mapping presence exists, but semantic appropriateness and per-AC executable VICE coverage are not proved. Thirteen active FR mappings remain provisional.
7. Immutable ROM hashes, trace-equivalence proof, and named machine, media, and workload manifests are absent.
8. The ledger uses whole-record precedence instead of per-field provenance, decisions, reasons, and merges.
9. Guardrails do not yet block empty bodies, placeholders, malformed IDs, misclassification, AC loss, or narrowing.

Live McpServer requirements created or updated for the recovery enabler:

- `FR-MCP-REQRECOVERY-001`: 8 pending acceptance criteria
- `TR-MCP-REQRECOVERY-001`: 7 pending acceptance criteria
- `TEST-MCP-REQRECOVERY-001`: 8 pending acceptance criteria
- `FR-MCP-SESSIONLIFE-003`: updated
- `TR-MCP-SESSIONLIFE-003`: updated
- `TEST-MCP-SESSIONLIFE-004`: 6 pending acceptance criteria
- `FR-MCP-PLUGININT-001`: updated
- `TR-MCP-PLUGININT-001`: updated
- `TEST-MCP-PLUGININT-001`: updated

Verified session-lifecycle mapping:

- FR: `FR-MCP-SESSIONLIFE-003`
- TR: `TR-MCP-SESSIONLIFE-003`
- TEST: `TEST-MCP-SESSIONLIFE-002`
- TEST: `TEST-MCP-SESSIONLIFE-004`

All criteria remain pending and unsatisfied. Do not mark them satisfied without executable proof.

Generated McpServer requirement projections were refreshed under:

- `F:\GitHub\McpServer\docs\Project`
- `F:\GitHub\McpServer\docs\Project\wiki`

## Current MCP Server Failure

The current marker was rewritten at local `2026-09-28 18:38:54` and reports:

- Service PID: `62896`
- Server version: `1.4.39+7e5162319a5474e57124b51d8d0d9d6e8b7d5470`
- Codex plugin version: `1.107.0`
- Plugin status: available
- Required namespaces include session log, TODO, requirements, triage, GraphRAG, memory, and failsafe.

The active failure is density-sensitive session-log mutation latency, not a marker, plugin-status, or triage outage.

Live post-restart timings from `C:\ProgramData\McpServer\logs\mcp-20260928.log`:

- `sessionlog_begin_turn`: 40.715 seconds
- `sessionlog_complete_turn`: 43.485 seconds
- `sessionlog_begin_turn`: 45.082 seconds
- A sparse hostile-review append took 3.813 seconds.

All three dense-session mutations succeeded. This is severe degradation, not a universal outage.

Deployed source is the clean worktree:

- Path: `F:\GitHub\McpServer\.worktrees\session-lifecycle`
- Branch: `grok/session-lifecycle`
- HEAD: `7e5162319a5474e57124b51d8d0d9d6e8b7d5470`

`SessionLogService.FindExistingSessionAsync` at lines 250 through 266 loads the tracked session plus every turn child collection through one multi-collection Include query. It has neither `AsSplitQuery()` nor `StorageCommandBudget` around materialization.

Causal conclusion:

- The unsplit tracked graph query is the strongest work-amplification explanation, at 92 percent confidence.
- Missing `StorageCommandBudget` is a containment and diagnostic defect. It allows the expensive read to run unbounded but does not create the work.
- Prior SQL Server error 1205 deadlocks corroborate persistence-path risk but do not prove they caused the three slow successful calls.
- Definitive causal proof requires an instrumented dense-session before/after test with `AsSplitQuery()` and budget enforcement.

The requirements-export and triage incident was successfully reported after service recovery:

- Report: `triage-report-5e6f748cba60455694dd02964c7e5072`
- Group: `triage-group-64e08b7f73b463eb`
- Routed workspace: `F:\GitHub\McpServer`
- Status when submitted: `collecting`
- Preserved failsafe: `.mcpServer\failsafe\Codex\workspaces\RjpcR2l0SHViXHZpY2Utc2hhcnA\pending\20260928T235515Z-triage_report-ebdc.yaml`

The current installed skill and schema both accept `markdown`. Do not claim the earlier export mismatch is repaired until an end-to-end plugin export proves it.

## McpServer Implementation Plan That Must Be Written and Approved

The plan file does not yet exist. The proposed path is:

`docs/plans/PLAN-REQRECOVERY-ENABLER-20260928.md`

The plan must be decision-complete and include the following gated slices.

### Slice 1: Prove and repair dense session logging

Write red tests first for:

- A dense tracked existing session containing every child collection.
- Query-shape proof that the graph is split and does not create a Cartesian collection join.
- Completion inside the five-second storage budget or a correctly classified retryable failure.
- Exact readback without duplicate children.
- Concurrent writes to the same session.
- SQL Server error 1205 classification.
- All eight supported plugin rows performing dense update and retry or recovery behavior.

Then independently review the red gate.

Implementation target:

- Add `AsSplitQuery()` to `FindExistingSessionAsync` while preserving tracking.
- Wrap graph materialization in `StorageCommandBudget.ExecuteAsync`.
- Preserve exact reconciliation and idempotent readback.
- Classify deadlock and budget exhaustion as retryable degraded or failed outcomes, never false success.

Run focused green tests, provider tests, plugin tests, and an independent hostile gate before proceeding.

### Slice 2: Add atomic requirements recovery

Required REST surface:

- `POST /mcpserver/requirements/recovery/dry-run`
- `POST /mcpserver/requirements/recovery/apply`
- `GET /mcpserver/requirements/recovery/{idempotencyKey}`

Required REPL surface:

- `workflow.requirements.planRecovery`
- `workflow.requirements.applyRecovery`
- `workflow.requirements.getRecovery`

The canonical request and receipt must bind:

- workspace ID
- target layer key
- product scope
- layer catalog version and hash
- scope start and end layer keys
- ordered requirement, AC, and mapping operations
- raw target-layer state including shadowed rows
- effective projection
- raw-state hash
- effective-state hash
- plan hash
- idempotency key
- result counts and IDs
- post-apply raw and effective hashes
- audit ID and receipt

Hash canonical UTF-8 JSON with SHA-256 after deterministic sorting by operation kind, requirement kind, requirement ID, AC ID, and mapping target.

Apply in one serializable provider transaction. Revalidate every precondition inside the transaction, reserve idempotency inside the same transaction, validate the final graph before the first mutation, and allow at most one concurrent plan to commit.

Persist a `RequirementsRecoveryRunEntity` with a unique `(WorkspaceId, IdempotencyKey)` key and migrations for SQLite, SQL Server, and PostgreSQL.

Required error contract:

- Invalid plan: 400, nonretryable
- Stale precondition: 409, retryable after a new dry run
- Idempotency conflict: 409, nonretryable
- Missing receipt: 404
- Backend unavailable, deadlock, or budget exhaustion: 503, retryable
- No partial mutation

### Slice 3: Synchronize every supported plugin

Canonical plugin content must remain under `plugins/core`. Do not hand-edit generated plugin copies.

Use the repository Nuke target:

`./build.ps1 SyncAgentPlugins --AgentPluginParent F:\GitHub`

The supported matrix is exactly:

- Codex
- Claude Code
- Claude Cowork
- Copilot
- Grok
- Cline
- Cline v2
- OpenCode

Run `./build.ps1 PluginSessionLogIntegration` plus every plugin's native Pester or Node build and test suite with zero failures and zero skips. Record source SHA, version parity, package hashes, and artifact hashes for every plugin.

### Slice 4: Validate and deploy McpServer

Required gates include:

- Focused tests with retained TRX
- Zero-skip SQLite, SQL Server, and PostgreSQL provider matrix
- `./build.ps1 Test`
- `./build.ps1 ValidateTraceability`
- `./build.ps1 PluginSessionLogIntegration`
- Independent hostile AGREE with accuracy and completeness at least 98

Deploy McpServer only through the Nuke `UpdateService` target. Never copy binaries manually. After deployment, wait for the marker rewrite, re-read it, verify plugin status, and rerun the dense live smoke test.

### Slice 5: Recover ViceSharp requirements

Only after the McpServer enabler is deployed and independently accepted:

- Rebuild the recovery ledger with per-field provenance, decision, reason, and merge behavior.
- Repair every one of the nine preview failures.
- Add immutable VICE commit and build hashes, ROM hashes, and named machine, media, and workload manifests.
- Rerun hostile preview review until it returns AGREE with both scores at least 98.
- Dry-run the atomic recovery.
- Present the exact plan hash and operations for operator comparison.
- Apply only after approval.
- Read back raw and effective state and rerun traceability.
- Preserve legacy UWP and Xbox requirements without implementing those platforms.

## xvic Lockstep Parity Plan

After requirements recovery is accepted, implement VIC-20 parity in gated order:

1. Prove the native xvic oracle and fail closed when native prerequisites are absent.
2. Version and hash the VICE build, ROMs, machine configuration, media, and workload manifests.
3. Compare every half-cycle rather than only periodic architectural snapshots.
4. Cover CPU registers, opcode and micro-op phase, bus address, data, read/write state, dummy cycles, IRQ/NMI pipeline, and stolen-cycle state.
5. Cover VIC-I, VIA, memory, IEC, input, storage, tape, audio, device side effects, reset, snapshot, determinism, and host pacing.
6. Repair clock, bus, and memory foundations before CPU instruction semantics.
7. Repair CPU before machine glue, VIC-I, VIA/IEC/input, audio/media, and UI.
8. Map every behavioral acceptance criterion to an executable differential test.
9. Do not silently return, skip, or pass when xvic, ROMs, media, hashes, or trace prerequisites are missing.

## VIC-20 Settings Visibility

Current source evidence:

- `src/ViceSharp.Avalonia/ViewModels/AttachPanelViewModel.cs`: `SelectedMachineProfile` updates settings but does not notify `IsVic20Selected`.
- `IsVic20Selected` derives from the selected profile ID.
- The refresh path explicitly notifies `IsVic20Selected`, but direct profile changes do not.
- `src/ViceSharp.Avalonia/Views/SettingsView.axaml` already binds VIC-20 memory and expansion-cart panels to `IsVisible="{Binding IsVic20Selected}"`.

Red tests must cover:

- C64 to VIC-20 to C64 immediate property notification and visibility.
- Hidden controls are not focusable or reachable.
- Direct set, host refresh, and persisted restore paths.
- Draft VIC-20 values survive hide and show transitions.
- Machine-family selection is centralized and tested.

Expected implementation is for the selected-profile setter to raise `OnPropertyChanged(nameof(IsVic20Selected))` when the machine family changes. Keep the existing view binding if behavioral tests prove it correct.

## Open TODOs Added During Recovery

- `PLAN-GRAPHRAG-001`, high priority: import `https://github.com/bdgscotland/c64-kb` into workspace GraphRAG. Ingestion has not run.
- `PLAN-RASPBIAN-001`, medium priority: add an ARM Linux desktop target with Nuke build and install targets for Raspbian. Do not duplicate this TODO.

## Receipts

- Historical requirement review: `docs/receipts/hv/20260928T180517Z-oldest-vice-requirements.response.jsonl`
- Recovery preview hostile gate: `docs/receipts/hv/20260928T203110Z-requirements-recovery-preview-gate.response.jsonl`
  - SHA-256: `78357CA7748838127F8919D951D54695C539FAD41F24A889AF461204D417229B`
- McpServer recovery requirements gate: `docs/receipts/hv/20260928T212001Z-mcp-recovery-requirements-gate.response.jsonl`
  - SHA-256: `4F10A7DC8E5F4F0D56B55555EE8B6246254CF365B28585B342190994DB602B81`
- Triage failure stop review: `docs/receipts/hv/20260928T220124Z-triage-failure-stop.response.jsonl`
  - SHA-256: `472B79DC8DBE632CA5DF88E48497610045251FE95B74829AB2E54F9BA31DE85C`
- Current failure diagnosis: `docs/receipts/hv/20260929T000145Z-actual-current-failure-diagnosis.response.jsonl`
  - SHA-256: `EFD512857D5172B145562833047D516885012673F282384DDDF416E7231B6A1A`
  - Verdict: AGREE
  - Counts: 11 PASS, 0 FAIL, 0 UNKNOWN
  - Accuracy: 99
  - Completeness: 99

## Resume Sequence

1. Re-read the current marker and verify its signature, health nonce, server version, and plugin status.
2. Query the live Codex session state. Reconcile any stale in-progress retry turn without deleting history.
3. Verify the preserved triage report and failsafe record.
4. Reproduce the dense-session latency with retained timing evidence.
5. Write `docs/plans/PLAN-REQRECOVERY-ENABLER-20260928.md` from the gated design above.
6. Run a full independent hostile review of that plan against workspace rules, all live requirements, all acceptance criteria, and the complete recovery scope.
7. Remediate every FAIL and UNKNOWN until the plan receives AGREE with accuracy and completeness at least 98.
8. Present the plan to the operator and stop for explicit approval.
9. After approval, execute BDPv4 strictly: requirements gate, red tests, hostile red gate, implementation, green tests, hostile green gate, full validation, Nuke deployment, post-deploy proof.
10. Do not begin ViceSharp product implementation until the requirements recovery enabler and recovered requirement set are deployed, applied, read back, traced, and independently accepted.

## Source-Control Boundary

- Before any commit or push, report branch, HEAD, origin, tracked dirty scope, untracked scope, and the exact files proposed for staging.
- Wait for explicit approval before committing or pushing this dirty workspace.
- Push only to GitHub `origin` unless the operator explicitly changes that instruction.
- Never use `git reset --hard`, destructive checkout, or broad cleanup against this workspace.
- Never use the dirty McpServer root or the dirty acceptance worktree for implementation when the clean deployed `session-lifecycle` worktree is available.
