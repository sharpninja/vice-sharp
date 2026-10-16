# Final timestamp-proof hostile validation: retain-old-F Dev Drive migration script

Run a fresh, independent, entirely local hostile validation of:

`E:\F-DevDrive-Migration\Move-FDevDrive.ps1`

No web, cloud execution, remote threads, remote CI, subagents, source edits, or storage mutation.

This is a user-directed lab operation. FR/TR/TEST and Byrd are N/A.

Progress JSONL contract:

- Emit periodic progress events into the response JSONL throughout this review.
- Every progress event must include a numeric completion percentage and an estimated time remaining (ETA).
- Emit an initial progress event, updates after each major validation surface, and a final 100 percent event before the verdict.

The immediately prior review is:

- `F:\GitHub\vice-sharp\docs\receipts\hv\20261005T002219Z-f-dev-drive-script-runtime-remediation-local-grok.response.jsonl`
- Verdict: DISAGREE, accuracy 89, completeness 89, pass 17, fail 2, unknown 0.
- Failures: `creation-timestamps-not-compared` and `root-last-write-applied-after-manifest`.

The current artifact claims to remediate those findings by:

- Adding `CreationUtcTicks` to every file and directory manifest entry, not only the root.
- Capturing the original root last-access timestamp before any traversal.
- Refreshing the stable root creation and last-write snapshot after the source is frozen read-only and the final robocopy completes.
- Manifest-comparing the refreshed root creation/write values and every child creation/write value.
- Applying the same refreshed root creation/write values after manifest comparison, then applying the original last-access value as the final destination-root operation.

Do not accept these claims without direct proof. Reproduce a creation-only child mismatch fixture. Reproduce a post-capture source root last-write change, then prove the refresh adopts the frozen value and retains the original last-access value. Review the complete script and every earlier requirement, not merely the last patch.

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
- Initial copy, read-only freeze, final copy, directory security synchronization, list-only robocopy equality, SHA-256 default streams, every named alternate stream, creation and last-write timestamps, attributes, DACL, owner, and SACL.
- Stable root creation/write, attributes, ACL, SACL, and alternate streams are compared; volatile last-access is captured before traversal and restored only after traversal.
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

- SHA-256 `F372908CF27C93F58543C0CB17944131662CA5C82FFE1348C6F1D9B67149EE15`
- 60448 bytes
- `-Mode Validate`: parser errors 0, exact multiplier 2, aligned geometry, `Remove-Partition` absent, default mode non-mutating.
- Creation-only mismatch fixture: same bytes and same last-write, different creation ticks, manifest mismatch detected.
- Stable-root refresh fixture: creation and last-write updated to the post-freeze source values while the original last-access snapshot remained unchanged.
- Supported handle query and parser behavior were proven in the preceding review and must be rechecked.

You may run only `-Mode Validate`, function-level temporary fixtures, the read-only handle query, and read-only inspections. Do not run Audit, Execute, Rollback, or any storage-changing cmdlet.

Verdict contract:

- Full readable verdict followed by parseable JSON.
- Every item PASS, FAIL, UNKNOWN, or N/A with exact evidence.
- Display every FAIL and UNKNOWN.
- AGREE only with accuracy and completeness each at least 98, FAIL 0, and UNKNOWN 0.
- Include model, effort, local identifiers, profile receipt, MCP session/request IDs, persistence disposition, and failsafe path if any.
