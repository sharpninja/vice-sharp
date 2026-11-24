2026-10-20T16:08:47 - **DISAGREE: PLAN-NATIVERESTORE-001 revision 4, before execution.**

**Accuracy 96%; completeness 93%. PASS 34, FAIL 3, UNKNOWN 0, ADD-AC 4. Execution remains blocked.**

FAIL: **F05, N08, N09**. UNKNOWN: none. Added ACs N10-N13 are not FAIL by themselves.

Saved: [verdict.md](/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T153600-plan-nativerestore-001-r4-gpt61sol/verdict.md), [verdict.json](/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T153600-plan-nativerestore-001-r4-gpt61sol/verdict.json), [verdict.persistence.json](/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T153600-plan-nativerestore-001-r4-gpt61sol/verdict.persistence.json).

Accuracy 96: Most prior factual/design defects are corrected and source-supported. The exact-checkpoint interpretation and first-boundary/trap regeneration claim remain inaccurate; the phase/status contracts still conflict. This is a holistic score, not a ratio of finding counts.

Completeness 93: New ownership closes detector, comparator and full-load observation gaps, but R12 lacks an authorized executable exact-checkpoint outcome, D2/D5 test activation is unallocated, and task-4 completion creates a D3 gate cycle. Added ACs expose first-sample observation history, detector failure modes, schema/provenance and budget edges.

Reviewer: **gpt-6.1-sol-max@Codex**.

- Own session: `Codex-20261020T203928Z-plugin-session`
- Own request: `req-20261020T203943Z-hv-nativerestore-r4`
- Parent session: `ClaudeCode-20261020T015046Z-plugin-session`
- Parent request: `req-20261020T172116Z-prompt-6c6d`
- Request: `/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T153600-plan-nativerestore-001-r4-gpt61sol/request.jsonl`
- Response: `/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T153600-plan-nativerestore-001-r4-gpt61sol/response.jsonl`

All 36 content pins match. Observed `develop` HEAD is `a74f6c520fbd0e42573a05e8513ffebbcb2cf4f0`; `input-pins.json` does not contain a checkout-commit pin. Native gitlink/HEAD matches `cc67418cba58e71d8e70fd353e9de47199ad2075`. The 28 inspected materialized source files match their pinned Git blobs. Final pin, source and native content inventories bracket this review.

The add-profile skill read the one global non-skill profile file in full. The no-marker-read rule superseded marker-read instructions. File-based PowerShell review helpers were parser/analyzer checked and dry-run. No Python, Node, nested reviewer or environment/hook repair was used. Supported primary writes use the real Codex source type; the wrapper header defaults to generic `codex`, with exact model/effort retained in launch and verdict content. Two historical queued records were neither replayed nor canceled.

Executed diagnostic:

```powershell
dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --no-build --no-restore --nologo --filter FullyQualifiedName~Vic20HalfCycleComparatorTests --logger "trx;LogFileName=comparator.trx" --results-directory <this receipt> --diag <this receipt>/comparator-vstest.diag.log
```

Result: exit 1; **1 failed, 0 passed, 0 skipped**, missing `HalfCycleState` at source line 23. Exact arguments, binary hash, stdout and TRX are retained as `comparator-command.json`, `comparator-output.txt` and `comparator.trx`. This is a baseline diagnostic, not a passing implementation gate.

All 35 round 3b IDs follow exactly once, re-verified against live sources; then new IDs. PASS concerns the plan contract or stated observation, not an unexecuted Phase D implementation gate.

**P01 PASS [info]: Input identity and drift.** Source: `input-pins.json:1`.

Evidence: All 36 SHA256 and byte-length pins match in `initial-pins.json`. Subject hash `3B490C06FFD6ED55E847B4A530FBA381784020D4889B0F8BB72B9F955AD5BFAA`, 58109 bytes. Live `develop` HEAD is `a74f6c520fbd0e42573a05e8513ffebbcb2cf4f0`.

Assessment: The content pins identify revision 4. `input-pins.json` is a file-pin array and does not pin a checkout commit, contrary to the brief; the live checkout is recorded separately.

