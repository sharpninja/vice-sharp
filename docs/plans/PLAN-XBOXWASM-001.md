# PLAN-XBOXWASM-001: Convert the Xbox head to Avalonia, targeting WASM only

Operator request 2026-10-19 (verbatim): "Convert XBOX version to Avalonia, only targeting WASM." TODO `TODO-XBOX-AVALONIA-WASM-001` (section Plans, priority high, plan first). Sequence step 10 of `docs/receipts/handoff/HANDOFF.lockstep-plan.v2.md` section 11 under goal `GOAL-LOCKSTEP-REMEDIATION-001`. This is a requirement change (operator profile 4.3): requirements are captured first, this plan is approved, then implementation follows BDPv4 with mocks green before production code. No code is written by this document. Hostile validation is deferred to sequence step 12 (D3); each slice exits on its focused gate plus the full applicable suite plus a self-assessment.

Written by Claude Code (claude-fable-5-1) on LAB-OMARCHY, 2026-10-19T20:18, `develop` at `4e5f92cc`. Every "measured" figure below is from `~/codex-dispatch/scripts/Get-ProjectInventory.ps1`, `Probe-DotnetWorkloads.ps1` and `Find-TextInTree.ps1` runs at 20:16 to 20:18 (receipts `~/codex-dispatch/diag/xbox-wasm-plan-20261019/` once copied; raw outputs in this session's log).

## 1. Current state (measured)

- `src/ViceSharp.Xbox` (the UWP XAML head, `PLAN-XBOXUWP-BDPv4`): `WinExe`, `net10.0-windows10.0.26100.0`, 34 C# files (6,519 lines), 17 XAML files (1,117 lines). References Abstractions, Core, Chips, Architectures, Host.InProcess, RomFetch, Xbox.ViewModels, Xbox.Input, Library.ViewModels, RomM. Platform-bound seams (grep): `Controls/VideoSurfaceHost.cs` (XAML `SwapChainPanel` over a raw Direct3D 11 swap chain, `LibraryImport` of `d3d11.dll` and `dxgi.dll`; Win2D was dropped), `Platform/WinRtGamepadSource.cs` (`Windows.Gaming.Input`), `Platform/XboxAudioWiring.cs` (XAudio2 source voice), `RomProvisioning/UwpStoragePicker.cs` and the Settings, FlashCartBuilder pages (`FileOpenPicker`), `ApplicationData.Current.LocalFolder` paths in `App.xaml.cs` and the Library, GameDetails, Lists pages (settings.json, vice-sharp.ini, snapshot-slot1.json, vicesharp.log, C64 ROM folder, romm-cache, romm-recents.json, romm-connection.json), `LocalSettings` (ShowPerfHud), and `XboxCoverImageLoader` (`Windows.Storage.Streams`).
- `src/ViceSharp.Xbox.ViewModels`: `net10.0`, 48 files (5,463 lines), references Abstractions, Protocol, Xbox.Input only. Portable; the boundary is enforced by `XboxViewModelsBoundaryTests`.
- `src/ViceSharp.Xbox.Input`: `net10.0`, 25 files (2,339 lines), references Abstractions and Protocol only. Portable (`GamepadButtonFlags`, `GamepadSnapshot`, `XboxJoystickMapper`, `XboxInputContext`, `DirectionalRepeater`).
- `src/ViceSharp.Host.InProcess`: 50 files (10,380 lines). The emulation pump runs on a dedicated worker thread (`Services/EmulationPumpService.cs` line 128, `new Thread(Loop)`); the gates use timer threads, `SemaphoreSlim`, `Stopwatch` and `Thread.SpinWait` (`SemaphoreEmulationGate.cs`, `ViceEmulationGate.cs`); `XboxManagedFrameGate` takes injectable `sleep` and `nowTicks` delegates (line 26); `ThreadAffinity.cs` calls `SetThreadAffinityMask` only when `VICESHARP_EMU_CPU` is set. The facade `IConsoleEmulatorHost` and `ConsoleHostComposition` exist (PLAN-XBOXUWP R2/R3).
- `src/ViceSharp.Host.Xbox`: the older Avalonia desktop scaffold (`PLATFORM-CROSS-001` Phase 1): 1 file (38 lines), references `ViceSharp.Avalonia` and `ViceSharp.Host` (gRPC, Kestrel). Not a WASM candidate as it stands.
- `src/ViceSharp.Avalonia`: desktop UI, `net10.0`, 37 C# files (9,726 lines), 7 AXAML (780 lines), packages Avalonia 12.0.5, Avalonia.Desktop, Fluent theme, Inter fonts, Grpc.Net.Client, the Remote Control server; references Host (gRPC), Protocol, Xbox.ViewModels, Library.ViewModels, RomM.
- Central package versions (`Directory.Packages.props`): Avalonia 12.0.5 family including Avalonia.Android, Avalonia.iOS and Avalonia.Headless.XUnit; no `Avalonia.Browser` entry yet.
- Toolchain on LAB-OMARCHY: dotnet 10.0.402, no workloads installed (`dotnet workload list` empty), so `wasm-tools` is absent; LEGION2 not measured for this.
- Determinism contract unchanged: the head only pushes input through `IMachineJoystickInput.SetJoystickState` and the RESTORE seam and pulls frames through `ILocalVideoFrameSource.TryCopyFrameInto`; the core per-cycle path is untouched (`PLAN-XBOXUWP` section 3, "Determinism guarantee").

## 2. Decisions (locked by this plan unless Payton changes them)

1. Target: one new head `src/ViceSharp.Xbox.Browser`, `TargetFramework` `net10.0-browser`, package `Avalonia.Browser` 12.0.5 (added to `Directory.Packages.props`), published as a static web bundle (`dotnet publish -c Release` to `wwwroot`). No desktop, no Android, no iOS target in this head; "only targeting WASM" means the browser is the sole published runtime of the Xbox experience.
2. Reuse unchanged: `ViceSharp.Xbox.ViewModels`, `ViceSharp.Xbox.Input`, `ViceSharp.Host.InProcess` (additive seams only, listed in slice 2), `ViceSharp.Library.ViewModels`, `ViceSharp.RomM`, Abstractions, Core, Chips, Architectures, RomFetch, Protocol DTOs. The head references no `Windows.*`, no `ViceSharp.Host` (gRPC), no `Grpc.*`, no `Microsoft.AspNetCore.*`.
3. Threading: the browser runtime is single-threaded (no `Thread`, no blocking waits on the UI thread). The pump becomes a frame scheduler driven by Avalonia's render tick (`requestAnimationFrame` under Avalonia.Browser): each tick advances the number of master cycles that `PacingMath` owes for the elapsed wall time, bounded by a per-tick budget, then yields. The existing `EmulationPumpService` stays for desktop; a new `BrowserFramePump` implements the same pump contract behind a seam (`IEmulationPumpHost`, additive in Host.InProcess). Multithreaded WASM is out of scope.
4. Video: `ILocalVideoFrameSource.TryCopyFrameInto` into an Avalonia `WriteableBitmap` (BGRA, 384 by 272 or the frame geometry the host reports), presented by an `Image` with nearest-neighbor integer scaling into a 4:3 letterbox, same visual contract as FR-XVIDEO-001.
5. Audio: Web Audio through an `AudioWorklet` fed from the existing audio ring and queue math (`ViceSharp.Host.InProcess` audio contract) over `[JSImport]`/`[JSExport]` interop. No audio device or a blocked autoplay policy degrades to a silent backend with pacing unaffected (same failure mode as FR-XAUDIO on UWP).
6. Input: the browser Gamepad API read once per frame into `GamepadSnapshot` (the WinRT-identical bit layout stays the single mapping input), through the unchanged `XboxJoystickMapper` and `XboxInputContext`; keyboard events map to the virtual keyboard emission path the ViewModels already own; the locked controller mapping of `PLAN-XBOXUWP` section 1 is carried unchanged.
7. Storage: the Emscripten virtual file system mounted with IndexedDB persistence (IDBFS) for the writable root; `XboxDataPathBridge.Configure` points the unmodified `ViceDataPathResolver` at that root exactly as it points at `LocalFolder` on UWP; settings, snapshot slot, RomM cache and recents, and the C64 ROM and keymap folder live there. ROM import uses Avalonia's `IStorageProvider` file picker (the browser file input) in place of `FileOpenPicker`; verified HTTPS download stays where CORS allows it and reports a typed failure otherwise. ViceSharp ships no Commodore ROMs.
8. JavaScript: only the thin interop glue that .NET WASM requires (`[JSImport]` bindings and the AudioWorklet processor), authored inside the head project as static assets; no Node tooling, no npm build; the bundle is produced by `dotnet publish` alone (operator rule, no Node in anything designed here). This decision is flagged in section 7 because an AudioWorklet processor is a JavaScript file by construction.
9. Disposition of the existing heads: `src/ViceSharp.Xbox` (UWP) and `src/ViceSharp.Host.Xbox` (Avalonia desktop scaffold) are retired from the solution and the Nuke targets when slice 9 exits, kept on disk until Payton clears deletion (never move or delete without clearance). The desktop Avalonia head and the WiX, winget, choco and scoop pipeline stay untouched.
10. Gates: every slice exits on its focused filter green (0 failed, 0 skipped), the full applicable suite green (`ViceSharp.slnx` test projects plus `build.tests`), `--filter Category=Determinism` 0 failed with the pass count equal to the pre-slice baseline, `git diff --check`, and a written self-assessment; no hostile review until step 12 (D3).
11. Runtime proof: a published bundle is served by a PowerShell-launched static file server and exercised by a headless Chromium run (screenshot and console log captured by the script); the proof asserts the first frame, the frame counter advancing, and one gamepad read when a controller is attached, mirroring the S0 criteria of `PLAN-XBOXUWP` on the browser. No Playwright (its driver is Node).

## 3. Requirement changes (captured on PAYTON-LEGION2 through the requirements workflow after approval, then mirrored; single-writer rule)

New records, each with acceptance criteria written as observable outcomes and a named test:
- `FR-XWASM-001` Browser head boots the C64 core: a supported browser (Chromium and Firefox current releases) loads the published bundle, the C64 reaches the BASIC prompt from reset, and the frame counter advances at the configured pacing. Tests `TEST-XWASM-001` (runtime proof script receipt), `TEST-XWASM-002` (pump cadence against a fake tick clock).
- `FR-XWASM-002` Video: live frames with correct BGRA colors, integer nearest-neighbor scale, 4:3 letterbox, no tearing (pure sink tests over `TryCopyFrameInto`, headless Avalonia render test).
- `FR-XWASM-003` Audio: SID output through Web Audio with the existing ring and queue bounds; silent fallback when no device or autoplay is blocked; volume and mute applied once (reuse `TEST-XAUDIO` math cases bound to the browser backend).
- `FR-XWASM-004` Input: Gamepad API reading mapped by the locked controller layout; virtual keyboard and RESTORE seam reachable from keyboard and controller (reuse `GamepadDataShapeTests`, `XboxJoystickMapperTests`, input-context tests, plus browser adapter tests over a scripted gamepad snapshot source).
- `FR-XWASM-005` Storage and provisioning: settings, snapshot slot, RomM cache and the ROM and keymap folder persist across page reloads in browser storage; ROM import through the file picker; `XboxDataPathBridge` resolves the writable root before any host build (reuse `XboxDataPathBridgeTests`, add IDBFS round-trip tests through a fake file system seam).
- `FR-XWASM-006` GPL and notices: the published bundle carries COPYING, THIRD_PARTY_NOTICES.md and the vendored `*.vkm` keymaps, and the About page shows them (publish gate test lists the bundle).
- `TR-XWASM-001` Topology: `ViceSharp.Xbox.Browser` references only the portable set in decision 2; a raw-file-read shape test guards the csproj (precedent `XboxUwpHeadShellTests`).
- `TR-XWASM-002` Single-threaded pump contract: no `Thread`, `Task.Run`, blocking waits or `SpinWait` in the browser head or its pump; cadence computed from `PacingMath` per tick; a per-tick cycle budget caps catch-up (reflection and source guard test).
- `TR-XWASM-003` Interop: all browser calls go through `[JSImport]`/`[JSExport]` seams behind interfaces the ViewModels already adapt to; JavaScript assets limited to the interop glue and the AudioWorklet processor; no Node tooling in the build.
- `TEST-XWASM-003` Publish gate: `dotnet publish` of the head on both hosts produces the bundle, size and file list recorded, notices present, Nuke `PublishBrowser` and `BrowserTest` targets green.
- Mappings: `FR-XWASM-00x` to the TRs and TESTs above; `TODO-XBOX-AVALONIA-WASM-001` linked to all; `PLAN-XBOXUWP` records (FR-XVIDEO, FR-XAUDIO, FR-XROM, FR-XBOXGPL, FR-XKBD, XBOXUI) are not deleted; their satisfied flags stay as they are, and section 7 asks whether they are retired or re-pointed when the UWP head is retired.

## 4. Slices and gates (BDPv4, mocks first)

- S0 Feasibility (both hosts): install the `wasm-tools` workload (tool install, lab authorization), add `Avalonia.Browser` 12.0.5 to central packages, build a minimal `ViceSharp.Xbox.Browser` that composes `ConsoleHostComposition` with a stub pump and renders one frame in the published bundle; runtime proof script (decision 11). Tests first: the csproj shape test (`TR-XWASM-001`) and the publish gate list test with a mock bundle. Go/no-go recorded before S1.
- S1 Topology and shells: project, AXAML App and MainView, navigation stack bound to the existing ViewModels; headless Avalonia tests (`Avalonia.Headless.XUnit` already pinned) prove page construction and focus graph against the ViewModels.
- S2 Pump: `IEmulationPumpHost` seam in Host.InProcess (additive), `BrowserFramePump` with injected tick clock; mock gate: cadence, budget cap, pause and resume, determinism unaffected (`Category=Determinism` rerun); then bind to the Avalonia render tick.
- S3 Video: `WriteableBitmap` sink over `TryCopyFrameInto`; mock gate with scripted frames and negative controls (wrong stride, wrong format rejected); then headless render test.
- S4 Input: Gamepad API adapter to `GamepadSnapshot` and keyboard adapter; mock gate with scripted readings through the unchanged mapper and context; then browser proof with a controller.
- S5 Audio: Web Audio worklet backend over the existing ring math; mock gate for queue bounds, underrun, mute and volume; silent fallback; then browser proof.
- S6 Storage and provisioning: IDBFS root, data path bridge, settings and snapshot persistence, file picker import, download path with typed CORS failure; mock gate over a fake file system; then browser round trip across reload.
- S7 Pages: Settings, Device Setup, Input Mapping, About, Library, Game Details, Lists, Flash Cart Builder ported to AXAML over the existing ViewModels; headless tests per page.
- S8 RomM and Library in the browser: HttpClient over fetch, CORS documented, cover loader without `Windows.Storage.Streams`; existing `TEST-ROMM` mock contracts bound to the browser adapters.
- S9 Packaging and retirement: Nuke `PublishBrowser` and `BrowserTest` targets (tests in `build.tests` per D9), GPL files in the bundle, CI coexistence; retire `src/ViceSharp.Xbox` and `src/ViceSharp.Host.Xbox` from the solution and targets (on disk until cleared).
- Exit: all slices green, full suite green on both hosts, `Category=Determinism` unchanged, self-assessment at or above 98/98, then the single step 12 review.

## 5. Blast radius

- New: `src/ViceSharp.Xbox.Browser` (head, AXAML, interop glue, AudioWorklet script), `Directory.Packages.props` (+Avalonia.Browser), `ViceSharp.slnx` (+head, -UWP head, -Host.Xbox at S9), Nuke targets and `build.tests` cases, `docs/testing/Browser-Head.md`.
- Additive in `ViceSharp.Host.InProcess`: the pump host seam and `BrowserFramePump`; no change to existing pump, gates or determinism paths.
- Untouched: core libraries, `ViceSharp.Avalonia` desktop head, gRPC host, packaging pipeline, native VICE and the lockstep path.
- Retired at S9 (kept on disk): `src/ViceSharp.Xbox`, `src/ViceSharp.Host.Xbox`, their `XboxUwpHeadShellTests` and `XboxHostShellTests` (re-pointed or retired per section 7).

## 6. Risks

- The .NET browser runtime is single-threaded and slower than desktop JIT; a full C64 frame budget at 50 Hz may not fit on low-end devices. Mitigation: S0 measures cycles per millisecond in the browser; the per-tick budget and a frame-skip policy are tested in S2; the plan does not promise full speed until S0 reports.
- Browser autoplay and audio policies gate Web Audio until a user gesture; S5 degrades to silence until the first interaction and shows the state.
- Storage quotas and IDBFS flush timing can lose writes on tab close; S6 flushes on every settings write and proves the reload round trip.
- CORS blocks RomM servers that do not send the headers; S8 documents the server requirement and the typed failure.
- AudioWorklet is JavaScript; it is the one asset outside PowerShell and C#, confined to the head and reviewed as source (section 7).
- Workload installation on both hosts changes the SDK state; receipts record `dotnet workload list` before and after.

## 7. Operator-confirm items (one line each)

1. Approve the requirement set in section 3 for capture on LEGION2 (then mirror), before any implementation.
2. Confirm decision 9: retire the UWP head and the Host.Xbox scaffold from the solution at S9, keep on disk until you clear deletion.
3. Confirm decision 8: the AudioWorklet processor is an allowed JavaScript asset (interop glue only, no Node tooling), or name the alternative (audio through the main-thread `ScriptProcessorNode`, deprecated but script-free from our side? it is still JavaScript; or no audio in the first release).
4. Confirm decision 11: headless Chromium driven by PowerShell is the runtime proof shape (no Playwright).
5. Rule on the `PLAN-XBOXUWP` requirement records when the UWP head retires: re-point to the browser head, or retire with the head.
6. Confirm that step 10 may start implementation only after Phase C to G are code complete (the current sequence) or may run on LAB-OMARCHY in parallel while LEGION2 owns the lockstep critical path.

Signed: claude-fable-5-1-high@ClaudeCode | Confidence: correctness 94%, accuracy 97%, completeness 92%
