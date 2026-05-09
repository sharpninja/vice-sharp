# ViceSharp Handoff (2026-05-09)

## Iteration 01 Wrap-Up

### Current Baseline
- Test baseline is `28 passed, 8 skipped`
- C64 builder now requires a real ROM provider and validates ROM availability
- C64 builder now routes ROM loading through `C64RomLoader` and loads ROM banks via `C64MemoryMap` ROM-load mode during initialization
- VICE lockstep validation remains enabled but blocked by native boot/VIC state drift after shim hardening.
- Native shim has bounded-timeout checkpoint stepping and explicit stop handling to prevent indefinite hangs in lockstep/boot-path flows.
- Debug monitor command surface exists: `r/z/n/m/d/b/ub/bl/cycles/reset`

### Monitor Commands (r z n m d b ub bl cycles reset)
- r: Registers with NV-BDIZC flags
- z [n]: Step n instructions
- n: Step over JSR
- m [addr]: Memory dump
- d [addr [n]]: Disassemble
- b/ub/bl: Breakpoint management
- cycles: Cycle counter

### Next Steps
1. Verify BASIC `READY.` boot path end-to-end
2. Re-enable VICE-backed CPU/VIC/CIA validation instead of skipping it
3. Fix remaining cycle-zero VIC row indexing and CPU status alignment so `First100CyclesMatch` and full lockstep can complete.
4. Finish remaining Iteration 1 features still missing from runtime scope: 1541, datasette, cartridges, snapshots, capture/export

### Current Turn (2026-05-09)
- Completed final shim and VIC-row consistency fixes for the requested path:
  - `native/vice-shim.c`: recover stale worker state and recover from checkpoint waits with bounded timeout/stop signals to prevent lockstep hangs.
  - `src/ViceSharp.Chips/VicIi/Mos6569.cs`: `GetPixelColor` row wrapping now uses modulo row mapping.
  - `src/ViceSharp.Chips/VicIi/VideoRenderer.cs`: `RenderScanline` row wrapping now uses modulo row mapping.
- Validation status at wrap-up:
  - Native shim build succeeds.
  - Focused lockstep/boot-path runtime runs are still failing (hang/mismatch remains in cycle-0/first-100 path); no new successful `First100CyclesMatch` execution during this wrap-up turn.
- Good handoff/session continuity path is active through `workflow.sessionlog` and `docs/requirements/requirements-wiki-documents.zip` was regenerated.
- Wrap-up completion:
  - Commit: `6ad07d9`
  - Push: `origin main` (`846a97e` → `6ad07d9`)
  - `git diff --check -- native/vice-shim.c` is clean (EOF normalized to LF and no trailing whitespace).
  - `docs/requirements/requirements-wiki-documents.zip` is committed as part of wrap-up.
  - Remaining workspace noise intentionally excluded from commit: pre-existing tracked changes in `docs/plan.md`, `native/build-vice-shim.sh`, `src/ViceSharp.Chips/Cpu/Mos6502.cs`, `src/ViceSharp.Core/*`, `tests/ViceSharp.TestHarness/*`, plus many `tmp-*` and artifact untracked files from prior runtime attempts.