Required action / AC: Keep the measured checkout identity and final integrity bracket; add explicit checkout metadata to future launches.

**P02 PASS [info]: Native starting state.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:7`.

Evidence: `initial-git.json` and `initial-inventory.json` independently show native gitlink/HEAD `cc67418cba58e71d8e70fd353e9de47199ad2075`, 8 tracked modifications, 155 untracked paths, 222 insertions and 8 deletions.

Assessment: The Linux state claims match live Git. Windows current dirt is deliberately a D0 measurement, with historical evidence distinguished.

Required action / AC: Keep separate fresh host receipts before restoration.

**P03 PASS [info]: R1 B and retained output.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:42`.

Evidence: `.gitmodules:4` currently says `ignore = dirty`. R1 B, D4 and Exit require `ignore = untracked`, empty tracked/index diffs, and the exact retained untracked content inventory.

Assessment: This follows the explicit ignore ruling and keeps build artifacts and the injected header.

Required action / AC: Preserve the path/hash/length equality and both-host checks.

**P04 PASS [info]: Plan review authority and execution hold.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:77`.

Evidence: D-pre requires both installations, Windows trace evidence, plan AGREE and explicit release before D0. Handoff v2:268 records the operator clarification authorizing plan HV.

Assessment: This review is authorized and does not release implementation. The separate D3 done-state contradiction is N09.

Required action / AC: Keep the entry hold; correct N09 before execution.

**P05 PASS [info]: Both desktop installations.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:77`.

Evidence: Linux `20261020T105400` and Windows `20261020T121000` installation READMEs record Nuke deployment/InstallMsi, installed 1.3.111 and ProductVersion commit `2c8d60284229b9fedc2d50f840153b9e8d8cbcbf`.

Assessment: The install precondition is supported by both host receipts. This reviewer did not launch the GUI or remeasure remote installations.

Required action / AC: Bind the existing installation receipts to D0 with their actual host attribution.

**P06 PASS [info]: Phase C evidence scope.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:13`.

Evidence: The pinned S2b/S4 Linux READMEs and Windows `20261020T102600` README support seeded reset-workload builds/equivalence at 100k, 1M and x64sc 4M. S5 explicitly deferred real snapshot register checks.

Assessment: These are bounded observer/reference results, not completed managed parity or native snapshot acceptance.

Required action / AC: Retain workload, seed, applicability rules and original traces in D5.

**P07 PASS [info]: Preservation and retirement authority.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:78`.

Evidence: D0 copies and hash-verifies tracked/injected source while retaining originals; D4 restores named tracked paths only. R2/R3 expressly retire the named builders; R4 retains untracked files. A03 prohibits automatic reapply/delete.

Assessment: The proposed preservation/restoration respects the operator scope. No such operation ran during review.

Required action / AC: Require the fresh preservation receipt and release before every real overwrite.

**F01 PASS [medium]: Counts and historical inventory.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:8`.

Evidence: The complete 2527-line historical inventory was parsed; counts are 1432 `.Po`, 745 `.o`, 59 `.a`, 2 `.exe` and 107 `.in`. Live Linux porcelain has 163 entries, 8 modified plus 155 untracked.

Assessment: The old 135-path and extension mistakes remain corrected.

Required action / AC: Use the fresh D0 inventory on each host rather than treating historical counts as current Windows state.

**F02 PASS [high]: Native build caller removal.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:80`.

Evidence: `TestHarness.csproj:10` defines `ViceNativeProject`; lines 68-74 define the `BuildViceNative` target and Windows `BuildNative` invocation. `Vice.Native.proj:5,14,32-39` owns the in-tree shim path. Scoped live searches find the project caller and no Nuke `BuildViceNative` target.

Assessment: D2 now names the actual executable caller/property and requires no builder invocation in both-host logs before retirement.

Required action / AC: Retain the no-reference receipt, and remove or make inert the whole obsolete target consistently.

