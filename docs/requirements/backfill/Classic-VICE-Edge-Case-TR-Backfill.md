# Classic VICE Edge-Case TR Backfill

## Document Information

| Field | Value |
|-------|-------|
| Source Corpus | `native/vice/vice/src` |
| Inventory | `docs/requirements/backfill/Classic-VICE-Edge-Case-Inventory.md` |
| Scanner | `tools/audit_vice_edge_cases.ps1` |
| Generated | 2026-05-21 |
| Scope | Requirements backfill; implementation status is tracked separately as Phase 1 slices land. |

## Promotion Rule

VICE source is a valid TR source only for observable compatibility behavior that ViceSharp must match. Source matches that are internal implementation choices, host-driver plumbing, logging behavior, or unsupported machine-only behavior are not promoted unless they create externally visible emulator behavior.

## Scanner Summary

The repeatable scanner reviewed 812 C/C++/header/inc files under `native/vice/vice/src` and found 11,235 candidate lines grouped by subsystem. Candidate lines were deduplicated by normalized text and then manually reviewed for observable behavior. The accepted records below are the promoted TR set for this backfill batch.

## Accepted Technical Requirements

### TR-VIC-EDGE-001: Invalid VIC-II Mode Priority and Collision Semantics

**ID:** TR-VIC-EDGE-001

**Phase 1:** Required for C64/x64sc display-mode parity.

**Behavior Summary:** Invalid VIC-II ECM/BMM/MCM selector combinations render as no visible graphics color, but x64sc still derives the hidden foreground priority bit from the underlying bitmap or character pixel. Sprite priority and sprite-background collision logic consume that hidden bit even when the visible pixel is rendered as background/no color.

**Affected Machine/Profile:** C64, C64C, SX-64, and x64sc VIC-II variants.

**Source References:**

- `native/vice/vice/src/viciisc/vicii-draw-cycle.c:41` defines `COL_NONE`.
- `native/vice/vice/src/viciisc/vicii-draw-cycle.c:133-141` maps invalid ECM/BMM/MCM selector combinations to `COL_NONE`.
- `native/vice/vice/src/viciisc/vicii-draw-cycle.c:196-224` stores `pixel_pri = px & 0x2` independently of visible color.
- `native/vice/vice/src/viciisc/vicii-draw-cycle.c:401-428` consumes the priority buffer for sprite-background and sprite-sprite collision behavior.

**Acceptance Expectation:** Invalid-mode visible pixels match x64sc output, and sprite priority/collision tests prove the hidden foreground bit still participates in collision and priority decisions.

**Traceability:** FR-VIC-002, FR-VIC-003, FR-VIC-005, TEST-VIC-001.

**Current Implementation Evidence:** The 2026-05-21 invalid ECM slice routes display-mode pixels through a display-mode-aware foreground/priority helper used by both collision processing and framebuffer rendering. Focused synthetic coverage exercises invalid ECM visible color, `$D01B` priority, and `$D01F` sprite-background collision behavior; native visible-frame/checkpoint validation remains open under `BACKFILL-VIDEO-001`.

### TR-VIC-EDGE-002: VIC-II Border Flip-Flop Cycle Checks

**ID:** TR-VIC-EDGE-002

**Phase 1:** Required for C64/x64sc border and sprite visibility parity.

**Behavior Summary:** Vertical and horizontal VIC-II border state is controlled by model-specific cycle-table checks. CSEL/RSEL changes can skip the side-border set or clear checks and leave the border open; closed vertical or side borders mask sprite pixels regardless of sprite sampling.

**Affected Machine/Profile:** C64, C64C, SX-64, and x64sc VIC-II PAL/NTSC variants.

**Source References:**

- `native/vice/vice/src/viciisc/vicii-cycle.c:168-202` implements vertical and horizontal border checks.
- `native/vice/vice/src/viciisc/vicii-cycle.c:408` and `native/vice/vice/src/viciisc/vicii-cycle.c:480-482` invoke border checks from the cycle path.
- `native/vice/vice/src/viciisc/vicii-chip-model.c:92-95` defines the left/right border check flags.
- `native/vice/vice/src/viciisc/vicii-chip-model.c:145-147` and `native/vice/vice/src/viciisc/vicii-chip-model.c:223-225` place PAL side-border checks on the cycle table.
- `native/vice/vice/src/viciisc/vicii-chip-model.c:306-308` and `native/vice/vice/src/viciisc/vicii-chip-model.c:384-386` place NTSC side-border checks on the cycle table.

