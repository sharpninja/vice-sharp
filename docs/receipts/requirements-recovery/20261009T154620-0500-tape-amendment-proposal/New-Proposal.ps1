[CmdletBinding()]
param()

# Proposal generation only. No MCP calls, file writes, product changes or native execution.
$ErrorActionPreference = 'Stop'
$workspace = 'F:\GitHub\vice-sharp'
$baselinePath = Join-Path $workspace 'docs\receipts\requirements-recovery\20261009T140421-0500-approved-tape-ac-application\effective-requirements.after.json'
$mediaPath = Join-Path $workspace 'docs\receipts\requirements-recovery\20261009T023102-0500-legacy-media-video-ac-proposal\proposed-field-delta.json'
$baseline = Get-Content -LiteralPath $baselinePath -Raw | ConvertFrom-Json -AsHashtable
$media = Get-Content -LiteralPath $mediaPath -Raw | ConvertFrom-Json -AsHashtable
$records = [Collections.Generic.List[object]]::new()
$coverage = [Collections.Generic.List[object]]::new()

function Add-Delta {
    param([string]$Kind, [string]$Id, [string]$Title, [string]$Body, [object[]]$Criteria)
    $collection = switch ($Kind) { fr { 'functional' } tr { 'technical' } test { 'testing' } }
    $matches = @($baseline[$collection] | Where-Object { $_.id -eq $Id })
    if ($matches.Count -ne 1) { throw "Expected one retained record: $Id" }
    $before = $matches[0]
    $after = $before | ConvertTo-Json -Depth 30 | ConvertFrom-Json -AsHashtable
    $after.title = $Title
    $bodyField = if ($Kind -eq 'test') { 'condition' } else { 'body' }
    $after[$bodyField] = $Body
    $after.status = 'pending'
    $after.acceptanceCriteria = @($Criteria | ForEach-Object {
        [ordered]@{ id = $_.id; text = $_.text; isSatisfied = $false }
    })
    foreach ($criterion in $Criteria) {
        if (@($criterion.cases).Count -eq 0 -or @($criterion.sources).Count -eq 0) {
            throw "Missing proposed test/source coverage: $Id/$($criterion.id)"
        }
        $coverage.Add([ordered]@{
            requirementId = $Id; acId = $criterion.id; kind = $Kind
            namedCaseGroups = @($criterion.cases); sourceAnchors = @($criterion.sources)
            evidenceStatus = 'proposed; no mocks, real providers, or acceptance executed'
        })
    }
    $changedFields = @(@('title', $bodyField, 'status', 'acceptanceCriteria') | Where-Object {
        ($before[$_] | ConvertTo-Json -Depth 25 -Compress) -cne ($after[$_] | ConvertTo-Json -Depth 25 -Compress)
    })
    $records.Add([ordered]@{
        kind = $Kind; id = $Id; operation = 'update'; approvalStatus = 'unapplied-awaiting-exact-operator-approval'
        changedFields = $changedFields
        before = $before; after = $after
        provenance = 'Source-derived amendment; not recovered verbatim legacy wording'
    })
}

function New-TapeCriterion {
    param([string]$Id, [string]$Text, [string[]]$Cases, [string[]]$Sources)
    [ordered]@{ id = $Id; text = $Text; cases = $Cases; sources = $Sources }
}

