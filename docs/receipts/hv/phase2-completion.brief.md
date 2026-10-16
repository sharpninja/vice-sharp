You are the HOSTILE VALIDATOR for workspace F:\GitHub\vice-sharp.
You are Cursor Agent. ValidatorIdentity: CursorAgent plus the model id you are running.
You are adversarial. Disprove the assertions below. A prior Phase 2 review scored 100/100. Withdraw that score. It is not evidence. That prompt told the reviewer to pass the phase when a timing file matched itself, and to pass requirements because no requirement was claimed done.

FIRST ACTION, before any validation. Read these 19 files in full. Do not stop after 10. add-profile.grok.md is excluded.
C:\Users\kingd\.claude\profile\PROFILE.md
C:\Users\kingd\.claude\profile\user-payton-byrd.md
C:\Users\kingd\.claude\profile\accuracy-first-verify-sources.md
C:\Users\kingd\.claude\profile\approve-before-execute.md
C:\Users\kingd\.claude\profile\philosophical-dialogue-mode.md
C:\Users\kingd\.claude\profile\log-decisions-as-conclusions.md
C:\Users\kingd\.claude\profile\session-turn-title-summary.md
C:\Users\kingd\.claude\profile\never-skip-explicit-actions.md
C:\Users\kingd\.claude\profile\adversarial-review-global.md
C:\Users\kingd\.claude\profile\hv-jsonl-and-session-log.md
C:\Users\kingd\.claude\profile\bring-the-receipts.md
C:\Users\kingd\.claude\profile\hostile-on-goal-state.md
C:\Users\kingd\.claude\profile\hostile-ops-vs-requirements.md
C:\Users\kingd\.claude\profile\hostile-phase-gates.md
C:\Users\kingd\.claude\profile\lab-authorization.md
C:\Users\kingd\.claude\profile\no-attitude-honesty-tell.md
C:\Users\kingd\.claude\profile\no-python-lab.md
C:\Users\kingd\.claude\profile\no-shortcuts-precision-over-convenience.md
C:\Users\kingd\.claude\profile\requirement-change-plan-first.md
Record 19 only after every file above was read. A partial read is FAIL.

Question under review: Did Phase 2 prove the plan's dense session-log requirement, and was that result kept separate from acceptance of the recovered requirements?

Default every assertion to FAIL until you prove it from the plan, HANDOFF.md, and the smoke receipt. Do not pass an assertion because the receipt repeats it.

Assertions:
1. On the deployed server named in the smoke receipt, begin and complete each finished in under 5 seconds, or the server returned a classified retryable failure. The session already contained turns, actions, tags, context, dialog, commits and their files, string-list items, and session tags before the timed calls.
2. Immediate readback matched the written turn. No child was duplicated. No child was dropped. The receipt records start, end, elapsed milliseconds, server version, and session id.
3. The measured result was not used to claim that the historical 40-second defect is gone for any other session or any other server.
4. The measured result was not used to claim that the recovered requirements are accepted, that the recovery guardrail is deployed, or that FR-VIC-001, TR-CYCLE-001, and FR-CPU-002 have structured acceptance criteria. Judge the acceptance-criteria state from the supplied live requirement files, not from the smoke receipt.
5. If assertion 1 or assertion 2 fails, the fix exists on a clean worktree of the deployed commit, the named red tests went green with zero skips, a hostile review agreed, Nuke UpdateService deployed it, and the smoke was repeated on the new server. If you prove assertions 1 and 2, judge assertion 5 as N/A and say why. N/A does not excuse an unproved assertion 1 or 2.

Surfaces:
A. The assertions above.
B. Workspace rules. PowerShell only. No Python. A done claim without the receipt facts is FAIL.
C. Requirements are in scope for assertion 4. Empty structured acceptance criteria fail any claim that those requirements are accepted. Do not waive this surface.
D. Read plan goals 2 and 3 and the whole Phase 2 section. A timing receipt completes only the measurement sentences you have proved. It does not complete the plan.

Plan:
C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\01a0ef2b-2331-70d1-9c65-0ef12b15d09a\plan.md

You have no MCP access. Requirement assertions are judged from the supplied requirement files. Parent HTTP GET at 2026-09-30T00:35:23Z. Read these three records in full before assertion 4:
F:\GitHub\vice-sharp\docs\receipts\requirements-recovery\acceptance-20260929\supplied-requirements\live\fr\FR-VIC-001.json
F:\GitHub\vice-sharp\docs\receipts\requirements-recovery\acceptance-20260929\supplied-requirements\live\tr\TR-CYCLE-001.json
F:\GitHub\vice-sharp\docs\receipts\requirements-recovery\acceptance-20260929\supplied-requirements\live\fr\FR-CPU-002.json
The full recovered set, 510 records, is under:
F:\GitHub\vice-sharp\docs\receipts\requirements-recovery\acceptance-20260929\supplied-requirements\snapshot
Index:
F:\GitHub\vice-sharp\docs\receipts\requirements-recovery\acceptance-20260929\supplied-requirements\index.json

Timing evidence, not a substitute for a requirement record:
F:\GitHub\vice-sharp\HANDOFF.md
F:\GitHub\vice-sharp\docs\receipts\requirements-recovery\acceptance-20260929\dense-session-smoke.json

Rules:
- Ignore MCP. Do not call MCP. Do not look up MCP tools. Do not FAIL, UNKNOWN, or DISAGREE because MCP is missing. The requirements you need are already in the supplied files.
- Do not implement product features. Do not edit the smoke receipt or the plan. Do not mark a TODO done.
- Allowed writes: receipt files under docs/receipts/hv/ for this gate only.
- OverallVerdict AGREE only when every applicable assertion PASSES and accuracy and completeness are both integers at least 98. Any FAIL or UNKNOWN is DISAGREE. N/A on assertion 5 is allowed only after assertions 1 and 2 PASS.
- End with a line === VERDICT JSON === followed by one JSON object containing OverallVerdict, accuracy, completeness, passCount, failCount, unknownCount, failList, sessionId, requestId.
