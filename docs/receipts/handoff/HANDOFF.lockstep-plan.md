# ViceSharp handoff: C64 and VIC-20 lockstep with VICE

Written 2026-10-17T07:40 local (America/Chicago) by Claude Code (claude-fable-5-1) on PAYTON-LEGION2, workspace `F:\GitHub\vice-sharp`, branch `develop`. MCP session `ClaudeCode-20261017T123700Z-lockstep-handoff`, request `req-20261017T123700Z-001-write-single-lockstep-plan-handoff`.

This is one forward-looking handoff. It replaces the need to read the two prior handoffs end to end; where they conflicted, the later evidence wins and the resolution is stated once in section 2. It does not edit the root `HANDOFF.md`, the two preserved `HANDOFF.before.md` files, or any plan. Nothing here is committed. No test, build, HV, requirement write, or native action was run to produce it; every "measured" fact below was read live on 2026-10-17 between 07:37 and 07:40 local, and every "receipt" fact is a claim taken from a named file.

## 1. The goal

Managed ViceSharp runs the C64 and the VIC-20 in half-cycle lockstep with pinned, unmodified native VICE (x64sc and xvic at commit `cc67418cba58e71d8e70fd353e9de47199ad2075`), from reset and from a native `.vsf` checkpoint, for at least 100,000 cycles each, with every divergence reported at its exact half-cycle and no inferred native value. "Done" means: the full applicable current-plus-prior suites pass with zero failed, skipped, and aborted tests; the oracle integrity requirements (`FR-VICE-001`, `TR-VICE-ORACLE-001`, `TEST-VICE-001`) and all seven `TR-CYCLE-001` acceptance criteria have executable evidence from the clean target; and an independent hostile review returns AGREE with accuracy and completeness both at least 98 and zero FAIL or UNKNOWN.

The compared fields per half-cycle, for both machines, are fixed by `PLAN-VICE-IMMUTABLE-001` Phase 4: A, X, Y, S, P, PC; opcode and micro-op phase; bus address, data, and read/write direction; dummy versus real access; IRQ and NMI pipeline stage; stolen-cycle / BA state. One comparator, `tests/ViceSharp.TestHarness/LockstepValidator.cs`, owns the comparison. No second algorithm.

## 2. Where the project truly stands

### 2.1 Resolved account

The C64 core is not "complete" for this goal. `CLAUDE.md` says Iteration 1 (C64) runs lockstep with x64sc; the lockstep pins (`LockstepValidationTests`: `ResetStateMatches`, `First100CyclesMatch`, `First10000CyclesMatch`, `First100000CyclesMatch`, plus the two CIA timer variants) exist and historically passed, but every native result to date was produced by a shim built from a patched `native/vice` checkout. Measured today: the gitlink and checkout are both `cc67418`, with 12 tracked modifications and 2,527 untracked paths, and the prebuilt `native/vice_x64.dll` and `native/vice_xvic.dll` plus hosted `.o` objects sit beside `vice-shim.c` and `vice-shim-vic20.c`. Under `FR-VICE-001` AC3 that evidence is diagnostic only. Resolution: C64 lockstep status is "passing against a diagnostic oracle, unproven against the immutable oracle". That is the real starting point for both machines.

The VIC-20 is further back. `Vic20NativeLockstepTests` compares CPU registers, dual VIA control registers, VIC-I registers, and raster advance against the same dirty xvic shim for budgets of 64 to 5,000 cycles (measured today in the file's `CycleCounts`), gated by `XvicOraclePrerequisites.ValidateCurrent()` (the Grok plan's Slice 3a product, historically 7 tests plus 1 smoke, HV 100/100; Slice 3b manifest input, HV 99/99). The half-cycle comparator for VIC-20 exists only as the single fixture `Vic20HalfCycleComparatorTests.RegistersMatch_BusAddressDiffers_ReportsExactHalfCycle`, and its XMLDoc wrongly cites `TR-CYCLE-001` AC4 (deferred correction tracked in `PLAN-VICEORACLE-001`). Slice 3c (the real per-half-cycle comparison and the six repair pairs) has not started. The 2026-09-29 and 2026-10-11 handoffs disagree on whether Phase 3 was authorized; the later record (2026-10-07 resume update, confirmed in `PLAN-C64SET-001` remaining text) says it was authorized and 3a/3b ran narrowly. Resolution: Phase 3 is authorized; do not ask again for an initial start sentence; 3c is blocked only by the immutable-oracle prerequisites, not by approval.

The immutable-oracle prerequisite (`docs/plans/PLAN-VICE-IMMUTABLE-001.md`, MCP TODO `PLAN-VICEORACLE-001`) is the critical path. Its state:

