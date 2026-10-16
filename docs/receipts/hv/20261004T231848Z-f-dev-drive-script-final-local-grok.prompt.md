# Final hostile validation: retain-old-F Dev Drive migration script

Run a fresh, independent, entirely local hostile validation of:

`E:\F-DevDrive-Migration\Move-FDevDrive.ps1`

No web, cloud execution, remote threads, remote CI, subagents, source edits, or storage mutation.

Prior reviews:

- `F:\GitHub\vice-sharp\docs\receipts\hv\20261004T221917Z-f-dev-drive-script-local-grok.response.jsonl`: DISAGREE 76/62, 10 FAIL, 1 UNKNOWN.
- `F:\GitHub\vice-sharp\docs\receipts\hv\20261004T225013Z-f-dev-drive-script-remediated-local-grok.response.jsonl`: DISAGREE 97/96, 1 FAIL, 0 UNKNOWN.

The last remaining failure was state loss. The current script now has:

- primary state on E;
- backup state on E;
- emergency state on C;
- validated serialization before replacement;
- fallback reads across all three copies;
- pinned-geometry, state-independent F-letter recovery when all copies are absent or unreadable;
- Execute recovery that restores old F, rebuilds state, and stops for receipt review;
- Rollback recovery that restores old F and rebuilds state without deleting either partition.

Do not accept these claims without direct proof.

This is a user-directed lab operation. FR/TR/TEST and Byrd are N/A.

Before review:

1. Run the complete Grok add-profile skill and read every required profile file in full.
2. Read the workspace marker, `AGENTS.md`, `HANDOFF.md`, and the E pause handoff.
3. Bootstrap the marker-required local Grok MCP plugin.
4. Open and complete a dedicated MCP session-log turn, storing the entire verdict. Use the supported failsafe only if primary persistence fails.
5. Do not modify the reviewed artifact or repository. Plugin-managed state is permitted.

Review the complete script and all operator requirements, not merely the last patch:

- Exact pinned geometry and exact 2x size.
- Fresh supported-size and free-space gate before shrinking E.
- New ReFS Dev Drive in the gap immediately before old F.
- No `Remove-Partition`, deletion, or formatting of old F.
- Initial copy, read-only freeze, final copy, explicit directory security synchronization, list-only robocopy equality, SHA-256 default streams, every named alternate stream, timestamps, attributes, DACL, owner, and SACL.
- Root metadata comparison.
- Only validated content may reach cutover.
- Final open-handle gate and receipt.
- Crash recovery at every partition-creation and drive-letter boundary.
- Missing, corrupt, stale, or partially written primary, backup, and emergency state.
- Execute and Rollback behavior when old is F, new is F, neither is F, or an unrelated partition is F.
- Temporary mount cleanup and every ShouldProcess decline and catch path.
- Old partition remains intact, read-only, and unmounted after success.
- Rollback restores old F writable and keeps new intact/unmounted.
- Script, current directory, state, logs, manifests, and receipts remain on C: or E:.
- Validate is default and mutation-free. Audit and Execute `-WhatIf` are mutation-free.
- All PowerShell parameter sets and runtime types are legal.

Pinned geometry remains:

- Disk `eui.E8238FA6BF530001001B448B455F6593`, size `2000398934016`
- E GUID `1f53f2e1-5be1-fc1b-e6b1-26f8e2338764`, offset `290455552`, current `1718491576832`, target `1367861100544`
- Old F GUID `930f8001-d112-4b91-acef-e9b55110f125`, offset `1789649747968`, size `210749095936`
- New F offset `1368151556096`, size `421498191872`
- E shrink `350630476288`

Current receipts to recompute:

- SHA-256 `5E5D4A5EFF2561E4D98CC2C7EA27CDC12377190C7E5D309898C2D06039322507`
- 54111 bytes
- `-Mode Validate`: parser errors 0, exact multiplier 2, aligned geometry, `Remove-Partition` absent, default mode non-mutating.
- State-fallback fixture: all three copies created; corrupt primary fell back to backup; corrupt primary plus backup fell back to emergency; phase and new GUID were recovered.
- ADS fixture: stream `proof`, length 3, SHA-256 `3774FED6F81E6D77FB664109F8AC3E35088E89722C6C8747166AFE631CFCF43A`.

You may run only `-Mode Validate` and read-only inspections. Do not run Audit, Execute, Rollback, or any storage-changing cmdlet.

Verdict contract:

- Full readable verdict followed by parseable JSON.
- Every item PASS, FAIL, UNKNOWN, or N/A with exact evidence.
- Display every FAIL and UNKNOWN.
- AGREE only with accuracy and completeness each at least 98, FAIL 0, and UNKNOWN 0.
- Include model, effort, local identifiers, profile receipt, MCP session/request IDs, persistence disposition, and failsafe path if any.
