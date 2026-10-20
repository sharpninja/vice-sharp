2026-10-20T13:44:21 - **DISAGREE: PLAN-NATIVERESTORE-001 revision 2.**

**Accuracy 92%; completeness 84%. PASS 18, FAIL 6, UNKNOWN 2, ADD-AC 4.** Phase D is not approved by this review.

Saved: [verdict.md](/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T131300-plan-nativerestore-001-r2-gpt61sol/verdict.md), [verdict.json](/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T131300-plan-nativerestore-001-r2-gpt61sol/verdict.json), [verdict.persistence.json](/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T131300-plan-nativerestore-001-r2-gpt61sol/verdict.persistence.json).

Accuracy 92: the corrected baseline, preservation rules, caller retirement and approved gate amendments are supported, but consumer completeness, exhaustive hunk claims and documentation validation remain inaccurate. Completeness 84: the H5 dependency, incomplete real VSF obligations, missing observable outcomes and unavailable primary audit/store evidence prevent execution readiness. These are holistic judgments, not test pass rates.

Reviewer: **Codex, gpt-6.1-sol, max**.

Reviewer IDs: `Codex-20261020T181838Z-hv-nativerestore-r2` / `req-20261020T181838Z-hv-nativerestore-r2`.

Parent: `ClaudeCode-20261020T015046Z-plugin-session` / `req-20261020T172116Z-prompt-6c6d`.

The reviewer IDs do not establish a persisted primary turn.

Class 1, plan review only. All 26 input hashes and lengths match. Requested commit: `4c5cb166`; initial observed HEAD: `7f931d62`; final observed HEAD: `8295696d`. Additional commits contain review packaging, handoff and Windows trace receipts; no pinned subject changed. The profile was read in full. No marker or API-key file was read. No native/full-solution build, managed test run, nested reviewer, native mutation, commit or push was performed. Historical test results remain attributed to their receipts.

**P01 PASS [info]: Input pins.**

Evidence: All 26 SHA256/length pairs matched before analysis and at closing verification. Subject hash: `9AE793C5DE8C20FD8C5CC5B567F5C6E1D07FC9AC8EEB59C379E79CDF612038D2`. Six materialized source files also match the pinned Git blobs.

Assessment: The reviewed artifact and source are independently identified. Retain the pins and disclosed checkout drift.

**P02 PASS [info]: Native starting state.** Source: plan line 7.

Evidence: Gitlink and native HEAD both equal `cc67418cba58e71d8e70fd353e9de47199ad2075`; live state has 8 modified tracked files, 222 insertions, 8 deletions, no staged native diff and 155 untracked paths. The 163 inventoried paths remained byte-identical throughout this review.

Assessment: The SHA, tracked paths and totals agree with live state. Repeat these measurements independently on both hosts at D0.

**P03 PASS [info]: R1 B and R4.** Source: plan line 38.

Evidence: R1 B, D4 and Exit explicitly retain the classified untracked inventory and change `.gitmodules` from `ignore=dirty` to `ignore=untracked`. The current file still has `ignore=dirty`; implementation has not occurred.

Assessment: The authorized exit permits untracked entries inside the submodule while the superproject reports no submodule dirt. Keep the amended tracked-clean plus inventory-equality gate; deletion and `info/exclude` are not required.

**P04 PASS [info]: Plan review, D3 and the implementation hold.** Source: plan line 3.

Evidence: The 12:21 ruling authorizes plan review. D-pre requires both an HV AGREE receipt and explicit implementation release; section 5 retains final acceptance/status changes until step 12.

Assessment: Plan HV and postponed implementation HV are distinct. This review grants no execution release.

**P05 PASS [info]: D-pre installation receipts.** Source: plan line 62.

Evidence: Both original installation READMEs identify Nuke installs of version `1.3.111` from `2c8d60284229b9fedc2d50f840153b9e8d8cbcbf`, with binary hashes. Windows used `InstallMsi`, which ran `PublishMsi`.

Assessment: The recorded installs use the approved route. This verifies receipts, not current installations or GUI launch. At entry, retain the accepted installed versions/commits and explicit release required by A02.

**P06 PASS [info]: Bounded Phase C evidence.** Source: plan line 13.

