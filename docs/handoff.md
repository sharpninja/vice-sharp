# 2026-06-25/26 VICE pacing, true-drive load, and renderer handoff

**Branch:** `codex/iec-timetravel-debugger`
**Base before wrap-up:** `dbca07d5818a3e9133798dbb6dea70b0a6e84f5d`
**Status at handoff write:** Ready for commit after retained-evidence focused validation.

## What changed

- Restored VICE pacing semantics so the VICE gate uses live audio back-pressure as the primary regulator before falling back to vsync.
- Changed `Run 8` autostart to type `LOAD"*",8,1` and `RUN` through C64 keyboard automation instead of host-preloading the first PRG into BASIC RAM.
- Made Drive 8 default to the VICE-faithful true-drive path, with attach and persisted restore carrying the D64 path into the rebuilt true-drive session.
- Preserved the user-visible ability to restore persisted true-drive false state even though Drive 8 now defaults true.
- Added durable D64 fixtures under `tests/ViceSharp.TestHarness/Fixtures/D64/` for lockstep and media tests.
- Added semaphore pacing coverage and updated pacing strategy tests so VICE is the default UI/host strategy and warp remains uncapped.
- Updated VIC-II/display/video tests and renderer paths for the observed y-offset/artifact regressions.
- Updated FFmpeg recorder tests for deterministic frame-rate evidence.

## Validation evidence

Retained evidence directory:

`F:\GitHub\vice-sharp\TestResults\wrapup-20260626T213900`

Focused validation command is recorded in `command.txt` and produced:

- `focused.trx`
- `console.log`
- `vstest.diag.log`
- `vstest.diag.host.26-06-25_21-39-22_44959_5.log`
- `vstest.diag.datacollector.26-06-25_21-39-21_81947_5.log`
- `git-diff-check.log`

Result:

```text
Passed: 189
Failed: 0
Skipped: 0
ExitCode: 0
```

`git diff --check` also completed with exit code 0 and is captured in `git-diff-check.log`.

## Red-first evidence retained in session history

Earlier targeted red tests failed before implementation as expected:

- `WarpModeTests.ViceGate_Tick_WithAudioTimingSource_UsesSoundRegulator`: expected `Sound`, actual `Vsync`.
- `HostMediaSimulatedLoadTests.ResetAndAutostartDrive8_TypesLoadCommandInsteadOfPreloadingRun`: expected first typed key `L`, actual `R`.
- Drive 8 default true-drive tests failed before the UI default/rebuild change.

The same focused areas later passed in targeted runs and in the retained 189-test wrap-up run.

## Full harness limitation

The prior full harness run was stopped after no observable progress. The best retained blame sequence evidence points at:

`ViceSharp.TestHarness.X64ScVariantLockstepTests.SelectedD64_PostKernalCloseStateMatchesNative_ForC64Pal`

as the incomplete test in the blame sequence artifacts. A future full-suite run should always use a stable results directory, TRX, `--diag`, `--blame-hang`, and dump collection.

## Next useful checks

- Re-run the installed MSI against the same two user videos after deployment.
- If full-suite proof is required, run the full harness with retained diagnostics and a hang timeout; do not run it as console-only evidence.

---

# ViceSharp - Phase 1 Closeout Handoff

**Date:** 2026-05-31 (marathon pull-in complete)
**HEAD:** `32880a4`
**Branch:** `main` (Azure DevOps `origin`)
**Status:** Phase 1 (Iteration 1 C64 bringup) **COMPLETE**, including all six deferral items that were pulled back into scope mid-session.

## 2026-06-12 Iteration 1 remediation addendum

`ARCH-CHIPGLUE-001` is now treated as an Iteration 1 completion requirement.
The remediation audit is recorded in
`docs/requirements/traceability/ARCH-CHIPGLUE-001-Chip-Audit-2026-06-12.md`
and verifies that shared chip implementations remain reusable chip models while
C64/C1541 board and device glue lives in Core machine/device adapters.

Validation recorded for the remediation:

- Consolidated chip-glue focused gate: 579 passed, 0 failed, 0 skipped.
- x64sc lockstep/checkpoint gate: 335 passed, 0 failed, 0 skipped.
- Source inventory and VICE separation reference captured in the audit document.

## Marathon items shipped (all 7)

