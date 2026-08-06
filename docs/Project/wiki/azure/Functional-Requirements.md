# Functional Requirements (MCP Server)

## ARCH-TRUEDRIVE-1541-002 True-Drive 1541 IEC timing and motor ramp

IecBus.Tick() implements ATN-response state machine (CLK/DATA within 985 cycles of ATN assert). IecDrive.Tick() implements 300,000-cycle motor ramp before rotation. IecDrive.ReadSector(18,0) returns BAM bytes from D64. VICE iecbus.c:247-266, drive/drive.c.
Scope: layer-1+

## BACKFILL-MEDIA-001 Media devices (D64, tape) I/O functional parity

D64DiskImageDevice sector R/W, motor ramp, BAM read all implemented. Datasette motor ramp, sense line, and record mode complete. Closes IEC ATN timing gap for Phase 1.
Scope: layer-1+

## BACKFILL-VIDEO-001 VIC-II visible-frame parity (RC window, DMA checkpoints, screen-RAM)

VIC-II RC/VC state machine matches VICE cycle-accurate behavior (RC window). Native checkpoints validate sprite DMA for all 5 non-PAL models. Screen RAM survives one PAL frame unchanged. VICE viciisc/vicii-cycle.c:541-563, vicii-fetch.c:135-166.
Scope: layer-1+

## FR-CFG-001 FR-CFG-001

Placeholder requirement backfilled for TODO link FR-CFG-001.
Scope: layer-1+

## FR-CFG-005 FR-CFG-005

Placeholder requirement backfilled for TODO link FR-CFG-005.
Scope: layer-1+

## FR-CHIPSTATE-001 Per-tick full chip state capture

Each captured tick snapshots the full internal state of every stateful chip (VIC, SID, CIA, PLA) for display in the debug screen.
Scope: layer-1+
**Acceptance Criteria:**
- [x] VIC, SID, CIA and PLA each implement IStatefulDevice
- [x] Each chip's state is captured per tick with zero hot-path allocation
- [x] Captured state decodes into named register/field values
- [x] The debug screen shows each chip's decoded state at the selected tick

## FR-CPUTICK-001 Per-CPU independent tick counter and per-CPU speed metric

Each CPU in the emulator keeps its own independent executed-cycle counter and the displayed speed is that CPU's executed-cycle rate versus its own target clock.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Each CPU ExecutedCycles increments once per executed cycle and not on a stolen or skipped cycle.
- [ ] Per-CPU speed percent equals delta-executed over delta-wall over targetHz; the C64 primary reads about 95-100 percent at real time and the drive about 100 percent when running.
- [ ] The status surface lists per-CPU rate entries for the host and each peripheral CPU distinctly.
- [ ] Machine reset zeroes every CPU's executed-cycle counter.

## FR-CSDB-001 CSDb discovery, ingest, scan, refresh

PLAN-ROMM-001. Search CSDb scene releases, ingest into RomM, scan, and refresh the library.
AC-CSDB-01: SearchAsync returns classified hits (demo/crack/sid) -> CsdbDiscoveryViewModelTests.Search_ReturnsHits.
AC-CSDB-02: ingest selection capped at 20 -> CsdbDiscoveryViewModelTests.Ingest_CapsAt20.
AC-CSDB-03: LocalCsdbGateway ingests via CsdbRomMWorkflow then triggers scan -> LocalCsdbGatewayTests.Ingest_WritesThenScans.
AC-CSDB-04: BridgeCsdbGateway POSTs /csdb/v1/ingest then calls RomMClient.Tasks.ScanLibraryAsync (bridge does not scan) -> BridgeCsdbGatewayTests.Posts_ThenScans.
AC-CSDB-05: completion raises LibraryRefreshRequested -> CsdbDiscoveryViewModelTests.Ingest_RaisesRefresh.
AC-CSDB-06: gateway selection by config (roms root writable -> Local, else Bridge) -> CsdbGatewaySelectionTests.PicksByConfig.
Scope: layer-1+

## FR-CTX-001 Context machine states + transitions

The context machine has states Gameplay/MainMenu/VirtualKeyboard/ConfirmDialog with defined transitions. Acceptance: a transition-table test drives every (state,input) edge; no undefined transition changes context.
Scope: layer-1+

## FR-CTX-002 Non-Gameplay forces ports neutral

Every non-Gameplay context forces both ports neutral; A does not fire in a menu. Acceptance: Resolve in any non-Gameplay context with A held + sticks deflected -> Joy1=Joy2=Neutral, no fire.
Scope: layer-1+

## FR-CTX-003 Non-Gameplay routes UI navigation

In non-Gameplay, D-pad/left-stick -> UI nav, A=UiActivate, B=UiBack. Acceptance: DPadDown -> UiNavigateDown; A -> UiActivate; B -> UiBack; sticks emit no joystick effect.
Scope: layer-1+

## FR-CTX-004 One-shot neutralizing push on context entry

Entering any non-Gameplay context emits a one-shot neutralizing push to both ports. Acceptance: Gameplay->MainMenu edge -> Joy1=Joy2 neutral push (an emitted neutral, not a suppressed no-op).
Scope: layer-1+

## FR-D71-001 D71 disk image format

D71 (double-sided 1541-format GCR) images: exactly 349,696 bytes, 70 tracks with the D64 zone table applied per side; attachable only to a 1571 drive; implements the shared IGcrDisk surface so the drive mechanism is image-format agnostic.
Scope: layer-1+

## FR-D81-001 D81 disk image format

D81 (3.5in MFM 800K) images: exactly 819,200 bytes, 80 tracks x 2 sides x 10 physical 512-byte MFM sectors (40 logical 256-byte sectors per track); attachable only to a 1581 drive; exposed to the WD1770/MFM floppy model via IMfmDiskImage.
Scope: layer-1+

## FR-DEVCARDART-001 Device-card vector art backgrounds

Every peripheral card in the Avalonia sidebar renders model-appropriate vector device art as a dimmed (opacity 0.20), right-anchored, hit-test-invisible background layer behind the card content. Drives select art by drive model (breadbin for 1540/1541/unknown, 1541-II, 1571, 1581, SFD-1001, CMD-HD, CMD-FD); Tape shows the datasette; Cartridge shows a cartridge. Card content, LED, bindings, and automation ids are unchanged.
Scope: layer-1+

## FR-DRV-002 Commodore 1571 true drive

