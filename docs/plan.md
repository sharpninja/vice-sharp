# ViceSharp Phase 1 Completion Plan

Updated: 2026-05-21
Workspace: `F:\GitHub\vice-sharp`
Baseline: `main` at `46edda9` with intended local plan updates pending

This file replaces the stale May 12 30-stage execution snapshot in its
entirety. Keep this document as the live Phase 1 closeout plan. Do not append
the old stage list back into this file.

## Sources Of Truth

Use these inputs, in this order, when this document and local state disagree:

1. Live MCP TODO state queried through `mcpserver-codex-plugin`.
2. `README.md` Completion Dashboard and current validation notes.
3. Current git state and recent MCP session log evidence.
4. `handoff.md` for session continuity.
5. Older files such as archived session logs or previous plan snapshots only as
   historical context.

Slice 0 reconciliation is now the baseline for this plan. It used live MCP TODO
state, subagent code/test review, and focused validation to separate stale open
TODOs from real Phase 1 blockers before new feature work continues.

## Phase 1 Definition

Phase 1 means Iteration 1 C64 bringup is complete enough to operate the C64 and
required x64sc profiles through real boot, input, media, capture, snapshot, and
host-control flows with native-comparable validation. It also includes a first
performance-tuning pass that reaches at least 25% of classic VICE performance
on the selected benchmark profile. It does not mean every future VICE machine,
every cartridge mapper, every platform host, or complete performance parity is
done.

Phase 1 is complete only when all of these are true:

1. Every Phase 1 implementation slice is driven by current canonical
   `FR-*`, `TR-*`, and `TEST-*` requirements, with VICE source evidence cited
   before code changes. If VICE source or classic documentation proves a
   requirement is incomplete or wrong, the requirement is corrected before the
   implementation is treated as done.
2. C64 and required x64sc profiles boot to BASIC `READY.` with ROM-backed reset
   and deterministic managed/native validation still green.
3. CPU, 6510 port, PLA, memory map, ROM loader, CIA, SID MVP, VIA, interrupt,
   and reset behavior remain closed under the normal full-suite gate.
4. VIC-II MVP parity includes deeper visible-frame validation: badlines, DMA CPU
   inhibition, sprite DMA, sprite priority and collisions, borders/open borders,
   bank switching effects, raster IRQ behavior, and model timing checkpoints.
5. Media MVP works from the runtime and host surfaces: true-drive 1541/D64
   attach and load path, TAP datasette motor/timing path, standard cartridge
   live memory mapping and boot behavior, full-machine snapshot resume, and
   deterministic frame/audio capture.
6. Keyboard, joystick/control-port, and host/UI control flows are usable through
   the gRPC boundary without bypassing the host-owned emulator lifecycle.
7. Final x64sc lockstep gates run without unsupported required-profile skips.
   Missing native VICE binaries or configured VICE data roots count as
   blockers, not passes.
8. A minimal upstream VICE testbench path exists for selected x64sc-compatible
   cases, including debugcart exit handling and process-level smoke validation.
9. `PERF-TUNING-001` is complete: the selected classic VICE comparison profile
   has a repeatable benchmark, the top measured vice-sharp hotspots have been
   profiled and remediated, and vice-sharp reaches at least 25% of classic VICE
   performance on that profile without breaking deterministic validation.
10. MCP TODO, session log, README dashboard, `handoff.md`, and this file all
   agree on what is done, what is deferred, and what remains post-Phase 1.

## Byrd Slice Rules

Every implementation slice follows the Byrd Development Process:

1. Open/update the MCP session turn and query the relevant TODOs.
2. Identify the exact canonical `FR-*`, `TR-*`, and `TEST-*` IDs, plus the
   VICE source files that define the behavior. Broad labels such as `FR-VIC`
   are audit findings, not sufficient slice anchors.
3. Correct requirement text, traceability maps, or test requirements first when
   the imported VICE requirement is incomplete, stale, or contradicted by VICE
   source behavior.
4. Pick one small slice with explicit exit criteria.
5. Add or update the failing validation first when practical.
6. Implement the minimum behavior needed for that slice.
7. Run focused tests, then run broader gates when shared behavior changed.
8. Update MCP TODO/session-log state and dashboard/docs before broadening scope.
9. Commit only a green, coherent slice; leave unrelated dirty files alone.

