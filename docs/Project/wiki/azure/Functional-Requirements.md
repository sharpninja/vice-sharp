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

## FR-CHIPSTATE-001 Per-tick full chip state capture

Each captured tick snapshots the full internal state of every stateful chip (VIC, SID, CIA, PLA) for display in the debug screen.
**Acceptance Criteria:**
- [x] VIC, SID, CIA and PLA each implement IStatefulDevice
- [x] Each chip's state is captured per tick with zero hot-path allocation
- [x] Captured state decodes into named register/field values
- [x] The debug screen shows each chip's decoded state at the selected tick

## FR-CPUTICK-001 Per-CPU independent tick counter and per-CPU speed metric

Each CPU in the emulator keeps its own independent executed-cycle counter and the displayed speed is that CPU's executed-cycle rate versus its own target clock.
**Acceptance Criteria:**
- [ ] Each CPU ExecutedCycles increments once per executed cycle and not on a stolen or skipped cycle.
- [ ] Per-CPU speed percent equals delta-executed over delta-wall over targetHz; the C64 primary reads about 95-100 percent at real time and the drive about 100 percent when running.
- [ ] The status surface lists per-CPU rate entries for the host and each peripheral CPU distinctly.
- [ ] Machine reset zeroes every CPU's executed-cycle counter.

## FR-DRV-005 IEC Serial Bus Protocol

The emulator shall expose an active-low IEC serial bus with ATN, CLK, DATA, and SRQ line behavior that drives 1541/D64 operations and observable bus activity.
**Acceptance Criteria:**
- [ ] IEC bus endpoints resolve ATN, CLK, DATA, and SRQ as active-low wired-OR lines.
- [ ] Mounted D64 directory and file operations generate observable IEC line activity from the bus signal source.
- [ ] Mounted D64 directory and file operations complete with correct data through the IEC path.

## FR-DRVLED-001 Per-drive activity LED

Each drive card shows an activity LED sourced from that drive's VIA2 (1C00) port B bit 3, set by the 1541 DOS ROM (VICE led_status model), independent of IEC bus traffic.

## FR-DRVTRUE-001 Per-drive True Drive toggle

Each IEC drive's UI exposes a True Drive toggle. Enabled = cycle-accurate emulated 1541 (6502+VIA+DOS over IEC); disabled (default) = lightweight simulated/buffered drive. Mirrors VICE per-unit DriveTrueEmulation / Fidelity TrueDevice vs Buffered. The runtime honors it (gated true-drive coordinator path), default off so existing behavior is unchanged.

## FR-HOST-006 Host Runtime Status and Control Telemetry

The host runtime shall expose emulator status telemetry for runtime state, timing, media, automation, and IEC bus activity to clients.
**Acceptance Criteria:**
- [ ] Host status responses include existing runtime fields including session, run state, cycle, frame, model, limiter, and automation status.
- [ ] Host status responses include IEC activity derived from emulator bus traffic and safe for UI polling.
- [ ] Status polling does not mutate emulator, drive, or bus state.

## FR-IECHOTPLUG-001 Hot drive add and remove and live device renumber

Drives can be turned on and off and have their device number changed at runtime without restarting the emulator.
**Acceptance Criteria:**
- [ ] A drive attached to a running session answers on the bus with no restart.
- [ ] A detached drive's pull contributions are removed and line states recompute.
- [ ] A drive's device number (8 to 11) can be changed at runtime and it answers the new number.

## FR-IECLOAD-001 True-drive 1541 LOAD over IEC

A single-system C64 with a true-drive 1541 attached completes LOAD"*",8,1, LOAD"$",8 and SAVE over the IEC bus, talking to the drive's DOS ROM via the faithful serial electrical model.

## FR-IECMON-001 IEC bus monitor (scope view)

Dedicated logic-analyzer panel showing a timing diagram of the IEC lines over emulator time, colored by which device drives each segment, with decoded IEC protocol bands, cursor/zoom/scroll, synced to forward step and reverse step.

## FR-IECSPY-001 IEC bus snapshot / spy

At any instant the IEC bus can be snapshotted to read each line's level (ATN/CLK/DATA/SRQ), which endpoints are pulling each line low, and which devices are talking. Read-only; never perturbs bus state. DONE.

## FR-MED-002 BMP frame-sequence video export (all / unique frames)

