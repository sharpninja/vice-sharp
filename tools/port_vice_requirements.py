from __future__ import annotations

from collections import Counter, defaultdict
import argparse
import json
from pathlib import Path
import re
from urllib import error, request


ROOT = Path(__file__).resolve().parents[1]
TODAY = "2026-05-13"

FR_DIR = ROOT / "docs" / "requirements" / "functional"
TR_DIR = ROOT / "docs" / "requirements" / "technical"
TEST_DIR = ROOT / "docs" / "requirements" / "test"
TRACE_DIR = ROOT / "docs" / "requirements" / "traceability"
SOURCES_DIR = ROOT / "docs" / "requirements" / "sources"
VICE_DOC = ROOT / "native" / "vice" / "vice" / "doc"


SOURCE_REFS = {
    "FR-CPU": [
        "`native/vice/vice/doc/vice.texi`: emulator feature sections for C64/C128/SCPU64 CPU compatibility and monitor-visible execution behavior."
    ],
    "FR-MEM": [
        "`native/vice/vice/doc/vice.texi`: machine-specific memory, cartridge, ROM, and RAM initialization settings."
    ],
    "FR-VIC": [
        "`native/vice/vice/doc/vice.texi`: video settings, C64/C128 VIC-II features, display mode, border, raster, and palette behavior."
    ],
    "FR-SID": [
        "`native/vice/vice/doc/vice.texi`: sound settings, SID model/filter behavior, audio resources, and SID file environment sections."
    ],
    "FR-CIA": [
        "`native/vice/vice/doc/CIA-README.txt`: CIA timer/alarm behavior.",
        "`native/vice/vice/doc/vice.texi`: keyboard, joystick, IEC, and machine-feature behavior involving CIA ports.",
    ],
    "FR-VIA": [
        "`native/vice/vice/doc/vice.texi`: VIC-20, PET/CBM-II, and disk-drive I/O feature sections.",
        "`native/vice/vice/doc/coding-guidelines.txt`: source layout only; no FR text derived from coding policy.",
    ],
    "FR-DRV": [
        "`native/vice/vice/doc/vice.texi`: disk drive emulation, drive resources, supported disk formats, autostart, and file-system device behavior.",
        "`native/vice/vice/doc/iec-bus.txt`: IEC bus topology and drive interaction overview.",
    ],
    "FR-TAP": [
        "`native/vice/vice/doc/vice.texi`: disk/tape image handling, tape settings, tape resources, and TAP file behavior."
    ],
    "FR-CRT": [
        "`native/vice/vice/doc/vice.texi`: C64 cartridge settings, supported cartridge formats, and machine-specific cartridge behavior."
    ],
    "FR-INP": [
        "`native/vice/vice/doc/vice.texi`: keyboard emulation, keymap files, joystick emulation, joymap files, control-port resources, mouse/lightpen/paddle behavior.",
        "`native/vice/vice/doc/html/images/keymaps.txt`: documented keymap coverage.",
        "`native/vice/vice/doc/joystick.md`: joystick API and host-driver behavior.",
    ],
    "FR-MED": [
        "`native/vice/vice/doc/vice.texi`: screenshot, media output, sound/video recording, and format-selection behavior exposed to users."
    ],
    "FR-MON": [
        "`native/vice/vice/doc/vice.texi`: monitor settings, monitor command-line options, debugging, disassembly, memory, register, breakpoint, and watch behavior."
    ],
    "FR-SNP": [
        "`native/vice/vice/doc/vice.texi`: snapshot, history, recording, and state persistence behavior exposed by emulator commands."
    ],
    "FR-PRF": [
        "`native/vice/vice/doc/vice.texi`: emulator feature sections for C64, C64DTV, C128, VIC20, PET, CBM-II, SCPU64, Plus/4, and C16 class machines."
    ],
    "FR-HOST": [
        "`native/vice/vice/doc/vice.texi`: invoking emulators, basic operation, settings/resources, reset, autostart, monitor, media, and diagnostics as observable host-control behavior."
    ],
    "FR-UI": [
        "`native/vice/vice/doc/vice.texi`: emulation window, menus, file selector, disk/tape images, reset, settings/resources, monitor, and help behavior as user-facing control requirements."
    ],
    "FR-CFG": [
        "`native/vice/vice/doc/vice.texi`: system files, resource files, command-line resource overrides, ROM/keymap/joymap/palette/romset/hotkey files, performance/debug/network/peripheral settings."
    ],
}


def write(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text.rstrip() + "\n", encoding="utf-8")


def replace_last_updated(text: str) -> str:
    return re.sub(r"(\| Last Updated\s+\| )[^|]+(\|)", rf"\g<1>{TODAY} \2", text)


def refs_for(fr_id: str) -> list[str]:
    for prefix, refs in SOURCE_REFS.items():
        if fr_id.startswith(prefix):
            return refs
    return ["`native/vice/vice/doc/vice.texi`: reviewed source corpus for observable emulator behavior."]


def ensure_source_refs(text: str) -> str:
    parts = re.split(r"(?=^## FR-[A-Z0-9]+-\d{3}: )", text, flags=re.M)
    if len(parts) <= 1:
        return text
    out = [parts[0]]
    for section in parts[1:]:
        match = re.match(r"^## (FR-[A-Z0-9]+-\d{3}):", section)
        if not match or "### Source References" in section:
            out.append(section)
            continue
        fr_id = match.group(1)
        block = "\n### Source References\n\n" + "\n".join(f"- {ref}" for ref in refs_for(fr_id)) + "\n\n"
        if "\n### Traceability\n" in section:
            section = section.replace("\n### Traceability\n", block + "### Traceability\n", 1)
        else:
            section = section.rstrip() + block
        out.append(section)
    return "".join(out)


