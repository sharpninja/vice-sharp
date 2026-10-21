2026-10-20T19:23:32 - AGREE. Accuracy98%, completeness98%. PASS54, FAIL0, UNKNOWN0, ADD-AC2 (N27,N28).

Independent Codex gpt-6.1-sol, effort max, reviewing the class1 PhaseD plan revision8 before execution. Reviewer session: Codex-20261020T235538Z-plugin-session; request: req-20261020T235529Z-nativerestore-r8. Parent: ClaudeCode-20261020T015046Z-plugin-session / req-20261020T172116Z-prompt-6c6d.

Round7 N19 is PASS: the false global export-timing assertion is removed from R12. The whole-plan search still found broader checkpoint prose and an overstated change summary; N27 records that low-severity editorial AC. N24-N26 are incorporated. N28 adds a positive-nonzero writer-failure control. Neither added AC exposes a safety gap or vetoes AGREE.

This AGREE approves the reviewed plan quality only. D0 still requires the explicit operator implementation release and all D-pre evidence. No PhaseD step, native build or native runtime checkpoint executed; no plan/TODO/AC done-state changed.

Scope and evidence: pins.initial.json; source-pins.json; state-before.json; state-after.json; final-integrity.json: 41/41 pins,25/25 pinned source blobs,163/163 native entries unchanged; native index empty; rootHEAD unchanged; git diff --check exit0.
The subject and all requested reference scopes were read end to end, including the entire patch, preserved path inventory, source contracts, build/validator sources and both prior verdicts. Stale revision4 labels in the brief do not change the live subject. input-pins.json has no checkout commit; the checkout identity above was independently measured. All C source anchors below refer to the pinned materialization under artifacts/immutable-vice/cc67418cba58e71d8e70fd353e9de47199ad2075/source/vice/src, unless explicitly labeled as preserved patch or current wrapper.

Allowed Release TestHarness comparator filter: 0passed,1failed,0skipped,exit1, missing HalfCycleState at fixture23. Exact arguments/stdout/TRX/VSTest diagnostics retained in TestResults. This known current gap has D2 ownership and is not a green gate.
Exact command: dotnet test /home/sharpninja/github/vice-sharp/tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter FullyQualifiedName~Vic20HalfCycleComparatorTests --results-directory /home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T185200-plan-nativerestore-001-r8-gpt61sol/TestResults --logger "trx;LogFileName=vic20-comparator.trx" --diag /home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T185200-plan-nativerestore-001-r8-gpt61sol/TestResults/vstest.diag.log --blame-hang --blame-hang-timeout 5m

Finding ledger. Each round7 identifier appears exactly once with fresh revision8 evidence. For the35 round3b identifiers, the historical revision4 disposition is read from the original round4 verdict, not inferred from round7: all were PASS except F05 FAIL. Historical statuses do not describe the present review.

- P01 PASS (low; round7=PASS; historical revision4=PASS, round3b=PASS): Subject identity and stale brief labels. docs/plans/PLAN-NATIVERESTORE-001.md:3.
  Evidence: All 41 input content pins match. The live subject is revision 8, SHA256 FDA123F4C4605B8199B4FC6733292A727EE5E33EC6EBBE441B2EB129AD8E7DA7. Read-only git reports develop at 21a111672db8dd409624ef90c6af6a75d86f4b29. input-pins.json is a file array and contains no checkout commit.
  Assessment: The request has stale revision-4 wording and an inaccurate checkout-pin description; neither changes the identified live subject. Historical revision-4 dispositions were separately extracted from the original round-4 JSON.
  Correction/owned acceptance: Use revision 8 and the measured checkout identity; retain the historical ledger without presenting revision-4 outcomes as current results.

- P02 PASS (high; round7=PASS; historical revision4=PASS, round3b=PASS): Live native baseline. docs/plans/PLAN-NATIVERESTORE-001.md:7.
  Evidence: state-before.json and state-after.json agree on every path, status, length and SHA256: 163 entries, 8 modified tracked and 155 untracked. Native index empty; gitlink and native HEAD both cc67418cba58e71d8e70fd353e9de47199ad2075. Diff: 8 files, 222 insertions, 8 deletions.
  Assessment: The Linux starting-state claim is reproduced and the review left that state unchanged. Historical Windows state is receipt evidence, not a new remote measurement.
  Correction/owned acceptance: D0 must repeat the content inventory independently on each host before restoration.

- P03 PASS (high; round7=PASS; historical revision4=PASS, round3b=PASS): R1 B and retained generated output. docs/plans/PLAN-NATIVERESTORE-001.md:42.
  Evidence: R1 explicitly selects .gitmodules ignore=untracked; current .gitmodules still says dirty. D4 and Exit distinguish empty tracked/index diffs from the retained classified untracked inventory, with unchanged hashes and lengths.
  Assessment: This is the operator-approved amendment of the older all-porcelain-empty exit. Build products remain present and excluded from superproject dirt.
  Correction/owned acceptance: Apply only R1 B in D4; keep generated and injected files until scoped deletion clearance.

- P04 PASS (high; round7=PASS; historical revision4=PASS, round3b=PASS): Plan review authority and implementation hold. docs/plans/PLAN-NATIVERESTORE-001.md:78.
  Evidence: D-pre requires desktop receipts, Windows matrix, this plan HV AGREE and the verbatim implementation release before D0. The supplied 12:21 clarification authorizes hostile validation of plans while D3 defers implementation HV to step 12.
  Assessment: There is no review-versus-D3 deadlock. This pre-execution review grants no implementation release or done-state.
  Correction/owned acceptance: Retain all four entry items and the later implementation review gate.

- P05 PASS (medium; round7=PASS; historical revision4=PASS, round3b=PASS): Both desktop installation prerequisites. docs/plans/PLAN-NATIVERESTORE-001.md:78.
  Evidence: Linux 20261020T105400 receipt records Nuke DeployUser 1.3.111 from 2c8d6028. Windows 20261020T121000 receipt records PublishMsi/InstallMsi. Fresh git diff 2c8d6028 HEAD over src, build and the observer builder is empty.
  Assessment: The installed product sources remain current for this plan-only revision; both host installation claims have concrete retained evidence.
  Correction/owned acceptance: D0 must bind installed version and commit to each host rather than cite only the Linux receipt.

- P06 PASS (medium; round7=PASS; historical revision4=PASS, round3b=PASS): Phase C reference evidence. docs/plans/PLAN-NATIVERESTORE-001.md:84.
  Evidence: Read the Linux S2b/S4 and Windows 20261020T102600 receipts: 100k and 1M for both machines and 4M x64sc are reported equivalent. Existing builder, equivalence runner, clock wrapper and bus sink were read in full.
  Assessment: Those receipts support the existing observer, not the new snapshot wrappers. D5 explicitly reruns the entire matrix with snapshot variables unset and zero VICE_SNAP records.
  Correction/owned acceptance: Require the new both-host D5 rerun; do not inherit equivalence for changed binaries.

