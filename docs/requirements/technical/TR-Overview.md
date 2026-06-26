# Technical Requirements Overview

## Document Information

| Field          | Value                                      |
|----------------|--------------------------------------------|
| Project        | ViceSharp                                  |
| Version        | 0.1.0-draft                                |
| Last Updated   | 2026-05-13                                 |
| Status         | Draft                                      |

## Purpose

This document indexes ViceSharp Technical Requirements (TRs). TRs define architecture constraints and non-functional qualities derived from Vice-Sharp architecture, not from classic VICE implementation choices.

## TR Document Index

| Document | Quality Attribute | TR ID(s) |
|----------|-------------------|----------|
| TR-Build-System.md | CI/CD / Build | TR-BUILD-001 |
| TR-Cycle-Accuracy.md | Accuracy / Fidelity | TR-CYCLE-001 |
| TR-Determinism.md | Correctness / Replay | TR-DET-001 |
| TR-GRPC-Boundary.md | Architecture / Boundary | TR-GRPC-BOUNDARY-001 |
| TR-Host-Status.md | Runtime Telemetry | TR-HOST-STATUS-001 |
| TR-Input-VKM.md | Input Translation | TR-INPUT-VKM-001 |
| TR-Library-First.md | Architecture / Reuse | TR-LIB-001 |
| TR-MVVM.md | Architecture / UI | TR-MVVM-001 |
| TR-Media-Encoding.md | Integration / Media | TR-MEDIA-001 |
| TR-Platform-Support.md | Portability | TR-PLAT-001 |
| TR-PubSub-Performance.md | Performance / Messaging | TR-PUBSUB-001 |
| TR-SIMD-Intrinsics.md | Performance / Throughput | TR-SIMD-001 |
| TR-State-Management.md | Reliability / Consistency | TR-STATE-001 |
| TR-System-Core.md | Architecture / Machine Definition | TR-SYSTEM-CORE-001 |
| TR-UI-Shell.md | UI Architecture | TR-UI-SHELL-001 |
| TR-Zero-Allocation.md | Performance / GC | TR-ALLOC-001 |

## Quality Attribute Summary

| TR ID | Title | Quality Attribute |
|-------|-------|-------------------|
| TR-ALLOC-001 | Zero Managed Allocations Per Emulation Cycle on Hot Path | Performance / GC |
| TR-BUILD-001 | Nuke Build System with Dual CI/CD Pipelines | CI/CD / Build |
| TR-CYCLE-001 | Sub-Cycle Bus-Phase Accuracy Matching VICE x64sc Behavior | Accuracy / Fidelity |
| TR-DET-001 | Bit-Exact Reproducibility Given Same Initial State and Inputs | Correctness / Replay |
| TR-GRPC-BOUNDARY-001 | Versioned gRPC Boundary Between Emulator Host and UI Clients | Architecture / Boundary |
| TR-HOST-STATUS-001 | Measured Emulator Runtime Telemetry | Runtime Telemetry |
| TR-INPUT-VKM-001 | VICE VKM Parser and Selected Map Resolver | Input Translation |
| TR-LIB-001 | Emulator Core as a Reusable Library with UI Shells as Thin Consumers | Architecture / Reuse |
| TR-MEDIA-001 | FFmpeg Integration via P/Invoke with Multiple Format Support | Integration / Media |
| TR-MVVM-001 | Strict MVVM Separation -- ViewModels Reference Abstractions Only, Views Contain Zero Logic | Architecture / UI |
| TR-PLAT-001 | Cross-Platform Support for Windows, Linux, macOS on x64 and ARM64 | Portability |
| TR-PUBSUB-001 | <50ns Publish, <100ns Deliver, 0 Allocations Per Frame | Performance / Messaging |
| TR-SIMD-001 | SIMD-Accelerated Rendering and Audio with Generic Specialization for CPU Core | Performance / Throughput |
| TR-STATE-001 | Mutation Queue with ACID State Transactions and Configurable State Window | Reliability / Consistency |
| TR-SYSTEM-CORE-001 | Definable System Core for Machine-Specific Bus Behavior | Architecture / Machine Definition |
| TR-UI-SHELL-001 | Avalonia Emulator Control Shell | UI Architecture |

## Architectural Constraints

1. Target runtime: .NET 10 with JIT desktop publication profiles.
2. Emulator core remains library-first and UI-independent.
3. UI control, media, input, state, capture, diagnostics, and monitor operations cross the host boundary through gRPC-backed abstractions.
4. The local Avalonia renderer may use only a host-owned direct frame surface for in-process presentation; ViewModels must not access runtime internals.
5. Hot-path emulation remains deterministic, low-allocation, and testable against reference traces.
6. Machine-specific bus behavior, programmable logic, and chip interconnect policy belong in a definable system core selected by the machine profile; `ArchitectureBuilder` remains the glue that instantiates chips and connects them to that core.