def generate_source_manifest() -> None:
    source_notes = {
        "vice.texi": ("Master manual", "Extract FRs for emulator features, machine profiles, keyboard/joystick, media, resources, monitor, snapshots, drives, cartridges, video, sound, tape, and configuration behavior."),
        "CIA-README.txt": ("Subsystem design note", "Extract CIA timer, alarm, underflow, delayed-load, and interrupt-observable behavior."),
        "iec-bus.txt": ("Subsystem design note", "Extract IEC bus topology and observable drive/serial-bus interaction behavior."),
        "joystick.md": ("Subsystem design note", "Extract joystick API behavior and host/controller mapping expectations."),
        "html/images/keymaps.txt": ("Generated-doc note", "Extract keyboard layout/keymap coverage notes not duplicated by source prose."),
        "mainpage.dox": ("Developer index", "Use as index only; detailed FRs come from included CIA/IEC/howto docs."),
        "coding-guidelines.txt": ("Developer process", "No FR extracted; used only to confirm source layout, not Vice-Sharp TRs."),
        "Documentation-Howto.txt": ("Developer process", "No FR extracted."),
        "Doxygen-Howto.txt": ("Developer process", "No FR extracted."),
        "Release-Howto.txt": ("Developer process", "No FR extracted."),
        "gpl.texi": ("License", "No FR extracted."),
        "html/fonts/OFL.txt": ("License", "No FR extracted."),
        "html/robots.txt": ("Website metadata", "No FR extracted."),
        "vim/README.md": ("Editor tooling", "No FR extracted."),
        "readmes/Readme-SDL.txt": ("Port readme", "Reviewed for observable UI/input behavior; canonical FR wording is taken from vice.texi where duplicated."),
        "readmes/Readme-SDL2.txt": ("Port readme", "Reviewed for observable UI/input behavior; canonical FR wording is taken from vice.texi where duplicated."),
    }
    files = sorted(p for p in VICE_DOC.rglob("*") if p.is_file() and p.suffix.lower() in {".txt", ".md", ".texi", ".dox"})
    lines = [
        "# VICE Source Manifest",
        "",
        "## Document Information",
        "",
        "| Field | Value |",
        "|-------|-------|",
        "| Source Corpus | `native/vice/vice/doc` |",
        f"| Generated | {TODAY} |",
        "| Purpose | Inventory classic VICE documentation reviewed for Functional Requirement extraction. |",
        "",
        "## Extraction Rules",
        "",
        "- Use classic VICE docs only for Functional Requirements.",
        "- Extract only observable emulator behavior, formats, controls, and compatibility expectations.",
        "- Do not extract Vice-Sharp Technical Requirements from VICE design choices; derive TRs from Vice-Sharp architecture docs and interfaces.",
        "- Generated HTML/assets are excluded unless they contain non-duplicated text, such as keymap coverage notes.",
        "",
        "## Source Inventory",
        "",
        "| Source | Category | FR Extraction Disposition |",
        "|--------|----------|---------------------------|",
    ]
    for path in files:
        rel = path.relative_to(VICE_DOC).as_posix()
        category, note = source_notes.get(rel, ("Build/port/support documentation", "Reviewed; no distinct emulator FR extracted unless duplicated in `vice.texi`."))
        lines.append(f"| `native/vice/vice/doc/{rel}` | {category} | {note} |")
    write(SOURCES_DIR / "VICE-Source-Manifest.md", "\n".join(lines))


FR_INP_006 = r"""

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
"""


FR_HOST_UI_ADDITIONS = r"""

---

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
"""


FR_CFG = r"""# FR-Configuration-Resources: Configuration and Resource Functional Requirements

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Subsystem      | Configuration / Resources      |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-05-13                     |

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
5. Reset-plus-drive-8 autorun reports unsupported until the host implements the full autostart path.

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
"""


