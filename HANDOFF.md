# ViceSharp Handoff - 2026-08-06

**Active branch:** `feat/iteration2-vic20` (off `main` @ v1.2.1 line). Iteration 2 = overall project VIC-20 (not RomM Phase E).
**Prior release baseline:** `main` at **v1.2.1** (NuGet + winget published). Iteration 1 C64 complete.
**Working tree noise (do not commit):** untracked `docs/S-Blox/`, `docs/reviews/*`, `docs/romless-vic-badline-fix.md`, `manifests/`, many `docs/*-2026-08-06.log` lockstep artifacts.

## VIC-20 every-cycle lockstep - PAL + NTSC 10s GREEN (2026-08-06)

- **PAL TenSecondPal:** matchedCycles = 11_084_050 / budget 11_084_050 (~35 s). Log: `docs/10s-lockstep-2026-08-06.log`. Receipt: `docs/receipts-lockstep-10s-2026-08-06.txt`.
- **NTSC TenSecondNtsc:** matchedCycles = 10_227_270 / budget 10_227_270 (~37 s). Log: `docs/10s-lockstep-ntsc-2026-08-06.log`. Receipt: `docs/receipts-lockstep-10s-ntsc-2026-08-06.txt`.
- **PAL TwoSecondPal** (VICESHARP_LOCKSTEP_2S=1): Passed ~8 s. FocusedWindow: Passed.
- **Via6522 unit:** 48/0/0 after bus-visible T1/T2 change.
- **Where diverged (NTSC residual c=521027):** soft-BIT $9124 (VIA2 T1) V flag only (nP=$E5 mP=$A5).
- **How diverged:** (1) soft-apply re-LOADed T1; VICE BIT(GET_ABS) loads once. (2) VIA Tick before CPU made CPU Read see post-Tick T1; VICE LOAD is at maincpu_clk before CLK_INC (saw $C0 V=1 vs managed $BF V=0).
- **How realigned:** SoftDeferBitBody latches one GET; soft-apply N/V/Z from latch. Via6522 captures pre-Tick `_t1BusVisible`/`_t2BusVisible` for Read; Peek stays post-Tick (export match). Rejected CPU-before-VIA Phi2 reorder (broke free-run phase).
- Env: `VICESHARP_LOCKSTEP_10S=1` for 10s gates; `VICESHARP_LOCKSTEP_2S=1` for 2s. Run NTSC and PAL in **isolated processes** (failed NTSC can poison later PAL short runs at c=5513).

## Iteration 2 VIC-20 - native lockstep gate GREEN (2026-08-05)

- **Branch:** `feat/iteration2-vic20` (off main / v1.2.1). Locked: **default drive 8 = 1540**.
- **Native xvic oracle:** `native/vice_xvic.dll` via `native/build-vice-shim-xvic.sh` + `vice-shim-vic20.c`; hosted `vic20cpu.c` / `mainviccpu.c` (`VICE_SHIM_HOSTED`). Managed: `ViceNative.CreateInstance("vic20"|"vic20ntsc"|"xvic")` -> `ViceNativeXvic`.
- **Open-bus (FR-VIC20-002):** `BasicBus` last-data latch; BASIC/KERNAL ROM reads do **not** refresh last-data (VICE `vic20memrom_*`); chargen does; uninstalled BLK falls through (not sticky 0xFF). Fixes kernal `CMP $A003,X` Carry/N vs xvic.
- **Lockstep receipts (criterion 3):** full **A/X/Y/S/P/PC** through **5000** cycles; **dual VIA** control peeks ($9110/$9120 DDRA/DDRB/ACR/PCR/IER) at 64/256/1024; **VIC-I** static peeks + VICE raster encoding ($9003/$9004) through 5000. Vic20 filter **87/0/0**. API: `IViceNative.PeekBus` / `vice_machine_peek_bus`. Mos6561 power-on zeros + raster encoding match VICE. Receipts: `docs/receipts-vic20-phase2-verify-2026-08-05.txt`. CI: VIC20 + dos1540 in `EnsureCiRomRoot`.
- **Residual:** intermittent mid-instruction PC lag after cycle ~5005 (control-flow diverge later; deeper CPU staging for multi-frame bit-exact).
- **Still open (Tier A polish):** J expansion UX, K input E2E, L cart/disk, M snapshots.
- **Do not** commit untracked `docs/S-Blox/`, reviews, manifests. Rebuild oracle: `MSYSTEM=MINGW64 bash -lc 'cd /f/GitHub/vice-sharp/native && bash ./build-vice-shim-xvic.sh'`.

