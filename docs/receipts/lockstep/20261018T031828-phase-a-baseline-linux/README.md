# Phase A baseline, LAB-OMARCHY (Linux), 2026-10-18

Plan: `docs/receipts/handoff/HANDOFF.lockstep-plan.v2.md`, Phase A step 2 (Linux baseline). Measured by Claude Code (claude-fable-5-1) on LAB-OMARCHY, workspace `/home/sharpninja/github/vice-sharp`, branch `develop`, HEAD `6fb84afcc1ab980c4b40525efe23324183a3c8f4`, between 2026-10-18T03:18:28 and 03:24:50 local (America/Chicago). Diagnostic class only: no native oracle exists on this machine, no requirement, TODO or acceptance state changed, no hostile review claimed.

## Native tree inventory (unchanged)

`native/vice` gitlink and checkout `cc67418cba58e71d8e70fd353e9de47199ad2075`. `git -C native/vice status --porcelain=v1 -uall` captured before any build (`native-inventory-before.txt`), after the native-safe suites, after the TestHarness build (`native-inventory-after-build.txt`) and after the harness runs (`native-inventory-after-harness.txt`): 163 entries each time, byte-identical (`cmp`). The TestHarness project runs `native/Vice.Native.proj` only when `$(OS) == Windows_NT` (`ViceSharp.TestHarness.csproj` line 72), so the Linux build never touches the native tree. LEGION2 native inventory on the same day: 12 modified, 2,527 untracked (measured over WSMan with the submodule path trusted).

## Native-safe suites, unfiltered, Release

Command per project: `dotnet test tests/ViceSharp.<Project>.Tests/ViceSharp.<Project>.Tests.csproj -c Release --nologo --logger "trx;LogFileName=<Project>.trx" --results-directory <this folder>`.

- OracleContract: 89 passed, 0 failed, 0 skipped (`OracleContract.trx`). The 2026-10-08 Windows figure was 87; two tests were added since.
- VsfCompatibility: 199 passed, 0 failed, 0 skipped (`VsfCompatibility.trx`).
- NativeClockProbe: 69 passed, 0 failed, 0 skipped (`NativeClockProbe.trx`).

## TestHarness build

First build failed: `SettingsRestartLifecycleTests.TrackingVideoSink` does not implement `IVideoCaptureSink.PaletteId` (`testharness-build.log`). The member was added by commit `6530196` (2026-10-16 09:25, PR7 fixes) four minutes after that workspace's last recorded harness run (`TestResults/pr7-resolution/final-harness.trx`, 09:21, 259 passed). The error is platform-independent, so the harness has been unbuildable on every machine since that commit. One-line fix applied in this working tree (uncommitted, see `git diff`): `public string PaletteId { get; set; } = "vice";` mirroring `FrameSequenceCapture` and `FfmpegVideoRecorder`. Second build: 0 warnings, 0 errors (`testharness-build-2.log`).

## TestHarness diagnostic filters, Release, `--no-build`

- Five PR7 CPU/NMI tests (`harness-cpu-nmi-five.trx`): 4 failed, 1 passed. Still failing: `CpuInterruptTests.LdaImm_IrqOnLastClk_DoesNotDispatchBeforeNextFetch`, `CpuInterruptTests.RtsThenLdaImm_NextTick_FetchesFollowingOpcode`, `CpuInterruptTests.Rts_DelayNextFetchTick_DoesNotTakeIrqBeforeReturnFetch`, `Mos6510NmiTests.Nmi_RtiReturnsCpuToPreNmiState`. Now passing: `Mos6510NmiTests.Nmi_IsEdgeTriggered_HoldingLineDoesNotRefire`.
- `Category=Determinism` (`harness-determinism.trx`): 5 passed, 0 failed, 0 skipped.
- `FullyQualifiedName~AcceptanceTests` (`harness-acceptance.trx`): 210 total, 209 passed, 1 failed: `FrMon005AcceptanceTests.DriveView_ShowsDriveMemory`, "Required ROM set for Commodore 1541 is missing or invalid" (no `VICESHARP_ROM_PATH` on this host). This filter is wider than the 17 `Fr*AcceptanceTests` classes of the 2026-09-30 Windows run (98 tests, 52 failed); the two runs are not the same denominator.
- Lockstep filter (`harness-lockstep.trx`, classes LockstepValidationTests, Vic20NativeLockstepTests, Vic20HalfCycleComparatorTests, CpuVisiblePcTimingTests, X64ScVariantLockstepTests): 208 total, 136 passed, 35 failed, 37 skipped.
  - 37 skipped: "Native VICE library not found. Build or copy 'vice_x64'/'vice-shim' into the test output" (LockstepValidationTests 6, X64ScVariantLockstepTests 31). Fail-closed as designed; no shim exists on LAB-OMARCHY and `native/build-vice-shim.sh` builds inside `native/vice`, which is forbidden.
  - 18 failed in Vic20NativeLockstepTests: "Required xvic executable is missing: '/home/sharpninja/github/vice-sharp/docs/requirements/oracles/C:\Users\kingd\AppData\Local\Microsoft\WinGet\Packages\VICE-Team.VICE.GTK3...'". The xvic oracle manifest (`docs/requirements/oracles/vic20-xvic-manifest.json`) carries a Windows absolute path, so `XvicOraclePrerequisites` cannot resolve on Linux. Plan impact: the manifest needs a host-relative or per-host executable location before Phase F can run here.
  - 16 failed in CpuVisiblePcTimingTests (managed-only, no oracle needed): 14 `Assert.Equal` value differences plus `wait-loop irq_delay delta=12 (want 11)` and the `Wolf64 ... BMI dummy INC_PC` pin. These are CPU timing regressions in the merged `develop` beyond the five PR7 failures; they belong to Phase E step 2.
  - 1 failed in Vic20HalfCycleComparatorTests (`Assert.NotNull` on a missing fixture value).
  - 132 passed in CpuVisiblePcTimingTests, 4 passed in X64ScVariantLockstepTests.

## Pins

`lockstep-file-pins-omarchy.txt` and `lockstep-file-pins-legion2.txt`: five of six lockstep files have identical SHA256 on both machines. `LockstepValidator.cs` is 2,050 lines on both; LEGION2's working copy is CRLF (2,050 CR bytes, `w/crlf` despite `eol=lf`), and its LF-normalized hash equals the LAB-OMARCHY hash `B1EF324968E81634...`. Git was configured on both machines on 2026-10-18 (`core.autocrlf=false`, `core.safecrlf=false`, `core.whitespace=cr-at-eol`, `merge.renormalize=true`) so line endings stop producing spurious differences.

## What this baseline does not claim

No Windows baseline (Phase A step 1) was run. No full-suite green is claimed anywhere. The `PaletteId` fix is a test-helper compile repair, not a product change, and is not committed. Decisions D1 to D9 of the v2 handoff remain open.
