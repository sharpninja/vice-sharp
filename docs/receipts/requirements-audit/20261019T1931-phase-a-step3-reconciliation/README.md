# Phase A step 3: store reconciliation (2026-10-19T19:31 to 19:39, LAB-OMARCHY)

Scope of record: `docs/receipts/handoff/HANDOFF.lockstep-plan.v2.md` section 11, sequence step 3 ("store reconciliation, now reduced to the 65 LEGION2-only ids (D11 stays C) and the status audit of step 5"). Goal TODO `GOAL-LOCKSTEP-REMEDIATION-001`, task "Step 3". Class 2 operator-directed work (operations on the requirement stores), no product code.

## What was measured

Both requirement stores were exported read-only at 19:31 with `Export-McpRequirements.ps1` through the mcpserver plugin 1.121.0 (`-SkipGenerateDocument`, so no projection was written): the LEGION2 store over the SSH route (`summary.json` in `legion2-export/`, pulled with per-file SHA256 verification, 8 files, 0 mismatches) and the LAB-OMARCHY store (`omarchy-export/summary.json`). Counts (passed, from the summaries):

- LEGION2: FR 235, TR 139, TEST 144, mapping rows 768.
- LAB-OMARCHY before: FR 235, TR 83, TEST 135, mapping rows 683.

`Compare-McpRequirementExports.ps1` (`compare-legion2-vs-omarchy.json`) found exactly two classes of difference and nothing else:

1. Ten FR records differing by description only: FR-CFG-002, FR-INP-006, FR-MED-002, FR-MED-003, FR-ROMM-LAUNCH-001, FR-SID-013, FR-SNP-001, FR-SNP-002, FR-UI-003, FR-VIC20-002. These are the embedded-heading fix that the LEGION2 agent applied on the LEGION2 store between 19:27 and 19:30 on Payton's "Yes" of 19:27 (LEGION2 local commit `cf7520c5`, receipts `docs/receipts/requirements-export/20261019T1930-fr-embedded-heading-fix` there). The 19:31 export already carried that after-state.
2. The ids the LAB-OMARCHY server rejects (D11, resolved C: left LEGION2-only): 56 TR and 9 TEST ids, plus 85 mapping rows that each reference at least one of them.

All other records are identical on both stores: FR 225, TR 83, TEST 135, mapping rows 683. No id or mapping row exists only on LAB-OMARCHY.

## What was written

`Invoke-FrHeadingMirror.ps1` (PowerShell, parser and analyzer clean, `-WhatIf` first) first proved the classification mechanically (`step3-reconciliation-proof.json`: every LEGION2-only mapping row references a LEGION2-only id; each of the 10 local descriptions equals LEGION2's description plus exactly one removed first line `## <ID>: <old title>` and the blank line after it), then sent one `workflow.requirements.updateFrBatch` with 10 description-only patches taking LEGION2's after-state (`fr-heading-mirror-apply/002-update-fr-descriptions.params.yaml`), then read the FR list back: 10 descriptions equal to LEGION2, no other field changed on those records, 225 other FR records unchanged (compared canonically, `createdAt` and `updatedAt` excluded because this read path synthesizes them).

## Result

Post-mirror export (`omarchy-export-after/summary.json`) compared against the same LEGION2 export (`compare-legion2-vs-omarchy-after.json`): FR 235 identical, 0 differing; TR 83 identical, 56 LEGION2-only; TEST 135 identical, 9 LEGION2-only; mapping rows 683 equal, 85 LEGION2-only. The stores are equal on every record both can hold. The status audit of step 5 is covered by the D8 receipts (`docs/receipts/requirements-audit/20261019T115841-d8-status-accuracy-dryrun-v2/` and the LEGION2 and LAB-OMARCHY apply receipts cited in handoff section 11).

Not done here and still open: D11 itself (renaming or server validation change for the 65 ids is a planned requirement change); the push of LEGION2 commit `cf7520c5` (Payton's call).

Originals of every file here are retained under `~/codex-dispatch/step3-reconcile-20261019/` on LAB-OMARCHY (`manifest.json` lists each copied file with its SHA256 and size). The raw export list files (`list-fr.yaml` and siblings) and the two full FR list readbacks of the mirror (`001-list-fr-before.response.yaml`, `003-list-fr-after.response.yaml`, about 450 KB each) are not copied; they are reproducible from the stores, their counts are in the summaries, and the compare reports and proof carry their conclusions.

Signed: claude-fable-5-1-high@ClaudeCode | Confidence: correctness 98%, accuracy 98%, completeness 97%