## PLAN-XBOXUWP + PLAN-ROMM resume snapshot (2026-08-05)

- **Track 1 (Xbox Tier D residuals) largely SHIPPED this session:** FEAT-XAOTBIND-001 (x:Bind migration), FEAT-XOCTOPUS-001 (CI/release Octopus LEGION2 steps), FIX-ROMLESSVIC-001 (merged test), FIX-XKBDNMI-001 (RESTORE asserts NMI), FEAT-XCTRLBIND-001 (Controls remapping), FEAT-XROMPICK-001 (model ROM readiness).
- **Track 2 (RomM):** merged `feat/romm-integration` into this branch (`3804a1d`). Portable `ViceSharp.Library.ViewModels` + `ViceSharp.RomM`, Xbox/Avalonia library UI, LAN bridge connection path present. Gate after merge: **Category=Xbox|RomM|Library 546/546** (0 fail, 0 skip).
- **Deploy loop:** `./build.ps1 DeployXboxLocal` (VS MSBuild Release-UWP). End CLI batches with Debug-UWP restore for operator VS F5. PublishAot stays opt-in (`ViceSharpPublishAot=true`); Release-UWP JIT.
- **Track 3 (Microsoft Store / S42) IN PROGRESS 2026-08-05:**
  - Phase 0 **GO with mitigations**: `docs/xbox/gpl-store-section6-review.md`
  - Privacy: `docs/PRIVACY.md` (listing URL source)
  - Listing copy + screenshot runbook: `docs/xbox/store-listing-copy.md`
  - GPL gate receipt: `XboxGplComplianceTests` **5 passed / 0 failed / 0 skipped** (Release)
  - ADO gaps: variable group `xbox-store-publish` **missing**; pipeline `azure-pipelines-xbox-store.yml` **not registered** (project has VICE-Sharp-CI id 15, VICE-Sharp-Release id 16 only); environment `xbox-store` not confirmed
  - **Operator guide (step-by-step + links):** `docs/xbox/store-next-steps-guide.md`
  - **Next operator:** Partner Center reserve name + fill 8 vars; create ADO var group + environment approval; register Store pipeline; capture screenshots; S42 console matrix
- **OPEN:** B-on-controller operator verification; S42 Tier-C console deploy + cert; Partner Center + ADO wiring; MCP TODO reconcile; device smoke for RomM Library after merge.
- **Parallel worktrees (reference):** `F:\GitHub\vice-sharp-romm` (`feat/romm-integration`, source of merge); romless repro merged via FIX-ROMLESSVIC-001.

## How to resume

1. Read `AGENTS-README-FIRST.yaml` (marker rotates on server restart; verify signature + /health nonce before MCP work).
2. Route TODO / session-log / requirements / triage through the mcpserver plugin (repl-invoke or wrappers), never raw REST, never storage files.
3. Baseline gate (one process, ~20 min, run detached):
   `dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter "Category!=Determinism&Category!=AiReview&Category!=ParityPending&Category!=ParityLegacy"`
   Green criterion: 0 failed, 21 skipped, total 2612-2615 (totals wobble in that band from cosmetic xunit trx name serialization of dynamic theory rows; per-class row sets are identical - never chase a fixed total).

## Shipped 2026-07-08 (all CI-confirmed)

