# Functional Requirements (QBrain.AI)

## FR-AIREVIEW-ROUTING-001 Auditable Astra xhigh AI review routing

Both required ViceSharp AI review theories run GPT-6 Astra with xhigh reasoning effort and retain independently verifiable completed review artifacts.
An xUnit NeverFails pass is not review success; malformed, error, incomplete, misrouted or missing artifacts fail the completion gate.
Critical and high findings are resolved and affected reviews rerun before completion.

## Acceptance Criteria
- **ac-01**: Both required ViceSharp AI review theories run GPT-6 Astra with xhigh reasoning effort and retain independently verifiable completed review artifacts.
- **ac-02**: An xUnit NeverFails pass is not review success; malformed, error, incomplete, misrouted or missing artifacts fail the completion gate.
- **ac-03**: Critical and high findings are resolved and affected reviews rerun before completion.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Both required ViceSharp AI review theories run GPT-6 Astra with xhigh reasoning effort and retain independently verifiable completed review artifacts.
- [ ] An xUnit NeverFails pass is not review success; malformed, error, incomplete, misrouted or missing artifacts fail the completion gate.
- [ ] Critical and high findings are resolved and affected reviews rerun before completion.

## FR-ARCH-TRUEDRIVE-1541-002 True-Drive 1541 IEC timing and motor ramp

IecBus.Tick() implements ATN-response state machine (CLK/DATA within 985 cycles of ATN assert). IecDrive.Tick() implements 300,000-cycle motor ramp before rotation. IecDrive.ReadSector(18,0) returns BAM bytes from D64. VICE iecbus.c:247-266, drive/drive.c.
Scope: layer-1+

## FR-BACKFILL-MEDIA-001 Media devices (D64, tape) I/O functional parity

D64DiskImageDevice sector R/W, motor ramp, BAM read all implemented. Datasette motor ramp, sense line, and record mode complete. Closes IEC ATN timing gap for Phase 1.
Scope: layer-1+

## FR-BACKFILL-VIDEO-001 VIC-II visible-frame parity (RC window, DMA checkpoints, screen-RAM)

VIC-II RC/VC state machine matches VICE cycle-accurate behavior (RC window). Native checkpoints validate sprite DMA for all 5 non-PAL models. Screen RAM survives one PAL frame unchanged. VICE viciisc/vicii-cycle.c:541-563, vicii-fetch.c:135-166.
Scope: layer-1+

## FR-CFG-001 Resource File and Command-Line Configuration

The emulator shall support persistent resource configuration and command-line or host-command overrides for emulator options that affect machine behavior and user-visible operation.

## Acceptance Criteria
- Resource files can persist named emulator settings.
- Command-line or host-command overrides can update resource-backed settings before or during a session when the setting is runtime-safe.
- Invalid resource names or values are rejected with diagnostics and do not corrupt the active configuration.
- Machine-specific resources are scoped to the active profile.
- Effective resource values can be queried for diagnostics.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Resource files can persist named emulator settings.
- [ ] Command-line or host-command overrides can update resource-backed settings before or during a session when the setting is runtime-safe.
- [ ] Invalid resource names or values are rejected with diagnostics and do not corrupt the active configuration.
- [ ] Machine-specific resources are scoped to the active profile.
- [ ] Effective resource values can be queried for diagnostics.

## FR-CFG-002 ROM and Romset Selection

The emulator shall load machine ROM images and named romsets needed by the selected machine profile, validate them before use, and report missing or invalid ROMs without starting an invalid machine session.

## Acceptance Criteria
- Machine profiles declare required and optional ROM artifacts.
- ROM files are validated for size and checksum when known.
- Named romsets can be selected for a session.
- Missing or invalid ROMs produce actionable diagnostics.
- ROM selection does not require UI code to access emulator runtime internals.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Machine profiles declare required and optional ROM artifacts.
- [ ] ROM files are validated for size and checksum when known.
- [ ] Named romsets can be selected for a session.
- [ ] Missing or invalid ROMs produce actionable diagnostics.
- [ ] ROM selection does not require UI code to access emulator runtime internals.

## FR-CFG-003 Palette Selection and Color Resource Handling

The emulator shall support selecting palette resources for video output so rendered frames can match documented machine and display color profiles.

## Acceptance Criteria
- Palette files can be discovered and selected for compatible machine/video profiles.
- Invalid palette files report diagnostics and do not replace the active palette.
- Palette changes apply at a frame boundary.
- Captured frames include enough palette metadata to reproduce displayed colors.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Palette files can be discovered and selected for compatible machine/video profiles.
- [ ] Invalid palette files report diagnostics and do not replace the active palette.
- [ ] Palette changes apply at a frame boundary.
- [ ] Captured frames include enough palette metadata to reproduce displayed colors.

## FR-CFG-004 Hotkey Configuration and Action Dispatch

The host UI shall allow configurable hotkeys to trigger emulator actions such as reset, media operations, monitor commands, snapshots, and settings actions.

## Acceptance Criteria
- Hotkey configuration files can define action mappings.
- Hotkeys dispatch to host commands rather than UI-local emulator operations.
- Invalid hotkey directives are reported without disabling unrelated mappings.
- Hotkeys can be scoped by UI mode so monitor text entry is not intercepted unexpectedly.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Hotkey configuration files can define action mappings.
- [ ] Hotkeys dispatch to host commands rather than UI-local emulator operations.
- [ ] Invalid hotkey directives are reported without disabling unrelated mappings.
- [ ] Hotkeys can be scoped by UI mode so monitor text entry is not intercepted unexpectedly.

## FR-CFG-005 Autostart and Program Launch Handling

The emulator shall support autostarting programs from disk, tape, cartridge, or supported archive/image sources by attaching required media and injecting the appropriate machine commands.

## Acceptance Criteria
- Autostart can identify supported media/image sources and select an applicable launch path.
- Autostart attaches required media through host media services.
- Autostart injects launch commands through host input/control services at deterministic boundaries.
- Autostart failures leave existing media and machine state unchanged unless explicitly committed.
- Reset-plus-drive-8 autorun succeeds for a readable drive 8 D64 containing a runnable PRG and reports an explicit failed-precondition status when required host, media, runtime, or keyboard automation prerequisites are missing.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Autostart can identify supported media/image sources and select an applicable launch path.
- [ ] Autostart attaches required media through host media services.
- [ ] Autostart injects launch commands through host input/control services at deterministic boundaries.
- [ ] Autostart failures leave existing media and machine state unchanged unless explicitly committed.
- [ ] Reset-plus-drive-8 autorun succeeds for a readable drive 8 D64 containing a runnable PRG and reports an explicit failed-precondition status when required host, media, runtime, or keyboard automation prerequisites are missing.

## FR-CFG-006 Host-Backed Peripheral Resource Configuration

The emulator shall expose configuration for host-backed peripheral devices such as filesystem devices, printers, RS232, Ethernet, tape-port devices, and user-port devices through host-owned services.

## Acceptance Criteria
- Peripheral devices can be enabled, disabled, and configured through host/session settings.
- Host-backed paths or endpoints are validated before the emulator commits the configuration.
- Peripheral status and errors are visible through host diagnostics.
- UI clients do not access host files, serial devices, sockets, or printers except through host service requests.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Peripheral devices can be enabled, disabled, and configured through host/session settings.
- [ ] Host-backed paths or endpoints are validated before the emulator commits the configuration.
- [ ] Peripheral status and errors are visible through host diagnostics.
- [ ] UI clients do not access host files, serial devices, sockets, or printers except through host service requests.

## FR-CFG-007 RAM Initialization and Debug Resource Behavior

The emulator shall support configurable RAM initialization and debug resource behavior needed for repeatable startup, diagnostics, and compatibility testing.

## Acceptance Criteria
- RAM initialization patterns can be selected before machine start.
- Given the same selected pattern and ROM/profile inputs, startup RAM state is deterministic.
- Debug resources can enable additional diagnostics without changing normal emulation behavior when disabled.
- Effective RAM/debug settings are captured in snapshot or diagnostic metadata where relevant.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] RAM initialization patterns can be selected before machine start.
- [ ] Given the same selected pattern and ROM/profile inputs, startup RAM state is deterministic.
- [ ] Debug resources can enable additional diagnostics without changing normal emulation behavior when disabled.
- [ ] Effective RAM/debug settings are captured in snapshot or diagnostic metadata where relevant.

## FR-CFG-008 Performance Limiter Configuration

The emulator shall expose performance limiter settings separately from measured runtime telemetry so users can request a throttle target and observe actual emulation speed.

## Acceptance Criteria
- A limiter target can be configured through host/session settings.
- Host status reports requested limiter target separately from measured FPS and effective clock speed.
- Limiter changes apply without resetting the active machine session.
- Invalid limiter values are rejected with diagnostics.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A limiter target can be configured through host/session settings.
- [ ] Host status reports requested limiter target separately from measured FPS and effective clock speed.
- [ ] Limiter changes apply without resetting the active machine session.
- [ ] Invalid limiter values are rejected with diagnostics.

## FR-CHIPSTATE-001 Per-tick full chip state capture

Each captured tick snapshots the full internal state of every stateful chip (VIC, SID, CIA, PLA) for display in the debug screen.
Scope: layer-1+
**Acceptance Criteria:**
- [x] VIC, SID, CIA and PLA each implement IStatefulDevice
- [x] Each chip's state is captured per tick with zero hot-path allocation
- [x] Captured state decodes into named register/field values
- [x] The debug screen shows each chip's decoded state at the selected tick

## FR-CIA-001 CIA Timer A and Timer B with Cascade Mode

Each CIA chip has two 16-bit countdown timers (Timer A and Timer B). Timers can count system clock cycles or external events. Timer B can optionally cascade from Timer A (counting Timer A underflows). Timers can operate in one-shot or continuous mode and generate interrupts on underflow.

## Acceptance Criteria
- Timer A counts down from its latch value each system clock cycle (when configured for system clock input).
- Timer B can count system clock cycles or Timer A underflows (cascade mode, selected by bits 5-6 of CRB).
- On underflow, the timer reloads from its latch value on the next cycle.
- One-shot mode (bit 3 of control register) stops the timer after a single underflow.
- Continuous mode restarts the timer automatically after underflow.
- Writing to the timer latch registers does not affect the running counter until the next underflow (or forced load via bit 4 of control register).
- Force-load (strobe bit 4) immediately copies the latch to the counter.
- Timer underflow toggles the PB6 (Timer A) or PB7 (Timer B) output pin when configured.
- The 1-cycle delay between timer reaching zero and the interrupt being asserted is modeled.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Timer A counts down from its latch value each system clock cycle (when configured for system clock input).
- [ ] Timer B can count system clock cycles or Timer A underflows (cascade mode, selected by bits 5-6 of CRB).
- [ ] On underflow, the timer reloads from its latch value on the next cycle.
- [ ] One-shot mode (bit 3 of control register) stops the timer after a single underflow.
- [ ] Continuous mode restarts the timer automatically after underflow.
- [ ] Writing to the timer latch registers does not affect the running counter until the next underflow (or forced load via bit 4 of control register).
- [ ] Force-load (strobe bit 4) immediately copies the latch to the counter.
- [ ] Timer underflow toggles the PB6 (Timer A) or PB7 (Timer B) output pin when configured.
- [ ] The 1-cycle delay between timer reaching zero and the interrupt being asserted is modeled.

## FR-CIA-002 Time-of-Day Clock

Each CIA has a Time-of-Day clock that counts in BCD format with tenths-of-seconds, seconds, minutes, and hours (12-hour format with AM/PM). The TOD is clocked by the 50Hz (PAL) or 60Hz (NTSC) power line frequency and can generate an interrupt on alarm match.

## Acceptance Criteria
- TOD registers at offsets $08-$0B provide tenths, seconds, minutes, hours in BCD format.
- The TOD increments at 50Hz (PAL) or 60Hz (NTSC) derived from the video sync signal.
- Writing to the hours register (offset $0B) latches the TOD display; it resumes when tenths ($08) is written.
- Reading the hours register latches all TOD registers; they unfreeze when tenths is read.
- A TOD alarm can be set by writing to the same registers with bit 7 of CRB set.
- When the TOD matches the alarm value, the TOD interrupt bit is set in the ICR.
- Hours register bit 7 indicates AM (0) or PM (1).
Scope: layer-1+
**Acceptance Criteria:**
- [ ] TOD registers at offsets $08-$0B provide tenths, seconds, minutes, hours in BCD format.
- [ ] The TOD increments at 50Hz (PAL) or 60Hz (NTSC) derived from the video sync signal.
- [ ] Writing to the hours register (offset $0B) latches the TOD display; it resumes when tenths ($08) is written.
- [ ] Reading the hours register latches all TOD registers; they unfreeze when tenths is read.
- [ ] A TOD alarm can be set by writing to the same registers with bit 7 of CRB set.
- [ ] When the TOD matches the alarm value, the TOD interrupt bit is set in the ICR.
- [ ] Hours register bit 7 indicates AM (0) or PM (1).

## FR-CIA-003 Keyboard Matrix Scanning via CIA1

CIA1 Port A and Port B are connected to the C64 keyboard matrix (8x8 matrix yielding 64 key positions). Port A selects the column (active-low output) and Port B reads the row (active-low input). Multiple keys can be detected simultaneously, including handling of ghosting and the RESTORE key (directly connected to NMI).

## Acceptance Criteria
- Writing to CIA1 Port A ($DC00) selects which keyboard columns are driven low.
- Reading CIA1 Port B ($DC01) returns the row state for the selected columns (0 = key pressed).
- Multiple columns can be selected simultaneously for matrix scanning.
- Key ghosting behavior matches hardware (pressing 3 keys in an L-shape can ghost a 4th).
- The RESTORE key is not part of the matrix; it directly triggers NMI via CIA2.
- The `IKeyboardMatrix` interface allows host-side key press/release events to set matrix state.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Writing to CIA1 Port A ($DC00) selects which keyboard columns are driven low.
- [ ] Reading CIA1 Port B ($DC01) returns the row state for the selected columns (0 = key pressed).
- [ ] Multiple columns can be selected simultaneously for matrix scanning.
- [ ] Key ghosting behavior matches hardware (pressing 3 keys in an L-shape can ghost a 4th).
- [ ] The RESTORE key is not part of the matrix; it directly triggers NMI via CIA2.
- [ ] The `IKeyboardMatrix` interface allows host-side key press/release events to set matrix state.

## FR-CIA-004 Joystick Port Reading via CIA1

Joystick port 2 shares CIA1 Port A with the keyboard columns, and joystick port 1 shares CIA1 Port B with the keyboard rows. The joystick signals (up, down, left, right, fire) override keyboard reads when active.

## Acceptance Criteria
- Joystick port 2 is read from CIA1 Port A ($DC00) bits 0-4 (active low).
- Joystick port 1 is read from CIA1 Port B ($DC01) bits 0-4 (active low).
- Bit mapping: 0=Up, 1=Down, 2=Left, 3=Right, 4=Fire.
- Joystick signals OR with keyboard matrix signals (both active-low, so a joystick direction can interfere with keyboard reading).
- The `IJoystickPort` interface allows the host to set joystick state (direction + fire).
- Simultaneous joystick and keyboard reading conflicts are handled as on real hardware.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Joystick port 2 is read from CIA1 Port A ($DC00) bits 0-4 (active low).
- [ ] Joystick port 1 is read from CIA1 Port B ($DC01) bits 0-4 (active low).
- [ ] Bit mapping: 0=Up, 1=Down, 2=Left, 3=Right, 4=Fire.
- [ ] Joystick signals OR with keyboard matrix signals (both active-low, so a joystick direction can interfere with keyboard reading).
- [ ] The `IJoystickPort` interface allows the host to set joystick state (direction + fire).
- [ ] Simultaneous joystick and keyboard reading conflicts are handled as on real hardware.

## FR-CIA-005 CIA Serial Port Shift Register

Each CIA has an 8-bit serial shift register that can transmit or receive data bit-by-bit, clocked by Timer A or an external clock. CIA2's serial port is used for the IEC serial bus connection to disk drives.

## Acceptance Criteria
- The shift register at offset $0C shifts one bit per Timer A underflow (output mode) or external clock edge (input mode).
- Direction is controlled by bit 6 of CRA: 0 = input, 1 = output.
- After 8 bits are shifted, the SDR interrupt bit is set in the ICR.
- In output mode, data written to the SDR register is loaded into the shift register on the next Timer A underflow.
- In input mode, incoming bits are shifted in on each external clock edge.
- The serial data appears on the SP pin and the clock on the CNT pin.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The shift register at offset $0C shifts one bit per Timer A underflow (output mode) or external clock edge (input mode).
- [ ] Direction is controlled by bit 6 of CRA: 0 = input, 1 = output.
- [ ] After 8 bits are shifted, the SDR interrupt bit is set in the ICR.
- [ ] In output mode, data written to the SDR register is loaded into the shift register on the next Timer A underflow.
- [ ] In input mode, incoming bits are shifted in on each external clock edge.
- [ ] The serial data appears on the SP pin and the clock on the CNT pin.

## FR-CIA-006 NMI Generation from CIA2

CIA2 generates Non-Maskable Interrupts (NMI) to the CPU. NMI sources include Timer A underflow, Timer B underflow, TOD alarm, serial port completion, and the FLAG pin (directly active-low edge from the IEC bus).

## Acceptance Criteria
- CIA2 interrupt output is connected to the CPU NMI line.
- Any enabled interrupt source in CIA2 ICR/IMR triggers an NMI.
- The NMI is edge-triggered: it fires on the transition from no-interrupt to interrupt-pending.
- Reading the CIA2 ICR ($DD0D) clears all interrupt flags and deasserts the NMI line.
- The FLAG pin (directly from IEC ATN) can trigger NMI when enabled.
- Multiple NMI sources can be pending simultaneously; reading ICR reveals which sources fired.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] CIA2 interrupt output is connected to the CPU NMI line.
- [ ] Any enabled interrupt source in CIA2 ICR/IMR triggers an NMI.
- [ ] The NMI is edge-triggered: it fires on the transition from no-interrupt to interrupt-pending.
- [ ] Reading the CIA2 ICR ($DD0D) clears all interrupt flags and deasserts the NMI line.
- [ ] The FLAG pin (directly from IEC ATN) can trigger NMI when enabled.
- [ ] Multiple NMI sources can be pending simultaneously; reading ICR reveals which sources fired.

## FR-CIA-007 IRQ Generation from CIA1

CIA1 generates Interrupt Requests (IRQ) to the CPU. IRQ sources mirror those of CIA2 (timers, TOD, serial, FLAG) but are connected to the IRQ line instead of NMI.

## Acceptance Criteria
- CIA1 interrupt output is connected to the CPU IRQ line.
- Any enabled interrupt source in CIA1 ICR/IMR asserts IRQ.
- IRQ is level-triggered: it remains asserted as long as any enabled source has its flag set.
- Reading CIA1 ICR ($DC0D) clears all interrupt flags and deasserts the IRQ line (if no other sources remain).
- The IRQ mask register ($DC0D write) selects which sources can generate IRQ (bit 7 = set/clear control).
- Timer A, Timer B, TOD alarm, SDR complete, and FLAG are all independently maskable.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] CIA1 interrupt output is connected to the CPU IRQ line.
- [ ] Any enabled interrupt source in CIA1 ICR/IMR asserts IRQ.
- [ ] IRQ is level-triggered: it remains asserted as long as any enabled source has its flag set.
- [ ] Reading CIA1 ICR ($DC0D) clears all interrupt flags and deasserts the IRQ line (if no other sources remain).
- [ ] The IRQ mask register ($DC0D write) selects which sources can generate IRQ (bit 7 = set/clear control).
- [ ] Timer A, Timer B, TOD alarm, SDR complete, and FLAG are all independently maskable.

## FR-COMPLETION-001 Exhaustive approved-work completion evidence

Complete every approved slice, including baseline repairs, Settings/Warp/video, portable CLI, native lifecycle, external integration and both Astra AI reviews.
All required tests finish with zero failures, skips, aborts or silent prerequisite returns before completion.
Run two fresh live application validation passes against final binaries and prove runtime effects independently of UI values.
Preserve unrelated dirty files and probes. Finalize tracked requirements, plan, handoff and receipts before explicit-manifest commit and non-force push; verify local and remote SHAs match.
Independent Astra xhigh agreement is required after requirements, red tests, green implementation and final combined evidence.

## Acceptance Criteria
- **ac-01**: Complete every approved slice, including baseline repairs, Settings/Warp/video, portable CLI, native lifecycle, external integration and both Astra AI reviews.
- **ac-02**: All required tests finish with zero failures, skips, aborts or silent prerequisite returns before completion.
- **ac-03**: Run two fresh live application validation passes against final binaries and prove runtime effects independently of UI values.
- **ac-04**: Preserve unrelated dirty files and probes. Finalize tracked requirements, plan, handoff and receipts before explicit-manifest commit and non-force push; verify local and remote SHAs match.
- **ac-05**: Independent Astra xhigh agreement is required after requirements, red tests, green implementation and final combined evidence.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Complete every approved slice, including baseline repairs, Settings/Warp/video, portable CLI, native lifecycle, external integration and both Astra AI reviews.
- [ ] All required tests finish with zero failures, skips, aborts or silent prerequisite returns before completion.
- [ ] Run two fresh live application validation passes against final binaries and prove runtime effects independently of UI values.
- [ ] Preserve unrelated dirty files and probes. Finalize tracked requirements, plan, handoff and receipts before explicit-manifest commit and non-force push; verify local and remote SHAs match.
- [ ] Independent Astra xhigh agreement is required after requirements, red tests, green implementation and final combined evidence.

## FR-CPU-001 Full 6502/6510 Instruction Set Including Undocumented Opcodes

The CPU emulation shall implement the complete MOS 6502/6510 instruction set, including all 256 opcode entries. This encompasses the 151 official opcodes as well as all undocumented/illegal opcodes (LAX, SAX, DCP, ISC, SLO, RLA, SRE, RRA, ANC, ALR, ARR, SBX, LAS, SHA, SHX, SHY, TAS, NOP variants, JAM/KIL). Behavior of undocumented opcodes shall match the known behavior documented by the VICE team and confirmed by hardware testing.

## Acceptance Criteria
- All 151 official 6502 opcodes execute with correct results for all addressing modes.
- All undocumented opcodes produce results matching the Lorenz test suite (C64 Tester) pass criteria.
- The BCD flag affects ADC/SBC results correctly when the decimal flag is set (6502 mode) and is a no-op on the 6510 in CMOS variants.
- All addressing modes are implemented: Immediate, Zero Page, Zero Page X/Y, Absolute, Absolute X/Y, Indirect, (Indirect,X), (Indirect),Y, Relative, Implied, Accumulator.
- JAM/KIL opcodes halt the CPU and raise a diagnostic event on `ICpu.Jammed`.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] All 151 official 6502 opcodes execute with correct results for all addressing modes.
- [ ] All undocumented opcodes produce results matching the Lorenz test suite (C64 Tester) pass criteria.
- [ ] The BCD flag affects ADC/SBC results correctly when the decimal flag is set (6502 mode) and is a no-op on the 6510 in CMOS variants.
- [ ] All addressing modes are implemented: Immediate, Zero Page, Zero Page X/Y, Absolute, Absolute X/Y, Indirect, (Indirect,X), (Indirect),Y, Relative, Implied, Accumulator.
- [ ] JAM/KIL opcodes halt the CPU and raise a diagnostic event on `ICpu.Jammed`.

## FR-CPU-002 Cycle-Accurate Execution Timing

Each instruction shall consume the exact number of clock cycles as the real MOS 6510, including the extra cycle for page-boundary crossings on indexed addressing modes, the extra cycle for taken branches, and the additional cycle for page-crossing branches. The CPU fetch, decode, and execute sub-cycles shall match VICE x64sc so other devices observe and react at sub-instruction granularity.

## Acceptance Criteria

- Every opcode consumes exactly the documented cycle count (per the "MOS 6510 Unintended Opcodes" reference and 64doc.txt).

- Page-boundary crossing adds exactly one cycle for absolute indexed reads (LDA abs,X / LDA abs,Y / LDA (zp),Y).

- Page-boundary crossing on write instructions (STA abs,X) always takes the extra cycle regardless of whether a page boundary is actually crossed.

- Taken branches add one cycle; taken branches crossing a page boundary add two cycles.

- Each clock phase advances once, and the CPU fetch, decode, and execute sub-cycle state at that phase matches VICE x64sc.

- The CIA/VIC-II timing interleave passes the VICE timing test suite.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Every opcode consumes exactly the documented cycle count (per the "MOS 6510 Unintended Opcodes" reference and 64doc.txt).
- [ ] Page-boundary crossing adds exactly one cycle for absolute indexed reads (LDA abs,X / LDA abs,Y / LDA (zp),Y).
- [ ] Page-boundary crossing on write instructions (STA abs,X) always takes the extra cycle regardless of whether a page boundary is actually crossed.
- [ ] Taken branches add one cycle; taken branches crossing a page boundary add two cycles.
- [ ] Each clock phase advances once, and the CPU fetch, decode, and execute sub-cycle state at that phase matches VICE x64sc.
- [ ] The CIA/VIC-II timing interleave passes the VICE timing test suite.

## FR-CPU-003 Interrupt Handling with Correct Timing

The CPU shall correctly handle hardware interrupts (IRQ, NMI) and the software BRK instruction. IRQ is level-triggered and masked by the I flag. NMI is edge-triggered on the falling edge. The interrupt sequence shall take exactly 7 cycles. Interrupt hijacking (where an NMI arrives during an IRQ sequence or vice versa) shall be handled correctly per real hardware behavior.

## Acceptance Criteria
- IRQ is sampled during the penultimate cycle of each instruction; when asserted and the I flag is clear, the IRQ sequence begins after the current instruction completes.
- NMI is edge-detected (falling edge); once detected, it is serviced after the current instruction regardless of the I flag.
- The BRK instruction pushes PC+2 (not PC+1) and sets the B flag in the pushed status register.
- Interrupt hijacking: if NMI occurs during the vector-fetch cycles of an IRQ, the NMI vector ($FFFA) is fetched instead of the IRQ vector ($FFFE).
- The interrupt acknowledge sequence takes exactly 7 cycles.
- The `IInterruptController` interface allows external devices to assert/deassert IRQ and NMI lines independently.
- Passes the Lorenz CIA interrupt timing tests.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] IRQ is sampled during the penultimate cycle of each instruction; when asserted and the I flag is clear, the IRQ sequence begins after the current instruction completes.
- [ ] NMI is edge-detected (falling edge); once detected, it is serviced after the current instruction regardless of the I flag.
- [ ] The BRK instruction pushes PC+2 (not PC+1) and sets the B flag in the pushed status register.
- [ ] Interrupt hijacking: if NMI occurs during the vector-fetch cycles of an IRQ, the NMI vector ($FFFA) is fetched instead of the IRQ vector ($FFFE).
- [ ] The interrupt acknowledge sequence takes exactly 7 cycles.
- [ ] The `IInterruptController` interface allows external devices to assert/deassert IRQ and NMI lines independently.
- [ ] Passes the Lorenz CIA interrupt timing tests.

## FR-CPU-004 6510 I/O Port at Addresses $0000/$0001

The MOS 6510 processor's built-in I/O port at addresses $0000 (data direction register) and $0001 (data port) shall be emulated. This port controls the PLA memory banking configuration (LORAM, HIRAM, CHAREN bits), the cassette motor, and the cassette sense line. The data port shall exhibit the correct behavior with pull-up/pull-down resistors and the capacitive discharge timing that affects reads of output pins.

## Acceptance Criteria
- Address $0000 is the Data Direction Register (DDR); bits set to 1 configure the corresponding $0001 bit as output.
- Address $0001 bits 0-2 (LORAM, HIRAM, CHAREN) control PLA banking and are reflected immediately in the address space configuration.
- Bit 3 controls the cassette motor (active low).
- Bit 4 reads the cassette switch sense (active low).
- Bit 5 is unused on C64 but reads as expected based on DDR/pull-up behavior.
- When a bit transitions from output to input, the capacitive discharge timing (approximately 350 microseconds to decay) is modeled.
- The `IMemoryMappedDevice` interface exposes the port to the address decoder.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Address $0000 is the Data Direction Register (DDR); bits set to 1 configure the corresponding $0001 bit as output.
- [ ] Address $0001 bits 0-2 (LORAM, HIRAM, CHAREN) control PLA banking and are reflected immediately in the address space configuration.
- [ ] Bit 3 controls the cassette motor (active low).
- [ ] Bit 4 reads the cassette switch sense (active low).
- [ ] Bit 5 is unused on C64 but reads as expected based on DDR/pull-up behavior.
- [ ] When a bit transitions from output to input, the capacitive discharge timing (approximately 350 microseconds to decay) is modeled.
- [ ] The `IMemoryMappedDevice` interface exposes the port to the address decoder.

## FR-CPU-005 8502 2MHz Mode Support for C128

For C128 machine profile support, the CPU shall implement the MOS 8502 processor's ability to switch between 1MHz and 2MHz operation. In 2MHz mode, the VIC-II cannot access the bus, so the display shows a static pattern. The clock speed is controlled by bit 0 of the VIC-II register at $D030.

## Acceptance Criteria
- Writing bit 0 of $D030 switches the CPU clock between 1MHz (0) and 2MHz (1).
- In 2MHz mode, the CPU executes instructions at double speed (halved cycle duration).
- In 2MHz mode, VIC-II DMA does not occur and the screen displays the characteristic "static" pattern.
- I/O chip access (CIAs, SID, VIC-II registers) forces the clock back to 1MHz for the duration of the access.
- The `IClockController` interface exposes the current clock rate and allows mode transitions.
- CIA timers and SID continue to operate at their expected rates relative to the system clock.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Writing bit 0 of $D030 switches the CPU clock between 1MHz (0) and 2MHz (1).
- [ ] In 2MHz mode, the CPU executes instructions at double speed (halved cycle duration).
- [ ] In 2MHz mode, VIC-II DMA does not occur and the screen displays the characteristic "static" pattern.
- [ ] I/O chip access (CIAs, SID, VIC-II registers) forces the clock back to 1MHz for the duration of the access.
- [ ] The `IClockController` interface exposes the current clock rate and allows mode transitions.
- [ ] CIA timers and SID continue to operate at their expected rates relative to the system clock.

## FR-CPUTICK-001 Per-CPU independent tick counter and per-CPU speed metric

Each CPU in the emulator keeps its own independent executed-cycle counter and the displayed speed is that CPU's executed-cycle rate versus its own target clock.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Each CPU ExecutedCycles increments once per executed cycle and not on a stolen or skipped cycle.
- [ ] Per-CPU speed percent equals delta-executed over delta-wall over targetHz; the C64 primary reads about 95-100 percent at real time and the drive about 100 percent when running.
- [ ] The status surface lists per-CPU rate entries for the host and each peripheral CPU distinctly.
- [ ] Machine reset zeroes every CPU's executed-cycle counter.

## FR-CRT-001 Standard 8K and 16K Cartridge Support

The emulator shall support standard 8KB and 16KB cartridge images. An 8K cartridge maps to ROML ($8000-$9FFF) with EXROM asserted. A 16K cartridge maps to both ROML ($8000-$9FFF) and ROMH ($A000-$BFFF or $E000-$FFFF depending on Ultimax mode) with both GAME and EXROM asserted.

## Acceptance Criteria
- CRT file format (VICE cartridge image format) headers are correctly parsed.
- 8K cartridges map to ROML at $8000-$9FFF; EXROM=0, GAME=1 (active-low).
- 16K cartridges map to ROML at $8000-$9FFF and ROMH at $A000-$BFFF; EXROM=0, GAME=0.
- The cartridge autostart vector at $8000 (magic bytes $C3, $C2, $CD, $38, $30) is detected and the cold-start routine is called.
- Cartridge ROM is read-only; writes to the cartridge address range go to underlying RAM.
- Cartridges can be inserted and removed at runtime via `ICartridgePort.Insert()`/`ICartridgePort.Remove()`.
- Removing a cartridge restores the default banking configuration.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] CRT file format (VICE cartridge image format) headers are correctly parsed.
- [ ] 8K cartridges map to ROML at $8000-$9FFF; EXROM=0, GAME=1 (active-low).
- [ ] 16K cartridges map to ROML at $8000-$9FFF and ROMH at $A000-$BFFF; EXROM=0, GAME=0.
- [ ] The cartridge autostart vector at $8000 (magic bytes $C3, $C2, $CD, $38, $30) is detected and the cold-start routine is called.
- [ ] Cartridge ROM is read-only; writes to the cartridge address range go to underlying RAM.
- [ ] Cartridges can be inserted and removed at runtime via `ICartridgePort.Insert()`/`ICartridgePort.Remove()`.
- [ ] Removing a cartridge restores the default banking configuration.

## FR-CRT-002 Ocean Type 1 Bank-Switching Cartridge

The Ocean Type 1 cartridge uses bank switching to map up to 512KB of ROM into the ROML region. Bank selection is performed by writing to the $DE00 I/O area. This cartridge type is used by many commercial games.

## Acceptance Criteria
- Writing to $DE00 selects the active ROM bank (bits 0-5 select from up to 64 banks of 8KB each).
- The selected bank is mapped at ROML ($8000-$9FFF).
- ROMH can optionally be present (some Ocean carts use both ROML and ROMH).
- Maximum size is 512KB (64 x 8KB banks).
- The EXROM/GAME line configuration follows the Ocean Type 1 specification.
- The cartridge CRT file chip packets correctly identify bank numbers.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Writing to $DE00 selects the active ROM bank (bits 0-5 select from up to 64 banks of 8KB each).
- [ ] The selected bank is mapped at ROML ($8000-$9FFF).
- [ ] ROMH can optionally be present (some Ocean carts use both ROML and ROMH).
- [ ] Maximum size is 512KB (64 x 8KB banks).
- [ ] The EXROM/GAME line configuration follows the Ocean Type 1 specification.
- [ ] The cartridge CRT file chip packets correctly identify bank numbers.

## FR-CRT-003 EasyFlash Cartridge with Flash Memory

The EasyFlash is a modern cartridge with 1MB of flash memory (2 x 512KB banks for ROML and ROMH) and 256 bytes of RAM at $DF00. It supports runtime programming of flash memory and flexible banking.

