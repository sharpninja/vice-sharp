# Testing Requirements (MCP Server)

## TEST-CHIPSTATE

### TEST-CHIPSTATE-001

Recorder tests with a fake stateful device verify per-tick chip-state capture and that Snapshot deep-copies (a later capture does not corrupt an earlier snapshot). VIC/SID/CIA/PLA CaptureState/DecodeState round-trip exercised end to end via GetChipStateAtTick.

**Acceptance Criteria:**
- [x] OnInstructionCompleted captures the device state into the tick
- [x] Snapshot deep-copies chip state across ring rotation


## TEST-CPUTICK

### TEST-CPUTICK-001

Unit tests covering ExecutedCycles increments per executed (non-stolen) cycle, per-CPU rate = executed/elapsed/target, reset zeroes counters, and the status surface lists host and peripheral CPU rates distinctly.



## TEST-CTX

### TEST-CTX-001

Full transition table + menu neutralization + neutral push + UI-nav mapping. Acceptance: every (state,input) edge deterministic; A does not fire in menu; one-shot neutral push emitted.



## TEST-DEPS

### TEST-DEPS-202607-001

Each upgrade slice S1-S8 exits on: dotnet restore + Release build with TreatWarningsAsErrors + slice-focused test filter + the full filtered TestHarness suite at 100 percent green (0 failed, 21 skipped baseline, >= 2594 passed; filter Category!=Determinism&Category!=AiReview&Category!=ParityPending&Category!=ParityLegacy). S3 adds an interactive Avalonia smoke; S6 proves nuget.org-only restore for aiUnit/RemoteControl; S7 proves PackNuget locally; S8 proves the source generator loads under the raised SDK floor (Roslyn 5.6).

**Acceptance Criteria:**
- [x] Every upgrade slice exits with the full filtered suite at 0 failed and stable skips, plus its slice-specific proof. (evidence: Local gates S1-S8 all 0 failed / 21 skipped; CI runs 1086, 1087, 1088, 1089, 1091, 1092, 1093, 1094, 1095 all succeeded.)


## TEST-DRV

### TEST-DRV-001

Focused integration tests shall prove D64 directory and file operations complete over the IEC bus and produce bus activity telemetry.

**Acceptance Criteria:**
- [ ] Tests prove mounted D64 directory and file operations execute through IEC bus line activity.
- [ ] Tests prove the same D64 operations complete with correct directory and file data over the IEC path.
- [ ] Focused IEC/D64 tests run with zero failed and zero skipped tests.


## TEST-DRV-MOTOR

### TEST-DRV-MOTOR-001

After SetMotor(true) and 300,000 Tick() calls, MotorRotationCycles>0. Before 300,000, stays at 0. Motor off/on resets ramp. ReadSector(18,0) returns BAM bytes 0x12/0x01. 5 pass.



## TEST-DRV-TYPE

### TEST-DRV-TYPE-001

DriveTypeShimTests (Collection NativeVice): vice_drive_set_type sets Drive8Type and vice_drive_get_type reads it back; out-of-range units rejected; after set+reset the native drive PC lands in $8000-$FFFF for 32KB-ROM models (proves memiec re-init); vice_drivecpu_get_clk is monotonic under stepping; vice_drivecpu_execute_all callable; Drive8Type re-baselines to 1541 on the next machine create (no cross-test residue).



## TEST-DRV1540-LOCKSTEP

### TEST-DRV1540-LOCKSTEP-001

Boot witness: drive {PC,A,X,Y,P,SP} equals native x64sc (Drive8Type=1540, TDE=1) at equal drive clocks sampled every 19,656 host cycles over 2M host cycles, plus host CPU state. Media gate: KERNAL-CLOSE anchored LOAD convergence from frostpoint.d64 over 30 emulated seconds.



## TEST-DRV1541II-LOCKSTEP

### TEST-DRV1541II-LOCKSTEP-001

Boot witness + anchored D64 LOAD gate vs native x64sc with Drive8Type=1542, same procedure as TEST-DRV1540-LOCKSTEP-001.



## TEST-DRV1571-LOCKSTEP

### TEST-DRV1571-LOCKSTEP-001

