# FR-VIC20: Commodore VIC-20 Functional Requirements

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Subsystem      | VIC-20 architecture            |
| Version        | 0.2.0                          |
| Last Updated   | 2026-08-17                     |
| Iteration      | 2                              |

---

## FR-VIC20-001: VIC-I 6560/6561 display

**ID:** FR-VIC20-001
**Title:** VIC-I (MOS 6560 NTSC / 6561 PAL) video
**Priority:** P0 -- Critical
**Iteration:** 2

### Description

The VIC-20 display is driven by the MOS 6560 (NTSC) or MOS 6561 (PAL) VIC-I chip, not the C64 VIC-II. Slice A may ship a black-frame stub that implements `IVideoChip` so host frame pull does not crash; full raster and character rendering land in later slices.

### Acceptance Criteria

1. A VIC-20 machine exposes a device with role `VideoChip` implementing `IVideoChip`.
2. PAL profile uses 6561 timing: 1_108_405 Hz, 71 cycles/line, 312 lines (VICE `vic20.h`).
3. NTSC profile uses 6560 timing: 1_022_727 Hz, 65 cycles/line, 261 lines (VICE `vic20.h`).
4. Framebuffer contract matches existing host path (BGRA, FrameCompleted).
5. Full character/bitmap/color rendering and on-chip audio are delivered after the stub gate.

### Traceability

- **Interfaces:** `IVideoChip`, `IMachineProfile`
- **Test Suite:** `Vic20SkeletonTests`, later `VicChipTests`

---

## FR-VIC20-002: VIC-20 memory map and expansions

**ID:** FR-VIC20-002
**Title:** VIC-20 base memory map and RAM expansions
**Priority:** P0 -- Critical
**Iteration:** 2

### Description

Unexpanded VIC-20 has 5KB base RAM (1KB system at $0000-$03FF, 4KB user at $1000-$1FFF), character ROM at $8000, I/O at $9000, color RAM at $9400, BASIC at $C000, KERNAL at $E000. Expansions (3K/8K/16K/24K/32K) map into BLK regions.

### Acceptance Criteria

1. After reset with official ROMs, BASIC is readable at $C000 and KERNAL at $E000.
2. Character ROM is readable at $8000.
3. Base RAM regions accept read/write at $0000-$03FF and $1000-$1FFF.
4. Expansion configurations are selectable via profile/resources (later slice).
5. Open-bus and I/O window rules match VICE for validated address ranges.

### Traceability

- **Interfaces:** `IBus`, `IArchitectureDescriptor`
- **Test Suite:** `Vic20SkeletonTests`, later `Vic20MemoryExpansionTests`

---

## FR-VIC20-003: VIA1/VIA2 integration

**ID:** FR-VIC20-003
**Title:** VIC-20 dual VIA 6522 wiring
**Priority:** P0 -- Critical
**Iteration:** 2

### Description

The VIC-20 uses two shared `Via6522` instances (one chip implementation, board wiring only in Architectures): VIA1 at $9110 (typically NMI path) and VIA2 at $9120 (typically IRQ path). No C1541-specific assumptions may live in the shared VIA chip.

### Acceptance Criteria

1. Machine builds with two `Via6522` devices registered on the bus at $9110 and $9120 (16-byte windows).
2. VIA1 and VIA2 are clocked and reset with the machine.
3. Shared `Via6522` source contains no VIC-20 or 1541 board glue (ARCH-CHIPGLUE-001).
4. Keyboard, joystick, and RESTORE wiring land in later slices without forking the chip.

### Traceability

- **Interfaces:** `Via6522`, FR-VIA-001..005
- **Test Suite:** `Vic20SkeletonTests`, later VIA keyboard tests

---

## FR-VIC20-004: VIC-20 keyboard matrix

**ID:** FR-VIC20-004
**Title:** VIC-20 keyboard matrix via VIA ports
**Priority:** P1 -- Important
**Iteration:** 2

### Description

