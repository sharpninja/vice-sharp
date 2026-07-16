# ViceSharp for Xbox: Microsoft Store publishing checklist

Actionable, repo-specific checklist to take the `ViceSharp.Xbox` UWP-on-console head
from the current sideload/dev-cert state to a live Microsoft Store listing that installs
on retail Xbox consoles (and Windows desktop). Ordered roughly in the sequence you would
work them. Items marked **(blocker)** must be done before submission; the rest can be
finished during the Partner Center draft.

Current state (from the repo):
- Head project: `src/ViceSharp.Xbox/ViceSharp.Xbox.csproj` (UWP TFM when `ViceSharpXboxUwp=true`).
- Manifest: `src/ViceSharp.Xbox/Package.appxmanifest` - identity `sharpninja.ViceSharp.Xbox`, `Publisher="CN=ViceSharpDev"` (a **dev cert**, not a Store identity).
- Packaging: Nuke `PublishXbox` -> Release Native-AOT MSIX (win-x64); `ValidateStorePackage` -> WACK/appcert gate; `DeployXbox` -> sideload.
- License: GPL-2.0-or-later (VICE derivative). No Commodore ROMs are shipped.

---

## 1. Accounts and one-time setup

- [ ] **(blocker)** Enrol/confirm a **Microsoft Partner Center** developer account (company or individual). Note the seller/publisher ID.
- [ ] **(blocker)** Confirm the account is eligible to publish to **Xbox** (UWP apps publish to Xbox retail through the standard app path; no separate ID@Xbox/GDK registration is required for a UWP app that does not use Xbox Live services).
- [ ] Decide the publishing entity and public **publisher display name** (currently `ViceSharpDev`); it appears on the listing.

## 2. Reserve the app and bind its Store identity

- [ ] **(blocker)** In Partner Center, **create the app** and **reserve the name** (e.g. "ViceSharp"). Reserve alternates if taken.
- [ ] **(blocker)** From Partner Center > Product management > **Product identity**, copy the Store-assigned:
  - `Package/Identity/Name` (e.g. `1234SharpNinja.ViceSharp`)
  - `Package/Identity/Publisher` (e.g. `CN=XXXXXXXX-....`)
  - `Package/Properties/PublisherDisplayName`
- [ ] **(blocker)** Replace the dev values in `Package.appxmanifest`:
  - `Identity/@Name` `sharpninja.ViceSharp.Xbox` -> Store `Name`
  - `Identity/@Publisher` `CN=ViceSharpDev` -> Store `Publisher`
  - `Properties/PublisherDisplayName` `ViceSharpDev` -> Store `PublisherDisplayName`
  - Keep `Version` as a placeholder; `PublishXbox`/GitVersion stamps the real one at pack time. The Store requires each submission's `Version` to be **higher than the last accepted** and the **revision (4th) field to be 0**.
- [ ] Do **not** commit the Store `Publisher` GUID to a public dev-cert flow that expects `CN=ViceSharpDev`; keep the dev-cert identity for local sideload on a separate, non-Store manifest/config if you still sideload.

## 3. Licensing and Store-policy compliance (critical for a VICE derivative)

- [ ] **(blocker)** **GPL-2.0-or-later source offer**: the Store distributes a binary of GPL software, so the listing (or the in-app About page) must carry a written offer of source and a link to the corresponding source. Confirm the About page GPL-2.0 disclosure + VICE attribution + source offer is present and reachable in the shipping build.
- [ ] **(blocker)** Include full **third-party/OSS notices** (VICE, reSID, any bundled fonts/assets) in-app and/or in the listing.
- [ ] **(blocker)** **No Commodore ROMs** are shipped (correct today). Do not add any ROM to the package; the app acquires ROMs at runtime per `docs/ROMs.md`. Shipping copyrighted Commodore KERNAL/BASIC ROMs would fail both IP policy and legality.
- [ ] **Emulator policy**: the current Microsoft Store policy permits emulators of non-Microsoft platforms. State clearly in the description that ViceSharp emulates the Commodore 64 (a non-Microsoft system) and that the user supplies their own software/ROMs. Avoid any language or bundled content implying piracy.
- [ ] Confirm the app name/icon/screenshots do not use Commodore trademarks in a way that implies endorsement; use "Commodore 64" descriptively only.

## 4. Manifest and capability review

- [ ] **(blocker)** **Minimise capabilities.** Current declared caps:
  - `internetClient` - for the RomM/ROM HTTPS downloads. Keep.
  - `privateNetworkClientServer` - **added for PLAN-ROMM-001 LAN discovery** (the subnet scan + connecting to a RomM server on the local network). Without it the UWP sandbox blocks local-address connections. Keep **only if** you ship LAN discovery; each capability triggers reviewer scrutiny and a user-facing consent.
  - [ ] **Xbox caveat:** validate on a **retail-profile** Xbox that `privateNetworkClientServer` actually permits the local-subnet scan on console (Xbox network sandboxing is stricter than desktop). If console blocks it, gate the "Scan LAN" button off on Xbox and fall back to manual URL / pairing-code entry, or document that discovery is desktop-only.
