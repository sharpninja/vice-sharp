# ViceSharp Handoff

Generated local: 2026-09-29 18:50:00

This file replaces the 2026-09-28 handoff. That older file said recovery had not been applied and that product work was waiting on a plan. Both statements are stale.

## Current State

- Plan: `C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\01a0ef2b-2331-70d1-9c65-0ef12b15d09a\plan.md`
- Phase 0 recorded the live baseline. Phase 2 recorded timings for one dense session. On 2026-09-30 the operator said "Finish this" after "It does not accept the requirement set." Ledger item 2 stays needs-operator. That sentence is not treated as closing mechanism wording. No requirement status was set to completed, and no TODO was marked done. Codex `gpt-6-astra` at effort `xhigh` reviewed that order at 2026-09-30T15:34:43Z and returned DISAGREE, accuracy 95, completeness 90, pass 2, fail 9, unknown 1. The same model and effort reviewed the updated packet at 2026-09-30T16:06:19Z and returned DISAGREE, accuracy 97, completeness 95, pass 2, fail 9, unknown 1. Thread `01a0f308-7f62-72f0-89c7-da43b8460cff`. Receipt: `docs/receipts/hv/20260930T155654Z-requirement-set-acceptance-r2.response.jsonl`. The same model and effort reviewed the audit packet at 2026-09-30T16:50:56Z and returned DISAGREE, accuracy 96, completeness 92. Receipt: `docs/receipts/hv/20260930T164102Z-requirement-set-acceptance-r3.verdict.md`. The same model and effort reviewed the corrected audit packet at 2026-09-30T17:02:03Z and returned AGREE, accuracy 99, completeness 99, pass 12, fail 0, unknown 0. Thread `01a0f33d-b297-7663-87e5-c83fea36b10c`. Receipt: `docs/receipts/hv/20260930T165500Z-requirement-set-acceptance-r4.verdict.md`. That agreement is that the ledger matches the files. It does not accept the recovered requirement set and it does not complete goal 2.
- The recovery guardrail is running. Source commit `a26464e234fa31cae85d433bdec27cbdd654b4c5` merged to `develop` as `fc75387043f8337855e34d92eb3a0e03aa82ce48` in https://github.com/sharpninja/McpServer/pull/73 at 2026-09-30T14:37:17Z. The running service was republished from detached `origin/develop` `fc753870` with Nuke `UpdateService --skip-version-bump --package-version 1.4.41`. Current health is `1.4.41+fc75387043f8337855e34d92eb3a0e03aa82ce48`. `GitVersion.yml` was not changed. The dirty `develop` checkout was not used. The Phase 2 smoke was measured earlier on `1.4.41+c195837ac888b6f02724221de6a040e63b3e6b47`.
- The first Phase 1 and Phase 2 100/100 reviews are withdrawn. The replacement requests are `docs/receipts/hv/phase1-completion.brief.md` and `docs/receipts/hv/phase2-completion.brief.md`.
- The reviewer has no MCP access. The requirement records supplied for review are under `docs/receipts/requirements-recovery/acceptance-20260929/supplied-requirements/`: 510 snapshot files plus 13 live HTTP GETs at 2026-09-30T00:35:23Z.
- The 2026-09-30T00:37:12Z Phase 1 review of the earlier supplied export, `docs/receipts/hv/20260930T003712Z-phase1-supplied.response.jsonl`, is DISAGREE. That export still had empty structured criteria. It is not the current store. The 15:34:43Z acceptance review is `docs/receipts/hv/20260930T152603Z-requirement-set-acceptance.response.jsonl`: DISAGREE, accuracy 95, completeness 90. The 16:06:19Z acceptance review is `docs/receipts/hv/20260930T155654Z-requirement-set-acceptance-r2.last.md`: DISAGREE, accuracy 97, completeness 95. The 16:50:56Z review is `docs/receipts/hv/20260930T164102Z-requirement-set-acceptance-r3.verdict.md`: DISAGREE, accuracy 96, completeness 92. The latest completed review is `docs/receipts/hv/20260930T165500Z-requirement-set-acceptance-r4.verdict.md`: AGREE, accuracy 99, completeness 99, at 2026-09-30T17:02:03Z, on ledger accuracy only. The recovered requirement set is not accepted, and goal 2 is not complete.
- Phase 2 completion review `docs/receipts/hv/20260930T002642Z-phase2-completion.response.jsonl` is AGREE, accuracy 100, completeness 100, on the timing measurement only. It does not accept the requirements.
- Phase 3 (xvic parity) and Phase 4 (VIC-20 settings visibility) have not started.
- Resume point: an explicit user message approving the start of Phase 3 and Phase 4. Approval of the plan is not that approval. Do not open slice 3a before that message.
- `FR-VIC-001`, `TR-CYCLE-001`, and `FR-CPU-002` now have structured acceptance criteria copied from the bullets already in their bodies. Counts are 8, 7, and 6. Body lengths stayed 1105, 995, and 1110. `isSatisfied` is false. The body text was not rewritten. Do not write xvic tests against a body rewrite until the user approves that text and the start.
- No commit or push was made for the 2026-09-29 acceptance receipts. `PLAN-C64SET-001` is not done. xvic parity is not claimed.

