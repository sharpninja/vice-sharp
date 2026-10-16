# Technical Requirements (MCP Server)

## TR-AIREVIEW-ROUTING-001

**Auditable Astra xhigh AI review routing** — Add ReasoningEffort through aiUnit strategy configuration, resolver, inline specifications, CLI invocation and result metadata.
Support AIUNIT_REASONING_EFFORT as the process override and emit Codex -c model_reasoning_effort with the resolved value.
Remove forced Grok attributes and prompt instructions. Use one selected agent per theory so no extra paid aggregator call is implied.
Test deterministic routing without paid calls, then publish through aiUnit supported Nuke packaging and its Azure pipeline using pool Default and existing stable-version release rules.
Consume the verified published aiUnit version in ViceSharp; retain package provenance and exact model/effort receipts for both paid theories.

## Acceptance Criteria
- **ac-01**: Add ReasoningEffort through aiUnit strategy configuration, resolver, inline specifications, CLI invocation and result metadata.
- **ac-02**: Support AIUNIT_REASONING_EFFORT as the process override and emit Codex -c model_reasoning_effort with the resolved value.
- **ac-03**: Remove forced Grok attributes and prompt instructions. Use one selected agent per theory so no extra paid aggregator call is implied.
- **ac-04**: Test deterministic routing without paid calls, then publish through aiUnit supported Nuke packaging and its Azure pipeline using pool Default and existing stable-version release rules.
- **ac-05**: Consume the verified published aiUnit version in ViceSharp; retain package provenance and exact model/effort receipts for both paid theories.
**Covered by:** FR: FR-AIREVIEW-ROUTING-001; TEST: TEST-AIREVIEW-ROUTING-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Add ReasoningEffort through aiUnit strategy configuration, resolver, inline specifications, CLI invocation and result metadata.
- [ ] Support AIUNIT_REASONING_EFFORT as the process override and emit Codex -c model_reasoning_effort with the resolved value.
- [ ] Remove forced Grok attributes and prompt instructions. Use one selected agent per theory so no extra paid aggregator call is implied.
- [ ] Test deterministic routing without paid calls, then publish through aiUnit supported Nuke packaging and its Azure pipeline using pool Default and existing stable-version release rules.
- [ ] Consume the verified published aiUnit version in ViceSharp; retain package provenance and exact model/effort receipts for both paid theories.

## TR-ALLOC-001

**Zero Managed Allocations Per Emulation Cycle on Hot Path** — The emulation hot path (the per-cycle execution of CPU, VIC-II, SID, and CIA ticks) shall produce zero managed heap allocations during steady-state operation. This eliminates GC pauses that would cause frame drops and audio glitches. All per-cycle data structures shall use value types (structs), stack allocation, or pre-allocated buffers.

## Acceptance Criteria
- A benchmark running 10 million emulation cycles reports zero `GC.GetAllocatedBytesForCurrentThread()` delta on the hot-path thread.
- `dotnet-counters` monitoring during a 60-second emulation session shows zero Gen0 collections attributable to the emulation thread.
- The `RunFramePerfProbe` benchmark (`tests/ViceSharp.Benchmarks/RunFramePerfProbe.cs`) asserts a zero `GC.GetAllocatedBytesForCurrentThread()` delta over the measured `RunFrame` window; the assertion is smoke-tested by `BenchmarksSmokeTests.RunFramePerfProbe_SmallRun_ReportsC64PalAndNoAllocations` (TEST-PERF-RUNFRAME-001).
- The PayloadArena reset occurs exactly once per frame and completes in under 100ns.
- All event/message payloads in the pub/sub system are value types (per TR-PUBSUB-001).
- No `new` keyword appears in hot-path code paths except for stack-allocated `Span<T>` or `stackalloc`.
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] The measured C64 PAL RunFrame loop reports zero current-thread allocations over the measured frame window. (evidence: RunFramePerfProbe 60 600: allocated=0 bytes)
- [x] BenchmarkDotNet run reports no managed allocation for C64PalRunFrameBenchmark. (evidence: BenchmarkDotNet C64PalRunFrameBenchmark: Allocated column reported no managed allocation)

## TR-AOT-001

**Full NativeAOT Compatibility with Zero Reflection on Hot Path** — All ViceSharp assemblies shall be fully compatible with .NET NativeAOT publication. This means no use of runtime reflection, `System.Reflection.Emit`, or dynamic code generation on the emulation hot path. All assemblies shall pass the NativeAOT trim analysis without warnings. Source generators shall be used where compile-time code generation is needed.
**Status:** pending
Scope: layer-1+

## TR-BASELINE-CORRECTNESS-001

**Approved baseline regression repairs** — BasicBus unregister and unmapped-write tests preserve the intentional open-bus latch values 0x55 and 0xBB. Native-shim users follow the NativeVice collection convention.
Repair cycle-2078 snapshot branch-timing divergence using a minimal pinned-VICE-referenced instruction sequence for taken, untaken and page-crossing branches on C64 and VIC-20.
Keep the 5000-cycle snapshot fixture unchanged and require standalone and post-native-lifecycle success. Preserve passing performance/prefetch behavior.
Trace acceptance coverage before removing obsolete prefetch quarantine. Replace obsolete renderer assertions with equivalent cycle-aware tests before retirement. Move intentionally failing manual diagnostics to an explicit diagnostic tool, never hide actual product failures.

## Acceptance Criteria
- **ac-01**: BasicBus unregister and unmapped-write tests preserve the intentional open-bus latch values 0x55 and 0xBB. Native-shim users follow the NativeVice collection convention.
- **ac-02**: Repair cycle-2078 snapshot branch-timing divergence using a minimal pinned-VICE-referenced instruction sequence for taken, untaken and page-crossing branches on C64 and VIC-20.
- **ac-03**: Keep the 5000-cycle snapshot fixture unchanged and require standalone and post-native-lifecycle success. Preserve passing performance/prefetch behavior.
- **ac-04**: Trace acceptance coverage before removing obsolete prefetch quarantine. Replace obsolete renderer assertions with equivalent cycle-aware tests before retirement. Move intentionally failing manual diagnostics to an explicit diagnostic tool, never hide actual product failures.
**Covered by:** FR: FR-COMPLETION-001, FR-NATIVERESIDUE-001, FR-VIC20-002; TEST: TEST-BASELINE-CORRECTNESS-001, TEST-COMPLETION-001, TEST-REMOTE-INTEGRATION-001, TEST-UI-COREBOUNDARY-001, TEST-DEPS-202607-001, TEST-NATIVERESIDUE-001, TEST-UISET-001, TEST-UISET-002
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] BasicBus unregister and unmapped-write tests preserve the intentional open-bus latch values 0x55 and 0xBB. Native-shim users follow the NativeVice collection convention.
- [ ] Repair cycle-2078 snapshot branch-timing divergence using a minimal pinned-VICE-referenced instruction sequence for taken, untaken and page-crossing branches on C64 and VIC-20.
- [ ] Keep the 5000-cycle snapshot fixture unchanged and require standalone and post-native-lifecycle success. Preserve passing performance/prefetch behavior.
- [ ] Trace acceptance coverage before removing obsolete prefetch quarantine. Replace obsolete renderer assertions with equivalent cycle-aware tests before retirement. Move intentionally failing manual diagnostics to an explicit diagnostic tool, never hide actual product failures.

## TR-BUILD-001

**Nuke Build System with GitHub origin as Source of Truth** — ViceSharp shall use the Nuke build system for all build, test, package, and publish operations. The build definition is a C# project (`build/_build.csproj`) that defines targets as code. GitHub `origin` (`https://github.com/sharpninja/vice-sharp.git`) is the git source of truth. Azure DevOps is retired and is not used. Historical `azure-pipelines.ci.yml` and `azure-pipelines.release.yml` files may remain in the tree; they are not the live git or wiki authority.
**Status:** pending
Scope: layer-1+

## TR-CHIPSTATE-CAPTURE-001

**IStatefulDevice per-tick chip capture** — IStatefulDevice (StateName, StateSize, CaptureState(Span), DecodeState) is implemented by VIC/SID/CIA/PLA. The recorder preallocates per-slot buffers (zero-alloc capture) and deep-copies on Snapshot; the pump registers Machine.Devices.All.OfType<IStatefulDevice>() before subscribing and the host decodes in the same order.
**Covered by:** FR: FR-CHIPSTATE-001; TEST: TEST-CHIPSTATE-001
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] A captured tick carries each device's StateSize bytes; capture allocates nothing on the hot path
- [x] Snapshot deep-copies chip state so ring reuse cannot corrupt an earlier snapshot

## TR-COMPLETION-MANIFEST-001

**Exhaustive approved-work completion evidence** — Add an exhaustive manifest covering every solution and relevant out-of-solution test project, every category excluded by ordinary Nuke Test and every required native/remote/AI workload.
Implement Nuke CompletionTest as the supported entry point; gates retain exact commands, configuration, filters, counts, TRX, stdout/stderr, diagnostic logs and relevant hang dumps.
Final validation includes Release solution build, managed/native/determinism/parity/CLI/integration/AI scopes, changed-AC traceability and git diff --check.
Fresh live passes first prove RemoteControl default-off failure, then enabled capabilities/actions and every applicable Settings control. Match debug-attach metadata to the owned PID and refresh session/tree IDs after restart.
Live representative profiles are c64, c64c, ntsc and VIC-20 PAL/NTSC; automate all 14 C64 profiles, retain READY/geometry/clock/pacing/Warp evidence and verify exactly 28159 BASIC bytes free for pinned-ROM VIC-20 all-RAM.
Prove actual cartridge image/bank/preset/writeback and uIEC I/O. Stop only validation processes started by this work.

## Acceptance Criteria
- **ac-01**: Add an exhaustive manifest covering every solution and relevant out-of-solution test project, every category excluded by ordinary Nuke Test and every required native/remote/AI workload.
- **ac-02**: Implement Nuke CompletionTest as the supported entry point; gates retain exact commands, configuration, filters, counts, TRX, stdout/stderr, diagnostic logs and relevant hang dumps.
- **ac-03**: Final validation includes Release solution build, managed/native/determinism/parity/CLI/integration/AI scopes, changed-AC traceability and git diff --check.
- **ac-04**: Fresh live passes first prove RemoteControl default-off failure, then enabled capabilities/actions and every applicable Settings control. Match debug-attach metadata to the owned PID and refresh session/tree IDs after restart.
- **ac-05**: Live representative profiles are c64, c64c, ntsc and VIC-20 PAL/NTSC; automate all 14 C64 profiles, retain READY/geometry/clock/pacing/Warp evidence and verify exactly 28159 BASIC bytes free for pinned-ROM VIC-20 all-RAM.
- **ac-06**: Prove actual cartridge image/bank/preset/writeback and uIEC I/O. Stop only validation processes started by this work.
**Covered by:** FR: FR-COMPLETION-001; TEST: TEST-BASELINE-CORRECTNESS-001, TEST-COMPLETION-001, TEST-REMOTE-INTEGRATION-001, TEST-UI-COREBOUNDARY-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Add an exhaustive manifest covering every solution and relevant out-of-solution test project, every category excluded by ordinary Nuke Test and every required native/remote/AI workload.
- [ ] Implement Nuke CompletionTest as the supported entry point; gates retain exact commands, configuration, filters, counts, TRX, stdout/stderr, diagnostic logs and relevant hang dumps.
- [ ] Final validation includes Release solution build, managed/native/determinism/parity/CLI/integration/AI scopes, changed-AC traceability and git diff --check.
- [ ] Fresh live passes first prove RemoteControl default-off failure, then enabled capabilities/actions and every applicable Settings control. Match debug-attach metadata to the owned PID and refresh session/tree IDs after restart.
- [ ] Live representative profiles are c64, c64c, ntsc and VIC-20 PAL/NTSC; automate all 14 C64 profiles, retain READY/geometry/clock/pacing/Warp evidence and verify exactly 28159 BASIC bytes free for pinned-ROM VIC-20 all-RAM.
- [ ] Prove actual cartridge image/bank/preset/writeback and uIEC I/O. Stop only validation processes started by this work.

## TR-CORE-CYCLE-001

**Sub-Cycle Bus-Phase Accuracy Matching VICE x64sc Behavior** — Managed C64 PAL execution must preserve C64 bus, CPU, and VIC-II ordering semantics while optimizing RunFrame so performance changes do not alter cycle-observable behavior.
**Covered by:** FR: FR-PERF-RUNFRAME-001; TEST: TEST-PERF-RUNFRAME-001
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Lockstep and checkpoint suites continue to pass after RunFrame performance optimizations. (evidence: dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter FullyQualifiedName~Lockstep|FullyQualifiedName~Checkpoint => 333 passed, 0 failed, 0 skipped)
- [x] Focused BasicBus/C64MemoryMap/VideoRenderer/VideoSurface/SID tests pass with no failed or skipped tests. (evidence: focused gate => 182 passed, 0 failed, 0 skipped)

## TR-CORE-DET-001

**Bit-Exact Reproducibility Given Same Initial State and Inputs** — Given the same initial emulator state, ROM set, mounted media, inputs, and timing model, managed C64 PAL execution must produce deterministic CPU, bus, video, audio, interrupt, and memory-observable results.
**Covered by:** FR: FR-PERF-RUNFRAME-001; TEST: TEST-PERF-RUNFRAME-001
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Lockstep and checkpoint suites continue to pass after performance optimizations. (evidence: lockstep/checkpoint gate => 333 passed, 0 failed, 0 skipped)
- [x] Optimizations do not change public observable machine state signatures or behavior contracts. (evidence: PR #3 keeps public API signatures stable and correctness gates green)

## TR-CPU-TICK-001

**Per-instance CPU executed-cycle counter and target clock** — ICpu exposes ExecutedCycles and TargetClockHz per instance; MachineState carries a per-CPU breakdown; UpdatePerformanceCounters uses the primary CPU executed cycles divided by its target clock.
**Covered by:** FR: FR-CPUTICK-001; TEST: TEST-CPUTICK-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] ICpu exposes a per-instance ExecutedCycles counter and TargetClockHz.
- [ ] MachineState carries a per-CPU breakdown (id, executedCycles, targetHz) for host and peripheral CPUs.

## TR-CTX-001

**Resolve is pure/deterministic** — Resolve is pure/deterministic. Acceptance: verified by TEST-CTX-001.
**Covered by:** FR: FR-CTX-001, FR-CTX-002, FR-CTX-003, FR-CTX-004, FR-GAMEPAD-009, FR-SYSBTN-002, FR-SYSBTN-003, FR-SYSBTN-007; TEST: TEST-CTX-001, TEST-GAMEPAD-002, TEST-SYSBTN-DEVICE
**Status:** pending
Scope: layer-1+

## TR-CYCLE-001