**F03 PASS [high]: Detector implementation is now owned.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:49`.

Evidence: `ViceNative.cs:36-71` currently has no logging; `ViceNative.Xvic.cs:31-40` shares its resolver arrangement. D2 explicitly adds env-gated attempt/result logging, positive/failing/unset controls and an empty retained gate log. R6 explicitly enumerates direct, helper and provider use per method.

Assessment: The prior nonexistent-logging assumption is corrected. Resolver diagnostics remain outside the per-cycle path; fresh-process and log-failure hardening is added as N11, not assumed proven implementation.

Required action / AC: Implement the stated detector mocks first; enforce N11 and retain both-host zero-attempt evidence.

**F04 PASS [high]: All twenty hunks classified.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:19`.

Evidence: The complete 599-line patch has 20 `@@` headers in 12 files. `initial-inventory.json` records every header. H1-H10 and H12 cover all 20; H11 is the separate injected header/configuration. H5/H6/H9 are rejected corrections with explicit R7 ownership.

Assessment: No tracked hunk is omitted, and VIC-20 audio remains Phase F item 6. The dead rotation read is separately disposed by R14.

Required action / AC: Re-derive the map from each fresh D0 patch and block any unclassified or required-uncovered hunk.

**F05 FAIL [high]: R12 B is narrowed and the pinned checkpoint recipe is not equivalent.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:61`.

Evidence: VSF plan:241 requires both xvic fixtures at 100000 cycles; the operator chose exact cycle N. Revision 4 instead freezes the first measured later boundary. Both actual CPU units include `6510dtvcore.c` (`mainc64cpu.c:809`, `mainviccpu.c:636`), whose lines 354-412 service IRQ/NMI before the exported-register trap, using an earlier local `ik`. R12/D5 then arm at the newly measured callback clock, rather than the original trigger clock.

Assessment: The source supports safe architectural registers at a trap boundary and a boundary MAINCPU clock, not an arbitrary interior-cycle snapshot or a guarantee of the first boundary. If the first trigger is serviced after an interrupt, re-arming at that callback clock can occur inside the interrupt sequence after `ik` was latched, delaying the next callback through a handler instruction. Equality of a discovered clock does not authorize replacing 100000. The limit is disclosed but made conditional on asking the operator to repeat an already explicit ruling.

Required action / AC: Keep literal 100000 or report it as the current R12 blocker until the operator explicitly approves a different checkpoint. Distinguish trigger and actual write clocks, and prove identical fresh-command regeneration on both hosts with pending IRQ/NMI and stolen-cycle cases before freezing any approved value. Preserve the full Slice 1 map; N12 strengthens schema/provenance checks.

**F06 PASS [high]: Comparator prerequisite explicitly moved into D2.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:62`.

Evidence: The authorized Release no-build/no-restore filter reproduced 1 failed, 0 passed, 0 skipped at `Vic20HalfCycleComparatorTests.cs:23`. Source searches find no current `HalfCycleState` or `CompareHalfCycleStates` definitions. R13 B explicitly assigns both implementations and a green fixture to D2.

Assessment: The fixture stays outside `ShimLegacy` and outside the R9 exception. Its missing implementation is now an authorized prerequisite instead of a Phase E dependency.

Required action / AC: Preserve the fixture assertions and prove each field and first-difference ordering with the same mock and real assertions.

**F07 PASS [high]: Restore drift and interruption checks.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:82`.

Evidence: D4 checks repository/host/gitlink identity, clean index and each live file against preserved bytes or Git-clean checkout state before overwrite. It rejects unexpected state and resumes only untouched paths.

Assessment: The plan rejects staged/live drift and defines partial-completion outcomes before restoration.

Required action / AC: Implement the stated happy and negative controls before any real checkout.

**F08 PASS [high]: Content-aware nonmutation bracket.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:78`.

Evidence: D0 hashes and records lengths of every modified tracked and untracked file; D2/D4/D5 and Exit require matching relevant inventories. D2 includes a changed-existing-untracked-file negative control.

Assessment: The old path-list-only blind spot remains closed.

Required action / AC: Keep complete inventories, not only porcelain path equality.

**F09 PASS [medium]: Script analyzer and dry-run ownership.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:79`.

Evidence: The live builder:26 and equivalence script:20 lack `SupportsShouldProcess` and create directories at lines 52/41. D1 explicitly adds it, no-side-effect dry-run Pester tests and analyzer-zero gates.

Assessment: The missing capability is planned as an earlier change rather than assumed present.

Required action / AC: Prove WhatIf launches no native process and writes no file/directory, then record the real runs.

**F10 PASS [medium]: Correct annotation and managed obligation retained.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:62`.

