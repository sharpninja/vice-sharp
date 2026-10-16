# FR-IO-CIA: CIA I/O Chip Functional Requirements

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Subsystem      | I/O (MOS 6526 CIA)             |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-04-13                     |

---

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

### Traceability

- **Interfaces:** `ICia`, `ITimer`
- **Test Suite:** `CiaTimerTests`, `TimerCascadeTests`, `TimerOneShotTests`, `TimerLatchBehaviorTests`

---

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

### Traceability

- **Interfaces:** `ICia`, `ITodClock`
- **Test Suite:** `TodClockTests`, `TodAlarmTests`, `TodLatchTests`, `TodBcdTests`

---

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

### Traceability

- **Interfaces:** `ICia`, `IKeyboardMatrix`
- **Test Suite:** `KeyboardMatrixTests`, `KeyGhostingTests`, `RestoreKeyNmiTests`

---

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

### Traceability

- **Interfaces:** `ICia`, `IJoystickPort`
- **Test Suite:** `JoystickReadTests`, `JoystickKeyboardConflictTests`

---

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

### Traceability

- **Interfaces:** `ICia`, `ISerialPort`
- **Test Suite:** `SerialShiftRegisterTests`, `SdrInterruptTests`

---

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

### Traceability

- **Interfaces:** `ICia`, `IInterruptController`
- **Test Suite:** `Cia2NmiTests`, `NmiEdgeTests`, `FlagPinNmiTests`

---

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

### Traceability

- **Interfaces:** `ICia`, `IInterruptController`
- **Test Suite:** `Cia1IrqTests`, `IrqMaskTests`, `IrqLevelTests`
