# TEST-Vice-Oracle: Immutable Native VICE Oracle Integrity Gates

## TEST-VICE-001: Immutable native VICE oracle integrity gates

**ID:** TEST-VICE-001  
**Priority:** Critical  
**Status:** Pending  
**Area:** VICE

### Condition

Verify that every native parity oracle uses the pinned unmodified VICE target and that wrapper-only integration fails closed when required target observations are unavailable.

### Acceptance Criteria

1. **AC1:** A clean-target preflight passes for the pinned `native/vice` commit, and a simulated dirty-target fixture is rejected before parity execution.
2. **AC2:** The wrapper integration test proves no tracked `native/vice` file changes across build and execution.
3. **AC3:** Oracle receipts prove native commit identity, clean tracked source, wrapper provenance, exact commands, and failed 0 skipped 0 for the claimed gate.
4. **AC4:** When a required real bus or timing observation is unavailable, the test fails at that first missing observation and does not substitute `PeekBus`, PC inference, fabricated data, or a modified native build.

### Traceability

- Functional requirement: `FR-VICE-001`
- Technical requirement: `TR-VICE-ORACLE-001`

