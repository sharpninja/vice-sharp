# Technical Requirements (MCP Server)

## [TR-REMOTECTRL-SERVER-001]

**[TR-REMOTECTRL-SERVER-001]** — Placeholder requirement backfilled for TODO link [TR-REMOTECTRL-SERVER-001].
Scope: layer-1+

## PERF-SPRITE-DMA-OPT-001

**PERF-SPRITE-DMA-OPT-001** — Placeholder requirement backfilled for TODO link PERF-SPRITE-DMA-OPT-001.
Scope: layer-1+

## TEST-DRV-001

**TEST-DRV-001** — Placeholder requirement backfilled for TODO link TEST-DRV-001.
Scope: layer-1+

## TEST-UI-001

**TEST-UI-001** — Placeholder requirement backfilled for TODO link TEST-UI-001.
Scope: layer-1+

## TEST-VIC-001

**TEST-VIC-001** — Placeholder requirement backfilled for TODO link TEST-VIC-001.
Scope: layer-1+

## TR-ALLOC-001

**Zero Managed Allocations Per Emulation Cycle on Hot Path** — Sustained managed emulation hot paths, including IMachine.RunFrame for C64 PAL, must avoid per-frame managed allocations so 50 Hz operation does not create GC pressure.
Scope: layer-1+
**Acceptance Criteria:**
- [x] The measured C64 PAL RunFrame loop reports zero current-thread allocations over the measured frame window. (evidence: RunFramePerfProbe 60 600: allocated=0 bytes)
- [x] BenchmarkDotNet run reports no managed allocation for C64PalRunFrameBenchmark. (evidence: BenchmarkDotNet C64PalRunFrameBenchmark: Allocated column reported no managed allocation)

## TR-CHIPSTATE-CAPTURE-001

**IStatefulDevice per-tick chip capture** — IStatefulDevice (StateName, StateSize, CaptureState(Span), DecodeState) is implemented by VIC/SID/CIA/PLA. The recorder preallocates per-slot buffers (zero-alloc capture) and deep-copies on Snapshot; the pump registers Machine.Devices.All.OfType<IStatefulDevice>() before subscribing and the host decodes in the same order.
Scope: layer-1+
**Acceptance Criteria:**
- [x] A captured tick carries each device's StateSize bytes; capture allocates nothing on the hot path
- [x] Snapshot deep-copies chip state so ring reuse cannot corrupt an earlier snapshot

## TR-CORE-CYCLE-001

**Sub-Cycle Bus-Phase Accuracy Matching VICE x64sc Behavior** — Managed C64 PAL execution must preserve C64 bus, CPU, and VIC-II ordering semantics while optimizing RunFrame so performance changes do not alter cycle-observable behavior.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Lockstep and checkpoint suites continue to pass after RunFrame performance optimizations. (evidence: dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter FullyQualifiedName~Lockstep|FullyQualifiedName~Checkpoint => 333 passed, 0 failed, 0 skipped)
- [x] Focused BasicBus/C64MemoryMap/VideoRenderer/VideoSurface/SID tests pass with no failed or skipped tests. (evidence: focused gate => 182 passed, 0 failed, 0 skipped)

## TR-CORE-DET-001

**Bit-Exact Reproducibility Given Same Initial State and Inputs** — Given the same initial emulator state, ROM set, mounted media, inputs, and timing model, managed C64 PAL execution must produce deterministic CPU, bus, video, audio, interrupt, and memory-observable results.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Lockstep and checkpoint suites continue to pass after performance optimizations. (evidence: lockstep/checkpoint gate => 333 passed, 0 failed, 0 skipped)
- [x] Optimizations do not change public observable machine state signatures or behavior contracts. (evidence: PR #3 keeps public API signatures stable and correctness gates green)

## TR-CPU-TICK-001

**Per-instance CPU executed-cycle counter and target clock** — ICpu exposes ExecutedCycles and TargetClockHz per instance; MachineState carries a per-CPU breakdown; UpdatePerformanceCounters uses the primary CPU executed cycles divided by its target clock.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] ICpu exposes a per-instance ExecutedCycles counter and TargetClockHz.
- [ ] MachineState carries a per-CPU breakdown (id, executedCycles, targetHz) for host and peripheral CPUs.