Export video as a numbered 24-bit BMP sequence, writing every frame or only frames that differ from the previous one (frames=all|unique capture option).
**Acceptance Criteria:**
- [x] Unique mode skips consecutive byte-identical frames
- [x] Frame files are written off the emulation worker thread

## FR-MED-003 WAV sound recording tapped off the SID output

Record the emulator's SID audio to a 16-bit PCM WAV file via a runtime-swappable tap installed in the SID -> output path.
**Acceptance Criteria:**
- [x] Output parses as valid RIFF/WAVE with data-chunk size matching samples

## FR-MED-004 Muxed video+audio export via external ffmpeg

Export emulator video and audio into a single muxed container (mp4/mkv/avi) by streaming raw BGRA + s16le PCM to an external ffmpeg process over loopback TCP, mirroring VICE ffmpegexedrv.

## FR-PACESEL-001 Selectable emulation pacing strategy

The pacing strategy (Semaphore vs VICE) is selectable in settings, applied live by swapping the gate on the worker thread, and persisted.
**Acceptance Criteria:**
- [x] Strategy is selectable in the Settings UI (Semaphore or VICE)
- [x] Change applies live - the pump swaps the gate with no session restart
- [x] Selection round-trips through the settings DTO and persists
- [x] Unknown or null strategy defaults to Semaphore

## FR-PERF-RUNFRAME-001 C64 PAL RunFrame Throughput