$transport = @('TransportSense_MediaCommandMatrix')
$sense = @('MachineSense_C64CpuPortAndVic20Via1','TapeRead_PulseInterruptRouting')
$startup = @('Startup_NoPendingAlarm_DeadlineBoundaries','Startup_PendingAlarm_NotOverwritten')
$stop = @('MotorOff_NoPendingAlarm_StopsAtDeadline','MotorOff_PendingReadBeyondDeadline_DoesNotInsertSecondAlarm','MotorOff_ReadBeforeDeadline_ReschedulesPastDeadline','MotorOff_StopModeWithExistingLateAlarm','MotorOff_RepeatedDoesNotExtend','MotorOn_CancelsStopAfterDeadlineBeforeCallback','MotorOn_AfterExecutedStop_Restarts')
$wind = @('FastWind_MotorStopped_CallbackBeforeMotorOn','FastWind_MotorOnBeforePendingCallback','FastWind_ExistingReadAlarm_Replaced')
$record = @('Recording_EligibilityAndWriteEdges','Recording_DuringStartupWindow','Recording_DuringDelayedStop','Recording_AfterStopDeadlineBeforeCallback','Recording_AfterExecutedStop')
$encoding = @('RecordThenMotorOn_FirstEdge','MotorOnThenRecord_FirstEdgeFromZero','Recording_GapEncodingAndFailures','Recording_LongGap_24BitBoundary')
$output = @('MachineMotor_C64Bit5AndDdr','MachineWrite_C64Bit3AndDdrEdges','MachineMotor_Vic20PcrRawByte','MachineWrite_Vic20Pb3AndDdrEdges','MachineTape_Sx64Disconnected')
$lifecycle = @('Datasette_EnableAndFluxGating','Tapeport_DispatchAndPowerupGating','Datasette_AttachDetachResetState')
$peripheral = @('SenseDongle_EnableDisablePowerup','CpClockF83_MotorWriteSenseTruthTable')
$tap = @('TapV0_ZeroDoesNotConsumeExtension','TapV1V2_ExtensionAndTruncation','Tap_BlockDecoderVersusDatasettePath','Tap_HeaderWarningVersusRejection','Tap_ClockHeaderVersusSelectedMachine')
$threshold = @('Tap_CbmThresholdBoundaries','Tap_TurboPilotHeaderBoundaries')
$gate = @('TapeContract_RejectsMissingNativeEvidence','TapeContract_UnchangedAssertionsAcrossProviders','TapeContract_FullCumulativeGate')
$srcTransport = @('src/datasette/datasette.c:1041-1156')
$srcSense = @('src/c64/c64pla.c:51-95','src/vic20/vic20via1.c:221-260','src/c64/c64datasette.c','src/vic20/vic20datasette.c')
$srcTime = @('src/datasette/datasette.c:695-851','src/datasette/datasette.c:1000-1020','src/datasette/datasette.c:1172-1205','src/alarm.h:147-183')
$srcWind = @('src/datasette/datasette.c:925-949','src/datasette/datasette.c:1069-1090','src/datasette/datasette.c:1127-1148')
$srcRecord = @('src/datasette/datasette.c:1000-1020','src/datasette/datasette.c:1035-1038','src/datasette/datasette.c:1091-1099','src/datasette/datasette.c:1172-1321')
$srcOutput = @('src/c64/c64pla.c:77-95','src/c64/c64mem.c:218-222','src/vic20/vic20via1.c:185-203','src/vic20/vic20via2.c:110-118','src/core/viacore.c:717-724')
$srcLifecycle = @('src/datasette/datasette.c:169-174','src/datasette/datasette.c:775-778','src/datasette/datasette.c:879-998','src/tapeport/tapeport.c:245-312')
$srcPeripheral = @('src/tapeport/sense-dongle.c:68-94','src/tapeport/cp-clockf83.c:161-190')
$srcTap = @('src/tape/tap.c:90-196','src/tape/tap.c:389-444','src/datasette/datasette.c:504-535','src/datasette/datasette.c:599-691')
$srcThreshold = @('src/tape/tap.c:52-82','src/tape/tap.c:970-992','src/tape/tap.c:1243-1385','src/tape/tap.c:1673-1680','src/c64/c64.c:231-249','src/vic20/vic20.c:267-285')
$srcGate = @('operator BDPv4 correction, 2026-10-08','original handoff completion plan, Locked decisions 6/10/12 and whole-plan exit')

Add-Delta fr 'FR-RUNTIME-TAPE-002' 'Datasette motor ramp + sense line + record mode' 'Datasette transport, tape sense, motor scheduling, machine wiring and recording shall reproduce pinned immutable VICE for identical machine, board, resource, media, control and clock state. Preserve separate motor input, native running state, scheduled alarm time and actual callback time. Machine wiring belongs in machine adapters. Native quirks are target behavior; managed pulse-buffer success is not parity.' @(
    (New-TapeCriterion ac-01 'From known STOP, cover STOP, START, FORWARD, REWIND and RECORD with absent, read-only and writable images. Match accepted mode and sense. Absent-image and read-only-image RECORD do not change mode or sense; accepted writable RECORD asserts sense when the datasette is enabled.' $transport $srcTransport),
    (New-TapeCriterion ac-02 'Match C64 CPU port $0001 input bit 4 and VIC-20 VIA1 port A input bit 6, including DDR/output masking. Separately match C64 CIA1 FLAG and VIC-20 VIA2 CA1 pulse routing; sense is not an interrupt pulse.' $sense $srcSense),
    (New-TapeCriterion ac-03 'A stopped motor with no pending alarm schedules the ordinary startup callback at current clock plus 32000; retain an existing alarm as VICE does. FORWARD/REWIND commands schedule their 1000-cycle alarm independently of motor-running state, including motor-on before and after that alarm dispatch. Compare ordered scheduled and actual events, not a pulse-duration getter.' ($startup+$wind) ($srcTime+$srcWind)),
    (New-TapeCriterion ac-04 'Motor-off on a running motor sets a stop deadline of current clock plus 32000 without immediate stopping or replacing an already pending alarm. Actual stopping occurs when a datasette callback observes the deadline reached. Cover callbacks before, at and beyond the deadline, active read rescheduling, STOP with a retained later alarm, repeated off preserving the deadline, and motor-on cancellation even after the deadline but before stop execution. A stopped motor starts again through the native pending-alarm rule.' $stop $srcTime),
    (New-TapeCriterion ac-05 'Recording eligibility follows attached image, accepted RECORD mode, the native running-motor flag and qualifying zero-to-nonzero write-line transition. The running flag is set immediately on motor-on and retained until the delayed-stop callback. Cover writes during startup and pending stop, no writes after executed stop, rejected RECORD, repeated/opposite edges, and retained write-line updates even while ineligible.' $record $srcRecord),
    (New-TapeCriterion ac-06 'Match emitted TAP bytes, last-write clock/line, remainder, image position/size/dirty state, counters and failure/STOP effects for RECORD-before-motor and motor-before-RECORD, same/future-clock first edges, short gaps 0/6/7/8/2046/2047, long gaps at and above 24-bit boundaries, TAP v0/v1/v2 output paths, and first/partial/failed writes. Preserve the native first-gap and truncation behavior.' $encoding $srcRecord),
    (New-TapeCriterion ac-07 'Match machine motor/write outputs: C64 DDR-masked bit-5 active-low motor and bit-3 write; VIC-20 changed PCR raw bit-1 motor and effective VIA2 PB3 write including DDRB-induced edges. Cover SX-64 disconnection, repeated writes and input/output direction changes without substituting intended hardware behavior for pinned code.' $output $srcOutput),
    (New-TapeCriterion ac-08 'Match enabled/disabled datasette sense/flux forwarding, tapeport device dispatch, attach/detach, resource-controlled CPU reset and retained state. Assert internal state separately from UI motor display. Preserve pending alarms, gap state and sense exactly as pinned VICE, including no universal tapeport-active suppression of every signal.' $lifecycle $srcLifecycle)
)

