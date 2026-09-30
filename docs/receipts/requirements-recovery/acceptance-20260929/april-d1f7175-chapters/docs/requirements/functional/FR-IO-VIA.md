# FR-IO-VIA: VIA I/O Chip Functional Requirements

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Subsystem      | I/O (MOS 6522 VIA)             |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-04-13                     |

---

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

### Traceability

- **Interfaces:** `IVia`, `ITimer`
- **Test Suite:** `ViaTimerTests`, `ViaTimerVsCiaComparisonTests`, `ViaPb7ToggleTests`

---

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

### Traceability

- **Interfaces:** `IVia`
- **Test Suite:** `ViaShiftRegisterTests`, `ViaShiftModeTests`

---

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

### Traceability

- **Interfaces:** `IVia`
- **Test Suite:** `ViaPortHandshakeTests`, `ViaControlLineTests`, `ViaLatchModeTests`

---

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

### Traceability

- **Interfaces:** `IVia`, `IAddressSpace`
- **Test Suite:** `Vic20ViaIntegrationTests`, `Vic20KeyboardTests`, `Vic20IecBusTests`

---

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

### Traceability

- **Interfaces:** `IVia`, `IDiskDrive`
- **Test Suite:** `DriveViaTests`, `DriveIecHandshakeTests`, `DriveMotorControlTests`
