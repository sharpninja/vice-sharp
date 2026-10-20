# Phase D precondition: latest desktop build installed via Nuke on PAYTON-LEGION2

Operator directive, relayed by OMAR-VICE# (10:53): "Before beginning phase D implementation, ensure both omarchy and legion2 have latest desktop build installed via nuke." Confirmed directly in this session by Payton at 12:08: "Yes: after the 1M run finishes, pull develop to 2c8d6028 and run Nuke PublishMsi then InstallMsi on LEGION2; record the commit, installed version and command output in a receipt." Class-2 operator action, not BDPv4 work.

## Commit

develop fast-forwarded from `ab52dfe0` to exactly `2c8d60284229b9fedc2d50f840153b9e8d8cbcbf` (7 incoming files, no overlap with the working tree; 2c8d6028 is an ancestor of origin/develop). This is the commit the LAB-OMARCHY install (release 1.3.111, `20261020T105400-phase-d-pre-desktop-install-linux`) was built from. Two tracked files were modified locally and neither is an input of the desktop build: `docs/receipts/handoff/README.md` (an Oct 16 edit) and `docs/receipts/plugin-params-file/Get-CommitCandidates.ps1` (receipt tooling).

## Command

`./build.ps1 InstallMsi --configuration Release`, run 12:10:08 to 12:12:56 local from a non-elevated console. InstallMsi depends on PublishMsi, so Nuke ran Restore, PublishMsi, InstallMsi in order; PublishMsi refuses a Debug configuration. InstallMsi elevated through `C:\Program Files\gsudo\Current\gsudo.exe` (one UAC prompt) and ran `msiexec /i ... /qn /norestart /log ...`. Full output: [nuke-installmsi.log](nuke-installmsi.log).

- Restore Succeeded 0:03; PublishMsi Succeeded 2:28; InstallMsi Succeeded 0:06; total 2:38; "Build succeeded"; build.ps1 exit 0.
- GitVersion: "MSI version from GitVersion: 1.3.111"; WiX build with `ProductVersion=1.3.111`.
- MSI: `artifacts/installer/ViceSharp.msi`, 40,697,856 bytes, SHA256 C5C901556475B7D0BB9102FFDA1B1BAF3F06C9559B442783EB28D86DC9A2401B.
- `12:12:56 [INF] InstallMsi complete (exit 0)`.
- Warnings: IL2104 trim notices from third-party assemblies (Avalonia.DesignerSupport, ASP.NET Core components, Grpc reflection, YamlDotNet, Avalonia.RemoteControl.Runtime), and one "Exception was suppressed" at Nuke startup: Nuke's own MSBuild project-model evaluation could not load `NuGet.Frameworks, Version=7.9.0.0` (Nuke caught it; every target succeeded).

## Installed version

- Windows Installer log (`artifacts/installer/install.log`): "Product: ViceSharp -- Installation completed successfully." and "Product Version: 1.3.111 ... Installation success or error status: 0." The client property `ProductVersion = 1.3.37` is the previously installed version, so this was an upgrade from 1.3.37 to 1.3.111.
- Apps list (Uninstall registry, HKLM): DisplayName ViceSharp, DisplayVersion 1.3.111, Publisher sharpninja, InstallDate 20261020, product code {41335A37-89DC-4FE3-929B-C2293E53B037}. `Get-Package -Name *ViceSharp*` returns nothing for this per-machine MSI in this console; the registry entry is the Apps-list source.
- `C:\Program Files\ViceSharp\ViceSharp.Avalonia.exe`: FileVersion 1.3.111.0, ProductVersion `1.3.111+2c8d60284229b9fedc2d50f840153b9e8d8cbcbf`, 122,156,736 bytes, SHA256 E940E53D37BDE69E41D143319B6084BF539CF1B9B4FF4669A0C730B1603123C3, identical to the published `src/ViceSharp.Avalonia/bin/Release/net10.0/win-x64/publish/ViceSharp.Avalonia.exe`.

The app was not launched. native/vice was not built or touched (the MSI path publishes ViceSharp.Avalonia and the WiX project only).