**Acceptance Expectation:** Border flip-flop state changes occur on the same model-specific public cycles as x64sc, closed borders mask sprites, and open-border tricks stay visible only when the relevant border check is skipped or cleared.

**Traceability:** FR-VIC-007, FR-VIC-004, FR-VIC-005, TEST-VIC-001.

**Current Implementation Evidence:** Commit `646b3a1` carries opened side-border state through `Mos6569` and `VideoRenderer`. Managed coverage includes right-open, left-carry, continuous carry, cycle-17 blank-line behavior, and PAL/NTSC/PAL-N/old-NTSC/HMOS border cycle invariance. Native x64sc side-border checkpoints and broader visible-frame validation remain open under `BACKFILL-VIDEO-001`.

### TR-VIC-EDGE-003: VIC-II Badline, Idle-State, and RC Windows

**ID:** TR-VIC-EDGE-003

**Phase 1:** Required for C64/x64sc raster and badline parity.

**Behavior Summary:** Badline activation depends on display-enable timing, raster low bits, first/last DMA line state, idle-state transitions, and RC update windows. Once active, badline fetches and CPU-steal timing must match the x64sc cycle model rather than a line-level approximation.

**Affected Machine/Profile:** C64, C64C, SX-64, and x64sc VIC-II variants.

**Source References:**

- `native/vice/vice/src/viciisc/vicii-cycle.c:56-61` sets badline and idle-state state.
- `native/vice/vice/src/viciisc/vicii-cycle.c:527-565` gates badline allowance, RC update, and idle-state transitions.
- `native/vice/vice/src/viciisc/vicii-cycle.c:576-598` ties badline state to BA and matrix fetch cycles.
- `native/vice/vice/src/vicii/vicii-fetch.c:135-166` schedules matrix fetches and badline state in the non-SC fetch path.

**Acceptance Expectation:** Badline-driven matrix fetches, idle-state transitions, and CPU DMA steals match x64sc traces for display-enable, Y-scroll, and FLI-style forced badline cases.

**Traceability:** FR-VIC-001, FR-VIC-006, FR-VIC-008, TEST-VIC-001.

### TR-VIC-EDGE-004: VIC-II Sprite DMA Latch and Per-Model Fetch Tables

**ID:** TR-VIC-EDGE-004

**Phase 1:** Required for C64/x64sc sprite multiplexing and CPU-stall parity.

**Behavior Summary:** Sprite DMA is latched by sprite-enable and Y-match checks before the fetch slots, and later fetch/BA behavior comes from per-model cycle tables. Clearing a sprite enable bit after the latch point does not cancel already-active DMA for that line.

**Affected Machine/Profile:** C64, C64C, SX-64, and x64sc VIC-II PAL/NTSC variants.

**Source References:**

- `native/vice/vice/src/viciisc/vicii-cycle.c:118-128` samples `$D015` and sprite Y to turn sprite DMA on.
- `native/vice/vice/src/viciisc/vicii-cycle.c:503` runs sprite DMA checks from the cycle path.
- `native/vice/vice/src/viciisc/vicii-cycle.c:626` carries the sprite 0 late-line check.
- `native/vice/vice/src/viciisc/vicii-chip-model.c:735-746` derives sprite BA and sprite DMA fetch table entries.
- `native/vice/vice/src/viciisc/vicii-fetch.c:275-309` performs sprite pointer and data fetches.

**Acceptance Expectation:** Sprite DMA latch tests, BA stall windows, p-access/s-access slots, and late-line/early-line rollover match x64sc for each supported VIC-II timing model.

**Traceability:** FR-VIC-004, FR-VIC-006, FR-VIC-010, TEST-VIC-001.

### TR-VIC-EDGE-005: VIC-II Matrix Fetch Idle and `0xff` Fill Behavior

**ID:** TR-VIC-EDGE-005

**Phase 1:** Required for C64/x64sc graphics fetch parity.

**Behavior Summary:** VIC-II matrix and graphics fetches use observable idle/fill values, including `0xff` matrix fill, RAM-derived color nibbles in some idle paths, and special address latching when changing between RAM and character ROM fetches.

**Affected Machine/Profile:** C64, C64C, SX-64, and x64sc VIC-II variants.

**Source References:**