Add-Delta tr 'TR-TAP-EDGE-001' 'TAP decoding, datasette sense and recording edge parity' 'This existing ID retains both historical obligations: TAP version/long-pulse/clock/CBM/TurboTape decoding and datasette transport/sense/recording. Keep the file/block decoder distinct from the clocked datasette read path. Use one reconciled canonical contract; do not retire either meaning or remove its FR/TEST mappings.' @(
    (New-TapeCriterion ac-01 'Apply FR-RUNTIME-TAPE-002 ac-01 media/control matrix to accepted transport state and sense. Ignored RECORD is not a RecordPressed success. Include enabled-state gating and unchanged observations.' $transport ($srcTransport+$srcLifecycle)),
    (New-TapeCriterion ac-02 'Apply FR-RUNTIME-TAPE-002 ac-02 and ac-07 to separate machine sense inputs, read-pulse interrupt inputs and motor/write outputs, including direction registers, raw VIC-20 PCR behavior and SX-64.' ($sense+$output) ($srcSense+$srcOutput)),
    (New-TapeCriterion ac-03 'Apply FR-RUNTIME-TAPE-002 ac-05 to ordered recording eligibility and write edges; assert output absence as well as writes and retained line state. Native running state is distinct from control input and completed startup delay.' $record $srcRecord),
    (New-TapeCriterion ac-04 'Apply FR-RUNTIME-TAPE-002 ac-06 to exact recording bytes and side effects, including both RECORD/motor orderings, first-edge clock handling, 24-bit truncation and failed/partial writes.' $encoding $srcRecord),
    (New-TapeCriterion ac-05 'Retain legacy TAP v0/v1/v2 decoding, long-pulse, header and clock-selection coverage. v0 zero consumes no extension. The file/block decoder uses its native pulse units and v2 halfwave combination; the live datasette path uses its own zero-gap resources, direction, machine-specific halfwave handling, wobble and azimuth state. Cover truncated extensions and warning-versus-rejection outcomes without merging these paths.' $tap $srcTap),
    (New-TapeCriterion ac-06 'Retain legacy CBM and TurboTape pilot/header detection with boundary fixtures against pinned tap.c. C64 and VIC-20 configured CBM pulse-unit ranges are inclusive 36-54, 55-73 and 74-100 after tap_init; TurboTape uses its own ranges and pilot/header control flow. Test each endpoint, adjacent outside value, missing/truncated pilot/header and malformed stream; do not replace loader behavior with a count of parsed pulses.' $threshold $srcThreshold)
)

Add-Delta tr 'TR-TAPE-EDGE-001' 'Datasette timing and tapeport peripheral line parity' 'This existing ID retains both historical obligations: datasette startup/delayed-stop/fast-wind timing and tapeport sense/motor/RTC/dongle semantics. Model scheduling in the emulated clock domain and preserve the selected native device behavior. A datasette is not the only possible source of tape sense.' @(
    (New-TapeCriterion ac-01 'Assert stopped-motor ordinary startup at t+32000 with no pending alarm and preservation of an existing pending alarm. Observe callback dispatch separately from scheduled time; no first-Tick bypass.' $startup $srcTime),
    (New-TapeCriterion ac-02 'Assert motor-off sets t+32000 stop deadline, retains a pending alarm and running state until a callback executes at or after that deadline, and repeated motor-off does not extend it. Include no-pending exact-deadline stop, retained late read/STOP alarms and a before-deadline callback rescheduling beyond the deadline.' $stop $srcTime),
    (New-TapeCriterion ac-03 'Assert motor-on cancels a stop not yet executed, including after the nominal deadline but before a late callback, without imposing a new startup delay on an already-running motor. After an executed stop, obey the ordinary start helper and its pending-alarm preservation.' $stop $srcTime),
    (New-TapeCriterion ac-04 'Compare complete ordered control, alarm, running-state and pulse traces at deadline-1/deadline/deadline+1 and at actual callback boundaries. Pin same-clock ordering, callback offset, tape resources and effective gap; reject missing, extra, duplicate, reordered or synthesized events.' ($startup+$stop) $srcTime),
    (New-TapeCriterion ac-05 'FORWARD and REWIND schedule a 1000-cycle alarm after the mode assignment, with absent or attached media and motor on or off. Cover replacement of a pending read alarm and motor-on before/after the fast callback; preserve the executed native behavior rather than the helper comment intent.' $wind $srcWind),
    (New-TapeCriterion ac-06 'Retain legacy sense-dongle and CP Clock F83 semantics. Dongle enable and power-up assert sense, while its disable callback does not itself clear sense. RTC motor input drives inverted SDA, write input drives normalized SCL, and sense is asserted only when RTC data is high and motor-state is false. Cover repeated calls, all truth-table inputs and selected-device lifecycle without synthesizing datasette PLAY.' $peripheral $srcPeripheral),
    (New-TapeCriterion ac-07 'Apply FR-RUNTIME-TAPE-002 ac-08 to per-call-site device enable gating, tapeport dispatch, attachment and reset. Assert internal motor state independently of the UI reset indication and pin reset resources.' $lifecycle $srcLifecycle)
)

