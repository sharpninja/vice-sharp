# ViceSharp.RomM

Part of the ViceSharp project (C#/.NET 10 VICE-derived Commodore emulator).

## Desktop library

In the Avalonia **Library** tab, enter the RomM URL and **Client API token**, then select **Connect**. Enter a same-computer server explicitly, such as `http://127.0.0.1:8080/`; LAN discovery scans other hosts. Search the C64 library and choose **Attach** or **Attach + play**. Completed downloads are reused from `ViceSharp/romm-cache` beneath the user's local application-data directory, and successful attachments/launches appear in Recents.

Download, attachment, and session-creation failures become visible Library status messages. A rejected disk can be corrected or replaced and retried without restarting the app. T64 archives use a separate converted D64 for True Drive session recreation; the original archive and recent-file identity are preserved. See the [desktop user guide](../../docs/USER-GUIDE.md#6c-romm-library) for the user workflow and [Linux installation](../../README.md#linux-install-for-your-user) for user-local deployment.

The desktop grid uses virtualized cover tiles, grouped variants, letter jumps and 60-record paging. A selected group's primary ROM supplies details and attachment. Page-boundary variants update the existing tile without clearing selection. Keyboard navigation remains relative to the selected tile after scrolling. Selecting a collection in the Library rail opens and selects the same collection in Lists, including repeat selections.

`LibraryCoverImage` owns its displayed bitmap and cancels stale loads on replacement or visual detachment. One `AvaloniaCoverImageLoader` and shared byte cache belong to each successful browse connection, survive tab switches, and are disposed on replacement or window close. The loader uses the connected URL/token, not uncommitted edits in the connection form. Stale detail or collection responses cannot replace the current selection after a newer request.

## Desktop validation

Use Nuke `RommDesktopTest` for the managed Heads and Library suites, and add `--romm-contract-mocks` for the pre-implementation mock gate. See [commands, scope and retained evidence](../../docs/testing/RomM-Desktop.md). These checks do not build or modify native VICE and are not proof of full emulator parity or a live server session.

## Security and persistence

- Public `url_cover` CDN assets use an anonymous client. Authenticated cover requests accept only server-relative `path_cover_*` paths, preventing bearer-token forwarding to another origin.
- ROM downloads accept a single relative filename, stay within the per-ROM cache directory, validate the expected byte count, and publish only a completed temporary file.
- On Windows, `FileRomMConnectionStore` protects tokens with current-user DPAPI and atomically migrates legacy plaintext connection files. The file-backed credential store intentionally fails on unsupported operating systems instead of writing plaintext.
- On Linux, the desktop uses `LinuxSecretServiceRomMConnectionStore`: tokens are stored in the current user's Secret Service keyring through `/usr/bin/secret-tool` (the `libsecret` package on Arch). The login keyring must be unlocked. `romm-connection.json` contains only the server URL, authentication mode, username, and a random keyring reference; it is written atomically with owner-only permissions. Failed keyring access is reported without falling back to plaintext storage.

The desktop stores connection metadata and recents beneath its per-user `ViceSharp` data directory, normally `~/.local/share/ViceSharp` on Linux (`XDG_DATA_HOME` is honored). Connect once in the Library tab using the local RomM URL and a Client API token; subsequent launches reload the remembered connection. The token is sent to `secret-tool` through standard input, never command-line arguments. If persistence fails, the connected session can remain usable while the UI reports that credentials could not be saved.

License: GPL-2.0-or-later (derivative of VICE). Part of the ViceSharp project: https://github.com/sharpninja/vice-sharp

### Commodore C= logo (CC BY-SA 4.0)

The Commodore C= logo used in ViceSharp product branding (where present) is from Wikimedia Commons ([Commodore C= logo.svg](https://commons.wikimedia.org/wiki/File:Commodore_C%3D_logo.svg)) by Alien426, licensed under [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/). See the repository `THIRD_PARTY_NOTICES.md` for full third-party attribution.
