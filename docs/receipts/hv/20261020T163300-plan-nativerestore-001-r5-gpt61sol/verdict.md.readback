2026-10-20T17:06:38 - Hostile validation round 5, PLAN-NATIVERESTORE-001 revision 5

**OverallVerdict: DISAGREE. Accuracy 97%; completeness 96%. PASS 40; FAIL 3; UNKNOWN 0; ADD-AC 4.**

The full FAIL list is N12 (external v1.3 acceptance input unallocated), N14 (R12 blocker contradicts the full execution/green exit), and N19 (false register-export exclusivity). The R12 literal value is now correct; F05, N08 and N09 pass as plan amendments. Execution remains held. These scores assess the plan holistically, not a pass-count ratio or implemented behavior.

Reviewer: Codex / gpt-6.1-sol / max, different family from the Claude implementer. Own session: Codex-20261020T213608Z-plugin-session. Own request: req-20261020T213610Z-nativerestore-r5. Parent session/request: ClaudeCode-20261020T015046Z-plugin-session / req-20261020T172116Z-prompt-6c6d.
Request: /home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T163300-plan-nativerestore-001-r5-gpt61sol/request.jsonl
Response stream: /home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T163300-plan-nativerestore-001-r5-gpt61sol/response.jsonl
Proof: /home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T163300-plan-nativerestore-001-r5-gpt61sol/verdict.persistence.json

**Identity, evidence and limits**

All 37 initial content/length pins match. The subject is revision 5, SHA256 657CC791953BA971164AE53CAD10803FF2ABB7996F721AB0315CEC920C2C9682, 66724 bytes. Checkout is develop at 72435f1acddd5c1c33ed3a721a9ba9da52a23082; the launch pin array itself contains no checkout commit. Native gitlink/HEAD is cc67418cba58e71d8e70fd353e9de47199ad2075. Initial live state is 8 modified plus 155 untracked native paths, 222 insertions and 8 deletions, clean index. Final integrity receipts bracket the review as described below.

- comparatorCommandReceipt: comparator-command.txt
- comparatorResult: Exit 1; failed 1, passed 0, skipped 0; missing reflected HalfCycleState at fixture line 23. This confirms the planned missing prerequisite, not a passing implementation gate.
- nativeExecution: None. No full solution build, native build, native binary launch, native/vice restore/clean/edit or artifacts/immutable-vice edit.
- sourceIdentity: Pinned materialization source Git blob hashes match the pinned commit; retained in pinned-sources.verified.json.
- nativeIntegrity: Initial Git status/pins precede diagnostics. The complete changed/untracked content inventory was captured later and rechecked at review end; it is not falsely described as a pre-test full-content bracket.
- persistence: Own supported plugin session uses MCP_FAILSAFE_DRAIN_DISABLED=1. Full prose and JSON are stored in ordered dialog chunks, reassembled and hash-verified before turn completion. Exact evidence is in verdict.persistence.json.

**Every round 4 finding, reverified against current sources**

**P01 PASS (info; round 4 PASS) - Input identity and drift**

Evidence: input-pins.json:1. All 37 initial file SHA256 and byte-length pins match input-pins.verified.json. Revision 5 is SHA256 657CC791953BA971164AE53CAD10803FF2ABB7996F721AB0315CEC920C2C9682, 66724 bytes. Live develop HEAD is 72435f1acddd5c1c33ed3a721a9ba9da52a23082.
Assessment: The request says revision 4 in one reading instruction, but the pinned subject identifies revision 5. The file-pin array does not itself record the checkout commit; the live checkout is recorded separately.
Correction/retained gate: Use revision 5 as the subject and retain both the initial and final integrity receipts; include explicit checkout metadata in future launch pins.

**P02 PASS (info; round 4 PASS) - Native starting state**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:7. live-state.json independently records gitlink and native HEAD cc67418cba58e71d8e70fd353e9de47199ad2075; 163 porcelain entries, comprising 8 tracked modifications and 155 untracked paths; 222 insertions, 8 deletions; native index clean.
Assessment: The Linux state claims match live Git. Current Windows dirt is a D0 measurement, rather than a claim that an October 4 receipt proves current remote bytes.
Correction/retained gate: Retain a separate fresh preservation inventory on each host before restoration.

**P03 PASS (info; round 4 PASS) - R1 B and retained output**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:42. .gitmodules:4 currently has ignore = dirty. R1 B, D4 and Exit require ignore = untracked, empty tracked/index diffs and exact retained untracked path/hash/length inventories.
Assessment: This preserves the explicit operator distinction between tracked code restoration and retained build output, including the Windows injected header.
Correction/retained gate: Keep the complete two-host content inventory and the approved ignore mode.

**P04 PASS (info; round 4 PASS) - Plan review authority and execution hold**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:77. D-pre requires the installation and Windows trace receipts, this plan at AGREE and an explicit operator implementation release. Handoff v2 section 11 records the clarification that plans are hostile-validated before implementation.
Assessment: This review is authorized. It does not release D0 or any native mutation.
Correction/retained gate: Keep the entry hold under this DISAGREE verdict.

