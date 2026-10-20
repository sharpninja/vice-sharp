# PLAN-OBSERVER-001 S2a plus the x64sc per-cycle hook: Windows gates (PAYTON-LEGION2)

Run 2026-10-20 08:12:17 to 08:12:29 local on PAYTON-LEGION2 at develop `4a969214` (x64sc hook, `--wrap=vicii_cycle`). Requested by OMAR-VICE# at 08:09: full project expect 229 (Linux) and `--filter BusAdapter` 8.

## Gates

Script: [Invoke-S2aX64scHookWindows.ps1](Invoke-S2aX64scHookWindows.ps1) (copied from the S2a Windows receipt script; collects every `<name>-evidence` folder; PSScriptAnalyzer 0 findings). Its header comment was corrected after the run (comment only, no code change). dotnet 10.0.402; gcc 15.2.0 (MSYS2 Rev8).

- Gate 1, `--filter BusAdapter`: total 8, passed 7, failed 1, exit 1 ([gate1-busadapter.trx](gate1-busadapter.trx)).
- Gate 2, full project: total 229, passed 228, failed 1, exit 1 ([gate2-full.trx](gate2-full.trx)).
- The one failure in both is the same as in the S2a Windows receipt: broken-stderr exits 65, the fixture accepts only 72. Root cause and probe: [../20261020T080500-phase-c-step3-s2a-bus-adapter-windows/README.md](../20261020T080500-phase-c-step3-s2a-bus-adapter-windows/README.md). The fixture was not changed.
- The x64sc hook tests pass; `clock-probe-x64sc-evidence/compile.log`: ExitCode 0, no warning or error lines, `-Wall -Wextra -Werror`. Every other compile log is also ExitCode 0 with no warnings ([summary.txt](summary.txt)).
- native/vice `git status` identical before and after (C7848A24).

## Status: rerun pending (MSYS2 ruling)

Operator ruling 2026-10-20 08:14 ("Fixtures too", extending the 08:07 directive "You should always use msys to compile native VICE code"): the C fixtures must be compiled inside the MSYS2 MINGW64 environment. The fixtures in this run were compiled by CompiledCFixture calling `C:\msys64\mingw64\bin\gcc.exe` directly from .NET, outside MSYS2, so these gate results do not satisfy the rule. The counts stand as a record of what ran; the Windows gates will be rerun after CompiledCFixture compiles through MSYS2 (requested from OMAR-VICE# at 08:15).
