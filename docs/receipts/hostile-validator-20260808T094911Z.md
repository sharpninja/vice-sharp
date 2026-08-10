# Hostile Validator Receipt

- **TimestampUtc:** 20260808T094911Z
- **ValidatorIdentity:** GrokSubagentHostile
- **Workspace:** `F:\GitHub\vice-sharp`
- **Role:** Adversarial re-verification of implementer claims (no product code changes)

## Claims reviewed

1. Mos6561 normal READY present geometry matches VICE `video_viewport` `first_x` (READY PAL: first_x=48, left border [0..47], paper origin 48, right border [400..447]); proven by tests driving shipped `FrameBuffer` after `RenderNow`.
2. Crop is `ComputeViewportFirstX` (not invent `src=0` full-line x=0 copy).
3. No continuous invent paper strip; space cells paint background via draw slots (VICE `drawing_table` style).
4. `LatchColumns` only updates `pending_text_cols`; live `text_cols` not set until `open_h`/`start_fetch` (test `LatchColumns_DoesNotEagerlySetLiveTextCols`).
5. `docs/audit-vic20-vs-vice-2026-08-07.md` does not claim whole-machine Exact; Exact rules are scoped and honest; sound/carts/live IEC/tape/snapshots Explicit Missing or Stub without Exact marketing.
6. Vic20 video + lockstep focused tests are green (0 failed) when re-run with the stated filter.
7. Product VIC-20 path uses `Mos6561` not `VicIStub` (`ArchitectureBuilder`).
8. Closed invent list items (`src=0` crop, paper strip, eager latch) are truly closed in live code.

## Per-claim verdicts

### Claim 1 — PASS

**Evidence:**

- VICE `native/vice/vice/src/video/video-viewport.c` `video_viewport_resize`: symmetric-small-border branch yields  
  `first_x = gfx_x - (canvas_w - gfx_w) / 2` when `gfx_w + small_x_border*2 > width`.
- READY PAL inputs: gfx_x=96, gfx_w=352, canvas=448, screen=568 → right=120, small=96, 352+192=544>448 → first_x=96-48=**48**.
- Managed `Mos6561.ComputeViewportFirstX` (lines 91-126) mirrors that control flow (plus an extra `firstX+canvasW>screenW` clamp that is **inert** for READY: 48+448=496 < 568).
- Crop present path uses that first_x on the shipped line buffer (lines 663-671).
- Tests (re-run green):
  - `ViewportFirstX_ReadyPal_Is48_MatchingViceVideoViewport`
  - `ReadyPal_NormalBorderWindow_HasLeftAndRightBorder_ViceFirstX` (FrameBuffer after `RenderNow`: !white at 0/47, white at 48, !white at 400/447; width 448)
  - Supporting: `BorderPixels_MatchBorderColor_NotBackground`, `BorderCyan_UsesVicePalettesVplRgb`, `ReadyStyle_PaperRegion_IsWhiteBand_NotShortCyanIsland`
- `RenderNow` (741-784) re-arms cycle state and drives `Tick()` through `DrawRasterLine` (not a separate invent grid).

**Caveat (does not fail claim as stated):** pixel framebuffer lockstep vs xvic remains Missing per audit; claim is geometry bands / first_x, not full FB parity.

### Claim 2 — PASS

**Evidence:**

- Live crop in `DrawRasterLine`:

```663:671:src/ViceSharp.Chips/Vic/Mos6561.cs
        var gfxX = ResolveGfxXForViewport();
        var gfxW = ResolveGfxWForViewport();
        var firstX = ComputeViewportFirstX(gfxX, gfxW, _frameWidth, lineW, gfxAreaMoves: true);
        var copyW = Math.Min(_frameWidth, Math.Max(0, lineW - firstX));
        if (copyW <= 0)
            return;
        var src = firstX * 4;
        var dst = fbY * _frameWidth * 4;
        Buffer.BlockCopy(line, src, _frameBuffer, dst, copyW * 4);
```

- Grep of `Mos6561.cs`: only `BlockCopy` uses `src = firstX * 4`; no invent full-line `src=0` crop for the present window.
- VICE host uses `viewport->first_x` as draw-buffer X origin in `video-canvas.c` (`video_canvas_refresh_all`).

### Claim 3 — PASS

**Evidence:**

