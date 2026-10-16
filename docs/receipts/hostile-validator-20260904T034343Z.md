# Hostile Validator Receipt (VIC-20 Exact-language closeout2)

- TimestampUtc: 2026-09-04T03:43:43Z
- ValidatorIdentity: GrokSubagentHostile
- Workspace: F:\GitHub\vice-sharp
- Work class: project implementation (VIC-20 plan closeout at HEAD after prior DISAGREE on Exact-language). Surfaces A, B, C, and D all apply. Not a user-directed ops action.
- add-profile: executed yes. Non-skill profile markdown files read in full: 18 (PROFILE.md, user-payton-byrd.md, accuracy-first-verify-sources.md, approve-before-execute.md, philosophical-dialogue-mode.md, log-decisions-as-conclusions.md, session-turn-title-summary.md, never-skip-explicit-actions.md, adversarial-review-global.md, bring-the-receipts.md, hostile-on-goal-state.md, hostile-ops-vs-requirements.md, hostile-phase-gates.md, lab-authorization.md, no-attitude-honesty-tell.md, no-python-lab.md, no-shortcuts-precision-over-convenience.md, requirement-change-plan-first.md). Excluded skill port add-profile.grok.md.
- Active plan: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\01a06a23-81e8-7b41-8fa0-4773aa19757c\goal\plan.md
- MCP session: GrokCode-20260904T033453Z-hostilevic20close, requestId req-20260904T033453Z-001-hostile-vic20-closeout2, beginTurnId 43356. Persistence proof: sessionlog_query text "Hostile validate VIC-20 Exact-language closeout2" returned totalCount=1, turn status completed, 3 processingDialog items, 6 actions, 2 designDecisions, 2 filesModified, 1 blocker. Session lastUpdated 2026-09-04T03:46:58.1008421+00:00.
- Method: add-profile first; live MCP todo_get PLAN-VIC20-EXACT-001 and requirements_list fr/tr/test/mapping; SHA256 wiki vs source; wiki manifest generatedAtUtc; independent isolated `dotnet test` re-runs (including Bgra_ReadyNtsc); git diff --check; vice_xvic.dll timestamps; plan checkbox re-read at end; exact-receipts.txt. No product feature edits. Receipt files only.
- Independent tests (validator, Release, --no-build after proving TestHarness bin Chips.dll 2026-09-04T02:42:24Z is newer than Mos6561.cs 02:41:46Z and TestHarness.dll 02:37:34Z is newer than Vic20PixelLockstep.cs 02:32:35Z):
  - Vic20PixelLockstep.Bgra_ReadyNtsc_SequenceEqual: Passed 1 Failed 0 Skipped 0 EXIT=0 testhost DurationMs=8573; log Duration 4 s
  - Vic20PixelLockstep.Index_ReadyPal_SequenceEqual: 1/0/0 EXIT=0 DurationMs=8160
  - Vic20PixelLockstep.Index_ReadyNtsc_SequenceEqual: 1/0/0 EXIT=0 DurationMs=7512
  - Vic20PixelLockstep.Index_BusyPal_SequenceEqual: 1/0/0 EXIT=0 DurationMs=7793
  - Vic20PixelLockstep.Bgra_ReadyPal_SequenceEqual: 1/0/0 EXIT=0 DurationMs=7746; log Duration 4 s
  - Vic20PixelLockstep.Bgra_BusyPal_SequenceEqual: 1/0/0 EXIT=0 DurationMs=7659
  - Flash040EraseLatencyTests: Passed 6 Failed 0 Skipped 0 EXIT=0; log Duration 46 ms
  - Vic20SnapshotRoundTripTests: Passed 4 Failed 0 Skipped 0 EXIT=0; log Duration 1 s
  - Vic20SoundLockstep.Tone_MatchesXvic: Passed 1 Failed 0 Skipped 0 EXIT=0; log Duration 722 ms
  - git diff --check: DIFF_CHECK_EXIT=0
