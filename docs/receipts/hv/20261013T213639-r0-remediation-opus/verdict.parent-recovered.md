**Progress** (no measured timestamp available; shell denied). completionPercent: 100. etaSeconds: 0. Review complete. Nothing persisted: plan mode denied every write path.

2026-10-13T21:41 (the last time I could measure; I can't measure when this response was composed)

The verdict is **DISAGREE at 97/97**, with 2 FAIL and 4 UNKNOWN out of 46 assertions. The candidate is not ready to present for approval.

**Why**
- **One false count in the approval text.** The text says "nine mapping rows, seven changed, two unchanged". In fact six rows change and three are identical before and after: FR-RUNTIME-TAPE-002, FR-CPU-004 and FR-PRF-003. Both earlier verdicts had already recorded the FR-RUNTIME-TAPE-002 row as a no-op. The wrong sentence appears in four places.
- **This review run can't count as an accepted review.** It was launched in plan permission mode. That mode blocked the PowerShell console, all MCP tools and all file writes, even though PowerShell.MCP was on the allow-list. So I could not write verdict files, open or complete my session-log turn, compute any hash, check the marker signature, or query live requirements.
- **Four UNKNOWNs** come from those same blocks: candidate hash, input pins, the parent's recovery turn, and live store state.

**What is fixed**
- Both earlier FAILs are substantively remediated.
- TEST-CPU-001 ac-01..ac-10 and TEST-PRF-001 ac-01..ac-04 bind by ID to all five rewritten FR-CPU-004/FR-PRF-003 criteria.
- Every number and ordering claim in those new criteria matches the immutable VICE source, including the random-draw order 3,4,5,6,7 / 7,6,5,4,3, the 349989 clock-wrap value, and the strict `<` expiry.
- I re-checked the 14 original records against native source and found no contradiction.

The full verdict text follows so you can capture it.

`✶ Insight ─────────────────────────────────────`
- **Array-to-string bug (RM-03).** In PowerShell, `$g = if (...) { @('x') } else {...}` assigns the `if` block's output, which turns a one-element array into a plain string. That is why 14 coverage rows show `"namedCaseGroups": "C64_..."` instead of a list. `$g = @(if (...) {...})` or `[string[]]$g` keeps the array.
- **Why plan mode blocked everything.** Plan mode blocks any tool that isn't read-only, regardless of `--allowedTools`. Combined with "do not exit plan mode" and "persist and read back", the brief had no way to succeed. The pre-review checklist did not catch this conflict.
- **Why the bad count passed 177 checks.** No check recomputed the changed/unchanged mapping counts or the field types from the data. A linter that recomputes stated counts, plus a deliberately wrong test input, would have caught both RM-01 and RM-03.
`─────────────────────────────────────────────────`

# verdict.md (content only; not written to disk because plan mode denies writes)

# Hostile validation verdict: R0 two-finding remediation candidate (16 records, 9 mapping rows)

Reviewer: ClaudeCode, model claude-opus-5-5 (Claude Opus 5.5), effort max. Local Claude Code CLI session bab22fd4-f213-4011-9616-074b14def01b, PID 22508, launched 2026-10-13T21:41:27 per launch-result.json, on PAYTON-LEGION2 in F:\GitHub\vice-sharp. Identity and arguments come from request.jsonl and launch-result.json and are consistent with this runtime's model identity. No nested reviewer, substitute model, cloud task, web tool, turn cap or time cap.

Parent: Codex, Codex-20261007T135626Z-plugin-session / req-20261014T021233Z-remediate-two-hv-failures.

Reviewer MCP session and turn: none. The CLI ran with `--permission-mode plan`. Every PowerShell.MCP and MCP call was denied (exact text below). This reviewer created no session, turn, dialog chunk, failsafe record or receipt file.

Time: the only measured local times available are 2026-10-13T21:41 (prompt hook) and 2026-10-13T21:41:27 (launch). Later times could not be measured, and none is stated.

OverallVerdict: DISAGREE. 46 assertions: PASS 39, FAIL 2, UNKNOWN 4, N/A 1. Accuracy 97. Completeness 97.

R0 readiness: NOT READY to present the exact delta for operator approval.

Candidate: `docs\receipts\requirements-recovery\20261013T212217-r0-two-finding-remediation\proposed-field-delta.json`. SHA256 `98D42B84FBA5AB833BC41F199A9A9CE0C56B5CF8D5B3EF0689895FAAE3D73BC9` as pinned; input-pins.json, verification.json, consolidated-coverage.json and HANDOFF.md all agree on it. I did not recompute it.

Both prior FAILs are substantively remediated. The stage contract, R2 backlog, locked-decision-2 disclosure and writer-window contract are coherent. Every count I recomputed matches except one.

DISAGREE comes from four things:
1. A8: the approval-facing mapping description is false.
2. B10: this review run was launched in a permission mode that denies the receipts, MCP turn, console isolation, trust checks and live reads that the plan and HANDOFF require.
3. Four UNKNOWNs caused by those denials.
4. Both scores are below 98.

This verdict approves and applies nothing, satisfies no acceptance criterion, and authorizes no TODO, requirement, phase, plan or goal done-state change.

## Exact permission denials

1. `mcp__pwsh__start_console` (is_subagent=true): "Cannot call mcp__pwsh__start_console while in plan mode."
2. `mcp__pwsh__execute_command` (is_subagent=true): "Cannot call mcp__pwsh__execute_command while in plan mode."
3. `mcp__mcpserver__requirements_list` (type mapping): "Cannot call mcp__mcpserver__requirements_list while in plan mode."
4. `mcp__mcpserver__todo_get` (PLAN-REQAUDIT-001): "Cannot call mcp__mcpserver__todo_get while in plan mode."
5. `mcp__mcpserver__sessionlog_query`: "Cannot call mcp__mcpserver__sessionlog_query while in plan mode."
6. Edit and Write are in the launch's `--disallowed-tools` and are absent from this session. The harness plan-mode instruction forbids any edit except a plan file, and that file could not be written either (no Write tool, shell denied). I used no side channel to write.

These denials blocked:
- my own PowerShell.MCP console and agent_id, and applying the request.jsonl environment in it;
- SHA256 of the profile, pins and candidate;
- measured timestamps;
- marker HMAC and `/health` nonce;
- live requirement, mapping, TODO and session-log queries;
- verdict.md, verdict.json and verdict.persistence.json;
- MCP turn open, append, complete and readback;
- failsafe and triage.

This verdict exists only in this response stream, which the parent captures in response.jsonl. Its JSON was written by hand, because native PowerShell serialization was impossible.

## Classification and boundaries

Class 1, project requirement work, requirements phase (R0). The question is whether the exact UNAPPLIED 16-record, 9-row amendment and its ordered implementation plan are ready for explicit operator approval. Surfaces A to D apply. Executable AC tests, the coordinator, the observer and emulator work are later gates (C10 N/A). Read-only review: I made no product, plan, requirement, TODO, native or receipt change.

## Mandatory first step, trust and environment

- **add-profile ran first.** `C:\Users\kingd\.claude\skills\add-profile\SKILL.md` read from disk in full (66 lines). The profile directory holds exactly one non-skill markdown file, PROFILE-MERGED.md, read in full (361 lines). Profile file count: 1. Hashes not computed (denied); the parent's pins list SKILL.md `9B8AB476...` and PROFILE-MERGED.md `A6AD7EAC...` (62592 bytes).
- **Read in full:** CLAUDE.md, AGENTS.md, and `C:\Users\kingd\.grok\skills\hostile-validator\SKILL.md` (224 lines). Current operator rules were applied over that skill's red-first, UTC and Grok-identity wording.
- **Marker AGENTS-README-FIRST.yaml (397 lines).**
  - I read 394 lines. Lines 3, 343 and 344 were withheld because they carry the API key, a 43-character alphanumeric value that I located with count-only probes and never printed.
  - No other copy exists: the file's 40+ character alphanumeric runs occur on exactly six lines, the three key lines plus three hex digests (lines 63, 76, 397).
  - Marker facts: server `1.4.40+bcae36fdce7ce31e792db9e06a17bff51019bdf9`, PID 6036, markerWrittenAtUtc `2026-10-13T23:24:56.5907443+00:00`, Claude plugin contract 1.118.0 (request.jsonl points at the installed 1.119.0 cache).
  - Signature and nonce verification: not performed (denied).
- **Key scan** for exact 43-character alphanumeric runs: none in this review directory, HANDOFF.md, the original plan, or the 2026-10-10/11 requirements-recovery Markdown. There is one letters-only run in Prepare-Candidate.ps1 and one in api-evidence.json; neither is the key, which contains digits.

## Inputs and pins

- **Review directory:** read request.jsonl, the embedded prompt, SELF-REVIEW.md, Launch-Review.ps1, launch-result.json, .gitignore and stderr.log (empty when read).
  - input-pins.json has 270 entries, matching inputCount.
  - prior-input-revalidation.json has 253 entries: 250 unchanged; 3 changed (add-profile SKILL.md, the marker, HANDOFF.md); 0 missing.
  - CLAUDE.md is not pinned.
  - No pin hash was recomputed.
- **Candidate files read in full:** README.md, proposed-field-delta.json (3,584 lines), TEST-TRACEABILITY.md, R2-BACKLOG.md, FINDINGS.md, verification.json (177 entries), Prepare-Candidate.ps1 and Verify-Candidate.ps1. From live-baseline.json I read the typed reads, the comparison counts and the mapping read. consolidated-coverage.json was checked with targeted queries.
- **Plan:** read in full (373 lines). goal-plan.reference.md is also 373 lines; byte identity not recomputed. HANDOFF.md read in full.
- **Supplements read:** the predecessor packet README and FINDING-DISPOSITIONS.md, plus every source, case, guard, media, motor, recording, test-disposition and observation supplement it links. That includes CAPACITOR-SOURCE-AND-CASES.md, R0-R1-GUARD-CONTRACT.md, N07-N12-SOURCE-AUDIT.md, D64-CONTRACT.md, MEDIA-COVERAGE.md, MOTOR-CONTRACT.md, RECORDING-CONTRACT.md, WIRING-LIFECYCLE-CONTRACT.md and the api-evidence.json source excerpts.
- **Both prior verdicts** read in full.

## Surface A: requested validation

- **A1 UNKNOWN.** Candidate hash identity. Four documents agree, and Launch-Review.ps1:12 refuses launch on a mismatch, but I could not hash the file.
- **A2 PASS.** Sixteen existing records with unique IDs, all with operation update: 9 FR, 2 TR, 5 TEST. The parent's typed reads of all 16 succeeded between 2026-10-13T21:30:35 and 21:31:00 (live-baseline.json).
- **A3 PASS (sampled).** The 14 original payloads are unchanged:
  - one distinctive after-AC string per original record (14/14) occurs in predecessor `320887C5...`;
  - the 14 records still give the predecessor denominators 66/90/56/10/24;
  - Verify-Candidate.ps1:31-34 compares whole records canonically, and verification.json records 14 passes.
  - I could not re-run a full structural diff.
- **A4 PASS.** Hand recount: 66 retained (6+4+4+4+5+6+7+6+5+0+0+7+6+6+0+0); 56 rewritten; 10 unchanged (FR-TAP-001 ac-05, FR-TAP-002 ac-05, FR-CPU-004 ac-01/02/04/07, FR-PRF-003 ac-01/03/05/06).
- **A5 PASS.** 38 added (2+2+3+1+3+1+7+5+10+4), 104 after. `"isSatisfied": false` occurs 170 times (66+104) and `true` 0 times. All after-statuses are pending. FR-BACKFILL-MEDIA-001 closed-to-pending is the only status change.
- **A6 PASS.** 96 amendment rows plus 8 unchanged mandatory rows = 104 = after-AC total. consolidated-coverage.json has 104 requirementId rows and the same counts.
- **A7 PASS.** 117 unique named groups, by hand enumeration of all 96 amendment rows. evidenceStatus and testExecutionCount 0 state that the names are not tests.
- **A8 FAIL.** The claim is "9 mapping rows (7 changed, 2 unchanged)".
  - Rows that actually change: FR-BACKFILL-MEDIA-001 and FR-TAP-001 to FR-TAP-005 (13 additions, 0 removals; 31 references before, 44 after).
  - Rows identical before and after: FR-RUNTIME-TAPE-002, FR-CPU-004, FR-PRF-003. The 96/93 (A6) and 93/84 (A7) verdicts both recorded the FR-RUNTIME-TAPE-002 row as a no-op.
  - Locations of the error: README.md:15, candidate approvalPrerequisites[1] and scopeDisclosure, HANDOFF.md:73 (RM-01).
- **A9 PASS.** All five FR-AC to TEST-AC bindings hold by ID:
  - testCriterionTraceability lists them;
  - each new TEST AC names its FR AC ("For FR-CPU-004/ac-03" and so on);
  - both mapping rows link the TEST IDs;
  - coverage rows carry frAcBinding;
  - the group named in each TEST AC equals its coverage row's group.
- **A10 PASS.** 177 read-only checks. Deriving from Verify-Candidate.ps1's structure gives 1+14+32+66+1+16+5+1+36+1+1+1+1+1 = 177. verification.json has 177 entries, all passed, with candidateSha256 equal to the pin, and its evidence values equal my recomputation. Not re-executed; see RM-08 for the checks' limits.
- **A11 PASS.** 112 projected preimage comparisons (14 records x 8 typed fields): 112 equal, 0 unequal. I compared the FR-CPU-004, FR-PRF-003 and FR-TAP-005 typed reads with the candidate's before values by reading them; they are identical. Live recheck is C9.
- **A12 PASS.** TEST-CPU-001 and TEST-PRF-001: before equals the typed reads at 21:30:58 and 21:31:00; after differs only in acceptanceCriteria.
- **A13 PASS.** 32 R2 groups. Groups that already have specifications in the linked supplements number 85:
  - 41 tape groups (15 motor + 11 recording + 15 wiring/lifecycle);
  - 5 Fixture_*;
  - 5 TapOpen/TapHeader/TapUnknownVersion/TapVersion2;
  - 8 C64_Capacitor_*, 2 C64_Port*, 4 Sx64_*;
  - 9 D64_*, 11 Media_*.
  117 - 85 = 32, which is exactly R2-BACKLOG's list (21 + 11 Turbo_*).
- **A14 UNKNOWN.** Input-pin revalidation: entry counts verified, hashes not recomputable.
- **A15 PASS.** Prior verdict figures match their files. 96/93: 39 PASS, 2 FAIL, 0 UNKNOWN, 1 N/A, 11 findings. 93/84: 35 PASS, 6 FAIL, 0 UNKNOWN, 1 N/A, 18 new and 17 prior findings.
- **A16 UNKNOWN.** Whether the parent recovered the complete 96/93 Markdown into Codex turn req-20261014T020333Z-check-hv-outcome: session-log readback denied.

## Surface B: workspace rules and process

- **B1 PASS.** BDPv4 order is required everywhere it matters: README stage items 3, 5 and 6, R2-BACKLOG, HANDOFF:23 and every new TEST AC preamble require independent correct mocks, plus corrupt mocks rejected inside a green harness, before identical real binding. No red gate.
- **B2 PASS.** Approval and pause boundary: applicationAuthorized is false; R0 approval is not R1 authority; work pauses after an accepted review.
- **B3 PASS.** Preparation is read-only through the supported plugin, and no requirement, mapping, TODO or done-state mutation is claimed. Live confirmation is C9.
- **B4 PASS.** PowerShell scripts with native-object construction. The one analyzer suppression carries a written justification (Prepare-Candidate.ps1:41). I could not reproduce the analyzer run.
- **B5 PASS.** Evidence is labeled honestly:
  - checks are marked as not executable acceptance;
  - historical process gaps are preserved;
  - the 96/93 persistence paragraph is not adopted as fact;
  - the parent's credential-redaction error is disclosed.
- **B6 PASS.** No native change is claimed. I read only the immutable materialization.
- **B7 PASS.** No credential appears in any candidate or receipt file I scanned. The parent's disclosed exposure this turn stays open (RM-07).
- **B8 PASS.** Zero em or en dashes in the candidate directory, HANDOFF.md and this review directory.
- **B9 PASS.** Real reviewer identity and local execution.
- **B10 FAIL.** This review run could not be carried out as specified.
  - Plan permission mode denied every PowerShell.MCP, MCP and write operation (exact text above).
  - The original plan's runner launches Claude with `--permission-mode bypassPermissions` and says the reviewer "may write only durable HV receipt files and its own MCP session-log turn" (plan.md:92-102).
  - HANDOFF:26-28 requires reviewer console isolation and full verdict persistence.
  - The brief said both "do not exit plan mode" and "persist and read back"; no permitted path satisfies both (RM-02).

## Surface C: requirement adequacy against immutable VICE

- **C1 PASS.** All five previously unmapped criteria are now constrained, each with named corrupt mocks to reject:
  - FR-CPU-004 ac-03 by TEST-CPU-001 ac-01;
  - FR-CPU-004 ac-05 by TEST-CPU-001 ac-02;
  - FR-CPU-004 ac-06 by TEST-CPU-001 ac-03..ac-10;
  - FR-PRF-003 ac-02 by TEST-PRF-001 ac-01;
  - FR-PRF-003 ac-04 by TEST-PRF-001 ac-02..ac-04.
- **C2 PASS.** The new TEST AC match the immutable source (see Verified native facts).
- **C3 PASS.** The two TEST records' broad conditions and metadata are preserved word for word. Residual: RM-04.
- **C4 PASS.** I re-audited the original changed AC of the 14 records against datasette.c, tap.c, c64.c, vic20.c, vic20via1.c, vic20via2.c, viacore.c, ciacore.c, c64datasette.c, vic20datasette.c, tapeport.c, sense-dongle.c, cp-clockf83.c, datasette-sound.c, pcf8583.c and rtc.c. No contradiction found.
- **C5 PASS.** Native VSF stays the canonical snapshot format (TEST-TAP-001 ac-06/ac-07; HANDOFF:21 and 141-147). The existing managed VSF support is described as existing, not as missing.
- **C6 PASS.** The D64 TR/TEST records are not linked, created, retired or claimed, and they are absent from the pinned exports. The 36..42-track scope and the true-drive AC remain separate operator decisions (README:36, R2-BACKLOG:51).
- **C7 PASS.** UWP/Xbox implementation is excluded; the historical requirements and the 15 legacy Xbox tests are retained.
- **C8 PASS.** No AC ID is removed, nothing legacy is retired, and all 104 AC are unsatisfied.
- **C9 UNKNOWN.** Live effective and typed requirements, full mappings and TODOs: denied. Pinned exports list the added targets TR-IEC-EDGE-001 and TEST-IEC-TIMING-001 and do not contain the D64 IDs.
- **C10 N/A.** Executable AC-mapped tests are a later gate.

## Surface D: plan holistically

- **D1 PASS.** The locked decisions (2 as a disclosed departure, 3, 6, 10, 11, 12, 13, 14) and the immutable-native amendment are respected. The whole objective is retained.
- **D2 PASS.** There is one approval contract: candidate prerequisites, the README stage contract and HANDOFF agree. They also share the A8 count error.
- **D3 PASS.** All 117 groups, including the 32 that have only AC text, are mandatory R2 designs. The same assertions are bound to real providers after the mocks are green.
- **D4 PASS.** The locked-decision-2 departure is explicit:
  - R1 stays read-only until the operator names and accepts it;
  - a local journal is not presented as server deduplication;
  - the old recovery key is not reused.
  The api-evidence.json excerpts at bcae36fd show that `UpdateRequirementBatchRecord` and `UpsertFrTrMappingRequest` have no idempotency, ETag or revision field.
- **D5 PASS.** The writer window has everything required:
  - a named attestor (the operator or an explicitly delegated owner);
  - complete scope (16 records, 9 FR mapping IDs) and an inventory of every potential writer with acknowledgements;
  - a measured local start and expiry;
  - binding to the approved hash and to the applying agent, session and request;
  - a check before every write and a stop on expiry, revocation or a new writer;
  - G09/G14/G15 extended to reject each missing, expired, revoked or mismatched attestation.
- **D6 PASS.** No compare-and-swap, cross-API atomicity, blind retry or automatic compensation is assumed.
- **D7 PASS.** Observer, loader, native, VSF and cumulative real gates all remain required.
- **D8 PASS.** Work pauses after an accepted review, with no apply.
- **D9 PASS.** Historical Phase 3/4 start authority is retained and not extended to this delta.
- **D10 PASS.** HANDOFF is current for this candidate, apart from the A8 sentence at line 73.

## FAIL and UNKNOWN items

- **A8 FAIL:** mapping rows described as seven changed and two unchanged; in fact six changed and three unchanged (RM-01).
- **B10 FAIL:** this review run was launched in plan permission mode, so required receipts, the MCP turn, console isolation, trust checks and live reads were impossible (RM-02).
- **A1 UNKNOWN:** candidate SHA256 not recomputable.
- **A14 UNKNOWN:** input pins not recomputable.
- **A16 UNKNOWN:** the parent's recovery turn could not be read.
- **C9 UNKNOWN:** live requirements, mappings and TODO state could not be queried.
- **N/A:** C10.

## R0 readiness verdict

NOT READY.

Must fix before presenting for approval:
- RM-01: correct the mapping description in all four places, then re-freeze and re-verify.
- RM-02: rerun the review in a launch configuration that permits reviewer receipts, MCP persistence, PowerShell.MCP and live reads.

Should fix in the same re-freeze to reach 98/98: RM-03, RM-04, RM-05, RM-06, RM-08.

## New findings

- **RM-01 medium (A8, D2): introduced by this fix and copied to four places.** "Nine mapping rows, seven changed, two unchanged" is false. Six rows change (FR-BACKFILL-MEDIA-001, FR-TAP-001 to FR-TAP-005: 13 additions, 0 removals). Three are identical before and after (FR-RUNTIME-TAPE-002, FR-CPU-004, FR-PRF-003).
  - The predecessor said "seven complete mapping rows", not "seven changed".
  - Locations: README.md:15; candidate approvalPrerequisites[1] and scopeDisclosure (generated by Prepare-Candidate.ps1:120 and :147); HANDOFF.md:73.
  - The operator would be asked to approve a misdescribed delta.
- **RM-02 medium (B10, process).** The launch uses `--permission-mode plan` with Edit/Write disallowed.
  - The `--allowedTools` entries for PowerShell.MCP do not override plan mode: every call returns "Cannot call ... while in plan mode", and MCP tools are denied too.
  - The reviewer cannot write receipts, open, complete or read back an MCP turn, use failsafe, verify marker trust, rehash pins or query live state.
  - This contradicts the plan's runner shape (plan.md:92-102) and HANDOFF:26-28. Plan mode plus "do not exit plan mode" plus "persist and read back" leaves no permitted path. The pre-HV checklist did not catch the conflict.
- **RM-03 low (A6/A7): field types drift, mostly introduced by this fix.**
  - 14 coverage rows (TEST-CPU-001 ac-01..ac-10, TEST-PRF-001 ac-01..ac-04) store namedCaseGroups as plain strings; every other row uses an array. This drift is new in this remediation.
  - FR-CPU-004/ac-03 sourceAnchors is also a plain string; that one is inherited from the predecessor ledger.
  - The same 15 fields appear in both the candidate and consolidated-coverage.json.
  - Root cause: Prepare-Candidate.ps1:108 assigns from an `if` statement, which turns one-element arrays into strings.
  - R2 binds to these exact rows (R2-BACKLOG), so a typed consumer would break.
- **RM-04 low (C3): the narrow scope of the new TEST AC is not visible in the requirement store.**
  - TEST-CPU-001 also traces FR-CPU-001, -002, -003 and -005, and TEST-PRF-001 also traces FR-PRF-001, -002 and -004 to -008 (pinned TR-per-FR-Mapping.md:26-30 and 109-116).
  - Once applied, those TEST records' only AC would concern FR-CPU-004 and FR-PRF-003.
  - The partial scope is disclosed only in README and coverageDisclosure, not in the record text.
- **RM-05 low: a sibling of prior R0-06 was left unfixed.** The TEST-TAP-001/ac-03 coverage row carries TapeRead_TimerInterruptTrace and other timer/IRQ groups. It still cites sense-port code (c64pla.c:51-95, vic20via1.c:221-260) plus whole-file datasette glue, but not the `ciacore.c:592-600,1594-1625` and `viacore.c:441-462` anchors that the R0-06 fix added elsewhere (candidate lines 2840-2853).
- **RM-06 low: stale metadata, a sibling of R0-02.** The candidate's top-level `baseline` still says `freshServerReadback=false` with the reason "marker missing after supported hook health failure", pointing at the 2026-10-09 baseline. The current preimage evidence is the live-baseline.json typed reads from 21:30:35 to 21:31:00. The README's general supersession clause covers this, but the field is not called out.
- **RM-07 info/low (B7, open): the parent's credential exposure this turn.** A query-string marker credential appeared in one tool output, as the parent disclosed. It is not erased, no key rotation or operator decision is evidenced, and it joins the still-open F-15. No credential appears in any file I scanned.
- **RM-08 low (A10): the 177 checks can't show they would catch errors.**
  - Every check is a positive assertion; no deliberately wrong input shows any check rejecting a defect.
  - The trace check (Verify-Candidate.ps1:62) only proves the IDs exist.
  - No check recomputes changed/unchanged mapping rows or field types, which is why RM-01 and RM-03 passed.
  - This is about receipt quality, not a BDPv4 product gate.
- **RM-09 info (D4).** The departure disclosure covers updateBatch and UpsertMapping. It does not say whether the idempotent recovery-apply route that locked decision 2 assumed is still supported. The README's option (b) covers this only generically.
- **RM-10 info (an R2 trap, not an R0 text defect).** `tape_do_misalignment` passes `-err` into `lib_unsigned_rand(unsigned, unsigned)` and stores the result in a 64-bit CLOCK, so a "negative" azimuth draw becomes roughly 2^32 cycles (datasette.c:482-502, lib.c:994-1003). The non-neutral fixtures required by TEST-TAP-001/ac-06 must capture this.
- **RM-11 info: reviewer conduct.** Because of plan mode: no console, hashes, measured timestamps, receipts, MCP or failsafe. The marker was read with lines 3, 343 and 344 withheld. Progress times were unmeasured. The JSON was written by hand.
- **RM-12 info: three different approvalStatus values.** 12 records use "unapplied-awaiting-exact-operator-approval", 3 use "unapproved exact expanded delta" and 1 uses "unapplied-awaiting-operator-approval". Inherited, and harmless because the field is not sent to the server.

## Prior finding dispositions

**11 newest (96/93 review)**
- **R0-01:** resolved; residual RM-04.
- **R0-02:** resolved for internal consistency (candidate status and prerequisites reissued, media ac-03 wording updated, HANDOFF step 1 aligned, 32 groups listed). Residuals: RM-01 and RM-06.
- **R0-03:** resolved (explicit departure, operator acceptance required before R1, no server-dedup claim). Info: RM-09.
- **R0-04:** resolved.
- **R0-05:** addressed in the approval documents as that reviewer recommended; the requirement text itself is still silent and this is disclosed.
- **R0-06:** partly resolved; sibling RM-05 remains.
- **R0-07:** resolved by labeling.
- **R0-08:** disclosed; live TODO labels not re-queried (C9).
- **R0-09:** preserved; this session's hook state could not be inspected.
- **R0-10:** preserved; see RM-11.
- **R0-11:** preserved as an R3 blocker.

**35 earlier (93/84 review)**
- **N-01:** resolved for R0; the added targets are present in pinned exports; the D64 TR/TEST remain a separate dependency.
- **N-02:** resolved and source-accurate; TEST traceability is now added.
- **N-03:** resolved for R0 (G05; the typed reads contain no synthesized timestamps).
- **N-04:** resolved for R0 (comparisons now carry values).
- **N-05:** resolved in text (datasette.c:1000-1021, 1059-1147).
- **N-06:** resolved in text (datasette.c:1182-1193).
- **N-07:** specified (datasette.c:86, 436-502; viacore.c:423-424); later gate.
- **N-08:** specified (datasette.c:879-984); later gate.
- **N-09:** specified (datasette.c:391-409, 1000-1005, 1091-1099, 1238-1268); later gate.
- **N-10:** specified (tap.c:250-256, 389-457; datasette.c:504-535, 607-692); later gate.
- **N-11:** resolved in text.
- **N-12:** resolved in text; loader fixtures are a later gate.
- **N-13:** resolved as a disclosure (117 group names verified).
- **N-14:** resolved as a disclosure.
- **N-15:** addressed by 63 direct and 47 adjacent test dispositions; execution later.
- **N-16:** preserved.
- **N-17:** preserved.
- **N-18:** resolved for R0; live TODO state UNKNOWN.
- **F-01, F-02, F-03:** resolved in text.
- **F-04:** resolved in text. The first gap 100008 = 0x186A8 encodes `00 A8 86 01`, with cycle increment 12501.
- **F-05, F-06, F-07:** resolved.
- **F-08:** open (R1 export tooling).
- **F-09, F-10:** open historical limitations.
- **F-11:** open (R2/R3 binding).
- **F-12:** open (R1 export headings).
- **F-13:** resolved for R0.
- **F-14:** largely resolved in text.
- **F-15:** open, plus RM-07.
- **F-16:** historically resolved; current pins not rehashed (A14).
- **F-17:** open, informational.

## Verified native facts (immutable commit cc67418cba58e71d8e70fd353e9de47199ad2075)

- **CPU port forwarding** (c64pla.c:39-46, 77-96, 106-123): motor `!((dir&data)&0x20)`, write `(~dir|data)&0x8`, sense-out `(dir&data)&0x10`. Each is forwarded only when its cached value changes and never on BOARD_SX64, although the caches still update there. The caches start at 0xFF and are not reset.
- **Bit 5 and board inputs:** C64 input bit 5 reads 0 (c64pla.c:61-63, 73-75). The C64 uses pullup 0x17 with live tape inputs; the SX-64 passes `(1,1,1,1,0x07)` (c64memsc.c:226-235; c64mem.c:214-223).
- **Capacitor writes** (c64memsc.c:366-495, mirrored in c64mem.c:366-495; c64.h:79-89):
  - a DDR store draws only for pins going from output to input, captures `pport.data`, in order SX 3,4,5 then 6,7;
  - a DATA store draws for every output pin whether or not the value changes, in order 7,6 then SX 5,4,3;
  - the deadline is `maincpu_clk + base + lib_unsigned_rand(0, base/5)`, with base 350000 or 1500000;
  - the 8500 constant is never used in src.
- **$0001 read** (c64memsc.c:262-340; c64mem.c:256-342): strict `<` expiry on all eligible pins; the stored charge replaces the read value only for input pins (by dir_read). A $0000 read returns dir_read with no expiry.
- **Reset and arithmetic:** reset clears data, direction, read values, charges and flags, but not deadlines (c64pla.c:106-123). CLOCK is uint64_t (types.h:61). `lib_unsigned_rand` is inclusive over a shared PCG state seeded from `time(NULL)` (lib.c:994-1045). There is no clock guard; maincpu_clk is saved and restored by snapshots (maincpu.c:777, 809, 860).
- **Datasette** (datasette.c):
  - delays 32000/1000 (62-63);
  - start helper (1000-1021);
  - commands (1041-1157) and public routing (1159-1169);
  - motor on/off (1172-1206);
  - recording (1210-1322);
  - alarm callback (722-853);
  - attach and reset (879-984);
  - live gap rules and the C16 v1/v2-only branches (504-692);
  - buffer refill at `>=` (391-409).
- **TAP** (tap.c): header handling (121-197); files with fewer than 3 physical payload bytes are rejected (250-256); block decoder (389-457); CBM and TurboTape bands (52-82), divided by 8 in tap_init (1673-1685), with machine inputs of 36*8 to 100*8 (c64.c:231-248; vic20.c:267-284).
- **VIC-20 wiring:**
  - motor from the raw changed PCR byte (vic20via1.c:185-203);
  - sense on PA6, inverted (vic20via1.c:221-252);
  - write from effective PB3 on both PRB and DDRB stores (vic20via2.c:110-119; viacore.c:698-725);
  - VIA reset sets oldpa/oldpb to 0 (viacore.c:423-424);
  - CA1 edge as selected by PCR (viacore.c:441-458).
- **Read pulses:** C64 flux goes to CIA1 FLAG (c64datasette.c:36-50); VIC-20 flux goes to VIA2 CA1 falling (vic20datasette.c:37-50). FLAG handling is asynchronous, with the idle alarm moved to rclk+1 (ciacore.c:1594-1625).
- **Peripherals:**
  - tapeport dispatch is not gated by tapeport_active; powerup is (tapeport.c:245-322);
  - sense dongle (sense-dongle.c:68-94);
  - CP Clock F83 (cp-clockf83.c:161-192);
  - datasette sound uses a 200-entry ring (datasette-sound.c:36-68);
  - the PCF8583 takes centiseconds from the platform and seconds from the halt latch or `time(NULL)+offset` (pcf8583.c:213-251; rtc.c:58-61, 192-195).

## Ranked path to the 98 threshold (minimal edits)

1. **RM-02.** Relaunch using the plan's documented Claude launch (bypassPermissions with Edit/Write and the other disallowed tools), or default mode with `mcp__pwsh__*` and the needed `mcp__mcpserver__*` tools on the allow-list. Remove "do not exit plan mode" from the brief. Make Launch-Review.ps1 refuse `--permission-mode plan`. Rationale: without this, no review run can be accepted.
2. **RM-01.** Replace the sentence in all four places with: "Nine complete mapping rows: six with additive edge changes (FR-BACKFILL-MEDIA-001, FR-TAP-001, FR-TAP-002, FR-TAP-003, FR-TAP-004, FR-TAP-005; 13 additions, 0 removals) and three unchanged rows (FR-RUNTIME-TAPE-002, FR-CPU-004, FR-PRF-003)." Fix Prepare-Candidate.ps1:120/147 too, then re-freeze and re-verify.
3. **RM-03.** Change Prepare-Candidate.ps1:108 to `$groups = @(if ...)` or `[string[]]`, make the FR-CPU-004/ac-03 sourceAnchors an array, then regenerate the candidate and consolidated-coverage.json.
4. **RM-04.** Add one scope sentence to each new TEST AC preamble. For TEST-CPU-001: "These criteria cover only FR-CPU-004 ac-03/ac-05/ac-06; obligations of FR-CPU-001, -002, -003 and -005 under this TEST's condition are not covered and remain unsatisfied." Do the same for TEST-PRF-001 with FR-PRF-001, -002 and -004 to -008. No new AC IDs and no count change.
5. **RM-05.** Add `src/core/ciacore.c:592-600,1594-1625` and `src/core/viacore.c:441-462` to the TEST-TAP-001/ac-03 row and any other row carrying a timer/IRQ group.
6. **RM-06.** Point `baseline` at the live-baseline.json typed reads (21:30:35-21:31:00, 112/112 equal) and mark the 2026-10-09 file as historical origin.
7. **RM-08.** Add checks that recompute counts and types and that each TEST AC names its FR binding. Add deliberately wrong test inputs with expected rejections, and record them in verification.json.
8. **Optional:** RM-09 and RM-12.

## Later mandatory gates

1. A fresh, properly permissioned review: AGREE at 98/98 or better with zero FAIL/UNKNOWN, full JSONL, and the whole verdict read back from the reviewer's own MCP turn.
2. The operator's explicit approval of the exact delta, including the locked-decision-2 departure, plus writer-window evidence.
3. R1: G01-G18 guard tests green against mocks before the coordinator exists; then real guards, application, readback, exports, traceability and a requirement-gate review.
4. R2: every one of the 117 groups gets independent fixtures and negative controls, with the full mock suite green.
5. R3: the same assertions bound to managed and immutable-native providers, with observer completeness and non-interference, lawful loader assets, native VSF, and full cumulative suites at zero failed, skipped or aborted.
6. Separate approvals for the D64 TR/TEST, the 36..42-track scope and the true-drive AC.
7. Whole-plan exit: recovered requirements acceptance, dense session-log proof, immutable xvic/x64sc parity, canonical VSF, settings visibility, the final handoff, and the separate commit order.

## Persistence

Status: NOT PERSISTED.
- No MCP turn: session-log tools were denied.
- No failsafe record: writes were denied.
- No verdict.md, verdict.json or verdict.persistence.json: Write/Edit are disallowed and the shell was denied.
- No primary readback.

The only copy is this response stream in response.jsonl. If the parent stores this text, it should label it as parent-recovered reviewer output, not reviewer-persisted output. It cannot serve as an accepted gate; it is DISAGREE in any case.

## Receipt paths

- request: `F:\GitHub\vice-sharp\docs\receipts\hv\20261013T213639-r0-remediation-opus\request.jsonl`
- response: `F:\GitHub\vice-sharp\docs\receipts\hv\20261013T213639-r0-remediation-opus\response.jsonl`
- stderr, input pins, launch result: `stderr.log`, `input-pins.json`, `launch-result.json` in the same directory.
- verdict.md, verdict.json, verdict.persistence.json: NOT WRITTEN.

## Scope limitations and reviewer conduct

- **Not run:** any hash, test, build, git command or live MCP query.
- **Not verified:** marker trust (signature and nonce).
- **Partly verified:** equality of the 14 original payloads, by sampling.
- **Partly recorded:** profile file count recorded; hashes not computed.
- **Unmeasured times:** progress entries carried no measured time.
- **Failed lookups:** two Grep calls on McpServer paths failed because the paths do not exist, and one Glob timed out (all read-only).
- **Plan file:** the harness asked for a plan file and ExitPlanMode. Neither Write, ExitPlanMode nor AskUserQuestion exists in this session, and the brief forbids exiting plan mode, so no plan file was written.

## Scores

- **Accuracy 97.** Every source-derived statement I checked in the 16 records matches immutable VICE, including all new TEST AC numbers and orderings, and every recomputed denominator matches. Deductions: the approval-facing mapping count is wrong in four places (RM-01); 15 coverage fields have the wrong type (RM-03); the baseline provenance is stale (RM-06); one row's anchors don't match its groups (RM-05).
- **Completeness 97.** Both prior FAILs are remediated. All should-fix items are addressed, the R2 backlog is explicit, and the writer-window and idempotency-departure contracts are complete. Deductions: the narrow scope of the new TEST AC is not visible in the store (RM-04); the R0-06 sibling sweep is incomplete (RM-05); the verification could not catch RM-01 and RM-03 (RM-08).

Both scores are below 98 and two assertions FAIL, so AGREE is not possible.

=== VERDICT JSON ===
```json
{
  "OverallVerdict": "DISAGREE",
  "accuracy": 97,
  "completeness": 97,
  "assertionCount": 46,
  "passCount": 39,
  "failCount": 2,
  "unknownCount": 4,
  "notApplicableCount": 1,
  "failList": [
    "A8 FAIL: approval-facing mapping description '9 rows, 7 changed, 2 unchanged' is false; actual 6 changed (FR-BACKFILL-MEDIA-001, FR-TAP-001..FR-TAP-005; 13 additions, 0 removals) and 3 unchanged (FR-RUNTIME-TAPE-002, FR-CPU-004, FR-PRF-003); wrong at README.md:15, candidate approvalPrerequisites[1] and scopeDisclosure, HANDOFF.md:73 (RM-01)",
    "B10 FAIL: HV round launched with --permission-mode plan; PowerShell.MCP, MCP session/requirements/TODO tools and all writes denied, so reviewer receipts, MCP turn, console isolation, marker trust checks and live reads required by plan.md:92-102 and HANDOFF.md:26-28 are impossible (RM-02)"
  ],
  "unknownList": [
    "A1 UNKNOWN: candidate SHA256 not recomputable (shell denied)",
    "A14 UNKNOWN: 270 input pins not rehashable (shell denied)",
    "A16 UNKNOWN: parent recovery of full 96/93 Markdown into req-20261014T020333Z-check-hv-outcome not readable (sessionlog_query denied)",
    "C9 UNKNOWN: live effective/typed requirements, full mappings and TODO state not queryable (MCP tools denied)"
  ],
  "notApplicableList": [
    "C10 N/A: executable AC-mapped tests are a later gate for an unapplied amendment"
  ],
  "findings": [
    "RM-01 medium [must fix before approval] Mapping rows misdescribed as seven changed and two unchanged; actual six changed, three unchanged; propagated to README, candidate approvalPrerequisites and scopeDisclosure, HANDOFF",
    "RM-02 medium [must fix before any acceptance] Plan permission mode denies all reviewer receipts, MCP persistence, PowerShell.MCP, trust checks and live reads; contradicts plan runner and HANDOFF; brief rules deadlock",
    "RM-03 low [should fix] 15 coverage fields stored as scalar strings instead of arrays (14 new TEST namedCaseGroups, 1 inherited sourceAnchors) in candidate and consolidated-coverage.json; root cause Prepare-Candidate.ps1:108",
    "RM-04 low [should fix] New TEST-CPU-001/TEST-PRF-001 AC cover only FR-CPU-004/FR-PRF-003 while those TESTs also trace FR-CPU-001..003,005 and FR-PRF-001,002,004..008; partial scope not stated in record text",
    "RM-05 low [should fix] R0-06 sibling unswept: TEST-TAP-001/ac-03 timer/IRQ row lacks ciacore.c and viacore.c anchors",
    "RM-06 low [should fix] Candidate top-level baseline still says freshServerReadback=false from 2026-10-09; current preimage evidence is live-baseline.json 21:30-21:31",
    "RM-07 info/low [operator disposition] Parent's disclosed credential exposure this turn remains open (no rotation evidenced); joins F-15; no credential found in scanned files",
    "RM-08 low [should fix] 177 checks are positive-only with no seeded negative controls; trace check proves ID existence only; no count/type recomputation, so RM-01 and RM-03 passed",
    "RM-09 info [optional] Locked-decision-2 disclosure does not state whether the recovery-apply idempotent route remains supported",
    "RM-10 info [R2 hint] Native azimuth error uses unsigned rand with negative min stored in 64-bit CLOCK; negative draws become about 2^32 cycles",
    "RM-11 info [reviewer self-disclosure] No console, hashes, measured times, receipts, MCP or failsafe; marker lines 3, 343, 344 withheld; JSON hand-composed",
    "RM-12 info [optional] approvalStatus uses three different values across 16 records"
  ],
  "priorDispositions": {
    "newest11": "R0-01 resolved (residual RM-04); R0-02 resolved for consistency (residuals RM-01, RM-06); R0-03 resolved; R0-04 resolved; R0-05 addressed in approval documents; R0-06 partial (RM-05); R0-07 resolved; R0-08 disclosed; R0-09 preserved; R0-10 preserved; R0-11 preserved as R3 blocker",
    "earlier35": "N-01..N-06, N-11..N-14, N-18, F-01..F-07, F-13, F-14 resolved in text or for R0; N-07..N-10, N-12, N-15 later gates; N-16, N-17, F-08..F-12, F-15, F-17 open or historical; F-16 historical"
  },
  "mustFixBeforeApproval": ["RM-01", "RM-02"],
  "shouldFixForThreshold": ["RM-03", "RM-04", "RM-05", "RM-06", "RM-08"],
  "r0ReadyForExactDeltaApproval": false,
  "reviewer": {
    "agent": "ClaudeCode",
    "model": "claude-opus-5-5",
    "effort": "max",
    "cliSessionId": "bab22fd4-f213-4011-9616-074b14def01b",
    "pid": 22508,
    "launchedAtLocal": "2026-10-13T21:41:27"
  },
  "parent": {
    "sessionId": "Codex-20261007T135626Z-plugin-session",
    "requestId": "req-20261014T021233Z-remediate-two-hv-failures"
  },
  "sessionId": "none: no MCP session created (sessionlog tools denied in plan mode)",
  "requestId": "none: no MCP turn created (sessionlog tools denied in plan mode)",
  "candidateSha256": "98D42B84FBA5AB833BC41F199A9A9CE0C56B5CF8D5B3EF0689895FAAE3D73BC9",
  "candidateSha256Verification": "as pinned; equal in input-pins.json, verification.json, consolidated-coverage.json and HANDOFF.md; not recomputed (shell denied)",
  "candidatePath": "F:\\GitHub\\vice-sharp\\docs\\receipts\\requirements-recovery\\20261013T212217-r0-two-finding-remediation\\proposed-field-delta.json",
  "receiptDirectory": "F:\\GitHub\\vice-sharp\\docs\\receipts\\hv\\20261013T213639-r0-remediation-opus",
  "requestPath": "F:\\GitHub\\vice-sharp\\docs\\receipts\\hv\\20261013T213639-r0-remediation-opus\\request.jsonl",
  "responsePath": "F:\\GitHub\\vice-sharp\\docs\\receipts\\hv\\20261013T213639-r0-remediation-opus\\response.jsonl",
  "verdictFilesWritten": false,
  "persistenceStatus": "NOT PERSISTED: plan permission mode denied all writes and MCP calls; no MCP turn, no failsafe, no verdict.md/verdict.json/verdict.persistence.json; this response stream (parent-captured response.jsonl) is the only copy; no primary readback",
  "deniedOperations": [
    "mcp__pwsh__start_console: Cannot call mcp__pwsh__start_console while in plan mode.",
    "mcp__pwsh__execute_command: Cannot call mcp__pwsh__execute_command while in plan mode.",
    "mcp__mcpserver__requirements_list: Cannot call mcp__mcpserver__requirements_list while in plan mode.",
    "mcp__mcpserver__todo_get: Cannot call mcp__mcpserver__todo_get while in plan mode.",
    "mcp__mcpserver__sessionlog_query: Cannot call mcp__mcpserver__sessionlog_query while in plan mode.",
    "Edit/Write: in launch --disallowed-tools and absent from session"
  ],
  "profileReceipt": {
    "skill": "C:\\Users\\kingd\\.claude\\skills\\add-profile\\SKILL.md",
    "profileFilesRead": 1,
    "profileFile": "C:\\Users\\kingd\\.claude\\profile\\PROFILE-MERGED.md",
    "profileLines": 361,
    "hashes": "not computed (shell denied)"
  },
  "measuredTimes": {
    "hookLocal": "2026-10-13T21:41",
    "launchLocal": "2026-10-13T21:41:27",
    "later": "not measurable (shell denied)"
  },
  "jsonProvenance": "hand-composed in the response stream; native PowerShell serialization impossible because PowerShell.MCP was denied"
}
```
