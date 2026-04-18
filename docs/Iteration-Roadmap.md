# ViceSharp Iteration Roadmap

## Iteration 0 — Foundations ✅ COMPLETED

**Goal:** Runnable scaffolding with zero emulation.

- Solution structure and build system (Nuke)
- 33+ public interfaces in ViceSharp.Abstractions
- Roslyn source generator for device registration
- ROM fetch tool
- CI/CD pipelines (Azure DevOps + GitHub Actions)
- Comprehensive documentation and GraphRAG knowledge base
- Determinism test harness (empty machine, bit-exact snapshots)

**Exit criteria:** `dotnet test` passes, NativeAOT console app starts and exits cleanly, all docs written.

## Iteration 1 — C64 (MVP)

**Goal:** Playable Commodore 64 emulation.

- MOS 6510 CPU — full instruction set, cycle-accurate
- VIC-II (6567/6569) — raster engine, sprites, border, badlines, DMA stealing
- SID (6581/8580) — 3 voices, filters, envelope, ring mod, sync
- CIA x2 (6526) — timers, TOD, keyboard matrix, joystick, IEC serial
- PLA (906114) — memory banking, address decoding
- 1541 drive emulation — GCR, IEC bus, DOS
- Datasette — TAP playback, motor control
- Cartridge support — Ocean, EasyFlash, Action Replay, Final Cartridge III
- Keyboard, joystick, mouse, lightpen input
- Media capture — screenshots (PNG), video (MP4 via FFmpeg), audio (WAV)
- Monitor/debugger — disassembly, breakpoints, memory view, watch
- Save/load state snapshots
- Avalonia desktop UI

**Exit criteria:** Loads and runs commercial C64 software, passes Klaus Dormann test suite, SID audio plays correctly, save/load state round-trips deterministically.

## Iteration 2 — VIC-20

**Goal:** Add VIC-20 as a second architecture.

- MOS 6502 CPU (reuse 6510 core minus I/O port)
- VIC (6560/6561) video chip
- VIA x2 (6522) — replaces CIA
- 5KB base RAM + expansion addressing
- VIC-20 cartridge types
- VIC-20 keyboard matrix

**Exit criteria:** Runs VIC-20 software, architecture switching works at runtime.

## Iteration 3 — C128

**Goal:** Add Commodore 128 with dual-CPU support.

- MOS 8502 CPU (2 MHz mode)
- Z80 coprocessor (CP/M mode)
- VIC-IIe (6569 superset with 2 MHz support)
- VDC (8563) — 80-column display
- MMU — extended banking, shared RAM
- 1571/1581 drive support
- C128/C64 mode switching

**Exit criteria:** Boots in C128 mode, switches to C64 mode, CP/M mode starts.

## Iteration 4 — PET

**Goal:** Add Commodore PET family.

- MOS 6502 CPU
- PIA (6520) and VIA (6522) I/O
- CRTC (6545) video controller
- IEEE-488 bus
- PET keyboard (business/graphics)
- PET 2001, 3032, 4032, 8032 variants

## Iteration 5 — Plus/4 and C16

**Goal:** Add TED-based machines.

- MOS 7501/8501 CPU
- TED (7360/8360) — combined video+audio+timer
- Plus/4 built-in software ROM
- C16 reduced memory model

## Future Iterations

- **Performance:** SIMD rendering, JIT CPU core, GPU-accelerated video
- **Networking:** RS-232 emulation, TCP/IP stack (RR-Net/TFE)
- **Peripherals:** REU, GeoRAM, IDE64, SD2IEC
- **Platform:** WebAssembly target, mobile (MAUI)
- **Community:** Plugin system, Lua scripting, ROM repository integration