TR_DOCS = {
    "TR-Host-Status.md": r"""# TR-Host-Status: Host Runtime Telemetry Technical Requirement

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Quality Area   | Runtime Telemetry              |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-05-13                     |

---

## TR-HOST-STATUS-001: Measured Emulator Runtime Telemetry

**ID:** TR-HOST-STATUS-001
**Title:** Measured Emulator Runtime Telemetry
**Priority:** P0 -- Critical
**Category:** Observability

### Description

ViceSharp host status shall distinguish requested throttle settings from measured emulation output and measured emulated clock speed.

### Technical Specification

1. The host computes effective clock speed as rolling emulated cycles per real second.
2. Effective clock percent is effective clock speed divided by the active machine profile nominal clock.
3. Requested limiter rate is reported separately from measured FPS and effective clock speed.
4. Cycle, frame, PC, power state, and run state are sampled from host-owned session state.
5. Status polling must not mutate emulator state.

### Acceptance Criteria

1. Status responses include nominal clock Hz, effective clock Hz, effective clock percent, limiter rate percent, measured FPS, frame count, cycle, PC, power state, and run state.
2. Tests can verify limiter target remains stable while effective clock and FPS vary with execution.
3. Paused sessions report stable cycle/frame counters and paused run state.

### Verification Method

- Host status unit tests.
- gRPC status contract tests.
- UI status bar ViewModel tests with fake host status clients.

### Architecture Sources

- `docs/Architecture.md`: host/UI boundary and clock/timing sections.
- `src/ViceSharp.Host`: host-owned session/status services.
- `src/ViceSharp.Protocol`: generated status contract types.

### Related FRs

- FR-HOST-006
- FR-UI-002
- FR-CFG-008
""",
    "TR-Input-VKM.md": r"""# TR-Input-VKM: VICE Keymap Translation Technical Requirement

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Quality Area   | Input Translation              |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-05-13                     |

---

## TR-INPUT-VKM-001: VICE VKM Parser and Selected Map Resolver

**ID:** TR-INPUT-VKM-001
**Title:** VICE VKM Parser and Selected Map Resolver
**Priority:** P0 -- Critical
**Category:** Input

### Description

ViceSharp machine keyboard input shall resolve normalized host key events through a selected machine-specific VICE keymap before updating keyboard matrix state.

### Technical Specification

1. Keyboard map parsing is host-owned and session-scoped.
2. C64 support parses VICE VKM comments, `!CLEAR`, `!INCLUDE`, `!UNDEF`, modifier directives, row/column entries, and shift flags.
3. Custom uploaded keymaps are validated before becoming active.
4. The machine keyboard translator is abstracted so other machine profiles can provide different matrix/key handling.
5. Real-time key state changes update CIA keyboard matrix lines through machine input abstractions, not UI runtime references.

### Acceptance Criteria

1. Built-in and custom VKM maps produce deterministic key-to-matrix mappings.
2. Invalid maps return diagnostics without replacing the selected map.
3. Host input service tests prove selected map entries affect C64 keyboard matrix state.
4. The parser and resolver are usable without Avalonia dependencies.

### Verification Method

- VKM parser unit tests with VICE C64 maps.
- Input integration tests against C64 keyboard matrix/CIA behavior.
- gRPC input service tests for selected-map behavior.

### Architecture Sources

- `docs/Architecture.md`: host/UI boundary, input boundary, and library-first assembly rules.
- `src/ViceSharp.Abstractions`: keyboard map and machine keyboard abstractions.
- `src/ViceSharp.Chips/Input`: C64 VKM parser and keyboard matrix implementation.
- `src/ViceSharp.Host`: host input service.

### Related FRs

- FR-INP-001
- FR-INP-006
- FR-HOST-004
""",
    "TR-UI-Shell.md": r"""# TR-UI-Shell: Avalonia Emulator Control Shell Technical Requirement

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Quality Area   | UI Architecture                |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-05-13                     |

---

## TR-UI-SHELL-001: Avalonia Emulator Control Shell

**ID:** TR-UI-SHELL-001
**Title:** Avalonia Emulator Control Shell
**Priority:** P1 -- Important
**Category:** UI Architecture

### Description

The Avalonia shell shall present emulator controls through ViewModels and host-client abstractions while preserving the host-owned boundary for emulator state and local rendering.

### Technical Specification

1. ViewModels depend on abstractions or host client facades, not concrete emulator runtime devices.
2. The shell provides status bar, collapsible sidebar, Peripherals/Settings/Monitor tabs, and monitor dock/pop-out composition.
3. Local rendering may use a host-owned direct frame source only from the composition/render-surface layer.
4. Keyboard focus returns to the emulator display after normal controls unless monitor/text entry explicitly takes focus.
5. UI tests can fake host clients without starting the emulator runtime.

### Acceptance Criteria

1. Boundary tests fail if Avalonia ViewModels reference `ViceSharp.Core`, `ViceSharp.Chips`, or concrete architecture/device types.
2. ViewModel tests cover sidebar collapse, tab switching, attach state, settings state, VKM selection, status bar state, and monitor pop-out state.
3. UI startup succeeds while disconnected or while the in-process host is starting.

### Verification Method

- Avalonia ViewModel tests using fake host clients.
- Assembly/reference boundary tests.
- Local startup smoke test.

### Architecture Sources

- `docs/Architecture.md`: MVVM and host/UI boundary sections.
- `docs/requirements/technical/TR-MVVM.md`: strict ViewModel separation.
- `docs/requirements/technical/TR-GRPC-Boundary.md`: UI control boundary and local renderer exception.
- `src/ViceSharp.Avalonia`: shell, ViewModels, and render surface.

### Related FRs

- FR-UI-001
- FR-UI-002
- FR-UI-003
- FR-UI-004
""",
}


TESTS = [
    ("TEST-CPU-001", "CPU Execution Reference Tests", "CPU instruction, timing, interrupt, and port behavior is verified with unit tests and VICE/Lorenz-style reference comparisons.", "FR-CPU"),
    ("TEST-MEM-001", "Memory and Banking Tests", "Address decoding, RAM-under-ROM, Ultimax, VIC bank selection, color RAM, and stack/zero-page behavior are verified with unit and integration tests.", "FR-MEM"),
    ("TEST-VIC-001", "VIC-II Video Reference Tests", "Raster timing, display modes, sprites, collisions, badlines, borders, FLI/AFLI, banking, and DMA timing are verified with deterministic frame or trace comparisons.", "FR-VIC"),
    ("TEST-SID-001", "SID Audio Behavior Tests", "Oscillator, waveform, filter, ADSR, modulation, sync, noise, digi, external input, and multi-SID behavior are verified with deterministic audio/register tests.", "FR-SID"),
    ("TEST-CIA-001", "CIA and Keyboard Matrix Tests", "CIA timers, TOD, keyboard matrix, joystick interaction, serial shift register, IRQ, and NMI behavior are verified with unit and integration tests.", "FR-CIA"),
    ("TEST-VIA-001", "VIA Integration Tests", "VIA timer, shift register, port handshake, VIC-20, and drive integration behavior are verified with unit and machine integration tests.", "FR-VIA"),
    ("TEST-DRV-001", "Drive and IEC Tests", "Drive CPU/timing, image formats, GCR, IEC bus protocol, fast loader, and host media attachment behavior are verified with smoke, protocol, and reference tests.", "FR-DRV"),
    ("TEST-TAP-001", "Tape and Datasette Tests", "Datasette motor, TAP parsing, pulse timing, write behavior, and turbo loader compatibility are verified with unit and timing tests.", "FR-TAP"),
    ("TEST-CRT-001", "Cartridge Mapping Tests", "Standard, Ocean, EasyFlash, Action Replay, Retro Replay, and Final Cartridge mapping behavior is verified with cartridge image and banking tests.", "FR-CRT"),
    ("TEST-INPUT-001", "Input and VKM Tests", "Keyboard, joystick, mouse, lightpen, paddle, and VICE VKM behavior is verified with parser, matrix, protocol, and machine integration tests.", "FR-INP"),
    ("TEST-MED-001", "Media Capture Tests", "Screenshot, video, audio, synchronized capture, and format selection behavior are verified through capture metadata and round-trip output tests.", "FR-MED"),
    ("TEST-MON-001", "Monitor Tests", "Disassembly, memory display, breakpoints, register operations, bank selection, watch expressions, and monitor RPC behavior are verified through monitor engine tests.", "FR-MON"),
    ("TEST-SNP-001", "Snapshot and Replay Tests", "Save/load, deterministic replay, and state diff behavior are verified through round-trip and byte/trace comparison tests.", "FR-SNP"),
    ("TEST-PRF-001", "Machine Profile Tests", "C64, C64C, SX-64, C128, VIC-20, PET, Plus/4, and C16 profiles are verified for required devices, ROMs, clocks, and address maps.", "FR-PRF"),
    ("TEST-GRPC-001", "gRPC Boundary Tests", "Protocol, status, control, input, monitor, media, snapshot, capture, and boundary enforcement paths are verified through generated clients and host integration tests.", "FR-HOST, FR-UI"),
    ("TEST-HOST-001", "Host Service Tests", "Host lifecycle, status, media, state, capture, diagnostics, and session ownership behavior are verified with in-process host service tests.", "FR-HOST, FR-CFG"),
    ("TEST-UI-001", "Avalonia Shell ViewModel Tests", "Sidebar, status bar, attach panel, settings, keyboard map selection, monitor dock/pop-out, focus, and startup behavior are verified with fake host clients.", "FR-UI"),
    ("TEST-CFG-001", "Configuration and Resource Tests", "Resource files, ROM/romset selection, palettes, hotkeys, autostart, peripherals, RAM init, debug resources, and limiter settings are verified with configuration and host service tests.", "FR-CFG"),
]