Evidence: The builder confines work/build/bin output to `artifacts/immutable-vice`. Linux S2b/S4 and the newly available Windows S3/S4c receipt record observer builds and 100k/1M/4M equivalence evidence. Windows 4M is now recorded as completed at 13:26; the pinned handoff retains its earlier deferral.

Assessment: This supports external building, bus observation and bounded reset-workload equivalence. It establishes neither a replacement DLL ABI nor native VSF resume or managed parity. Refresh D-pre’s trace matrix from the actual receipts.

**P07 PASS [info]: Preservation and narrow restore authority.** Source: plan line 63.

Evidence: D0 copies tracked modifications and injected source. D4 requires a committed preservation receipt, explicit per-path checkout, no reset/clean, and retention of every untracked file. The historical inventory contains 2,527 paths; the preserved header hash matches `5450FDA8CFEDB8A22BADA8CD04EE75FF830C48796592FF9EC0141E899D846259`.

Assessment: Copy/hash/retain and approved legacy-builder retirement remain compatible. Preserve originals and both host receipts; infer no deletion or automatic patch-reapply authority.

**F01 PASS [medium]: Corrected untracked count.** Source: plan line 8.

Evidence: Revision 2 states 155 untracked and 163 total porcelain entries, matching `git -C native/vice status --porcelain=v1 -uall`. D0 explicitly inventories hidden and ordinary paths.

Assessment: The round 1 defect is corrected. Use complete per-host `-uall` enumeration during execution.

**F02 PASS [high]: Windows harness caller retirement.** Source: plan line 65.

Evidence: The live harness csproj defines `ViceNativeProject` at line 10 and invokes `BuildNative` at line 72. D2 explicitly removes both, retires the project and three legacy builders, and requires both-host build-log evidence that the retired path did not run.

Assessment: The omitted executable caller now has a concrete disposition. Scope the no-reference check to active build/executable references; historical receipts may retain names.

**F03 FAIL [high]: R6 consumer inventory and gate denominator.** Source: plan line 45.

Evidence: The token grep reproduces 43 files, 5 with Category and 38 without, but it is neither complete nor semantic:

- `SnapshotResumeSpikeTests.cs:47-108` uses plain `[Fact]` and `ViceNative`, outside the 43.
- `Vic20NativeLockstepTests.cs:18-46` is outside the 43 and uses `ViceNative.CreateInstance` and `XvicOraclePrerequisites`.
- `XAudio2AudioMathTests.cs:26-29` matches documentation saying there is no `ViceFact`.
- `XboxTestConventionTests.cs:159-173` contains matching sample strings.

The proposed proof checks selected `ViceFact`/`ViceTheory` methods and `vice_x64` loads, rather than all direct/indirect consumers and both DLLs.

Assessment: Literal tagging can exclude managed tests while leaving actual native consumers selected. Linux skips/failures and Windows dirty-DLL execution can survive the inventory pass.

Correction: D0/D2 must enumerate actual test methods and transitive fixtures/providers using `ViceNative`, `ViceNativeXvic`, bridge APIs and native instances. Exclude only authorized shim-bound cases, record each Phase E/F owner, and prove selection and absence of both DLL loads. Include missed-plain-Fact and string-only negative controls.

**F04 FAIL [high]: H1-H11 classification accuracy.** Source: plan line 24.

Evidence: The Oct 4 patch contains 20 hunks in 12 files.

- The `mainviccpu.c` hunk at patch line 230 installs host-register bootstrap. H7 describes hosted `CLK_INC` as H1/H2 and does not classify this H3-like resume behavior.
- Pinned `drive-snapshot.c:135-162` creates every DRIVE module before testing TDE. H5 incorrectly says a no-drive unit is omitted.
- `has_tde` is payload byte 0, not a module-header field.
- Section 2 says only H5/H9 are behavior corrections although H6 is also one.
- H9 says no Phase D-G workload compares VIC sound, despite handoff Phase F item 6 and existing native audio cases.
- The 1,432 historical dependency files are `.Po`, not `.po`.

Assessment: Several old omissions are corrected, but the exhaustive and reachability claims remain inaccurate.

