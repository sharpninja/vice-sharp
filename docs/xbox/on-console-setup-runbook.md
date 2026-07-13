# ViceSharp Xbox head: on-console setup and S0 execution runbook

Audience: the operator preparing to SUBMIT the `ViceSharp.Xbox` UWP head to the Microsoft Store on a physical Xbox (One / Series S|X).

IMPORTANT (updated 2026-07-13): the physical console is NOT needed for development or functional validation. UWP apps, `Windows.Gaming.Input` controller reading, Win2D, and XAudio2 all run on a Windows 11 dev PC, so the whole app (including live Xbox-controller input) is validated on the dev machine (plan Tier D). This console runbook applies only to the DEFERRED Tier C work: actual console deploy, the AppContainer resource budget and on-console PAL-50 perf, real-TV device-family behaviors, and Store certification. For day-to-day dev, skip to "Dev-PC path" below; use sections 1 and 4-8 only when preparing a Store submission.

Dev-PC path (Tier D, primary for all development):
1. Install the windows-app / UWP workload on your dev PC (section 2).
2. Build and run the head on Windows: `dotnet build src/ViceSharp.Xbox/ViceSharp.Xbox.csproj -c Debug -p:ViceSharpXboxUwp=true`, then launch it on the desktop.
3. Plug in an Xbox controller and validate the full app there (mapping, 10-foot UI focus, virtual keyboard, audio/video). No console required.
4. The AOT publish check (`dotnet publish ... -p:PublishAot=true`) also runs on the dev PC (section 5).

Honesty note: modern-.NET Native-AOT UWP running on the physical Xbox console (as opposed to the Windows dev PC) is the one thing this runbook cannot prove until Store-submission time. Sections 5-7 run on the dev PC (Tier D); the console-specific portions (section 6 deploy to console, section 7 on-console checks) are Tier C, done only at submission. Items marked (VERIFY) are ones to confirm on first run. Observation (Microsoft docs, 2024-2026) vs inference is called out where it matters.

## 0. What you need
- An Xbox One or Series S|X console you can dedicate to Developer Mode.
- A Microsoft Partner Center individual developer account (about 19 USD one-time in the US). Observation: Dev Mode activation requires this account.
- A Windows 11 build PC with Visual Studio 2026 (or the .NET 10 SDK 10.0.201+ already used by this repo) plus the Windows App / UWP workload (section 2).
- Both devices on the same local network.

## 1. Activate Xbox Developer Mode
1. On the console, install "Dev Mode Activation" from the Microsoft Store and follow it to register the console under your Partner Center account.
2. Switch the console to Developer Mode. You can toggle between Retail and Dev modes later; ViceSharp is sideloaded in Dev Mode.
3. Note the console's Device Portal address (shown in Dev Home on the console, typically `https://<console-ip>:11443`). Enable Device Portal and set the pairing credentials.

Resource budget to keep in mind (observation, Microsoft "System resources for UWP apps/games on Xbox One"): a Dev-Mode app gets roughly 1 GB RAM, 2-4 shared CPU cores, up to 45 percent GPU, DirectX 11 only, x64 only. A C64 emulator fits comfortably; slice S41 measures it (target PAL 50 fps).

