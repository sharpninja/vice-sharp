# Functional Requirements (MCP Server)

## FR-ARCH-001 Ad-hoc machine architecture loading

The emulator shall accept a YAML document describing a machine architecture (chips, memory regions, interrupt lines, master clock, video standard, reset vector) and shall validate the document against a published schema. Validation errors shall report the offending field path. The architecture descriptor produced by the loader shall round-trip equivalent in chip set, base addresses, and memory layout to the hardcoded C64 builder when loaded from docs/samples/c64.machine.yaml. The console host shall accept a --machine-yaml <path> flag that loads, validates, builds, and runs the machine, and shall return exit code 1 with a stderr message when the file is missing or invalid. Default behavior is preserved when the flag is absent.

## FR-CFG-001 Resource File and Command-Line Configuration

## FR-CFG-001: Resource File and Command-Line Configuration

**ID:** FR-CFG-001
**Title:** Resource File and Command-Line Configuration
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The emulator shall support persistent resource configuration and command-line or host-command overrides for emulator options that affect machine behavior and user-visible operation.

### Acceptance Criteria

1. Resource files can persist named emulator settings.
2. Command-line or host-command overrides can update resource-backed settings before or during a session when the setting is runtime-safe.
3. Invalid resource names or values are rejected with diagnostics and do not corrupt the active configuration.
4. Machine-specific resources are scoped to the active profile.
5. Effective resource values can be queried for diagnostics.

### Source References

- `native/vice/vice/doc/vice.texi`: system files, resource files, resources and command-line, settings/resources, and machine-specific settings.

### Traceability

- **Interfaces:** `IEmulatorSession`, `IConfigurationStore`, `HostControlService`
- **Technical Requirements:** TR-STATE-001, TR-GRPC-BOUNDARY-001
- **Test Suite:** `ConfigurationResourceTests`, `HostSettingsServiceTests`

---

## FR-CFG-002 ROM and Romset Selection

## FR-CFG-002: ROM and Romset Selection

**ID:** FR-CFG-002
**Title:** ROM and Romset Selection
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The emulator shall load machine ROM images and named romsets needed by the selected machine profile, validate them before use, and report missing or invalid ROMs without starting an invalid machine session.

### Acceptance Criteria

1. Machine profiles declare required and optional ROM artifacts.
2. ROM files are validated for size and checksum when known.
3. Named romsets can be selected for a session.
4. Missing or invalid ROMs produce actionable diagnostics.
5. ROM selection does not require UI code to access emulator runtime internals.

### Source References

- `native/vice/vice/doc/vice.texi`: ROM files and romset files sections.

### Traceability

- **Interfaces:** `IArchitectureDescriptor`, `IRomProvider`, `HostControlService`
- **Technical Requirements:** TR-LIB-001, TR-STATE-001
- **Test Suite:** `RomProviderTests`, `MachineProfileRomValidationTests`

---

## FR-CFG-003 Palette Selection and Color Resource Handling

## FR-CFG-003: Palette Selection and Color Resource Handling

**ID:** FR-CFG-003
**Title:** Palette Selection and Color Resource Handling
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The emulator shall support selecting palette resources for video output so rendered frames can match documented machine and display color profiles.

### Acceptance Criteria

1. Palette files can be discovered and selected for compatible machine/video profiles.
2. Invalid palette files report diagnostics and do not replace the active palette.
3. Palette changes apply at a frame boundary.
4. Captured frames include enough palette metadata to reproduce displayed colors.

### Source References

- `native/vice/vice/doc/vice.texi`: palette files and video settings sections.

### Traceability

- **Interfaces:** `IVideoChip`, `IFrameSink`, `HostStateService`
- **Technical Requirements:** TR-SIMD-001, TR-DET-001
- **Test Suite:** `PaletteResourceTests`, `FramePaletteMetadataTests`

---

## FR-CFG-004 Hotkey Configuration and Action Dispatch

## FR-CFG-004: Hotkey Configuration and Action Dispatch

**ID:** FR-CFG-004
**Title:** Hotkey Configuration and Action Dispatch
**Priority:** P2 -- Enhancement
**Iteration:** 2

### Description

The host UI shall allow configurable hotkeys to trigger emulator actions such as reset, media operations, monitor commands, snapshots, and settings actions.

### Acceptance Criteria

1. Hotkey configuration files can define action mappings.
2. Hotkeys dispatch to host commands rather than UI-local emulator operations.
3. Invalid hotkey directives are reported without disabling unrelated mappings.
4. Hotkeys can be scoped by UI mode so monitor text entry is not intercepted unexpectedly.

### Source References

- `native/vice/vice/doc/vice.texi`: hotkeys files, hotkey directives, action names, and hotkey command-line options.

### Traceability

- **Interfaces:** `UiHostClient`, `HostControlService`, `HostMediaService`, `HostMonitorService`
- **Technical Requirements:** TR-UI-SHELL-001, TR-GRPC-BOUNDARY-001
- **Test Suite:** `HotkeyConfigurationTests`, `UiActionDispatchTests`

---

## FR-CFG-005 Autostart and Program Launch Handling

## FR-CFG-005: Autostart and Program Launch Handling

**ID:** FR-CFG-005
**Title:** Autostart and Program Launch Handling
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The emulator shall support autostarting programs from disk, tape, cartridge, or supported archive/image sources by attaching required media and injecting the appropriate machine commands.

### Acceptance Criteria

1. Autostart can identify supported media/image sources and select an applicable launch path.
2. Autostart attaches required media through host media services.
3. Autostart injects launch commands through host input/control services at deterministic boundaries.
4. Autostart failures leave existing media and machine state unchanged unless explicitly committed.
5. Reset-plus-drive-8 autorun reports unsupported until the host implements the full autostart path.

### Source References

- `native/vice/vice/doc/vice.texi`: command-line autostart, autostart settings, autostart resources, autostart command-line options, and disk/tape image autostart sections.

### Traceability

- **Interfaces:** `HostMediaService`, `HostInputService`, `HostControlService`, `IAutostartService`
- **Technical Requirements:** TR-GRPC-BOUNDARY-001, TR-DET-001
- **Test Suite:** `AutostartServiceTests`, `ResetDrive8AutorunTests`

---

## FR-CFG-006 Host-Backed Peripheral Resource Configuration

## FR-CFG-006: Host-Backed Peripheral Resource Configuration

**ID:** FR-CFG-006
**Title:** Host-Backed Peripheral Resource Configuration
**Priority:** P2 -- Enhancement
**Iteration:** 3

### Description

The emulator shall expose configuration for host-backed peripheral devices such as filesystem devices, printers, RS232, Ethernet, tape-port devices, and user-port devices through host-owned services.

### Acceptance Criteria

1. Peripheral devices can be enabled, disabled, and configured through host/session settings.
2. Host-backed paths or endpoints are validated before the emulator commits the configuration.
3. Peripheral status and errors are visible through host diagnostics.
4. UI clients do not access host files, serial devices, sockets, or printers except through host service requests.

### Source References

- `native/vice/vice/doc/vice.texi`: peripheral settings, filesystem device settings, printer settings, RS232 settings, Ethernet emulation, tape port devices, and userport devices.

### Traceability

- **Interfaces:** `IConfigurationStore`, `HostControlService`, `HostStateService`
- **Technical Requirements:** TR-LIB-001, TR-GRPC-BOUNDARY-001, TR-PLAT-001
- **Test Suite:** `PeripheralConfigurationTests`, `HostPeripheralDiagnosticsTests`

---

## FR-CFG-007 RAM Initialization and Debug Resource Behavior

## FR-CFG-007: RAM Initialization and Debug Resource Behavior

**ID:** FR-CFG-007
**Title:** RAM Initialization and Debug Resource Behavior
**Priority:** P2 -- Enhancement
**Iteration:** 3

### Description

The emulator shall support configurable RAM initialization and debug resource behavior needed for repeatable startup, diagnostics, and compatibility testing.

### Acceptance Criteria

1. RAM initialization patterns can be selected before machine start.
2. Given the same selected pattern and ROM/profile inputs, startup RAM state is deterministic.
3. Debug resources can enable additional diagnostics without changing normal emulation behavior when disabled.
4. Effective RAM/debug settings are captured in snapshot or diagnostic metadata where relevant.

### Source References

- `native/vice/vice/doc/vice.texi`: RAM init pattern settings and debug settings sections.

### Traceability

- **Interfaces:** `IMachine`, `ISnapshotManager`, `HostStateService`
- **Technical Requirements:** TR-DET-001, TR-STATE-001
- **Test Suite:** `RamInitializationTests`, `DebugResourceTests`

---

## FR-CFG-008 Performance Limiter Configuration

## FR-CFG-008: Performance Limiter Configuration

**ID:** FR-CFG-008
**Title:** Performance Limiter Configuration
**Priority:** P1 -- Important
**Iteration:** 1

### Description

The emulator shall expose performance limiter settings separately from measured runtime telemetry so users can request a throttle target and observe actual emulation speed.

### Acceptance Criteria

1. A limiter target can be configured through host/session settings.
2. Host status reports requested limiter target separately from measured FPS and effective clock speed.
3. Limiter changes apply without resetting the active machine session.
4. Invalid limiter values are rejected with diagnostics.

### Source References

- `native/vice/vice/doc/vice.texi`: performance settings, performance resources, and performance command-line options.

### Traceability

- **Interfaces:** `HostControlService`, `HostStatusService`, `IClockedDevice`
- **Technical Requirements:** TR-HOST-STATUS-001, TR-GRPC-BOUNDARY-001
- **Test Suite:** `LimiterConfigurationTests`, `HostStatusTests`

## FR-CIA-001 CIA Timer A and Timer B with Cascade Mode

## FR-CIA-001: Timer A/B with Cascading

**ID:** FR-CIA-001
**Title:** CIA Timer A and Timer B with Cascade Mode
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

Each CIA chip has two 16-bit countdown timers (Timer A and Timer B). Timers can count system clock cycles or external events. Timer B can optionally cascade from Timer A (counting Timer A underflows). Timers can operate in one-shot or continuous mode and generate interrupts on underflow.

### Acceptance Criteria

1. Timer A counts down from its latch value each system clock cycle (when configured for system clock input).
2. Timer B can count system clock cycles or Timer A underflows (cascade mode, selected by bits 5-6 of CRB).
3. On underflow, the timer reloads from its latch value on the next cycle.
4. One-shot mode (bit 3 of control register) stops the timer after a single underflow.
5. Continuous mode restarts the timer automatically after underflow.
6. Writing to the timer latch registers does not affect the running counter until the next underflow (or forced load via bit 4 of control register).
7. Force-load (strobe bit 4) immediately copies the latch to the counter.
8. Timer underflow toggles the PB6 (Timer A) or PB7 (Timer B) output pin when configured.
9. The 1-cycle delay between timer reaching zero and the interrupt being asserted is modeled.

### Source References

- `native/vice/vice/doc/CIA-README.txt`: CIA timer/alarm behavior.
- `native/vice/vice/doc/vice.texi`: keyboard, joystick, IEC, and machine-feature behavior involving CIA ports.

### Traceability

- **Interfaces:** `ICia`, `ITimer`
- **Test Suite:** `CiaTimerTests`, `TimerCascadeTests`, `TimerOneShotTests`, `TimerLatchBehaviorTests`

---

## FR-CIA-002 Time-of-Day Clock

## FR-CIA-002: Time-of-Day (TOD) Clock

**ID:** FR-CIA-002
**Title:** Time-of-Day Clock
**Priority:** P1 -- Important
**Iteration:** 1

### Description

Each CIA has a Time-of-Day clock that counts in BCD format with tenths-of-seconds, seconds, minutes, and hours (12-hour format with AM/PM). The TOD is clocked by the 50Hz (PAL) or 60Hz (NTSC) power line frequency and can generate an interrupt on alarm match.

### Acceptance Criteria

1. TOD registers at offsets $08-$0B provide tenths, seconds, minutes, hours in BCD format.
2. The TOD increments at 50Hz (PAL) or 60Hz (NTSC) derived from the video sync signal.
3. Writing to the hours register (offset $0B) latches the TOD display; it resumes when tenths ($08) is written.
4. Reading the hours register latches all TOD registers; they unfreeze when tenths is read.
5. A TOD alarm can be set by writing to the same registers with bit 7 of CRB set.
6. When the TOD matches the alarm value, the TOD interrupt bit is set in the ICR.
7. Hours register bit 7 indicates AM (0) or PM (1).

### Source References

- `native/vice/vice/doc/CIA-README.txt`: CIA timer/alarm behavior.
- `native/vice/vice/doc/vice.texi`: keyboard, joystick, IEC, and machine-feature behavior involving CIA ports.

### Traceability

- **Interfaces:** `ICia`, `ITodClock`
- **Test Suite:** `TodClockTests`, `TodAlarmTests`, `TodLatchTests`, `TodBcdTests`

---

## FR-CIA-003 Keyboard Matrix Scanning via CIA1

## FR-CIA-003: Keyboard Matrix Scanning

**ID:** FR-CIA-003
**Title:** Keyboard Matrix Scanning via CIA1
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

CIA1 Port A and Port B are connected to the C64 keyboard matrix (8x8 matrix yielding 64 key positions). Port A selects the column (active-low output) and Port B reads the row (active-low input). Multiple keys can be detected simultaneously, including handling of ghosting and the RESTORE key (directly connected to NMI).

### Acceptance Criteria

1. Writing to CIA1 Port A ($DC00) selects which keyboard columns are driven low.
2. Reading CIA1 Port B ($DC01) returns the row state for the selected columns (0 = key pressed).
3. Multiple columns can be selected simultaneously for matrix scanning.
4. Key ghosting behavior matches hardware (pressing 3 keys in an L-shape can ghost a 4th).
5. The RESTORE key is not part of the matrix; it directly triggers NMI via CIA2.
6. The `IKeyboardMatrix` interface allows host-side key press/release events to set matrix state.

### Source References

- `native/vice/vice/doc/CIA-README.txt`: CIA timer/alarm behavior.
- `native/vice/vice/doc/vice.texi`: keyboard, joystick, IEC, and machine-feature behavior involving CIA ports.

### Traceability

- **Interfaces:** `ICia`, `IKeyboardMatrix`
- **Test Suite:** `KeyboardMatrixTests`, `KeyGhostingTests`, `RestoreKeyNmiTests`

---

## FR-CIA-004 Joystick Port Reading via CIA1

## FR-CIA-004: Joystick Reading

**ID:** FR-CIA-004
**Title:** Joystick Port Reading via CIA1
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

Joystick port 2 shares CIA1 Port A with the keyboard columns, and joystick port 1 shares CIA1 Port B with the keyboard rows. The joystick signals (up, down, left, right, fire) override keyboard reads when active.

### Acceptance Criteria

1. Joystick port 2 is read from CIA1 Port A ($DC00) bits 0-4 (active low).
2. Joystick port 1 is read from CIA1 Port B ($DC01) bits 0-4 (active low).
3. Bit mapping: 0=Up, 1=Down, 2=Left, 3=Right, 4=Fire.
4. Joystick signals OR with keyboard matrix signals (both active-low, so a joystick direction can interfere with keyboard reading).
5. The `IJoystickPort` interface allows the host to set joystick state (direction + fire).
6. Simultaneous joystick and keyboard reading conflicts are handled as on real hardware.

### Source References

- `native/vice/vice/doc/CIA-README.txt`: CIA timer/alarm behavior.
- `native/vice/vice/doc/vice.texi`: keyboard, joystick, IEC, and machine-feature behavior involving CIA ports.

### Traceability

- **Interfaces:** `ICia`, `IJoystickPort`
- **Test Suite:** `JoystickReadTests`, `JoystickKeyboardConflictTests`

---

## FR-CIA-005 CIA Serial Port Shift Register

## FR-CIA-005: Serial Port Shift Register

**ID:** FR-CIA-005
**Title:** CIA Serial Port Shift Register
**Priority:** P1 -- Important
**Iteration:** 2

### Description

Each CIA has an 8-bit serial shift register that can transmit or receive data bit-by-bit, clocked by Timer A or an external clock. CIA2's serial port is used for the IEC serial bus connection to disk drives.

### Acceptance Criteria

1. The shift register at offset $0C shifts one bit per Timer A underflow (output mode) or external clock edge (input mode).
2. Direction is controlled by bit 6 of CRA: 0 = input, 1 = output.
3. After 8 bits are shifted, the SDR interrupt bit is set in the ICR.
4. In output mode, data written to the SDR register is loaded into the shift register on the next Timer A underflow.
5. In input mode, incoming bits are shifted in on each external clock edge.
6. The serial data appears on the SP pin and the clock on the CNT pin.

### Source References

- `native/vice/vice/doc/CIA-README.txt`: CIA timer/alarm behavior.
- `native/vice/vice/doc/vice.texi`: keyboard, joystick, IEC, and machine-feature behavior involving CIA ports.

### Traceability

- **Interfaces:** `ICia`, `ISerialPort`
- **Test Suite:** `SerialShiftRegisterTests`, `SdrInterruptTests`

---

## FR-CIA-006 NMI Generation from CIA2

## FR-CIA-006: NMI Generation (CIA2)

**ID:** FR-CIA-006
**Title:** NMI Generation from CIA2
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

