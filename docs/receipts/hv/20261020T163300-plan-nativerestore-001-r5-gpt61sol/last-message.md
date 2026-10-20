2026-10-20T17:15:36 - Hostile validation round 5, PLAN-NATIVERESTORE-001 revision 5

**OverallVerdict: DISAGREE. Accuracy 97%; completeness 96%. PASS 40; FAIL 3; UNKNOWN 0; ADD-AC 4.**

The full FAIL list is **N12**, **N14**, and **N19**. F05, N08 and N09 now pass as plan amendments. Execution remains held.

Saved: [complete prose verdict](/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T163300-plan-nativerestore-001-r5-gpt61sol/verdict.md), [complete JSON verdict](/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T163300-plan-nativerestore-001-r5-gpt61sol/verdict.json), [persistence proof](/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T163300-plan-nativerestore-001-r5-gpt61sol/verdict.persistence.json).

Reviewer: Codex / gpt-6.1-sol / max, different family from the Claude implementer.

- Reviewer session: `Codex-20261020T213608Z-plugin-session`
- Reviewer request: `req-20261020T213610Z-nativerestore-r5`
- Parent session: `ClaudeCode-20261020T015046Z-plugin-session`
- Parent request: `req-20261020T172116Z-prompt-6c6d`
- [Request JSONL](/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T163300-plan-nativerestore-001-r5-gpt61sol/request.jsonl)
- [Response JSONL](/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T163300-plan-nativerestore-001-r5-gpt61sol/response.jsonl)

The exact review turn is **completed** in fresh server history. All 17 readable content chunks are primary-persisted. Readback matches the complete prose, 45,009 bytes, and JSON, 53,338 bytes, byte for byte. No CRLF/LF normalization was needed. Historical queued records were neither replayed nor canceled.

**Identity, evidence and limits**

All 37 input content/length pins match before and after review. The subject is revision 5, SHA256 `657CC791953BA971164AE53CAD10803FF2ABB7996F721AB0315CEC920C2C9682`, 66,724 bytes.

The checkout is `develop` at `72435f1acddd5c1c33ed3a721a9ba9da52a23082`. Contrary to the brief, the input-pin array does not itself contain a checkout-commit pin.

Native gitlink and HEAD are `cc67418cba58e71d8e70fd353e9de47199ad2075`. Live state remains 8 modified tracked files plus 155 untracked paths, 222 insertions and 8 deletions, with a clean index.

- The authorized Release comparator filter returned exit 1: **1 failed, 0 passed, 0 skipped**, at the missing reflected `HalfCycleState`, fixture line 23. Exact command, stdout, TRX and diagnostics are retained.
- No full solution build, native build, native binary launch, native restoration, cleanup or source edit occurred.
- All 23 inspected materialized source files match their pinned Git blobs.
- Initial Git state and input pins precede diagnostics. The complete changed/untracked content inventory was captured later and rechecked at review end; it is not represented as a pre-test full-content bracket.
- Final verification found zero changes across those 163 native paths, no status changes outside this receipt directory, and `git diff --check` exit 0.
- Review scripts passed parser and analyzer checks with zero findings and were dry-run with `-WhatIf`.

**Every round 4 finding, reverified against current sources**

**P01 PASS — Input identity and drift**

Evidence: `input-pins.json:1`, `input-pins.verified.json`, and the final integrity receipt. All 37 file hashes and lengths match. The pinned subject identifies revision 5.

Assessment: The request contains a stale revision-4 reading instruction and incorrectly describes the pin array as containing checkout metadata.

Retained gate: Review revision 5 and retain the independently measured checkout identity and final integrity bracket.

**P02 PASS — Native starting state**

Evidence: plan line 7 and `live-state.json`. Native gitlink/HEAD match the pin; porcelain contains 163 entries, comprising 8 tracked modifications and 155 untracked paths. Diff statistics are 222 insertions and 8 deletions; the native index is clean.

Assessment: Linux claims match live Git. Current Windows dirt remains a D0 measurement; the October 4 receipt is historical evidence.

Retained gate: Obtain separate fresh preservation inventories on both hosts.

**P03 PASS — R1 B and retained output**