## Acceptance Criteria
- Two 512KB flash chips provide 64 banks of 8KB each for ROML ($8000-$9FFF) and ROMH ($A000-$BFFF or $E000-$FFFF).
- The bank register at $DE00 selects the active bank (bits 0-5).
- The control register at $DE02 configures GAME/EXROM lines (bits 0-1) and LED state (bit 7).
- 256 bytes of battery-backed RAM are accessible at $DF00-$DFFF.
- Flash memory programming (byte-level writes using the standard flash command sequences) is emulated.
- Flash erase (sector and chip erase) operations are supported.
- EasyFlash CRT images are loaded with correct bank and chip assignments.
- Modified flash contents can be saved back to the CRT file via `IFlashMemory.Save()`.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Two 512KB flash chips provide 64 banks of 8KB each for ROML ($8000-$9FFF) and ROMH ($A000-$BFFF or $E000-$FFFF).
- [ ] The bank register at $DE00 selects the active bank (bits 0-5).
- [ ] The control register at $DE02 configures GAME/EXROM lines (bits 0-1) and LED state (bit 7).
- [ ] 256 bytes of battery-backed RAM are accessible at $DF00-$DFFF.
- [ ] Flash memory programming (byte-level writes using the standard flash command sequences) is emulated.
- [ ] Flash erase (sector and chip erase) operations are supported.
- [ ] EasyFlash CRT images are loaded with correct bank and chip assignments.
- [ ] Modified flash contents can be saved back to the CRT file via `IFlashMemory.Save()`.

## FR-CRT-004 Action Replay and Retro Replay Cartridge

The Action Replay (and its modern successor Retro Replay) is a utility cartridge providing a machine monitor, fast loader, freezer function, and other tools. It uses bank switching, RAM expansion, and the FREEZE button feature.

## Acceptance Criteria
- The Action Replay ROM is mapped according to the cartridge's banking logic ($DE00/$DE01 control registers).
- The FREEZE button (directly asserting NMI + GAME/EXROM change) triggers the freezer entry point.
- The cartridge's 8KB RAM is accessible when banked in.
- Bank switching between ROM pages is controlled via writes to $DE00.
- The cartridge can be disabled (effectively removed from the bus) by the control register.
- REU (RAM Expansion Unit) passthrough is supported on Retro Replay.
- The cartridge CRT format for Action Replay and Retro Replay is correctly parsed.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The Action Replay ROM is mapped according to the cartridge's banking logic ($DE00/$DE01 control registers).
- [ ] The FREEZE button (directly asserting NMI + GAME/EXROM change) triggers the freezer entry point.
- [ ] The cartridge's 8KB RAM is accessible when banked in.
- [ ] Bank switching between ROM pages is controlled via writes to $DE00.
- [ ] The cartridge can be disabled (effectively removed from the bus) by the control register.
- [ ] REU (RAM Expansion Unit) passthrough is supported on Retro Replay.
- [ ] The cartridge CRT format for Action Replay and Retro Replay is correctly parsed.

## FR-CRT-005 Final Cartridge III (FC3)

The Final Cartridge III is a utility cartridge with 64KB of ROM in 4 banks, providing a desktop environment, fast loader, freezer, and machine monitor. It uses a complex banking scheme controlled by writes to $DFFF.

## Acceptance Criteria
- Four 16KB ROM banks are selectable via writes to $DFFF.
- Bits 0-1 of $DFFF select the bank; bit 4 controls NMI generation; bits 5-6 control GAME/EXROM.
- The FREEZE button triggers NMI and banks in the FC3 ROM.
- The cartridge's fast loader functionality works correctly.
- The desktop environment (GUI) launches on cold start when configured.
- The FC3 can be disabled by writing the appropriate value to $DFFF (setting GAME=1, EXROM=1).
- The CRT file format for FC3 is correctly parsed with all 4 bank chips.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Four 16KB ROM banks are selectable via writes to $DFFF.
- [ ] Bits 0-1 of $DFFF select the bank; bit 4 controls NMI generation; bits 5-6 control GAME/EXROM.
- [ ] The FREEZE button triggers NMI and banks in the FC3 ROM.
- [ ] The cartridge's fast loader functionality works correctly.
- [ ] The desktop environment (GUI) launches on cold start when configured.
- [ ] The FC3 can be disabled by writing the appropriate value to $DFFF (setting GAME=1, EXROM=1).
- [ ] The CRT file format for FC3 is correctly parsed with all 4 bank chips.

## FR-CSDB-001 CSDb discovery, ingest, scan, refresh

PLAN-ROMM-001. Search CSDb scene releases, ingest into RomM, scan, and refresh the library.
AC-CSDB-01: SearchAsync returns classified hits (demo/crack/sid) -> CsdbDiscoveryViewModelTests.Search_ReturnsHits.
AC-CSDB-02: ingest selection capped at 20 -> CsdbDiscoveryViewModelTests.Ingest_CapsAt20.
AC-CSDB-03: LocalCsdbGateway ingests via CsdbRomMWorkflow then triggers scan -> LocalCsdbGatewayTests.Ingest_WritesThenScans.
AC-CSDB-04: BridgeCsdbGateway POSTs /csdb/v1/ingest then calls RomMClient.Tasks.ScanLibraryAsync (bridge does not scan) -> BridgeCsdbGatewayTests.Posts_ThenScans.
AC-CSDB-05: completion raises LibraryRefreshRequested -> CsdbDiscoveryViewModelTests.Ingest_RaisesRefresh.
AC-CSDB-06: gateway selection by config (roms root writable -> Local, else Bridge) -> CsdbGatewaySelectionTests.PicksByConfig.

## Acceptance Criteria
- `SearchAsync` returns classified hits (demo/crack/sid) ->
- ingest selection capped at 20 -> `CsdbDiscoveryViewModelTests.Ingest_CapsAt20`.
- `LocalCsdbGateway` ingests via `CsdbRomMWorkflow` then triggers scan ->
- `BridgeCsdbGateway` POSTs `/csdb/v1/ingest` then calls `RomMClient.Tasks.ScanLibraryAsync`
- completion raises `LibraryRefreshRequested` ->
- gateway selection by config (roms root writable -> Local, else Bridge) ->
Scope: layer-1+
**Acceptance Criteria:**
- [ ] `SearchAsync` returns classified hits (demo/crack/sid) ->
- [ ] ingest selection capped at 20 -> `CsdbDiscoveryViewModelTests.Ingest_CapsAt20`.
- [ ] `LocalCsdbGateway` ingests via `CsdbRomMWorkflow` then triggers scan ->
- [ ] `BridgeCsdbGateway` POSTs `/csdb/v1/ingest` then calls `RomMClient.Tasks.ScanLibraryAsync`
- [ ] completion raises `LibraryRefreshRequested` ->
- [ ] gateway selection by config (roms root writable -> Local, else Bridge) ->

## FR-CTX-001 Context machine states + transitions

The context machine has states Gameplay/MainMenu/VirtualKeyboard/ConfirmDialog with defined transitions. Acceptance: a transition-table test drives every (state,input) edge; no undefined transition changes context.

## Acceptance Criteria
- a transition-table test drives every (state,input) edge; no undefined transition changes context.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] a transition-table test drives every (state,input) edge; no undefined transition changes context.

## FR-CTX-002 Non-Gameplay forces ports neutral

Every non-Gameplay context forces both ports neutral; A does not fire in a menu. Acceptance: Resolve in any non-Gameplay context with A held + sticks deflected -> Joy1=Joy2=Neutral, no fire.

## Acceptance Criteria
- 8 directions per port; A->JOY2 fire, B->JOY1 fire; menu neutralization; Guide inert.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] 8 directions per port; A->JOY2 fire, B->JOY1 fire; menu neutralization; Guide inert.

## FR-CTX-003 Non-Gameplay routes UI navigation

In non-Gameplay, D-pad/left-stick -> UI nav, A=UiActivate, B=UiBack. Acceptance: DPadDown -> UiNavigateDown; A -> UiActivate; B -> UiBack; sticks emit no joystick effect.

## Acceptance Criteria
- DPadDown -> UiNavigateDown; A -> UiActivate; B -> UiBack; sticks emit no joystick effect.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] DPadDown -> UiNavigateDown; A -> UiActivate; B -> UiBack; sticks emit no joystick effect.

## FR-CTX-004 One-shot neutralizing push on context entry

Entering any non-Gameplay context emits a one-shot neutralizing push to both ports. Acceptance: Gameplay->MainMenu edge -> Joy1=Joy2 neutral push (an emitted neutral, not a suppressed no-op).

## Acceptance Criteria
- Gameplay->MainMenu edge -> Joy1=Joy2 neutral push (an emitted neutral, not a suppressed no-op).
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Gameplay->MainMenu edge -> Joy1=Joy2 neutral push (an emitted neutral, not a suppressed no-op).

## FR-D71-001 D71 disk image format

D71 (double-sided 1541-format GCR) images: exactly 349,696 bytes, 70 tracks with the D64 zone table applied per side; attachable only to a 1571 drive; implements the shared IGcrDisk surface so the drive mechanism is image-format agnostic.

## Acceptance Criteria
- D71 (double-sided 1541-format GCR) images: exactly 349,696 bytes, 70 tracks with the D64 zone table applied per side; attachable only to a 1571 drive; implements the shared IGcrDisk surface so the drive mechanism is image-format agnostic.
- At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] D71 (double-sided 1541-format GCR) images: exactly 349,696 bytes, 70 tracks with the D64 zone table applied per side; attachable only to a 1571 drive; implements the shared IGcrDisk surface so the drive mechanism is image-format agnostic.
- [ ] At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.

## FR-D81-001 D81 disk image format

D81 (3.5in MFM 800K) images: exactly 819,200 bytes, 80 tracks x 2 sides x 10 physical 512-byte MFM sectors (40 logical 256-byte sectors per track); attachable only to a 1581 drive; exposed to the WD1770/MFM floppy model via IMfmDiskImage.

## Acceptance Criteria
- D81 (3.5in MFM 800K) images: exactly 819,200 bytes, 80 tracks x 2 sides x 10 physical 512-byte MFM sectors (40 logical 256-byte sectors per track); attachable only to a 1581 drive; exposed to the WD1770/MFM floppy model via IMfmDiskImage.
- At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] D81 (3.5in MFM 800K) images: exactly 819,200 bytes, 80 tracks x 2 sides x 10 physical 512-byte MFM sectors (40 logical 256-byte sectors per track); attachable only to a 1581 drive; exposed to the WD1770/MFM floppy model via IMfmDiskImage.
- [ ] At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.

## FR-DEVCARDART-001 Device-card vector art backgrounds

Every peripheral card in the Avalonia sidebar renders model-appropriate vector device art as a dimmed (opacity 0.20), right-anchored, hit-test-invisible background layer behind the card content. Drives select art by drive model (breadbin for 1540/1541/unknown, 1541-II, 1571, 1581, SFD-1001, CMD-HD, CMD-FD); Tape shows the datasette; Cartridge shows a cartridge. Card content, LED, bindings, and automation ids are unchanged.

## Acceptance Criteria
- Every peripheral card in the Avalonia sidebar renders model-appropriate vector device art as a dimmed (opacity 0.20), right-anchored, hit-test-invisible background layer behind the card content.
- Drives select art by drive model (breadbin for 1540/1541/unknown, 1541-II, 1571, 1581, SFD-1001, CMD-HD, CMD-FD); Tape shows the datasette; Cartridge shows a cartridge.
- Card content, LED, bindings, and automation ids are unchanged.
- At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Every peripheral card in the Avalonia sidebar renders model-appropriate vector device art as a dimmed (opacity 0.20), right-anchored, hit-test-invisible background layer behind the card content.
- [ ] Drives select art by drive model (breadbin for 1540/1541/unknown, 1541-II, 1571, 1581, SFD-1001, CMD-HD, CMD-FD); Tape shows the datasette; Cartridge shows a cartridge.
- [ ] Card content, LED, bindings, and automation ids are unchanged.
- [ ] At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.

## FR-DRV-001 Commodore 1541 Single Floppy Disk Drive Emulation

The 1541 floppy disk drive shall be emulated as a complete subsystem with its own 6502 CPU, 2KB RAM, two VIA 6522 chips, and drive ROM. The drive operates independently of the main computer, communicating via the IEC serial bus. True drive emulation (running the drive CPU in parallel with the main CPU) is required for compatibility with fast loaders and copy protection.

## Acceptance Criteria
- The 1541 is emulated with its own 6502 CPU running at 1MHz.
- Drive RAM (2KB at $0000-$07FF) and ROM (16KB at $C000-$FFFF) are correctly mapped.
- Two VIA 6522 chips are emulated (VIA1 at $1800, VIA2 at $1C00) per FR-VIA-005.
- The drive firmware (ROM) executes the standard Commodore DOS 2.6 routines.
- D64 disk images are supported (35 tracks, 683 sectors, 174,848 bytes).
- G64 disk images are supported (raw GCR encoding for copy-protected disks).
- The drive motor spin-up delay (approximately 300ms) is modeled.
- Head stepping delay (approximately 8ms per half-track) is modeled.
- The drive can be attached/detached at runtime via `IDiskDrive.Mount()`/`IDiskDrive.Eject()`.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The 1541 is emulated with its own 6502 CPU running at 1MHz.
- [ ] Drive RAM (2KB at $0000-$07FF) and ROM (16KB at $C000-$FFFF) are correctly mapped.
- [ ] Two VIA 6522 chips are emulated (VIA1 at $1800, VIA2 at $1C00) per FR-VIA-005.
- [ ] The drive firmware (ROM) executes the standard Commodore DOS 2.6 routines.
- [ ] D64 disk images are supported (35 tracks, 683 sectors, 174,848 bytes).
- [ ] G64 disk images are supported (raw GCR encoding for copy-protected disks).
- [ ] The drive motor spin-up delay (approximately 300ms) is modeled.
- [ ] Head stepping delay (approximately 8ms per half-track) is modeled.
- [ ] The drive can be attached/detached at runtime via `IDiskDrive.Mount()`/`IDiskDrive.Eject()`.

## FR-DRV-002 Commodore 1571 Double-Sided Floppy Disk Drive Emulation

The 1571 is a double-sided version of the 1541, used primarily with the C128. It supports 70 tracks (35 per side), MFM encoding for CP/M compatibility, and burst transfer mode for faster data transfer.

## Acceptance Criteria
- The 1571 is emulated with its own 6502 CPU and expanded ROM.
- Double-sided disk support: 70 tracks (35 per side), 1,366 sectors.
- D71 disk images are supported.
- GCR encoding is used for the first side (Commodore-native).
- MFM encoding is available for CP/M mode on both sides.
- Burst transfer mode enables faster serial bus communication.
- The 1571 can operate in 1541-compatibility mode (single-sided, GCR only).
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The 1571 is emulated with its own 6502 CPU and expanded ROM.
- [ ] Double-sided disk support: 70 tracks (35 per side), 1,366 sectors.
- [ ] D71 disk images are supported.
- [ ] GCR encoding is used for the first side (Commodore-native).
- [ ] MFM encoding is available for CP/M mode on both sides.
- [ ] Burst transfer mode enables faster serial bus communication.
- [ ] The 1571 can operate in 1541-compatibility mode (single-sided, GCR only).

## FR-DRV-003 Commodore 1581 3.5" Floppy Disk Drive Emulation

The 1581 is a 3.5-inch disk drive using MFM encoding with 80 tracks, double-sided, providing approximately 800KB of storage. It uses a WD1772 floppy disk controller instead of the VIA-based approach of the 1541/1571.

## Acceptance Criteria
- The 1581 is emulated with its own CPU and WD1772 FDC controller.
- D81 disk images are supported (80 tracks, 2 sides, 10 sectors per track, 256 bytes per sector).
- MFM encoding is used throughout.
- Partitioning and subdirectory support (CMD-style) is available.
- The IEC serial bus interface operates at the higher transfer rates supported by the 1581.
- The drive can be mounted/ejected at runtime.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The 1581 is emulated with its own CPU and WD1772 FDC controller.
- [ ] D81 disk images are supported (80 tracks, 2 sides, 10 sectors per track, 256 bytes per sector).
- [ ] MFM encoding is used throughout.
- [ ] Partitioning and subdirectory support (CMD-style) is available.
- [ ] The IEC serial bus interface operates at the higher transfer rates supported by the 1581.
- [ ] The drive can be mounted/ejected at runtime.

## FR-DRV-004 Group Code Recording Encoding and Decoding

The 1541 and 1571 drives use Group Code Recording (GCR) to encode data on disk. Each 4-bit nybble is encoded as a 5-bit GCR pattern to ensure sufficient flux transitions. The GCR codec handles encoding/decoding of data blocks, header blocks, and inter-sector gaps.

## Acceptance Criteria
- The 4-to-5-bit GCR encoding table matches the Commodore standard (16 valid nybble-to-GCR mappings).
- Data blocks (256 bytes data + checksum) are correctly GCR-encoded to produce 325 GCR bytes on disk.
- Header blocks (8 bytes: magic, checksum, sector, track, ID) are GCR-encoded to 10 GCR bytes.
- Inter-sector gaps (SYNC marks of $FF bytes followed by gap bytes) are correctly generated.
- The 4 speed zones (tracks 1-17: 3.25 speed, 18-24: 3.50, 25-30: 3.75, 31-35: 4.00) determine bits per track.
- Decoding GCR data back to binary is bit-accurate.
- Invalid GCR patterns (not in the standard table) are detectable for copy protection analysis.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The 4-to-5-bit GCR encoding table matches the Commodore standard (16 valid nybble-to-GCR mappings).
- [ ] Data blocks (256 bytes data + checksum) are correctly GCR-encoded to produce 325 GCR bytes on disk.
- [ ] Header blocks (8 bytes: magic, checksum, sector, track, ID) are GCR-encoded to 10 GCR bytes.
- [ ] Inter-sector gaps (SYNC marks of $FF bytes followed by gap bytes) are correctly generated.
- [ ] The 4 speed zones (tracks 1-17: 3.25 speed, 18-24: 3.50, 25-30: 3.75, 31-35: 4.00) determine bits per track.
- [ ] Decoding GCR data back to binary is bit-accurate.
- [ ] Invalid GCR patterns (not in the standard table) are detectable for copy protection analysis.

## FR-DRV-005 IEC Serial Bus Protocol

The IEC (IEEE-488 derived) serial bus connects the C64 to disk drives, printers, and other peripherals. The bus uses three signal lines (ATN, CLK, DATA) in an active-low open-collector configuration. The protocol includes LISTEN, TALK, UNLISTEN, UNTALK, and secondary address commands.

## Acceptance Criteria
- The three-wire serial bus (ATN, CLK, DATA) operates with correct timing.
- The ATN line is controlled by the computer; asserting ATN forces all devices to listen.
- LISTEN (command $20+device) puts a device in receive mode.
- TALK (command $40+device) puts a device in send mode.
- UNLISTEN ($3F) and UNTALK ($5F) release devices.
- Secondary addresses ($60-$6F for OPEN, $E0-$EF for CLOSE, $F0-$FF for channel) are supported.
- Byte transfer uses the standard Commodore serial protocol timing (EOI signaling for last byte).
- Bus turnaround (switching from computer-transmit to device-transmit after TALK) follows correct timing.
- Multiple devices on the bus (up to device #30) are supported simultaneously.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] IEC bus endpoints resolve ATN, CLK, DATA, and SRQ as active-low wired-OR lines.
- [ ] Mounted D64 directory and file operations generate observable IEC line activity from the bus signal source.
- [ ] Mounted D64 directory and file operations complete with correct data through the IEC path.

## FR-DRV-006 Fast Loader Compatibility

Many C64 programs use custom fast loader routines that replace the slow standard IEC protocol with faster custom transfer protocols. The emulator shall support common fast loaders by accurately emulating the drive CPU timing and IEC bus signal transitions.

## Acceptance Criteria
- True drive emulation (drive CPU runs in parallel) ensures fast loader compatibility.
- The timing of IEC bus signal transitions is accurate to within 1 CPU cycle.
- Common fast loaders that must work: EPYX Fast Load, Turbo Disk, SpeedDOS, JiffyDOS protocol, DolphinDOS parallel transfer.
- The drive CPU and main CPU clocks are synchronized (not free-running) to prevent drift.
- Parallel port connection for DolphinDOS and SpeedDOS is emulated when configured.
- Custom transfer protocols that use CIA timer-based handshaking are supported.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] True drive emulation (drive CPU runs in parallel) ensures fast loader compatibility.
- [ ] The timing of IEC bus signal transitions is accurate to within 1 CPU cycle.
- [ ] Common fast loaders that must work: EPYX Fast Load, Turbo Disk, SpeedDOS, JiffyDOS protocol, DolphinDOS parallel transfer.
- [ ] The drive CPU and main CPU clocks are synchronized (not free-running) to prevent drift.
- [ ] Parallel port connection for DolphinDOS and SpeedDOS is emulated when configured.
- [ ] Custom transfer protocols that use CIA timer-based handshaking are supported.

## FR-DRV1540-001 Commodore 1540 true drive

The 1540 (VIC-1540) drive variant: identical 1541 board topology (6502 + 2KB RAM + two 6522 VIAs + 16KB DOS ROM at $C000) with the dos1540-325302+3-01.bin DOS ROM. Acceptance: full cycle-lockstep vs native VICE x64sc with Drive8Type=1540 (boot witness drive-CPU register lockstep at equal drive clocks over 2M host cycles, plus the KERNAL-CLOSE anchored D64 LOAD gate).

## Acceptance Criteria
- The 1540 (VIC-1540) drive variant: identical 1541 board topology (6502 + 2KB RAM + two 6522 VIAs + 16KB DOS ROM at $C000) with the dos1540-325302+3-01.bin DOS ROM.
- Acceptance: full cycle-lockstep vs native VICE x64sc with Drive8Type=1540 (boot witness drive-CPU register lockstep at equal drive clocks over 2M host cycles, plus the KERNAL-CLOSE anchored D64 LOAD gate).
- At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The 1540 (VIC-1540) drive variant: identical 1541 board topology (6502 + 2KB RAM + two 6522 VIAs + 16KB DOS ROM at $C000) with the dos1540-325302+3-01.bin DOS ROM.
- [ ] Acceptance: full cycle-lockstep vs native VICE x64sc with Drive8Type=1540 (boot witness drive-CPU register lockstep at equal drive clocks over 2M host cycles, plus the KERNAL-CLOSE anchored D64 LOAD gate).
- [ ] At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.

## FR-DRV1541II-001 Commodore 1541-II true drive

The 1541-II variant: identical 1541 board topology with the dos1541ii-251968-03.bin DOS ROM (VICE type id 1542). Acceptance: full cycle-lockstep vs native VICE x64sc with Drive8Type=1542 (boot witness + anchored D64 LOAD gate).

## Acceptance Criteria
- The 1541-II variant: identical 1541 board topology with the dos1541ii-251968-03.bin DOS ROM (VICE type id 1542).
- Acceptance: full cycle-lockstep vs native VICE x64sc with Drive8Type=1542 (boot witness + anchored D64 LOAD gate).
- At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The 1541-II variant: identical 1541 board topology with the dos1541ii-251968-03.bin DOS ROM (VICE type id 1542).
- [ ] Acceptance: full cycle-lockstep vs native VICE x64sc with Drive8Type=1542 (boot witness + anchored D64 LOAD gate).
- [ ] At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.

## FR-DRVLED-001 Per-drive activity LED

Each drive card shows an activity LED sourced from that drive's VIA2 (1C00) port B bit 3, set by the 1541 DOS ROM (VICE led_status model), independent of IEC bus traffic.

## Acceptance Criteria
- Each drive card shows an activity LED sourced from that drive's VIA2 (1C00) port B bit 3, set by the 1541 DOS ROM (VICE led_status model), independent of IEC bus traffic.
- At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Each drive card shows an activity LED sourced from that drive's VIA2 (1C00) port B bit 3, set by the 1541 DOS ROM (VICE led_status model), independent of IEC bus traffic.
- [ ] At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.

## FR-DRVMODEL-001 Drive model selection plumbing end-to-end

A DriveModel enum (Abstractions; VICE-canonical values 1540, 1541, 1542=1541-II, 1571, 1581) flows from the protocol (CreateEmulatorSessionRequest.true_drive_model, MediaAttachmentDto.drive_model) through DefaultEmulatorRuntimeFactory and C64TrueDriveRigBuilder to a DriveDescriptorFactory selecting the per-model drive descriptor. Persisted as the VICE-canonical Drive8Type integer in vice.ini via ViceSettings. Every default is 1541; an absent or zero value normalizes to 1541 so behavior is unchanged until a model is selected.

## Acceptance Criteria
- A DriveModel enum (Abstractions; VICE-canonical values 1540, 1541, 1542=1541-II, 1571, 1581) flows from the protocol (CreateEmulatorSessionRequest.true_drive_model, MediaAttachmentDto.drive_model) through DefaultEmulatorRuntimeFactory and C64TrueDriveRigBuilder to a DriveDescriptorFactory selecting the per-model drive descriptor.
- Persisted as the VICE-canonical Drive8Type integer in vice.ini via ViceSettings.
- Every default is 1541; an absent or zero value normalizes to 1541 so behavior is unchanged until a model is selected.
- At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A DriveModel enum (Abstractions; VICE-canonical values 1540, 1541, 1542=1541-II, 1571, 1581) flows from the protocol (CreateEmulatorSessionRequest.true_drive_model, MediaAttachmentDto.drive_model) through DefaultEmulatorRuntimeFactory and C64TrueDriveRigBuilder to a DriveDescriptorFactory selecting the per-model drive descriptor.
- [ ] Persisted as the VICE-canonical Drive8Type integer in vice.ini via ViceSettings.
- [ ] Every default is 1541; an absent or zero value normalizes to 1541 so behavior is unchanged until a model is selected.
- [ ] At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.

## FR-DRVMODEL-002 Drive model selection surface (UI, settings, launcher)

The Avalonia drive card exposes a drive-model selector that drives SetTrueDriveAsync with the model and adjusts attach file filters per model; the launcher accepts -drive8type/-drive9type; the multisystem topology YAML accepts an optional model field on drive peripherals; the selection persists to vice.ini Drive8Type and restores at startup.

## Acceptance Criteria
- The Avalonia drive card exposes a drive-model selector that drives SetTrueDriveAsync with the model and adjusts attach file filters per model; the launcher accepts -drive8type/-drive9type; the multisystem topology YAML accepts an optional model field on drive peripherals; the selection persists to vice.ini Drive8Type and restores at startup.
- At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The Avalonia drive card exposes a drive-model selector that drives SetTrueDriveAsync with the model and adjusts attach file filters per model; the launcher accepts -drive8type/-drive9type; the multisystem topology YAML accepts an optional model field on drive peripherals; the selection persists to vice.ini Drive8Type and restores at startup.
- [ ] At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.

## FR-DRVTRUE-001 Per-drive True Drive toggle

Each IEC drive's UI exposes a True Drive toggle. Enabled = cycle-accurate emulated 1541 (6502+VIA+DOS over IEC); disabled (default) = lightweight simulated/buffered drive. Mirrors VICE per-unit DriveTrueEmulation / Fidelity TrueDevice vs Buffered. The runtime honors it (gated true-drive coordinator path), default off so existing behavior is unchanged.

## Acceptance Criteria
- Each IEC drive's UI exposes a True Drive toggle.
- Enabled = cycle-accurate emulated 1541 (6502+VIA+DOS over IEC); disabled (default) = lightweight simulated/buffered drive.
- Mirrors VICE per-unit DriveTrueEmulation / Fidelity TrueDevice vs Buffered.
- The runtime honors it (gated true-drive coordinator path), default off so existing behavior is unchanged.
- At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Each IEC drive's UI exposes a True Drive toggle.
- [ ] Enabled = cycle-accurate emulated 1541 (6502+VIA+DOS over IEC); disabled (default) = lightweight simulated/buffered drive.
- [ ] Mirrors VICE per-unit DriveTrueEmulation / Fidelity TrueDevice vs Buffered.
- [ ] The runtime honors it (gated true-drive coordinator path), default off so existing behavior is unchanged.
- [ ] At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.

## FR-GAMEPAD-001 Left stick + D-pad drive JOY2 (OR-merged pre-SOCD)

Left stick and D-pad both drive JOY2, OR-merged before SOCD. Acceptance: LeftX past right threshold + DPadUp -> Joy2.DirectionMask == 0x09, JOY1 unaffected.

## Acceptance Criteria
- LeftX past right threshold + DPadUp -> Joy2.DirectionMask == 0x09, JOY1 unaffected.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] LeftX past right threshold + DPadUp -> Joy2.DirectionMask == 0x09, JOY1 unaffected.

## FR-GAMEPAD-002 Right stick drives JOY1 only

Right stick drives JOY1 with no D-pad contribution. Acceptance: RightY past up threshold -> Joy1 Up(0x01); D-pad does not alter Joy1.

## Acceptance Criteria
- RightY past up threshold -> Joy1 Up(0x01); D-pad does not alter Joy1.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] RightY past up threshold -> Joy1 Up(0x01); D-pad does not alter Joy1.

## FR-GAMEPAD-003 A -> JOY2 fire, B -> JOY1 fire

A -> JOY2 fire, B -> JOY1 fire, emitted as the separate bool. Acceptance: Buttons=A -> Joy2.Fire only; Buttons=B -> Joy1.Fire only; no mask change.

## Acceptance Criteria
- Buttons=A -> Joy2.Fire only; Buttons=B -> Joy1.Fire only; no mask change.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Buttons=A -> Joy2.Fire only; Buttons=B -> Joy1.Fire only; no mask change.

## FR-GAMEPAD-004 Radial deadzone + per-axis hysteresis 8-way

Radial deadzone + per-axis Activate/Release hysteresis 8-way. Acceptance: mag 0.20 -> mask 0; 0.60 sets, 0.45 holds, 0.35 clears.

## Acceptance Criteria
- mag 0.20 -> mask 0; 0.60 sets, 0.45 holds, 0.35 clears.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] mag 0.20 -> mask 0; 0.60 sets, 0.45 holds, 0.35 clears.

## FR-GAMEPAD-005 Axis sign mapping to direction bits

Axis sign +Y=Up/-Y=Down/+X=Right/-X=Left. Acceptance: the four signed extremes map to the four bits.

## Acceptance Criteria
- the four signed extremes map to the four bits.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] the four signed extremes map to the four bits.

## FR-GAMEPAD-006 SOCD Neutral clears opposing pairs post-merge

SocdMode.Neutral clears opposing pairs post-merge. Acceptance: stick Left + D-pad Right -> both Left and Right cleared.

## Acceptance Criteria
- stick Left + D-pad Right -> both Left and Right cleared.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] stick Left + D-pad Right -> both Left and Right cleared.

## FR-GAMEPAD-007 Default locked mapping, remappable, port swap

Default profile is the locked mapping; remappable at runtime; swap via XboxInputConfig.SwapPorts. Acceptance: defaults LeftStick->Joystick2, RightStick->Joystick1, Dpad->Joystick2, Joy2Fire=A, Joy1Fire=B; a swapped config routes to the other port.

## Acceptance Criteria
- defaults LeftStick->Joystick2, RightStick->Joystick1, Dpad->Joystick2, Joy2Fire=A, Joy1Fire=B; a swapped config routes to the other port.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] defaults LeftStick->Joystick2, RightStick->Joystick1, Dpad->Joystick2, Joy2Fire=A, Joy1Fire=B; a swapped config routes to the other port.

## FR-GAMEPAD-008 Fail-safe center on disconnect/no-pad/exception

Fail-safe center both ports on disconnect/no-pad/reader-exception; promote next pad. Acceptance: primary disconnect while held -> next Tick emits (Joystick1,0,false)+(Joystick2,0,false), second pad becomes primary.

## Acceptance Criteria
- primary disconnect while held -> next Tick emits (Joystick1,0,false)+(Joystick2,0,false), second pad becomes primary.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] primary disconnect while held -> next Tick emits (Joystick1,0,false)+(Joystick2,0,false), second pad becomes primary.

## FR-GAMEPAD-009 Single XboxInputContext consumes whole snapshot

The whole snapshot is consumed only by the single XboxInputContext (no separate app-button path). Acceptance: no AppButtonChanged event exists; system buttons reach the context, never a joystick port directly.

## Acceptance Criteria
- no `AppButtonChanged` event exists; system buttons reach the context, never a joystick port directly.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] no `AppButtonChanged` event exists; system buttons reach the context, never a joystick port directly.

## FR-HOST-001 Host-Owned Emulator Session Lifecycle

The emulator host shall own machine session creation, lifecycle transitions, and process-level fault reporting. UI clients request state changes through the host boundary instead of instantiating emulator core objects directly.

## Acceptance Criteria
- A host process can create and destroy a C64 machine session without referencing any UI framework.
- The host exposes start, pause, resume, reset, and stop commands for an active session.
- Lifecycle state is reported as idle, running, paused, faulted, or stopped.
- Lifecycle commands are serialized per session so transitions cannot race.
- Each command response includes the session id, command sequence, resulting state, and structured error details when applicable.
- Runtime counters for frame number, cycle count, speed percentage, and elapsed host time are queryable by the UI.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A host process can create and destroy a C64 machine session without referencing any UI framework.
- [ ] The host exposes start, pause, resume, reset, and stop commands for an active session.
- [ ] Lifecycle state is reported as idle, running, paused, faulted, or stopped.
- [ ] Lifecycle commands are serialized per session so transitions cannot race.
- [ ] Each command response includes the session id, command sequence, resulting state, and structured error details when applicable.
- [ ] Runtime counters for frame number, cycle count, speed percentage, and elapsed host time are queryable by the UI.

## FR-HOST-002 Host-Mediated Disk, Tape, and Cartridge Attachment

The host shall expose runtime attachment and ejection of disk, tape, and cartridge media through boundary commands. UI clients provide user intent and file references or byte payloads; the host validates formats and applies the changes at emulator-safe boundaries.

## Acceptance Criteria
- Disk images can be mounted and ejected through the host boundary for the configured drive device.
- TAP images can be mounted and ejected through the host boundary for the datasette device.
- Standard cartridge images can be inserted and removed through the host boundary.
- Media format validation failures are reported without mutating the current device state.
- Attachment commands return stable media status including device id, media kind, display name, write-protection state, and current error state.
- Attach and eject operations are applied at command boundaries that do not tear device state mid-cycle.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Disk images can be mounted and ejected through the host boundary for the configured drive device.
- [ ] TAP images can be mounted and ejected through the host boundary for the datasette device.
- [ ] Standard cartridge images can be inserted and removed through the host boundary.
- [ ] Media format validation failures are reported without mutating the current device state.
- [ ] Attachment commands return stable media status including device id, media kind, display name, write-protection state, and current error state.
- [ ] Attach and eject operations are applied at command boundaries that do not tear device state mid-cycle.

