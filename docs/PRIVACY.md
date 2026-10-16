# Privacy Policy (ViceSharp)

**Effective date:** 2026-10-15  
**Product:** ViceSharp (desktop Avalonia host, Xbox UWP host, libraries, and related tools)  
**Publisher:** sharpninja / Payton Byrd  
**Contact:** via https://github.com/sharpninja/vice-sharp/issues  

This policy describes how ViceSharp handles information when you use the application. It is written for Microsoft Store listing requirements and for general users of the open-source project.

## Summary

- ViceSharp does **not** create a ViceSharp account.
- ViceSharp does **not** sell personal data.
- Most data stays **on your device**.
- Local desktop startup downloads missing default C64 firmware from the project's hash-pinned VICE GitHub mirror. Other network features use the endpoints you configure or select, such as a RomM library server.
- Commodore system ROMs and game media are **your responsibility**; the app does not ship copyrighted Commodore ROM images.

## Information stored on your device

Depending on platform and features you use, ViceSharp may store locally:

- Application settings (machine model, audio/video preferences, input bindings, UI preferences).
- Paths or handles to media you attach (disks, tapes, cartridges) when you choose to open them.
- Optional **RomM connection** details you provide. Linux stores the server URL and a keyring identifier in per-user metadata, while the token is held by Secret Service. Windows protects saved tokens with current-user DPAPI. If secure storage is unavailable, the connection can remain usable for the current session, but its token is not saved in plaintext.
- Optional **recent items** or library cache files (downloaded game images and cover art cached for offline launch).
- Diagnostic or log data you enable for troubleshooting (if present in a given build).

On Xbox / UWP, app data typically lives under the app's sandboxed local storage. On desktop, settings and caches use the host's configured local data folders.

On Linux, settings, firmware and the RomM download cache use `$XDG_DATA_HOME/ViceSharp` (normally `~/.local/share/ViceSharp`). User-local application releases are stored separately under `$XDG_DATA_HOME/vicesharp`; redeployment preserves the data directory and earlier releases.

## Network activity

ViceSharp may open network connections when you use features that require them:

| Activity | When | Destination |
| --- | --- | --- |
| ROM acquisition | Local desktop startup needs missing default C64 firmware, or you start a provision flow | The hash-pinned VICE GitHub mirror for default C64 firmware; other configured sources for explicit provisioning. Existing verified local firmware can be reused offline. See [ROMs.md](ROMs.md). |
| RomM library browse / download | You connect a RomM server | The **base URL you configure** (often a server on your LAN or self-hosted instance). |
| RomM LAN discovery | You use "Scan LAN" | Local network probes to find a RomM service; requires local network permission on UWP. |
| CSDb discovery / ingest | You use CSDb features | CSDb-related endpoints via RomM.Client.Csdb and/or a **csdb-bridge** you run; traffic depends on your setup. |
| Optional updates / package managers | Outside the app | winget, GitHub, NuGet, or Microsoft Store update channels are governed by those services' policies. |

ViceSharp does not phone home to a proprietary analytics backend as part of the core emulator. If a future build adds telemetry, it will be documented here before enablement.

## Permissions (UWP / Store package)

The Xbox / Store package may declare:

- **internetClient** - HTTPS downloads and remote library access.
- **privateNetworkClientServer** - LAN discovery and connection to a RomM server on your private network.

You can avoid default desktop firmware downloads by providing an existing ROM set through `VICESHARP_ROM_PATH` or `VICE_DATA_PATH`, or by retaining the verified firmware cache. Library and discovery connections are controlled separately by their connection and scan actions and by local-network permissions where the OS prompts.

## Children

ViceSharp is a general-purpose emulator utility. It is not directed at children under 13. Do not enter another person's credentials into the app.

## Third-party services

If you connect ViceSharp to third-party or self-hosted services (RomM, CSDb, ROM mirrors, Microsoft Store, GitHub), those services process data under **their** privacy policies. ViceSharp only sends what is needed for the feature you invoke (for example an API token you pasted, or a search string you typed).

## Source code and open distribution

ViceSharp is free software under **GPL-2.0-or-later**. Source: https://github.com/sharpninja/vice-sharp  

Reviewing the source is the most accurate way to verify what the app stores and transmits.

## Changes

Material changes to this policy will be reflected by updating this document and the effective date. Store submissions that depend on this URL should point at a stable path (this file in the public repository or wiki export).

## Contact

Questions or deletion requests for data the app stored only on your device: clear app data / uninstall, or open an issue at the repository above. The publisher does not operate a central user database for ViceSharp sessions.
