# ViceSharp handoff merge

Merge written 2026-10-16T14:59:19 local (America/Chicago, measured offset -05:00). This file is a new document. It does not replace, edit, or delete either source, and it does not replace the repo-root `HANDOFF.md`.

Sources, both read in full:

- Source A, 388 lines: `docs/receipts/handoff/20261011T112946-0500-canonical-reconciliation/HANDOFF.before.md`. In-file generation stamp: 2026-10-04T12:41:21-05:00. Body runs through 2026-10-08T14:42:20-05:00. SHA-256 measured 2026-10-16T14:59:19: `B7A13C695B04D2A2560AEEA3C174189594D8F0F776402662E688C86352E909E8`.
- Source B, 171 lines: `docs/receipts/handoff/20261016T092300-handoff-sync/HANDOFF.before.md`. In-file update stamp: Codex on PAYTON-LEGION2, 2026-10-13T23:51:19. SHA-256 measured 2026-10-16T14:59:19: `EA2737B0084B411D9E927227E183596A5779345D809E46B2C968F53A327A9579`. Source B does not record its own hash.

Path-versus-body date, not a claim conflict: source B lives in a folder named `20261016T092300-handoff-sync`, while its own text says it was updated 2026-10-13T23:51:19. Both facts are kept. This merge does not explain the gap.

Source B says it preserves source A at the path above with SHA-256 `B7A13C695B04D2A2560AEEA3C174189594D8F0F776402662E688C86352E909E8`, and says to treat A as history, not as instructions to repeat applies, restart completed reviews, or drain queues. The on-disk bytes of A matched that hash at merge time. Source B also says "This file replaces contradictory current-state sections." That is B's editorial stance. This merge does not apply that replacement. Where A and B disagree, both statements stay, dated, under the conflict register.

A byte-identical complete goal-plan reference sits beside A: `docs/receipts/handoff/20261011T112946-0500-canonical-reconciliation/goal-plan.reference.md`. The original external plan is unchanged. Reconciliation receipts for the 2026-10-11 edit are under `docs/receipts/handoff/20261011T112946-0500-canonical-reconciliation/README.md` (exact copies, state snapshots, before/after hashes, native-status comparison, link checks, diff-check output, TODO field changes, persistence disposition). That edit ran no mock or emulator tests and claimed no HV acceptance. The documentation skill guided separating current state from retained history.

## Conflict register

Neither side is chosen. A later sentence that says it supersedes an earlier one is recorded as that later sentence's claim.

C1. Phase 3 start. A's checkpoint (kept as the state at that checkpoint) says Phase 3 and Phase 4 still required an explicit user sentence, that Phase 4's VIC-20 settings-visibility slice was implemented later, and that Phase 3 still required an explicit start sentence. A's "Current State" and "Next unfinished work" say Phase 3 xvic parity has not started, that the resume point is an explicit user message approving that start, and that slice 3a must not be opened before that message. A's 2026-10-07T17:11:10-05:00 resume says Phase 3 was subsequently authorized, that live `PLAN-C64SET-001` records narrowly reviewed slices 3a and 3b, and that a fresh ask for initial Phase 3 authorization is not required. Source B repeats that authorization and those narrow slices, and adds: later R0 statements that require initial Phase 3 or Phase 4 approval conflict with this history and cannot erase it. Narrow 3a/3b evidence is not full xvic parity in either source.

C2. Who runs hostile validation. A's operator-contract section says HV is the sole cloud exception and must run through Grok launched locally, with no `--max-turns` or other artificial limit. A's 2026-10-07 rotation says alternate local Grok `grok-4.7` at `xhigh` and local Claude Code `claude-opus-5-5` at `max`, including remediation re-reviews. Resuming an interrupted review keeps that review's slot. Do not substitute models, execute in the cloud, spawn reviewer subagents, or impose artificial limits. Launch with an isolated session cache and failsafe root and an accurate `MCP_SESSION_MODEL` before hooks run, so the new review cannot supersede an old review turn. A's 2026-10-08 override says Grok is unavailable and upcoming HV, including remediation, uses the locally installed Claude Code CLI with model `claude-opus-5-5` and effort `max`. That override says it temporarily supersedes all alternating-reviewer and Grok-only instructions below it and in linked plans. Do not schedule a Grok review while the override applies. Retain it until the operator changes it. Do not infer restored availability. It changes reviewer assignment only, not product requirements, gate scope, prior verdicts, or implementation permission. Source B, for these remediation rounds, repeats Opus 5.5 max, says Grok is unavailable, and says that direction supersedes older alternation wording. Use locally launched `claude-opus-5-5`, effort `max`, per invocation. No model substitution, nested reviewer, or artificial turn or runtime limit. Require a full profile reload, isolated reviewer PowerShell and MCP state, live JSONL progress with a numeric completion percentage and an ETA, complete request and response JSONL, and full verdict persistence.

C3. What to do next. A's bottom "Next unfinished work" says: re-run traceability and `git diff --check`, then a new local Grok review; after that gate, ask approval of `PLAN-VSFLOCKSTEP-001` for Slice 1 only; the older phase-exit gate stays open; Phase 3 still needs its own start sentence. Later sections inside A say those current-state and next-step lines are superseded, and they move the work through Phase 0 recovery, accepted Phase 0, Phase 1 mocks, a noon waiver, and Oct 8 feasibility and remediation. Source B's next sequence says the R0 candidate review has reached AGREE, no additional review is pending, the work is paused, the review does not authorize an apply, and a future resume must present the exact expanded delta plus idempotency and writer-window decisions before any requirement mutation. All three next-step layers are in section 16. Do not execute an older list as if it were the latest instruction.

C4. Effective requirement counts, by measurement, not interchangeable. At the Phase 2 smoke: FR 232, TR 138, TEST 140, MAP 231. The 2026-09-30T15:15:09Z export: TR `totalCount` 136. The structured-AC scan covered FR 232, TR 138, TEST 140. Measured 2026-10-11 for source B: effective `layer-1`, FR 235, TR 139, TEST 142, mappings 233. Source B says counts are not semantic or AC-coverage proof.

C5. MCP server identity. Phase 2 smoke ran on `1.4.41+c195837ac888b6f02724221de6a040e63b3e6b47`, health Healthy, storage reachable. The running service was later republished from detached `origin/develop` `fc753870` with Nuke `UpdateService --skip-version-bump --package-version 1.4.41`. Health at the item-9 recheck: `1.4.41+fc75387043f8337855e34d92eb3a0e03aa82ce48`. `GitVersion.yml` was not changed. The dirty `develop` checkout was not used. Source B's 2026-10-11 marker reports server `1.4.40+bcae36fdce7ce31e792db9e06a17bff51019bdf9`, PID 18288, startup `2026-10-11T11:39:42.6681389+00:00`. Source B says old 1.4.41 smoke receipts are not that service's proof, and that a historical begin of 322 ms and complete of 312 ms proves only its measured session and server.

C6. Git HEAD. Checkpoint commit `c364e73037f646a7c5d0005c931fb689f665ce9f` is on `origin/main` as the range `4dfdde5..c364e73`. Message: `docs(receipts): record requirement-set audit checkpoint`. It contains 699 files: the audit receipts, the handoff text at that commit, and `docs/requirements/oracles/vic20-xvic-manifest.json`. Immediately after `git push origin HEAD:main`, `origin/main` matched that HEAD. The handoff text is newer than `c364e73` and is not in that commit. Measured 2026-10-11: branch `main`, HEAD `54131ffc868f55a0ec944e99aa9fa67af3613180`, origin `https://github.com/sharpninja/vice-sharp.git`, no fetch, no push, and no remote-equality claim. These are different commits.

C7. Traceability diagnostics, two measurements. During 2026-10-07 recovery: 235 canonical IDs, 149 referenced, 86 missing references, 1,011 noncanonical references, command exit 0. Not a clean gate. On 2026-10-08 in the Phase 2 evidence root: traceability exit 0 still reports 83 unreferenced canonical IDs and 1,011 noncanonical references. Not a clean gate.

C8. Profile layout at reload. Reloads on 2026-10-08 (12:48 and 13:25:44-05:00, and the 10:50 recovery) counted 22 profile parts, 15 other Global records, and one manifest, 38 total. The 10:50 recovery said all 38 Global MCP records were read, including 22 hash-verified profile parts, and the marker hash was unchanged. Source B, inspected 2026-10-13, says the Codex add-profile skill reads `C:\Users\kingd\.claude\profile\PROFILE-MERGED.md`. The 22-part count is the 2026-10-08 layout, not the 2026-10-13 skill description.

C9. Noon waiver. Source A at 2026-10-08T08:19:01-05:00 says continue without inter-phase HV until 2026-10-08T12:00:00-05:00 (America/Chicago), then have Claude HV everything missed. The single-use heartbeat `catch-up-vicesharp-hv-at-noon` was ACTIVE at that writing. The catch-up then stopped on HTTP 429 and is not a verdict (section 15). Source B says the noon waiver expired and is not ongoing, and that the interrupted draft DISAGREE 96/94 is not a terminal verdict.

C10. "The reviewer has no MCP access" in A's current-state section describes the 2026-09-30 supplied-export review. Later reviews in both sources have MCP turns and primary readback. It is not a standing rule that reviewers lack MCP. Source B requires full primary verdict readback.

C11. Open TODO cardinality. Source A: 36 open on the 2026-09-29 query, full id list in session turn `req-20260929T234800Z-why-are-you-waiting`. Named as staying open and outside that handoff's exit: `PLAN-GRAPHRAG-001`, `PLAN-RASPBIAN-001`, `PLAN-ARCHVIC20-001`, `PLAN-C64SET-001`, `PLAN-GROKCOMPLETION-001`. Source B, measured 2026-10-11: 39 returned of 39, and `PLAN-GROKCOMPLETION-001`, `PLAN-REQAUDIT-001`, `PLAN-VICEORACLE-001`, `PLAN-VSFLOCKSTEP-001`, and `PLAN-C64SET-001` all remain `done: false`. The two counts are different queries. Inside `PLAN-VICEORACLE-001`, only the immutable-oracle Phase 0 task is done; five tasks and the overall TODO stay open. That is the same claim in A (2026-10-07) and B, not a conflict with the TODO staying open.

## Goal and plan

The objective in both sources is 100 percent completion of `C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\01a0ef2b-2331-70d1-9c65-0ef12b15d09a\plan.md`, with HV agreement. Not tape-only, documentation-only, or mock-only completion. Source B records original plan SHA-256 `436BB2B1DA834491A9B0C8F0F6EFD3F1EBD81F001885AD31D6FE8180CDE26473`.

Source A says this file replaced the 2026-09-28 handoff. That older file said recovery had not been applied and that product work was waiting on a plan. Both of those statements are stale.

Source B, 2026-10-13T23:51:19: paused after accepted R0 remediation HV. The full goal remains incomplete. No requirement application or implementation is authorized by that result. The exact delta remains unapproved and unapplied. All mock, implementation, and real-emulator gates remain mandatory.

Operator sentence that bounded the 2026-10-13 work: "Resume. Remediate both HV failures, rerun HV with Opus 5.5 max. Iterate to HV agree then pause." Source B says that bounded request is fulfilled.

Work location in both: this chat, PAYTON-LEGION2, `F:\GitHub\vice-sharp`. F: relocation is complete. Normal development on F: is authorized. No further migration or partition work. Source A records the operator confirming that on 2026-10-07. Source B repeats it as a standing rule.

After compaction, both sources require a full add-profile reload before substantive work, not a cached summary. Source A points at `C:\Users\kingd\.codex\skills\add-profile\SKILL.md` and, on 2026-10-08, at all fresh Global MCP bodies. Source B says the same skill, and that as inspected 2026-10-13 it reads `PROFILE-MERGED.md` (conflict C8). Consult current MCP workspace guidance. A summary is not a substitute. Do not voluntarily compact early. Automatic platform compaction is outside that guarantee. Source B also says read the current `AGENTS-README-FIRST.yaml` and bootstrap the supported Codex plugin. That sentence is an instruction inside B for a Codex resume. It is not a claim that this merge was produced by Codex.

Standing continuity from A: follow explicit scope, environment, sequence, tools, and approval boundaries literally. Surface any inability before substituting an action.

Reports: measured sortable local timestamps `yyyy-MM-ddTHH:mm:ss`. No invented times. No em dashes. No tables. Existing MCP identifiers keep the protocol-required UTC naming. Source B adds: new receipt names use sortable local time. Every completion claim needs command output, an on-disk check, or MCP readback.

## Operating contract

Kept from both, with the conflicts above left open:

