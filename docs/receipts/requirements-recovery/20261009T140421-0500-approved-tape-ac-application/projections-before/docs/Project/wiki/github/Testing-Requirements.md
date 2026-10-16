# Testing Requirements (MCP Server)

## TEST-AIREVIEW-ROUTING

### TEST-AIREVIEW-ROUTING-001

Add aiUnit deterministic tests for strategy/default/inline/process precedence and exact Codex model/effort argument routing.
Verify both ViceSharp theory definitions select Astra/xhigh and no Grok override remains in their execution configuration or prompts.
Validate artifact schema, non-error completed status, requested and actual model/effort metadata and durable logs independently of xUnit status.
Run both paid theories with Astra/xhigh after publication/consumption verification; retain artifact paths and resolve all critical/high findings with rerun evidence.

## Acceptance Criteria
- **ac-01**: Add aiUnit deterministic tests for strategy/default/inline/process precedence and exact Codex model/effort argument routing.
- **ac-02**: Verify both ViceSharp theory definitions select Astra/xhigh and no Grok override remains in their execution configuration or prompts.
- **ac-03**: Validate artifact schema, non-error completed status, requested and actual model/effort metadata and durable logs independently of xUnit status.
- **ac-04**: Run both paid theories with Astra/xhigh after publication/consumption verification; retain artifact paths and resolve all critical/high findings with rerun evidence.

**Acceptance Criteria:**
- [ ] Add aiUnit deterministic tests for strategy/default/inline/process precedence and exact Codex model/effort argument routing.
- [ ] Verify both ViceSharp theory definitions select Astra/xhigh and no Grok override remains in their execution configuration or prompts.
- [ ] Validate artifact schema, non-error completed status, requested and actual model/effort metadata and durable logs independently of xUnit status.
- [ ] Run both paid theories with Astra/xhigh after publication/consumption verification; retain artifact paths and resolve all critical/high findings with rerun evidence.


## TEST-ARCH-CHIPGLUE

### TEST-ARCH-CHIPGLUE-001

Source-boundary, focused integration, and lockstep/checkpoint tests verify that reusable chip implementations stay free of machine-specific board and device glue. Machine-specific wiring for C64, C1541, IEC, datasette, cartridge, input, and media helper behavior is owned by Core machine/device definitions or host services.

## Acceptance Criteria
- `ChipGlueBoundaryTests` prove moved and retired helper locations: duplicate/fake chip stubs are absent from `src/ViceSharp.Chips`, Core-owned device adapters exist, and shared chip files do not contain guarded C64/C1541 board policy.
- Focused tests cover the moved or guarded behavior for C64 memory map, processor port, CIA, VIA, SID, VIC-II/video, IEC/drive, standard cartridge mapping, C64 input/VKM, datasette/TAP, and media capture.
- The `ARCH-CHIPGLUE-001` audit document inventories every remaining `src/ViceSharp.Chips` type and maps each acceptance criterion to direct evidence.
- The x64sc lockstep/checkpoint gate passes after the remediation with `0` failed and `0` skipped tests.

**Acceptance Criteria:**
- [ ] `ChipGlueBoundaryTests` prove moved and retired helper locations: duplicate/fake chip stubs are absent from `src/ViceSharp.Chips`, Core-owned device adapters exist, and shared chip files do not contain guarded C64/C1541 board policy.
- [ ] Focused tests cover the moved or guarded behavior for C64 memory map, processor port, CIA, VIA, SID, VIC-II/video, IEC/drive, standard cartridge mapping, C64 input/VKM, datasette/TAP, and media capture.
- [ ] The `ARCH-CHIPGLUE-001` audit document inventories every remaining `src/ViceSharp.Chips` type and maps each acceptance criterion to direct evidence.
- [ ] The x64sc lockstep/checkpoint gate passes after the remediation with `0` failed and `0` skipped tests.


## TEST-BASELINE-CORRECTNESS

### TEST-BASELINE-CORRECTNESS-001

Retain existing BasicBus tests with correct latched-bus expectations and a passing native-collection convention test.
Add minimal branch-sequence red regressions for both machine families and compare cycle/register/PC results to pinned VICE.
Run unchanged 5000-cycle snapshot fixture alone and after audio/dual-VIA/reset/snapshot/dispose; require passing historical performance and both investigated prefetch tests.
Retain traceability from every retired renderer/quarantine assertion to equivalent cycle-aware acceptance tests. Account for manual diagnostic relocation explicitly in the completion manifest.

## Acceptance Criteria
- **ac-01**: Retain existing BasicBus tests with correct latched-bus expectations and a passing native-collection convention test.
- **ac-02**: Add minimal branch-sequence red regressions for both machine families and compare cycle/register/PC results to pinned VICE.
- **ac-03**: Run unchanged 5000-cycle snapshot fixture alone and after audio/dual-VIA/reset/snapshot/dispose; require passing historical performance and both investigated prefetch tests.
- **ac-04**: Retain traceability from every retired renderer/quarantine assertion to equivalent cycle-aware acceptance tests. Account for manual diagnostic relocation explicitly in the completion manifest.

**Acceptance Criteria:**
- [ ] Retain existing BasicBus tests with correct latched-bus expectations and a passing native-collection convention test.
- [ ] Add minimal branch-sequence red regressions for both machine families and compare cycle/register/PC results to pinned VICE.
- [ ] Run unchanged 5000-cycle snapshot fixture alone and after audio/dual-VIA/reset/snapshot/dispose; require passing historical performance and both investigated prefetch tests.
- [ ] Retain traceability from every retired renderer/quarantine assertion to equivalent cycle-aware acceptance tests. Account for manual diagnostic relocation explicitly in the completion manifest.