def update_fr_docs() -> None:
    input_path = FR_DIR / "FR-Input.md"
    text = input_path.read_text(encoding="utf-8")
    if "FR-INP-006" not in text:
        text = text.rstrip() + FR_INP_006
    write(input_path, ensure_source_refs(replace_last_updated(text)))

    host_path = FR_DIR / "FR-Host-UI-Boundary.md"
    text = host_path.read_text(encoding="utf-8")
    text = text.replace("## FR-UI-001: Thin Host UI Control Client", "## FR-UI-001: Dockable Host UI Control Client")
    text = text.replace("**Title:** UI Control Consumes Emulator Host Through gRPC Boundary", "**Title:** Dockable Host UI Control Client")
    text = text.replace(
        "The UI control layer shall operate as a thin client of the emulator host.",
        "The UI control layer shall operate as a dockable thin client of the emulator host.",
    )
    if "FR-HOST-006" not in text:
        text = text.rstrip() + FR_HOST_UI_ADDITIONS
    write(host_path, ensure_source_refs(replace_last_updated(text)))

    write(FR_DIR / "FR-Configuration-Resources.md", ensure_source_refs(FR_CFG))
    for path in sorted(FR_DIR.glob("FR-*.md")):
        write(path, ensure_source_refs(replace_last_updated(path.read_text(encoding="utf-8"))))


def update_tr_docs() -> None:
    for name, text in TR_DOCS.items():
        write(TR_DIR / name, text)


def update_test_docs() -> None:
    lines = [
        "# TEST-Requirements: ViceSharp Test Requirements",
        "",
        "## Document Information",
        "",
        "| Field | Value |",
        "|-------|-------|",
        "| Project | ViceSharp |",
        "| Version | 0.1.0-draft |",
        f"| Last Updated | {TODAY} |",
        "| Status | Draft |",
        "",
        "## Purpose",
        "",
        "These test requirements define the verification conditions used to validate Functional Requirements ported from classic VICE documentation and Technical Requirements derived from the Vice-Sharp architecture.",
        "",
    ]
    for tid, title, condition, related in TESTS:
        lines += [
            "---",
            "",
            f"## {tid}: {title}",
            "",
            f"**ID:** {tid}",
            f"**Title:** {title}",
            "**Priority:** P1 -- Important",
            "",
            "### Condition",
            "",
            condition,
            "",
            "### Traceability",
            "",
            f"- **Related FR Area(s):** {related}",
            "",
        ]
    write(TEST_DIR / "TEST-Requirements.md", "\n".join(lines))


def parse_fr_docs() -> list[dict[str, str]]:
    rows = []
    for path in sorted(FR_DIR.glob("FR-*.md")):
        text = path.read_text(encoding="utf-8")
        sections = re.split(r"(?=^## FR-[A-Z0-9]+-\d{3}: )", text, flags=re.M)
        for section in sections[1:]:
            match = re.match(r"^## (FR-[A-Z0-9]+-\d{3}):\s*(.+)$", section, flags=re.M)
            if not match:
                continue
            title_match = re.search(r"^\*\*Title:\*\*\s*(.+)$", section, flags=re.M)
            iter_match = re.search(r"^\*\*Iteration:\*\*\s*(.+)$", section, flags=re.M)
            interface_match = re.search(r"^- \*\*Interfaces:\*\*\s*(.+)$", section, flags=re.M)
            rows.append(
                {
                    "id": match.group(1),
                    "heading": match.group(2).strip(),
                    "title": title_match.group(1).strip() if title_match else match.group(2).strip(),
                    "iteration": iter_match.group(1).strip() if iter_match else "TBD",
                    "interfaces": interface_match.group(1).strip() if interface_match else "TBD",
                    "file": path.name,
                    "body": section.strip(),
                }
            )
    return rows


def parse_tr_docs() -> list[dict[str, str]]:
    rows = []
    for path in sorted(TR_DIR.glob("TR-*.md")):
        text = path.read_text(encoding="utf-8")
        sections = re.split(r"(?=^## TR-[A-Z0-9]+(?:-[A-Z0-9]+)*-\d{3}: )", text, flags=re.M)
        for section in sections[1:]:
            match = re.match(r"^## (TR-[A-Z0-9]+(?:-[A-Z0-9]+)*-\d{3}):\s*(.+)$", section, flags=re.M)
            if not match:
                continue
            title_match = re.search(r"^\*\*Title:\*\*\s*(.+)$", section, flags=re.M)
            rows.append({"id": match.group(1), "title": title_match.group(1).strip() if title_match else match.group(2).strip(), "file": path.name, "body": section.strip()})
    return rows