Evidence: R13 B explicitly corrects `Vic20HalfCycleComparatorTests.cs:7` to `FR-SNP-005` AC3 / `TEST-SNP-002` AC4 while preserving assertions. D5:93 and Exit:95 label native PHI2/AEC traces as reference evidence and keep managed `TR-CYCLE-001` AC4 and task 5 pending.

Assessment: The former wrong-target omission is corrected; this small comparator fixture still does not satisfy the full restored-state requirements.

Required action / AC: Keep task 5 pending through actual managed comparison and the required final gate.

**F11 PASS [medium]: AC2 reference and blast radius.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:79`.

Evidence: The live canonical TR AC2 still names `build-vice-shim.ps1`. D1 explicitly changes only that reference through the wrapper while retaining clean materialization, original source hashes, generated-artifact inventory and external units. Section 5 lists the new units/tool/detector/comparator.

Assessment: The interface substitution can preserve AC2 semantics and does not authorize AC1 or checkpoint reductions.

Required action / AC: Retain exact before/after AC2 readback and regenerate canonical projections through supported tools.

**R01 PASS [high]: Current reviewer credential handling.** Source: `response.jsonl:1`.

Evidence: No marker or credential file was read or printed by this reviewer. Supported plugin operations used analyzed receipt-local PowerShell helpers; only the plugin internally consumes its marker.

Assessment: The round 1 incident was not repeated. This is not an attestation of historical credential rotation.

Required action / AC: Keep the credential prohibition and do not publish the old private raw stream.

**U01 PASS [high]: Own primary persistence and final proof.** Source: `verdict.persistence.json:1`.

Evidence: Bootstrap/begin/checkpoint wrappers return `persisted:true`, `queued:false` and `degraded:false` for this own review turn. Status reports two historical pending records; drain is disabled. `verdict.persistence.json` proves complete-content hashes and completed-turn history.

Assessment: Primary writes are distinguished from untouched historical queues. This disposition is supported by the final proof, not merely a local cache flag.

Required action / AC: Retain full prose/JSON readback and exact completed requestId proof; never count queued content as primary.

**U02 PASS [medium]: Effective store and mappings.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:78`.

Evidence: Fresh supported effective/listMappings/todo.get receipts are retained as `store-*.txt` and `store-effective.selected.json`. Seven relevant FR/TR/TEST records are pending with unsatisfied ACs; FR-VICE and FR-SNP mappings are present. `PLAN-VICEORACLE-001` tasks 4 and 5 are false.

Assessment: The live store confirms the mappings and pending obligations. No requirement or TODO was mutated by this review, and no Windows store readback is claimed.

Required action / AC: Quote fresh effective mappings in D0 and preserve pending full managed gates.

**A01 PASS [medium]: Fresh host hunk disposition.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:78`.

Evidence: D0 requires each fresh `@@` hunk mapped, an explained host-context difference recorded, or a blocker. Missing-hunk and wrong-host controls are explicit.

Assessment: Different host deltas have an owned observable outcome.

Required action / AC: Bind each disposition to that host preservation hash and block unclassified content.

**A02 PASS [medium]: Entry evidence and explicit release.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:77`.

Evidence: `EntryEvidence_BindsBothHostsAndExplicitRelease` requires both installed commit/version receipts, Windows trace matrix, plan AGREE and operator release before D0.

Assessment: Receipt arrival or this review alone cannot release implementation.

Required action / AC: Do not execute D0 under this DISAGREE verdict.

**A03 PASS [medium]: Post-restore failure outcome.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:82`.

Evidence: D4 stops before Phase E on D5 failure, retains all originals/receipts, reports both host path states and keeps restored source pinned; no automatic patch reapply or untracked deletion.

Assessment: The first-host and both-host failure cases have a safe retained state.

Required action / AC: Keep this stopped state and obtain scoped authority for any recovery mutation.

**N01 PASS [high]: Approved three-clock precondition.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:56`.

