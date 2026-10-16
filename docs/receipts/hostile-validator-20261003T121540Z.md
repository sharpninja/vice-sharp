Independent hostile validation: Phase 2 FR-SNP-003 remediation

TimestampUtc: 2026-10-03T12:45:57.8412450Z
ValidatorIdentity: Codex. Model: gpt-6-astra. Reasoning effort: xhigh. Classification: class-1 project implementation. Workspace: F:\GitHub\vice-sharp.

OverallVerdict: AGREE. PASS 18, FAIL 0, UNKNOWN 0. The denominator is the 18 claims below, not the test count. Full FAIL list: none. Full UNKNOWN list: none.

Accuracy: 99%. Current source, actual edit/test transcripts, original TRX, fresh executions, artifact hashes, PDB source hashes and live MCP state agree. Completeness: 99%. All requested A-D surfaces, prior B2/D2 and the specified edge cases were examined. These ratings apply to the bounded review contract, not exhaustive correctness of every possible emulator state or acceptance of the recovered requirement set.

The earlier serializer diverged from written FR-SNP-003 AC4 value preservation: distinct cartridge images yielded identical attachment text. The fix restores the resolved mapping and every image byte. This is consistent with VICE media-event identity/value preservation, without asserting a native xvic file format or a saved-text loader round trip. The older ledger-match and timing-only AGREE reviews are withdrawn as evidence for this gate.

Add-profile receipt: PowerShell.MCP execute_command and Show-TextFiles exclusively. Read C:\Users\kingd\.codex\skills\add-profile\SKILL.md in full, then add-profile.md first, then all enumerated global markdown files in C:\Users\kingd\.claude\profile, excluding only add-profile.grok.md. Count: 22. No hardcoded list substituted for enumeration. Initial confirmation: 2026-10-03T07:19:02.8630651-05:00 - Profile loaded: read 22 global profile files. UTC: 2026-10-03T12:19:02.8683795+00:00. After automatic compaction, repeated the entire skill and all 22 reads. Reload confirmation: 2026-10-03T07:39:05.9959071-05:00 - Profile loaded: read 22 global profile files. UTC: 2026-10-03T12:39:05.9959423Z. Exact file inventory is in profile-receipt.json. The profile shaped this review by requiring original evidence of the missing process step, all four surfaces, and full MCP persistence before completion.

Surface A: requested validation and technical claims

A1 PASS: Current cartridge attachment serialization preserves its value and mapping; eject remains unchanged.
RuntimeSnapshotStore.cs:418-430 appends invariant mapping and Convert.ToHexString over the entire recorded image for attached=1 only. C64MemoryMap.cs:500-566 resolves/validates the mapping before recording and copies the effective image. Fresh independent current-binary probes passed 17/17: Auto plus all 11 explicit mappings, random full images up to 1 MiB, caller mutation after attach, equal-length images with a differing last byte, multiple attachments, exact eject text, empty/short images and invalid mapping rejection. See cartridge-edge-probes.json and reproducible probe source.

A2 PASS: The real product regression was demonstrated red against the restored pre-fix serializer.
Original tool transcript records serializer reversion at 11:57:26Z with completion 11:57:31Z, then the actual test result at 11:59:15Z. pre-fix-red.trx finished 2026-10-03T11:59:14.828Z with total 1, failed 1, passed 0, notExecuted 0. Assert.NotEqual failed because the two Standard8K SaveReplay strings were identical. This is test output plus the actual edit sequence, not a timestamp-only reconstruction.

A3 PASS: The shared contract passes against an isolated test double and the repaired real production path.
FrSnp003AcceptanceTests.cs:3402-3456 routes the original real acceptance test and new mock fact through the same AssertCartridgeReplayValueContract. Assertions still require distinct strings, Standard8K mapping and complete 5A/A5 image hex. MockCartridgeReplay is an explicit delegate, not a test-local type impersonating RuntimeSnapshotStore. Saved mock-green and product-green each passed 1, failed 0, skipped 0. Fresh-focused.trx and fresh-mock.trx independently each passed 1 with exit 0.

A4 PASS: The full FR-SNP-003 acceptance class passes 107 with zero failures or skips.
Independently parsed class-green.trx (107/0/0) and ran the current Release assembly: fresh-class.trx total/passed 107, failed/error/aborted/notExecuted 0, native exit 0. One of 107 is the explicit process mock; the production regression remains separately exercised.

