# FR-Configuration-Resources: Configuration and Resource Functional Requirements

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Subsystem      | Configuration / Resources      |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-05-13 |

---

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
5. Reset-plus-drive-8 autorun succeeds for a readable drive 8 D64 containing a runnable PRG and reports an explicit failed-precondition status when required host, media, runtime, or keyboard automation prerequisites are missing.

### Source References

- `native/vice/vice/doc/vice.texi`: command-line autostart, autostart settings, autostart resources, autostart command-line options, and disk/tape image autostart sections.

### Traceability

- **Interfaces:** `HostMediaService`, `HostInputService`, `HostControlService`, `IAutostartService`
- **Technical Requirements:** TR-GRPC-BOUNDARY-001, TR-DET-001
- **Test Suite:** `AutostartServiceTests`, `ResetDrive8AutorunTests`

---

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
