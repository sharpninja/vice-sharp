# Claude Test Prompt: BDP-Compliant Regression Test for Sprite DMA Cache Optimization (PERF-SPRITE-DMA-OPT-001)

You are operating in the F:\GitHub\vice-sharp workspace.

## Your Governing Directive (verbatim from the user)
"Don't sit there offering things. That is not useful. Formulate a plan. Write the unit test to prove it doesn't cause regressions, then implement it. The test is a useful artifact no matter what. Iterate until you get to the 25% threshold."

## Task (One Coherent Gated Slice Only)
The expensive per-cycle sprite DMA mapping logic added for correctness (TR-VIC-EDGE-004, non-PAL per-model tables from VICE vicii-chip-model.c) became the dominant hotspot in dotTrace CPU sampling on the running Avalonia emulator in warp mode at BASIC READY.

A cheap cache of the two stall-window booleans was added to Mos6569 to remove repeated fan-in cost from render and stall-check paths while keeping the full VICE-derived Compute path as the single source of truth inside the authoritative latch update.

**The missing artifact is the dedicated BDP regression test that proves the cache produces identical observable behavior to the full compute path for all models and key combinations.** This test is valuable on its own even if further speed work is required to reach the target.

Your job is to deliver exactly that test artifact following the Byrd Development Process v3 and every rule in AGENTS.md with zero exceptions.

## Strict Rules You Must Follow (No Exemptions)
- Read AGENTS-README-FIRST.yaml and AGENTS.md immediately.
- Bootstrap your matching plugin (Claude uses mcpserver-claude-code-plugin per the marker) and verify marker signature + health nonce before any MCP session/TODO work.
- Route ALL session log, TODO, and requirements operations exclusively through the plugin (workflow.sessionlog.* or McpSession.psm1 equivalents). Never read or write docs/todo.yaml directly.
- Use canonical IDs: Session = Claude-YYYYMMddTHHmmssZ-suffix; Request = req-YYYYMMddTHHmmssZ-###-slug; TODOs in form PHASE-AREA-###.
- BDP v3 (tests first):
  1. Write the unit test(s) covering the full acceptance criteria before any other code change.
  2. Each test XMLDOC must state the driving FR-*/TR-*/TEST-* IDs, use case, and acceptance criteria.
  3. Validate with mocks/stubs first (ensure the test would catch divergence; run red then green).
  4. Only after the test is validated do you touch production logic (here the minimal TestOnly hooks if still needed).
  5. The entire relevant test suite (current + previous) + LockstepValidationTests.First100000CyclesMatch must be green before you consider the slice complete.
- One coherent gated slice only. Do not broaden to fix unrelated non-PAL DMA test failures, status bar text, or other hotspots.
- Vice-sharp scope only. Do not mix errata about the plugin itself into this workspace context.
- Writing style: Never use the em dash (U+2014) or en dash (U+2013 except numeric ranges) in any chat, code comments, commit messages, docs, or session logs. Use hyphen, colon, period, semicolon, or parentheses.
- Azure DevOps (origin) is the primary source of truth. GitHub is a downstream mirror only.
- Post a new session log turn before starting work. Update it with actions and results.
- Identify exact canonical FR/TR/TEST IDs plus the VICE source files before any code change.

## Technical Context You Must Use
The cache fields and population logic are already present in the tree (this is the "implement" part that was done earlier). Your BDP-mandated first artifact is the missing test that locks the behavior.

Key locations (read these first):
- src/ViceSharp.Chips/VicIi/Mos6569.cs (sprite DMA latch, ComputeIsInSpriteDmaStallWindow, MapCurrentCycleToRasterX, the two private bool _inSpriteDmaStallWindow0/1 fields, the public IsCpuCycleStolen / IsCpuCycleStealMandatory properties that now use the cache, the TestOnly_* accessors, UpdateSpriteDmaLatchForCurrentCycle, LatchSpriteDmaForCurrentLine, reset path).
- tests/ViceSharp.TestHarness/VicIISpriteDmaStallTests.cs (existing PAL/NTSC/FLI facts for style, the NtscDmaTableStub pattern, AdvanceTo/BuildVic helpers, the class-level XMLDOCs).

