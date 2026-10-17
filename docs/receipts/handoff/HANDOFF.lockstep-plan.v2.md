# ViceSharp handoff v2: C64 and VIC-20 lockstep with VICE, two-machine edition

Written 2026-10-17 (local, America/Chicago, timestamps measured) by Claude Code (claude-fable-5-1) on LAB-OMARCHY, workspace `/home/sharpninja/github/vice-sharp`, branch `develop` at `d00e8c5584d0031b3637c649335964f8d5225da0`. MCP session: the hook-opened ClaudeCode session of this chat; the task turns are `req-20261017T125711Z-prompt-e491` and its successors on 2026-10-17.

This file supersedes `HANDOFF.lockstep-plan.md` (written 07:40 today on PAYTON-LEGION2, SHA256 `7EDAA9A0494831229A84774CC99309E5F2BACBEB2945B5FC6039575284B7C5DD`, copied here byte-identical) as the single forward plan. Where v1 and this file differ, this file wins; the differences are listed in section 2.3. It does not edit v1, the two preserved `HANDOFF.before.md` files, the root `HANDOFF.md`, or any plan under `docs/plans/`. Nothing is committed by the turn that wrote it.

Every "measured" fact was read live on 2026-10-17 between 07:57 and the time of writing, on LAB-OMARCHY or over the WSMan route to PAYTON-LEGION2, through the mcpserver-claude-code-plugin wrapper (never raw REST). Every "receipt" fact names its file. Observation and inference are labeled where they differ.

## 1. The goal (unchanged from v1)

Managed ViceSharp runs the C64 and the VIC-20 in half-cycle lockstep with pinned, unmodified native VICE (x64sc and xvic at gitlink `cc67418cba58e71d8e70fd353e9de47199ad2075`), from reset and from a native `.vsf` checkpoint, for at least 100,000 cycles each, with every divergence reported at its exact half-cycle and no inferred native value. The compared fields per half-cycle are fixed by `docs/plans/PLAN-VICE-IMMUTABLE-001.md` Phase 4: A, X, Y, S, P, PC; opcode and micro-op phase; bus address, data and read/write direction; dummy versus real access; IRQ and NMI pipeline stage; stolen-cycle and BA state. One comparator, `tests/ViceSharp.TestHarness/LockstepValidator.cs`, owns the comparison.

Done means: the full applicable current-plus-prior suites pass with zero failed, skipped and aborted tests on the machine that claims the gate; `FR-VICE-001`, `TR-VICE-ORACLE-001`, `TEST-VICE-001` and all seven `TR-CYCLE-001` acceptance criteria have executable evidence from the clean target; and an independent hostile review returns AGREE with accuracy and completeness both at least 98 and zero FAIL or UNKNOWN.

## 2. Where the project truly stands on 2026-10-17

### 2.1 Two machines, two requirement stores, one repo

Observation (measured). The repository is the same on both machines: `develop` at `d00e8c5` on LAB-OMARCHY (clean at session start) and on PAYTON-LEGION2 (`git status` there: `docs/receipts/handoff/README.md` modified, `.tmp-c64wiki/` and `HANDOFF.lockstep-plan.md` untracked, and the four R0 hostile-review receipt folders `docs/receipts/hv/20261011T123239-0500-r0-consolidated-opus`, `20261013T213639-r0-remediation-opus`, `20261013T222855-r0-round2-opus` and `20261013T232143-r0-anchor-opus` deleted in the working tree but still present in the commit and in this checkout).

Observation (measured). The requirement stores were not the same. PAYTON-LEGION2 (server 1.4.40) held FR 235, TR 139, TEST 142, 747 mapping rows (233 FR mapping groups), one layer `layer-1`, statuses FR 216 pending / 10 completed / 5 closed / 4 in_progress, TR 108 / 20 / 8 / 3, TEST 125 / 11 / 4 / 2. LAB-OMARCHY (server 1.4.41-recovery) held FR 10, TR 9, TEST 8, 23 mapping rows, one layer `layer-1` created 2026-10-16T00:15:29Z, every record `in_progress`. The LAB-OMARCHY records are not a stale subset: all 27 shared ids differ in content from LEGION2 and carry the Linux workspace's "PR7 regression remediation" notes, and `TEST-ROMM-LAUNCH-001` plus 8 mapping rows exist only on LAB-OMARCHY. Receipts: `~/codex-dispatch/req-backup-20261017T082025/summary.json` (LAB-OMARCHY before), `~/codex-dispatch/legion2-export-20261017T082422/extracted/summary.json` (LEGION2), `~/codex-dispatch/diag/compare-legion2-vs-omarchy-before.json` (the diff).