Evidence: `.gitmodules:4` currently says `ignore = dirty`. R1 B, D4 and Exit require `ignore = untracked`, empty tracked/index diffs, and exact retained untracked path/hash/length inventories.

Assessment: This preserves the operator’s distinction between tracked restoration and retained build output, including the Windows injected header.

Retained gate: Keep complete content inventories on both hosts and the approved ignore mode.

**P04 PASS — Plan-review authority and execution hold**

Evidence: D-pre, plan line 77, requires installation and Windows trace receipts, plan AGREE, and explicit operator implementation release. Handoff v2 section 11 authorizes plan hostile validation before implementation.

Assessment: This review is authorized. It does not release D0 or native mutation.

Retained gate: Keep the implementation hold under this DISAGREE verdict.

**P05 PASS — Both desktop installations**

Evidence: The pinned Linux `20261020T105400` and Windows `20261020T121000` installation READMEs record Nuke deployment/InstallMsi, version `1.3.111`, and ProductVersion commit `2c8d60284229b9fedc2d50f840153b9e8d8cbcbf`.

Assessment: Both required installation receipts exist. This reviewer did not launch either GUI or claim a fresh remote installation measurement.

Retained gate: Bind those host-attributed receipts to D0.

**P06 PASS — Phase C evidence scope**

Evidence: Plan line 13, the S2b/S4 Linux receipts, and the Windows `20261020T102600` receipt support seeded reset-workload observer equivalence at 100k, 1M and x64sc 4M. Observer-plan S5 allocates the snapshot register cross-check separately.

Assessment: These bounded reference results establish the stated precondition without establishing managed parity or snapshot acceptance.

Retained gate: Preserve seed, workload, original streams and host matrix in D5.

**P07 PASS — Preservation and retirement authority**

Evidence: D0 copies and hash-verifies preservation bytes while retaining originals. D4 restores named tracked paths. R2/R3 retire the named builders; R4 retains untracked content; A03 prohibits automatic reapply or deletion.

Assessment: The proposed operations honor the scoped authority and never-move rule. None ran during review.

Retained gate: Require fresh committed preservation receipts and implementation release before real overwrites.

**F01 PASS — Counts and historical inventory**

Evidence: Plan lines 8–9 and `historical-inventory.json`. All 2,527 historical paths were parsed: 1,432 `.Po`, 745 `.o`, 59 `.a`, 2 `.exe`, 107 `.in`, and 106 Makefiles. Live Linux state is 8 modified plus 155 untracked paths.

Assessment: Earlier collapsed-directory and extension-count errors remain corrected.

Retained gate: Recompute each host’s D0 inventory; do not treat historical Windows counts as current remote state.

**F02 PASS — Native build caller removal**

Evidence: TestHarness project lines 10 and 68–74 define `ViceNativeProject` and invoke Windows `BuildNative`. `Vice.Native.proj` lines 5, 14 and 32–39 own the in-tree build. Live searches confirm the caller and absence of the named Nuke targets and generator.

Assessment: D2 names the executable path that must be removed and requires both-host build-log evidence.

Retained gate: Remove or make inert the obsolete project target consistently; retain the no-reference receipt before retiring the project.

**F03 PASS — Detector and semantic consumer ownership**

Evidence: `ViceNative.cs:29–71` and `ViceNative.Xvic.cs:31–48,141–153` contain no current audit. D2 now owns both resolver/create paths, fresh-process positive/fallback/failed-load controls, and the retained gate log. R6 inventories direct, helper and provider consumers per method.

Assessment: The missing detector is explicitly planned. Discovery and allocation evidence receive additional controls in N16/N17.

Retained gate: Implement mocks first and retain actual both-host gate logs.

**F04 PASS — All twenty hunks classified**

Evidence: The complete 599-line patch contains 20 hunk headers across 12 files. The historical inventory enumerates every header. Plan H1–H10 and H12 cover them; H11 addresses the separate injected header/configuration.

Assessment: No tracked hunk is omitted. H5/H6/H9 remain rejected corrections under R7; VIC-20 audio remains Phase F item 6. R14 separately disposes the rotation dead read.