- Non-blank lines paint cells via 8 bit slots with `slot 0 => bg` (std) / multicolor table (lines 590-636), matching VICE `vic-draw.c` `drawing_table` / `PUT_PIXEL` (transparent=0 still writes bg when t is false for non-transparent path; managed always writes the slot color including 0=bg).
- Initial line fill is **border**, not paper; paper appears only where glyph slots paint background.
- No continuous paper-rectangle invent found in `Mos6561.cs` (grep paper/invent/FillSolid: only comments + border `FillSolid` at RenderNow start).
- Test `SpaceGlyph_PaintsBackgroundPaper_WithoutInventStrip` passed on re-run.

### Claim 4 — PASS

**Evidence:**

- VICE `vic-cycle.c` `vic_cycle_latch_columns`: only `pending_text_cols = MIN(regs[2]&0x7f, max)`; live `text_cols` set in `vic_cycle_open_h` / `vic_cycle_start_fetch`.
- Managed `LatchColumns` (472-479): only `_pendingTextCols` (and public `_columns` display helper when pending>0); does **not** assign `_textCols`.
- Managed `OpenH` / `StartFetch` assign `_textCols = _pendingTextCols` (394-401).
- `CaptureVideoLockstepState.TextCols` is `_textCols` (277).
- Test `LatchColumns_DoesNotEagerlySetLiveTextCols` (origin 99 never opens): Assert TextCols==0 — **Passed** on re-run.

**Note:** `ConfigureTiming` still seeds `_textCols` for profile setup; product `machine.Reset()` and `RenderNow` zero live cols until open. Claim is about latch vs open, which holds.

### Claim 5 — PASS

**Evidence (file read):** `docs/audit-vic20-vs-vice-2026-08-07.md`

- Explicit: whole-machine Exact **not** claimed (lines 16-18, 185-187).
- Sound: **Missing** Explicit (77-81).
- Carts: **Stub** / Missing, no Exact (123-125).
- Live IEC: idle Exact only; live **Missing** (115-117).
- Tape/datasette: **Missing** (119-121).
- Snapshots: **Missing** (127-129).
- Pixel FB lockstep vs xvic: **Missing** (line 12).
- Exact labels scoped to named rules (color RAM store/read/peek rules, READY first_x window, normal geometry constants, etc.).

### Claim 6 — PASS

**Command (validator re-run, not implementer receipt):**

```text
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20VideoTests|FullyQualifiedName~Vic20VideoLockstep"
```

**Result:**

```text
Passed!  - Failed:     0, Passed:    22, Skipped:     0, Total:    22, Duration: 5 s
EXIT=0
```

Includes lockstep:

- `Vic20VideoLockstep.EveryCycle_VicI_VideoState_Match_2k`
- `Vic20VideoLockstep.EveryCycle_VicI_VideoState_Match_FocusedWindow`

and geometry/latch/paper tests listed under claim 1-4. **Skipped: 0** (skipped are not passes; none present).

### Claim 7 — PASS

**Evidence:**

- `ArchitectureBuilder.BuildVic20Machine` constructs `var vic = new Mos6561(irqLine)` and registers `DeviceRole.VideoChip` (`ArchitectureBuilder.cs` ~375-402).
- Repo grep `new VicIStub` / product construction: **no** product `new VicIStub`; only type definition in `src/ViceSharp.Chips/Vic/VicIStub.cs`.
- Tests assert `Assert.IsType<Mos6561>(...VideoChip)`.

**Residual (not a product-path FAIL):** `VicIStub` type remains in tree (audit invent list #7 still "Check tree"); claim is product path uses Mos6561, which is true.

### Claim 8 — PASS

Closed invent items verified in **live** code:

| Invent | Status in live tree | Proof |
|--------|---------------------|--------|
| `src=0` crop | Closed | crop `src = firstX * 4` via `ComputeViewportFirstX` |
| Continuous paper strip | Closed | slot-based glyph paint; no strip fill |
| Eager live column latch | Closed | `LatchColumns` pending-only; test green |

## Explicit FAIL list

*(none)*

## Residuals / non-FAIL caveats

1. Managed `ComputeViewportFirstX` has an extra end clamp (`firstX + canvasW > screenW`) not present in the VICE `video_viewport_resize` body reviewed; inert for READY PAL.
2. Pixel FB compare vs xvic still Missing (audit honest); not claimed Exact machine-wide.
3. `VicIStub` class file still exists (unused by product builder).
4. Border band tests primarily use !IsWhite for edges; cyan border RGB is covered by separate palette/border tests that also passed.

## OverallVerdict

**AGREE**

All eight claims independently re-verified as **PASS**. No claim left FAIL or UNKNOWN.

## Twin JSON

`docs/receipts/hostile-validator-20260808T094911Z.json`
