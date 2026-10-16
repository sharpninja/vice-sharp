# Hostile validator receipt

TimestampUtc: 2026-10-05T19:12:58.197Z
TimestampLocal: 2026-10-05T14:12:58.197-05:00
ValidatorIdentity: GrokSubagentHostile
Model: Grok 4.7 at xhigh
Classification: Class 2 operator-directed lab migration. ViceSharp product requirements and Byrd phase gates are N/A.
add-profile: executed yes; non-Grok markdown files read 22; excluded add-profile.grok.md; add-profile.md read first.
SessionId: GrokCode-20261005T185531Z-f-devdrive-hv-final2
RequestId: req-20261005T185531Z-001-f-devdrive-hostile-final2
TurnId: 97740
ParentRequestJsonl: F:\GitHub\vice-sharp\docs\receipts\hv\20261005T185234Z-f-dev-drive-grok-4-7-xhigh-final2.request.jsonl
ParentResponseJsonl: F:\GitHub\vice-sharp\docs\receipts\hv\20261005T185234Z-f-dev-drive-grok-4-7-xhigh-final2.response.jsonl
ParentResponseHandling: Not opened, written, or recreated. Existence was tested only.
FailsafeUsed: False
FailsafePath: none
PassCount: 24
FailCount: 0
UnknownCount: 0
NaCount: 3
AccuracyPercent: 99
CompletenessPercent: 99
OverallVerdict: AGREE
completionPercent: 100
eta: complete

## Scores

Every scored claim was re-derived from the current source, AST, analyzer, cargo output, PE bytes, or pinned PDB symbol. Disclosed wording notes in D1 were not promoted to failures because the required order and readback gates are present.
Surfaces A through D were scored. Live machine state was excluded by the brief and is not an UNKNOWN. The parent response jsonl was not opened because the brief forbids it.

## FAIL list

None.

## UNKNOWN list

None. Live environment facts were excluded by the operator and are not scored as UNKNOWN.

## N/A list

B2, C1, D3

## Commands

- Get-FileHash and ReadAllLines for the script and pinned exe
- Parser.ParseFile, function inventory, CommandAst counts, confirmation metadata, Invoke-ScriptAnalyzer
- cargo fmt --check; cargo clippy --offline --release --all-targets -- -D warnings; cargo test --offline --release; cargo build --offline --release; isolated CARGO_TARGET_DIR
- PE parse, section SHA-256, .rdata byte diff, dbghelp SymFromName on the pinned PDB

Key outputs: script SHA-256 704F71E490FF17EBC560F4596639170970E2D6CF4C02D5BF5CE6804F0A5C8E71 length 50790 lines 1057; exe SHA-256 7C96CEDC9837DC2BE57C62B6BCB2E48F56567CE9E240546D4DFA1D249B7F509D length 13824; PARSE_ERRORS=0; FUNCTION_COUNT=38; ANALYZER_WARNING_ERROR=0; FMT_EXIT=0; CLIPPY_EXIT=0; TEST 19 passed, 0 failed, 0 ignored, doc-tests 0; BUILD_EXIT=0; PIN_UNCHANGED=True; PE machine 0x8664 subsystem 1 entry 0x1000 imports ntdll.dll; SymFromName NtProcessStartup RVA 0x1000; .text SHA-256 7DEE0F4A622AFFABC7FEE1FAC3F1A58180184E9DB0610022270545F5703693C7 on both images.

## Findings

### A1 PASS

Surface: A
Claim: The PowerShell source and pinned boot image match the expected identity.
Evidence: Get-FileHash and ReadAllLines on 2026-10-05: Move-FDevDrive.ps1 length 50790, SHA-256 704F71E490FF17EBC560F4596639170970E2D6CF4C02D5BF5CE6804F0A5C8E71, 1057 lines. fdev-boot-copy.exe length 13824, SHA-256 7C96CEDC9837DC2BE57C62B6BCB2E48F56567CE9E240546D4DFA1D249B7F509D. The script ExpectedBinarySha256 constant is that same exe digest.
Impact: The reviewed bytes are the bytes named in the brief.

### A2 PASS

Surface: A
Claim: The script parses, has 38 functions, and has no PSScriptAnalyzer warning or error.
Evidence: Parser.ParseFile returned PARSE_ERRORS=0 and FUNCTION_COUNT=38, names Get-LocalTimestamp through Invoke-Finalize. Import-Module PSScriptAnalyzer; Invoke-ScriptAnalyzer -Severity Warning,Error returned ANALYZER_WARNING_ERROR=0.
Impact: Static script quality gates pass.

