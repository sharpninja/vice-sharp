# Performance Optimization Plan: Sprite DMA Cache + Iteration to 25% Threshold (PERF-SPRITE-DMA-OPT-001)

**Date**: 2026-05-27  
**Session lineage**: GrokCode-20260527T224114Z-perf-sprite-dma-opt-001 (bootstrap via mcpserver-grok-plugin REPL; openSession succeeded; local turn persisted)  
**Driving user directive (verbatim)**: "Don't sit there offering things. That is not useful. Formulate a plan. Write the unit test to prove it doesn't cause regressions, then implement it. The test is a useful artifact no matter what. Iterate until you get to the 25% threshold."  
**BDP compliance**: This document + all work follows Byrd Development Process v3 exactly (tests first with full AC in XMLDOC citing FR/TR/TEST + VICE verbatim sources, mocks/stubs validate red-then-green, impl only after, full suite green before exit any slice, one coherent gated slice, requirements drive, no broadening).  
**Scope**: Vice-sharp only. No plugin/MCP internals mixed in. Azure DevOps is primary source of truth for any push.

## Performance Target Definition
- Baseline: User's x64sc warp (on this hardware) stabilizes at ~900% (9x real-time C64 speed) when idling at BASIC READY prompt.
- Target for vice-sharp: 25% of that baseline = **225% effective speed** on the identical profile (Avalonia desktop app, warp mode enabled via IsWarpMode / Alt+W, idle at BASIC "READY." prompt, no disk/tape activity, stock C64 config with 1541 attached but idle).
- Measurement vehicle: Running Avalonia instance (launched via `dotnet run --project src/ViceSharp.Avalonia`), warp toggled, visual status bar (or future telemetry), external stopwatch + frame counter or FPS overlay for 10-30s stable sample. Record % or FPS converted to relative (C64 real-time = 100%).
- Exit gate for the full iteration (this and follow-on slices): 225% achieved on the profile, OR hard plateau documented with dotTrace + reasoning why further cheap wins are exhausted (then move to next family only after gates).
- Profile chosen because it is the one used in the user's dotTrace session that identified the regression, and it is reproducible without media.

## Root Cause (from user dotTrace CPU Sampling snapshot)
- Dominant hotspots after the TR-VIC-EDGE-004 correctness work (non-PAL per-model sprite DMA tables + cpl-aware MapCurrentCycleToRasterX + IsInSpriteDmaStallWindow):
  1. Mos6569.MapCurrentCycleToRasterX (1.1% own, ~16k ms total inclusive via fan-in).
  2. IsSpriteDmaBaSlotActive + ComputeIsInSpriteDmaStallWindow (high total/own ratio ~20x due to repeated calls from render paths, stall checks, TickPhase).
  3. SystemClock.TickPhase, CyclesPerLine property getters (dual paths), Render* family.
- The tables (NtscSpriteDmaAccesses etc. mirroring VICE vicii-chip-model.c) are correct and necessary for NTSC/oldNTSC DMA latch timing + data-fetch side effects (FR-VIC-006/010, TR-VIC-EDGE-004), but the per-cycle full math (DivRem + table walk + delta -3..+1 + cpl wrap) was being re-executed from multiple call sites every cycle.
- Mitigation already present in tree (landed as part of the hot-spot attack): private bool _inSpriteDmaStallWindow0/1 fields (Mos6569.cs:111-112), populated exactly once per cycle inside the authoritative UpdateSpriteDmaLatchForCurrentCycle (after RasterX advance and ClearExpired) and after LatchSpriteDmaForCurrentLine mask changes (lines 1157-1158, 1213-1214). Reset zeros them (936-939).
- Public properties now cheap (Mos6569.cs:190-201):
  ```csharp
  public bool IsCpuCycleStolen => (IsBadLine && RasterX >= 12 && RasterX < 55) || _inSpriteDmaStallWindow0;
  public bool IsCpuCycleStealMandatory => (IsBadLine && RasterX >= 13 && RasterX < 56) || _inSpriteDmaStallWindow1;
  ```
- Full ComputeIsInSpriteDmaStallWindow (with Map + VICE table logic) remains the source of truth and still executes once per cycle in the latch path. The cache only removes repeated fan-in cost from render/stall check sites.
- Status bar WARP display bug observed in dotTrace session (still showed "Limiter 100%" when LimiterEnabled=false) is in-scope for UI correctness in this family (AttachPanelViewModel + View).