**P05 PASS (info; round 4 PASS) - Both desktop installations**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:77. The pinned Linux 20261020T105400 and Windows 20261020T121000 installation READMEs record Nuke deployment/InstallMsi, installed version 1.3.111 and ProductVersion commit 2c8d60284229b9fedc2d50f840153b9e8d8cbcbf.
Assessment: Both required host installation receipts exist. This reviewer did not launch either GUI or claim a fresh remote installation measurement.
Correction/retained gate: Bind those exact host-attributed receipts to D0.

**P06 PASS (info; round 4 PASS) - Phase C evidence scope**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:13. The S2b/S4 Linux and 20261020T102600 Windows receipts support seeded reset-workload observer equivalence at 100k, 1M and x64sc 4M. PLAN-OBSERVER-001 S5 still allocates the snapshot register cross-check separately.
Assessment: Bounded reference traces establish the stated precondition, without proving managed parity or snapshot acceptance.
Correction/retained gate: Retain the seed, workload, original streams and host matrix in the D5 rerun.

**P07 PASS (info; round 4 PASS) - Preservation and retirement authority**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:78. D0 copies and hash-verifies preservation bytes and retains originals. D4 restores only named tracked paths. R2/R3 retire the named old builders; R4 retains untracked content; A03 prohibits automatic reapply or deletion.
Assessment: The proposed operations honor the operator scope and never-move rule. None ran during this review.
Correction/retained gate: Require the fresh committed preservation receipt and operator release before real overwrites.

**F01 PASS (medium; round 4 PASS) - Counts and historical inventory**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:8. All 2527 historical paths were parsed in historical-inventory.json: 1432 .Po, 745 .o, 59 .a, 2 .exe, 107 .in and 106 Makefiles. Live Linux Git returns 8 modified and 155 untracked paths.
Assessment: The old collapsed-directory count and extension errors remain corrected; the historical Windows counts are not asserted as live state.
Correction/retained gate: Recompute the host inventory in D0 rather than treating the historical list as a current remote receipt.

**F02 PASS (high; round 4 PASS) - Native build caller removal**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:80. TestHarness.csproj:10 and :68-74 define the ViceNativeProject property and Windows BuildNative invocation. Vice.Native.proj:5,14,32-39 owns that in-tree build. Scoped live searches confirm the caller and absence of the named Nuke targets and generator.
Assessment: D2 removes the actual executable path and checks build logs on both hosts; it no longer assumes a nonexistent Nuke target is the caller.
Correction/retained gate: Remove or make inert the obsolete csproj target consistently, and retain the no-reference receipt before retiring the project.

**F03 PASS (high; round 4 PASS) - Detector and semantic consumer ownership**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:80. ViceNative.cs:29-71 and ViceNative.Xvic.cs:31-48,141-153 have no current audit. D2 now implements both resolver/create paths with fresh-process positive, fallback and failed-load controls; R6 inventories direct, helper and provider consumers per method.
Assessment: The plan owns the missing detector rather than claiming current availability strings prove no loads. Discovery and hot-path measurement need the additional controls N16/N17.
Correction/retained gate: Implement the stated mocks first and retain the actual gate log on both hosts.

**F04 PASS (high; round 4 PASS) - All twenty hunks classified**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:19. The complete 599-line preserved patch has 20 hunk headers across 12 files. historical-inventory.json enumerates every header. Plan H1-H10 and H12 cover them; H11 covers the separate injected header/config flags.
Assessment: No tracked hunk is omitted. H5/H6/H9 are rejected corrections under R7; VIC-20 audio remains allocated to Phase F item 6. R14 separately disposes the rotation dead read.
Correction/retained gate: Re-derive the map from each fresh host patch and block unclassified or required-uncovered hunks.

**F05 PASS (high; round 4 FAIL) - R12 literal checkpoint value is preserved**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:61. R12 and D5 now require trigger 100000, callback write clock 100000 on both hosts, and equal written MAINCPU clock; otherwise BLOCKER-R12-XVIC-CHECKPOINT. There is no re-arm at a discovered boundary. Active 6510dtvcore.c:354-412 services IRQ/NMI before EXPORT_REGISTERS and the trap.
Assessment: Revision 5 honors the repeated literal ruling and distinguishes the safe exported-register boundary from an arbitrary interior cycle. This is a corrected acceptance contract, not a claim that immutable VICE can meet it. The downstream blocker exit conflict is N14.
Correction/retained gate: Keep literal 100000, measured trigger/write evidence and the named blocker; repair the blocked exit without substituting a later clock.

