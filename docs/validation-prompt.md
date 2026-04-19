# VICE-Sharp Cycle-Exact Validation Prompt

**Task:**  
Create and execute a complete, reproducible validation that proves VICE-Sharp is cycle-exact to classic VICE x64sc (CPU, VIC-II, CIA, bus timing) using only the official VICE nightly build’s built-in monitor trace capability. Do not build VICE yourself. Measure runtime performance only after cycle accuracy is confirmed 100 %.

**Constraints**  
- Use the latest ad-hoc VICE nightly x64sc binary from https://github.com/VICE-Team/svn-mirror/releases (monitor-enabled version).  
- Build VICE-Sharp from current main with Core + Chips determinism enabled.  
- All traces must be generated in identical format for line-by-line diff.

**Step 1: Preparation**  
1. Download latest VICE nightly x64sc.  
2. Prepare identical test assets: C64 ROM set (BASIC/KERNAL/CHARGEN), cycle-precise test PRGs (CPU opcode timing, VIC raster IRQ/badline/sprite DMA, CIA timers/TOD, frame sync).  
3. Include one custom test PRG that runs fixed cycle loops and logs raster/cycle counts.

**Step 2: Instrumentation**  
**Classic VICE:**  
Launch with: `x64sc.exe -moncommands trace.mon -logfile vice-trace.log`  
`trace.mon` must enable monitor tracing of every instruction with: raster line/cycle stamp, PC, registers, flags, disassembly, cumulative emulated cycles.

**VICE-Sharp:**  
Add/enable a zero-allocation deterministic logger that outputs exactly the same fields and format as the VICE monitor to `sharp-trace.log`.

**Step 3: Execution**  
- Load the identical PRG + same input sequence into both emulators.  
- Run for a fixed number of frames (default: 10) or until a known breakpoint.  
- Dump full monitor-style traces for each test PRG.

**Step 4: Comparison & Validation**  
- Run a diff script on the two trace logs.  
- On any mismatch, report: cycle/raster position, PC/register state, and 10-instruction context before/after.  
- Confirm frame-end total emulated cycles match exactly.  
- 100 % match across the full test suite = cycle-exact confirmation.

**Step 5: Performance Measurement**  
Once accuracy is locked:  
- Measure wall-clock time for exactly 1 000 000 emulated cycles in both emulators.  
- Run 10 times, discard cold-start run, compute average host cycles per emulated cycle.

**Step 6: Iteration & Edge Cases**  
- Fix any divergence in VICE-Sharp and re-run full suite.  
- Lock trace replay as a regression test.  
- Explicitly validate: PAL ↔ NTSC switch, 1541 drive bus timing, long-run stability (≥1 000 000 frames, zero drift), expansion/REU bus if in scope.

**Output Requirements**  
Produce:  
1. Final validation report (pass/fail per test).  
2. First-mismatch examples (if any).  
3. Performance numbers (host cycles per emulated cycle).  
4. Ready-to-commit trace-replay regression test for VICE-Sharp.

Execute this plan exactly and report results.