- Accuracy rating: 93/100. Language SHA/MCP bodies and isolated native-duration tests re-verified. Residual 7: 10s/umbrella/2s not re-run here (not claimed this closeout); wiki FR-VIC20.md remains the historical stub copy.
- Completeness rating: 94/100. Prior D1/D2 phrases re-scanned to NOHIT on canonical docs. A8/D3 scored from live plan.md and exact-receipts.txt after a parallel 033542 AGREE.

## OverallVerdict

DISAGREE

Explicit FAIL list:

- A8: Spawn claim was that the independent hostile AGREE checkbox is still `[ ]` and PAL/busy is not this closeout. Live plan.md L41 is now `[x] Independent hostile AGREE`. Scratch `vic20-exact-receipts.txt` L10 records `closeout hostile AGREE: docs/receipts/hostile-validator-20260904T033542Z.md` (LastWriteTimeUtc 2026-09-04T03:39:39Z). PAL/busy 023329 is still labeled slice-only (L8), but the checkbox is no longer open for this closeout2.
- D3: Plan AC4 requires independent hostile AGREE on this completion claim, including this closeout2 brief's required independent Bgra_ReadyNtsc re-run. Receipt 033542 AGREE is a docs-only D1/D2 recheck that explicitly did not re-run lockstep tests and reused 032339 evidence. The plan item was marked `[x]` on that receipt while this required test-rerun review was still in_progress. hostile-on-goal-state: do not mark a plan step done while a required test is unrun.

Do not treat `docs/receipts/hostile-validator-20260904T023329Z.md` (PAL/busy) or `docs/receipts/hostile-validator-20260904T033542Z.md` (docs-only D1/D2 recheck) as this closeout2 AGREE. Uncheck plan L41. Do not mark PLAN-VIC20-EXACT-001 as whole-machine Exact.

Applicable PASS count: A1-A7, B1-B5, C1-C2, D1-D2. FAIL count: A8, D3. UNKNOWN: none applicable. B6 N/A.

## Claims reviewed

### A. Requested validation

- A1 Prior D1 fixed: audit Pixel FB capture no longer says full bit-exact SequenceEqual still open; READY PAL/NTSC/busy index and native BGRA SequenceEqual green; Palette agrees; wiki audit SHA256 match. Verdict: PASS
- A2 Prior D2 fixed: HANDOFF/USER-GUIDE/Iteration-Roadmap/README no longer say full-canvas/full BGRA remains Partial; remaining polish does not list full-canvas BGRA; wiki Project-Overview/User-Guide/Iteration-Roadmap SHA256 match sources. Verdict: PASS
- A3 MCP FR-VIC20-001, TR-VIC20-PIXEL-001, TEST-VIC20-001 bodies no longer describe full-canvas BGRA as the Partial residual; TEST structured ACs name Index and BGRA including Bgra_ReadyNtsc_SequenceEqual; wiki Functional/Technical/Testing match MCP after generateDocument wiki/all at 2026-09-04T03:33:11Z. Verdict: PASS
- A4 Isolated Vic20PixelLockstep index+BGRA READY PAL, READY NTSC, busy PAL Failed=0 Skipped=0; independent Bgra_ReadyNtsc re-run; native TryCaptureVisibleFrame SequenceEqual vs managed FrameBuffer. Verdict: PASS
- A5 Cart/flash/sound and snapshot ACs still hold (isolated Flash040EraseLatencyTests 6/0/0, Tone_MatchesXvic 1/0/0 Duration 722 ms, Vic20SnapshotRoundTripTests 4/0/0). Native WriteSnapshot hang not counted as a pass. Verdict: PASS
- A6 git diff --check exit 0. vice_xvic.dll LastWriteTimeUtc 2026-08-11T18:10:36.4494845Z Length=10930779 (not rebuilt). Verdict: PASS
- A7 Does not claim whole-machine Exact. IEEE-488, rsuser, printer, niche carts, native WriteSnapshot hang remain Explicit Missing. 10s every-cycle was not re-run this closeout. Verdict: PASS
- A8 Implementer has not marked the independent hostile AGREE checkbox `[x]` yet, and has not treated the PAL/busy receipt as this closeout. Verdict: FAIL

