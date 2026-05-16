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

## Items to add via MCP Todo

- Priority High: XMLDOCS
    - Required for all unit and integration tests
        - Must state the FR/TR being tested
        - Must describe the use case being tested
        - Must describe the acceptance criteria of the test
- Priority High: Completion Dashboard
    - Section in root README.md
    - Tracks complete feature set of VICE to ViceSharp
        - Per Feature State
        - Per Feature Completion %
        - Per Feature Interation
- Priority High: Benchmarking
    - Add Benchmark.net based harness to compare performance of VICE and ViceSharp performing predefined sets of work
    - Individually test CPU, Video, Audio, IEC, Cartridge, Keyboard
    - Test complete system performance over scripted activities
- Priority High: Repository Maintenance
    - Reconcile Chips and remove old, unused definitions
    - Ensure the requirements imported from VICE documentation are properly imported to the MCP Server
    - Populate the github wiki
- Priority High: Ad-Hoc Machine Definition
    - Abstract machine architecture to a YAML document (create schema)
    - Add support to the Console project to accept an ad-hoc machine architecture via command line which will assemble and validate the architecture, then start the machine.
    - Create a helper app (Avalonia 12) to generate a machine architecture using the available chips and other system blocks.
- Priority Medium: Cross Platform
    - Create wireframes for UI for each platform
    - Create UWP Host for Xbox One / Xbox Series
    - Create Avalonia 12 Mobile Host
    - Add MacOS Support