- Phase 0 (quarantine and preserve the dirty delta): accepted 2026-10-07 by local Grok 4.7 xhigh, AGREE 99/98, 26 PASS, 0 FAIL/UNKNOWN. Receipt prefix `docs/receipts/hv/20261007T171400-0500-phase0-remediation-grok`. Preservation receipt `docs/receipts/native-vice/20261004T090000-0500-preexisting-delta/`. This is the only accepted task in the TODO (measured today: task 1 done, tasks 2 to 6 open).
- Phase 1 (native-safe mock-contract tests): the projects exist and ran green in their last recorded unfiltered Release runs on 2026-10-08: `tests/ViceSharp.OracleContract.Tests` 87/87, `tests/ViceSharp.VsfCompatibility.Tests` 199/199, `tests/ViceSharp.NativeClockProbe.Tests` 69/69 (receipt index `docs/receipts/native-vice/20261008T142142-0500-stdio-mock-contract/`). The required hostile acceptance of this gate never completed: the noon catch-up Opus review stopped on HTTP 429 with a draft DISAGREE 96/94 and 16 findings (prefix `docs/receipts/hv/20261008-noon-catchup-opus.resume-02`), the remediation packet `docs/receipts/hv/20261008T143538-0500-opus-remediation-packet/` was prepared and never launched. Resolution: Phase 1 is "green, unaccepted".
- Phase 2 (wrapper-only feasibility on a separate clean materialization): the materialization `artifacts/immutable-vice/cc67418cba58e71d8e70fd353e9de47199ad2075/source` exists (measured today) and the stock upstream build completed exit 0 under the 2026-10-08 approval ("Following the VICE Team's reqs is approved"). Bounded feasibility is proven: binary-monitor save/load/step acknowledgements on stock x64sc and xvic; an external `vic_cycle` link wrapper that forwards the real native clock; an external clock-permission gate (`tools/immutable-vice/clock-gate.c`) that pauses and resumes the native clock from outside; post-startup CDB attachment observing fast-fetch register pairs and clock writes. Not proven: regular and dummy bus reads with address/data/direction, interrupt stage, stolen cycles, and deterministic per-cycle pause and resume. Two CDB hardware-breakpoint probes recorded zero hits (retained failures). Receipt root `docs/receipts/native-vice/20261008T082100-0500-phase2-feasibility/`. Resolution: Phase 2 is "feasible in part, exit gate not met".
- Phase 3 (classify and migrate the 12 modifications plus the injected `vice/src/vice-shim-runtime.h`, then restore `native/vice`): not started. Destructive; needs the operator's standing 2026-10-04 approval to be reconfirmed at execution time (decision D4).
- Phase 4 (xvic Slice 3c on the accepted wrapper): not started.

Native VSF (`docs/plans/PLAN-VSFLOCKSTEP-001.md`, TODO `PLAN-VSFLOCKSTEP-001`, all eight tasks open): native `.vsf` is the day-one canonical snapshot format and the restored-checkpoint half of the lockstep goal depends on it. Managed resume exists today in the test harness (`LockstepValidator.RunFromSnapshot` and `StageManagedFromNative`, C64 only, register transfer plus `ResetInFlightState`, no pipeline import, no xvic staging, no managed-to-native save). The external legacy `MAINCPU` adapter `src/ViceSharp.ViceCompatibility/X64ScSnapshotCompatibility.cs` exists with 199 passing mock/byte tests and one real stock-x64sc load of the normalized 1.2 fixture (PC `$5769`), but is not wired into any production path. The corrected plan has not passed Gate 0: its last two reviews were DISAGREE 94/82 and DISAGREE 89/84 (2026-10-03), the findings were remediated in the plan text, and no fresh review or operator approval followed. The PR7 work merged from the Linux workspace added versioned managed runtime snapshots (v3 machine snapshots with clocks and topology); that does not satisfy, waive, or replace the VSF requirement. Resolution: VSF Gate 0 is open; Slices 1 to 5 are on this goal's critical path (restored-checkpoint lockstep for both machines); Slices 6 and 7 (canonical UI/protocol routing, MSI, final corruption matrix) are product completion, not lockstep (decision D5).

Requirements (measured today through the plugin `requirements_list`): FR 235, TR 139, TEST 142, mappings 233, identical to the 2026-10-11 counts. All lockstep records are pending with zero satisfied criteria: `FR-VICE-001` (4 AC), `TR-VICE-ORACLE-001` (5), `TEST-VICE-001` (4), `TR-CYCLE-001` (7), `FR-CPU-002` (6), `FR-VIC-006` (6), `FR-VIC-010` (7), `FR-SNP-005` (9), `TR-LOCKSTEP-VSF-001` (7), `TEST-SNP-002` (8), `FR-VSFLOCKSTEP-001` (3), `TR-VSFLOCKSTEP-RESUME-001` (2), `TEST-VSFLOCKSTEP-001` (1). Two anomalies observed today, not in either prior handoff: `FR-VIC-001` (C64 raster engine) reads `status=closed` with 0 of 8 criteria satisfied, and `FR-VIC20-001` reads `pending` with 6 of 6 satisfied. Neither was changed (decision D8). The R0 sixteen-record tape/CPU/media requirement delta (`docs/receipts/requirements-recovery/20261013T231703-r0-anchor-remediation/proposed-field-delta.json`, SHA256 `63386E9251185B101F53E9257CDA9270025A68006B83F243314E6E299A55D702`) has an accepted review (Opus 5.5 max, 2026-10-13, AGREE 99/98, 49 PASS, 0 FAIL/UNKNOWN, verdict `docs/receipts/hv/20261013T232143-r0-anchor-opus/verdict.md`, SHA256 rechecked today `B94E8FA19985F13FED9476CC686D2FDE8F39EBA20DF439EAC24E7ACAA780A1D9`) and remains UNAPPROVED and UNAPPLIED. It matters here because it rewrites `FR-CPU-004` and `TEST-CPU-001` criteria that the full-suite C64 acceptance tests must satisfy (decision D2).

