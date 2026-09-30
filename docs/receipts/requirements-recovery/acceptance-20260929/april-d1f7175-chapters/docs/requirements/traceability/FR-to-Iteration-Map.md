# FR-to-Iteration Traceability Map

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Project        | ViceSharp                      |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-04-13                     |

## Purpose

This document maps each Functional Requirement to its target implementation iteration. Iterations are numbered from 0 (foundations) through 4 (extended machine support).

---

## Iteration 0 -- Foundations (CPU Core, Address Space)

Core infrastructure upon which all other features depend.

| FR ID       | FR Title                          | Subsystem    |
|-------------|-----------------------------------|--------------|
| FR-CPU-001  | Full 6502/6510 instruction set    | CPU          |
| FR-CPU-002  | Cycle-accurate execution          | CPU          |
| FR-CPU-003  | Interrupt handling                | CPU          |
| FR-CPU-004  | 6510 I/O port                     | CPU          |
| FR-MEM-001  | Address decoding & PLA banking    | Memory       |
| FR-MEM-002  | RAM under ROM access              | Memory       |
| FR-MEM-006  | Zero page / stack behavior        | Memory       |

**Exit Criteria:** CPU passes Lorenz test suite (basic instruction tests). Address space correctly decodes all 32 PLA configurations.

---

## Iteration 1 -- C64 MVP (Video, Audio, I/O, Input, Basic Tools)

Minimum viable C64 emulation: boot to BASIC prompt, run simple programs, display graphics, produce sound.

| FR ID       | FR Title                          | Subsystem    |
|-------------|-----------------------------------|--------------|
| FR-MEM-004  | VIC bank switching                | Memory       |
| FR-MEM-005  | Color RAM                         | Memory       |
| FR-VIC-001  | Raster engine & timing            | Video        |
| FR-VIC-002  | Character display modes           | Video        |
| FR-VIC-003  | Bitmap display modes              | Video        |
| FR-VIC-004  | Sprite engine                     | Video        |
| FR-VIC-005  | Sprite collision detection        | Video        |
| FR-VIC-006  | Badline handling & DMA stealing   | Video        |
| FR-VIC-009  | VIC-II bank switching             | Video        |
| FR-SID-001  | Three-voice oscillator            | Audio        |
| FR-SID-002  | Waveform generation               | Audio        |
| FR-SID-004  | Filter (6581 variant)             | Audio        |
| FR-SID-006  | ADSR envelope                     | Audio        |
| FR-SID-009  | Noise LFSR                        | Audio        |
| FR-CIA-001  | Timer A/B with cascading          | I/O          |
| FR-CIA-002  | TOD clock                         | I/O          |
| FR-CIA-003  | Keyboard matrix scanning          | I/O          |
| FR-CIA-004  | Joystick reading                  | I/O          |
| FR-CIA-006  | NMI generation (CIA2)             | I/O          |
| FR-CIA-007  | IRQ generation (CIA1)             | I/O          |
| FR-INP-001  | Keyboard matrix emulation         | Input        |
| FR-INP-002  | Joystick port 1 & 2              | Input        |
| FR-MED-001  | Screenshot capture                | Media        |
| FR-MON-001  | Disassembly view                  | Monitor      |
| FR-MON-002  | Memory hex/ASCII display          | Monitor      |
| FR-MON-003  | Breakpoint management             | Monitor      |
| FR-MON-004  | Register inspection/manipulation  | Monitor      |
| FR-SNP-001  | Save machine state                | Snapshot     |
| FR-SNP-002  | Load machine state                | Snapshot     |
| FR-PRF-001  | C64 (NMOS) profile                | Profiles     |

**Exit Criteria:** C64 boots to BASIC ready prompt. Simple BASIC programs run. Common games load and are playable. Audio is recognizable. Screenshots can be captured.

---

## Iteration 2 -- Advanced Features, Drives, Cartridges

Cycle-exact tricks, disk drive emulation, tape, cartridges, advanced SID features. Enables demo scene content and most commercial software.

