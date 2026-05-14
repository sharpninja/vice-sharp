# FR-to-Iteration Traceability Map

## Document Information

| Field | Value |
|-------|-------|
| Project | ViceSharp |
| Version | 0.1.0-draft |
| Last Updated | 2026-05-13 |

## Purpose

This map groups Functional Requirements by planned implementation iteration.

---

## Iteration 0

| FR ID | Title | Subsystem |
|-------|-------|-----------|
| FR-CPU-001 | Full 6502/6510 Instruction Set Including Undocumented Opcodes | CPU |
| FR-CPU-002 | Cycle-Accurate Execution Timing | CPU |
| FR-CPU-003 | Interrupt Handling with Correct Timing | CPU |
| FR-CPU-004 | 6510 I/O Port at Addresses $0000/$0001 | CPU |
| FR-MEM-001 | Address Decoding and PLA Banking Configuration | Memory |
| FR-MEM-002 | RAM Under ROM Access | Memory |
| FR-MEM-006 | Zero Page and Stack Page Behavior | Memory |
---

## Iteration 1

| FR ID | Title | Subsystem |
|-------|-------|-----------|
| FR-CFG-002 | ROM and Romset Selection | Configuration / Resources |
| FR-CFG-008 | Performance Limiter Configuration | Configuration / Resources |
| FR-CIA-001 | CIA Timer A and Timer B with Cascade Mode | I/O (CIA 6526) |
| FR-CIA-002 | Time-of-Day Clock | I/O (CIA 6526) |
| FR-CIA-003 | Keyboard Matrix Scanning via CIA1 | I/O (CIA 6526) |
| FR-CIA-004 | Joystick Port Reading via CIA1 | I/O (CIA 6526) |
| FR-CIA-006 | NMI Generation from CIA2 | I/O (CIA 6526) |
| FR-CIA-007 | IRQ Generation from CIA1 | I/O (CIA 6526) |
| FR-HOST-001 | Host-Owned Emulator Session Lifecycle | Host / UI Boundary |
| FR-HOST-003 | Host-Streamed Video Frames for Remote UI Clients | Host / UI Boundary |
| FR-HOST-004 | Host-Normalized Keyboard, Joystick, and Machine Control | Host / UI Boundary |
| FR-HOST-005 | Host-Owned Snapshot, Screenshot, and Diagnostic Operations | Host / UI Boundary |
| FR-HOST-006 | Host Runtime Status and Control Telemetry | Host / UI Boundary |
| FR-INP-001 | Keyboard Matrix Emulation | Input Devices |
| FR-INP-002 | Joystick Port 1 and Port 2 Emulation | Input Devices |
| FR-INP-006 | VICE VKM Keymap Selection and Real-Time Keyboard Translation | Input Devices |
| FR-MED-001 | Screenshot Capture (PNG/BMP) | Media Capture |
| FR-MEM-004 | VIC-II Bank Switching via CIA2 | Memory |
| FR-MEM-005 | Color RAM ($D800-$DBFF) | Memory |
| FR-MON-001 | Real-Time Disassembly View | Machine Monitor |
| FR-MON-002 | Memory Hex and ASCII Display | Machine Monitor |
| FR-MON-003 | Breakpoint Management | Machine Monitor |
| FR-MON-004 | CPU Register Inspection and Manipulation | Machine Monitor |
| FR-PRF-001 | Commodore 64 (Original NMOS) Machine Profile | Machine Profiles |
| FR-SID-001 | Three Independent Voice Oscillators | Audio (SID) |
| FR-SID-002 | Waveform Generation (Triangle, Sawtooth, Pulse, Noise) | Audio (SID) |
| FR-SID-004 | 6581 SID Filter Emulation | Audio (SID) |
| FR-SID-006 | ADSR Envelope Generator | Audio (SID) |
| FR-SID-009 | Noise Waveform Linear Feedback Shift Register | Audio (SID) |
| FR-SNP-001 | Save Complete Machine State to Snapshot | Snapshot / Replay |
| FR-SNP-002 | Load Machine State from Snapshot | Snapshot / Replay |
| FR-UI-001 | Dockable Host UI Control Client | Host / UI Boundary |
| FR-UI-002 | Emulator Status and Machine Control Bar | Host / UI Boundary |
| FR-UI-003 | Collapsible Tabbed Emulator Sidebar | Host / UI Boundary |
| FR-UI-004 | Docked and Pop-Out Monitor Control | Host / UI Boundary |
| FR-VIC-001 | Raster Engine with PAL/NTSC Timing | Video (VIC-II) |
| FR-VIC-002 | Character Display Modes (Standard, Multicolor, ECM) | Video (VIC-II) |
| FR-VIC-003 | Bitmap Display Modes (Standard, Multicolor) | Video (VIC-II) |
| FR-VIC-004 | Sprite Engine (8 Hardware Sprites) | Video (VIC-II) |
| FR-VIC-005 | Sprite Collision Detection | Video (VIC-II) |
| FR-VIC-006 | Badline Handling and CPU DMA Cycle Stealing | Video (VIC-II) |
| FR-VIC-009 | VIC-II Bank Switching (See also FR-MEM-004) | Video (VIC-II) |
---

## Iteration 2