The cache mitigation (PERF-SPRITE-DMA-OPT-001 / BACKFILL-VIDEO-001):
- Private fields (populated once per cycle in the authoritative paths only):
  private bool _inSpriteDmaStallWindow0;
  private bool _inSpriteDmaStallWindow1;
- Public properties (now cheap field reads + badline term):
  public bool IsCpuCycleStolen => (IsBadLine && RasterX >= 12 && RasterX < 55) || _inSpriteDmaStallWindow0;
  public bool IsCpuCycleStealMandatory => (IsBadLine && RasterX >= 13 && RasterX < 56) || _inSpriteDmaStallWindow1;
- Population happens after RasterX advance / ClearExpired and after mask changes in LatchSpriteDmaForCurrentLine. Reset zeros both.
- Full expensive VICE table math (ComputeIsInSpriteDmaStallWindow calling MapCurrentCycleToRasterX + delta -3..+1 + cpl wrap using the per-model tables) still runs exactly once per cycle in the latch path.

## VICE Source Evidence (verbatim, use these in your XMLDOC)
- vicii-chip-model.c:272-403 (cycle_tab_ntsc + SprDma*/BaSpr* / ChkSprDma for 65 cpl 6567R8/8562).
- vicii-chip-model.c:437-566 (cycle_tab_ntsc_old for 64 cpl 6567R56A).
- vicii-cycle.c:118 (check_sprite_dma Y-latch entry point), :499/502/503 (model cycle count flags + late-line handling + cycle_is_check_spr_dma), :54-63 (badline check).
- vicii-sprite.c (BA mask composition with badlines, expansion, multi-line DMA height after latch).
- vicii-fetch.c:275-309 (sprite pointer + data fetch side effects driven by the per-model DMA windows).
- vicii-draw-cycle.c (render paths query stolen cycles for every pixel).
- PAL reference: cycle_tab_pal at chip-model.c:111+ (unchanged).

## Driving Requirements (cite all in XMLDOC)
- BACKFILL-VIDEO-001 (native VIC depth + checkpoints).
- FR-VIC-006 (VIC-II cycle stealing / BA windows).
- FR-VIC-010 (model-specific timing: NTSC 65cpl, oldNTSC 64cpl, PAL 63cpl).
- TR-CYCLE-001 (cycle-accurate stolen vs mandatory with 1-cycle offset mirroring bad-line).
- TR-VIC-EDGE-004 (non-PAL DMA tables + data-fetch side effects; the source of the expensive path that became the hotspot).
- TEST-VIC-001 (parity with VICE + lockstep 100k; no regression to PAL or prior NTSC facts).
- PERF-SPRITE-DMA-OPT-001 (hot path reduction without behavioral change).

Reference the existing plan: docs/perf-sprite-dma-optimization-plan-001.md (the user directive is quoted at the top).

## Exact Acceptance Criteria for the Test You Must Write First
Create (or append) one new [Fact] in VicIISpriteDmaStallTests.cs, e.g.:

SpriteDmaStall_CacheEquivalence_FullModels_Badline_Fli_Compose_NoRegression

The test must:
- Use the existing AdvanceTo, BuildVic, and NtscDmaTableStub helpers/pattern.
- Add the minimal public TestOnly accessors to Mos6569 if they are not already present (following the exact style of the existing public GetSpriteDmaAccessTableForTest):
  public bool TestOnly_GetCachedStallWindow(int leadingEdgeOffset) => ... the private field;
  public bool TestOnly_ComputeStallWindow(int leadingEdgeOffset) => ComputeIsInSpriteDmaStallWindow(...);
