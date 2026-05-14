# Functional Requirements Overview

## Document Information

| Field          | Value                                      |
|----------------|--------------------------------------------|
| Project        | ViceSharp                                  |
| Version        | 0.1.0-draft                                |
| Last Updated   | 2026-05-13                                 |
| Status         | Draft                                      |

## Purpose

This document serves as the master index for all ViceSharp Functional Requirements (FRs). FRs define observable, testable emulator behavior. Classic VICE documentation is used as the source corpus for emulator-visible FR behavior; Vice-Sharp architecture documents constrain TRs and implementation boundaries.

## FR Document Index

| Document | Subsystem | FR Range |
|----------|-----------|----------|
| FR-Audio-SID.md | Audio (SID) | FR-SID-001 .. FR-SID-012 |
| FR-CPU-Emulation.md | CPU | FR-CPU-001 .. FR-CPU-005 |
| FR-Cartridges.md | Cartridges | FR-CRT-001 .. FR-CRT-005 |
| FR-Configuration-Resources.md | Configuration / Resources | FR-CFG-001 .. FR-CFG-008 |
| FR-Host-UI-Boundary.md | Host / UI Boundary | FR-HOST-001 .. FR-UI-004 |
| FR-IO-CIA.md | I/O (CIA 6526) | FR-CIA-001 .. FR-CIA-007 |
| FR-IO-VIA.md | I/O (VIA 6522) | FR-VIA-001 .. FR-VIA-005 |
| FR-Input.md | Input Devices | FR-INP-001 .. FR-INP-006 |
| FR-Machine-Profiles.md | Machine Profiles | FR-PRF-001 .. FR-PRF-008 |
| FR-Media-Capture.md | Media Capture | FR-MED-001 .. FR-MED-005 |
| FR-Memory-System.md | Memory | FR-MEM-001 .. FR-MEM-006 |
| FR-Monitor.md | Machine Monitor | FR-MON-001 .. FR-MON-006 |
| FR-Snapshot.md | Snapshot / Replay | FR-SNP-001 .. FR-SNP-004 |
| FR-Storage-Drives.md | Disk Drives | FR-DRV-001 .. FR-DRV-006 |
| FR-Storage-Tape.md | Tape / Datasette | FR-TAP-001 .. FR-TAP-005 |
| FR-Video-VIC-II.md | Video (VIC-II) | FR-VIC-001 .. FR-VIC-010 |

## Traceability Matrix -- FR to Interface to Iteration