Full-suite health, which every phase exit requires, is unknown on this machine. The last measured C64 acceptance filter (2026-09-30, 98 tests across the `Fr*AcceptanceTests` classes) was 52 failed / 46 passed; two classes were later fixed in a focused 9/9 run and the rest (`FR-CPU-004` port and tape bits, `FR-SID-005`, `FR-CFG-003/004`, `FR-CIA-005`, `FR-CRT-002`, `FR-INP-003`, `FR-MED-005`, `FR-MON-005/006`, `FR-SNP-003`) stayed red. The merged PR7 Linux work reports 487 focused passes on Linux plus five preexisting CPU/NMI failures that reproduce on `ed7c2e6`; their names are only in the Linux workspace's `TestResults/pr7-resolution/` (absent here, measured). The only Windows gate since the merge is the RomM integration (Nuke `RommDesktopTest`: 46 mock-contract, then Heads 21 and Library 201 passed, 13 Linux-only tests excluded by operator approval; `docs/receipts/worktree-integration/20261016T102915-review.md`). No TestHarness run has happened on Windows since the merge because its normal build invokes `native/Vice.Native.proj`, which mutates `native/vice`. Resolution: a measured Windows baseline is the first action (Phase A).

Repository (measured today): `develop` at `d00e8c5584d0031b3637c649335964f8d5225da0` equals `origin/develop`; `main` at `922148892b44cb711f20874c7c143e559ecb0a57` equals `origin/main`; `develop` is ahead of `main` by three documentation commits (`c4d6a36`, `5a51647`, `d00e8c5`). Working tree: `docs/receipts/handoff/README.md` modified (uncommitted rewrite of the receipts index into a full merge) and `.tmp-c64wiki/` untracked. The committed `docs/receipts/handoff/HANDOFF.merged.md` is a compare-and-contrast register of the two prior handoffs; it is history, not instructions. Payton's later Oct 16 instruction (`develop` is the working baseline; PR7 merged `main` into `develop` on origin) governs branch choice (decision D6).

Open TODOs (measured today): 39 open. On this path: `PLAN-VICEORACLE-001`, `PLAN-VSFLOCKSTEP-001`, `PLAN-C64SET-001` (holds the Phase 3 authorization history), `PLAN-GROKCOMPLETION-001` and `PLAN-REQAUDIT-001` (the whole-goal and requirement-audit containers), `PLAN-VICBA-001` (VIC-II 3-cycle BA to AEC prefetch model; a known C64 lockstep defect against raster-bar workloads), `BUG-DRV1540-001` (1540 boot-phase offset; needs per-drive-cycle native observability, which the wrapper work can supply), `PLAN-NATIVERESIDUE-003` (intermittent native ACCESS_VIOLATION under sustained create/dispose; a full-suite stability risk). `PLAN-ARCHVIC20-001` has no tasks or requirements recorded; it is the VIC-20 architecture container and should be linked, not duplicated.

Hostile validation: the designated reviewer is locally launched Claude Code `claude-opus-5-5` at effort `max` while Grok is unavailable (operator directive 2026-10-08, reaffirmed through 2026-10-13). The 2026-10-16 instruction "no more HV" was given for the RomM worktree integration and its running review; the standing profile rule (hostile AGREE before any done state) and every plan gate still require HV (decision D3). Launch shape and receipt contract are in `goal-plan.reference.md` (section "Hostile review runner") and the operator profile section 7.

MCP and plugin (measured today): marker `AGENTS-README-FIRST.yaml` reports server `1.4.40+bcae36fd`, PID 6372, written 2026-10-16T22:40:06Z, plugin contract 1.118.0; the active plugin cache is `C:\Users\kingd\.claude\plugins\cache\mcpserver-local\mcpserver\1.119.0` (version drift from the marker; not repaired). The user settings install the MCP hook bridge for SessionStart, UserPromptSubmit, PostToolUse, and Stop with `PluginRootHint F:\GitHub\mcpserver-claude-code-plugin`. `CLAUDE_PLUGIN_ROOT` is unset in this session, so per the profile the hooks are reported as unverified, not repaired. The plugin's 26 pending, 26 failsafe, and 2 quarantine items reported on 2026-10-16 were not reconciled; keep `MCP_FAILSAFE_DRAIN_DISABLED=1`.

### 2.2 Conflicts resolved

