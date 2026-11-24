2026-10-20T17:54:17 - Hostile validation round 6: PLAN-NATIVERESTORE-001 revision 6.

OverallVerdict: DISAGREE. Accuracy 97%; completeness 97%. PASS 45, FAIL 3, UNKNOWN 0, ADD-AC 3. The FAIL list is R02 (reviewer timestamp process), N19 (stale false register-export exclusivity) and N20 (gate selection propagation). No Phase D execution or done-state is authorized.

Accuracy is below the threshold because one source claim remains false and this reviewer reused a measured timestamp in one commentary instead of measuring anew at emission. Completeness is below the threshold because the proposed gate filters do not enforce real-test exclusivity or every-gate shim retirement. The three ADD-AC items are additional controls, not independent FAILs.

This is a class-1 pre-execution plan review. All 47 round-5 IDs receive one fresh disposition below; all 35 round-3b IDs also carry their historical revision-4 disposition in the JSON, read from the round-4 verdict only for that historical purpose. Current findings were rechecked against source. The subject is revision 6 despite stale revision-4 wording in the brief. All 41 input pins matched before analysis; input-pins is a file array rather than a checkout-commit pin. Live checkout is develop at ff24117d7d756404fad439a3c14c14336709eb8c.

R15 is an explicit operator-decided semantic amendment: external v1.2/v1.4 replaces external v1.2/v1.3 for AC1. D1 owns the requirement-wrapper update, primary readback and Slice 1 line 340 propagation before acceptance. Synthetic v1.3 byte normalization and the 199 adapter cases remain. AC2 changes the builder reference while preserving its semantics. R12 remains literal 100000 for both trigger and accepted write clock on both hosts. A mismatch yields a nonpassing BLOCKED cumulative result with case IDs and independent counts, no Phase E and no done-state. These contract repairs pass; their gate-filter propagation does not.

Each finding follows with source/evidence, rationale and correction. PASS addresses the plan contract or observed starting fact, not unexecuted production acceptance.

