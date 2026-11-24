# PLAN-NUKETESTS-001: Nuke build tests move to a project beside the build project

Status: approved 2026-10-19; steps 1 to 4 green on LAB-OMARCHY (`docs/receipts/nuke-tests/README.md`), committed `fa303b4`; step 5 green on PAYTON-LEGION2 and step 6 captured there (2026-10-19T14:36, receipts `docs/receipts/nuke-tests/20261019T143210-targets-windows/` and `20261019T143442-requirement-capture-legion2/` on LEGION2); step 6 mirrored to LAB-OMARCHY at 14:40:31 with readback equal to LEGION2. All six steps are executed; the plan is not marked done before the code-complete review (D3). No mapping row for the new TEST record: this plan changes no FR and mappings are FR-keyed. Nothing marked done before the code-complete review (D3). Written 2026-10-19 on LAB-OMARCHY by Claude Code (claude-opus-5-5) on branch `develop`. Source of the requirement change: Payton, 2026-10-19, "These should be nuke tests, which are not project tests. Nuke's tests should be located as sibling to the nuke build project, not sibling to the project." Location chosen by Payton: option A, a root-level sibling of `build/`. Recorded in `docs/receipts/handoff/HANDOFF.lockstep-plan.v2.md` section 11 (D9, sequence step 2b). This is part of the remediation phase: no hostile review runs until remediation is code complete (D3).

## 1. Current state (measured 2026-10-19T10:57 to 11:00, receipts `~/codex-dispatch/diag/nuke-test-plan-20261019/`)

- `build/_build.csproj` (Nuke 10.1.0, net10.0, Exe) holds `build/UserLocalDeployment.cs` (269 lines, `internal` types `UserDeploymentPaths`, `UserDeploymentResult`, `UserLocalDeployment`, used by target `DeployUser` in `build/Build.Yay.cs` lines 130 to 164) and `build/CompletionTestManifest.cs` (namespace `ViceSharp.Build.Completion`). The build project is not in `ViceSharp.slnx`.
- `tests/ViceSharp.Library.Tests/ViceSharp.Library.Tests.csproj` lines 30 to 33 compile both build files into the product test assembly by `Compile Include="..\..\build\..." Link="Build\..."`.
- Tests of build code inside the product test project: `Packaging/UserLocalDeploymentTests.cs` (6 methods, 13 cases, each `SkipUnless = nameof(IsLinux)`) and `CompletionTestManifestTests.cs` (21 test methods, 6 of them theories; case count measured in step 1). `Packaging/PackageMetadataTests.cs` reads product `.csproj` files and the text of `build/Build.cs`; its subject is product packaging metadata, so it stays a product test.
- `build/Build.Romm.cs` line 33 to 34: `RommDesktopTest` filters the 13 cases out on Windows (`FullyQualifiedName!~ViceSharp.Library.Tests.Packaging.UserLocalDeploymentTests`).
- `Test` and `CiTest` (`build/Build.cs` lines 97 and 133) run the whole solution with a category filter, so today the 13 cases are skipped on Windows inside the product suite.
- `tests/packaging/Verify-DeployYay.ps1` and `Verify-ArchPackage.ps1` are the PowerShell verifications named by TR-BUILD-001; they are not part of this move.
- Requirements (LAB-OMARCHY mirror, `listTr`): TR-BUILD-001 ac-10 and ac-11 state the user-local deployment contract the 13 cases prove; TR-PLAT-001 ac-8 ends "User-local installation never installs native system packages." No TEST record covers user-local deployment or the completion manifest (`listTest`: 134 records, none relevant).

## 2. Decisions (locked by this plan unless Payton changes them)

