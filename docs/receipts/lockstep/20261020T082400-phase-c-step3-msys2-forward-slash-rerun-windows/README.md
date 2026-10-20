# PLAN-OBSERVER-001 Windows fixture gates: MSYS2 compiles with forward-slash paths (PAYTON-LEGION2)

Run 2026-10-20 08:23:54 to 08:24:12 local on PAYTON-LEGION2 at develop `8c426015` ("forward slashes for every Windows path handed to MSYS2"), fast-forwarded from `131b9394` (3 incoming files, no overlap). This is the compliant Windows run under all three operator rules of 2026-10-20: 08:07 "You should always use msys to compile native VICE code", 08:14 "Fixtures too", 08:19 "The lab rules are to use foreward slashes on Windows". Request from OMAR-VICE# 08:22 and 08:23.

## Gates

Script: [Invoke-Msys2FixtureRerunWindows.ps1](Invoke-Msys2FixtureRerunWindows.ps1), copied from the 131b9394 rerun receipt with the head updated and the argument check refined: the element after `-lc` is the bash script, whose `printf` `\n` escapes are backslashes by design, so it is excluded and every other argument must be backslash-free. PSScriptAnalyzer 0 findings. Stale `*-evidence` folders under `bin/Release/net10.0` were removed before the build. dotnet 10.0.402.

- Build, exit 0 ([build.log](build.log)).
- Gate 1, `--filter BusObserver`: total 58, passed 58, failed 0, exit 0 ([gate1-busobserver.trx](gate1-busobserver.trx)).
- Gate 2, `--filter BusAdapter`: total 8, passed 8, failed 0, exit 0 (broken-stderr exits 72) ([gate2-busadapter.trx](gate2-busadapter.trx)).
- Gate 3, full ViceSharp.NativeClockProbe.Tests: total 232, passed 232, failed 0, not executed 0, exit 0 ([gate3-full.trx](gate3-full.trx)).

## Compile logs (8 fixtures)

bus-adapter-real, bus-probe, bus-probe-rows, clock-gate, clock-probe, clock-probe-x64sc, stdio-adapter-real, stdio-adapter-scripted: each `compile.log` has ExitCode 0, `resolved compiler: /mingw64/bin/gcc`, `MSYSTEM=MINGW64`, `-Werror`, no warning or error lines, `Executable: C:/msys64/usr/bin/bash.exe`, and no backslash in any gcc argument ([summary.txt](summary.txt); executables not copied). Environment line: `MSYSTEM=MINGW64 CHERE_INVOKING=1 MSYS2_PATH_TYPE=minimal`.

These results supersede today's earlier Windows fixture receipts (S1, S1b, S1c, S2a, x64sc hook: compiled outside MSYS2; 131b9394 rerun: MSYS2 but backslash paths).

## native/vice

Not built, cleaned or restored. `git status` of native/vice before and after is identical ([before](native-inventory-before.txt), [after](native-inventory-after.txt), C7848A24).
