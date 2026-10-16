# Hostile plan review: immutable VICE remediation rerun

You are the HOSTILE VALIDATOR for the local workspace F:\GitHub\vice-sharp.

This is independent review only. Do not edit product code, plans, requirements Markdown, native/vice, TODO state, goal state, or git state. Do not use web search, cloud runners, hosted agents, remote threads, remote repositories, or delegated subagents. Work only on this local machine. Do not set or infer a turn cap or artificial time limit.

FIRST ACTION, mandatory before validation:
Execute the add-profile skill and follow it literally. Read every required non-skill Markdown file under C:\Users\kingd\.claude\profile in full, excluding only add-profile.grok.md. Record the exact file count. Then read F:\GitHub\vice-sharp\AGENTS-README-FIRST.yaml and F:\GitHub\vice-sharp\AGENTS.md before evaluating claims.

Review class: project planning and requirements remediation. This is a plan gate, not a claim that product implementation, requirement persistence, Phase 0, Phase 1, or the whole plan is complete.

Operator constraints that are acceptance criteria:
1. Native VICE at gitlink commit cc67418cba58e71d8e70fd353e9de47199ad2075 is the immutable target. Never edit, patch, generate into, or behaviorally change native/vice.
2. External wrappers may observe or adapt around the target only from outside native/vice. Missing observability is a named blocker, never permission to change VICE or fabricate values.
3. Native VICE VSF is the canonical ViceSharp snapshot format and is day-one primary behavior, not secondary functionality.
4. Managed VSF support already existed. The plan must preserve that work and restore canonical native-VSF behavior.
5. TR-VSFLOCKSTEP-RESUME-001 remains active and must preserve legacy MAINCPU v1.2 resume. It is not retired.
6. UWP/Xbox implementation is out of scope, but legacy requirements remain active and are not retired.
7. All work is local in this chat. Only local Grok may perform hostile validation.
8. This Grok launch has no --max-turns and no artificial wall-clock limit.
9. PowerShell only through PowerShell.MCP. No Python.
10. No commit or push is authorized.
11. Every FAIL, UNKNOWN, and score below 98 must be shown. AGREE requires AccuracyScore >= 98, CompletenessScore >= 98, FailCount 0, and UnknownCount 0.

Prior hostile review to remediate:
- Verdict: F:\GitHub\vice-sharp\docs\receipts\hv\20261004T085243-0500-immutable-vice-remediation-plan-local-grok.verdict.md
- OverallVerdict DISAGREE, AccuracyScore 96, CompletenessScore 93, FAIL A3, C6, E4.
- A3/E4: TR-VSFLOCKSTEP-RESUME-001 AC2 still mandated applying native/patches/vice-shim-runtime.patch and the plans failed to rehome legacy v1.2 behavior to an external wrapper.
- C6: the active handoff plan still mandated Cursor Agent/Gemini rather than uncapped local Grok.

Current evidence to inspect independently:
- F:\GitHub\vice-sharp\docs\plans\PLAN-VICE-IMMUTABLE-001.md
  SHA256 48BD99D5FCB0FA2F66DC9C942CF9D26B5C80B52034114405A87F9FAA18981372
- F:\GitHub\vice-sharp\docs\plans\PLAN-VSFLOCKSTEP-001.md
  SHA256 27B5843A50A7560C6FF8AE0DA3454E7ED73F89DE87698C20F2AABA0B124CE994
- F:\GitHub\vice-sharp\docs\requirements\technical\TR-Native-Vice-Snapshot.md
  SHA256 35FE3325D8E8D6D47787D29EFCB0445375E3EA3AE38D4C6EEB14E475D52999C7
- C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5Cvice-sharp\01a0ef2b-2331-70d1-9c65-0ef12b15d09a\plan.md
  SHA256 1BC22EE29B7FD28A0EBE7EAE043D7B76ACA6BA0A526C0D49CD9AAF0D714549FC
- F:\GitHub\vice-sharp\docs\requirements\functional\FR-Vice-Reference.md
- F:\GitHub\vice-sharp\docs\requirements\technical\TR-Vice-Oracle.md
- F:\GitHub\vice-sharp\docs\requirements\test\TEST-Vice-Oracle.md
- F:\GitHub\vice-sharp\docs\requirements\traceability\VICE-Immutable-Oracle-Matrix-2026-10-03.md
- F:\GitHub\vice-sharp\docs\receipts\native-vice\20261004T090000-0500-preexisting-delta\README.md
- F:\GitHub\vice-sharp\native\build-vice-shim.sh
- F:\GitHub\vice-sharp\native\patches\vice-shim-runtime.patch

Implementer claims to attack:

A1. The corrected plans treat native VICE as immutable and forbid every edit, patch, generated file, semantic change, patched binary, or dirty checkout from serving as parity evidence.

A2. The old native delta is preserved as diagnostic evidence only. No restoration or cleanup has occurred. The plans require a separate clean source materialization and out-of-tree build before any native-backed acceptance proof.

