# Hostile validator receipt

TimestampUtc: 2026-10-05T18:08:49.0000000Z
ValidatorIdentity: GrokSubagentHostile
Model: grok-4.7. Effort: xhigh.
Classification: Class 2 operator-directed lab migration. Surface C is N/A. Surface D uses the migration acceptance criteria. The ViceSharp VSF plan is a different plan and is N/A.
Add-profile: executed. Directory markdown count: 23. Non-Grok files read: 22. Excluded: add-profile.grok.md. First file: add-profile.md, read through PowerShell.MCP before the other profile files.
Request JSONL: F:\GitHub\vice-sharp\docs\receipts\hv\20261005T174728Z-f-dev-drive-grok-4-7-xhigh-code-only.request.jsonl
Response JSONL: F:\GitHub\vice-sharp\docs\receipts\hv\20261005T174728Z-f-dev-drive-grok-4-7-xhigh-code-only.response.jsonl
SessionId: GrokCode-20261005T175344Z-f-dev-drive-hv
RequestId: req-20261005T175344Z-001-f-dev-drive-code-only
TurnId: 97729
OverallVerdict: DISAGREE
AccuracyPercent: 99
CompletenessPercent: 99
CompletionPercent: 100
Eta: complete
Counts: PASS 33, FAIL 2, UNKNOWN 0, N/A 4
FailsafeUsed: false
FailsafePath: null

## FAIL
- A18: Finalize tells the operator that original F remains read-only for Pending and Running before that protection is established.
  Evidence: Initialize-MigrationRegistry line 645 writes Status 0 and Register-FinalizerTask line 669 registers the SYSTEM AtStartup task inside Install-MigrationPayload. Invoke-Prepare calls that payload install before Protect-SourcePartition. Invoke-Finalize line 945 always records that original F remains read-only. Lines 955-957 return for status 0 or 1 and record the same sentence, plus a next action to leave BootExecute armed. Neither statement checks IsReadOnly or Test-BootImageArmed. A power loss after payload install and before Protect-SourcePartition or Add-BootImage boots into Finalize while F is still writable and BootExecute is not armed. The function then returns success without cutover.
  Impact: The log and task result describe a protected waiting copy. The copy is not armed, F is still writable, and the startup task will repeat the same successful no-op. Data is not deleted or switched.
  Remediation: Write Status Pending and register the startup task only after the source read-only readback succeeds, or make both details conditional on the live source IsReadOnly flag and the BootExecute readback.
- D8: Progress text for a pre-protection Pending state is not truthful.
  Evidence: Same defect and evidence as A18. This is the migration acceptance failure for progress-message truthfulness and the power-loss window. It is one defect scored on surfaces A and D.
  Impact: Same impact as A18.
  Remediation: Same remediation as A18.

## UNKNOWN
- None. Live environment state was excluded by the operator and is N/A, not UNKNOWN.

## Score justification
Accuracy is 99 because every finding cites a command result or a line read in this session. It is not 100 because the IOCTL field placement is the calculated PARTITION_INFORMATION_EX layout, with length at offset 16, partition id at 48, attributes at 64, and a 144-byte buffer, without a local SDK header. Completeness is 99 because the orchestrator, lib.rs, main.rs, Cargo.toml, Cargo.lock, and build.rs were read in full and every allowed gate was rerun. It is not 100 because the native boot entry was not executed, which is the accepted limit for this no_std BootExecute image.

## Commands
- Get-FileHash and ReadAllLines for the orchestrator, pinned exe, both prior receipts, AGENTS.md, and HANDOFF.md.
- cargo fmt --manifest-path E:\F-DevDrive-Migration\FDevBootCopy\Cargo.toml --check. Exit 0.
- cargo clippy --manifest-path E:\F-DevDrive-Migration\FDevBootCopy\Cargo.toml --release --all-targets -- -D warnings, with isolated CARGO_TARGET_DIR. Exit 0.
- cargo test --manifest-path E:\F-DevDrive-Migration\FDevBootCopy\Cargo.toml --release -- --test-threads=1. 19 passed, 0 failed, 0 ignored. Doc-tests 0. Exit 0.
- cargo build --manifest-path E:\F-DevDrive-Migration\FDevBootCopy\Cargo.toml --release. Exit 0. Pinned hash unchanged. Isolated .text hash equal.
- Parser.ParseFile and Invoke-ScriptAnalyzer 1.25.0. Parse errors 0. Warning and error findings 0. Information 46, all PSAvoidUsingPositionalParameters.
- AST command inventory, confirmation-parameter check, and static order indexes.
- Manual PE header and import-directory parse of the pinned and isolated binaries.
- uint64 geometry arithmetic for the pinned constants.
- pwsh -NoLogo -NoProfile -NonInteractive confirmation probe. Result True, exit 0.
- Native LASTEXITCODE probe using cmd.exe exit 7, 9, and 4. Pipeline assignment preserved 7, Out-Host preserved 9, and the direct call preserved 4. The fsutil checks are not masked by Out-Host or Out-String.