## FR-HOST-003 Host-Streamed Video Frames for Remote UI Clients

The host shall stream committed video frames to external or remote UI clients without allowing UI rendering to block the emulation thread. In-process Avalonia rendering may instead use a host-owned direct frame-source binding as described by FR-UI-001 and TR-GRPC-BOUNDARY-001.

## Acceptance Criteria
- Video frames are streamed with frame number, cycle stamp, video standard, dimensions, pixel format, and skipped-frame metadata.
- Video backpressure uses a latest-complete-frame policy; stale frames may be dropped without changing emulation state.
- A newly connected remote UI client can request the latest committed frame before subscribing to the live stream.
- Disconnecting a remote UI client does not pause, reset, or mutate the emulator session.
- Frame payloads use a documented pixel format so non-Avalonia clients can render them.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Video frames are streamed with frame number, cycle stamp, video standard, dimensions, pixel format, and skipped-frame metadata.
- [ ] Video backpressure uses a latest-complete-frame policy; stale frames may be dropped without changing emulation state.
- [ ] A newly connected remote UI client can request the latest committed frame before subscribing to the live stream.
- [ ] Disconnecting a remote UI client does not pause, reset, or mutate the emulator session.
- [ ] Frame payloads use a documented pixel format so non-Avalonia clients can render them.

## FR-HOST-004 Host-Normalized Keyboard, Joystick, and Machine Control

The host shall accept normalized input events and machine control commands from UI clients and inject or apply them at deterministic boundaries.

## Acceptance Criteria
- Keyboard key-down, key-up, RESTORE, and SHIFT LOCK events can be submitted through the host boundary.
- Joystick port 1 and port 2 direction/fire state can be submitted through the host boundary.
- Start, pause, resume, reset, and shutdown controls are submitted through the host boundary.
- Input events preserve client order through a monotonically increasing sequence number.
- Unknown mappings, invalid port numbers, and stale sequence numbers are rejected with structured errors.
- The same normalized input event stream can be recorded for deterministic replay.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Keyboard key-down, key-up, RESTORE, and SHIFT LOCK events can be submitted through the host boundary.
- [ ] Joystick port 1 and port 2 direction/fire state can be submitted through the host boundary.
- [ ] Start, pause, resume, reset, and shutdown controls are submitted through the host boundary.
- [ ] Input events preserve client order through a monotonically increasing sequence number.
- [ ] Unknown mappings, invalid port numbers, and stale sequence numbers are rejected with structured errors.
- [ ] The same normalized input event stream can be recorded for deterministic replay.

## FR-HOST-005 Host-Owned Snapshot, Screenshot, and Diagnostic Operations

The host shall expose state save/load, screenshot capture, and basic diagnostic commands without granting UI clients direct access to emulator internals.

## Acceptance Criteria
- Snapshot save requests produce a versioned artifact or artifact handle with checksum metadata.
- Snapshot load requests validate the artifact before replacing the active machine state.
- Screenshot requests capture the latest committed frame with dimensions, pixel format, and palette metadata.
- State and capture commands coordinate with the emulation loop so snapshots and screenshots are not torn.
- Long-running state operations report progress and can be cancelled before commit.
- Diagnostic status includes active machine profile, lifecycle state, attached media, recent structured errors, and current frame/cycle counters.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Snapshot save requests produce a versioned artifact or artifact handle with checksum metadata.
- [ ] Snapshot load requests validate the artifact before replacing the active machine state.
- [ ] Screenshot requests capture the latest committed frame with dimensions, pixel format, and palette metadata.
- [ ] State and capture commands coordinate with the emulation loop so snapshots and screenshots are not torn.
- [ ] Long-running state operations report progress and can be cancelled before commit.
- [ ] Diagnostic status includes active machine profile, lifecycle state, attached media, recent structured errors, and current frame/cycle counters.

## FR-HOST-006 Host Runtime Status and Control Telemetry

The host shall expose runtime telemetry and machine-control state needed by emulator shells without requiring UI clients to read emulator internals directly.

## Acceptance Criteria
- Host status reports power state, run state, limiter target, measured frames per second, frame count, cycle count, program counter, nominal clock, effective clock Hz, and effective clock percent.
- Effective clock speed is measured from emulated cycles per real second and remains distinct from the requested limiter target.
- Pause, resume, step one cycle, step one frame, cold reset, and warm reset commands are exposed through the host boundary.
- Rewind controls return explicit unsupported status until backing host history support exists.
- Reset-plus-drive-8 autorun returns `Ok` for supported drive 8 D64/PRG launches and explicit failed-precondition status when backing host or media prerequisites are missing.
- Telemetry responses are safe for polling by UI clients and do not mutate emulator state.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Host status responses include existing runtime fields including session, run state, cycle, frame, model, limiter, and automation status.
- [ ] Host status responses include IEC activity derived from emulator bus traffic and safe for UI polling.
- [ ] Status polling does not mutate emulator, drive, or bus state.

## FR-HOST-DIAG-001 Self-Describing Host Diagnostics Attach

The host shall expose a read-only diagnostics surface that allows humans and external tools to discover the running host, active UI session, session list, and current performance state without scanning ports, reading source code, using UI automation, or creating throwaway probe sessions.

## Acceptance Criteria
- A local diagnostics client can discover host process metadata, endpoint, protocol package, and version.
- A local diagnostics client can list live emulator sessions without mutating the registry or creating a new session.
- A local diagnostics client can identify the session currently bound to the UI.
- A local diagnostics client can request a performance snapshot with an explicit session id or omit the session id to use the current UI session.
- The desktop app publishes deterministic debug attach metadata under `%LOCALAPPDATA%\ViceSharp\debug-attach.json` and removes it best-effort on clean shutdown.
- The desktop UI exposes a Copy Debug Attach Info command containing endpoint, active session, app version, and current status.
- The diagnostics surface remains read-only and loopback/development scoped.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A local diagnostics client can discover host process metadata, endpoint, protocol package, and version.
- [ ] A local diagnostics client can list live emulator sessions without mutating the registry or creating a new session.
- [ ] A local diagnostics client can identify the session currently bound to the UI.
- [ ] A local diagnostics client can request a performance snapshot with an explicit session id or omit the session id to use the current UI session.
- [ ] The desktop app publishes deterministic debug attach metadata under `%LOCALAPPDATA%\ViceSharp\debug-attach.json` and removes it best-effort on clean shutdown.
- [ ] The desktop UI exposes a Copy Debug Attach Info command containing endpoint, active session, app version, and current status.
- [ ] The diagnostics surface remains read-only and loopback/development scoped.

## FR-IECHOTPLUG-001 Hot drive add and remove and live device renumber

Drives can be turned on and off and have their device number changed at runtime without restarting the emulator.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A drive attached to a running session answers on the bus with no restart.
- [ ] A detached drive's pull contributions are removed and line states recompute.
- [ ] A drive's device number (8 to 11) can be changed at runtime and it answers the new number.

## FR-IECLOAD-001 True-drive 1541 LOAD over IEC

A single-system C64 with a true-drive 1541 attached completes LOAD"*",8,1, LOAD"$",8 and SAVE over the IEC bus, talking to the drive's DOS ROM via the faithful serial electrical model.

## Acceptance Criteria
- A single-system C64 with a true-drive 1541 attached completes LOAD"*",8,1, LOAD"$",8 and SAVE over the IEC bus, talking to the drive's DOS ROM via the faithful serial electrical model.
- At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A single-system C64 with a true-drive 1541 attached completes LOAD"*",8,1, LOAD"$",8 and SAVE over the IEC bus, talking to the drive's DOS ROM via the faithful serial electrical model.
- [ ] At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.

## FR-IECMON-001 IEC bus monitor (scope view)

Dedicated logic-analyzer panel showing a timing diagram of the IEC lines over emulator time, colored by which device drives each segment, with decoded IEC protocol bands, cursor/zoom/scroll, synced to forward step and reverse step.

## Acceptance Criteria
- Dedicated logic-analyzer panel showing a timing diagram of the IEC lines over emulator time, colored by which device drives each segment, with decoded IEC protocol bands, cursor/zoom/scroll, synced to forward step and reverse step.
- At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Dedicated logic-analyzer panel showing a timing diagram of the IEC lines over emulator time, colored by which device drives each segment, with decoded IEC protocol bands, cursor/zoom/scroll, synced to forward step and reverse step.
- [ ] At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.

## FR-IECSPY-001 IEC bus snapshot / spy

At any instant the IEC bus can be snapshotted to read each line's level (ATN/CLK/DATA/SRQ), which endpoints are pulling each line low, and which devices are talking. Read-only; never perturbs bus state. DONE.

## Acceptance Criteria
- At any instant the IEC bus can be snapshotted to read each line's level (ATN/CLK/DATA/SRQ), which endpoints are pulling each line low, and which devices are talking.
- Read-only; never perturbs bus state.
- At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] At any instant the IEC bus can be snapshotted to read each line's level (ATN/CLK/DATA/SRQ), which endpoints are pulling each line low, and which devices are talking.
- [ ] Read-only; never perturbs bus state.
- [ ] At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.

## FR-INP-001 Keyboard Matrix Emulation

The C64 keyboard is an 8x8 matrix scanned through CIA1 Port A (column select, active-low output) and Port B (row read, active-low input). The emulator shall map host keyboard events to the C64 matrix positions, support positional and symbolic mapping modes, and handle simultaneous key presses including ghosting behavior.

## Acceptance Criteria
- All 64 key positions in the C64 matrix are mappable from host keyboard events.
- Positional mapping mode: host keys map to the C64 key at the same physical position.
- Symbolic mapping mode: host keys map to the C64 key that produces the same character.
- Multiple simultaneous key presses are supported (up to the limits of the host keyboard).
- Key ghosting in the matrix is modeled: pressing 3 keys that form an L-shape in the matrix causes a phantom 4th key to appear pressed.
- The RESTORE key is handled separately (it triggers NMI, not a matrix position).
- The SHIFT LOCK key toggles and holds the left SHIFT matrix position.
- The `IKeyboardMatrix` interface accepts key-down and key-up events with C64 matrix coordinates.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] All 64 key positions in the C64 matrix are mappable from host keyboard events.
- [ ] Positional mapping mode: host keys map to the C64 key at the same physical position.
- [ ] Symbolic mapping mode: host keys map to the C64 key that produces the same character.
- [ ] Multiple simultaneous key presses are supported (up to the limits of the host keyboard).
- [ ] Key ghosting in the matrix is modeled: pressing 3 keys that form an L-shape in the matrix causes a phantom 4th key to appear pressed.
- [ ] The RESTORE key is handled separately (it triggers NMI, not a matrix position).
- [ ] The SHIFT LOCK key toggles and holds the left SHIFT matrix position.
- [ ] The `IKeyboardMatrix` interface accepts key-down and key-up events with C64 matrix coordinates.

## FR-INP-002 Joystick Port 1 and Port 2 Emulation

The C64 has two DE-9 joystick ports. Each port reads 5 digital signals: up, down, left, right, and fire. The emulator shall support mapping host input devices (keyboard keys, gamepads, analog sticks) to joystick port signals.

## Acceptance Criteria
- Each joystick port reports 5 independent digital signals (up, down, left, right, fire).
- Host keyboard keys can be mapped to joystick directions and fire.
- Host gamepad/controller input can be mapped to joystick signals via `IJoystickPort.SetState()`.
- Analog stick input from host controllers is converted to digital signals with a configurable dead zone.
- Joystick ports can be swapped at runtime (port 1 <-> port 2).
- Autofire functionality is available with configurable rate (in frames between toggles).
- Both ports can be active simultaneously with independent mappings.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Each joystick port reports 5 independent digital signals (up, down, left, right, fire).
- [ ] Host keyboard keys can be mapped to joystick directions and fire.
- [ ] Host gamepad/controller input can be mapped to joystick signals via `IJoystickPort.SetState()`.
- [ ] Analog stick input from host controllers is converted to digital signals with a configurable dead zone.
- [ ] Joystick ports can be swapped at runtime (port 1 <-> port 2).
- [ ] Autofire functionality is available with configurable rate (in frames between toggles).
- [ ] Both ports can be active simultaneously with independent mappings.

## FR-INP-003 Commodore 1351 Proportional Mouse Emulation

The 1351 mouse uses the SID's potentiometer inputs (POT X, POT Y) to report proportional (analog) position data. The mouse connects to the joystick port and uses the analog pot lines for X/Y movement and the digital lines for buttons.

## Acceptance Criteria
- Mouse X movement is reported via the POTX register ($D419 for port 1, $D41A for port 2).
- Mouse Y movement is reported via the POTY register.
- The pot registers report the proportional position as a value that wraps at 0-255.
- Left mouse button is mapped to the fire button (joystick bit 4).
- Right mouse button is mapped to a secondary control line (typically UP direction on joystick port).
- Host mouse movement is scaled and mapped to the 1351 proportional output.
- The `IMousePort` interface accepts delta-X and delta-Y from the host pointing device.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Mouse X movement is reported via the POTX register ($D419 for port 1, $D41A for port 2).
- [ ] Mouse Y movement is reported via the POTY register.
- [ ] The pot registers report the proportional position as a value that wraps at 0-255.
- [ ] Left mouse button is mapped to the fire button (joystick bit 4).
- [ ] Right mouse button is mapped to a secondary control line (typically UP direction on joystick port).
- [ ] Host mouse movement is scaled and mapped to the 1351 proportional output.
- [ ] The `IMousePort` interface accepts delta-X and delta-Y from the host pointing device.

## FR-INP-004 Lightpen Input

The VIC-II supports a lightpen input on the LP pin (directly on the joystick port). When triggered, the VIC-II latches the current raster position into the lightpen X ($D013) and Y ($D014) registers.

## Acceptance Criteria
- A falling edge on the lightpen input latches the current X and Y raster positions.
- The X position ($D013) reports the horizontal position in double-pixel units (0-163).
- The Y position ($D014) reports the raster line number (0-255).
- The lightpen latch triggers once per frame (subsequent triggers within the same frame are ignored).
- A lightpen interrupt can be generated (if enabled in $D01A).
- Host mouse position can be translated to lightpen position relative to the visible display area.
- The `ILightpenPort` interface accepts screen coordinates and triggers the latch.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A falling edge on the lightpen input latches the current X and Y raster positions.
- [ ] The X position ($D013) reports the horizontal position in double-pixel units (0-163).
- [ ] The Y position ($D014) reports the raster line number (0-255).
- [ ] The lightpen latch triggers once per frame (subsequent triggers within the same frame are ignored).
- [ ] A lightpen interrupt can be generated (if enabled in $D01A).
- [ ] Host mouse position can be translated to lightpen position relative to the visible display area.
- [ ] The `ILightpenPort` interface accepts screen coordinates and triggers the latch.

## FR-INP-005 Paddle Controller Input

Paddle controllers connect to the joystick port and use the SID's potentiometer inputs for analog position reading. Each joystick port supports two paddles (sharing the POTX and POTY lines). The fire buttons use the joystick digital lines.

## Acceptance Criteria
- Paddle X (first paddle) is read via POTX; Paddle Y (second paddle) via POTY.
- The pot value ranges from 0 to 255 based on the paddle position.
- Paddle button 1 maps to the fire button (joystick bit 4).
- Paddle button 2 maps to an additional digital line (typically bit 2, LEFT).
- The SID pot scanning timing (512 cycles per measurement) is accurately modeled.
- Two paddles per port (four paddles total) can be active simultaneously.
- The `IPaddlePort` interface accepts analog position values (0.0-1.0) for each paddle.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Paddle X (first paddle) is read via POTX; Paddle Y (second paddle) via POTY.
- [ ] The pot value ranges from 0 to 255 based on the paddle position.
- [ ] Paddle button 1 maps to the fire button (joystick bit 4).
- [ ] Paddle button 2 maps to an additional digital line (typically bit 2, LEFT).
- [ ] The SID pot scanning timing (512 cycles per measurement) is accurately modeled.
- [ ] Two paddles per port (four paddles total) can be active simultaneously.
- [ ] The `IPaddlePort` interface accepts analog position values (0.0-1.0) for each paddle.

## FR-INP-006 VICE VKM Keymap Selection and Real-Time Keyboard Translation

The emulator shall use VICE keymap files to translate host keyboard events into machine-specific keyboard matrix state. Built-in maps and uploaded custom maps are selected per emulator session and applied by the host-owned machine keyboard input handler.

## Acceptance Criteria
- Built-in VICE keymaps are discoverable by machine and map style.
- A selected keymap is retained per emulator session until changed or the session ends.
- Custom keymaps can be uploaded by content and metadata without requiring shared file paths between UI and host.
- The parser supports VICE keymap comments, `!CLEAR`, `!INCLUDE`, `!UNDEF`, modifier directives, row/column entries, and shift flags needed for SHIFT, C=, and CTRL behavior.
- Includes resolve relative to the current keymap file or uploaded bundle context.
- Invalid custom maps report diagnostics and do not replace the active map.
- Real-time key down/up events update the C64 keyboard matrix through the selected map without constructing UI-local emulator devices.
- Machine-specific keyboard translators can be registered for future non-C64 profiles.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Built-in VICE keymaps are discoverable by machine and map style.
- [ ] A selected keymap is retained per emulator session until changed or the session ends.
- [ ] Custom keymaps can be uploaded by content and metadata without requiring shared file paths between UI and host.
- [ ] The parser supports VICE keymap comments, `!CLEAR`, `!INCLUDE`, `!UNDEF`, modifier directives, row/column entries, and shift flags needed for SHIFT, C=, and CTRL behavior.
- [ ] Includes resolve relative to the current keymap file or uploaded bundle context.
- [ ] Invalid custom maps report diagnostics and do not replace the active map.
- [ ] Real-time key down/up events update the C64 keyboard matrix through the selected map without constructing UI-local emulator devices.
- [ ] Machine-specific keyboard translators can be registered for future non-C64 profiles.

## FR-INPROC-001 Kestrel-free in-process host path

The console host runs the core in-process with no Kestrel/ASP.NET on the emulation/render/input path. Acceptance: ConsoleHostComposition.Build works with no WebApplication/Kestrel; ViceSharp.Host.InProcess has no Microsoft.AspNetCore.* reference.

## Acceptance Criteria
- ConsoleHostComposition.Build works with no WebApplication/Kestrel; ViceSharp.Host.InProcess has no Microsoft.AspNetCore.* reference.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] ConsoleHostComposition.Build works with no WebApplication/Kestrel; ViceSharp.Host.InProcess has no Microsoft.AspNetCore.* reference.

## FR-INPROC-002 IConsoleEmulatorHost surface

IConsoleEmulatorHost exposes session lifecycle + lock-free frame pull + FrameGeometry + joystick/key/RESTORE injection. Acceptance: a headless test exercises start->advance->frame-pull->input->reset->close; TryGetFrameGeometry.BufferLength == FrameBuffer.Length.

## Acceptance Criteria
- a headless test exercises start->advance->frame-pull->input->reset->close; TryGetFrameGeometry.BufferLength == FrameBuffer.Length.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] a headless test exercises start->advance->frame-pull->input->reset->close; TryGetFrameGeometry.BufferLength == FrameBuffer.Length.

## FR-INPROC-003 Explicit swap-immune joystick injection

Joystick injection uses explicit Joystick1/Joystick2 (swap-immune). Acceptance: SetJoystick(Joystick2, Up|fire) -> SetJoystickState(2,0x01,true); toggling SwapJoystickPorts does not change the control port.

## Acceptance Criteria
- SetJoystick(Joystick2, Up|fire) -> SetJoystickState(2,0x01,true); toggling SwapJoystickPorts does not change the control port.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] SetJoystick(Joystick2, Up|fire) -> SetJoystickState(2,0x01,true); toggling SwapJoystickPorts does not change the control port.

## FR-INSTALL-001 MSI registers supported Commodore file types

The ViceSharp MSI shall register per-machine Open verbs for supported Commodore media extensions so Explorer can open them with ViceSharp.Avalonia.exe "%1". Registered extensions: d64, g64, d71, d81, tap, t64, crt, prg. Generic .bin/.rom are not claimed as defaults. Double-clicking a registered file starts ViceSharp and drop-starts the path (attach+autostart or PRG load).

Acceptance:
1. installer/ViceSharp.wxs writes HKLM Software\Classes for each registered extension and ProgId shell\open\command with [INSTALLFOLDER]ViceSharp.Avalonia.exe and %1.
2. Capabilities FileAssociations and RegisteredApplications entries exist for Default Programs.
3. Startup argument parsing picks the first existing supported path from args (skipping the exe and dash flags).
4. MainWindow opens that path via DropAndStartFileAsync after the window is shown.

## Acceptance Criteria
- The ViceSharp MSI shall register per-machine Open verbs for supported Commodore media extensions so Explorer can open them with ViceSharp.Avalonia.exe "%1".
- Registered extensions: d64, g64, d71, d81, tap, t64, crt, prg.
- Generic .bin/.rom are not claimed as defaults.
- Double-clicking a registered file starts ViceSharp and drop-starts the path (attach+autostart or PRG load).
- installer/ViceSharp.wxs writes HKLM Software\Classes for each registered extension and ProgId shell\open\command with [INSTALLFOLDER]ViceSharp.Avalonia.exe and %1.
- Capabilities FileAssociations and RegisteredApplications entries exist for Default Programs.
- At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The ViceSharp MSI shall register per-machine Open verbs for supported Commodore media extensions so Explorer can open them with ViceSharp.Avalonia.exe "%1".
- [ ] Registered extensions: d64, g64, d71, d81, tap, t64, crt, prg.
- [ ] Generic .bin/.rom are not claimed as defaults.
- [ ] Double-clicking a registered file starts ViceSharp and drop-starts the path (attach+autostart or PRG load).
- [ ] installer/ViceSharp.wxs writes HKLM Software\Classes for each registered extension and ProgId shell\open\command with [INSTALLFOLDER]ViceSharp.Avalonia.exe and %1.
- [ ] Capabilities FileAssociations and RegisteredApplications entries exist for Default Programs.
- [ ] At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.

## FR-MACHINEPICKER-001 Settings Computer and Model dropdowns

Avalonia Settings splits machine selection into two ComboBoxes matching Xbox SettingsPage. Computer is the family (Commodore 64 / VIC-20). Model is the variant list for that family from the host profile catalog (C64 PAL/NTSC/C64C/... or VIC-20 PAL/NTSC). Changing Computer publishes that family's Models and selects the family default profile. Changing Model sets the host profile id and flags Apply+Restart.

## Acceptance Criteria
- **ac-two-combos**: Settings exposes Computer and Model ComboBoxes with AutomationIds Settings.Computer and Settings.MachineVariant.
- **ac-models-filter**: Selecting Commodore 64 lists only x64sc models; selecting VIC-20 lists only xvic models. Minimal host is excluded.
- **ac-restart**: Changing Computer or Model flags restart and Apply+Restart switches the live machine family/variant.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Settings exposes Computer and Model ComboBoxes with AutomationIds Settings.Computer and Settings.MachineVariant.
- [ ] Selecting Commodore 64 lists only x64sc models; selecting VIC-20 lists only xvic models. Minimal host is excluded.
- [ ] Changing Computer or Model flags restart and Apply+Restart switches the live machine family/variant.

## FR-MCP-TRIAGE-002 FR-MCP-TRIAGE-002

Placeholder requirement backfilled for TODO link FR-MCP-TRIAGE-002.
Scope: layer-1+

## FR-MED-001 Screenshot Capture (PNG/BMP)

The emulator shall capture the current video frame as a still image in PNG or BMP format. Screenshots capture the full VIC-II output including borders (optionally cropped to the display area only). The capture is triggered programmatically or by user action.

## Acceptance Criteria
- PNG format output with correct color representation (RGB, 8-bit per channel).
- BMP format output as an uncompressed bitmap.
- Full frame capture includes the border area (standard PAL: 403x284 visible pixels; standard NTSC: 411x234).
- Cropped capture includes only the main display area (320x200 pixels, or 160x200 in multicolor mode upscaled to 320x200).
- The screenshot captures the frame as it appears after all VIC-II rendering (sprites, priority, collision) is complete.
- Screenshots are accessible via `IMediaCapture.CaptureScreenshot()` which returns the image data or writes to a file path.
- Integer scaling options (1x, 2x, 3x, 4x) are available.
- Palette selection (VICE default, Pepto, CCS64, Community Colors) is configurable.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] PNG format output with correct color representation (RGB, 8-bit per channel).
- [ ] BMP format output as an uncompressed bitmap.
- [ ] Full frame capture includes the border area (standard PAL: 403x284 visible pixels; standard NTSC: 411x234).
- [ ] Cropped capture includes only the main display area (320x200 pixels, or 160x200 in multicolor mode upscaled to 320x200).
- [ ] The screenshot captures the frame as it appears after all VIC-II rendering (sprites, priority, collision) is complete.
- [ ] Screenshots are accessible via `IMediaCapture.CaptureScreenshot()` which returns the image data or writes to a file path.
- [ ] Integer scaling options (1x, 2x, 3x, 4x) are available.
- [ ] Palette selection (VICE default, Pepto, CCS64, Community Colors) is configurable.

## FR-MED-002 Video Recording (MP4 via FFmpeg)

The emulator shall record video output to MP4 (H.264) format by streaming raw frames to an external `ffmpeg` process (mirroring VICE's `ffmpegexedrv`; discovery via `PATH` or `VICESHARP_FFMPEG`, no libav P/Invoke bindings). Recording captures consecutive frames at the native frame rate (PAL: 50fps, NTSC: approximately 59.94fps) and encodes them in real-time.

## Acceptance Criteria
- Video is encoded to H.264 in an MP4 container.
- Frame rate matches the emulated system (50fps PAL, 59.94fps NTSC).
- Resolution options include native (403x284 PAL) and integer-scaled variants.
- Recording can be started, paused, resumed, and stopped via `IMediaCapture`.
- The encoding does not drop frames during real-time recording on the reference hardware (a modern desktop CPU).
- Quality presets (low/medium/high) are configurable, mapping to CRF values.
- The recording pipeline uses zero managed allocations per frame on the hot path.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Unique mode skips consecutive byte-identical frames
- [x] Frame files are written off the emulation worker thread

## FR-MED-003 Audio Recording (WAV/FLAC)

The emulator shall record audio output to WAV (uncompressed PCM) or FLAC (lossless compressed) format. The recording captures the SID audio output at the configured sample rate.

## Acceptance Criteria
- WAV format: 16-bit PCM, mono or stereo, at configurable sample rates (44100, 48000, 96000 Hz).
- FLAC format: lossless compression of the same PCM data.
- Recording captures the mixed SID output including all voices, filter, and volume.
- For dual-SID configurations, stereo recording captures left/right channels independently.
- Recording can be started, paused, resumed, and stopped independently of video recording.
- Audio samples are buffered to prevent gaps during recording.
- The recording includes digi playback ($D418) output at full fidelity.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Output parses as valid RIFF/WAVE with data-chunk size matching samples

## FR-MED-004 Synchronized Audio/Video Capture

The emulator shall support recording audio and video simultaneously with correct synchronization. The audio and video streams are muxed into a single container file (MP4) with timestamps that maintain A/V sync throughout the recording.

## Acceptance Criteria
- Audio and video streams are written to a single MP4 container with correct timestamps.
- A/V sync drift does not exceed 1 frame duration (20ms PAL, 16.7ms NTSC) over any recording length.
- The muxer correctly interleaves audio and video packets.
- Starting A/V recording captures both streams from the same frame/sample boundary.
- Pausing and resuming A/V recording maintains sync after the gap.
- The `IMuxer` interface handles timestamp generation and stream interleaving.
- Audio sample count per video frame is correctly calculated to avoid drift (PAL: 882.17 samples/frame at 44100Hz).
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Audio and video streams are written to a single MP4 container with correct timestamps.
- [ ] A/V sync drift does not exceed 1 frame duration (20ms PAL, 16.7ms NTSC) over any recording length.
- [ ] The muxer correctly interleaves audio and video packets.
- [ ] Starting A/V recording captures both streams from the same frame/sample boundary.
- [ ] Pausing and resuming A/V recording maintains sync after the gap.
- [ ] The `IMuxer` interface handles timestamp generation and stream interleaving.
- [ ] Audio sample count per video frame is correctly calculated to avoid drift (PAL: 882.17 samples/frame at 44100Hz).

## FR-MED-005 Output Format Selection and Configuration

The media capture system shall support multiple output formats with configurable encoding parameters. Format selection is exposed through the `IMediaCapture` interface.

## Acceptance Criteria
- Video formats: MP4 (H.264), AVI (uncompressed or MJPEG), GIF (animated, for short clips).
- Audio formats: WAV (PCM 16-bit), FLAC (lossless), MP3 (lossy, for smaller files).
- Image formats: PNG (lossless), BMP (uncompressed), JPEG (lossy).
- Each format exposes its available quality/compression parameters.
- Format availability is reported by `IMediaCapture.GetSupportedFormats()`.
- Unavailable formats (e.g., if the external `ffmpeg` executable is not present) are gracefully reported as unsupported without crashing.
- The default format for each capture type is configurable via `IMediaCapture.SetDefaultFormat()`.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Video formats: MP4 (H.264), AVI (uncompressed or MJPEG), GIF (animated, for short clips).
- [ ] Audio formats: WAV (PCM 16-bit), FLAC (lossless), MP3 (lossy, for smaller files).
- [ ] Image formats: PNG (lossless), BMP (uncompressed), JPEG (lossy).
- [ ] Each format exposes its available quality/compression parameters.
- [ ] Format availability is reported by `IMediaCapture.GetSupportedFormats()`.
- [ ] Unavailable formats (e.g., if the external `ffmpeg` executable is not present) are gracefully reported as unsupported without crashing.
- [ ] The default format for each capture type is configurable via `IMediaCapture.SetDefaultFormat()`.

## FR-MEM-001 Address Decoding and PLA Banking Configuration

The memory subsystem shall implement the C64 PLA (Programmable Logic Array) address decoding, which maps the 64KB address space to RAM, ROM, and I/O regions based on the 6510 I/O port bits (LORAM, HIRAM, CHAREN) and the cartridge GAME/EXROM lines. All 32 possible PLA configurations shall be supported.

## Acceptance Criteria
- The address decoder consults the 5 input bits (LORAM, HIRAM, CHAREN, GAME, EXROM) to select from all 32 banking configurations.
- Each configuration correctly maps the following regions: $0000-$0FFF (RAM), $1000-$7FFF (RAM), $8000-$9FFF (RAM/ROML), $A000-$BFFF (RAM/BASIC/ROMH), $C000-$CFFF (RAM), $D000-$DFFF (RAM/CHARROM/IO), $E000-$FFFF (RAM/KERNAL/ROMH).
- Banking changes take effect on the cycle following the write to $0001.
- The `IBankController` publishes a `BankConfigChanged` event when the active configuration changes.
- The address decoder is stateless relative to its inputs -- the same 5-bit input always produces the same mapping.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The address decoder consults the 5 input bits (LORAM, HIRAM, CHAREN, GAME, EXROM) to select from all 32 banking configurations.
- [ ] Each configuration correctly maps the following regions: $0000-$0FFF (RAM), $1000-$7FFF (RAM), $8000-$9FFF (RAM/ROML), $A000-$BFFF (RAM/BASIC/ROMH), $C000-$CFFF (RAM), $D000-$DFFF (RAM/CHARROM/IO), $E000-$FFFF (RAM/KERNAL/ROMH).
- [ ] Banking changes take effect on the cycle following the write to $0001.
- [ ] The `IBankController` publishes a `BankConfigChanged` event when the active configuration changes.
- [ ] The address decoder is stateless relative to its inputs -- the same 5-bit input always produces the same mapping.

## FR-MEM-002 RAM Under ROM Access

The CPU shall be able to write to RAM underlying any ROM region, and read from RAM underlying ROM via appropriate banking configuration. When BASIC ROM is mapped at $A000-$BFFF, writes go to the underlying RAM. When a cartridge ROM is mapped, writes to that region go to underlying RAM (not the cartridge ROM).

## Acceptance Criteria
- Writes to addresses $A000-$BFFF always store to underlying RAM regardless of whether BASIC ROM is banked in.
- Writes to addresses $E000-$FFFF always store to underlying RAM regardless of whether KERNAL ROM is banked in.
- Writes to addresses $D000-$DFFF when Character ROM is banked in store to underlying RAM.
- Reading from a RAM-under-ROM address when ROM is banked out returns the previously written RAM value.
- The VIC-II always reads from the "VIC view" of memory (it sees character ROM at $1000-$1FFF and $9000-$9FFF in the VIC address space, not RAM).
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Writes to addresses $A000-$BFFF always store to underlying RAM regardless of whether BASIC ROM is banked in.
- [ ] Writes to addresses $E000-$FFFF always store to underlying RAM regardless of whether KERNAL ROM is banked in.
- [ ] Writes to addresses $D000-$DFFF when Character ROM is banked in store to underlying RAM.
- [ ] Reading from a RAM-under-ROM address when ROM is banked out returns the previously written RAM value.
- [ ] The VIC-II always reads from the "VIC view" of memory (it sees character ROM at $1000-$1FFF and $9000-$9FFF in the VIC address space, not RAM).

## FR-MEM-003 Ultimax Cartridge Mode

When the GAME line is asserted (active) and EXROM is deasserted (active), the system enters Ultimax mode. In this mode, only 4KB of RAM at $0000-$0FFF is visible to the CPU, the cartridge ROML is at $8000-$9FFF, and ROMH is at $E000-$FFFF. I/O area at $D000-$DFFF remains accessible. All other address ranges read open bus (the last value on the data bus).

## Acceptance Criteria
- With GAME=1, EXROM=0 the address space is configured for Ultimax mode.
- RAM is only accessible at $0000-$0FFF.
- ROML from the cartridge is mapped at $8000-$9FFF.
- ROMH from the cartridge is mapped at $E000-$FFFF.
- I/O space is mapped at $D000-$DFFF.
- Reads from unmapped regions ($1000-$7FFF, $A000-$CFFF) return open-bus values.
- The VIC-II can access cartridge ROM in Ultimax mode.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] With GAME=1, EXROM=0 the address space is configured for Ultimax mode.
- [ ] RAM is only accessible at $0000-$0FFF.
- [ ] ROML from the cartridge is mapped at $8000-$9FFF.
- [ ] ROMH from the cartridge is mapped at $E000-$FFFF.
- [ ] I/O space is mapped at $D000-$DFFF.
- [ ] Reads from unmapped regions ($1000-$7FFF, $A000-$CFFF) return open-bus values.
- [ ] The VIC-II can access cartridge ROM in Ultimax mode.

