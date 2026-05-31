# ViceSharp.Host.Xbox

PLATFORM-CROSS-001 Phase 1: UWP-style Xbox host shell for ViceSharp.

This project is a thin entry-point that boots the shared `ViceSharp.Avalonia.App`
plus the in-process gRPC host composition from `ViceSharp.Host`. It contains
no UI of its own; all view models, controls, and host services are inherited
from those two projects so the Xbox build stays bit-for-bit consistent with
the desktop build (per FR-Host-UI-Boundary).

## Target framework

The project currently targets plain `net10.0` as a workload-available fallback.
The intended long-term target is `net10.0-windows10.0.19041.0` (or a newer
UWP/windows-app revision), but the UWP / windows-app SDK workload is not
installed on this build host. Plain `net10.0` keeps the project buildable on
this machine while still running on Xbox developer mode via the standard
.NET 10 runtime plus `Avalonia.Desktop`.

To switch to the Windows-specific UWP target once the workload is available:

1. Install the workload, for example:
   ```pwsh
   dotnet workload install windows-app
   ```
2. Edit `ViceSharp.Host.Xbox.csproj` and replace
   `<TargetFramework>net10.0</TargetFramework>` with
   `<TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>`.
3. If the workload requires it, add `<UseUwp>true</UseUwp>` and a
   `Package.appxmanifest` (see "Package manifest" below).

## Build

From the repository root:

```pwsh
dotnet build ViceSharp.slnx --nologo
```

For a Release x64 build suitable for sideloading:

```pwsh
dotnet build src/ViceSharp.Host.Xbox/ViceSharp.Host.Xbox.csproj -c Release -p:Platform=x64
```

The publish output ends up under
`src/ViceSharp.Host.Xbox/bin/Release/net10.0/`.

## Sideload onto an Xbox in developer mode

1. Put your Xbox into developer mode (Microsoft Store -> Dev Mode Activation).
2. Launch the Xbox Device Portal on the console, note the IP address and PIN,
   then open the portal in a desktop browser at `https://<console-ip>:11443`.
3. Pair the desktop dev environment with the console using the PIN. Visual
   Studio also supports this via Debug -> Properties -> Remote Machine.
4. Publish the Xbox host shell. The recommended pipeline once the UWP
   workload is in place is:
   ```pwsh
   dotnet publish src/ViceSharp.Host.Xbox/ViceSharp.Host.Xbox.csproj `
       -c Release -p:Platform=x64 -r win10-x64 --self-contained true
   ```
   While the project is still on the `net10.0` fallback target, drop
   `-p:Platform=x64` if it conflicts with the SDK and publish for the
   `win-x64` runtime identifier instead.
5. Package the publish output into an `.appx` (or `.msixbundle` once the
   project carries a `Package.appxmanifest`) and upload through the Xbox
   Device Portal's "Add" workflow under the "My games & apps" page, or use
   `WinAppDeployCmd.exe install` from a desktop developer command prompt.
6. Launch ViceSharp from the Xbox dashboard under "Dev home" -> "My
   games & apps".

## Package manifest

A `Package.appxmanifest` is intentionally NOT included in this scaffold
because the UWP / windows-app workload is unavailable on the current build
host: shipping a manifest without the matching workload causes spurious
build errors. Once the workload is installed:

- Add a `Package.appxmanifest` next to this README modelled after the
  standard UWP template.
- Reference it from the csproj with `<AppxPackage>true</AppxPackage>` (or
  via the windows-app project SDK), and add the Xbox-specific
  `<TargetDeviceFamily Name="Windows.Xbox" MinVersion="10.0.19041.0"
  MaxVersionTested="10.0.22000.0" />` entry.

## Tests

The scaffold is covered by
`tests/ViceSharp.TestHarness/HostShells/XboxHostShellTests.cs`:

```pwsh
dotnet test ViceSharp.slnx --filter "FullyQualifiedName~XboxHostShell"
```

The test asserts the csproj exists, targets `net10.0`, and references both
`ViceSharp.Avalonia` and `ViceSharp.Host`.
