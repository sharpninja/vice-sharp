# ViceSharp Handoff (2026-04-22)

## Iteration 01 In Progress

### Current Baseline
- Solution build is green through `ViceSharp.slnx`
- Test baseline is `28 passed, 8 skipped`
- C64 builder now requires a real ROM provider and validates ROM availability
- C64 memory map now banks BASIC/KERNAL/CHAR ROMs instead of copying ROM bytes into writable RAM
- Debug monitor command surface exists: `r/z/n/m/d/b/ub/bl/cycles/reset`

### Monitor Commands (r z n m d b ub bl cycles reset)
- r: Registers with NV-BDIZC flags
- z [n]: Step n instructions
- n: Step over JSR
- m [addr]: Memory dump
- d [addr [n]]: Disassemble
- b/ub/bl: Breakpoint management
- cycles: Cycle counter

### Next Steps
1. Verify BASIC `READY.` boot path end-to-end
2. Re-enable VICE-backed CPU/VIC/CIA validation instead of skipping it
3. Finish remaining Iteration 1 features still missing from runtime scope: 1541, datasette, cartridges, snapshots, capture/export
