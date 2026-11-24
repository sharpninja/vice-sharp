2026-10-20T19:30:33 - **AGREE. Accuracy 98%, completeness 98%.**

**54 PASS, 0 FAIL, 0 UNKNOWN, 2 ADD-AC.** All 54 round 7 findings were independently re-evaluated. N19’s original global register-export timing error is fixed; N24-N26 are incorporated. New N27 and N28 are nonblocking refinements with no identified safety gap.

Saved receipts:

- [Complete prose verdict](/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T185200-plan-nativerestore-001-r8-gpt61sol/verdict.md)
- [Complete JSON verdict](/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T185200-plan-nativerestore-001-r8-gpt61sol/verdict.json)
- [Completed-turn persistence proof](/home/sharpninja/github/vice-sharp/docs/receipts/hv/20261020T185200-plan-nativerestore-001-r8-gpt61sol/verdict.persistence.json)

Reviewer: Codex, `gpt-6.1-sol`, effort `max`. Session `Codex-20261020T235538Z-plugin-session`; request `req-20261020T235529Z-nativerestore-r8`. Parent session `ClaudeCode-20261020T015046Z-plugin-session`; parent request `req-20261020T172116Z-prompt-6c6d`.

Both complete verdict artifacts were primary-persisted, retrieved, reassembled and verified byte for byte. The completed history query reports `status=completed`, 39 dialog entries and 9 actions. No queued write was counted as persisted. No CRLF/LF normalization was necessary.

This approves the reviewed **plan quality**. D0 still requires the explicit operator implementation release and all D-pre evidence. No Phase D step, native build or native checkpoint executed during this review.

The live subject is revision 8, SHA-256 `FDA123F4C4605B8199B4FC6733292A727EE5E33EC6EBBE441B2EB129AD8E7DA7`. The checkout is `develop` at `21a111672db8dd409624ef90c6af6a75d86f4b29`. The brief’s revision 4 labels are stale; `input-pins.json` contains file pins, not a checkout commit.

All 41 input pins match. All 25 inspected materialized source files match their pinned Git objects. All 163 native status entries retain identical status, path, length and SHA-256 before and after review: 8 modified tracked files and 155 untracked files. The native index remains empty; gitlink and native HEAD remain `cc67418cba58e71d8e70fd353e9de47199ad2075`. `git diff --check` exits 0.

The permitted Release comparator run completed with **0 passed, 1 failed, 0 skipped, exit 1**, failing at fixture line 23 because `HalfCycleState` is absent. That is an acknowledged current gap assigned to D2, not a passing implementation gate. Exact arguments, stdout/stderr, TRX and VSTest diagnostics are retained in the receipt’s `TestResults/`.

All plan references below identify [PLAN-NATIVERESTORE-001.md](/home/sharpninja/github/vice-sharp/docs/plans/PLAN-NATIVERESTORE-001.md). Native C anchors refer to the pinned materialization under `artifacts/immutable-vice/cc67418cba58e71d8e70fd353e9de47199ad2075/source/vice/src`, unless explicitly identified as the preserved patch or current wrapper.

For all 35 round 3b IDs, the historical revision 4 disposition was read from the original round 4 JSON: **PASS for every ID except F05, which was FAIL**. Those historical dispositions are recorded individually in `verdict.json`; they are not substituted for the fresh revision 8 assessments below.

- **P01 PASS — Subject identity and stale labels.** Plan section 1 and the 41 rehashed pins identify revision 8 unambiguously. Read-only Git supplies the checkout identity omitted from the pin array. The stale revision 4 wording does not change the subject; the historical ledger remains separately attributed.

- **P02 PASS — Live native baseline.** The before/after inventories agree on all 163 entries and their content. The measured diff remains 8 files, 222 insertions and 8 deletions. Historical Windows state is receipt evidence, not a new remote measurement. D0 must independently repeat preservation on each host.

- **P03 PASS — R1 B and retained output.** Plan R1, D4 and Exit explicitly distinguish empty tracked/index diffs from the retained untracked inventory. Current `.gitmodules` still says `dirty`; D4 owns its change to `untracked`. This honors the operator’s amendment without deleting build products.

