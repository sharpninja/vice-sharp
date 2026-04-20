# ViceSharp 30-Stage Execution Plan

## BYRD Adherence
- One change per commit
- Build+test after every commit
- Zero warnings
- Runnable always
- Push every commit
- No speculative, no partial, no refactor outside task

## Execution Table

| Stage | Task | File | Validation Required |
|-------|------|------|----------------------|
| 1 | Update plan baseline | docs/plan.md | This file, Iteration 0 marked 100% |
| 2 | handoff.md snapshot | handoff.md | Aligned to current files |
| 3 | C64Machine.cs wiring | src/ViceSharp.Core/ArchitectureBuilder.cs | Full bus/clock/interrupt router, empty chips, compiles clean |
| 4 | Memory map + PLA | src/ViceSharp.Chips/PLA/ | $0000-$FFFF exact regions, RAM/ROM/IO/PLA priority |
| 5 | Reset sequencing | src/ViceSharp.Chips/Cpu/ | 7-cycle reset to all chips + port init |
| 6 | Mos6510 fetch loop | src/ViceSharp.Chips/Cpu/Mos6510.cs | Empty single-cycle exec skeleton |
| 7 | CPU opcode table | src/ViceSharp.Chips/Cpu/ | 256-entry table, all unimplemented → trap |
| 8 | Addressing modes | src/ViceSharp.Chips/Cpu/ | All 13 modes + exact cycle counts |
| 9 | Official opcodes | src/ViceSharp.Chips/Cpu/ | All 151 official 6510 |
| 10 | Illegal opcodes | src/ViceSharp.Chips/Cpu/ | All 105 unofficial (LAX/SAX etc) |
| 11 | Processor port $00/$01 | src/ViceSharp.Chips/Cpu/Mos6510.cs | Full banking + open-bus behavior |
| 12 | CPU interrupt logic | src/ViceSharp.Chips/Cpu/ | IRQ/NMI/RDY/RES exact pin handling |
| 13 | Mos6569 raster core | src/ViceSharp.Chips/VicIi/ | 63 cycles/line, 312 lines/frame |
| 14 | VIC-II badlines + DMA | src/ViceSharp.Chips/VicIi/ | Exact 40-cycle stall, sprite DMA |
| 15 | VIC-II sprites + collisions | src/ViceSharp.Chips/VicIi/ | Full DMA/rendering pipeline |
| 16 | VIC-II IRQ | src/ViceSharp.Chips/VicIi/ | Raster compare, lightpen, sprite collision |
| 17 | Mos6526 skeleton | src/ViceSharp.Chips/Cia/ | Timer A/B countdown registers |
| 18 | CIA timers + TOD | src/ViceSharp.Chips/Cia/ | Exact cycle countdown + interrupt gen |
| 19 | CIA I/O ports | src/ViceSharp.Chips/Cia/ | Keyboard matrix, joystick, VIC bank select |
| 20 | SID 3-voice + filter | src/ViceSharp.Chips/Sid/ | Full synthesis + ADSR + 8580 inheritance |
| 21 | IEC/D64 + Input complete | src/ViceSharp.Chips/IEC/, Input/ | D64 sector r/w, joystick/keyboard final |
| 22 | ROM loader + SHA1 | src/ViceSharp.RomFetch/ | Kernal/BASIC/CHARGEN load + verify |
| 23 | First boot | tests/ViceSharp.TestHarness/ | Reset → execute to BASIC $E55B |
| 24 | First raster IRQ | tests/ViceSharp.TestHarness/ | Run to line 0 interrupt |
| 25 | VICE trace logger | tests/ViceSharp.TestHarness/ | Exact match to x64sc monitor format |
| 26 | Golden trace capture | tests/ViceSharp.TestHarness/ | 10000 cycles from latest VICE nightly |
| 27 | Lockstep validation | tests/ViceSharp.TestHarness/ | Cycle-by-cycle diff, zero deviation |
| 28 | Determinism replay | tests/ViceSharp.TestHarness/ | 100000 cycle bit-exact test |
| 29 | Final handoff update | handoff.md | All chips "Complete", add boot + validation proof |
| 30 | README final | README.md | Iteration 1 Complete, link validation results |

## Gates (between every stage)
- Build 100% clean + all tests pass
- NativeAOT valid
- No new warnings
- System runs (console harness)

## Forbidden
- No optimizations
- No features outside current stage
- No working-dir junk

## Chip Folders (current)
- `src/ViceSharp.Chips/Cpu/` - MOS6510/6502
- `src/ViceSharp.Chips/VicIi/` - MOS6569
- `src/ViceSharp.Chips/Cia/` - MOS6526
- `src/ViceSharp.Chips/Sid/` - MOS6581
- `src/ViceSharp.Chips/PLA/` - MOS906114
- `src/ViceSharp.Chips/IEC/` - Disk CPU
- `src/ViceSharp.Chips/Input/` - Keyboard/Joystick

## Iteration Status
- **Iteration 0 (Foundations)** — Complete (100%)
- **Iteration 1 (C64 Bringup)** — In progress (Stage 1 of 30)
