# FR-Machine-Profiles: Machine Profile Functional Requirements

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Subsystem      | Machine Profiles               |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-04-13                     |

---

## FR-PRF-001: C64 (NMOS) Profile

**ID:** FR-PRF-001
**Title:** Commodore 64 (Original NMOS) Machine Profile
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The primary machine profile for the original Commodore 64 with NMOS chips: MOS 6510 CPU, MOS 6567 (NTSC) or MOS 6569 (PAL) VIC-II, MOS 6581 SID, two MOS 6526 CIAs. This is the baseline profile and the first target for full compatibility.

### Acceptance Criteria

1. CPU: MOS 6510 with all undocumented opcodes exhibiting NMOS behavior.
2. VIC-II: MOS 6569R3 (PAL) or MOS 6567R8 (NTSC) with correct timing per variant.
3. SID: MOS 6581 with characteristic analog filter curve and combined waveform behavior.
4. CIA: Two MOS 6526 with correct timer and TOD behavior.
5. RAM: 64KB main RAM + 1KB Color RAM.
6. ROM: BASIC V2 ($A000), KERNAL ($E000), Character Generator ($D000).
7. System clock: 985248 Hz (PAL) or 1022727 Hz (NTSC).
8. The profile is selectable via `IMachineProfile.LoadProfile("c64")`.
9. PAL/NTSC variant is configurable within the C64 profile.
10. Passes the VICE test suite for x64sc accuracy level.

### Traceability

- **Interfaces:** `IMachineProfile`
- **Test Suite:** `C64ProfileTests`, `C64PalNtscTests`, `ViceTestSuiteRunner`

---

## FR-PRF-002: C64C (New Revision) Profile

**ID:** FR-PRF-002
**Title:** Commodore 64C Machine Profile
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The C64C (cost-reduced revision) uses the MOS 8580 SID (which has a different filter and combined waveform behavior) and a revised VIC-II (MOS 8562/8565). The C64C profile configures the emulator with these variant chips.

### Acceptance Criteria

1. SID: MOS 8580 with linear filter curve and revised combined waveform tables.
2. VIC-II: MOS 8562 (NTSC) or MOS 8565 (PAL) -- functionally equivalent to earlier revisions but with different luminance levels.
3. All other components are identical to the C64 NMOS profile.
4. The difference in SID filter behavior is audible and matches real hardware comparisons.
5. Selectable via `IMachineProfile.LoadProfile("c64c")`.

### Traceability

- **Interfaces:** `IMachineProfile`
- **Test Suite:** `C64CProfileTests`, `Sid8580FilterTests`

---

## FR-PRF-003: SX-64 Profile

**ID:** FR-PRF-003
**Title:** Commodore SX-64 Machine Profile
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The SX-64 is a portable C64 with a built-in 1541 disk drive and a 5" color monitor. It lacks a cassette port and has a different KERNAL ROM (no tape routines, different screen colors on startup).

### Acceptance Criteria

1. KERNAL: SX-64 variant KERNAL ROM with different default colors (blue screen instead of light blue).
2. No cassette port: tape-related I/O port bits behave differently (cassette sense always reads "no tape").
3. Built-in 1541 drive: device 8 is always present.
4. The 6510 I/O port bit 4 (cassette sense) is permanently pulled high.
5. All other hardware is identical to the C64 NMOS profile.
6. Selectable via `IMachineProfile.LoadProfile("sx64")`.

### Traceability

- **Interfaces:** `IMachineProfile`
- **Test Suite:** `Sx64ProfileTests`, `Sx64KernalTests`, `Sx64NoCassetteTests`

---

## FR-PRF-004: C128 Profile

**ID:** FR-PRF-004
**Title:** Commodore 128 Machine Profile
**Priority:** P2 -- Enhancement
**Iteration:** 3

### Description

The Commodore 128 features dual CPUs (MOS 8502 and Zilog Z80), 128KB RAM, an 80-column VDC display chip, and C64 compatibility mode. The C128 profile is a major expansion of the emulation capabilities.

### Acceptance Criteria

1. CPU: MOS 8502 (enhanced 6510 with 2MHz mode per FR-CPU-005) and Z80 processor.
2. VIC-II: MOS 8564 (NTSC) or MOS 8566 (PAL) for the 40-column display.
3. VDC: MOS 8563 for the 80-column display (independent video output).
4. RAM: 128KB main RAM, 16KB or 64KB VDC RAM.
5. ROM: BASIC V7.0, C128 KERNAL, C64 mode KERNAL, Character Generator.
6. MMU (Memory Management Unit) at $D500 controls the expanded banking.
7. C64 compatibility mode (selectable at startup or via GO64 command).
8. The 2MHz mode is functional per FR-CPU-005.
9. Selectable via `IMachineProfile.LoadProfile("c128")`.

### Traceability

- **Interfaces:** `IMachineProfile`
- **Test Suite:** `C128ProfileTests`, `C128DualCpuTests`, `C128VdcTests`, `C128C64ModeTests`