- P01 PASS (info): Input identity and drift. [docs/plans/PLAN-NATIVERESTORE-001.md:3]
  Evidence: All 41 initial SHA256 and byte-length pins match. Subject is revision 6, 72648 bytes, SHA256 AF56C59034D8164AA0AEC6045B556AE7948623C3D87F182EF85FEC4637DF33FA. Live develop HEAD ff24117d7d756404fad439a3c14c14336709eb8c.
  Judgment: The revision-4 phrases in the brief are stale; input-pins is a file-pin array and does not actually contain the checkout commit.
  Correction: Review revision 6; retain separate live checkout metadata and final rehash.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- P02 PASS (info): Native starting state. [docs/plans/PLAN-NATIVERESTORE-001.md:8]
  Evidence: Live native HEAD and gitlink are cc67418cba58e71d8e70fd353e9de47199ad2075; 163 porcelain entries: 8 tracked modified, 155 untracked; 222 insertions, 8 deletions; index clean. native-initial.json retains content hashes.
  Judgment: The Linux claims match live Git. Historical Windows data is not presented as a fresh Windows measurement.
  Correction: Measure and preserve each host independently in D0.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- P03 PASS (info): R1 B and retained output. [docs/plans/PLAN-NATIVERESTORE-001.md:83]
  Evidence: .gitmodules currently says ignore = dirty; D4 explicitly changes it to untracked and requires empty tracked/index diffs plus the exact retained path/hash/length inventory.
  Judgment: This implements the operator distinction between tracked modifications and retained build output without hiding tracked dirt.
  Correction: Keep the untracked inventory and approved ignore mode on both hosts.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- P04 PASS (info): Plan-review authority and implementation hold. [docs/plans/PLAN-NATIVERESTORE-001.md:78]
  Evidence: D-pre requires host install receipts, Windows traces, this plan at AGREE, and the verbatim operator release; handoff v2 section 11 records the 12:21 clarification permitting plan HV.
  Judgment: This review is authorized and does not release D0 or restoration.
  Correction: Retain the implementation hold under this DISAGREE result.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- P05 PASS (info): Both desktop installation receipts. [docs/plans/PLAN-NATIVERESTORE-001.md:78]
  Evidence: Linux 20261020T105400 and Windows 20261020T121000 READMEs record Nuke DeployUser/InstallMsi, release 1.3.111 and ProductVersion commit 2c8d60284229b9fedc2d50f840153b9e8d8cbcbf.
  Judgment: The required host-attributed receipts exist; this reviewer did not remeasure the installed GUIs remotely.
  Correction: Bind those exact receipts to D0 entry evidence.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- P06 PASS (info): Phase C evidence scope. [docs/plans/PLAN-NATIVERESTORE-001.md:13]
  Evidence: Linux S2b/S4 and Windows 20261020T102600 receipts support seeded reset-workload equivalence at 100k, 1M and x64sc 4M. PLAN-OBSERVER-001 S5 separately defers the real snapshot register check.
  Judgment: Bounded native reference equivalence is not managed parity or snapshot acceptance.
  Correction: Retain seed, workload, streams and both-host matrix in the D5 rerun.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- P07 PASS (info): Copy, preservation and retirement authority. [docs/plans/PLAN-NATIVERESTORE-001.md:79]
  Evidence: D0 copies and hash-verifies originals; D4 restores only named tracked paths after preservation; R2/R3 authorize retirement of named old builders; R4 retains all untracked files; A03 prohibits automatic reapply.
  Judgment: The approved planned operations preserve originals and do not authorize any review-time mutation.
  Correction: Keep host-specific committed preservation evidence before overwrite; no moves or untracked deletion.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- F01 PASS (medium): Counts and historical inventory. [docs/plans/PLAN-NATIVERESTORE-001.md:9]
  Evidence: The entire 2527-path historical list was parsed: 1432 .Po, 745 .o, 59 .a, 2 .exe, 107 .in and 106 Makefiles. Live Linux count is 8 modified and 155 untracked. counts.json retains the denominator.
  Judgment: The collapsed-directory and extension errors are corrected; historical Windows counts remain explicitly historical.
  Correction: Recompute current Windows and Linux inventories in D0.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- F02 PASS (high): Actual native build caller retirement. [docs/plans/PLAN-NATIVERESTORE-001.md:81]
  Evidence: TestHarness.csproj:10 and 68-75 invoke Vice.Native.proj BuildNative on Windows. D2 removes the property/call, old builders and project, then demands both-host solution build logs with no shim invocation. New targets/tool are absent today.
  Judgment: The plan identifies the executable call path rather than assuming a nonexistent Nuke native target was responsible.
  Correction: Verify zero remaining callers before deleting the approved obsolete project.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- F03 PASS (high): Semantic native consumer and load detector ownership. [docs/plans/PLAN-NATIVERESTORE-001.md:81]
  Evidence: Actual consumers include plain Fact tests and helper/provider paths, beyond the 43 token hits. D0 owns per-method inventory; D2 owns dual-binding resolve/create audit, loud failures and discovery controls.
  Judgment: The detector is planned work, not an existing capability or proof from an availability string. Cross-gate filter propagation is separately failed under N20.
  Correction: Implement and bind the same correct/incorrect mock cases before real gate use; retain actual gate audit.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- F04 PASS (high): All twenty historical hunks classified. [docs/plans/PLAN-NATIVERESTORE-001.md:19]
  Evidence: All 599 patch lines and 20 hunk headers in 12 files were inspected. H1-H12 cover hosted exports, bootstrap, MAINCPU reads, drive clocks/reset, SID probes, VIC-20 sound and injected inputs; H5/H6/H9 are identified behavior corrections.
  Judgment: No hunk is silently treated as byte normalization. Rejected sound behavior is explicitly owned by Phase F item 6; R7/R14 dispositions are explicit.
  Correction: Keep the D0 live hunk-to-disposition comparison and named downstream owners.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- F05 PASS (high): Literal checkpoint value preserved. [docs/plans/PLAN-NATIVERESTORE-001.md:61]
  Evidence: R12 remains literal trigger 100000 and write clock 100000 on both hosts; D1/D5 distinguish the two clocks, never rearm at the later value, and raise BLOCKER-R12-XVIC-CHECKPOINT otherwise.
  Judgment: This honors the repeated ruling without asserting immutable VICE can produce a correct interior-instruction snapshot. BLOCKED is expressly nonpassing.
  Correction: Keep the exact value and measured context; obtain a new operator decision to lift a real blocker.
  Historical revision-4 disposition of this round-3b ID: FAIL. Current status above is revision 6.

