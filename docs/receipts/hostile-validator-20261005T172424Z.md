# Hostile validator receipt

TimestampUtc: 2026-10-05T17:24:24.1206315Z
ValidatorIdentity: GrokSubagentHostile
Model: grok-4.7. Effort: xhigh. Host: PAYTON-LEGION2.
Classification: Class 2 operator-directed lab migration. Surface C is N/A. Surface D uses the migration acceptance criteria, not the ViceSharp VSF plan.
Add-profile: executed. Directory markdown count: 22. Non-Grok files read: 21. Excluded: add-profile.grok.md. First file: add-profile.md.
Request JSONL: F:\GitHub\vice-sharp\docs\receipts\hv\20261005T170723Z-f-dev-drive-grok-4-7-xhigh-remediation.request.jsonl
Response JSONL: F:\GitHub\vice-sharp\docs\receipts\hv\20261005T170723Z-f-dev-drive-grok-4-7-xhigh-remediation.response.jsonl
SessionId: GrokCode-20261005T170945Z-fdev-hv
RequestId: req-20261005T170945Z-001-fdev-remediation-hv
Plugin: F:\GitHub\mcpserver-grok-plugin 1.119.0. Marker trust: Invoke-FullBootstrap signatureAndHealth true.
OverallVerdict: DISAGREE
AccuracyPercent: 98
CompletenessPercent: 98
CompletionPercent: 100
Eta: complete
Eta note: progress lines before this receipt used eta unknown because no measured finish time existed.
Row counts: PASS 37, FAIL 3, UNKNOWN 1, N/A 2
FailsafeUsed: false

## FAIL
- A18: Completed, current, and next on every boot and PowerShell user state
  Evidence: display_dashboard, display_operation_progress, both skip messages, display_failure, and the panic handler path include all three fields. from_completed_count(0) is the initial state, count 3 is tested, and usize::MAX caps at 7 completed phases with next None. Write-MigrationState always emits the three fields and the live timestamp sample is 2026-10-05T12:20:12.218-05:00. The Running restart preamble at main.rs lines 812-816 does not. Captured Plan and Status JSON also omit completed, current, and next.
- B1: Plan failure invariant overclaims read-only protection
  Evidence: Invoke-Plan emits: the original partition remains read-only after any failure. Protect-SourcePartition is the first source read-only write and it runs after preflight, E shrink, target creation, and payload install. The catch does not set source read-only. A failure before that call leaves F writable. The narrower claim that Prepare never makes the source writable is true and is A19.
- D10: Progress visibility in every boot and PowerShell state
  Evidence: Same gap as A18. The restart preamble and the captured Plan and Status documents omit Completed, Current, and Next.

## UNKNOWN
- U1: Elevated Dev Drive query
  Evidence: Non-elevated fsutil devdrv query F: exited 1 with Error 5 Access is denied. Elevation was forbidden. This does not prove or disprove the flag the elevated preflight checks.

## Score justification
Accuracy is 98 because every scored finding cites a command, file line, or captured output from this session. It is not 100 because the GPT GUID byte comparison relies on the Windows IOCTL layout contract rather than a raw sector dump.
Completeness is 98 because the orchestrator, lib.rs, main.rs, Cargo.toml, build.rs, and Cargo.lock were read in full and every required gate was rerun. It is not 100 because the elevated Dev Drive query was not run and the superseded runner scripts were identified rather than re-audited line by line.