- "Phase 3 needs a start sentence" (Sept 29 and Oct 4 text) versus "Phase 3 was authorized, 3a/3b ran" (Oct 7 onward): authorized. Immutable prerequisites gate 3c, not approval.
- "Reviewer is local Grok" / "alternate Grok and Opus" / "Opus 5.5 max only": Opus 5.5 max only, until Payton changes it.
- "Reviewer has no MCP access" (Sept 30 supplied-export review): historical. Reviewers log their own MCP turn and the parent reads the full verdict back.
- "Native VICE must be restored now" (Oct 3 and 4) versus "do not restore anything yet": restoration is immutable-plan Phase 3, after Phase 1 and 2 acceptance, under reconfirmed approval. Nothing is restored before that.
- "RED tests first" (older plan and PR7 Linux wording) versus "mocks green first": mocks green first, with discriminating negative controls inside a green harness. Red production runs are never a gate.
- "Phase 2 of the C64 iteration has not exited" (Sept 30 phase-exit gate, 94 requirements in scope) remains true and is folded into Phase E below as the C64 acceptance-suite remediation.
- Effective requirement counts: FR 235 / TR 139 / TEST 142 / MAP 233 (Oct 11 and today). Older 232/138/140/231 figures are the Sept 30 store.
- Handoff receipts index: the committed `HANDOFF.merged.md` and the uncommitted `README.md` rewrite are both compare-and-contrast artifacts. This file is the single forward account; neither of those is an instruction source.

## 3. Operating contract for this path

These are the rules the next agent inherits, stated once:

- VICE is immutable. Never edit, patch, generate into, clean, or restore `native/vice` outside immutable-plan Phase 3 with reconfirmed approval. Build only from the separate materialization under `artifacts/immutable-vice/`. On every mismatch answer how managed diverged and how it returns to VICE, then change managed code.
- Missing native observation fails closed. No `PeekBus`, PC inference, fabricated bus values, or comparator exclusions. A capability the wrappers cannot provide is a named blocker reported to Payton, not a reason to touch VICE.
- BDPv4 as the operator defined it on 2026-10-08: requirement-derived tests, independent discriminating mocks green before production code, negative controls assert rejection inside a green suite, then bind the same assertions to production, then the full applicable current-plus-prior suite with zero failed, skipped, and aborted tests. Focused filters are diagnostic only.
- Every phase exit: full suite receipts (exact command, TRX, counts), native before/after inventory unchanged, request and response JSONL under `docs/receipts/hv/{yyyyMMddTHHmmss}-{gate}.*`, full verdict persisted in the reviewer's MCP turn and read back, AGREE 98/98 with zero FAIL/UNKNOWN. No TODO, requirement, AC, or plan step is marked done otherwise.
- Requirement text changes are planned and approved before implementation (profile 4.3). Bugs in already-specified behavior are pre-approved once the plan is stated.
- Local execution on PAYTON-LEGION2 through PowerShell.MCP; PowerShell only; no Python; JSON and YAML from native objects; GitHub `origin` only; no commit or push without a file-named order; never force-push; copy, verify, and retain originals; never print credentials.
- Keep the C64 idle pins (`First10000CyclesMatch`, `First100000CyclesMatch`) green on every CPU or clock edit; confirm the filter matches before relying on it.
- Session logging through the plugin: one turn per request, decisions recorded as conclusions, titles summarizing the request.

## 4. The plan

Phases are sequential unless stated. Each lists entry, work, exit, and the requirements and tests it serves. "HV" means the Opus 5.5 max gate described above.

### Phase A: Measured Windows baseline and decisions (no product code)

Entry: this handoff, `develop` at `d00e8c5`, decisions D1 to D9 answered or explicitly deferred by Payton.

Work:
1. Re-run the three native-safe suites unfiltered in Release through PowerShell.MCP (`ViceSharp.OracleContract.Tests`, `ViceSharp.VsfCompatibility.Tests`, `ViceSharp.NativeClockProbe.Tests`) with native before/after inventory capture; record counts against the Oct 8 figures (87, 199, 69).
2. Run the existing TestHarness lockstep and acceptance filters once as a diagnostic against the current prebuilt dirty shim, without rebuilding it (do not invoke `Vice.Native.proj`; if the harness build cannot avoid it, stop and report rather than build). Targets: `LockstepValidationTests`, `Vic20NativeLockstepTests`, `Vic20HalfCycleComparatorTests`, `CpuVisiblePcTimingTests`, `X64ScVariantLockstepTests`, every `Fr*AcceptanceTests` class, `Category=Determinism`. Identify the five CPU/NMI failures from PR7 by name. Label all of this "diagnostic oracle".
3. Record the baseline as `docs/receipts/lockstep/{yyyyMMddTHHmmss}-phase-a-baseline/` with commands, TRX, counts, native inventory, marker hash, and the decision answers.
4. Link `PLAN-ARCHVIC20-001` to `FR-VIC20-*` and this handoff through the TODO wrapper; add a checkpoint to `PLAN-VICEORACLE-001` naming this file. No other TODO or requirement mutation.

Exit: baseline receipt exists; decisions recorded in the session turn; no HV required (operational class).

### Phase B: Accept the native-safe mock gate (immutable Phase 1)