- P07 PASS (high; round7=PASS; historical revision4=PASS, round3b=PASS): Preservation and retirement authority. docs/plans/PLAN-NATIVERESTORE-001.md:79.
  Evidence: D0 copies and hashes every modified tracked and injected source file, retains originals and requires committed preservation before D4. R2/R3 explicitly retire the shim builders/project and R4 retains injected/untracked files. D4 restores only enumerated tracked paths.
  Assessment: The plan follows copy/hash/retain and the approved restoration boundary. It does not authorize native cleanup or moving originals.
  Correction/owned acceptance: Keep per-host receipts and the precise operator retirement scope; never use clean/reset-hard.

- F01 PASS (medium; round7=PASS; historical revision4=PASS, round3b=PASS): Counts and historical inventory. docs/plans/PLAN-NATIVERESTORE-001.md:7.
  Evidence: Recount: 163 Linux status entries = 8 tracked +155 untracked. The full preserved patch has 599 lines, 20 hunks and 12 files. Its inventory contains 2527 paths: 745 .o, 59 .a, 2 .exe, 1432 .Po, 106 Makefile and 107 .in.
  Assessment: Historical Windows counts and live Linux counts are distinct and agree with the plan. The entire inventory was parsed; all patch hunks were read.
  Correction/owned acceptance: Use the new D0 host inventory as the restoration baseline rather than transplanting historical counts.

- F02 PASS (high; round7=PASS; historical revision4=PASS, round3b=PASS): Real native build caller and retirement. docs/plans/PLAN-NATIVERESTORE-001.md:81.
  Evidence: TestHarness.csproj:10 sets ViceNativeProject; :68-72 defines BuildViceNative and invokes Vice.Native.proj BuildNative on Windows. Searches find this caller. The three native-safe projects are absent from ViceSharp.slnx, and both new Nuke targets and the generator directory are absent.
  Assessment: D2 names the actual property/call removal, reference audit, project/script retirement and new solution/target integration. These are planned changes, not claimed current features.
  Correction/owned acceptance: Retain build logs on both hosts proving no old target or shim script invocation.

- F03 PASS (high; round7=PASS; historical revision4=PASS, round3b=FAIL): Semantic consumers and load-detector ownership. docs/plans/PLAN-NATIVERESTORE-001.md:81.
  Evidence: Fresh rg reproduces 43 token-candidate files, 5 with Category traits and 56 broader semantic candidates. Plain Fact consumers include SnapshotResumeSpikeTests and Vic20NativeLockstepTests. ViceNative currently has a combined resolver with no audit log. D0/R6/D2 include per-method transitive inventory and both bindings.
  Assessment: Counts alone are not the gate. Revision 8 owns resolver/create/availability coverage, discovery-time audit, positive controls and a zero-attempt ordinary gate before D4.
  Correction/owned acceptance: Implement the full semantic inventory and detector contracts, including N24, before accepting the ordinary denominator.

- F04 PASS (high; round7=PASS; historical revision4=PASS, round3b=PASS): All twenty historical hunks. docs/plans/PLAN-NATIVERESTORE-001.md:19.
  Evidence: Full patch audit maps H1 at patch 5/16; H2 104/115; H3 140; H4 178; H5 53; H6 74/89; H7 219/424/435; H8 270/291/306; H9 466/487; H10 508/590; H12 230. H11 is preserved injected material outside those twenty hunks.
  Assessment: The classification separates instrumentation, MAINCPU version/layout changes, drive clock initialization/reset behavior and VIC sound corrections. H5/H6/H9 are explicitly rejected by R7 with precondition or later owner; they are not mislabeled as adapter normalization.
  Correction/owned acceptance: D0/A01 must classify every current hunk and block an uncovered required change.

- F05 PASS (high; round7=PASS; historical revision4=FAIL, round3b=FAIL): Literal checkpoint and complete blocker lifecycle. docs/plans/PLAN-NATIVERESTORE-001.md:61.
  Evidence: R12 and D5 require trigger=100000, actual callback write clock=100000 and written MAINCPU clock=100000 on both hosts before accepting either xvic fixture. D1 uses unique candidates. D5/Exit list blocked fixture, manifest, determinism, resume/round-trip and cumulative xvic case IDs, nonzero result, independent counts and Phase E hold.
  Assessment: This honors the repeated literal ruling exactly. The plan does not promise the pinned core can meet it and does not treat a later boundary as acceptance. A visible nonpassing blocker is an honest allowed outcome.
  Correction/owned acceptance: If either host misses, record both measured clocks/context under BLOCKER-R12-XVIC-CHECKPOINT; execute independent obligations and leave all dependent acceptance pending.

- F06 PASS (high; round7=PASS; historical revision4=PASS, round3b=FAIL): Comparator prerequisite allocated to D2. docs/plans/PLAN-NATIVERESTORE-001.md:62.
  Evidence: The permitted Release comparator filter completed with 0 passed, 1 failed, 0 skipped, exit 1: missing HalfCycleState at fixture:23. The fixture reflection at :48-67 requires parameterless creation and long/byte/ushort/int/bool/uint fields plus an enum Read. R13 requires the same assertions untouched and D2 implementation.
  Assessment: The current failure is acknowledged and given an implementation owner before the ordinary gate. It cannot enter the exact 20-test known-red set.
  Correction/owned acceptance: Implement mocks first, then the exact record/static comparator/mismatch contract and make the unchanged assertions pass.

- F07 PASS (high; round7=PASS; historical revision4=PASS, round3b=PASS): Pre-overwrite and interrupted restoration safety. docs/plans/PLAN-NATIVERESTORE-001.md:83.
  Evidence: D4 verifies repository/host/gitlink/native HEAD, clean index and per-path live-versus-preserved hash before overwrite; a Git-clean already-restored path is accepted. Mock cases cover changed bytes, staging, missing/corrupt receipt, wrong host/repository/gitlink and partial completion.
  Assessment: Unexpected state stops without overwrite. Resume restores only verified untouched paths; it does not blindly apply the historical patch or trust only an unstaged diff.
  Correction/owned acceptance: Keep every preflight and resume negative control before real per-host checkout of listed paths.

- F08 PASS (high; round7=PASS; historical revision4=PASS, round3b=PASS): Content-aware native integrity bracket. docs/plans/PLAN-NATIVERESTORE-001.md:79.
  Evidence: D0 records hash and length of every modified/untracked path; D2/D5 compare the same inventory and include a changed-existing-untracked-file negative control. This review independently compared all 163 entries before/after with no change.
  Assessment: An overwritten existing build artifact is detectable even when status and path count do not change.
  Correction/owned acceptance: Use path, classification, length and content hash; a path-only or count-only comparison is insufficient.

