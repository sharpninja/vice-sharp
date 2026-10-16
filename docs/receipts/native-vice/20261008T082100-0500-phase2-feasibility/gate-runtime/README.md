# External stock-xvic callback permission probe

Recorded 2026-10-08T11:10:31-05:00. This proves narrow post-`vic_cycle`
permission control, not full device freezing, complete bus observation, exact
half-cycle control, parity, or Phase 2 acceptance.

Evidence correction recorded 2026-10-08T13:47:42-05:00 for draft HV F-04/F-11.
The seven original probe stdout files are all zero bytes. They do not retain
native startup/reset diagnostics. No missing output or historical helper
preimage was recreated. Pre-edit copies and fresh readbacks are retained in
`../../20261008T134300-0500-hv-evidence-corrections/`.

## Mock-first implementation

`tests/ViceSharp.NativeClockProbe.Tests/GATE-SLICE.md` defines the interface,
failure codes, fixture scope and order before gate implementation. Current
effective Layer 1 FR-VICE-001, TR-VICE-ORACLE-001 and TEST-VICE-001 remained
pending with every AC unsatisfied.

Before writing `tools/immutable-vice/clock-gate.c` or its header, full suites
passed: `../gate-mock-01/result.json` 31/31, `../gate-prior-vsf-01/result.json`
199/199, `../gate-prior-oracle-01/result.json` 82/82. The 31 include the prior
12 clock tests and 19 new mock fixtures. Each new fixture also rejects wrong
status, phase, value, omitted/extra/duplicated/reordered events as expected
inside the passing harness.

After implementation, the same 19 fixtures/assertions bind the real compiled
C gate with independent scripted I/O. Full suites passed:
`../gate-implementation-01/result.json` 50/50,
`../gate-prior-vsf-02/result.json` 199/199,
`../gate-prior-oracle-02/result.json` 82/82. These are 331 native-safe unit and
compiled-fixture tests, not 331 real-emulator acceptance cases. All runs are
unfiltered Release, exit 0, zero failed/skipped/aborted. Build/test receipts
bracket unchanged native integrity. Twenty compiler/run logs were copied and
hash-verified under `../gate-fixture-evidence/`, originals retained.

That historical copy set did not include the clock-probe compiler log, and the
untracked pre-refactor `ClockHookImplementationTests.cs` bytes were not retained.
Those are provenance gaps, not proof that the refactor was unchanged. Later
50/50 reruns and copies of both current compiler logs establish current results
only; see `../../20261008T131300-0500-phase1-fixture-reuse/compile-log-copy-proof.json`.

The gate validates dependencies, publishes the exact uint64 clock, flushes,
then receives one byte. Only `c` permits return. Codes 64-70 distinguish
publication, flush, EOF, quit, invalid command, dependency and input failures.
No native state is written or inferred by the gate.

## Real executable and host probe

`gate-observer.c` binds the tested gate to stdio for diagnostics. It emits
clock and terminal records on stderr. Native output is directed to stdout, but
all seven captured stdout files are empty, so native diagnostic retention was
not established. Buffered stdout combined with `_Exit` is a candidate cause,
not a diagnosed runtime cause. The comment correction did not change adapter
behavior or repair the missing output.
Nonzero gate status calls `_Exit(status)` before returning to native code.
That is deliberate diagnostic termination, not a graceful native shutdown,
successful snapshot operation or normal emulator exit.

`compile-result.json` and `link.launch.json` retain all arguments and input
hashes. The executable is outside both native source trees, linked from the
same 99 unchanged stock objects/archives with the external clock wrapper,
gate and diagnostic observer. `link.result.json`: exit 0; executable SHA256
`A15ED9740135D4E95A2594D8155E2FC807B45453FFCF525E731DEDF02733FF60`.
No upstream source, stock object or original executable was replaced.

`Invoke-GateProbe.ps1` retains launch/result JSON, timestamped observations and
host permissions and line-preserved stderr for every run. It also creates a
stdout file for each run, but all seven are empty. File creation is not proof
of captured native messages. The original files and their empty-file hashes
are retained in the correction readback, not replaced with another run's logs.
It refuses receipt-directory overwrite. It withholds input with a requested
250 ms wait after every observed callback, then grants one byte or terminates.
Those waits are bounded diagnostic observations, not an indefinite proof of
every device being frozen. A hung probe fails and terminates only its own
process tree; no timeout or forced termination occurred in these seven runs.

- `permissions-a` and `permissions-b`: 16 permits each, 17 actual clock records
  each, quit record and exit 67. The ordered prefixes match each other and the
  ungated native callback prefix, including `7,8,7,8`.
- `terminal-eof`: one record, stdin closed, exit 66.
- `terminal-invalid`, `terminal-uppercase`, `terminal-newline`, `terminal-nul`:
  one record each, terminal record and exit 68. No second callback appeared.
- Across all runs, 39 withheld-permission intervals had no next record or EOF.
  The minimum measured interval was 249.6824 ms, not an asserted exact 250 ms.

`integrity-result.json` records every result and zero changes to all 3847
original source files, all 99 stock link inputs, the original xvic executable
and all eight native-baseline fields. The original stock xvic SHA256 remains
`C01442BC1F15DD8C7006FABE0D35DFA9707664A3120DD1FA014A51EEB481B92C`.

The first script's console summary used `Select-Object` on an ordered hashtable
and displayed null fields. Its durable result JSON was already correct; fresh
readback verified it. The console projection was corrected with a PSCustomObject
cast before the other six runs. Original result data was not rewritten.

## Open gates

Real CPU address/data/RW, dummy/real, interrupt-stage and stolen-cycle streams
are not supplied by this gate. Fast fetch bypasses native memory tables, and
missing observations must not be invented from PC or instruction history.
All-device freeze, runtime output-pipe failure, full-state equivalence after
long delays, complete lockstep, legacy v1.3 native loading, reset-boundary
snapshot behavior and production harness integration remain separate gates.
Unit publication/flush failure controls are not real broken-pipe evidence.

The stdio adapter remains diagnostic-only. Its buffering/termination behavior
has not passed a mock-first adapter contract gate, and these runs did not meet
the raw native stdout retention expectation in `GATE-SLICE.md`. Before relying
on native-log retention or integrating this adapter into the harness, validate
that contract, implement any necessary correction after its green mock gate,
then rerun with new receipt paths. Do not replace or reclassify these logs.

The noon local Opus 5.5 max catch-up must review this whole slice, its
requirements, tests, helper refactor, real probes and limits, plus every other
deferred slice. No requirement, phase, TODO or whole goal is marked complete.
