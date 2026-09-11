# TR-Build-System: Build System Technical Requirement

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Quality Area   | CI/CD / Build Automation       |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-09-10                     |

---

## TR-BUILD-001: Nuke Build with GitHub as the git source of truth

**ID:** TR-BUILD-001
**Title:** Nuke Build System with GitHub origin as Source of Truth
**Priority:** P0 -- Critical
**Category:** Build / CI/CD

### Description

ViceSharp shall use the Nuke build system for all build, test, package, and publish operations. The build definition is a C# project (`build/_build.csproj`) that defines targets as code. GitHub `origin` (`https://github.com/sharpninja/vice-sharp.git`) is the git source of truth. Azure DevOps is retired and is not used. Historical `azure-pipelines.ci.yml` and `azure-pipelines.release.yml` files may remain in the tree; they are not the live git or wiki authority.

### Rationale

Nuke provides a strongly-typed, IDE-debuggable build system written in C#, matching the project language. GitHub is the deployment and collaboration remote. Local Nuke targets (`PublishNuget`, `PublishMsi`, `PublishGitHubRelease`, `PublishWiki`) are the supported publish path.

### Technical Specification

1. **Nuke Build Project:**
   - The `_build` project (`build/_build.csproj`) is a .NET console application using the Nuke framework.
   - Build targets are: `Clean`, `Restore`, `Compile`, `GitCommit`, `Test`, `CiTest`, `DeterminismTest`, `ParityTest`, `RomFetch`, `PackNuget`, `Pack`, `PublishNuget`, `RunConsole`, `RunAvalonia`, `PublishWiki`, `PublishMsi`, `InstallMsi`, `PublishWinget`.
   - Target dependencies form a DAG (directed acyclic graph) with correct ordering.
   - The build can be executed locally via `build.ps1` (Windows), `build.sh` (Linux/macOS), or the `nuke` CLI.

2. **Git remote:**
   - `origin` is GitHub `sharpninja/vice-sharp`. Push and PR target `origin` / `main`.
   - Azure DevOps git, wiki, and pipelines are retired and are not used.

3. **CI and publication:**
   - Supported gates are local or operator-run Nuke targets (`Compile`, `Test`, `CiTest`, `DeterminismTest`, `PublishNuget`, `PublishMsi`, `PublishGitHubRelease`, `PublishWiki`).
   - `.github/` currently holds `FUNDING.yml` and `copilot-instructions.md`. There is no `.github/workflows` tree. A GitHub Actions workflow is possible future work, not a live requirement.
   - Historical Azure Pipelines YAML may remain in the repo. Do not treat those files as an active Azure service.

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
3. `nuke CiTest` (or `./build.ps1 CiTest`) runs the documented category filter locally. Historical Azure `VICE-Sharp-CI` numbers are not a live gate.
4. A `v*` tag plus Nuke `PublishNuget` / `PublishGitHubRelease` packs and publishes packages versioned exactly to the tag. There is no live Azure release pipeline.
5. NuGet packages are versioned correctly: the release tag version wins on tagged HEADs, GitVersion-derived versions apply otherwise.
6. Build failures produce clear, actionable error messages with the failing target and step identified.

### Verification Method

- Local build execution on developer machines.
- Operator-run Nuke logs reviewed for completeness and timing. Historical Azure pipeline logs are not a live gate.
- Version string inspection on built packages (PackNuget verifies package contents and nuspec dependencies before declaring success).

### Related TRs

- TR-PLAT-001 (Platform support; operator-run Nuke on Windows)

### Design Decisions

- Nuke is preferred over MSBuild-only or FAKE because it provides C# build logic that is debuggable in the IDE.
- GitHub `origin` is the git source of truth. Azure DevOps is retired and is not used.
- Packages publish to nuget.org via Nuke `PublishNuget`. Historical Azure pipeline YAML may remain in the tree.
- GitHub remains a mirror only; no GitHub Actions CI exists (potential future work for community PR validation).
- The `_build` project uses the same .NET SDK version as the main solution (specified in `global.json`).