- F09 PASS (medium; round7=PASS; historical revision4=PASS, round3b=PASS): Analyzer and effective WhatIf support. docs/plans/PLAN-NATIVERESTORE-001.md:80.
  Evidence: Current builder:26 and equivalence runner:20 use CmdletBinding without SupportsShouldProcess and create directories. D1 explicitly fixes both and tests that WhatIf creates no file or directory. Save/Restore scripts also have mock WhatIf contracts.
  Assessment: The plan names the real missing behavior rather than claiming an unsupported dry run already works. New scripts require parser and analyzer gates before use.
  Correction/owned acceptance: Prove zero-write WhatIf before every real script path; keep analyzer results and command evidence.

- F10 PASS (high; round7=PASS; historical revision4=PASS, round3b=FAIL): Requirement mapping and honest validator documentation. docs/plans/PLAN-NATIVERESTORE-001.md:94.
  Evidence: The fixture summary currently claims TR-CYCLE-001 AC4; R13 corrects it to FR-SNP-005 AC3 and TEST-SNP-002 AC4. LockstepValidator was read end to end and remains IViceNative/register/CIA based. D5 owns a class-summary test because XmlDocsConventionTests only scans attributed methods.
  Assessment: REAL-C64-PHI2-AEC-001 is oracle-reference evidence. Managed TR-CYCLE-001 AC4 and task 5 stay pending Phase E; the state-level comparator is not mislabeled as an implemented trace comparator.
  Correction/owned acceptance: Keep native reference evidence, managed acceptance and present class behavior distinct in docs and store state.

- F11 PASS (medium; round7=PASS; historical revision4=PASS, round3b=PASS): AC2 reference change preserves semantics. docs/plans/PLAN-NATIVERESTORE-001.md:80.
  Evidence: The canonical snapshot requirement and live store still reference the old shim builder. D1 changes that reference to build-vice-observer.ps1 through the requirements wrapper while preserving clean out-of-tree materialization and external-unit-only semantics.
  Assessment: Only AC1 is explicitly semantically amended by R15. AC2 is a reference update, not permission to patch or build inside native/vice.
  Correction/owned acceptance: Read back AC2 and update all plan/validation references through the named D1 propagation step.

- R01 PASS (high; round7=PASS; historical revision4=PASS, round3b=PASS): Current reviewer credential handling. docs/receipts/hv/20261020T185200-plan-nativerestore-001-r8-gpt61sol/Review-Read.ps1:1.
  Evidence: No marker or credential-bearing file was read, copied or printed by this reviewer. The supported plugin alone reads its marker. Read/search helpers reject credential paths and wrapper output is guarded. No raw session/TODO/requirements REST call was made.
  Assessment: The explicit round-1 correction overrides the older marker-reading default. A plugin source-directory search preceded the extra glob guard; a file-list audit confirmed its ignored marker was outside that search, with no credential exposure observed.
  Correction/owned acceptance: Continue using the supported wrappers and never inspect or reproduce the marker.

- U01 PASS (high; round7=PASS; historical revision4=PASS, round3b=PASS): Own primary reviewer session and complete-content audit. docs/receipts/hv/20261020T185200-plan-nativerestore-001-r8-gpt61sol/verdict.persistence.json:1.
  Evidence: Supported Status/bootstrap/begin succeeded with drain disabled. Own session Codex-20261020T235538Z-plugin-session and request req-20261020T235529Z-nativerestore-r8 are primary persisted; checkpoint history returned in_progress with 5 decision dialogs and 7 actions. Full-body and completed-state proofs belong to verdict.persistence.json and its retained history responses.
  Assessment: This uses the authorized Codex plugin with independent parent links. There was no untrusted error and no reason to repair hooks, replay queues or use failsafe-only acceptance. Final acceptance requires the completed full-content proof, not this checkpoint alone.
  Correction/owned acceptance: Persist both full verdict artifacts in ordered chunks, reassemble and hash compare, complete the turn and retain the completed-state history proof.

- U02 PASS (high; round7=PASS; historical revision4=PASS, round3b=PASS): Live requirements, mappings and TODO state. docs/plans/PLAN-NATIVERESTORE-001.md:79.
  Evidence: Supported wrapper queries read FR-VICE-001, TR-VICE-ORACLE-001, TEST-VICE-001, TR-VSFLOCKSTEP-RESUME-001, TR-CYCLE-001, FR-SNP-005, TEST-SNP-002, both FR mapping sets and PLAN-VICEORACLE-001. Requirement ACs remain unsatisfied; tasks 4 and 5 remain false. Raw responses are store-*.txt in this receipt.
  Assessment: The named mapping edges exist. Pending current state is appropriate before D1 semantic/reference amendments and real D5 acceptance; old handoff or reviewer labels are not substituted for live store evidence.
  Correction/owned acceptance: Reconcile the D1 amendments through the wrapper, keep task 4 pending until step 12 and task 5 pending through Phase E.

- A01 PASS (high; round7=PASS; historical revision4=PASS, round3b=PASS): Current hunk classification gate. docs/plans/PLAN-NATIVERESTORE-001.md:79.
  Evidence: D0 splits each current patch by @@ and maps each hunk to section 2 or an explained host difference. Missing and wrong-host hunk controls are required; an unclassified or required-uncovered hunk blocks D4.
  Assessment: A historical twenty-hunk classification does not silently authorize arbitrary current modifications.
  Correction/owned acceptance: Retain the full current patch and one disposition per hunk on each host.

- A02 PASS (high; round7=PASS; historical revision4=PASS, round3b=PASS): Both-host entry evidence and explicit release. docs/plans/PLAN-NATIVERESTORE-001.md:78.
  Evidence: D-pre/D0 require per-host installed version/commit, complete Windows matrix, this plan AGREE receipt and the verbatim operator release. D4 repeats the committed-preservation/HV/release conditions.
  Assessment: The entry dependencies do not disappear because a plan review passes or older restoration authority exists.
  Correction/owned acceptance: Keep the explicit implementation hold until every entry item is present.

- A03 PASS (high; round7=PASS; historical revision4=PASS, round3b=PASS): Failure after restoration. docs/plans/PLAN-NATIVERESTORE-001.md:83.
  Evidence: PostRestoreFailure_StopsWithPreservedEvidence covers failure after the first host and after both. It retains originals/receipts, reports each path state, leaves restored pinned source in place and stops before Phase E without automatic reapply.
  Assessment: Rollback does not destroy evidence, restore a dirty native patch automatically or turn a failed gate into success. Independent Phase D cases remain reportable under the explicit BLOCKED rules.
  Correction/owned acceptance: Use the documented stopped/nonpassing state and ask the operator for any further native-source change.

