# PLAN-OBSERVER-001 Windows fixture gates rerun with MSYS2 compiles (PAYTON-LEGION2)

Run 2026-10-20 08:22:07 to 08:22:24 local on PAYTON-LEGION2 at develop `131b9394`, the commit in which CompiledCFixture compiles inside the MSYS2 MINGW64 login shell and the bus adapter sink checks `fprintf < 0 || fflush(stderr) != 0`. Fast-forwarded from `4a969214` (7 incoming files, no overlap). Rules: Payton 2026-10-20 08:07 "You should always use msys to compile native VICE code", 08:14 "Fixtures too", 08:19 "The lab rules are to use foreward slashes on Windows". Request from OMAR-VICE# 08:20: BusAdapter 8 with broken-stderr 72, full project 232, compile.log showing `/mingw64/bin/gcc` and `MSYSTEM=MINGW64`.

## Gates

Script: [Invoke-Msys2FixtureRerunWindows.ps1](Invoke-Msys2FixtureRerunWindows.ps1) (forward-slash paths; PSScriptAnalyzer 0 findings). The stale `*-evidence` folders under the test project's `bin/Release/net10.0` were removed before the build, so every compile log here comes from this run (the earlier copies are retained in the earlier receipt folders). dotnet 10.0.402.

- Build, exit 0 ([build.log](build.log)).
- Gate 1, `--filter BusObserver`: total 58, passed 58, failed 0, exit 0 ([gate1-busobserver.trx](gate1-busobserver.trx)).
- Gate 2, `--filter BusAdapter`: total 8, passed 8, failed 0, exit 0 ([gate2-busadapter.trx](gate2-busadapter.trx)). broken-stderr now exits 72 on Windows.
- Gate 3, full ViceSharp.NativeClockProbe.Tests: total 232, passed 232, failed 0, not executed 0, exit 0 ([gate3-full.trx](gate3-full.trx)).

These supersede the counts of the five earlier Windows receipts of today (S1, S1b, S1c, S2a, x64sc hook), whose fixtures were compiled outside MSYS2.

## Compile logs (8 fixtures)

bus-adapter-real, bus-probe, bus-probe-rows, clock-gate, clock-probe, clock-probe-x64sc, stdio-adapter-real, stdio-adapter-scripted: each `compile.log` has ExitCode 0, `resolved compiler: /mingw64/bin/gcc`, `MSYSTEM=MINGW64`, `-Werror`, and no warning or error lines ([summary.txt](summary.txt); executables not copied).

## Open: forward slashes

Every compile.log still shows backslashes in its Executable (`C:\msys64\usr\bin\bash.exe`) and in the gcc Arguments (fixture source and output paths from `Path.Combine`). CompiledCFixture.cs line 16 (`C:\msys64`) and line 27 (`\usr\bin\bash.exe`) build them. This does not satisfy the 08:19 lab rule; reported to OMAR-VICE#. A further rerun is due once the paths are forward slashes.

## native/vice

Not built, cleaned or restored. `git status` of native/vice before and after is identical ([before](native-inventory-before.txt), [after](native-inventory-after.txt), C7848A24).