**F06 PASS (high; round 4 PASS) - Comparator prerequisite allocated to D2**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:62. The authorized Release no-build/no-restore comparator filter reproduced 1 failed, 0 passed, 0 skipped at Vic20HalfCycleComparatorTests.cs:23. The reflected HalfCycleState and CompareHalfCycleStates definitions are absent; R13 B explicitly implements them in D2.
Assessment: The comparator is an authorized Phase D prerequisite outside ShimLegacy and outside the R9 exception. Its assertions remain unchanged.
Correction/retained gate: Prove every reflected property/type, enum value, mismatch member, message and first-difference ordering with mock and real cases before the D2 gate.

**F07 PASS (high; round 4 PASS) - Restore drift and interruption checks**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:82. D4 checks repository, host, gitlink, clean index and preserved live bytes, or Git-clean restored state, before each overwrite. It reports restored, untouched or unexpected paths and resumes only untouched paths.
Assessment: Staged drift, modified preserved input, partial completion and corrupt or missing receipts have observable stop outcomes before real restoration.
Correction/retained gate: Run the listed mock controls and happy/resume paths before either host overwrite.

**F08 PASS (high; round 4 PASS) - Content-aware nonmutation bracket**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:78. D0 hashes every modified tracked and untracked path with length. D2/D4/D5 and Exit compare relevant complete inventories. A changed-existing-untracked-file negative control is explicit.
Assessment: Path-list equality alone can hide changed generated or injected bytes; the revised content bracket closes that hole.
Correction/retained gate: Retain all content hashes and producing-command inventories, including hidden paths.

**F09 PASS (medium; round 4 PASS) - Script analyzer and dry-run ownership**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:79. The current builder:26 and equivalence script:20 lack SupportsShouldProcess and create output directories. D1 adds no-file/no-directory WhatIf Pester controls and analyzer-zero gates before their real D5 runs.
Assessment: The required capability is allocated before use, rather than assumed available in the current scripts.
Correction/retained gate: Prove WhatIf also launches no native process; retain analyzer output and real commands.

**F10 PASS (medium; round 4 PASS) - Correct comparator mapping and pending managed gate**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:62. R13 B corrects the comparator summary to FR-SNP-005 AC3 / TEST-SNP-002 AC4 and preserves assertions. D5:93, Exit:95 and section 5:102 keep managed TR-CYCLE-001 AC4 and PLAN-VICEORACLE-001 task 5 pending.
Assessment: The small state-level comparator fixture and native PHI2/AEC evidence do not claim the deferred managed cycle acceptance or complete snapshot import.
Correction/retained gate: Keep the full managed obligation pending through Phase E and the final acceptance gate.

**F11 PASS (medium; round 4 PASS) - AC2 reference amendment and blast radius**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:79. The live canonical TR AC2 still names build-vice-shim.ps1. D1 explicitly changes that reference through the requirements wrapper to build-vice-observer.ps1 while retaining pinned clean materialization, original-source hashes, generated-output inventory and external wrapper units.
Assessment: The reference substitution can preserve AC2 semantics. It does not authorize silently reducing AC1 or the literal R12 checkpoint.
Correction/retained gate: Retain exact AC2 before/after readback and regenerate canonical projections through supported tools.

**R01 PASS (high; round 4 PASS) - Current reviewer credential handling**

Evidence: response.jsonl:1. No marker or credential file was read, printed or copied by this reviewer. Supported plugin operations use receipt-local PowerShell helpers; the plugin internally consumes its marker. Old raw review streams containing the round 1 exposure were not read.
Assessment: The credential incident is not repeated. This review makes no claim about historical key rotation.
Correction/retained gate: Keep the explicit credential prohibition and the old private stream outside publication.

**U01 PASS (high; round 4 PASS) - Own primary persistence and completed-turn proof**

Evidence: verdict.persistence.json:1. Own bootstrap, begin, observations and decisions returned persisted:true, queued:false, degraded:false. The final persistence proof retains complete prose and JSON readback hashes and the exact completed request from the supported history query.
Assessment: Final PASS depends on that completed proof. Two historical pending failsafes were reported by Status and were never replayed or canceled; they are not counted as persisted review content.
Correction/retained gate: Retain the complete ordered-content verification and exact own completed-turn status.

**U02 PASS (medium; round 4 PASS) - Effective store and mappings**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:78. Fresh supported requirements.effective/listMappings and todo.get receipts are retained in store-*.txt and store-effective.selected.json. All seven selected FR/TR/TEST records are pending with unsatisfied ACs; tasks 4 and 5 remain false.
Assessment: The current store confirms the obligations and traceability links. No requirement or TODO was mutated by this review; no remote Windows store readback is claimed.
Correction/retained gate: Read back effective host-layer mappings in D0 and preserve pending acceptance states.

**A01 PASS (medium; round 4 PASS) - Fresh host hunk disposition**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:78. D0 maps each fresh host hunk, records explained host-context differences, and blocks unclassified or required-uncovered content. Missing-hunk and wrong-host controls are named.
Assessment: A historical superset patch does not force current hosts to have byte-identical dirt, and the plan supplies an observable outcome for a difference.
Correction/retained gate: Bind each disposition to the host-specific preservation hash.