A3. The recovered legacy v1.2 snapshot behavior remains active but has been rehomed: the canonical TR and both plans now require a narrow external temporary-copy compatibility adapter outside native/vice. It adds exactly the absent v1.3 and v1.4 fields for v1.2, only the absent v1.4 field for v1.3, preserves following bytes and modules, passes v1.4 byte-for-byte, and fails closed on malformed or unsupported layout. native/patches/vice-shim-runtime.patch is diagnostic evidence only and is never applied.

A4. PLAN-VSFLOCKSTEP-001 names tests-first coverage for v1.2, v1.3, v1.4, malformed/unsupported input, clean source materialization, and before/after source-tree integrity. It does not authorize product implementation before its gates.

A5. The external handoff plan now names Codex as local executor and uncapped local Grok 4.7 at xhigh as the only hostile reviewer. It contains no operative Cursor/Gemini runner requirement. It bans web, cloud, remote CI, hosted agents, and delegated reviewers.

A6. Failsafe handling is explicit in the plans and this prompt: supported plugin v4 failsafe is allowed for HV session-log persistence during the current outage; handwritten failsafe YAML is forbidden; exact pending paths and SHA256, zero quarantine, failsafe-pending disposition, and PrimaryReadback false are mandatory; later drain plus primary queryHistory readback is required before any done-state mutation.

A7. The implementer does not claim the requirements database was updated. Both workflow.requirements.getTr and workflow.requirements.updateTr were refused by PAYTON-LEGION2:7147. v4 failsafe does not support requirement mutations, so the canonical Markdown is staged but primary update/readback and generated wiki regeneration remain a hard pre-Phase-1 gate. Decide whether the plan is safe and decision-complete under that explicit blocker. Do not convert the blocker into a false persistence claim.

A8. Parent Codex used supported v4 session-log failsafe for this remediation. Verify:
- F:\GitHub\vice-sharp\.mcpServer\failsafe\Codex\workspaces\RjpcR2l0SHViXHZpY2Utc2hhcnA\pending\20261004T144244Z-session_submit-8482.yaml
- SHA256 53547BF4DEAC637836EEC3B759257997735A9E48D689CF0E684B45670C4DC5EA
- workflow.failsafe.status reported pendingCount 5 and quarantineCount 0.
This proves queued preservation only. Primary persistence is not claimed.

Mandatory attack surfaces:
A. Reverify every claim A1-A8 from files and local commands.
B. Audit AGENTS.md, AGENTS-README-FIRST.yaml, the loaded operator profile, PowerShell-only/no-Python, receipts, local-only execution, no native mutation, no commit/push, and the BDPv4 phase boundary.
C. Audit FR-VICE-001, TR-VICE-ORACLE-001, TEST-VICE-001, FR-SNP-005, TR-LOCKSTEP-VSF-001, TR-VSFLOCKSTEP-RESUME-001, TEST-SNP-002, and TEST-VSFLOCKSTEP-001. Every FR including legacy remains active and needs appropriate acceptance criteria. Do not claim primary requirement-store state while MCP is unavailable.
D. Audit both active plans holistically, including blockers, preservation, red tests, wrapper feasibility, legacy VSF behavior, clean build provenance, xvic parity, settings visibility, UWP/Xbox scope, failsafe/drain requirements, and all done-state gates.

Required MCP session-log and failsafe behavior for this review:
1. Use the supported Grok plugin session-log workflow, never raw REST and never handwritten YAML.
2. Create a dedicated GrokCode review session/turn and attempt primary persistence.
3. If primary MCP persistence is unavailable, use the supported plugin v4 failsafe. Do not FAIL or UNKNOWN solely because the server is down if all of these are proven.
4. Call workflow.failsafe.status. Require quarantineCount 0.
5. The complete terminal result must be in the queued client.SessionLog.SubmitAsync payload: full prose, every PASS/FAIL/UNKNOWN, accuracy and completeness, OverallVerdict, structured verdict, and request/response jsonl paths.
6. Report every exact failsafe pending file used by this review with SHA256, pendingCount, quarantineCount, PersistenceDisposition: failsafe-pending, and PrimaryReadback: false.
7. State that the parent must run workflow.failsafe.drain and primary workflow.sessionlog.queryHistory readback after recovery before any goal, TODO, requirement, plan phase, or whole-plan done-state change.
8. Do not hand-edit, delete, rename, or manufacture failsafe YAML.

Required output:
- Sortable local timestamp and UTC timestamp.
- Work class.
- Profile file count.
- Per-claim PASS/FAIL/UNKNOWN for A1-A8.
- Separate B, C, and D findings.
- Full FAIL list and full UNKNOWN list.
- PassCount, FailCount, UnknownCount.
- AccuracyScore and CompletenessScore as integers 0-100.
- OverallVerdict AGREE only when both scores are at least 98 and FAIL/UNKNOWN are zero.
- GrokCode SessionId and RequestId.
- PersistenceDisposition, PrimaryReadback, pendingCount, quarantineCount, exact failsafe paths and SHA256 values.
- End with a single machine-readable JSON object containing every summary field above.

The parent will retain your full machine stream as:
F:\GitHub\vice-sharp\docs\receipts\hv\20261004T094437-0500-immutable-vice-remediation-plan-rerun-local-grok.response.jsonl
and will materialize a readable verdict beside it. Do not edit reviewed workspace artifacts.