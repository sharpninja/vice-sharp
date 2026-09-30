# Functional Requirements Overview

## Document Information

| Field          | Value                                      |
|----------------|--------------------------------------------|
| Project        | ViceSharp                                  |
| Version        | 0.1.0-draft                                |
| Last Updated   | 2026-04-13                                 |
| Status         | Draft                                      |

## Purpose

This document serves as the master index for all ViceSharp Functional Requirements (FRs). Each FR defines observable, testable behavior that the emulator must exhibit. FRs are organized by subsystem and traced to both the ViceSharp interface layer and the target iteration.

## FR Document Index

| Document                    | Subsystem           | FR Range               |
|-----------------------------|---------------------|------------------------|
| FR-CPU-Emulation.md         | CPU                 | FR-CPU-001 .. FR-CPU-005 |
| FR-Memory-System.md         | Memory              | FR-MEM-001 .. FR-MEM-006 |
| FR-Video-VIC-II.md          | Video (VIC-II)      | FR-VIC-001 .. FR-VIC-010 |
| FR-Audio-SID.md             | Audio (SID)         | FR-SID-001 .. FR-SID-012 |
| FR-IO-CIA.md                | I/O (CIA 6526)      | FR-CIA-001 .. FR-CIA-007 |
| FR-IO-VIA.md                | I/O (VIA 6522)      | FR-VIA-001 .. FR-VIA-005 |
| FR-Storage-Drives.md        | Disk Drives         | FR-DRV-001 .. FR-DRV-006 |
| FR-Storage-Tape.md          | Tape / Datasette    | FR-TAP-001 .. FR-TAP-005 |
| FR-Cartridges.md            | Cartridges          | FR-CRT-001 .. FR-CRT-005 |
| FR-Input.md                 | Input Devices       | FR-INP-001 .. FR-INP-005 |
| FR-Media-Capture.md         | Media Capture       | FR-MED-001 .. FR-MED-005 |
| FR-Monitor.md               | Machine Monitor     | FR-MON-001 .. FR-MON-006 |
| FR-Snapshot.md              | Snapshot / Replay   | FR-SNP-001 .. FR-SNP-004 |
| FR-Machine-Profiles.md      | Machine Profiles    | FR-PRF-001 .. FR-PRF-008 |

## Traceability Matrix -- FR to Interface to Iteration