- VICE, including x64sc and xvic quirks, is the immutable behavioral target. On each mismatch, answer how managed behavior diverged and how it returns to VICE. Never modify the reference to make managed code pass.
- Do not edit, patch, restore, clean, delete, or generate into `native/vice`. Preserve its existing dirty state. Upstream-required in-source generation was approved only in a separate pinned materialization outside the submodule, with source-byte provenance. That is not permission to use the dirty patch as an oracle. Compare original tracked-source bytes and commit before and after. Record generated files separately, including producer commands and hashes. A nonempty generated-file inventory is allowed in that separate checkout. This supersedes stricter zero-generated and untracked-file conditions in older plan text and review packets. Pinned checkout: `artifacts/immutable-vice/cc67418cba58e71d8e70fd353e9de47199ad2075/source`. Entry script `build-stock-vice.sh` invokes the installed MSYS2 MINGW64 toolchain via explicit Git Bash. It runs upstream `autogen.sh`, `configure --enable-option-checking=fatal --enable-headlessui --disable-pdf-docs --disable-html-docs`, then `make -j4`, only in that checkout. Operator correction: "Following the VICE Team's reqs is approved."
- Native VICE `.vsf` is the day-one canonical snapshot format, not a secondary export. Managed VSF resume already exists. Preserve and productionize it. Do not propose it as new functionality or replace it with proprietary snapshots.
- Every FR, including legacy FRs, needs appropriate AC. UWP and Xbox implementation is out of scope. Historical requirements for them are not retired or deleted unless the operator explicitly directs that.
- BDPv4, operator correction 2026-10-07, verbatim intent in A: "RED tests are not acceptable. BDPv4 requires mocking data to prove tests work to green before starting code." Write AC-covering tests first. Prove them green against valid mocked data before production implementation. Deliberately incorrect mock data must be rejected inside a green harness. A failing test or an expected-red production run is not a gate. Only after the mock gate passes may production implementation begin. Then the full applicable current-plus-prior suite must pass with zero failed and zero skipped tests before advancing. Mock evidence is not real VICE parity evidence. This corrects process, not product requirements, and supersedes conflicting historical instructions. Source B restates the same rule and adds: executable exit also rejects aborted tests, and focused filters, name-counting scripts, source inspection, and mock success cannot replace the full suite or required real runtime evidence. The local Grok r2b review reported AGREE at 99/99 with PASS 22, FAIL 0, UNKNOWN 0, but it explicitly accepted the now-rejected expected-red production step. That receipt is historical evidence only for that sequence, not authorization to start implementation. Re-review the corrected active plans before advancing. Preserve the original receipt.
- Shell: PowerShell only. No Python. Source B adds: all shell work uses PowerShell.MCP. No raw shell fallback, no cloud execution, no remote chat, and no web-review substitution. Source A's older operator contract also forbids cloud runners, hosted agents, web tools, remote Codex threads, cloud CI, pushes, and other cloud execution, with HV as the sole exception under conflict C2.
- Acceptance of a review requires AGREE, accuracy and completeness both at least 98, and zero FAIL and zero UNKNOWN. Source B also requires full primary verdict readback. No goal, TODO, requirement, satisfaction, or phase done-state advance on an incomplete or rejected review. Source A states the same 98/98 bar for marking a requirement, TODO, goal, or plan step done, and requires executable proof.
- Attempt supported primary MCP saves first. On a genuine persistence or triage failure, preserve the complete record, including an entire HV verdict where applicable, through the supported failsafe queue. Label queued versus persisted. Verify the failsafe. Continue the active task. Operator, 2026-10-07: "If you genuinely cannot save to MCP, you must use the fallback." Operator, also answered "Yes, use failsafe for triage too." Source A says that supersedes the marker's stop-on-triage-failure instruction for this work, and that the implementation window is not to be spent retrying the same reporting failure. Do not bypass the plugin, edit storage directly, discard records, summarize away verdict content, or treat a queued requirement mutation as a confirmed primary update. Source B adds: keep `MCP_FAILSAFE_DRAIN_DISABLED=1`. Do not replay historical records, cancel old turns, or discard quarantine evidence.
- New requirement deltas need explicit exact-delta approval after HV. Approval of a smaller tape proposal does not authorize the expanded 16-record candidate. Its idempotency departure and writer-window prerequisites also need the explicit authority described in its README before R1.
- No commit or push without the original plan's separate file-named operator order. GitHub `origin` only. No destructive checkout or broad cleanup. Copy, verify, and retain originals. The historical push of `c364e73` already happened (conflict C6). It is not a standing permission to commit again.
- Do not mark `PLAN-C64SET-001`, any requirement, or any TODO done from the 99/99 ledger agreement.

## Repository, native tree, marker, and server

Workspace `F:\GitHub\vice-sharp`. Branch `main`. Origin `https://github.com/sharpninja/vice-sharp.git`. HEAD chronology is conflict C6.

Native gitlink, both the 2026-10-07 baseline and the 2026-10-11 measurement: `cc67418cba58e71d8e70fd353e9de47199ad2075`. Read-only baseline `docs/receipts/native-vice/20261007T133100-0500-readonly-source-baseline.json`: 6,374 files hashed, 12 tracked changes, 2,527 untracked paths. Opus independently rehashed it with zero missing, extra, or changed files. It is dirty-source preservation evidence, not clean-native acceptance. Source B on 2026-10-11: 12 tracked modifications, 2,527 untracked paths under `--untracked-files=all`. Default directory-collapsed inventory: 1,160 entries. Different counting modes do not imply deletions. Patched-native evidence remains diagnostic. Root `git status --porcelain=v1` before the 2026-10-11 handoff edit: 880 entries. Dirty inventory is not permission to stage or reset. No restoration of the preexisting native dirt is authorized.

Repeated native-integrity pin used across the 2026-10-08 runs, each time reported unchanged for that run: eight native baseline fields, 3,847 original stock source files, 99 stock link inputs, and the stock or original gate executable hashes named in that section. The 2,815 generated files in the separate stock checkout have their own inventory. `native/vice` remained unchanged across those probes.

Marker on 2026-10-11, SHA-256 `113AC9DE1D98D4B807B3FFFB2A658166E757057FBB5BAB488B88B724003112AC`. Server identity is conflict C5. Read the current marker for endpoint and credentials. Never print credentials. During an earlier marker reload, an incomplete redaction exposed the repeated API key to a tool transcript. Subsequent rendering replaces the exact key everywhere. Never copy that value into receipts or handoffs.

Parent session throughout the Oct 7-11 work: `Codex-20261007T135626Z-plugin-session`. Source B's reconciliation request: `req-20261011T162438Z-goal-handoff-reconcile`. Historical IDs are evidence, not turns to overwrite. That turn's begin save timed out after 30 seconds and was initially queued, not persisted. Later `appendDialog` reported recovery. Triage returned exit 1/False without a report ID. Supported failsafe: `.mcpServer/failsafe/Codex/workspaces/RjpcR2l0SHViXHZpY2Utc2hhcnA/pending/20261011T162729Z-handoff_beginturn_and_triage_failure-4a5f.yaml`, SHA-256 `7749D1636D74B23FEA3EF1CA5A626F0FECABB54F06D05373FFF44233900EF2C2`. No HTTP 503 was directly observed. No all-queues-reconciled claim. Source B says current 30-second wrapper failures are not under-five-second success and are not an observed HTTP 503.

PowerShell.MCP console #16116 was corrected to the vice-sharp Codex cache and failsafe queue. Inherited environment values had pointed at other workspaces. Only process-local routing was corrected. No global configuration change. Preserve the historical quarantine. Do not blindly drain stale snapshots.

Measured window for B's live state: 2026-10-11T11:28:00-05:00 through 2026-10-11T11:29:07-05:00. Source B says refresh before treating that block as current in a later session.

## Requirements ledger, recovery, and traceability

Recovery run `slice5-apply-clean-18028995ae17` is applied. Payload hash `d858d4e30feeab57a4f652ee9e8dc6e3ef70a34e8674e52e7ba5488cfb727164`. Do not POST another body on that idempotency key. Do not recreate the four replaced legacy FR IDs.

Historical receipts under `docs/receipts/requirements-recovery/acceptance-20260929/` are not current counts or current service proof. Reconcile intervening approved changes at final exit.

The preview gate `docs/receipts/hv/20260928T203110Z-requirements-recovery-preview-gate.response.jsonl` was DISAGREE. Applying that preview did not accept the requirement set.

The 99/99 ledger review (Codex `gpt-6-astra`, effort `xhigh`, 2026-09-30T17:02:03Z, thread `01a0f33d-b297-7663-87e5-c83fea36b10c`, accuracy 99, completeness 99, pass 12, fail 0, unknown 0, receipt `docs/receipts/hv/20260930T165500Z-requirement-set-acceptance-r4.verdict.md`) agreed that the ledger matches the files. It did not accept the recovered requirement set and it did not complete goal 2. Do not mark TODOs or requirements done from it.

Ledger items, and only for the claim recorded in the ledger:

- Closed: item 2 (operator said replicate VICE behavior; `FR-CPU-002` ac-05 says each clock phase advances once and the CPU fetch, decode, and execute sub-cycle state at that phase matches VICE x64sc; status still pending; it does not name `IClockedDevice.Tick` or `ICpu.Phase`). Item 4 (four named TR bodies and two deleted placeholder ids). Item 5 (`FR-PRF-005` no longer allows a stub or silent sound; April sentences that disagree with x64sc were not restored; the requirement stays the 6560/6561 and the four VIC voices). Item 8 (`provenance-accepted-20260930.json` stays unchanged: 645 empty old hashes and 110 empty source commits; it was not rewritten). Item 9 (five live HTTP 400 dry-run rejections; `FR-VIC-001` body still 1,105 characters). Item 9 is the plan guardrail row. It is not preview A9.
- Open: items 1, 3, 6, 7, 10, and 11. Item 1: acceptance criteria were not restored where no heading existed. Item 3: semantic classification is not done. Item 6: mappings are not per-AC coverage. Item 7 stays open because the committed manifest still uses KERNAL ROM filenames and existing reset-cycle constants. Committing the file did not create named-program measurements. Item 10 is preview D3. Item 11 is preview A9.
- Needs-operator: none.
- The nine-defect ledger later included items 10 and 11. Closed items mean only their specific ledger claims. Source B says the same open and closed sets, and says preserve the provenance gaps honestly rather than inventing data. `docs/receipts/requirements-recovery/acceptance-20260929/nine-defect-ledger.json` is modified in the worktree and is not in `c364e73`.

Item 9 recheck at 2026-09-30T15:49:00Z, workspace `F:\GitHub\vice-sharp`, method `workflow.requirements.planRecovery`, dry-run. Placeholder body and acceptance-criteria removal returned `validation_error`, `retryable` false, exception `System.ArgumentException`. Empty body, malformed id, and type-prefix mismatch returned `schema_validation_failed`, `retryable` false, and were not planned. `FR-VIC-001` parsed description was unchanged across before and after reads: 1,105 characters, equal, SHA-256 `f2c678e7e25411e612c37686ff65fe489553ec13300297a68163eb54faf97b7e`. The earlier 1,215 figure was not the parsed description. A direct dry-run POST to `/mcpserver/requirements/recovery/dry-run` at 2026-09-30T16:13:00Z returned HTTP 400 `validation_error`, `retryable` false, for empty body, placeholder body, malformed id, type-prefix mismatch, and acceptance-criteria removal. After those five calls a later GET was saved as `FR-VIC-001-after-get.json` in `docs/receipts/requirements-recovery/acceptance-20260929/guardrail-probes-http-20260930T161259Z`. Its `body` is 1,105 characters with that same SHA-256, the acceptance heading is present, and the structured list has 8 entries. That SHA matches the 15:15:09Z supplied description. The saved GET is later than the 16:13:00Z probes. The plugin schema still rejects empty body, malformed id, and type prefix before the server sees them. Those plugin results are `schema_validation_failed`, not a second copy of the HTTP 400. Plugin receipt directory: `docs/receipts/requirements-recovery/acceptance-20260929/guardrail-probes-20260930T154900Z`.

Structured acceptance criteria were copied from existing `## Acceptance Criteria` bullets only. The scan covered FR 232, TR 138, and TEST 140. 205 records received criteria, 90 already had criteria and were left unchanged, and 215 had no heading and were not given invented criteria. Status values were not changed. Receipt: `docs/receipts/requirements-recovery/acceptance-20260929/structured-ac-backfill.json`.

`FR-VIC-001`, `TR-CYCLE-001`, and `FR-CPU-002` have structured acceptance criteria copied from the bullets already in their bodies. Counts are 8, 7, and 6. Body lengths stayed 1,105, 995, and 1,110. `isSatisfied` is false. The body text was not rewritten. Do not write xvic tests against a body rewrite until the user approves that text and the start.

Placeholder rows `[TR-REMOTECTRL-SERVER-001]` and `PERF-SPRITE-DMA-OPT-001` were deleted on 2026-09-30. The 2026-09-30T15:15:09Z export records both as `not_found`. The unbracketed `TR-REMOTECTRL-SERVER-001` remains.

Four legacy ids return 404: `ARCH-TRUEDRIVE-1541-002`, `BACKFILL-MEDIA-001`, `BACKFILL-VIDEO-001`, `RUNTIME-TAPE-002`. Their `FR-*` replacements return 200. `FR-MCP-REQRECOVERY-001`, `TR-MCP-REQRECOVERY-001`, and `TEST-MCP-REQRECOVERY-001` are 404 on this workspace. Do not mark them satisfied.

Item 7 manifest: `docs/requirements/oracles/vic20-xvic-manifest.json` is in commit `c364e73`. The r4 review saw it untracked. Item 7 stays open. VICE commit `cc67418cba58e71d8e70fd353e9de47199ad2075`. Media is an explicit empty array. Workload `program` values are the ROM names `kernal.901486-07.bin` (machine `vic20`) and `kernal.901486-06.bin` (machine `vic20ntsc`), from `Vic20ViceRomNames`. Cycle counts are the constants already in `Vic20NativeLockstepTests.CycleCounts` and `Vic20DivergeProbe`. The Codex review at 15:34:43Z rejected the earlier `power-on-reset` label. No new cycle numbers were invented. ROM filenames, reset constants, and an empty media list are not named workload parity.

All current and legacy FRs still need appropriate AC, semantic classification, and per-AC executable VICE proof. Mapping count is not coverage. Preserve the full immutable VICE, build, ROM, media, and workload manifest and the requirement-audit obligations, not only fourteen tape records.

The reviewer for the 2026-09-30 supplied export had no MCP access (conflict C10). Records supplied for that review are under `docs/receipts/requirements-recovery/acceptance-20260929/supplied-requirements/`: 510 snapshot files plus 13 live HTTP GETs at 2026-09-30T00:35:23Z.

Earlier disagreeing acceptance reviews, kept as history: `docs/receipts/hv/20260930T003712Z-phase1-supplied.response.jsonl` is DISAGREE (that export still had empty structured criteria and is not the current store). `docs/receipts/hv/20260930T152603Z-requirement-set-acceptance.response.jsonl` at 15:34:43Z: DISAGREE, accuracy 95, completeness 90, pass 2, fail 9, unknown 1. `docs/receipts/hv/20260930T155654Z-requirement-set-acceptance-r2.last.md` at 16:06:19Z: DISAGREE, accuracy 97, completeness 95, pass 2, fail 9, unknown 1. Thread `01a0f308-7f62-72f0-89c7-da43b8460cff`. Response stream: `docs/receipts/hv/20260930T155654Z-requirement-set-acceptance-r2.response.jsonl`. `docs/receipts/hv/20260930T164102Z-requirement-set-acceptance-r3.verdict.md` at 16:50:56Z: DISAGREE, accuracy 96, completeness 92. Same model and effort for those reviews: Codex `gpt-6-astra` at `xhigh`.

