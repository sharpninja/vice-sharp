# External VICE compatibility and monitor protocol

`X64ScSnapshotCompatibility.Normalize` validates an uncompressed C64SC VSF 2.0
container and normalizes legacy MAINCPU 1.2/1.3 payloads for the pinned immutable
VICE 1.4 reader. Current 1.4 input remains byte-identical. The method returns a
new array, never mutates the input, and fails closed on malformed or unsupported
layouts. It performs no filesystem or native calls.

This library is the external adapter slice of `TR-VSFLOCKSTEP-RESUME-001` AC1.
It is not yet integrated into the production bridge or normal snapshot paths.
It does not replace native VSF, VICE, or the existing managed snapshot support.
Native resume and all lockstep gates remain open. See the native-safe tests at
`tests/ViceSharp.VsfCompatibility.Tests` for source anchors, fixture provenance,
retained failures and execution receipts. Hostile validation is pending.

`BinaryMonitorProtocol` encodes API-2 commands, reads exactly one bounded response
without consuming a following frame, and requires explicit matching successful
acknowledgements. It preserves native errors and unsolicited event IDs as data.
The stream owner must supply transport timeouts; command-specific body semantics
remain the caller's responsibility. It does not implement a transport, alter
VICE, or infer cycle observations. See the test project's `BINARY-MONITOR-SLICE.md`
and the Phase 2 `binary-runtime/README.md` receipts. Successful snapshot command
acknowledgements do not establish complete snapshot or lockstep parity.
