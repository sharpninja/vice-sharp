# FR-Memory-System: Memory System Functional Requirements

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Subsystem      | Memory / Address Space         |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-04-13                     |

---

## FR-MEM-001: Address Decoding and PLA Banking

**ID:** FR-MEM-001
**Title:** Address Decoding and PLA Banking Configuration
**Priority:** P0 -- Critical
**Iteration:** 0

### Description

The memory subsystem shall implement the C64 PLA (Programmable Logic Array) address decoding, which maps the 64KB address space to RAM, ROM, and I/O regions based on the 6510 I/O port bits (LORAM, HIRAM, CHAREN) and the cartridge GAME/EXROM lines. All 32 possible PLA configurations shall be supported.

### Acceptance Criteria

1. The address decoder consults the 5 input bits (LORAM, HIRAM, CHAREN, GAME, EXROM) to select from all 32 banking configurations.
2. Each configuration correctly maps the following regions: $0000-$0FFF (RAM), $1000-$7FFF (RAM), $8000-$9FFF (RAM/ROML), $A000-$BFFF (RAM/BASIC/ROMH), $C000-$CFFF (RAM), $D000-$DFFF (RAM/CHARROM/IO), $E000-$FFFF (RAM/KERNAL/ROMH).
3. Banking changes take effect on the cycle following the write to $0001.
4. The `IBankController` publishes a `BankConfigChanged` event when the active configuration changes.
5. The address decoder is stateless relative to its inputs -- the same 5-bit input always produces the same mapping.

### Traceability

- **Interfaces:** `IAddressSpace`, `IBankController`
- **Test Suite:** `PlaDecodingTests`, `BankConfigurationMatrixTests`

---

## FR-MEM-002: RAM Under ROM Access

**ID:** FR-MEM-002
**Title:** RAM Under ROM Access
**Priority:** P0 -- Critical
**Iteration:** 0

### Description

The CPU shall be able to write to RAM underlying any ROM region, and read from RAM underlying ROM via appropriate banking configuration. When BASIC ROM is mapped at $A000-$BFFF, writes go to the underlying RAM. When a cartridge ROM is mapped, writes to that region go to underlying RAM (not the cartridge ROM).

### Acceptance Criteria

1. Writes to addresses $A000-$BFFF always store to underlying RAM regardless of whether BASIC ROM is banked in.
2. Writes to addresses $E000-$FFFF always store to underlying RAM regardless of whether KERNAL ROM is banked in.
3. Writes to addresses $D000-$DFFF when Character ROM is banked in store to underlying RAM.
4. Reading from a RAM-under-ROM address when ROM is banked out returns the previously written RAM value.
5. The VIC-II always reads from the "VIC view" of memory (it sees character ROM at $1000-$1FFF and $9000-$9FFF in the VIC address space, not RAM).

### Traceability

- **Interfaces:** `IAddressSpace`
- **Test Suite:** `RamUnderRomTests`, `VicMemoryViewTests`

---

## FR-MEM-003: Ultimax Mode

**ID:** FR-MEM-003
**Title:** Ultimax Cartridge Mode
**Priority:** P1 -- Important
**Iteration:** 2

### Description

When the GAME line is asserted (active) and EXROM is deasserted (active), the system enters Ultimax mode. In this mode, only 4KB of RAM at $0000-$0FFF is visible to the CPU, the cartridge ROML is at $8000-$9FFF, and ROMH is at $E000-$FFFF. I/O area at $D000-$DFFF remains accessible. All other address ranges read open bus (the last value on the data bus).

### Acceptance Criteria

1. With GAME=1, EXROM=0 the address space is configured for Ultimax mode.
2. RAM is only accessible at $0000-$0FFF.
3. ROML from the cartridge is mapped at $8000-$9FFF.
4. ROMH from the cartridge is mapped at $E000-$FFFF.
5. I/O space is mapped at $D000-$DFFF.
6. Reads from unmapped regions ($1000-$7FFF, $A000-$CFFF) return open-bus values.
7. The VIC-II can access cartridge ROM in Ultimax mode.

### Traceability

- **Interfaces:** `IAddressSpace`, `ICartridgePort`
- **Test Suite:** `UltimaxModeTests`, `OpenBusTests`

---

## FR-MEM-004: VIC Bank Switching

**ID:** FR-MEM-004
**Title:** VIC-II Bank Switching via CIA2
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The VIC-II chip views a 16KB window of the 64KB address space, selectable by bits 0-1 of CIA2 Port A ($DD00). The four possible banks are: Bank 0 ($0000-$3FFF), Bank 1 ($4000-$7FFF), Bank 2 ($8000-$BFFF), Bank 3 ($C000-$FFFF). The VIC-II's view of Character ROM overlaps in banks 0 and 2.

### Acceptance Criteria

1. CIA2 Port A bits 0-1 (active-low, inverted) select the VIC bank: %11 = Bank 0, %10 = Bank 1, %01 = Bank 2, %00 = Bank 3.
2. VIC-II addresses are relative to the selected bank start.
3. In Banks 0 and 2, addresses $1000-$1FFF within the bank read from Character ROM instead of RAM.
4. Bank switching takes effect immediately for subsequent VIC-II accesses.
5. The `IVicBankSelector` interface reports the currently active bank and base address.

### Traceability

- **Interfaces:** `IAddressSpace`, `IVicBankSelector`
- **Test Suite:** `VicBankSwitchTests`, `CharacterRomOverlayTests`

---

## FR-MEM-005: Color RAM

**ID:** FR-MEM-005
**Title:** Color RAM ($D800-$DBFF)
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The 1K nybble-wide Color RAM at $D800-$DBFF shall be emulated. Color RAM is a separate 4-bit-wide SRAM that is always visible in the I/O region and is not affected by banking configuration. Only the lower 4 bits of each byte are significant; the upper 4 bits read as open bus.

### Acceptance Criteria

1. Color RAM is accessible at $D800-$DBFF regardless of banking configuration.
2. Only the low nybble (bits 0-3) is stored and returned on reads.
3. The upper nybble reads from the open bus (typically the value last placed on the data bus).
4. Color RAM is preserved across banking changes.
5. Both the CPU and VIC-II can access Color RAM (VIC-II reads it during character fetch).

### Traceability

- **Interfaces:** `IAddressSpace`
- **Test Suite:** `ColorRamTests`, `NybbleWideBehaviorTests`

---

## FR-MEM-006: Zero Page and Stack Behavior

**ID:** FR-MEM-006
**Title:** Zero Page and Stack Page Behavior
**Priority:** P0 -- Critical
**Iteration:** 0

### Description

The zero page ($0000-$00FF) and stack page ($0100-$01FF) shall behave as fast-access RAM with the 6510 I/O port overlay at $0000-$0001. Stack operations (PHA, PLA, JSR, RTS, BRK, RTI) shall correctly wrap the stack pointer within the $0100-$01FF page.

### Acceptance Criteria

1. Addresses $0002-$00FF behave as standard RAM (zero page).
2. Addresses $0000-$0001 are intercepted by the 6510 I/O port (per FR-CPU-004).
3. Stack operations always access $0100 + SP; the SP wraps within 8 bits (no access outside $0100-$01FF).
4. JSR pushes PC+2 (high byte first, then low byte).
5. RTS pulls low byte first, then high byte, and adds 1 to the result.
6. Stack underflow/overflow wraps without generating an exception.

### Traceability

- **Interfaces:** `IAddressSpace`
- **Test Suite:** `ZeroPageTests`, `StackBehaviorTests`, `StackWrapTests`