Observation (measured). The additive sync performed today brought LAB-OMARCHY to the counts recorded in section 9. Three classes of record could not be synced through the supported plugin route and remain operator decisions (section 6, D10 to D12): the 27 content conflicts (never overwritten), 57 TR ids and 9 TEST ids that the LAB-OMARCHY server rejects on create because they do not match `TR-<AREA>-<SUBAREA>[-<QUALIFIER>]-###` or the TEST pattern (among them `TR-CYCLE-001`, the central lockstep TR), and 17 records whose LEGION2 status is `closed`, a value the plugin schema does not accept (created with the server default).

Inference. Until D10 to D12 are decided, PAYTON-LEGION2 remains the authoritative requirement store for lockstep work and LAB-OMARCHY is a near-complete mirror. Any requirement write for this plan goes to LEGION2 first and is mirrored, never the reverse, until the two are reconciled.

Observation (measured). TODO stores are also split: LEGION2 has 39 open TODOs including every lockstep container (`PLAN-VICEORACLE-001`, `PLAN-VSFLOCKSTEP-001`, `PLAN-C64SET-001`, `PLAN-GROKCOMPLETION-001`, `PLAN-REQAUDIT-001`, `PLAN-VICBA-001`, `BUG-DRV1540-001`, `PLAN-NATIVERESIDUE-003`, `PLAN-ARCHVIC20-001`); LAB-OMARCHY has 4 open TODOs, all from the Linux PR7 work: `DIAG-CPUINT-001` (the five CPU IRQ/NMI failures), `DIAG-AUDIO-001`, `DIAG-DRIVERO-001`, `DIAG-SNAPSHOT-001`. TODOs were not synced today (not requested).

### 2.2 What R0 actually is, measured

The operator's premise for this session was that R0 on LEGION2 had solved the native wrapper and started realigning requirements to VICE. Observation (measured on LEGION2 through the plugin and in the repo): that did not happen.

- The R0 sixteen-record requirement delta (`docs/receipts/requirements-recovery/20261013T231703-r0-anchor-remediation/proposed-field-delta.json`, SHA256 `63386E9251185B101F53E9257CDA9270025A68006B83F243314E6E299A55D702`) has an accepted review (Opus 5.5 max, 2026-10-13, AGREE 99/98) and is still UNAPPROVED and UNAPPLIED on both servers. The affected records on LEGION2 read exactly as before the delta (`FR-CPU-004` pending 7 AC / 0 satisfied, `TEST-CPU-001` pending 0 AC, `FR-TAP-005` pending 6 / 0, `FR-PRF-003` pending 6 / 0, `TEST-PRF-001` pending 0 AC).
- No requirements recovery run exists on either server for R0. The only recovery records on LEGION2 are the 2026-09-29 slice5 keys (`slice5-apply-clean-18028995ae17`, applied; `slice5-apply-4db60f189336`); `slice5-dryrun-382a13f4c072` is not found.
- LEGION2 session history (30 most recent sessions) shows no R0 or wrapper work after the 2026-10-13 review rounds. The only 2026-10-17 session is the v1 handoff writer (`ClaudeCode-20261017T123700Z-lockstep-handoff`, 1 turn, in_progress).
- The native wrapper is exactly where v1 left it: immutable-plan Phase 2 "feasible in part, exit gate not met" (receipt root `docs/receipts/native-vice/20261008T082100-0500-phase2-feasibility/`), Phase 1 "green, unaccepted", Phase 3 and 4 not started. `artifacts/immutable-vice/` exists only on LEGION2 (absent on LAB-OMARCHY, measured).

Resolution: R0 is a reviewed, unapplied requirement delta plus a paused development state. It is not progress on the wrapper. v1 section 2.1 stands.

### 2.3 Corrections to v1 from today's measurements