- `native/vice/vice/src/viciisc/vicii-fetch.c:192-199` fills prefetch matrix bytes with `0xff` and color nibbles from RAM.
- `native/vice/vice/src/viciisc/vicii-fetch.c:208-227` reads idle and idle graphics addresses.
- `native/vice/vice/src/viciisc/vicii-fetch.c:234-264` implements the 6569 fetch-address latch behavior during RAM/ROM mode changes.
- `native/vice/vice/src/vicii/vicii-fetch.c:72-109` handles matrix `0xff` fill and background color source behavior.

**Acceptance Expectation:** Idle graphics, matrix `0xff` fill, background-color source, and RAM/ROM transition fetch tests match x64sc visible output and fetch traces.

**Traceability:** FR-VIC-001, FR-VIC-006, FR-VIC-008, FR-VIC-009, TEST-VIC-001.

**Current Implementation Evidence:** The 2026-05-21 matrix/idle slice adds managed coverage for VICE `viciisc/vicii-fetch.c` behavior: prefetch slots latch matrix `$ff`, prefetch color nibbles are read from raw CPU-program RAM at the visible PC (`ram_base_phi2[reg_pc]`), real matrix fetches latch screen bytes plus color RAM low nibbles, standard text graphics fetches consume the populated matrix latch, and idle graphics fetches use `$39ff` only when ECM is active. `VicIIMatrixIdleFetchTests` covers these managed latch/address cases; focused matrix/idle plus adjacent bad-line/core-timing validation passed `18/18`, and broader VIC/video validation passed `179/179`. Native x64sc matrix checkpoints and the 6569 RAM-to-character-ROM fetch-address latch path from `viciisc/vicii-fetch.c:234-264` remain under `BACKFILL-VIDEO-001`.

### TR-VIC-EDGE-006: VIC-II Register Readback and Collision Latch Semantics

**ID:** TR-VIC-EDGE-006

**Phase 1:** Required for C64/x64sc register-level parity.

**Behavior Summary:** VIC-II register reads must expose unused bits as ones according to register-specific masks, return IRQ status with fixed high bits, treat collision registers as read-only on writes, and clear collision latches on the same read side effects as x64sc.

**Affected Machine/Profile:** C64, C64C, SX-64, and x64sc VIC-II variants.

**Source References:**

- `native/vice/vice/src/viciisc/vicii-mem.c:48-63` defines unused-bit readback masks.
- `native/vice/vice/src/viciisc/vicii-mem.c:229` clears IRQ bits from `$D019` writes.
- `native/vice/vice/src/viciisc/vicii-mem.c:265-267` treats collision-register writes as read-only.
- `native/vice/vice/src/viciisc/vicii-mem.c:517` returns IRQ status with `0x70` high bits.
- `native/vice/vice/src/viciisc/vicii-mem.c:522-554` latches and clears collision state on reads.
- `native/vice/vice/src/viciisc/vicii-mem.c:570-713` applies hard-coded readback masks for VIC-II registers.

**Acceptance Expectation:** Register readback, IRQ clear, collision read-clear, and unused-bit tests match x64sc for `$D000-$D03F`.

**Traceability:** FR-VIC-001, FR-VIC-005, TEST-VIC-001.

**Current Implementation Evidence:** The 2026-05-21 register-readback slice applies VICE `vicii-mem.c` hardcoded read masks for `$D019` fixed bits 6-4, `$D01A` high-nibble readback, unused `$D02F-$D03F` reads as `$FF`, and collision-register write-ignore behavior. Focused register-readback/IRQ/collision validation passed `37/37`; broader VIC/video validation passed `174/174`. Native x64sc register checkpoint expansion remains under `BACKFILL-VIDEO-001`.

### TR-VIC-EDGE-007: VIC-II VSP Bug Memory-Corruption Behavior

**ID:** TR-VIC-EDGE-007

**Phase 1:** Deferred unless Phase 1 explicitly includes VSP/AGSP demo compatibility.

**Behavior Summary:** x64sc simulates the VIC-II VSP bug with line/channel probability tables and memory corruption when a qualifying badline follows an idle, non-badline state in the vulnerable cycle window. This behavior is externally observable as memory corruption in VSP/AGSP programs.

**Affected Machine/Profile:** C64 and x64sc VIC-II profiles; deferred compatibility feature.

**Source References:**

