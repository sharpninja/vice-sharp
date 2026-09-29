# Hostile Validator Receipt (VIC-20 plan closeout at HEAD)

- TimestampUtc: 2026-09-04T03:23:39Z
- ValidatorIdentity: GrokSubagentHostile
- Workspace: F:\GitHub\vice-sharp
- Work class: project implementation (VIC-20 plan closeout at HEAD). Surfaces A, B, C, and D all apply. Not a user-directed ops action.
- add-profile: executed yes. Non-skill profile markdown files read in full: 18 (PROFILE.md, user-payton-byrd.md, accuracy-first-verify-sources.md, approve-before-execute.md, philosophical-dialogue-mode.md, log-decisions-as-conclusions.md, session-turn-title-summary.md, never-skip-explicit-actions.md, adversarial-review-global.md, bring-the-receipts.md, hostile-on-goal-state.md, hostile-ops-vs-requirements.md, hostile-phase-gates.md, lab-authorization.md, no-attitude-honesty-tell.md, no-python-lab.md, no-shortcuts-precision-over-convenience.md, requirement-change-plan-first.md). Excluded skill port add-profile.grok.md.
- Active plan: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\01a06a23-81e8-7b41-8fa0-4773aa19757c\goal\plan.md
- MCP session: GrokCode-20260904T031444Z-hostilevic20, requestId req-20260904T031444Z-001-hostile-vic20-closeout, beginTurnId 43347. Persistence proof: sessionlog_query text "Hostile validate VIC-20 plan closeout at HEAD" returned totalCount=1, turn status completed, 2 processingDialog items (analysis + decision), 6 actions, 2 designDecisions, 2 filesModified, 1 blocker. Session lastUpdated 2026-09-04T03:26:48.2441895+00:00.
- Method: add-profile first; live MCP todo_get PLAN-VIC20-EXACT-001 and requirements_list; re-read implementer gate logs; independent isolated `dotnet test` re-runs; git diff --check; vice_xvic.dll timestamps; HANDOFF/README/audit/USER-GUIDE/Iteration-Roadmap; FR/TR/TEST/mapping. No product feature edits. Receipt files only.
- Independent tests (validator, Release, --no-build after proving TestHarness bin Chips.dll 2026-09-04T02:42:24Z is newer than Mos6561.cs 02:41:46Z and TestHarness.dll 02:37:34Z is newer than Vic20PixelLockstep.cs 02:32:35Z):
  - Vic20PixelLockstep.Bgra_ReadyNtsc_SequenceEqual: Passed 1 Failed 0 Skipped 0 EXIT=0 Duration 6 s
  - DisplayName DualVia_ControlRegs_Lockstep cycles 64: Passed 1 Failed 0 Skipped 0 EXIT=0 Duration 128 ms
  - Flash040EraseLatencyTests: Passed 6 Failed 0 Skipped 0 EXIT=0 Duration 77 ms
  - Vic20SnapshotRoundTripTests: Passed 4 Failed 0 Skipped 0 EXIT=0 Duration 1 s
  - Vic20VideoLockstep.EveryCycle_VicI_VideoState_Match_2k_Ntsc: Passed 1 Failed 0 Skipped 0 EXIT=0 Duration 1 s
  - Vic20SoundLockstep.Tone_MatchesXvic: Passed 1 Failed 0 Skipped 0 EXIT=0 Duration 739 ms
  - git diff --check: DIFF_CHECK_EXIT=0
- Accuracy rating: 88/100. Gate logs and independent spot-checks agree on the named tests. Residual 12: 2s/500k/full 179 not re-run here (durations in implementer lockstep log still disprove vacuous env-off returns).
- Completeness rating: 90/100. Audit L68 vs L88 Exact contradiction was in-scope for plan AC4 and was scored. IEEE/rsuser/printer correctly treated as Explicit Missing.

## OverallVerdict

DISAGREE

Explicit FAIL list:

- D1: Plan AC4 requires audit Exact language to match evidence. `docs/audit-vic20-vs-vice-2026-08-07.md` Pixel FB capture bullet still says "full bit-exact SequenceEqual still open" while the Palette section (updated this slice) and README claim READY PAL/NTSC and busy PAL native BGRA SequenceEqual Exact/green. Prior PAL/busy hostile follow-up already named this L68/L88 split; closeout did not fix L68.
- D2: HANDOFF.md L90 and L103 still say full-canvas / full BGRA remains Partial. docs/USER-GUIDE.md still says full-canvas BGRA remains Partial. docs/Iteration-Roadmap.md L71 still lists full-canvas BGRA parity as remaining polish. README this slice removed full-canvas BGRA from "Still open". Plan AC4 named audit/HANDOFF/README as the closeout language gate. HANDOFF and the wiki mirror of the audit are not aligned with the evidence README now states.

