# FR-to-Interface Traceability Map

## Document Information

| Field | Value |
|-------|-------|
| Project | ViceSharp |
| Version | 0.1.0-draft |
| Last Updated | 2026-05-13 |

## Purpose

This map links Functional Requirements to the ViceSharp interfaces or host service surfaces expected to satisfy them.

---

## Audio (SID)

| FR ID | FR Title | Primary Interfaces |
|-------|----------|--------------------|
| FR-SID-001 | Three Independent Voice Oscillators | `IAudioChip`, `IVoice` |
| FR-SID-002 | Waveform Generation (Triangle, Sawtooth, Pulse, Noise) | `IAudioChip`, `IWaveformGenerator` |
| FR-SID-003 | Combined Waveform Output | `IAudioChip`, `IWaveformGenerator` |
| FR-SID-004 | 6581 SID Filter Emulation | `IAudioChip`, `IFilter` |
| FR-SID-005 | 8580 SID Filter Emulation | `IAudioChip`, `IFilter` |
| FR-SID-006 | ADSR Envelope Generator | `IAudioChip`, `IEnvelopeGenerator` |
| FR-SID-007 | Ring Modulation | `IAudioChip`, `IVoice` |
| FR-SID-008 | Hard Sync (Oscillator Synchronization) | `IAudioChip`, `IVoice` |
| FR-SID-009 | Noise Waveform Linear Feedback Shift Register | `IAudioChip`, `IWaveformGenerator` |
| FR-SID-010 | Direct Digital Sample Playback via Volume Register | `IAudioChip` |
| FR-SID-011 | External Audio Input | `IAudioChip` |
| FR-SID-012 | Dual-SID (Stereo SID) Configuration | `IAudioChip`, `IAddressSpace` |
| FR-SID-013 | SID Audio Backend Wiring | `IAudioChip`, `IAudioBackend` |
| FR-SID-014 | VICE-Compatible Signed SID Voice Output and Demo Pacing | `IAudioChip`, `IAudioBackend` |
---

## CPU

| FR ID | FR Title | Primary Interfaces |
|-------|----------|--------------------|
| FR-CPU-001 | Full 6502/6510 Instruction Set Including Undocumented Opcodes | `ICpu`, `IInstructionDecoder` |
| FR-CPU-002 | Cycle-Accurate Execution Timing | `ICpu`, `IClockedDevice` |
| FR-CPU-003 | Interrupt Handling with Correct Timing | `ICpu`, `IInterruptController` |
| FR-CPU-004 | 6510 I/O Port at Addresses $0000/$0001 | `ICpu`, `IMemoryMappedDevice` |
| FR-CPU-005 | 8502 2MHz Mode Support for C128 | `ICpu`, `IClockController` |
---

## Cartridges

| FR ID | FR Title | Primary Interfaces |
|-------|----------|--------------------|
| FR-CRT-001 | Standard 8K and 16K Cartridge Support | `ICartridgePort`, `IAddressSpace` |
| FR-CRT-002 | Ocean Type 1 Bank-Switching Cartridge | `ICartridgePort` |
| FR-CRT-003 | EasyFlash Cartridge with Flash Memory | `ICartridgePort`, `IFlashMemory` |
| FR-CRT-004 | Action Replay and Retro Replay Cartridge | `ICartridgePort` |
| FR-CRT-005 | Final Cartridge III (FC3) | `ICartridgePort` |
---

## Configuration / Resources

| FR ID | FR Title | Primary Interfaces |
|-------|----------|--------------------|
| FR-CFG-001 | Resource File and Command-Line Configuration | `IEmulatorSession`, `IConfigurationStore`, `HostControlService` |
| FR-CFG-002 | ROM and Romset Selection | `IArchitectureDescriptor`, `IRomProvider`, `HostControlService` |
| FR-CFG-003 | Palette Selection and Color Resource Handling | `IVideoChip`, `IFrameSink`, `HostStateService` |
| FR-CFG-004 | Hotkey Configuration and Action Dispatch | `UiHostClient`, `HostControlService`, `HostMediaService`, `HostMonitorService` |
| FR-CFG-005 | Autostart and Program Launch Handling | `HostMediaService`, `HostInputService`, `HostControlService`, `IAutostartService` |
| FR-CFG-006 | Host-Backed Peripheral Resource Configuration | `IConfigurationStore`, `HostControlService`, `HostStateService` |
| FR-CFG-007 | RAM Initialization and Debug Resource Behavior | `IMachine`, `ISnapshotManager`, `HostStateService` |
| FR-CFG-008 | Performance Limiter Configuration | `HostControlService`, `HostStatusService`, `IClockedDevice` |
---