## All findings
### A1 PASS
Move-FDevDrive.ps1 matches the expected hash, length, and line count.

Evidence: Get-FileHash SHA256 0AC489D10A9ADF45D8A769ACF348D38300CB65457158E2B15B573C2FC2412F84. Length 47176. ReadAllLines count 1013. LastWriteTimeUtc 2026-10-05T17:34:22.5065526Z. The full file was read from line 1 through line 1013.

### A2 PASS
The pinned boot binary matches the expected hash and size, and the script pin matches that hash.

Evidence: Get-FileHash of E:\F-DevDrive-Migration\FDevBootCopy\target\release\fdev-boot-copy.exe is A0B951D9A828F167582628C584FE620101D08D726F3ADC3091B47D34C0958AB5. Length 13312. Move-FDevDrive.ps1 line 48 ExpectedBinarySha256 is that same value.

### A3 PASS
The Rust project inputs were read in full and there is no separate linker config.

Evidence: Cargo.toml is 26 lines, Cargo.lock is 8 lines, build.rs is 6 lines, src\lib.rs ends at line 731, and src\main.rs ends at line 1047. A forced directory listing excluding target and .mcpServer returned only those five files. build.rs is the only linker input and passes /SUBSYSTEM:NATIVE, /ENTRY:NtProcessStartup, /NODEFAULTLIB, and ntdll.lib.

### A4 PASS
cargo fmt --check exits 0.

Evidence: cargo 1.99.0 (5f94df478 2026-08-27), rustc 1.99.0 (b940084d7 2026-09-28). Command: cargo fmt --manifest-path E:\F-DevDrive-Migration\FDevBootCopy\Cargo.toml --check. FMT_EXIT=0. Output empty.

### A5 PASS
cargo clippy --release --all-targets -- -D warnings exits 0.

Evidence: CARGO_TARGET_DIR C:\Users\kingd\AppData\Local\Temp\hv-fdev-cargo-20261005T175344Z. Output was Compiling fdev-boot-copy and Finished release profile in 1.21s. CLIPPY_EXIT=0. No warning text.

### A6 PASS
cargo test --release passes 19, fails 0, ignores 0, and doc tests run 0.

Evidence: Same isolated CARGO_TARGET_DIR. cargo test --release -- --test-threads=1. Result line: 19 passed; 0 failed; 0 ignored; 0 measured; 0 filtered out. Doc-tests fdev_boot_copy running 0 tests; 0 passed; 0 failed; 0 ignored. TEST_EXIT=0.

### A7 PASS
cargo build --release exits 0 and does not change the pinned binary.

Evidence: BUILD_EXIT=0. Pinned hash after the build remained A0B951D9A828F167582628C584FE620101D08D726F3ADC3091B47D34C0958AB5 and length 13312. The isolated rebuild hash is 0785B832EF22A9E3D7F07AB89BC983AC108B78F84F9EAEBD882E0FB6B94B451A, also 13312 bytes. Section .text SHA256 is identical: DBF9B7F839A9BE061D67B5538801DE61CDAB20C2413BABBA68E1CD279A4E41BE, 7680 bytes. COFF timestamps differ: pinned 1791221649, fresh 1791223334.

### A8 PASS
The pinned PE is AMD64, native, entry RVA 0x1000, with no CLR descriptor and only ntdll.dll imported.

Evidence: Manual PE parse of the pinned file: Machine 0x8664, Magic 0x020B, Subsystem 1, AddressOfEntryPoint 0x00001000, data-directory count 16, import RVA 15920 size 40, delay-import RVA 0 size 0, CLR RVA 0 size 0, import DLL list ntdll.dll. Import size 40 is one 20-byte descriptor plus the null descriptor. build.rs sets /ENTRY:NtProcessStartup and main.rs defines the no_mangle NtProcessStartup entry. The ASCII name is not stored in the image.

### A9 PASS
The PowerShell parser reports 0 errors and PSScriptAnalyzer reports 0 warning or error findings.

Evidence: Parser.ParseFile error count 0. PSScriptAnalyzer 1.25.0 severity grouping is Information=46 only. Rule grouping is PSAvoidUsingPositionalParameters=46. WarningError list empty.

### A10 PASS
Function count is 38 and the forbidden destructive commands are absent.