Evidence: Fresh fixture byte inspection confirms MAINCPU 1.2, clock 109163946, PC 22377, P A0, DRIVE8 2.0 payload 0,1. Pinned `drive-snapshot.c:540-550` skips the three clock copies when type is NONE; `drive-resources.c:243-246` sets NONE and disables the drive.

Assessment: R10 protects the specific clock-copy path while keeping the approved fixture. It does not erase resource side effects or the separate accepted dead read.

Required action / AC: Retain the computed final flags, validation and post-load resource/continuation receipt.

**N02 PASS [high]: Ordered CPU-load and complete-load distinction.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:90`.

Evidence: R11 wraps both cross-object calls. `c64-snapshot.c:121-153` and `vic20-snapshot.c:129-165` confirm CPU success precedes later readers and a later failure schedules reset. D5 requires ordered resume, `load,0`, next gate at clock+1, permitted idle/stolen prefix and first CPU read/dummy at PC, and rejects later failures/unrelated resets.

Assessment: A loaded CPU module is no longer mistaken for full-load success; repeated clock values are disambiguated by ordered markers. No unavailable opcode-fetch wire tag is required. N10 adds observer-history/window coverage.

Required action / AC: Bind all positive and rejected cases to both real observer binaries without substituting two fields for AC1.

**A04 PASS [medium]: Host checkout conversion and identity.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:82`.

Evidence: D4 uses `git diff --exit-code` on each path to recognize an already restored checkout, and checks root, native gitdir, SHA and host before writes. Windows CRLF interruption/resume is explicitly a mock case.

Assessment: This avoids comparing raw LF blob bytes with a CRLF worktree as the restore predicate.

Required action / AC: Retain raw preservation-copy hashes and reject any unexpected conversion or staged drift.

**A05 PASS [medium]: Stable test identities and full execution.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:80`.

Evidence: D2/D5 retain selected IDs and terminal per-project TRX, require zero skipped/aborted/not-executed, exactly the named R9 failure set, and reject swapped failures, missing projects, discovery drift and partial runs.

Assessment: The gate exposes both an improved and a worsened known-red set rather than silently updating it. The scheduling gap for new real cases is N08.

Required action / AC: Stop on any set change and disposition it explicitly; keep every project result.

**A06 PASS [medium]: Up-to-date builder rerun.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:79`.

Evidence: Live Make:220-231 relies on current stdout and replaces `stock-link-lines.json` before checking missing link lines. D1 requires hash-verified prior evidence reuse or a named safe prerequisite before replacing it; D5 reruns All on both hosts.

Assessment: The prior repeat-build failure now has an explicit implementation owner and safe evidence-retention outcome.

Required action / AC: Prove up-to-date and interrupted-stage cases mocks first and retain the real rerun receipts.

**R02 PASS [medium]: Measured live timestamps.** Source: `response.jsonl:1`.

Evidence: Progress objects were emitted from PowerShell `DateTime.Now` at execution, with numeric percentages and measured-at-emission local time; ETAs are estimates and not deadlines. Artifact timestamps are likewise command-generated.

Assessment: The former estimated/backfilled timestamp defect is not repeated; historical source timestamps remain source data.

Required action / AC: Continue measured progress through final primary persistence and output.

**N03 PASS [high]: R14 A accepted dead read.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:63`.

Evidence: `drive-snapshot.c:315,394,433` passes the partly uninitialized rotation array. `rotation.c:194` assigns `speed_zone`, then line 203 overwrites it before use. Patch H5 zeroed only clocks. R14 A expressly accepts this dead read; D1 documents it and D5 retains two-host continuation equality.

Assessment: The operator-approved disposition resolves the earlier UNKNOWN. It is an acceptance of this pinned dead-read path, not a claim of portable defined C behavior or a sanitizer-clean reader.

Required action / AC: Keep the exact scope and executable two-host evidence; do not expand it to live uninitialized clock reads.

**N04 PASS [medium]: Architectural register and forwarding contract.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:57`.

Evidence: `mos6510.h:116-136` separates N/Z and recomposes P. The fixture P is A0. R11/D1 specify architectural P combinations, wide clock/PC, exactly-once forwarding, preserved return/state, no resume on reader failure, ordered load result and checked stderr/flush failure 72.

