2026-10-20T17:58:46 - **DISAGREE. Accuracy 97%; completeness 97%.**

**45 PASS, 3 FAIL, 0 UNKNOWN, 3 ADD-AC.** Failures: **R02, N19, N20**. R02 is my reviewer-process failure; N19 and N20 concern the plan.

Saved: [complete prose verdict](/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T172500-plan-nativerestore-001-r6-gpt61sol/verdict.md), [JSON verdict](/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T172500-plan-nativerestore-001-r6-gpt61sol/verdict.json), [persistence proof](/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T172500-plan-nativerestore-001-r6-gpt61sol/verdict.persistence.json).

The complete prose and JSON were persisted in 18 ordered chunks, read back byte-for-byte, and verified with SHA256 equality. No CRLF/LF normalization occurred. A history query proves the exact review turn is **completed**:

- Session: `Codex-20261020T222731Z-plugin-session`
- Request: `req-20261020T222756Z-nativerestore-r6`
- Parent: `ClaudeCode-20261020T015046Z-plugin-session` / `req-20261020T172116Z-prompt-6c6d`

No Phase D step executed. No native build, native execution, full-solution build/test, commit, push, move or deletion occurred.

This is a class-1 pre-execution review. PASS below means the plan adequately allocates the obligation or its starting-state claim matches evidence; it does not mean future implementation gates passed. ADD-AC findings are additional controls, not independent failures.

All 47 round-5 IDs receive one disposition below. All 35 round-3b IDs also carry their historical revision-4 disposition in the JSON, read from the round-4 verdict solely for that historical purpose. Those historical dispositions were PASS except F05, which was FAIL.

The subject is revision 6 despite stale revision-4 wording in the brief. All 41 initial and final file pins matched. The file-pin array does not contain the checkout commit; independently measured checkout: `develop` at `ff24117d7d756404fad439a3c14c14336709eb8c`.

R15 passes as an explicit operator-decided semantic amendment: external v1.2/v1.4 replaces external v1.2/v1.3 for AC1. D1 owns the requirement-wrapper update, primary readback and Slice 1 propagation before acceptance. Synthetic v1.3 normalization and existing adapter cases remain. AC2 changes its builder reference while preserving semantics.

R12 remains literal 100,000 for both trigger and accepted write clock on both hosts. A mismatch produces a nonpassing BLOCKED cumulative result, identified cases and independent counts, with no Phase E or done-state. That lifecycle repair passes. Its propagation into gate filters fails under N20.

- **P01 PASS: Input identity and drift.** Plan:3. All 41 SHA256 and byte-length pins match. Revision 6 is 72,648 bytes, SHA256 `AF56C59034D8164AA0AEC6045B556AE7948623C3D87F182EF85FEC4637DF33FA`. The brief’s revision-4 phrases are stale, and its claim that input-pins records the checkout commit is inaccurate. Retain separate checkout metadata and final rehash evidence.

- **P02 PASS: Native starting state.** Plan:8. Native HEAD and gitlink are `cc67418cba58e71d8e70fd353e9de47199ad2075`; 163 porcelain entries comprise 8 tracked modifications and 155 untracked paths; diff is 222 insertions and 8 deletions; native index is clean. Historical Windows data is not treated as fresh remote measurement. D0 must measure both hosts independently.

- **P03 PASS: R1 B and retained output.** Plan:83. Current `.gitmodules` says `ignore = dirty`; D4 explicitly changes it to `untracked`, requires empty tracked/index diffs, and retains the exact untracked path/hash/length inventory. This preserves build output while exposing tracked dirt.

- **P04 PASS: Plan-review authority and implementation hold.** Plan:78. D-pre requires both installation receipts, Windows trace evidence, plan AGREE and the verbatim implementation release. Handoff v2 records the 12:21 clarification permitting plan reviews. This review does not release D0 or restoration.

- **P05 PASS: Both desktop installation receipts.** Plan:78. Linux `20261020T105400` and Windows `20261020T121000` receipts record Nuke deployment/InstallMsi, release `1.3.111`, and ProductVersion commit `2c8d60284229b9fedc2d50f840153b9e8d8cbcbf`. These are host-attributed historical receipts; I did not remeasure installed GUIs remotely.

