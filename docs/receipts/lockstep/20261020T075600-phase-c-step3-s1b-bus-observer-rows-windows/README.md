# PLAN-OBSERVER-001 S1b row-aware bus observer: Windows gates (PAYTON-LEGION2)

Run 2026-10-20 07:56:57 to 07:57:05 local on PAYTON-LEGION2 at develop `9a7ab550` (S1b mock gate `0125915c`, S1b wrapper `6197252f`, then the S1 Windows receipt). Requested by OMAR-VICE# at 07:52: `--filter BusObserver` 50 passed with the `-DVICE_SHARP_ROWS=32` fixture `bus-probe-rows` compiled beside `bus-probe`; full project 203 per its 07:52 correction.

## Gates

Script: [Invoke-S1bBusObserverWindows.ps1](Invoke-S1bBusObserverWindows.ps1) (copied from the S1 Windows receipt script, extended to collect both evidence folders; PSScriptAnalyzer 0 findings). dotnet 10.0.402; gcc 15.2.0 (MSYS2 Rev8) at `C:\msys64\mingw64\bin\gcc.exe`; `VICE_NATIVE_CC` unset.

- Build: `dotnet build tests\ViceSharp.NativeClockProbe.Tests\ViceSharp.NativeClockProbe.Tests.csproj -c Release`, exit 0 ([build.log](build.log)).
- Gate 1: `dotnet test ... -c Release --no-build --filter BusObserver`: total 50, executed 50, passed 50, failed 0, not executed 0, exit 0 ([gate1-busobserver.trx](gate1-busobserver.trx), [log](gate1-busobserver.log)).
- Gate 2: the full ViceSharp.NativeClockProbe.Tests project: total 203, executed 203, passed 203, failed 0, not executed 0, exit 0 ([gate2-full.trx](gate2-full.trx), [log](gate2-full.log)).

Both counts equal the Linux counts OMAR-VICE# reported (50 and 203).

## Compile logs

- [bus-probe-evidence/compile.log](bus-probe-evidence/compile.log): ExitCode 0, no warning or error lines, `-std=c11 -Wall -Wextra -Werror`, no `VICE_SHARP_ROWS` define.
- [bus-probe-rows-evidence/compile.log](bus-probe-rows-evidence/compile.log): ExitCode 0, no warning or error lines, `-std=c11 -Wall -Wextra -Werror` plus `-DVICE_SHARP_ROWS=32`.

The probe executables (bus-probe.exe, bus-probe-rows.exe) stay on PAYTON-LEGION2 and are not copied here; their SHA256 and every log's hash are in [summary.txt](summary.txt). The per-run logs from a first run at 07:56:25 are also in the evidence folders: that run had the same gate counts (50 and 203, exit 0) but a defective summary line for the compile logs (an operator-precedence slip in the script), so the script was fixed and the gates re-run; summary.txt is from the re-run.

## native/vice

Not built, cleaned or restored. `git status` of native/vice before and after is identical ([before](native-inventory-before.txt), [after](native-inventory-after.txt), SHA256 C7848A24... both).

## Status: rerun pending (MSYS2 ruling)

Operator ruling 2026-10-20 08:14 ("Fixtures too", extending the 08:07 directive "You should always use msys to compile native VICE code"): the C fixtures must be compiled inside the MSYS2 MINGW64 environment. The fixtures in this run were compiled by CompiledCFixture calling `C:\msys64\mingw64\bin\gcc.exe` directly from .NET, outside MSYS2, so these gate results do not satisfy the rule. The counts stand as a record of what ran; the Windows gates will be rerun after CompiledCFixture compiles through MSYS2 (requested from OMAR-VICE# at 08:15).