Assessment: The added AC is incorporated at the chosen public seam on both machines; no per-cycle register claim is made.

Required action / AC: Run the same contract against doubles and both real links, with no-snapshot inertness evidence.

**N05 PASS [medium]: Payload validation and effective drive resources.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:56`.

Evidence: D0/D1 reject truncated or invalid DRIVE flags/counts before launch and verify final nonoverridable NONE flags; D5 records post-load type/TDE and continuation. `resources_get_int` reads registered `unit->type` (`drive-resources.c:273-275`); the reader writes that field, so the observation is feasible. The setter detaches images at lines 158-162 and changes bus state; `iecbus.c:512-547` confirms routing effects.

Assessment: The prior payload/resource observation AC now has owners. Preservation of explicit media context is strengthened in N12 rather than assumed from type/TDE alone.

Required action / AC: Reject zero/out-of-range drive counts under the pinned format, retain effective resources and state the approved media context on both hosts.

**N06 PASS [medium]: Helper-only and managed-fake controls.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:49`.

Evidence: R6/D0 name direct plain Fact, helper/provider-only, pure managed `IViceNative` fake and string-only controls. The live semantic search returns 56 candidate files, including plain Fact/theory callers and nonconsumer string mentions.

Assessment: The controls now distinguish transitive native consumers from interface-only managed tests; the count remains candidates, not a complete method inventory.

Required action / AC: Tag per method and retain method IDs and Phase E/F owners; do not use a broad file/class exclusion.

**N07 PASS [medium]: Absolute budget and complete interval ownership.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:79`.

Evidence: `initcmdline.c:279-286` assigns an absolute clock limit; `mainc64cpu.c:813-818` and `mainviccpu.c:640-645` test it after an instruction with `>`. D1 now requires restored-clock plus interval above the prelude, gate permits throughout, and nonempty expected continuation. D5 rejects prelude-only/early/truncated/missing/extra/reordered output.

Assessment: The original unspecified-n/short-prefix defect is addressed as a contract. The low-clock and instruction-overshoot boundary still deserves the explicit additive AC N13.

Required action / AC: Prove the budget precondition and full expected window on both hosts; do not infer exact termination from `limitcycles`.

**N08 FAIL [high]: D1/D2/D5 test binding and prerequisite order is not closed.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:79`.

Evidence: D1 says the VSF Slice 1 tests are written against builder receipts before further builder changes. D2 adds the three projects and runs the whole Nuke Test selection with only R9 failures. D5 owns provisioning and real validation of the generated xvic fixtures and manifest; D1 can already establish wrapper inertness, but does not provision those fixture inputs. The live Nuke filter (`Build.cs:112`) excludes no `NativeViceSnapshot` category.

Assessment: A fresh executor has no stated D2 binding/activation allocation: real-bound fixture prerequisite tests see missing future artifacts and violate A05, while leaving them mock-bound requires an explicit later same-case real binding contract. The plan cannot rely on stale host artifacts or an undeclared filter to bridge this phase dependency.

Required action / AC: Allocate real fixture/manifest provisioning and same-case binding before the D2 full gate, or explicitly schedule those real-binding tests into D5 while retaining the complete D1 contract gate and all cumulative D5 obligations. State per-project/category/test IDs at both boundaries; no skips or silent exclusion.

**N09 FAIL [high]: Task 4 exit conflicts with the D3 done-state hold.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:95`.

Evidence: Exit:95 requires `PLAN-VICEORACLE-001` task 4 marked through the TODO wrapper. Section 5:102 says status changes stay pending until step 12. Handoff v2:173 and lines 281-285 defer done states to the final code-complete review, after Phase E and later phases, whose entry requires D.

Assessment: Either task 4 is marked before the required review, violating D3, or Phase D cannot satisfy its exit until a later review that itself depends on leaving Phase D. This is an internal gate cycle, not missing implementation evidence.

Required action / AC: Make D5 record executed evidence while leaving task 4 and task 5 pending; allocate task 4 done-state to step 12 after the required AGREE. Propagate the wording to Exit, section 5 and TODO checkpoint instructions.

**N10 ADD-AC [medium]: D1/D5 `SnapshotObservationHistory_MatchesLoadedBoundary`.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:90`.

