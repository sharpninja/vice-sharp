# Hostile validation rerun 2: remediated ViceSharp refresh-docs using MCP failsafe

You are the independent local hostile reviewer for F:\GitHub\vice-sharp. Run entirely locally. No web, cloud execution, remote threads, remote CI, subagents, or repository edits.

This is the fresh full rerun after the review in docs/receipts/hv/20261004T172348Z-refresh-docs-final-failsafe-local-grok.verdict.md returned DISAGREE 92/94 with two failures. Both are now remediated:
1. HANDOFF.md no longer says Phases 3 and 4 are both unstarted. It now says Phase 2 has not exited, Phase 3 xvic parity has not started, and the later Phase 4 visibility behavior has only its narrow focused gate.
2. docs/USER-GUIDE.md no longer calls C64/C1541 kind matching case-sensitive. It now says direct dispatch is case-insensitive and other kinds require nested yaml or fail.

Before validation:
1. Run the complete Grok add-profile skill and read every required profile file in full.
2. Read AGENTS-README-FIRST.yaml, AGENTS.md, and HANDOFF.md.
3. Bootstrap and use the marker-required local Grok MCP plugin.
4. Open and complete a dedicated session-log turn. If the backend is unavailable, use only the supported plugin failsafe. No raw REST and no handwritten YAML.
5. Persist the entire verdict and structured result in session log or the supported session-submit failsafe.
6. Report all failsafe paths, hashes, pending and quarantine counts, disposition, local turn status, and PrimaryReadback.

Scope:
- Documentation refresh only. No commit, push, product implementation, requirement/TODO/plan completion, or done-state change.
- Native VICE is immutable and remains the behavioral oracle. The nested checkout has pre-existing dirt and must not be called clean.
- VICE .vsf is the canonical required snapshot format. Managed VSF staging already exists and is incomplete/product-unexposed.
- UWP/Xbox implementation is out of current scope. Historical requirements remain active and are not retired.

Review the complete current contents and scoped diff for:
README.md
HANDOFF.md
docs/README.md
docs/Architecture.md
docs/Iteration-Roadmap.md
docs/USER-GUIDE.md
docs/VICE-MIGRATION.md
docs/Public-API.md
docs/plan.md
docs/wiki.yaml
docs/plans/PLAN-VICE-IMMUTABLE-001.md
docs/plans/PLAN-VSFLOCKSTEP-001.md

A. Requested result:
A1 accurate code-facing docs with no broad parity claim.
A2 bounded historical evidence.
A3 exact snapshot truth: proprietary current product formats, canonical VSF requirement, existing incomplete managed staging.
A4 exact xvic topology/console/kind-dispatch status.
A5 exact VIC-20 settings visibility behavior and focused test count: 8 passed, 0 failed, 0 skipped.
A6 wiki manifest: mcp-wiki-export/v1, 37 documents, all six error counts 0.
A7 honest historical-document retention.
A8 blocked generateDocument export with no raw/manual substitute.
A9 no native VICE edits by this refresh; existing nested dirt reported.
A10 traceability openly red: 235 canonical, 149 referenced, 86 missing, 1,011 noncanonical; FR 132/101/31, TR 59/32/27, TEST 44/16/28.
A11 x64sc name does not prove parity.
A12 unknown kind fallback and case-insensitive C64/C1541 direct dispatch match MultiSystemBlueprint.BuildKind.
A13 HANDOFF consistently records Phase 2 not exited, Phase 3 unstarted, and only the narrow Phase 4 visibility gate implemented.
A14 no remaining stale statement that SelectedMachineProfile fails to notify IsVic20Selected.

B. Workspace rules:
PowerShell.MCP only, no Python, no raw REST, no cloud, no commit/push/deletion/destructive cleanup, no handwritten YAML, no done-state change.

C. Requirements:
VICE oracle, native immutability, canonical VSF, legacy requirements active, no false FR/TR/TEST satisfaction, export blocker and traceability debt explicit.

D. Plans:
Read both active plans. Their statuses must match receipts and must not claim implementation, primary persistence, or completion. No checked steps added.

Final evidence:
- git diff --check exit 0 with no output after remediation.
- obsolete-statement scan returned no matches.
- Markdown link audit: 11 files, 0 broken.
- wiki audit: 37 documents, all six error counts 0.
- focused visibility tests: 8 passed, 0 failed, 0 skipped.
- traceability output: F:\GitHub\vice-sharp\.mcpServer\tmp\PowerShell.MCP.Output\pwsh_output_20261004_120334_084_vr4zl0ft.vjy.txt
- failed prior review full response: docs/receipts/hv/20261004T172348Z-refresh-docs-final-failsafe-local-grok.response.jsonl
- failed prior verdict: docs/receipts/hv/20261004T172348Z-refresh-docs-final-failsafe-local-grok.verdict.md

Verdict contract:
- Full readable verdict plus final parseable JSON.
- Every item PASS/FAIL/UNKNOWN/N/A with evidence.
- Display every FAIL and UNKNOWN.
- AGREE only when AccuracyScore >=98, CompletenessScore >=98, FAIL 0, UNKNOWN 0.
- Even AGREE validates only this docs slice and cannot authorize done-state while PrimaryReadback is false.
- Include model/effort, local identifiers, session/request IDs, plugin identity, profile receipt, jsonl paths, and failsafe receipts.
