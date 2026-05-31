# ViceSharp.Host.Android

PLATFORM-CROSS-001 Android host shell for ViceSharp. This is a thin Avalonia
mobile head that reuses the shared `App` from `ViceSharp.Avalonia`; the
emulator core and UI live in the existing projects, not here.

## Build prerequisites

By default this project targets `net10.0-android`. That requires:

1. .NET SDK 10.0.201 or newer (`global.json` pins the rollForward channel).
2. The `android` workload:

   ```pwsh
   dotnet workload install android
   ```

   On Windows the workload pulls in the Microsoft.Android.Sdk pack
   (currently 36.x), which bundles the Android SDK, NDK, and an OpenJDK
   build under `C:\Program Files\Android` /
   `C:\Program Files (x86)\Android`. The SDK target is API 36; the
   minimum `SupportedOSPlatformVersion` is 23 because the transitive
   AndroidX Lifecycle library used by Avalonia.Android requires it.
3. (Device deploy only) An attached or emulated Android device and
   `adb` on `PATH`. Workload installs `adb` under the SDK platform-tools
   directory.

If the workload is missing the project falls back to plain `net10.0` so the
solution stays buildable on any host:

```pwsh
dotnet build src/ViceSharp.Host.Android/ViceSharp.Host.Android.csproj `
  /p:ViceSharpHostAndroidFallback=true
```

In fallback mode the project compiles a desktop Avalonia exe instead of an
APK. The fallback is solely for solution buildability; it is not a shipping
target.

## Build the APK

```pwsh
dotnet build src/ViceSharp.Host.Android/ViceSharp.Host.Android.csproj `
  -c Release
```

Output lands under
`src/ViceSharp.Host.Android/bin/Release/net10.0-android/` as
`org.vicesharp.host.android-Signed.apk` (debug-signed by the build).

## Deploy to a device

1. Enable USB debugging on the device.
2. Plug it in (or start an emulator with `emulator -avd <name>`).
3. From the repo root:

   ```pwsh
   dotnet build src/ViceSharp.Host.Android/ViceSharp.Host.Android.csproj `
     -t:Install -c Release
   ```

   Alternative: `adb install bin/Release/.../*-Signed.apk` after a normal
   build.

## Known gaps

- The shared `App` from `ViceSharp.Avalonia` currently only wires the
  classic desktop lifetime (`IClassicDesktopStyleApplicationLifetime`). On
  Android the lifetime is `ISingleViewApplicationLifetime`; the host
  shell launches an empty surface until the shared `App` learns the
  mobile lifetime branch. That work is out of scope for the Phase 1
  closeout scaffold.
- The repo enables `IsAotCompatible` and `EnableTrimAnalyzer` globally
  via `Directory.Build.props`. Avalonia.Android is not AOT/trim clean
  today, so this project opts out of those analyzers. Re-enable once
  Avalonia ships a clean profile.