- F06 PASS (high): Comparator prerequisite allocated to D2. [docs/plans/PLAN-NATIVERESTORE-001.md:62]
  Evidence: The permitted Release comparator run produced Failed 1, Passed 0, Skipped 0 at Assert.NotNull(stateType), line 23. D2 now owns HalfCycleState and CompareHalfCycleStates, mocks first, preserving existing assertions and exact R9 set.
  Judgment: The observed baseline failure is acknowledged and allocated; it is not a passing implementation gate or added baseline exception.
  Correction: Make the reflection fixture and independent per-field negative controls green before D2 acceptance.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- F07 PASS (high): Restore pre-overwrite and resume safety. [docs/plans/PLAN-NATIVERESTORE-001.md:83]
  Evidence: D4 checks repository/host/gitlink, empty index, preserved hashes or already-clean Git comparison per path; mock controls cover staged drift, partial completion, missing/corrupt receipts and wrong identity.
  Judgment: Unexpected live state stops before overwrite, and interrupted work resumes only verified untouched paths.
  Correction: Retain both host state reports and rehash preserved copies before and after.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- F08 PASS (high): Content-aware nonmutation bracket. [docs/plans/PLAN-NATIVERESTORE-001.md:79]
  Evidence: D0 and D2/D4/D5 use SHA256 plus length for every modified and untracked path, not just porcelain path equality; a mock changed-untracked-file control is mandatory.
  Judgment: An overwrite of an existing generated or injected file is observable.
  Correction: Keep the full per-path baseline and final comparison on each host.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- F09 PASS (high): PowerShell analyzer and dry-run ownership. [docs/plans/PLAN-NATIVERESTORE-001.md:76]
  Evidence: Current builder:26 and equivalence script:20 lack SupportsShouldProcess and create directories on entry. D1 first adds tested WhatIf behavior; section 4 demands Pester, parser/analyzer and a dry run before real state-changing execution.
  Judgment: The missing capability is allocated before its D5 use, not falsely claimed present.
  Correction: Require dry runs to create neither files nor directories and retain zero-finding analyzer output.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- F10 PASS (high): Comparator mapping and pending managed acceptance. [docs/plans/PLAN-NATIVERESTORE-001.md:94]
  Evidence: Comparator summary currently mislabels TR-CYCLE-001 AC4. R13/D2 correct to FR-SNP-005 AC3 and TEST-SNP-002 AC4; D5 supplies REAL-C64-PHI2-AEC-001 native reference evidence and a class-summary test while managed AC4/task 5 stay pending.
  Judgment: State comparison and native trace evidence do not satisfy managed cycle behavior or falsely describe future lockstep implementation as current.
  Correction: Preserve this distinction in docs, requirements evidence and TODO updates.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- F11 PASS (medium): AC2 reference update and blast radius. [docs/plans/PLAN-NATIVERESTORE-001.md:103]
  Evidence: D1 routes AC2 builder-reference replacement through the requirement wrapper, preserving pinned clean out-of-tree build and external units; section 5 separately names AC1 as the R15 semantic amendment.
  Judgment: A reference-only AC2 change is distinguished from the operator-decided AC1 change.
  Correction: Read back both ACs and regenerated projections; preserve all other semantics.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- R01 PASS (high): Current reviewer credential handling. [request.jsonl:1]
  Evidence: No marker, credential store or API-key file was opened or printed. Supported file-based PowerShell wrappers internally consumed their configuration; public verdict/history records were read without raw REST.
  Judgment: The original credential disclosure is not repeated; no historical key rotation is claimed.
  Correction: Keep marker and credential files excluded from future reviewer reads.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- U01 PASS (high): Own supported primary session turn. [verdict.persistence.json:1]
  Evidence: Supported Status/bootstrap/BeginTurn and appended observations/decisions returned persisted:true, queued:false, degraded:false for own session/turn. Final complete-content readback and completed history proof are required in verdict.persistence.json.
  Judgment: This PASS is conditional on the final proof accompanying delivery. Two preexisting queued records were neither replayed nor canceled and are not current review persistence.
  Correction: Do not deliver a persisted/completed claim unless final reassembly and history proof succeed.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- U02 PASS (medium): Effective requirements, mappings and TODO readback. [docs/plans/PLAN-NATIVERESTORE-001.md:79]
  Evidence: Supported effective-requirements, FR-VICE-001 and FR-SNP-005 mapping reads and PLAN-VICEORACLE-001 TODO read succeeded. Seven selected requirements and tasks 4/5 are pending; current AC1 still has external 1.2/1.3, as expected before D1 amendment.
  Judgment: Live store state, not a prior verdict, establishes remaining obligations. No requirement/TODO writes or Windows-store remeasurement occurred in this review.
  Correction: D0/D1 must retain exact mappings and approved AC updates/readback through wrappers.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- A01 PASS (medium): Every current hunk gets a host disposition. [docs/plans/PLAN-NATIVERESTORE-001.md:79]
  Evidence: D0 requires each @@ hunk to map to a classification row, explained host-context difference, or blocking unclassified hunk; wrong-host and missing-hunk controls are required.
  Judgment: The old Windows superset is not used as evidence that current host deltas are identical.
  Correction: Keep the separate host hunk ledgers and fail on unclassified or uncovered behavior.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- A02 PASS (high): Both-host entry evidence and explicit release. [docs/plans/PLAN-NATIVERESTORE-001.md:78]
  Evidence: EntryEvidence_BindsBothHostsAndExplicitRelease names both installations, complete Windows matrix, plan AGREE and the operator release; D0 cannot run until all are present.
  Judgment: Neither receipt arrival nor plan-review activity constitutes an implementation release.
  Correction: Keep D0 held after this result.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- A03 PASS (high): Post-restore failure and rollback authority. [docs/plans/PLAN-NATIVERESTORE-001.md:83]
  Evidence: Failure after the first or both hosts stops before Phase E with preserved originals, per-host states and pinned restored source; no automatic patch reapply or deletion. D4 owns mock cases.
  Judgment: Safe interruption does not require restoring the rejected dirty oracle automatically.
  Correction: Preserve evidence and seek only a scoped new recovery decision if needed.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- N01 PASS (high): R10 prevents the live uninitialized clock-copy path. [docs/plans/PLAN-NATIVERESTORE-001.md:56]
  Evidence: Fixture scan confirms DRIVE8 2.0 payload [0,1], length 2. Pinned drive-snapshot.c:315-319,368-419,540-550 leaves three clocks uninitialized then copies them for non-NONE units. D1 computes drive<n>type 0 preconditions, enforces them last and rejects missing flags.
  Judgment: The approved bytes and rejected patch remain; the launch precondition prevents this distinct active clock path. It does not claim to eliminate the R14 dead read.
  Correction: Prove valid, missing and malformed precondition cases before native loads on both hosts.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- N02 PASS (high): Full-load result and ordered resume boundary. [docs/plans/PLAN-NATIVERESTORE-001.md:91]
  Evidence: Autostart calls machine_read_snapshot cross-object; both machine readers call maincpu_snapshot_read_module cross-object. CPU reader success precedes later modules; c64-snapshot.c:146-153 resets on later failure. D5 requires resume, then load,0 before next gate, first clock C+1 and PC access after possible stolen cycles.
  Judgment: A matching CPU module alone cannot masquerade as complete load. load,-1 and reset signatures are rejected; stream order, not a guessed clock threshold, is authoritative.
  Correction: Retain both wraps, exact forwarding and all late-failure/reset controls.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- A04 PASS (medium): Repository identity and Windows autocrlf. [docs/plans/PLAN-NATIVERESTORE-001.md:83]
  Evidence: D4 verifies host/repository/gitlink and uses git diff --exit-code for already-restored paths; Pester includes CRLF-converted Windows checkout. Preserved-copy hashes compare actual host bytes separately.
  Judgment: The resume check does not assume Git blob bytes equal CRLF worktree bytes.
  Correction: Keep exact original copies and let Git apply configured checkout conversion.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- A05 PASS (high): Identity-based execution evidence. [docs/plans/PLAN-NATIVERESTORE-001.md:81]
  Evidence: A05 retains selected IDs and terminal TRX for every project, zero skipped/aborted/not-executed, exactly the 20 named R9 failures, and controls for swapped sets, missing projects, discovery drift and partial runs.
  Judgment: The execution contract itself is discriminating. Its selection propagation into D5/CiTest is the distinct new FAIL N20.
  Correction: Keep this strict denominator while correcting selectors; do not broaden R9 or treat blocked as passed.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- A06 PASS (medium): Up-to-date builder and interrupted rerun. [docs/plans/PLAN-NATIVERESTORE-001.md:80]
  Evidence: Builder:220-231 replaces link evidence from current make stdout and fails when no line appears. D1 now requires reuse only after object hash verification and a named prerequisite failure before replacing prior evidence, with mock resume and D5 real reruns.
  Judgment: The real current rerun limitation has an explicit safe remedy allocated before use.
  Correction: Retain verified original link evidence; do not overwrite it with an empty rerun capture.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- R02 FAIL (high): Reviewer timestamp emission rule. [response.jsonl:1]
  Evidence: One commentary update reused an earlier command-measured local timestamp instead of measuring it at emission. The next Progress command measured a fresh timestamp and the reviewer immediately disclosed the violation. Command-generated progress JSON objects were measured when emitted.
  Judgment: This is a current reviewer-process failure under the explicit timestamp rule, not a defect in revision 6 and not excused by the earlier value having once been measured.
  Correction: Keep this failure visible in the receipt and full verdict. Measure anew immediately before every future timestamped output; never backfill or describe this round as unqualified timestamp-compliant.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- N03 PASS (medium): R14 dead-read disposition. [docs/plans/PLAN-NATIVERESTORE-001.md:64]
  Evidence: rotation.c:194 reads each rotation_table_ptr entry into speed_zone and :203 overwrites it before use. H5 initialized only clocks. R14 explicitly accepts this read; D5 retains two-host continuation trace evidence.
  Judgment: Operator acceptance resolves the previous open decision. This does not prove portable defined C behavior, sanitizer cleanliness or safety of separate live uninitialized clock reads.
  Correction: Retain the exact limited disposition without generalizing it.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- N04 PASS (high): Architectural register observer and forwarding. [docs/plans/PLAN-NATIVERESTORE-001.md:57]
  Evidence: Both MAINCPU readers populate global maincpu_regs. mos6510.h:116-136 reconstructs N/Z/unused bit; the supplied fixture has P=$A0. D1 tests all status combinations, wide clock/PC, exactly-once real calls, preserved result/state, and sink failures.
  Judgment: The selected public seams expose resume-boundary registers on both machines. Per-cycle local registers are not promised.
  Correction: Bind these same cases to the real wrappers and retain both-host streams.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- N05 PASS (high): DRIVE payload validation and resource/media effects. [docs/plans/PLAN-NATIVERESTORE-001.md:91]
  Evidence: Pinned writer stores has_tde/has_drives as first two payload bytes; missing modules follow TDE-off reader path. set_drive_type detaches media and iecbus routes changed unit state. D1 validates bytes/length; D5 records effective type/TDE/media per unit using public APIs.
  Judgment: The precondition side effect is observable rather than silently preserving an inaccurate media claim.
  Correction: Reject invalid/truncated payloads before launch and retain explicit no-media context where detached.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- N06 PASS (high): Provider/helper consumers and managed fakes. [docs/plans/PLAN-NATIVERESTORE-001.md:79]
  Evidence: D0 requires a semantic per-method inventory and four controls, including helper/provider-only native use and a selected pure managed fake. Current literal-dot semantic grep yields 55 candidate files; plan records 56 historically, not a completed inventory.
  Judgment: Neither a token count nor every IViceNative implementation is a reliable load classification. The final D0 ledger remains authoritative.
  Correction: Reconcile current candidates per method and retain the fake in applicable gates.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- N07 PASS (high): Absolute cycle limit and full nonempty window. [docs/plans/PLAN-NATIVERESTORE-001.md:91]
  Evidence: initcmdline.c:279-286 stores an absolute limit; CPU loops check maincpu_clk > limit after an instruction. D5 records prelude, restored clock, W cycles/2W half-cycles, gate permits, retained overshoot tail and explicit termination reason.
  Judgment: No exact-cycle stop is inferred from limitcycles; complete continuation is required, not a short clock/PC prefix.
  Correction: Preserve the window and reject truncation, early exit, overflow and prelude-only results.
  Historical revision-4 disposition of this round-3b ID: PASS. Current status above is revision 6.

