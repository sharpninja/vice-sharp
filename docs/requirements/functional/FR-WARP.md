# FR-WARP-001: Warp mode

**ID:** FR-WARP-001
**Title:** Warp mode runs uncapped relative to 100 percent limiter
**Priority:** high
**Area:** WARP

## Description

Settings Warp (VICE -warp) disables the speed limiter so the emulator runs as fast as the host allows. The status bar must show LIMITER WARP and CLOCK percent well above 100 when warp is applied through the UI or RemoteControl.

## Acceptance Criteria

1. Warp via Settings or RemoteControl sets status LIMITER to WARP.
2. After warp is applied, status CLOCK percent is well above 100.

## Traceability

- TR: TR-WARP-STATUS-001
- TEST: TEST-UISET-002
- Tests: WarpModeTests, live RemoteControl dumps
