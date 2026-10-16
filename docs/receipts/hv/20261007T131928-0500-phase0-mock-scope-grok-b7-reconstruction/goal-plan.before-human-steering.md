# Handoff completion plan

Status: decision-complete BDPv4 plan. No product code, requirement mutation, commit, or deploy until this plan is approved and its plan-review hostile gate returns AGREE with accuracy and completeness both at least 98.

Workspace: `F:\GitHub\vice-sharp`. Agent identity for execution: `Codex`. Independent hostile validation uses the alternating local reviewers defined below. Process: `F:\GitHub\McpServer\docs\Development-Process-draft-v4.md`.

## Operator resume directive: 2026-10-07

Reviewer rotation directive, 2026-10-07: alternate `grok-4.7` at `xhigh` and `claude-opus-5-5` at `max` for successive hostile reviews, using locally installed Grok and Claude Code CLIs. The already-running mock-green correction review (Grok PID 34256, receipt prefix `docs/receipts/hv/20261007T110000-0500-bdpv4-mock-green-correction`) retains its slot; the next review is Opus 5.5 max, then Grok 4.7 xhigh. Remediation re-reviews also alternate. Resuming an interrupted review is continuation of that same review, not a new slot. Preserve accurate reviewer identity and the rotation in MCP and the handoff. This directive supersedes older Grok-only restrictions and future "local Grok" gate wording in this plan and its linked plans; historical receipts retain their actual identity. It does not require two approvals for each gate, lower the 98/98 threshold, or permit cloud execution, hosted agents, web review, subagents, fallback models, or artificial turn/time limits. If the exact next model is unavailable, report that limitation rather than substitute silently.

Operator correction: "RED tests are not acceptable. BDPv4 requires mocking data to prove tests work to green before starting code." Write AC-covering unit tests first and prove them green against valid mocked data before production implementation. Deliberately incorrect mock data must prove the assertions detect defects; negative tests assert the expected rejection and keep the suite green. No expected-red production run or red-gate review is required or acceptable. Only after the mock-validation gate passes may production implementation begin. Then the full applicable suite for the current increment and all prior work must pass with zero failed and zero skipped tests before advancing. Follow this order for every remaining slice. Mock evidence is not real VICE parity evidence. This corrects the existing process, not product requirements, and overrides conflicting older profile, marker, or review wording.

The operator confirmed the F: relocation is complete and resumed normal development in `F:\GitHub\vice-sharp`. No further migration or partition work is needed. The immediate prerequisite is the independent Phase 0 recovery gate in `docs/plans/PLAN-VICE-IMMUTABLE-001.md`; its current state and receipts are recorded in the 2026-10-07 resume update at the top of `HANDOFF.md`. This update does not mark any phase, requirement, TODO, or the whole plan complete and does not change this plan's exit scope.

## What this plan finishes

The 2026-09-28 handoff in `HANDOFF.md` stopped at "write the enabler plan, hostile-review it, and wait." Later the same day and on 2026-09-29, recovery apply, mapping chase, and the four legacy FR renames were executed and committed. This plan finishes the handoff contract from that later state:

1. Reconcile the stale handoff with live server, git, TODO, and requirement state. Do not recreate finished work as new TODOs.
2. Accept or explicitly remediate the recovered ViceSharp requirement set. The preview hostile gate was DISAGREE. Applying the preview did not accept it.
3. Prove dense session-log behavior on the deployed server. The split-query fix is already in the deployed commit. Timing proof is not.
4. After Phase 2's final local Grok AGREE and a separate user approval to start, prove fail-closed `xvic` half-cycle parity for VIC-20, repairing foundations before CPU, and repairing CPU as one VICE-macro engine rather than more phase flags.
5. Fix VIC-20 settings visibility notification on the already-specified binding.
6. Replace `HANDOFF.md` with the post-audit truth.

`PLAN-GRAPHRAG-001` and `PLAN-RASPBIAN-001` stay open. They are not exit criteria of this plan. Do not create a second copy of either TODO.

## Verified this planning turn