| # | Item | Commit(s) | Driving IDs |
|---|---|---|---|
| 1 | AOT YAML fix (YamlStream representation model) | `aecfcff` | ARCH-ADHOCMACHINE-001 AOT gate |
| 2 | PERF-BENCHMARK-001 native VICE baseline | `3f4d65e` | PERF-BENCHMARK-001 |
| 3 | Wiki publishing automation (PowerShell + Nuke) | `d62b578` | REPO-MAINT-001 |
| 4 | 8580 SID filter deepening (real SVF + linear curve) | `371d749` | BACKFILL-SID-001 |
| 5 | 7 advanced cartridge mappers (Magic Desk, Ocean, FC-III, AR, EasyFlash, SS5, RR-Net) | `a2be2b6` `f90ab87` `881d302` `14a19d4` | BACKFILL-CART-001 |
| 6 | ARCH-ADHOCMACHINE-001 Avalonia helper app | `32d45bb` | ARCH-ADHOCMACHINE-001 |
| 7 | PLATFORM-CROSS-001 host shells (macOS, Xbox, Android, iOS) | `5c84bc6` `0aee29c` `73bf823` `32880a4` | PLATFORM-CROSS-001 |
| - | QA-XMLDOCS-001 ratchet retrofit (test docs + Use case + Acceptance markers) | `32d45bb` | QA-XMLDOCS-001 |

## Test totals at closeout

```
dotnet test ViceSharp.slnx --nologo
Passed: 1702 / Skipped: 2 / Failed: 0 (post-marathon; baseline before marathon was 1641)
```

Skipped: two ROM-gated process-smoke tests (gated on Console.exe + ROMs absent on this machine; not a Phase 1 blocker).

Focused lockstep filter (`X64ScVariantLockstep|Lockstep`): 322 passed / 0 failed.

## Performance (PERF-TUNING-001)

Phase 1 target: 246,312 emulated cycles/sec (25% of 985,248 PAL real-time).

Measured via `dotnet run -c Release --project tests/ViceSharp.Benchmarks -- --perf-probe`:

| Budget | Elapsed | Cycles/sec | % realtime |
|--------|--------:|-----------:|-----------:|
| 10,000,000 | 865 ms | 11,557,173 | 1173% |
| 10,000,000 | 706 ms | 14,159,246 | 1437% |
| 20,000,000 | 1144 ms | 17,479,339 | 1774% |

Lowest measurement is 47x the Phase 1 target. No optimisation work landed for the Phase 1 gate.

Machine: PAYTON-LEGION2, net10.0, Release JIT, room-less C64 with one PAL frame warm-up.

## Wiki publishing automation (REPO-MAINT-001)

`tools/Publish-Wiki.ps1` plus the Nuke `PublishWiki` target close the wiki sync gate. The script:

1. Re-exports the requirements wiki ZIP from MCP (`POST /mcpserver/requirements/generate?format=wiki&docType=all`) when `-RegenerateSource` is supplied, then expands into `docs/Project/wiki/{azure,github}/`.
2. Clones each wiki target repo (Azure DevOps + GitHub are both git-backed wikis), mirrors the source dir over, and pushes when there are real changes.
3. Skips a target when its token env var is absent (`ADO_PAT`, `GITHUB_TOKEN`), so the same target is safe to invoke in CI environments that only carry one credential.

Invocation:
```
nuke PublishWiki                                    # full regen + push both targets
pwsh tools/Publish-Wiki.ps1 -DryRun                 # stage clones, show diffs, no push
pwsh tools/Publish-Wiki.ps1 -Target azure -RegenerateSource
```

Smoke test at HEAD (no PATs set): both targets correctly skip with WARN message, exit 0.

## Native VICE baseline (PERF-BENCHMARK-001)

Native VICE comparison wired via `NativeViceBaseline.Run` in `tests/ViceSharp.Benchmarks/`. Drives the native shim's `vice_machine_step_cycle` (warp-mode style raw cycle pumping with no host-clock pacing) and reports cycles/sec. Run via `dotnet run -c Release --project tests/ViceSharp.Benchmarks -- --perf-compare <budget>`.

Measured at HEAD `aecfcff` (PAYTON-LEGION2):

| Layer | Cycles/sec | % PAL realtime |
|-------|----------:|---------------:|
| Managed (ViceSharp `SystemClock.Step`) | 9,995,412 | 1014% |
| Native (VICE shim `vice_machine_step_cycle`) | 552,921 | 56% |
| Ratio managed/native | **18.08x** | (managed is faster) |

Notes:
- Native shim emits a per-cycle checkpoint callback that managed code does not, which is the dominant cost on the native side. A real `step_n_cycles` shim entry without per-cycle callbacks would narrow the gap; deferred.
- Managed is comfortably above the 25% PAL real-time gate (PERF-TUNING-001) at 1014%.
- Both numbers move on every run because the workload is single-threaded JIT-bound; report the lowest of three when treating as a regression gate.

## Phase 1 slices closed in this session

