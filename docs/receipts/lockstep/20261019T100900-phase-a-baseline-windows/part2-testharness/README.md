# Phase A step 1 part 2: TestHarness diagnostic filters, PAYTON-LEGION2

Plan: `docs/receipts/handoff/HANDOFF.lockstep-plan.v2.md` Phase A step 1 (diagnostic TestHarness filters against the existing prebuilt shim, without `native/Vice.Native.proj`). Machine PAYTON-LEGION2, user `kingd`, workspace `F:\GitHub\vice-sharp`, branch `develop`, HEAD `d00e8c5584d0031b3637c649335964f8d5225da0` during the run (develop was fast-forwarded to `6fb84af` at 11:57:35, after the run; that commit touches only two handoff markdown files). Gitlink `cc67418cba58e71d8e70fd353e9de47199ad2075`. Measured window 2026-10-19T10:31:39 to 11:27:03 local (`run.log`). Claude Code claude-opus-5-5, session `ClaudeCode-20261019T152439Z-plugin-session`, request `req-20261019T152609Z-prompt-b96d`. **Diagnostic class only**: no gate, no done state, no HV (remediation phase).

## Native inventory

Method, reproduced from part 1 (same hash): `git status --porcelain --untracked-files=all` inside `native/vice` with `GIT_OPTIONAL_LOCKS=0`, written with `Out-File -Encoding utf8NoBOM`. Before and after: 2,539 entries (12 tracked, 2,527 untracked), SHA256 `C7848A24867CD60C49FE6088692A6E4565240868C89D811F806B429CDB3CBFC5`. Unchanged across the build, all nine filters and the diag-verbosity build.

## TestHarness build

- Compile fix (uncommitted): `tests/ViceSharp.TestHarness/SettingsRestartLifecycleTests.cs`, class `TrackingVideoSink` gains `public string PaletteId { get; set; } = "vice";` with an FR-CFG-003 XML doc line, mirroring `FrameSequenceCapture`. Cause: `IVideoCaptureSink.PaletteId` added by commit `6530196`.
- Native project avoided: `dotnet build tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --nologo "-p:ViceNativeProject=F:\GitHub\vice-sharp\native\__vice-native-disabled-phase-a__.proj" -v:detailed`. Target `BuildViceNative` runs only when `Exists('$(ViceNativeProject)')`; the global property points at a path the script proves absent, so the condition is false. Exit 0, 0 errors; MSBuild reports 1 warning (its text was not extracted).
- Proof (`build-diag-proof.txt`): diag-verbosity rebuild, exit 0. Every evaluation shows `ViceNativeProject` = the disabled path and "global property, cannot be modified"; 0 lines mention `Vice.Native.proj`; native inventory unchanged. Limitation: this SDK's text logger records no false-condition target skips for any target (0 in the 158,516,696-byte diag log), so there is no positive "BuildViceNative skipped" line. The full diag log is kept outside the repo, path and SHA256 in the proof file.
- Tests ran with `--no-build --blame-hang-timeout 30m`, one TRX per filter.

## Diagnostic filters (passed / failed / skipped)