- Marker signature matches `CC22BD31BC0ABD0675B1C45ACCFB7820F5621A60ECD74EC7DE0ABB21A7EBC492`.
- `GET /health?nonce=0e3936b1ad904fd891ee9150fe7a2305` echoed that nonce. Status Healthy. Storage reachable. Version `1.4.41+c195837ac888b6f02724221de6a040e63b3e6b47`. Plugin contract in the marker is `1.109.0`. Service PID `104272`. Marker written `2026-09-29T21:55:31.7166178Z`.
- ViceSharp git: branch `main`, HEAD `4dfdde53ae9f5daaef84d8dea1cea329bbd336d1`, origin `https://github.com/sharpninja/vice-sharp.git`, `origin/main...HEAD` is `0 0`, tracked dirty `0`, untracked `0`.
- That HEAD commit is `docs(receipts): legacy FR rename 4/4 success`. Parents include `6785f33` (rename plan), `f71cdfb` (mapping chase), `92e35d2` (recovery apply). The handoff's HEAD `687b03a` and its dirty-tree counts are historical.
- Deployed `SessionLogService.FindExistingSessionAsync` at `c195837` is an async method that calls `StorageCommandBudget.ExecuteAsync` around the include graph and ends that graph with `.AsSplitQuery()`. Deadlock 1205 throws `StorageGraphMaterializationException` and does not return null.
- McpServer checkout `F:\GitHub\McpServer` is branch `develop`, HEAD `6546a59f2baad45d99e717bb8f20b56e06d5911f`, 136 status lines. Its working-tree `FindExistingSessionAsync` is the old unsplit expression-bodied query. That tree is not the implementation base.
- Open ViceSharp TODOs returned by `todo_list` with `done: false`: 36. Still open: `PLAN-GRAPHRAG-001`, `PLAN-RASPBIAN-001`, `PLAN-ARCHVIC20-001`, `PLAN-C64SET-001`, `PLAN-GROKCOMPLETION-001`.
- `docs/plans/PLAN-REQRECOVERY-ENABLER-20260928.md` does not exist.
- `SelectedMachineProfile` sets through `SetSettingsProperty`, which raises only that property. `IsVic20Selected` is raised from `ApplySettingsFromHost` (line 1185). `RestoreSettings` raises `SelectedMachineProfile` and does not raise `IsVic20Selected`. The profile assign inside `RefreshSettingsAsync` (about line 1106) also raises only `SelectedMachineProfile`. `SettingsView.axaml` already binds visibility to `IsVic20Selected`. Structural tests in `Vic20SettingsUiSurfaceTests` check that the binding text exists. They do not check the setter notification.
- In-progress session query `turnStatus=in_progress` returned `totalCount` 97. At the time of that historical query, the Grok execution turn was session `GrokCode-20260929T215620Z-plugin-session`, request `req-20260929T215900Z-prompt-eb9d`, and a ClaudeCode turn was also open. Those are continuity evidence, not the active execution identity. Do not reuse, complete, or delete either historical turn, and do not bulk-close the other historical in-progress sessions.
- Codex session `Codex-20260927T203427Z-plugin-session` records a completed architecture audit: stop one-sample CPU timing patches; the managed core must return to one VICE-macro per-cycle micro-op engine before Wolf64 or xvic parity resumes. Receipt cited there: `TestResults/cpu-visible-architecture-audit.trx`, 140 passed, 8 failed, 0 skipped. That audit is input to the CPU slice. It is not a done claim for xvic.

## Not re-verified this planning turn

Treat these as receipt claims until Phase 0 reads them live:

- Effective counts after the rename receipt (`legacy-fr-rename-20260929T085555`, 2026-09-29 08:56:40 CT): FR 232, TR 138, TEST 140, MAP 231.
- Clean recovery idempotency key `slice5-apply-clean-18028995ae17`, payload hash `d858d4e30feeab57a4f652ee9e8dc6e3ef70a34e8674e52e7ba5488cfb727164`, apply/GET/replay/409 conflict passed on server `1.0.0+deda7d6a`. Plugin `getRecovery` failed on that server.
- Mapping chase applied 140/140 mapping writes. MAP 115 to 231. Eight short TRs created. Four short TRs updated. Four empty TR bodies filled from preview Before. Three wrong-type TEST ids removed from the TR collection.
- Four legacy ids renamed in place of content: `ARCH-TRUEDRIVE-1541-002` to `FR-ARCH-TRUEDRIVE-1541-002`, `BACKFILL-MEDIA-001` to `FR-BACKFILL-MEDIA-001`, `BACKFILL-VIDEO-001` to `FR-BACKFILL-VIDEO-001`, `RUNTIME-TAPE-002` to `FR-RUNTIME-TAPE-002`. Net count change 0. Mutation PUT on each new id succeeded.
- Preview hostile gate `docs/receipts/hv/20260928T203110Z-requirements-recovery-preview-gate.response.jsonl`: DISAGREE, accuracy 99, completeness 99, 15 PASS, 9 FAIL, 0 UNKNOWN. The nine FAIL themes in the handoff were not re-scored after apply.
- Dense-session timings in the handoff (about 40 to 45 seconds) were measured on server `1.4.39+7e51623`, before `c195837`.

## Locked decisions

