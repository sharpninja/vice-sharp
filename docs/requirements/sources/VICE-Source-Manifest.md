# VICE Source Manifest

## Document Information

| Field | Value |
|-------|-------|
| Source Corpus | `native/vice/vice/doc` |
| Generated | 2026-05-13 |
| Purpose | Inventory classic VICE documentation reviewed for Functional Requirement extraction. |

## Extraction Rules

- Use classic VICE docs only for Functional Requirements.
- Extract only observable emulator behavior, formats, controls, and compatibility expectations.
- Do not extract Vice-Sharp Technical Requirements from VICE design choices; derive TRs from Vice-Sharp architecture docs and interfaces.
- Generated HTML/assets are excluded unless they contain non-duplicated text, such as keymap coverage notes.

## Source Inventory

| Source | Category | FR Extraction Disposition |
|--------|----------|---------------------------|
| `native/vice/vice/doc/building/CMake-Howto.txt` | Build/port/support documentation | Reviewed; no distinct emulator FR extracted unless duplicated in `vice.texi`. |
| `native/vice/vice/doc/building/FreeBSD-GTK3-Howto.txt` | Build/port/support documentation | Reviewed; no distinct emulator FR extracted unless duplicated in `vice.texi`. |
| `native/vice/vice/doc/building/GTK3-Fedora-cross-build-setup.md` | Build/port/support documentation | Reviewed; no distinct emulator FR extracted unless duplicated in `vice.texi`. |
| `native/vice/vice/doc/building/Linux-GTK3-Howto.txt` | Build/port/support documentation | Reviewed; no distinct emulator FR extracted unless duplicated in `vice.texi`. |
| `native/vice/vice/doc/building/macOS-Distribution-Howto.txt` | Build/port/support documentation | Reviewed; no distinct emulator FR extracted unless duplicated in `vice.texi`. |
| `native/vice/vice/doc/building/macOS-Howto.txt` | Build/port/support documentation | Reviewed; no distinct emulator FR extracted unless duplicated in `vice.texi`. |
| `native/vice/vice/doc/building/macOS-Xcode-Howto.txt` | Build/port/support documentation | Reviewed; no distinct emulator FR extracted unless duplicated in `vice.texi`. |
| `native/vice/vice/doc/building/NetBSD-GTK3-Howto.txt` | Build/port/support documentation | Reviewed; no distinct emulator FR extracted unless duplicated in `vice.texi`. |
| `native/vice/vice/doc/building/NetBSD-howto.txt` | Build/port/support documentation | Reviewed; no distinct emulator FR extracted unless duplicated in `vice.texi`. |
| `native/vice/vice/doc/building/OpenBSD-GTK3-Howto.txt` | Build/port/support documentation | Reviewed; no distinct emulator FR extracted unless duplicated in `vice.texi`. |
| `native/vice/vice/doc/building/SDL-Howto.txt` | Build/port/support documentation | Reviewed; no distinct emulator FR extracted unless duplicated in `vice.texi`. |
| `native/vice/vice/doc/building/Windows-MinGW-GTK3-Howto.txt` | Build/port/support documentation | Reviewed; no distinct emulator FR extracted unless duplicated in `vice.texi`. |
| `native/vice/vice/doc/CIA-README.txt` | Subsystem design note | Extract CIA timer, alarm, underflow, delayed-load, and interrupt-observable behavior. |
| `native/vice/vice/doc/coding-guidelines.txt` | Developer process | No FR extracted; used only to confirm source layout, not Vice-Sharp TRs. |
| `native/vice/vice/doc/Documentation-Howto.txt` | Developer process | No FR extracted. |
| `native/vice/vice/doc/Doxygen-Howto.txt` | Developer process | No FR extracted. |
| `native/vice/vice/doc/gpl.texi` | License | No FR extracted. |
| `native/vice/vice/doc/html/fonts/OFL.txt` | License | No FR extracted. |
| `native/vice/vice/doc/html/images/keymaps.txt` | Generated-doc note | Extract keyboard layout/keymap coverage notes not duplicated by source prose. |
| `native/vice/vice/doc/html/robots.txt` | Website metadata | No FR extracted. |
| `native/vice/vice/doc/iec-bus.txt` | Subsystem design note | Extract IEC bus topology and observable drive/serial-bus interaction behavior. |
| `native/vice/vice/doc/joystick.md` | Subsystem design note | Extract joystick API behavior and host/controller mapping expectations. |
| `native/vice/vice/doc/mainpage.dox` | Developer index | Use as index only; detailed FRs come from included CIA/IEC/howto docs. |
| `native/vice/vice/doc/readmes/Readme-SDL.txt` | Port readme | Reviewed for observable UI/input behavior; canonical FR wording is taken from vice.texi where duplicated. |
| `native/vice/vice/doc/readmes/Readme-SDL2.txt` | Port readme | Reviewed for observable UI/input behavior; canonical FR wording is taken from vice.texi where duplicated. |
| `native/vice/vice/doc/Release-Howto.txt` | Developer process | No FR extracted. |
| `native/vice/vice/doc/vice.texi` | Master manual | Extract FRs for emulator features, machine profiles, keyboard/joystick, media, resources, monitor, snapshots, drives, cartridges, video, sound, tape, and configuration behavior. |
| `native/vice/vice/doc/vim/README.md` | Editor tooling | No FR extracted. |
