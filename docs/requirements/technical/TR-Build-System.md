# TR-Build-System: Build System Technical Requirement

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Quality Area   | CI/CD / Build Automation       |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-10-15                     |

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
   - Build targets are: `Clean`, `Restore`, `Compile`, `GitCommit`, `Test`, `CiTest`, `DeterminismTest`, `ParityTest`, `RomFetch`, `PackNuget`, `Pack`, `PackYay`, `DeployUser`, `DeployYay`, `PublishNuget`, `RunConsole`, `RunAvalonia`, `PublishWiki`, `PublishMsi`, `InstallMsi`, `PublishWinget`.
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
   - `Test`: Runs tests with filter `Category!=Determinism&Category!=AiReview&Category!=ParityPending&Category!=ParityLegacy&Category!=Integration`.
   - `CiTest`: Runs the same category filter and stages hash-pinned CI ROMs (`EnsureCiRomRoot`).
   - `DeterminismTest`: Runs only `Category=Determinism` (bit-exact replay checks).
   - `ParityTest`: Runs the lockstep/parity gates against VICE `x64sc`.
   - `RomFetch`: Fetches ROM data for local runs.
   - `PackNuget` / `Pack`: Packs the `ViceSharp.Core` bundle (Abstractions, Chips, RomFetch, Core, Architectures in one package) plus 12 individual packages (SourceGen, Protocol, Monitor, Launcher, AdhocHelper, Host, Avalonia, Console, Host.MacOS/Android/iOS/Xbox); `ViceSharp.Console` and `ViceSharp.Avalonia` are dotnet tools. Package contents are verified before success.
   - `PublishNuget`: Publishes the 13 v-tagged release packages to nuget.org.
   - `RunConsole` / `RunAvalonia`: Launch the CLI shell / desktop UI.
   - `PublishWiki`: Regenerates requirements wiki exports.
   - `PublishMsi` / `InstallMsi` / `PublishWinget`: Package and install the Avalonia desktop app (WiX MSI, winget manifest).
   - `PackYay`: Creates a local Arch Linux package for the current `x86_64` or `aarch64` host using `makepkg`, run as a regular user. Always publishes the Avalonia project as optimized Release and self-contained, without a full-solution restore dependency. Requires `base-devel`/`makepkg`, the package's native dependencies, and a build SDK compatible with `global.json`. The installed application does not require a system .NET SDK or runtime.
   - `PackYay` outputs `PKGBUILD`, `.SRCINFO`, a SHA256-pinned `vicesharp-<version>-<arch>.tar.gz` payload, and `vicesharp-<version>-1-<arch>.pkg.tar.zst` beneath `artifacts/yay/<version>/<arch>/`. The package includes the desktop application, `vicesharp` launcher, desktop menu entry, icon, and license notices. ROM images are not bundled; missing default C64 firmware is acquired at desktop startup.
   - `PackYay` performs no package installation or AUR publication. Users may install the archive with `yay -U <package-path>` or build and install the generated recipe with `yay -Bi <recipe-directory>`.
   - `DeployUser`: Publishes a self-contained Release Linux desktop build and installs a new retained release below `$XDG_DATA_HOME/vicesharp/releases` (default `~/.local/share/vicesharp/releases`). It creates `~/.local/bin/vicesharp`, a user desktop entry and icon, then atomically switches the `current` release link. It runs as the normal user and invokes no package manager, sudo, system installation, or AUR publication.
   - `DeployUser` requires an absolute data directory within the user's home, rejects root execution and unsafe symlink or unmanaged destinations, and validates the executable payload before activation. It preserves existing `ViceSharp` settings, firmware and RomM caches, retains earlier releases, and restores entrypoints if activation fails. The installed system package, if present, is not modified.
   - `DeployYay`: Compatibility alias depending only on `DeployUser`. Existing deployment commands now use user space; `PackYay` remains the separate package-only target.

5. **Versioning:**
   - Version is derived from Git history via GitVersion (`GitVersion.yml`); `next-version` pins the major.minor base and the commit height auto-increments the build field.
   - The MSI/winget ProductVersion is `{Major}.{Minor}.{CommitsSinceVersionSource}`.
   - A `vX.Y.Z` release tag on HEAD overrides the pack version so published package versions equal the tag (e.g. `v1.0.2`, released 2026-07-08).
   - `PackYay`, `DeployUser` and `DeployYay` honor the shared `--package-version-override` parameter; otherwise they use the release tag on HEAD or the GitVersion-derived default. The Arch package release is `1`. Each user deployment gets a unique release directory, including same-version rebuilds.
   - The default branch is `main`; day-to-day work happens on `main` and feature branches.