No slice is complete when it only changes code. It is complete when the code,
tests, dashboard, and MCP state all agree.

## Slice 0 - State Reconciliation

Goal: remove planning ambiguity before more implementation work.

Actions:

1. Query MCP TODO again and classify each open item as Phase 1 blocker,
   Phase 1 validation-only, or post-Phase 1.
2. Reconcile dashboard/TODO mismatches:
   - `RUNTIME-1541-002` is marked done in MCP but still has stale remaining
     text. Split or clear that stale remainder.
   - `BACKFILL-HOSTUI-001` is still open in MCP while the README dashboard
     marks Host UI + monitor control as complete. Close, split, or correct it.
   - `QA-XMLDOCS-001` is open in MCP while the README dashboard claims the
     XMLDOCS test contract is complete. Decide whether remaining retrofits are
     Phase 1 or post-Phase 1.
   - `BACKFILL-SID-001` remains open; decide whether 8580/filter deepening is
     post-MVP or required for final lockstep.
3. Refresh `README.md`, `handoff.md`, and MCP TODOs only after the current truth
   is known.

Current result from 2026-05-19:

- `QA-XMLDOCS-001` is validation-only/closeable: the ratchet is already
  `ExpectedMaxViolations = 0`, and `XmlDocsConventionTests` passed 1/1.
- `BACKFILL-SID-001` is validation-only/closeable for Phase 1: focused
  ROM-independent SID coverage passed 58/58; deeper analog 8580/filter work is
  post-MVP unless final lockstep finds a concrete regression.
- `BACKFILL-HOSTUI-001` is validation-only/closeable for the host-core scope:
  focused host/gRPC/Avalonia boundary coverage passed 115/115, and the
  in-process frame smoke skips only when the runtime falls back to
  the minimal machine without a video chip.
- `RUNTIME-1541-002` is done for the D64/1541 substrate. Full drive-CPU
  lockstep, KERNAL load-path validation, and GCR/fastloader depth remain under
  `ARCH-TRUEDRIVE-1541-002` and `BACKFILL-MEDIA-001`.
- `RUNTIME-CART-002` is validation-only/closeable for standard raw/CRT
  cartridge mapping. Advanced cartridge families remain post-MVP.
- `RUNTIME-CAPTURE-002` stays open only for configurable capture formats; BMP
  single/multi-frame and WAV recording are already implemented.
- ROM/native integration tests resolve VICE data from `VICESHARP_ROM_PATH`,
  `VICE_DATA_PATH`, `VICE_HOME`, or a local `x64sc.exe` install on `PATH`.
  The repo-local checkout is not expected to contain ROMs.

Gate:

- No Phase 1 blocker has contradictory README/MCP status.
- Every deferred open TODO has an explicit reason and target phase.
- `docs/plan.md` remains the current closeout plan.

## Slice 0.5 - VICE Requirements Traceability Gate

Goal: make imported VICE requirements drive design and code instead of serving
as after-the-fact labels.

Current audit result from 2026-05-20:

- Imported requirements exist, but enforcement is uneven. The current extractor
  found 144 canonical IDs in `docs/requirements`, 77 referenced from `src` or
  `tests`, 67 canonical IDs not referenced from source/test files, and 54
  noncanonical IDs in source/test files. For FRs only, the current split is
  108 canonical, 67 referenced, and 41 unreferenced.
- Active code and tests still contain broad or noncanonical labels such as
  `FR-VIC`, `FR-CIA-TIMER`, and `FR-INPUT-KEYBOARD`, which makes it unclear
  which imported requirement actually drove the implementation.
- Some requirement docs cite aspirational suite names while the real tests use
  different names, so traceability can appear complete without executable
  evidence.
- `FR-VIC-007` described opening borders but did not explicitly state the
  inverse behavior: closed vertical/side borders mask sprite pixels, and only
  an opened border exposes sprite pixels in the border area.
- `FR-VIC-010` described sprite DMA ordering as sprite 0 through sprite 7, but
  VICE PAL x64sc source schedules sprites 3-7 at one-based table cycles 1/2,
  3/4, 5/6, 7/8, and 9/10, then sprites 0-2 at 58/59, 60/61, and 62/63.
  VICE maps PAL cycle `c` to internal cycle `c - 1`, so vice-sharp
  `CurrentCycle` expects sprites 3-7 at 0/1, 2/3, 4/5, 6/7, and 8/9, then
  sprites 0-2 at 57/58, 59/60, and 61/62.