Entry: Phase A receipt.

Work: repin the 826-input packet from `docs/receipts/hv/20261008T143538-0500-opus-remediation-packet/` against the current tree, disclose every changed input, then launch one Opus 5.5 max review of the whole Phase 1 scope (the three mock projects, the 16 draft findings and their dispositions, the shared fixtures, stdio mock contract, and the Oct 8 evidence corrections). Remediate findings inside the mock projects only; no production code.

Exit: AGREE 98/98, zero FAIL/UNKNOWN, full verdict read back; the three suites unfiltered green; native inventory unchanged; `PLAN-VICEORACLE-001` task 2 done. Serves `FR-VICE-001` AC1 to AC4, `TR-VICE-ORACLE-001` AC1 to AC5, `TEST-VICE-001` AC1 to AC4, `TR-CYCLE-001` AC4 at contract level.

### Phase C: External bus observer and cycle control for both machines (immutable Phase 2)

Entry: Phase B accepted.

Work, in the plan's mechanism order (public interfaces, external process control, link-time interposition outside both source trees), on the separate materialization only:
1. Specify the observation record (half-cycle index, PHI1/PHI2, AEC, BA, access kind read/write/idle/dummy, address, data, interrupt stage, stolen-cycle flag, clock) as a mock-first contract in `ViceSharp.OracleContract.Tests` and `ViceSharp.NativeClockProbe.Tests`, with negative controls for every missing field; mock gate green; HV.
2. Bind the stdio diagnostic adapter (the 19 mock-contract cases in `StdioAdapterContractTests`) to a real C binding; prove stdout retention and broken-pipe behavior.
3. Extend the external `vic_cycle` and clock-gate wrappers to emit the full record for xvic, then add the x64sc equivalent (`maincpu` and `vicii-cycle` hooks) by the same link-time method. Deterministic per-cycle pause/resume replaces the instruction-boundary `-limitcycles` termination.
4. Prove the real observations against the stock binaries for the ROM-only workloads in `docs/requirements/oracles/vic20-xvic-manifest.json` and the C64 reset workload, retaining raw traces and hashes.
5. Legacy VSF `MAINCPU` adapter: keep the external adapter as the only compatibility path (serves `TR-VSFLOCKSTEP-RESUME-001` AC1).

Exit: full applicable suite green (native-safe projects plus the wrapper tests); separate materialization unchanged except inventoried generated files; dirty `native/vice` byte-identical; HV AGREE; `PLAN-VICEORACLE-001` task 3 done. If any required field cannot be observed without touching VICE, stop with a named blocker. Serves `TR-VICE-ORACLE-001` AC3 and AC5, `FR-VICE-001` AC4, `TEST-VICE-001` AC4.

### Phase D: Native-safe builds and restoration of the oracle (immutable Phase 3)

Entry: Phase C accepted; decision D4 reconfirmed in writing at this point, not earlier.

Work:
1. Rewrite `native/build-vice-shim.ps1` and add `build-vice-shim-xvic.ps1` to materialize `cc67418` into the clean root, build through MSYS2 MINGW64, link only external translation units, and keep every generated file outside both trees (serves `TR-VSFLOCKSTEP-RESUME-001` AC2; VSF Slice 1 tests `ImmutableViceBuildTests.*`, `NativeViceSnapshotPrerequisiteTests.*`, mocks first).
2. Change `native/Vice.Native.proj` and the Nuke `BuildViceNative` path so the TestHarness build never writes under `native/vice`. Make the three native-safe projects part of `ViceSharp.slnx` and the Nuke `Test` target only now (today the solution lists six test projects and none of the three).
3. Classify each of the 12 tracked modifications and the injected header (observation to rehome in wrappers; legacy snapshot normalization already rehomed; behavior corrections that belong in managed code; target modifications to reject). The historical reasons are in the Phase 0 preservation receipt and `native/patches/vice-shim-runtime.patch` (evidence only, never applied).
4. Restore `native/vice` to the gitlink: tracked diff empty, `status --porcelain=v1 -uall` empty, only classified generated or injected paths removed, nothing else cleaned. Copy, verify by hash, and retain the dirty state in a receipt first.
5. Rebuild both shims from the clean materialization and re-run every native-backed suite against them.

Exit: clean-target preflight passes (`TEST-VICE-001` AC1, AC2); C64 idle pins and `Vic20NativeLockstepTests` pass against the immutable shim (this is the first immutable-oracle evidence for both machines); `REAL-C64-PHI2-AEC-001` passes with real x64sc half-cycle access observations (`TR-CYCLE-001` AC4); comparator XMLDoc corrected; full suite green; HV AGREE; `PLAN-VICEORACLE-001` tasks 4 and 5 done.

### Phase E: C64 half-cycle lockstep against the immutable oracle

Entry: Phase D accepted. Runs before VIC-20 because the shared CPU engine and `SystemClock` are repaired here and the C64 pins are the regression fence for everything after.