## TEST-CFG

### TEST-CFG-001

Resource files, ROM/romset selection, palettes, hotkeys, autostart, peripherals, RAM init, debug resources, and limiter settings are verified with configuration and host service tests.



## TEST-CHIPSTATE

### TEST-CHIPSTATE-001

Recorder tests with a fake stateful device verify per-tick chip-state capture and that Snapshot deep-copies (a later capture does not corrupt an earlier snapshot). VIC/SID/CIA/PLA CaptureState/DecodeState round-trip exercised end to end via GetChipStateAtTick.

**Acceptance Criteria:**
- [x] OnInstructionCompleted captures the device state into the tick
- [x] Snapshot deep-copies chip state across ring rotation


## TEST-CIA

### TEST-CIA-001

CIA timers, TOD, keyboard matrix, joystick interaction, serial shift register, IRQ, and NMI behavior are verified with unit and integration tests.



## TEST-CLI-LAUNCHER

### TEST-CLI-LAUNCHER-001

VICE-style launcher parser, topology, debugcart polarity, bounded `-limitcycles`, PRG autostart dispatch, help text, and process smoke behavior are verified with parser, stub entrypoint, and real console process tests.



## TEST-COMPLETION

### TEST-COMPLETION-001

Completion manifest coverage verification detects an omitted project, category, required workload or prerequisite instead of accepting a narrower run.
Final Release build and every required manifest gate completes with zero failed/skipped/aborted tests and retained evidence.
Both fresh live passes cover Apply/Revert/Restart, validation, speed, individual RAM blocks, persistence preferences, builder open/close, effective visibility and independent host/runtime state.
Audit every changed AC against named implementation and executable evidence; the informational traceability script exit code alone cannot prove completion.
Require final Astra agreement over combined diff, requirement evidence, owned-file manifest and receipts before commit/push and local/origin SHA proof.

## Acceptance Criteria
- **ac-01**: Completion manifest coverage verification detects an omitted project, category, required workload or prerequisite instead of accepting a narrower run.
- **ac-02**: Final Release build and every required manifest gate completes with zero failed/skipped/aborted tests and retained evidence.
- **ac-03**: Both fresh live passes cover Apply/Revert/Restart, validation, speed, individual RAM blocks, persistence preferences, builder open/close, effective visibility and independent host/runtime state.
- **ac-04**: Audit every changed AC against named implementation and executable evidence; the informational traceability script exit code alone cannot prove completion.
- **ac-05**: Require final Astra agreement over combined diff, requirement evidence, owned-file manifest and receipts before commit/push and local/origin SHA proof.
- **ac-06**: Both fresh live passes and WarpModeTests satisfy existing TR-WARP-STATUS-001: LimiterRatePercent 0 and EffectiveClockPercent greater than 150, with actual runtime pacing evidence.

**Acceptance Criteria:**
- [ ] Completion manifest coverage verification detects an omitted project, category, required workload or prerequisite instead of accepting a narrower run.
- [ ] Final Release build and every required manifest gate completes with zero failed/skipped/aborted tests and retained evidence.
- [ ] Both fresh live passes cover Apply/Revert/Restart, validation, speed, individual RAM blocks, persistence preferences, builder open/close, effective visibility and independent host/runtime state.
- [ ] Audit every changed AC against named implementation and executable evidence; the informational traceability script exit code alone cannot prove completion.
- [ ] Require final Astra agreement over combined diff, requirement evidence, owned-file manifest and receipts before commit/push and local/origin SHA proof.
- [ ] Both fresh live passes and WarpModeTests satisfy existing TR-WARP-STATUS-001: LimiterRatePercent 0 and EffectiveClockPercent greater than 150, with actual runtime pacing evidence.


## TEST-CPU

### TEST-CPU-001

CPU instruction, timing, interrupt, and port behavior is verified with unit tests and VICE/Lorenz-style reference comparisons.



## TEST-CPUTICK

### TEST-CPUTICK-001

Unit tests covering ExecutedCycles increments per executed (non-stolen) cycle, per-CPU rate = executed/elapsed/target, reset zeroes counters, and the status surface lists host and peripheral CPU rates distinctly.



## TEST-CRT

### TEST-CRT-001

Standard, Ocean, EasyFlash, Action Replay, Retro Replay, and Final Cartridge mapping behavior is verified with cartridge image and banking tests.



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

Drive CPU/timing, image formats, GCR, IEC bus protocol, fast loader, and host media attachment behavior are verified with smoke, protocol, and reference tests.

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



## TEST-GRPC

### TEST-GRPC-001

Protocol, status, control, input, monitor, media, snapshot, capture, and boundary enforcement paths are verified through generated clients and host integration tests.



## TEST-HOST

### TEST-HOST-001

Host lifecycle, status, media, state, capture, diagnostics, and session ownership behavior are verified with in-process host service tests.



## TEST-HOST-DIAG

### TEST-HOST-DIAG-001

Diagnostics protocol descriptors, session enumeration, host info, current UI session resolution, performance snapshots, streaming snapshots, attach file lifecycle, and development-scoped reflection are verified with red-first unit and in-process gRPC tests.



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



## TEST-INPUT

### TEST-INPUT-001

Keyboard, joystick, mouse, lightpen, paddle, and VICE VKM behavior is verified with parser, matrix, protocol, and machine integration tests.



## TEST-INSTALL

### TEST-INSTALL-001

InstallerFileAssociationTests: wxs registers each DesktopFileAssociations entry and open command; TryGetOpenPath skips exe/flags and unknown extensions.



