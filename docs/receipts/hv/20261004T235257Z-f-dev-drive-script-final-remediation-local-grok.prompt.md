# Final remediation hostile validation: retain-old-F Dev Drive migration script

Run a fresh, independent, entirely local hostile validation of:

`E:\F-DevDrive-Migration\Move-FDevDrive.ps1`

No web, cloud execution, remote threads, remote CI, subagents, source edits, or storage mutation.

This is a user-directed lab operation. FR/TR/TEST and Byrd are N/A.

The immediately prior review is:

- `F:\GitHub\vice-sharp\docs\receipts\hv\20261004T231848Z-f-dev-drive-script-final-local-grok.response.jsonl`
- Verdict: DISAGREE, accuracy 92, completeness 92, pass 35, fail 3, unknown 0.
- Failures: `state-stale`, `partition-recovery-without-state`, and `timestamps`.

The current artifact claims to remediate those findings by:

- Reading every schema-valid state copy and selecting the newest `UpdatedUtc`, falling back to `CreatedUtc`, rather than returning the first valid copy.
- Rejecting equally fresh but divergent state copies instead of choosing arbitrarily.
- Recovering an exact-geometry new partition into freshly rebuilt state when every state copy was lost but old F still owns F:.
- Allowing recovery of a `Planned`, `EShrunk`, or `CreateNewPartitionStarted` state whose exact-geometry partition exists, with pinned disk, offset, size, GPT type, and protected-GUID checks.
- Copying the source root's creation, last-access, and last-write UTC timestamps to the new root.
- Including root creation, last-access, and last-write ticks in the equality manifest.

Do not accept these claims without direct proof. Reproduce the stale-primary fixture, state-loss plus exact-partition recovery analysis, and root timestamp comparison. Review the complete script and all operator requirements, not merely the last patch.

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
- Initial copy, read-only freeze, final copy, explicit directory security synchronization, list-only robocopy equality, SHA-256 default streams, every named alternate stream, timestamps, attributes, DACL, owner, and SACL.
- Root creation, last-access, last-write, attributes, ACL, SACL, and alternate streams are compared.
- Only validated content may reach cutover.
- Final open-handle gate and receipt.
- Crash recovery at every partition-creation and drive-letter boundary.
- Missing, corrupt, stale, partially written, or mutually inconsistent primary, backup, and emergency state.
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

- SHA-256 `BF43892EB53483BF785D5576724DB30777EAA9460F024A10BA90789477D74F79`
- 57299 bytes
- `-Mode Validate`: parser errors 0, exact multiplier 2, aligned geometry, `Remove-Partition` absent, default mode non-mutating.
- State fixture: stale primary did not win, corrupt primary fell back, and equally fresh divergent copies were rejected.
- Partition recovery helper fixture: the exact pinned partition became `NewPartitionCreated` with its GUID and partition number.
- Root timestamp setter fixture: creation, last-access, and last-write ticks all matched exactly.

You may run only `-Mode Validate`, function-level temporary fixtures, and read-only inspections. Do not run Audit, Execute, Rollback, or any storage-changing cmdlet.

Verdict contract:

- Full readable verdict followed by parseable JSON.
- Every item PASS, FAIL, UNKNOWN, or N/A with exact evidence.
- Display every FAIL and UNKNOWN.
- AGREE only with accuracy and completeness each at least 98, FAIL 0, and UNKNOWN 0.
- Include model, effort, local identifiers, profile receipt, MCP session/request IDs, persistence disposition, and failsafe path if any.
