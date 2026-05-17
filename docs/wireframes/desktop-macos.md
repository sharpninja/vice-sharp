# Desktop - MacOS (Avalonia 12)

Tracking: `PLATFORM-CROSS-001` Phase 1. Reference: [desktop-windows.md](desktop-windows.md).

## Goals

Keep the desktop UI feature-identical to Windows while honoring MacOS conventions:

- Application menu bar lives in the system menu bar (top of screen), not in the window.
- Window controls are traffic lights at top-left.
- Full-screen uses MacOS's split-screen-friendly fullscreen.
- Sheets replace modal dialogs.
- `Cmd` replaces `Ctrl` for shortcuts.

## Window Chrome

- Native MacOS chrome with extended title bar so the toolbar can sit beside the traffic lights when collapsed.
- Default size: 1120 x 720; minimum: 780 x 520.
- Title bar shows `ViceSharp - <attached media name or "No Media">`.
- Supports unified title-and-toolbar style (Avalonia's `ExtendClientAreaToDecorationsHint`).

## Menu Bar (System)

```
[Apple] [ViceSharp] [File] [Emulator] [Monitor] [View] [Window] [Help]
```

| Menu | Items |
|------|-------|
| ViceSharp | About ViceSharp, Preferences (Cmd+,), Hide ViceSharp (Cmd+H), Quit (Cmd+Q) |
| File | Open Disk... (Cmd+O), Open Tape..., Open Cartridge..., Open Snapshot..., Recent, Detach All |
| Emulator | Pause (Cmd+P), Resume, Step Cycle (Cmd+.), Step Frame (Shift+Cmd+.), Rewind Cycle, Rewind Frame, Cold Reset (Cmd+R), Warm Reset (Shift+Cmd+R), Autostart Drive 8 |
| Monitor | Show Monitor (Cmd+M), Detach Monitor, Show Trace, Show Memory, Show Registers |
| View | Toggle Sidebar (Cmd+\\), Dock Sidebar Left, Dock Sidebar Right, Toggle Full Screen (Ctrl+Cmd+F), Zoom 1x / 2x / 3x |
| Window | Minimize, Zoom, Bring All to Front |
| Help | ViceSharp Help, VICE Documentation, Report an Issue |

## Layout (expanded sidebar, MacOS)

```
+------------------------------------------------------------------------------+
|*o o o   ViceSharp - mygame.d64                                               |  <-- traffic lights + unified title
+----------+----+--------------------------------------------------------------+
| Attach | Monitor | Settings  |                                               |  <-- segmented control replaces tabs
| ---------------------------- |                                               |
| Drive 8                      |                                               |
|   mygame.d64        [X]      |                                               |
| Drive 9            [Browse]  |                                               |
| Datasette          [Browse]  |                  VideoSurface                  |
| Cartridge          [Browse]  |                (letterboxed)                   |
| ----------                   |                                               |
| Keyboard Map                 |                                               |
|   c64-pos-us.vkm    [Edit]   |                                               |
|                              |                                               |
| Dock: < O >                  |                                               |
+----+-------------------------+-----------------------------------------------+
|  Power On | Run Running | Limiter 100% | FPS 50.0 | Clock 0.985 MHz | ...    |
|                                                  [|>][II][>|][>>|][R][RR][8] |
+------------------------------------------------------------------------------+
```

Notes:

- MacOS uses a segmented control across the sidebar header instead of inline buttons.
- The dock-side picker uses a 2-state segmented widget (left / right).
- Status-bar transport buttons use SF-Symbol-style glyphs: play, pause, step, frame-step, rewind, cold reset, warm reset, autostart-8.

## Full Screen

When the user toggles full screen, the menu bar autohides per MacOS convention, the sidebar collapses to a hover-revealed overlay, and `VideoSurface` fills the entire display, still aspect-correct with letterboxing.

```
+------------------------------------------------------------------------------+
|                                                                              |
|                                                                              |
|                                                                              |
|                        VideoSurface (full screen)                            |
|                                                                              |
|                                                                              |
|                                                                              |
|   [hover reveals overlay]                                                    |
|   +------+------+--------------------------------------+                     |
|   | Attach| Monitor | Pause | Step | Reset | Exit FS  |                     |
|   +------+------+--------------------------------------+                     |
+------------------------------------------------------------------------------+
```

## Monitor (pop-out, MacOS)

The monitor opens as a separate window with the standard MacOS chrome:

```
+----------------------------------------------------+
|*o o o   Monitor - mygame.d64                       |
+--+----+--------+-------+--------+------------------+
|R | M  | Trace  | Disasm| Watch  |                  |  <-- toolbar with toggleable panes
+--+----+--------+-------+--------+------------------+
|                                                    |
| PC=E5CD A=00 X=00 Y=00 SP=F3 P=nv-Bdizc            |
|                                                    |
| .E5CD  A9 00      LDA #$00                         |
| .E5CF  85 D0      STA $D0                          |
| ...                                                |
|                                                    |
+----------------------------------------------------+
| > _                                                |
+----------------------------------------------------+
```

The monitor honors `Cmd+Shift+L` to clear, `Cmd+F` to search the trace, `Cmd+G` to find next.

## Settings Sheet

Triggered from `ViceSharp > Preferences...` (Cmd+,). Opens as a sheet attached to the main window.

```
+------------------------------------------------------+
| Preferences                                  [Done]  |
+------------------------------------------------------+
| [General] [Video] [Audio] [Input] [ROMs] [Advanced]  |
+------------------------------------------------------+
|                                                      |
|  Limiter rate:  [====O==========]  100%              |
|                                                      |
|  Pause on focus loss:        [v]                     |
|  Autostart on attach:        [ ]                     |
|  Confirm cold reset:         [v]                     |
|                                                      |
+------------------------------------------------------+
```

## Control Affordances by Input

| Input | Behavior |
|-------|----------|
| Mouse click on `VideoSurface` | Focuses the surface; subsequent key events forward. |
| Trackpad two-finger swipe | Reserved for VICE's optional 1351 mouse mode. |
| Trackpad two-finger tap | Toggle sidebar visibility. |
| Magic Mouse scroll | Scroll inside monitor panes only. |
| Hardware keyboard | Mapped via `ToHostKeyName`; `Cmd` shortcuts intercepted by menus. |
| Caps-lock | Forwarded as ShiftLock-equivalent if the loaded keymap requests it. |

## Platform-Specific Considerations

| Concern | MacOS approach |
|---------|----------------|
| File picker | `OpenFilePickerAsync` already maps to NSOpenPanel via Avalonia; default to `~/Documents/ViceSharp`. |
| Sandboxing | If shipped via Mac App Store, document the entitlements needed (com.apple.security.files.user-selected.read-write). |
| Notarization | Required for direct distribution; tracked separately. |
| Retina displays | `VideoSurface` already uses `WriteableBitmap` at native DPI; verify integer scaling looks clean on @2x and @3x. |
| Dark mode | Already handled by `FluentTheme RequestedThemeVariant="Default"`. |
| Touch Bar | Surface Pause / Step / Reset on the Touch Bar when present. |

## Shortcut Map (MacOS)

| Shortcut | Action |
|----------|--------|
| Cmd+O | Open Disk (Drive 8) |
| Cmd+P | Pause |
| Cmd+R | Warm reset |
| Shift+Cmd+R | Cold reset |
| Cmd+. | Step cycle |
| Shift+Cmd+. | Step frame |
| Cmd+M | Pop out / show monitor |
| Cmd+, | Preferences |
| Cmd+\\ | Toggle sidebar |
| Ctrl+Cmd+F | Toggle full screen |
| Cmd+Q | Quit |

## What Differs from Windows

- Menu bar in system menu rather than window.
- Sidebar tabs use segmented control instead of stacked buttons.
- Preferences open as sheet, not separate window.
- Shortcut prefixes use `Cmd` instead of `Ctrl`.
- Traffic lights instead of min/max/close.
- Touch Bar surfaces transport controls on supported hardware.
