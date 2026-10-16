# Native tape state audit for the corrected draft

Source-derived observations, not runtime proof. Native target: `cc67418cba58e71d8e70fd353e9de47199ad2075`. Paths below are relative to its `vice/` directory. Inspection used the separate materialization under `F:\GitHub\vice-sharp\artifacts\immutable-vice\cc67418cba58e71d8e70fd353e9de47199ad2075\source\vice`; no native source was edited.

Exact proposed AC text is in [proposed-field-delta.json](proposed-field-delta.json). The case names below describe required future fixtures, not completed tests. All need independently derived expected values, correct-mock success, expected rejection of incorrect mocks, and the same production-bound assertions after approval.

## N-07: process history is an input

`datasette.c:436-502` contains function-static wobble phase, floating rounding remainder and azimuth remainder. These are not per-image fields. Wobble bypass requires zero frequency or amplitude together with zero speed tuning; azimuth has its own zero-resource bypass. Native arithmetic rounds/clamps and carries a remainder. Attachment scans gaps across the image at `879-910`, so the scan itself participates in that state history.

The test contract must distinguish a fresh process, reset within a process, detach/reattach and snapshot reload. It must not invent a common clean state for them. Baseline timing fixtures explicitly neutralize wobble, tuning and azimuth. Separate non-neutral fixtures remain mandatory; neutralization is not permission to omit their behavior.

Required case groups:

- `Fixture_ProcessHistoryAndAttachScan`: same image in fresh processes; repeated attach; different preceding image; prior port/device history where supported. Pin the full prelude, attachment scan and resources. Reject equal-looking snapshots with different unrepresented process history.
- `Fixture_WobbleAzimuthRounding`: independent frequency/amplitude/tuning/azimuth boundaries, resource changes, phase/remainder carry and clamp behavior. Pin native binary/platform arithmetic and RNG history. Reject a stateless per-pulse implementation or reset that silently clears retained remainder.
- `Fixture_MachinePortRetainedState`: initial `last_write_bit = -1`, C64 previous-port statics initialized to `0xff`, reset-retained output history, SX-64 capacitor state, and VIA previous-output state. `c64pla.c:40-46,106-123` distinguishes static output history from reset port fields; `viacore.c:378-424` includes `oldpb = 0`. Do not initialize previous output from a guessed current effective port value.
- `Fixture_AudioRingAndPhase`: pin ring contents/indices, chip enable, start clock, halfwave flag, square sign and split-pulse history. `datasette-sound.c:36-52` shows that clearing queue indices is not a universal reset of every static sound field. Assert emitted samples and order, not only audio-enabled state.
- `Fixture_RtcPersistentAndHostClock`: `pcf8583.c:136-161` loads persistent RAM/registers/offset and initializes serial state. Its I2C start at `213-249` uses a halt latch or `rtc_get_latch(offset)`, but reads centiseconds separately. `rtc.c:58-61,192-195` delegates centiseconds to the platform and seconds to `time(NULL) + offset`; calendar helpers use `localtime`. Pin/capture persistent context, host-time inputs, timezone and call order. Halting seconds alone does not establish determinism.

If a required native input or observation cannot be controlled/captured with an approved wrapper without changing VICE, retain a blocked case. Do not patch VICE, synthesize an expected success or silently skip it. Native VSF remains canonical and its required real round-trip/cross-load gates remain in scope; a snapshot file is not assumed to contain every process-static state above.

## N-08: lifecycle transitions, not just final state

`datasette_set_tape_image` assigns the new image before `datasette_internal_reset` (`879-888`). The reset selects mode from that new image or retained `notape_mode`, unsets an alarm only for selected START/FORWARD/REWIND, calls public `datasette_control`, and clears `motor_stop_clk` without clearing the native running flag (`952-984`). UI motor-off is a separate observation. Public control suppresses playback-time commands and routes network-connected operations differently (`1159-1169`).

Required fixtures:

- `Lifecycle_AttachNewStopImage_PendingAlarm`: old pending read alarm plus new STOP image; inspect whether it survives, and compare the subsequent actual callback. Cover motor running/stopped, enabled/disabled forwarding and stop deadline present/absent.
- `Lifecycle_Detach_ReplaysNoTapeMode`: retain no-image START/FORWARD/REWIND history, attach then detach, and reset with no image. Assert helper invocation, alarm timing and transient sense before attachment's final clear. Include retained STOP as a contrast.
- `Lifecycle_Reset_CancelsStopWithoutClearingRunning`: pending delayed stop, reset/attach, cleared deadline, retained internal motor, UI indication and subsequent callback. Do not describe the retained motor as permanently running regardless of future commands.
- `Lifecycle_ControlDispatch_EventPlaybackAndNetwork`: ordinary direct commands, playback-suppressed commands and network-routed commands; pin actual routing and event ordering. Include CPU-reset resource and autostart reset-handling alternatives.

