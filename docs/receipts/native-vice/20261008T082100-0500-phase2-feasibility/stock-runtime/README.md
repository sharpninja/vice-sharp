# Stock VICE runtime feasibility receipts

Recorded 2026-10-08T09:17:17-05:00 on PAYTON-LEGION2. This is Phase 2 diagnostic
evidence for `PLAN-VICE-IMMUTABLE-001`, `TR-VSFLOCKSTEP-RESUME-001`, and the
original whole-goal plan. No phase, requirement, TODO, or parity gate is accepted.
Opus HV is deferred to noon by the operator and must include every result below.

## Proven scope

The separate stock build at
`artifacts/immutable-vice/cc67418cba58e71d8e70fd353e9de47199ad2075/source`
completed exit 0. Both executables report VICE 3.10, and their `-help` calls exit
0. Version/result JSON contains executable hashes. The parent directory's
`stock-build-02-result.json` records the build, 3847 unchanged original source
files, and the separate inventory of 2815 generated artifacts. Compiler warnings
remain in the original logs.

`runtime-integrity-result.json` independently rechecks all 3847 original source
files after these probes and records no differences, the pinned commit and empty
tracked status. The eight-field dirty `native/vice` inventory also matches before
and after. No source edits or legacy patch application occurred.

## Attempts, including failures

- `xvic-01` and `x64sc-01`: process exit -1, machine initialization failed.
  The parent supplied machine-specific ROM directories incorrectly. Pinned
  `vice/src/sysfile.c:180-196` appends the machine subpath. Subsequent launches
  therefore pass the common `vice/data` root. Original failed receipts remain.
- `xvic-02`: reset-boundary save/step exited 0. Loading that snapshot stalled
  after `Unit 8: RESET (For undump)` without completing the next monitor command.
  The parent recorded `load-stall.json` and terminated only that probe process.
  This edge case remains unresolved, not passed or silently excluded.
- `x64sc-02`: reset-boundary save/load/step exited 0, but resumed VSF bytes differ.
  Sound was disabled with `+sound`; the load logged SID initialization trouble.
  The result is not a snapshot-equivalence pass.
- `xvic-03-ready` and `x64sc-03-ready`: BASIC-ready save/load/step with initialized
  `dummy` sound output, all process exits 0. `load-result.json` contains exact
  before/after monitor registers and clocks, which match for each machine, plus
  complete module hashes. Whole-file hashes do not match. xvic differs in VIA1,
  VIA2, DRIVE8, DRIVECPU0, 1541VIA1D0 and VIA2D0. x64sc differs in SIDEXTENDED,
  DRIVE8, DRIVECPU0, 1541VIA1D0 and VIA2D0. Each `module-byte-differences.json`
  retains every differing byte and payload offset. These are observations, not
  a conclusion that the source is defective or that differences are harmless.
- `x64sc-04-legacy`: the real compiled external adapter normalized the retained
  MAINCPU 1.2 fixture, adding 12 bytes, without modifying the input. The native
  load recovered clock 109163946, PC $5769, A $C2, X $02, Y $00, SP $FA, P $A0,
  exactly matching the independently read fixture fields. It then executed an
  instruction to clock 109163949, PC $5766, saved a new VSF, and exited 0.
  `normalization-result.json` pins input, output, compiled adapter and bridge
  hashes. `load-result.json` records all seven field comparisons, zero differences,
  and explicit limitations. This is one historical 1.2 fixture, not a 1.3 native
  proof, managed interoperability proof, or complete-device parity result.

## Reproduction and limits

Every launch has its full argument vector, executable, working directory, PID
and timestamp in `save-launch.json` or `load-launch.json`. `save-step.mon` and
`load-step.mon` are the exact monitor programs. All stdout, stderr and monitor
logs remain adjacent to the snapshots. Processes were launched locally via
PowerShell.MCP and `Start-Process -WindowStyle Hidden`, with MSYS2 MINGW64 DLLs
on the child PATH. No runtime process remains running at this checkpoint.

`InvokeNormalizer.cs.txt` is a diagnostic invocation bridge for PowerShell's
inability to box `ReadOnlySpan<byte>`. `Add-Type` compiled it against the existing
Release `ViceSharp.ViceCompatibility.dll`; it directly delegates to `Normalize`
and contains no parsing, mutation algorithm, filesystem or native behavior.
The 38-case mock gate predates that production implementation; its full byte
suite remains 77/77 and the cumulative oracle contract suite 82/82. Latest TRX,
stdout and result receipts are in the parent directory's
`runtime-cumulative-vsf-02/` and `runtime-cumulative-oracle-03/`, both unfiltered
Release runs, exit 0, zero failed, skipped, aborted, errors and timeouts.

Text monitor `undump` discards `mon_read_snapshot`'s return value in pinned
`vice/src/monitor/mon_parse.y:267`. Process exit 0 is therefore insufficient for
fail-closed load acceptance. The binary monitor checks this return value and
reports command failure or success in `monitor_binary.c:887-921`; obtaining that
acknowledgement is the next public-interface proof. Register recovery is useful
independent evidence but cannot replace complete state validation.

Native logs also report unavailable optional 2000/4000/CMDHD ROMs, missing
headless joystick maps, and T64 snapshot support not implemented. The pinned
`vice/src/tape/tape-snapshot.c:52-62` logs that T64 error but returns 0, explicitly
to avoid failing default snapshots. Sound overflow warnings are retained. None
of these messages was removed or converted into a clean-log claim.

Instruction stepping is not half-cycle control. No complete bus address/data,
dummy/real, interrupt-stage or stolen-cycle stream has been demonstrated.
Do not feed memory peeks, PC-derived events, or inferred bus values to the parity
comparator. Native-safe TestHarness build integration remains outstanding, so
these 159 unit/byte tests are not the full real-emulator acceptance suite.