- **P06 PASS: Phase C evidence scope.** Plan:13. Linux S2b/S4 and Windows `20261020T102600` receipts support seeded reset-workload equivalence at 100k, 1M and x64sc 4M. S5 separately owns the snapshot register check. Bounded native equivalence does not establish managed parity or snapshot acceptance.

- **P07 PASS: Preservation and retirement authority.** Plan:79. D0 copies and hash-verifies originals; D4 restores named tracked paths only after preservation. R2/R3 authorize retirement of named obsolete builders; R4 retains untracked files; A03 prohibits automatic patch reapplication. Keep host-specific committed preservation evidence before overwriting.

- **F01 PASS: Counts and historical inventory.** Plan:9. The complete 2,527-path historical list contains 1,432 `.Po`, 745 `.o`, 59 `.a`, 2 `.exe`, 107 `.in` and 106 Makefiles. Current Linux remains 8 modified and 155 untracked. D0 must recompute current host inventories.

- **F02 PASS: Actual native build caller retirement.** Plan:81. `ViceSharp.TestHarness.csproj:10,68-75` invokes `Vice.Native.proj`’s `BuildNative` on Windows. D2 removes that property/call, obsolete project and builders, then requires both-host build logs without shim invocation. The new targets and generator are absent today. Verify zero remaining references before approved deletion.

- **F03 PASS: Semantic consumers and detector ownership.** Plan:81. Plain Fact tests and helper/provider paths extend beyond the 43 token hits. D0 owns per-method classification; D2 owns dual-binding audit, loud failures and discovery controls. The detector is planned work, not an existing capability. Cross-gate propagation is separately failed under N20.

- **F04 PASS: All twenty historical hunks classified.** Plan:19. All 599 patch lines and 20 hunks in 12 files were inspected. H1-H12 cover hosted exports, bootstrap, MAINCPU compatibility, drive clocks/reset, SID probes, VIC-20 sound and injected inputs. H5/H6/H9 are correctly identified as behavior corrections. VIC-20 sound remains owned by Phase F item 6; no hunk is silently called normalization.

- **F05 PASS: Literal checkpoint value preserved.** Plan:61. R12 requires trigger and write clock both equal 100,000 on both hosts. D1/D5 distinguish those clocks, prohibit rearming at the later value and otherwise raise `BLOCKER-R12-XVIC-CHECKPOINT`. This honors the ruling without claiming immutable VICE can snapshot correctly inside an instruction.

- **F06 PASS: Comparator prerequisite allocated to D2.** Plan:62. The permitted Release run returned **Failed 1, Passed 0, Skipped 0**, failing `Assert.NotNull(stateType)` at fixture line 23. D2 owns `HalfCycleState` and `CompareHalfCycleStates`, mocks first, preserving the existing assertions and exact R9 set. This is an acknowledged baseline failure, not a passing gate.

- **F07 PASS: Restore pre-overwrite and resume safety.** Plan:83. D4 checks repository, host, gitlink, index and preserved/live hashes per path. Controls cover staged drift, partial completion, missing/corrupt receipts and wrong identity. Unexpected state stops before overwrite; resume touches only verified untouched paths.

- **F08 PASS: Content-aware nonmutation bracket.** Plan:79. D0/D2/D4/D5 compare SHA256 and length for every modified and untracked path. A changed-existing-untracked-file negative control is required. This detects overwrites that unchanged path lists would miss.

- **F09 PASS: Analyzer and dry-run ownership.** Plan:76. Both current canonical scripts lack `SupportsShouldProcess` and create directories on entry. D1 allocates tested `-WhatIf` behavior before real use, with Pester and parser/analyzer evidence. Dry runs must create neither files nor directories.

- **F10 PASS: Comparator mapping and pending managed acceptance.** Plan:94. D2 corrects the comparator mapping to `FR-SNP-005` AC3 and `TEST-SNP-002` AC4. D5 supplies native `REAL-C64-PHI2-AEC-001` reference evidence and a summary test. Managed `TR-CYCLE-001` AC4 and task 5 remain pending. Native evidence does not satisfy managed behavior.

- **F11 PASS: AC2 reference update and blast radius.** Plan:103. D1 routes the builder-reference update through the requirement wrapper while preserving pinned clean out-of-tree materialization and external units. AC1’s R15 amendment is separately identified as semantic. Read back both ACs and preserve all other semantics.