The 1571: 6502 with runtime-switchable 1/2 MHz clock (VIA1 PA bit 5), two VIAs at $1800/$1C00, WD1770 FDC at $2000-$2FFF (regs mirrored & 3), CIA 6526 at $4000-$7FFF (regs & 0x0F), 32KB DOS ROM at $8000, 2KB RAM mirrored at $0800; double-sided GCR with side select (VIA1 PA bit 2), D64 and D71 attach; per memiec.c:178-199. Acceptance: full cycle-lockstep vs x64sc with Drive8Type=1571 (boot witness + anchored LOAD gates on both D64 and D71).
Scope: layer-1+

## FR-DRV-003 Commodore 1581 true drive

The 1581: 6502 at fixed 2 MHz, 8KB RAM at $0000-$1FFF, CIA at $4000-$5FFF (plain 6526 semantics matching the native oracle), WD1770 at $6000-$7FFF, 32KB DOS ROM at $8000; IEC entirely through the CIA (ATN falling edge triggers the CIA FLAG line); MFM 800K D81 media through the WD1770 type-2 read path; per memiec.c:249-256 and cia1581d.c. Acceptance: full cycle-lockstep vs x64sc with Drive8Type=1581 (boot witness + anchored D81 LOAD gate).
Scope: layer-1+

## FR-DRV-005 IEC Serial Bus Protocol

The emulator shall expose an active-low IEC serial bus with ATN, CLK, DATA, and SRQ line behavior that drives 1541/D64 operations and observable bus activity.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] IEC bus endpoints resolve ATN, CLK, DATA, and SRQ as active-low wired-OR lines.
- [ ] Mounted D64 directory and file operations generate observable IEC line activity from the bus signal source.
- [ ] Mounted D64 directory and file operations complete with correct data through the IEC path.

## FR-DRV1540-001 Commodore 1540 true drive

The 1540 (VIC-1540) drive variant: identical 1541 board topology (6502 + 2KB RAM + two 6522 VIAs + 16KB DOS ROM at $C000) with the dos1540-325302+3-01.bin DOS ROM. Acceptance: full cycle-lockstep vs native VICE x64sc with Drive8Type=1540 (boot witness drive-CPU register lockstep at equal drive clocks over 2M host cycles, plus the KERNAL-CLOSE anchored D64 LOAD gate).
Scope: layer-1+

## FR-DRV1541II-001 Commodore 1541-II true drive

The 1541-II variant: identical 1541 board topology with the dos1541ii-251968-03.bin DOS ROM (VICE type id 1542). Acceptance: full cycle-lockstep vs native VICE x64sc with Drive8Type=1542 (boot witness + anchored D64 LOAD gate).
Scope: layer-1+

## FR-DRVLED-001 Per-drive activity LED

Each drive card shows an activity LED sourced from that drive's VIA2 (1C00) port B bit 3, set by the 1541 DOS ROM (VICE led_status model), independent of IEC bus traffic.
Scope: layer-1+

## FR-DRVMODEL-001 Drive model selection plumbing end-to-end

A DriveModel enum (Abstractions; VICE-canonical values 1540, 1541, 1542=1541-II, 1571, 1581) flows from the protocol (CreateEmulatorSessionRequest.true_drive_model, MediaAttachmentDto.drive_model) through DefaultEmulatorRuntimeFactory and C64TrueDriveRigBuilder to a DriveDescriptorFactory selecting the per-model drive descriptor. Persisted as the VICE-canonical Drive8Type integer in vice.ini via ViceSettings. Every default is 1541; an absent or zero value normalizes to 1541 so behavior is unchanged until a model is selected.
Scope: layer-1+

## FR-DRVMODEL-002 Drive model selection surface (UI, settings, launcher)

The Avalonia drive card exposes a drive-model selector that drives SetTrueDriveAsync with the model and adjusts attach file filters per model; the launcher accepts -drive8type/-drive9type; the multisystem topology YAML accepts an optional model field on drive peripherals; the selection persists to vice.ini Drive8Type and restores at startup.
Scope: layer-1+

## FR-DRVTRUE-001 Per-drive True Drive toggle

Each IEC drive's UI exposes a True Drive toggle. Enabled = cycle-accurate emulated 1541 (6502+VIA+DOS over IEC); disabled (default) = lightweight simulated/buffered drive. Mirrors VICE per-unit DriveTrueEmulation / Fidelity TrueDevice vs Buffered. The runtime honors it (gated true-drive coordinator path), default off so existing behavior is unchanged.
Scope: layer-1+

## FR-GAMEPAD-001 Left stick + D-pad drive JOY2 (OR-merged pre-SOCD)

Left stick and D-pad both drive JOY2, OR-merged before SOCD. Acceptance: LeftX past right threshold + DPadUp -> Joy2.DirectionMask == 0x09, JOY1 unaffected.
Scope: layer-1+

## FR-GAMEPAD-002 Right stick drives JOY1 only

Right stick drives JOY1 with no D-pad contribution. Acceptance: RightY past up threshold -> Joy1 Up(0x01); D-pad does not alter Joy1.
Scope: layer-1+

## FR-GAMEPAD-003 A -> JOY2 fire, B -> JOY1 fire

A -> JOY2 fire, B -> JOY1 fire, emitted as the separate bool. Acceptance: Buttons=A -> Joy2.Fire only; Buttons=B -> Joy1.Fire only; no mask change.
Scope: layer-1+

## FR-GAMEPAD-004 Radial deadzone + per-axis hysteresis 8-way

Radial deadzone + per-axis Activate/Release hysteresis 8-way. Acceptance: mag 0.20 -> mask 0; 0.60 sets, 0.45 holds, 0.35 clears.
Scope: layer-1+

## FR-GAMEPAD-005 Axis sign mapping to direction bits

Axis sign +Y=Up/-Y=Down/+X=Right/-X=Left. Acceptance: the four signed extremes map to the four bits.
Scope: layer-1+

## FR-GAMEPAD-006 SOCD Neutral clears opposing pairs post-merge

SocdMode.Neutral clears opposing pairs post-merge. Acceptance: stick Left + D-pad Right -> both Left and Right cleared.
Scope: layer-1+

## FR-GAMEPAD-007 Default locked mapping, remappable, port swap

Default profile is the locked mapping; remappable at runtime; swap via XboxInputConfig.SwapPorts. Acceptance: defaults LeftStick->Joystick2, RightStick->Joystick1, Dpad->Joystick2, Joy2Fire=A, Joy1Fire=B; a swapped config routes to the other port.
Scope: layer-1+

## FR-GAMEPAD-008 Fail-safe center on disconnect/no-pad/exception

Fail-safe center both ports on disconnect/no-pad/reader-exception; promote next pad. Acceptance: primary disconnect while held -> next Tick emits (Joystick1,0,false)+(Joystick2,0,false), second pad becomes primary.
Scope: layer-1+