## FR-MEM-004 VIC-II Bank Switching via CIA2

The VIC-II chip views a 16KB window of the 64KB address space, selectable by bits 0-1 of CIA2 Port A ($DD00). The four possible banks are: Bank 0 ($0000-$3FFF), Bank 1 ($4000-$7FFF), Bank 2 ($8000-$BFFF), Bank 3 ($C000-$FFFF). The VIC-II's view of Character ROM overlaps in banks 0 and 2.

## Acceptance Criteria
- CIA2 Port A bits 0-1 (active-low, inverted) select the VIC bank: %11 = Bank 0, %10 = Bank 1, %01 = Bank 2, %00 = Bank 3.
- VIC-II addresses are relative to the selected bank start.
- In Banks 0 and 2, addresses $1000-$1FFF within the bank read from Character ROM instead of RAM.
- Bank switching takes effect immediately for subsequent VIC-II accesses.
- The `IVicBankSelector` interface reports the currently active bank and base address.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] CIA2 Port A bits 0-1 (active-low, inverted) select the VIC bank: %11 = Bank 0, %10 = Bank 1, %01 = Bank 2, %00 = Bank 3.
- [ ] VIC-II addresses are relative to the selected bank start.
- [ ] In Banks 0 and 2, addresses $1000-$1FFF within the bank read from Character ROM instead of RAM.
- [ ] Bank switching takes effect immediately for subsequent VIC-II accesses.
- [ ] The `IVicBankSelector` interface reports the currently active bank and base address.

## FR-MEM-005 Color RAM ($D800-$DBFF)

The 1K nybble-wide Color RAM at $D800-$DBFF shall be emulated. Color RAM is a separate 4-bit-wide SRAM that is always visible in the I/O region and is not affected by banking configuration. Only the lower 4 bits of each byte are significant; the upper 4 bits read as open bus.

## Acceptance Criteria
- Color RAM is accessible at $D800-$DBFF regardless of banking configuration.
- Only the low nybble (bits 0-3) is stored and returned on reads.
- The upper nybble reads from the open bus (typically the value last placed on the data bus).
- Color RAM is preserved across banking changes.
- Both the CPU and VIC-II can access Color RAM (VIC-II reads it during character fetch).
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Color RAM is accessible at $D800-$DBFF regardless of banking configuration.
- [ ] Only the low nybble (bits 0-3) is stored and returned on reads.
- [ ] The upper nybble reads from the open bus (typically the value last placed on the data bus).
- [ ] Color RAM is preserved across banking changes.
- [ ] Both the CPU and VIC-II can access Color RAM (VIC-II reads it during character fetch).

## FR-MEM-006 Zero Page and Stack Page Behavior

The zero page ($0000-$00FF) and stack page ($0100-$01FF) shall behave as fast-access RAM with the 6510 I/O port overlay at $0000-$0001. Stack operations (PHA, PLA, JSR, RTS, BRK, RTI) shall correctly wrap the stack pointer within the $0100-$01FF page.

## Acceptance Criteria
- Addresses $0002-$00FF behave as standard RAM (zero page).
- Addresses $0000-$0001 are intercepted by the 6510 I/O port (per FR-CPU-004).
- Stack operations always access $0100 + SP; the SP wraps within 8 bits (no access outside $0100-$01FF).
- JSR pushes PC+2 (high byte first, then low byte).
- RTS pulls low byte first, then high byte, and adds 1 to the result.
- Stack underflow/overflow wraps without generating an exception.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Addresses $0002-$00FF behave as standard RAM (zero page).
- [ ] Addresses $0000-$0001 are intercepted by the 6510 I/O port (per FR-CPU-004).
- [ ] Stack operations always access $0100 + SP; the SP wraps within 8 bits (no access outside $0100-$01FF).
- [ ] JSR pushes PC+2 (high byte first, then low byte).
- [ ] RTS pulls low byte first, then high byte, and adds 1 to the result.
- [ ] Stack underflow/overflow wraps without generating an exception.

## FR-MON-001 Real-Time Disassembly View

The monitor shall provide a disassembly view that decodes 6502/6510 machine code into human-readable assembly mnemonics. The disassembler shall support all official and undocumented opcodes, display correct addressing mode syntax, and handle code that overlaps data regions.

## Acceptance Criteria
- All 256 opcodes (including undocumented) are disassembled with correct mnemonics and operands.
- All addressing modes display with standard syntax (e.g., `LDA #$FF`, `STA $D020`, `JMP ($FFFE)`, `LDA ($FB),Y`).
- The disassembly view can be positioned at any address and scrolls forward/backward.
- Backward disassembly uses heuristic alignment (scanning back N bytes and disassembling forward to find the correct instruction boundary at the target address).
- Undocumented opcodes display with their commonly used mnemonics (LAX, SAX, DCP, ISC, etc.) and are visually distinguished from official opcodes.
- The `IDisassembler` interface accepts an address and returns structured disassembly data (address, bytes, mnemonic, operand, size).
- Labels and symbols can be optionally applied to addresses.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] All 256 opcodes (including undocumented) are disassembled with correct mnemonics and operands.
- [ ] All addressing modes display with standard syntax (e.g., `LDA #$FF`, `STA $D020`, `JMP ($FFFE)`, `LDA ($FB),Y`).
- [ ] The disassembly view can be positioned at any address and scrolls forward/backward.
- [ ] Backward disassembly uses heuristic alignment (scanning back N bytes and disassembling forward to find the correct instruction boundary at the target address).
- [ ] Undocumented opcodes display with their commonly used mnemonics (LAX, SAX, DCP, ISC, etc.) and are visually distinguished from official opcodes.
- [ ] The `IDisassembler` interface accepts an address and returns structured disassembly data (address, bytes, mnemonic, operand, size).
- [ ] Labels and symbols can be optionally applied to addresses.

## FR-MON-002 Memory Hex and ASCII Display

The monitor shall display memory contents in a combined hex/ASCII view, showing 16 bytes per row with the address, hexadecimal byte values, and the printable ASCII representation of each byte.

## Acceptance Criteria
- Each row displays: a 4-digit hex address, 16 hex byte values (space-separated, with a wider gap at the 8-byte midpoint), and the 16-character ASCII representation.
- Non-printable characters ($00-$1F, $80-$FF) display as a dot (`.`) in the ASCII column.
- The PETSCII character set is optionally available for the text column.
- The display can be scrolled to any address ($0000-$FFFF).
- Memory contents reflect the current banking configuration (the same address can show different values depending on which bank is selected).
- Modified bytes (since last refresh) can be visually highlighted.
- The display updates in real-time when the emulation is paused and memory is edited.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Each row displays: a 4-digit hex address, 16 hex byte values (space-separated, with a wider gap at the 8-byte midpoint), and the 16-character ASCII representation.
- [ ] Non-printable characters ($00-$1F, $80-$FF) display as a dot (`.`) in the ASCII column.
- [ ] The PETSCII character set is optionally available for the text column.
- [ ] The display can be scrolled to any address ($0000-$FFFF).
- [ ] Memory contents reflect the current banking configuration (the same address can show different values depending on which bank is selected).
- [ ] Modified bytes (since last refresh) can be visually highlighted.
- [ ] The display updates in real-time when the emulation is paused and memory is edited.

## FR-MON-003 Breakpoint Management

The monitor shall support execution breakpoints, read/write watchpoints, and conditional breakpoints. When a breakpoint is hit, the emulation pauses and the monitor displays the current CPU state.

## Acceptance Criteria
- Execution breakpoints: pause emulation when the PC reaches the specified address.
- Read watchpoints: pause emulation when a specified address is read.
- Write watchpoints: pause emulation when a specified address is written (optionally filtering by value).
- Conditional breakpoints: expression-based conditions (e.g., `A == $FF`, `X > $10`, `MEM[$D020] == $01`).
- Breakpoints can be enabled, disabled, and removed individually.
- A maximum of at least 64 simultaneous breakpoints is supported without performance degradation.
- Temporary breakpoints (auto-removed after first hit) are supported for step-over and run-to-cursor.
- The `IBreakpointManager` interface provides CRUD operations and hit-count tracking for each breakpoint.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Execution breakpoints: pause emulation when the PC reaches the specified address.
- [ ] Read watchpoints: pause emulation when a specified address is read.
- [ ] Write watchpoints: pause emulation when a specified address is written (optionally filtering by value).
- [ ] Conditional breakpoints: expression-based conditions (e.g., `A == $FF`, `X > $10`, `MEM[$D020] == $01`).
- [ ] Breakpoints can be enabled, disabled, and removed individually.
- [ ] A maximum of at least 64 simultaneous breakpoints is supported without performance degradation.
- [ ] Temporary breakpoints (auto-removed after first hit) are supported for step-over and run-to-cursor.
- [ ] The `IBreakpointManager` interface provides CRUD operations and hit-count tracking for each breakpoint.

## FR-MON-004 CPU Register Inspection and Manipulation

The monitor shall display and allow modification of all CPU registers (A, X, Y, SP, PC) and the processor status flags (N, V, -, B, D, I, Z, C). Register changes take effect on the next instruction execution.

## Acceptance Criteria
- All registers are displayed: A (accumulator), X, Y (index registers), SP (stack pointer), PC (program counter).
- All status flags are individually displayed and modifiable: N (negative), V (overflow), B (break), D (decimal), I (interrupt disable), Z (zero), C (carry).
- Registers can be set to arbitrary values via the monitor interface.
- Setting the PC to a new address causes execution to continue from that address.
- Setting SP adjusts the effective stack pointer (addresses $0100+SP).
- Register display includes both hexadecimal and decimal/binary representations.
- The `ICpu` interface exposes `GetRegisters()` and `SetRegisters()` for programmatic access.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] All registers are displayed: A (accumulator), X, Y (index registers), SP (stack pointer), PC (program counter).
- [ ] All status flags are individually displayed and modifiable: N (negative), V (overflow), B (break), D (decimal), I (interrupt disable), Z (zero), C (carry).
- [ ] Registers can be set to arbitrary values via the monitor interface.
- [ ] Setting the PC to a new address causes execution to continue from that address.
- [ ] Setting SP adjusts the effective stack pointer (addresses $0100+SP).
- [ ] Register display includes both hexadecimal and decimal/binary representations.
- [ ] The `ICpu` interface exposes `GetRegisters()` and `SetRegisters()` for programmatic access.

## FR-MON-005 Memory Bank View Selection

The monitor shall allow the user to select which memory "bank" or view is displayed: CPU view (as the CPU sees it with current PLA configuration), RAM view (raw RAM regardless of ROM overlay), ROM view (character/BASIC/KERNAL ROM), I/O view, and individual bank configurations.

## Acceptance Criteria
- CPU view: shows memory as the CPU currently sees it (respecting PLA banking).
- RAM view: shows raw RAM contents at all addresses, ignoring ROM overlays.
- ROM view: shows the ROM contents at their mapped addresses ($A000 BASIC, $D000 CHARROM, $E000 KERNAL).
- I/O view: shows the I/O register space at $D000-$DFFF.
- Drive view: shows memory from the attached disk drive's address space.
- Bank selection is independent of the actual PLA banking state.
- Editing memory in a specific bank view writes to that specific target (e.g., editing RAM view writes to RAM even if ROM is currently banked in).
Scope: layer-1+
**Acceptance Criteria:**
- [ ] CPU view: shows memory as the CPU currently sees it (respecting PLA banking).
- [ ] RAM view: shows raw RAM contents at all addresses, ignoring ROM overlays.
- [ ] ROM view: shows the ROM contents at their mapped addresses ($A000 BASIC, $D000 CHARROM, $E000 KERNAL).
- [ ] I/O view: shows the I/O register space at $D000-$DFFF.
- [ ] Drive view: shows memory from the attached disk drive's address space.
- [ ] Bank selection is independent of the actual PLA banking state.
- [ ] Editing memory in a specific bank view writes to that specific target (e.g., editing RAM view writes to RAM even if ROM is currently banked in).

## FR-MON-006 Watch Expressions

The monitor shall support watch expressions that continuously evaluate and display values from memory, registers, or computed expressions. Watch values update whenever the emulation state changes (single-step, breakpoint hit, or periodic refresh).

## Acceptance Criteria
- Watch expressions can reference memory addresses: `[$D020]`, `[$0400:$0427]` (range).
- Watch expressions can reference registers: `A`, `X`, `Y`, `SP`, `PC`.
- Watch expressions support arithmetic: `[$FB] + [$FC] * 256` (constructs a 16-bit pointer).
- Watch values are displayed in configurable formats: hex, decimal, binary, ASCII/PETSCII.
- Array watches display a contiguous range of bytes.
- Watches can be added, removed, and reordered.
- A watch that reads a side-effect register (like SID OSC3 at $D41B) is marked as potentially disruptive.
- The watch list persists across monitor open/close within a session.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Watch expressions can reference memory addresses: `[$D020]`, `[$0400:$0427]` (range).
- [ ] Watch expressions can reference registers: `A`, `X`, `Y`, `SP`, `PC`.
- [ ] Watch expressions support arithmetic: `[$FB] + [$FC] * 256` (constructs a 16-bit pointer).
- [ ] Watch values are displayed in configurable formats: hex, decimal, binary, ASCII/PETSCII.
- [ ] Array watches display a contiguous range of bytes.
- [ ] Watches can be added, removed, and reordered.
- [ ] A watch that reads a side-effect register (like SID OSC3 at $D41B) is marked as potentially disruptive.
- [ ] The watch list persists across monitor open/close within a session.

## FR-NATIVE-LIFECYCLE-001 Bounded and isolated native oracle lifecycle

Native VIC-20 sound-oracle reset preserves the live mixer timing and clocks.
Create and step finish or fail within 5000 ms; stop finishes or fails within 2000 ms.
Dispose is idempotent; a live worker never observes freed state. Failed stop poisons shared native-library state and all later operations fail immediately.
Reset, destroy, snapshot read and snapshot write apply the same shutdown-failure policy.
Normal audio, dual-VIA, reset, snapshot and dispose work repeatedly in one process for PAL and NTSC.

## Acceptance Criteria
- **ac-01**: Native VIC-20 sound-oracle reset preserves the live mixer timing and clocks.
- **ac-02**: Create and step finish or fail within 5000 ms; stop finishes or fails within 2000 ms.
- **ac-03**: Dispose is idempotent; a live worker never observes freed state. Failed stop poisons shared native-library state and all later operations fail immediately.
- **ac-04**: Reset, destroy, snapshot read and snapshot write apply the same shutdown-failure policy.
- **ac-05**: Normal audio, dual-VIA, reset, snapshot and dispose work repeatedly in one process for PAL and NTSC.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Native VIC-20 sound-oracle reset preserves the live mixer timing and clocks.
- [ ] Create and step finish or fail within 5000 ms; stop finishes or fails within 2000 ms.
- [ ] Dispose is idempotent; a live worker never observes freed state. Failed stop poisons shared native-library state and all later operations fail immediately.
- [ ] Reset, destroy, snapshot read and snapshot write apply the same shutdown-failure policy.
- [ ] Normal audio, dual-VIA, reset, snapshot and dispose work repeatedly in one process for PAL and NTSC.

## FR-NATIVERESIDUE-001 Order-independent native oracle across .vsf resumes

Every native (x64sc shim) machine created in a test process must present identical boot and post-activity-reset state regardless of whether a .vsf snapshot was resumed earlier in the same process. Snapshot side effects on process-wide VICE resources (e.g. DriveNTrueEmulation from a has_tde=0 DRIVE8 module) must be re-baselined to VICE defaults at machine create so lockstep suites are order-independent and can run in one process.

## Acceptance Criteria
- Every native (x64sc shim) machine created in a test process must present identical boot and post-activity-reset state regardless of whether a .vsf snapshot was resumed earlier in the same process.
- Snapshot side effects on process-wide VICE resources (e.g. DriveNTrueEmulation from a has_tde=0 DRIVE8 module) must be re-baselined to VICE defaults at machine create so lockstep suites are order-independent and can run in one process.
- At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Every native (x64sc shim) machine created in a test process must present identical boot and post-activity-reset state regardless of whether a .vsf snapshot was resumed earlier in the same process.
- [ ] Snapshot side effects on process-wide VICE resources (e.g. DriveNTrueEmulation from a has_tde=0 DRIVE8 module) must be re-baselined to VICE defaults at machine create so lockstep suites are order-independent and can run in one process.
- [ ] At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.

## FR-PACESEL-001 Selectable emulation pacing strategy

The pacing strategy (Semaphore vs VICE) is selectable in settings, applied live by swapping the gate on the worker thread, and persisted.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Strategy is selectable in the Settings UI (Semaphore or VICE)
- [x] Change applies live - the pump swaps the gate with no session restart
- [x] Selection round-trips through the settings DTO and persists
- [x] Unknown or null strategy defaults to Semaphore

## FR-PERF-RUNFRAME-001 C64 PAL RunFrame Throughput

Managed C64 PAL emulation must execute `IMachine.RunFrame()` fast enough for a host application to sustain 50.125 Hz PAL playback with remaining budget for blit and audio work.

## Acceptance Criteria
- A production C64 PAL machine built through `ArchitectureBuilder` with real C64 ROMs is the measured target; romless and minimal-host machines are not valid evidence.
- After 60 warmup frames, a 600-frame Release/net10.0 managed-only run reports median frame time `<= 18.0 ms`.
- The same run reports p95 frame time `<= 22.0 ms`.
- The measured `RunFrame()` loop reports `0` bytes allocated on the current thread.
- Public signatures for `IMachine`, `IVideoChip`, `IAudioChip`, `IBus`, `IKeyboardMatrix`, `ArchitectureBuilder`, and `C64MachineProfiles` remain unchanged.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Production C64 PAL machine is built through ArchitectureBuilder with real C64 ROMs; romless and minimal-host machines are not valid evidence. (evidence: tests/ViceSharp.Benchmarks/BenchmarkMachineFactory.cs; tests/ViceSharp.Benchmarks/C64PalRunFrameBenchmark.cs; BenchmarksSmokeTests.C64PalRunFrameBenchmark_UsesRealC64Pal passed)
- [x] Release/net10.0 managed-only 60 warmup plus 600 measured frame run reports median <= 18.0 ms. (evidence: RunFramePerfProbe 60 600: median=1.575ms)
- [x] The same measured run reports p95 <= 22.0 ms. (evidence: RunFramePerfProbe 60 600: p95=2.753ms)
- [x] The measured RunFrame loop reports 0 bytes allocated on the current thread. (evidence: RunFramePerfProbe 60 600: allocated=0 bytes; BenchmarkDotNet Allocated reported no managed allocation)
- [x] Public signatures for IMachine, IVideoChip, IAudioChip, IBus, IKeyboardMatrix, ArchitectureBuilder, and C64MachineProfiles remain unchanged. (evidence: PR #3 diff only changes internal implementation plus benchmark/test/docs; no public interface signatures changed)

## FR-PRF-001 Commodore 64 (Original NMOS) Machine Profile

The primary machine profile for the original Commodore 64 with NMOS chips: MOS 6510 CPU, MOS 6567 (NTSC) or MOS 6569 (PAL) VIC-II, MOS 6581 SID, two MOS 6526 CIAs. This is the baseline profile and the first target for full compatibility.

## Acceptance Criteria
- CPU: MOS 6510 with all undocumented opcodes exhibiting NMOS behavior.
- VIC-II: MOS 6569R3 (PAL) or MOS 6567R8 (NTSC) with correct timing per variant.
- SID: MOS 6581 with characteristic analog filter curve and combined waveform behavior.
- CIA: Two MOS 6526 with correct timer and TOD behavior.
- RAM: 64KB main RAM + 1KB Color RAM.
- ROM: BASIC V2 ($A000), KERNAL ($E000), Character Generator ($D000).
- System clock: 985248 Hz (PAL) or 1022727 Hz (NTSC).
- The profile is selectable via `IMachineProfile.LoadProfile("c64")`.
- PAL/NTSC variant is configurable within the C64 profile.
- Passes the VICE test suite for x64sc accuracy level.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] CPU: MOS 6510 with all undocumented opcodes exhibiting NMOS behavior.
- [ ] VIC-II: MOS 6569R3 (PAL) or MOS 6567R8 (NTSC) with correct timing per variant.
- [ ] SID: MOS 6581 with characteristic analog filter curve and combined waveform behavior.
- [ ] CIA: Two MOS 6526 with correct timer and TOD behavior.
- [ ] RAM: 64KB main RAM + 1KB Color RAM.
- [ ] ROM: BASIC V2 ($A000), KERNAL ($E000), Character Generator ($D000).
- [ ] System clock: 985248 Hz (PAL) or 1022727 Hz (NTSC).
- [ ] The profile is selectable via `IMachineProfile.LoadProfile("c64")`.
- [ ] PAL/NTSC variant is configurable within the C64 profile.
- [ ] Passes the VICE test suite for x64sc accuracy level.

## FR-PRF-002 Commodore 64C Machine Profile

The C64C (cost-reduced revision) uses the MOS 8580 SID (which has a different filter and combined waveform behavior) and a revised VIC-II (MOS 8562/8565). The C64C profile configures the emulator with these variant chips.

## Acceptance Criteria
- SID: MOS 8580 with linear filter curve and revised combined waveform tables.
- VIC-II: MOS 8562 (NTSC) or MOS 8565 (PAL) -- functionally equivalent to earlier revisions but with different luminance levels.
- All other components are identical to the C64 NMOS profile.
- The difference in SID filter behavior is audible and matches real hardware comparisons.
- Selectable via `IMachineProfile.LoadProfile("c64c")`.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] SID: MOS 8580 with linear filter curve and revised combined waveform tables.
- [ ] VIC-II: MOS 8562 (NTSC) or MOS 8565 (PAL) -- functionally equivalent to earlier revisions but with different luminance levels.
- [ ] All other components are identical to the C64 NMOS profile.
- [ ] The difference in SID filter behavior is audible and matches real hardware comparisons.
- [ ] Selectable via `IMachineProfile.LoadProfile("c64c")`.

## FR-PRF-003 Commodore SX-64 Machine Profile

The SX-64 is a portable C64 with a built-in 1541 disk drive and a 5" color monitor. It lacks a cassette port and has a different KERNAL ROM (no tape routines, different screen colors on startup).

## Acceptance Criteria
- KERNAL: SX-64 variant KERNAL ROM with different default colors (blue screen instead of light blue).
- No cassette port: tape-related I/O port bits behave differently (cassette sense always reads "no tape").
- Built-in 1541 drive: device 8 is always present.
- The 6510 I/O port bit 4 (cassette sense) is permanently pulled high.
- All other hardware is identical to the C64 NMOS profile.
- Selectable via `IMachineProfile.LoadProfile("sx64")`.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] KERNAL: SX-64 variant KERNAL ROM with different default colors (blue screen instead of light blue).
- [ ] No cassette port: tape-related I/O port bits behave differently (cassette sense always reads "no tape").
- [ ] Built-in 1541 drive: device 8 is always present.
- [ ] The 6510 I/O port bit 4 (cassette sense) is permanently pulled high.
- [ ] All other hardware is identical to the C64 NMOS profile.
- [ ] Selectable via `IMachineProfile.LoadProfile("sx64")`.

## FR-PRF-004 Commodore 128 Machine Profile

The Commodore 128 features dual CPUs (MOS 8502 and Zilog Z80), 128KB RAM, an 80-column VDC display chip, and C64 compatibility mode. The C128 profile is a major expansion of the emulation capabilities.

## Acceptance Criteria
- CPU: MOS 8502 (enhanced 6510 with 2MHz mode per FR-CPU-005) and Z80 processor.
- VIC-II: MOS 8564 (NTSC) or MOS 8566 (PAL) for the 40-column display.
- VDC: MOS 8563 for the 80-column display (independent video output).
- RAM: 128KB main RAM, 16KB or 64KB VDC RAM.
- ROM: BASIC V7.0, C128 KERNAL, C64 mode KERNAL, Character Generator.
- MMU (Memory Management Unit) at $D500 controls the expanded banking.
- C64 compatibility mode (selectable at startup or via GO64 command).
- The 2MHz mode is functional per FR-CPU-005.
- Selectable via `IMachineProfile.LoadProfile("c128")`.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] CPU: MOS 8502 (enhanced 6510 with 2MHz mode per FR-CPU-005) and Z80 processor.
- [ ] VIC-II: MOS 8564 (NTSC) or MOS 8566 (PAL) for the 40-column display.
- [ ] VDC: MOS 8563 for the 80-column display (independent video output).
- [ ] RAM: 128KB main RAM, 16KB or 64KB VDC RAM.
- [ ] ROM: BASIC V7.0, C128 KERNAL, C64 mode KERNAL, Character Generator.
- [ ] MMU (Memory Management Unit) at $D500 controls the expanded banking.
- [ ] C64 compatibility mode (selectable at startup or via GO64 command).
- [ ] The 2MHz mode is functional per FR-CPU-005.
- [ ] Selectable via `IMachineProfile.LoadProfile("c128")`.

## FR-PRF-005 Commodore VIC-20 Machine Profile

The VIC-20 uses the MOS 6502 CPU, MOS 6560 (NTSC) or MOS 6561 (PAL) VIC chip (not VIC-II), two MOS 6522 VIA chips, and 5KB of RAM (expandable). The display and sound capabilities differ significantly from the C64. Default floppy on unit 8 is the Commodore 1540 (`DriveModel.C1540`), not the 1541. See also `FR-VIC20.md`.

## Acceptance Criteria

- CPU: Standard MOS 6502 (not 6510; no built-in I/O port).

- VIC chip: MOS 6560/6561 with 22-column text display and 4-voice sound.

- VIA: Two MOS 6522 VIAs (per FR-VIA-004 / FR-VIC20-003) instead of CIAs.

- RAM: 5KB base (1KB at $0000, 4KB at $1000) expandable to 32KB+ with cartridge RAM.

- ROM: BASIC V2, VIC-20 KERNAL, Character Generator (VICE VIC20 data names).

- Memory map differs significantly from C64.

- Sound uses the VIC chip's 4 voices (3 square wave + 1 noise) instead of SID.

- Selectable via profile aliases such as `vic20`, `vic20pal`, `vic20ntsc` (family `xvic`).

- Memory expansion configurations (3KB, 8KB, 16KB, 24KB, 32KB) are selectable.

- Default drive unit 8 model is 1540 (FR-VIC20-006).
Scope: layer-1+
**Acceptance Criteria:**
- [ ] CPU: Standard MOS 6502 (not 6510; no built-in I/O port).
- [ ] VIC chip: MOS 6560/6561 with 22-column text display and 4-voice sound.
- [ ] VIA: Two MOS 6522 VIAs (per FR-VIA-004 / FR-VIC20-003) instead of CIAs.
- [ ] RAM: 5KB base (1KB at $0000, 4KB at $1000) expandable to 32KB+ with cartridge RAM.
- [ ] ROM: BASIC V2, VIC-20 KERNAL, Character Generator (VICE VIC20 data names).
- [ ] Memory map differs significantly from C64.
- [ ] Sound uses the VIC chip's 4 voices (3 square wave + 1 noise) instead of SID.
- [ ] Selectable via profile aliases such as `vic20`, `vic20pal`, `vic20ntsc` (family `xvic`).
- [ ] Memory expansion configurations (3KB, 8KB, 16KB, 24KB, 32KB) are selectable.
- [ ] Default drive unit 8 model is 1540 (FR-VIC20-006).

## FR-PRF-006 Commodore PET Machine Profile

The Commodore PET series uses a MOS 6502 CPU, MOS 6520 PIA or MOS 6522 VIA chips, and a monochrome 40x25 or 80x25 character display. Multiple PET models (2001, 3032, 4032, 8032, 8296) have different configurations.

## Acceptance Criteria
- CPU: Standard MOS 6502.
- Display: CRTC (6545) controlled monochrome character display (40 or 80 columns).
- I/O: MOS 6520 PIA and/or MOS 6522 VIA depending on model.
- RAM: 8KB, 16KB, or 32KB depending on model.
- ROM: PET BASIC (V1, V2, or V4), PET KERNAL, Character Generator.
- IEEE-488 parallel bus for disk drive communication (not IEC serial).
- Built-in cassette drive (PET 2001).
- Selectable via `IMachineProfile.LoadProfile("pet")` with model sub-selection.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] CPU: Standard MOS 6502.
- [ ] Display: CRTC (6545) controlled monochrome character display (40 or 80 columns).
- [ ] I/O: MOS 6520 PIA and/or MOS 6522 VIA depending on model.
- [ ] RAM: 8KB, 16KB, or 32KB depending on model.
- [ ] ROM: PET BASIC (V1, V2, or V4), PET KERNAL, Character Generator.
- [ ] IEEE-488 parallel bus for disk drive communication (not IEC serial).
- [ ] Built-in cassette drive (PET 2001).
- [ ] Selectable via `IMachineProfile.LoadProfile("pet")` with model sub-selection.

## FR-PRF-007 Commodore Plus/4 Machine Profile

The Plus/4 uses the MOS 7501/8501 CPU, TED chip (handling both video and sound), and 64KB RAM. The TED chip replaces both the VIC-II and SID with a simpler combined audio/video chip.

## Acceptance Criteria
- CPU: MOS 7501 or 8501 (6502-compatible with built-in I/O port).
- TED: MOS 7360/8360 providing 40x25 text and 320x200/160x200 graphics, plus 2 square-wave sound channels.
- RAM: 64KB main RAM.
- ROM: BASIC V3.5 (with built-in machine monitor, renumber, etc.), Plus/4 KERNAL, 3-plus-1 software ROM.
- No SID chip; sound is generated by the TED.
- No VIC-II; video is generated by the TED with 121-color palette.
- Selectable via `IMachineProfile.LoadProfile("plus4")`.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] CPU: MOS 7501 or 8501 (6502-compatible with built-in I/O port).
- [ ] TED: MOS 7360/8360 providing 40x25 text and 320x200/160x200 graphics, plus 2 square-wave sound channels.
- [ ] RAM: 64KB main RAM.
- [ ] ROM: BASIC V3.5 (with built-in machine monitor, renumber, etc.), Plus/4 KERNAL, 3-plus-1 software ROM.
- [ ] No SID chip; sound is generated by the TED.
- [ ] No VIC-II; video is generated by the TED with 121-color palette.
- [ ] Selectable via `IMachineProfile.LoadProfile("plus4")`.

## FR-PRF-008 Commodore C16 Machine Profile

The C16 is essentially a cut-down Plus/4 with 16KB RAM and no built-in software ROMs. It uses the same TED chip and is software-compatible with the Plus/4 within its memory limits.

## Acceptance Criteria
- Identical to Plus/4 profile except: 16KB RAM instead of 64KB.
- No 3-plus-1 software ROMs.
- The TED chip and CPU are identical to the Plus/4.
- Programs that fit within 16KB of RAM run identically to the Plus/4.
- Memory access above 16KB wraps or mirrors as on real hardware.
- Selectable via `IMachineProfile.LoadProfile("c16")`.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Identical to Plus/4 profile except: 16KB RAM instead of 64KB.
- [ ] No 3-plus-1 software ROMs.
- [ ] The TED chip and CPU are identical to the Plus/4.
- [ ] Programs that fit within 16KB of RAM run identically to the Plus/4.
- [ ] Memory access above 16KB wraps or mirrors as on real hardware.
- [ ] Selectable via `IMachineProfile.LoadProfile("c16")`.

## FR-PUBSUB-001 Internal Pub/Sub Event Bus

ViceSharp shall provide an internal synchronous topic-based Pub/Sub event bus for transient intra-frame device-to-device communication, including interrupts, NMI, bus availability, address-enable control, DMA, clock, and state notifications. The bus exposes typed publish and subscribe APIs, raw payload compatibility, deterministic registration-order delivery, handle-based unsubscription, frame reset behavior, and message pool integration.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Public IPubSub exposes typed Publish/Subscribe, raw payload compatibility, Unsubscribe by SubscriptionHandle, Flush, FrameReset, and SubscriptionCount. (evidence: src/ViceSharp.Abstractions/IPubSub.cs)
- [x] Publish delivers synchronously to subscribers in registration order for each topic. (evidence: tests/ViceSharp.TestHarness/LockFreePubSubTests.cs)
- [x] Message pool exhaustion, return, and frame reset behavior are covered by focused tests. (evidence: tests/ViceSharp.TestHarness/LockFreePubSubTests.cs)

## FR-REMOTECTRL-001 Live Avalonia visual-tree inspection over gRPC

ViceSharp.Avalonia can expose its live Avalonia visual tree for remote inspection and (optionally) interaction over gRPC via the SharpNinja.Avalonia.RemoteControl embeddable server, to support UI development/validation. The server is disabled by default and only starts when explicitly enabled via environment switches, and then only with a bearer token on a loopback transport (interaction and live frames remain deny-by-default opt-ins).

## Acceptance Criteria
- **ac-01**: RemoteControl is disabled when VICESHARP_REMOTECONTROL_ENABLE is unset; enabled startup without a bearer token fails closed.
- **ac-02**: Explicitly enabled, authenticated loopback gRPC exposes the current live MainWindow visual tree.
- **ac-03**: Interaction and live-frame access remain denied unless their separate opt-ins permit them; authentication and action failures are explicit.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] RemoteControl is disabled when VICESHARP_REMOTECONTROL_ENABLE is unset; enabled startup without a bearer token fails closed.
- [ ] Explicitly enabled, authenticated loopback gRPC exposes the current live MainWindow visual tree.
- [ ] Interaction and live-frame access remain denied unless their separate opt-ins permit them; authentication and action failures are explicit.

## FR-REMOTECTRL-CLI-001 Portable RemoteControl gRPC command line

CLI restores and builds from a standalone ViceSharp checkout without a sibling repository.
Support grpc transport and its environment default; reject unsupported transports explicitly.
Provide machine-readable capabilities and tree results suitable for retained validation evidence.
Resolve exact automation IDs first; reject ambiguous matches, malformed trees and ancestor-hidden controls.
Authentication, disabled control, denied actions, failed mutations, cancellation, timeout and invalid frames produce accurate failures and exit codes.