Boot witness (no disk; WD1770 idle-state parity implied) + anchored LOAD gates from BOTH frostpoint.d64 and a committed canonical frostpoint.d71 (349,696 bytes, size-pinned) vs native x64sc with Drive8Type=1571.



## TEST-DRV1581-LOCKSTEP

### TEST-DRV1581-LOCKSTEP-001

Boot witness + anchored LOAD gate from a committed canonical frostpoint.d81 (819,200 bytes, size-pinned) vs native x64sc with Drive8Type=1581; the LOAD exercises the WD1770 type-2 read path end-to-end.



## TEST-DRVLED

### TEST-DRVLED-001

Drive DOS ROM setting VIA2 PB3 sets the drive LedOn; per-drive status DTO reflects it; the card LED view-model binds it.



## TEST-DRVLIFE

### TEST-DRVLIFE-001

Attach creates+clocks+bus-connects the drive instance; detach unregisters from clock and disconnects from bus.



## TEST-DRVTRUE

### TEST-DRVTRUE-001

VM: TrueDrive toggles per drive (default off). Runtime: with TrueDrive on, the session uses the coordinator true-drive 1541 path (LOAD works); with it off, the simulated drive is used and existing tests stay green.



## TEST-GAMEPAD

### TEST-GAMEPAD-001

Off-console converter/merge/SOCD/fire/profile/pump behavior. Acceptance: 0 failed/0 skipped.


### TEST-GAMEPAD-002

On-console live-controller smoke. Acceptance: live pad drives JOY2/JOY1 with fire per mapping.



## TEST-IEC-TIMING

### TEST-IEC-TIMING-001

After setting IecBus.Atn=false and calling Tick() up to 985 times, Clock==false and Data==false. After ATN release and Tick() up to 985 times, both return true. 5 pass.



## TEST-IECDECODE

### TEST-IECDECODE-001

Canonical captured sequences (LOAD command frame, directory read) decode to the expected IEC event list.



## TEST-IECELEC

### TEST-IECELEC-001

With the live electrical model + true-drive, normal C64 idle CIA2 PA reads $47 and C64GS reads $07; native CIA2-IO lockstep stays green.



## TEST-IECHOTPLUG

### TEST-IECHOTPLUG-001

Tests that a drive attached mid-run answers on the bus, a detached drive removes its pulls and recomputes line states, and a renumbered drive answers its new device number.



## TEST-IECLOAD

### TEST-IECLOAD-001

LOAD"*",8,1 and LOAD"$",8 complete over IEC against a real D64 on a single-system C64.



## TEST-IECMON

### TEST-IECMON-001

Scope view-model binds trace + decoded events; host trace-delta contract returns samples since cycle N; step/rewind move the cursor.



## TEST-IECSPY

### TEST-IECSPY-001

Snapshot reports idle all-high/no-talkers; single puller -> line low + talker; multi-puller wired-AND lists all; release restores high; snapshot never mutates bus. DONE (5/5 green).



## TEST-IECTRACE

### TEST-IECTRACE-001

Edge capture order + cycle stamps; step-boundary marks; ring bound; rewind re-derivation equals original trace.



## TEST-INPROC

### TEST-INPROC-001

Assembly-reference (no ASP.NET in the portable lib) + moved-type re-run. Acceptance: EmulationPumpService.Assembly == Host.InProcess with no Microsoft.AspNetCore.* referenced.


### TEST-INPROC-002

Facade + determinism gate (bit-exact TotalCycles across two runs) + explicit-port + geometry + single-worker. Acceptance: bit-exact repro; BufferLength==FrameBuffer.Length; AppliedWorkerAffinityMask==null.


### TEST-INPROC-003

Gate zero-P/Invoke + inline-sleep-seam + no-thread-growth + Pause-unparks. Acceptance: no [DllImport]/[LibraryImport]; no new timer thread; Pause unparks.



## TEST-INPROC-DEVICE

### TEST-INPROC-DEVICE

On-console boot throughput (>=100 frames/2s, unpinned, two emulation threads). Acceptance: sustained on-device throughput.



## TEST-MED

### TEST-MED-002

FrameSequenceCapture unique-dedup, drop-on-mismatch, and host frames=unique routing.


### TEST-MED-003

WAV header/data correctness, after-stop ignore, concurrent-stop lock safety; CaptureAudioTap clamp/forward.


