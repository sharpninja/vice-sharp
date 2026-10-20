# Testing Requirements (QBrain.AI)

- TEST-AIREVIEW-ROUTING-001: Add aiUnit deterministic tests for strategy/default/inline/process precedence and exact Codex model/effort argument routing.
Verify both ViceSharp theory definitions select Astra/xhigh and no Grok override remains in their execution configuration or prompts.
Validate artifact schema, non-error completed status, requested and actual model/effort metadata and durable logs independently of xUnit status.
Run both paid theories with Astra/xhigh after publication/consumption verification; retain artifact paths and resolve all critical/high findings with rerun evidence.

## Acceptance Criteria
- **ac-01**: Add aiUnit deterministic tests for strategy/default/inline/process precedence and exact Codex model/effort argument routing.
- **ac-02**: Verify both ViceSharp theory definitions select Astra/xhigh and no Grok override remains in their execution configuration or prompts.
- **ac-03**: Validate artifact schema, non-error completed status, requested and actual model/effort metadata and durable logs independently of xUnit status.
- **ac-04**: Run both paid theories with Astra/xhigh after publication/consumption verification; retain artifact paths and resolve all critical/high findings with rerun evidence.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Add aiUnit deterministic tests for strategy/default/inline/process precedence and exact Codex model/effort argument routing.
  - [ ] Verify both ViceSharp theory definitions select Astra/xhigh and no Grok override remains in their execution configuration or prompts.
  - [ ] Validate artifact schema, non-error completed status, requested and actual model/effort metadata and durable logs independently of xUnit status.
  - [ ] Run both paid theories with Astra/xhigh after publication/consumption verification; retain artifact paths and resolve all critical/high findings with rerun evidence.
- TEST-ARCH-CHIPGLUE-001: Source-boundary, focused integration, and lockstep/checkpoint tests verify that reusable chip implementations stay free of machine-specific board and device glue. Machine-specific wiring for C64, C1541, IEC, datasette, cartridge, input, and media helper behavior is owned by Core machine/device definitions or host services.

## Acceptance Criteria
- `ChipGlueBoundaryTests` prove moved and retired helper locations: duplicate/fake chip stubs are absent from `src/ViceSharp.Chips`, Core-owned device adapters exist, and shared chip files do not contain guarded C64/C1541 board policy.
- Focused tests cover the moved or guarded behavior for C64 memory map, processor port, CIA, VIA, SID, VIC-II/video, IEC/drive, standard cartridge mapping, C64 input/VKM, datasette/TAP, and media capture.
- The `ARCH-CHIPGLUE-001` audit document inventories every remaining `src/ViceSharp.Chips` type and maps each acceptance criterion to direct evidence.
- The x64sc lockstep/checkpoint gate passes after the remediation with `0` failed and `0` skipped tests.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] `ChipGlueBoundaryTests` prove moved and retired helper locations: duplicate/fake chip stubs are absent from `src/ViceSharp.Chips`, Core-owned device adapters exist, and shared chip files do not contain guarded C64/C1541 board policy.
  - [ ] Focused tests cover the moved or guarded behavior for C64 memory map, processor port, CIA, VIA, SID, VIC-II/video, IEC/drive, standard cartridge mapping, C64 input/VKM, datasette/TAP, and media capture.
  - [ ] The `ARCH-CHIPGLUE-001` audit document inventories every remaining `src/ViceSharp.Chips` type and maps each acceptance criterion to direct evidence.
  - [ ] The x64sc lockstep/checkpoint gate passes after the remediation with `0` failed and `0` skipped tests.
- TEST-BASELINE-CORRECTNESS-001: Retain existing BasicBus tests with correct latched-bus expectations and a passing native-collection convention test.
Add minimal branch-sequence red regressions for both machine families and compare cycle/register/PC results to pinned VICE.
Run unchanged 5000-cycle snapshot fixture alone and after audio/dual-VIA/reset/snapshot/dispose; require passing historical performance and both investigated prefetch tests.
Retain traceability from every retired renderer/quarantine assertion to equivalent cycle-aware acceptance tests. Account for manual diagnostic relocation explicitly in the completion manifest.

## Acceptance Criteria
- **ac-01**: Retain existing BasicBus tests with correct latched-bus expectations and a passing native-collection convention test.
- **ac-02**: Add minimal branch-sequence red regressions for both machine families and compare cycle/register/PC results to pinned VICE.
- **ac-03**: Run unchanged 5000-cycle snapshot fixture alone and after audio/dual-VIA/reset/snapshot/dispose; require passing historical performance and both investigated prefetch tests.
- **ac-04**: Retain traceability from every retired renderer/quarantine assertion to equivalent cycle-aware acceptance tests. Account for manual diagnostic relocation explicitly in the completion manifest.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Retain existing BasicBus tests with correct latched-bus expectations and a passing native-collection convention test.
  - [ ] Add minimal branch-sequence red regressions for both machine families and compare cycle/register/PC results to pinned VICE.
  - [ ] Run unchanged 5000-cycle snapshot fixture alone and after audio/dual-VIA/reset/snapshot/dispose; require passing historical performance and both investigated prefetch tests.
  - [ ] Retain traceability from every retired renderer/quarantine assertion to equivalent cycle-aware acceptance tests. Account for manual diagnostic relocation explicitly in the completion manifest.
- TEST-BUILD-001: The Nuke build tests in `build.tests/ViceSharp.Build.Tests.csproj` run through `BuildTest` on every host (PLAN-NUKETESTS-001). Traceability: TR-BUILD-001 ac-10, ac-11, ac-12; TR-PLAT-001 ac-8.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] The 13 user-local deployment cases pass on Linux through `BuildTest` and prove TR-BUILD-001 ac-10 and ac-11.
  - [ ] The completion-manifest cases pass on every host through `BuildTest`.
  - [ ] The placement guards of PLAN-NUKETESTS-001 step 2 pass.
  - [ ] The Windows `BuildTest` run selects no Linux-host case and reports the excluded count.
- TEST-CFG-001: ## TEST-CFG-001: Configuration and Resource Tests

**ID:** TEST-CFG-001
**Title:** Configuration and Resource Tests
**Priority:** P1 -- Important

### Condition

Resource files, ROM/romset selection, palettes, hotkeys, autostart, peripherals, RAM init, debug resources, and limiter settings are verified with configuration and host service tests.

Desktop C64 bootstrap coverage verifies complete local-set reuse, configured-root precedence over the per-user cache, acquisition of only missing default ROMs, canonical filenames, pinned-hash rejection, offline cache reuse, and actionable acquisition failure/cancellation. Startup coverage verifies a ROM-backed C64 with video after provisioning and no silent minimal-machine fallback. Generic/headless factories and remote-host connections must not trigger local downloads; UI/runtime boundary tests remain applicable.

### Traceability

- **Related FR Area(s):** FR-CFG
- **Related FR:** FR-CFG-002
- **Related TRs:** TR-LIB-001, TR-MVVM-001
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Resource files, ROM/romset selection, palettes, hotkeys, autostart, peripherals, RAM init, debug resources, and limiter settings are verified with configuration and host service tests.
  - [ ] Desktop C64 bootstrap coverage verifies complete local-set reuse, configured-root precedence over the per-user cache, acquisition of only missing default ROMs, canonical filenames, pinned-hash rejection, offline cache reuse, and actionable acquisition failure/cancellation. Startup coverage verifies a ROM-backed C64 with video after provisioning and no silent minimal-machine fallback. Generic/headless factories and remote-host connections must not trigger local downloads; UI/runtime boundary tests remain applicable.
- TEST-CHIPSTATE-001: Recorder tests with a fake stateful device verify per-tick chip-state capture and that Snapshot deep-copies (a later capture does not corrupt an earlier snapshot). VIC/SID/CIA/PLA CaptureState/DecodeState round-trip exercised end to end via GetChipStateAtTick.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] OnInstructionCompleted captures the device state into the tick
  - [ ] Snapshot deep-copies chip state across ring rotation
- TEST-CIA-001: CIA timers, TOD, keyboard matrix, joystick interaction, serial shift register, IRQ, and NMI behavior are verified with unit and integration tests.
  Scope: layer-1+
- TEST-CLI-LAUNCHER-001: VICE-style launcher parser, topology, debugcart polarity, bounded `-limitcycles`, PRG autostart dispatch, help text, and process smoke behavior are verified with parser, stub entrypoint, and real console process tests.
  Scope: layer-1+
- TEST-COMPLETION-001: Completion manifest coverage verification detects an omitted project, category, required workload or prerequisite instead of accepting a narrower run.
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
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Completion manifest coverage verification detects an omitted project, category, required workload or prerequisite instead of accepting a narrower run.
  - [ ] Final Release build and every required manifest gate completes with zero failed/skipped/aborted tests and retained evidence.
  - [ ] Both fresh live passes cover Apply/Revert/Restart, validation, speed, individual RAM blocks, persistence preferences, builder open/close, effective visibility and independent host/runtime state.
  - [ ] Audit every changed AC against named implementation and executable evidence; the informational traceability script exit code alone cannot prove completion.
  - [ ] Require final Astra agreement over combined diff, requirement evidence, owned-file manifest and receipts before commit/push and local/origin SHA proof.
  - [ ] Both fresh live passes and WarpModeTests satisfy existing TR-WARP-STATUS-001: LimiterRatePercent 0 and EffectiveClockPercent greater than 150, with actual runtime pacing evidence.
