# TR-Build-System: Build System Technical Requirement

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Quality Area   | CI/CD / Build Automation       |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-04-13                     |

---

## TR-BUILD-001: Nuke Build with Dual CI/CD (Azure DevOps + GitHub Actions)

**ID:** TR-BUILD-001
**Title:** Nuke Build System with Dual CI/CD Pipelines
**Priority:** P0 -- Critical
**Category:** Build / CI/CD

### Description

ViceSharp shall use the Nuke build system for all build, test, package, and publish operations. The build definition is a C# project (`_build`) that defines targets as code. Two CI/CD pipelines are maintained: Azure DevOps (primary, source of truth) and GitHub Actions (mirror, community-facing).

### Rationale

Nuke provides a strongly-typed, IDE-debuggable build system written in C#, matching the project language. Dual CI/CD ensures the primary Azure DevOps pipeline is the deployment authority while the GitHub mirror provides community visibility and PR validation.

### Technical Specification

1. **Nuke Build Project:**
   - The `_build` project is a .NET console application using the Nuke framework.
   - Build targets include: `Clean`, `Restore`, `Compile`, `Test`, `Pack`, `Publish`, `PublishAot`, `IntegrationTest`, `BenchmarkRun`.
   - Target dependencies form a DAG (directed acyclic graph) with correct ordering.
   - The build can be executed locally via `nuke` CLI or `dotnet run --project _build`.

2. **Azure DevOps Pipeline (Primary):**
   - YAML pipeline (`azure-pipelines.yml`) triggers on `main` and `release/*` branches.
   - PR validation runs `Compile` + `Test` targets.
   - CI builds run `Compile` + `Test` + `Pack` + `PublishAot` targets.
   - Release builds additionally publish NuGet packages to the Azure DevOps Artifacts feed.
   - Multi-platform matrix: Windows x64, Ubuntu x64, macOS ARM64.

3. **GitHub Actions Pipeline (Mirror):**
   - Workflow file (`.github/workflows/ci.yml`) triggers on push/PR to `main`.
   - Runs the same `Compile` + `Test` targets as Azure DevOps for validation.
   - Does not publish packages (Azure DevOps is the single source for artifact publication).
   - Multi-platform matrix matching Azure DevOps targets.

4. **Build Targets:**
   - `Clean`: Removes all `bin`/`obj`/`artifacts` directories.
   - `Restore`: Runs `dotnet restore` with the global packages folder.
   - `Compile`: Builds the solution in the specified configuration (Debug/Release).
   - `Test`: Runs all unit tests with coverage collection (minimum 80% line coverage).
   - `IntegrationTest`: Runs integration tests (Lorenz suite, timing tests) with a longer timeout.
   - `BenchmarkRun`: Executes BenchmarkDotNet benchmarks and archives results.
   - `Pack`: Creates NuGet packages for `ViceSharp.Abstractions` and `ViceSharp.Core`.
   - `Publish`: Publishes framework-dependent binaries.
   - `PublishAot`: Publishes NativeAOT binaries for all target RIDs (per TR-AOT-001).

5. **Versioning:**
   - Version is derived from a `.version` file and Git tags (GitVersion or Nerdbank.GitVersioning).
   - Pre-release versions use the branch name and build counter as suffix.
   - Release versions are tagged on `main` (e.g., `v1.0.0`).

### Acceptance Criteria

1. `nuke Compile` succeeds locally on Windows, Linux, and macOS.
2. `nuke Test` runs all unit tests and reports results in a structured format (TRX or JUnit XML).
3. `nuke PublishAot` produces native binaries for at least `win-x64`, `linux-x64`, and `osx-arm64`.
4. The Azure DevOps pipeline completes a full CI build (Compile + Test + Pack + PublishAot) in under 15 minutes.
5. The GitHub Actions pipeline completes PR validation (Compile + Test) in under 10 minutes.
6. NuGet packages are versioned correctly with pre-release suffixes for non-release branches.
7. Test coverage reports are generated and uploaded as pipeline artifacts.
8. Build failures produce clear, actionable error messages with the failing target and step identified.

### Verification Method

- Local build execution on developer machines (all three OS).
- Pipeline execution logs reviewed for completeness and timing.
- Version string inspection on built packages.
- Coverage report review for minimum threshold compliance.

### Related TRs

- TR-AOT-001 (PublishAot target validates AoT compatibility)
- TR-PLAT-001 (Multi-platform build matrix)

### Design Decisions

- Nuke is preferred over MSBuild-only or FAKE because it provides C# build logic that is debuggable in the IDE.
- Azure DevOps is the primary CI/CD because it hosts the primary repository and artifact feed.
- GitHub Actions mirrors the build for community contributors who submit PRs to the GitHub mirror.
- The `_build` project uses the same .NET SDK version as the main solution (specified in `global.json`).
