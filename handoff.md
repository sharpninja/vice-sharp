# ViceSharp Handoff (2026-05-20)

## Current Baseline

- Workspace: `F:\GitHub\vice-sharp`
- Branch: `main`
- Current HEAD: `c9871f4` (`docs(dashboard): 84-slice session close + HOSTUI 100% + core primitives 100%`)
- Scope of this handoff: Phase 1 Slice 0 reconciliation, Slice 0.5 requirements traceability gate, Slice 1 visible-sprite progress, VICE display-mode pixel routing, VICE data-root resolver correction, and native x64sc data-root unblocking. This is not a final Phase 1 completion claim.
- `docs/plan.md` has replaced the stale May 12 30-stage snapshot in full and is the active Phase 1 closeout plan.

## Slice 0 Result

The plan, README dashboard, user docs, and MCP TODO state now distinguish stale open TODOs from real Phase 1 blockers.

Closed or closeable for Phase 1:

- `QA-XMLDOCS-001`: zero-violation XMLDOCS ratchet is active.
- `BACKFILL-SID-001`: focused Phase 1 SID surface is covered; analog 8580/filter deepening is post-MVP unless final lockstep finds a concrete regression.
- `BACKFILL-HOSTUI-001`: host-core gRPC, monitor/control, generated clients, view models, registry, frame source, and InProcessGrpcHost are covered. Launcher/UI shell work remains separate.
- `RUNTIME-1541-002`: D64/1541 substrate is done; true-drive CPU lockstep and ROM-backed KERNAL load validation remain under `ARCH-TRUEDRIVE-1541-002` and `BACKFILL-MEDIA-001`.
- `RUNTIME-CART-002`: standard 8K/16K raw/CRT cartridge live mapping is done; broad mapper families are post-MVP.

Still real Phase 1 blockers:

- `BACKFILL-VIDEO-001`
- `BACKFILL-MEDIA-001`
- `BACKFILL-INPUT-001`
- `BACKFILL-LOCKSTEP-001`
- `RUNTIME-TAPE-002`
- `RUNTIME-SNAPSHOT-002`
- `RUNTIME-CAPTURE-002`
- `ARCH-TESTBENCH-001`
- `ARCH-TRUEDRIVE-1541-002`
- `CLI-LAUNCHER-001`

## MCP TODO Updates

Submitted through `mcpserver-codex-plugin` in this session:

- Marked `QA-XMLDOCS-001` done.
- Marked `BACKFILL-SID-001` done.
- Marked `BACKFILL-HOSTUI-001` done.
- Kept `RUNTIME-1541-002` done and cleared stale remaining text.
- Marked `RUNTIME-CART-002` done.
- Kept `RUNTIME-CAPTURE-002` open only for configurable formats/options.
- Kept `RUNTIME-TAPE-002` open for datasette motor/spin-up/record and launcher attach smoke.
- Kept `RUNTIME-SNAPSHOT-002` open for full chip/timing/bus snapshot and deterministic resume.

## Slice 0.5 Requirements Traceability Gate

The latest Slice 1 work exposed a project-wide deficiency: imported VICE
requirements exist, but they were not consistently forcing design, code, and
test decisions before implementation.

Corrective docs/code changes made in this turn:

