# Draft transport, machine wiring and lifecycle contract

Status: unapplied R0 supplement, not executed tests or an acceptance result. These fifteen groups plus the eleven [recording groups](RECORDING-CONTRACT.md) describe the remaining 26 groups in the existing `FR-RUNTIME-TAPE-002` coverage list. The earlier [motor matrix](../20261011T085402-0500-tape-motor-matrix/MOTOR-CONTRACT.md) covers ac-03/ac-04. Neither the 41-group count nor documentation coverage proves all tape dependencies or real-provider coverage complete.

The immutable source commit is `cc67418cba58e71d8e70fd353e9de47199ad2075`; `contract-vectors.json` pins each inspected source file against its Git blob and SHA256. No native edits are permitted. Public command/register preludes, process history and observed callback order are part of every fixture. Unobservable state or unsupported setup remains an explicit open gate.

## TransportSense_MediaCommandMatrix (ac-01)

Cross absent/read-only/writable media, STOP/START/FORWARD/REWIND/RECORD, datasette enabled 0/1, and internal motor 0/1. Run independently on C64 and VIC-20 PAL/NTSC. Reset every case to a proven STOP prelude; initial forwarded sense=0, no stop deadline, no pending alarm. Do not claim a native reset clears all function-static history.

STOP keeps mode STOP and requests sense=0. START/FORWARD/REWIND select that mode and request sense=1, including when no image is attached (the notape_mode field is used). RECORD selects RECORD and requests sense=1 only for writable media; absent/read-only RECORD leaves mode and sense unchanged. All accepted commands reset W=0. START/FORWARD/REWIND while M=1 then invoke the start helper, which seeds W=C/B=0. Writable RECORD never invokes that helper. Rejected RECORD preserves W/B.

Every internal command reports the selected control mode and clears last_tap/next_tap, including rejected RECORD. Enabled=0 suppresses forwarded sense calls, not the mode, buffer or UI changes; assert the previous machine-side sense remains rather than inventing a zero write. Add a reachable read-only START/running prelude before rejected RECORD to prove nonzero W and active sense are retained.

FORWARD/REWIND set the fast alarm C+1000 even if M=0. START while M=1 sets C+32000 only when no alarm is pending. STOP and RECORD do not universally remove alarms. Expected alarm call order is the existing motor contract, not inferred transport intent.

Negative controls: rejected RECORD becomes accepted, disabled sense callback occurs, no-image START is rejected, buffers survive rejected RECORD, STOP instantly clears M, or fast wind waits for motor-on before scheduling.

Source: `src/datasette/datasette.c:1041-1169,1000-1021`.

## MachineSense_C64CpuPortAndVic20Via1 (ac-02)

For each machine cross sense 0/1, DDR bit input/output and output latch bit 0/1. Expected input bit is !sense when configured input, otherwise the output latch bit. C64 uses CPU port $0001 bit4; VIC-20 uses VIA1 PA bit6, not VIA2 or a CPU-port surrogate. `senseRows` supplies all sixteen bit-level rows.

Also assert complete register bytes using independent surrounding-bit fixtures:

- C64: new data_out=(old data_out & ~dir)|(data & dir), truncated to eight bits. data_read=(data|~dir)&(new data_out|pullup), truncated to eight bits; apply the explicit caps/sense/write-in/motor-in masks at `c64pla.c:57-74`. Normal C64 uses pullup=0x17, caps_sense=1. Input bit5 clears even when motor_in=0. Vary old data_out and unrelated DDR/latch bits with 0x00,0xFF,0x55,0xAA backgrounds. Do not overwrite all bits with the isolated sense formula.
- VIC-20: joy_bits=((joy & 7)<<2)|((joy & 0x10)<<1), OR 0x40 only when sense=0. Read=(PRA&DDRA)|((iec_pa_read|joy_bits)&~DDRA), truncated to eight bits. The IEC provider's non-IEC bits are zero. Exercise joystick patterns 0,7,0x10,0x17 and all eight IEC combinations of bits0,1,7, with the four backgrounds above.