## FR-GAMEPAD-009 Single XboxInputContext consumes whole snapshot

The whole snapshot is consumed only by the single XboxInputContext (no separate app-button path). Acceptance: no AppButtonChanged event exists; system buttons reach the context, never a joystick port directly.
Scope: layer-1+

## FR-HOST-006 Host Runtime Status and Control Telemetry

The host runtime shall expose emulator status telemetry for runtime state, timing, media, automation, and IEC bus activity to clients.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Host status responses include existing runtime fields including session, run state, cycle, frame, model, limiter, and automation status.
- [ ] Host status responses include IEC activity derived from emulator bus traffic and safe for UI polling.
- [ ] Status polling does not mutate emulator, drive, or bus state.

## FR-IECHOTPLUG-001 Hot drive add and remove and live device renumber

Drives can be turned on and off and have their device number changed at runtime without restarting the emulator.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A drive attached to a running session answers on the bus with no restart.
- [ ] A detached drive's pull contributions are removed and line states recompute.
- [ ] A drive's device number (8 to 11) can be changed at runtime and it answers the new number.

## FR-IECLOAD-001 True-drive 1541 LOAD over IEC

A single-system C64 with a true-drive 1541 attached completes LOAD"*",8,1, LOAD"$",8 and SAVE over the IEC bus, talking to the drive's DOS ROM via the faithful serial electrical model.
Scope: layer-1+

## FR-IECMON-001 IEC bus monitor (scope view)

Dedicated logic-analyzer panel showing a timing diagram of the IEC lines over emulator time, colored by which device drives each segment, with decoded IEC protocol bands, cursor/zoom/scroll, synced to forward step and reverse step.
Scope: layer-1+

## FR-IECSPY-001 IEC bus snapshot / spy

At any instant the IEC bus can be snapshotted to read each line's level (ATN/CLK/DATA/SRQ), which endpoints are pulling each line low, and which devices are talking. Read-only; never perturbs bus state. DONE.
Scope: layer-1+

## FR-INPROC-001 Kestrel-free in-process host path

The console host runs the core in-process with no Kestrel/ASP.NET on the emulation/render/input path. Acceptance: ConsoleHostComposition.Build works with no WebApplication/Kestrel; ViceSharp.Host.InProcess has no Microsoft.AspNetCore.* reference.
Scope: layer-1+

## FR-INPROC-002 IConsoleEmulatorHost surface

IConsoleEmulatorHost exposes session lifecycle + lock-free frame pull + FrameGeometry + joystick/key/RESTORE injection. Acceptance: a headless test exercises start->advance->frame-pull->input->reset->close; TryGetFrameGeometry.BufferLength == FrameBuffer.Length.
Scope: layer-1+

## FR-INPROC-003 Explicit swap-immune joystick injection

Joystick injection uses explicit Joystick1/Joystick2 (swap-immune). Acceptance: SetJoystick(Joystick2, Up|fire) -> SetJoystickState(2,0x01,true); toggling SwapJoystickPorts does not change the control port.
Scope: layer-1+

## FR-MED-002 BMP frame-sequence video export (all / unique frames)

Export video as a numbered 24-bit BMP sequence, writing every frame or only frames that differ from the previous one (frames=all|unique capture option).
Scope: layer-1+
**Acceptance Criteria:**
- [x] Unique mode skips consecutive byte-identical frames
- [x] Frame files are written off the emulation worker thread

## FR-MED-003 WAV sound recording tapped off the SID output

Record the emulator's SID audio to a 16-bit PCM WAV file via a runtime-swappable tap installed in the SID -> output path.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Output parses as valid RIFF/WAVE with data-chunk size matching samples

## FR-MED-004 Muxed video+audio export via external ffmpeg

Export emulator video and audio into a single muxed container (mp4/mkv/avi) by streaming raw BGRA + s16le PCM to an external ffmpeg process over loopback TCP, mirroring VICE ffmpegexedrv.
Scope: layer-1+

## FR-NATIVERESIDUE-001 Order-independent native oracle across .vsf resumes

Every native (x64sc shim) machine created in a test process must present identical boot and post-activity-reset state regardless of whether a .vsf snapshot was resumed earlier in the same process. Snapshot side effects on process-wide VICE resources (e.g. DriveNTrueEmulation from a has_tde=0 DRIVE8 module) must be re-baselined to VICE defaults at machine create so lockstep suites are order-independent and can run in one process.
Scope: layer-1+

## FR-PACESEL-001 Selectable emulation pacing strategy

The pacing strategy (Semaphore vs VICE) is selectable in settings, applied live by swapping the gate on the worker thread, and persisted.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Strategy is selectable in the Settings UI (Semaphore or VICE)
- [x] Change applies live - the pump swaps the gate with no session restart
- [x] Selection round-trips through the settings DTO and persists
- [x] Unknown or null strategy defaults to Semaphore

## FR-PERF-RUNFRAME-001 C64 PAL RunFrame Throughput