Evidence: FunctionDefinitionAst count 38, names checked against the file. CommandAst exact counts: Remove-Partition 0, Clear-Disk 0, Initialize-Disk 0, Format-Volume 0, Remove-Item 0. Dynamic command-name count 0. No Invoke-Expression, diskpart, cmd, python, bash, or CIM write command appears in the command-name inventory. Remove-PartitionAccessPath count is 5 and is an access-path removal, not Remove-Partition.

### A11 PASS
After the explicit confirmation gate, the elevated child is NonInteractive and installed confirmation-capable mutating calls pass -Confirm:$false.

Evidence: ConfirmPreference is set to None at line 12. The only Start-Process is line 529 with -Verb RunAs and -Confirm:$false. Its argument list includes -NonInteractive and an encoded Prepare -ConfirmDestructive command. NonInteractive literal count is 2: the child arguments and the SYSTEM task arguments at line 663. AST violations for installed mutating commands that expose Confirm and do not pass -Confirm:$false: empty. All 10 Set-Partition calls pass -Confirm:$false. Get-Command shows New-Partition and Register-ScheduledTask have no Confirm parameter and ConfirmImpact None. A separate pwsh -NonInteractive probe with ConfirmPreference None and a local ConfirmImpact High ShouldProcess returned PROBE_RESULT=True, exit 0, and did not prompt. Remove-BootImage and Remove-DriveAccessPath use ShouldProcess without -Confirm:$false, but their inner New-ItemProperty and Remove-PartitionAccessPath calls pass -Confirm:$false, and the probe shows the preference suppresses the prompt.

### A12 PASS
Prepare static order is preflight, hidden target, payload, source protection, BootExecute readback, then reboot.

Evidence: Invoke-Prepare throws at line 755 unless -ConfirmDestructive is set, then calls Invoke-SelfElevation only when not already elevated. Try-block indexes: Assert-Preflight 301, Initialize-TargetClonePartition 389, Install-MigrationPayload 429, Protect-SourcePartition 470, Add-BootImage 803, Test-BootImageArmed 863, Restart-Computer 1322. Install-MigrationPayload checks the installed SHA-256 before Initialize-MigrationRegistry and Register-FinalizerTask. Protect-SourcePartition flushes, dismounts, sets read-only, then reads back IsReadOnly, online, drive letter F, and NoDefaultDriveLetter. Restart-Computer -Force -Confirm:$false is after the readback. Enable-MigrationLogging may create the ProgramData directory before preflight and does not change disk layout or BootExecute.
Impact: No disk mutation was executed.

### A13 PASS
Finalizer control flow matches the required status gates.

Evidence: Invoke-Finalize lines 955-982: status 0 or 1 returns before Remove-BootImage. Remove-BootImage then runs for every remaining status. Status 3 calls Restore-SourceReadOnlyMapping and unregisters the task. Any status other than 2 throws. Status 2 requires VerifiedBytes equal to 210749095936 and LastNtStatus 0 before Invoke-VerifiedCutover. The cutover catch calls Restore-SourceReadOnlyMapping. The first string index of Restore is the status-3 path, which is why a naive single-index order check is false; the source order above was read directly.

### A14 PASS
The design is one UAC, then an unattended native boot program and a SYSTEM finalizer.

Evidence: One RunAs boundary at line 529. Child flags include -NonInteractive. Boot image subsystem is native. Register-FinalizerTask uses New-ScheduledTaskTrigger -AtStartup and New-ScheduledTaskPrincipal -UserId SYSTEM -LogonType ServiceAccount -RunLevel Highest. The task is not an interactive logon trigger. Automatic reboot is only Restart-Computer after Protect-SourcePartition returns and BootExecute readback succeeds.

### A15 PASS
The Rust boot path validates identity and geometry, copies every byte from offset zero, flushes, rereads both sides, and records Verified only after success.

Evidence: decide_run maps 0, 1, 2, and 3 and returns Err for any other value. validate_migration_layout requires the configured GUIDs, distinct identities, exact byte counts, source GPT bit 60, and destination not read-only. partition_facts rejects a non-GPT style, a short buffer, and a zero length. copy_blocks starts at offset 0. Native read_exact_at and write_all_at reject a short transfer. flush_destination calls NtFlushBuffersFile before verify_blocks. A mismatch returns before the Verified registry writes at main.rs lines 950-961. Running and Start clear VerifiedBytes and set status Running before opening devices. SkipVerified and SkipFailed return before NativeDevice::open. Unknown status returns before any device open. Tests cover the pure functions, including mismatch with flush_count 0.

