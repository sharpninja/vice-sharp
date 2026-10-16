# ViceSharp Handoff

Generated local: 2026-10-04T12:41:21-05:00

## Resume update: 2026-10-07T09:30:11.003-05:00

This update supersedes conflicting current-state statements in the historical sections below. The operator confirmed the F: relocation is complete and authorized normal development on `F:\GitHub\vice-sharp`; no further migration or partition work is needed. The original completion goal remains the Grok session plan at `C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\01a0ef2b-2331-70d1-9c65-0ef12b15d09a\plan.md`. Phase 3 was subsequently authorized, and the live `PLAN-C64SET-001` TODO records narrowly reviewed Slices 3a and 3b. Do not ask again for an initial Phase 3 authorization or confuse those slices with complete xvic parity.

Operator correction, 2026-10-07: "RED tests are not acceptable. BDPv4 requires mocking data to prove tests work to green before starting code." Write AC-covering unit tests first and prove them green against valid mocked data before production implementation. Use deliberately incorrect mock data to prove the assertions detect defects, with negative tests asserting the expected rejection so the suite remains green. A failing test or an expected-red production run is not an acceptable gate. Only after the mock-validation gate passes may production implementation begin; then the full applicable suite for the current increment and all prior work must pass with zero failed and zero skipped tests before advancing. Mock evidence is not real VICE parity evidence. This corrects the existing process, not the product requirements, and supersedes conflicting historical instructions below.

The local Grok r2b review reported AGREE at 99/99 with PASS 22, FAIL 0, UNKNOWN 0, but explicitly accepted the now-rejected expected-red production step. Its receipt is historical evidence only for that sequence, not authorization to start implementation. Re-review the corrected active plans before advancing; preserve the original receipt unchanged.

Reviewer rotation directive, 2026-10-07: alternate local Grok `grok-4.7` at `xhigh` and local Claude Code `claude-opus-5-5` at `max` for successive HVs, including remediation re-reviews. Current mock-green correction review: Grok PID 34256, receipt prefix `docs/receipts/hv/20261007T110000-0500-bdpv4-mock-green-correction`. Let it finish; next is Opus 5.5 max, then Grok 4.7 xhigh. Resuming an interrupted review retains that review's slot. Do not substitute models, execute in the cloud, spawn reviewer subagents, or impose artificial limits. Thresholds, full JSONL, complete primary MCP verdict readback, and all A/B/C/D obligations are unchanged. This overrides historical Grok-only restrictions. The full goal plan contains both local launch shapes.

Operator fallback directive, 2026-10-07: "If you genuinely cannot save to MCP, you must use the fallback." Attempt the supported primary save first. On a genuine persistence failure, preserve the complete record, including the entire HV verdict where applicable, through the supported failsafe queue and continue under this authorization. Report the exact failure, queued path, and verification evidence; distinguish local retention from primary persistence and replay/read back when available. Do not bypass the plugin, edit storage directly, discard records, summarize away verdict content, or treat a queued requirement mutation as a confirmed primary update. This supersedes the older stop-only behavior for inability to save to MCP.

The next gate is Phase 0 recovery of `docs/plans/PLAN-VICE-IMMUTABLE-001.md`, before Slice 3c or native VSF implementation. Native VICE is the original immutable target, not a new requirement. The current checkout still contains the preserved preexisting 12 tracked modifications and 2,527 untracked paths; no restoration is authorized by this update. Dirty-native results remain diagnostic only.

New primary session: `Codex-20261007T135626Z-plugin-session`, turn `req-20261007T135720Z-turn-866a`. The refreshed marker and installed Codex plugin 1.118.0 restored wrapper access. Supported failsafe drain: 4 scanned, 4 replayed, 0 failed, 0 quarantined. The approved `TR-VSFLOCKSTEP-RESUME-001` body and both structured AC corrections now match primary readback exactly, still pending and unsatisfied. Supported wiki generation updated the projection. Evidence: `docs/receipts/native-vice/20261007T092700-0500-phase0-primary-recovery/`.