A5 PASS: The exact Phase 2 filter passes twice at 211, with zero failures or skips.
Parent phase2-run1.trx and phase2-run2.trx each contain 211 passes. Fresh-phase2-first.trx and fresh-phase2-second.trx independently each contain 211 passes and zero failed/error/aborted/notExecuted, exit 0. The complete exact filter and commands are retained in both stdout receipts. Comparing test names with the previous 210-test set found only the new mock-contract fact added and no prior fact removed.

A6 PASS: Through-Phase-2 acceptance coverage remains 94 in scope and 0 missing.
Fresh invocation of tools/check_phase_acceptance_coverage.ps1 -ThroughPhase 2 returned exit 0, 94 in-scope requirements with AC and 0 missing from tests/. Source lines 13-19,75-103 use markdown Iteration integers and tests/ only, excluding bin/obj. PhaseAcceptanceCoverageTests.cs:21-51 executes this script and checks its exit. No priority substitution, src-only match or coverage-check relaxation was found.

A7 PASS: C64 idle 10,000 and 100,000 cycle lockstep passes.
Saved idle-lockstep.trx and fresh-idle.trx each passed exactly First10000CyclesMatch and First100000CyclesMatch: 2 passed, 0 failed, 0 skipped, exit 0. This is the requested idle gate, not a reset-PC compare or the unrelated Wolf64 disk mismatch.

A8 PASS: git diff --check passes after the current gates.
Fresh git diff --check returned exit 0 and no output, repeated after evidence review. coverage-and-diff.txt records the command and native exit. The working tree is intentionally dirty from existing work; a whitespace check does not imply a clean tree.

A9 PASS: The tested current artifacts match the claimed hashes and the two reviewed source files.
Recomputed SHA-256 for ViceSharp.Core.dll/.pdb and ViceSharp.TestHarness.dll/.pdb under bin/Release/net10.0 matches all four supplied values. Recomputed again after probes. Portable PDB document hashes match current RuntimeSnapshotStore.cs and FrSnp003AcceptanceTests.cs. Fresh tests ran that exact Release assembly in separate processes; the persistent PowerShell host old loaded DLLs were not used as test evidence.

A10 PASS: The fix restores FR/VICE-aligned media value preservation without claiming native replay-file compatibility.
Before the fix, attachment serialization omitted mapping and image bytes, violating FR-SNP-003 AC4. Current serialization stores both. Native event.c:168-215 stores attachment identity and clock and embeds first-use image bytes when EventImageInclude is enabled; event.c:1248 defaults it to 1. The alternative stores CRC and filename, and included duplicate images may use references. This supports the semantic analogy only. No xvic format equivalence or serialized-text loader round trip is claimed.

Surface B: workspace and process

B1 PASS: The review followed the active profile, workspace, tool and MCP storage contracts.
Executed the complete add-profile skill first through PowerShell.MCP/Show-TextFiles, read add-profile.md first, enumerated and fully read 22 markdown profile files excluding only add-profile.grok.md. Repeated the complete skill after automatic compaction. Read AGENTS-README-FIRST.yaml and AGENTS.md, canonical HANDOFF and applicable plugin skills. Reviewer commands/reads used PowerShell.MCP, no Python or direct REST/state-file edits. Parent shell/build/test commands also used PowerShell.MCP; parent structured apply_patch edits followed its explicit Codex base editing instruction, observed in the launch metadata. These were not shell substitutions for the mandatory profile reads.

B2 PASS: Prior B2 is remediated: red, shared mock green, then product fix and green are independently proven.
Original tool records and TRX jointly establish: real red finished 11:59:14.828Z; shared-contract/mock patch completed 11:59:43.109Z; mock test finished 12:00:29.642Z while serializer remained pre-fix; production patch completed 12:00:49.041Z; real product green finished 12:01:29.908Z. The patch content is the smallest cartridge-value branch restoration. Parent MCP turn req-20261003T115514Z-prompt-01e5 corroborates the decision and gates. Byrd v4:76 and plan:8,31,36,41,49 require this order. The prior DISAGREE remains a truthful historical receipt; the remediation does not claim the original edit obeyed the missing step.

B3 PASS: Durable review receipts and a complete new Codex MCP review turn carry the full result.
The named request JSONL and growing full response stream exist under docs/receipts/hv; the response thread.started identifies the reviewer thread. This review uses new turn req-20261003T122054Z-remediation-hv, not the prior completed turn. Full prose, all claims, scores, complete verdict JSON and both JSONL paths are stored through mcpserver-codex-plugin. queryHistory plus typed server QueryAsync readback establishes persistence; exact completed body proof is saved as session-log-proof.json.

