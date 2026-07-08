# ViceSharp Cross-Platform Wireframes

Tracking TODO: `PLATFORM-CROSS-001`. The host project scaffolds have since landed in the solution (`src/ViceSharp.Host.Xbox`, `src/ViceSharp.Host.Android`, `src/ViceSharp.Host.iOS`, `src/ViceSharp.Host.MacOS`); these wireframes remain the UX contract those hosts implement.

This directory captures the host UI surface ViceSharp must present on every supported platform. It is the design reference for the platform hosts:

- Xbox One / Xbox Series host (Avalonia shell today; UWP is a documented future switch)
- Avalonia 12 mobile host (Android + iOS)
- MacOS support for the existing Avalonia desktop host

The current Avalonia 12 desktop UI is the canonical reference: any feature visible there must reach every host, even if its presentation changes radically.

## Index

| File | Purpose |
|------|---------|
| [desktop-windows.md](desktop-windows.md) | Current Avalonia desktop on Windows (reference state). |
| [desktop-macos.md](desktop-macos.md) | Same screens adapted to MacOS conventions. |
| [mobile-portrait.md](mobile-portrait.md) | Avalonia 12 mobile, portrait orientation. |
| [mobile-landscape.md](mobile-landscape.md) | Avalonia 12 mobile, landscape orientation (gameplay). |
| [xbox.md](xbox.md) | UWP/Xbox 10-foot UI with gamepad-first navigation. |

## Canonical Screen Inventory

Every host must surface these screens, even if they are folded into a single pane or hidden behind a menu:

1. **Machine view** - the emulated video output (`VideoSurface`, 384x272 PAL @ 4:3) plus a status bar (Power, Run, Limiter, FPS, Clock, Cycle, PC, IEC).
2. **Attach / media picker** - select disk (D64/D71/D81/G64), tape (TAP/T64), cartridge (CRT/raw 8K/16K), snapshot (VSF) for each slot (Drive 8/9/10/11, Datasette, Cartridge).
3. **Monitor** - command-line debugger over the host's gRPC `IHostProtocolClient`; supports registers, memory window, disassembly trace, step/run/breakpoints.
4. **Settings** - clock limiter ratio, video filter, audio output, keyboard map (`.vkm`), controller layout, ROM paths.
5. **Keyboard mapping** - load/inspect VICE `.vkm` map; preview matrix.
6. **Status bar / transport controls** - Pause, Resume, +1 cyc, +1 frm, -1 cyc, -1 frm, Cold reset, Warm reset, Run 8 (autostart drive 8).

## Universal Input Contract

The host abstraction (`IHostProtocolClient.SetKeyStateAsync`) accepts a logical key name plus modifiers. Every platform must map its native input to this contract:

| Input modality | Source events | Mapping target |
|----------------|---------------|----------------|
| Hardware keyboard | KeyDown / KeyUp | `SetKeyStateAsync(key, isPressed, physicalKey, text, modifiers)` |
| On-screen keyboard (touch) | Pointer press/release on key glyph | same as hardware keyboard |
| Touch joystick | Virtual stick / fire button | Control-port state via `SetJoyStateAsync` (planned) |
| Gamepad | Xbox / DualSense / generic HID | Control-port state + face-button menu nav |
| Mouse / trackpad / touch tap | Pointer events on `VideoSurface` | Focus video; pass through to optional 1351 mouse emulation |

## Cross-Platform UX Principles

1. **Video first.** The emulator surface is the focal point of every host. Chrome must never crowd it; on small screens chrome collapses into overlays.
2. **Aspect-ratio preserved.** All hosts letterbox/pillarbox to keep 384:272 (4:3 effective) intact. No stretching.
3. **Single source of truth for state.** All hosts read status and issue commands through `IHostProtocolClient`; never duplicate emulator logic in UI code.
4. **Pop-out monitor.** On every host except gamepad-only, the monitor can detach into a secondary surface (window / sheet / split-view / second display).
5. **Keyboard-first authoring, gamepad-first leisure.** Desktop assumes keyboard + mouse; mobile assumes touch; Xbox assumes gamepad. Each host re-orders the toolbar to match.
6. **Accessibility.** Tab order and focus visuals follow platform conventions. No control depends on color alone. Touch targets are >=44pt on mobile, >=72px on TV.
7. **Save state continuity.** Pause / resume / cold-reset / warm-reset are surfaced consistently and bound to consistent shortcuts (Esc menu / Start / two-finger tap depending on platform).

## Navigation Flow (shared)

```
              +------------------+
              |  Machine view    |  <-- default landing screen
              |  (video + bar)   |
              +---+----+----+----+
                  |    |    |
            Attach|  Monitor|  Settings
                  v    v    v
        +---------+----+----+----------+
        | Attach  | Monitor| Settings  |
        | / Media | / Trace| / Keyboard|
        +---------+--------+-----------+
                  ^
                  | back / Esc / B button
                  +-> Machine view (always reachable)
```

## Status Bar Reference

The Avalonia desktop status bar is the canonical text layout; every host reproduces the same fields (truncated as space allows):

```
Power Off | Run Stopped | Limiter 100% | FPS 50.0 | Clock 0.985 MHz (100%) | Cycle 0 | PC 0000 | IEC Idle
```

On multi-CPU topologies (a true-drive rig or the C128's two CPUs) the line appends per-CPU effective clock rates, e.g. `| CPUs C64 100%, 1541 100%`.

## Out of Scope (Phase 1)

- Host project scaffolding (UWP `.csproj`, Avalonia Mobile `.csproj`, MacOS bundling). (Since landed: the four host scaffolds now exist under `src/`.)
- Concrete XAML / AXAML / SwiftUI markup.
- ViewModel implementations.
- Asset production (icons, splash screens, app-store metadata).

The remaining items land in the platform-specific TODOs.