1. Handoff ingest mode is `DraftOnly` on path `HANDOFF.md`. Do not call `handoff_approve` with `approved: true`. The handoff text would recreate a plan-before-apply story that the git history already passed. Approval of this completion plan is the operator gate. It is not approval to create TODOs from the stale handoff.
2. Do not re-apply the 442-operation preview. Do not POST a different body under `slice5-apply-clean-18028995ae17`. A conflicting body on that key must stay 409. New requirement writes use a new idempotency key only after Phase 1 lists the exact field delta and the operator approves that delta in a separate message. A requirement text change is not a bugfix.
3. Do not recreate the four legacy FR ids. Do not delete the four `FR-*` replacements.
4. McpServer code changes, if Phase 2 or Phase 1 guardrail work needs them, start from a clean worktree of `c195837ac888b6f02724221de6a040e63b3e6b47` or a later deployed SHA that still contains the split `FindExistingSessionAsync`. Do not edit `F:\GitHub\McpServer` develop while it has 136 dirty paths. Do not copy binaries. Deploy only with Nuke `UpdateService` from that clean worktree, and only after that slice's hostile green gate.
5. If the Phase 2 live smoke completes inside five seconds and readback matches, do not change session-log code.
6. xvic product work (Phase 3) starts only after both of these: the final Phase 2 local Grok verdict is AGREE at accuracy >= 98 and completeness >= 98, and the user sends an explicit approval to start Phase 3 after that verdict is shown. Approval of this plan is not approval to start Phase 3. Phase 1 AGREE, a smoke file, and green tests do not start Phase 3. If `FR-VIC-001`, `TR-CYCLE-001`, or `FR-CPU-002` is still a placeholder or has zero acceptance criteria, put that delta in the Phase 3 approval request and do not write xvic tests until the user approves the text and the start.
7. CPU repair, when it is reached, does not add phase booleans, predecessor exceptions, or one-opcode host patches. The rejected alternative is another `Mos6502.cs` flag. The locked approach is one per-cycle engine whose phases are the VICE macros in `native/vice/vice/src/6510dtvcore.c` (`CLK_INC`, `INC_PC`, `JUMP`, `STORE`, `GET_ABS`, `GET_ZERO`, `GET_IND_Y`, `GET_ABS_X`, `DO_INTERRUPT`, `BRANCH`). Before any CPU edit, answer both VICE-first questions: how the managed core diverged, and how it returns to VICE. Blast radius: `src/ViceSharp.Chips/Cpu/Mos6502.cs`, opcode files, `SystemClock` steal/BA, snapshot staging, `LockstepValidator`, C64 idle lockstep, VIC-20, and every test that pins the hybrid phase flags. This is an architectural break. Approval of this plan approves that approach. It does not approve starting the rewrite before Phases 0, 1, and the oracle slice are green.
8. Every CPU change keeps C64 idle `First10000` and `First100000` green. Wolf64 disk mismatch work stays out of this plan except as that regression pin. Do not resume Wolf64 sample patches from this plan.
9. VIC-20 settings visibility is a bug in already-specified behavior (`FR-UISETVIS-001`, `PLAN-C64SET-001`). After this plan is approved, that slice executes without a second approval. Do not mark `PLAN-C64SET-001` done. Its remaining dropdown, warp, and remote-control acceptance stays open.
10. Missing `xvic`, ROM, hash, or trace prerequisites fail the test. They do not skip and they do not pass. A Byrd exit is failed 0 and skipped 0 in the named filter. A missing oracle blocks the phase.
11. No commit and no push until a later message that names the files. Before that message, report branch, HEAD, origin, tracked dirty paths, untracked paths, and the exact stage set. Push only to GitHub `origin`.
12. Do not mark any TODO, FR, TR, TEST, or plan step done without hostile AGREE, both scores at least 98, request jsonl, response jsonl, and the full verdict stored on the reviewer session turn. A sub-98 score is DISAGREE.
13. Legacy UWP and Xbox requirement rows stay. This plan does not implement those platforms and does not delete those rows.
14. PowerShell only, through the `pwsh` MCP tools. No Python. JSON and YAML come from native objects. Session, TODO, and requirements writes go through `sessionlog_*`, `todo_*`, `requirements_*`, and `handoff_*`.
15. Continue the active Codex plugin turn for implementation. Do not reuse a historical reviewer execution turn. Each hostile gate launches the next reviewer in the rotation and records its own correctly identified MCP session/turn. A completed historical turn is never overwritten.
16. Alternate local Grok `grok-4.7` at `xhigh` and local Claude Code `claude-opus-5-5` at `max`, including remediation reviews. No cloud runner, hosted agent, web search, delegated subagent, other model, or remote CI may substitute for the designated reviewer. Do not set `--max-turns`, a wall-clock timeout, or any other artificial turn or time limit; let the reviewer return a terminal result unless the operator explicitly stops it.

## Hostile review runner

Invoke both runners only through PowerShell.MCP from `F:\GitHub\vice-sharp`, with web search, subagents, and product edits prohibited. Grok launch shape:

```pwsh
& 'C:\Users\kingd\.grok\bin\grok.exe' --prompt-file $promptPath --model 'grok-4.7' --reasoning-effort xhigh --output-format streaming-json --permission-mode bypassPermissions --disable-web-search --no-subagents --disallowed-tools Edit,Write,WebSearch,WebFetch --verbatim
```

Claude Code launch shape, with `$claudePath` resolved from the installed local `claude.exe` and verified in the request receipt:

```pwsh
Get-Content -LiteralPath $promptPath -Raw | & $claudePath --print --model claude-opus-5-5 --effort max --output-format stream-json --verbose --permission-mode bypassPermissions --disallowed-tools Edit,Write,WebSearch,WebFetch,Agent,Task,Bash,PowerShell
```

Shell work inside either reviewer must use PowerShell.MCP. Do not use Claude cloud/background-environment or ultrareview commands. Confirm the exact requested model and effort from the launch and runtime receipts; do not enable fallback models.

