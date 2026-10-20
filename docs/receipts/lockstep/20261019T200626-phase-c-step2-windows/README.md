# Phase C step 2 on PAYTON-LEGION2: stdio diagnostic adapter bound to a real C binding (Windows)

Goal GOAL-LOCKSTEP-REMEDIATION-001, task "Step 8", Phase C step 2, Windows half (OMAR-VICE# brief 2026-10-19T19:52). D3: no review; self-assessment below. The Linux binding is OMAR-VICE#'s, from the same translation units and the same test cases.

Suite run at HEAD 5bf6e2f7280c904a9a62d72446f5ee867c919e1f plus this step's files (committed together with this receipt). Compiler: MSYS2 `C:\msys64\mingw64\bin\gcc.exe` 15.2.0 by full path (`CompiledCFixture`; `-std=c11 -Wall -Wextra -Werror -O0 -g`). native/vice and artifacts/immutable-vice were not touched.

## Translation units

- `tools/immutable-vice/gate-observer.c`: byte copy of the retained Oct 8 adapter `docs/receipts/native-vice/20261008T082100-0500-phase2-feasibility/gate-runtime/gate-observer.c` (SHA256 9E9DFB8A920C6766 both; original retained). Unmodified.
- `tools/immutable-vice/clock-gate.c` and `clock-gate.h`: unchanged.
- Test-only: `Fixtures/stdio-probe-io.h` (force-included with `gcc -include` to route the adapter's fprintf/fflush/fgetc/ferror/_Exit to recording probes), `Fixtures/stdio-probe-driver.c` (scripted results from the fixture input, trace on stdout), `Fixtures/stdio-real-driver.c` (stands in for native code on real pipes).

## Tests added (`StdioAdapterRealBindingTests.cs`)

- `StdioAdapterRealBindingTests`: the 19 `StdioAdapterFixtures` cases against the real adapter through the scripted seam, with the identical inputs, literal expectations and `AssertAdapter`. 19 passed.
- `RealStdioContractTests` (mock first, new cases): correct and corrupted mocks for the real-pipe contract. 5 passed.
- `StdioAdapterRealProcessTests`: the same real-pipe cases on the real process. 5 passed.

One correction during the step: the broken-stderr fixture first expected exit 64 (my inference that `fprintf` to a closed pipe fails). The real process returned 65. The protocol allows both (64 when fprintf reports the failure, 65 when the runtime buffers it and fflush reports it), and the requirement is that the gate fails closed. The fixture now accepts 64 or 65 and additionally requires that no `NATIVE_AFTER` line appears, which proves execution never returned to native code.

## Full applicable suite (unfiltered, Release)

TRX, console logs, run.log and summary.json in this folder; compiler logs and the raw real-pipe observations (hex) in `c-evidence/`.

- ViceSharp.OracleContract.Tests: 143 total, 143 passed, 0 failed, 0 skipped.
- ViceSharp.VsfCompatibility.Tests: 199 total, 199 passed, 0 failed, 0 skipped.
- ViceSharp.NativeClockProbe.Tests: 153 total, 153 passed, 0 failed, 0 skipped (124 plus 29).
- native/vice inventory: 2,539 entries, SHA256 C7848A24867CD60C before and after: unchanged.

## Measured on Windows (MSVCRT through mingw64)

- Stdout retention: native stdout written before a gate stop is retained. In `continue-then-quit`, stdout holds `NATIVE_BEFORE` and `NATIVE_AFTER,7` after `_Exit(67)`. C11 7.22.4.5 leaves flushing on `_Exit` implementation-defined; glibc does not flush, so the Linux result may differ (plausibly the seven empty Oct 8 stdout logs, F-04).
- Line endings: every protocol line arrives as CRLF (`0D0A`) because stdio is in text mode. The step 1 `VICE_OBS` contract treats CRLF as a framing fault, so the step 3 wrapper must set binary mode on its output stream on Windows. Named item for step 3.
- Broken pipe: a closed stderr ends the process with 65 and no return to native. On Linux, SIGPIPE would terminate the process before the protocol can report 64 or 65 unless SIGPIPE is ignored; the Linux binding must establish that.

## Self-assessment

- Accuracy 98: counts from the TRX files; byte observations from `c-evidence/stdio-adapter-real-evidence/real-stdio-observations.log`.
- Completeness 93, so not a pass. Gaps:
  1. Linux binding not yet run (OMAR-VICE#).
  2. The scripted seam proves the adapter logic; it does not prove the C runtime's own failure reporting. The real-pipe cases cover continue, quit, EOF, uppercase and broken stderr only; real fflush and ferror faults are not inducible here.
  3. The adapter is still diagnostic. It does not flush native stdout before `_Exit`, so retention depends on the C runtime. Changing that would alter the protocol (a new fflush(stdout) event) and needs its own mock-first contract change; not done here.
  4. CRLF on Windows must be handled in step 3.