B4 PASS: No tests were weakened or skipped, and the reviewer stayed within the read-only product scope.
The actual remediation test patch extracts the existing production assertions unchanged and adds a separately named mock fact. No existing test was removed from the selected set. Every fresh required current gate has notExecuted=0 and failed=0. The historical intentional red is distinguished from the exit gates. Reviewer writes are limited to docs/receipts evidence and the required supported MCP session-log turn. No product/test/requirement/TODO/plan/HANDOFF/Git-history edit was made by this reviewer.

Surface C: requirements and acceptance coverage

C1 PASS: Live effective FR/TR/TEST mappings and canonical requirements support the reviewed slice.
Supported plugin workflow.requirements.effective returned FR-SNP-003 -> TR-STATE-001 -> TEST-SNP-001. All 17 scoped acceptance FRs have effective TR/TEST mappings (effective-requirements-and-plan.json). Canonical FR-Snapshot.md:84-114 states eight AC, including AC4 event type/frame/cycle/value. TR-State-Management.md and TEST-Requirements.md snapshot/replay sections were inspected. FR-SNP-003 remains pending and all eight isSatisfied values are false; this review does not rewrite acceptance state or require unrelated UWP/Xbox work.

C2 PASS: The relevant AC have behavioral evidence, with serialization and in-memory playback kept distinct.
AC1/4: timestamped input recording and complete mapping/image serialization, real SaveReplay contract and 17 edge probes. AC2/3: Playback_IsBitExact independently stepped reference, cartridge attach/eject/seek and chip/CPU/RAM/VIC/audio state regressions rerun in the 107-test class. AC5: Pause/Step/FastForward clock progress and no-rerecord tests. AC6: production IReplayEngine contract plus actual Record/Play/Seek calls. AC7: snapshot reference and event-only representation, not full state per frame; full cartridge bytes are event value. AC8: periodic-slot, boundary and backward/repeated seek tests. See the detailed AC evidence below. A method-existence check or a green suite count alone is not the basis for AC4.

Surface D: active plan and exit gate

D1 PASS: Plan non-goals and state boundaries remain intact.
Live PLAN-C64SET-001 done=false; effective requirement records remain pending; all plan checklist boxes remain open. git diff shows no requirement/TODO/provenance edit and HEAD remains c364e73037f646a7c5d0005c931fb689f665ce9f. Reviewed remediation commands contain no commit/push or Phase 3/4 implementation. Existing HANDOFF modifications predate this remediation; no post-repair HANDOFF completion was claimed. FR-CPU-002 ac-05 and FR-PRF-005 text remain unchanged. No UWP/Xbox work is required by this slice.

D2 PASS: Prior D2 is remediated for the Phase 2 hostile-review exit gate.
The active plan holistic check now has the previously missing mock-before-product proof, 94/0 coverage, the exact Release filter green twice, idle 2/0/0, fresh independent source and edge-case review, and this new review receipt. The prior ledger AGREE and timing-only AGREE are excluded. The parent may record this narrow Phase 2 exit and its receipt paths after the response stream is complete. HANDOFF exit-note publication remains a parent follow-up, not a claim made here. PLAN-C64SET-001, requirements and TODOs must remain undone even after AGREE under plan:19-26.

AC evidence detail: FR-Snapshot.md:97-104 defines the eight AC. AC1/4 are covered by Recording_CapturesInputWithFrameAndCycle, observed input replay facts, SaveReplay_KeepsAttachedCartridgeBytesAndMapping and the independent full-value probes. AC2/3 are covered by real playback against independent machines, including Playback_IsBitExact, Playback_ReplaysCartridgeAttach, Playback_ReplaysCartridgeEject, Seek_RestoresOceanImageAfterEject, running-baseline VIC/SID/live-PCM and mid-instruction/CIA state facts. AC5 has actual clock advancement in Replay_CanPauseStepAndFastForward and single-step no-rerecord facts. AC6 checks the production interface and is exercised by real Record/Play/Seek operations throughout. AC7 stores a snapshot reference plus input events and periodic slot references, not a full RAM image per frame. Complete cartridge bytes belong to the attachment input value. AC8 has ObservedKey_KeepsPeriodicSnapshot and boundary, backward, repeated-seek and periodic mouse/light-pen tests. The C1 evidence file records the effective FR/TR/TEST mappings and unsatisfied state. The coverage script proves ID presence only; it is not independently treated as behavior coverage.