- v1 said the five PR7 CPU/NMI failure names were unknown on LEGION2. They are recorded on LAB-OMARCHY in `TestResults/pr7-resolution/snapshot-green-verified.trx` (62 executed, 57 passed, 5 failed): `CpuInterruptTests.LdaImm_IrqOnLastClk_DoesNotDispatchBeforeNextFetch`, `CpuInterruptTests.RtsThenLdaImm_NextTick_FetchesFollowingOpcode`, `CpuInterruptTests.Rts_DelayNextFetchTick_DoesNotTakeIrqBeforeReturnFetch`, `Mos6510NmiTests.Nmi_IsEdgeTriggered_HoldingLineDoesNotRefire`, `Mos6510NmiTests.Nmi_RtiReturnsCpuToPreNmiState`. They are tracked as `DIAG-CPUINT-001` on LAB-OMARCHY only.
- v1 measured `LockstepValidator.cs` at 1,782 lines on LEGION2; it is 2,050 lines in this checkout at the same commit (measured with `wc -l`). The difference is not explained; Phase A step 2 pins the file hash on both machines before any edit.
- v1 listed the three native-safe test projects as absent from `ViceSharp.slnx`; confirmed here: the solution lists six test projects (`AiReview.Tests`, `Benchmarks`, `Heads.Tests`, `Library.IntegrationTests`, `Library.Tests`, `TestHarness`) and the directories `tests/ViceSharp.OracleContract.Tests`, `tests/ViceSharp.VsfCompatibility.Tests`, `tests/ViceSharp.NativeClockProbe.Tests` exist on disk.
- v1 said requirement counts were identical on both machines since 2026-10-11. True for LEGION2 only; LAB-OMARCHY had diverged (section 2.1).
- `native/vice` on LAB-OMARCHY has 8 tracked modifications and 163 porcelain entries with untracked files (measured), a different dirty state from LEGION2's 12 and 2,527. Both are diagnostic-only oracles under `FR-VICE-001` AC3. Linux shim build scripts `native/build-vice-shim.sh` and `native/build-vice-shim-xvic.sh` exist beside the PowerShell one.
- The lockstep records all still read pending with zero satisfied criteria except: `TR-CORE-CYCLE-001` completed 2/2, `TR-SIDAUDIO-CLOCK-001` completed 2/2, `FR-SIDAUDIO-001` completed 3/3, `FR-VIC20-001` pending 6/6, `TEST-VIC20-001` pending 5/5, `TR-VIC20-PIXEL-001` pending 3/3, and the closed-with-zero-criteria group `FR-VIC-001` (8 AC, 0 satisfied), `FR-BACKFILL-VIDEO-001`, `TR-VIC-EDGE-001`, `TEST-VIC-CHECKPOINT-001`, `TEST-VIC-RC-001`. The anomaly v1 reported (D8) is wider than two records: 17 records carry `closed`, a status the plugin cannot write or read back as valid.
- Plugin defect found and fixed today (class-2 operator orders "Fix it now" and "Must fix all hand-build YAML in all plugins"): every YAML producer in the nine mcpserver plugin repos hand-built YAML from strings (timeout envelope, session-state writer, failsafe record ToYaml, cache-manager pending records, plan-to-TODO map, REPL diagnostic log entries, one literal param). All now serialize objects through shared helpers in `lib/yaml-object-mutation.ps1` and the new `lib/plan-todo-map.ps1`, covered by `tests/YamlConstruction.Tests.ps1` (16 tests, mock gate with negative controls and a static lint) and `tests/TimeoutEnvelope.Tests.ps1` (9 tests). Committed and pushed on each repo's current branch at 2026-10-17T11:22 (claude-code `7991235569`, codex `c311410352`, others in `~/codex-dispatch/diag/plugin-commit.json`). Caches refreshed: LAB-OMARCHY Claude 1.118.0 (site edits) and codex 1.120.0 (copy). Triage report `triage-report-206847f3e43a45c8bab5cd5e6ddf6632`. LEGION2's caches are not patched; stale codex caches 1.119.0 and 1.108.0 on LAB-OMARCHY are untouched.

### 2.4 What has not changed

Everything in v1 sections 2.1 and 2.2 that is not corrected above stands: C64 lockstep status "passing against a diagnostic oracle, unproven against the immutable oracle"; VIC-20 half-cycle comparator exists as one fixture; Phase 3 of the Grok plan is authorized and 3c is gated by immutable prerequisites, not approval; VSF Gate 0 open with Slices 1 to 5 on the critical path; full-suite health unknown on Windows and partially known on Linux (section 2.3); reviewer is locally launched `claude-opus-5-5` at `max`; the 2026-10-16 "no more HV" instruction was scoped to the RomM integration.

## 3. Operating contract for this path