- `FR-VIC-010` also needed the VICE `sprite_dma` latch semantics surfaced:
  PAL public cycles 55/56 (vice-sharp 54/55) sample `$D015` and sprite Y once,
  then BA/data DMA use the latched mask. Clearing `$D015` after latch-on must
  not cancel the active DMA window, and enabling after both checks must wait for
  a later matching raster line.

Gate result from 2026-05-21:

- `FR-VIC-007` already states that closed vertical/side borders mask sprite
  output, opened borders expose sprite pixels, and closed border pixels are not
  treated as foreground character/bitmap pixels for sprite-background checks.
- `FR-VIC-010` already states that sprite DMA is table-driven per active
  VIC-II model, includes the PAL x64sc table-cycle normalization, and captures
  VICE `check_sprite_dma` / `sprite_dma` latch behavior at PAL public cycles
  55/56.
- `TEST-VIC-001` and `X64SC-Requirement-Coverage.md` already include closed
  border masking, opened-border sprite visibility, sprite priority, and
  per-model sprite DMA timing.
- After the 2026-05-21 Slice 0.5 cleanup, the touched VIC-II source/test slice
  no longer contains the broad `FR-VIC` label. The remaining noncanonical
  labels are debt outside that slice. New work must not add new broad or
  noncanonical IDs, and touched files must move toward canonical IDs.
- The 2026-05-21 classic VICE edge-case backfill scanned
  `native/vice/vice/src`, recorded the repeatable inventory under
  `docs/requirements/backfill/`, and promoted 19 observable compatibility
  behaviors to canonical `TR-*-EDGE-*` requirements with source references,
  FR/TEST mappings, and deferred/non-Phase-1 notes where appropriate.
  `tools/check_requirement_traceability.ps1` now reports 163 canonical IDs,
  81 referenced canonical IDs, 82 unreferenced canonical IDs, and 53
  noncanonical IDs in `src`/`tests`; the added edge TRs are requirements-only
  anchors for future implementation slices, so they intentionally increase the
  unreferenced count until those slices add executable coverage.

Required gate for each further Phase 1 implementation slice:

1. Name the canonical `FR-*`, `TR-*`, `TEST-*`, and owning `BACKFILL-*` IDs for
   the slice.
2. Cite the VICE documentation or source files that define the behavior.
3. If the canonical requirement is missing, stale, or wrong, amend the
   requirement before changing code.
4. Add or update executable tests that reference the canonical IDs.
5. Run `tools/check_requirement_traceability.ps1` and record its output in the
   handoff/session notes. New broad or noncanonical IDs in touched files are
   blockers unless they are explicitly documented as a cleanup finding.

Gate:

- `FR-VIC-007`, `FR-VIC-010`, `TEST-VIC-001`, and the x64sc coverage note are
  amended to reflect the VICE behavior already found during Slice 1.
- Active Slice 1 renderer/border code and tests reference canonical VIC-II IDs
  rather than broad `FR-VIC` labels.
- Further Slice 1 work starts from canonical requirements, VICE evidence, and
  focused validation rather than code-first discovery.

## Classic VICE Edge-Case Integration

The `TR-*-EDGE-*` requirements from the 2026-05-21 classic VICE source
backfill are now planning inputs, not a side inventory. A Phase 1 slice that
touches an affected subsystem must name the relevant edge TR before design,
add or update executable coverage for the observable behavior, and record any
deferment explicitly.