Correction: Map all 20 hunks explicitly, including xvic bootstrap. Correct the writer/reader and payload facts, three-correction count, extension spelling and Phase F audio ownership. Keep R7’s native-hunk rejection while preserving existing obligations and blocking required-uncovered behavior.

**F05 FAIL [high]: R8 and complete VSF Slice 1 semantics.** Source: plan line 68.

Evidence: `PLAN-VSFLOCKSTEP-001:325-350` requires both-machine prerequisites, fixture provisioning and deterministic regeneration, the deterministic-module manifest, v1.2/v1.3 native resume, v1.4 native round-trip and the cumulative `NativeSnapshotCompatibilityTest` target. TR AC1 requires the real resumes/round-trip. `X64ScSnapshotCompatibility.cs:28-30` explicitly disclaims native proof.

D1 names test families and a future amendment; D2 replaces commands; D5 concretely specifies an x64sc autostart clock/PC test. It does not allocate the native writer/round-trip, xvic fixture generation, manifest/cumulative-target execution or deferred S5 register comparison to concrete observer-capable steps. It also does not state how normalized v1.2/v1.3 bytes reach the unmodified reader.

Assessment: Replacing libraries with observer executables changes the interface, not the existing acceptance obligations. Clock/PC agreement cannot replace complete resume, round-trip or register evidence.

Correction: Before execution, finish the Slice 1 amendment with an obligation-to-step/test/immutable-command/evidence map for both machines. Specify normalization, native writing, real versioned evidence, fixtures, manifest, cumulative gate and the retained register obligation, or obtain an explicit scoped requirement/scheduling amendment.

**F06 PASS [high]: R9 known-red exception.** Source: plan line 48.

Evidence: The diagnostic receipt names 20 failures: 4 CPU/NMI and 16 visible-PC. The Nuke filter selects these uncategorized classes. R9, D2, D5 and Exit require exactly that named failure set, no other failures and no skips, without filtering out the 20.

Assessment: The explicit operator exception removes the Phase D/Phase E green-gate cycle. It remains a baseline exception, not a passing full-suite claim. Compare case identities at both runs; improvements or substitutions also change the authorized set.

**F07 PASS [high]: Pre-overwrite and resume rules.** Source: plan line 67.

Evidence: D4 checks the index and each live hash against preserved or already-restored bytes before overwriting; unexpected state stops. It checks cached/working diffs, rehashes copies, distinguishes restored/untouched/unexpected paths and names the required drift, partial-run and receipt Pester controls.

Assessment: The round 1 unsafe-overwrite and undefined-resume gaps are resolved at plan level. Implement that contract against the native repository on both hosts; A04 strengthens its identity and checkout-byte controls.

**F08 PASS [high]: Hash/length inventory bracket.** Source: plan line 63.

Evidence: D0 now records hashes and lengths for all untracked paths and modified tracked files. D2/D4/D5/Exit compare paths and content, with an existing-untracked-path overwrite negative control. D4 separately accounts for the authorized tracked restoration.

Assessment: Changed content can no longer hide behind unchanged porcelain paths. Retain both-host inventories and compare the appropriate pre-build or post-restore baseline, including additions/removals.

**F09 PASS [medium]: Pester, analyzer and WhatIf ownership.** Source: plan line 60.

Evidence: The existing builder and equivalence runner still lack `SupportsShouldProcess` and create directories immediately. D1 now explicitly owns mock-first no-side-effect Pester cases, the implementation change, analyzer zero findings and dry runs before real execution. Installed review tools report Pester 5.9.1 and PSScriptAnalyzer 1.25.0.

Assessment: This is now an implementation prerequisite rather than an assumed existing capability. Apply the stated mock gate to all side effects, then bind the same cases to both hosts. Approved native-toolchain shell use does not itself violate PowerShell orchestration.

**F10 FAIL [medium]: Comparator XMLDoc enforcement and AC4 scope.** Source: plan line 68.

Evidence: D5 restores `REAL-C64-PHI2-AEC-001` and the XMLDoc item, but claims `XmlDocsConventionTests` enforces the comparator correction. That test scans attributed test methods only (`XmlDocsConventionTests.cs:38-40,56-74`). `LockstepValidator` is an ordinary class with a class summary at lines 15-18, so it is not checked.