- `native/vice/vice/src/viciisc/vicii-cycle.c:259-261` defines VSP probability thresholds.
- `native/vice/vice/src/viciisc/vicii-cycle.c:314-346` documents and evaluates the VSP bug trigger.
- `native/vice/vice/src/viciisc/vicii-cycle.c:524` captures the vulnerable prior idle state.
- `native/vice/vice/src/viciisc/vicii-cycle.c:536-540` checks the VSP bug condition during badline evaluation.

**Acceptance Expectation:** When VSP support is enabled, affected memory corruption behavior is deterministic under a controlled random seed and matches x64sc-compatible VSP test programs.

**Traceability:** FR-VIC-008, TEST-VIC-001.

### TR-CPU-EDGE-001: 6510 Interrupt Latency Edge Cases

**ID:** TR-CPU-EDGE-001

**Phase 1:** Required for C64/x64sc CPU parity.

**Behavior Summary:** 6510 interrupt dispatch must model the x64sc latency edge cases: BRK delays NMI by one opcode, taken branches delay IRQ/NMI by one cycle, and SEI changes IRQ delay-counter handling so a pending IRQ is deferred correctly.

**Affected Machine/Profile:** C64/x64sc 6510 CPU core.

**Source References:**

- `native/vice/vice/src/mainc64cpu.c:185-193` handles SEI-specific interrupt delay counters.
- `native/vice/vice/src/mainc64cpu.c:663-708` implements NMI/IRQ delay behavior for BRK, branches, and IRQ-enable changes.
- `native/vice/vice/src/maincpu.c:454-501` carries the generic 6510 interrupt-delay logic.

**Acceptance Expectation:** IRQ/NMI tests around BRK, taken branches, CLI/SEI, and pending interrupt lines match x64sc instruction and cycle traces.

**Traceability:** FR-CPU-002, FR-CPU-003, TEST-CPU-001.

### TR-CPU-EDGE-002: 6510 BA-Low Dummy Access and Bus Stall Semantics

**ID:** TR-CPU-EDGE-002

**Phase 1:** Required for C64/x64sc CPU/VIC bus parity.

**Behavior Summary:** CPU memory access helpers must distinguish real reads/writes from dummy reads/writes and apply BA-low checks to reads, stack accesses, and zero-page accesses so VIC-II and other DMA bus steals stall the CPU with the same side effects as x64sc.

**Affected Machine/Profile:** C64/x64sc 6510 CPU and VIC-II shared bus.

**Source References:**

- `native/vice/vice/src/mainc64cpu.c:270-288` defines dummy read/write helpers.
- `native/vice/vice/src/mainc64cpu.c:320-330` wires BA-low checks into load helpers.
- `native/vice/vice/src/mainc64cpu.c:362-371` implements BA-checked real and dummy reads.
- `native/vice/vice/src/mainc64cpu.c:395-448` routes load, dummy load, pull, and stack peek operations through BA-aware helpers.

**Acceptance Expectation:** CPU/VIC bus arbitration tests prove dummy accesses, stack accesses, zero-page reads, and DMA stalls match x64sc traces during badlines, sprite DMA, and REU/VIC contention.

**Traceability:** FR-CPU-002, FR-VIC-006, FR-VIC-010, TEST-CPU-001, TEST-VIC-001.

### TR-RAM-EDGE-001: VICE RAM Initialization Pattern Semantics

**ID:** TR-RAM-EDGE-001

**Phase 1:** Required when startup parity or RAM-init resources are part of the Phase 1 gate; otherwise deferred to configuration completion.

**Behavior Summary:** RAM initialization must support VICE-compatible start value, value inversion, pattern inversion, random span/repeat, and random chance behavior. C64 and other machines use different factory defaults derived from VICE test programs.

**Affected Machine/Profile:** C64/x64sc startup RAM, plus other machine profiles when enabled.

**Source References:**

- `native/vice/vice/src/ram.c:49-60` defines the RAM init parameter fields.
- `native/vice/vice/src/ram.c:137-178` documents x64sc and VIC-20 pattern defaults.
- `native/vice/vice/src/ram.c:197-221` exposes command-line/resource controls.
- `native/vice/vice/src/ram.c:233-339` implements random-bit and pattern initialization behavior.

**Acceptance Expectation:** Given the same RAM-init resources and random seed, startup RAM bytes match the VICE pattern model for C64/x64sc, including inversion and random-chance edge cases.

**Traceability:** FR-CFG-007, TEST-CFG-001, TEST-MEM-001.