### A3 PASS

Surface: A
Claim: Destructive and dynamic command counts are zero, and there is exactly one Start-Process.
Evidence: CommandAst counts: Remove-Partition 0, Clear-Disk 0, Initialize-Disk 0, Format-Volume 0, Remove-Item 0, Invoke-Expression 0, iex 0, diskpart 0, Start-Process 1. Word-boundary Remove-Partition is 0; the five substring hits are Remove-PartitionAccessPath. DYNAMIC_NAME_COUNT=0. Four Ampersand invocations are literal fsutil calls, not variable commands. Start-Process is line 541 with -Verb RunAs, -Wait, -PassThru, -Confirm:$false, and child arguments -NonInteractive -EncodedCommand.
Impact: No reviewed path deletes a partition, formats a disk, or invokes an expression. Elevation is the one RunAs boundary.

### A4 PASS

Surface: A
Claim: Every confirmation-capable mutating cmdlet call passes -Confirm:$false.
Evidence: Installed metadata: New-Partition confirm=False, Register-ScheduledTask confirm=False, and the scheduled-task constructor cmdlets confirm=False. Resize-Partition, Set-Partition, Copy-Item, New-Item, New-ItemProperty, Remove-PartitionAccessPath, Add-PartitionAccessPath, Unregister-ScheduledTask, Restart-Computer, Repair-Volume, Add-Content, and Start-Process confirm=True, and each mutating call includes -Confirm. MISSING_CONFIRM_COUNT=1 is ForEach-Object at line 314, which only collects volume drive letters. Script ConfirmPreference is None.
Impact: Prepare, cutover, and cleanup do not depend on a later confirmation prompt. New-Partition and Register-ScheduledTask do not expose Confirm.

### A5 PASS

Surface: A
Claim: A16/D9 is closed: Plan states payload install and hash before protection, then read-only readback, then registry and finalizer, then BootExecute.
Evidence: prepareSequence lines 521-527: verify; shrink E and create partition 4; install and hash-verify payload without pending state or a startup task; flush, dismount, and read-only verification; after readback, persist GUIDs and pending state and register the SYSTEM finalizer; arm BootExecute and reboot. Invoke-Prepare CommandAst order is Assert-Preflight 773, Initialize-TargetClonePartition 776, Install-MigrationPayload 777, Protect-SourcePartition 778, Initialize-MigrationRegistry 782, Register-FinalizerTask 783, Add-BootImage 788, Restart-Computer 798.
Impact: An operator who reads Plan sees the same safe order the code executes. The old line that put the finalizer and registry before protection is gone.

### A6 PASS

Surface: A
Claim: A17/D10 is closed: a Prepare exception after registry initialization starts cannot leave Status Pending after removing the task.
Evidence: Assignment AST: $registryInitializationStarted is $false at line 769 and $true at line 781, the statement immediately before Initialize-MigrationRegistry at line 782. The catch writes Status 3, LastNtStatus 3221225473 (0xC0000001), and VerifiedBytes 0 at lines 813-815 before Unregister-FinalizerTask at line 823, and only when the guard is true and the registry key exists. Failures before line 781 skip that write. Get-CurrentState lines 362-368 require both source IsReadOnly and BootExecute arming before Pending or Running says a boot copy is ready.
Impact: The false ready sentence cannot be selected for a disarmed Pending or Running state. A pre-guard failure does not rewrite an existing registry record.

### A7 PASS

Surface: A
Claim: A19/D11 is closed: display_failure and SkipFailed do not claim read-only success.
Evidence: main.rs lines 385-387 and 806-808 both say: Source read-only protection was not confirmed on this path; do not use F until Windows confirms it is read-only. The panic handler uses display_failure. LayoutError::SourceNotReadOnly is only a stage name when the attribute check failed. No remaining Rust string says the original F mapping is unchanged and read-only.
Impact: Failure, skip-failed, and panic Next text no longer asserts protection that this path did not read back.

### A8 PASS