## TR-CYCLE-001

**VIC-II cycle counter frame-periodic** — Managed VIC-II CycleCounter advances by exactly 19,656 per PAL frame. VICE vicii-cycle.c:576-598.
Scope: layer-1+

## TR-DEPS-202607-001

**Dependency currency policy (2026-07 upgrade wave)** — Every NuGet dependency tracks the highest STABLE version mutually compatible with net10.0 and all other dependencies; Directory.Packages.props (CPM) is the single version source for all projects including tests/ViceSharp.AiReview.Tests; all packages resolve from nuget.org when published there (vendored nuget-local feed retires once SharpNinja.aiUnit and SharpNinja.Avalonia.RemoteControl.* map to nuget.org). Prereleases excluded. Locked targets and ceiling evidence recorded in the approved 2026-07-08 upgrade plan.
Scope: layer-1+

## TR-DET-001

**Bit-Exact Reproducibility Given Same Initial State and Inputs** — Given the same initial emulator state, ROM set, mounted media, inputs, and timing model, managed C64 PAL execution must produce deterministic CPU, bus, video, audio, interrupt, and memory-observable results.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Lockstep and checkpoint suites continue to pass after performance optimizations. (evidence: dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter FullyQualifiedName~Lockstep|FullyQualifiedName~Checkpoint => 333 passed, 0 failed, 0 skipped)
- [x] Optimizations do not change public observable machine state signatures or behavior contracts. (evidence: PR #3 keeps public API signatures stable and lockstep/checkpoint evidence green)

## TR-DRV-EDGE-001

**1541 drive motor 300,000-cycle ramp before rotation** — IecDrive.Tick() enforces 300,000-cycle motor ramp-up (300ms at 1MHz drive clock) before MotorRotationCycles advances. Motor off resets ramp. VICE drive/drive.c.
Scope: layer-1+

## TR-DRVLIFE-001

**Drive attach/detach lifecycle** — Enabling a drive creates/starts the drive instance, registers it on the clock and connects it to the always-on IEC bus; disabling stops it, unregisters from the clock and disconnects from the bus.
Scope: layer-1+

## TR-GRPC-BOUNDARY-001

**Host Protocol Boundary DTOs** — Host protocol and gRPC adapters expose emulator status, media, command, monitor, and settings state through DTOs without UI clients reaching into emulator internals.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Status, media, command, monitor, and settings requests remain DTO-only across the host protocol boundary.
- [ ] The status DTO includes IEC activity telemetry without removing existing status fields.
- [ ] gRPC service adapters and the Avalonia gRPC client preserve IEC activity telemetry on status round trips.

## TR-HOST-STATUS-001

**Measured Emulator Runtime Telemetry** — Host status telemetry shall report runtime, media, automation, and IEC activity from authoritative runtime state without mutating it.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Status responses include existing runtime telemetry and IEC activity fields.
- [ ] IEC activity is derived from authoritative IEC line changes or an equivalent host-owned bus signal monitor.
- [ ] Status polling does not mutate machine, drive, or bus state.

## TR-IECDECODE-001

**IEC protocol decoder** — Pure state machine over trace samples emitting decoded IEC events: ATN command frame, LISTEN/TALK + device, OPEN/secondary/channel, data bytes with EOI, turnaround, timeout/error.
Scope: layer-1+

## TR-IEC-EDGE-001

**IEC bus ATN-response within 985-cycle spec window** — IecBus.Tick() asserts CLK and DATA low within 985 cycles (Tat=1ms at PAL 985,248Hz) of ATN falling edge. Releases both on ATN rising edge. VICE iecbus/iecbus.c:247-266.
Scope: layer-1+

## TR-IECELEC-001

**Faithful IEC serial electrical model with native parity** — CIA2 reads the live open-collector IEC bus through the C64 inverting buffers; idle DATA-low is supplied by the true-drive 1541 holding DATA. C64GS (no IEC) keeps disconnected->low. Idle reads match native ($47 normal C64 / $07 C64GS) and native CIA2-IO lockstep stays green.
Scope: layer-1+

## TR-IECSPY-001

**IInterSystemBus.Snapshot API** — IInterSystemBus exposes Snapshot() returning BusSnapshot (per-line level + pullers, talking endpoints, attached endpoints). Read-only. DONE.
Scope: layer-1+

## TR-IECTRACE-001

**IEC trace recorder (edge + step marks)** — IecBusTraceRecorder subscribes to bus.LineChanged, cycle-stamps each edge from SystemClock plus a sample per step boundary, into a bounded ring; re-derives deterministically after rewind; inactive unless the monitor is open.
Scope: layer-1+

## TR-MED-FFMPEG-001

**FfmpegVideoRecorder over two loopback TCP sockets** — External ffmpeg process fed raw BGRA + s16le via BackgroundByteWriter; mirrors VICE ffmpegexedrv. Pre-connect audio buffered + flushed.
Scope: layer-1+

## TR-MED-SOUND-001

**CaptureAudioTap + WavAudioRecorder** — Runtime-swappable tap in the SID->output path feeds a 16-bit PCM WAV recorder whose file writes run off the worker.
Scope: layer-1+

## TR-MED-VIDEO-001

**BMP-sequence sink with all/unique dedup, off-worker writes** — FrameSequenceCapture writes numbered 24-bit BMPs via a background queue; unique mode skips byte-identical consecutive frames.
Scope: layer-1+

## TR-MVVM-001

**TR-MVVM-001** — Placeholder requirement backfilled for TODO link TR-MVVM-001.
Scope: layer-1+

## TR-PACESEL-STRAT-001

**Live-switchable pacing strategy plumbing** — EmulationGateStrategies provides canonical ids + gate factory. EmulationPumpService.SetStrategy swaps the gate on the worker thread (no restart). LimiterSettingsDto.PacingStrategy flows through proto/gRPC, SettingsServiceHost applies it live to the pump and round-trips it, and the UI exposes a dropdown.
Scope: layer-1+
**Acceptance Criteria:**
- [x] EmulationGateStrategies.CreateGate/Normalize/DisplayName map ids to gates, defaulting to Semaphore
- [x] SetStrategy swaps the gate live; SettingsServiceHost applies and round-trips PacingStrategy

## TR-PERF-ALLOC-001

**Zero Managed Allocations Per RunFrame Hot Path** — Sustained managed C64 PAL RunFrame execution must avoid per-frame managed allocations so 50 Hz operation does not create GC pressure.
Scope: layer-1+
**Acceptance Criteria:**
- [x] The measured C64 PAL RunFrame loop reports zero current-thread allocations over the measured frame window. (evidence: RunFramePerfProbe 60 600: allocated=0 bytes)
- [x] BenchmarkDotNet run reports no managed allocation for C64PalRunFrameBenchmark. (evidence: BenchmarkDotNet C64PalRunFrameBenchmark: Allocated column reported no managed allocation)

## TR-PERF-AOT-001

**NativeAOT-Compatible RunFrame Hot Path** — RunFrame hot-path implementation must remain compatible with NativeAOT constraints: no new reflection, expression trees, dynamic code generation, or LINQ allocations on the hot call graph.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Performance optimizations introduce no reflection, expression trees, or dynamic code generation in the RunFrame call graph. (evidence: Code review of PR #3 optimized BasicBus and VideoRenderer with direct/static paths only)
- [x] Performance optimizations avoid LINQ and closure allocation in the measured RunFrame hot path. (evidence: PR #3 uses loops and span fills in hot path; RunFramePerfProbe reports allocated=0 bytes)

## TR-PUBSUB-PERF-001

**High-Performance Zero-Allocation Pub/Sub** — ViceSharp shall implement the internal Pub/Sub interconnect with fixed-capacity message storage, 64-byte inline payloads, a MessageKind discriminant, preallocated subscriber route arrays, synchronous publish/delivery, no boxing, and zero managed heap allocation on the steady-state hot path.
Scope: layer-1+
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
Scope: layer-1+

## TR-REMOTECTRL-SERVER-001

**Embeddable RemoteControl server integration + vendored feed** — ViceSharp.Avalonia references SharpNinja.Avalonia.RemoteControl.Server (0.7.3), vendored into a repo-local NuGet feed (nuget-local/ + NuGet.config package source mapping) so restore is reproducible on Azure DevOps/CI without the sibling source repo. The Avalonia App builds a DI ServiceCollection, calls AddAvaloniaRemoteControl, registers an IRemoteControlRootProvider returning the desktop MainWindow, and attaches the host to the classic-desktop lifetime. Startup is gated behind VICESHARP_REMOTECONTROL_ENABLE and fails closed without a bearer token. Requires Avalonia >= 12.0.3 and Grpc >= 2.80.0 (central package versions bumped accordingly).
Scope: layer-1+

## TR-REVEXEC-001

**Frame-snapshot ring reverse execution** — A frame-granular snapshot ring keyed by cycle backs RewindCycle/RewindFrame: restore nearest snapshot <= target, re-run deterministically to target. Replaces the NotImplemented host stubs. Bounded memory.
Scope: layer-1+

## TR-SID-AMPLIFY-001

**SID output amplify/clip + external-filter enable (float host seam)** — PLAN-VICEPARITY-001 S12 aligns the managed SID audio-output stage with reSID. GenerateSample now applies reSID amplify(input, scaleFactor) = clip((scaleFactor*input)/2) (sid.cc:54-57) on the pre-amplify external-filter output SID::output() (=extfilt.output(), sid.h:190-194), then scales by 1/2^15 for the float [-1,1] host contract - lossless (every short is exact in float, /2^15 is exact) and C# int division truncates toward zero exactly like C++. The 6581 amplify scaleFactor is 3 and the 8580 is 5 (set_chip_model, sid.cc:86,121), so the 6581 mixes 1.5x louder, matching VICE. ClipPcm16 saturates to signed 16-bit [-32768,32767]. The external filter gained an enable flag (default true, mirroring VICE resid.cc:200 enable_external_filter(true)); the disabled branch passes through (Vlp=Vi<<11, Vhp=0, extfilt.h:100-105) and exists only to lock FR-SID-OUTPUT AC-03 (VICE always enables). CaptureAudioTap is unchanged (the amplify is on the float host path, not the capture tee). Verified: OUTPUT-01 composite lockstep vs the c64 oracle (SID::output() bit-exact + amplified-float identity); SidDigiPlaybackTests (range/relative assertions) survive the 1.5x change; LockstepValidation + audio suites + Category=Determinism green.
Scope: layer-1+

## TR-SIDAUDIO-CLOCK-001

**SID ticks once per phi2 cycle** — Sid6581.ClockDivisor is 1 so the SystemClock ticks the SID every master cycle; the 24-bit accumulator advances by Frequency per tick and the ADSR rate tables are resid phi2-cycle values. ConfigureAudioClock divides by ClockDivisor so the 44.1 kHz sample rate self-corrects.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Sid6581.ClockDivisor returns 1
- [x] Direct-Tick SID tests are divisor-independent; only SystemClock-driven calibration changed

## TR-SIDEBARUI-LAYOUT-001

**Anchor-derived collapse expander + wrap-panel button groups** — AttachPanelViewModel exposes CollapseExpanderDock (Avalonia Dock) and CollapseGlyph derived from DockSide and raised on change. AttachPanelView docks a full-height collapse Button on CollapseExpanderDock and re-docks it on change. The tab buttons, Monitor buttons and Settings action buttons use WrapPanel with per-button margins.
Scope: layer-1+
**Acceptance Criteria:**
- [x] CollapseExpanderDock is Dock.Right when DockSide is Left and Dock.Left when DockSide is Right
- [x] Changing DockSide raises PropertyChanged for CollapseExpanderDock and CollapseGlyph

## TR-SID-EDGE-004

**reSID waveform DAC centering and normalized mix scale** — Sid6581 and Sid8580 subtract the model-specific waveform zero level before ADSR envelope application, preserve no-waveform silence relative to the D418 baseline, and normalize mixed output before host audio submission.
Scope: layer-1+

## TR-SID-ORACLE-002

**SID exact-oracle sampling method + buffered clock exports** — Two shim exports added for SID parity beyond the single-cycle oracle: vice_sid_exact_set_sampling(machine, method, sampleFreq, passFreq, filterScale) reconfigures the private reSID engine sampling method (0=FAST 1=INTERPOLATE 2=RESAMPLE 3=RESAMPLE_FASTMEM; clock_freq fixed 985248.0 PAL) so the 8580 SAMPLE_FAST write pipeline is observable; vice_sid_exact_clock_buffered(machine, cycles, buffer, len, out remaining) drives reSID SID::clock(delta_t, buf, n) for the S13 fixed-point resampler FIR parity. Implemented as resid_shim_set_sampling / resid_shim_clock_buffered in native/vice/vice/src/sid/resid.cc, wrapped in native/vice-shim.c under g_state_lock + is_active_machine, declared in vice-shim.h, bound via ViceNative LibraryImport + ViceNativeBridge. 8580 lockstep uses the c64c machine selector (SidModel 1 = MOS8580). reSID filter dither is zeroed in the shim for bit-exact comparability. Build: resid.o archives into src/sid/libsid.a; delete both before rebuilding. Landed by commit 93aef16 (dll SHA256 b743c666).
Scope: layer-1+

## TR-SID-RESAMPLE-001

**SID fixed-point sampling engine port (fast/interpolate/Kaiser-FIR resample)** — PLAN-VICEPARITY-001 S13 ports reSID's sampling engine (sid.cc:542-1038, sid.h:140-179) into src/ViceSharp.Chips/Audio/Sid6581.Sampling.cs. Constants FirN=125, FirRes=285, FirShift=15, RingSize=1<<14, FixpShift=16. SetSamplingParameters is a statement-for-statement port of set_sampling_parameters: the resampling constraint checks, cycles_per_sample = (int)(clock/sample*(1<<16)+0.5) 16.16 fixed point, the mirrored ring short[RingSize*2], the Kaiser-windowed sinc FIR table (I0 Bessel, beta=0.1102*(A-8.7), the only legitimate float stage, rounded to short round-half-away-from-zero). ClockBuffered mirrors SID::clock(delta_t, buf, n): clock_fast, clock_interpolate, clock_resample. It calls the managed single-cycle Tick() where reSID calls clock() and reads CycleExternalFilterOutput where reSID calls output(), so the emitted short stream is bit-exact vs the oracle. RESAMPLE_FASTMEM deliberately NOT ported. All buffers preallocated; zero per-sample allocation. Verified by OUTPUT-08..13 vs SidExactClockBuffered (TR-SID-ORACLE-002) on both 6581 (c64) and 8580 (c64c). Slice LW (commit 4924b94) COMPLETED the live-audio wiring: the live path now drives this SAMPLE_RESAMPLE engine one cycle per Tick (EmitLiveResampleTick), Resample-always (VICE x64sc default), bit-exact vs the buffered pull and zero-allocation; SetRelativeSpeed scales the 16.16 cadence only (warp = pitch, synthesis untouched). Benchmark SidSamplingBenchmarks: live tail ~0.30x real-time (100% speed holds). Post-review fix 9b00899 handles the zero-cycle-window underflow below ~4.5% warp. PLAN-VICEPARITY-001 CLOSED 466/466.
Scope: layer-1+

## TR-SNAPFULL-001

**Complete machine snapshot/restore with deterministic round-trip** — GetState()+restore capture all execution-affecting state (CPU incl. pending IRQ/NMI + pipeline, VIC sequencer, CIA timer pipelines + TOD, SID, PLA/processor port, memory, and the 1541 drive + IEC bus when present). Invariant: restore then run N cycles equals continuous run N (full MachineState + memory equality).
Scope: layer-1+

## TR-SNDREG-GATE-001

**Sound back-pressure regulator in ViceEmulationGate** — IAudioChip exposes QueuedSampleCount and IsAudioTimingSource (Sid6581 overrides). ViceEmulationGate.EvaluateSound decides NotTimingSource/Advance/BackPressure; Tick applies warp->sound->vsync precedence and reports LastRegulator; HighWaterSamples is about 46 ms.
Scope: layer-1+
**Acceptance Criteria:**
- [x] EvaluateSound returns BackPressure at or over high-water, Advance below, NotTimingSource when inactive
- [x] Gate Tick selects Sound/Vsync/Warp and exposes it via LastRegulator

## TR-SYS-SCHED-001

**Single-worker interleave scheduling with IEC bus-event sync** — One emulation worker interleaves systems with per-system deficit pacing and resynchronizes at InterSystemBus.LineChanged; lazy sync-on-IEC-access retained as the fine-grained backstop.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Each system advances on its own clock via a self-correcting deficit pacer.
- [ ] Systems resync at LineChanged so IEC bus edges are observed in order before a read.

## TR-TAP-EDGE-001

**Datasette sense line and record mode** — Datasette.SenseLine = false when PlayPressed or RecordPressed (CIA1  bit 4 active-low). TryWritePulse stores pulse when MotorEnabled && RecordPressed. RecordedPulseCount tracks stored pulses.
Scope: layer-1+

## TR-TAPE-EDGE-001

**Datasette motor 32,000-cycle ramp before pulse delivery** — Datasette.Tick() enforces 32,000-cycle motor ramp (MOTOR_DELAY=32000, datasette/datasette.c:62) before TryReadNextPulse delivers pulses. Ramp only activates when Tick() is used as timing. Motor off resets ramp.
Scope: layer-1+

## TR-TICKHIST-CAPTURE-001

**Write-delta capture and reconstruction** — BasicBus publishes MemoryWriteEvent (pre-write byte, gated on a live subscriber); the CPU publishes instruction-completed events; one LockFreePubSub per machine is wired in ArchitectureBuilder and exposed via IMachine.PubSub. TickHistoryRecorder keeps a 100-deep ring bundling write-deltas per instruction; reconstruction reverse-applies later ticks' writes to the current paused RAM.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Bus publishes MemoryWriteEvent with the pre-write byte only when subscribed
- [x] Recorder ring is bounded to 100 and reconstruction recovers as-of-tick RAM

## TR-TICKHIST-PERF-001

**Tick-history capture is opt-in (zero default overhead)** — The time-travel recorder must stay unsubscribed by default so per-instruction chip-state capture and per-write delta recording impose zero overhead; it is armed only when the History panel reads the trace.
Scope: layer-1+
**Acceptance Criteria:**
- [x] With recording disabled (default) advancing the emulation pump does not subscribe the recorder and tick history stays empty.
- [x] Reading the tick history via GetTickHistory arms recording so subsequent emulation captures.
- [x] BasicBus publishes MemoryWriteEvent only when SubscriptionCount is greater than zero so the default path has zero per-write cost.

## TR-TICKHIST-RPC-001

**Tick-history monitor RPCs** — IMonitorService gains GetTickHistory, ReadMemoryAtTick (reconstructed) and GetChipStateAtTick, with proto/gRPC messages and mappers. IHostProtocolClient surfaces ReadRegisters/ReadMemory/Disassemble plus the three new calls.
Scope: layer-1+
**Acceptance Criteria:**
- [x] GetTickHistory returns index-ordered ticks; ReadMemoryAtTick at the newest tick equals current memory
- [x] Out-of-range tick index returns InvalidArgument

## TR-TICKHIST-UI-001

**History tab and debug screen** — SidebarTab.History hosts TickHistoryView bound to TickHistoryViewModel; IsPaused is fed via AttachPanelViewModel.ApplyStatus. Selecting a tick while paused shows registers, each chip's decoded state, and a reconstructed scrolling hex dump.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Refresh lists ticks newest-first; inspect while paused opens the debug screen
- [x] Inspect while running does not open the debug screen

## TR-UIAXAML-001

**All Avalonia views authored in AXAML + MVVM** — Every Avalonia view in ViceSharp.Avalonia is authored declaratively in AXAML with MVVM bindings (no imperative control-tree construction in code-behind), to ease maintenance. The shell window, the sidebar, each per-peripheral control, and the settings panel are AXAML UserControls/Windows bound to view models; code-behind is limited to InitializeComponent and thin glue.
Scope: layer-1+

## TR-UIAXAML-VIEWS-001

**All Avalonia views authored in AXAML + MVVM** — Every Avalonia view in ViceSharp.Avalonia is authored declaratively in AXAML with MVVM bindings (no imperative control-tree construction in code-behind), to ease maintenance. The shell window, the sidebar, each per-peripheral control, and the settings panel are AXAML UserControls/Windows bound to view models; code-behind is limited to InitializeComponent and thin glue.
Scope: layer-1+

## TR-UI-SHELL-001

**Avalonia Emulator Control Shell** — The Avalonia shell shall bind host protocol DTOs into status bar and sidebar ViewModels without reaching into emulator internals.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The shell has status bar and peripherals sidebar surfaces that display IEC activity.
- [ ] ViewModels consume host client abstractions and DTOs rather than emulator internals.
- [ ] ViewModel tests cover peripherals and status active-to-idle IEC transitions.

## TR-VIC-EDGE-001

**VIC-II native screen-RAM visible-frame checkpoint** — Native VICE screen RAM (-) survives one full PAL frame (19,656 cycles) unchanged. VIC-II c-access DMA is read-only. VICE vicii-fetch.c:135-166.
Scope: layer-1+

## TR-VIC-EDGE-002

**VIC-II PAL raster position frame-periodic** — Managed PAL VIC-II raster (rasterLine, rasterX) returns to identical position after exactly one frame (312x63=19,656 cycles). VICE vicii-cycle.c:576-598.
Scope: layer-1+

## TR-VIC-EDGE-003

**VIC-II RC window state machine cycle-accurate** — VIC-II row counter (RC) and video counter (VC) state machine matches VICE viciisc/vicii-cycle.c:541-563. VC update at RasterX=13: vc=vcBase, vmli=0, bad line sets rc=0. RC update at RasterX=57: rc==7 sets idle; if not idle or bad line, rc=(rc+1)&7.
Scope: layer-1+

## TR-VIC-EDGE-004

**VIC-II non-PAL sprite DMA window native checkpoint** — All 5 non-PAL VIC-II models (Mos6567, Mos8562, Mos6567R56A, Mos6572, Mos8565) fire sprite-3 DMA near line 0x50. Managed IsCpuCycleStolen true at (SpriteY+1, rasterX 0). VICE vicii-chip-model.c:272-403/437-566.
Scope: layer-1+

## TR-VIC-EDGE-005

**VIC-II c-access DMA read-only semantics** — VIC-II matrix DMA c-access reads screen RAM without write side-effects. Screen RAM bytes written before a frame remain unchanged after. VICE vicii-fetch.c:135-166.
Scope: layer-1+

## TR-VIC-EDGE-006

**VIC-II screen RAM immediate read-back** — ViceNativeBridge ReadMemory/WriteMemory roundtrip for - returns written value in zero elapsed cycles. VICE c-access is read-only.
Scope: layer-1+

## TR-VSFLOCKSTEP-RESUME-001

**Backward-compatible snapshot module read and VIC-II model alignment** — maincpu_snapshot_read_module (mainc64cpu.c, the single-cycle reader used by x64sc) version-guards ane_log_level/lxa_log_level (module >= v1.3) and maincpu_jammed (>= v1.4) so v1.2 MAINCPU modules from an older x64sc resume without overrunning; ba_low_flags stays the last field before the interrupt sub-modules. The shim (vice-shim.c) pre-scans the snapshot's VIC-II model byte and sets the VICIIModel resource before machine_read_snapshot (6569 vs 8565 are both PAL, so SID type is unchanged) and normalises snapshot_last_error to 0 on a successful read. The mainc64cpu.c change is carried in native/patches/vice-shim-runtime.patch so it survives a fresh submodule checkout.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A v1.2 MAINCPU module resumes (ane/lxa/jammed skipped by version) and a current v1.4 module is unaffected; verified by full external-.vsf load and the unchanged round-trip test.
- [ ] The fix is reproduced by build-vice-shim.sh applying vice-shim-runtime.patch (reverse-apply check passes against the working tree).