### TR-SID-EDGE-001: SID ADSR Delay and Envelope Pipeline Semantics

**ID:** TR-SID-EDGE-001

**Phase 1:** Required for SID register/audio parity if SID is in the Phase 1 gate; analog-depth edge cases may be deferred by test scope.

**Behavior Summary:** SID envelope generation must model ADSR delay bug behavior, envelope decrement pipeline delays, exponential-counter transitions, hold-zero behavior, and rate-counter state transitions that are visible through audio output and ENV3 reads.

**Affected Machine/Profile:** C64 SID 6581/8580 profiles.

**Source References:**

- `native/vice/vice/src/resid/envelope.cc:42-55` documents the ADSR delay bug and sampling guidance.
- `native/vice/vice/src/resid/envelope.cc:94-122` documents exponential-counter delay behavior.
- `native/vice/vice/src/resid/envelope.cc:230-247` updates state and envelope pipelines.
- `native/vice/vice/src/resid/envelope.h:120-179` clocks envelope and exponential pipelines.
- `native/vice/vice/src/resid/envelope.h:386-412` maps exponential-counter period thresholds.

**Acceptance Expectation:** ADSR delay, envelope freeze/hold-zero, ENV3 sampling, and envelope transition tests match VICE/reSID behavior.

**Traceability:** FR-SID-001, FR-SID-002, TEST-SID-001.

### TR-SID-EDGE-002: SID Waveform Test-Bit, Noise, and Floating DAC Behavior

**ID:** TR-SID-EDGE-002

**Phase 1:** Deferred unless waveform-level SID parity is part of the Phase 1 gate.

**Behavior Summary:** SID waveform generation must model test-bit shift-register reset timing, combined waveform writeback, noise shift-register transitions, and floating DAC TTL differences between MOS6581 and MOS8580.

**Affected Machine/Profile:** C64 SID 6581/8580 profiles.

**Source References:**

- `native/vice/vice/src/resid/wave.cc:28-46` defines model-specific shift-register reset and floating-output TTL constants.
- `native/vice/vice/src/resid/wave.cc:172-197` determines combined-waveform pre-writeback.
- `native/vice/vice/src/resid/wave.cc:224-250` handles test-bit and shift-register reset behavior.
- `native/vice/vice/src/resid/wave.cc:254-290` shifts noise state and updates floating DAC TTL.

**Acceptance Expectation:** Waveform tests cover test-bit transitions, combined waveforms, noise shift state, and 6581/8580 floating-output fade behavior against VICE/reSID reference output.

**Traceability:** FR-SID-001, FR-SID-003, TEST-SID-001.

### TR-SID-EDGE-003: MOS8580 One-Cycle Write Pipeline and Digi Boost

**ID:** TR-SID-EDGE-003

**Phase 1:** Deferred unless 8580-specific audio parity is in the Phase 1 gate.

**Behavior Summary:** MOS8580 SID writes are delayed by one cycle in fast sampling mode, and the external input path can simulate the 8580 digi-boost hardware modification. These behaviors affect audible output and write/read timing in SID tests.

**Affected Machine/Profile:** C64C/MOS8580 SID profiles.

**Source References:**

- `native/vice/vice/src/resid/sid.cc:154` documents digi-boost external input use.
- `native/vice/vice/src/resid/sid.cc:202-215` implements the MOS8580 one-cycle write pipeline in fast sampling mode.
- `native/vice/vice/src/resid/sid.cc:749-752` clocks pipelined MOS8580 writes.

**Acceptance Expectation:** 8580 mode tests prove write side effects occur with the VICE/reSID pipeline delay and that digi-boost input affects generated samples when configured.

**Traceability:** FR-SID-001, FR-SID-004, TEST-SID-001.

### TR-IEC-EDGE-001: IEC ATN, EOI, ACK, NACK, and Bit Timeout Timing

**ID:** TR-IEC-EDGE-001

**Phase 1:** Required if Phase 1 includes IEC/drive loading beyond host-only PRG injection; otherwise deferred to drive completion.

**Behavior Summary:** IEC serial devices must model ATN edge handling, listener/talker role changes, EOI signaling, frame acknowledge, NACK recovery, and microsecond-scale clock/data timing windows.

**Affected Machine/Profile:** C64 IEC bus and 1541-style device interactions.

**Source References:**

