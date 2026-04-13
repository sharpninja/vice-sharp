# Third-Party Notices

ViceSharp is a derivative work of VICE and incorporates knowledge, algorithms, and specifications from the following sources.

## VICE (Versatile Commodore Emulator)

- **Project:** https://vice-emu.sourceforge.io/
- **License:** GPL-2.0-or-later
- **Authors:** The VICE Team (Andreas Boose, Dag Lem, Marco van den Heuvel, and many others)

ViceSharp is a clean-room C# port informed by VICE's architecture and behavior, not a direct code translation. VICE's GPL license applies to this derivative work.

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

## NuGet Dependencies

See `Directory.Packages.props` for the complete list of NuGet package dependencies and their respective licenses.