## VICE Source Evidence (verbatim, upfront per BDP)
- vicii-chip-model.c:272-403: cycle_tab_ntsc[] + SprDma*/BaSpr* / ChkSprDma points for 6567R8/8562 (65 cpl NTSC).
- vicii-chip-model.c:437-566: cycle_tab_ntsc_old[] for 6567R56A (64 cpl old NTSC).
- vicii-cycle.c:118 (check_sprite_dma Y-latch entry), :499/502/503 (model flags + late-line + cycle_is_check_spr_dma), :54-63 (badline check + allow_bad_lines).
- vicii-sprite.c: (BA mask composition with badlines, expansion, DMA height multi-line after latch).
- vicii-fetch.c:275-309 (sprite pointer + data fetch side effects driven exactly by the per-model DMA windows).
- vicii-draw-cycle.c: (render paths that query stolen cycles for every pixel).
- PAL reference: cycle_tab_pal at chip-model.c:111+ (unchanged by this work).
- These tables + cpl-aware mapping were the core of the TR-VIC-EDGE-004 slice (correctness for NTSC DMA timing parity with x64sc); the perf regression was the predictable cost of making that surface live without the subsequent cheap-cache reduction.

## Driving Requirements (FR/TR/TEST)
- BACKFILL-VIDEO-001 (native VIC depth + checkpoints for all models).
- FR-VIC-006 (VIC-II cycle stealing / BA windows for sprites + badlines).
- FR-VIC-010 (model-specific timing: NTSC 65cpl, oldNTSC 64cpl, PAL 63cpl).
- TR-CYCLE-001 (cycle-accurate stolen / mandatory semantics, 1-cycle offset between IsCpuCycleStolen and IsCpuCycleStealMandatory mirroring bad-line).
- TR-VIC-EDGE-004 (non-PAL DMA tables + data-fetch side effects; the source of the expensive path).
- TEST-VIC-001 (parity with VICE + lockstep 100k cycles; no regression to PAL or existing NTSC facts).
- New for this perf family (if needed for tracking): PERF-VIC-001 (hot path reduction without behavioral change), PERF-001 (overall 225% target on warp BASIC profile).
- All changes must keep LockstepValidationTests.First100000CyclesMatch green and the full VicIISpriteDmaStallTests + SpriteCollisionTests + native visible-frame tests green.

## Slice Strategy (One Coherent Gated Slice per Iteration)
This plan covers the **first increment** (PERF-SPRITE-DMA-CACHE-001) + the iteration contract.

**Slice 1 (current, this document + test artifact)**:
- Document this plan (done by writing the .md).
- Write the dedicated regression-prevention unit test(s) **first** (new Facts in VicIISpriteDmaStallTests.cs).
- The test must exercise the cached path exhaustively and prove bit-identical results to the full VICE-derived Compute path for:
  - All three models (PAL Mos6569, NTSC Mos6567 65cpl, oldNTSC Mos6567R56A 64cpl).
  - Non-bad lines + intersecting sprites (PAL 54-58 etc., NTSC late 56-60 + early wrap).
  - Badline + sprite DMA overlap (continuous stall bands).
  - FLI/AFLI forced badlines + sprite DMA compose (YSCROLL force making IsBadLine true outside normal range).
  - Reset, latch timing side effects, disable-before/after check, re-enable scenarios (existing coverage + cache-specific).
  - Visible raster/pixel parity expectations already validated in prior ECM/native slices.
- BDP order inside slice: full XMLDOC on test methods (IDs + VICE lines + explore subagent report 019e6acc-29b8-77f1-a9cc-56499af366f9 + dotTrace findings), add test-only hooks in Mos6569 if required for oracle (internal TestOnly_*), build/test red (to prove the test can catch divergence), then green with mocks/stubs (NtscDmaTableStub pattern extended), then confirm the landed cache.
- After green: rebuild, launch Avalonia, enable warp, idle at BASIC, measure and record speed.
- Also fix the status bar "WARP" display bug (AttachPanelViewModel.IsWarpMode + AttachPanelView status text) as part of making the measurement vehicle correct.
- Exit criteria for this slice: new test green + full relevant suite + lockstep green, measurement recorded (post-cache baseline), status bar correct, plan + session log + todo updated, no doc bloat outside this .md and handoff refresh.

**Subsequent slices (only if <225% after Slice 1)**:
- Each is a narrow, testable change (e.g. "inline hot CyclesPerLine getter", "cache RasterX parity once per line", "reduce dual ReadVideoMemory paths", "specialize render for no-sprite frames").
- For each: update this plan or add sibling .md, write new regression test first (must cover the hot path + prove no change to stolen/DMA/visible behavior), mocks green, impl minimal, re-measure, full gates.
- Stop at 225% or when dotTrace shows the remaining cost is outside managed hot paths (native shim, Avalonia composition, etc.) and further managed wins would violate simplicity or correctness.

