# ViceSharp Handoff (2026-05-12)

## Current Baseline

- HEAD before this runtime slice: `f2dbb29` (`docs: refresh vice-sharp handoff baseline`) on `main`.
- Workspace was clean before implementing the runtime TODO set.
- Validation rerun in this turn: `dotnet test .\ViceSharp.slnx --nologo` passes `62/62`.
- C64 ROM wiring and BASIC boot proof are green; `BasicBootProofTests.C64_Boot_Reaches_Ready_Prompt` remains part of the harness.
- VICE-backed lockstep validation is green through `LockstepValidationTests.First100000CyclesMatch`.
- `ARCH-LOCKSTEP-001` and `ARCH-ROM-001` are done in MCP TODO with validation evidence.
- Bounded runtime feature gaps are implemented and covered by focused tests.

## Runtime TODO Results

Implemented MCP TODOs as of 2026-05-12:

1. `RUNTIME-1541-001` - `IecD64Attachment` validates a 35-track D64 image and reads deterministic sectors through the IEC drive buffer.
2. `RUNTIME-TAPE-001` - `TapImage`, `TapPulseReader`, and `Datasette` validate TAP attach and gated pulse reads.
3. `RUNTIME-CART-001` - `StandardCartridgeImage` validates raw 8K/16K ROML/ROMH mapping and read-only behavior.
4. `RUNTIME-SNAPSHOT-001` - `RuntimeSnapshotStore` captures, saves, loads, and restores a deterministic 64K runtime snapshot with public CPU state.
5. `RUNTIME-CAPTURE-001` - `FrameCapture`, `RecordingFrameSink`, and `BmpFrameArtifactWriter` write deterministic BMP frame artifacts from BGRA buffers.

## Recommended Next Slice

The next runtime slice should deepen one implemented gate instead of widening all of them at once. Recommended order:

1. Extend `RUNTIME-1541-001` into real drive command or IEC protocol behavior.
2. Add TAP motor/spin-up timing against the datasette gate.
3. Integrate standard cartridge mapping into the C64 memory map.
4. Expand snapshots beyond public CPU state and 64K bus-visible memory.
5. Add PNG or configurable capture formats after the BMP artifact gate.

## Validation Commands

Use these as the minimum regression gates for the next code slice:

```powershell
dotnet test .\ViceSharp.slnx --nologo
```

When changing runtime or lockstep behavior, also run focused tests around the touched area and preserve the 100k lockstep gate.

## Notes

- `docs/plan.md` is the consolidated plan artifact.
- Do not directly edit `docs/todo.yaml`; use the MCP TODO path through `mcpserver-codex-plugin`.
- Full subsystem parity for 1541, datasette, cartridges, snapshots, and capture/export is still broader future work; this slice closes the bounded MCP TODO gates only.