Repository state when this file was replaced:

- Workspace: `F:\GitHub\vice-sharp`
- Branch: `main`
- HEAD: `4dfdde53ae9f5daaef84d8dea1cea329bbd336d1`
- Origin: `https://github.com/sharpninja/vice-sharp.git`
- `origin/main` matches that HEAD.
- Untracked receipt paths from this audit are present. Do not clean, reset, or revert them unless the operator asks.

Live server at the Phase 2 smoke:

- Version: `1.4.41+c195837ac888b6f02724221de6a040e63b3e6b47`
- Health: Healthy, storage reachable
- Effective counts: FR 232, TR 138, TEST 140, MAP 231
- Recovery run `slice5-apply-clean-18028995ae17` is applied. Payload hash `d858d4e30feeab57a4f652ee9e8dc6e3ef70a34e8674e52e7ba5488cfb727164`.
- Do not POST another body on that idempotency key.

## Receipts

These files exist. The 100/100 reviews are not completion gates.

- Phase 0 baseline: `docs/receipts/requirements-recovery/acceptance-20260929/phase0-baseline.json`
- Phase 0b plan review: `docs/receipts/hv/20260929T230403Z-handoff-completion-plan-ignore-mcp.response.jsonl` (AGREE 99/99 on the plan text)
- Phase 1 ledger: `docs/receipts/requirements-recovery/acceptance-20260929/nine-defect-ledger.json`
- Phase 1 review, withdrawn as a completion gate: `docs/receipts/hv/20260929T233104Z-recovery-acceptance-audit-profile.response.jsonl`. The brief at `docs/receipts/hv/recovery-acceptance-audit.brief.md` told the reviewer that no done claim was made and that open rows were the expected report.
- Phase 2 smoke: `docs/receipts/requirements-recovery/acceptance-20260929/dense-session-smoke.json`
- Phase 2 review, withdrawn as a completion gate: `docs/receipts/hv/20260929T233645Z-dense-session-smoke.response.jsonl`. The brief told the reviewer to pass the phase when the timing receipt matched and to pass requirements because no requirement was claimed done.

Phase 2 measured session `GrokCode-20260929T215620Z-plugin-session` on that server only. `begin` was HTTP 201 in 322 ms. `complete` was HTTP 200 in 312 ms. Readback matched. No session-log code changed. This does not claim that older 40-second sessions are now fast.

## Recovery Acceptance

The preview gate `docs/receipts/hv/20260928T203110Z-requirements-recovery-preview-gate.response.jsonl` was DISAGREE. Applying that preview did not accept the requirement set.

Ledger status:

- Closed, and only for the narrower claim in the ledger: item 4 (four named TR bodies and two deleted placeholder ids) and item 9 (five live HTTP 400 dry-run rejections, `FR-VIC-001` body still 1105 characters). Item 9 is the plan guardrail row. It is not preview A9.
- Open: item 1 (acceptance criteria were not restored where no heading existed), item 3 (semantic classification is not done), item 6 (mappings are not per-AC coverage), item 7 (manifest is untracked), item 10 (preview D3), and item 11 (preview A9).
- Needs-operator: item 2 (mechanism wording), item 5 (14 April acceptance bullets are absent across five ids: `FR-MED-005` 1, `FR-PRF-005` 5, `FR-VIC-002` 1, `FR-VIC-003` 2, `FR-VIC-010` 5; receipt `docs/receipts/requirements-recovery/acceptance-20260929/april-ac-loss-20260930.json`), and item 8 (645 null old hashes after the `TEST-VIC-001` correction, and 110 null source commits). No requirement text was restored.

