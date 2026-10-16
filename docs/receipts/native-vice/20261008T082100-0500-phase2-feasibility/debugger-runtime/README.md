# External stock-xvic debugger feasibility probes

## Later diagnostic results: 2026-10-08T11:49:07-05:00

Post-initialization attachment subsequently produced actual hardware hits in
both the external wrapped clock probe and unchanged stock xvic. See
`../bus-runtime/README.md` for exact observations and limits. Earlier failures
remain valid observations; their cause has not been established.

- `fixture-entry.*` uses the independent compiled C clock fixture, with an
  execution watchpoint at its PE entry. It records no hit while the fixture
  emits its actual 7/observe 11/end 11 sequence. CDB PID 78080 was stopped;
  child 40548 exited between the ownership query and Stop-Process, whose
  cannot-find-process error was retained. Both were subsequently absent.
- `fixture-trace.*` explicitly traces ten host instructions, records its one
  completion marker and exits 0. This proves that bounded host instruction
  tracing worked, not emulated bus coverage.
- `fixture-step-entry.cdb`, `fixture-step-loop.cdb` and their run receipts
  attempted recursive `t "$<script"` tracing. Only one step appeared before
  the debugger waited; owned CDB PID 90236 was stopped. This attempt failed.
  Later explicit `t 0n1 "$<script"` worked for the retained stock fast-fetch
  probes. Do not generalize the earlier syntax result to every trace path.

Official references consulted:
[t command](https://learn.microsoft.com/en-us/windows-hardware/drivers/debuggercmds/t--trace-),
[processor breakpoints](https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/processor-breakpoints---ba-breakpoints-),
[trace command programs](https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/executing-until-a-specified-state-is-reached),
and [.apply_dbp](https://learn.microsoft.com/en-us/windows-hardware/drivers/debuggercmds/-apply-dbp--apply-data-breakpoint-to-context-).
Context restoration is only an unverified hypothesis for the earlier misses.
No `.apply_dbp` or target-context modification was attempted.

## Original unsuccessful probes

Recorded 2026-10-08T10:09:08-05:00. Both probes are unsuccessful diagnostics,
not cycle-control or bus-parity evidence. They do not establish that every
permitted external observation mechanism is infeasible.

Local CDB `10.0.19041.685` ran the unchanged stock xvic executable with SHA256
`C01442BC1F15DD8C7006FABE0D35DFA9707664A3120DD1FA014A51EEB481B92C`.
`nm` resolves `maincpu_mainloop` at `0x140050b80`, `maincpu_clk` at
`0x140262a10`, and `vic_cycle` at `0x140062990`, with image base `0x140000000`.
Each script uses rebased module RVAs. Symbol search was restricted to this local
receipt directory, with inherited symbol/source search paths cleared for the
child debugger. Exact process arguments and PIDs are in each launch JSON.

- `clock-probe.cdb` arms a hardware write watchpoint on the eight-byte clock,
  requests twelve stops with clock/RIP reads, then quits. The log shows the
  watchpoint enabled, but records zero stops while VICE logs startup/reset.
  The parent stopped only verified owned xvic PID 50212 and CDB PID 26796.
  No end marker was reached. `clock-probe.result.json` has `completed: false`.
- `entry-probe.cdb` checks a hardware execute breakpoint at `maincpu_mainloop`,
  explicitly enables single-step exception breaking, and quits on the first
  hit. Runtime disassembly at the rebased address matches the pinned prologue.
  It also records zero hits despite native CPU startup/reset logs. The parent
  stopped only verified owned xvic PID 2580 and CDB PID 52164.
  `entry-probe.result.json` has `completed: false`.

The build's `config.h` leaves `USE_VICE_THREAD` undefined. This rules out that
particular compile-time worker-thread explanation, not every debugger/thread
failure. The failure cause remains unknown. No software breakpoint, emulated
memory/register write, target patch or cleanup was requested. Debugger quit was
not reached in either case; forced termination is retained as failure evidence.

`integrity-result.json` confirms zero changes to the dirty `native/vice`
inventory, zero changes among all 3847 original stock source files, and the
same stock xvic hash after both probes. Neither process is a passing runtime gate.

## Public-interface source limits

### Follow-up manual interruption

At 2026-10-08T10:18:35-05:00 the third diagnostic explicitly interrupted its
verified owned xvic PID 40448 under CDB PID 66104 using `DebugBreakProcess`.
`inspect-hardware.interrupt.json` records success, native error 0, closed handle
and `cycleControlSuccess: false`. The CDB log records the external break-in
thread, then reads main thread 0 at `xvic+0x50d74`, immediately before a call
to `vic_cycle`. DR0 points at the clock, DR7 is `0x90401`, DR6 is `0xffff0ff1`,
and the actual clock read is `0x9795243a`. CDB subsequently quit with exit 0.

This was a requested manual interrupt, not a proven hardware breakpoint hit.
It does not resolve why the prior watchpoints failed. GDB was not installed;
package metadata showed a Python dependency and no installation was attempted.
The separate link-time wrapper feasibility slice proceeded only after its own
passing mock and cumulative test gates. See `../clock-runtime/README.md`.

### Remaining observation gaps

The permitted sequence remains public interfaces, external process control,
then external link/function-table interposition. Source findings at the pin:

- `vice/src/monitor/monitor_binary.c:1452` returns instruction history registers,
  instruction bytes and a clock. Raster/cycle register placeholders are `ffff`;
  this is not a complete per-cycle bus observation stream.
- `vice/src/vic20/vic20cpu.c:43` defines each `CLK_INC` as incrementing
  `maincpu_clk` then calling `vic_cycle`.
- The fast opcode-fetch path at `vic20cpu.c:115` reads `bank_base` directly and
  clocks the fetched bytes. Memory-table interposition alone must not claim to
  observe all fetches.
- `vice/src/vic20/vic20mem.c:536` selects watchpoint tables, including dummy
  accesses when enabled, but does not itself disable the fast fetch path.
  Watchpoint helpers at lines 260-283 push an address before the actual access;
  they do not themselves capture returned read data for a complete bus trace.

Next work must prove actual observations and unchanged target semantics before
claiming a solution. Missing bus data/dummy classification/interrupt-stage or
stolen-cycle evidence must fail closed. Do not infer those values from an
instruction-level history, modify VICE, or weaken the comparator. Include both
failed debugger probes and their unresolved cause in noon Opus catch-up HV.