- TEST-CPU-001: CPU instruction, timing, interrupt, and port behavior is verified with unit tests and VICE/Lorenz-style reference comparisons.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] These added criteria cover only FR-CPU-004/ac-03, ac-05 and ac-06. Other obligations under this TEST condition, including FR-CPU-001, FR-CPU-002, FR-CPU-003 and FR-CPU-005, are not covered by these criteria and remain unsatisfied until their own required evidence is accepted. Validate assertions first against independent correct mocks and deliberately corrupt mock responses rejected inside a green harness; then bind the same inputs, expected results and assertions to real managed and immutable native providers. Unknown native observations block real acceptance. Retain input, seed/history, source/binary/ROM/resource hashes and full receipts. For FR-CPU-004/ac-03, C64_PortMotorAndWrite_DdrTruthTable covers all DATA/DDR combinations for bits 5 and 3, changes caused by DATA and DDR, input/output transitions and repeated identical writes. Assert motor = !(DDR & DATA & 0x20), write = (~DDR | DATA) & 0x08, exact changed-effective-output calls and no call when the native effective output is unchanged. Reject swapped bits, wrong polarity, forwarding on every write, and missed DDR-only changes. Preserve unrelated bits and pin prior native output-history state.
  - [ ] These added criteria cover only FR-CPU-004/ac-03, ac-05 and ac-06. Other obligations under this TEST condition, including FR-CPU-001, FR-CPU-002, FR-CPU-003 and FR-CPU-005, are not covered by these criteria and remain unsatisfied until their own required evidence is accepted. Validate assertions first against independent correct mocks and deliberately corrupt mock responses rejected inside a green harness; then bind the same inputs, expected results and assertions to real managed and immutable native providers. Unknown native observations block real acceptance. Retain input, seed/history, source/binary/ROM/resource hashes and full receipts. For FR-CPU-004/ac-05, C64_PortBit5_ReadDirectionMatrix enumerates DATA bit5 low/high crossed with DDR input/output, both transitions and repeated reads. Assert input reads clear bit5 and output reads match pinned c64pla port calculation, preserving all unrelated read bits. Reject input pull-up, output forced-zero, SX capacitor substitution on C64, stale direction and unintended read-side output changes.
  - [ ] These added criteria cover only FR-CPU-004/ac-03, ac-05 and ac-06. Other obligations under this TEST condition, including FR-CPU-001, FR-CPU-002, FR-CPU-003 and FR-CPU-005, are not covered by these criteria and remain unsatisfied until their own required evidence is accepted. Validate assertions first against independent correct mocks and deliberately corrupt mock responses rejected inside a green harness; then bind the same inputs, expected results and assertions to real managed and immutable native providers. Unknown native observations block real acceptance. Retain input, seed/history, source/binary/ROM/resource hashes and full receipts. For FR-CPU-004/ac-06, C64_Capacitor_DeadlineInclusiveJitter: for clock 10, bits 6/7 use deadline 350010 with draw 0 and 420010 with draw 70000. SX bits 3/4/5 use 1500010 with draw 0 and 1800010 with draw 300000. Include interior draws and each bit separately. Reject microsecond conversion, an exclusive upper bound, wrong base, or one shared deadline for unequal draws. Endpoint fixtures use independent mocked draws; native cases must record an actual seeded/prelude-derived draw, not assert that a selected seed reaches an endpoint without evidence.
  - [ ] These added criteria cover only FR-CPU-004/ac-03, ac-05 and ac-06. Other obligations under this TEST condition, including FR-CPU-001, FR-CPU-002, FR-CPU-003 and FR-CPU-005, are not covered by these criteria and remain unsatisfied until their own required evidence is accepted. Validate assertions first against independent correct mocks and deliberately corrupt mock responses rejected inside a green harness; then bind the same inputs, expected results and assertions to real managed and immutable native providers. Unknown native observations block real acceptance. Retain input, seed/history, source/binary/ROM/resource hashes and full receipts. For FR-CPU-004/ac-06, C64_Capacitor_DdrTransitionMatrix: cover 0->0, 0->1, 1->0 and 1->1 for each eligible pin and data 0/1. Only 1->0 captures old data and consumes a random draw. Preserve an already-held charge/deadline through the other transitions, subject to a subsequent DATA read's expiration. Reject capture from data_read instead of data, reseeding on every DDR write, or forgetting zero-valued captures.
  - [ ] These added criteria cover only FR-CPU-004/ac-03, ac-05 and ac-06. Other obligations under this TEST condition, including FR-CPU-001, FR-CPU-002, FR-CPU-003 and FR-CPU-005, are not covered by these criteria and remain unsatisfied until their own required evidence is accepted. Validate assertions first against independent correct mocks and deliberately corrupt mock responses rejected inside a green harness; then bind the same inputs, expected results and assertions to real managed and immutable native providers. Unknown native observations block real acceptance. Retain input, seed/history, source/binary/ROM/resource hashes and full receipts. For FR-CPU-004/ac-06, C64_Capacitor_RepeatedDataWrite: write high twice while output at distinct clocks, write low, then write the same low again. Each write consumes its eligible draws and moves deadlines even without a data-value change. Repeat while input: no capacitor update or draw. Reject same-value suppression, zero-value suppression, or charging an input pin.
  - [ ] These added criteria cover only FR-CPU-004/ac-03, ac-05 and ac-06. Other obligations under this TEST condition, including FR-CPU-001, FR-CPU-002, FR-CPU-003 and FR-CPU-005, are not covered by these criteria and remain unsatisfied until their own required evidence is accepted. Validate assertions first against independent correct mocks and deliberately corrupt mock responses rejected inside a green harness; then bind the same inputs, expected results and assertions to real managed and immutable native providers. Unknown native observations block real acceptance. Retain input, seed/history, source/binary/ROM/resource hashes and full receipts. For FR-CPU-004/ac-06, C64_Capacitor_ReadBoundaryAndLazyExpiry: independent fixtures at D-1, D and D+1 show retained, retained and cleared charge. Also perform repeated reads; DDR-only reads after D; no-read gaps followed by a DATA read; and DATA reads while output. Output reads still execute expiry but do not overlay charge. Reject `<=`, host-time expiry, DDR-read discharge, output overlay, or a synthetic read inserted into the prelude.
  - [ ] These added criteria cover only FR-CPU-004/ac-03, ac-05 and ac-06. Other obligations under this TEST condition, including FR-CPU-001, FR-CPU-002, FR-CPU-003 and FR-CPU-005, are not covered by these criteria and remain unsatisfied until their own required evidence is accepted. Validate assertions first against independent correct mocks and deliberately corrupt mock responses rejected inside a green harness; then bind the same inputs, expected results and assertions to real managed and immutable native providers. Unknown native observations block real acceptance. Retain input, seed/history, source/binary/ROM/resource hashes and full receipts. For FR-CPU-004/ac-06, C64_Capacitor_BoardPinMatrix: normal C64 bits 6/7 and SX-64 bits 3..7 use the appropriate paths. Ordinary C64 bits 3/4/5 keep their native tape/pull-up behavior, not SX capacitor substitution. Include mixed eligible/ineligible DDR masks and preserved unrelated return bits. Reject applying SX behavior to C64 or treating all SX inputs as zero.
  - [ ] These added criteria cover only FR-CPU-004/ac-03, ac-05 and ac-06. Other obligations under this TEST condition, including FR-CPU-001, FR-CPU-002, FR-CPU-003 and FR-CPU-005, are not covered by these criteria and remain unsatisfied until their own required evidence is accepted. Validate assertions first against independent correct mocks and deliberately corrupt mock responses rejected inside a green harness; then bind the same inputs, expected results and assertions to real managed and immutable native providers. Unknown native observations block real acceptance. Retain input, seed/history, source/binary/ROM/resource hashes and full receipts. For FR-CPU-004/ac-06, C64_Capacitor_RngConsumptionOrder: with scripted distinct draws 1,2,3,4,5 and all eligible pins, SX DDR transitions consume in order 3,4,5,6,7; SX DATA writes consume 7,6,5,4,3. C64 DDR uses 6,7 and DATA uses 7,6. Test sparse masks and earlier global draws. Reject sorting both paths alike, drawing for an ineligible pin, restarting the RNG per pin, or sharing one draw across pins.
  - [ ] These added criteria cover only FR-CPU-004/ac-03, ac-05 and ac-06. Other obligations under this TEST condition, including FR-CPU-001, FR-CPU-002, FR-CPU-003 and FR-CPU-005, are not covered by these criteria and remain unsatisfied until their own required evidence is accepted. Validate assertions first against independent correct mocks and deliberately corrupt mock responses rejected inside a green harness; then bind the same inputs, expected results and assertions to real managed and immutable native providers. Unknown native observations block real acceptance. Retain input, seed/history, source/binary/ROM/resource hashes and full receipts. For FR-CPU-004/ac-06, C64_Capacitor_ResetAndClockWrap: reset after charge must clear charges/flags and leave the observed deadline fields as native does. A later input read must not resurrect charge. With clock `UINT64_MAX-10`, base350000 and draw0, deadline wraps to349989; an immediate DATA read at that high clock sees deadline<clock and expires. Reject saturating/signed arithmetic, invented wrap-aware expiration, or preserving active flags on reset. Full native reset/prelude effects remain part of the fixture, not inferred from this function alone.
  - [ ] These added criteria cover only FR-CPU-004/ac-03, ac-05 and ac-06. Other obligations under this TEST condition, including FR-CPU-001, FR-CPU-002, FR-CPU-003 and FR-CPU-005, are not covered by these criteria and remain unsatisfied until their own required evidence is accepted. Validate assertions first against independent correct mocks and deliberately corrupt mock responses rejected inside a green harness; then bind the same inputs, expected results and assertions to real managed and immutable native providers. Unknown native observations block real acceptance. Retain input, seed/history, source/binary/ROM/resource hashes and full receipts. For FR-CPU-004/ac-06, C64_Capacitor_PalNtscAndConfiguredModel: repeat board/pin cases for accepted PAL/NTSC and configured C64/SX model variants. Constants remain in CPU-cycle units; wall-clock duration can differ with clock rate. In these pinned paths a configured 8500 model does not select the unused 8500 constant. Reject host-frequency scaling of deadlines, a hardware-intent replacement, or omission of a required board/model/resource combination. Pin the actually supported model/resource values before building executable fixtures.
- TEST-CPUTICK-001: Unit tests covering ExecutedCycles increments per executed (non-stolen) cycle, per-CPU rate = executed/elapsed/target, reset zeroes counters, and the status surface lists host and peripheral CPU rates distinctly.
  Scope: layer-1+
- TEST-CRT-001: Standard, Ocean, EasyFlash, Action Replay, Retro Replay, and Final Cartridge mapping behavior is verified with cartridge image and banking tests.
  Scope: layer-1+
- TEST-CTX-001: Full transition table + menu neutralization + neutral push + UI-nav mapping. Acceptance: every (state,input) edge deterministic; A does not fire in menu; one-shot neutral push emitted.
  Scope: layer-1+
- TEST-DEPS-202607-001: Each upgrade slice S1-S8 exits on: dotnet restore + Release build with TreatWarningsAsErrors + slice-focused test filter + the full filtered TestHarness suite at 100 percent green (0 failed, 21 skipped baseline, >= 2594 passed; filter Category!=Determinism&Category!=AiReview&Category!=ParityPending&Category!=ParityLegacy). S3 adds an interactive Avalonia smoke; S6 proves nuget.org-only restore for aiUnit/RemoteControl; S7 proves PackNuget locally; S8 proves the source generator loads under the raised SDK floor (Roslyn 5.6).
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Every upgrade slice exits with the full filtered suite at 0 failed and stable skips, plus its slice-specific proof. (evidence: Local gates S1-S8 all 0 failed / 21 skipped; CI runs 1086, 1087, 1088, 1089, 1091, 1092, 1093, 1094, 1095 all succeeded.)
- TEST-DRV-001: Drive CPU/timing, image formats, GCR, IEC bus protocol, fast loader, and host media attachment behavior are verified with smoke, protocol, and reference tests.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Tests prove mounted D64 directory and file operations execute through IEC bus line activity.
  - [ ] Tests prove the same D64 operations complete with correct directory and file data over the IEC path.
  - [ ] Focused IEC/D64 tests run with zero failed and zero skipped tests.
- TEST-DRV1540-LOCKSTEP-001: Boot witness: drive {PC,A,X,Y,P,SP} equals native x64sc (Drive8Type=1540, TDE=1) at equal drive clocks sampled every 19,656 host cycles over 2M host cycles, plus host CPU state. Media gate: KERNAL-CLOSE anchored LOAD convergence from frostpoint.d64 over 30 emulated seconds.
  Scope: layer-1+