- N01 PASS (high; round7=PASS; historical revision4=PASS, round3b=PASS): R10 avoids active uninitialized drive-clock copies. docs/plans/PLAN-NATIVERESTORE-001.md:55.
  Evidence: The supplied fixture has DRIVE8 v2 payload length 2 with bytes 0,1, despite its truedrive name. Pinned drive-snapshot.c:315-319 declares uninitialized arrays, :368-419 fills them only for TDE and :540-550 copies clocks for non-NONE units. R10 computes NONE/TDE-off flags from every module and places them last.
  Assessment: A launch precondition can make the unsafe active-copy path unreachable without editing the immutable reader. A missing module and malformed payload are explicit cases.
  Correction/owned acceptance: Verify final arguments and effective resources before claiming a native legacy load.

- N02 PASS (high; round7=PASS; historical revision4=PASS, round3b=FAIL): Complete-load result and ordering. docs/plans/PLAN-NATIVERESTORE-001.md:91.
  Evidence: Both machine snapshot readers call MAINCPU first and can fail/reset later. D1 wraps MAINCPU and machine_read_snapshot separately. D5 requires resume with clk/A/X/Y/SP/PC/P, then load,0 before the next gate; first gate is C+1, idle/stolen prefix allowed, first CPU read/dummy at PC.
  Assessment: MAINCPU success alone cannot prove whole-machine success. The pinned mem_read_check_ba permits stolen cycles before the first CPU access; the plan tests both that case and later-module failure/reset.
  Correction/owned acceptance: Preserve authoritative record order and all seven fields; reject missing/full-load failure, unrelated reset or truncated continuation.

- A04 PASS (high; round7=PASS; historical revision4=PASS, round3b=PASS): Repository identity and Windows autocrlf. docs/plans/PLAN-NATIVERESTORE-001.md:83.
  Evidence: D4 checks root, submodule Git directory, host, gitlink and checkout identity. An already-restored path is recognized by Git-clean status, allowing Git clean/smudge conversion, rather than requiring raw blob bytes to equal CRLF checkout bytes. A CRLF mock is explicit.
  Assessment: This avoids both wrong-repository restoration and false drift on a valid Windows checkout.
  Correction/owned acceptance: Keep byte-hash protection for preserved dirty paths and Git-aware comparison for already-restored paths.

- A05 PASS (high; round7=PASS; historical revision4=PASS, round3b=PASS): Exact known-red identities and full execution. docs/plans/PLAN-NATIVERESTORE-001.md:81.
  Evidence: D2 retains discovered IDs and terminal TRX per project, zero skips/aborts/unexecuted selected cases, and exactly the 20 FQNs from the Phase E diagnostic receipt. Negative controls include swapped equal-count sets, missing project, discovery drift and partial run.
  Assessment: A failure count of twenty cannot hide a new failure or omitted project. The comparator is repaired before this gate and is outside the exception.
  Correction/owned acceptance: If any known-red identity changes, fail the gate and obtain an explicit revised disposition; never silently alter the set.

- A06 PASS (medium; round7=PASS; historical revision4=PASS, round3b=PASS): Up-to-date builder rerun and interrupted evidence. docs/plans/PLAN-NATIVERESTORE-001.md:80.
  Evidence: Current builder:220-231 extracts link lines only from current make stdout and fails on up-to-date output. D1 requires reuse of retained stock-link-lines.json only after named-object hashes match, otherwise a named prerequisite stops replacement. D5 owns real All reruns on both hosts.
  Assessment: The idempotent/resume claim is testable and does not trust stale link evidence or require destructive clean builds.
  Correction/owned acceptance: Run the up-to-date and interrupted-stage mocks before the real reruns.

- R02 PASS (high; round7=PASS; historical revision4=PASS, round3b=PASS): Current reviewer progress and timestamps. docs/receipts/hv/20261020T185200-plan-nativerestore-001-r8-gpt61sol/Review-Read.ps1:39.
  Evidence: Progress JSON was emitted live at meaningful checkpoints with command-measured local timestamps, numeric completion and ETA. New audit/dialog timestamps are generated with Get-Date; historical timestamps and protocol UTC IDs are identified as source/tool records.
  Assessment: No progress history was reconstructed or backfilled. The wrapper requires UTC-shaped IDs, while user-facing progress uses sortable local time.
  Correction/owned acceptance: Continue measuring progress at emission and preserve the full response stream.

- N03 PASS (medium; round7=PASS; historical revision4=PASS, round3b=UNKNOWN): R14 explicitly accepted dead read. docs/plans/PLAN-NATIVERESTORE-001.md:64.
  Evidence: Pinned rotation.c:194 reads rotation_table_ptr into speed_zone; :203 overwrites it before use. H5 initializes only the clocks, so the preserved patched path contains the same rotation-table read. R14 records the operator acceptance and D5 continuation-equality evidence.
  Assessment: This is a scoped operational disposition, not a claim that an uninitialized C read is generally defined or sanitizer-clean.
  Correction/owned acceptance: Retain the source argument and real both-host continuation evidence; do not patch VICE or broaden the claim.

- N04 PASS (high; round7=PASS; historical revision4=PASS, round3b=ADD-AC): Architectural native register observer. docs/plans/PLAN-NATIVERESTORE-001.md:80.
  Evidence: MOS6510_REGS_GET_STATUS at mos6510.h:116-136 reconstructs N/Z and P_UNUSED. MAINCPU readers update global maincpu_regs in mainc64cpu.c:934 onward and mainviccpu.c:756 onward. Their calls and machine_read_snapshot calls cross objects on both machines.
  Assessment: The two wraps can observe loaded architecture before the next gate and forward exactly once. D1 covers P combinations, wide clock/PC, return/state preservation, no resume on reader failure and sink write/flush exit 72.
  Correction/owned acceptance: Prove wrap symbols in each binary and bind the same assertions to the real both-host paths.

- N05 PASS (high; round7=PASS; historical revision4=PASS, round3b=ADD-AC): DRIVE validation and effective media context. docs/plans/PLAN-NATIVERESTORE-001.md:91.
  Evidence: D0 scans DRIVE8-11 presence/first bytes/length for every fixture. D1 rejects invalid/truncated has_tde/has_drives data. drive-resources.c drive_resources_type changes unit/bus state and detaches images; iecbus.c:493-547 routes by resource state. D5 emits effective type/TDE/media per unit.
  Assessment: R10 is not observationally neutral to media; the plan makes detached/no-media context visible and never treats missing/truncated payloads as valid input.
  Correction/owned acceptance: Assert final resources and attached image or explicit none, as well as the precondition argument list.

- N06 PASS (medium; round7=PASS; historical revision4=PASS, round3b=ADD-AC): Transitive providers and managed-fake controls. docs/plans/PLAN-NATIVERESTORE-001.md:49.
  Evidence: R6 and D0/D2 include fixture/helper/provider paths and four semantic controls: direct plain Fact, test-to-helper-to-provider, pure managed IViceNative fake and string-only mention. Comment-only token matches are excluded from semantic classification.
  Assessment: The plan handles a shim consumer reached only through discovery/data provision without excluding innocent managed fakes.
  Correction/owned acceptance: Retain per-method ownership and positive/negative classification results, not only grep counts.