**A02 PASS (medium; round 4 PASS) - Entry evidence and explicit release**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:77. EntryEvidence_BindsBothHostsAndExplicitRelease requires both installed commit/version receipts, Windows trace results, plan AGREE and explicit operator release before D0.
Assessment: Neither receipt arrival nor this review independently authorizes implementation.
Correction/retained gate: Do not execute Phase D under this DISAGREE verdict.

**A03 PASS (medium; round 4 PASS) - Post-restore failure retention**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:82. D4 stops before Phase E on a later failure, retains all originals and receipts, reports each host path state and leaves restored source pinned. It prohibits automatic old-patch reapply and untracked deletion, with first-host and both-host failure controls.
Assessment: The failure recovery state is safe and auditable. This does not resolve the distinct complete-with-blocker contradiction in N14.
Correction/retained gate: Retain the stopped state and obtain scoped operator authority for any recovery mutation.

**N01 PASS (high; round 4 PASS) - R10 three-clock launch precondition**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:56. The approved fixture has MAINCPU 1.2, clock 109163946, PC 22377, P A0 and DRIVE8 2.0 payload 0,1. Pinned drive-snapshot.c:540-550 skips the three clock copies for type NONE; drive-resources.c:243-246 sets NONE and disables the unit.
Assessment: R10 addresses the specific uninitialized clock-copy path while preserving the approved fixture. It does not erase the separately accepted rotation dead read or resource/media side effects.
Correction/retained gate: Retain final nonoverridable flags, payload validation and post-load drive/media/continuation receipts.

**N02 PASS (high; round 4 PASS) - Ordered CPU-module and full-load success**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:90. Both CPU-reader calls cross object boundaries from c64-snapshot.c:121 and vic20-snapshot.c:129; machine_read_snapshot crosses from autostart.c:887 to each machine wrapper. Later reader failures reset at c64-snapshot.c:146-153 / vic20-snapshot.c:158-165. D5 orders resume, load,0, then gate clock+1 and permits stolen/idle cycles before the first PC read/dummy.
Assessment: The plan distinguishes CPU-module success from full-load success before the next gate. It rejects later failures and unrelated reset signatures without requiring an unavailable opcode-fetch wire tag.
Correction/retained gate: Bind the ordered positive and rejected cases to both real observer links; AC1 still requires complete post-resume behavior.

**A04 PASS (medium; round 4 PASS) - Repository identity and Windows checkout conversion**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:82. D4 verifies root, submodule gitdir, host, gitlink and clean index. Already-restored files are recognized with Git diff --exit-code, rather than raw LF-blob comparison; Windows CRLF interruption/resume is a named mock.
Assessment: Git applies the actual host checkout conversion. Preservation-copy hashes still compare exact host bytes and unexpected edits stop restoration.
Correction/retained gate: Keep identity, CRLF, staged-drift and partial-resume controls before real overwrites.

**A05 PASS (medium; round 4 PASS) - Selected identities and full execution**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:80. D2/D5 retain selected IDs and terminal TRX per project; require zero skipped, aborted and not-executed cases and exactly the named R9 failures; reject an equal-sized swapped set, missing project, discovery drift and partial run.
Assessment: The stable identity gate exposes both improvement and regression in the known-red set. Its normal unblocked branch is explicit; the new blocked branch conflicts with it as N14.
Correction/retained gate: Stop on any known-red set change and disposition it explicitly instead of replacing the baseline.

**A06 PASS (medium; round 4 PASS) - Up-to-date builder rerun**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:79. The live Make stage:220-231 depends on current stdout and fails without link lines on an up-to-date tree. D1 requires hash-verified reuse of retained stock-link-lines.json or a named prerequisite before replacing evidence; D5 reruns All on both hosts.
Assessment: The rerun gap has an implementation owner and safe evidence-retention outcome, including interrupted stage resume.
Correction/retained gate: Prove both mock cases before implementation and retain actual both-host rerun receipts.

**R02 PASS (medium; round 4 PASS) - Measured live timestamps**

Evidence: response.jsonl:1. Live progress objects and artifact timestamps are generated by PowerShell Get-Date when emitted. Percentages are numeric; ETA is an estimate, not a review deadline. Historical dates quoted from source remain attributed source data.
Assessment: No progress timestamp is estimated or backfilled. The supported plugin still uses UTC-formatted session/request identifiers under its own contract.
Correction/retained gate: Keep command-measured local progress through persistence and final output; raise the plugin identifier-format exception explicitly.

**N03 PASS (high; round 4 PASS) - R14 accepted benign dead read**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:63. drive-snapshot.c:315,394,433 passes a partly uninitialized rotation array. rotation.c:194 assigns speed_zone, then :203 overwrites it before use. R14 A expressly accepts that pinned dead read; D1 documents the scope and D5 retains both-host continuation comparison.
Assessment: The operator ruling resolves the prior uncertainty. It does not establish portable defined C behavior, sanitizer cleanliness or safety of the distinct live uninitialized clock reads.
Correction/retained gate: Keep that exact scope and executable continuation evidence, without broadening the exception.

