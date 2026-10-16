You are the HOSTILE VALIDATOR for workspace F:\GitHub\vice-sharp.

FIRST ACTION: execute the add-profile skill at C:\Users\kingd\.grok\skills\add-profile\SKILL.md and follow it literally. Read every required non-skill Markdown profile file in C:\Users\kingd\.claude\profile in full. Record the profile file count.

This is a local-only review. Do not use web search, web fetch, cloud agents, subagents, Python, or Node. Use PowerShell only through the available PowerShell MCP tooling. Do not modify disks, partitions, volumes, drive letters, services, tasks, source files, or configs. Review only. You may write only the required hostile-validation receipt Markdown and JSON files under F:\GitHub\vice-sharp\docs\receipts and your MCP session-log turn. Do not print or log credentials, API keys, passwords, or tokens.

Classify this as class 2 user-directed local storage diagnosis and recommendation. Surface C requirements is N/A. Surface D is N/A because no product plan completion is claimed. Still evaluate surfaces A and B fully.

User request:
"Figure out a location to relocate F: where we can double it's size."

Implementer claims to attack:
1. Live F: is a healthy ReFS volume on NVMe Disk 0 with 210,721,832,960 bytes capacity (196.25 GiB), about 194.7 GiB used, and only about 1.5-2.0 GiB free during inspection.
2. A true 2x target is 421,443,665,920 bytes (392.50 GiB).
3. No currently installed separate local physical disk has enough free space for a 392.50 GiB destination: C: has 317,507,244,032 bytes free (295.70 GiB), USB disks 2 and 3 report no media, and no mapped network filesystem was present.
4. E: is the only installed local capacity donor/staging candidate. E: is an NTFS partition on the same NVMe Disk 0 as F:, with 522,290,339,840 bytes free (486.42 GiB). Disk 0 also has a 70,866,960,384-byte (66.00 GiB) unallocated gap immediately before F:.
5. An existing dormant migration package at E:\F-ReFS-maintenance-20260907T172800Z already targets recreating F: as a 500 GiB ReFS volume by shrinking E: 237.72 GiB, deleting only old F:, creating a new 500 GiB partition over the final region of Disk 0, restoring from the E: backup, and reusing F:. The live Disk 0 unique ID, GPT GUID, disk size, and D/E/F partition offsets and sizes match the pinned plan. Evidence and installed plan SHA256 both equal 3F19DC53922377540CAE6ED388D8040E3A1AA294AB73823B44DC35F43DA43279.
6. A 500 GiB F: would be 2.548 times current F: capacity and, from current free-space arithmetic, shrinking E: by 237.72 GiB would leave E: about 248.70 GiB free.
7. The existing migration is NOT execution-ready and must not be run as-is. Its installed state says Phase=NotStarted. Its prior maintenance worker status is Failed, and event receipts show two failures before any storage mutation. The service is Stopped and Disabled. The backup is from September and must be refreshed and reverified against current F: before destructive work.
8. Current non-elevated Get-PartitionSupportedSize calls for E: and F: failed with "Access to a CIM resource was not available to the client." Therefore the recommendation is conditional on a fresh elevated supported-size/geometry preflight immediately before mutation.
9. Recommendation: if the goal is capacity while preserving F: and ReFS, use the existing Disk 0 E:/F: layout as donor/staging and repair/revalidate the 500 GiB migration procedure. Do not simply run the existing failed worker. If "relocate" requires a different physical disk for fault isolation, there is no installed candidate; a new physical disk is required.
10. No disk, partition, volume, drive letter, service, scheduled task, or user data was modified during this diagnosis. One tool-generated 32,293,617-byte temporary PowerShell output file under F:\GitHub\vice-sharp\.mcpServer\tmp was deleted after inspection because the nearly-full F: volume could not absorb it indefinitely.

Evidence to independently inspect:
- Live PowerShell: Get-Disk, Get-PhysicalDisk, Get-Partition, Get-Volume, Get-PSDrive, Get-StoragePool, Get-VirtualDisk
- E:\F-ReFS-maintenance-20260907T172800Z\evidence\F-ReFSMigration.Plan.json
- E:\F-ReFS-maintenance-20260907T172800Z\evidence\F-ReFSMigration.PackageManifest.json
- E:\F-ReFS-maintenance-20260907T172800Z\evidence\astra-repartition-procedure-review-20260907.md
- C:\ProgramData\TruckMateMaintenance\F-ReFS-20260907\migration-state.json
- C:\ProgramData\TruckMateMaintenance\F-ReFS-20260907\maintenance-worker-status.json
- C:\ProgramData\TruckMateMaintenance\F-ReFS-20260907\maintenance-worker-events.jsonl
- E:\F-ReFS-maintenance-20260907T172800Z\volume-backup
- F:\GitHub\vice-sharp\AGENTS.md
- F:\GitHub\vice-sharp\AGENTS-README-FIRST.yaml

Mandatory checks:
- Recompute all capacity arithmetic and independently map volumes to physical disks.
- Verify whether "E: is the only installed local candidate" is accurate.
- Distinguish same-disk capacity expansion from a true cross-disk relocation and flag ambiguity.
- Verify every pinned-plan identity/geometry/hash claim.
- Verify prior failures really occurred before any storage mutation.
- Assess whether the conditional recommendation is safe and complete for a diagnosis-only answer.
- Confirm no storage mutation occurred during this review.
- Audit PowerShell/no-Python, receipts, honesty, and MCP-only session logging.
- Do not demand FR/TR/TEST/AC for this class-2 task.
- Use MCP failsafe if session logging cannot persist; name any failsafe path in the receipt. If failsafe also fails, report it explicitly.
- Persist the complete verdict in your own MCP session-log turn and prove it with query-history readback.

Acceptance gate:
- OverallVerdict=AGREE only if every applicable claim passes.
- Both Accuracy and Completeness must be numeric and at least 98.
- Report PASS/FAIL/UNKNOWN counts and every failure/unknown.
- Write a durable Markdown receipt and matching JSON twin:
  F:\GitHub\vice-sharp\docs\receipts\hostile-validator-20261004T213900Z-f-drive-capacity-location.md
  F:\GitHub\vice-sharp\docs\receipts\hostile-validator-20261004T213900Z-f-drive-capacity-location.json
- End with the receipt paths, OverallVerdict, scores, counts, reviewer sessionId, and reviewer requestId.