- Five CPU/NMI tests by name (`CpuNmiFive.trx`): 1 / 4 / 0. Failing: `CpuInterruptTests.LdaImm_IrqOnLastClk_DoesNotDispatchBeforeNextFetch`, `CpuInterruptTests.RtsThenLdaImm_NextTick_FetchesFollowingOpcode`, `CpuInterruptTests.Rts_DelayNextFetchTick_DoesNotTakeIrqBeforeReturnFetch` (each "Expected: 169 Actual: 0"), `Mos6510NmiTests.Nmi_RtiReturnsCpuToPreNmiState` ("CPU did not reach PC=$0400 within 64 ticks (PC=$0000)"). Passing: `Mos6510NmiTests.Nmi_IsEdgeTriggered_HoldingLineDoesNotRefire`. Class: managed CPU interrupt pipeline. Same split as LAB-OMARCHY.
- `Category=Determinism`: 5 / 0 / 0.
- `FullyQualifiedName~AcceptanceTests`: 210 / 0 / 0. Same as LAB-OMARCHY with `VICESHARP_ROM_PATH` set.
- `LockstepValidationTests`: 6 / 0 / 0.
- `Vic20NativeLockstepTests`: **ABORTED, not a result.** TRX records 4 of the class's 18 cases (3 passed, 1 failed) before the run aborted. Recorded failure: `NativeXvic_Managed_CpuRegs_Lockstep("vic20", 64)`: native PC=$FD22 X=$00 S=$00 P=$26, managed PC=$FD99 X=$01 S=$FD P=$25. Then `NativeXvic_Managed_DualVia_ControlRegs_Lockstep` spun inside native code: child `ViceSharp.TestHarness.exe` PID 25876 CPU-bound (about 490 s CPU at 10:43), managed stack `ViceSharp.Core.ViceNativeXvic+Vic20NativeInstance.Step()` (`Vic20NativeLockstepTests.child-stack.txt`; testhost stack `Vic20NativeLockstepTests.hang-stack.txt`). Blame aborted at 11:23:29: "Test host process crashed : xvic vice_machine_create call=1 model=vic20" after 30 minutes of inactivity. `vice_xvic.dll` was loaded from a temp copy under `%TEMP%\ViceSharpNative\9798DE388F918BD7\`.
- `Vic20HalfCycleComparatorTests`: 0 / 1 / 0. `RegistersMatch_BusAddressDiffers_ReportsExactHalfCycle`: "Assert.NotNull() Failure: Value is null".
- `CpuVisiblePcTimingTests`: 132 / 16 / 0. The 16 failures are managed-only timing assertions (names in the TRX), for example `WaitLoopIteration_IrqPending_CountsThirteenViceClocks` ("irq_delay delta=12 (want 11)"), `Jsr_ReturnAddressPushTimingMatchesHostedVice` (248 vs 247), `TaxThenTakenBmi_Cycle1_ExportsFallthrough`. Same count as LAB-OMARCHY (Phase E step 2).
- `X64ScVariantLockstepTests`: 292 / 14 / 0 of 307, with 1 not executed. Not executed: `SelectedD64_PostKernalCloseStateMatchesNative_ForC64Pal` ("Set VICESHARP_SELECTED_D64 ... or set VICESHARP_RUN_SELECTED_D64=1", an environment opt-in, not a skip attribute; TRX `notExecuted` counter reads 0). Failures in two classes: ultimax and max cartridge profiles diverge at cycle 4 (managed PC=$E002 versus native; post-reset PC off by 1, 57346 vs 57345), 10 cases; c64gs and gs diverge at cycle 5407 (PC=$FD84; post-reset PC off by 1, 64900 vs 64899), 4 cases.

## Livelock probe (diagnostic only, `livelock-probe/`)

`dotnet test ... --no-build --blame-hang-timeout 3m --blame-hang-dump-type none --filter "FullyQualifiedName~NativeXvic_Managed_DualVia_ControlRegs_Lockstep"`, 11:54:28 to 12:02:11. `DualVia(cycles: 1024)` passed in 1.16 s; the next DualVia case in the same process spun again and the run aborted with the same "xvic vice_machine_create call=1" reason. The orphaned child (PID 30524, started by this probe at 11:54:31, 445 s CPU) was stopped at 12:02:11. Inference: the livelock does not scale with the cycle budget; it follows a second native instance created in the same process. Not proven; the hang dumps hold the native stacks.

## Pins

Script `Invoke-PhaseAPart2.ps1` (parser 0 errors, Invoke-ScriptAnalyzer Warning/Error 0 findings, `-WhatIf` dry run before the real run), SHA256 `5ED72119B31FA35E9E39CDC8936D7032727294677D8B74B72F25C254CC33F1FC`. `run.log` `CA7BB3A6...0AEA6`, `summary.json` `FBA1896B...547853`, `build-diag-proof.txt` `14CCF481...C7BD`; every other file's SHA256 is in the session-log turn. Hang dumps: 915,679,292 bytes in total (two dumps, each stored twice by the Blame collector, under `0126623a-.../` and `SYSTEM_PAYTON-LEGION2_.../`); they must never be committed.

## What this baseline does not claim

No gate, no requirement or criterion satisfaction, no Windows full-suite result (only these filters), no root cause for the xvic livelock, no proof the shim is the immutable oracle (the native tree is dirty: 12 tracked, 2,527 untracked), and no claim about the unextracted build warning.

Accuracy 96: counts and names are read from the TRX files and logs here; the livelock-cause line is labeled inference. Completeness 90: the build warning text is not extracted, 14 of the 18 Vic20NativeLockstepTests cases never ran, and the dumps are not analyzed.
