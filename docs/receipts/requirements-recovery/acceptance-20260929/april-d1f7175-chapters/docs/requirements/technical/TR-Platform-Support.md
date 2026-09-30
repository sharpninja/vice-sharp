# TR-Platform-Support: Platform Support Technical Requirement

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Quality Area   | Portability                    |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-04-13                     |

---

## TR-PLAT-001: Windows/Linux/macOS, x64/ARM64, .NET 10

**ID:** TR-PLAT-001
**Title:** Cross-Platform Support for Windows, Linux, macOS on x64 and ARM64
**Priority:** P0 -- Critical
**Category:** Portability

### Description

ViceSharp shall run on Windows, Linux, and macOS on both x64 and ARM64 architectures. The emulation core library shall be platform-agnostic. Platform-specific code (audio output, input handling, window management) shall be isolated behind abstractions with per-platform implementations.

### Rationale

The Commodore 64 community spans all major desktop platforms. A library-first design (TR-LIB-001) naturally supports multiple platforms, but platform-specific I/O (audio, video display, input) requires explicit abstraction.

### Technical Specification

1. **Target Framework:** .NET 10 (net10.0). No platform-specific TFMs in the core library.
2. **Runtime Identifiers:** The following RIDs are first-class targets:
   - `win-x64`, `win-arm64`
   - `linux-x64`, `linux-arm64`
   - `osx-x64`, `osx-arm64`
3. **Platform Abstraction Layer:**
   - `IAudioOutput`: Platform-specific audio output (WASAPI on Windows, PulseAudio/ALSA on Linux, CoreAudio on macOS).
   - `IVideoOutput`: Platform-specific window/surface management.
   - `IInputSource`: Platform-specific input device enumeration and event handling.
4. **Native Library Loading:**
   - FFmpeg shared libraries are loaded via `NativeLibrary.TryLoad()` with platform-specific paths.
   - SIMD capability detection uses `System.Runtime.Intrinsics` which is platform-aware.
5. **File System:**
   - All file paths use `Path.Combine()` and forward slashes internally.
   - No hardcoded path separators or drive letters.
6. **Endianness:**
   - The emulation core is little-endian (matching x86/ARM in LE mode); all platforms use little-endian.

### Acceptance Criteria

1. The emulation core library (`ViceSharp.Core`) compiles and runs on all 6 target RIDs without conditional compilation (`#if`).
2. The CI/CD pipeline builds and tests on Windows x64, Ubuntu x64, and macOS ARM64.
3. NativeAOT publish succeeds on all 6 target RIDs (per TR-AOT-001).
4. Audio output plays correctly on all three OS platforms (verified by manual testing and automated A/V sync tests).
5. Input devices (keyboard, gamepad) are recognized on all platforms.
6. The Lorenz test suite passes on all platforms (same pass rate).
7. No `PlatformNotSupportedException` is thrown during normal operation on any supported platform.

### Verification Method

- Multi-platform CI matrix (GitHub Actions for Windows/Linux/macOS).
- Cross-platform integration tests running a subset of the Lorenz test suite.
- NativeAOT publish smoke test on each target RID.

### Related TRs

- TR-AOT-001 (NativeAOT publish targets all platforms)
- TR-SIMD-001 (SIMD support differs between x64 SSE/AVX and ARM64 NEON)
- TR-LIB-001 (Core library is platform-agnostic)
- TR-MEDIA-001 (FFmpeg native libraries are platform-specific)

### Design Decisions

- The core library targets `net10.0` without any `-windows`, `-linux`, or `-macos` suffix.
- Platform-specific audio/video/input implementations are in separate assemblies (e.g., `ViceSharp.Platform.Windows`, `ViceSharp.Platform.Linux`, `ViceSharp.Platform.MacOS`).
- The platform assembly is selected at application startup via a factory, not compile-time conditionals.
- ARM64 Windows (Surface Pro X, Snapdragon laptops) is a supported target from the start.
