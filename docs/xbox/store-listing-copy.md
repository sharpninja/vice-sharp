# Microsoft Store listing copy (ViceSharp Xbox UWP)

**Slice:** FEAT-XSTORELIST-001  
**Package:** `ViceSharp.Xbox` (device families: Xbox + Desktop)  
**Privacy policy URL (use this):**  
`https://github.com/sharpninja/vice-sharp/blob/main/docs/PRIVACY.md`  
(After wiki publish, prefer the wiki page URL if exported; keep this file as source of truth.)

**Support URL:** `https://github.com/sharpninja/vice-sharp/issues`  
**Website:** `https://github.com/sharpninja/vice-sharp`  
**Source / GPL offer:** `https://github.com/sharpninja/vice-sharp`

---

## Product name

ViceSharp

## Short description (max ~256 chars; trim if Partner Center is tighter)

VICE-compatible Commodore 64 emulator for Xbox and Windows. Cycle-aware C64 core, gamepad-friendly UI, optional RomM game library. Free software (GPL-2.0-or-later). No Commodore ROMs included.

## Description (long)

ViceSharp is a .NET Commodore 64 emulator derived from the VICE project. The Xbox / Windows Store package is a 10-foot UWP host with gamepad navigation, on-screen keyboard, settings, and optional library browsing.

**Emulation**

- Commodore 64 focus (VICE-compatible managed core; Iteration 1 lockstep program against x64sc).
- Disk, tape, and cartridge attach; T64 support in the 1.2 line.
- Snapshots, monitor, and host features evolve with the open-source project.

**Game library (optional)**

- Connect to your own RomM 5.x server (paste a Client API Token, or use LAN scan where the OS allows local network access).
- Browse cover art, search, collections/lists, download, attach, and boot.
- Optional CSDb discovery/ingest into RomM when you configure that path.

**Important legal notes**

- License: **GPL-2.0-or-later**. Complete corresponding source: https://github.com/sharpninja/vice-sharp
- ViceSharp is a VICE-derived work; VICE is developed by the VICE Team (https://vice-emu.sourceforge.io/).
- **No Commodore ROMs are included.** You must supply legally obtained system ROMs and software. See project ROM documentation.
- The Microsoft Store delivery channel is subject to Microsoft's terms; it does not relicense the ViceSharp source code.
- "Commodore 64" is used descriptively. No affiliation with or endorsement by Commodore trademark holders is implied.

**Privacy**

See the Privacy Policy linked on this listing. Settings and optional library credentials stay on your device; network use goes to services you configure.

## What's new (template for 1.2.1-class builds)

- RomM-backed library on Xbox and desktop heads (browse, lists, CSDb discovery path).
- T64 tape attach and host UX improvements.
- Packaging and Store submission pipeline for the UWP head.
- Ongoing cycle-aware C64 core work; see GitHub releases for exact tags.

## Features (bullet list for Partner Center)

- Commodore 64 emulator (VICE-compatible .NET core)
- Gamepad-first 10-foot UI for Xbox
- Optional RomM library browse, download, and launch
- Optional CSDb discovery into RomM
- Settings, control remapping, About / GPL disclosure
- User-supplied ROMs only (nothing copyrighted from Commodore ships in the package)
- Free and open source (GPL-2.0-or-later)

## Category

Utilities & tools (or the closest Partner Center emulator-appropriate category available at submission time). Prefer **Utilities** over Games if the Store rejects emulator-as-game listings.

## Age rating notes (IARC questionnaire)

- No user-generated content hosted by the publisher.
- No social networking / chat built into the core app.
- Network: user-configured library/ROM endpoints only.
- Content: app is a tool; **user media** may include any era of C64 software (publisher does not ship games).

## Keywords / search terms

commodore, c64, emulator, vice, retrogaming, 6502, romm

## Pricing and availability

- Free
- Markets: worldwide free markets initially (operator may restrict later)
- Device families: **Xbox** and **Windows Desktop** (matches Package.appxmanifest)

## Screenshot capture runbook

Capture at **1920x1080** (Xbox) and matching desktop window captures if Desktop family remains enabled.

| # | Screen | How | Notes |
| --- | --- | --- | --- |
| 1 | Home | Launch app on Dev-Mode Xbox or desktop UWP | Clean background, no debug banners |
| 2 | Emulation running | Boot a legally owned title after ROM provision | Prefer a public-domain or own homebrew title for marketing if possible |
| 3 | Library (RomM) | Connected library grid | Blur or avoid covers you lack rights to market if required |
| 4 | Settings | Machine / ROM readiness | Show user-owned ROM path messaging without leaking tokens |
| 5 | About | GPL + source URL visible | Required for certification story |
| 6 | Controls / remapping | Optional | Shows gamepad completeness |

**Capture path on Xbox:** Dev Mode Device Portal screenshots, or HDMI capture.  
**Capture path on PC:** Win+Alt+PrtScn or the UWP head fullscreen.

Store the finals under `docs/xbox/store-screenshots/` (git-lfs or compressed PNG; do not commit huge RAW dumps). Filename pattern: `01-home.png`, `02-emulation.png`, ...

## Partner Center checklist (paste-ready)

- [ ] Name reserved: ViceSharp  
- [ ] Privacy URL set to docs/PRIVACY.md public raw or blob URL  
- [ ] Support URL = GitHub issues  
- [ ] Short + long description pasted  
- [ ] GPL / no-ROM / source offer language included  
- [ ] Screenshots uploaded per device family  
- [ ] IARC completed  
- [ ] Xbox + Desktop families enabled  
- [ ] Package identity vars stored in ADO `xbox-store-publish` group  

## Related docs

- [gpl-store-section6-review.md](gpl-store-section6-review.md)  
- [microsoft-store-publishing-checklist.md](microsoft-store-publishing-checklist.md)  
- [../xbox-store-publishing.md](../xbox-store-publishing.md)  
- [../PRIVACY.md](../PRIVACY.md)  
