# TR-to-Design-Decision Traceability Map

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Project        | ViceSharp                      |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-04-13                     |

## Purpose

This document maps each Technical Requirement to the architectural and design decisions that implement it. This provides traceability from quality constraints through to concrete implementation choices.

---

## TR-CYCLE-001: Sub-Cycle Bus-Phase Accuracy

| Decision ID | Decision                                                    | Rationale                                                                |
|-------------|-------------------------------------------------------------|--------------------------------------------------------------------------|
| DD-CLK-001  | Half-cycle tick model (PHI1/PHI2)                           | Bus-phase accuracy requires ticking each device twice per clock cycle.   |
| DD-CLK-002  | Fixed device tick order: VIC-II, CIA1, CIA2, SID, CPU       | Matches the analog timing priority on real hardware; ensures determinism.|
| DD-CLK-003  | BA/AEC signal modeling in `IClockedDevice`                  | VIC-II DMA stealing is observable at the bus-phase level.                |
| DD-CLK-004  | CPU pipeline state exposed per-tick                         | Allows other devices to observe mid-instruction CPU state.               |

---

## TR-AOT-001: NativeAOT Compatibility

| Decision ID | Decision                                                    | Rationale                                                                |
|-------------|-------------------------------------------------------------|--------------------------------------------------------------------------|
| DD-AOT-001  | Source-generated opcode dispatch (switch expression)         | Eliminates reflection-based delegate array; AoT-safe.                    |
| DD-AOT-002  | `[LibraryImport]` for all FFmpeg P/Invoke                   | Compile-time marshaling; no runtime codegen.                             |
| DD-AOT-003  | `[UnmanagedCallersOnly]` for FFmpeg callbacks               | AoT-compatible reverse P/Invoke.                                         |
| DD-AOT-004  | Explicit DI registration (no assembly scanning)             | Assembly scanning uses reflection; explicit registration is AoT-safe.    |
| DD-AOT-005  | Source-generated configuration binding                      | Replaces `Microsoft.Extensions.Configuration.Binder` reflection.         |
| DD-AOT-006  | No `System.Linq.Expressions` in any assembly                | Expression tree compilation uses `Reflection.Emit`.                      |

---

## TR-ALLOC-001: Zero Managed Allocations on Hot Path

| Decision ID | Decision                                                    | Rationale                                                                |
|-------------|-------------------------------------------------------------|--------------------------------------------------------------------------|
| DD-MEM-001  | `readonly record struct` for CPU/VIC/SID/CIA state          | Value types live on the stack or in pre-allocated arrays; no GC pressure.|
| DD-MEM-002  | `PayloadArena<T>` bump allocator for per-frame buffers      | O(1) allocation, O(1) reset; zero GC involvement.                        |
| DD-MEM-003  | Pre-allocated frame buffer (native memory or pinned array)  | Reused every frame; no allocation per frame.                             |
| DD-MEM-004  | Pre-allocated audio ring buffer                             | Fixed-size ring buffer; producer-consumer without allocation.            |
| DD-MEM-005  | Static lambdas / direct method refs for callbacks           | Avoids closure allocation (display class).                               |
| DD-MEM-006  | `Span<byte>` for all buffer operations                      | Stack-only; no heap allocation for slicing.                              |
| DD-MEM-007  | No LINQ on hot path; `for`/`foreach` over arrays            | LINQ allocates enumerator objects.                                       |
| DD-MEM-008  | Structured logging with pre-allocated templates              | Avoids string interpolation allocation.                                  |

---

## TR-SIMD-001: SIMD-Accelerated Rendering and Audio