| Area | Edge TRs | Phase 1 Effect |
|------|----------|----------------|
| VIC-II visible parity | `TR-VIC-EDGE-001` through `TR-VIC-EDGE-006` | Required under `BACKFILL-VIDEO-001`. These drive invalid-mode priority/collision, border flip-flops, badline/idle windows, sprite DMA latch/fetch timing, matrix idle/fill behavior, and register/collision readback. |
| VIC-II VSP/AGSP | `TR-VIC-EDGE-007` | Deferred unless Phase 1 explicitly accepts VSP/AGSP demo compatibility. Do not spend Slice 1 time here unless a final x64sc gate finds a concrete VSP blocker. |
| CPU and shared bus | `TR-CPU-EDGE-001`, `TR-CPU-EDGE-002`, `TR-RAM-EDGE-001` | Required when CPU, VIC DMA, reset/startup RAM, or lockstep slices touch interrupt latency, BA-low stalls, dummy accesses, or RAM-init determinism. |
| Disk, IEC, and media images | `TR-IEC-EDGE-001`, `TR-DRV-EDGE-001`, `TR-MEDIA-EDGE-001` | Required under `ARCH-TRUEDRIVE-1541-002` and `BACKFILL-MEDIA-001` for real KERNAL load/autostart, IEC bit timing, directory/BAM behavior, and weak/sync media image behavior. |
| Tape and tapeport | `TR-TAP-EDGE-001`, `TR-TAPE-EDGE-001` | Required under `RUNTIME-TAPE-002` and `BACKFILL-MEDIA-001` for TAP pulse interpretation, motor/sense behavior, and CIA FLAG timing. |
| SID | `TR-SID-EDGE-001` through `TR-SID-EDGE-003` | Phase 1 validation-only unless final lockstep or audio/register gates expose a concrete SID parity failure. Keep deeper analog/filter work post-MVP. |
| VDC/C128 | `TR-VDC-EDGE-001` | Non-Phase-1 unless a C128 profile enters the required x64sc-equivalent scope. |

For x64sc parity, these edge cases change the implementation posture: the
remaining work is no longer broad "make it match VICE" parity. It is a set of
observable compatibility traps that must be tested directly. The highest-risk
Phase 1 impacts are VIC-II raster-visible behavior, CPU/VIC bus timing, and
media/tape protocol timing because small cycle or latch differences can pass
basic boot tests while still failing demos, fastloaders, native checkpoints, or
longer KERNAL workflows.

## Slice Order

| Order | Slice | Primary TODOs | Scope | Exit Gate |
|-------|-------|---------------|-------|-----------|
| 0 | Reconcile tracked state | All open Phase 1 TODOs | Resolve dashboard/TODO conflicts and split post-MVP work. | MCP TODO, README, handoff, and this plan agree. |
| 0.5 | VICE requirements traceability gate | All open Phase 1 TODOs | Audit imported VICE FR coverage, noncanonical labels, stale test-suite names, and missing acceptance criteria before more feature work. | Requirement text and test requirements are current for the next slice; traceability check output is recorded. |
| 1 | VIC-II visible parity | `BACKFILL-VIDEO-001` | Badline/DMA CPU inhibition, sprite DMA, sprite priority/collisions, border/open-border behavior, bank switching side effects, visible frame checkpoints, and `TR-VIC-EDGE-001` through `TR-VIC-EDGE-006`. | Focused VIC tests, x64sc raster/chip checkpoints, full solution test. |
| 2 | True-drive 1541/D64 close | `ARCH-TRUEDRIVE-1541-002`, `BACKFILL-MEDIA-001` | Compare drive CPU lockstep, validate real IEC/D64 load behavior through C64 KERNAL paths, cover `TR-IEC-EDGE-001`, `TR-DRV-EDGE-001`, and `TR-MEDIA-EDGE-001`, and document any native-shim exposure gaps. | 100k drive CPU lockstep or documented native-shim blocker, D64 attach/load smoke, full solution test. |
| 3 | Datasette MVP completion | `RUNTIME-TAPE-002`, `BACKFILL-MEDIA-001` | Datasette motor, spin-up/spin-down timing, play/record/rewind state, gated pulse delivery, CIA FLAG behavior, `TR-TAP-EDGE-001`, and `TR-TAPE-EDGE-001`. | TAP/datasette timing tests and host/launcher tape attach smoke. |
| 4 | Snapshot and capture close | `RUNTIME-SNAPSHOT-002`, `RUNTIME-CAPTURE-002`, `BACKFILL-MEDIA-001` | Snapshot all required chip/timing/bus state; add configured capture formats required by Phase 1. | Deterministic save/load/resume test, capture artifact tests, full solution test. |
| 5 | Input parity close | `BACKFILL-INPUT-001` | Joystick/control-port parity, longer keyboard workflows, model-specific keyboard matrix behavior, and host input injection coverage. | Input parity tests, host input protocol tests, full solution test. |
| 6 | Testbench and launcher shim | `ARCH-TESTBENCH-001`, `CLI-LAUNCHER-001` | Minimal x64sc-compatible runner, debugcart exits, `-limitcycles`, selected VICE-style flags, PRG/SYS autostart, screenshot/monitor smoke. | Selected upstream testbench cases run from process-level smoke tests. |
| 7 | Final x64sc lockstep | `BACKFILL-LOCKSTEP-001` | Run all required x64sc profile gates with media/input/video/snapshot coverage, no required skips, and explicit triage for every Phase-1-relevant edge TR. | Final x64sc lockstep green; blockers must identify missing asset, native shim gap, exact failing subsystem, or deferred edge TR. |
| 8 | Performance first pass | `PERF-TUNING-001` | Profile CPU, VIC/video, memory/bus, scheduler, host UI/frame-source, and allocation hotspots against a selected classic VICE/x64sc comparison profile; remediate the highest-impact bottlenecks. | Repeatable benchmark shows vice-sharp reaches at least 25% of classic VICE performance on the selected profile, with deterministic validation still green. |
| 9 | Phase 1 closeout | docs plus MCP TODO/session state | README dashboard refresh, handoff refresh, MCP TODO/session closure, and explicit post-Phase 1 split list. | Full closeout validation set passes and docs agree. |