The historical immutable-plan HV primary turn contains its full prose and scores but omits the final structured verdict JSON. Fresh local Grok review is required; no Phase 0 completion or later phase is claimed. Traceability diagnostic: 235 canonical IDs, 149 referenced, 86 missing references, 1,011 noncanonical references, command exit 0. Do not describe that as a clean traceability gate. No product tests or builds were run during recovery.

Standing continuity: follow explicit scope, environment, sequence, tools, and approval boundaries literally; surface any inability before substituting an action. After compaction execute the complete `C:\Users\kingd\.codex\skills\add-profile\SKILL.md` and all required profile reads before substantive work. Do not voluntarily compact early. Use sortable local timestamps, PowerShell.MCP, local execution, and the alternating local HV reviewers above with no turn cap or artificial timeout. Native VICE must never be edited to make managed code pass.

This file replaces the 2026-09-28 handoff. That older file said recovery had not been applied and that product work was waiting on a plan. Both statements are stale.

## 2026-10-03 Native VICE snapshot correction

The operator made native VICE snapshot capability mandatory and non-negotiable, then clarified that VICE VSF has been the actual ViceSharp snapshot-format requirement since day one. It is the canonical default, not secondary or opt-in functionality. Do not claim Phase 2 complete until every normal public and desktop snapshot path uses native x64sc C64 or xvic VIC-20 `.vsf`, and load, save, bidirectional round trip, and deterministic continuation satisfy `FR-SNP-005`, `TR-LOCKSTEP-VSF-001`, and `TEST-SNP-002`.

Managed VSF resume support already exists and must be preserved:

- `LockstepValidator.RunFromSnapshot` loads native `.vsf` state and starts managed/native comparison.
- `LockstepValidator.StageManagedFromNative` transfers RAM, written 6510 processor-port direction/data, CPU architectural registers, VIC-II state, and both CIAs into the managed C64. It does not import the exported native opcode/micro-cycle, BA-low, pending-interrupt, IRQ/NMI clock, or interrupt-delay fields. `InjectSnapshotResumeState` resets in-flight execution, so this is a measured first-cycle reconstruction, not complete native pipeline transfer.
- `Mos6502.InjectSnapshotResumeState`, `Mos6569.InjectSnapshotState`, and `Mos6526.InjectSnapshotState` are production chip hooks used by that path.
- `IViceNative`, `ViceNative`, and `ViceNative.Xvic` already expose native snapshot read and write operations.

The gap is not the absence of managed VSF support. The gap is that the existing bridge remains in the test harness, is incomplete for all represented machine and peripheral state, lacks complete xvic staging and managed-to-native save transfer, is not exposed by `SnapshotServiceHost`, and was displaced by proprietary formats as the normal capture, file, quick-slot, and named-slot behavior.

The corrected plan is `docs/plans/PLAN-VSFLOCKSTEP-001.md`. It promotes and completes the existing bridge, keeps native VICE as the version-specific VSF reader and writer, and restores `.vsf` as the canonical default across supported public and Avalonia desktop snapshot paths. It does not propose a replacement managed VSF codec. The two existing proprietary readers move behind the explicit protobuf `ImportLegacySnapshot` operation for one-time migration only; normal restore rejects them, no capture path emits them, and replay remains separate. The plan locks closed model selectors, typed immutable state, both native-DLL lease ABIs, exact current/legacy VSF framing, transactional load/save, the Avalonia file/quick/ten-slot surface, fixture/toolchain/ROM inputs, cumulative BDPv4 gates, and post-publish `win-x64` MSI staging/install proof. No native snapshot product implementation has started under it.

The first 2026-10-03 plan hostile review was cancelled after this canonical-format correction. `docs/receipts/hv/20261003T131341Z-vsf-plan-corrected.response.jsonl` has 61 events and no `turn.completed` event. It is an incomplete receipt and cannot authorize Gate 0 or any done-state change.

