# FR-ID / finding crosswalk (PLAN-VICEPARITY-001 P0-6)

Maps the requirement scheme used by requirements.yaml (new, per-algorithm FR ids)
to the older ids annotated in code, tests and docs/requirements exports. Purpose:
old annotations stay resolvable during remediation, no AC gets double-covered, and
the MCP requirements import (on reconnect) can supersede rather than duplicate.
The `finding` field links each AC to the PLAN-VICEPARITY-001 audit findings:
a 2-3 digit finding number (01-47; reconcile ids against the persisted TODO
when MCP is back) or the literal `new` for the 17 ACs born from the
authoring-time discoveries enumerated in requirements.yaml's newFindings
header (assign real finding numbers when PLAN-VICEPARITY-001 is updated on
reconnect). Every DIVERGENT AC cites a finding; 99 FAITHFUL locks carry a
blank finding (nothing diverged, nothing was "found").

## SID (new id -> old scheme)

- FR-SID-ENV -> FR-SID-006 (ADSR envelope)
- FR-SID-WAVE-ACC, FR-SID-WAVE-SAWTRI, FR-SID-WAVE-PULSE, FR-SID-WAVE-TESTBIT,
  FR-SID-WAVE-DACRES -> FR-SID-001, FR-SID-002 (oscillator/waveform core)
- FR-SID-WAVE-SYNC -> FR-SID-008 (hard sync)
- FR-SID-WAVE-RING -> FR-SID-007 (ring modulation)
- FR-SID-WAVE-NOISE -> FR-SID-009 (noise LFSR; superseded suite deleted in P0-4)
- FR-SID-WAVE-COMBINED -> FR-SID-003 (combined waveforms; AND-model suites deleted in P0-4)
- FR-SID-FILTER-6581, FR-SID-CUTOFFDAC, FR-SID-FILTER-CLOCK -> FR-SID-004
  (6581 filter; docs export superseded via MCP on reconnect)
- FR-SID-FILTER-8580, FR-SID-8580 -> FR-SID-005 (8580 filter/model; ditto)
- FR-SID-VOICE, FR-SID-MIXVOL, FR-SID-OUTPUT -> FR-SID-010, FR-SID-014 (voice DAC/mix/output)
- FR-SID-OSC3ENV3, FR-SID-CLOCK, FR-SID-DATABUS, FR-SID-POT -> no old FR
  (previously untracked behaviors; new coverage)

## VIC-II (new id -> old scheme)

- FR-VIC-CYCLE -> FR-VIC-006, FR-VIC-010, TR-CYCLE-001
- FR-VIC-FETCH, FR-VIC-MATRIX-ADDR -> FR-VIC-005
- FR-VIC-DRAW-GFX, FR-VIC-DRAW-COLOR, FR-VIC-XSCROLL -> FR-VIC-002, FR-VIC-003,
  BACKFILL-VIDEO-001, PLAN-VICRENDER-001 (per-line renderer + change-log hack, retired at V7)
- FR-VIC-DISPLAYMODE -> FR-VIC-008
- FR-VIC-SPRITE-RENDER, FR-VIC-SPRITE-DMA, FR-VIC-SPRITE-COLLISION,
  FR-VIC-SPRITE-PRIORITY -> FR-VIC-004
- FR-VIC-BORDER -> FR-VIC-007, PLAN-VICRENDER-001
- FR-VIC-RASTER-IRQ -> FR-VIC-001
- FR-VIC-LIGHTPEN -> FR-VIC-001, TR-VIC-EDGE-003
- FR-VICII-RASTER-001 / TR-LOCKSTEP-VSF-001 (raster-bar lockstep suite) remain
  live oracle-based gates alongside the new scheme (not superseded).

## Test-infrastructure TRs introduced by Phase 0

- TR-SID-ORACLE-001: single-cycle reSID oracle (P0-1) + SID wiring (P0-7) + IVT smoke (P0-3)
- TR-VIC-ORACLE-001: per-pixel frame oracle (P0-2)
- TR-PARITY-GATE-001: ParityAc quarantine + coverage manifest (P0-8)

## Rules

1. New tests cite the NEW FR ids ([ParityAc] binds the yaml test id; the manifest
   enforces tag + uniqueness).
2. Old annotations in surviving files are left as-is until their slice touches the
   file, then updated to the new id with the old id in parentheses.
3. MCP reconnect: create the new FR/AC/TR/TEST records from requirements.yaml,
   supersede FR-SID-004/005 (never duplicate), and attach this crosswalk to
   PLAN-REQAUDIT-001.
