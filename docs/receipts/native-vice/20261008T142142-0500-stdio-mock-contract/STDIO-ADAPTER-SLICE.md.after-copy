# Diagnostic stdio adapter mock contract

2026-10-08: F-12 remediation inside the existing unaccepted gate. This slice
does not authorize production changes or Phase 2 acceptance.

The adapter in the retained Phase 2 `gate-runtime/gate-observer.c` receipt is
diagnostic test tooling, not a production oracle or harness integration.
Its current purpose is the private clock/permission protocol specified by
`GATE-SLICE.md`. Test that boundary separately from `clock-gate.c`.

Requirement linkage: FR-VICE-001 AC2/AC4, TR-VICE-ORACLE-001 AC2/AC3/AC5,
TEST-VICE-001 AC2/AC4. These mocks are supporting evidence only. They cannot
satisfy ACs requiring real immutable-VICE observations or source integrity.
The current effective requirements and every AC remain pending/false.

## Locked mock scope

`IStdioAdapterProbe.Run(AdapterInput)` returns whether execution returned to
native code, a nullable process exit status, and an ordered trace of the
gate protocol's stderr/input/termination interactions. The shared fixture
catalog owns literal protocol strings, expected statuses and event order;
the provider supplies only observations.

- Publish `VICE_GATE,<uint64>\n` on stderr, then flush stderr before reading
  exactly one stdin byte. A nonnegative fprintf result succeeds; any negative
  result fails publication with status 64. Nonzero fflush fails with 65.
- Read EOF with ferror zero as status 66, nonzero ferror as 70. Do not query
  ferror after a valid byte. Lowercase c returns to native code; q stops with
  67; other bytes stop with 68.
- Every nonzero result attempts `VICE_GATE_STOP,<status>\n` and a stderr flush,
  then terminates with the original status without returning to native code.
  Failure of stop-message output or flushing must not permit execution.
- Cover zero and maximum clocks, positive/zero/negative write returns,
  both signs of nonzero flush/ferror results, EOF, quit, uppercase, newline,
  NUL, high byte, and failures while reporting a stop.

Each fixture must pass through correct mocks. Negative controls must reject
null results/traces, wrong return/exit state, extra/omitted/duplicated events,
wrong operation/stream/value, and each adjacent event-order reversal. The
controls assert expected rejection inside a passing test, never a red gate.

## Sequence and receipts

1. Add the reusable fixture catalog, provider seam and assertions first.
2. Run the entire native-safe clock project plus all prior oracle and VSF
   suites, unfiltered Release, with retained TRX, diagnostics, stdout/stderr
   and compiler logs. Require zero failed, skipped, aborted, errors/timeouts.
   Bracket every build/test with the unchanged native inventory.
3. Submit these additive test inputs and receipts to the same Opus max
   catch-up review. No new adapter behavior or harness integration before
   the required independent gate acceptance.
4. At the authorized real-binding stage, use the identical fixture inputs,
   literal expectations and supplied-provider assertion with the actual C
   adapter under scripted stdio. Preserve original adapter bytes. Mocks
   alone do not complete that stage or resolve all of F-12.

## Explicitly still open

Native stdout retention, buffering corrections, real broken-pipe behavior,
all-device freeze and actual parity observations require their own real
evidence. This trace deliberately covers gate protocol interactions only;
it does not require native stdout to remain unflushed or endorse its loss.
The seven empty historical stdout logs stay unchanged and incomplete.

The existing host probes and core-gate tests are not a substitute for binding
this adapter contract to real C. All original real-emulator, legacy VSF and
whole-plan gates remain mandatory. No requirement or phase is marked done.