**Sub-Cycle Bus-Phase Accuracy Matching VICE x64sc Behavior** — The emulation core shall operate at sub-cycle granularity, modeling the two phases of the system clock (PHI1 and PHI2) and the bus ownership arbitration between the CPU and VIC-II. This level of accuracy is required to correctly emulate cycle-exact tricks used by C64 demos and games. The reference implementation for correctness is VICE x64sc (the cycle-exact VICE variant).

## Acceptance Criteria
- All devices (CPU, VIC-II, CIA, SID) are ticked at half-cycle (bus phase) granularity.
- The VIC-II's DMA steal pattern (badlines, sprites) matches VICE x64sc cycle-by-cycle.
- CIA timer countdown occurs at the correct bus phase (PHI2 falling edge).
- CPU memory accesses occur only during PHI2 when AEC is asserted.
- The Lorenz test suite passes at 100% (same pass rate as VICE x64sc).
- The VICE cycle-exact test programs (e.g., those in the VICE test suite repository) produce identical output.
- Raster effects that depend on sub-cycle timing (FLI, AGSP, sprite stretching) work correctly.
**Covered by:** FR: FR-BACKFILL-VIDEO-001, FR-CIA-001, FR-CIA-002, FR-CIA-003, FR-CIA-004, FR-CIA-005, FR-CIA-006, FR-CIA-007, FR-CPU-001, FR-CPU-002, FR-CPU-003, FR-CPU-004, FR-CPU-005, FR-CRT-001, FR-CRT-002, FR-CRT-003, FR-CRT-004, FR-CRT-005, FR-DRV-001, FR-DRV-002, FR-DRV-003, FR-DRV-004, FR-DRV-005, FR-DRV-006, FR-INP-001, FR-INP-002, FR-INP-003, FR-INP-004, FR-INP-005, FR-INP-006, FR-MEM-001, FR-MEM-002, FR-MEM-003, FR-MEM-004, FR-MEM-005, FR-MEM-006, FR-TAP-001, FR-TAP-002, FR-TAP-003, FR-TAP-004, FR-TAP-005, FR-VIA-001, FR-VIA-002, FR-VIA-003, FR-VIA-004, FR-VIA-005, FR-VIC-001, FR-VIC-002, FR-VIC-003, FR-VIC-004, FR-VIC-005, FR-VIC-006, FR-VIC-007, FR-VIC-008, FR-VIC-009, FR-VIC-010, FR-VIC20-003, FR-VIC20-004; TEST: TEST-VIC-CHECKPOINT-001, TEST-VIC-RC-001, TEST-CIA-001, TEST-X64SC-LOCKSTEP-001, TEST-CPU-001, TEST-CRT-001, TEST-DRV-001, TEST-INPUT-001, TEST-MEM-001, TEST-TAP-001, TEST-VIA-001, TEST-VIC-001, TEST-VIC20-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] All devices (CPU, VIC-II, CIA, SID) are ticked at half-cycle (bus phase) granularity.
- [ ] The VIC-II's DMA steal pattern (badlines, sprites) matches VICE x64sc cycle-by-cycle.
- [ ] CIA timer countdown occurs at the correct bus phase (PHI2 falling edge).
- [ ] CPU memory accesses occur only during PHI2 when AEC is asserted.
- [ ] The Lorenz test suite passes at 100% (same pass rate as VICE x64sc).
- [ ] The VICE cycle-exact test programs (e.g., those in the VICE test suite repository) produce identical output.
- [ ] Raster effects that depend on sub-cycle timing (FLI, AGSP, sprite stretching) work correctly.

## TR-DEPS-202607-001

**Dependency currency policy (2026-07 upgrade wave)** — Every NuGet dependency tracks the highest STABLE version mutually compatible with net10.0 and all other dependencies; Directory.Packages.props (CPM) is the single version source for all projects including tests/ViceSharp.AiReview.Tests; all packages resolve from nuget.org when published there (vendored nuget-local feed retires once SharpNinja.aiUnit and SharpNinja.Avalonia.RemoteControl.* map to nuget.org). Prereleases excluded. Locked targets and ceiling evidence recorded in the approved 2026-07-08 upgrade plan.
**Covered by:** FR: FR-NATIVERESIDUE-001; TEST: TEST-BASELINE-CORRECTNESS-001, TEST-DEPS-202607-001, TEST-NATIVERESIDUE-001
**Status:** completed
Scope: layer-1+

## TR-DET-001

**Bit-Exact Reproducibility Given Same Initial State and Inputs** — Given the same initial machine state (snapshot) and the same sequence of input events (keyboard, joystick, timing), the emulator shall produce bit-exact identical output (video frames, audio samples, final machine state) across all runs, all platforms, and supported compilation modes (Debug and Release).