- TEST-DRV1541II-LOCKSTEP-001: Boot witness + anchored D64 LOAD gate vs native x64sc with Drive8Type=1542, same procedure as TEST-DRV1540-LOCKSTEP-001.
  Scope: layer-1+
- TEST-DRV1571-LOCKSTEP-001: Boot witness (no disk; WD1770 idle-state parity implied) + anchored LOAD gates from BOTH frostpoint.d64 and a committed canonical frostpoint.d71 (349,696 bytes, size-pinned) vs native x64sc with Drive8Type=1571.
  Scope: layer-1+
- TEST-DRV1581-LOCKSTEP-001: Boot witness + anchored LOAD gate from a committed canonical frostpoint.d81 (819,200 bytes, size-pinned) vs native x64sc with Drive8Type=1581; the LOAD exercises the WD1770 type-2 read path end-to-end.
  Scope: layer-1+
- TEST-DRVLED-001: Drive DOS ROM setting VIA2 PB3 sets the drive LedOn; per-drive status DTO reflects it; the card LED view-model binds it.
  Scope: layer-1+
- TEST-DRVLIFE-001: Attach creates+clocks+bus-connects the drive instance; detach unregisters from clock and disconnects from bus.
  Scope: layer-1+
- TEST-DRV-MOTOR-001: After SetMotor(true) and 300,000 Tick() calls, MotorRotationCycles>0. Before 300,000, stays at 0. Motor off/on resets ramp. ReadSector(18,0) returns BAM bytes 0x12/0x01. 5 pass.
  Scope: layer-1+
- TEST-DRVTRUE-001: VM: TrueDrive toggles per drive (default off). Runtime: with TrueDrive on, the session uses the coordinator true-drive 1541 path (LOAD works); with it off, the simulated drive is used and existing tests stay green.
  Scope: layer-1+
- TEST-DRV-TYPE-001: DriveTypeShimTests (Collection NativeVice): vice_drive_set_type sets Drive8Type and vice_drive_get_type reads it back; out-of-range units rejected; after set+reset the native drive PC lands in $8000-$FFFF for 32KB-ROM models (proves memiec re-init); vice_drivecpu_get_clk is monotonic under stepping; vice_drivecpu_execute_all callable; Drive8Type re-baselines to 1541 on the next machine create (no cross-test residue).
  Scope: layer-1+
- TEST-GAMEPAD-001: Off-console converter/merge/SOCD/fire/profile/pump behavior. Acceptance: 0 failed/0 skipped.
  Scope: layer-1+
- TEST-GAMEPAD-002: On-console live-controller smoke. Acceptance: live pad drives JOY2/JOY1 with fire per mapping.
  Scope: layer-1+
- TEST-GRPC-001: Protocol, status, control, input, monitor, media, snapshot, capture, and boundary enforcement paths are verified through generated clients and host integration tests.
  Scope: layer-1+
- TEST-HOST-001: Host lifecycle, status, media, state, capture, diagnostics, and session ownership behavior are verified with in-process host service tests.
  Scope: layer-1+
- TEST-HOST-DIAG-001: Diagnostics protocol descriptors, session enumeration, host info, current UI session resolution, performance snapshots, streaming snapshots, attach file lifecycle, and development-scoped reflection are verified with red-first unit and in-process gRPC tests.
  Scope: layer-1+
- TEST-IECDECODE-001: Canonical captured sequences (LOAD command frame, directory read) decode to the expected IEC event list.
  Scope: layer-1+
- TEST-IECELEC-001: With the live electrical model + true-drive, normal C64 idle CIA2 PA reads $47 and C64GS reads $07; native CIA2-IO lockstep stays green.
  Scope: layer-1+
- TEST-IECHOTPLUG-001: Tests that a drive attached mid-run answers on the bus, a detached drive removes its pulls and recomputes line states, and a renumbered drive answers its new device number.
  Scope: layer-1+
- TEST-IECLOAD-001: LOAD"*",8,1 and LOAD"$",8 complete over IEC against a real D64 on a single-system C64.
  Scope: layer-1+
- TEST-IECMON-001: Scope view-model binds trace + decoded events; host trace-delta contract returns samples since cycle N; step/rewind move the cursor.
  Scope: layer-1+
- TEST-IECSPY-001: Snapshot reports idle all-high/no-talkers; single puller -> line low + talker; multi-puller wired-AND lists all; release restores high; snapshot never mutates bus. DONE (5/5 green).
  Scope: layer-1+
- TEST-IEC-TIMING-001: After setting IecBus.Atn=false and calling Tick() up to 985 times, Clock==false and Data==false. After ATN release and Tick() up to 985 times, both return true. 5 pass.
  Scope: layer-1+
- TEST-IECTRACE-001: Edge capture order + cycle stamps; step-boundary marks; ring bound; rewind re-derivation equals original trace.
  Scope: layer-1+
- TEST-INPROC-001: Assembly-reference (no ASP.NET in the portable lib) + moved-type re-run. Acceptance: EmulationPumpService.Assembly == Host.InProcess with no Microsoft.AspNetCore.* referenced.
  Scope: layer-1+
- TEST-INPROC-002: Facade + determinism gate (bit-exact TotalCycles across two runs) + explicit-port + geometry + single-worker. Acceptance: bit-exact repro; BufferLength==FrameBuffer.Length; AppliedWorkerAffinityMask==null.
  Scope: layer-1+
- TEST-INPROC-003: Gate zero-P/Invoke + inline-sleep-seam + no-thread-growth + Pause-unparks. Acceptance: no [DllImport]/[LibraryImport]; no new timer thread; Pause unparks.
  Scope: layer-1+
- TEST-INPROC-DEVICE: On-console boot throughput (>=100 frames/2s, unpinned, two emulation threads). Acceptance: sustained on-device throughput.
  Scope: layer-1+
- TEST-INPUT-001: ## TEST-INPUT-001: Input and VKM Tests

**ID:** TEST-INPUT-001
**Title:** Input and VKM Tests
**Priority:** P1 -- Important

### Condition

Keyboard, joystick, mouse, lightpen, paddle, and VICE VKM behavior is verified with parser, matrix, protocol, and machine integration tests.

Desktop packaging verification checks every vendored C64 `.vkm` file in the installable archive against its source content. `BundledKeyboardMapTests` verifies host discovery of the packaged alternate layouts without an external VICE installation and preserves configured data-root priority (FR-INP-006, TR-INPUT-VKM-001).

### Traceability

- **Related FR Area(s):** FR-INP
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Keyboard, joystick, mouse, lightpen, paddle, and VICE VKM behavior is verified with parser, matrix, protocol, and machine integration tests.
  - [ ] Desktop packaging verification checks every vendored C64 `.vkm` file in the installable archive against its source content. `BundledKeyboardMapTests` verifies host discovery of the packaged alternate layouts without an external VICE installation and preserves configured data-root priority (FR-INP-006, TR-INPUT-VKM-001).
- TEST-INSTALL-001: InstallerFileAssociationTests: wxs registers each DesktopFileAssociations entry and open command; TryGetOpenPath skips exe/flags and unknown extensions.
  Scope: layer-1+
- TEST-MED-001: ## TEST-MED-001: Media Capture Tests

**ID:** TEST-MED-001
**Title:** Media Capture Tests
**Priority:** P1 -- Important

### Condition

Screenshot, video, audio, synchronized capture, and format selection behavior are verified through capture metadata and round-trip output tests.

### Traceability

- **Related FR Area(s):** FR-MED
  Scope: layer-1+
- TEST-MED-002: FrameSequenceCapture unique-dedup, drop-on-mismatch, and host frames=unique routing.
  Scope: layer-1+
- TEST-MED-003: WAV header/data correctness, after-stop ignore, concurrent-stop lock safety; CaptureAudioTap clamp/forward.
  Scope: layer-1+
- TEST-MED-004: ffprobe-verified video+audio streams; Start failure cleanup; BackgroundByteWriter ordering/fault.
  Scope: layer-1+
- TEST-MEM-001: Address decoding, RAM-under-ROM, Ultimax, VIC bank selection, color RAM, and stack/zero-page behavior are verified with unit and integration tests.
  Scope: layer-1+
- TEST-MON-001: Disassembly, memory display, breakpoints, register operations, bank selection, watch expressions, and monitor RPC behavior are verified through monitor engine tests.
  Scope: layer-1+
- TEST-NATIVE-LIFECYCLE-001: Add red regressions proving oracle reset does not zero live playback timing, then exact-length PCM equality for PAL and NTSC.
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
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Add red regressions proving oracle reset does not zero live playback timing, then exact-length PCM equality for PAL and NTSC.
  - [ ] Run AudioThenDualViaResetSnapshotDispose repeatedly in one process for PAL and NTSC, including independent and post-sequence snapshot tests.
  - [ ] Run create/step timeout, stop timeout, poison-after-failure and repeat-dispose fault cases in owned child processes; assert bounded wall time and immediate later failure.
  - [ ] Require native prerequisites, selected workload flags and actual cycle budgets; missing DLLs/ROMs or disabled required workloads fail instead of returning early. Restore environment variables.
  - [ ] Build the supported patch from the pinned clean native source and verify the hash of the DLL loaded by the passing native tests.
- TEST-NATIVERESIDUE-001: NativeResidueDiagTests: probe 01 diffs a fresh machine's 200-cycle boot state before/after a demo .vsf resume on a scratch machine; probe 02 replicates the post-activity-reset divergence (boot + full PAL frame + reset + per-cycle walk 140-260 sampling DD00/DD01, CIA1+CIA2, VIC beam, CPU). Guards the PLAN-NATIVERESIDUE-001 mechanism fix: vice_machine_create_model re-baselines Drive{8..11}TrueEmulation to the VICE default 1 via resources_set_int so a has_tde=0 DRIVE8 snapshot module cannot disable True Drive Emulation process-wide.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Fresh-machine boot state is bit-identical before and after a .vsf snapshot resume in the same process. (evidence: TEST-NATIVE-RESIDUE-01 green post-fix; poison combo 36 passed / 0 failed / 1 skipped in one process.)
  - [ ] Post-activity-reset per-cycle trace (DD00/CIA/VIC/CPU, cycles 140-260) is bit-identical before and after a .vsf resume. (evidence: TEST-NATIVE-RESIDUE-02 green post-fix; full suite one process 0 failed / 2594 passed.)
- TEST-NATIVERESIDUE-002: A machine created after a scratch machine attaches/detaches a disk (or resumes a has_tde=0 .vsf) must present drive-8 attach_clk/detach_clk/attach_detach_clk == 0. Guards drive-snapshot.c uninitialized-stack + shim create-time re-baseline. Code: NativeResidueDiagTests.FreshMachine_DriveAttachDetachClocks_AreZero_AfterResidue (label TEST-NATIVE-RESIDUE-03).
  Scope: layer-1+