1. Scope: every test whose subject is code in `build/` moves, which is `UserLocalDeploymentTests` and `CompletionTestManifestTests`. Payton's rule names "nuke tests", and the completion-manifest tests are Nuke tests by the same definition. `PackageMetadataTests` stays (its subject is product metadata). If Payton wants only the 13 moved, step 3 drops `CompletionTestManifestTests` and the second `Compile Include` stays.
2. Location and name: `build.tests/ViceSharp.Build.Tests.csproj` at the repository root, beside `build/` (option A). xunit.v3, net10.0, same test packages as the Library tests.
3. Binding to build code: `ProjectReference` to `build/_build.csproj` plus `InternalsVisibleTo("ViceSharp.Build.Tests")` in the build project, so the tests exercise the real build assembly instead of a second compilation of linked files. Fallback if referencing the Exe build project fails to restore or build: keep linked `Compile Include` items in the new project (the current pattern), recorded as a plan amendment.
4. The new project is not added to `ViceSharp.slnx`, so `Test`, `CiTest` and every solution-wide run stop seeing these tests. Product denominators drop by the moved case counts on both hosts.
5. A new Nuke target `BuildTest` runs the project, writes a TRX under `TestResults/build-tests/<yyyyMMddTHHmmss>/`, and fails when zero tests are selected or any selected test does not pass (the rule `RommDesktopTest` already enforces).
6. Host-specific cases are selected by host, never skipped: the 13 deployment cases get `[Trait("Host", "Linux")]` in place of `SkipUnless`, and `BuildTest` adds `Host!=Linux` to the filter on non-Linux hosts. The Windows `BuildTest` denominator is stated in its log as "selected N, excluded by host M".
7. `RommDesktopTest` loses its Windows-only filter (nothing to filter once the 13 cases leave `ViceSharp.Library.Tests`).
8. Windows deployment contract tests (`PublishMsi`, `InstallMsi`, `PublishWinget`, `DeployXbox`) are not in this plan. They are a separate requirement change, already sequence step 10 of handoff section 11; they will land in this same project when planned. Section 11 step 2b is corrected to say so (propagation fix).
9. Test bodies, inputs and assertions move unchanged. Only the namespace, the `SkipUnless` attribute (decision 6) and the repository-root helper (if the new project needs its own `RepoRoot` metadata) change.

## 3. Requirement changes (captured on PAYTON-LEGION2 after approval; single-writer rule)

- TR-BUILD-001, new ac-12: "Tests of Nuke build code live in `build.tests/ViceSharp.Build.Tests.csproj` beside `build/`, outside `ViceSharp.slnx`, and run through `nuke BuildTest`. No product test project compiles or tests files from `build/`. `BuildTest` fails when it selects zero tests or any selected test does not pass, and host-specific cases are excluded by host filter, never skipped."
- TR-BUILD-001 Technical Specification section 1 adds `BuildTest` to the target list and a `BuildTest` bullet to section 4; Verification Method adds "`./build.sh BuildTest` / `./build.ps1 BuildTest` TRX on each host, with the host-excluded count."
- New TEST record (id fixed at capture time as the next free `TEST-BUILD-###` on LEGION2, after checking that none exists): "Nuke build tests". Criteria: (1) the 13 user-local deployment cases pass on Linux through `BuildTest` and prove TR-BUILD-001 ac-10 and ac-11; (2) the completion-manifest cases pass on every host through `BuildTest`; (3) the placement guards of step 2 pass; (4) the Windows `BuildTest` run selects no Linux-host case and reports the excluded count. Traceability: TR-BUILD-001 ac-10, ac-11, ac-12; TR-PLAT-001 ac-8.
- No FR changes. No status or satisfied flag is set by this plan (D3 and D8: nothing marked done before the code-complete review).

## 4. Slices and gates (BDPv4, mocks first)

Step 1. Baseline, no edits. Run `ViceSharp.Library.Tests` on LAB-OMARCHY and record total, passed, failed, skipped, and the case counts of the two moving classes (TRX under `docs/receipts/nuke-tests/<stamp>-baseline/`). The LEGION2 agent records the Windows baseline the same way.

