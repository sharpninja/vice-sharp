# Testing Requirements (MCP Server)

## TEST-CHIPSTATE

### TEST-CHIPSTATE-001

Recorder tests with a fake stateful device verify per-tick chip-state capture and that Snapshot deep-copies (a later capture does not corrupt an earlier snapshot). VIC/SID/CIA/PLA CaptureState/DecodeState round-trip exercised end to end via GetChipStateAtTick.

**Acceptance Criteria:**
- [x] OnInstructionCompleted captures the device state into the tick
- [x] Snapshot deep-copies chip state across ring rotation


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



## TEST-DRVLED

### TEST-DRVLED-001

Drive DOS ROM setting VIA2 PB3 sets the drive LedOn; per-drive status DTO reflects it; the card LED view-model binds it.



## TEST-DRVLIFE

### TEST-DRVLIFE-001

Attach creates+clocks+bus-connects the drive instance; detach unregisters from clock and disconnects from bus.



## TEST-DRVTRUE

### TEST-DRVTRUE-001

VM: TrueDrive toggles per drive (default off). Runtime: with TrueDrive on, the session uses the coordinator true-drive 1541 path (LOAD works); with it off, the simulated drive is used and existing tests stay green.



## TEST-IEC-TIMING

### TEST-IEC-TIMING-001

After setting IecBus.Atn=false and calling Tick() up to 985 times, Clock==false and Data==false. After ATN release and Tick() up to 985 times, both return true. 5 pass.



## TEST-IECDECODE

### TEST-IECDECODE-001

Canonical captured sequences (LOAD command frame, directory read) decode to the expected IEC event list.



## TEST-IECELEC

### TEST-IECELEC-001

With the live electrical model + true-drive, normal C64 idle CIA2 PA reads $47 and C64GS reads $07; native CIA2-IO lockstep stays green.



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


## TEST-REMOTECTRL

### TEST-REMOTECTRL-001

Tests prove the RemoteControl integration is off by default (no host started when VICESHARP_REMOTECONTROL_ENABLE is unset) and fails closed when enabled without a token; and that, when enabled with a token, the configured IRemoteControlRootProvider returns the live MainWindow. App-launch gate: connect the RemoteControl client tool and confirm the visual tree is readable.



## TEST-REVEXEC

### TEST-REVEXEC-001

RewindCycle/RewindFrame restore exact prior state; step-then-rewind round-trips; ring eviction bound honored.



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


## TEST-TAPE-RAMP

### TEST-TAPE-RAMP-001

With tape inserted, MotorEnabled=true, PlayPressed=true: TryReadNextPulse returns false during first 12,808 Tick() calls; returns true after 32,000 Tick() calls. Motor off/on resets ramp. 4 pass.



## TEST-TAPE-SENSE

### TEST-TAPE-SENSE-001

SenseLine==false when PlayPressed or RecordPressed, true otherwise. TryWritePulse returns true only when MotorEnabled && RecordPressed, incrementing RecordedPulseCount. 7 pass.



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