CIA2 generates Non-Maskable Interrupts (NMI) to the CPU. NMI sources include Timer A underflow, Timer B underflow, TOD alarm, serial port completion, and the FLAG pin (directly active-low edge from the IEC bus).

### Acceptance Criteria

1. CIA2 interrupt output is connected to the CPU NMI line.
2. Any enabled interrupt source in CIA2 ICR/IMR triggers an NMI.
3. The NMI is edge-triggered: it fires on the transition from no-interrupt to interrupt-pending.
4. Reading the CIA2 ICR ($DD0D) clears all interrupt flags and deasserts the NMI line.
5. The FLAG pin (directly from IEC ATN) can trigger NMI when enabled.
6. Multiple NMI sources can be pending simultaneously; reading ICR reveals which sources fired.

### Source References

- `native/vice/vice/doc/CIA-README.txt`: CIA timer/alarm behavior.
- `native/vice/vice/doc/vice.texi`: keyboard, joystick, IEC, and machine-feature behavior involving CIA ports.

### Traceability

- **Interfaces:** `ICia`, `IInterruptController`
- **Test Suite:** `Cia2NmiTests`, `NmiEdgeTests`, `FlagPinNmiTests`

---

## FR-CIA-007 IRQ Generation from CIA1

## FR-CIA-007: IRQ Generation (CIA1)

**ID:** FR-CIA-007
**Title:** IRQ Generation from CIA1
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

CIA1 generates Interrupt Requests (IRQ) to the CPU. IRQ sources mirror those of CIA2 (timers, TOD, serial, FLAG) but are connected to the IRQ line instead of NMI.

### Acceptance Criteria

1. CIA1 interrupt output is connected to the CPU IRQ line.
2. Any enabled interrupt source in CIA1 ICR/IMR asserts IRQ.
3. IRQ is level-triggered: it remains asserted as long as any enabled source has its flag set.
4. Reading CIA1 ICR ($DC0D) clears all interrupt flags and deasserts the IRQ line (if no other sources remain).
5. The IRQ mask register ($DC0D write) selects which sources can generate IRQ (bit 7 = set/clear control).
6. Timer A, Timer B, TOD alarm, SDR complete, and FLAG are all independently maskable.

### Source References

- `native/vice/vice/doc/CIA-README.txt`: CIA timer/alarm behavior.
- `native/vice/vice/doc/vice.texi`: keyboard, joystick, IEC, and machine-feature behavior involving CIA ports.

### Traceability

- **Interfaces:** `ICia`, `IInterruptController`
- **Test Suite:** `Cia1IrqTests`, `IrqMaskTests`, `IrqLevelTests`

## FR-CPU-001 Full 6502/6510 Instruction Set Including Undocumented Opcodes

## FR-CPU-001: Full 6502/6510 Instruction Set

**ID:** FR-CPU-001
**Title:** Full 6502/6510 Instruction Set Including Undocumented Opcodes
**Priority:** P0 -- Critical
**Iteration:** 0

### Description

The CPU emulation shall implement the complete MOS 6502/6510 instruction set, including all 256 opcode entries. This encompasses the 151 official opcodes as well as all undocumented/illegal opcodes (LAX, SAX, DCP, ISC, SLO, RLA, SRE, RRA, ANC, ALR, ARR, SBX, LAS, SHA, SHX, SHY, TAS, NOP variants, JAM/KIL). Behavior of undocumented opcodes shall match the known behavior documented by the VICE team and confirmed by hardware testing.

### Acceptance Criteria

1. All 151 official 6502 opcodes execute with correct results for all addressing modes.
2. All undocumented opcodes produce results matching the Lorenz test suite (C64 Tester) pass criteria.
3. The BCD flag affects ADC/SBC results correctly when the decimal flag is set (6502 mode) and is a no-op on the 6510 in CMOS variants.
4. All addressing modes are implemented: Immediate, Zero Page, Zero Page X/Y, Absolute, Absolute X/Y, Indirect, (Indirect,X), (Indirect),Y, Relative, Implied, Accumulator.
5. JAM/KIL opcodes halt the CPU and raise a diagnostic event on `ICpu.Jammed`.

### Source References

- `native/vice/vice/doc/vice.texi`: emulator feature sections for C64/C128/SCPU64 CPU compatibility and monitor-visible execution behavior.

### Traceability

- **Interfaces:** `ICpu`, `IInstructionDecoder`
- **Test Suite:** `Cpu6502Tests`, `IllegalOpcodeTests`, `LorenzTestSuiteRunner`

---

## FR-CPU-002 Cycle-Accurate Execution Timing

## FR-CPU-002: Cycle-Accurate Execution Timing

**ID:** FR-CPU-002
**Title:** Cycle-Accurate Execution Timing
**Priority:** P0 -- Critical
**Iteration:** 0

### Description

Each instruction shall consume the exact number of clock cycles as the real MOS 6510, including the extra cycle for page-boundary crossings on indexed addressing modes, the extra cycle for taken branches, and the additional cycle for page-crossing branches. The CPU shall expose its internal pipeline state (fetch, decode, execute sub-cycles) to allow other devices to observe and react at sub-instruction granularity.

### Acceptance Criteria

1. Every opcode consumes exactly the documented cycle count (per the "MOS 6510 Unintended Opcodes" reference and 64doc.txt).
2. Page-boundary crossing adds exactly one cycle for absolute indexed reads (LDA abs,X / LDA abs,Y / LDA (zp),Y).
3. Page-boundary crossing on write instructions (STA abs,X) always takes the extra cycle regardless of whether a page boundary is actually crossed.
4. Taken branches add one cycle; taken branches crossing a page boundary add two cycles.
5. The `IClockedDevice.Tick()` method is invoked once per clock phase, and CPU sub-cycle state is observable via `ICpu.Phase`.
6. The CIA/VIC-II timing interleave passes the VICE timing test suite.

### Source References

- `native/vice/vice/doc/vice.texi`: emulator feature sections for C64/C128/SCPU64 CPU compatibility and monitor-visible execution behavior.

### Traceability

- **Interfaces:** `ICpu`, `IClockedDevice`
- **Test Suite:** `CycleTimingTests`, `ViceTimingComparisonTests`

---

## FR-CPU-003 Interrupt Handling with Correct Timing

## FR-CPU-003: Interrupt Handling (IRQ, NMI, BRK)

**ID:** FR-CPU-003
**Title:** Interrupt Handling with Correct Timing
**Priority:** P0 -- Critical
**Iteration:** 0

### Description

The CPU shall correctly handle hardware interrupts (IRQ, NMI) and the software BRK instruction. IRQ is level-triggered and masked by the I flag. NMI is edge-triggered on the falling edge. The interrupt sequence shall take exactly 7 cycles. Interrupt hijacking (where an NMI arrives during an IRQ sequence or vice versa) shall be handled correctly per real hardware behavior.

### Acceptance Criteria

1. IRQ is sampled during the penultimate cycle of each instruction; when asserted and the I flag is clear, the IRQ sequence begins after the current instruction completes.
2. NMI is edge-detected (falling edge); once detected, it is serviced after the current instruction regardless of the I flag.
3. The BRK instruction pushes PC+2 (not PC+1) and sets the B flag in the pushed status register.
4. Interrupt hijacking: if NMI occurs during the vector-fetch cycles of an IRQ, the NMI vector ($FFFA) is fetched instead of the IRQ vector ($FFFE).
5. The interrupt acknowledge sequence takes exactly 7 cycles.
6. The `IInterruptController` interface allows external devices to assert/deassert IRQ and NMI lines independently.
7. Passes the Lorenz CIA interrupt timing tests.

### Source References

- `native/vice/vice/doc/vice.texi`: emulator feature sections for C64/C128/SCPU64 CPU compatibility and monitor-visible execution behavior.

### Traceability

- **Interfaces:** `ICpu`, `IInterruptController`
- **Test Suite:** `InterruptTimingTests`, `NmiEdgeDetectionTests`, `InterruptHijackTests`

---

## FR-CPU-004 6510 I/O Port at Addresses $0000/$0001

## FR-CPU-004: 6510 I/O Port ($0000/$0001)

**ID:** FR-CPU-004
**Title:** 6510 I/O Port at Addresses $0000/$0001
**Priority:** P0 -- Critical
**Iteration:** 0

### Description

The MOS 6510 processor's built-in I/O port at addresses $0000 (data direction register) and $0001 (data port) shall be emulated. This port controls the PLA memory banking configuration (LORAM, HIRAM, CHAREN bits), the cassette motor, and the cassette sense line. The data port shall exhibit the correct behavior with pull-up/pull-down resistors and the capacitive discharge timing that affects reads of output pins.

### Acceptance Criteria

1. Address $0000 is the Data Direction Register (DDR); bits set to 1 configure the corresponding $0001 bit as output.
2. Address $0001 bits 0-2 (LORAM, HIRAM, CHAREN) control PLA banking and are reflected immediately in the address space configuration.
3. Bit 3 controls the cassette motor (active low).
4. Bit 4 reads the cassette switch sense (active low).
5. Bit 5 is unused on C64 but reads as expected based on DDR/pull-up behavior.
6. When a bit transitions from output to input, the capacitive discharge timing (approximately 350 microseconds to decay) is modeled.
7. The `IMemoryMappedDevice` interface exposes the port to the address decoder.

### Source References

- `native/vice/vice/doc/vice.texi`: emulator feature sections for C64/C128/SCPU64 CPU compatibility and monitor-visible execution behavior.

### Traceability

- **Interfaces:** `ICpu`, `IMemoryMappedDevice`
- **Test Suite:** `IoPortTests`, `BankingConfigTests`, `CapacitiveDischargeTests`

---

## FR-CPU-005 8502 2MHz Mode Support for C128

## FR-CPU-005: 8502 2MHz Mode Support (C128)

**ID:** FR-CPU-005
**Title:** 8502 2MHz Mode Support for C128
**Priority:** P2 -- Enhancement
**Iteration:** 3

### Description

For C128 machine profile support, the CPU shall implement the MOS 8502 processor's ability to switch between 1MHz and 2MHz operation. In 2MHz mode, the VIC-II cannot access the bus, so the display shows a static pattern. The clock speed is controlled by bit 0 of the VIC-II register at $D030.

### Acceptance Criteria

1. Writing bit 0 of $D030 switches the CPU clock between 1MHz (0) and 2MHz (1).
2. In 2MHz mode, the CPU executes instructions at double speed (halved cycle duration).
3. In 2MHz mode, VIC-II DMA does not occur and the screen displays the characteristic "static" pattern.
4. I/O chip access (CIAs, SID, VIC-II registers) forces the clock back to 1MHz for the duration of the access.
5. The `IClockController` interface exposes the current clock rate and allows mode transitions.
6. CIA timers and SID continue to operate at their expected rates relative to the system clock.

### Source References

- `native/vice/vice/doc/vice.texi`: emulator feature sections for C64/C128/SCPU64 CPU compatibility and monitor-visible execution behavior.

### Traceability

- **Interfaces:** `ICpu`, `IClockController`
- **Test Suite:** `C128ClockModeTests`, `DualSpeedTimingTests`

## FR-CRT-001 Standard 8K and 16K Cartridge Support

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

### Source References

- `native/vice/vice/doc/vice.texi`: C64 cartridge settings, supported cartridge formats, and machine-specific cartridge behavior.

### Traceability

- **Interfaces:** `ICartridgePort`, `IAddressSpace`
- **Boundary:** FR-HOST-002 exposes cartridge insert/remove/status through the host service.
- **Test Suite:** `StandardCartridgeTests`, `CrtFileParserTests`, `CartridgeAutostartTests`

---

## FR-CRT-002 Ocean Type 1 Bank-Switching Cartridge

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

### Source References

- `native/vice/vice/doc/vice.texi`: C64 cartridge settings, supported cartridge formats, and machine-specific cartridge behavior.

### Traceability

- **Interfaces:** `ICartridgePort`
- **Test Suite:** `OceanType1Tests`, `OceanBankSwitchTests`

---

## FR-CRT-003 EasyFlash Cartridge with Flash Memory

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

### Source References

- `native/vice/vice/doc/vice.texi`: C64 cartridge settings, supported cartridge formats, and machine-specific cartridge behavior.

### Traceability

- **Interfaces:** `ICartridgePort`, `IFlashMemory`
- **Test Suite:** `EasyFlashTests`, `FlashProgrammingTests`, `EasyFlashRamTests`

---

## FR-CRT-004 Action Replay and Retro Replay Cartridge

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

### Source References

- `native/vice/vice/doc/vice.texi`: C64 cartridge settings, supported cartridge formats, and machine-specific cartridge behavior.

### Traceability

- **Interfaces:** `ICartridgePort`
- **Test Suite:** `ActionReplayTests`, `FreezeButtonTests`, `RetroReplayTests`

---

## FR-CRT-005 Final Cartridge III (FC3)

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

### Source References

- `native/vice/vice/doc/vice.texi`: C64 cartridge settings, supported cartridge formats, and machine-specific cartridge behavior.

### Traceability

- **Interfaces:** `ICartridgePort`
- **Test Suite:** `FinalCartridge3Tests`, `Fc3BankSwitchTests`, `Fc3FreezerTests`

## FR-DOC-001 Completion Dashboard surfaces VICE-to-ViceSharp parity

The root README shall contain a Completion Dashboard section that lists features grouped by iteration, with state (done / active / bounded / planned), completion percentage, and linked MCP TODO id per row. The dashboard shall be regenerated when subagent slices land and shall cite a date stamp identifying the snapshot. The dashboard data source shall be the MCP TODO store accessible at /mcpserver/todo.

## FR-DRV-001 Commodore 1541 Single Floppy Disk Drive Emulation

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

### Source References

- `native/vice/vice/doc/vice.texi`: disk drive emulation, drive resources, supported disk formats, autostart, and file-system device behavior.
- `native/vice/vice/doc/iec-bus.txt`: IEC bus topology and drive interaction overview.

### Traceability

- **Interfaces:** `IDiskDrive`, `IClockedDevice`
- **Boundary:** FR-HOST-002 exposes drive mount/eject/status through the host service.
- **Test Suite:** `Drive1541Tests`, `D64ImageTests`, `G64ImageTests`, `DriveTimingTests`

---

## FR-DRV-002 Commodore 1571 Double-Sided Floppy Disk Drive Emulation

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

### Source References

- `native/vice/vice/doc/vice.texi`: disk drive emulation, drive resources, supported disk formats, autostart, and file-system device behavior.
- `native/vice/vice/doc/iec-bus.txt`: IEC bus topology and drive interaction overview.

### Traceability

- **Interfaces:** `IDiskDrive`, `IClockedDevice`
- **Test Suite:** `Drive1571Tests`, `D71ImageTests`, `DoubleSidedTests`, `BurstTransferTests`

---

## FR-DRV-003 Commodore 1581 3.5" Floppy Disk Drive Emulation

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

### Source References

- `native/vice/vice/doc/vice.texi`: disk drive emulation, drive resources, supported disk formats, autostart, and file-system device behavior.
- `native/vice/vice/doc/iec-bus.txt`: IEC bus topology and drive interaction overview.

### Traceability

- **Interfaces:** `IDiskDrive`
- **Test Suite:** `Drive1581Tests`, `D81ImageTests`, `Wd1772Tests`

---

## FR-DRV-004 Group Code Recording Encoding and Decoding

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

### Source References

- `native/vice/vice/doc/vice.texi`: disk drive emulation, drive resources, supported disk formats, autostart, and file-system device behavior.
- `native/vice/vice/doc/iec-bus.txt`: IEC bus topology and drive interaction overview.

### Traceability

- **Interfaces:** `IDiskDrive`, `IGcrCodec`
- **Test Suite:** `GcrEncodingTests`, `GcrDecodingTests`, `SpeedZoneTests`, `InvalidGcrTests`

---

## FR-DRV-005 IEC Serial Bus Protocol

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

### Source References

- `native/vice/vice/doc/vice.texi`: disk drive emulation, drive resources, supported disk formats, autostart, and file-system device behavior.
- `native/vice/vice/doc/iec-bus.txt`: IEC bus topology and drive interaction overview.

### Traceability

- **Interfaces:** `ISerialBus`, `IDiskDrive`
- **Test Suite:** `IecProtocolTests`, `IecTimingTests`, `MultiDeviceTests`, `EoiHandshakeTests`

---

## FR-DRV-006 Fast Loader Compatibility

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

### Source References

- `native/vice/vice/doc/vice.texi`: disk drive emulation, drive resources, supported disk formats, autostart, and file-system device behavior.
- `native/vice/vice/doc/iec-bus.txt`: IEC bus topology and drive interaction overview.

### Traceability

- **Interfaces:** `IDiskDrive`, `ISerialBus`
- **Test Suite:** `FastLoaderCompatibilityTests`, `DriveTimingSyncTests`, `JiffyDosTests`

## FR-HOST-001 Host-Owned Emulator Session Lifecycle

## FR-HOST-001: Host Process and Machine Session Control

**ID:** FR-HOST-001
**Title:** Host-Owned Emulator Session Lifecycle
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The emulator host shall own machine session creation, lifecycle transitions, and process-level fault reporting. UI clients request state changes through the host boundary instead of instantiating emulator core objects directly.