## Commands
- Invoke-FullBootstrap on F:\GitHub\vice-sharp returned signatureAndHealth true at 2026-10-05T17:09:45.725Z.
- cargo fmt --manifest-path E:\F-DevDrive-Migration\FDevBootCopy\Cargo.toml --check with CARGO_TARGET_DIR C:\Users\kingd\AppData\Local\Temp\hv-fdev-cargo-20261005T170945Z. FMT_EXIT=0.
- cargo clippy --release --all-targets -- -D warnings. CLIPPY_EXIT=0.
- cargo test --release -- --test-threads=1. 19 passed; 0 failed; 0 ignored; doc-tests 0. TEST_EXIT=0.
- cargo build --release. BUILD_EXIT=0. Pinned hash unchanged.
- Parser.ParseFile, Invoke-ScriptAnalyzer 1.25.0, Get-Command metadata, and a no-prompt ShouldProcess probe.
- Child pwsh -File Move-FDevDrive.ps1 -Mode Prepare, Plan, and Status. Exits 1, 0, 0. Snapshot identical.
- fsutil devdrv query F: as the current non-admin user. Exit 1. Error 5.

## All findings
### A1 PASS
Orchestrator hash, length, and full read

Get-FileHash SHA256 5DBB88DA17757A62BB17D6BFC4FA89054F3630B44C92F3F51EB763AA7774C979. Length 45849. Lines 994. All lines read.

### A2 PASS
Pinned boot binary and script pin

Pinned SHA256 C655F76C0724C44DC2EA91DB7E6A3E3C03FABA35D6E4958FD45F11477A180D21. Length 13312. Script ExpectedBinarySha256 at line 48 matches. Isolated cargo build left the pinned hash unchanged.

### A3 PASS
All Rust and build inputs read

Cargo.toml 21 lines, build.rs 6 lines, Cargo.lock 8 lines, src/lib.rs 731 lines, src/main.rs 1047 lines. No .cargo directory and no separate linker script. Link arguments are only in build.rs.

### A4 PASS
cargo fmt --check

cargo 1.99.0, rustc 1.99.0, isolated CARGO_TARGET_DIR, FMT_EXIT=0.

### A5 PASS
cargo clippy -D warnings

CLIPPY_EXIT=0. Output was Compiling and Finished only. The host counted cargo stderr progress lines; no warning text was present.

### A6 PASS
cargo test --release

19 passed, 0 failed, 0 ignored, 0 measured. Doc-tests running 0, 0 passed, 0 failed, 0 ignored. TEST_EXIT=0.

### A7 PASS
cargo build --release

BUILD_EXIT=0. Fresh isolated binary SHA256 D74DDE16188AD2467CC880240D3ABB0A4D929A68C8FA6FE22DA230D98AF30E4F, length 13312. .text bytes identical. Differences are COFF timestamp, checksum, debug timestamps, and the RSDS GUID.

### A8 PASS
PE headers and imports

Machine 0x8664. Magic 0x020B. Subsystem 1. Entry RVA 0x00001000. CLR RVA 0 size 0. Delay import RVA 0 size 0. Import count 1: ntdll.dll.

### A9 PASS
Parser and PSScriptAnalyzer

Parse errors 0. PSScriptAnalyzer 1.25.0: Error 0, Warning 0, Information 46, all PSAvoidUsingPositionalParameters.

### A10 PASS
AST destructive-command search

FunctionDefinitionAst count 38. Exact command counts for Remove-Partition, Clear-Disk, Initialize-Disk, Format-Volume, and Remove-Item are 0. Remove-Partition text hits are the Remove-PartitionAccessPath name. No Invoke-Expression, diskpart, CIM/WMI, cmd.exe, python, bash, Read-Host, or null-name dynamic commands.

### A11 PASS
Unattended confirmation contract

ConfirmPreference is set to None at line 12 and remains None after the line 735 gate. New-Partition and Register-ScheduledTask do not expose Confirm. Every other discovered mutating command that exposes Confirm passes -Confirm:$false, including all 10 Set-Partition calls. A local High-impact ShouldProcess call with ConfirmPreference None returned True and did not prompt. Live partitions were not mutated.

### A12 PASS
Prepare order