## Acceptance Criteria
- A "replay determinism" test loads a snapshot, replays 10 million cycles of recorded input, and compares the final state hash: the hash must be identical across 100 consecutive runs.
- The same test produces identical hashes on Windows x64, Linux x64, macOS ARM64, and Linux ARM64.
- The same test produces identical hashes for Debug and Release builds.
- Video frame checksums (CRC32 of raw pixel data) match for every frame between two runs of the same replay.
- Audio sample checksums match for every audio buffer between two runs of the same replay.
- Determinism is verified mechanically by the `Category=Determinism` bit-exact replay suites (run by the Nuke `DeterminismTest` target, e.g. `SidDeterminismTests`) plus the cycle-exact lockstep comparison gates against VICE `x64sc` and VSF baselines (Nuke `ParityTest` target).
**Covered by:** FR: FR-CFG-003, FR-CFG-005, FR-CFG-007, FR-PRF-001, FR-PRF-002, FR-PRF-003, FR-PRF-004, FR-PRF-005, FR-PRF-006, FR-PRF-007, FR-PRF-008; TEST: TEST-CFG-001, TEST-CLI-LAUNCHER-001, TEST-HOST-001, TEST-X64SC-LOCKSTEP-001, TEST-UIDROP-002, TEST-PRF-001
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Lockstep and checkpoint suites continue to pass after performance optimizations. (evidence: dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter FullyQualifiedName~Lockstep|FullyQualifiedName~Checkpoint => 333 passed, 0 failed, 0 skipped)
- [x] Optimizations do not change public observable machine state signatures or behavior contracts. (evidence: PR #3 keeps public API signatures stable and lockstep/checkpoint evidence green)

## TR-DRV-EDGE-001

**1541 drive motor 300,000-cycle ramp before rotation** — IecDrive.Tick() enforces 300,000-cycle motor ramp-up (300ms at 1MHz drive clock) before MotorRotationCycles advances. Motor off resets ramp. VICE drive/drive.c.
**Covered by:** FR: FR-ARCH-TRUEDRIVE-1541-002, FR-BACKFILL-MEDIA-001, FR-D71-001, FR-D81-001, FR-DRV1540-001, FR-DRV1541II-001, FR-DRVMODEL-001, FR-DRVMODEL-002, FR-VIC20-006; TEST: TEST-DRV-MOTOR-001, TEST-IEC-TIMING-001, TEST-TAPE-RAMP-001, TEST-TAPE-SENSE-001, TEST-DRV1571-LOCKSTEP-001, TEST-DRV1581-LOCKSTEP-001, TEST-DRV1540-LOCKSTEP-001, TEST-DRV1541II-LOCKSTEP-001, TEST-DRV-TYPE-001, TEST-VIC20-001
**Status:** closed
Scope: layer-1+

## TR-DRV-FDD-001

**MFM floppy drive model port** — MfmFloppyDrive.cs ports native fdd.c: init state write_protect=1, disk_change=1, rate=2, tracks=83 (:92-115); CRC-1021 table (:117-135); raw-track build (:435-557: 6250-byte track, D81 ISO layout gap2=22 gap3=35 sector_size=2 head_invert=1 image_sectors=40 per :192-209); flush-back parser (:265-433); Rotate (:559-576); Read/Write with the 0x100 sync-mark ninth bit (:626-680); SeekPulse clamped 0..83 with current_half_track=(track+1)*2 (:690-727). Buffers allocated once at image attach; Read/Write/Rotate zero-allocation. IMfmDiskImage span-based sector access implemented by D81Image.
**Status:** pending
Scope: layer-1+

## TR-DRVLIFE-001

**Drive attach/detach lifecycle** — Enabling a drive creates/starts the drive instance, registers it on the clock and connects it to the always-on IEC bus; disabling stops it, unregisters from the clock and disconnects from the bus.
**Covered by:** FR: FR-DRVLED-001, FR-DRVTRUE-001, FR-IECLOAD-001; TEST: TEST-DRVLED-001, TEST-DRVTRUE-001, TEST-DRVLIFE-001, TEST-IECELEC-001, TEST-IECLOAD-001
**Status:** pending
Scope: layer-1+

## TR-DRV-PACE-001

**Drive pacing via native 16.16 accumulator** — SystemCoordinator.SynchronizePeripheralSystemsToHost replaces the linear hostCycles*driveHz/hostRate target with native drivecpu.c:381-394 semantics: per peripheral (lastHostCycles, ulong cycleAccum); cycleAccum += syncFactor*delta; target += cycleAccum>>16; cycleAccum &= 0xFFFF; syncFactor = multiplier * floor(65536*driveHz/hostHz) per drivesync.c:41-57. The 1571 exposes a runtime clock multiplier (IDriveClockMultiplier, VIA1 PA bit 5) applied at the next sync; the 1581 runs at its fixed 2 MHz descriptor frequency. Reset re-anchors the accumulator to 0. Witness: managed pacer target equals native vice_drivecpu_get_clk at every 19,656-cycle sample.
**Status:** pending
Scope: layer-1+

## TR-DRV-WD1770-001

**WD1770 FDC statement-for-statement port** — New chip directory src/ViceSharp.Chips/Fdc/. Wd1770.cs ports native wd1770.c exactly: commands/status (:68-140), state (:142-167), lazy catch-up Execute() microcode (:204-817) that runs only at register access (no per-cycle tick), store/read/peek (:821-922), reset (:931-941), SetSide/SetMotor/DiskChange (:983-996), register decode & 3. Constants: clock_frequency=2 always, is1772=0, dden=0 (no runtime setter in native). Timing: PREPARE=48, BYTE_RATE=64 drive-cycles/byte, SETTLING=60000, STEP_RATE table per :54-63. The IRQ output stays internal state (native never wires it to the drive CPU). POCO state mirroring the native snapshot list; IStatefulDevice; zero-allocation hot ops.
**Status:** pending
Scope: layer-1+

## TR-GAMEPAD-001

**Portable input lib, zero Windows.*** — Portable net10.0 lib, zero Windows.* reference, IsAotCompatible/TreatWarningsAsErrors clean. Acceptance: verified by TEST-GAMEPAD-001 / harness build.
**Covered by:** FR: FR-GAMEPAD-009; TEST: TEST-CTX-001
**Status:** pending
Scope: layer-1+

## TR-GAMEPAD-002

**XboxJoystickMapper pure, hysteresis threaded** — XboxJoystickMapper pure, no static mutable state/IO/clock, hysteresis threaded. Acceptance: verified by TEST-GAMEPAD-001.
**Covered by:** FR: FR-GAMEPAD-001, FR-GAMEPAD-002, FR-GAMEPAD-003, FR-GAMEPAD-004, FR-GAMEPAD-005, FR-GAMEPAD-006; TEST: TEST-GAMEPAD-001
**Status:** pending
Scope: layer-1+

## TR-GAMEPAD-003

**Explicit Joystick1/Joystick2 only** — Explicit InputPort.Joystick1/Joystick2 only, never PrimaryJoystick. Acceptance: verified by TEST-GAMEPAD-001.
**Covered by:** FR: FR-GAMEPAD-007; TEST: TEST-GAMEPAD-001
**Status:** pending
Scope: layer-1+

## TR-GAMEPAD-004

**Device add/remove drained inside Tick** — Device add/remove drained on the emulation thread inside Tick. Acceptance: verified by TEST-GAMEPAD-001.
**Covered by:** FR: FR-GAMEPAD-008; TEST: TEST-GAMEPAD-001, TEST-GAMEPAD-002
**Status:** pending
Scope: layer-1+

## TR-GAMEPAD-005

**Delta emission only on change** — Delta emission only on change. Acceptance: verified by TEST-GAMEPAD-001.
**Status:** pending
Scope: layer-1+

## TR-GRPC-BOUNDARY-001

**Versioned gRPC Boundary Between Emulator Host and UI Clients** — ViceSharp shall separate emulator execution from host UI control through a versioned gRPC boundary. The host process owns the emulator core, devices, media state, persistence, and local render-source composition. Host UI control, media, session, input, snapshot, capture, and diagnostic operations consume generated gRPC clients or narrow gRPC-backed client abstractions and must not instantiate or mutate core emulator objects directly.

The in-process Avalonia renderer is the local rendering exception: a host-owned render surface may bind directly to a local emulator/frame source so frame presentation does not have to loop through gRPC. This exception is narrow and does not permit Avalonia ViewModels to reference runtime internals or concrete emulator devices. External or remote UIs still use the gRPC video service and stream APIs when direct in-process rendering is unavailable.

## Acceptance Criteria
- UI assemblies do not reference `ViceSharp.Core`, `ViceSharp.Chips`, or `ViceSharp.Architectures` directly.
- Host services expose enough control, remote output, input, media, state, and diagnostic operations to satisfy FR-HOST-001 through FR-HOST-005 and FR-UI-001.
- Contract compatibility checks detect deleted fields, reused field numbers, and incompatible service changes.
- An integration test can start the host, connect a remote UI client, receive a frame stream, submit input, request a screenshot, and shut down cleanly.
- Architecture tests enforce that only host/composition assemblies reference both concrete core assemblies and boundary service implementations.
- Streaming tests verify that client disconnects, slow frame consumers, and reconnects do not mutate emulator state.
- Release validation includes the host and at least one reference UI client.
- An in-process Avalonia rendering test can bind the host-owned render surface to a local frame source without introducing `ViceSharp.Core` or runtime-internal references into ViewModels.
**Covered by:** FR: FR-CFG-001, FR-CFG-004, FR-CFG-005, FR-CFG-006, FR-CFG-008, FR-HOST-001, FR-HOST-002, FR-HOST-003, FR-HOST-004, FR-HOST-005, FR-HOST-006, FR-UI-001, FR-UI-002, FR-UI-003, FR-UI-004, FR-UIDROP-002; TEST: TEST-CFG-001, TEST-CLI-LAUNCHER-001, TEST-HOST-001, TEST-X64SC-LOCKSTEP-001, TEST-UIDROP-002, TEST-GRPC-001, TEST-DRV-001, TEST-UI-001, TEST-UI-DIAG-001
**Status:** in_progress
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Status, media, command, monitor, and settings requests remain DTO-only across the host protocol boundary.
- [ ] The status DTO includes IEC activity telemetry without removing existing status fields.
- [ ] gRPC service adapters and the Avalonia gRPC client preserve IEC activity telemetry on status round trips.

## TR-HOST-DIAG-001

**Read-Only Diagnostics gRPC Contract** — ViceSharp shall expose a generated gRPC `DiagnosticsService` for local diagnostics tools. The service is read-only and must not create, remove, start, stop, reset, or otherwise mutate emulator sessions.
**Covered by:** FR: FR-HOST-DIAG-001; TEST: TEST-CLI-LAUNCHER-001, TEST-GRPC-001, TEST-HOST-001, TEST-HOST-DIAG-001, TEST-UI-DIAG-001, TEST-X64SC-LOCKSTEP-001
**Status:** pending
Scope: layer-1+

## TR-HOST-DIAG-002

**Current UI Session Tracking Bridge** — The desktop UI and host diagnostics state shall keep the current UI-bound session id synchronized so external diagnostics tools can attach to the same session that the visible app is using.
**Covered by:** FR: FR-HOST-DIAG-001, FR-UI-001, FR-UI-002, FR-UI-003, FR-UI-004; TEST: TEST-CLI-LAUNCHER-001, TEST-GRPC-001, TEST-HOST-001, TEST-HOST-DIAG-001, TEST-UI-DIAG-001, TEST-X64SC-LOCKSTEP-001, TEST-UI-001
**Status:** pending
Scope: layer-1+

## TR-HOST-DIAG-003

**Debug Attach File Lifecycle** — The desktop app shall publish deterministic local attach metadata at `%LOCALAPPDATA%\ViceSharp\debug-attach.json` for the running in-process host.
**Covered by:** FR: FR-HOST-DIAG-001, FR-UI-001, FR-UI-002, FR-UI-003, FR-UI-004; TEST: TEST-CLI-LAUNCHER-001, TEST-GRPC-001, TEST-HOST-001, TEST-HOST-DIAG-001, TEST-UI-DIAG-001, TEST-X64SC-LOCKSTEP-001, TEST-UI-001
**Status:** pending
Scope: layer-1+

## TR-HOST-DIAG-004

**Development-Scoped gRPC Reflection** — ViceSharp shall support gRPC reflection for local development diagnostics without enabling reflection unconditionally in release/runtime use.
**Covered by:** FR: FR-HOST-DIAG-001; TEST: TEST-CLI-LAUNCHER-001, TEST-GRPC-001, TEST-HOST-001, TEST-HOST-DIAG-001, TEST-UI-DIAG-001, TEST-X64SC-LOCKSTEP-001
**Status:** pending
Scope: layer-1+

## TR-HOST-PRG-001

**Host-owned PRG memory load over the gRPC emulator host contract** — PRG drop loading belongs to the emulator host, not Avalonia ViewModels. `EmulatorHost.LoadProgram` is the versioned RPC. The host reads the PRG (file path and/or payload), writes the payload through the session machine bus, decides BASIC-start from live TXTTAB (`$2B/$2C`), updates BASIC pointers only on that match, and queues `HostKeyboardAutomation` BASIC RUN (KERNAL keyboard buffer) when RUN is required. UI shells call `IHostProtocolClient.LoadProgramAsync` and must not poke Core devices. Shared chips stay machine-agnostic; TXTTAB is the BASIC-start oracle instead of a hardcoded family address table.

## Acceptance Criteria
- Avalonia ViewModels do not write emulator RAM or inject KERNAL keystrokes directly.
- `LoadProgram` is declared on the `EmulatorHost` proto service and mapped by `GrpcEmulatorHostService`.
- BASIC-start detection uses live TXTTAB (`$2B/$2C`) for the current machine and VIC-20 memory config, not a C64-only `$0801` constant.
- RUN uses the existing host keyboard-buffer automation after READY, scanning the current KERNAL HIBASE screen page (`$0288`) so VIC-20 unexpanded `$1E00` and +8K `$1000` are found as well as C64 `$0400`.
**Covered by:** FR: FR-CFG-005, FR-UIDROP-002; TEST: TEST-CFG-001, TEST-CLI-LAUNCHER-001, TEST-HOST-001, TEST-UIDROP-002, TEST-X64SC-LOCKSTEP-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Avalonia ViewModels do not write emulator RAM or inject KERNAL keystrokes directly.
- [ ] `LoadProgram` is declared on the `EmulatorHost` proto service and mapped by `GrpcEmulatorHostService`.
- [ ] BASIC-start detection uses live TXTTAB (`$2B/$2C`) for the current machine and VIC-20 memory config, not a C64-only `$0801` constant.
- [ ] RUN uses the existing host keyboard-buffer automation after READY, scanning the current KERNAL HIBASE screen page (`$0288`) so VIC-20 unexpanded `$1E00` and +8K `$1000` are found as well as C64 `$0400`.

## TR-HOST-STATUS-001

**Measured Emulator Runtime Telemetry** — ViceSharp host status shall distinguish requested throttle settings from measured emulation output and measured emulated clock speed.

## Acceptance Criteria
- Status responses include nominal clock Hz, effective clock Hz, effective clock percent, limiter rate percent, measured FPS, frame count, cycle, PC, power state, and run state.
- Tests can verify limiter target remains stable while effective clock and FPS vary with execution.
- Paused sessions report stable cycle/frame counters and paused run state.
**Covered by:** FR: FR-CFG-008, FR-DRV-005, FR-HOST-006, FR-UI-002; TEST: TEST-CFG-001, TEST-CLI-LAUNCHER-001, TEST-HOST-001, TEST-X64SC-LOCKSTEP-001, TEST-DRV-001, TEST-GRPC-001, TEST-UI-001, TEST-UI-DIAG-001
**Status:** in_progress
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Status responses include existing runtime telemetry and IEC activity fields.
- [ ] IEC activity is derived from authoritative IEC line changes or an equivalent host-owned bus signal monitor.
- [ ] Status polling does not mutate machine, drive, or bus state.

## TR-IECDECODE-001

**IEC protocol decoder** — Pure state machine over trace samples emitting decoded IEC events: ATN command frame, LISTEN/TALK + device, OPEN/secondary/channel, data bytes with EOI, turnaround, timeout/error.
**Covered by:** FR: FR-IECMON-001; TEST: TEST-IECDECODE-001, TEST-IECMON-001, TEST-IECTRACE-001
**Status:** pending
Scope: layer-1+

## TR-IEC-EDGE-001

**IEC bus ATN-response within 985-cycle spec window** — IecBus.Tick() asserts CLK and DATA low within 985 cycles (Tat=1ms at PAL 985,248Hz) of ATN falling edge. Releases both on ATN rising edge. VICE iecbus/iecbus.c:247-266.
**Covered by:** FR: FR-ARCH-TRUEDRIVE-1541-002; TEST: TEST-DRV-MOTOR-001, TEST-IEC-TIMING-001
**Status:** closed
Scope: layer-1+

## TR-IECELEC-001

**Faithful IEC serial electrical model with native parity** — CIA2 reads the live open-collector IEC bus through the C64 inverting buffers; idle DATA-low is supplied by the true-drive 1541 holding DATA. C64GS (no IEC) keeps disconnected->low. Idle reads match native ($47 normal C64 / $07 C64GS) and native CIA2-IO lockstep stays green.
**Covered by:** FR: FR-DRVTRUE-001, FR-IECLOAD-001; TEST: TEST-DRVTRUE-001, TEST-DRVLIFE-001, TEST-IECELEC-001, TEST-IECLOAD-001
**Status:** pending
Scope: layer-1+

## TR-IECSPY-001

**IInterSystemBus.Snapshot API** — IInterSystemBus exposes Snapshot() returning BusSnapshot (per-line level + pullers, talking endpoints, attached endpoints). Read-only. DONE.
**Covered by:** FR: FR-IECSPY-001; TEST: TEST-IECSPY-001
**Status:** pending
Scope: layer-1+

## TR-IECTRACE-001

**IEC trace recorder (edge + step marks)** — IecBusTraceRecorder subscribes to bus.LineChanged, cycle-stamps each edge from SystemClock plus a sample per step boundary, into a bounded ring; re-derives deterministically after rewind; inactive unless the monitor is open.
**Covered by:** FR: FR-IECMON-001; TEST: TEST-IECDECODE-001, TEST-IECMON-001, TEST-IECTRACE-001
**Status:** pending
Scope: layer-1+

## TR-INPROC-001

**Portable lib, no ASP.NET FrameworkReference** — Portable net10.0 lib, IsAotCompatible, no ASP.NET FrameworkReference. Acceptance: verified by TEST-INPROC-001.
**Covered by:** FR: FR-INPROC-001; TEST: TEST-INPROC-001
**Status:** pending
Scope: layer-1+

## TR-INPROC-002

**XboxManagedFrameGate zero P/Invoke, idempotent** — XboxManagedFrameGate zero P/Invoke, no aux timer thread, idempotent Start/Stop. Acceptance: verified by TEST-INPROC-003.
**Status:** pending
Scope: layer-1+

## TR-INPROC-003

**Pacing does not affect emulated cycle results** — Pacing does not affect emulated cycle results; deterministic advance bit-exact. Acceptance: verified by TEST-INPROC-002.
**Covered by:** FR: FR-INPROC-002, FR-INPROC-003; TEST: TEST-INPROC-002
**Status:** pending
Scope: layer-1+

## TR-INPROC-004

**Single unpinned pump worker** — Single unpinned pump worker, VICESHARP_EMU_CPU unset -> AppliedWorkerAffinityMask==null. Acceptance: verified by TEST-INPROC-002.
**Status:** pending
Scope: layer-1+

## TR-INPROC-005

**One managed pacing gate; audio decoupled ring** — One managed pacing gate; audio decoupled via ring buffer, SubmitSamples non-blocking, Pause() unparks the worker (supersedes TR-XPACE; single gate). Acceptance: verified by TEST-INPROC-003, TEST-XAUDIO-004.
**Status:** pending
Scope: layer-1+

## TR-INPUT-VKM-001

**VICE VKM Parser and Selected Map Resolver** — ViceSharp machine keyboard input shall resolve normalized host key events through a selected machine-specific VICE keymap before updating keyboard matrix state.

## Acceptance Criteria
- Built-in and custom VKM maps produce deterministic key-to-matrix mappings.
- Invalid maps return diagnostics without replacing the selected map.
- Host input service tests prove selected map entries affect C64 keyboard matrix state.
- The parser and resolver are usable without Avalonia dependencies.
**Covered by:** FR: FR-INP-006; TEST: TEST-INPUT-001, TEST-X64SC-LOCKSTEP-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Built-in and custom VKM maps produce deterministic key-to-matrix mappings.
- [ ] Invalid maps return diagnostics without replacing the selected map.
- [ ] Host input service tests prove selected map entries affect C64 keyboard matrix state.
- [ ] The parser and resolver are usable without Avalonia dependencies.

## TR-INSTALL-ASSOC-001

**HKLM file association registry for MSI** — Per-machine HKLM Software\Classes ProgIds, DefaultIcon, shell\open\command with quoted exe and %1, OpenWithProgids, Capabilities FileAssociations, and RegisteredApplications. Do not claim .bin or .rom.
**Covered by:** FR: FR-INSTALL-001; TEST: TEST-INSTALL-001
**Status:** pending
Scope: layer-1+

## TR-LIB-001

**Emulator Core as a Reusable Library with UI Shells as Thin Consumers** — The ViceSharp emulator core shall be packaged as a standalone .NET library (NuGet package) with no dependencies on any UI framework, windowing system, or platform-specific API. UI applications (desktop, web, headless test harness, embedded) consume the library and provide their own rendering, audio output, and input handling through well-defined abstractions.
**Covered by:** FR: FR-CFG-002, FR-CFG-006, FR-MON-001, FR-MON-002, FR-MON-003, FR-MON-004, FR-MON-005, FR-MON-006; TEST: TEST-CFG-001, TEST-CLI-LAUNCHER-001, TEST-HOST-001, TEST-X64SC-LOCKSTEP-001, TEST-MON-001
**Status:** pending
Scope: layer-1+

## TR-LOCKSTEP-VSF-001

**Native VICE VSF Snapshot Lockstep** — Productionize and complete the existing managed VSF resume bridge so native VICE .vsf is the canonical snapshot implementation for supported x64sc C64 and xvic VIC-20 product paths. Native VICE decodes and writes version-specific modules; complete immutable state bundles transfer between the matching native instance and managed adapters with transactional apply and deterministic continuation.
**Covered by:** FR: FR-SNP-005, FR-VSFLOCKSTEP-001; TEST: TEST-SNP-002, TEST-VSFLOCKSTEP-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Native .vsf load and save use the matching pinned VICE snapshot implementation through IViceNative.ReadSnapshot and WriteSnapshot. The managed envelope walk accepts current 58-byte and pinned-reader-compatible legacy 37-byte headers, reads each 22-byte module header with a little-endian inclusive total length, reaches exact EOF, and preserves machine identity, container/module versions, byte order, framing, and restore order without parsing version-specific payloads.
- [ ] The existing managed C64 staging path moves from the test harness into production and is completed, and an equivalent VIC-20 adapter is added. Reusable state contracts remain machine-agnostic while C64/x64sc and VIC-20/xvic wiring stays in the owning adapters.
- [ ] Snapshot application uses the same non-blocking try-create ABI in both native DLLs, treats C64 and xvic as independent per-DLL singleton leases, bounds acquisition to ten seconds with cancellation, completes envelope validation plus native read and immutable state export before managed mutation, revalidates session generation/profile/media under the lock, and rolls back completely on apply failure; rollback failure stops and faults the session until recreation.
- [ ] Snapshot state preserves every cycle-visible CPU architectural and in-flight pipeline, bus, banking, raster/video pipeline, interrupt, timer, serial, oscillator/envelope/filter, peripheral, media, drive, and cartridge value required for deterministic continuation. Resetting in-flight CPU state is not accepted as full import.
- [ ] Native x64sc and xvic remain the behavioral/file oracles. Every normal supported snapshot API and Avalonia win-x64 file, quick-slot, and ten-named-slot command invokes native VSF by default; hosts without the packaged capability return unavailable without proprietary fallback. UWP/Xbox implementation is out of scope and historical requirements are not retired.
- [ ] The two proprietary readers are isolated behind the explicit protobuf ImportLegacySnapshot(RestoreSnapshotRequest) returns (RestoreSnapshotResponse) operation and matching ISnapshotService.ImportLegacySnapshotAsync. Normal restore rejects those formats with a migration error, no normal save emits them, replay remains separate, and CPU-plus-RAM injection or native-only shim success is insufficient.
- [ ] The supported win-x64 publish and MSI build both shims, recursively verify their PE imports, stage only the proven loose non-system closure after DotNetPublish and before WiX harvest, include VICE license/source-offer material and no ROMs, and pass installed default C64 and VIC-20 native-VSF smoke. Unsupported RIDs report unavailable.

## TR-MCP-TRIAGE-004

**TR-MCP-TRIAGE-004** — Placeholder requirement backfilled for TODO link TR-MCP-TRIAGE-004.
**Status:** pending
Scope: layer-1+

## TR-MED-FFMPEG-001

**FfmpegVideoRecorder over two loopback TCP sockets** — External ffmpeg process fed raw BGRA + s16le via BackgroundByteWriter; mirrors VICE ffmpegexedrv. Pre-connect audio buffered + flushed.
**Covered by:** FR: FR-MED-004; TEST: TEST-MED-001, TEST-MED-004
**Status:** pending
Scope: layer-1+

## TR-MEDIA-001

**External FFmpeg Process Integration with Multiple Format Support** — Muxed video+audio encoding shall use an external `ffmpeg` executable driven as a child process, mirroring VICE's `ffmpegexedrv`. ViceSharp links no FFmpeg libraries and performs no P/Invoke into libav*; the emulator streams raw frames and PCM to the ffmpeg process, and ffmpeg performs all codec and container work. Audio-only (WAV) and image/frame-sequence capture are implemented with managed encoders and do not require ffmpeg.
**Covered by:** FR: FR-MED-001, FR-MED-002, FR-MED-003, FR-MED-004, FR-MED-005; TEST: TEST-MED-001, TEST-MED-002, TEST-MED-003, TEST-MED-004
**Status:** pending
Scope: layer-1+

## TR-MED-SOUND-001

**CaptureAudioTap + WavAudioRecorder** — Runtime-swappable tap in the SID->output path feeds a 16-bit PCM WAV recorder whose file writes run off the worker.
**Covered by:** FR: FR-MED-003; TEST: TEST-MED-001, TEST-MED-003
**Status:** pending
Scope: layer-1+

## TR-MED-VIDEO-001

**BMP-sequence sink with all/unique dedup, off-worker writes** — FrameSequenceCapture writes numbered 24-bit BMPs via a background queue; unique mode skips byte-identical consecutive frames.
**Covered by:** FR: FR-MED-002; TEST: TEST-MED-001, TEST-MED-002
**Status:** pending
Scope: layer-1+

## TR-MVVM-001

**Strict MVVM Separation -- ViewModels Reference Abstractions Only, Views Contain Zero Logic** — All UI shells shall follow a strict Model-View-ViewModel (MVVM) pattern. ViewModels shall depend only on `ViceSharp.Abstractions` (never on `ViceSharp.Core` directly or any UI framework). Views shall contain zero business logic -- they bind to ViewModel properties and commands exclusively. This ensures that ViewModels are testable without a UI framework and that the UI layer is trivially replaceable.

## Acceptance Criteria
- The `ViceSharp.Avalonia` project references `ViceSharp.Protocol` and `ViceSharp.Host` but no runtime projects (Core, Chips, Architectures, RomFetch), verified by `AvaloniaBoundaryTests.AvaloniaProject_ReferencesProtocolAndHostCompositionButNotRuntimeProjects`.
- Avalonia source (including all ViewModels) contains no references to runtime internals (`ViceSharp.Core`, `ViceSharp.Chips`, `ViceSharp.Architectures`, `ViceSharp.RomFetch`, `IMachine`, `IVideoChip`, `ArchitectureBuilder`), verified by `AvaloniaBoundaryTests.AvaloniaSources_DoNotReferenceRuntimeInternals`.
- All ViewModel public properties and commands are exercised by unit tests that do not require a UI framework.
- View code-behind files contain fewer than 20 lines of code each (excluding auto-generated code).
- Source-level boundary tests (`AvaloniaBoundaryTests`) enforce the dependency rules and fail the test gate on violations.
- Swapping the UI framework (e.g., replacing Avalonia views with MAUI views) requires changes only in the View layer.
- A remote host UI can be tested with mocked host client facades and without starting `ViceSharp.Core`.
- The local Avalonia render surface can be tested as a host/composition concern without adding core, chip, or architecture references to ViewModels.
**Covered by:** FR: FR-ROMM-AVUI-001, FR-UI-001, FR-UIDROP-002; TEST: TEST-ROMM-AVUI-001, TEST-GRPC-001, TEST-UI-001, TEST-UI-DIAG-001, TEST-UIDROP-002
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The `ViceSharp.Avalonia` project references `ViceSharp.Protocol` and `ViceSharp.Host` but no runtime projects (Core, Chips, Architectures, RomFetch), verified by `AvaloniaBoundaryTests.AvaloniaProject_ReferencesProtocolAndHostCompositionButNotRuntimeProjects`.
- [ ] Avalonia source (including all ViewModels) contains no references to runtime internals (`ViceSharp.Core`, `ViceSharp.Chips`, `ViceSharp.Architectures`, `ViceSharp.RomFetch`, `IMachine`, `IVideoChip`, `ArchitectureBuilder`), verified by `AvaloniaBoundaryTests.AvaloniaSources_DoNotReferenceRuntimeInternals`.
- [ ] All ViewModel public properties and commands are exercised by unit tests that do not require a UI framework.
- [ ] View code-behind files contain fewer than 20 lines of code each (excluding auto-generated code).
- [ ] Source-level boundary tests (`AvaloniaBoundaryTests`) enforce the dependency rules and fail the test gate on violations.
- [ ] Swapping the UI framework (e.g., replacing Avalonia views with MAUI views) requires changes only in the View layer.
- [ ] A remote host UI can be tested with mocked host client facades and without starting `ViceSharp.Core`.
- [ ] The local Avalonia render surface can be tested as a host/composition concern without adding core, chip, or architecture references to ViewModels.

## TR-NATIVE-LIFECYCLE-001

**Bounded and isolated native oracle lifecycle** — Oracle sound state is separate from live mixer state; no reset memset clears playback timing.
Use absolute monotonic deadlines for create/step 5000 ms and stop 2000 ms; remove infinite waits and propagate native failure to managed callers.
Poison belongs to the shared loaded native library, not only one wrapper instance; retain memory if its worker is still alive.
Capture all required modified native source in the supported patch against the pinned clean native gitlink; retain patch/build and loaded-DLL SHA256 receipts.
Audio comparisons require identical sample counts and complete PCM sequences; truncated Math.Min comparisons cannot establish parity.

## Acceptance Criteria
- **ac-01**: Oracle sound state is separate from live mixer state; no reset memset clears playback timing.
- **ac-02**: Use absolute monotonic deadlines for create/step 5000 ms and stop 2000 ms; remove infinite waits and propagate native failure to managed callers.
- **ac-03**: Poison belongs to the shared loaded native library, not only one wrapper instance; retain memory if its worker is still alive.
- **ac-04**: Capture all required modified native source in the supported patch against the pinned clean native gitlink; retain patch/build and loaded-DLL SHA256 receipts.
- **ac-05**: Audio comparisons require identical sample counts and complete PCM sequences; truncated Math.Min comparisons cannot establish parity.
**Covered by:** FR: FR-NATIVE-LIFECYCLE-001, FR-VIC20-SOUND-001; TEST: TEST-NATIVE-LIFECYCLE-001, TEST-VIC20-SOUND-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Oracle sound state is separate from live mixer state; no reset memset clears playback timing.
- [ ] Use absolute monotonic deadlines for create/step 5000 ms and stop 2000 ms; remove infinite waits and propagate native failure to managed callers.
- [ ] Poison belongs to the shared loaded native library, not only one wrapper instance; retain memory if its worker is still alive.
- [ ] Capture all required modified native source in the supported patch against the pinned clean native gitlink; retain patch/build and loaded-DLL SHA256 receipts.
- [ ] Audio comparisons require identical sample counts and complete PCM sequences; truncated Math.Min comparisons cannot establish parity.

## TR-PACESEL-STRAT-001

**Live-switchable pacing strategy plumbing** — EmulationGateStrategies provides canonical ids + gate factory. EmulationPumpService.SetStrategy swaps the gate on the worker thread (no restart). LimiterSettingsDto.PacingStrategy flows through proto/gRPC, SettingsServiceHost applies it live to the pump and round-trips it, and the UI exposes a dropdown.
**Covered by:** FR: FR-PACESEL-001; TEST: TEST-PACESEL-001
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] EmulationGateStrategies.CreateGate/Normalize/DisplayName map ids to gates, defaulting to Semaphore
- [x] SetStrategy swaps the gate live; SettingsServiceHost applies and round-trips PacingStrategy

