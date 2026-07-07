# VIC-II vs VICE (viciisc) Parity Audit: Final Report

ViceSharp managed VIC-II (`Mos6569` + `PixelSequencer` + `C64MemoryMap` Phi1/Phi2 fetch) audited cycle-by-cycle against `native/vice/vice/src/viciisc/`. Every divergence below was adversarially verified against native source lines.

---

## 1. Executive Summary

**Overall fidelity: high, with one structural draw-pipeline defect dominating all others.** The managed VIC-II is a close, in many places bit-exact, transcription of VICE viciisc for the PAL 6569R3 model: the per-cycle state machine (badline latch, VC/RC/VMLI/VCBASE lifecycle, refresh counter, start-of-frame arming), the Phi1 cycle-to-fetch dispatch and g-access address generator (g_fetch_addr, the 6569 RAM-to-charROM "fetch magic", ECM `&0x39FF` mask), the horizontal border flip-flops and pixel-exact `draw_border8`, the sprite render pipeline (sbuf shift register, priority winner, both collision accumulators), and the register R/W masking surface all match VICE.

**The single highest-impact root cause is the display-start draw-pipeline timing:** VICE's `vicii_draw_cycle()` applies a one-cycle `cycle_flags_pipe` delay to `draw_graphics8()`/`draw_sprites8()` (vicii-draw-cycle.c:679-687), and resets `dbuf_offset` at `raster_cycle == 1` (vicii-draw-cycle.c:675-677). The port omitted the `cycle_flags_pipe` register entirely and hung the `DbufOffset` reset on the wrong hook (line-begin instead of raster-cycle 1). This mispairs every character's pixels with the next character's colour across the whole line and shifts the resolved-colour buffer 8px. Compounded by the c-access "prefetch" being hardcoded to "first 3 slots" (which corrupts the leftmost 3 columns of every bad line), these three are the confirmed drivers of the open "VIC-II boot display-start garbage / 15-0 checkerboard" symptom.

**Confirmed divergences by severity (after merging duplicates that describe the same underlying drift):**

| Severity | Count | Character |
|----------|-------|-----------|
| Critical | 0 | none |
| High | 3 | draw-pipeline timing + c-access prefetch; break PAL boot lockstep |
| Medium | 15 | mostly NTSC-model timing, mid-line register-write tricks, collision/IRQ ordering, palette generation |
| Low | 10 | latent buffer sizing, NTSC light-pen X, unwired light-pen input, cosmetic palette fidelity, snapshot resume |
| **Total** | **28** | (from 40 raw findings; 12 were duplicate views of a shared root cause) |

**No functional area was entirely free of a confirmed divergence,** but several are near-exact with only NTSC-only, latent, or cosmetic drift (see Section 4).

---

## 2. Divergences by Functional Area (severity-ordered, duplicates merged)

### 2.A Draw pipeline (highest impact)

---

#### H1. `cycle_flags_pipe` absent: `draw_graphics8` visibility gate is not delayed one cycle, mispairing gbuf with vbuf across the whole line
**Severity: HIGH (CONFIRMED)**

