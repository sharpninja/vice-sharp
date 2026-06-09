# Testing Requirements (MCP Server)

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



## TEST-IEC-TIMING

### TEST-IEC-TIMING-001

After setting IecBus.Atn=false and calling Tick() up to 985 times, Clock==false and Data==false. After ATN release and Tick() up to 985 times, both return true. 5 pass.



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


## TEST-TAPE-RAMP

### TEST-TAPE-RAMP-001

With tape inserted, MotorEnabled=true, PlayPressed=true: TryReadNextPulse returns false during first 12,808 Tick() calls; returns true after 32,000 Tick() calls. Motor off/on resets ramp. 4 pass.



## TEST-TAPE-SENSE

### TEST-TAPE-SENSE-001

SenseLine==false when PlayPressed or RecordPressed, true otherwise. TryWritePulse returns true only when MotorEnabled && RecordPressed, incrementing RecordedPulseCount. 7 pass.



## TEST-UI

### TEST-UI-001

Focused ViewModel and protocol tests shall prove IEC activity appears in both peripherals and status surfaces from host telemetry.

**Acceptance Criteria:**
- [ ] ViewModel tests prove peripherals panel drive entries show IEC active and idle states from host telemetry.
- [ ] ViewModel tests prove status bar IEC activity uses the same source while preserving status fields.
- [ ] Focused UI tests run with zero failed and zero skipped tests.


## TEST-VIC-CHECKPOINT

### TEST-VIC-CHECKPOINT-001

Managed PAL frame periodic raster/cycle-counter (2 Facts), native screen-RAM roundtrip and one-frame DMA read-only (2 ViceFacts), sprite-3 DMA window for 5 models (5 ViceTheory). All 9 pass.



## TEST-VIC-RC

### TEST-VIC-RC-001

After writing DEN=1/YSCROLL=0/1 and advancing to specific rasterLine/rasterX, CurrentRowCounter and IsGraphicsIdle match VICE viciisc/vicii-cycle.c:541-563 expectations. All 11 pass.
