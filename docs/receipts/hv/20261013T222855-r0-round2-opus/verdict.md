# Hostile validation verdict: R0 remediation round 2 candidate (16 records, 9 mapping rows)

Reviewer: ClaudeCode, model claude-opus-5-5 (Claude Opus 5.5), effort max. Local Claude Code CLI session bab22fd4-f213-4011-9616-074b14def01b (resumed; launch-result.json records start 2026-10-13T22:33:16, PID 13176) on PAYTON-LEGION2 in F:\GitHub\vice-sharp. Own PowerShell.MCP sub-agent console, agent_id sa-76fe112f (window #30468); the parent console was not touched. No nested reviewer, substitute model, cloud task, web tool, turn cap or time cap.

Parent: Codex, Codex-20261007T135626Z-plugin-session / req-20261014T021233Z-remediate-two-hv-failures. Operator scope, as quoted by the parent: "Resume. Remediate both HV failures, rerun HV with Opus 5.5 max. Iterate to HV agree then pause."

Reviewer MCP session and turn: ClaudeCode-20261014T033325Z-plugin-session / req-20261014T033328Z-prompt-5bc0. The plugin prompt hook created this turn for this CLI session (see RN-05).

Times are measured local times (UTC-05:00) from my console. This verdict was composed from evidence measured through 2026-10-13T23:04:49. File-write and persistence times are in verdict.persistence.json.

OverallVerdict: DISAGREE. 49 assertions: PASS 47, FAIL 1, UNKNOWN 0, N/A 1. Accuracy 98. Completeness 98.

R0 readiness: NOT READY to present for exact-delta operator approval. The only reason is A18 (RN-01). Everything else needed for presentation is in place.

Candidate: docs\receipts\requirements-recovery\20261013T221507-r0-remediation-round2\proposed-field-delta.json, SHA256 A13429ED21F4764E88EAC9CE5323691AEC98D1C4C227E64BD4AD8E2B64604EF9, 193898 bytes. I recomputed the hash at 22:39:09, 22:55:09 and 23:00:18.

Summary:
- Both round-1 FAILs are fixed.
  - Every approval-facing place now describes the mappings correctly: six changed rows, three unchanged, 13 additions, 0 removals (A8).
  - This review ran in default permission mode. I could isolate my console, verify marker trust, rehash every pin, query live state, write receipts and persist to my own MCP turn (B10).
- All four round-1 UNKNOWNs are resolved with independent evidence (A1, A15, A16, C9).
- Every count in the brief recomputes exactly. RM-03, RM-04, RM-06 and RM-08 are fixed.
- One defect remains (A18 FAIL, RN-01). The remediation documents say CIA/VIA anchors were added to "every timer/IRQ sibling row" and that timer/IRQ anchors are "reconciled". But four rows were not touched: FR-TAP-005 ac-01, ac-03, ac-05 and ac-06.
  - Their criteria require interrupt/timer observations, and the 96/93 finding R0-06 named them explicitly.
  - They still carry the unchanged generic anchor list, with no ciacore.c or viacore.c anchor.
  - The defect is low severity and simple to fix.
- My own round-1 recommendation for RM-05 was scoped by group name and missed these rows. That was my error as reviewer, but it does not make the candidate's claim accurate.
- Both scores reach 98. AGREE is blocked only by the single FAIL.

This verdict approves and applies nothing. It satisfies no acceptance criterion and authorizes no change to the done state of any TODO, requirement, mapping, phase, plan or goal. The full original goal remains incomplete.

## Classification and boundaries

Class 1, project requirement work, requirements phase (R0).

The question: is the exact UNAPPLIED 16-record, 9-row amendment, with its ordered implementation plan, accurate and complete enough to present for explicit operator approval?
- Surfaces A, B, C and D apply.
- Later gates: executable AC-mapped tests, the R1 coordinator, the native observer and emulator work (C10 N/A).
- I did not require completed emulator work before reviewing requirement text.

This was a read-only review.
- My only writes:
  - verdict.md, verdict.json and verdict.persistence.json in this review directory;
  - my own MCP session turn (title, tags, dialog, actions, completion);
  - process-scoped environment variables in my own console;
  - PowerShell.MCP's own Temp output spill files.
- I changed no requirement, mapping, TODO, plan, handoff, product, test, native or canonical document.
- I ran no build, test or Python, used no web access, and did no queue drain, replay, settings change, plugin repair or turn cancellation.
- Native source was read only from artifacts\immutable-vice\cc67418cba58e71d8e70fd353e9de47199ad2075\source\vice. My only git commands were read-only hash-object and ls-tree against .git\modules\native\vice. I did not touch native\vice.

## Mandatory first step, trust and environment

- **add-profile ran first this round (2026-10-13T22:34:56).**
  - C:\Users\kingd\.claude\skills\add-profile\SKILL.md read in full: 4574 bytes, SHA256 9B8AB476410BB2E778A97BA545C94D4A4591A1E40B7E6352777C40C95229A86E.
  - The profile directory holds one non-skill file, PROFILE-MERGED.md, read in full: 62592 bytes, 361 lines, SHA256 A6AD7EAC0CB9CFFE9B9687E02A936484A3ED34FC49CF0CDD2C5FD6C869112AF3.
  - Profile file count: 1.
- **Governance files read in full this round.** All three equal their pins.
  - AGENTS.md, SHA256 5649ACD0870C000119185DF3C911EEF009407AA09C43C5A4C68182F1CB26D727.
  - CLAUDE.md, SHA256 1595F5E7FE057EFA6F71A1A136A8E311FB453B1E034929AA3E810CD628CF07B3.
  - C:\Users\kingd\.grok\skills\hostile-validator\SKILL.md, 15318 bytes, SHA256 F600F16720FC559060DE65B2A6AEF6947BC57A1FF6D4B447704B240CFD1F4BBD.
  - Where the skill's wording (red-first, UTC receipt names, GrokSubagentHostile identity) conflicts with current operator rules, the operator rules were applied. The brief's receipt directory and my real identity win.
- **Marker** F:\GitHub\vice-sharp\AGENTS-README-FIRST.yaml.
  - SHA256 8ADA0C1316EB8499C352FEA7BF62490BE9F6F9B12264D5168D9DF1DA75AC071D, 34217 bytes, 397 lines, last write 2026-10-13T18:24:56.
  - Read in full. My console removed the API key by exact-value redaction before any output (3 occurrences, on lines 3, 343 and 344).
  - Server 1.4.40+bcae36fdce7ce31e792db9e06a17bff51019bdf9, PID 6036, markerWrittenAtUtc 2026-10-13T23:24:56.5907443Z.
  - The marker's plugin contract says 1.118.0, but the installed Claude plugin is 1.119.0. Disclosed, not repaired.
  - The old marker and key were not used.
- **Trust (22:37:22).**
  - Plugin 1.119.0 lib\marker-resolver.ps1: Test-MarkerSignature True, Invoke-FullBootstrap True.
  - /health echoed my independent nonce hv-r0r2-893961d2e7c54fda and reported Healthy.
- **Console isolation (22:36:22), before any plugin operation.**
  - Removed six inherited names: GROK_API_KEY, GROK_BASE_URL, GROK_MEMORY, MCP_AGENT_API_KEY, MCP_AGENT_MODEL_NAME, MCP_AGENT_MODEL_URL. Values were never printed.
  - Applied all 17 request.jsonl environment values (0 mismatches), including:
    - MCP_CACHE_DIR_OVERRIDE = F:\GitHub\vice-sharp\.mcpServer\claude\reviews\20261013T222855-r0-round2-opus;
    - MCP_FAILSAFE_DRAIN_DISABLED = 1.
  - Every wrapper call passed -CacheRoot, because Invoke-McpPlugin.ps1:216-222 ignores MCP_CACHE_DIR_OVERRIDE when -WorkspacePath is explicit.
- **Plugin Status (22:36:56).**
  - available; agent ClaudeCode; isolated cacheDir;
  - hasSession and hasCurrentTurn true;
  - pending 0, failsafe 0, quarantine 0.
  - Plugin child processes run with WorkingDirectory set to the workspace (Invoke-McpPlugin.ps1:225).
- **CLAUDE.md startup steps.** I performed steps 1, 2, 8 and 9 read-only, as above. I did not perform steps 3 to 7 (delete stale plugin caches, refresh hooks), because the brief forbids settings changes, plugin repair and environment repair.
- **Request and prompt.**
  - prompt.md (SHA256 15DDCE8E33498847A70A751AC8B06BB6EF0FD006E2E5E25491F034B74229E29B) is byte-equal to the request.jsonl prompt.
  - request.jsonl uses --permission-mode default with an explicit allow-list. Launch-Review.ps1:17-18 refuses any other mode.

## Inputs, pins and live state

- **Input pins.**
  - input-pins.json: 294 of 294 entries equal by SHA256 at 22:39:09 and again at 22:55:09; 0 missing.
  - prior-input-revalidation.json: 270 entries. 269 are equal and 1 changed intentionally (HANDOFF.md, D6B7DF52 to 59B479B6). All 270 "after" values equal the current files.
  - verification.json is unchanged (last write 22:27:34, B361BD2C...), so my 22:42:01 re-run of Verify-Candidate.ps1 wrote nothing.
- **Candidate directory, read in full.**
  - README.md, FINDINGS.md, TEST-TRACEABILITY.md, R2-BACKLOG.md;
  - proposed-field-delta.json, consolidated-coverage.json, verification.json, live-baseline.json;
  - Prepare-Candidate.ps1, Candidate-Checks.ps1, Verify-Candidate.ps1;
  - HANDOFF.round1.md, HANDOFF.before.md.
  - The large JSON files were parsed programmatically and inspected structurally.
- **Plan and HANDOFF.**
  - The original plan and goal-plan.reference.md are byte-identical (SHA256 436BB2B1DA834491A9B0C8F0F6EFD3F1EBD81F001885AD31D6FE8180CDE26473, 48261 bytes, checked 22:46:42). Read in full (373 lines).
  - HANDOFF.md (SHA256 59B479B66BF781B2B67AB2C691C283C67B5AB23C7A5DBA4E783C5F6F477BE3FA, 169 lines) read in full. Only lines 3, 7, 9, 71 and 73 differ from the round-1 copy.
- **Supplements read in full this round.**
  - Predecessor packet README and FINDING-DISPOSITIONS.md.
  - CAPACITOR-SOURCE-AND-CASES.md, R0-R1-GUARD-CONTRACT.md, N07-N12-SOURCE-AUDIT.md.
  - Media README, D64-CONTRACT.md, MEDIA-COVERAGE.md.
  - Motor README, MOTOR-CONTRACT.md.
  - Recording README, RECORDING-CONTRACT.md, WIRING-LIFECYCLE-CONTRACT.md.
  - The direct and adjacent test-disposition READMEs, the API guard README and the observation README.
- **Prior verdicts read in full.** All pinned and unchanged.
  - 96/93 verdict (SHA256 A7BC4062...).
  - 93/84 verdict (0D3680D6...).
  - The entire parent-recovered 97/97 verdict (44190 characters, 609F0050...) and its JSON (1A9055F7...).
- **Live state**, read through the supported plugin in my isolated console:
  - Typed reads (22:42:47 to 22:43:13):
    - all 16 records exist;
    - the 128 before-fields (16 x 8) equal live;
    - live versus live-baseline.json: 0 differences over 137 typed fields;
    - the 9 FR records have linkedUseCases [].
  - Full mappings (22:43:37): 233 rows. The 9 before rows equal live. No D64 identifier appears in any row.
  - Targets (22:44:26 to 22:44:51): all 14 distinct after-targets exist.
    - TR-DRV-EDGE-001, TR-IEC-EDGE-001, TEST-DRV-MOTOR-001 and TEST-IEC-TIMING-001 are closed with 0 AC. They are exactly four of the five records in the separate drive proposal (20261008T153521-0500-legacy-drive-ac-proposal), a disclosed dependency.
    - TR-DET-001 is completed with 2 AC.
    - The other nine are pending.
    - TR-MEDIA-D64-001 and TEST-D64IO-001 return McpNotFoundException.
  - Effective (22:45:05): layer-1; 235 FR, 139 TR, 142 TEST, 233 mappings. The 128 before-fields equal the effective projection.
  - TODOs (22:45:43): 39 open. PLAN-REQAUDIT-001, PLAN-GROKCOMPLETION-001, PLAN-VICEORACLE-001, PLAN-VSFLOCKSTEP-001 and PLAN-C64SET-001 are all done:false.

## Surface A: requested validation

- **A1 PASS. Candidate identity.** SHA256 A13429ED...EF9 at 193898 bytes equals input-pins.json, verification.json, the ledger's proposalSha256 and HANDOFF.md:9. Round-1 UNKNOWN resolved.
- **A2 PASS. Records.** 16 existing records, unique IDs, operation update: 9 FR, 2 TR, 5 TEST. All exist live.
- **A3 PASS. Original payloads unchanged.** All 14 original records are canonically equal, by full structural comparison (not sampling), to the round-1 candidate and to the 14-record draft 320887C5.
- **A4 PASS. Retained AC.** 66 retained: 56 rewritten, 10 unchanged (FR-TAP-001 ac-05, FR-TAP-002 ac-05, FR-CPU-004 ac-01/02/04/07, FR-PRF-003 ac-01/03/05/06).
- **A5 PASS. Added AC.** 38 added, 104 after, all unsatisfied. FR-BACKFILL-MEDIA-001 closed to pending is the only status change.
- **A6 PASS. Coverage rows.** 96 amendment rows plus 8 unchanged mandatory rows = 104. consolidated-coverage.json equals the candidate coverage (16/66/104/96/8/117/0).
- **A7 PASS. Named groups.** 117 distinct named groups, labeled as specifications, not tests.
- **A8 PASS. Mapping rows (RM-01 fixed).**
  - Six of the 9 rows change: FR-BACKFILL-MEDIA-001 +2, FR-TAP-001 +4, FR-TAP-002 +1, FR-TAP-003 +1, FR-TAP-004 +4, FR-TAP-005 +1. That is 13 additions and 0 removals, from 31 references before to 44 after.
  - Three rows are identical before and after: FR-RUNTIME-TAPE-002, FR-CPU-004, FR-PRF-003.
  - mappingChangeSummary matches my recount.
  - README.md:15, approvalPrerequisites[1], scopeDisclosure and HANDOFF.md:9 and :73 state it correctly.
- **A9 PASS. FR-AC to TEST-AC bindings.** All five hold:
  - FR-CPU-004 ac-03, ac-05 and ac-06 bind to TEST-CPU-001 ac-01, ac-02 and ac-03..ac-10;
  - FR-PRF-003 ac-02 and ac-04 bind to TEST-PRF-001 ac-01 and ac-02..ac-04.
  - Each TEST AC names its FR criterion, and both mapping rows link the TEST IDs.
- **A10 PASS. 178 read-only checks.** I re-ran Verify-Candidate.ps1 at 22:42:01: 178 checks, 0 failed, check names identical and in the same order as verification.json, same candidate SHA256.
- **A11 PASS. 8 corruption controls.** The correct fixture was accepted and all 8 expected rejections occurred (22:42:01, and again at 23:00:18). Adequacy is assessed below; the remaining gaps are RN-02 (info).
- **A12 PASS. Preimages.** The 112 projected preimage fields equal fresh live reads and the effective projection. Including the two TEST records, 128 of 128.
- **A13 PASS. TEST conditions intact.**
  - Before values of TEST-CPU-001 and TEST-PRF-001 equal live; after differs only in acceptanceCriteria.
  - Each of the 14 new AC equals the disclosed scope sentence plus the exact round-1 text.
  - Title, condition and metadata are unchanged.
- **A14 PASS. 32 R2 groups.** 117 groups minus 85 with supplement specifications leaves 32: 21 named groups plus 11 Turbo_* groups, as listed in R2-BACKLOG.md.
- **A15 PASS. Input-pin revalidation.** 294 of 294 current pins and 270 prior entries, as above. Round-1 UNKNOWN resolved.
- **A16 PASS. Parent recovery is faithful.** Round-1 UNKNOWN resolved.
  - Codex turn req-20261014T020333Z-check-hv-outcome holds seven HV_RECOVERED_A7BC4062..._PART_n_OF_7 chunks. Joined and LF-normalized, they equal the 96/93 verdict.md.
  - Turn req-20261014T021233Z-remediate-two-hv-failures holds seven "HV-R0-BLOCKED-ROUND1 n/7" chunks. Joined, they give 44189 characters equal to verdict.parent-recovered.md.
  - Round-1 response.jsonl has SHA256 7AF21F53D687E50ABA07FC72E037F5387A7186B07ED7054B52FE931DD8D36C3C, equal to terminal-result.json. It has 2648 events and one success result event, whose text equals verdict.parent-recovered.md after LF normalization.
- **A17 PASS. Prior verdict figures and README.md:11.**
  - 97/97: 39 PASS, 2 FAIL, 4 UNKNOWN, 1 N/A. 96/93: 39/2/0/1. 93/84: 35/6/0/1. All match their receipts.
  - terminal-result.json records exit code 0 and streams flushed and closed, as README.md:11 says.
- **A18 FAIL. Sibling-sweep claim for RM-05 and R0-06.**
  - The claims:
    - README.md:44 says the round "adds CIA/VIA anchors to every timer/IRQ sibling row".
    - HANDOFF.md:9 says "timer/IRQ source anchors are reconciled".
    - FINDINGS.md:29 disposes of R0-06 by adding "actual CIA FLAG/IRQ and VIA CA1 source anchors for timer/interrupt rows".
  - Observed: the group-scoped sweep defined at FINDINGS.md:11 is complete. All 8 rows carrying TimerInterrupt, PulseInterrupt, C64Flag or Vic20Ca1 groups have ciacore.c and viacore.c anchors.
  - Observed: four rows still lack them. Their criteria require interrupt or timer observations:
    - FR-TAP-005 ac-01: "native live scheduling/interrupt traces";
    - ac-03: "ordered native and managed tape/interrupt/timer observations";
    - ac-05: "timer and load outcomes";
    - ac-06: "pulse, timer and interrupt observations".
  - These rows carry only tap.c:389-456, datasette.c:599-692, datasette.c:714-851, c64datasette.c:36-49 and vic20datasette.c:37-49. That list is identical to the 14-record draft that R0-06 criticized ("All six FR-TAP-005 rows share one generic anchor list without CIA timer/FLAG or loader-specific anchors"), and neither remediation round changed it.
  - The claim of complete sibling coverage is therefore not accurate (RN-01).

## Surface B: workspace rules and process

- **B1 PASS. BDPv4 order.** Required everywhere it matters: README stage items 3, 5 and 6; R2-BACKLOG.md:3 and :7; every new TEST AC. Independent correct mocks come first, corrupt mocks must be rejected inside a green harness, then the same assertions bind to real providers. No red gate.
- **B2 PASS. Approval and pause boundary.** applicationAuthorized is false. R0 approval is not R1 authority. Work pauses after an accepted review (README.md:20).
- **B3 PASS. Read-only preparation.** It went through the supported plugin. Live state equals every before value, so no requirement or mapping mutation occurred.
- **B4 PASS. Tooling.** PowerShell scripts build JSON from native objects; no Python. I did not re-run PSScriptAnalyzer.
- **B5 PASS. Honest labeling.** Document lint is not called a mock gate, parent recovery is not called reviewer persistence, and historical gaps are preserved. The A18 overclaim is scored there.
- **B6 PASS. No native change** was claimed or made. The 25 immutable files used in the surface C audits equal the blobs of commit cc67418cba58e71d8e70fd353e9de47199ad2075 (git hash-object versus ls-tree, 22:59:25).
- **B7 PASS. Credentials.** The current marker key occurs 0 times in 22 scanned files at 22:59:37: the candidate directory, this review directory except the two locked capture streams, and HANDOFF.md. The parent's disclosed exposure stays open (RM-07).
- **B8 PASS. Dashes.** 0 em dashes and 0 en dashes in the same 22 files.
- **B9 PASS. Identity.** Real reviewer identity and local execution: claude-opus-5-5, effort max, local CLI, own console.
- **B10 PASS. This run executed as specified (RM-02 fixed).** Default permission mode; console isolation; trust verification; live reads; receipts in this directory; my own MCP turn. Completion and readback evidence are in verdict.persistence.json.
- **B11 PASS. Supported MCP routes only.**
  - Invoke-McpPlugin.ps1 Status, Invoke and CompleteTurn, always with -CacheRoot.
  - No raw REST and no storage-file access.
  - Drain disabled; no queue replay.

## Surface C: requirement adequacy against immutable VICE

- **C1 PASS.** All five previously unmapped CPU/SX-64 criteria are constrained by named TEST AC with corrupt-mock rejections.
- **C2 PASS.** The new TEST AC match immutable VICE. See verified facts below; the files were blob-verified this round.
- **C3 PASS (RM-04 fixed).** The broad TEST conditions and metadata are preserved, and each new TEST AC states its partial FR scope. Live mappings confirm TEST-CPU-001 is mapped from FR-CPU-001..005 and TEST-PRF-001 from FR-PRF-001..008.
- **C4 PASS.** The 14 original records are structurally identical to the payloads I audited against source in round 1, and those source files are unchanged.
- **C5 PASS.** Native VSF remains canonical and mandatory (README.md:24 and :50; TEST-TAP-001 ac-06/ac-07).
- **C6 PASS. D64 dependencies are separate and not linked.**
  - No D64 identifier appears in any of the 233 live mapping rows.
  - TR-MEDIA-D64-001 and TEST-D64IO-001 do not exist.
  - The 36..42-track scope and the true-drive proposal remain operator decisions (README.md:36).
- **C7 PASS.** UWP/Xbox implementation is excluded, but its requirements are retained (HANDOFF.md:22; plan lines 79 and 320), and the 15 legacy Xbox tests are retained.
- **C8 PASS.** No AC ID is removed, nothing is retired, and all 104 AC are unsatisfied.
- **C9 PASS.** Live effective and typed requirements, full mappings and TODOs are consistent with the candidate. Round-1 UNKNOWN resolved.
- **C10 N/A.** Executable AC-mapped tests are a later gate for an unapplied amendment.

## Surface D: plan holistically

- **D1 PASS. Locked decisions and objective.**
  - Respected: locked decision 2 (as a disclosed departure), 3, 6 and 10 to 15, plus the immutable-native amendment.
  - Locked decision 16's Grok/Claude alternation is superseded for this run by the operator's explicit "rerun HV with Opus 5.5 max" (see RN-08).
  - The whole objective is retained (README.md:50).
- **D2 PASS. One approval contract.** The candidate's approvalPrerequisites, the README stage contract and HANDOFF agree.
- **D3 PASS.** All 117 groups, including the 32 AC-text-only groups, are mandatory R2 designs.
- **D4 PASS. Locked-decision-2 departure disclosed** (README.md:30).
  - R1 stays read-only until the operator explicitly approves it.
  - A local journal is not claimed as server deduplication.
  - The old recovery key is not reused.
  - RM-09 detail: RN-06.
- **D5 PASS. Writer-window evidence is complete (README.md:32):**
  - an attestor;
  - scope of 16 records and 9 mapping rows;
  - writer inventory with acknowledgements;
  - measured start and expiry;
  - binding to the candidate hash and to the applying agent, session and request;
  - a check before each mutation;
  - a stop on expiry, revocation or a new writer;
  - G09/G14/G15 extensions.
- **D6 PASS.** No compare-and-swap, no cross-API atomic transaction, no blind retry and no automatic compensation (README.md:22, :30, :32).
- **D7 PASS.** Observer, loader, native, VSF and cumulative real gates all remain required (README.md:24, :40).
- **D8 PASS.** After an accepted HV the parent pauses for the operator's separate approval (README.md:20).
- **D9 PASS.** Historical Phase 3/4 start authority is retained and not extended to this delta (README.md:50).
- **D10 PASS.** HANDOFF is current for this candidate, apart from the A18 phrase at line 9.

## FAIL and UNKNOWN items

- **A18 FAIL.** "Every timer/IRQ sibling row" and "reconciled" are inaccurate. FR-TAP-005 ac-01, ac-03, ac-05 and ac-06 lack CIA/VIA anchors, although their criteria require interrupt/timer observations and R0-06 named them (RN-01).
- **UNKNOWN:** none.
- **N/A:** C10.

## R0 readiness verdict

NOT READY, for one low-severity reason. Fix RN-01 with either of the two edits in the ranked path, re-freeze, re-verify, and rerun this HV with the same launcher configuration. If that fix lands without regressions, I see no other blocker to AGREE.

## Negative-control adequacy (RM-08 attack)

Observed at 23:00:18 against in-memory copies of the real candidate. Nothing was written.
- **No false positive.** The unmodified candidate yields 0 linter issues.
- **Detected:**
  - changed count forced to 7 (mapping-count/changed);
  - one namedCaseGroups field made scalar (coverage-array/namedCaseGroups);
  - one FR-TAP-001 test edge dropped (mapping-count/removals);
  - baseline freshServerReadback set to false (stale-baseline).
- **Not detected by Candidate-Checks.ps1:**
  - the RM-04 scope sentence stripped from TEST-CPU-001 ac-01;
  - ciacore/viacore anchors removed from TEST-TAP-001/ac-03;
  - FR-TAP-005 ac-01 set to isSatisfied true.
  - The full 178-check verifier does reject the satisfied flag, through its "No satisfied criteria" check. Neither tool guards the RM-04 sentence or the anchors.
- **The built-in eight controls are adequate for the classes RM-08 asked about** (RM-01, RM-03, RM-06 and trace text):
  - they use an independent fixture;
  - each injects one targeted fault and names its expected rejection;
  - the correct fixture is accepted.
- They are document lint, correctly labeled as not a product mock gate. The uncovered classes are RN-02 (info).

## New findings

### RN-01 low [must fix before presentation; A18]: R0-06/RM-05 sibling sweep incomplete and overclaimed

- **What is missing.** FR-TAP-005 ac-01, ac-03, ac-05 and ac-06 require interrupt/timer observations in their criterion text. Their coverage rows still carry the 14-record draft's generic anchors (tap.c:389-456, datasette.c:599-692, datasette.c:714-851, c64datasette.c:36-49, vic20datasette.c:37-49), with no src/core/ciacore.c or src/core/viacore.c anchor.
- **History.**
  - R0-06 (96/93) named all six FR-TAP-005 rows.
  - Only ac-04 was fixed, in round 1.
  - ac-02 (named-loader success) legitimately defers loader-specific anchors to R2 (FINDINGS.md:29).
- **How the overclaim arose.**
  - The round-2 sweep keyed on group names: FINDINGS.md:11, and the Prepare-Candidate.ps1:144-146 regex TimerInterrupt|C64Flag|Vic20Ca1|PulseInterrupt.
  - The approval-facing README.md:44, HANDOFF.md:9 and FINDINGS.md:29 then describe the sweep as complete for every timer/IRQ row. SELF-REVIEW.md:18 repeats the claim.
- **Impact.** Low severity.
  - The requirement text is unaffected.
  - R2-BACKLOG.md:7 still requires source-derived expectations for these Turbo_* groups.
  - But the operator would read a false completeness claim about the coverage section of the hashed candidate.
- **Reviewer self-correction.** Two statements in my round-1 verdict were narrower than R0-06 itself and contributed to this miss:
  - the RM-05 fix wording ("any other row carrying a timer/IRQ group");
  - the R0-06 disposition ("partly resolved; sibling RM-05 remains").

### RN-02 info [optional regression protection]: linter blind spots

As observed above, Candidate-Checks.ps1 has no check for:
- the RM-04 partial-scope sentence;
- the CIA/VIA anchors;
- satisfaction flags.

RM-08 did not ask for these checks. Adding them would stop these classes regressing in future re-freezes.

### RN-03 info [tracking hygiene; N-18/R0-08 class]: owner TODO notes lag

- What lags (queried 22:45:43):
  - PLAN-REQAUDIT-001's latest note (2026-10-11T12:32) still points at the 14-record packet and says the latest accepted status is DISAGREE 93/84.
  - PLAN-GROKCOMPLETION-001's remaining text points at the 080155 draft.
- Why it is only informational:
  - the notes are dated and were true when written;
  - HANDOFF is current;
  - the brief forbids reviewer TODO edits.
- Suggestion: after the next HV, add a dated checkpoint note through the supported TODO workflow, with no done-state change.

### RN-04 info: stale referent

R2-BACKLOG.md:5 says "The last reviewer called these 32 names, including the ten Turbo_* groups". That reviewer was the 96/93 reviewer, and its count of ten was wrong (eleven is correct). Two reviews have happened since, so "the last reviewer" no longer identifies it.

### RN-05 info [environment disclosure; R0-09 class]: hook-created turns and model field

- The plugin prompt hook created session ClaudeCode-20261014T033325Z-plugin-session for this CLI session, with two turns:
  - req-20261014T033328Z-prompt-67cc, canceled by the hook as superseded;
  - req-20261014T033328Z-prompt-5bc0, in progress, used as my reviewer turn.
- These request IDs follow the plugin's format, not the canonical req-UTC-seq-slug format. I did not open a second turn, to avoid leaving an orphaned duplicate.
- The round-1 hook turn req-20261014T024134Z-prompt-0321 (session ClaudeCode-20261014T024130Z-plugin-session) remains in progress from the plan-mode run. Not touched.
- Invoke-McpPlugin.ps1:250 forces the child MCP_SESSION_MODEL to "claude", so the session's structured model field reads "claude". My actual model, claude-opus-5-5, is recorded in the turn tags and dialog.
- I replayed no queue and canceled nothing.

### RN-06 info [RM-09 detail]: recovery methods exist

Plugin Status lists workflow.requirements.planRecovery, applyRecovery and getRecovery. README.md:30 offers "an independently verified supported idempotent API contract for separate review" but does not name these methods. Naming them as the candidate route to evaluate (not to assume) would make the locked-decision-2 choice concrete. Optional.

### RN-07 info: pin manifest omits six cited native files

The CPU-port criteria cite six native files that are not in this round's input-pins.json: c64memsc.c, c64.h, c64pla.h, types.h, lib.c and maincpu.c. I verified them against commit blobs, together with the other 19 audit files. Optional: pin them in the next manifest.

### RN-08 info: reviewer-rotation decision not recorded

- Plan locked decision 16 requires alternating local Grok and local Claude reviewers, including for remediation reviews.
- The last four R0 reviews were all Claude Opus. This run follows the operator's explicit instruction "rerun HV with Opus 5.5 max".
- HANDOFF does not record that operator designation as superseding the alternation for these rounds.
- Optional: record it, so that a later reader does not see an unexplained departure.

## Prior finding dispositions

97/97 round-1 findings (12):
- **RM-01:** resolved (A8).
- **RM-02:** resolved (B10; Launch-Review.ps1:17-18).
- **RM-03:** resolved. Type census at 22:58:49:
  - namedCaseGroups: 96/96 arrays;
  - sourceAnchors: 91/91 arrays;
  - mapping trIds and testIds: 9/9 arrays on both sides;
  - testAcIds: 5/5 arrays;
  - the ledger is equal.
- **RM-04:** resolved (A13, C3).
- **RM-05:** resolved as scoped (8/8 rows carrying the named groups). Its sibling class is not complete (RN-01).
- **RM-06:** resolved.
  - baseline points at live-baseline.json: freshServerReadback true, captured 22:20:11, 16 typed records, 112 checks, 0 differences.
  - The 2026-10-09 block is kept as historicalOriginOnly.
  - No stale counts remain in the candidate's metadata (probe at 22:59:08).
- **RM-07:** open for operator disposition (FINDINGS.md:14). No key in scanned files.
- **RM-08:** resolved for its requested scope (A10, A11). Residual: RN-02.
- **RM-09:** open, optional (FINDINGS.md:15; RN-06).
- **RM-10:** carried as an R2 trap (FINDINGS.md:16).
- **RM-11:** preserved history (FINDINGS.md:17).
- **RM-12:** retained. The records use three approvalStatus strings (12, 3 and 1 records); the field is not sent to the server.

96/93 findings (11):
- **R0-01:** resolved; its residual RM-04 is now resolved too.
- **R0-02:** resolved (single stage contract, 32 groups in R2-BACKLOG, HANDOFF aligned).
- **R0-03:** resolved (README.md:30).
- **R0-04:** resolved (README.md:32).
- **R0-05:** addressed as a disclosed, separate dependency (README.md:36; live absence confirmed).
- **R0-06:** partly resolved. FR-TAP-005 ac-01, ac-03, ac-05 and ac-06 remain (RN-01).
- **R0-07:** resolved by labeling (README.md:37).
- **R0-08:** disclosed (README.md:38). Live labels unchanged at 22:45:43.
- **R0-09:** preserved; new analog RN-05.
- **R0-10:** preserved.
- **R0-11:** preserved as an R3 blocker (README.md:40).

93/84 findings (35): unchanged from round 1, except where live evidence now exists.
- **Resolved:**
  - N-01: resolved for R0. The added targets exist live; the D64 TR/TEST remain a separate dependency.
  - N-02: resolved and source-accurate; TEST traceability added.
  - N-03 and N-04: resolved for R0 (presence- and type-aware, value-bearing comparisons; the typed reads carry no synthesized timestamps).
  - N-05, N-06, N-11: resolved in text.
  - N-13 and N-14: resolved as disclosures.
  - N-18: resolved for R0; RN-03 info.
  - F-01 to F-07: resolved in text.
  - F-13: resolved for R0.
  - F-16: resolved; current pins rehashed 294/294.
- **Specified, or addressed in text, with execution at later gates:**
  - N-07, N-08, N-09, N-10: specified.
  - N-12: resolved in text; loader fixtures come later.
  - N-15: addressed by 63 direct and 47 adjacent test dispositions.
  - F-14: largely resolved in text.
- **Open or preserved:**
  - N-16, N-17: preserved.
  - F-08: R1 export tooling.
  - F-09, F-10: historical limitations.
  - F-11: R2/R3 binding.
  - F-12: R1 export headings.
  - F-15: open, plus RM-07.
  - F-17: informational.

## Verified native facts (commit cc67418cba58e71d8e70fd353e9de47199ad2075)

These come from my round-1 audit of the same files, which are now blob-verified as unchanged.

- **CPU port (c64pla.c:39-46, 77-96, 106-123).**
  - motor !((dir&data)&0x20); write (~dir|data)&0x8; sense-out (dir&data)&0x10.
  - Each is forwarded only when it changes, and never on BOARD_SX64, where the caches still update.
  - The caches start at 0xFF and survive reset.
  - C64 input bit 5 reads 0.
- **Capacitors (c64memsc.c:366-495; c64mem.c:366-495; c64.h:79-89).**
  - Deadline: maincpu_clk + base + lib_unsigned_rand(0, base/5), with base 350000 for bits 6/7 and 1500000 for SX-64 bits 3/4/5.
  - DDR stores draw for pins going from output to input, in order 3,4,5,6,7.
  - DATA stores draw for every output pin, in order 7,6,5,4,3, even when the value does not change.
  - The 8500 constant is unused.
- **$0001 read (c64memsc.c:262-340).**
  - Expiry uses strict <. The stored charge substitutes only input bits.
  - A $0000 read returns dir_read without expiry.
  - Reset clears charges and flags but not deadlines (c64pla.c:106-123).
  - CLOCK is uint64_t (types.h:61): UINT64_MAX-10 plus 350000 wraps to 349989.
  - lib_unsigned_rand is inclusive, over shared PCG state (lib.c:994-1045).
- **Datasette, TAP, VIA, CIA and tapeport** facts as listed in the round-1 verdict:
  - datasette.c:62-63, 391-409, 504-692, 722-853, 879-984, 1000-1021, 1041-1169, 1172-1322;
  - tap.c:121-197, 250-256, 389-457, 1673-1685;
  - vic20via1.c:185-203, 221-252; vic20via2.c:110-119;
  - viacore.c:423-424, 441-458;
  - c64datasette.c:36-50; vic20datasette.c:37-50;
  - ciacore.c:592-600, 1594-1625;
  - tapeport.c:245-322.
- **RM-10 azimuth quirk:** datasette.c:482-502 with lib.c:994-1003.

## Ranked path to the 98 threshold (exact minimal edits)

1. **RN-01 (blocks AGREE).** Choose one of two edits.
   - Preferred edit:
     - Add "src/core/ciacore.c:592-600,1594-1625" and "src/core/viacore.c:441-462" to the sourceAnchors of the FR-TAP-005 ac-01, ac-03, ac-05 and ac-06 coverage rows, keeping their five existing anchors.
     - Make the change in Prepare-Candidate.ps1, so that the candidate and consolidated-coverage.json regenerate identically. Extend the selection at lines 144-146 to these four row IDs, or to every row whose criterion text requires interrupt/timer observations.
     - Then make README.md:44, HANDOFF.md:9 and FINDINGS.md:11 and :29 list the swept rows explicitly: FR-RUNTIME-TAPE-002/ac-02, TR-TAP-EDGE-001/ac-02, TEST-TAPE-SENSE-001/ac-02, FR-TAP-003/ac-01, FR-TAP-003/ac-02, TEST-TAP-001/ac-01, TEST-TAP-001/ac-03, and FR-TAP-005 ac-01, ac-03, ac-04, ac-05 and ac-06.
   - Alternative, if anchors stay R2 work: leave the anchors unchanged and replace the three claims with the precise group-scoped statement plus this sentence: "FR-TAP-005 ac-01/03/05/06 require interrupt/timer observations; their CIA/VIA source anchors are an explicit R2 obligation of the corresponding Turbo_* groups."
   - Either way, then:
     - re-freeze;
     - re-run Verify-Candidate;
     - refresh the pins and the HANDOFF hash;
     - relaunch with the same default-mode launcher.
2. **RN-02 (recommended with step 1).** Add linter checks, each with one independent corrupt fixture:
   - the exact partial-scope sentence in all 14 TEST AC;
   - CIA/VIA anchors on every row in the step-1 list;
   - zero isSatisfied true.
3. **RN-04 (optional).** Change "The last reviewer" in R2-BACKLOG.md:5 to "The 96/93 reviewer".
4. **RN-06 (optional).** Name planRecovery, applyRecovery and getRecovery in README.md:30 as the route that needs separate verification.
5. **RN-03 and RN-08 (optional).**
   - After the next HV, add a dated checkpoint note on PLAN-REQAUDIT-001 through the supported TODO workflow, with no done-state change, if the operator permits.
   - Record the operator's reviewer designation in HANDOFF.

## Later mandatory gates

1. A fresh HV on the corrected candidate: AGREE at 98/98 or better, zero FAIL/UNKNOWN, full JSONL, and the whole verdict read back from the reviewer's own MCP turn. Then pause.
2. The operator's explicit approval of:
   - the exact 16-record, 9-row delta;
   - the locked-decision-2 departure;
   - the writer-window evidence.
3. R1:
   - G01-G18 guard tests green against independent mocks, with all rejection controls, before any coordinator code;
   - then real guards, application inside a verified writer window, full readback, exports, traceability and a requirement-gate HV.
4. R2: every one of the 117 groups gets independent fixtures, expected observations and negative controls, and the full mock suite is green.
5. R3: the same assertions bound to managed and immutable-native providers, with:
   - observer completeness and non-interference;
   - lawful loader assets;
   - native VSF;
   - full cumulative suites at zero failed, skipped or aborted.
6. Separate approvals for the D64 TR/TEST, the 36..42-track scope and the five-record true-drive proposal.
7. Whole-plan exit:
   - acceptance of the recovered requirements;
   - dense session-log proof;
   - immutable xvic/x64sc parity;
   - canonical native VSF;
   - settings visibility;
   - the final handoff and the commit order.

## Persistence

1. verdict.md and verdict.json are written before MCP persistence.
2. The whole verdict.md and the complete verdict.json are then saved, in ordered dialog chunks of at most 6500 characters, to my turn req-20261014T033328Z-prompt-5bc0.
3. The turn is completed, then queried back and compared with these files.
4. The outcome, hashes and chunk counts go in verdict.persistence.json and my final response.

If primary persistence genuinely fails, I would use the authorized full-content failsafe and label it queued, not persisted.

## Receipt paths

- F:\GitHub\vice-sharp\docs\receipts\hv\20261013T222855-r0-round2-opus\verdict.md
- F:\GitHub\vice-sharp\docs\receipts\hv\20261013T222855-r0-round2-opus\verdict.json
- F:\GitHub\vice-sharp\docs\receipts\hv\20261013T222855-r0-round2-opus\verdict.persistence.json
- Parent-owned files in the same directory: request.jsonl, response.jsonl, stderr.log, input-pins.json, launch-result.json, prompt.md, SELF-REVIEW.md, Launch-Review.ps1, prior-input-revalidation.json.

## Scope limitations and reviewer conduct

- **Scope.**
  - Source inspection and stored-state comparison only. No build, mock, managed or native execution.
  - The native source facts rely on my round-1 audit of the same blob-verified files. This round I re-audited the anchors and criteria the remediation touched, not every line again.
  - Large JSON files and live responses were parsed programmatically. I examined in detail the 16 records, 9 rows, 14 targets, coverage field types and anchors.
  - The credential scan covered only the current key. The two locked capture streams were not read.
- **Conduct.**
  - This CLI session was resumed after context compaction. Facts measured before compaction are cited from my own console output in this session. I re-measured the key ones afterwards: pins, hashes, mutation probes, sweeps, type census, native blobs.
  - Progress entries that carried a time used a measured console time; short status lines carried no time. No time was estimated.

## Scores

- **Accuracy 98.**
  - Correct: every count in the brief, and every hash, binding, mapping delta, live comparison and source-derived statement I checked. All four prior UNKNOWNs resolve in the candidate's favor.
  - Deductions:
    - the remediation's claim that every timer/IRQ sibling row now has CIA/VIA anchors is inaccurate for four rows (README.md:44, HANDOFF.md:9, FINDINGS.md:29);
    - a minor stale referent at R2-BACKLOG.md:5.
- **Completeness 98.**
  - Covered: all 12 RM findings, all 11 R0 findings and the 35 earlier findings are dispositioned, and the sibling classes of RM-03, RM-04 and RM-06 sweep clean.
  - Deductions:
    - the R0-06 remainder (four FR-TAP-005 rows) is still unswept after two rounds, though R2-BACKLOG.md:7 mitigates it;
    - the owner TODO notes lag the candidate (info).

Both scores reach 98, but A18 FAILs, so AGREE is not possible.

Structured twin: verdict.json in this directory.