| Slice | Commit | Driving IDs | Summary |
|-------|--------|-------------|---------|
| 1 | `f608d02` | BACKFILL-VIDEO-001, TR-VIC-EDGE-003..006 | VIC-II RC window state machine, non-PAL sprite DMA tables, screen-RAM/sprite-DMA native checkpoints |
| 2 | `aecf6f8` | ARCH-TRUEDRIVE-1541-002, TR-IEC-EDGE-001, TR-DRV-EDGE-001 | IecBus.Tick ATN edge state machine + IecDrive 300k-cycle motor ramp |
| 3 | `42d3a75` | RUNTIME-TAPE-002, TR-TAPE-EDGE-001, TR-TAP-EDGE-001 | Datasette 32k-cycle motor ramp, SenseLine, RecordPressed + TryWritePulse |
| 4 | `261a549` | RUNTIME-SNAPSHOT-002, RUNTIME-CAPTURE-002, FR-SNP-001 AC3/4/5 | VIC-II/CIA-TOD/SID-ADSR snapshot round-trip + PNG/JPEG capture |
| 5 | `f7221f0` | BACKFILL-INPUT-001, FR-INP-001, FR-INP-006 | C64HostKeyboardMapper.LoadFromFile + C64JoystickPort.EnumerateDevices + key-repeat hold test |
| 6 | `f7221f0` | ARCH-TESTBENCH-001, CLI-LAUNCHER-001, FR-CFG-005 AC6-8 | ViceArgsParser.GetHelpText + ViceTopologyBuilder.ParseDescriptor + SkipIfNoBuildArtifact + ROM-less process smoke |
| 7 | `af6dde9` | BACKFILL-LOCKSTEP-001, TR-CYCLE-001 | 10-frame x64sc lockstep depth across all no-cartridge variants |
| 8 | `6086e11` | PERF-TUNING-001 | PerfProbe single-shot cycles-per-second measurement |
| 9 | (this commit) | DOC-DASHBOARD-001 | Handoff + README dashboard refresh + plan.md closeout |

## Post-Phase 1 deferrals (all marathon items landed; only deepening + advanced topics remain)

- **PERF-BENCHMARK-001 deepening**: a real `step_n_cycles` shim path (without per-cycle checkpoint callbacks) to make managed/native cycles/sec directly comparable.
- **ARCH-ADHOCMACHINE-001 helper app deepening**: live YAML schema lint, runtime preview, machine launch button.
- **PLATFORM-CROSS-001 platform-specific TFM enablement**: the Android + iOS shells default to net10.0; flipping to net10.0-android / net10.0-ios requires trimming the AspNetCore transitive dep from ViceSharp.Avalonia. Opt in today with `/p:ViceSharpHost{Android,Ios}Fallback=false` after that dep work lands.
- **BACKFILL-SID-001 (8580 filter deepening)**: real reSID-style 8580 filter coefficient table; voice 3 OSC3/ENV3 read-back differences vs 6581.
- **Advanced cartridge mappers deepening**: Action Replay freeze ROM + EXROM/GAME pin manipulation + LED; EasyFlash $DE02 control register + flash write emulation; Super Snapshot V5 freeze button; RR-Net CS8900A Ethernet at $DE00-$DE0F; FC-III bits 2-6 (LED/NMI/freeze).
- **VSP / AGSP**: VIC sprite/AGSP advanced raster effects are not part of Phase 1.

## Requirements (MCP Server snapshot)

```
FR total: 15
TR total: 14
TEST total: 6
Mappings: 6 (every Phase-1 FR has TR + TEST coverage)
```

Wiki export: `docs/requirements/requirements-wiki-documents.zip` (regenerated at slice-9 close).

## Validation set executed at closeout

```
dotnet build ViceSharp.slnx --nologo               # OK, 0 warnings, 0 errors
dotnet test ViceSharp.slnx --nologo                # 1641 / 2 skip / 0 fail
dotnet test --filter "FullyQualifiedName~X64ScVariantLockstepTests"  # 322 / 0 fail
dotnet publish src\ViceSharp.Console\ViceSharp.Console.csproj -c Release -r win-x64 --self-contained /p:PublishReadyToRun=true  # supported packaging path
git diff --check                                   # clean
```

Native ahead-of-time publishing is no longer a project requirement. Use the supported self-contained managed/ReadyToRun packaging path for console and desktop publish validation.

## Files of note added this session

- `src/ViceSharp.Chips/IEC/IecBus.cs` + `IecDrive.cs`: ATN edge state machine + motor ramp
- `src/ViceSharp.Chips/Tape/Datasette.cs`: motor ramp + SenseLine + record buffer
- `src/ViceSharp.Core/Capture/PngFrameArtifactWriter.cs` + `JpegFrameArtifactWriter.cs`: pure-C# PNG (zlib via ZLibStream) + baseline JFIF JPEG encoders
- `src/ViceSharp.Launcher/ViceArgsParser.cs` + `ViceTopologyBuilder.cs`: help text + descriptor parsing
- `tests/ViceSharp.Benchmarks/PerfProbe.cs`: single-shot cycles/sec measurement
- `tests/ViceSharp.TestHarness/SkipIfNoBuildArtifactAttribute.cs`: build-artifact gate for process smoke
- `tests/ViceSharp.TestHarness/X64ScVariantLockstepTests.cs`: +12 ten-frame lockstep cases
- New test files for IEC timing, drive motor ramp, datasette motor ramp + sense line, snapshot slice 4 coverage, capture slice 4 coverage, joystick port enumeration