The comparator remains IViceNative/register/CIA based; observer-field comparison belongs to Phase E item 1. TR-CYCLE-001 concerns the managed core. A native-only trace assertion is supporting reference evidence, not full managed AC4 satisfaction.

Assessment: The missing exit items are restored, but the proposed validator cannot enforce the documentation claim.

Correction: Verify the comparator documentation directly or remove the false enforcement claim. Describe current behavior and the future half-cycle contract accurately. Retain the real trace gate and keep managed AC4 pending until managed evidence exists.

**F11 PASS [medium]: Blast radius and reference-update ownership.** Source: plan line 74.

Evidence: The inventory now names the actual caller, project, one PowerShell builder and two shell launchers for retirement, observer scripts, and preservation/restore scripts. No exclude file/script remains. D1 owns the TR AC2 builder-reference update; D1/D2 own the Slice 1 amendment and command replacement.

Assessment: The contradictory R1 A inventory is gone. The AC2 name substitution can preserve clean-materialization/external-wrapper semantics; it does not resolve AC1 under F05. Complete canonical/generated propagation and the supported traceability audit during implementation, reporting existing debt separately.

**R01 PASS [high]: Round 2 credential handling.**

Evidence: No marker, API-key file or raw round 1 response was read, copied or printed. Only the supported wrapper accessed its marker internally. Output was captured and checked before display.

Assessment: The round 1 credential-read failure was not repeated. This does not erase the prior exposure or claim rotation. Retain the no-marker-read rule and keep the old raw stream private.

**U01 UNKNOWN [high]: Primary reviewer persistence and completion.**

Evidence: Status succeeded, but bootstrap/beginTurn failed with:

`MCP_UNTRUSTED: marker refresh failed before REPL request: MCP_UNTRUSTED: Health check failed - Permission denied`

Exactly one supported triage attempt failed identically. History queries also failed. `openSession` returned `persisted:false`; `beginTurn` returned rejected/persisted:false. Update and completion were rejected because no session metadata was cached.

Assessment: Primary full-content persistence, readback and completed-state proof remain unavailable. Local fallback records are not server receipts.

Correction: Keep the verdict queued only and acceptance blocked unless explicitly waived. Later authorized recovery must persist/read back the whole verdict and prove completion. No trust repair, historical replay or cancellation is authorized here.

**U02 UNKNOWN [medium]: Effective requirements and TODO tasks.** Source: plan line 63.

Evidence: Canonical FR-VICE-001 → TR-VICE-ORACLE-001 → TEST-VICE-001, TR-VSFLOCKSTEP-RESUME-001 AC1/AC2 and TR-CYCLE-001 AC4 were read. D0 now owns effective mapping and task 4/5 readback. The trust failure prevents attesting current layers, mappings and statuses.

Assessment: Future readback ownership is corrected, but it is not current store evidence.

Correction: Retain exact effective mappings and both task states through supported wrappers before implementation depends on them. Do not infer store completion from projections.

**A01 PASS [medium]: `DeltaComparison_ClassifiesEveryCurrentHunk`.** Source: plan line 63.

Evidence: D0 preserves/maps each current hunk, lists explained host/context differences, blocks unclassified or required-uncovered behavior, and names missing-hunk/wrong-host controls.

Assessment: Different LEGION2 bytes no longer imply either failure or overwrite permission. Apply this to the fresh host delta; F04 remains a defect in the reference classification.

**A02 PASS [medium]: `EntryEvidence_BindsBothHostsAndExplicitRelease`.** Source: plan line 62.

Evidence: Entry requires both installed commit/version pairs, the Windows trace matrix or explicit deferral/owner, this plan’s AGREE receipt and the operator’s implementation release.

Assessment: Receipt arrival or plan approval cannot lift the implementation hold. Use the new Windows 4M result while retaining explicit release.

**A03 PASS [medium]: `PostRestoreFailure_StopsWithPreservedEvidence`.** Source: plan line 67.

Evidence: Failure after one or both hosts stops before Phase E, retains originals/receipts, reports each host/path and leaves restored source pinned. Automatic patch reapply and untracked deletion are prohibited; both failure positions have named tests.