- **R01 PASS: Credential handling.** Request receipt. No marker, credential store or API-key file was opened or printed. Supported file-based PowerShell wrappers internally consumed configuration. No raw REST was used. No claim is made about historical credential rotation.

- **U01 PASS: Own primary session turn.** Persistence receipt. Status/bootstrap/BeginTurn, observations and decisions returned `persisted:true`, `queued:false`, `degraded:false`. The final full-content readback and completed-turn history proof succeeded. Two historical queued records were neither replayed nor canceled.

- **U02 PASS: Requirements, mappings and TODO readback.** Plan:79. Supported effective-requirement, FR-VICE-001/FR-SNP-005 mapping and PLAN-VICEORACLE-001 reads succeeded. Seven selected requirements and tasks 4/5 remain pending. AC1 still has external v1.2/v1.3 before the planned D1 amendment. No requirements or TODOs were changed by this review.

- **A01 PASS: Every current hunk gets a host disposition.** Plan:79. D0 maps each hunk to a classification row, an explained host difference or a blocking unclassified hunk. Wrong-host and missing-hunk controls are required. The old Windows superset is not proof of current host equality.

- **A02 PASS: Entry evidence and explicit release.** Plan:78. The entry AC requires both installations, complete Windows traces, plan AGREE and the operator release. Receipt arrival or review activity alone does not authorize implementation. D0 remains held.

- **A03 PASS: Post-restore failure and recovery authority.** Plan:83. Failure after either or both hosts stops before Phase E, retains originals and receipts, reports host states and leaves restored source pinned. No automatic patch reapply or deletion is permitted. D4 owns both failure controls.

- **N01 PASS: R10 prevents the active uninitialized-clock path.** Plan:56. Fixture inspection confirms DRIVE8 2.0 payload `[0,1]`, length 2. Pinned `drive-snapshot.c:315-319,368-419,540-550` leaves three clocks uninitialized and copies them for non-NONE units. D1 computes and enforces final `-drive<n>type 0` flags and rejects missing flags. This does not claim to remove R14’s separate dead read.

- **N02 PASS: Full-load result and ordered resume boundary.** Plan:91. Both selected wrappers intercept cross-object calls. MAINCPU success precedes later modules, whose failure can reset the machine. D5 requires `resume`, then `load,0` before the next gate, clock C+1, and first CPU access at PC after any idle/stolen prefix. `load,-1` and unrelated reset signatures are rejected.

- **A04 PASS: Repository identity and Windows autocrlf.** Plan:83. D4 uses Git’s clean-worktree comparison for already-restored paths and separately verifies preserved host-byte hashes. A CRLF-converted Windows mock is required. It does not assume blob bytes equal checkout bytes.

- **A05 PASS: Identity-based execution evidence.** Plan:81. The contract retains selected IDs and terminal TRX per project, prohibits skipped/aborted/unexecuted cases and requires exactly the 20 named R9 failures. Controls reject substituted IDs, missing projects, discovery drift and partial runs. Its selector propagation fails separately under N20; do not weaken this denominator.

- **A06 PASS: Up-to-date builder and interrupted rerun.** Plan:80. Current Make handling fails when no link line appears. D1 requires reuse only after object-hash verification and a named prerequisite failure before replacing prior evidence. Mock resume and D5 real reruns are allocated.

- **R02 FAIL, high: Reviewer timestamp emission.** Response stream. I reused an earlier measured local timestamp in one commentary instead of measuring anew at emission. I immediately disclosed it after a fresh command measurement. Command-generated progress JSON timestamps were measured at emission. This is a reviewer-process failure, not a revision-6 defect. Keep it visible; it cannot be retroactively repaired or described as compliant.

- **N03 PASS: R14 dead-read disposition.** Plan:64. `rotation.c:194` reads `rotation_table_ptr` into `speed_zone`; line 203 overwrites it before use. H5 initialized only clocks. R14 explicitly accepts this limited read, with D5 continuation evidence retained. This does not establish portable defined C behavior or safety of the separate active clock reads.