def parse_test_docs() -> list[dict[str, str]]:
    rows = []
    for path in sorted(TEST_DIR.glob("TEST-*.md")):
        text = path.read_text(encoding="utf-8")
        sections = re.split(r"(?=^## TEST-[A-Z0-9]+-\d{3}: )", text, flags=re.M)
        for section in sections[1:]:
            match = re.match(r"^## (TEST-[A-Z0-9]+-\d{3}):\s*(.+)$", section, flags=re.M)
            if not match:
                continue
            title_match = re.search(r"^\*\*Title:\*\*\s*(.+)$", section, flags=re.M)
            rows.append({"id": match.group(1), "title": title_match.group(1).strip() if title_match else match.group(2).strip(), "condition": section.strip(), "file": path.name})
    return rows


SUBSYSTEMS = {
    "FR-Audio-SID.md": "Audio (SID)",
    "FR-Cartridges.md": "Cartridges",
    "FR-CPU-Emulation.md": "CPU",
    "FR-Configuration-Resources.md": "Configuration / Resources",
    "FR-Host-UI-Boundary.md": "Host / UI Boundary",
    "FR-Input.md": "Input Devices",
    "FR-IO-CIA.md": "I/O (CIA 6526)",
    "FR-IO-VIA.md": "I/O (VIA 6522)",
    "FR-Machine-Profiles.md": "Machine Profiles",
    "FR-Media-Capture.md": "Media Capture",
    "FR-Memory-System.md": "Memory",
    "FR-Monitor.md": "Machine Monitor",
    "FR-Snapshot.md": "Snapshot / Replay",
    "FR-Storage-Drives.md": "Disk Drives",
    "FR-Storage-Tape.md": "Tape / Datasette",
    "FR-Video-VIC-II.md": "Video (VIC-II)",
}


def subsystem_for(file: str) -> str:
    return SUBSYSTEMS.get(file, file.removesuffix(".md"))


def check_duplicates(rows: list[dict[str, str]], kind: str) -> None:
    counts = Counter(row["id"] for row in rows)
    dupes = [id_ for id_, count in counts.items() if count > 1]
    if dupes:
        raise SystemExit(f"Duplicate {kind} IDs: {', '.join(dupes)}")


