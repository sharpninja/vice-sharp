# Hostile validation: ViceSharp refresh-docs using MCP failsafe

You are the independent local hostile reviewer for F:\GitHub\vice-sharp. Run entirely on this local machine. Do not use web search, cloud agents, remote threads, remote CI, or subagents. Do not edit repository files.

Before validation:
1. Execute the complete Grok add-profile skill and read every required profile file in full.
2. Read F:\GitHub\vice-sharp\AGENTS-README-FIRST.yaml and F:\GitHub\vice-sharp\AGENTS.md.
3. Bootstrap and use the marker-required local Grok MCP plugin.
4. Open and complete a dedicated MCP session-log turn for this review. If the MCP backend is unavailable, use the plugin's supported failsafe queue. Do not use raw REST and do not hand-author YAML. Persist the full verdict, all findings, scores, structured verdict, and receipt paths in the session log or supported session-submit failsafe.
5. Explicitly report whether failsafe was used, every resulting failsafe path, SHA-256, pending count, quarantine count, and PrimaryReadback true or false. A failsafe retention receipt is not primary persistence. Require later drain and primary readback before any done-state change.

Review target:
- User request: run refresh-docs, then explicitly "Use failsafe".
- This is project documentation work. No commit, push, requirement/TODO completion, plan completion, or product implementation is claimed.
- Native VICE is the immutable behavioral target. The refresh did not intentionally edit native/vice. The nested native/vice checkout already contains dirty files from earlier work, so do not infer that it is clean. Verify that this refresh's scoped diff is documentation only and report the pre-existing dirty native state accurately.
- VICE .vsf is the canonical required ViceSharp snapshot format since day one. Managed VSF staging already exists and must not be described as absent or secondary.
- UWP/Xbox product implementation is out of current scope, but historical requirements remain active and are not retired.

Files in the refresh slice:
- README.md
- docs/README.md
- docs/Architecture.md
- docs/Iteration-Roadmap.md
- docs/USER-GUIDE.md
- docs/VICE-MIGRATION.md
- docs/Public-API.md
- docs/plan.md
- docs/wiki.yaml
- docs/plans/PLAN-VICE-IMMUTABLE-001.md
- docs/plans/PLAN-VSFLOCKSTEP-001.md

Inspect the full current files and the scoped git diff. Validate these requested surfaces:

A. Requested result
A1. Current documentation reflects the actual repository code and does not claim broad VICE parity.
A2. Historical completion evidence is explicitly bounded and is not promoted into current acceptance.
A3. Snapshot documentation accurately says current product formats are proprietary, while VICE .vsf is canonical and managed VSF staging already exists but is incomplete/product-unexposed.
A4. xvic support is described exactly: launcher topology generation exists, the console hardcodes x64sc, and generic multi-system kind dispatch recognizes only C64 and C1541.
A5. VIC-20 settings visibility is documented consistently with current SettingsView/AttachPanelViewModel behavior and the focused validation: 8 passed, 0 failed, 0 skipped.
A6. docs/wiki.yaml is valid after full-object YAML mutation: schema mcp-wiki-export/v1, 37 documents, 0 duplicate IDs, 0 duplicate targets, 0 missing sources, 0 missing navigation references, and 0 non-GitHub platform entries.
A7. Retention/pruning is honest: no historical document was deleted because the reviewed legacy material remains useful evidence and is now scoped as historical.
A8. Requirements wiki export through workflow.requirements.generateDocument is blocked by the unavailable MCP backend. No raw REST or hand-authored generated substitute was used.
A9. No native VICE file was edited by this refresh. Report the existing nested native/vice dirtiness as pre-existing workspace state, not as a clean immutable tree.
A10. Traceability is reported as an open red baseline, not green: 235 canonical IDs, 149 referenced, 86 missing, 1,011 noncanonical; FR 132/101/31, TR 59/32/27, TEST 44/16/28.
A11. The x64sc binary-name documentation does not claim that selecting the name proves behavioral parity.

B. Workspace rules
- PowerShell.MCP only, no Python, no raw REST, no cloud execution.
- No commit/push.
- No destructive cleanup.
- No manual YAML text mutation.
- Exact receipts and honest blocker reporting.
- MCP writes retained via supported failsafe because the backend connection is refused.
- No done-state change without a fresh qualifying HV.

C. Requirements and acceptance criteria
- Verify the refresh preserves the active requirement truth: VICE behavior is the oracle; native VICE is immutable; canonical VSF is not secondary; legacy requirements are not retired.
- Verify the docs do not falsely satisfy or complete FR/TR/TEST records.
- Treat the blocked generated export and the red traceability counts as explicit unresolved evidence, not silent success.

D. Current plan holistically
- Read docs/plans/PLAN-VICE-IMMUTABLE-001.md and docs/plans/PLAN-VSFLOCKSTEP-001.md.
- Verify status prose matches the actual receipts and does not claim implementation, primary MCP persistence, or completion.
- Verify no plan step is marked done by this refresh.

Local validation receipts already produced:
- git diff --check: exit 0, no output after the final documentation edits.
- Markdown local-link audit: 10 files checked, 0 broken local links.
- wiki manifest audit: 37 documents and all six error counts above are 0.
- focused test command:
  dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --no-restore --filter "FullyQualifiedName~Vic20SettingsVisibilityNotificationTests|FullyQualifiedName~Vic20SettingsUiSurfaceTests" --logger "console;verbosity=normal"
  Result: total 8, passed 8, failed 0, skipped 0, duration 2.753s.
- requirement traceability command:
  .\tools\check_requirement_traceability.ps1
  Latest full output: F:\GitHub\vice-sharp\.mcpServer\tmp\PowerShell.MCP.Output\pwsh_output_20261004_120334_084_vr4zl0ft.vjy.txt
- The parent Codex plugin status before HV: status available, hasSession true, hasCurrentTurn true, pendingCount 11, quarantineCount 0. A prior workflow.failsafe.drain attempt proved the backend connection was actively refused at PAYTON-LEGION2:7147.

Verdict contract:
- Produce a complete readable verdict plus a final parseable JSON object.
- Classify every check PASS, FAIL, UNKNOWN, or N/A with evidence.
- List every failure and unknown explicitly.
- Report OverallVerdict AGREE only when AccuracyScore >= 98, CompletenessScore >= 98, FAIL = 0, and UNKNOWN = 0.
- A qualifying AGREE validates this documentation slice only. It must not authorize any requirement, TODO, goal, or plan done-state because PrimaryReadback is expected false while failsafe-pending.
- Include Grok model/effort, local session/thread identifiers, MCP sessionId/requestId if any, plugin identity, profile-load receipt, request/response receipt paths, failsafe disposition, and the full structured verdict.