- `native/vice/vice/src/serial/serial-iec-device.c:279-291` handles ATN falling/rising edge state.
- `native/vice/vice/src/serial/serial-iec-device.c:388-430` models initial listener timeout and EOI acknowledge timing.
- `native/vice/vice/src/serial/serial-iec-device.c:487-534` handles ATN/listen frame acceptance and acknowledge behavior.
- `native/vice/vice/src/serial/serial-iec-device.c:555-622` handles talker role reversal and EOI signaling.
- `native/vice/vice/src/serial/serial-iec-device.c:633-721` clocks bit transfer, ACK, NACK, and retry timing.

**Acceptance Expectation:** IEC protocol tests match x64sc device-line transitions and timing for ATN, EOI, byte acknowledge, NACK, and role reversal.

**Traceability:** FR-DRV-005, FR-DRV-006, TEST-DRV-001.

### TR-DRV-EDGE-001: VDrive Directory, BAM, and REL Flush Quirks

**ID:** TR-DRV-EDGE-001

**Phase 1:** Deferred unless disk-directory and REL-file behavior are in the Phase 1 gate.

**Behavior Summary:** Virtual drive behavior includes observable DOS quirks: 1541 directory traversal starts at track 18 sector 1, some directory/block counts are adjusted for native partitions, zero inputs are accepted in directory matching, and listen/unlisten events flush REL file buffers.

**Affected Machine/Profile:** 1541/1571/1581-style virtual drives and host filesystem drive emulation.

**Source References:**

- `native/vice/vice/src/vdrive/vdrive-dir.c:279-302` traverses 1541 directory blocks from track 18 sector 1.
- `native/vice/vice/src/vdrive/vdrive-dir.c:393-396` updates block counts for native partitions.
- `native/vice/vice/src/vdrive/vdrive-dir.c:515` explicitly allows zero inputs.
- `native/vice/vice/src/vdrive/vdrive-dir.c:749` documents 32-byte directory-entry formatting behavior.
- `native/vice/vice/src/serial/fsdrive.c:67` documents status-channel continuity.
- `native/vice/vice/src/serial/fsdrive.c:228-267` sends listen/unlisten events to flush REL file write buffers.

**Acceptance Expectation:** Directory listing, BAM/block-count, zero-pattern matching, status-channel, and REL flush tests reproduce VICE virtual-drive behavior.

**Traceability:** FR-DRV-001, FR-DRV-003, FR-DRV-005, TEST-DRV-001.

### TR-MEDIA-EDGE-001: P64 GCR Pulse Stream Weak-Bit and Syncmark Handling

**ID:** TR-MEDIA-EDGE-001

**Phase 1:** Deferred unless P64/GCR image parity is in scope.

**Behavior Summary:** P64/GCR conversion treats weak regions as special pulse values and preserves syncmark-border alignment considerations when converting between pulse streams and GCR bytes.

**Affected Machine/Profile:** Disk image and low-level GCR media behavior for C64 drives.

**Source References:**

- `native/vice/vice/src/lib/p64/p64.c:663-673` converts GCR bytes to pulse stream entries and emits `0xffffffffUL` for weak pulses.
- `native/vice/vice/src/lib/p64/p64.c:686-724` converts pulse streams back to GCR and notes syncmark-border realignment.
- `native/vice/vice/src/lib/p64/p64.c:724-763` carries logic-based GCR conversion and syncmark alignment notes.

**Acceptance Expectation:** P64/GCR tests preserve weak-bit regions and syncmark-aligned GCR byte reconstruction with VICE-compatible pulse semantics.

**Traceability:** FR-DRV-004, TEST-DRV-001.

### TR-TAP-EDGE-001: TAP Version, Long-Pulse, and TurboTape Threshold Behavior

**ID:** TR-TAP-EDGE-001

**Phase 1:** Required if TAP loading is part of the Phase 1 gate; otherwise deferred to tape completion.

**Behavior Summary:** TAP parsing must honor VICE pulse thresholds, version-specific long-pulse encoding, version 2 halfwave behavior, machine/video clock selection, and TurboTape pilot/header detection thresholds.

**Affected Machine/Profile:** C64 TAP loading, plus other TAP-tagged machines when enabled.

**Source References:**