## Acceptance Criteria
- **ac-01**: CLI restores and builds from a standalone ViceSharp checkout without a sibling repository.
- **ac-02**: Support grpc transport and its environment default; reject unsupported transports explicitly.
- **ac-03**: Provide machine-readable capabilities and tree results suitable for retained validation evidence.
- **ac-04**: Resolve exact automation IDs first; reject ambiguous matches, malformed trees and ancestor-hidden controls.
- **ac-05**: Authentication, disabled control, denied actions, failed mutations, cancellation, timeout and invalid frames produce accurate failures and exit codes.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] CLI restores and builds from a standalone ViceSharp checkout without a sibling repository.
- [ ] Support grpc transport and its environment default; reject unsupported transports explicitly.
- [ ] Provide machine-readable capabilities and tree results suitable for retained validation evidence.
- [ ] Resolve exact automation IDs first; reject ambiguous matches, malformed trees and ancestor-hidden controls.
- [ ] Authentication, disabled control, denied actions, failed mutations, cancellation, timeout and invalid frames produce accurate failures and exit codes.

## FR-REVEXEC-001 Reverse execution (backward step)

The emulator can step backward by cycle and by frame, restoring exact prior state, so protocols can be watched forward and backward. Backed by a frame-granular snapshot ring and deterministic re-run.

## Acceptance Criteria
- The emulator can step backward by cycle and by frame, restoring exact prior state, so protocols can be watched forward and backward.
- Backed by a frame-granular snapshot ring and deterministic re-run.
- At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The emulator can step backward by cycle and by frame, restoring exact prior state, so protocols can be watched forward and backward.
- [ ] Backed by a frame-granular snapshot ring and deterministic re-run.
- [ ] At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.

## FR-ROMM-AVUI-001 Avalonia Steam desktop UI

PLAN-ROMM-001. Steam-style desktop library, details, and lists views on the Avalonia head.
AC-AUI-01 [V]: LibraryView builds and binds LibraryBrowseViewModel (build + headless VM tests AC-BROWSE-*).
AC-AUI-02 [V]: connection dialog + browse/search/list/launch end to end (E2E).
AC-AUI-03 [V]: GameDetailsView builds and binds RomDetailViewModel (cover, metadata, files table, Attach+play/Attach only/Add-to-list, membership) (build + RomDetailViewModelTests).
AC-AUI-04 [V]: ListsView builds and binds CollectionsViewModel (rail, toolbar Rename/Delete/Add games, per-row remove) (build + CollectionsViewModelTests).