| Decision ID | Decision                                                    | Rationale                                                                |
|-------------|-------------------------------------------------------------|--------------------------------------------------------------------------|
| DD-SIMD-001 | `Vector128<byte>` baseline for pixel writes                 | Available on all targets (SSE2 on x64, NEON on ARM64).                   |
| DD-SIMD-002 | `Vector256<byte>` opportunistic for AVX2 bulk operations    | 2x throughput on AVX2-capable CPUs.                                      |
| DD-SIMD-003 | `CpuCore<TBus>` generic specialization                      | JIT/AoT monomorphizes for each bus type; no virtual dispatch.            |
| DD-SIMD-004 | Scalar fallback for all SIMD paths                          | Ensures correctness on platforms without SIMD; determinism verification. |
| DD-SIMD-005 | SIMD applied at render/audio output, not CPU decode         | CPU decode is single-instruction; SIMD is for bulk data.                 |

---

## TR-DET-001: Bit-Exact Determinism

| Decision ID | Decision                                                    | Rationale                                                                |
|-------------|-------------------------------------------------------------|--------------------------------------------------------------------------|
| DD-DET-001  | Integer-only arithmetic in emulation core                   | Floating-point rounding differs across platforms/modes.                   |
| DD-DET-002  | Fixed-point Q16.16 for SID filter computation               | Deterministic across platforms; converts to float only at audio output.  |
| DD-DET-003  | Explicit RAM initialization pattern (alternating $00/$FF)   | Matches known C64 power-on state; reproducible cold starts.              |
| DD-DET-004  | Single-threaded emulation core                              | No data races, no scheduling non-determinism.                            |
| DD-DET-005  | Frame buffer stores palette indices, not RGB                | Decouples deterministic emulation from platform-dependent color output.  |
| DD-DET-006  | `[Deterministic]` attribute + Roslyn analyzer               | Static enforcement of no-float, no-random, no-DateTime in annotated code.|

---

## TR-STATE-001: ACID State Transactions

| Decision ID | Decision                                                    | Rationale                                                                |
|-------------|-------------------------------------------------------------|--------------------------------------------------------------------------|
| DD-STA-001  | `StateMutation` struct ring buffer (pre-allocated)          | Zero-allocation mutation recording; fixed memory footprint.              |
| DD-STA-002  | XOR-based delta compression for state window                | Compact diffs; unchanged bytes compress to zero.                         |
| DD-STA-003  | Keyframe + delta reconstruction                             | Fast rewind without storing full state per frame.                        |
| DD-STA-004  | Copy-on-write published snapshot for UI access              | UI thread reads a stable snapshot; no torn reads.                        |
| DD-STA-005  | Configurable transaction granularity (instruction/cycle)    | Balances accuracy vs. overhead for different use cases.                  |

---

## TR-PUBSUB-001: High-Performance Pub/Sub

| Decision ID | Decision                                                    | Rationale                                                                |
|-------------|-------------------------------------------------------------|--------------------------------------------------------------------------|
| DD-PUB-001  | `readonly record struct` message payloads (max 64 bytes)    | Value types; no boxing; inline in ring buffer slots.                     |
| DD-PUB-002  | Lock-free ring buffer for publish path                      | Sub-50ns publish without contention.                                     |
| DD-PUB-003  | Pre-allocated subscriber array (not List/Dictionary)        | O(1) iteration; no allocation on subscribe/deliver.                      |
| DD-PUB-004  | Synchronous (inline) delivery within emulation loop         | No thread marshaling overhead; deterministic order.                      |
| DD-PUB-005  | Priority-sorted subscribers at registration time            | Fixed delivery order during emulation; deterministic behavior.           |
| DD-PUB-006  | `Unsafe.As<TFrom,TTo>()` for zero-copy payload access      | Reinterprets bytes without copy; union-like access.                      |

---

## TR-PLAT-001: Cross-Platform Support

| Decision ID | Decision                                                    | Rationale                                                                |
|-------------|-------------------------------------------------------------|--------------------------------------------------------------------------|
| DD-PLT-001  | Core library targets `net10.0` (no platform suffix)         | Platform-agnostic; runs anywhere .NET 10 runs.                           |
| DD-PLT-002  | Platform-specific assemblies (`.Platform.Windows`, etc.)    | Isolates OS-specific code (audio, video, input).                         |
| DD-PLT-003  | Runtime platform detection via factory pattern              | No compile-time conditionals; single build artifact.                     |
| DD-PLT-004  | `NativeLibrary.SetDllImportResolver()` for FFmpeg loading   | Platform-specific library paths resolved at runtime.                     |
| DD-PLT-005  | `Path.Combine()` for all file paths; forward slashes only   | Cross-platform path handling.                                            |