## TEST-MED

### TEST-MED-001

Screenshot, video, audio, synchronized capture, and format selection behavior are verified through capture metadata and round-trip output tests.


### TEST-MED-002

FrameSequenceCapture unique-dedup, drop-on-mismatch, and host frames=unique routing.


### TEST-MED-003

WAV header/data correctness, after-stop ignore, concurrent-stop lock safety; CaptureAudioTap clamp/forward.


### TEST-MED-004

ffprobe-verified video+audio streams; Start failure cleanup; BackgroundByteWriter ordering/fault.



## TEST-MEM

### TEST-MEM-001

Address decoding, RAM-under-ROM, Ultimax, VIC bank selection, color RAM, and stack/zero-page behavior are verified with unit and integration tests.



## TEST-MON

### TEST-MON-001

Disassembly, memory display, breakpoints, register operations, bank selection, watch expressions, and monitor RPC behavior are verified through monitor engine tests.



## TEST-NATIVE-LIFECYCLE

### TEST-NATIVE-LIFECYCLE-001

Add red regressions proving oracle reset does not zero live playback timing, then exact-length PCM equality for PAL and NTSC.
Run AudioThenDualViaResetSnapshotDispose repeatedly in one process for PAL and NTSC, including independent and post-sequence snapshot tests.
Run create/step timeout, stop timeout, poison-after-failure and repeat-dispose fault cases in owned child processes; assert bounded wall time and immediate later failure.
Require native prerequisites, selected workload flags and actual cycle budgets; missing DLLs/ROMs or disabled required workloads fail instead of returning early. Restore environment variables.
Build the supported patch from the pinned clean native source and verify the hash of the DLL loaded by the passing native tests.

## Acceptance Criteria
- **ac-01**: Add red regressions proving oracle reset does not zero live playback timing, then exact-length PCM equality for PAL and NTSC.
- **ac-02**: Run AudioThenDualViaResetSnapshotDispose repeatedly in one process for PAL and NTSC, including independent and post-sequence snapshot tests.
- **ac-03**: Run create/step timeout, stop timeout, poison-after-failure and repeat-dispose fault cases in owned child processes; assert bounded wall time and immediate later failure.
- **ac-04**: Require native prerequisites, selected workload flags and actual cycle budgets; missing DLLs/ROMs or disabled required workloads fail instead of returning early. Restore environment variables.
- **ac-05**: Build the supported patch from the pinned clean native source and verify the hash of the DLL loaded by the passing native tests.

**Acceptance Criteria:**
- [ ] Add red regressions proving oracle reset does not zero live playback timing, then exact-length PCM equality for PAL and NTSC.
- [ ] Run AudioThenDualViaResetSnapshotDispose repeatedly in one process for PAL and NTSC, including independent and post-sequence snapshot tests.
- [ ] Run create/step timeout, stop timeout, poison-after-failure and repeat-dispose fault cases in owned child processes; assert bounded wall time and immediate later failure.
- [ ] Require native prerequisites, selected workload flags and actual cycle budgets; missing DLLs/ROMs or disabled required workloads fail instead of returning early. Restore environment variables.
- [ ] Build the supported patch from the pinned clean native source and verify the hash of the DLL loaded by the passing native tests.


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

The benchmark harness builds a real-ROM C64 PAL machine through `ArchitectureBuilder`, measures `IMachine.RunFrame()` after warmup over the required 600-frame window, reports median and p95 frame time, and proves the measured hot path allocates zero bytes on the current thread.

## Acceptance Criteria
- `C64PalRunFrameBenchmark` builds Commodore 64 PAL through `ArchitectureBuilder` with the real ROM provider.
- `RunFramePerfProbe` reports median `<= 18 ms`, p95 `<= 22 ms`, and `0` allocated bytes for the 60 warmup / 600 measured frame workflow.
- Focused BasicBus/C64MemoryMap/VideoRenderer/VideoSurface/SID tests pass with `0` failed and `0` skipped.
- Lockstep and checkpoint gates pass with `0` failed and `0` skipped.
- BenchmarkDotNet completes `C64PalRunFrameBenchmark` and reports no managed allocation.

**Acceptance Criteria:**
- [x] Benchmark/probe builds Commodore 64 PAL through ArchitectureBuilder with the real ROM provider. (evidence: BenchmarksSmokeTests.C64PalRunFrameBenchmark_UsesRealC64Pal passed)
- [x] RunFramePerfProbe 60/600 reports median <= 18 ms, p95 <= 22 ms, and 0 allocated bytes. (evidence: median=1.575ms; p95=2.753ms; allocated=0 bytes)
- [x] Focused BasicBus/C64MemoryMap/VideoRenderer/VideoSurface/SID and Lockstep/Checkpoint gates pass with 0 failed and 0 skipped tests. (evidence: focused=182 passed; lockstep/checkpoint=333 passed)
- [x] BenchmarkDotNet C64PalRunFrameBenchmark completes and reports no managed allocation. (evidence: BenchmarkDotNet mean=2.262ms median=2.255ms; Allocated reported none)


## TEST-PRF

### TEST-PRF-001

C64, C64C, SX-64, C128, VIC-20, PET, Plus/4, and C16 profiles are verified for required devices, ROMs, clocks, and address maps.



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



## TEST-REMOTE-INTEGRATION

### TEST-REMOTE-INTEGRATION-001

Run all seven integration tests with zero failures and zero skips, including server-side collection create and cleanup.
Verify optional bridge authentication is sent consistently by the caller-owned client for connection and search.
Verify authenticated discovery/download/launch of the required C64 seed against the existing remote endpoints. Missing access, disabled provisioning or absent seed blocks completion.