## Host / UI Boundary

| FR ID | FR Title | Primary Interfaces |
|-------|----------|--------------------|
| FR-HOST-001 | Host-Owned Emulator Session Lifecycle | `IEmulator`, `IMachine`, `IArchitectureDescriptor`, `HostControlService` |
| FR-HOST-002 | Host-Mediated Disk, Tape, and Cartridge Attachment | `IDiskDrive`, `ITapeUnit`, `ICartridgePort`, `HostMediaService` |
| FR-HOST-003 | Host-Streamed Video Frames for Remote UI Clients | `IFrameSink`, `HostOutputService`, `VideoService` |
| FR-HOST-004 | Host-Normalized Keyboard, Joystick, and Machine Control | `IKeyboardMatrix`, `IJoystickPort`, `IInputSource`, `HostInputService`, `HostControlService` |
| FR-HOST-005 | Host-Owned Snapshot, Screenshot, and Diagnostic Operations | `ISnapshotManager`, `IMediaCapture`, `IMonitor`, `HostStateService` |
| FR-HOST-006 | Host Runtime Status and Control Telemetry | `HostControlService`, `HostStatusService`, `IMachine`, `ICpu`, `IClockedDevice` |
| FR-UI-001 | Dockable Host UI Control Client | `UiHostClient`, `HostControlService`, `HostOutputService`, `HostInputService`, `AvaloniaRenderSurface` |
| FR-UI-002 | Emulator Status and Machine Control Bar | `UiHostClient`, `HostControlService`, `HostStatusService` |
| FR-UI-003 | Collapsible Tabbed Emulator Sidebar | `UiHostClient`, `HostMediaService`, `HostInputService`, `HostControlService` |
| FR-UI-004 | Docked and Pop-Out Monitor Control | `IMonitor`, `HostMonitorService`, `UiHostClient` |
---

## I/O (CIA 6526)

| FR ID | FR Title | Primary Interfaces |
|-------|----------|--------------------|
| FR-CIA-001 | CIA Timer A and Timer B with Cascade Mode | `ICia`, `ITimer` |
| FR-CIA-002 | Time-of-Day Clock | `ICia`, `ITodClock` |
| FR-CIA-003 | Keyboard Matrix Scanning via CIA1 | `ICia`, `IKeyboardMatrix` |
| FR-CIA-004 | Joystick Port Reading via CIA1 | `ICia`, `IJoystickPort` |
| FR-CIA-005 | CIA Serial Port Shift Register | `ICia`, `ISerialPort` |
| FR-CIA-006 | NMI Generation from CIA2 | `ICia`, `IInterruptController` |
| FR-CIA-007 | IRQ Generation from CIA1 | `ICia`, `IInterruptController` |
---

## I/O (VIA 6522)

| FR ID | FR Title | Primary Interfaces |
|-------|----------|--------------------|
| FR-VIA-001 | VIA 6522 Timer A and Timer B Operation | `IVia`, `ITimer` |
| FR-VIA-002 | VIA Shift Register | `IVia` |
| FR-VIA-003 | VIA Port A and Port B with Handshake Protocols | `IVia` |
| FR-VIA-004 | VIC-20 VIA Integration (VIA1 and VIA2) | `IVia`, `IAddressSpace` |
| FR-VIA-005 | Disk Drive VIA Integration (1541/1571) | `IVia`, `IDiskDrive` |
---

## Input Devices

| FR ID | FR Title | Primary Interfaces |
|-------|----------|--------------------|
| FR-INP-001 | Keyboard Matrix Emulation | `IKeyboardMatrix` |
| FR-INP-002 | Joystick Port 1 and Port 2 Emulation | `IJoystickPort` |
| FR-INP-003 | Commodore 1351 Proportional Mouse Emulation | `IMousePort` |
| FR-INP-004 | Lightpen Input | `ILightpenPort`, `IVideoChip` |
| FR-INP-005 | Paddle Controller Input | `IPaddlePort` |
| FR-INP-006 | VICE VKM Keymap Selection and Real-Time Keyboard Translation | `IKeyboardInputMap`, `IKeyboardInputMapSelection`, `IKeyboardMatrix`, `IMachineKeyboardInput`, `HostInputService` |
---

## Machine Profiles

