# Xbox - UWP (10-foot UI, gamepad-first)

Tracking: `PLATFORM-CROSS-001` Phase 1. Reference: [desktop-windows.md](desktop-windows.md).

Xbox One and Xbox Series host the ViceSharp shell as a **UWP** app (Xbox doesn't yet support WinUI 3 or full .NET 10 desktop). The runtime constraint is therefore:

- UI framework: UWP XAML (10-foot styled, high-contrast safe).
- Runtime: UWP CoreCLR (no NativeAOT on Xbox today).
- Input: Gamepad-first; USB keyboard / mouse / chatpad supported; touch not applicable.
- Memory budget: Game mode grants ~5 GB (Series X) / 1 GB (One S) - we ship as a "general purpose" app for now, capped at ~1 GB to keep certification cheap.

The UWP project is **deferred** to a follow-up TODO; this document only describes the UX it must deliver.

## Window Chrome

- Full-screen by default. UWP on Xbox does not show a window frame.
- TV-safe area: 5% inset on all sides (HDTV overscan tolerance). All chrome respects this inset.
- High-contrast theme honored.

## Screen List

| Screen | Surfaced as |
|--------|-------------|
| Home | Tile grid: Resume Last, Library, Settings, Monitor, Exit. |
| Machine view | Video-only with overlay status. |
| Library | Disk/tape/cartridge file picker, scoped to known folders + USB. |
| Monitor | Read-only register/disasm/trace overlay; full keyboard required for command input. |
| Settings | Big-text settings list with radio groups and sliders. |
| Keyboard map | List of `.vkm` files in known locations. |

## Home Screen (focus on Resume)

```
+-----------------------------------------------------------------------------+
|                            ViceSharp                                        |
|                                                                             |
|     +----------+    +----------+    +----------+    +----------+            |
|     | Resume   |    | Library  |    | Settings |    | Monitor  |            |
|     |          |    |          |    |          |    |          |            |
|     | mygame   |    | Browse   |    | Speed,   |    | Debugger |            |
|     | .d64     |    | media    |    | input    |    | tools    |            |
|     +----------+    +----------+    +----------+    +----------+            |
|                                                                             |
|     +----------+                                                            |
|     | Exit     |                                                            |
|     |          |                                                            |
|     +----------+                                                            |
|                                                                             |
|   (A) Open       (B) Back       (Y) Quick Pause       (Menu) Help           |
+-----------------------------------------------------------------------------+
```

## Machine View

```
+-----------------------------------------------------------------------------+
|                                                                             |
|                                                                             |
|                                                                             |
|                          VideoSurface                                       |
|                 (letterboxed to 4:3 inside TV-safe area)                    |
|                                                                             |
|                                                                             |
|                                                                             |
|                                                                             |
|  +-----------------------------------------------------------------------+  |
|  | Power On | Run Running | 50.0 FPS | 0.985 MHz | Cycle 12345 | PC E5CD|  |  <-- auto-hide after 5s
|  +-----------------------------------------------------------------------+  |
|  (A) Pause/Resume   (X) Cold Reset   (Y) Monitor   (LB)(RB) Step   (Menu)   |
+-----------------------------------------------------------------------------+
```

## In-Game Menu (Menu button)

```
+-----------------------------------------------------------------------------+
|                                                                             |
|                                                                             |
|              +--------------------------------------+                       |
|              |  Resume                              |  <-- focus            |
|              |  Pause                               |                       |
|              |  Step Cycle                          |                       |
|              |  Step Frame                          |                       |
|              |  Rewind 1 Cycle                      |                       |
|              |  Rewind 1 Frame                      |                       |
|              |  Warm Reset                          |                       |
|              |  Cold Reset                          |                       |
|              |  Save State                          |                       |
|              |  Load State                          |                       |
|              |  Library...                          |                       |
|              |  Settings...                         |                       |
|              |  Monitor...                          |                       |
|              |  Exit to Home                        |                       |
|              +--------------------------------------+                       |
|                                                                             |
|   (A) Select   (B) Close   (LS) Scroll                                      |
+-----------------------------------------------------------------------------+
```

## Library Screen

```
+-----------------------------------------------------------------------------+
| Library                                                                     |
+-----------------------------------------------------------------------------+
| Source: [Local] [OneDrive] [USB]            Filter: [Disks] [Tapes] [Carts] |
+-----------------------------------------------------------------------------+
|                                                                             |
|  +---------+ +---------+ +---------+ +---------+ +---------+ +---------+    |
|  |         | |         | |         | |         | |         | |         |   |
|  | game1   | | game2   | | demo    | | utility | | sid     | | snapsh  |   |
|  | .d64    | | .d64    | | .tap    | | .crt    | | .vsf    | | .vsf    |   |
|  +---------+ +---------+ +---------+ +---------+ +---------+ +---------+    |
|                                                                             |
|  +---------+ +---------+                                                    |
|  | game7   | | game8   |   ...                                              |
|  +---------+ +---------+                                                    |
|                                                                             |
|  Selected: game1.d64                                                        |
|  Slot: [Drive 8 v]                                                          |
|                                                                             |
|   (A) Attach   (B) Back   (Y) Attach + Autostart   (X) Detach All           |
+-----------------------------------------------------------------------------+
```

## Settings Screen

```
+-----------------------------------------------------------------------------+
| Settings                                                                    |
+-----------------------------------------------------------------------------+
|                                                                             |
|  Speed limiter         [---O----] 100%                                      |
|                                                                             |
|  Video filter          (o) None   ( ) CRT-Lite   ( ) CRT-Full               |
|                                                                             |
|  Audio                 [ON  ]    Volume [-----O-] 80%                       |
|                                                                             |
|  Controller layout     [Default v]                                          |
|                                                                             |
|  Keyboard map          c64-pos-us.vkm     [Change]                          |
|                                                                             |
|  ROM paths             ROM root: D:\ViceSharp\roms\   [Change]              |
|                                                                             |
|  Behavior              [v] Pause on Guide button                            |
|                        [v] Auto-save snapshot on Exit                       |
|                        [ ] Show debug overlay                               |
|                                                                             |
|   (A) Toggle / Edit   (B) Back   (X) Reset to Defaults                      |
+-----------------------------------------------------------------------------+
```

## Monitor Screen

```
+-----------------------------------------------------------------------------+
| Monitor                                                                     |
+-----------------------------------------------------------------------------+
| Registers                                                                   |
|   PC=E5CD  A=00  X=00  Y=00  SP=F3  P=nv-Bdizc                              |
|                                                                             |
| Disassembly                                                                 |
|   .E5CD A9 00      LDA #$00                                                 |
|   .E5CF 85 D0      STA $D0                                                  |
|   .E5D1 A2 00      LDX #$00                                                 |
|   .E5D3 9D 00 03   STA $0300,X                                              |
|   .E5D6 E8         INX                                                      |
|                                                                             |
| Trace tail                                                                  |
|   12345 .E5CD LDA  #$00                                                     |
|   12347 .E5CF STA  $D0                                                      |
|   12350 .E5D1 LDX  #$00                                                     |
|                                                                             |
| Command (requires USB keyboard)                                             |
|   > _                                                                       |
|                                                                             |
|   (A) Step   (X) Step Frame   (B) Back   (LB)(RB) Scroll                    |
+-----------------------------------------------------------------------------+
```

## Gamepad Mapping (Default)

| Gamepad input | Action |
|---------------|--------|
| Left stick | Joystick port 1 direction |
| Right stick | Reserved (planned 1351 mouse mode) |
| D-Pad | Joystick port 1 direction (alternative) |
| A | Fire 1 / menu select |
| B | Fire 2 / back |
| X | Cold reset (confirm) / settings cancel |
| Y | Open monitor / menu accept |
| LB | Step cycle |
| RB | Step frame |
| LT | Rewind cycle (hold to repeat) |
| RT | Rewind frame (hold to repeat) |
| View | Toggle status overlay |
| Menu | Open in-game menu |
| Left stick click | Toggle on-screen keyboard |
| Right stick click | Quick save snapshot |
| Guide (system) | OS-level pause; honored by `Pause on Guide button` setting |

Joystick port assignment (which physical gamepad maps to which emulated control port) is editable in Settings.

## On-Screen Keyboard (gamepad-driven)

```
+-----------------------------------------------------------------------------+
|              +---+---+---+---+---+---+---+---+---+---+                      |
|              | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 0 |                      |
|              +---+---+---+---+---+---+---+---+---+---+                      |
|              | Q | W | E | R | T | Y | U | I | O | P |                      |
|              +---+---+---+---+---+---+---+---+---+---+                      |
|              | A | S | D | F | G | H | J | K | L | : |                      |
|              +---+---+---+---+---+---+---+---+---+---+                      |
|              | Z | X | C | V | B | N | M | , | . | / |                      |
|              +---+---+---+---+---+---+---+---+---+---+                      |
|              | Shift | Ctrl |   Space   | C= | Run/Stop|                    |
|              +-------+------+-----------+----+--------+                     |
|                                                                             |
|   (A) Press   (B) Close   (X) Shift   (Y) Toggle Caps                       |
+-----------------------------------------------------------------------------+
```

Selection moves with D-Pad / Left stick. Press-and-hold A locks the key down until released. LB / RB nudge focus between key rows; LT / RT page between alt key panes (e.g., Commodore-key alternates).

## Navigation Flow

```
Home tile grid
  +-- Resume   -> Machine view (last attached media)
  +-- Library  -> Library screen
  |               +-- A on tile -> select; A again -> Attach
  +-- Settings -> Settings screen
  +-- Monitor  -> Monitor screen
  +-- Exit     -> confirm + close app

Machine view
  +-- Menu button -> in-game menu
  |                  +-- Library, Settings, Monitor, Save State, etc.
  +-- Y button    -> pop monitor overlay
  +-- LS click    -> on-screen keyboard
  +-- B (from menu) -> close menu
  +-- B (from machine view) -> confirm exit to Home
```

## Control Affordances by Input

| Input | Behavior |
|-------|----------|
| Gamepad | Primary input; every action accessible via gamepad alone. |
| USB keyboard | Forwards to emulator with same `ToHostKeyName` mapping as desktop. |
| USB mouse | Optional 1351 mouse emulation if user enables. |
| Chatpad / smart-keyboard | Treated as USB keyboard. |
| Voice (Kinect / headset) | Not used. |

## Platform-Specific Considerations

| Concern | Xbox / UWP approach |
|---------|---------------------|
| Certification | Avoid unsupported P/Invoke; use only WACK-approved APIs. |
| Storage | Use `Windows.Storage` for local + `KnownFolders` for USB; never raw `File.IO`. |
| ROMs | Surface a documented "Drop ROMs into `LocalState\roms\`" flow; show first-launch helper. |
| Anti-cheat / DRM | Not applicable. |
| Networking | Optional only (e.g., snapshot sync). Must remain offline-functional. |
| Achievements | Out of scope. |
| Performance | Target locked 50 / 60 FPS depending on emulated machine; no allocation in render loop. |
| Memory | Cap allocations at ~512 MB to leave headroom on Xbox One S. |

## What Differs from Desktop / Mobile

- No window chrome; everything full-screen.
- Gamepad replaces mouse + keyboard as the primary input.
- Navigation is button-driven; no hover states, no right-click.
- Monitor command-line requires an external keyboard (otherwise read-only).
- Library replaces the file-picker dialog with a tile grid.
- The in-game menu (Menu button) replaces the desktop's status-bar buttons.