Negative controls: active-high sense, ignoring DDR, wrong chip/bit, clearing unrelated input bits or allowing fake IEC sense bits. Source: `src/c64/c64pla.c:51-99`, `src/c64/c64mem.c:214-223`, `src/vic20/vic20via1.c:221-252`.

## TapeRead_PulseInterruptRouting (ac-02)

Keep flux notification and a read-input level distinct. Cross machine, port1/other port, on=0/1, and interrupt mask enabled/disabled. Neither is a tape-sense notification.

- C64 flux on either on value invokes CIA1 `ciacore_set_flag` once. C64 read_in invokes it only for nonzero on. Wrong port invokes neither. After catch-up at clock C, FLAG bit0x10 is ORed into irqflags/new_irqflags and cleared from ack_irqflags. If idle alarm is later than C+1, it is rescheduled to C+1; an earlier/equal alarm is retained. This is asynchronous flag handling, not an unconditional immediate CPU IRQ.
- VIC-20 flux on either on value invokes VIA2 CA1 falling edge. read_in invokes VIA2 CA1 rising for on=1 and falling for on=0. Wrong port invokes neither. For PCR bit0 equal to edge (fall=0/rise=1), IFR bit0x02 sets and the IRQ callback uses current C and `(IFR & IER & 0x7f)!=0`. A nonmatching edge does not set CA1. Include other pending IFR bits to avoid equating IRQ only with CA1. In CA2 handshake mode with output low, a qualifying CA1 edge raises CA2 before setting IFR; test that ordered callback. Input-latching behavior is conditional on the pinned build's MYVIA_NEED_LATCHING and ACR setting, not a universal no-op.

Final CPU interrupt timing must be bound to the pinned CIA model/scheduler and VIA build in real traces. This supplement does not invent the missing whole-chip timing receipt. Sense-only changes must not generate flux interrupts.

Negative controls: use VIA1 instead of VIA2, require on=1 for flux, apply CIA level rules to VIC read_in, ignore PCR edge selection, set CPU IRQ with mask clear, discard unrelated IFR bits, or assert synchronous C64 CPU IRQ merely because FLAG was called.

Source: `src/c64/c64datasette.c:36-70`, `src/vic20/vic20datasette.c:37-70`, `src/core/ciacore.c:592-600,1594-1625`, `src/core/viacore.c:203-214,441-458`, `src/cia.h`, `src/via.h`.

## MachineMotor_C64Bit5AndDdr (ac-07)

Native motor argument is `!((DDR & DATA)&0x20)`. Cross DDR5/DATA5 in {0,1}; expected motor values are 1,1,1,0 for (0,0),(0,1),(1,0),(1,1). Callback occurs only when raw `(DDR&DATA)&0x20` differs from retained old_port_data_out. Test data and DDR writes independently, repeats, unrelated-bit changes and histories before/after CPU-port reset. The comparison cache initially is 0xFF and is not cleared by `c64pla_pport_reset`.

## MachineWrite_C64Bit3AndDdrEdges (ac-07)

Native write argument is `(~DDR|DATA)&8`, raw 0 or 8. Input always produces 8; output follows DATA3. Callback only on change from retained old_port_write_bit (initially 0xFF, not reset by CPU-port reset). Hold DATA3=0 and transition DDR3 output to input: 0 to 8 must reach recording as a qualifying edge. Reverse DDR direction: 8 to 0 reaches recording but emits no byte. Repeating either DDR or DATA must not repeat the callback. When a config change alters multiple outputs, motor callback precedes write callback, which precedes sense-out callback. That order can change the first recording gap and is asserted.

## MachineMotor_Vic20PcrRawByte (ac-07)

For every old/new PCR byte pair in 0..255, identical byte means no motor callback. Any different byte produces motor=`!(newPCR&2)`, even if bit1 is unchanged. The source computes a normalized temporary byte but does not use it for the motor. `vicPcrRows` provides 36 sentinel pairs, not the full 65536-pair denominator. In particular 0 to 12 produces motor=1, 0 to 2 produces motor=0, and 0 to 32 produces motor=1. Preserve the separate userport PA2 callback from raw bit5. Do not replace the result with idealized CA2 output mode semantics.

