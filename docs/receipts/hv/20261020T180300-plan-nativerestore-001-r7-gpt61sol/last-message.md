2026-10-20T18:47:46 - **DISAGREE. Accuracy 97%; completeness 98%. PASS 50, FAIL 1, UNKNOWN 0; ADD-AC 3.**

**Full FAIL list: N19.** R12 still makes an exclusive register-export claim contradicted by the active BRK path. The public trap checkpoint design remains valid. N20 through N23 pass as revised plan contracts. The three new ADD-AC findings do not independently cause DISAGREE.

Saved: [complete prose verdict](/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T180300-plan-nativerestore-001-r7-gpt61sol/verdict.md), [complete JSON verdict](/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T180300-plan-nativerestore-001-r7-gpt61sol/verdict.json), [persistence proof](/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T180300-plan-nativerestore-001-r7-gpt61sol/verdict.persistence.json).

Reviewer: Codex, `gpt-6.1-sol`, effort `max`, LAB-OMARCHY. Exact model and effort are attributed to the CLI launch receipt; the MCP server’s model header is generic `codex`.

- Reviewer session: `Codex-20261020T230558Z-hv-nativerestore-r7`
- Reviewer request: `req-20261020T230558Z-hv-nativerestore-r7`
- Parent session: `ClaudeCode-20261020T015046Z-plugin-session`
- Parent request: `req-20261020T172116Z-prompt-6c6d`
- [Request stream](/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T180300-plan-nativerestore-001-r7-gpt61sol/request.jsonl)
- [Response stream](/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T180300-plan-nativerestore-001-r7-gpt61sol/response.jsonl)

This is a class-1 plan review before execution. The subject is revision 7. The brief’s revision-4 and round-3b phrases are stale; historical revision-4 dispositions are recorded separately from current findings.

Accuracy remains below 98 because N19 retains an incorrect exclusive source claim after the specific propagation request. Completeness reaches 98 because every round-6 ID, inherited historical disposition, mandatory attack surface, source seam, requirement mapping and requested what-if has an explicit outcome.

All 41 input pins matched before analysis and at final rehash. The plan is 76,497 bytes, SHA256 `5E50D2A1BDD3C8608CABB71D98646C0C324562DEA13DF40AD025DB3978CE6552`. Live `develop` HEAD is `1c46ce70f6702e6955c4b8dddce8f5f2a74f29c0`. The checkout commit is separate live Git evidence; `input-pins.json` does not contain it.

Native gitlink and HEAD remain `cc67418cba58e71d8e70fd353e9de47199ad2075`. All 163 native path/status/hash/length entries match the starting measurements: eight tracked modifications, 155 untracked files, empty index, 222 insertions and eight deletions. Twenty-four inspected materialized source files equal their pinned Git blobs. `git diff --check` exits 0.

The permitted managed command was:

```powershell
dotnet test "/home/sharpninja/github/vice-sharp/tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj" -c Release --filter "FullyQualifiedName~Vic20HalfCycleComparatorTests" --results-directory "/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T180300-plan-nativerestore-001-r7-gpt61sol/TestResults" --logger "trx;LogFileName=vic20-comparator.trx" --diag "/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T180300-plan-nativerestore-001-r7-gpt61sol/TestResults/vstest.diag.log" --blame-hang --blame-hang-timeout 5m
```

Result: **exit 1; Passed 0, Failed 1, Skipped 0**, missing reflected `HalfCycleState` at fixture line 23. This is the acknowledged R13/D2 prerequisite, not a passing implementation gate or an expansion of R9. A separate diagnostic repeat of the same filter with `--no-build --no-restore` produced the same assertion failure and reached/JIT-compiled `ViceNative.FindLibraryPath`; that establishes path-resolution activity, not a loaded-DLL claim.

Every round-6 finding follows. Historical revision-4 dispositions were independently parsed from the actual round-4 receipt and are not evidence for current correctness.

- **P01 PASS**, plan line 3, input identity. All 41 pins match; the live subject is revision 7 with the hash and checkout identity above. The stale brief labels do not change the subject. Retain file-pin and checkout evidence separately. Historical revision 4: PASS.

