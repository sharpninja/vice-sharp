# Immutable Native VICE Oracle Traceability

Recorded from the operator directive on 2026-10-03: native VICE is the immutable behavioral target. Wrappers outside `native/vice` are allowed; changing native VICE is forbidden.

- `FR-VICE-001` AC1-AC4 map to `TR-VICE-ORACLE-001` AC1-AC5.
- `TEST-VICE-001` AC1 proves clean-target preflight and dirty-target rejection.
- `TEST-VICE-001` AC2 proves wrapper build and execution leave tracked target source unchanged.
- `TEST-VICE-001` AC3 proves target and wrapper provenance plus zero-failure/zero-skip evidence.
- `TEST-VICE-001` AC4 proves missing observability fails closed without target modification or inferred events.

No acceptance criterion is satisfied yet. The existing dirty `native/vice` checkout and binaries built from it are not acceptable completion evidence for these requirements.