Assessment: Recovery evidence and mutation authority are separated correctly. Retain that stopped state; later mutation requires scoped authority.

**N01 FAIL [high]: Reachable H5 fixture and phase dependency.** Source: plan line 24.

Evidence: Independent inspection of the required fixture, hash `5C284F5CF0A1BC7E01A2E4117A74158B7A9D60485F5626448F4B7655C0E23C19`, finds:

- `MAINCPU` 1.2, clock `109163946`, PC `22377`.
- `DRIVE8` 2.0 at offset `66204`, inclusive length `24`.
- Payload `has_tde=0`, `has_drives=1`.

Pinned `drive-snapshot.c:317-319` leaves the clock arrays uninitialized, reads them only under TDE at lines 368-419, then copies them for configured non-NONE units at lines 540-550. Missing modules also take the TDE-off continue path.

H5 schedules fail-closed rejection only in Phase E, while D5 and AC1 require real resume of this fixture before Phase E.

Assessment: This branch already occurs in an approved input. A later rejection neither establishes safe earlier loading nor preserves mandatory resume. Module headers alone cannot establish TDE/configured-drive preconditions.

Correction: D0 must report this fixture and stop required-uncovered execution before D4/D5. Resolve the safe external disposition and prerequisite order before affected native loading, including missing modules and configured unit types. If a required fixture is rejected, resolve its existing resume obligation explicitly. Keep the native hunk rejected under R7.

**N02 ADD-AC [medium]: D5 `SnapshotResume_SelectsUniqueSuccessfulLoadBoundary`.** Source: plan line 68.

Evidence: `autostart.c:1552-1575` opens the snapshot, reboots and waits for a deferred load trap; delay/randomization is at lines 1528-1537. MAINCPU restoration precedes register import after the trap. The fetch path loads PC before incrementing the clock and reaching the hook; LOAD can first steal cycles. Snapshot failure triggers CPU reset. `VICE_OBS` has read/write/dummy kinds, not a fetch tag.

Assessment: `clk+1` is a sound narrow expectation for a successful, constrained load. It is not a universal unique-success locator or complete register proof.

Add AC: Bind a unique observed resume boundary and successful load outcome to the exact normalized fixture. State valid state preconditions and the first observable fetch rule. Mock first, then run the same real cases: accept the constrained positive case; reject absent/ambiguous boundaries, wrong clock/PC, unrelated reset, later-module failure and missing prerequisites. Do not infer complete resume from two fields.

**A04 ADD-AC [medium]: D4 `Restore_RejectsWrongPinnedIdentityAndResumesCheckoutBytes`.** Source: plan line 67.

Evidence: D4 describes already-restored bytes as the index version and checks gitlink/checked-out equality at the end. It spans two repositories and Windows/Linux representations.

Add AC: Wrong repository, changed recorded/native/gitlink identity or wrong-host receipt stops before the first overwrite. Resume recognizes the actual bytes checkout produces on each host, including configured line-ending conversion. Prove both-host positive resume and these negative controls.

**A05 ADD-AC [medium]: D2/D5 `KnownRedGate_RequiresCompleteExecutionAndStableDiscovery`.** Source: plan line 65.

Evidence: R9 defines failure-set equality but does not explicitly bind it to discovery, complete execution and a normal nonzero Nuke result caused only by those failures.

Add AC: Retain selected case IDs and terminal results for every project. All selected cases execute; skipped, aborted and not-executed counts are zero. The only nonzero outcome is the exact R9 set. Reject swapped equal-sized sets, missing projects, discovery drift and partial/aborted runs. Report intentional R6 denominator changes separately.

**A06 ADD-AC [medium]: D1/D5 `ObserverBuild_RepeatAllRetainsUsableLinkEvidence`.** Source: plan line 68.

Evidence: D5 requires all six stages on existing build trees. Make extracts link lines only from current stdout, overwrites `stock-link-lines.json`, then throws if none was emitted (`build-vice-observer.ps1:220-231`). Successful up-to-date make can emit no link command.

Assessment: This is a static inference; no prohibited build ran.