- Added `docs/requirements/traceability/Requirements-Implementation-Audit-2026-05-20.md`.
- Added `tools/check_requirement_traceability.ps1`.
- Updated `docs/plan.md` with mandatory Slice 0.5 gating before further Phase 1 feature work.
- Updated `FR-VIC-007` to state closed vertical/side borders mask sprite output and opened borders expose sprite pixels according to border flip-flop state.
- Updated `FR-VIC-010` to remove the incorrect sprite 0-to-7 timing assumption and require model-specific VICE tables. PAL x64sc source schedules sprites 3-7 at one-based VICE table cycles 1/2, 3/4, 5/6, 7/8, 9/10 and sprites 0-2 at 58/59, 60/61, 62/63; VICE maps those to zero-based internal cycles, so vice-sharp `CurrentCycle` expects sprites 3-7 at 0/1, 2/3, 4/5, 6/7, 8/9 and sprites 0-2 at 57/58, 59/60, 61/62.
- Updated `FR-VIC-010` again to surface the VICE `sprite_dma` latch: PAL public cycles 55/56 (`CurrentCycle` 54/55) sample `$D015` and sprite Y, and later BA/data DMA uses the latched mask until sprite MC base completion. Clearing `$D015` after latch-on no longer cancels the active BA window, while enabling after both checks waits for a later matching raster line.
- Updated `TEST-VIC-001` and x64sc coverage notes for closed-border sprite masking, opened-border sprite visibility, sprite priority, and per-model sprite DMA timing.
- Updated active border/render comments in `Mos6569.cs`, `VideoRenderer.cs`, `VideoRendererTests.cs`, and `VicIIBorderFlipFlopTests.cs` to cite canonical `FR-VIC-*`, `TR-CYCLE-001`, `TEST-VIC-001`, and `BACKFILL-VIDEO-001`.
- Continued `BACKFILL-VIDEO-001` with read-only subagents after they processed `AGENTS-README-FIRST.yaml`: Sagan verified the VICE sprite timing table and Faraday inspected sprite DMA tests. The subagent evidence caught the imported `FR-VIC-010` `62/next-line 0` error before closure.
- Replaced the broad PAL sprite-DMA stall helper in `Mos6569.cs` with PAL x64sc table-driven per-sprite BA masks, and updated `VicIISpriteDmaStallTests.cs` / `VicIISpriteDmaTests.cs` comments to canonical IDs.
- Continued the latch semantics slice with read-only subagents after they processed `AGENTS-README-FIRST.yaml`: Kepler verified the VICE `check_sprite_dma`/`sprite_dma` source behavior and Noether identified the live `$D015`/Y re-evaluation gap in `Mos6569.cs`. No MCP endpoints or repo ROM lookups were used by the subagents.
- Added a PAL x64sc `sprite_dma` active mask in `Mos6569.cs` and focused AC4/AC5 tests in `VicIISpriteDmaStallTests.cs` for disable-before-check, disable-after-latch, re-enable-before-check, and re-enable-after-check behavior.
- Continued the display-mode pixel slice with read-only subagents after they processed `AGENTS-README-FIRST.yaml`: Gibbs verified the VICE `viciisc/vicii-draw-cycle.c` color table for standard text, multicolor text, standard bitmap, multicolor bitmap, ECM, and invalid modes; Poincare inspected the local renderer/test gaps. Both were instructed to report progress to the main agent at least every five minutes.
- Updated `VideoRenderer` to route `FR-VIC-002`, `FR-VIC-003`, and `FR-VIC-008` visible pixels through the VICE display-mode color table, including invalid ECM combinations rendering as black.
- Added focused `VideoRendererTests` coverage for multicolor text, ECM background selection, standard bitmap, multicolor bitmap, and all invalid ECM selector combinations.
- Updated `RomProvider` so the legacy `characters` request resolves the installed VICE `chargen-901225-01.bin` file; repo-local ROMs are still not expected.
- Updated `VideoSurfaceIntegrationTests.Rendered_Frame_Image_Is_Blue_Border_Frame` to configure VIC colors deterministically before asserting multi-color frame output.

Traceability check:

```powershell
.\tools\check_requirement_traceability.ps1
```

Result after the display-mode pixel slice: 144 canonical IDs, 77 referenced from
`src/tests`, 67 not referenced from source/test files, 54 noncanonical IDs
still present. FR-only breakdown: 108 canonical, 67 referenced, 41
unreferenced. This is diagnostic debt for the
next cleanup slices; use `-FailOnNonCanonical` only after existing broad labels
are triaged.

MCP fallback:

- Direct `workflow.sessionlog.beginTurn` hit `MCP_UNTRUSTED: Health check failed`.
- Per the fallback strategy, the traceability turn was written to local plugin
  failsafe storage for later replay:
  `.mcpServer/failsafe/codex/20260520T012157Z-80811-11182.json`.
- The follow-up `FR-VIC-010` sprite-DMA traceability turn was also written to
  local plugin failsafe storage:
  `.mcpServer/failsafe/codex/20260520T015900Z-fr-vic010.json`.
- The `FR-VIC-010` `sprite_dma` latch semantics turn was written to local
  plugin failsafe storage:
  `.mcpServer/failsafe/codex/20260520T023300Z-fr-vic010-latch.json`.