Applicable PASS count: A1-A8, B1-B5, B6 N/A, C1-C2, D3. FAIL count: D1, D2. UNKNOWN: none applicable.

Do not mark the closeout plan step or PLAN-VIC20-EXACT-001 as whole-machine Exact. Do not treat prior receipt `docs/receipts/hostile-validator-20260904T023329Z.md` as this closeout AGREE.

## Claims reviewed

### A. Requested validation

- A1 Isolated Vic20PixelLockstep index+BGRA READY PAL, READY NTSC, busy PAL all 1/0/0. Verdict: PASS
- A2 Cart/flash managed 37/0/0 and isolated sound 8/0/0. Verdict: PASS
- A3 Vic20SnapshotRoundTripTests 4/0/0. Native WriteSnapshot hang Explicit Missing, not a skip. Verdict: PASS
- A4 Isolated video 2k/500k PAL+NTSC, workload 250k PAL+NTSC, 2s PAL+NTSC with VICESHARP_LOCKSTEP_2S=1 Failed=0 Skipped=0. 10s not run, fallback captured. Verdict: PASS
- A5 Process-isolated Vic20 umbrella Failed=0 Skipped=0. Managed batch 179/0/0. Native facts/theory isolated. Same-process hang not counted as pass. Verdict: PASS
- A6 git diff --check exit 0. vice_xvic.dll not rebuilt (timestamp 2026-08-11). Verdict: PASS
- A7 Does not claim whole-machine Exact. IEEE/rsuser/printer/niche carts/WriteSnapshot hang remain Explicit Missing. Verdict: PASS
- A8 Prior PAL/busy hostile AGREE is slice evidence only, not this closeout. Verdict: PASS

### B. Workspace rules

- B1 Byrd v4 for this implementation closeout. No FAIL from FR createdAt vs file mtimes. Verdict: PASS
- B2 Receipts / honesty of pass claims vs artifacts. Verdict: PASS
- B3 MCP-only storage. Verdict: PASS
- B4 PowerShell-only / no Python. Verdict: PASS
- B5 Honesty: no whole-Exact overclaim; 10s absence disclosed; vacuous NativeVice filter misses not counted as green in the rollup. Verdict: PASS
- B6 Look-before-delete: N/A (no deletions). Scored N/A not FAIL.

### C. Requirements (pixel/lockstep claimed-complete scope)

- C1 FR-VIC20-001 / TR-VIC20-PIXEL-001 / TEST-VIC20-001 map exists. Verdict: PASS
- C2 FR-VIC20-001 AC-PX-01..06 have named tests (index PAL/NTSC/busy, BGRA PAL/NTSC/busy, geometry/alpha). Not suite-green theater. Verdict: PASS

IEEE/rsuser/printer not scored as missing AC (Explicit Missing / non-goals).

### D. Plan holistically (this closeout, not only PAL/busy residual)

- D1 Audit Exact language matches evidence (plan AC4). Verdict: FAIL
- D2 HANDOFF/README/audit set is internally consistent with HEAD evidence (plan AC4). Verdict: FAIL
- D3 Independent hostile AGREE is still the open checklist item; implementer did not treat PAL/busy AGREE as this closeout. Verdict: PASS

## Per-claim evidence

### A1 PASS

Attack: early pixel log MSBuild `--no-build` splat (EXIT=1 on 5 of 6); vacuous `if (!ViceNativeXvic.IsAvailable) return`; tautology expand.

Re-verify:

- Implementer retry section (ignore first MSBuild splat): six isolated processes, each `Passed! Failed: 0, Passed: 1, Skipped: 0` EXIT=0. Names: Index_ReadyPal/Ntsc/BusyPal_SequenceEqual and Bgra_ReadyPal/Ntsc/BusyPal_SequenceEqual. PIXEL_GATE=PASS. Log: C:\Users\kingd\AppData\Local\Temp\grok-goal-9c6afa0abcfc\implementer\vic20-pixel-gate.log
- Test source uses native TryCaptureVisibleFrame then SequenceEqual vs managed FrameBuffer (not index-expand tautology). Alpha 0xFF asserted on READY PAL and READY NTSC BGRA paths. PAL size 448x284 asserted on READY PAL BGRA.
- Independent isolated re-run of the closeout-new NTSC BGRA fact: filter FullyQualifiedName~Vic20PixelLockstep.Bgra_ReadyNtsc_SequenceEqual, Passed 1 Failed 0 Skipped 0 EXIT=0, Duration 6 s. Six-second native create/step disproves the unavailable-xvic bare return.

### A2 PASS