- N08 PASS (high): D1 mock contracts, D2 boundary and D5 real bindings. [docs/plans/PLAN-NATIVERESTORE-001.md:80]
  Evidence: D1 creates each named Slice 1 family as VsfSlice1Contract with fake/receipt inputs; D5 adds same-assertion VsfSlice1Real cases from artifacts produced that run; D2 explicitly contains no future real artifact dependency.
  Judgment: The D2-before-D5 dependency is now explicit and no skip/stale fixture is accepted. Selector propagation remains N20.
  Correction: Retain the same case assertions and list both category denominators explicitly.

- N09 PASS (high): Tasks stay pending through the D3 review hold. [docs/plans/PLAN-NATIVERESTORE-001.md:96]
  Evidence: Exit and section 5 keep task 4 pending until step 12 accepted HV and task 5 until Phase E/final managed gate. Live TODO shows both false.
  Judgment: Phase D no longer depends on an unauthorized intermediate done-state; execution evidence can be attached while tasks remain pending.
  Correction: Keep requirement satisfaction and TODO completion separate from evidence attachment.

- N10 PASS (high): Marker-relative windows and own observational history. [docs/plans/PLAN-NATIVERESTORE-001.md:91]
  Evidence: D5 aligns each run to its own checkpoint/load marker, preserves raw indices, rejects trimming/reused history and requires matching first-offset AEC even when the first resumed access is stolen. Existing bus-adapter/obs-emitter maintain process-local history.
  Judgment: The rule demands a real observable result rather than assuming observer-local state is restored by VSF.
  Correction: Keep first-stolen-cycle and stale-history controls and fail on mismatches.