## Acceptance Criteria
- **ac-01**: Run all seven integration tests with zero failures and zero skips, including server-side collection create and cleanup.
- **ac-02**: Verify optional bridge authentication is sent consistently by the caller-owned client for connection and search.
- **ac-03**: Verify authenticated discovery/download/launch of the required C64 seed against the existing remote endpoints. Missing access, disabled provisioning or absent seed blocks completion.

**Acceptance Criteria:**
- [ ] Run all seven integration tests with zero failures and zero skips, including server-side collection create and cleanup.
- [ ] Verify optional bridge authentication is sent consistently by the caller-owned client for connection and search.
- [ ] Verify authenticated discovery/download/launch of the required C64 seed against the existing remote endpoints. Missing access, disabled provisioning or absent seed blocks completion.


## TEST-REMOTECTRL

### TEST-REMOTECTRL-001

Tests prove the RemoteControl integration is off by default (no host started when VICESHARP_REMOTECONTROL_ENABLE is unset) and fails closed when enabled without a token; and that, when enabled with a token, the configured IRemoteControlRootProvider returns the live MainWindow. App-launch gate: connect the RemoteControl client tool and confirm the visual tree is readable.

## Acceptance Criteria
- **ac-01**: Test unset enable switch and enabled-without-token startup; both expose no usable control host.
- **ac-02**: Launch the owned app with explicit valid enable/token settings; read capabilities and the live MainWindow tree over gRPC.
- **ac-03**: Verify missing/wrong bearer token rejection, loopback policy and separate denied/enabled action/frame permissions.
- **ac-04**: Retain the two fresh final-binary live passes and real gRPC CLI contract evidence required by TEST-REMOTECTRL-CLI-001 and TEST-COMPLETION-001.

**Acceptance Criteria:**
- [ ] Test unset enable switch and enabled-without-token startup; both expose no usable control host.
- [ ] Launch the owned app with explicit valid enable/token settings; read capabilities and the live MainWindow tree over gRPC.
- [ ] Verify missing/wrong bearer token rejection, loopback policy and separate denied/enabled action/frame permissions.
- [ ] Retain the two fresh final-binary live passes and real gRPC CLI contract evidence required by TEST-REMOTECTRL-CLI-001 and TEST-COMPLETION-001.


## TEST-REMOTECTRL-CLI

### TEST-REMOTECTRL-CLI-001

Restore/build/test an isolated checkout with no sibling paths and verify published package resolution.
Use a real in-process gRPC server to verify bearer authentication, default-disabled and action permissions.
Exercise exact-ID precedence, ambiguous matches, ancestor-hidden nodes and malformed trees.
Exercise failed mutations, cancellation/deadlines, unsupported transport, malformed frame/PNG handling and CLI exit codes.
Verify capability/tree JSON and successful enabled control actions against generated gRPC contracts.

## Acceptance Criteria
- **ac-01**: Restore/build/test an isolated checkout with no sibling paths and verify published package resolution.
- **ac-02**: Use a real in-process gRPC server to verify bearer authentication, default-disabled and action permissions.
- **ac-03**: Exercise exact-ID precedence, ambiguous matches, ancestor-hidden nodes and malformed trees.
- **ac-04**: Exercise failed mutations, cancellation/deadlines, unsupported transport, malformed frame/PNG handling and CLI exit codes.
- **ac-05**: Verify capability/tree JSON and successful enabled control actions against generated gRPC contracts.

**Acceptance Criteria:**
- [ ] Restore/build/test an isolated checkout with no sibling paths and verify published package resolution.
- [ ] Use a real in-process gRPC server to verify bearer authentication, default-disabled and action permissions.
- [ ] Exercise exact-ID precedence, ambiguous matches, ancestor-hidden nodes and malformed trees.
- [ ] Exercise failed mutations, cancellation/deadlines, unsupported transport, malformed frame/PNG handling and CLI exit codes.
- [ ] Verify capability/tree JSON and successful enabled control actions against generated gRPC contracts.


## TEST-REVEXEC

### TEST-REVEXEC-001

RewindCycle/RewindFrame restore exact prior state; step-then-rewind round-trips; ring eviction bound honored.



## TEST-ROMM-AVUI

### TEST-ROMM-AVUI-001

Build, headless view-model, and end-to-end validation prove LibraryView and ListsView binding, connection, browse, search, list, collection, and launch behavior.



## TEST-ROMM-DETAIL

### TEST-ROMM-DETAIL-001

RomMGateway detail mapping and RomDetailViewModel add-to-collection behavior are proven by deterministic automated tests for files, cover, summary, launchability, and collection mutation.



## TEST-ROMM-PKG

### TEST-ROMM-PKG-001

Package metadata and pack-output tests prove both RomM libraries are packable with the approved package ids, GPL-2.0-or-later metadata, and exact dependency graph.



## TEST-ROMM-SEC

### TEST-ROMM-SEC-001

`RomMCoverImageSourceTests` verifies anonymous public covers and rejection of absolute authenticated-path inputs. `RomMGatewayDownloadTests` verifies filename containment and that truncated content never publishes a partial cache entry. `FileRomMConnectionStoreTests` verifies current-user DPAPI round-trip and immediate migration of legacy plaintext tokens.



## TEST-SETTINGS-TXN

### TEST-SETTINGS-TXN-001