Work, each item as a mock-first slice with its own full-suite gate and HV:
1. Extend `LockstepValidator` to compare the full field set from the Phase C observer for x64sc (today it compares registers and CIA timers); mock fixtures accept equal records and reject each field mismatch at the exact half-cycle.
2. Remediate the five preexisting CPU/NMI failures named in Phase A and the red `Fr*AcceptanceTests` criteria that touch the CPU, CIA, VIC-II, and port (`FR-CPU-004`, `FR-CIA-005`, `FR-VIC-006`, `FR-VIC-010`); reconcile each to its VICE-derived AC first, never by weakening assertions. `FR-CPU-004` criteria depend on decision D2.
3. `PLAN-VICBA-001`: port VICE's stateful 3-cycle BA to AEC `prefetch_cycles` model (`viciisc/vicii-cycle.c`), model-aware PAL/NTSC, with `RasterBarLockstepTests` and `X64ScVariantLockstepTests.FirstTenProfileFramesMatch` green together (`TR-CYCLE-001` AC2).
4. If the per-instruction phase flags in `Mos6502.cs` cannot satisfy the full-field comparison, execute locked decision 7 of the goal plan: one per-cycle engine whose phases are the VICE macros in `6510dtvcore.c`, 256 opcodes, decimal ADC/SBC, indirect JMP page wrap, JAM; pin each closed mismatch in `CpuVisiblePcTimingTests`. This is the architectural break the plan already approved; it still gets its own decision-complete slice plan before code.
5. Restored-checkpoint lockstep: VSF Slices 2 and 3 (production C64 bridge, full pipeline and media state transfer, `C64ViceSnapshotLockstepTests.RestoredCheckpoint_MatchesX64ScFor100000Cycles`). Requires decision D5.

Exit: from-reset 100,000-cycle full-field lockstep green on every C64 profile in `X64ScVariantLockstepTests`; restored-checkpoint 100,000-cycle lockstep green on the supplied fixture (`tests/ViceSharp.TestHarness/Fixtures/Vsf/ready-c64sc-truedrive.vsf`, SHA256 `5C284F5CF0A1BC7E01A2E4117A74158B7A9D60485F5626448F4B7655C0E23C19`); Lorenz suite and the VICE cycle-exact test programs match x64sc (`TR-CYCLE-001` AC5, AC6) with named workloads added to a C64 oracle manifest beside the VIC-20 one; full suite green; HV AGREE.

### Phase F: VIC-20 half-cycle lockstep (Grok plan Slice 3c, immutable Phase 4)

Entry: Phase E accepted. The six repair pairs are the goal plan's locked order; each is a mock-first slice, its own diagnostic filter, the full suite, C64 pins green, and HV.

1. Foundations: `SystemClock`, VIC-20 memory map and banking, bus. No instruction semantics.
2. CPU engine on the VIC-20 (`vic20cpu.c` fast opcode-fetch path, `maincpu_steal_cycles` bookkeeping observed through the wrapper); pin mismatches in `CpuVisiblePcTimingTests`.
3. Machine glue (`PLAN-ARCHVIC20-001`: RAM expansion variants, cartridge port, IEC wiring).
4. VIC-I (6560/6561), including `FR-VIC20-001` criteria (today marked 6 of 6 satisfied while pending; re-prove them here).
5. VIA (shared chip, VIC-20 wiring in the machine adapter), IEC, input, storage, tape.
6. Audio, device side effects, reset, snapshot, determinism, host pacing. `FR-VIC20-005` cartridge criteria (FE3, Ultimem, Mega-Cart) close here.
7. Restored-checkpoint lockstep: VSF Slice 4 (`Vic20ViceSnapshotLockstepTests.RestoredCheckpoint_MatchesXvicFor100000Cycles`) with the generated `ready-xvic-pal-unexpanded.vsf` and `ready-xvic-ntsc-unexpanded.vsf` fixtures from the ROM allowlist in the VSF plan.

Exit: from-reset 100,000-cycle full-field lockstep green for `vic20` and `vic20ntsc` on the manifest workloads; restored-checkpoint lockstep green; `Vic20DeterminismTests` green; full suite green; HV AGREE; `PLAN-VICEORACLE-001` task 6 done.

### Phase G: Whole-goal closure

Entry: Phases E and F accepted.

