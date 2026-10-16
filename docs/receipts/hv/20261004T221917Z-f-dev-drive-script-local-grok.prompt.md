# Hostile validation: retain-old-F Dev Drive migration script

You are the independent local hostile reviewer for `F:\GitHub\vice-sharp`. Run entirely locally. Do not use web search, cloud execution, remote threads, remote CI, subagents, or repository/source edits.

The artifact under review is outside F so it remains available while F is unmounted:

`E:\F-DevDrive-Migration\Move-FDevDrive.ps1`

This is a user-directed lab operation, not ViceSharp product requirements work. Do not demand FR/TR/TEST records. Review the requested result, workspace rules, storage safety, edge cases, and the complete script logic.

Before reviewing:

1. Execute the complete Grok add-profile skill and read every required profile file in full.
2. Read `F:\GitHub\vice-sharp\AGENTS-README-FIRST.yaml`, `AGENTS.md`, `HANDOFF.md`, and `E:\F-ReFS-maintenance-20260907T172800Z\evidence\codex-pause-handoff-20261004T214430Z.md`.
3. Bootstrap and use the marker-required local Grok MCP plugin.
4. Open and complete a dedicated MCP session-log turn. If primary persistence is unavailable, use only the supported plugin failsafe and report that disposition precisely.
5. Persist the entire verdict and structured result in the session log or supported session-submit failsafe.
6. Do not edit the reviewed script or any repository file. Plugin-managed cache, session, and failsafe artifacts are permitted.

## Operator requirement

The completed script must:

1. Shrink E: on the pinned Disk 0.
2. Create a new ReFS Dev Drive in the contiguous gap immediately before old F:.
3. Make the new partition exactly twice the old F partition size.
4. Keep old F intact while copying.
5. Copy old F to the new Dev Drive.
6. Validate the entire copied tree and metadata before cutover.
7. Remove the F access path from the old volume only after validation.
8. Assign F to the validated new Dev Drive transactionally.
9. Leave the old partition intact, read-only, and unmounted. It must never be deleted.
10. Keep all script, state, logs, manifests, and recovery artifacts on C: or E:, never F:.
11. Provide a safe rollback that restores F to the old partition without deleting the new partition.
12. Fail closed on identity, geometry, supported-size, health, copy, validation, Dev Drive, or cutover discrepancies.

## Pinned live facts

- Disk 0 unique id: `eui.E8238FA6BF530001001B448B455F6593`
- Disk size: `2000398934016`
- E GUID: `1f53f2e1-5be1-fc1b-e6b1-26f8e2338764`
- E offset: `290455552`
- E current size: `1718491576832`
- Old F GUID: `930f8001-d112-4b91-acef-e9b55110f125`
- Old F offset: `1789649747968`
- Old F size: `210749095936`
- Required new F size: `421498191872`
- Required new F offset: `1368151556096`
- Required E target size: `1367861100544`
- Required E shrink: `350630476288`
- Existing old package is forbidden because it deletes old F.

## Required attacks

### A. Requested outcome

- Verify the exact geometry math and 1 MiB alignment.
- Verify no code path invokes `Remove-Partition` or deletes either partition.
- Verify `Format-Volume -DevDrive` creates ReFS with the expected label and allocation unit.
- Verify initial copy, read-only freeze, final copy, list-only comparison, SHA-256 content manifests, metadata/ACL comparison, and only-then cutover.
- Verify old F remains intact and unmounted after success.
- Verify rollback can restore the old partition and leaves the new one intact/unmounted.
- Verify execution is administrator, location, token, identity, health, free-space, supported-size, and state gated.
- Verify `Validate` is the default and storage-mutation-free; `Audit` must also be storage-mutation-free.

### B. Logic and edge cases

- Reruns after E shrink, partition creation, formatting, partial copy, validation, failed cutover, completion, and rollback.
- Every `ShouldProcess` decline point and every exception path.
- Old-F thaw or rollback after pre-cutover and cutover failures.
- Partition access-path handling, including folder mounts and volume GUID paths.
- Manifest determinism, root handling, excluded system directories, reparse points, ACLs, timestamps, attributes, file hashes, and alternate streams.
- Robocopy arguments and exit-code semantics for both copy and verification.
- State corruption, missing state, an unexpected partition occupying the range, and a state-pinned existing new partition.
- Whether a failed final Dev Drive query rolls back correctly.
- Whether use of the active F workspace or open handles can invalidate the operation.
- Any PowerShell parameter-set, cmdlet, type-conversion, or runtime bug.

### C. Workspace rules

- PowerShell only, no Python.
- No web or cloud.
- No storage mutation during review.
- No project requirement or TODO done-state change.
- No source edits by the reviewer.

## Existing validation receipts

- Script SHA-256: `84EBF69B3FEB7EC3B59F7EBEA551F63C44406A7F47F3AC5AD05CEE73932FA65B`
- Script bytes: `37159`
- `-Mode Validate`: parser errors 0, exact multiplier 2, geometry aligned, default mode non-mutating.
- Independent AST scan: `Remove-Partition` calls 0, `Remove-PartitionAccessPath` calls 4, `Resize-Partition` calls 1, `New-Partition` calls 1, `Format-Volume` calls 1.

Recompute these. You may execute only `-Mode Validate`. Do not execute `Audit`, `Execute`, or `Rollback`.

## Verdict contract

- Return a full readable verdict followed by final parseable JSON.
- Grade every applicable item PASS, FAIL, UNKNOWN, or N/A with exact file/line or command receipts.
- Display every FAIL and UNKNOWN.
- `OverallVerdict` may be `AGREE` only when accuracy and completeness are both at least 98, FAIL is 0, and UNKNOWN is 0.
- Do not soften any data-loss, rollback, idempotency, validation, or PowerShell runtime defect.
- Include model, effort, local identifiers, profile receipt, MCP session/request IDs, persistence disposition, and any failsafe path.
