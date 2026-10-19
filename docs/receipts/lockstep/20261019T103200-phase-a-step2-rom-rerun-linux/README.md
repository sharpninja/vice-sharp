# Phase A step 2 follow-up, LAB-OMARCHY (Linux), 2026-10-19: ROM root rerun

Plan: `docs/receipts/handoff/HANDOFF.lockstep-plan.v2.md`, section 11 sequence step 2 (set `VICESHARP_ROM_PATH`, re-run the acceptance and VIC-20 filters). Measured by Claude Code (claude-fable-5-1) on LAB-OMARCHY, workspace `/home/sharpninja/github/vice-sharp`, branch `develop`, HEAD `6fb84afcc1ab980c4b40525efe23324183a3c8f4` with the uncommitted `PaletteId` test-helper fix in the working tree, 2026-10-19T10:29:28 to 10:29:49 local. Script: `~/codex-dispatch/scripts/Invoke-PhaseAStep2RomRerun.ps1` (analyzer 0 findings). Diagnostic class only: no requirement, TODO or acceptance state changed, no hostile review claimed.

## ROM root

No VICE package or data root exists on this host (`pacman -Qs vice` lists only `vicesharp 0.1.751-1`; no `x64sc` or `xvic` on PATH; `VICESHARP_ROM_PATH` was unset). The VICE submodule checkout carries the GPL ROM sets under `native/vice/vice/data/` (`C64/` 10 files, `VIC20/` 6 files, `DRIVES/` 14 files; listing with SHA256 prefixes in `~/codex-dispatch/diag/` via `Show-SubmoduleRomData.ps1`). The six VIC-20 hashes match the `romHashes` entries of `docs/requirements/oracles/vic20-xvic-manifest.json`. `ViceDataPathResolver` accepts the directory because it contains `C64/`. The runs below set `VICESHARP_ROM_PATH=/home/sharpninja/github/vice-sharp/native/vice/vice/data` for the test process only (read-only use; the native inventory is unchanged, 163 entries, SHA256 `A31D0C086A9A36D1...` before and after).

## Runs (Release, `--no-build` against the 2026-10-18 build)

- `FullyQualifiedName~AcceptanceTests` (`harness-acceptance-romroot.trx`): 210 total, 210 passed, 0 failed, 0 skipped. The 2026-10-18 failure `FrMon005AcceptanceTests.DriveView_ShowsDriveMemory` (missing 1541 ROM set) passes with the ROM root set.
- `Vic20NativeLockstepTests` plus `Vic20HalfCycleComparatorTests` (`harness-vic20-romroot.trx`): 19 total, 0 passed, 19 failed, 0 skipped. Every failure is the same prerequisite error: "Required xvic executable is missing: '/home/sharpninja/github/vice-sharp/docs/requirements/oracles/C:\Users\kingd\AppData\Local\...\GTK3VICE-3.10-win64\bin\xvic.exe'". The ROM root did not change this outcome, as expected.

## What the VIC-20 outcome means (observation from `XvicOraclePrerequisites.cs`)

The validator resolves `xvicPath` relative to the manifest directory when it is not rooted on the current host, requires the file, and checks its SHA256 against `viceBuildHash`; it then requires the `vice_xvic` native library (`ViceNativeXvic.ResolvedPath`) and checks it against `viceXvicDllSha256`; then the VIC-20 ROM allowlist under `VICESHARP_ROM_PATH/VIC20`. The `romHashes[].path` fields are not used (names plus the ROM root are). So on Linux the VIC-20 oracle tests need three things, not one: a Linux xvic executable with its own hash, a Linux `vice_xvic` shim with its own hash, and a manifest that carries per-host executable and library entries (or a per-host override) instead of one Windows path. The first two are Phase D outputs (native-safe builds from the clean materialization). The section 10 amendment "the manifest must become host-relative" is therefore necessary but not sufficient; the plan wording is corrected in section 11.

## What this receipt does not claim

No full-suite green. No change to the manifest. No VIC-20 oracle evidence on Linux. The `PaletteId` fix remains uncommitted.