Add-Delta test 'TEST-TAPE-RAMP-001' 'Datasette startup, delayed-stop and fast-wind parity' 'Validate FR-RUNTIME-TAPE-002 ac-03/ac-04 and TR-TAPE-EDGE-001 ac-01 through ac-05 using shared clock/control/alarm cases. Green correct-mock and expected negative-control evidence precedes production changes. Reuse the same cases and assertions with real managed/native observations; no fixed historical passing total is an acceptance condition.' @(
    (New-TapeCriterion ac-01 'Startup_NoPendingAlarm_DeadlineBoundaries and Startup_PendingAlarm_NotOverwritten assert ordered schedules, callback times and events; incorrect early/late/missing/duplicated/reordered events are rejected within a passing mock harness.' $startup $srcTime),
    (New-TapeCriterion ac-02 'Run all named delayed-stop and cancellation cases in the amendment ledger, retaining deadline and callback boundaries separately. Reject immediate stop, guaranteed-deadline stop despite an existing late alarm, repeated-off deadline extension and unconditional off/on restart.' $stop $srcTime),
    (New-TapeCriterion ac-03 'Real-provider validation reuses every mock input/assertion and obtains actual native and managed ordered observations. Source inspection, an analytical trace or a duration getter cannot substitute for actual pulse/state evidence. Missing capability fails closed.' $gate $srcGate),
    (New-TapeCriterion ac-04 'Run every case without skips in full applicable cumulative native-safe and real-emulator suites after native-safe integration. Retain exact commands, counts, TRX/logs, identities, failure history and accepted hostile review before satisfaction.' $gate $srcGate),
    (New-TapeCriterion ac-05 'Run each named fast-wind case for both FORWARD and REWIND, attached/absent media and stopped/running motor. Assert 1000-cycle command alarms, preservation by motor-on when pending and 32000 startup only when the ordinary helper has no pending alarm; reject unconditional startup timing.' $wind $srcWind)
)

Add-Delta test 'TEST-TAPE-SENSE-001' 'Datasette transport, machine wiring, recording and lifecycle parity' 'Validate FR-RUNTIME-TAPE-002 ac-01/ac-02/ac-05 through ac-08 and the corresponding TR-TAP-EDGE-001 and TR-TAPE-EDGE-001 criteria. Establish independent correct and discriminating wrong mocks, then bind unchanged inputs/assertions to real providers. Preserve historical evidence without treating old counts or wrong expectations as acceptance.' @(
    (New-TapeCriterion ac-01 'TransportSense_MediaCommandMatrix covers five commands from STOP, absent/read-only/writable images, enabled-state sense gating and later STOP. Reject wrong polarity, ignored-command state changes and missing transitions.' $transport ($srcTransport+$srcLifecycle)),
    (New-TapeCriterion ac-02 'MachineSense_C64CpuPortAndVic20Via1 covers both machines and DDR/output masking; TapeRead_PulseInterruptRouting separately covers CIA1 FLAG and VIA2 CA1. Reject wrong port/bit/direction/interrupt target and extra or missing pulse events.' $sense $srcSense),
    (New-TapeCriterion ac-03 'Run all recording eligibility/window/edge cases in the ledger, including startup, pending stop, after nominal deadline before callback and after executed stop. Assert writes/no-writes and retained line state. Reject no-image recording and invented startup inhibits.' $record $srcRecord),
    (New-TapeCriterion ac-04 'Run both RECORD/motor orderings and gap/error cases including 0/6/7/8/2046/2047, remainder carry, same/future-clock edges, v0/v1/v2, 0xffffff/0x1000000/0x1000001 and zero/one/two/three-byte long-payload writes. Assert bytes, image position/size/dirty state, counters, retained state and STOP side effects against independent native expectations.' $encoding $srcRecord),
    (New-TapeCriterion ac-05 'Reuse all fixtures/assertions with actual immutable native and managed providers, then run full cumulative suites. Missing, failed, skipped, aborted or mock-only cases cannot satisfy AC; require independent AGREE, both scores at least 98 and zero FAIL/UNKNOWN.' $gate $srcGate),
    (New-TapeCriterion ac-06 'Run machine motor/write cases for C64 bit 5/bit 3, input/output DDR and DDR-only changes, VIC-20 PCR raw-byte motor and VIA2 PB3/DDRB transitions, repeated writes and SX-64. Wrong bit, normalized-but-nonnative PCR behavior, omitted DDR edge and connected SX-64 mocks must be rejected.' $output $srcOutput),
    (New-TapeCriterion ac-07 'Run datasette and tapeport enable/dispatch/attach/detach/reset cases with each reset-resource branch. Reject UI-only internal-state assertions, synthesized motor clears and universal tapeport-active suppression.' $lifecycle $srcLifecycle),
    (New-TapeCriterion ac-08 'Run sense-dongle lifecycle and CP Clock F83 truth-table cases. Reject datasette-only sense, forced deassertion by the dongle disable callback, wrong SDA inversion, wrong SCL normalization and incorrect RTC sense combinations.' $peripheral $srcPeripheral)
)