### B. Workspace rules

- B1 Byrd v4 for this implementation closeout. No FAIL from FR createdAt vs file mtimes. Verdict: PASS
- B2 Receipts / honesty of pass claims vs artifacts. Tests and docs re-verified independently. Verdict: PASS
- B3 MCP-only storage. Verdict: PASS
- B4 PowerShell-only / no Python. Verdict: PASS
- B5 Honesty: no whole-Exact overclaim; 10s absence disclosed; vacuous NativeVice returns disproved by 4 s / 7-8 s durations. Premature checkbox scored under A8/D3, not as fabricated test counts. Verdict: PASS
- B6 Look-before-delete: N/A (no deletions). Scored N/A not FAIL.

### C. Requirements (pixel/lockstep claimed-complete scope)

- C1 FR-VIC20-001 / TR-VIC20-PIXEL-001 / TEST-VIC20-001 map exists. Verdict: PASS
- C2 FR-VIC20-001 AC-PX-01..06 have named tests (index PAL/NTSC/busy, BGRA PAL/NTSC/busy, geometry/alpha). TEST-VIC20-001 structured ACs include Bgra_ReadyNtsc_SequenceEqual. Not suite-green theater. Verdict: PASS

IEEE/rsuser/printer not scored as missing AC (Explicit Missing / non-goals).

### D. Plan holistically (this closeout2, not PAL/busy and not 033542)

- D1 Audit Exact language matches evidence (plan AC4). Verdict: PASS
- D2 HANDOFF/README/USER-GUIDE/Iteration-Roadmap set is internally consistent with HEAD evidence (plan AC4). Verdict: PASS
- D3 Independent hostile AGREE on this completion claim remains the required exit; 033542 docs-only AGREE plus a checked plan box is not this closeout2. Verdict: FAIL

## Per-claim evidence

### A1 PASS (prior D1)

Attack: audit L68 still "full bit-exact SequenceEqual still open" vs Palette L88 Exact.

Re-verify:

- `docs/audit-vic20-vs-vice-2026-08-07.md` L68: Pixel FB capture: xvic `capture_visible_frame` wired (first_x crop + canvas palette BGRA); READY PAL/NTSC/busy index and native BGRA SequenceEqual green at HEAD (NTSC unpainted clamp rows opaque black).
- L88 Palette: Exact for READY PAL, READY NTSC, and busy PAL native BGRA SequenceEqual (unpainted NTSC clamp rows are opaque black).
- L188: READY PAL/NTSC/busy native BGRA SequenceEqual is Exact-scoped; do not read that as whole-machine Exact.
- Select-String NOHIT `full bit-exact SequenceEqual still open` on the audit (hits only old receipts).
- Wiki `docs/Project/wiki/github/VIC20-vs-VICE-Audit.md` SHA256 29646F918955E02EA928E3CA206174D02C5D14E23E65BBB50F15A5BD16CDD621 equals the source audit. mtimeUtc 2026-09-04T03:33:11.0619297Z.

Residual not FAIL: audit ordered remaining work item 6 still says "Pixel FB lockstep when native export exists". Matrix/Pixel FB bullet/conclusion are the AC4 language that previously failed.

### A2 PASS (prior D2)

Attack: HANDOFF L90/L103, USER-GUIDE, Iteration-Roadmap remaining polish still Partial full-canvas BGRA.

Re-verify:

- HANDOFF L90: Do not claim whole-machine Exact. READY PAL/NTSC/busy palette-index and native BGRA SequenceEqual are Exact-scoped (xvic canvas export). IEEE/rsuser/printer, niche carts, and native WriteSnapshot hang stay Explicit Missing.
- HANDOFF L103: Palette-index and native BGRA SequenceEqual are green for READY PAL/NTSC/busy at HEAD.
- USER-GUIDE VIC-20 row: READY palette-index and native BGRA SequenceEqual are green for PAL/NTSC/busy captures (scoped Exact, not whole-machine).
- Iteration-Roadmap L71 remaining polish: input E2E, native snapshot write hang, niche carts/peripherals, zip virtual media. No full-canvas BGRA.
- README L70: READY PAL/NTSC and busy PAL native BGRA SequenceEqual is green (xvic canvas RGB). L73 Still open: input E2E, native snapshot write hang, niche carts/peripherals, zip virtual media.
- SHA256 equal pairs: README vs wiki Project-Overview.md (4C2797315D39AE0E3D63050953CFEFA29F790051D3BD764DA095CBB3C3E78EF9); USER-GUIDE vs wiki User-Guide.md; Iteration-Roadmap vs wiki Iteration-Roadmap.md.
- Select-String NOHIT `full-canvas BGRA` on those sources (hits only old receipts).

Residual not FAIL: HANDOFF L38 2026-08-11 Phase G recap still says canvas RGB may Partial. That is the historical scoped-Exact recap, not the current discipline block at L90.

### A3 PASS

MCP requirements_list:

- FR-VIC20-001 body: READY PAL/NTSC/busy index match; READY PAL, READY NTSC, and busy PAL native BGRA SequenceEqual; Exact scoped; other border modes and mid-line Partial. ACs AC-PX-01..06 isSatisfied true. AC-PX-05 text names READY PAL, READY NTSC, and busy PAL BGRA.
- TR-VIC20-PIXEL-001 body: index Exact-scoped READY PAL/NTSC/busy; BGRA SequenceEqual Exact-scoped for the same named captures; other border modes Partial; not whole-machine Exact. AC-TP-01..03 isSatisfied true.
- TEST-VIC20-001 condition names Index_ReadyPal/Ntsc/BusyPal and Bgra_ReadyPal/Ntsc/BusyPal. Structured ACs AC-TEST-PX-02..06 include `Vic20PixelLockstep.Bgra_ReadyNtsc_SequenceEqual passes 0 fail 0 skip (opaque clamp-row alpha)` isSatisfied true, evidence isolated pixel gate 2026-09-04.
- Mapping: FrId FR-VIC20-001 TrIds TR-VIC20-PIXEL-001 TestIds TEST-VIC20-001.
- Wiki Functional-Requirements.md L593-604, Technical-Requirements.md L608-617, Testing-Requirements.md L555-564 match those MCP bodies/ACs plus generateDocument evidence suffixes.
- `.mcp-requirements-manifest.json` generatedAtUtc 2026-09-04T03:33:11.0619297+00:00. Wiki Functional/Technical/Testing LastWriteTimeUtc 2026-09-04T03:33:11.0619297Z.

Status fields remain pending; tracking hygiene, not missing IDs. Canonical `docs/requirements/functional/FR-VIC20.md` is still the older iteration-2 stub for FR-VIC20-001; MCP is the pixel lockstep set used by this plan (same scoring as 032339 C1). Wiki `FR-VIC20.md` is that stub copy. Not raised to FAIL against claim 3, which named MCP store plus wiki Functional/Technical/Testing.

### A4 PASS

Attack: vacuous `if (!ViceNativeXvic.IsAvailable) return`; tautology expand; trust implementer logs.

Re-verify independent isolated Release --no-build:

- Filter FullyQualifiedName~Vic20PixelLockstep.Bgra_ReadyNtsc_SequenceEqual: log `Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 4 s` EXIT=0. Testhost wall 8573 ms. Four-second native create/step disproves the unavailable-xvic bare return.
- Same 1/0/0 EXIT=0 for Index_ReadyPal, Index_ReadyNtsc, Index_BusyPal, Bgra_ReadyPal, Bgra_BusyPal (DurationMs 7512-8160).
- Logs under `C:\Users\kingd\AppData\Local\Temp\hostile-vic20-closeout2\`.
- Test source `tests/ViceSharp.TestHarness/Vic20/Vic20PixelLockstep.cs` Bgra_ReadyPal/Ntsc/BusyPal: `native.TryCaptureVisibleFrame` then SequenceEqual vs `vic.FrameBuffer`. Comment: Real native BGRA path (not expand-indices tautology). Alpha 0xFF asserted on READY PAL and READY NTSC BGRA paths.

### A5 PASS

Independent isolated:

- Flash040EraseLatencyTests: Passed 6 Failed 0 Skipped 0 EXIT=0 Duration 46 ms (TYPE_B cycle budgets).
- Vic20SoundLockstep.Tone_MatchesXvic: Passed 1 Failed 0 Skipped 0 EXIT=0 Duration 722 ms (not instant return).
- Vic20SnapshotRoundTripTests: Passed 4 Failed 0 Skipped 0 EXIT=0 Duration 1 s. No Skip on WriteSnapshot. Hang remains Explicit Missing.

Did not re-run the full cart 37 or sound 8 this turn; named Flash040 + Tone + snapshot class are the required spot-checks. NativeVice same-process hang is not a pass.

### A6 PASS

Independent `git diff --check` DIFF_CHECK_EXIT=0. native/vice_xvic.dll LastWriteTimeUtc=2026-08-11T18:10:36.4494845Z Length=10930779 SHA256 F03A7A3545C5964FBAB863D3F3D1AB7A2EB99C2543ED94071F80F07648513886. TestHarness bin copy matches. Core bin copy is older/smaller (2026-08-11T18:03:19Z Length=10927648); testhost uses the 10930779 copy. No native export rebuild this slice.

### A7 PASS

MCP todo_get PLAN-VIC20-EXACT-001: Done=true from 2026-08-11 scoped program. Note: HEAD 2026-09-04 remaining IEEE/rsuser/printer Explicit Missing; niche carts Explicit Missing; native WriteSnapshot hang Explicit Missing; NativeVice isolation. Pixel index+BGRA PAL/NTSC/busy SequenceEqual green at HEAD. Not whole-machine Exact. Remaining field lists those residuals, not BGRA as open. Plan non-goals match. HANDOFF L109 and audit L186/L188 deny whole-machine Exact. 10s not implied green (plan deviations still name the 2s/250k fallback).

DoneSummary still contains historical 2026-08-11 canvas RGB may Partial. Remaining/Note are the AC4 remaining-text gate. Not an overclaim.

### A8 FAIL

Attack: checkbox already `[x]`; PAL/busy used as closeout; 033542 used as this closeout2.

Re-verify:

- First plan read this turn (after add-profile): L41 was `[ ] Independent hostile AGREE`.
- Live re-read 2026-09-04T03:42:13Z: L41 is `[x] Independent hostile AGREE on this completion claim; copy receipt path into {SCRATCH}/vic20-exact-receipts.txt`.
- Scratch file `C:\Users\kingd\AppData\Local\Temp\grok-goal-9c6afa0abcfc\implementer\vic20-exact-receipts.txt` LastWriteTimeUtc 2026-09-04T03:39:39Z L8 prior PAL/busy slice only; L9 032339 DISAGREE; L10 closeout hostile AGREE 033542; L11 session GrokCode-20260904T033322Z-hostile-d1d2.
- 033542 receipt Method line: Did not re-run lockstep tests this turn (this slice is docs-only).
- PAL/busy 023329 is not the recorded closeout (L8 slice only). The open-checkbox half of the claim is false.

### B1 PASS

Late closeout review. Did not FAIL from FR createdAt vs file mtimes. Pixel/lockstep tests exist for the claimed ACs. This review is the implementation-exit hostile with independent tests.

### B2 PASS

Did not trust plan checkboxes, 033542, or implementer logs alone. Re-ran named isolated tests, re-hashed wiki vs source, re-queried MCP, re-ran git diff --check, re-read plan.md after the race.

### B3 PASS

git status --short for docs/todo.yaml / todo.yaml / docs/Project/TODO.yaml: empty. TODO/requirements/session via MCP tools only. Receipts written under docs/receipts (allowed).

### B4 PASS

Validator used pwsh invoke_expression and dotnet. No python. ConvertFrom-Json used for MCP dump parse.

### B5 PASS

Test-result claims match artifacts. 10s not implied green. Whole-machine Exact not claimed. Premature `[x]` scored under A8/D3.

### B6 N/A

No deletions.

### C1 PASS

MCP mapping FR-VIC20-001 -> TR-VIC20-PIXEL-001 -> TEST-VIC20-001. FR has six structured ACs. TR has AC-TP-01..03. TEST has AC-TEST-PX-02..06 including Bgra_ReadyNtsc.

### C2 PASS

Named tests exist and independently passed this turn: Index_ReadyPal/Ntsc/BusyPal_SequenceEqual, Bgra_ReadyPal/Ntsc/BusyPal_SequenceEqual. Geometry/alpha asserts in those facts. Not suite-green theater.

### D1 PASS

Plan AC4 audit Exact language vs evidence. Prior FAIL L68 open vs L88 Exact is gone. Wiki audit SHA match. Item 6 leftover remaining-work line is a nit, not the prior D1 phrase.

### D2 PASS

Plan AC4 HANDOFF/README set. Prior FAIL Partial phrases are gone on HANDOFF L90/L103, USER-GUIDE, Iteration-Roadmap remaining polish, README Still open. Wiki mirrors SHA-match. L38 historical recap not raised to FAIL.

### D3 FAIL

Plan AC4 last checklist item and verification step 6: independent hostile OverallVerdict AGREE on this completion claim, not only PAL/busy, and this closeout2 brief required an independent Bgra_ReadyNtsc re-run.

033542 OverallVerdict AGREE is a D1/D2 language recheck that did not re-run tests. exact-receipts L10 treats it as closeout hostile AGREE. Plan L41 is `[x]` as of 2026-09-04T03:42:13Z while this required test-rerun turn (beginTurnId 43356) was in_progress.

hostile-on-goal-state: do not mark a plan step done when a required test is unrun. 033542 left the required closeout2 re-run unrun. This validator did run it green; that does not make the premature `[x]` on 033542 this closeout2 AGREE.

## Residual nits (not FAIL)

1. HANDOFF.md L38 2026-08-11 Phase G recap still says canvas RGB may Partial.
2. Audit ordered remaining work item 6 still says Pixel FB lockstep when native export exists.
3. PLAN-VIC20-EXACT-001 DoneSummary still has the 2026-08-11 canvas RGB may Partial sentence.
4. Canonical/wiki `FR-VIC20.md` remains the iteration-2 stub for FR-VIC20-001; MCP + wiki Functional-Requirements.md hold the pixel lockstep body.
5. FR/TR/TEST status fields remain pending while ACs isSatisfied true.

## Follow-ups after DISAGREE (parent; not optional if they want closeout AGREE)

1. Uncheck plan.md L41 Independent hostile AGREE.
2. Do not treat 023329 or 033542 as this closeout2 AGREE. 033542 omitted the required independent Bgra_ReadyNtsc re-run.
3. Re-run this closeout2 hostile after the checkbox is open again, or use this receipt only after unchecking and obtaining a later AGREE. This receipt is DISAGREE.
4. Keep IEEE/rsuser/printer, niche carts, and native WriteSnapshot hang Explicit Missing. Do not claim whole-machine Exact.

## Files written by validator

- docs/receipts/hostile-validator-20260904T034343Z.md
- docs/receipts/hostile-validator-20260904T034343Z.json
