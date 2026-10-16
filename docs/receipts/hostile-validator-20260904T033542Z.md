# Hostile Validator Receipt (VIC-20 D1/D2 closeout recheck)

- TimestampUtc: 2026-09-04T03:35:42Z
- ValidatorIdentity: GrokSubagentHostile
- Workspace: F:\GitHub\vice-sharp
- Work class: project implementation (VIC-20 plan closeout D1/D2 recheck after docs/receipts/hostile-validator-20260904T032339Z.md DISAGREE). Surfaces A, B, C, and D all apply. Not a user-directed ops action.
- add-profile: executed yes. Non-skill profile markdown files read in full: 18 (PROFILE.md, user-payton-byrd.md, accuracy-first-verify-sources.md, approve-before-execute.md, philosophical-dialogue-mode.md, log-decisions-as-conclusions.md, session-turn-title-summary.md, never-skip-explicit-actions.md, adversarial-review-global.md, bring-the-receipts.md, hostile-on-goal-state.md, hostile-ops-vs-requirements.md, hostile-phase-gates.md, lab-authorization.md, no-attitude-honesty-tell.md, no-python-lab.md, no-shortcuts-precision-over-convenience.md, requirement-change-plan-first.md). Excluded skill port add-profile.grok.md.
- Active plan: C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\01a06a23-81e8-7b41-8fa0-4773aa19757c\goal\plan.md
- MCP session: GrokCode-20260904T033322Z-hostile-d1d2, requestId req-20260904T033322Z-001-hostile-d1-d2-recheck, beginTurnId 43355. Persistence proof: sessionlog_query text Hostile recheck D1/D2 VIC-20 closeout docs returned totalCount=1, turn status completed, 3 processingDialog items, 6 actions, 4 designDecisions, 2 filesModified. Session lastUpdated 2026-09-04T03:37:57.5433200+00:00.
- Method: add-profile first; live MCP todo_get PLAN-VIC20-EXACT-001 and requirements_list mapping; independent Select-String/Get-Content of canonical audit/HANDOFF/README/USER-GUIDE/Iteration-Roadmap; wiki File.Open ReadWrite probe; git diff --check; A7 todo Note re-read. No product feature edits. Receipt files only. Did not re-run lockstep tests this turn (this slice is docs-only; git diff --stat on the named canonical files is 5 markdown files).
- Accuracy rating: 93/100. D1/D2 phrases re-scanned on disk. Residual 7: A1-A6 not re-executed here; they rest on prior independent isolated tests in docs/receipts/hostile-validator-20260904T032339Z.md plus no src/tests change in this docs slice.
- Completeness rating: 96/100. Named prior FAILs, A7, wiki ACL, mapping, and plan AC4 language were re-checked. Residual nits recorded below were not raised to FAIL (do not raise the bar).

## OverallVerdict

AGREE

Explicit FAIL list: none.

Applicable PASS count: A1-A8, B1-B5, C1-C2, D1-D3. FAIL count: 0. UNKNOWN: none applicable. B6 N/A (no deletions).

Wiki under docs/Project/wiki/github: validator write probe Access to the path is denied (Attributes ReadOnly+Archive). Do not FAIL solely because generated wiki copies could not be written by this validator. A later generated refresh at 2026-09-04T03:33:11Z already matches the canonical Exact-scoped wording.

Do not mark PLAN-VIC20-EXACT-001 as whole-machine Exact. IEEE/rsuser/printer, niche carts, and native WriteSnapshot hang remain Explicit Missing.

## Claims reviewed

### A. Requested validation