- TEST-NATIVERESIDUE-003: A machine created after a scratch machine resumes a true-drive .vsf and runs activity must present drive-8 cycle_accum == 0. Guards drivecpu_reset_clk cycle_accum omission. Code: NativeResidueDiagTests.FreshMachine_DriveCycleAccum_IsZero_AfterSnapshotResumeActivity (label TEST-NATIVE-RESIDUE-04).
  Scope: layer-1+
- TEST-PACESEL-001: PacingStrategySelectionTests (factory + live SetStrategy), SettingsServiceHostTests (UpdateSettings applies PacingStrategy to the pump and round-trips it), and AttachPanelViewModelTests (pacing change flags pending, not restart).
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] SetStrategy on a not-started pump switches GateName immediately; unknown defaults to Semaphore
  - [ ] UpdateSettings with a VICE pacing strategy sets the pump gate to VICE and round-trips vice
- TEST-PERF-RUNFRAME-001: The benchmark harness builds a real-ROM C64 PAL machine through `ArchitectureBuilder`, measures `IMachine.RunFrame()` after warmup over the required 600-frame window, reports median and p95 frame time, and proves the measured hot path allocates zero bytes on the current thread.

## Acceptance Criteria
- `C64PalRunFrameBenchmark` builds Commodore 64 PAL through `ArchitectureBuilder` with the real ROM provider.
- `RunFramePerfProbe` reports median `<= 18 ms`, p95 `<= 22 ms`, and `0` allocated bytes for the 60 warmup / 600 measured frame workflow.
- Focused BasicBus/C64MemoryMap/VideoRenderer/VideoSurface/SID tests pass with `0` failed and `0` skipped.
- Lockstep and checkpoint gates pass with `0` failed and `0` skipped.
- BenchmarkDotNet completes `C64PalRunFrameBenchmark` and reports no managed allocation.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Benchmark/probe builds Commodore 64 PAL through ArchitectureBuilder with the real ROM provider. (evidence: BenchmarksSmokeTests.C64PalRunFrameBenchmark_UsesRealC64Pal passed)
  - [ ] RunFramePerfProbe 60/600 reports median <= 18 ms, p95 <= 22 ms, and 0 allocated bytes. (evidence: median=1.575ms; p95=2.753ms; allocated=0 bytes)
  - [ ] Focused BasicBus/C64MemoryMap/VideoRenderer/VideoSurface/SID and Lockstep/Checkpoint gates pass with 0 failed and 0 skipped tests. (evidence: focused=182 passed; lockstep/checkpoint=333 passed)
  - [ ] BenchmarkDotNet C64PalRunFrameBenchmark completes and reports no managed allocation. (evidence: BenchmarkDotNet mean=2.262ms median=2.255ms; Allocated reported none)
- TEST-PRF-001: C64, C64C, SX-64, C128, VIC-20, PET, Plus/4, and C16 profiles are verified for required devices, ROMs, clocks, and address maps.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] These added criteria cover only FR-PRF-003/ac-02 and ac-04. Other obligations under this TEST condition, including FR-PRF-001, FR-PRF-002 and FR-PRF-004 through FR-PRF-008, are not covered by these criteria and remain unsatisfied until their own required evidence is accepted. Validate assertions first against independent correct mocks and deliberately corrupt mock responses rejected inside a green harness; then bind the same inputs, expected results and assertions to real managed and immutable native providers. Unknown native observations block real acceptance. Retain input, seed/history, source/binary/ROM/resource hashes and full receipts. For FR-PRF-003/ac-02, Sx64_PortOutputs_AreDisconnected crosses DATA/DDR bits 3,4,5 and direction transitions for SX-64 and C64 control boards. Assert SX-64 never forwards motor, write or sense-out to a cassette device, including repeated and changed effective outputs; its CPU reads still obey DATA, DDR and independently established retained-charge state. Reject C64 forwarding on SX-64, globally disconnecting C64, or hardcoding all SX input bits high or low.
  - [ ] These added criteria cover only FR-PRF-003/ac-02 and ac-04. Other obligations under this TEST condition, including FR-PRF-001, FR-PRF-002 and FR-PRF-004 through FR-PRF-008, are not covered by these criteria and remain unsatisfied until their own required evidence is accepted. Validate assertions first against independent correct mocks and deliberately corrupt mock responses rejected inside a green harness; then bind the same inputs, expected results and assertions to real managed and immutable native providers. Unknown native observations block real acceptance. Retain input, seed/history, source/binary/ROM/resource hashes and full receipts. For FR-PRF-003/ac-04, Sx64_InputCapacitors_ResetAndIndependentBits exercises each bit 3,4,5 separately and mixed masks with different captured 0/1 values. Assert output reads use normal port data, input reads use the corresponding retained data_set_bit, expiry clears only eligible expired charge/flags, and reset clears charge and flags without inventing deadline clearing. Reject shared state between pins, output charge overlay, resurrection after reset and dropped unrelated read bits.
  - [ ] These added criteria cover only FR-PRF-003/ac-02 and ac-04. Other obligations under this TEST condition, including FR-PRF-001, FR-PRF-002 and FR-PRF-004 through FR-PRF-008, are not covered by these criteria and remain unsatisfied until their own required evidence is accepted. Validate assertions first against independent correct mocks and deliberately corrupt mock responses rejected inside a green harness; then bind the same inputs, expected results and assertions to real managed and immutable native providers. Unknown native observations block real acceptance. Retain input, seed/history, source/binary/ROM/resource hashes and full receipts. For FR-PRF-003/ac-04, Sx64_InputCapacitors_StrictFalloffBoundary tests each bit with an independently chosen deadline D at clocks D-1, D, D+1; only deadline < currentClock expires on $0001 read. Cover repeated reads, no-read gaps, output-mode reads and $0000-only reads. Assert DATA-read expiry side effects even for output pins but no charge overlay there; DDR reads do not expire charge. Reject <=, eager/host-clock expiry, input-only expiry and DDR-read discharge.
  - [ ] These added criteria cover only FR-PRF-003/ac-02 and ac-04. Other obligations under this TEST condition, including FR-PRF-001, FR-PRF-002 and FR-PRF-004 through FR-PRF-008, are not covered by these criteria and remain unsatisfied until their own required evidence is accepted. Validate assertions first against independent correct mocks and deliberately corrupt mock responses rejected inside a green harness; then bind the same inputs, expected results and assertions to real managed and immutable native providers. Unknown native observations block real acceptance. Retain input, seed/history, source/binary/ROM/resource hashes and full receipts. For FR-PRF-003/ac-04, Sx64_InputCapacitors_DeadlineSelection uses clock10 with independently scripted inclusive jitter endpoints 0 and300000 to produce deadlines1500010 and1800010 for bits3/4/5. DATA writes while output, including same-value and zero writes, and DDR output-to-input transitions capture data and consume independent draws. Other DDR transitions or DATA writes while input do not. Cross-check C64_Capacitor_RngConsumptionOrder and ResetAndClockWrap assertions for SX bit order, sparse masks, uint64 wrap and prior global RNG history; reject one shared draw, exclusive endpoints, wrong1500000-cycle base, omitted repeated writes and invented native endpoint seeds.
- TEST-PUBSUB-001: Focused unit, smoke, and benchmark-probe coverage shall verify the Pub/Sub event bus contract, including typed/raw/packed delivery, unsubscription, deterministic delivery order, collision isolation, route growth, message pool exhaustion/reset, frame arena payloads, zero publish allocation, and release probe performance.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Focused Pub/Sub test suite and benchmark smoke tests pass. (evidence: dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj --no-restore --filter FullyQualifiedName~LockFreePubSubTests|FullyQualifiedName~BenchmarksSmokeTests.PubSub => Passed: 22, Failed: 0)
  - [ ] Release quick probe proves the Pub/Sub performance targets without managed allocation. (evidence: dotnet run -c Release --project tests/ViceSharp.Benchmarks -- --pubsub-probe 1000000 => publish-one=43.78ns publish-three=58.14ns publish-packed=35.62ns pool-rent-return=16.80ns arena-alloc=3.20ns allocated=0 bytes)
  - [ ] The full solution builds after the Pub/Sub implementation. (evidence: dotnet build ViceSharp.slnx --no-restore => Build succeeded)
- TEST-QA-TESTSILENCE-001: WindowsAudioSessionMuteTests (tests/ViceSharp.TestHarness/Audio) verifies: when a render endpoint exists the fixture engages and the process Windows audio session reads muted (endpointExists equals IsEngaged, then muted is true); disposing an engaged fixture restores the prior mute state; and construction plus double-dispose never throw on any host. Validates TR-QA-TESTSILENCE-001.
  Scope: layer-1+
- TEST-REMOTECTRL-001: Tests prove the RemoteControl integration is off by default (no host started when VICESHARP_REMOTECONTROL_ENABLE is unset) and fails closed when enabled without a token; and that, when enabled with a token, the configured IRemoteControlRootProvider returns the live MainWindow. App-launch gate: connect the RemoteControl client tool and confirm the visual tree is readable.

## Acceptance Criteria
- **ac-01**: Test unset enable switch and enabled-without-token startup; both expose no usable control host.
- **ac-02**: Launch the owned app with explicit valid enable/token settings; read capabilities and the live MainWindow tree over gRPC.
- **ac-03**: Verify missing/wrong bearer token rejection, loopback policy and separate denied/enabled action/frame permissions.
- **ac-04**: Retain the two fresh final-binary live passes and real gRPC CLI contract evidence required by TEST-REMOTECTRL-CLI-001 and TEST-COMPLETION-001.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Test unset enable switch and enabled-without-token startup; both expose no usable control host.
  - [ ] Launch the owned app with explicit valid enable/token settings; read capabilities and the live MainWindow tree over gRPC.
  - [ ] Verify missing/wrong bearer token rejection, loopback policy and separate denied/enabled action/frame permissions.
  - [ ] Retain the two fresh final-binary live passes and real gRPC CLI contract evidence required by TEST-REMOTECTRL-CLI-001 and TEST-COMPLETION-001.
- TEST-REMOTECTRL-CLI-001: Restore/build/test an isolated checkout with no sibling paths and verify published package resolution.
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
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Restore/build/test an isolated checkout with no sibling paths and verify published package resolution.
  - [ ] Use a real in-process gRPC server to verify bearer authentication, default-disabled and action permissions.
  - [ ] Exercise exact-ID precedence, ambiguous matches, ancestor-hidden nodes and malformed trees.
  - [ ] Exercise failed mutations, cancellation/deadlines, unsupported transport, malformed frame/PNG handling and CLI exit codes.
  - [ ] Verify capability/tree JSON and successful enabled control actions against generated gRPC contracts.
- TEST-REMOTE-INTEGRATION-001: Run all seven integration tests with zero failures and zero skips, including server-side collection create and cleanup.
Verify optional bridge authentication is sent consistently by the caller-owned client for connection and search.
Verify authenticated discovery/download/launch of the required C64 seed against the existing remote endpoints. Missing access, disabled provisioning or absent seed blocks completion.

