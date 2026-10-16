# Legacy 1541 requirement source audit

Recorded 2026-10-08T15:29:01-05:00 by Codex. Status: source audit, unreviewed. No requirement changes or implementation acceptance.

## Result

The closed `FR-ARCH-TRUEDRIVE-1541-002` and its four mapped TR/TEST records do not establish true-drive parity with pinned VICE. The named tests check simplified helper behavior, not the separate managed C1541 rig or native drive execution. Seven inspected native files match their Git blobs at `cc67418cba58e71d8e70fd353e9de47199ad2075` exactly. This is source evidence, not a native/managed runtime comparison.

The supported MCP effective-requirements readback retained in `effective-five-records.json` contains:

- `FR-ARCH-TRUEDRIVE-1541-002`
- `TR-DRV-EDGE-001`
- `TR-IEC-EDGE-001`
- `TEST-DRV-MOTOR-001`
- `TEST-IEC-TIMING-001`

All five have status `closed` and empty structured acceptance-criteria arrays. Their prose does contain expectations. Empty arrays alone do not prove the absence of every historical acceptance criterion. The preceding tape/provenance audit separately retained the pre-rename FR snapshot and inferred September recovery proposals; those historical artifacts remain unchanged.

## Findings

### D01: An arbitrary helper deadline is not the native IEC contract

`IecBus.cs:55-76` sets both CLK and DATA low on an ATN falling edge and high on its rising edge. `IecTimingTests.cs` constructs this helper directly and accepts a transition anywhere within 985 calls. It does not execute a drive CPU, observe VIA state, compare native traces, or validate independent line holders.

In pinned `vice/src/iecbus/iecbus.c:237-286`, the C64-oriented configured-drive path first executes the drive CPU to the supplied clock, updates host contributions, signals the drive VIA on the ATN transition, then composes drive outputs and resolves ports. `vice/src/c64/c64iec.c:121-135` combines host and drive contributions. An unconditional release of both lines based solely on ATN is not that contract.

The VIC-20 path is distinct: `vice/src/vic20/vic20iec.c:58-72` resolves host and enabled-drive contributions; lines 95-118 calculate DATA/ATNA contributions; lines 143-212 synchronize drive CPUs and handle VIA1-port ATN access. Its generic `iec_update_cpu_bus` is empty, so the C64 function alone must not be used as xvic coverage.

Consequence: the existing helper tests cannot serve as evidence that the actual managed C64 or VIC-20 drive bus matches VICE. This finding does not assert that the running C1541 bus necessarily has the helper's behavior.

### D02: The fixed 300,000-cycle ramp is not the inspected VICE motor path

`IecDrive.cs:39-45,91-126` deliberately holds its rotation counter at zero for the first 300,000 ticks. The legacy FR and TR describe that hold as native behavior.

Pinned `vice/src/drive/iecieee/via2d.c:324-335` updates `BRA_MOTOR_ON` and calls `rotation_begins` on the motor-on transition. `vice/src/drive/rotation.c:295-305` sets the rotation clock origin to the current drive clock. Lines 1106-1124 gate rotation on the motor bit and choose a rotation implementation. In the simple rotation path, lines 1002-1018 accumulate elapsed drive cycles immediately; there is no fixed 300,000-cycle startup hold in this inspected path.

Native attachment/read delays are separate: `rotation.c:1145-1164` checks attachment clocks before a byte read, and `drive.h:190` defines `DRIVE_ATTACH_DELAY` as `3 * 600000`. Do not replace one invented delay with an unrelated native delay. No claim about every possible VICE configuration or physical-drive mechanics is made here.

Consequence: derive motor/rotation acceptance from the pinned native state transitions and resource settings, not from the helper's constant or a generic physical spin-up estimate.

### D03: The test requirement and test disagree at the exact boundary

`TEST-DRV-MOTOR-001` says the rotation counter is positive after 300,000 calls. `IecDriveMotorRampTests.cs:98-117` checks 300,001 calls. Inspection of `IecDrive.Tick` shows the 300,000th call only increments the ramp counter; the next call increments rotation.

This is a concrete requirement/test mismatch, not a reason to change the native parity requirement to 300,001. Correcting that single number would leave D02 unresolved. No tests were executed during this audit.

### D04: Image-byte access and true-drive execution are conflated

`IecDrive.cs:50-55` identifies its disk image as input to the KERNAL serial trap when True Drive is OFF. `ReadSector` at lines 223-227 directly reads the image into a buffer without a motor/ramp check. The BAM test at `IecDriveMotorRampTests.cs:154-175` constructs this helper with a fixture image and reads the sector without starting the motor. This checks image bytes, not a DOS/CPU/VIA/GCR timed read.

The separate true-drive construction is visible in `ArchitectureBuilder.cs:433-512`, `C64TrueDriveRigBuilder.cs:44-72`, and `MultiSystemBlueprint.cs:142-154`. It connects `C1541IecInterfaceDevice` and `C1541DriveMechanismDevice` to drive VIAs. The interface has DATA/ATNA composition at lines 92-97; the mechanism's tick path is at lines 113-135. Neither legacy test class instantiates that rig. This audit does not claim those actual devices pass parity.

## Required follow-through, not yet applied

Preserve all five IDs, existing mappings, historical bodies, and test evidence. Do not retire the FR or silently remove tests. Prepare a separate exact field delta for approval that distinguishes simulated image access from true-drive parity and replaces unsupported native-attribution claims with source-derived, unsatisfied AC.

The acceptance design must cover host/drive line contention, ATN/ATNA combinations and edges, enabled/disabled drive sets, machine-specific C64/VIC-20 wiring, drive-clock synchronization, motor on/off transitions, zone/resource settings, attachment/read delays, and actual DOS-mediated sector access. Expected outputs must come from pinned VICE, not current managed output. Match traces at explicit clock/phase boundaries, not merely within a loose deadline.

Under BDPv4, correct and single-fault incorrect mocks must first prove every new assertion in a green native-safe suite. Then bind those same fixtures and assertions to approved real adapters and retain all mandatory real-emulator, VSF, and cumulative gates. This audit does not authorize an implementation phase or bypass the pending Opus review.

## Receipts and limits

- `native-source-pins.json`: seven native files, expected/actual Git blob identities and SHA-256.
- `managed-input-pins.json`: nine inspected managed/test inputs, SHA-256.
- `source-excerpts.json`: numbered source ranges supporting the findings.
- `effective-requirements.json` and `effective-five-records.json`: supported MCP readback, not edited requirement storage.
- `verification.json`: post-audit integrity, requirement-content comparison, and diff-check result.
- `session-turn.completed-primary-readback.json`: populated only after actual primary MCP readback.

Only new audit receipts are written in this slice. No production/test source, native checkout, canonical requirement, mapping, TODO, commit, or push is changed. No build or tests run. No HV verdict or scores are available for this audit; it remains unaccepted.