Surface: A
Claim: The 20261005T180849Z A18/D8 power-loss window stays closed.
Evidence: Install-MigrationPayload CommandAsts are Write-MigrationState, directory create, two Copy-Item calls, Get-FileHash, and Assert-Exact. It contains zero Initialize-MigrationRegistry and zero Register-FinalizerTask calls. Protect-SourcePartition flushes and dismounts F, sets IsReadOnly and NoDefaultDriveLetter, then returns only after IsReadOnly, online, F mapping, and NoDefaultDriveLetter read back true (lines 719-743). Registry, finalizer, BootExecute readback, and reboot happen after that return. Finalize line 969 makes no protection claim. Pending or Running lines 980-1001 read both prerequisites, and a missing one records Failed, zero bytes, NTSTATUS 0xC0000001, removes an armed boot entry, unregisters the task, distinguishes read-only from writable and unchanged, and does not copy or cut over. Both true keeps the retry and says so.
Impact: Power loss before protection creates no Pending key and no startup task. Power loss after protection cannot clear the source read-only attribute on any later reviewed path.

### A9 PASS

Surface: A
Claim: The raw copy starts at zero, flushes, rereads both devices, and records Verified only after a full match.
Evidence: copy_blocks starts offset at 0 and rejects bad geometry before device access. Native read and write require the transferred length to match the request. execute_migration checks bytes_processed against 210749095936, calls flush_destination, then verify_blocks with separate buffers. VerifiedBytes, LastNtStatus 0, and Status 2 are written only after both checks, then NtFlushKey. A mismatch returns before those writes and does not flush. decide_run maps 0 Start, 1 Restart, 2 SkipVerified, 3 SkipFailed, and any other value to an error. Skip paths return before NativeDevice::open. Restart and Start clear VerifiedBytes and set Running before open. display_dashboard, display_operation_progress, both skip banners, the restart preamble, display_failure, and the panic path each have Completed, Current, and Next.
Impact: A short I/O or mismatch cannot mark Verified. Terminal and unknown states do not copy. Running recopies from byte zero.

### A10 PASS

Surface: A
Claim: Finalize permits cutover only for Verified with exact bytes and NTSTATUS 0, after the named source and target checks.
Evidence: Status 0 and 1 return before cutover. Status 3 restores the read-only F mapping and returns. Any other non-2 status throws after BootExecute removal and does not call cutover. Status 2 requires VerifiedBytes equal to SourceBytes and LastNtStatus 0. Cutover refuses a missing target or a writable source, checks the registry GUID, unmaps and offlines the old partition while leaving it read-only, temporarily mounts the target, checks ReFS and the label, resizes to TargetFinalBytes when still clone-sized, requires volume size at least final size minus 67108864, scans with Repair-Volume, checks Dev Drive by fsutil exit code, assigns F, rechecks old read-only, offline, and unmapped, rechecks the new volume, and then unregisters the task.
Impact: Pending, Running, Failed, and unknown cannot cut over. The 64 MiB floor still rejects an unexpanded source-sized volume. The partition size assertion is exact.

### A11 PASS

Surface: A
Claim: Pinned geometry does not overlap, the final target is exactly twice the source, and the short last chunk stays sector aligned.
Evidence: E offset 290455552 plus prepared size 1367861100544 equals target offset 1368151556096. Target offset plus final size 421498191872 equals source offset 1789649747968. Final size equals source size times 2. Clone-end to source-start gap equals the source size. Source end 2000398843904. Disk size slack 90112. Source, clone, and final sizes modulo 4096 are 0. Source modulo 8388608 is 2097152, and that remainder modulo 4096 is 0. E, target, and source offsets modulo 1048576 are 0.
Impact: The constants describe a non-overlapping clone stage and an exact doubled final partition. The 2 MiB tail is a legal unbuffered I/O length.

### A12 PASS

Surface: A
Claim: The pinned PE is a Native AMD64 image, imports only ntdll, and its entry symbol is NtProcessStartup at RVA 0x1000. Fresh code matches.
Evidence: Pinned and fresh images: machine 0x8664, magic 0x20B, subsystem 1, entry 0x1000, image base 0x140000000, characteristics 0x0022, import DLL ntdll.dll only, delay-import directory 0, CLR directory 0, length 13824. dbghelp SymFromName on the pinned fdev_boot_copy.pdb returned NtProcessStartup at 0x140001000 (RVA 0x1000). .text SHA-256 of both images is 7DEE0F4A622AFFABC7FEE1FAC3F1A58180184E9DB0610022270545F5703693C7 (7680 bytes). .pdata and empty .data match. Full-file fresh SHA-256 A6E8B6CCEFC1FC205041F7211F09CB3F5FFDCC0B3D75F8C0FCDE267D53206E5D differs. Pin checksum 0x5418 stamp 1791226211 RSDS GUID 8ab198ae-330d-409a-acb7-046a77863798. Fresh checksum 0x720F stamp 1791227258 RSDS GUID 7a5b6dad-d0ee-40ce-a1bf-29cbbf55b20a. .rdata differs in 21 bytes: three debug TimeDateStamp low pairs and the 16-byte GUID. Pin SHA-256 after the isolated build was unchanged. Isolated target was %TEMP%\fdev-hv-final2-target.
Impact: Executable code is equivalent. The full-file difference is link metadata, not .text.