- N11 PASS (high): Detector initialization, error and inert behavior. [docs/plans/PLAN-NATIVERESTORE-001.md:81]
  Evidence: D2 covers the combined resolver plus xvic availability/create paths, alias/fallback ordering in fresh processes, pre-discovery env setup, retained logs, missing/unwritable audit failure and positive controls in separate logs.
  Judgment: A native availability query alone is not proof of actual library loading; the new detector must observe the specified resolve/load attempts without polluting the warmed step path.
  Correction: Retain xvic-first/x64-first and failed-load controls for both bindings; never silently swallow audit failure.

- N12 PASS (high): R15 explicitly amends the external fixture obligation. [docs/plans/PLAN-NATIVERESTORE-001.md:63]
  Evidence: All three repository VSFs were parsed: two identical real MAINCPU 1.2 files and one real 1.4 file, none 1.3. R15 verbatim changes real inputs to v1.2/v1.4. D1 wrapper update/readback and Slice 1 line 340 propagation are explicit; D5 real 1.2 resume and native-written 1.4 round trip retain complete window checks.
  Judgment: This is an authorized semantic AC1 amendment, not synthetic v1.3 mislabeled as external. Existing synthetic 1.3 normalization and 199 byte cases remain; AC2 only changes the builder reference.
  Correction: Read back the exact revised AC1 and AC2 before acceptance. Remove the obsolete optional v1.3-input blocker phrase at Exit:96 as propagation cleanup.

- N13 PASS (medium): Per-host budgets and evidence. [docs/plans/PLAN-NATIVERESTORE-001.md:91]
  Evidence: D5 allocates exact invocation, prelude, clock budget, unit conversion, expected permits, final window, low-clock and high-restored-clock controls and retained tail per host and fixture.
  Judgment: The acceptance denominator and interval no longer adapt to convenient output.
  Correction: Retain measured values on both hosts; use the additional terminal control in N23.

