# FR-Storage-Drives: Disk Drive Functional Requirements

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Subsystem      | Storage / Disk Drives          |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-04-13                     |

---

## FR-DRV-001: 1541 Drive Emulation

**ID:** FR-DRV-001
**Title:** Commodore 1541 Single Floppy Disk Drive Emulation
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The 1541 floppy disk drive shall be emulated as a complete subsystem with its own 6502 CPU, 2KB RAM, two VIA 6522 chips, and drive ROM. The drive operates independently of the main computer, communicating via the IEC serial bus. True drive emulation (running the drive CPU in parallel with the main CPU) is required for compatibility with fast loaders and copy protection.

### Acceptance Criteria

1. The 1541 is emulated with its own 6502 CPU running at 1MHz.
2. Drive RAM (2KB at $0000-$07FF) and ROM (16KB at $C000-$FFFF) are correctly mapped.
3. Two VIA 6522 chips are emulated (VIA1 at $1800, VIA2 at $1C00) per FR-VIA-005.
4. The drive firmware (ROM) executes the standard Commodore DOS 2.6 routines.
5. D64 disk images are supported (35 tracks, 683 sectors, 174,848 bytes).
6. G64 disk images are supported (raw GCR encoding for copy-protected disks).
7. The drive motor spin-up delay (approximately 300ms) is modeled.
8. Head stepping delay (approximately 8ms per half-track) is modeled.
9. The drive can be attached/detached at runtime via `IDiskDrive.Mount()`/`IDiskDrive.Eject()`.

### Traceability

- **Interfaces:** `IDiskDrive`, `IClockedDevice`
- **Test Suite:** `Drive1541Tests`, `D64ImageTests`, `G64ImageTests`, `DriveTimingTests`

---

## FR-DRV-002: 1571 Drive Emulation

**ID:** FR-DRV-002
**Title:** Commodore 1571 Double-Sided Floppy Disk Drive Emulation
**Priority:** P2 -- Enhancement
**Iteration:** 3

### Description

The 1571 is a double-sided version of the 1541, used primarily with the C128. It supports 70 tracks (35 per side), MFM encoding for CP/M compatibility, and burst transfer mode for faster data transfer.

### Acceptance Criteria

1. The 1571 is emulated with its own 6502 CPU and expanded ROM.
2. Double-sided disk support: 70 tracks (35 per side), 1,366 sectors.
3. D71 disk images are supported.
4. GCR encoding is used for the first side (Commodore-native).
5. MFM encoding is available for CP/M mode on both sides.
6. Burst transfer mode enables faster serial bus communication.
7. The 1571 can operate in 1541-compatibility mode (single-sided, GCR only).

### Traceability

- **Interfaces:** `IDiskDrive`, `IClockedDevice`
- **Test Suite:** `Drive1571Tests`, `D71ImageTests`, `DoubleSidedTests`, `BurstTransferTests`

---

## FR-DRV-003: 1581 Drive Emulation

**ID:** FR-DRV-003
**Title:** Commodore 1581 3.5" Floppy Disk Drive Emulation
**Priority:** P2 -- Enhancement
**Iteration:** 3

### Description

The 1581 is a 3.5-inch disk drive using MFM encoding with 80 tracks, double-sided, providing approximately 800KB of storage. It uses a WD1772 floppy disk controller instead of the VIA-based approach of the 1541/1571.

### Acceptance Criteria

1. The 1581 is emulated with its own CPU and WD1772 FDC controller.
2. D81 disk images are supported (80 tracks, 2 sides, 10 sectors per track, 256 bytes per sector).
3. MFM encoding is used throughout.
4. Partitioning and subdirectory support (CMD-style) is available.
5. The IEC serial bus interface operates at the higher transfer rates supported by the 1581.
6. The drive can be mounted/ejected at runtime.

### Traceability

- **Interfaces:** `IDiskDrive`
- **Test Suite:** `Drive1581Tests`, `D81ImageTests`, `Wd1772Tests`

---

## FR-DRV-004: GCR Encoding/Decoding

