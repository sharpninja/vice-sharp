# VIC-20 vs VICE (xvic) Full Implementation Audit

**Date:** 2026-08-08 (re-baselined)  
**Canonical tree:** `native/vice/vice/src/vic20/` (+ `core/viacore.c`, `data/VIC20/`)  
**Managed tree:** `src/ViceSharp.Chips/{Vic,Cpu,IEC}`, `src/ViceSharp.Core/Vic20`, `ArchitectureBuilder.BuildVic20Machine`  

**Standard of truth:** VICE source logic only. Screenshots and "looks close" are not acceptance.  
**Statuses:** Exact | Partial | Stub | Missing | Invented  

**Process note:** All VIC-20 / VICE-faithfulness work uses a **hostile validator** (independent read-only agent; assumes overclaim; Exact requires VICE file+function + matching managed control flow).

**Video lockstep:** Native `vice_vic20_get_video_state` + managed `CaptureVideoLockstepState`. Tests: `Vic20VideoLockstep` (2k + 500k). CPU probe also compares video unless `VICESHARP_LOCKSTEP_VIDEO=0`. Pixel capture: xvic `capture_visible_frame` + indices wired; index SequenceEqual lockstep in progress (FR-VIC20-001 / PLAN-VIC20-EXACT-001 Phase A).

---

## Hostile claim scope (what may be Exact)

The tree does **not** claim whole-machine Exact-same-as-VICE. Exact is allowed only for the rules/subsystems listed in the matrix below as **Exact**. Everything else is Partial, Stub, or Explicit Missing.

---

## Chip-by-chip matrix

### CPU

- **VICE:** `vic20cpu.c` `CLK_INC` + maincpu  
- **Managed:** `Mos6502` + `SystemClock` Phi2 (CPU before VIC)  
- **Status:** **Partial**  
- **Why not Exact:** Shared 6502; lockstep-hardened slices only.

### Bus / open-bus

- **VICE:** `vic20bus.c`, `vic20memrom.c`  
- **Managed:** `BasicBus` last-data + V-bus latch  
- **Status:** **Partial** (selected rules Exact below)  
- **Exact rules:** BASIC/KERNAL ROM reads do not refresh last-data; chargen does.  
- **Not Exact:** Full C-bus / V-bus matrix for all unconnected regions.

### Memory map

- **VICE:** `vic20mem.c`, `vic20io.c`  
- **Managed:** `Vic20SystemRam` + `RomDevice` + overlays  
- **Status:** **Partial**  
- **Missing:** Full `mem_conf`, VFLI, complete cart banking matrix.

### Color RAM

- **VICE:** `colorram_read` / `colorram_store` / `colorram_peek` in `vic20mem.c`  
- **Managed:** `Vic20ColorRam.cs`  
- **Status:** **Exact** for those three rules (4-bit store; read OR `VBusLastData & 0xF0`; peek no side effect)  
- **Missing:** VFLI color banking path.

### VIC-I cycle engine

- **VICE:** `vic-cycle.c`  
- **Managed:** `Mos6561.Tick`  
- **Status:** **Partial**  
- **Exact-ish / present:** open_v/h, START delay, matrix/chargen into cbuf/gbuf, memptr/row, `LatchColumns` pending-only (live `text_cols` only on open_h/start_fetch; test `LatchColumns_DoesNotEagerlySetLiveTextCols`), `display_xstop` from start_fetch.  
- **Missing / diverge:** Interlace, light pen, full `vic_cycle_do_fetch` map (VFLI/unconnected), idle `no_fetch` bus refresh.

### VIC-I draw + present