### Acceptance Criteria

1. `./build.ps1 Compile` (or `nuke Compile`) succeeds locally.
2. `nuke Test` and `nuke CiTest` run the test suite with the documented category filters and report results in a structured format (TRX).
3. `nuke CiTest` (or `./build.ps1 CiTest`) runs the documented category filter locally. Historical Azure `VICE-Sharp-CI` numbers are not a live gate.
4. A `v*` tag plus Nuke `PublishNuget` / `PublishGitHubRelease` packs and publishes packages versioned exactly to the tag. There is no live Azure release pipeline.
5. NuGet packages are versioned correctly: the release tag version wins on tagged HEADs, GitVersion-derived versions apply otherwise.
6. Build failures produce clear, actionable error messages with the failing target and step identified.
7. On an Arch Linux `x86_64` or `aarch64` host, `./build.sh PackYay` or `./build.ps1 PackYay` produces the documented local artifacts for that architecture and version. It always packages an optimized Release self-contained Avalonia application, independently of the local default build configuration, and does not restore the whole solution.
8. The Arch package's metadata declares native runtime dependencies and its payload includes an executable launcher, application binaries, desktop entry, icon, and license notices. The generated recipe verifies the payload's SHA256 checksum, and packaging preserves executable permissions and managed/native application files.
9. `PackYay` leaves package installation and AUR publication to separate explicit actions. Building the package does not require root; the installed app runs without a system .NET SDK or runtime.
10. `./build.sh DeployUser --package-version-override 1.3.3` (or the PowerShell equivalent or `DeployYay` compatibility alias) installs only within the current user's home. It requires no elevation or package manager, retains previous releases and user data, forwards launcher arguments correctly, and leaves an existing system installation unchanged.
11. A failed update cannot activate an incomplete or non-executable payload. Previous entrypoints and the selected release remain usable after a pre-activation failure. Unsafe paths and unmanaged destinations fail with actionable errors instead of being overwritten.

### Verification Method

- Local build execution on developer machines.
- Operator-run Nuke logs reviewed for completeness and timing. Historical Azure pipeline logs are not a live gate.
- Version string inspection on built packages (PackNuget verifies package contents and nuspec dependencies before declaring success).
- Arch packaging: retain the `PackYay` command and output, then run `pwsh ./tests/packaging/Verify-ArchPackage.ps1 -PackagePath <path-to-vicesharp.pkg.tar.zst>` to verify the generated package and recipe. Run `git diff --check` for the implementation/documentation slice.
- User-local deployment: run `pwsh ./tests/packaging/Verify-DeployYay.ps1`. It tests synthetic payload installation, special-character paths, actual launcher and desktop execution, retained releases, invalid destinations and failed-update recovery, then checks the compiled Nuke graph for `DeployYay -> DeployUser`. It performs no real installation. For a real deployment, retain the Nuke log, verify the running executable is in the selected user release, and compare the existing system-package file hashes before and after.

### Related TRs

- TR-PLAT-001 (Platform support; operator-run Nuke on Windows)

### Design Decisions

- Nuke is preferred over MSBuild-only or FAKE because it provides C# build logic that is debuggable in the IDE.
- GitHub `origin` is the git source of truth. Azure DevOps is retired and is not used.
- Packages publish to nuget.org via Nuke `PublishNuget`. Historical Azure pipeline YAML may remain in the tree.
- GitHub `origin` remains the source of truth; no GitHub Actions CI exists (potential future work for community PR validation).
- The `_build` project uses the same .NET SDK version as the main solution (specified in `global.json`).
- `PackYay` publishes only the Avalonia project graph and bundles the .NET runtime so creating and running an Arch desktop package does not depend on unrelated platform workloads or the distribution's available .NET runtime version. A generated local `PKGBUILD` keeps the package rebuildable with `makepkg`/yay without publishing to AUR.
- The operator requires installation to touch only user space. `DeployUser` publishes directly to retained user releases; `DeployYay` is a compatibility alias so an older command cannot request sudo. `PackYay` stays separate for explicit package construction. Reading system runtimes and tools does not grant the installer permission to change system files.
- The 2026-10-15 packaging and user-local deployment extensions are recorded here locally because the workspace MCP marker/session route is unavailable. Registration readback succeeds, but server configuration projection was denied access to `/opt/mcpserver/app/appsettings.yaml`; reconciliation and generated requirement export remain pending. Generated requirement exports are not edited manually.