- N14 PASS (high): Explicit nonpassing BLOCKED lifecycle and cumulative denominator. [docs/plans/PLAN-NATIVERESTORE-001.md:61]
  Evidence: R12, D5:87/93 and Exit:96 explicitly report each blocked xvic case ID, measured clocks, independent counts and nonzero cumulative exit; Phase E, done-states, partial-pass and skip interpretations are forbidden. One-host and both-host mismatch mock/real controls are allocated.
  Judgment: The old complete-with-blocker contradiction is repaired at the acceptance-contract level. Ordinary/cumulative selector propagation still fails under new N20 and does not justify weakening this contract.
  Correction: Keep the blocked cumulative denominator and ordinary execution denominator distinct, and fix N20.

- N15 PASS (high): Armed checkpoint writer preserves gate interactions. [docs/plans/PLAN-NATIVERESTORE-001.md:80]
  Evidence: D1 now explicitly tests armed clock-gate composition: one record/permit per advanced cycle, held permit prevents further advancement, quit/EOF/error unchanged, measured trigger/write clocks under interrupt and stolen runs; D5 repeats real both-host cases.
  Judgment: Unset-variable equivalence alone is no longer offered as evidence for the armed path.
  Correction: Keep all armed controls and no-snapshot rerun with zero VICE_SNAP lines.

- N16 PASS (high): Audit starts before discovery. [docs/plans/PLAN-NATIVERESTORE-001.md:81]
  Evidence: D2 sets audit env in the launched dotnet test process before discovery, never truncates after discovery, and names fresh attribute-only/provider-only attempt controls plus selected managed fake and separate positives.
  Judgment: A trait exclusion cannot itself prove discovery/provider side effects are absent; the added interval and controls address that gap.
  Correction: Observe every specified attempt or fail the gate visibly; never initialize the audit inside a test body.

- N17 PASS (high): Seam-level zero-allocation and determinism test. [docs/plans/PLAN-NATIVERESTORE-001.md:81]
  Evidence: D2 explicitly allocates warmed repeated step calls for both native bindings with audit unset, zero incremental allocations and log calls, unchanged deterministic output, and deliberately allocating/logging doubles rejected.
  Judgment: This tests the native step seam directly rather than citing adjacent managed tests as allocation evidence.
  Correction: Keep independent expected output and identical real-bound cases; exclude cold initialization from the warmed measurement explicitly.

- N18 PASS (high): Clock versus half-cycle budget and prelude controls. [docs/plans/PLAN-NATIVERESTORE-001.md:91]
  Evidence: D5 states W CPU cycles = 2W records, whole-cycle windows, odd requests and overflow rejected, low-clock early checkpoint with raised prelude-safe limit, high-clock legacy fixture, permit count and retained overshoot tail.
  Judgment: The explicit unit and infeasible-budget outcomes requested in round 5 are now present. Low-clock termination needs the additive N23 control to remove remaining execution ambiguity.
  Correction: Preserve the exact window; document one canonical low-clock limit and termination result.

- N19 FAIL (medium): Export-exclusivity correction was not propagated. [docs/plans/PLAN-NATIVERESTORE-001.md:54]
  Evidence: Section 1 line 14 now correctly includes DMA and BRK exports, but R11 line 54 still says interpreter locals are exported only at traps, JAM and monitor entry. Pinned active 6510dtvcore.c calls EXPORT_REGISTERS at DMA:451 and BRK:1012 in addition to traps/JAM/monitor.
  Judgment: The correct new sentence and stale exclusive sentence contradict each other. The public trap seam remains valid; this FAIL concerns an incorrect source claim, not permission to expose arbitrary-cycle registers.
  Correction: Replace every exclusive list with the narrower public-seam claim and search all copies. Also qualify first-boundary wording for the already-latched interrupt case in N22.

- N20 FAIL (high): Gate filters contradict real-test exclusivity and every-gate retirement. [docs/plans/PLAN-NATIVERESTORE-001.md:81]
  Evidence: Build.cs:97-112 Test runs solution with category exclusions; CiTest:133-142 has a separate identical filter. D2 adds only Category!=ShimLegacy to Test and adds the three projects to the solution. D5:93 requires VsfSlice1Real only in the cumulative target; :94 reruns Test under A05; section 5:102 says ShimLegacy leaves every gate. filter-projection.json shows VsfSlice1Real still selected by ordinary Test/CiTest and ShimLegacy still selected by unchanged CiTest.
  Judgment: After D5 adds real tests, the stated exact edits cannot enforce the promised denominators. With R12 blocked, ordinary Test executes blocked native cases or violates A05 by omitting selected cases; CiTest still reaches retired consumers. This is a gate-chain defect, not a request to change the exact 20 known-red set.
  Correction: D2 must explicitly apply consistent selectors to Test and CiTest: ordinary selections exclude ShimLegacy and VsfSlice1Real while retaining contract cases; cumulative selection includes every required real case with stable IDs, explicit BLOCKED outcomes and nonzero exit. Add selection/projection controls at D2 and after D5 registration; retain per-gate denominators and reject leakage or omissions.