| FR ID | FR Title | Primary Interfaces |
|-------|----------|--------------------|
| FR-PRF-001 | Commodore 64 (Original NMOS) Machine Profile | `IMachineProfile` |
| FR-PRF-002 | Commodore 64C Machine Profile | `IMachineProfile` |
| FR-PRF-003 | Commodore SX-64 Machine Profile | `IMachineProfile` |
| FR-PRF-004 | Commodore 128 Machine Profile | `IMachineProfile` |
| FR-PRF-005 | Commodore VIC-20 Machine Profile | `IMachineProfile` |
| FR-PRF-006 | Commodore PET Machine Profile | `IMachineProfile` |
| FR-PRF-007 | Commodore Plus/4 Machine Profile | `IMachineProfile` |
| FR-PRF-008 | Commodore C16 Machine Profile | `IMachineProfile` |
---

## Media Capture

| FR ID | FR Title | Primary Interfaces |
|-------|----------|--------------------|
| FR-MED-001 | Screenshot Capture (PNG/BMP) | `IMediaCapture` |
| FR-MED-002 | Video Recording (MP4 via FFmpeg) | `IMediaCapture`, `IVideoEncoder` |
| FR-MED-003 | Audio Recording (WAV/FLAC) | `IMediaCapture`, `IAudioEncoder` |
| FR-MED-004 | Synchronized Audio/Video Capture | `IMediaCapture`, `IMuxer` |
| FR-MED-005 | Output Format Selection and Configuration | `IMediaCapture` |
---

## Memory

| FR ID | FR Title | Primary Interfaces |
|-------|----------|--------------------|
| FR-MEM-001 | Address Decoding and PLA Banking Configuration | `IAddressSpace`, `IBankController`, `ISystemCore` |
| FR-MEM-002 | RAM Under ROM Access | `IAddressSpace` |
| FR-MEM-003 | Ultimax Cartridge Mode | `IAddressSpace`, `ICartridgePort` |
| FR-MEM-004 | VIC-II Bank Switching via CIA2 | `IAddressSpace`, `IVicBankSelector` |
| FR-MEM-005 | Color RAM ($D800-$DBFF) | `IAddressSpace` |
| FR-MEM-006 | Zero Page and Stack Page Behavior | `IAddressSpace` |
---

## Machine Monitor

| FR ID | FR Title | Primary Interfaces |
|-------|----------|--------------------|
| FR-MON-001 | Real-Time Disassembly View | `IMonitor`, `IDisassembler` |
| FR-MON-002 | Memory Hex and ASCII Display | `IMonitor` |
| FR-MON-003 | Breakpoint Management | `IMonitor`, `IBreakpointManager` |
| FR-MON-004 | CPU Register Inspection and Manipulation | `IMonitor`, `ICpu` |
| FR-MON-005 | Memory Bank View Selection | `IMonitor`, `IAddressSpace` |
| FR-MON-006 | Watch Expressions | `IMonitor` |
---

## Runtime Performance

| FR ID | FR Title | Primary Interfaces |
|-------|----------|--------------------|
| FR-PERF-RUNFRAME-001 | C64 PAL RunFrame Throughput | `IMachine`, `IClock`, `IBus`, `IVideoChip`, `IAudioChip` |
---

## Snapshot / Replay

| FR ID | FR Title | Primary Interfaces |
|-------|----------|--------------------|
| FR-SNP-001 | Save Complete Machine State to Snapshot | `ISnapshotManager` |
| FR-SNP-002 | Load Machine State from Snapshot | `ISnapshotManager` |
| FR-SNP-003 | Deterministic Input Replay | `ISnapshotManager`, `IReplayEngine` |
| FR-SNP-004 | Snapshot Comparison and State Diffing | `ISnapshotManager` |
---

## Disk Drives

| FR ID | FR Title | Primary Interfaces |
|-------|----------|--------------------|
| FR-DRV-001 | Commodore 1541 Single Floppy Disk Drive Emulation | `IDiskDrive`, `IClockedDevice` |
| FR-DRV-002 | Commodore 1571 Double-Sided Floppy Disk Drive Emulation | `IDiskDrive`, `IClockedDevice` |
| FR-DRV-003 | Commodore 1581 3.5" Floppy Disk Drive Emulation | `IDiskDrive` |
| FR-DRV-004 | Group Code Recording Encoding and Decoding | `IDiskDrive`, `IGcrCodec` |
| FR-DRV-005 | IEC Serial Bus Protocol | `ISerialBus`, `IDiskDrive` |
| FR-DRV-006 | Fast Loader Compatibility | `IDiskDrive`, `ISerialBus` |
---

## Tape / Datasette