- The failsafe payload was verified by decoding `paramsYamlBase64` and by
  normalizing it through `mcpserver-codex-plugin/lib/pending-import-to-yaml.js`.

## Validation Run In This Slice

Focused ROM-independent reconciliation gate:

```powershell
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj --nologo --no-restore --filter "FullyQualifiedName~XmlDocsConventionTests|FullyQualifiedName~Sid6581NonLinearCutoffTests|FullyQualifiedName~Sid8580NoiseLfsrTests|FullyQualifiedName~SidAdsrBugTests|FullyQualifiedName~SidAudioBackendTests|FullyQualifiedName~SidCombinedWaveform8580Tests|FullyQualifiedName~SidCombinedWaveformTests|FullyQualifiedName~SidDeterminismTests|FullyQualifiedName~SidDigiPlaybackTests|FullyQualifiedName~SidDualSidTests|FullyQualifiedName~SidFilter6581Tests|FullyQualifiedName~SidHardSyncTests|FullyQualifiedName~SidOscillatorTests|FullyQualifiedName~SidRegisterReadbackTests|FullyQualifiedName~SidRingModTests|FullyQualifiedName~MonitorServiceHost|FullyQualifiedName~AttachPanelViewModelTests|FullyQualifiedName~AvaloniaBoundaryTests|FullyQualifiedName~EmulatorRuntimeRegistryTests|FullyQualifiedName~GrpcHostServiceAdaptersTests|FullyQualifiedName~GrpcHostProtocolClientTests|FullyQualifiedName~InputServiceHostTests|FullyQualifiedName~InProcessGrpcHostTests|FullyQualifiedName~StandardCartridgeTests"
```

Result: passed 192, skipped 1, failed 0.

The skipped test was from the pre-resolver environment. After VICE data-root discovery was fixed, the local ROM/video focused gates below ran with zero skips.

Earlier focused evidence used for TODO classification:

- XMLDOCS: 1/1 passed.
- SID ROM-independent focused set: 58/58 passed.
- Host/gRPC focused set: 115/115 passed.
- Standard cartridge focused set: 6/6 passed.

Known validation dependency:

- ROM-dependent integration must use a configured VICE data root or the local `x64sc.exe` install. The repo-local checkout is not expected to contain ROMs, and missing configured VICE data must not be treated as a pass for final Phase 1 lockstep.
- Current local WinVICE install resolved from `x64sc.exe`: `C:\Users\kingd\.choco\lib\winvice-nightly\tools\GTK3VICE-3.8-win64`. Its data folders are directly under that root (`C64`, `DRIVES`, etc.).
- Native `vice_x64.dll` is currently a checked-in build whose internal VICE data lookup expects `module_directory\vice\vice\data`. Managed loading now relocates the native DLLs into `%TEMP%\ViceSharpNative\<hash>` and symlinks that expected data path to the installed WinVICE data root, so native x64sc validation does not require repo-local ROMs.

## Files Changed In This Slice

- `README.md`
- `docs/plan.md`
- `docs/ROMs.md`
- `docs/USER-GUIDE.md`
- `docs/VICE-MIGRATION.md`
- `handoff.md`
- `docs/requirements/backfill/X64SC-Requirement-Coverage.md`
- `docs/requirements/functional/FR-Video-VIC-II.md`
- `docs/requirements/test/TEST-Requirements.md`
- `docs/requirements/traceability/Requirements-Implementation-Audit-2026-05-20.md`
- `tools/check_requirement_traceability.ps1`
- `src/ViceSharp.Chips/VicIi/Mos6569.cs`
- `src/ViceSharp.Chips/VicIi/VideoRenderer.cs`
- `src/ViceSharp.Console/Program.cs`
- `src/ViceSharp.Core/ViceNative.cs`
- `src/ViceSharp.Host/Runtime/DefaultEmulatorRuntimeFactory.cs`
- `src/ViceSharp.RomFetch/RomProvider.cs`
- `src/ViceSharp.RomFetch/ViceDataPathResolver.cs`
- `src/ViceSharp.Architectures/C1541/C1541ViceRomNames.cs`
- `tests/ViceSharp.TestHarness/AvaloniaBoundaryTests.cs`
- `tests/ViceSharp.TestHarness/C64VkmKeyboardTests.cs`
- `tests/ViceSharp.TestHarness/LocalVideoFrameSourceTests.cs`
- `tests/ViceSharp.TestHarness/MachineTestFactory.cs`
- `tests/ViceSharp.TestHarness/VicIIRasterIrqTests.cs`
- `tests/ViceSharp.TestHarness/VicIISpriteDmaStallTests.cs`
- `tests/ViceSharp.TestHarness/VicIISpriteDmaTests.cs`
- `tests/ViceSharp.TestHarness/VicIIBorderFlipFlopTests.cs`
- `tests/ViceSharp.TestHarness/VideoRendererTests.cs`
- `tests/ViceSharp.TestHarness/VideoSurfaceIntegrationTests.cs`
- `tests/ViceSharp.TestHarness/ViceDataPathResolverTests.cs`
- `tests/ViceSharp.TestHarness/X64ScVariantLockstepTests.cs`
- `tests/ViceSharp.TestHarness/XmlDocsConventionTests.cs`