- **P02 PASS**, plan line 8, native starting state. Read-only Git and content inventories confirm the pinned native commit, eight modified tracked files, 155 untracked files, 222 additions, eight deletions and empty index. Final measurements equal the initial measurements. Measure Windows separately in D0 rather than treating historical Windows counts as current. Historical revision 4: PASS.

- **P03 PASS**, plan line 83, R1 B. `.gitmodules` currently uses `ignore = dirty`; D4 explicitly changes it to `untracked` only and requires empty tracked/index diffs while retaining the untracked inventory. This implements the operator’s source-versus-build-output distinction. Historical revision 4: PASS.

- **P04 PASS**, plan line 78, review authority and implementation hold. D-pre requires both installations, the Windows trace matrix, plan AGREE and explicit operator release. The supplied 12:21 clarification authorizes plan HV. This review does not release D0 or D4. Historical revision 4: PASS.

- **P05 PASS**, plan line 78, desktop installations. Linux `20261020T105400` and Windows `20261020T121000` receipts record Nuke installation of 1.3.111 from `2c8d60284229b9fedc2d50f840153b9e8d8cbcbf`. Git reports no subsequent changes in `src`, `build`, `native` or central build/package props. These remain host-attributed receipts, not fresh remote installation or GUI-launch measurements. Historical revision 4: PASS.

- **P06 PASS**, plan line 13, Phase C evidence scope. The Linux S2b/S4 and Windows `20261020T102600` receipts support seeded reset-workload equivalence at 100k, 1M and x64sc 4M. S5 separately defers the real snapshot register check. D5 repeats the matrix after linking the new wrappers; existing equivalence is not managed parity or snapshot acceptance. Historical revision 4: PASS.

- **P07 PASS**, plan line 79, preservation and retirement. D0 copies, hash-verifies and retains originals; D4 restores only named tracked paths after preservation. R2/R3 authorize retirement, R4 retains untracked files, and A03 prohibits automatic reapply. No review-time restoration, move or deletion occurred. Historical revision 4: PASS.

- **F01 PASS**, plan line 9, counts. The full 2,527-path historical inventory was parsed: 1,432 `.Po`, 745 `.o`, 59 `.a`, two `.exe`, 107 `.in` and 106 Makefiles. Live Linux remains eight modified plus 155 untracked. The original inventory retains its paths; `inspection.json` records parsed counts and classifications. D0 must recompute each host’s current inventory. Historical revision 4: PASS.

- **F02 PASS**, plan line 81, actual native build callers. `TestHarness.csproj:10` defines `ViceNativeProject`; lines 70–72 invoke `Vice.Native.proj`’s `BuildNative` on Windows. The native project invokes the old in-tree builder. D2 removes those executable paths and requires both-host build logs without shim invocation. The two proposed Nuke targets and generator are absent today. Require zero remaining callers before retirement. Historical revision 4: PASS.

- **F03 PASS**, plan line 81, semantic inventory and detector. The live scan finds 43 token-candidate files, five with an existing Category trait, and 56 wider semantic candidates. Plain facts, attributes and helper/provider paths matter. D0 owns the per-method inventory; D2 owns discovery-to-exit auditing for both bindings. This is planned capability, not existing proof. N24 adds a successful ordinary-discovery control; the existing empty-log gate prevents unsafe restoration. Historical revision 4: PASS.

- **F04 PASS**, plan section 2, all twenty hunks. All 599 patch lines and twenty hunk headers across twelve files were inspected. H1–H12 cover hosted exports/bootstrap, MAINCPU changes, drive clocks/reset, SID probes, VIC-20 sound and injected inputs. H5/H6/H9 are identified behavior changes explicitly rejected by R7. Sound divergence has Phase F item 6 ownership. D0 must block on any unexplained live hunk. Historical revision 4: PASS.