## Acceptance Criteria
- **ac-01**: Run all seven integration tests with zero failures and zero skips, including server-side collection create and cleanup.
- **ac-02**: Verify optional bridge authentication is sent consistently by the caller-owned client for connection and search.
- **ac-03**: Verify authenticated discovery/download/launch of the required C64 seed against the existing remote endpoints. Missing access, disabled provisioning or absent seed blocks completion.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Run all seven integration tests with zero failures and zero skips, including server-side collection create and cleanup.
  - [ ] Verify optional bridge authentication is sent consistently by the caller-owned client for connection and search.
  - [ ] Verify authenticated discovery/download/launch of the required C64 seed against the existing remote endpoints. Missing access, disabled provisioning or absent seed blocks completion.
- TEST-REVEXEC-001: RewindCycle/RewindFrame restore exact prior state; step-then-rewind round-trips; ring eviction bound honored.
  Scope: layer-1+
- TEST-ROMM-AVUI-001: Build, headless view-model, and end-to-end validation prove LibraryView and ListsView binding, connection, browse, search, list, collection, and launch behavior.
  Scope: layer-1+
- TEST-ROMM-DETAIL-001: RomMGateway detail mapping and RomDetailViewModel add-to-collection behavior are proven by deterministic automated tests for files, cover, summary, launchability, and collection mutation.
  Scope: layer-1+
- TEST-ROMM-LAUNCH-001: ## TEST-ROMM-LAUNCH-001: Supported media and launch recovery

**ID:** TEST-ROMM-LAUNCH-001
**Title:** Supported media and launch recovery
**Priority:** P1 -- Important

### Condition

Verify download completion and cache reuse, slot selection, supported D64/T64 attach and autostart, UI-visible failure and retry, and each desktop launch boundary. Unsupported G64/D71/D81 selections, including uppercase names, must disable attachment and issue neither a download request nor a launcher call. Use synthetic media and in-memory HTTP fixtures; never commit user game files.

### Traceability

- Functional requirement: FR-ROMM-LAUNCH-001.
- Technical requirements: TR-LIB-001, TR-ROMM-SEC-001.
- Regressions: `MediaExtensionMapTests.UnsupportedDiskFormat_NotLaunchable`, `LibraryBrowseViewModelTests.UnsupportedDisk_AttachDisabled`, plus the existing D64/T64 and launcher suites.
  Scope: layer-1+
- TEST-ROMM-PKG-001: Package metadata and pack-output tests prove both RomM libraries are packable with the approved package ids, GPL-2.0-or-later metadata, and exact dependency graph.
  Scope: layer-1+
- TEST-ROMM-SEC-001: ## TEST-ROMM-SEC-001: RomM credential, origin, and download safety

**ID:** TEST-ROMM-SEC-001
**Title:** RomM trusted-boundary regression suite
**Priority:** P0 -- Critical

### Condition

`RomMCoverImageSourceTests` verifies anonymous public covers and rejection of absolute authenticated-path inputs. `RomMGatewayDownloadTests` verifies filename containment and that truncated content never publishes a partial cache entry. `FileRomMConnectionStoreTests` verifies current-user DPAPI round-trip and immediate migration of legacy plaintext tokens. `LinuxSecretServiceRomMConnectionStoreTests` uses an injected command runner to verify Linux keyring round-trip, secret-free metadata and arguments, unavailable/locked keyring failures, cancellation, legacy migration, atomic replacement rollback, and successful replacement despite old-item cleanup failure. Deployment validation additionally performs a real Secret Service round-trip without logging credential values.

### Traceability

- **Related FRs:** FR-ROMM-CONN-001, FR-ROMM-COVER-001, FR-ROMM-LAUNCH-001
- **Related TR:** TR-ROMM-SEC-001
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] `RomMCoverImageSourceTests` verifies anonymous public covers and rejection of absolute authenticated-path inputs. `RomMGatewayDownloadTests` verifies filename containment and that truncated content never publishes a partial cache entry. `FileRomMConnectionStoreTests` verifies current-user DPAPI round-trip and immediate migration of legacy plaintext tokens. `LinuxSecretServiceRomMConnectionStoreTests` uses an injected command runner to verify Linux keyring round-trip, secret-free metadata and arguments, unavailable/locked keyring failures, cancellation, legacy migration, atomic replacement rollback, and successful replacement despite old-item cleanup failure. Deployment validation additionally performs a real Secret Service round-trip without logging credential values.
- TEST-SETTINGS-TXN-001: Add host regression cases for stage-restart Apply, retained restart after Revert, successful Apply+Restart and failed Apply/Restart with unchanged runtime/Accepted/Draft.
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
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Add host regression cases for stage-restart Apply, retained restart after Revert, successful Apply+Restart and failed Apply/Restart with unchanged runtime/Accepted/Draft.
  - [ ] Inject invalid family/profile, RAM, pacing/resource, image/preset and IEC root/unit/conflict inputs; assert no partial mutation or transient success notification.
  - [ ] Inject replacement preparation and attachment failures; assert rollback and candidate cleanup without writeback.
  - [ ] Verify Warp changes in the single UpdateSettings call, status rate 0, runtime uncapped behavior and preserved standalone Warp RPC compatibility.
  - [ ] Race refresh against Apply, deliver out-of-order responses, edit one or multiple fields, and verify stale rejection and three-way field merge.
  - [ ] Round-trip every RAM/cart/uIEC snapshot and INI field, legacy defaults and independent save-on-exit preferences.
  - [ ] Test old responses with absent State: readable display, explicit upgrade message and disabled state-aware Apply/Restart.
  - [ ] Verify actual cartridge type, detach, image/bank/preset/writeback and uIEC behavior after relevant family and profile restart.
- TEST-SID-001: ## TEST-SID-001: SID Audio Behavior Tests

**ID:** TEST-SID-001
**Title:** SID Audio Behavior Tests
**Priority:** P1 -- Important

### Condition

Oscillator, waveform, filter, ADSR, modulation, sync, noise, digi, external input, and multi-SID behavior are verified with deterministic audio/register tests.

For Linux host output, fake-device regressions verify platform selection, opt-out/headless silence, ordered PCM conversion, master volume/mute, bounded queues, pause/resume/stop/disposal, and missing or failed devices. Device loss must stop audio-driven pacing without changing SID synthesis. Unit tests do not open a real Linux audio device; installed-device smoke evidence is retained separately.

### Traceability

- **Related FR Area(s):** FR-SID
- **Linux output mappings:** FR-SID-013, TR-PLAT-001; `LinuxSdlAudioBackendTests` and `ViceGateSoundRegulatorTests`.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Oscillator, waveform, filter, ADSR, modulation, sync, noise, digi, external input, and multi-SID behavior are verified with deterministic audio/register tests.
  - [ ] For Linux host output, fake-device regressions verify platform selection, opt-out/headless silence, ordered PCM conversion, master volume/mute, bounded queues, pause/resume/stop/disposal, and missing or failed devices. Device loss must stop audio-driven pacing without changing SID synthesis. Unit tests do not open a real Linux audio device; installed-device smoke evidence is retained separately.
- TEST-SID-002: SID waveform centering, PCM equivalency, and Pieces of Light runtime capture validation prove that live audio back-pressure paces the emulator like VICE before and after runtime segment transitions.
  Scope: layer-1+
- TEST-SIDAUDIO-001: SidClockRateTests drives the SID via the SystemClock and asserts OSC3 = 0x10 after 8192 master cycles at voice freq 0x8000 (was 0x01). SidAudioPumpTests recalibrated to ClockDivisor 1 (PalTicksPerFrame 19656) with sample counts unchanged.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] SidPhase_AdvancesAtPhi2Rate_WhenClockedBySystemClock passes (OSC3 == 0x10)
  - [ ] SidAudioPumpTests sample-count assertions still pass after recalibration
- TEST-SIDEBARUI-001: AttachPanelViewModelTests.CollapseExpander_DockAndGlyph_TrackAnchorSide asserts the expander dock side and chevron glyph follow the panel anchor and raise PropertyChanged.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Anchored Left yields CollapseExpanderDock Right and glyph the left chevron
  - [ ] Anchored Right yields CollapseExpanderDock Left and glyph the right chevron, with PropertyChanged raised for both
- TEST-SNAPFULL-001: Restore then run N cycles equals continuous run N (full MachineState + memory equality) at multiple cut points, for C64 and C64+1541.
  Scope: layer-1+
- TEST-SNDREG-001: ViceGateSoundRegulatorTests (13) cover EvaluateSound outcomes and boundary, gate regulator selection, back-pressure blocking advance, warp precedence, and the SID IsAudioTimingSource/QueuedSampleCount wiring.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Back-pressure when queue at/over high-water advances zero cycles; below advances a chunk
  - [ ] Warp selects Warp regulator even when audio buffer is full
- TEST-SNP-001: ## TEST-SNP-001: Snapshot and Replay Tests

**ID:** TEST-SNP-001
**Title:** Snapshot and Replay Tests
**Priority:** P1 -- Important

### Condition

Save/load, deterministic replay, and state diff behavior are verified through round-trip and byte/trace comparison tests.

### Traceability

- **Related FR Area(s):** FR-SNP
  Scope: layer-1+
- TEST-SNP-002: Prove native VICE .vsf as the canonical ViceSharp snapshot format using the existing managed resume bridge after productionization and completion: default public and desktop snapshot paths, native x64sc/xvic read and write, complete managed state transfer, bidirectional round trips, 100000-cycle lockstep, transactionality, corruption handling, and mandatory non-skipped prerequisites.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Load mandatory x64sc and xvic .vsf fixtures through the managed production path and compare every supported module field with the native VICE oracle.
  - [ ] Save C64 and VIC-20 state from ViceSharp as .vsf, load the files in x64sc and xvic, and prove matching represented state.
  - [ ] Run VICE-to-ViceSharp-to-VICE and ViceSharp-to-VICE-to-ViceSharp round trips and prove all supported module fields survive.
  - [ ] From each restored C64 and VIC-20 checkpoint, run at least 100000 cycles in lockstep and fail at the first CPU, bus, video, timer, interrupt, or mapped-memory divergence.
  - [ ] Accept pinned-reader-compatible 58-byte current and 37-byte legacy VICE headers; walk 22-byte module headers with little-endian inclusive total lengths to exact EOF; reject wrong machine, unsupported versions, invalid/missing/duplicate mandatory modules, truncated framing, invalid or overflowing lengths, trailing bytes, and corruption rejected by structure or native VICE without visible mutation. Do not claim arbitrary bit-flip detection because VSF has no general checksum.
  - [ ] Treat both shims, the pinned VICE commit, exact MSYS2 MINGW64 tool paths, exact ROM-hash allowlist, and fixture/state-transfer manifests as mandatory prerequisites. Missing or stale prerequisites fail Category=NativeViceSnapshot with zero skipped tests.
  - [ ] Verify CaptureSnapshot, Avalonia .vsf file save/load, quick save/load, and ten named slots use native VSF by default; unsupported hosts return unavailable without fallback. Only generated protobuf ImportLegacySnapshot accepts the two proprietary formats, normal restore rejects them, and no capture emits them. UWP/Xbox implementation is not changed or claimed complete.
  - [ ] Build and inspect the local win-x64 publish and MSI for both shims, the recursively verified loose PE closure staged after publish and before WiX, VICE license/source-offer files, and no ROMs; install locally and pass default C64/VIC-20 native-VSF capture/restore smoke from the installed app.
