# TR-Platform-Support: Platform Support Technical Requirement

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Quality Area   | Portability                    |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-10-15                     |

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
   - `IAudioBackend`: Platform-specific audio output (WinMM on Windows and SDL2 on Linux). A macOS live-output backend remains pending.
   - `IVideoOutput`: Platform-specific window/surface management.
   - `IInputSource`: Platform-specific input device enumeration and event handling.
4. **Native Dependency Discovery:**
   - Linux desktop audio uses the SDL2 ABI (`libSDL2-2.0.so.0`) behind `IAudioBackend`, with 44.1 kHz mono PCM16 and bounded device-queue back-pressure. `AudioBackendFactory` preserves the explicit `VICESHARP_AUDIO` opt-in for headless hosts and returns no backend when initialization fails. Device failure releases pending writes and falls back to non-audio pacing. This Linux implementation does not establish macOS live-audio support.
   - The external `ffmpeg` executable is discovered per-platform via `PATH` or `VICESHARP_FFMPEG` (no native library loading; see TR-MEDIA-001).
   - SIMD capability detection uses `System.Runtime.Intrinsics` which is platform-aware.
5. **File System:**
   - All file paths use `Path.Combine()` and forward slashes internally.
   - No hardcoded path separators or drive letters.
6. **Endianness:**
   - The emulation core is little-endian (matching x86/ARM in LE mode); all platforms use little-endian.

### Acceptance Criteria

1. The emulation core library (`ViceSharp.Core`) compiles and runs on all 6 target RIDs without conditional compilation (`#if`).
2. Build and test validation uses the operator-run Nuke gates defined by TR-BUILD-001, with retained evidence for each tested platform. GitHub `origin` is the source of truth; Azure DevOps is retired. Automated multi-OS CI remains future work.
3. Release publish succeeds on all supported target RIDs.
4. Audio output plays correctly on all three OS platforms (verified by manual testing and automated A/V sync tests).
5. Input devices (keyboard, gamepad) are recognized on all platforms.
6. The Lorenz test suite passes on all platforms (same pass rate).
7. No `PlatformNotSupportedException` is thrown during normal operation on any supported platform.
8. Linux audio tests use an injected device to verify PCM order, gain/mute, queue capacity, lifecycle interruption and failure without playing sound. A separate local smoke check verifies the actual installed library/device path. User-local installation never installs native system packages.

### Verification Method

- Operator-run Nuke build/test gates from the GitHub-backed repository, with exact commands, platform details, logs and pass/fail/skip counts. Historical Azure pipeline files are not active validation; no multi-platform CI coverage is claimed.
- Cross-platform integration tests running a subset of the Lorenz test suite.
- Release publish smoke test on each target RID.

### Related TRs

- TR-SIMD-001 (SIMD support differs between x64 SSE/AVX and ARM64 NEON)
- TR-LIB-001 (Core library is platform-agnostic)
- TR-MEDIA-001 (external ffmpeg executable discovery is per-platform)

### Design Decisions

- The core library targets `net10.0` without any `-windows`, `-linux`, or `-macos` suffix.
- Desktop audio implementations live in `ViceSharp.Host.InProcess` behind the shared `IAudioBackend` abstraction.
- `AudioBackendFactory` selects the Windows or Linux audio implementation at runtime when audio is enabled; unavailable Linux output falls back to silent emulation.
- ARM64 Windows (Surface Pro X, Snapdragon laptops) is a supported target from the start.