- **F05 PASS**, plan line 61, literal checkpoint. R12 requires trigger 100,000 and actual write clock 100,000 on both hosts for each xvic fixture. Mismatch produces `BLOCKER-R12-XVIC-CHECKPOINT`, measured clocks and interrupt/stolen context. No later-clock rearm or approximate acceptance is allowed. Dependent case IDs remain BLOCKED, cumulative exit is nonzero, independent denominators remain visible, and Phase E stays held. The separate global source-claim defect is N19. Historical revision 4: FAIL.

- **F06 PASS**, plan line 62, comparator prerequisite. The permitted test confirms the missing reflected state type. R13/D2 explicitly own `HalfCycleState` and `CompareHalfCycleStates`, preserving the fixture assertions and the exact R9 set. The baseline failure is allocated work, not acceptance evidence. The unchanged fixture and independent per-field discrimination controls must pass before D2 acceptance. Historical revision 4: PASS.

- **F07 PASS**, plan line 83, overwrite and restart safety. D4 checks repository, host, gitlink, clean index and preservation hashes or verified Git-clean restored state before overwrite. Wrong identity, staged drift, corrupt/missing receipts and partial completion have controls. Unexpected state stops before overwrite; only verified untouched paths resume. Historical revision 4: PASS.

- **F08 PASS**, plan line 79, content-aware integrity. D0/D2/D4/D5 compare SHA256 and length for modified and untracked paths, including a changed-existing-untracked-file negative control. Path equality alone cannot hide an overwritten artifact. This review’s 163-file bracket matches exactly. Historical revision 4: PASS.

- **F09 PASS**, plan line 76, analyzer and WhatIf. Current scripts lack the required `SupportsShouldProcess` behavior and create directories. D1 allocates mock-proven dry runs creating neither files nor directories, parser/analyzer checks and Pester before real use. The missing capability is acknowledged and ordered before D5. Review scripts were separately analyzed and mutation helpers dry-run. Historical revision 4: PASS.

- **F10 PASS**, plan line 94, comparator and managed acceptance mapping. R13/D2 correct the fixture summary to `FR-SNP-005` AC3 and `TEST-SNP-002` AC4. D5 native `REAL-C64-PHI2-AEC-001` evidence does not complete managed `TR-CYCLE-001` AC4 or task 5. The class-summary correction has a dedicated check because the existing XMLDocs scanner examines attributed methods. Historical revision 4: PASS.

- **F11 PASS**, plan line 103, AC2 reference change. D1 uses the requirement wrapper and readback for the builder-reference replacement while preserving pinned-source integrity, generated inventory and external-wrapper semantics. R15 separately identifies AC1’s semantic amendment. Read back both ACs and their projections. Historical revision 4: PASS.

- **R01 PASS**, request and response receipts, credential handling. No marker, credential store or API-key file was opened, copied, printed or quoted. The supported plugin consumed its own marker internally. A broad requirement response was stopped by the credential-pattern output guard before exposure or saving; scoped supported reads succeeded. No historical key-rotation claim is made. Historical revision 4: PASS.

- **U01 PASS**, `verdict.persistence.json`, own primary turn. Supported writes returned `persisted:true`, `queued:false`, `degraded:false`. Both complete verdict bodies were read back, reassembled and matched by raw UTF-8 hash before completion, then matched again from completed-turn history. The two historical queued records were neither replayed nor canceled. Historical revision 4: PASS.

- **U02 PASS**, plan line 79, live store state. Seven selected FR/TR/TEST records, both FR mappings and `PLAN-VICEORACLE-001` were read through supported wrappers. All seven requirements remain pending; tasks 4 and 5 are false. Current AC1 still says external 1.2/1.3 before the planned R15 amendment. No requirement or TODO mutation occurred during review. Historical revision 4: PASS.

- **A01 PASS**, plan line 79, host hunk dispositions. Each live hunk must map to H1–H12, an explained host-context difference or a blocking unclassified hunk. Wrong-host and missing-hunk controls prevent treating the historical Windows superset as proof of identical current deltas. Historical revision 4: PASS.