## 2. Install the build workload (gates S34 XAML build and all on-console slices)
On the build PC:
1. Install Visual Studio 2026 with the UWP / Windows App workload, OR from a developer shell run the workload install for the modern-.NET UWP head. (VERIFY the exact workload id on your SDK; the repo's `src/ViceSharp.Host.Xbox/README.md` records that this workload is not present on the current build host, which is why S1-S33 are authored to build without it.)
   - Try: `dotnet workload install windows-app` (VERIFY: on some SDK builds the id differs; `dotnet workload search` lists available ids).
2. Confirm the UWP head restores under the UWP TFM:
   - `dotnet build src/ViceSharp.Xbox/ViceSharp.Xbox.csproj -c Debug -p:ViceSharpXboxUwp=true`
   - Without `-p:ViceSharpXboxUwp=true` the project builds as plain `net10.0` (the workload-free fallback used by CI); with it, it targets `net10.0-windows10.0.26100.0` + `UseUwp`.

## 3. Create a self-signed signing certificate (sideload signing)
MSIX packages sideloaded in Dev Mode are signed with a self-signed developer certificate (default choice; Partner Center signing is the alternative if you later pursue Store distribution).
1. In a PowerShell (pwsh) admin session on the build PC:
   - `New-SelfSignedCertificate -Type Custom -Subject "CN=ViceSharpDev" -KeyUsage DigitalSignature -FriendlyName "ViceSharp Dev" -CertStoreLocation "Cert:\CurrentUser\My" -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")`
   - Export the public cert (`.cer`) for the Publisher and keep the private key in the store for signing. The `Package.appxmanifest` `Identity Publisher` MUST match the certificate subject `CN=ViceSharpDev`.
2. The MSIX build signs with this cert (VS handles it when the cert is selected in the project's packaging settings; command-line packaging uses `SignTool` or the MSIX `/p:PackageCertificateThumbprint`).

## 4. Pair the console to the build PC
1. On the console (Dev Home), open the Visual Studio pairing screen ("Show Visual Studio pin"). Observation: the classic UWP-on-Xbox deploy flow uses x64 platform, Target device = Remote Machine, and Dev Home PIN pairing.
2. In Visual Studio, set the `ViceSharp.Xbox` startup project, Platform = x64, Target = Remote Machine, enter the console IP, and complete PIN pairing. (Command-line alternative: `WinAppDeployCmd` against the console IP, section 6.)

## 5. Build the S0 feasibility probe
S0 proves three things jointly before any production slice is accepted (plan section 2): (a) a Native-AOT UWP MSIX links clean and runs on the console, (b) the trimmed managed core links clean, (c) Win2D + Native-AOT packaging is clean.
1. Publish the probe (Release = AOT; the Store publish path):
   - `dotnet publish src/ViceSharp.Xbox/ViceSharp.Xbox.csproj -c Release -r win-x64 -p:ViceSharpXboxUwp=true -p:PublishAot=true`
   - GREEN requires exit 0 with zero IL2xxx / IL3xxx trim/AOT warnings (they are errors under `TreatWarningsAsErrors`).
2. If AOT fails but you need to unblock on-device testing, publish the Debug JIT MSIX instead (dev deploy path):
   - `dotnet publish src/ViceSharp.Xbox/ViceSharp.Xbox.csproj -c Debug -r win-x64 -p:ViceSharpXboxUwp=true`
   - This is the documented S0 fallback (a): treat Store/AOT as blocked pending investigation, keep dev-deploy alive.

## 6. Deploy and launch on the console
1. From Visual Studio: F5 / Deploy to the paired Remote Machine.
2. Command-line alternative:
   - `WinAppDeployCmd install -file ViceSharp.Xbox_<ver>_x64.msix -ip <console-ip> -pin <pairing-pin>`
3. Launch the app from the console Dev Home apps list.

## 7. S0 GO / NO-GO checklist (record the result before accepting S1+)
Mark each PASS/FAIL on the console:
- [ ] AOT publish exit 0 with 0 IL2xxx/IL3xxx warnings (or the documented JIT fallback is in use, with AOT flagged blocked).
- [ ] Probe MSIX installs via Dev Mode and launches under Dev Home.
- [ ] The managed C64 core advances at least one frame on-device (visible frame plus an advancing counter).
- [ ] The gamepad reads once on-device with no AppContainer / P-Invoke / marshalling fault.
- [ ] Win2D swap chain composes and presents on-device under AOT (or the documented raw-DX11 fallback is authorized).

GO = all pass (AOT variant) OR the JIT/raw-DX11 fallbacks are explicitly accepted with their limitation recorded. Record the GO decision (and any fallback taken) before any S1+ production slice is accepted as merged.

## 8. Rollback / uninstall (bad sideload recovery)
- Uninstall via Device Portal (Apps list -> Remove) or:
  - `WinAppDeployCmd uninstall -package <PackageFullName> -ip <console-ip> -pin <pin>`
- Redeploy the previous known-good MSIX with `WinAppDeployCmd update`/`install`.
- The desktop build and the `ViceSharp.Host.Xbox` Avalonia scaffold are unaffected by any Xbox-head sideload (the head is additive), so a bad MSIX never touches your desktop workflow.

## 9. On-console validation slices this runbook unblocks
- S0: feasibility go/no-go (this runbook, sections 5-7).
- S36: deploy + boot-to-10-foot-menu + C64 render smoke.
- S37: live gamepad smoke (left stick/D-pad -> JOY2, right stick -> JOY1, A/B fire, menu neutralization, port swap).
- S38: audio + video smoke (Win2D video, XAudio2 audio, mute, suspend/resume).
- S39: system-button + input-context smoke (incl. RESTORE via the FR-XKBD seam).
- S40: first-run ROM provisioning (download from the VICE GitHub mirror) + settings persistence + media attach end to end.
- S41: performance within the resource budget (PAL 50 fps target).

## Sources (observation, dated)
- Modernize your UWP app with .NET and Native AOT (Microsoft Learn, ms.date 2026-01-26): net10.0-windows UWP + PublishAot is GA; AOT is the Store publish path.
- Gamepad and vibration; Windows.Gaming.Input.Gamepad (winrt-26100): dual sticks -1..1, triggers 0..1, GamepadButtons; supported in UWP on Xbox One.
- System resources for UWP apps/games on Xbox One: app-partition ~1 GB RAM, 2-4 shared cores, 45 percent GPU, DX11, x64.
- Designing for Xbox and TV; Gamepad and remote interactions: 10-foot UI, XY focus, RequiresPointer, TV-safe area.
- Xbox Dev Mode activation (Partner Center, about 19 USD one-time); classic UWP-on-Xbox deploy (x64 Remote Machine, Dev Home PIN pairing) is documented but archived (VS 2015-2019 era), which is why the modern-.NET-AOT-on-console path is the S0 unknown.
