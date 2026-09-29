# ROM-less VIC rendering: resolved - no ViceSharp change needed

**Status: RESOLVED in CbmEngine. There is no VIC-II defect to fix here.** This note exists to record the
investigation and the measurement trap that briefly made it look like a ViceSharp bug.

## What "ROM-less" is
A downstream game (BBCrawler, via CbmEngine) drives an emulated C64 with no copyrighted VICE ROMs: blank
BASIC/CHARGEN, no KERNAL, the 6510 parked in a `JMP *` loop, and the host writing screen RAM `$0400`, bitmap `$2000`,
colour RAM `$D800` and the VIC registers directly, then calling `RunFrame`. The managed VIC-II needs no ROMs to
rasterise a bitmap, so this works.

## The real fix (in CbmEngine, not here)
The only thing that was actually wrong ROM-less was the **VIC bank**. The VIC resets to bank 3
(`src/ViceSharp.Core/C64MemoryMap.cs:128`), but the host's screen (`$0400`) and bitmap (`$2000`) live in bank 0.
The bank moves only on the CIA2 port-A output change that a **CPU store** to `$DD00` triggers (`VIC bank = 3 - ($DD00 & 3)`);
a raw bus write does NOT move it. CbmEngine's `CommodoreSystem.BuildRomless` fixes this in `InitVicBank0` by running a
tiny CPU stub (`LDA #$3F; STA $DD02; LDA #$03; STA $DD00`) via `cpu.ExecuteInstruction()`. With the bank correct, the
VIC fetches the host's content and the multicolour bitmap renders. Verified end to end.

## The trap that made this look like a VIC bug (do not repeat)
`Mos6569.BadLineCountThisFrame` is a **per-frame counter reset at start-of-frame** (`src/ViceSharp.Chips/VicIi/Mos6569.cs`,
reset near line 1526). A full `IMachine.RunFrame()` steps exactly one PAL frame and lands on that reset, so reading the
counter **after** `RunFrame` always returns **0 - even though 25 bad lines fired during the frame**. Reading that `0`
as "the VIC raises no bad lines ROM-less" is wrong. To observe bad lines, sample the counter **during** a frame (step a
partial frame), or - better - assert on the rendered framebuffer, which is immune to the frame boundary.

## The VIC is correct (parity-proven)
The `_allowBadLines` DEN-at-raster-`$30` latch (`Mos6569.cs:1297-1299` / `3101-3102`) fires exactly as VICE does,
independent of whether a KERNAL runs. Native x64sc (KERNAL text, yscroll 3) and the managed parked-CPU machine
(host-written DEN, yscroll 3) raise an IDENTICAL bad-line set: 25 lines, rasters 51..243 step 8. The `3/0` reset seed
and the latch are deliberately left unchanged - see the lockstep-parity comments at `Mos6569.cs:110-127` and
`C64MemoryMap.cs:116-127`.

## Verification tests (in the `vice-sharp-romless` worktree)
`tests/ViceSharp.TestHarness/RomlessBadLineTests.cs`:
- `ParkedCpu_HostDrivenDen_RaisesBadLines` - parks the CPU, sets DEN by hand, steps ~half a frame (NOT a full
  `RunFrame`, to avoid the counter reset), asserts `BadLineCountThisFrame > 0`.
- `RomlessMulticolourBitmap_RendersDistinctContent_NotJustBackground` - the full recipe (bank-0 CPU stub + host content
  + `RunFrame`), asserts the framebuffer shows >= 3 distinct colours.

Both pass, confirming the VIC needs no change. The downstream proof lives in CbmEngine at
`tests/CbmEngine.Tests.Integration/RomlessMachineTests.cs`.