Managed C64 PAL emulation must execute IMachine.RunFrame() fast enough for a host application to sustain 50.125 Hz PAL playback with remaining budget for blit and audio work.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Production C64 PAL machine is built through ArchitectureBuilder with real C64 ROMs; romless and minimal-host machines are not valid evidence. (evidence: tests/ViceSharp.Benchmarks/BenchmarkMachineFactory.cs; tests/ViceSharp.Benchmarks/C64PalRunFrameBenchmark.cs; BenchmarksSmokeTests.C64PalRunFrameBenchmark_UsesRealC64Pal passed)
- [x] Release/net10.0 managed-only 60 warmup plus 600 measured frame run reports median <= 18.0 ms. (evidence: RunFramePerfProbe 60 600: median=1.575ms)
- [x] The same measured run reports p95 <= 22.0 ms. (evidence: RunFramePerfProbe 60 600: p95=2.753ms)
- [x] The measured RunFrame loop reports 0 bytes allocated on the current thread. (evidence: RunFramePerfProbe 60 600: allocated=0 bytes; BenchmarkDotNet Allocated reported no managed allocation)
- [x] Public signatures for IMachine, IVideoChip, IAudioChip, IBus, IKeyboardMatrix, ArchitectureBuilder, and C64MachineProfiles remain unchanged. (evidence: PR #3 diff only changes internal implementation plus benchmark/test/docs; no public interface signatures changed)

## FR-PUBSUB-001 Internal Pub/Sub Event Bus

ViceSharp shall provide an internal synchronous topic-based Pub/Sub event bus for transient intra-frame device-to-device communication, including interrupts, NMI, bus availability, address-enable control, DMA, clock, and state notifications. The bus exposes typed publish and subscribe APIs, raw payload compatibility, deterministic registration-order delivery, handle-based unsubscription, frame reset behavior, and message pool integration.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Public IPubSub exposes typed Publish/Subscribe, raw payload compatibility, Unsubscribe by SubscriptionHandle, Flush, FrameReset, and SubscriptionCount. (evidence: src/ViceSharp.Abstractions/IPubSub.cs)
- [x] Publish delivers synchronously to subscribers in registration order for each topic. (evidence: tests/ViceSharp.TestHarness/LockFreePubSubTests.cs)
- [x] Message pool exhaustion, return, and frame reset behavior are covered by focused tests. (evidence: tests/ViceSharp.TestHarness/LockFreePubSubTests.cs)

## FR-REMOTECTRL-001 Live Avalonia visual-tree inspection over gRPC

ViceSharp.Avalonia can expose its live Avalonia visual tree for remote inspection and (optionally) interaction over gRPC via the SharpNinja.Avalonia.RemoteControl embeddable server, to support UI development/validation. The server is disabled by default and only starts when explicitly enabled via environment switches, and then only with a bearer token on a loopback transport (interaction and live frames remain deny-by-default opt-ins).
Scope: layer-1+

## FR-REVEXEC-001 Reverse execution (backward step)

The emulator can step backward by cycle and by frame, restoring exact prior state, so protocols can be watched forward and backward. Backed by a frame-granular snapshot ring and deterministic re-run.
Scope: layer-1+

## FR-ROMM-AVUI-001 Avalonia Steam desktop UI

PLAN-ROMM-001. Steam-style desktop library, details, and lists views on the Avalonia head.
AC-AUI-01 [V]: LibraryView builds and binds LibraryBrowseViewModel (build + headless VM tests AC-BROWSE-*).
AC-AUI-02 [V]: connection dialog + browse/search/list/launch end to end (E2E).
AC-AUI-03 [V]: GameDetailsView builds and binds RomDetailViewModel (cover, metadata, files table, Attach+play/Attach only/Add-to-list, membership) (build + RomDetailViewModelTests).
AC-AUI-04 [V]: ListsView builds and binds CollectionsViewModel (rail, toolbar Rename/Delete/Add games, per-row remove) (build + CollectionsViewModelTests).
Scope: layer-1+

## FR-ROMM-BROWSE-001 RomM browse, search, filter, page, jump

PLAN-ROMM-001. Browse thousands of titles scoped to the active machine. TR-ROMM-THREAD-001.
AC-BROWSE-01: BrowseAsync maps CustomLimitOffsetPage -> LibraryPage (items,total,offset,charIndex) -> RomMGatewayBrowseTests.Page_Maps.
AC-BROWSE-02: active machine slug (c64/c128/c-plus-4/vic-20/cpet, from ICurrentMachineProvider) resolves to numeric id and always scopes the query; no user platform picker -> RomMGatewayBrowseTests.PlatformSlug_ResolvesAndFilters.
AC-BROWSE-03: searchTerm passes through to search_term -> RomMGatewayBrowseTests.SearchTerm_Passed.
AC-BROWSE-04: LoadMoreAsync appends next page; HasMore false at offset>=total -> LibraryBrowseViewModelTests.Paging_AppendsToEnd.
AC-BROWSE-05: A-Z jump maps letter -> offset via charIndex; JumpToLetterAsync loads that page -> LibraryBrowseViewModelTests.Jump_UsesCharIndex.
AC-BROWSE-06: rapid search input debounces (300 ms) to a single query -> LibraryBrowseViewModelTests.Search_Debounced.
AC-BROWSE-07: changing the active machine (Settings) re-scopes browse to the new slug and reloads; no in-library source/platform selector -> LibraryBrowseViewModelTests.MachineChange_RescopesAndReloads.
AC-BROWSE-08: background load raises PropertyChanged on the captured context (no cross-thread) -> LibraryBrowseViewModelTests.OffContext_Dispatches.
Scope: layer-1+

## FR-ROMM-COLLECT-001 Lists via RomM server-side collections

PLAN-ROMM-001. Game lists are RomM collections (/api/collections), synced across both heads. TR-ROMM-JSON-001.
AC-COLLECT-01: GetCollectionsAsync lists user collections; smart/virtual flagged read-only -> RomMGatewayCollectionsTests.List_FlagsReadOnly.
AC-COLLECT-02: CreateCollectionAsync POSTs and returns the collection -> RomMGatewayCollectionsTests.Create_Posts.
AC-COLLECT-03: add/remove send CollectionRomsPayload{rom_ids[]} on the right verb -> RomMGatewayCollectionsTests.AddRemove_SendPayload.
AC-COLLECT-04: rename (PUT) and delete (DELETE) -> RomMGatewayCollectionsTests.RenameDelete.
AC-COLLECT-05: CollectionsViewModel add/remove selected roms then refresh -> CollectionsViewModelTests.AddRemove_Refreshes.
AC-COLLECT-06: every collections DTO deserializes reflection-free via RomMJsonContext -> RomMJsonContextTests.Collections_SourceGenOnly.
Scope: layer-1+

## FR-ROMM-CONN-001 RomM connection and authentication

PLAN-ROMM-001. Connect both heads to a RomM 5.x server and authenticate. TR-ROMM-NUGET-001, TR-ROMM-JSON-001.
AC-CONN-01: base URL + client token authenticates; Authorization: Bearer rmm_... on requests -> RomMGatewayAuthTests.ClientToken_SetsBearer.
AC-CONN-02: device-pair exchange returns a token and persists it via the store -> RomMPairingTests.Exchange_ReturnsAndPersists.
AC-CONN-03: OAuth password token auto-refreshes on near-expiry through RomMAuthHandler -> RomMAuthRefreshTests.NearExpiry_Refreshes.
AC-CONN-04: a 401 sets the connection VM to a sign-in-expired reauth state and raises ConnectionInvalid -> LibraryConnectionViewModelTests.Unauthorized_SurfacesReauth.
AC-CONN-05: FileRomMConnectionStore round-trips baseUrl/authMode/token via source-gen JSON -> FileRomMConnectionStoreTests.SaveLoad_RoundTrips.
AC-CONN-06: token never appears in a URL/query string -> RomMGatewayAuthTests.Token_NeverInUri.
Scope: layer-1+

## FR-ROMM-COVER-001 Cover art and cache

PLAN-ROMM-001. Cover art loads efficiently for large virtualized grids.
AC-COVER-01: url_cover fetched without auth; path_cover_* fetched with bearer under the confirmed prefix -> RomMCoverImageSourceTests.AuthRules.
AC-COVER-02: two-tier cache; second request for same cover hits cache (no second HTTP) -> CoverCacheTests.SecondRequest_Cached.
AC-COVER-03: concurrency gate (<=4) + off-screen cancellation -> CoverCacheTests.Concurrency_Gated_Cancellable.
AC-COVER-04: a fetch failure yields the placeholder and never throws -> CoverCacheTests.Failure_Placeholder.
Scope: layer-1+

## FR-ROMM-DETAIL-001 RomM ROM detail

PLAN-ROMM-001. Show a game's detail (cover, metadata, files, launchable, collection membership).
AC-DETAIL-01: GetRomAsync maps DetailedRomSchema (files/cover/summary/launchable) -> RomMGatewayDetailTests.Detail_Maps.
AC-DETAIL-02: RomDetailViewModel invokes add-to-collection -> RomDetailViewModelTests.AddToCollection_Invokes.
Scope: layer-1+

## FR-ROMM-LAUNCH-001 Download, attach, boot

PLAN-ROMM-001. Selecting a game downloads it, attaches to the right slot, and boots.
AC-LAUNCH-01: DownloadAsync streams to cacheDir/{romId}/{fs_name}, reuses on size match, reports progress -> RomMGatewayDownloadTests.Streams_Reuses_Progress.
AC-LAUNCH-02: slot = SelectedSlot or MediaExtensionMap default from fs_name -> MediaExtensionMapTests.Extension_MapsSlot.
AC-LAUNCH-03: .prg is not launchable and Attach is disabled -> MediaExtensionMapTests.Prg_NotLaunchable + LibraryBrowseViewModelTests.Prg_AttachDisabled.
AC-LAUNCH-04: attach/attach+start invoke IGameLauncher with resolved slot + autostart flag -> LibraryBrowseViewModelTests.AttachStart_InvokesLauncher.
AC-LAUNCH-05: Xbox launcher reads bytes -> AttachMediaAsync payload -> AutostartDrive8Async(disk)/ColdResetAsync(cart) -> XboxGameLauncherTests.PayloadAttachAndBoot.
AC-LAUNCH-06: Avalonia launcher delegates to DropAndStartFileAsync(autostart)/AttachAsync(attach-only) -> AvaloniaGameLauncherTests.DelegatesToShell.
AC-LAUNCH-07: two-phase status (Downloading N% then Starting) -> LibraryBrowseViewModelTests.Launch_TwoPhaseStatus.
Scope: layer-1+

## FR-ROMM-PKG-001 NuGet packaging of RomM libraries

PLAN-ROMM-001. Ship ViceSharp.Library.ViewModels + ViceSharp.RomM as nuget.org packages. TR-ROMM-BOUNDARY-001, TR-ROMM-NUGET-001.
AC-PKG-01: both libraries IsPackable with id + GPL-2.0-or-later metadata -> PackageMetadataTests.Ids_License.
AC-PKG-02: PublishNuget packs both nupkgs with correct deps (RomM.Client, RomM.Client.Csdb, ViceSharp.*) -> PackageMetadataTests.Dependencies + build.ps1 PublishNuget --skip [V].
AC-PKG-03: ViceSharp.Library.ViewModels references only Protocol; no RomM./Http/Grpc./Avalonia./Windows./engine names -> LibraryViewModelsBoundaryTests.
AC-PKG-04: restore resolves RomM.Client/Csdb from nuget.org with the nuget.org-only config -> NuGetConfigTests.NugetOrgOnly + restore [V].
Scope: layer-1+

## FR-ROMM-XBOXUI-001 Xbox Game Pass UI and navigation

PLAN-ROMM-001. 10-foot gamepad-first library, details, and lists pages on the UWP head.
AC-XUI-01: Library in NavigationDestination, a HomePage button, and Push route -> XboxNavigationTests.Library_Routable.
AC-XUI-02 [V]: LibraryPage builds Debug-UWP, binds LibraryBrowseViewModel (grid tiles, search, read-only machine indicator, slot picker, A/Y actions, A-Z strip; no source/platform picker).
AC-XUI-03 [V]: A=Attach, Y=Attach+Autostart via the input command path.
AC-XUI-04 [V]: no RPC_E_WRONG_THREAD on async tile/cover load (guarded by AC-BROWSE-08).
AC-XUI-05 [V]: GameDetailsPage builds Debug-UWP, binds RomDetailViewModel (cover, screenshots, metadata, files, slot picker, A/Y/X); VM logic in RomDetailViewModelTests.
AC-XUI-06 [V]: ListsPage builds Debug-UWP, binds CollectionsViewModel (rail, selected-list games, X=remove/Y=rename/Menu=delete/+New list); VM logic in CollectionsViewModelTests.
Scope: layer-1+

## FR-SID-013 SID audio backend wiring

The emulator shall wire SID sample production through the host audio backend without dropping or duplicating real-time audio buffers.
Scope: layer-1+

## FR-SID-014 VICE-compatible signed SID voice output and demo pacing

SID voice output must be centered and scaled like VICE/reSID so live host audio back-pressure paces demos at the same rate as VICE across runtime segment transitions.
Scope: layer-1+

## FR-SIDAUDIO-001 SID plays at correct pitch

The SID must tick at the phi2 master-clock rate so pitch, envelopes, noise and sync are correct (BUG-SIDAUDIO-001). It was registered as a slow device (ClockDivisor 16) while its accumulator advanced once per Tick, making everything 16x too slow.
Scope: layer-1+
**Acceptance Criteria:**
- [x] With voice freq 0x8000, after stepping the SystemClock 8192 master cycles OSC3 reads 0x10 (was 0x01 at the 16x-slow rate)
- [x] Audio sample rate remains 44.1 kHz (self-corrects via ConfigureAudioClock at either divisor)
- [x] ADSR, noise-LFSR and hard-sync run at the phi2 rate

## FR-SIDEBARUI-001 Responsive sidebar layout with collapse expander

The attach sidebar has a collapse expander on its inner edge (facing the video) that flips side with the panel anchor, and its button groups wrap to new rows when the panel is narrow.
Scope: layer-1+
**Acceptance Criteria:**
- [x] The collapse expander sits on the inner edge - Right when the panel is anchored Left, Left when anchored Right
- [x] The expander toggles the sidebar pane and its chevron points toward the collapse direction
- [x] Tab and action button groups wrap to new rows (WrapPanel) when the panel is narrow

## FR-SNDREG-001 VICE gate sound back-pressure regulator

When the SID is the audio timing source, the VICE pacing gate paces the worker to the audio device draining its sample buffer (regulator 1), taking precedence over vsync.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Buffer at or over the high-water mark => worker blocks (advances nothing)
- [x] Buffer has room => worker advances a chunk
- [x] Warp skips both sound and vsync (highest precedence)
- [x] No active audio device => falls through to the vsync regulator

## FR-SYSBTN-001 Ship default gameplay binding set

Ship the default gameplay binding set. Acceptance: BindingProfile.Default enumerates the locked table by value.
Scope: layer-1+

## FR-SYSBTN-002 Menu toggles the main menu (fixed)

Menu toggles the main menu; not remappable away from menu control. Acceptance: Menu edge in Gameplay -> MainMenu+OpenMainMenu; in MainMenu -> Gameplay+CloseMenu.
Scope: layer-1+

## FR-SYSBTN-003 View toggles the virtual keyboard overlay

View toggles the virtual keyboard overlay. Acceptance: View edge toggles Gameplay<->VirtualKeyboard.
Scope: layer-1+

## FR-SYSBTN-004 Each non-nav AppCommand maps to a host command

Each non-nav AppCommand maps to an existing host command. Acceptance: AppCommandDispatcher invokes the correct recorded host call once with correct args on fakes.
Scope: layer-1+

## FR-SYSBTN-005 LT hold-to-warp hysteresis

LT hold-to-warp hysteresis (on>=0.6/off<=0.4). Acceptance: rising 0.0->0.7 one WarpHoldOn; falling 0.7->0.3 one WarpHoldOff; 0.4..0.6 none.
Scope: layer-1+

## FR-SYSBTN-006 SwapJoystickPorts flips config, no host change

SwapJoystickPorts flips XboxInputConfig.SwapPorts; no host InputSettings change. Acceptance: dispatch flips the flag, issues no ISettingsService call; second dispatch flips back.
Scope: layer-1+

## FR-SYSBTN-007 Exit only from main menu behind ConfirmDialog

Exit only from the main menu behind ConfirmDialog. Acceptance: no RequestExit binding in Default; RequestExit only from MainMenu->ConfirmDialog, only ConfirmYes exits.
Scope: layer-1+

## FR-SYSBTN-008 Bindings remappable + persisted + reset

Bindings remappable and persisted with reset-to-defaults. Acceptance: Save then Load round-trips an edited profile byte-for-byte; ResetToDefaults yields Default.
Scope: layer-1+

## FR-SYSINDEP-001 Independent per-system scheduling coupled only by the async IEC bus

Each system (C64, each drive) runs on its own clock and the systems couple only through the asynchronous wired-OR IEC bus, replacing cycle-lockstep.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] With a drive attached, the drive CPU advances on its own clock and is not stepped in cycle-lockstep per host instruction.
- [ ] An IEC line transition is observed by every other endpoint's system before that system reads the line.
- [ ] A real IEC transaction such as a directory or program load completes correctly under independent scheduling, at parity with the existing true-drive LOAD test.
- [ ] Each system sustains about 100 percent of its own CPU clock under load including audio on, with no fixed-chunk under-throttle.