- N07 PASS (high; round7=PASS; historical revision4=PASS, round3b=ADD-AC): Absolute limit and nonempty continuation. docs/plans/PLAN-NATIVERESTORE-001.md:91.
  Evidence: initcmdline.c:279-286 parses an absolute maincpu_clk limit; active CPU loops check it after instructions. Legacy MAINCPU C is 109163946. D1/D5 use C+W with a documented low-clock prelude exception, acknowledge prelude permits and require exactly W compared cycles.
  Assessment: A prelude-only process or instruction overshoot cannot masquerade as resumed acceptance. Overflow, early exit and a truncated baseline are rejected.
  Correction/owned acceptance: Record per-host budget, complete window, retained overshoot and terminal reason.

- N08 PASS (high; round7=PASS): Mock-first phase allocation. docs/plans/PLAN-NATIVERESTORE-001.md:80.
  Evidence: D1 creates every Slice 1 family as VsfSlice1Contract against fake providers/recorded receipts. D5 registers same-case real bindings as VsfSlice1Real, using artifacts produced by that run. No case skips.
  Assessment: D2 can prove contracts before D5 fixtures exist, while only real D5 execution can satisfy Slice 1 acceptance. BDPv4 retains the same assertions across providers.
  Correction/owned acceptance: Keep chronological mock-green/negative-control evidence before production edits and separate contract versus real denominators.

- N09 PASS (high; round7=PASS): Task acceptance remains pending. docs/plans/PLAN-NATIVERESTORE-001.md:96.
  Evidence: Exit and blast radius leave PLAN-VICEORACLE-001 tasks 4/5 pending through the TODO wrapper with evidence attached; task 4 changes done-state only at step 12 after required AGREE, task 5 after Phase E/final immutable gate. Live TODO reports both false.
  Assessment: This respects D3 and avoids task-4 completion being used as premature permission for task 5.
  Correction/owned acceptance: Attach evidence without toggling done during Phase D.

- N10 PASS (high; round7=PASS): Marker-relative windows and own history. docs/plans/PLAN-NATIVERESTORE-001.md:91.
  Evidence: D5 starts each writing/resumed window at its own checkpoint/load marker, preserves raw indices, compares offsets without shifts/trimming, and derives BA/AEC only from that run. It rejects discarded prefixes, borrowed history and unequal reused prelude clocks.
  Assessment: obs-emitter.c and bus-adapter.c use process-local indices/history. The plan does not assume those histories become equal merely because MAINCPU clocks match.
  Correction/owned acceptance: Compare the full prescribed interval and report a real mismatch rather than warming away its prefix.

- N11 PASS (high; round7=PASS): Load-log existence, failure and inertness. docs/plans/PLAN-NATIVERESTORE-001.md:81.
  Evidence: D2 ordinary gate requires an existing empty audit log and rejects missing/unwritable logs. Resolve/create controls cover both native bindings, aliases/fallback and failure, with separate positive logs. The unset path must preserve stepping and deterministic output.
  Assessment: A missing file is not interpreted as zero attempts, and a failed audit append cannot silently pass the gate.
  Correction/owned acceptance: Initialize the ordinary audit before discovery and keep positive controls separate from its retained empty log.

- N12 PASS (high; round7=PASS): Authorized R15 real-input amendment. docs/plans/PLAN-NATIVERESTORE-001.md:91.
  Evidence: No real 1.3 fixture is present; pinned MAINCPU writers emit 1.4. R15 explicitly changes real AC1 inputs to 1.2 and 1.4. D1 applies/readbacks that semantic amendment; D5 uses supplied real 1.2 plus a fresh pinned-native 1.4 round trip and full sequence comparison.
  Assessment: This is an authorized semantic change, not invented provenance or silent weakening. The existing synthetic 1.3 byte cases remain.
  Correction/owned acceptance: Label synthetic versus real evidence and do not claim a native v1.3 resume.

- N13 PASS (medium; round7=PASS): Per-host prelude and budget receipts. docs/plans/PLAN-NATIVERESTORE-001.md:91.
  Evidence: D5 records each host/fixture prelude evidence, C, W, absolute limit, permits, exact window and terminal result. c64.c:1070 and vic20.c:951 specify the three-second autostart delay; autostart.c:1528-1537 also contains random-delay handling.
  Assessment: The plan requires measured prelude context rather than assuming one host duration or reusing a writing-run history. Seed and final launch arguments are pinned.
  Correction/owned acceptance: Reject any budget reached before load and retain the reason and host-specific evidence.

- N14 PASS (high; round7=PASS): Observable nonpassing BLOCKED exit. docs/plans/PLAN-NATIVERESTORE-001.md:96.
  Evidence: R12, D5:93 and Exit define stable blocked case IDs, blocker name, both measured clocks/context, separate independent denominators and nonzero cumulative result. Full Slice 1 acceptance, dependent ACs and task done-states stay pending; Phase E is held until operator resolution and case execution.
  Assessment: A genuine inability to write at literal 100000 is visible and cannot become a skip, pass, partial completion or implicit waiver. It does not stop the other Phase D obligations from being executed and reported.
  Correction/owned acceptance: Preserve the two terminal outcome classes COMPLETE/BLOCKED and their noninterchangeable acceptance semantics.

- N15 PASS (high; round7=PASS): Checkpoint writer and cycle-gate interaction. docs/plans/PLAN-NATIVERESTORE-001.md:80.
  Evidence: D1 has gate-double controls for exactly one gate/permit per advanced cycle, no advance with a withheld permit, unchanged quit/EOF/error, no rearming and independent trigger/write clocks. Existing clock-gate and bus-adapter control flow was read; D5 repeats armed cases for real on both hosts.
  Assessment: Arming a trap from the cycle hook is not assumed free of control-flow effects. The plan proves it without bypassing the gate or building inside native/vice.
  Correction/owned acceptance: Keep the armed interrupt/stolen cases and the no-snapshot equivalence matrix as distinct real gates.

- N16 PASS (high; round7=PASS): Audit includes discovery. docs/plans/PLAN-NATIVERESTORE-001.md:81.
  Evidence: ViceFactAttribute and ViceTheoryAttribute query availability during construction. D2 sets VICESHARP_NATIVE_LOAD_LOG on the launched dotnet test process before discovery and forbids truncation after discovery; attribute-only and provider-only attempt controls run in fresh processes.
  Assessment: An audit initialized only inside a selected test would miss those probes; revision 8 explicitly covers the earlier interval.
  Correction/owned acceptance: Retain the full discovery-plus-execution log from the actual ordinary gate.