- **P04 PASS — Review authority and implementation hold.** D-pre requires desktop receipts, Windows trace matrix, plan AGREE and the verbatim implementation release. The supplied D3 clarification permits plan HV while deferring implementation HV to step 12. No authority deadlock remains.

- **P05 PASS — Both desktop installations.** The Linux receipt records Nuke `DeployUser` 1.3.111 from `2c8d6028`; the Windows receipt records `PublishMsi`/`InstallMsi`. A fresh diff from that commit to HEAD over `src`, `build` and the observer builder is empty. D0 still binds the installed version and commit to each host.

- **P06 PASS — Phase C evidence scope.** The retained Linux and Windows receipts cover 100k and 1M for both machines and 4M x64sc. They establish the existing observer’s reference behavior. D5 correctly requires another complete matrix with the new snapshot units, snapshot variables unset and zero `VICE_SNAP` records.

- **P07 PASS — Preservation and retirement authority.** D0 copies, hashes and retains modified tracked and injected source files. D4 requires committed preservation and restores only enumerated tracked paths. R2/R3 authorize retirement of the named builders/project; R4 retains injected and untracked files. No move, `clean` or `reset --hard` is authorized.

- **F01 PASS — Counts and historical inventory.** The preserved patch contains 599 lines, 20 hunks and 12 files. Its 2,527-path inventory includes 745 `.o`, 59 `.a`, 2 `.exe`, 1,432 `.Po`, 106 `Makefile` and 107 `.in` entries. Those historical counts remain distinct from Linux’s live 163-entry baseline.

- **F02 PASS — Actual native build caller.** `ViceSharp.TestHarness.csproj:10` defines `ViceNativeProject`; lines 68-72 invoke its Windows `BuildNative` target. D2 names that property and call, audits remaining references and retires the project/scripts. The three native-safe projects, two new Nuke targets and generator are currently absent as the plan states.

- **F03 PASS — Semantic consumers and detector ownership.** Fresh searches reproduce 43 token candidates, 5 categorized files and 56 broader semantic candidates. Plain `[Fact]` consumers exist outside the narrow token set. D0/R6/D2 require per-method transitive classification, both native bindings, discovery-time auditing and a zero-attempt ordinary gate.

- **F04 PASS — All twenty historical hunks.** The complete patch maps to H1 at patch lines 5/16; H2 104/115; H3 140; H4 178; H5 53; H6 74/89; H7 219/424/435; H8 270/291/306; H9 466/487; H10 508/590; H12 230. H11 is injected material outside those hunks. H5/H6/H9 remain explicitly rejected behavior corrections with stated dispositions.

- **F05 PASS — Literal 100,000 and blocker lifecycle.** R12 and D5 accept xvic fixtures only when trigger, callback write clock and written `MAINCPU` clock are all 100,000 on both hosts. Any miss leaves accepted artifacts unchanged, records both clocks/context, reports dependent case IDs BLOCKED, exits nonzero and holds Phase E. This honors the literal ruling without claiming feasibility in advance.

- **F06 PASS — Comparator prerequisite in D2.** The focused test reproduced the missing-state failure. Reflection requires parameterless creation, the named typed properties, enum `Read`, static comparison and the exact mismatch shape. R13 assigns their mock-first implementation to D2 and preserves the fixture assertions. The comparator cannot enter the known-red exception.

- **F07 PASS — Restoration preflight and resume.** D4 checks repository, host, gitlink, native HEAD, clean index and live-versus-preserved bytes before overwrite. Already-restored paths must be Git-clean. The negative controls cover unexpected bytes, staging, bad receipts, wrong identity and partial completion. Resume touches only verified untouched paths.

- **F08 PASS — Content-aware integrity.** D0 captures hashes and lengths, and D2/D5 compare the same inventory. A mock changes an existing untracked file to prove that equal paths and counts are insufficient. This review independently reproduced that content-aware bracket.

- **F09 PASS — Analyzer and effective `-WhatIf`.** Both existing canonical scripts lack `SupportsShouldProcess`; D1 explicitly repairs them and proves that `-WhatIf` creates neither files nor directories. Save/Restore have equivalent dry-run contracts. The plan does not claim unsupported dry runs already work.

- **F10 PASS — Correct mappings and validator documentation.** R13 removes the fixture’s incorrect `TR-CYCLE-001` AC4 claim and retains `FR-SNP-005` AC3 / `TEST-SNP-002` AC4. D5 accurately describes the current register/CIA validator and adds a class-summary check. Native PHI2/AEC evidence does not close managed AC4 or task 5.