Step 2. Placement guards, mock gate first. New tests in `build.tests`: (a) `ProductTestProjects_DoNotCompileBuildSources` scans every `tests/**/*.csproj` for `Compile` items whose path resolves under `build/`; (b) `BuildTestsProject_IsNotInSolution` reads `ViceSharp.slnx`; (c) `BuildTestFilter_ExcludesLinuxHostCasesOnNonLinux` calls a pure helper `BuildTestFilter.For(isLinux)` that `BuildTest` also uses. Mock gate: each guard runs against fixture inputs (a fixture csproj with and without a `..\..\build\` link, a fixture slnx with and without the project, both host values), correct fixtures pass, and each wrong fixture is asserted to be rejected (negative controls inside a passing suite). Gate: all guard tests green against fixtures, every negative control rejected, zero skipped. No production edit before this gate.

Step 3. Move. Create the project, add `InternalsVisibleTo` to the build project, move the two test files with git (history kept), replace `SkipUnless` with the `Host` trait, delete the two `Compile Include` items from the Library tests project, add `BuildTest` (using `BuildTestFilter.For`), remove the `RommDesktopTest` Windows filter. Bind the step 2 guards to the real tree.

Step 4. Gates on LAB-OMARCHY: `./build.sh BuildTest` green (all moved cases plus guards, zero skipped; the 13 Linux cases selected); `ViceSharp.Library.Tests` green with its count equal to the step 1 baseline minus the moved cases; `./build.sh Compile` green with TreatWarningsAsErrors; `git diff --check` clean; `pwsh ./tests/packaging/Verify-DeployYay.ps1` still green (it does not depend on the moved files, but it exercises `DeployUser`).

Step 5. Gates on PAYTON-LEGION2 (LEGION2 agent, after the change reaches `develop` by Payton's commit order): `./build.ps1 BuildTest` green with the 13 Linux cases excluded by host and the excluded count logged; `ViceSharp.Library.Tests` green with zero skipped; `./build.ps1 RommDesktopTest` green without its old filter.

Step 6. Requirement capture on LEGION2 (section 3 text, exactly as approved), mirrored to LAB-OMARCHY by the additive sync. Self-assessment with accuracy and completeness ratings written into the receipt folder.

## 5. Blast radius

Files: new `build.tests/ViceSharp.Build.Tests.csproj` and the guard tests; `build/_build.csproj` (one `InternalsVisibleTo` item); `build/Build.cs` or a new `build/Build.Tests.cs` partial (target `BuildTest`); `build/Build.Romm.cs` (filter removed); `tests/ViceSharp.Library.Tests/ViceSharp.Library.Tests.csproj` (two link items removed); the two test files moved. No product source, no `native/`, no package version change. Not breaking for product code. Behavioral change for operators: `Test` and `CiTest` no longer run the build-code tests, so `BuildTest` must be run beside them; TR-BUILD-001 ac-12 makes that explicit.

## 5a. Approval and amendments

Approved by Payton on 2026-10-19 ("I approve the change", session-log turn `req-20261019T165118Z-prompt-cf52`). Amendments made during execution:
- Step 3 says "move the two test files with git". Profile rule 4.6 ("NEVER Move a file. Always copy, verify and RETAIN original until I tell you it's clear to delete it.") overrides that wording: the two files were copied into `build.tests/` with SHA256 verification (`docs/receipts/nuke-tests/20261019T115600-copy/copy-receipt.json`), the originals were retained in `tests/ViceSharp.Library.Tests/` and excluded from that project by `Compile Remove` until Payton cleared deletion ("Delete", 2026-10-19); they and the `Compile Remove` items were then removed after re-verifying each original's SHA256.
- Step 4 says `tests/packaging/Verify-DeployYay.ps1` "does not depend on the moved files". That was wrong: it ran the deployment cases from `ViceSharp.Library.Tests` and failed after the move (first targets run). The script now points at `build.tests/ViceSharp.Build.Tests.csproj` and passes; `docs/testing/RomM-Desktop.md` was updated for the removed Windows filter. Both are added to the blast radius.
- Decision 6 says the Windows log states "selected N, excluded by host M". The run cannot count excluded cases without a second discovery pass, so M is pinned by the guard `LinuxHostTrait_MarksOnlyTheDeploymentContract` (exactly one class, 6 methods, 13 cases carry `Host=Linux`) and `BuildTest` logs the selected count and the filter. The Linux simulation of the Windows filter selected 96 of 109 (`docs/receipts/nuke-tests/20261019T115815-host-filter-simulation-linux/`).

## 6. Risks

- Referencing an Exe Nuke project from a test project may pull Nuke's build-time assembly attributes or package assets that conflict with xunit.v3 (decision 3 fallback covers it).
- `CompletionTestManifestTests` reads test project paths relative to the repository root; the new project needs the same root lookup (decision 9).
- The Windows run depends on the LEGION2 agent and on a commit order.
