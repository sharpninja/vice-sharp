# TR-Vice-Oracle: Read-Only Native VICE Oracle Boundary

## TR-VICE-ORACLE-001: Read-only native VICE oracle boundary

**ID:** TR-VICE-ORACLE-001  
**Priority:** Critical  
**Status:** Pending  
**Area:** VICE  
**Subarea:** ORACLE

### Requirement

The `native/vice` gitlink is a read-only behavioral oracle. All stepping, tracing, snapshot, bus-event, and lifecycle integration must be implemented by external wrappers, launchers, link-time interposition, or public VICE interfaces that leave the pinned target source and semantics unchanged.

No patch application, generated source injection, or direct source edit under `native/vice` is permitted.

### Acceptance Criteria

1. **AC1:** The native oracle preflight fails before validation when any tracked file under `native/vice` differs from the pinned gitlink commit.
2. **AC2:** Wrapper builds and tests do not edit, generate source into, or apply patches to tracked `native/vice` paths.
3. **AC3:** Required CPU bus events, timing, snapshots, and lifecycle observations come from real immutable VICE execution through external wrapper mechanisms. `PeekBus`, PC-derived values, fabricated events, and modified native builds are forbidden substitutes.
4. **AC4:** Each oracle receipt records the `native/vice` commit, wrapper version or hash, clean-target proof, exact command, and zero-failure/zero-skip result for the claimed scope.
5. **AC5:** A missing wrapper capability is an explicit blocker and cannot be remediated by modifying `native/vice`.

### Traceability

- Functional requirement: `FR-VICE-001`
- Test requirement: `TEST-VICE-001`

