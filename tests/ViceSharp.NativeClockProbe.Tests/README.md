# External native cycle-hook proof

Decision locked 2026-10-08T10:27:09-05:00 before production wrapper code.
Approved PLAN-VICE-IMMUTABLE-001 Phase 2, FR-VICE-001 AC2/AC4,
TR-VICE-ORACLE-001 AC2/AC3/AC5 and TEST-VICE-001 AC2/AC4.

The smallest external link-time probe wraps the existing `vic_cycle(void)`.
It calls the real function exactly once, then calls an external observer with
the actual `maincpu_clk`. It must preserve native side effects and call order.
Pinned `vice/src/vic20/vic20cpu.c:43` increments that clock before `vic_cycle`;
`vice/src/types.h:61` defines CLOCK as uint64_t. This is not a complete bus probe,
half-cycle classifier, snapshot substitute or accepted cycle-control gate.

Gate order:

1. Correct mocks satisfy independent ordered event fixtures. Incorrect mocks
   must be rejected for exit code, missing/extra/reordered events, wrong phase,
   wrong observed clock and input mutation, inside a passing test harness.
   Cover empty sequences, zero, UInt64 maximum, changes made by the native
   callback and nonmonotonic clocks. All prior native-safe suites remain green.
2. Only after that gate, add external C wrapper code. Bind identical assertions
   to a standalone compiled C fixture using the real linker's `--wrap=vic_cycle`.
   The fixture supplies an independent native-call double and observer. It is
   not VICE and cannot prove native runtime behavior. Preserve compiler and
   execution receipts. No project references may invoke VICE build targets.
3. Only after cumulative green, link an external probe executable from the
   unchanged stock object/archive inputs and new external wrapper inputs.
   Stock executable, all original sources and `native/vice` stay unchanged.
   Missing actual bus observations still fail closed. No native source patch,
   software instruction patch, comparator exclusion or acceptance claim.
4. Noon local Opus catch-up reviews this entire slice and retained failures.

The test project is intentionally outside the main solution until native-safe
harness integration is approved and verified. All real emulator gates remain.

The subsequent permission-gate increment is documented in `GATE-SLICE.md`.
Latest full project result is 50/50 with prior 199/199 and 82/82, not a change
to the original six clock-hook fixtures. The chronology below retains the
earlier clock-only gate counts.

## Chronological evidence

Receipt root: `docs/receipts/native-vice/20261008T082100-0500-phase2-feasibility/`.

- Before C implementation: `clock-mock-01/result.json`, 6/6 passing;
  `clock-prior-vsf-01/result.json`, 199/199; and
  `clock-prior-oracle-01/result.json`, 82/82.
- After C implementation and real bindings: `clock-implementation-01/result.json`,
  12/12 passing; `clock-prior-vsf-02/result.json`, 199/199; and
  `clock-prior-oracle-02/result.json`, 82/82.
- All six runs are full unfiltered Release suites, exit 0, zero failed,
  skipped/not-executed, aborted, errors or timeouts. Each result retains its
  exact command, TRX, diagnostic log and native-integrity checks.
- `clock-fixture-evidence/` preserves compile and six execution logs copied
  with hash verification from test output. The originals are retained.
- `clock-runtime/README.md` indexes the subsequent real stock-xvic link and
  callback-clock observations. Those observations are not bus/parity proof.

The retained `clock-code-verify-02` failure was xUnit1013 on a public assertion
helper; making that helper internal fixed it without suppressing the rule.
An initial PowerShell compiler invocation failed to parse before executing.
The first correctly quoted direct compiler invocation exited 1 with no binary;
the same arguments succeeded with the MinGW directory in the child PATH.
`clock-fixture-build-01.json` and `clock-fixture-build-02.json` preserve both
results. No failure or stopped process is counted as a passing gate.

Reproduction (through the workspace PowerShell.MCP console):

```powershell
dotnet test tests/ViceSharp.NativeClockProbe.Tests/ViceSharp.NativeClockProbe.Tests.csproj -c Release --logger trx --results-directory TestResults/NativeClockProbe
```

The real-binding tests use `VICE_NATIVE_CC` when supplied, otherwise the local
Windows compiler at `C:\msys64\mingw64\bin\gcc.exe` or `/usr/bin/gcc` on Linux.
A missing compiler fails, never skips. Linux execution is not proven by the
retained Windows receipts. The test process prepends the compiler directory
only to child PATH; it does not install tools or change global configuration.