**N04 PASS (medium; round 4 PASS) - Architectural registers and transparent forwarding**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:79. mos6510.h:116-136 stores N/Z separately and recomposes architectural P. D1 specifies P combinations, wide clock/PC, exactly-once real calls, preserved results/state, no resume record on reader failure, ordered load status, and stderr write/flush failure code 72.
Assessment: The observable contract matches the selected public seams on both machines. No unsupported per-cycle register export is claimed.
Correction/retained gate: Use the same assertions for doubles and both real links, with separate inert and armed-path evidence.

**N05 PASS (medium; round 4 PASS) - DRIVE payload, resources and media context**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:56. D0/D1 reject truncated or invalid has_tde/has_drives payloads and verify final NONE flags. D5 records type/TDE and image name or explicit no-media through resources_get_int and file_system_get_disk_name. drive-resources.c:158-162 detaches images; :273-275 registers the unit type pointer; iecbus.c:512-547 routes from resource state.
Assessment: The validation and post-load side effects now have owners and observable outcomes, including media detached by the precondition.
Correction/retained gate: Keep zero/out-of-range count rejection and both-host post-load media/continuation evidence.

**N06 PASS (medium; round 4 PASS) - Helper/provider-only consumers and managed fakes**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:49. R6/D0 enumerate plain Fact consumers, helper/provider-only calls, pure managed IViceNative fakes and string-only matches. Live semantic search includes SnapshotResumeSpikeTests and Vic20NativeLockstepTests missed by the old token-only inventory.
Assessment: The plan classifies native access per method rather than silently excluding an entire file or interface user. Candidate counts remain a search result, not a completed method inventory.
Correction/retained gate: Retain fully qualified method IDs and each Phase E/F rebind owner; add discovery coverage N16.

**N07 PASS (medium; round 4 PASS) - Absolute resume budget and complete interval**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:79. initcmdline.c:279-286 assigns an absolute clock limit; mainc64cpu.c:813-818 and mainviccpu.c:640-645 check > after an instruction. D1/D5 require fixture clock plus continuation, a prelude-safe budget, gate permits, a nonempty complete window and rejection of prelude-only/early/truncated/reordered output.
Assessment: The plan no longer infers exact termination from limitcycles or accepts a short clock/PC prefix as full resume proof. Unit conversion and low-clock feasibility deserve N18.
Correction/retained gate: Prove the prelude precondition and full window on both hosts, retaining instruction overshoot separately.

**N08 PASS (high; round 4 FAIL) - D1 contract, D2 selection and D5 real binding**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:79. D1 creates every named Slice 1 family with mock/recorded inputs and Category=VsfSlice1Contract. D5 adds same-case real tests in the same classes with Category=VsfSlice1Real and retained fully qualified IDs. D2 explicitly selects Contract and no Real case; D5 reads only artifacts produced in that run.
Assessment: This closes the prior future-artifact dependency without skips, stale fixtures or silently mock-only acceptance. A05 retains the boundary denominators.
Correction/retained gate: Keep the same assertions and per-project/category/ID receipts at both boundaries.

**N09 PASS (high; round 4 FAIL) - Tasks 4 and 5 remain pending inside Phase D**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:95. Exit:95 and section 5:102 attach executed evidence while leaving both tasks pending; task 4 done-state is allocated to step 12 after AGREE, and task 5 follows Phase E and the final gate. D5:93 also preserves managed AC4 as pending.
Assessment: The old requirement to mark task 4 inside Phase D has been removed. The D3 final-review hold no longer cycles through that done-state.
Correction/retained gate: Propagate the pending state into actual TODO checkpoints and do not use Phase D reference evidence to satisfy task 5.

**N10 PASS (medium; round 4 ADD-AC) - Marker-relative windows and observational history**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:90. obs-emitter.c:23-31,49-63 keeps process-local cycle_index and ba_low_run. D5 now starts each origin at its own checkpoint/load marker, retains raw indices, compares from the first half-cycle and rejects shifts, discarded warmup, stale history and reused/unequal prelude clocks. AEC must match for a BA-low/stolen first access.
Assessment: The round 4 observable AC is incorporated. This is a required implementation result, not an assertion that current observer-local history is snapshot-restored.
Correction/retained gate: Prove the same full-window assertions with mock histories and both real binary streams; stop on mismatch rather than trim.

**N11 PASS (medium; round 4 ADD-AC) - Detector initialization, failure and inert contract**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:80. D2 explicitly covers both classes, resolver/create paths, fresh xvic-first/x64-first processes, aliases/fallbacks and failed loads. Unwritable logging fails loudly; a missing retained gate log fails; positive logs stay separate; unset logging has no work/allocation and stepping is unchanged.
Assessment: The requested outcomes are now stated at D2. Existing managed determinism tests alone do not substantiate the no-allocation assertion, addressed by additive N17.
Correction/retained gate: Retain the fresh-process log controls and add direct diagnostic hot-path discrimination.