Add host regression cases for stage-restart Apply, retained restart after Revert, successful Apply+Restart and failed Apply/Restart with unchanged runtime/Accepted/Draft.
Inject invalid family/profile, RAM, pacing/resource, image/preset and IEC root/unit/conflict inputs; assert no partial mutation or transient success notification.
Inject replacement preparation and attachment failures; assert rollback and candidate cleanup without writeback.
Verify Warp changes in the single UpdateSettings call, status rate 0, runtime uncapped behavior and preserved standalone Warp RPC compatibility.
Race refresh against Apply, deliver out-of-order responses, edit one or multiple fields, and verify stale rejection and three-way field merge.
Round-trip every RAM/cart/uIEC snapshot and INI field, legacy defaults and independent save-on-exit preferences.
Test old responses with absent State: readable display, explicit upgrade message and disabled state-aware Apply/Restart.
Verify actual cartridge type, detach, image/bank/preset/writeback and uIEC behavior after relevant family and profile restart.

## Acceptance Criteria
- **ac-01**: Add host regression cases for stage-restart Apply, retained restart after Revert, successful Apply+Restart and failed Apply/Restart with unchanged runtime/Accepted/Draft.
- **ac-02**: Inject invalid family/profile, RAM, pacing/resource, image/preset and IEC root/unit/conflict inputs; assert no partial mutation or transient success notification.
- **ac-03**: Inject replacement preparation and attachment failures; assert rollback and candidate cleanup without writeback.
- **ac-04**: Verify Warp changes in the single UpdateSettings call, status rate 0, runtime uncapped behavior and preserved standalone Warp RPC compatibility.
- **ac-05**: Race refresh against Apply, deliver out-of-order responses, edit one or multiple fields, and verify stale rejection and three-way field merge.
- **ac-06**: Round-trip every RAM/cart/uIEC snapshot and INI field, legacy defaults and independent save-on-exit preferences.
- **ac-07**: Test old responses with absent State: readable display, explicit upgrade message and disabled state-aware Apply/Restart.
- **ac-08**: Verify actual cartridge type, detach, image/bank/preset/writeback and uIEC behavior after relevant family and profile restart.

**Acceptance Criteria:**
- [ ] Add host regression cases for stage-restart Apply, retained restart after Revert, successful Apply+Restart and failed Apply/Restart with unchanged runtime/Accepted/Draft.
- [ ] Inject invalid family/profile, RAM, pacing/resource, image/preset and IEC root/unit/conflict inputs; assert no partial mutation or transient success notification.
- [ ] Inject replacement preparation and attachment failures; assert rollback and candidate cleanup without writeback.
- [ ] Verify Warp changes in the single UpdateSettings call, status rate 0, runtime uncapped behavior and preserved standalone Warp RPC compatibility.
- [ ] Race refresh against Apply, deliver out-of-order responses, edit one or multiple fields, and verify stale rejection and three-way field merge.
- [ ] Round-trip every RAM/cart/uIEC snapshot and INI field, legacy defaults and independent save-on-exit preferences.
- [ ] Test old responses with absent State: readable display, explicit upgrade message and disabled state-aware Apply/Restart.
- [ ] Verify actual cartridge type, detach, image/bank/preset/writeback and uIEC behavior after relevant family and profile restart.


## TEST-SID

### TEST-SID-001

Oscillator, waveform, filter, ADSR, modulation, sync, noise, digi, external input, and multi-SID behavior are verified with deterministic audio/register tests.


### TEST-SID-002

SID waveform centering, PCM equivalency, and Pieces of Light runtime capture validation prove that live audio back-pressure paces the emulator like VICE before and after runtime segment transitions.



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


## TEST-SNP

### TEST-SNP-001

Save/load, deterministic replay, and state diff behavior are verified through round-trip and byte/trace comparison tests.


### TEST-SNP-002

Prove native VICE .vsf as the canonical ViceSharp snapshot format using the existing managed resume bridge after productionization and completion: default public and desktop snapshot paths, native x64sc/xvic read and write, complete managed state transfer, bidirectional round trips, 100000-cycle lockstep, transactionality, corruption handling, and mandatory non-skipped prerequisites.

**Acceptance Criteria:**
- [ ] Load mandatory x64sc and xvic .vsf fixtures through the managed production path and compare every supported module field with the native VICE oracle.
- [ ] Save C64 and VIC-20 state from ViceSharp as .vsf, load the files in x64sc and xvic, and prove matching represented state.
- [ ] Run VICE-to-ViceSharp-to-VICE and ViceSharp-to-VICE-to-ViceSharp round trips and prove all supported module fields survive.
- [ ] From each restored C64 and VIC-20 checkpoint, run at least 100000 cycles in lockstep and fail at the first CPU, bus, video, timer, interrupt, or mapped-memory divergence.
- [ ] Accept pinned-reader-compatible 58-byte current and 37-byte legacy VICE headers; walk 22-byte module headers with little-endian inclusive total lengths to exact EOF; reject wrong machine, unsupported versions, invalid/missing/duplicate mandatory modules, truncated framing, invalid or overflowing lengths, trailing bytes, and corruption rejected by structure or native VICE without visible mutation. Do not claim arbitrary bit-flip detection because VSF has no general checksum.
- [ ] Treat both shims, the pinned VICE commit, exact MSYS2 MINGW64 tool paths, exact ROM-hash allowlist, and fixture/state-transfer manifests as mandatory prerequisites. Missing or stale prerequisites fail Category=NativeViceSnapshot with zero skipped tests.
- [ ] Verify CaptureSnapshot, Avalonia .vsf file save/load, quick save/load, and ten named slots use native VSF by default; unsupported hosts return unavailable without fallback. Only generated protobuf ImportLegacySnapshot accepts the two proprietary formats, normal restore rejects them, and no capture emits them. UWP/Xbox implementation is not changed or claimed complete.
- [ ] Build and inspect the local win-x64 publish and MSI for both shims, the recursively verified loose PE closure staged after publish and before WiX, VICE license/source-offer files, and no ROMs; install locally and pass default C64/VIC-20 native-VSF capture/restore smoke from the installed app.


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