Attack: 37 could omit Flash040 erase latency; sound 8 could omit xvic SequenceEqual.

Re-verify:

- Implementer cart+flash managed: Passed 37 Failed 0 Skipped 0 EXIT=0 in 912 ms. Sound: eight isolated processes, each 1/0/0 EXIT=0 including Silence_MatchesXvic and Tone_MatchesXvic (971 ms). Log: ...\vic20-cart-sound-gate.log
- Independent Flash040EraseLatencyTests: 6/0/0 EXIT=0 (TYPE_B cycle budgets). Independent Vic20SoundLockstep.Tone_MatchesXvic: 1/0/0 EXIT=0 Duration 739 ms.

### A3 PASS

Implementer isolated class: Passed 4 Failed 0 Skipped 0 EXIT=0. Independent same filter: 4/0/0 EXIT=0 Duration 1 s. Four facts exist (inventory, unexpanded blob RT, short resume lockstep, FE3 config blob). No Skip on WriteSnapshot. Hang remains Explicit Missing.

### A4 PASS

Attack: env2s unset vacuous return counted as pass; 2k PAL banner Passed 2 is a prefix collision; missing DivergeProbe TwoSecondNtsc; 10s implied green.

Re-verify:

- Lockstep log: 2k PAL banner Passed 2 because FullyQualifiedName~...Match_2k also matches 2k_Ntsc; separate 2k_Ntsc 1/0/0. FocusedWindow (500k) PAL banner Passed 2 then Ntsc 1/0/0. Workload 250k PAL+NTSC 1/0/0 each. TwoSecondPal/Ntsc with env2s=1 durations 8 s and 7 s (not instant return). DivergeProbe TwoSecondPal env2s=1 Duration 8 s. Line: "10s not run this gate: wall budget used 2s PAL/NTSC instead (plan fallback)." LOCKSTEP_FAILED_PROCESSES=0.
- No Vic20DivergeProbe TwoSecondNtsc fact exists. Workload TwoSecondNtsc is the NTSC 2s CPU gate (RunEveryCycle).
- Independent 2k NTSC: 1/0/0 EXIT=0 Duration 1 s.

### A5 PASS

Attack: "No test matches" EXIT=0 as green; MSB4177 EXIT=1 comma filters; 179 vs listed 242; hang counted as pass.

Re-verify:

- Managed Vic20 batch: Passed 179 Failed 0 Skipped 0 Duration 2 s. `--list-tests FullyQualifiedName~Vic20` listed 242; NativeVice collection accounts for the rest (pixel/lockstep/sound/snapshot/native facts). 2 s duration matches managed-only.
- First DualVia/VicI FQN(cycles: N) filters: "No test matches" EXIT=0 (vacuous). Retry DisplayName AND filters: 1/0/0 each. CpuRegs comma filters MSB4177 EXIT=1. PAL no-comma retries: 1/0/0 for 64/4096/256!~ntsc/1024!~ntsc/5000!~ntsc. NTSC DisplayName vic20ntsc 256/1024/5000: 1/0/0. Rollup names those retries, not the misses. UMBRELLA=PASS. Explicit Missing named: IEEE-488, rsuser, printer, niche carts, native WriteSnapshot hang.
- Independent DualVia cycles 64 DisplayName filter: 1/0/0 EXIT=0 Duration 128 ms.
- Hang lines are banners only ("class hang", "hang is not a pass", WriteSnapshot hang). No killed testhost counted as Failed=0.

### A6 PASS

Independent `git diff --check` DIFF_CHECK_EXIT=0. native/vice_xvic.dll LastWriteTimeUtc=2026-08-11T18:10:36.4494845Z Length=10930779. TestHarness bin and native\ copies match that length/time. Core bin copy is older/smaller (2026-08-11T18:03:19Z Length=10927648); testhost uses the 10930779 copy. No native export rebuild this slice.

### A7 PASS

MCP todo_get PLAN-VIC20-EXACT-001: Done=true from 2026-08-11 scoped program; Remaining residuals IEEE/rsuser/printer, niche carts, WriteSnapshot hang, NativeVice isolation. Note updated HEAD 2026-09-04: pixel index+BGRA PAL/NTSC/busy green; Not whole-machine Exact. Plan non-goals match. Implementer A7 does not overclaim Exact.

### A8 PASS

This review is a new receipt. Prior AGREE docs/receipts/hostile-validator-20260904T023329Z.md is PAL/busy residual only and left NTSC BGRA open. Plan checklist still has independent hostile AGREE unchecked. Implementer exact-receipts.txt says "closeout hostile: PENDING".

### B1 PASS