The next completed review is `docs/receipts/hv/20261003T133142Z-vsf-canonical-plan.response.jsonl`: DISAGREE, accuracy 94, completeness 82, pass 14, fail 7, unknown 0. It found: an overstatement of current CPU pipeline transfer; missing interface/ownership/state/error contracts; non-executable xvic toolchain and fixture decisions; missing red/mock/cumulative phase stops; unresolved native singleton, cancellation, locking, session-change, and rollback-failure behavior; an impossible checksum-corruption promise and duplicate-module gap; and unspecified runtime packaging/install proof.

The fresh installed-local-Grok review is `docs/receipts/hv/20261003T144113Z-vsf-canonical-plan-local-grok.verdict.md`: DISAGREE, accuracy 89, completeness 84, pass 4, fail 8, unknown 7. It independently passed the CPU-baseline correction but found incomplete protobuf/import and desktop-slot decisions, incomplete legacy VSF framing rules, no exact ROM allowlist binding, an unrelated quarantined parity exit, ambiguous per-DLL singleton behavior, incorrect pre-publish native staging order, stale generated mappings, and missing independent evidence for the fixture, toolchain, MCP readback, selectors, and PE closure. The plan and canonical/live requirements now address every numbered item, and the supporting local evidence is retained in `docs/receipts/hv/20261003T100623-0500-vsf-gate0-local-evidence.md`. No finding is considered closed until another fresh review agrees.

MCP readback remains authoritative and pending. `FR-VSFLOCKSTEP-001` identifies the supplied fixture's exact 24 modules and SHA-256 `5C284F5CF0A1BC7E01A2E4117A74158B7A9D60485F5626448F4B7655C0E23C19`, replacing the incorrect 16-module claim. `FR-SNP-005` has 9 acceptance criteria, `FR-VSFLOCKSTEP-001` 3, `TR-LOCKSTEP-VSF-001` 7, `TR-VSFLOCKSTEP-RESUME-001` 2, `TEST-SNP-002` 8, and `TEST-VSFLOCKSTEP-001` 1. All six are pending at `layer-1`, all 30 criteria are unsatisfied, and both FRs map to both TRs and both TESTs. Readback is retained in `docs/receipts/requirements-recovery/20261003T101735-0500-vsf-mcp-readback.json`.

Recovered legacy records stay active and pending: `FR-VSFLOCKSTEP-001`, `TR-VSFLOCKSTEP-RESUME-001`, and `TEST-VSFLOCKSTEP-001`. They are mapped into the full-capability requirement alongside `TR-LOCKSTEP-VSF-001` and `TEST-SNP-002`. The local MCP wiki projection was regenerated at `2026-10-03T15:16:14.1534405+00:00` and now shows both four-link FR mappings. No legacy requirement was retired.

The FR-SNP-003 cartridge replay remediation received a narrow hostile AGREE at 2026-10-03T12:45:57Z: accuracy 99, completeness 99, pass 18, fail 0, unknown 0. Thread `01a0fdf8-fa4e-74f1-8b53-676b26f748d4`; MCP session `Codex-20261002T175731Z-plugin-session`; request `req-20261003T122054Z-remediation-hv`; request and response streams are `docs/receipts/hv/20261003T121540Z-phase2-fr-snp003-remediation.request.jsonl` and `.response.jsonl`. This proves only cartridge replay value preservation. It does not prove native VSF compatibility and cannot exit Phase 2.

## Checkpoint

Commit `c364e73037f646a7c5d0005c931fb689f665ce9f` is on `origin/main` (`4dfdde5..c364e73`). Message: `docs(receipts): record requirement-set audit checkpoint`. It contains 699 files: the audit receipts, the handoff text at that commit, and `docs/requirements/oracles/vic20-xvic-manifest.json`.