Retained gate: Re-derive each fresh host hunk map and block unclassified or required-uncovered content.

**F05 PASS — Literal R12 checkpoint preserved**

Evidence: Plan line 61 and D5 require trigger `100000`, callback write clock `100000` on both hosts, and matching written `MAINCPU` clock. Otherwise they raise `BLOCKER-R12-XVIC-CHECKPOINT`. Nothing re-arms at a discovered boundary. Active `6510dtvcore.c:354–412` services pending IRQ/NMI before exporting registers and dispatching the trap.

Assessment: Revision 5 honors the repeated literal ruling. It distinguishes trigger from write clock and does not claim immutable VICE can produce an arbitrary interior-cycle snapshot. The downstream blocker-exit conflict is N14.

Retained gate: Keep literal `100000`, measured trigger/write clocks and the named blocker. Repair the blocked exit without substituting another clock.

**F06 PASS — Comparator prerequisite allocated to D2**

Evidence: Plan line 62. The authorized comparator run reproduced 1 failed, 0 passed, 0 skipped at fixture line 23. The reflected `HalfCycleState` and `CompareHalfCycleStates` definitions are absent; R13 B explicitly implements them in D2.

Assessment: This prerequisite remains outside `ShimLegacy` and the R9 exception. The fixture assertions remain unchanged.

Retained gate: Prove every reflected property/type, enum value, mismatch member, message and first-difference ordering before D2 acceptance.

**F07 PASS — Restore drift and interruption checks**

Evidence: D4, line 82, checks repository, host, gitlink, index and preserved live bytes, or Git-clean restored state, before overwriting. It distinguishes restored, untouched and unexpected paths and resumes only untouched paths.

Assessment: Staged drift, modified preservation input, partial completion and corrupt/missing receipts have observable stop outcomes.

Retained gate: Run the listed negative controls and happy/resume cases before either host overwrite.

**F08 PASS — Content-aware nonmutation bracket**

Evidence: D0 hashes every modified tracked and untracked path with length. D2/D4/D5 and Exit compare complete relevant inventories, including a changed-existing-untracked-file negative control.

Assessment: The prior path-list-only blind spot is closed.

Retained gate: Preserve complete hashes and producing-command inventories, including hidden paths.

**F09 PASS — Analyzer and dry-run ownership**

Evidence: Current builder line 26 and equivalence script line 20 lack `SupportsShouldProcess` and create output directories. D1 adds no-file/no-directory `WhatIf` controls and analyzer-zero gates before real D5 runs.

Assessment: The required capability is allocated before use.

Retained gate: Also prove that `WhatIf` launches no native process; retain analyzer output and real commands.

**F10 PASS — Comparator mapping and pending managed gate**

Evidence: R13 B corrects the comparator summary to `FR-SNP-005 AC3` / `TEST-SNP-002 AC4`. D5 line 93, Exit line 95, and section 5 line 102 keep managed `TR-CYCLE-001 AC4` and task 5 pending.

Assessment: The state-level comparator fixture and native PHI2/AEC evidence do not claim complete managed cycle or snapshot acceptance.

Retained gate: Keep the managed obligation pending through Phase E and final acceptance.

**F11 PASS — AC2 reference amendment**

Evidence: The live canonical AC2 still names `build-vice-shim.ps1`. D1 changes that reference through the requirements wrapper to `build-vice-observer.ps1` while retaining clean pinned materialization, original-source hashes, generated-output inventory and external wrapper units.

Assessment: This reference substitution can preserve AC2 semantics. It does not authorize reducing AC1 or literal R12.

Retained gate: Preserve exact before/after AC2 readback and use supported projection generation.

**R01 PASS — Reviewer credential handling**

Evidence: Current response stream and receipt-local helpers. No marker or credential file was read, printed or copied by this reviewer. The supported plugin internally consumes its marker. Old raw streams containing the round 1 exposure were not read.

Assessment: The credential incident was not repeated. No historical key-rotation claim is made.

Retained gate: Maintain the explicit prohibition and keep the old private stream outside publication.

**U01 PASS — Primary persistence and completed-turn proof**