Add-Delta fr 'FR-TAP-001' 'Datasette Motor Control' 'Motor input, native running state, transport motion, public state exposure and tape audio shall follow pinned immutable VICE for the selected machine and resources. C64 motor control is DDR-masked CPU-port bit 5, not bit 3. Ordinary startup scheduling and actual delayed-stop callback execution are distinct from physical-drive approximations. Preserve the existing public MotorState obligation and tape-audio functionality.' @(
    (New-TapeCriterion ac-01 'C64 CPU-port bit 5 controls active-low motor output as !((DDR & DATA) & 0x20); input direction drives motor-on. Bit 3 is tape write, not motor. VIC-20 and SX-64 behavior follows FR-RUNTIME-TAPE-002 ac-07.' $output $srcOutput),
    (New-TapeCriterion ac-02 'Use the native emulated-clock schedule: ordinary start adds 32000 only if no alarm is pending; FORWARD/REWIND uses its 1000-cycle command alarm. Do not substitute an approximate real-hardware half-second timer.' ($startup+$wind) ($srcTime+$srcWind)),
    (New-TapeCriterion ac-03 'Tape motion, read-pulse delivery and stopping match the ordered native transport/alarm trace, including late stop execution and pending-alarm cases. Do not conflate immediate native running-state assertion with first movement or apply a universal spin-up-complete gate to every operation.' ($startup+$stop+$wind) ($srcTime+$srcWind)),
    (New-TapeCriterion ac-04 'PLAY/sense is not a prerequisite for asserting the native motor-running flag. Cover motor-on in STOP and RECORD, and distinguish transport mode/sense from motor control and running state.' ($transport+$record+$output) ($srcTransport+$srcRecord+$srcOutput)),
    (New-TapeCriterion ac-05 'Motor state changes are reflected in the `ITapeUnit.MotorState` property.' @('TapePublicApi_MotorState') @('retained FR-TAP-001 ac-05, unchanged public-interface obligation')),
    (New-TapeCriterion ac-06 'Tape audio remains required. With identical sound-enabled/volume/sample-format/rate and tape halfwave/gap inputs, match pinned VICE datasette sound buffer and output samples, including disabled flush, buffer wrap, empty output and gap boundaries. Native datasette sound-chip output, not an assumed SID EXT IN route, is the acceptance target.' @('DatasetteAudio_ResourceBufferAndSamples') @('src/datasette/datasette-sound.c:34-266','src/datasette/datasette.c:839-841'))
)

Add-Delta fr 'FR-TAP-002' 'TAP File Format Support (v0, v1 and v2 native behavior)' 'Read/write TAP behavior shall match pinned VICE at the corresponding file/block decoder and live datasette layer. v0 zero does not consume a three-byte extension. v1/v2 extension, halfwave, header, clock and resource behavior must not be merged into a different invented format. Retain public mount/eject and machine-tag obligations.' @(
    (New-TapeCriterion ac-01 'For nonzero TAP bytes, preserve native pulse units and layer-specific conversion: file/block decoding uses pulse units while live datasette gaps use byte times eight before resource adjustments. Do not globally label every v0 byte a half-period.' $tap $srcTap),
    (New-TapeCriterion ac-02 'A v0 zero consumes only that byte. The file/block decoder returns 256 pulse units; the live datasette uses DatasetteZeroGapDelay and its adjustments. Adjacent bytes remain separate pulses, not a 24-bit extension.' @('TapV0_ZeroDoesNotConsumeExtension','Tap_BlockDecoderVersusDatasettePath') $srcTap),
    (New-TapeCriterion ac-03 'For v1/v2 zero, cover three-byte little-endian extensions, truncated extension and all-zero length at each native layer. File/block v2 combines two halfwaves; live behavior depends on selected machine tape behavior and retains its native direction/resource effects.' @('TapV1V2_ExtensionAndTruncation','Tap_BlockDecoderVersusDatasettePath') $srcTap),
    (New-TapeCriterion ac-04 'Header handling matches native short-read/magic rejection, stored version/system/video values, mismatch warning versus error behavior, declared data handling and clock fallback. Do not reject a mismatch that the corresponding native path only warns about.' @('Tap_HeaderWarningVersusRejection','Tap_ClockHeaderVersusSelectedMachine') $srcTap),
    (New-TapeCriterion ac-05 'Tape images can be mounted and unmounted at runtime via `ITapeUnit.Mount()`/`ITapeUnit.Eject()`.' @('TapePublicApi_MountEject') @('retained FR-TAP-002 ac-05, unchanged public-interface obligation')),
    (New-TapeCriterion ac-06 'Writing to TAP preserves native recording eligibility, clocks, byte encoding, first-gap and failure effects from FR-RUNTIME-TAPE-002 ac-05/ac-06, including v0/v1/v2 output behavior and 24-bit truncation.' ($record+$encoding) $srcRecord),
    (New-TapeCriterion ac-07 'Header machine tags distinguish C64, VIC-20 and C16/Plus4 and use the native machine/video clock table. Tests pin selected machine separately from the media header and preserve native mismatch/fallback behavior.' @('Tap_ClockHeaderVersusSelectedMachine') $srcTap),
    (New-TapeCriterion ac-08 'Cover mount/eject, forward/reverse gap access and truncated media without treating file metadata decoding as a live pulse trace. Both layers require independent expected observations; missing native live evidence fails closed.' ($tap+$lifecycle+$gate) ($srcTap+$srcLifecycle+$srcGate))
)