| FR ID | Title | Primary Interface(s) | Iteration |
|-------|-------|----------------------|-----------|
| FR-CFG-001 | Resource File and Command-Line Configuration | `IEmulatorSession`, `IConfigurationStore`, `HostControlService` | 2 |
| FR-CFG-002 | ROM and Romset Selection | `IArchitectureDescriptor`, `IRomProvider`, `HostControlService` | 1 |
| FR-CFG-003 | Palette Selection and Color Resource Handling | `IVideoChip`, `IFrameSink`, `HostStateService` | 2 |
| FR-CFG-004 | Hotkey Configuration and Action Dispatch | `UiHostClient`, `HostControlService`, `HostMediaService`, `HostMonitorService` | 2 |
| FR-CFG-005 | Autostart and Program Launch Handling | `HostMediaService`, `HostInputService`, `HostControlService`, `IAutostartService` | 2 |
| FR-CFG-006 | Host-Backed Peripheral Resource Configuration | `IConfigurationStore`, `HostControlService`, `HostStateService` | 3 |
| FR-CFG-007 | RAM Initialization and Debug Resource Behavior | `IMachine`, `ISnapshotManager`, `HostStateService` | 3 |
| FR-CFG-008 | Performance Limiter Configuration | `HostControlService`, `HostStatusService`, `IClockedDevice` | 1 |
| FR-CIA-001 | CIA Timer A and Timer B with Cascade Mode | `ICia`, `ITimer` | 1 |
| FR-CIA-002 | Time-of-Day Clock | `ICia`, `ITodClock` | 1 |
| FR-CIA-003 | Keyboard Matrix Scanning via CIA1 | `ICia`, `IKeyboardMatrix` | 1 |
| FR-CIA-004 | Joystick Port Reading via CIA1 | `ICia`, `IJoystickPort` | 1 |
| FR-CIA-005 | CIA Serial Port Shift Register | `ICia`, `ISerialPort` | 2 |
| FR-CIA-006 | NMI Generation from CIA2 | `ICia`, `IInterruptController` | 1 |
| FR-CIA-007 | IRQ Generation from CIA1 | `ICia`, `IInterruptController` | 1 |
| FR-CPU-001 | Full 6502/6510 Instruction Set Including Undocumented Opcodes | `ICpu`, `IInstructionDecoder` | 0 |
| FR-CPU-002 | Cycle-Accurate Execution Timing | `ICpu`, `IClockedDevice` | 0 |
| FR-CPU-003 | Interrupt Handling with Correct Timing | `ICpu`, `IInterruptController` | 0 |
| FR-CPU-004 | 6510 I/O Port at Addresses $0000/$0001 | `ICpu`, `IMemoryMappedDevice` | 0 |
| FR-CPU-005 | 8502 2MHz Mode Support for C128 | `ICpu`, `IClockController` | 3 |
| FR-CRT-001 | Standard 8K and 16K Cartridge Support | `ICartridgePort`, `IAddressSpace` | 2 |
| FR-CRT-002 | Ocean Type 1 Bank-Switching Cartridge | `ICartridgePort` | 2 |
| FR-CRT-003 | EasyFlash Cartridge with Flash Memory | `ICartridgePort`, `IFlashMemory` | 3 |
| FR-CRT-004 | Action Replay and Retro Replay Cartridge | `ICartridgePort` | 3 |
| FR-CRT-005 | Final Cartridge III (FC3) | `ICartridgePort` | 3 |
| FR-DRV-001 | Commodore 1541 Single Floppy Disk Drive Emulation | `IDiskDrive`, `IClockedDevice` | 2 |
| FR-DRV-002 | Commodore 1571 Double-Sided Floppy Disk Drive Emulation | `IDiskDrive`, `IClockedDevice` | 3 |
| FR-DRV-003 | Commodore 1581 3.5" Floppy Disk Drive Emulation | `IDiskDrive` | 3 |
| FR-DRV-004 | Group Code Recording Encoding and Decoding | `IDiskDrive`, `IGcrCodec` | 2 |
| FR-DRV-005 | IEC Serial Bus Protocol | `ISerialBus`, `IDiskDrive` | 2 |
| FR-DRV-006 | Fast Loader Compatibility | `IDiskDrive`, `ISerialBus` | 3 |
| FR-HOST-001 | Host-Owned Emulator Session Lifecycle | `IEmulator`, `IMachine`, `IArchitectureDescriptor`, `HostControlService` | 1 |
| FR-HOST-002 | Host-Mediated Disk, Tape, and Cartridge Attachment | `IDiskDrive`, `ITapeUnit`, `ICartridgePort`, `HostMediaService` | 2 |
| FR-HOST-003 | Host-Streamed Video Frames for Remote UI Clients | `IFrameSink`, `HostOutputService`, `VideoService` | 1 |
| FR-HOST-004 | Host-Normalized Keyboard, Joystick, and Machine Control | `IKeyboardMatrix`, `IJoystickPort`, `IInputSource`, `HostInputService`, `HostControlService` | 1 |
| FR-HOST-005 | Host-Owned Snapshot, Screenshot, and Diagnostic Operations | `ISnapshotManager`, `IMediaCapture`, `IMonitor`, `HostStateService` | 1 |
| FR-HOST-006 | Host Runtime Status and Control Telemetry | `HostControlService`, `HostStatusService`, `IMachine`, `ICpu`, `IClockedDevice` | 1 |
| FR-INP-001 | Keyboard Matrix Emulation | `IKeyboardMatrix` | 1 |
| FR-INP-002 | Joystick Port 1 and Port 2 Emulation | `IJoystickPort` | 1 |
| FR-INP-003 | Commodore 1351 Proportional Mouse Emulation | `IMousePort` | 2 |
| FR-INP-004 | Lightpen Input | `ILightpenPort`, `IVideoChip` | 3 |
| FR-INP-005 | Paddle Controller Input | `IPaddlePort` | 3 |
| FR-INP-006 | VICE VKM Keymap Selection and Real-Time Keyboard Translation | `IKeyboardInputMap`, `IKeyboardInputMapSelection`, `IKeyboardMatrix`, `IMachineKeyboardInput`, `HostInputService` | 1 |
| FR-MED-001 | Screenshot Capture (PNG/BMP) | `IMediaCapture` | 1 |
| FR-MED-002 | Video Recording (MP4 via FFmpeg) | `IMediaCapture`, `IVideoEncoder` | 2 |
| FR-MED-003 | Audio Recording (WAV/FLAC) | `IMediaCapture`, `IAudioEncoder` | 2 |
| FR-MED-004 | Synchronized Audio/Video Capture | `IMediaCapture`, `IMuxer` | 2 |
| FR-MED-005 | Output Format Selection and Configuration | `IMediaCapture` | 2 |
| FR-MEM-001 | Address Decoding and PLA Banking Configuration | `IAddressSpace`, `IBankController` | 0 |
| FR-MEM-002 | RAM Under ROM Access | `IAddressSpace` | 0 |
| FR-MEM-003 | Ultimax Cartridge Mode | `IAddressSpace`, `ICartridgePort` | 2 |
| FR-MEM-004 | VIC-II Bank Switching via CIA2 | `IAddressSpace`, `IVicBankSelector` | 1 |
| FR-MEM-005 | Color RAM ($D800-$DBFF) | `IAddressSpace` | 1 |
| FR-MEM-006 | Zero Page and Stack Page Behavior | `IAddressSpace` | 0 |
| FR-MON-001 | Real-Time Disassembly View | `IMonitor`, `IDisassembler` | 1 |
| FR-MON-002 | Memory Hex and ASCII Display | `IMonitor` | 1 |
| FR-MON-003 | Breakpoint Management | `IMonitor`, `IBreakpointManager` | 1 |
| FR-MON-004 | CPU Register Inspection and Manipulation | `IMonitor`, `ICpu` | 1 |
| FR-MON-005 | Memory Bank View Selection | `IMonitor`, `IAddressSpace` | 2 |
| FR-MON-006 | Watch Expressions | `IMonitor` | 2 |
| FR-PRF-001 | Commodore 64 (Original NMOS) Machine Profile | `IMachineProfile` | 1 |
| FR-PRF-002 | Commodore 64C Machine Profile | `IMachineProfile` | 2 |
| FR-PRF-003 | Commodore SX-64 Machine Profile | `IMachineProfile` | 2 |
| FR-PRF-004 | Commodore 128 Machine Profile | `IMachineProfile` | 3 |
| FR-PRF-005 | Commodore VIC-20 Machine Profile | `IMachineProfile` | 3 |
| FR-PRF-006 | Commodore PET Machine Profile | `IMachineProfile` | 4 |
| FR-PRF-007 | Commodore Plus/4 Machine Profile | `IMachineProfile` | 4 |
| FR-PRF-008 | Commodore C16 Machine Profile | `IMachineProfile` | 4 |
| FR-SID-001 | Three Independent Voice Oscillators | `IAudioChip`, `IVoice` | 1 |
| FR-SID-002 | Waveform Generation (Triangle, Sawtooth, Pulse, Noise) | `IAudioChip`, `IWaveformGenerator` | 1 |
| FR-SID-003 | Combined Waveform Output | `IAudioChip`, `IWaveformGenerator` | 2 |
| FR-SID-004 | 6581 SID Filter Emulation | `IAudioChip`, `IFilter` | 1 |
| FR-SID-005 | 8580 SID Filter Emulation | `IAudioChip`, `IFilter` | 2 |
| FR-SID-006 | ADSR Envelope Generator | `IAudioChip`, `IEnvelopeGenerator` | 1 |
| FR-SID-007 | Ring Modulation | `IAudioChip`, `IVoice` | 2 |
| FR-SID-008 | Hard Sync (Oscillator Synchronization) | `IAudioChip`, `IVoice` | 2 |
| FR-SID-009 | Noise Waveform Linear Feedback Shift Register | `IAudioChip`, `IWaveformGenerator` | 1 |
| FR-SID-010 | Direct Digital Sample Playback via Volume Register | `IAudioChip` | 2 |
| FR-SID-011 | External Audio Input | `IAudioChip` | 3 |
| FR-SID-012 | Dual-SID (Stereo SID) Configuration | `IAudioChip`, `IAddressSpace` | 3 |
| FR-SNP-001 | Save Complete Machine State to Snapshot | `ISnapshotManager` | 1 |
| FR-SNP-002 | Load Machine State from Snapshot | `ISnapshotManager` | 1 |
| FR-SNP-003 | Deterministic Input Replay | `ISnapshotManager`, `IReplayEngine` | 2 |
| FR-SNP-004 | Snapshot Comparison and State Diffing | `ISnapshotManager` | 3 |
| FR-TAP-001 | Datasette Motor Control | `ITapeUnit` | 2 |
| FR-TAP-002 | TAP File Format Support (v0 and v1) | `ITapeUnit`, `ITapCodec` | 2 |
| FR-TAP-003 | Cycle-Accurate Tape Read Timing | `ITapeUnit`, `IClockedDevice` | 2 |
| FR-TAP-004 | Tape Write Support | `ITapeUnit` | 3 |
| FR-TAP-005 | Turbo Tape Loader Compatibility | `ITapeUnit` | 3 |
| FR-UI-001 | Dockable Host UI Control Client | `UiHostClient`, `HostControlService`, `HostOutputService`, `HostInputService`, `AvaloniaRenderSurface` | 1 |
| FR-UI-002 | Emulator Status and Machine Control Bar | `UiHostClient`, `HostControlService`, `HostStatusService` | 1 |
| FR-UI-003 | Collapsible Tabbed Emulator Sidebar | `UiHostClient`, `HostMediaService`, `HostInputService`, `HostControlService` | 1 |
| FR-UI-004 | Docked and Pop-Out Monitor Control | `IMonitor`, `HostMonitorService`, `UiHostClient` | 1 |
| FR-VIA-001 | VIA 6522 Timer A and Timer B Operation | `IVia`, `ITimer` | 3 |
| FR-VIA-002 | VIA Shift Register | `IVia` | 3 |
| FR-VIA-003 | VIA Port A and Port B with Handshake Protocols | `IVia` | 3 |
| FR-VIA-004 | VIC-20 VIA Integration (VIA1 and VIA2) | `IVia`, `IAddressSpace` | 3 |
| FR-VIA-005 | Disk Drive VIA Integration (1541/1571) | `IVia`, `IDiskDrive` | 3 |
| FR-VIC-001 | Raster Engine with PAL/NTSC Timing | `IVideoChip`, `IClockedDevice` | 1 |
| FR-VIC-002 | Character Display Modes (Standard, Multicolor, ECM) | `IVideoChip` | 1 |
| FR-VIC-003 | Bitmap Display Modes (Standard, Multicolor) | `IVideoChip` | 1 |
| FR-VIC-004 | Sprite Engine (8 Hardware Sprites) | `IVideoChip`, `ISpriteUnit` | 1 |
| FR-VIC-005 | Sprite Collision Detection | `IVideoChip`, `ISpriteUnit` | 1 |
| FR-VIC-006 | Badline Handling and CPU DMA Cycle Stealing | `IVideoChip`, `IClockedDevice` | 1 |
| FR-VIC-007 | Border Behavior Including Open Border Tricks | `IVideoChip` | 2 |
| FR-VIC-008 | Flexible Line Interpretation (FLI) Support | `IVideoChip` | 2 |
| FR-VIC-009 | VIC-II Bank Switching (See also FR-MEM-004) | `IVideoChip`, `IVicBankSelector` | 1 |
| FR-VIC-010 | Sprite Multiplexing DMA Timing | `IVideoChip`, `ISpriteUnit` | 2 |

## Source Corpus

- Primary FR source corpus: `native/vice/vice/doc`.
- Detailed source inventory: `docs/requirements/sources/VICE-Source-Manifest.md`.
- Current x64sc parity backfill coverage: `docs/requirements/backfill/X64SC-Requirement-Coverage.md`.
- Required x64sc model selector/profile matrix: `docs/requirements/backfill/X64SC-Model-Matrix.md`.
- Technical and test requirements are derived from Vice-Sharp architecture and verification strategy, not from VICE implementation choices.
