# RomM launch behavior

## FR-ROMM-LAUNCH-001: Download, attach, and boot library media

**ID:** FR-ROMM-LAUNCH-001
**Title:** Download, attach, and boot library media
**Priority:** P1 -- Important

The library shall acquire selected media into the user cache and attach or start it through the host boundary. This record preserves the launch contract in `docs/plans/PLAN-ROMM-BDPv4.md` and clarifies supported-format gating from PR7.

### Acceptance Criteria

1. AC-LAUNCH-01: Downloads stream to `<cacheDir>/{romId}/{fs_name}`, reuse a complete size-matched cached file, and report progress.
2. AC-LAUNCH-02: Attachment uses the selected slot or the extension mapping's default slot.
3. AC-LAUNCH-03: Raw PRG and disk formats without a host parser (G64, D71, D81) remain browsable but are not launchable. Attach and Attach-and-Start are disabled and do not download or invoke the launcher. Extension matching is case-insensitive. Supported D64 and T64 behavior remains available.
4. AC-LAUNCH-04: Attach and Attach-and-Start invoke `IGameLauncher` with the resolved slot and autostart flag.
5. AC-LAUNCH-05: The Xbox launcher attaches media bytes and uses disk autostart or cartridge cold reset as appropriate.
6. AC-LAUNCH-06: The Avalonia launcher delegates to the shell's attach or drop-and-start path.
7. AC-LAUNCH-07: Status progresses through downloading and starting; failures remain visible and retryable.

### Traceability

- Technical requirements: TR-LIB-001, TR-ROMM-SEC-001.
- Test requirement: TEST-ROMM-LAUNCH-001.
- Tests: `MediaExtensionMapTests`, `LibraryBrowseViewModelTests`, `AvaloniaGameLauncherTests`, `XboxGameLauncherTests`, `RomMGatewayDownloadTests`.