On 2026-09-30 the operator said "Finish this" after "It does not accept the requirement set." Ledger item 2 was later closed when the operator required VICE behavior. No requirement status was set to completed, and no TODO was marked done. Phase 0 recorded the live baseline. Phase 2 recorded timings for one dense session.

Traceability numbers are conflict C7.

Initial recovery evidence: `docs/receipts/native-vice/20261007T092700-0500-phase0-primary-recovery/`. The initial recovery's supported drain replayed 4 records without failures. Primary readback verified the approved `TR-VSFLOCKSTEP-RESUME-001` body and both structured AC corrections, pending and unsatisfied. Plan-remediation turn `req-20261007T213226Z-prompt-8b0b` and initial recovery turn `req-20261007T135720Z-turn-866a` are historical, not active turns to reuse. The approval, quarantine, and re-review turn is `req-20261007T220948Z-prompt-cca6`.

The historical immutable-plan HV primary turn contains its full prose and scores but omits the final structured verdict JSON. At the time of that note, a fresh local Grok review was required and no Phase 0 completion was claimed. Phase 0 was later accepted (section 8). No product tests or builds were run during that recovery.

McpServer guardrail publish: source commit `a26464e234fa31cae85d433bdec27cbdd654b4c5` merged to `develop` as `fc75387043f8337855e34d92eb3a0e03aa82ce48` in https://github.com/sharpninja/McpServer/pull/73 at 2026-09-30T14:37:17Z.

## Phase-exit gate and still-red acceptance facts

BDPv4 phase number is the requirement `Iteration:` field. Priority P0, P1, and P2 is severity and is not a phase. The operator order "Make it so" is this gate. Phase 2 has not exited. The Phase 4 VIC-20 settings-visibility behavior was implemented later and has only its narrow focused gate. It does not exit Phase 2 or prove xvic parity. Phase 3 language is conflict C1.

`tools/check_phase_acceptance_coverage.ps1 -ThroughPhase 2` counts a section when its `##` heading is an FR, TR, or TEST id, `Iteration:` is 0, 1, or 2, and `### Acceptance Criteria` has at least one numbered or bullet criterion. A hit counts only under `tests/` in `.cs` and `.md` files. A mention in `src/` does not count.

Before the acceptance files existed, that command exited 1 on 2026-09-30: 94 requirements in scope, 17 missing from `tests/`, 101 criteria.

- Phase 0: `FR-CPU-004`, `FR-MEM-006` (13 criteria).
- Phase 1: `FR-MEM-004`, `FR-SID-004`, `FR-SID-009`, `FR-UI-004` (21).
- Phase 2: `FR-CFG-003`, `FR-CFG-004`, `FR-CIA-005`, `FR-CRT-002`, `FR-INP-003`, `FR-MED-005`, `FR-MON-005`, `FR-MON-006`, `FR-SID-005`, `FR-SNP-003`, `FR-VIC20-005` (67).

`tests/ViceSharp.TestHarness/PhaseAcceptanceCoverageTests.cs` fact `ExitingPhase_RequiresTestsForThatPhaseAndPriorPhases` expects the script to exit 0. Do not loosen that fact.

Re-run during the handoff update, local 2026-09-30 14:58:09: the script exited 0. In-scope requirements: 94. Missing from `tests/`: 0. Each of the 17 ids is named under `tests/`. The acceptance facts are not all green, and the phase has not exited. Script exit 0 only means the 17 ids are named under `tests/`.

The gate files are untracked and are not in `c364e73`:

- `tools/check_phase_acceptance_coverage.ps1`
- `tests/ViceSharp.TestHarness/PhaseAcceptanceCoverageTests.cs`
- `tests/ViceSharp.TestHarness/FrCfg003AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrCfg004AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrCia005AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrCpu004AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrCrt002AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrInp003AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrMed005AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrMem004AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrMem006AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrMon005AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrMon006AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrSid004AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrSid005AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrSid009AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrSnp003AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrUi004AcceptanceTests.cs`
- `tests/ViceSharp.TestHarness/FrVic20005AcceptanceTests.cs`

Release filter of those classes plus `PhaseAcceptanceCoverageTests`, before the last stimulus corrections: Failed 52, Passed 46, Skipped 0, Total 98, exit 1. The coverage fact was not one of the 52 failures. Source B cites this same 98-test run (52 failed, 46 passed) and says a later focused nine-pass correction did not establish a green full exit. Do not weaken old assertions simply to pass. First reconcile to approved VICE-derived criteria and preserve test disposition.

Stimulus corrections after that filter changed test code only. No product source was edited.

- `FR-SID-009`: OSC3 is voice 3. The noise fact writes `$D412` and reads `$D41B`. It had written `$D404`.
- `FR-VIC20-005`: each cart attaches on its own host, and `AppliedToRuntime` is required. Dirty FE3 and Ultimem come back from `FlushImage`. Mega-Cart NVRAM comes back from `FlushNvram`, and `FlushImage` stays null. Ultimem `$2000` is stored at image offset `$6000` because the power-up BLK1 bank register is 3. An earlier combined session left the FE3 preset `8k` on the port. Ultimem rejects that preset after `Eject`, and the host still returns Ok, so the kind was `None`.

Focused rerun of `FrSid009AcceptanceTests` and `FrVic20005AcceptanceTests`: Failed 0, Passed 9, Skipped 0, exit 0. The 98-test filter was not repeated after that, so the full fail count is not re-measured.

Still red from that run, excluding the two facts fixed above:

- `FR-CPU-004`: the cassette motor stays off when bit 3 is driven active low. Sense bit 4 stays set when play is pressed (expected 0, actual 16). An output-low bit that becomes an input reads the pull-up on the first read (expected 0, actual 32).
- `FR-SID-005`: there is no `SetSidModel` or `SwitchFilterModel`. The resonance fact stays red. The same stimulus produced 8580 band-pass 30320 and 6581 band-pass 26265. A smaller absolute band-pass value is the wrong comparison. Do not flip that inequality.
- `FR-CFG-003`, `FR-CFG-004`, `FR-CIA-005` CNT and SP pins, `FR-CRT-002` ROMH and CRT CHIP packets, `FR-INP-003` mouse methods, `FR-MED-005`, `FR-MON-005` (`Unknown: bank`), `FR-MON-006`, and `FR-SNP-003`.

A standard VIC-20 BLK image, separate from FE3, Ultimem, and Mega-Cart, does not have its own fact.

Do not implement cassette motor, sense, discharge, monitor bank or watch commands, palette files, hotkeys, the media catalog, replay, the mouse port, Ocean ROMH, or a runtime SID switch to turn this suite green. The next code for each still-red criterion is a correct failing test, then mocks, then implementation. Source B says full acceptance failures across CPU port and tape, SID, configuration, CIA pins, cartridge, input, media, monitor, and replay remain until reconciled and proved under BDPv4.

## Immutable boundary: Phase 0 through 8 October feasibility

`docs/plans/PLAN-VICE-IMMUTABLE-001.md` remains prerequisite. The boundary was always required. `FR-VICE-001`, `TR-VICE-ORACLE-001`, `TEST-VICE-001`, and all seven `TR-CYCLE-001` acceptance criteria remain mandatory. `TR-CYCLE-001` stayed pending with all seven ACs unchanged and unsatisfied at the 2026-10-07 Opus review. Existing dirty shim and `native/patches/vice-shim-runtime.patch` cannot establish acceptance. Do not run the current TestHarness or solution native-mutating build to validate these mocks. Do not invoke a native-mutating legacy build.

Operator approval: "Yes, mocks first; retain all real gates." Every existing real-emulator gate remains mandatory and unwaived.

### Phase 0 acceptance, 2026-10-07T17:48:03-05:00

Phase 0 of `PLAN-VICE-IMMUTABLE-001` is accepted. Local Grok 4.7 xhigh returned AGREE, accuracy 99, completeness 98, PASS 26, FAIL 0, UNKNOWN 0, and exited 0 at 17:47:27-05:00. Receipt prefix: `docs/receipts/hv/20261007T171400-0500-phase0-remediation-grok`. Primary completed turn: `GrokCode-20261007T222539Z-phase0-remediation-hv` / `req-20261007T222539Z-phase0-remediation-hv`. Parent readback proves the entire verdict prose and JSON in a 22,855-character response. See `.parent-primary-proof.json`, `.parent-final-primary-query.json`, `.readback.json`, and `.exit.json`. All 59 pinned inputs were unchanged at review exit. Full response stream: 3,988,656 bytes, SHA-256 `2132F05EBA4A7AB13AECE5112772EE4632E4CA9EF6481F4C28596E689132CA4B`. Source B repeats this acceptance and says only this task was accepted, not all Phase 0 tests and not the whole plan. Do not relaunch the completed Grok review.

The review disclosed the parent's earlier raw-pwsh runner violation and mistaken hash-comparison shorthand. Independent checks confirmed the corrected current evidence. The hook-created side turn with model `grok` is not the `grok-4.7` verdict turn and was not used as acceptance evidence.

MCP `PLAN-VICEORACLE-001` records only its Phase 0 task done. The other five tasks and the overall TODO remain open. Readback: `.accepted-todo-readback.json`. At that moment, next was the entire native-safe AC-mapped mock-contract project, all six groups, valid and deliberately incorrect mocks, full unfiltered suite green before production implementation. No mock project or production code had begun yet. That "nothing begun" sentence is the state at 17:48 on 7 October. Later the same day and on 8 October, mock projects and feasibility code were added. Those later facts do not erase the 17:48 sentence.

Earlier the same afternoon, local Opus 5.5 max returned DISAGREE (accuracy 94, completeness 85, PASS 22, FAIL 4, UNKNOWN 0), exit 0, same logical review and CLI session `fa2d7384-f174-4f2e-8b3c-4a4cc6477893`. Receipt prefix: `docs/receipts/hv/20261007T141100-0500-phi2-ac-remediation-opus`. Completed primary turn: `ClaudeCode-20261007T191324Z-plugin-session` / `req-20261007T191327Z-prompt-be46`. Parent verified the complete 58,553-character canonical verdict and structured JSON, allowing only CRLF-to-LF normalization. Proof: `.parent-primary-proof.json`. Separate correction turn `ClaudeCode-20261007T203504Z-plugin-session` / `req-20261007T211928Z-turn-6c6f` is also completed and freshly read back. Do not relaunch that completed retry.

Opus's four findings: missing durable MCP tracking; missing or unknown access-kind and empty or gapped observation cases; unresolved pending-record reconciliation; focused-filter exit wording. The continuation created and read back open MCP `PLAN-VICEORACLE-001`, including deferred comparator XMLDoc correction and real gate `REAL-C64-PHI2-AEC-001`; amended mock groups 1, 4, and 6; and required full applicable current-plus-prior suites with zero failed, skipped, and aborted tests at every executable exit. The whole-goal plan no longer treats a named blocker as 100 percent completion and explicitly excludes UWP and Xbox implementation while preserving legacy requirements. Before-file copies and receipts: `docs/receipts/hv/20261007T163600-0500-phase0-opus-remediation/`. Those edits awaited independent acceptance at that writing. The later Grok AGREE is the acceptance of Phase 0, not of every later finding.

Earlier Grok findings on incorrect PHI2/AEC mapping, unsupported `TR-CYCLE-001` closure, and intermediate steering hashes were verified by Opus. Four reconstructed historical full files remain read-only under `docs/receipts/hv/20261007T131928-0500-phase0-mock-scope-grok-b7-reconstruction/`. Original review verdicts are unchanged.

`REAL-C64-PHI2-AEC-001` remains an actual remaining task in source B. Missing observation fails closed and never supplies inferred native values.

### Phase 1 mock project and noon waiver, 2026-10-08T08:19:01-05:00

Phase 1 has a standalone native-safe mock project at `tests/ViceSharp.OracleContract.Tests`. Full unfiltered run at 2026-10-08T07:33:34-05:00: 82 tests, 82 passed, 0 failed, 0 skipped, 0 aborted. Evidence: `docs/receipts/native-vice/20261008T070900-0500-phase1-mocks/full-tests-02-result.json`, matching TRX and log, `scenario-ac-ledger.json`, and `native-comparison.json`. Earlier compile and eight data-binding failures remain preserved. This is mock evidence only, not real VICE or VSF parity. Phase 1 HV remained pending at that writing. No TODO, requirement, or goal completion was claimed. The next approved work named then was Phase 2 wrapper-only feasibility in `PLAN-VICE-IMMUTABLE-001`.

The noon waiver terms are conflict C9. Correct mocks, deliberately incorrect mock controls, full cumulative tests, immutable native VICE, and all real-emulator gates remained mandatory during the waiver. It was a temporary review-timing waiver, not a passing verdict, not done-state authorization, and not approval of an otherwise unapproved plan. Same logical Claude review session `cd19ecc2-b410-4b7c-8a8a-dca9bbb6b008` could be resumed at noon where supported. Receipt prefix: `docs/receipts/hv/20261008T073800-0500-phase1-oracle-contracts-opus`, including `.resume-01.*`. Resume 01 exited 1 with a provider session-limit error, not a verdict. No live reviewer remained at the last check of that section. Recheck before launching to prevent duplicates. Preserve `.resume-01.metadata-correction.json`, which corrects the inherited `freshSession` flag without rewriting the original request.

This 08:19 section says it supersedes conflicting current-state, review-timing, and next-step statements below it in source A. Historical acceptance and failure records remain intact.

### Stock build and snapshot feasibility, 2026-10-08T09:17:17-05:00

Phase 2 was not complete at 09:17. External adapter `src/ViceSharp.ViceCompatibility/X64ScSnapshotCompatibility.cs` normalizes validated C64SC VSF 2.0 MAINCPU 1.2 and 1.3 bytes without native edits or file I/O. The mock suite passed 38/38 before implementation. The combined mock and implementation byte suite then passed 77/77, and the prior oracle-contract suite passed 82/82. All were full unfiltered Release runs with zero failed, skipped, or aborted. These 159 tests are native-safe unit and byte evidence, not native resume or parity acceptance. The adapter was not yet wired into production snapshot paths at that writing. Preserve the failed first mock run and the corrected C64-to-C64SC fixture history. See `tests/ViceSharp.VsfCompatibility.Tests/README.md`.

