# ViceSharp 30-Stage Execution Plan

## Current Consolidated State (2026-05-12)
- Live workspace is dirty on `main...origin/main`; active slice is `ARCH-LOCKSTEP-001` plus the existing native shim/cache consolidation changes.
- Current validation is green: focused boot/lockstep/C64 machine gate passes 8/8, focused VIC/reset render gate passes 3/3, `LockstepValidationTests.First100000CyclesMatch` passes 1/1, and `dotnet test .\ViceSharp.slnx --no-build --nologo` passes 49/49.
- C64 ROM wiring is implemented through `C64RomLoader` and `C64MemoryMap` ROM-load mode; `BasicBootProofTests.C64_Boot_Reaches_Ready_Prompt` is green in the full harness.
- VICE-backed lockstep validation now clears `ResetStateMatches`, `First100CyclesMatch`, `First10000CyclesMatch`, and `First100000CyclesMatch`.
- Native shim reset determinism was tightened for hosted validation: RAM random-init chance is forced to zero and VIC-II registers are cleared through VICE's register reset path before each shim reset.
- CIA2 port-A idle input remains aligned with VICE for the KERNAL setup (`DD00=$07`, `DD02=$3F`, readback `$47`).
- MCP TODO cleanup completed for the active items: `ARCH-LOCKSTEP-001` and `ARCH-ROM-001` were marked done through the plugin with current validation evidence.
- Creating new runtime-gap TODOs for 1541, datasette, cartridges, snapshots, and capture/export is blocked by the plugin create path returning `Authentication required: no credential is configured on this client` for `Todo.CreateAsync`; do not hand-edit `docs/todo.yaml`.

## Open Work Queue

| Order | ID / Source | Scope | Current Status | Required Gate |
|-------|-------------|-------|----------------|---------------|
| 1 | `ARCH-LOCKSTEP-001` | Align managed lockstep reset/native bootstrap semantics. Normalize reset/bootstrap PC visibility, CPU side-effect timing, branch/stack timing, C64 RAM reset assumptions, and native reset determinism before widening to full lockstep. | Done through MCP TODO update; 100k lockstep and full harness are green. | Preserve `First100000CyclesMatch` as the regression gate. |
| 2 | `ARCH-ROM-001` | Keep ROM wiring current and verify real boot path. ROM load plumbing is done; remaining work is status cleanup. | Done through MCP TODO update; BASIC `READY.` proof is green in `BasicBootProofTests`. | Preserve boot proof and ROM-load coverage in the suite. |
| 3 | Stage 28 | Determinism replay after 10k lockstep gate. | Complete in working tree; `First100000CyclesMatch` passes. | Keep the 100k gate in the suite and preserve the validation evidence in TODO/session/handoff. |
| 4 | Handoff runtime gaps | 1541, datasette, cartridges, snapshots, capture/export. | Next planning slice; TODO creation is blocked by plugin `Todo.CreateAsync` auth until the create path is repaired. | Create bounded TODOs through MCP before implementation; each gets its own validation slice. |
| 5 | Stage 29 | Final handoff update. | Blocked on runtime-gap TODO split. | Handoff includes boot proof, 100k lockstep evidence, and intentionally excluded scope. |
| 6 | Stage 30 | README final. | Blocked on runtime-gap TODO split. | README can truthfully mark Iteration 1 complete and link validation results. |

## Next Execution Slice
1. Repair or work around the plugin `workflow.todo.create` auth/request path without hand-editing `docs/todo.yaml`.
2. Create bounded runtime-gap TODOs for 1541, datasette, cartridges, snapshots, and capture/export through MCP.
3. Refresh handoff/README from the new validation baseline.
4. Keep `First100000CyclesMatch` in the normal validation path and treat any future mismatch as a regression with exact cycle/register evidence.

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
- **Iteration 1 (C64 Bringup)** — In progress (Stage 28 of 30 complete in working tree)
- **Active blocker** — MCP `workflow.todo.create` auth/request path blocks bounded runtime-gap TODO split
- **Open TODO cleanup** — `ARCH-LOCKSTEP-001` and `ARCH-ROM-001` are done in MCP TODO; runtime-gap TODO creation remains blocked