After the confirm gate and elevation, the order is Assert-Preflight, Initialize-TargetClonePartition, Install-MigrationPayload, Protect-SourcePartition, Add-BootImage readback, Restart-Computer -Force. Enable-MigrationLogging may create the ProgramData directory before Assert-Preflight. That is not a disk or BootExecute mutation.

### A13 PASS
Finalizer order

Status 0 and 1 return before Remove-BootImage. Terminal results remove BootExecute. Status 3 calls Restore-SourceReadOnlyMapping. Any other status throws. Status 2 requires VerifiedBytes 210749095936 and LastNtStatus 0 before Invoke-VerifiedCutover. Cutover unmaps F, keeps the source read-only, takes it offline, mounts the target, expands to 421498191872, scans, checks health, label, and Dev Drive, assigns F, verifies the old partition, then unregisters the task.

### A14 PASS
No-confirm Prepare, Plan, and Status snapshot

Child pwsh -File without -ConfirmDestructive: Prepare exit 1 at line 736 before mutation. Plan exit 0. Status exit 0. Canonical before and after snapshot SHA256 both AFB1A6F8143A7FA1F0E44AF3876D75CA5CA25782F69B3E8ACF065D9EC6786F1B.

### A15 PASS
Live unarmed disk state

Disk 0 partitions are 1=D, 2=E size 1718491576832, 3=F size 210749095936, IsReadOnly false, IsOffline false. No partition 4. BootExecute is only autocheck autochk *. FDevMigration registry, task, System32 binary, and ProgramData script are absent. Task query count 0. A superseded file remains at C:\ProgramData\ViceSharp\Migration\Run-FDevDriveMigrationAtBoot.ps1 and is not scheduled.

### A16 PASS
Pinned geometry and free-space reserve

Prepared E end equals target offset. Target final end equals source offset. Final size is exact double. Source size is 4096-aligned. The final copy chunk is 2097152 bytes, also 4096-aligned. E free after shrink is 171639791616, above 137438953472. Live disk size, offsets, and GUIDs match the pins. Disk 0 is PHYSICALDRIVE0.

### A17 PASS
Rust boot copy behavior

decide_run maps 0/1/2/3 and rejects unknown. Layout requires distinct GUIDs, exact sizes, source GPT read-only bit 60, and destination writable. copy_blocks starts at offset 0 and writes every byte. flush_destination is explicit and precedes verify. verify_blocks rereads both sides and returns before any later flush on mismatch. Verified is written only after both byte counts match. Terminal decisions return before NativeDevice open. Running clears VerifiedBytes and copies from zero.

### A18 FAIL
Completed, current, and next on every boot and PowerShell user state

display_dashboard, display_operation_progress, both skip messages, display_failure, and the panic handler path include all three fields. from_completed_count(0) is the initial state, count 3 is tested, and usize::MAX caps at 7 completed phases with next None. Write-MigrationState always emits the three fields and the live timestamp sample is 2026-10-05T12:20:12.218-05:00. The Running restart preamble at main.rs lines 812-816 does not. Captured Plan and Status JSON also omit completed, current, and next.

### A19 PASS
Failure safety without source deletion

Prepare catch removes BootExecute only if armed and unregisters the task. It never clears source read-only. Native failure records Failed, and the finalizer removes BootExecute and restores the original F mapping read-only. Cutover catch calls the same restore. No delete-partition or format command exists. E shrink is not rolled back. That is outside the stated safety claim.

### A20 PASS
One UAC then unattended design

The only Start-Process is Invoke-SelfElevation with -Verb RunAs. The child arguments are -NoLogo -NoProfile -NonInteractive -EncodedCommand for -Mode Prepare -ConfirmDestructive. ConfirmPreference None, -Confirm:$false, and the SYSTEM AtStartup task are present. This path was not executed live, which claim 21 also states.

### A21 PASS
Corrected historical statement