The parent writes the request jsonl first from a native PowerShell object at `docs/receipts/hv/{local-sortable}-{gate}.request.jsonl`. It records the sortable local timestamp and UTC timestamp, workspace, actual reviewer identity, model and effort, exact launch command, full prompt, evidence paths, rotation slot, gate name, and the fact that there is no turn cap or artificial timeout. The prompt file is retained beside the request.

The brief comes from `C:\Users\kingd\.grok\skills\hostile-validator\SKILL.md`. Its first action is `add-profile`. Surfaces A through D remain mandatory. The reviewer is read-only for product and plan files. It may write only durable HV receipt files and its own MCP session-log turn.

The reviewer must use its agent-appropriate supported MCP plugin session-log surface and real Pascal-case identity. If primary MCP persistence is unavailable, the prompt explicitly requires the supported plugin v4 failsafe. The reviewer must not handwrite or edit failsafe YAML. It must run `workflow.failsafe.status`, prove `quarantineCount: 0`, identify every queued record used for the review by exact absolute path and SHA-256, label the disposition `failsafe-pending`, and state `PrimaryReadback: false`. The full verdict prose, every PASS/FAIL/UNKNOWN finding, both numeric scores, structured verdict, and request/response jsonl paths must be present in the queued `client.SessionLog.SubmitAsync` payload. After MCP recovery, the parent must run `workflow.failsafe.drain` and prove primary `workflow.sessionlog.queryHistory` readback before any goal, TODO, requirement, phase, or whole-plan done-state change. Failsafe proves queued preservation, not primary persistence.

The parent retains the complete reviewer machine stream as `docs/receipts/hv/{local-sortable}-{gate}.response.jsonl`, plus the raw terminal JSON and a human-readable verdict. A one-line summary is not the response file. AGREE requires accuracy and completeness both at least 98, zero FAIL, zero UNKNOWN, durable request and response jsonl, and the full reviewer result either read back from primary MCP or verifiably queued in v4 failsafe under the operator's explicit outage exception. The parent displays every FAIL, UNKNOWN, sub-98 score, missing receipt, quarantine entry, or persistence gap and does not mark anything done until the later drain and primary readback succeed.

## Phase 0. Trust, ingest, and live baseline

Class: operator reconciliation. No product code.

Steps:

1. Update the active Codex plugin turn for this local chat. Record this plan path and log the Phase 0 conclusions as design decisions, not only actions. If primary MCP persistence is unavailable, use the supported plugin v4 failsafe, prove the queued record and zero quarantine, and do not claim primary persistence until drain and query-history readback succeed.
2. Re-read `AGENTS-README-FIRST.yaml`. Recompute the marker HMAC. Call `/health` with a new nonce. Record version, PID, and plugin version from the marker. If signature or nonce fails, stop MCP use and report `MCP_UNTRUSTED`.
3. `handoff_ingest` with `workspacePath` `F:\GitHub\vice-sharp`, `sourceKind` `Path`, `path` `HANDOFF.md`, `mode` `DraftOnly`. Inspect the run with `handoff_get`. Leave it unapproved. Write the run id into the session turn.
4. Re-query effective requirements counts and these ids: `FR-VIC-001`, `TR-CYCLE-001`, `FR-CPU-002`, `FR-VIC-006`, `FR-VIC-010`, `FR-UISETVIS-001`, `FR-MCP-REQRECOVERY-001`, `TR-MCP-REQRECOVERY-001`, `TEST-MCP-REQRECOVERY-001`. Save the raw responses under `docs/receipts/requirements-recovery/acceptance-20260929/`.
5. GET recovery run `slice5-apply-clean-18028995ae17` through the requirements tool surface. Expected from the receipt: status applied, hash `d858d4e30feeab57a4f652ee9e8dc6e3ef70a34e8674e52e7ba5488cfb727164`. If the new server returns 404, stop. The apply did not survive `1.4.41`. That is a blocker, not a reason to re-apply blindly.
6. Call plugin `workflow.requirements.getRecovery` for that same key. The 2026-09-29 receipt says this path exited 1 while HTTP GET worked. Record pass or the exact failure. A continued plugin failure is a McpServer defect to triage, then fix only in Phase 2's clean worktree rules if the operator still wants it in this plan. Do not block the requirement audit on it. Do not claim the plugin path is fixed.
7. Compare live counts to FR 232, TR 138, TEST 140, MAP 231. Any difference stops the phase. Report the delta. Do not "correct" it inside Phase 0.
8. Confirm the four `FR-*` ids GET 200 and the four legacy ids GET 404.
9. Leave the 97 in-progress sessions in place. The only session mutation in this phase is the current Grok turn.

Exit: a receipt file `docs/receipts/requirements-recovery/acceptance-20260929/phase0-baseline.json` built by `ConvertTo-Json` from a native object, plus the session-log actions. No hostile gate yet. No requirement writes.

## Phase 0b. Hostile review of this plan

Before Phase 1 writes any ledger judgment into a requirement, and before any code edit, run the local Grok runner above. Gate name: `handoff-completion-plan`.