def update_indexes() -> tuple[list[dict[str, str]], list[dict[str, str]], list[dict[str, str]]]:
    frs, trs, tests = parse_fr_docs(), parse_tr_docs(), parse_test_docs()
    check_duplicates(frs, "FR")
    check_duplicates(trs, "TR")
    check_duplicates(tests, "TEST")

    by_file: dict[str, list[dict[str, str]]] = defaultdict(list)
    for fr in frs:
        by_file[fr["file"]].append(fr)

    lines = [
        "# Functional Requirements Overview",
        "",
        "## Document Information",
        "",
        "| Field          | Value                                      |",
        "|----------------|--------------------------------------------|",
        "| Project        | ViceSharp                                  |",
        "| Version        | 0.1.0-draft                                |",
        f"| Last Updated   | {TODAY}                                 |",
        "| Status         | Draft                                      |",
        "",
        "## Purpose",
        "",
        "This document serves as the master index for all ViceSharp Functional Requirements (FRs). FRs define observable, testable emulator behavior. Classic VICE documentation is used as the source corpus for emulator-visible FR behavior; Vice-Sharp architecture documents constrain TRs and implementation boundaries.",
        "",
        "## FR Document Index",
        "",
        "| Document | Subsystem | FR Range |",
        "|----------|-----------|----------|",
    ]
    for file in sorted(by_file):
        ids = [item["id"] for item in sorted(by_file[file], key=lambda row: row["id"])]
        range_text = f"{ids[0]} .. {ids[-1]}" if len(ids) > 1 else ids[0]
        lines.append(f"| {file} | {subsystem_for(file)} | {range_text} |")
    lines += [
        "",
        "## Traceability Matrix -- FR to Interface to Iteration",
        "",
        "| FR ID | Title | Primary Interface(s) | Iteration |",
        "|-------|-------|----------------------|-----------|",
    ]
    for fr in sorted(frs, key=lambda row: row["id"]):
        lines.append(f"| {fr['id']} | {fr['title']} | {fr['interfaces']} | {fr['iteration']} |")
    lines += [
        "",
        "## Source Corpus",
        "",
        "- Primary FR source corpus: `native/vice/vice/doc`.",
        "- Detailed source inventory: `docs/requirements/sources/VICE-Source-Manifest.md`.",
        "- Technical and test requirements are derived from Vice-Sharp architecture and verification strategy, not from VICE implementation choices.",
    ]
    write(FR_DIR / "FR-Overview.md", "\n".join(lines))

    quality = {
        "TR-CYCLE-001": "Accuracy / Fidelity",
        "TR-AOT-001": "Deployment / Startup",
        "TR-ALLOC-001": "Performance / GC",
        "TR-SIMD-001": "Performance / Throughput",
        "TR-DET-001": "Correctness / Replay",
        "TR-STATE-001": "Reliability / Consistency",
        "TR-PUBSUB-001": "Performance / Messaging",
        "TR-PLAT-001": "Portability",
        "TR-LIB-001": "Architecture / Reuse",
        "TR-MVVM-001": "Architecture / UI",
        "TR-GRPC-BOUNDARY-001": "Architecture / Boundary",
        "TR-MEDIA-001": "Integration / AoT",
        "TR-BUILD-001": "CI/CD / Build",
        "TR-HOST-STATUS-001": "Runtime Telemetry",
        "TR-INPUT-VKM-001": "Input Translation",
        "TR-UI-SHELL-001": "UI Architecture",
    }
    by_tr_file: dict[str, list[dict[str, str]]] = defaultdict(list)
    for tr in trs:
        by_tr_file[tr["file"]].append(tr)
    lines = [
        "# Technical Requirements Overview",
        "",
        "## Document Information",
        "",
        "| Field          | Value                                      |",
        "|----------------|--------------------------------------------|",
        "| Project        | ViceSharp                                  |",
        "| Version        | 0.1.0-draft                                |",
        f"| Last Updated   | {TODAY}                                 |",
        "| Status         | Draft                                      |",
        "",
        "## Purpose",
        "",
        "This document indexes ViceSharp Technical Requirements (TRs). TRs define architecture constraints and non-functional qualities derived from Vice-Sharp architecture, not from classic VICE implementation choices.",
        "",
        "## TR Document Index",
        "",
        "| Document | Quality Attribute | TR ID(s) |",
        "|----------|-------------------|----------|",
    ]
    for file in sorted(by_tr_file):
        ids = ", ".join(row["id"] for row in sorted(by_tr_file[file], key=lambda row: row["id"]))
        attrs = ", ".join(sorted({quality.get(row["id"], "Architecture") for row in by_tr_file[file]}))
        lines.append(f"| {file} | {attrs} | {ids} |")
    lines += ["", "## Quality Attribute Summary", "", "| TR ID | Title | Quality Attribute |", "|-------|-------|-------------------|"]
    for tr in sorted(trs, key=lambda row: row["id"]):
        lines.append(f"| {tr['id']} | {tr['title']} | {quality.get(tr['id'], 'Architecture')} |")
    lines += [
        "",
        "## Architectural Constraints",
        "",
        "1. Target runtime: .NET 10 with NativeAOT publication profile.",
        "2. Emulator core remains library-first and UI-independent.",
        "3. UI control, media, input, state, capture, diagnostics, and monitor operations cross the host boundary through gRPC-backed abstractions.",
        "4. The local Avalonia renderer may use only a host-owned direct frame surface for in-process presentation; ViewModels must not access runtime internals.",
        "5. Hot-path emulation remains deterministic, low-allocation, and testable against reference traces.",
    ]
    write(TR_DIR / "TR-Overview.md", "\n".join(lines))

    lines = [
        "# FR-to-Interface Traceability Map",
        "",
        "## Document Information",
        "",
        "| Field | Value |",
        "|-------|-------|",
        "| Project | ViceSharp |",
        "| Version | 0.1.0-draft |",
        f"| Last Updated | {TODAY} |",
        "",
        "## Purpose",
        "",
        "This map links Functional Requirements to the ViceSharp interfaces or host service surfaces expected to satisfy them.",
        "",
    ]
    for file in sorted(by_file):
        lines += ["---", "", f"## {subsystem_for(file)}", "", "| FR ID | FR Title | Primary Interfaces |", "|-------|----------|--------------------|"]
        for fr in sorted(by_file[file], key=lambda row: row["id"]):
            lines.append(f"| {fr['id']} | {fr['title']} | {fr['interfaces']} |")
    counts = Counter()
    for fr in frs:
        for token in re.findall(r"`([^`]+)`", fr["interfaces"]):
            counts[token] += 1
    lines += ["", "---", "", "## Interface Coverage Summary", "", "| Interface | FR Count |", "|-----------|----------|"]
    for iface, count in sorted(counts.items(), key=lambda item: (-item[1], item[0])):
        lines.append(f"| `{iface}` | {count} |")
    write(TRACE_DIR / "FR-to-Interface-Map.md", "\n".join(lines))

    by_iter: dict[str, list[dict[str, str]]] = defaultdict(list)
    for fr in frs:
        by_iter[fr["iteration"]].append(fr)
    lines = [
        "# FR-to-Iteration Traceability Map",
        "",
        "## Document Information",
        "",
        "| Field | Value |",
        "|-------|-------|",
        "| Project | ViceSharp |",
        "| Version | 0.1.0-draft |",
        f"| Last Updated | {TODAY} |",
        "",
        "## Purpose",
        "",
        "This map groups Functional Requirements by planned implementation iteration.",
        "",
    ]
    def iter_sort(value: str) -> tuple[int, str]:
        match = re.search(r"\d+", value)
        return (int(match.group(0)) if match else 999, value)
    for iteration in sorted(by_iter, key=iter_sort):
        lines += ["---", "", f"## Iteration {iteration}", "", "| FR ID | Title | Subsystem |", "|-------|-------|-----------|"]
        for fr in sorted(by_iter[iteration], key=lambda row: row["id"]):
            lines.append(f"| {fr['id']} | {fr['title']} | {subsystem_for(fr['file'])} |")
    write(TRACE_DIR / "FR-to-Iteration-Map.md", "\n".join(lines))

    tr_decisions = {
        "TR-CYCLE-001": ["DD-CLK-001 | Half-cycle/bus-phase clocking remains the target fidelity model.", "DD-REF-001 | VICE x64sc/reference traces are accepted as behavioral comparison targets."],
        "TR-AOT-001": ["DD-AOT-001 | Use explicit registration/source generation over hot-path reflection."],
        "TR-ALLOC-001": ["DD-PERF-001 | Hot-path state favors structs, spans, and pooled buffers."],
        "TR-SIMD-001": ["DD-PERF-002 | Rendering/audio processing may use SIMD where it preserves determinism."],
        "TR-DET-001": ["DD-DET-001 | Same initial state plus same input sequence must produce bit-exact state/output."],
        "TR-STATE-001": ["DD-STATE-001 | Mutations and snapshots are host/core-owned and replayable."],
        "TR-PUBSUB-001": ["DD-MSG-001 | High-frequency device events use bounded zero-allocation messaging."],
        "TR-PLAT-001": ["DD-PLAT-001 | Supported shell/core targets remain Windows/Linux/macOS on x64/ARM64."],
        "TR-LIB-001": ["DD-ARCH-001 | Emulator core remains a reusable library with thin consumers."],
        "TR-MVVM-001": ["DD-UI-001 | Avalonia ViewModels depend on abstractions/client facades only."],
        "TR-GRPC-BOUNDARY-001": ["DD-HOST-001 | Host owns emulator sessions and all mutating control surfaces.", "DD-RENDER-001 | Local Avalonia rendering is a host-owned direct frame-source exception only."],
        "TR-MEDIA-001": ["DD-MEDIA-001 | Capture/encoding stays behind AoT-compatible media abstractions."],
        "TR-BUILD-001": ["DD-BLD-001 | Build/test validation uses the solution and repository conventions."],
        "TR-HOST-STATUS-001": ["DD-HOST-002 | Runtime telemetry separates requested limiter target from measured effective speed."],
        "TR-INPUT-VKM-001": ["DD-INP-001 | Machine-specific keyboard translation resolves selected VICE VKM maps before matrix mutation."],
        "TR-UI-SHELL-001": ["DD-UI-002 | Emulator controls live in a focused shell with status, tabs, monitor, and dockable panels."],
    }
    lines = [
        "# TR-to-Design-Decision Traceability Map",
        "",
        "## Document Information",
        "",
        "| Field | Value |",
        "|-------|-------|",
        "| Project | ViceSharp |",
        "| Version | 0.1.0-draft |",
        f"| Last Updated | {TODAY} |",
        "",
        "## Purpose",
        "",
        "This map links Technical Requirements to Vice-Sharp architecture decisions and source documents.",
        "",
    ]
    for tr in sorted(trs, key=lambda row: row["id"]):
        lines += ["---", "", f"## {tr['id']}: {tr['title']}", "", "| Decision ID | Decision |", "|-------------|----------|"]
        for item in tr_decisions.get(tr["id"], ["DD-ARCH-999 | Requirement follows current Vice-Sharp architecture documentation."]):
            decision_id, decision = item.split(" | ", 1)
            lines.append(f"| {decision_id} | {decision} |")
        lines += ["", "**Architecture Sources**", "", "- `docs/Architecture.md`", f"- `docs/requirements/technical/{tr['file']}`", ""]
    write(TRACE_DIR / "TR-to-Design-Decision-Map.md", "\n".join(lines))
    return frs, trs, tests