The latest hostile review agreed that the ledger matches the files. It did not accept the recovered requirement set and it did not complete goal 2. Codex `gpt-6-astra` at effort `xhigh`, 2026-09-30T17:02:03Z, thread `01a0f33d-b297-7663-87e5-c83fea36b10c`, accuracy 99, completeness 99, pass 12, fail 0, unknown 0. Receipt: `docs/receipts/hv/20260930T165500Z-requirement-set-acceptance-r4.verdict.md`.

Resume here:

- Item 2 is closed. The operator said to replicate VICE behavior. `FR-CPU-002` ac-05 now says each clock phase advances once and the CPU fetch, decode, and execute sub-cycle state at that phase matches VICE x64sc. The requirement status is still pending.
- Item 5 is closed. The operator rule is to do it the same way as VICE. `FR-PRF-005` no longer allows a stub or silent sound. April sentences that disagree with x64sc were not restored.
- Item 8 is closed. `provenance-accepted-20260930.json` is a receipt of what was done. It stays unchanged: 645 empty old hashes and 110 empty source commits.
- Still open: items 1, 3, 6, 7, 10, and 11. Item 7 stays open because the committed manifest still uses KERNAL ROM filenames and existing reset-cycle constants. Committing the file did not create named-program measurements.
- Closed: items 2, 4, 5, 8, and 9, each for the claim recorded in the ledger.
- At this checkpoint, Phase 3 and Phase 4 still required an explicit user sentence to start. Phase 4's VIC-20 settings-visibility slice was implemented later; Phase 3 still requires an explicit start sentence.
- Do not mark `PLAN-C64SET-001`, any requirement, or any TODO done from the 99/99 ledger agreement.
- The phase-exit gate below has not exited. Script exit 0 only means the 17 ids are named under `tests/`.
- This handoff is newer than `c364e73` and is not in that commit.

## Phase-exit gate

BDPv4 phase number is the requirement `Iteration:` field. Priority P0, P1, and P2 is severity and is not a phase. The operator order "Make it so" is this gate. Phase 2 has not exited, and Phase 3 xvic parity has not started. The Phase 4 VIC-20 settings-visibility behavior was implemented later and has only its narrow focused gate; it does not exit Phase 2 or prove xvic parity.

`tools/check_phase_acceptance_coverage.ps1 -ThroughPhase 2` counts a section when its `##` heading is an FR, TR, or TEST id, `Iteration:` is 0, 1, or 2, and `### Acceptance Criteria` has at least one numbered or bullet criterion. A hit counts only under `tests/` in `.cs` and `.md` files. A mention in `src/` does not count.

Before the acceptance files existed, that command exited 1 on 2026-09-30: 94 requirements in scope, 17 missing from `tests/`, 101 criteria.

- Phase 0: `FR-CPU-004`, `FR-MEM-006` (13 criteria).
- Phase 1: `FR-MEM-004`, `FR-SID-004`, `FR-SID-009`, `FR-UI-004` (21).
- Phase 2: `FR-CFG-003`, `FR-CFG-004`, `FR-CIA-005`, `FR-CRT-002`, `FR-INP-003`, `FR-MED-005`, `FR-MON-005`, `FR-MON-006`, `FR-SID-005`, `FR-SNP-003`, `FR-VIC20-005` (67).

`tests/ViceSharp.TestHarness/PhaseAcceptanceCoverageTests.cs` fact `ExitingPhase_RequiresTestsForThatPhaseAndPriorPhases` expects the script to exit 0. Do not loosen that fact.

Re-run during this handoff update, local 2026-09-30 14:58:09: the script exited 0. In-scope requirements: 94. Missing from `tests/`: 0. Each of the 17 ids is named under `tests/`. The acceptance facts are not all green, and the phase has not exited.

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

Release filter of those classes plus `PhaseAcceptanceCoverageTests`, before the last stimulus corrections: Failed 52, Passed 46, Skipped 0, Total 98, exit 1. The coverage fact was not one of the 52 failures.

