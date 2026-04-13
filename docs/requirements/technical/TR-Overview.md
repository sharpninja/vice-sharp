# Technical Requirements Overview

## Document Information

| Field          | Value                                      |
|----------------|--------------------------------------------|
| Project        | ViceSharp                                  |
| Version        | 0.1.0-draft                                |
| Last Updated   | 2026-04-13                                 |
| Status         | Draft                                      |

## Purpose

This document serves as the master index for all ViceSharp Technical Requirements (TRs). Each TR defines a non-functional quality attribute, architectural constraint, or engineering standard that the system must meet. TRs are distinct from FRs in that they describe *how well* the system performs rather than *what* it does.

## TR Document Index

| Document                    | Quality Attribute         | TR Range                |
|-----------------------------|---------------------------|-------------------------|
| TR-Cycle-Accuracy.md        | Accuracy / Fidelity       | TR-CYCLE-001            |
| TR-AoT-Compilation.md       | Deployment / Startup      | TR-AOT-001              |
| TR-Zero-Allocation.md       | Performance / GC          | TR-ALLOC-001            |
| TR-SIMD-Intrinsics.md       | Performance / Throughput  | TR-SIMD-001             |
| TR-Determinism.md           | Correctness / Replay      | TR-DET-001              |
| TR-State-Management.md      | Reliability / Consistency | TR-STATE-001            |
| TR-PubSub-Performance.md    | Performance / Messaging   | TR-PUBSUB-001           |
| TR-Platform-Support.md      | Portability               | TR-PLAT-001             |
| TR-Library-First.md         | Architecture / Reuse      | TR-LIB-001              |
| TR-MVVM.md                  | Architecture / UI         | TR-MVVM-001             |
| TR-Media-Encoding.md        | Integration / AoT         | TR-MEDIA-001            |
| TR-Build-System.md          | CI/CD / Build             | TR-BUILD-001            |

## Quality Attribute Summary

| Quality Attribute          | Key Metric                                          | TR ID         |
|----------------------------|-----------------------------------------------------|---------------|
| Cycle Accuracy             | Sub-cycle bus-phase accuracy matching VICE x64sc     | TR-CYCLE-001  |
| AoT Compatibility          | All assemblies pass `dotnet publish -r` trim analysis | TR-AOT-001    |
| Memory Efficiency          | 0 managed allocations per emulation cycle (hot path) | TR-ALLOC-001  |
| Compute Throughput         | SIMD-accelerated rendering and audio mixing          | TR-SIMD-001   |
| Determinism                | Bit-exact output for same input + initial state      | TR-DET-001    |
| State Reliability          | ACID transactions for state mutation                 | TR-STATE-001  |
| Messaging Latency          | <50ns publish, <100ns deliver, 0 allocs/frame        | TR-PUBSUB-001 |
| Portability                | Win/Linux/macOS, x64/ARM64, .NET 10                  | TR-PLAT-001   |
| Modularity                 | Core emulator as library, UI shells as consumers     | TR-LIB-001    |
| UI Separation              | ViewModels reference Abstractions only               | TR-MVVM-001   |
| Media Integration          | FFmpeg via P/Invoke, AoT compatible                  | TR-MEDIA-001  |
| Build Automation           | Nuke build, dual CI/CD pipelines                     | TR-BUILD-001  |

## Architectural Constraints

The following constraints apply globally and are referenced by multiple TRs:

1. **Target Runtime:** .NET 10 with NativeAOT publication profile.
2. **No Reflection on Hot Path:** Generic specialization and source generators replace reflection-based dispatch.
3. **Struct-First Design:** Value types preferred over reference types in the emulation core to minimize GC pressure.
4. **Interface Segregation:** All cross-layer dependencies flow through interfaces defined in the `.Abstractions` assembly.
5. **Unsafe Code Budget:** `unsafe` code is permitted only in clearly bounded hot-path methods with documented invariants.
