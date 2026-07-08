# Avalonia UI Redesign - Byrd Development Process v4 Plan

Status: COMPLETE (follow-ups tracked as PLAN-DRVTRUE-002). Branch base: `069d28c` (codex/iec-timetravel-debugger).

## Implementation status (2026-06-16, honest scope after adversarial review)

Verified by automated tests (32 VM/factory/routing tests + the committed
true-drive LOAD test, all green) and a 4-reviewer adversarial pass:

- **S0 (prereq):** XmlDocsConventionTests ratchet back to 0 (11 pre-existing
  violations documented). Done.
- **S1 Shell:** `MainWindow.axaml` declarative shell - Menu + bottom transport
  bar + `SplitView` flyout; single `⇄` side-toggle (Left/Right buttons gone);
  VM `IsPaneOpen`/`ToggleSidebar`/`ToggleDockSide`/`PanePlacement` tested. Done
  (AXAML render itself only via the manual app-launch gate, not run here).
- **S2 PeripheralCardView:** reusable AXAML card renders every slot via an
  `ItemsControl`; rendered in-app inside the `AttachPanelView` peripherals tab.
  The imperative bulk has since been dismantled: `AttachPanelView` now lives at
  `src/ViceSharp.Avalonia/Views/AttachPanelView.cs` and is under 300 lines. The
  dedicated `SidebarView` extraction is still NOT done (only the placeholder
  comment in `MainWindow.axaml.cs` marks it).
- **S3 SettingsView:** reusable AXAML settings UserControl, bound, rendered in
  the settings tab; old imperative settings cluster deleted. Done.
- **S4 True Drive + LED + runtime gating:** end-to-end for drive 8/9 (single
  true-drive at a time).
  - Done: per-drive True Drive toggle (card + menu) now switches a running
    session - `true_drive`/`true_drive_device` flow through the proto +
    `CreateEmulatorSessionRequest` + the gRPC adapter; `DefaultEmulatorRuntimeFactory.Create(request)`
    honors `request.TrueDrive` and builds `C64TrueDriveRigBuilder` (coordinator
    C64 + emulated 1541). Update 2026-06-25/26: Drive 8 now defaults to the
    VICE-faithful true-drive path (`AttachPanelViewModel` constructs the Drive8
    slot with `trueDrive: true`); default-off survives only as the
    persisted-restore override;
    `GrpcHostProtocolClient.SetTrueDriveAsync` recreates the session with the
    selection; `AttachPanelViewModel` observes the per-drive toggle and enforces
    single true-drive. Verified end-to-end through a live in-process gRPC host
    (`SetTrueDriveAsync_RecreatesSessionOverProtocol`) plus the rig LOAD
    (`TrueDriveLoadTests`); rig session monitors the live IEC bus.
  - Remaining (PLAN-DRVTRUE-002): faithful VIA2 PB3 LED telemetry (the LED is
    still the IEC-activity proxy; `SetDriveLed` has no producer); two
    simultaneous true-drives (only one rig at a time); the app-launch visual gate.
- **S5 Menu commands:** `ShellViewModel` routes transport + menu actions to the
  host/VM (mock-tested); menu structure is a representative subset of the VICE
  x64sc layout (Snapshot load/save, datasette, cartridge, drive 10/11 not yet).

Defects found in review and fixed: Warp checkbox staleness after load/revert
(derived `IsWarpMode` now notified); true-drive rig activity monitor watched the
host's unused bus (now monitors the rig's live bus).

## Goal

Migrate the Avalonia UI from imperative C# (`MainWindow.axaml.cs` ~502 lines,
`AttachPanelView.cs` ~863 lines) to declarative AXAML + MVVM, restructured as:
a flyout sidebar with a single side-toggle, reusable per-peripheral UserControls,
a settings UserControl, a VICE-x64sc-style menu bar, and the per-drive True Drive
toggle + activity LED.

## BDPv4 application

1. **Requirements drive tests.** FR/TR/TEST captured in MCP before code (below).
2. **Tests first.** Each slice's automated tests are written and made to pass
   against view-model logic/bindings with mocked host clients **before** the
   real AXAML/runtime wiring.
3. **Validate with mocks.** VM/command/binding behavior is proven with mock
   `IHostProtocolClient` / fake services first.
4. **Then implement.** AXAML + runtime wiring added to satisfy the green tests.
5. **All tests green to exit a phase.** Full suite 0 failed / 0 skipped before
   starting the next slice.

**UI test-strategy note (honest scope).** AXAML *render* correctness cannot be
unit-tested - it is validated by an explicit **app-launch visual gate** per slice
(build + run + screenshot). The automated tests-first cover everything testable:
view-model state, command routing (mocked host), data-template binding contracts,
and runtime behavior (True Drive gating). Each slice lists both gates.

## Requirements (MCP, source of truth)

- TR-UIAXAML-VIEWS-001 - all views in AXAML + MVVM (umbrella technical requirement).
- FR-UIFLYOUT-001 / TEST-UIFLYOUT-001 - flyout sidebar + single side-toggle.
- FR-UIPERIPHERAL-001 / TEST-UIPERIPHERAL-001 - reusable per-peripheral UserControl.
- FR-UISETTINGS-001 / TEST-UISETTINGS-001 - settings UserControl.
- FR-UIMENUBAR-001 / TEST-UIMENUBAR-001 - VICE-style menu bar.
- FR-DRVTRUE-001 / TEST-DRVTRUE-001 - per-drive True Drive toggle + runtime gating.
- FR-DRVLED-001 / TEST-DRVLED-001 - per-drive activity LED (VIA2 PB3, done at VM level).

