# Desktop - Windows (Avalonia 12) - Reference State

Tracking: `PLATFORM-CROSS-001` Phase 1. Source: `src/ViceSharp.Avalonia/MainWindow.axaml.cs`.

This document captures the current Avalonia desktop UI as the reference for every other host. It is descriptive, not aspirational; if the running app contradicts this doc, the doc is wrong.

## Window Chrome

- Native Win32 chrome (Windows acrylic when enabled).
- Default size: 1120 x 720; minimum: 780 x 520.
- Title: `ViceSharp`.

## Screen List

| Screen | Surfaced as |
|--------|-------------|
| Machine view | The primary `VideoSurface` (384x272 BGRA) filling the main column. |
| Attach panel | Dockable sidebar (left or right, 300 px expanded / 44 px collapsed). |
| Monitor | Detachable via `Pop Out` button -> secondary `Window` (680 x 520). |
| Settings | Reachable via the attach panel header (Settings tab). |
| Keyboard map picker | `OpenFilePickerAsync` triggered from attach panel "Load .vkm" button. |
| Status bar | Bottom dock, full window width, ~34 px tall. |

## Layout (expanded sidebar, dock-left)

```
+------------------------------------------------------------------------------+
| ViceSharp                                                          - [] X    |  <-- Win32 title bar
+----------+----+--------------------------------------------------------------+
|  Sidebar | || |                                                              |
|          |    |                                                              |
| [Hamburger]   |                                                              |
| Attach        |                                                              |
| ----------    |                                                              |
| Drive 8: ___  |                                                              |
| [Browse]      |                                                              |
| Drive 9: ___  |                              VideoSurface                    |
| [Browse]      |                            (384 x 272 PAL,                   |
| Tape:    ___  |                          letterboxed to fit)                 |
| [Browse]      |                                                              |
| Cart:    ___  |                                                              |
| [Browse]      |                                                              |
|               |                                                              |
| Dock: (L) (R) |                                                              |
|               |                                                              |
| [Monitor v]   |                                                              |
| [Settings v]  |                                                              |
| [Pop Out]     |                                                              |
+---------------+--------------------------------------------------------------+
| Power On | Run Running | Limiter 100% | FPS 50.0 | Clock 0.985 MHz (100%)... |
|                                            [Pause][Resume][+1cyc][+1frm]...  |
+------------------------------------------------------------------------------+
```

## Layout (collapsed sidebar)

```
+------------------------------------------------------------------------------+
| ViceSharp                                                          - [] X    |
+----+-------------------------------------------------------------------------+
|    |                                                                         |
| == |                                                                         |
|    |                                                                         |
|    |                       VideoSurface (full column)                        |
|    |                                                                         |
|    |                                                                         |
+----+-------------------------------------------------------------------------+
| Power On | Run Running | Limiter 100% | ... | [Pause][Resume][+1cyc][+1frm]  |
+------------------------------------------------------------------------------+
```

The `==` glyph re-expands the sidebar. Dock side toggle moves the column to the opposite edge without losing state.

## Monitor Window (pop-out)

```
+----------------------------------------------------+
| ViceSharp Monitor                       - [] X     |
+----------------------------------------------------+
| PC=E5CD A=00 X=00 Y=00 SP=F3 P=nv-Bdizc            |
|                                                    |
| .E5CD  A9 00      LDA #$00                         |
| .E5CF  85 D0      STA $D0                          |
| .E5D1  A2 00      LDX #$00                         |
| ...                                                |
|                                                    |
+----------------------------------------------------+
| > _                                                |  <-- command prompt
+----------------------------------------------------+
```

## Navigation Flow

```
Machine view (default)
  |
  +-- hamburger -> collapse / expand sidebar
  |
  +-- sidebar > Attach tab
  |     +-- Browse Drive 8/9/10/11 -> OpenFilePickerAsync (.d64/.d71/.d81/.g64)
  |     +-- Browse Datasette       -> OpenFilePickerAsync (.tap/.t64)
  |     +-- Browse Cartridge       -> OpenFilePickerAsync (.crt/.bin)
  |     +-- Load .vkm              -> OpenFilePickerAsync (.vkm)
  |
  +-- sidebar > Monitor tab
  |     +-- Pop Out -> secondary Window with full monitor
  |
  +-- sidebar > Settings tab
  |
  +-- status bar buttons (always visible)
        Pause | Resume | +1 cyc | +1 frm | -1 cyc | -1 frm | Cold | Warm | Run 8
```

## Control Affordances by Input

| Input | Behavior |
|-------|----------|
| Mouse click on `VideoSurface` | Focuses the surface so subsequent key events reach the emulator. |
| Mouse click on status button | Sends the corresponding RPC (Pause / Step / Reset / etc.). |
| Mouse drag on `GridSplitter` | Resizes sidebar vs. video column. |
| Keyboard (focused video) | Mapped via `ToHostKeyName` and forwarded as `SetKeyStateAsync`. |
| Alt+F4 | Closes window (not forwarded to emulator). |
| Tab | Walks focus through sidebar controls, then status buttons. |

## Keyboard Shortcuts (planned, not yet bound)

| Shortcut | Action |
|----------|--------|
| F5 | Resume |
| Shift+F5 | Pause |
| F11 | Step instruction |
| F10 | Step frame |
| Shift+F11 | Rewind 1 cycle |
| Shift+F10 | Rewind 1 frame |
| Ctrl+R | Warm reset |
| Ctrl+Shift+R | Cold reset |
| Ctrl+O | Open media (Drive 8 default) |
| Ctrl+M | Pop out monitor |
| Esc | Toggle sidebar |

## What Carries Over to Other Hosts

- The screen list (machine view, attach, monitor, settings, keyboard, status).
- The status bar field set and ordering.
- The single-`VideoSurface` aspect-preserved rendering pipeline.
- The `IHostProtocolClient` contract for all commands.

Things that do not carry over verbatim:

- Sidebar docking (mobile has no sidebar; Xbox has tab strip).
- File-picker chrome (replaced by sheets / content pickers).
- Pop-out monitor (becomes split view / sheet on mobile).
