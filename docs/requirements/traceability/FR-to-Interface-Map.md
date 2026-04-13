# FR-to-Interface Traceability Map

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Project        | ViceSharp                      |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-04-13                     |

## Purpose

This document maps each Functional Requirement to the ViceSharp interface(s) it traces to. This establishes which interfaces must be implemented to satisfy each FR, and conversely which FRs drive the design of each interface.

---

## CPU Subsystem

| FR ID       | FR Title                          | Primary Interfaces                          |
|-------------|-----------------------------------|---------------------------------------------|
| FR-CPU-001  | Full 6502/6510 instruction set    | `ICpu`, `IInstructionDecoder`               |
| FR-CPU-002  | Cycle-accurate execution          | `ICpu`, `IClockedDevice`                    |
| FR-CPU-003  | Interrupt handling                | `ICpu`, `IInterruptController`              |
| FR-CPU-004  | 6510 I/O port                     | `ICpu`, `IMemoryMappedDevice`               |
| FR-CPU-005  | 8502 2MHz mode (C128)             | `ICpu`, `IClockController`                  |

## Memory Subsystem

| FR ID       | FR Title                          | Primary Interfaces                          |
|-------------|-----------------------------------|---------------------------------------------|
| FR-MEM-001  | Address decoding & PLA banking    | `IAddressSpace`, `IBankController`          |
| FR-MEM-002  | RAM under ROM access              | `IAddressSpace`                             |
| FR-MEM-003  | Ultimax mode                      | `IAddressSpace`, `ICartridgePort`           |
| FR-MEM-004  | VIC bank switching                | `IAddressSpace`, `IVicBankSelector`         |
| FR-MEM-005  | Color RAM                         | `IAddressSpace`                             |
| FR-MEM-006  | Zero page / stack behavior        | `IAddressSpace`                             |

## Video Subsystem (VIC-II)

| FR ID       | FR Title                          | Primary Interfaces                          |
|-------------|-----------------------------------|---------------------------------------------|
| FR-VIC-001  | Raster engine & timing            | `IVideoChip`, `IClockedDevice`              |
| FR-VIC-002  | Character display modes           | `IVideoChip`                                |
| FR-VIC-003  | Bitmap display modes              | `IVideoChip`                                |
| FR-VIC-004  | Sprite engine                     | `IVideoChip`, `ISpriteUnit`                 |
| FR-VIC-005  | Sprite collision detection        | `IVideoChip`, `ISpriteUnit`                 |
| FR-VIC-006  | Badline handling & DMA stealing   | `IVideoChip`, `IClockedDevice`              |
| FR-VIC-007  | Border behavior & open borders    | `IVideoChip`                                |
| FR-VIC-008  | FLI / AFLI support                | `IVideoChip`                                |
| FR-VIC-009  | VIC-II bank switching             | `IVideoChip`, `IVicBankSelector`            |
| FR-VIC-010  | Sprite multiplexing DMA timing    | `IVideoChip`, `ISpriteUnit`                 |

## Audio Subsystem (SID)

| FR ID       | FR Title                          | Primary Interfaces                          |
|-------------|-----------------------------------|---------------------------------------------|
| FR-SID-001  | Three-voice oscillator            | `IAudioChip`, `IVoice`                      |
| FR-SID-002  | Waveform generation               | `IAudioChip`, `IWaveformGenerator`          |
| FR-SID-003  | Combined waveforms                | `IAudioChip`, `IWaveformGenerator`          |
| FR-SID-004  | Filter (6581 variant)             | `IAudioChip`, `IFilter`                     |
| FR-SID-005  | Filter (8580 variant)             | `IAudioChip`, `IFilter`                     |
| FR-SID-006  | ADSR envelope                     | `IAudioChip`, `IEnvelopeGenerator`          |
| FR-SID-007  | Ring modulation                   | `IAudioChip`, `IVoice`                      |
| FR-SID-008  | Hard sync                         | `IAudioChip`, `IVoice`                      |
| FR-SID-009  | Noise LFSR                        | `IAudioChip`, `IWaveformGenerator`          |
| FR-SID-010  | Digi playback ($D418)             | `IAudioChip`                                |
| FR-SID-011  | External audio input              | `IAudioChip`                                |
| FR-SID-012  | Dual-SID configuration            | `IAudioChip`, `IAddressSpace`               |

## I/O Subsystem (CIA)

