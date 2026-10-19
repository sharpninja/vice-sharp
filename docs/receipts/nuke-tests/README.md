# PLAN-NUKETESTS-001 receipts, LAB-OMARCHY (Linux), 2026-10-19

Plan: `docs/plans/PLAN-NUKETESTS-001.md`, approved by Payton on 2026-10-19 ("I approve the change"). Executed by Claude Code (claude-opus-5-5) on LAB-OMARCHY, workspace `/home/sharpninja/github/vice-sharp`, branch `develop`, HEAD `6fb84afcc1ab980c4b40525efe23324183a3c8f4` plus the uncommitted working tree described here, between 2026-10-19T11:51:51 and 12:02:20 local. Remediation phase: no hostile review (D3); no requirement, TODO or plan state was changed.

## Chronology (BDPv4 order)

1. Step 1, baseline before any edit (`20261019T115300-baseline-linux/`): `ViceSharp.Library.Tests` 214 total, 214 passed, 0 failed, 0 skipped. Moving classes: `UserLocalDeploymentTests` 13 passed, `CompletionTestManifestTests` 77 passed. Staying: `PackageMetadataTests` 2 passed.
2. Step 2, mock gate before any production edit (`20261019T115437-mock-gate-linux/`): new project `build.tests/ViceSharp.Build.Tests.csproj` with only the placement rules and fixture tests. `BuildPlacementMockGateTests` 15 total, 15 passed, 0 failed, 0 skipped: 7 positive cases and 8 negative controls, each asserting that a wrong fixture is rejected (build/ links with either separator, the project listed in a fixture solution three ways, four wrong host-filter fakes) plus the `buildtools`/`build.tests` prefix boundary.
3. Step 3, copy (`20261019T115600-copy/copy-receipt.json`): the two test files copied into `build.tests/Packaging/` and `build.tests/Completion/`, SHA256 equal to the originals before editing (`F7AE10E4...2AB4`, `5AEF7302...2E4B`). The originals are retained in `tests/ViceSharp.Library.Tests/` and excluded there by `Compile Remove` until Payton clears deletion (profile 4.6). Copies edited: namespace, class-level `[Trait("Host", "Linux")]` replacing six `SkipUnless` attributes; test bodies unchanged. Production edits: `build/BuildTestFilter.cs`, `build/Build.Tests.cs` (target `BuildTest`), `InternalsVisibleTo` in `build/_build.csproj`, the `RommDesktopTest` Windows filter removed, the two `Compile Include` links removed from the Library tests project, `ProjectReference` from the build tests to `build/_build.csproj`, and `BuildPlacementTests` binding the rules to the real tree.
4. Step 4 gates:
   - `20261019T115747-bound-linux/`: `ViceSharp.Build.Tests` 109 total, 109 passed, 0 failed, 0 skipped (completion manifest 77, deployment 13, mock gate 15, bound guards 4).
   - `20261019T115801-library-after-linux/`: `ViceSharp.Library.Tests` 124 total, 124 passed, 0 failed, 0 skipped (214 minus 90 moved).
   - `20261019T115815-host-filter-simulation-linux/`: the Windows filter `Host!=Linux` applied by hand on Linux selects 96 of 109, 96 passed, 0 skipped; the 13 deployment cases are not selected.
   - `20261019T120000-targets-linux/` (first run, retained): `./build.ps1 BuildTest` exit 0 (109/109), `./build.ps1 Compile` exit 0 (0 warnings, 0 errors), `tests/packaging/Verify-DeployYay.ps1` exit 1. The plan wrongly said that script does not depend on the moved files; it ran the deployment cases from the Library tests project and found 0 of them. Fixed in `tests/packaging/Verify-DeployYay.ps1` (project path now `build.tests/ViceSharp.Build.Tests.csproj`, analyzer 0 findings), and `docs/testing/RomM-Desktop.md` updated for the removed Windows filter (propagation audit `~/codex-dispatch/scripts/Find-MovedTestReferences.ps1`).
   - `20261019T120500-targets-linux/` (after the fix): `BuildTest` exit 0, selected 109, passed 109, host filter none; `Compile` exit 0, 0 warnings, 0 errors; `Verify-DeployYay.ps1` exit 0, "PASS: 13 installer tests; zero failures/skips; actual desktop launch verified; DeployYay delegates only to DeployUser"; `git diff --check` clean; no trailing whitespace in the new untracked files. In this folder `summary.json` holds the step log lines instead of bare exit codes (a defect in the runner script); `run.log` is authoritative.

## Observations not caused by this change (inference labeled)

- Every Nuke start logs "[WRN] Exception was suppressed: ... Could not load file or assembly 'NuGet.Frameworks, Version=7.9.0.0' ... GetTargetFrameworkIdentifier(net10.0)". The build project pins `NuGet.Packaging` 7.6.0 while SDK 10.0.402 ships NuGet 7.9. Inference: pre-existing, because this change adds only an `InternalsVisibleTo` item to the build project; not proved by a run on the unmodified tree.
- `docs/Project/wiki/github/RomM-Desktop-Validation.md` still describes the old Windows exclusion; it is a generated wiki export and is refreshed by `PublishWiki`, not edited by hand.

## Final tree (after Payton's "Delete" and commit approval, 2026-10-19)

The two retained originals were deleted at 14:26 by `~/codex-dispatch/scripts/Remove-RetainedOriginals.ps1` (WhatIf first; each original re-verified against the copy-receipt SHA256 before removal) and the two `Compile Remove` items removed. Gates on the final tree: `20261019T142733-library-final-linux/` `ViceSharp.Library.Tests` 124/124/0/0; `20261019T143500-targets-linux/` `BuildTest` 109/109 (host filter none), `Compile` 0 warnings 0 errors, `Verify-DeployYay.ps1` PASS 13, `git diff --check` clean.

## Not yet done

Step 5 (Windows gates on PAYTON-LEGION2) and step 6 (TR-BUILD-001 ac-12 and the new TEST record on LEGION2) run after this change reaches `origin/develop`.