Stimulus corrections after that filter changed test code only. No product source was edited.

- `FR-SID-009`: OSC3 is voice 3. The noise fact writes `$D412` and reads `$D41B`. It had written `$D404`.
- `FR-VIC20-005`: each cart attaches on its own host, and `AppliedToRuntime` is required. Dirty FE3 and Ultimem come back from `FlushImage`. Mega-Cart NVRAM comes back from `FlushNvram`, and `FlushImage` stays null. Ultimem `$2000` is stored at image offset `$6000` because the power-up BLK1 bank register is 3. An earlier combined session left the FE3 preset `8k` on the port. Ultimem rejects that preset after `Eject`, and the host still returns Ok, so the kind was `None`.

Focused rerun of `FrSid009AcceptanceTests` and `FrVic20005AcceptanceTests`: Failed 0, Passed 9, Skipped 0, exit 0. The 98-test filter was not repeated after that, so the full fail count is not re-measured.

Still red from that run, excluding the two facts fixed above:

- `FR-CPU-004`: the cassette motor stays off when bit 3 is driven active low. Sense bit 4 stays set when play is pressed (expected 0, actual 16). An output-low bit that becomes an input reads the pull-up on the first read (expected 0, actual 32).
- `FR-SID-005`: there is no `SetSidModel` or `SwitchFilterModel`. The resonance fact stays red. The same stimulus produced 8580 band-pass 30320 and 6581 band-pass 26265. A smaller absolute band-pass value is the wrong comparison. Do not flip that inequality.
- `FR-CFG-003`, `FR-CFG-004`, `FR-CIA-005` CNT and SP pins, `FR-CRT-002` ROMH and CRT CHIP packets, `FR-INP-003` mouse methods, `FR-MED-005`, `FR-MON-005` (`Unknown: bank`), `FR-MON-006`, and `FR-SNP-003`.

A standard VIC-20 BLK image, separate from FE3, Ultimem, and Mega-Cart, does not have its own fact.

Do not implement cassette motor, sense, discharge, monitor bank or watch commands, palette files, hotkeys, the media catalog, replay, the mouse port, Ocean ROMH, or a runtime SID switch to turn this suite green. The next code for each still-red criterion is a correct failing test, then mocks, then implementation.

`FR-CPU-002` ac-05 stays the VICE x64sc phase sentence and does not name `IClockedDevice.Tick` or `ICpu.Phase`. `FR-PRF-005` stays the 6560/6561 and the four VIC voices, with no stub and no silent allowance. Ledger items 2, 4, 5, 8, and 9 stay closed for the ledger claims. Items 1, 3, 6, 7, 10, and 11 stay open. `provenance-accepted-20260930.json` was not rewritten. `docs/receipts/requirements-recovery/acceptance-20260929/nine-defect-ledger.json` is modified in the worktree and is not in `c364e73`. This note does not change that file.

## Current State