## TEST-TAP

### TEST-TAP-001

Datasette motor, TAP parsing, pulse timing, write behavior, and turbo loader compatibility are verified with unit and timing tests.



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

Sidebar, status bar, attach panel, settings, keyboard map selection, monitor dock/pop-out, focus, and startup behavior are verified with fake host clients.

**Acceptance Criteria:**
- [ ] ViewModel tests prove peripherals panel drive entries show IEC active and idle states from host telemetry.
- [ ] ViewModel tests prove status bar IEC activity uses the same source while preserving status fields.
- [ ] Focused UI tests run with zero failed and zero skipped tests.


## TEST-UI-COREBOUNDARY

### TEST-UI-COREBOUNDARY-001

Extend boundary scanner coverage to relevant real C# and AXAML sources and demonstrate the existing direct dependency as a red case.
Exercise profile selection, bank updates, import, build, save and failure reporting through the abstraction with controlled file I/O and collection notifications.

## Acceptance Criteria
- **ac-01**: Extend boundary scanner coverage to relevant real C# and AXAML sources and demonstrate the existing direct dependency as a red case.
- **ac-02**: Exercise profile selection, bank updates, import, build, save and failure reporting through the abstraction with controlled file I/O and collection notifications.

**Acceptance Criteria:**
- [ ] Extend boundary scanner coverage to relevant real C# and AXAML sources and demonstrate the existing direct dependency as a red case.
- [ ] Exercise profile selection, bank updates, import, build, save and failure reporting through the abstraction with controlled file I/O and collection notifications.


## TEST-UI-DEVCARDART

### TEST-UI-DEVCARDART-001

DeviceArtKeyTests: slot-kind defaults (Drive8/9 to Breadbin, Tape to Datasette, Cartridge to Cartridge); model mapping rows (1540/1541 to Breadbin, 1541-II, 1571, 1581, SFD-1001, CMD-HD, CMD-FD/fd2000, unknown/empty to Breadbin); AllKeys superset of every resolvable key; DeviceArtKey PropertyChanged on ApplyAttachment; MarkEmpty retains last model.


### TEST-UI-DEVCARDART-002

DeviceArtAssetTests: every SVG source exists with a viewBox; DeviceArt.axaml exists, XML-parses, DrawingImage key set is a superset of the catalog keys, all 6 brushes present; per-SVG sha256 (CRLF-normalized) matches the generated source comments and the comment set equals the SVG file set; PeripheralCardView/App.axaml wiring text-asserts.


### TEST-UI-DEVCARDART-003

DeviceArtHeadlessTests (AvaloniaFact): AvaloniaXamlLoader loads DeviceArt.axaml; every catalog key resolves to a DrawingImage with non-null Drawing; DeviceArtKeyConverter returns null without throwing for unknown keys.



## TEST-UI-DIAG

### TEST-UI-DIAG-001

The Avalonia shell exposes a Copy Debug Attach Info command and formats endpoint, current session id, app version, attach JSON, and latest status for clipboard use.



## TEST-UIDROP

### TEST-UIDROP-002

Verify PRG drop acceptance and host RAM load: `ShellViewModel` routes `*.prg` to `LoadProgramAsync` without attach/reset; `IsDropStartSupported` is true for `.prg` and existing media and false for unsupported types; `PrgMemoryLoader` writes at the load address, updates BASIC pointers only when load equals TXTTAB, and reports ran; `EmulatorHostService.LoadProgramAsync` loads payload into the session, sets Ran, and starts BASIC RUN automation only for BASIC-start PRGs; invalid PRGs return InvalidArgument; `GrpcEmulatorHostService` maps LoadProgram request/response fields.



## TEST-UIFLYOUT

### TEST-UIFLYOUT-001

VM: toggling DockSide flips Left<->Right and IsPaneOpen opens/closes; default state correct. App-launch: flyout opens/closes, docks both sides via the single icon button, no Left/Right buttons.



## TEST-UIMENUBAR

### TEST-UIMENUBAR-001

VM: menu commands invoke the existing actions/host services (attach/eject, reset, snapshot, warp, true-drive toggle, swap joysticks). App-launch: menu structure matches the plan and commands work.



## TEST-UIPERIPHERAL

### TEST-UIPERIPHERAL-001

VM: AttachSlotViewModel exposes status/RO/activity/TrueDrive/SupportsTrueDrive and Attach/Eject route to host. App-launch: Drive8/Drive9/Tape/Cartridge all render via the one PeripheralCardView with correct per-slot controls.



## TEST-UISET

### TEST-UISET-001

VIC-20 RAM settings acceptance verifies all selected BLK regions in fresh host/runtime state after Apply+Restart and exactly 28159 BASIC bytes free with the pinned ROM.

## Acceptance Criteria
- **ac-all-spec**: All+Apply+Restart keeps BLK checks and Vic20MemorySpec all.
- **ac-bytes-free**: After pinned-ROM VIC-20 All RAM plus Apply+Restart, the READY BASIC boot reports exactly 28159 BYTES FREE; verify actual runtime memory as well as checked BLK controls.

**Acceptance Criteria:**
- [ ] All+Apply+Restart keeps BLK checks and Vic20MemorySpec all.
- [ ] After pinned-ROM VIC-20 All RAM plus Apply+Restart, the READY BASIC boot reports exactly 28159 BYTES FREE; verify actual runtime memory as well as checked BLK controls.