LastBootUpTime is 2026-10-05T06:54:34.5-05:00. The 06:55:35 transcript ran the superseded ProgramData ViceSharp runner and failed on Assert-NoReparsePoints at the old script line 542. The current 994-line script has zero Assert-NoReparsePoints and zero Invoke-Execute matches, and its LastWriteTimeUtc is 2026-10-05T16:54:49Z. Current boot state is unarmed. The superseded attempt is not a failure of this corrected statement.

### A22 PASS
No source deletion and no authorized destructive run

This review did not pass -ConfirmDestructive, elevate, resize, create, or delete partitions, change letters, arm BootExecute, reboot, or delete files.

### B1 FAIL
Plan failure invariant overclaims read-only protection

Invoke-Plan emits: the original partition remains read-only after any failure. Protect-SourcePartition is the first source read-only write and it runs after preflight, E shrink, target creation, and payload install. The catch does not set source read-only. A failure before that call leaves F writable. The narrower claim that Prepare never makes the source writable is true and is A19.

### B2 PASS
Receipts were rechecked

Hashes, gates, PE, AST, analyzer, geometry, and the three read-only modes were rerun in this session. The prior 20261005T162750Z receipt was not inherited.

### B3 PASS
PowerShell only and no Python

All commands ran through PowerShell.MCP on PAYTON-LEGION2. No python, py, or .py file is in the migration tree. JSON was produced with ConvertTo-Json.

### B4 PASS
Source protection and no deletion

Same evidence as A10, A19, and A22.

### B5 PASS
Approval gate before mutation

Invoke-Prepare throws at line 736 unless -ConfirmDestructive is set. The observed child exit was 1 and the snapshot was identical.

### B6 PASS
Sortable local timestamps

Get-LocalTimestamp uses yyyy-MM-ddTHH:mm:ss.fffK. Observed Plan timestamp 2026-10-05T12:20:12.218-05:00.

### B7 PASS
Local execution

Host PAYTON-LEGION2. No web search, cloud workspace, hosted agent, or second hostile validator was used.

### B8 PASS
MCP session storage

Session opened and the turn begun through sessionlog_open and sessionlog_begin_turn. No session, TODO, or requirements storage file was edited.

### B9 N/A
Byrd v4

Class 2 operator lab migration. Byrd phase gates do not apply to the migration action.

### C1 N/A
ViceSharp requirements

No ViceSharp product behavior was changed or claimed. HANDOFF VSF work is a different plan. Surface C is N/A.

### D1 PASS
One UAC and unattended design

Same evidence as A11 and A20.

### D2 PASS
Source read-only before BootExecute

Protect-SourcePartition verifies IsReadOnly, DriveLetter F, online, and NoDefaultDriveLetter before Add-BootImage.

### D3 PASS
Raw full copy

copy_blocks reads and writes from offset 0 through EXPECTED_SOURCE_BYTES 210749095936.

### D4 PASS
Explicit destination flush

NtFlushBuffersFile runs after the copy and before verification.

### D5 PASS
Independent full reread

verify_blocks rereads source and destination and compares every byte. A mismatch test shows flush_count 0.

### D6 PASS
Safe failure behavior

Same behavior evidence as A19. The false universal sentence is B1, not a second behavior failure.

### D7 PASS
Verified-only cutover

Invoke-Finalize checks status 2, exact verified bytes, and NTSTATUS 0 before unmapping F.

### D8 PASS
Old source preserved

Cutover sets the old partition read-only, offline, and unmapped. It does not delete it.

### D9 PASS
Target final size is exact double

421498191872 equals 210749095936 times 2, and the final end equals the source offset.

### D10 FAIL
Progress visibility in every boot and PowerShell state

Same gap as A18. The restart preamble and the captured Plan and Status documents omit Completed, Current, and Next.

### U1 UNKNOWN
Elevated Dev Drive query

Non-elevated fsutil devdrv query F: exited 1 with Error 5 Access is denied. Elevation was forbidden. This does not prove or disprove the flag the elevated preflight checks.