- **F11 PASS — AC2 reference amendment.** D1 changes the old builder reference through the requirements wrapper while preserving clean out-of-tree materialization and external-unit-only semantics. AC2 is not a semantic relaxation. D1 owns the readback and propagation.

- **R01 PASS — Reviewer credential handling.** This reviewer did not read, copy or print the marker or a credential-bearing file. The supported plugin alone reads its marker. Wrapper output is guarded, and no raw session/TODO/requirements REST write occurred. An early source-directory search was audited as excluding the ignored marker; no exposure was observed.

- **U01 PASS — Own persisted review turn.** Supported bootstrap, Status, begin and subsequent writes succeeded with drain disabled. Complete prose and JSON were stored in ordered chunks, retrieved and hash-verified before completion, then verified again from completed history. `verdict.persistence.json` proves primary persistence and completed state.

- **U02 PASS — Live requirements and mappings.** Supported queries read the named FR/TR/TEST records, both mapping sets and `PLAN-VICEORACLE-001`. ACs remain unsatisfied and tasks 4/5 remain false. D1 owns amendments through the wrapper; this review changed no requirement or TODO done-state.

- **A01 PASS — Every current hunk gets a disposition.** D0 splits current patches by `@@`, maps each hunk to section 2 or an explained host difference, and blocks D4 on unclassified or required-uncovered changes. Missing-hunk and wrong-host controls prevent historical preservation from authorizing new dirt.

- **A02 PASS — Both-host evidence and release.** D-pre/D0 require each installation identity, complete Windows matrix, this review receipt and the explicit release. D4 repeats the preservation/HV/release conditions. Plan AGREE alone cannot release implementation.

- **A03 PASS — Failure after restoration.** The named contract covers failure after one host and after both. It retains originals and evidence, reports path states, leaves restored source pinned and stops before Phase E. It never automatically reapplies the dirty patch.

- **N01 PASS — R10 avoids active uninitialized clock copies.** The supplied fixture’s `DRIVE8` payload is two bytes, `0,1`. The pinned reader initializes clock arrays only with TDE but copies them for non-NONE units. R10 computes and places the required NONE/TDE-off launch flags last, without changing the reader.

- **N02 PASS — Whole-load success and exact ordering.** Both snapshot readers load `MAINCPU` before later modules that can fail and reset. The two wraps separately observe architecture and whole-load return. D5 requires `resume`, then `load,0`, before the next gate at C+1; stolen/idle prefix is allowed before the first CPU read/dummy at PC.

- **A04 PASS — Repository identity and autocrlf.** Dirty preserved paths use content hashes; already-restored paths use Git-aware clean comparison, allowing host conversion. D4 explicitly includes a CRLF mock and wrong-repository/host/gitlink controls. Unexpected state stops without overwrite.

- **A05 PASS — Exact known-red identities.** D2 retains selected IDs and terminal TRX for every project, with zero skipped, aborted or unexecuted selected cases. Only the exact 20 diagnostic FQNs are permitted failures. Swapped sets, missing projects, discovery drift and partial runs fail the gate.

- **A06 PASS — Up-to-date builder reruns.** The current Make stage depends on current stdout link lines. D1 instead permits retained link evidence only after object hashes match, otherwise stops before replacement. Mock up-to-date/interrupted cases precede D5 real reruns on both hosts.

- **R02 PASS — Measured progress and timestamps.** Progress objects were emitted during the review using command-measured local timestamps, numeric completion and ETA. Audit timestamps are measured rather than estimated. Historical source timestamps and protocol UTC IDs are identified as such.

- **N03 PASS — R14’s scoped dead-read disposition.** `rotation.c:194` reads the table value and line 203 overwrites it before use; H5 did not remove that same read. R14 expressly accepts this disposition and D5 requires two-host continuation evidence. It is not a general C-definedness or sanitizer-clean claim.

- **N04 PASS — Architectural register observer.** `MOS6510_REGS_GET_STATUS` reconstructs N/Z and the unused bit. Both MAINCPU readers update `maincpu_regs`, and both required calls cross objects on both machines. D1 covers status combinations, wide values, exactly-once forwarding, preserved results/state and sink failure exit 72.