Evidence root: `docs/receipts/native-vice/20261008T082100-0500-phase2-feasibility/`. It includes mock and test TRX and logs, per-build and per-test native-integrity snapshots, source pins, copied-fixture provenance, requirement before and after readbacks, traceability output, and stock build receipts. Traceability at this root is the second measurement in conflict C7.

Stock upstream build completed with exit 0, PID 36708. See `stock-build-02-result.json` and retained stdout and stderr. The first configure failed for missing libcurl. The exact upstream-documented `mingw-w64-x86_64-curl` prerequisite was installed, exit 0. `pkg-config` reports 8.17.0. No VICE patch was applied. All 3,847 original tracked files matched their pre-build hashes after build and runtime probes. Compiler warnings were retained, not suppressed, and not claimed absent.

Stock runtime receipts are under `stock-runtime/`. Read its `README.md` before repeating probes. Both xvic and x64sc report VICE 3.10 and execute native monitor save, load, and step at BASIC-ready checkpoints, exit 0. Registers and clocks matched the uninterrupted one-instruction runs, but complete VSF bytes did not: xvic VIA and drive modules and x64sc SID and drive modules differ. The reset-boundary xvic load stalled and was terminated. Preserve that failed edge case. The real external adapter normalized the retained MAINCPU 1.2 fixture, and stock x64sc recovered all seven checked CPU fields and executed another instruction, exit 0. This is runtime evidence for that fixture only, not full device, v1.3, managed, or lockstep parity. Text-monitor `undump` ignores its return status. Use the public binary-monitor acknowledgement for fail-closed automation. Do not turn exit 0 alone into a snapshot acceptance gate.

Latest cumulative native-safe suites at that increment: 159/159 passing, failed, skipped, and aborted 0. `runtime-cumulative-vsf-02/result.json` (77) and `runtime-cumulative-oracle-03/result.json` (82). No production code changed in that runtime-probe increment. `InvokeNormalizer.cs.txt` is only a diagnostic byte-array invocation bridge to the already-tested production method, because PowerShell cannot box `ReadOnlySpan<byte>`. It contains no second normalizer. No probe process remained running.

Incidental MCP requirement timestamp anomaly accepted by triage as `triage-report-e85d4371f54c4751959e723de7b46b02`, group `triage-group-0420cc09f47afcbd`. Failsafe retained at `.mcpServer/failsafe/Codex/workspaces/RjpcR2l0SHViXHZpY2Utc2hhcnA/pending/20261008T134738Z-requirements_createdat_projection-17ec.yaml`. Do not blindly replay stale session records while reconciling it. No MCP repair was in scope.

Primary `TR-VSFLOCKSTEP-RESUME-001` AC2 reflects the approved upstream build procedure. Readback confirms AC1 and the description unchanged, both ACs unsatisfied, status pending. Before and after records: `docs/receipts/native-vice/20261008T082100-0500-phase2-feasibility/resume-tr-before-upstream-build-approval.json` and `resume-tr-after-upstream-build-approval.json`. Include this approval in the noon HV scope. Recorded at 2026-10-08T08:31:47-05:00.

The 09:17 state is retained as chronological evidence. Later 8 October sections supersede it for current counts and the acknowledgement next step.

### Binary monitor, clock wrapper, gate, and bus samples, 2026-10-08 morning

Turn for the binary-monitor and debugger block: `Codex-20261007T135626Z-plugin-session` / `req-20261008T142219Z-prompt-bc0b`.

The explicit binary-monitor acknowledgement gap is covered by stock xvic and x64sc runtime receipts. Both acknowledged snapshot save and load and instruction steps. Both returned native error `0x8f` for missing files, rejected by the production guard. Stock x64sc explicitly acknowledged loading the normalized legacy 1.2 fixture with PC `$5769`. Both probe processes exited 0. Latest full native-safe suites then were 199/199 plus 82/82, zero failed, skipped, or aborted. The new 61 mock cases and prior suites passed before the codec implementation. Receipt: `docs/receipts/native-vice/20261008T082100-0500-phase2-feasibility/binary-runtime/README.md`.

Full native snapshot state still differs. VIA differences match the pinned writer and reader CABSTATE mask asymmetry. SID and drive field mappings are identified, but their exact runtime causes remain unproven. Do not patch VICE, mask comparator differences, or confuse native-to-native differences with managed divergence. Whole-snapshot identity is diagnostic, not a newly added requirement.

2026-10-08T10:09:08-05:00: two local CDB hardware-breakpoint probes produced zero stops and zero hits, so neither is passing cycle-control evidence. The clock-write watchpoint and follow-up execute watchpoint were shown enabled, and runtime entry disassembly matched the pinned symbol, but the cause remains unknown. Both verified owned debugger and emulator pairs were stopped. No probe remained running. `debugger-runtime/README.md` under the Phase 2 receipt root retains exact launch commands, failure logs, source limitations, and integrity readback. Do not conclude that every external-control mechanism is impossible from these two failures.

Clock wrapper, turn `req-20261008T151514Z-prompt-9817`, 2026-10-08T10:52:40-05:00. The external `vic_cycle` link wrapper forwards the real native clock after calling the real callback exactly once. Mocks passed 6/6 with prior 199/199 and 82/82 before C implementation. Same-assertion compiled-C bindings then passed with 12/12 plus the same prior suites: 293 cumulative native-safe tests, zero failed, skipped, or aborted. These are not full real-emulator acceptance tests. `tests/ViceSharp.NativeClockProbe.Tests/README.md` indexes the chronology, commands, and retained failures.

The separate wrapped stock-xvic executable links 99 unchanged stock inputs plus external objects. Two `-limitcycles 100000` runs emitted 99,998 actual callback-clock observations each, byte-identical stdout, including the initial `7,8,7,8` reversal. All four short and long baseline and probe runs exited 1 and explicitly logged `cycle limit reached` in stdout. None timed out. The cycle limit is instruction-boundary termination, not deterministic per-cycle pause and resume. Exact receipts: `docs/receipts/native-vice/20261008T082100-0500-phase2-feasibility/clock-runtime/README.md`.

Address, data, and RW, dummy versus real, interrupt stage, stolen cycle, and cycle-boundary pause and resume remained unproven at 10:52. The later manual debugger interruption exposed a running CPU and clock but did not prove a hardware breakpoint hit or explain the prior failure. Fast fetch bypasses memory tables. Do not fabricate addresses or data from PC or history. No filtered clock reversal, no native patch, and no comparator exclusions.

After recovery at 10:50, profile reload is the count in conflict C8. Console #16116 retained the correct workspace, cache, and queue routing.

Control gate, same turn `req-20261008T151514Z-prompt-9817`, 2026-10-08T11:10:31-05:00. External callback-permission gate `tools/immutable-vice/clock-gate.c` and `.h` publishes the actual clock, flushes, and requires one lowercase `c` to return to native execution. EOF, quit, invalid input, and I/O failures return distinct nonzero statuses. It was written after 19 new discriminating mock fixtures passed within a 31/31 clock suite and prior 199/199 plus 82/82 suites. The same fixtures bound to compiled C then passed within 50/50, with prior suites again 199/199 and 82/82. Cumulative 331 native-safe tests, zero failed, skipped, or aborted. No full real-emulator acceptance claim.

Seven real separately linked stock-xvic probes completed without forced termination. Two runs granted 16 permissions and got 17 records each, with matching prefixes also matching the ungated callback prefix. Quit returned 67, EOF 66, invalid, uppercase, newline, and NUL returned 68. Every terminal case stopped without another callback. Across 39 withheld-permission intervals there was no next record. Minimum measured 249.6824 ms. These are narrow post-callback controls, not an all-device freeze or complete half-cycle or bus proof. Evidence: `docs/receipts/native-vice/20261008T082100-0500-phase2-feasibility/gate-runtime/README.md`.

Observation, turn `req-20261008T161608Z-prompt-c06c`, 2026-10-08T11:49:07-05:00. The prior control turn completed with primary readback. Post-startup external CDB hardware attachment works. The unchanged stock xvic produced actual packed fast-fetch before and after register results, 128 consecutive clock-write observations, and 20 actual normal-store argument observations. All 39 fast-fetch pairs have host RIP +3 and the same clock. This is partial diagnostic evidence, not a complete bus decoder or parity gate. Regular and dummy reads, interrupt stages, stolen cycles, deterministic start, and full cycle alignment remain unproven. Zero dummy-store hits in this sample does not establish coverage or absence. CDB exited 0 through `q`. Native target exit -1073740972 is debugger termination, not normal shutdown. Receipts: `docs/receipts/native-vice/20261008T082100-0500-phase2-feasibility/bus-runtime/README.md`. Fixture entry-watchpoint and recursive-trace failures are indexed in sibling `debugger-runtime/README.md`.

Source B's summary of this feasibility work: external stock builds, binary-monitor save, load, and step acknowledgements, debugger fast-fetch, `vic_cycle`, and clock-permission probes prove bounded feasibility only. Optimized or inlined paths, repeated clocks, missing dummy hits, and debugger termination cannot be promoted into complete half-cycle observation. Retain failed debugger attempts, empty gate stdout, the reset-boundary hang, and snapshot-module asymmetries.

Source inspection shows instruction stepping in `monitor_binary.c` and a fast opcode-fetch path in `vic20cpu.c`. Do not mislabel instruction history or memory peeks as complete per-cycle bus evidence.

### Interrupted noon catch-up, 2026-10-08T12:55:41-05:00

The noon local Opus 5.5 max catch-up stopped with exit 1 and HTTP 429 `usage_limit_reached`. Provider message: `You've hit your session limit`. Reset: `4:50pm (America/Chicago)`. PID 117120 has exited. Do not treat the old monitor or its 72 percent progress estimate as a running or accepted review. The monitor `collect-vicesharp-opus-catch-up-verdict` was deleted after terminal capture. No automatic retry was started.

Receipt prefix: `docs/receipts/hv/20261008-noon-catchup-opus.resume-02`. The full response has 1,438 valid JSONL events, zero parse errors, 3,276,225 bytes, SHA-256 `5CF0CC2C842576EF145CB23522D0C7FFE37ED321AA3C67608C6A3D4470CC607A`. Its last three tool calls contain 43 claims (37 PASS, 5 FAIL, 1 UNKNOWN), 16 findings, and a proposed DISAGREE with accuracy 96 and completeness 94. The claims and findings calls succeeded. The verdict-construction call was rejected by PowerShell.MCP before execution. No finalized verdict or completed reviewer-turn readback was obtained. Preserve that distinction. `interrupted-draft-recovery.json` retains the full literal tool inputs and results and structured claims and findings, extracted with the PowerShell parser without executing the recorded commands. `terminal-summary.md` indexes the findings and next actions.

The entire 73,039-character recovery artifact is saved in nine primary MCP dialog chunks on `Codex-20261007T135626Z-plugin-session` / `req-20261008T172748Z-prompt-d3fd`. Readback matched every chunk, allowing only CRLF-to-LF normalization. See `.partial-draft-primary-proof.json`. This proves parent preservation, not reviewer completion or acceptance. The first single large wrapper call failed before starting its child because serialized parameters exceeded the Windows command-line limit. Smaller supported wrapper calls succeeded. Triage accepted `triage-report-ad5cb20268a041dfb8d5fefa6f231228`. Supported failsafe `20261008T175401Z-wrapper_argv_length-a2a6.yaml` is retained. Do not blindly drain existing queues.

Reviewer rerun TRX files show 82 + 199 + 50 = 331 native-safe unit tests passed, zero failed, notExecuted, or aborted. Post-terminal rehash at 12:51:08 confirms all eight native baseline fields, all 3,847 original materialized source files, 99 stock link inputs, and both stock executable hashes unchanged. Of 826 launch inputs, 825 are unchanged. The sole change is the original reviewer-progress JSONL: Opus appended its timestamp correction. Hashing its original 1,593-byte prefix reproduces the launch hash exactly. The handoff update after that post-terminal check is an additional disclosed pin change for any future review.

Unaccepted read-only source and binary findings are in sibling `.post-launch-source-addendum.json`, `.post-launch-interrupt-addendum.json`, and `.post-launch-reset-clones-addendum.json`. They correct optimized read-helper coverage and constrain actual interrupt, reset, and stolen-cycle observation. They are not a completed bus decoder or proof of parity. No product code, test code, native source, acceptance state, commit, or push changed during this recovery. Do not advance implementation gates on the draft. Source B says this interrupted draft DISAGREE 96/94 is not a terminal verdict, and that the 826-input packet is retained evidence, not current approval.

The 12:48 reload is conflict C8.

### Same-day remediation after the interrupt

Each of these changed inputs after the interrupted review. Repin and disclose them before the same Opus max logical review resumes. None of them is full finding resolution, HV acceptance, a phase advance, a production or native edit, a commit, or a push. The quota-blocked review and the whole original goal stayed open. Upstream-required build generation stayed in the separate pinned materialization.

Mock remediation, 2026-10-08T13:05:32-05:00, turn `req-20261008T175952Z-prompt-f7fb`. Within the unaccepted Phase 1 mock gate, draft F-07 and F-08 were addressed with four independent lifecycle controls (wrong receipt and wrong postflight argument, each for unchanged and changed inventory) and an all-zero test-receipt rejection. Every new lifecycle case first accepts the correct mock transcript and then rejects exactly one corrupted argument. Original tests and assertions remain. Full native-safe runs: oracle 87/87, VSF 199/199, clock 50/50, total 336 passed, failed, notExecuted, and aborted 0, all exit 0. Native inventories unchanged across every build and run. This is local mock validation, not independent acceptance or real-emulator proof.

New F-09 scenario ledger maps 16 missing, unknown, or gap PHI2/AEC cases to missing-observation ACs, and the other 18 only to `TR-CYCLE-001` AC4. It contains all original 82 test names and five new cases. The original reviewed ledger is preserved byte-identical. Index: `docs/receipts/native-vice/20261008T130200-0500-phase1-mock-remediation/README.md`. F-10 reusable fixtures and provider binding remained open at 13:05, before the 13:33 extraction. Continue remediation within the existing gate. Do not infer another implementation waiver from the quota failure.