- Plan: `C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\01a0ef2b-2331-70d1-9c65-0ef12b15d09a\plan.md`
- Phase-exit gate: see the section above. Script exit 0 at local 2026-09-30 14:58:09 (94 in scope, 0 missing from `tests/`). The acceptance suite is not green. Phase 2 has not exited.
- Phase 0 recorded the live baseline. Phase 2 recorded timings for one dense session. On 2026-09-30 the operator said "Finish this" after "It does not accept the requirement set." Ledger item 2 was later closed when the operator required VICE behavior instead of `IClockedDevice.Tick()` and `ICpu.Phase`. No requirement status was set to completed, and no TODO was marked done. Codex `gpt-6-astra` at effort `xhigh` reviewed that order at 2026-09-30T15:34:43Z and returned DISAGREE, accuracy 95, completeness 90, pass 2, fail 9, unknown 1. The same model and effort reviewed the updated packet at 2026-09-30T16:06:19Z and returned DISAGREE, accuracy 97, completeness 95, pass 2, fail 9, unknown 1. Thread `01a0f308-7f62-72f0-89c7-da43b8460cff`. Receipt: `docs/receipts/hv/20260930T155654Z-requirement-set-acceptance-r2.response.jsonl`. The same model and effort reviewed the audit packet at 2026-09-30T16:50:56Z and returned DISAGREE, accuracy 96, completeness 92. Receipt: `docs/receipts/hv/20260930T164102Z-requirement-set-acceptance-r3.verdict.md`. The same model and effort reviewed the corrected audit packet at 2026-09-30T17:02:03Z and returned AGREE, accuracy 99, completeness 99, pass 12, fail 0, unknown 0. Thread `01a0f33d-b297-7663-87e5-c83fea36b10c`. Receipt: `docs/receipts/hv/20260930T165500Z-requirement-set-acceptance-r4.verdict.md`. That agreement is that the ledger matches the files. It does not accept the recovered requirement set and it does not complete goal 2.
- The recovery guardrail is running. Source commit `a26464e234fa31cae85d433bdec27cbdd654b4c5` merged to `develop` as `fc75387043f8337855e34d92eb3a0e03aa82ce48` in https://github.com/sharpninja/McpServer/pull/73 at 2026-09-30T14:37:17Z. The running service was republished from detached `origin/develop` `fc753870` with Nuke `UpdateService --skip-version-bump --package-version 1.4.41`. Current health is `1.4.41+fc75387043f8337855e34d92eb3a0e03aa82ce48`. `GitVersion.yml` was not changed. The dirty `develop` checkout was not used. The Phase 2 smoke was measured earlier on `1.4.41+c195837ac888b6f02724221de6a040e63b3e6b47`.
- The first Phase 1 and Phase 2 100/100 reviews are withdrawn. The replacement requests are `docs/receipts/hv/phase1-completion.brief.md` and `docs/receipts/hv/phase2-completion.brief.md`.
- The reviewer has no MCP access. The requirement records supplied for review are under `docs/receipts/requirements-recovery/acceptance-20260929/supplied-requirements/`: 510 snapshot files plus 13 live HTTP GETs at 2026-09-30T00:35:23Z.
- The 2026-09-30T00:37:12Z Phase 1 review of the earlier supplied export, `docs/receipts/hv/20260930T003712Z-phase1-supplied.response.jsonl`, is DISAGREE. That export still had empty structured criteria. It is not the current store. The 15:34:43Z acceptance review is `docs/receipts/hv/20260930T152603Z-requirement-set-acceptance.response.jsonl`: DISAGREE, accuracy 95, completeness 90. The 16:06:19Z acceptance review is `docs/receipts/hv/20260930T155654Z-requirement-set-acceptance-r2.last.md`: DISAGREE, accuracy 97, completeness 95. The 16:50:56Z review is `docs/receipts/hv/20260930T164102Z-requirement-set-acceptance-r3.verdict.md`: DISAGREE, accuracy 96, completeness 92. The latest completed review is `docs/receipts/hv/20260930T165500Z-requirement-set-acceptance-r4.verdict.md`: AGREE, accuracy 99, completeness 99, at 2026-09-30T17:02:03Z, on ledger accuracy only. The recovered requirement set is not accepted, and goal 2 is not complete.
- Phase 2 completion review `docs/receipts/hv/20260930T002642Z-phase2-completion.response.jsonl` is AGREE, accuracy 100, completeness 100, on the timing measurement only. It does not accept the requirements.
- Phase 3 (xvic parity) has not started. Phase 4's VIC-20 settings-visibility behavior is implemented and has a focused 8-pass, 0-fail, 0-skip gate; that result does not prove xvic parity.
- Older-plan resume point: an explicit user message approving the start of Phase 3 xvic parity. Approval of the VSF plan is not that approval. Do not open slice 3a before that message.
- `FR-VIC-001`, `TR-CYCLE-001`, and `FR-CPU-002` now have structured acceptance criteria copied from the bullets already in their bodies. Counts are 8, 7, and 6. Body lengths stayed 1105, 995, and 1110. `isSatisfied` is false. The body text was not rewritten. Do not write xvic tests against a body rewrite until the user approves that text and the start.
- Checkpoint `c364e73037f646a7c5d0005c931fb689f665ce9f` is on `origin/main` and includes the acceptance receipts. `PLAN-C64SET-001` is not done. xvic parity is not claimed.

