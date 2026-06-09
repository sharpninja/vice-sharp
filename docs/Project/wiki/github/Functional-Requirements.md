# Functional Requirements (MCP Server)

## ARCH-TRUEDRIVE-1541-002 True-Drive 1541 IEC timing and motor ramp

IecBus.Tick() implements ATN-response state machine (CLK/DATA within 985 cycles of ATN assert). IecDrive.Tick() implements 300,000-cycle motor ramp before rotation. IecDrive.ReadSector(18,0) returns BAM bytes from D64. VICE iecbus.c:247-266, drive/drive.c.

## BACKFILL-MEDIA-001 Media devices (D64, tape) I/O functional parity

D64DiskImageDevice sector R/W, motor ramp, BAM read all implemented. Datasette motor ramp, sense line, and record mode complete. Closes IEC ATN timing gap for Phase 1.

## BACKFILL-VIDEO-001 VIC-II visible-frame parity (RC window, DMA checkpoints, screen-RAM)

VIC-II RC/VC state machine matches VICE cycle-accurate behavior (RC window). Native checkpoints validate sprite DMA for all 5 non-PAL models. Screen RAM survives one PAL frame unchanged. VICE viciisc/vicii-cycle.c:541-563, vicii-fetch.c:135-166.

## FR-CFG-001 FR-CFG-001

Placeholder requirement backfilled for TODO link FR-CFG-001.

## FR-CFG-005 FR-CFG-005

Placeholder requirement backfilled for TODO link FR-CFG-005.

## FR-DRV-005 IEC Serial Bus Protocol

The emulator shall expose an active-low IEC serial bus with ATN, CLK, DATA, and SRQ line behavior that drives 1541/D64 operations and observable bus activity.
**Acceptance Criteria:**
- [ ] IEC bus endpoints resolve ATN, CLK, DATA, and SRQ as active-low wired-OR lines.
- [ ] Mounted D64 directory and file operations generate observable IEC line activity from the bus signal source.
- [ ] Mounted D64 directory and file operations complete with correct data through the IEC path.

## FR-HOST-006 Host Runtime Status and Control Telemetry

The host runtime shall expose emulator status telemetry for runtime state, timing, media, automation, and IEC bus activity to clients.
**Acceptance Criteria:**
- [ ] Host status responses include existing runtime fields including session, run state, cycle, frame, model, limiter, and automation status.
- [ ] Host status responses include IEC activity derived from emulator bus traffic and safe for UI polling.
- [ ] Status polling does not mutate emulator, drive, or bus state.

## FR-PUBSUB-001 Internal Pub/Sub Event Bus

ViceSharp shall provide an internal synchronous topic-based Pub/Sub event bus for transient intra-frame device-to-device communication, including interrupts, NMI, bus availability, address-enable control, DMA, clock, and state notifications. The bus exposes typed publish and subscribe APIs, raw payload compatibility, deterministic registration-order delivery, handle-based unsubscription, frame reset behavior, and message pool integration.
**Acceptance Criteria:**
- [x] Public IPubSub exposes typed Publish/Subscribe, raw payload compatibility, Unsubscribe by SubscriptionHandle, Flush, FrameReset, and SubscriptionCount. (evidence: src/ViceSharp.Abstractions/IPubSub.cs)
- [x] Publish delivers synchronously to subscribers in registration order for each topic. (evidence: tests/ViceSharp.TestHarness/LockFreePubSubTests.cs)
- [x] Message pool exhaustion, return, and frame reset behavior are covered by focused tests. (evidence: tests/ViceSharp.TestHarness/LockFreePubSubTests.cs)

## FR-UI-002 Emulator Status and Machine Control Bar

The UI shall provide a status and control bar for runtime state, controls, performance fields, and IEC activity.
**Acceptance Criteria:**
- [ ] The status bar presents existing run state, cycle, frame, model, limiter, automation, and control commands.
- [ ] The status bar presents IEC activity from host telemetry without replacing existing fields.
- [ ] The status bar remains usable without stealing emulator focus for normal display interaction.

## FR-UI-003 Collapsible Tabbed Emulator Sidebar

The UI shall provide a dockable tabbed sidebar with peripherals and settings surfaces driven by host protocol state.
**Acceptance Criteria:**
- [ ] The peripherals tab exposes drive attachment state and media commands for configured drives.
- [ ] Drive entries expose IEC active/idle state from the same host telemetry source as the status bar.
- [ ] Drive IEC activity returns idle in the peripherals tab after bus activity settles.

## FR-VIC-001 VIC-II PAL raster cycle counter and frame-periodic behavior

Managed PAL VIC-II advances rasterLine/rasterX cyclically by exactly 312*63=19,656 ticks per frame. CycleCounter increments monotonically. VICE vicii-cycle.c:576-598.

## FR-VIC-002 FR-VIC-002

Placeholder requirement backfilled for TODO link FR-VIC-002.

## FR-VIC-003 FR-VIC-003

Placeholder requirement backfilled for TODO link FR-VIC-003.

## FR-VIC-004 FR-VIC-004

Placeholder requirement backfilled for TODO link FR-VIC-004.

## FR-VIC-005 FR-VIC-005

Placeholder requirement backfilled for TODO link FR-VIC-005.

## FR-VIC-006 FR-VIC-006

Placeholder requirement backfilled for TODO link FR-VIC-006.

## FR-VIC-007 FR-VIC-007

Placeholder requirement backfilled for TODO link FR-VIC-007.

## FR-VIC-008 VIC-II FLI forced bad line RC window interrupt

Changing YSCROLL mid-frame to match current raster line low 3 bits forces a bad line. VC update at cycle 13 resets rc=0 and clears idle_state, interrupting the idle window. VICE viciisc/vicii-cycle.c:51-60.

## FR-VIC-010 FR-VIC-010

Placeholder requirement backfilled for TODO link FR-VIC-010.

## RUNTIME-TAPE-002 Datasette motor ramp + sense line + record mode

Datasette.Tick() enforces MOTOR_DELAY=32,000-cycle ramp (datasette.c:62) before pulse delivery when Tick is timing mechanism. SenseLine=!PlayPressed||!RecordPressed (CIA1  bit 4). TryWritePulse stores pulses in record mode.