## Acceptance Criteria
- `LibraryView` builds and binds `LibraryBrowseViewModel` `[V]` (build + headless VM tests
- connection dialog + browse/search/list/launch end to end `[V]` (E2E).
- `GameDetailsView` builds and binds `RomDetailViewModel` (cover, metadata, files table,
- `ListsView` builds and binds `CollectionsViewModel` (rail, toolbar Rename/Delete/Add games,
Scope: layer-1+
**Acceptance Criteria:**
- [ ] `LibraryView` builds and binds `LibraryBrowseViewModel` `[V]` (build + headless VM tests
- [ ] connection dialog + browse/search/list/launch end to end `[V]` (E2E).
- [ ] `GameDetailsView` builds and binds `RomDetailViewModel` (cover, metadata, files table,
- [ ] `ListsView` builds and binds `CollectionsViewModel` (rail, toolbar Rename/Delete/Add games,

## FR-ROMM-BROWSE-001 RomM browse, search, filter, page, jump

PLAN-ROMM-001. Browse thousands of titles scoped to the active machine. TR-ROMM-THREAD-001.
AC-BROWSE-01: BrowseAsync maps CustomLimitOffsetPage -> LibraryPage (items,total,offset,charIndex) -> RomMGatewayBrowseTests.Page_Maps.
AC-BROWSE-02: active machine slug (c64/c128/c-plus-4/vic-20/cpet, from ICurrentMachineProvider) resolves to numeric id and always scopes the query; no user platform picker -> RomMGatewayBrowseTests.PlatformSlug_ResolvesAndFilters.
AC-BROWSE-03: searchTerm passes through to search_term -> RomMGatewayBrowseTests.SearchTerm_Passed.
AC-BROWSE-04: LoadMoreAsync appends next page; HasMore false at offset>=total -> LibraryBrowseViewModelTests.Paging_AppendsToEnd.
AC-BROWSE-05: A-Z jump maps letter -> offset via charIndex; JumpToLetterAsync loads that page -> LibraryBrowseViewModelTests.Jump_UsesCharIndex.
AC-BROWSE-06: rapid search input debounces (300 ms) to a single query -> LibraryBrowseViewModelTests.Search_Debounced.
AC-BROWSE-07: changing the active machine (Settings) re-scopes browse to the new slug and reloads; no in-library source/platform selector -> LibraryBrowseViewModelTests.MachineChange_RescopesAndReloads.
AC-BROWSE-08: background load raises PropertyChanged on the captured context (no cross-thread) -> LibraryBrowseViewModelTests.OffContext_Dispatches.

## Acceptance Criteria
- `BrowseAsync` maps `CustomLimitOffsetPage` -> `LibraryPage` (items, total, offset,
- the active machine's platform slug (`c64/c128/c-plus-4/vic-20/cpet`, from
- `searchTerm` passes through to `search_term` -> `RomMGatewayBrowseTests.SearchTerm_Passed`.
- `LoadMoreAsync` appends the next page; `HasMore` false at `offset>=total` ->
- A-Z jump maps letter -> offset via `charIndex`; `JumpToLetterAsync` loads that page ->
- rapid search input debounces (300 ms) to a single query ->
- changing the active machine (Settings) re-scopes browse to the new platform slug and
- background load raises PropertyChanged on the captured context (no cross-thread) ->
Scope: layer-1+
**Acceptance Criteria:**
- [ ] `BrowseAsync` maps `CustomLimitOffsetPage` -> `LibraryPage` (items, total, offset,
- [ ] the active machine's platform slug (`c64/c128/c-plus-4/vic-20/cpet`, from
- [ ] `searchTerm` passes through to `search_term` -> `RomMGatewayBrowseTests.SearchTerm_Passed`.
- [ ] `LoadMoreAsync` appends the next page; `HasMore` false at `offset>=total` ->
- [ ] A-Z jump maps letter -> offset via `charIndex`; `JumpToLetterAsync` loads that page ->
- [ ] rapid search input debounces (300 ms) to a single query ->
- [ ] changing the active machine (Settings) re-scopes browse to the new platform slug and
- [ ] background load raises PropertyChanged on the captured context (no cross-thread) ->

## FR-ROMM-COLLECT-001 Lists via RomM server-side collections

PLAN-ROMM-001. Game lists are RomM collections (/api/collections), synced across both heads. TR-ROMM-JSON-001.
AC-COLLECT-01: GetCollectionsAsync lists user collections; smart/virtual flagged read-only -> RomMGatewayCollectionsTests.List_FlagsReadOnly.
AC-COLLECT-02: CreateCollectionAsync POSTs and returns the collection -> RomMGatewayCollectionsTests.Create_Posts.
AC-COLLECT-03: add/remove send CollectionRomsPayload{rom_ids[]} on the right verb -> RomMGatewayCollectionsTests.AddRemove_SendPayload.
AC-COLLECT-04: rename (PUT) and delete (DELETE) -> RomMGatewayCollectionsTests.RenameDelete.
AC-COLLECT-05: CollectionsViewModel add/remove selected roms then refresh -> CollectionsViewModelTests.AddRemove_Refreshes.
AC-COLLECT-06: every collections DTO deserializes reflection-free via RomMJsonContext -> RomMJsonContextTests.Collections_SourceGenOnly.

## Acceptance Criteria
- `GetCollectionsAsync` lists user collections; smart/virtual flagged read-only ->
- `CreateCollectionAsync` POSTs and returns the collection ->
- add/remove send `CollectionRomsPayload{rom_ids[]}` on the right verb ->
- rename (PUT) and delete (DELETE) -> `RomMGatewayCollectionsTests.RenameDelete`.
- `CollectionsViewModel` add/remove selected roms then refresh ->
- every collections DTO deserializes reflection-free via `RomMJsonContext` ->
Scope: layer-1+
**Acceptance Criteria:**
- [ ] `GetCollectionsAsync` lists user collections; smart/virtual flagged read-only ->
- [ ] `CreateCollectionAsync` POSTs and returns the collection ->
- [ ] add/remove send `CollectionRomsPayload{rom_ids[]}` on the right verb ->
- [ ] rename (PUT) and delete (DELETE) -> `RomMGatewayCollectionsTests.RenameDelete`.
- [ ] `CollectionsViewModel` add/remove selected roms then refresh ->
- [ ] every collections DTO deserializes reflection-free via `RomMJsonContext` ->

## FR-ROMM-CONN-001 RomM connection and authentication

PLAN-ROMM-001. Connect the supported desktop product path to a RomM 5.x server and authenticate. TR-ROMM-NUGET-001, TR-ROMM-JSON-001, TR-ROMM-SEC-001.
AC-CONN-01: base URL + client token authenticates; Authorization: Bearer rmm_... on requests -> RomMGatewayAuthTests.ClientToken_SetsBearer.
AC-CONN-02: device-pair exchange returns a token and persists it via the store -> RomMPairingTests.Exchange_ReturnsAndPersists.
AC-CONN-03: OAuth password token auto-refreshes on near-expiry through RomMAuthHandler -> RomMAuthRefreshTests.NearExpiry_Refreshes.
AC-CONN-04: a 401 sets the connection VM to a sign-in-expired reauth state and raises ConnectionInvalid -> LibraryConnectionViewModelTests.Unauthorized_SurfacesReauth.
AC-CONN-05: on Windows, FileRomMConnectionStore persists protected_token with current-user DPAPI, writes atomically, and immediately migrates legacy plaintext records; unsupported operating systems fail closed -> FileRomMConnectionStoreTests.SaveLoad_RoundTrips + Load_LegacyPlaintext_MigratesImmediately.
AC-CONN-06: token never appears in a URL/query string -> RomMGatewayAuthTests.Token_NeverInUri.

## Acceptance Criteria
- base URL + client token authenticates; `Authorization: Bearer rmm_...` on requests ->
- device-pair exchange returns a token and persists it via the store ->
- OAuth password token auto-refreshes on near-expiry through `RomMAuthHandler` ->
- a 401 sets the connection VM to a "sign-in expired" reauth state and raises
- `FileRomMConnectionStore` round-trips baseUrl/authMode/token via source-gen JSON ->
- token never appears in a URL/query string -> `RomMGatewayAuthTests.Token_NeverInUri`.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] base URL + client token authenticates; `Authorization: Bearer rmm_...` on requests ->
- [ ] device-pair exchange returns a token and persists it via the store ->
- [ ] OAuth password token auto-refreshes on near-expiry through `RomMAuthHandler` ->
- [ ] a 401 sets the connection VM to a "sign-in expired" reauth state and raises
- [ ] `FileRomMConnectionStore` round-trips baseUrl/authMode/token via source-gen JSON ->
- [ ] token never appears in a URL/query string -> `RomMGatewayAuthTests.Token_NeverInUri`.

## FR-ROMM-COVER-001 Cover art and cache

PLAN-ROMM-001. Cover art loads efficiently for large virtualized grids. TR-ROMM-SEC-001.
AC-COVER-01: url_cover is fetched without auth; only server-relative path_cover_* is fetched with bearer under the configured RomM base address; an absolute authenticated-path input is rejected -> RomMCoverImageSourceTests.AuthRules + AuthenticatedPath_RejectsAbsoluteUri.
AC-COVER-02: two-tier cache; second request for same cover hits cache (no second HTTP) -> CoverCacheTests.SecondRequest_Cached.
AC-COVER-03: concurrency gate (<=4) + off-screen cancellation -> CoverCacheTests.Concurrency_Gated_Cancellable.
AC-COVER-04: a fetch failure yields the placeholder and never throws -> CoverCacheTests.Failure_Placeholder.

## Acceptance Criteria
- `url_cover` fetched without auth; `path_cover_*` fetched with bearer under the confirmed
- two-tier cache; the second request for the same cover hits cache (no second HTTP) ->
- concurrency gate (<=4) + off-screen cancellation ->
- a fetch failure yields the placeholder and never throws ->
Scope: layer-1+
**Acceptance Criteria:**
- [ ] `url_cover` fetched without auth; `path_cover_*` fetched with bearer under the confirmed
- [ ] two-tier cache; the second request for the same cover hits cache (no second HTTP) ->
- [ ] concurrency gate (<=4) + off-screen cancellation ->
- [ ] a fetch failure yields the placeholder and never throws ->

## FR-ROMM-DETAIL-001 RomM ROM detail

PLAN-ROMM-001. Show a game's detail (cover, metadata, files, launchable, collection membership).
AC-DETAIL-01: GetRomAsync maps DetailedRomSchema (files/cover/summary/launchable) -> RomMGatewayDetailTests.Detail_Maps.
AC-DETAIL-02: RomDetailViewModel invokes add-to-collection -> RomDetailViewModelTests.AddToCollection_Invokes.

## Acceptance Criteria
- `GetRomAsync` maps `DetailedRomSchema` (files/cover/summary/launchable) ->
- `RomDetailViewModel` invokes add-to-collection ->
Scope: layer-1+
**Acceptance Criteria:**
- [ ] `GetRomAsync` maps `DetailedRomSchema` (files/cover/summary/launchable) ->
- [ ] `RomDetailViewModel` invokes add-to-collection ->

## FR-ROMM-LAUNCH-001 Download, attach, boot

PLAN-ROMM-001. Selecting a game downloads it, attaches to the right slot, and boots on the supported Avalonia product path. TR-ROMM-SEC-001. Xbox/UWP launch acceptance is archived and outside product scope.
AC-LAUNCH-01: DownloadAsync accepts one relative filename, contains it under cacheDir/{romId}, reuses only on expected-size match, streams to a temporary file, rejects overflow/truncation, and atomically publishes complete content with progress -> RomMGatewayDownloadTests.Streams_Reuses_Progress + RejectsFileNamesOutsideRomCache + TruncatedDownload_DoesNotPublishPartialCacheEntry.
AC-LAUNCH-02: slot = SelectedSlot or MediaExtensionMap default from fs_name -> MediaExtensionMapTests.Extension_MapsSlot.
AC-LAUNCH-03: .prg is not launchable and Attach is disabled -> MediaExtensionMapTests.Prg_NotLaunchable + LibraryBrowseViewModelTests.Prg_AttachDisabled.
AC-LAUNCH-04: attach/attach+start invoke IGameLauncher with resolved slot + autostart flag -> LibraryBrowseViewModelTests.AttachStart_InvokesLauncher.
AC-LAUNCH-05: Avalonia launcher delegates to DropAndStartFileAsync(autostart)/AttachAsync(attach-only) -> AvaloniaGameLauncherTests.DelegatesToShell.
AC-LAUNCH-06: two-phase status (Downloading N% then Starting) -> LibraryBrowseViewModelTests.Launch_TwoPhaseStatus.

## Acceptance Criteria
- `DownloadAsync` streams to `<cacheDir>/{romId}/{fs_name}`, reuses on size match, reports
- slot = `SelectedSlot` or `MediaExtensionMap` default from `fs_name` ->
- `.prg` is not launchable and Attach is disabled -> `MediaExtensionMapTests.Prg_NotLaunchable`
- attach/attach+start invoke `IGameLauncher` with the resolved slot + autostart flag ->
- Xbox launcher reads bytes -> `AttachMediaAsync` payload -> `AutostartDrive8Async`(disk)/
- Avalonia launcher delegates to `DropAndStartFileAsync`(autostart)/`AttachAsync`
- two-phase status ("Downloading N%" then "Starting") ->
Scope: layer-1+
**Acceptance Criteria:**
- [ ] `DownloadAsync` streams to `<cacheDir>/{romId}/{fs_name}`, reuses on size match, reports
- [ ] slot = `SelectedSlot` or `MediaExtensionMap` default from `fs_name` ->
- [ ] `.prg` is not launchable and Attach is disabled -> `MediaExtensionMapTests.Prg_NotLaunchable`
- [ ] attach/attach+start invoke `IGameLauncher` with the resolved slot + autostart flag ->
- [ ] Xbox launcher reads bytes -> `AttachMediaAsync` payload -> `AutostartDrive8Async`(disk)/
- [ ] Avalonia launcher delegates to `DropAndStartFileAsync`(autostart)/`AttachAsync`
- [ ] two-phase status ("Downloading N%" then "Starting") ->

## FR-ROMM-PKG-001 NuGet packaging of RomM libraries

PLAN-ROMM-001. Ship ViceSharp.Library.ViewModels + ViceSharp.RomM as nuget.org packages. TR-ROMM-BOUNDARY-001, TR-ROMM-NUGET-001.
AC-PKG-01: both libraries IsPackable with id + GPL-2.0-or-later metadata -> PackageMetadataTests.Ids_License.
AC-PKG-02: PublishNuget packs both nupkgs with correct deps (RomM.Client, RomM.Client.Csdb, ViceSharp.*) -> PackageMetadataTests.Dependencies + build.ps1 PublishNuget --skip [V].
AC-PKG-03: ViceSharp.Library.ViewModels references only Protocol; no RomM./Http/Grpc./Avalonia./Windows./engine names -> LibraryViewModelsBoundaryTests.
AC-PKG-04: restore resolves RomM.Client/Csdb from nuget.org with the nuget.org-only config -> NuGetConfigTests.NugetOrgOnly + restore [V].

## Acceptance Criteria
- both libraries `IsPackable` with id + GPL-2.0-or-later metadata ->
- `PublishNuget` packs both nupkgs with correct deps (RomM.Client, RomM.Client.Csdb,
- `ViceSharp.Library.ViewModels` references only Protocol; assembly/source name no `RomM.`/
- restore resolves RomM.Client/Csdb from nuget.org with the nuget.org-only config ->
Scope: layer-1+
**Acceptance Criteria:**
- [ ] both libraries `IsPackable` with id + GPL-2.0-or-later metadata ->
- [ ] `PublishNuget` packs both nupkgs with correct deps (RomM.Client, RomM.Client.Csdb,
- [ ] `ViceSharp.Library.ViewModels` references only Protocol; assembly/source name no `RomM.`/
- [ ] restore resolves RomM.Client/Csdb from nuget.org with the nuget.org-only config ->

## FR-ROMM-XBOXUI-001 Xbox Game Pass UI and navigation

PLAN-ROMM-001. 10-foot gamepad-first library, details, and lists pages on the UWP head.
AC-XUI-01: Library in NavigationDestination, a HomePage button, and Push route -> XboxNavigationTests.Library_Routable.
AC-XUI-02 [V]: LibraryPage builds Debug-UWP, binds LibraryBrowseViewModel (grid tiles, search, read-only machine indicator, slot picker, A/Y actions, A-Z strip; no source/platform picker).
AC-XUI-03 [V]: A=Attach, Y=Attach+Autostart via the input command path.
AC-XUI-04 [V]: no RPC_E_WRONG_THREAD on async tile/cover load (guarded by AC-BROWSE-08).
AC-XUI-05 [V]: GameDetailsPage builds Debug-UWP, binds RomDetailViewModel (cover, screenshots, metadata, files, slot picker, A/Y/X); VM logic in RomDetailViewModelTests.
AC-XUI-06 [V]: ListsPage builds Debug-UWP, binds CollectionsViewModel (rail, selected-list games, X=remove/Y=rename/Menu=delete/+New list); VM logic in CollectionsViewModelTests.

## Acceptance Criteria
- `Library` in `NavigationDestination`, a HomePage button, and `Push` route ->
- `LibraryPage` builds `Debug-UWP`, binds `LibraryBrowseViewModel` (grid tiles, search,
- A = Attach, Y = Attach+Autostart via the input command path `[V]` (dev-PC E2E; command
- no `RPC_E_WRONG_THREAD` on async tile/cover load `[V]` (dev-PC E2E; guarded by AC-BROWSE-08).
- `GameDetailsPage` builds `Debug-UWP`, binds `RomDetailViewModel` (cover, screenshots,
- `ListsPage` builds `Debug-UWP`, binds `CollectionsViewModel` (collections rail, selected-list
Scope: layer-1+
**Acceptance Criteria:**
- [ ] `Library` in `NavigationDestination`, a HomePage button, and `Push` route ->
- [ ] `LibraryPage` builds `Debug-UWP`, binds `LibraryBrowseViewModel` (grid tiles, search,
- [ ] A = Attach, Y = Attach+Autostart via the input command path `[V]` (dev-PC E2E; command
- [ ] no `RPC_E_WRONG_THREAD` on async tile/cover load `[V]` (dev-PC E2E; guarded by AC-BROWSE-08).
- [ ] `GameDetailsPage` builds `Debug-UWP`, binds `RomDetailViewModel` (cover, screenshots,
- [ ] `ListsPage` builds `Debug-UWP`, binds `CollectionsViewModel` (collections rail, selected-list

## FR-RUNTIME-TAPE-002 Datasette motor ramp + sense line + record mode

Datasette transport, tape-sense, motor timing, and recording must reproduce pinned immutable VICE behavior for the same selected machine, tape media state, control sequence, and emulated clock sequence. Machine-specific tape-sense and pulse wiring belong in machine adapters. Internal pulse-buffer success or compatibility with managed-only tests does not establish VICE parity.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Drive STOP, START, FORWARD, REWIND, and RECORD from a known STOP state with no image, read-only image, and writable image. Each accepted mode and tape-sense transition matches pinned VICE. No-image RECORD and read-only-image RECORD leave mode and sense unchanged; accepted RECORD with writable media asserts sense.
- [ ] For identical accepted tape-sense transitions, C64 CPU port $0001 input bit 4 and VIC-20 VIA1 port A input bit 6 match pinned VICE, including input/output direction masking. C64 CIA1 FLAG and VIC-20 VIA2 CA1 pulse events are tested separately and are not substituted for tape-sense input.
- [ ] With an initially stopped motor and no pending datasette alarm, a motor-on transition schedules the startup read alarm at the current emulated clock plus 32000. The ordered read-side event trace immediately before, at, and after that deadline matches pinned VICE. A pre-existing pending alarm is preserved as VICE preserves it; metadata retrieval alone is not counted as a pulse event.
- [ ] For a running motor, motor-off schedules stopping at current clock plus 32000 without stopping immediately. Motor-on before that stop executes cancels the pending stop without imposing a new startup ramp. Repeated motor-off does not extend an existing stop deadline. Ordered boundary events and restart after an executed stop match pinned VICE.
- [ ] Recording writes occur only under the native combination of an attached image, accepted RECORD mode, running motor, and qualifying write-line transition. No image, read-only media that rejected RECORD, wrong mode, motor stopped, repeated levels, and nonqualifying transitions produce the same absence of tape writes as pinned VICE.
- [ ] For identical accepted recording signal traces and media, emitted TAP bytes, elapsed-gap/remainder handling, first-write state, image-position updates, and failure/STOP side effects match pinned VICE. Cover short and long gap boundaries and partial or failed writes; an incremented managed pulse-buffer count alone does not satisfy this criterion.

## FR-SETTINGS-TXN-001 Truthful transactional Settings state

Expose Active runtime settings, Accepted last accepted target including staged restart fields, and local unsaved Draft independently.
Dirty compares normalized Draft to Accepted. Restart-required compares restart-relevant Draft to Active.
Plain Apply applies live fields, accepts the complete target, clears dirty and retains a pending restart. Revert restores Accepted and does not cancel an earlier accepted restart.
Apply + Restart publishes a prepared replacement and then makes Active and Accepted equal. Any failure preserves prior runtime, accepted state and unsaved draft.
Validate profile/family, RAM, limiter, resources, cartridge image/preset and IEC root/unit/reserved-device conflicts before any externally visible mutation.
Derive active cartridge kind from the actual device. A non-none kind needs a compatible image; none detaches. Preserve image, banks, preset, writeback and uIEC across applicable restart.
Apply Warp through the existing limiter in the same settings transaction. Existing standalone Warp control remains compatible.
Serialize refresh and Apply, reject stale responses, preserve edited draft fields and refresh untouched fields. Same-field conflicts keep local draft until Apply or Revert.
Older hosts lacking State remain readable and clearly disable state-aware Apply/Restart with an upgrade explanation.
Snapshots and INI persistence include RAM, cartridge and uIEC fields. Defaults are no RAM expansion, no cartridge, preset start, writeback false, uIEC disabled with empty root and unit 9. Save-on-exit preferences remain outside host Apply/Revert.

## Acceptance Criteria
- **ac-01**: Expose Active runtime settings, Accepted last accepted target including staged restart fields, and local unsaved Draft independently.
- **ac-02**: Dirty compares normalized Draft to Accepted. Restart-required compares restart-relevant Draft to Active.
- **ac-03**: Plain Apply applies live fields, accepts the complete target, clears dirty and retains a pending restart. Revert restores Accepted and does not cancel an earlier accepted restart.
- **ac-04**: Apply + Restart publishes a prepared replacement and then makes Active and Accepted equal. Any failure preserves prior runtime, accepted state and unsaved draft.
- **ac-05**: Validate profile/family, RAM, limiter, resources, cartridge image/preset and IEC root/unit/reserved-device conflicts before any externally visible mutation.
- **ac-06**: Derive active cartridge kind from the actual device. A non-none kind needs a compatible image; none detaches. Preserve image, banks, preset, writeback and uIEC across applicable restart.
- **ac-07**: Apply Warp through the existing limiter in the same settings transaction. Existing standalone Warp control remains compatible.
- **ac-08**: Serialize refresh and Apply, reject stale responses, preserve edited draft fields and refresh untouched fields. Same-field conflicts keep local draft until Apply or Revert.
- **ac-09**: Older hosts lacking State remain readable and clearly disable state-aware Apply/Restart with an upgrade explanation.
- **ac-10**: Snapshots and INI persistence include RAM, cartridge and uIEC fields. Defaults are no RAM expansion, no cartridge, preset start, writeback false, uIEC disabled with empty root and unit 9. Save-on-exit preferences remain outside host Apply/Revert.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Expose Active runtime settings, Accepted last accepted target including staged restart fields, and local unsaved Draft independently.
- [ ] Dirty compares normalized Draft to Accepted. Restart-required compares restart-relevant Draft to Active.
- [ ] Plain Apply applies live fields, accepts the complete target, clears dirty and retains a pending restart. Revert restores Accepted and does not cancel an earlier accepted restart.
- [ ] Apply + Restart publishes a prepared replacement and then makes Active and Accepted equal. Any failure preserves prior runtime, accepted state and unsaved draft.
- [ ] Validate profile/family, RAM, limiter, resources, cartridge image/preset and IEC root/unit/reserved-device conflicts before any externally visible mutation.
- [ ] Derive active cartridge kind from the actual device. A non-none kind needs a compatible image; none detaches. Preserve image, banks, preset, writeback and uIEC across applicable restart.
- [ ] Apply Warp through the existing limiter in the same settings transaction. Existing standalone Warp control remains compatible.
- [ ] Serialize refresh and Apply, reject stale responses, preserve edited draft fields and refresh untouched fields. Same-field conflicts keep local draft until Apply or Revert.
- [ ] Older hosts lacking State remain readable and clearly disable state-aware Apply/Restart with an upgrade explanation.
- [ ] Snapshots and INI persistence include RAM, cartridge and uIEC fields. Defaults are no RAM expansion, no cartridge, preset start, writeback false, uIEC disabled with empty root and unit 9. Save-on-exit preferences remain outside host Apply/Revert.

## FR-SID-001 Three Independent Voice Oscillators

The SID chip shall emulate three independent voice channels, each with a 16-bit frequency register, a 16-bit pulse width register, and independent waveform/control selection. Each oscillator generates a 24-bit phase accumulator output that drives waveform generation.

## Acceptance Criteria
- Each voice (1-3) has an independent 16-bit frequency register (low/high byte pairs at $D400/$D401, $D407/$D408, $D40E/$D40F).
- The phase accumulator increments by the frequency value each clock cycle.
- Frequency values translate to output frequency via: F_out = (F_reg * clock_freq) / 16777216.
- Each voice has a 12-bit pulse width register ($D402/$D403, $D409/$D40A, $D410/$D411).
- The oscillator output (OSC3 readable at $D41B) reflects the upper 8 bits of voice 3's oscillator.
- The envelope output (ENV3 readable at $D41C) reflects voice 3's current envelope value.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Each voice (1-3) has an independent 16-bit frequency register (low/high byte pairs at $D400/$D401, $D407/$D408, $D40E/$D40F).
- [ ] The phase accumulator increments by the frequency value each clock cycle.
- [ ] Frequency values translate to output frequency via: F_out = (F_reg * clock_freq) / 16777216.
- [ ] Each voice has a 12-bit pulse width register ($D402/$D403, $D409/$D40A, $D410/$D411).
- [ ] The oscillator output (OSC3 readable at $D41B) reflects the upper 8 bits of voice 3's oscillator.
- [ ] The envelope output (ENV3 readable at $D41C) reflects voice 3's current envelope value.

## FR-SID-002 Waveform Generation (Triangle, Sawtooth, Pulse, Noise)

Each SID voice shall generate four selectable waveforms: Triangle, Sawtooth, Pulse, and Noise. Waveforms are selected by bits 4-7 of the control register ($D404, $D40B, $D412).

## Acceptance Criteria
- Triangle waveform: 12-bit output derived from the upper bits of the phase accumulator, folded at the midpoint.
- Sawtooth waveform: 12-bit output is the upper 12 bits of the phase accumulator directly.
- Pulse waveform: 12-bit output is all-1s when the upper 12 bits of the accumulator are less than the pulse width, else all-0s.
- Noise waveform: output from a 23-bit LFSR (see FR-SID-009), clocked when bit 19 of the oscillator transitions.
- When no waveform bit is set, the output is 0.
- Waveform selection takes effect immediately upon register write.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Triangle waveform: 12-bit output derived from the upper bits of the phase accumulator, folded at the midpoint.
- [ ] Sawtooth waveform: 12-bit output is the upper 12 bits of the phase accumulator directly.
- [ ] Pulse waveform: 12-bit output is all-1s when the upper 12 bits of the accumulator are less than the pulse width, else all-0s.
- [ ] Noise waveform: output from a 23-bit LFSR (see FR-SID-009), clocked when bit 19 of the oscillator transitions.
- [ ] When no waveform bit is set, the output is 0.
- [ ] Waveform selection takes effect immediately upon register write.

## FR-SID-003 Combined Waveform Output

When multiple waveform bits are set simultaneously, the SID outputs a combined waveform that is the logical AND of the individual waveform outputs (on the 6581) or a slightly different combination (on the 8580). Combined waveforms produce characteristic thin, metallic sounds used by many SID tunes.

## Acceptance Criteria
- On the 6581, combined waveforms are computed as the bitwise AND of the selected waveform outputs, with additional analog bleed-through modeled via lookup tables derived from chip analysis.
- On the 8580, combined waveforms use a different lookup table reflecting the digital die revision behavior.
- Triangle + Sawtooth, Triangle + Pulse, Sawtooth + Pulse, and Triangle + Sawtooth + Pulse combinations all produce distinct outputs.
- Noise combined with any other waveform produces the AND of noise LFSR output bits and the other waveform(s), and additionally corrupts the noise LFSR state.
- Combined waveform lookup tables are configurable (replaceable) via `IWaveformGenerator.SetCombinedWaveformTable()`.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] On the 6581, combined waveforms are computed as the bitwise AND of the selected waveform outputs, with additional analog bleed-through modeled via lookup tables derived from chip analysis.
- [ ] On the 8580, combined waveforms use a different lookup table reflecting the digital die revision behavior.
- [ ] Triangle + Sawtooth, Triangle + Pulse, Sawtooth + Pulse, and Triangle + Sawtooth + Pulse combinations all produce distinct outputs.
- [ ] Noise combined with any other waveform produces the AND of noise LFSR output bits and the other waveform(s), and additionally corrupts the noise LFSR state.
- [ ] Combined waveform lookup tables are configurable (replaceable) via `IWaveformGenerator.SetCombinedWaveformTable()`.

## FR-SID-004 6581 SID Filter Emulation

The 6581 SID filter is a state-variable filter with low-pass, band-pass, and high-pass outputs. The 6581 filter has a non-linear frequency response due to its analog implementation, with a characteristic "warm" sound. The filter cutoff frequency is controlled by an 11-bit value.

## Acceptance Criteria
- The filter cutoff frequency is set by $D415 (low 3 bits) and $D416 (high 8 bits), forming an 11-bit value.
- The filter resonance is set by the upper 4 bits of $D417.
- Low-pass, band-pass, and high-pass modes are selectable via $D418 bits 4-6 and may be combined.
- Each voice can be individually routed through the filter via $D417 bits 0-2.
- The external audio input can be routed through the filter via $D417 bit 3.
- The 6581 filter's non-linear cutoff frequency mapping (the "kinked" curve) is modeled.
- Filter distortion at high resonance matches the 6581 analog behavior (soft clipping).
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The filter cutoff frequency is set by $D415 (low 3 bits) and $D416 (high 8 bits), forming an 11-bit value.
- [ ] The filter resonance is set by the upper 4 bits of $D417.
- [ ] Low-pass, band-pass, and high-pass modes are selectable via $D418 bits 4-6 and may be combined.
- [ ] Each voice can be individually routed through the filter via $D417 bits 0-2.
- [ ] The external audio input can be routed through the filter via $D417 bit 3.
- [ ] The 6581 filter's non-linear cutoff frequency mapping (the "kinked" curve) is modeled.
- [ ] Filter distortion at high resonance matches the 6581 analog behavior (soft clipping).

## FR-SID-005 8580 SID Filter Emulation

The 8580 SID filter has a more linear frequency response than the 6581, producing a "cleaner" sound. The filter topology is the same (state-variable) but the analog characteristics differ significantly.

## Acceptance Criteria
- The 8580 filter cutoff frequency mapping is approximately linear (compared to the 6581's non-linear curve).
- Filter resonance behavior is less pronounced at extreme settings compared to 6581.
- The 8580 does not exhibit the same distortion/clipping at high resonance as the 6581.
- The filter implementation is selected based on the active `IMachineProfile` SID model.
- Runtime switching between 6581 and 8580 filter models is supported via configuration.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The 8580 filter cutoff frequency mapping is approximately linear (compared to the 6581's non-linear curve).
- [ ] Filter resonance behavior is less pronounced at extreme settings compared to 6581.
- [ ] The 8580 does not exhibit the same distortion/clipping at high resonance as the 6581.
- [ ] The filter implementation is selected based on the active `IMachineProfile` SID model.
- [ ] Runtime switching between 6581 and 8580 filter models is supported via configuration.

## FR-SID-006 ADSR Envelope Generator

Each SID voice has an ADSR (Attack, Decay, Sustain, Release) envelope generator that shapes the amplitude of the waveform output. The envelope is triggered by the gate bit in the control register. The ADSR bug (where certain attack/decay transitions can cause the envelope to skip to incorrect values) shall be accurately emulated.

## Acceptance Criteria
- Attack rate is set by the upper 4 bits of $D405/$D40C/$D413 with 16 rate values from 2ms to 8s.
- Decay rate is set by the lower 4 bits of $D405/$D40C/$D413 with 16 rate values from 6ms to 24s.
- Sustain level is set by the upper 4 bits of $D406/$D40D/$D414 (0-15 mapped to 0-$FF).
- Release rate is set by the lower 4 bits of $D406/$D40D/$D414 with the same 16 rates as decay.
- Setting the gate bit (bit 0 of control register) starts the attack phase.
- Clearing the gate bit starts the release phase from the current envelope level.
- The ADSR bug is reproduced: if the envelope counter reaches zero during decay/release and the rate period comparator triggers at the same cycle, the envelope can jump to $FF.
- The envelope output is an 8-bit value (0-$FF) that multiplies the waveform output.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Attack rate is set by the upper 4 bits of $D405/$D40C/$D413 with 16 rate values from 2ms to 8s.
- [ ] Decay rate is set by the lower 4 bits of $D405/$D40C/$D413 with 16 rate values from 6ms to 24s.
- [ ] Sustain level is set by the upper 4 bits of $D406/$D40D/$D414 (0-15 mapped to 0-$FF).
- [ ] Release rate is set by the lower 4 bits of $D406/$D40D/$D414 with the same 16 rates as decay.
- [ ] Setting the gate bit (bit 0 of control register) starts the attack phase.
- [ ] Clearing the gate bit starts the release phase from the current envelope level.
- [ ] The ADSR bug is reproduced: if the envelope counter reaches zero during decay/release and the rate period comparator triggers at the same cycle, the envelope can jump to $FF.
- [ ] The envelope output is an 8-bit value (0-$FF) that multiplies the waveform output.

## FR-SID-007 Ring Modulation

Ring modulation replaces the triangle waveform output of a voice with the product of that voice's triangle output and the MSB of the preceding voice's oscillator (voice 1 modulated by voice 3, voice 2 by voice 1, voice 3 by voice 2).

## Acceptance Criteria
- Ring modulation is enabled by bit 2 of the control register ($D404/$D40B/$D412).
- When enabled, the triangle output is XORed with the MSB of the modulating voice's phase accumulator.
- Ring mod only affects the triangle waveform; other selected waveforms are not modified.
- The modulating voice does not need to be gated or have its output enabled for ring mod to work.
- Ring mod and hard sync can be combined on the same voice.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Ring modulation is enabled by bit 2 of the control register ($D404/$D40B/$D412).
- [ ] When enabled, the triangle output is XORed with the MSB of the modulating voice's phase accumulator.
- [ ] Ring mod only affects the triangle waveform; other selected waveforms are not modified.
- [ ] The modulating voice does not need to be gated or have its output enabled for ring mod to work.
- [ ] Ring mod and hard sync can be combined on the same voice.

## FR-SID-008 Hard Sync (Oscillator Synchronization)

Hard sync resets a voice's phase accumulator to zero whenever the modulating voice's phase accumulator MSB transitions from 1 to 0. This produces harmonically rich timbres.

## Acceptance Criteria
- Hard sync is enabled by bit 1 of the control register ($D404/$D40B/$D412).
- When the modulating voice's oscillator MSB transitions from 1 to 0, the synced voice's phase accumulator is reset to 0.
- Voice sync chain: voice 1 synced by voice 3, voice 2 by voice 1, voice 3 by voice 2.
- The synced voice's frequency determines the harmonic content; the modulating voice's frequency determines the fundamental.
- Hard sync works with all waveform types.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Hard sync is enabled by bit 1 of the control register ($D404/$D40B/$D412).
- [ ] When the modulating voice's oscillator MSB transitions from 1 to 0, the synced voice's phase accumulator is reset to 0.
- [ ] Voice sync chain: voice 1 synced by voice 3, voice 2 by voice 1, voice 3 by voice 2.
- [ ] The synced voice's frequency determines the harmonic content; the modulating voice's frequency determines the fundamental.
- [ ] Hard sync works with all waveform types.

## FR-SID-009 Noise Waveform Linear Feedback Shift Register

The noise waveform is generated by a 23-bit Linear Feedback Shift Register (LFSR). The LFSR is clocked when bit 19 of the phase accumulator transitions from 0 to 1. The feedback polynomial matches the original SID chip.

## Acceptance Criteria
- The LFSR is 23 bits wide with feedback taps at bits 17 and 22 (XOR).
- The LFSR is clocked when bit 19 of the oscillator's phase accumulator transitions high.
- The noise output is derived from specific bits of the LFSR (bits 0, 2, 5, 9, 11, 14, 18, 20).
- Writing to the test bit (bit 3 of control register) resets the LFSR to all-ones.
- Selecting noise in combination with other waveforms corrupts the LFSR by clearing bits that correspond to zero bits in the other waveform output.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The LFSR is 23 bits wide with feedback taps at bits 17 and 22 (XOR).
- [ ] The LFSR is clocked when bit 19 of the oscillator's phase accumulator transitions high.
- [ ] The noise output is derived from specific bits of the LFSR (bits 0, 2, 5, 9, 11, 14, 18, 20).
- [ ] Writing to the test bit (bit 3 of control register) resets the LFSR to all-ones.
- [ ] Selecting noise in combination with other waveforms corrupts the LFSR by clearing bits that correspond to zero bits in the other waveform output.

## FR-SID-010 Direct Digital Sample Playback via Volume Register

The SID's master volume register ($D418 bits 0-3) can be used for 4-bit direct digital sample playback by rapidly writing sample values. This technique ("digi" or "$D418 samples") is used extensively in C64 demos and games.

## Acceptance Criteria
- Writes to $D418 bits 0-3 immediately affect the audio output level.
- The volume register acts as a 4-bit DAC that adds a DC offset to the mixed output.
- Rapid writes to $D418 produce audible PCM audio at the write rate.
- The audio output pipeline has sufficiently low latency that per-rasterline $D418 writes produce clean audio without significant aliasing.
- Galway/Daglish-style digi playback (NMI-driven 4-bit samples) is clearly audible and recognizable.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Writes to $D418 bits 0-3 immediately affect the audio output level.
- [ ] The volume register acts as a 4-bit DAC that adds a DC offset to the mixed output.
- [ ] Rapid writes to $D418 produce audible PCM audio at the write rate.
- [ ] The audio output pipeline has sufficiently low latency that per-rasterline $D418 writes produce clean audio without significant aliasing.
- [ ] Galway/Daglish-style digi playback (NMI-driven 4-bit samples) is clearly audible and recognizable.

## FR-SID-011 External Audio Input

The SID chip has an external audio input (EXT IN) that can be mixed into the output and optionally routed through the filter. This is used by some cartridges and peripherals.

## Acceptance Criteria
- The external audio input channel can be enabled via $D417 bit 3 (route through filter) or mixed directly.
- External audio input is accessible via `IAudioChip.SetExternalInput()`.
- When routed through the filter, the external input is processed identically to the voice outputs.
- The external input level is correctly scaled relative to the SID voice outputs.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The external audio input channel can be enabled via $D417 bit 3 (route through filter) or mixed directly.
- [ ] External audio input is accessible via `IAudioChip.SetExternalInput()`.
- [ ] When routed through the filter, the external input is processed identically to the voice outputs.
- [ ] The external input level is correctly scaled relative to the SID voice outputs.

## FR-SID-012 Dual-SID (Stereo SID) Configuration

The emulator shall support configurations with two (or more) SID chips at configurable addresses, enabling stereo SID playback as used by many modern SID compositions. Common second-SID addresses are $D420, $D500, and $DE00.

## Acceptance Criteria
- A second SID chip can be enabled at a configurable address (default $D420).
- The second SID operates independently with its own set of voices, filters, and envelope generators.
- Each SID can be configured independently as 6581 or 8580 model.
- Audio output from each SID can be routed to left/right stereo channels.
- Third SID support at a third configurable address is available for 3SID tunes.
- SID address ranges do not overlap with other I/O devices.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A second SID chip can be enabled at a configurable address (default $D420).
- [ ] The second SID operates independently with its own set of voices, filters, and envelope generators.
- [ ] Each SID can be configured independently as 6581 or 8580 model.
- [ ] Audio output from each SID can be routed to left/right stereo channels.
- [ ] Third SID support at a third configurable address is available for 3SID tunes.
- [ ] SID address ranges do not overlap with other I/O devices.

## FR-SID-013 SID Audio Backend Wiring

The emulator shall wire SID sample production through the host audio backend without dropping, duplicating, or bypassing real-time audio buffers.

## Acceptance Criteria
- SID sample production is delivered to the selected `IAudioBackend` implementation.
- Host audio backend writes preserve stream order and do not duplicate samples.
- Backend tests prove pause, resume, mute, and disposal paths do not leave stale SID audio buffers active.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] SID sample production is delivered to the selected `IAudioBackend` implementation.
- [ ] Host audio backend writes preserve stream order and do not duplicate samples.
- [ ] Backend tests prove pause, resume, mute, and disposal paths do not leave stale SID audio buffers active.

## FR-SID-014 VICE-Compatible Signed SID Voice Output and Demo Pacing

SID voice output shall be centered and scaled like VICE/reSID so live host audio back-pressure paces demos at the same rate as VICE across runtime segment transitions.

## Acceptance Criteria
- Selected SID waveforms are centered around the model-specific DAC zero level before envelope multiplication.
- 6581 and 8580 models use distinct zero levels matching reSID behavior.
- The mixed PCM output remains normalized without flattening active voices.
- Runtime validation of the Pieces of Light demo remains near 100% before and after leaving the first segment with Space.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Selected SID waveforms are centered around the model-specific DAC zero level before envelope multiplication.
- [ ] 6581 and 8580 models use distinct zero levels matching reSID behavior.
- [ ] The mixed PCM output remains normalized without flattening active voices.
- [ ] Runtime validation of the Pieces of Light demo remains near 100% before and after leaving the first segment with Space.

## FR-SIDAUDIO-001 SID plays at correct pitch

The SID must tick at the phi2 master-clock rate so pitch, envelopes, noise and sync are correct (BUG-SIDAUDIO-001). It was registered as a slow device (ClockDivisor 16) while its accumulator advanced once per Tick, making everything 16x too slow.
Scope: layer-1+
**Acceptance Criteria:**
- [x] With voice freq 0x8000, after stepping the SystemClock 8192 master cycles OSC3 reads 0x10 (was 0x01 at the 16x-slow rate)
- [x] Audio sample rate remains 44.1 kHz (self-corrects via ConfigureAudioClock at either divisor)
- [x] ADSR, noise-LFSR and hard-sync run at the phi2 rate

## FR-SIDEBARUI-001 Responsive sidebar layout with collapse expander

The attach sidebar has a collapse expander on its inner edge (facing the video) that flips side with the panel anchor, and its button groups wrap to new rows when the panel is narrow.
Scope: layer-1+
**Acceptance Criteria:**
- [x] The collapse expander sits on the inner edge - Right when the panel is anchored Left, Left when anchored Right
- [x] The expander toggles the sidebar pane and its chevron points toward the collapse direction
- [x] Tab and action button groups wrap to new rows (WrapPanel) when the panel is narrow

## FR-SNDREG-001 VICE gate sound back-pressure regulator

When the SID is the audio timing source, the VICE pacing gate paces the worker to the audio device draining its sample buffer (regulator 1), taking precedence over vsync.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Buffer at or over the high-water mark => worker blocks (advances nothing)
- [x] Buffer has room => worker advances a chunk
- [x] Warp skips both sound and vsync (highest precedence)
- [x] No active audio device => falls through to the vsync regulator

## FR-SNP-001 Save Complete Machine State to Snapshot

The emulator shall capture the complete machine state at any point during execution and serialize it to a snapshot file. The snapshot includes CPU registers, all RAM, all I/O chip registers and internal state, VIC-II raster state, SID oscillator/envelope state, CIA timer state, and peripheral state.

## Acceptance Criteria
- CPU state: all registers (A, X, Y, SP, PC, P), interrupt pending flags, and pipeline phase.
- Memory: complete 64KB RAM contents, Color RAM (1KB nybbles), and any expansion RAM.
- VIC-II: all 47 registers, raster position, sprite state (DMA, counters, data buffers), display state, badline state.
- SID: all registers, oscillator phase accumulators, envelope state (phase, counter, rate), filter state, noise LFSR state.
- CIA1 and CIA2: all registers, timer counters and latches, TOD clock state, shift register state, interrupt flags.
- 6510 I/O port: DDR value, port value, capacitive charge state.
- Peripherals: disk drive state (if true drive emulation), tape position, cartridge banking state.
- Snapshot files use a versioned binary format with integrity checksums.
- The `ISnapshotManager.Save()` method returns a snapshot handle or writes to a specified path.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] CPU state: all registers (A, X, Y, SP, PC, P), interrupt pending flags, and pipeline phase.
- [ ] Memory: complete 64KB RAM contents, Color RAM (1KB nybbles), and any expansion RAM.
- [ ] VIC-II: all 47 registers, raster position, sprite state (DMA, counters, data buffers), display state, badline state.
- [ ] SID: all registers, oscillator phase accumulators, envelope state (phase, counter, rate), filter state, noise LFSR state.
- [ ] CIA1 and CIA2: all registers, timer counters and latches, TOD clock state, shift register state, interrupt flags.
- [ ] 6510 I/O port: DDR value, port value, capacitive charge state.
- [ ] Peripherals: disk drive state (if true drive emulation), tape position, cartridge banking state.
- [ ] Snapshot files use a versioned binary format with integrity checksums.
- [ ] The `ISnapshotManager.Save()` method returns a snapshot handle or writes to a specified path.

## FR-SNP-002 Load Machine State from Snapshot

The emulator shall restore a complete machine state from a previously saved snapshot file. After loading, execution continues from exactly the point at which the snapshot was taken, with all devices in their saved state.

## Acceptance Criteria
- Loading a snapshot restores the exact CPU state (registers, flags, pipeline phase).
- All 64KB of RAM plus Color RAM are restored byte-for-byte.
- VIC-II state is restored including mid-raster position -- the first frame after load completes the line from the saved raster position.
- SID state is restored including oscillator phase -- audio output continues seamlessly.
- CIA state is restored including running timer values -- interrupt timing is preserved.
- Peripheral state (drives, tape, cartridges) is restored.
- Snapshot format version mismatches are detected and reported.
- Invalid or corrupt snapshots are rejected with a descriptive error.
- Loading a snapshot does not leak memory (previous state is fully released).
- The `ISnapshotManager.Load()` method accepts a snapshot handle or file path.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Loading a snapshot restores the exact CPU state (registers, flags, pipeline phase).
- [ ] All 64KB of RAM plus Color RAM are restored byte-for-byte.
- [ ] VIC-II state is restored including mid-raster position -- the first frame after load completes the line from the saved raster position.
- [ ] SID state is restored including oscillator phase -- audio output continues seamlessly.
- [ ] CIA state is restored including running timer values -- interrupt timing is preserved.
- [ ] Peripheral state (drives, tape, cartridges) is restored.
- [ ] Snapshot format version mismatches are detected and reported.
- [ ] Invalid or corrupt snapshots are rejected with a descriptive error.
- [ ] Loading a snapshot does not leak memory (previous state is fully released).
- [ ] The `ISnapshotManager.Load()` method accepts a snapshot handle or file path.

## FR-SNP-003 Deterministic Input Replay

The emulator shall support recording input events (keyboard, joystick, timing) from a starting snapshot and replaying them to reproduce the exact same execution. This enables tool-assisted speedruns (TAS), automated testing, and bug reproduction.

## Acceptance Criteria
- Recording mode captures all external input events with their precise frame and cycle timestamps.
- Playback mode loads a base snapshot and replays the recorded input events.
- Playback produces bit-exact results: the same memory contents, same VIC-II output, same audio output as the original recording at every point.
- The replay format stores: base snapshot reference, input event stream (event type, frame number, cycle within frame, value).
- Replay can be paused, single-stepped, and fast-forwarded.
- The `IReplayEngine` interface provides record/playback/seek operations.
- Replay files are compact (only input events are stored, not full state per frame).
- Seeking to an arbitrary frame is supported via periodic auto-snapshots during recording.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Recording mode captures all external input events with their precise frame and cycle timestamps.
- [ ] Playback mode loads a base snapshot and replays the recorded input events.
- [ ] Playback produces bit-exact results: the same memory contents, same VIC-II output, same audio output as the original recording at every point.
- [ ] The replay format stores: base snapshot reference, input event stream (event type, frame number, cycle within frame, value).
- [ ] Replay can be paused, single-stepped, and fast-forwarded.
- [ ] The `IReplayEngine` interface provides record/playback/seek operations.
- [ ] Replay files are compact (only input events are stored, not full state per frame).
- [ ] Seeking to an arbitrary frame is supported via periodic auto-snapshots during recording.

## FR-SNP-004 Snapshot Comparison and State Diffing

The emulator shall support comparing two snapshots and reporting the differences in machine state. This is useful for debugging, understanding program behavior, and verifying determinism.

## Acceptance Criteria
- Two snapshots can be compared via `ISnapshotManager.Compare(snapshot1, snapshot2)`.
- The diff result reports all changed CPU registers and flags.
- The diff result reports all changed memory addresses with old and new values.
- The diff result reports all changed I/O chip register values (VIC-II, SID, CIA).
- A summary mode reports the count of changes per subsystem (e.g., "RAM: 42 bytes changed, VIC-II: 3 registers changed").
- A detailed mode reports every individual change.
- Unchanged state is not included in the diff output (only deltas).
- The diff can be serialized to a human-readable format for inspection.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Two snapshots can be compared via `ISnapshotManager.Compare(snapshot1, snapshot2)`.
- [ ] The diff result reports all changed CPU registers and flags.
- [ ] The diff result reports all changed memory addresses with old and new values.
- [ ] The diff result reports all changed I/O chip register values (VIC-II, SID, CIA).
- [ ] A summary mode reports the count of changes per subsystem (e.g., "RAM: 42 bytes changed, VIC-II: 3 registers changed").
- [ ] A detailed mode reports every individual change.
- [ ] Unchanged state is not included in the diff output (only deltas).
- [ ] The diff can be serialized to a human-readable format for inspection.

## FR-SNP-005 Native VICE Snapshot Interoperability

For supported x64sc C64 and xvic VIC-20 systems, native VICE .vsf is the canonical ViceSharp snapshot format. All normal supported public and Avalonia desktop capture, save, restore, file, quick-slot, and named-slot paths must read or write native VICE snapshots through the existing managed state-transfer bridge. Proprietary ViceSharp runtime and replay payloads are not peer snapshot formats and may only remain behind an explicit legacy migration boundary.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] ViceSharp loads native VICE .vsf snapshots produced by x64sc and xvic for supported C64 and VIC-20 configurations without translating them to a ViceSharp-only snapshot format first.
- [ ] After native .vsf load, managed CPU, RAM, video-chip, CIA/VIA, SID/VIC audio, machine timing, cartridge, tape, and drive state represented by the snapshot matches the corresponding VICE resumed state at the same checkpoint.
- [ ] Starting from the same native .vsf checkpoint, ViceSharp and the corresponding VICE engine remain in checkpoint and cycle lockstep for at least 100000 cycles with zero CPU, bus, video, timer, interrupt, and mapped-memory divergences.
- [ ] ViceSharp writes native VICE .vsf snapshots for supported x64sc and xvic machines that the corresponding VICE engine loads successfully and resumes with matching state.
- [ ] Native VICE to ViceSharp to native VICE and ViceSharp to native VICE to ViceSharp round trips preserve every supported snapshot module and deterministic continuation behavior.
- [ ] Wrong machine identity, unsupported container or mandatory-module version, a manifest-invalid mandatory module set, truncated header or module framing, invalid or oversized lengths, trailing bytes, and byte corruption that violates envelope invariants or makes native VICE reject the snapshot are rejected before externally visible machine-state mutation with a descriptive compatibility error. VSF has no general payload checksum, so arbitrary payload bit flips not rejected by structural validation or native VICE are not falsely claimed detectable.
- [ ] Acceptance tests provision or generate their required native shim and .vsf fixtures; absence of the shim or fixture is a gate failure, never a skipped passing result.
- [ ] Every normal supported public and Avalonia win-x64 desktop snapshot entry point, including CaptureSnapshot, .vsf file save/load, quick save/load, and ten named slots, uses native VICE .vsf as the canonical default without an opt-in selector. A host without the packaged native runtime returns unavailable and never falls back to a proprietary format; UWP/Xbox implementation remains out of active scope without retiring its historical requirements.
- [ ] Existing vice-sharp.machine-snapshot.v2 and vice-sharp.runtime-snapshot.v1 payloads are accepted only by an explicit ImportLegacySnapshot operation and RPC that restores them once; no normal snapshot capture or save emits them, and replay payloads remain outside the snapshot API.

## FR-SYSBTN-001 Ship default gameplay binding set

Ship the default gameplay binding set. Acceptance: BindingProfile.Default enumerates the locked table by value.

## Acceptance Criteria
- BindingProfile.Default enumerates the locked table by value.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] BindingProfile.Default enumerates the locked table by value.

## FR-SYSBTN-002 Menu toggles the main menu (fixed)

Menu toggles the main menu; not remappable away from menu control. Acceptance: Menu edge in Gameplay -> MainMenu+OpenMainMenu; in MainMenu -> Gameplay+CloseMenu.

## Acceptance Criteria
- Menu edge in Gameplay -> MainMenu+OpenMainMenu; in MainMenu -> Gameplay+CloseMenu.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Menu edge in Gameplay -> MainMenu+OpenMainMenu; in MainMenu -> Gameplay+CloseMenu.

## FR-SYSBTN-003 View toggles the virtual keyboard overlay

View toggles the virtual keyboard overlay. Acceptance: View edge toggles Gameplay<->VirtualKeyboard.

## Acceptance Criteria
- View edge toggles Gameplay<->VirtualKeyboard.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] View edge toggles Gameplay<->VirtualKeyboard.

## FR-SYSBTN-004 Each non-nav AppCommand maps to a host command

Each non-nav AppCommand maps to an existing host command. Acceptance: AppCommandDispatcher invokes the correct recorded host call once with correct args on fakes.

## Acceptance Criteria
- AppCommandDispatcher invokes the correct recorded host call once with correct args on fakes.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] AppCommandDispatcher invokes the correct recorded host call once with correct args on fakes.

## FR-SYSBTN-005 LT hold-to-warp hysteresis

LT hold-to-warp hysteresis (on>=0.6/off<=0.4). Acceptance: rising 0.0->0.7 one WarpHoldOn; falling 0.7->0.3 one WarpHoldOff; 0.4..0.6 none.

## Acceptance Criteria
- rising 0.0->0.7 one WarpHoldOn; falling 0.7->0.3 one WarpHoldOff; 0.4..0.6 none.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] rising 0.0->0.7 one WarpHoldOn; falling 0.7->0.3 one WarpHoldOff; 0.4..0.6 none.

## FR-SYSBTN-006 SwapJoystickPorts flips config, no host change

SwapJoystickPorts flips XboxInputConfig.SwapPorts; no host InputSettings change. Acceptance: dispatch flips the flag, issues no ISettingsService call; second dispatch flips back.

## Acceptance Criteria
- dispatch flips the flag, issues no ISettingsService call; second dispatch flips back.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] dispatch flips the flag, issues no ISettingsService call; second dispatch flips back.

## FR-SYSBTN-007 Exit only from main menu behind ConfirmDialog

Exit only from the main menu behind ConfirmDialog. Acceptance: no RequestExit binding in Default; RequestExit only from MainMenu->ConfirmDialog, only ConfirmYes exits.

## Acceptance Criteria
- no RequestExit binding in Default; RequestExit only from MainMenu->ConfirmDialog, only ConfirmYes exits.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] no RequestExit binding in Default; RequestExit only from MainMenu->ConfirmDialog, only ConfirmYes exits.

## FR-SYSBTN-008 Bindings remappable + persisted + reset

Bindings remappable and persisted with reset-to-defaults. Acceptance: Save then Load round-trips an edited profile byte-for-byte; ResetToDefaults yields Default.

## Acceptance Criteria
- Save then Load round-trips an edited profile byte-for-byte; ResetToDefaults yields Default.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Save then Load round-trips an edited profile byte-for-byte; ResetToDefaults yields Default.

## FR-SYSINDEP-001 Independent per-system scheduling coupled only by the async IEC bus

Each system (C64, each drive) runs on its own clock and the systems couple only through the asynchronous wired-OR IEC bus, replacing cycle-lockstep.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] With a drive attached, the drive CPU advances on its own clock and is not stepped in cycle-lockstep per host instruction.
- [ ] An IEC line transition is observed by every other endpoint's system before that system reads the line.
- [ ] A real IEC transaction such as a directory or program load completes correctly under independent scheduling, at parity with the existing true-drive LOAD test.
- [ ] Each system sustains about 100 percent of its own CPU clock under load including audio on, with no fixed-chunk under-throttle.

## FR-TAP-001 Datasette Motor Control

The Commodore Datasette tape drive motor is controlled by bit 3 of the 6510 I/O port ($0001). The motor is active-low (writing 0 turns the motor on). The motor has a spin-up delay before tape movement begins and a mechanical inertia effect.

## Acceptance Criteria
- Bit 3 of $0001 controls the motor: 0 = motor on, 1 = motor off.
- The motor spin-up delay (approximately 0.5 seconds on real hardware) is modeled.
- The tape position advances only when the motor is running and spin-up is complete.
- The PLAY button state (bit 4 of $0001, cassette sense) must be active (low) for the motor to engage.
- Motor state changes are reflected in the `ITapeUnit.MotorState` property.
- Audio output from the tape (when connected to SID EXT IN) is modeled.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Bit 3 of $0001 controls the motor: 0 = motor on, 1 = motor off.
- [ ] The motor spin-up delay (approximately 0.5 seconds on real hardware) is modeled.
- [ ] The tape position advances only when the motor is running and spin-up is complete.
- [ ] The PLAY button state (bit 4 of $0001, cassette sense) must be active (low) for the motor to engage.
- [ ] Motor state changes are reflected in the `ITapeUnit.MotorState` property.
- [ ] Audio output from the tape (when connected to SID EXT IN) is modeled.

## FR-TAP-002 TAP File Format Support (v0 and v1)

The TAP file format stores raw tape pulse timing data. TAP v0 stores pulse lengths as single bytes (with 0 indicating an overflow requiring 3 additional bytes). TAP v1 extends the overflow encoding. The emulator shall read and write both TAP format versions.

## Acceptance Criteria
- TAP v0 files are correctly parsed: each byte represents a pulse half-period in units of 8 system clock cycles.
- A byte value of 0 in TAP v0 indicates the next 3 bytes form a 24-bit little-endian overflow value.
- TAP v1 files use an extended header and the same pulse encoding with improved overflow handling.
- The TAP header (magic bytes, version, platform, video standard, data length) is validated on load.
- Tape images can be mounted and unmounted at runtime via `ITapeUnit.Mount()`/`ITapeUnit.Eject()`.
- Writing to TAP format captures the pulse timing from emulated writes.
- The platform byte in the header distinguishes C64, VIC-20, and C16/Plus4 tapes.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] TAP v0 files are correctly parsed: each byte represents a pulse half-period in units of 8 system clock cycles.
- [ ] A byte value of 0 in TAP v0 indicates the next 3 bytes form a 24-bit little-endian overflow value.
- [ ] TAP v1 files use an extended header and the same pulse encoding with improved overflow handling.
- [ ] The TAP header (magic bytes, version, platform, video standard, data length) is validated on load.
- [ ] Tape images can be mounted and unmounted at runtime via `ITapeUnit.Mount()`/`ITapeUnit.Eject()`.
- [ ] Writing to TAP format captures the pulse timing from emulated writes.
- [ ] The platform byte in the header distinguishes C64, VIC-20, and C16/Plus4 tapes.

## FR-TAP-003 Cycle-Accurate Tape Read Timing

Tape reading relies on the CIA1 Timer A or the FLAG pin to measure the time between pulses from the datasette. The Kernal tape routine uses Timer A in one-shot mode to measure pulse widths and classify them as short, medium, or long pulses for data decoding.

## Acceptance Criteria
- Each pulse from the tape triggers the FLAG input on CIA1 (directly connected to the cassette read line).
- The time between pulses is measured by CIA1 Timer A and/or by CIA1 FLAG interrupts.
- Standard Commodore encoding uses three pulse widths: short (352 cycles TAP value $30), medium (512 cycles TAP value $42), and long (672 cycles TAP value $56).
- The standard Commodore tape header (FOUND marker), data blocks, and checksums are readable.
- Tape reading at normal speed produces identical results to real hardware.
- The timing accuracy supports both PAL and NTSC system clock rates.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Each pulse from the tape triggers the FLAG input on CIA1 (directly connected to the cassette read line).
- [ ] The time between pulses is measured by CIA1 Timer A and/or by CIA1 FLAG interrupts.
- [ ] Standard Commodore encoding uses three pulse widths: short (352 cycles TAP value $30), medium (512 cycles TAP value $42), and long (672 cycles TAP value $56).
- [ ] The standard Commodore tape header (FOUND marker), data blocks, and checksums are readable.
- [ ] Tape reading at normal speed produces identical results to real hardware.
- [ ] The timing accuracy supports both PAL and NTSC system clock rates.

## FR-TAP-004 Tape Write Support

The emulator shall support writing data to tape images via the datasette write line. The write signal from the VIA/CIA generates pulses that are recorded to the TAP file with correct timing.

## Acceptance Criteria
- The tape write line output generates pulses whose timing is captured.
- Written pulses are stored in TAP format with correct timing values.
- The standard Commodore SAVE routine produces valid tape data.
- Writes can be appended to existing TAP files or written to new files.
- The record interlock (RECORD + PLAY buttons) must be engaged for writes.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The tape write line output generates pulses whose timing is captured.
- [ ] Written pulses are stored in TAP format with correct timing values.
- [ ] The standard Commodore SAVE routine produces valid tape data.
- [ ] Writes can be appended to existing TAP files or written to new files.
- [ ] The record interlock (RECORD + PLAY buttons) must be engaged for writes.

## FR-TAP-005 Turbo Tape Loader Compatibility

Many commercial C64 programs used turbo tape loaders that employ faster pulse encoding than the standard Commodore format. The emulator shall support turbo loaders by providing accurate tape read timing down to individual clock cycles.

## Acceptance Criteria
- Turbo loaders using shorter pulse widths than standard (down to approximately 160 cycles per pulse) function correctly.
- Common turbo formats (Novaload, Biturbo, Turbotape, Cyberload, Pavloda) load successfully.
- Custom pulse encoding schemes are supported through accurate pulse-level emulation.
- The CIA FLAG input timing is accurate to 1 system clock cycle.
- Half-wave and full-wave detection methods both work correctly.
- Multi-speed loaders (that change pulse rates mid-load) are supported.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Turbo loaders using shorter pulse widths than standard (down to approximately 160 cycles per pulse) function correctly.
- [ ] Common turbo formats (Novaload, Biturbo, Turbotape, Cyberload, Pavloda) load successfully.
- [ ] Custom pulse encoding schemes are supported through accurate pulse-level emulation.
- [ ] The CIA FLAG input timing is accurate to 1 system clock cycle.
- [ ] Half-wave and full-wave detection methods both work correctly.
- [ ] Multi-speed loaders (that change pulse rates mid-load) are supported.

## FR-TESTGATE-001 S0 feasibility gate before production

The head is delivered as an ordered BDP v4 slice sequence where S0 is a feasibility go/no-go that must pass before any production slice. Acceptance: S0 GO recorded before S1; the ordered slice list is the single source of execution order.

## Acceptance Criteria
- S0 GO recorded before S1; the ordered slice list is the single source of execution order.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] S0 GO recorded before S1; the ordered slice list is the single source of execution order.

## FR-TESTGATE-002 0 failed AND 0 skipped per slice scope

Every gate enforces 0 failed AND 0 skipped within that slice's filter scope. Acceptance: each slice trx within its --filter reports Failed=0 and Skipped=0.

## Acceptance Criteria
- Determinism pass-count == baseline; AvaloniaBoundaryTests green; AOT 0 warnings; Category=Xbox Skipped=0.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Determinism pass-count == baseline; AvaloniaBoundaryTests green; AOT 0 warnings; Category=Xbox Skipped=0.

## FR-TESTGATE-003 Determinism + desktop never regress

Determinism and the desktop suite never regress across any gate. Acceptance: Category=Determinism 0 failed with pass count == pre-S1 baseline at every gate; Avalonia boundary + desktop build stay green.

## Acceptance Criteria
- Category=Determinism 0 failed with pass count == pre-S1 baseline at every gate; Avalonia boundary + desktop build stay green.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Category=Determinism 0 failed with pass count == pre-S1 baseline at every gate; Avalonia boundary + desktop build stay green.

## FR-TESTGATE-004 Majority off-console; device slices isolated

The majority of gates run off-console; on-console work is isolated to marked device slices. Acceptance: production off-console slices need no hardware; only S0 + device slices are onConsole=true.

## Acceptance Criteria
- production off-console slices need no hardware; only S0 + device slices are onConsole=true.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] production off-console slices need no hardware; only S0 + device slices are onConsole=true.

## FR-TICKHIST-001 Last-100-ticks time-travel debugger

A History panel lists the last 100 executed CPU instructions; when paused, selecting a tick opens a debug screen with that tick's registers, reconstructed memory, and chip state.
Scope: layer-1+
**Acceptance Criteria:**
- [x] History panel lists the last 100 completed instructions, newest first
- [x] Selecting a tick while paused shows that tick's CPU registers
- [x] Memory dump is reconstructed as-of-tick (later ticks' write-deltas reverse-applied to current RAM)
- [x] Inspection is only available while the emulator is paused

## FR-UI-001 Dockable Host UI Control Client

The UI control layer shall operate as a dockable thin client of the emulator host. It sends commands and input events, presents host status, and performs media/session/state operations through generated gRPC clients or narrow gRPC-backed client abstractions without referencing or mutating core emulator objects directly.

For local desktop presentation, the in-process Avalonia renderer may bind a host-owned render surface directly to a local emulator/frame source. That direct renderer boundary is limited to frame presentation and is not available to ViewModels. External or remote UIs display streamed output through the gRPC video service/stream APIs.

## Acceptance Criteria
- The UI control layer can connect to an emulator host, create or attach to a session, and reflect lifecycle state through gRPC-backed abstractions.
- A media attach panel can dock on the left or right side of the main window and expose disk, tape, cartridge, readonly, status, validation, and recent-media affordances.
- External or remote UIs display streamed video frames and route audio samples from host stream APIs to the platform audio backend.
- The UI maps keyboard and joystick input to normalized host input events.
- The UI invokes media attach/eject, snapshot save/load, and screenshot commands through host services.
- The UI surfaces host errors and command validation failures without crashing.
- Reconnecting to an existing remote session restores lifecycle state, current media status, and the latest committed frame.
- UI ViewModels depend on abstraction-level host client facades; generated gRPC clients are isolated behind adapters.
- The in-process Avalonia render surface may consume a local frame source directly only from the host/composition layer; ViewModels must not reference runtime internals, concrete emulator devices, or frame-source implementations.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The UI control layer can connect to an emulator host, create or attach to a session, and reflect lifecycle state through gRPC-backed abstractions.
- [ ] A media attach panel can dock on the left or right side of the main window and expose disk, tape, cartridge, readonly, status, validation, and recent-media affordances.
- [ ] External or remote UIs display streamed video frames and route audio samples from host stream APIs to the platform audio backend.
- [ ] The UI maps keyboard and joystick input to normalized host input events.
- [ ] The UI invokes media attach/eject, snapshot save/load, and screenshot commands through host services.
- [ ] The UI surfaces host errors and command validation failures without crashing.
- [ ] Reconnecting to an existing remote session restores lifecycle state, current media status, and the latest committed frame.
- [ ] UI ViewModels depend on abstraction-level host client facades; generated gRPC clients are isolated behind adapters.
- [ ] The in-process Avalonia render surface may consume a local frame source directly only from the host/composition layer; ViewModels must not reference runtime internals, concrete emulator devices, or frame-source implementations.

## FR-UI-002 Emulator Status and Machine Control Bar

The desktop UI shall provide a bottom status and control bar that surfaces host telemetry and machine controls while keeping keyboard/rendering focus stable.

## Acceptance Criteria
- The status bar displays power state, run state, limiter target, measured FPS, cycle, PC, and effective clock speed.
- The status bar exposes pause, resume, step cycle, step frame, rewind cycle, rewind frame, cold reset, warm reset, and reset-plus-drive-8 autorun controls.
- Unsupported controls remain visible but report disabled/unsupported state from the host.
- Using status controls does not stop emulator rendering or steal keyboard focus unless a text field explicitly takes focus.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The status bar presents existing run state, cycle, frame, model, limiter, automation, and control commands.
- [ ] The status bar presents IEC activity from host telemetry without replacing existing fields.
- [ ] The status bar remains usable without stealing emulator focus for normal display interaction.

## FR-UI-003 Collapsible Tabbed Emulator Sidebar

The desktop UI shall provide a collapsible sidebar with Peripherals, Settings, and Monitor tabs so routine emulator controls remain available without crowding the display surface.

## Acceptance Criteria
- A hamburger control collapses and expands the sidebar without stopping emulator input or rendering.
- The Peripherals tab contains disk, tape, cartridge, recent media, readonly, and keyboard map controls.
- The Settings tab contains limiter target, display scale/crop, and host/session settings controls.
- The Monitor tab embeds the reusable monitor control.
- Tab state remains synchronized with host status and survives sidebar collapse/expand.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The peripherals tab exposes drive attachment state and media commands for configured drives.
- [ ] Drive entries expose IEC active/idle state from the same host telemetry source as the status bar.
- [ ] Drive IEC activity returns idle in the peripherals tab after bus activity settles.

## FR-UI-004 Docked and Pop-Out Monitor Control

The UI shall provide a reusable machine monitor control that can be docked in the sidebar or popped into a separate window while sharing the same host monitor session.

## Acceptance Criteria
- The monitor control can execute commands, display output, and request register, memory, disassembly, breakpoint, and stepping operations through the host boundary.
- The monitor can dock inside the sidebar or pop out to a separate window without creating a second emulator session.
- Docked and popped monitor state stays synchronized.
- The monitor intentionally takes keyboard focus only while the user interacts with its command input.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The monitor control can execute commands, display output, and request register, memory, disassembly, breakpoint, and stepping operations through the host boundary.
- [ ] The monitor can dock inside the sidebar or pop out to a separate window without creating a second emulator session.
- [ ] Docked and popped monitor state stays synchronized.
- [ ] The monitor intentionally takes keyboard focus only while the user interacts with its command input.

## FR-UIDROP-002 Drop PRG into current session RAM and RUN at BASIC start

The Avalonia video surface shall accept a dropped `*.prg` file and load it into the current emulator session RAM at the PRG little-endian load address. The drop shall not attach the file as disk, tape, or cartridge media and shall not reset the machine. BASIC start is the live TXTTAB at `$2B/$2C` for the current machine and memory configuration (C64 `$0801`; VIC-20 unexpanded `$1001`, +3K `$0401`, +8K and above `$1201`). The host shall not assume C64 `$0801`. If the load address equals that live TXTTAB, the host shall update BASIC end pointers (VARTAB, ARYTAB, STREND) and inject RUN. If the load address is not the current BASIC start, the host shall load the bytes only and shall not RUN. Invalid or too-short PRG files return InvalidArgument without mutating emulator memory.

## Acceptance Criteria
- Drag-over of a local `*.prg` path is accepted (Copy).
- `DropAndStartFileAsync` of a `*.prg` calls host `LoadProgram` and does not attach media or reset.
- PRG bytes are written at the 2-byte little-endian load address.
- When load address equals the live TXTTAB, VARTAB/ARYTAB/STREND are set to load plus payload length and RUN is queued. TXTTAB is read from the current session (C64 `$0801`; VIC-20 `$1001` / `$0401` / `$1201` by RAM config).
- When load address differs from the live TXTTAB (including a C64 `$0801` PRG on VIC-20 unexpanded `$1001`), memory is written and RUN is not queued.
- A PRG shorter than 3 bytes is rejected with InvalidArgument and memory is unchanged.
- Existing disk and cartridge drop-and-start paths remain unchanged.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Drag-over of a local `*.prg` path is accepted (Copy).
- [ ] `DropAndStartFileAsync` of a `*.prg` calls host `LoadProgram` and does not attach media or reset.
- [ ] PRG bytes are written at the 2-byte little-endian load address.
- [ ] When load address equals the live TXTTAB, VARTAB/ARYTAB/STREND are set to load plus payload length and RUN is queued. TXTTAB is read from the current session (C64 `$0801`; VIC-20 `$1001` / `$0401` / `$1201` by RAM config).
- [ ] When load address differs from the live TXTTAB (including a C64 `$0801` PRG on VIC-20 unexpanded `$1001`), memory is written and RUN is not queued.
- [ ] A PRG shorter than 3 bytes is rejected with InvalidArgument and memory is unchanged.
- [ ] Existing disk and cartridge drop-and-start paths remain unchanged.

## FR-UIFLYOUT-001 Flyout sidebar with single side-toggle

The Attach/settings sidebar is a proper flyout (Avalonia SplitView/Flyout). A single icon button toggles the flyout's side (left/right), replacing the separate Left and Right buttons.

## Acceptance Criteria
- The Attach/settings sidebar is a proper flyout (Avalonia SplitView/Flyout).
- A single icon button toggles the flyout's side (left/right), replacing the separate Left and Right buttons.
- At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The Attach/settings sidebar is a proper flyout (Avalonia SplitView/Flyout).
- [ ] A single icon button toggles the flyout's side (left/right), replacing the separate Left and Right buttons.
- [ ] At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.

## FR-UIMENUBAR-001 VICE-style menu bar

A top menu bar with structure modeled on VICE's x64sc GTK UI: File (smart attach, attach/detach disk 8-11, tape + datasette controls, cartridge, reset soft/hard, exit), Snapshot (load/save, quick, media recording), Settings (full settings, machine/drive/audio/video/input categories, toggle warp, toggle true drive per drive, swap joysticks), Debug (monitor, step), Help (about). Menu commands bind to the existing view-model actions/host services.

## Acceptance Criteria
- A top menu bar with structure modeled on VICE's x64sc GTK UI: File (smart attach, attach/detach disk 8-11, tape + datasette controls, cartridge, reset soft/hard, exit), Snapshot (load/save, quick, media recording), Settings (full settings, machine/drive/audio/video/input categories, toggle warp, toggle true drive per drive, swap joysticks), Debug (monitor, step), Help (about).
- Menu commands bind to the existing view-model actions/host services.
- At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A top menu bar with structure modeled on VICE's x64sc GTK UI: File (smart attach, attach/detach disk 8-11, tape + datasette controls, cartridge, reset soft/hard, exit), Snapshot (load/save, quick, media recording), Settings (full settings, machine/drive/audio/video/input categories, toggle warp, toggle true drive per drive, swap joysticks), Debug (monitor, step), Help (about).
- [ ] Menu commands bind to the existing view-model actions/host services.
- [ ] At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.

## FR-UIPERIPHERAL-001 Reusable per-peripheral UserControl

Each peripheral in the sidebar (Drive 8, Drive 9, Tape, Cartridge) is rendered by a single reusable AXAML UserControl bound to a per-slot view model (status, attach/eject, RO, activity LED, True Drive toggle for drives).

## Acceptance Criteria
- Each peripheral in the sidebar (Drive 8, Drive 9, Tape, Cartridge) is rendered by a single reusable AXAML UserControl bound to a per-slot view model (status, attach/eject, RO, activity LED, True Drive toggle for drives).
- At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Each peripheral in the sidebar (Drive 8, Drive 9, Tape, Cartridge) is rendered by a single reusable AXAML UserControl bound to a per-slot view model (status, attach/eject, RO, activity LED, True Drive toggle for drives).
- [ ] At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.

## FR-UISETTINGS-001 Settings panel as a UserControl

The settings panel is a self-contained reusable AXAML UserControl bound to the settings view model (machine profile, video/renderer/palette, audio, input/joystick, limiter, resource mode).

## Acceptance Criteria
- The settings panel is a self-contained reusable AXAML UserControl bound to the settings view model (machine profile, video/renderer/palette, audio, input/joystick, limiter, resource mode).
- At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The settings panel is a self-contained reusable AXAML UserControl bound to the settings view model (machine profile, video/renderer/palette, audio, input/joystick, limiter, resource mode).
- [ ] At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.

## FR-UISETVIS-001 Family-specific Settings visibility

VIC-20-only Settings (memory preset, BLK0/1/2/3/5, expansion kind/preset/write-back, flash builder) are not effectively visible when a C64 family model is selected. When a VIC-20 family model is selected, those controls are visible and any C64-only Settings (none in the current Avalonia surface besides family-filtered Models) stay hidden. Shared limiter/display/audio/input/uIEC/persistence remain visible for both families.

## Acceptance Criteria
- **ac-c64-hides-vic20**: On C64 Computer, Settings.Vic20MemoryPreset, Vic20Blk0-5, ExpansionCartKind/Preset/WriteBack have IsEffectivelyVisible false.
- **ac-vic20-shows-vic20**: On VIC-20 Computer, those same controls have IsEffectivelyVisible true.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] On C64 Computer, Settings.Vic20MemoryPreset, Vic20Blk0-5, ExpansionCartKind/Preset/WriteBack have IsEffectivelyVisible false.
- [ ] On VIC-20 Computer, those same controls have IsEffectivelyVisible true.

## FR-VIA-001 VIA 6522 Timer A and Timer B Operation

The 6522 VIA has two 16-bit timers with different capabilities than the CIA timers. Timer 1 supports free-running and one-shot modes with optional PB7 output toggling. Timer 2 operates in one-shot mode and can optionally count pulses on PB6. Timer behavior has a known 1-cycle difference from CIA timers (the VIA timer counts N+1 cycles for a latch value of N).

## Acceptance Criteria
- Timer 1 counts down from its latch value and generates an interrupt on underflow.
- Timer 1 free-running mode automatically reloads and continues counting after underflow.
- Timer 1 one-shot mode stops after a single underflow.
- Timer 1 can toggle PB7 on each underflow (when configured via ACR bit 7).
- Timer 2 operates as a one-shot countdown or counts negative pulses on PB6.
- A latch value of N results in an interrupt after N+1 clock cycles (unlike the CIA which is N+1 only when force-loaded).
- Writing to the Timer 1 low-byte latch does not start the timer; writing the high byte starts it.
- Writing Timer 2 high byte starts the timer and clears the T2 interrupt flag.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Timer 1 counts down from its latch value and generates an interrupt on underflow.
- [ ] Timer 1 free-running mode automatically reloads and continues counting after underflow.
- [ ] Timer 1 one-shot mode stops after a single underflow.
- [ ] Timer 1 can toggle PB7 on each underflow (when configured via ACR bit 7).
- [ ] Timer 2 operates as a one-shot countdown or counts negative pulses on PB6.
- [ ] A latch value of N results in an interrupt after N+1 clock cycles (unlike the CIA which is N+1 only when force-loaded).
- [ ] Writing to the Timer 1 low-byte latch does not start the timer; writing the high byte starts it.
- [ ] Writing Timer 2 high byte starts the timer and clears the T2 interrupt flag.

## FR-VIA-002 VIA Shift Register

The 6522 VIA has an 8-bit bidirectional shift register that can be clocked by Timer 2, the system clock, or an external clock. It is used for serial data transfer in VIC-20 and disk drive contexts.

## Acceptance Criteria
- The shift register can operate in 8 modes controlled by ACR bits 2-4.
- Mode 0: Shift register disabled.
- Modes 1-3: Shift in under Timer 2 control, external clock, or free-running Timer 2 clock.
- Modes 4-7: Shift out under Timer 2 control, external clock, free-running, or system clock.
- An interrupt is generated after 8 bits have been shifted.
- The CB1 pin provides the clock and CB2 provides the data for shift register operations.
- Data is shifted MSB first.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The shift register can operate in 8 modes controlled by ACR bits 2-4.
- [ ] Mode 0: Shift register disabled.
- [ ] Modes 1-3: Shift in under Timer 2 control, external clock, or free-running Timer 2 clock.
- [ ] Modes 4-7: Shift out under Timer 2 control, external clock, free-running, or system clock.
- [ ] An interrupt is generated after 8 bits have been shifted.
- [ ] The CB1 pin provides the clock and CB2 provides the data for shift register operations.
- [ ] Data is shifted MSB first.

## FR-VIA-003 VIA Port A and Port B with Handshake Protocols

The VIA's two 8-bit I/O ports (Port A and Port B) support handshaking protocols for interfacing with external devices. Port A uses CA1/CA2 control lines; Port B uses CB1/CB2. Handshake modes include pulse output and independent interrupt control.

## Acceptance Criteria
- Port A Data Direction Register (DDRA) controls which bits are input (0) and output (1).
- Port B Data Direction Register (DDRB) controls which bits are input (0) and output (1).
- CA1 active edge (rising or falling) is selected by PCR bit 0.
- CA2 can operate as: negative edge input, positive edge input, handshake output, or pulse output (PCR bits 1-3).
- CB1/CB2 control operates identically to CA1/CA2 with PCR bits 4-7.
- Reading Port A (ORA) or Port B (ORB) clears the corresponding CA1/CB1 interrupt flags.
- Latching mode (ACR bits 0-1) can latch port input data on the active CA1/CB1 edge.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Port A Data Direction Register (DDRA) controls which bits are input (0) and output (1).
- [ ] Port B Data Direction Register (DDRB) controls which bits are input (0) and output (1).
- [ ] CA1 active edge (rising or falling) is selected by PCR bit 0.
- [ ] CA2 can operate as: negative edge input, positive edge input, handshake output, or pulse output (PCR bits 1-3).
- [ ] CB1/CB2 control operates identically to CA1/CA2 with PCR bits 4-7.
- [ ] Reading Port A (ORA) or Port B (ORB) clears the corresponding CA1/CB1 interrupt flags.
- [ ] Latching mode (ACR bits 0-1) can latch port input data on the active CA1/CB1 edge.

## FR-VIA-004 VIC-20 VIA Integration (VIA1 and VIA2)

The VIC-20 uses two VIA 6522 chips. VIA1 ($9110-$911F) handles the keyboard matrix, joystick, and cassette interface. VIA2 ($9120-$912F) handles the IEC serial bus, user port, and provides the NMI source. Both VIAs must be fully functional for VIC-20 machine profile support.

## Acceptance Criteria
- VIA1 is mapped at $9110-$911F with mirroring within its address range.
- VIA2 is mapped at $9120-$912F with mirroring within its address range.
- VIA1 Port A reads the keyboard column and joystick state.
- VIA1 Port B drives the keyboard row selection.
- VIA2 Port B handles IEC serial bus signals (ATN OUT, CLK OUT/IN, DATA OUT/IN).
- VIA2 CA1 is connected to the cassette READ line.
- VIA1 generates IRQ; VIA2 generates NMI via its interrupt output.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] VIA1 is mapped at $9110-$911F with mirroring within its address range.
- [ ] VIA2 is mapped at $9120-$912F with mirroring within its address range.
- [ ] VIA1 Port A reads the keyboard column and joystick state.
- [ ] VIA1 Port B drives the keyboard row selection.
- [ ] VIA2 Port B handles IEC serial bus signals (ATN OUT, CLK OUT/IN, DATA OUT/IN).
- [ ] VIA2 CA1 is connected to the cassette READ line.
- [ ] VIA1 generates IRQ; VIA2 generates NMI via its interrupt output.

## FR-VIA-005 Disk Drive VIA Integration (1541/1571)

The 1541 and 1571 disk drives each contain two VIA 6522 chips. VIA1 ($1800-$180F) handles the IEC serial bus communication with the host computer. VIA2 ($1C00-$1C0F) controls the drive motor, head stepper, read/write head, and density selection.

## Acceptance Criteria
- Drive VIA1 handles IEC serial bus communication: ATN IN, CLK IN/OUT, DATA IN/OUT.
- Drive VIA2 Port A reads data from the read head (GCR byte or bit stream).
- Drive VIA2 Port B controls: stepper motor (bits 0-1), motor on/off (bit 2), LED (bit 3), write protect sense (bit 4), density select (bits 5-6), head step direction (bit 7 on 1571).
- Drive VIA2 Timer 1 is used for byte-ready timing in the GCR read pipeline.
- BYTE READY signal (VIA2 CA1) triggers when a complete byte has been shifted in from the disk.
- Both VIAs generate interrupts to the drive's own CPU (6502 in the 1541).
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Drive VIA1 handles IEC serial bus communication: ATN IN, CLK IN/OUT, DATA IN/OUT.
- [ ] Drive VIA2 Port A reads data from the read head (GCR byte or bit stream).
- [ ] Drive VIA2 Port B controls: stepper motor (bits 0-1), motor on/off (bit 2), LED (bit 3), write protect sense (bit 4), density select (bits 5-6), head step direction (bit 7 on 1571).
- [ ] Drive VIA2 Timer 1 is used for byte-ready timing in the GCR read pipeline.
- [ ] BYTE READY signal (VIA2 CA1) triggers when a complete byte has been shifted in from the disk.
- [ ] Both VIAs generate interrupts to the drive's own CPU (6502 in the 1541).

## FR-VIC-001 Raster Engine with PAL/NTSC Timing

The VIC-II raster engine shall generate video output with cycle-accurate timing for both PAL (6569, 312 lines, 63 cycles/line) and NTSC (6567, 263 lines, 65 cycles/line) variants. The raster counter, raster interrupt, and display/idle state transitions shall match real hardware timing.

## Acceptance Criteria
- PAL variant generates 312 raster lines with 63 CPU cycles per line (504 pixels per line).
- NTSC variant generates 263 raster lines with 65 CPU cycles per line (520 pixels per line).
- The raster counter ($D011 bit 7 + $D012) increments once per raster line.
- A raster interrupt fires when the raster counter matches the value set in $D011/$D012.
- The raster interrupt triggers at cycle 0 of the matching line (PAL) with a 1-cycle acknowledge latency.
- The display window begins at line 51 (PAL) and ends at line 250.
- Display/idle state transitions occur at the correct cycles within each line.
- A light-pen high-to-low transition latches the current raster X position and low raster line into `$D013`/`$D014`, sets the light-pen interrupt latch, and re-arms only at the frame boundary.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] PAL variant generates 312 raster lines with 63 CPU cycles per line (504 pixels per line).
- [ ] NTSC variant generates 263 raster lines with 65 CPU cycles per line (520 pixels per line).
- [ ] The raster counter ($D011 bit 7 + $D012) increments once per raster line.
- [ ] A raster interrupt fires when the raster counter matches the value set in $D011/$D012.
- [ ] The raster interrupt triggers at cycle 0 of the matching line (PAL) with a 1-cycle acknowledge latency.
- [ ] The display window begins at line 51 (PAL) and ends at line 250.
- [ ] Display/idle state transitions occur at the correct cycles within each line.
- [ ] A light-pen high-to-low transition latches the current raster X position and low raster line into `$D013`/`$D014`, sets the light-pen interrupt latch, and re-arms only at the frame boundary.

## FR-VIC-002 Character Display Modes (Standard, Multicolor, ECM)

The VIC-II shall support all three character display modes: Standard Character Mode, Multicolor Character Mode, and Extended Color Mode (ECM). Character data is fetched from the screen matrix and character generator according to the configured VIC bank and pointers.

## Acceptance Criteria
- Standard mode displays 40x25 characters using 8x8 pixel character cells with 2 colors (background + foreground from Color RAM).
- Multicolor character mode (MCM bit set) displays 4x8 double-wide pixel characters with up to 4 colors per cell when color nybble bit 3 is set.
- Extended Color Mode (ECM bit set) allows 4 background colors selected by the upper 2 bits of the screen code, limiting the character set to 64 characters.
- ECM + MCM combination is invalid for visible graphics output and renders as color 0, but x64sc still derives the hidden foreground/priority bit from the underlying character pixel: hires character one-bits when Color RAM bit 3 is clear, and multicolor character pairs %10/%11 when Color RAM bit 3 is set.
- Screen matrix base and character generator base are controlled by $D018.
- Character data fetch timing (c-access and g-access) occurs at the correct cycles per line.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Standard mode displays 40x25 characters using 8x8 pixel character cells with 2 colors (background + foreground from Color RAM).
- [ ] Multicolor character mode (MCM bit set) displays 4x8 double-wide pixel characters with up to 4 colors per cell when color nybble bit 3 is set.
- [ ] Extended Color Mode (ECM bit set) allows 4 background colors selected by the upper 2 bits of the screen code, limiting the character set to 64 characters.
- [ ] ECM + MCM combination is invalid for visible graphics output and renders as color 0, but x64sc still derives the hidden foreground/priority bit from the underlying character pixel: hires character one-bits when Color RAM bit 3 is clear, and multicolor character pairs %10/%11 when Color RAM bit 3 is set.
- [ ] Screen matrix base and character generator base are controlled by $D018.
- [ ] Character data fetch timing (c-access and g-access) occurs at the correct cycles per line.

## FR-VIC-003 Bitmap Display Modes (Standard, Multicolor)

The VIC-II shall support Standard Bitmap Mode and Multicolor Bitmap Mode. In bitmap modes, the display is a 320x200 (standard) or 160x200 (multicolor, double-wide pixels) bitmap with color data from the screen matrix and Color RAM.

## Acceptance Criteria
- Standard bitmap mode (BMM=1, MCM=0) displays 320x200 pixels; each 8x8 cell has a foreground/background pair from the screen matrix byte.
- Multicolor bitmap mode (BMM=1, MCM=1) displays 160x200 double-wide pixels; each 4x8 cell uses up to 4 colors from screen matrix, Color RAM, and background register.
- The bitmap base address is selected by bit 3 of $D018 (either $0000 or $2000 relative to VIC bank).
- BMM + ECM combination is invalid for visible graphics output and renders as color 0, but x64sc still derives the hidden foreground/priority bit from the underlying standard-bitmap one-bits.
- BMM + MCM + ECM combination is invalid for visible graphics output and renders as color 0, but x64sc still derives the hidden foreground/priority bit from multicolor bitmap pairs %10/%11.
- Bitmap data fetch timing matches hardware (g-access reads bitmap data instead of character generator).
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Standard bitmap mode (BMM=1, MCM=0) displays 320x200 pixels; each 8x8 cell has a foreground/background pair from the screen matrix byte.
- [ ] Multicolor bitmap mode (BMM=1, MCM=1) displays 160x200 double-wide pixels; each 4x8 cell uses up to 4 colors from screen matrix, Color RAM, and background register.
- [ ] The bitmap base address is selected by bit 3 of $D018 (either $0000 or $2000 relative to VIC bank).
- [ ] BMM + ECM combination is invalid for visible graphics output and renders as color 0, but x64sc still derives the hidden foreground/priority bit from the underlying standard-bitmap one-bits.
- [ ] BMM + MCM + ECM combination is invalid for visible graphics output and renders as color 0, but x64sc still derives the hidden foreground/priority bit from multicolor bitmap pairs %10/%11.
- [ ] Bitmap data fetch timing matches hardware (g-access reads bitmap data instead of character generator).

## FR-VIC-004 Sprite Engine (8 Hardware Sprites)

The VIC-II shall emulate all 8 hardware sprites with correct positioning, priority, expansion, and multicolor capabilities. Each sprite is 24x21 pixels (standard) or 12x21 double-wide pixels (multicolor) and can be independently expanded 2x horizontally and/or vertically.

## Acceptance Criteria
- Eight sprites (0-7) are independently positionable via $D000-$D010.
- Sprite enable register ($D015) controls which sprites are displayed.
- Sprite X-expansion ($D01D) doubles horizontal size; Y-expansion ($D017) doubles vertical size.
- Multicolor mode ($D01C) per sprite uses 3 shared colors + 1 individual color.
- Sprite data pointers are read from screen matrix + $03F8-$03FF (relative to VIC bank).
- Sprite-to-background priority is controlled by $D01B per sprite.
- Sprite display priority follows the rule: lower-numbered sprites appear in front of higher-numbered sprites.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Eight sprites (0-7) are independently positionable via $D000-$D010.
- [ ] Sprite enable register ($D015) controls which sprites are displayed.
- [ ] Sprite X-expansion ($D01D) doubles horizontal size; Y-expansion ($D017) doubles vertical size.
- [ ] Multicolor mode ($D01C) per sprite uses 3 shared colors + 1 individual color.
- [ ] Sprite data pointers are read from screen matrix + $03F8-$03FF (relative to VIC bank).
- [ ] Sprite-to-background priority is controlled by $D01B per sprite.
- [ ] Sprite display priority follows the rule: lower-numbered sprites appear in front of higher-numbered sprites.

## FR-VIC-005 Sprite Collision Detection

The VIC-II shall detect sprite-to-sprite and sprite-to-background collisions and report them through the collision registers and optional interrupt generation.

## Acceptance Criteria
- Sprite-sprite collision register ($D01E) sets a bit for each sprite involved in a collision with another sprite.
- Sprite-background collision register ($D01F) sets a bit for each sprite that overlaps non-background pixels.
- Collision registers are latched -- they retain set bits until read, at which point they are cleared.
- Reading the collision register clears it atomically.
- Collision interrupt (if enabled in $D01A) fires when a new collision is detected.
- In multicolor mode, the "transparent" color (bit pattern %00) does not trigger collisions.
- Expanded sprites use the expanded pixel area for collision detection.
- Invalid ECM display-mode pixels that render as color 0 still participate in sprite priority and sprite-background collision when the x64sc hidden foreground/priority bit is set.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Sprite-sprite collision register ($D01E) sets a bit for each sprite involved in a collision with another sprite.
- [ ] Sprite-background collision register ($D01F) sets a bit for each sprite that overlaps non-background pixels.
- [ ] Collision registers are latched -- they retain set bits until read, at which point they are cleared.
- [ ] Reading the collision register clears it atomically.
- [ ] Collision interrupt (if enabled in $D01A) fires when a new collision is detected.
- [ ] In multicolor mode, the "transparent" color (bit pattern %00) does not trigger collisions.
- [ ] Expanded sprites use the expanded pixel area for collision detection.
- [ ] Invalid ECM display-mode pixels that render as color 0 still participate in sprite priority and sprite-background collision when the x64sc hidden foreground/priority bit is set.

## FR-VIC-006 Badline Handling and CPU DMA Cycle Stealing

A "badline" occurs when the lower 3 bits of the raster counter match the Y-scroll value ($D011 bits 0-2) and the display is enabled. During a badline, the VIC-II steals 40 CPU cycles (plus 3 setup cycles) to fetch character pointers from the screen matrix (c-access). The CPU is halted during these stolen cycles.

## Acceptance Criteria
- Badline condition triggers when (raster_line & 7) == (d011 & 7) and display is enabled, within the display window (lines 48-247).
- During a badline, the VIC-II performs 40 c-accesses to read the screen matrix.
- The CPU is halted for 40-43 cycles during a badline (3 setup cycles + 40 character fetch cycles).
- Sprite DMA stealing occurs independently: each enabled sprite steals 2 cycles per line (p-access + s-access pattern).
- Sprite and badline DMA can overlap, with sprite DMA taking priority.
- The cycle at which the CPU is first halted for a badline is deterministic and occurs at cycle 15 of the raster line.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Badline condition triggers when (raster_line & 7) == (d011 & 7) and display is enabled, within the display window (lines 48-247).
- [ ] During a badline, the VIC-II performs 40 c-accesses to read the screen matrix.
- [ ] The CPU is halted for 40-43 cycles during a badline (3 setup cycles + 40 character fetch cycles).
- [ ] Sprite DMA stealing occurs independently: each enabled sprite steals 2 cycles per line (p-access + s-access pattern).
- [ ] Sprite and badline DMA can overlap, with sprite DMA taking priority.
- [ ] The cycle at which the CPU is first halted for a badline is deterministic and occurs at cycle 15 of the raster line.

## FR-VIC-007 Border Behavior Including Open Border Tricks

The VIC-II border unit shall accurately emulate both the main border (top/bottom) and side borders (left/right), including the ability to "open" borders using well-known VIC tricks (toggling the RSEL/CSEL bits at specific raster positions).

## Acceptance Criteria
- With RSEL=1 (25 rows), the upper border ends at line 51 and lower border begins at line 251.
- With RSEL=0 (24 rows), the upper border ends at line 55 and lower border begins at line 247.
- With CSEL=1 (40 columns), side borders are at pixels 24-343.
- With CSEL=0 (38 columns), side borders are at pixels 31-334.
- Opening the top/bottom border: if RSEL is cleared before the border comparison line and set after, the vertical border flip-flop is not set, allowing sprites to display in the border area.
- Opening the side borders: toggling CSEL at the correct cycle prevents the horizontal border flip-flop from being set.
- On PAL x64sc timing, the horizontal border clear check occurs at cycle 17 when CSEL=1 and cycle 18 when CSEL=0; the horizontal border set check occurs at cycle 57 when CSEL=1 and cycle 56 when CSEL=0.
- Changing CSEL from 1 to 0 at cycle 56 skips the right-border set check for that line, leaving the side border open until the next line's border checks.
- Border color is set by $D020; background color by $D021.
- Closed vertical or side borders mask sprite output in the border area; sprites are visible in border pixels only when the corresponding border flip-flop is open.
- Sprite-background visibility and collision checks treat closed border pixels as border pixels, not as foreground character or bitmap pixels.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] With RSEL=1 (25 rows), the upper border ends at line 51 and lower border begins at line 251.
- [ ] With RSEL=0 (24 rows), the upper border ends at line 55 and lower border begins at line 247.
- [ ] With CSEL=1 (40 columns), side borders are at pixels 24-343.
- [ ] With CSEL=0 (38 columns), side borders are at pixels 31-334.
- [ ] Opening the top/bottom border: if RSEL is cleared before the border comparison line and set after, the vertical border flip-flop is not set, allowing sprites to display in the border area.
- [ ] Opening the side borders: toggling CSEL at the correct cycle prevents the horizontal border flip-flop from being set.
- [ ] On PAL x64sc timing, the horizontal border clear check occurs at cycle 17 when CSEL=1 and cycle 18 when CSEL=0; the horizontal border set check occurs at cycle 57 when CSEL=1 and cycle 56 when CSEL=0.
- [ ] Changing CSEL from 1 to 0 at cycle 56 skips the right-border set check for that line, leaving the side border open until the next line's border checks.
- [ ] Border color is set by $D020; background color by $D021.
- [ ] Closed vertical or side borders mask sprite output in the border area; sprites are visible in border pixels only when the corresponding border flip-flop is open.
- [ ] Sprite-background visibility and collision checks treat closed border pixels as border pixels, not as foreground character or bitmap pixels.

## FR-VIC-008 Flexible Line Interpretation (FLI) Support

The emulator shall support Flexible Line Interpretation (FLI) and Advanced FLI (AFLI) techniques, which exploit badline forcing to change the character pointer base every raster line, enabling more than the normal 8 unique character colors per 8-pixel-tall cell.

## Acceptance Criteria
- Forcing a badline every line (by changing Y-scroll each line) triggers a new c-access fetch each raster line.
- The 3-cycle "FLI bug" (gray pixels at the left of each line due to the VIC-II not having fetched character data yet) is accurately reproduced.
- Changing $D018 (VIC memory pointers) during specific cycles of a raster line takes effect for subsequent fetches on that line.
- AFLI mode (combining FLI with bitmap mode) is supported.
- The CPU cycle-accurate timing of the bank switch and pointer changes matches VICE x64sc behavior.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Forcing a badline every line (by changing Y-scroll each line) triggers a new c-access fetch each raster line.
- [ ] The 3-cycle "FLI bug" (gray pixels at the left of each line due to the VIC-II not having fetched character data yet) is accurately reproduced.
- [ ] Changing $D018 (VIC memory pointers) during specific cycles of a raster line takes effect for subsequent fetches on that line.
- [ ] AFLI mode (combining FLI with bitmap mode) is supported.
- [ ] The CPU cycle-accurate timing of the bank switch and pointer changes matches VICE x64sc behavior.

## FR-VIC-009 VIC-II Bank Switching (See also FR-MEM-004)

The VIC-II address generation shall correctly translate 14-bit VIC addresses (0-$3FFF) into the 16-bit system address space by prepending the bank selection bits from CIA2 Port A. Character ROM overlay detection shall occur within the VIC address translation.

## Acceptance Criteria
- The VIC-II generates 14-bit addresses ($0000-$3FFF) that are offset by the bank base.
- Character ROM overlay at VIC addresses $1000-$1FFF is active only in banks 0 and 2.
- When the bank changes mid-frame, subsequent VIC-II fetches use the new bank immediately.
- Sprite data fetches respect the active bank.
- Bitmap data fetches respect the active bank.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The VIC-II generates 14-bit addresses ($0000-$3FFF) that are offset by the bank base.
- [ ] Character ROM overlay at VIC addresses $1000-$1FFF is active only in banks 0 and 2.
- [ ] When the bank changes mid-frame, subsequent VIC-II fetches use the new bank immediately.
- [ ] Sprite data fetches respect the active bank.
- [ ] Bitmap data fetches respect the active bank.

## FR-VIC-010 Sprite Multiplexing DMA Timing

The VIC-II sprite DMA shall be emulated with sub-cycle accuracy to support sprite multiplexing techniques. Each sprite's DMA pattern (p-access for pointer, s-access for data) occurs at specific cycles on each raster line. Software sprite multiplexers rely on exact knowledge of when these cycles occur.

## Acceptance Criteria
- Each sprite's p-access and s-access cycles are table-driven by the active VIC-II model and must not be inferred from sprite number alone.
- For PAL x64sc timing, VICE table cycles are sprites 3-7 at 1/2, 3/4, 5/6, 7/8, and 9/10; sprites 0-2 at 58/59, 60/61, and 62/63. In vice-sharp `CurrentCycle` / `RasterX` terms these normalize to sprites 3-7 at 0/1, 2/3, 4/5, 6/7, and 8/9; sprites 0-2 at 57/58, 59/60, and 61/62.
- The CPU is halted according to the VICE BA/DMA mask for the matching sprite access slots. For PAL x64sc, sprites 3-7 consume their early-line p-/s-access slots after `sprite_dma` is latched on the prior late-line checks, so their BA lead covers the previous line's final cycles and the following line's first cycles.
- `$D015` enable bits and sprite Y registers are sampled by VICE `check_sprite_dma` at PAL public cycles 55 and 56, which normalize to vice-sharp cycles 54 and 55. Once `sprite_dma` is latched, later `$D015` clears do not cancel the already-active DMA/BA window.
- Disabling a sprite (clearing its bit in $D015) before both sprite-DMA check cycles prevents its DMA from occurring.
- Re-enabling a sprite and setting its Y-position before a matching line's sprite-DMA checks triggers the applicable BA/data slots; missing those checks waits until a later line whose low raster byte matches the sprite Y value.
- The exact cycle positions for each sprite's DMA match the VICE x64sc reference.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Each sprite's p-access and s-access cycles are table-driven by the active VIC-II model and must not be inferred from sprite number alone.
- [ ] For PAL x64sc timing, VICE table cycles are sprites 3-7 at 1/2, 3/4, 5/6, 7/8, and 9/10; sprites 0-2 at 58/59, 60/61, and 62/63. In vice-sharp `CurrentCycle` / `RasterX` terms these normalize to sprites 3-7 at 0/1, 2/3, 4/5, 6/7, and 8/9; sprites 0-2 at 57/58, 59/60, and 61/62.
- [ ] The CPU is halted according to the VICE BA/DMA mask for the matching sprite access slots. For PAL x64sc, sprites 3-7 consume their early-line p-/s-access slots after `sprite_dma` is latched on the prior late-line checks, so their BA lead covers the previous line's final cycles and the following line's first cycles.
- [ ] `$D015` enable bits and sprite Y registers are sampled by VICE `check_sprite_dma` at PAL public cycles 55 and 56, which normalize to vice-sharp cycles 54 and 55. Once `sprite_dma` is latched, later `$D015` clears do not cancel the already-active DMA/BA window.
- [ ] Disabling a sprite (clearing its bit in $D015) before both sprite-DMA check cycles prevents its DMA from occurring.
- [ ] Re-enabling a sprite and setting its Y-position before a matching line's sprite-DMA checks triggers the applicable BA/data slots; missing those checks waits until a later line whose low raster byte matches the sprite Y value.
- [ ] The exact cycle positions for each sprite's DMA match the VICE x64sc reference.

## FR-VIC20-001 VIC-I (MOS 6560 NTSC / 6561 PAL) video

The VIC-20 display is driven by the MOS 6560 (NTSC) or MOS 6561 (PAL) VIC-I chip, not the C64 VIC-II. Slice A may ship a black-frame stub that implements `IVideoChip` so host frame pull does not crash; full raster and character rendering land in later slices.

## Acceptance Criteria
- A VIC-20 machine exposes a device with role `VideoChip` implementing `IVideoChip`.
- PAL profile uses 6561 timing: 1_108_405 Hz, 71 cycles/line, 312 lines (VICE `vic20.h`).
- NTSC profile uses 6560 timing: 1_022_727 Hz, 65 cycles/line, 261 lines (VICE `vic20.h`).
- Framebuffer contract matches existing host path (BGRA, FrameCompleted).
- Full character/bitmap/color rendering and on-chip audio are delivered after the stub gate.
Scope: layer-1+
**Acceptance Criteria:**
- [x] READY boot identical canvas width/height (PAL 448x284; NTSC normal) (evidence: Vic20PixelLockstep geometry asserts)
- [x] Index SequenceEqual READY idle PAL one full frame managed vs xvic (evidence: Index_ReadyPal_SequenceEqual)
- [x] Index SequenceEqual READY idle NTSC one full frame (evidence: Index_ReadyNtsc_SequenceEqual)
- [x] Index SequenceEqual busy screen PAL (evidence: Index_BusyPal_SequenceEqual)
- [x] BGRA SequenceEqual READY PAL, READY NTSC, and busy PAL vs xvic capture_visible_frame after canvas-palette align (evidence: Bgra_ReadyPal/Ntsc/BusyPal_SequenceEqual)
- [x] Capture not sentinel/all-black after READY; BGRA alpha 0xFF (NTSC unpainted clamp rows opaque black) (evidence: Vic20PixelLockstep alpha asserts)

## FR-VIC20-002 VIC-20 base memory map and RAM expansions

Unexpanded VIC-20 has 5KB base RAM (1KB system at $0000-$03FF, 4KB user at $1000-$1FFF), character ROM at $8000, I/O at $9000, color RAM at $9400, BASIC at $C000, KERNAL at $E000. Expansions (3K/8K/16K/24K/32K) map into BLK regions.

## Acceptance Criteria
- After reset with official ROMs, BASIC is readable at $C000 and KERNAL at $E000.
- Character ROM is readable at $8000.
- Base RAM regions accept read/write at $0000-$03FF and $1000-$1FFF.
- Expansion configurations are selectable via profile/resources (later slice).
- Open-bus and I/O window rules match VICE for validated address ranges.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] After reset with official ROMs, BASIC is readable at $C000 and KERNAL at $E000.
- [ ] Character ROM is readable at $8000.
- [ ] Base RAM regions accept read/write at $0000-$03FF and $1000-$1FFF.
- [ ] Expansion configurations are selectable via profile/resources (later slice).
- [ ] Open-bus and I/O window rules match VICE for validated address ranges.

## FR-VIC20-003 VIC-20 dual VIA 6522 wiring

The VIC-20 uses two shared `Via6522` instances (one chip implementation, board wiring only in Architectures): VIA1 at $9110 (typically NMI path) and VIA2 at $9120 (typically IRQ path). No C1541-specific assumptions may live in the shared VIA chip.

## Acceptance Criteria
- Machine builds with two `Via6522` devices registered on the bus at $9110 and $9120 (16-byte windows).
- VIA1 and VIA2 are clocked and reset with the machine.
- Shared `Via6522` source contains no VIC-20 or 1541 board glue (ARCH-CHIPGLUE-001).
- Keyboard, joystick, and RESTORE wiring land in later slices without forking the chip.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Machine builds with two `Via6522` devices registered on the bus at $9110 and $9120 (16-byte windows).
- [ ] VIA1 and VIA2 are clocked and reset with the machine.
- [ ] Shared `Via6522` source contains no VIC-20 or 1541 board glue (ARCH-CHIPGLUE-001).
- [ ] Keyboard, joystick, and RESTORE wiring land in later slices without forking the chip.

## FR-VIC20-004 VIC-20 keyboard matrix via VIA ports

Keyboard scanning uses VIA port lines (not CIA). Host key events map through a VIC-20 matrix type.

## Acceptance Criteria
- Key press/release is visible to software scanning the matrix through VIA ports.
- Matrix layout matches VICE VIC-20 VKM / hardware mapping for the default layout.
- Disabled until wiring slice; skeleton builds without requiring host keyboard.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Key press/release is visible to software scanning the matrix through VIA ports.
- [ ] Matrix layout matches VICE VIC-20 VKM / hardware mapping for the default layout.
- [ ] Disabled until wiring slice; skeleton builds without requiring host keyboard.

## FR-VIC20-005 VIC-20 FE3, Ultimem, and Mega-Cart runtime behavior

The supported VIC-20 expansion set attaches through the architecture cartridge path. FE3 MODE_FLASH follows the AM29F040B TYPE_B command and erase timing model. Writable cartridge state survives detach through host-managed atomic write-back.

## Acceptance Criteria
- FE3, Ultimem, Mega-Cart, and standard BLK images mount into their documented address regions.
- FE3 byte programming uses AND semantics; sector/chip erases complete only after the 50 / 1,000,000 / 8,000,000 VICE cycle budgets, including busy status and sector suspend/resume behavior.
- Dirty FE3 and Ultimem flash images are returned for write-back, and dirty Mega-Cart NVRAM is returned separately.
- Detach persists dirty state with a same-directory temporary file and atomic replacement without modifying clean images.
- The machine builds and boots without a cartridge attached.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] FE3, Ultimem, Mega-Cart, and standard BLK images mount into their documented address regions.
- [ ] FE3 byte programming uses AND semantics; sector/chip erases complete only after the 50 / 1,000,000 / 8,000,000 VICE cycle budgets, including busy status and sector suspend/resume behavior.
- [ ] Dirty FE3 and Ultimem flash images are returned for write-back, and dirty Mega-Cart NVRAM is returned separately.
- [ ] Detach persists dirty state with a same-directory temporary file and atomic replacement without modifying clean images.
- [ ] The machine builds and boots without a cartridge attached.

