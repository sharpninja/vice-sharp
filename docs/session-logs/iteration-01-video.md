# Session Log - Iteration 01: Video Output

**Date**: 2026-04-20  
**Status**: Video infrastructure complete, palette debugging in progress

## Accomplished

### 1. Original Task Gaps Resolved
- Full machine wiring: 7 devices (RAM, CPU, VIC-II, CIA#1, CIA#2, PLA, SID)
- ROM-to-BASIC boot path: KERNAL executing at $FCE3
- Trace validation: VICE raster format working

### 2. VIC-II Video Pipeline
- Created `VideoRenderer` class with text mode rendering
- Integrated with `Mos6569` via `_renderer.Tick()` call
- Character ROM access at $D000 for 8x8 pixel glyphs
- Color RAM at $D800 for per-character foreground colors
- Screen RAM at $0400 for character codes

### 3. Avalonia VideoSurface
- `WriteableBitmap` connected to VIC-II framebuffer
- `FrameCompleted` event triggers frame copy
- `InvalidateVisual()` redraws the window

### 4. VIC Register Defaults
- `$D020` (border) = 6 (blue)
- `$D021` (bg) = 11 (dark gray)
- Color RAM initialized to 14 (light blue)

## Current Issue

**Palette byte order**: Window displays solid yellow instead of blue.
- The BGRA format order needs debugging
- Renderer writes: `lineBuffer[offset] = (byte)(pixel >> 0)` = lowest byte = B
- Palette entries may have R and B swapped

## Next Steps

1. Debug palette byte order in VideoRenderer
2. Verify blue border renders correctly
3. Verify character ROM reads produce visible text
4. Test C64 BASIC prompt display

## Commits

```
c0ca2c5 fix(vic): corrected BGRA palette byte order for C64 colors
a63b5e3 fix(vic): corrected C64 palette BGRA byte order
69a7dd9 feat(vic): initialize VIC registers and color RAM for visible output
02d26dc feat(vic): implement text mode rendering with character ROM access
ca6a9b3 fix(avalonia): simplify timer, remove InvalidateVisual from timer
556e19f fix(avalonia): fill bitmap with blue so window isn't black
```

## Files Modified

- `src/ViceSharp.Chips/VicIi/VideoRenderer.cs` - New file with text mode rendering
- `src/ViceSharp.Chips/VicIi/Mos6569.cs` - Added VideoRenderer integration
- `src/ViceSharp.Avalonia/VideoSurface.cs` - Framebuffer to WriteableBitmap
- `src/ViceSharp.Avalonia/MainWindow.axaml.cs` - Timer frame rendering
- `src/ViceSharp.Core/ArchitectureBuilder.cs` - Color RAM initialization