- **v1.0.2 released**: tag on `534cded`, release run 1074, all 13 package ids verified in the nuget.org flat container (Core bundle, SourceGen, Protocol, Monitor, Launcher, AdhocHelper, Host, Avalonia, Console, Host.MacOS/Android/iOS/Xbox; Console + Avalonia are dotnet tools).
- **PLAN-NATIVERESIDUE-001 closed** (`ba0f94f`): a `.vsf` whose DRIVE8 module carries `has_tde=0` disabled TrueDrive process-wide via `resources_set_int` (drive-snapshot.c:334-363); `vice_machine_create_model` now re-baselines `Drive{8..11}TrueEmulation` to the VICE default 1 through `resources_set_int` so iecbus statics and $DD00 callbacks recompute. SnapshotResume two-process partition removed; suite proven green in ONE process. Guards: TEST-NATIVE-RESIDUE-01/02 in `NativeResidueDiagTests`.
- **CI made self-sufficient** (`a229801`, `d8f61cf`, `4e3db9b`): agents have no ROMs and no shim, so `EnsureCiRomRoot` in `build/Build.cs` stages 13 hash-pinned VICE data files (C64 ROMs incl. all variant kernals, DRIVES dos1541/dos1541ii, gtk3_pos.vkm) from the VICE-Team svn-mirror into `artifacts/vice-data` and sets `VICESHARP_ROM_PATH` for the test process only. Shim-dependent tests MUST use `[ViceFact]`/`[ViceTheory]` (plain `[Fact]` throws DllNotFoundException on shimless agents). First-ever green CI run: 1072.
- **Docs reconciled to shipped reality** (`aee29ea`, 88 files): README, USER-GUIDE, VICE-MIGRATION, ROMs, Architecture, Public-API (regenerated from the 50 real Abstractions interfaces), requirements docs; 22 stale files pruned; `docs/wiki.yaml` manifest lists all 28 wiki documents; winget license fixed MIT -> GPL-2.0-or-later.
- **github divergence resolved** (`20b330c`): github-only commit `42f2f26` (VicModeChangeEvent seam) merged with strategy `ours` (tree unchanged; content preserved on `rescue/vic-modechange-event`); github main fast-forwarded, tags identical on both remotes; github wiki published per manifest (`b455b88`, 28 pages + sidebar).
- **Dependency wave S0-S8 complete** (TR-DEPS-202607-001 + TEST-DEPS-202607-001, both completed with evidence; commits `72696d9`, `055b9d5`, `26e5ca8`, `87eb80a`, `9214cdb`, `1cad822`+`1adf386`, `23d030a`, `0f5192f`): all 30 central entries at newest mutually compatible stables (Avalonia 12.0.5, Extensions/TestHost 10.0.9, Protobuf 3.35.1, Grpc.Tools 2.82.0, Test.Sdk 18.7.0, Roslyn pair 5.6.0, YamlDotNet 18.1.0, FluentAssertions 8.10.0, coverlet 10.0.1, aiUnit 2.1.3, RemoteControl 0.7.4); 5 dead pins removed; AiReview under CPM with inherited TreatWarningsAsErrors; vendored `nuget-local` feed DELETED (NuGet.config is nuget.org-only); `global.json` floor 10.0.301 (Roslyn 5.6 generator gate). Excluded as prerelease: xunit.v3 4.x, NSubstitute 6.x, Protobuf 4.x, Extensions 11.x.
- **Test fixtures in repo** (`93cdc7e`): `vice-snapshot-20260630171307.vsf` (read by the residue probes and lockstep re-baseline suites at repo root; do not delete), `native/.build-nopatch.sh`, `native/.ccwrap/*`. Operator rule: if it is needed to run a test, it goes in the repo.

## Iteration-1 completion (branch `fix/nativeresidue-002-drive-clock-hardening`, off `master`; NOT pushed)

Working through the "complete outstanding iteration 1 work" plan. Done, all committed locally, gates green:
- **Slice A - PLAN-NATIVERESIDUE-002** (710c1e9/8894f1a/9fb06dd/0691344): the two latent drive-clock bugs fixed; **BUG-LOCKSTEP-001 CLOSED** (full gate 0 failed / 2596 passed / 21 skipped). See docs/receipts-nativeresidue-002-2026-07-09.md.
- **Slice F - BUG-TESTDEBT-001**: verified already fixed at HEAD (6a32e7c/77e7190), closed with receipt.
- **Slice B - PLAN-VICEPARITY-001 S10** (67959e6/0fdaa6a/35d9e05): SID reSID data-bus read semantics ($19/$1A POT 0xFF, $1B/$1C OSC3/ENV3 latch, other reads = fading shared bus), per-model DataBusTtl virtual, Peek/Read split, dead-code retirement. Parity ratchet 405->419. Baseline gate 0 failed / 2603 passed / 21 skipped.