## FR-VIC20-006 VIC-20 default unit 8 drive model is 1540

Historical VIC-20 kits shipped with the 1540. ViceSharp VIC-20 sessions default drive unit 8 to `DriveModel.C1540` and DOS ROM `dos1540-325302+3-01.bin`. C64 sessions keep default 1541. Users may still select 1541/1541-II later.

## Acceptance Criteria
- `Vic20MachineProfile.DefaultDriveModel` is `DriveModel.C1540` for all standard VIC-20 profiles.
- Topology / host attach for drive 8 uses 1540 DOS ROM name when following profile defaults.
- C64 profiles are unchanged (default 1541).
- Tests pin the Vic20 default to 1540.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] `Vic20MachineProfile.DefaultDriveModel` is `DriveModel.C1540` for all standard VIC-20 profiles.
- [ ] Topology / host attach for drive 8 uses 1540 DOS ROM name when following profile defaults.
- [ ] C64 profiles are unchanged (default 1541).
- [ ] Tests pin the Vic20 default to 1540.

## FR-VIC20-SOUND-001 Deterministic VIC-I sound and live PCM delivery

Writes to VIC-I sound registers update the managed sound engine. Machine execution advances the engine with the emulation clock and delivers fixed-size PCM16 mono batches without allocating on the silent register-write hot path.