| FR ID       | FR Title                          | Primary Interfaces                          |
|-------------|-----------------------------------|---------------------------------------------|
| FR-CIA-001  | Timer A/B with cascading          | `ICia`, `ITimer`                            |
| FR-CIA-002  | TOD clock                         | `ICia`, `ITodClock`                         |
| FR-CIA-003  | Keyboard matrix scanning          | `ICia`, `IKeyboardMatrix`                   |
| FR-CIA-004  | Joystick reading                  | `ICia`, `IJoystickPort`                     |
| FR-CIA-005  | Serial port shift register        | `ICia`, `ISerialPort`                       |
| FR-CIA-006  | NMI generation (CIA2)             | `ICia`, `IInterruptController`              |
| FR-CIA-007  | IRQ generation (CIA1)             | `ICia`, `IInterruptController`              |

## I/O Subsystem (VIA)

| FR ID       | FR Title                          | Primary Interfaces                          |
|-------------|-----------------------------------|---------------------------------------------|
| FR-VIA-001  | VIA 6522 timer operation          | `IVia`, `ITimer`                            |
| FR-VIA-002  | Shift register                    | `IVia`                                      |
| FR-VIA-003  | Port A/B handshake modes          | `IVia`                                      |
| FR-VIA-004  | VIC-20 VIA integration            | `IVia`, `IAddressSpace`                     |
| FR-VIA-005  | Disk drive VIA integration        | `IVia`, `IDiskDrive`                        |

## Storage -- Disk Drives

| FR ID       | FR Title                          | Primary Interfaces                          |
|-------------|-----------------------------------|---------------------------------------------|
| FR-DRV-001  | 1541 drive emulation              | `IDiskDrive`, `IClockedDevice`              |
| FR-DRV-002  | 1571 drive emulation              | `IDiskDrive`, `IClockedDevice`              |
| FR-DRV-003  | 1581 drive emulation              | `IDiskDrive`                                |
| FR-DRV-004  | GCR encoding/decoding             | `IDiskDrive`, `IGcrCodec`                   |
| FR-DRV-005  | IEC bus protocol                  | `ISerialBus`, `IDiskDrive`                  |
| FR-DRV-006  | Fast loader support               | `IDiskDrive`, `ISerialBus`                  |

## Storage -- Tape

| FR ID       | FR Title                          | Primary Interfaces                          |
|-------------|-----------------------------------|---------------------------------------------|
| FR-TAP-001  | Datasette motor control           | `ITapeUnit`                                 |
| FR-TAP-002  | TAP format support                | `ITapeUnit`, `ITapCodec`                    |
| FR-TAP-003  | Tape read timing                  | `ITapeUnit`, `IClockedDevice`               |
| FR-TAP-004  | Tape write support                | `ITapeUnit`                                 |
| FR-TAP-005  | Turbo loader compatibility        | `ITapeUnit`                                 |

## Cartridges

| FR ID       | FR Title                          | Primary Interfaces                          |
|-------------|-----------------------------------|---------------------------------------------|
| FR-CRT-001  | Standard 8K/16K cartridges        | `ICartridgePort`, `IAddressSpace`           |
| FR-CRT-002  | Ocean Type 1 cartridge            | `ICartridgePort`                            |
| FR-CRT-003  | EasyFlash cartridge               | `ICartridgePort`, `IFlashMemory`            |
| FR-CRT-004  | Action Replay / Retro Replay      | `ICartridgePort`                            |
| FR-CRT-005  | Final Cartridge III               | `ICartridgePort`                            |

## Input Devices

| FR ID       | FR Title                          | Primary Interfaces                          |
|-------------|-----------------------------------|---------------------------------------------|
| FR-INP-001  | Keyboard matrix emulation         | `IKeyboardMatrix`                           |
| FR-INP-002  | Joystick port 1 & 2              | `IJoystickPort`                             |
| FR-INP-003  | Mouse 1351 proportional           | `IMousePort`                                |
| FR-INP-004  | Lightpen input                    | `ILightpenPort`, `IVideoChip`              |
| FR-INP-005  | Paddle controllers                | `IPaddlePort`                               |

## Media Capture