- **A02 PASS**, plan line 78, explicit release. `EntryEvidence_BindsBothHostsAndExplicitRelease` requires both installations, the complete Windows matrix, plan AGREE and the verbatim operator release before D0. Receipt arrival alone is insufficient. D0 remains held under this result. Historical revision 4: PASS.

- **A03 PASS**, plan line 83, post-restore failure. Failure after one or both hosts stops before Phase E, retaining originals, host states and pinned restored source. Rejected patches are not automatically reapplied and outputs are not deleted. Any further recovery needs a scoped operator decision. Historical revision 4: PASS.

- **N01 PASS**, plan line 56, active uninitialized clock-copy path. The approved fixture’s DRIVE8 2.0 payload is `[0,1]`, two bytes. `drive-snapshot.c:315–319` leaves clock arrays uninitialized; lines 368–419 populate TDE-on entries; lines 540–550 copy for non-NONE units. D1 computes `-drive<n>type 0`, places it last and rejects launch without the precondition. This preserves fixture bytes and keeps H5 rejected without pretending to eliminate R14’s separate dead read. Historical revision 4: PASS.

- **N02 PASS**, plan line 91, full-load ordering. Autostart calls `machine_read_snapshot` across objects; both machine readers call `maincpu_snapshot_read_module` across objects. MAINCPU success precedes later modules, and a later C64 failure resets the CPU. D5 requires `resume`, then `load,0` before the next gate, first clock C+1 and first CPU read/dummy at PC after any stolen prefix. CPU-module success alone cannot pass full load. Historical revision 4: PASS.

- **A04 PASS**, plan line 83, autocrlf. D4 separates preserved host-byte hashes from Git checkout equivalence. Already-restored paths use `git diff --exit-code`; a CRLF-converted Windows mock is explicit. Git blob bytes need not equal Windows worktree bytes. Historical revision 4: PASS.

- **A05 PASS**, plan line 81, execution denominator. D2/D5 retain selected IDs and terminal TRX per project, require zero skipped/aborted/not-executed, and permit exactly the twenty named R9 failures. Controls reject same-sized substitutions, missing projects, discovery drift and partial runs. A changed known-red set requires reporting, not silent amendment. Historical revision 4: PASS.

- **A06 PASS**, plan line 80, up-to-date builder rerun. The current Make stage depends on link lines appearing in stdout. D1 now permits retained `stock-link-lines.json` reuse only after object hashes match, otherwise stopping before replacing evidence. Mock interrupted/up-to-date controls precede both-host real `All` reruns. Historical revision 4: PASS.

- **R02 PASS**, current response stream, timestamp handling. Current progress objects and timestamped commentaries use command-measured emission timestamps. The round-6 reused-timestamp failure remains disclosed in that historical verdict and is not retroactively relabeled compliant. Continue measuring, never estimating or backfilling. Historical revision 4: PASS; round 6: FAIL.

- **N03 PASS**, plan line 64, R14. `rotation.c:194` reads the pointer-array value into `speed_zone`; line 203 overwrites it before use. H5 initialized clocks, not that array. R14 explicitly accepts this limited dead read, with two-host continuation evidence retained in D5. This is not a claim of portable defined C behavior or sanitizer cleanliness. Historical revision 4: PASS.

- **N04 PASS**, plan line 57, register observer. Both MAINCPU readers populate `maincpu_regs`. `MOS6510_REGS_GET_STATUS` reconstructs N/Z and the unused bit; the fixture status is `$A0`. D1 covers status combinations, wide clock/PC, exactly-once forwarding, preserved results/state and sink failure. These seams expose resume-boundary registers, not arbitrary-cycle interpreter locals. Historical revision 4: PASS.

- **N05 PASS**, plan line 91, drive payload and media effects. The pinned writer stores `has_tde`/`has_drives` first; missing modules follow the TDE-off path. `drive_resources_type` detaches media, NONE disables the unit, and IEC routing changes. D1 validates payloads; D5 records effective type, TDE and media after load through public APIs. Detached/no-media context remains explicit. Historical revision 4: PASS.

