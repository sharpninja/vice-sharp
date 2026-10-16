# External VSF compatibility byte contracts

Scope: `TR-VSFLOCKSTEP-RESUME-001` AC1, Phase 2 of `PLAN-VICE-IMMUTABLE-001`.
This native-safe project does not reference TestHarness or any native build target.
It tests the external `ViceSharp.ViceCompatibility` library, not an alternative
snapshot format or a modified VICE reader.

The contract cases first passed against mocks before the implementation existed.
Each supported version, header variant and module placement has an independent
expected byte sequence. Incorrect mocks corrupt every output byte individually,
change output length, mutate input, accept invalid data, or throw the wrong
exception. The real implementation uses the same fixtures and assertions.

The layout is pinned to upstream commit
`cc67418cba58e71d8e70fd353e9de47199ad2075`. The x64sc machine tag is `C64SC`,
not `C64`. The supported VSF container version is 2.0. MAINCPU 1.2, 1.3 and 1.4
have inclusive sizes 113, 121 and 125 bytes, using eight-byte clocks. The adapter
validates the complete module chain before returning any normalized bytes,
inserts only missing zero defaults, and preserves unknown modules and order.
Unsupported/compressed layouts fail closed; the adapter has no file I/O.

Run the complete suites, without a filter, through PowerShell.MCP:

```powershell
dotnet test .\tests\ViceSharp.VsfCompatibility.Tests\ViceSharp.VsfCompatibility.Tests.csproj -c Release --logger "trx;LogFileName=vsf-compatibility.trx" --results-directory .\TestResults\vsf-compatibility --diag .\TestResults\vsf-compatibility\vstest.diag.log --blame-hang --blame-hang-timeout 2m
dotnet test .\tests\ViceSharp.OracleContract.Tests\ViceSharp.OracleContract.Tests.csproj -c Release --logger "trx;LogFileName=oracle-contracts.trx" --results-directory .\TestResults\oracle-contracts --diag .\TestResults\oracle-contracts\vstest.diag.log --blame-hang --blame-hang-timeout 2m
```

Retained execution evidence is under
`docs/receipts/native-vice/20261008T082100-0500-phase2-feasibility/`:

- `mock-tests-01`: 13 passed, 24 failed due to reconfiguring a throwing substitute.
  Corrected by using separate substitutes, not by removing assertions.
- `mock-tests-02`: 37 passed, zero failed/skipped/aborted. A later source/fixture
  check found the provisional `C64` tag was wrong for x64sc.
- `mock-tests-03`: corrected `C64SC` fixtures plus a rejecting `C64` case,
  38 passed, zero failed/skipped/aborted, before adapter implementation.
- `implementation-tests-01`: all 77 mock and real byte tests passed, zero
  failed/skipped/aborted.
- `cumulative-oracle-02`: all prior 82 oracle mock contracts passed, zero
  failed/skipped/aborted.
- Each run retains TRX, diagnostic log, stdout and native-integrity snapshots.

## Fixture provenance and limits

The binary fixtures are byte-identical copies of retained workspace snapshots;
their originals remain intact. Copy receipts and SHA-256 values are in
`fixture-copy-provenance.json` in the evidence directory above.

- `Fixtures/native-maincpu-1.2.vsf`: copied from
  `tests/ViceSharp.TestHarness/Fixtures/Vsf/ready-c64sc-truedrive.vsf`.
- `Fixtures/native-maincpu-1.4.vsf`: copied from
  `validation-output/wolf64-frame-diff-20260915/wolf64-menu.vsf`.

These are retained compatibility inputs, not freshly produced immutable-oracle
evidence. The 1.3 fixture is synthetic. Byte-contract success does not prove
native resume, VIC-II model alignment, snapshot error normalization, immutable
native round trips, 100000-cycle parity, or integration into public snapshot
paths. Those gates remain mandatory and open. No requirement is satisfied here.

HV is pending under the operator's 2026-10-08 waiver until noon America/Chicago.
The scheduled local Opus catch-up must review this entire slice and its failures.

## Binary monitor slice

`BINARY-MONITOR-SLICE.md` locks the additional public API-2 codec contract for
FR-VICE-001 AC4, TR-VICE-ORACLE-001 AC3/AC5 and TEST-VICE-001 AC4. The same
61 assertion cases run first with independent correct/incorrect mocks, then
through the production adapter. They cover framing, fragmentation, adjacent
packets, truncation, body bounds, input ownership, event IDs and failure ACKs.

`binary-mock-03` passed 138/138 before production; `binary-prior-mock-02` passed
82/82. `binary-implementation-02` then passed 199/199 and
`binary-prior-implementation-01` passed 82/82. All are full unfiltered Release
runs with zero failed/skipped/aborted and retained native-integrity checks.
The intermediate mock-name parser failure and parent assembly-lock build failure
are preserved and are not passing runs. Details and actual stock native ACK
evidence are in the Phase 2 `binary-runtime/README.md`. These 281 native-safe
tests still do not replace full real-emulator gates or independent HV.
