# Phase D precondition: latest desktop build installed through Nuke on LAB-OMARCHY, 2026-10-20T10:53 to 10:56

Payton, 2026-10-20T10:53, verbatim: "Before beginning phase D implementation, ensure both omarchy and legion2 have latest desktop build installed via nuke." Class-2 operator action (not BDPv4 work); recorded as D-pre in `docs/plans/PLAN-NATIVERESTORE-001.md`. The Windows half (PublishMsi and InstallMsi on PAYTON-LEGION2) is LEGION2's, requested at 10:53.

## Before

- `develop` head `2c8d6028` (the Phase D plan draft). Installed user-local release: `~/.local/share/vicesharp/current` pointed at `releases/1.3.37-0f748d243a014d25b6752b48d9819e59` (installed 2026-10-16T13:30); launcher `~/.local/bin/vicesharp`, desktop entry `~/.local/share/applications/vicesharp.desktop`; five retained releases (0.1.751 twice, 0.1.758, 1.3.34, 1.3.37).

## Run 1 (10:54:05 to 10:54:16): `./build.sh DeployUser`, exit 0, succeeded in 11 s (incremental publish)

Observed: `dotnet publish src/ViceSharp.Avalonia/ViceSharp.Avalonia.csproj --configuration Release --runtime linux-x64 --self-contained` ran and installed `releases/0.1.835-5ed43086...`; `current` was switched to it. Two findings: (1) `[WRN] DeployUser: GitVersion unavailable (Could not find 'dotnet-gitversion' via '/usr/bin/which'.); using git commit-count fallback`, so the release label was 0.1.835 although the tool is installed as a global dotnet tool (`gitversion.tool 6.8.2`, `~/.dotnet/tools/dotnet-gitversion`), because `~/.dotnet/tools` is not on the PATH of the shell that launched Nuke; (2) Nuke logged `[WRN] Exception was suppressed` with `InvalidProjectFileException: The expression "[MSBuild]::GetTargetFrameworkIdentifier(net10.0)" cannot be evaluated. Could not load file or assembly 'NuGet.Frameworks, Version=7.9.0.0'` from its own project-model evaluation (`Microsoft.NET.TargetFrameworkInference.targets` under SDK 10.0.402); the publish itself was unaffected. Both are build-tooling observations for the Nuke owner, not product defects; neither is fixed here.

## Run 2 (10:55:35 to 10:55:49): `PATH="$HOME/.dotnet/tools:$PATH" ./build.sh DeployUser`, exit 0, succeeded in 14 s

`MSI version from GitVersion: 1.3.111` (Major.Minor.CommitsSinceVersionSource; GitVersion's own FullSemVer for the head is 1.3.3-alpha.111). Installed `releases/1.3.111-1beaac12c5d74c038596917d33d04454`; `current` switched to it.

## After (verified by `Get-InstalledViceSharpVersion.ps1`, reading the installed assembly)

- Release: `~/.local/share/vicesharp/releases/1.3.111-1beaac12c5d74c038596917d33d04454` (393 files).
- `ViceSharp.Avalonia.dll`: AssemblyVersion 1.3.111.0, FileVersion 1.3.111.0, ProductVersion `1.3.111+2c8d60284229b9fedc2d50f840153b9e8d8cbcbf` (the head commit), SHA256 `DCDC77B6A8CCE26D720C708B3DB997F667E2CA029232466DDB1644E8C714ED2B`.
- Apphost `ViceSharp.Avalonia` SHA256 `FD8F29E3A0A3D0CB52EB0D7C848342ADC4335266E893E0C23BC6A00E41A174FB`.
- The 0.1.835 release from run 1 and the five earlier releases remain under `releases/` (DeployUser keeps older releases; nothing deleted).

Observation versus inference: the installed binary's ProductVersion carrying `2c8d6028` is the observation that the installed build is the current head; that it launches is not claimed (no GUI was started).

Signed: claude-fable-5-1-high@ClaudeCode | Confidence: correctness 97%, accuracy 98%, completeness 96%
