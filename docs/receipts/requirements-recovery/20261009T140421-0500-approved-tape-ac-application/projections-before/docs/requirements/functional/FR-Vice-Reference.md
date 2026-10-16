# FR-Vice-Reference: Explicit Integrity Guardrail for the Native VICE Behavioral Target

## FR-VICE-001: Preserve the established immutable native VICE behavioral target

**ID:** FR-VICE-001  
**Priority:** Critical  
**Status:** Pending  
**Area:** VICE

### Requirement

This requirement does not introduce new emulator behavior. It makes the original VICE-as-reference contract executable: ViceSharp shall conform managed emulator behavior to a pinned, unmodified native VICE revision. Native VICE source and oracle behavior shall never be changed to make ViceSharp pass. Observation and control integrations must be implemented as wrappers outside `native/vice`.

The underlying authority predates this guardrail. The first committed `TR-CYCLE-001` names VICE x64sc as the reference implementation and accepted gold standard, while `FR-SNP-005` and `TR-LOCKSTEP-VSF-001` preserve native x64sc and xvic as the behavioral and file-compatibility oracles.

### Acceptance Criteria

1. **AC1:** Every parity or lockstep acceptance run identifies the pinned `native/vice` commit and proves no tracked `native/vice` file differs from that commit before or after the run.
2. **AC2:** Emulator-behavior corrections are made in managed ViceSharp. Native integration code may wrap or observe VICE only from outside `native/vice` and may not alter target source or behavior.
3. **AC3:** Oracle binaries used as acceptance evidence are built from the pinned unmodified VICE target. Evidence from a patched or behavior-modified VICE build is rejected.
4. **AC4:** When an external wrapper cannot obtain a required observation from immutable VICE, the parity gate fails closed and reports the missing observation instead of inferring data or changing VICE.

### Traceability

- Technical requirement: `TR-VICE-ORACLE-001`
- Test requirement: `TEST-VICE-001`