- **N04 PASS: Architectural register observer and forwarding.** Plan:57. Both MAINCPU readers populate `maincpu_regs`. `mos6510.h:116-136` reconstructs N/Z/unused status; the supplied fixture has P=`$A0`. D1 covers status combinations, wide clock/PC, exactly-once real calls, preserved result/state and sink failure. Per-cycle register exposure is not promised.

- **N05 PASS: DRIVE payload and resource/media effects.** Plan:91. The writer emits `has_tde`/`has_drives` first; missing modules follow TDE-off handling. `set_drive_type` detaches media and changes bus routing. D1 validates payload bytes/length; D5 records effective type/TDE/media per unit, including explicit no-media context.

- **N06 PASS: Provider/helper consumers and managed fakes.** Plan:79. D0 requires per-method classification and four controls, including provider-only native use and a selected managed fake. Current literal-dot semantic grep yields 55 candidate files; the plan records 56 historically. Neither count is a completed semantic inventory. Reconcile candidates per method in D0.

- **N07 PASS: Absolute cycle budget and complete window.** Plan:91. `-limitcycles` is absolute, and CPU loops check `maincpu_clk > limit` after instructions. D5 records prelude, restored clock, W cycles/2W half-cycles, permits, overshoot tail and termination reason. No exact stop or complete resume is inferred from a short prefix.

- **N08 PASS: D1 contracts, D2 boundary and D5 real bindings.** Plan:80. D1 creates mock-bound `VsfSlice1Contract` families; D5 adds same-assertion `VsfSlice1Real` cases using artifacts generated that run. D2 has no future real-artifact dependency. Selector propagation remains N20.

- **N09 PASS: Tasks stay pending through the D3 hold.** Plan:96. Task 4 remains pending until step 12 accepted HV; task 5 remains pending until managed/final gates. Live TODO shows both false. Evidence attachment is separated from done-state changes.

- **N10 PASS: Marker-relative windows and observational history.** Plan:91. Each run aligns to its own checkpoint/load marker, preserves raw indices and rejects trimming or reused history. First-offset AEC must agree even for a stolen first access. Existing observer history is process-local, so its restoration is not assumed.

- **N11 PASS: Detector initialization, failure and inert behavior.** Plan:81. D2 covers combined resolution and xvic paths, fresh-process alias/fallback ordering, pre-discovery environment setup, retained logs and loud missing/unwritable-log failures. Availability queries alone do not prove actual loading. Both-binding controls and separate positive logs remain required.

- **N12 PASS: Explicit R15 amendment.** Plan:63. All three repository VSFs were parsed: two identical real MAINCPU 1.2 files and one real 1.4 file; none is 1.3. R15 explicitly changes real inputs to v1.2/v1.4. D1 wrapper update/readback and Slice 1 propagation are allocated; D5 retains real resume and full round-trip window checks. Synthetic v1.3 normalization remains. Remove Exit:96’s obsolete optional v1.3-input blocker phrase as propagation cleanup.

- **N13 PASS: Per-host budget evidence.** Plan:91. Exact invocation, prelude, clock budget, units, permits, final window, low/high-clock controls and tail evidence are allocated per fixture and host. The comparison interval cannot adapt to convenient output. N23 adds a terminal-outcome control.

- **N14 PASS: Explicit nonpassing BLOCKED lifecycle.** Plan:61,87,93,96. Blocked xvic case IDs, both measured clocks, independent counts and nonzero cumulative exit are explicit. Phase E, done-states, partial-pass and skip interpretations are prohibited. One-host and both-host mismatch controls are allocated. N20 remains a distinct selector-propagation defect.

- **N15 PASS: Armed checkpoint gate interactions.** Plan:80. D1 now covers one record/permit per advanced cycle, withheld permits, unchanged quit/EOF/error outcomes, and independently measured trigger/write clocks under interrupt and stolen-cycle requests. D5 repeats real both-host cases. Unset-variable equivalence alone is no longer used to prove the armed path.

- **N16 PASS: Audit starts before discovery.** Plan:81. Nuke sets the audit environment before discovery and never truncates afterward. Fresh attribute-only/provider-only controls, a selected managed fake and separate positives are explicit. Every specified attempt must be observed or cause visible gate failure.

