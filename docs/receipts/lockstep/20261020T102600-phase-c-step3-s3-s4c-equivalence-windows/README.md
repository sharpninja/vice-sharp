# PLAN-OBSERVER-001 S3 and S4c on Windows: observer equivalence and real VICE_OBS traces (PAYTON-LEGION2)

Runs of `tools/immutable-vice/Invoke-ObserverEquivalence.ps1` on PAYTON-LEGION2 against the S2b Windows binaries (`artifacts/immutable-vice/cc67418cba58e71d8e70fd353e9de47199ad2075/bin-windows`, built 10:23 at develop ab52dfe0: x64sc-observer.exe SHA256 BADAEA41..., xvic-observer.exe 4D5BAC00...) and the stock binaries of the same Make stage. Requested by OMAR-VICE# (10:10 and 10:22): `-Machine both -Cycles 100000`, then 1000000, then x64sc 4000000. Every run: "equivalent: every check passed", exit 0.

## Environment

The runner starts the MinGW-built emulators from .NET; they import libcurl-4.dll, libpng16-16.dll, libstdc++-6.dll and zlib1.dll, which live only in C:/msys64/mingw64/bin, a folder in neither the machine nor the user PATH here. So every run used [Invoke-EquivalenceCleanEnv.ps1](Invoke-EquivalenceCleanEnv.ps1): a fresh `pwsh -NoProfile` whose PATH is the registry PATH with exactly one declared prefix, C:/msys64/mingw64/bin (the parent console's PATH is not inherited). OMAR-VICE# later made the runner declare that path itself (ad296ab9). Running the built binaries is not compiling. Runner parameters: `-seed 1`, `+logcolorize`, `-sounddev dummy`, `VICESHARP_BUS_RAW=1`; trace validation through `ObservationTraceValidationTests`.

## Results (all equal to the Linux reference reported by OMAR-VICE#)

100k, 10:26:09 to 10:29:34 ([20261020T102600-100k](20261020T102600-100k)):
- xvic: gate 99,998 (withheld and keep); access withheld 100,002, keep 47,815, 0 absent from withheld; VICE_OBS 199,996 per gated run; gate sequences identical, 0 clock gaps, 1 reset; PNG EC7D9617BAF8D007DD78BB67FED8F599AFFDE6D5B9629EA7161F7DFCA5E25AE1 for stock, withheld and keep.
- x64sc: gate 99,999; access withheld 100,001, keep 43,503, 0 absent; VICE_OBS 199,998; identical, 0 gaps, 0 resets; PNG 5283DAEF3E770A05C35725A67FD2A2AE318B5A37D1C64A556357473CC8D9E794.
- obs validation 6 passed, 0 failed.

1M, 10:29:56 to 11:07:15 ([20261020T103000-1m](20261020T103000-1m)):
- xvic: gate 999,997; access withheld 1,000,001, keep 401,603, 0 absent; VICE_OBS 1,999,994; identical, 0 gaps, 1 reset; PNG DD3221CF4FE634838990314BF498AAF091287A2DDD5B8F0CF869A156F99D8709.
- x64sc: gate 999,999; access withheld 1,000,001, keep 439,497, 0 absent; VICE_OBS 1,999,998; identical, 0 gaps, 0 resets; PNG 5283DAEF3E770A05C35725A67FD2A2AE318B5A37D1C64A556357473CC8D9E794.
- obs validation 6 passed, 0 failed.

x64sc 4M, 12:14:54 to 13:26 ([20261020T121500-4m-x64sc](20261020T121500-4m-x64sc)):
- gate 3,999,998; access withheld 3,891,785, keep 1,479,503, 0 absent; VICE_OBS 7,999,996 per gated run; identical, 0 gaps, 0 resets; PNG 308BEE06176ECBB5CFAE3B6C52760BBB5A831C017A60A7EC69FFFFEAA5A5779F for all three.
- Validation: withheld 7,891,781 records (PHI1 3,999,998, PHI2 3,891,783), 108,215 idle PHI2 with missing data, 216,430 stolen; keep 5,479,500 records (PHI2 1,479,502), 2,520,496 idle PHI2, 216,430 stolen; 0 other errors and 0 sequence faults in both; obs validation 4 passed, 0 failed.

In no run is there a STOP line, a missing or foreign report, or an access with kind missing. The headless renderer produced byte-identical PNGs on both hosts.

## Files

Each run folder holds the runner's `equivalence.json`, `console.log`, `obs-validation.log`, the `*.obs-validation.json` files, the three PNGs, and `trace-hashes.txt` (SHA256 and size of every stderr trace). The traces themselves (65 MB, 679 MB and 1.38 GB per folder) stay in the gitignored artifacts tree, retained.

Runtime note: most of the wall time is the runner's per-line `Where-Object` counting over the stderr traces (about 8 minutes per 1M gated trace); a single-pass counter would cut it, pending Payton's go.