## Component target (all AXAML)

- `MainWindow.axaml` - DockPanel: `Menu` (top) + transport/status bar (bottom) +
  `SplitView` flyout (pane = SidebarView, content = video surface).
- `SidebarView` UserControl - tab strip + keyboard-map block + `ItemsControl` of
  peripheral cards.
- `PeripheralCardView` UserControl - one reusable control bound to
  `AttachSlotViewModel` (status, RO, activity LED, True Drive toggle [drives],
  Attach/Eject, Recent).
- `SettingsView` UserControl - bound to the settings VM.

## Slices (tests-first, gated; each exits only at full-suite green)

### S1 - Shell: menu + flyout + single toggle (FR-UIFLYOUT-001, FR-UIMENUBAR-001 skeleton)
- VM first: add `IsPaneOpen` + a `ToggleDockSide()` to the panel/shell VM.
- **Red tests (TEST-UIFLYOUT-001):** `ToggleDockSide_FlipsLeftRight`,
  `ToggleSidebar_TogglesIsPaneOpen`, default state. Write before wiring.
- **Green:** VM tests pass with the new members.
- **Implement:** `MainWindow.axaml` = Menu + SplitView (PanePlacement bound to
  DockSide, IsPaneOpen bound), single side-toggle icon button (no Left/Right),
  hosting the existing panel + video; trim the imperative shell code.
- **Validation:** build; VM tests green; full suite 0/0; **app-launch:** flyout
  opens/closes, docks both sides via one icon, menu bar present.
- **Acceptance:** Left/Right buttons gone; one icon toggles side; sidebar is a
  flyout; menu bar renders.

### S2 - PeripheralCardView UserControl (FR-UIPERIPHERAL-001)
- **Red tests (TEST-UIPERIPHERAL-001):** assert `AttachSlotViewModel` exposes the
  card's binding surface (status/RO/activity/TrueDrive/SupportsTrueDrive) and
  Attach/Eject route to a mock host; one template renders all slot kinds.
- **Implement:** `PeripheralCardView.axaml` + `SidebarView` `ItemsControl` with it
  as `DataTemplate`; remove the per-slot imperative builder.
- **Validation:** build; tests green; full suite 0/0; **app-launch:** Drive8/9/
  Tape/Cartridge all render via the one control with correct controls.
- **Acceptance:** single reusable control renders every peripheral.

### S3 - SettingsView UserControl (FR-UISETTINGS-001)
- **Red tests (TEST-UISETTINGS-001):** settings VM selection + apply against mocks.
- **Implement:** `SettingsView.axaml` bound to the settings VM.
- **Validation:** build; tests green; full suite 0/0; **app-launch:** settings
  render + apply.

### S4 - True Drive toggle + LED + runtime gating (FR-DRVTRUE-001, FR-DRVLED-001)
- **Red tests (TEST-DRVTRUE-001):** `TrueDrive` defaults off; with it on the
  runtime builds the coordinator true-drive path and LOAD works; with it off the
  simulated drive is used and existing media/activity tests stay green.
- **Implement:** card toggle + LED bound to `TrueDrive`/`LedOn`; re-introduce the
  gated factory true-drive (default off) so there is no blast radius.
- **Validation:** build; tests green; **full suite + lockstep gate 0/0** (parity);
  **app-launch:** toggle a drive to True Drive, attach a D64, LOAD works; LED lights.
- **Acceptance:** per-drive True Drive toggle switches simulated/emulated; LOAD
  works when on; suite + native parity green.

### S5 - Menu commands fill-out (FR-UIMENUBAR-001)
- **Red tests (TEST-UIMENUBAR-001):** menu command handlers invoke the existing
  VM actions/host services (attach/eject, reset, snapshot, warp, true-drive,
  swap joysticks) against mocks.
- **Implement:** bind the full menu structure.
- **Validation:** build; tests green; full suite 0/0; **app-launch:** menu matches
  the VICE-style structure and commands work.

## Menu structure (VICE x64sc GTK-style)

- File: Smart attach…; Attach disk ▸ (8/9/10/11); Detach disk ▸; Attach/Detach
  tape; Datasette ▸ (Play/Stop/Rewind/Record); Attach/Detach cartridge;
  Reset ▸ (Soft/Hard); Exit.
- Snapshot: Load/Save…; Quickload/Quicksave; Media recording ▸ (Screenshot/Audio/Video).
- Settings: Settings…; Machine/Drive/Audio/Video/Input; ☑ Warp; per-drive ☑ True
  Drive; Swap joysticks.
- Debug: Monitor; Pause/Resume; Step cycle/frame.
- Help: About.

## Risks

- AXAML render correctness needs the app-launch gate every slice - no shortcut.
- S4 re-introduces the factory true-drive; keep it default-off/gated so the
  media/activity tests (ISSUE-2) and native lockstep stay green.