Add-Delta fr 'FR-TAP-003' 'Cycle-Accurate Tape Read Timing' 'Tape read pulse delivery, timer/interrupt effects and data decoding shall match immutable VICE for the selected C64 or VIC-20, clock/resources, firmware and workload. File/block decoder thresholds and real machine signal timing are different observable layers; neither can substitute for the other.' @(
    (New-TapeCriterion ac-01 'C64 read pulses reach CIA1 FLAG and VIC-20 read pulses reach VIA2 CA1 with the pinned native edge/state semantics. Tape sense remains a distinct input.' $sense $srcSense),
    (New-TapeCriterion ac-02 'With identical pinned firmware and tape workload, timer/interrupt observations used to measure pulses match native VICE, including boundary and pending-interrupt state. A decoder return value is not timer/interrupt evidence.' @('TapeRead_TimerInterruptTrace') ($srcSense+$srcGate)),
    (New-TapeCriterion ac-03 'Configured C64/VIC-20 file-decoder CBM pulse-unit ranges are inclusive 36-54 short, 55-73 middle and 74-100 long. Test endpoint and adjacent values after tap_init conversion. Keep live pulse clocks separate; do not pair inconsistent fixed cycle counts with TAP byte values.' @('Tap_CbmThresholdBoundaries') $srcThreshold),
    (New-TapeCriterion ac-04 'Pinned standard Commodore header, data-block and checksum workloads produce the same decoded bytes/status and machine-visible load outcome as native VICE. Include malformed, truncated and checksum-error fixtures.' @('TapeRead_CbmHeaderDataChecksum') @('src/tape/tap.c:535-955','full original plan real oracle/workload prerequisites')),
    (New-TapeCriterion ac-05 'At normal speed, the same named tape workload produces the same ordered native and managed machine-visible pulse/state/output observations. Hardware assumptions or source inspection alone cannot replace the immutable native target.' @('TapeRead_NormalSpeedMachineTrace') ($srcTime+$srcGate)),
    (New-TapeCriterion ac-06 'Run PAL and NTSC selected-machine/resource profiles with separately pinned tape headers and clock domains. Preserve native differences and any lack of cross-standard rescaling; do not improve the native target.' @('Tap_ClockHeaderVersusSelectedMachine','TapeRead_NormalSpeedMachineTrace') @('src/tape/tap.c:90-196','src/datasette/datasette.c:710-719'))
)

Add-Delta fr 'FR-TAP-004' 'Tape Write Support' 'Write data to tape images using the selected machine native write-line path and accepted datasette RECORD state. C64 uses CPU-port bit 3; VIC-20 uses effective VIA2 PB3. Preserve native timing, image position, existing content, first-write quirks and write failures, not an in-memory pulse-count substitute.' @(
    (New-TapeCriterion ac-01 'Capture qualifying native write-line transitions from C64 CPU-port bit 3 and VIC-20 VIA2 PB3, including DDR-only changes, repeated/opposite edges and SX-64 disconnection.' $output $srcOutput),
    (New-TapeCriterion ac-02 'Store the same bytes, gap/remainder, last-write state, file position/size/dirty state and counters as pinned VICE across short/long/24-bit boundaries and partial/failed output.' $encoding $srcRecord),
    (New-TapeCriterion ac-03 'A standard Commodore SAVE workload with pinned firmware, writable tape, resources and initial state produces the same bytes/status and machine-visible effects as immutable VICE, and its corresponding native/managed load results match. Include failure and write-protected cases.' @('TapeWrite_KernalSaveAndReload') ($srcRecord+$srcGate)),
    (New-TapeCriterion ac-04 'New-image, existing-image current-position and explicit end-position recordings preserve the same prefix/suffix and file-position/size effects as native VICE. Existing-file writes must not silently become unconditional append or truncate.' @('TapeWrite_NewExistingAndAppendPosition') @('src/datasette/datasette.c:1000-1005','src/datasette/datasette.c:1210-1291')),
    (New-TapeCriterion ac-05 'Writable-image RECORD acceptance is the native interlock; absent/read-only-image RECORD is ignored. Do not require an additional independent PlayPressed boolean once native RECORD mode is accepted. Motor-running and qualifying-edge conditions still apply.' ($transport+$record) ($srcTransport+$srcRecord))
)