- TEST-SYSBTN-001: Binding table + edge/LT-hysteresis + swap-flag (off-console). Acceptance: Default matches locked table; Press once; LT one On + one Off.
  Scope: layer-1+
- TEST-SYSBTN-002: Dispatcher command->host mapping (fakes). Acceptance: each command maps to one correct host call; Swap issues no ISettingsService call.
  Scope: layer-1+
- TEST-SYSBTN-003: Source-gen round-trip + reset-to-defaults + remap. Acceptance: profile round-trips byte-identical; ResetToDefaults yields Default.
  Scope: layer-1+
- TEST-SYSBTN-DEVICE: On-console button response (Menu/X/LT/A gating). Acceptance: system-button behaviors confirmed on device.
  Scope: layer-1+
- TEST-SYSINDEP-001: Tests that the drive CPU advances on its own clock (not host lockstep), IEC bus edges are observed by other systems before a read, per-system pacing sustains clock under load, and the true-drive LOAD parity test still passes.
  Scope: layer-1+
- TEST-TAP-001: Retain all existing datasette motor, TAP parsing, pulse timing, tape write and turbo-loader coverage, and explicitly map both historical meanings of TR-TAP-EDGE-001/TR-TAPE-EDGE-001. Unit assertions first discriminate independent correct/wrong mocks in a green suite, then remain unchanged for real managed/native providers and named workload gates.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Cover FR-TAP-001 motor control/timing and FR-RUNTIME-TAPE-002 command/sense/motor/write behavior, including pending-alarm stop, fast wind, DDR-only transitions, both RECORD/motor orders, failures and lifecycle. Reuse TEST-TAPE-RAMP-001 and TEST-TAPE-SENSE-001 cases without omitting them from cumulative gates.
  - [ ] Cover all FR-TAP-002 and TR-TAP-EDGE-001 decoder/header/clock/version scenarios, with separate block-decoder and live-datasette assertions. Reject v0 extension consumption, universal halfwave conversion, incorrect truncation, mismatch rejection and clock-domain conflation.
  - [ ] Cover FR-TAP-003 and FR-TAP-005 pulse boundaries, CBM and TurboTape pilot/header paths, standard load/checksum outcomes and every already-required named turbo format (Novaload, Biturbo, Turbotape, Cyberload, Pavloda), half/full-wave and changing-speed workloads. Pin actual fixtures and firmware; missing fixture, oracle or observation is a failure, never an invented successful loader result.
  - [ ] Cover retained public motor-state/mount/eject obligations, native tape-audio behavior and machine-visible SAVE/reload/new-existing-append results. Wrong API state, audio samples, persisted bytes, position or failure effects must be rejected by independent negative controls.
  - [ ] Cover sense dongle enable/disable/power-up and CP Clock F83 motor/SDA/write/SCL/sense cases from TR-TAPE-EDGE-001 ac-06, with device selection and no-datasette cases.
  - [ ] For every AC-mapped case, valid independent scripted mock responses pass all assertions. Deliberately wrong value, missing/extra/duplicate/reordered observation, wrong clock/side effect and failure-versus-success response must be rejected as an expected negative control within a passing native-safe suite. No failing suite or production-derived expected value qualifies. Pin fresh-process identity or the complete ordered prelude, including the attach-time full-image scan and any prior ports/devices; ordinary reset, detach or VSF reload must not be assumed to clear unrepresented process state. Record initial last-write line/clock, C64 previous-port output statics and SX-64 capacitors, VIA previous outputs, wobble phase/rounding remainder, azimuth RNG/remainder, audio ring/phase state and RTC persistent context/host-time inputs wherever observable. Timing-baseline fixtures explicitly disable wobble/tuning/azimuth; separate mandatory non-neutral fixtures exercise those resources and retained state with pinned native arithmetic/platform and RNG history. Neutral baseline success does not waive non-neutral behavior. RTC transactions pin persistent RAM/registers/offset, time source (including centiseconds), timezone and call order; halted seconds alone do not prove a deterministic RTC. If a required native input or observation cannot be fixed or captured without changing VICE, that case remains blocked, not skipped or satisfied. Native VSF remains the canonical snapshot format and its real interoperability gates remain mandatory.
  - [ ] Bind the same inputs/assertions to real managed/native providers, retain exact identities and first divergence, run full applicable current-plus-prior suites with zero failed/skipped/aborted cases, and obtain AGREE at accuracy/completeness at least 98 with zero FAIL/UNKNOWN before satisfaction. Preserve all original xvic/x64sc/VSF and later start-approval gates.
- TEST-TAPE-RAMP-001: Validate FR-RUNTIME-TAPE-002 ac-03/ac-04 and TR-TAPE-EDGE-001 ac-01 through ac-05 using shared clock/control/alarm cases. Green correct-mock and expected negative-control evidence precedes production changes. Reuse the same cases and assertions with real managed/native observations; no fixed historical passing total is an acceptance condition.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Startup_NoPendingAlarm_DeadlineBoundaries and Startup_PendingAlarm_NotOverwritten assert ordered schedules, callback times and events; incorrect early/late/missing/duplicated/reordered events are rejected within a passing mock harness. Start_CommandWhileMotorRunning_NoPendingAlarm and Start_CommandWhileMotorRunning_PendingAlarm cover absent/read-only/writable media, clock zero/nonzero, seek/no-seek, retained write state and a pending stop deadline; reject omission of the command helper path, wrong alarm replacement or absence, wrong seek, wrong write-clock/bit seeding, and clearing a stop deadline that START leaves intact.
  - [ ] Run all named delayed-stop and cancellation cases in the amendment ledger, retaining deadline and callback boundaries separately. Reject immediate stop, guaranteed-deadline stop despite an existing late alarm, repeated-off deadline extension and unconditional off/on restart. MotorOn_CancelsPendingStop covers deadline-1 and the exact deadline with motor-on ordered before callback dispatch; assert stop-clock clearing, unchanged running/alarm/write state and no startup ramp, and reject wrong mocks that restart only in these earlier cancellation cases.
  - [ ] Real-provider validation reuses every mock input/assertion and obtains actual native and managed ordered observations. Source inspection, an analytical trace or a duration getter cannot substitute for actual pulse/state evidence. Missing capability fails closed.
  - [ ] Run every case without skips in full applicable cumulative native-safe and real-emulator suites after native-safe integration. Retain exact commands, counts, TRX/logs, identities, failure history and accepted hostile review before satisfaction.
  - [ ] Run each named fast-wind case for both FORWARD and REWIND, attached/absent media and stopped/running motor. Assert 1000-cycle command alarms, preservation by motor-on when pending and 32000 startup only when the ordinary helper has no pending alarm; reject unconditional startup timing.
- TEST-TAPE-SENSE-001: Validate FR-RUNTIME-TAPE-002 ac-01/ac-02/ac-05 through ac-08 and the corresponding TR-TAP-EDGE-001 and TR-TAPE-EDGE-001 criteria. Establish independent correct and discriminating wrong mocks, then bind unchanged inputs/assertions to real providers. Preserve historical evidence without treating old counts or wrong expectations as acceptance.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] TransportSense_MediaCommandMatrix covers five commands from STOP, absent/read-only/writable images, enabled-state sense gating and later STOP. Reject wrong polarity, ignored-command state changes and missing transitions.
  - [ ] MachineSense_C64CpuPortAndVic20Via1 covers both machines and DDR/output masking; TapeRead_PulseInterruptRouting separately covers CIA1 FLAG and VIA2 CA1. Reject wrong port/bit/direction/interrupt target and extra or missing pulse events.
  - [ ] Run all recording eligibility/window/edge cases in the ledger, including startup, pending stop, after nominal deadline before callback and after executed stop. Assert writes/no-writes and retained line state. Reject no-image recording and invented startup inhibits.
  - [ ] Run both RECORD/motor orderings and gap/error cases including 0/6/7/8/2046/2047, remainder carry, same/future-clock edges, v0/v1/v2, 0xffffff/0x1000000/0x1000001 and zero/one/two/three-byte long-payload writes. Assert bytes, image position/size/dirty state, counters, retained state and STOP side effects against independent native expectations.
  - [ ] Reuse all fixtures/assertions with actual immutable native and managed providers, then run full cumulative suites. Missing, failed, skipped, aborted or mock-only cases cannot satisfy AC; require independent AGREE, both scores at least 98 and zero FAIL/UNKNOWN.
  - [ ] Run machine motor/write cases for C64 bit 5/bit 3, input/output DDR and DDR-only changes, VIC-20 PCR raw-byte motor and VIA2 PB3/DDRB transitions, repeated writes and SX-64. Wrong bit, normalized-but-nonnative PCR behavior, omitted DDR edge and connected SX-64 mocks must be rejected.
  - [ ] Run datasette and tapeport enable/dispatch/attach/detach/reset cases with each reset-resource branch. Reject UI-only internal-state assertions, synthesized motor clears and universal tapeport-active suppression.
  - [ ] Run sense-dongle lifecycle and CP Clock F83 truth-table cases. Reject datasette-only sense, forced deassertion by the dongle disable callback, wrong SDA inversion, wrong SCL normalization and incorrect RTC sense combinations.
- TEST-TESTGATE-002: XboxTestConventionTests: Category=Xbox uses plain [Fact]/[Theory] + [Trait], never [ViceFact]/Assert.Skip, so 0-skipped is genuine per slice scope. Acceptance: each slice trx within its --filter reports Failed=0 and Skipped=0. (Shaped TEST id created to carry the plan's named XboxTestConventionTests, which the store rejects as a raw testId.)
  Scope: layer-1+
- TEST-TESTGATE-008: AOT proxy + on-console feasibility (= S0). Acceptance: the three S0 go/no-go criteria GREEN.
  Scope: layer-1+
- TEST-TESTGATE-009: DesktopRegressionGuard. Acceptance: Determinism pass-count == baseline; AvaloniaBoundaryTests green; AOT 0 warnings.
  Scope: layer-1+
- TEST-TESTGATE-010: On-console device checklists. Acceptance: device slices marked onConsole; checklists recorded.
  Scope: layer-1+
- TEST-TICKHIST-001: TickHistoryRecorderTests (ring order/capacity, write bundling, reconstruction), TickHistoryCaptureTests (bus event + pump capture + GetTickHistory/ReadMemoryAtTick), TickHistoryViewModelTests (refresh newest-first, paused inspect, close).
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Reconstruct at the newest tick yields current memory; at an earlier tick yields the pre-write byte
  - [ ] InspectAsync opens the debug screen with a memory dump only when paused
- TEST-TICKHIST-002: Verify the emulation pump does not capture when recording is disabled (default) and that GetTickHistory arms recording.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Pump_DoesNotCaptureHistory_WhenRecordingDisabled - default off yields empty history after pumping.
  - [ ] GetTickHistory_ArmsHistoryRecording - calling the RPC sets HistoryRecordingEnabled true.
- TEST-UI-001: Sidebar, status bar, attach panel, settings, keyboard map selection, monitor dock/pop-out, focus, and startup behavior are verified with fake host clients.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] ViewModel tests prove peripherals panel drive entries show IEC active and idle states from host telemetry.
  - [ ] ViewModel tests prove status bar IEC activity uses the same source while preserving status fields.
  - [ ] Focused UI tests run with zero failed and zero skipped tests.
