# FR-Input: Input Device Functional Requirements

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Subsystem      | Input Devices                  |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-05-13 |

---

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