| FR ID | FR Title | Primary Interfaces |
|-------|----------|--------------------|
| FR-TAP-001 | Datasette Motor Control | `ITapeUnit` |
| FR-TAP-002 | TAP File Format Support (v0 and v1) | `ITapeUnit`, `ITapCodec` |
| FR-TAP-003 | Cycle-Accurate Tape Read Timing | `ITapeUnit`, `IClockedDevice` |
| FR-TAP-004 | Tape Write Support | `ITapeUnit` |
| FR-TAP-005 | Turbo Tape Loader Compatibility | `ITapeUnit` |
---

## Video (VIC-II)

| FR ID | FR Title | Primary Interfaces |
|-------|----------|--------------------|
| FR-VIC-001 | Raster Engine with PAL/NTSC Timing | `IVideoChip`, `IClockedDevice` |
| FR-VIC-002 | Character Display Modes (Standard, Multicolor, ECM) | `IVideoChip` |
| FR-VIC-003 | Bitmap Display Modes (Standard, Multicolor) | `IVideoChip` |
| FR-VIC-004 | Sprite Engine (8 Hardware Sprites) | `IVideoChip`, `ISpriteUnit` |
| FR-VIC-005 | Sprite Collision Detection | `IVideoChip`, `ISpriteUnit` |
| FR-VIC-006 | Badline Handling and CPU DMA Cycle Stealing | `IVideoChip`, `IClockedDevice` |
| FR-VIC-007 | Border Behavior Including Open Border Tricks | `IVideoChip` |
| FR-VIC-008 | Flexible Line Interpretation (FLI) Support | `IVideoChip` |
| FR-VIC-009 | VIC-II Bank Switching (See also FR-MEM-004) | `IVideoChip`, `IVicBankSelector` |
| FR-VIC-010 | Sprite Multiplexing DMA Timing | `IVideoChip`, `ISpriteUnit` |

---

## Interface Coverage Summary

| Interface | FR Count |
|-----------|----------|
| `HostControlService` | 12 |
| `IAudioChip` | 12 |
| `IVideoChip` | 12 |
| `IAddressSpace` | 10 |
| `IClockedDevice` | 8 |
| `IDiskDrive` | 8 |
| `IMachineProfile` | 8 |
| `IMonitor` | 8 |
| `ICartridgePort` | 7 |
| `ICia` | 7 |
| `ICpu` | 7 |
| `IMediaCapture` | 6 |
| `ISnapshotManager` | 6 |
| `ITapeUnit` | 6 |
| `HostInputService` | 5 |
| `IVia` | 5 |
| `UiHostClient` | 5 |
| `HostMediaService` | 4 |
| `HostStateService` | 4 |
| `IKeyboardMatrix` | 4 |
| `HostStatusService` | 3 |
| `IInterruptController` | 3 |
| `IJoystickPort` | 3 |
| `IMachine` | 3 |
| `ISpriteUnit` | 3 |
| `IVoice` | 3 |
| `IWaveformGenerator` | 3 |
| `HostMonitorService` | 2 |
| `HostOutputService` | 2 |
| `IArchitectureDescriptor` | 2 |
| `IConfigurationStore` | 2 |
| `IFilter` | 2 |
| `IFrameSink` | 2 |
| `ISerialBus` | 2 |
| `ITimer` | 2 |
| `IVicBankSelector` | 2 |
| `AvaloniaRenderSurface` | 1 |
| `IAudioEncoder` | 1 |
| `IAutostartService` | 1 |
| `IBankController` | 1 |
| `IBreakpointManager` | 1 |
| `IClockController` | 1 |
| `IDisassembler` | 1 |
| `IEmulator` | 1 |
| `IEmulatorSession` | 1 |
| `IEnvelopeGenerator` | 1 |
| `IFlashMemory` | 1 |
| `IGcrCodec` | 1 |
| `IInputSource` | 1 |
| `IInstructionDecoder` | 1 |
| `IKeyboardInputMap` | 1 |
| `IKeyboardInputMapSelection` | 1 |
| `ILightpenPort` | 1 |
| `IMachineKeyboardInput` | 1 |
| `IMemoryMappedDevice` | 1 |
| `IMousePort` | 1 |
| `IMuxer` | 1 |
| `IPaddlePort` | 1 |
| `IReplayEngine` | 1 |
| `IRomProvider` | 1 |
| `ISerialPort` | 1 |
| `ITapCodec` | 1 |
| `ITodClock` | 1 |
| `IVideoEncoder` | 1 |
| `VideoService` | 1 |