### TEST-MED-004

ffprobe-verified video+audio streams; Start failure cleanup; BackgroundByteWriter ordering/fault.



## TEST-NATIVERESIDUE

### TEST-NATIVERESIDUE-001

NativeResidueDiagTests: probe 01 diffs a fresh machine's 200-cycle boot state before/after a demo .vsf resume on a scratch machine; probe 02 replicates the post-activity-reset divergence (boot + full PAL frame + reset + per-cycle walk 140-260 sampling DD00/DD01, CIA1+CIA2, VIC beam, CPU). Guards the PLAN-NATIVERESIDUE-001 mechanism fix: vice_machine_create_model re-baselines Drive{8..11}TrueEmulation to the VICE default 1 via resources_set_int so a has_tde=0 DRIVE8 snapshot module cannot disable True Drive Emulation process-wide.

**Acceptance Criteria:**
- [x] Fresh-machine boot state is bit-identical before and after a .vsf snapshot resume in the same process. (evidence: TEST-NATIVE-RESIDUE-01 green post-fix; poison combo 36 passed / 0 failed / 1 skipped in one process.)
- [x] Post-activity-reset per-cycle trace (DD00/CIA/VIC/CPU, cycles 140-260) is bit-identical before and after a .vsf resume. (evidence: TEST-NATIVE-RESIDUE-02 green post-fix; full suite one process 0 failed / 2594 passed.)

### TEST-NATIVERESIDUE-002

A machine created after a scratch machine attaches/detaches a disk (or resumes a has_tde=0 .vsf) must present drive-8 attach_clk/detach_clk/attach_detach_clk == 0. Guards drive-snapshot.c uninitialized-stack + shim create-time re-baseline. Code: NativeResidueDiagTests.FreshMachine_DriveAttachDetachClocks_AreZero_AfterResidue (label TEST-NATIVE-RESIDUE-03).


### TEST-NATIVERESIDUE-003

A machine created after a scratch machine resumes a true-drive .vsf and runs activity must present drive-8 cycle_accum == 0. Guards drivecpu_reset_clk cycle_accum omission. Code: NativeResidueDiagTests.FreshMachine_DriveCycleAccum_IsZero_AfterSnapshotResumeActivity (label TEST-NATIVE-RESIDUE-04).



## TEST-PACESEL

### TEST-PACESEL-001

PacingStrategySelectionTests (factory + live SetStrategy), SettingsServiceHostTests (UpdateSettings applies PacingStrategy to the pump and round-trips it), and AttachPanelViewModelTests (pacing change flags pending, not restart).

**Acceptance Criteria:**
- [x] SetStrategy on a not-started pump switches GateName immediately; unknown defaults to Semaphore
- [x] UpdateSettings with a VICE pacing strategy sets the pump gate to VICE and round-trips vice


## TEST-PERF-RUNFRAME

### TEST-PERF-RUNFRAME-001

The benchmark harness builds a real-ROM C64 PAL machine through ArchitectureBuilder, measures IMachine.RunFrame after warmup over the required 600-frame window, reports median and p95 frame time, and proves the measured hot path allocates zero bytes on the current thread.

**Acceptance Criteria:**
- [x] Benchmark/probe builds Commodore 64 PAL through ArchitectureBuilder with the real ROM provider. (evidence: BenchmarksSmokeTests.C64PalRunFrameBenchmark_UsesRealC64Pal passed)
- [x] RunFramePerfProbe 60/600 reports median <= 18 ms, p95 <= 22 ms, and 0 allocated bytes. (evidence: median=1.575ms; p95=2.753ms; allocated=0 bytes)
- [x] Focused BasicBus/C64MemoryMap/VideoRenderer/VideoSurface/SID and Lockstep/Checkpoint gates pass with 0 failed and 0 skipped tests. (evidence: focused=182 passed; lockstep/checkpoint=333 passed)
- [x] BenchmarkDotNet C64PalRunFrameBenchmark completes and reports no managed allocation. (evidence: BenchmarkDotNet mean=2.262ms median=2.255ms; Allocated reported none)


## TEST-PUBSUB

### TEST-PUBSUB-001

