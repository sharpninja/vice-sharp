# Technical Requirements (MCP Server)

## [TR-REMOTECTRL-SERVER-001]

**[TR-REMOTECTRL-SERVER-001]** — Placeholder requirement backfilled for TODO link [TR-REMOTECTRL-SERVER-001].

## PERF-SPRITE-DMA-OPT-001

**PERF-SPRITE-DMA-OPT-001** — Placeholder requirement backfilled for TODO link PERF-SPRITE-DMA-OPT-001.

## TEST-DRV-001

**TEST-DRV-001** — Placeholder requirement backfilled for TODO link TEST-DRV-001.

## TEST-UI-001

**TEST-UI-001** — Placeholder requirement backfilled for TODO link TEST-UI-001.

## TEST-VIC-001

**TEST-VIC-001** — Placeholder requirement backfilled for TODO link TEST-VIC-001.

## TR-ALLOC-001

**Zero Managed Allocations Per Emulation Cycle on Hot Path** — Sustained managed emulation hot paths, including IMachine.RunFrame for C64 PAL, must avoid per-frame managed allocations so 50 Hz operation does not create GC pressure.
**Acceptance Criteria:**
- [x] The measured C64 PAL RunFrame loop reports zero current-thread allocations over the measured frame window. (evidence: RunFramePerfProbe 60 600: allocated=0 bytes)
- [x] BenchmarkDotNet run reports no managed allocation for C64PalRunFrameBenchmark. (evidence: BenchmarkDotNet C64PalRunFrameBenchmark: Allocated column reported no managed allocation)

## TR-CHIPSTATE-CAPTURE-001

**IStatefulDevice per-tick chip capture** — IStatefulDevice (StateName, StateSize, CaptureState(Span), DecodeState) is implemented by VIC/SID/CIA/PLA. The recorder preallocates per-slot buffers (zero-alloc capture) and deep-copies on Snapshot; the pump registers Machine.Devices.All.OfType<IStatefulDevice>() before subscribing and the host decodes in the same order.
**Acceptance Criteria:**
- [x] A captured tick carries each device's StateSize bytes; capture allocates nothing on the hot path
- [x] Snapshot deep-copies chip state so ring reuse cannot corrupt an earlier snapshot

## TR-CORE-CYCLE-001

**Sub-Cycle Bus-Phase Accuracy Matching VICE x64sc Behavior** — Managed C64 PAL execution must preserve C64 bus, CPU, and VIC-II ordering semantics while optimizing RunFrame so performance changes do not alter cycle-observable behavior.
**Acceptance Criteria:**
- [x] Lockstep and checkpoint suites continue to pass after RunFrame performance optimizations. (evidence: dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter FullyQualifiedName~Lockstep|FullyQualifiedName~Checkpoint => 333 passed, 0 failed, 0 skipped)
- [x] Focused BasicBus/C64MemoryMap/VideoRenderer/VideoSurface/SID tests pass with no failed or skipped tests. (evidence: focused gate => 182 passed, 0 failed, 0 skipped)

## TR-CORE-DET-001

