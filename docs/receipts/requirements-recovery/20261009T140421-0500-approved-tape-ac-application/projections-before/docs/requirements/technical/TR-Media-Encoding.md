# TR-Media-Encoding: Media Encoding Technical Requirement

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Quality Area   | Integration / Media            |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-07-08                     |

---

## TR-MEDIA-001: External FFmpeg Process, Multiple Format Support

**ID:** TR-MEDIA-001
**Title:** External FFmpeg Process Integration with Multiple Format Support
**Priority:** P1 -- Important
**Category:** Integration

### Description

Muxed video+audio encoding shall use an external `ffmpeg` executable driven as a child process, mirroring VICE's `ffmpegexedrv`. ViceSharp links no FFmpeg libraries and performs no P/Invoke into libav*; the emulator streams raw frames and PCM to the ffmpeg process, and ffmpeg performs all codec and container work. Audio-only (WAV) and image/frame-sequence capture are implemented with managed encoders and do not require ffmpeg.

### Rationale

Shelling out to the ffmpeg executable keeps ViceSharp free of native library bindings, version-specific libav API coupling, and platform-specific binary distribution while still supporting the industry-standard codecs and containers (H.264, MP4, MKV, AVI). It exactly mirrors the mechanism of VICE's `ffmpegexedrv`, keeping capture behavior comparable between ViceSharp and VICE.

### Technical Specification

1. **Process Integration (`src/ViceSharp.Core/Media`):**
   - `FfmpegLocator` resolves the ffmpeg executable: the `VICESHARP_FFMPEG` environment variable (full path override) first, then each directory on `PATH`.
   - `FfmpegVideoRecorder` records video and audio into a single muxed container by launching ffmpeg as a child process and streaming raw BGRA frames plus int16 PCM over two loopback TCP sockets (video + optional audio).
   - `FfmpegVideoEncoding` defines the format table, mirroring VICE `ffmpegexedrv` choices: mp4 (libx264 + aac, yuv420p), mkv (matroska, libx264 + aac, yuv420p), avi (mpeg4 + libmp3lame).
   - Stopping capture closes both sockets so ffmpeg finalizes the file (`-shortest`) and the process exit is awaited.

2. **Hot-Path Safety:**
   - Both feeds arrive on the single emulation worker thread; socket writes are moved off the worker by `BackgroundByteWriter` (the worker only copies and enqueues).
   - Video queue depth is bounded: if ffmpeg stops draining, overflow frames are dropped and counted so the emulator stays responsive; emulator audio preserves order.

3. **Graceful Degradation:**
   - When ffmpeg cannot be located, the capture surface advertises no muxed video formats and requests to start mp4/mkv/avi capture are rejected gracefully (per FR-MED-005).
   - WAV audio recording (`WavAudioRecorder`, managed RIFF/WAVE 16-bit PCM writer) and BMP frame-sequence capture (`FrameSequenceCapture`) remain available without ffmpeg.

4. **Supported Formats:**
   - Muxed video (requires external ffmpeg): MP4 (H.264/libx264 + AAC), MKV (libx264 + AAC), AVI (mpeg4 + MP3/libmp3lame).
   - Audio (managed, no ffmpeg): WAV (uncompressed 16-bit PCM).
   - Images/frames (managed, no ffmpeg): screenshots and per-frame BMP sequences.

5. **Capture Routing:**
   - One gRPC `CaptureService` routes to recorders in `ViceSharp.Core.Media`; frames tee from `EmulatorRuntimeSession.CommitFrame` and audio from `CaptureAudioTap` in the SID path.
   - The audio tap installs only when a real audio device exists, so headless/test hosts stay timing-clean and silent.

### Acceptance Criteria

1. With ffmpeg present (PATH or `VICESHARP_FFMPEG`), video recording to H.264/MP4 produces valid output (playable by VLC and browser HTML5 video).
2. Audio recording to WAV produces valid output (RIFF/WAVE header parses; playable by standard audio players) without requiring ffmpeg.
3. Synchronized A/V recording produces an MP4 with correct A/V sync (per FR-MED-004).
4. When ffmpeg is absent, muxed video formats are not advertised and capture start requests fail gracefully with a structured error; WAV and frame-sequence capture still work.
5. Video recording never blocks the emulation worker thread: a stalled ffmpeg consumer results in counted dropped frames, not emulator stalls.
6. The ffmpeg child process is fully reaped on stop (no orphaned processes, output file finalized).

### Verification Method

- `MediaServiceHostTests`, `CaptureServiceHostTests`, and `RuntimeCaptureTests` exercise the capture service surface, format advertisement, and graceful degradation.
- `WavAudioRecorderTests` validates the managed WAV writer output.
- `FrameSequenceCaptureTests` validates BMP frame-sequence capture.
- Media file validation using ffprobe/manual playback to verify codec, container, and sync on recorded artifacts.

### Related FRs

- FR-MED-001 through FR-MED-005 (all media capture features)

### Related TRs

- TR-ALLOC-001 (Frame handoff copies into pre-allocated buffers; socket writes stay off the emulation worker)
- TR-PLAT-001 (ffmpeg executable discovery works per-platform via PATH conventions)

### Design Decisions

- The external-process design supersedes the earlier P/Invoke libav binding spec (original DD-MEDIA-001); no `ViceSharp.Media.FFmpeg` assembly exists and none is planned.
- The media encoding pipeline runs off the emulation hot path: the worker copies and enqueues; background writers own the socket I/O.
- ffmpeg is an optional runtime dependency, never a build dependency; capture features degrade gracefully when it is missing.