- **N05 PASS — DRIVE validation and media effects.** D0 scans all DRIVE module presence, first bytes and lengths. D1 rejects malformed/truncated inputs. The pinned resource path changes unit/bus state and detaches media; D5 reports effective type, TDE and image name or explicit none. R10’s consequences remain observable.

- **N06 PASS — Provider/helper and fake controls.** Classification includes transitive fixtures, helpers and providers. Controls distinguish direct consumers, provider-only consumers, pure managed fakes and string-only mentions. A grep count cannot replace that inventory.

- **N07 PASS — Absolute limit and nonempty window.** `-limitcycles` is absolute and checked after instructions. D1/D5 use C+W with the explicit low-clock prelude exception, consume prelude permits and require the entire prescribed window. Prelude-only exit, overflow, truncation and unreported overshoot cannot pass.

- **N08 PASS — Mock-first allocation.** D1 creates the Slice 1 families as contract tests against fakes/recorded receipts. D5 binds the same assertions to artifacts produced by that real run. Contract success cannot be reported as native acceptance, and D2 does not depend on future fixtures.

- **N09 PASS — Pending task acceptance.** Tasks 4/5 remain pending with evidence attached. Task 4’s done-state belongs after step 12’s required AGREE; task 5 belongs after Phase E and the final immutable gate. The live TODO agrees.

- **N10 PASS — Marker-relative windows and own history.** D5 aligns each stream to its own checkpoint/load marker, retains raw indices and compares offsets without shifting or trimming. BA/AEC uses that run’s history. Borrowed history and discarded warm-up cannot manufacture equality.

- **N11 PASS — Log existence, failure and inertness.** The ordinary gate requires an existing empty log and rejects missing/unwritable logs. Both bindings, aliases, fallbacks and failures have separate controls. Unset auditing must preserve stepping, allocation behavior and deterministic output.

- **N12 PASS — Authorized R15 amendment.** No real 1.3 input exists and the pinned writer emits 1.4. R15 explicitly changes real external inputs to 1.2/1.4. D1 applies/readbacks that semantic amendment; synthetic 1.3 remains, and D5 uses real 1.2 plus a fresh native 1.4 round trip.

- **N13 PASS — Per-host prelude budgets.** D5 records each fixture/host’s prelude evidence, clock, interval, absolute limit, permits, complete window and terminal outcome. The pinned three-second delay and deferred load support the need for this evidence; one host’s prelude is not reused as another’s.

- **N14 PASS — Observable nonpassing BLOCKED state.** Stable dependent IDs, blocker name, both clocks/context, independent denominators and nonzero cumulative result are required. BLOCKED cannot become skip, partial pass or completion. Phase E and dependent done-states remain held until resolution and execution.

- **N15 PASS — Armed writer preserves the gate.** D1 explicitly tests one gate/permit per advanced cycle, no advance while withheld, unchanged quit/EOF/error behavior, one arm and separate trigger/write clocks. D5 repeats armed interrupt/stolen cases on both hosts, separately from no-snapshot equivalence.

- **N16 PASS — Audit begins before discovery.** Availability attributes probe during construction. D2 sets the audit environment on the launched test process before discovery and forbids later truncation. Fresh-process attribute/provider controls cover those earlier attempts.

- **N17 PASS — Zero-allocation control.** The plan requires warmed seam-level measurements for both bindings with audit unset, zero incremental allocations/log calls, unchanged output and a deliberately allocating/logging double that must fail. This directly tests the hot-path invariant.

- **N18 PASS — Cycle/half-cycle units.** W is whole CPU cycles and the comparison contains exactly 2W observations. Odd half-cycle requests and overflow are rejected. Existing emission order supports that definition; no interval may shrink to fit the output.

- **N19 PASS — Original export-timing defect fixed.** R12 now names the chosen trap boundary whose copied pending mask includes the trap and uses the actual write clock. BRK’s post-fetch export remains acknowledged. The false global “correct registers only at an instruction boundary” claim is removed. Residual broader prose is separately recorded as N27.

- **N20 PASS — Shared selectors and cumulative projection.** D2 explicitly addresses both current Test/CiTest filter copies with one ordinary selector, retaining contract cases and excluding ShimLegacy/real cases. The cumulative target retains real IDs and nonzero BLOCKED results. Projections run before and after D5 registration.