| FR ID | Title | Subsystem |
|-------|-------|-----------|
| FR-CFG-001 | Resource File and Command-Line Configuration | Configuration / Resources |
| FR-CFG-003 | Palette Selection and Color Resource Handling | Configuration / Resources |
| FR-CFG-004 | Hotkey Configuration and Action Dispatch | Configuration / Resources |
| FR-CFG-005 | Autostart and Program Launch Handling | Configuration / Resources |
| FR-CIA-005 | CIA Serial Port Shift Register | I/O (CIA 6526) |
| FR-CRT-001 | Standard 8K and 16K Cartridge Support | Cartridges |
| FR-CRT-002 | Ocean Type 1 Bank-Switching Cartridge | Cartridges |
| FR-DRV-001 | Commodore 1541 Single Floppy Disk Drive Emulation | Disk Drives |
| FR-DRV-004 | Group Code Recording Encoding and Decoding | Disk Drives |
| FR-DRV-005 | IEC Serial Bus Protocol | Disk Drives |
| FR-HOST-002 | Host-Mediated Disk, Tape, and Cartridge Attachment | Host / UI Boundary |
| FR-INP-003 | Commodore 1351 Proportional Mouse Emulation | Input Devices |
| FR-MED-002 | Video Recording (MP4 via FFmpeg) | Media Capture |
| FR-MED-003 | Audio Recording (WAV/FLAC) | Media Capture |
| FR-MED-004 | Synchronized Audio/Video Capture | Media Capture |
| FR-MED-005 | Output Format Selection and Configuration | Media Capture |
| FR-MEM-003 | Ultimax Cartridge Mode | Memory |
| FR-MON-005 | Memory Bank View Selection | Machine Monitor |
| FR-MON-006 | Watch Expressions | Machine Monitor |
| FR-PRF-002 | Commodore 64C Machine Profile | Machine Profiles |
| FR-PRF-003 | Commodore SX-64 Machine Profile | Machine Profiles |
| FR-SID-003 | Combined Waveform Output | Audio (SID) |
| FR-SID-005 | 8580 SID Filter Emulation | Audio (SID) |
| FR-SID-007 | Ring Modulation | Audio (SID) |
| FR-SID-008 | Hard Sync (Oscillator Synchronization) | Audio (SID) |
| FR-SID-010 | Direct Digital Sample Playback via Volume Register | Audio (SID) |
| FR-SNP-003 | Deterministic Input Replay | Snapshot / Replay |
| FR-TAP-001 | Datasette Motor Control | Tape / Datasette |
| FR-TAP-002 | TAP File Format Support (v0 and v1) | Tape / Datasette |
| FR-TAP-003 | Cycle-Accurate Tape Read Timing | Tape / Datasette |
| FR-VIC-007 | Border Behavior Including Open Border Tricks | Video (VIC-II) |
| FR-VIC-008 | Flexible Line Interpretation (FLI) Support | Video (VIC-II) |
| FR-VIC-010 | Sprite Multiplexing DMA Timing | Video (VIC-II) |
---

## Iteration 3

| FR ID | Title | Subsystem |
|-------|-------|-----------|
| FR-CFG-006 | Host-Backed Peripheral Resource Configuration | Configuration / Resources |
| FR-CFG-007 | RAM Initialization and Debug Resource Behavior | Configuration / Resources |
| FR-CPU-005 | 8502 2MHz Mode Support for C128 | CPU |
| FR-CRT-003 | EasyFlash Cartridge with Flash Memory | Cartridges |
| FR-CRT-004 | Action Replay and Retro Replay Cartridge | Cartridges |
| FR-CRT-005 | Final Cartridge III (FC3) | Cartridges |
| FR-DRV-002 | Commodore 1571 Double-Sided Floppy Disk Drive Emulation | Disk Drives |
| FR-DRV-003 | Commodore 1581 3.5" Floppy Disk Drive Emulation | Disk Drives |
| FR-DRV-006 | Fast Loader Compatibility | Disk Drives |
| FR-INP-004 | Lightpen Input | Input Devices |
| FR-INP-005 | Paddle Controller Input | Input Devices |
| FR-PRF-004 | Commodore 128 Machine Profile | Machine Profiles |
| FR-PRF-005 | Commodore VIC-20 Machine Profile | Machine Profiles |
| FR-SID-011 | External Audio Input | Audio (SID) |
| FR-SID-012 | Dual-SID (Stereo SID) Configuration | Audio (SID) |
| FR-SNP-004 | Snapshot Comparison and State Diffing | Snapshot / Replay |
| FR-TAP-004 | Tape Write Support | Tape / Datasette |
| FR-TAP-005 | Turbo Tape Loader Compatibility | Tape / Datasette |
| FR-VIA-001 | VIA 6522 Timer A and Timer B Operation | I/O (VIA 6522) |
| FR-VIA-002 | VIA Shift Register | I/O (VIA 6522) |
| FR-VIA-003 | VIA Port A and Port B with Handshake Protocols | I/O (VIA 6522) |
| FR-VIA-004 | VIC-20 VIA Integration (VIA1 and VIA2) | I/O (VIA 6522) |
| FR-VIA-005 | Disk Drive VIA Integration (1541/1571) | I/O (VIA 6522) |
---

## Iteration 4

| FR ID | Title | Subsystem |
|-------|-------|-----------|
| FR-PRF-006 | Commodore PET Machine Profile | Machine Profiles |
| FR-PRF-007 | Commodore Plus/4 Machine Profile | Machine Profiles |
| FR-PRF-008 | Commodore C16 Machine Profile | Machine Profiles |