### TEST-UISET-002

Two fresh final-binary RemoteControl passes exercise every applicable VIC-20 Settings control. Verify effective visibility, Active/Accepted/Draft semantics, single-transaction Warp above150 percent, PAL/NTSC runtime geometry/clocks, Revert, cartridge persistence and actual uIEC I/O.

## Acceptance Criteria
- **ac-inventory**: In each of two fresh final-binary live passes, drive every applicable VIC-20 Settings control and verify effective visibility, fresh host Active/Accepted state and actual runtime effects, including image/bank/preset/writeback and uIEC I/O.
- **ac-warp**: Warp via the single Settings transaction sets LIMITER WARP, LimiterRatePercent 0 and measured EffectiveClockPercent greater than 150 in WarpModeTests and live RemoteControl receipts.
- **ac-palntsc**: PAL vs NTSC Apply+Restart yields CLOCK ~1.108 vs ~1.023 MHz and ContentHeight 284 vs 234 with READY.
- **ac-revert**: Revert is exercised while dirty and restores Accepted values without cancelling an earlier accepted pending restart; save-on-exit preferences remain independent.
- **ac-state-and-runtime**: Apply, Apply+Restart, validation failure, speed cycling, every RAM BLK, persistence preferences and builder open/close follow TEST-SETTINGS-TXN-001 and TEST-COMPLETION-001, with host/runtime oracles after refreshing owned-process debug attach and session IDs.

**Acceptance Criteria:**
- [ ] In each of two fresh final-binary live passes, drive every applicable VIC-20 Settings control and verify effective visibility, fresh host Active/Accepted state and actual runtime effects, including image/bank/preset/writeback and uIEC I/O.
- [ ] Warp via the single Settings transaction sets LIMITER WARP, LimiterRatePercent 0 and measured EffectiveClockPercent greater than 150 in WarpModeTests and live RemoteControl receipts.
- [ ] PAL vs NTSC Apply+Restart yields CLOCK ~1.108 vs ~1.023 MHz and ContentHeight 284 vs 234 with READY.
- [ ] Revert is exercised while dirty and restores Accepted values without cancelling an earlier accepted pending restart; save-on-exit preferences remain independent.
- [ ] Apply, Apply+Restart, validation failure, speed cycling, every RAM BLK, persistence preferences and builder open/close follow TEST-SETTINGS-TXN-001 and TEST-COMPLETION-001, with host/runtime oracles after refreshing owned-process debug attach and session IDs.

### TEST-UISET-003

Headless and live RemoteControl tests for C64 variants (at least c64, c64c, ntsc from host catalog) mutate shared Settings controls, Apply+Restart PAL vs NTSC CLOCK ~0.985 vs ~1.023 MHz with READY, Warp LIMITER WARP and CLOCK percent well above 100, and assert VIC-20-only AutomationIds are not effectively visible on C64 and are visible on VIC-20.

## Acceptance Criteria
- **ac-c64-inventory**: Shared Settings inventory mutates and sticks for C64 models c64, c64c, and ntsc.
- **ac-c64-palntsc-warp**: C64 PAL versus NTSC boots READY with clocks approximately 0.985 versus 1.023 MHz; Warp status and live runtime satisfy TR-WARP-STATUS-001, including EffectiveClockPercent greater than 150.
- **ac-visibility**: VIC-20-only AutomationIds are not effectively visible on C64 Computer and are visible on VIC-20 Computer.
- **ac-catalog-14**: Automatically cover all 14 available C64 host catalog profiles and live c64/c64c/ntsc representatives. Cover case-insensitive IDs, unavailable/out-of-family/null/reentry rejection, stable Models within family and disabled Apply with a visible explanation for unusable catalog.
- **ac-draft-preservation**: Exercise shared synchronization for initial load, refresh, Apply, Revert and persisted state, preserving VIC-20 draft RAM/cart fields across family switches without attaching VIC-20 hardware to a C64 runtime.

**Acceptance Criteria:**
- [ ] Shared Settings inventory mutates and sticks for C64 models c64, c64c, and ntsc.
- [ ] C64 PAL versus NTSC boots READY with clocks approximately 0.985 versus 1.023 MHz; Warp status and live runtime satisfy TR-WARP-STATUS-001, including EffectiveClockPercent greater than 150.
- [ ] VIC-20-only AutomationIds are not effectively visible on C64 Computer and are visible on VIC-20 Computer.
- [ ] Automatically cover all 14 available C64 host catalog profiles and live c64/c64c/ntsc representatives. Cover case-insensitive IDs, unavailable/out-of-family/null/reentry rejection, stable Models within family and disabled Apply with a visible explanation for unusable catalog.
- [ ] Exercise shared synchronization for initial load, refresh, Apply, Revert and persisted state, preserving VIC-20 draft RAM/cart fields across family switches without attaching VIC-20 hardware to a C64 runtime.


## TEST-UISETTINGS

### TEST-UISETTINGS-001

VM: settings VM exposes machine/video/audio/input/limiter/resource selections + apply. App-launch: SettingsView renders + applies settings.



## TEST-VIA

### TEST-VIA-001

VIA timer, shift register, port handshake, VIC-20, and drive integration behavior are verified with unit and machine integration tests.



## TEST-VIC

### TEST-VIC-001