### A13 PASS

Surface: A
Claim: The Rust release gates pass without overwriting the pinned image.
Evidence: rustc 1.99.0 and cargo 1.99.0. In E:\F-DevDrive-Migration\FDevBootCopy with CARGO_TARGET_DIR isolated: cargo fmt --check exit 0; cargo clippy --offline --release --all-targets -- -D warnings exit 0; cargo test --offline --release exit 0 with 19 passed, 0 failed, 0 ignored, and doc-tests running 0 tests; cargo build --offline --release exit 0. Checked-in cargo inputs are Cargo.toml, Cargo.lock, build.rs, src\lib.rs, and src\main.rs. build.rs passes /SUBSYSTEM:NATIVE, /ENTRY:NtProcessStartup, /NODEFAULTLIB, and ntdll.lib. No Python files are present.
Impact: Format, lint, test, and rebuild evidence matches the brief.

### A14 PASS

Surface: A
Claim: The 19 tests exercise transfer, layout, progress, and status logic. Native orchestration gaps are source-inspection findings, not unresolved facts.
Evidence: Lib test attributes: 19. Main test attributes: 0. Cargo.toml sets the binary test flag false. Each test asserts a result: progress labels and replay, copy without verify or flush, flush error mapping, verify reads without writes, mismatch with flush_count 0, unaligned geometry before device access, decide_run for 0, 1, 2, 3, and 99, and layout rejection for a writable source, a wrong destination id, a read-only destination, and a wrong destination length. Uncovered but resolved by reading source: DuplicatePartitionIdentity is reached when both expected ids are equal; SourceIdentityMismatch and SourceLengthMismatch have no direct test; short native I/O, registry checkpoint order, and the panic handler live in main.rs.
Impact: The library tests are genuine. They do not execute the boot entry. That does not leave an implementation fact unresolved.

### A15 PASS

Surface: A
Claim: No path deletes the old partition or clears its read-only flag after protection succeeds.
Evidence: Destructive command counts are zero. Cutover sets the old partition read-only, NoDefaultDriveLetter true, and offline, then checks those postconditions. Restore-SourceReadOnlyMapping forces read-only and remaps F. The Prepare catch does not call Set-Partition on the source. The native source open does not request GENERIC_WRITE. failureInvariant lines 531 distinguishes pre-protection unchanged F from post-protection read-only preservation.
Impact: The old partition remains a preserved partition on every reviewed path.

### A16 PASS

Surface: A
Claim: Source shows one explicit destructive consent, one RunAs UAC, a noninteractive child, and a SYSTEM AtStartup finalizer.
Evidence: Prepare throws unless -ConfirmDestructive is set (line 760). The only Start-Process uses RunAs and passes -NonInteractive plus the same Prepare -ConfirmDestructive command. An already-elevated process does not call Start-Process again. Restart-Computer -Force -Confirm:$false is the reboot. Register-FinalizerTask uses AtStartup, SYSTEM, ServiceAccount, Highest, and reads back SYSTEM, the executable, and the installed script argument. This review did not execute Plan, Status, Prepare, or Finalize.
Impact: The unattended design is present in source. It was not launched.

### A17 PASS

Surface: A
Claim: Pending and Running operator text requires source protection and exact BootExecute arming.
Evidence: Get-CurrentState lines 362-368 and Invoke-Finalize lines 984-1001 share that gate. The armed true branch says readback confirms F is read-only and BootExecute is armed. The false branch says preparation is incomplete or removes the unsafe entry and records Failed. Finalize initial detail says protection and BootExecute will be described only after both are read back.
Impact: Status and Finalize no longer tell the operator a boot copy is ready from registry status alone.

### B1 PASS

Surface: B
Claim: This review re-checked the artifacts and did not adopt the earlier verdicts.
Evidence: The four prior receipts were read for the rejected claims, then the current file, Rust sources, hashes, parser, AST, analyzer, confirmation metadata, geometry, PE, PDB symbol, and cargo fmt, clippy, test, and build were run in this session. Prior overall verdicts were DISAGREE. This verdict is from the current bytes.
Impact: Earlier receipts were attack input, not authority.