- **N06 PASS**, plan line 79, transitive consumers and fakes. D0 includes direct and helper/provider native consumers while excluding comments, strings and pure managed `IViceNative` fakes. Plain Fact, provider-only, fake-only and string-only controls are explicit. The 56-file scan is a candidate set, not a completed method ledger. Historical revision 4: PASS.

- **N07 PASS**, plan line 91, absolute limit and full window. `-limitcycles` is absolute, and CPU loops check `clock > limit` after an instruction. D1/D5 require W CPU cycles and 2W half-cycles after successful load, with permits, retained overshoot and terminal reason. Prelude-only or short-prefix output cannot pass. Historical revision 4: PASS.

- **N08 PASS**, plan line 80, phase dependencies. D1 creates every D5 Slice 1 family mock-bound as `VsfSlice1Contract`. D5 adds same-assertion `VsfSlice1Real` cases using artifacts produced that run. D2 selects contracts without depending on future real artifacts. N20 now enforces this split.

- **N09 PASS**, plan line 96, pending tasks. Task 4 remains pending until accepted step-12 HV; task 5 remains pending until Phase E/final managed acceptance. Live TODO state agrees. Attaching evidence does not claim completion.

- **N10 PASS**, plan line 91, marker-relative windows. Each stream uses its own marker and retains raw indices. Own BA-derived AEC must match from the first offset, including a stolen first access. Reused history, dropped prefixes and warm-up trimming are rejected. Wrapper-local history is not assumed to be serialized by VSF.

- **N11 PASS**, plan line 81, detector initialization and failure. D2 covers combined resolver and xvic availability/create paths, both first-use orders, aliases/fallbacks and failed loads. Audit starts before discovery; missing/unwritable logs fail loudly; positive controls use separate logs. Availability probing is not mislabeled successful loading.

- **N12 PASS**, plan line 63, R15. The repository contains two identical real MAINCPU 1.2 fixtures and one real 1.4 fixture, no real 1.3. R15 explicitly changes real inputs to 1.2/1.4; D1 owns wrapper amendment/readback, and D5 owns real resume and native-written 1.4 round trip. Synthetic 1.3 remains covered. N26 adds propagation cleanup.

- **N13 PASS**, plan line 91, per-host budgets. Receipts include exact invocation, prelude, restored clock, W/2W conversion, expected permits, low/high-clock controls, retained tail and terminal reason per host and fixture. The compared interval cannot adapt to convenient output.

- **N14 PASS**, plan line 96, BLOCKED lifecycle. Every xvic dependency affected by a non-100,000 write clock has a stable blocked ID and measured host context. Independent obligations run in their own denominators; cumulative exit is nonzero; no blocked case becomes pass or skip. Phase E and done-state remain held. N20 removes the prior ordinary/cumulative selection deadlock.

- **N15 PASS**, plan line 80, armed writer and gate. D1 controls require one gate record/permit per advanced cycle, no advancement while held, unchanged quit/EOF/error behavior and independently measured trigger/write clocks under interrupts and stolen cycles. D5 repeats both hosts. Unarmed equivalence alone no longer stands in for armed behavior.

- **N16 PASS**, plan line 81, discovery interval. The launched `dotnet test` process receives the audit environment before discovery, and the log is never truncated afterward. Fresh attribute/provider attempt controls and a selected managed fake are explicit. N24 strengthens the accepted ordinary-discovery outcome.

- **N17 PASS**, plan line 81, zero allocation and determinism. D2 measures zero incremental allocations and log calls over warmed repeated stepping for both bindings with audit unset, preserves independent deterministic output and rejects allocating/logging doubles. Cold resolver work remains separate from the cycle path.

- **N18 PASS**, plan line 91, units and feasibility. W CPU cycles means 2W half-cycle records. Odd half-cycle requests and arithmetic overflow are rejected. Low restored clock and high legacy clock have explicit controls, permits and retained tail. N23 supplies the reconciled terminal contract.