### Acceptance Criteria

1. A host process can create and destroy a C64 machine session without referencing any UI framework.
2. The host exposes start, pause, resume, reset, and stop commands for an active session.
3. Lifecycle state is reported as idle, running, paused, faulted, or stopped.
4. Lifecycle commands are serialized per session so transitions cannot race.
5. Each command response includes the session id, command sequence, resulting state, and structured error details when applicable.
6. Runtime counters for frame number, cycle count, speed percentage, and elapsed host time are queryable by the UI.

### Source References

- `native/vice/vice/doc/vice.texi`: invoking emulators, basic operation, settings/resources, reset, autostart, monitor, media, and diagnostics as observable host-control behavior.

### Traceability

- **Interfaces:** `IEmulator`, `IMachine`, `IArchitectureDescriptor`, `HostControlService`
- **Technical Requirement:** TR-GRPC-BOUNDARY-001
- **Test Suite:** `HostSessionLifecycleTests`, `GrpcHostControlTests`

---

## FR-HOST-002 Host-Mediated Disk, Tape, and Cartridge Attachment

## FR-HOST-002: Media Attachment Protocol

**ID:** FR-HOST-002
**Title:** Host-Mediated Disk, Tape, and Cartridge Attachment
**Priority:** P0 -- Critical
**Iteration:** 2

### Description

The host shall expose runtime attachment and ejection of disk, tape, and cartridge media through boundary commands. UI clients provide user intent and file references or byte payloads; the host validates formats and applies the changes at emulator-safe boundaries.

### Acceptance Criteria

1. Disk images can be mounted and ejected through the host boundary for the configured drive device.
2. TAP images can be mounted and ejected through the host boundary for the datasette device.
3. Standard cartridge images can be inserted and removed through the host boundary.
4. Media format validation failures are reported without mutating the current device state.
5. Attachment commands return stable media status including device id, media kind, display name, write-protection state, and current error state.
6. Attach and eject operations are applied at command boundaries that do not tear device state mid-cycle.

### Source References

- `native/vice/vice/doc/vice.texi`: invoking emulators, basic operation, settings/resources, reset, autostart, monitor, media, and diagnostics as observable host-control behavior.

### Traceability

- **Interfaces:** `IDiskDrive`, `ITapeUnit`, `ICartridgePort`, `HostMediaService`
- **Related FRs:** FR-DRV-001, FR-TAP-002, FR-CRT-001
- **Technical Requirement:** TR-GRPC-BOUNDARY-001
- **Test Suite:** `HostMediaAttachmentTests`, `GrpcMediaCommandTests`

---

## FR-HOST-003 Host-Streamed Video Frames for Remote UI Clients

## FR-HOST-003: Remote Video Frame Streaming Protocol

**ID:** FR-HOST-003
**Title:** Host-Streamed Video Frames for Remote UI Clients
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The host shall stream committed video frames to external or remote UI clients without allowing UI rendering to block the emulation thread. In-process Avalonia rendering may instead use a host-owned direct frame-source binding as described by FR-UI-001 and TR-GRPC-BOUNDARY-001.

### Acceptance Criteria

1. Video frames are streamed with frame number, cycle stamp, video standard, dimensions, pixel format, and skipped-frame metadata.
2. Video backpressure uses a latest-complete-frame policy; stale frames may be dropped without changing emulation state.
3. A newly connected remote UI client can request the latest committed frame before subscribing to the live stream.
4. Disconnecting a remote UI client does not pause, reset, or mutate the emulator session.
5. Frame payloads use a documented pixel format so non-Avalonia clients can render them.

### Source References

- `native/vice/vice/doc/vice.texi`: invoking emulators, basic operation, settings/resources, reset, autostart, monitor, media, and diagnostics as observable host-control behavior.

### Traceability

- **Interfaces:** `IFrameSink`, `HostOutputService`, `VideoService`
- **Technical Requirement:** TR-GRPC-BOUNDARY-001
- **Test Suite:** `HostOutputStreamTests`, `GrpcFrameStreamTests`

---

## FR-HOST-004 Host-Normalized Keyboard, Joystick, and Machine Control

## FR-HOST-004: Input and Machine Control Protocol

**ID:** FR-HOST-004
**Title:** Host-Normalized Keyboard, Joystick, and Machine Control
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The host shall accept normalized input events and machine control commands from UI clients and inject or apply them at deterministic boundaries.

### Acceptance Criteria

1. Keyboard key-down, key-up, RESTORE, and SHIFT LOCK events can be submitted through the host boundary.
2. Joystick port 1 and port 2 direction/fire state can be submitted through the host boundary.
3. Start, pause, resume, reset, and shutdown controls are submitted through the host boundary.
4. Input events preserve client order through a monotonically increasing sequence number.
5. Unknown mappings, invalid port numbers, and stale sequence numbers are rejected with structured errors.
6. The same normalized input event stream can be recorded for deterministic replay.

### Source References

- `native/vice/vice/doc/vice.texi`: invoking emulators, basic operation, settings/resources, reset, autostart, monitor, media, and diagnostics as observable host-control behavior.

### Traceability

- **Interfaces:** `IKeyboardMatrix`, `IJoystickPort`, `IInputSource`, `HostInputService`, `HostControlService`
- **Related FRs:** FR-INP-001, FR-INP-002
- **Technical Requirement:** TR-GRPC-BOUNDARY-001
- **Test Suite:** `HostInputInjectionTests`, `GrpcInputOrderingTests`, `GrpcHostControlTests`

---

## FR-HOST-005 Host-Owned Snapshot, Screenshot, and Diagnostic Operations

## FR-HOST-005: State, Capture, and Diagnostics Commands

**ID:** FR-HOST-005
**Title:** Host-Owned Snapshot, Screenshot, and Diagnostic Operations
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The host shall expose state save/load, screenshot capture, and basic diagnostic commands without granting UI clients direct access to emulator internals.

### Acceptance Criteria

1. Snapshot save requests produce a versioned artifact or artifact handle with checksum metadata.
2. Snapshot load requests validate the artifact before replacing the active machine state.
3. Screenshot requests capture the latest committed frame with dimensions, pixel format, and palette metadata.
4. State and capture commands coordinate with the emulation loop so snapshots and screenshots are not torn.
5. Long-running state operations report progress and can be cancelled before commit.
6. Diagnostic status includes active machine profile, lifecycle state, attached media, recent structured errors, and current frame/cycle counters.

### Source References

- `native/vice/vice/doc/vice.texi`: invoking emulators, basic operation, settings/resources, reset, autostart, monitor, media, and diagnostics as observable host-control behavior.

### Traceability

- **Interfaces:** `ISnapshotManager`, `IMediaCapture`, `IMonitor`, `HostStateService`
- **Related FRs:** FR-SNP-001, FR-SNP-002, FR-MED-001
- **Technical Requirement:** TR-GRPC-BOUNDARY-001
- **Test Suite:** `HostStateCommandTests`, `GrpcSnapshotTests`, `GrpcScreenshotTests`

---

## FR-HOST-006 Host Runtime Status and Control Telemetry

## FR-HOST-006: Host Runtime Status and Control Telemetry

**ID:** FR-HOST-006
**Title:** Host Runtime Status and Control Telemetry
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The host shall expose runtime telemetry and machine-control state needed by emulator shells without requiring UI clients to read emulator internals directly.

### Acceptance Criteria

1. Host status reports power state, run state, limiter target, measured frames per second, frame count, cycle count, program counter, nominal clock, effective clock Hz, and effective clock percent.
2. Effective clock speed is measured from emulated cycles per real second and remains distinct from the requested limiter target.
3. Pause, resume, step one cycle, step one frame, cold reset, and warm reset commands are exposed through the host boundary.
4. Unsupported controls such as rewind and reset-plus-drive-8 autorun return explicit unsupported status until backing host history/autorun support exists.
5. Telemetry responses are safe for polling by UI clients and do not mutate emulator state.

### Source References

- `native/vice/vice/doc/vice.texi`: performance settings, reset behavior, monitor settings, and emulator status/control behavior exposed through user-facing commands.

### Traceability

- **Interfaces:** `HostControlService`, `HostStatusService`, `IMachine`, `ICpu`, `IClockedDevice`
- **Technical Requirements:** TR-GRPC-BOUNDARY-001, TR-HOST-STATUS-001
- **Test Suite:** `HostStatusTests`, `GrpcHostControlTests`, `StatusBarViewModelTests`

---

## FR-INP-001 Keyboard Matrix Emulation

## FR-INP-001: Keyboard Matrix Emulation

**ID:** FR-INP-001
**Title:** Keyboard Matrix Emulation
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The C64 keyboard is an 8x8 matrix scanned through CIA1 Port A (column select, active-low output) and Port B (row read, active-low input). The emulator shall map host keyboard events to the C64 matrix positions, support positional and symbolic mapping modes, and handle simultaneous key presses including ghosting behavior.

### Acceptance Criteria

1. All 64 key positions in the C64 matrix are mappable from host keyboard events.
2. Positional mapping mode: host keys map to the C64 key at the same physical position.
3. Symbolic mapping mode: host keys map to the C64 key that produces the same character.
4. Multiple simultaneous key presses are supported (up to the limits of the host keyboard).
5. Key ghosting in the matrix is modeled: pressing 3 keys that form an L-shape in the matrix causes a phantom 4th key to appear pressed.
6. The RESTORE key is handled separately (it triggers NMI, not a matrix position).
7. The SHIFT LOCK key toggles and holds the left SHIFT matrix position.
8. The `IKeyboardMatrix` interface accepts key-down and key-up events with C64 matrix coordinates.

### Source References

- `native/vice/vice/doc/vice.texi`: keyboard emulation, keymap files, joystick emulation, joymap files, control-port resources, mouse/lightpen/paddle behavior.
- `native/vice/vice/doc/html/images/keymaps.txt`: documented keymap coverage.
- `native/vice/vice/doc/joystick.md`: joystick API and host-driver behavior.

### Traceability

- **Interfaces:** `IKeyboardMatrix`
- **Boundary:** FR-HOST-004 normalizes remote UI key events before injection.
- **Test Suite:** `KeyboardMatrixTests`, `SymbolicMappingTests`, `PositionalMappingTests`, `KeyGhostingTests`

---

## FR-INP-002 Joystick Port 1 and Port 2 Emulation

## FR-INP-002: Joystick Port 1 and Port 2

**ID:** FR-INP-002
**Title:** Joystick Port 1 and Port 2 Emulation
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The C64 has two DE-9 joystick ports. Each port reads 5 digital signals: up, down, left, right, and fire. The emulator shall support mapping host input devices (keyboard keys, gamepads, analog sticks) to joystick port signals.

### Acceptance Criteria

1. Each joystick port reports 5 independent digital signals (up, down, left, right, fire).
2. Host keyboard keys can be mapped to joystick directions and fire.
3. Host gamepad/controller input can be mapped to joystick signals via `IJoystickPort.SetState()`.
4. Analog stick input from host controllers is converted to digital signals with a configurable dead zone.
5. Joystick ports can be swapped at runtime (port 1 <-> port 2).
6. Autofire functionality is available with configurable rate (in frames between toggles).
7. Both ports can be active simultaneously with independent mappings.

### Source References

- `native/vice/vice/doc/vice.texi`: keyboard emulation, keymap files, joystick emulation, joymap files, control-port resources, mouse/lightpen/paddle behavior.
- `native/vice/vice/doc/html/images/keymaps.txt`: documented keymap coverage.
- `native/vice/vice/doc/joystick.md`: joystick API and host-driver behavior.

### Traceability

- **Interfaces:** `IJoystickPort`
- **Boundary:** FR-HOST-004 normalizes remote UI joystick state before injection.
- **Test Suite:** `JoystickPortTests`, `JoystickMappingTests`, `AutofireTests`

---

## FR-INP-003 Commodore 1351 Proportional Mouse Emulation

## FR-INP-003: Mouse 1351 Proportional

**ID:** FR-INP-003
**Title:** Commodore 1351 Proportional Mouse Emulation
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The 1351 mouse uses the SID's potentiometer inputs (POT X, POT Y) to report proportional (analog) position data. The mouse connects to the joystick port and uses the analog pot lines for X/Y movement and the digital lines for buttons.

### Acceptance Criteria

1. Mouse X movement is reported via the POTX register ($D419 for port 1, $D41A for port 2).
2. Mouse Y movement is reported via the POTY register.
3. The pot registers report the proportional position as a value that wraps at 0-255.
4. Left mouse button is mapped to the fire button (joystick bit 4).
5. Right mouse button is mapped to a secondary control line (typically UP direction on joystick port).
6. Host mouse movement is scaled and mapped to the 1351 proportional output.
7. The `IMousePort` interface accepts delta-X and delta-Y from the host pointing device.

### Source References

- `native/vice/vice/doc/vice.texi`: keyboard emulation, keymap files, joystick emulation, joymap files, control-port resources, mouse/lightpen/paddle behavior.
- `native/vice/vice/doc/html/images/keymaps.txt`: documented keymap coverage.
- `native/vice/vice/doc/joystick.md`: joystick API and host-driver behavior.

### Traceability

- **Interfaces:** `IMousePort`
- **Test Suite:** `Mouse1351Tests`, `PotentiometerReadTests`, `MouseButtonTests`

---

## FR-INP-004 Lightpen Input

## FR-INP-004: Lightpen Input

**ID:** FR-INP-004
**Title:** Lightpen Input
**Priority:** P2 -- Enhancement
**Iteration:** 3

### Description

The VIC-II supports a lightpen input on the LP pin (directly on the joystick port). When triggered, the VIC-II latches the current raster position into the lightpen X ($D013) and Y ($D014) registers.

### Acceptance Criteria

1. A falling edge on the lightpen input latches the current X and Y raster positions.
2. The X position ($D013) reports the horizontal position in double-pixel units (0-163).
3. The Y position ($D014) reports the raster line number (0-255).
4. The lightpen latch triggers once per frame (subsequent triggers within the same frame are ignored).
5. A lightpen interrupt can be generated (if enabled in $D01A).
6. Host mouse position can be translated to lightpen position relative to the visible display area.
7. The `ILightpenPort` interface accepts screen coordinates and triggers the latch.

### Source References

- `native/vice/vice/doc/vice.texi`: keyboard emulation, keymap files, joystick emulation, joymap files, control-port resources, mouse/lightpen/paddle behavior.
- `native/vice/vice/doc/html/images/keymaps.txt`: documented keymap coverage.
- `native/vice/vice/doc/joystick.md`: joystick API and host-driver behavior.

### Traceability

- **Interfaces:** `ILightpenPort`, `IVideoChip`
- **Test Suite:** `LightpenLatchTests`, `LightpenInterruptTests`, `LightpenCoordinateTests`

---

## FR-INP-005 Paddle Controller Input

## FR-INP-005: Paddle Controllers

**ID:** FR-INP-005
**Title:** Paddle Controller Input
**Priority:** P2 -- Enhancement
**Iteration:** 3

### Description

Paddle controllers connect to the joystick port and use the SID's potentiometer inputs for analog position reading. Each joystick port supports two paddles (sharing the POTX and POTY lines). The fire buttons use the joystick digital lines.

### Acceptance Criteria

1. Paddle X (first paddle) is read via POTX; Paddle Y (second paddle) via POTY.
2. The pot value ranges from 0 to 255 based on the paddle position.
3. Paddle button 1 maps to the fire button (joystick bit 4).
4. Paddle button 2 maps to an additional digital line (typically bit 2, LEFT).
5. The SID pot scanning timing (512 cycles per measurement) is accurately modeled.
6. Two paddles per port (four paddles total) can be active simultaneously.
7. The `IPaddlePort` interface accepts analog position values (0.0-1.0) for each paddle.

### Source References

- `native/vice/vice/doc/vice.texi`: keyboard emulation, keymap files, joystick emulation, joymap files, control-port resources, mouse/lightpen/paddle behavior.
- `native/vice/vice/doc/html/images/keymaps.txt`: documented keymap coverage.
- `native/vice/vice/doc/joystick.md`: joystick API and host-driver behavior.

### Traceability

- **Interfaces:** `IPaddlePort`
- **Test Suite:** `PaddleControllerTests`, `PotScanTimingTests`, `DualPaddleTests`

---

## FR-INP-006 VICE VKM Keymap Selection and Real-Time Keyboard Translation

## FR-INP-006: VICE Keymap Selection and Translation

**ID:** FR-INP-006
**Title:** VICE VKM Keymap Selection and Real-Time Keyboard Translation
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The emulator shall use VICE keymap files to translate host keyboard events into machine-specific keyboard matrix state. Built-in maps and uploaded custom maps are selected per emulator session and applied by the host-owned machine keyboard input handler.

### Acceptance Criteria

1. Built-in VICE keymaps are discoverable by machine and map style.
2. A selected keymap is retained per emulator session until changed or the session ends.
3. Custom keymaps can be uploaded by content and metadata without requiring shared file paths between UI and host.
4. The parser supports VICE keymap comments, `!CLEAR`, `!INCLUDE`, `!UNDEF`, modifier directives, row/column entries, and shift flags needed for SHIFT, C=, and CTRL behavior.
5. Includes resolve relative to the current keymap file or uploaded bundle context.
6. Invalid custom maps report diagnostics and do not replace the active map.
7. Real-time key down/up events update the C64 keyboard matrix through the selected map without constructing UI-local emulator devices.
8. Machine-specific keyboard translators can be registered for future non-C64 profiles.