- N17 PASS (high; round7=PASS): Zero-allocation hot-path detector control. docs/plans/PLAN-NATIVERESTORE-001.md:81.
  Evidence: D2 requires a focused seam-level allocation/log-call measurement over warmed repeated step calls for both bindings with audit unset, a deliberately allocating/logging double that must fail, and unchanged deterministic output. Logging belongs only in resolve/create/probe paths.
  Assessment: This directly tests the zero-incremental-allocation requirement rather than relying on a full-suite pass or a general performance claim.
  Correction/owned acceptance: Run both binding controls and retain allocation, log-call and deterministic-output evidence.

- N18 PASS (medium; round7=PASS): Clock versus half-cycle units. docs/plans/PLAN-NATIVERESTORE-001.md:91.
  Evidence: D1/D5 define W in whole CPU cycles and exactly 2W VICE_OBS half-cycles, reject odd half-cycle requests and overflow, and check every sample. Existing bus-adapter emits PHI1 and PHI2 before the gate.
  Assessment: The window length cannot silently double, halve or shrink to fit observed output.
  Correction/owned acceptance: Keep explicit units and counts in both mocked and real launch/window receipts.

- N19 PASS (medium; round7=FAIL): Original global export-timing error removed. docs/plans/PLAN-NATIVERESTORE-001.md:61.
  Evidence: R12 now states the chosen public trap boundary whose copied pending mask contains the trap and accepts the actual write clock. Both active CPU units include 6510dtvcore.c. Its BRK export at :1012 follows FETCH_OPCODE at :1804; the revised plan explicitly acknowledges BRK after fetch in section 1.
  Assessment: The round-7 false assertion that correct registers are exported only at an instruction boundary is gone. The chosen trap design is supported. Section 1 still has universal external-checkpoint wording and section 7 overstates how fully prose was narrowed; N27 records that residual without pretending the original timing defect remains.
  Correction/owned acceptance: Keep the narrow operational claim; apply the nonblocking N27 scope/summary cleanup.

- N20 PASS (high; round7=PASS): Shared ordinary selector and cumulative projection. docs/plans/PLAN-NATIVERESTORE-001.md:81.
  Evidence: Build.cs:112 and :140 have separate current Test/CiTest filters. D2 replaces them with one shared selector adding !=ShimLegacy and !=VsfSlice1Real, preserves contracts, and makes the cumulative target select only real cases with stable IDs/nonzero BLOCKED result. Projection checks run at D2 and after D5 registration.
  Assessment: The native-free ordinary gate can complete even when real xvic cases are blocked, while no real obligation disappears from the cumulative denominator.
  Correction/owned acceptance: Bind the retained selected-ID projections to actual discovery, not a hand-maintained count.

- N21 PASS (high; round7=PASS): Candidate isolation and publication failure. docs/plans/PLAN-NATIVERESTORE-001.md:80.
  Evidence: D1 covers one-host/both-host mismatch, missing/malformed checkpoint, write failure and interrupted retry: accepted fixture/manifest unchanged, existing candidate bytes retained in unique receipt paths, stale artifact never accepted, nonzero result and blocked IDs. D5 owns real binding.
  Assessment: The callback can write before clock validation without publishing an unacceptable fixture or replacing old accepted evidence.
  Correction/owned acceptance: Never overwrite accepted artifacts during candidate collection or reuse an interrupted artifact as new output.

- N22 PASS (high; round7=PASS): Trap armed after pending-mask capture. docs/plans/PLAN-NATIVERESTORE-001.md:80.
  Evidence: 6510dtvcore.c:1758 copies global_pending_int before DO_INTERRUPT at :1763; the macro uses that captured mask, services NMI/IRQ before :408-411 trap/export. D1 explicitly covers triggering while an earlier interrupt is already being dispatched.
  Assessment: That history can reach the next boundary after the first handler opcode. The write clock is measured, not inferred from the trigger or an earlier generic instruction boundary.
  Correction/owned acceptance: Reject a later measured write under literal R12 and preserve interrupt/stolen context on both hosts.

- N23 PASS (high; round7=PASS): Low-clock prelude and bounded terminal outcome. docs/plans/PLAN-NATIVERESTORE-001.md:80.
  Evidence: D1 canonically uses C+W unless inside the measured prelude, then prelude+W with a stated reason. After load,0 the required W/2W window must execute, followed by limit or gate quit within a recorded overshoot allowance; tail, permits and exit reason are retained. D5 repeats the same low-clock case.
  Assessment: An absolute limit combined with a restored lower clock cannot leave the producer waiting forever for an unsupplied permit or silently stretch the window.
  Correction/owned acceptance: Reject early termination, an indefinite wait, omitted tail or a changed interval.

- N24 PASS (medium; round7=ADD-AC): Successful ordinary discovery resolves no legacy libraries. docs/plans/PLAN-NATIVERESTORE-001.md:81.
  Evidence: Revision 8 explicitly adds FindLibraryPath, IsAvailable and AvailabilityMessage to audit scope. D2 mock/D5 recheck require ordinary discovery with legacy attributes/providers excluded to resolve/load none of vice_x64, vice-shim or vice_xvic, keep the managed fake selected and execute its denominator; deliberate attempts have separate logs.
  Assessment: This incorporates the round-7 added AC. Current availability attributes make the problem concrete; successful discovery behavior, not only detection of inevitable probes, now has an owner.
  Correction/owned acceptance: Implement the discovery control without weakening the selected-case or audit interval requirements.

- N25 PASS (low; round7=ADD-AC): Absent candidates are recorded honestly. docs/plans/PLAN-NATIVERESTORE-001.md:80.
  Evidence: Unique receipt paths are now required. D1 states absent/not-created/removed-by-native-writer outcomes with result/checkpoint-fail/process logs and no invented hash. Pinned c64-snapshot.c:92-94 and vic20-snapshot.c:91-93 remove failed newly created snapshots.
  Assessment: The round-7 evidence refinement is incorporated while existing bytes and accepted artifacts remain protected.
  Correction/owned acceptance: Hash and retain bytes only if they exist; never create fictitious candidate evidence.

- N26 PASS (low; round7=ADD-AC): R15 readback and obsolete blocker cleanup. docs/plans/PLAN-NATIVERESTORE-001.md:91.
  Evidence: D5/D1 readback explicitly requires real AC1 1.2/1.4, unchanged synthetic 1.2/1.3/1.4 byte cases, unchanged AC2 semantics and reconciled blocker lists. Exit now says the only current named blocker is R12 and the real-1.3 question was resolved by R15.
  Assessment: The round-7 propagation AC is incorporated. No optional v1.3 blocker remains as an alternative acceptance branch.
  Correction/owned acceptance: Capture that readback before acceptance and preserve the synthetic matrix at VSF plan:340.