- `native/vice/vice/src/tape/tap.c:52-82` defines CBM and TurboTape pulse thresholds.
- `native/vice/vice/src/tape/tap.c:91-105` maps TAP machine/video headers to clock rates.
- `native/vice/vice/src/tape/tap.c:121-192` validates TAP header machine/video fields.
- `native/vice/vice/src/tape/tap.c:389-422` decodes version 0/1/2 pulse lengths and halfwave behavior.
- `native/vice/vice/src/tape/tap.c:970-992` decodes TurboTape short/long pulse bytes.
- `native/vice/vice/src/tape/tap.c:1251-1385` selects CBM versus TurboTape pilot detection.

**Acceptance Expectation:** TAP fixtures cover version 0/1/2 pulse encoding, long pulses, clock selection, CBM pilot detection, and TurboTape pilot/header detection against VICE behavior.

**Traceability:** FR-TAP-002, FR-TAP-003, FR-TAP-005, TEST-TAP-001.

### TR-TAPE-EDGE-001: Tapeport Sense, Motor, and RTC/Dongle Line Semantics

**ID:** TR-TAPE-EDGE-001

**Phase 1:** Deferred unless tapeport peripheral line behavior is included in Phase 1.

**Behavior Summary:** Tapeport devices can assert sense independently of a datasette. The sense dongle asserts the sense line when enabled and after power-up, and the CP Clock F83 RTC combines motor-state and RTC data-line state to drive tape sense.

**Affected Machine/Profile:** C64 tapeport peripherals and datasette sense-line behavior.

**Source References:**

- `native/vice/vice/src/tapeport/sense-dongle.c:50-58` registers a tapeport dongle with no motor/write-line functions.
- `native/vice/vice/src/tapeport/sense-dongle.c:77` asserts tape sense when enabled.
- `native/vice/vice/src/tapeport/sense-dongle.c:93` asserts tape sense after power-up.
- `native/vice/vice/src/tapeport/cp-clockf83.c:161-174` combines motor state and RTC data-line state into tape sense.
- `native/vice/vice/src/tapeport/cp-clockf83.c:178-190` maps motor/write-line calls to RTC SDA/SCL transitions.

**Acceptance Expectation:** Tapeport peripheral tests prove sense-line state, motor-line inversion, and RTC SDA/SCL effects match VICE for supported tapeport devices.

**Traceability:** FR-TAP-001, FR-TAP-004, TEST-TAP-001.

### TR-VDC-EDGE-001: VDC Register, Display-Width, and Busy-Status Edge Cases

**ID:** TR-VDC-EDGE-001

**Phase 1:** Deferred unless C128/VDC profile work enters Phase 1.

**Behavior Summary:** VDC behavior includes observable edge cases for invalid displayed widths, semigraphics width overrides, VDC busy status timing during memory reads/writes, v0 horizontal-scroll/border quirks, register readback masks, and invalid register reads returning `0xff`.

**Affected Machine/Profile:** C128 VDC profiles only; non-Phase-1 by default.

**Source References:**

- `native/vice/vice/src/vdc/vdc-draw.c:177-252` handles displayed-width edge cases and semigraphics overrides.
- `native/vice/vice/src/vdc/vdc-mem.c:53-62` defines register readback masks.
- `native/vice/vice/src/vdc/vdc-mem.c:335-340` sets VDC busy timing for address writes.
- `native/vice/vice/src/vdc/vdc-mem.c:388-407` documents and applies v0 horizontal scroll/border behavior.
- `native/vice/vice/src/vdc/vdc-mem.c:545-568` handles memory-data reads, lightpen side effects, and invalid register readback.
- `native/vice/vice/src/vdc/vdc-mem.c:572-588` models ready and active-area status bits.

**Acceptance Expectation:** C128/VDC tests cover displayed-width invalid values, semigraphics overrides, register readback masks, busy timing, invalid register reads, and v0 border/xscroll quirks against VICE behavior.

**Traceability:** FR-PRF-004, TEST-PRF-001.

## Not Promoted in This Batch

- CIA/CIA2 source TRs were not created because the available VICE source checkout does not include the C64 CIA core files; current CIA requirements remain derived from VICE documentation until source is available.
- Platform/host-driver findings from logging, printer, RS232, sampler, generic OpenCBM, and monitor implementation code were not promoted because the reviewed matches were host plumbing or diagnostics rather than ViceSharp compatibility requirements.
- DTV, Plus/4, VIC-20, PET, SCPU64, and C128-only findings are marked deferred unless they directly affect the current C64/x64sc Phase 1 gate.