| FR ID         | Title                               | Primary Interface(s)                | Iteration |
|---------------|-------------------------------------|-------------------------------------|-----------|
| FR-CPU-001    | Full 6502/6510 instruction set      | ICpu, IInstructionDecoder           | 0         |
| FR-CPU-002    | Cycle-accurate execution            | ICpu, IClockedDevice                | 0         |
| FR-CPU-003    | Interrupt handling                  | ICpu, IInterruptController          | 0         |
| FR-CPU-004    | 6510 I/O port                       | ICpu, IMemoryMappedDevice           | 0         |
| FR-CPU-005    | 8502 2MHz mode (C128)               | ICpu, IClockController              | 3         |
| FR-MEM-001    | Address decoding & PLA banking      | IAddressSpace, IBankController      | 0         |
| FR-MEM-002    | RAM under ROM access                | IAddressSpace                       | 0         |
| FR-MEM-003    | Ultimax mode                        | IAddressSpace, ICartridgePort       | 2         |
| FR-MEM-004    | VIC bank switching                  | IAddressSpace, IVicBankSelector     | 1         |
| FR-MEM-005    | Color RAM                           | IAddressSpace                       | 1         |
| FR-MEM-006    | Zero-page / stack behavior          | IAddressSpace                       | 0         |
| FR-VIC-001    | Raster engine & timing              | IVideoChip, IClockedDevice          | 1         |
| FR-VIC-002    | Character display modes             | IVideoChip                          | 1         |
| FR-VIC-003    | Bitmap display modes                | IVideoChip                          | 1         |
| FR-VIC-004    | Sprite engine                       | IVideoChip, ISpriteUnit             | 1         |
| FR-VIC-005    | Sprite collision detection          | IVideoChip, ISpriteUnit             | 1         |
| FR-VIC-006    | Badline handling & DMA stealing     | IVideoChip, IClockedDevice          | 1         |
| FR-VIC-007    | Border behavior & open borders      | IVideoChip                          | 2         |
| FR-VIC-008    | FLI / AFLI support                  | IVideoChip                          | 2         |
| FR-VIC-009    | VIC-II bank switching               | IVideoChip, IVicBankSelector        | 1         |
| FR-VIC-010    | Sprite multiplexing DMA timing      | IVideoChip, ISpriteUnit             | 2         |
| FR-SID-001    | Three-voice oscillator              | IAudioChip, IVoice                  | 1         |
| FR-SID-002    | Waveform generation                 | IAudioChip, IWaveformGenerator      | 1         |
| FR-SID-003    | Combined waveforms                  | IAudioChip, IWaveformGenerator      | 2         |
| FR-SID-004    | Filter (6581 variant)               | IAudioChip, IFilter                 | 1         |
| FR-SID-005    | Filter (8580 variant)               | IAudioChip, IFilter                 | 2         |
| FR-SID-006    | ADSR envelope                       | IAudioChip, IEnvelopeGenerator      | 1         |
| FR-SID-007    | Ring modulation                     | IAudioChip, IVoice                  | 2         |
| FR-SID-008    | Hard sync                           | IAudioChip, IVoice                  | 2         |
| FR-SID-009    | Noise LFSR                          | IAudioChip, IWaveformGenerator      | 1         |
| FR-SID-010    | Digi playback ($D418)               | IAudioChip                          | 2         |
| FR-SID-011    | External audio input                | IAudioChip                          | 3         |
| FR-SID-012    | Dual-SID configuration              | IAudioChip, IAddressSpace           | 3         |
| FR-CIA-001    | Timer A/B with cascading            | ICia, ITimer                        | 1         |
| FR-CIA-002    | TOD clock                           | ICia, ITodClock                     | 1         |
| FR-CIA-003    | Keyboard matrix scanning            | ICia, IKeyboardMatrix               | 1         |
| FR-CIA-004    | Joystick reading                    | ICia, IJoystickPort                 | 1         |
| FR-CIA-005    | Serial port shift register          | ICia, ISerialPort                   | 2         |
| FR-CIA-006    | NMI generation (CIA2)               | ICia, IInterruptController          | 1         |
| FR-CIA-007    | IRQ generation (CIA1)               | ICia, IInterruptController          | 1         |
| FR-VIA-001    | VIA 6522 timer operation            | IVia, ITimer                        | 3         |
| FR-VIA-002    | Shift register                      | IVia                                | 3         |
| FR-VIA-003    | Port A/B handshake modes            | IVia                                | 3         |
| FR-VIA-004    | VIC-20 VIA integration              | IVia, IAddressSpace                 | 3         |
| FR-VIA-005    | Disk drive VIA integration          | IVia, IDiskDrive                    | 3         |
| FR-DRV-001    | 1541 drive emulation                | IDiskDrive, IClockedDevice          | 2         |
| FR-DRV-002    | 1571 drive emulation                | IDiskDrive, IClockedDevice          | 3         |
| FR-DRV-003    | 1581 drive emulation                | IDiskDrive                          | 3         |
| FR-DRV-004    | GCR encoding/decoding               | IDiskDrive, IGcrCodec               | 2         |
| FR-DRV-005    | IEC bus protocol                    | ISerialBus, IDiskDrive              | 2         |
| FR-DRV-006    | Fast loader support                 | IDiskDrive, ISerialBus              | 3         |
| FR-TAP-001    | Datasette motor control             | ITapeUnit                           | 2         |
| FR-TAP-002    | TAP v0/v1 format                    | ITapeUnit, ITapCodec                | 2         |
| FR-TAP-003    | Tape read timing                    | ITapeUnit, IClockedDevice           | 2         |
| FR-TAP-004    | Tape write support                  | ITapeUnit                           | 3         |
| FR-TAP-005    | Turbo loader compatibility          | ITapeUnit                           | 3         |
| FR-CRT-001    | Standard 8K/16K cartridges          | ICartridgePort, IAddressSpace       | 2         |
| FR-CRT-002    | Ocean Type 1 cartridge              | ICartridgePort                      | 2         |
| FR-CRT-003    | EasyFlash cartridge                 | ICartridgePort, IFlashMemory        | 3         |
| FR-CRT-004    | Action Replay / Retro Replay        | ICartridgePort                      | 3         |
| FR-CRT-005    | Final Cartridge III                 | ICartridgePort                      | 3         |
| FR-INP-001    | Keyboard matrix emulation           | IKeyboardMatrix                     | 1         |
| FR-INP-002    | Joystick port 1 & 2                 | IJoystickPort                       | 1         |
| FR-INP-003    | Mouse 1351 proportional             | IMousePort                          | 2         |
| FR-INP-004    | Lightpen input                      | ILightpenPort, IVideoChip           | 3         |
| FR-INP-005    | Paddle controllers                  | IPaddlePort                         | 3         |
| FR-MED-001    | Screenshot capture                  | IMediaCapture                       | 1         |
| FR-MED-002    | Video recording                     | IMediaCapture, IVideoEncoder        | 2         |
| FR-MED-003    | Audio recording                     | IMediaCapture, IAudioEncoder        | 2         |
| FR-MED-004    | Synchronized A/V capture            | IMediaCapture, IMuxer               | 2         |
| FR-MED-005    | Format selection                    | IMediaCapture                       | 2         |
| FR-MON-001    | Disassembly view                    | IMonitor, IDisassembler             | 1         |
| FR-MON-002    | Memory hex/ASCII display            | IMonitor                            | 1         |
| FR-MON-003    | Breakpoint management               | IMonitor, IBreakpointManager        | 1         |
| FR-MON-004    | Register inspection/manipulation    | IMonitor, ICpu                      | 1         |
| FR-MON-005    | Memory bank view selection          | IMonitor, IAddressSpace             | 2         |
| FR-MON-006    | Watch expressions                   | IMonitor                            | 2         |
| FR-SNP-001    | Save machine state                  | ISnapshotManager                    | 1         |
| FR-SNP-002    | Load machine state                  | ISnapshotManager                    | 1         |
| FR-SNP-003    | Deterministic replay                | ISnapshotManager, IReplayEngine     | 2         |
| FR-SNP-004    | Snapshot comparison / diff          | ISnapshotManager                    | 3         |
| FR-PRF-001    | C64 (NMOS) profile                  | IMachineProfile                     | 1         |
| FR-PRF-002    | C64C (new CMOS) profile             | IMachineProfile                     | 2         |
| FR-PRF-003    | SX-64 profile                       | IMachineProfile                     | 2         |
| FR-PRF-004    | C128 profile                        | IMachineProfile                     | 3         |
| FR-PRF-005    | VIC-20 profile                      | IMachineProfile                     | 3         |
| FR-PRF-006    | PET profile                         | IMachineProfile                     | 4         |
| FR-PRF-007    | Plus/4 profile                      | IMachineProfile                     | 4         |
| FR-PRF-008    | C16 profile                         | IMachineProfile                     | 4         |

## Iteration Summary

| Iteration | Focus                                   | FR Count |
|-----------|-----------------------------------------|----------|
| 0         | CPU core, address space, foundations    | 6        |
| 1         | C64 MVP (video, audio, I/O, input)     | 24       |
| 2         | Advanced features, drives, cartridges  | 24       |
| 3         | Additional machines, peripherals       | 18       |
| 4         | PET, Plus/4, C16 profiles              | 3        |