- **N19 FAIL**, medium severity, [plan line 61](/home/sharpninja/github/vice-sharp/docs/plans/PLAN-NATIVERESTORE-001.md:61), exclusive export claim.

  R11 line 54 now includes DMA and BRK, but R12 still says the pinned VICE exports a correct register file **“only at an instruction boundary.”** Section 7 line 164 claims every exclusive list was replaced.

  Both active CPU sources include `6510dtvcore.c`. Its `FETCH_OPCODE` at line 1804 precedes BRK dispatch at lines 1845–1847. BRK calls `EXPORT_REGISTERS` at line 1012 before `INC_PC` and `DO_IRQBRK`. The fetch implementations in `c64/c64cpusc.c:124–179` have already advanced opcode-fetch clocks.

  This is a concrete export after fetch cycles and before BRK completes. It does not provide an externally requestable arbitrary-cycle checkpoint, and it does not invalidate the selected trap seam. It does invalidate the global exclusive claim.

  **Correction:** replace global export exclusivity with the narrow checkpoint statement: the public trap seam used here runs at the boundary whose copied pending mask includes `IK_TRAP`, with exported registers available. A request inside an instruction may be delayed; acceptance uses the actual write clock. Search R11, R12 and section 7 for all exclusive copies. Round 6: FAIL.

- **N20 PASS**, plan line 81, shared selectors and projection. Live `Build.cs:112` and `:140` carry separate ordinary filters. D2 now explicitly creates one shared selector for `Test` and `CiTest`, retaining existing exclusions and adding `Category!=ShimLegacy&Category!=VsfSlice1Real`. Cumulative selection is `Category=VsfSlice1Real`, retaining stable blocked IDs and nonzero exit. Projection controls run at D2 and after D5 registration with per-gate IDs. `selector-projection.json` models this contract; it is not execution of the future target. Round 6: FAIL.

- **N21 PASS**, plan line 80, generator publication failure. Candidate creation precedes clock acceptance. Mismatch on either/both hosts, malformed/missing checkpoint record, write failure and interrupted retry leave accepted fixture/manifest unchanged, retain evidence, return nonzero with blocked IDs and reject stale artifact reuse. D5 owns real cases. N25 clarifies evidence when native failure leaves no candidate bytes. Round 6: ADD-AC.

- **N22 PASS**, plan line 80, latched pending mask. `6510dtvcore.c:1758` copies `global_pending_int` before `DO_INTERRUPT` at 1763; the macro uses local `ik` at 356 and tests `IK_TRAP` at 408. R12 now names the first boundary whose copied mask contains the trap. D1 explicitly tests a trigger during an already-dispatched interrupt after capture, reporting the actual later callback clock. The handler’s first opcode can precede the next sample; the mock no longer forces an earlier callback. Round 6: ADD-AC.

- **N23 PASS**, plan line 80, reconciled budget and bounded ending. D1 canonically uses C+W unless that lies inside the autostart prelude, then measured prelude length+W. After `load,0` and exactly W compared cycles/2W records, the process must terminate by limit or gate quit within the recorded overshoot allowance. Permit count, tail and exit reason are mandatory. Unsupplied-permit waits, early stopping and stretched windows are rejected. Round 6: ADD-AC.

Three new findings are **ADD-AC**, without an independent safety failure:

- **N24 ADD-AC**, medium, D2/D5, `OrdinaryDiscovery_ExcludedShimMetadataDoesNotResolveLibraries`.

  `ViceFactAttribute` and `ViceTheoryAttribute` query `ViceNative.IsAvailable` during construction. The comparator-only diagnostic reached/JIT-compiled `FindLibraryPath` although the selected comparator body does not use that binding. This proves path-resolution activity, not DLL loading.

  The existing deliberate-attempt controls and empty-log ordinary gate safely stop restoration. Add a positive case proving ordinary discovery with excluded legacy attributes/providers resolves or loads none of the three libraries, keeps the managed fake selected and completes its denominator. Deliberate native attempts must separately log/fail. Include availability/`FindLibraryPath` probes in audit scope, not only `NativeLibrary.Load`.