## MachineWrite_Vic20Pb3AndDdrEdges (ac-07)

Through VIA2 register storage, effective PB is PRB|~DDRB; timer output can replace bit7 but not bit3. Both PRB and DDRB writes reach store_prb. If effective PB3 differs from oldpb3, the callback argument is raw `(~DDRB|effectivePB)&8`. Update oldpb after dispatch. Exercise all sixteen old/new (PRB3,DDRB3) combinations, actual register-write order, repeated writes, unrelated bits, and PB7 timer mode. Unit truth-table rows treat the old effective value as a prelude result; they do not inject oldpb in the native provider. Separate the always-present joystick output callback from tape-write calls.

## MachineTape_Sx64Disconnected (ac-07)

Repeat the C64 DDR/data cases with BOARD_SX64. No motor, write-bit or sense-out tapeport callback is made, but the old output caches still update. mem_pla_config_changed passes sense=write_in=motor_in=caps_sense=1 and pullup=0x07; input tape bits clear under their masks, while DDR output reads still follow latches. Capture full data_read, not a blanket zero result. Preserve warm-process history if switching board models is supported; otherwise use separate processes and report that boundary.

Negative controls for all ac-07 groups: omit DDR-induced edge, callback on every same value, normalize raw8 to1, invert active-low motor, use normalized PCR instead of raw bit1, ignore a changed unrelated PCR bit, reset retained port caches, or invoke disconnected SX64 callbacks.

Sources: `src/c64/c64pla.c:39-123`, `src/c64/c64mem.c:214-223`, `src/vic20/vic20via1.c:185-203`, `src/vic20/vic20via2.c:110-119`, `src/core/viacore.c:717-724`.

## Datasette_EnableAndFluxGating (ac-08)

Enable normalizes any nonzero argument to1 and only changes datasette_enabled; it does not synthesize sense, reset the device, stop the motor or clear pending alarms. Test arguments 0,1,2,-1 and the next transport command. Datasette sense forwarding is gated by enabled. During attached START, a read callback sends flux only if enabled and no long_gap_pending. Test both enabled states, pending gap0/nonzero, fullwave0/1; disabled suppresses the output call but not automatically all read/state advancement. FORWARD/REWIND do not send that START flux call.

## Tapeport_DispatchAndPowerupGating (ac-08)

For set_motor, toggle_write_bit and set_sense_out, cross current device NONE/non-NONE, name null/present, callback null/present, and tapeport_active0/1. Dispatch exactly once iff non-NONE with name and callback; active does not add a gate here. Powerup additionally requires active and iterates valid ports with the same registration checks. Native Datasette's descriptor has no powerup callback; use a supported registered test device for that dispatch contract, not an invented Datasette reset.

Flux, read_in, tape_sense, write_in and motor_in functions forward directly to the machine. No device/active guard belongs in those wrappers. Machine-specific wrong-port behavior is separate. Do not invoke invalid array indices to test a missing guard. Each mock must reject the mutant that suppresses every signal when inactive.

## Datasette_AttachDetachResetState (ac-08)

Attachment assigns current_image before internal reset. It resets buffer indices, allocates the buffer if needed, scans gaps to compute total counter, sets logical P=0, selects sound halfwaves only for version2, forwards final sense0 if enabled, clears buffer indices/fullwave, reports attachment status. Detach frees the buffer if allocated. The scan is real stateful decoding, not a pure file-length calculation. Record physical stream position separately from logical P.

Pin images, process history, resources and decoded scan effects. Include null/non-null buffer, fresh/warm process, TAPv0/v1/v2 and autostart_ignore_reset0/1. A v1 payload `01 02 03` with neutral tuning/wobble/azimuth provides three short gaps; unknown-version and nonneutral cases remain required dependencies, not replaced by this fixture.

