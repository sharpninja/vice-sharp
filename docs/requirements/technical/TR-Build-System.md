# TR-Build-System: Build System Technical Requirement

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Quality Area   | CI/CD / Build Automation       |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-07-08                     |

---

## TR-BUILD-001: Nuke Build with Azure DevOps CI/CD (CI + Release Pipelines)

**ID:** TR-BUILD-001
**Title:** Nuke Build System with Azure DevOps CI and Release Pipelines
**Priority:** P0 -- Critical
**Category:** Build / CI/CD

### Description

ViceSharp shall use the Nuke build system for all build, test, package, and publish operations. The build definition is a C# project (`build/_build.csproj`) that defines targets as code. Two Azure DevOps pipelines are maintained: `VICE-Sharp-CI` (Nuke-generated `azure-pipelines.ci.yml`) and `VICE-Sharp-Release` (Nuke-generated `azure-pipelines.release.yml`), both running on the self-hosted `Default` agent pool. GitHub is a downstream mirror synced on demand; it carries no CI.

### Rationale

Nuke provides a strongly-typed, IDE-debuggable build system written in C#, matching the project language. Azure DevOps is the primary source of truth and the deployment authority; splitting CI validation from tag-triggered release publication keeps the release path auditable and repeatable.

### Technical Specification

1. **Nuke Build Project:**
   - The `_build` project (`build/_build.csproj`) is a .NET console application using the Nuke framework.
   - Build targets are: `Clean`, `Restore`, `Compile`, `GitCommit`, `Test`, `CiTest`, `DeterminismTest`, `ParityTest`, `RomFetch`, `PackNuget`, `Pack`, `PublishNuget`, `RunConsole`, `RunAvalonia`, `PublishWiki`, `PublishMsi`, `InstallMsi`, `PublishWinget`.
   - Target dependencies form a DAG (directed acyclic graph) with correct ordering.
   - The build can be executed locally via `build.ps1` (Windows), `build.sh` (Linux/macOS), or the `nuke` CLI.

2. **Azure DevOps Pipelines (Primary and Only CI):**
   - `azure-pipelines.ci.yml` (pipeline `VICE-Sharp-CI`) is Nuke-generated and triggers on the `master`, `main`, and `feat/*` branches, with PR validation against `master`/`main`.
   - CI runs the `CiTest` target; `CiTest` stages hash-pinned ROMs via `EnsureCiRomRoot` so agents without a VICE data root still exercise ROM-dependent tests.
   - `azure-pipelines.release.yml` (pipeline `VICE-Sharp-Release`) triggers on `v*` tags and runs `PublishNuget`, which restores, builds, packs, and pushes all 13 NuGet packages to nuget.org in a single self-sufficient job.
   - Both pipelines run on the self-hosted `Default` agent pool (Windows).

3. **GitHub Mirror (No CI):**
   - GitHub is a downstream mirror synced on demand; `.github/` contains only `FUNDING.yml` and `copilot-instructions.md`.
   - No GitHub Actions workflows exist; a community-facing Actions mirror is not implemented and remains possible future work.
   - Azure DevOps is the single source for artifact publication.

4. **Build Targets:**
   - `Clean`: Removes all `bin`/`obj`/`artifacts` directories.
   - `Restore`: Runs `dotnet restore`.
   - `Compile`: Builds the solution with TreatWarningsAsErrors.
   - `Test`: Runs unit tests excluding `Category=Determinism` and `Category=AiReview`.
   - `CiTest`: Runs tests with filter `Category!=Determinism&Category!=AiReview&Category!=ParityPending&Category!=ParityLegacy` and stages hash-pinned CI ROMs (`EnsureCiRomRoot`).
   - `DeterminismTest`: Runs only `Category=Determinism` (bit-exact replay checks).
   - `ParityTest`: Runs the lockstep/parity gates against VICE `x64sc`.
   - `RomFetch`: Fetches ROM data for local runs.
   - `PackNuget` / `Pack`: Packs the `ViceSharp.Core` bundle (Abstractions, Chips, RomFetch, Core, Architectures in one package) plus 12 individual packages (SourceGen, Protocol, Monitor, Launcher, AdhocHelper, Host, Avalonia, Console, Host.MacOS/Android/iOS/Xbox); `ViceSharp.Console` and `ViceSharp.Avalonia` are dotnet tools. Package contents are verified before success.
   - `PublishNuget`: Publishes the 13 v-tagged release packages to nuget.org.
   - `RunConsole` / `RunAvalonia`: Launch the CLI shell / desktop UI.
   - `PublishWiki`: Regenerates requirements wiki exports.
   - `PublishMsi` / `InstallMsi` / `PublishWinget`: Package and install the Avalonia desktop app (WiX MSI, winget manifest).

5. **Versioning:**
   - Version is derived from Git history via GitVersion (`GitVersion.yml`); `next-version` pins the major.minor base and the commit height auto-increments the build field.
   - The MSI/winget ProductVersion is `{Major}.{Minor}.{CommitsSinceVersionSource}`.
   - A `vX.Y.Z` release tag on HEAD overrides the pack version so published package versions equal the tag (e.g. `v1.0.2`, released 2026-07-08).
   - The default branch is `master`; day-to-day work happens on `main` and feature branches.

### Acceptance Criteria

1. `./build.ps1 Compile` (or `nuke Compile`) succeeds locally.
2. `nuke Test` and `nuke CiTest` run the test suite with the documented category filters and report results in a structured format (TRX).
3. The `VICE-Sharp-CI` pipeline completes the `CiTest` gate on the self-hosted `Default` pool. Baseline at v1.0.2: Failed 0 / Passed 2594 / Skipped 21 / Total 2615, single process, filter `Category!=Determinism&Category!=AiReview&Category!=ParityPending&Category!=ParityLegacy`.
4. The `VICE-Sharp-Release` pipeline, triggered by a `v*` tag, packs and publishes all 13 NuGet packages versioned exactly to the tag.
5. NuGet packages are versioned correctly: the release tag version wins on tagged HEADs, GitVersion-derived versions apply otherwise.
6. Build failures produce clear, actionable error messages with the failing target and step identified.

### Verification Method

- Local build execution on developer machines.
- Pipeline execution logs reviewed for completeness and timing.
- Version string inspection on built packages (PackNuget verifies package contents and nuspec dependencies before declaring success).

### Related TRs

- TR-PLAT-001 (Platform support; CI currently runs on the self-hosted Windows pool)

### Design Decisions

- Nuke is preferred over MSBuild-only or FAKE because it provides C# build logic that is debuggable in the IDE.
- Azure DevOps is the primary CI/CD because it hosts the primary repository; packages publish to nuget.org.
- The pipeline YAML files are Nuke-generated (`nuke --generate-configuration`), keeping pipeline definitions in C# build code.
- GitHub remains a mirror only; no GitHub Actions CI exists (potential future work for community PR validation).
- The `_build` project uses the same .NET SDK version as the main solution (specified in `global.json`).