Evidence: Own wrapper operations returned `persisted:true`, `queued:false`, `degraded:false`. Fresh history confirms the exact completed reviewer request. Complete prose and JSON were reassembled and hash-verified.

Assessment: This is primary persistence. Two historical pending failsafes were never replayed or canceled.

Retained gate: Preserve exact full-content hashes and completed-request proof.

**U02 PASS — Effective store and mappings**

Evidence: Fresh supported `requirements.effective`, mappings and `todo.get` receipts are retained. All seven selected FR/TR/TEST records remain pending with unsatisfied ACs; tasks 4 and 5 remain false.

Assessment: Live storage confirms the obligations and traceability links. No requirement or TODO was mutated; no Windows store readback is claimed.

Retained gate: Read effective host-layer mappings in D0 and preserve pending acceptance states.

**A01 PASS — Fresh host hunk disposition**

Evidence: D0 maps each fresh hunk, records explained host differences, and blocks unclassified or required-uncovered content. Missing-hunk and wrong-host controls are named.

Assessment: The historical superset patch does not require identical current host dirt.

Retained gate: Bind each disposition to its host preservation hash.

**A02 PASS — Entry evidence and explicit release**

Evidence: `EntryEvidence_BindsBothHostsAndExplicitRelease` requires installed commit/version receipts, Windows traces, plan AGREE and operator release before D0.

Assessment: Neither receipt arrival nor this review independently releases implementation.

Retained gate: Do not execute Phase D under this DISAGREE verdict.

**A03 PASS — Post-restore failure retention**

Evidence: D4 stops before Phase E on later failure, retains originals/receipts, reports host path states and leaves restored source pinned. It prohibits automatic patch reapply and untracked deletion, with first-host and both-host failure controls.

Assessment: Recovery retains a safe, auditable state. The separate complete-with-blocker contradiction is N14.

Retained gate: Preserve the stopped state; recovery mutations require scoped authority.

**N01 PASS — R10 three-clock precondition**

Evidence: The approved fixture has `MAINCPU 1.2`, clock `109163946`, PC `22377`, P `$A0`, and `DRIVE8 2.0` payload `0,1`. `drive-snapshot.c:540–550` skips the three clock copies for type NONE; `drive-resources.c:243–246` sets NONE and disables the unit.

Assessment: R10 addresses the specific uninitialized clock-copy path while retaining the fixture. It does not erase the accepted dead read or resource/media side effects.

Retained gate: Preserve final nonoverridable flags, payload checks and post-load drive/media/continuation evidence.

**N02 PASS — CPU-module versus full-load success**

Evidence: CPU-reader calls cross objects from `c64-snapshot.c:121` and `vic20-snapshot.c:129`; `machine_read_snapshot` crosses from `autostart.c:887` to each machine wrapper. Later failures reset the machine. D5 orders `resume`, `load,0`, then gate clock+1, allowing idle/stolen cycles before the first PC read/dummy.

Assessment: CPU-module success is distinguished from complete load before the next gate. Later failures and unrelated resets are rejected.

Retained gate: Bind ordered positive/rejected cases to both real links; AC1 still requires complete continuation.

**A04 PASS — Repository identity and Windows conversion**

Evidence: D4 verifies root, submodule gitdir, host, gitlink and index. Git diff recognizes an already-restored checkout; raw LF blob bytes are not the predicate. Windows CRLF interruption/resume is a named mock.

Assessment: Git applies host checkout conversion while preservation hashes retain exact host bytes.

Retained gate: Keep identity, CRLF, staged-drift and partial-resume controls.

**A05 PASS — Selected identities and full execution**

Evidence: D2/D5 retain selected IDs and terminal TRX per project; require zero skipped, aborted and not-executed cases and exactly the R9 failures; reject swapped sets, missing projects, discovery drift and partial runs.

Assessment: The unblocked gate exposes both improvement and regression in the known-red set. N14 identifies the new blocked-branch conflict.

Retained gate: Stop and disposition any set change explicitly.

**A06 PASS — Up-to-date builder rerun**

Evidence: Current Make lines 220–231 require current stdout link lines. D1 requires hash-verified reuse of retained `stock-link-lines.json` or a named prerequisite before replacing evidence. D5 reruns All on both hosts.

