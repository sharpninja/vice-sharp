# PLAN-OBSERVER-001 S1c watch rows as allowed delegates: Windows gates (PAYTON-LEGION2)

Run 2026-10-20 08:01:33 to 08:01:44 local on PAYTON-LEGION2 at develop `c711365a` (S1c), fast-forwarded from `9a7ab550` (12 incoming files, no overlap with the working tree). Requested by OMAR-VICE# at 08:00: `--filter BusObserver` expect 58, full project expect 211. Mirrors the Linux receipt `20261020T075700-phase-c-step3-s1c-watch-rows-linux`.

## Gates

Script: [Invoke-S1cWatchRowsWindows.ps1](Invoke-S1cWatchRowsWindows.ps1) (copied from the S1b Windows receipt script; PSScriptAnalyzer 0 findings). dotnet 10.0.402; gcc 15.2.0 (MSYS2 Rev8) at `C:\msys64\mingw64\bin\gcc.exe`; `VICE_NATIVE_CC` unset.

- Build: `dotnet build tests\ViceSharp.NativeClockProbe.Tests\ViceSharp.NativeClockProbe.Tests.csproj -c Release`, exit 0 ([build.log](build.log)).
- Gate 1: `dotnet test ... -c Release --no-build --filter BusObserver`: total 58, executed 58, passed 58, failed 0, not executed 0, exit 0 ([gate1-busobserver.trx](gate1-busobserver.trx), [log](gate1-busobserver.log)).
- Gate 2: the full ViceSharp.NativeClockProbe.Tests project: total 211, executed 211, passed 211, failed 0, not executed 0, exit 0 ([gate2-full.trx](gate2-full.trx), [log](gate2-full.log)).

Both counts equal the expected Linux counts (58 and 211).

## Compile logs

- [bus-probe-evidence/compile.log](bus-probe-evidence/compile.log): ExitCode 0, no warning or error lines, `-std=c11 -Wall -Wextra -Werror`, the four wraps including `--wrap=mem_toggle_watchpoints`, no `VICE_SHARP_ROWS` define.
- [bus-probe-rows-evidence/compile.log](bus-probe-rows-evidence/compile.log): ExitCode 0, no warning or error lines, the same flags and four wraps plus `-DVICE_SHARP_ROWS=32`.

The probe executables stay on PAYTON-LEGION2 and are not copied here; their SHA256 and every log's hash are in [summary.txt](summary.txt).

## Citation for the x64sc toggle path (sent to OMAR-VICE#)

x64sc: `src/c64/c64cpusc.c` 217 includes `src/mainc64cpu.c`, whose line 622 assigns `maincpu_monitor_interface->toggle_watchpoints_func = mem_toggle_watchpoints` (object `c64cpusc.o`; the function is defined in `c64memsc.o`, `c64memsc.c` 188), so `--wrap=mem_toggle_watchpoints` redirects it. VIC-20: `src/maincpu.c` 395 against `vic20mem.c` 536.

## native/vice

Not built, cleaned or restored. `git status` of native/vice before and after is identical ([before](native-inventory-before.txt), [after](native-inventory-after.txt), SHA256 C7848A24... both).

## Status: rerun pending (MSYS2 ruling)

Operator ruling 2026-10-20 08:14 ("Fixtures too", extending the 08:07 directive "You should always use msys to compile native VICE code"): the C fixtures must be compiled inside the MSYS2 MINGW64 environment. The fixtures in this run were compiled by CompiledCFixture calling `C:\msys64\mingw64\bin\gcc.exe` directly from .NET, outside MSYS2, so these gate results do not satisfy the rule. The counts stand as a record of what ran; the Windows gates will be rerun after CompiledCFixture compiles through MSYS2 (requested from OMAR-VICE# at 08:15).
