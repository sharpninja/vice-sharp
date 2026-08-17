# ViceSharp.RomM

Part of the ViceSharp project (C#/.NET 10 VICE-derived Commodore emulator).

## Security and persistence

- Public `url_cover` CDN assets use an anonymous client. Authenticated cover requests accept only server-relative `path_cover_*` paths, preventing bearer-token forwarding to another origin.
- ROM downloads accept a single relative filename, stay within the per-ROM cache directory, validate the expected byte count, and publish only a completed temporary file.
- On Windows, `FileRomMConnectionStore` protects tokens with current-user DPAPI and atomically migrates legacy plaintext connection files. The file-backed credential store intentionally fails on unsupported operating systems instead of writing plaintext.

License: GPL-2.0-or-later (derivative of VICE). Part of the ViceSharp project: https://github.com/sharpninja/vice-sharp

### Commodore C= logo (CC BY-SA 4.0)

The Commodore C= logo used in ViceSharp product branding (where present) is from Wikimedia Commons ([Commodore C= logo.svg](https://commons.wikimedia.org/wiki/File:Commodore_C%3D_logo.svg)) by Alien426, licensed under [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/). See the repository `THIRD_PARTY_NOTICES.md` for full third-party attribution.
