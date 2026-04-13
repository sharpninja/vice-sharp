# TR-Media-Encoding: Media Encoding Technical Requirement

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Quality Area   | Integration / AoT Compat       |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-04-13                     |

---

## TR-MEDIA-001: FFmpeg P/Invoke, AoT Compatible, Multiple Format Support

**ID:** TR-MEDIA-001
**Title:** FFmpeg Integration via P/Invoke with NativeAOT Compatibility
**Priority:** P1 -- Important
**Category:** Integration

### Description

Video and audio encoding shall use FFmpeg libraries (libavcodec, libavformat, libavutil, libswscale, libswresample) accessed via P/Invoke. The FFmpeg bindings shall be NativeAOT-compatible (no reflection, no dynamic assembly loading). Multiple output formats shall be supported through FFmpeg's codec infrastructure.

### Rationale

FFmpeg is the industry standard for media encoding/decoding, supports all required formats (H.264, FLAC, WAV, MP4, AVI), and is available on all target platforms. P/Invoke is the most AoT-friendly interop mechanism in .NET.

### Technical Specification

1. **P/Invoke Bindings:**
   - All FFmpeg function calls use `[DllImport]` or `[LibraryImport]` (source-generated, preferred for AoT) attributes.
   - Bindings are auto-generated from FFmpeg C headers using a code generator tool.
   - Pointer-heavy FFmpeg APIs are wrapped in safe C# abstractions that manage lifetime.

2. **NativeAOT Compatibility:**
   - All P/Invoke declarations use `[LibraryImport]` (compile-time marshaling) instead of `[DllImport]` where possible.
   - No `Marshal.GetDelegateForFunctionPointer()` usage (incompatible with AoT).
   - Callback functions passed to FFmpeg use `[UnmanagedCallersOnly]` static methods.

3. **Library Loading:**
   - FFmpeg shared libraries are loaded via `NativeLibrary.SetDllImportResolver()` to support platform-specific paths.
   - Missing FFmpeg libraries result in graceful degradation (media capture features report as unavailable, per FR-MED-005).
   - Supported FFmpeg versions: 6.x and 7.x (API compatibility layer handles minor version differences).

4. **Supported Formats:**
   - Video codecs: H.264 (libx264), H.265/HEVC (libx265), VP9 (libvpx-vp9), MJPEG.
   - Audio codecs: PCM (WAV), FLAC, MP3 (libmp3lame), AAC.
   - Container formats: MP4, AVI, MKV, WAV, FLAC.
   - Image formats (single-frame): PNG, BMP, JPEG (via libavcodec).

5. **Memory Management:**
   - FFmpeg's internal allocations are managed by FFmpeg's own allocator.
   - Frame data passed from the emulator to FFmpeg uses pinned buffers or native memory to avoid GC relocation.
   - All FFmpeg resources (AVFormatContext, AVCodecContext, AVFrame, AVPacket) are wrapped in `IDisposable` wrappers with deterministic cleanup.

### Acceptance Criteria

1. `[LibraryImport]` source-generated P/Invoke compiles and runs under NativeAOT on all target platforms.
2. Video recording to H.264/MP4 produces valid output (playable by VLC and browser HTML5 video).
3. Audio recording to WAV and FLAC produces valid output (playable by standard audio players).
4. Synchronized A/V recording produces an MP4 with correct A/V sync (per FR-MED-004).
5. When FFmpeg libraries are not present, `IMediaCapture.GetSupportedFormats()` returns an empty set and capture methods throw `MediaNotAvailableException`.
6. No FFmpeg-related memory leaks: a 10-minute recording session followed by disposal shows no growth in native memory.
7. The FFmpeg bindings pass trim analysis with zero warnings.

### Verification Method

- NativeAOT publish and execution test with FFmpeg recording.
- Media file validation tests using FFprobe to verify codec, container, and sync.
- Memory leak test using native memory profiling during extended recording.
- Graceful degradation test with FFmpeg libraries removed from the search path.

### Related FRs

- FR-MED-001 through FR-MED-005 (all media capture features)

### Related TRs

- TR-AOT-001 (P/Invoke bindings must be AoT-compatible)
- TR-ALLOC-001 (Frame data transfer uses pinned/native buffers, not managed arrays)
- TR-PLAT-001 (FFmpeg libraries are platform-specific binaries)

### Design Decisions

- `[LibraryImport]` (source-generated) is strongly preferred over `[DllImport]` (runtime-generated) for AoT compatibility.
- FFmpeg bindings are isolated in a separate assembly (`ViceSharp.Media.FFmpeg`) so the core library has no FFmpeg dependency.
- The media encoding pipeline runs on a dedicated thread (producer-consumer pattern with the emulation thread), not on the emulation hot path.
- Frame data is copied to a native-memory staging buffer before handoff to FFmpeg, decoupling the emulation frame buffer from the encoder.