## Slice 1 Progress - VIC-II Visible Parity

2026-05-19 landed the first Slice 1 implementation step:

- `VideoRenderer` now composes visible sprite pixels into the BGRA framebuffer
  instead of leaving sprite behavior only in collision latches.
- Lower-numbered sprites win when opaque sprite pixels overlap.
- `$D01B` sprite priority now keeps behind-background sprites behind
  foreground character pixels while still drawing them over background pixels.
- Focused gate passed: sprite renderer/collision/DMA set 39/39, broader
  video unit/service gate 122/122 with installed VICE data, XMLDOCS 1/1.

2026-05-20 continued Slice 1 with a VICE-source-driven display-mode pixel
slice:

- `VideoRenderer` now routes standard text, multicolor text, extended color,
  standard bitmap, multicolor bitmap, and invalid ECM combinations through the
  VICE `viciisc/vicii-draw-cycle.c` color table.
- Focused framebuffer tests cover `FR-VIC-002`, `FR-VIC-003`, `FR-VIC-008`,
  and `TEST-VIC-001` color routing, including invalid modes rendering black.
- `RomProvider` now resolves the legacy `characters` alias to the installed
  VICE `chargen-901225-01.bin` data file, so local VICE data roots work without
  repo-local ROMs.

2026-05-21 continued Slice 1 with a VICE-source-driven border timing slice:

- `FR-VIC-007` and the mirrored Project/wiki requirements now cite the PAL
  x64sc horizontal border clear/set cycles from VICE
  `viciisc/vicii-cycle.c` and the cycle-56 CSEL 1-to-0 right-side-border-open
  behavior from VICE `vicii/vicii-mem.c`.
- `Mos6569` snapshots whether a completed raster line skipped the right-border
  set check, and sprite visibility now allows side-border pixels on that line
  when the horizontal border flip-flop stayed open.
- `VicIIBorderFlipFlopTests` covers the cycle-56 CSEL 1-to-0 state transition,
  and `VideoRendererTests` now proves the opened right side border renders a
  sprite pixel in the BGRA framebuffer at x=340. Focused renderer/border tests
  passed 28/28; the broader VIC/video gate passed 155/155.

2026-05-21 continued Slice 1 with invalid ECM priority/collision backfill:

- `FR-VIC-002`, `FR-VIC-003`, and `FR-VIC-005` now explicitly capture the
  x64sc behavior from `viciisc/vicii-draw-cycle.c`: invalid ECM selector
  combinations render visible graphics as color 0/`COL_NONE`, but keep the
  hidden `px & 0x2` foreground/priority bit for `$D01B` sprite priority and
  `$D01F` sprite-background collision.
- `Mos6569` now exposes one display-mode-aware foreground/priority helper used
  by both collision processing and the framebuffer renderer, replacing the old
  simplified foreground approximation.
