# Technical Requirements (MCP Server)

## PERF-SPRITE-DMA-OPT-001

**PERF-SPRITE-DMA-OPT-001** — Placeholder requirement backfilled for TODO link PERF-SPRITE-DMA-OPT-001.

## TEST-VIC-001

**TEST-VIC-001** — Placeholder requirement backfilled for TODO link TEST-VIC-001.

## TR-CYCLE-001

**VIC-II cycle counter frame-periodic** — Managed VIC-II CycleCounter advances by exactly 19,656 per PAL frame. VICE vicii-cycle.c:576-598.

## TR-DRV-EDGE-001

**1541 drive motor 300,000-cycle ramp before rotation** — IecDrive.Tick() enforces 300,000-cycle motor ramp-up (300ms at 1MHz drive clock) before MotorRotationCycles advances. Motor off resets ramp. VICE drive/drive.c.

## TR-GRPC-BOUNDARY-001

**TR-GRPC-BOUNDARY-001** — Placeholder requirement backfilled for TODO link TR-GRPC-BOUNDARY-001.

## TR-IEC-EDGE-001

**IEC bus ATN-response within 985-cycle spec window** — IecBus.Tick() asserts CLK and DATA low within 985 cycles (Tat=1ms at PAL 985,248Hz) of ATN falling edge. Releases both on ATN rising edge. VICE iecbus/iecbus.c:247-266.

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

## TR-TAP-EDGE-001

**Datasette sense line and record mode** — Datasette.SenseLine = false when PlayPressed or RecordPressed (CIA1  bit 4 active-low). TryWritePulse stores pulse when MotorEnabled && RecordPressed. RecordedPulseCount tracks stored pulses.

## TR-TAPE-EDGE-001

**Datasette motor 32,000-cycle ramp before pulse delivery** — Datasette.Tick() enforces 32,000-cycle motor ramp (MOTOR_DELAY=32000, datasette/datasette.c:62) before TryReadNextPulse delivers pulses. Ramp only activates when Tick() is used as timing. Motor off resets ramp.

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