---

## FR-PRF-005: VIC-20 Profile

**ID:** FR-PRF-005
**Title:** Commodore VIC-20 Machine Profile
**Priority:** P2 -- Enhancement
**Iteration:** 3

### Description

The VIC-20 uses the MOS 6502 CPU, MOS 6560 (NTSC) or MOS 6561 (PAL) VIC chip (not VIC-II), two MOS 6522 VIA chips, and 5KB of RAM (expandable). The display and sound capabilities differ significantly from the C64.

### Acceptance Criteria

1. CPU: Standard MOS 6502 (not 6510; no built-in I/O port).
2. VIC chip: MOS 6560/6561 with 22-column text display and 4-voice sound.
3. VIA: Two MOS 6522 VIAs (per FR-VIA-004) instead of CIAs.
4. RAM: 5KB base (1KB at $0000, 4KB at $1000) expandable to 32KB+ with cartridge RAM.
5. ROM: BASIC V2, VIC-20 KERNAL, Character Generator.
6. Memory map differs significantly from C64.
7. Sound uses the VIC chip's 4 voices (3 square wave + 1 noise) instead of SID.
8. Selectable via `IMachineProfile.LoadProfile("vic20")`.
9. Memory expansion configurations (3KB, 8KB, 16KB, 24KB, 32KB) are selectable.

### Traceability

- **Interfaces:** `IMachineProfile`
- **Test Suite:** `Vic20ProfileTests`, `VicChipTests`, `Vic20MemoryExpansionTests`

---

## FR-PRF-006: PET Profile

**ID:** FR-PRF-006
**Title:** Commodore PET Machine Profile
**Priority:** P3 -- Future
**Iteration:** 4

### Description

The Commodore PET series uses a MOS 6502 CPU, MOS 6520 PIA or MOS 6522 VIA chips, and a monochrome 40x25 or 80x25 character display. Multiple PET models (2001, 3032, 4032, 8032, 8296) have different configurations.

### Acceptance Criteria

1. CPU: Standard MOS 6502.
2. Display: CRTC (6545) controlled monochrome character display (40 or 80 columns).
3. I/O: MOS 6520 PIA and/or MOS 6522 VIA depending on model.
4. RAM: 8KB, 16KB, or 32KB depending on model.
5. ROM: PET BASIC (V1, V2, or V4), PET KERNAL, Character Generator.
6. IEEE-488 parallel bus for disk drive communication (not IEC serial).
7. Built-in cassette drive (PET 2001).
8. Selectable via `IMachineProfile.LoadProfile("pet")` with model sub-selection.

### Traceability

- **Interfaces:** `IMachineProfile`
- **Test Suite:** `PetProfileTests`, `PetCrtcTests`, `PetIeee488Tests`

---

## FR-PRF-007: Plus/4 Profile

**ID:** FR-PRF-007
**Title:** Commodore Plus/4 Machine Profile
**Priority:** P3 -- Future
**Iteration:** 4

### Description

The Plus/4 uses the MOS 7501/8501 CPU, TED chip (handling both video and sound), and 64KB RAM. The TED chip replaces both the VIC-II and SID with a simpler combined audio/video chip.

### Acceptance Criteria

1. CPU: MOS 7501 or 8501 (6502-compatible with built-in I/O port).
2. TED: MOS 7360/8360 providing 40x25 text and 320x200/160x200 graphics, plus 2 square-wave sound channels.
3. RAM: 64KB main RAM.
4. ROM: BASIC V3.5 (with built-in machine monitor, renumber, etc.), Plus/4 KERNAL, 3-plus-1 software ROM.
5. No SID chip; sound is generated by the TED.
6. No VIC-II; video is generated by the TED with 121-color palette.
7. Selectable via `IMachineProfile.LoadProfile("plus4")`.

### Traceability

- **Interfaces:** `IMachineProfile`
- **Test Suite:** `Plus4ProfileTests`, `TedChipTests`, `TedAudioTests`

---

## FR-PRF-008: C16 Profile

**ID:** FR-PRF-008
**Title:** Commodore C16 Machine Profile
**Priority:** P3 -- Future
**Iteration:** 4

### Description

The C16 is essentially a cut-down Plus/4 with 16KB RAM and no built-in software ROMs. It uses the same TED chip and is software-compatible with the Plus/4 within its memory limits.

### Acceptance Criteria

1. Identical to Plus/4 profile except: 16KB RAM instead of 64KB.
2. No 3-plus-1 software ROMs.
3. The TED chip and CPU are identical to the Plus/4.
4. Programs that fit within 16KB of RAM run identically to the Plus/4.
5. Memory access above 16KB wraps or mirrors as on real hardware.
6. Selectable via `IMachineProfile.LoadProfile("c16")`.

### Traceability

- **Interfaces:** `IMachineProfile`
- **Test Suite:** `C16ProfileTests`, `C16MemoryLimitTests`