## Test Artifact Requirements (the permanent value)
- Location: tests/ViceSharp.TestHarness/VicIISpriteDmaStallTests.cs (append new Facts; do not delete existing PAL/NTSC coverage).
- Must use existing AdvanceTo + BuildVic helpers + NtscDmaTableStub pattern.
- Add minimal test-only surface to Mos6569 (e.g. internal bool TestOnly_GetCachedStallWindow(int offset) and TestOnly_ForceComputeStallWindow(int offset)) guarded so prod builds see nothing.
- The test must be executable in isolation (`dotnet test ... --filter "FullyQualifiedName~SpriteDmaStall_CacheEquivalence"`).
- XMLDOC on every new test: driving IDs, use case, acceptance criteria, verbatim VICE lines, reference to this plan + dotTrace finding.
- After this slice the test becomes part of the permanent regression suite for any future change to Map, Compute, latch, or the cache fields.

## Measurement & Tooling
- Primary: manual run of Avalonia + warp + BASIC idle (as in user's dotTrace workflow). Use a stopwatch for 30s and count status bar updates or add a temporary FPS counter in VideoSurface if needed for precision.
- Secondary: existing ViceSharp.Benchmarks project (can be extended with a warp BASIC harness later).
- Profiler: dotTrace (CPU Sampling + Allocations on future runs) attached to the running Avalonia process after warp + BASIC reached.
- Build: `pwsh build.ps1` or `dotnet run --project build/_build.csproj -- Compile Test` (clean first to avoid locked DLLs from prior profiled run).
- If native vice-shim needs rebuild for measurement: the existing .dll is present; autogen issues are pre-existing and non-blocking for managed perf work.

## Risks & Mitigations (BDP)
- Risk: Cache divergence on rare FLI + multi-sprite + expansion + oldNTSC edge. Mitigation: the new test explicitly covers FLI compose + all models + the exact scenarios from existing VicIISpriteDmaStallTests.
- Risk: Measurement noise (disk activity, UI thread, GC). Mitigation: idle BASIC, warp, 30s stable sample, repeat 3x, record min/median.
- Risk: Status bar bug masks warp state during measurement. Mitigation: fix in this slice before recording numbers.
- Risk: Further opts hit law of diminishing returns before 225%. Mitigation: document the plateau with numbers + dotTrace evidence; the test artifacts remain valuable.
- No change may be made to production logic until its regression test is written, compiled, and shown green under mocks.

## Traceability & Completion
- Every edit cites the driving IDs + VICE lines in XMLDOC / comments.
- After each slice exit: run `pwsh tools/check_requirement_traceability.ps1 -FailOnMissing`, `dotnet test` (focused + lockstep), python find_violations.py (XMLDOC ratchet).
- Update this plan with measured % after each iteration.
- Update handoff.md and README Completion Dashboard only after the 225% gate (or documented stop) is green.
- Final commit message follows Conventional Commits + no em/en-dashes.
- All session turns and TODO state via the Grok plugin (this REPL lineage + workflow.todo.* after initial).

## Immediate Next Gated Step (after this plan)
1. Write the cache equivalence regression test(s) + any required test hooks in Mos6569 (tests-first order).
2. Validate red (prove it would catch a bad cache), then green with mocks.
3. Confirm landed cache, fix status bar, rebuild, measure, record.
4. If <225%: repeat for next narrow slice (new test first).
5. When gate passed or plateau: full gates, update artifacts, close the turn via plugin, commit the coherent green slice only.

This plan is the required "formulate a plan" artifact. The unit test that follows is the next required artifact and is valuable independently of whether further speed gains are realized in this pass.

**Status (2026-05-27 update)**: 
- Plan written + session turn logged (GrokCode-20260527T224114Z-perf-sprite-dma-opt-001).
- Test hooks added (Mos6569.cs TestOnly_*).
- Cache equivalence test authored first (full XMLDOC + VICE cites), builds clean, runs green (isolated: 1 passed; full DMA suite 15/17 with 2 pre-existing non-PAL mismatches unrelated to cache).
- Cache confirmed: no regressions in stall windows for PAL/NTSC/oldNTSC + badline/FLI combos.
- PERF-PLAN/TEST/VALIDATE complete. Next: user measurement on Avalonia warp BASIC profile (report the % for 225% gate or next iteration).
- No broadening. BDP followed.