- **Managed:** `Mos6569.cs:1299-1300` (`psVisEn = RasterX>=14 && RasterX<54 && !_verticalBorderActive`) feeding `PixelSequencer.DrawGraphics8`; gate at `PixelSequencer.cs:497-528`.
- **VICE:** `vicii-draw-cycle.c:679` (`draw_graphics8(cycle_flags_pipe)`), `:687` (`cycle_flags_pipe = vicii.cycle_flags`), load block `:275-294`.
- **What VICE does:** `vicii_draw_cycle()` passes `cycle_flags_pipe` (the PREVIOUS cycle's flags, updated only at :687 at end of the function) to `draw_graphics8`, so `vis_en` is the prior cycle's VISIBLE_M. The gbuf/vbuf/cbuf/xscroll load block (:275-291) is gated by vis(N-1) while it stores the CURRENT `vicii.gbuf` and `vbuf[dmli]`. VISIBLE_M is set for FetchC cycles rc14..53; delayed by one, the load fires for N in [15,54] (the FetchG cycles), so `gbuf(N)` pixels pair with `vbuf[dmli=N-15]` colour. Correct pairing.
- **What managed does:** `psVisEn` uses the CURRENT RasterX with no delay, so the load fires for RasterX [14,53]. Every column pairs char N pixels with char N+1 colour for the entire line, and the last real g-access (rc54) is dropped.
- **Root cause:** The port implemented `draw_graphics8`'s body but omitted `vicii_draw_cycle`'s `cycle_flags_pipe` register; the visibility gate is sampled at the current cycle instead of one cycle late relative to gbuf/vbuf.
- **Impact:** Whole-line off-by-one character pairing on every text/bitmap line (the 15/0 boot checkerboard) plus a left border 1-2 cycles too wide. Breaks bit-exact lockstep on essentially all displayed content.
- **Fix:** Mirror `cycle_flags_pipe` by delaying ONLY the visibility flag one cycle. Latch `_prevVisEn` at the end of each Tick and pass `_prevVisEn` (not current `psVisEn`) into `DrawGraphics8`, while still passing the current gbuf, `_verticalBorderActive`, and `_idleState` (VICE reads vborder/idle_state live at :275/283/284). Net: load block fires for RasterX 15..54, aligning `gbuf(col k)` with `vbuf[Dmli=k]`. **Caveat:** a naive window shift (`psVisEn>=15 && <55`) was tried and reverted; a COUPLED left-border-width defect exists. Implement as an explicit one-cycle flag pipe, validate cycle-by-cycle against a native gbuf/vbuf/dmli export, and fix the border-flip-flop timing (M5) alongside rather than just shifting the visEn bounds.

---

#### H2. `DbufOffset` reset at line start (`BeginLine`) instead of VICE `raster_cycle == 1`, shifting the colour buffer 8px
**Severity: HIGH (CONFIRMED)**

- **Managed:** `PixelSequencer.cs:329` (`BeginLine` sets `DbufOffset=0`), invoked `Mos6569.cs:1166` at the RasterX==0 wrap tick.
- **VICE:** `vicii-draw-cycle.c:675-677` (`if (vicii.raster_cycle == 1) vicii.dbuf_offset = 0;`).
- **What VICE does:** Zeroes `dbuf_offset` at `raster_cycle==1` (managed RasterX==1), then `draw_colors8` increments by 8 each cycle. Because `draw_colors8` outputs the ring-delayed `pixel_buffer` (previous cycle's render), `dbuf[m*8]` holds graphics(cycle m) uniformly.
- **What managed does:** Resets `DbufOffset=0` at RasterX==0 (one cycle early). `DrawColors8` then runs for RasterX 0 with offs=0, so `LineIndices[m*8]` holds graphics(RasterX m-1); every cycle's colour is one 8px slot to the right of VICE's dbuf placement.
- **Root cause:** Reset hung on the managed line-begin hook (RasterX 0) rather than replicated as an in-draw check at `raster_cycle==1`.
- **Impact:** The entire resolved-colour line shifted 8px horizontally relative to VICE's dbuf indexing. Currently masked only if `VideoRenderer`'s fixed read offset (`FirstVisibleRasterX*8=96`) was hand-tuned to swallow it; remains a mechanism divergence that interacts with H1 and any dbuf snapshot/lockstep comparison.
- **Fix:** Move the reset out of `BeginLine` into `DrawColors8`/`Tick`, executing `DbufOffset=0` when `RasterX==1`, so cycle m's output lands at `LineIndices[m*8]` exactly as VICE.

---

#### M1. ECM background (`COL_D02X_EXT`) resolved inline via live `_regs` instead of kept symbolic through the cregs pipeline
**Severity: MEDIUM (CONFIRMED)**

- **Managed:** `PixelSequencer.cs:402-405` (`cc = _regs[D021 + ((VbufReg>>6)&3)] & 0x0F`).
- **VICE:** `vicii-draw-cycle.c:216-218` (`case COL_D02X_EXT: cc = COL_D021 + (vbuf_reg >> 6)`).
- **What VICE does:** Converts to the SYMBOLIC code `COL_D021+(vbuf_reg>>6)` (0x21..0x24), left in render_buffer; resolved later in `draw_colors8` via `cregs[]` (:601/:617), so ECM background obeys the one-cycle cregs write timing and, on 8565, the pixel-0 grey-dot check (`pixel_buffer[0]==last_color_reg`).
- **What managed does:** Immediately resolves ECM background to a 0..15 palette index from live `_regs`, storing the concrete value. Bypasses the cregs pipeline; because RenderBuffer holds 0..15 not 0x21..0x24, the 8565 grey-dot comparison can never fire for ECM background pixels.
- **Root cause:** Leftover V3 immediate-resolution path not migrated to the symbolic-code + cregs pipeline.
- **Impact:** ECM-mode background pixels ignore the one-cycle-delayed cregs colour-write timing and miss the 8565 grey-dot. Edge case (ECM + mid-visible colour write / 8565) but a genuine value/timing divergence.
- **Fix:** Set `cc = (byte)(0x21 + ((VbufReg>>6)&3))` (symbolic) and let `DrawColors8` resolve it through Cregs.

---

#### M2. `LinePriority` written without the one-cycle colour-pipe delay that `LineIndices` carries, misaligning colour and priority by 8px
**Severity: MEDIUM (CONFIRMED)**

- **Managed:** `PixelSequencer.cs:536-543` (`LinePriority[rasterX*8+i]=PriBuffer[i]`, no delay) vs `LineIndices` written via `DbufOffset` in `DrawColors8:629,648`.
- **VICE:** `vicii-draw-cycle.c:224` (`pri_buffer[i]`) consumed same-cycle by `draw_sprites` (:402); only colour (dbuf) carries the `pixel_buffer` delay (:601-603).
- **What VICE does:** Never persists priority to a line buffer; `pri_buffer` is consumed within the same cycle by `draw_sprites` before the colour pipe. Colour and priority are consumed together per-cycle, never read back at mismatched offsets.
- **What managed does:** Writes `LinePriority` at `rasterX*8` with the CURRENT cycle's PriBuffer while `LineIndices` uses the one-cycle-delayed PixelBuffer, so at slot N*8 colour is from cycle N-1 but priority from cycle N. `VideoRenderer.cs:286-288` reads both at the same index: 8px out of step.
- **Root cause:** A parallel `LinePriority` buffer bolted on outside the colour ring; lacks the inter-cycle delay `LineIndices` inherits from `pixel_buffer`.
- **Impact:** In the non-fast-path live render (multicolor/bitmap/ECM or sprites enabled), sprite-behind-foreground decisions use foreground flags shifted 8px from the actual colour pixels: wrong sprite/background priority at cell edges.
- **Fix:** Preferred (VICE-faithful): maintain a priority ring parallel to `PixelBuffer` inside `DrawColors8` and write `LinePriority[DbufOffset+i]` from the previous-cycle priority so priority and colour share the identical 8px delay. Broader-correct alternative: since `DrawSpritePixel` already composites sprites into RenderBuffer -> LineIndices exactly like VICE (priority consumed same-cycle), the geometric `VideoRenderer.TryGetSpritePixel` overlay plus the `LinePriority` line buffer are redundant in live render; dropping the geometric overlay in the live path eliminates both the double-compositing and the misalignment. If the geometric overlay must stay, apply the priority-ring option.

---

#### L1. `DrawColors8` overflow guard uses 504 vs VICE `VICII_DRAW_BUFFER_SIZE=520`
**Severity: LOW (CONFIRMED)**

- **Managed:** `PixelSequencer.cs:602` (`if (offs > 504 - 8) return`).
- **VICE:** `vicii-draw-cycle.c:631` (`offs > VICII_DRAW_BUFFER_SIZE - 8`), `viciitypes.h:60` (`VICII_DRAW_BUFFER_SIZE (65*8)`).
- **What VICE does:** dbuf is 65*8=520; guard trips at offs>512, giving slack for the 65-cycle NTSC line.
- **What managed does:** `LineIndices` is 63*8=504; guard trips at offs>496. Unreachable in normal PAL operation (DbufOffset never exceeds 496), but the buffer is 16 bytes smaller than VICE's.
- **Root cause:** Buffer sized to the visible 63 PAL cycles rather than VICE's 65-cycle dbuf.
- **Impact:** No divergence in PAL 63-cycle operation; latent NTSC-65 truncation risk, especially while fixing H2 (the reset-location bug).
- **Fix (correct but must be COMPLETE):** Resize `LineIndices` AND `LinePriority` (`PixelSequencer.cs:132`, also `new byte[63*8]`, indexed by the same offs and read at `VideoRenderer.cs:288`) to 65*8=520, or NTSC truncates priority the same way. Change the guard constant to 520-8=512. Update the "504-byte" doc comments (lines 117,121,129) and any `Mos6569.cs:1021` reference. Motivate the comment with NTSC 65-cycle lines (not "slack" / "rc=0 wrap"). Verify `VideoRenderer` live bounds (`FirstVisibleRasterX*8 + ScreenWidth`) for NTSC do not exceed 520.

---

### 2.B Matrix addressing / Phi1-Phi2 fetch

---

#### H3. c-access prefetch garbage hardcoded to first 3 slots instead of VICE `prefetch_cycles` BA counter (corrupts leftmost 3 columns of EVERY bad line)
**Severity: HIGH (CONFIRMED)** — merges fetch-phi1 and matrix-addressing views of the same defect.

- **Managed:** `Mos6569.cs:2372` (`IsMatrixPrefetchSlot => cAccessOrdinal < 3`), `:2382` (`LatchVideoMatrixPrefetch`), consumed by `C64MemoryMap.cs:862-878` (`FetchVideoMatrixPhi2`, prefetch branch 868-871).
- **VICE:** `vicii-fetch.c:192-201` (`vicii_fetch_matrix`); `vicii-cycle.c:580-602` (`prefetch_cycles` countdown).
- **What VICE does:** Takes the prefetch branch (`vbuf=0xff`, `cbuf=ram_base_phi2[reg_pc]&0xf`) only when `prefetch_cycles != 0`. `prefetch_cycles` is a BA counter reset to 3+1 when BA is not low, decremented once per BA-low cycle. On a normal bad line BaFetch first goes low at cycle 12 (`vicii-chip-model.c:134`) and the decrement runs BEFORE the matrix fetch (:585 then :595), so by the first c-access at cycle 15 the counter is already 0 and ALL 40 c-accesses (cycles 15-54) are real fetches.
- **What managed does:** `IsMatrixPrefetchSlot` returns `ordinal < 3`, so the first three c-accesses of EVERY bad line take the prefetch branch: `_videoBuffer[slot]=0xFF`, `_colorBuffer[slot]=CPU-RAM nibble`. The following g-access reads `vbuf[0..2]=0xFF` for display columns 0-2. The `prefetch_cycles` BA counter is not modeled.
- **Root cause:** VICE's dynamic BA-latency counter replaced with a static "first 3 columns are prefetch" heuristic, conflating VICE's prefetch cycles (refresh cycles 12-14, no c-access) with the first real c-accesses (cycles 15-17).
- **Impact:** vbuf/cbuf for columns 0,1,2 hold `0xFF`/CPU-bus garbage instead of real screen char/color on every bad line, so `LastReadPhi1` and the pixel-sequencer graphics for the first three columns diverge from VICE on the boot screen and all text/bitmap screens. This is the known "VIC-II boot display-start garbage" item.
- **Fix:** Port `prefetch_cycles`. (1) Update the counter on EVERY raster cycle in the per-cycle clock (VICE runs BA logic at 582-591 before the matrix fetch at 601), not only during c-access slots; add a per-cycle BA hook. (2) `ba_low` must combine bad-line fetch BA (asserted RasterX 11 / Phi1(12) onward while `_badLine`) AND sprite Phi2 BA (`vicii_check_sprite_ba` equivalent), since VICE resets the counter only when BOTH are inactive. (3) Reset value 4 (decremented in the same cycle BA first goes low). (4) Replace `IsMatrixPrefetchSlot` with `(prefetchCounter > 0)` at each c-access; keep the genuine branch's `cbuf` from the CPU program-fetch RAM byte (`ram_base_phi2[reg_pc]&0xf`, which `ReadCpuProgramRamByte` already does) and `vbuf=0xFF`. Result: normal bad lines fetch all 40 c-accesses for real while late-enabled bad lines still show correct 0xFF prefetch.

---

#### M6. Reset VIC-bank state asymmetric (`_vicBank=3` / `_vicPhi1Bank=0`); VICE keeps Phi1==Phi2 and both 0
**Severity: MEDIUM (CONFIRMED)** — merges the two per-path bank-reset findings.

- **Managed:** `C64MemoryMap.cs:210-211` (`Reset` sets `_vicBank=3`, `_vicPhi1Bank=0`), field init `:116`. `_vicBank` used by whole-line `ReadVideoMemory` (`:641/:644` via `TranslateVicAddress:929-931`); `_vicPhi1Bank` used by per-cycle `ReadVicPhi1Ram`.
- **VICE:** `vicii.c:354-355` (`vbank_phi1=0`, `vbank_phi2=0`); `c64/c64memsc.c:962` (`vicii_set_vbank` sets both together via `vicii_set_vbanks(tmp,tmp)`).
- **What VICE does:** x64sc always drives Phi1 and Phi2 banks from a single value; both initialize to 0 at reset. Phi1 graphics/refresh/sprite and Phi2 matrix/video fetches share the same bank starting at bank 0.
- **What managed does:** `Reset()` seeds the two fields to different literals (3 and 0). Until the first CIA2 $DD00 write resynchronizes them, the VIC reads Phi1 (graphics/char) from bank 0 while the Phi2 video-memory/whole-line path uses bank 3 ($C000).
- **Root cause:** Two bank fields reset independently to different literals; did not mirror VICE's Phi1==Phi2 invariant nor its reset value of 0.
- **Impact:** During boot frames before the KERNAL programs CIA2, Phi1 and Phi2 read from inconsistent banks, and the two managed paths disagree. Contributes to boot-display divergence.
- **Fix:** In `Reset()` set both to VICE's power-up value 0: `_vicBank = 0; _vicPhi1Bank = 0;`. Also change the field initializer at `:116` from 3 to 0. Optionally re-derive the bank from the power-up CIA2 PA read (decodes to bank 0), but a direct 0/0 assignment equals VICE's reset state.

---

#### M14. `color_latency` g-fetch branch taken unconditionally (wrong for 8565/8562)
**Severity: MEDIUM (CONFIRMED)**

- **Managed:** `Mos6569.cs:2280-2301` (`ConsumeGraphicsFetchAddress`), `:2348-2350` (`IdleGraphicsFetchAddress`).
- **VICE:** `vicii-fetch.c:234-262` (`vicii_fetch_graphics`); `vicii-chip-model.c:245,255,265` (color_latency: 6569R1/R3=1, 8565=0).
- **What VICE does:** Branches on `color_latency`: set (6569R1/R3) computes addr from `(regs[0x11] | (reg11_delay & 0x20))` with the RAM->charROM latch magic; clear (8565/8562) uses `addr = g_fetch_addr(reg11_delay)` with no magic. `vicii_fetch_idle_gfx` likewise picks reg11 from `regs[0x11]` vs `reg11_delay`.
- **What managed does:** Always uses the color_latency=1 path and always runs the magic; no selector.
- **Root cause:** Only the 6569R3 case implemented.
- **Impact:** Correct for the current 6569R3; any 8565/8562 model would use the wrong g-fetch mode source and spuriously apply the $D018-split charROM latch, corrupting g-access addresses on mode/$D011 splits.
- **Fix:** Gate on the chip-model `color_latency` flag; when 0, use `address = ComputeGraphicsFetchAddress(reg11Delay)` with no magic and pick idle reg11 from `reg11Delay`, mirroring `vicii-fetch.c:216-222,260-262`.

---

#### M7. Sprite s-access with DMA inactive does not merge the open-bus value into `sprite.data`
**Severity: MEDIUM (CONFIRMED)** — depends on L2 (`last_bus_phi2`).

- **Managed:** `C64MemoryMap.cs:815-822` (`FetchSpriteDataPhi2` returns early when `!IsSpriteDmaActive`), `:793-808` (`ReadVicSpriteData1`).
- **VICE:** `vicii-fetch.c:110-131` (`sprite_dma_cycle_0`), `:133-154` (`sprite_dma_cycle_2`), `:282-299` (`vicii_fetch_sprite_dma_1`).
- **What VICE does:** `sprite_dma_cycle_0` initializes `sprdata=vicii.last_bus_phi2` and, regardless of `check_sprite_dma(i)`, always executes `sprite[i].data = (data & 0x00ffff) | (sprdata<<16)`; `sprite_dma_cycle_2` likewise always merges into the low byte. When DMA is inactive the merged value is current Phi2 open bus.
- **What managed does:** Returns immediately when the sprite has no active DMA; neither high nor low byte of `sprite.Data` is updated, retaining the stale latch instead of the open-bus value.
- **Root cause:** The port gated the entire s-access (including the unconditional data merge) on the DMA-active check; VICE gates only the memory fetch and always writes the resulting byte.
- **Impact:** `sprite.Data` holds stale bytes rather than open-bus when DMA is inactive. Generally unobservable (inactive-DMA sprite is not displayed) but diverges from VICE's exact sprite-shift-register state used in snapshot/lockstep comparisons.
- **Fix:** Always merge the byte into `sprite.Data` on the SprDma0/SprDma2 lanes, using the fetched byte when DMA is active and the current Phi2 open-bus (`last_bus_phi2` equivalent, see L2) when it is not.

---

#### M9. Sprite s-access always performs the RAM fetch, ignoring VICE's `prefetch_cycles` BA-settle gate
**Severity: MEDIUM (CONFIRMED)** — merges sprites-dma "always fetch" and fetch-phi1 "prefetch gating missing on sprite s-accesses".

- **Managed:** `C64MemoryMap.cs:793-808` (`ReadVicSpriteData1`), `:815-822` (`FetchSpriteDataPhi2`), with `Mos6569.cs:2050` (`LatchSpriteData`).
- **VICE:** `vicii-fetch.c:114-120` (`sprite_dma_cycle_0`), `:137-143` (`sprite_dma_cycle_2`), `:282-299` (`vicii_fetch_sprite_dma_1`).
- **What VICE does:** Each s-access performs the real fetch only when `!vicii.prefetch_cycles` (the BA-settle window); during settle it keeps `last_bus_phi2` yet still advances `mc` (`mc = (mc+1)&0x3f`). The Phi1 `vicii_fetch_sprite_dma_1` fetches unconditionally regardless of `prefetch_cycles`.
- **What managed does:** Unconditionally calls `ReadVicPhi1Ram(GetSpriteDataFetchAddress)` whenever `IsSpriteDmaActive`, with no `prefetch_cycles` concept.
- **Root cause:** The managed sprite fetch path never modeled the BA-settle counter for the sprite lanes (it is faked only via the static c-access slot heuristic).
- **Impact:** Fetched data byte differs from VICE only when an s-access lands within the settle window of BA going low (tightly packed sprite DMA at line start after an idle gap); rarely reached on typical screens.
- **Fix:** Reuse the shared `prefetch_cycles` counter from H3 (driven by ALL BA sources: badline matrix and every sprite BA window, not a static slot index). Gate ONLY the two Phi2 sprite lanes: `FetchSpriteDataPhi2` (`sprite_dma_cycle_0` pointer cycle and `sprite_dma_cycle_2` dma1 cycle); when the counter is nonzero substitute the last Phi2 bus value while STILL advancing `mc`. **Do NOT** gate `ReadVicSpriteData1` (the Phi1 SprDma1 access): `vicii_fetch_sprite_dma_1` fetches unconditionally, so gating it would introduce a NEW divergence. Requires accurately modeling `last_bus_phi2` (L2) and the sprite-BA cycle windows.

---

#### L6. ECM g-address mask (`&0x39FF`) not applied in the secondary bitmap collision/foreground helper
**Severity: LOW (CONFIRMED)**

- **Managed:** `Mos6569.cs:3152,3198,3205` (`DeriveGraphicsPxForInvalidEcm` / `IsStandardBitmapForeground` / `IsMulticolorBitmapForeground` compute `BitmapPointerBase + screenIndex*8 + charRow`).
- **VICE:** `vicii-fetch.c:163-182` (`g_fetch_addr`, ECM mask at 177-179).
- **What VICE does:** In ECM (`mode & 0x40`), masks `a &= 0x39ff`, clearing address bits 9 and 10 so vc bits 6/7 do not reach the g-address.
- **What managed does:** The secondary helper builds the bitmap g-address with no `&0x39FF`, so in ECM+BMM (an invalid mode) it fetches from a different address than VICE. The authoritative per-cycle `ComputeGraphicsFetchAddress` does apply the mask; only this secondary path omits it.
- **Root cause:** The ECM mask was implemented for the per-cycle path and for text via `glyph&0x3F`, but the bitmap branch of the secondary priority/collision helper was not masked.
- **Impact:** Wrong foreground/priority (`px & 0x2`) and thus wrong sprite-background collision bits in the invalid ECM+BMM mode. Edge case, secondary path only.
- **Fix:** Apply the mask ONLY inside `DeriveGraphicsPxForInvalidEcm` (where ecm is guaranteed set). For BMM: read from `((BitmapPointerBase + screenIndex*8 + charRow) & 0x39ff)`. For consistency also mask the text branch (line 3165): `((CharacterBase + screenCode*8 + charRow) & 0x39ff)` (equivalently reduce screenCode to `&0x3F`). **Do NOT** modify `IsStandardBitmapForeground`/`IsMulticolorBitmapForeground` (3198/3205): they are only called for valid non-ECM modes (947-948, ecm==false fall-through 3123-3128), so masking there would corrupt valid bitmap collision addressing.

---

### 2.C Cycle state machine, border flip-flops, sprite DMA/expansion (NTSC timing + mid-line writes)

---

#### M3. NTSC (65-cycle) `check_sprite_dma` latch fires one cycle too late
**Severity: MEDIUM (CONFIRMED)** — merges the cycle-state-machine, sprites-dma, and model-variants views (all the same `check1` bug).

- **Managed:** `Mos6569.cs:1794` (`UpdateSpriteDmaLatchForCurrentCycle`, `check1` selection).
- **VICE:** `vicii-chip-model.c:383,385` (`cycle_tab_ntsc` ChkSprDma at Phi1(56)/Phi1(57)), built at `:808` (stored at `cycle_table[cycle-1]`), consumed `vicii-cycle.c:499`.
- **What VICE does:** For NTSC-65, ChkSprDma is at raster_cycle 55 and 56, identical to the old-NTSC table.
- **What managed does:** `check1 = (CyclesPerLine==63)?54:(CyclesPerLine==65?56:55)` yields 56 for NTSC-65, so the latch runs at RasterX 56/57, one cycle late.
- **Root cause:** The NTSC-65 branch was given 56 instead of 55; old-NTSC (the `else=55`) is correct and both NTSC variants share ChkSprDma at rc 55/56.
- **Impact:** On 6567R8/8562/6572 sprite DMA turns on one cycle late every line; because `check_exp` already ran at RasterX 55, the Y-expansion cadence on the turn-on line desyncs (mistimed double-height sprites) and BA stealing shifts. PAL unaffected.
- **Fix:** `int check1 = (CyclesPerLine == PalCyclesPerLine) ? 54 : 55;` (both NTSC variants use RasterX 55/56).

---

#### M4. NTSC (65-cycle) `check_sprite_display` / mc-reload fires one cycle too early
**Severity: MEDIUM (CONFIRMED)** — merges cycle-state-machine, sprites-dma, and model-variants views.

- **Managed:** `Mos6569.cs:1889` (`else if (RasterX == 57)`) and `PixelSequencer.cs:842` (`sprEn = rasterX == 57`).
- **VICE:** `vicii-chip-model.c:389` (`cycle_tab_ntsc` ChkSprDisp at Phi1(59)) vs `:226` PAL Phi1(58) / `:552` old-NTSC Phi1(58); consumed `vicii-cycle.c:511`.
- **What VICE does:** ChkSprDisp (mc=mcbase for all sprites, then `sprite_display_bits` latch) is at raster_cycle 58 for NTSC-65 (the same cycle as SprPtr(0)); rc57 for PAL and old-NTSC.
- **What managed does:** Hardcodes RasterX==57 for every model, so on NTSC-65 the latch runs at rc57 (one cycle early).
- **Root cause:** The mcbase/exp/display cycle points are fixed PAL literals; NTSC-65's shifted back-half moves ChkSprDisp to rc58.
- **Impact:** On 6567R8/8562, sprite display bit and mc reload latched one cycle before the p-access; display-enable/Y-match off by one. PAL/old-NTSC unaffected.
- **Fix (note the corrected constant):** Model-select the display cycle to match each model's sprite-0 p-access `CurrentCycle`: PAL 57, old-NTSC 57, and **NTSC-65 = 59, NOT 58**. Rationale: VICE fires ChkSprDisp on the same raster_cycle as SprPtr(0), and the managed NTSC back-half is already +1-shifted (DMA check at 56/57, sprite-0 p-access at RasterX 59), so 59 is the internally-consistent value. Replace `else if (RasterX == 57)` with `int dispCycle = CyclesPerLine == NtscCyclesPerLine ? 59 : 57;` in both `Mos6569.cs:1889` and `PixelSequencer.cs:842`. (The raw "58" in some finding text is one cycle short of the managed's own sprite-0 fetch cycle.)

---

#### M11. `DrawSprites8` sprite pixel pipeline hardcoded to the PAL cycle model for all chips
**Severity: MEDIUM (CONFIRMED)** — merges sprites-render-collision and model-variants views.

- **Managed:** `PixelSequencer.cs:838` (xpos), `:845-846` (`s_palDmaCycle0/2`), tables at `Mos6569`/`PixelSequencer.cs:230-252` (63-entry PAL).
- **VICE:** `vicii-chip-model.c:273` (`cycle_tab_ntsc` Phi1(1) xpos 0x19c) vs `:112` (PAL 0x194); `:389` (NTSC SprPtr(0) Phi1(59)) vs `:226` (PAL Phi1(58)); consumed via `cycle_is_check_spr_disp` / `cycle_is_sprite_ptr_dma0` / `cycle_is_sprite_dma1_dma2`.
- **What VICE does:** Derives spr_en, the SprPtr/SprDma1 halt/reload masks, and xpos per cycle from the active model's `cycle_tab`, so they follow the 63/64/65-cycle layout automatically.
- **What managed does:** Computes `xpos = (0x194 + 8*rasterX) % 0x1F8` (PAL base/wrap) and reads PAL-only 63-entry `s_palDmaCycle0/2`; for NTSC `rasterX>=63` returns 0 (truncating the extra sprite cycles).
- **Root cause:** Only a PAL sprite fetch/xpos table was implemented; no model branch.
- **Impact:** On all NTSC/PAL-N/old-NTSC models sprite horizontal position and fetch/halt/reload cycles use PAL offsets (mispositioned sprites, wrong-cycle DMA), and the two NTSC-only cycles (rasterX 63/64) perform no sprite DMA. PAL x64sc lockstep unaffected.
- **Fix:** Wire model selection into `DrawSprites8`, making ALL FOUR of these model-specific: (1) xpos base (0x194 PAL, 0x19c NTSC and old-NTSC); (2) xpos wrap modulus (0x1F8 PAL, 0x200 old-NTSC, 0x208 NTSC-65); (3) the truncation guard extended to <64/<65 for NTSC; (4) the sprEn/ChkSprDisp cycle (rasterX==57 PAL, rasterX==58 NTSC-65, consistent with M4). Add NTSC SprPtr/SprDma1 index tables including the extra sprite cycles through Phi1(65), and derive all of them directly from the selected VICE `cycle_tab` so they stay consistent with `vicii_chip_model_set`.

---

#### M8. Start-of-frame force-clears sprite DMA + display state that VICE preserves
**Severity: MEDIUM (CONFIRMED)**

- **Managed:** `Mos6569.cs:1379-1382` (`ApplyFrameStart`).
- **VICE:** `vicii-cycle.c:202-218` (`vicii_cycle_start_of_frame`).
- **What VICE does:** Resets ONLY `start_of_frame`, `raster_line`, `refresh_counter`, `allow_bad_lines`, `vcbase`, `vc`, `light_pen.triggered`. Never touches `sprite_dma`, `sprite_display_bits`, or per-sprite `mc/mcbase/exp_flop`; sprite DMA turns off only via `sprite_mcbase_update` when `mcbase==63`.
- **What managed does:** `ApplyFrameStart` executes `_spriteDmaActiveMask = 0; Array.Clear(...); _spriteDisplayBits = 0;`, terminating every in-progress sprite DMA and all display bits at the frame boundary.
- **Root cause:** Legacy per-frame accounting (BACKFILL-VIDEO) reset kept after adopting the VICE-exact mcbase==63 turn-off model.
- **Impact:** Any sprite whose DMA/display window straddles the raster wrap (low-Y sprites re-enabled on lines 256-311, common in lower-border sprite work and multiplexers) is force-stopped at line 0 instead of continuing until mcbase==63; the first line(s) drop or expansion cadence resets. Boot display (no sprites) unaffected.
- **Fix:** Delete the four sprite-state resets from `ApplyFrameStart`; let `sprite_dma` turn off exclusively via `sprite_mcbase_update` (mcbase==63) and `sprite_display_bits` be maintained by `check_sprite_display`. Keep only diagnostic per-frame counter resets if needed.

---

#### M5. Vertical border FF not evaluated every cycle; missing `vborder = set_vborder` copy at raster cycle 1 (mid-line DEN/RSEL writes ignored)
**Severity: MEDIUM (CONFIRMED)** — merges border-flipflops findings #1 (cycle-0 copy) and #2 (per-cycle top/bottom checks) and the cycle-state-machine vborder finding into one discipline fix.

- **Managed:** `Mos6569.cs:1545-1550` (`UpdateVerticalBorderForLineStart`), `:1552-1583` (`UpdateBorderFlipFlopsForCurrentCycle` / `CheckVerticalBorderTop/BottomForCurrentLine`).
- **VICE:** `vicii-cycle.c:476-482` (`check_vborder_top`, `check_vborder_bottom` every cycle, then `vborder=set_vborder` at CYCLE(1)); comparison logic `:165-182`.
- **What VICE does:** Calls `check_vborder_top(raster_line)` and `check_vborder_bottom(raster_line)` on EVERY cycle, each re-reading live registers (top vs `(regs[0x11]&0x08)?0x33:0x37` gated by DEN; bottom vs `0xfb:0xf7`), and unconditionally copies `vicii.vborder = vicii.set_vborder` at raster cycle 0 / CYCLE(1), IN ADDITION to the copy inside `check_hborder` at the left-border check.
- **What managed does:** `CheckVerticalBorderTop` runs only at line start; `CheckVerticalBorderBottom` at line start plus the left-border-check cycle; the `_verticalBorderActive = _verticalBorderNextActive` transfer happens ONLY at `LeftBorderCheckCycle` (RasterX 16/17), never at raster cycle 1. So for cycles 1..16 of a transition line the flag lags VICE, and a mid-line $D011 write is not re-sampled.
- **Root cause:** Border state maintained for a whole-line snapshot renderer rather than a strict per-cycle flip-flop; the per-cycle re-evaluation and the cycle-1 latch were collapsed to line-start plus left-check.
- **Impact:** On the bottom/stop line `_verticalBorderActive` is stale (false) for RasterX 0-15, so the vis_en graphics gate (`Mos6569.cs:1299`) enables graphics at RasterX 14-15 where VICE has vborder==1; pixel output is masked by main_border but the graphics pipeline and PriBuffer (feeding sprite-background collision) advance two extra cycles, so a sprite overlapping the far-left border on the stop line can register a spurious collision. Programs toggling DEN/RSEL mid-line (FLD / open-border raster tricks) diverge: VICE flips the FF at the exact write cycle, managed only at the next line boundary. Static screens unaffected.
- **Fix:** In the per-cycle path (run every non-wrap cycle): (1) call `CheckVerticalBorderTopForCurrentLine()` every cycle (VICE's `check_vborder_top` writes vborder directly, so a mid-line DEN write opens the top border immediately); (2) call `CheckVerticalBorderBottomForCurrentLine()` every cycle so `set_vborder`/`_verticalBorderNextActive` tracks a mid-line RSEL change; (3) keep the transfer at `LeftBorderCheckCycle` AND add the same `_verticalBorderActive = _verticalBorderNextActive` transfer at raster cycle 1 (in `UpdateVerticalBorderForLineStart`, after the top/bottom checks and before Capture), mirroring `vicii-cycle.c:480-482`. Because evaluation is per-cycle, no special-casing in the $D011 Write handler is needed. Re-capture into `_verticalBorderActiveByRasterLine` at the transfer points so the whole-line arrays stay consistent. Guard with a regression test that writes $D011 mid-line on the top/bottom comparison lines (open-border trick) vs x64sc.

---

### 2.D Collision, raster IRQ, register R/W

---

#### M10. Deferred collision-register clear applied BEFORE the per-cycle sprite draw (and the can_* IRQ gate captured after the clear) instead of after the draw
**Severity: MEDIUM (CONFIRMED)** — merges the sprites-render-collision and raster-irq views of the identical ordering bug.

- **Managed:** `Mos6569.cs:1097-1104` (pending clear at top of Tick), `:1308-1320` (can capture + `DrawSprites8` + IRQ).
- **VICE:** `vicii-cycle.c:407-433`.
- **What VICE does:** In one `vicii_cycle`: captures `can_sprite_sprite`/`can_sprite_background` BEFORE the draw (:407-408, reading the still-non-zero accumulators); `vicii_draw_cycle()` ORs new collisions in (:411); the deferred `clear_collisions` from a prior $D01E/$D01F read is applied AFTER the draw (:414-425), also wiping this cycle's collisions; the collision IRQ (:428-432) is gated by the pre-draw can_* (false in the clearing cycle), so no IRQ fires that cycle and the re-trigger happens the following cycle.
- **What managed does:** Applies `_pendingCollisionClear` at the very top of Tick (:1097) before the draw; canSs/canSb sampled at :1308-1309 AFTER the clear read the just-zeroed latch as true; `DrawSprites8` re-accumulates into the latch; the IRQ check sees `can==true && latch!=0` and fires in the SAME clearing cycle, and the latch retains that cycle's collision.
- **Root cause:** The clear was positioned for the retired per-line collision approximation (stale comment :1090-1096) and never repositioned when the per-cycle `DrawSprites8` pipeline replaced it; the can_* gate is captured after the clear rather than before the draw.
- **Impact:** After a program reads $D01E/$D01F to acknowledge a collision, managed re-fires the SS/SB collision IRQ one cycle early and leaves the register non-zero at the end of the clearing cycle, whereas VICE keeps it zero and re-fires one cycle later. Shifts collision-driven raster IRQ timing by one cycle and changes the read-back value. Reachable from standard collision-handler code that reads $D01E then continues rendering overlapping sprites.
- **Fix:** Remove the top-of-Tick clear (`:1097-1104`). Relocate it into the sprite-collision block: capture canSs/canSb from the latch BEFORE any clear (leave the existing :1308-1309 sample, which is already pre-clear once the top clear is removed); run `DrawSprites8` and OR its masks into the latch (:1311-1312); THEN apply the selective clear (if `_pendingCollisionClear==0x1E` zero `_spriteSpriteCollisionLatch` else zero `_spriteBackgroundCollisionLatch`, then `_pendingCollisionClear=0`) BEFORE the irqBits computation (:1313); THEN do the IRQ check using the pre-draw canSs/canSb. Update the stale comment at :1090-1096.

---

#### M12. Raster IRQ line asserted one VIC cycle late while collision/light-pen assert immediately (asymmetry absent in VICE)
**Severity: MEDIUM (CONFIRMED)**

- **Managed:** `Mos6569.cs:1201-1205` (sets `_rasterIrqAssertPending`) and `:1084-1088` (applies it at top of NEXT Tick); collision at `:1319`, light-pen at `:2911,:2923` call `RefreshInterruptLine` immediately.
- **VICE:** `vicii-irq.c:47-62` and `:116-121`.
- **What VICE does:** `vicii_irq_raster_trigger -> vicii_irq_raster_set -> vicii_irq_set_line_clk(maincpu_clk)` sets irq_status bit7 and calls `maincpu_set_irq_clk` in the SAME cycle as the latch. Collisions (`vicii_irq_sscoll_set`/`sbcoll_set`) and light pen use `maincpu_set_irq` at the current clock. No source is delayed relative to the others.
- **What managed does:** The raster compare sets $D019 bit0/bit7 in-cycle but defers the physical `_irqLine.Assert` to the top of the following Tick; collision and light-pen IRQs assert same-cycle. The raster line rise lags collision/light-pen line rises by one VIC cycle.
- **Root cause:** A CPU IRQ-recognition-latency was modeled on the VIC side for the raster source only (documented at :40-48), on the premise that the managed CPU samples `IInterruptLine` with no internal latency; it was not applied symmetrically.
- **Impact:** If the managed CPU applies any standard 6502 IRQ latency itself, raster IRQs are recognized one cycle later than collision/light-pen IRQs relative to their register-set cycle, shifting cycle-exact stable-raster / raster-split timing by one cycle on demo-style code (even though boot READY snapshot lockstep passes).
- **Fix (low-risk, preferred):** Make the physical IRQ-line rise uniform. Route collision (:1318-1319) and light-pen (:2911,:2923) asserts through the same deferred path: generalize `_rasterIrqAssertPending` to a shared `_irqAssertPending` (or equivalent flags) so all sources present their line rise at the top of the next Tick (:1084-1088). Keep the in-cycle $D019 bit0/bit7 set (already correct and register-parity-tested). The alternative (remove the raster deferral and move a full INTERRUPT_DELAY=2 model into the CPU core) is cleaner architecturally but riskier: it requires the two-cycle irq_clk comparison in the CPU and re-validating the already-green raster lockstep. Also review IRQ-line RELEASE timing for the same symmetry (VICE's clear paths use `irq_pending_clk = cpu_clk + 3`).

---

#### M13. $D017 store omits sprite-crunch (mc rewrite) and `exp_flop` side effect
**Severity: MEDIUM (CONFIRMED)**

- **Managed:** `Mos6569.cs:2718-2724` (`UpdateSpriteRegisters` offset 0x17).
- **VICE:** `vicii-mem.c:183-214` (`d017_store`).
- **What VICE does:** For each sprite whose new Y-expand bit is 0 while `sprite[i].exp_flop` is 0, performs the crunch when `cycle_is_check_spr_crunch(cycle_flags)`: `mc = (0x2a & (mcbase & mc)) | (0x15 & (mcbase | mc))` (:198-205), and unconditionally sets `exp_flop = 1` (:209). `regs[0x17]` written last (:213). Early-returns if `value == regs[0x17]` (:190-192).
- **What managed does:** Only sets `IsExpandedY` for all 8 sprites; never touches `ExpFlop` or applies the crunch formula. The `ExpFlop` toggle is handled solely in the per-cycle fetch pipeline (`:1878-1885`), which cannot reproduce the store-time crunch nor the immediate `exp_flop=1`.
- **Root cause:** The $D017 write was reduced to a pure expansion-flag decode; the store-time crunch/exp_flop interaction with running fetch state was not ported.
- **Impact:** The vertical sprite-crunch / sprite-stretch trick renders incorrectly, and turning off Y-expansion does not immediately arm `exp_flop`, so the following line's toggle can be one line off. Affects demos/games using sprite crunch; not boot.
- **Fix:** In `Mos6569.Write`, handle offset 0x17 before the `IsExpandedY` decode: (1) early-out if `value == _registers[0x17]`; (2) for each sprite where the bit is 0 and `!ExpFlop`: if the current cycle is the check-spr-crunch cycle (RasterX==15, VICE Phi2 cycle 15) apply `Mc = (byte)((0x2a & (McBase & Mc)) | (0x15 & (McBase | Mc)))`; then set `ExpFlop = true` unconditionally (independent of the crunch-cycle test); (3) store the register and set `IsExpandedY`. Run against the live `_sprites` Mc/McBase used by the per-cycle pipeline.

---

#### L2. `vicii.last_bus_phi2` not modeled: register R/W never latches the bus, idle sprite fetches use the wrong value
**Severity: LOW (CONFIRMED)** — prerequisite for M7 and M9.

- **Managed:** `Mos6569.cs:2481` (Read), `:2586` (Write) — no `last_bus_phi2` field exists.
- **VICE:** `vicii-mem.c:338` (store) and `:738` (read) set `vicii.last_bus_phi2 = value`; `vicii-cycle.c:605` resets it to 0xff each cycle; `vicii-fetch.c:112,135` consume it.
- **What VICE does:** Every `vicii_store` and `vicii_read` latch the transferred byte into `last_bus_phi2`; it resets to 0xff at end of cycle. During an idle-sprite s-access, `sprite_dma_cycle_0/2` read `sprdata = last_bus_phi2`, so an idle sprite data byte reflects any same-cycle CPU access to a VIC register (else 0xff).
- **What managed does:** No `last_bus_phi2` state; Read/Write do not latch, no per-cycle reset. Idle sprite fetches never see a CPU-bus value.
- **Root cause:** The phi2 bus-latch side effect of register R/W was not ported.
- **Impact:** When a sprite shift register is active but the sprite is not doing DMA and the CPU accesses a VIC register the same cycle, the idle sprite data byte diverges. Narrow; no effect on common boot display.
- **Fix:** Add `byte _lastBusPhi2 = 0xFF;`. In `Read(offset)` set `_lastBusPhi2 = result` for the exact returned byte (masked/OR'd value, or 0xFF for >=0x2F, mirroring vicii-mem.c:738). In `Write(offset,value)` set `_lastBusPhi2 = value` at entry (vicii-mem.c:338). Reset to 0xFF once per cycle at end-of-cycle (vicii-cycle.c:605), preserving intra-cycle ordering so an earlier same-cycle $D0xx access is still visible to that cycle's sprite phi2 fetch. Expose to `C64MemoryMap`; the non-DMA idle branch in `ReadVicSpriteData1`/`FetchSpriteDataPhi2` must use `_vic.LastBusPhi2` instead of `ReadVicIdleGap()`/RAM $3FFF AND still merge that byte into the 24-bit Data lanes (fixes M7). Add a focused test writing a known value to a $D0xx register in the same cycle as an idle sprite s-access.

---

### 2.E Palette / color

---

#### M15. Palette is an arbitrary hardcoded RGB table, not VICE's YUV-derived 8565r2 palette (default model)
**Severity: MEDIUM (CONFIRMED)** — this is the umbrella color-fidelity divergence; L7/L8/L9 are facets of it.

- **Managed:** `VicPalette.cs:14-32` (static `Colors[]`), consumed verbatim by `VideoRenderer.cs:52-53`.
- **VICE:** `vicii-color.c:513-531` (`vicii_colors_8565r2`) + `:650` (`video_color_palette_internal`); default model `VICII_MODEL_8565` (`vicii-resources.c:202`), TOBIAS_COLORS build (`vicii-color.c:50`).
- **What VICE does:** Stores 16 colors as YUV (luma*256, chroma angle, saturation*256) and converts to RGB inside `video_color_palette_internal` with default brightness/contrast/saturation/gamma resources. The rendered RGB is the OUTPUT of that pipeline, not any literal in the source.
- **What managed does:** Holds 16 fixed RGB literals matching no VICE model palette. Only Black and White coincide; the other 14 differ substantially (e.g. managed Purple 0x9B27B1 vs VICE's darker desaturated purple).
- **Root cause:** The port baked a static approximate RGB palette instead of porting VICE's per-model YUV tables plus the YUV->RGB generation pipeline.
- **Impact:** Every colored pixel differs from VICE, so the framebuffer is not bit-exact on any colored screen, including the boot display (light-blue on blue). Framebuffer lockstep/snapshot vs x64sc fails on all non-black/white pixels. Does NOT affect color-index-level determinism/lockstep.
- **Fix:** Port `vicii_colors_8565r2` YUV table and the `video_color_palette_internal` YUV->RGB pipeline (`src/video/video-color.c`) with default video resources, generating the 16 RGB entries at init instead of hardcoding. For byte-exact parity also account for `SEPERATE_ODD_EVEN_COLORS` (`vicii_colors_8565r2_odd/_even`, :533-571) and any active CRT/PAL filter. Validate each of the 16 base RGBs against x64sc's rendered palette for model 8565.

---

#### L7. No per-model palette selection; a single static table used for all models
**Severity: LOW (CONFIRMED)**

- **Managed:** `VicPalette.cs:14` (single `Colors[]`), `VideoRenderer.cs:52`.
- **VICE:** `vicii-color.c:630-648` (`vicii_color_update_palette` switch).
- **What VICE does:** Under TOBIAS_COLORS, installs a different palette per chip: (6567R56A, 6569R1) -> 6569r1; (6567, 6572, 6569) -> 6569r5; (8562, 8565) -> 8565r2. Each has distinct luma/chroma/saturation, so NTSC/old-PAL/new-PAL render measurably different colors.
- **What managed does:** One global `Colors[]` with no model notion; selecting 6569 vs 8565 vs 6567 has zero effect.
- **Root cause:** Model-conditional palette generation not ported.
- **Impact:** Cross-model color parity impossible. Color-index-level output unaffected (indices 0-15 identical), so index-based lockstep is not broken; rendering-accuracy only.
- **Fix:** Replicate the TOBIAS_COLORS switch (:631-648): map ViceSharp VIC-II model classes onto the three palette groups and run each YUV table through `video_color_palette_internal` with default resources. Rendering-accuracy improvement, not a determinism fix.

---

#### L8. No YUV->RGB conversion nor video color-adjustment pipeline (saturation/brightness/contrast/gamma)
**Severity: LOW (CONFIRMED)**

- **Managed:** `VicPalette.cs:14-53` (fixed RGB, no conversion).
- **VICE:** `vicii-color.c:650` (`video_color_palette_internal`); palette type `CBM_PALETTE_YUV` (:584).
- **What VICE does:** Converts luma+angle+saturation to RGB and applies brightness/contrast/saturation/gamma/tint; changing any resource re-derives the palette.
- **What managed does:** Stores post-conversion RGB literals with no YUV representation and no adjustment stage; video color settings have no effect.
- **Root cause:** The entire color-generation stage (`video-color.c` equivalent) omitted.
- **Impact:** Even for default 8565, numbers cannot match VICE (gamma/saturation applied); no configuration reproduces VICE output bit-exactly. Does not affect lockstep/determinism.
- **Fix (only if pixel-RGB fidelity becomes a goal):** Port the full `video-color.c` pipeline: (1) select the default model palette per the TOBIAS_COLORS branch including odd/even variants and reproduce the odd/even averaging; (2) implement `video_calc_ycbcrtable` and `video_convert_renderer_to_rgb_gamma` with VICE defaults (brightness=contrast=saturation=1000, gamma=1000 -> 2.8 PAL/2.2 NTSC) and clamping; (3) recompute the RGB table when any color resource changes. Keep conversion strictly in the display/VideoRenderer layer so core output stays index-based.

---

#### L9. Grey-ramp luminances do not match VICE's 8565r2 luma values
**Severity: LOW (CONFIRMED)**

- **Managed:** `VicPalette.cs:27-31` (DarkGrey 0x44, Grey 0x77, LightGrey 0xAA).
- **VICE:** `vicii-color.c:526-530` (Dark/Medium/Light Grey lumas).
- **What VICE does:** 8565r2 greys are pure (saturation 0) with lumas Dark=0.306*256=78.3, Medium=0.461*256=118.0, Light=0.639*256=163.6, then gamma-adjusted.
- **What managed does:** R=G=B = 0x44/68, 0x77/119, 0xAA/170. Even pre-gamma (78/118/164) they disagree, and gamma widens the light-grey gap.
- **Root cause:** Grey levels eyeballed rather than derived from the 8565r2 luma constants and gamma curve.
- **Impact:** The three greys (widely used in charset/UI) render at the wrong brightness.
- **Fix:** Do not fix only the greys; regenerate the ENTIRE 16-entry palette via VICE's pipeline (`video_cbm_palette_to_ycbcr` then `video_convert_renderer_to_rgb_gamma` with default resources) for the target model. Under default settings the greys become ~(78,78,78)/(118,118,118)/(164,164,164). Displayed/screenshot RGB only; not lockstep index determinism. (This is a subset of M15 / L8; fix them together.)

---

### 2.F Light pen

---

#### L3. Light-pen X uses the hardcoded PAL xpos base (0x194) and PAL wrap for all models; NTSC X is wrong
**Severity: LOW (CONFIRMED)** — merges lightpen and model-variants views.

- **Managed:** `Mos6569.cs:2895-2896` (`TriggerLightPenInternal`).
- **VICE:** `vicii-lightpen.c:75`; `vicii-chip-model.c:272-289` (`cycle_tab_ntsc`); `vicii-chip-model.h:164-167` (`cycle_get_xpos`).
- **What VICE does:** `x = cycle_get_xpos(cycle_table[raster_cycle]) / 2` from a per-model table. NTSC starts cycle-1 Phi1 xpos at 0x19c (not 0x194) and the 65-cycle table stores a duplicated 0x184 at cycles 62 and 63; `cycle_get_xpos` masks low 3 bits.
- **What managed does:** `phi1Xpos = (0x194 + 8*RasterX) % 0x1F8`, base and wrap hardcoded regardless of model; no per-model table.
- **Root cause:** The xpos formula was derived from the PAL 6569 table and inlined; NTSC base (0x19c) and the 62/63 stall not modeled.
- **Impact:** On NTSC models every normal $D013 latch is off by 4 for most cycles and diverges further around the wrap and cycles 62/63. PAL correct. Niche.
- **Fix:** Port VICE's per-model `cycle_table` xpos, keying on chip model / `_cyclesPerLine`. A base-only fix is insufficient: the wrap modulus differs (PAL 0x1F8, NTSC 0x200) and the NTSC 62/63 xpos stall (0x184 held) must be reproduced. Build a precomputed per-model Phi1-xpos table indexed by raster_cycle with the `&~7` rounding. The sprite pipeline (`PixelSequencer.cs:838`) hardcodes the same PAL formula and needs the same table.

---

#### L5. CIA1 PB4 light-pen line is not wired to `SetLightPen`; nothing in the machine ever triggers the pen
**Severity: LOW (CONFIRMED)**

- **Managed:** `Mos6569.cs:2846` (`SetLightPen`) — no caller anywhere in src.
- **VICE:** `c64/c64cia1.c:153` (`vicii_set_light_pen(maincpu_clk, !(m & 0x10))`).
- **What VICE does:** Drives the VIC-II light-pen input from CIA1 port B bit 4: whenever CIA1 PB state changes, calls `vicii_set_light_pen`; pulling PB4 low asserts the pen and schedules the trigger.
- **What managed does:** No call to `SetLightPen`/`TriggerLightPen` outside the class; the CIA glue never invokes them. The latch can only be exercised by direct test calls.
- **Root cause:** CIA1-to-VIC light-pen wiring not implemented in the managed architecture/device adapter.
- **Impact:** Real programs using the light pen via CIA1 ($DC01 bit 4) never get an LP IRQ and never see $D013/$D014 update. Light-pen software non-functional end-to-end.
- **Fix:** In the C64 CIA1 glue, replicate `cia1_internal_lightpen_check`: on every CIA1 PA and PB store, compute `m = val & pb & joyport1Digital` and call `_vic.SetLightPen(!(m & 0x10))`. Do not pass a clock argument (managed `SetLightPen(bool)` already schedules one cycle later via `_lightPenTriggerPending`). Drive from both PA-store and PB-store paths (VICE `store_ciapa:163` and `store_ciapb:176`), not PB4 changes alone.

---

#### L4. `vicii_lightpen_timing` (real light-pen pointing-device path) is entirely unported
**Severity: LOW (CONFIRMED)**

- **Managed:** `Mos6569.cs` — no equivalent; only `SetLightPen`/`TriggerLightPenInternal` exist.
- **VICE:** `vicii-lightpen.c:110-130` (`vicii_lightpen_timing`).
- **What VICE does:** For a real pen at (x,y): `x += 0x80 - screen_leftborderwidth; y += first_displayed_line;` if `x < 104` the pen is off-screen and `pulse_time=0` (no trigger); otherwise `pulse_time = maincpu_clk + (x/8) + (y*cycles_per_line)` and `light_pen.x_extra_bits = (x >> 1) & 0x3` (sub-CLK precision from the actual pixel position).
- **What managed does:** No analogue. The only `x_extra_bits` source is the chip-model constant (`color_latency?2:1`); no pixel-derived value, no off-screen suppression, no coordinate-based pulse scheduling.
- **Root cause:** Only the CIA-line path was ported; the pointing-device timing callback (registered in VICE via `lightpen_register_timing_callback`) was omitted.
- **Impact:** Any emulated light-pen pointing device latches $D013 with the wrong sub-CLK X offset and fires even off-screen. Pen-driven programs (paint/menu software) diverge.
- **Fix:** Port `vicii_lightpen_timing` as a `Mos6569` method taking (x,y): apply `x += 0x80 - screenLeftBorderWidth; y += firstDisplayedLine;` return/schedule no trigger when `x < 104`; else set `_lightPenXExtraBits = (byte)((x >> 1) & 0x3)` and schedule at `maincpu_clk + (x/8) + (y*cyclesPerLine)`. Complete only if paired with the joyport pointing-device layer (also unported) that actually calls it, and reconciled with L3's xpos base-translation so LINE and pointing-device paths stay consistent.

---

### 2.G Snapshot / state resume

---

#### L10. `InjectSnapshotState` resume re-derives vc/vcbase/rc/vmli/refresh instead of restoring them like VICE
**Severity: LOW (CONFIRMED)**

- **Managed:** `Mos6569.State.cs:34-84` (`InjectSnapshotState`; docstring :21-32 states counters "re-derive within a frame").
- **VICE:** `vicii-snapshot.c:105-108,131,223-227,250,270` (vcbase, vc, rc, vmli, refresh_counter restored explicitly; draw pipeline via `vicii_draw_cycle_snapshot_read`).
- **What VICE does:** On restore, loads vcbase, vc, rc, vmli and refresh_counter directly from the snapshot (does NOT re-derive from raster position), and restores the draw pipeline before continuing.
- **What managed does:** `InjectSnapshotState` (the mid-frame lockstep resume path) seeds only registers, `CurrentRasterLine`, `RasterX`, raster-IRQ state, `_reg11Delay`, color logs, and optionally `_allowBadLines`/`_idleState`. It does not seed `_videoCounter`, `_vcBase`, `_rowCounter`, `_vmli`, `_refreshCounter`, or `_spriteDmaActiveMask`; the docstring asserts they re-derive within a frame.
- **Root cause:** The resume path was built to seed only what is needed to restart at frame top; vc/vcbase/rc/vmli genuinely re-derive only at start-of-frame (all zero), not at an arbitrary mid-frame raster position.
- **Impact:** If a lockstep .vsf resume point is after the visible area has begun, the managed VIC starts with stale/zero counters and diverges for the rest of the frame (wrong characters, wrong row, wrong refresh address). At frame top it is equivalent. The CPU-compare lockstep test does not currently observe these counters, so it matters for rendered-frame parity and (via `_spriteDmaActiveMask`) mid-frame sprite-BA timing, not the existing register/cycle compare.
- **Fix:** Managed already has a vmli field (`_videoMatrixLineIndex`, `Mos6569.cs:1459`). Extend `InjectSnapshotState` to seed `_videoCounter` (vc), `_vcBase` (vcbase), `_rowCounter` (rc), `_videoMatrixLineIndex` (vmli), `_refreshCounter`, `_spriteDmaActiveMask`, and per-sprite MC/MCBASE from the .vsf VIC-II module (matching vicii-snapshot.c:224-227/250/261-262). Simpler alternative sufficient for the current CPU-only lockstep metric: constrain lockstep resume to require the native .vsf be captured at raster line 0 / cycle 0 and assert that precondition.

---

## 3. Prioritized Remediation Plan

Fix root causes first; the draw-pipeline timing fixes are expected to resolve several visible symptoms at once. Each step uses the VIC parity test suite as the guardrail: run the focused VIC index-parity / lockstep filter and require it green before advancing, and add the noted regression per step.

**Phase 1 - Draw-pipeline root cause (unblocks PAL boot lockstep).** Do these three together because they interact and a partial fix was previously reverted:
1. **H1 `cycle_flags_pipe` one-cycle vis-gate delay** (`Mos6569.cs`/`PixelSequencer.DrawGraphics8`). Validate cycle-by-cycle against a native gbuf/vbuf/dmli export.
2. **H2 `DbufOffset` reset at `RasterX==1`** (move out of `BeginLine` into `DrawColors8`/`Tick`).
3. **M5 border-flip-flop per-cycle discipline + cycle-1 `vborder=set_vborder` copy** (fixes the coupled "left border ~2 cycles too wide" defect flagged in H1's caveat).
   - Guardrail: the 15/0 boot-checkerboard should disappear; verify against x64sc frame. Add the mid-line $D011 open-border regression.

**Phase 2 - Matrix/fetch root cause (fixes leftmost-3-columns garbage).**
4. **H3 `prefetch_cycles` BA counter** replacing the `slot<3` heuristic. Add a per-cycle BA hook combining badline + sprite BA. This single counter is also the prerequisite for M9 and part of M7's open-bus source, so build it as shared infrastructure.
5. **M6 VIC-bank reset to 0/0** (trivial; removes the Phi1/Phi2 boot-bank mismatch that compounds boot-display divergence). Do before re-baselining boot frames.
   - Guardrail: boot-screen character/color columns 0-2 now match; re-run the full boot lockstep.

**Phase 3 - Collision / IRQ ordering (one cycle of collision/IRQ timing).**
6. **M10 collision-clear-after-draw reorder** (remove top-of-Tick clear, relocate after accumulate, before IRQ check; capture can_* pre-draw).
7. **M12 uniform IRQ-line assert** (route collision/light-pen through the same deferred path as raster). Prefer the deferral-for-all approach over a CPU INTERRUPT_DELAY rewrite.
   - Guardrail: collision read-back and collision-IRQ timing tests; keep raster lockstep green.

**Phase 4 - Sprite state and register-write side effects.**
8. **M8 stop force-clearing sprite DMA/display at frame start** (delete the four resets).
9. **L2 `last_bus_phi2` field** (register R/W latch + per-cycle 0xff reset), then **M7 always-merge idle sprite byte** and **M9 sprite prefetch/BA gate** (reuse the Phase-2 counter; do NOT gate the Phi1 SprDma1 access).
10. **M13 $D017 sprite-crunch + `exp_flop`** side effect.
   - Guardrail: sprite-crunch/stretch and multiplexer straddle-wrap regressions vs x64sc.

**Phase 5 - NTSC model correctness (no effect on PAL x64sc lockstep, so lower priority).**
11. **M3 `check1 = ...?54:55`** and **M4 model-select display cycle (PAL 57 / NTSC-65 59)**.
12. **M11 model-select the whole `DrawSprites8` xpos base/wrap/guard/ChkSprDisp and NTSC sprite tables**, plus **M14 `color_latency` g-fetch branch** and **L1 resize `LineIndices`+`LinePriority` to 520**. L1 should be done before/with M11 since NTSC exercises the larger buffer.
13. **L3 per-model light-pen xpos table** (shared with the sprite xpos table from M11).
   - Guardrail: stand up an NTSC (6567R8) parity fixture; these are currently unguarded.

**Phase 6 - Fidelity and completeness (non-determinism, cosmetic or niche).**
14. **M15 + L7 + L8 + L9 palette pipeline** (port VICE's YUV tables + `video_color_palette_internal` + per-model selection; regenerate all 16 entries). Fix as one unit in the display layer; do not touch core index output.
15. **L5 CIA1 PB4 wiring**, then **L4 `vicii_lightpen_timing`** (+ joyport pointing-device layer).
16. **L6 ECM `&0x39FF` mask** in the secondary collision helper.
17. **L10 snapshot resume** counter seeding (or assert frame-top-only resume as the interim guard).

---

## 4. Areas With No / Minimal Confirmed Divergence

No functional area was entirely free of a confirmed divergence, but several are near-exact, with only NTSC-only, latent, or cosmetic drift:

- **Cycle state machine (PAL path): essentially clean.** Raster-cycle advance, badline latch, VC update at RasterX 13, RC update at RasterX 57 with the exact rc==7 idle/vcbase sequence, VCBASE capture, VMLI reset, refresh counter, start-of-frame arming, DEN gating, reset raster_cycle=6, and in-cycle ordering all match VICE for PAL and 64-cycle old-NTSC. Its only confirmed drifts (M3/M4/M5) are NTSC-model timing and renderer-masked vborder discipline.
- **Phi1 fetch dispatch: clean.** `ReadVicPhi1Pal/NtscOld/Ntsc` faithfully transcribe VICE's per-model `cycle_tab_*` (p/s/refresh/g/idle slots correct for all three models); the g-access address generator, refresh counter, and sprite-pointer addressing match. All confirmed fetch drifts are on the Phi2 side folded into the dispatch.
- **Horizontal border flip-flops: clean.** Main-border FF open/close, CSEL left/right check cycles (RasterX 16/17 and 56/55), RSEL start/stop constants (0x33/0x37, 0xfb/0xf7), DEN gate, and the pixel-exact `draw_border8` `border_state` 1-cycle pipeline (including the CSEL=0 pixel-7 special case) all match. Only the VERTICAL FF update discipline (M5) diverges.
- **Sprite render core (PAL): near bit-exact.** The 8-pixel trigger loop, sbuf 24-bit shift register, expx/mc flops, hires/multicolor bit fetch, lowest-numbered-opaque priority winner, COL_D025/26/27 symbolic resolution via Cregs, both collision accumulators, DMA halt/activate at pixels 2/3/7, and MC-bit update at pixel 6 (8565) vs 7 (6569) all match. Only collision-clear ordering (M10) and PAL-hardcoded cycle tables (M11) diverge.
- **Register R/W masking surface: near bit-exact.** Unused-bit read masks, $D019 acknowledge, $D012 unchanged-value early-out, $D011 9-bit compare recompute, deferred $D01E/$D01F clear, sprite-X LSB/MSB recombination, and Peek OR-in all match. Only M13 ($D017), L2 (last_bus_phi2), the immediate-cregs colour write, and read-only $D013/$D014 write-through remain.
- **Model scalar constants: clean.** All seven model classes reproduce cycles-per-line, raster lines, color_latency, lightpen_old_irq_mode, and CPU clocks exactly; grey-dot/color-latency path selection is correct per model. Only the sprite cycle machinery and palette are not model-parameterized.

The dominant fidelity story: the managed VIC-II is a high-fidelity PAL 6569R3 port whose one structural gap (the missing `cycle_flags_pipe` / `dbuf_offset` draw-pipeline timing plus the `prefetch_cycles` counter) accounts for the visible boot-display defect, with the remaining medium/low items concentrated in NTSC-model timing, mid-line register-write raster tricks, and cosmetic palette generation.
---

## Addendum (2026-07-07): two additional confirmed divergences found during Phase 1+2 remediation

The full-frame boot oracle test (TEST-VIC-BOOTSTART-04) exposed two visible-window geometry drifts the area audits did not cover (the render window mapping was outside every area's scope):

#### H4. Visible window starts one raster line high (frame row 0 = line 15, VICE = line 16)
- **Managed:** `VideoRenderer.PalFirstVisibleRasterLine` was 15.
- **VICE:** `VICII_PAL_NORMAL_FIRST_DISPLAYED_LINE = 0x10` (16), vicii-timing.h:68, applied via vicii-timing.c:131; the frame oracle window starts at `vicii.first_displayed_line` (vice-shim.c:1515).
- **Impact:** the whole managed frame sat one line low vs VICE's visible canvas.
- **Fix (applied):** constant changed to 16.

#### H5. Visible window starts one raster cycle left (frame x 0 = dbuf[96], VICE = dbuf[104])
- **Managed:** `VideoRenderer.FirstVisibleRasterX` was 12 (read base 96).
- **VICE:** the canvas line is copied from `vicii.dbuf[DBUF_OFFSET]` with `DBUF_OFFSET = 17*8 - vicii.screen_leftborderwidth` (vicii-draw.c:71,91) and PAL normal `screen_leftborderwidth = 0x20` (vicii-timing.h:31): 136 - 32 = 104 = 8*13.
- **Impact:** the whole managed frame sat 8px right of VICE's visible canvas (one of the two extra left-border cycles in the boot symptom).
- **Fix (applied):** constant changed to 13.

#### M6 correction: REFUTED BY THE LOCKSTEP ORACLE (do not apply)
The audit fix for M6 (reset both VIC banks to 0 citing vicii.c:354-355) was implemented and then REVERTED: with _vicBank = 0 the CPU stream diverges from native x64sc within 200 cycles (LockstepValidationTests.First10000CyclesMatch), because managed bank 0 enables the char-ROM window at VIC $1000-$1FFF while VICE fetches raw RAM in its pre-CIA2 init state. The managed 3/0 boot seed is observationally equivalent to native (the C64 RAM init pattern repeats every $4000, so base $C000 and base $0000 fetch identical bytes) and the KERNAL programs the real bank via $DD00 before the display enables. Keep 3/0.

Phase 1+2 outcome (H1, H2, H3, M5, H4, H5 applied; M6 refuted and kept as-is), verified by TEST-VIC-BOOTSTART-01..04: the managed boot frame is bit-exact vs the x64sc oracle (0 of 104448 pixels diverge), and CPU lockstep vs native x64sc stays green for the first 100000 cycles.

#### H6 (2026-07-07, found during Phase 5): sprite draw beam position +8px vs VICE on every model
- **Managed:** `PixelSequencer.DrawSprites8` derived xpos as the Phi2 xpos of the piped cycle ((0x198 + 8*pipe) % 0x1F8), equal to the pre-audit formula's value.
- **VICE:** the merged cycle-table xpos stores the PHI1 xpos floored to 8 (vicii-chip-model.c:767, entry |= (xpos_phi[0] >> 3) << XPOS_B; read back by cycle_get_xpos, vicii-chip-model.h:164-167), so draw_sprites8(cycle_flags_pipe) compares sprite X against ((0x194 + 8*(N-1)) % 0x1F8) & ~7 during the draw of cycle N.
- **Impact:** every sprite rendered 8px (one cycle) left of VICE and of real hardware; sprite X=$18 (24) failed to line up with the CSEL=1 display edge. The V6 unit tests encoded the shifted convention, masking it.
- **Fix (applied):** xpos = ((0x194 + 8*flagsRasterX) % 0x1F8) & ~7; anchored by TEST-VIC-SPRXPOS-01 (sprite X=24 triggers during the cycle-17 draw, the first display batch). Sprite suites re-anchored: trigger geometry moves one cycle later, and the Phi1 test stubs now serve display columns on the true FetchG cycles 15-54.