## TR-PERF-ALLOC-001

**Zero Managed Allocations Per RunFrame Hot Path** — Sustained managed C64 PAL RunFrame execution must avoid per-frame managed allocations so 50 Hz operation does not create GC pressure.
**Covered by:** FR: FR-PERF-RUNFRAME-001; TEST: TEST-PERF-RUNFRAME-001
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] The measured C64 PAL RunFrame loop reports zero current-thread allocations over the measured frame window. (evidence: RunFramePerfProbe 60 600: allocated=0 bytes)
- [x] BenchmarkDotNet run reports no managed allocation for C64PalRunFrameBenchmark. (evidence: BenchmarkDotNet C64PalRunFrameBenchmark: Allocated column reported no managed allocation)

## TR-PERF-AOT-001

**NativeAOT-Compatible RunFrame Hot Path** — RunFrame hot-path implementation must remain compatible with NativeAOT constraints: no new reflection, expression trees, dynamic code generation, or LINQ allocations on the hot call graph.
**Covered by:** FR: FR-PERF-RUNFRAME-001; TEST: TEST-PERF-RUNFRAME-001
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Performance optimizations introduce no reflection, expression trees, or dynamic code generation in the RunFrame call graph. (evidence: Code review of PR #3 optimized BasicBus and VideoRenderer with direct/static paths only)
- [x] Performance optimizations avoid LINQ and closure allocation in the measured RunFrame hot path. (evidence: PR #3 uses loops and span fills in hot path; RunFramePerfProbe reports allocated=0 bytes)

## TR-PLAT-001

**Cross-Platform Support for Windows, Linux, macOS on x64 and ARM64** — ViceSharp shall run on Windows, Linux, and macOS on both x64 and ARM64 architectures. The emulation core library shall be platform-agnostic. Platform-specific code (audio output, input handling, window management) shall be isolated behind abstractions with per-platform implementations.
**Covered by:** FR: FR-CFG-006; TEST: TEST-CFG-001, TEST-CLI-LAUNCHER-001, TEST-HOST-001, TEST-X64SC-LOCKSTEP-001
**Status:** pending
Scope: layer-1+

## TR-PUBSUB-001

**<50ns Publish, <100ns Deliver, 0 Allocations Per Frame** — The internal event/message bus used for inter-device communication (e.g., VIC-II asserting IRQ on the CPU, CIA timer underflow signaling the SID) shall achieve sub-100ns latency with zero managed heap allocations. This pub/sub system is the backbone of the emulation's device interconnect and fires hundreds of thousands of times per second.
**Status:** pending
Scope: layer-1+

## TR-PUBSUB-PERF-001

**High-Performance Zero-Allocation Pub/Sub** — ViceSharp shall implement the internal Pub/Sub interconnect with fixed-capacity message storage, 64-byte inline payloads, a MessageKind discriminant, preallocated subscriber route arrays, synchronous publish/delivery, no boxing, and zero managed heap allocation on the steady-state hot path.
**Covered by:** FR: FR-PUBSUB-001; TEST: TEST-PUBSUB-001
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Publishing one million typed messages to one subscriber is below 50ns per publish operation. (evidence: dotnet run -c Release --project tests/ViceSharp.Benchmarks -- --pubsub-probe 1000000 => publish-one=43.78ns)
- [x] Publishing one million typed messages to three subscribers is below 100ns per publish/delivery operation. (evidence: dotnet run -c Release --project tests/ViceSharp.Benchmarks -- --pubsub-probe 1000000 => publish-three=58.14ns)
- [x] Message pool rent/return is below 20ns and uses fixed-capacity slot handles. (evidence: src/ViceSharp.Core/LockFreePubSub.cs; PubSubPerfProbe => pool-rent-return=16.80ns)
- [x] PayloadArena allocation is below 10ns and supports frame reset semantics. (evidence: src/ViceSharp.Core/LockFreePubSub.cs; PubSubPerfProbe => arena-alloc=3.20ns)
- [x] The typed publish hot path performs zero managed allocations after warmup. (evidence: tests/ViceSharp.TestHarness/LockFreePubSubTests.cs Publish_TypedPayload_HotPathDoesNotAllocate; PubSubPerfProbe => allocated=0 bytes)
- [x] Default message pool capacity is 8192 slots and does not resize during emulation. (evidence: src/ViceSharp.Core/LockFreePubSub.cs DefaultMessageCapacity = 8192 and LockFreeMessagePool(capacity = 8192))
- [x] Delivery order is deterministic in registration order and preserved when subscriber route arrays grow. (evidence: tests/ViceSharp.TestHarness/LockFreePubSubTests.cs Publish_DeliversSubscribersInRegistrationOrder and Subscribe_WhenRouteSubscriberArrayGrows_PreservesDeliveryOrder)

## TR-QA-TESTSILENCE-001

**Audio-producing tests self-mute the host process Windows audio session** — Test classes that drive the real WinMmAudioBackend (SidAudioWiring, WarpAudioDiscard, DemoWorkloadSpeedDiag, WinMmAudioThroughputDiag, ClockThroughputBenchmark) mute the current process Windows audio session (WASAPI ISimpleAudioVolume on the default session) for the duration of the class and restore the prior mute state on dispose, keeping a developer box silent during a run. Implemented with a raw function-pointer vtable COM helper, AOT and trim clean (no ComImport or reflection), that no-ops on non-Windows or headless hosts. Test infrastructure only; it changes nothing under test.
**Status:** pending
Scope: layer-1+

## TR-REMOTECTRL-001

**Embeddable RemoteControl server integration + vendored feed** — ViceSharp.Avalonia references SharpNinja.Avalonia.RemoteControl.Server (0.7.3), vendored into a repo-local NuGet feed (nuget-local/ + NuGet.config package source mapping) so restore is reproducible on Azure DevOps/CI without the sibling source repo. The Avalonia App builds a DI ServiceCollection, calls AddAvaloniaRemoteControl, registers an IRemoteControlRootProvider returning the desktop MainWindow, and attaches the host to the classic-desktop lifetime. Startup is gated behind VICESHARP_REMOTECONTROL_ENABLE and fails closed without a bearer token. Requires Avalonia >= 12.0.3 and Grpc >= 2.80.0 (central package versions bumped accordingly).
**Status:** pending
Scope: layer-1+

## TR-REMOTECTRL-CLI-001

**Portable RemoteControl gRPC command line** — Use published SharpNinja Avalonia RemoteControl Protocol 0.7.4 matching the embedded Server 0.7.4, its generated gRPC client and the existing Grpc.Net.Client dependency.
Own and dispose the channel; propagate bearer metadata and cancellation with bounded calls.
Add CLI and test projects to ViceSharp.slnx and the exhaustive CompletionTest manifest.
Calculate effective visibility through ancestors and validate malformed references/cycles rather than assuming a matching node is actionable.
Validate frame data before writing PNG output and retain machine-readable response/error contracts.

## Acceptance Criteria
- **ac-01**: Use published SharpNinja Avalonia RemoteControl Protocol 0.7.4 matching the embedded Server 0.7.4, its generated gRPC client and the existing Grpc.Net.Client dependency.
- **ac-02**: Own and dispose the channel; propagate bearer metadata and cancellation with bounded calls.
- **ac-03**: Add CLI and test projects to ViceSharp.slnx and the exhaustive CompletionTest manifest.
- **ac-04**: Calculate effective visibility through ancestors and validate malformed references/cycles rather than assuming a matching node is actionable.
- **ac-05**: Validate frame data before writing PNG output and retain machine-readable response/error contracts.
**Covered by:** FR: FR-REMOTECTRL-001, FR-REMOTECTRL-CLI-001; TEST: TEST-COMPLETION-001, TEST-REMOTECTRL-001, TEST-REMOTECTRL-CLI-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Use published SharpNinja Avalonia RemoteControl Protocol 0.7.4 matching the embedded Server 0.7.4, its generated gRPC client and the existing Grpc.Net.Client dependency.
- [ ] Own and dispose the channel; propagate bearer metadata and cancellation with bounded calls.
- [ ] Add CLI and test projects to ViceSharp.slnx and the exhaustive CompletionTest manifest.
- [ ] Calculate effective visibility through ancestors and validate malformed references/cycles rather than assuming a matching node is actionable.
- [ ] Validate frame data before writing PNG output and retain machine-readable response/error contracts.

## TR-REMOTECTRL-SERVER-001

**Embeddable RemoteControl 0.7.4 integration** — Reference SharpNinja.Avalonia.RemoteControl.Server 0.7.4, with the CLI using matching published Protocol 0.7.4; restore/build is reproducible without sibling source paths through the supported package feeds.
Use AddAvaloniaRemoteControl plus a DI IRemoteControlRootProvider for the live desktop MainWindow, and bind the host lifecycle to the classic-desktop lifetime.
Preserve loopback/bearer/default-disabled behavior, deny-by-default action/frame gates and compatible central Avalonia/Grpc dependency constraints.

## Acceptance Criteria
- **ac-01**: Reference SharpNinja.Avalonia.RemoteControl.Server 0.7.4, with the CLI using matching published Protocol 0.7.4; restore/build is reproducible without sibling source paths through the supported package feeds.
- **ac-02**: Use AddAvaloniaRemoteControl plus a DI IRemoteControlRootProvider for the live desktop MainWindow, and bind the host lifecycle to the classic-desktop lifetime.
- **ac-03**: Preserve loopback/bearer/default-disabled behavior, deny-by-default action/frame gates and compatible central Avalonia/Grpc dependency constraints.
**Covered by:** FR: FR-REMOTECTRL-001; TEST: TEST-COMPLETION-001, TEST-REMOTECTRL-001, TEST-REMOTECTRL-CLI-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Reference SharpNinja.Avalonia.RemoteControl.Server 0.7.4, with the CLI using matching published Protocol 0.7.4; restore/build is reproducible without sibling source paths through the supported package feeds.
- [ ] Use AddAvaloniaRemoteControl plus a DI IRemoteControlRootProvider for the live desktop MainWindow, and bind the host lifecycle to the classic-desktop lifetime.
- [ ] Preserve loopback/bearer/default-disabled behavior, deny-by-default action/frame gates and compatible central Avalonia/Grpc dependency constraints.

## TR-REMOTE-INTEGRATION-001

**Existing RomM and CSDb integration gate** — Use existing RomM http://192.168.0.148:8080/ and CSDb bridge http://192.168.0.148:8090/; do not substitute or start the stopped local stack.
Provision the existing per-user bridge connection flow with a dedicated vicesharp-e2e identity; keep service administrative credentials on the service.
Fixture-owned HttpClient supports optional VICESHARP_CSDB_BRIDGE_API_KEY via X-Api-Key for both connection and search requests.
Authenticated preflight requires access and at least one downloadable launchable C64 D64 or CRT seed before running seven integration tests.

## Acceptance Criteria
- **ac-01**: Use existing RomM http://192.168.0.148:8080/ and CSDb bridge http://192.168.0.148:8090/; do not substitute or start the stopped local stack.
- **ac-02**: Provision the existing per-user bridge connection flow with a dedicated vicesharp-e2e identity; keep service administrative credentials on the service.
- **ac-03**: Fixture-owned HttpClient supports optional VICESHARP_CSDB_BRIDGE_API_KEY via X-Api-Key for both connection and search requests.
- **ac-04**: Authenticated preflight requires access and at least one downloadable launchable C64 D64 or CRT seed before running seven integration tests.
**Covered by:** FR: FR-COMPLETION-001, FR-CSDB-001, FR-ROMM-BROWSE-001, FR-ROMM-COLLECT-001, FR-ROMM-CONN-001, FR-ROMM-LAUNCH-001; TEST: TEST-BASELINE-CORRECTNESS-001, TEST-COMPLETION-001, TEST-REMOTE-INTEGRATION-001, TEST-UI-COREBOUNDARY-001, TEST-ROMM-SEC-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Use existing RomM http://192.168.0.148:8080/ and CSDb bridge http://192.168.0.148:8090/; do not substitute or start the stopped local stack.
- [ ] Provision the existing per-user bridge connection flow with a dedicated vicesharp-e2e identity; keep service administrative credentials on the service.
- [ ] Fixture-owned HttpClient supports optional VICESHARP_CSDB_BRIDGE_API_KEY via X-Api-Key for both connection and search requests.
- [ ] Authenticated preflight requires access and at least one downloadable launchable C64 D64 or CRT seed before running seven integration tests.

## TR-REVEXEC-001

**Frame-snapshot ring reverse execution** — A frame-granular snapshot ring keyed by cycle backs RewindCycle/RewindFrame: restore nearest snapshot <= target, re-run deterministically to target. Replaces the NotImplemented host stubs. Bounded memory.
**Covered by:** FR: FR-REVEXEC-001; TEST: TEST-REVEXEC-001, TEST-SNAPFULL-001
**Status:** pending
Scope: layer-1+

## TR-ROMM-BOUNDARY-001

**Adapter isolates HTTP from the VM library** — PLAN-ROMM-001. All HTTP/RomM.Client access lives in ViceSharp.RomM; ViceSharp.Library.ViewModels has no System.Net.Http reference and references only ViceSharp.Protocol. Verified by LibraryViewModelsBoundaryTests.NoHttp (covers AC-PKG-03).
**Covered by:** FR: FR-ROMM-DETAIL-001; TEST: TEST-ROMM-DETAIL-001
**Status:** pending
Scope: layer-1+

## TR-ROMM-JSON-001

**Source-gen JSON for all new adapter DTOs** — PLAN-ROMM-001. 100% of new adapter DTOs (Collections/Search/cover/char_index/pairing) serialize and deserialize reflection-free via RomMJsonContext (satisfies IsAotCompatible/EnableTrimAnalyzer/TreatWarningsAsErrors). Verified by RomMJsonContextTests.AllTypes_SourceGen.
**Status:** pending
Scope: layer-1+

## TR-ROMM-NUGET-001

**nuget.org-only package resolution** — PLAN-ROMM-001. RomM.Client 1.0.0 and RomM.Client.Csdb 1.0.0 resolve from nuget.org only; NuGet.config stays clear + nuget.org; packages pinned in Directory.Packages.props (CPM). Verified by NuGetConfigTests + DirectoryPackagesTests.RommPinned.
**Covered by:** FR: FR-ROMM-PKG-001; TEST: TEST-ROMM-PKG-001
**Status:** pending
Scope: layer-1+

## TR-ROMM-SEC-001

**Keep RomM bearer tokens and downloaded content inside trusted boundaries** — Public covers use an anonymous client; bearer auth is restricted to server-relative RomM cover paths. ROM filenames and resolved cache destinations are contained, downloaded through a temporary file, expected-size validated, and atomically published. Windows file-backed tokens use current-user DPAPI with atomic legacy-plaintext migration; unsupported operating systems fail closed.
**Covered by:** FR: FR-ROMM-CONN-001, FR-ROMM-COVER-001, FR-ROMM-LAUNCH-001; TEST: TEST-REMOTE-INTEGRATION-001, TEST-ROMM-SEC-001
**Status:** pending
Scope: layer-1+

## TR-ROMM-THREAD-001

**Captured-SynchronizationContext PropertyChanged dispatch** — PLAN-ROMM-001. LibraryObservableObject posts PropertyChanged off-context to the captured UI SynchronizationContext and raises inline when already on-context, preventing RPC_E_WRONG_THREAD in UWP XAML binding. Verified by LibraryObservableObjectTests.Dispatch (covers AC-BROWSE-08).
**Status:** pending
Scope: layer-1+

## TR-SETTINGS-TXN-001

**Truthful transactional Settings state** — Add SettingsStateDto(Active, Accepted) and optional State on settings responses; preserve existing DTO fields and protobuf tags, using GetSettingsResponse.state=3 and UpdateSettingsResponse.state=4.
Prepare a fully configured replacement with media restored before publication; candidate cleanup produces no writeback or success notifications.
Commit under existing synchronization, defer publication until success, and roll back runtime/session/pump state on failure. Do not swallow attachment exceptions.
UpdateSettings honors Limiter.IsEnabled=false atomically and the view model makes no second Warp RPC for Apply.
A single synchronization path handles initial state, refresh, Apply response, Revert and persistence; perform field-wise three-way merge against Accepted.
Preserve existing field and standalone-control compatibility. Detect absent State explicitly rather than fabricating Active/Accepted truth.

## Acceptance Criteria
- **ac-01**: Add SettingsStateDto(Active, Accepted) and optional State on settings responses; preserve existing DTO fields and protobuf tags, using GetSettingsResponse.state=3 and UpdateSettingsResponse.state=4.
- **ac-02**: Prepare a fully configured replacement with media restored before publication; candidate cleanup produces no writeback or success notifications.
- **ac-03**: Commit under existing synchronization, defer publication until success, and roll back runtime/session/pump state on failure. Do not swallow attachment exceptions.
- **ac-04**: UpdateSettings honors Limiter.IsEnabled=false atomically and the view model makes no second Warp RPC for Apply.
- **ac-05**: A single synchronization path handles initial state, refresh, Apply response, Revert and persistence; perform field-wise three-way merge against Accepted.
- **ac-06**: Preserve existing field and standalone-control compatibility. Detect absent State explicitly rather than fabricating Active/Accepted truth.
**Covered by:** FR: FR-MACHINEPICKER-001, FR-SETTINGS-TXN-001, FR-UISETVIS-001, FR-VIC20-002, FR-VIC20-005, FR-WARP-001; TEST: TEST-SETTINGS-TXN-001, TEST-UISET-003, TEST-BASELINE-CORRECTNESS-001, TEST-UISET-001, TEST-UISET-002, TEST-VIC20-FLASH-001, TEST-COMPLETION-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Add SettingsStateDto(Active, Accepted) and optional State on settings responses; preserve existing DTO fields and protobuf tags, using GetSettingsResponse.state=3 and UpdateSettingsResponse.state=4.
- [ ] Prepare a fully configured replacement with media restored before publication; candidate cleanup produces no writeback or success notifications.
- [ ] Commit under existing synchronization, defer publication until success, and roll back runtime/session/pump state on failure. Do not swallow attachment exceptions.
- [ ] UpdateSettings honors Limiter.IsEnabled=false atomically and the view model makes no second Warp RPC for Apply.
- [ ] A single synchronization path handles initial state, refresh, Apply response, Revert and persistence; perform field-wise three-way merge against Accepted.
- [ ] Preserve existing field and standalone-control compatibility. Detect absent State explicitly rather than fabricating Active/Accepted truth.

## TR-SID-AMPLIFY-001

**SID output amplify/clip + external-filter enable (float host seam)** — PLAN-VICEPARITY-001 S12 aligns the managed SID audio-output stage with reSID. GenerateSample now applies reSID amplify(input, scaleFactor) = clip((scaleFactor*input)/2) (sid.cc:54-57) on the pre-amplify external-filter output SID::output() (=extfilt.output(), sid.h:190-194), then scales by 1/2^15 for the float [-1,1] host contract - lossless (every short is exact in float, /2^15 is exact) and C# int division truncates toward zero exactly like C++. The 6581 amplify scaleFactor is 3 and the 8580 is 5 (set_chip_model, sid.cc:86,121), so the 6581 mixes 1.5x louder, matching VICE. ClipPcm16 saturates to signed 16-bit [-32768,32767]. The external filter gained an enable flag (default true, mirroring VICE resid.cc:200 enable_external_filter(true)); the disabled branch passes through (Vlp=Vi<<11, Vhp=0, extfilt.h:100-105) and exists only to lock FR-SID-OUTPUT AC-03 (VICE always enables). CaptureAudioTap is unchanged (the amplify is on the float host path, not the capture tee). Verified: OUTPUT-01 composite lockstep vs the c64 oracle (SID::output() bit-exact + amplified-float identity); SidDigiPlaybackTests (range/relative assertions) survive the 1.5x change; LockstepValidation + audio suites + Category=Determinism green.
**Status:** pending
Scope: layer-1+

## TR-SIDAUDIO-CLOCK-001

**SID ticks once per phi2 cycle** — Sid6581.ClockDivisor is 1 so the SystemClock ticks the SID every master cycle; the 24-bit accumulator advances by Frequency per tick and the ADSR rate tables are resid phi2-cycle values. ConfigureAudioClock divides by ClockDivisor so the 44.1 kHz sample rate self-corrects.
**Covered by:** FR: FR-SIDAUDIO-001; TEST: TEST-SIDAUDIO-001
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Sid6581.ClockDivisor returns 1
- [x] Direct-Tick SID tests are divisor-independent; only SystemClock-driven calibration changed

## TR-SIDEBARUI-LAYOUT-001

**Anchor-derived collapse expander + wrap-panel button groups** — AttachPanelViewModel exposes CollapseExpanderDock (Avalonia Dock) and CollapseGlyph derived from DockSide and raised on change. AttachPanelView docks a full-height collapse Button on CollapseExpanderDock and re-docks it on change. The tab buttons, Monitor buttons and Settings action buttons use WrapPanel with per-button margins.
**Covered by:** FR: FR-SIDEBARUI-001; TEST: TEST-SIDEBARUI-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [x] CollapseExpanderDock is Dock.Right when DockSide is Left and Dock.Left when DockSide is Right
- [x] Changing DockSide raises PropertyChanged for CollapseExpanderDock and CollapseGlyph

## TR-SID-EDGE-004

**reSID waveform DAC centering and normalized mix scale** — Sid6581 and Sid8580 subtract the model-specific waveform zero level before ADSR envelope application, preserve no-waveform silence relative to the D418 baseline, and normalize mixed output before host audio submission.
**Covered by:** FR: FR-SID-001, FR-SID-002, FR-SID-003, FR-SID-004, FR-SID-005, FR-SID-006, FR-SID-007, FR-SID-008, FR-SID-009, FR-SID-010, FR-SID-011, FR-SID-012, FR-SID-013, FR-SID-014; TEST: TEST-SID-001, TEST-SID-002, TEST-X64SC-LOCKSTEP-001
**Status:** completed
Scope: layer-1+

## TR-SID-ORACLE-002

**SID exact-oracle sampling method + buffered clock exports** — Two shim exports added for SID parity beyond the single-cycle oracle: vice_sid_exact_set_sampling(machine, method, sampleFreq, passFreq, filterScale) reconfigures the private reSID engine sampling method (0=FAST 1=INTERPOLATE 2=RESAMPLE 3=RESAMPLE_FASTMEM; clock_freq fixed 985248.0 PAL) so the 8580 SAMPLE_FAST write pipeline is observable; vice_sid_exact_clock_buffered(machine, cycles, buffer, len, out remaining) drives reSID SID::clock(delta_t, buf, n) for the S13 fixed-point resampler FIR parity. Implemented as resid_shim_set_sampling / resid_shim_clock_buffered in native/vice/vice/src/sid/resid.cc, wrapped in native/vice-shim.c under g_state_lock + is_active_machine, declared in vice-shim.h, bound via ViceNative LibraryImport + ViceNativeBridge. 8580 lockstep uses the c64c machine selector (SidModel 1 = MOS8580). reSID filter dither is zeroed in the shim for bit-exact comparability. Build: resid.o archives into src/sid/libsid.a; delete both before rebuilding. Landed by commit 93aef16 (dll SHA256 b743c666).
**Status:** completed
Scope: layer-1+

## TR-SID-RESAMPLE-001

**SID fixed-point sampling engine port (fast/interpolate/Kaiser-FIR resample)** — PLAN-VICEPARITY-001 S13 ports reSID's sampling engine (sid.cc:542-1038, sid.h:140-179) into src/ViceSharp.Chips/Audio/Sid6581.Sampling.cs. Constants FirN=125, FirRes=285, FirShift=15, RingSize=1<<14, FixpShift=16. SetSamplingParameters is a statement-for-statement port of set_sampling_parameters: the resampling constraint checks, cycles_per_sample = (int)(clock/sample*(1<<16)+0.5) 16.16 fixed point, the mirrored ring short[RingSize*2], the Kaiser-windowed sinc FIR table (I0 Bessel, beta=0.1102*(A-8.7), the only legitimate float stage, rounded to short round-half-away-from-zero). ClockBuffered mirrors SID::clock(delta_t, buf, n): clock_fast, clock_interpolate, clock_resample. It calls the managed single-cycle Tick() where reSID calls clock() and reads CycleExternalFilterOutput where reSID calls output(), so the emitted short stream is bit-exact vs the oracle. RESAMPLE_FASTMEM deliberately NOT ported. All buffers preallocated; zero per-sample allocation. Verified by OUTPUT-08..13 vs SidExactClockBuffered (TR-SID-ORACLE-002) on both 6581 (c64) and 8580 (c64c). Slice LW (commit 4924b94) COMPLETED the live-audio wiring: the live path now drives this SAMPLE_RESAMPLE engine one cycle per Tick (EmitLiveResampleTick), Resample-always (VICE x64sc default), bit-exact vs the buffered pull and zero-allocation; SetRelativeSpeed scales the 16.16 cadence only (warp = pitch, synthesis untouched). Benchmark SidSamplingBenchmarks: live tail ~0.30x real-time (100% speed holds). Post-review fix 9b00899 handles the zero-cycle-window underflow below ~4.5% warp. PLAN-VICEPARITY-001 CLOSED 466/466.
**Status:** completed
Scope: layer-1+

## TR-SIMD-001

**SIMD-Accelerated Rendering and Audio with Generic Specialization for CPU Core** — Performance-critical rendering and audio processing paths shall use SIMD (Single Instruction, Multiple Data) intrinsics to process multiple pixels or audio samples in parallel. The CPU core shall use generic specialization (constrained generics with `where T : struct`) to allow the JIT compiler to generate specialized machine code for different CPU variants (6502, 6510, 8502) without virtual dispatch overhead.
**Covered by:** FR: FR-CFG-003; TEST: TEST-CFG-001, TEST-CLI-LAUNCHER-001, TEST-HOST-001, TEST-X64SC-LOCKSTEP-001
**Status:** pending
Scope: layer-1+

## TR-SNAPFULL-001

**Complete machine snapshot/restore with deterministic round-trip** — GetState()+restore capture all execution-affecting state (CPU incl. pending IRQ/NMI + pipeline, VIC sequencer, CIA timer pipelines + TOD, SID, PLA/processor port, memory, and the 1541 drive + IEC bus when present). Invariant: restore then run N cycles equals continuous run N (full MachineState + memory equality).
**Covered by:** FR: FR-REVEXEC-001; TEST: TEST-REVEXEC-001, TEST-SNAPFULL-001
**Status:** pending
Scope: layer-1+

## TR-SNDREG-GATE-001

**Sound back-pressure regulator in ViceEmulationGate** — IAudioChip exposes QueuedSampleCount and IsAudioTimingSource (Sid6581 overrides). ViceEmulationGate.EvaluateSound decides NotTimingSource/Advance/BackPressure; Tick applies warp->sound->vsync precedence and reports LastRegulator; HighWaterSamples is about 46 ms.
**Covered by:** FR: FR-SNDREG-001; TEST: TEST-SNDREG-001
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] EvaluateSound returns BackPressure at or over high-water, Advance below, NotTimingSource when inactive
- [x] Gate Tick selects Sound/Vsync/Warp and exposes it via LastRegulator

## TR-STATE-001

**Mutation Queue with ACID State Transactions and Configurable State Window** — All mutations to emulated machine state shall be performed through a mutation queue that provides ACID-like transaction semantics. State changes within a single emulation step (one clock cycle or one instruction) are grouped into a transaction that is either fully applied or fully rolled back. A configurable state window maintains a ring buffer of recent states to support rewind, debugging, and snapshot operations.
**Covered by:** FR: FR-CFG-001, FR-CFG-002, FR-CFG-007, FR-SNP-001, FR-SNP-002, FR-SNP-003, FR-SNP-004; TEST: TEST-CFG-001, TEST-CLI-LAUNCHER-001, TEST-HOST-001, TEST-X64SC-LOCKSTEP-001, TEST-SNP-001
**Status:** pending
Scope: layer-1+

## TR-SYSBTN-001

**Portable lib, no WinRT/ASP.NET, zero reflection** — Portable lib (no WinRT/ASP.NET, IsAotCompatible, zero reflection). Acceptance: verified by TEST-SYSBTN-001.
**Covered by:** FR: FR-SYSBTN-001; TEST: TEST-SYSBTN-001
**Status:** pending
Scope: layer-1+

## TR-SYSBTN-002

**Edge-triggered activation (Press/Hold/Toggle)** — Edge-triggered activation (Press once/Hold paired/Toggle). Acceptance: verified by TEST-SYSBTN-001.
**Covered by:** FR: FR-SYSBTN-001, FR-SYSBTN-005; TEST: TEST-SYSBTN-001
**Status:** pending
Scope: layer-1+

## TR-SYSBTN-003

**Source-gen JsonSerializerContext persistence** — Source-gen JsonSerializerContext persistence, no reflection. Acceptance: verified by TEST-SYSBTN-003.
**Covered by:** FR: FR-SYSBTN-008; TEST: TEST-SYSBTN-003
**Status:** pending
Scope: layer-1+

## TR-SYSBTN-004

**State-mutating dispatch marshaled to session lock** — State-mutating dispatch marshaled onto the session lock, not the input thread. Acceptance: verified by TEST-SYSBTN-002.
**Covered by:** FR: FR-SYSBTN-004, FR-SYSBTN-006; TEST: TEST-SYSBTN-002
**Status:** pending
Scope: layer-1+

## TR-SYS-SCHED-001

**Single-worker interleave scheduling with IEC bus-event sync** — One emulation worker interleaves systems with per-system deficit pacing and resynchronizes at InterSystemBus.LineChanged; lazy sync-on-IEC-access retained as the fine-grained backstop.
**Covered by:** FR: FR-IECHOTPLUG-001, FR-SYSINDEP-001; TEST: TEST-IECHOTPLUG-001, TEST-SYSINDEP-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Each system advances on its own clock via a self-correcting deficit pacer.
- [ ] Systems resync at LineChanged so IEC bus edges are observed in order before a read.

## TR-SYSTEM-CORE-001

**Definable System Core for Machine-Specific Bus Behavior** — ViceSharp machine variants shall express board-level behavior through a definable system core that defines chip interconnect rules, bus arbitration, address decoding, programmable logic behavior, interrupt routing, and model-specific peripheral wiring. Individual chips remain reusable components; machine profiles select a system core specification, and `ArchitectureBuilder` is the glue that instantiates the chips and connects them to the selected system core.

The x64sc C64-family variants are the first acceptance testbed for this requirement because they share a CPU/VIC-II/SID/CIA lineage while differing in PAL/NTSC/PAL-N timing, VIC-II revision, SID revision, CIA revision, PLA/board behavior, datasette/IEC/user-port availability, keyboard availability, ROM selection, C64GS behavior, PET64/SX-64 policy, Japanese resources, and MAX/Ultimax wiring.

## Acceptance Criteria
- The default C64 PAL profile is built by `ArchitectureBuilder` with a selected C64 system core and explicit CPU, VIC-II, SID, CIA1, CIA2, PLA/address decode, RAM, ROM, color RAM, keyboard, joystick, IEC, datasette, and cartridge connections.
- x64sc variants can override timing, chip revision, ROM/resource identity, board availability flags, and bus/interconnect policy through profile/system-core data.
- A test can prove that two variants with the same chips but different board policy produce different bus-visible behavior without changing chip code.
- Core timing tests validate interrupt routing, raster/badline timing, VIC/CPU bus ordering, and selected memory windows through the system core.
- Final lockstep validation can select every x64sc profile and compare CPU, memory windows, CIA/VIC/SID registers, IRQ/NMI, and raster checkpoints against native x64sc.
- The `src/ViceSharp.Chips` source tree is inventoried and every remaining type is classified as reusable chip behavior, chip variant, media-format helper, moved device helper, or retired duplicate/stub.
- Shared chip implementations contain no C64, C1541, or other machine-specific board glue; C64 CPU timing, processor-port reset policy, CIA/SID address windows, VIC-bank translation, and IEC/datasette/cartridge glue live outside shared chip cores.
- VIA behavior is implemented once as a shared chip, and C1541 drive wiring is encapsulated in drive/device adapters rather than in the VIA implementation.
- C64/device helpers that are not shared chips, including IEC drive runtime, standard cartridge mapping, datasette, C64 input/VKM, and media capture helpers, no longer live in `src/ViceSharp.Chips`.
- `TEST-ARCH-CHIPGLUE-001` and the x64sc lockstep/checkpoint gate pass with 0 failed and 0 skipped tests for the chip-glue remediation scope.
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The default C64 PAL profile is built by `ArchitectureBuilder` with a selected C64 system core and explicit CPU, VIC-II, SID, CIA1, CIA2, PLA/address decode, RAM, ROM, color RAM, keyboard, joystick, IEC, datasette, and cartridge connections.
- [ ] x64sc variants can override timing, chip revision, ROM/resource identity, board availability flags, and bus/interconnect policy through profile/system-core data.
- [ ] A test can prove that two variants with the same chips but different board policy produce different bus-visible behavior without changing chip code.
- [ ] Core timing tests validate interrupt routing, raster/badline timing, VIC/CPU bus ordering, and selected memory windows through the system core.
- [ ] Final lockstep validation can select every x64sc profile and compare CPU, memory windows, CIA/VIC/SID registers, IRQ/NMI, and raster checkpoints against native x64sc.
- [ ] The `src/ViceSharp.Chips` source tree is inventoried and every remaining type is classified as reusable chip behavior, chip variant, media-format helper, moved device helper, or retired duplicate/stub.
- [ ] Shared chip implementations contain no C64, C1541, or other machine-specific board glue; C64 CPU timing, processor-port reset policy, CIA/SID address windows, VIC-bank translation, and IEC/datasette/cartridge glue live outside shared chip cores.
- [ ] VIA behavior is implemented once as a shared chip, and C1541 drive wiring is encapsulated in drive/device adapters rather than in the VIA implementation.
- [ ] C64/device helpers that are not shared chips, including IEC drive runtime, standard cartridge mapping, datasette, C64 input/VKM, and media capture helpers, no longer live in `src/ViceSharp.Chips`.
- [ ] `TEST-ARCH-CHIPGLUE-001` and the x64sc lockstep/checkpoint gate pass with 0 failed and 0 skipped tests for the chip-glue remediation scope.

## TR-TAP-EDGE-001

**Datasette sense line and record mode** — Datasette.SenseLine = false when PlayPressed or RecordPressed (CIA1  bit 4 active-low). TryWritePulse stores pulse when MotorEnabled && RecordPressed. RecordedPulseCount tracks stored pulses.
**Covered by:** FR: FR-BACKFILL-MEDIA-001, FR-RUNTIME-TAPE-002; TEST: TEST-DRV-MOTOR-001, TEST-TAPE-RAMP-001, TEST-TAPE-SENSE-001
**Status:** closed
Scope: layer-1+

## TR-TAPE-EDGE-001

**Datasette motor 32,000-cycle ramp before pulse delivery** — Datasette.Tick() enforces 32,000-cycle motor ramp (MOTOR_DELAY=32000, datasette/datasette.c:62) before TryReadNextPulse delivers pulses. Ramp only activates when Tick() is used as timing. Motor off resets ramp.
**Covered by:** FR: FR-BACKFILL-MEDIA-001, FR-RUNTIME-TAPE-002; TEST: TEST-DRV-MOTOR-001, TEST-TAPE-RAMP-001, TEST-TAPE-SENSE-001
**Status:** closed
Scope: layer-1+

## TR-TESTGATE-001

**Category=Xbox tests are non-skipping** — Xbox off-console tests use plain [Fact]/[Theory] + [Trait("Category","Xbox")], never [ViceFact]/Assert.Skip, so Category=Xbox is genuinely non-skipping. Acceptance: verified by XboxTestConventionTests.
**Covered by:** FR: FR-TESTGATE-002; TEST: TEST-TESTGATE-002
**Status:** pending
Scope: layer-1+

## TR-TESTGATE-003

**Documented gate command set** — The gate command set is dotnet build slnx + scoped dotnet test + dotnet publish AOT + a documented on-console checklist. Acceptance: verified by TEST-TESTGATE-009.
**Covered by:** FR: FR-TESTGATE-001, FR-TESTGATE-003; TEST: TEST-TESTGATE-008, TEST-TESTGATE-009
**Status:** pending
Scope: layer-1+

## TR-TESTGATE-004

**Head validated by raw-file-read** — Head validated by raw-file-read (harness references the portable libs, not the head). Acceptance: verified by slice audit / TEST-TESTGATE-010.
**Covered by:** FR: FR-TESTGATE-004; TEST: TEST-TESTGATE-010
**Status:** pending
Scope: layer-1+

## TR-TICKHIST-CAPTURE-001

**Write-delta capture and reconstruction** — BasicBus publishes MemoryWriteEvent (pre-write byte, gated on a live subscriber); the CPU publishes instruction-completed events; one LockFreePubSub per machine is wired in ArchitectureBuilder and exposed via IMachine.PubSub. TickHistoryRecorder keeps a 100-deep ring bundling write-deltas per instruction; reconstruction reverse-applies later ticks' writes to the current paused RAM.
**Covered by:** FR: FR-TICKHIST-001; TEST: TEST-TICKHIST-001, TEST-TICKHIST-002
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Bus publishes MemoryWriteEvent with the pre-write byte only when subscribed
- [x] Recorder ring is bounded to 100 and reconstruction recovers as-of-tick RAM

## TR-TICKHIST-PERF-001

**Tick-history capture is opt-in (zero default overhead)** — The time-travel recorder must stay unsubscribed by default so per-instruction chip-state capture and per-write delta recording impose zero overhead; it is armed only when the History panel reads the trace.
**Covered by:** FR: FR-TICKHIST-001; TEST: TEST-TICKHIST-001, TEST-TICKHIST-002
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] With recording disabled (default) advancing the emulation pump does not subscribe the recorder and tick history stays empty.
- [x] Reading the tick history via GetTickHistory arms recording so subsequent emulation captures.
- [x] BasicBus publishes MemoryWriteEvent only when SubscriptionCount is greater than zero so the default path has zero per-write cost.

