# PLAN-NATIVERESTORE-001 step D1 on LAB-OMARCHY: builder, native observers, adapter, requirement amendment, Slice 1 contracts (2026-10-20T21:30 to 21:56)

Executed by Claude Code (claude-fable-5-1-high@ClaudeCode, session OMAR-VICE#) after D0 (receipt `20261020T211402-phase-d-preservation-linux/`, Complete on both hosts: LEGION2 `20261020T213032-phase-d-preservation-windows`, patch byte-identical to the Oct 4 Phase 0 patch). Nothing under `native/vice` was read by a build or changed; no native tool ran (the builder changes are proven by Pester dry runs and a module contract; the real rebuild is D5).

## Commits

- `0a98fe5f` D1 part 1: `src/ViceSharp.ViceCompatibility/X64ScNativeLaunch.cs` (R10 precondition, N05 payload validation, N07 and N23 budget, launch arguments with the precondition flags last); `tools/immutable-vice/snapshot-observer.c` and `.h` (R11 B: wraps `maincpu_snapshot_read_module` and `machine_read_snapshot`; `VICE_SNAP` resume with the status byte recomposed per mos6510.h 116 to 136, resume-fail, load, drives), `snapshot-checkpoint.c` (R12 literal trigger through the pinned trap API; write clock measured in the callback; every nonzero writer result is checkpoint-fail; malformed configuration inert and reported once), `obs-snapshot-glue.c` (maincpu_regs and drive resources through the real headers), `bus-adapter.c` (sink export and the per-cycle checkpoint hook, inert when unset); builder Link stage with the three units and two more wraps.
- `8bb39878` D1 part 2: `SupportsShouldProcess` on `native/build-vice-observer.ps1` and `tools/immutable-vice/Invoke-ObserverEquivalence.ps1` (F09); `native/lib/ObserverBuild.psm1` link-line reuse with object hashes (A06).
- D1 part 3 (this receipt's commit): the R15 requirement amendment through the wrapper, the document mirrors, the Slice 1 amendment in `PLAN-VSFLOCKSTEP-001.md`, the S5 note in `PLAN-OBSERVER-001.md`, `native/README-native-build.md`, the `VsfSlice1Contract` suites.

## Tests first, then implementation (BDPv4 order, with counts)

- Launch adapter: `NativeLaunchPreconditionContractTests` mock gate 13 passed (21:33:59), then `NativeLaunchPreconditionImplementationTests` bound to the same assertions plus the approved fixtures; filter `NativeLaunchPrecondition` 28 passed, 0 failed, 0 skipped (21:35:15).
- Native units: `SnapshotObserverContractTests` and `SnapshotCheckpointContractTests` mock gate 22 passed (21:38:26); implementation tests compiling the units with doubles through the real `--wrap` 16 passed (21:40:46, after one fix: the doubles must define the real symbol names, not `__real_`, because `--wrap` resolves `__real_X` to `X`); whole `ViceSharp.NativeClockProbe.Tests` 358 passed, 0 failed, 0 skipped (21:43:27).
- Builder: `native/tests/ObserverBuild.Tests.ps1` 8 passed (captured, reused, changed object, missing object, no hash list, nothing retained, mixed run); `native/tests/BuildScriptsDryRun.Tests.ps1` 4 passed (both scripts declare `SupportsShouldProcess`; `-WhatIf` for every stage and for the runner creates no directory and no file); all five native Pester suites 29 passed, 0 failed, 0 skipped (21:48:02); PSScriptAnalyzer 0 findings over the module, the tests and both scripts (21:48:39).
- Slice 1 contracts (`Category=VsfSlice1Contract`, mock-bound, N08): `VsfSlice1PrerequisiteContractTests` (libraries from the pinned commit with the required wraps, fixtures present and hash-pinned with the literal xvic checkpoint clock, MSYS2 tools on Windows and pass-through on Linux, generated files outside `native/vice`, pinned tree identical before and after, legacy normalization capability; one rejection control per Slice 1 line 340 defect) and `VsfSlice1ManifestAndResumeContractTests` (schema-1 per-line ledger with omission, duplicate, blank, restore-order and literal-checkpoint controls; regeneration determinism with the N21 and N25 retention and no-invented-hash controls; the resume boundary AC with the constrained positive case, the low-clock positive and fourteen rejected cases; the N18 half-cycle conversion). Whole `ViceSharp.VsfCompatibility.Tests` 240 passed, 0 failed, 0 skipped (21:55:03).

## Requirement amendment (R15, through the wrapper)

`~/codex-dispatch/scripts/Update-TrVsfLockstepResume001.ps1` (dry run first with `-WhatIf`, params written, no store change; then the real run at 21:50): `workflow.requirements.updateTr` on `TR-VSFLOCKSTEP-RESUME-001` with AC1 amended to external real inputs v1.2 and v1.4 (Payton 2026-10-20T17:23, verbatim "change to v1.2 and v1.4") and AC2's builder reference changed to `native/build-vice-observer.ps1` (R2 A, semantics unchanged); `requirements/updateTr.params.json`, `updateTr.result.txt` (type: result) and `listTr.readback.txt` (the amended texts read back). `docs/requirements/technical/TR-Native-Vice-Snapshot.md` mirrors the two criteria; `docs/plans/PLAN-VSFLOCKSTEP-001.md` Slice 1 carries the 2026-10-20 amendment paragraph and the observer builder in its validation commands; `docs/plans/PLAN-OBSERVER-001.md` S5 carries the implementation note.

Observation (not inference): both `listTr` readbacks show every record's `createdAt` and `updatedAt` equal to the call time (02:27:52Z at 21:27 local, 02:50:03Z at 21:50 local) while the amended text is present, so these timestamps are projection times, not stored history; filed as triage `triage-report-48ba2a893bfc46d19f25bac778873d42` (group `triage-group-c7ff4ae6fb02b1ad`, 21:56). Evidence of the amendment is the wrapper call, its result and the readback text, not the record timestamp.

## Not done in D1 (by design)

The real builder rerun with the two new wrappers, the no-snapshot equivalence rerun proving the wrappers inert, the Nuke targets and the generator tool, the `ShimLegacy` tagging, the load detector, `HalfCycleState` and `CompareHalfCycleStates`, the `VsfSlice1Real` tests, and every native run belong to D2 and D5 per the plan.

## Self-assessment

Correctness 96 (every count above is from a recorded run; the native units are proven against doubles, not yet against the pinned binaries, which D5 does), accuracy 98, completeness 94 (D1's list is complete; the Slice 1 contract suites cover each named obligation at the mock level, and the real-bound forms are D5 work).

Next: D2.
