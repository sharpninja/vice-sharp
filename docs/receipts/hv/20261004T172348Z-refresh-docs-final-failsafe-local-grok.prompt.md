# Hostile validation rerun: final ViceSharp refresh-docs using MCP failsafe

You are the independent local hostile reviewer for F:\GitHub\vice-sharp. Run entirely on this local machine. Do not use web search, cloud agents, remote threads, remote CI, or subagents. Do not edit repository files.

This is a fresh final-text rerun after the prior review at docs/receipts/hv/20261004T170351Z-refresh-docs-failsafe-local-grok.verdict.md identified two non-failing stale statements. Those statements were corrected in docs/USER-GUIDE.md and HANDOFF.md. Revalidate the complete final slice, not only those two lines.

Before validation:
1. Execute the complete Grok add-profile skill and read every required profile file in full.
2. Read F:\GitHub\vice-sharp\AGENTS-README-FIRST.yaml, F:\GitHub\vice-sharp\AGENTS.md, and F:\GitHub\vice-sharp\HANDOFF.md.
3. Bootstrap and use the marker-required local Grok MCP plugin.
4. Open and complete a dedicated MCP session-log turn. If the backend is unavailable, use the plugin's supported failsafe queue. Do not use raw REST and do not hand-author YAML.
5. Persist the full verdict, every finding, scores, structured verdict, and receipt paths in the session log or supported session-submit failsafe.
6. Explicitly report failsafe use, every resulting failsafe path, SHA-256, pending count, quarantine count, and PrimaryReadback true or false. Failsafe retention is not primary persistence. Require drain and primary readback before any done-state change.

Review target:
- User request: run refresh-docs, then explicitly "Use failsafe".
- This is project documentation work. No commit, push, requirement/TODO completion, plan completion, or product implementation is claimed.
- Native VICE is the immutable target. The nested native checkout has pre-existing dirt; do not call it clean and do not attribute it to this refresh without evidence.
- VICE .vsf is the canonical required ViceSharp snapshot format since day one. Managed VSF staging already exists and must not be described as absent or secondary.
- UWP/Xbox product implementation is out of current scope, but historical requirements remain active and are not retired.

Final refresh slice:
- README.md
- HANDOFF.md
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

A. Requested result
A1. Current documentation reflects repository code and does not claim broad VICE parity.
A2. Historical completion evidence is bounded and not promoted into current acceptance.
A3. Snapshot docs accurately distinguish proprietary product formats from canonical VICE .vsf and acknowledge existing incomplete harness staging.
A4. xvic is described exactly: topology generation exists, console hardcodes x64sc, generic kind dispatch directly recognizes only C64 and C1541.
A5. VIC-20 settings visibility matches SettingsView and AttachPanelViewModel and its focused test result: total 8, passed 8, failed 0, skipped 0.
A6. docs/wiki.yaml audit remains: schema mcp-wiki-export/v1, 37 documents, and 0 duplicate IDs, duplicate targets, missing sources, missing navigation references, or non-GitHub targets.
A7. Historical docs were retained because they remain useful evidence and are scoped as historical.
A8. Requirements wiki export through workflow.requirements.generateDocument remains blocked by backend unavailability. No raw REST or hand-authored generated substitute was used.
A9. This refresh did not edit native/vice. Report the nested checkout's pre-existing dirty state accurately.
A10. Traceability remains an open red baseline: 235 canonical, 149 referenced, 86 missing, 1,011 noncanonical; FR 132/101/31, TR 59/32/27, TEST 44/16/28.
A11. The x64sc name does not claim parity.
A12. USER-GUIDE now accurately says unknown kind values fall back to a supplied nested yaml machine specification and error only when none is supplied, matching MultiSystemBlueprint.BuildKind.
A13. HANDOFF now accurately records Phase 4 VIC-20 visibility as implemented with its narrow focused gate while Phase 3 xvic parity remains unstarted and requires an explicit start sentence.

B. Workspace rules
- PowerShell.MCP only, no Python, no raw REST, no cloud execution.
- No commit, push, deletion, destructive cleanup, or manual YAML text mutation.
- Supported MCP failsafe retention and exact blocker reporting.
- No done-state change.

C. Requirements and acceptance criteria
- VICE behavior remains the oracle; native VICE remains immutable; canonical VSF is not secondary; legacy requirements remain active.
- No FR/TR/TEST is falsely satisfied or completed.
- Blocked export and traceability debt remain explicit.

D. Current plan holistically
- Read both active plan files.
- Status prose must match receipts and must not claim implementation, primary MCP persistence, or completion.
- No plan step may be newly marked done.

Final local validation:
- git diff --check: exit 0, no output.
- stale-statement rg after remediation: no matches for the four obsolete patterns.
- Markdown links: 11 files checked, 0 broken local links.
- wiki manifest: 37 documents with all six error counts 0.
- focused visibility tests: total 8, passed 8, failed 0, skipped 0, duration 2.753s.
- latest traceability full output: F:\GitHub\vice-sharp\.mcpServer\tmp\PowerShell.MCP.Output\pwsh_output_20261004_120334_084_vr4zl0ft.vjy.txt
- prior HV full response: F:\GitHub\vice-sharp\docs\receipts\hv\20261004T170351Z-refresh-docs-failsafe-local-grok.response.jsonl
- prior HV verdict: F:\GitHub\vice-sharp\docs\receipts\hv\20261004T170351Z-refresh-docs-failsafe-local-grok.verdict.md

Verdict contract:
- Complete readable verdict plus final parseable JSON.
- Classify every check PASS, FAIL, UNKNOWN, or N/A with evidence.
- Display every FAIL and UNKNOWN.
- OverallVerdict AGREE only when AccuracyScore >= 98, CompletenessScore >= 98, FAIL = 0, UNKNOWN = 0.
- A qualifying AGREE validates only this documentation slice. It must not authorize any requirement, TODO, goal, or plan done-state while PrimaryReadback is false.
- Include model/effort, local identifiers, MCP sessionId/requestId, plugin identity, profile receipt, jsonl paths, failsafe disposition, and full structured verdict.
