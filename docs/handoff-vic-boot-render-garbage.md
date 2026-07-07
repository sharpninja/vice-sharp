# Handoff: VIC-II boot-screen display-start garbage (per-cycle renderer)

Status: DIAGNOSED, NOT FIXED. Surfaced 2026-07-06 when the Avalonia desktop MSI was
redeployed (build at commit 7d3fe2c). NOT caused by the S9 SID work - commit 7d3fe2c
touched zero VIC files. This is a pre-existing bug in the V-slice per-cycle renderer.

## Symptom
On the C64 boot screen (and every display line), the first ~3 display columns render a
15/0 checkerboard ("garbage") instead of the character content: "64K" shows as "K",
"READY." as "DY.", the leading "****" of the banner is eaten. Border colour and the
rest of each line are correct. Reported live speed was ~43% (FPS 19.7) - possibly a
separate concern.

## Definitive diagnosis (managed frame vs VICE oracle, both 384x272)
- VICE (native `vice_vic_capture_frame_indices`): left border = 32px (RasterX 12-15),
  then clean background/text from the first column. No garbage.
- Managed (`Mos6569.FrameBuffer`, live path): left border = 48px (RasterX 12-17, 2
  cycles too wide) THEN a 24px garbage checkerboard (RasterX 18-20) THEN the display.
So two coupled defects at the display start:
  1. Left border is ~2 cycles too wide.
  2. Garbage display data at the border->display transition; the over-wide border
     partially masks it.
The garbage IS in the core `PixelSequencer.LineIndices` (the live VideoRenderer only
reads `LineIndices[96 + x]`), so this is a core per-cycle-render bug, not a
live-render-only bug. The oracle INDEX-parity tests pass because they do not exercise
the display-start columns of a booted frame precisely.

## What was ruled out
- Palette: index 14 ("Light Blue") is RGB(6B,5E,D1) - a periwinkle; the "purple"
  border is actually correct.
- `visEn` window: shifting `Mos6569` psVisEn from `RasterX>=14 && <54` to `>=15 && <55`
  did NOT fix it (tested + reverted). VICE ties vis_en to the FetchC cycle flag
  (vicii-chip-model.c:762-764); managed cycle 14 == VICE cycle 15 (FetchC), so
  `visEn=14` is correct. Do not re-try this.

## Suspect chain (needs cycle-by-cycle confirmation)
Border flip-flop timing (`Mos6569.LeftBorderCheckCycle => Csel?16:17`,
`UpdateBorderFlipFlopsForCurrentCycle`) + the 1-cycle `BorderState` lag in
`PixelSequencer.DrawBorder8` + the 6569 colour-ring 1-cycle delay in `DrawColors8`
+ the `DbufOffset` -> `FirstVisibleRasterX*8=96` mapping + the display-start gbuf
(g-access `ReadVicGfxDataOrIdle` at C64MemoryMap PAL cycle>=15). Per-cycle capture
(line ~90) showed gbuf 0xF0/0x0F at RasterX 15-17 where VICE renders spaces.

## Reproduction (anchor for the fix)
1. `var m = MachineTestFactory.CreateC64Machine(); for 400 frames m.RunFrame();`
2. `var vic = (Mos6569)m.Devices.GetByRole(DeviceRole.VideoChip);` read `vic.FrameBuffer`
   (BGRA, 384x272). Reverse-map BGRA->palette index via `VicPalette.Colors`.
3. Dump palette indices for the left ~96px of display rows (frame Y ~50-65).
4. Native oracle: `ViceNativeBridge.CreateMachine("c64")`, `StepCycle` ~3,000,000x,
   `TryCaptureVicFrameIndices(...)`. Native is clean; managed shows the checkerboard.

## Recommended fix approach
Add a per-cycle export to the shim of VICE's `dbuf`/`gbuf`/`vbuf`/`cbuf`/`dmli`/border
state for one display line, diff managed `LineIndices`/pipeline against it cycle by
cycle, and correct the exact divergent cycle. Guardrail: the full VIC index-parity
suite must stay green. Do NOT guess-fix the timing chain (one guess already failed).

Key files: src/ViceSharp.Chips/VicIi/Mos6569.cs (Tick draw pipeline ~1288-1330,
LeftBorderCheckCycle ~1624), PixelSequencer.cs (DrawGraphics8 ~439, DrawColors8 ~598,
DrawBorder8 ~939), VideoRenderer.cs (RenderRasterLine live path ~180-197),
src/ViceSharp.Core/C64MemoryMap.cs (ReadVicPhi1Pal ~702, ReadVicGfxDataOrIdle),
native/vice/vice/src/viciisc/vicii-draw-cycle.c (draw_graphics8 227-295).
