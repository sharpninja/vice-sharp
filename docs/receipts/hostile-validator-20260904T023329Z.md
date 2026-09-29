# Hostile Validator Receipt (VIC-20 BGRA residual / AC-PX-05)

- TimestampUtc: 2026-09-04T02:33:29Z
- ValidatorIdentity: GrokSubagentHostile
- Workspace: F:\GitHub\vice-sharp
- Work class: project implementation (VIC-20 pixel BGRA residual of PLAN-VIC20-EXACT / AC-PX-05). Not a user-directed ops action. Surfaces A, B, C, and D all apply.
- add-profile: executed yes. Non-skill profile markdown files read in full: 18 (PROFILE.md, user-payton-byrd.md, accuracy-first-verify-sources.md, approve-before-execute.md, philosophical-dialogue-mode.md, log-decisions-as-conclusions.md, session-turn-title-summary.md, never-skip-explicit-actions.md, adversarial-review-global.md, bring-the-receipts.md, hostile-on-goal-state.md, hostile-ops-vs-requirements.md, hostile-phase-gates.md, lab-authorization.md, no-attitude-honesty-tell.md, no-python-lab.md, no-shortcuts-precision-over-convenience.md, requirement-change-plan-first.md). Excluded skill port add-profile.grok.md.
- MCP session: GrokCode-20260904T022624Z-hostile-bgra, requestId req-20260904T022624Z-001-hostile-validate-vic20-bgra, turnId 43329. Persistence proof: sessionlog_query text "Hostile validate VIC-20 BGRA residual" returned totalCount=1, turn status completed, 4 processingDialog items, 6 actions, 3 designDecisions. Session lastUpdated 2026-09-04T02:35:52.6409726+00:00.
- Method: add-profile first; live MCP todo_get PLAN-VIC20-EXACT-001; independent source inspection of tests, Mos6561.Palette, native vice_machine_capture_visible_frame; independent isolated `dotnet test` re-runs (not implementer logs alone); git diff --check; MCP requirements_list TEST-VIC20-001; HANDOFF/plan/audit/TR wiki. No product feature edits. Receipt files only.
- Independent test log: C:\Users\kingd\AppData\Local\Temp\PowerShell.MCP.Output\pwsh_output_20260903_213054_129_bvzikp1l.m11.txt
- Accuracy rating: 95/100 (named isolated re-runs + native capture source + palette vs PALette.vpl). Residual 5: TEST/TR wiki still describe full-canvas BGRA as Partial; that is scoped language, not a failed SequenceEqual.
- Completeness rating: 92/100. NTSC BGRA not re-run because no such test exists and A8 leaves it open. Full TestHarness suite not in residual scope.

## OverallVerdict

AGREE

All applicable A/B/C/D claims PASS. Explicit FAIL list: none.

Mandatory surfaces that could not be evaluated: none applicable. (get_todo_execution_context rejected PLAN-VIC20-EXACT-001 as a non-execution TODO; todo_get succeeded and was used instead.)

## Claims reviewed

### A. Requested validation

- A1 Bgra_ReadyPal_SequenceEqual is native TryCaptureVisibleFrame BGRA SequenceEqual vs managed FrameBuffer (not index-expand tautology); claimed 1/0/0. Verdict: PASS
- A2 Bgra_BusyPal_SequenceEqual claimed 1/0/0. Verdict: PASS
- A3 BorderCyan_UsesXvicCanvasRgb claimed 1/0/0. Verdict: PASS
- A4 Vic20VideoTests claimed 20 passed 0 failed 0 skipped. Verdict: PASS
- A5 Index READY PAL/NTSC/busy still green after palette change. Verdict: PASS
- A6 Mos6561.Palette RGB matches xvic canvas export (video_calc_palette / Tobias YUV), not raw PALette.vpl. Verdict: PASS
- A7 git diff --check clean (exit 0) after code/docs edits. Verdict: PASS
- A8 Does not claim whole PLAN-VIC20-EXACT-001 reopened or whole-machine Exact; NTSC BGRA still open. Verdict: PASS

### B. Workspace rules

- B1 Byrd v4 for this residual (tests covering AC; red before green; no post-hoc FR-vs-mtime FAIL). Verdict: PASS
- B2 Receipts / honesty of pass claims vs artifacts. Verdict: PASS
- B3 MCP-only storage (no todo.yaml / session-log YAML edits in git status). Verdict: PASS
- B4 PowerShell-only / no Python in this validation and no Python artifacts in the slice. Verdict: PASS
- B5 Honesty: no whole-Exact overclaim; NTSC BGRA disclosed. Verdict: PASS
- B6 Look-before-delete: N/A (no deletions). Scored N/A not FAIL.

### C. Requirements (project implementation)

- C1 FR-VIC20-001 / TR-VIC20-PIXEL-001 / TEST-VIC20-001 / plan AC-PX-05 exist and map. Verdict: PASS
- C2 AC-PX-05 (BGRA SequenceEqual for AC-PX-02 READY PAL and AC-PX-04 busy PAL) has real named tests, not suite-green theater. Verdict: PASS

### D. Plan holistically (residual-done gate, not whole PLAN reopen)