- Focused renderer/collision tests cover ECM+MCM hires, ECM+MCM multicolor,
  ECM+BMM, and ECM+BMM+MCM cases, including multicolor `%01` pairs remaining
  non-foreground while `%10` pairs block behind-background sprites and latch
  sprite-background collisions. Focused renderer/collision tests passed 37/37,
  the broader VIC/video gate passed 163/163, and the traceability audit
  completed with the existing repo-wide canonical/noncanonical backlog.

2026-05-21 continued Slice 1 with continuous side-border backfill:

- `TR-VIC-EDGE-002` now has managed coverage for cycle-56 CSEL 1-to-0
  right-side-border opening, carried left-side-border opening on the following
  line, repeated continuous side-border carry across multiple lines, and the
  cycle-17 CSEL 0-to-1 blank-line edge.
- `Mos6569` now snapshots per-line horizontal display state, left-border-open
  carry, and right-border-open state so render-time visibility follows the
  cycle-driven flip-flop state instead of static `$D016` geometry alone.
- `VideoRenderer` now suppresses border drawing for opened side borders and
  renders background fill for non-sprite opened-border pixels while still
  allowing sprites through the opened border.
- Focused border/renderer tests passed 52/52. The broader VIC/video gate
  passed 170/170. The traceability audit now reports 80 referenced canonical
  IDs, including `TR-VIC-EDGE-002`.

2026-05-21 continued Slice 1 with VIC-II register readback/collision latch
backfill:

- `TR-VIC-EDGE-006` now has managed coverage for VICE `vicii-mem.c`
  hardcoded readback behavior: `$D019` exposes fixed bits 6-4, `$D01A`
  exposes the fixed high nibble, unused `$D02F-$D03F` reads as `$FF`, and
  writes to `$D01E/$D01F` do not fabricate collision latch state.
- Existing raster IRQ tests now expect the fixed `$D019` readback bits while
  preserving write-one-to-clear and IRQ-line behavior.
- Focused register-readback/IRQ/collision tests passed 37/37. The broader VIC/video
  gate passed 174/174. The traceability audit now reports 81 referenced
  canonical IDs, including `TR-VIC-EDGE-006`.

Remaining Slice 1 work:

- Continue from the Slice 0.5 gate result above. The 2026-05-20 border/sprite
  masking work exposed a systemic traceability gap: the VICE behavior was
  discoverable from classic VICE source, but the canonical imported FR did not
  explicitly drive the design before implementation. The corrected rule is now
  to update or cite the canonical requirement before broadening a slice.
- `TR-VIC-EDGE-002`: remaining open-border/border flip-flop depth is native
  x64sc checkpoint validation and model-aware visible-frame validation beyond
  the current managed PAL/non-PAL cycle-state tests.
- `TR-VIC-EDGE-004`: remaining sprite DMA depth covers non-PAL per-model
  tables, sprite data-fetch side effects, and native lockstep checkpoints for
  multiplexing edge cases.
- `TR-VIC-EDGE-001`: native x64sc visible-frame/checkpoint validation remains
  for the display-mode pixel effects now covered by synthetic framebuffer
  tests, including invalid ECM priority/collision pixels.
- `TR-VIC-EDGE-003` and `FR-VIC-008`: FLI/AFLI timing depth covers forced
  badlines, the left-edge FLI bug, mid-line `$D018` effects, and AFLI bitmap
  behavior.
- `TR-VIC-EDGE-005`: matrix idle/fill behavior must be checked before visible
  parity is called complete, especially where native checkpoints expose
  observable output differences.
  - Background-agent inspection found the smallest useful slice: add focused
    `VicIIMatrixIdleFetchTests` coverage for badline/prefetch matrix `0xff`
    fill plus RAM-derived color nibbles, idle graphics fetch from `$3fff` or
    `$39ff` according to ECM/display mode, and non-idle graphics fetch using
    populated matrix bytes.
  - Defer the 6569 RAM-to-character-ROM address latch path and native shim
    expansion for matrix/idle fields until after the managed matrix idle/fill
    behavior has a focused green gate.
- `TR-VIC-EDGE-006`: native x64sc register checkpoints remain for the managed
  readback/collision latch behavior now covered by synthetic tests.
  Background-agent inspection recommends a native-backed
  `VicIIRegisterReadbackNativeTests` slice that compares managed reads and
  writes with `ViceNativeBridge.ReadMemory` / `WriteMemory` across
  `$D000-$D03F`, starting with non-collision readback/write-ignore cases and
  then covering collision read-clear timing.