- N21 ADD-AC (medium): D1/D2/D5 RejectedCheckpoint_PreservesCandidatesAndPublishedFixture. [docs/plans/PLAN-NATIVERESTORE-001.md:88]
  Evidence: The callback necessarily invokes machine_write_snapshot before the generator can inspect its write clock, while R12 says generator writes no fixture on a mismatch. Cross-host acceptance also occurs after candidate bytes exist.
  Judgment: The safe fail-closed acceptance is stated, but candidate retention and an already-published fixture/manifest during failure or restart need an observable contract. This is an added AC, not an alleged native write during review.
  Correction: Own in generator contract D1/D2 and real D5: on one-host mismatch, malformed/missing checkpoint record, write failure or interrupted retry, accepted fixture and manifest remain unchanged; retain candidate bytes, hashes and logs under a receipt, never delete/move or silently publish them; emit named BLOCKED/nonzero with affected IDs, and do not accept a stale prior artifact as newly generated.

- N22 ADD-AC (medium): D1/D5 TrapRequest_AfterInterruptMaskWasLatched. [docs/plans/PLAN-NATIVERESTORE-001.md:80]
  Evidence: 6510dtvcore.c:1758 copies global_pending_int, :1763 calls DO_INTERRUPT, and macro :354 copies its argument into ik. A clock-hook request during an already-dispatched IRQ/NMI may not be in ik at :408; :1804 then fetches a handler opcode before the next sample.
  Judgment: A pending interrupt at trigger and a trigger raised after the pending mask was latched are different histories. Acceptance remains safe because actual write clock must equal 100000, but a mock must not force the callback to the earlier boundary.
  Correction: Add a correct/rejected mock control for trigger during the dispatched interrupt after mask capture and bind it to both-host D5 context. Preserve the measured callback clock and reject later clocks under literal R12; narrow first-boundary wording to the actual pending-mask dispatch semantics.

- N23 ADD-AC (medium): D1/D5 LowClockResume_TerminatesAfterCompleteWindow. [docs/plans/PLAN-NATIVERESTORE-001.md:91]
  Evidence: D1 says limit=C+W and prelude cannot reach it; D5 low-clock positive case instead raises the limit to prelude length+W. Restoring C rewinds the absolute clock; CPU limit is tested only after instructions and can overshoot.
  Judgment: Unit conversion is explicit, but the exact finite terminal result and gate-permit accounting when the raised prelude-safe limit exceeds C+W need one canonical outcome. A missing terminal choice can leave a producer waiting for another permit after the compared window.
  Correction: Own D1 launch-budget contract and D5: low-clock positive observes load,0 and exactly W compared cycles/2W half-cycles, then reaches an explicit bounded terminal gate/process outcome; record permits, any retained tail and exit reason. Reject a producer awaiting an unsupplied permit, early stop or silently stretched window. Reconcile the D1 C+W wording with the D5 exception without prescribing a new protocol.

Requirements and obligation mapping:

- TR-VSFLOCKSTEP-RESUME-001: AC1 R15 semantic input amendment explicitly allocated to D1 wrapper update/readback and Slice 1 propagation; real 1.2/1.4 resume/roundtrip and existing synthetic 1.3 bytes retained. AC2 only replaces builder reference while preserving pinned out-of-tree/external-only semantics. No satisfaction claimed.

- FR-VICE-001 / TR-VICE-ORACLE-001 / TEST-VICE-001: Live effective/mapping readbacks pending. D0 preservation, clean pinned source and unchanged inventories allocate TEST-VICE-001 AC1/AC2; dirty native output remains diagnostic. Other original ACs retained.

- TR-CYCLE-001 AC4: Managed CPU PHI2/AEC behavior remains pending Phase E. REAL-C64-PHI2-AEC-001 is native reference evidence only, not managed satisfaction.

- FR-SNP-005 AC3 / TEST-SNP-002 AC4: Comparator fixture mapping corrected in D2; HalfCycleState and state comparator owned there. This does not complete the full snapshot import/export requirements.

- PLAN-VICEORACLE-001 tasks 4 and 5: Both currently false. Evidence attached while pending; task 4 done-state only after step 12 accepted HV, task 5 after managed/final gates. No TODO was changed by this review.

- PLAN-VSFLOCKSTEP-001 Slice 1: Builder/preflight, both-machine libraries reinterpreted as observer binaries, all schema-1 fields/edges, fixture provenance, prerequisites, deterministic PAL/NTSC literal-100000 generation, R10 launch, real resume, round trip, negatives and cumulative command each have D1/D2/D5 owners. N20 prevents the stated gate isolation; N21-N23 add failure/terminal controls.

Structured what-if pass:

- Different checkpoint clocks on hosts: One-host and both-host mismatch are nonpassing BLOCKED, IDs and both measured clocks retained; no rearm or alternate literal. N14 PASS; candidate publication addition N21.

- Pending interrupt at checkpoint: Writer uses measured callback clock after register export; later value fails literal R12. Add the already-latched dispatch history N22; never force the mock clock.