Add-Delta test 'TEST-TAP-001' 'Tape format, machine I/O, peripherals, audio and loader contracts' 'Retain all existing datasette motor, TAP parsing, pulse timing, tape write and turbo-loader coverage, and explicitly map both historical meanings of TR-TAP-EDGE-001/TR-TAPE-EDGE-001. Unit assertions first discriminate independent correct/wrong mocks in a green suite, then remain unchanged for real managed/native providers and named workload gates.' @(
    (New-TapeCriterion ac-01 'Cover FR-TAP-001 motor control/timing and FR-RUNTIME-TAPE-002 command/sense/motor/write behavior, including pending-alarm stop, fast wind, DDR-only transitions, both RECORD/motor orders, failures and lifecycle. Reuse TEST-TAPE-RAMP-001 and TEST-TAPE-SENSE-001 cases without omitting them from cumulative gates.' ($transport+$sense+$startup+$stop+$wind+$record+$encoding+$output+$lifecycle) ($srcTransport+$srcTime+$srcRecord+$srcOutput+$srcLifecycle)),
    (New-TapeCriterion ac-02 'Cover all FR-TAP-002 and TR-TAP-EDGE-001 decoder/header/clock/version scenarios, with separate block-decoder and live-datasette assertions. Reject v0 extension consumption, universal halfwave conversion, incorrect truncation, mismatch rejection and clock-domain conflation.' $tap $srcTap),
    (New-TapeCriterion ac-03 'Cover FR-TAP-003 and FR-TAP-005 pulse boundaries, CBM and TurboTape pilot/header paths, standard load/checksum outcomes and every already-required named turbo format (Novaload, Biturbo, Turbotape, Cyberload, Pavloda), half/full-wave and changing-speed workloads. Pin actual fixtures and firmware; missing fixture, oracle or observation is a failure, never an invented successful loader result.' ($threshold+@('TapeRead_TimerInterruptTrace','TapeRead_CbmHeaderDataChecksum','TapeRead_NormalSpeedMachineTrace','TapeRead_TurboNamedLoaderMatrix')) ($srcThreshold+$srcSense+$srcGate)),
    (New-TapeCriterion ac-04 'Cover retained public motor-state/mount/eject obligations, native tape-audio behavior and machine-visible SAVE/reload/new-existing-append results. Wrong API state, audio samples, persisted bytes, position or failure effects must be rejected by independent negative controls.' @('TapePublicApi_MotorState','TapePublicApi_MountEject','DatasetteAudio_ResourceBufferAndSamples','TapeWrite_KernalSaveAndReload','TapeWrite_NewExistingAndAppendPosition') @('FR-TAP-001 ac-05/ac-06','FR-TAP-002 ac-05','FR-TAP-004 ac-01 through ac-05')),
    (New-TapeCriterion ac-05 'Cover sense dongle enable/disable/power-up and CP Clock F83 motor/SDA/write/SCL/sense cases from TR-TAPE-EDGE-001 ac-06, with device selection and no-datasette cases.' $peripheral $srcPeripheral),
    (New-TapeCriterion ac-06 'For every AC-mapped case, valid independent scripted mock responses pass all assertions. Deliberately wrong value, missing/extra/duplicate/reordered observation, wrong clock/side effect and failure-versus-success response must be rejected as an expected negative control within a passing native-safe suite. No failing suite or production-derived expected value qualifies.' $gate $srcGate),
    (New-TapeCriterion ac-07 'Bind the same inputs/assertions to real managed/native providers, retain exact identities and first divergence, run full applicable current-plus-prior suites with zero failed/skipped/aborted cases, and obtain AGREE at accuracy/completeness at least 98 with zero FAIL/UNKNOWN before satisfaction. Preserve all original xvic/x64sc/VSF and later start-approval gates.' $gate $srcGate)
)

# Reuse exactly the existing parent-media proposal row; this does not approve its other records.
$parent = @($media.records | Where-Object { $_.id -eq 'FR-BACKFILL-MEDIA-001' })
if ($parent.Count -ne 1) { throw 'Expected one existing parent-media proposal row' }
$liveParent = @($baseline.functional | Where-Object { $_.id -eq 'FR-BACKFILL-MEDIA-001' })[0]
foreach ($field in @('id','title','body','status','priority','scopeStartLayerKey')) {
    if ($liveParent[$field] -cne $parent[0].before[$field]) { throw "Parent proposal baseline drift: $field" }
}
$records.Add($parent[0])
foreach ($c in $parent[0].after.acceptanceCriteria) {
    $coverage.Add([ordered]@{
        requirementId='FR-BACKFILL-MEDIA-001'; acId=$c.id; kind='fr'
        namedCaseGroups=@('ExistingMediaProposal_'+$c.id)
        sourceAnchors=@('unchanged parent row and proposed-ac-coverage.json in 20261009T023102-0500-legacy-media-video-ac-proposal')
        evidenceStatus='reused unapproved proposal; drive/D64/IEC dependencies remain open'
    })
}

