# ViceSharp Xbox head: dev-PC validation guide (S0 + S36-S41)

Audience: the operator validating the `ViceSharp.Xbox` UWP head on a **Windows 11 dev PC with an Xbox controller** (no console needed). This closes plan slices S0 (run half) and S36-S41. S42 (console/Store) is deferred and NOT covered here.

Everything below is the human/hardware validation that a headless build agent cannot perform. All the code is written, committed, and its off-console gates are green; the UWP head builds clean (verified). What remains is: run it, press the controller, confirm behavior, record PASS/FAIL.

Report back per slice with PASS/FAIL + any error text or screenshot. Anything that FAILs, I fix.

---

## A. One-time prerequisites

1. **Visual Studio 2026 (VS 18)** with the **UWP / Windows App development** workload. VS supplies the UWP XAML markup compiler; the `dotnet` CLI does not (see Known walls). VS 2026 will offer to install the missing UWP components on first open of the project.
2. **Windows Developer Mode** on: Settings > System > For developers > Developer Mode = On (required to sideload/run an unpackaged-signed UWP app).
3. **An Xbox controller** paired to the dev PC (USB cable or Bluetooth). Confirm Windows sees it: `joy.cpl` shows it responding.
4. **VICE C64 ROMs** available for first-run provisioning (S40): either allow the app to download them from the VICE GitHub mirror (needs internet; the manifest declares `internetClient`), or have `kernal`, `basic`, `chargen` on a USB path for import. ViceSharp ships NO ROMs.

## B. Build and launch (the simplest dev-PC path)