| FR ID       | FR Title                          | Primary Interfaces                          |
|-------------|-----------------------------------|---------------------------------------------|
| FR-MED-001  | Screenshot capture                | `IMediaCapture`                             |
| FR-MED-002  | Video recording                   | `IMediaCapture`, `IVideoEncoder`            |
| FR-MED-003  | Audio recording                   | `IMediaCapture`, `IAudioEncoder`            |
| FR-MED-004  | Synchronized A/V capture          | `IMediaCapture`, `IMuxer`                   |
| FR-MED-005  | Format selection                  | `IMediaCapture`                             |

## Monitor / Debugger

| FR ID       | FR Title                          | Primary Interfaces                          |
|-------------|-----------------------------------|---------------------------------------------|
| FR-MON-001  | Disassembly view                  | `IMonitor`, `IDisassembler`                 |
| FR-MON-002  | Memory hex/ASCII display          | `IMonitor`                                  |
| FR-MON-003  | Breakpoint management             | `IMonitor`, `IBreakpointManager`            |
| FR-MON-004  | Register inspection/manipulation  | `IMonitor`, `ICpu`                          |
| FR-MON-005  | Memory bank view selection        | `IMonitor`, `IAddressSpace`                 |
| FR-MON-006  | Watch expressions                 | `IMonitor`                                  |

## Snapshot / Replay

| FR ID       | FR Title                          | Primary Interfaces                          |
|-------------|-----------------------------------|---------------------------------------------|
| FR-SNP-001  | Save machine state                | `ISnapshotManager`                          |
| FR-SNP-002  | Load machine state                | `ISnapshotManager`                          |
| FR-SNP-003  | Deterministic replay              | `ISnapshotManager`, `IReplayEngine`         |
| FR-SNP-004  | Snapshot comparison / diff        | `ISnapshotManager`                          |

## Machine Profiles

| FR ID       | FR Title                          | Primary Interfaces                          |
|-------------|-----------------------------------|---------------------------------------------|
| FR-PRF-001  | C64 (NMOS) profile                | `IMachineProfile`                           |
| FR-PRF-002  | C64C (new CMOS) profile           | `IMachineProfile`                           |
| FR-PRF-003  | SX-64 profile                     | `IMachineProfile`                           |
| FR-PRF-004  | C128 profile                      | `IMachineProfile`                           |
| FR-PRF-005  | VIC-20 profile                    | `IMachineProfile`                           |
| FR-PRF-006  | PET profile                       | `IMachineProfile`                           |
| FR-PRF-007  | Plus/4 profile                    | `IMachineProfile`                           |
| FR-PRF-008  | C16 profile                       | `IMachineProfile`                           |

---

## Interface Coverage Summary

The following table lists all interfaces and the count of FRs that trace to each.

| Interface               | FR Count | Key Subsystem(s)              |
|-------------------------|----------|-------------------------------|
| `ICpu`                  | 6        | CPU, Monitor                  |
| `IClockedDevice`        | 7        | CPU, VIC-II, Drives, Tape     |
| `IAddressSpace`         | 10       | Memory, VIA, Cartridges, SID  |
| `IVideoChip`            | 11       | VIC-II, Lightpen              |
| `IAudioChip`            | 12       | SID                           |
| `ICia`                  | 7        | CIA                           |
| `IVia`                  | 5        | VIA                           |
| `IInterruptController`  | 4        | CPU, CIA                      |
| `ISpriteUnit`           | 3        | VIC-II                        |
| `ICartridgePort`        | 6        | Cartridges, Memory            |
| `IDiskDrive`            | 6        | Drives, VIA                   |
| `ITapeUnit`             | 5        | Tape                          |
| `IMediaCapture`         | 5        | Media                         |
| `IMonitor`              | 6        | Monitor                       |
| `ISnapshotManager`      | 4        | Snapshot                      |
| `IMachineProfile`       | 8        | Profiles                      |
| `IKeyboardMatrix`       | 2        | Input, CIA                    |
| `IJoystickPort`         | 2        | Input, CIA                    |
| `ISerialBus`            | 2        | Drives                        |
| `IFilter`               | 2        | SID                           |
| `IVoice`                | 3        | SID                           |
| `IWaveformGenerator`    | 3        | SID                           |
| `IEnvelopeGenerator`    | 1        | SID                           |
| `ITimer`                | 3        | CIA, VIA                      |
| `IBankController`       | 1        | Memory                        |
| `IVicBankSelector`      | 2        | Memory, VIC-II                |
| `IReplayEngine`         | 1        | Snapshot                      |
| `IBreakpointManager`    | 1        | Monitor                       |
| `IDisassembler`         | 1        | Monitor                       |