**SID parity program COMPLETE (2026-07-10)** - all remaining slices landed:
- **S11 / S11b**: 8580 reSID filter port (filter8580new m==1) + write pipeline + per-model ttl(0xA2000)/scaleFactor(5); shim exports vice_sid_exact_set_sampling + vice_sid_exact_clock_buffered; DATABUS-07 flipped; oracle via "c64c". Ratchet ->444.
- **S12**: amplify(scaleFactor)/clip PCM16 seam + extfilt enable branch; OUTPUT-01..07. Ratchet ->451.
- **S13**: fixed-point Kaiser FIR resampler (fast/interpolate/resample); OUTPUT-08..13. Ratchet ->457.
- **CH**: retired the dead Chamberlin SVF stack (10 members + 7 guards + SidFilter6581Tests.cs). PLAN-SIDCHAMBERLIN-001 closed.
- **BC**: ported reSID batched SID::clock(delta_t) (Sid6581.BatchedClock.cs); SAMPLE_FAST now value-bit-exact; un-pended the last 4 SID quarantines + authored the last 9 (EXTFILT-01..07, CLOCK-05/08). Ratchet ->466 with a strict completion pin (Assert.Equal(466, covered)).
- **LW** (4924b94, pushed): live audio wired through the reSID SAMPLE_RESAMPLE engine (Resample always, VICE x64sc parity); push tail bit-exact vs the buffered pull; warp = cadence-only; zero-alloc; benchmark in SidSamplingBenchmarks.
- **CL** (a38964b/e100433, local): NativeCollectionConventionTests + [Collection("NativeVice")] on the 6 native-bridge test classes.
- **G**: DROPPED per operator (ADO wiki push).

Closure gates: parity 466/0/0 (completion pin holds), determinism 5/0, XmlDocs green, full baseline 0 NEW failures (7 pre-existing VideoRendererTests only) on the clean re-run. One run-1 full-suite native ACCESS_VIOLATION did NOT reproduce (flaky native-shim instability; candidate PLAN-NATIVERESIDUE-002 follow-up, not a SID blocker) - see the closure receipts.

**Native build recipe (learned this session, load-bearing):** `make x64sc-program` does NOT rebuild changed VICE-core `.o` (they stay stale). Before rebuilding, delete the changed `.o` (and `libdrive.a` for drive files), then run under the MSYS2 MINGW64 login shell so the compiler gets a writable `/tmp`:
`MSYSTEM=MINGW64 /c/msys64/usr/bin/bash.exe -lc 'rm -f <changed>.o; bash /f/GitHub/vice-sharp/native/.build-nopatch.sh'`. The dll is gitignored (built locally). Vendored edits go in native/patches/vice-shim-runtime.patch (regenerate via `git -C native/vice diff`; keep the 5 pre-existing hunks byte-identical).

**Oracle note:** SidExactRead (read path) is the reliable bus observable; SidExactGetState().BusValue export is NOT a dependable live-latch snapshot (returned stale/garbage) - compare via the read path + managed spec constants.

## In flight (interrupted mid-task)