- Drive real Mos6569 (PAL 63cpl), Mos6567 (NTSC 65cpl), and Mos6567R56A (oldNTSC 64cpl) instances through representative scenarios:
  - Non-bad line + intersecting sprite.
  - Bad line ($30) + intersecting sprite (overlap/continuous stall band).
  - FLI forced badline (YSCROLL write on sprite-intersect line) + sprite DMA compose.
  - Reset mid-frame.
  - Latch timing side effects (enable/disable around check cycles).
- At every sampled cycle and for both offsets (0 = stolen, 1 = mandatory), assert that the cached value exactly equals the authoritative full Compute value.
- Cover the public IsCpuCycleStolen / IsCpuCycleStealMandatory properties indirectly via the hooks.
- Full XMLDOC on the Fact (and on the helper if you create one) containing:
  - Driving IDs listed above.
  - Verbatim VICE source lines.
  - Reference to the plan document and the user's dotTrace finding (MapCurrentCycleToRasterX + repeated Compute fan-in).
  - Use case: any future change to cache population, Compute, latch sites, or the stolen properties must not alter observable stall behavior.
  - Complete acceptance criteria matching the list above.
  - Explicit BDP order statement: "This test (full AC + citations) written first. Mocks/stubs validated red-then-green. Landed cache confirmed only after."
- The test must be executable in isolation via --filter and must pass cleanly.
- After your changes the dedicated new test + the rest of VicIISpriteDmaStallTests (the original PAL facts) must be green. Note any pre-existing non-PAL facts that were already red before your work; do not attempt to fix them in this slice.
- Rebuild (focused on Chips + TestHarness) and run the test must succeed with 0 errors/warnings on the managed side.
- Update the plan document (docs/perf-sprite-dma-optimization-plan-001.md) with a one-paragraph status note only (no other doc bloat).
- Create or append a local session log .md under docs/session-logs/ using a canonical Claude-... ID (or use the plugin if your bootstrap succeeds) recording the turn, actions, and results.
- Use todo_write (harness) or the plugin TODO surface with exactly one item in_progress at a time.

## Success Criteria (Slice Exit Gate)
- The new cache equivalence Fact exists, has complete BDP XMLDOC with all required cites and IDs, and passes.
- Minimal TestOnly hooks are present in Mos6569.cs (if they were missing).
- Clean rebuild succeeds.
- Relevant test suite green for the new artifact.
- Plan and session log updated (minimal).
- You have posted the required plugin session turn(s) with actions and results.
- You have not touched anything outside the sprite DMA cache regression test surface.
- You have not used em/en-dashes anywhere.
- You stopped after this one coherent slice.

## How to Begin (Exact Order)
1. Read AGENTS-README-FIRST.yaml and AGENTS.md in full.
2. Bootstrap your plugin and verify signature + nonce.
3. Post the initial session log turn (plugin).
4. Open a todo list (one item in_progress).
5. Read the plan (docs/perf-sprite-dma-optimization-plan-001.md), the current Mos6569 DMA code (latch, cache fields, properties, Compute, TestOnly hooks if present, reset), and the full VicIISpriteDmaStallTests.cs (style + helpers).
6. Identify the exact VICE lines and requirement IDs (listed above).
7. Write the failing (or red-capable) test + any needed hooks first.
8. Validate red (prove it can catch divergence) then green with mocks/stubs.
9. Confirm the already-landed cache produces identical results.
10. Rebuild + run the full relevant suite.
11. Update the plan (status paragraph only) and session log.
12. Complete the turn via plugin, mark todos, and output the diff + green run results + exact next gated step (the measurement on the 225% profile or the next narrow test-first opt).

Provide the code changes (via the proper edit mechanism), the build/test output showing green, the session log entry, and confirmation that every rule was followed.

The test you deliver must be the permanent, high-value artifact described in the plan. Deliver it exactly.

Begin.