### B2 N/A

Surface: B
Claim: Byrd Development Process v4 phase order does not apply to this operator-directed lab migration.
Evidence: Classification is class 2. The brief says ViceSharp product requirements are N/A. No product FR/TR/TEST completion was claimed for this tooling.
Impact: Surface B does not fail the migration for lacking a Byrd phase gate.

### B3 PASS

Surface: B
Claim: MCP session state was written through the session-log tools, not by editing storage files.
Evidence: Session open and beginTurn used mcpserver sessionlog_open and sessionlog_begin_turn for GrokCode. No todo.yaml, session-log file, or requirements store was edited. Raw REST was not used.
Impact: The audit route matches the workspace marker and the Grok plugin contract.

### B4 PASS

Surface: B
Claim: Lab shell work used PowerShell.MCP only, with no Python.
Evidence: Commands ran through pwsh execute_command. PY_COUNT under E:\F-DevDrive-Migration, excluding target, is 0. JSON for this receipt is ConvertTo-Json from objects. No Bash, Node, or Python parser was used.
Impact: The PowerShell-only and no-Python rules hold for this review and for the migration tree that was inspected.

### B5 PASS

Surface: B
Claim: The review did not mutate migration state.
Evidence: Move-FDevDrive.ps1 was not invoked. No partition, volume, letter, registry, scheduled task, BootExecute, or reboot operation was performed. The only writes are this receipt pair and the MCP session-log turn. The cargo rebuild used an isolated target directory, and the pinned exe hash was the same afterward.
Impact: The safety boundary in the brief was kept.

### C1 N/A

Surface: C
Claim: ViceSharp functional and technical requirements do not govern this lab migration.
Evidence: Class 2 operator-directed migration. HANDOFF.md product plan PLAN-VSFLOCKSTEP-001 is unrelated and was not claimed complete by this tooling.
Impact: Missing ViceSharp FR/TR/TEST coverage is not a failure for this request.

### D1 PASS

Surface: D
Claim: The migration plan text matches the safe code order.
Evidence: Same evidence as A5. failureInvariant line 531 matches the pre-protection and post-protection split. Observed wording, not scored as a failure: line 524 says the finalizer script is hash-verified, while Install-MigrationPayload hashes only the exe; line 525 says the volume cannot be written, while the code reads back IsReadOnly rather than attempting a write; line 712 prints a prospective read-only detail before flush. The required order and the readback-before-return gate are present.
Impact: Plan no longer tells the operator the rejected unsafe order.

### D2 PASS

Surface: D
Claim: The twelve power-loss and rerun statements hold in source.
Evidence: Payload install does not create Pending or the task. Protect runs before registry and task registration and readback is required before return. Pending, finalizer readback, BootExecute readback, and reboot follow that return. Finalize makes no initial protection claim and independently reads both prerequisites. A missing prerequisite becomes non-repeating Failed with zero bytes and failure NTSTATUS, with no copy or cutover. Both true retains the retry. Power loss before Set-Partition creates no Pending task and does not modify F. No later path sets source IsReadOnly false. A gap after task and registry but before BootExecute is converted by Finalize. Rerun reuses a source-sized target, calls Protect again, overwrites Status with 0, replaces the task with -Force, and arms BootExecute. No delete command exists.
Impact: The reported power-loss window stays safe. The exception path is A6, and it now records Failed before it drops the task.

### D3 N/A

Surface: D
Claim: The ViceSharp product plan is not the definition of done for this migration.
Evidence: HANDOFF.md is a VICE snapshot and phase-exit handoff. It does not claim this Dev Drive migration step complete. The operator classified the work as class 2.
Impact: Product plan items that remain open do not fail this tooling review.

### D4 PASS

Surface: D
Claim: The failed claims in the four named receipts are closed in the current source, and their live-environment items were excluded rather than left unknown.
Evidence: 162750Z and 172424Z progress and invariant failures are closed by the current Completed/Current/Next fields and by failureInvariant line 531. 180849Z A18/D8 is A8. 184242Z A16/D9, A17/D10, and A19/D11 are A5, A6, and A7. The old U1 live Dev Drive query was not rerun because this brief forbids environment inspection.
Impact: No prior code defect named in those receipts remains open in this source.