- [ ] Do **not** declare restricted capabilities (`broadFileSystemAccess`, etc.) unless truly needed; they require extra Store justification.
- [ ] Confirm `TargetDeviceFamily` set matches the intended reach: `Windows.Xbox` (ship target) + `Windows.Desktop` (optional). Set `MaxVersionTested` to the OS you validated on.
- [ ] Verify all tile/logo assets referenced in the manifest exist at the right scales (`Assets\Square150x150Logo.png`, `Square44x44Logo.png`, `Wide310x150Logo.png`, `StoreLogo.png`, `SplashScreen.png`) and meet Store asset requirements.

## 5. Build the Store package

- [ ] **(blocker)** Produce the packaged upload from the UWP head (needs the windows-app/UWP workload on the build box):
  ```pwsh
  ./build.ps1 PublishXbox
  ```
  This runs `dotnet publish -c Release -r win-x64 -p:ViceSharpXboxUwp=true -p:PublishAot=true` and produces the MSIX under `src/ViceSharp.Xbox/**/AppPackages`.
- [ ] For a Store submission, generate a **`.msixupload`** bundle (App bundle = x64 for Xbox; add arm64/x64/x86 only for the desktop family you target). In Visual Studio: Project > Publish > **Create App Packages** > "Microsoft Store using ..." with the reserved identity. Ensure the bundle is **not** signed with the dev cert (the Store re-signs).
- [ ] Confirm the package `Version` is monotonic vs. the last submission and its revision field is `0`.

## 6. Certification pre-flight (WACK) and Xbox checks

- [ ] **(blocker)** Run the **Windows App Certification Kit** locally before uploading:
  ```pwsh
  ./build.ps1 ValidateStorePackage --store-package-path <path-to-.msixupload-or-.msix>
  ```
  Requires `appcert.exe` (Windows SDK), an interactive admin session (never Session 0). The target fails unless `OVERALL_RESULT=PASS`; the report lands in `artifacts/wack/`.
- [ ] **Xbox certification specifics** (validate on a real console via `DeployXbox` first):
  - [ ] Full **controller navigation** - every screen reachable and operable with a gamepad only (no mandatory pointer/keyboard). The RomM Library/Details/Lists/CSDb pages and the on-screen keyboard must be gamepad-complete.
  - [ ] **TV-safe area** respected (the pages use `TvSafeAreaRootStyle`); nothing critical in the overscan margin.
  - [ ] App **launches, suspends, resumes, and terminates** cleanly; handles constrained Xbox **memory** budget; no crash/hang on cold boot.
  - [ ] No `RPC_E_WRONG_THREAD` on async cover/tile loads (LibraryObservableObject dispatch covers this; confirm on device).
  - [ ] Reasonable performance and no TDR (device-removed) under sustained emulation.

## 7. Age rating, privacy, and listing metadata

- [ ] **(blocker)** Complete the **IARC age-rating** questionnaire in Partner Center.
- [ ] **(blocker)** Provide a **Privacy policy URL** (the app has network capabilities and downloads content; a privacy policy is required even if data collection is minimal). State what the app connects to (user-configured RomM server, ROM mirror, CSDb) and that it stores connection details locally.
- [ ] Fill the **Store listing**:
  - [ ] Description, feature list, "what's new".
  - [ ] **Screenshots**: Xbox 1920x1080 captures of the emulator + RomM Library/Details/Lists/CSDb; desktop screenshots if the Desktop family is included.
  - [ ] Store logos/icons per required sizes.
  - [ ] Category (e.g. Utilities & tools, or the platform's emulator-appropriate category).
  - [ ] Support contact + website.
- [ ] Set **markets**, **pricing** (free), and the **device families** the submission targets (Xbox, and Desktop if included).

## 8. Submit, certify, roll out

- [ ] Upload the `.msixupload` to the Partner Center submission; resolve any package-validation warnings.
- [ ] Choose **rollout** (immediate on-pass, manual, or phased) and any **Sandbox/flight** groups you want to test with first.
- [ ] Submit for **certification**; watch for a certification report. Emulators and network apps sometimes draw manual review - be ready to answer on ROM handling and the GPL source offer.
- [ ] On pass, publish (or let the chosen rollout run). Verify the live listing installs on a **retail** Xbox and desktop.

## 9. Post-publish hygiene

- [ ] Tag the released commit and record the exact `Version` shipped.
- [ ] Keep the WACK report and the submitted `.msixupload` as release artifacts.
- [ ] For each update: bump `Version` (revision `0`), re-run `PublishXbox` + `ValidateStorePackage`, resubmit.

---

### Known repo follow-ups feeding this checklist

- The Store identity swap in `Package.appxmanifest` (section 2) is a **manual, one-time** edit gated on the Partner Center reservation; it is intentionally NOT automated (the dev-cert identity is what the local sideload/runbook uses).
- The `privateNetworkClientServer` **Xbox behaviour** (section 4) needs an on-console check; if the console sandbox blocks the LAN scan, gate the "Scan LAN" button to desktop and rely on manual URL / device pairing on Xbox.
- The device-pairing UI (Settings pairing page, `RomMPairingCoordinator`) and the keystore-backed connection store are the Xbox-friendly alternative to typing a token on a 10-foot UI; finishing them improves the certification/UX story (see PLAN-ROMM-001 Phase E).
