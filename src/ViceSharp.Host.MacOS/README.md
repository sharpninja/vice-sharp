# ViceSharp.Host.MacOS

**PLATFORM-CROSS-001** macOS host shell for ViceSharp.

This is the macOS-native shell project. It is intentionally a thin wrapper:
the actual UI (views, view-models, gRPC host plumbing) is provided by
`ViceSharp.Avalonia`, and the in-process emulator host composition comes from
`ViceSharp.Host`. This project just supplies the macOS-flavoured entrypoint
(`Program.cs`) and bundle metadata (`Info.plist`, csproj `CFBundle*`
properties) so we can produce an `.app` bundle on a macOS build host.

## Build

```sh
# From the repo root.
dotnet build src/ViceSharp.Host.MacOS/ViceSharp.Host.MacOS.csproj -c Release
```

The project targets `net10.0` (cross-platform), so the build runs on Windows,
Linux, and macOS hosts equally. macOS-specific bundling kicks in when you
build with a macOS RuntimeIdentifier:

```sh
# Apple Silicon.
dotnet publish src/ViceSharp.Host.MacOS/ViceSharp.Host.MacOS.csproj \
    -c Release -r osx-arm64 --self-contained true

# Intel.
dotnet publish src/ViceSharp.Host.MacOS/ViceSharp.Host.MacOS.csproj \
    -c Release -r osx-x64 --self-contained true
```

## Run (macOS host)

```sh
dotnet run --project src/ViceSharp.Host.MacOS/ViceSharp.Host.MacOS.csproj -c Release
```

## Prerequisites

- .NET 10 SDK (`dotnet --version` >= 10.0.200).
- For producing an actual macOS `.app` bundle: a macOS build host. Cross-builds
  on Windows / Linux produce a portable `net10.0` binary but do not codesign
  or bundle (those steps are macOS-only).
- The macOS `.NET` workload is optional for the Phase 1 closeout scaffold: the
  TFM is the cross-platform `net10.0`, so no `macos` workload is required to
  compile this csproj. The workload becomes relevant when we promote to
  `net10.0-macos` later for codesigning and entitlements work.

## Bundle id

`com.sharpninja.vicesharp.macos`

Set in both the csproj (`<CFBundleIdentifier>`) and `Info.plist`. Keep them in
sync if you ever change it.

## What this project does NOT do

- It does not contain any UI XAML, view-models, or view code; that all lives
  in `ViceSharp.Avalonia` and must not be duplicated here.
- It does not contain any emulator runtime code; that all lives in
  `ViceSharp.Host` (composition) and the lower-tier runtime projects
  (`ViceSharp.Core`, `ViceSharp.Chips`, `ViceSharp.Architectures`,
  `ViceSharp.RomFetch`, etc.). This shell only references
  `ViceSharp.Avalonia` and `ViceSharp.Host`, preserving the host boundary
  enforced by `AvaloniaBoundaryTests`.

## See also

- `src/ViceSharp.Avalonia/` - shared Avalonia UI (App, MainWindow, views,
  view-models).
- `src/ViceSharp.Host/` - in-process emulator + gRPC composition.
- `tests/ViceSharp.TestHarness/HostShells/MacOSHostShellTests.cs` - scaffold
  smoke test that pins the public surface of this project.
