# Hostile validator receipt

TimestampUtc: 20261005T162750Z
ValidatorIdentity: GrokSubagentHostile
Model: grok-4.7. Effort: xhigh. Host: PAYTON-LEGION2.
Classification: Class 2 operator-directed lab migration. Surface C is N/A. Surface D uses the migration acceptance criteria, not the ViceSharp VSF plan.
Add-profile: executed. Directory markdown count: 23. Non-Grok files read: 22. Excluded: add-profile.grok.md.
Request JSONL: F:\GitHub\vice-sharp\docs\receipts\hv\20261005T160826Z-f-dev-drive-grok-4-7-xhigh.request.jsonl
Response JSONL: F:\GitHub\vice-sharp\docs\receipts\hv\20261005T160826Z-f-dev-drive-grok-4-7-xhigh.response.jsonl
SessionId: GrokCode-20261005T160943Z-plugin-session
RequestId: req-20261005T161228Z-001-hostile-validate-f-dev-drive
Plugin wrapper: C:\Users\kingd\.codex\plugins\cache\mcpserver-codex-plugin\mcpserver\1.119.0\lib\Invoke-McpPlugin.ps1
PluginRoot override: F:\GitHub\mcpserver-grok-plugin
OverallVerdict: DISAGREE
AccuracyPercent: 84
CompletenessPercent: 97
CompletionPercent: 100
Eta: complete
Row counts: PASS 18, FAIL 4, UNKNOWN 2, N/A 2
Implementer claims A1-A19: PASS 16, FAIL 2, UNKNOWN 1.
FailsafeUsed: false

## FAIL
- A16: Every boot console and user-facing state shows completed, current, and next.
  Evidence: PowerShell timestamps match yyyy-MM-ddTHH:mm:ss.fffK, observed 2026-10-05T11:19:53.774-05:00. display_failure omits Completed. The dashboard, progress line, skip messages, and PowerShell state include all three fields.
- A19: No live Prepare, UAC, disk mutation, BootExecute arm, or reboot has occurred yet.
  Evidence: Current disk and BootExecute are unarmed. Last boot 2026-10-05T06:54:34.500-05:00. Superseded SYSTEM runner started 20261005065535 and failed at 06:58:13. A reboot and a migration execution already occurred.
- B1: Honesty about that reboot.
  Evidence: Same evidence as A19.
- D3: Progress visibility.
  Evidence: Same gap as A16.

## UNKNOWN
- A18: One UAC then fully unattended completion.
  Evidence: One RunAs, NonInteractive child, Restart-Computer -Force, and a SYSTEM startup task are in the script. Set-Partition ConfirmImpact is High and -Confirm false is absent. Real-partition confirmation was not executed.
- D1: One UAC and unattended completion.
  Evidence: Same open point as A18.

## All findings
### A1 PASS
Move-FDevDrive.ps1 SHA-256 is 79F5BD2D216F3566CD53BDDBF819C70DF683579660FA63B1F306BE2A959935EA.

Get-FileHash matched. Length 45181. Lines 993. LastWriteTime 2026-10-05T10:50:22.519-05:00.

### A2 PASS
Boot exe SHA-256 FE58816EB2325F8D8C75A83CFACA6337C4518C2E50DAC935E720E185B0967BCF and size 13312.

Pinned file matched. Isolated rebuild hash 7D0EBE873937289E9A3545B51BF15E72CB9A99833E405A702898C741D998C07C, same length, COFF timestamp 1791217406 versus pinned 1791215396. Pinned file was not overwritten.

### A3 PASS
cargo fmt --check exits 0.

cargo fmt --manifest-path E:\F-DevDrive-Migration\FDevBootCopy\Cargo.toml --check. FMT_EXIT=0. cargo 1.99.0, rustc 1.99.0.

### A4 PASS
cargo clippy --release --all-targets -- -D warnings exits 0.

CLIPPY_EXIT=0. Isolated CARGO_TARGET_DIR.

### A5 PASS
cargo test --release is 17 passed, 0 failed, 0 ignored, and doc tests 0.

17 passed; 0 failed; 0 ignored. Doc-tests running 0 tests; 0 passed; 0 failed; 0 ignored. TEST_EXIT=0.

### A6 PASS
cargo build --release exits 0.

BUILD_EXIT=0. Pinned hash unchanged.