Focused unit, smoke, and benchmark-probe coverage shall verify the Pub/Sub event bus contract, including typed/raw/packed delivery, unsubscription, deterministic delivery order, collision isolation, route growth, message pool exhaustion/reset, frame arena payloads, zero publish allocation, and release probe performance.

**Acceptance Criteria:**
- [x] Focused Pub/Sub test suite and benchmark smoke tests pass. (evidence: dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj --no-restore --filter FullyQualifiedName~LockFreePubSubTests|FullyQualifiedName~BenchmarksSmokeTests.PubSub => Passed: 22, Failed: 0)
- [x] Release quick probe proves the Pub/Sub performance targets without managed allocation. (evidence: dotnet run -c Release --project tests/ViceSharp.Benchmarks -- --pubsub-probe 1000000 => publish-one=43.78ns publish-three=58.14ns publish-packed=35.62ns pool-rent-return=16.80ns arena-alloc=3.20ns allocated=0 bytes)
- [x] The full solution builds after the Pub/Sub implementation. (evidence: dotnet build ViceSharp.slnx --no-restore => Build succeeded)


## TEST-QA-TESTSILENCE

### TEST-QA-TESTSILENCE-001

WindowsAudioSessionMuteTests (tests/ViceSharp.TestHarness/Audio) verifies: when a render endpoint exists the fixture engages and the process Windows audio session reads muted (endpointExists equals IsEngaged, then muted is true); disposing an engaged fixture restores the prior mute state; and construction plus double-dispose never throw on any host. Validates TR-QA-TESTSILENCE-001.



## TEST-REMOTECTRL

### TEST-REMOTECTRL-001

Tests prove the RemoteControl integration is off by default (no host started when VICESHARP_REMOTECONTROL_ENABLE is unset) and fails closed when enabled without a token; and that, when enabled with a token, the configured IRemoteControlRootProvider returns the live MainWindow. App-launch gate: connect the RemoteControl client tool and confirm the visual tree is readable.



## TEST-REVEXEC

### TEST-REVEXEC-001

RewindCycle/RewindFrame restore exact prior state; step-then-rewind round-trips; ring eviction bound honored.



## TEST-SID

### TEST-SID-002

Focused SID tests verify signed waveform centering and PCM equivalency; runtime capture verifies Pieces of Light at PAL pacing before and after Space exits the first segment.



## TEST-SIDAUDIO

### TEST-SIDAUDIO-001

SidClockRateTests drives the SID via the SystemClock and asserts OSC3 = 0x10 after 8192 master cycles at voice freq 0x8000 (was 0x01). SidAudioPumpTests recalibrated to ClockDivisor 1 (PalTicksPerFrame 19656) with sample counts unchanged.

**Acceptance Criteria:**
- [x] SidPhase_AdvancesAtPhi2Rate_WhenClockedBySystemClock passes (OSC3 == 0x10)
- [x] SidAudioPumpTests sample-count assertions still pass after recalibration


## TEST-SIDEBARUI

### TEST-SIDEBARUI-001

AttachPanelViewModelTests.CollapseExpander_DockAndGlyph_TrackAnchorSide asserts the expander dock side and chevron glyph follow the panel anchor and raise PropertyChanged.

**Acceptance Criteria:**
- [x] Anchored Left yields CollapseExpanderDock Right and glyph the left chevron
- [x] Anchored Right yields CollapseExpanderDock Left and glyph the right chevron, with PropertyChanged raised for both


## TEST-SNAPFULL

### TEST-SNAPFULL-001

Restore then run N cycles equals continuous run N (full MachineState + memory equality) at multiple cut points, for C64 and C64+1541.



## TEST-SNDREG

### TEST-SNDREG-001

ViceGateSoundRegulatorTests (13) cover EvaluateSound outcomes and boundary, gate regulator selection, back-pressure blocking advance, warp precedence, and the SID IsAudioTimingSource/QueuedSampleCount wiring.

**Acceptance Criteria:**
- [x] Back-pressure when queue at/over high-water advances zero cycles; below advances a chunk
- [x] Warp selects Warp regulator even when audio buffer is full


## TEST-SYSBTN

### TEST-SYSBTN-001

Binding table + edge/LT-hysteresis + swap-flag (off-console). Acceptance: Default matches locked table; Press once; LT one On + one Off.


### TEST-SYSBTN-002

