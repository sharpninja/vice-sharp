# FR-Video-VIC-II: VIC-II Video Chip Functional Requirements

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Subsystem      | Video (MOS 6569 VIC-II)        |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-04-13                     |

---

## FR-VIC-001: Raster Engine and Timing

**ID:** FR-VIC-001
**Title:** Raster Engine with PAL/NTSC Timing
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The VIC-II raster engine shall generate video output with cycle-accurate timing for both PAL (6569, 312 lines, 63 cycles/line) and NTSC (6567, 263 lines, 65 cycles/line) variants. The raster counter, raster interrupt, and display/idle state transitions shall match real hardware timing.

### Acceptance Criteria

1. PAL variant generates 312 raster lines with 63 CPU cycles per line (504 pixels per line).
2. NTSC variant generates 263 raster lines with 65 CPU cycles per line (520 pixels per line).
3. The raster counter ($D011 bit 7 + $D012) increments once per raster line.
4. A raster interrupt fires when the raster counter matches the value set in $D011/$D012.
5. The raster interrupt triggers at cycle 0 of the matching line (PAL) with a 1-cycle acknowledge latency.
6. The display window begins at line 51 (PAL) and ends at line 250.
7. Display/idle state transitions occur at the correct cycles within each line.

### Traceability

- **Interfaces:** `IVideoChip`, `IClockedDevice`
- **Test Suite:** `RasterTimingTests`, `PalNtscVariantTests`, `RasterInterruptTests`

---

## FR-VIC-002: Character Display Modes

**ID:** FR-VIC-002
**Title:** Character Display Modes (Standard, Multicolor, ECM)
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The VIC-II shall support all three character display modes: Standard Character Mode, Multicolor Character Mode, and Extended Color Mode (ECM). Character data is fetched from the screen matrix and character generator according to the configured VIC bank and pointers.

### Acceptance Criteria

1. Standard mode displays 40x25 characters using 8x8 pixel character cells with 2 colors (background + foreground from Color RAM).
2. Multicolor character mode (MCM bit set) displays 4x8 double-wide pixel characters with up to 4 colors per cell when color nybble bit 3 is set.
3. Extended Color Mode (ECM bit set) allows 4 background colors selected by the upper 2 bits of the screen code, limiting the character set to 64 characters.
4. ECM + MCM combination is invalid and produces a black screen (all outputs zero).
5. Screen matrix base and character generator base are controlled by $D018.
6. Character data fetch timing (c-access and g-access) occurs at the correct cycles per line.

### Traceability

- **Interfaces:** `IVideoChip`
- **Test Suite:** `StandardCharModeTests`, `MulticolorCharModeTests`, `EcmModeTests`, `InvalidModeTests`

---

## FR-VIC-003: Bitmap Display Modes

**ID:** FR-VIC-003
**Title:** Bitmap Display Modes (Standard, Multicolor)
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The VIC-II shall support Standard Bitmap Mode and Multicolor Bitmap Mode. In bitmap modes, the display is a 320x200 (standard) or 160x200 (multicolor, double-wide pixels) bitmap with color data from the screen matrix and Color RAM.

### Acceptance Criteria

1. Standard bitmap mode (BMM=1, MCM=0) displays 320x200 pixels; each 8x8 cell has a foreground/background pair from the screen matrix byte.
2. Multicolor bitmap mode (BMM=1, MCM=1) displays 160x200 double-wide pixels; each 4x8 cell uses up to 4 colors from screen matrix, Color RAM, and background register.
3. The bitmap base address is selected by bit 3 of $D018 (either $0000 or $2000 relative to VIC bank).
4. BMM + ECM combination is invalid and produces a black screen.
5. BMM + MCM + ECM combination is invalid and produces a black screen.
6. Bitmap data fetch timing matches hardware (g-access reads bitmap data instead of character generator).

### Traceability

- **Interfaces:** `IVideoChip`
- **Test Suite:** `StandardBitmapModeTests`, `MulticolorBitmapModeTests`, `InvalidModeCombinationTests`

---

## FR-VIC-004: Sprite Engine