- TEST-UI-COREBOUNDARY-001: Extend boundary scanner coverage to relevant real C# and AXAML sources and demonstrate the existing direct dependency as a red case.
Exercise profile selection, bank updates, import, build, save and failure reporting through the abstraction with controlled file I/O and collection notifications.

## Acceptance Criteria
- **ac-01**: Extend boundary scanner coverage to relevant real C# and AXAML sources and demonstrate the existing direct dependency as a red case.
- **ac-02**: Exercise profile selection, bank updates, import, build, save and failure reporting through the abstraction with controlled file I/O and collection notifications.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Extend boundary scanner coverage to relevant real C# and AXAML sources and demonstrate the existing direct dependency as a red case.
  - [ ] Exercise profile selection, bank updates, import, build, save and failure reporting through the abstraction with controlled file I/O and collection notifications.
- TEST-UI-DEVCARDART-001: DeviceArtKeyTests: slot-kind defaults (Drive8/9 to Breadbin, Tape to Datasette, Cartridge to Cartridge); model mapping rows (1540/1541 to Breadbin, 1541-II, 1571, 1581, SFD-1001, CMD-HD, CMD-FD/fd2000, unknown/empty to Breadbin); AllKeys superset of every resolvable key; DeviceArtKey PropertyChanged on ApplyAttachment; MarkEmpty retains last model.
  Scope: layer-1+
- TEST-UI-DEVCARDART-002: DeviceArtAssetTests: every SVG source exists with a viewBox; DeviceArt.axaml exists, XML-parses, DrawingImage key set is a superset of the catalog keys, all 6 brushes present; per-SVG sha256 (CRLF-normalized) matches the generated source comments and the comment set equals the SVG file set; PeripheralCardView/App.axaml wiring text-asserts.
  Scope: layer-1+
- TEST-UI-DEVCARDART-003: DeviceArtHeadlessTests (AvaloniaFact): AvaloniaXamlLoader loads DeviceArt.axaml; every catalog key resolves to a DrawingImage with non-null Drawing; DeviceArtKeyConverter returns null without throwing for unknown keys.
  Scope: layer-1+
- TEST-UI-DIAG-001: The Avalonia shell exposes a Copy Debug Attach Info command and formats endpoint, current session id, app version, attach JSON, and latest status for clipboard use.
  Scope: layer-1+
- TEST-UIDROP-002: Verify PRG drop acceptance and host RAM load: `ShellViewModel` routes `*.prg` to `LoadProgramAsync` without attach/reset; `IsDropStartSupported` is true for `.prg` and existing media and false for unsupported types; `PrgMemoryLoader` writes at the load address, updates BASIC pointers only when load equals TXTTAB, and reports ran; `EmulatorHostService.LoadProgramAsync` loads payload into the session, sets Ran, and starts BASIC RUN automation only for BASIC-start PRGs; invalid PRGs return InvalidArgument; `GrpcEmulatorHostService` maps LoadProgram request/response fields.
  Scope: layer-1+
- TEST-UIFLYOUT-001: VM: toggling DockSide flips Left<->Right and IsPaneOpen opens/closes; default state correct. App-launch: flyout opens/closes, docks both sides via the single icon button, no Left/Right buttons.
  Scope: layer-1+
- TEST-UIMENUBAR-001: VM: menu commands invoke the existing actions/host services (attach/eject, reset, snapshot, warp, true-drive toggle, swap joysticks). App-launch: menu structure matches the plan and commands work.
  Scope: layer-1+
- TEST-UIPERIPHERAL-001: VM: AttachSlotViewModel exposes status/RO/activity/TrueDrive/SupportsTrueDrive and Attach/Eject route to host. App-launch: Drive8/Drive9/Tape/Cartridge all render via the one PeripheralCardView with correct per-slot controls.
  Scope: layer-1+
- TEST-UISET-001: ## TEST-UISET-001: VIC-20 RAM settings acceptance

**ID:** TEST-UISET-001
**Priority:** medium
**Status:** pending; no completion claim.

### Description

VIC-20 RAM settings acceptance verifies all selected BLK regions in fresh host/runtime state after Apply+Restart and exactly 28159 BASIC bytes free with the pinned ROM.

### Acceptance Criteria

- **ac-all-spec**: All+Apply+Restart keeps BLK checks and Vic20MemorySpec all.
- **ac-bytes-free**: After pinned-ROM VIC-20 All RAM plus Apply+Restart, the READY BASIC boot reports exactly 28159 BYTES FREE; verify actual runtime memory as well as checked BLK controls.

### Traceability

- Approved plan: [PLAN-GROK-COMPLETION-20260906](../../plans/PLAN-GROK-COMPLETION-20260906.md).
- Gate and test mapping: [Grok completion AC matrix](../traceability/Grok-Completion-AC-Matrix-2026-09-06.md).
- Criteria remain pending until executable evidence and independent Astra agreement are recorded.
  Scope: layer-1+
- TEST-UISET-002: Two fresh final-binary RemoteControl passes exercise every applicable VIC-20 Settings control. Verify effective visibility, Active/Accepted/Draft semantics, single-transaction Warp above150 percent, PAL/NTSC runtime geometry/clocks, Revert, cartridge persistence and actual uIEC I/O.

## Acceptance Criteria
- **ac-inventory**: In each of two fresh final-binary live passes, drive every applicable VIC-20 Settings control and verify effective visibility, fresh host Active/Accepted state and actual runtime effects, including image/bank/preset/writeback and uIEC I/O.
- **ac-warp**: Warp via the single Settings transaction sets LIMITER WARP, LimiterRatePercent 0 and measured EffectiveClockPercent greater than 150 in WarpModeTests and live RemoteControl receipts.
- **ac-palntsc**: PAL vs NTSC Apply+Restart yields CLOCK ~1.108 vs ~1.023 MHz and ContentHeight 284 vs 234 with READY.
- **ac-revert**: Revert is exercised while dirty and restores Accepted values without cancelling an earlier accepted pending restart; save-on-exit preferences remain independent.
- **ac-state-and-runtime**: Apply, Apply+Restart, validation failure, speed cycling, every RAM BLK, persistence preferences and builder open/close follow TEST-SETTINGS-TXN-001 and TEST-COMPLETION-001, with host/runtime oracles after refreshing owned-process debug attach and session IDs.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] In each of two fresh final-binary live passes, drive every applicable VIC-20 Settings control and verify effective visibility, fresh host Active/Accepted state and actual runtime effects, including image/bank/preset/writeback and uIEC I/O.
  - [ ] Warp via the single Settings transaction sets LIMITER WARP, LimiterRatePercent 0 and measured EffectiveClockPercent greater than 150 in WarpModeTests and live RemoteControl receipts.
  - [ ] PAL vs NTSC Apply+Restart yields CLOCK ~1.108 vs ~1.023 MHz and ContentHeight 284 vs 234 with READY.
  - [ ] Revert is exercised while dirty and restores Accepted values without cancelling an earlier accepted pending restart; save-on-exit preferences remain independent.
  - [ ] Apply, Apply+Restart, validation failure, speed cycling, every RAM BLK, persistence preferences and builder open/close follow TEST-SETTINGS-TXN-001 and TEST-COMPLETION-001, with host/runtime oracles after refreshing owned-process debug attach and session IDs.
- TEST-UISET-003: Headless and live RemoteControl tests for C64 variants (at least c64, c64c, ntsc from host catalog) mutate shared Settings controls, Apply+Restart PAL vs NTSC CLOCK ~0.985 vs ~1.023 MHz with READY, Warp LIMITER WARP and CLOCK percent well above 100, and assert VIC-20-only AutomationIds are not effectively visible on C64 and are visible on VIC-20.

## Acceptance Criteria
- **ac-c64-inventory**: Shared Settings inventory mutates and sticks for C64 models c64, c64c, and ntsc.
- **ac-c64-palntsc-warp**: C64 PAL versus NTSC boots READY with clocks approximately 0.985 versus 1.023 MHz; Warp status and live runtime satisfy TR-WARP-STATUS-001, including EffectiveClockPercent greater than 150.
- **ac-visibility**: VIC-20-only AutomationIds are not effectively visible on C64 Computer and are visible on VIC-20 Computer.
- **ac-catalog-14**: Automatically cover all 14 available C64 host catalog profiles and live c64/c64c/ntsc representatives. Cover case-insensitive IDs, unavailable/out-of-family/null/reentry rejection, stable Models within family and disabled Apply with a visible explanation for unusable catalog.
- **ac-draft-preservation**: Exercise shared synchronization for initial load, refresh, Apply, Revert and persisted state, preserving VIC-20 draft RAM/cart fields across family switches without attaching VIC-20 hardware to a C64 runtime.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Shared Settings inventory mutates and sticks for C64 models c64, c64c, and ntsc.
  - [ ] C64 PAL versus NTSC boots READY with clocks approximately 0.985 versus 1.023 MHz; Warp status and live runtime satisfy TR-WARP-STATUS-001, including EffectiveClockPercent greater than 150.
  - [ ] VIC-20-only AutomationIds are not effectively visible on C64 Computer and are visible on VIC-20 Computer.
  - [ ] Automatically cover all 14 available C64 host catalog profiles and live c64/c64c/ntsc representatives. Cover case-insensitive IDs, unavailable/out-of-family/null/reentry rejection, stable Models within family and disabled Apply with a visible explanation for unusable catalog.
  - [ ] Exercise shared synchronization for initial load, refresh, Apply, Revert and persisted state, preserving VIC-20 draft RAM/cart fields across family switches without attaching VIC-20 hardware to a C64 runtime.
- TEST-UISETTINGS-001: VM: settings VM exposes machine/video/audio/input/limiter/resource selections + apply. App-launch: SettingsView renders + applies settings.
  Scope: layer-1+
- TEST-VIA-001: VIA timer, shift register, port handshake, VIC-20, and drive integration behavior are verified with unit and machine integration tests.
  Scope: layer-1+
- TEST-VIC-001: Raster timing, display modes, sprites, collisions, badlines, borders, FLI/AFLI, banking, and DMA timing are verified with deterministic frame or trace comparisons. The gate includes closed-border sprite masking, open-border sprite visibility, sprite priority over background/foreground pixels, per-model sprite DMA access timing, and VIC-II matrix/idle fetch behavior including prefetch `$ff` fill and ECM idle graphics addresses.
  Scope: layer-1+