**N12 FAIL (high; round 4 ADD-AC) - Synthetic v1.3 labeling does not allocate the external v1.3 acceptance input**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:90. D5:88 now covers the full per-line schema/edge ledger and :90 records effective media identity. However :90 calls the obligation real v1.2/v1.3 resume while providing only a synthetic v1.3 layout derived from v1.2 unless the operator later supplies a real file. TR-VSFLOCKSTEP-RESUME-001 AC1 requires external v1.2 and v1.3 fixtures; Slice 1:340 distinguishes synthetic layout cases; section 5:102 says no AC is reduced or reworded.
Assessment: The ledger and honest synthetic label pass, but the original external-v1.3 provenance obligation remains unallocated before real acceptance. A derived layout may be useful native resume coverage; labeling it synthetic alone does not establish the unchanged external-fixture acceptance claim or authorize substituting it.
Correction/retained gate: Pin an actual external v1.3 input and provenance before claiming that acceptance, or leave that obligation explicitly blocked. If synthetic substitution is intended, obtain an explicit semantic amendment and read it back through the requirement wrapper; do not claim AC1 unchanged.

**N13 PASS (medium; round 4 ADD-AC) - Per-host budget and window evidence**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:90. D5 records per fixture/host the three-second autostart prelude, exact positive half-cycle interval, gate permits/protocol and full window. It includes low-clock and high-restored-clock positives and overshoot, prelude-only, overflow and truncated-baseline negatives, with tail retained outside comparison.
Assessment: The round 4 budget/window AC is incorporated. N18 adds explicit unit and infeasible-budget outcomes rather than treating this as proven runtime behavior.
Correction/retained gate: Keep every required sample and retain the tail and final termination reason.

**New findings and observable acceptance criteria**

**N14 FAIL (high) - The R12 blocked branch contradicts D5 and A05**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:95. R12 and D5:86-87 correctly withhold xvic fixtures if either host writes after 100000. Yet D5:92 says a missing fixture is an assertion failure; :93 runs full Nuke Test under A05 with only the R9 twenty failures; Exit:95 requires every Slice 1 obligation green and also permits Phase D complete-with-named-blocker. A03 stops before Phase E on a later failure.
Assessment: For host clocks 100000/100001 there is no consistent accepted path: executing required real cases adds failures beyond R9; omitting them violates A05; calling the phase complete violates the all-green exit. A named receipt alone does not define the blocked denominator or whether Phase E is prohibited. This is a new gate-chain conflict, not a request to relax literal R12.
Correction/AC: Define an explicit BLOCKED, nonpassing Phase D exit for this named blocker. Retain each dependent obligation/case ID as blocked, keep the cumulative target and full acceptance pending, prohibit Phase E and all done-state claims, and execute/report independent obligations with their separate denominator. Add D1 mock and D5 real classification controls for one-host and both-host checkpoint mismatch; never convert blocked cases to pass or skip.

**N15 ADD-AC (medium) - D1/D5 CheckpointGate_ArmedPathPreservesPermissions**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:79. bus-adapter.c owns the clock wrap and forwards to the real gate; clock-gate.c publishes one record then consumes a permit and handles EOF/quit. D1 tests trap registration/write timing and unset inertness; the equivalence reruns explicitly have no armed snapshot.
Assessment: No-snapshot equivalence cannot prove that the newly armed wrapper forwards the gate exactly once or retains hold/quit/error behavior. This is an additional observable test contract, not an observed native regression.
Correction/AC: Before implementation, add passing mock controls and D5 real cases on both machines proving one gate record/permit per advanced cycle, no advance while a permit is withheld, unchanged quit/EOF/error outcome, and independently reported trigger/write clocks under pending IRQ/NMI and stolen-cycle requests.

**N16 ADD-AC (medium) - D0/D2 ShimAudit_CoversDiscoveryBeforeFiltering**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:80. ViceFactAttribute.cs:17 and ViceTheoryAttribute.cs:17 query ViceNative during attribute construction. R6 includes helper/provider-only consumers, but the plan does not explicitly retain the audit interval through discovery before trait filtering.
Assessment: A method can be excluded at execution while discovery or its data provider still attempts a load. Trait coverage alone is not runtime proof of zero attempts.
Correction/AC: Start the retained audit before discovery and prove with fresh-process attribute/provider-only controls that attempts are prevented or cause an observable gate failure; do not truncate the log after discovery. Preserve the pure managed fake as selected and keep positive control logs separate.

**N17 ADD-AC (medium) - D2 NativeLoadAudit_UnsetPathHasNoIncrementalHotPathCost**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:80. D2 asserts no unset logging work/allocation and cites existing determinism tests. SidDeterminismTests:10-26 concerns the managed SID and is Category=Determinism, excluded by the current Nuke Test filter. The existing native audio allocation check is a different operation and will be ShimLegacy.
Assessment: A correct required result is stated, but the cited evidence cannot discriminate a diagnostic allocation or logging call in native step paths. No current allocation regression is alleged.
Correction/AC: Add a focused seam-level test with unset audit state, warmed repeated step calls for both native bindings, allocation/log-call measurements and unchanged deterministic output. Deliberately allocating/logging doubles must fail the assertion. Measure zero incremental diagnostic cost rather than asserting that already-allocating instance construction allocates nothing.

