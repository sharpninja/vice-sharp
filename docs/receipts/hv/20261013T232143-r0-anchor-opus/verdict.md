# Hostile validation verdict: R0 anchor remediation round 3 candidate (16 records, 9 mapping rows)

Reviewer: ClaudeCode, model claude-opus-5-5 (Claude Opus 5.5), effort max. Local Claude Code CLI session bab22fd4-f213-4011-9616-074b14def01b (resumed; launch-result.json records the round-3 launch), on PAYTON-LEGION2 in F:\GitHub\vice-sharp. Own PowerShell.MCP sub-agent console, agent_id sa-3a02def0 (window #9944). The round-2 agent_id sa-76fe112f was rejected after the CLI resume, so I allocated a new console with is_subagent=true. The parent console was not touched. No nested reviewer, substitute model, cloud task, web tool, turn cap or time cap.

Parent: Codex, Codex-20261007T135626Z-plugin-session / req-20261014T021233Z-remediate-two-hv-failures. Operator scope, as quoted by the parent: "Resume. Remediate both HV failures, rerun HV with Opus 5.5 max. Iterate to HV agree then pause."

Reviewer MCP session and turn: ClaudeCode-20261014T042517Z-plugin-session / req-20261014T042520Z-prompt-6807. The plugin prompt hook created this turn for this run (see RP-06).

Times are measured local times (UTC-05:00) from my console. This verdict was composed from evidence measured through 2026-10-13T23:39:51. File-write and persistence times are in verdict.persistence.json.

OverallVerdict: AGREE. 50 assertions: PASS 49, FAIL 0, UNKNOWN 0, N/A 1. Accuracy 99. Completeness 98.

R0 readiness: READY to present the exact delta for the operator's explicit approval. This holds only once this verdict's primary MCP readback succeeds; verdict.persistence.json records that. After that, the parent pauses. Nothing may be applied before the operator's separate approval.

Candidate: docs\receipts\requirements-recovery\20261013T231703-r0-anchor-remediation\proposed-field-delta.json, SHA256 63386E9251185B101F53E9257CDA9270025A68006B83F243314E6E299A55D702, 194242 bytes. I recomputed the hash at 23:30:25, 23:31:25 and 23:38:17.

Summary:
- The sole blocking finding from round 2, RN-01, is fixed.
  - FR-TAP-005 ac-01, ac-03, ac-05 and ac-06 now carry src/core/ciacore.c:592-600,1594-1625 and src/core/viacore.c:441-462, and keep all five earlier anchors.
  - The documents now claim an explicit 12-row set, not an unbounded "every row".
  - Two independent content sweeps find no remaining interrupt or timer row without both anchors. One sweep uses interrupt, FLAG, CA1, IRQ and timer terms; the other uses CIA and VIA names.
  - Both anchor ranges match the immutable source.
- Nothing else changed in the candidate.
  - All 16 record payloads and all 9 mapping rows are canonically identical to round 2.
  - The only other differences are the baseline capture metadata and createdAtLocal.
  - A read-only re-run of the candidate's own generator reproduces the candidate exactly, apart from timestamps.
- Every count in the brief recomputes exactly.
- Live state matches every before value.
- 324 of 324 input pins match.
- All RN-02 to RN-08 dispositions are verified. RN-05's triage report exists in the routed McpServer store.
- Remaining items are information only (RP-01 to RP-07).
  - Two are my own reviewer corrections: round-2 RN-08 was partly inaccurate, and the viacore anchor range is slightly over-inclusive.
  - The rest are optional hardening and dated tracking notes. None affects the exact delta the operator would approve.

This verdict applies nothing. It authorizes no change to the done state of any TODO, requirement, mapping, phase, plan or goal. The full original goal remains incomplete.

## Classification and boundaries

Class 1, project requirement work, requirements phase (R0).

The question: is the exact UNAPPLIED 16-record, 9-row amendment, with its ordered implementation plan, accurate and complete enough to present for explicit operator approval?
- Surfaces A, B, C and D apply.
- Later gates: executable AC-mapped tests, the R1 coordinator, the native observer and emulator work (C10 N/A).
- I did not require finished product code before reviewing requirement text.

This was a read-only review.
- My only writes:
  - verdict.md, verdict.json and verdict.persistence.json in this review directory;
  - my own MCP session turn (title, tags, dialog, actions, completion);
  - process-scoped environment variables in my own console;
  - local plugin cache state under my isolated review cache, including a read-only query subfolder (RP-06);
  - one supported incidental triage report (RP-06);
  - PowerShell.MCP's own Temp output spill files.
- I changed no requirement, mapping, TODO, plan, handoff, product, test, native or canonical document.
- I ran no build, test or Python, used no web access, and did no queue drain, replay, settings change, plugin repair or turn cancellation.
- Native source was read only from artifacts\immutable-vice\cc67418cba58e71d8e70fd353e9de47199ad2075\source\vice. I did not touch native\vice.

## Mandatory first step, trust and environment

- **add-profile ran first this round (2026-10-13T23:26:37).**
  - C:\Users\kingd\.claude\skills\add-profile\SKILL.md read in full: 4574 bytes, SHA256 9B8AB476410BB2E778A97BA545C94D4A4591A1E40B7E6352777C40C95229A86E.
  - The profile directory holds one non-skill file, PROFILE-MERGED.md, read in full: 62592 bytes, SHA256 A6AD7EAC0CB9CFFE9B9687E02A936484A3ED34FC49CF0CDD2C5FD6C869112AF3.
  - Profile file count: 1.
- **Governance files read in full this round.** All three are unchanged.
  - AGENTS.md, SHA256 5649ACD0870C000119185DF3C911EEF009407AA09C43C5A4C68182F1CB26D727.
  - CLAUDE.md, SHA256 1595F5E7FE057EFA6F71A1A136A8E311FB453B1E034929AA3E810CD628CF07B3.
  - C:\Users\kingd\.grok\skills\hostile-validator\SKILL.md, SHA256 F600F16720FC559060DE65B2A6AEF6947BC57A1FF6D4B447704B240CFD1F4BBD.
  - Current operator rules override the skill's red-first, UTC-receipt and GrokSubagentHostile wording.
- **Marker** F:\GitHub\vice-sharp\AGENTS-README-FIRST.yaml.
  - SHA256 8ADA0C1316EB8499C352FEA7BF62490BE9F6F9B12264D5168D9DF1DA75AC071D, 34217 bytes, last write 2026-10-13T18:24:56.
  - Read in full after in-console exact-value redaction of the API key: 3 occurrences, on lines 3, 343 and 344, including the query-string example.
  - The only other 32-plus alphanumeric runs are three hex digests (lines 63, 76 and 397).
  - Server 1.4.40+bcae36fdce7ce31e792db9e06a17bff51019bdf9, PID 6036.
  - The marker's plugin contract says 1.118.0, but the installed Claude plugin is 1.119.0. Disclosed, not repaired.
- **Trust.**
  - At 23:27:49: Invoke-FullBootstrap True; my independent nonce hv-r0r3-4b5da1265e27476f was echoed by /health; Healthy; same server version.
  - At 23:28:04: Test-MarkerSignature True.
- **Console isolation (23:27:06), before any plugin operation.**
  - Removed six inherited names: GROK_API_KEY, GROK_BASE_URL, GROK_MEMORY, MCP_AGENT_API_KEY, MCP_AGENT_MODEL_NAME, MCP_AGENT_MODEL_URL. Values were never printed.
  - Applied all 17 request.jsonl values (0 mismatches), including:
    - MCP_CACHE_DIR_OVERRIDE = F:\GitHub\vice-sharp\.mcpServer\claude\reviews\20261013T232143-r0-anchor-opus;
    - MCP_FAILSAFE_DRAIN_DISABLED = 1;
    - MCP_SESSION_MODEL = claude-opus-5-5.
  - Every wrapper call passed -CacheRoot.
- **Plugin Status.** available; agent ClaudeCode; isolated cacheDir; hasSession and hasCurrentTurn true; pending 0; failsafe 0; quarantine 0.
- **CLAUDE.md startup steps.** I performed steps 1, 2, 8 and 9 read-only. I did not perform steps 3 to 7 (cache deletion and hook refresh), because the brief forbids settings changes, plugin repair and environment repair.
- **Request and prompt.**
  - prompt.md (SHA256 BB624906C3B867B1DC4318B6DE1FA56E1931461A2ECD2B3442F661EDD56A7CE8) is byte-equal to the request.jsonl prompt.
  - request.jsonl uses --permission-mode default with an explicit allow-list. Launch-Review.ps1 refuses plan mode and now requires 10 controls.

## Inputs, pins and live state

- **Input pins.**
  - input-pins.json: 324 of 324 equal at 23:28:29 and again at 23:39:51, including 31 immutable native files.
  - prior-input-revalidation.json: 294 entries, exactly the round-2 pin set.
    - 293 are equal. 1 changed intentionally: HANDOFF.md, 59B479B6 to AFB26B6B.
    - All 294 "after" values equal the current files.
  - 30 new pins: six native files (RN-07), seven round-2 receipts, three round-3 launcher files and fourteen candidate files.
- **Candidate directory, read in full.**
  - README.md, FINDINGS.md, R2-BACKLOG.md, TEST-TRACEABILITY.md;
  - Prepare-Candidate.ps1, Candidate-Checks.ps1, Verify-Candidate.ps1;
  - Launch-Review.ps1 and SELF-REVIEW.md.
  - The JSON files were parsed completely and compared structurally against the round-2 candidate, which I had reviewed in depth. Every difference is listed below.
  - Text line diffs against round 2:
    - README.md: lines 1, 11 and 44;
    - FINDINGS.md: the new RN block plus the RM-05 and R0-06 dispositions;
    - R2-BACKLOG.md: line 5;
    - Candidate-Checks.ps1: the 12-identity anchor check, the fixture identity and two controls;
    - Prepare-Candidate.ps1: lines 144-146;
    - HANDOFF.md: lines 3, 7, 9, 26 and 71.
    - TEST-TRACEABILITY.md and Verify-Candidate.ps1 are unchanged.
- **Plan and HANDOFF.**
  - The plan and its reference copy are byte-identical (436BB2B1...6473, 48261 bytes). Read in full.
  - HANDOFF.md (AFB26B6B...) read in full.
- **Supplements and verdicts read in full this round.**
  - Predecessor packet: README, FINDING-DISPOSITIONS.md, CAPACITOR-SOURCE-AND-CASES.md, R0-R1-GUARD-CONTRACT.md, N07-N12-SOURCE-AUDIT.md.
  - Media README, D64-CONTRACT.md, MEDIA-COVERAGE.md.
  - Motor README, MOTOR-CONTRACT.md.
  - Recording README, RECORDING-CONTRACT.md, WIRING-LIFECYCLE-CONTRACT.md.
  - The direct and adjacent test-disposition READMEs, the API guard README and the observation README.
  - The 96/93 and 93/84 verdicts.
  - The round-2 verdict.md, verdict.json, verdict.persistence.json and terminal-result.json.
- **Live state**, read through the supported plugin:
  - Typed reads (23:32:09 to 23:32:48):
    - 16 of 16 records;
    - 128 of 128 before-fields equal live;
    - live versus live-baseline.json: 137 typed fields, 0 differences;
    - the baseline's 112 projected comparisons are all equal;
    - the 9 FR records have empty linkedUseCases.
  - Mappings (23:32:57): 233 rows. The 9 before rows equal live as sets. No D64 identifier in any row. 14 distinct after-targets. 31 references before, 44 after.
  - Targets (23:33:08 to 23:33:29): all 14 exist.
    - TR-DRV-EDGE-001, TR-IEC-EDGE-001, TEST-DRV-MOTOR-001 and TEST-IEC-TIMING-001 are closed with 0 AC; these belong to the separate drive proposal.
    - TR-DET-001 is completed.
    - The other nine are pending.
    - TR-MEDIA-D64-001 and TEST-D64IO-001 return McpNotFoundException.
    - FR-ARCH-TRUEDRIVE-1541-002 is closed with 0 AC.
  - Effective (23:33:41): layer-1; 235 FR, 139 TR, 142 TEST, 233 mappings.
    - The 128 before-fields equal the effective projection, with body or condition in place of description.
    - Notes and scopeEndLayerKey are null for all 16 records, and AC items carry only id, isSatisfied and text.
  - TODOs (23:34:09): 39 open. PLAN-GROKCOMPLETION-001, PLAN-REQAUDIT-001, PLAN-VICEORACLE-001, PLAN-VSFLOCKSTEP-001 and PLAN-C64SET-001 are all done:false.

## Surface A: requested validation

- **A1 PASS. Candidate identity.** SHA256 63386E92...D702 at 194242 bytes equals input-pins.json, verification.json, the ledger's proposalSha256 and HANDOFF.md:9 and :71.
- **A2 PASS. Records.** 16 existing records, unique IDs, operation update: 9 FR, 2 TR, 5 TEST. All exist live.
- **A3 PASS. Original payloads unchanged.**
  - All 16 records are canonically equal to the round-2 candidate (23:30:10).
  - Verify-Candidate's 14 "Original payload preserved" checks against the 320887C5 draft pass (23:31:25).
- **A4 PASS. Retained AC.** 66 retained, recomputed independently at 23:36:51: 56 rewritten and 10 unchanged.
- **A5 PASS. Added AC.** 38 added, 104 after, 0 satisfied. FR-BACKFILL-MEDIA-001 closed to pending is the only status change.
- **A6 PASS. Coverage rows.** 96 amendment rows plus 8 unchanged mandatory rows = 104, in the same order as round 2. consolidated-coverage.json equals the candidate coverage.
- **A7 PASS. Named groups.** 117 distinct groups: 85 with supplement specifications and 32 AC-text-only. They are not tests.
- **A8 PASS. Mapping rows.**
  - Six of the 9 rows change: FR-BACKFILL-MEDIA-001 +2, FR-TAP-001 +4, FR-TAP-002 +1, FR-TAP-003 +1, FR-TAP-004 +4, FR-TAP-005 +1. That is 13 additions and 0 removals.
  - Three rows are unchanged: FR-RUNTIME-TAPE-002, FR-CPU-004, FR-PRF-003.
  - mappingChangeSummary and every approval-facing statement agree.
- **A9 PASS. FR-AC to TEST-AC bindings.** All five hold by identity:
  - testCriterionTraceability;
  - the "For FR-x/ac-y" text in each of the 14 TEST AC;
  - the live mapping rows that link TEST-CPU-001 and TEST-PRF-001.
- **A10 PASS. 178 read-only checks.** My re-run of Verify-Candidate.ps1 at 23:31:25 gives 178 checks and 0 failed. Names, order and evidence are identical to verification.json, and no file changed.
- **A11 PASS. 10 independent controls.**
  - The correct fixture is accepted and all 10 expected rejections occur.
  - My own probes on in-memory copies of the real candidate (23:31:45): removing either anchor from any of the 12 rows is detected 24 of 24 times. A truncated CIA range, a wrong unchanged count and scalar anchors are also detected.
  - Adequacy is assessed below.
- **A12 PASS. Preimages.** 112 projected preimages are equal. Live: 128/128 typed and 128/128 effective, and 137 typed fields with 0 differences.
- **A13 PASS. TEST conditions intact.** TEST-CPU-001 (0 to 10 AC) and TEST-PRF-001 (0 to 4 AC):
  - only acceptanceCriteria changes;
  - every other field is equal;
  - all 14 new AC carry the partial-scope sentence.
- **A14 PASS. 32 R2 groups.** 21 named plus 11 Turbo_* groups. All resolve to coverage groups.
- **A15 PASS. Input pins.** 324/324 current pins, and 294 prior entries revalidated as above.
- **A16 PASS. RN-01 fix.**
  - The four FR-TAP-005 rows gained exactly the CIA and VIA anchors, with all five earlier anchors retained.
  - The 12-row set is complete: each row has exactly one coverage row carrying both anchors.
  - The content sweeps find no unanchored row with an interrupt or timer obligation.
    - The remaining term hits are false positives: motor-running flag, falloff flag, "half-second timer".
    - The CIA/VIA-name hits are wiring and hidden-state rows, correctly anchored to vic20via1.c, vic20via2.c and viacore.c:717-724 or 378-424.
  - README.md:44, FINDINGS.md:5 and HANDOFF.md:9 now bound the claim to these 12 rows.
- **A17 PASS. Round-2 receipts.**
  - My round-2 verdict.md, verdict.json and verdict.persistence.json are unchanged.
  - terminal-result.json: exit 0, streams flushed and closed.
  - response.jsonl: SHA256 18D882CEE7E25023E03A039AF8D8CCBA5A976ECA420A453AF8E34E66C2B895F5, 2191 lines, all valid JSON, exactly one success result event.
  - Round-2 turn req-20261014T033328Z-prompt-5bc0 is completed, with 15 dialog items and 7 actions. Its 7+3 chunks were proven equal at the 23:13:02 readback, and completed turns are immutable.
- **A18 PASS. Generator reproduces the candidate.**
  - Prepare-Candidate.ps1, re-run read-only with live reads at 23:37:46 to 23:38:17, returns a Delta equal to the candidate on every key except createdAtLocal and baseline timing.
  - Its ledger output equals consolidated-coverage.json except preparedAtLocal and the proposalSha256 placeholder.
  - The generator throws on preimage drift, so this also re-proves live equality.
- **A19 PASS. RN-02 to RN-08 dispositions.**
  - RN-02: 12-identity anchor lint plus two independent controls, with no claim of partial-scope-sentence lint.
  - RN-03: dated TODO notes unchanged and not done.
  - RN-04: R2-BACKLOG.md:5 reads "The 96/93 reviewer".
  - RN-05: triage-report-a13e89ad99ab44debb30978ac991583d exists in the routed F:\GitHub\McpServer store, status grouped, group triage-group-2ab0858711b73b32 (23:35:05).
  - RN-06: optional; no invented capability.
  - RN-07: all six native files pinned.
  - RN-08: HANDOFF.md:26.

## Surface B: workspace rules and process

- **B1 PASS. BDPv4 order.** Required throughout: README stage items 3, 5 and 6; R2-BACKLOG.md:3 and :7; every TEST AC preamble. Independent mocks are green first, negative controls are rejected inside a green harness, then identical real assertions run. No red gate.
- **B2 PASS. Approval and pause boundary.** applicationAuthorized is false. R0 approval is not R1 authority. The parent pauses after an accepted HV (README.md:20).
- **B3 PASS. Read-only preparation.** Live state equals every before value, and the generator re-run confirms it.
- **B4 PASS. Tooling.**
  - PowerShell with native-object JSON; no Python.
  - PSScriptAnalyzer 1.25.0 at Warning and Error: 0 findings and 0 parse errors in Prepare-Candidate.ps1, Candidate-Checks.ps1, Verify-Candidate.ps1 and Launch-Review.ps1 (23:31:12).
- **B5 PASS. Honest labeling.**
  - Document lint is not called a mock gate.
  - Parent recovery is not called reviewer persistence.
  - The anchor claim is now bounded.
  - Historical gaps are preserved.
- **B6 PASS. Native files.** No native change. The 31 pinned immutable files are unchanged, and the anchor ranges were re-read from the immutable source.
- **B7 PASS. Credentials.** 0 occurrences of either workspace key in 23 scanned files at 23:36:35: the candidate directory, this review directory except the locked capture streams, and HANDOFF.md. The parent's disclosed exposure stays open for the operator (RM-07).
- **B8 PASS. Dashes.** 0 em dashes and 0 en dashes in the same 23 files.
- **B9 PASS. Identity.** Real reviewer identity, local execution, own console.
- **B10 PASS. This run executed as specified.** Default permission mode; console isolation; trust checks; live reads; receipts here; my own MCP turn. Completion and primary readback are recorded in verdict.persistence.json. Without that readback this verdict is not an accepted gate.
- **B11 PASS. Supported MCP routes only.**
  - Invoke-McpPlugin.ps1 with -CacheRoot.
  - The /health nonce call is the marker's own trust step.
  - The RN-05 triage check used the plugin's read-only workflow.triage.getReport against the McpServer workspace marker, with that key redacted.
  - No raw REST data calls, no storage-file access, drain disabled, no replay.

## Surface C: requirement adequacy against immutable VICE

- **C1 PASS.** The five formerly unmapped CPU/SX-64 criteria are constrained by named TEST AC, with corrupt-mock rejections, linked by identity.
- **C2 PASS.** The TEST AC remain source-accurate. They are unchanged since my round-1 audit, and their source files are pinned and unchanged.
- **C3 PASS.** The broad TEST conditions and metadata are preserved, and each new TEST AC states its partial FR scope.
- **C4 PASS.** The 14 original records are unchanged from the source-audited payloads.
- **C5 PASS.** Native VSF remains canonical and mandatory. Existing managed VSF support is acknowledged, not called absent.
- **C6 PASS. D64 dependencies.**
  - They are separate and not linked: no D64 identifier in any of 233 live rows.
  - TR-MEDIA-D64-001 and TEST-D64IO-001 do not exist.
  - The 36..42-track scope and the true-drive proposal remain operator decisions (README.md:36).
- **C7 PASS.** UWP/Xbox implementation is excluded and its historical requirements and tests are retained (HANDOFF.md:22; plan lines 79 and 320).
- **C8 PASS.** No AC is removed, nothing is retired, and all 104 AC are unsatisfied.
- **C9 PASS.** Live effective and typed requirements, full mappings and TODOs are consistent with the candidate.
- **C10 N/A.** Executable AC-mapped tests are a later gate.

## Surface D: plan holistically

- **D1 PASS. Locked decisions.**
  - Respected: 2 (as a disclosed departure), 3, 6 and 10 to 15, plus the immutable-native amendment.
  - Decision 16's alternation is superseded by plan.md:3, the operator's 2026-10-08 temporary Opus override, and HANDOFF.md:26 records the designation. See RP-01.
- **D2 PASS.** One approval contract: candidate approvalPrerequisites, README stage contract and HANDOFF agree.
- **D3 PASS.** All 117 groups, including the 32 AC-text-only groups, are mandatory R2 designs.
- **D4 PASS. Locked-decision-2 departure disclosed** (README.md:30).
  - R1 stays read-only until the operator explicitly accepts it.
  - A local journal is not claimed as server deduplication.
  - The old recovery key is not reused.
- **D5 PASS. Writer-window contract complete (README.md:32):**
  - a named attestor;
  - scope of 16 records and 9 mapping-row FR IDs;
  - writer inventory with acknowledgements;
  - measured start and expiry;
  - binding to the candidate hash and the applying session and request;
  - a check before each mutation;
  - a stop on expiry, revocation or a new writer;
  - G09/G14/G15 extensions.
- **D6 PASS.** No compare-and-swap, cross-API atomicity, blind retry or automatic compensation (README.md:22, :30, :32).
- **D7 PASS.** Observer, loader, native, VSF and cumulative real gates are retained (README.md:24, :40).
- **D8 PASS.** After an accepted HV the parent pauses (README.md:20; HANDOFF.md:7).
- **D9 PASS.** Historical Phase 3/4 authority is retained and not extended (README.md:50).
- **D10 PASS.** HANDOFF is current for this candidate.

## FAIL and UNKNOWN items

- **FAIL:** none.
- **UNKNOWN:** none.
- **N/A:** C10.

## R0 readiness verdict

READY to present for exact-delta approval, once this verdict's MCP readback succeeds. The parent must then pause.

The operator's approval must name:
- the exact candidate hash 63386E9251185B101F53E9257CDA9270025A68006B83F243314E6E299A55D702;
- the 16 records;
- the 9 mapping rows: six changed, three unchanged, 13 additions, 0 removals;
- the locked-decision-2 departure;
- the writer-window evidence prerequisites.

This approval does not authorize R2/R3 work or any done-state change.

## Negative-control adequacy

- **The built-in ten controls (Candidate-Checks.ps1 Test-CandidateNegativeControl) are adequate for their stated scope:**
  - they use an independent fixture, not candidate data;
  - each injects one targeted fault and asserts a named expected rejection;
  - the correct fixture is accepted;
  - they cover counts, count types, two array fields, trace text, trace criterion, stale baseline, missing mapping row, and CIA and VIA anchors separately.
- **My probes on the real candidate confirm the new check is exact:**
  - all 24 single-anchor removals across the 12 rows are rejected with the precise identity;
  - a truncated CIA range is rejected.
- **Not detected by the linter:**
  - a stripped partial-scope sentence (explicitly not claimed; RP-03);
  - a satisfied flag;
  - a dropped coverage row.
  - The last two are caught by Verify-Candidate: "No satisfied criteria" (line 41) and the 104-row "Coverage denominator" (line 80).
- These are document checks, correctly labeled as not a product mock gate.

## New findings (all information only, none blocking)

### RP-01 info [reviewer self-correction]: round-2 RN-08 was partly inaccurate

- My round-2 RN-08 said HANDOFF did not record the operator's Opus designation over the plan's alternation. That missed two places where it was already recorded:
  - plan.md:3 already records the 2026-10-08 temporary reviewer override, which supersedes the alternating and Grok wording;
  - the round-2 HANDOFF.md:26 already said "Grok is unavailable. Use locally launched claude-opus-5-5, effort max".
- My round-2 D1 wording also undersold the plan's own override.
- The parent's strengthened HANDOFF.md:26 is harmless.

### RP-02 info [optional]: viacore anchor range is slightly over-inclusive

viacore.c:441-458 is the CA1 case of viacore_signal. Lines 459-462 start the CA2 case. The anchor 441-462 still covers CA1 fully. It was my own recommended range; tightening it to 441-458 is optional.

### RP-03 info [optional; RN-02 residual]: no partial-scope sentence lint

Candidate-Checks.ps1 does not lint the partial-scope sentence of the 14 TEST AC. This is explicitly not claimed. Adding it would protect future re-freezes.

### RP-04 info [tracking hygiene; RN-03]: owner TODO notes are dated

- PLAN-REQAUDIT-001's latest note (2026-10-11T12:32) still says the latest accepted status is DISAGREE 93/84.
- 9 of the 10 PLAN-GROKCOMPLETION-001 task labels still use red/green wording.
- These are dated history; HANDOFF is current; no done-state changed.
- Suggestion: after this HV is accepted and read back, add a dated checkpoint note through the supported TODO workflow, if the operator permits.

### RP-05 info [optional; RN-06/RM-09]: recovery methods still unnamed

README.md:30 still does not name workflow.requirements.planRecovery, applyRecovery or getRecovery as the route that needs separate verification. Nothing is assumed.

### RP-06 info [environment disclosure; RN-05/R0-09 class]

- **Hook-created turn.** The prompt hook created session ClaudeCode-20261014T042517Z-plugin-session with turn req-20261014T042520Z-prompt-6807, which I use as my reviewer turn. Its request ID is in plugin format.
- **Session-id rotation.** The cached session-state rotated to ClaudeCode-20261014T042520Z-plugin-session at 23:25:22. That session is local only; it was not on the server at 23:28:18. Writes target the turn's own session.
- **Model field.** The server showed the structured model field as "claude" at 23:28:18. The final value is recorded in verdict.persistence.json.
- **Read-only McpServer query.** It created only a local session-state.yaml (ClaudeCode-20261014T043504Z-plugin-session) in my isolated cache subfolder. No server session was created (query at 23:35:39).
- **Untouched history.** The round-2 setTurnTitle failsafe remains in the round-2 isolated pending directory, already triaged.
- **Round-3 recurrence.** setTurnTitle at 23:42:57 again reported "rejected" because the retitled flag was absent, and it wrote failsafe 20261014T044253Z-session_setTurnTitle-1eb8.yaml to my isolated pending directory. The server shows the title applied (23:43:23). I filed triage-report-6f1c801607fe4fff88e480940c809129 (group triage-group-3ca7b6bf43804600, status collecting) at 23:43:41, as standing policy and this brief permit. The failsafe is not replayed.
- **No changes by me.** I replayed and canceled nothing.

### RP-07 info: RN-05 triage status

triage-report-a13e89ad99ab44debb30978ac991583d is in status "grouped" (intake), not resolved. The brief's "accepted collecting" describes its submission state.

## Prior finding dispositions

98/98 round-2 findings (8):
- **RN-01:** resolved (A16).
- **RN-02:** resolved for its stated scope (A11, A19); residual RP-03.
- **RN-03:** disclosed and unchanged (RP-04).
- **RN-04:** resolved.
- **RN-05:** resolved (A19; RP-06 and RP-07).
- **RN-06:** open, optional (RP-05).
- **RN-07:** resolved.
- **RN-08:** resolved; my original finding was partly inaccurate (RP-01).

97/97 round-1 findings (12):
- **RM-01, RM-02, RM-03, RM-04, RM-06, RM-08:** resolved and still holding. Type census: all group and anchor fields are arrays.
- **RM-05:** now fully resolved through RN-01.
- **RM-07:** open for operator disposition.
- **RM-09:** optional.
- **RM-10:** carried as an R2 trap.
- **RM-11:** preserved history.
- **RM-12:** retained.

96/93 findings (11):
- **R0-01 to R0-05, R0-07:** resolved.
- **R0-06:** now resolved (12-row set).
- **R0-08:** disclosed.
- **R0-09, R0-10:** preserved.
- **R0-11:** preserved as an R3 blocker.

93/84 findings (35):
- **Resolved in text or for R0:** N-01 to N-06, N-11, N-13, N-14, N-18, F-01 to F-07, F-13, F-14, F-16.
- **Specified for later gates:** N-07 to N-10, N-12, N-15.
- **Open or historical:** N-16, N-17, F-08 to F-12, F-15, F-17.

## Verified native facts (commit cc67418cba58e71d8e70fd353e9de47199ad2075)

- **ciacore.c:592-600 is cia_set_irq_flag:**
  - it catches up;
  - it ORs the bits into irqflags and new_irqflags;
  - it clears ack_irqflags.
- **ciacore.c:1594-1625 is ciacore_async_interrupt plus ciacore_set_flag:**
  - FLAG is set with CIA_IM_FLG;
  - the idle alarm is moved to rclk+1 only if it is later.
- **viacore.c:441-458 is the VIA_SIG_CA1 case of viacore_signal:**
  - PCR edge selection;
  - CA2 toggle;
  - IFR |= CA1;
  - update_myviairq;
  - latching that depends on the build configuration.
- **Unchanged from my round-1 and round-2 audits of the same pinned files:**
  - CPU-port forwarding and capacitor facts (deadline base plus base/5 jitter, RNG order 3,4,5,6,7 and 7,6,5,4,3, strict < expiry, UINT64_MAX-10 wrapping to 349989);
  - datasette, TAP, VIA, CIA and tapeport facts;
  - the RM-10 azimuth quirk.

## Ranked path to threshold

Not required: both scores are at least 98, with zero FAIL and UNKNOWN.

Optional improvements, none blocking:
1. RP-03 lint.
2. RP-02 range tightening.
3. RP-05 naming.
4. RP-04 TODO checkpoint note after acceptance.

## Later mandatory gates

1. This verdict's primary MCP readback (verdict.persistence.json). Then pause.
2. The operator's explicit approval of the exact delta, the locked-decision-2 departure and the writer-window prerequisites.
3. R1:
   - G01-G18 guard tests green against independent mocks before any coordinator code;
   - then real guards, application inside a verified writer window, full readback, exports, traceability and a requirement-gate HV.
4. R2: all 117 groups, including the 32 AC-text-only groups, get independent fixtures and negative controls, with the full mock suite green.
5. R3: the same assertions on managed and immutable-native providers, with:
   - observer completeness and non-interference;
   - lawful loader assets;
   - native VSF;
   - full cumulative suites at zero failed, skipped or aborted.
6. Separate approvals for the D64 TR/TEST, the 36..42-track scope and the true-drive proposal.
7. Whole-plan exit: recovered requirements, dense session-log proof, immutable xvic/x64sc parity, canonical VSF, settings visibility, the final handoff and the commit order.

## Persistence

1. verdict.md and verdict.json are written first.
2. The whole verdict.md and the complete verdict.json are then saved, in ordered dialog chunks of at most 6500 characters, to turn req-20261014T042520Z-prompt-6807.
3. The turn is completed and read back.
4. Results go in verdict.persistence.json.

A genuine primary failure would use the authorized full-content failsafe, labeled queued, not persisted.

## Receipt paths

- F:\GitHub\vice-sharp\docs\receipts\hv\20261013T232143-r0-anchor-opus\verdict.md
- F:\GitHub\vice-sharp\docs\receipts\hv\20261013T232143-r0-anchor-opus\verdict.json
- F:\GitHub\vice-sharp\docs\receipts\hv\20261013T232143-r0-anchor-opus\verdict.persistence.json
- Parent-owned files in the same directory: request.jsonl, response.jsonl, stderr.log, input-pins.json, launch-result.json, prompt.md, SELF-REVIEW.md, Launch-Review.ps1, prior-input-revalidation.json.

## Scope limitations and reviewer conduct

- **Scope.**
  - Source inspection, structural comparison, live reads and read-only script re-runs only. No build, mock, managed or native execution.
  - Requirement text unchanged since earlier rounds was not re-audited line by line. I relied on my earlier audits, the unchanged pins and full structural equality.
  - The credential scan covered both workspace keys. The two locked capture streams were not read.
- **Conduct.**
  - Progress entries carried measured console times.
  - Short status lines carried no time. No time was estimated.

## Scores

- **Accuracy 99.**
  - Correct: every count, hash, binding, mapping delta, live comparison, receipt claim and source anchor I checked, including the bounded 12-row claim and the RN-05 triage report.
  - No inaccurate candidate statement found.
  - Deductions: none material. The over-inclusive anchor range (RP-02) is not an inaccuracy.
- **Completeness 98.**
  - Covered: all findings from four review rounds are dispositioned, and the sibling sweeps are complete.
  - Deductions: optional hardening remains (RP-03, RP-05), and the owner TODO notes are dated (RP-04).

Both scores reach 98, with zero FAIL and zero UNKNOWN: AGREE.

Structured twin: verdict.json in this directory.