$mappingIds = @('FR-RUNTIME-TAPE-002','FR-BACKFILL-MEDIA-001','FR-TAP-001','FR-TAP-002','FR-TAP-003','FR-TAP-004','FR-TAP-005')
$mappingBefore = @($baseline.mappings | Where-Object { $_.frId -in $mappingIds })
$mappingAfter = $mappingBefore | ConvertTo-Json -Depth 20 | ConvertFrom-Json -AsHashtable
foreach ($mapping in $mappingAfter) {
    $addedTr = switch ($mapping.frId) {
        'FR-TAP-001' { @('TR-TAPE-EDGE-001','TR-TAP-EDGE-001') }
        'FR-TAP-002' { @('TR-TAP-EDGE-001') }
        'FR-TAP-003' { @('TR-TAP-EDGE-001') }
        'FR-TAP-004' { @('TR-TAP-EDGE-001','TR-TAPE-EDGE-001') }
        'FR-TAP-005' { @('TR-TAP-EDGE-001') }
        default { @() }
    }
    $mapping.trIds = @(@($mapping.trIds) + @($addedTr) | Select-Object -Unique)
    $addedTests = if ($mapping.frId -in @('FR-TAP-001','FR-TAP-004')) { @('TEST-TAPE-RAMP-001','TEST-TAPE-SENSE-001') } else { @() }
    $mapping.testIds = @(@($mapping.testIds) + @($addedTests) | Select-Object -Unique)
}

# Preserve the complete existing media-parent mapping proposal, including its drive/D64/IEC links.
$existingParentMapping = @($media.mappingsAfter | Where-Object { $_.frId -eq 'FR-BACKFILL-MEDIA-001' })
if ($existingParentMapping.Count -ne 1) { throw 'Existing parent mapping proposal missing' }
$parentMapping = @($mappingAfter | Where-Object { $_.frId -eq 'FR-BACKFILL-MEDIA-001' })[0]
$parentMapping.trIds = @(@($parentMapping.trIds) + @($existingParentMapping[0].trIds) | Select-Object -Unique)
$parentMapping.testIds = @(@($parentMapping.testIds) + @($existingParentMapping[0].testIds) | Select-Object -Unique)

foreach ($delta in $records) {
    if ($delta.before.id -ne $delta.after.id) { throw 'ID changed' }
    if ($delta.after.status -ne 'pending') { throw 'Nonpending proposal' }
    $criteria = @($delta.after.acceptanceCriteria)
    if ($criteria.Count -eq 0 -or @($criteria | Where-Object isSatisfied).Count -gt 0) { throw 'Invalid pending criteria' }
    if (@($criteria.id | Select-Object -Unique).Count -ne $criteria.Count) { throw 'Duplicate AC ID' }
    foreach ($oldAc in $delta.before.acceptanceCriteria) {
        if ($oldAc.id -notin $criteria.id) { throw "AC identity removed: $($delta.id)/$($oldAc.id)" }
    }
    foreach ($field in @('priority','scopeStartLayerKey','scopeEndLayerKey','workspaceId')) {
        if ($delta.before[$field] -cne $delta.after[$field]) { throw "Preserved field changed: $($delta.id)/$field" }
    }
}
foreach ($oldMap in $mappingBefore) {
    $newMap = @($mappingAfter | Where-Object { $_.frId -eq $oldMap.frId })[0]
    foreach ($id in $oldMap.trIds) { if ($id -notin $newMap.trIds) { throw "TR mapping removed: $id" } }
    foreach ($id in $oldMap.testIds) { if ($id -notin $newMap.testIds) { throw "TEST mapping removed: $id" } }
}
$newCount = ($records | ForEach-Object { @($_.after.acceptanceCriteria).Count } | Measure-Object -Sum).Sum
if ($coverage.Count -ne $newCount) { throw 'AC coverage ledger count mismatch' }

[ordered]@{
    schema='vice-sharp/exact-tape-amendment-proposal/v1'
    status='unapplied; exact-delta approval and independent HV required'
    createdAtLocal=[TimeZoneInfo]::ConvertTimeBySystemTimeZoneId([DateTimeOffset]::UtcNow,'America/Chicago').ToString('yyyy-MM-ddTHH:mm:sszzz')
    workspace=$workspace
    nativeCommit='cc67418cba58e71d8e70fd353e9de47199ad2075'
    baseline=[ordered]@{path=$baselinePath;sha256=(Get-FileHash -LiteralPath $baselinePath -Algorithm SHA256).Hash;freshServerReadback=$false;reason='marker missing after supported hook health failure'}
    reusedParentProposal=[ordered]@{path=$mediaPath;sha256=(Get-FileHash -LiteralPath $mediaPath -Algorithm SHA256).Hash;recordId='FR-BACKFILL-MEDIA-001';otherRecordsIncluded=$false}
    recordCount=$records.Count
    beforeCriteriaCount=($records | ForEach-Object { @($_.before.acceptanceCriteria).Count } | Measure-Object -Sum).Sum
    afterCriteriaCount=$newCount
    records=@($records.ToArray())
    mappingsBefore=$mappingBefore
    mappingsAfter=$mappingAfter
    coverage=@($coverage.ToArray())
    invariants=@('No requirement or AC identity removed','All after statuses pending and all AC unsatisfied','All previous mapping edges retained','Priority, scope and workspace preserved in effective projections','Full-field metadata preservation unproven until fresh getFr/listTr/listTest preimages are obtained','No MCP application, product/native edits, test execution or gate advancement')
}
