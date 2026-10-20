# PLAN-OBSERVER-001 S1 bus observer: Windows gates (PAYTON-LEGION2)

Run 2026-10-20 07:48:30 to 07:48:41 local on PAYTON-LEGION2 at develop `7dbfd291`, after fast-forwarding from `4e3c6d6e` (14 incoming files, no overlap with the working tree). Requested by OMAR-VICE# at 07:46 to mirror the Linux receipt `20261020T074300-phase-c-step3-s1-bus-observer-linux`.

## Gates

Script: [Invoke-S1BusObserverWindows.ps1](Invoke-S1BusObserverWindows.ps1) (PSScriptAnalyzer 0 findings). dotnet 10.0.402; gcc 15.2.0 (MSYS2 Rev8) at `C:\msys64\mingw64\bin\gcc.exe`; `VICE_NATIVE_CC` unset.

- Build: `dotnet build tests\ViceSharp.NativeClockProbe.Tests\ViceSharp.NativeClockProbe.Tests.csproj -c Release`, exit 0 ([build.log](build.log)).
- Gate 1: `dotnet test ... -c Release --no-build --filter BusObserver`: total 22, executed 22, passed 22, failed 0, not executed 0, exit 0 ([gate1-busobserver.trx](gate1-busobserver.trx), [log](gate1-busobserver.log)).
- Gate 2: the full ViceSharp.NativeClockProbe.Tests project: total 175, executed 175, passed 175, failed 0, not executed 0, exit 0 ([gate2-full.trx](gate2-full.trx), [log](gate2-full.log)).

Both counts equal the Linux receipt (22 and 175).

## compile.log

[bus-probe-evidence/compile.log](bus-probe-evidence/compile.log) (from `bin\Release\net10.0\bus-probe-evidence`, 818 bytes): gcc `-std=c11 -Wall -Wextra -Werror -O0 -g -Wl,--wrap=vic_cycle -Wl,--wrap=mem_initialize_memory -Wl,--wrap=mem_mmu_translate` over bus-driver.c, native-bus-double.c, clock-observer.c and bus-observer.c; ExitCode 0, TimedOut False, empty STDOUT and STDERR. [compile-after-gate1.log](compile-after-gate1.log) is the gate 1 copy; it differs from the final copy only in its timestamp line (gate 2 recompiled). The per-run logs are in [bus-probe-evidence](bus-probe-evidence). The probe executable (bus-probe.exe, 506,636 bytes) stays on PAYTON-LEGION2 and is not committed; its SHA256 and every log's hash are in [summary.txt](summary.txt).

## native/vice

Not built, cleaned or restored. The test project only copies `tools/immutable-vice/*.c|h` and `Fixtures/*.c|h` to its output (no project reference). `git status` of native/vice before and after the run is identical ([before](native-inventory-before.txt), [after](native-inventory-after.txt), SHA256 C7848A24... both).

## Design review notes (sent to OMAR-VICE#)

1. Correct as claimed for a single row layout: the patched rows are the ones the dummy pointers resolve to after the real rebuild, and watch rows delegate to them, so each access ends in exactly one trampoline.
2. Hazard before S1b, x64sc only: `dummy_read_row` and `dummy_write_row` call `saved_read[page]` and `saved_write[page]`, the originals of the row captured at the last `mem_initialize_memory`. On x64sc a `$01` banking change re-aliases the dummy pointers to another config row, and the next `vice_sharp_bus_cycle` points them back at the wrapper rows. From then on, dummy cycles read and write through the old config's handlers, for example RAM instead of I/O, so side-effecting dummy reads such as the CIA ICR change emulation, not just observation. S1b's per-config saved originals close this; until then S1 must not be linked into x64sc.
3. Noise: `vice_sharp_bus_cycle` emits `foreign` whenever `_mem_read_tab_ptr` differs from the patched row. With watch flag 1 the real pointer legitimately points at the delegating watch rows (header lines 153 to 155), so every cycle reports `foreign read` although nothing is foreign. Suggest admitting the watch row as a known delegate or giving it its own kind, so a gate that treats `foreign` as failure stays usable with watchpoints on.
4. Minor: the `translate` detail prints `*base != NULL` after setting `*base = NULL`, so it is always 0; logging the real base before the override would carry information.

## Status: rerun pending (MSYS2 ruling)

Operator ruling 2026-10-20 08:14 ("Fixtures too", extending the 08:07 directive "You should always use msys to compile native VICE code"): the C fixtures must be compiled inside the MSYS2 MINGW64 environment. The fixtures in this run were compiled by CompiledCFixture calling `C:\msys64\mingw64\bin\gcc.exe` directly from .NET, outside MSYS2, so these gate results do not satisfy the rule. The counts stand as a record of what ran; the Windows gates will be rerun after CompiledCFixture compiles through MSYS2 (requested from OMAR-VICE# at 08:15).
