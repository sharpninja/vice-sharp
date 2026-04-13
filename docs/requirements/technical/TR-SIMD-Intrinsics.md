# TR-SIMD-Intrinsics: SIMD Acceleration Technical Requirement

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Quality Area   | Performance / Throughput       |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-04-13                     |

---

## TR-SIMD-001: SIMD-Accelerated Rendering and Audio

**ID:** TR-SIMD-001
**Title:** SIMD-Accelerated Rendering and Audio with Generic Specialization for CPU Core
**Priority:** P1 -- Important
**Category:** Performance

### Description

Performance-critical rendering and audio processing paths shall use SIMD (Single Instruction, Multiple Data) intrinsics to process multiple pixels or audio samples in parallel. The CPU core shall use generic specialization (constrained generics with `where T : struct`) to allow the JIT/AoT compiler to generate specialized machine code for different CPU variants (6502, 6510, 8502) without virtual dispatch overhead.

### Rationale

SIMD can process 4-16 pixels or audio samples per instruction, providing 4-16x throughput improvement for bulk data operations. Generic specialization eliminates virtual dispatch on the hottest code path (instruction decode and execute) while maintaining a single source implementation.

### Technical Specification

1. **SIMD Rendering Pipeline:**
   - VIC-II pixel output uses `Vector128<byte>` or `Vector256<byte>` to write 16-32 pixels per SIMD instruction.
   - Sprite rendering uses SIMD for collision detection (bitwise AND of sprite masks).
   - Color palette lookup uses SIMD gather operations where available.

2. **SIMD Audio Pipeline:**
   - SID audio mixing (combining 3 voices + filter output) uses `Vector128<float>` or `Vector256<float>`.
   - Audio resampling (SID clock rate to output sample rate) uses SIMD FMA instructions.
   - Volume envelope application uses SIMD multiply.

3. **Generic Specialization for CPU:**
   - The CPU core is implemented as `CpuCore<TBus>` where `TBus : struct, IBus`.
   - Different bus implementations (C64Bus, C128Bus, Vic20Bus) provide specialized memory access without virtual dispatch.
   - The JIT/AoT compiler generates separate native code for each `TBus` instantiation.

4. **Platform Detection:**
   - SIMD support is detected at startup via `System.Runtime.Intrinsics.X86.Sse2.IsSupported`, `Avx2.IsSupported`, `System.Runtime.Intrinsics.Arm.AdvSimd.IsSupported`.
   - Fallback scalar implementations exist for all SIMD-accelerated paths.
   - The fastest available SIMD width is selected automatically.

### Acceptance Criteria

1. VIC-II line rendering with SIMD is at least 4x faster than the scalar fallback (measured by benchmark).
2. SID audio sample generation with SIMD is at least 2x faster than scalar.
3. Generic specialization eliminates all virtual/interface dispatch from the CPU decode-execute loop (verified by JIT disassembly inspection).
4. Scalar fallback paths produce bit-identical output to SIMD paths (verified by determinism tests).
5. All SIMD code compiles and runs correctly on: x64 (SSE2 minimum, AVX2 optional), ARM64 (NEON/AdvSIMD).
6. NativeAOT compilation produces specialized code for each generic instantiation (no shared generic fallback).

### Verification Method

- Benchmark suite comparing SIMD vs. scalar for rendering and audio pipelines.
- JIT disassembly capture (via BenchmarkDotNet `[DisassemblyDiagnoser]`) verifying absence of virtual dispatch in CPU loop.
- Cross-platform CI running on x64 and ARM64 targets.
- Determinism tests comparing SIMD and scalar output byte-by-byte.

### Related TRs

- TR-ALLOC-001 (SIMD operates on stack-allocated vectors, no heap allocation)
- TR-AOT-001 (SIMD intrinsics are AoT-compatible; generic specialization requires AoT to monomorphize)
- TR-DET-001 (Scalar and SIMD must produce identical results for determinism)

### Design Decisions

- `Vector128<T>` is the baseline SIMD width (available on all modern CPUs including ARM64 NEON).
- `Vector256<T>` is used opportunistically when AVX2 is available for bulk operations.
- The CPU core does not use SIMD internally (single-instruction-at-a-time emulation); SIMD is applied at the rendering and audio output stages.
- Generic specialization is preferred over code duplication for CPU variants.