**Bit-Exact Reproducibility Given Same Initial State and Inputs** — Given the same initial emulator state, ROM set, mounted media, inputs, and timing model, managed C64 PAL execution must produce deterministic CPU, bus, video, audio, interrupt, and memory-observable results.
**Acceptance Criteria:**
- [x] Lockstep and checkpoint suites continue to pass after performance optimizations. (evidence: lockstep/checkpoint gate => 333 passed, 0 failed, 0 skipped)
- [x] Optimizations do not change public observable machine state signatures or behavior contracts. (evidence: PR #3 keeps public API signatures stable and correctness gates green)

## TR-CPU-TICK-001

**Per-instance CPU executed-cycle counter and target clock** — ICpu exposes ExecutedCycles and TargetClockHz per instance; MachineState carries a per-CPU breakdown; UpdatePerformanceCounters uses the primary CPU executed cycles divided by its target clock.
**Acceptance Criteria:**
- [ ] ICpu exposes a per-instance ExecutedCycles counter and TargetClockHz.
- [ ] MachineState carries a per-CPU breakdown (id, executedCycles, targetHz) for host and peripheral CPUs.

## TR-CYCLE-001

**VIC-II cycle counter frame-periodic** — Managed VIC-II CycleCounter advances by exactly 19,656 per PAL frame. VICE vicii-cycle.c:576-598.

## TR-DET-001

**Bit-Exact Reproducibility Given Same Initial State and Inputs** — Given the same initial emulator state, ROM set, mounted media, inputs, and timing model, managed C64 PAL execution must produce deterministic CPU, bus, video, audio, interrupt, and memory-observable results.
**Acceptance Criteria:**
- [x] Lockstep and checkpoint suites continue to pass after performance optimizations. (evidence: dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter FullyQualifiedName~Lockstep|FullyQualifiedName~Checkpoint => 333 passed, 0 failed, 0 skipped)
- [x] Optimizations do not change public observable machine state signatures or behavior contracts. (evidence: PR #3 keeps public API signatures stable and lockstep/checkpoint evidence green)

## TR-DRV-EDGE-001

**1541 drive motor 300,000-cycle ramp before rotation** — IecDrive.Tick() enforces 300,000-cycle motor ramp-up (300ms at 1MHz drive clock) before MotorRotationCycles advances. Motor off resets ramp. VICE drive/drive.c.

## TR-DRVLIFE-001

**Drive attach/detach lifecycle** — Enabling a drive creates/starts the drive instance, registers it on the clock and connects it to the always-on IEC bus; disabling stops it, unregisters from the clock and disconnects from the bus.

## TR-GRPC-BOUNDARY-001

**Host Protocol Boundary DTOs** — Host protocol and gRPC adapters expose emulator status, media, command, monitor, and settings state through DTOs without UI clients reaching into emulator internals.
**Acceptance Criteria:**
- [ ] Status, media, command, monitor, and settings requests remain DTO-only across the host protocol boundary.
- [ ] The status DTO includes IEC activity telemetry without removing existing status fields.
- [ ] gRPC service adapters and the Avalonia gRPC client preserve IEC activity telemetry on status round trips.

## TR-HOST-STATUS-001

**Measured Emulator Runtime Telemetry** — Host status telemetry shall report runtime, media, automation, and IEC activity from authoritative runtime state without mutating it.
**Acceptance Criteria:**
- [ ] Status responses include existing runtime telemetry and IEC activity fields.
- [ ] IEC activity is derived from authoritative IEC line changes or an equivalent host-owned bus signal monitor.
- [ ] Status polling does not mutate machine, drive, or bus state.

## TR-IECDECODE-001

**IEC protocol decoder** — Pure state machine over trace samples emitting decoded IEC events: ATN command frame, LISTEN/TALK + device, OPEN/secondary/channel, data bytes with EOI, turnaround, timeout/error.

## TR-IEC-EDGE-001

**IEC bus ATN-response within 985-cycle spec window** — IecBus.Tick() asserts CLK and DATA low within 985 cycles (Tat=1ms at PAL 985,248Hz) of ATN falling edge. Releases both on ATN rising edge. VICE iecbus/iecbus.c:247-266.

## TR-IECELEC-001

**Faithful IEC serial electrical model with native parity** — CIA2 reads the live open-collector IEC bus through the C64 inverting buffers; idle DATA-low is supplied by the true-drive 1541 holding DATA. C64GS (no IEC) keeps disconnected->low. Idle reads match native ($47 normal C64 / $07 C64GS) and native CIA2-IO lockstep stays green.

## TR-IECSPY-001

**IInterSystemBus.Snapshot API** — IInterSystemBus exposes Snapshot() returning BusSnapshot (per-line level + pullers, talking endpoints, attached endpoints). Read-only. DONE.

## TR-IECTRACE-001

**IEC trace recorder (edge + step marks)** — IecBusTraceRecorder subscribes to bus.LineChanged, cycle-stamps each edge from SystemClock plus a sample per step boundary, into a bounded ring; re-derives deterministically after rewind; inactive unless the monitor is open.

## TR-MED-FFMPEG-001

**FfmpegVideoRecorder over two loopback TCP sockets** — External ffmpeg process fed raw BGRA + s16le via BackgroundByteWriter; mirrors VICE ffmpegexedrv. Pre-connect audio buffered + flushed.

## TR-MED-SOUND-001

**CaptureAudioTap + WavAudioRecorder** — Runtime-swappable tap in the SID->output path feeds a 16-bit PCM WAV recorder whose file writes run off the worker.

## TR-MED-VIDEO-001

**BMP-sequence sink with all/unique dedup, off-worker writes** — FrameSequenceCapture writes numbered 24-bit BMPs via a background queue; unique mode skips byte-identical consecutive frames.

## TR-PACESEL-STRAT-001

**Live-switchable pacing strategy plumbing** — EmulationGateStrategies provides canonical ids + gate factory. EmulationPumpService.SetStrategy swaps the gate on the worker thread (no restart). LimiterSettingsDto.PacingStrategy flows through proto/gRPC, SettingsServiceHost applies it live to the pump and round-trips it, and the UI exposes a dropdown.
**Acceptance Criteria:**
- [x] EmulationGateStrategies.CreateGate/Normalize/DisplayName map ids to gates, defaulting to Semaphore
- [x] SetStrategy swaps the gate live; SettingsServiceHost applies and round-trips PacingStrategy

## TR-PERF-ALLOC-001

**Zero Managed Allocations Per RunFrame Hot Path** — Sustained managed C64 PAL RunFrame execution must avoid per-frame managed allocations so 50 Hz operation does not create GC pressure.
**Acceptance Criteria:**
- [x] The measured C64 PAL RunFrame loop reports zero current-thread allocations over the measured frame window. (evidence: RunFramePerfProbe 60 600: allocated=0 bytes)
- [x] BenchmarkDotNet run reports no managed allocation for C64PalRunFrameBenchmark. (evidence: BenchmarkDotNet C64PalRunFrameBenchmark: Allocated column reported no managed allocation)

## TR-PUBSUB-PERF-001

**High-Performance Zero-Allocation Pub/Sub** — ViceSharp shall implement the internal Pub/Sub interconnect with fixed-capacity message storage, 64-byte inline payloads, a MessageKind discriminant, preallocated subscriber route arrays, synchronous publish/delivery, no boxing, and zero managed heap allocation on the steady-state hot path.
**Acceptance Criteria:**
- [x] Publishing one million typed messages to one subscriber is below 50ns per publish operation. (evidence: dotnet run -c Release --project tests/ViceSharp.Benchmarks -- --pubsub-probe 1000000 => publish-one=43.78ns)
- [x] Publishing one million typed messages to three subscribers is below 100ns per publish/delivery operation. (evidence: dotnet run -c Release --project tests/ViceSharp.Benchmarks -- --pubsub-probe 1000000 => publish-three=58.14ns)
- [x] Message pool rent/return is below 20ns and uses fixed-capacity slot handles. (evidence: src/ViceSharp.Core/LockFreePubSub.cs; PubSubPerfProbe => pool-rent-return=16.80ns)
- [x] PayloadArena allocation is below 10ns and supports frame reset semantics. (evidence: src/ViceSharp.Core/LockFreePubSub.cs; PubSubPerfProbe => arena-alloc=3.20ns)
- [x] The typed publish hot path performs zero managed allocations after warmup. (evidence: tests/ViceSharp.TestHarness/LockFreePubSubTests.cs Publish_TypedPayload_HotPathDoesNotAllocate; PubSubPerfProbe => allocated=0 bytes)
- [x] Default message pool capacity is 8192 slots and does not resize during emulation. (evidence: src/ViceSharp.Core/LockFreePubSub.cs DefaultMessageCapacity = 8192 and LockFreeMessagePool(capacity = 8192))
- [x] Delivery order is deterministic in registration order and preserved when subscriber route arrays grow. (evidence: tests/ViceSharp.TestHarness/LockFreePubSubTests.cs Publish_DeliversSubscribersInRegistrationOrder and Subscribe_WhenRouteSubscriberArrayGrows_PreservesDeliveryOrder)

## TR-REMOTECTRL-001

**Embeddable RemoteControl server integration + vendored feed** — ViceSharp.Avalonia references SharpNinja.Avalonia.RemoteControl.Server (0.7.3), vendored into a repo-local NuGet feed (nuget-local/ + NuGet.config package source mapping) so restore is reproducible on Azure DevOps/CI without the sibling source repo. The Avalonia App builds a DI ServiceCollection, calls AddAvaloniaRemoteControl, registers an IRemoteControlRootProvider returning the desktop MainWindow, and attaches the host to the classic-desktop lifetime. Startup is gated behind VICESHARP_REMOTECONTROL_ENABLE and fails closed without a bearer token. Requires Avalonia >= 12.0.3 and Grpc >= 2.80.0 (central package versions bumped accordingly).

## TR-REMOTECTRL-SERVER-001

**Embeddable RemoteControl server integration + vendored feed** — ViceSharp.Avalonia references SharpNinja.Avalonia.RemoteControl.Server (0.7.3), vendored into a repo-local NuGet feed (nuget-local/ + NuGet.config package source mapping) so restore is reproducible on Azure DevOps/CI without the sibling source repo. The Avalonia App builds a DI ServiceCollection, calls AddAvaloniaRemoteControl, registers an IRemoteControlRootProvider returning the desktop MainWindow, and attaches the host to the classic-desktop lifetime. Startup is gated behind VICESHARP_REMOTECONTROL_ENABLE and fails closed without a bearer token. Requires Avalonia >= 12.0.3 and Grpc >= 2.80.0 (central package versions bumped accordingly).

## TR-REVEXEC-001

**Frame-snapshot ring reverse execution** — A frame-granular snapshot ring keyed by cycle backs RewindCycle/RewindFrame: restore nearest snapshot <= target, re-run deterministically to target. Replaces the NotImplemented host stubs. Bounded memory.

## TR-SIDAUDIO-CLOCK-001

**SID ticks once per phi2 cycle** — Sid6581.ClockDivisor is 1 so the SystemClock ticks the SID every master cycle; the 24-bit accumulator advances by Frequency per tick and the ADSR rate tables are resid phi2-cycle values. ConfigureAudioClock divides by ClockDivisor so the 44.1 kHz sample rate self-corrects.
**Acceptance Criteria:**
- [x] Sid6581.ClockDivisor returns 1
- [x] Direct-Tick SID tests are divisor-independent; only SystemClock-driven calibration changed

## TR-SIDEBARUI-LAYOUT-001

**Anchor-derived collapse expander + wrap-panel button groups** — AttachPanelViewModel exposes CollapseExpanderDock (Avalonia Dock) and CollapseGlyph derived from DockSide and raised on change. AttachPanelView docks a full-height collapse Button on CollapseExpanderDock and re-docks it on change. The tab buttons, Monitor buttons and Settings action buttons use WrapPanel with per-button margins.
**Acceptance Criteria:**
- [x] CollapseExpanderDock is Dock.Right when DockSide is Left and Dock.Left when DockSide is Right
- [x] Changing DockSide raises PropertyChanged for CollapseExpanderDock and CollapseGlyph

## TR-SNAPFULL-001

**Complete machine snapshot/restore with deterministic round-trip** — GetState()+restore capture all execution-affecting state (CPU incl. pending IRQ/NMI + pipeline, VIC sequencer, CIA timer pipelines + TOD, SID, PLA/processor port, memory, and the 1541 drive + IEC bus when present). Invariant: restore then run N cycles equals continuous run N (full MachineState + memory equality).

## TR-SNDREG-GATE-001

**Sound back-pressure regulator in ViceEmulationGate** — IAudioChip exposes QueuedSampleCount and IsAudioTimingSource (Sid6581 overrides). ViceEmulationGate.EvaluateSound decides NotTimingSource/Advance/BackPressure; Tick applies warp->sound->vsync precedence and reports LastRegulator; HighWaterSamples is about 46 ms.
**Acceptance Criteria:**
- [x] EvaluateSound returns BackPressure at or over high-water, Advance below, NotTimingSource when inactive
- [x] Gate Tick selects Sound/Vsync/Warp and exposes it via LastRegulator

## TR-SYS-SCHED-001

**Single-worker interleave scheduling with IEC bus-event sync** — One emulation worker interleaves systems with per-system deficit pacing and resynchronizes at InterSystemBus.LineChanged; lazy sync-on-IEC-access retained as the fine-grained backstop.
**Acceptance Criteria:**
- [ ] Each system advances on its own clock via a self-correcting deficit pacer.
- [ ] Systems resync at LineChanged so IEC bus edges are observed in order before a read.

## TR-TAP-EDGE-001

**Datasette sense line and record mode** — Datasette.SenseLine = false when PlayPressed or RecordPressed (CIA1  bit 4 active-low). TryWritePulse stores pulse when MotorEnabled && RecordPressed. RecordedPulseCount tracks stored pulses.

## TR-TAPE-EDGE-001

**Datasette motor 32,000-cycle ramp before pulse delivery** — Datasette.Tick() enforces 32,000-cycle motor ramp (MOTOR_DELAY=32000, datasette/datasette.c:62) before TryReadNextPulse delivers pulses. Ramp only activates when Tick() is used as timing. Motor off resets ramp.

## TR-TICKHIST-CAPTURE-001

**Write-delta capture and reconstruction** — BasicBus publishes MemoryWriteEvent (pre-write byte, gated on a live subscriber); the CPU publishes instruction-completed events; one LockFreePubSub per machine is wired in ArchitectureBuilder and exposed via IMachine.PubSub. TickHistoryRecorder keeps a 100-deep ring bundling write-deltas per instruction; reconstruction reverse-applies later ticks' writes to the current paused RAM.
**Acceptance Criteria:**
- [x] Bus publishes MemoryWriteEvent with the pre-write byte only when subscribed
- [x] Recorder ring is bounded to 100 and reconstruction recovers as-of-tick RAM

## TR-TICKHIST-PERF-001

**Tick-history capture is opt-in (zero default overhead)** — The time-travel recorder must stay unsubscribed by default so per-instruction chip-state capture and per-write delta recording impose zero overhead; it is armed only when the History panel reads the trace.
**Acceptance Criteria:**
- [x] With recording disabled (default) advancing the emulation pump does not subscribe the recorder and tick history stays empty.
- [x] Reading the tick history via GetTickHistory arms recording so subsequent emulation captures.
- [x] BasicBus publishes MemoryWriteEvent only when SubscriptionCount is greater than zero so the default path has zero per-write cost.

## TR-TICKHIST-RPC-001

**Tick-history monitor RPCs** — IMonitorService gains GetTickHistory, ReadMemoryAtTick (reconstructed) and GetChipStateAtTick, with proto/gRPC messages and mappers. IHostProtocolClient surfaces ReadRegisters/ReadMemory/Disassemble plus the three new calls.
**Acceptance Criteria:**
- [x] GetTickHistory returns index-ordered ticks; ReadMemoryAtTick at the newest tick equals current memory
- [x] Out-of-range tick index returns InvalidArgument

## TR-TICKHIST-UI-001

**History tab and debug screen** — SidebarTab.History hosts TickHistoryView bound to TickHistoryViewModel; IsPaused is fed via AttachPanelViewModel.ApplyStatus. Selecting a tick while paused shows registers, each chip's decoded state, and a reconstructed scrolling hex dump.
**Acceptance Criteria:**
- [x] Refresh lists ticks newest-first; inspect while paused opens the debug screen
- [x] Inspect while running does not open the debug screen

## TR-UIAXAML-001

**All Avalonia views authored in AXAML + MVVM** — Every Avalonia view in ViceSharp.Avalonia is authored declaratively in AXAML with MVVM bindings (no imperative control-tree construction in code-behind), to ease maintenance. The shell window, the sidebar, each per-peripheral control, and the settings panel are AXAML UserControls/Windows bound to view models; code-behind is limited to InitializeComponent and thin glue.

## TR-UIAXAML-VIEWS-001

**All Avalonia views authored in AXAML + MVVM** — Every Avalonia view in ViceSharp.Avalonia is authored declaratively in AXAML with MVVM bindings (no imperative control-tree construction in code-behind), to ease maintenance. The shell window, the sidebar, each per-peripheral control, and the settings panel are AXAML UserControls/Windows bound to view models; code-behind is limited to InitializeComponent and thin glue.

## TR-UI-SHELL-001

**Avalonia Emulator Control Shell** — The Avalonia shell shall bind host protocol DTOs into status bar and sidebar ViewModels without reaching into emulator internals.
**Acceptance Criteria:**
- [ ] The shell has status bar and peripherals sidebar surfaces that display IEC activity.
- [ ] ViewModels consume host client abstractions and DTOs rather than emulator internals.
- [ ] ViewModel tests cover peripherals and status active-to-idle IEC transitions.

## TR-VIC-EDGE-001

**VIC-II native screen-RAM visible-frame checkpoint** — Native VICE screen RAM (-) survives one full PAL frame (19,656 cycles) unchanged. VIC-II c-access DMA is read-only. VICE vicii-fetch.c:135-166.

## TR-VIC-EDGE-002

**VIC-II PAL raster position frame-periodic** — Managed PAL VIC-II raster (rasterLine, rasterX) returns to identical position after exactly one frame (312x63=19,656 cycles). VICE vicii-cycle.c:576-598.

## TR-VIC-EDGE-003

**VIC-II RC window state machine cycle-accurate** — VIC-II row counter (RC) and video counter (VC) state machine matches VICE viciisc/vicii-cycle.c:541-563. VC update at RasterX=13: vc=vcBase, vmli=0, bad line sets rc=0. RC update at RasterX=57: rc==7 sets idle; if not idle or bad line, rc=(rc+1)&7.

## TR-VIC-EDGE-004

**VIC-II non-PAL sprite DMA window native checkpoint** — All 5 non-PAL VIC-II models (Mos6567, Mos8562, Mos6567R56A, Mos6572, Mos8565) fire sprite-3 DMA near line 0x50. Managed IsCpuCycleStolen true at (SpriteY+1, rasterX 0). VICE vicii-chip-model.c:272-403/437-566.

## TR-VIC-EDGE-005

**VIC-II c-access DMA read-only semantics** — VIC-II matrix DMA c-access reads screen RAM without write side-effects. Screen RAM bytes written before a frame remain unchanged after. VICE vicii-fetch.c:135-166.

## TR-VIC-EDGE-006

**VIC-II screen RAM immediate read-back** — ViceNativeBridge ReadMemory/WriteMemory roundtrip for - returns written value in zero elapsed cycles. VICE c-access is read-only.

