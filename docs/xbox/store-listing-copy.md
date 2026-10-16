# Microsoft Store listing copy (Vice#)

**Slice:** FEAT-XSTORELIST-001  
**Reserved product name:** `Vice#` (must match Package/Properties/DisplayName)  
**Package:** UWP head for Xbox + Windows Desktop  
**Publisher display name:** Sharp Ninja  

**Privacy policy URL:**  
https://github.com/sharpninja/vice-sharp/blob/main/docs/PRIVACY.md  

**Support URL:** https://github.com/sharpninja/vice-sharp/issues  
**Website:** https://github.com/sharpninja/vice-sharp  
**Source / GPL offer:** https://github.com/sharpninja/vice-sharp  

Identity details: [store-product-identity.md](store-product-identity.md)

---

## Product name (reserved)

Vice#

## Short description (Partner Center; under 10000 chars; aim under 256)

Vice# is a free Commodore 64 emulator for Xbox and Windows. VICE-compatible .NET core, gamepad-first UI, optional RomM library. No ROMs included. GPL open source.

(Character count: 155)

## Description (long) - paste into Partner Center

Vice# is a free Commodore 64 emulator for Xbox consoles and Windows PCs. It is the Microsoft Store build of the open-source ViceSharp project: a modern .NET implementation informed by VICE (the Versatile Commodore Emulator), with a 10-foot, gamepad-friendly interface designed for the living room.

### Emulation

Play classic Commodore 64 software with a cycle-aware managed core. Attach disks, tapes (including T64), and cartridges, then boot titles you own. Settings, control remapping, an on-screen keyboard, and an About screen with license and source information are built in. The project continues to evolve in the open; feature depth grows with each release.

### Optional game library (RomM)

Connect Vice# to your own RomM 5.x server on your network. Scan the LAN when the OS allows, or paste a Client API Token. Browse cover art, search your collection, manage lists, download media, attach it, and launch. Optional CSDb discovery can feed titles into RomM when you configure that path. Library features require a server you control; nothing is hosted by the publisher.

### What you must provide

Vice# does not include Commodore system ROMs or commercial game files. You supply legally obtained ROMs and software (for example from your own backups or permitted sources). Point the app at your ROM data as described in the project documentation.

### Open source

Vice# / ViceSharp is free software under GPL-2.0-or-later. Complete corresponding source code is available at:

https://github.com/sharpninja/vice-sharp

VICE is developed by the VICE Team (https://vice-emu.sourceforge.io/). Vice# is a VICE-derived work: a clean-room C# port informed by VICE architecture and behavior.

The Microsoft Store delivery channel is subject to Microsoft terms; it does not relicense the Vice# source code.

### Privacy and network use

Settings and optional library credentials stay on your device. Network access is used only for features you choose (for example HTTPS ROM acquisition or connecting to your RomM server). See the Privacy Policy linked on this listing.

### Trademarks

"Commodore 64" and related names are used only to describe the emulated system. No affiliation with or endorsement by any trademark holder is implied.

## What's new (1.2.1)

- First Microsoft Store package for the Xbox / Windows UWP host
- Optional RomM-backed game library (browse, lists, CSDb path)
- T64 tape attach and host UX improvements
- Cycle-aware C64 core from the ViceSharp 1.2 line

## Features (Partner Center feature list)

- Commodore 64 emulator (VICE-compatible .NET core)
- Gamepad-first 10-foot UI for Xbox
- Runs on Windows Desktop as well as Xbox
- Optional RomM library browse, download, and launch
- Optional CSDb discovery into your RomM server
- Settings, control remapping, virtual keyboard
- About screen with GPL source offer
- User-supplied ROMs only (no Commodore ROMs in the package)
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

## Store / Xbox marketing art

All logo marks use the **official Commodore C= logo** (CC BY-SA 4.0):

- Source: https://commons.wikimedia.org/wiki/File:Commodore_C%3D_logo.svg  
- Attribution: see `store-screenshots/LOGO-ATTRIBUTION-CC-BY-SA-4.0.txt`  
- Local files: `Commodore_C_logo.svg`, `Commodore_C_logo-1280.png`

| Asset | Size | Path |
| --- | --- | --- |
| Poster | 720×1080 | [store-screenshots/xbox-poster-720x1080.png](store-screenshots/xbox-poster-720x1080.png) |
| Poster | 1440×2160 | [store-screenshots/xbox-poster-1440x2160.png](store-screenshots/xbox-poster-1440x2160.png) |
| Box art 1:1 | 2160×2160 | [store-screenshots/xbox-boxart-2160x2160.png](store-screenshots/xbox-boxart-2160x2160.png) |
| Super hero 16:9 | 3840×2160 | [store-screenshots/xbox-superhero-3840x2160.png](store-screenshots/xbox-superhero-3840x2160.png) |
| Branded key art | 584×800 | [store-screenshots/branded-key-art-584x800.png](store-screenshots/branded-key-art-584x800.png) |
| Titled hero art | 1920×1080 | [store-screenshots/titled-hero-art-1920x1080.png](store-screenshots/titled-hero-art-1920x1080.png) |
| Featured promo square | 1080×1080 | [store-screenshots/featured-promo-square-1080x1080.png](store-screenshots/featured-promo-square-1080x1080.png) (no title) |
| App tile | 300 / 150 / 71 | `app-tile-*.png` (logo only) |

Titled assets: **Vice#** in Pet Me 64 under the C= mark. Solid `#101014` backgrounds.

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