### Source References

- `native/vice/vice/doc/vice.texi`: keyboard emulation, keymap files, symbolic and positional mapping, keymap control commands, key mappings, special rows, and modifier flags.
- `native/vice/vice/doc/html/images/keymaps.txt`: documented keymap coverage for C64, C128, PET, Plus/4, and CBM-II.

### Traceability

- **Interfaces:** `IKeyboardInputMap`, `IKeyboardInputMapSelection`, `IKeyboardMatrix`, `IMachineKeyboardInput`, `HostInputService`
- **Related FRs:** FR-INP-001, FR-HOST-004, FR-UI-001
- **Technical Requirement:** TR-INPUT-VKM-001
- **Test Suite:** `C64VkmKeyboardTests`, `HostInputServiceTests`, `GrpcInputMappingTests`

## FR-MED-001 Screenshot Capture (PNG/BMP)

## FR-MED-001: Screenshot Capture

**ID:** FR-MED-001
**Title:** Screenshot Capture (PNG/BMP)
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The emulator shall capture the current video frame as a still image in PNG or BMP format. Screenshots capture the full VIC-II output including borders (optionally cropped to the display area only). The capture is triggered programmatically or by user action.

### Acceptance Criteria

1. PNG format output with correct color representation (RGB, 8-bit per channel).
2. BMP format output as an uncompressed bitmap.
3. Full frame capture includes the border area (standard PAL: 403x284 visible pixels; standard NTSC: 411x234).
4. Cropped capture includes only the main display area (320x200 pixels, or 160x200 in multicolor mode upscaled to 320x200).
5. The screenshot captures the frame as it appears after all VIC-II rendering (sprites, priority, collision) is complete.
6. Screenshots are accessible via `IMediaCapture.CaptureScreenshot()` which returns the image data or writes to a file path.
7. Integer scaling options (1x, 2x, 3x, 4x) are available.
8. Palette selection (VICE default, Pepto, CCS64, Community Colors) is configurable.

### Source References

- `native/vice/vice/doc/vice.texi`: screenshot, media output, sound/video recording, and format-selection behavior exposed to users.

### Traceability

- **Interfaces:** `IMediaCapture`
- **Boundary:** FR-HOST-005 exposes screenshot commands and artifact metadata through the host service.
- **Test Suite:** `ScreenshotCaptureTests`, `PngFormatTests`, `PaletteTests`

---

## FR-MED-002 Video Recording (MP4 via FFmpeg)

## FR-MED-002: Video Recording

**ID:** FR-MED-002
**Title:** Video Recording (MP4 via FFmpeg)
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The emulator shall record video output to MP4 (H.264) format using FFmpeg libraries via P/Invoke. Recording captures consecutive frames at the native frame rate (PAL: 50fps, NTSC: approximately 59.94fps) and encodes them in real-time or offline.

### Acceptance Criteria

1. Video is encoded to H.264 in an MP4 container.
2. Frame rate matches the emulated system (50fps PAL, 59.94fps NTSC).
3. Resolution options include native (403x284 PAL) and integer-scaled variants.
4. Recording can be started, paused, resumed, and stopped via `IMediaCapture`.
5. The encoding does not drop frames during real-time recording on the reference hardware (a modern desktop CPU).
6. Quality presets (low/medium/high) are configurable, mapping to CRF values.
7. The recording pipeline uses zero managed allocations per frame on the hot path.

### Source References

- `native/vice/vice/doc/vice.texi`: screenshot, media output, sound/video recording, and format-selection behavior exposed to users.

### Traceability

- **Interfaces:** `IMediaCapture`, `IVideoEncoder`
- **Test Suite:** `VideoRecordingTests`, `H264EncoderTests`, `FrameRateAccuracyTests`

---

## FR-MED-003 Audio Recording (WAV/FLAC)

## FR-MED-003: Audio Recording

**ID:** FR-MED-003
**Title:** Audio Recording (WAV/FLAC)
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The emulator shall record audio output to WAV (uncompressed PCM) or FLAC (lossless compressed) format. The recording captures the SID audio output at the configured sample rate.

### Acceptance Criteria

1. WAV format: 16-bit PCM, mono or stereo, at configurable sample rates (44100, 48000, 96000 Hz).
2. FLAC format: lossless compression of the same PCM data.
3. Recording captures the mixed SID output including all voices, filter, and volume.
4. For dual-SID configurations, stereo recording captures left/right channels independently.
5. Recording can be started, paused, resumed, and stopped independently of video recording.
6. Audio samples are buffered to prevent gaps during recording.
7. The recording includes digi playback ($D418) output at full fidelity.

### Source References

- `native/vice/vice/doc/vice.texi`: screenshot, media output, sound/video recording, and format-selection behavior exposed to users.

### Traceability

- **Interfaces:** `IMediaCapture`, `IAudioEncoder`
- **Test Suite:** `AudioRecordingTests`, `WavFormatTests`, `FlacFormatTests`, `StereoRecordingTests`

---

## FR-MED-004 Synchronized Audio/Video Capture

## FR-MED-004: Synchronized A/V Capture

**ID:** FR-MED-004
**Title:** Synchronized Audio/Video Capture
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The emulator shall support recording audio and video simultaneously with correct synchronization. The audio and video streams are muxed into a single container file (MP4) with timestamps that maintain A/V sync throughout the recording.

### Acceptance Criteria

1. Audio and video streams are written to a single MP4 container with correct timestamps.
2. A/V sync drift does not exceed 1 frame duration (20ms PAL, 16.7ms NTSC) over any recording length.
3. The muxer correctly interleaves audio and video packets.
4. Starting A/V recording captures both streams from the same frame/sample boundary.
5. Pausing and resuming A/V recording maintains sync after the gap.
6. The `IMuxer` interface handles timestamp generation and stream interleaving.
7. Audio sample count per video frame is correctly calculated to avoid drift (PAL: 882.17 samples/frame at 44100Hz).

### Source References

- `native/vice/vice/doc/vice.texi`: screenshot, media output, sound/video recording, and format-selection behavior exposed to users.

### Traceability

- **Interfaces:** `IMediaCapture`, `IMuxer`
- **Test Suite:** `AvSyncTests`, `MuxerTests`, `LongRecordingSyncTests`

---

## FR-MED-005 Output Format Selection and Configuration

## FR-MED-005: Format Selection

**ID:** FR-MED-005
**Title:** Output Format Selection and Configuration
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The media capture system shall support multiple output formats with configurable encoding parameters. Format selection is exposed through the `IMediaCapture` interface.

### Acceptance Criteria

1. Video formats: MP4 (H.264), AVI (uncompressed or MJPEG), GIF (animated, for short clips).
2. Audio formats: WAV (PCM 16-bit), FLAC (lossless), MP3 (lossy, for smaller files).
3. Image formats: PNG (lossless), BMP (uncompressed), JPEG (lossy).
4. Each format exposes its available quality/compression parameters.
5. Format availability is reported by `IMediaCapture.GetSupportedFormats()`.
6. Unavailable formats (e.g., if FFmpeg libraries are not present) are gracefully reported as unsupported without crashing.
7. The default format for each capture type is configurable via `IMediaCapture.SetDefaultFormat()`.

### Source References

- `native/vice/vice/doc/vice.texi`: screenshot, media output, sound/video recording, and format-selection behavior exposed to users.

### Traceability

- **Interfaces:** `IMediaCapture`
- **Test Suite:** `FormatSelectionTests`, `FormatAvailabilityTests`, `GracefulDegradationTests`

## FR-MEM-001 Address Decoding and PLA Banking Configuration

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

### Source References

- `native/vice/vice/doc/vice.texi`: machine-specific memory, cartridge, ROM, and RAM initialization settings.

### Traceability

- **Interfaces:** `IAddressSpace`, `IBankController`
- **Test Suite:** `PlaDecodingTests`, `BankConfigurationMatrixTests`

---

## FR-MEM-002 RAM Under ROM Access

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

### Source References

- `native/vice/vice/doc/vice.texi`: machine-specific memory, cartridge, ROM, and RAM initialization settings.

### Traceability

- **Interfaces:** `IAddressSpace`
- **Test Suite:** `RamUnderRomTests`, `VicMemoryViewTests`

---

## FR-MEM-003 Ultimax Cartridge Mode

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

### Source References

- `native/vice/vice/doc/vice.texi`: machine-specific memory, cartridge, ROM, and RAM initialization settings.

### Traceability

- **Interfaces:** `IAddressSpace`, `ICartridgePort`
- **Test Suite:** `UltimaxModeTests`, `OpenBusTests`

---

## FR-MEM-004 VIC-II Bank Switching via CIA2

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

### Source References

- `native/vice/vice/doc/vice.texi`: machine-specific memory, cartridge, ROM, and RAM initialization settings.

### Traceability

- **Interfaces:** `IAddressSpace`, `IVicBankSelector`
- **Test Suite:** `VicBankSwitchTests`, `CharacterRomOverlayTests`

---

## FR-MEM-005 Color RAM ($D800-$DBFF)

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

### Source References

- `native/vice/vice/doc/vice.texi`: machine-specific memory, cartridge, ROM, and RAM initialization settings.

### Traceability

- **Interfaces:** `IAddressSpace`
- **Test Suite:** `ColorRamTests`, `NybbleWideBehaviorTests`

---

## FR-MEM-006 Zero Page and Stack Page Behavior

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

### Source References

- `native/vice/vice/doc/vice.texi`: machine-specific memory, cartridge, ROM, and RAM initialization settings.

### Traceability

- **Interfaces:** `IAddressSpace`
- **Test Suite:** `ZeroPageTests`, `StackBehaviorTests`, `StackWrapTests`

## FR-MON-001 Real-Time Disassembly View

## FR-MON-001: Disassembly View

**ID:** FR-MON-001
**Title:** Real-Time Disassembly View
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The monitor shall provide a disassembly view that decodes 6502/6510 machine code into human-readable assembly mnemonics. The disassembler shall support all official and undocumented opcodes, display correct addressing mode syntax, and handle code that overlaps data regions.

### Acceptance Criteria

1. All 256 opcodes (including undocumented) are disassembled with correct mnemonics and operands.
2. All addressing modes display with standard syntax (e.g., `LDA #$FF`, `STA $D020`, `JMP ($FFFE)`, `LDA ($FB),Y`).
3. The disassembly view can be positioned at any address and scrolls forward/backward.
4. Backward disassembly uses heuristic alignment (scanning back N bytes and disassembling forward to find the correct instruction boundary at the target address).
5. Undocumented opcodes display with their commonly used mnemonics (LAX, SAX, DCP, ISC, etc.) and are visually distinguished from official opcodes.
6. The `IDisassembler` interface accepts an address and returns structured disassembly data (address, bytes, mnemonic, operand, size).
7. Labels and symbols can be optionally applied to addresses.

### Source References

- `native/vice/vice/doc/vice.texi`: monitor settings, monitor command-line options, debugging, disassembly, memory, register, breakpoint, and watch behavior.

### Traceability

- **Interfaces:** `IMonitor`, `IDisassembler`
- **Test Suite:** `DisassemblerTests`, `AllOpcodeDisassemblyTests`, `BackwardDisassemblyTests`

---

## FR-MON-002 Memory Hex and ASCII Display

## FR-MON-002: Memory Hex/ASCII Display

**ID:** FR-MON-002
**Title:** Memory Hex and ASCII Display
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The monitor shall display memory contents in a combined hex/ASCII view, showing 16 bytes per row with the address, hexadecimal byte values, and the printable ASCII representation of each byte.

### Acceptance Criteria

1. Each row displays: a 4-digit hex address, 16 hex byte values (space-separated, with a wider gap at the 8-byte midpoint), and the 16-character ASCII representation.
2. Non-printable characters ($00-$1F, $80-$FF) display as a dot (`.`) in the ASCII column.
3. The PETSCII character set is optionally available for the text column.
4. The display can be scrolled to any address ($0000-$FFFF).
5. Memory contents reflect the current banking configuration (the same address can show different values depending on which bank is selected).
6. Modified bytes (since last refresh) can be visually highlighted.
7. The display updates in real-time when the emulation is paused and memory is edited.

### Source References

- `native/vice/vice/doc/vice.texi`: monitor settings, monitor command-line options, debugging, disassembly, memory, register, breakpoint, and watch behavior.

### Traceability

- **Interfaces:** `IMonitor`
- **Test Suite:** `MemoryDisplayTests`, `PetsciiDisplayTests`, `BankViewTests`

---

## FR-MON-003 Breakpoint Management

## FR-MON-003: Breakpoint Management

**ID:** FR-MON-003
**Title:** Breakpoint Management
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The monitor shall support execution breakpoints, read/write watchpoints, and conditional breakpoints. When a breakpoint is hit, the emulation pauses and the monitor displays the current CPU state.

### Acceptance Criteria

1. Execution breakpoints: pause emulation when the PC reaches the specified address.
2. Read watchpoints: pause emulation when a specified address is read.
3. Write watchpoints: pause emulation when a specified address is written (optionally filtering by value).
4. Conditional breakpoints: expression-based conditions (e.g., `A == $FF`, `X > $10`, `MEM[$D020] == $01`).
5. Breakpoints can be enabled, disabled, and removed individually.
6. A maximum of at least 64 simultaneous breakpoints is supported without performance degradation.
7. Temporary breakpoints (auto-removed after first hit) are supported for step-over and run-to-cursor.
8. The `IBreakpointManager` interface provides CRUD operations and hit-count tracking for each breakpoint.

### Source References

- `native/vice/vice/doc/vice.texi`: monitor settings, monitor command-line options, debugging, disassembly, memory, register, breakpoint, and watch behavior.

### Traceability

- **Interfaces:** `IMonitor`, `IBreakpointManager`
- **Test Suite:** `BreakpointTests`, `WatchpointTests`, `ConditionalBreakpointTests`, `BreakpointPerformanceTests`

---

## FR-MON-004 CPU Register Inspection and Manipulation

## FR-MON-004: Register Inspection and Manipulation

**ID:** FR-MON-004
**Title:** CPU Register Inspection and Manipulation
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The monitor shall display and allow modification of all CPU registers (A, X, Y, SP, PC) and the processor status flags (N, V, -, B, D, I, Z, C). Register changes take effect on the next instruction execution.

### Acceptance Criteria

1. All registers are displayed: A (accumulator), X, Y (index registers), SP (stack pointer), PC (program counter).
2. All status flags are individually displayed and modifiable: N (negative), V (overflow), B (break), D (decimal), I (interrupt disable), Z (zero), C (carry).
3. Registers can be set to arbitrary values via the monitor interface.
4. Setting the PC to a new address causes execution to continue from that address.
5. Setting SP adjusts the effective stack pointer (addresses $0100+SP).
6. Register display includes both hexadecimal and decimal/binary representations.
7. The `ICpu` interface exposes `GetRegisters()` and `SetRegisters()` for programmatic access.

### Source References

- `native/vice/vice/doc/vice.texi`: monitor settings, monitor command-line options, debugging, disassembly, memory, register, breakpoint, and watch behavior.

### Traceability

- **Interfaces:** `IMonitor`, `ICpu`
- **Test Suite:** `RegisterInspectionTests`, `RegisterModificationTests`, `FlagManipulationTests`

---

## FR-MON-005 Memory Bank View Selection

## FR-MON-005: Memory Bank View Selection

**ID:** FR-MON-005
**Title:** Memory Bank View Selection
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The monitor shall allow the user to select which memory "bank" or view is displayed: CPU view (as the CPU sees it with current PLA configuration), RAM view (raw RAM regardless of ROM overlay), ROM view (character/BASIC/KERNAL ROM), I/O view, and individual bank configurations.

### Acceptance Criteria

1. CPU view: shows memory as the CPU currently sees it (respecting PLA banking).
2. RAM view: shows raw RAM contents at all addresses, ignoring ROM overlays.
3. ROM view: shows the ROM contents at their mapped addresses ($A000 BASIC, $D000 CHARROM, $E000 KERNAL).
4. I/O view: shows the I/O register space at $D000-$DFFF.
5. Drive view: shows memory from the attached disk drive's address space.
6. Bank selection is independent of the actual PLA banking state.
7. Editing memory in a specific bank view writes to that specific target (e.g., editing RAM view writes to RAM even if ROM is currently banked in).

### Source References

- `native/vice/vice/doc/vice.texi`: monitor settings, monitor command-line options, debugging, disassembly, memory, register, breakpoint, and watch behavior.

### Traceability

- **Interfaces:** `IMonitor`, `IAddressSpace`
- **Test Suite:** `BankViewSelectionTests`, `BankViewEditTests`, `DriveMemoryViewTests`

---

## FR-MON-006 Watch Expressions

## FR-MON-006: Watch Expressions

**ID:** FR-MON-006
**Title:** Watch Expressions
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The monitor shall support watch expressions that continuously evaluate and display values from memory, registers, or computed expressions. Watch values update whenever the emulation state changes (single-step, breakpoint hit, or periodic refresh).