Assessment: Up-to-date and interrupted-stage outcomes now have an owner and safe retention rule.

Retained gate: Prove mock cases first and retain actual rerun receipts.

**R02 PASS — Measured timestamps**

Evidence: Progress objects and artifact timestamps use command-measured PowerShell time. Percentages are numeric; ETA is an estimate, not a bound. Historical dates remain attributed source data.

Assessment: No progress timestamp is estimated or backfilled. Provider-generated session/request IDs retain the plugin’s UTC format.

Retained gate: Continue measured local reporting and disclose the provider identifier-format exception.

**N03 PASS — R14 accepted dead read**

Evidence: `drive-snapshot.c:315,394,433` passes the partly uninitialized rotation array. `rotation.c:194` assigns `speed_zone`; line 203 overwrites it before use. R14 A accepts that pinned path. D1 documents it; D5 requires both-host continuation comparison.

Assessment: The operator ruling resolves the prior uncertainty. It does not establish portable defined C behavior, sanitizer cleanliness or safety of live uninitialized clock reads.

Retained gate: Preserve that exact scope.

**N04 PASS — Architectural registers and forwarding**

Evidence: `mos6510.h:116–136` stores N/Z separately and reconstructs P. D1 specifies P combinations, wide clock/PC, exactly-once forwarding, preserved result/state, no resume record on reader failure, ordered load status, and checked stderr/flush failure code 72.

Assessment: The contract matches the public seams on both machines without claiming per-cycle register export.

Retained gate: Use identical assertions for doubles and real links, with separate inert/armed evidence.

**N05 PASS — DRIVE payload, resources and media**

Evidence: D0/D1 reject invalid/truncated `has_tde` and `has_drives` payloads and verify final NONE flags. D5 records type/TDE and image name or explicit no-media. The pinned setter detaches images and changes bus routing; the resource getters can observe effective values.

Assessment: Payload validation and post-load side effects now have observable outcomes, including detached media.

Retained gate: Preserve count validation and both-host post-load media/continuation evidence.

**N06 PASS — Helper/provider consumers and managed fakes**

Evidence: R6/D0 cover plain Facts, helper/provider-only calls, pure managed `IViceNative` fakes and string-only matches. Semantic search includes `SnapshotResumeSpikeTests` and `Vic20NativeLockstepTests`, missed by the old token inventory.

Assessment: Classification is per method. The 56-file candidate count is reproducible with the plan’s unescaped-dot search pattern; it is not a completed semantic inventory.

Retained gate: Preserve method IDs, rebind owners and discovery coverage N16.

**N07 PASS — Absolute budget and full interval**

Evidence: `initcmdline.c:279–286` defines an absolute clock limit. Both CPU loops test `>` after an instruction. D1/D5 require a prelude-safe fixture-clock-plus-continuation budget, permits, a nonempty full window, and rejection of prelude-only/early/truncated/reordered output.

Assessment: The plan no longer treats `-limitcycles` as exact termination or accepts a short clock/PC prefix as complete resume.

Retained gate: Prove the full window on both hosts and retain overshoot separately; N18 strengthens units.

**N08 PASS — D1/D2/D5 binding allocation**

Evidence: D1 creates every Slice 1 family with fake/recorded inputs and `Category=VsfSlice1Contract`. D5 adds same-case real bindings with `Category=VsfSlice1Real`, retained IDs and artifacts produced during that run. D2 explicitly contains Contract and no Real cases.

Assessment: This closes the future-artifact dependency without skips, stale fixtures or mock-only acceptance.

Retained gate: Preserve assertions and per-project/category/ID denominators at both boundaries.

**N09 PASS — Tasks 4 and 5 pending inside Phase D**

Evidence: Exit line 95 and section 5 line 102 attach evidence while leaving both tasks pending. Task 4 done-state is allocated to step 12 after AGREE; task 5 follows Phase E and final acceptance. D5 line 93 preserves managed AC4 as pending.

Assessment: The earlier done-state cycle is removed.

Retained gate: Propagate pending status into TODO checkpoints.

**N10 PASS — Marker-relative windows and history**