Easiest: **from Visual Studio.**
1. Open `ViceSharp.slnx` in VS 2026.
2. Set `ViceSharp.Xbox` as the startup project. Configuration = **Debug**, Platform = **x64**, Target = **Local Machine**.
3. In `ViceSharp.Xbox` project properties (or via the build), ensure `ViceSharpXboxUwp=true` is active for the UWP TFM. (The project defaults to the `net10.0` fallback; VS's UWP launch profile sets the UWP TFM. If VS builds the fallback, add `-p:ViceSharpXboxUwp=true` to the build or set it in a Directory.Build.rsp for the project.)
4. Press **F5**. VS builds, signs with the auto dev cert, deploys the MSIX to the local machine, launches it, and attaches the debugger.

Command-line alternative (build only; then deploy the MSIX):
```pwsh
$msb = (& "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -prerelease -find 'MSBuild\**\Bin\MSBuild.exe')
& $msb src\ViceSharp.Xbox\ViceSharp.Xbox.csproj /t:Restore,Build /p:Configuration=Debug /p:Platform=x64 /p:ViceSharpXboxUwp=true
# -> expect exit 0. To produce + register an MSIX locally:
& $msb src\ViceSharp.Xbox\ViceSharp.Xbox.csproj /t:Restore,Publish /p:Configuration=Debug /p:Platform=x64 /p:ViceSharpXboxUwp=true /p:UapAppxPackageBuildMode=SideloadOnly
# then: Add-AppxPackage the produced .msix (or its .appxbundle), or use the generated Install.ps1 in the AppPackages output.
```

## C. Per-slice validation checklist

### S0 - feasibility GO / NO-GO (the gate everything depends on)
Launch the app (section B). Confirm all three, then record the GO decision:
- [ ] App launches without an AppContainer / P-Invoke / marshalling fault.
- [ ] One **C64 frame renders** (the DX11 composition swap chain presents a visible, advancing picture).
- [ ] The **gamepad reads** at least once (any stick/button visibly affects the app).
- **GO** = all three PASS. Record it (this unblocks accepting S1+ as validated). If AOT (Release) is a concern, note it separately - dev validation runs Debug/JIT; AOT is a Store-submission gate (see Known walls).

### S36 - boot-to-menu + render
- [ ] App boots to the **10-foot main menu**.
- [ ] **XY focus navigation** works with the D-pad / left stick (focus visibly moves between tiles; a focus ring is shown).
- [ ] A **live C64 frame** renders (in the emulator view or a menu preview).

### S37 - live gamepad -> C64 joysticks
Boot a C64 program that reads a joystick (e.g. a game, or watch the input overlay if present). Mapping under test:
- [ ] **Left stick** AND **D-pad** drive **JOY2** - all 8 directions (up/down/left/right + diagonals).
- [ ] **Right stick** drives **JOY1** - all 8 directions.
- [ ] **A** = JOY2 fire. **B** = JOY1 fire.
- [ ] **Open the menu** (Menu button) -> the controller is **neutralized** for the C64 (A does NOT fire the joystick while a menu is open).
- [ ] **L3** (press the left stick) -> **swaps** the two ports (left stick now drives JOY1, etc.).
- [ ] **Guide/Nexus** button -> does NOT disturb the app (shell-reserved).
- [ ] **Disconnect the controller** mid-play -> both ports **center** (no stuck direction/fire).

### S38 - audio + video
- [ ] Boot the C64: the picture is **letterboxed, correct colors, no tearing**.
- [ ] **SID audio is audible.**
- [ ] **Mute** toggles silence; **volume** scales the level.
- [ ] **Suspend/Resume** (minimize / Alt+Tab away and back) -> no deadlock, no state corruption, audio+video resume cleanly.

### S39 - system buttons + input context (verified default bindings)
From `BindingProfile.cs` (the shipped defaults):
- [ ] **Menu** -> opens / closes the app main menu (toggle).
- [ ] **View** -> toggles the **virtual keyboard** (toggle).
- [ ] **X** -> **autostart drive 8** (press).
- [ ] **Y** -> **warm reset** (press; behind a yes/no confirm).
- [ ] **LB** (left shoulder) -> **quick-save state** (press).
- [ ] **RB** (right shoulder) -> **quick-load state** (press).
- [ ] **LT** (left trigger, **held**) -> **warp** on while held, off on release.
- [ ] **L3** (left stick press) -> **swap joystick ports** (toggle).
- [ ] **A** fires JOY2 **only in gameplay context**, never while a menu is open.
- [ ] **RESTORE** (via the virtual keyboard tile) -> fires the C64 **NMI**. NOTE: RESTORE->NMI-edge wiring is tracked as an open follow-up (FIX-XKBDNMI-001); if RESTORE does nothing, that is the known gap - flag it and I will wire it.

### S40 - first-run ROM provisioning + settings persistence + media attach
- [ ] **First run**: the wizard acquires the 3 core ROMs (download from the VICE mirror OR USB import) into the app's `LocalFolder/vice/C64`, and the C64 boots to **READY.**
- [ ] Change a **palette or scale** setting -> **relaunch** the app -> the change **persisted** (written to `vice.ini` `[ViceSharpXbox]`).
- [ ] **Attach a `.d64`** with a working **1541 / 1540 / 1541-II** drive-model selector -> it loads.
- [ ] All of the above is reachable **controller-only** and stays inside the TV-safe area.

### S41 - performance
- [ ] **Sustained ~50 fps (PAL)** over a fixed workload (a demo/game running a minute).
- [ ] Both **PAL and NTSC** machines run.
- [ ] No CPU starvation / stutter on the dev PC. (The true AppContainer resource budget is a console-only S42 check - not required here.)

## D. Known walls (read before you build)

1. **Build with VS MSBuild, not `dotnet`.** `dotnet build -p:ViceSharpXboxUwp=true` fails with `CS0103 InitializeComponent` / `CS5001 no Main` because the dotnet SDK's MSBuild does not import the UWP XAML markup compiler; VS MSBuild does. This is a host-selection matter, not a missing component (verified 2026-07-13, FIX-XWIN2D-001). The `net10.0` fallback (no `-p:ViceSharpXboxUwp=true`) still builds fine under `dotnet` - that is CI's path.
2. **AOT (Release) may not actually AOT-link yet.** In testing, VS-MSBuild `Publish` with `-p:PublishAot=true` exited 0 but produced a **managed apphost only** (ILC did not run). For dev validation use **Debug/JIT** (section B). Whether Native-AOT of the UWP MSIX is honored in your toolchain is the open S0 (a) question; the plan already allows JIT Debug + the net10.0 fallback, so this does not block dev validation - only Store-AOT.
3. **RESTORE key (S39)** may not fire the NMI yet - open follow-up FIX-XKBDNMI-001. Flag if so.
4. **ROM download (S40)** needs network + the `internetClient` capability (declared in the manifest). If offline, use USB import.
5. **Video is raw Direct3D 11** now (Win2D removed, FIX-XWIN2D-001). A machine with no usable GPU/DXGI degrades to a silent no-blit surface rather than crashing - if you see a black emulator view but audio runs, suspect the GPU/DXGI path and report it.

## E. What to send back
For each slice S0, S36-S41: **PASS** or **FAIL**, and for any FAIL the exact error text / a screenshot / what you saw vs expected. I turn those into fixes (tests-first) and re-verify. When all six are PASS, the plan is validated end to end on the dev PC; only S42 (console/Store) then remains, at submission time.
