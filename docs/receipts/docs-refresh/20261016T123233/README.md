# Documentation refresh and wrap-up

Recorded 2026-10-16T12:36:10 on PAYTON-LEGION2, `F:\GitHub\vice-sharp`.
Operator request: refresh-docs, then wrap-up. Latest correction: no more HV.
No product code, native VICE, requirements, satisfaction state or paused R0 work
was changed. No additional reviewer was launched.

## Changes

- Updated README, documentation index, user guide and Avalonia/RomM package guides
  for virtualized grouped cover tiles, selection-relative keyboard navigation,
  collections, loader ownership and connection boundaries.
- Added `docs/testing/RomM-Desktop.md` with the exact Nuke commands, mock/production
  distinction, approved Windows exclusions and prior integration receipts.
- Updated `docs/wiki.yaml` through native-object YAML deserialization and
  serialization. Added three documents (Avalonia Desktop, RomM Desktop Validation,
  Documentation Index) and navigation for those plus the existing RomM Library.
  All 41 manifest entries have sources and navigation. GitHub remains the sole
  publishing platform; the server retains empty Azure metadata in its ZIP.
- No docs pruned. Legacy requirements, historical plans, cancelled-track archives
  and receipts remain useful evidence and were retained.
- No documentation screenshot script was found among tracked scripts. No new
  live UI screenshot or visual acceptance is claimed.

## Export and checks

Supported `workflow.requirements.generateDocument`, `format=wiki`, `docType=all`
returned `application/zip`. Its exact 423,249 bytes are retained at
`docs/requirements/requirements-wiki-documents.zip`, SHA256
`72831F7E8276C9167D123D25E74A9C6E1B90C1C71AA89817D9447C6547B0EC77`.
All 46 ZIP entries match the regenerated files on disk. None of the prior 42
wiki files was removed. Five generated requirement/mapping bodies are unchanged;
their heading now names QBrain.AI. Refreshed project mirrors also incorporate
already-committed Linux ROM, credential-storage and packaging documentation.

Live inventory returned 235 FRs, 139 TRs, 142 TESTs and 747 mappings. No requirements
were created or updated. The unapplied R0 proposal remains unapplied.

[validation.json](validation.json) records the checks and hashes:

- `Validate-Docs.ps1`: parser errors 0; PSScriptAnalyzer Warning/Error findings 0;
  72 local source links valid; manifest, ZIP and requirement checks passed.
- 6,374 native baseline file hashes unchanged. This preserves preexisting native
  modifications; it is not a claim that native VICE is a clean upstream checkout.
  The validator uses the retained private integration baseline and is an audit
  receipt for this checkout, not a fresh-clone general validation target.
- `git diff --check` passed.
- `./build.ps1 RommDesktopTest --configuration Release --romm-contract-mocks`
  exited 0, 46 passed, 0 failed, 0 skipped. See
  [captured output](mock-command-check.log) and [TRX](mock-command-check.trx).
  Production-suite results are the earlier integration receipts, not rerun here.

## Retained failures and limits

- Exported project pages retain repository-relative links after relocation to
  wiki paths. Navigation is present, but some links in page bodies remain broken.
  Supported triage accepted `triage-report-9af6dae387364ba296240585a2cd3110`.
  Generated files were not hand-edited, and no server repair was performed.
- `./build.ps1 --help` under captured output listed targets, then failed with
  `The handle is invalid.` and exit -1. Restore and Compile did not run.
  See [failure output](nuke-help.log). Supported triage accepted
  `triage-report-02bc9f2423514b57a697af8ad089370e`.
- The successful mock command retains a suppressed Nuke/MSBuild NuGet.Frameworks
  assembly mismatch warning and Avalonia AVLN5001 for obsolete TextBox.Watermark.
  This task does not claim a warning-free build or repair either warning.
- No HV, live RomM session, Linux execution, full emulator suite, whole-machine
  parity acceptance, requirement completion or broader goal completion is claimed.

## Persistence and sync

MCP session `Codex-20261007T135626Z-plugin-session`, request
`req-20261016T172317Z-turn-0d59`. Export, checks, decisions, commit and push are
recorded through the supported plugin and verified by server readback.
The final commit SHA and remote result belong in that turn after the Git actions.
Unrelated `.tmp-c64wiki/`, private worktree backups and existing native changes
are excluded from the documentation commit. No wiki-repository publish was run.

Self-assessment: accuracy 99% for the source-pinned changes and recorded checks;
completeness 95% because exported body-link correctness remains a triaged limitation.
These are not hostile-validation scores or an AGREE verdict.