Evidence: `obs-emitter.c:23–31,49–63` retains process-local indices and BA history. D5 starts each origin at its checkpoint/load marker, preserves raw indices, compares from the first half-cycle, and rejects shifts, discarded warmup, stale history and reused prelude clocks. First-access derived AEC must match.

Assessment: The round 4 AC is incorporated. Current observer history is not claimed to be snapshot-restored.

Retained gate: Run identical mock and real window assertions; stop on mismatch.

**N11 PASS — Detector initialization and failure contract**

Evidence: D2 covers both classes, resolver/create paths, fresh xvic-first/x64-first processes, aliases/fallbacks and failed loads. Unwritable logging fails loudly; missing retained gate logs fail; positive logs remain separate; unset diagnostics have no logging/allocation and preserve stepping.

Assessment: Required outcomes are stated. N17 adds evidence that can substantiate the allocation assertion.

Retained gate: Preserve fresh-process controls and direct diagnostic-cost discrimination.

**N12 FAIL, high — External v1.3 acceptance input remains unallocated**

Evidence: D5 line 88 now covers the full per-line schema/edge ledger, and line 90 records effective media identity. However, line 90 calls the obligation real v1.2/v1.3 resume while supplying a synthetic v1.3 layout derived from v1.2 unless the operator later provides a real file. `TR-VSFLOCKSTEP-RESUME-001 AC1` requires external v1.2 and v1.3 fixtures. Slice 1 line 340 distinguishes synthetic layout cases. Section 5 line 102 says no AC is reduced or reworded.

Assessment: **The ledger and honest synthetic label pass.** The original external-v1.3 provenance obligation remains unallocated before real acceptance. A derived layout supplies useful native resume coverage, but its label does not establish the unchanged external-fixture acceptance claim.

Correction: Pin an actual external v1.3 input and provenance before claiming that acceptance, or leave the obligation explicitly blocked. If synthetic substitution is intended, obtain an explicit semantic amendment through the requirements wrapper; do not claim AC1 unchanged.

**N13 PASS — Per-host budget/window evidence**

Evidence: D5 records each fixture/host’s prelude, exact positive half-cycle interval, permits/protocol and full window. It includes low-clock and high-restored-clock positives and overshoot, prelude-only, overflow and truncated-baseline negatives, retaining tail evidence outside comparison.

Assessment: The round 4 budget/window AC is incorporated. N18 adds explicit units and infeasible-budget outcomes.

Retained gate: Check every required sample and retain the tail and termination reason.

**New findings and acceptance criteria**

**N14 FAIL, high — R12’s blocked branch contradicts D5/A05**

Evidence: R12 and D5 lines 86–87 correctly withhold xvic fixtures if either host writes after `100000`. D5 line 92 makes missing fixtures assertion failures. Line 93 runs full Nuke Test under A05 with only the R9 twenty failures. Exit line 95 requires every Slice 1 obligation green while also permitting “complete-with-named-blocker.” A03 stops before Phase E on later failure.

Assessment: For host clocks `100000/100001`, no consistent accepted path exists:

- Execute required real cases: failures exceed R9.
- Omit them: A05 is violated.
- Call the phase complete: the all-green exit is violated.

A named receipt does not define the blocked denominator or conclusively prohibit Phase E. This is a gate-chain conflict, not a request to relax literal R12.

Correction: Define an explicit **BLOCKED, nonpassing Phase D exit**. Retain each dependent obligation/case ID as blocked; keep the cumulative target and acceptance pending; prohibit Phase E and done-state claims; execute/report independent obligations with their separate denominator. Add D1 mock and D5 real classification controls for one-host and both-host mismatch. Blocked cases must not become pass or skip.

**N15 ADD-AC, medium — D1/D5 `CheckpointGate_ArmedPathPreservesPermissions`**

Evidence: `bus-adapter.c` owns the clock wrap and forwards to the real gate. `clock-gate.c` publishes a record, consumes a permit and handles EOF/quit. D1 tests trap timing and unset inertness; equivalence reruns explicitly have no armed snapshot.

Assessment: No-snapshot equivalence does not prove armed-path forwarding, hold, quit or error behavior. No runtime regression is alleged.

