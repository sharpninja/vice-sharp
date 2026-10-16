# R0-01: actual TEST-record acceptance criteria

All changes below are proposed only. Existing FR-to-TEST mapping rows are retained explicitly in the exact candidate. They no longer lead solely to zero-AC TEST records for the five rewritten criteria at issue.

- FR-CPU-004/ac-03 -> TEST-CPU-001/ac-01: motor/write DATA/DDR truth table, transitions, repeated writes and changed-effective-output calls. Negative controls reject swapped bits, polarity errors and missing or extra forwarding.
- FR-CPU-004/ac-05 -> TEST-CPU-001/ac-02: bit5 input/output read matrix, preserved unrelated bits and read side effects. Controls reject pull-up, forced-zero output, stale direction and SX behavior on C64.
- FR-CPU-004/ac-06 -> TEST-CPU-001/ac-03..ac-10: eight independently specified capacitor groups, copied into TEST criterion text from the source/case contract: jitter endpoints, DDR transitions, repeated writes, lazy strict expiry, board/pin matrix, RNG order, reset/wrap, PAL/NTSC/configured models. Independent mocks and negative controls precede identical real assertions.
- FR-PRF-003/ac-02 -> TEST-PRF-001/ac-01: SX outputs disconnected, C64 control still connected, native DATA/DDR/retained-charge reads retained.
- FR-PRF-003/ac-04 -> TEST-PRF-001/ac-02..ac-04: independent bits/reset, strict falloff/read side effects and deadline/RNG selection. The exact TEST text includes corruption controls, not only case names.

The machine-readable `testCriterionTraceability` field contains these same bindings. Verification resolves each FR criterion, TEST criterion, record and mapping edge by identity. This proves proposed textual traceability only. Neither TEST record's broad pre-existing condition is replaced; all unrelated CPU and profile obligations remain required. The zero-AC TEST-X64SC-LOCKSTEP-001 is not claimed repaired or used as a substitute for these explicit criteria.