## TR-TICKHIST-RPC-001

**Tick-history monitor RPCs** — IMonitorService gains GetTickHistory, ReadMemoryAtTick (reconstructed) and GetChipStateAtTick, with proto/gRPC messages and mappers. IHostProtocolClient surfaces ReadRegisters/ReadMemory/Disassemble plus the three new calls.
**Covered by:** FR: FR-TICKHIST-001; TEST: TEST-TICKHIST-001, TEST-TICKHIST-002
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] GetTickHistory returns index-ordered ticks; ReadMemoryAtTick at the newest tick equals current memory
- [x] Out-of-range tick index returns InvalidArgument

## TR-TICKHIST-UI-001

**History tab and debug screen** — SidebarTab.History hosts TickHistoryView bound to TickHistoryViewModel; IsPaused is fed via AttachPanelViewModel.ApplyStatus. Selecting a tick while paused shows registers, each chip's decoded state, and a reconstructed scrolling hex dump.
**Covered by:** FR: FR-TICKHIST-001; TEST: TEST-TICKHIST-001, TEST-TICKHIST-002
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Refresh lists ticks newest-first; inspect while paused opens the debug screen
- [x] Inspect while running does not open the debug screen

## TR-UIAXAML-001

**All Avalonia views authored in AXAML + MVVM** — Every Avalonia view in ViceSharp.Avalonia is authored declaratively in AXAML with MVVM bindings (no imperative control-tree construction in code-behind), to ease maintenance. The shell window, the sidebar, each per-peripheral control, and the settings panel are AXAML UserControls/Windows bound to view models; code-behind is limited to InitializeComponent and thin glue.
**Status:** pending
Scope: layer-1+