Evidence: `obs-emitter.c:23-31,49-63` retains process-local `cycle_index` and `ba_low_run`. Snapshot loading restores VICE, not those wrapper fields. A writing run and a resumed run have different prelude lengths, and a resumed BA-low/stolen prefix can require history absent from the prelude.

Assessment: Ordered `VICE_SNAP` records solve event identity but do not by themselves align process-relative half-cycle indices or prove correct derived AEC at the first restored cycles. This is an added observable AC, not an observed native failure.

Required action / AC: D1 mock and D5 real cases must compare the entire approved post-checkpoint window from its first half-cycle, preserving raw process indices/provenance, with correct AEC for a BA-low/stolen first access and unequal/reused prelude clocks. Reject a shifted window, discarded warmup prefix and stale observational history; identify the checkpoint clock/marker used for each origin.

**N11 ADD-AC [medium]: D0/D2 `NativeLoadAudit_FailsClosedAndStaysOffHotPath`.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:80`.

Evidence: The combined resolver is installed only by `ViceNative` static initialization (lines 29-33), while `ViceNativeXvic` has a separate availability/create path (lines 31-48,141-153). Returning zero also permits runtime fallback probing. The plan names a failing-path control without specifying failed-load versus failed-log behavior.

Assessment: The detector must cover each process initialization order and be unable to turn an unwritable or absent log into a zero-load pass. The env-gated diagnostic must not introduce per-cycle allocations or alter native step behavior.

Required action / AC: Use fresh-process controls with xvic first, x64 first, alias/fallback and failing loads; require the audit to observe every attempt. An unwritable log or missing retained gate log fails the gate. With the variable unset and during repeated step calls, prove no logging work/allocation and unchanged deterministic behavior. Keep positive controls in separate logs.

**N12 ADD-AC [medium]: D1/D5 `ManifestAndFixtureProof_CoversAllPinnedFields`.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:88`.

Evidence: VSF plan:231-234 requires full framing, fixture metadata, restore order, exact fields/functions/adapters/test IDs and blank/duplicate rejection. D5:88 describes its manifest test over names/versions. D5:86 pins one supplied C64 file and two new xvic files, while line 90 additionally requires real external v1.3 resume. Type/TDE alone does not identify media detached by the precondition.

Assessment: The canonical schema remains normative, but the abbreviated map leaves several assertion/provenance outcomes implicit. Adding the explicit AC preserves existing semantics rather than inventing a smaller module-name gate.

Required action / AC: Retain a per-line Slice 1 ledger and independent omissions/duplicates for every schema-1 required field, source/function/managed-field/adapter/test edge, fixture hash/length/order, ROM/media hash and restore order. Pin the actual external v1.2/v1.3 input paths and provenance before real acceptance, distinguish synthetic byte cases, and record effective media identity or explicit no-media context after load.

**N13 ADD-AC [medium]: D1/D5 `ResumeBudget_HandlesLowClockAndInstructionOvershoot`.** Source: `docs/plans/PLAN-NATIVERESTORE-001.md:79`.

Evidence: Both `c64.c:1070` and `vic20.c:951` initialize autostart with 3 seconds. `autostart.c:1434-1436,1528-1537` delays loading; the CPU limit is checked before `autostart_advance` and only after `maincpu_clk > limit`. A 100000-clock fixture plus a short window can terminate in the prelude, and instruction termination can overshoot the last required sample.

Assessment: The new budget rule rejects that outcome but does not pin a low-clock positive case, the comparison interval length, or how retained tail cycles relate to the required window. A large legacy clock alone does not exercise this boundary.

Required action / AC: For each required fixture/host, record the prelude limit evidence, exact positive continuation interval, overflow rejection, permit count/protocol and final observation window. Add correct low-clock and restored-clock-above-budget cases plus one-instruction overshoot, prelude-only, overflow and truncated-baseline negative controls. Every required sample remains checked; retained tail evidence is explicitly outside the compared window, never silently dropped.