### Acceptance Criteria

1. Watch expressions can reference memory addresses: `[$D020]`, `[$0400:$0427]` (range).
2. Watch expressions can reference registers: `A`, `X`, `Y`, `SP`, `PC`.
3. Watch expressions support arithmetic: `[$FB] + [$FC] * 256` (constructs a 16-bit pointer).
4. Watch values are displayed in configurable formats: hex, decimal, binary, ASCII/PETSCII.
5. Array watches display a contiguous range of bytes.
6. Watches can be added, removed, and reordered.
7. A watch that reads a side-effect register (like SID OSC3 at $D41B) is marked as potentially disruptive.
8. The watch list persists across monitor open/close within a session.

### Source References

- `native/vice/vice/doc/vice.texi`: monitor settings, monitor command-line options, debugging, disassembly, memory, register, breakpoint, and watch behavior.

### Traceability

- **Interfaces:** `IMonitor`
- **Test Suite:** `WatchExpressionTests`, `WatchArithmeticTests`, `WatchFormatTests`

## FR-PLAT-001 Cross-platform host wireframes and scope

The repository shall contain wireframes describing the host UI on each target platform (Windows desktop, MacOS desktop, mobile portrait, mobile landscape, Xbox/UWP). Each wireframe shall enumerate the canonical screens (machine view, attach panel, monitor, settings, keyboard map, status bar), the navigation flow, and the per-input affordances (mouse, touch, gamepad). The wireframes shall precede host code so platform host implementations have a stable target.

## FR-PRF-001 Commodore 64 (Original NMOS) Machine Profile

## FR-PRF-001: C64 (NMOS) Profile

**ID:** FR-PRF-001
**Title:** Commodore 64 (Original NMOS) Machine Profile
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The primary machine profile for the original Commodore 64 with NMOS chips: MOS 6510 CPU, MOS 6567 (NTSC) or MOS 6569 (PAL) VIC-II, MOS 6581 SID, two MOS 6526 CIAs. This is the baseline profile and the first target for full compatibility.

### Acceptance Criteria

1. CPU: MOS 6510 with all undocumented opcodes exhibiting NMOS behavior.
2. VIC-II: MOS 6569R3 (PAL) or MOS 6567R8 (NTSC) with correct timing per variant.
3. SID: MOS 6581 with characteristic analog filter curve and combined waveform behavior.
4. CIA: Two MOS 6526 with correct timer and TOD behavior.
5. RAM: 64KB main RAM + 1KB Color RAM.
6. ROM: BASIC V2 ($A000), KERNAL ($E000), Character Generator ($D000).
7. System clock: 985248 Hz (PAL) or 1022727 Hz (NTSC).
8. The profile is selectable via `IMachineProfile.LoadProfile("c64")`.
9. PAL/NTSC variant is configurable within the C64 profile.
10. Passes the VICE test suite for x64sc accuracy level.

### Source References

- `native/vice/vice/doc/vice.texi`: emulator feature sections for C64, C64DTV, C128, VIC20, PET, CBM-II, SCPU64, Plus/4, and C16 class machines.

### Traceability

- **Interfaces:** `IMachineProfile`
- **Test Suite:** `C64ProfileTests`, `C64PalNtscTests`, `ViceTestSuiteRunner`

---

## FR-PRF-002 Commodore 64C Machine Profile

## FR-PRF-002: C64C (New Revision) Profile

**ID:** FR-PRF-002
**Title:** Commodore 64C Machine Profile
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The C64C (cost-reduced revision) uses the MOS 8580 SID (which has a different filter and combined waveform behavior) and a revised VIC-II (MOS 8562/8565). The C64C profile configures the emulator with these variant chips.

### Acceptance Criteria

1. SID: MOS 8580 with linear filter curve and revised combined waveform tables.
2. VIC-II: MOS 8562 (NTSC) or MOS 8565 (PAL) -- functionally equivalent to earlier revisions but with different luminance levels.
3. All other components are identical to the C64 NMOS profile.
4. The difference in SID filter behavior is audible and matches real hardware comparisons.
5. Selectable via `IMachineProfile.LoadProfile("c64c")`.

### Source References

- `native/vice/vice/doc/vice.texi`: emulator feature sections for C64, C64DTV, C128, VIC20, PET, CBM-II, SCPU64, Plus/4, and C16 class machines.

### Traceability

- **Interfaces:** `IMachineProfile`
- **Test Suite:** `C64CProfileTests`, `Sid8580FilterTests`

---

## FR-PRF-003 Commodore SX-64 Machine Profile

## FR-PRF-003: SX-64 Profile

**ID:** FR-PRF-003
**Title:** Commodore SX-64 Machine Profile
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The SX-64 is a portable C64 with a built-in 1541 disk drive and a 5" color monitor. It lacks a cassette port and has a different KERNAL ROM (no tape routines, different screen colors on startup).

### Acceptance Criteria

1. KERNAL: SX-64 variant KERNAL ROM with different default colors (blue screen instead of light blue).
2. No cassette port: tape-related I/O port bits behave differently (cassette sense always reads "no tape").
3. Built-in 1541 drive: device 8 is always present.
4. The 6510 I/O port bit 4 (cassette sense) is permanently pulled high.
5. All other hardware is identical to the C64 NMOS profile.
6. Selectable via `IMachineProfile.LoadProfile("sx64")`.

### Source References

- `native/vice/vice/doc/vice.texi`: emulator feature sections for C64, C64DTV, C128, VIC20, PET, CBM-II, SCPU64, Plus/4, and C16 class machines.

### Traceability

- **Interfaces:** `IMachineProfile`
- **Test Suite:** `Sx64ProfileTests`, `Sx64KernalTests`, `Sx64NoCassetteTests`

---

## FR-PRF-004 Commodore 128 Machine Profile

## FR-PRF-004: C128 Profile

**ID:** FR-PRF-004
**Title:** Commodore 128 Machine Profile
**Priority:** P2 -- Enhancement
**Iteration:** 3

### Description

The Commodore 128 features dual CPUs (MOS 8502 and Zilog Z80), 128KB RAM, an 80-column VDC display chip, and C64 compatibility mode. The C128 profile is a major expansion of the emulation capabilities.

### Acceptance Criteria

1. CPU: MOS 8502 (enhanced 6510 with 2MHz mode per FR-CPU-005) and Z80 processor.
2. VIC-II: MOS 8564 (NTSC) or MOS 8566 (PAL) for the 40-column display.
3. VDC: MOS 8563 for the 80-column display (independent video output).
4. RAM: 128KB main RAM, 16KB or 64KB VDC RAM.
5. ROM: BASIC V7.0, C128 KERNAL, C64 mode KERNAL, Character Generator.
6. MMU (Memory Management Unit) at $D500 controls the expanded banking.
7. C64 compatibility mode (selectable at startup or via GO64 command).
8. The 2MHz mode is functional per FR-CPU-005.
9. Selectable via `IMachineProfile.LoadProfile("c128")`.

### Source References

- `native/vice/vice/doc/vice.texi`: emulator feature sections for C64, C64DTV, C128, VIC20, PET, CBM-II, SCPU64, Plus/4, and C16 class machines.

### Traceability

- **Interfaces:** `IMachineProfile`
- **Test Suite:** `C128ProfileTests`, `C128DualCpuTests`, `C128VdcTests`, `C128C64ModeTests`

---

## FR-PRF-005 Commodore VIC-20 Machine Profile

## FR-PRF-005: VIC-20 Profile

**ID:** FR-PRF-005
**Title:** Commodore VIC-20 Machine Profile
**Priority:** P2 -- Enhancement
**Iteration:** 3

### Description

The VIC-20 uses the MOS 6502 CPU, MOS 6560 (NTSC) or MOS 6561 (PAL) VIC chip (not VIC-II), two MOS 6522 VIA chips, and 5KB of RAM (expandable). The display and sound capabilities differ significantly from the C64.

### Acceptance Criteria

1. CPU: Standard MOS 6502 (not 6510; no built-in I/O port).
2. VIC chip: MOS 6560/6561 with 22-column text display and 4-voice sound.
3. VIA: Two MOS 6522 VIAs (per FR-VIA-004) instead of CIAs.
4. RAM: 5KB base (1KB at $0000, 4KB at $1000) expandable to 32KB+ with cartridge RAM.
5. ROM: BASIC V2, VIC-20 KERNAL, Character Generator.
6. Memory map differs significantly from C64.
7. Sound uses the VIC chip's 4 voices (3 square wave + 1 noise) instead of SID.
8. Selectable via `IMachineProfile.LoadProfile("vic20")`.
9. Memory expansion configurations (3KB, 8KB, 16KB, 24KB, 32KB) are selectable.

### Source References

- `native/vice/vice/doc/vice.texi`: emulator feature sections for C64, C64DTV, C128, VIC20, PET, CBM-II, SCPU64, Plus/4, and C16 class machines.

### Traceability

- **Interfaces:** `IMachineProfile`
- **Test Suite:** `Vic20ProfileTests`, `VicChipTests`, `Vic20MemoryExpansionTests`

---

## FR-PRF-006 Commodore PET Machine Profile

## FR-PRF-006: PET Profile

**ID:** FR-PRF-006
**Title:** Commodore PET Machine Profile
**Priority:** P3 -- Future
**Iteration:** 4

### Description

The Commodore PET series uses a MOS 6502 CPU, MOS 6520 PIA or MOS 6522 VIA chips, and a monochrome 40x25 or 80x25 character display. Multiple PET models (2001, 3032, 4032, 8032, 8296) have different configurations.

### Acceptance Criteria

1. CPU: Standard MOS 6502.
2. Display: CRTC (6545) controlled monochrome character display (40 or 80 columns).
3. I/O: MOS 6520 PIA and/or MOS 6522 VIA depending on model.
4. RAM: 8KB, 16KB, or 32KB depending on model.
5. ROM: PET BASIC (V1, V2, or V4), PET KERNAL, Character Generator.
6. IEEE-488 parallel bus for disk drive communication (not IEC serial).
7. Built-in cassette drive (PET 2001).
8. Selectable via `IMachineProfile.LoadProfile("pet")` with model sub-selection.

### Source References

- `native/vice/vice/doc/vice.texi`: emulator feature sections for C64, C64DTV, C128, VIC20, PET, CBM-II, SCPU64, Plus/4, and C16 class machines.

### Traceability

- **Interfaces:** `IMachineProfile`
- **Test Suite:** `PetProfileTests`, `PetCrtcTests`, `PetIeee488Tests`

---

## FR-PRF-007 Commodore Plus/4 Machine Profile

## FR-PRF-007: Plus/4 Profile

**ID:** FR-PRF-007
**Title:** Commodore Plus/4 Machine Profile
**Priority:** P3 -- Future
**Iteration:** 4

### Description

The Plus/4 uses the MOS 7501/8501 CPU, TED chip (handling both video and sound), and 64KB RAM. The TED chip replaces both the VIC-II and SID with a simpler combined audio/video chip.

### Acceptance Criteria

1. CPU: MOS 7501 or 8501 (6502-compatible with built-in I/O port).
2. TED: MOS 7360/8360 providing 40x25 text and 320x200/160x200 graphics, plus 2 square-wave sound channels.
3. RAM: 64KB main RAM.
4. ROM: BASIC V3.5 (with built-in machine monitor, renumber, etc.), Plus/4 KERNAL, 3-plus-1 software ROM.
5. No SID chip; sound is generated by the TED.
6. No VIC-II; video is generated by the TED with 121-color palette.
7. Selectable via `IMachineProfile.LoadProfile("plus4")`.

### Source References

- `native/vice/vice/doc/vice.texi`: emulator feature sections for C64, C64DTV, C128, VIC20, PET, CBM-II, SCPU64, Plus/4, and C16 class machines.

### Traceability

- **Interfaces:** `IMachineProfile`
- **Test Suite:** `Plus4ProfileTests`, `TedChipTests`, `TedAudioTests`

---

## FR-PRF-008 Commodore C16 Machine Profile

## FR-PRF-008: C16 Profile

**ID:** FR-PRF-008
**Title:** Commodore C16 Machine Profile
**Priority:** P3 -- Future
**Iteration:** 4

### Description

The C16 is essentially a cut-down Plus/4 with 16KB RAM and no built-in software ROMs. It uses the same TED chip and is software-compatible with the Plus/4 within its memory limits.

### Acceptance Criteria

1. Identical to Plus/4 profile except: 16KB RAM instead of 64KB.
2. No 3-plus-1 software ROMs.
3. The TED chip and CPU are identical to the Plus/4.
4. Programs that fit within 16KB of RAM run identically to the Plus/4.
5. Memory access above 16KB wraps or mirrors as on real hardware.
6. Selectable via `IMachineProfile.LoadProfile("c16")`.

### Source References

- `native/vice/vice/doc/vice.texi`: emulator feature sections for C64, C64DTV, C128, VIC20, PET, CBM-II, SCPU64, Plus/4, and C16 class machines.

### Traceability

- **Interfaces:** `IMachineProfile`
- **Test Suite:** `C16ProfileTests`, `C16MemoryLimitTests`

## FR-PRF-009 Benchmark harness and parity reporting against native VICE

The repository shall contain a BenchmarkDotNet harness that runs deterministic workloads for CPU, VIC-II, SID, CIA, and full-system boot, registered in ViceSharp.slnx as a non-test project. Each benchmark class shall be smoke-tested in the xUnit harness with one iteration per workload so CI catches benchmark wiring breakage without invoking BenchmarkDotNet itself. A NativeViceBaseline placeholder shall scope the eventual native VICE shim integration and the comparable measurement contract.

## FR-QA-001 Test methods require structured XMLDOCS

Every [Fact], [Theory], [ViceFact], and [ViceTheory] method in the test corpus shall carry an XML doc comment that names the FR-/TR- being tested, describes the use case being tested, and describes the acceptance criteria. A convention test shall enforce this via a ratchet that decreases as the corpus is retrofitted, and an environment switch (VICESHARP_XMLDOCS_ENFORCE=1) shall escalate the convention test to zero-tolerance mode.

## FR-SID-001 Three Independent Voice Oscillators

## FR-SID-001: Three-Voice Oscillator

**ID:** FR-SID-001
**Title:** Three Independent Voice Oscillators
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The SID chip shall emulate three independent voice channels, each with a 16-bit frequency register, a 16-bit pulse width register, and independent waveform/control selection. Each oscillator generates a 24-bit phase accumulator output that drives waveform generation.

### Acceptance Criteria

1. Each voice (1-3) has an independent 16-bit frequency register (low/high byte pairs at $D400/$D401, $D407/$D408, $D40E/$D40F).
2. The phase accumulator increments by the frequency value each clock cycle.
3. Frequency values translate to output frequency via: F_out = (F_reg * clock_freq) / 16777216.
4. Each voice has a 12-bit pulse width register ($D402/$D403, $D409/$D40A, $D410/$D411).
5. The oscillator output (OSC3 readable at $D41B) reflects the upper 8 bits of voice 3's oscillator.
6. The envelope output (ENV3 readable at $D41C) reflects voice 3's current envelope value.

### Source References

- `native/vice/vice/doc/vice.texi`: sound settings, SID model/filter behavior, audio resources, and SID file environment sections.

### Traceability

- **Interfaces:** `IAudioChip`, `IVoice`
- **Test Suite:** `OscillatorFrequencyTests`, `PhaseAccumulatorTests`, `Osc3ReadbackTests`

---

## FR-SID-002 Waveform Generation (Triangle, Sawtooth, Pulse, Noise)

## FR-SID-002: Waveform Generation

**ID:** FR-SID-002
**Title:** Waveform Generation (Triangle, Sawtooth, Pulse, Noise)
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

Each SID voice shall generate four selectable waveforms: Triangle, Sawtooth, Pulse, and Noise. Waveforms are selected by bits 4-7 of the control register ($D404, $D40B, $D412).

### Acceptance Criteria

1. Triangle waveform: 12-bit output derived from the upper bits of the phase accumulator, folded at the midpoint.
2. Sawtooth waveform: 12-bit output is the upper 12 bits of the phase accumulator directly.
3. Pulse waveform: 12-bit output is all-1s when the upper 12 bits of the accumulator are less than the pulse width, else all-0s.
4. Noise waveform: output from a 23-bit LFSR (see FR-SID-009), clocked when bit 19 of the oscillator transitions.
5. When no waveform bit is set, the output is 0.
6. Waveform selection takes effect immediately upon register write.

### Source References

- `native/vice/vice/doc/vice.texi`: sound settings, SID model/filter behavior, audio resources, and SID file environment sections.

### Traceability

- **Interfaces:** `IAudioChip`, `IWaveformGenerator`
- **Test Suite:** `TriangleWaveTests`, `SawtoothWaveTests`, `PulseWaveTests`, `NoiseWaveTests`

---

## FR-SID-003 Combined Waveform Output

## FR-SID-003: Combined Waveforms

**ID:** FR-SID-003
**Title:** Combined Waveform Output
**Priority:** P1 -- Important
**Iteration:** 2

### Description

When multiple waveform bits are set simultaneously, the SID outputs a combined waveform that is the logical AND of the individual waveform outputs (on the 6581) or a slightly different combination (on the 8580). Combined waveforms produce characteristic thin, metallic sounds used by many SID tunes.

