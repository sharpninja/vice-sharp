# TR-WARP-STATUS-001: Warp status DTO and CLOCK percent

**ID:** TR-WARP-STATUS-001
**Title:** Host status DTO emits warp as rate 0 and live CLOCK percent
**Priority:** high
**Area:** WARP
**Subarea:** STATUS

## Description

ToStatusDto emits LimiterRatePercent 0 when LimiterEnabled is false so the status bar can show WARP. EffectiveClockPercent is computed from sampled EffectiveClockHz over MasterClockHz. Warp must push audio relative speed to the live-audio ceiling so VIC-I sound render does not pin CLOCK at 100 percent.

## Acceptance Criteria

1. ToStatusDto emits LimiterRatePercent 0 when LimiterEnabled is false.
2. GetStatus after warp reports EffectiveClockPercent greater than 150 in WarpModeTests and live RC dumps.

## Traceability

- FR: FR-WARP-001
- TEST: TEST-UISET-002