def mcp_connection() -> tuple[str, dict[str, str]]:
    marker = (ROOT / "AGENTS-README-FIRST.yaml").read_text(encoding="utf-8")
    base = re.search(r"(?m)^baseUrl:\s*(.+)$", marker)
    key = re.search(r"(?m)^apiKey:\s*(.+)$", marker)
    if not base or not key:
        raise SystemExit("AGENTS-README-FIRST.yaml is missing baseUrl/apiKey")
    return base.group(1).strip().rstrip("/"), {"X-Api-Key": key.group(1).strip()}


def http_json(method: str, url: str, headers: dict[str, str], payload: dict | None = None):
    data = None if payload is None else json.dumps(payload).encode("utf-8")
    req_headers = dict(headers)
    if payload is not None:
        req_headers["Content-Type"] = "application/json"
    req = request.Request(url, data=data, headers=req_headers, method=method)
    try:
        with request.urlopen(req, timeout=30) as response:
            body = response.read()
    except error.HTTPError as exc:
        details = exc.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"{method} {url} failed: {exc.code} {details}") from exc
    if not body:
        return None
    return json.loads(body.decode("utf-8"))


def upsert_collection(base: str, headers: dict[str, str], route: str, id_key: str, rows: list[dict], payload_builder) -> int:
    existing = {row[id_key] for row in http_json("GET", f"{base}{route}", headers)}
    count = 0
    for row in rows:
        payload = payload_builder(row)
        if row["id"] in existing:
            http_json("PUT", f"{base}{route}/{row['id']}", headers, payload)
        else:
            http_json("POST", f"{base}{route}", headers, {"id": row["id"], **payload})
        count += 1
    return count