**N18 ADD-AC (medium) - D1/D5 ResumeBudget_UsesExplicitUnitsAndPreludePrecondition**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:90. D5 describes the required window in half-cycles while -limitcycles uses maincpu clock cycles; obs-emitter emits two half-cycle records per clock. A short window after a low-clock snapshot can imply an absolute limit below the three-second prelude, while the text forbids such a limit.
Assessment: The required positive/negative matrix is now present, but a fresh executor still needs an observable outcome for unit conversion and an infeasible prelude-safe budget. This need not prescribe a new protocol.
Correction/AC: State and test CPU-clock versus half-cycle conversion, odd-window rounding, overflow and permit counts. The low-clock positive must actually load and supply the entire window; a budget reached in the prelude is rejected before acceptance with its reason. Retain exact tail and termination reason without changing the required comparison interval.

**N19 FAIL (low) - The register-export exclusivity statement is false**

Evidence: docs/plans/PLAN-NATIVERESTORE-001.md:14. The plan calls the exported trap the only such point apart from JAM and monitor entry. In the active pinned 6510dtvcore.c, EXPORT_REGISTERS also appears at :451 in IK_DMA handling and :1012 in BRK, in addition to :410 at the trap.
Assessment: The selected public trap seam is still supported. The absolute source claim is incorrect and should not serve as a proof that all other exported boundary states are absent.
Correction/AC: Narrow the statement to the verified public trap seam used by this design, with its instruction-boundary and pending-interrupt limits. Do not infer arbitrary-cycle checkpoint capability or propose a native change from the other export sites.

**Requirements and whole-plan assessment**

- Class 1 implementation-plan review: all four hostile surfaces apply; this review itself makes no implementation or done-state claim.
- TR-VSFLOCKSTEP-RESUME-001 AC1: real legacy resume and current round trip remain D5 obligations, with the external-v1.3 gap N12. AC2: only the named builder reference changes through the wrapper; clean materialization and immutability semantics remain.
- TEST-VICE-001 AC1 and AC2 remain clean-target preflight obligations on both hosts. Native source and build output outside the approved materialization remain immutable.
- TR-CYCLE-001 AC4 is managed and stays pending. REAL-C64-PHI2-AEC-001 is native reference evidence, not managed satisfaction.
- FR-SNP-005 AC3 / TEST-SNP-002 AC4 are the comparator fixture mapping; the fixture does not prove full state import. R13 owns the required missing implementation in D2.
- PLAN-VICEORACLE-001 tasks 4 and 5 remain pending; evidence is attached in D5, task 4 done-state is after step 12 AGREE, task 5 after Phase E and final acceptance.
- Slice 1 lines 230-243 and 325-350 retain pinned commit/build, full schema and edges, ROM/media hashes, x64sc and literal xvic fixtures, byte-deterministic generation, native prerequisite failures, existing 199 adapter cases, real bindings and the cumulative target. The R8 observer substitution is explicit; it does not waive R12 or missing external inputs.

The trap callback has a complete exported architectural register file at the dispatch boundary; both machine writers are public cross-object calls and write MAINCPU at that boundary before later modules. The load observer wraps both CPU-module and machine-load calls, allowing a later failure/reset to be distinguished before the next gated cycle. Emitted VICE_SNAP lines intentionally occur only on snapshot paths: the no-snapshot equivalence rerun must emit none, and snapshot-bearing runs make their own resume/round-trip claim rather than a byte-identical stderr equivalence claim. No immutable-source change is needed or authorized for these seams.

The exact 100000 capability has not been executed here. Pending interrupts and stolen cycles can delay the callback; revision 5 correctly blocks instead of discovering a substitute. The conflict is the branch after blocking: independent work may finish, but the phase cannot honestly be a completed all-green Slice 1 gate. A nonpassing BLOCKED receipt, retained dependent case IDs, cumulative acceptance pending and no advance to E resolve the chain without changing R12.

**Structured what-if pass**