- TEST-VIC20-001: Vic20PixelLockstep verifies normal-border geometry and named SequenceEqual facts vs xvic capture_visible_frame: Index_ReadyPal_SequenceEqual, Index_ReadyNtsc_SequenceEqual, Index_BusyPal_SequenceEqual, Bgra_ReadyPal_SequenceEqual, Bgra_ReadyNtsc_SequenceEqual, Bgra_BusyPal_SequenceEqual. Capture is non-sentinel with opaque alpha (NTSC unpainted clamp rows opaque black). Other border modes remain Partial; not whole-machine Exact.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] Vic20PixelLockstep.Index_ReadyPal_SequenceEqual passes 0 fail 0 skip (evidence: isolated pixel gate 2026-09-04)
  - [ ] Vic20PixelLockstep.Index_ReadyNtsc_SequenceEqual passes 0 fail 0 skip (evidence: isolated pixel gate 2026-09-04)
  - [ ] Vic20PixelLockstep.Index_BusyPal_SequenceEqual passes 0 fail 0 skip (evidence: isolated pixel gate 2026-09-04)
  - [ ] Vic20PixelLockstep.Bgra_ReadyPal_SequenceEqual and Bgra_BusyPal_SequenceEqual pass 0 fail 0 skip (evidence: isolated pixel gate 2026-09-04)
  - [ ] Vic20PixelLockstep.Bgra_ReadyNtsc_SequenceEqual passes 0 fail 0 skip (opaque clamp-row alpha) (evidence: isolated pixel gate 2026-09-04)
- TEST-VIC20-FLASH-001: `Flash040EraseLatencyTests` verifies the VICE TYPE_B sector timeout, sector/chip cycle budgets, busy status, cancel, suspend, and resume behavior. `Vic20ExpansionCartTests`, `UltimemParityTests`, `MegaCartParityTests`, and `MediaServiceHostVic20ExpansionTests` verify mapping, dirty-state separation, and atomic detach persistence.
  Scope: layer-1+
- TEST-VIC20-SOUND-001: `Vic20SoundLockstep` verifies deterministic managed silence/tone/multi-register output, no silent register-store allocation after warmup, and focused byte-identical silence/tone batches against native xvic. `Vic20AudioWiringTests` verifies machine-clock advancement and 256-sample delivery to the configured audio backend.
  Scope: layer-1+
- TEST-VIC-CHECKPOINT-001: Managed PAL frame periodic raster/cycle-counter (2 Facts), native screen-RAM roundtrip and one-frame DMA read-only (2 ViceFacts), sprite-3 DMA window for 5 models (5 ViceTheory). All 9 pass.
  Scope: layer-1+
- TEST-VICE-001: Verify that every native parity oracle uses the pinned unmodified VICE target and that wrapper-only integration fails closed when required target observations are unavailable.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] A clean-target preflight passes for the pinned native/vice commit and a simulated dirty-target fixture is rejected before parity execution.
  - [ ] The wrapper integration test proves no tracked native/vice file changes across build and execution.
  - [ ] Oracle receipts prove native commit identity, clean tracked source, wrapper provenance, exact commands, and failed 0 skipped 0 for the claimed gate.
  - [ ] When a required real bus or timing observation is unavailable, the test fails at that first missing observation and does not substitute PeekBus, PC inference, fabricated data, or a modified native build.
- TEST-VIC-RC-001: After writing DEN=1/YSCROLL=0/1 and advancing to specific rasterLine/rasterX, CurrentRowCounter and IsGraphicsIdle match VICE viciisc/vicii-cycle.c:541-563 expectations. All 11 pass.
  Scope: layer-1+
- TEST-VSFLOCKSTEP-001: The current focused external-x64sc VSF acceptance gate must load the committed hash-pinned ready-c64sc-truedrive.vsf through the pinned native shim, prove the complete 24-module container frames exactly to EOF, assert rc=0 and snapshot_last_error=0, and compare resumed MAINCPU registers to the MAINCPU module. Historical pass or skip counts are not evidence; this record remains pending until current zero-skip execution proof exists.
  Scope: layer-1+
  **Acceptance Criteria:**
  - [ ] The focused ExternalX64ScVsf_FullyResumes_CpuMatchesSnapshot gate runs without skipped prerequisites, loads the hash-pinned 24-module fixture through pinned x64sc with rc=0 and snapshot_last_error=0, and proves resumed A, X, Y, SP, and PC equal the MAINCPU module values.
- TEST-X64SC-LOCKSTEP-001: Every required x64sc model profile is validated against native x64sc for deterministic startup, BASIC prompt, keyboard input, disk attach/autostart, cartridge boot, and reset scenarios. Validation compares CPU registers, flags, cycle count, selected memory windows, CIA/VIC/SID observable register state, IRQ/NMI state, and frame/raster checkpoints. Missing ROMs, unavailable native x64sc binaries, unsupported variants, skipped variants, or stubbed checks fail the gate.
  Scope: layer-1+
- TEST-XAUDIO-001: Factory-null + SID inert. Acceptance: headless CreateDefault() null; SID null-backend IsAudioTimingSource==false, no samples.
  Scope: layer-1+
- TEST-XAUDIO-002: XAudio2AudioMath queue/ring boundaries. Acceptance: full ring RoomAvailable false + drop-oldest; empty under-run silence; index wraps.
  Scope: layer-1+
- TEST-XAUDIO-003: Volume/mute gain-once. Acceptance: EffectiveGain applied once in AudioSampleConverter.
  Scope: layer-1+
- TEST-XAUDIO-004: Non-blocking submit + Pause unparks worker (off-console). Acceptance: SubmitSamples non-blocking; Pause releases waiter/unparks worker.
  Scope: layer-1+
- TEST-XAUDIO-DEVICE: On-console audio smoke. Acceptance: SID audible; mute silences; suspend/resume clean.
  Scope: layer-1+
- TEST-XBOXAOT-001: Off-console AOT/trim link gate 0 IL warnings + reflection-free audit. Acceptance: publish exit 0, 0 IL2xxx/IL3xxx; ViceNative uncalled.
  Scope: layer-1+
- TEST-XBOXCI-001: azure-pipelines.*.yml exclude device targets. Acceptance: no PublishXbox/DeployXbox in CI/release YAMLs.
  Scope: layer-1+
- TEST-XBOXDEPLOY-007: On-console install/launch/first-frame + rollback. Acceptance: MSIX deploys clean; first frame; rollback documented/works.
  Scope: layer-1+
- TEST-XBOXGPL-001: License/vkm/no-ROM/source-URL staging. Acceptance: Licenses/ contents + vkm present + no ROM *.bin + compiled SourceUrl.
  Scope: layer-1+
- TEST-XBOXGPL-002: Automated tests must fail if C= logo attribution is missing from THIRD_PARTY_NOTICES.md
or from AboutInfo/AboutViewModel. Cover Alien426, CC BY-SA 4.0, and commons.wikimedia.org.
  Scope: layer-1+
- TEST-XBOXPKG-001: Csproj/slnx shape. Acceptance: core refs present, forbidden refs absent, slnx builds.
  Scope: layer-1+
- TEST-XBOXPKG-003: Manifest XML + capability rule. Acceptance: Windows.Xbox device family, x64, internetClient-iff-download-kept.
  Scope: layer-1+
- TEST-XBOXTOPO-001: Csproj shape + reference-ban. Acceptance: conditional TFM + core refs present; banned refs absent.
  Scope: layer-1+
- TEST-XBOXTOPO-002: Default-props solution build needs no UWP workload. Acceptance: slnx build exit 0 workload-absent.
  Scope: layer-1+
- TEST-XBOXUI-001: NavigationViewModel push/GoBack stack correctness; overlays push no stack entry. Acceptance: Settings->DeviceSetup, GoBack twice -> Home in order.
  Scope: layer-1+
- TEST-XBOXUI-002a: FocusMapBuilder BuildGrid(3,2) edges correct, top-edge Up null, no wrap.
  Scope: layer-1+
- TEST-XBOXUI-002b: On-console focus-visual smoke. Acceptance: high-visibility focus in TV-safe rect.
  Scope: layer-1+
- TEST-XBOXUI-003: InputContext observer: quick menu -> non-Gameplay; closing with empty stack -> Gameplay.
  Scope: layer-1+
- TEST-XBOXUI-004: Simulated navigation/overlay toggles never stop/dispose the pull adapter.
  Scope: layer-1+
- TEST-XBOXUI-005a: Cadence: 10 fires/200ms -> 10 pulls +/-1.
  Scope: layer-1+
- TEST-XBOXUI-005b: VideoSurfacePureSink forbidden-identifier scan (no RunFrame/StepInstruction/Reset/SetJoystickState/SetKeyState).
  Scope: layer-1+
- TEST-XBOXUI-006: RETURN tile emits SetKeyState("Return",true) then false; shift-latch+F1 emits "F2"; every KeyName in known-good list; RESTORE tile calls SetRestoreState.
  Scope: layer-1+
- TEST-XBOXUI-007: Boundary: ViewModels reference only Abstractions+Protocol+Xbox.Input; forbidden-identifier scan (no Core/Chips/Architectures/Host/Avalonia/Grpc/XAML).
  Scope: layer-1+
- TEST-XBOXUI-008: AboutViewModel LicenseIdentifier=="GPL-2.0-or-later", AttributionText contains "VICE", SourceOfferText non-empty + URL.
  Scope: layer-1+
- TEST-XKBD-001: Off-console facade + ViewModel test against a fake/spy keyboard input. Acceptance: SetRestoreState routes to NMI path keycode 0x31 distinct from SetKeyState.
  Scope: layer-1+
- TEST-XSET-001: Umbrella off-console suite (XboxDataPathBridgeTests, SettingsOptionCatalogTests, XboxSettingsViewModelTests, XboxDeviceSetupViewModelTests, RomProvisionEvaluatorTests, XboxRomProvisioningViewModelTests, XboxUiStateStoreTests, XboxViewModelBindabilityTests). Acceptance: 0 failed/0 skipped per slice filter.
  Scope: layer-1+
- TEST-XSET-DEVICE: On-console first-run provisioning + persistence E2E. Acceptance: ROMs provisioned + boot to READY; setting persists across relaunch; d64 attach with drive-model selector.
  Scope: layer-1+
- TEST-XVIDEO-001: Pure byte-exact copy + no-advance spy. Acceptance: 384x272 BGRA copy sized from FrameGeometry.BufferLength; core never advanced.
  Scope: layer-1+
- TEST-XVIDEO-002: GC.GetAllocatedBytes delta==0 steady-state render tick. Acceptance: zero-allocation over steady-state window.
  Scope: layer-1+
- TEST-XVIDEO-DEVICE: On-console video smoke. Acceptance: image correct, letterboxed, no tear.
  Scope: layer-1+