Raster timing, display modes, sprites, collisions, badlines, borders, FLI/AFLI, banking, and DMA timing are verified with deterministic frame or trace comparisons. The gate includes closed-border sprite masking, open-border sprite visibility, sprite priority over background/foreground pixels, per-model sprite DMA access timing, and VIC-II matrix/idle fetch behavior including prefetch `$ff` fill and ECM idle graphics addresses.



## TEST-VIC-CHECKPOINT

### TEST-VIC-CHECKPOINT-001

Managed PAL frame periodic raster/cycle-counter (2 Facts), native screen-RAM roundtrip and one-frame DMA read-only (2 ViceFacts), sprite-3 DMA window for 5 models (5 ViceTheory). All 9 pass.



## TEST-VIC-RC

### TEST-VIC-RC-001

After writing DEN=1/YSCROLL=0/1 and advancing to specific rasterLine/rasterX, CurrentRowCounter and IsGraphicsIdle match VICE viciisc/vicii-cycle.c:541-563 expectations. All 11 pass.



## TEST-VIC20

### TEST-VIC20-001

Vic20PixelLockstep verifies normal-border geometry and named SequenceEqual facts vs xvic capture_visible_frame: Index_ReadyPal_SequenceEqual, Index_ReadyNtsc_SequenceEqual, Index_BusyPal_SequenceEqual, Bgra_ReadyPal_SequenceEqual, Bgra_ReadyNtsc_SequenceEqual, Bgra_BusyPal_SequenceEqual. Capture is non-sentinel with opaque alpha (NTSC unpainted clamp rows opaque black). Other border modes remain Partial; not whole-machine Exact.

**Acceptance Criteria:**
- [x] Vic20PixelLockstep.Index_ReadyPal_SequenceEqual passes 0 fail 0 skip (evidence: isolated pixel gate 2026-09-04)
- [x] Vic20PixelLockstep.Index_ReadyNtsc_SequenceEqual passes 0 fail 0 skip (evidence: isolated pixel gate 2026-09-04)
- [x] Vic20PixelLockstep.Index_BusyPal_SequenceEqual passes 0 fail 0 skip (evidence: isolated pixel gate 2026-09-04)
- [x] Vic20PixelLockstep.Bgra_ReadyPal_SequenceEqual and Bgra_BusyPal_SequenceEqual pass 0 fail 0 skip (evidence: isolated pixel gate 2026-09-04)
- [x] Vic20PixelLockstep.Bgra_ReadyNtsc_SequenceEqual passes 0 fail 0 skip (opaque clamp-row alpha) (evidence: isolated pixel gate 2026-09-04)


## TEST-VIC20-FLASH

### TEST-VIC20-FLASH-001

`Flash040EraseLatencyTests` verifies the VICE TYPE_B sector timeout, sector/chip cycle budgets, busy status, cancel, suspend, and resume behavior. `Vic20ExpansionCartTests`, `UltimemParityTests`, `MegaCartParityTests`, and `MediaServiceHostVic20ExpansionTests` verify mapping, dirty-state separation, and atomic detach persistence.



## TEST-VIC20-SOUND

### TEST-VIC20-SOUND-001

`Vic20SoundLockstep` verifies deterministic managed silence/tone/multi-register output, no silent register-store allocation after warmup, and focused byte-identical silence/tone batches against native xvic. `Vic20AudioWiringTests` verifies machine-clock advancement and 256-sample delivery to the configured audio backend.



## TEST-VICE

### TEST-VICE-001

Verify that every native parity oracle uses the pinned unmodified VICE target and that wrapper-only integration fails closed when required target observations are unavailable.

**Acceptance Criteria:**
- [ ] A clean-target preflight passes for the pinned native/vice commit and a simulated dirty-target fixture is rejected before parity execution.
- [ ] The wrapper integration test proves no tracked native/vice file changes across build and execution.
- [ ] Oracle receipts prove native commit identity, clean tracked source, wrapper provenance, exact commands, and failed 0 skipped 0 for the claimed gate.
- [ ] When a required real bus or timing observation is unavailable, the test fails at that first missing observation and does not substitute PeekBus, PC inference, fabricated data, or a modified native build.


## TEST-VSFLOCKSTEP

### TEST-VSFLOCKSTEP-001

The current focused external-x64sc VSF acceptance gate must load the committed hash-pinned ready-c64sc-truedrive.vsf through the pinned native shim, prove the complete 24-module container frames exactly to EOF, assert rc=0 and snapshot_last_error=0, and compare resumed MAINCPU registers to the MAINCPU module. Historical pass or skip counts are not evidence; this record remains pending until current zero-skip execution proof exists.

**Acceptance Criteria:**
- [ ] The focused ExternalX64ScVsf_FullyResumes_CpuMatchesSnapshot gate runs without skipped prerequisites, loads the hash-pinned 24-module fixture through pinned x64sc with rc=0 and snapshot_last_error=0, and proves resumed A, X, Y, SP, and PC equal the MAINCPU module values.


## TEST-X64SC-LOCKSTEP

### TEST-X64SC-LOCKSTEP-001

Every required x64sc model profile is validated against native x64sc for deterministic startup, BASIC prompt, keyboard input, disk attach/autostart, cartridge boot, and reset scenarios. Validation compares CPU registers, flags, cycle count, selected memory windows, CIA/VIC/SID observable register state, IRQ/NMI state, and frame/raster checkpoints. Missing ROMs, unavailable native x64sc binaries, unsupported variants, skipped variants, or stubbed checks fail the gate.



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


### TEST-XBOXGPL-002

Automated tests must fail if C= logo attribution is missing from THIRD_PARTY_NOTICES.md
or from AboutInfo/AboutViewModel. Cover Alien426, CC BY-SA 4.0, and commons.wikimedia.org.



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