def mapping_for(fr_id: str) -> tuple[list[str], list[str]]:
    tr_ids = ["TR-LIB-001"]
    test_ids = ["TEST-HOST-001"]
    if fr_id.startswith("FR-CPU"):
        tr_ids = ["TR-CYCLE-001", "TR-DET-001", "TR-ALLOC-001"]
        test_ids = ["TEST-CPU-001"]
    elif fr_id.startswith("FR-MEM"):
        tr_ids = ["TR-CYCLE-001", "TR-DET-001", "TR-STATE-001"]
        test_ids = ["TEST-MEM-001"]
    elif fr_id.startswith("FR-VIC"):
        tr_ids = ["TR-CYCLE-001", "TR-DET-001", "TR-SIMD-001"]
        test_ids = ["TEST-VIC-001"]
    elif fr_id.startswith("FR-SID"):
        tr_ids = ["TR-CYCLE-001", "TR-DET-001", "TR-SIMD-001"]
        test_ids = ["TEST-SID-001"]
    elif fr_id.startswith("FR-CIA"):
        tr_ids = ["TR-CYCLE-001", "TR-DET-001"]
        test_ids = ["TEST-CIA-001"]
    elif fr_id.startswith("FR-VIA"):
        tr_ids = ["TR-CYCLE-001", "TR-DET-001"]
        test_ids = ["TEST-VIA-001"]
    elif fr_id.startswith("FR-DRV"):
        tr_ids = ["TR-CYCLE-001", "TR-DET-001", "TR-GRPC-BOUNDARY-001"]
        test_ids = ["TEST-DRV-001", "TEST-HOST-001"]
    elif fr_id.startswith("FR-TAP"):
        tr_ids = ["TR-CYCLE-001", "TR-DET-001", "TR-GRPC-BOUNDARY-001"]
        test_ids = ["TEST-TAP-001", "TEST-HOST-001"]
    elif fr_id.startswith("FR-CRT"):
        tr_ids = ["TR-DET-001", "TR-STATE-001", "TR-GRPC-BOUNDARY-001"]
        test_ids = ["TEST-CRT-001", "TEST-HOST-001"]
    elif fr_id.startswith("FR-INP"):
        tr_ids = ["TR-DET-001", "TR-GRPC-BOUNDARY-001"]
        test_ids = ["TEST-INPUT-001"]
    elif fr_id.startswith("FR-MED"):
        tr_ids = ["TR-MEDIA-001", "TR-GRPC-BOUNDARY-001", "TR-AOT-001"]
        test_ids = ["TEST-MED-001", "TEST-HOST-001"]
    elif fr_id.startswith("FR-MON"):
        tr_ids = ["TR-GRPC-BOUNDARY-001", "TR-MVVM-001"]
        test_ids = ["TEST-MON-001", "TEST-GRPC-001"]
    elif fr_id.startswith("FR-SNP"):
        tr_ids = ["TR-STATE-001", "TR-DET-001", "TR-GRPC-BOUNDARY-001"]
        test_ids = ["TEST-SNP-001", "TEST-HOST-001"]
    elif fr_id.startswith("FR-PRF"):
        tr_ids = ["TR-LIB-001", "TR-PLAT-001", "TR-DET-001"]
        test_ids = ["TEST-PRF-001"]
    elif fr_id.startswith("FR-HOST"):
        tr_ids = ["TR-GRPC-BOUNDARY-001", "TR-LIB-001"]
        test_ids = ["TEST-HOST-001", "TEST-GRPC-001"]
    elif fr_id.startswith("FR-UI"):
        tr_ids = ["TR-UI-SHELL-001", "TR-MVVM-001", "TR-GRPC-BOUNDARY-001"]
        test_ids = ["TEST-UI-001", "TEST-GRPC-001"]
    elif fr_id.startswith("FR-CFG"):
        tr_ids = ["TR-STATE-001", "TR-GRPC-BOUNDARY-001", "TR-LIB-001"]
        test_ids = ["TEST-CFG-001", "TEST-HOST-001"]

    if fr_id in {"FR-INP-001", "FR-INP-006", "FR-HOST-004"}:
        tr_ids.append("TR-INPUT-VKM-001")
    if fr_id in {"FR-HOST-006", "FR-UI-002", "FR-CFG-008"}:
        tr_ids.append("TR-HOST-STATUS-001")
    if fr_id.startswith("FR-UI"):
        tr_ids.append("TR-UI-SHELL-001")
    return sorted(set(tr_ids)), sorted(set(test_ids))


def sync_mcp(frs: list[dict[str, str]], trs: list[dict[str, str]], tests: list[dict[str, str]]) -> None:
    base, headers = mcp_connection()
    fr_count = upsert_collection(
        base,
        headers,
        "/mcpserver/requirements/fr",
        "id",
        frs,
        lambda row: {"title": row["title"], "body": row["body"]},
    )
    tr_count = upsert_collection(
        base,
        headers,
        "/mcpserver/requirements/tr",
        "id",
        trs,
        lambda row: {"title": row["title"], "body": row["body"]},
    )
    test_count = upsert_collection(
        base,
        headers,
        "/mcpserver/requirements/test",
        "id",
        tests,
        lambda row: {"condition": row["condition"]},
    )
    for fr in frs:
        tr_ids, test_ids = mapping_for(fr["id"])
        http_json("PUT", f"{base}/mcpserver/requirements/mapping/{fr['id']}", headers, {"trIds": tr_ids, "testIds": test_ids})

    try:
        todo = http_json("GET", f"{base}/mcpserver/todo/UI-CTRL-001", headers)
    except RuntimeError:
        todo = None
    if todo:
        fr_links = list(dict.fromkeys(todo.get("functionalRequirements") or []))
        if "FR-INP-003" in fr_links:
            fr_links = ["FR-INP-006" if item == "FR-INP-003" else item for item in fr_links]
        if "FR-INP-006" not in fr_links:
            fr_links.append("FR-INP-006")
        http_json("PUT", f"{base}/mcpserver/todo/UI-CTRL-001", headers, {"functionalRequirements": fr_links})

    live_frs = http_json("GET", f"{base}/mcpserver/requirements/fr", headers)
    live_trs = http_json("GET", f"{base}/mcpserver/requirements/tr", headers)
    live_tests = http_json("GET", f"{base}/mcpserver/requirements/test", headers)
    live_maps = http_json("GET", f"{base}/mcpserver/requirements/mapping", headers)
    print(
        json.dumps(
            {
                "upserted": {"FR": fr_count, "TR": tr_count, "TEST": test_count, "Mappings": len(frs)},
                "live": {"FR": len(live_frs), "TR": len(live_trs), "TEST": len(live_tests), "Mappings": len(live_maps)},
                "hasFR_INP_006": any(row.get("id") == "FR-INP-006" for row in live_frs),
                "frInp003Title": next((row.get("title") for row in live_frs if row.get("id") == "FR-INP-003"), None),
            },
            indent=2,
        )
    )


def main() -> None:
    parser = argparse.ArgumentParser(description="Port classic VICE docs into Vice-Sharp requirement artifacts.")
    parser.add_argument("--sync-mcp", action="store_true", help="Upsert parsed FR/TR/TEST requirements and mappings into MCP Server.")
    args = parser.parse_args()
    generate_source_manifest()
    update_fr_docs()
    update_tr_docs()
    update_test_docs()
    frs, trs, tests = update_indexes()
    print(f"Generated VICE source manifest, {len(frs)} FRs, {len(trs)} TRs, and {len(tests)} TEST requirements.")
    if args.sync_mcp:
        sync_mcp(frs, trs, tests)


if __name__ == "__main__":
    main()
