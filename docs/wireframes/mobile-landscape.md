# Mobile - Landscape (Avalonia 12 mobile)

Tracking: `PLATFORM-CROSS-001` Phase 1. Reference: [mobile-portrait.md](mobile-portrait.md).

Landscape is the **gameplay orientation**. Maximize the video surface; collapse all chrome to overlays that auto-hide; surface touch joystick affordances on either side of the screen.

## Target Devices

- Phones in landscape (primary gameplay target).
- Tablets in landscape (more chrome space; same model with thicker side rails).

## Window Chrome

- Full-screen, immersive (Android `IMMERSIVE_STICKY` / iOS `prefersHomeIndicatorAutoHidden`).
- Safe-area insets respected on each side (notch on left when rotated).

## Layout (touch joystick mode, default)

```
+--------+----------------------------------------------------------+--------+
| safe   |                                                          | safe   |
| area   |                                                          | area   |
+--------+----------------------------------------------------------+--------+
|        |                                                          |        |
|        |                                                          |        |
|        |                                                          |  o  o  |
|        |                                                          | o[F]o  |  <-- fire button cluster (right)
|        |                                                          |  o  o  |
| [JoyP] |              VideoSurface (full height,                  |        |
|  /-\   |             letterbox/pillarbox as needed)               | [Pause]|
| | O |  |                                                          | [Step] |
|  \-/   |                                                          | [Esc]  |
|        |                                                          |        |
|        |                                                          |        |
+--------+----------------------------------------------------------+--------+
|  [Menu reveal arrow ^]                                                     |  <-- swipe up to show menu
+----------------------------------------------------------------------------+
```

Idle-hide rule: after 3 seconds of no touch outside the video region, the joystick / fire cluster / right-side controls fade to ~25% opacity. They snap back to full opacity on any touch.

## Layout (keyboard mode, on-screen)

```
+--------+----------------------------------------------------------+--------+
|        |                                                          | [Pause]|
|        |              VideoSurface (top half)                     | [Step] |
|        |                                                          | [Esc]  |
+--------+----------------------------------------------------------+--------+
| [1][2][3][4][5][6][7][8][9][0][+][-][CLR][INST/DEL]                        |
| [CTRL][Q][W][E][R][T][Y][U][I][O][P][@][*][UP-arrow][RESTORE]              |
| [RUNSTOP][SHIFTLOCK][A][S][D][F][G][H][J][K][L][:][;][=][RETURN]           |
| [C=][SHIFT][Z][X][C][V][B][N][M][,][.][/][SHIFT][CRSR-UD][CRSR-LR]         |
|                       [             SPACE             ]                    |
+----------------------------------------------------------------------------+
```

Toggle between joystick and keyboard mode with a single tap on the bottom-right "kbd" / "joy" toggle that appears with the menu reveal.

## Layout (menu revealed)

Swipe up from bottom edge or tap the small chevron to reveal a translucent overlay:

```
+--------+----------------------------------------------------------+--------+
|        |                                                          |        |
|        |              VideoSurface dims by ~40%                   |        |
|        |                                                          |        |
+--------+----------------------------------------------------------+--------+
| [Attach] [Monitor] [Settings] [Keys] [Joy/Kbd] [Portrait] [Exit]           |
+----------------------------------------------------------------------------+
```

Each menu button opens a half-sheet over the video; tapping outside or on the video dismisses the sheet and resumes immersive mode.

## Attach half-sheet (over landscape)

```
+----------------------------------------------------------------------------+
|                                  (video dimmed)                            |
+--------------------+-------------------------------------------------------+
| Attach Media                                                       [X]     |
+----------------------------------------------------------------------------+
|  Drive 8        mygame.d64                                  [Detach]       |
|  Drive 9        --                                          [Browse]       |
|  Datasette      --                                          [Browse]       |
|  Cartridge      --                                          [Browse]       |
|                                                                            |
|  Snapshot       --                          [Save State]  [Load State]     |
+----------------------------------------------------------------------------+
```

## Monitor half-sheet (over landscape)

```
+----------------------------------------------------------------------------+
| Monitor                                                            [X]     |
+----------------------------------------------------------------------------+
| PC=E5CD A=00 X=00 Y=00 SP=F3 P=nv-Bdizc                                    |
|                                                                            |
| .E5CD A9 00      LDA #$00                                                  |
| .E5CF 85 D0      STA $D0                                                   |
| .E5D1 A2 00      LDX #$00                                                  |
| .E5D3 9D 00 03   STA $0300,X                                               |
|                                                                            |
| > _                                                                        |
+----------------------------------------------------------------------------+
```

## Navigation Flow

```
Immersive gameplay (default)
  +-- swipe up from bottom or tap chevron -> menu overlay
  |     +-- Attach    -> half-sheet
  |     +-- Monitor   -> half-sheet
  |     +-- Settings  -> half-sheet
  |     +-- Keys      -> on-screen keyboard mode toggle
  |     +-- Joy/Kbd   -> toggle touch joystick vs keyboard
  |     +-- Portrait  -> request rotation lock to portrait
  |     +-- Exit      -> confirm and exit
  |
  +-- tap on left thumb area -> show / move virtual joystick anchor
  +-- tap on right thumb area -> tap fire button(s)
  +-- two-finger tap on video -> pause / resume toggle
  +-- three-finger tap on video -> quick save state
```

## Control Affordances by Input

| Input | Behavior |
|-------|----------|
| Drag on left virtual joystick | Drives joystick port 1 direction state. |
| Tap on right fire cluster | Fire 1 / Fire 2 / Auto-fire (long-press) for port 1. |
| Tap on on-screen key | Sends keypress for as long as held. |
| Two-finger tap | Pause / resume. |
| Three-finger tap | Quick save snapshot. |
| Pinch | Reserved (no zoom; aspect preserved). |
| External controller | Replaces virtual joystick; keyboard mode auto-disables. |
| Bluetooth keyboard | Replaces on-screen keyboard automatically. |
| Apple Pencil / S-Pen | Treated as pointer; can drive 1351 mouse mode if enabled. |

## Platform-Specific Considerations

| Concern | Mobile landscape approach |
|---------|---------------------------|
| Notch / camera | All overlays clip to safe area; video can extend behind notch on iOS if user opts in. |
| Rotation lock | If user locks portrait at OS level, app stays in portrait layout. |
| Haptics | Joystick edge-hit haptic; fire button click haptic; optional Cycle/Frame step haptic. |
| Battery | Limiter is honored; default to native speed; warn at 200%+. |
| Audio | Music apps continue when user backgrounds; pause emulator. |
| Picture-in-picture | iPad-only: PiP the video while user uses other apps; emulator continues if user allows. |

## What Differs from Portrait

- Video is the dominant surface; chrome is overlay-only.
- Virtual joystick is the default input, not the on-screen keyboard.
- Menu is hidden by default; revealed on swipe.
- Tabs are replaced by half-sheets that don't displace the video.