Work: VSF Slice 5 (managed-to-native write so both machines round-trip through the native writer, `NativeViceSnapshotRoundTripTests.*` for all four directions); `tools/check_requirement_traceability.ps1` and `tools/check_phase_acceptance_coverage.ps1` run (today's known debt: 86 unreferenced canonical IDs and 1,011 noncanonical references, not a clean gate); `git diff --check`; wiki export through the plugin; requirement satisfaction set only through the plugin after the final HV; rewrite the root `HANDOFF.md` to the end state; commit and push only on a file-named order.

Exit: the definition of done in section 1. VSF Slices 6 and 7, `PLAN-C64SET-001`, the dense-session proof, RomM, and UI plans remain their own TODOs (decision D1).

## 5. Risks and blockers

- Observer feasibility (Phase C) is the single largest risk. Optimized and inlined paths (`vic20cpu.c` fast fetch, inline `alarm_set`) hide the bus from link-time wrapping; the two CDB breakpoint probes found nothing. Mitigation: stay in mechanism order, retain every failed probe, and stop at a named blocker rather than lower the field set. A blocker leaves the goal incomplete; it is not a completion condition.
- Full-suite health. Zero failed and zero skipped is required at every exit, and the current tree carries 52 historical C64 acceptance failures (Sept 30), five CPU/NMI failures from the merge, three `Skip =` sites in the harness (measured today), and `PLAN-NATIVERESIDUE-003` instability. Phase A measures it; Phase E pays it down. Do not weaken assertions to pass.
- The CPU engine rewrite (Phase E step 4) is the highest blast radius: `Mos6502.cs`, opcode files, `SystemClock` steal/BA, snapshot staging, `LockstepValidator`, C64 and VIC-20 lockstep, and every test that pins the hybrid phase flags. It needs its own decision-complete slice plan and hostile plan review before code.
- Requirement drift. The R0 delta is accepted but unapplied; `FR-VIC-001` and `FR-VIC20-001` carry inconsistent status fields; the mapping helper can replacement-upsert and lose links (McpServer triage `5deae93eb0764736ae97184817d14eb6`). Apply one reviewed delta under a verified exclusive-writer window with full readback, never overlapping drafts.
- Branch divergence. The Linux workspace published to `main` while this chat works on `develop`. A second merge of the same files is likely unless one branch is declared the only implementation branch.
- Reviewer availability. Opus 5.5 max hit session limits twice (Oct 8 noon, Oct 11 R0 round). Review time is unbounded; do not schedule retries, and resume the same logical review where the CLI supports it.
- Native restoration is destructive and irreversible for the untracked paths unless copied first. Phase D step 4 copies and hash-verifies before any removal.
- Prior unresolved process items still ride along: the F-02 credential-exposure disposition, four historical reviewer-queue records, 26 pending plus 26 failsafe plus 2 quarantine plugin items, and the Oct 16 publication exclusions (465 files, 113 formerly tracked). None blocks lockstep work; none is closed by this handoff.

## 6. Decisions Payton must make before the next agent goes past Phase A

Each is a real fork. The next agent must not assume an answer.

- D1. Scope. Confirm that this lockstep path is the critical path and that the non-lockstep items of the Grok whole-goal plan (dense session-log proof, Phase 4 settings, VSF Slices 6 and 7, RomM follow-ups) are parked in their own TODOs rather than sequenced ahead of Phase B.
- D2. R0 requirement delta. Approve applying the exact sixteen-record candidate (SHA256 `63386E92...55D702`) with the idempotency and exclusive-writer decisions its README asks for, or defer it. It changes `FR-CPU-004` and `TEST-CPU-001` criteria that Phase E must satisfy.
- D3. Hostile validation. Confirm that "no more HV" (2026-10-16) was scoped to the RomM integration and that every phase gate here uses locally launched `claude-opus-5-5` at `max`, or name a different reviewer and model.
- D4. Native restoration. Reconfirm, at Phase D entry, the 2026-10-04 approval to restore `native/vice` to `cc67418` after Phases B and C are accepted and the dirty state is copied and hash-verified.
- D5. VSF plan. Approve the corrected `PLAN-VSFLOCKSTEP-001` Gate 0 for Slices 1 to 5 as part of this path (Slice 1 is folded into Phase D), with Slices 6 and 7 deferred to product completion, or require the whole plan in sequence.
- D6. Branch. Confirm `develop` is the only implementation branch for this path and that the Linux workspace stops publishing product changes to `main` until the next explicit merge.
- D7. Carry-over process items (F-02 credential disposition, historical reviewer-queue records, plugin queue reconciliation): dispose now, or leave open and out of this path.
- D8. Requirement status anomalies. Authorize correcting `FR-VIC-001` from `closed` to `pending` and reviewing the `FR-VIC20-001` 6 of 6 satisfied flags through the plugin, or leave them until Phase G.
- D9. Windows gate denominator. Confirm that the 13 explicitly Linux-only tests excluded on 2026-10-16 stay excluded from the Windows full-suite rule, so "zero skipped" is measured against the Windows-applicable set.

## 7. Exact next actions for the next agent

1. Run `/add-profile`, read the whole marker, verify signature and health nonce, bootstrap the Claude plugin, open one session turn titled for the work. Record `CLAUDE_PLUGIN_ROOT`; if unset, report hooks unverified.
2. Confirm `git rev-parse HEAD` on `develop` is `d00e8c5584d0031b3637c649335964f8d5225da0` or later and that `native/vice` still reads 12 modified and 2,527 untracked at `cc67418`. If not, stop and report the delta before anything else.
3. Present section 6 to Payton as one batched question and wait. Do not fix forward on any of them.
4. Execute Phase A exactly as written and write its receipt. Do not build the native shim.
5. On Phase A completion and D3 answered, prepare the Phase B review packet (repin, disclose changes) and launch the reviewer with the standing launch shape, unbounded, with request and response JSONL under `docs/receipts/hv/`.

## 8. Paths, hashes, and identifiers the next agent needs

- Goal plan (external, unchanged): `C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\01a0ef2b-2331-70d1-9c65-0ef12b15d09a\plan.md`, SHA256 `436BB2B1DA834491A9B0C8F0F6EFD3F1EBD81F001885AD31D6FE8180CDE26473`; byte-identical copy `docs/receipts/handoff/20261011T112946-0500-canonical-reconciliation/goal-plan.reference.md`.
- Plans: `docs/plans/PLAN-VICE-IMMUTABLE-001.md`, `docs/plans/PLAN-VSFLOCKSTEP-001.md`, `docs/plans/PLAN-GROK-COMPLETION-20260906.md`.
- Prior handoffs (history only): `docs/receipts/handoff/20261011T112946-0500-canonical-reconciliation/HANDOFF.before.md` (SHA256 `B7A13C695B04D2A2560AEEA3C174189594D8F0F776402662E688C86352E909E8`), `docs/receipts/handoff/20261016T092300-handoff-sync/HANDOFF.before.md` (SHA256 `EA2737B0084B411D9E927227E183596A5779345D809E46B2C968F53A327A9579`), `docs/receipts/handoff/20261016T092300-handoff-sync/HANDOFF.origin-main-6530196.md` (the Linux workspace's PR7 and audio work), root `HANDOFF.md` (Oct 16 operational checkpoint).
- Native oracle: gitlink `cc67418cba58e71d8e70fd353e9de47199ad2075`; materialization `artifacts/immutable-vice/cc67418cba58e71d8e70fd353e9de47199ad2075/source`; preservation receipt `docs/receipts/native-vice/20261004T090000-0500-preexisting-delta/`; feasibility root `docs/receipts/native-vice/20261008T082100-0500-phase2-feasibility/` (subfolders `stock-runtime`, `binary-runtime`, `clock-runtime`, `gate-runtime`, `bus-runtime`, `debugger-runtime`); clock gate source `tools/immutable-vice/`.
- Mock gates: `tests/ViceSharp.OracleContract.Tests`, `tests/ViceSharp.VsfCompatibility.Tests`, `tests/ViceSharp.NativeClockProbe.Tests`; last receipts `docs/receipts/native-vice/20261008T142142-0500-stdio-mock-contract/`; remediation packet `docs/receipts/hv/20261008T143538-0500-opus-remediation-packet/`.
- Accepted Phase 0 HV: prefix `docs/receipts/hv/20261007T171400-0500-phase0-remediation-grok`, primary turn `GrokCode-20261007T222539Z-phase0-remediation-hv` / `req-20261007T222539Z-phase0-remediation-hv`.
- R0 candidate and verdict: `docs/receipts/requirements-recovery/20261013T231703-r0-anchor-remediation/` (delta SHA256 `63386E9251185B101F53E9257CDA9270025A68006B83F243314E6E299A55D702`), `docs/receipts/hv/20261013T232143-r0-anchor-opus/verdict.md` (SHA256 `B94E8FA19985F13FED9476CC686D2FDE8F39EBA20DF439EAC24E7ACAA780A1D9`), reviewer turn `ClaudeCode-20261014T042517Z-plugin-session` / `req-20261014T042520Z-prompt-6807`.
- Requirement recovery key never to be reused: `slice5-apply-clean-18028995ae17`, payload hash `d858d4e30feeab57a4f652ee9e8dc6e3ef70a34e8674e52e7ba5488cfb727164`.
- VIC-20 oracle manifest: `docs/requirements/oracles/vic20-xvic-manifest.json` (ROM-name workloads only; named programs still absent).
- C64 VSF fixture: `tests/ViceSharp.TestHarness/Fixtures/Vsf/ready-c64sc-truedrive.vsf`, SHA256 `5C284F5CF0A1BC7E01A2E4117A74158B7A9D60485F5626448F4B7655C0E23C19`, 190,854 bytes, 24 modules.
- Lockstep code: `tests/ViceSharp.TestHarness/LockstepValidator.cs` (1,782 lines; `RunFromSnapshot` at line 394), `LockstepValidationTests.cs`, `Vic20/Vic20NativeLockstepTests.cs`, `Vic20/Vic20HalfCycleComparatorTests.cs`, `CpuVisiblePcTimingTests.cs`, `X64ScVariantLockstepTests.cs`; production chips under `src/ViceSharp.Chips/Cpu/`, `VicIi/`, `Cia/`; legacy VSF adapter `src/ViceSharp.ViceCompatibility/X64ScSnapshotCompatibility.cs`.
- Parent Codex session that holds the Oct 7 to Oct 16 history: `Codex-20261007T135626Z-plugin-session`. Do not reuse or overwrite its turns.

Accuracy rating: 95 (every state claim above is either read live today or cited to a file; the five PR7 failure names and the current Windows full-suite counts are unknown and said so). Completeness rating: 92 (the plan sequences every known obligation on the lockstep path; it does not enumerate the 32 AC-text-only tape groups or the full VSF Slice 6 and 7 surface, which are deferred by decision D5 and D1).