- Checkpoint clocks differ between hosts, for example 100000 and 100001 (D1/D5 and Exit; F05,N14): Correctly withhold the xvic fixture and name BLOCKER-R12-XVIC-CHECKPOINT; the downstream blocked/full-gate contradiction is FAIL N14.
- Trap fires after a pending IRQ/NMI (D1/D5; F05,N15): Active core services the interrupt before the trap, so write clock may exceed trigger. Revision 5 measures both and never re-arms at the later value. Real capability remains to be executed.
- Checkpoint request occurs inside a stolen-cycle run (D1/D5; F05,N15): Trap registration does not manufacture a complete architectural register file at that interior cycle; only the measured callback boundary is eligible. A write other than literal 100000 blocks xvic acceptance.
- Resume first access is stolen or BA-low (D1/D5; N02,N10): Permit the idle/stolen prefix, then require PC read/dummy, ordered load success, every half-cycle and independently correct AEC history from the first sample; no trimming or shifted comparison.
- Shim consumer is reachable only from a data provider or discovery attribute (D0/D2; N06,N16): R6 classifies it semantically. N16 adds proof that the audit includes discovery before filtering and no attempt is hidden by log truncation.
- Load-log path is unwritable or the retained gate log is missing (D2; N11): D2 explicitly fails loudly/fails the gate. Positive probes use separate logs and cannot manufacture an empty negative log.
- Known-red twenty changes, including improvement or an equal-sized swapped set (D2/D5; A05): A05 rejects any identity change, missing project or partial run; the executor must report and disposition the change instead of silently refreshing the baseline.
- Windows uses autocrlf and restoration is interrupted (D0/D4; F07,A04): Compare preserved host bytes exactly; recognize an already clean restored worktree through Git diff, not LF blob bytes; unexpected or staged drift stops. Resume touches only verified untouched paths.
- Crash/restart after first or second host restore, or after later gate failure (D4/D5; A03): A03 retains originals and preservation receipts, reports per-host path states, leaves restored source pinned and stops before E. No automatic reapply, delete or move is authorized.
- Wrong workspace, host, gitlink or corrupt preservation input (D0/D4; A01,F07,A04): A04/F07 stop before overwrite; host-specific receipts and hashes prevent cross-host or cross-workspace reuse.
- Real D5 artifacts do not yet exist when D2 runs (D1/D2/D5; N08): Only Contract same-case bindings exist in D1/D2; Real bindings and newly produced artifacts are allocated to D5, without skips or stale host paths.
- CPU module loads but a later snapshot module fails (D1/D5; N02,N04): Ordered CPU resume is followed by load,-1 and reset; it is rejected. Full load,0 must precede the next gate; two matching fields cannot stand in for complete resume.
- Low restored clock plus short continuation is below autostart prelude, or budget overflows (D1/D5; N07,N13,N18): N13 requires positive and negative controls; N18 makes units and prelude-safe feasibility explicit. No prelude-only exit or trimmed baseline can pass.
- No actual external v1.3 fixture is available (D1/D5 requirements binding; N12): Synthetic layout coverage is labeled honestly, but cannot silently stand in for the unchanged external-fixture acceptance obligation. Allocate an explicit blocked outcome or an operator-approved semantic amendment.
- Armed checkpoint gate is held, quit, reaches EOF or fails (D1/D5; N15): The current no-snapshot equivalence is insufficient; N15 requires exact forwarding/permit and terminal outcomes on the armed path.
- Unset load detector logs or allocates during repeated native steps (D2; N17): The stated zero-incremental-cost contract must discriminate the violation through focused allocation/log-call controls; managed SID self-consistency does not prove it.

**Reviewer process disclosures**

- add-profile ran at initial entry and after context recovery; every non-skill profile markdown file was read in full (one global file each load). Private profile details are not reproduced.
- Only receipt-local PowerShell scripts and read-only Git/search were used for review. Scripts were parser/analyzer checked and WhatIf-tested; no nested reviewer, model substitution, commit, push, file move or deletion occurred.
- The initial analyzer invocation passed an unsupported path array; it was corrected to directory plus Recurse, and all scripts were rechecked. This was a reviewer command error, not evidence of a passing check.
- An initial detailed-history query used a nested query object and returned a broader result than intended. It was corrected to flat agent/limit/offset parameters, and final proof filters the exact own session and request. No historical turn was modified.
- The fixture inspection helper initially used a 32-bit clock/register offset; it was corrected to the pinned 64-bit payload layout and rerun. Prior receipts are retained and the corrected receipt is identified.
- The plugin generated its own actual session ID despite a requested custom ID. The verdict uses the returned actual ID. The generic plugin model field is codex; the precise model/effort come from the supplied CLI launch receipt, not a fabricated server field.
- The plugin contract generates UTC-formatted session/request identifiers although profile filenames/progress use local sortable time. Those provider-generated IDs are retained accurately and the contract-format exception is disclosed.
- The request contains a stale revision-4 phrase and asks one legacy JSON field for revision-4 dispositions. This verdict reviews pinned revision 5, supplies all 41 round-4 dispositions, and additionally records revision-4 dispositions for each of the 35 round-3b IDs.
- No unsupported/untrusted plugin result occurred; the conditional triage/failsafe branch was not needed. Primary and historical queued state remain distinguished.

Accuracy 97% reflects the remaining acceptance/source inaccuracies. Completeness 96% reflects the contradictory blocked exit and the four additional observable ACs. Both are below the mandatory 98/98 bar. No phase, TODO, FR/TR/TEST or implementation gate is marked done by this review.