AC: Before implementation, add passing mock controls and D5 real cases on both machines proving one gate record/permit per advanced cycle, no advance while waiting for a withheld permit, unchanged quit/EOF/error outcomes, and distinct measured trigger/write clocks under pending IRQ/NMI and stolen-cycle requests.

**N16 ADD-AC, medium — D0/D2 `ShimAudit_CoversDiscoveryBeforeFiltering`**

Evidence: `ViceFactAttribute.cs:17` and `ViceTheoryAttribute.cs:17` query `ViceNative` during attribute construction. R6 covers provider consumers but does not explicitly retain the audit interval through discovery before filtering.

Assessment: An excluded execution method can still cause a discovery/provider load attempt. Traits alone do not prove zero attempts.

AC: Begin the retained audit before discovery. Fresh-process attribute/provider-only controls must prove that attempts are prevented or fail the gate observably. Do not truncate the log after discovery. Keep managed fakes selected and positive probes in separate logs.

**N17 ADD-AC, medium — D2 `NativeLoadAudit_UnsetPathHasNoIncrementalHotPathCost`**

Evidence: D2 cites existing determinism tests for no unset logging/allocation. `SidDeterminismTests:10–26` exercises the managed SID and is excluded by the current Nuke filter. The native audio allocation check concerns another operation and becomes `ShimLegacy`.

Assessment: The required result is stated, but cited tests cannot discriminate a diagnostic allocation or logging call in native step paths. No current regression is alleged.

AC: Add focused seam-level tests with unset audit state, warmed repeated steps for both bindings, allocation/log-call measurements and unchanged deterministic output. Deliberately allocating/logging doubles must fail. Measure zero incremental diagnostic cost; do not assert that existing instance construction allocates nothing.

**N18 ADD-AC, medium — D1/D5 `ResumeBudget_UsesExplicitUnitsAndPreludePrecondition`**

Evidence: D5 expresses the comparison window in half-cycles; `-limitcycles` uses CPU clocks; the emitter produces two half-cycle records per clock. A short continuation after a low-clock fixture can imply a limit below the three-second prelude.

Assessment: The required case matrix exists, but conversion and infeasible-budget outcomes need explicit assertions.

AC: State and test clock/half-cycle conversion, odd-window rounding, overflow and permit counts. The low-clock positive must load and supply the entire required window. Reject prelude-only termination with its reason. Preserve the exact tail and termination reason without changing the comparison interval.

**N19 FAIL, low — False register-export exclusivity**

Evidence: Plan line 14 calls the trap the only register-export point apart from JAM and monitor entry. Active pinned `6510dtvcore.c` also calls `EXPORT_REGISTERS` at line 451 for `IK_DMA` and line 1012 in `BRK`, alongside the trap at line 410.

Assessment: The chosen public trap seam remains supported. The absolute source claim is false.

Correction: Narrow the statement to the verified public trap seam and its instruction-boundary/pending-interrupt limits. Do not infer arbitrary-cycle checkpoint capability or propose native changes from the other export sites.

**Requirements and whole-plan assessment**

This is class 1 implementation-plan review; all four hostile surfaces apply.

- `TR-VSFLOCKSTEP-RESUME-001 AC1`: Real legacy resume and current round trip remain D5 obligations; N12 identifies the external-v1.3 gap.
- AC2: The builder-reference amendment preserves clean materialization and immutability semantics.
- `TEST-VICE-001 AC1/AC2`: Both-host clean-target preflights remain required.
- `TR-CYCLE-001 AC4`: Managed acceptance remains pending. `REAL-C64-PHI2-AEC-001` supplies native reference evidence.
- `FR-SNP-005 AC3` / `TEST-SNP-002 AC4`: These are the comparator fixture’s mapping. That fixture does not establish complete state import.
- `PLAN-VICEORACLE-001` tasks 4 and 5 remain pending. Task 4 done-state follows step 12 AGREE; task 5 follows Phase E and final acceptance.
- Slice 1 retains pinned builds, complete schema/edges, ROM/media hashes, required fixtures, deterministic regeneration, non-skippable prerequisites, 199 adapter cases, real bindings and the cumulative target. R8’s observer substitution is explicit; it does not waive R12 or missing external inputs.

