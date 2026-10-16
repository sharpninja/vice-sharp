# RomM Security Technical Requirement

## TR-ROMM-SEC-001: Credential, origin, and download-boundary protection

**ID:** TR-ROMM-SEC-001
**Title:** Keep RomM bearer tokens and downloaded content inside trusted boundaries
**Last Updated:** 2026-08-17
**Status:** Implemented on the supported Windows desktop path

The RomM adapter must:

1. Fetch public absolute cover URLs with an anonymous client.
2. Send bearer authentication only for server-relative cover paths resolved under the configured RomM base address; reject absolute authenticated-path inputs.
3. Accept only one relative ROM filename, contain the resolved destination under the per-ROM cache directory, stream into a temporary file, validate the expected byte count, and atomically publish a complete cache entry.
4. Protect file-backed tokens on Windows with current-user DPAPI, atomically persist the protected record, and immediately migrate a legacy plaintext record after successful load.
5. Fail closed on unsupported operating systems instead of silently persisting plaintext credentials.

**Related FRs:** FR-ROMM-CONN-001, FR-ROMM-COVER-001, FR-ROMM-LAUNCH-001
**Verification:** TEST-ROMM-SEC-001
