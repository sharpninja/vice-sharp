# FR-Performance: Runtime Performance Functional Requirements

## FR-PERF-RUNFRAME-001: C64 PAL RunFrame Throughput

**ID:** FR-PERF-RUNFRAME-001

**Description:** Managed C64 PAL emulation must execute `IMachine.RunFrame()` fast enough for a host application to sustain 50.125 Hz PAL playback with remaining budget for blit and audio work.

**Acceptance Criteria:**
- A production C64 PAL machine built through `ArchitectureBuilder` with real C64 ROMs is the measured target; romless and minimal-host machines are not valid evidence.
- After 60 warmup frames, a 600-frame Release/net10.0 managed-only run reports median frame time `<= 18.0 ms`.
- The same run reports p95 frame time `<= 22.0 ms`.
- The measured `RunFrame()` loop reports `0` bytes allocated on the current thread.
- Public signatures for `IMachine`, `IVideoChip`, `IAudioChip`, `IBus`, `IKeyboardMatrix`, `ArchitectureBuilder`, and `C64MachineProfiles` remain unchanged.

**Technical Requirements:** TR-CYCLE-001, TR-DET-001, TR-ALLOC-001, TR-AOT-001

**Test Requirements:** TEST-PERF-RUNFRAME-001, TEST-X64SC-LOCKSTEP-001