- Native x64sc visible-frame/checkpoint validation against the installed VICE
  data root for the remaining border, sprite, display-mode, and FLI cases.

## Phase 1 Blockers

Treat these as current blockers after Slice 0 reconciliation:

- `BACKFILL-VIDEO-001`: final visible VIC-II parity.
- `BACKFILL-MEDIA-001`: umbrella media parity for disk, tape, cartridge,
  snapshot, and capture.
- `BACKFILL-INPUT-001`: keyboard and control-port parity beyond held-key gates.
- `BACKFILL-LOCKSTEP-001`: final all-profile x64sc validation gate.
- `RUNTIME-TAPE-002`: datasette motor and timing behavior.
- `RUNTIME-SNAPSHOT-002`: full chip/timing snapshot round-trip.
- `RUNTIME-CAPTURE-002`: configurable capture formats required for Phase 1.
- `ARCH-TESTBENCH-001`: upstream VICE testbench runner smoke.
- `ARCH-TRUEDRIVE-1541-002`: true drive-CPU lockstep, unless the native shim
  exposure is formally split as post-Phase 1.
- `CLI-LAUNCHER-001`: launcher-level flags and process smoke needed for
  drop-in x64sc-style workflows.
- `PERF-TUNING-001`: profile and remediate first-pass performance hotspots
  until vice-sharp reaches at least 25% of classic VICE performance on the
  selected benchmark profile.

## Slice 0 Validation-Only Closures

These items should not consume further Phase 1 implementation time unless a
new failing gate proves otherwise:

- `QA-XMLDOCS-001`: close with 0 ratchet violations.
- `BACKFILL-SID-001`: close for Phase 1; analog/8580 deepening is post-MVP.
- `BACKFILL-HOSTUI-001`: close for host-core control; launcher/UI shell work
  remains under `CLI-LAUNCHER-001` or platform host TODOs.
- `RUNTIME-1541-002`: keep done and clear stale remaining text; true-drive
  lockstep remains under `ARCH-TRUEDRIVE-1541-002`.
- `RUNTIME-CART-002`: close for standard 8K/16K raw/CRT mapping; broad mapper
  families stay post-MVP.

## Likely Post-Phase 1 Deferrals

Do not pull these into Phase 1 unless the user explicitly changes scope:

- `ARCH-ADHOCMACHINE-001`: Avalonia 12 ad-hoc machine helper app and richer
  chip catalog metadata.
- `PLATFORM-CROSS-001`: UWP Xbox, Avalonia mobile, and MacOS host code.
- `PERF-BENCHMARK-001`: native VICE performance measurement integration beyond
  the `PERF-TUNING-001` first-pass Phase 1 benchmark/profile target.
- Further analog SID/8580 filter deepening beyond the current Phase 1 gates.
- Broad cartridge mapper families beyond standard 8K/16K/CRT behavior needed
  for Phase 1 tests.
- Wiki publishing that requires operator authorization.

## Validation Set

Use focused tests inside each slice, then run this closeout set before declaring
Phase 1 complete:

```powershell
dotnet build .\ViceSharp.slnx --nologo
dotnet test .\ViceSharp.slnx --nologo
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj --nologo --filter "FullyQualifiedName~X64ScVariantLockstepTests"
dotnet publish .\src\ViceSharp.Console\ViceSharp.Console.csproj -c Release -r win-x64 --self-contained /p:PublishAot=true
git diff --check
```

Add native VICE/testbench commands to this list once `ARCH-TESTBENCH-001` lands.
Add the selected `PERF-TUNING-001` benchmark/profile command once the first-pass
performance profile is defined; Phase 1 closeout must include the measured
classic VICE baseline, vice-sharp result, and percentage.
If a command is skipped, the closeout note must state the exact reason.

## Done Statement

Phase 1 can be called complete when Slice 9 is closed and the final handoff says:

- The repository is on a known commit.
- The working tree state is intentional.
- Full validation results are listed with exact commands and counts.
- `PERF-TUNING-001` is done and the final handoff lists the selected benchmark,
  classic VICE baseline, vice-sharp result, and percentage.
- All Phase 1 TODOs are done or explicitly split to later phases.
- README, MCP TODO, session logs, `handoff.md`, and this plan agree.