Historical sequence evidence: C:\Users\kingd\.codex\sessions\2026\08\16\rollout-2026-08-16T18-26-23-01a00ce5-d30d-7451-ab45-fa3e4fbb825f.jsonl. Original tool records (line numbers 31429/31432, 31481/31484, 31488/31491, 31502/31505, 31513/31516, 31527/31530) establish pre-fix reversion, real red, shared mock patch, mock green, production patch, product green. The retained remediation-tool-sequence.json contains only relevant tool calls/results. The live parent MCP turn req-20261003T115514Z-prompt-01e5 corroborates the sequence. Filesystem LastWriteTime is supplementary, not the phase-order proof. Parent native structured apply_patch edits were required by its Codex base editing contract; shell, build and test execution used PowerShell.MCP. No Python was used.

Parent evidence root: F:\GitHub\vice-sharp\TestResults\fr-snp-003-mock-remediation. Independently parsed pre-fix-red/pre-fix-red.trx (intentional failed 1), mock-green/mock-green.trx (passed 1), product-green/product-green.trx (passed 1), class-green/class-green.trx (passed 107), phase2-run1/phase2-run1.trx (passed 211), phase2-run2/phase2-run2.trx (passed 211), idle-lockstep/idle-lockstep.trx (passed 2), plus coverage-through-phase2.txt (94/0). All have zero skipped/notExecuted; current green runs have failed 0. Full counters and test names are saved in saved-gate-audit.json.

Fresh review commands: dotnet vstest F:\GitHub\vice-sharp\tests\ViceSharp.TestHarness\bin\Release\net10.0\ViceSharp.TestHarness.dll with /TestCaseFilter, /Logger:trx;LogFileName=<name>.trx, /ResultsDirectory:F:\GitHub\vice-sharp\docs\receipts\hostile-validator-20261003T121540Z-evidence and /Logger:console;verbosity=normal. Exact commands, output, native exit codes and TRX are retained per run. This reuses the verified current Release binary without making unauthorized product/build edits. Focused real test: 1/0/0; mock: 1/0/0; class: 107/0/0; phase filter first: 211/0/0; phase filter second: 211/0/0; idle: 2/0/0. Every native exit is 0. Coverage command: pwsh -NoProfile -NonInteractive -File .\tools\check_phase_acceptance_coverage.ps1 -ThroughPhase 2, exit 0. git diff --check, exit 0, no output.

Exact Phase 2 filter: FullyQualifiedName~PhaseAcceptanceCoverageTests|FullyQualifiedName~FrCpu004|FullyQualifiedName~FrMem006|FullyQualifiedName~FrMem004|FullyQualifiedName~FrSid004|FullyQualifiedName~FrSid009|FullyQualifiedName~FrUi004|FullyQualifiedName~FrCfg003|FullyQualifiedName~FrCfg004|FullyQualifiedName~FrCia005|FullyQualifiedName~FrCrt002|FullyQualifiedName~FrInp003|FullyQualifiedName~FrMed005|FullyQualifiedName~FrMon005|FullyQualifiedName~FrMon006|FullyQualifiedName~FrSid005|FullyQualifiedName~FrSnp003|FullyQualifiedName~FrVic20005

Idle filter: FullyQualifiedName~LockstepValidationTests.First10000CyclesMatch|FullyQualifiedName~LockstepValidationTests.First100000CyclesMatch

Verified artifact hashes under tests/ViceSharp.TestHarness/bin/Release/net10.0: 
ViceSharp.Core.dll SHA256 05FE046E60DE8FA6888A7EF03471E270944C39CAC7E7EC583F9D40B15D9C3DF4 (match)
ViceSharp.Core.pdb SHA256 F2B2D6759BAAFE2304E1AC2229DB488D58C0A90B21270FE1976167F68553D87F (match)
ViceSharp.TestHarness.dll SHA256 31EF73CD49D284AC47CD23CC0C5908B3E9264F9A1CDCA06B31A5AD6F26CFA8C0 (match)
ViceSharp.TestHarness.pdb SHA256 DA13E51CBB2C8DA04B4FC917445F394B53B9B00AFE95444661D80C89B8FDF4CD (match)
PDB/source matches: RuntimeSnapshotStore.cs AC21058CD43A459A58ECB3CA544ED7B2314B3AE8984A66BC0A0B48010DE8BBF9; FrSnp003AcceptanceTests.cs 2D81D483D1AD2B95FC9D46EEE172DF19249BF28B8DF1E4E5C38C02093451BDC3.