Requirements and scope: `FR-VICE-001 -> TR-VICE-ORACLE-001 / TEST-VICE-001` mappings and `FR-SNP-005` snapshot mappings were read from the supported store. `TEST-VICE-001` AC1/AC2 remain clean-target/nonmutation gates in D4/D5; its real bus-observation criteria remain fail closed. `TR-VSFLOCKSTEP-RESUME-001` AC1 keeps all legacy byte, real resume and v1.4 native round-trip obligations; AC2 may change the builder reference while preserving the source/materialization contract. `FR-SNP-005` AC3 and `TEST-SNP-002` AC4 remain full restored-state/100000-cycle obligations, only partly covered by this fixture. Managed `TR-CYCLE-001` AC4 and task 5 remain pending Phase E and the final required review. Task 4 must likewise retain D3 pending status until step 12 (N09). H6 reset and H9 sound ownership remain Phase E/F; no native correction is retained in the oracle.

Structured what-if and chained-rule audit:

- Host checkpoint clocks differ: R12 requires both hosts equal before freezing and rejects any later mismatch. It must not silently retarget literal 100000; F05.
- Pending IRQ/NMI before checkpoint: active `6510dtvcore` runs the interrupt sequence before its trap. Trigger and callback clocks may differ; re-arming at the callback clock may defer the trap another instruction; F05.
- Checkpoint requested within a stolen run: the existing stolen-cycle wrap exposes every clock, but the trap waits for a later dispatch boundary. Preserve the requested cycle or stop under R12, never label it the requested interior snapshot; F05.
- First resumed access is stolen: N02 permits idle/stolen prefix and requires first CPU access at PC. N10 additionally checks derived AEC/history from the first sample and marker-relative window origin.
- Shim is reached only through a data provider: R6/N06 must tag its test method transitively while retaining the pure managed fake. N11 audits fresh-process initialization and fallback paths.
- Load log cannot be written: retained-empty-log evidence is mandatory; N11 makes this a named failing gate and forbids treating no file as zero attempts.
- The twenty known-red set changes: A05 fails equality for an improvement, new failure or equal-size swap. Retain IDs/TRX and obtain a disposition, never silently update the baseline.
- Windows autocrlf checkout and interrupted restore: A04 uses Git-clean status for already restored bytes, preserves raw copies, resumes untouched paths and rejects unexpected/staged drift.
- Crash after first host restore or after both: A03 stops before E, retains originals/evidence and pinned restored source, with no automatic reapply or cleanup.
- Full CPU load succeeds but a later module fails: ordered resume then `load,-1` and reset is rejected; no full-load claim from CPU registers alone, N02.
- Small restored clock versus autostart prelude, or instruction limit overshoot: N13 requires both a successful full interval and explicit rejected boundaries.
- Fresh D2 run before D5 fixture generation: N08 requires a closed test-binding/prerequisite allocation without stale artifacts, new skips or a hidden filter.
- D3 combined with the task-4 Phase D exit: N09 exposes the early-done/deadlock alternatives and moves the done write to step 12.

Persistence: The complete prose and JSON, every disposition and both scores were primary-persisted in this own turn as 17 ordered content chunks. A supported history query reassembled both files with exact SHA256 equality:

- Prose: `0131157664812BCE2E7B08E0440F0A6C6E8368A24595CE8C67B7F42E79C98AC2`
- JSON: `D108CBA76A50D87CAB084D4A044E042A5D4896986AE1F6583AC8A036BB4D4D23`

No LF/CRLF normalization was required. The completed-history query proves this exact request is `completed` and its full response bundle matches SHA256 `98A1930C4FD9C645A9CA33EA481048E89C658AB8477084CCB38F84526E154AF2`. Proof is retained in `verdict.persistence.json` and `completed-history.txt`. The stop gate returned no block, with retained startup warnings about the receipt path exceeding a Unix socket limit. Historical queued records remain untouched.

Final integrity: all 36 pins match; all 163 inventoried native paths and 28 inspected materialized sources remain unchanged; HEAD remains unchanged; `git diff --check` exits 0; outside-receipt Git status is unchanged. No commit or push.