Managed C64 PAL emulation must execute IMachine.RunFrame() fast enough for a host application to sustain 50.125 Hz PAL playback with remaining budget for blit and audio work.
**Acceptance Criteria:**
- [x] Production C64 PAL machine is built through ArchitectureBuilder with real C64 ROMs; romless and minimal-host machines are not valid evidence. (evidence: tests/ViceSharp.Benchmarks/BenchmarkMachineFactory.cs; tests/ViceSharp.Benchmarks/C64PalRunFrameBenchmark.cs; BenchmarksSmokeTests.C64PalRunFrameBenchmark_UsesRealC64Pal passed)
- [x] Release/net10.0 managed-only 60 warmup plus 600 measured frame run reports median <= 18.0 ms. (evidence: RunFramePerfProbe 60 600: median=1.575ms)
- [x] The same measured run reports p95 <= 22.0 ms. (evidence: RunFramePerfProbe 60 600: p95=2.753ms)
- [x] The measured RunFrame loop reports 0 bytes allocated on the current thread. (evidence: RunFramePerfProbe 60 600: allocated=0 bytes; BenchmarkDotNet Allocated reported no managed allocation)
- [x] Public signatures for IMachine, IVideoChip, IAudioChip, IBus, IKeyboardMatrix, ArchitectureBuilder, and C64MachineProfiles remain unchanged. (evidence: PR #3 diff only changes internal implementation plus benchmark/test/docs; no public interface signatures changed)

## FR-PUBSUB-001 Internal Pub/Sub Event Bus

ViceSharp shall provide an internal synchronous topic-based Pub/Sub event bus for transient intra-frame device-to-device communication, including interrupts, NMI, bus availability, address-enable control, DMA, clock, and state notifications. The bus exposes typed publish and subscribe APIs, raw payload compatibility, deterministic registration-order delivery, handle-based unsubscription, frame reset behavior, and message pool integration.
**Acceptance Criteria:**
- [x] Public IPubSub exposes typed Publish/Subscribe, raw payload compatibility, Unsubscribe by SubscriptionHandle, Flush, FrameReset, and SubscriptionCount. (evidence: src/ViceSharp.Abstractions/IPubSub.cs)
- [x] Publish delivers synchronously to subscribers in registration order for each topic. (evidence: tests/ViceSharp.TestHarness/LockFreePubSubTests.cs)
- [x] Message pool exhaustion, return, and frame reset behavior are covered by focused tests. (evidence: tests/ViceSharp.TestHarness/LockFreePubSubTests.cs)

## FR-REMOTECTRL-001 Live Avalonia visual-tree inspection over gRPC

ViceSharp.Avalonia can expose its live Avalonia visual tree for remote inspection and (optionally) interaction over gRPC via the SharpNinja.Avalonia.RemoteControl embeddable server, to support UI development/validation. The server is disabled by default and only starts when explicitly enabled via environment switches, and then only with a bearer token on a loopback transport (interaction and live frames remain deny-by-default opt-ins).

## FR-REVEXEC-001 Reverse execution (backward step)

The emulator can step backward by cycle and by frame, restoring exact prior state, so protocols can be watched forward and backward. Backed by a frame-granular snapshot ring and deterministic re-run.

## FR-SIDAUDIO-001 SID plays at correct pitch

The SID must tick at the phi2 master-clock rate so pitch, envelopes, noise and sync are correct (BUG-SIDAUDIO-001). It was registered as a slow device (ClockDivisor 16) while its accumulator advanced once per Tick, making everything 16x too slow.
**Acceptance Criteria:**
- [x] With voice freq 0x8000, after stepping the SystemClock 8192 master cycles OSC3 reads 0x10 (was 0x01 at the 16x-slow rate)
- [x] Audio sample rate remains 44.1 kHz (self-corrects via ConfigureAudioClock at either divisor)
- [x] ADSR, noise-LFSR and hard-sync run at the phi2 rate

## FR-SIDEBARUI-001 Responsive sidebar layout with collapse expander

The attach sidebar has a collapse expander on its inner edge (facing the video) that flips side with the panel anchor, and its button groups wrap to new rows when the panel is narrow.
**Acceptance Criteria:**
- [x] The collapse expander sits on the inner edge - Right when the panel is anchored Left, Left when anchored Right
- [x] The expander toggles the sidebar pane and its chevron points toward the collapse direction
- [x] Tab and action button groups wrap to new rows (WrapPanel) when the panel is narrow

## FR-SNDREG-001 VICE gate sound back-pressure regulator

When the SID is the audio timing source, the VICE pacing gate paces the worker to the audio device draining its sample buffer (regulator 1), taking precedence over vsync.
**Acceptance Criteria:**
- [x] Buffer at or over the high-water mark => worker blocks (advances nothing)
- [x] Buffer has room => worker advances a chunk
- [x] Warp skips both sound and vsync (highest precedence)
- [x] No active audio device => falls through to the vsync regulator

## FR-SYSINDEP-001 Independent per-system scheduling coupled only by the async IEC bus

Each system (C64, each drive) runs on its own clock and the systems couple only through the asynchronous wired-OR IEC bus, replacing cycle-lockstep.
**Acceptance Criteria:**
- [ ] With a drive attached, the drive CPU advances on its own clock and is not stepped in cycle-lockstep per host instruction.
- [ ] An IEC line transition is observed by every other endpoint's system before that system reads the line.
- [ ] A real IEC transaction such as a directory or program load completes correctly under independent scheduling, at parity with the existing true-drive LOAD test.
- [ ] Each system sustains about 100 percent of its own CPU clock under load including audio on, with no fixed-chunk under-throttle.

## FR-TICKHIST-001 Last-100-ticks time-travel debugger

A History panel lists the last 100 executed CPU instructions; when paused, selecting a tick opens a debug screen with that tick's registers, reconstructed memory, and chip state.
**Acceptance Criteria:**
- [x] History panel lists the last 100 completed instructions, newest first
- [x] Selecting a tick while paused shows that tick's CPU registers
- [x] Memory dump is reconstructed as-of-tick (later ticks' write-deltas reverse-applied to current RAM)
- [x] Inspection is only available while the emulator is paused

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

## FR-UIFLYOUT-001 Flyout sidebar with single side-toggle

The Attach/settings sidebar is a proper flyout (Avalonia SplitView/Flyout). A single icon button toggles the flyout's side (left/right), replacing the separate Left and Right buttons.

## FR-UIMENUBAR-001 VICE-style menu bar

A top menu bar with structure modeled on VICE's x64sc GTK UI: File (smart attach, attach/detach disk 8-11, tape + datasette controls, cartridge, reset soft/hard, exit), Snapshot (load/save, quick, media recording), Settings (full settings, machine/drive/audio/video/input categories, toggle warp, toggle true drive per drive, swap joysticks), Debug (monitor, step), Help (about). Menu commands bind to the existing view-model actions/host services.

## FR-UIPERIPHERAL-001 Reusable per-peripheral UserControl

Each peripheral in the sidebar (Drive 8, Drive 9, Tape, Cartridge) is rendered by a single reusable AXAML UserControl bound to a per-slot view model (status, attach/eject, RO, activity LED, True Drive toggle for drives).

## FR-UISETTINGS-001 Settings panel as a UserControl

The settings panel is a self-contained reusable AXAML UserControl bound to the settings view model (machine profile, video/renderer/palette, audio, input/joystick, limiter, resource mode).

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