Fixture extraction, 2026-10-08T13:33:57-05:00, turn `req-20261008T181223Z-prompt-3ff5`. F-10's test-only extraction is locally verified within the existing unaccepted Phase 1 gate. `tests/ViceSharp.OracleContract.Tests/OracleContractFixtures.cs` holds shared rows, input builders, and independent expectations. `ContractAssertions` exposes supplied-provider assertions for decisions, boundary resolution, comparator results, and audit call history. Boundary requests no longer contain a pre-resolved path. The returned path is asserted independently, with a new wrong-path control in each of the ten boundary cases. Real filesystem resolution is still unimplemented and unproven and remains a mandatory real-binding gate.

All three native-safe suites ran unfiltered in Release: oracle 87/87 at 13:29:50, VSF 199/199 at 13:29:58, clock 50/50 at 13:30:06 (all 2026-10-08 -05:00), total 336, exit 0, failed, notExecuted, aborted, error, and timeout 0. Every build and run retained matching native before and after inventories. All 87 TRX names and 85 ordered theory rows match the pre-refactor cases exactly. Seventeen static token comparisons preserve moved inputs, expectations, assertions, and lifecycle controls. Original sources and ledgers are retained. Current source copies and both current C compiler logs are copied and hash verified. Index: `docs/receipts/native-vice/20261008T131300-0500-phase1-fixture-reuse/README.md`.

Latest reload finished 2026-10-08T13:25:44-05:00 is conflict C8.

Evidence corrections, 2026-10-08T13:50:54-05:00. Draft F-04, F-05, and F-06 descriptions were corrected from direct receipt readback. All seven original gate stdout files are empty. Neither the README nor the diagnostic adapter's header now claims retained native messages. Only the header comment changed. Bytes from the first `#include` onward are identical. A separate object compiled successfully using the passing test runner's child environment convention. The first direct compile exited 1 with no object and an empty log. Both attempts are retained, and the first cause is unproven. The existing gate executable and runtime behavior were not changed or rerun. Native stdout retention and mock-first stdio adapter coverage remain open, not waived.

Clock documentation now cites the native RESET message between the first and second `7,8` pairs in all three wrapped stdout logs, explicitly as correlation rather than causal or reset-event proof. Binary-monitor chronology now includes `binary-mock-01` at 09:28:37 (125/125: 77 prior plus 48 protocol mocks), the exact 13 additional boundary and error cases, `binary-mock-02` at 09:30:20 (137 passed, 1 fixture-parser failure), and corrected `binary-mock-03` at 09:31:04 (138/138). Original TRX and logs were preserved.

F-11's unretained pre-refactor clock-helper bytes and historical clock compile log remain missing evidence. Later passing runs and current copies do not reconstruct them. Source B says unavailable pre-refactor source and compiler evidence is still carried forward until a later disposition is verified.

For F-14, live MCP confirmed 11 decision dialogs and zero matching actions across four completed historical turns. The supported session workflow treats completed turns as immutable. Eleven explicitly current corrective action and dialog pairs now carry the exact original decisions and source linkage on `req-20261008T184031Z-prompt-336a`. Primary readback matched all 11 pairs. Full primary turn JSON comparisons confirm all four source turns remain unchanged. This is forward audit repair, not retroactive compliance or independent acceptance of the disposition. Source B calls these forward-only logging corrections and says they stay unresolved until their actual later disposition is verified.

Index: `docs/receipts/native-vice/20261008T134300-0500-hv-evidence-corrections/README.md`. It retains pre-edit copies, exact original log and TRX hashes and excerpts, compile attempts, corrective primary readbacks, and native integrity.

Routed triage, 2026-10-08T13:58:02-05:00, turn `req-20261008T185607Z-prompt-9c0d`. The direct evidence missing from draft A21 is available. The supported read-only `mcp__mcpserver__triage_status` tool, scoped to `F:\GitHub\McpServer`, returned both exact report IDs: `triage-report-70a8021a03c34270bacfde4f1c7e98c6` in group `triage-group-67df6e92fea64c1d`, and `triage-report-e85d4371f54c4751959e723de7b46b02` in group `triage-group-0420cc09f47afcbd`. Both responses have `status: grouped`, `originalWorkspacePath: F:\GitHub\vice-sharp`, and `workspacePath: F:\GitHub\McpServer`. This proves report existence and routing directly, not through parent session descriptions. Grouped does not mean either underlying bug is resolved. Exact primary response bodies and hashes: `docs/receipts/native-vice/20261008T135800-0500-routed-triage-readback/primary-report-readback.json`. No triage report, server configuration or code, native source, historical review, or acceptance state was modified. Supply this to the same Opus max review and let the reviewer reassess A21. Do not rewrite its original UNKNOWN or claim hostile acceptance.

Secret exposure, 2026-10-08T14:16:11-05:00, turn `req-20261008T190255Z-prompt-b8db`. Draft F-02 remains open pending operator disposition and independent review. A read-only comparison found that the active credential label in Global `MEMORY-LAB-002` version 5 equals the local Tailscale credential's `keyId`, not its full `authKey`. Three selected retained tool outputs contain those identifiers. None of the 22 selected tool outputs contains the current full local Tailscale auth key or the current workspace API key. This is bounded evidence, not an audit of every output, and not a claim that credentials were never exposed elsewhere. No key or identifier values are included in the new assessment receipt. The retained 07:15:14 marker output contains a redaction placeholder, so the historical key cannot be compared with the current key. The current marker at that writing records server startup at 09:13:49 and documents rotation on restart. Historical-key invalidation remains an inference, not direct verification. No provider validity query, credential change, service restart, cloud operation, native-source edit, or acceptance-state change was performed. The operator was asked whether to leave credentials unchanged with this uncertainty recorded or to prepare a rotation plan for approval. Do not interpret silence as authorization. Sanitized evidence: `docs/receipts/native-vice/20261008T141611-0500-secret-exposure-assessment/assessment.json` and sibling `README.md`. Source B says the credential-exposure disposition and the public-web versus local-only deviation questions remain unresolved until a later verified disposition.

Stdio mocks, 2026-10-08T14:28:11-05:00, turn `req-20261008T191836Z-prompt-faf5`. F-12 now has 19 reusable mock-contract cases in `tests/ViceSharp.NativeClockProbe.Tests/StdioAdapterContractTests.cs`, with a supplied-provider assertion for identical later C binding. Correct mocks pass. Independent wrong return or exit state, null, missing, extra, or duplicated events, operation, stream, or value changes, and adjacent-order reversals are asserted to fail inside each passing test. `STDIO-ADAPTER-SLICE.md` was written first and explicitly separates this diagnostic protocol from native stdout retention and production integration.

Unfiltered Release runs passed clock 69/69 at 14:25:18, VSF 199/199 at 14:25:57, and oracle 87/87 at 14:26:16 (all 2026-10-08 -05:00), total 355/355, exit 0, and failed, notExecuted, aborted, error, and timeout 0. Prior 50 clock cases are preserved. Every build and test retained unchanged native pre and post inventories. Final eight baseline fields, 3,847 stock source files, 99 link inputs, and the original gate executable match. Both current C compiler logs and source and preimage evidence are retained in `docs/receipts/native-vice/20261008T142142-0500-stdio-mock-contract/`. The actual stdio adapter and production C code were not edited or newly bound. Real adapter binding, stdout retention, and broken-pipe behavior remain mandatory and unrun. This is not full F-12 resolution or a phase advance. Source B says historical oracle, VSF, and clock-stdio runs reported 87, 199, and 69 tests, and that the old 355 aggregate is mock evidence in its recorded scope, not a fresh run and not proof that all current mock and real obligations passed. Shared-fixture, negative-control, and stdio remediation receipts are preserved in source A.

Opus remediation packet preparation, 2026-10-08T14:42:20-05:00, turn `req-20261008T193152Z-prompt-658a`. `docs/receipts/hv/20261008T143538-0500-opus-remediation-packet/` prepares continuation of the same Opus max logical review. It is not a launched review or a verdict. It preserves all 16 original findings, separate parent dispositions, old and current evidence hashes, and the full earlier scope. The original 826 inputs initially revalidated as 815 unchanged, 11 changed, zero missing. Final revalidation records changes after this handoff update. No quota retry or automatic retry schedule was started. The reported provider reset remains 16:50 local, with retry authorization unresolved. Any later launch must recheck both authority and current evidence.

F-13 now has direct current runtime metadata: `gpt-6-astra`, effort `xhigh`, turn_context at 2026-10-08T19:31:11.167Z. This is not a claim about every historical turn. Codex wrapper 1.118.0 line 123 hardcodes `MCP_SESSION_MODEL=codex`, and updateTurn has no model field. Accurate identity is recorded in current dialog, action, and tags. Structured placeholder fields are not repaired. The incidental defect has direct grouped readback as `triage-report-13c287e9d2034e07ace0442a9c03697c`. Normal failsafe is retained. No plugin or historical-turn mutation.

Operator questions pending at that writing: F-02 credential disposition; preserving or quarantining the four old reviewer queue records ending `2c6e`, `6e35`, `f583`, and `a8c8`; and F-03's documented web-policy deviation and local-only correction. No permission is inferred from silence. Proposed review continuation uses a fresh MCP cache and failsafe root and the same Claude CLI conversation. Its ability to preserve history must be verified at actual launch. Source B says carry forward the four historical reviewer-queue disposition questions, the credential-exposure disposition, and the public-web versus local-only deviation questions.

Source B's remaining-task list for this boundary, still open as of 2026-10-13: full native-safe mock acceptance; external capability and provenance; safe migration of preexisting native changes without changing VICE; native-safe TestHarness builds; deferred comparator XMLDoc correction; actual `REAL-C64-PHI2-AEC-001`; all real parity gates.

Use the existing single `LockstepValidator` for every half-cycle: A, X, Y, S, P, PC, opcode and micro-op, bus address, data, and direction, dummy versus real access, IRQ and NMI pipeline, and stolen-cycle and BA. Repair foundations first. Then one VICE-macro per-cycle CPU engine across 256 opcodes, decimal ADC and SBC, indirect JMP wrap, and JAM. Then machine glue, VIC-I, VIA, IEC, input, storage, tape, audio, side effects, reset, snapshot, determinism, and pacing. No new phase booleans or sample-specific CPU exceptions. Maintain real C64 `First10000` and `First100000` pins on CPU or clock edits. Missing filters or oracle prerequisites fail, they do not skip. Wolf64 campaigns remain excluded except the stated regression pin.

## Canonical native VSF

The operator made native VICE snapshot capability mandatory, then clarified that VICE VSF has been the ViceSharp snapshot-format requirement since day one. It is the canonical default, not secondary or opt-in. Do not claim Phase 2 complete until every normal public and desktop snapshot path uses native x64sc C64 or xvic VIC-20 `.vsf`, and load, save, bidirectional round trip, and deterministic continuation satisfy `FR-SNP-005`, `TR-LOCKSTEP-VSF-001`, and `TEST-SNP-002`.

Managed VSF resume support already exists and must be preserved:

- `LockstepValidator.RunFromSnapshot` loads native `.vsf` state and starts managed and native comparison.
- `LockstepValidator.StageManagedFromNative` transfers RAM, written 6510 processor-port direction and data, CPU architectural registers, VIC-II state, and both CIAs into the managed C64. It does not import the exported native opcode or micro-cycle, BA-low, pending-interrupt, IRQ or NMI clock, or interrupt-delay fields. `InjectSnapshotResumeState` resets in-flight execution, so this is a measured first-cycle reconstruction, not complete native pipeline transfer.
- `Mos6502.InjectSnapshotResumeState`, `Mos6569.InjectSnapshotState`, and `Mos6526.InjectSnapshotState` are production chip hooks used by that path.
- `IViceNative`, `ViceNative`, and `ViceNative.Xvic` already expose native snapshot read and write operations.

The gap is not the absence of managed VSF support. The existing bridge remains in the test harness, is incomplete for all represented machine and peripheral state, lacks complete xvic staging and managed-to-native save transfer, is not exposed by `SnapshotServiceHost`, and was displaced by proprietary formats as the normal capture, file, quick-slot, and named-slot behavior.

The corrected plan is `docs/plans/PLAN-VSFLOCKSTEP-001.md`. It promotes and completes the existing bridge, keeps native VICE as the version-specific VSF reader and writer, and restores `.vsf` as the canonical default across supported public and Avalonia desktop snapshot paths. It does not propose a replacement managed VSF codec. The two existing proprietary readers move behind the explicit protobuf `ImportLegacySnapshot` operation for one-time migration only. Normal restore rejects them. No capture path emits them. Replay remains separate. The plan locks closed model selectors, typed immutable state, both native-DLL lease ABIs, exact current and legacy VSF framing, transactional load and save, the Avalonia file, quick, and ten-slot surface, fixture, toolchain, and ROM inputs, cumulative BDPv4 gates, and post-publish `win-x64` MSI staging and install proof. No native snapshot product implementation has started under it, as of the 2026-10-03 correction. Source B says the plan and `PLAN-VSFLOCKSTEP-001` remain open, and that tape HV is not VSF plan approval.

The first 2026-10-03 plan hostile review was cancelled after the canonical-format correction. `docs/receipts/hv/20261003T131341Z-vsf-plan-corrected.response.jsonl` has 61 events and no `turn.completed` event. It is an incomplete receipt and cannot authorize Gate 0 or any done-state change.

Next completed review: `docs/receipts/hv/20261003T133142Z-vsf-canonical-plan.response.jsonl`. DISAGREE, accuracy 94, completeness 82, pass 14, fail 7, unknown 0. Findings: an overstatement of current CPU pipeline transfer; missing interface, ownership, state, and error contracts; non-executable xvic toolchain and fixture decisions; missing red, mock, and cumulative phase stops; unresolved native singleton, cancellation, locking, session-change, and rollback-failure behavior; an impossible checksum-corruption promise and a duplicate-module gap; unspecified runtime packaging and install proof.

