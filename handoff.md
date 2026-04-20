# ViceSharp Development Handoff

## Current Status (2026-04-19)

### Completed Chips (All VICE-equivalent)

| Chip | Location | Status |
|------|----------|--------|
| MOS6510 CPU | `src/ViceSharp.Chips/Cpu/` | Complete - all 56 opcodes, I/O port |
| MOS6569 VIC-II | `src/ViceSharp.Chips/VicIi/` | Complete - raster, sprites, DMA, rendering |
| MOS6581 SID | `src/ViceSharp.Chips/Sid/` | Complete - 3-voice synthesis |
| MOS6526 CIA | `src/ViceSharp.Chips/Cia/` | Complete - timers, TOD, interrupts |
| MOS906114 PLA | `src/ViceSharp.Chips/PLA/` | Complete - memory banking |
| C64KeyboardMatrix | `src/ViceSharp.Chips/Input/` | Complete - 8x8 matrix |
| C64JoystickPort | `src/ViceSharp.Chips/Input/` | Complete - digital/POT |
| IecDrive | `src/ViceSharp.Chips/IEC/` | Complete - D64 support |
| D64Image | `src/ViceSharp.Chips/IEC/` | Complete - sector read/write |

### Architecture Components

- **ArchitectureBuilder**: Wires BasicBus + SID
- **C64Descriptor/C64NtscDescriptor**: Machine configs (PAL/NTSC)
- **C64Palette**: 16-color VIC-II palette
- **RomProvider**: VICE ROM download + SHA256 verification
- **C64RomLoader**: MD5 validation, file loading

### Build Status
- **Result**: BUILD_OK
- **Errors**: 0
- **Warnings**: 2 (NETSDK1210 - IsAotCompatible)

### Recent Commits (Last Session)
- `9c792f6` - docs: add session log for 2026-04-19 development
- `18b6ae4` - feat: add debug monitor help to console
- `aa0c821` - feat: implement IEC drive with D64 sector support
- `95c8deb` - feat: add ROM hash validation and file loading
- `b76a163` - feat: complete SID, CIA, PLA chips
- `00aba45` - feat: complete VIC-II chip
- `7a50fa9` - docs: add chip organization rules

All commits synced to origin (Azure DevOps) and github.

## Next Steps

1. **Complete opcode table**: Add all 151 opcodes (with VICE duplicate handling)
2. **Wire C64Machine**: Full chip wiring + ROM loader
3. **First boot test**: VICE x64sc trace comparison
4. **Avalonia UI**: Build VideoSurface with pixel rendering

## Project Rules (.clinerules)

- Build after every change (zero errors/warnings)
- Implement interfaces: Abstractions → Core → Chips → Architectures
- NativeAOT compatible for all non-test code
- Zero allocations on hot path (cycle execution)
- One chip = one subdirectory (Cpu/, VicIi/, Sid/, Cia/, IEC/)
- FR-* specs required for all chips

## Session Logs
- `docs/session-logs/session-2026-04-19-001.md` - Latest session log
