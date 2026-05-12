# ViceSharp Handoff (2026-05-12)

## Current Baseline

- HEAD: `29d6dd6` (`test: extend c64 lockstep replay`) on `main`.
- Workspace was clean before this continuity-doc refresh.
- Validation rerun in this turn: `dotnet test .\ViceSharp.slnx --nologo` passes `49/49`.
- C64 ROM wiring and BASIC boot proof are green; `BasicBootProofTests.C64_Boot_Reaches_Ready_Prompt` remains part of the harness.
- VICE-backed lockstep validation is green through `LockstepValidationTests.First100000CyclesMatch`.
- `ARCH-LOCKSTEP-001` and `ARCH-ROM-001` are done in MCP TODO with validation evidence.
- Runtime feature gaps are now tracked as bounded MCP TODOs instead of stale prose-only follow-up items.

## Runtime Gap TODOs

Open MCP TODOs as of 2026-05-12:

1. `RUNTIME-1541-001` - bounded 1541 drive emulation validation slice.
2. `RUNTIME-TAPE-001` - bounded datasette runtime validation slice.
3. `RUNTIME-CART-001` - bounded cartridge mapping validation slice.
4. `RUNTIME-SNAPSHOT-001` - bounded snapshot save/load validation slice.
5. `RUNTIME-CAPTURE-001` - bounded capture/export validation slice.

## Recommended Next Slice

Start with `RUNTIME-1541-001` unless the user redirects. Keep it as a validation-first slice:

1. Identify the smallest existing 1541/IEC surface that can be validated without broad disk-drive implementation.
2. Add a focused test that captures the current missing behavior or the first deterministic attach/read gate.
3. Implement only enough code to pass that gate.
4. Preserve the current boot and lockstep regression gates, especially `First100000CyclesMatch`.
5. Update MCP TODO/session state before broadening into datasette, cartridge, snapshot, or capture work.

## Validation Commands

Use these as the minimum regression gates for the next code slice:

```powershell
dotnet test .\ViceSharp.slnx --nologo
```

When changing runtime or lockstep behavior, also run focused tests around the touched area and preserve the 100k lockstep gate.

## Notes

- `docs/plan.md` is the consolidated plan artifact.
- Do not directly edit `docs/todo.yaml`; use the MCP TODO path through `mcpserver-codex-plugin`.
- Do not mark Iteration 1 fully complete until the open runtime gap TODOs are either implemented or explicitly moved out of the iteration scope.