- Checkpoint inside stolen-cycle run: Armed gate controls preserve exactly-once gate forwarding and permits; delayed write clock remains measured and rejected if not 100000. N15 PASS.

- First resumed access is stolen: Retain idle/stolen prefix, first CPU access at PC, own BA history and full marker-relative equality; no warm-up trimming. N02/N10 PASS.

- Shim reached only through data provider or attribute: Pre-discovery audit retained; fresh native-attempt controls observable or gate fails; managed fake remains selected. N06/N16 PASS. CiTest leakage is N20 FAIL.

- Audit file unwritable or missing: Attempt/gate fails loudly, never empty-log success; positive controls have separate logs. N11 PASS.

- Exact 20 known-red set changes: A05 rejects added, removed or equal-sized substituted IDs, incomplete TRX or skipped cases. Report improvement or regression for operator decision; never silently mutate R9.

- Windows autocrlf checkout: Git compares restored checkout semantically; preserved hashes compare host bytes; CRLF mock required. Wrong host or unexpected content stops before overwrite. A04 PASS.

- Crash/restart between hosts or after candidate write: D4 classifies restored/untouched/unexpected and stops after later failure without reapply; generator publication/retry retention addition N21 closes remaining candidate outcome.

- Low-clock fixture and raised prelude-safe budget: Whole-cycle interval and load required, odd/overflow/early exit rejected. Add explicit terminal permit/process outcome N23; do not rely on limitcycles as exact stop.

- Cross-workspace/session or stale evidence: Restorer checks repository, host, gitlink and verified preservation; D5 real cases consume only artifacts produced that run. Own review turn uses explicit parent IDs and exact history lookup.

- R12 blocked while ordinary and cumulative suites execute: Explicit BLOCKED lifecycle is correct, but unchanged selectors leak real tests into ordinary gates and retired consumers into CiTest. N20 FAIL; fix selectors, not denominator or R12.

Native seam assessment: Public trap runs after EXPORT_REGISTERS with IMPORT afterward; machine_write_snapshot delegates synchronously to native snapshot writer, which writes MAINCPU clock from maincpu_clk. Both machine read wrappers are cross-object. Safe boundary feasibility is supported by source, not executed here. Actual callback clocks remain acceptance evidence; already-latched interrupt nuance is N22. New VICE_SNAP lines are not part of old no-snapshot equivalence; D1/D5 require inert reruns with none.

Retained baseline test: dotnet test "/home/sharpninja/github/vice-sharp/tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj" -c Release --filter "FullyQualifiedName~Vic20HalfCycleComparatorTests" --results-directory "/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T172500-plan-nativerestore-001-r6-gpt61sol/TestResults" --logger "trx;LogFileName=vic20-comparator.trx" --diag "/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T172500-plan-nativerestore-001-r6-gpt61sol/TestResults/vstest.diag.log" --blame-hang --blame-hang-timeout 5m. Exit 1; Passed 0, Failed 1, Skipped 0. Failure is missing HalfCycleState at the reflection assertion, allocated to D2. See comparator-test.log and TestResults/vic20-comparator.trx. No native/full solution gate ran.

Integrity: native gitlink/HEAD cc67418cba58e71d8e70fd353e9de47199ad2075, 8 modified tracked plus 155 untracked, clean native index. Complete historical patch/inventory and requested documents were read; 20 materialized source files were checked against pinned Git blobs. Final pin, native-content and tracked-worktree comparisons are retained in review-integrity-final.json. The native content baseline was measured after the allowed test, not before it.

Reviewer: Codex / gpt-6.1-sol / max, through the requested Codex CLI; no model substitution or nested reviewer. Add-profile read all non-skill Markdown files (1), and was repeated after compaction. Supported plugin root /home/sharpninja/github/mcpserver-codex-plugin; MCP_FAILSAFE_DRAIN_DISABLED=1. Primary writes succeeded; two historical queued records were untouched. No untrusted result occurred, so the conditional triage/failsafe branch was not invoked. Exact model/effort comes from request.jsonl; the server model header is generic codex.

Own session: Codex-20261020T222731Z-plugin-session; request: req-20261020T222756Z-nativerestore-r6. Parent: ClaudeCode-20261020T015046Z-plugin-session / req-20261020T172116Z-prompt-6c6d.

Request: /home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T172500-plan-nativerestore-001-r6-gpt61sol/request.jsonl. Response stream: /home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T172500-plan-nativerestore-001-r6-gpt61sol/response.jsonl. The response stream is still live until CLI exit; no final stream hash is claimed here. Complete prose and complete JSON are stored in ordered readable primary turn chunks, read back/reassembled and hash-compared; exact completed history proof, raw/LF-normalization results and final artifact hashes are in verdict.persistence.json. Delivery of this paragraph as a persistence claim requires that proof to succeed first.

Execution remains held. Correct the two plan findings N19/N20 and incorporate or explicitly dispose the three additive ACs; the reviewer-process R02 failure remains recorded in this round and requires no product edit. A new qualifying review is required before implementation release.