**ID:** FR-VIC-004
**Title:** Sprite Engine (8 Hardware Sprites)
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The VIC-II shall emulate all 8 hardware sprites with correct positioning, priority, expansion, and multicolor capabilities. Each sprite is 24x21 pixels (standard) or 12x21 double-wide pixels (multicolor) and can be independently expanded 2x horizontally and/or vertically.

### Acceptance Criteria

1. Eight sprites (0-7) are independently positionable via $D000-$D010.
2. Sprite enable register ($D015) controls which sprites are displayed.
3. Sprite X-expansion ($D01D) doubles horizontal size; Y-expansion ($D017) doubles vertical size.
4. Multicolor mode ($D01C) per sprite uses 3 shared colors + 1 individual color.
5. Sprite data pointers are read from screen matrix + $03F8-$03FF (relative to VIC bank).
6. Sprite-to-background priority is controlled by $D01B per sprite.
7. Sprite display priority follows the rule: lower-numbered sprites appear in front of higher-numbered sprites.

### Traceability

- **Interfaces:** `IVideoChip`, `ISpriteUnit`
- **Test Suite:** `SpriteDisplayTests`, `SpriteExpansionTests`, `SpriteMulticolorTests`, `SpritePriorityTests`

---

## FR-VIC-005: Sprite Collision Detection

**ID:** FR-VIC-005
**Title:** Sprite Collision Detection
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The VIC-II shall detect sprite-to-sprite and sprite-to-background collisions and report them through the collision registers and optional interrupt generation.

### Acceptance Criteria

1. Sprite-sprite collision register ($D01E) sets a bit for each sprite involved in a collision with another sprite.
2. Sprite-background collision register ($D01F) sets a bit for each sprite that overlaps non-background pixels.
3. Collision registers are latched -- they retain set bits until read, at which point they are cleared.
4. Reading the collision register clears it atomically.
5. Collision interrupt (if enabled in $D01A) fires when a new collision is detected.
6. In multicolor mode, the "transparent" color (bit pattern %00) does not trigger collisions.
7. Expanded sprites use the expanded pixel area for collision detection.

### Traceability

- **Interfaces:** `IVideoChip`, `ISpriteUnit`
- **Test Suite:** `SpriteCollisionTests`, `CollisionRegisterLatchTests`, `CollisionInterruptTests`

---

## FR-VIC-006: Badline Handling and DMA Stealing

**ID:** FR-VIC-006
**Title:** Badline Handling and CPU DMA Cycle Stealing
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

A "badline" occurs when the lower 3 bits of the raster counter match the Y-scroll value ($D011 bits 0-2) and the display is enabled. During a badline, the VIC-II steals 40 CPU cycles (plus 3 setup cycles) to fetch character pointers from the screen matrix (c-access). The CPU is halted during these stolen cycles.

### Acceptance Criteria

1. Badline condition triggers when (raster_line & 7) == (d011 & 7) and display is enabled, within the display window (lines 48-247).
2. During a badline, the VIC-II performs 40 c-accesses to read the screen matrix.
3. The CPU is halted for 40-43 cycles during a badline (3 setup cycles + 40 character fetch cycles).
4. Sprite DMA stealing occurs independently: each enabled sprite steals 2 cycles per line (p-access + s-access pattern).
5. Sprite and badline DMA can overlap, with sprite DMA taking priority.
6. The cycle at which the CPU is first halted for a badline is deterministic and occurs at cycle 15 of the raster line.

### Traceability

- **Interfaces:** `IVideoChip`, `IClockedDevice`
- **Test Suite:** `BadlineDetectionTests`, `DmaStealingTimingTests`, `CpuHaltTests`

---

## FR-VIC-007: Border Behavior and Open Borders

**ID:** FR-VIC-007
**Title:** Border Behavior Including Open Border Tricks
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The VIC-II border unit shall accurately emulate both the main border (top/bottom) and side borders (left/right), including the ability to "open" borders using well-known VIC tricks (toggling the RSEL/CSEL bits at specific raster positions).

### Acceptance Criteria