Dispatcher command->host mapping (fakes). Acceptance: each command maps to one correct host call; Swap issues no ISettingsService call.


### TEST-SYSBTN-003

Source-gen round-trip + reset-to-defaults + remap. Acceptance: profile round-trips byte-identical; ResetToDefaults yields Default.



## TEST-SYSBTN-DEVICE

### TEST-SYSBTN-DEVICE

On-console button response (Menu/X/LT/A gating). Acceptance: system-button behaviors confirmed on device.



## TEST-SYSINDEP

### TEST-SYSINDEP-001

Tests that the drive CPU advances on its own clock (not host lockstep), IEC bus edges are observed by other systems before a read, per-system pacing sustains clock under load, and the true-drive LOAD parity test still passes.



## TEST-TAPE-RAMP

### TEST-TAPE-RAMP-001

With tape inserted, MotorEnabled=true, PlayPressed=true: TryReadNextPulse returns false during first 12,808 Tick() calls; returns true after 32,000 Tick() calls. Motor off/on resets ramp. 4 pass.



## TEST-TAPE-SENSE

### TEST-TAPE-SENSE-001

SenseLine==false when PlayPressed or RecordPressed, true otherwise. TryWritePulse returns true only when MotorEnabled && RecordPressed, incrementing RecordedPulseCount. 7 pass.



## TEST-TESTGATE

### TEST-TESTGATE-002