Add AC: Repeating All after success either completes with verified usable link evidence or stops with an explicit safe prerequisite before replacing required prior evidence. Cover up-to-date output and interrupted-stage resume, mocks first in D1 and real rerun in D5. Retain originals until authorized disposition.

**R02 FAIL [medium]: Reviewer progress timestamp contract.**

Evidence: My first progress object used `2026-10-20T00:00:00` without measuring it. I disclosed the placeholder and used measured PowerShell timestamps thereafter. Some progress intervals also exceeded the working communication cadence.

Assessment: Later correction cannot make the first timestamp measured. This is my review-process failure, not an implementer plan defect.

Correction: Retain the deviation. Future bootstrap must measure before emitting the first progress object and maintain progress during drafting as well as inspection.

Requirement coverage remains:

- FR-VICE-001 → TR-VICE-ORACLE-001 → TEST-VICE-001: D0/D4 identity and dirty-state checks; D1/D2/D5 outside-tree building and unchanged-source evidence. TEST AC1/AC2 remain obligations, pending final acceptance.
- TR-VSFLOCKSTEP-RESUME-001 AC1: F05/N01 block complete real resume and round-trip claims. AC2’s reference-update ownership passes under F11.
- TR-CYCLE-001 AC4: D5 restores supporting native PHI2/AEC evidence under approved A2. Managed gating and full-field comparison remain Phase E work.
- PLAN-VICEORACLE-001 tasks 4/5: D0 owns current-state readback; U02 prevents current store attestation.

The 20-hunk audit maps c64cpusc include/CLK_INC to H1; mainc64cpu include/stolen-loop to H2, bootstrap to H3 and legacy read to H4; drive initialization to H5; both drive reset hunks to H6; xvic includes/CLK_INC to H7, with the mainviccpu bootstrap omission under F04; reSID APIs to H8; two sound guard/clamp hunks to H9; sound exports/header to H10; and injected header/config to H11 outside the tracked patch.

The required what-if pass found:

- Interrupted revert and staged drift: F07/A03 pass at plan level; A04 strengthens pin/root and checkout-byte outcomes.
- Different LEGION2 delta: A01 passes; unclassified or required-uncovered behavior blocks restoration.
- TDE-off fixture: N01 fails; the supplied fixture already contains it.
- Missed consumer: F03 fails; direct plain-Fact and xvic users escape the seed grep.
- Existing untracked content overwritten: F08’s hash/length bracket detects it.
- Submodule’s own untracked status: intentionally permitted by R1 B.
- Known-red set changes: R9 rejects changes by identity, including improvements; A05 adds complete execution proof.
- Snapshot/reset ambiguity: N02 adds a discriminating load-boundary AC.
- Failure after one/both restores: A03 retains the pinned stopped state.
- Repeated All build: A06 adds the missing existing-tree outcome.

R9 resolves the old green-gate phase cycle. Explicit plan-review authority resolves D3 timing. H5 versus D5 remains a dependency failure; primary audit versus acceptance remains unresolved under U01.

Initial skill/profile reads and analyzer bootstrap used inline PowerShell before file helpers were established. Subsequent MCP calls used the analyzed helper. No Bash, Python or Node command ran. The supported begin wrapper reported a recovery path under `.mcpServer` despite the receipt-local cache override; that side effect is disclosed. No marker, hooks, settings, environment or historical queue was repaired or replayed.

**Persistence is QUEUED ONLY.** Primary bootstrap and the single supported triage attempt failed. Completion was rejected; final history also failed. No primary completed-state proof exists.

The complete prose and JSON were retained in 17 ordered chunks and the supported-helper YAML fallback. Readback reproduced both documents exactly; no CRLF/LF content normalization was required:

- Prose: `EFBFB76A57ED827FD846D8F3C0EAC675C903BCB7142508245E025451E44BF5B8`
- JSON: `F7C22A6F6FB44F17B99C2F39454762E883C46680D4F9E695765C78D461091EE9`

The persistence receipt records the failures, local readbacks and hashes. All 12 review helpers passed parser/analyzer checks. Final native inventories and all 26 pins remain unchanged; `git diff --check` passes.

**OverallVerdict: DISAGREE. Accuracy 92; completeness 84. Neither score meets 98.**