### A16 PASS
Every user-visible Rust state includes Completed, Current, and Next.

Evidence: display_dashboard lines 280-300, display_operation_progress lines 330-364, and display_failure lines 367-388 all emit the three labels. display_failure uses Progress::from_completed_count on LAST_COMPLETED_PHASES and prints Nothing yet when the replay is empty. Skip verified is lines 801-804, skip failed is lines 807-809, and the Running restart preamble is lines 812-816. The panic handler calls display_failure. Initial, copy, flush, and verify states go through display_dashboard or display_operation_progress. from_completed_count caps at 7 via min(7); the zero count performs no advances. Tests cover count 3, usize::MAX, and the initial Progress::new state.

### A17 PASS
Plan and Status JSON include Completed, Current, and Next, and the Plan failure sentence matches the phase split.

Evidence: Get-CurrentState lines 367-372 and Invoke-Plan lines 486-493 always emit completed, current, and next. Status next is selected from NotConfigured, Pending, Running, Verified, Failed, and unknown. Plan line 519 says failures before source protection leave F unchanged, and that after read-only protection succeeds later failures preserve or restore read-only F and never delete the old partition. Prepare catch lines 787-806 remove BootExecute only if armed and unregister the task. It never calls Set-Partition on the source and never clears read-only. Protect-SourcePartition is the first source read-only write and it is after preflight, target creation, and payload install.

### A18 FAIL
Finalize tells the operator that original F remains read-only for Pending and Running before that protection is established.

Evidence: Initialize-MigrationRegistry line 645 writes Status 0 and Register-FinalizerTask line 669 registers the SYSTEM AtStartup task inside Install-MigrationPayload. Invoke-Prepare calls that payload install before Protect-SourcePartition. Invoke-Finalize line 945 always records that original F remains read-only. Lines 955-957 return for status 0 or 1 and record the same sentence, plus a next action to leave BootExecute armed. Neither statement checks IsReadOnly or Test-BootImageArmed. A power loss after payload install and before Protect-SourcePartition or Add-BootImage boots into Finalize while F is still writable and BootExecute is not armed. The function then returns success without cutover.
Impact: The log and task result describe a protected waiting copy. The copy is not armed, F is still writable, and the startup task will repeat the same successful no-op. Data is not deleted or switched.
Remediation: Write Status Pending and register the startup task only after the source read-only readback succeeds, or make both details conditional on the live source IsReadOnly flag and the BootExecute readback.

### A19 PASS
Failure paths do not delete the old source or make it writable.

Evidence: The five deletion and format commands have AST count 0. Prepare catch does not clear source read-only. Restore-SourceReadOnlyMapping forces the original partition read-only and remaps F. Cutover sets the old partition read-only, offline, and unmapped. The native program opens the source read-only and never writes it.

### A20 PASS
Pinned geometry is internally consistent and the copy chunks stay sector aligned.

Evidence: E offset plus prepared E size equals target offset 1368151556096. Target offset plus final size equals source offset 1789649747968. Final size 421498191872 equals source size 210749095936 times 2. Source end is 2000398843904, leaving 90112 bytes of disk slack inside disk size 2000398934016. Shrink bytes 350630476288 cover the clone. Source, chunk, and final sizes are divisible by 4096. Source size modulo 8388608 is 2097152, so the final chunk is 2097152 bytes and still 4096-aligned.

### A21 PASS
The code defects in the two rejected receipts are closed, and the excluded environment finding was not reopened.

Evidence: Receipt 20261005T162750Z failed display_failure for omitting Completed. display_failure now prints it. Receipt 20261005T172424Z failed the Running preamble at the old main.rs lines 812-816 and Plan/Status JSON. Those three fields are now present at the restart preamble and in both JSON writers. That receipt also failed the universal read-only Plan sentence. Line 519 now distinguishes pre-protection and post-protection failures, and the catch matches that split. The earlier missing -Confirm:$false coverage is closed by A11. The live disk, boot, and Dev Drive UNKNOWN from those receipts is outside this review by the operator scope correction and is not scored as UNKNOWN.

### A22 PASS
The unit tests exercise the pure logic, and the untested native orchestration is acceptable to inspect statically.

Evidence: Nineteen lib tests cover progress replay, transfer failure, flush failure, mismatch without flush, geometry rejection, decide_run, and layout rejection. Cargo.toml sets the binary test flag false, so main.rs is not executed as a test. The boot entry, IOCTL parsing, and registry checkpoint order were read line by line. That is the practical proof for a no_std native BootExecute image. Gaps that do not change the verdict: from_completed_count(0) is implied by the empty loop and the initial-state test; DuplicatePartitionIdentity is present but the identity test returns earlier on DestinationIdentityMismatch; SourceLengthMismatch has no direct test.
Impact: No product defect by itself.
Remediation: Add direct tests for the zero replay, duplicate-id, and source-length branches if the pure helpers change.