### A7 PASS
PE is AMD64, Native, entry 0x1000, no CLR, imports only ntdll.dll.

Machine 0x8664. Magic 0x020B. Subsystem 1. Entry 0x00001000. CLR RVA 0 size 0. Delay import 0. Import count 1 ntdll.dll.

### A8 PASS
0 parser errors and 0 PSScriptAnalyzer warning or error findings.

Parse errors 0. Analyzer 1.25.0 error/warning count 0. Information count 46, rule PSAvoidUsingPositionalParameters.

### A9 PASS
38 functions and none of the five forbidden commands.

FunctionDefinitionAst count 38. Remove-Partition, Clear-Disk, Initialize-Disk, Format-Volume, and Remove-Item counts are 0. Resize-Partition, New-Partition, Set-Partition, Remove-PartitionAccessPath, Repair-Volume, and Restart-Computer are present.

### A10 PASS
Prepare order is preflight, hidden target, payload, read-only F, BootExecute, reboot.

Invoke-Prepare order matches. The target stage shrinks E before New-Partition when partition 4 is absent.

### A11 PASS
Verified finalizer order matches the acceptance sequence.

Status 0 or 1 returns first. Remove-BootImage precedes cutover. Status 2 checks 210749095936 verified bytes and NTSTATUS 0. Cutover unmaps F, keeps the source read-only, takes it offline, mounts the target, expands with Resize-Partition, scans, checks Dev Drive, assigns F, and verifies the old partition.

### A12 PASS
Prepare without -ConfirmDestructive exits 1. Plan and Status exit 0 and do not mutate.

Prepare exit 1 at line 735. Plan exit 0. Status exit 0. ALL_CHANGED=False.

### A13 PASS
Live layout is still D, E, F with the stated sizes and no boot arming.

E size 1718491576832. F size 210749095936, writable and online. No partition 4. BootExecute is only autocheck autochk *. Registry, installed boot image, and finalizer task are absent. A superseded runner file remains under C:\ProgramData\ViceSharp\Migration.

### A14 PASS
Pinned geometry and free-space reserve hold.

Target offset equals prepared E end. Final target end equals F offset. Double is exact. Live free after shrink is 171639799808, above 137438953472.

### A15 PASS
Boot app validates, copies, flushes, rereads, and records Verified only after success.

decide_run, validate_migration_layout, copy_blocks from offset 0, NtFlushBuffersFile, and verify_blocks implement the claim. Tests passed.

### A16 FAIL
Every boot console and user-facing state shows completed, current, and next.

PowerShell timestamps match yyyy-MM-ddTHH:mm:ss.fffK, observed 2026-10-05T11:19:53.774-05:00. display_failure omits Completed. The dashboard, progress line, skip messages, and PowerShell state include all three fields.

### A17 PASS
Failure cleanup never deletes the source or makes it writable.

Prepare catch removes BootExecute only if armed and unregisters the task. Restore paths force the original partition read-only. No delete-partition command exists. E shrink and partition 4 are not rolled back.

### A18 UNKNOWN
One UAC then fully unattended completion.

One RunAs, NonInteractive child, Restart-Computer -Force, and a SYSTEM startup task are in the script. Set-Partition ConfirmImpact is High and -Confirm false is absent. Real-partition confirmation was not executed.

### A19 FAIL
No live Prepare, UAC, disk mutation, BootExecute arm, or reboot has occurred yet.

Current disk and BootExecute are unarmed. Last boot 2026-10-05T06:54:34.500-05:00. Superseded SYSTEM runner started 20261005065535 and failed at 06:58:13. A reboot and a migration execution already occurred.

### B1 FAIL
Honesty about that reboot.

Same evidence as A19.

### B2 PASS
No Python, no source deletion, and the approval gate holds.

Python file count 0. Forbidden delete commands absent. Prepare guard did not mutate.

### B3 N/A
Byrd v4 is not applied to this lab migration.

Class 2 operator action.

### C1 N/A
ViceSharp FR/TR/TEST do not apply.

No product requirement was claimed. HANDOFF VSF work is a different plan.

### D1 UNKNOWN
One UAC and unattended completion.

Same open point as A18.

### D2 PASS
Read-only source, verified copy, preserved source, doubled target, and safe failure.

Code and tests support these criteria. See A10 through A15 and A17.

### D3 FAIL
Progress visibility.

Same gap as A16.