| FR ID       | FR Title                          | Subsystem    |
|-------------|-----------------------------------|--------------|
| FR-MEM-003  | Ultimax mode                      | Memory       |
| FR-VIC-007  | Border behavior & open borders    | Video        |
| FR-VIC-008  | FLI / AFLI support                | Video        |
| FR-VIC-010  | Sprite multiplexing DMA timing    | Video        |
| FR-SID-003  | Combined waveforms                | Audio        |
| FR-SID-005  | Filter (8580 variant)             | Audio        |
| FR-SID-007  | Ring modulation                   | Audio        |
| FR-SID-008  | Hard sync                         | Audio        |
| FR-SID-010  | Digi playback ($D418)             | Audio        |
| FR-CIA-005  | Serial port shift register        | I/O          |
| FR-DRV-001  | 1541 drive emulation              | Drives       |
| FR-DRV-004  | GCR encoding/decoding             | Drives       |
| FR-DRV-005  | IEC bus protocol                  | Drives       |
| FR-TAP-001  | Datasette motor control           | Tape         |
| FR-TAP-002  | TAP format support                | Tape         |
| FR-TAP-003  | Tape read timing                  | Tape         |
| FR-CRT-001  | Standard 8K/16K cartridges        | Cartridges   |
| FR-CRT-002  | Ocean Type 1 cartridge            | Cartridges   |
| FR-INP-003  | Mouse 1351 proportional           | Input        |
| FR-MED-002  | Video recording                   | Media        |
| FR-MED-003  | Audio recording                   | Media        |
| FR-MED-004  | Synchronized A/V capture          | Media        |
| FR-MED-005  | Format selection                  | Media        |
| FR-MON-005  | Memory bank view selection        | Monitor      |
| FR-MON-006  | Watch expressions                 | Monitor      |
| FR-SNP-003  | Deterministic replay              | Snapshot     |
| FR-PRF-002  | C64C (new CMOS) profile           | Profiles     |
| FR-PRF-003  | SX-64 profile                     | Profiles     |

**Exit Criteria:** Demo scene productions (Crest, Oxyron, Booze Design) display correctly. 1541 drive loads software. Tape loading works. Standard cartridges function.

---

## Iteration 3 -- Additional Machines and Peripherals

C128, VIC-20, advanced drives, advanced cartridges, and additional input devices.

| FR ID       | FR Title                          | Subsystem    |
|-------------|-----------------------------------|--------------|
| FR-CPU-005  | 8502 2MHz mode (C128)             | CPU          |
| FR-VIA-001  | VIA 6522 timer operation          | I/O          |
| FR-VIA-002  | Shift register                    | I/O          |
| FR-VIA-003  | Port A/B handshake modes          | I/O          |
| FR-VIA-004  | VIC-20 VIA integration            | I/O          |
| FR-VIA-005  | Disk drive VIA integration        | I/O          |
| FR-DRV-002  | 1571 drive emulation              | Drives       |
| FR-DRV-003  | 1581 drive emulation              | Drives       |
| FR-DRV-006  | Fast loader support               | Drives       |
| FR-TAP-004  | Tape write support                | Tape         |
| FR-TAP-005  | Turbo loader compatibility        | Tape         |
| FR-CRT-003  | EasyFlash cartridge               | Cartridges   |
| FR-CRT-004  | Action Replay / Retro Replay      | Cartridges   |
| FR-CRT-005  | Final Cartridge III               | Cartridges   |
| FR-SID-011  | External audio input              | Audio        |
| FR-SID-012  | Dual-SID configuration            | Audio        |
| FR-INP-004  | Lightpen input                    | Input        |
| FR-INP-005  | Paddle controllers                | Input        |
| FR-SNP-004  | Snapshot comparison / diff        | Snapshot     |
| FR-PRF-004  | C128 profile                      | Profiles     |
| FR-PRF-005  | VIC-20 profile                    | Profiles     |

**Exit Criteria:** C128 boots to both C64 and C128 modes. VIC-20 boots and runs software. Advanced cartridges (EasyFlash, AR) function.

---

## Iteration 4 -- PET, Plus/4, C16

Legacy Commodore machines with distinct architectures.

| FR ID       | FR Title                          | Subsystem    |
|-------------|-----------------------------------|--------------|
| FR-PRF-006  | PET profile                       | Profiles     |
| FR-PRF-007  | Plus/4 profile                    | Profiles     |
| FR-PRF-008  | C16 profile                       | Profiles     |

**Exit Criteria:** PET, Plus/4, and C16 boot to BASIC. Basic programs run on each platform.

---

## Summary by Iteration

| Iteration | Description                     | FR Count | Cumulative |
|-----------|---------------------------------|----------|------------|
| 0         | Foundations                     | 7        | 7          |
| 1         | C64 MVP                        | 30       | 37         |
| 2         | Advanced + Drives + Cartridges  | 27       | 64         |
| 3         | Additional Machines             | 21       | 85         |
| 4         | PET / Plus/4 / C16             | 3        | 88         |