- Surface A: the verified facts in this plan (HEAD, health version, deployed `AsSplitQuery`, open TODO ids, setter notification gap, and local Grok launch identity `grok-4.7` at `xhigh` with no turn or time cap).
- Surface B: workspace rules. This phase is still planning. Do not FAIL it for tests that later phases name and have not started. Those tests require mock-validated green gates before production implementation.
- Surface C: N/A for the ingest itself. Apply surface C to any claim that a requirement or plan step is already done. Recovery apply is not "accepted." xvic is not done. `PLAN-C64SET-001` is not done.
- Surface D: this plan's exit criteria and the handoff resume sequence.
- Jsonl paths: `docs/receipts/hv/{local-sortable}-handoff-completion-plan.request.jsonl` and `.response.jsonl`.
- The local Grok reviewer stores the entire verdict on its own MCP session turn.
- AGREE only at accuracy >= 98 and completeness >= 98 with zero FAIL and UNKNOWN. Otherwise remediate this plan text and use the next local reviewer in the rotation. Do not start Phase 1 mutations or Phase 3.

Phase 0's read-only baseline may run before this review so the reviewer has live counts. Phase 0 must not change requirement rows.

## Phase 1. Recovery acceptance audit

Class: project requirements. No ViceSharp emulator code. No second recovery apply.

Write `docs/receipts/requirements-recovery/acceptance-20260929/nine-defect-ledger.json` with one object per original FAIL:

1. Empty TR bodies, placeholder TRs, and lost acceptance criteria.
2. Inferred acceptance criteria that state implementation mechanism.
3. Noncanonical ids and wrong type prefixes.
4. The four empty TR bodies and two placeholder TRs named by the preview gate.
5. Narrowing and acceptance-criteria loss against the April 2026 oracle (root commit `d1f7175`, tarballs under `docs/receipts/requirements-recovery/20260928T195211Z`).
6. Mapping existence versus semantic appropriateness and per-AC executable VICE coverage. Presence of 231 mappings is not coverage proof.
7. Missing immutable VICE commit, VICE build hash, ROM hashes, and named machine, media, and workload manifests.
8. Whole-record ledger versus per-field provenance, decision, reason, and merge.
9. Guardrails that still allow empty bodies, placeholders, malformed ids, misclassification, AC loss, or narrowing through the live recovery API.

For each item set `status` to `closed`, `open`, or `needs-operator`. `closed` requires a live GET or a test name in the receipt. `needs-operator` is mandatory for item 2 and for any proposal to rewrite FR/TR body text, including restoring `FR-VIC-001` and `TR-CYCLE-001` from the April 2026 wording. Do not perform that rewrite in this phase.

Item 7's missing manifests are open. The closure of item 7 is a checked-in manifest, not a code change:

- Path: `docs/requirements/oracles/vic20-xvic-manifest.json`
- Fields: `viceCommit`, `viceBuildHash`, `romHashes` (name, path, sha256), `machine` (`xvic` PAL and NTSC profiles), `media` (named disk/tape/cartridge files or an explicit empty set), `workloads` (named programs and the cycle window each one must match).
- Hashes are computed on the machine that holds the ROMs. If `VICESHARP_ROM_PATH` or `xvic` is absent, item 7 stays `open` and Phase 3 cannot exit. Do not invent hashes.

Item 8 closure is a supplemental provenance file next to the applied preview, one row per changed field: requirement id, field, old hash, new hash, source commit, decision, reason. It does not mutate the store. If a row's only honest decision is "applied from a DISAGREE preview," mark it `needs-operator` rather than inventing a rationale.

Item 9 stays a McpServer test slice on the clean worktree if the live API still accepts an empty body or a placeholder. Tests first, named below. If the live API already rejects those cases, record the probe status codes and do not add code.

Mock-validated tests before production implementation, only if item 9 is still open, in the McpServer clean worktree:

- `RequirementsRecoveryGuardrailTests.RejectsEmptyBody`
- `RequirementsRecoveryGuardrailTests.RejectsPlaceholderBody`
- `RequirementsRecoveryGuardrailTests.RejectsMalformedId`
- `RequirementsRecoveryGuardrailTests.RejectsTypePrefixMismatch`
- `RequirementsRecoveryGuardrailTests.RejectsAcceptanceCriteriaRemoval`
- Mock contract: an illegal fixture receives HTTP 400, nonretryable, no partial mutation, and unchanged readback hash. A deliberately incorrect mock returning HTTP 200 or changing the hash must be caught by assertions, with negative tests asserting that rejection. All mock-validation tests must pass before production code starts; the same contract must pass against real code before the implementation gate exits.

Phase 1 exit:

- Ledger file exists.
- Every `closed` row cites a live artifact.
- Every `needs-operator` row is listed in the session turn and in the user report.
- local Grok hostile review `docs/receipts/hv/{utc}-recovery-acceptance-audit` returns AGREE at >= 98 and >= 98.
- No requirement row was written in this phase unless the operator already approved a specific delta during execution. Default is zero writes.