- **VICE:** `vic-draw.c` + full line buffer; `raster_line_draw_borders`; host `first_x` via `video_viewport_resize` / `video_canvas_refresh`  
- **Managed:** full-line buffer; glyph paint all slots (no invent paper strip); border blank left of `display_xstart` / right of `display_xstop`; crop `ComputeViewportFirstX`  
- **Status:**  
  - **Exact** for normal READY present window (PAL first_x=48, L+R borders, paper origin 48): tests `ReadyPal_NormalBorderWindow_*`, `ViewportFirstX_ReadyPal_*`, `SpaceGlyph_PaintsBackgroundPaper_*`  
  - **Partial** overall draw (no half_char / old_mc mid-line path, no raster cache)  
  - **Pixel FB capture:** xvic `capture_visible_frame` wired (first_x crop + palette BGRA); geometry match tests green; full bit-exact SequenceEqual still open  
- **Not Invented:** crop is not `src=0`; paper is not a continuous invent strip.

### VIC-I mem / regs

- **VICE:** `vic-mem.c`  
- **Managed:** reg array; height from reg3 bit0; raster encode on read 3/4  
- **Status:** **Partial**  
- **Present:** Sound register side effects and live batched sample delivery; focused silence and tone batches are byte-identical to native xvic. **Missing:** lightpen/pot and mid-line `raster_changes_*`; the full waveform/input space is not certified.

### VIC-I sound

- **VICE:** `vic20sound.c`  
- **Managed:** `Mos6561` clocks `Vic20Sound`, exposes `IAudioChip`, and publishes PCM through the configured host backend  
- **Status:** **Partial** — live `$900A-$900E` tone/noise/volume effects and backend registration are tested; native bit-exact waveform lockstep is not yet certified

### Palette

- **VICE:** `data/VIC20/PALette.vpl` + `vic-color.c` YUV/CRT  
- **Managed:** static RGB `Mos6561.Palette`  
- **Status:** **Exact** for Tobias 6561-101 RGB bytes; **Partial** overall (no YUV/CRT)

### Geometry / timing

- **VICE:** `vic-timing.h/.c` normal/full/debug/none  
- **Managed:** normal-only constants  
- **Status:** **Exact** for normal PAL/NTSC width/first/last/leftborder; **Partial** (other border modes Missing)

### PAR

- **VICE:** `vic_get_pixel_aspect`  
- **Managed:** `GetPixelAspectRatio`  
- **Status:** **Exact** (1.66574035/2 and 1.50411479/2)

### VIA core

- **VICE:** `core/viacore.c`  
- **Managed:** `Via6522.cs`  
- **Status:** **Partial** (SR/handshake incomplete)

### VIA1 / VIA2 board

- **Status:** **Partial** (roles Exact for NMI/IRQ bases; tape/userport/live IEC pins Missing)

### Keyboard / joystick

- **Status:** **Partial**

### IEC serial

- **Status:** **Exact** for idle PA levels only; live bus **Missing**

### Datasette / IEEE / rsuser / printer

- **Status:** **Missing** (Explicit)

### Cartridges

- **Status:** **Partial** (FE3 MODE_FLASH via managed flash040 TYPE_B command FSM; Ultimem/MegaCart MVP). FE3 erase latency now advances on the machine clock, and dirty FE3/Ultimem images plus MegaCart NVRAM persist atomically on detach. Full VICE cart suite is still **Missing**; no whole-cart Exact claim.

### Snapshots

- **Status:** **Missing** (Explicit)

### Machine glue

- **Status:** **Partial**

---

## Invent list (closed vs remaining)

| # | Item | Status |
|---|------|--------|
| 1 | Pre-cropped FB + canvas X = display_xstart (`src=0`) | **Closed** — full line + `ComputeViewportFirstX` |
| 2 | Continuous invent paper strip | **Closed** — space cells via draw slots (slot 0 bg) |
| 3 | Eager live column latch | **Closed** — pending only until open_h |
| 4 | Idle IEC as full board serial | **Remaining** if overclaimed; matrix says idle Exact only |
| 5 | Cart MVP as VICE ports | **Remaining** Stub — Explicit not Exact |
| 6 | Sound store-only as sound | **Closed** — VIC-I is a clocked `IAudioChip` with live PCM delivery; bit-exact native audio remains Partial |
| 7 | VicIStub alternate surface | Check tree; non-product if unused |
| 8 | `RenderNow` harness driver | Allowed harness-only; not a VICE API claim |
| 9 | Via6522 incomplete SR modes | **Remaining** Partial |

