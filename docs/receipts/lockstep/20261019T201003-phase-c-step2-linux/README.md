# Phase C step 2, Linux half: the stdio gate adapter bound to real C on LAB-OMARCHY, 2026-10-19T20:10

Plan: `docs/receipts/handoff/HANDOFF.lockstep-plan.md` section 4 Phase C step 2 ("bind the stdio diagnostic adapter to a real C binding; prove stdout retention and broken-pipe behavior"), amended by `HANDOFF.lockstep-plan.v2.md` section 4 ("built and proved on both hosts") and section 11 (D3: self-assessment instead of hostile review until step 12). Goal TODO `GOAL-LOCKSTEP-REMEDIATION-001`, task "Step 8". The Windows half is `docs/receipts/lockstep/20261019T200626-phase-c-step2-windows/` (LEGION2, commit `b2ddf745`). Serves FR-VICE-001 AC2/AC4, TR-VICE-ORACLE-001 AC2/AC3/AC5, TEST-VICE-001 AC2/AC4 as supporting evidence; no requirement, AC or plan step is marked done.

Measured by Claude Code (claude-fable-5-1) on LAB-OMARCHY, workspace `/home/sharpninja/github/vice-sharp`, branch `develop`, HEAD `b2ddf745b70dfd8695c6d39ed5fb6bf8ef714f6b` (LEGION2's step 2 push, fast-forwarded here with no local change), dotnet 10.0.402, gcc 16.2.1 at `/usr/bin/gcc` (the `CompiledCFixture` default; `VICE_NATIVE_CC` unset), between 20:10:03 and 20:10:55 local.

## One harness, not two

A LAB-OMARCHY draft of the same binding (ld `--wrap` interposition of `fprintf`, `fflush`, `fgetc`, `ferror` and `_Exit`; the 19 fixtures bound 19/19; adapter byte pin; three real-pipe tests; focused run 42/42 at 20:07; full suites on the draft tree 143/199/147 at 20:08, inventory unchanged) was written in parallel and overlapped LEGION2's push by file name (`stdio-real-driver.c`), the csproj link line and the identical `gate-observer.c`. The draft was preserved under `~/codex-dispatch/phase-c-step2-20261019/omarchy-draft/` (every file hash-listed in `manifest.json` here), reverted from the tree, and LEGION2's harness adopted as the single binding for both hosts. The adapter under test is `tools/immutable-vice/gate-observer.c`, SHA256 `9E9DFB8A920C6766F372675E442B336C1B46A0B0A44581085ACBE05936437BD5`, byte-identical to the retained Oct 8 adapter `docs/receipts/native-vice/20261008T082100-0500-phase2-feasibility/gate-runtime/gate-observer.c`.

## Focused run on Linux (diagnostic)

`dotnet test tests/ViceSharp.NativeClockProbe.Tests/ViceSharp.NativeClockProbe.Tests.csproj -c Release --nologo --filter "FullyQualifiedName~Stdio" --logger "trx;LogFileName=stdio-focused-linux.trx"` (`focused-linux/`): 48 passed, 0 failed, 0 skipped. That is `StdioAdapterContractTests` 19 (mocks), `StdioAdapterRealBindingTests` 19 (real adapter under the force-included recording seam), `RealStdioContractTests` 5 (mocks of the real-stdio assertion), `StdioAdapterRealProcessTests` 5 (real pipes, real libc, real exit). Native inventory 163 entries, unchanged before and after.

## Full applicable suite at b2ddf745 (gate)

`docs/receipts/lockstep/20261019T201029-phase-c-step2-native-safe-linux/` (`Invoke-NativeSafeSuites.ps1`, TRX, console logs, inventories): OracleContract 143 passed, 0 failed, 0 skipped; VsfCompatibility 199, 0, 0; NativeClockProbe 153, 0, 0; `native/vice` inventory 163 entries, SHA256 `A31D0C086A9A36D1...` before and after. These equal LEGION2's Windows counts (143, 199, 153).

## Linux facts (from `c-evidence/stdio-adapter-real-evidence/real-stdio-observations.log`, decoded)

- `continue-both` (stdin `cc`): exit 0; stderr `VICE_GATE,7` LF `VICE_GATE,8` LF; stdout `NATIVE_BEFORE`, `NATIVE_AFTER,7`, `NATIVE_AFTER,8`, all retained.
- `eof-first` (stdin empty): exit 66; stderr `VICE_GATE,7` LF `VICE_GATE_STOP,66` LF; stdout empty.
- `continue-then-quit` (stdin `cq`): exit 67; stderr `VICE_GATE,7`, `VICE_GATE,8`, `VICE_GATE_STOP,67`; stdout empty, although the first cycle completed and wrote `NATIVE_BEFORE` and `NATIVE_AFTER,7` before the quit.
- `uppercase-first` (stdin `C`): exit 68; stderr `VICE_GATE,7`, `VICE_GATE_STOP,68`; stdout empty.
- `broken-stderr` (stderr closed by the harness before the permission byte): exit 64; stdout empty; no `NATIVE_AFTER` line.

Observation: on Linux every nonzero gate status loses the buffered native stdout, including output from cycles that had already returned to native; only the exit-0 path retains it. Inference: glibc's `_Exit` does not flush stdio streams, and a pipe makes stdout fully buffered, so the loss is the adapter's `_Exit` design, not the harness; this is the mechanism behind F-04's seven empty historical stdout logs. Windows retains the same output (LEGION2's measurement), so the two hosts differ on exactly this point.

Observation: the broken stderr pipe ended with status 64 here, 65 on Windows. Inference: the child inherited SIGPIPE ignored from the .NET test host, so `fprintf` failed with EPIPE and the adapter reported publication failure; a process launched from a shell with the default SIGPIPE disposition would be killed by the signal before any status, so the fixture's 64-or-65 acceptance is launcher-dependent on Linux. Not measured under a shell; reported, fixture unchanged.

Observation: line endings are LF on Linux, CRLF under the Windows MSVCRT text mode (LEGION2); the step 3 `VICE_OBS` contract treats CRLF as a framing fault, so the Windows wrapper must set binary mode (LEGION2's step 3 item).

## Provenance of `c-evidence/`

The compiled-fixture logs are copied from the test output folder, which the draft run at 20:07 and LEGION2's harness runs at 20:10 shared. `real-run-*.log` files (three) are the preserved draft's real-pipe runs; `run-*.log` files and `real-stdio-observations.log` are LEGION2's harness. `manifest.json` names the source path and SHA256 of every file.

## Self-assessment (D3; no hostile review)

Accuracy 98: every count and byte above is from a TRX, a run log or the decoded observations file in this folder; the two inferences are labeled. Completeness 92, not a pass. Gaps: (1) native stdout retention across a nonzero gate status is unproven on Linux and measured as lost; a buffering or termination correction needs its own mock-first contract before any adapter change (STDIO-ADAPTER-SLICE.md), so F-04 stays open; (2) the broken-pipe behavior under a default SIGPIPE disposition is inferred, not measured; (3) the preserved LAB-OMARCHY draft is retained but not reconciled against LEGION2's seam approach (two ways to script stdio; one chosen, the other kept as evidence only); (4) no independent acceptance until step 12. PLAN-VICEORACLE-001 task 3 stays open.

Signed: claude-fable-5-1-high@ClaudeCode | Confidence: correctness 98%, accuracy 98%, completeness 92%