Repository state when this file was replaced:

- Workspace: `F:\GitHub\vice-sharp`
- Branch: `main`
- HEAD at the receipt commit: `c364e73037f646a7c5d0005c931fb689f665ce9f`
- Origin: `https://github.com/sharpninja/vice-sharp.git`
- `origin/main` matched that HEAD immediately after `git push origin HEAD:main`.
- The receipt paths from this audit are in that commit. Do not clean, reset, or revert them unless the operator asks.

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
- Open: item 1 (acceptance criteria were not restored where no heading existed), item 3 (semantic classification is not done), item 6 (mappings are not per-AC coverage), item 7 (manifest is committed and still is not a named-program workload oracle), item 10 (preview D3), and item 11 (preview A9).
- Needs-operator: none. Item 2 is closed: `FR-CPU-002` matches VICE x64sc phase behavior. Item 5 is closed: `FR-PRF-005` requires the 6560/6561 and the four VIC voices, and the x64sc sprite and illegal-mode text stays. Item 8 is closed: the provenance receipt was not rewritten.

Item 9 recheck at 2026-09-30T15:49:00Z, workspace `F:\GitHub\vice-sharp`, method `workflow.requirements.planRecovery`, dry-run. Placeholder body and acceptance-criteria removal returned `validation_error`, `retryable` false, exception `System.ArgumentException`. Empty body, malformed id, and type-prefix mismatch returned `schema_validation_failed`, `retryable` false, and were not planned. `FR-VIC-001` parsed description text was unchanged across the before and after reads (1105 characters, equal, SHA-256 f2c678e7e25411e612c37686ff65fe489553ec13300297a68163eb54faf97b7e). The earlier 1215 figure was not the parsed description. A direct dry-run POST to `/mcpserver/requirements/recovery/dry-run` at 2026-09-30T16:13:00Z returned HTTP 400 `validation_error`, `retryable` false, for empty body, placeholder body, malformed id, type-prefix mismatch, and acceptance-criteria removal. After those five calls a later GET was saved as `FR-VIC-001-after-get.json` in `docs/receipts/requirements-recovery/acceptance-20260929/guardrail-probes-http-20260930T161259Z`. Its `body` field is 1105 characters, SHA-256 `f2c678e7e25411e612c37686ff65fe489553ec13300297a68163eb54faf97b7e`, the acceptance heading is present, and the structured list has 8 entries. That SHA matches the 15:15:09Z supplied description. The saved GET is later than the 16:13:00Z probes. The plugin schema still rejects empty body, malformed id, and type prefix before the server sees them. Those plugin results are `schema_validation_failed`, not a second copy of the HTTP 400. Plugin receipt directory: `docs/receipts/requirements-recovery/acceptance-20260929/guardrail-probes-20260930T154900Z`. The running service is health `1.4.41+fc75387043f8337855e34d92eb3a0e03aa82ce48`.

The placeholder rows `[TR-REMOTECTRL-SERVER-001]` and `PERF-SPRITE-DMA-OPT-001` were deleted on 2026-09-30. The 2026-09-30T15:15:09Z export records both as `not_found`. The unbracketed `TR-REMOTECTRL-SERVER-001` remains. TR `totalCount` in that export is 136.

Structured acceptance criteria were copied from existing `## Acceptance Criteria` bullets only. The scan covered FR 232, TR 138, and TEST 140. 205 records received criteria, 90 already had criteria and were left unchanged, and 215 had no heading and were not given invented criteria. Status values were not changed. Receipt: `docs/receipts/requirements-recovery/acceptance-20260929/structured-ac-backfill.json`.