CPU reset consults DatasetteResetWithCPU. Resource0 produces no internal reset from this entry point; resource1 resets valid ports. Explicit transport RESET is a different path and must not be blocked by that resource. Public RESET internally calls internal_reset and then falls through to STOP, so ordered duplicate control/sense/UI effects cannot be reduced to one final STOP assertion. Internal reset preserves M and B, clears D, long-gap elapsed/pending, direction, counter offset and fullwave; image counter becomes0. Unless autostart_ignore_reset, tap_seek_start clears decoded-file data/size, sets file number=-1, P=0 and seeks to payload offset. Its fseek return is not propagated as a reset failure. UI motor-off is not evidence M=0.

## Lifecycle_AttachNewStopImage_PendingAlarm (ac-08)

At C=100000, retain M=1 and an old START read alarm A=110000. Attach a new image whose mode is STOP. Reset chooses the NEW mode; it does not unset A merely because the old image was playing. It requests STOP on the new image, resets gap state and D, and reports UI motor-off while M stays1. Attachment scanning and final sense0 still occur. At A, the callback consumes the pending alarm against the new STOP image; do not read the old image or invent immediate alarm removal. Pair with a new START-mode fixture only where the public API actually allows that state; otherwise retain it as a unit seam case with real reachability open.

## Lifecycle_Reset_CancelsStopWithoutClearingRunning (ac-08)

Start with M=1, D=132000 and an attached STOP image with pending A=140000. Internal reset clears D but preserves M and A, resets image counters/gap fields, and emits UI motor-off. Later A consumes the alarm but does not execute the old stop deadline, since D=0. For an initial START/FORWARD/REWIND image, reset unsets A before requesting STOP; test separately. Pair autostart-ignore0/1 and resource-controlled CPU reset. Negative control: clear M because the UI says off, retain D, or always cancel A.

## Lifecycle_Detach_ReplaysNoTapeMode (ac-08)

Prepare retained notape_mode through no-image START/FORWARD/REWIND, then attach an image and later detach it. Assignment of NULL precedes internal reset, so reset selects the retained notape_mode rather than the removed image's mode. With normal local event dispatch, it unsets a pending alarm for these modes then invokes that command again. With M=1, START schedules C+32000 and seeds W=C/B=0; FORWARD/REWIND schedule C+1000, and the helper preserves that alarm while seeding W/B. With M=0, START leaves no alarm; fast modes still schedule C+1000. D becomes0, M is retained. Sense1 from replay precedes final attachment sense0 when enabled. UI not-attached and buffer freeing follow. Assert the ordered transient sense, mode and alarm effects, not just the final sense0. Include retained STOP as a control.

## Lifecycle_ControlDispatch_EventPlaybackAndNetwork (ac-08)

Public control with event_playback_active returns before recording events, internal dispatch, UI changes or clearing buffers. Otherwise, it records EVENT_DATASETTE with exactly one uint32 command (four bytes in the pinned native ABI). Network-disconnected records locally and invokes internal control; network-connected records through network_event_record but does not immediately invoke internal control. Playback-port entry points read uint32 command and invoke internal control for their named port; the supplied offset is unused by those functions. Preserve port routing; do not invent a port field in this payload.

Repeat reset/attach and write-failure sequences under these routes. Internal reset still performs its own field/UI work after a suppressed/deferred public STOP; that does not imply STOP's mode/buffer/sense effects executed. Distinguish public event playback suppression from the playback callback that actually applies the command.

Sources for ac-08: `src/datasette/datasette.c:155-174,241-249,775-778,879-998,1041-1169,1328-1357`; `src/tapeport/tapeport.c:245-322`; `src/tape/tap.c:1558-1568`.

## Gate boundary

These contracts preserve known VICE quirks rather than intended hardware behavior. They require discriminating green mocks before implementation, then identical assertions against real providers and full current-plus-prior gates. Source identity, docs links and vector arithmetic checks cannot satisfy those gates. Native observability, CRT behavior, process-static state, full chip IRQ timing, successful loader fixtures, amendment guards, superseded-test disposition, coherent handoff and whole-packet Opus review remain open until separately evidenced.