- **N17 PASS: Seam-level allocation test.** Plan:81. D2 requires warmed repeated step calls on both bindings with audit unset, zero incremental allocations/log calls and unchanged deterministic output. Allocating/logging doubles must be rejected. This directly tests the intended seam.

- **N18 PASS: Clock/half-cycle budget rules.** Plan:91. W cycles equals 2W records; windows use whole cycles; odd requests and overflow fail. Low-clock prelude-safe and high-restored-clock cases, permits and retained overshoot are explicit. N23 adds the remaining finite termination control.

- **N19 FAIL, medium: Export-exclusivity correction did not propagate.** Plan:54. Section 1 line 14 correctly includes DMA and BRK, but R11 still says interpreter locals are exported **only** at traps, JAM and monitor entry. Active `6510dtvcore.c` also exports at DMA line 451 and BRK line 1012. Replace every exclusive list with the narrower public-seam claim and sweep all copies. The public trap seam remains valid; this does not authorize arbitrary-cycle registers.

- **N20 FAIL, high: Gate filters contradict exclusive real-test execution and every-gate shim retirement.** Plan:81,93,94,102; `Build.cs:97-112,133-142`. Test and CiTest have separate filters. D2 adds only `Category!=ShimLegacy` to Test while adding the projects to the solution. Neither filter excludes `VsfSlice1Real`; CiTest still selects ShimLegacy. The retained static projection proves this from the stated edits.

  Once D5 registers real cases, ordinary Test selects them despite the “cumulative target only” rule. With R12 blocked, this adds failures outside R9 or violates A05 by omitting selected cases. CiTest also reaches retired consumers.

  D2 must explicitly reconcile Test, CiTest and cumulative selectors. Ordinary gates should retain contract cases while excluding ShimLegacy and VsfSlice1Real; the cumulative target must include every required real case with stable IDs, explicit BLOCKED results and nonzero exit. Add selection controls at D2 and after D5 registration. Preserve all denominators and the exact R9 set.

- **N21 ADD-AC, medium: Rejected checkpoint preserves candidates and published artifacts.** Owner: D1/D2 generator contract and real D5. The callback writes candidate bytes before the generator can inspect the write clock or compare hosts. Define the observable result for mismatch, malformed/missing checkpoint record, write failure and interrupted retry: accepted fixture/manifest remain unchanged; candidate bytes, hashes and logs are retained; nothing is moved, deleted or silently published; the named BLOCKED/nonzero outcome includes affected IDs; stale prior artifacts cannot count as generated in that run.

- **N22 ADD-AC, medium: Trap request after interrupt-mask capture.** Owner: D1/D5. `6510dtvcore.c:1758` captures the pending mask, line 1763 dispatches it, and the macro copies it to `ik`. A clock-hook request raised during an already-dispatched IRQ/NMI can be absent from that captured mask; a handler opcode can execute before the next sample.

  Add the distinct mock/real history where the trigger occurs after mask capture. Preserve the actual callback clock and reject later clocks under literal R12. Do not force the mock callback to an earlier boundary. Narrow “first boundary” wording to the actual dispatch semantics.

- **N23 ADD-AC, medium: Low-clock resume terminates after the complete window.** Owner: D1 budget contract and D5. D1 says C+W, while D5’s low-clock case raises the limit to prelude length+W. Restoring C rewinds the absolute clock, and instruction-level checking can overshoot.

  Require `load,0`, exactly W compared cycles/2W half-cycles, and an explicit finite terminal gate/process outcome. Record permits, retained tail and exit reason. Reject waiting indefinitely for an unsupplied permit, early stop or silently stretched windows. Reconcile D1’s C+W wording with the D5 exception.

Requirements and obligation assessment:

- `TR-VSFLOCKSTEP-RESUME-001`: R15’s AC1 semantic amendment, wrapper update/readback and Slice 1 propagation are explicit. Real v1.2/v1.4 behavior and synthetic v1.3 byte cases remain. AC2 preserves pinned out-of-tree/external-only semantics.
- `FR-VICE-001`, `TR-VICE-ORACLE-001`, `TEST-VICE-001`: live reads succeeded and returned pending obligations. D0 preservation and clean-target checks allocate AC1/AC2; patched-native results remain diagnostic.
- `TR-CYCLE-001` AC4: managed acceptance remains pending Phase E. Native PHI2/AEC evidence is reference evidence only.
- `FR-SNP-005` AC3 and `TEST-SNP-002` AC4: comparator mapping and implementation are owned by D2; this does not complete broader snapshot requirements.
- `PLAN-VICEORACLE-001` tasks 4/5: both remain false. Evidence may be attached while pending; completion follows their required later gates.
- VSF Slice 1: builder/preflight, provenance, schema fields/edges, prerequisites, literal-100000 PAL/NTSC fixtures, R10 launch, real resume, round trip, negatives and cumulative command have owners. N20 prevents the promised gate isolation; N21-N23 add failure/terminal controls.

