# RomM launch regression coverage

## TEST-ROMM-LAUNCH-001: Supported media and launch recovery

**ID:** TEST-ROMM-LAUNCH-001
**Title:** Supported media and launch recovery
**Priority:** P1 -- Important

### Condition

Verify download completion and cache reuse, slot selection, supported D64/T64 attach and autostart, UI-visible failure and retry, and each desktop launch boundary. Unsupported G64/D71/D81 selections, including uppercase names, must disable attachment and issue neither a download request nor a launcher call. Use synthetic media and in-memory HTTP fixtures; never commit user game files.

### Traceability

- Functional requirement: FR-ROMM-LAUNCH-001.
- Technical requirements: TR-LIB-001, TR-ROMM-SEC-001.
- Regressions: `MediaExtensionMapTests.UnsupportedDiskFormat_NotLaunchable`, `LibraryBrowseViewModelTests.UnsupportedDisk_AttachDisabled`, plus the existing D64/T64 and launcher suites.
