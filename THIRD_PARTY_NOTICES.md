# Third-Party Notices

ViceSharp is a derivative work of VICE and incorporates knowledge, algorithms, and specifications from the following sources.

## VICE (Versatile Commodore Emulator)

- **Project:** https://vice-emu.sourceforge.io/
- **License:** GPL-2.0-or-later
- **Authors:** The VICE Team (Andreas Boose, Dag Lem, Marco van den Heuvel, and many others)

ViceSharp is a clean-room C# port informed by VICE's architecture and behavior, not a direct code translation. VICE's GPL license applies to this derivative work.

### Bundled VICE keymap data (`*.vkm`)

- **License:** GPL-2.0-or-later
- **Origin:** VICE `data/C64/*.vkm` keyboard mapping files
- **Redistributed in:** the Xbox (UWP) head packages these under `Assets/vice-data/C64/*.vkm`

The C64 keyboard keymap files (`*.vkm`) shipped with the ViceSharp Xbox head are verbatim GPL-2.0-or-later VICE data. They are redistributed here unmodified, under the same GPL-2.0-or-later license as the VICE project, so the emulator can seed its writable keymap folder on first run. No Commodore ROM images (`kernal-*.bin`, `basic-*.bin`, `chargen-*.bin`) are bundled; ROMs remain user-provided or fetched at runtime.

### Bundled PetMe64 font (`PetMe64.ttf`)

- **Author:** Kreative Korporation (Kreative Software), https://www.kreativekorp.com/software/fonts/c64/
- **License:** Kreative Software Relay Fonts Free Use License 1.2f (redistributable free of charge with the license included verbatim and credit given; see `src/ViceSharp.Xbox/Assets/Fonts/PetMe-FreeLicense.txt`)
- **Origin:** VICE `data/common/PetMe64.ttf` (VICE bundles the PetMe font family under the same license)
- **Redistributed in:** the Xbox (UWP) head packages the font under `Assets/Fonts/` and renders the virtual keyboard's keycaps with it (family "Pet Me 64")

The PetMe64 TrueType font reproduces the Commodore 64's character set pixel-for-pixel. It is redistributed unmodified together with its verbatim license text, with credit to Kreative Korporation as the license requires.

## resid / resid-fp

- **Project:** SID chip emulation library by Dag Lem
- **License:** GPL-2.0-or-later
- **Usage:** SID emulation algorithms and filter models

The resid-fp (fast precision) variant provides the cycle-accurate SID filter model that ViceSharp's SID implementation targets for behavioral equivalence.

## Klaus Dormann's 6502 Test Suite

- **Project:** https://github.com/Klaus2m5/6502_65C02_functional_tests
- **License:** GPL-2.0 (with permission for test use)
- **Usage:** CPU instruction validation

Used in ViceSharp's determinism and correctness test suite to validate 6502/6510 instruction behavior against a known-good reference.

## Commodore Specifications and Documentation

The following Commodore/MOS Technology specifications are referenced for behavioral accuracy. These are factual hardware specifications and are not subject to copyright on their functional descriptions:

- **MOS 6502/6510/8502** — CPU instruction set, addressing modes, cycle timing
- **MOS 6567/6569 (VIC-II)** — Video Interface Controller specifications
- **MOS 6581/8580 (SID)** — Sound Interface Device specifications
- **MOS 6526 (CIA)** — Complex Interface Adapter specifications
- **MOS 6522 (VIA)** — Versatile Interface Adapter specifications
- **MOS 906114 (PLA)** — Programmable Logic Array specifications

## Key Technical References

- **Christian Bauer** — "The MOS 6567/6569 video controller (VIC-II) and its application in the Commodore 64" (VIC-II timing article)
- **Bob Yannes** — SID designer interview (SID technical details)
- **C64 Programmer's Reference Guide** — Commodore, 1982
- **Mapping the Commodore 64** — Sheldon Leemon, COMPUTE! Publications, 1984

## File Format Specifications

The following community-documented file formats are implemented:

- **D64** — 1541 disk image format
- **G64** — GCR-encoded disk image format
- **T64** — tape container format
- **TAP** — raw tape pulse format
- **CRT** — cartridge image format
- **PRG** — program file format
- **P00** — PC64 file format

## Commodore C= logo (Wikimedia Commons)

**Requirement:** FR-XBOXGPL-007 (see `docs/requirements/functional/FR-Xbox-Branding.md`).

- **File:** Commodore C= logo.svg
- **Source:** https://commons.wikimedia.org/wiki/File:Commodore_C%3D_logo.svg
- **Author:** Alien426 (Wikimedia Commons)
- **License:** Creative Commons Attribution-ShareAlike 4.0 International (CC BY-SA 4.0)
  https://creativecommons.org/licenses/by-sa/4.0/
- **Used in:** Microsoft Store / Xbox marketing assets under `docs/xbox/store-screenshots/` (posters, box art, super-hero art, key/hero art, featured promo square, app tile icons); local copies `Commodore_C_logo.svg` and `Commodore_C_logo-1280.png` with attribution file `LOGO-ATTRIBUTION-CC-BY-SA-4.0.txt`.

This mark is **not** a Commodore system ROM and is separate from GPL VICE content. When redistributed (including derivative marketing composites that include the mark), attribution and ShareAlike terms of CC BY-SA 4.0 apply. The product name **Vice#** and ViceSharp wordmarks are separate from this logo.

## NuGet Dependencies

See `Directory.Packages.props` for the complete list of NuGet package dependencies and their respective licenses.
