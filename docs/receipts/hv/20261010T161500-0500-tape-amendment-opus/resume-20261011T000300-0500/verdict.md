# Hostile validation verdict: exact unapplied eleven-record tape requirement amendment

Reviewer: ClaudeCode, model claude-opus-5-5 (Claude Opus 5.5), effort max, local Claude CLI session 09cb215d-0e89-4272-aa11-a66514b50ea0 on PAYTON-LEGION2, own PowerShell.MCP sub-agent console sa-5bb1338b (PID 87788). Independent reviewer; no nested reviewer, substitute model, cloud task, web tool, turn cap or time cap.

Timestamp: 2026-10-11T00:43:55-05:00 (2026-10-11T05:43:55Z).

Logical review: the same eleven-record tape amendment review (midnight retry after two HTTP 429 attempts at 2026-10-10T16:17 and 19:58). Not a new reviewer slot and not a replacement of the completed five-record review.

Parent: Codex, Codex-20261007T135626Z-plugin-session / req-20261011T050131Z-midnight-opus-retry (earlier linkage req-20261010T211205Z-tape-amendment-hv and req-20261011T005547Z-resume-opus-after-reset).

Reviewer MCP session: ClaudeCode-20261011T050449Z-plugin-session / req-20261011T050452Z-prompt-ba39.

OverallVerdict: DISAGREE. PASS 35, FAIL 6, UNKNOWN 0, N/A 1 (42 assertions). Accuracy 93. Completeness 84.

The packet is structurally exact and its source-derived corrections are accurate against immutable VICE: every hash, count, changed field, retained criterion identity, baseline value and selected native file I rechecked is correct, and every numeric or branch claim I checked in the amended text matches the pinned source. DISAGREE comes from (1) two of the fifteen additive mapping links pointing at records that do not exist in the live store, (2) other pending requirements (FR-CPU-004, FR-PRF-003) that contradict the corrected wiring and would be left unreconciled, (3) a missing start-helper entry point and a dropped cancellation case that leave planned tests non-discriminating, (4) R0/R1 safeguards that do not account for read-synthesized requirement timestamps or carry value-bearing comparisons, and (5) a stale root handoff and TODO tracking. This verdict does not approve or apply the amendment, does not accept the earlier five-record application, satisfies no acceptance criterion, and authorizes no TODO, requirement, phase, plan or goal done-state change. The full original goal remains incomplete.

## Classification and boundaries

Class: project requirement work, requirements phase only. The review covers approval-readiness and semantic adequacy of the exact UNAPPLIED delta in docs/receipts/requirements-recovery/20261009T154620-0500-tape-amendment-proposal before the operator's separate approval. Surfaces A, B, C and D all apply.

The review was read-only except: this directory's reviewer-evidence.json, verdict.json, verdict.md and verdict.persistence.json; updates to my own session turn; one scratch file created and deleted in %TEMP%; and PowerShell.MCP's own temporary output spill files. No requirement, mapping, TODO, plan, handoff, product, test, native or canonical-document mutation. No builds, tests, Python, web access, queue drain or replay. Native source was read from artifacts/immutable-vice/cc67418cba58e71d8e70fd353e9de47199ad2075/source/vice; the only git commands were read-only cat-file -t and ls-tree through --git-dir on .git/modules/native/vice.

## Mandatory first step, trust and environment

- add-profile executed from C:\Users\kingd\.claude\skills\add-profile\SKILL.md through the plugin workflow.memory.list/get (scope Global): unfiltered list totalCount 38; manifest MEMORY-PROFILE-000; 22 profile parts decoded with SHA256 equal to both snapshot and manifest hashes, list body equal to get body for all; 15 other Global memories read in full; 38 total records; no failures. Applied guidance: 98/98 gate with zero FAIL/UNKNOWN, full verdict in my MCP turn, BDPv4 per MEMORY-PROCEDURE-002 (green discriminating mocks with in-suite negative controls before production; older red-first wording superseded), PowerShell.MCP only, native-object serialization, no Python, no em/en dashes, no tables. Conflicts resolved by the latest brief: isolated console over MEMORY-PREFERENCE-001, Opus 5.5 signature over MEMORY-PREFERENCE-004, no Codex reviewer launch over MEMORY-PREFERENCE-007.
- Hostile-validator skill read from C:\Users\kingd\.grok\skills\hostile-validator\SKILL.md and applied with the current overrides.
- Console isolation: own sub-agent console sa-5bb1338b. Before the first plugin call I removed six inherited MCP_/GROK_ names and applied the 17-variable environment from this directory's request.jsonl (SHA256 56A7566508A09E104230C309092A3D6DCB0675C6437C8B912B4236ABAF17ABA2) with 0 mismatches; MCP_FAILSAFE_DRAIN_DISABLED=1; cache and failsafe under .mcpServer\claude\reviews\20261010T161500-0500-tape-amendment-opus.
- Marker F:\GitHub\vice-sharp\AGENTS-README-FIRST.yaml (SHA256 96ACFE5069A1FC5D8E39A6248CA9CD1FD9328308E576B79FBEB4FFC85516B508, written 2026-10-10T21:00:39.0413705Z, server PID 73428): plugin Test-MarkerSignature True; plugin Invoke-FullBootstrap (signature plus /health nonce) True at 2026-10-11T00:08:07; independent nonce hv-tape-midnight-20261011T000807933-87788 echoed exactly; server 1.4.40+67e0b325f3991c57af4f017ee3f7ed91280f6147. Installed Claude plugin 1.119.0; marker declares 1.118.0 (disclosed, not repaired). All credential values (apiKey including its inline X-Api-Key and query-parameter occurrences, the signature value, and secret-like environment values) were replaced before any rendering; no raw marker text was printed.
- Plugin status: available, agent ClaudeCode, pendingCount 0, failsafeCount 0, quarantine 0.