---

## TR-LIB-001: Library-First Architecture

| Decision ID | Decision                                                    | Rationale                                                                |
|-------------|-------------------------------------------------------------|--------------------------------------------------------------------------|
| DD-LIB-001  | `ViceSharp.Core` has zero UI framework references           | Embeddable in any host application.                                      |
| DD-LIB-002  | `IEmulator.RunFrame()` frame-based API                      | Host controls timing; no internal run loop.                              |
| DD-LIB-003  | Host-provided output buffers (`Span<byte>`)                 | Core writes data; host decides how to display/play it.                   |
| DD-LIB-004  | Input injection via interface methods                       | Host maps platform input to emulator input.                              |
| DD-LIB-005  | ROM images provided by host, not bundled                    | Legal compliance; host manages ROM sourcing.                             |

---

## TR-MVVM-001: Strict MVVM Separation

| Decision ID | Decision                                                    | Rationale                                                                |
|-------------|-------------------------------------------------------------|--------------------------------------------------------------------------|
| DD-MVM-001  | `ViceSharp.ViewModels` depends only on Abstractions         | ViewModels testable without UI framework or Core implementation.         |
| DD-MVM-002  | Composition root is the only assembly referencing both Core and ViewModels | Clean dependency graph; single wiring point.           |
| DD-MVM-003  | Architecture test (NetArchTest/ArchUnitNET) in CI           | Automated enforcement of dependency rules.                               |
| DD-MVM-004  | View code-behind limited to 20 lines                        | Forces logic into ViewModels.                                            |
| DD-MVM-005  | CommunityToolkit.Mvvm for INPC/ICommand infrastructure      | Lightweight, source-generated, AoT-compatible.                           |

---

## TR-MEDIA-001: FFmpeg P/Invoke Integration

| Decision ID | Decision                                                    | Rationale                                                                |
|-------------|-------------------------------------------------------------|--------------------------------------------------------------------------|
| DD-FFM-001  | `[LibraryImport]` source-generated marshaling               | AoT-compatible; compile-time verified.                                   |
| DD-FFM-002  | `ViceSharp.Media.FFmpeg` separate assembly                  | Core has no FFmpeg dependency; graceful degradation.                      |
| DD-FFM-003  | Native-memory staging buffer for frame handoff              | Decouples emulation buffer from encoder; no GC pinning during encode.    |
| DD-FFM-004  | Dedicated encoding thread (producer-consumer)               | Encoding does not block the emulation hot path.                          |
| DD-FFM-005  | `IDisposable` wrappers for all FFmpeg handles               | Deterministic cleanup; no native memory leaks.                           |

---

## TR-BUILD-001: Nuke Build with Dual CI/CD

| Decision ID | Decision                                                    | Rationale                                                                |
|-------------|-------------------------------------------------------------|--------------------------------------------------------------------------|
| DD-BLD-001  | Nuke build system (C# build logic)                          | Strongly typed; debuggable in IDE; matches project language.             |
| DD-BLD-002  | Azure DevOps as primary CI/CD                               | Hosts primary repo and artifact feed.                                    |
| DD-BLD-003  | GitHub Actions as mirror CI                                 | Community visibility; PR validation for GitHub contributors.             |
| DD-BLD-004  | Multi-platform build matrix (Win/Linux/macOS)               | Validates TR-PLAT-001 on every build.                                    |
| DD-BLD-005  | Nerdbank.GitVersioning for version management               | Deterministic version strings from Git history.                          |
| DD-BLD-006  | NativeAOT publish as CI target                              | Catches AoT regressions on every commit.                                 |
