# TR-to-Design-Decision Traceability Map

## Document Information

| Field | Value |
|-------|-------|
| Project | ViceSharp |
| Version | 0.1.0-draft |
| Last Updated | 2026-05-13 |

## Purpose

This map links Technical Requirements to Vice-Sharp architecture decisions and source documents.

---

## TR-ALLOC-001: Zero Managed Allocations Per Emulation Cycle on Hot Path

| Decision ID | Decision |
|-------------|----------|
| DD-PERF-001 | Hot-path state favors structs, spans, and pooled buffers. |

**Architecture Sources**

- `docs/Architecture.md`
- `docs/requirements/technical/TR-Zero-Allocation.md`

---

## TR-AOT-001: Full NativeAOT Compatibility with Zero Reflection on Hot Path

| Decision ID | Decision |
|-------------|----------|
| DD-AOT-001 | Use explicit registration/source generation over hot-path reflection. |

**Architecture Sources**

- `docs/Architecture.md`
- `docs/requirements/technical/TR-AoT-Compilation.md`

---

## TR-BUILD-001: Nuke Build System with Dual CI/CD Pipelines

| Decision ID | Decision |
|-------------|----------|
| DD-BLD-001 | Build/test validation uses the solution and repository conventions. |

**Architecture Sources**

- `docs/Architecture.md`
- `docs/requirements/technical/TR-Build-System.md`

---

## TR-CYCLE-001: Sub-Cycle Bus-Phase Accuracy Matching VICE x64sc Behavior

| Decision ID | Decision |
|-------------|----------|
| DD-CLK-001 | Half-cycle/bus-phase clocking remains the target fidelity model. |
| DD-REF-001 | VICE x64sc/reference traces are accepted as behavioral comparison targets. |

**Architecture Sources**

- `docs/Architecture.md`
- `docs/requirements/technical/TR-Cycle-Accuracy.md`

---

## TR-DET-001: Bit-Exact Reproducibility Given Same Initial State and Inputs

| Decision ID | Decision |
|-------------|----------|
| DD-DET-001 | Same initial state plus same input sequence must produce bit-exact state/output. |

**Architecture Sources**

- `docs/Architecture.md`
- `docs/requirements/technical/TR-Determinism.md`

---

## TR-SYSTEM-CORE-001: Definable System Core for Machine-Specific Bus Behavior

| Decision ID | Decision |
|-------------|----------|
| DD-SYSCORE-001 | Machine profiles select system-core definitions for board logic, bus routing, and programmable logic behavior; `ArchitectureBuilder` glues those definitions to concrete chip instances. |
| DD-SYSCORE-002 | x64sc variants are the first proving ground for definable-computer behavior because they vary board policy while sharing reusable chip families. |

**Architecture Sources**

- `docs/Architecture.md`
- `docs/requirements/technical/TR-System-Core.md`
- `docs/requirements/backfill/X64SC-Model-Matrix.md`

---

## TR-GRPC-BOUNDARY-001: Versioned gRPC Boundary Between Emulator Host and UI Clients

| Decision ID | Decision |
|-------------|----------|
| DD-HOST-001 | Host owns emulator sessions and all mutating control surfaces. |
| DD-RENDER-001 | Local Avalonia rendering is a host-owned direct frame-source exception only. |

**Architecture Sources**

- `docs/Architecture.md`
- `docs/requirements/technical/TR-GRPC-Boundary.md`

---

## TR-HOST-STATUS-001: Measured Emulator Runtime Telemetry

| Decision ID | Decision |
|-------------|----------|
| DD-HOST-002 | Runtime telemetry separates requested limiter target from measured effective speed. |

**Architecture Sources**

- `docs/Architecture.md`
- `docs/requirements/technical/TR-Host-Status.md`

---

## TR-INPUT-VKM-001: VICE VKM Parser and Selected Map Resolver

| Decision ID | Decision |
|-------------|----------|
| DD-INP-001 | Machine-specific keyboard translation resolves selected VICE VKM maps before matrix mutation. |

**Architecture Sources**

- `docs/Architecture.md`
- `docs/requirements/technical/TR-Input-VKM.md`

---

## TR-LIB-001: Emulator Core as a Reusable Library with UI Shells as Thin Consumers

| Decision ID | Decision |
|-------------|----------|
| DD-ARCH-001 | Emulator core remains a reusable library with thin consumers. |

**Architecture Sources**

- `docs/Architecture.md`
- `docs/requirements/technical/TR-Library-First.md`

---

## TR-MEDIA-001: FFmpeg Integration via P/Invoke with NativeAOT Compatibility

| Decision ID | Decision |
|-------------|----------|
| DD-MEDIA-001 | Capture/encoding stays behind AoT-compatible media abstractions. |

**Architecture Sources**

- `docs/Architecture.md`
- `docs/requirements/technical/TR-Media-Encoding.md`

---

## TR-MVVM-001: Strict MVVM Separation -- ViewModels Reference Abstractions Only, Views Contain Zero Logic

| Decision ID | Decision |
|-------------|----------|
| DD-UI-001 | Avalonia ViewModels depend on abstractions/client facades only. |

**Architecture Sources**

- `docs/Architecture.md`
- `docs/requirements/technical/TR-MVVM.md`

---

## TR-PLAT-001: Cross-Platform Support for Windows, Linux, macOS on x64 and ARM64

| Decision ID | Decision |
|-------------|----------|
| DD-PLAT-001 | Supported shell/core targets remain Windows/Linux/macOS on x64/ARM64. |

**Architecture Sources**

- `docs/Architecture.md`
- `docs/requirements/technical/TR-Platform-Support.md`

---

## TR-PUBSUB-001: <50ns Publish, <100ns Deliver, 0 Allocations Per Frame

| Decision ID | Decision |
|-------------|----------|
| DD-MSG-001 | High-frequency device events use bounded zero-allocation messaging. |

**Architecture Sources**

- `docs/Architecture.md`
- `docs/requirements/technical/TR-PubSub-Performance.md`

---

## TR-SIMD-001: SIMD-Accelerated Rendering and Audio with Generic Specialization for CPU Core

| Decision ID | Decision |
|-------------|----------|
| DD-PERF-002 | Rendering/audio processing may use SIMD where it preserves determinism. |

**Architecture Sources**

- `docs/Architecture.md`
- `docs/requirements/technical/TR-SIMD-Intrinsics.md`

---

## TR-STATE-001: Mutation Queue with ACID State Transactions and Configurable State Window

| Decision ID | Decision |
|-------------|----------|
| DD-STATE-001 | Mutations and snapshots are host/core-owned and replayable. |

**Architecture Sources**

- `docs/Architecture.md`
- `docs/requirements/technical/TR-State-Management.md`

---

## TR-UI-SHELL-001: Avalonia Emulator Control Shell

| Decision ID | Decision |
|-------------|----------|
| DD-UI-002 | Emulator controls live in a focused shell with status, tabs, monitor, and dockable panels. |

**Architecture Sources**

- `docs/Architecture.md`
- `docs/requirements/technical/TR-UI-Shell.md`