If any foundational id (`FR-VIC-001`, `TR-CYCLE-001`, `FR-CPU-002`) is still a placeholder or still has zero acceptance criteria, carry the proposed restored text into the Phase 3 approval request. Do not write xvic tests against that placeholder. Phase 3 still waits for the Phase 2 gate below.

## Phase 2. Dense session-log proof

Class: McpServer behavior. Run only a measurement first.

Smoke, against the live server, on a session that already has every child collection the handoff named (turns, actions, tags, context, dialog, commits and files, string lists, session tags):

- `sessionlog_begin_turn` and `sessionlog_complete_turn` each finish in under 5 seconds, or the server returns a classified retryable failure (deadlock or budget), never a false success.
- Immediate readback equals the written turn: no duplicate children, no dropped children.
- Record start, end, elapsed milliseconds, version `1.4.41+c195837...` or newer, and the session id in `docs/receipts/requirements-recovery/acceptance-20260929/dense-session-smoke.json`.

If that smoke passes, Phase 2 is evidence only. No code change. Run the local Grok runner on the smoke receipt. Do not claim the 40-second defect is gone on any server other than the one measured.

If the smoke exceeds 5 seconds or readback is wrong, stop using the dirty develop tree and use a clean worktree from the deployed SHA. Before production implementation, write these AC-covering tests and prove them green against mocks, including negative cases that assert detection of incorrect mock data:

- Dense existing session materializes through a split query (assert the executed command text or the EF split flag the project already uses; do not only assert elapsed time).
- Elapsed budget: under 5 seconds or a typed retryable exception.
- Readback identity, including child counts.
- Two concurrent writers to the same session: one commits, the other retries or fails classified. No silent merge.
- SQL 1205 maps to the retryable deadlock exception, not to "session missing."
- One dense update through each supported plugin row: Codex, Claude Code, Claude Cowork, Copilot, Grok, Cline, Cline v2, OpenCode. Zero failures, zero skips.

Green: those tests, then `./build.ps1 Test`, `./build.ps1 PluginSessionLogIntegration`, provider matrix for SQLite, SQL Server, and PostgreSQL with zero skips. Then the local Grok green gate. Then `./build.ps1 UpdateService` only. After deploy, wait for the marker rewrite, re-read it, repeat the smoke.

Plugin `getRecovery` failure from Phase 0 is fixed in this same clean worktree only when the smoke branch is already required. If the smoke passes and `getRecovery` still fails, file triage and stop that fix for a separate approval. Do not hide it inside an unrelated commit.

## Mandatory amendment: immutable native VICE target

Operator correction, 2026-10-03: native VICE is the immutable behavioral target. Code under `native/vice` may not be edited, patched, generated into, or behaviorally changed. External wrappers are allowed only outside the submodule and must preserve target semantics.

This amendment supersedes every conflicting sentence in this plan, including any instruction to fix the native reference. The authoritative BDPv4 boundary plan is `F:\GitHub\vice-sharp\docs\plans\PLAN-VICE-IMMUTABLE-001.md`, backed by `FR-VICE-001`, `TR-VICE-ORACLE-001`, and `TEST-VICE-001`.

`TR-VSFLOCKSTEP-RESUME-001` remains active and retains its legacy v1.2 resume behavior. Its implementation boundary is rehomed outside `native/vice`: a dedicated compatibility adapter normalizes validated v1.2 and v1.3 `MAINCPU` modules in a temporary copy for the unmodified v1.4 reader, while v1.4 passes through byte-for-byte. The current mandate to apply `native/patches/vice-shim-runtime.patch` is superseded and rejected; that patch is diagnostic preservation evidence only and may never participate in a build.

Observed blocker at amendment time:

- The gitlink and checkout both identify `cc67418cba58e71d8e70fd353e9de47199ad2075`.
- The checkout contains 12 modified tracked source files, 1,148 untracked entries, and the injected source header `vice/src/vice-shim-runtime.h`.
- Native-backed evidence from binaries built with those modifications is diagnostic only and cannot prove immutable-target parity.
- The in-flight corrected Slice 3c hostile review was stopped before producing output because its implementation premise was stale.
- The long-running Phase 3 baseline may finish, but its result is quarantined from immutable-target acceptance.

Phase 3c is closed until the operator approves `PLAN-VICE-IMMUTABLE-001.md`, its plan-review hostile gate agrees at accuracy and completeness of at least 98 with no FAIL or UNKNOWN, and its preservation, mock-validated green integrity-test, wrapper-feasibility, and migration gates are complete. Missing native observability is a blocker. It is never authorization to modify VICE or infer the missing value.


## Phase 3. xvic parity

Start only after the final Phase 2 local Grok verdict is AGREE (accuracy >= 98 and completeness >= 98) and the user explicitly approves starting Phase 3. The final Phase 2 verdict is the smoke-receipt review when no code changed. When code changed, it is the post-`UpdateService` smoke review, after the green-gate AGREE, the Nuke deploy, the marker rewrite, and the repeated smoke. Show that verdict, then stop. Do not open slice 3a in the same turn.