Keyboard scanning uses VIA port lines (not CIA). Host key events map through a VIC-20 matrix type.

### Acceptance Criteria

1. Key press/release is visible to software scanning the matrix through VIA ports.
2. Matrix layout matches VICE VIC-20 VKM / hardware mapping for the default layout.
3. Disabled until wiring slice; skeleton builds without requiring host keyboard.

### Traceability

- **Test Suite:** later `Vic20KeyboardTests`

---

## FR-VIC20-005: VIC-20 expansion cartridges and persistence

**ID:** FR-VIC20-005
**Title:** VIC-20 FE3, Ultimem, and Mega-Cart runtime behavior
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The supported VIC-20 expansion set attaches through the architecture cartridge path. FE3 MODE_FLASH follows the AM29F040B TYPE_B command and erase timing model. Writable cartridge state survives detach through host-managed atomic write-back.

### Acceptance Criteria

1. FE3, Ultimem, Mega-Cart, and standard BLK images mount into their documented address regions.
2. FE3 byte programming uses AND semantics; sector/chip erases complete only after the 50 / 1,000,000 / 8,000,000 VICE cycle budgets, including busy status and sector suspend/resume behavior.
3. Dirty FE3 and Ultimem flash images are returned for write-back, and dirty Mega-Cart NVRAM is returned separately.
4. Detach persists dirty state with a same-directory temporary file and atomic replacement without modifying clean images.
5. The machine builds and boots without a cartridge attached.

### Traceability

- **TR:** TR-VIC20-FLASH-001
- **Test Suite:** `Flash040EraseLatencyTests`, `Vic20ExpansionCartTests`, `UltimemParityTests`, `MegaCartParityTests`, `MediaServiceHostVic20ExpansionTests`

---

## FR-VIC20-006: Default drive 8 is Commodore 1540

**ID:** FR-VIC20-006
**Title:** VIC-20 default unit 8 drive model is 1540
**Priority:** P0 -- Critical
**Iteration:** 2

### Description

Historical VIC-20 kits shipped with the 1540. ViceSharp VIC-20 sessions default drive unit 8 to `DriveModel.C1540` and DOS ROM `dos1540-325302+3-01.bin`. C64 sessions keep default 1541. Users may still select 1541/1541-II later.

### Acceptance Criteria

1. `Vic20MachineProfile.DefaultDriveModel` is `DriveModel.C1540` for all standard VIC-20 profiles.
2. Topology / host attach for drive 8 uses 1540 DOS ROM name when following profile defaults.
3. C64 profiles are unchanged (default 1541).
4. Tests pin the Vic20 default to 1540.

### Traceability

- **Interfaces:** `DriveModel`, `C1541ViceRomNames.Dos1540`
- **Test Suite:** `Vic20SkeletonTests`

---

## FR-VIC20-SOUND-001: VIC-I sound generation

**ID:** FR-VIC20-SOUND-001
**Title:** Deterministic VIC-I sound and live PCM delivery
**Priority:** P1 -- Important
**Iteration:** 2

### Description

Writes to VIC-I sound registers update the managed sound engine. Machine execution advances the engine with the emulation clock and delivers fixed-size PCM16 mono batches without allocating on the silent register-write hot path.

### Acceptance Criteria

1. VIC-I sound register writes update the corresponding managed oscillator state.
2. Equal initial state and writes produce byte-identical PCM batches across managed runs.
3. Focused silence and tone batches are byte-identical to native xvic.
4. Live hosts receive 256-sample batches through the configured audio backend.
5. Silent `Mos6561.Store` calls allocate no managed memory after warmup.

### Traceability

- **TR:** TR-VIC20-SOUND-001
- **Test Suite:** `Vic20SoundLockstep`, `Vic20AudioWiringTests`

---

## FR-PRF-005 cross-reference

Profile selection for VIC-20 is also specified under `FR-Machine-Profiles.md` (`FR-PRF-005`). Iteration 2 owns delivery of that profile.
