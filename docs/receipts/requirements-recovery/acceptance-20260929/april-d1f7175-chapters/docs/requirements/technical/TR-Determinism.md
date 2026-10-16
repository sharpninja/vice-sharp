# TR-Determinism: Determinism Technical Requirement

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Quality Area   | Correctness / Reproducibility  |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-04-13                     |

---

## TR-DET-001: Bit-Exact Reproducibility

**ID:** TR-DET-001
**Title:** Bit-Exact Reproducibility Given Same Initial State and Inputs
**Priority:** P0 -- Critical
**Category:** Correctness

### Description

Given the same initial machine state (snapshot) and the same sequence of input events (keyboard, joystick, timing), the emulator shall produce bit-exact identical output (video frames, audio samples, final machine state) across all runs, all platforms, and all compilation modes (Debug, Release, NativeAOT).

### Rationale

Determinism is a foundational requirement for: (1) snapshot-based replay (FR-SNP-003), (2) automated testing against reference outputs, (3) TAS (tool-assisted speedrun) support, (4) networked multiplayer synchronization (future), and (5) bug reproduction.

### Technical Specification

1. **No Floating-Point in Emulation Core:** The emulation core (CPU, VIC-II, CIA, SID oscillator/envelope) shall use only integer arithmetic. Floating-point is permitted only in the audio output resampling stage (post-SID, not part of the emulated state).
2. **No Uninitialized State:** All state variables are explicitly initialized. RAM initialization follows the known C64 power-on pattern (alternating $00/$FF blocks).
3. **No Random Number Generation:** The emulation core does not use `System.Random` or any PRNG. All "random" behavior comes from the emulated hardware state (e.g., SID noise LFSR, uninitialized RAM patterns).
4. **Deterministic Tick Order:** Devices are ticked in a fixed, documented order within each clock phase. The order does not change based on runtime conditions.
5. **No Thread-Dependent State:** The emulation core runs on a single thread. There are no data races, lock-dependent ordering, or thread-pool scheduling dependencies.
6. **Platform-Independent Arithmetic:** All integer operations produce identical results on x86, x64, and ARM64. No reliance on platform-specific overflow behavior beyond what C# guarantees.

### Acceptance Criteria

1. A "replay determinism" test loads a snapshot, replays 10 million cycles of recorded input, and compares the final state hash: the hash must be identical across 100 consecutive runs.
2. The same test produces identical hashes on Windows x64, Linux x64, macOS ARM64, and Linux ARM64.
3. The same test produces identical hashes for Debug, Release, and NativeAOT builds.
4. Video frame checksums (CRC32 of raw pixel data) match for every frame between two runs of the same replay.
5. Audio sample checksums match for every audio buffer between two runs of the same replay.
6. The `[Deterministic]` custom attribute is applied to all emulation-core methods, and a Roslyn analyzer verifies no non-deterministic operations are used.

### Verification Method

- Cross-platform determinism test in CI (Windows, Linux, macOS runners).
- Replay regression test suite that compares frame/audio checksums against golden reference files.
- Static analysis (Roslyn analyzer) for floating-point usage and non-deterministic patterns in annotated code.

### Related FRs

- FR-SNP-003 (Deterministic replay depends on this TR)
- FR-SNP-001 / FR-SNP-002 (Snapshot save/load must be complete for determinism)

### Related TRs

- TR-CYCLE-001 (Cycle accuracy ensures the tick order matches hardware)
- TR-SIMD-001 (SIMD and scalar paths must produce identical results)
- TR-STATE-001 (ACID state transactions ensure consistent snapshots)

### Design Decisions

- SID filter computation uses fixed-point arithmetic (Q16.16) in the emulation core; float conversion happens only at the audio output boundary.
- The frame buffer stores raw pixel indices (palette indices), not RGB values, until the output stage.
- RAM initialization pattern is configurable but defaults to the documented C64 power-on pattern for deterministic cold starts.