### Acceptance Criteria

1. On the 6581, combined waveforms are computed as the bitwise AND of the selected waveform outputs, with additional analog bleed-through modeled via lookup tables derived from chip analysis.
2. On the 8580, combined waveforms use a different lookup table reflecting the digital die revision behavior.
3. Triangle + Sawtooth, Triangle + Pulse, Sawtooth + Pulse, and Triangle + Sawtooth + Pulse combinations all produce distinct outputs.
4. Noise combined with any other waveform produces the AND of noise LFSR output bits and the other waveform(s), and additionally corrupts the noise LFSR state.
5. Combined waveform lookup tables are configurable (replaceable) via `IWaveformGenerator.SetCombinedWaveformTable()`.

### Source References

- `native/vice/vice/doc/vice.texi`: sound settings, SID model/filter behavior, audio resources, and SID file environment sections.

### Traceability

- **Interfaces:** `IAudioChip`, `IWaveformGenerator`
- **Test Suite:** `CombinedWaveform6581Tests`, `CombinedWaveform8580Tests`, `NoiseLfsrCorruptionTests`

---

## FR-SID-004 6581 SID Filter Emulation

## FR-SID-004: Filter (6581 Variant)

**ID:** FR-SID-004
**Title:** 6581 SID Filter Emulation
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The 6581 SID filter is a state-variable filter with low-pass, band-pass, and high-pass outputs. The 6581 filter has a non-linear frequency response due to its analog implementation, with a characteristic "warm" sound. The filter cutoff frequency is controlled by an 11-bit value.

### Acceptance Criteria

1. The filter cutoff frequency is set by $D415 (low 3 bits) and $D416 (high 8 bits), forming an 11-bit value.
2. The filter resonance is set by the upper 4 bits of $D417.
3. Low-pass, band-pass, and high-pass modes are selectable via $D418 bits 4-6 and may be combined.
4. Each voice can be individually routed through the filter via $D417 bits 0-2.
5. The external audio input can be routed through the filter via $D417 bit 3.
6. The 6581 filter's non-linear cutoff frequency mapping (the "kinked" curve) is modeled.
7. Filter distortion at high resonance matches the 6581 analog behavior (soft clipping).

### Source References

- `native/vice/vice/doc/vice.texi`: sound settings, SID model/filter behavior, audio resources, and SID file environment sections.

### Traceability

- **Interfaces:** `IAudioChip`, `IFilter`
- **Test Suite:** `Filter6581Tests`, `FilterCutoffCurveTests`, `FilterResonanceTests`, `FilterRoutingTests`

---

## FR-SID-005 8580 SID Filter Emulation

## FR-SID-005: Filter (8580 Variant)

**ID:** FR-SID-005
**Title:** 8580 SID Filter Emulation
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The 8580 SID filter has a more linear frequency response than the 6581, producing a "cleaner" sound. The filter topology is the same (state-variable) but the analog characteristics differ significantly.

### Acceptance Criteria