The trap callback has exported architectural registers at dispatch. Public machine writers write `MAINCPU` at that boundary before later modules. Both load wraps are cross-object seams, allowing CPU success to be distinguished from later machine-load failure before the next gate.

Snapshot-bearing runs intentionally emit `VICE_SNAP`; their resume/round-trip claim is separate from no-snapshot equivalence. The equivalence rerun must emit no such lines. No immutable-source modification is needed or authorized for these seams.

The literal `100000` capability was not executed during this review. Interrupts and stolen cycles can delay the callback; revision 5 correctly records that limitation rather than substituting a clock. Independent work may finish, but blocked required cases cannot become a completed all-green Slice 1 gate.

**Structured what-if pass**

- **Different checkpoint clocks across hosts:** Withhold the fixture and raise the named blocker. N14 requires a coherent nonpassing blocked exit.
- **Pending IRQ/NMI before the trap:** Interrupt service can delay writing. Measure both clocks; do not re-arm at the callback clock.
- **Request inside a stolen-cycle run:** Registration cannot manufacture complete architectural state at the interior cycle. A different callback clock blocks literal xvic acceptance.
- **Stolen/BA-low first resumed access:** Permit the prefix, then require PC read/dummy, ordered success, correct AEC and the entire untrimmed window.
- **Provider/discovery-only native consumer:** Classify semantically and audit discovery before filtering; N16.
- **Unwritable/missing load log:** Fail loudly/fail the gate. Separate positive probes cannot manufacture an empty negative log.
- **Changed known-red set:** Reject improvements, regressions and equal-sized swaps until explicitly dispositioned.
- **Windows autocrlf with interrupted restoration:** Preserve exact host bytes, recognize Git-clean restored paths, reject drift and resume only untouched paths.
- **Failure after either restoration host:** Retain originals/evidence, report both host states, leave sources pinned and stop before E.
- **Wrong workspace, host, gitlink or preservation input:** Stop before overwrite.
- **D2 before real artifacts exist:** Contract bindings run in D1/D2; real bindings and fresh artifacts belong to D5.
- **CPU module succeeds, later module fails:** Reject ordered `resume` followed by `load,-1` and reset.
- **Low clock, prelude-only exit, overflow or overshoot:** Prove the prelude-safe full interval, explicit units and retained tail; N18.
- **No external v1.3 input:** Keep synthetic coverage labeled, but leave unchanged external-fixture acceptance blocked or explicitly amend it.
- **Armed gate held/quit/EOF/error:** Prove forwarding and terminal behavior separately from unset equivalence; N15.
- **Unset detector allocates/logs during steps:** Direct allocation/log-call controls must reject the violation; N17.

**Reviewer process disclosures**

- `add-profile` ran initially and after context recovery; the one non-skill global profile file was read fully on each load.
- No nested reviewer, model substitution, commit, push, file move or deletion occurred.
- An initial analyzer invocation supplied an unsupported path array. I corrected it to directory plus `Recurse` and reran checks.
- An initial detailed-history query used a nested query object and returned broader results than intended. I corrected the parameters and verified the exact own session/request. No historical turn was modified.
- The fixture helper initially used an incorrect clock/register payload offset. I corrected it to the pinned 64-bit layout, reran inspection and retained both receipts.
- The plugin generated its own session ID despite a requested custom ID. The verdict uses the actual returned ID.
- The server model field is generic `codex`; exact model/effort attribution comes from the CLI launch receipt.
- Provider-generated identifiers retain UTC formatting; progress and artifact reporting use measured local sortable time.
- The stale revision-4 request wording was disclosed. JSON includes all 41 current round-4 dispositions and revision-4 dispositions for all 35 round-3b IDs.
- No untrusted-plugin result occurred, so the conditional triage/failsafe branch was unnecessary.

Accuracy **97%** reflects the remaining acceptance/source inaccuracies. Completeness **96%** reflects the contradictory blocked exit and four additional observable ACs. Both remain below 98/98. No phase, TODO, requirement or implementation gate is marked done by this review.