- **N21 PASS — Candidate publication isolation.** Mismatch, malformed/missing record, write failure and interrupted retry leave accepted fixtures/manifests unchanged. Existing bytes remain in unique receipt paths and stale artifacts cannot count as new output. Real binding belongs to D5.

- **N22 PASS — Trigger after pending-mask capture.** The active core captures `global_pending_int` before dispatch. D1 explicitly covers a trigger during an already-dispatched interrupt, allowing the first handler opcode before the later trap boundary. That measured later clock fails literal R12.

- **N23 PASS — Low-clock bounded termination.** The canonical budget exception is measured prelude+W. The run must observe `load,0`, compare exactly W cycles/2W half-cycles and terminate by limit or gate quit within its recorded allowance. An unsupplied permit, early stop or stretched interval fails.

- **N24 PASS — Successful ordinary discovery control incorporated.** Audit scope now explicitly includes `FindLibraryPath`, `IsAvailable` and `AvailabilityMessage`. D2/D5 require ordinary excluded discovery to resolve/load none of the three library names, preserve the managed fake and complete its denominator. Deliberate attempts use separate logs.

- **N25 PASS — Absent candidates recorded honestly.** Unique candidate paths and explicit absent/not-created/native-writer-removed outcomes are now stated. Result, `checkpoint-fail` and process logs remain evidence; nonexistent bytes receive no invented hash. Both pinned writers demonstrate removal on failure.

- **N26 PASS — R15 propagation incorporated.** D1 readback requires real 1.2/1.4, unchanged synthetic 1.2/1.3/1.4, unchanged AC2 semantics and reconciled blocker lists. Exit removes the obsolete optional real-1.3 blocker branch.

- **N27 ADD-AC, low — D1 `ReviewSummary_MatchesChosenCheckpointScope`.** The whole-plan search still finds broader wording at section 1, R11 and section 7. Section 1 says “the only checkpoint an external unit can obtain”; R11 enumerates other exports; section 7 claims the revision makes no claim about other export sites. That summary is literally overstated. The operational trap/write-clock acceptance remains sound, so this is a nonblocking editorial propagation AC. D1 should make section 1/R11/R12/section 7 consistently describe the chosen seam and have the change summary match the retained prose.

- **N28 ADD-AC, low — D1/D5 `SnapshotWriter_AnyNonzeroResultRejectsPublication`.** Pinned `vic20-snapshot.c:99-102` returns positive `1` after closing/removing a candidate when IEEE488 VIA writing fails; other failures return `-1`. Add an explicit positive-1 control: every nonzero writer result produces failure, prevents successful checkpoint/publication/manifest updates and records retained-or-absent evidence honestly. Existing failure and publication-isolation gates already fail closed, so this refinement does not veto AGREE.

The trap writer is supported by native usage, not just a declaration. `interrupt.c:320-370` queues and dispatches callbacks; active `6510dtvcore.c:410-411` exports registers before trap dispatch. Upstream `event.c:692-698` calls `machine_write_snapshot` from a registered trap callback, with another writer trap at lines 777-798. This supports the callback pattern while providing no guarantee of literal cycle 100,000.

Both observer wraps are cross-object on both machines: autostart calls machine snapshot reading in the machine unit, and machine snapshot readers call MAINCPU reading in the CPU unit. The inner architecture and outer return can therefore be observed before the next gate. Snapshot runs use framed `VICE_SNAP` evidence; they do not claim whole-stderr equivalence. The no-snapshot matrix must emit zero such records.

The comparator reflection requires `HalfCycle` as `long`; register/data fields as `byte`; PC/address as `ushort`; `MicroOpPhase` as `int`; event/dummy flags as `bool`; IRQ/NMI/BA stages as `uint`; and an enum containing `Read`. Its mismatch requires the exact field names and types, `$1000`/`$1001` strings and a message containing “half-cycle 37”. R13 preserves those executable assertions.

The supplied fixture was inspected from bytes: `MAINCPU` 1.2, payload 91, clock 109,163,946, A 194, X 2, Y 0, SP 250, PC 22,377, P 160; 24 modules; `DRIVE8` payload `0,1`. The separate real 1.4 fixture has clock 30,250,590. No real 1.3 provenance is invented.

