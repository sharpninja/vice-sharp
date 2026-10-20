# PLAN-OBSERVER-001 S2a bus adapter: Windows gates (PAYTON-LEGION2), one finding

Run 2026-10-20 08:05:26 to 08:05:36 local on PAYTON-LEGION2 at develop `a7421a17` (S2a). Requested by OMAR-VICE# at 08:04: `--filter BusAdapter` 8 passed and the full project 219 passed on Linux; "Watch the broken-stderr case: the fixture accepts only exit 72 ... if the MSVCRT buffers stderr differently and you see another status, report it as a finding rather than widening the fixture."

## Gates

Script: [Invoke-S2aBusAdapterWindows.ps1](Invoke-S2aBusAdapterWindows.ps1) (PSScriptAnalyzer 0 findings). dotnet 10.0.402; gcc 15.2.0 (MSYS2 Rev8).

- Gate 1, `--filter BusAdapter`: total 8, passed 7, failed 1, exit 1 ([gate1-busadapter.trx](gate1-busadapter.trx)).
- Gate 2, full project: total 219, passed 218, failed 1, exit 1 ([gate2-full.trx](gate2-full.trx)).
- The one failure in both: `BusAdapterRealProcessTests.Real_adapter_on_real_pipes_matches_the_contract(name: "broken-stderr")`, `Assert.Contains() Failure: Collection: [72] Not found: 65`. The fixture was not changed.
- The other three real-pipe cases match their contracts (exit 0, 67, 66); Windows text-mode stdio writes CRLF line ends ([bus-adapter-real-evidence/bus-adapter-observations.log](bus-adapter-real-evidence/bus-adapter-observations.log)).
- All compile logs: ExitCode 0, no warning or error lines, `-Wall -Wextra -Werror`. native/vice `git status` identical before and after (C7848A24).

## Finding: on msvcrt the sink cannot see the broken pipe; the gate's flush does

- 65 is `clock-gate.c` line 14: `io->flush` failed. So `vice_sharp_bus_sink`'s `fprintf(stderr, ...)` to the closed pipe returned success and execution reached the gate, whose `fflush(stderr)` failed.
- The fixture executables import `msvcrt.dll` (objdump `DLL Name`), MSYS2 mingw64's C runtime.
- Probe ([msvcrt-stderr-probe](msvcrt-stderr-probe)): stderr redirected to a pipe whose read end is closed, compiled and run inside the MSYS2 MINGW64 environment (`/mingw64/bin/gcc`, gcc 15.2.0, `-std=c11 -Wall -Wextra -Werror`).
  - `probe.c`: first `fprintf` returns 23, `ferror` 0; second `fprintf` returns 17, `ferror` 0; `fflush` returns -1. msvcrt buffers stderr when it is a pipe, so the error appears only at the flush.
  - `probe2.c flush`: `fprintf` 23, then `fflush` -1: detected.
  - `probe2.c unbuf` (`setvbuf(stderr, NULL, _IONBF, 0)` first): `fprintf` still returns 23, but `ferror` is 32: detected.
- So on this CRT, `fprintf(...) < 0` alone never detects the failure. Candidate fix for the adapter (OMAR-VICE# decides): `if (fprintf(stderr, ...) < 0 || fflush(stderr) != 0)` in the sink, which reports `VICE_BUS_STOP,72` on both hosts and keeps the event-before-gate order. An equivalent: unbuffered stderr at startup plus an `ferror(stderr)` check after each write.

## Read-only answers for S2b (sent to OMAR-VICE#)

[Resolve-ViceLinkInputs.ps1](Resolve-ViceLinkInputs.ps1) expands the stock link inputs from the materialization's generated `src/Makefile` into [vice-link-inputs.json](vice-link-inputs.json). Self-check: its xvic expansion equals the Oct 8 recorded gated xvic link token for token (114 of 114, identical sequence; `docs/receipts/native-vice/20261008T082100-0500-phase2-feasibility/gate-runtime/link.launch.json`).

- The materialization `artifacts/immutable-vice/cc67418cba58e71d8e70fd353e9de47199ad2075/source/vice` is configured and built: `configure`, `src/Makefile.in` and `config.status` exist; `config.log` line 7: `./configure --enable-option-checking=fatal --enable-headlessui --disable-pdf-docs --disable-html-docs`; 994 `.o` files under `src`.
- Per-emulator targets exist in `src/Makefile`: `x64sc$(EXEEXT):` line 1838, `xvic$(EXEEXT):` line 1862, with recursive library rules (`$(c64sc_lib):` 2568, `$(vic20_lib):` 2766, `$(viciisc_lib):` 2781), so `make -C src x64sc.exe xvic.exe` builds only those two (the `.exe` suffix is needed on Windows).
- The only gated binary on LEGION2 is the Oct 8 `artifacts/immutable-vice/clock-hook/xvic-gate-probe.exe` (SHA256 A15ED974...): `C:\msys64\mingw64\bin\c++.exe -Wl,--wrap=vic_cycle` plus clock-observer.o, gate-observer.o and clock-gate.o, then the stock base objects and libraries, run in the materialization's `src`. No gated x64sc exists on LEGION2.
- x64sc stock inputs: 53 base objects, `midi.o`, 52 LDADD tokens (libc64sc.a, libviciisc.a, libc64scstubs.a and the shared libraries) and `LIBS` `-lm -liconv -lwinmm -lntdll -ld2d1 -ld3d11 -lavrt -lws2_32 -lcurl`; link driver `c++ -std=c++11`, `LDFLAGS` empty. Full lists in vice-link-inputs.json.
- Operator directive 2026-10-20 08:07: native VICE code is compiled through MSYS2 (inside the MSYS2 MINGW64 environment), not by calling gcc.exe or c++.exe directly from PowerShell. The Oct 8 link above called c++.exe directly; S2b should run its configure, make and link inside MSYS2.

## Status: rerun pending (MSYS2 ruling)

Operator ruling 2026-10-20 08:14 ("Fixtures too", extending the 08:07 directive "You should always use msys to compile native VICE code"): the C fixtures must be compiled inside the MSYS2 MINGW64 environment. The fixtures in this run were compiled by CompiledCFixture calling `C:\msys64\mingw64\bin\gcc.exe` directly from .NET, outside MSYS2, so these gate results do not satisfy the rule. The counts stand as a record of what ran; the Windows gates will be rerun after CompiledCFixture compiles through MSYS2 (requested from OMAR-VICE# at 08:15).