- **N25 ADD-AC**, low, D1/D2/D5, `FailedWriter_RecordsAbsentCandidateHonestly`.

  `c64-snapshot.c:92–94` and `vic20-snapshot.c:91–93` close and remove a newly created snapshot on module-write failure; file creation can also fail before bytes exist. N21 requests candidate bytes/hashes for write failures.

  Use unique receipt candidate paths. When no bytes remain, record absent/noncreated/removed-by-native-writer with result, `checkpoint-fail` and process logs; never invent a hash. Retain every candidate byte that actually exists, and leave accepted fixture/manifest unchanged. This refines evidence without changing VICE or weakening publication isolation.

- **N26 ADD-AC**, low, D1, R15 propagation.

  R15 and D5 preserve synthetic 1.3 coverage. `PLAN-VSFLOCKSTEP-001:340` defines the synthetic 1.2/1.3/1.4 matrix, rather than only an external-input list. Exit line 96 still mentions the optional real-v1.3-input blocker superseded by R15.

  Require readback showing external real AC1 inputs 1.2/1.4, unchanged synthetic 1.2/1.3/1.4 cases and unchanged AC2 integrity semantics. Remove the obsolete optional blocker phrase and reconcile requirement, plan, projection and blocker lists. The existing explicit preservation clauses prevent a safety gap.

Requirements and phase ownership were checked as follows:

- `TR-VSFLOCKSTEP-RESUME-001` AC1 remains pending. D1 applies the explicit R15 real-input amendment; D5 proves complete native load and full continuation/round-trip windows. Synthetic 1.3 normalization remains.
- AC2 remains pending. D1/D5 replace the obsolete builder reference while preserving immutable-source, approved-generation and external-wrapper semantics.
- `TEST-VICE-001` AC1/AC2 remain pending. D0/D4/D5 own clean-target preflight, rejection of tracked dirt and preservation across build/execution.
- `TR-CYCLE-001` AC4 remains pending for Phase E. Native reference traces and a state comparator do not prove managed PHI2/AEC behavior.
- `FR-SNP-005` AC3 and `TEST-SNP-002` AC4 retain the comparator fixture mapping. D2’s reflected state/comparator is not whole-product snapshot completion.
- `PLAN-VICEORACLE-001` task 4 remains pending until accepted step-12 HV; task 5 awaits managed comparison and the final immutable gate.

The Slice 1 obligation audit covers every substantive line:

- Line 325: D1/D5 explicitly amend DLL prerequisites to pinned observer binaries, proven through link receipts, hashes and wraps for both machines.
- Line 326: D1/D2/D5 provision hash-pinned x64sc and literal-clock PAL/NTSC xvic fixtures, or report the named nonpassing R12 cases.
- Line 327 and manifest lines 230–243: D1/D5 cover schema-1 framing, generator/commit, ROM allowlist, fixture hash/length/model/command/clocks and module/source/managed/adapter/test edges, with omission and duplicate controls.
- Line 328: D1/D5 verify every required Windows MSYS2 command, explicit Linux pass-through and negative prerequisite controls.
- Line 329: D1/D2/D5 require two fresh-run byte comparisons and cross-host equality, isolated candidate failures and no stale artifact acceptance.
- Lines 330–333: the existing 199 byte cases remain unchanged, including synthetic 1.2/1.3/1.4 and malformed layouts, with R10/N05 additions. The 199 count is historical evidence, not a fresh run in this review.
- Lines 334–335: build inputs, generated files and outputs stay outside `native/vice`; original materialization hashes and generated inventory remain distinct.
- Line 336: missing mandatory artifacts fail rather than use `ViceFact` skips. Only explicit R12 case IDs may be nonpassing BLOCKED.
- Lines 338–340: mocks precede implementation, the full corruption/omission matrix remains, and R15 changes real inputs without deleting synthetic 1.3.
- Line 342: provisioning, manifest, immutable external build and bounded legacy adapter remain mandatory; neither runtime patching nor adapter broadening is authorized.
- Lines 347–350: D1/D2/D5 replace retired script commands with the canonical observer builder, fixture generator and cumulative through-slice-1 target.

The structured what-if pass produced these outcomes:

- **Host checkpoint clocks differ:** one-host and both-host mismatches are BLOCKED by stable xvic ID, with both measured contexts and nonzero cumulative exit. No rearm or substituted literal is accepted.
- **Trap requested during a dispatched interrupt:** local `ik` can omit the new trap; a handler opcode may execute before the next sample. Actual callback clock is retained and rejected if later than literal R12.
- **Request inside stolen cycles:** armed gate controls preserve one record/permit per advanced cycle, held permits prevent advancement, and actual write clock remains distinct.
- **First resumed access is stolen:** idle/stolen prefix remains; first CPU read/dummy must be at PC. Own BA history and marker-relative equality apply from the first offset, without trimming.
- **Provider/attribute-only shim consumer:** semantic inventory and pre-discovery audit cover the path. Deliberate attempts log/fail; N24 adds the successful ordinary-discovery case.
- **Missing/unwritable audit file:** attempt or gate fails loudly. It cannot be treated as an empty successful audit.
- **Known-red set changes:** exact FQNs and complete terminal project evidence reject substitutions, improvements, regressions, skips and partial runs against the approved R9 set. Report the change for the operator.
- **Windows autocrlf:** preserved original bytes keep their hashes; restored-state equivalence uses Git. CRLF, wrong-host and unexpected-content controls remain explicit.
- **Crash/restart during preservation/restoration/candidate write:** no overwrite precedes verified preservation; restart classifies restored/untouched/unexpected; later failure stops before E without automatic reapply. Accepted fixtures remain unchanged and stale retry output is rejected.
- **Low restored clock:** the canonical prelude-safe limit, complete W/2W interval, bounded ending, permits, tail and exit reason are required. Waiting for an unsupplied permit or stretching the window fails.
- **Missing runtime/fixture or malformed prerequisites:** the complete mock matrix rejects omissions/corruption. Real artifacts are mandatory and never substituted from stale host paths.
- **Cross-workspace/session evidence:** D4 verifies repository/host/gitlink and committed host preservation. D5 consumes artifacts produced that run. This review uses its own parent-linked turn and exact history lookup.
- **R12 blocked during ordinary/cumulative gates:** shared ordinary selectors retain contracts and exclude legacy/real cases; cumulative selection retains every required real ID and remains nonpassing. The previous selector dependency deadlock is removed.

Source supports the selected checkpoint seam: both active cores use `6510dtvcore.c`; the public trap executes after register export and before import; synchronous `machine_write_snapshot` delegates to native writers serializing MAINCPU from the exported globals and `maincpu_clk`. Both snapshot-read wraps cross object boundaries. This is source feasibility, not native execution during review. Callback timing follows the copied pending mask, and actual write clock/full-load ordering remain D5 acceptance evidence. New `VICE_SNAP` lines are excluded from old no-snapshot equivalence claims; unarmed reruns require none.

Persistence is proven:

- Own turn status is **`completed`** in `history-completed.yaml`.
- Complete prose: 13 chunks, 50,006 bytes, SHA256 `8E36FE96DF037CAB1E56DFE2ACFBE22AB5764773E1B7B5C95C894334E118C9F9`.
- Complete JSON: 18 chunks, 69,612 bytes, SHA256 `81420D0C92C5881F784AB8007026301DF658F0FA64B262CD4C13BFEBEF2C9220`.
- Both raw readback hashes match exactly; **no CRLF/LF normalization** was needed.
- Completed history contains 36 dialog entries and four actions, including the review decision and complete finding bodies.
- Current review content is primary-persisted, not queued. Final supported Status still reports the same two historical queued records; neither was replayed or canceled.
- The required stop gate returned exit 0, `{}` and empty stderr.

No Phase D step executed. No native build or binary launch, full-solution build/test, commit, push, file move or deletion occurred. No marker or credential file was read; no raw REST, Python, Node or nested reviewer was used. Windows runtime obligations were assessed through future contracts and host-attributed receipts, not freshly executed remotely.

**DISAGREE: N19 is the only FAIL; UNKNOWN list is empty. Accuracy 97%, completeness 98%. Implementation remains held.**