Requirement ownership remains explicit:

- `FR-VICE-001`, `TR-VICE-ORACLE-001`, `TEST-VICE-001` AC1/AC2: D0 mappings, D2 retirement, D4 clean checks and D5 preflight. No satisfaction change occurred here.
- `TR-VSFLOCKSTEP-RESUME-001` AC1: explicit R15 semantic amendment in D1; real resume and full round-trip evidence in D5.
- AC2: reference-only builder amendment, preserving immutable-source semantics.
- `TR-CYCLE-001` AC4: native reference evidence in D5; managed acceptance remains Phase E.
- `FR-SNP-005` AC3 / `TEST-SNP-002` AC4: exact comparator contract in D2; state comparison alone does not close full managed snapshot parity.
- `PLAN-VICEORACLE-001` tasks 4/5: evidence attached during D; done-states remain assigned to step 12 and Phase E respectively.

The Slice 1 obligation map covers every substantive item in the requested ranges:

- Lines 230-234: schema 1 fields, fixture/module fields, mandatory deterministic state, native/managed/adapter/test edges and omission/duplicate controls.
- Lines 238-239, 325, 328, 334-335: explicit observer-delivery amendment, Windows MSYS2 prerequisites, external units, symbols, build receipts and immutable inventories.
- Lines 240-243, 326, 329: supplied C64 fixture, both xvic fixtures, literal checkpoint or named BLOCKED outcome, fresh-run/cross-host determinism, ROM/media hashes and future media-fixture ownership.
- Lines 330-333, 340, 342: unchanged synthetic versions and malformed/duplicate/overflow cases; external normalization limited to the required versioned fields.
- Lines 336 and 338: missing inputs fail rather than skip; real cases retain their cumulative denominator and explicit blocker outcomes.
- Lines 321-324 and 340: mock-green and independent negative controls precede production; the same assertions bind to real evidence.
- Lines 344-350: obsolete commands are explicitly replaced and both new Nuke targets are wired and executed with retained host evidence.

The structured what-if pass produced observable outcomes:

- Different host checkpoint clocks: reject publication, retain evidence, report both clocks and dependent BLOCKED IDs, continue independent D obligations, hold E.
- Pending interrupt or trigger after mask capture: report the later actual callback clock; do not rearm or approximate.
- Trigger inside stolen cycles: preserve one permit per advanced cycle and distinguish trigger/write clocks.
- Stolen first resumed access: allow the idle/stolen prefix, require the first CPU access at PC and compare the complete marker-relative window using that run’s history.
- Provider-only shim consumer: classify transitively and audit discovery; keep managed fakes selected.
- Unwritable/missing audit log: fail loudly; absence never proves zero attempts.
- Changed known-red set: fail exact-identity acceptance without silently expanding the exception.
- Windows autocrlf: combine preserved-byte hashes with Git-aware restored-path comparison.
- Interruption after one host or candidate write: retain originals and unique evidence, resume only verified paths and never automatically reapply dirt.
- Low-clock/prelude interaction: require the documented budget exception, full window and bounded terminal result.
- MAINCPU succeeds but later module fails: `load,-1`/reset is failure, never full resume.
- Positive nonzero writer failure: reject publication and record candidate absence honestly under N28.

All four mandatory surfaces pass: requested claims/live evidence; workspace/operator rules; class 1 requirements/mappings; and the holistic plan, dependencies, rollback and what-ifs. No remaining unsafe rule chain or silent-completion path was identified.

Accuracy is **98%** because the live state, source paths and operational contracts are independently supported, with residual narrative overstatement disclosed as N27. Completeness is **98%** because all prior IDs, historical dispositions, named obligations and requested what-ifs are covered; N28 strengthens an existing safe failure contract. Future native execution remains implementation work.

The complete persisted prose hash is `4B1D17E4FDA8AA1C33D4D081FCE4FF6FC1DB5BA90104F286F3D72D6A2EC6933E`; JSON hash is `AD2518B740EF3E49F24D69335561BC3AA5DC832F169700F150134958AFB4BFA0`. Both raw readback hashes match exactly. The supported stop gate exited 0 without blocking. Historical queues were left untouched. No commit, push, move, native-source edit or materialization write occurred.