Fresh installed-local-Grok review: `docs/receipts/hv/20261003T144113Z-vsf-canonical-plan-local-grok.verdict.md`. DISAGREE, accuracy 89, completeness 84, pass 4, fail 8, unknown 7. It independently passed the CPU-baseline correction and found incomplete protobuf and import and desktop-slot decisions, incomplete legacy VSF framing rules, no exact ROM allowlist binding, an unrelated quarantined parity exit, ambiguous per-DLL singleton behavior, incorrect pre-publish native staging order, stale generated mappings, and missing independent evidence for the fixture, toolchain, MCP readback, selectors, and PE closure. The plan and canonical and live requirements now address every numbered item. Supporting local evidence: `docs/receipts/hv/20261003T100623-0500-vsf-gate0-local-evidence.md`. No finding is closed until another fresh review agrees.

MCP readback remains authoritative and was pending at that writing. `FR-VSFLOCKSTEP-001` identifies the supplied fixture's exact 24 modules and SHA-256 `5C284F5CF0A1BC7E01A2E4117A74158B7A9D60485F5626448F4B7655C0E23C19`, replacing the incorrect 16-module claim. `FR-SNP-005` has 9 acceptance criteria, `FR-VSFLOCKSTEP-001` has 3, `TR-LOCKSTEP-VSF-001` has 7, `TR-VSFLOCKSTEP-RESUME-001` has 2, `TEST-SNP-002` has 8, and `TEST-VSFLOCKSTEP-001` has 1. All six are pending at `layer-1`. All 30 criteria are unsatisfied. Both FRs map to both TRs and both TESTs. Readback: `docs/receipts/requirements-recovery/20261003T101735-0500-vsf-mcp-readback.json`.

Recovered legacy records stay active and pending: `FR-VSFLOCKSTEP-001`, `TR-VSFLOCKSTEP-RESUME-001`, and `TEST-VSFLOCKSTEP-001`. They are mapped into the full-capability requirement alongside `TR-LOCKSTEP-VSF-001` and `TEST-SNP-002`. The local MCP wiki projection was regenerated at `2026-10-03T15:16:14.1534405+00:00` and shows both four-link FR mappings. No legacy requirement was retired.

The FR-SNP-003 cartridge replay remediation received a narrow hostile AGREE at 2026-10-03T12:45:57Z: accuracy 99, completeness 99, pass 18, fail 0, unknown 0. Thread `01a0fdf8-fa4e-74f1-8b53-676b26f748d4`. MCP session `Codex-20261002T175731Z-plugin-session`. Request `req-20261003T122054Z-remediation-hv`. Streams: `docs/receipts/hv/20261003T121540Z-phase2-fr-snp003-remediation.request.jsonl` and `.response.jsonl`. This proves only cartridge replay value preservation. It does not prove native VSF compatibility and cannot exit Phase 2.

Source B's open gap list: full in-flight and peripheral state, xvic staging, managed-to-native save, and default host, protocol, desktop, and slot integration. Register transfer plus pipeline reset is not full resume. Native VICE remains the version-specific reader and writer. Retain external validated MAINCPU 1.2 and 1.3 compatibility and byte-preserving 1.4, not an internal patch or replacement codec. File, quick, and ten-slot paths use canonical VSF. Proprietary readers are explicit import-only. Preserve closed selectors, exact framing, cancellation, lifecycle, transaction, and rollback, ROM binding, structural corruption, duplicate, and truncation, and both-machine round trips. No invented checksum guarantee. No arbitrary compatibility caps.

Native VSF save has sound, drive, and optional tape-stream side effects. It is not a neutral getter. Separate pristine execution from capture-prelude runs without weakening canonical VSF support. VSF omits some process-static tape, audio, and RTC state. The audited RTC serial train is not serialized or rebuilt on read. Specify fresh-process and same-process native histories, including quirks.

All eight MCP tasks on `PLAN-VSFLOCKSTEP-001` remain open: plan gate; prerequisites and fixtures; production C64 staging; full C64 pipeline and media and 100,000-cycle x64sc; VIC-20 and 100,000-cycle xvic; managed capture through the native writer; canonical UI and protocol plus win-x64 MSI installation; round-trip, corruption, and cumulative gates. Packaging uses Nuke and verified post-publish native closure. No ROM redistribution.

Approval of the VSF plan is not approval to start Phase 3 (see C1 for the separate Phase 3 authorization history). A's older next step said ask for explicit operator approval of `PLAN-VSFLOCKSTEP-001` only after the hostile gate passes, and that approval authorizes Slice 1 only. That next step is one layer of conflict C3.

## Tape amendment, midnight HV, and the R0 candidate

### Midnight HV is terminal

Do not duplicate it. Review receipt: `docs/receipts/hv/20261010T161500-0500-tape-amendment-opus/resume-20261011T000300-0500/README.md`. Complete verdict: `verdict.md` in that folder. Structured verdict: `verdict.json` in that folder.

Single retry resumed CLI session `09cb215d-0e89-4272-aa11-a66514b50ea0`, launched `2026-10-11T00:04:40.3850986-05:00`, PID 33052. Terminal receipt `2026-10-11T00:54:29.1068954-05:00`: exit 0, `claude-opus-5-5`, requested effort `max`. Successful process execution is not approval.

DISAGREE, accuracy 93, completeness 84. 35 PASS, 6 FAIL, 0 UNKNOWN, 1 N/A. Failed assertions:

1. A8: two D64 mapping targets did not exist.
2. C3: tape AC omitted behavior and hidden state.
3. C4: named mocks did not discriminate running-motor start and before-deadline cancellation.
4. C5: contradictory CPU, tape, and performance requirements, and dependent media requirements.
5. C9: incomplete target checks, stable-field handling, and value-bearing application guards.
6. D4: stale handoff and TODO tracking.

All 18 new findings and 17 prior dispositions remain in the verdict. Preparation and the handoff edit do not close them by self-assessment.

Fresh primary readback at 2026-10-11T11:21:17-05:00 verified completed reviewer turn `ClaudeCode-20261011T050449Z-plugin-session` / `req-20261011T050452Z-prompt-ba39`. Eight prose chunks reproduce 42,236 LF-normalized characters. Eleven JSON chunks reproduce the file except one terminal LF. Parent `req-20261011T050131Z-midnight-opus-retry` holds the exact 50,824-character final response in nine chunks. Stream: 2,707 events, zero parse errors, SHA-256 `BA6DFC9643C571306D09070DF0F80297A30B4B319B40B398B1DF10EE828D14C2`, empty stderr. Original captures were closed before terminal parsing.

Original pre and post input checks: 146 of 146 unchanged. At 11:21, 145 remained unchanged. Only the regenerated marker differed. This is not a review of later drafts. Midnight automation is paused. No quota loop. No duplicate retry. Preserve N-16 and N-17 disclosures: historical duplicate and unfinished hook turns, estimated interim reviewer timestamps, and direct .NET receipt writes. Do not claim blanket workflow compliance.

### Sixteen-record R0 candidate

Candidate: `docs/receipts/requirements-recovery/20261013T231703-r0-anchor-remediation/README.md`. `proposed-field-delta.json` SHA-256 `63386E9251185B101F53E9257CDA9270025A68006B83F243314E6E299A55D702`. Unapproved and unapplied. It replaces round 1 and the prior 14-record candidate, whose hash `320887C5F2F09154F1EC7970B041A638632741A7750E838FE05F77883BB1DA83` is historical. Apply one eventual reviewed version. Never apply overlapping drafts successively.

Sixteen existing records: `FR-RUNTIME-TAPE-002`, `TR-TAP-EDGE-001`, `TR-TAPE-EDGE-001`, `TEST-TAPE-RAMP-001`, `TEST-TAPE-SENSE-001`, `FR-TAP-001`, `FR-TAP-002`, `FR-TAP-003`, `FR-TAP-004`, `TEST-TAP-001`, `FR-BACKFILL-MEDIA-001`, `FR-CPU-004`, `FR-PRF-003`, `FR-TAP-005`, `TEST-CPU-001`, `TEST-PRF-001`. Retain all 66 original AC IDs. 104 proposed AC, 56 rewritten texts, 10 unchanged, 38 added. Eight unchanged CPU and SX-64 criteria and the broad existing TEST conditions remain mandatory. Nine full mapping rows contain six proposed changes and three unchanged rows (`FR-RUNTIME-TAPE-002`, `FR-CPU-004`, `FR-PRF-003`). 13 references added, none removed.

Preparation update, 2026-10-13T23:21:05. The source sentence is concatenated. The numbers in it are: round 3 preserves the 16-record payloads from round 2, including unchanged original 14, and all 9 mapping rows. Only coverage anchors and receipt metadata change. It has 104 unsatisfied AC and 117 named groups, 6 changed plus 3 unchanged mapping rows, 13 added and 0 removed references. 178 document checks pass, plus 10 independent expected-rejection controls, including missing CIA and VIA anchors. These are not emulator or coordinator mock gates. All 32 AC-text-only groups remain mandatory R2 work.

Exact anchor set: `FR-RUNTIME-TAPE-002/ac-02`; `TR-TAP-EDGE-001/ac-02`; `TEST-TAPE-SENSE-001/ac-02`; `FR-TAP-003/ac-01` and `ac-02`; `TEST-TAP-001/ac-01` and `ac-03`; `FR-TAP-005/ac-01`, `ac-03`, `ac-04`, `ac-05`, and `ac-06`. `FR-TAP-005/ac-02` loader-specific evidence remains required at R2. Candidate SHA-256 is the same `63386E9251185B101F53E9257CDA9270025A68006B83F243314E6E299A55D702`. Round 2 handoff copy: `docs/receipts/requirements-recovery/20261013T231703-r0-anchor-remediation/HANDOFF.round2.md`, SHA-256 `59B479B66BF781B2B67AB2C691C283C67B5AB23C7A5DBA4E783C5F6F477BE3FA`, retained.

Preparation evidence, not accepted executable gates:

- Initial remediation `docs/receipts/requirements-recovery/20261011T065515-0500-tape-hv-remediation/README.md` and state-composite predecessor `docs/receipts/requirements-recovery/20261011T072531-0500-tape-state-composite/README.md`: retained history and source and finding reasoning.
- Media fixtures `docs/receipts/requirements-recovery/20261011T083152-0500-media-fixture-specification/README.md`: 20 planned groups, 16 in-memory recipes, 13 source pins. Not successful native loader runs. D64 and drive dependencies require explicit approved scope.
- Motor and alarm matrix `docs/receipts/requirements-recovery/20261011T085402-0500-tape-motor-matrix/README.md`: 64 vectors, 15 groups, 366 declared projection cases, not executed.
- Recording and wiring `docs/receipts/requirements-recovery/20261011T092059-0500-tape-recording-wiring-contract/README.md`: remaining 26 named runtime groups, 117 source-derived examples, 14 source pins. The combined 41 names do not establish complete tape acceptance.
- Test disposition `docs/receipts/requirements-recovery/20261011T095710-0500-tape-test-disposition/README.md`: 63 methods in 13 files. Adjacent-body audit `docs/receipts/requirements-recovery/20261011T115707-0500-adjacent-tape-test-disposition/README.md` adds 47 methods in ten files and nine lexical classifications. These are retained assertion and disposition inventories, not accepted full-suite execution. Do not delete, weaken, or skip real tests without explicit disposition.
- Requirements API guard audit `docs/receipts/requirements-recovery/20261011T102600-0500-requirements-api-guard/README.md`: typed-field and mapping semantics, 18 specified groups, 69 unexecuted negative controls. Not a proven coordinator or runtime fault matrix.
- Tape observation contract `docs/receipts/requirements-recovery/20261011T110100-0500-tape-observation-contract/README.md`: 14 upstream blob-matched sources, 14 local inputs, three tool or binary pins, ten capability groups and ten conformance groups, 44 unexecuted controls. SHA-256 `2D1692BCBD8E8DD922CE40C483A7DC41CF470E80565C1E357021BFB594D449CD`. Eighteen document and source checks are not mock or native tests.

Critical source findings to carry into the packet:

- The external observer is clock-only, not complete tape, bus, or CPU-pipeline observation.
- `alarm_set` is inline, without an external relocation in the inspected datasette object. Observing `alarm_new` returned state is only a candidate. Final queue samples cannot prove every transient schedule or unset, or noninterference.
- Native VSF save side effects are stated in the VSF section above.
- A bounded search found no standalone TAP or T64, and no five named loader references, in its stated scope. That is not universal absence across archives, external assets, or generated assets.
- Typed records expose fields omitted by projections, including linked use cases. Read timestamps may be synthesized. Compare stable fields with presence and type awareness. AC lists are ordered. Mappings are ordinal duplicate-free sets.
- Mapping helper source can catch a failed prior read and then replacement-upsert, risking lost links. Source-only triage `5deae93eb0764736ae97184817d14eb6`, group `320f2143bb2556d0`, was accepted in McpServer. Not a fault-injected reproduction or a repair.
- A database batch transaction is not cross-API atomicity and is not protection for a pre-lock read and merge. R1 needs verified conditional writes or an explicitly coordinated exclusive-writer window. Do not stop other agents implicitly. Lost acknowledgements require complete actual-state readback, not a blind retry or rollback.

### Accepted R0 review, then pause

Latest verdict: `docs/receipts/hv/20261013T232143-r0-anchor-opus/verdict.md`. AGREE, accuracy 99, completeness 98. 49 PASS, 0 FAIL, 0 UNKNOWN, 1 N/A. Both original blocking findings and the subsequent anchor-sweep finding are resolved for the sole current 16-record R0 candidate. Seven nonblocking information or optional findings remain preserved in the verdict, not silently fixed. Do not launch another reviewer. Do not apply requirements or implement the application coordinator without a new operator instruction.

The reviewer completed its own MCP turn `ClaudeCode-20261014T042517Z-plugin-session` / `req-20261014T042520Z-prompt-6807`. Full persistence receipt: `docs/receipts/hv/20261013T232143-r0-anchor-opus/verdict.persistence.json`. Authoritative Markdown V2 (6 chunks) and JSON (3 chunks) read back exactly. Parent independently confirmed this and all 324 pinned inputs at 2026-10-13T23:49:18. Terminal receipt: `docs/receipts/hv/20261013T232143-r0-anchor-opus/terminal-result.json`. Process exit 0, 1,134 valid JSONL events, zero malformed lines, empty stderr. Streams flushed and closed at 2026-10-13T23:50:26. The pre-completion handoff is copied and hash-verified as `docs/receipts/hv/20261013T232143-r0-anchor-opus/HANDOFF.reviewed.md`, SHA-256 `AFB26B6BDDDDA97A9C951102A668C0D8A7AC14F84731150AD20045884D70FD86`. The accepted inputs remain unchanged. Only the live status checkpoint follows the review.

