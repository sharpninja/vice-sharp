# ViceSharp.Host.iOS

PLATFORM-CROSS-001 iOS host shell for ViceSharp. This is a thin Avalonia
mobile head that reuses the shared `App` from `ViceSharp.Avalonia`; the
emulator core and UI live in the existing projects, not here.

## Build prerequisites

By default this project targets `net10.0-ios`. That requires:

1. .NET SDK 10.0.201 or newer (`global.json` pins the rollForward channel).
2. The `ios` workload:

   ```pwsh
   dotnet workload install ios
   ```

3. **Mac for actual device / simulator builds.** The msbuild step
   succeeds on Windows (the workload provides the targets), but
   producing a runnable `.app` / `.ipa` requires a paired Mac running
   matching Xcode. Pair via Visual Studio:
   `Tools > iOS > Pair to Mac`.
4. On the Mac side: Xcode 16 or newer with the iOS 18 SDK, Apple
   developer signing certificate, and a provisioning profile for the
   `org.vicesharp.host.ios` bundle id (or change the id under your own
   organisation).

If the workload is missing the project falls back to plain `net10.0` so the
solution stays buildable on any host:

```pwsh
dotnet build src/ViceSharp.Host.iOS/ViceSharp.Host.iOS.csproj `
  /p:ViceSharpHostIosFallback=true
```

In fallback mode the project compiles a desktop Avalonia exe instead of an
iOS bundle. The fallback is solely for solution buildability; it is not a
shipping target.

## Build

Library compile (works on Windows with the workload installed):

```pwsh
dotnet build src/ViceSharp.Host.iOS/ViceSharp.Host.iOS.csproj -c Release
```

Device / simulator app (requires paired Mac):

```pwsh
dotnet build src/ViceSharp.Host.iOS/ViceSharp.Host.iOS.csproj `
  -c Release `
  /p:RuntimeIdentifier=iossimulator-arm64
```

## Deploy

- Simulator: launch from Visual Studio with the host shell selected,
  or `xcrun simctl install booted bin/Release/.../ViceSharp.Host.iOS.app`
  from the paired Mac.
- Device: archive and ship through Xcode Organizer / TestFlight using
  the artifacts produced under the paired Mac's `obj/` and `bin/`
  directories.

## Known gaps

- The shared `App` from `ViceSharp.Avalonia` currently only wires the
  classic desktop lifetime. On iOS the lifetime is
  `ISingleViewApplicationLifetime`; the host shell launches an empty
  surface until the shared `App` learns the mobile lifetime branch.
  That work is out of scope for the Phase 1 closeout scaffold.
- The repo enables `IsAotCompatible` and `EnableTrimAnalyzer` globally.
  Avalonia.iOS is not AOT/trim clean today, so this project opts out
  of those analyzers. Re-enable once Avalonia ships a clean profile.