## FR-TESTGATE-001 S0 feasibility gate before production

The head is delivered as an ordered BDP v4 slice sequence where S0 is a feasibility go/no-go that must pass before any production slice. Acceptance: S0 GO recorded before S1; the ordered slice list is the single source of execution order.
Scope: layer-1+

## FR-TESTGATE-002 0 failed AND 0 skipped per slice scope

Every gate enforces 0 failed AND 0 skipped within that slice's filter scope. Acceptance: each slice trx within its --filter reports Failed=0 and Skipped=0.
Scope: layer-1+

## FR-TESTGATE-003 Determinism + desktop never regress

Determinism and the desktop suite never regress across any gate. Acceptance: Category=Determinism 0 failed with pass count == pre-S1 baseline at every gate; Avalonia boundary + desktop build stay green.
Scope: layer-1+

## FR-TESTGATE-004 Majority off-console; device slices isolated

The majority of gates run off-console; on-console work is isolated to marked device slices. Acceptance: production off-console slices need no hardware; only S0 + device slices are onConsole=true.
Scope: layer-1+

## FR-TICKHIST-001 Last-100-ticks time-travel debugger

A History panel lists the last 100 executed CPU instructions; when paused, selecting a tick opens a debug screen with that tick's registers, reconstructed memory, and chip state.
Scope: layer-1+
**Acceptance Criteria:**
- [x] History panel lists the last 100 completed instructions, newest first
- [x] Selecting a tick while paused shows that tick's CPU registers
- [x] Memory dump is reconstructed as-of-tick (later ticks' write-deltas reverse-applied to current RAM)
- [x] Inspection is only available while the emulator is paused

## FR-UI-002 Emulator Status and Machine Control Bar

The UI shall provide a status and control bar for runtime state, controls, performance fields, and IEC activity.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The status bar presents existing run state, cycle, frame, model, limiter, automation, and control commands.
- [ ] The status bar presents IEC activity from host telemetry without replacing existing fields.
- [ ] The status bar remains usable without stealing emulator focus for normal display interaction.

## FR-UI-003 Collapsible Tabbed Emulator Sidebar

The UI shall provide a dockable tabbed sidebar with peripherals and settings surfaces driven by host protocol state.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The peripherals tab exposes drive attachment state and media commands for configured drives.
- [ ] Drive entries expose IEC active/idle state from the same host telemetry source as the status bar.
- [ ] Drive IEC activity returns idle in the peripherals tab after bus activity settles.

## FR-UIFLYOUT-001 Flyout sidebar with single side-toggle

The Attach/settings sidebar is a proper flyout (Avalonia SplitView/Flyout). A single icon button toggles the flyout's side (left/right), replacing the separate Left and Right buttons.
Scope: layer-1+

## FR-UIMENUBAR-001 VICE-style menu bar

A top menu bar with structure modeled on VICE's x64sc GTK UI: File (smart attach, attach/detach disk 8-11, tape + datasette controls, cartridge, reset soft/hard, exit), Snapshot (load/save, quick, media recording), Settings (full settings, machine/drive/audio/video/input categories, toggle warp, toggle true drive per drive, swap joysticks), Debug (monitor, step), Help (about). Menu commands bind to the existing view-model actions/host services.
Scope: layer-1+

## FR-UIPERIPHERAL-001 Reusable per-peripheral UserControl

Each peripheral in the sidebar (Drive 8, Drive 9, Tape, Cartridge) is rendered by a single reusable AXAML UserControl bound to a per-slot view model (status, attach/eject, RO, activity LED, True Drive toggle for drives).
Scope: layer-1+

## FR-UISETTINGS-001 Settings panel as a UserControl

The settings panel is a self-contained reusable AXAML UserControl bound to the settings view model (machine profile, video/renderer/palette, audio, input/joystick, limiter, resource mode).
Scope: layer-1+

## FR-VIC-001 VIC-II PAL raster cycle counter and frame-periodic behavior

Managed PAL VIC-II advances rasterLine/rasterX cyclically by exactly 312*63=19,656 ticks per frame. CycleCounter increments monotonically. VICE vicii-cycle.c:576-598.
Scope: layer-1+

## FR-VIC-002 FR-VIC-002

Placeholder requirement backfilled for TODO link FR-VIC-002.
Scope: layer-1+

## FR-VIC-003 FR-VIC-003

Placeholder requirement backfilled for TODO link FR-VIC-003.
Scope: layer-1+

## FR-VIC-004 FR-VIC-004

Placeholder requirement backfilled for TODO link FR-VIC-004.
Scope: layer-1+

## FR-VIC-005 FR-VIC-005

Placeholder requirement backfilled for TODO link FR-VIC-005.
Scope: layer-1+

## FR-VIC-006 FR-VIC-006

Placeholder requirement backfilled for TODO link FR-VIC-006.
Scope: layer-1+

## FR-VIC-007 FR-VIC-007

Placeholder requirement backfilled for TODO link FR-VIC-007.
Scope: layer-1+

## FR-VIC-008 VIC-II FLI forced bad line RC window interrupt

Changing YSCROLL mid-frame to match current raster line low 3 bits forces a bad line. VC update at cycle 13 resets rc=0 and clears idle_state, interrupting the idle window. VICE viciisc/vicii-cycle.c:51-60.
Scope: layer-1+

## FR-VIC-010 FR-VIC-010

Placeholder requirement backfilled for TODO link FR-VIC-010.
Scope: layer-1+

## FR-VSFLOCKSTEP-001 Resume externally-staged VICE .vsf snapshots in the native oracle

The native VICE oracle (vice_x64 shim) reads and resumes a .vsf snapshot staged by a standalone x64sc, including snapshots from an older VICE release whose module versions differ from the bundled submodule, so lockstep can start from a user-supplied known state (C64SC identity, reSID engine, true-drive set).
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A supplied x64sc PAL/C64C .vsf (tests/ViceSharp.TestHarness/Fixtures/Vsf/ready-c64sc-truedrive.vsf) loads with rc=0 and snapshot_last_error=0; all 16 C64 modules (MAINCPU..USERPORT) are consumed.
- [ ] The resumed MAINCPU registers (A/X/Y/SP/PC) equal those encoded in the snapshot's MAINCPU module.
- [ ] No regression to the reSID lockstep timing gate or SID parity (X64ScVariantLockstep 306 pass / 0 fail / 1 skip; SID parity 8/8).

## FR-XAUDIO-001 SID audio through console endpoint

SID audio plays through the console default endpoint when foreground and not muted. Acceptance (on-console): a known SID tune / $D418 tone is audible; matches desktop WinMm within resampling tolerance.
Scope: layer-1+

## FR-XAUDIO-002 Master volume/mute, gain applied once

Master volume and mute are honored; EffectiveGain applied exactly once. Acceptance: Volume scales output; Muted silences without stopping the ring; gain applied in AudioSampleConverter before samples reach the ring.
Scope: layer-1+

## FR-XAUDIO-003 Suspend/resume audio without deadlock

Audio pauses on suspend/background and resumes without breaking determinism or deadlocking the worker. Acceptance: Suspending stops the source voice; the worker is not parked in SubmitSamples (non-blocking ring); Resuming continues; core state unchanged.
Scope: layer-1+

## FR-XAV-001 Headless stays silent + deterministic

Headless/test stays silent and deterministic. Acceptance: XboxAudioBackendFactory.CreateDefault() returns null headless; SID built null-backend has IsAudioTimingSource==false and emits no samples; parity/pacing suite unchanged.
Scope: layer-1+

## FR-XBOXGPL-006 MSIX GPL compliance payload

The MSIX bundles COPYING + THIRD_PARTY_NOTICES.md (incl. vkm GPL attribution) + the *.vkm Content, bundles zero ROM *.bin, and exposes the source URL; the release attaches MSIX + source archive. Acceptance: staging test asserts Licenses/ contents + vkm Assets present + no kernal/basic/chargen *.bin + compiled SourceUrl.
Scope: layer-1+

## FR-XBOXPKG-001 Head csproj references only core + Host.InProcess

The head csproj references only the managed core (+ Host.InProcess) and none of Host/Grpc.AspNetCore/AspNetCore.App/Avalonia. Acceptance: csproj + slnx text test asserts presence of core refs + absence of forbidden refs; slnx builds.
Scope: layer-1+

## FR-XBOXPKG-003 Manifest declares Windows.Xbox + capability rule

Package.appxmanifest declares Windows.Xbox (Min 10.0.19041.0, MaxTested 10.0.26100.0), x64 identity, and declares internetClient iff the HTTPS ROM download path is kept (else no restricted capability). Acceptance: XML-parse asserts the TargetDeviceFamily, x64, and the internetClient-present-iff-download-kept rule.
Scope: layer-1+

## FR-XBOXTOPO-001 New ViceSharp.Xbox UWP-on-console head project

A new project src/ViceSharp.Xbox exists and is registered in ViceSharp.slnx as the true UWP-on-Xbox-console head (net10.0-windows10.0.26100.0 + UseUwp; Native AOT for Release/Store, JIT for Debug/Dev-Mode sideload), distinct from and additive to the existing src/ViceSharp.Host.Xbox Avalonia scaffold, which stays byte-for-byte intact.
Scope: layer-1+

## FR-XBOXUI-001 Six destinations + overlays, non-destructive back stack

Six navigable destinations + quick-menu/virtual-keyboard overlays reachable from any state; back stack never recreates/pauses the running video surface. Acceptance: push Settings->DeviceSetup, GoBack twice returns to Home in order; opening overlays pushes no stack entry.
Scope: layer-1+

## FR-XBOXUI-002 Controller-only high-visibility focus

Every control operable by D-pad/left-stick + A/B, no pointer, high-visibility focus in the TV-safe rect. Acceptance: BuildGrid(3,2) edges correct, top Up null, no wrap; on-console focus-visual smoke.
Scope: layer-1+

## FR-XBOXUI-003 One authoritative input context

UI-nav and gameplay input never simultaneous; exactly one context authoritative. Acceptance: quick menu -> non-Gameplay; closing with empty stack -> Gameplay.
Scope: layer-1+

## FR-XBOXUI-004 Session keeps running under overlays

Session keeps running while overlays shown (video advancing unless explicitly paused). Acceptance: simulated navigation/overlay toggles never stop/dispose the pull adapter.
Scope: layer-1+

## FR-XBOXUI-005 Video renders committed frames ~50Hz as pure sink

Video renders committed frames ~50Hz as a pure sink. Acceptance: 10 fires/200ms -> 10 pulls +/-1; pure-sink forbidden-identifier scan passes.
Scope: layer-1+

## FR-XBOXUI-006 On-screen C64 keyboard exact key strings

On-screen controller-navigable C64 keyboard maps every key to the exact SetKeyState string incl RETURN/RUNSTOP/F1-F8. Acceptance: RETURN tile emits SetKeyState("Return",true) then false; shift-latch+F1 emits "F2"; every KeyName is in the known-good list.
Scope: layer-1+

## FR-XBOXUI-007 ViewModels depend only on Abstractions+Protocol

ViceSharp.Xbox.ViewModels depends only on Abstractions + Protocol (portable, Kestrel-free). Acceptance: csproj-reference assertion + forbidden-identifier scan (no Core/Chips/Architectures/Host/Avalonia/Grpc/XAML).
Scope: layer-1+

## FR-XBOXUI-008 About page GPL disclosure

About page discloses GPL-2.0-or-later, VICE attribution, source-availability offer. Acceptance: LicenseIdentifier=="GPL-2.0-or-later", AttributionText contains "VICE", SourceOfferText non-empty + URL.
Scope: layer-1+

## FR-XDEV-001 Device Setup attach/eject via host media

Device Setup presents Drive8/Drive9/Tape/Cartridge cards with typed attach + eject through the host media boundary. Acceptance: patterns per slot; Attach reads bytes via IStoragePicker and calls AttachMediaAsync; Eject calls DetachMediaAsync; no runtime-internal calls.
Scope: layer-1+

## FR-XDEV-002 Drive-model selector rebuilds true-drive rig

Drive cards expose the implemented drive-model selector and rebuild the true-drive rig on model change. Acceptance: AvailableDriveModels==DriveModelCatalog.Implemented; active true-drive model change calls SetTrueDriveAsync(true, device, diskPath, (int)model) with 1542 for 1541-II; inactive-slot change is inert.
Scope: layer-1+

## FR-XDEV-003 Single true-drive rig at a time

Only one true-drive rig at a time. Acceptance: enabling Drive9 true-drive sets Drive8.TrueDrive=false with one rebuild; disabling all issues SetTrueDriveAsync(false).
Scope: layer-1+

## FR-XKBD-001 RESTORE/NMI seam distinct from SetKeyState

A dedicated RESTORE/NMI trigger exists on the Abstractions keyboard seam and the in-process facade; the virtual-keyboard RESTORE tile drives it, not SetKeyState. Acceptance: IMachineKeyboardInput.SetRestoreState(bool) routes to the C64 NMI path (keycode 0x31) distinct from SetKeyState; VirtualKeyboardViewModel RESTORE tile calls the facade's SetRestoreState, and a spy keyboard input records the NMI trigger, not a SetKeyState("*").
Scope: layer-1+

## FR-XROM-001 First-run ROM provisioning gate

First run evaluates ROM provisioning and blocks normal boot until Complete (offering the wizard). Acceptance: Evaluate returns Complete only when every required role is present+hash-valid; NotProvisioned/Partial otherwise; IsBootBlocked unless Complete (or kernal not required).
Scope: layer-1+

## FR-XROM-002 Wizard acquires core ROMs (HTTPS/USB)

The wizard acquires the core ROM set by verified HTTPS download (requires internetClient) or USB/storage import into LocalFolder\vice\C64. Acceptance: Download fetches basic/kernal/characters SHA256-checked after confirm; Import validates size+MD5 before copy; mismatch rejected leaving state unchanged.
Scope: layer-1+

## FR-XROM-003 Provision shipped vkm keymaps

Shipped GPL *.vkm keymaps are provisioned into the writable C64 folder so the picker lists them. Acceptance: after XboxDataPathBridge.Configure the packaged vkm exist under C64Path and ListKeyboardMapsAsync returns them; else the embedded gtk3_pos fallback.
Scope: layer-1+

## FR-XSET-001 Four Settings pages bind desktop values via host

Four 10-foot Settings pages bind to the same values/option ids as desktop and apply through the host UpdateSettings pipeline. Acceptance: Refresh loads GetSettings/ListSettingsProfiles; any resource change sets HasPendingSettingsChanges; Apply sends DTO ids equal to SettingsOptionCatalog.To*(...); host-canonical returned settings adopted.
Scope: layer-1+

## FR-XSET-002 Rebuild-required changes flagged and gated

Rebuild-required changes are flagged and gated as on desktop. Acceptance: profile/resource-mode -> RequiresRestart; Apply(restart:true) sends RestartSession=true; others keep false.
Scope: layer-1+

## FR-XSET-003 Validation without applying

Validation runs without applying and surfaces per-resource results. Acceptance: Validate sends ValidateSettingsResourcesRequest, populates results, sends no UpdateSettings; editing clears stale results.
Scope: layer-1+

## FR-XSET-004 RevertSettings restores last-applied

RevertSettings restores the last-applied local state. Acceptance: after dirtying, Revert restores each field, clears dirty + validation.
Scope: layer-1+

## FR-XSET-005 Xbox-only prefs to vice-sharp.ini

Master volume/mute/TV-safe-inset/per-stick deadzone are Xbox-only prefs persisted to vice-sharp.ini, not the emulator DTOs. Acceptance: setting them then Save writes [ViceSharpXbox]; fresh Load returns them; no UpdateSettingsRequest carries them.
Scope: layer-1+

## FR-XVIDEO-001 Live C64 frames, BGRA, letterbox, no tear

Display live C64 frames with correct BGRA colors, 4:3 letterbox, no tearing (Win2D nearest-neighbor integer scale into the TV-safe rect). Acceptance (on-console): READY. screen + animation; colors match desktop; centered/letterboxed, no tear.
Scope: layer-1+

## FR-XVIDEO-002 Render is a pure read-only sink

Frame rendering is a pure read-only sink and does not perturb determinism. Acceptance: the pull adapter invokes only TryCopyFrameInto; render-enabled vs disabled replay is bit-identical; steady-state allocation delta == 0.
Scope: layer-1+

## RUNTIME-TAPE-002 Datasette motor ramp + sense line + record mode

Datasette.Tick() enforces MOTOR_DELAY=32,000-cycle ramp (datasette.c:62) before pulse delivery when Tick is timing mechanism. SenseLine=!PlayPressed||!RecordPressed (CIA1  bit 4). TryWritePulse stores pulses in record mode.
Scope: layer-1+