## TR-UIAXAML-PICKER-001

**Avalonia Computer/Model picker binds like Xbox** — AttachPanelViewModel mirrors XboxSettingsViewModel Computers/Models/SelectedComputer/SelectedModel using host SettingsProfileDto.Machine as family key (x64sc, xvic). SettingsView.axaml replaces the single Settings.MachineProfile ComboBox with Settings.Computer and Settings.MachineVariant. IsVic20Selected remains family xvic.

## Acceptance Criteria
- **ac-vm**: ViewModel tests cover Computers list, Models filter, family switch default profile, PAL/NTSC keeps Models instance.
**Covered by:** FR: FR-MACHINEPICKER-001, FR-UISETVIS-001; TEST: TEST-SETTINGS-TXN-001, TEST-UISET-003
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] ViewModel tests cover Computers list, Models filter, family switch default profile, PAL/NTSC keeps Models instance.

## TR-UIAXAML-VIEWS-001

**All Avalonia views authored in AXAML + MVVM** — Every Avalonia view in ViceSharp.Avalonia is authored declaratively in AXAML with MVVM bindings (no imperative control-tree construction in code-behind), to ease maintenance. The shell window, the sidebar, each per-peripheral control, and the settings panel are AXAML UserControls/Windows bound to view models; code-behind is limited to InitializeComponent and thin glue.
**Covered by:** FR: FR-UISETTINGS-001, FR-VIC20-002; TEST: TEST-UI-COREBOUNDARY-001, TEST-UISET-001, TEST-UISET-002, TEST-BASELINE-CORRECTNESS-001
**Status:** pending
Scope: layer-1+