All comparisons include intermediate ordered events. A final sense value alone cannot distinguish a transient assertion followed by clear.

## N-09: stream cursor and logical image position differ

`datasette_move_buffer_forward` seeks to the logical position plus header offset and reads an entire buffer (`391-409`). The C stream cursor can consequently be ahead of the logical pulse position. `datasette_start_motor` seeks (`1000-1005`), whereas accepted RECORD only changes mode/sense and resets gap state (`1091-1099`). Recording writes to the current stream and increments logical position separately (`1238-1268`).

`Recording_FileCursor_CommandOrder` must retain RECORD-before-motor, already-running-motor RECORD, read-then-RECORD, and intervening START/seek cases. `Recording_FileCursor_BufferBoundary` covers beginning/interior/end positions and before/at/after buffer refill, preserving prefix/suffix and failure/partial-write observations.

These sequences remain in scope. Pin exact native executable, platform/C runtime, update/read-only file mode, initial media bytes, I/O prelude and repeated native observations. The draft does not assert a universal platform-independent outcome for read/write sequencing. An unstable or unobservable oracle blocks the case. Do not insert a native seek to make the target easier to reproduce or exclude a troublesome ordering.

## N-10: header acceptance is not decoder semantics

`tap_open` derives payload size from the actual file and rejects fewer than three bytes (`tap.c:250-256`). The header stores its version byte; `tap_get_pulse` has explicit zero-byte cases only for versions 0, 1 and 2 (`389-456`). Unknown versions return zero for a zero byte without consuming a three-byte extension. The regular live path instead takes the extension branch for zero bytes of every nonzero version (`datasette.c:504-535,607-631`).

Qualify that last observation: `datasette_read_gap` has only version-1 and version-2 branches for C16 tape behavior (`632-692`). Unknown C16 live versions do not follow the regular C64/VIC-20 extension path. This source qualification does not expand the emulator target to C16; it prevents a false universal format rule.

Required fixtures:

- `TapOpen_ActualSize_ZeroOneTwoThree`: physical payload lengths 0, 1, 2 and 3, independent of declared length.
- `TapOpen_DeclaredLength_IndependentOfActualSize`: valid physical payloads of at least three bytes, declared length below/equal/above actual, and independent short-read/magic cases.
- `TapHeader_AllVersionBytes`: all 256 version bytes with valid unrelated header fields. Header acceptance and subsequent decoding are separate assertions.
- `TapUnknownVersion_BlockVsLive`: versions 3 through 255, zero/nonzero bytes, complete/all-zero/truncated extensions and following-byte alignment, under pinned regular machine tape behavior. Keep decoded units, consumed bytes, physical cursor and logical position distinct.
- `TapVersion2_TruncatedSecondHalfwave`: absent second pulse and partial second extension, plus complete pair. On failed `fread(size,3,1)`, physical stream consumption is not proven by `pos_advance`; retain both observations where available.

## N-11: dependencies are not implicit approvals

Fresh effective read confirms `FR-ARCH-TRUEDRIVE-1541-002` is closed with zero AC. Its separate proposed drive criteria remain unapproved. The composite explicitly retains that approval dependency and the underlying true-drive/media obligations. It does not treat the empty live AC list as successful acceptance.

`FR-RUNTIME-TAPE-002` already exists in the live corpus. The new media wording refers to that existing record and its proposed amendment, not to a nonexistent future record. Neither the earlier application nor this draft approves the revised AC. These are dependency corrections, not emulator test evidence.

## N-12: keep all turbo-loader obligations

The fresh `FR-TAP-005` read contains six unsatisfied criteria. All six IDs remain in the composite. Named formats Novaload, Biturbo, Turbotape, Cyberload and Pavloda still must load successfully; matching two failures does not satisfy them. Missing real fixtures remain explicit missing evidence.

The approximately-160-cycle example remains a workload obligation, not a minimum parser threshold. Tests must distinguish block pulse units from live clocks. CIA1 FLAG timing means exact native ordered observations, not permission for a one-cycle divergence. `c64datasette.c:36-49` and `vic20datasette.c:37-49` expose different machine interrupt routing, which the draft keeps separate. `datasette.c:714-851` distinguishes alarm scheduling, actual callback offset, initial flux, long-gap subdivision and speed handling. Source comments about hardware intent cannot replace executed native behavior.

Required case groups cover each named loader, pulse widths below/at/above 160, custom encoding, precise C64 FLAG state, separate VIC-20 CA1 obligations, half/full-wave block versus live behavior, and a continuous mid-load rate-change workload. PAL/NTSC machine settings and media tags remain independently pinned. No unconditional cross-standard rescaling or universal halfwave conversion is introduced.

## Unclosed work

This audit supplies explicit proposed obligations and source anchors. It does not supply all fixture bytes, native observation adapters, executable mock cases, real traces, whole-plan coverage or a new HV verdict. The remaining unchanged CPU capacitor-duration criterion also needs native variant audit before the expanded packet is review-ready.