- D1 Implementer does not claim whole PLAN-VIC20-EXACT-001 or whole-machine Exact. Verdict: PASS
- D2 Goal/plan AC-PX-05 residual (READY PAL + busy PAL BGRA) is evidenced. Verdict: PASS
- D3 NTSC BGRA correctly left open; AC-PX-05 does not require NTSC BGRA. Verdict: PASS

## Per-claim evidence

### A1 PASS

Attack: tautology (expand native indices through Mos6561.Palette then compare to FrameBuffer); vacuous pass via `if (!ViceNativeXvic.IsAvailable) return;`; wrong filter in implementer log (no FQN in vic20-bgra-ready-green-2026-09-04.log).

Re-verify:

- Test source `tests/ViceSharp.TestHarness/Vic20/Vic20PixelLockstep.cs` Bgra_ReadyPal_SequenceEqual: after index SequenceEqual, calls `native.TryCaptureVisibleFrame(nBgra, ...)` then `nBgra.AsSpan(0,len).SequenceEqual(mBgra)` where mBgra is `vic.FrameBuffer`. Comment: "Real native BGRA path (not expand-indices tautology)."
- `ViceNative.CreateInstance("vic20")` routes to `ViceNativeXvic` (`ViceNative.cs` L262-266). Xvic `TryCaptureVisibleFrame` P/Invokes `vice_machine_capture_visible_frame`.
- Native `native/vice-shim-vic20.c` L1289-1348 expands native row indices through `vic.raster.canvas->palette` entries (B,G,R,A=0xFF), not managed Palette.
- Independent isolated re-run `--no-build` after confirming Chips.dll 2026-09-04 02:17:00Z and TestHarness.dll 02:17:10Z are newer than Mos6561.cs 02:14:48Z and Vic20PixelLockstep.cs 02:15:43Z:
  - filter `FullyQualifiedName~Vic20PixelLockstep.Bgra_ReadyPal_SequenceEqual`
  - `Passed ViceSharp.TestHarness.Vic20.Vic20PixelLockstep.Bgra_ReadyPal_SequenceEqual [5 s]`
  - Total tests: 1 Passed: 1 BGRA_READY_EXIT=0
  - Duration 5s plus xvic `vice_machine_create` logs disprove the unavailable-xvic bare return.

### A2 PASS

Independent isolated re-run:

- filter `FullyQualifiedName~Vic20PixelLockstep.Bgra_BusyPal_SequenceEqual`
- `Passed ViceSharp.TestHarness.Vic20.Vic20PixelLockstep.Bgra_BusyPal_SequenceEqual [5 s]`
- Total tests: 1 Passed: 1 BGRA_BUSY_EXIT=0
- Same native TryCaptureVisibleFrame path after $900F=0x25 poke + one PAL frame (71*312 steps), with index SequenceEqual first.

Implementer log `docs/receipts/vic20-bgra-busy-cyan-2026-09-04.log` showed BUSY Passed 1 / Failed 0 / Skipped 0 without FQN; independent named run supplies the missing identity.

### A3 PASS

Independent isolated re-run:

- filter `FullyQualifiedName~Vic20VideoTests.BorderCyan_UsesXvicCanvasRgb`
- `Passed ViceSharp.TestHarness.Vic20.Vic20VideoTests.BorderCyan_UsesXvicCanvasRgb [92 ms]`
- Total tests: 1 Passed: 1 BORDER_CYAN_EXIT=0
- Asserts corner BGRA R=0x80 G=0xFF B=0xFF (canvas cyan), not PALette.vpl 0x64/0xE3/0xDE.

Managed-only test; live native RGB is proven by A1/A2 SequenceEqual, not by this fact alone.

### A4 PASS

Independent isolated re-run:

- filter `FullyQualifiedName~Vic20VideoTests`
- Total tests: 20 Passed: 20 VIDEO_EXIT=0
- 20 named Passed lines (including BorderCyan_UsesXvicCanvasRgb). Source `[Fact]` count in Vic20VideoTests.cs is 20. No skipped line.

### A5 PASS

Independent isolated re-runs after palette change (dll newer than source):

- Index_ReadyPal_SequenceEqual: Passed [5 s], 1/1, INDEX_READY_EXIT=0
- Index_ReadyNtsc_SequenceEqual: Passed [5 s], 1/1, INDEX_NTSC_EXIT=0
- Index_BusyPal_SequenceEqual: Passed [5 s], 1/1, INDEX_BUSY_EXIT=0

Each process-isolated. Index path does not depend on RGB table values, so palette change should not break it; re-run confirms it did not.

### A6 PASS

Attack: table still PALette.vpl; comment theater; sampled tautology.

Re-verify:

- `native/vice/vice/data/VIC20/PALette.vpl` cyan line is `64 E3 DE`. Old Mos6561 PackRgb for index 3 was 0x64,0xE3,0xDE (git diff). New is PackRgb(0x80, 0xFF, 0xFF).
- Red log `docs/receipts/vic20-bgra-red-palette-dump2-2026-09-04.log` (before palette edit) failed with `nR=80 nG=FF nB=FF mR=64 mG=E3 mB=DE` and `nativeCanvasRgb: [3]=(80,FF,FF)`. That dump is the live canvas palette, not the .vpl file.
- Native capture uses `vic.raster.canvas->palette`. VICE `video-color.c` `video_calc_palette` fills that canvas palette from Tobias YUV (`vic-color.c`), not raw .vpl bytes.
- Mos6561.cs comment now cites video_calc_palette / Tobias YUV / not raw PALette.vpl.
- SequenceEqual green (A1/A2) means managed static table matches that canvas export for every visible pixel on READY PAL and busy PAL.

Caveat (not FAIL): table was sampled from capture, not independently recomputed from sat/bri/con/gamma math. AC-PX-05 requires SequenceEqual vs xvic canvas, which holds.

### A7 PASS

`git diff --check` on current tree: DIFF_CHECK_EXIT=0 (validator, twice). Touched product/docs files in status: HANDOFF.md, README.md, docs/Iteration-Roadmap.md, docs/audit-vic20-vs-vice-2026-08-07.md, src/ViceSharp.Chips/Vic/Mos6561.cs, tests/.../Vic20PixelLockstep.cs, tests/.../Vic20VideoTests.cs.

### A8 PASS

- MCP `todo_get` PLAN-VIC20-EXACT-001: Done=true, CompletedDate=2026-08-11, DoneSummary still scoped Exact, Remaining still residuals. Not reopened.
- HANDOFF resume-next item 1: pending hostile AGREE; NTSC BGRA still open.
- No `Bgra_Ntsc` / NTSC BGRA test in repo (grep).
- README still lists NTSC BGRA SequenceEqual as open.
- Goal plan AC1: BGRA for READY PAL and the same busy case; NTSC BGRA is not in AC-PX-05 (AC-PX-03 is index only).

### B1 PASS

Late-slice review: do not FAIL from FR createdAt vs file mtimes. Red BGRA logs exist at 02:10/02:12/02:14Z before palette write 02:14:48Z and green 02:21Z. AC-covering tests exist and were shown red, then green. Residual gate is focused isolated tests, not the full harness (plan residual, not Phase G reopen).

### B2 PASS

Implementer logs omitted FQN on several banners. Validator did not trust them: re-ran named isolated tests. Claims match the re-run artifacts.

### B3 PASS

git status --short for docs/todo.yaml / todo.yaml / docs/Project/TODO.yaml: empty. TODO read via MCP todo_get only.

### B4 PASS

Validator used pwsh invoke_expression and dotnet. No python in this review. No .py files in the slice diff.

### B5 PASS

A8 language matches store + HANDOFF. Audit Palette section Exact for READY PAL + busy PAL BGRA is the residual under review, not whole-machine Exact. Older audit/HANDOFF Partial bullets for "full-canvas" BGRA remain as NTSC/other-mode leftovers (see follow-up).

### B6 N/A

No deletions.

### C1 PASS

- FR-VIC20-001 in docs/requirements/functional/FR-VIC20.md
- TR-VIC20-PIXEL-001 wiki: index primary Exact; BGRA after palette table match
- TEST-VIC20-001 MCP + wiki mapping FR-VIC20-001 | TR-VIC20-PIXEL-001 | TEST-VIC20-001
- Plan AC-PX-05: BGRA SequenceEqual for same cases as AC-PX-02 and AC-PX-04

MCP TEST-VIC20-001 Status is still pending and Condition still says "Full-canvas BGRA SequenceEqual remains an explicit Partial residual." That stays true for NTSC/full remaining canvas; PAL ready+busy is the residual this gate covers. Parent should tighten the TEST/TR wording after this AGREE (follow-up, not FAIL of AC coverage).

### C2 PASS

TEST-VIC20-PX-05 in the 2026-08-11 plan names `Bgra_ReadyPal_SequenceEqual` + busy. Both exist and pass in isolation. Not "suite green".

### D1 PASS

todo_get DoneSummary scoped; HANDOFF forbids whole-machine Exact; A8 explicit.

### D2 PASS

Goal plan.md AC1 and plan.md AC-PX-05 require READY PAL BGRA and busy PAL BGRA after palette alignment. Independent tests green. Dimensions PAL 448x284 asserted on READY path; busy path asserts index geometry first.

### D3 PASS

NTSC BGRA open is consistent with AC-PX-05 (PAL ready + PAL busy only) and with A8.

## Explicit FAIL list

None.

## Follow-ups after AGREE (parent; not FAILs)

1. Tighten TEST-VIC20-001 and TR-VIC20-PIXEL-001 so PAL ready/busy native BGRA SequenceEqual is no longer described only as Partial; keep NTSC BGRA / other border modes Partial.
2. Audit L68 still says "full bit-exact SequenceEqual still open" while Palette L88 says Exact for READY PAL and busy PAL BGRA. Reconcile wording.
3. Do not mark PLAN-VIC20-EXACT-001 as whole-machine Exact. Do not add NTSC BGRA to the done claim.

## Files written by validator

- docs/receipts/hostile-validator-20260904T023329Z.md
- docs/receipts/hostile-validator-20260904T023329Z.json