Source B's next execution sequence:

1. R0 candidate review has reached AGREE 99/98 with zero FAIL and UNKNOWN and full primary readback. No additional review is pending. Successful loader fixtures, detailed runtime matrices, observation and fault reachability, and noninterference proof belong to mandatory R2 and R3 gates. They are not a claim that execution is already complete before R0 text approval.
2. Preserve unresolved historical process and evidence findings and the exact reviewed packet. All 324 pinned inputs matched at final verification. Documents that parse or list test names are not a mock or implementation gate.
3. Remain paused. The accepted review does not complete the original plan and does not authorize an apply.
4. On a future operator resume, present the exact expanded delta and explicit idempotency and writer-window decisions for separate approval. No requirement, mapping, or status mutation before that approval. Smaller-proposal approval does not cover new AC or records.
5. After R0 approval, write guard and coordinator tests, prove discriminating green mocks, then implement and verify real safeguards. Execute only approved R1 under preimage and writer protection. Read back every field, mapping, and unchanged item. Compensating writes require prescribed approval.
6. Follow R2 and R3 and every original native-safe mock, real-emulator, x64sc and xvic, VSF, UI, and cumulative gate. No preparation shortcut waives them.

This sequence is the latest next-step layer in conflict C3.

## Phase 3 xvic parity

See conflict C1. Do not collapse these statements.

Historical handoff language and the live `PLAN-C64SET-001` record Phase 3 start authorization and narrow 3a and 3b work. Do not automatically ask again for initial authorization. Do not describe these slices as never started. Reconcile exact approval and gate provenance before further implementation. That history does not authorize the expanded requirements and does not bypass immutable prerequisites. Later R0 statements that require initial Phase 3 or Phase 4 approval conflict with this history and cannot erase it.

Historical 3a: seven prerequisite tests plus one smoke, HV 100/100. Historical 3b: one manifest-input test and one mismatch test, HV 99/99. Narrow old evidence does not establish clean immutable parity or the later strengthened unfiltered cumulative gate.

A's checkpoint and current-state sections, written as the state at those points, say Phase 3 has not started and that an explicit start sentence was still required, and that approval of the VSF plan is not that approval. Those sentences stay in the record. They are the earlier side of C1.

The older-plan resume point in A's current state is that explicit user message. `PLAN-C64SET-001` is not done. xvic parity is not claimed.

## Phase 4 settings

VIC-20-only controls hide on C64 and show on VIC-20. Retain notification cases for the selection setter, host-refresh assignment even when a later GET fails, and restore. Preserve draft preset and cart across family switches and existing bindings. Visibility flags do not prove actual Tab, focus, or RemoteControl behavior.

Historical focused run: eight passed, zero failed, zero skipped. Source A names the tests `Vic20SettingsVisibilityNotificationTests` and `Vic20SettingsUiSurfaceTests` and calls this a focused 8-pass, 0-fail, 0-skip gate. Source B adds the review: Grok 99/98 in `docs/receipts/hv/20261003T122555-0500-handoff-phase4-green-local-grok-powershell-resume.verdict.md`. Not current unfiltered acceptance. Full native-safe TestHarness and OracleContract, plus earlier applicable suites and HV, remain mandatory. `PLAN-C64SET-001` remains open for the 14-profile catalog, PAL and NTSC clock, warp, RemoteControl, and other approved work.

A's checkpoint said Phase 4 still required an explicit user sentence at that checkpoint, and that the visibility slice was implemented later. That is chronology inside C1, not a claim that the focused gate never happened.

## Phase 5 and final exit

The 2026-10-13 checkpoint is not Phase 5 completion. Final exit requires: reconciled baseline and approved deltas; an accepted whole requirement set; dense-session proof; every immutable xvic repair pair; canonical VSF; full settings gates; clean and pinned native provenance where required; cumulative zero-failure, zero-skip, and zero-abort suites; and complete accepted HV with primary readback. No missing oracle, named blocker, or preparation ledger substitutes for completion. Preserve the original approval sequence and the separate commit boundary.

## Session log, failsafe, triage, and quarantine

Phase 2 measured session `GrokCode-20260929T215620Z-plugin-session` on the server that was current for that smoke only. `begin` was HTTP 201 in 322 ms. `complete` was HTTP 200 in 312 ms. Readback matched. No session-log code changed. This does not claim that older 40-second sessions are now fast. Receipt: `docs/receipts/requirements-recovery/acceptance-20260929/dense-session-smoke.json`. Phase 2 review withdrawn as a completion gate: `docs/receipts/hv/20260929T233645Z-dense-session-smoke.response.jsonl`. The brief told the reviewer to pass the phase when the timing receipt matched and to pass requirements because no requirement was claimed done. A separate Phase 2 completion review `docs/receipts/hv/20260930T002642Z-phase2-completion.response.jsonl` is AGREE, accuracy 100, completeness 100, on the timing measurement only. It does not accept the requirements.

The first Phase 1 and Phase 2 100/100 reviews are withdrawn. Replacement requests: `docs/receipts/hv/phase1-completion.brief.md` and `docs/receipts/hv/phase2-completion.brief.md`. Phase 1 review withdrawn as a completion gate: `docs/receipts/hv/20260929T233104Z-recovery-acceptance-audit-profile.response.jsonl`. Brief: `docs/receipts/hv/recovery-acceptance-audit.brief.md`. It told the reviewer that no done claim was made and that open rows were the expected report. Phase 0 baseline: `docs/receipts/requirements-recovery/acceptance-20260929/phase0-baseline.json`. Phase 0b plan review: `docs/receipts/hv/20260929T230403Z-handoff-completion-plan-ignore-mcp.response.jsonl` (AGREE 99/99 on the plan text). Phase 1 ledger: `docs/receipts/requirements-recovery/acceptance-20260929/nine-defect-ledger.json`. These 100/100 reviews are not completion gates.

Source B: follow the complete original dense-child and readback contract and the classified-failure contract. If a repair is actually required, use a clean worktree based on verified deployed split-query code, never the dirty McpServer checkout. Mocks first, then real and full suites and all plugin rows (Codex, Claude Code, Claude Cowork, Copilot, Grok, Cline, Cline v2, OpenCode), SQLite, SQL Server, and PostgreSQL, `Test`, `PluginSessionLogIntegration`, HV, Nuke `UpdateService`, a fresh marker, and repeated smoke. No manual deploy and no unrelated environment repair during HV. Historical begin and complete timings are conflict C5.

Queue and quarantine, from source A, not to be replayed:

- The earlier operator-approved quarantine preserved five corrupt hook cancellation records byte-for-byte, with reason files and verified read-only backups, replayed zero, deleted zero, and left both historical primary sessions deeply equal. Receipt: `docs/receipts/hv/20261007T125315-0500-failsafe-quarantine/verification.json`.
- The supported bounded Codex drain replayed `978b` and `7058` (2 replayed, 0 failed, 0 quarantined). The isolated Grok triage queue replayed `40a8` (1 replayed, 0 failed, 0 quarantined). Originals were copied and hash-verified first. Primary effects were read back.
- The operator answered Yes to preserving and quarantining additional stale `20261007T185015Z-session_submit-7dc1.yaml`. Supported helper quarantine preserved a matching read-only backup and quarantine copies plus a reason file, replayed zero, deleted zero, and left completed turn `req-20261007T184921Z-prompt-ccb7` deeply equal before and after. Proof: `docs/receipts/hv/20261007T171002-0500-approved-7dc1-quarantine/verification.json`.
- Reviewer record `2b93` already auto-drained during its completed review. Do not recreate it.
- Seven quarantined records remain: those five corrupt hook records, older separately retained Codex triage `2b86`, and newly approved stale snapshot `7dc1`. At the Phase 0 acceptance note: zero active pending and seven quarantined records retained. Retained quarantine evidence is not an active pending write and is not claimed replayed. Do not discard it or cancel historical review turns.
- Source B: keep drain disabled. Do not replay historical records or discard quarantine evidence.

Other triage IDs kept above and not repeated as new facts: `triage-report-e85d4371f54c4751959e723de7b46b02`, `triage-report-70a8021a03c34270bacfde4f1c7e98c6`, `triage-report-ad5cb20268a041dfb8d5fefa6f231228`, `triage-report-13c287e9d2034e07ace0442a9c03697c`, and McpServer source-only triage `5deae93eb0764736ae97184817d14eb6` group `320f2143bb2556d0`. The 2026-10-11 handoff begin-turn triage returned exit 1/False with no report ID.

The Codex wrapper hardcode of `MCP_SESSION_MODEL` and the F-13 identity note are in the 2026-10-08T14:42 packet paragraph. The launch rule that isolates the cache before hooks run is in conflict C2.

## Hostile-validation chronology

Listed from older to newer. Scores are accuracy then completeness. None of these is silently replaced by a later verdict unless the later source says that specific review was withdrawn or was not terminal.

- Phase 0b plan text: AGREE 99/99. `docs/receipts/hv/20260929T230403Z-handoff-completion-plan-ignore-mcp.response.jsonl`.
- Phase 1 and Phase 2 100/100 reviews withdrawn as completion gates. Paths in the session-log section.
- Phase 2 timing only: AGREE 100/100. `docs/receipts/hv/20260930T002642Z-phase2-completion.response.jsonl`. Does not accept requirements.
- Supplied-export Phase 1: DISAGREE. `docs/receipts/hv/20260930T003712Z-phase1-supplied.response.jsonl`.
- Requirement-set r1 at 15:34:43Z: DISAGREE 95/90, pass 2, fail 9, unknown 1.
- Requirement-set r2 at 16:06:19Z: DISAGREE 97/95, pass 2, fail 9, unknown 1. Thread `01a0f308-7f62-72f0-89c7-da43b8460cff`.
- Requirement-set r3 at 16:50:56Z: DISAGREE 96/92.
- Requirement-set r4 at 17:02:03Z: AGREE 99/99, pass 12, fail 0, unknown 0. Ledger matches files only. Thread `01a0f33d-b297-7663-87e5-c83fea36b10c`.
- Local Grok r2b: AGREE 99/99, pass 22, but it accepted the rejected expected-red step. Historical only.
- FR-SNP-003 cartridge replay, 2026-10-03T12:45:57Z: AGREE 99/99, pass 18. Narrow. Not native VSF and not a Phase 2 exit.
- Cancelled VSF plan review: 61 events, no `turn.completed`. Cannot authorize Gate 0.
- VSF canonical plan: DISAGREE 94/82, pass 14, fail 7, unknown 0.
- VSF canonical plan, local Grok: DISAGREE 89/84, pass 4, fail 8, unknown 7. Numbered items addressed in the plan. Not closed until a fresh review agrees.
- Phase 4 focused settings: eight passed, zero failed, zero skipped, plus Grok 99/98 at `docs/receipts/hv/20261003T122555-0500-handoff-phase4-green-local-grok-powershell-resume.verdict.md`. Not unfiltered acceptance.
- Phase 0 Opus: DISAGREE 94/85, pass 22, fail 4. CLI `fa2d7384-f174-4f2e-8b3c-4a4cc6477893`. Do not relaunch.
- Phase 0 Grok: AGREE 99/98, pass 26. The accepted Phase 0 task only.
- Phase 1 oracle resume 01: exit 1, provider session limit, not a verdict. CLI `cd19ecc2-b410-4b7c-8a8a-dca9bbb6b008`.
- Noon catch-up: not terminal. Proposed DISAGREE 96/94 inside tool calls. HTTP 429. PID 117120 exited. Waiver later described as expired (C9).
- Historical 3a HV 100/100 and 3b HV 99/99. Narrow. Not full xvic parity (C1).
- Midnight tape amendment: DISAGREE 93/84, 35 PASS, 6 FAIL, 0 UNKNOWN, 1 N/A. Terminal for that review. Do not duplicate. Not a review of later drafts.
- R0 anchor remediation: AGREE 99/98, 49 PASS, 0 FAIL, 0 UNKNOWN, 1 N/A. Pauses the bounded remediation. Does not complete the plan and does not authorize an apply.

Reviewer assignment across this list is conflict C2. Thresholds, full JSONL, complete verdict persistence through primary or the authorized failsafe, and all applicable review surfaces stay mandatory. No artificial turn or time limit. No cloud task. No substitute model beyond what C2 already records. No nested reviewer in source B's remediation rule.

## Open work, exclusions, and next-step chronology

Open TODO counts are conflict C11.

Outside this plan's exit, both sources: `PLAN-GRAPHRAG-001` and `PLAN-RASPBIAN-001` stay open. Source A also names `PLAN-ARCHVIC20-001` in the 2026-09-29 stay-open subset.

Other original exclusions stay excluded: unrelated drive, audio, UI, RemoteControl visual-launch, RAMLink, EQ, and IEC-debug work; broad cleanup; manual deploy; unapproved commit or push. No duplicate TODOs. No hidden dependency scope expansion.

Unresolved history source B says to carry until a later verified disposition: credential-exposure disposition; public-web versus local-only deviation; four historical reviewer-queue disposition questions; unavailable pre-refactor source and compiler evidence; forward-only logging corrections; snapshot asymmetry and the reset hang; incomplete catch-up HV. The noon waiver is not ongoing. The 826-input packet is retained evidence, not current approval.

Next-step layers, conflict C3, oldest first:

