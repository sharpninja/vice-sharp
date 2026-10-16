# Hostile validation rerun: remediated retain-old-F Dev Drive migration script

You are the independent local hostile reviewer for `F:\GitHub\vice-sharp`. Run entirely locally. Do not use web search, cloud execution, remote threads, remote CI, subagents, or repository/source edits.

Review the complete current artifact:

`E:\F-DevDrive-Migration\Move-FDevDrive.ps1`

The prior full response is:

`F:\GitHub\vice-sharp\docs\receipts\hv\20261004T221917Z-f-dev-drive-script-local-grok.response.jsonl`

That review returned DISAGREE 76/62, with 10 failures and 1 unknown. The script has been remediated. Recompute everything from the current file. Do not accept claimed remediation without direct source and command evidence.

This is a user-directed lab operation, not ViceSharp product requirements work. FR/TR/TEST and Byrd are N/A.

Before reviewing:

1. Execute the complete Grok add-profile skill and read every required profile file in full.
2. Read `F:\GitHub\vice-sharp\AGENTS-README-FIRST.yaml`, `AGENTS.md`, `HANDOFF.md`, and `E:\F-ReFS-maintenance-20260907T172800Z\evidence\codex-pause-handoff-20261004T214430Z.md`.
3. Bootstrap and use the marker-required local Grok MCP plugin.
4. Open and complete a dedicated MCP session-log turn. If primary persistence is unavailable, use only the supported plugin failsafe and report the exact disposition.
5. Persist the entire verdict and structured result in the session log or supported session-submit failsafe.
6. Do not edit the reviewed script or repository files. Plugin-managed cache, session, and failsafe artifacts are permitted.

## Operator requirement

The script must shrink pinned E:, create a new ReFS Dev Drive exactly twice old F in the contiguous gap immediately before old F, preserve old F while copying, fully validate copied content and metadata before cutover, transactionally move the F access path, and leave old F intact, read-only, and unmounted. It must keep all artifacts on C: or E:, provide rollback without deleting either partition, and fail closed.

Pinned geometry:

- Disk unique id `eui.E8238FA6BF530001001B448B455F6593`
- Disk size `2000398934016`
- E GUID `1f53f2e1-5be1-fc1b-e6b1-26f8e2338764`
- E offset `290455552`
- E current size `1718491576832`
- Old F GUID `930f8001-d112-4b91-acef-e9b55110f125`
- Old F offset `1789649747968`
- Old F size `210749095936`
- New F size `421498191872`
- New F offset `1368151556096`
- E target `1367861100544`
- E shrink `350630476288`

## Mandatory regression attacks

Recheck every prior finding:

1. Crash at every instruction boundary of the F-letter cutover, including state save, old access-path removal, old NoDefaultDriveLetter, new F assignment, Dev Drive query, and completed-state save. Execute and Rollback must recover the two-neither-F state and the new-is-F state.
2. Crash after `New-Partition` but before the returned GUID is saved. The exact new geometry may be adopted only when pre-call state authorizes it.
3. Temporary mount left by exceptions, process death, or every ShouldProcess decline. Rerun must recognize or remove the state-pinned mount safely.
4. Root last-write time and normalized attributes must be compared, not zeroed.
5. Every named alternate stream must be hashed and compared, while the default stream remains covered by the normal SHA-256.
6. SACLs must be included in security comparison. Lack of `SeSecurityPrivilege` must fail closed before storage mutation.
7. Disk, E, old F, and new volume health gates.
8. Script location, current directory, StateRoot, logs, manifests, and receipts confined to C: or E:.
9. Audit and Execute `-WhatIf` must not write any file or mutate storage.
10. A final Sysinternals handle.exe gate must refuse cutover while any F handle exists and retain a receipt.
11. Directory security copy must be explicit and verified, not assumed from ambiguous robocopy help.

Also recheck the complete requested outcome and all edge cases, not only those eleven items:

- No `Remove-Partition` or partition deletion path.
- Valid PowerShell parameter sets and runtime calls.
- Identity, geometry, free-space, fresh supported-size, health, state, collision, and authorization gates.
- Initial copy, read-only freeze, final copy, directory security synchronization, list-only robocopy exit 0, manifests, and only-then cutover.
- Reruns from every persisted phase, corrupt/missing state, unexpected range occupant, completed state, and rolled-back state.
- Every catch and ShouldProcess path leaves old F usable or recoverable.
- Rollback retains both partitions and restores old F writable.
- Reparse points and excluded system directories.
- Robocopy flags and exit-code semantics.
- State validation receipt hashes before completing an interrupted cutover.

## Current receipts to recompute

- Script SHA-256 `126FF5F044256D125EB8324847B00623537A3D7AC8A8984B6AC54CBB09EDFE08`
- Script bytes `48088`
- `-Mode Validate`: parser errors 0, multiplier 2, aligned geometry, no `Remove-Partition`, default mode non-mutating.
- ADS fixture self-test: one stream named `proof`, length 3, SHA-256 `3774FED6F81E6D77FB664109F8AC3E35088E89722C6C8747166AFE631CFCF43A`.
- AST: `Remove-Partition` 0, `Resize-Partition` 1, `New-Partition` 1, `Format-Volume` 1, final handle gate 1.

You may execute only `-Mode Validate` and other read-only inspections. Do not execute Audit, Execute, Rollback, Resize-Partition, New-Partition, Format-Volume, Set-Partition, Add-PartitionAccessPath, or Remove-PartitionAccessPath.

## Verdict contract

- Full readable verdict followed by final parseable JSON.
- Grade every item PASS, FAIL, UNKNOWN, or N/A with file/line or command evidence.
- Display every FAIL and UNKNOWN.
- `OverallVerdict=AGREE` only if accuracy and completeness are both at least 98, FAIL 0, and UNKNOWN 0.
- Include model, effort, local identifiers, profile receipt, MCP session/request IDs, persistence disposition, and failsafe path if any.