- N27 ADD-AC (low; new this round): D1 ReviewSummary_MatchesChosenCheckpointScope. docs/plans/PLAN-NATIVERESTORE-001.md:164.
  Evidence: Whole-plan search retained in export-wording-audit.json finds export-related prose at :14, :54, :61 and :164. Section 1 still says only checkpoint an external unit can obtain is at that trap boundary; R11 still enumerates other exports; section 7 says the revision makes no claim about where else the core exports. The latter description is literally overstated.
  Assessment: The actual global timing error is fixed and all operational acceptance uses the chosen trap plus measured clock. No unsafe checkpoint is admitted. This is a nonblocking propagation/editorial AC, not a new assertion that arbitrary-cycle external snapshots are possible.
  Correction/owned acceptance: D1 owns the propagation check: section 1/R11/R12/section 7 describe only the chosen seam and its acceptance rule, and the change summary matches the actual retained prose. State that other export sites exist without an unsupported universal exclusivity claim.

- N28 ADD-AC (low; new this round): D1/D5 SnapshotWriter_AnyNonzeroResultRejectsPublication. docs/plans/PLAN-NATIVERESTORE-001.md:80.
  Evidence: Pinned vic20/vic20-snapshot.c:99-102 closes/removes the candidate and returns positive 1 when IEEE488 VIA module writing fails. Other writer failures return -1. D1 already requires any write failure to produce checkpoint-fail and prevent publication, but does not enumerate this positive nonzero path.
  Assessment: Existing failure, record-validation and candidate-isolation gates fail closed. An independent positive-1 control makes the expected outcome explicit and prevents an implementer from testing only negative return codes. It needs no VICE change and is not a safety-gap veto.
  Correction/owned acceptance: D1 mocks and D5 real classification must treat every nonzero writer result, including +1, as failure: no successful checkpoint/accepted fixture/manifest update, nonzero generator outcome, and honest retained-or-absent candidate evidence.

Pinned-source and rule-chain assessment:

- Trap feasibility is supported by more than the declaration: interrupt.c:320-370 queues and dispatches the public callback; active 6510dtvcore.c exports before dispatch at 410-411. event.c:692-698 calls machine_write_snapshot inside an upstream trap callback registered at 772, with another writer trap at 777-798. Thus the callback-writer pattern is native usage. It still does not guarantee literal100000.

- MAINCPU snapshot writers serialize maincpu_clk and global architecture. A callback can capture the actual boundary clock and the generator independently verifies the written MAINCPU clock. A trigger during an already-dispatched interrupt or stolen run is not assumed equal to that clock. Both hosts must prove the literal match or report R12 BLOCKED.

- Both required observer calls are cross-object on both machines: autostart.c to machine_read_snapshot in c64.c/vic20.c; each machine snapshot reader to maincpu_snapshot_read_module in mainc64cpu.c/mainviccpu.c. Inner architecture and outer return are independently visible before the next gate. New VICE_SNAP lines are framed resume evidence; no claim of whole-stderr byte equivalence is made for snapshot runs. The no-snapshot S3/S4c rerun must emit zero such lines.

- The comparator reflection requires HalfCycle long; A/X/Y/S/P/Opcode/BusData byte; PC/BusAddress ushort; MicroOpPhase int; HasBusEvent/IsDummy bool; IrqStage/NmiStage/BaState uint; BusAccess an enum with Read. It requires parameterless creation, settable properties, static CompareHalfCycleStates and mismatch HalfCycle long, Field/Managed/Native/Message strings, including $1000/$1001 and half-cycle 37. R13 preserves the executable assertions and fixed first-difference order.

- The fixture facts were read from bytes: ready-c64sc-truedrive.vsf has MAINCPU1.2 payload91, clock109163946, A194 X2 Y0 SP250 PC22377 P160, DRIVE8 bytes0,1 payload2 and 24 modules. The separate real1.4 file has clock30250590 and SHA1D6A178A1C415FA1B5BCCC30551E5FC210701E363762B05EEDCDF237F88F8EFF. No real1.3 input is invented. Full module framing parsed to each file end.

- Rule chaining found no remaining unsafe or silent-completion cycle: D1 mocks make D2 independent of future real artifacts; shared ordinary selectors keep R12 real cases out while keeping contracts in; D5 retains the blocked real IDs/nonzero cumulative result; D3/task done-state holds and Phase E stop remain effective. A03 governs post-restore failure without automatically undoing the approved clean source. R9 identity drift stops acceptance instead of silently widening the exception.

Requirements and acceptance ownership:

- FR-VICE-001 / TR-VICE-ORACLE-001 / TEST-VICE-001 AC1+AC2 (D0 mapping read / D2 retirement / D4 clean checks / D5 preflight): Mapping read from live wrapper; pinned-source/build/clean-target evidence required. No requirement is marked satisfied by this plan review.

- TR-VSFLOCKSTEP-RESUME-001 AC1 (D1 explicit R15 amendment / D5): Real external inputs become 1.2 and 1.4 by the verbatim operator ruling, wrapper readback first; seven registers/clock, complete load and full round-trip continuation are retained. Synthetic 1.3 remains.

- TR-VSFLOCKSTEP-RESUME-001 AC2 (D1 / D5): Builder reference changes only; clean out-of-tree materialization, external wrappers and unchanged source semantics remain.

- TR-CYCLE-001 AC4 (D5 reference / Phase E managed): Native REAL-C64-PHI2-AEC-001 supports the oracle reference. Managed core acceptance stays pending; native traces cannot satisfy it by themselves.

- FR-SNP-005 AC3 / TEST-SNP-002 AC4 (D2 comparator contract / later real comparison): Correct the fixture mapping and implement its exact reflection contract without weakening assertions. The state comparator alone does not close full managed snapshot parity.

- PLAN-VICEORACLE-001 tasks 4+5 (Phase D evidence / step12 / Phase E): Both current tasks false. Attach D evidence but leave done-states pending: task4 only after step12 required AGREE, task5 after Phase E and final immutable gate.

Slice1 obligation map, including every substantive line in the requested source ranges:

- VSF plan 230-234: PASS. D1 mocks / D5 manifest. Schema 1 top-level viceCommit, fixtureGeneratorVersion, containerFraming, romAllowlist, fixtures, machines; every required per-fixture and per-module field/edge, source/managed/adapter/test mapping, mandatory state and omission/duplicate controls. Manifest drives coverage without replacing the parser.

- VSF plan 238-239, 325, 328, 334-335: PASS. D1 builder contracts / D5 real. Explicit R8 amendment replaces DLL/shim delivery with pinned observer executables. Windows MSYS2 MINGW64 and required tools remain; Linux canonical builder remains PowerShell. External units, build evidence, wrap symbols and pre/post materialization/native inventories prove immutable source.