1. A's "Next unfinished work": re-run requirement traceability and `git diff --check`, then a new local Grok review of the fully remediated packet; after AGREE at 98/98 or higher, ask approval of `PLAN-VSFLOCKSTEP-001` for Slice 1 only; do not loosen `ExitingPhase_RequiresTestsForThatPhaseAndPriorPhases` or mark `PLAN-C64SET-001` done; Phase 3 of the older parity plan still required its own explicit start sentence in that section; the VSF plan does not silently grant that start.
2. Inside A, the 2026-10-07T17:11 section says it supersedes conflicting current-state statements below it, and the next gate at that moment was Phase 0 recovery of `PLAN-VICE-IMMUTABLE-001` before slice 3c or native VSF implementation. The 17:48 update then says Phase 0 is accepted and next is the mock-contract project. The 08:19 update on 8 October says it supersedes conflicting current-state, review-timing, and next-step statements below it, and names Phase 2 wrapper-only feasibility as the next approved work, under the noon waiver. Later 8 October sections continue feasibility and post-interrupt remediation and say the same Opus max review must be repinned. They do not mark the goal complete.
3. Source B, 2026-10-13T23:51:19, says the bounded Opus remediation is fulfilled, the R0 review agrees, and the work is paused. The six-step sequence in the R0 section is the latest stated next execution sequence. It does not delete the older lists. It says not to repeat applies, restart completed reviews, or drain queues, and not to treat preparation as an executable gate.

## Receipt index

Every path named in either source is listed once. Hashes and counts stay in the sections above.

Requirements recovery and acceptance:

- `docs/receipts/requirements-recovery/acceptance-20260929/phase0-baseline.json`
- `docs/receipts/requirements-recovery/acceptance-20260929/nine-defect-ledger.json`
- `docs/receipts/requirements-recovery/acceptance-20260929/dense-session-smoke.json`
- `docs/receipts/requirements-recovery/acceptance-20260929/supplied-requirements/`
- `docs/receipts/requirements-recovery/acceptance-20260929/structured-ac-backfill.json`
- `docs/receipts/requirements-recovery/acceptance-20260929/guardrail-probes-20260930T154900Z`
- `docs/receipts/requirements-recovery/acceptance-20260929/guardrail-probes-http-20260930T161259Z` and `FR-VIC-001-after-get.json`
- `docs/receipts/requirements-recovery/20261003T101735-0500-vsf-mcp-readback.json`
- `docs/receipts/requirements-recovery/20261011T065515-0500-tape-hv-remediation/README.md`
- `docs/receipts/requirements-recovery/20261011T072531-0500-tape-state-composite/README.md`
- `docs/receipts/requirements-recovery/20261011T083152-0500-media-fixture-specification/README.md`
- `docs/receipts/requirements-recovery/20261011T085402-0500-tape-motor-matrix/README.md`
- `docs/receipts/requirements-recovery/20261011T092059-0500-tape-recording-wiring-contract/README.md`
- `docs/receipts/requirements-recovery/20261011T095710-0500-tape-test-disposition/README.md`
- `docs/receipts/requirements-recovery/20261011T102600-0500-requirements-api-guard/README.md`
- `docs/receipts/requirements-recovery/20261011T110100-0500-tape-observation-contract/README.md`
- `docs/receipts/requirements-recovery/20261011T115707-0500-adjacent-tape-test-disposition/README.md`
- `docs/receipts/requirements-recovery/20261013T231703-r0-anchor-remediation/README.md`
- `docs/receipts/requirements-recovery/20261013T231703-r0-anchor-remediation/HANDOFF.round2.md`
- `docs/requirements/oracles/vic20-xvic-manifest.json`
- `provenance-accepted-20260930.json` (named as a receipt, path not expanded in either source)

Plans:

- `C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\01a0ef2b-2331-70d1-9c65-0ef12b15d09a\plan.md`
- `docs/plans/PLAN-VICE-IMMUTABLE-001.md`
- `docs/plans/PLAN-VSFLOCKSTEP-001.md`
- `docs/receipts/handoff/20261011T112946-0500-canonical-reconciliation/goal-plan.reference.md`
- `docs/receipts/handoff/20261011T112946-0500-canonical-reconciliation/README.md`
- `STDIO-ADAPTER-SLICE.md` (named without a directory in source A)

Hostile validation:

- `docs/receipts/hv/20260928T203110Z-requirements-recovery-preview-gate.response.jsonl`
- `docs/receipts/hv/20260929T230403Z-handoff-completion-plan-ignore-mcp.response.jsonl`
- `docs/receipts/hv/20260929T233104Z-recovery-acceptance-audit-profile.response.jsonl`
- `docs/receipts/hv/recovery-acceptance-audit.brief.md`
- `docs/receipts/hv/20260929T233645Z-dense-session-smoke.response.jsonl`
- `docs/receipts/hv/20260930T002642Z-phase2-completion.response.jsonl`
- `docs/receipts/hv/20260930T003712Z-phase1-supplied.response.jsonl`
- `docs/receipts/hv/phase1-completion.brief.md`
- `docs/receipts/hv/phase2-completion.brief.md`
- `docs/receipts/hv/20260930T152603Z-requirement-set-acceptance.response.jsonl`
- `docs/receipts/hv/20260930T155654Z-requirement-set-acceptance-r2.response.jsonl`
- `docs/receipts/hv/20260930T155654Z-requirement-set-acceptance-r2.last.md`
- `docs/receipts/hv/20260930T164102Z-requirement-set-acceptance-r3.verdict.md`
- `docs/receipts/hv/20260930T165500Z-requirement-set-acceptance-r4.verdict.md`
- `docs/receipts/hv/20261003T121540Z-phase2-fr-snp003-remediation.request.jsonl` and `.response.jsonl`
- `docs/receipts/hv/20261003T122555-0500-handoff-phase4-green-local-grok-powershell-resume.verdict.md`
- `docs/receipts/hv/20261003T131341Z-vsf-plan-corrected.response.jsonl`
- `docs/receipts/hv/20261003T133142Z-vsf-canonical-plan.response.jsonl`
- `docs/receipts/hv/20261003T144113Z-vsf-canonical-plan-local-grok.verdict.md`
- `docs/receipts/hv/20261003T100623-0500-vsf-gate0-local-evidence.md`
- `docs/receipts/hv/20261007T125315-0500-failsafe-quarantine/verification.json`
- `docs/receipts/hv/20261007T131928-0500-phase0-mock-scope-grok-b7-reconstruction/`
- `docs/receipts/hv/20261007T141100-0500-phi2-ac-remediation-opus`
- `docs/receipts/hv/20261007T163600-0500-phase0-opus-remediation/`
- `docs/receipts/hv/20261007T171002-0500-approved-7dc1-quarantine/verification.json`
- `docs/receipts/hv/20261007T171400-0500-phase0-remediation-grok`
- `docs/receipts/hv/20261008T073800-0500-phase1-oracle-contracts-opus`
- `docs/receipts/hv/20261008-noon-catchup-opus.resume-02`
- `docs/receipts/hv/20261008T143538-0500-opus-remediation-packet/`
- `docs/receipts/hv/20261010T161500-0500-tape-amendment-opus/resume-20261011T000300-0500/README.md`, `verdict.md`, and `verdict.json`
- `docs/receipts/hv/20261013T232143-r0-anchor-opus/verdict.md`
- `docs/receipts/hv/20261013T232143-r0-anchor-opus/verdict.persistence.json`
- `docs/receipts/hv/20261013T232143-r0-anchor-opus/terminal-result.json`
- `docs/receipts/hv/20261013T232143-r0-anchor-opus/HANDOFF.reviewed.md`

Native and feasibility:

- `docs/receipts/native-vice/20261007T092700-0500-phase0-primary-recovery/`
- `docs/receipts/native-vice/20261007T133100-0500-readonly-source-baseline.json`
- `docs/receipts/native-vice/20261008T070900-0500-phase1-mocks/full-tests-02-result.json`
- `docs/receipts/native-vice/20261008T082100-0500-phase2-feasibility/` and children `stock-runtime/README.md`, `stock-build-02-result.json`, `binary-runtime/README.md`, `debugger-runtime/README.md`, `clock-runtime/README.md`, `gate-runtime/README.md`, `bus-runtime/README.md`, `runtime-cumulative-vsf-02/result.json`, `runtime-cumulative-oracle-03/result.json`, `resume-tr-before-upstream-build-approval.json`, `resume-tr-after-upstream-build-approval.json`, `InvokeNormalizer.cs.txt`
- `docs/receipts/native-vice/20261008T130200-0500-phase1-mock-remediation/README.md`
- `docs/receipts/native-vice/20261008T131300-0500-phase1-fixture-reuse/README.md`
- `docs/receipts/native-vice/20261008T134300-0500-hv-evidence-corrections/README.md`
- `docs/receipts/native-vice/20261008T135800-0500-routed-triage-readback/primary-report-readback.json`
- `docs/receipts/native-vice/20261008T141611-0500-secret-exposure-assessment/assessment.json` and `README.md`
- `docs/receipts/native-vice/20261008T142142-0500-stdio-mock-contract/`
- `artifacts/immutable-vice/cc67418cba58e71d8e70fd353e9de47199ad2075/source`
- `tools/immutable-vice/clock-gate.c` and `clock-gate.h`
- `build-stock-vice.sh`
- `src/ViceSharp.ViceCompatibility/X64ScSnapshotCompatibility.cs`
- `tests/ViceSharp.VsfCompatibility.Tests/README.md`
- `tests/ViceSharp.OracleContract.Tests` and `OracleContractFixtures.cs`
- `tests/ViceSharp.NativeClockProbe.Tests/README.md` and `StdioAdapterContractTests.cs`
- `native/patches/vice-shim-runtime.patch`
- `native/vice` (immutable, dirty, not to be edited)

Failsafe files:

- `.mcpServer/failsafe/Codex/workspaces/RjpcR2l0SHViXHZpY2Utc2hhcnA/pending/20261008T134738Z-requirements_createdat_projection-17ec.yaml`
- `.mcpServer/failsafe/Codex/workspaces/RjpcR2l0SHViXHZpY2Utc2hhcnA/pending/20261011T162729Z-handoff_beginturn_and_triage_failure-4a5f.yaml`
- `20261008T175401Z-wrapper_argv_length-a2a6.yaml` (supported failsafe name as given; directory not expanded in the source)
- `20261007T185015Z-session_submit-7dc1.yaml`

Code and symbols named as evidence, not as a file inventory of the whole repo: `LockstepValidator.RunFromSnapshot`, `LockstepValidator.StageManagedFromNative`, `Mos6502.InjectSnapshotResumeState`, `Mos6569.InjectSnapshotState`, `Mos6526.InjectSnapshotState`, `IViceNative`, `ViceNative`, `ViceNative.Xvic`, `SnapshotServiceHost`, `ImportLegacySnapshot`, `ContractAssertions`, `ExitingPhase_RequiresTestsForThatPhaseAndPriorPhases`, `Vic20SettingsVisibilityNotificationTests`, `Vic20SettingsUiSurfaceTests`, `Vic20ViceRomNames`, `Vic20NativeLockstepTests.CycleCounts`, `Vic20DivergeProbe`, `monitor_binary.c`, `vic20cpu.c`.

## Coverage checklist

Merge apparatus, not source text. Each source heading is named and pointed at the section that holds its facts.

Source A, `20261011T112946-0500-canonical-reconciliation/HANDOFF.before.md`:

- Title and generation stamp: section "What this file is".
- Opus remediation packet preparation, 2026-10-08T14:42:20-05:00: immutable-boundary section.
- Diagnostic stdio mocks, 2026-10-08T14:28:11-05:00: same section.
- Secret-exposure assessment, 2026-10-08T14:16:11-05:00: same section.
- Routed triage evidence, 2026-10-08T13:58:02-05:00: same section.
- Current evidence corrections, 2026-10-08T13:50:54-05:00: same section.
- Current fixture extraction, 2026-10-08T13:33:57-05:00: same section.
- Current mock remediation, 2026-10-08T13:05:32-05:00: same section.
- Latest review state, 2026-10-08T12:55:41-05:00: interrupted noon catch-up.
- Latest observation progress, 2026-10-08T11:49:07-05:00: binary monitor through bus samples.
- Latest control progress, 2026-10-08T11:10:31-05:00: same section.
- Latest progress, 2026-10-08T10:52:40-05:00: same section.
- Work in progress, 2026-10-08T09:56:27-05:00, including the 10:09:08 debugger probes: same section.
- Earlier progress, 2026-10-08T09:17:17-05:00: stock build and snapshot feasibility.
- Upstream build procedure approved, 2026-10-08T08:31:47-05:00: operating contract and that feasibility section.
- Active continuation and noon HV waiver, 2026-10-08T08:19:01-05:00: Phase 1 mock project and conflict C9.
- Temporary HV reviewer override, 2026-10-08: conflict C2 and operating contract.
- Resume update, 2026-10-07T17:48:03-05:00: Phase 0 acceptance.
- Historical resume update, 2026-10-07T17:11:10-05:00: Phase 0 Opus findings, queue and quarantine, BDPv4 correction, rotation, and fallback.
- 2026-10-03 Native VICE snapshot correction: canonical native VSF.
- Checkpoint: conflict C6 and the ledger section.
- Phase-exit gate: its own section.
- Current State: conflicts C1, C4, C5, C6, C10, and the ledger and goal sections. Not deleted.
- Receipts: session-log section and the receipt index.
- Recovery Acceptance: ledger section.
- Operator Contract: operating contract and conflict C2.
- Open TODOs: conflict C11.
- Next unfinished work: conflict C3 and section 16.

Source B, `20261016T092300-handoff-sync/HANDOFF.before.md`:

- Title and 2026-10-13T23:51:19 update line: "What this file is" and the goal section.
- Resume here: goal section, R0 accepted review, and preparation update.
- Operating contract: operating contract and conflicts C2 and C5.
- Live state at reconciliation: repository section and conflicts C4, C5, C6, and C11.
- Midnight HV is terminal: tape section.
- Current R0 candidate and supplements: tape section.
- Next execution sequence: tape section and conflict C3.
- Whole-goal obligations, Phase 0/0b: goal, ledger, and Phase 0 acceptance.
- Whole-goal obligations, Phase 1: ledger section.
- Whole-goal obligations, Phase 2: session-log section and conflict C5.
- Immutable prerequisites and original Phase 3: immutable-boundary remaining tasks and conflict C1.
- Native VSF is mandatory: canonical native VSF.
- Original Phase 4: Phase 4 settings.
- Original Phase 5 and final audit: Phase 5 section.
- Other unresolved history: open-work section.
- Checkpoint evidence: "What this file is".
