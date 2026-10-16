# FR-Cartridges: Cartridge Functional Requirements

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Subsystem      | Cartridges / Expansion Port    |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-04-13                     |

---

## FR-CRT-001: Standard 8K/16K Cartridges

**ID:** FR-CRT-001
**Title:** Standard 8K and 16K Cartridge Support
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The emulator shall support standard 8KB and 16KB cartridge images. An 8K cartridge maps to ROML ($8000-$9FFF) with EXROM asserted. A 16K cartridge maps to both ROML ($8000-$9FFF) and ROMH ($A000-$BFFF or $E000-$FFFF depending on Ultimax mode) with both GAME and EXROM asserted.

### Acceptance Criteria

1. CRT file format (VICE cartridge image format) headers are correctly parsed.
2. 8K cartridges map to ROML at $8000-$9FFF; EXROM=0, GAME=1 (active-low).
3. 16K cartridges map to ROML at $8000-$9FFF and ROMH at $A000-$BFFF; EXROM=0, GAME=0.
4. The cartridge autostart vector at $8000 (magic bytes $C3, $C2, $CD, $38, $30) is detected and the cold-start routine is called.
5. Cartridge ROM is read-only; writes to the cartridge address range go to underlying RAM.
6. Cartridges can be inserted and removed at runtime via `ICartridgePort.Insert()`/`ICartridgePort.Remove()`.
7. Removing a cartridge restores the default banking configuration.

### Traceability

- **Interfaces:** `ICartridgePort`, `IAddressSpace`
- **Test Suite:** `StandardCartridgeTests`, `CrtFileParserTests`, `CartridgeAutostartTests`

---

## FR-CRT-002: Ocean Type 1 Cartridge

**ID:** FR-CRT-002
**Title:** Ocean Type 1 Bank-Switching Cartridge
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The Ocean Type 1 cartridge uses bank switching to map up to 512KB of ROM into the ROML region. Bank selection is performed by writing to the $DE00 I/O area. This cartridge type is used by many commercial games.

### Acceptance Criteria

1. Writing to $DE00 selects the active ROM bank (bits 0-5 select from up to 64 banks of 8KB each).
2. The selected bank is mapped at ROML ($8000-$9FFF).
3. ROMH can optionally be present (some Ocean carts use both ROML and ROMH).
4. Maximum size is 512KB (64 x 8KB banks).
5. The EXROM/GAME line configuration follows the Ocean Type 1 specification.
6. The cartridge CRT file chip packets correctly identify bank numbers.

### Traceability

- **Interfaces:** `ICartridgePort`
- **Test Suite:** `OceanType1Tests`, `OceanBankSwitchTests`

---

## FR-CRT-003: EasyFlash Cartridge

**ID:** FR-CRT-003
**Title:** EasyFlash Cartridge with Flash Memory
**Priority:** P2 -- Enhancement
**Iteration:** 3

### Description

The EasyFlash is a modern cartridge with 1MB of flash memory (2 x 512KB banks for ROML and ROMH) and 256 bytes of RAM at $DF00. It supports runtime programming of flash memory and flexible banking.

### Acceptance Criteria

1. Two 512KB flash chips provide 64 banks of 8KB each for ROML ($8000-$9FFF) and ROMH ($A000-$BFFF or $E000-$FFFF).
2. The bank register at $DE00 selects the active bank (bits 0-5).
3. The control register at $DE02 configures GAME/EXROM lines (bits 0-1) and LED state (bit 7).
4. 256 bytes of battery-backed RAM are accessible at $DF00-$DFFF.
5. Flash memory programming (byte-level writes using the standard flash command sequences) is emulated.
6. Flash erase (sector and chip erase) operations are supported.
7. EasyFlash CRT images are loaded with correct bank and chip assignments.
8. Modified flash contents can be saved back to the CRT file via `IFlashMemory.Save()`.

### Traceability

- **Interfaces:** `ICartridgePort`, `IFlashMemory`
- **Test Suite:** `EasyFlashTests`, `FlashProgrammingTests`, `EasyFlashRamTests`

---

## FR-CRT-004: Action Replay / Retro Replay

**ID:** FR-CRT-004
**Title:** Action Replay and Retro Replay Cartridge
**Priority:** P2 -- Enhancement
**Iteration:** 3

### Description

The Action Replay (and its modern successor Retro Replay) is a utility cartridge providing a machine monitor, fast loader, freezer function, and other tools. It uses bank switching, RAM expansion, and the FREEZE button feature.

### Acceptance Criteria

1. The Action Replay ROM is mapped according to the cartridge's banking logic ($DE00/$DE01 control registers).
2. The FREEZE button (directly asserting NMI + GAME/EXROM change) triggers the freezer entry point.
3. The cartridge's 8KB RAM is accessible when banked in.
4. Bank switching between ROM pages is controlled via writes to $DE00.
5. The cartridge can be disabled (effectively removed from the bus) by the control register.
6. REU (RAM Expansion Unit) passthrough is supported on Retro Replay.
7. The cartridge CRT format for Action Replay and Retro Replay is correctly parsed.

### Traceability

- **Interfaces:** `ICartridgePort`
- **Test Suite:** `ActionReplayTests`, `FreezeButtonTests`, `RetroReplayTests`

---

## FR-CRT-005: Final Cartridge III

**ID:** FR-CRT-005
**Title:** Final Cartridge III (FC3)
**Priority:** P2 -- Enhancement
**Iteration:** 3

### Description

The Final Cartridge III is a utility cartridge with 64KB of ROM in 4 banks, providing a desktop environment, fast loader, freezer, and machine monitor. It uses a complex banking scheme controlled by writes to $DFFF.

### Acceptance Criteria

1. Four 16KB ROM banks are selectable via writes to $DFFF.
2. Bits 0-1 of $DFFF select the bank; bit 4 controls NMI generation; bits 5-6 control GAME/EXROM.
3. The FREEZE button triggers NMI and banks in the FC3 ROM.
4. The cartridge's fast loader functionality works correctly.
5. The desktop environment (GUI) launches on cold start when configured.
6. The FC3 can be disabled by writing the appropriate value to $DFFF (setting GAME=1, EXROM=1).
7. The CRT file format for FC3 is correctly parsed with all 4 bank chips.

### Traceability

- **Interfaces:** `ICartridgePort`
- **Test Suite:** `FinalCartridge3Tests`, `Fc3BankSwitchTests`, `Fc3FreezerTests`