## Recommended Next Slice

Continue `BACKFILL-VIDEO-001` only under the Slice 0.5 traceability gate. The first visible-frame implementation step has landed: `VideoRenderer` now composes visible sprite pixels into the framebuffer, lower-numbered sprites win overlap priority, and `$D01B` keeps behind-background sprites behind foreground character pixels while still drawing over background pixels. The follow-up border/sprite masking work is requirement-backed by `FR-VIC-004`, `FR-VIC-007`, `TR-CYCLE-001`, and `TEST-VIC-001`.

Latest focused validation:

```powershell
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj --nologo --no-restore --filter "FullyQualifiedName~VideoRendererTests|FullyQualifiedName~SpriteCollisionTests|FullyQualifiedName~SpriteYExpansionMulticolorTests|FullyQualifiedName~VicIISpriteCollisionIrqTests|FullyQualifiedName~VicIISpriteDmaTests|FullyQualifiedName~VicIISpriteDmaStallTests"
```

Result: passed 39, failed 0.

Latest `FR-VIC-010` sprite-DMA timing gate:

```powershell
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj --nologo --no-restore --filter "FullyQualifiedName~VicIISpriteDmaStallTests|FullyQualifiedName~VicIISpriteDmaTests"
```

Result after the `sprite_dma` latch slice: passed 15, skipped 0, failed 0.

Broader related video gate after the `FR-VIC-010` correction:

```powershell
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj --nologo --no-restore --filter "FullyQualifiedName~VicIISpriteDmaStallTests|FullyQualifiedName~VicIISpriteDmaTests|FullyQualifiedName~VicIIBadLineTests|FullyQualifiedName~VicIIBorderFlipFlopTests|FullyQualifiedName~VideoRendererTests"
```

Result after the `sprite_dma` latch slice: passed 39, skipped 0, failed 0.

Broader video unit/service gate after the display-mode color-table slice:

```powershell
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj --nologo --no-restore --filter "FullyQualifiedName~VicIIColorRegistersTests|FullyQualifiedName~VicIIBadLineTests|FullyQualifiedName~VicIIDisplayModeTests|FullyQualifiedName~VicIID011ControlTests|FullyQualifiedName~VicIiCoreTimingTests|FullyQualifiedName~VicIIMemoryPointerTests|FullyQualifiedName~VicIILightPenTests|FullyQualifiedName~VicIIRasterIrqTests|FullyQualifiedName~VicIISpriteCollisionIrqTests|FullyQualifiedName~VicIISpriteDmaTests|FullyQualifiedName~VicIISpriteDmaStallTests|FullyQualifiedName~VicIISpriteRegisterTests|FullyQualifiedName~SpriteCollisionTests|FullyQualifiedName~SpriteYExpansionMulticolorTests|FullyQualifiedName~VideoRendererTests|FullyQualifiedName~VideoSurfaceTests|FullyQualifiedName~VideoServiceHostTests|FullyQualifiedName~LocalVideoFrameSourceTests|FullyQualifiedName~VideoSurfaceIntegrationTests"
```

Result: passed 154, skipped 0, failed 0.

Display-mode pixel routing gate:

```powershell
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj --nologo --no-restore --filter "FullyQualifiedName~VideoRendererTests"
```

Result after the display-mode color-table slice: passed 23, skipped 0, failed 0.