v1 section 3 applies verbatim, with three additions:

- Two execution hosts. PAYTON-LEGION2 runs Windows gates through PowerShell.MCP; LAB-OMARCHY runs Linux gates with `pwsh -NoProfile -NonInteractive -File` directly (the operator's standing Linux exception). A gate claimed on one host names the host, and the Windows-applicable and Linux-applicable test sets are stated with their denominators (decision D9 covers the 13 Linux-only tests).
- One requirement writer. Requirement, AC and mapping writes for this plan go to PAYTON-LEGION2 through the plugin and are mirrored to LAB-OMARCHY by the same additive sync used today (`~/codex-dispatch/scripts/Export-McpRequirements.ps1`, `Compare-McpRequirementExports.ps1`, `Import-McpRequirementsAdditive.ps1`) until D10 to D12 reconcile the stores. Never write the same id on both servers independently.
- The requirements export through `workflow.requirements.generateDocument` writes projection files into the workspace (`docs/Project/*.md`) as a side effect on whichever machine runs it. Run it only when a wiki or projection refresh is wanted, and record the resulting working-tree diff.

## 4. The plan

Phases A to G keep v1's structure, entry and exit conditions. Amendments are marked. Each phase exit carries its own full-suite receipt, native inventory unchanged, request and response JSONL under `docs/receipts/hv/{yyyyMMddTHHmmss}-{gate}.*`, and AGREE 98/98.

### Phase A: Measured baselines on both machines, store reconciliation, decisions (no product code)

Entry: this handoff; `develop` at `d00e8c5` or later on both machines; decisions D1 to D12 answered or explicitly deferred by Payton.

Work (amended):
1. Windows baseline on PAYTON-LEGION2 exactly as v1 Phase A steps 1 to 3 (three native-safe suites unfiltered in Release; diagnostic TestHarness lockstep and acceptance filters against the current prebuilt dirty shim without invoking `Vice.Native.proj`; receipt under `docs/receipts/lockstep/{yyyyMMddTHHmmss}-phase-a-baseline-windows/`).
2. Linux baseline on LAB-OMARCHY: the same three native-safe suites; the same TestHarness filters against the Linux shim if `native/build-vice-shim.sh` output exists without rebuilding (if the harness build cannot avoid `Vice.Native.proj`, stop and report); `Category=Determinism`; the five `DIAG-CPUINT-001` failures re-run by name; receipt under `docs/receipts/lockstep/{yyyyMMddTHHmmss}-phase-a-baseline-linux/`. Pin SHA256 of `LockstepValidator.cs` and the six lockstep test files on both machines and record whether they match.
3. Store reconciliation per D10 to D12, executed on LEGION2 first and mirrored: resolve the 27 content conflicts, the 66 rejected ids, the 17 `closed` statuses. Then re-export both stores and prove equality by `Compare-McpRequirementExports.ps1` (zero onlyInSource, zero onlyInTarget, zero differing).
4. TODO alignment: mirror the lockstep TODO containers to LAB-OMARCHY or link the LAB-OMARCHY `DIAG-*` TODOs into `PLAN-VICEORACLE-001` and `PLAN-C64SET-001` on LEGION2 (D13). No other TODO or requirement mutation.
5. Decide D2 (R0 delta) before Phase E starts, because it rewrites `FR-CPU-004` and `TEST-CPU-001` criteria that Phase E must satisfy.

Exit: both baseline receipts exist; both stores compare equal; decisions recorded in the session turn; no HV required (operational class).

### Phase B: Accept the native-safe mock gate (immutable Phase 1)

Unchanged from v1. Runs on PAYTON-LEGION2 (the 826-input packet and remediation receipts live there). Exit adds: the three native-safe projects are also green unfiltered on LAB-OMARCHY (they are native-safe by construction), recorded in the same receipt.

### Phase C: External bus observer and cycle control for both machines (immutable Phase 2)

Unchanged from v1, on the separate materialization under `artifacts/immutable-vice/` on PAYTON-LEGION2. Amendment: step 2 (stdio diagnostic adapter bound to a real C binding) is built and proved on both hosts, because the Linux shim build path (`build-vice-shim.sh`) must consume the same external translation units later in Phase D.

### Phase D: Native-safe builds and restoration of the oracle (immutable Phase 3)

Unchanged from v1 with one amendment to step 1: `native/build-vice-shim.ps1` and `build-vice-shim-xvic.ps1` are the canonical scripts; the existing `build-vice-shim.sh` and `build-vice-shim-xvic.sh` are retired or made thin launchers of the PowerShell scripts (operator rule: PowerShell only), and the Linux materialization is created under `artifacts/immutable-vice/` on LAB-OMARCHY by the same script. Step 4 (restore `native/vice`) is executed on both machines, each after its own copy-and-hash preservation receipt, because the dirty states differ (12 modified / 2,527 untracked on LEGION2; 8 modified / 163 porcelain entries on LAB-OMARCHY).

### Phase E: C64 half-cycle lockstep against the immutable oracle

Unchanged from v1 with these amendments:
- Step 2 names the five failures now known (section 2.3) as the first slice. Each is reconciled to its VICE-derived AC (`FR-CPU-002` ac-05 phase sentence; `TR-CYCLE-001` AC1 to AC3 for interrupt pipeline stage) before any assertion is touched. `DIAG-CPUINT-001` closes only through this slice.
- `TR-CYCLE-001` must exist on the store where the gate is claimed. If D11 keeps the current id, LAB-OMARCHY cannot hold it, and every Phase E gate is claimed on LEGION2.
- Step 5 (restored-checkpoint lockstep) consumes the merged versioned managed snapshots from PR7 as implementation input only; `DIAG-SNAPSHOT-001` (restored-game continuity) is folded into the VSF Slice 2/3 gate rather than closed separately.

### Phase F: VIC-20 half-cycle lockstep (Grok plan Slice 3c, immutable Phase 4)

Unchanged from v1. Amendment: `FR-VIC20-001` (pending with 6 of 6 satisfied), `TEST-VIC20-001` (5 of 5) and `TR-VIC20-PIXEL-001` (3 of 3) are re-proved on the immutable xvic in repair pair 4 before their satisfied flags are trusted; until then they are treated as unproven (D8 extended).

### Phase G: Whole-goal closure

Unchanged from v1. Amendment: closure requires both requirement stores equal (Phase A step 3 re-run) and both machines' full applicable suites green, each with its stated denominator.

## 5. Risks and blockers

v1 section 5 stands. Added today:

- Split stores. Two servers hold the same ids with different content, and one server rejects 66 ids the other holds. Until Phase A step 3 is done, any requirement write risks a second divergence. Mitigation: single-writer rule (section 3), additive-only mirroring, compare before and after every write.
- Server version skew. LEGION2 runs 1.4.40, LAB-OMARCHY 1.4.41-recovery with stricter id validation and a `generateDocument` path that fails with "Access to the path is denied" on LAB-OMARCHY (measured today) after partially writing `docs/Project/Functional-Requirements.md`. Mitigation: do not run `generateDocument` on LAB-OMARCHY until the server can write the workspace, and record projection diffs when it runs anywhere.
- Deleted receipts on LEGION2. The four R0 hostile-review receipt folders are deleted in the LEGION2 working tree (measured). They remain in `d00e8c5`. Phase A step 1 must restore or explicitly dispose of them before any R0 decision (D2) is executed on that machine.
- Plugin version drift. LEGION2 cache is 1.119.0, LAB-OMARCHY cache is 1.118.0 (patched today), marker contract says 1.120.0. The YAML-construction fix is committed on each plugin repo's current branch (not yet on their default branches) and applied to the LAB-OMARCHY caches; LEGION2's caches run the unfixed code until they are refreshed from the fixed commits.

## 6. Decisions Payton must make before the next agent goes past Phase A

v1 decisions D1 to D9 stand unchanged. D10 to D13 below were added by this handoff and then resolved by the agent on 2026-10-17 at Payton's instruction "resolve your questions and keep going" (section 9 records the outcome); they stay listed so the rule applied is visible and reversible:

- D10 (resolved: LAB-OMARCHY content wins for all 27, applied to LEGION2; receipts `~/codex-dispatch/conflict-merge-20261017/`). The 27 content conflicts (10 FR, 9 TR, 7 TEST, plus `TEST-ROMM-LAUNCH-001` and 8 mapping rows only on LAB-OMARCHY). A: LEGION2 wins; overwrite the LAB-OMARCHY versions from the LEGION2 export (reversible from `req-backup-20261017T082025`). B: LAB-OMARCHY wins; copy the Linux PR7 versions to LEGION2. C: merge by hand record by record under a plan (requirement change, profile 4.3).
- D11 (resolved: C for now, left LEGION2-only; the validation inconsistency is filed as McpServer triage `triage-report-c4099eed5e4f4f82a075a486ddbc3a24`, group `triage-group-bc5b5580d87e1637`; renaming is a requirement change that needs its own plan). The 66 ids LAB-OMARCHY rejects (57 two-segment TR ids such as `TR-CYCLE-001`, `TR-CTX-001`, `TR-SIMD-001`, and 9 TEST ids such as `TEST-XBOXUI-002a`, `TEST-INPROC-DEVICE`). A: rename them to the canonical format on LEGION2 under a planned, reviewed requirement change (every mapping, test XMLDoc and receipt that cites them follows). B: relax the validation in the McpServer build deployed on LAB-OMARCHY (McpServer change, separate repo). C: leave them LEGION2-only and claim every gate that cites them on LEGION2.
- D12 (resolved: B; setting statuses is a done-state change that section 4.5 of the profile reserves for hostile AGREE, and the plugin cannot write `closed`). The 17 `closed` records. A: set them to `completed` where their criteria are satisfied and to `pending` where they are not (through the plugin, on both servers). B: leave `closed` on LEGION2 and default on LAB-OMARCHY until Phase G.
- D13 (resolved: A; the nine containers were created on LAB-OMARCHY from the LEGION2 export, receipts `~/codex-dispatch/todo-mirror-20261017/`). TODO stores. A: mirror the nine lockstep containers to LAB-OMARCHY. B: keep TODOs on LEGION2 only and link the four LAB-OMARCHY `DIAG-*` TODOs by id in the LEGION2 containers' notes.

## 7. Exact next actions for the next agent

1. Run `/add-profile`, read the whole marker, verify signature and health nonce, confirm plugin status on the machine you are on. On LAB-OMARCHY the plugin cache is 1.118.0 with today's timeout-envelope fix; on LEGION2 it is 1.119.0 without it.
2. Confirm `git rev-parse HEAD` on `develop` is `d00e8c5584d0031b3637c649335964f8d5225da0` or later. On LEGION2 also confirm whether the four R0 HV receipt folders are still deleted in the working tree; if so, stop and report before any R0 action.
3. Present section 6 (D1 to D13) to Payton as one batched question and wait. Do not fix forward on any of them.
4. Execute Phase A on the machine you are on, then on the other machine over its supported route, and write both receipts. Do not build the native shim.
5. On Phase A completion and D3 answered, prepare the Phase B review packet on LEGION2 and launch the reviewer with the standing launch shape.

## 8. Paths, hashes and identifiers (additions to v1 section 8)

- v1 handoff: `docs/receipts/handoff/HANDOFF.lockstep-plan.md`, SHA256 `7EDAA9A0494831229A84774CC99309E5F2BACBEB2945B5FC6039575284B7C5DD`.
- LAB-OMARCHY requirement backup before sync: `~/codex-dispatch/req-backup-20261017T082025/` (raw list YAML, effective layer, `summary.json`, `hashes.json`).
- LEGION2 export: `~/codex-dispatch/legion2-export-20261017T082422/` (zip SHA256 `ABEBD3E1F3F0E6DC7D5715E70DE86AA7D08DCF70061151E66B80DEE055E30DC9`, extracted under `extracted/`).
- Comparison before sync: `~/codex-dispatch/diag/compare-legion2-vs-omarchy-before.json`.
- Import receipts: `~/codex-dispatch/import-omarchy-20261017T082422-run3/` (one params and one response file per wrapper call, `import-summary.json`).
- Sync scripts (PowerShell, analyzer-clean): `~/codex-dispatch/scripts/Export-McpRequirements.ps1`, `Compare-McpRequirementExports.ps1`, `Import-McpRequirementsAdditive.ps1`, `Try-BlockedIdRoutes.ps1`, `Restore-TransferFile.ps1`.
- Plugin fix: `~/github/mcpserver-claude-code-plugin` (uncommitted: `lib/Invoke-McpPlugin.ps1`, `lib/McpPluginShim.psm1`, `tests/TimeoutEnvelope.Tests.ps1`).
- PR7 failure evidence: `TestResults/pr7-resolution/snapshot-green-verified.trx` (LAB-OMARCHY only).
- LEGION2 WSMan route: `~/.creds/Invoke-LegionWsman.ps1` (runs as `lab-wsman`; pwsh 7 at `C:\Program Files\PowerShell\7-LabWsman`; the REPL is at `C:\Users\kingd\.dotnet\tools\mcpserver-repl.exe` and must be supplied through `MCP_REPL_EXECUTABLE` and PATH for that user).

## 9. Sync outcome on 2026-10-17 and ratings

Measured after the final export at 2026-10-17T08:59:13 (`~/codex-dispatch/req-final-20261017T085913/summary.json`, compared in `~/codex-dispatch/diag/compare-legion2-vs-omarchy-final.json`):

- LAB-OMARCHY before: FR 10, TR 9, TEST 8, mapping rows 23 (all `in_progress`).
- LEGION2 (source): FR 235, TR 139, TEST 142, mapping rows 747 (233 FR groups).
- LAB-OMARCHY after: FR 235, TR 82, TEST 134, mapping rows 612 (230 FR groups). Statuses after: FR 213 pending / 13 in_progress / 9 completed; TR 52 / 12 / 18; TEST 113 / 10 / 11.
- Created today: FR 225, TR 73, TEST 126, mapping links 532 (pass 1: 108 FR sets; pass 2: 120 FR sets, 200 links). No record was updated or deleted.
- Residual versus LEGION2: FR 0 missing, 15 differing (10 content conflicts, 5 status-only where LEGION2 reads `closed`); TR 57 missing (server rejects the ids), 17 differing (9 conflicts, 8 status-only); TEST 9 missing (server rejects the ids), 11 differing (7 conflicts, 4 status-only), 1 LAB-OMARCHY-only (`TEST-ROMM-LAUNCH-001`); mapping rows 143 missing (every one references a rejected TR or TEST id), 8 LAB-OMARCHY-only.
- Side effect to dispose of: `docs/Project/Functional-Requirements.md` in this checkout was overwritten by the server's `generateDocument` projection during the backup and after-export calls (working tree shows it modified; the committed version is intact in `d00e8c5`). Not reverted; operator decision.

Second pass, after Payton's "resolve your questions and keep going" (measured 2026-10-17T18:28, `~/codex-dispatch/diag/compare-legion2-final-vs-omarchy-final.json`):

- D10 applied: LEGION2 received LAB-OMARCHY's content for the 27 shared ids (26 updates, of which 8 TR updates had to be sent singly because the LEGION2 wrapper passes params on the command line and the batch exceeded the Windows argument limit), `TEST-ROMM-LAUNCH-001` created, 6 FR mapping sets unioned. Two LEGION2 statuses changed from `completed` to `in_progress` (`FR-SID-013`, `TR-DET-001`); both had zero acceptance criteria on LEGION2 and now carry 5 and 6 unsatisfied criteria. The rule and per-id record: `~/codex-dispatch/conflict-merge-20261017/merge-decision.json`.
- LEGION2 after: FR 235, TR 139, TEST 143, mapping rows 755. LAB-OMARCHY after: FR 235, TR 82, TEST 134, rows 612. Compare: FR 230 identical and 5 status-only; TR 74 identical, 8 status-only, 57 missing on LAB-OMARCHY; TEST 130 identical, 4 status-only, 9 missing; 143 mapping rows missing, all referencing the 66 missing ids; nothing exists only on LAB-OMARCHY any more.
- D13 applied: `PLAN-VICEORACLE-001`, `PLAN-VSFLOCKSTEP-001`, `PLAN-C64SET-001`, `PLAN-GROKCOMPLETION-001`, `PLAN-REQAUDIT-001`, `PLAN-VICBA-001`, `BUG-DRV1540-001`, `PLAN-NATIVERESIDUE-003`, `PLAN-ARCHVIC20-001` created on LAB-OMARCHY (9 created, 0 errors).
- `docs/Project/Functional-Requirements.md` restored to the committed content.
- Single-writer rule (section 3) stays in force: LEGION2 remains the writer for the 66 ids and their mappings until D11 is implemented.

Accuracy rating: 96. Every count above is from a saved export or compare file named here; the one inference (LEGION2 is authoritative until D10 to D12) is labeled. The LockstepValidator line-count difference is unexplained and said so. Completeness rating: 93. The plan covers every known lockstep obligation and the new two-store reality; it does not sync TODOs, does not enumerate the 32 AC-text-only tape groups, and leaves the 66 rejected ids, 27 conflicts and 17 statuses to decisions rather than resolving them.
