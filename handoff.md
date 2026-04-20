# ViceSharp Development Handoff

## Current Status (2026-04-19 Evening)

### Completed Chips (All VICE-equivalent)

| Chip | Location | Status |
|------|----------|--------|
| MOS6510 CPU | `src/ViceSharp.Chips/Cpu/` | Complete - all 56 opcodes + KIL/LAX/SAX illegal |
| MOS6569 VIC-II | `src/ViceSharp.Chips/VicIi/` | Complete - raster, sprites, DMA, rendering |
| MOS6581 SID | `src/ViceSharp.Chips/Sid/` | Complete - 3-voice synthesis |
| MOS6526 CIA | `src/ViceSharp.Chips/Cia/` | Complete - timers, TOD, interrupts |
| MOS906114 PLA | `src/ViceSharp.Chips/PLA/` | Complete - memory banking |
| C64KeyboardMatrix | `src/ViceSharp.Chips/Input/` | Complete - 8x8 matrix |
| C64JoystickPort | `src/ViceSharp.Chips/Input/` | Complete - digital/POT |
| IecDrive | `src/ViceSharp.Chips/IEC/` | Complete - D64 support |
| D64Image | `src/ViceSharp.Chips/IEC/` | Complete - sector read/write |

### Architecture Components

- **ArchitectureBuilder**: Wires BasicBus + VIC + CIA + PLA + SID (a1f1394)
- **C64Descriptor/C64NtscDescriptor**: Machine configs (PAL/NTSC)
- **C64Palette**: 16-color VIC-II palette
- **RomProvider**: VICE ROM download + SHA256 verification
- **C64RomLoader**: MD5 validation, file loading

### Build Status
- **Result**: BUILD_OK
- **Errors**: 0
- **Warnings**: 2 (NETSDK1210 - IsAotCompatible)

### Recent Commits (This Session)
- `8c0a7f2` - docs: add late evening session log
- `664def0` - feat: add LAX and SAX illegal opcodes
- `e8fd19d` - feat: add KIL and LAX illegal opcodes
- `f50c5bd` - refactor: remove partial C64Machine
- `8aed926` - docs: add evening session log
- `a1f1394` - feat: wire VIC-II, CIA, PLA, SID in ArchitectureBuilder

All commits synced to origin (Azure DevOps) and github.

## Next Steps

1. **Complete opcode table**: Add DCP/ISC/RLA/RRA/SLO/SRE (need type fixes)
   - Compare() needs overload accepting byte for zero page
   - Absolute/AbsoluteX/AbsoluteY addressing need byte return
2. **First boot test**: VICE x64sc trace comparison
3. **Avalonia UI**: Build VideoSurface with pixel rendering

## Project Rules (.clinerules)

- Build after every change (zero errors/warnings)
- Implement interfaces: Abstractions → Core → Chips → Architectures
- NativeAOT compatible for all non-test code
- Zero allocations on hot path (cycle execution)
- One chip = one subdirectory (Cpu/, VicIi/, Sid/, Cia/, IEC/)
- FR-* specs required for all chips

## Session Logs
- `docs/session-logs/session-2026-04-19-001.md` - Morning session
- `docs/session-logs/session-2026-04-19-002.md` - Evening session
- `docs/session-logs/session-2026-04-19-003.md` - Late evening session