Item 9 recheck at 2026-09-30T15:49:00Z, workspace `F:\GitHub\vice-sharp`, method `workflow.requirements.planRecovery`, dry-run. Placeholder body and acceptance-criteria removal returned `validation_error`, `retryable` false, exception `System.ArgumentException`. Empty body, malformed id, and type-prefix mismatch returned `schema_validation_failed`, `retryable` false, and were not planned. `FR-VIC-001` parsed description text was unchanged across the before and after reads (1105 characters, equal, SHA-256 f2c678e7e25411e612c37686ff65fe489553ec13300297a68163eb54faf97b7e). The earlier 1215 figure was not the parsed description. A direct dry-run POST to `/mcpserver/requirements/recovery/dry-run` at 2026-09-30T16:13:00Z returned HTTP 400 `validation_error`, `retryable` false, for empty body, placeholder body, malformed id, type-prefix mismatch, and acceptance-criteria removal. After those five calls a later GET was saved as `FR-VIC-001-after-get.json` in `docs/receipts/requirements-recovery/acceptance-20260929/guardrail-probes-http-20260930T161259Z`. Its `body` field is 1105 characters, SHA-256 `f2c678e7e25411e612c37686ff65fe489553ec13300297a68163eb54faf97b7e`, the acceptance heading is present, and the structured list has 8 entries. That SHA matches the 15:15:09Z supplied description. The saved GET is later than the 16:13:00Z probes. The plugin schema still rejects empty body, malformed id, and type prefix before the server sees them. Those plugin results are `schema_validation_failed`, not a second copy of the HTTP 400. Plugin receipt directory: `docs/receipts/requirements-recovery/acceptance-20260929/guardrail-probes-20260930T154900Z`. The running service is health `1.4.41+fc75387043f8337855e34d92eb3a0e03aa82ce48`.

The placeholder rows `[TR-REMOTECTRL-SERVER-001]` and `PERF-SPRITE-DMA-OPT-001` were deleted on 2026-09-30. The 2026-09-30T15:15:09Z export records both as `not_found`. The unbracketed `TR-REMOTECTRL-SERVER-001` remains. TR `totalCount` in that export is 136.

Structured acceptance criteria were copied from existing `## Acceptance Criteria` bullets only. The scan covered FR 232, TR 138, and TEST 140. 205 records received criteria, 90 already had criteria and were left unchanged, and 215 had no heading and were not given invented criteria. Status values were not changed. Receipt: `docs/receipts/requirements-recovery/acceptance-20260929/structured-ac-backfill.json`.

Four legacy ids return 404: `ARCH-TRUEDRIVE-1541-002`, `BACKFILL-MEDIA-001`, `BACKFILL-VIDEO-001`, `RUNTIME-TAPE-002`. Their `FR-*` replacements return 200. `FR-MCP-REQRECOVERY-001`, `TR-MCP-REQRECOVERY-001`, and `TEST-MCP-REQRECOVERY-001` are 404 on this workspace. Do not mark them satisfied.

Item 7: `docs/requirements/oracles/vic20-xvic-manifest.json` exists and is untracked. VICE commit `cc67418cba58e71d8e70fd353e9de47199ad2075`. Media is an explicit empty array. Workload `program` values are the ROM names `kernal.901486-07.bin` (machine `vic20`) and `kernal.901486-06.bin` (machine `vic20ntsc`), from `Vic20ViceRomNames`. Cycle counts are the constants already in `Vic20NativeLockstepTests.CycleCounts` and `Vic20DivergeProbe`. The Codex review at 15:34:43Z rejected the earlier `power-on-reset` label. No new cycle numbers were invented.

## Operator Contract

- VICE, including x64sc and xvic quirks, is the behavioral oracle unless an approved requirement says otherwise.
- Legacy UWP and Xbox requirements stay. They are not retired.
- On every ViceSharp versus VICE mismatch, answer how the managed core diverged from VICE and how it gets back to VICE before changing code.
- Hostile reviews in this plan run in Cursor on the best listed Gemini Pro model. On 2026-09-29 that id was `gemini-3.1-pro`. Do not substitute Flash.
- Gemini reviews for this plan ignore MCP. The parent Grok turn stores the verdict and the jsonl paths.
- PowerShell only. Do not use Python.
- Do not mark a requirement, TODO, goal, or plan step done without executable proof and a hostile AGREE at accuracy and completeness both at least 98.

## Open TODOs

36 open. These stay open and are not this handoff's exit: `PLAN-GRAPHRAG-001`, `PLAN-RASPBIAN-001`, `PLAN-ARCHVIC20-001`, `PLAN-C64SET-001`, `PLAN-GROKCOMPLETION-001`. The full open id list from the 2026-09-29 query is in the session turn `req-20260929T234800Z-why-are-you-waiting`.

## Next Unfinished Pair

1. User approval to start Phase 3 and Phase 4, including any approved text for `FR-VIC-001`, `TR-CYCLE-001`, and `FR-CPU-002` if those bodies must change.
2. Phase 3 slice 3a only after that approval: fail-closed xvic oracle prerequisites. Do not claim parity from this file.
3. Phase 4 may run after the same approval when it does not touch CPU, clock, bus, or the native shim. `SelectedMachineProfile` still does not raise `IsVic20Selected`. `PLAN-C64SET-001` stays open even if that notification is later fixed.