## Acceptance Criteria
- VIC-I sound register writes update the corresponding managed oscillator state.
- Equal initial state and writes produce byte-identical PCM batches across managed runs.
- Focused silence and tone batches are byte-identical to native xvic.
- Live hosts receive 256-sample batches through the configured audio backend.
- Silent `Mos6561.Store` calls allocate no managed memory after warmup.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] VIC-I sound register writes update the corresponding managed oscillator state.
- [ ] Equal initial state and writes produce byte-identical PCM batches across managed runs.
- [ ] Focused silence and tone batches are byte-identical to native xvic.
- [ ] Live hosts receive 256-sample batches through the configured audio backend.
- [ ] Silent `Mos6561.Store` calls allocate no managed memory after warmup.

## FR-VICE-001 Immutable native VICE behavioral target

ViceSharp shall conform managed emulator behavior to a pinned, unmodified native VICE revision. Native VICE source and oracle behavior shall never be changed to make ViceSharp pass; observation and control integrations must be implemented as wrappers outside native/vice.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Every parity or lockstep acceptance run identifies the pinned native/vice commit and proves no tracked native/vice file differs from that commit before or after the run.
- [ ] Emulator-behavior corrections are made in managed ViceSharp; native integration code may wrap or observe VICE only from outside native/vice and may not alter target source or behavior.
- [ ] Oracle binaries used as acceptance evidence are built from the pinned unmodified VICE target; evidence from a patched or behavior-modified VICE build is rejected.
- [ ] When an external wrapper cannot obtain a required observation from immutable VICE, the parity gate fails closed and reports the missing observation instead of inferring data or changing VICE.

## FR-VSFLOCKSTEP-001 Resume externally-staged VICE .vsf snapshots in the native oracle

The native VICE oracle (vice_x64 shim) reads and resumes a .vsf snapshot staged by a standalone x64sc, including snapshots from an older VICE release whose module versions differ from the bundled submodule, so lockstep can start from a user-supplied known state (C64SC identity, reSID engine, true-drive set).
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The committed x64sc PAL/C64C fixture tests/ViceSharp.TestHarness/Fixtures/Vsf/ready-c64sc-truedrive.vsf with SHA-256 5C284F5CF0A1BC7E01A2E4117A74158B7A9D60485F5626448F4B7655C0E23C19 walks exactly to EOF as 24 framed modules (MAINCPU, C64MEM, C64CART, CIA1, CIA2, SID, SIDEXTENDED, VDRIVEIMAGE8, DRIVE8, DRIVE9, DRIVE10, DRIVE11, FSDRIVE, VIC-II, GLUE, C64MEMHACKS, TAPEPORT, DATASETTE, KEYBOARD, JOYPORT0, JOYSTICK0, JOYPORT1, JOYSTICK1, USERPORT) and loads through the pinned x64sc shim with rc=0 and snapshot_last_error=0 with no skipped prerequisite.
- [ ] The resumed MAINCPU registers A, X, Y, SP, and PC equal those encoded in the snapshot MAINCPU module.
- [ ] The current non-skipped reSID, SID parity, shim round-trip, and x64sc lockstep gates complete with zero failures after the external snapshot resume; historical pass or skip counts are not completion evidence.

## FR-WARP-001 Warp mode runs uncapped relative to 100 percent limiter

Settings Warp (VICE -warp) disables the speed limiter so the emulator runs as fast as the host allows. The status bar must show LIMITER WARP and CLOCK percent well above 100 when warp is applied through the UI or RemoteControl.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Warp via Settings or RemoteControl sets status LIMITER to WARP.
- [ ] After warp is applied, status CLOCK percent is well above 100.

## FR-XAUDIO-001 SID audio through console endpoint

SID audio plays through the console default endpoint when foreground and not muted. Acceptance (on-console): a known SID tune / $D418 tone is audible; matches desktop WinMm within resampling tolerance.

## Acceptance Criteria
- SID audio plays through the console default endpoint when foreground and not muted.
- Acceptance (on-console): a known SID tune / $D418 tone is audible; matches desktop WinMm within resampling tolerance.
- At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] SID audio plays through the console default endpoint when foreground and not muted.
- [ ] Acceptance (on-console): a known SID tune / $D418 tone is audible; matches desktop WinMm within resampling tolerance.
- [ ] At least one mapped TEST requirement exercises each criterion and retains pass/fail evidence; an unrun, skipped, or failing check does not satisfy the FR.

## FR-XAUDIO-002 Master volume/mute, gain applied once

Master volume and mute are honored; EffectiveGain applied exactly once. Acceptance: Volume scales output; Muted silences without stopping the ring; gain applied in AudioSampleConverter before samples reach the ring.

## Acceptance Criteria
- Volume scales output; Muted silences without stopping the ring; gain applied in AudioSampleConverter before samples reach the ring.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Volume scales output; Muted silences without stopping the ring; gain applied in AudioSampleConverter before samples reach the ring.

## FR-XAUDIO-003 Suspend/resume audio without deadlock

Audio pauses on suspend/background and resumes without breaking determinism or deadlocking the worker. Acceptance: Suspending stops the source voice; the worker is not parked in SubmitSamples (non-blocking ring); Resuming continues; core state unchanged.

## Acceptance Criteria
- Suspending stops the source voice; the worker is not parked in SubmitSamples (non-blocking ring); Resuming continues; core state unchanged.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Suspending stops the source voice; the worker is not parked in SubmitSamples (non-blocking ring); Resuming continues; core state unchanged.

## FR-XAV-001 Headless stays silent + deterministic

Headless/test stays silent and deterministic. Acceptance: XboxAudioBackendFactory.CreateDefault() returns null headless; SID built null-backend has IsAudioTimingSource==false and emits no samples; parity/pacing suite unchanged.

## Acceptance Criteria
- XboxAudioBackendFactory.CreateDefault() returns null headless; SID built null-backend has IsAudioTimingSource==false and emits no samples; parity/pacing suite unchanged.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] XboxAudioBackendFactory.CreateDefault() returns null headless; SID built null-backend has IsAudioTimingSource==false and emits no samples; parity/pacing suite unchanged.

## FR-XBOXGPL-006 MSIX GPL compliance payload

The MSIX bundles COPYING + THIRD_PARTY_NOTICES.md (incl. vkm GPL attribution) + the *.vkm Content, bundles zero ROM *.bin, and exposes the source URL; the release attaches MSIX + source archive. Acceptance: staging test asserts Licenses/ contents + vkm Assets present + no kernal/basic/chargen *.bin + compiled SourceUrl.

## Acceptance Criteria
- staging test asserts Licenses/ contents + vkm Assets present + no kernal/basic/chargen *.bin + compiled SourceUrl.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] staging test asserts Licenses/ contents + vkm Assets present + no kernal/basic/chargen *.bin + compiled SourceUrl.

## FR-XBOXGPL-007 Official Commodore C= logo source and CC BY-SA 4.0 attribution

When ViceSharp product branding uses the Commodore C= logo mark, the logo asset
must be the officially published Wikimedia Commons file "Commodore C= logo.svg"
(CC BY-SA 4.0, author Alien426; https://commons.wikimedia.org/wiki/File:Commodore_C%3D_logo.svg).
The project must retain a local copy of the source SVG (and any rasterization used
for packaging) plus a written attribution. Attribution text must be disclosed in
THIRD_PARTY_NOTICES.md, shipped with every NuGet package payload, surfaced in
product/package README files, and shown on About dialogs for Xbox UWP and Avalonia
desktop heads. ShareAlike obligations apply to derivative branding that includes
the mark. This requirement is separate from FR-XBOXGPL-006 (GPL/VICE license payload).
Scope: layer-1+
**Acceptance Criteria:**
- [x] Branding assets use the Wikimedia Commons Commodore C= logo.svg (or a direct rasterization thereof), not an unattributed third-party redraw. (evidence: docs/xbox/store-screenshots/Commodore_C_logo.svg; docs/xbox/store-screenshots/LOGO-ATTRIBUTION-CC-BY-SA-4.0.txt)
- [x] THIRD_PARTY_NOTICES.md names the file, author Alien426, CC BY-SA 4.0, and the commons URL. (evidence: THIRD_PARTY_NOTICES.md; tests/ViceSharp.TestHarness/Xbox/XboxGplComplianceTests.cs)
- [x] Every NuGet package packs THIRD_PARTY_NOTICES.md at package root via Directory.Build.props. (evidence: Directory.Build.props)
- [x] Root README and per-package README files include CC BY-SA 4.0 C= logo attribution. (evidence: README.md; src/**/README.md)
- [x] About surfaces expose LogoAttributionText (or equivalent) with CC BY-SA 4.0, Alien426, and commons URL for Xbox and Avalonia. (evidence: src/ViceSharp.Xbox.ViewModels/AboutInfo.cs; AboutPage.xaml; MainWindow.axaml.cs OnMenuAbout; AboutViewModelTests)

## FR-XBOXPKG-001 Head csproj references only core + Host.InProcess

The head csproj references only the managed core (+ Host.InProcess) and none of Host/Grpc.AspNetCore/AspNetCore.App/Avalonia. Acceptance: csproj + slnx text test asserts presence of core refs + absence of forbidden refs; slnx builds.

## Acceptance Criteria
- csproj + slnx text test asserts presence of core refs + absence of forbidden refs; slnx builds.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] csproj + slnx text test asserts presence of core refs + absence of forbidden refs; slnx builds.

## FR-XBOXPKG-003 Manifest declares Windows.Xbox + capability rule

Package.appxmanifest declares Windows.Xbox (Min 10.0.19041.0, MaxTested 10.0.26100.0), x64 identity, and declares internetClient iff the HTTPS ROM download path is kept (else no restricted capability). Acceptance: XML-parse asserts the TargetDeviceFamily, x64, and the internetClient-present-iff-download-kept rule.

## Acceptance Criteria
- XML-parse asserts the TargetDeviceFamily, x64, and the internetClient-present-iff-download-kept rule.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] XML-parse asserts the TargetDeviceFamily, x64, and the internetClient-present-iff-download-kept rule.

## FR-XBOXTOPO-001 New ViceSharp.Xbox UWP-on-console head project

A new project src/ViceSharp.Xbox exists and is registered in ViceSharp.slnx as the true UWP-on-Xbox-console head (net10.0-windows10.0.26100.0 + UseUwp; Native AOT for Release/Store, JIT for Debug/Dev-Mode sideload), distinct from and additive to the existing src/ViceSharp.Host.Xbox Avalonia scaffold, which stays byte-for-byte intact.

## Acceptance Criteria
- csproj exists, appears in slnx, builds under default props; Host.Xbox and its test unchanged.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] csproj exists, appears in slnx, builds under default props; Host.Xbox and its test unchanged.

## FR-XBOXUI-001 Six destinations + overlays, non-destructive back stack

Six navigable destinations + quick-menu/virtual-keyboard overlays reachable from any state; back stack never recreates/pauses the running video surface. Acceptance: push Settings->DeviceSetup, GoBack twice returns to Home in order; opening overlays pushes no stack entry.

## Acceptance Criteria
- MSIX deploys clean; menu focus-navigable; C64 frame visible.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] MSIX deploys clean; menu focus-navigable; C64 frame visible.

## FR-XBOXUI-002 Controller-only high-visibility focus

Every control operable by D-pad/left-stick + A/B, no pointer, high-visibility focus in the TV-safe rect. Acceptance: BuildGrid(3,2) edges correct, top Up null, no wrap; on-console focus-visual smoke.

## Acceptance Criteria
- BuildGrid(3,2) edges correct, top Up null, no wrap; on-console focus-visual smoke.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] BuildGrid(3,2) edges correct, top Up null, no wrap; on-console focus-visual smoke.

## FR-XBOXUI-003 One authoritative input context

UI-nav and gameplay input never simultaneous; exactly one context authoritative. Acceptance: quick menu -> non-Gameplay; closing with empty stack -> Gameplay.

## Acceptance Criteria
- quick menu -> non-Gameplay; closing with empty stack -> Gameplay.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] quick menu -> non-Gameplay; closing with empty stack -> Gameplay.

## FR-XBOXUI-004 Session keeps running under overlays

Session keeps running while overlays shown (video advancing unless explicitly paused). Acceptance: simulated navigation/overlay toggles never stop/dispose the pull adapter.

## Acceptance Criteria
- simulated navigation/overlay toggles never stop/dispose the pull adapter.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] simulated navigation/overlay toggles never stop/dispose the pull adapter.

## FR-XBOXUI-005 Video renders committed frames ~50Hz as pure sink

Video renders committed frames ~50Hz as a pure sink. Acceptance: 10 fires/200ms -> 10 pulls +/-1; pure-sink forbidden-identifier scan passes.

## Acceptance Criteria
- 10 fires/200ms -> 10 pulls +/-1; pure-sink forbidden-identifier scan passes.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] 10 fires/200ms -> 10 pulls +/-1; pure-sink forbidden-identifier scan passes.

## FR-XBOXUI-006 On-screen C64 keyboard exact key strings

On-screen controller-navigable C64 keyboard maps every key to the exact SetKeyState string incl RETURN/RUNSTOP/F1-F8. Acceptance: RETURN tile emits SetKeyState("Return",true) then false; shift-latch+F1 emits "F2"; every KeyName is in the known-good list.

## Acceptance Criteria
- exact key strings; shift-latch swaps F1->F2; RESTORE drives the NMI seam.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] exact key strings; shift-latch swaps F1->F2; RESTORE drives the NMI seam.

## FR-XBOXUI-007 ViewModels depend only on Abstractions+Protocol

ViceSharp.Xbox.ViewModels depends only on Abstractions + Protocol (portable, Kestrel-free). Acceptance: csproj-reference assertion + forbidden-identifier scan (no Core/Chips/Architectures/Host/Avalonia/Grpc/XAML).

## Acceptance Criteria
- csproj-reference assertion + forbidden-identifier scan (no Core/Chips/Architectures/Host/Avalonia/Grpc/XAML).
Scope: layer-1+
**Acceptance Criteria:**
- [ ] csproj-reference assertion + forbidden-identifier scan (no Core/Chips/Architectures/Host/Avalonia/Grpc/XAML).

## FR-XBOXUI-008 About page GPL disclosure

About page discloses GPL-2.0-or-later, VICE attribution, source-availability offer. Acceptance: LicenseIdentifier=="GPL-2.0-or-later", AttributionText contains "VICE", SourceOfferText non-empty + URL.

## Acceptance Criteria
- About discloses GPL/VICE/source URL.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] About discloses GPL/VICE/source URL.

## FR-XDEV-001 Device Setup attach/eject via host media

Device Setup presents Drive8/Drive9/Tape/Cartridge cards with typed attach + eject through the host media boundary. Acceptance: patterns per slot; Attach reads bytes via IStoragePicker and calls AttachMediaAsync; Eject calls DetachMediaAsync; no runtime-internal calls.

## Acceptance Criteria
- AvailableDriveModels==DriveModelCatalog.Implemented; 1542 flows to SetTrueDriveAsync; single-rig invariant.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] AvailableDriveModels==DriveModelCatalog.Implemented; 1542 flows to SetTrueDriveAsync; single-rig invariant.

## FR-XDEV-002 Drive-model selector rebuilds true-drive rig

Drive cards expose the implemented drive-model selector and rebuild the true-drive rig on model change. Acceptance: AvailableDriveModels==DriveModelCatalog.Implemented; active true-drive model change calls SetTrueDriveAsync(true, device, diskPath, (int)model) with 1542 for 1541-II; inactive-slot change is inert.

## Acceptance Criteria
- AvailableDriveModels==DriveModelCatalog.Implemented; active true-drive model change calls SetTrueDriveAsync(true, device, diskPath, (int)model) with 1542 for 1541-II; inactive-slot change is inert.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] AvailableDriveModels==DriveModelCatalog.Implemented; active true-drive model change calls SetTrueDriveAsync(true, device, diskPath, (int)model) with 1542 for 1541-II; inactive-slot change is inert.

## FR-XDEV-003 Single true-drive rig at a time

Only one true-drive rig at a time. Acceptance: enabling Drive9 true-drive sets Drive8.TrueDrive=false with one rebuild; disabling all issues SetTrueDriveAsync(false).

## Acceptance Criteria
- enabling Drive9 true-drive sets Drive8.TrueDrive=false with one rebuild; disabling all issues SetTrueDriveAsync(false).
Scope: layer-1+
**Acceptance Criteria:**
- [ ] enabling Drive9 true-drive sets Drive8.TrueDrive=false with one rebuild; disabling all issues SetTrueDriveAsync(false).

## FR-XKBD-001 RESTORE/NMI seam distinct from SetKeyState

A dedicated RESTORE/NMI trigger exists on the Abstractions keyboard seam and the in-process facade; the virtual-keyboard RESTORE tile drives it, not SetKeyState. Acceptance: IMachineKeyboardInput.SetRestoreState(bool) routes to the C64 NMI path (keycode 0x31) distinct from SetKeyState; VirtualKeyboardViewModel RESTORE tile calls the facade's SetRestoreState, and a spy keyboard input records the NMI trigger, not a SetKeyState("*").

## Acceptance Criteria
- IMachineKeyboardInput.SetRestoreState(bool) routes to the C64 NMI path (keycode 0x31) distinct from SetKeyState; VirtualKeyboardViewModel RESTORE tile calls the facade's SetRestoreState, and a spy keyboard input records the NMI trigger, not a SetKeyState("*").
Scope: layer-1+
**Acceptance Criteria:**
- [ ] IMachineKeyboardInput.SetRestoreState(bool) routes to the C64 NMI path (keycode 0x31) distinct from SetKeyState; VirtualKeyboardViewModel RESTORE tile calls the facade's SetRestoreState, and a spy keyboard input records the NMI trigger, not a SetKeyState("*").

## FR-XROM-001 First-run ROM provisioning gate

First run evaluates ROM provisioning and blocks normal boot until Complete (offering the wizard). Acceptance: Evaluate returns Complete only when every required role is present+hash-valid; NotProvisioned/Partial otherwise; IsBootBlocked unless Complete (or kernal not required).

## Acceptance Criteria
- Evaluate returns Complete only when every required role is present+hash-valid; NotProvisioned/Partial otherwise; IsBootBlocked unless Complete (or kernal not required).
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Evaluate returns Complete only when every required role is present+hash-valid; NotProvisioned/Partial otherwise; IsBootBlocked unless Complete (or kernal not required).

## FR-XROM-002 Wizard acquires core ROMs (HTTPS/USB)

The wizard acquires the core ROM set by verified HTTPS download (requires internetClient) or USB/storage import into LocalFolder\vice\C64. Acceptance: Download fetches basic/kernal/characters SHA256-checked after confirm; Import validates size+MD5 before copy; mismatch rejected leaving state unchanged.

## Acceptance Criteria
- Download fetches basic/kernal/characters SHA256-checked after confirm; Import validates size+MD5 before copy; mismatch rejected leaving state unchanged.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Download fetches basic/kernal/characters SHA256-checked after confirm; Import validates size+MD5 before copy; mismatch rejected leaving state unchanged.

## FR-XROM-003 Provision shipped vkm keymaps

Shipped GPL *.vkm keymaps are provisioned into the writable C64 folder so the picker lists them. Acceptance: after XboxDataPathBridge.Configure the packaged vkm exist under C64Path and ListKeyboardMapsAsync returns them; else the embedded gtk3_pos fallback.

## Acceptance Criteria
- after XboxDataPathBridge.Configure the packaged vkm exist under C64Path and ListKeyboardMapsAsync returns them; else the embedded gtk3_pos fallback.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] after XboxDataPathBridge.Configure the packaged vkm exist under C64Path and ListKeyboardMapsAsync returns them; else the embedded gtk3_pos fallback.

## FR-XSET-001 Four Settings pages bind desktop values via host

Four 10-foot Settings pages bind to the same values/option ids as desktop and apply through the host UpdateSettings pipeline. Acceptance: Refresh loads GetSettings/ListSettingsProfiles; any resource change sets HasPendingSettingsChanges; Apply sends DTO ids equal to SettingsOptionCatalog.To*(...); host-canonical returned settings adopted.

## Acceptance Criteria
- canonical-id apply + host-canonical adoption + restart gating + non-applying validate + full revert.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] canonical-id apply + host-canonical adoption + restart gating + non-applying validate + full revert.

## FR-XSET-002 Rebuild-required changes flagged and gated

Rebuild-required changes are flagged and gated as on desktop. Acceptance: profile/resource-mode -> RequiresRestart; Apply(restart:true) sends RestartSession=true; others keep false.

## Acceptance Criteria
- profile/resource-mode -> RequiresRestart; Apply(restart:true) sends RestartSession=true; others keep false.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] profile/resource-mode -> RequiresRestart; Apply(restart:true) sends RestartSession=true; others keep false.

## FR-XSET-003 Validation without applying

Validation runs without applying and surfaces per-resource results. Acceptance: Validate sends ValidateSettingsResourcesRequest, populates results, sends no UpdateSettings; editing clears stale results.

## Acceptance Criteria
- Validate sends ValidateSettingsResourcesRequest, populates results, sends no UpdateSettings; editing clears stale results.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Validate sends ValidateSettingsResourcesRequest, populates results, sends no UpdateSettings; editing clears stale results.

## FR-XSET-004 RevertSettings restores last-applied

RevertSettings restores the last-applied local state. Acceptance: after dirtying, Revert restores each field, clears dirty + validation.

## Acceptance Criteria
- after dirtying, Revert restores each field, clears dirty + validation.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] after dirtying, Revert restores each field, clears dirty + validation.

## FR-XSET-005 Xbox-only prefs to vice-sharp.ini

Master volume/mute/TV-safe-inset/per-stick deadzone are Xbox-only prefs persisted to vice-sharp.ini, not the emulator DTOs. Acceptance: setting them then Save writes [ViceSharpXbox]; fresh Load returns them; no UpdateSettingsRequest carries them.

## Acceptance Criteria
- round-trip equality; only vice-sharp.ini changes; defaults when absent.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] round-trip equality; only vice-sharp.ini changes; defaults when absent.

## FR-XVIDEO-001 Live C64 frames, BGRA, letterbox, no tear

Display live C64 frames with correct BGRA colors, 4:3 letterbox, no tearing (Win2D nearest-neighbor integer scale into the TV-safe rect). Acceptance (on-console): READY. screen + animation; colors match desktop; centered/letterboxed, no tear.

## Acceptance Criteria
- image correct; SID audible; mute silences; suspend/resume clean.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] image correct; SID audible; mute silences; suspend/resume clean.

## FR-XVIDEO-002 Render is a pure read-only sink

Frame rendering is a pure read-only sink and does not perturb determinism. Acceptance: the pull adapter invokes only TryCopyFrameInto; render-enabled vs disabled replay is bit-identical; steady-state allocation delta == 0.

## Acceptance Criteria
- the pull adapter invokes only TryCopyFrameInto; render-enabled vs disabled replay is bit-identical; steady-state allocation delta == 0.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] the pull adapter invokes only TryCopyFrameInto; render-enabled vs disabled replay is bit-identical; steady-state allocation delta == 0.