Four legacy ids return 404: `ARCH-TRUEDRIVE-1541-002`, `BACKFILL-MEDIA-001`, `BACKFILL-VIDEO-001`, `RUNTIME-TAPE-002`. Their `FR-*` replacements return 200. `FR-MCP-REQRECOVERY-001`, `TR-MCP-REQRECOVERY-001`, and `TEST-MCP-REQRECOVERY-001` are 404 on this workspace. Do not mark them satisfied.

Item 7: `docs/requirements/oracles/vic20-xvic-manifest.json` is in commit `c364e73037f646a7c5d0005c931fb689f665ce9f`. The r4 review saw it untracked. Item 7 stays open. VICE commit `cc67418cba58e71d8e70fd353e9de47199ad2075`. Media is an explicit empty array. Workload `program` values are the ROM names `kernal.901486-07.bin` (machine `vic20`) and `kernal.901486-06.bin` (machine `vic20ntsc`), from `Vic20ViceRomNames`. Cycle counts are the constants already in `Vic20NativeLockstepTests.CycleCounts` and `Vic20DivergeProbe`. The Codex review at 15:34:43Z rejected the earlier `power-on-reset` label. No new cycle numbers were invented.

## Operator Contract

- VICE, including x64sc and xvic quirks, is the behavioral oracle unless an approved requirement says otherwise.
- UWP and Xbox are outside the current implementation scope. Their historical requirements are not deleted or retired unless the operator explicitly directs that change.
- On every ViceSharp versus VICE mismatch, answer how the managed core diverged from VICE and how it gets back to VICE before changing code.
- All implementation, investigation, requirements work, builds, tests, and documentation stay local in this chat. Do not use cloud runners, hosted agents, web tools, remote Codex threads, cloud CI, pushes, or other cloud execution.
- Hostile validation is the sole exception and must run through Grok launched and operated locally. Persist its request/response receipts in `docs/receipts/hv/` and its verdict in the active local MCP session.
- Do not set `--max-turns` or any other artificial turn or time limit on hostile validation. Let the local Grok reviewer run until it returns a terminal verdict or the operator explicitly stops it.
- PowerShell only. Do not use Python.
- Do not mark a requirement, TODO, goal, or plan step done without executable proof and a hostile AGREE at accuracy and completeness both at least 98.

## Open TODOs

36 open. These stay open and are not this handoff's exit: `PLAN-GRAPHRAG-001`, `PLAN-RASPBIAN-001`, `PLAN-ARCHVIC20-001`, `PLAN-C64SET-001`, `PLAN-GROKCOMPLETION-001`. The full open id list from the 2026-09-29 query is in the session turn `req-20260929T234800Z-why-are-you-waiting`.

## Next unfinished work

1. Re-run requirement traceability and `git diff --check`, then submit the fully remediated packet and its local evidence to a new local Grok hostile review. Preserve every failure if the verdict is not AGREE at 98/98 or higher.
2. Ask for explicit operator approval of `PLAN-VSFLOCKSTEP-001` only after the hostile gate passes. Approval authorizes Slice 1 only; do not implement native snapshot product code before it.
3. The older Phase-exit gate is still open. The coverage script exits 0 because the 17 ids are named under `tests/`; acceptance facts for missing behavior are still red. Do not loosen `ExitingPhase_RequiresTestsForThatPhaseAndPriorPhases` or mark `PLAN-C64SET-001` done.
4. Phase 3 of the older parity plan still requires its own explicit start sentence. Phase 4's VIC-20 settings-visibility behavior is now implemented and covered by `Vic20SettingsVisibilityNotificationTests` and `Vic20SettingsUiSurfaceTests`; that focused gate is not proof of xvic parity. The VSF plan does not silently grant the start of Phase 3.