- A1 Isolated Vic20PixelLockstep index+BGRA READY PAL, READY NTSC, busy PAL all 1/0/0. Verdict: PASS (prior independent re-run; no product code in this docs slice)
- A2 Cart/flash managed 37/0/0 and isolated sound 8/0/0. Verdict: PASS (prior)
- A3 Vic20SnapshotRoundTripTests 4/0/0. Native WriteSnapshot hang Explicit Missing, not a skip. Verdict: PASS (prior)
- A4 Isolated video 2k/500k PAL+NTSC, workload 250k PAL+NTSC, 2s PAL+NTSC with VICESHARP_LOCKSTEP_2S=1 Failed=0 Skipped=0. 10s not run, fallback captured. Verdict: PASS (prior)
- A5 Process-isolated Vic20 umbrella Failed=0 Skipped=0. Managed batch 179/0/0. Native facts/theory isolated. Same-process hang not counted as pass. Verdict: PASS (prior)
- A6 git diff --check exit 0 this turn. vice_xvic.dll not rebuilt this docs slice. Verdict: PASS
- A7 Does not claim whole-machine Exact. IEEE/rsuser/printer/niche carts/WriteSnapshot hang remain Explicit Missing. Verdict: PASS
- A8 Prior PAL/busy hostile AGREE is slice evidence only, not this closeout. This receipt is the closeout recheck after D1/D2. Verdict: PASS

### B. Workspace rules

- B1 Byrd v4 for this implementation closeout. No FAIL from FR createdAt vs file mtimes. Verdict: PASS
- B2 Receipts / honesty of pass claims vs artifacts. Verdict: PASS
- B3 MCP-only storage. Verdict: PASS
- B4 PowerShell-only / no Python. Verdict: PASS
- B5 Honesty: no whole-Exact overclaim; D1/D2 language now matches evidence. Verdict: PASS
- B6 Look-before-delete: N/A (no deletions). Scored N/A not FAIL.

### C. Requirements (pixel/lockstep claimed-complete scope)

- C1 FR-VIC20-001 / TR-VIC20-PIXEL-001 / TEST-VIC20-001 map exists. Verdict: PASS
- C2 FR-VIC20-001 AC-PX-01..06 have named tests (index PAL/NTSC/busy, BGRA PAL/NTSC/busy, geometry/alpha). Not suite-green theater. Verdict: PASS

IEEE/rsuser/printer not scored as missing AC (Explicit Missing / non-goals).

### D. Plan holistically (this closeout AC4 language gate)

- D1 Audit Exact language matches evidence (plan AC4). Verdict: PASS
- D2 HANDOFF/README/USER-GUIDE/Iteration-Roadmap set is internally consistent with HEAD evidence (plan AC4). Verdict: PASS
- D3 Independent hostile AGREE is this receipt; PAL/busy AGREE was not used as closeout. Verdict: PASS

## Per-claim evidence

### D1 PASS (was FAIL)

Attack: audit Pixel FB capture still says SequenceEqual still open.

Re-verify 2026-09-04T03:33:22Z Select-String on docs/audit-vic20-vs-vice-2026-08-07.md: NOHIT SequenceEqual still open / full-canvas BGRA / BGRA remains Partial.

L68 live: Pixel FB capture: xvic capture_visible_frame wired (first_x crop + canvas palette BGRA); READY PAL/NTSC/busy index and native BGRA SequenceEqual green at HEAD (NTSC unpainted clamp rows opaque black).

L88 live: Exact for READY PAL, READY NTSC, and busy PAL native BGRA SequenceEqual.

L188 live conclusion: READY PAL/NTSC/busy native BGRA SequenceEqual is Exact-scoped; do not read that as whole-machine Exact. Whole-machine Exact is not claimed (L186).

mtimeUtc audit: 2026-09-04T03:28:16.1211288Z.

Residual not FAIL (do not raise the bar): L178 still lists ordered remaining work item 6 Pixel FB lockstep when native export exists. Matrix status and audit conclusion are the AC4 Exact-language gate that previously failed.

### D2 PASS (was FAIL)

Attack: HANDOFF L90/L103, USER-GUIDE, Iteration-Roadmap still say BGRA Partial.

Re-verify live lines:

- HANDOFF L90: Do not claim whole-machine Exact. READY PAL/NTSC/busy palette-index and native BGRA SequenceEqual are Exact-scoped (xvic canvas export). IEEE/rsuser/printer, niche carts, and native WriteSnapshot hang stay Explicit Missing.
- HANDOFF L103: Palette-index and native BGRA SequenceEqual are green for READY PAL/NTSC/busy at HEAD.
- USER-GUIDE L192: READY palette-index and native BGRA SequenceEqual are green for PAL/NTSC/busy captures (scoped Exact, not whole-machine).
- Iteration-Roadmap L71: READY PAL/NTSC/busy native BGRA SequenceEqual are also covered. Remaining polish: input E2E, native snapshot write hang, niche carts/peripherals, zip virtual media. No full-canvas BGRA parity in remaining polish.
- README L70: READY PAL/NTSC and busy PAL native BGRA SequenceEqual is green (xvic canvas RGB).
- README L73 Still open: input E2E, native snapshot write hang, niche carts/peripherals, zip virtual media. No full-canvas BGRA.