## TR-UI-COREBOUNDARY-001

**Avalonia flash-cart builder abstraction boundary** — Own flash-cart builder interfaces in Abstractions and supply a Host composition factory/private adapter; Avalonia C# and AXAML do not depend on Core flash-cart types.
Preserve existing Core/Xbox public APIs and observable collection/property-change behavior.

## Acceptance Criteria
- **ac-01**: Own flash-cart builder interfaces in Abstractions and supply a Host composition factory/private adapter; Avalonia C# and AXAML do not depend on Core flash-cart types.
- **ac-02**: Preserve existing Core/Xbox public APIs and observable collection/property-change behavior.
**Covered by:** FR: FR-COMPLETION-001, FR-UISETTINGS-001; TEST: TEST-BASELINE-CORRECTNESS-001, TEST-COMPLETION-001, TEST-REMOTE-INTEGRATION-001, TEST-UI-COREBOUNDARY-001, TEST-UISET-001, TEST-UISET-002
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Own flash-cart builder interfaces in Abstractions and supply a Host composition factory/private adapter; Avalonia C# and AXAML do not depend on Core flash-cart types.
- [ ] Preserve existing Core/Xbox public APIs and observable collection/property-change behavior.

## TR-UI-DEVART-001

**Device art SVG-to-XAML pipeline and resource-key contract** — Device art is authored as constrained SVGs in assets-src/device-art/ (viewBox required; svg/g/path/rect/circle/ellipse only; no transforms/gradients/filters/text; fills from 6 placeholder hexes) and shipped exclusively as one generated merged ResourceDictionary src/ViceSharp.Avalonia/Assets/DeviceArt/DeviceArt.axaml of DrawingImage resources under the DeviceArt.* key contract, produced by tools/generate-device-art.py (Python stdlib only, deterministic output, per-source sha256 sync comments over CRLF-normalized bytes). No runtime SVG dependency; normal builds and CI never invoke Python; generated output must stay hash-synced to its sources (test-enforced). Placeholder colors map to shared DeviceArt*Brush resources tuned to the dark palette.
**Covered by:** FR: FR-DEVCARDART-001; TEST: TEST-UI-DEVCARDART-001
**Status:** pending
Scope: layer-1+

## TR-UI-SHELL-001

**Avalonia Emulator Control Shell** — The Avalonia shell shall present emulator controls through ViewModels and host-client abstractions while preserving the host-owned boundary for emulator state and local rendering.

## Acceptance Criteria
- Boundary tests fail if Avalonia ViewModels reference `ViceSharp.Core`, `ViceSharp.Chips`, or concrete architecture/device types.
- ViewModel tests cover sidebar collapse, tab switching, attach state, settings state, VKM selection, status bar state, and monitor pop-out state.
- UI startup succeeds while disconnected or while the in-process host is starting.
**Covered by:** FR: FR-CFG-004, FR-UI-002, FR-UI-003, FR-UI-004, FR-UIFLYOUT-001, FR-UIMENUBAR-001, FR-UIPERIPHERAL-001, FR-UISETTINGS-001; TEST: TEST-CFG-001, TEST-CLI-LAUNCHER-001, TEST-HOST-001, TEST-X64SC-LOCKSTEP-001, TEST-GRPC-001, TEST-UI-001, TEST-UI-DIAG-001, TEST-UIFLYOUT-001, TEST-UIMENUBAR-001, TEST-UIPERIPHERAL-001, TEST-UI-COREBOUNDARY-001, TEST-UISET-001, TEST-UISET-002
**Status:** in_progress
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The shell has status bar and peripherals sidebar surfaces that display IEC activity.
- [ ] ViewModels consume host client abstractions and DTOs rather than emulator internals.
- [ ] ViewModel tests cover peripherals and status active-to-idle IEC transitions.

## TR-VIC20-FLASH-001

**VICE-compatible AM29F040B timing and safe expansion write-back** — Flash040Core exposes AdvanceCycles and the VIC-20 machine clock advances it. TYPE_B timeout/sector/chip budgets, busy reads, cancel, suspend, and resume are deterministic. Expansion-cart dirty payloads remain type-correct; host detach uses a same-directory temporary file and atomic replacement.
**Covered by:** FR: FR-VIC20-005; TEST: TEST-SETTINGS-TXN-001, TEST-VIC20-FLASH-001
**Status:** pending
Scope: layer-1+

## TR-VIC20-PIXEL-001

**Pixel compare path index-primary then BGRA** — Managed and xvic expose normal-border index/BGRA buffers at a common sync point. READY PAL/NTSC/busy index SequenceEqual is Exact-scoped. READY PAL, READY NTSC, and busy PAL native BGRA SequenceEqual vs xvic capture_visible_frame is also Exact-scoped (canvas RGB, opaque alpha; NTSC unpainted clamp rows opaque black). Other border modes remain Partial. Not whole-machine Exact.
**Covered by:** FR: FR-UISETTINGS-001, FR-VIC20-001, FR-VIC20-002; TEST: TEST-UI-COREBOUNDARY-001, TEST-UISET-001, TEST-UISET-002, TEST-VIC20-001, TEST-BASELINE-CORRECTNESS-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [x] Managed index and BGRA buffers match xvic canvas geometry (evidence: Vic20PixelLockstep geometry)
- [x] Compare sync point defined and used by all pixel tests (evidence: TryCaptureVisibleFrame + FrameBuffer SequenceEqual)
- [x] Index compare is Exact-scoped for READY PAL/NTSC/busy; BGRA SequenceEqual is Exact-scoped for the same named captures after canvas palette match (evidence: six isolated Vic20PixelLockstep facts)

## TR-VIC20-SOUND-001

**VIC-I sound advances on the machine clock and batches PCM without hot-path allocation** — Mos6561 owns one machine-agnostic Vic20Sound engine. System-clock advancement produces deterministic PCM16 mono and dispatches 256-sample batches without per-store allocation. Native parity is scoped to the covered silence and tone inputs.
**Covered by:** FR: FR-VIC20-SOUND-001; TEST: TEST-NATIVE-LIFECYCLE-001, TEST-VIC20-SOUND-001
**Status:** pending
Scope: layer-1+

## TR-VIC-EDGE-001

**VIC-II native screen-RAM visible-frame checkpoint** — Native VICE screen RAM (-) survives one full PAL frame (19,656 cycles) unchanged. VIC-II c-access DMA is read-only. VICE vicii-fetch.c:135-166.
**Covered by:** FR: FR-BACKFILL-VIDEO-001; TEST: TEST-VIC-CHECKPOINT-001, TEST-VIC-RC-001
**Status:** closed
Scope: layer-1+

## TR-VIC-EDGE-002

**VIC-II PAL raster position frame-periodic** — Managed PAL VIC-II raster (rasterLine, rasterX) returns to identical position after exactly one frame (312x63=19,656 cycles). VICE vicii-cycle.c:576-598.
**Covered by:** FR: FR-BACKFILL-VIDEO-001, FR-VIC-001; TEST: TEST-VIC-CHECKPOINT-001, TEST-VIC-RC-001, TEST-VIC-001, TEST-X64SC-LOCKSTEP-001
**Status:** closed
Scope: layer-1+

## TR-VIC-EDGE-003

**VIC-II RC window state machine cycle-accurate** — VIC-II row counter (RC) and video counter (VC) state machine matches VICE viciisc/vicii-cycle.c:541-563. VC update at RasterX=13: vc=vcBase, vmli=0, bad line sets rc=0. RC update at RasterX=57: rc==7 sets idle; if not idle or bad line, rc=(rc+1)&7.
**Covered by:** FR: FR-BACKFILL-VIDEO-001, FR-VIC-008; TEST: TEST-VIC-CHECKPOINT-001, TEST-VIC-RC-001, TEST-VIC-001, TEST-X64SC-LOCKSTEP-001
**Status:** closed
Scope: layer-1+

## TR-VIC-EDGE-004

**VIC-II non-PAL sprite DMA window native checkpoint** — All 5 non-PAL VIC-II models (Mos6567, Mos8562, Mos6567R56A, Mos6572, Mos8565) fire sprite-3 DMA near line 0x50. Managed IsCpuCycleStolen true at (SpriteY+1, rasterX 0). VICE vicii-chip-model.c:272-403/437-566.
**Covered by:** FR: FR-BACKFILL-VIDEO-001; TEST: TEST-VIC-CHECKPOINT-001, TEST-VIC-RC-001
**Status:** closed
Scope: layer-1+

## TR-VIC-EDGE-005

**VIC-II c-access DMA read-only semantics** — VIC-II matrix DMA c-access reads screen RAM without write side-effects. Screen RAM bytes written before a frame remain unchanged after. VICE vicii-fetch.c:135-166.
**Covered by:** FR: FR-BACKFILL-VIDEO-001; TEST: TEST-VIC-CHECKPOINT-001, TEST-VIC-RC-001
**Status:** closed
Scope: layer-1+

## TR-VIC-EDGE-006

**VIC-II screen RAM immediate read-back** — ViceNativeBridge ReadMemory/WriteMemory roundtrip for - returns written value in zero elapsed cycles. VICE c-access is read-only.
**Covered by:** FR: FR-BACKFILL-VIDEO-001; TEST: TEST-VIC-CHECKPOINT-001, TEST-VIC-RC-001
**Status:** closed
Scope: layer-1+

## TR-VICE-ORACLE-001