Plugin reload + Agent Help (mcpserver core synced to 1.36.0, `mcpserver-repl` 1.4.5 confirmed on PATH):
1. Validate cache: `%USERPROFILE%\.claude\plugins\cache` has `mcpserver-local` (active family; confirm 1.36.0 from the plugin's own `.claude-plugin/plugin.json`), `mcpserver-cowork` (stale candidate - verify before deleting), `caveman` (unrelated, keep). The version inspection was interrupted by a transient permission-classifier outage; re-run it.
2. Run the claude-hook-validation skill; refresh MCP hooks if missing; restart Claude Code if hooks were installed.
3. Open a `workflow.agenthelp.createSession` (new in repl 1.4.5) and submit the outstanding MCP issues:
   - repl-invoke `Invoke-WorkflowAppendActions` audit counters never increment: regex `(?m)^\s*type:` misses `- type:` list items (repl-invoke.ps1:891-897; filed as triage-report-8a6539340a174c00a57dee53ec8f42ea).
   - No per-action readback: `workflow.sessionlog.queryHistory` returns only session headers, so appends cannot be content-verified; session header `lastUpdated`/`filesModifiedCount` never advance (anchored to `started`).
   - `Invoke-McpPlugin.ps1` defaults `-Command Status`: calling it with only `-Method` silently prints the status blob instead of invoking (footgun; cost one silent append failure).
   - `workflow.requirements.createTr` requires `subarea` (schema) but the skill docs do not mention it; the server also derived `subarea: 202607` from the id rather than honoring the supplied value.
4. Log the reload + help-session outcomes; triage any confirmed server defects.

## Parked items

- Azure DevOps wiki push needs `ADO_PAT` set, then `tools/Publish-Wiki.ps1 -Target azure` (github wiki already published).
- ~~Latent VICE bugs documented, not fixed~~ FIXED 2026-07-09 (PLAN-NATIVERESIDUE-002, branch `fix/nativeresidue-002-drive-clock-hardening`): drive `attach_clk`/`detach_clk`/`attach_detach_clk` uninitialized-stack read on `has_tde=0` restore (drive-snapshot.c zero-init) and `cycle_accum` omitted from `drivecpu_reset_clk` (drivecpu.c + drivecpu65c02.c) both fixed via the vendored runtime patch, plus a shim create-time drive-clock re-baseline. **BUG-LOCKSTEP-001 CLOSED**: full baseline gate 0 failed / 2596 passed / 21 skipped / 2617 total (was 136+2 lockstep failures). Receipts: docs/receipts-nativeresidue-002-2026-07-09.md. New residue candidates recorded there (live unit->type not re-baselined; drivecpu65c02 cycle_accum SMW/SMR width asymmetry). Build note: `make x64sc-program` does NOT rebuild changed VICE-core `.o`; delete the stale `.o`+`libdrive.a` and build under `MSYSTEM=MINGW64 bash -lc`.
- ~~VICE parity program (PLAN-VICEPARITY-001) remaining slices~~ **COMPLETE 2026-07-10** (SID side fully bit-exact vs reSID; VIC per-cycle work landed through V7 + audit phases). Only manifest-wide pending AC left is TEST-VIC-FETCH-06 (VIC-II FAITHFUL-lock conflict, out of SID scope, needs the parity owner). Candidate follow-up: PLAN-NATIVERESIDUE-002 native-shim lifecycle hardening (a sustained-consecutive-native-load ACCESS_VIOLATION observed once, non-reproducing).

## Open TODO backlog (MCP store)

PLAN-UIDOCK-001 (Dock.Avalonia UI), PLAN-AUDIOEQ-001, PLAN-PLAYLIST-001, PLAN-FULLSCREEN-001, PLAN-DEVCARDART-001, PLAN-MONFRAME-001, PLAN-DRIVE1581-001, PLAN-DRIVE1541II-001, PLAN-DRIVE1540-001, PLAN-DRIVE1571-001, PLAN-DRIVECMDHD-001, PLAN-CARTRAMLINK-001, PLAN-ARCHVIC20-001 (query the store for the live list; this snapshot is 2026-07-09).

## Operational gotchas (hard-won this session)

- Long test runs: OS-detach with `Start-Process dotnet ... -RedirectStandardOutput` and tail the log with a Monitor; never run two test/build invocations concurrently (native lock + obj/bin contention); verify the log grows before trusting a launch.
- `git add a b c bad-path` aborts the WHOLE add on one bad pathspec and a following commit ships whatever was staged earlier; always check the commit stat against intent.
- Diagnostic probes: `LiveLimiterBandProbeTests` reports via a by-design `Assert.Fail`; `Demo_SilentWarp` headroom is host-load sensitive (re-run in isolation before believing a failure). Live-app probes attach via `%LOCALAPPDATA%\ViceSharp\debug-attach.json`; NEVER dispose the probe client (it kills the session).
- github force-push is classifier-blocked regardless of instruction phrasing; the sanctioned divergence pattern is merge `-s ours` + fast-forward.