Select-String NOHIT those Partial phrases on all five canonical files except HANDOFF L38 historical 2026-08-11 Phase G snapshot canvas RGB may Partial. That line is the 2026-08-11 scoped-Exact recap, not the current discipline block at L90. Not raised to FAIL.

git diff --check on those five files: DIFF_CHECK_EXIT=0. git diff --stat: 5 files, 14 insertions, 14 deletions (docs only).

Wiki: File.Open ReadWrite and WriteAllBytes both Access to the path is denied. Attributes ReadOnly, Archive. Generated copies at 2026-09-04T03:33:11Z now match canonical Pixel FB / USER-GUIDE / Iteration-Roadmap / Project-Overview wording. Not a FAIL.

### A7 PASS (spot-check)

MCP todo_get PLAN-VIC20-EXACT-001: Done=true from 2026-08-11 scoped program. Note: HEAD 2026-09-04 remaining IEEE/rsuser/printer Explicit Missing; niche carts Explicit Missing; native WriteSnapshot hang Explicit Missing; NativeVice isolation. Pixel index+BGRA PAL/NTSC/busy SequenceEqual green at HEAD. Not whole-machine Exact.

DoneSummary still contains historical 2026-08-11 canvas RGB may Partial. Remaining field lists IEEE/rsuser/printer/niche carts/WriteSnapshot hang, not BGRA as open. HANDOFF L109: Not whole-machine Exact. Audit L186/L188 same. Plan non-goals match. Not an overclaim.

### A6 PASS

Independent git diff --check DIFF_CHECK_EXIT=0 this turn on the five canonical docs.

### A1-A5 PASS

No src/ or tests/ in this docs slice (git diff --stat named files only). Prior closeout hostile independent isolated re-runs remain the test evidence: Bgra_ReadyNtsc 1/0/0 Duration 6 s; DualVia cycles 64 1/0/0; Flash040EraseLatencyTests 6/0/0; Vic20SnapshotRoundTripTests 4/0/0; Vic20VideoLockstep 2k Ntsc 1/0/0; Vic20SoundLockstep Tone 1/0/0. See docs/receipts/hostile-validator-20260904T032339Z.md.

### A8 PASS

This is a new receipt. Prior AGREE docs/receipts/hostile-validator-20260904T023329Z.md is PAL/busy residual only. Prior DISAGREE docs/receipts/hostile-validator-20260904T032339Z.md named D1/D2, now re-checked PASS.

### B1-B5 PASS, B6 N/A

Late closeout language recheck. MCP todo/session/requirements via tools. git status --short for docs/todo.yaml / todo.yaml / docs/Project/TODO.yaml: empty. Validator used pwsh invoke_expression. No python invoked. Honesty: L38 historical Partial left as recap, not buried.

### C1 PASS

requirements_list type=mapping includes FrId FR-VIC20-001 TrIds TR-VIC20-PIXEL-001 TestIds TEST-VIC20-001.

### C2 PASS

Named tests still exist from prior gate. Structured TEST AC hygiene follow-up from prior receipt is not a new gating FAIL.

### D3 PASS

Plan checklist hostile AGREE was the remaining item. This receipt is that closeout AGREE. PAL/busy receipt not substituted.

## Residual nits (not FAIL)

1. HANDOFF.md L38 2026-08-11 Phase G recap still says canvas RGB may Partial.
2. Audit ordered remaining work item 6 still says Pixel FB lockstep when native export exists.
3. PLAN-VIC20-EXACT-001 DoneSummary still has the 2026-08-11 canvas RGB may Partial sentence.
4. Generated wiki is ReadOnly; validator cannot rewrite it. A generator refresh already matched canonical at 2026-09-04T03:33:11Z.

## Files written by validator

- docs/receipts/hostile-validator-20260904T033542Z.md
- docs/receipts/hostile-validator-20260904T033542Z.json
