# TR-Library-First: Library-First Architecture Technical Requirement

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Quality Area   | Architecture / Reuse           |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-04-13                     |

---

## TR-LIB-001: Emulator as Library, UI Shells as Thin Consumers

**ID:** TR-LIB-001
**Title:** Emulator Core as a Reusable Library with UI Shells as Thin Consumers
**Priority:** P0 -- Critical
**Category:** Architecture

### Description

The ViceSharp emulator core shall be packaged as a standalone .NET library (NuGet package) with no dependencies on any UI framework, windowing system, or platform-specific API. UI applications (desktop, web, headless test harness, embedded) consume the library and provide their own rendering, audio output, and input handling through well-defined abstractions.

### Rationale

A library-first design enables: (1) multiple UI frontends (Avalonia, MAUI, Blazor, headless), (2) embedding in other applications (game development tools, music production, testing harnesses), (3) clean separation of concerns, (4) independent versioning of core vs. UI, and (5) automated testing without any UI dependency.

### Technical Specification

1. **Core Library Assembly:** `ViceSharp.Core` contains the complete emulation engine with zero UI dependencies.
2. **Abstractions Assembly:** `ViceSharp.Abstractions` defines all interfaces and value types shared between core and consumers. The core depends on Abstractions; consumers depend on Abstractions.
3. **No UI Framework References:** The core library shall not reference `System.Windows`, `Avalonia`, `Microsoft.Maui`, `System.Drawing`, or any UI framework assembly.
4. **No Threading Assumptions:** The core library does not create threads. The host application controls the emulation thread and timing.
5. **Frame-Based API:**
   - `IEmulator.RunFrame()` advances the emulation by one video frame and returns the frame data (video buffer, audio buffer, state events).
   - `IEmulator.RunCycles(int count)` advances by a specific number of cycles (for debugging).
   - `IEmulator.Step()` advances by one CPU instruction.
6. **Output Buffers:**
   - Video: The core writes to a pre-allocated pixel buffer (`Span<byte>` or `Memory<byte>`) provided by the host.
   - Audio: The core writes to a pre-allocated sample buffer provided by the host.
   - The core does not perform any rendering, blitting, or audio playback.
7. **Input Injection:**
   - The host injects input events via `IKeyboardMatrix`, `IJoystickPort`, `IMousePort`.
   - Events are queued and consumed at the correct emulation time.

### Acceptance Criteria

1. `ViceSharp.Core` compiles with zero references to any UI framework (verified by dependency analysis).
2. `ViceSharp.Core` can be consumed from a headless console application that runs the emulation and writes frame checksums to stdout.
3. `ViceSharp.Core` can be consumed from a unit test project that validates emulation behavior without any display or audio.
4. The `IEmulator` API is sufficient to build a complete UI (demonstrated by at least one working UI shell).
5. The core library NuGet package size is under 5MB (excluding ROMs and test data).
6. The core library has zero transitive dependencies beyond the .NET BCL and `ViceSharp.Abstractions`.

### Verification Method

- Dependency analysis tool (e.g., `dotnet list package --include-transitive`) verifying no UI framework dependencies.
- Headless test harness in the CI pipeline that exercises the full emulation API without a UI.
- NuGet package size check in the build pipeline.

### Related TRs

- TR-PLAT-001 (Platform-agnostic core enables cross-platform UI shells)
- TR-AOT-001 (NativeAOT publishing of the library)
- TR-MVVM-001 (UI shells use MVVM pattern on top of the library)

### Design Decisions

- The core library does not implement a "run loop" -- the host application calls `RunFrame()` at the appropriate cadence (vsync, timer, or free-running).
- The core library exposes synchronous APIs only; the host is responsible for threading and async patterns.
- ROM images are not bundled with the library; the host provides ROM data via `IEmulator.LoadRom()`.