Probe limits and observations: The current public attach API rejects empty/short/invalid images before an event is recorded. A private null-image serialization fallback is not evidence of a reachable valid attach with missing value. Cartridge replay Play/Seek uses the in-memory event store; no text LoadReplay behavior was inferred. SaveReplay XML documentation still says shorter than 64KB, and the test-class summary still says playback does not exist. Those comments are stale; the canonical AC7 defines compactness as events rather than full state per frame and does not impose that size cap. This review does not accept those comments as product facts or rewrite them. I corrected a reviewer-only PowerShell expected-array precedence error in the independent probe, then reran all 17 cases successfully. No production or acceptance assertion was changed to accommodate it.

Design decisions: B2 is remediated by an actual repeated red/mock/product sequence, not by rewriting the prior failed history. Original tool calls and TRX results prove the serializer stayed pre-fix through the mock green; the shared assertions still test the real product separately. D2 now satisfies the technical and process exit gates for this bounded Phase 2 review. No whole recovered-set acceptance or requirement/TODO/PLAN-C64SET-001 done authorization follows; the active plan forbids those state changes even after AGREE. SaveReplay proves FR-SNP-003 AC4 value preservation, not native xvic text format or an unimplemented file loader.

Plan boundary: The current request is the remediation exit review, not a claim that every plan checkbox or the whole recovered set is complete. All checklist boxes remain open. No new Phase 3/4 or UWP/Xbox scope was introduced. The parent may record this narrow Phase 2 gate and update the exit note after the complete JSONL stream is available. The explicit plan non-goals still prohibit marking PLAN-C64SET-001, requirements or TODOs done, including after AGREE. No commit or push is authorized by this receipt.

Reviewer thread id: 01a0fdf8-fa4e-74f1-8b53-676b26f748d4
MCP sessionId: Codex-20261002T175731Z-plugin-session
MCP requestId: req-20261003T122054Z-remediation-hv
Human receipt: F:\GitHub\vice-sharp\docs\receipts\hostile-validator-20261003T121540Z.md
Structured receipt: F:\GitHub\vice-sharp\docs\receipts\hostile-validator-20261003T121540Z.json
Request JSONL: F:\GitHub\vice-sharp\docs\receipts\hv\20261003T121540Z-phase2-fr-snp003-remediation.request.jsonl
Response JSONL: F:\GitHub\vice-sharp\docs\receipts\hv\20261003T121540Z-phase2-fr-snp003-remediation.response.jsonl
Evidence directory: F:\GitHub\vice-sharp\docs\receipts\hostile-validator-20261003T121540Z-evidence

Session-log persistence proof: workflow.sessionlog.queryHistory returned session Codex-20261002T175731Z-plugin-session with 7 turns. client.SessionLog.QueryAsync returned this new turn and the entire staged 21,385-character verdict body at 2026-10-03T12:48:02.5056495Z; exact comparison succeeded after canonicalizing CRLF to LF. Final completed-turn status and exact complete-body readback are recorded in F:\GitHub\vice-sharp\docs\receipts\hostile-validator-20261003T121540Z-evidence\session-log-proof.json. No local session-log file was edited. The parent owns the continuing response JSONL stream; its terminal event is emitted when this review returns.

=== VERDICT JSON ===
{
  "TimestampUtc": "2026-10-03T12:45:57.8412450Z",
  "ValidatorIdentity": "Codex",
  "Model": "gpt-6-astra",
  "ReasoningEffort": "xhigh",
  "OverallVerdict": "AGREE",
  "accuracy": 99,
  "completeness": 99,
  "passCount": 18,
  "failCount": 0,
  "unknownCount": 0,
  "failList": [],
  "unknownList": [],
  "threadId": "01a0fdf8-fa4e-74f1-8b53-676b26f748d4",
  "sessionId": "Codex-20261002T175731Z-plugin-session",
  "requestId": "req-20261003T122054Z-remediation-hv",
  "receiptMarkdown": "F:\\GitHub\\vice-sharp\\docs\\receipts\\hostile-validator-20261003T121540Z.md",
  "receiptJson": "F:\\GitHub\\vice-sharp\\docs\\receipts\\hostile-validator-20261003T121540Z.json",
  "requestJsonl": "F:\\GitHub\\vice-sharp\\docs\\receipts\\hv\\20261003T121540Z-phase2-fr-snp003-remediation.request.jsonl",
  "responseJsonl": "F:\\GitHub\\vice-sharp\\docs\\receipts\\hv\\20261003T121540Z-phase2-fr-snp003-remediation.response.jsonl",
  "evidenceDirectory": "F:\\GitHub\\vice-sharp\\docs\\receipts\\hostile-validator-20261003T121540Z-evidence",
  "sessionLogProof": "F:\\GitHub\\vice-sharp\\docs\\receipts\\hostile-validator-20261003T121540Z-evidence\\session-log-proof.json"
}