One slice at a time after that approval. Each slice: AC-covering tests green against valid mocks and negative tests proving incorrect mocks are rejected, local Grok mock-validation gate, production implementation, full applicable current-plus-prior suite with failed 0 and skipped 0, then local Grok implementation gate. Keep the C64 idle pin green if CPU or clock code changed. A focused filter is diagnostic and does not replace the full applicable gate.

### Slice 3a. Fail-closed oracle

New test file: `tests/ViceSharp.TestHarness/Vic20/XvicOraclePrerequisiteTests.cs`.

Cases, each asserted as a rejected prerequisite in green mock-based unit tests, never hidden by `Skip`:

- `xvic` binary or `vice_xvic` native library missing.
- VIC-20 ROM set missing under `VICESHARP_ROM_PATH`.
- Manifest file missing.
- Manifest hash does not match the file on disk.
- Workload named in the manifest is missing.

Before implementation, identify the current helper. The planning baseline records `MachineTestFactory` throwing when ROMs are missing and `CompletionTestManifestTests` using the literal `sha256-vice-xvic-final`; recheck before changing either. Validate the tests with valid prerequisite mocks and deliberately incorrect mocks that claim success or skip without an oracle. Negative tests must assert the expected rejection and remain green. Then verify the real paths without weakening a throw into a skip. Missing prerequisites in a real parity run block that gate; mock success never establishes oracle availability.

Command:

```pwsh
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~XvicOraclePrerequisiteTests"
```

### Slice 3b. Manifest is the contract input

The manifest from Phase 1 item 7 is the only oracle identity the differential tests accept. Tests load it. They do not hard-code a placeholder hash. A mismatch fails.

### Slice 3c. Half-cycle comparator

Extend `tests/ViceSharp.TestHarness/LockstepValidator.cs`. Do not add a second comparator.

Compared on every half-cycle, managed versus native `xvic`:

- CPU A, X, Y, S, P, PC
- opcode and micro-op phase
- bus address, data, read or write
- dummy cycle versus real access
- IRQ and NMI pipeline stage
- stolen-cycle / BA state

The current comparator's register-and-PC equality is not this slice's exit. First validate the test contract against mocks: matching registers and bus data are accepted, while matching registers with mismatched bus data produce the exact divergence cycle. These tests must be green before production implementation. Real parity then requires a real bus event obtained by an accepted external wrapper around immutable VICE. If the wrapper cannot expose the event without changing `native/vice`, stop at a named blocker. Wrapper changes belong outside `native/vice` and must be tested fail-closed when the capability is absent. Do not invent bus values for native-backed evidence.

Repair order inside later mock-validation/implementation pairs, each pair its own diagnostic filter, full applicable suite, and local Grok gates:

1. Clock, bus, and memory foundations (`SystemClock`, PLA/banking, VIC-20 memory map). No instruction-semantics edits in this pair.
2. CPU micro-op engine, full 256 opcodes, decimal ADC/SBC, NMOS indirect JMP page wrap, JAM. This is decision 7. Pin each closed mismatch with a named test in `CpuVisiblePcTimingTests` before widening the VIC-20 workload. After each pair, run the C64 idle filters and keep them green.
3. Machine glue.
4. VIC-I.
5. VIA, IEC, input, storage, tape.
6. Audio, device side effects, reset, snapshot, determinism, host pacing.

Commands for the idle pin:

```pwsh
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~CpuVisiblePcTimingTests"
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~First10000|FullyQualifiedName~First100000"
```

Confirm the idle test names with a search before the first CPU edit. If the filter matches nothing, stop. Do not substitute a different suite and call the pin green.

Snapshot rule: a snapshot resume continues in-flight pipeline state. It does not restart the managed CPU mid-instruction. First prove the multi-cycle-opcode resume test green with mocked next-half-cycle data and negative cases for reset or lost pipeline state. After implementation, compare the real resumed next half-cycle to immutable `xvic`.

Native steal rule: the external wrapper must observe the same immutable VICE bookkeeping `maincpu_steal_cycles` performs for the VIC-20 CPU core. First prove the stolen-cycle test green against mock data, including asserted rejection of an omitted cycle. After implementation, prove the real observation. If the wrapper cannot observe it without changing `native/vice`, stop at a named blocker; never fix or alter the native reference.

Phase 3 is not one PR-sized edit. Stop after each numbered pair and report the filter counts. Do not open pair 2 while pair 1 has a failing or skipped test.

## Phase 4. VIC-20 settings visibility

Starts only under the same gate as Phase 3: final Phase 2 local Grok AGREE, then an explicit user approval. It may run in parallel with Phase 3 after that approval only when it does not touch CPU, clock, bus, or native shim files. It does not wait on xvic lockstep being finished.

Test file, validated green against mocks before production changes: `tests/ViceSharp.TestHarness/Ui/Vic20SettingsVisibilityNotificationTests.cs`.

Cases:

- Construct the settings view model with C64 and VIC-20 profiles available. Set `SelectedMachineProfile` from C64 to `vic20`. Assert a property-changed notification whose name is `IsVic20Selected`, then back to C64, same notification. Validate the assertion first with correct mocked notifications and a negative test detecting an omitted notification; then verify the production path.
- Call the host-refresh profile assignment path (the assign around line 1106) and assert the same notification even when the later settings GET fails.
- Call `RestoreSettings` across a family change and assert the same notification.
- Set VIC-20 memory preset and expansion-cart kind, switch to C64, switch back to VIC-20. Assert the draft preset and cart kind are unchanged. Read the current fields before writing the assertion. Prove the tests green with preserved mock values and asserted detection of incorrectly cleared mock values before any production correction. Then verify preservation in the real view model.
- Assert hidden-family controls are not focusable by testing the visibility flag the view binds (`IsVic20Selected` false implies the VIC-20 panels are not visible). A headless unit test cannot press Tab through the Avalonia window. Do not claim keyboard focus beyond that flag unless a later RemoteControl launch actually tabs the window. `PLAN-REMOTECTRL-001` visual launch stays out of this slice.

Implementation only after the mock-validation suite is green and its local Grok gate agrees:

- In the `SelectedMachineProfile` setter, when the machine family changes, raise `OnPropertyChanged(nameof(IsVic20Selected))` after `SetSettingsProperty` returns true.
- Raise the same notification from `RestoreSettings` and from the profile assign in `RefreshSettingsAsync`.
- Leave the `ApplySettingsFromHost` notification in place.
- Leave `SettingsView.axaml` bindings in place.
- Read `XboxSettingsViewModel` for the same setter gap. If it has the gap, extend the same test class to that type and fix it the same way. If it already notifies, do not touch it.

Green command:

```pwsh
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20SettingsVisibilityNotificationTests|FullyQualifiedName~Vic20SettingsUiSurfaceTests"
```

Failed 0, skipped 0. Then the local Grok green gate. Then update `PLAN-C64SET-001` remaining text to say the visibility notification slice is done, and leave the TODO `done: false`.

## Phase 5. Replace the handoff

After Phase 1 AGREE, and after whichever of Phases 2, 3, and 4 have actually finished, rewrite `HANDOFF.md` so it matches live HEAD, server version, requirement counts, open TODOs, and the next unfinished pair. Delete the stale "no apply has happened" instructions by replacing the file's content. Do not leave both stories in the file.

If execution stops at an earlier phase, the handoff rewrite still happens for the phases that finished, and it names the unfinished phase as the resume point. Do not claim xvic parity in the new handoff unless Phase 3's last pair local Grok gate agreed.

No commit in this phase unless the operator has given the separate commit order in decision 11.

## Out of scope

- `PLAN-GRAPHRAG-001` ingestion of `https://github.com/bdgscotland/c64-kb`.
- `PLAN-RASPBIAN-001` ARM Linux Nuke targets.
- Wolf64 disk `FirstMismatchCycle` campaign, except the C64 idle pin.
- `BUG-SIDAUDIO-001`, `BUG-DRV1540-001`, `PLAN-DRIVE1540-001`, `PLAN-UIREDESIGN-001` leftovers, `PLAN-REMOTECTRL-001` visual launch, `PLAN-CARTRAMLINK-001`, `PLAN-AUDIOEQ-001`, `PLAN-IECDEBUG-001`.
- Marking `FR-MCP-REQRECOVERY-001` or its TR/TEST satisfied. Their criteria stay pending until an executable proof says otherwise.
- Using the dirty McpServer develop tree as a deploy source.
- `git reset --hard`, destructive checkout, or broad cleanup.
- Manual service binary copy.

## Exit of the whole plan

The plan is complete only when all of the following are true:

- Phase 0 baseline matches the live store or the mismatch is recorded as a blocker and work stopped.
- Phase 0b and Phase 1 local Grok verdicts are AGREE at >= 98 and >= 98, with jsonl on disk and the full verdict in the reviewer's own MCP session turn.
- Foundational FR/TR text is either accepted as-is with criteria, or the operator approved the rewrite and that rewrite was read back.
- Phase 2 smoke receipt exists. Code changed only if the smoke failed, and then only through Nuke after green gates.
- Phase 3 stayed closed until the final Phase 2 local Grok AGREE and a later user message approving the start. After that start, Phase 3 either finished every repair pair with failed 0 and skipped 0, or stopped at a named pair with a failing oracle prerequisite recorded as the blocker.
- Phase 4 filter is green and `PLAN-C64SET-001` is still not done.
- `FR-VICE-001`, `TR-VICE-ORACLE-001`, and `TEST-VICE-001` are satisfied only with a pinned, clean `native/vice` checkout before and after validation, wrapper provenance receipts, failed 0, and skipped 0. Any patched-native result remains diagnostic only.
- `HANDOFF.md` describes that end state.
- No commit or push occurred without a separate order.

## Session logging during execution

On every phase exit, append actions and at least one design decision (judgment, consequence, affected ids or files) to the active Codex plugin turn. When primary MCP persistence is unavailable, use the supported plugin v4 failsafe and record the exact pending path, SHA-256, status count, and quarantine count; later drain and read back the primary turn before any done-state change. Complete the turn only when this plan stops for the operator or the whole-plan exit is met. Do not report zero decisions.