- VSF plan 240-243, 326, 329: PASS. D0 scan / D1 generator mocks / D2 targets / D5 real. Keep supplied x64sc hash/190854 bytes/58-byte header/24 ordered modules. Generate xvic PAL and NTSC at literal100000 or explicit R12 BLOCKED; verify two fresh runs and cross-host bytes. Pinned ROM subset, media hashes/profiles and future media-fixture ownership remain.

- VSF plan 330-333, 340, 342: PASS. Existing adapter cases / D1 / D5. Retain synthetic 1.2/1.3/1.4, malformed, duplicate and overflow cases and all 199 existing cases. Normalize only versioned MAINCPU fields externally; R15 changes real external inputs only to 1.2/1.4.

- VSF plan 336, 338: PASS. D2 semantic classification / D5 cumulative target. Missing native binaries/fixtures fail rather than ViceFact skip. Contract tests run ordinarily, real tests in their own cumulative denominator; R12 cases are explicitly BLOCKED/nonzero, never skipped/pass.

- VSF plan 321-324, 340: PASS. D1 before production / D2 / D5. Mock-first correct inputs and independent defects prove every assertion, followed by the same real-bound assertions. No mock success is claimed as native runtime acceptance.

- VSF plan 344-350: PASS. D1 amendment / D2 Nuke / D5 execution. Replace the retired shim commands explicitly, wire GenerateNativeSnapshotFixtures and NativeSnapshotCompatibilityTest through the .NET generator and observer path, and run both hosts with retained denominators/evidence.

Structured what-if pass:

- Checkpoint differs between hosts (D1 mocks / D5 real): Reject publication even if one host writes exactly 100000; retain unique candidates or honest absence, both clocks/context, BLOCKED IDs and nonzero cumulative result. Run independent obligations; Phase E held.

- Trap fires after a pending interrupt (D1 / D5): Use the callback clock after NMI/IRQ, including a trigger after the earlier pending-mask capture. A later clock is not 100000; literal xvic acceptance fails without rearming.

- Checkpoint requested during a stolen-cycle run (D1 / D5): Arm once through the ordinary hook/gate, retain trigger/write clock and stolen context; exactly one permit per advanced cycle and none while withheld. Accept only actual write/MAINCPU 100000.

- Resume first access is stolen (D5 N02/N10): Require resume then load,0 before the next gate at C+1; permit idle/stolen prefix before first CPU read/dummy at PC. Compare all 2W half-cycles using the resumed run own BA history, with no trimming or borrowed warm-up.

- Shim reached only by a data provider (D0 inventory / D2 / D5): Classify the transitive consumer, audit before discovery, observe or fail deliberate attempts, and prove ordinary excluded discovery resolves none while the pure managed fake remains selected.

- Audit log cannot be written or is missing (D2 / D5): Attempt/gate fails loudly; absence is not zero loads. Accepted ordinary gate requires an existing empty untruncated log and complete terminal project results.

- The twenty known-red identities change (D2 / D5 A05): An added, removed or swapped identity fails the exact-set gate, even with twenty total failures. No automatic expansion, silent exclusion, partial run or promotion of the comparator into the set.

- Windows autocrlf checkout (D4 A04): Validate preserved dirty bytes by hash, and restored bytes via Git-aware clean comparison. CRLF mock, identity and index controls precede restoration; unexpected bytes stop without overwrite.

- Interruption after one host or candidate write (D1/D4/D5 A03): Unique candidate evidence and accepted manifest stay isolated; restore resumes only verified untouched paths. Retain originals, stop before Phase E on a failed gate, and never reapply the dirty patch automatically.

- Low-clock fixture and long autostart prelude (D1/D5 N07/N23): Document the prelude+W exception, consume all required permits, observe load,0 and exactly W cycles, then bounded limit/quit with retained tail and reason. Reject overflow, odd half-cycle request, prelude-only exit and indefinite wait.

- Native reader succeeds on MAINCPU but later fails (D1/D5 N02): A resume record followed by load,-1/reset signature is failure, never full resume. The outer wrapper observes the real full-load return before the next gate.

- Native writer returns positive 1 and removes candidate (D1/D5 new N28): Existing write-failure contract rejects publication and requires checkpoint-fail; add an explicit +1 control and record absence without a hash if no bytes remain.

Mandatory surfaces:

- Requested claims and live evidence: PASS. All 54 round-7 IDs freshly evaluated; 41 pins, native content/state, all20 historical hunks, fixture bytes, current callers and missing mechanisms verified. Two nonblocking added ACs are explicit.

- Workspace and operator rules: PASS. No marker/credential read, native mutation/build, nested reviewer, Python/Node/Bash automation, move, commit or push. PowerShell helpers analyzed/dry-run, isolated supported MCP logging, copy/retain restoration contracts, mock-first plan order and zero-allocation detector contract retained.

- Class1 requirements and mappings: PASS. Live wrapper mappings and pending states read; all named ACs/owners, explicit R15 semantic amendment, AC2 reference-only amendment, literal R12 and deferred managed acceptance are preserved.

- Holistic plan, dependencies, rollback and what-ifs: PASS. Both hosts, entry hold, full Slice1 map, trap/observer feasibility, correct comparator seam, exact20 exception, all requested what-ifs and nonpassing blocker lifecycle have observable outcomes.

Accuracy98: 98: live state, pinned source paths and changed contracts are independently supported. Residual universal-checkpoint/change-summary prose is disclosed as N27; no arbitrary-cycle feasibility or executed PhaseD success is claimed.
Completeness98: 98: all four mandatory surfaces, all54 prior IDs, all35 historical revision4 dispositions, every named Slice1 obligation and all requested what-ifs are covered. N28 strengthens an existing safe failure contract; future native both-host gates remain implementation work, not review omissions.

Persistence contract: verdict.persistence.json records complete ordered prose+JSON readback/reassembly hashes and the completed own-turn history query. It must show primaryPersisted=true, queued=false, fullContentVerified=true, status=completed. Request stream: /home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T185200-plan-nativerestore-001-r8-gpt61sol/request.jsonl. Response stream: /home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T185200-plan-nativerestore-001-r8-gpt61sol/response.jsonl. The same56 findings, both scores, complete prose and complete JSON are stored on the reviewer turn. The final persistence receipt discloses raw versus LF-normalized hashes; no queued write is counted as persisted.

Process disclosure: add-profile read all1 global profile files in full and was reloaded after compaction; the marker was left exclusively to the plugin. Linux PowerShell routing followed the explicit profile exception. The plugin requires UTC-shaped session/request IDs; user-facing reports use measured local time. Its generic codex header is not independent proof of the exact model; request.jsonl pins the authorized model/effort. Two historical default-root queued records were left untouched, with drain disabled throughout. One initial read-only helper had a singular-noun analyzer warning, corrected before further use; another local audit display initially projected dictionary keys as null and was corrected. Neither changed external state or verdict evidence. No commit, push, move, native build or native-source/materialization write occurred.