XboxTestConventionTests: Category=Xbox uses plain [Fact]/[Theory] + [Trait], never [ViceFact]/Assert.Skip, so 0-skipped is genuine per slice scope. Acceptance: each slice trx within its --filter reports Failed=0 and Skipped=0. (Shaped TEST id created to carry the plan's named XboxTestConventionTests, which the store rejects as a raw testId.)


### TEST-TESTGATE-008

AOT proxy + on-console feasibility (= S0). Acceptance: the three S0 go/no-go criteria GREEN.


### TEST-TESTGATE-009

DesktopRegressionGuard. Acceptance: Determinism pass-count == baseline; AvaloniaBoundaryTests green; AOT 0 warnings.


### TEST-TESTGATE-010

On-console device checklists. Acceptance: device slices marked onConsole; checklists recorded.



## TEST-TICKHIST

### TEST-TICKHIST-001

TickHistoryRecorderTests (ring order/capacity, write bundling, reconstruction), TickHistoryCaptureTests (bus event + pump capture + GetTickHistory/ReadMemoryAtTick), TickHistoryViewModelTests (refresh newest-first, paused inspect, close).

**Acceptance Criteria:**
- [x] Reconstruct at the newest tick yields current memory; at an earlier tick yields the pre-write byte
- [x] InspectAsync opens the debug screen with a memory dump only when paused

### TEST-TICKHIST-002

Verify the emulation pump does not capture when recording is disabled (default) and that GetTickHistory arms recording.

**Acceptance Criteria:**
- [x] Pump_DoesNotCaptureHistory_WhenRecordingDisabled - default off yields empty history after pumping.
- [x] GetTickHistory_ArmsHistoryRecording - calling the RPC sets HistoryRecordingEnabled true.


## TEST-UI

### TEST-UI-001

Focused ViewModel and protocol tests shall prove IEC activity appears in both peripherals and status surfaces from host telemetry.

**Acceptance Criteria:**
- [ ] ViewModel tests prove peripherals panel drive entries show IEC active and idle states from host telemetry.
- [ ] ViewModel tests prove status bar IEC activity uses the same source while preserving status fields.
- [ ] Focused UI tests run with zero failed and zero skipped tests.


## TEST-UI-DEVCARDART

### TEST-UI-DEVCARDART-001

DeviceArtKeyTests: slot-kind defaults (Drive8/9 to Breadbin, Tape to Datasette, Cartridge to Cartridge); model mapping rows (1540/1541 to Breadbin, 1541-II, 1571, 1581, SFD-1001, CMD-HD, CMD-FD/fd2000, unknown/empty to Breadbin); AllKeys superset of every resolvable key; DeviceArtKey PropertyChanged on ApplyAttachment; MarkEmpty retains last model.


### TEST-UI-DEVCARDART-002

DeviceArtAssetTests: every SVG source exists with a viewBox; DeviceArt.axaml exists, XML-parses, DrawingImage key set is a superset of the catalog keys, all 6 brushes present; per-SVG sha256 (CRLF-normalized) matches the generated source comments and the comment set equals the SVG file set; PeripheralCardView/App.axaml wiring text-asserts.


### TEST-UI-DEVCARDART-003

DeviceArtHeadlessTests (AvaloniaFact): AvaloniaXamlLoader loads DeviceArt.axaml; every catalog key resolves to a DrawingImage with non-null Drawing; DeviceArtKeyConverter returns null without throwing for unknown keys.



## TEST-UIFLYOUT

### TEST-UIFLYOUT-001

VM: toggling DockSide flips Left<->Right and IsPaneOpen opens/closes; default state correct. App-launch: flyout opens/closes, docks both sides via the single icon button, no Left/Right buttons.



## TEST-UIMENUBAR

### TEST-UIMENUBAR-001

VM: menu commands invoke the existing actions/host services (attach/eject, reset, snapshot, warp, true-drive toggle, swap joysticks). App-launch: menu structure matches the plan and commands work.



## TEST-UIPERIPHERAL

### TEST-UIPERIPHERAL-001

VM: AttachSlotViewModel exposes status/RO/activity/TrueDrive/SupportsTrueDrive and Attach/Eject route to host. App-launch: Drive8/Drive9/Tape/Cartridge all render via the one PeripheralCardView with correct per-slot controls.



## TEST-UISETTINGS

### TEST-UISETTINGS-001

VM: settings VM exposes machine/video/audio/input/limiter/resource selections + apply. App-launch: SettingsView renders + applies settings.



## TEST-VIC-CHECKPOINT

### TEST-VIC-CHECKPOINT-001

Managed PAL frame periodic raster/cycle-counter (2 Facts), native screen-RAM roundtrip and one-frame DMA read-only (2 ViceFacts), sprite-3 DMA window for 5 models (5 ViceTheory). All 9 pass.



## TEST-VIC-RC

### TEST-VIC-RC-001

After writing DEN=1/YSCROLL=0/1 and advancing to specific rasterLine/rasterX, CurrentRowCounter and IsGraphicsIdle match VICE viciisc/vicii-cycle.c:541-563 expectations. All 11 pass.



## TEST-VSFLOCKSTEP

### TEST-VSFLOCKSTEP-001

SnapshotResumeSpikeTests.ExternalX64ScVsf_FullyResumes_CpuMatchesSnapshot loads the supplied ready-c64sc-truedrive.vsf through the shim and asserts rc==0 and that the resumed MAINCPU registers equal the snapshot's MAINCPU module. Evidence: PASS (rc=0, snapshot_last_error=0, A=C2 X=02 Y=00 SP=FA PC=5769 both sides); ShimRoundTrip + SID parity 8/8; X64ScVariantLockstep 306 pass / 0 fail / 1 skip.

**Acceptance Criteria:**
- [ ] dotnet test ...TestHarness --filter ExternalX64ScVsf_FullyResumes => passed; rc=0, err=0, MAINCPU regs match.


## TEST-XAUDIO

### TEST-XAUDIO-001

Factory-null + SID inert. Acceptance: headless CreateDefault() null; SID null-backend IsAudioTimingSource==false, no samples.


### TEST-XAUDIO-002

XAudio2AudioMath queue/ring boundaries. Acceptance: full ring RoomAvailable false + drop-oldest; empty under-run silence; index wraps.


### TEST-XAUDIO-003

Volume/mute gain-once. Acceptance: EffectiveGain applied once in AudioSampleConverter.


### TEST-XAUDIO-004

Non-blocking submit + Pause unparks worker (off-console). Acceptance: SubmitSamples non-blocking; Pause releases waiter/unparks worker.



## TEST-XAUDIO-DEVICE

### TEST-XAUDIO-DEVICE

On-console audio smoke. Acceptance: SID audible; mute silences; suspend/resume clean.



## TEST-XBOXAOT

### TEST-XBOXAOT-001

Off-console AOT/trim link gate 0 IL warnings + reflection-free audit. Acceptance: publish exit 0, 0 IL2xxx/IL3xxx; ViceNative uncalled.



## TEST-XBOXCI

### TEST-XBOXCI-001

azure-pipelines.*.yml exclude device targets. Acceptance: no PublishXbox/DeployXbox in CI/release YAMLs.



## TEST-XBOXDEPLOY

### TEST-XBOXDEPLOY-007

On-console install/launch/first-frame + rollback. Acceptance: MSIX deploys clean; first frame; rollback documented/works.



## TEST-XBOXGPL

### TEST-XBOXGPL-001

License/vkm/no-ROM/source-URL staging. Acceptance: Licenses/ contents + vkm present + no ROM *.bin + compiled SourceUrl.



## TEST-XBOXPKG

### TEST-XBOXPKG-001

Csproj/slnx shape. Acceptance: core refs present, forbidden refs absent, slnx builds.


### TEST-XBOXPKG-003

Manifest XML + capability rule. Acceptance: Windows.Xbox device family, x64, internetClient-iff-download-kept.



## TEST-XBOXTOPO

### TEST-XBOXTOPO-001

Csproj shape + reference-ban. Acceptance: conditional TFM + core refs present; banned refs absent.


### TEST-XBOXTOPO-002

Default-props solution build needs no UWP workload. Acceptance: slnx build exit 0 workload-absent.



## TEST-XBOXUI

### TEST-XBOXUI-001

NavigationViewModel push/GoBack stack correctness; overlays push no stack entry. Acceptance: Settings->DeviceSetup, GoBack twice -> Home in order.


### TEST-XBOXUI-002a

FocusMapBuilder BuildGrid(3,2) edges correct, top-edge Up null, no wrap.


### TEST-XBOXUI-002b

On-console focus-visual smoke. Acceptance: high-visibility focus in TV-safe rect.


### TEST-XBOXUI-003

InputContext observer: quick menu -> non-Gameplay; closing with empty stack -> Gameplay.


### TEST-XBOXUI-004

Simulated navigation/overlay toggles never stop/dispose the pull adapter.


### TEST-XBOXUI-005a

Cadence: 10 fires/200ms -> 10 pulls +/-1.


### TEST-XBOXUI-005b

VideoSurfacePureSink forbidden-identifier scan (no RunFrame/StepInstruction/Reset/SetJoystickState/SetKeyState).


### TEST-XBOXUI-006

RETURN tile emits SetKeyState("Return",true) then false; shift-latch+F1 emits "F2"; every KeyName in known-good list; RESTORE tile calls SetRestoreState.


### TEST-XBOXUI-007

Boundary: ViewModels reference only Abstractions+Protocol+Xbox.Input; forbidden-identifier scan (no Core/Chips/Architectures/Host/Avalonia/Grpc/XAML).


### TEST-XBOXUI-008

AboutViewModel LicenseIdentifier=="GPL-2.0-or-later", AttributionText contains "VICE", SourceOfferText non-empty + URL.



## TEST-XKBD

### TEST-XKBD-001

Off-console facade + ViewModel test against a fake/spy keyboard input. Acceptance: SetRestoreState routes to NMI path keycode 0x31 distinct from SetKeyState.



## TEST-XSET

### TEST-XSET-001

Umbrella off-console suite (XboxDataPathBridgeTests, SettingsOptionCatalogTests, XboxSettingsViewModelTests, XboxDeviceSetupViewModelTests, RomProvisionEvaluatorTests, XboxRomProvisioningViewModelTests, XboxUiStateStoreTests, XboxViewModelBindabilityTests). Acceptance: 0 failed/0 skipped per slice filter.



## TEST-XSET-DEVICE

### TEST-XSET-DEVICE

On-console first-run provisioning + persistence E2E. Acceptance: ROMs provisioned + boot to READY; setting persists across relaunch; d64 attach with drive-model selector.



## TEST-XVIDEO

### TEST-XVIDEO-001

Pure byte-exact copy + no-advance spy. Acceptance: 384x272 BGRA copy sized from FrameGeometry.BufferLength; core never advanced.


### TEST-XVIDEO-002

GC.GetAllocatedBytes delta==0 steady-state render tick. Acceptance: zero-allocation over steady-state window.



## TEST-XVIDEO-DEVICE

### TEST-XVIDEO-DEVICE

On-console video smoke. Acceptance: image correct, letterboxed, no tear.