### B1 PASS
This review re-verified the artifacts and did not inherit the earlier receipts.

Evidence: Hashes, cargo gates, parser, analyzer, AST, PE parse, geometry arithmetic, and the confirmation probe were run in this session. The two older receipts were read and then checked against the current files.

### B2 PASS
Receipts exist for the gates this review was allowed to run.

Evidence: Commands and outputs are in this receipt. The migration script was not executed because the operator forbade Plan, Status, Prepare, and Finalize runs.

### B3 PASS
Shell work used PowerShell.MCP only, with no Python and no handwritten JSON.

Evidence: All commands went through pwsh execute_command. cargo, the parser, and the PE parser ran under that session. No python, py, or bash invocation was used. JSON receipts are ConvertTo-Json output.

### B4 PASS
Session state is going through MCP tools rather than storage files.

Evidence: sessionlog_open created GrokCode-20261005T175344Z-f-dev-drive-hv. sessionlog_begin_turn created req-20261005T175344Z-001-f-dev-drive-code-only with turnId 97729. No todo.yaml, session-log file, or requirements store was edited.

### B5 PASS
The review stayed inside the read-only safety boundary.

Evidence: No migration mode was launched. No elevation prompt was requested. No partition, registry, BootExecute, task, or reboot operation was performed. The cargo rebuild used an isolated CARGO_TARGET_DIR. The pinned binary hash was checked after the build.

### B6 N/A
Byrd Development Process v4 does not apply to this lab migration.

Evidence: The operator classified the work as class 2. It does not change ViceSharp emulator behavior, and no product phase completion was claimed.

### C1 N/A
ViceSharp functional and technical requirements do not apply.

Evidence: HANDOFF.md is the VSF and parity continuity record for the emulator. This migration does not claim those requirements or change product code.

### D0 N/A
The ViceSharp product plan is not the definition of done for this migration.

Evidence: HANDOFF.md points at PLAN-VSFLOCKSTEP-001 and the older parity gate. The operator said those product requirements are N/A. The migration acceptance criteria are D1 through D8.

### D1 PASS
One explicit confirmation, one UAC, and no later confirmation prompt are present in source.

Evidence: Same evidence as A11 and A14. Prepare returns before elevation when -ConfirmDestructive is absent.

### D2 PASS
Source protection and BootExecute readback happen before the automatic reboot.

Evidence: Same order evidence as A12. Add-BootImage also verifies exactly one BootExecute entry before the later Test-BootImageArmed check.

### D3 PASS
The copy starts at byte zero and an interrupted Running state restarts from byte zero.

Evidence: copy_blocks initializes offset to 0. run sets VerifiedBytes to 0 and status Running before execute_migration for both Start and Restart. Restart text says it restarts from byte zero.

### D4 PASS
Destination flush and the independent full compare happen before any Verified checkpoint.

Evidence: execute_migration calls flush_destination and then verify_blocks. Registry status Verified is written only after both byte-count checks. The mismatch test expects flush_count 0 because verification does not flush.

### D5 PASS
Only a verified byte count and NTSTATUS 0 can cut over, and failure restores read-only F.

Evidence: Same control flow as A13. Pending and Running do not cut over. Unknown status throws after removing BootExecute and does not call cutover.

### D6 PASS
The old source is preserved and is not deleted.

Evidence: Cutover verification requires the old partition to be read-only, offline, and without a drive-letter access path. No delete-partition command exists.

### D7 PASS
The target partition is expanded to exactly twice the source size.

Evidence: TargetFinalBytes is 421498191872 and the cutover asserts that exact size. The ReFS volume check allows the volume to be up to 67108864 bytes smaller than the partition, which still rejects an unexpanded source-sized volume. The partition assertion is exact.

### D8 FAIL
Progress text for a pre-protection Pending state is not truthful.

Evidence: Same defect and evidence as A18. This is the migration acceptance failure for progress-message truthfulness and the power-loss window. It is one defect scored on surfaces A and D.
Impact: Same impact as A18.
Remediation: Same remediation as A18.

### E1 N/A
Live environment preconditions are outside this review.

Evidence: The operator forbade inspection or reporting of computer name, disk layout, free space, Dev Drive flag, partitions, BootExecute, migration registry, scheduled tasks, installed payloads, boot time, UAC history, and reboot history. No Move-FDevDrive.ps1 mode was run. Runtime fail-closed behavior was reviewed only as source.