ROM alias and deterministic surface smoke:

```powershell
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj --nologo --no-restore --filter "FullyQualifiedName~BootReadyPrompt_FrameBufferMatchesCharacterRomGlyphs"
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj --nologo --no-restore --filter "FullyQualifiedName~Rendered_Frame_Image_Is_Blue_Border_Frame"
```

Result: passed 1/1 for each command. The earlier `characters` lookup failure now resolves to the local VICE `chargen-901225-01.bin` file.

Combined managed video + native x64sc register/reset gate:

```powershell
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj --nologo --no-restore --filter "FullyQualifiedName~VicIIColorRegistersTests|FullyQualifiedName~VicIIBadLineTests|FullyQualifiedName~VicIIDisplayModeTests|FullyQualifiedName~VicIID011ControlTests|FullyQualifiedName~VicIiCoreTimingTests|FullyQualifiedName~VicIIMemoryPointerTests|FullyQualifiedName~VicIILightPenTests|FullyQualifiedName~VicIIRasterIrqTests|FullyQualifiedName~VicIISpriteCollisionIrqTests|FullyQualifiedName~VicIISpriteDmaTests|FullyQualifiedName~VicIISpriteDmaStallTests|FullyQualifiedName~VicIISpriteRegisterTests|FullyQualifiedName~SpriteCollisionTests|FullyQualifiedName~SpriteYExpansionMulticolorTests|FullyQualifiedName~VideoRendererTests|FullyQualifiedName~VideoSurfaceTests|FullyQualifiedName~VideoServiceHostTests|FullyQualifiedName~LocalVideoFrameSourceTests|FullyQualifiedName~X64ScVariantLockstepTests.ResetStateMatches_ForEveryRequiredX64ScVariant|FullyQualifiedName~X64ScVariantLockstepTests.ViciiRegisterCheckpointsMatchNative_AfterFirstProfileScanline_ForEveryRequiredX64ScVariant|FullyQualifiedName~X64ScVariantLockstepTests.ChipRegisterCheckpointsMatchNative_AfterFirstProfileScanline_ForEveryRequiredX64ScVariant"
```

Result: passed 165, skipped 0, failed 0.

Resolver/ROM/VKM smoke:

```powershell
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj --nologo --no-restore --filter "FullyQualifiedName~ViceDataPathResolverTests|FullyQualifiedName~C1541RomResolutionTests|FullyQualifiedName~C64MachineProfileTests.RomSet|FullyQualifiedName~C64VkmKeyboardTests.Load_Gtk3PosVkm_ResolvesRowsColumnsAndShiftFlags|FullyQualifiedName~LocalVideoFrameSourceTests"
```

Result: passed 19, skipped 0, failed 0.

Console auto-discovery smoke:

```powershell
dotnet run --project .\src\ViceSharp.Console\ViceSharp.Console.csproj --no-restore -- --cycles 1
```

Result: exited 0; built `Commodore 64 PAL`, initial PC `$FCE2`, final PC `$FCE2`, total cycles 1.

XMLDOCS gate:

```powershell
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj --nologo --no-restore --filter "FullyQualifiedName~XmlDocsConventionTests"
```

Result: passed 1, failed 0.

Diff whitespace gate:

```powershell
git diff --check
```

Result: passed.

Next recommended video sub-slice: start a narrow `FR-VIC-008` forced-badline/FLI sentinel, or continue x64sc visible-frame checkpoints for the newly implemented display-mode pixel routing using the installed VICE data root discovered from `x64sc.exe`. Avoid broadening `VideoRenderer` again until the current color-table slice is committed and its native visible-frame comparison target is selected.

For sprite DMA, start from `FR-VIC-010` and VICE source files
`native/vice/vice/src/viciisc/vicii-chip-model.c` and
`native/vice/vice/src/viciisc/vicii-cycle.c`; normalize VICE one-based PAL
cycles through `VICII_PAL_CYCLE(c) == c - 1`, and account for `sprite_dma`
latching at PAL public cycles 55/56 before writing vice-sharp `CurrentCycle`
expectations. Do not carry forward the old coarse "sprites 0..3 at 55..62,
sprites 4..7 at 0..7" comment as a requirement.

Keep using MCP TODO/session-log updates between slices. Do not directly edit `docs/todo.yaml`.