1. With RSEL=1 (25 rows), the upper border ends at line 51 and lower border begins at line 251.
2. With RSEL=0 (24 rows), the upper border ends at line 55 and lower border begins at line 247.
3. With CSEL=1 (40 columns), side borders are at pixels 24-343.
4. With CSEL=0 (38 columns), side borders are at pixels 31-334.
5. Opening the top/bottom border: if RSEL is cleared before the border comparison line and set after, the vertical border flip-flop is not set, allowing sprites to display in the border area.
6. Opening the side borders: toggling CSEL at the correct cycle prevents the horizontal border flip-flop from being set.
7. Border color is set by $D020; background color by $D021.

### Traceability

- **Interfaces:** `IVideoChip`
- **Test Suite:** `BorderBehaviorTests`, `OpenBorderTrickTests`, `RselCselTimingTests`

---

## FR-VIC-008: FLI / AFLI Support

**ID:** FR-VIC-008
**Title:** Flexible Line Interpretation (FLI) Support
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The emulator shall support Flexible Line Interpretation (FLI) and Advanced FLI (AFLI) techniques, which exploit badline forcing to change the character pointer base every raster line, enabling more than the normal 8 unique character colors per 8-pixel-tall cell.

### Acceptance Criteria

1. Forcing a badline every line (by changing Y-scroll each line) triggers a new c-access fetch each raster line.
2. The 3-cycle "FLI bug" (gray pixels at the left of each line due to the VIC-II not having fetched character data yet) is accurately reproduced.
3. Changing $D018 (VIC memory pointers) during specific cycles of a raster line takes effect for subsequent fetches on that line.
4. AFLI mode (combining FLI with bitmap mode) is supported.
5. The CPU cycle-accurate timing of the bank switch and pointer changes matches VICE x64sc behavior.

### Traceability

- **Interfaces:** `IVideoChip`
- **Test Suite:** `FliTests`, `FliBugTests`, `AfliTests`

---

## FR-VIC-009: VIC-II Bank Switching

**ID:** FR-VIC-009
**Title:** VIC-II Bank Switching (See also FR-MEM-004)
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The VIC-II address generation shall correctly translate 14-bit VIC addresses (0-$3FFF) into the 16-bit system address space by prepending the bank selection bits from CIA2 Port A. Character ROM overlay detection shall occur within the VIC address translation.

### Acceptance Criteria

1. The VIC-II generates 14-bit addresses ($0000-$3FFF) that are offset by the bank base.
2. Character ROM overlay at VIC addresses $1000-$1FFF is active only in banks 0 and 2.
3. When the bank changes mid-frame, subsequent VIC-II fetches use the new bank immediately.
4. Sprite data fetches respect the active bank.
5. Bitmap data fetches respect the active bank.

### Traceability

- **Interfaces:** `IVideoChip`, `IVicBankSelector`
- **Test Suite:** `VicAddressTranslationTests`, `MidFrameBankSwitchTests`

---

## FR-VIC-010: Sprite Multiplexing DMA Timing

**ID:** FR-VIC-010
**Title:** Sprite Multiplexing DMA Timing
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The VIC-II sprite DMA shall be emulated with sub-cycle accuracy to support sprite multiplexing techniques. Each sprite's DMA pattern (p-access for pointer, s-access for data) occurs at specific cycles on each raster line. Software sprite multiplexers rely on exact knowledge of when these cycles occur.

### Acceptance Criteria

1. Each sprite's p-access (pointer fetch) occurs at a specific cycle position on the raster line, with sprite 0 earliest and sprite 7 latest.
2. Each sprite's three s-accesses (data fetch, 3 bytes) occur at consecutive cycles following the p-access.
3. The CPU is halted during sprite DMA cycles (2 cycles per sprite for setup, 3 for data).
4. Disabling a sprite (clearing its bit in $D015) at the correct cycle prevents its DMA from occurring.
5. Re-enabling a sprite and setting its Y-position to match the current raster line triggers DMA on the next line.
6. The exact cycle positions for each sprite's DMA match the VICE x64sc reference.

### Traceability

- **Interfaces:** `IVideoChip`, `ISpriteUnit`
- **Test Suite:** `SpriteDmaTimingTests`, `SpriteMultiplexTests`, `SpriteDmaCycleTests`