Structured what-if results:

- **Host checkpoint clocks differ:** named BLOCKED result, both clocks and case IDs, no alternate trigger. N14 passes; N21 adds candidate retention.
- **Pending interrupt at trigger:** measured callback clock governs acceptance. N22 adds the already-captured-mask history.
- **Trigger inside stolen cycles:** armed gate controls preserve forwarding and permits; a later write clock fails literal R12.
- **First resumed access is stolen:** retain the prefix, own BA history, PC access and complete marker-relative comparison; no trimming.
- **Native consumer exists only in a provider/attribute:** pre-discovery audit observes attempts or fails visibly; managed fake stays selected. CiTest leakage fails under N20.
- **Audit file is unwritable/missing:** loud attempt/gate failure, never empty-log success.
- **Known-red set changes:** A05 rejects added, removed or substituted IDs and incomplete execution; no silent R9 update.
- **Windows autocrlf:** Git handles checkout conversion; original host bytes remain hash-verified; wrong host/unexpected state stops.
- **Crash between hosts or after candidate creation:** D4 reports restored/untouched/unexpected states without automatic reapply; N21 adds publication/retry outcomes.
- **Low-clock fixture:** full window remains mandatory; N23 adds finite terminal permit/process behavior.
- **Cross-workspace/session or stale evidence:** identity checks and same-run artifact provenance reject inappropriate reuse.
- **R12 blocked during ordinary/cumulative execution:** the lifecycle is explicit, but the stated filters conflict with it. N20 fails; fix selectors rather than R12 or denominators.

The public trap callback runs after `EXPORT_REGISTERS`, with `IMPORT_REGISTERS` afterward. `machine_write_snapshot` synchronously delegates to the native writer, whose MAINCPU module writes `maincpu_clk`. Both selected read seams are cross-object. This supports boundary feasibility from source; it is not runtime evidence from this review. Newly emitted `VICE_SNAP` lines do not establish old equivalence claims: D1/D5 require inert no-snapshot reruns with zero such lines.

The permitted baseline command was:

```powershell
dotnet test "/home/sharpninja/github/vice-sharp/tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj" -c Release --filter "FullyQualifiedName~Vic20HalfCycleComparatorTests" --results-directory "/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T172500-plan-nativerestore-001-r6-gpt61sol/TestResults" --logger "trx;LogFileName=vic20-comparator.trx" --diag "/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T172500-plan-nativerestore-001-r6-gpt61sol/TestResults/vstest.diag.log" --blame-hang --blame-hang-timeout 5m
```

Exit 1: **Passed 0, Failed 1, Skipped 0**. The missing reflected `HalfCycleState` failure is allocated to D2. Output, TRX and diagnostics are retained.

Final integrity checks found zero drift across 41 pins, unchanged checkout/native status, unchanged hashes for all 163 inventoried modified/untracked native paths, zero mismatches among 20 materialized source files checked against pinned Git blobs, and `git diff --check` exit 0. The 163-file content baseline was captured after the permitted comparator run, not before it.

Reviewer identity is Codex / `gpt-6.1-sol` / `max`, through the requested CLI, with no substitution or nested reviewer. Add-profile read all non-skill profile Markdown files, one file, and repeated that read after compaction. Supported primary plugin writes succeeded with failsafe draining disabled. No untrusted result occurred, so the conditional triage/failsafe path was unnecessary. Exact model/effort is attributed to the launch receipt; the server model header is generic `codex`.

**Execution remains held.** Correct N19/N20 and incorporate or explicitly dispose N21-N23. R02 remains recorded as my process failure and requires no product edit. A new qualifying review is required before implementation release.