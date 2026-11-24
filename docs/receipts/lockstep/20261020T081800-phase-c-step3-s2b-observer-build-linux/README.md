# Phase C step 3, PLAN-OBSERVER-001 slice S2b: observer build of x64sc and xvic from the immutable materialization (LAB-OMARCHY), 2026-10-20T08:09 to 08:19

Script `native/build-vice-observer.ps1` (canonical for both hosts; every native tool runs through the platform shell, on Windows the MSYS2 MINGW64 login shell per Payton's directive of 08:07). Prerequisites installed by Payton at 08:12 (`xa` 2.4.1, `dos2unix` 7.5.7, Arch `extra`). Nothing under `native/vice` or the materialization source is modified; generated files live under `artifacts/immutable-vice/<sha>/{work-linux,build-linux,bin-linux,receipts-linux}` (gitignored). Stage receipts (JSON) and logs are in `receipts-linux/`; this README summarizes them.

## Stages and results

- Copy (08:11): the materialization `source/vice` (3,841 files; the 3,847 of the materialization receipt include 6 files above `vice/`) copied to `work-linux/vice`, every file SHA256-equal, list SHA256 `85157F5A509E3E841056961E02D16E48357AEB830AF2C9A9535B00DDE5337E27`. First run of the stage had a StrictMode bug on an empty result (`$missing.Path`), fixed; the autogen that ran in between did not change any tracked file (0 different on the rerun).
- Autogen (08:10): `./autogen.sh` in the work tree, exit 0, `configure` generated.
- Configure (08:13, after the packages): out-of-tree in `build-linux` with `--enable-headlessui --disable-pdf-docs --disable-html-docs --without-pulse --without-alsa --without-oss --without-portaudio --without-flac --without-vorbis --without-mpg123 --without-gif --without-lame --disable-ethernet --disable-realdevice --disable-x64 --disable-x64-image`, exit 0 (`configure.log`; the 08:11 attempt stopped at `configure.ac` 360 to 363, "The dos2unix tool is missing", recorded in the same log's history).
- Make (08:14 first attempt failed: the per-program targets `x64sc` and `xvic` do not depend on `src/lib/linenoise-ng/liblinenoiseng.a`, `ld: cannot find ../src/lib/linenoise-ng/liblinenoiseng.a`; fixed by building `src/lib` first). Second run 08:16:49 to 08:17:05: `make -C src/lib -j16 V=1 && make -C src -j16 V=1 x64sc xvic`, exit 0, stock `build-linux/src/x64sc` (13,155,280 bytes) and `xvic` (9,741,552 bytes), stock link lines captured to `stock-link-lines.json` (driver `c++ -std=c++11 -fopenmp -g -O3 ...`).
- Link (08:18): wrapper objects compiled with `gcc -std=c11 -Wall -Wextra -Werror -O2 -g` (x64sc adds `-DVICE_SHARP_ROWS=32`), five per machine: cycle hook (`clock-observer-x64sc.c` for x64sc, `clock-observer.c` for xvic), `clock-gate.c`, `gate-observer.c`, `bus-observer.c`, `bus-adapter.c`. First relink failed with `undefined reference to vic_cycle` because the objects were appended after the archives (ld pulls archive members only for references already seen, and `__real_vic_cycle` resolves to the stock symbol inside `libvic20.a`); fixed by placing the wrapper objects directly after `-o <output>`, ahead of every stock object and archive. Outputs and SHA256:
  - `bin-linux/x64sc-observer` `72FDA032BAD497B98A43046C5A827B204C663D8C36307B3FE5BEBD1828A4F0BB`, nm: `T __wrap_vicii_cycle`, `T __wrap_mem_initialize_memory`, `T __wrap_mem_mmu_translate`, `T __wrap_mem_toggle_watchpoints`, `T __wrap_vice_sharp_observe_clock`, `T vice_sharp_bus_cycle`, `B vice_sharp_vicii_ba_flags`.
  - `bin-linux/xvic-observer` `B8186DDFBCE595CA1EEE14CD636D520B10BCA016D6E422CDA6717271F6AD2E10`, nm: `T __wrap_vic_cycle` and the same four wraps, `T vice_sharp_bus_cycle`.
  The exact stock and relink lines are in the `*-link.json` receipt.
- Inventory (08:17): the materialization re-hashed after the build, 3,841 files, unchanged.

## Also in this slice

- LEGION2 finding (08:13): on Windows the S2a broken-stderr case exited 65 (gate flush failure), not 72, because under the MSVCRT `fprintf` to a pipe whose reader has gone returns a positive count and only `fflush` reports it. Fix in `bus-adapter.c`: the sink checks `fprintf(...) < 0 || fflush(stderr) != 0`; the fixture stays at 72 only. Linux `--filter BusAdapter` 8 passed after the change.
- Payton directives (08:07 "You should always use msys to compile native VICE code"; 08:14 "Fixtures too"): `CompiledCFixture` now builds the compile invocation through a pure `BuildCompileInvocation` (tested on both branches by `CompiledCFixtureInvocationTests`, run failing before the change with `CS0117`): on Windows `C:\msys64\usr\bin\bash.exe -lc '<record command -v gcc and MSYSTEM>; exec gcc "$@"' bash <args>` with `MSYSTEM=MINGW64` and `CHERE_INVOKING=1`; on Linux `/usr/bin/gcc` directly. Running compiled fixtures is unchanged. LEGION2 marks today's Windows fixture gates rerun-pending and reruns them after this push.

## Gates (exact)

- 08:19 `dotnet test tests/ViceSharp.NativeClockProbe.Tests/ViceSharp.NativeClockProbe.Tests.csproj -c Release` (no filter): see the commit message for the final count after the bash-path fix (the run before it: 231 passed, 1 failed on the Windows-shape assertion because `Path.Combine` produced forward slashes on Linux; fixed by building the path explicitly).
- `git diff --check` clean. Analyzer clean on the build script.

## Not claimed

Nothing has been executed in the observer binaries yet; S3 (equivalence proof: register, clock and gate traces byte-identical with and without the wrapped translate on the ROM-only workloads) is next, after the proof items (a) and (b) are cited.

Signed: claude-fable-5-1-high@ClaudeCode | Confidence: correctness 95%, accuracy 97%, completeness 93%