## Inputs, pins and baseline

- Current pins: 146 of 146 match SHA256 and byte length at 2026-10-11T00:16:41 and again at 00:39:59 (0 missing, 0 drift). The parent's prior-input-checks.json agrees (146 unchanged).
- Prior review pins: 94 total, 93 unchanged, 1 changed (AGENTS-README-FIRST.yaml, 3A13AA1C... to 96ACFE50...), 0 missing.
- Native: 16 selected files; my git blob SHA1 equals the commit-tree blob (vice/ prefix) and the pinned blob, and SHA256 matches, for 16 of 16. Selected-file identity only.
- Live requirements: 235 FR, 139 TR, 142 TEST, 233 mappings, layer-1. All 516 records and 233 mappings are identical between the retained 2026-10-10T16:09 effective response and my live reads at 00:14:48 and 00:39:59.
- Triage readbacks (routed store F:\GitHub\McpServer): triage-report-25583068f60e48b19d678d556ea2b304 grouped; triage-report-b0d27fb541e84ff6ae8278667c3e0a34 grouped. Grouped is intake, not resolution.

## Surface A: requested validation (exactness and safety)

- A1 PASS. Exact eleven-record scope, kinds and operations. The JSON (SHA256 1D8A442588ADD7883693694352954DF2077D166FB47ED4E2924AFE738A481507) contains exactly the eleven listed IDs; every operation is update and every approvalStatus is unapplied.
- A2 PASS. Declared changedFields equal the actual before/after differences for 11 of 11 (FR: body and acceptanceCriteria, plus title for FR-TAP-002 and title/status for FR-BACKFILL-MEDIA-001; TR: title, body, acceptanceCriteria; TEST: title, condition, acceptanceCriteria).
- A3 PASS. Unrelated fields preserved: id, priority, workspaceId and scopeStartLayerKey identical; nothing outside title/body/condition/status/acceptanceCriteria changes; notes is absent from the delta and null live for all eleven.
- A4 PASS. All 47 prior criterion identities retained and 71 proposed (6 to 8, 4 to 6, 4 to 7, 4 to 5, 5 to 8, 6 to 6, 7 to 8, 6 to 6, 5 to 5, 0 to 7, 0 to 5); no duplicates; all IDs ac-NN. Retention is identity continuity only (N-14).
- A5 PASS. All after statuses pending, all 71 criteria isSatisfied false, FR-BACKFILL-MEDIA-001 closed to pending.
- A6 PASS. Coverage ledger: 71 rows with 71 unique keys equal to the 71 criteria; 286 group references and 58 unique names, of which 5 are ExistingMediaProposal placeholders (N-13). No mocks or providers executed.
- A7 PASS. Seven mapping rows, 15 additive links, zero removals; TR-CYCLE-001 and TEST-X64SC-LOCKSTEP-001 links retained; FR-RUNTIME-TAPE-002 row is a no-op.
- A8 FAIL. Not every additive link is applicable: TR-MEDIA-D64-001 and TEST-D64IO-001 do not exist in the live store; they exist only as op=create rows of the excluded, unapproved parent media/video proposal (SHA256 4CF5B9A3A8FA47A5904F53FC3DC39140FDB7E5A8A852F8B65AA723A285D0C1AD). Neither generator nor verifier checks target existence (N-01).
- A9 PASS. The FR-BACKFILL-MEDIA-001 record row is JSON-identical to the parent row; its mapping values are identical in order (property order only differs); no other parent row is included.
- A10 PASS. My rerun of Verify-Proposal.ps1 at 00:27:00 reproduced 250 checks, 0 failed, with names and results identical to artifact-checks.json, and left the proposal directory unchanged; the initial run's 8 Markdown failures are explained by the section-parser fix; the parent's 2026-10-10 rerun is also 250/0. Structural only.
- A11 PASS. Hashes: JSON 1D8A4425...; draft1 7020A59D11EEF640919579EC404E3DC237B70C9E7C33272D5195F7B3B3F4845C; parent 4CF5B9A3...; tape-preimages.json 470F6E228C95EE45D17082AF2158EBCE2ADB0853AD1CA96FAC5B93CC8D399550; recovery.json F69733AAA3ED2C21477A776D46846501E0D2B1453A3D025336D313F1E46935EB.
- A12 PASS. Proposal before-values equal the fresh baseline: my recompute against the retained effective response gives 88 of 88 record fields and 28 of 28 mapping fields equal with non-null values; against my live read at 00:14:48, 88 of 88. The retained receipt's own per-check values are null (N-04).
- A13 PASS. No drift: 516 records and 233 mappings unchanged from 2026-10-10T16:09 to 2026-10-11T00:39:59.
- A14 PASS. Sixteen selected native files are the immutable commit blobs (see Inputs).
- A15 PASS. Current and prior pins as above.
- A16 PASS. The proposal claims no product code, tests, requirement or mapping writes, phase advancement or parity; verification.json records zero writes, tests and builds.

## Surface B: workspace rules