**ID:** FR-DRV-004
**Title:** Group Code Recording Encoding and Decoding
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The 1541 and 1571 drives use Group Code Recording (GCR) to encode data on disk. Each 4-bit nybble is encoded as a 5-bit GCR pattern to ensure sufficient flux transitions. The GCR codec handles encoding/decoding of data blocks, header blocks, and inter-sector gaps.

### Acceptance Criteria

1. The 4-to-5-bit GCR encoding table matches the Commodore standard (16 valid nybble-to-GCR mappings).
2. Data blocks (256 bytes data + checksum) are correctly GCR-encoded to produce 325 GCR bytes on disk.
3. Header blocks (8 bytes: magic, checksum, sector, track, ID) are GCR-encoded to 10 GCR bytes.
4. Inter-sector gaps (SYNC marks of $FF bytes followed by gap bytes) are correctly generated.
5. The 4 speed zones (tracks 1-17: 3.25 speed, 18-24: 3.50, 25-30: 3.75, 31-35: 4.00) determine bits per track.
6. Decoding GCR data back to binary is bit-accurate.
7. Invalid GCR patterns (not in the standard table) are detectable for copy protection analysis.

### Traceability

- **Interfaces:** `IDiskDrive`, `IGcrCodec`
- **Test Suite:** `GcrEncodingTests`, `GcrDecodingTests`, `SpeedZoneTests`, `InvalidGcrTests`

---

## FR-DRV-005: IEC Serial Bus Protocol

**ID:** FR-DRV-005
**Title:** IEC Serial Bus Protocol
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The IEC (IEEE-488 derived) serial bus connects the C64 to disk drives, printers, and other peripherals. The bus uses three signal lines (ATN, CLK, DATA) in an active-low open-collector configuration. The protocol includes LISTEN, TALK, UNLISTEN, UNTALK, and secondary address commands.

### Acceptance Criteria

1. The three-wire serial bus (ATN, CLK, DATA) operates with correct timing.
2. The ATN line is controlled by the computer; asserting ATN forces all devices to listen.
3. LISTEN (command $20+device) puts a device in receive mode.
4. TALK (command $40+device) puts a device in send mode.
5. UNLISTEN ($3F) and UNTALK ($5F) release devices.
6. Secondary addresses ($60-$6F for OPEN, $E0-$EF for CLOSE, $F0-$FF for channel) are supported.
7. Byte transfer uses the standard Commodore serial protocol timing (EOI signaling for last byte).
8. Bus turnaround (switching from computer-transmit to device-transmit after TALK) follows correct timing.
9. Multiple devices on the bus (up to device #30) are supported simultaneously.

### Traceability

- **Interfaces:** `ISerialBus`, `IDiskDrive`
- **Test Suite:** `IecProtocolTests`, `IecTimingTests`, `MultiDeviceTests`, `EoiHandshakeTests`

---

## FR-DRV-006: Fast Loader Support

**ID:** FR-DRV-006
**Title:** Fast Loader Compatibility
**Priority:** P2 -- Enhancement
**Iteration:** 3

### Description

Many C64 programs use custom fast loader routines that replace the slow standard IEC protocol with faster custom transfer protocols. The emulator shall support common fast loaders by accurately emulating the drive CPU timing and IEC bus signal transitions.

### Acceptance Criteria

1. True drive emulation (drive CPU runs in parallel) ensures fast loader compatibility.
2. The timing of IEC bus signal transitions is accurate to within 1 CPU cycle.
3. Common fast loaders that must work: EPYX Fast Load, Turbo Disk, SpeedDOS, JiffyDOS protocol, DolphinDOS parallel transfer.
4. The drive CPU and main CPU clocks are synchronized (not free-running) to prevent drift.
5. Parallel port connection for DolphinDOS and SpeedDOS is emulated when configured.
6. Custom transfer protocols that use CIA timer-based handshaking are supported.

### Traceability

- **Interfaces:** `IDiskDrive`, `ISerialBus`
- **Test Suite:** `FastLoaderCompatibilityTests`, `DriveTimingSyncTests`, `JiffyDosTests`
