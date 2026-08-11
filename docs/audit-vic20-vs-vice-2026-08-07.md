# VIC-20 vs VICE (xvic) Full Implementation Audit

**Date:** 2026-08-08 (re-baselined)  
**Canonical tree:** `native/vice/vice/src/vic20/` (+ `core/viacore.c`, `data/VIC20/`)  
**Managed tree:** `src/ViceSharp.Chips/{Vic,Cpu,IEC}`, `src/ViceSharp.Core/Vic20`, `ArchitectureBuilder.BuildVic20Machine`  

**Standard of truth:** VICE source logic only. Screenshots and "looks close" are not acceptance.  
**Statuses:** Exact | Partial | Stub | Missing | Invented  

**Process note:** All VIC-20 / VICE-faithfulness work uses a **hostile validator** (independent read-only agent; assumes overclaim; Exact requires VICE file+function + matching managed control flow).

**Video lockstep:** Native `vice_vic20_get_video_state` + managed `CaptureVideoLockstepState`. Tests: `Vic20VideoLockstep` (2k + 500k). CPU probe also compares video unless `VICESHARP_LOCKSTEP_VIDEO=0`. Pixel framebuffer compare vs xvic still **Missing** (`capture_visible_frame` stub).

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
- **Missing:** Sound store side effects (Explicit Missing), lightpen/pot, mid-line `raster_changes_*`.

### VIC-I sound

- **VICE:** `vic20sound.c`  
- **Managed:** `$900A-$900E` register store only  
- **Status:** **Missing** (Explicit; no Exact claim)

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

- **Status:** **Partial** (FE3 MODE_FLASH via managed flash040 TYPE_B command FSM; Ultimem/MegaCart MVP). Full VICE cart suite still **Missing**. FE3 erase latency instant (Partial). No whole-cart Exact claim.

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
| 6 | Sound store-only as sound | **Remaining** Missing — Explicit |
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
3. Sound port `vic20sound.c`  
4. Live IEC + VIA board pins  
5. Carts port or permanent non-parity label  
6. Pixel FB lockstep when native export exists  

Every new Exact: Byrd tests-first, VICE citation, hostile validator.

---

## Audit conclusion

**Whole-machine "Exact same as VICE" is not claimed.**  

Exact labels above are scoped rules only. Sound, live IEC, carts, tape, snapshots, full draw mid-line, and pixel FB lockstep are Explicit Missing/Partial.

Policy: no new invent; Exact requires VICE file+function + matching control flow; video geometry proven by pixel/band tests on shipped `FrameBuffer`.
