# RomM Security Technical Requirement

## TR-ROMM-SEC-001: Credential, origin, and download-boundary protection

**ID:** TR-ROMM-SEC-001
**Title:** Keep RomM bearer tokens and downloaded content inside trusted boundaries
**Last Updated:** 2026-10-15
**Status:** Implemented on Windows and Linux desktop paths

The RomM adapter must:

1. Fetch public absolute cover URLs with an anonymous client.
2. Send bearer authentication only for server-relative cover paths resolved under the configured RomM base address; reject absolute authenticated-path inputs.
3. Accept only one relative ROM filename, contain the resolved destination under the per-ROM cache directory, stream into a temporary file, validate the expected byte count, and atomically publish a complete cache entry.
4. Protect file-backed tokens on Windows with current-user DPAPI, atomically persist the protected record, and immediately migrate a legacy plaintext record after successful load.
5. Fail closed on unsupported operating systems instead of silently persisting plaintext credentials.
6. On Linux, use the current user's Secret Service keyring through `secret-tool`, passing credentials only through standard input. Persist only connection metadata and a random keyring reference in an atomic, owner-readable/writable file. Migrate legacy plaintext only after keyring storage succeeds.
7. Preserve the previous Linux connection if replacement metadata cannot be committed. Reject unavailable, locked, missing, or timed-out keyring operations with an actionable message that does not contain command output or credential values. Cleanup failure after a successful replacement must not invalidate the new connection.

**Related FRs:** FR-ROMM-CONN-001, FR-ROMM-COVER-001, FR-ROMM-LAUNCH-001
**Verification:** TEST-ROMM-SEC-001
