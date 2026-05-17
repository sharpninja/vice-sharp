# Mobile - Portrait (Avalonia 12 mobile)

Tracking: `PLATFORM-CROSS-001` Phase 1. Reference: [desktop-windows.md](desktop-windows.md).

Portrait is the **library / setup orientation**: configure media, browse files, edit settings, review monitor output. The actual gameplay surface is small; users are expected to rotate to landscape (see [mobile-landscape.md](mobile-landscape.md)) for play.

## Target Devices

- Android phones >= 6.0" (primary target).
- Android tablets in portrait (uses same layout, looser margins).
- iPhones >= iOS 17 (Avalonia 12 mobile target).
- iPads in portrait (same layout, looser margins).

## Window Chrome

- Full-screen single-window app; uses platform safe-area insets.
- Status bar / notch / home indicator respected via Avalonia 12 mobile `SafeAreaPadding`.
- No window controls (mobile platforms manage app lifecycle).

## Screen List in Portrait

| Screen | Surfaced as |
|--------|-------------|
| Machine view | Top third of the screen (video letterboxed). |
| Transport controls | Strip beneath video. |
| Bottom navigation | Tab bar: Attach / Monitor / Settings / Keyboard. |
| Active tab content | Middle of screen, scrollable. |
| Status bar | Compact line above tab bar showing FPS / Clock / Power dot. |

## Layout (Attach tab visible)

```
+------------------------------+
|       [safe area inset]      |
+------------------------------+
|                              |
|        VideoSurface          |
|     (letterboxed, ~30%       |
|      of vertical space)      |
|                              |
+------------------------------+
| [II] [|>] [>|] [>>] [R] [RR] |  <-- transport strip
+------------------------------+
|  Power . | FPS 50 | 0.985MHz |  <-- compact status
+------------------------------+
|                              |
|  Attach Media                |
|  -----------                 |
|                              |
|  Drive 8                     |
|   mygame.d64    [Detach]     |
|  Drive 9                     |
|   --              [Browse]   |
|  Datasette                   |
|   --              [Browse]   |
|  Cartridge                   |
|   --              [Browse]   |
|                              |
|  Keyboard Map                |
|   c64-pos-us.vkm  [Change]   |
|                              |
|                              |
|                              |
+------------------------------+
| [Attach][Monitor][Set][Keys] |  <-- bottom nav (always visible)
+------------------------------+
|       [home indicator]       |
+------------------------------+
```

## Layout (Monitor tab visible)

```
+------------------------------+
|        VideoSurface          |
+------------------------------+
| [II] [|>] [>|] [>>] [R] [RR] |
+------------------------------+
|  Power . | FPS 50 | 0.985MHz |
+------------------------------+
|  PC=E5CD A=00 X=00 Y=00      |
|  SP=F3 P=nv-Bdizc            |
|                              |
|  .E5CD A9 00   LDA #$00      |
|  .E5CF 85 D0   STA $D0       |
|  .E5D1 A2 00   LDX #$00      |
|  .E5D3 9D 00 03 STA $0300,X  |
|  ...                         |
|                              |
| +--------------------------+ |
| | > _                       | |  <-- monitor command input
| +--------------------------+ |
+------------------------------+
| [Attach][Monitor][Set][Keys] |
+------------------------------+
```

## Layout (Settings tab visible)

```
+------------------------------+
|        VideoSurface          |
+------------------------------+
| [II] [|>] [>|] [>>] [R] [RR] |
+------------------------------+
|  Power . | FPS 50 | 0.985MHz |
+------------------------------+
|  Settings                    |
|  --------                    |
|                              |
|  Speed limiter               |
|   [---O-------]  100%        |
|                              |
|  Audio                       |
|   Enabled            [ON  ]  |
|   Volume [-----O-----] 80%   |
|                              |
|  Display                     |
|   Scaling: [Integer v]       |
|   Aspect:  [PAL 4:3 v]       |
|                              |
|  Behavior                    |
|   Pause on background  [ON]  |
|   Haptic step          [OFF] |
|                              |
+------------------------------+
| [Attach][Monitor][Set][Keys] |
+------------------------------+
```

## Layout (Keys tab visible - on-screen keyboard)

```
+------------------------------+
|        VideoSurface          |
+------------------------------+
| [II] [|>] [>|] [>>] [R] [RR] |
+------------------------------+
|  Keymap: c64-pos-us [Change] |
+------------------------------+
|  [1][2][3][4][5][6][7][8][9] |
|  [Q][W][E][R][T][Y][U][I][O] |
|  [A][S][D][F][G][H][J][K][L] |
|  [Shf][Z][X][C][V][B][N][M]  |
|  [CTRL][   SPACE   ][C=][RUN]|
+------------------------------+
| [Attach][Monitor][Set][Keys] |
+------------------------------+
```

The on-screen keyboard is **modal-less**: typing forwards into the emulator while everything else (status, transport) remains live.

## Navigation Flow

```
   Machine view header is always visible
              |
        bottom nav:
   +----+------+-----+------+
   |Attach|Mon |Set  |Keys  |
   +-+--+--+---+--+--+--+---+
     |     |      |     |
     v     v      v     v
   (panes scroll; nav stays anchored)

   Long-press transport buttons -> shows label tooltip
   Swipe down on status bar -> shows expanded status sheet
   Pinch on VideoSurface -> reserved (zoom disabled to preserve aspect)
   Double-tap VideoSurface -> request landscape (rotate prompt)
```

## Control Affordances by Input

| Input | Behavior |
|-------|----------|
| Tap on tab | Switch active tab. |
| Tap on transport button | Send corresponding RPC. |
| Tap on on-screen key | `SetKeyStateAsync(pressed)` immediately; `released` on touch-up. |
| Drag across keys | Allows chord-typing (Shift + key) without finger jugglery. |
| Long-press on key | Lock as held (toggles `isPressed`). |
| Two-finger tap on VideoSurface | Pause / resume toggle. |
| System back gesture | Pops navigation; from Machine view, prompts quit confirmation. |
| Bluetooth keyboard | Routed exactly like desktop keyboard via `ToHostKeyName`. |
| External controller (MFi / Xbox / DualSense) | Routes to control ports (joystick 1 by default). |

## Platform-Specific Considerations

| Concern | Mobile approach |
|---------|-----------------|
| File picker | Platform document picker (Android SAF / iOS Files); copies into app sandbox before attach. |
| ROM acquisition | First-launch onboarding offers `RomFetch` flow or user-supplied path. |
| Background lifecycle | Auto-pause on backgrounding; resume on foreground if user toggled "Resume on return". |
| Battery | Default speed limiter to 100% to avoid burning battery; warn if user pushes to 200%+. |
| Audio routing | Honor system audio session category (playback). |
| Haptics | Optional haptic tick on Step Cycle / Step Frame. |
| Notch / dynamic island | All chrome respects safe area; video does not. |

## What Differs from Desktop

- No sidebar dock; bottom nav tab bar replaces it.
- No pop-out monitor; monitor is a tab.
- No menu bar; settings is a tab.
- Status bar is single-line and below the video.
- Touch is the primary input; on-screen keyboard is the default keyboard path.