1. The 8580 filter cutoff frequency mapping is approximately linear (compared to the 6581's non-linear curve).
2. Filter resonance behavior is less pronounced at extreme settings compared to 6581.
3. The 8580 does not exhibit the same distortion/clipping at high resonance as the 6581.
4. The filter implementation is selected based on the active `IMachineProfile` SID model.
5. Runtime switching between 6581 and 8580 filter models is supported via configuration.

### Source References

- `native/vice/vice/doc/vice.texi`: sound settings, SID model/filter behavior, audio resources, and SID file environment sections.

### Traceability

- **Interfaces:** `IAudioChip`, `IFilter`
- **Test Suite:** `Filter8580Tests`, `FilterModelComparisonTests`

---

## FR-SID-006 ADSR Envelope Generator

## FR-SID-006: ADSR Envelope Generator

**ID:** FR-SID-006
**Title:** ADSR Envelope Generator
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

Each SID voice has an ADSR (Attack, Decay, Sustain, Release) envelope generator that shapes the amplitude of the waveform output. The envelope is triggered by the gate bit in the control register. The ADSR bug (where certain attack/decay transitions can cause the envelope to skip to incorrect values) shall be accurately emulated.

### Acceptance Criteria

1. Attack rate is set by the upper 4 bits of $D405/$D40C/$D413 with 16 rate values from 2ms to 8s.
2. Decay rate is set by the lower 4 bits of $D405/$D40C/$D413 with 16 rate values from 6ms to 24s.
3. Sustain level is set by the upper 4 bits of $D406/$D40D/$D414 (0-15 mapped to 0-$FF).
4. Release rate is set by the lower 4 bits of $D406/$D40D/$D414 with the same 16 rates as decay.
5. Setting the gate bit (bit 0 of control register) starts the attack phase.
6. Clearing the gate bit starts the release phase from the current envelope level.
7. The ADSR bug is reproduced: if the envelope counter reaches zero during decay/release and the rate period comparator triggers at the same cycle, the envelope can jump to $FF.
8. The envelope output is an 8-bit value (0-$FF) that multiplies the waveform output.

### Source References

- `native/vice/vice/doc/vice.texi`: sound settings, SID model/filter behavior, audio resources, and SID file environment sections.

### Traceability

- **Interfaces:** `IAudioChip`, `IEnvelopeGenerator`
- **Test Suite:** `AdsrTimingTests`, `AdsrBugTests`, `GateToggleTests`, `EnvelopeOutputTests`

---

## FR-SID-007 Ring Modulation

## FR-SID-007: Ring Modulation

**ID:** FR-SID-007
**Title:** Ring Modulation
**Priority:** P1 -- Important
**Iteration:** 2

### Description

Ring modulation replaces the triangle waveform output of a voice with the product of that voice's triangle output and the MSB of the preceding voice's oscillator (voice 1 modulated by voice 3, voice 2 by voice 1, voice 3 by voice 2).

### Acceptance Criteria

1. Ring modulation is enabled by bit 2 of the control register ($D404/$D40B/$D412).
2. When enabled, the triangle output is XORed with the MSB of the modulating voice's phase accumulator.
3. Ring mod only affects the triangle waveform; other selected waveforms are not modified.
4. The modulating voice does not need to be gated or have its output enabled for ring mod to work.
5. Ring mod and hard sync can be combined on the same voice.

### Source References

- `native/vice/vice/doc/vice.texi`: sound settings, SID model/filter behavior, audio resources, and SID file environment sections.

### Traceability

- **Interfaces:** `IAudioChip`, `IVoice`
- **Test Suite:** `RingModTests`, `RingModCombinationTests`

---

## FR-SID-008 Hard Sync (Oscillator Synchronization)

## FR-SID-008: Hard Sync

**ID:** FR-SID-008
**Title:** Hard Sync (Oscillator Synchronization)
**Priority:** P1 -- Important
**Iteration:** 2

### Description

Hard sync resets a voice's phase accumulator to zero whenever the modulating voice's phase accumulator MSB transitions from 1 to 0. This produces harmonically rich timbres.

### Acceptance Criteria

1. Hard sync is enabled by bit 1 of the control register ($D404/$D40B/$D412).
2. When the modulating voice's oscillator MSB transitions from 1 to 0, the synced voice's phase accumulator is reset to 0.
3. Voice sync chain: voice 1 synced by voice 3, voice 2 by voice 1, voice 3 by voice 2.
4. The synced voice's frequency determines the harmonic content; the modulating voice's frequency determines the fundamental.
5. Hard sync works with all waveform types.

### Source References

- `native/vice/vice/doc/vice.texi`: sound settings, SID model/filter behavior, audio resources, and SID file environment sections.

### Traceability

- **Interfaces:** `IAudioChip`, `IVoice`
- **Test Suite:** `HardSyncTests`, `SyncTimingTests`

---

## FR-SID-009 Noise Waveform Linear Feedback Shift Register

## FR-SID-009: Noise LFSR

**ID:** FR-SID-009
**Title:** Noise Waveform Linear Feedback Shift Register
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The noise waveform is generated by a 23-bit Linear Feedback Shift Register (LFSR). The LFSR is clocked when bit 19 of the phase accumulator transitions from 0 to 1. The feedback polynomial matches the original SID chip.

### Acceptance Criteria

1. The LFSR is 23 bits wide with feedback taps at bits 17 and 22 (XOR).
2. The LFSR is clocked when bit 19 of the oscillator's phase accumulator transitions high.
3. The noise output is derived from specific bits of the LFSR (bits 0, 2, 5, 9, 11, 14, 18, 20).
4. Writing to the test bit (bit 3 of control register) resets the LFSR to all-ones.
5. Selecting noise in combination with other waveforms corrupts the LFSR by clearing bits that correspond to zero bits in the other waveform output.

### Source References

- `native/vice/vice/doc/vice.texi`: sound settings, SID model/filter behavior, audio resources, and SID file environment sections.

### Traceability

- **Interfaces:** `IAudioChip`, `IWaveformGenerator`
- **Test Suite:** `NoiseLfsrTests`, `NoiseLfsrCorruptionTests`, `TestBitResetTests`

---

## FR-SID-010 Direct Digital Sample Playback via Volume Register

## FR-SID-010: Digi Playback ($D418)

**ID:** FR-SID-010
**Title:** Direct Digital Sample Playback via Volume Register
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The SID's master volume register ($D418 bits 0-3) can be used for 4-bit direct digital sample playback by rapidly writing sample values. This technique ("digi" or "$D418 samples") is used extensively in C64 demos and games.

### Acceptance Criteria

1. Writes to $D418 bits 0-3 immediately affect the audio output level.
2. The volume register acts as a 4-bit DAC that adds a DC offset to the mixed output.
3. Rapid writes to $D418 produce audible PCM audio at the write rate.
4. The audio output pipeline has sufficiently low latency that per-rasterline $D418 writes produce clean audio without significant aliasing.
5. Galway/Daglish-style digi playback (NMI-driven 4-bit samples) is clearly audible and recognizable.

### Source References

- `native/vice/vice/doc/vice.texi`: sound settings, SID model/filter behavior, audio resources, and SID file environment sections.

### Traceability

- **Interfaces:** `IAudioChip`
- **Test Suite:** `DigiPlaybackTests`, `VolumeRegisterTimingTests`

---

## FR-SID-011 External Audio Input

## FR-SID-011: External Audio Input

**ID:** FR-SID-011
**Title:** External Audio Input
**Priority:** P2 -- Enhancement
**Iteration:** 3

### Description

The SID chip has an external audio input (EXT IN) that can be mixed into the output and optionally routed through the filter. This is used by some cartridges and peripherals.

### Acceptance Criteria

1. The external audio input channel can be enabled via $D417 bit 3 (route through filter) or mixed directly.
2. External audio input is accessible via `IAudioChip.SetExternalInput()`.
3. When routed through the filter, the external input is processed identically to the voice outputs.
4. The external input level is correctly scaled relative to the SID voice outputs.

### Source References

- `native/vice/vice/doc/vice.texi`: sound settings, SID model/filter behavior, audio resources, and SID file environment sections.

### Traceability

- **Interfaces:** `IAudioChip`
- **Test Suite:** `ExternalAudioInputTests`, `ExternalFilterRoutingTests`

---

## FR-SID-012 Dual-SID (Stereo SID) Configuration

## FR-SID-012: Dual-SID Configuration

**ID:** FR-SID-012
**Title:** Dual-SID (Stereo SID) Configuration
**Priority:** P2 -- Enhancement
**Iteration:** 3

### Description

The emulator shall support configurations with two (or more) SID chips at configurable addresses, enabling stereo SID playback as used by many modern SID compositions. Common second-SID addresses are $D420, $D500, and $DE00.

### Acceptance Criteria

1. A second SID chip can be enabled at a configurable address (default $D420).
2. The second SID operates independently with its own set of voices, filters, and envelope generators.
3. Each SID can be configured independently as 6581 or 8580 model.
4. Audio output from each SID can be routed to left/right stereo channels.
5. Third SID support at a third configurable address is available for 3SID tunes.
6. SID address ranges do not overlap with other I/O devices.

### Source References

- `native/vice/vice/doc/vice.texi`: sound settings, SID model/filter behavior, audio resources, and SID file environment sections.

### Traceability

- **Interfaces:** `IAudioChip`, `IAddressSpace`
- **Test Suite:** `DualSidTests`, `StereoRoutingTests`, `SidAddressMappingTests`

## FR-SNP-001 Save Complete Machine State to Snapshot

## FR-SNP-001: Save Machine State

**ID:** FR-SNP-001
**Title:** Save Complete Machine State to Snapshot
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The emulator shall capture the complete machine state at any point during execution and serialize it to a snapshot file. The snapshot includes CPU registers, all RAM, all I/O chip registers and internal state, VIC-II raster state, SID oscillator/envelope state, CIA timer state, and peripheral state.

### Acceptance Criteria

1. CPU state: all registers (A, X, Y, SP, PC, P), interrupt pending flags, and pipeline phase.
2. Memory: complete 64KB RAM contents, Color RAM (1KB nybbles), and any expansion RAM.
3. VIC-II: all 47 registers, raster position, sprite state (DMA, counters, data buffers), display state, badline state.
4. SID: all registers, oscillator phase accumulators, envelope state (phase, counter, rate), filter state, noise LFSR state.
5. CIA1 and CIA2: all registers, timer counters and latches, TOD clock state, shift register state, interrupt flags.
6. 6510 I/O port: DDR value, port value, capacitive charge state.
7. Peripherals: disk drive state (if true drive emulation), tape position, cartridge banking state.
8. Snapshot files use a versioned binary format with integrity checksums.
9. The `ISnapshotManager.Save()` method returns a snapshot handle or writes to a specified path.

### Source References

- `native/vice/vice/doc/vice.texi`: snapshot, history, recording, and state persistence behavior exposed by emulator commands.

### Traceability

- **Interfaces:** `ISnapshotManager`
- **Boundary:** FR-HOST-005 exposes snapshot save commands and artifact metadata through the host service.
- **Test Suite:** `SnapshotSaveTests`, `SnapshotCompletenessTests`, `SnapshotFormatTests`

---

## FR-SNP-002 Load Machine State from Snapshot

## FR-SNP-002: Load Machine State

**ID:** FR-SNP-002
**Title:** Load Machine State from Snapshot
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The emulator shall restore a complete machine state from a previously saved snapshot file. After loading, execution continues from exactly the point at which the snapshot was taken, with all devices in their saved state.

### Acceptance Criteria

1. Loading a snapshot restores the exact CPU state (registers, flags, pipeline phase).
2. All 64KB of RAM plus Color RAM are restored byte-for-byte.
3. VIC-II state is restored including mid-raster position -- the first frame after load completes the line from the saved raster position.
4. SID state is restored including oscillator phase -- audio output continues seamlessly.
5. CIA state is restored including running timer values -- interrupt timing is preserved.
6. Peripheral state (drives, tape, cartridges) is restored.
7. Snapshot format version mismatches are detected and reported.
8. Invalid or corrupt snapshots are rejected with a descriptive error.
9. Loading a snapshot does not leak memory (previous state is fully released).
10. The `ISnapshotManager.Load()` method accepts a snapshot handle or file path.

### Source References

- `native/vice/vice/doc/vice.texi`: snapshot, history, recording, and state persistence behavior exposed by emulator commands.

### Traceability

- **Interfaces:** `ISnapshotManager`
- **Boundary:** FR-HOST-005 exposes snapshot load commands and validation errors through the host service.
- **Test Suite:** `SnapshotLoadTests`, `SnapshotRoundtripTests`, `SnapshotVersionTests`, `CorruptSnapshotTests`

---

## FR-SNP-003 Deterministic Input Replay

## FR-SNP-003: Deterministic Replay

**ID:** FR-SNP-003
**Title:** Deterministic Input Replay
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The emulator shall support recording input events (keyboard, joystick, timing) from a starting snapshot and replaying them to reproduce the exact same execution. This enables tool-assisted speedruns (TAS), automated testing, and bug reproduction.

### Acceptance Criteria

1. Recording mode captures all external input events with their precise frame and cycle timestamps.
2. Playback mode loads a base snapshot and replays the recorded input events.
3. Playback produces bit-exact results: the same memory contents, same VIC-II output, same audio output as the original recording at every point.
4. The replay format stores: base snapshot reference, input event stream (event type, frame number, cycle within frame, value).
5. Replay can be paused, single-stepped, and fast-forwarded.
6. The `IReplayEngine` interface provides record/playback/seek operations.
7. Replay files are compact (only input events are stored, not full state per frame).
8. Seeking to an arbitrary frame is supported via periodic auto-snapshots during recording.

### Source References

- `native/vice/vice/doc/vice.texi`: snapshot, history, recording, and state persistence behavior exposed by emulator commands.

### Traceability

- **Interfaces:** `ISnapshotManager`, `IReplayEngine`
- **Test Suite:** `ReplayDeterminismTests`, `ReplaySeekTests`, `ReplayInputRecordTests`

---

## FR-SNP-004 Snapshot Comparison and State Diffing

## FR-SNP-004: Snapshot Comparison / Diff

**ID:** FR-SNP-004
**Title:** Snapshot Comparison and State Diffing
**Priority:** P2 -- Enhancement
**Iteration:** 3

### Description

The emulator shall support comparing two snapshots and reporting the differences in machine state. This is useful for debugging, understanding program behavior, and verifying determinism.

### Acceptance Criteria

1. Two snapshots can be compared via `ISnapshotManager.Compare(snapshot1, snapshot2)`.
2. The diff result reports all changed CPU registers and flags.
3. The diff result reports all changed memory addresses with old and new values.
4. The diff result reports all changed I/O chip register values (VIC-II, SID, CIA).
5. A summary mode reports the count of changes per subsystem (e.g., "RAM: 42 bytes changed, VIC-II: 3 registers changed").
6. A detailed mode reports every individual change.
7. Unchanged state is not included in the diff output (only deltas).
8. The diff can be serialized to a human-readable format for inspection.

### Source References

- `native/vice/vice/doc/vice.texi`: snapshot, history, recording, and state persistence behavior exposed by emulator commands.

### Traceability

- **Interfaces:** `ISnapshotManager`
- **Test Suite:** `SnapshotComparisonTests`, `DiffFormattingTests`, `IdenticalSnapshotTests`

## FR-TAP-001 Datasette Motor Control

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

### Source References

- `native/vice/vice/doc/vice.texi`: disk/tape image handling, tape settings, tape resources, and TAP file behavior.

### Traceability

- **Interfaces:** `ITapeUnit`
- **Test Suite:** `DatasetteMotorTests`, `MotorSpinUpTests`, `CassetteSenseTests`

---

## FR-TAP-002 TAP File Format Support (v0 and v1)

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

### Source References

- `native/vice/vice/doc/vice.texi`: disk/tape image handling, tape settings, tape resources, and TAP file behavior.

### Traceability

- **Interfaces:** `ITapeUnit`, `ITapCodec`
- **Boundary:** FR-HOST-002 exposes TAP mount/eject/status through the host service.
- **Test Suite:** `TapV0ReadTests`, `TapV1ReadTests`, `TapWriteTests`, `TapHeaderValidationTests`

---

## FR-TAP-003 Cycle-Accurate Tape Read Timing

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

### Source References

- `native/vice/vice/doc/vice.texi`: disk/tape image handling, tape settings, tape resources, and TAP file behavior.

### Traceability

- **Interfaces:** `ITapeUnit`, `IClockedDevice`
- **Test Suite:** `TapeReadTimingTests`, `PulseWidthClassificationTests`, `TapeDataDecodingTests`

---

## FR-TAP-004 Tape Write Support

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

### Source References

- `native/vice/vice/doc/vice.texi`: disk/tape image handling, tape settings, tape resources, and TAP file behavior.

### Traceability

- **Interfaces:** `ITapeUnit`
- **Test Suite:** `TapeWriteTests`, `TapWriteFormatTests`

---

## FR-TAP-005 Turbo Tape Loader Compatibility

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

### Source References

- `native/vice/vice/doc/vice.texi`: disk/tape image handling, tape settings, tape resources, and TAP file behavior.

### Traceability

- **Interfaces:** `ITapeUnit`
- **Test Suite:** `TurboLoaderTests`, `ShortPulseTimingTests`, `MultiSpeedLoaderTests`

## FR-UI-001 Dockable Host UI Control Client

## FR-UI-001: Dockable Host UI Control Client

**ID:** FR-UI-001
**Title:** Dockable Host UI Control Client
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The UI control layer shall operate as a dockable thin client of the emulator host. It sends commands and input events, presents host status, and performs media/session/state operations through generated gRPC clients or narrow gRPC-backed client abstractions without referencing or mutating core emulator objects directly.

For local desktop presentation, the in-process Avalonia renderer may bind a host-owned render surface directly to a local emulator/frame source. That direct renderer boundary is limited to frame presentation and is not available to ViewModels. External or remote UIs display streamed output through the gRPC video service/stream APIs.

### Acceptance Criteria

1. The UI control layer can connect to an emulator host, create or attach to a session, and reflect lifecycle state through gRPC-backed abstractions.
2. A media attach panel can dock on the left or right side of the main window and expose disk, tape, cartridge, readonly, status, validation, and recent-media affordances.
3. External or remote UIs display streamed video frames and route audio samples from host stream APIs to the platform audio backend.
4. The UI maps keyboard and joystick input to normalized host input events.
5. The UI invokes media attach/eject, snapshot save/load, and screenshot commands through host services.
6. The UI surfaces host errors and command validation failures without crashing.
7. Reconnecting to an existing remote session restores lifecycle state, current media status, and the latest committed frame.
8. UI ViewModels depend on abstraction-level host client facades; generated gRPC clients are isolated behind adapters.
9. The in-process Avalonia render surface may consume a local frame source directly only from the host/composition layer; ViewModels must not reference runtime internals, concrete emulator devices, or frame-source implementations.

### Source References

- `native/vice/vice/doc/vice.texi`: emulation window, menus, file selector, disk/tape images, reset, settings/resources, monitor, and help behavior as user-facing control requirements.

### Traceability

- **Interfaces:** `UiHostClient`, `HostControlService`, `HostOutputService`, `HostInputService`, `AvaloniaRenderSurface`
- **Related TRs:** TR-MVVM-001, TR-GRPC-BOUNDARY-001
- **Test Suite:** `RemoteUiClientTests`, `UiReconnectTests`, `UiHostBoundaryTests`

---

## FR-UI-002 Emulator Status and Machine Control Bar

## FR-UI-002: Emulator Status and Machine Control Bar

**ID:** FR-UI-002
**Title:** Emulator Status and Machine Control Bar
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The desktop UI shall provide a bottom status and control bar that surfaces host telemetry and machine controls while keeping keyboard/rendering focus stable.

### Acceptance Criteria

1. The status bar displays power state, run state, limiter target, measured FPS, cycle, PC, and effective clock speed.
2. The status bar exposes pause, resume, step cycle, step frame, rewind cycle, rewind frame, cold reset, warm reset, and reset-plus-drive-8 autorun controls.
3. Unsupported controls remain visible but report disabled/unsupported state from the host.
4. Using status controls does not stop emulator rendering or steal keyboard focus unless a text field explicitly takes focus.

### Source References

- `native/vice/vice/doc/vice.texi`: emulation window, reset, performance settings, monitor settings, and command-line control behavior.

### Traceability

- **Interfaces:** `UiHostClient`, `HostControlService`, `HostStatusService`
- **Technical Requirements:** TR-HOST-STATUS-001, TR-UI-SHELL-001, TR-GRPC-BOUNDARY-001
- **Test Suite:** `StatusBarViewModelTests`, `GrpcHostControlTests`, `AvaloniaShellTests`

---

## FR-UI-003 Collapsible Tabbed Emulator Sidebar

## FR-UI-003: Collapsible Tabbed Emulator Sidebar

**ID:** FR-UI-003
**Title:** Collapsible Tabbed Emulator Sidebar
**Priority:** P1 -- Important
**Iteration:** 1

### Description

The desktop UI shall provide a collapsible sidebar with Peripherals, Settings, and Monitor tabs so routine emulator controls remain available without crowding the display surface.

### Acceptance Criteria

1. A hamburger control collapses and expands the sidebar without stopping emulator input or rendering.
2. The Peripherals tab contains disk, tape, cartridge, recent media, readonly, and keyboard map controls.
3. The Settings tab contains limiter target, display scale/crop, and host/session settings controls.
4. The Monitor tab embeds the reusable monitor control.
5. Tab state remains synchronized with host status and survives sidebar collapse/expand.

### Source References

- `native/vice/vice/doc/vice.texi`: menus, file selector, disk/tape images, settings/resources, keyboard settings, control port settings, and monitor settings.

### Traceability

- **Interfaces:** `UiHostClient`, `HostMediaService`, `HostInputService`, `HostControlService`
- **Technical Requirements:** TR-UI-SHELL-001, TR-GRPC-BOUNDARY-001
- **Test Suite:** `SidebarViewModelTests`, `AttachPanelViewModelTests`, `SettingsPanelViewModelTests`

---

## FR-UI-004 Docked and Pop-Out Monitor Control

## FR-UI-004: Docked and Pop-Out Monitor Control

**ID:** FR-UI-004
**Title:** Docked and Pop-Out Monitor Control
**Priority:** P1 -- Important
**Iteration:** 1

### Description

The UI shall provide a reusable machine monitor control that can be docked in the sidebar or popped into a separate window while sharing the same host monitor session.

### Acceptance Criteria

1. The monitor control can execute commands, display output, and request register, memory, disassembly, breakpoint, and stepping operations through the host boundary.
2. The monitor can dock inside the sidebar or pop out to a separate window without creating a second emulator session.
3. Docked and popped monitor state stays synchronized.
4. The monitor intentionally takes keyboard focus only while the user interacts with its command input.

### Source References

- `native/vice/vice/doc/vice.texi`: monitor settings, debug settings, memory/register/disassembly-oriented monitor behavior, and machine-control commands.

### Traceability

- **Interfaces:** `IMonitor`, `HostMonitorService`, `UiHostClient`
- **Technical Requirements:** TR-UI-SHELL-001, TR-GRPC-BOUNDARY-001
- **Test Suite:** `MonitorControlViewModelTests`, `GrpcMonitorServiceTests`, `MonitorPopOutTests`

## FR-VIA-001 VIA 6522 Timer A and Timer B Operation

## FR-VIA-001: VIA 6522 Timer Operation

**ID:** FR-VIA-001
**Title:** VIA 6522 Timer A and Timer B Operation
**Priority:** P1 -- Important
**Iteration:** 3

### Description

The 6522 VIA has two 16-bit timers with different capabilities than the CIA timers. Timer 1 supports free-running and one-shot modes with optional PB7 output toggling. Timer 2 operates in one-shot mode and can optionally count pulses on PB6. Timer behavior has a known 1-cycle difference from CIA timers (the VIA timer counts N+1 cycles for a latch value of N).

### Acceptance Criteria

1. Timer 1 counts down from its latch value and generates an interrupt on underflow.
2. Timer 1 free-running mode automatically reloads and continues counting after underflow.
3. Timer 1 one-shot mode stops after a single underflow.
4. Timer 1 can toggle PB7 on each underflow (when configured via ACR bit 7).
5. Timer 2 operates as a one-shot countdown or counts negative pulses on PB6.
6. A latch value of N results in an interrupt after N+1 clock cycles (unlike the CIA which is N+1 only when force-loaded).
7. Writing to the Timer 1 low-byte latch does not start the timer; writing the high byte starts it.
8. Writing Timer 2 high byte starts the timer and clears the T2 interrupt flag.

### Source References

- `native/vice/vice/doc/vice.texi`: VIC-20, PET/CBM-II, and disk-drive I/O feature sections.
- `native/vice/vice/doc/coding-guidelines.txt`: source layout only; no FR text derived from coding policy.

### Traceability

- **Interfaces:** `IVia`, `ITimer`
- **Test Suite:** `ViaTimerTests`, `ViaTimerVsCiaComparisonTests`, `ViaPb7ToggleTests`

---

## FR-VIA-002 VIA Shift Register

## FR-VIA-002: Shift Register

**ID:** FR-VIA-002
**Title:** VIA Shift Register
**Priority:** P1 -- Important
**Iteration:** 3

### Description

The 6522 VIA has an 8-bit bidirectional shift register that can be clocked by Timer 2, the system clock, or an external clock. It is used for serial data transfer in VIC-20 and disk drive contexts.

### Acceptance Criteria

1. The shift register can operate in 8 modes controlled by ACR bits 2-4.
2. Mode 0: Shift register disabled.
3. Modes 1-3: Shift in under Timer 2 control, external clock, or free-running Timer 2 clock.
4. Modes 4-7: Shift out under Timer 2 control, external clock, free-running, or system clock.
5. An interrupt is generated after 8 bits have been shifted.
6. The CB1 pin provides the clock and CB2 provides the data for shift register operations.
7. Data is shifted MSB first.

### Source References

- `native/vice/vice/doc/vice.texi`: VIC-20, PET/CBM-II, and disk-drive I/O feature sections.
- `native/vice/vice/doc/coding-guidelines.txt`: source layout only; no FR text derived from coding policy.

### Traceability

- **Interfaces:** `IVia`
- **Test Suite:** `ViaShiftRegisterTests`, `ViaShiftModeTests`

---

## FR-VIA-003 VIA Port A and Port B with Handshake Protocols

## FR-VIA-003: Port A/B Handshake Modes

**ID:** FR-VIA-003
**Title:** VIA Port A and Port B with Handshake Protocols
**Priority:** P1 -- Important
**Iteration:** 3

### Description

The VIA's two 8-bit I/O ports (Port A and Port B) support handshaking protocols for interfacing with external devices. Port A uses CA1/CA2 control lines; Port B uses CB1/CB2. Handshake modes include pulse output and independent interrupt control.

### Acceptance Criteria

1. Port A Data Direction Register (DDRA) controls which bits are input (0) and output (1).
2. Port B Data Direction Register (DDRB) controls which bits are input (0) and output (1).
3. CA1 active edge (rising or falling) is selected by PCR bit 0.
4. CA2 can operate as: negative edge input, positive edge input, handshake output, or pulse output (PCR bits 1-3).
5. CB1/CB2 control operates identically to CA1/CA2 with PCR bits 4-7.
6. Reading Port A (ORA) or Port B (ORB) clears the corresponding CA1/CB1 interrupt flags.
7. Latching mode (ACR bits 0-1) can latch port input data on the active CA1/CB1 edge.

### Source References

- `native/vice/vice/doc/vice.texi`: VIC-20, PET/CBM-II, and disk-drive I/O feature sections.
- `native/vice/vice/doc/coding-guidelines.txt`: source layout only; no FR text derived from coding policy.

### Traceability

- **Interfaces:** `IVia`
- **Test Suite:** `ViaPortHandshakeTests`, `ViaControlLineTests`, `ViaLatchModeTests`

---

## FR-VIA-004 VIC-20 VIA Integration (VIA1 and VIA2)

## FR-VIA-004: VIC-20 VIA Integration

**ID:** FR-VIA-004
**Title:** VIC-20 VIA Integration (VIA1 and VIA2)
**Priority:** P1 -- Important
**Iteration:** 3

### Description

The VIC-20 uses two VIA 6522 chips. VIA1 ($9110-$911F) handles the keyboard matrix, joystick, and cassette interface. VIA2 ($9120-$912F) handles the IEC serial bus, user port, and provides the NMI source. Both VIAs must be fully functional for VIC-20 machine profile support.

### Acceptance Criteria

1. VIA1 is mapped at $9110-$911F with mirroring within its address range.
2. VIA2 is mapped at $9120-$912F with mirroring within its address range.
3. VIA1 Port A reads the keyboard column and joystick state.
4. VIA1 Port B drives the keyboard row selection.
5. VIA2 Port B handles IEC serial bus signals (ATN OUT, CLK OUT/IN, DATA OUT/IN).
6. VIA2 CA1 is connected to the cassette READ line.
7. VIA1 generates IRQ; VIA2 generates NMI via its interrupt output.

### Source References

- `native/vice/vice/doc/vice.texi`: VIC-20, PET/CBM-II, and disk-drive I/O feature sections.
- `native/vice/vice/doc/coding-guidelines.txt`: source layout only; no FR text derived from coding policy.

### Traceability

- **Interfaces:** `IVia`, `IAddressSpace`
- **Test Suite:** `Vic20ViaIntegrationTests`, `Vic20KeyboardTests`, `Vic20IecBusTests`

---

## FR-VIA-005 Disk Drive VIA Integration (1541/1571)

## FR-VIA-005: Disk Drive VIA Integration

**ID:** FR-VIA-005
**Title:** Disk Drive VIA Integration (1541/1571)
**Priority:** P1 -- Important
**Iteration:** 3

### Description

The 1541 and 1571 disk drives each contain two VIA 6522 chips. VIA1 ($1800-$180F) handles the IEC serial bus communication with the host computer. VIA2 ($1C00-$1C0F) controls the drive motor, head stepper, read/write head, and density selection.

### Acceptance Criteria

1. Drive VIA1 handles IEC serial bus communication: ATN IN, CLK IN/OUT, DATA IN/OUT.
2. Drive VIA2 Port A reads data from the read head (GCR byte or bit stream).
3. Drive VIA2 Port B controls: stepper motor (bits 0-1), motor on/off (bit 2), LED (bit 3), write protect sense (bit 4), density select (bits 5-6), head step direction (bit 7 on 1571).
4. Drive VIA2 Timer 1 is used for byte-ready timing in the GCR read pipeline.
5. BYTE READY signal (VIA2 CA1) triggers when a complete byte has been shifted in from the disk.
6. Both VIAs generate interrupts to the drive's own CPU (6502 in the 1541).

### Source References

- `native/vice/vice/doc/vice.texi`: VIC-20, PET/CBM-II, and disk-drive I/O feature sections.
- `native/vice/vice/doc/coding-guidelines.txt`: source layout only; no FR text derived from coding policy.

### Traceability

- **Interfaces:** `IVia`, `IDiskDrive`
- **Test Suite:** `DriveViaTests`, `DriveIecHandshakeTests`, `DriveMotorControlTests`

## FR-VIC-001 Raster Engine with PAL/NTSC Timing

VIC-II raster engine shall generate cycle-accurate video output for PAL 6569 with 312 lines and 63 cycles per line and NTSC 6567 with 263 lines and 65 cycles per line. Acceptance: raster counter $D011 bit 7 plus $D012 increments once per raster line; raster interrupt fires when $D011/$D012 match and triggers at cycle 0 of the matching PAL line with 1-cycle acknowledge latency; PAL display window begins at line 51 and ends at line 250; display/idle transitions occur at the correct cycles; light-pen high-to-low transition latches current raster X and low raster line into $D013/$D014, sets the light-pen interrupt latch, and re-arms only at the frame boundary. Source: native/vice/vice/doc/vice.texi. Tests: RasterTimingTests, PalNtscVariantTests, RasterInterruptTests, VicIILightPenTests.

## FR-VIC-002 Character Display Modes (Standard, Multicolor, ECM)

VIC-II shall support Standard Character Mode, Multicolor Character Mode, and Extended Color Mode with screen matrix and character data fetched from the selected VIC bank and $D018 pointers. Acceptance: standard mode displays 40x25 8x8 character cells with background and Color RAM foreground; MCM displays 4x8 double-wide pixels with up to four colors when Color RAM bit 3 is set; ECM selects four backgrounds from upper screen-code bits and limits character selection to 64 glyphs; invalid ECM/BMM/MCM selector combinations render visible graphics as color 0 while x64sc still derives hidden foreground priority and sprite-background collision bits from the underlying character pixel, including hires character one-bits when Color RAM bit 3 is clear and multicolor pairs %10/%11 when Color RAM bit 3 is set; c-access and g-access timing occurs at the correct cycles. Sources: native/vice/vice/doc/vice.texi and native/vice/vice/src/viciisc/vicii-draw-cycle.c:41,133-141,196-224,401-428. Tests: StandardCharModeTests, MulticolorCharModeTests, EcmModeTests, InvalidModeTests.

## FR-VIC-003 Bitmap Display Modes (Standard, Multicolor)

## FR-VIC-003: Bitmap Display Modes

**ID:** FR-VIC-003
**Title:** Bitmap Display Modes (Standard, Multicolor)
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The VIC-II shall support Standard Bitmap Mode and Multicolor Bitmap Mode. In bitmap modes, the display is a 320x200 (standard) or 160x200 (multicolor, double-wide pixels) bitmap with color data from the screen matrix and Color RAM.

### Acceptance Criteria

1. Standard bitmap mode (BMM=1, MCM=0) displays 320x200 pixels; each 8x8 cell has a foreground/background pair from the screen matrix byte.
2. Multicolor bitmap mode (BMM=1, MCM=1) displays 160x200 double-wide pixels; each 4x8 cell uses up to 4 colors from screen matrix, Color RAM, and background register.
3. The bitmap base address is selected by bit 3 of $D018 (either $0000 or $2000 relative to VIC bank).
4. BMM + ECM combination is invalid for visible graphics output and renders as color 0, but x64sc still derives the hidden foreground/priority bit from the underlying standard-bitmap one-bits.
5. BMM + MCM + ECM combination is invalid for visible graphics output and renders as color 0, but x64sc still derives the hidden foreground/priority bit from multicolor bitmap pairs %10/%11.
6. Bitmap data fetch timing matches hardware (g-access reads bitmap data instead of character generator).

### Source References

- `native/vice/vice/doc/vice.texi`: video settings, C64/C128 VIC-II features, display mode, border, raster, and palette behavior.
- `native/vice/vice/src/viciisc/vicii-draw-cycle.c`: invalid ECM bitmap-mode `colors[]` entries map visible output to `COL_NONE`, while `draw_graphics()` keeps `pixel_pri = px & 0x2` for sprite priority and collision handling.

### Traceability

- **Interfaces:** `IVideoChip`
- **Test Suite:** `StandardBitmapModeTests`, `MulticolorBitmapModeTests`, `InvalidModeCombinationTests`

## FR-VIC-004 Sprite Engine (8 Hardware Sprites)

## FR-VIC-004: Sprite Engine

**ID:** FR-VIC-004
**Title:** Sprite Engine (8 Hardware Sprites)
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The VIC-II shall emulate all 8 hardware sprites with correct positioning, priority, expansion, and multicolor capabilities. Each sprite is 24x21 pixels (standard) or 12x21 double-wide pixels (multicolor) and can be independently expanded 2x horizontally and/or vertically.

### Acceptance Criteria

1. Eight sprites (0-7) are independently positionable via $D000-$D010.
2. Sprite enable register ($D015) controls which sprites are displayed.
3. Sprite X-expansion ($D01D) doubles horizontal size; Y-expansion ($D017) doubles vertical size.
4. Multicolor mode ($D01C) per sprite uses 3 shared colors + 1 individual color.
5. Sprite data pointers are read from screen matrix + $03F8-$03FF (relative to VIC bank).
6. Sprite-to-background priority is controlled by $D01B per sprite.
7. Sprite display priority follows the rule: lower-numbered sprites appear in front of higher-numbered sprites.

### Source References

- `native/vice/vice/doc/vice.texi`: video settings, C64/C128 VIC-II features, display mode, border, raster, and palette behavior.

### Traceability

- **Interfaces:** `IVideoChip`, `ISpriteUnit`
- **Test Suite:** `SpriteDisplayTests`, `SpriteExpansionTests`, `SpriteMulticolorTests`, `SpritePriorityTests`

---

## FR-VIC-005 Sprite Collision Detection

## FR-VIC-005: Sprite Collision Detection

**ID:** FR-VIC-005
**Title:** Sprite Collision Detection
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The VIC-II shall detect sprite-to-sprite and sprite-to-background collisions and report them through the collision registers and optional interrupt generation.

### Acceptance Criteria

1. Sprite-sprite collision register ($D01E) sets a bit for each sprite involved in a collision with another sprite.
2. Sprite-background collision register ($D01F) sets a bit for each sprite that overlaps non-background pixels.
3. Collision registers are latched -- they retain set bits until read, at which point they are cleared.
4. Reading the collision register clears it atomically.
5. Collision interrupt (if enabled in $D01A) fires when a new collision is detected.
6. In multicolor mode, the "transparent" color (bit pattern %00) does not trigger collisions.
7. Expanded sprites use the expanded pixel area for collision detection.
8. Invalid ECM display-mode pixels that render as color 0 still participate in sprite priority and sprite-background collision when the x64sc hidden foreground/priority bit is set.

### Source References

- `native/vice/vice/doc/vice.texi`: video settings, C64/C128 VIC-II features, display mode, border, raster, and palette behavior.
- `native/vice/vice/src/viciisc/vicii-draw-cycle.c`: `draw_graphics()` writes the per-pixel priority buffer from `px & 0x2`; sprite/background priority and collision code consume that buffer even when the selected color entry is `COL_NONE`.

### Traceability

- **Interfaces:** `IVideoChip`, `ISpriteUnit`
- **Test Suite:** `SpriteCollisionTests`, `CollisionRegisterLatchTests`, `CollisionInterruptTests`

## FR-VIC-006 Badline Handling and CPU DMA Cycle Stealing

## FR-VIC-006: Badline Handling and DMA Stealing

**ID:** FR-VIC-006
**Title:** Badline Handling and CPU DMA Cycle Stealing
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

A "badline" occurs when the lower 3 bits of the raster counter match the Y-scroll value ($D011 bits 0-2) and the display is enabled. During a badline, the VIC-II steals 40 CPU cycles (plus 3 setup cycles) to fetch character pointers from the screen matrix (c-access). The CPU is halted during these stolen cycles.

### Acceptance Criteria

1. Badline condition triggers when (raster_line & 7) == (d011 & 7) and display is enabled, within the display window (lines 48-247).
2. During a badline, the VIC-II performs 40 c-accesses to read the screen matrix.
3. The CPU is halted for 40-43 cycles during a badline (3 setup cycles + 40 character fetch cycles).
4. Sprite DMA stealing occurs independently: each enabled sprite steals 2 cycles per line (p-access + s-access pattern).
5. Sprite and badline DMA can overlap, with sprite DMA taking priority.
6. The cycle at which the CPU is first halted for a badline is deterministic and occurs at cycle 15 of the raster line.

### Source References

- `native/vice/vice/doc/vice.texi`: video settings, C64/C128 VIC-II features, display mode, border, raster, and palette behavior.

### Traceability

- **Interfaces:** `IVideoChip`, `IClockedDevice`
- **Test Suite:** `BadlineDetectionTests`, `DmaStealingTimingTests`, `CpuHaltTests`

---

## FR-VIC-007 Border Behavior Including Open Border Tricks

## FR-VIC-007: Border Behavior and Open Borders

**ID:** FR-VIC-007
**Title:** Border Behavior Including Open Border Tricks
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The VIC-II border unit shall accurately emulate both the main border (top/bottom) and side borders (left/right), including the ability to "open" borders using well-known VIC tricks (toggling the RSEL/CSEL bits at specific raster positions).

### Acceptance Criteria

1. With RSEL=1 (25 rows), the upper border ends at line 51 and lower border begins at line 251.
2. With RSEL=0 (24 rows), the upper border ends at line 55 and lower border begins at line 247.
3. With CSEL=1 (40 columns), side borders are at pixels 24-343.
4. With CSEL=0 (38 columns), side borders are at pixels 31-334.
5. Opening the top/bottom border: if RSEL is cleared before the border comparison line and set after, the vertical border flip-flop is not set, allowing sprites to display in the border area.
6. Opening the side borders: toggling CSEL at the correct cycle prevents the horizontal border flip-flop from being set.
7. On PAL x64sc timing, the horizontal border clear check occurs at cycle 17 when CSEL=1 and cycle 18 when CSEL=0; the horizontal border set check occurs at cycle 57 when CSEL=1 and cycle 56 when CSEL=0.
8. Changing CSEL from 1 to 0 at cycle 56 skips the right-border set check for that line, leaving the side border open until the next line's border checks.
9. Border color is set by $D020; background color by $D021.
10. Closed vertical or side borders mask sprite output in the border area; sprites are visible in border pixels only when the corresponding border flip-flop is open.
11. Sprite-background visibility and collision checks treat closed border pixels as border pixels, not as foreground character or bitmap pixels.

### Source References

- `native/vice/vice/doc/vice.texi`: video settings, C64/C128 VIC-II features, display mode, border, raster, and palette behavior.
- `native/vice/vice/src/viciisc/vicii-draw-cycle.c`: `colors[]` and `draw_graphics()` define the VICE x64sc display-mode color routing for standard text, multicolor text, extended color, invalid modes, and foreground priority.
- `native/vice/vice/src/viciisc/vicii-cycle.c`: vertical and horizontal border flip-flop checks.
- `native/vice/vice/src/viciisc/vicii-chip-model.c`: PAL x64sc `ChkBrdL1`, `ChkBrdL0`, `ChkBrdR0`, and `ChkBrdR1` cycle-table entries.
- `native/vice/vice/src/vicii/vicii-mem.c`: RSEL/CSEL edge timing behavior around border comparisons.

### Traceability

- **Interfaces:** `IVideoChip`
- **Related FRs:** `FR-VIC-004`, `FR-VIC-005`, `FR-VIC-010`
- **Test Requirement:** `TEST-VIC-001`
- **Test Suite:** `VicIIBorderFlipFlopTests`, `VideoRendererTests`, `VideoSurfaceIntegrationTests`, `OpenBorderTrickTests`, `RselCselTimingTests`

---

## FR-VIC-008 Flexible Line Interpretation (FLI) Support

## FR-VIC-008: FLI / AFLI Support

**ID:** FR-VIC-008
**Title:** Flexible Line Interpretation (FLI) Support
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The emulator shall support Flexible Line Interpretation (FLI) and Advanced FLI (AFLI) techniques, which exploit badline forcing to change the character pointer base every raster line, enabling more than the normal 8 unique character colors per 8-pixel-tall cell.

### Acceptance Criteria

1. Forcing a badline every line (by changing Y-scroll each line) triggers a new c-access fetch each raster line.
2. The 3-cycle "FLI bug" (gray pixels at the left of each line due to the VIC-II not having fetched character data yet) is accurately reproduced.
3. Changing $D018 (VIC memory pointers) during specific cycles of a raster line takes effect for subsequent fetches on that line.
4. AFLI mode (combining FLI with bitmap mode) is supported.
5. The CPU cycle-accurate timing of the bank switch and pointer changes matches VICE x64sc behavior.

### Source References

- `native/vice/vice/doc/vice.texi`: video settings, C64/C128 VIC-II features, display mode, border, raster, and palette behavior.

### Traceability

- **Interfaces:** `IVideoChip`
- **Test Suite:** `FliTests`, `FliBugTests`, `AfliTests`

---

## FR-VIC-009 VIC-II Bank Switching (See also FR-MEM-004)

## FR-VIC-009: VIC-II Bank Switching

**ID:** FR-VIC-009
**Title:** VIC-II Bank Switching (See also FR-MEM-004)
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The VIC-II address generation shall correctly translate 14-bit VIC addresses (0-$3FFF) into the 16-bit system address space by prepending the bank selection bits from CIA2 Port A. Character ROM overlay detection shall occur within the VIC address translation.

### Acceptance Criteria

1. The VIC-II generates 14-bit addresses ($0000-$3FFF) that are offset by the bank base.
2. Character ROM overlay at VIC addresses $1000-$1FFF is active only in banks 0 and 2.
3. When the bank changes mid-frame, subsequent VIC-II fetches use the new bank immediately.
4. Sprite data fetches respect the active bank.
5. Bitmap data fetches respect the active bank.

### Source References

- `native/vice/vice/doc/vice.texi`: video settings, C64/C128 VIC-II features, display mode, border, raster, and palette behavior.

### Traceability

- **Interfaces:** `IVideoChip`, `IVicBankSelector`
- **Test Suite:** `VicAddressTranslationTests`, `MidFrameBankSwitchTests`

---

## FR-VIC-010 Sprite Multiplexing DMA Timing

## FR-VIC-010: Sprite Multiplexing DMA Timing

**ID:** FR-VIC-010
**Title:** Sprite Multiplexing DMA Timing
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The VIC-II sprite DMA shall be emulated with sub-cycle accuracy to support sprite multiplexing techniques. Each sprite's DMA pattern (p-access for pointer, s-access for data) occurs at specific cycles on each raster line. Software sprite multiplexers rely on exact knowledge of when these cycles occur.

### Acceptance Criteria

1. Each sprite's p-access and s-access cycles are table-driven by the active VIC-II model and must not be inferred from sprite number alone.
2. For PAL x64sc timing, VICE table cycles are sprites 3-7 at 1/2, 3/4, 5/6, 7/8, and 9/10; sprites 0-2 at 58/59, 60/61, and 62/63. In vice-sharp `CurrentCycle` / `RasterX` terms these normalize to sprites 3-7 at 0/1, 2/3, 4/5, 6/7, and 8/9; sprites 0-2 at 57/58, 59/60, and 61/62.
3. The CPU is halted according to the VICE BA/DMA mask for the matching sprite access slots. For PAL x64sc, sprites 3-7 consume their early-line p-/s-access slots after `sprite_dma` is latched on the prior late-line checks, so their BA lead covers the previous line's final cycles and the following line's first cycles.
4. `$D015` enable bits and sprite Y registers are sampled by VICE `check_sprite_dma` at PAL public cycles 55 and 56, which normalize to vice-sharp cycles 54 and 55. Once `sprite_dma` is latched, later `$D015` clears do not cancel the already-active DMA/BA window.
5. Disabling a sprite (clearing its bit in $D015) before both sprite-DMA check cycles prevents its DMA from occurring.
6. Re-enabling a sprite and setting its Y-position before a matching line's sprite-DMA checks triggers the applicable BA/data slots; missing those checks waits until a later line whose low raster byte matches the sprite Y value.
7. The exact cycle positions for each sprite's DMA match the VICE x64sc reference.

### Source References

- `native/vice/vice/doc/vice.texi`: video settings, C64/C128 VIC-II features, display mode, border, raster, and palette behavior.
- `native/vice/vice/src/viciisc/viciitypes.h`: `VICII_PAL_CYCLE(c)` maps public PAL cycle numbers to zero-based internal cycles.
- `native/vice/vice/src/viciisc/vicii-cycle.c`: `check_sprite_dma` samples `$D015` and sprite Y at PAL public cycles 55/56, latches `sprite_dma`, and uses that latch for later BA/data DMA until sprite MC base completion clears it.
- `native/vice/vice/src/viciisc/vicii-chip-model.c`: per-model sprite DMA fetch tables.
- `native/vice/vice/src/viciisc/vicii-fetch.c`: sprite pointer/data fetch operations.

### Traceability

- **Interfaces:** `IVideoChip`, `ISpriteUnit`
- **Related FRs:** `FR-VIC-004`, `FR-VIC-006`, `FR-VIC-007`
- **Technical Requirement:** `TR-CYCLE-001`
- **Test Requirement:** `TEST-VIC-001`, `TEST-X64SC-LOCKSTEP-001`
- **Test Suite:** `VicIISpriteDmaTests`, `VicIISpriteDmaStallTests`, `X64ScVariantLockstepTests`, `SpriteDmaTimingTests`, `SpriteMultiplexTests`, `SpriteDmaCycleTests`