- B1 PASS. BDPv4 sequencing: R2/R3 and the TEST criteria require correct mocks green, wrong mocks rejected as expected negative controls inside a passing suite, the same assertions bound to real providers, full cumulative suites, and no red gate (MEMORY-PROCEDURE-002).
- B2 PASS. Approval boundary: unapplied, explicitly outside the earlier five-record approval; the 2026-10-10 MCP recovery is labelled operational evidence, not hostile acceptance.
- B3 PASS. MCP-only storage: no direct storage edit; R1 uses supported updateBatch and mapping operations; the generator and verifier make no MCP calls and no writes.
- B4 PASS. PowerShell only, native-object serialization; parent tool calls not directly observable.
- B5 PASS. Claims match artifacts; recorded defect N-04 (comparison entries without values despite the README's description). The earlier 11-event miscount is corrected by EVIDENCE-CORRECTION.md; the parent's completed turn req-20261010T211205Z-tape-amendment-hv still carries the superseded text, which completed turns cannot change.
- B6 PASS. Reviewer secret handling: in-console parsing and full-text redaction; 62 evidence files scanned with 0 current-credential hits before reading; earlier raw response streams not read; this attempt's response stream (3,843,284 bytes at 00:43:05) contains 0 occurrences of any of 10 credential candidates.
- B7 PASS. Isolated console and environment applied before plugin use (first plugin call 00:07:43).
- B8 PASS. Agent identity ClaudeCode; own hook-created session and turn used; structured model field is the host default claude, exact model carried in tags and dialog; see N-16.
- B9 PASS. No drain or replay; no writes to parent or earlier review turns; parent failsafe 20261011T005552Z-session_submit-8629.yaml untouched.
- B10 PASS. Native immutability preserved.
- B11 PASS. No em/en dashes in 41 parent-authored packet, audit, recovery and review-root files (generated exports remain F-08).

## Surface C: requirement adequacy against immutable VICE

- C1 PASS. All eleven records carry structured, testable criteria (71) with explicit observables, negative controls and fail-closed missing-evidence rules.
- C2 PASS. The amended text is accurate wherever I checked it against pinned source (see Verified native facts): stop deadline versus actual callback, pending later alarms and STOP, post-deadline cancellation, 1000 versus 32000, immediate running flag and recording windows, both RECORD/motor orderings (gaps 100 and 1200), C64 bit 5 motor and bit 3 write with DDR masking, raw VIC-20 PCR bit 1 and effective PB3 including DDRB writes, SX-64, enable/dispatch/attach/detach/reset, sense dongle, CP Clock F83, v0 zero, block decoder versus live path, v2, header warnings versus rejections, 36-54/55-73/74-100, gap encoding and 24-bit truncation, partial writes and the native datasette sound chip.
- C3 FAIL. Completeness gaps: the start helper's second entry point (N-05); hidden process-lifetime state (N-07); attach/detach/reset quirks (N-08); RECORD-with-running-motor write position (N-09); unknown TAP versions and minimum size (N-10).
- C4 FAIL. Discrimination gaps: no named case rejects a mock that schedules startup only on motor-on transitions (N-05); before-deadline cancellation has no named case (N-06).
- C5 FAIL. Corpus consistency: FR-CPU-004 ac-03/ac-05 and FR-PRF-003 ac-02/ac-04 contradict the amended wiring and pinned VICE (N-02); reused FR-BACKFILL-MEDIA-001 text depends on unapproved or stale criteria (N-11); newly linked FR-TAP-005 unaudited (N-12).
- C6 PASS. The union TR contracts carry both historical meanings of each collided ID (backfill document lines 391-434) and add the historical trace links; nothing is retired or renamed. Backfill and wiki reconciliation remains an R1 step.
- C7 PASS. The closed parent FR is proposed pending with five unsatisfied criteria, not falsely complete.
- C8 PASS. The ledger is presented as planned case groups, not existing tests; FR-TAP-001 ac-05 and FR-TAP-002 ac-05 (ITapeUnit MotorState, Mount/Eject) are unchanged; TEST-TAP-001 ac-03 names all five loaders; my search confirms src/tests have no ITapeUnit or MotorState and Datasette implements ITapeDevice.
- C9 FAIL. R0/R1 are not sufficient for this exact delta: no target-existence or ordering check against the parent proposal (N-01), no exclusion of read-synthesized timestamps (N-03), no value-bearing comparison receipts (N-04), no cross-record contradiction check (N-02).
- C10 N/A. Executable AC-mapped tests are not expected for an unapplied requirement-only amendment, and none are claimed.

## Surface D: plan holistically

- D1 PASS. Consistent with the controlling plan's locked decisions 2, 3, 6, 10, 11 and 12 and the immutable-native amendment (plan.md SHA256 436BB2B1DA834491A9B0C8F0F6EFD3F1EBD81F001885AD31D6FE8180CDE26473, read in full; PLAN-VICE-IMMUTABLE-001.md and HANDOFF.md read in full).
- D2 PASS. Nothing is claimed complete. Recovered-requirements acceptance, dense session proof scope, full immutable xvic/x64sc cycle parity, native VSF as canonical snapshot format, settings visibility, full cumulative and real-emulator gates and the truthful final handoff all remain open.
- D3 PASS. Deferred tape work is visible in durable state: the five applied records are pending with 23 unsatisfied criteria; the amendment, audit, recovery and attempts are retained as receipts and parent turns.
- D4 FAIL. HANDOFF.md was last written 2026-10-08T14:44:01 local, before the 2026-10-09 application, the source audit and this proposal, the 2026-10-10 service replacement and recovery, and three review attempts. None of 39 open TODOs references the tape records or amendment; PLAN-REQAUDIT-001's note is dated 2026-09-28 and still says red tests first; PLAN-VICEORACLE-001's remaining text is stale (N-18, F-13).
- D5 PASS. This review is an approval-readiness review of an unapplied delta; it is not acceptance of the five-record application, a completed phase or goal completion.

## FAIL and UNKNOWN items

- A8 FAIL: two of fifteen additive mapping links target nonexistent records (N-01).
- C3 FAIL: in-scope behavior omitted from the criteria (N-05, N-07, N-08, N-09, N-10).
- C4 FAIL: planned cases do not reject two classes of wrong mocks (N-05, N-06).
- C5 FAIL: contradictions with FR-CPU-004 and FR-PRF-003 and dependent wording (N-02, N-11, N-12).
- C9 FAIL: R0/R1 safeguards insufficient (N-01, N-02, N-03, N-04).
- D4 FAIL: stale handoff and TODO tracking (N-18).
- UNKNOWN: none. N/A: C10.

## Must fix before approval

1. N-01: remove or defer the FR-BACKFILL-MEDIA-001 links to TR-MEDIA-D64-001 and TEST-D64IO-001, or explicitly include and approve the two parent creation rows and apply them first; add a target-existence pre-check to R1.
2. N-02: add exact corrections for FR-CPU-004 ac-03/ac-05 and FR-PRF-003 ac-02/ac-04 for explicit approval, or obtain the operator's explicit acceptance of a recorded open contradiction with a tracked follow-up.
3. N-03 and N-04: amend R1 to exclude read-synthesized createdAt/updatedAt from preservation evidence, compare stable fields, and retain compared values or hashes.
4. N-05: specify both start-helper entry points in FR-RUNTIME-TAPE-002 ac-03 and TR-TAPE-EDGE-001 ac-01 and add a named case for START/FORWARD/REWIND with the motor already running.
5. N-06: restore a before-deadline cancellation case.
6. N-11: reword the reused media row so it references live records and states its dependency on the separate drive approval.

A corrected exact delta needs a fresh HV and the operator's explicit approval.

## Later mandatory gates (not blockers for this requirement text, but required before tests or acceptance)

N-07 hidden static state in the fixture/provider design; N-08 lifecycle cases; N-09 recording-position scope decision; N-10 unknown TAP versions and minimum size; N-12 FR-TAP-005 audit; N-15 superseded-test disposition before R2; prior F-08, F-10, F-11 and F-12; the R2 green discriminating mock gate and its HV; R3 real managed/native gates, xvic/x64sc parity, native VSF, settings visibility, full cumulative suites and the final handoff.

## New findings

### N-01 high (A8/C9): two of the 15 additive mapping links target records that do not exist

The reused FR-BACKFILL-MEDIA-001 mapping row adds TR-MEDIA-D64-001 and TEST-D64IO-001. Neither exists in the live store (516 records at 00:14:48 and 00:39:59); both are op=create rows of the unapproved parent media/video proposal, which this amendment deliberately excludes. Applying R1's mapping operations would fail or create dangling links, and the README presents 15 additive edges without disclosing the dependency. New-Proposal.ps1 lines 209-214 import the full parent mapping; Verify-Proposal.ps1 has no existence check. Recommendation: drop or defer the two links, or include the two creation rows with their own approval and apply them first; add a target-existence pre-check.

### N-02 medium-high (C5): other pending requirements contradict the corrected C64 and SX-64 port wiring

FR-CPU-004 ac-03 says "Bit 3 controls the cassette motor (active low)" and ac-05 says "Bit 5 is unused on C64"; FR-PRF-003 ac-02/ac-04 say SX-64 cassette sense always reads "no tape" and bit 4 is permanently pulled high. Pinned VICE drives the motor from bit 5 and writes on bit 3 (c64pla.c:77-89), reads input bit 5 as 0 (c64pla.c:61-63), and on SX-64 passes tape_sense, write_in and motor_in as 1 with pull-up 0x07, so input bits 3-5 read 0 (c64mem.c:218-222, c64pla.c:65-75). The amendment corrects FR-TAP-001 and adds SX-64 behavior to FR-RUNTIME-TAPE-002 but leaves these contradictions, so the corpus would hold mutually unsatisfiable criteria. tests/ViceSharp.TestHarness/FrCpu004AcceptanceTests.cs:67-90 already asserts bit 5 under the name PortBit3_ControlsTheCassetteMotorActiveLow and documents that the FR text is wrong. The prior review's F-05 statement that FR-TAP-001 was the only C64 motor wiring requirement was inaccurate.

### N-03 medium (C9): requirement timestamps are synthesized per read across getFr, listTr and listTest

Eight getFr reads (seven FRs, FR-RUNTIME-TAPE-002 twice) between 00:15:40 and 00:15:53 each returned createdAt/updatedAt equal to the instant of the read; listTr returned 139 distinct createdAt values inside 05:16:09.75Z to 05:16:09.83Z and listTest 142 inside 05:16:12.12Z to 05:16:12.19Z. The defect is systemic, not a single-FR quirk, and triage report 25583068 describes only the single-FR case. R0 step 3 (capture timestamps) and R1 step 3 (compare full metadata, stop on mismatch) would always stop or rely on meaningless values. Stable fields are available: TR-TAP-EDGE-001 area TAP/subarea EDGE; TR-TAPE-EDGE-001 area TAPE/subarea EDGE; TEST-TAPE-RAMP-001 and TEST-TAPE-SENSE-001 area TAPE, testType unit; TEST-TAP-001 area TAP, testType unit; notes null for all eleven. Recommendation: exclude timestamps from preservation evidence, compare stable fields plus the effective projection, and have the parent append this broadened observation to the existing triage report. I did not submit a new triage report because the defect is already triaged and this review's write scope is limited to its own receipts and turn.

### N-04 low (B5/C9): retained baseline comparison entries carry no compared values

All 88 recordChecks in tape-preimages.json store before=null and current=null with equal=true, while the recovery README says the file contains "individual comparisons, not merely counts". The equality claim is true (my recompute: 88/88 and 28/28 with non-null values) but the receipt cannot show it by itself. R1 comparisons must retain compared values or per-field hashes.

### N-05 medium (C3/C4): the second entry point of the ordinary start helper is not specified or tested

datasette_start_motor runs on a stopped-motor motor-on transition (datasette.c:1186-1190) and also whenever START, FORWARD or REWIND is accepted while the motor already runs, with or without an image (1065-1067, 1076-1078, 1087-1089, 1123-1125, 1134-1136, 1145-1147). That path arms the 32000-cycle startup alarm when none is pending and seeds last_write_clk/last_write_bit (1000-1021). FR-RUNTIME-TAPE-002 ac-03 describes ordinary startup only for a stopped motor, and no named case covers PLAY pressed with the motor already on (for example the C64 DDR-input motor-on state after reset, c64pla.c:77-81). A mock that schedules startup only on motor transitions passes every named case.

### N-06 low-medium (C4): before-deadline cancellation lost its named case

The previously approved MotorOn_CancelsPendingStop is absent from the 58 groups; only MotorOn_CancelsStopAfterDeadlineBeforeCallback remains, so a mock that mishandles motor-on before the deadline is not rejected by any named group.

### N-07 medium (C3, later mandatory gate): hidden process-lifetime native state is not required to be pinned

State that attach, detach and reset do not clear changes expected observations: tape wobble is on by default (3 Hz, 0.5 percent; datasette.h:64-74) with function-static wobble_sin_count/restf (datasette.c:436-480) advanced even by the attach-time full-image gap scan (897-901); azimuth error draws lib_unsigned_rand when nonzero (482-502); last_write_bit starts at -1 (86); C64 old_port_* statics start at 0xff and survive pport reset (c64pla.c:40-46, 106-123); VIA reset sets oldpb=0 while the effective PB output is 0xFF (viacore.c:378-424); datasette sound ring and sign statics (datasette-sound.c:36-45); CP Clock F83 RTC wall clock. The criteria demand identical resource state but do not require pinning or neutralizing these hidden states.

### N-08 low (C3): attach, detach and reset quirks are not enumerated

Attach and reset set motor_stop_clk=0 without clearing datasette_motor (datasette.c:978), cancelling a pending delayed stop and leaving the motor running indefinitely; a stale read alarm survives attach because internal_reset inspects the new image's STOP mode (886-888, 954-966); detach and no-image reset re-apply the stale notape_mode command (967), which can schedule fast-wind or startup alarms and transiently assert sense before attach clears it (905-907). Only the generic Datasette_AttachDetachResetState group covers this.

### N-09 low (C3): recording file position depends on command order

fseek happens only in datasette_start_motor (datasette.c:1003-1005). RECORD accepted while the motor already runs performs no fseek (1091-1099), so writes go to the stdio position left by earlier buffered reads (396-403), and C requires a positioning call between input and output on update streams. FR-TAP-004 ac-04 could bind to platform-dependent native behavior; the fixture design must decide scope.

### N-10 low (C3): unknown TAP versions and the minimum-size rejection are not enumerated

The header reader stores any version byte (tap.c:141); the block decoder returns 0 for a zero byte without consuming an extension for versions other than 0, 1 and 2 (404-415), while the live path consumes a three-byte extension for any version of 1 or more (datasette.c:512-531). tap_open ignores the declared data length and rejects images with fewer than 3 data bytes (tap.c:250-256).

### N-11 low (C5): reused media row text depends on unapproved or stale criteria

FR-BACKFILL-MEDIA-001 ac-02 requires "the separately proposed FR-ARCH-TRUEDRIVE-1541-002 criteria", but that live record is closed with 0 criteria and its criteria exist only in an unapproved drive proposal; ac-03 cites "the separately proposed FR-RUNTIME-TAPE-002 criteria", which were applied on 2026-10-09 and are amended by this packet.

### N-12 low (C5): newly linked FR-TAP-005 was not audited

FR-TAP-005 is now mapped to the union TR-TAP-EDGE-001 but keeps hardware approximations ("down to approximately 160 cycles per pulse"; "CIA FLAG input timing is accurate to 1 system clock cycle", C64 only) of the same class the amendment corrects in FR-TAP-001 ac-02 and FR-TAP-003 ac-03.

### N-13 info (A6): five of the 58 case-group names are placeholders

ExistingMediaProposal_ac-01..05 point to another proposal's ledger; 53 names are tape case groups.

### N-14 info (A4): retained criterion IDs carry rewritten, sometimes inverted, meanings

45 of 47 retained IDs have new text; only FR-TAP-001 ac-05 and FR-TAP-002 ac-05 are unchanged. FR-TAP-001 ac-04 reverses the PLAY prerequisite and FR-TAP-002 ac-02 reverses v0 extension consumption. The approval request should state that identity continuity is not obligation continuity.

### N-15 low-medium (C/D, later mandatory gate): existing tests cite amended IDs with superseded expectations

StorageRuntimeTests.cs:181-199 decodes a v0 zero byte followed by a 24-bit overflow (citing FR-TAP-002); :202-206 rejects a header length larger than the data (citing FR-TAP-002, while native ignores the declared length); DatasetteSenseLineTests and DatasetteMotorRampTests cite the TR IDs with earlier semantics; FrCpu004AcceptanceTests carries a misleading bit-3 name. R2/R3 require full cumulative suites green, so a requirement-traceable superseded-test disposition step is needed before R2.

### N-16 info (B8): hook-created duplicate turn and leftover attempt sessions

Hooks created two in-progress turns for this launch in the same second (req-20261011T050452Z-prompt-75df and -ba39; the plugin binds -ba39). The two quota-failed attempts left in-progress hook sessions ClaudeCode-20261010T211713Z-plugin-session and ClaudeCode-20261011T005900Z-plugin-session with two turns each. Hook request IDs are not in the canonical sequence/slug format and the structured model field is the host default claude. I did not mutate any of these.

### N-17 info (B): reviewer conduct notes

Two interim status lines labelled 00:08 and 00:10 and four "approx" stamps (00:09:30, 00:11:40, 00:12:30, 00:13:30) were my estimates, not console-measured; I corrected this inline and used console stamps afterward. I created and deleted one scratch file in %TEMP%, and PowerShell.MCP spilled large outputs into its own Temp files. reviewer-evidence.json and verdict.json were written with .NET WriteAllText because the console's file-append cmdlet guard accepts only literal var1 content; both are serializations of in-memory native objects and were read back exactly. Two read-only native MCP triage_status calls ran outside the console environment.

### N-18 low (D4): root handoff and TODO tracking are stale

See D4.

## Prior findings disposition (2026-10-09 review, DISAGREE 95/88, unchanged by this verdict)

- F-01: resolved by proposed text (unapplied); executable proof is a future gate. FR-RUNTIME-TAPE-002 ac-04, TR-TAPE-EDGE-001 ac-02/ac-04, TEST-TAPE-RAMP-001 ac-02 and the MotorOff/MotorOn groups separate deadline from callback; verified datasette.c:729-751, 806-851, 1052-1058, 1182-1205 and alarm.h:176-183. The earlier shorthand that STOP implies exact-deadline stopping is correctly narrowed.
- F-02: resolved by proposed text with residual gap N-05. Verified datasette.c:925-949, 1069-1090, 1006-1008.
- F-03: resolved by proposed text (unapplied). Verified datasette.c:735-739, 1182-1191, 1303-1321.
- F-04: resolved by proposed text (unapplied). I recomputed gap 100 (byte 12, remainder 4) and gap 1200 (byte 150) from datasette.c:1010-1020, 1035-1038, 1091-1099, 1210-1322.
- F-05: partially resolved; new defect N-02.
- F-06: resolved by the union text and trace links; backfill/wiki projection reconciliation is a future R1 step.
- F-07: resolved in text by the reused parent row; new defects N-01 and N-11.
- F-08: still open. docs/Project/Technical-Requirements.md and its wiki copy each contain 139 U+2014 (00:32:45); R1 export regeneration will reproduce them unless the generator changes.
- F-09: partially addressed going forward (fresh stable non-projected fields captured); pre-2026-10-09 values remain unrecoverable; timestamps unusable (N-03).
- F-10: still-open historical limitation; R1 step 4 commits to retaining raw export receipts.
- F-11: still open and widened (N-15). FR-RUNTIME-TAPE-002, TEST-TAPE-RAMP-001, TEST-TAPE-SENSE-001, FR-TAP-004, FR-TAP-005, TEST-TAP-001 and FR-BACKFILL-MEDIA-001 have 0 references in src/tests; the TR IDs and FR-TAP-001/002/003 are cited only by pre-existing code and tests with superseded expectations.
- F-12: still open (tooling); will affect the four retitled TEST records after application.
- F-13: still open (D4, N-18).
- F-14: largely resolved by proposed text; residual gaps N-07, N-08, N-09, N-10.
- F-15: still-open historical limitation. The marker key changed with the 2026-10-10 service replacement; invalidation of the earlier key is inferred, not verified. Not repeated here (B6).
- F-16: resolved; both post-launch receipts are pinned and rehash to their pinned values.
- F-17: still-open process observation (informational); see N-16.

## Verified native facts (immutable commit cc67418cba58e71d8e70fd353e9de47199ad2075)

- MOTOR_DELAY 32000 and MOTOR_DELAY_FAST 1000 (datasette.c:62-63).
- The start helper arms startup only when no datasette alarm is pending and is reached from motor-on and from START/FORWARD/REWIND with the motor running (datasette.c:1000-1021, 1065-1089, 1123-1147, 1186-1190).
- Motor-off sets the deadline once and arms an alarm only when none is pending; motor-on clears the deadline without restarting a running motor (datasette.c:1182-1205).
- The callback stops the motor only when the clock has reached the deadline; active reading replaces the same alarm (datasette.c:729-751, 806-851; alarm.h:176-183).
- STOP leaves a pending alarm in place (datasette.c:1052-1058).
- FORWARD/REWIND assign the mode first and then arm clock+1000 regardless of motor state (datasette.c:925-949, 1069-1090).
- Writes require an image, RECORD mode and the motor flag plus a 0 to nonzero edge; the line value is always retained (datasette.c:1303-1321).
- Accepted RECORD resets last_write_clk without the start helper (datasette.c:1091-1099, 1035-1038).
- Gap encoding: below 7 carried; 7 to 2046 one byte (7 emits 0x00); 2047 and above a zero byte plus the low 24 bits for v1/v2; failures stop the tape; size and has_changed update only after success (datasette.c:1215-1291).
- C64: motor !(DDR & DATA & 0x20), write (~DDR | DATA) & 0x08, sense on input bit 4; SX-64 disables forwarding and reads inputs 3-5 as 0 (c64pla.c:61-96; c64mem.c:218-222).
- VIC-20: motor from the raw changed PCR bit 1, write from effective PB3 including DDRB writes, sense on VIA1 PA6 (vic20via1.c:188-198, 246-251; vic20via2.c:115-117; viacore.c:717-724).
- Read pulses: C64 CIA1 FLAG; VIC-20 VIA2 CA1 falling (c64datasette.c:36-41; vic20datasette.c:37-42).
- CBM bands 36-54, 55-73, 74-100 after tap_init division by 8; TurboTape bands 10-34 and 35-54 unchanged (c64.c:231-248; vic20.c:267-284; tap.c:52-82, 1673-1680).
- Block decoder: v0 zero 256 units, v1/v2 zero 24-bit extension shifted right by 3, v2 sums halfwaves; live path byte times 8, v0 zero DatasetteZeroGapDelay, then wobble and misalignment (tap.c:389-444; datasette.c:504-535).
- Header: short read or bad magic rejected; tag and video mismatches warn; Plus4 system mismatch logs an error without rejecting; declared length ignored on open; fewer than 3 data bytes rejected (tap.c:121-197, 218-265).
- Datasette audio is its own sound chip with a 200-entry gap ring, flushed when DatasetteSound is off (default 0) (datasette-sound.c:36-76; datasette.c:270-295, 839-841).
- Sense dongle asserts sense on enable and power-up only; CP Clock F83 drives SDA from inverted motor, SCL from normalized write, and asserts sense only when RTC data is high and motor state is false (sense-dongle.c:68-94; cp-clockf83.c:161-192).
- Tape traps serve T64 only and are deinstalled while a TAP is attached; trap resources TrapDevice1/2 default 0 (tape.c:232-272, 480-499, 584-595; traps.c:74-150).

## Scope limitations

- Source inspection and stored-state comparison only; no native or managed execution, mock run, build or runtime trace.
- Sixteen selected native files only; whole-checkout cleanliness of native/vice not audited.
- Historical pre-2026-10-09 metadata cannot be reconstructed, and requirement timestamps are read-synthesized.
- Parent tool calls are not directly observable.
- No triage submission by this reviewer (see N-03).
- The general host environment was not audited, per instruction.

## Scores

Accuracy 93: hashes, counts, changed fields, identity retention, mapping counts, exact parent-row reuse, baseline equality, native identity and every checked source-derived claim are correct; deductions for 2 of 15 additive links invalid in the live store yet presented as ready edges (N-01), the overstated recovery comparisons (N-04), the narrow characterization of the timestamp defect (N-03), the reused row's stale dependencies (N-11) and the placeholder group count (N-13).

Completeness 84: strong coverage of the prior five FAIL themes and the TR union; deductions for the unreconciled FR-CPU-004/FR-PRF-003 contradictions (N-02), the missing start-helper entry point and cancellation case (N-05, N-06), unenumerated hidden state, lifecycle, recording-position and version cases (N-07 to N-10), the unaudited FR-TAP-005 (N-12), the missing superseded-test disposition (N-15), insufficient R1 safeguards (N-01, N-03, N-04) and stale handoff/TODO tracking (N-18).

Both scores are below 98; AGREE is not possible regardless of the FAIL count.

## Persistence

These files are written before MCP persistence. The full verdict prose, verdict.json and every finding are saved to my own session turn in ordered chunks; primary readback proof, hashes and the final response-stream scan are recorded in verdict.persistence.json in this directory and in the final response. If primary persistence genuinely fails, the authorized full-content failsafe will be used and labelled failsafe-pending with PrimaryReadback false.

## Receipt paths

- F:\GitHub\vice-sharp\docs\receipts\hv\20261010T161500-0500-tape-amendment-opus\resume-20261011T000300-0500\verdict.md
- F:\GitHub\vice-sharp\docs\receipts\hv\20261010T161500-0500-tape-amendment-opus\resume-20261011T000300-0500\verdict.json
- F:\GitHub\vice-sharp\docs\receipts\hv\20261010T161500-0500-tape-amendment-opus\resume-20261011T000300-0500\verdict.persistence.json
- F:\GitHub\vice-sharp\docs\receipts\hv\20261010T161500-0500-tape-amendment-opus\resume-20261011T000300-0500\reviewer-evidence.json
- F:\GitHub\vice-sharp\docs\receipts\hv\20261010T161500-0500-tape-amendment-opus\resume-20261011T000300-0500\request.jsonl
- F:\GitHub\vice-sharp\docs\receipts\hv\20261010T161500-0500-tape-amendment-opus\resume-20261011T000300-0500\response.jsonl
- F:\GitHub\vice-sharp\docs\receipts\hv\20261010T161500-0500-tape-amendment-opus\resume-20261011T000300-0500\stderr.log
- F:\GitHub\vice-sharp\docs\receipts\hv\20261010T161500-0500-tape-amendment-opus\resume-20261011T000300-0500\input-pins.json
- F:\GitHub\vice-sharp\docs\receipts\hv\20261010T161500-0500-tape-amendment-opus\resume-20261011T000300-0500\prior-input-checks.json
- F:\GitHub\vice-sharp\docs\receipts\hv\20261010T161500-0500-tape-amendment-opus\resume-20261011T000300-0500\resume.prompt.md
- F:\GitHub\vice-sharp\docs\receipts\hv\20261010T161500-0500-tape-amendment-opus\prompt.md

## VERDICT JSON

```json
{"overallVerdict":"DISAGREE","passCount":35,"failCount":6,"unknownCount":0,"notApplicableCount":1,"accuracy":93,"completeness":84,"findings":["N-01 high [must fix before approval] Two of the 15 additive mapping links target records that do not exist","N-02 medium-high [must fix or explicitly acknowledge before approval] Other pending requirements contradict the corrected C64 and SX-64 port wiring","N-03 medium [must fix before application (R1 safeguard)] Requirement timestamps are synthesized per read across getFr, listTr and listTest","N-04 low [must fix before application (receipt quality)] Retained baseline comparison entries carry no compared values","N-05 medium [must fix before approval (AC wording and named case)] The second entry point of the ordinary start helper is not specified or tested","N-06 low-medium [should fix before approval] Before-deadline cancellation lost its named case","N-07 medium [later mandatory gate; must be explicit before tests] Hidden process-lifetime native state is not required to be pinned","N-08 low [later mandatory gate] Attach, detach and reset quirks are not enumerated","N-09 low [later mandatory gate (fixture scope decision)] Recording file position depends on command order","N-10 low [later mandatory gate] Unknown TAP versions and the minimum-size rejection are not enumerated","N-11 low [should fix before approval (wording)] Reused media row text depends on unapproved or stale criteria","N-12 low [later mandatory gate] Newly linked FR-TAP-005 was not audited","N-13 info [disclosure] Five of the 58 case-group names are placeholders","N-14 info [disclosure for the approval text] Retained criterion IDs carry rewritten, sometimes inverted, meanings","N-15 low-medium [later mandatory gate (before R2)] Existing tests cite amended IDs with superseded expectations","N-16 info [disclosure] Hook-created duplicate turn and leftover attempt sessions","N-17 info [reviewer self-disclosure] Reviewer conduct notes","N-18 low [current process gap (D4)] Root handoff and TODO tracking are stale","F-01 prior: resolved by proposed text (unapplied); executable proof is a future mandatory gate","F-02 prior: resolved by proposed text with a residual gap (N-05)","F-03 prior: resolved by proposed text (unapplied); future mandatory gate","F-04 prior: resolved by proposed text (unapplied); future mandatory gate","F-05 prior: partially resolved; new defect N-02","F-06 prior: resolved by proposed union text and mappings; projection reconciliation is a future R1 step","F-07 prior: resolved in text by the reused parent row; new defects N-01 and N-11","F-08 prior: still open (tooling); R1 export regeneration will reproduce it unless fixed","F-09 prior: partially addressed going forward; still-open historical limitation","F-10 prior: still-open historical limitation; future gate in R1 step 4","F-11 prior: still open and widened (N-15); future mandatory gate","F-12 prior: still open (tooling); affects the four retitled TEST records after application","F-13 prior: still open (D4, N-18)","F-14 prior: largely resolved by proposed text; residual gaps N-07, N-08, N-09, N-10","F-15 prior: still-open historical limitation; not repeated in this review","F-16 prior: resolved","F-17 prior: still-open process observation (informational); see N-16"],"receiptPaths":["F:\\GitHub\\vice-sharp\\docs\\receipts\\hv\\20261010T161500-0500-tape-amendment-opus\\resume-20261011T000300-0500\\verdict.md","F:\\GitHub\\vice-sharp\\docs\\receipts\\hv\\20261010T161500-0500-tape-amendment-opus\\resume-20261011T000300-0500\\verdict.json","F:\\GitHub\\vice-sharp\\docs\\receipts\\hv\\20261010T161500-0500-tape-amendment-opus\\resume-20261011T000300-0500\\verdict.persistence.json","F:\\GitHub\\vice-sharp\\docs\\receipts\\hv\\20261010T161500-0500-tape-amendment-opus\\resume-20261011T000300-0500\\reviewer-evidence.json","F:\\GitHub\\vice-sharp\\docs\\receipts\\hv\\20261010T161500-0500-tape-amendment-opus\\resume-20261011T000300-0500\\request.jsonl","F:\\GitHub\\vice-sharp\\docs\\receipts\\hv\\20261010T161500-0500-tape-amendment-opus\\resume-20261011T000300-0500\\response.jsonl","F:\\GitHub\\vice-sharp\\docs\\receipts\\hv\\20261010T161500-0500-tape-amendment-opus\\resume-20261011T000300-0500\\stderr.log","F:\\GitHub\\vice-sharp\\docs\\receipts\\hv\\20261010T161500-0500-tape-amendment-opus\\resume-20261011T000300-0500\\input-pins.json","F:\\GitHub\\vice-sharp\\docs\\receipts\\hv\\20261010T161500-0500-tape-amendment-opus\\resume-20261011T000300-0500\\prior-input-checks.json","F:\\GitHub\\vice-sharp\\docs\\receipts\\hv\\20261010T161500-0500-tape-amendment-opus\\resume-20261011T000300-0500\\resume.prompt.md","F:\\GitHub\\vice-sharp\\docs\\receipts\\hv\\20261010T161500-0500-tape-amendment-opus\\prompt.md"],"sessionId":"ClaudeCode-20261011T050449Z-plugin-session","requestId":"req-20261011T050452Z-prompt-ba39","persistenceStatus":"pending at file write; see verdict.persistence.json and the final response"}
```

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>