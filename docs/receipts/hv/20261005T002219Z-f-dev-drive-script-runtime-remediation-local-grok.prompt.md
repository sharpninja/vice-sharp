# Final runtime remediation hostile validation: retain-old-F Dev Drive migration script

Run a fresh, independent, entirely local hostile validation of:

`E:\F-DevDrive-Migration\Move-FDevDrive.ps1`

No web, cloud execution, remote threads, remote CI, subagents, source edits, or storage mutation.

This is a user-directed lab operation. FR/TR/TEST and Byrd are N/A.

The immediately prior review is:

- `F:\GitHub\vice-sharp\docs\receipts\hv\20261004T235257Z-f-dev-drive-script-final-remediation-local-grok.response.jsonl`
- Verdict: DISAGREE, accuracy 94, completeness 94, pass 32, fail 2, unknown 0.
- Failures: `root-last-access-not-stable` and `open-handle-gate-cannot-pass`.

The current artifact claims to remediate those findings by:

- Capturing the old F root's creation, last-access, and last-write ticks in durable state before any directory traversal.
- Manifest-comparing the stable root creation and last-write timestamps.
- Applying the captured root creation and last-write timestamps after traversal and verifying both.
- Applying the captured root last-access timestamp as the final filesystem operation, without a readback that would itself schedule another NTFS last-access update.
- Replacing the failing `handle.exe -vt` mode with the locally proven `handle.exe -accepteula -nobanner 'F:\'` invocation.
- Parsing the real process-oriented output format and rejecting every matching F handle while accepting `No matching handles found.`

Do not accept these claims without direct proof. Reproduce the handle invocation and parser behavior on this machine. Exercise the root timestamp helper on a temporary tree and analyze the ordering in the real Execute path. Recheck every earlier requirement, not merely the last patch.

Before review:

1. Run the complete Grok add-profile skill and read every required profile file in full.
2. Read the workspace marker, `AGENTS.md`, `HANDOFF.md`, and `E:\F-ReFS-maintenance-20260907T172800Z\evidence\codex-pause-handoff-20261004T214430Z.md`.
3. Bootstrap the marker-required local Grok MCP plugin.
4. Open and complete a dedicated MCP session-log turn, storing the entire verdict. Use the supported failsafe only if primary persistence fails.
5. Do not modify the reviewed artifact or repository. Plugin-managed state is permitted.

Validate all of the following:

- Exact pinned geometry and exact 2x new-F size.
- Fresh supported-size and free-space gate before shrinking E.
- New ReFS Dev Drive in the gap immediately before old F.
- No `Remove-Partition`, deletion, or formatting of old F.
- Initial copy, read-only freeze, final copy, directory security synchronization, list-only robocopy equality, SHA-256 default streams, every named alternate stream, timestamps, attributes, DACL, owner, and SACL.
- Stable root timestamps, attributes, ACL, SACL, and alternate streams are compared; volatile last-access is captured before traversal and restored only after traversal.
- Only validated content may reach cutover.
- Final open-handle gate uses a working local command, captures a receipt, and cannot silently miss real F handles.
- Crash recovery at every partition-creation and drive-letter boundary.
- Missing, corrupt, stale, partially written, or mutually inconsistent primary, backup, and emergency state.
- State loss with an existing exact-geometry new partition recovers safely.
- Execute and Rollback behavior when old is F, new is F, neither is F, or an unrelated partition is F.
- Temporary mount cleanup and every ShouldProcess decline and catch path.
- Old partition remains intact, read-only, and unmounted after success.
- Rollback restores old F writable and keeps new intact and unmounted.
- Script, current directory, state, logs, manifests, and receipts remain on C: or E:.
- Validate is default and mutation-free. Audit and Execute `-WhatIf` are mutation-free.
- All PowerShell parameter sets and runtime types are legal.

Pinned geometry:

- Disk `eui.E8238FA6BF530001001B448B455F6593`, size `2000398934016`
- E GUID `1f53f2e1-5be1-fc1b-e6b1-26f8e2338764`, offset `290455552`, current `1718491576832`, target `1367861100544`
- Old F GUID `930f8001-d112-4b91-acef-e9b55110f125`, offset `1789649747968`, size `210749095936`
- New F offset `1368151556096`, size `421498191872`
- E shrink `350630476288`

Current artifact receipts to recompute:

- SHA-256 `F1EE2F555CC1F4A1133E8639D32A4E9331ED123CF9A2816A7FD9DD7A4DB5E0E4`
- 59826 bytes
- `-Mode Validate`: parser errors 0, exact multiplier 2, aligned geometry, `Remove-Partition` absent, default mode non-mutating.
- The installed handle binary at `C:\Users\kingd\AppData\Local\Microsoft\WinGet\Packages\Microsoft.Sysinternals.Handle_Microsoft.Winget.Source_8wekyb3d8bbwe\handle.exe` returned exit 0 for `-accepteula -nobanner 'F:\'` and emitted 57 real matching-handle lines.
- A real line such as `ChatGPT.exe        pid: 20640  type: File          1BE8: F:\GitHub\vice-sharp\.git\info` matched the new parser; `No matching handles found.` did not.
- Root helper fixture immediately matched captured creation, last-access, and last-write ticks. The script performs no destination-root filesystem read after its final last-access setter.

You may run only `-Mode Validate`, function-level temporary fixtures, the read-only handle query, and read-only inspections. Do not run Audit, Execute, Rollback, or any storage-changing cmdlet.

Verdict contract:

- Full readable verdict followed by parseable JSON.
- Every item PASS, FAIL, UNKNOWN, or N/A with exact evidence.
- Display every FAIL and UNKNOWN.
- AGREE only with accuracy and completeness each at least 98, FAIL 0, and UNKNOWN 0.
- Include model, effort, local identifiers, profile receipt, MCP session/request IDs, persistence disposition, and failsafe path if any.