Late closeout review. Did not FAIL from FR createdAt vs file mtimes. Pixel/lockstep tests exist for the claimed ACs. NTSC BGRA test and Mos6561 clamp-row opaque-black land in the uncommitted slice; this review is the implementation-exit hostile, not a missing red-phase timestamp FAIL.

### B2 PASS

Did not trust plan checkboxes or implementer logs alone. Re-read logs, re-ran named isolated tests, re-ran git diff --check, re-stated dll timestamps.

### B3 PASS

git status --short for docs/todo.yaml / todo.yaml / docs/Project/TODO.yaml: empty. TODO/requirements/session via MCP tools only. Receipts written under docs/receipts (allowed).

### B4 PASS

Validator used pwsh invoke_expression and dotnet. No python. Implementer scratch is PowerShell gate logs.

### B5 PASS

Test-result claims match artifacts after retry isolation. 10s not implied green. Whole-machine Exact not claimed. Docs contradiction is scored under D, not as fabricated test counts.

### B6 N/A

No deletions.

### C1 PASS

MCP mapping FR-VIC20-001 -> TR-VIC20-PIXEL-001 -> TEST-VIC20-001. FR-VIC20-001 has six structured ACs (AC-PX-01..06). TR-VIC20-PIXEL-001 has AC-TP-01..03. TEST-VIC20-001 condition text covers PAL/NTSC/busy index, non-sentinel, shared-palette BGRA, opaque alpha. Status fields remain pending; that is tracking hygiene, not missing IDs.

Markdown docs/requirements/functional/FR-VIC20.md FR-VIC20-001 is the older stub video AC set; MCP FR-VIC20-001 is the pixel lockstep set used by this plan. Cart/sound markdown ACs exist; MCP FR-VIC20-005 and FR-VIC20-SOUND-001 structured AC arrays are empty. Parent scoped C to pixel/lockstep; cart/sound tests exist and were gated. Not a C FAIL for IEEE/rsuser/printer.

### C2 PASS

Named tests: Index_ReadyPal/Ntsc/BusyPal_SequenceEqual, Bgra_ReadyPal/Ntsc/BusyPal_SequenceEqual, Vic20PixelFrameTests geometry/non-sentinel. Video 2k/500k and workload 250k/2s cover lockstep ACs in-repo. TEST-VIC20-001 structured AC still only Index_ReadyPal; condition text is broader. Follow-up: add TEST ACs for NTSC/busy/BGRA. Not missing test coverage.

### D1 FAIL

Plan AC4: "audit/HANDOFF/README Exact language matches evidence only". Audit Pixel FB capture (L68) still: "geometry match tests green; full bit-exact SequenceEqual still open". Palette (L88, this slice): Exact for READY PAL, READY NTSC, and busy PAL native BGRA SequenceEqual. Wiki mirror docs/Project/wiki/github/VIC20-vs-VICE-Audit.md still has L68 open. Evidence: independent Bgra_ReadyNtsc 1/0/0 and implementer six-fact pixel gate. L68 is false relative to HEAD evidence.

### D2 FAIL

HANDOFF L90: "full-canvas BGRA remains Partial per the audit". HANDOFF L103: "native-canvas-versus-managed full BGRA remains Partial". USER-GUIDE VIC-20 row: "full-canvas BGRA remains Partial". Iteration-Roadmap L71 remaining polish still includes full-canvas BGRA parity. README this slice: BGRA SequenceEqual green for READY PAL/NTSC and busy PAL; full-canvas BGRA removed from Still open. Closeout AC4 required the named docs to match evidence. They do not agree with each other or with L68 vs L88.

Scoped "other border modes Partial" would be honest if L68 used that qualifier. It does not.

### D3 PASS

Plan hostile AGREE checkbox still open. exact-receipts.txt closeout hostile PENDING. Prior PAL/busy AGREE not used as this closeout. PLAN-VIC20-EXACT-001 remains scoped done from 2026-08-11, not whole-machine Exact.

## Follow-ups after DISAGREE (parent; not optional if they want closeout AGREE)

1. Change audit Pixel FB capture status so SequenceEqual for READY PAL/NTSC and busy PAL is Exact (or equivalent evidence-matched wording). Keep other border modes / full remaining canvas Partial if that is still true.
2. Align HANDOFF L90/L103, USER-GUIDE, Iteration-Roadmap remaining polish, and wiki export with that wording.
3. Tighten TEST-VIC20-001 structured ACs and TR-VIC20-PIXEL-001 body so NTSC BGRA SequenceEqual is not described only as a Partial residual.
4. Re-run this closeout hostile. Do not mark the plan complete on the PAL/busy receipt.

## Files written by validator

- docs/receipts/hostile-validator-20260904T032339Z.md
- docs/receipts/hostile-validator-20260904T032339Z.json
