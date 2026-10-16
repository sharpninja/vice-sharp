# FR-Storage-Tape: Tape / Datasette Functional Requirements

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Subsystem      | Storage / Datasette            |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-04-13                     |

---

## FR-TAP-001: Datasette Motor Control

**ID:** FR-TAP-001
**Title:** Datasette Motor Control
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The Commodore Datasette tape drive motor is controlled by bit 3 of the 6510 I/O port ($0001). The motor is active-low (writing 0 turns the motor on). The motor has a spin-up delay before tape movement begins and a mechanical inertia effect.

### Acceptance Criteria

1. Bit 3 of $0001 controls the motor: 0 = motor on, 1 = motor off.
2. The motor spin-up delay (approximately 0.5 seconds on real hardware) is modeled.
3. The tape position advances only when the motor is running and spin-up is complete.
4. The PLAY button state (bit 4 of $0001, cassette sense) must be active (low) for the motor to engage.
5. Motor state changes are reflected in the `ITapeUnit.MotorState` property.
6. Audio output from the tape (when connected to SID EXT IN) is modeled.

### Traceability

- **Interfaces:** `ITapeUnit`
- **Test Suite:** `DatasetteMotorTests`, `MotorSpinUpTests`, `CassetteSenseTests`

---

## FR-TAP-002: TAP Format Support (v0 and v1)

**ID:** FR-TAP-002
**Title:** TAP File Format Support (v0 and v1)
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The TAP file format stores raw tape pulse timing data. TAP v0 stores pulse lengths as single bytes (with 0 indicating an overflow requiring 3 additional bytes). TAP v1 extends the overflow encoding. The emulator shall read and write both TAP format versions.

### Acceptance Criteria

1. TAP v0 files are correctly parsed: each byte represents a pulse half-period in units of 8 system clock cycles.
2. A byte value of 0 in TAP v0 indicates the next 3 bytes form a 24-bit little-endian overflow value.
3. TAP v1 files use an extended header and the same pulse encoding with improved overflow handling.
4. The TAP header (magic bytes, version, platform, video standard, data length) is validated on load.
5. Tape images can be mounted and unmounted at runtime via `ITapeUnit.Mount()`/`ITapeUnit.Eject()`.
6. Writing to TAP format captures the pulse timing from emulated writes.
7. The platform byte in the header distinguishes C64, VIC-20, and C16/Plus4 tapes.

### Traceability

- **Interfaces:** `ITapeUnit`, `ITapCodec`
- **Test Suite:** `TapV0ReadTests`, `TapV1ReadTests`, `TapWriteTests`, `TapHeaderValidationTests`

---

## FR-TAP-003: Tape Read Timing

**ID:** FR-TAP-003
**Title:** Cycle-Accurate Tape Read Timing
**Priority:** P1 -- Important
**Iteration:** 2

### Description

Tape reading relies on the CIA1 Timer A or the FLAG pin to measure the time between pulses from the datasette. The Kernal tape routine uses Timer A in one-shot mode to measure pulse widths and classify them as short, medium, or long pulses for data decoding.

### Acceptance Criteria

1. Each pulse from the tape triggers the FLAG input on CIA1 (directly connected to the cassette read line).
2. The time between pulses is measured by CIA1 Timer A and/or by CIA1 FLAG interrupts.
3. Standard Commodore encoding uses three pulse widths: short (352 cycles TAP value $30), medium (512 cycles TAP value $42), and long (672 cycles TAP value $56).
4. The standard Commodore tape header (FOUND marker), data blocks, and checksums are readable.
5. Tape reading at normal speed produces identical results to real hardware.
6. The timing accuracy supports both PAL and NTSC system clock rates.

### Traceability

- **Interfaces:** `ITapeUnit`, `IClockedDevice`
- **Test Suite:** `TapeReadTimingTests`, `PulseWidthClassificationTests`, `TapeDataDecodingTests`

---

## FR-TAP-004: Tape Write Support

**ID:** FR-TAP-004
**Title:** Tape Write Support
**Priority:** P2 -- Enhancement
**Iteration:** 3

### Description

The emulator shall support writing data to tape images via the datasette write line. The write signal from the VIA/CIA generates pulses that are recorded to the TAP file with correct timing.

### Acceptance Criteria

1. The tape write line output generates pulses whose timing is captured.
2. Written pulses are stored in TAP format with correct timing values.
3. The standard Commodore SAVE routine produces valid tape data.
4. Writes can be appended to existing TAP files or written to new files.
5. The record interlock (RECORD + PLAY buttons) must be engaged for writes.

### Traceability

- **Interfaces:** `ITapeUnit`
- **Test Suite:** `TapeWriteTests`, `TapWriteFormatTests`

---

## FR-TAP-005: Turbo Loader Compatibility

**ID:** FR-TAP-005
**Title:** Turbo Tape Loader Compatibility
**Priority:** P2 -- Enhancement
**Iteration:** 3

### Description

Many commercial C64 programs used turbo tape loaders that employ faster pulse encoding than the standard Commodore format. The emulator shall support turbo loaders by providing accurate tape read timing down to individual clock cycles.

### Acceptance Criteria

1. Turbo loaders using shorter pulse widths than standard (down to approximately 160 cycles per pulse) function correctly.
2. Common turbo formats (Novaload, Biturbo, Turbotape, Cyberload, Pavloda) load successfully.
3. Custom pulse encoding schemes are supported through accurate pulse-level emulation.
4. The CIA FLAG input timing is accurate to 1 system clock cycle.
5. Half-wave and full-wave detection methods both work correctly.
6. Multi-speed loaders (that change pulse rates mid-load) are supported.

### Traceability

- **Interfaces:** `ITapeUnit`
- **Test Suite:** `TurboLoaderTests`, `ShortPulseTimingTests`, `MultiSpeedLoaderTests`
