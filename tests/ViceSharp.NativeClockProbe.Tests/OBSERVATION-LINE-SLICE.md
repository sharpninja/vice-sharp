# Observation line contract (Phase C step 1)

Scope: the stdout line an external wrapper (the extended `vic_cycle` and clock-gate wrappers of Phase C step 3) writes for each half-cycle. Mock-first; no decoder or C emitter exists yet. Serves FR-VICE-001 AC4, TR-VICE-ORACLE-001 AC3 and AC5, TEST-VICE-001 AC4. The field meanings are those of `tests/ViceSharp.OracleContract.Tests/OBSERVATION-RECORD-SLICE.md`.

## Format

```
VICE_OBS,1,<halfCycle>,<PHI1|PHI2>,<aec 0|1>,<ba 0|1>,<read|write|idle|dummy>,<address>,<data>,<interruptMask>,<stolen 0|1>,<clock>\n
```

Decimal integers without sign, padding or `0x`, positional like the existing `VICE_GATE,<clock>` line. Exactly 12 columns and one `\n` terminator; CRLF, a missing terminator or a second line is a framing fault. Version `1`.

## Decoder results

`LineDecode` carries exactly one of `Record` or `Error`. Errors: `Missing:<Field>` (empty column), `Malformed:<Field>` (out-of-domain token), `Framing:Prefix`, `Framing:Version`, `Framing:ColumnCount`, `Framing:Terminator`.

## Mock gate (`ObservationLineContractTests`)

9 valid lines, 10 missing-field controls, 24 malformed-value controls (every field has at least one), 10 framing controls, plus 2 structure guards. Each case proves the correct mock passes and every single-field corruption of the result is rejected (13 corruptions per valid line, 4 per rejection). The real decoder and the C emitter bind to the same fixtures in Phase C steps 2 to 4.