**Read-only native VICE oracle boundary** — The native/vice gitlink is a read-only behavioral oracle. All stepping, tracing, snapshot, bus-event, and lifecycle integration must be implemented by external wrappers, launchers, link-time interposition, or public VICE interfaces that leave the pinned target source and semantics unchanged.
**Covered by:** FR: FR-VICE-001; TEST: TEST-VICE-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The native oracle preflight fails before validation when any tracked file under native/vice differs from the pinned gitlink commit.
- [ ] Wrapper builds and tests do not edit, generate source into, or apply patches to tracked native/vice paths.
- [ ] Required CPU bus events, timing, snapshots, and lifecycle observations come from real immutable VICE execution through external wrapper mechanisms; PeekBus, PC-derived values, and fabricated events are forbidden.
- [ ] Each oracle receipt records the native/vice commit, wrapper version or hash, clean-target proof, command, and zero-failure/zero-skip result for the claimed scope.
- [ ] A missing wrapper capability is an explicit blocker and cannot be remediated by modifying native/vice.

## TR-VSFLOCKSTEP-RESUME-001

**Backward-compatible snapshot module read and VIC-II model alignment** — An external compatibility adapter outside `native/vice` shall normalize legacy x64sc `MAINCPU` v1.2 and v1.3 modules for the pinned, unmodified VICE reader while current v1.4 modules remain byte-identical. The wrapper shall align the VIC-II model before `machine_read_snapshot` and normalize wrapper-reported `snapshot_last_error` after a successful read. No part of this compatibility path may edit, patch, generate into, or behaviorally change `native/vice`.
**Covered by:** FR: FR-NATIVERESIDUE-001, FR-SNP-005, FR-VSFLOCKSTEP-001; TEST: TEST-BASELINE-CORRECTNESS-001, TEST-DEPS-202607-001, TEST-NATIVERESIDUE-001, TEST-SNP-002, TEST-VSFLOCKSTEP-001
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] An external compatibility adapter outside `native/vice` loads a validated `MAINCPU` v1.2 module by inserting the absent v1.3 `ane_log_level` and `lxa_log_level` fields and absent v1.4 `maincpu_jammed` field with the pinned immutable VICE defaults before `maincpu_ba_low_flags`, updates the inclusive module length, preserves every following byte and module, handles v1.3 by inserting only `maincpu_jammed`, and passes current v1.4 through byte-for-byte. Malformed or unsupported layouts fail closed. External v1.2 and v1.3 fixtures plus a current v1.4 native round-trip prove resume behavior with zero failed, zero skipped, and zero aborted prerequisites.
- [ ] native/build-vice-shim.ps1 materializes the pinned VICE commit into a separate initially clean source root outside native/vice, follows the pinned VICE Team build instructions there (including required in-source build generation, explicitly approved 2026-10-08), and links only external wrapper translation units stored outside the submodule. Pre-build and post-build commit, tracked status and a complete inventory of original upstream source bytes must match exactly. Record all generated/build files separately with paths, hashes and producing commands; those artifacts are permitted only in the separate materialization/output roots and may not replace or modify original upstream source files. Any patch application, generated file or source mutation under native/vice fails the build. native/patches/vice-shim-runtime.patch is diagnostic legacy evidence only and is never applied.

## TR-WARP-STATUS-001

**Host status DTO emits warp as rate 0 and live CLOCK percent** — ToStatusDto emits LimiterRatePercent 0 when LimiterEnabled is false so the status bar can show WARP. EffectiveClockPercent is computed from sampled EffectiveClockHz over MasterClockHz. Warp must push audio relative speed to the live-audio ceiling so VIC-I sound render does not pin CLOCK at 100 percent.
**Covered by:** FR: FR-WARP-001; TEST: TEST-COMPLETION-001, TEST-SETTINGS-TXN-001, TEST-UISET-002
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] ToStatusDto emits LimiterRatePercent 0 when LimiterEnabled is false.
- [ ] GetStatus after warp reports EffectiveClockPercent greater than 150 in WarpModeTests and live RC dumps.

## TR-XAUDIO-001

**XAudio2 backend Native-AOT + AppContainer safe** — XAudio2 backend Native-AOT + AppContainer safe (no winmm/kernel32/reflection, DisableRuntimeMarshalling-compatible blittable interop). Acceptance: verified by TEST-XAUDIO-001/002.
**Covered by:** FR: FR-XAUDIO-001, FR-XAUDIO-002, FR-XAV-001; TEST: TEST-XAUDIO-DEVICE, TEST-XAUDIO-001, TEST-XAUDIO-003
**Status:** pending
Scope: layer-1+

## TR-XAUDIO-002

**Audio decoupled via lock-free ring buffer** — Audio decoupled via lock-free ring buffer; SubmitSamples non-blocking; no audio-as-clock back-pressure; Pause unblocks. Acceptance: verified by TEST-XAUDIO-004.
**Covered by:** FR: FR-XAUDIO-003; TEST: TEST-XAUDIO-004, TEST-XAUDIO-DEVICE
**Status:** pending
Scope: layer-1+

## TR-XAUDIO-003

**MasterAudioControl + AudioSampleConverter in Abstractions** — MasterAudioControl + AudioSampleConverter relocated to Abstractions, one master gain / one float->PCM16 shared by both backends, no behavior change. Acceptance: verified by TEST-XAUDIO-003, PortableAudioTypesLocationTests.
**Covered by:** FR: FR-XAUDIO-002; TEST: TEST-XAUDIO-001, TEST-XAUDIO-003
**Status:** pending
Scope: layer-1+

## TR-XBOXAOT-001

**Core+Host.InProcess graph links clean under AOT** — Core+Host.InProcess graph links clean under Native AOT, 0 IL2xxx/IL3xxx. Acceptance: verified by TEST-XBOXAOT-001.
**Status:** pending
Scope: layer-1+

## TR-XBOXAOT-002

**MSIX manifest declares Windows.Xbox** — ViceSharpXboxUwp=true emits an MSIX whose manifest declares Windows.Xbox. Acceptance: verified by XboxManifestTests / device.
**Status:** pending
Scope: layer-1+

## TR-XBOXAOT-003

**No shipped Xbox path invokes ViceNative/WinMm P/Invoke** — No shipped Xbox path invokes ViceNative/WinMm/winmm-kernel32 P/Invoke. Acceptance: verified by TEST-XBOXAOT-001.
**Status:** pending
Scope: layer-1+

## TR-XBOXCI-005

**Nuke Xbox targets absent from CI/release YAMLs** — Nuke PublishXbox/DeployXbox/ValidateXbox exist and are absent from both CI/release YAMLs; ValidateXbox runs off-console. Acceptance: verified by TEST-XBOXCI-001.
**Covered by:** FR: FR-XBOXGPL-006; TEST: TEST-XBOXGPL-001
**Status:** pending
Scope: layer-1+

## TR-XBOXGPL-BRAND-001

**C= logo provenance and multi-surface attribution plumbing** — Branding assets under docs/xbox/store-screenshots must derive from the Wikimedia
Commons Commodore C= logo.svg (local SVG + rasterization). Attribution must be
centralized in THIRD_PARTY_NOTICES.md and AboutInfo.LogoAttributionText /
LogoSourceUrl / LogoLicenseUrl, packed into NuGet via Directory.Build.props,
mirrored in package README files, and bound on Xbox AboutPage and Avalonia About.
**Covered by:** FR: FR-XBOXGPL-007; TEST: TEST-XBOXGPL-002
**Status:** completed
Scope: layer-1+

## TR-XBOXPKG-002

**Conditional-TFM fallback + head-only UWP guarded** — ViceSharpXboxUwp conditional-TFM fallback + head-only UWP/XAML/manifest/MSIX guarded so a workload-less agent builds slnx. Acceptance: verified by TEST-XBOXPKG-001.
**Covered by:** FR: FR-XBOXPKG-001, FR-XBOXPKG-003; TEST: TEST-XBOXPKG-001, TEST-XBOXPKG-003
**Status:** pending
Scope: layer-1+

## TR-XBOXTOPO-001

**TFM-conditional fallback (default net10.0)** — TFM-conditional fallback (ViceSharpXboxUwp default false=net10.0) so slnx builds with no UWP workload. Acceptance: verified by TEST-XBOXTOPO-002.
**Covered by:** FR: FR-XBOXTOPO-001; TEST: TEST-XBOXTOPO-001, TEST-XBOXTOPO-002
**Status:** pending
Scope: layer-1+

## TR-XBOXTOPO-002

**Head references only core + Host.InProcess** — Head references only Abstractions/Core/Chips/Architectures + Host.InProcess (R2 amendment); bans Host/Avalonia/Protocol-Grpc/Monitor and Grpc.*/AspNetCore.*/Avalonia.*. Acceptance: verified by TEST-XBOXTOPO-001.
**Covered by:** FR: FR-XBOXTOPO-001; TEST: TEST-XBOXTOPO-001, TEST-XBOXTOPO-002
**Status:** pending
Scope: layer-1+

## TR-XBOXTOPO-003

**Desktop-intact regression guard** — Regression / desktop-intact guard: existing desktop + Host.Xbox scaffold + tests stay green. Acceptance: verified by TEST-TESTGATE-009.
**Status:** pending
Scope: layer-1+

## TR-XBOXUI-001

**Explicit navigation stack independent of Frame journal** — Explicit navigation stack independent of the Frame journal; overlays tracked separately. Acceptance: verified by TEST-XBOXUI-001.
**Covered by:** FR: FR-XBOXUI-001; TEST: TEST-XBOXUI-001, TEST-XBOXUI-003
**Status:** pending
Scope: layer-1+

## TR-XBOXUI-002

**RequiresPointer=Never + XYFocus + safe-area margin** — RequiresPointer=Never shared style + explicit XYFocus from FocusMap + 90% safe-area root margin. Acceptance: verified by TEST-XBOXUI-002a/002b.
**Covered by:** FR: FR-XBOXUI-002; TEST: TEST-XBOXUI-002a, TEST-XBOXUI-002b
**Status:** pending
Scope: layer-1+

## TR-XBOXUI-003

**InputContext observer over single authority** — InputContext observer over the single XboxInputContext authority. Acceptance: verified by TEST-XBOXUI-003.
**Covered by:** FR: FR-XBOXUI-003; TEST: TEST-XBOXUI-003
**Status:** pending
Scope: layer-1+

## TR-XBOXUI-004

**VideoSurfaceHost + pull timer constructed once** — VideoSurfaceHost + pull timer constructed once at shell level, never disposed by navigation. Acceptance: verified by TEST-XBOXUI-004.
**Covered by:** FR: FR-XBOXUI-004; TEST: TEST-XBOXUI-004
**Status:** pending
Scope: layer-1+

## TR-XBOXUI-005

**SwapChainPanel + Win2D pull into pinned buffer** — SwapChainPanel + Win2D pull into a pinned buffer, nearest-neighbor into the TV-safe rect. Acceptance: verified by TEST-XBOXUI-005a/005b.
**Covered by:** FR: FR-XBOXUI-005, FR-XVIDEO-002; TEST: TEST-XBOXUI-005a, TEST-XBOXUI-005b, TEST-XVIDEO-001, TEST-XVIDEO-002
**Status:** pending
Scope: layer-1+

## TR-XBOXUI-006

**VirtualKeyboardLayout names from C64KeyboardMap** — VirtualKeyboardLayout names from the C64KeyboardMap alias set. Acceptance: verified by TEST-XBOXUI-006.
**Covered by:** FR: FR-XBOXUI-006, FR-XKBD-001; TEST: TEST-XBOXUI-006, TEST-XKBD-001
**Status:** pending
Scope: layer-1+

## TR-XBOXUI-007

**TR-MVVM-001 applied (Abstractions + Protocol)** — = TR-MVVM-001 applied (ViewModels reference Abstractions + Protocol). Acceptance: verified by TEST-XBOXUI-007.
**Covered by:** FR: FR-XBOXUI-007; TEST: TEST-XBOXUI-007
**Status:** pending
Scope: layer-1+

## TR-XBOXUI-008

**Fixed disclosure strings from one constants file** — Fixed disclosure strings from one constants file. Acceptance: verified by TEST-XBOXUI-008.
**Covered by:** FR: FR-XBOXUI-008; TEST: TEST-XBOXUI-008
**Status:** pending
Scope: layer-1+

## TR-XKBD-001

**RESTORE trigger is a distinct seam path** — The RESTORE trigger is a distinct seam path; determinism preserved (pushed through the same input seam), no change to per-cycle advance. Acceptance: verified by TEST-XKBD-001.
**Covered by:** FR: FR-XKBD-001; TEST: TEST-XBOXUI-006, TEST-XKBD-001
**Status:** pending
Scope: layer-1+

## TR-XMVVM-001

**Xbox ViewModels reference only Abstractions + Protocol** — Xbox ViewModels reference only Abstractions + Protocol; UWP APIs isolated behind the head. Acceptance: verified by TEST-XBOXUI-007, boundary tests.
**Covered by:** FR: FR-XDEV-001, FR-XDEV-002, FR-XDEV-003, FR-XSET-001, FR-XSET-002, FR-XSET-003, FR-XSET-004; TEST: TEST-XSET-001
**Status:** pending
Scope: layer-1+

## TR-XPATH-001

**Startup path bridge to AppContainer writable root** — Startup path bridge makes the unmodified ViceDataPathResolver resolve the AppContainer writable root; keymap writes land in LocalFolder. Acceptance: verified by TEST-XSET-001, XboxDataPathBridgeTests.
**Covered by:** FR: FR-XROM-001, FR-XROM-002, FR-XROM-003; TEST: TEST-XSET-001
**Status:** pending
Scope: layer-1+

## TR-XPERSIST-001

**Xbox prefs persist via ViceSettings INI** — Xbox prefs persist via the portable ViceSettings INI [ViceSharpXbox], not Avalonia SessionPersistence. Acceptance: verified by TEST-XSET-001, XboxUiStateStoreTests.
**Covered by:** FR: FR-XSET-005; TEST: TEST-XSET-001
**Status:** pending
Scope: layer-1+

## TR-XSET-002

**SettingsOptionCatalog single-sourced in Protocol** — SettingsOptionCatalog single-sourced in Protocol, byte-identical to desktop. Acceptance: verified by TEST-XSET-001, SettingsOptionCatalogTests.
**Covered by:** FR: FR-XSET-001; TEST: TEST-XSET-001
**Status:** pending
Scope: layer-1+

## TR-XVIDEO-001

**Win2D SwapChainPanel pure read-only sink** — Win2D SwapChainPanel pure read-only sink, BGRA passthrough, zero per-frame allocation, FrameGeometry-sized. Acceptance: verified by TEST-XVIDEO-001/002.
**Covered by:** FR: FR-XBOXUI-005, FR-XVIDEO-001, FR-XVIDEO-002; TEST: TEST-XBOXUI-005a, TEST-XBOXUI-005b, TEST-XVIDEO-001, TEST-XVIDEO-002, TEST-XVIDEO-DEVICE
**Status:** pending
Scope: layer-1+