---

## Exact rule labels (not machine Exact)

| Rule | VICE citation | Managed |
|------|---------------|---------|
| `VIC_PIXEL_WIDTH=2` | `victypes.h` | `Mos6561.PixelWidth` |
| Normal border sizes | `vic-timing.h` | constants + EnsureFrameBufferSize |
| READY first_x window | `video-viewport.c` | `ComputeViewportFirstX` + crop |
| Color base `$9600`/`$9400` | `vic.c` | `ResolveColorBase` |
| Color nybble open-bus | `colorram_*` | `Vic20ColorRam` |
| Idle IEC PA `$7E` | `vic20iec` idle | `Vic20IecPort` |
| PAR formulas | `vic_get_pixel_aspect` | `GetPixelAspectRatio` |
| PALette.vpl RGB | `data/VIC20/PALette.vpl` | static `Palette[]` |
| Raster encode reg3/4 | `vic-mem` | `Read` |
| Latch pending cols only | `vic_cycle_latch_columns` | `LatchColumns` |

---

## Ordered remaining work toward broader Exact

1. half_char / mid-line color path (`vic-draw.c`)  
2. Full fetch map + interlace/lightpen  
3. Native bit-exact VIC-I audio waveform lockstep against `vic20sound.c`  
4. Live IEC + VIA board pins  
5. Carts port or permanent non-parity label  
6. Pixel FB lockstep when native export exists  

Every new Exact: Byrd tests-first, VICE citation, hostile validator.

---

## Audit conclusion

**Whole-machine "Exact same as VICE" is not claimed.**  

Exact labels above are scoped rules only. VIC-I silence/tone batches match native xvic, but sound remains Partial until the full waveform/input space is certified; live IEC, niche carts, tape, native snapshot writes, full mid-line drawing, and full-canvas BGRA parity remain Explicit Missing/Partial.

Policy: no new invent; Exact requires VICE file+function + matching control flow; video geometry proven by pixel/band tests on shipped `FrameBuffer`.

---

## 2026-08-11 bit-exact program (PLAN-VIC20-EXACT-001)

Phased gates landed this session (receipts under docs/receipts/vic20-phase-*):

- **Pixel FB (Phase A):** index SequenceEqual READY PAL/NTSC/busy; BGRA via shared PALette after index. Capture first_x recompute + NTSC row clamp in vice-shim-vic20.c.
- **Video residual (Phase B):** PAL+NTSC video lockstep 2k/500k; vic.regs clear on reset (NativeVice residue).
- **Carts (Phase C):** inventory matrix; FE3 flash040 TYPE_B erase latency (50 / 1e6 / 8e6); Ultimem/Mega unit parity.
- **Sound (Phase D):** Vic20Sound port of vic20sound.c; native vice_vic20_render_samples; silence+tone SequenceEqual vs xvic.
- **Workload/bus (Phase E):** focused non-idle CPU lockstep PAL+NTSC (250k); keyboard matrix tests green.
- **Snapshots (Phase F):** inventory + managed RAM sample round-trip; headless xvic WriteSnapshot hang = Explicit Missing residual.
- **Whole-machine claim:** still scoped Exact per matrix; IEEE/rsuser/printer Explicit Missing; cart niche types Explicit Missing.


## Phase G closeout 2026-08-11

PLAN-VIC20-EXACT-001 **done** (scoped Exact). Umbrella process-isolated **46/0/0** + 2s PAL/NTSC workload green. Hostile AGREE on scoped claims (docs/receipts/hostile-validator-20260811T203852Z.md). Whole-machine Exact **not** claimed; residuals: niche carts, IEEE/rsuser/printer, WriteSnapshot hang, NativeVice process isolation.
