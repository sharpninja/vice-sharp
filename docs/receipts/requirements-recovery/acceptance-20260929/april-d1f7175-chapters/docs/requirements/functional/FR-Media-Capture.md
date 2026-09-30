# FR-Media-Capture: Media Capture Functional Requirements

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Subsystem      | Media Capture                  |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-04-13                     |

---

## FR-MED-001: Screenshot Capture

**ID:** FR-MED-001
**Title:** Screenshot Capture (PNG/BMP)
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The emulator shall capture the current video frame as a still image in PNG or BMP format. Screenshots capture the full VIC-II output including borders (optionally cropped to the display area only). The capture is triggered programmatically or by user action.

### Acceptance Criteria

1. PNG format output with correct color representation (RGB, 8-bit per channel).
2. BMP format output as an uncompressed bitmap.
3. Full frame capture includes the border area (standard PAL: 403x284 visible pixels; standard NTSC: 411x234).
4. Cropped capture includes only the main display area (320x200 pixels, or 160x200 in multicolor mode upscaled to 320x200).
5. The screenshot captures the frame as it appears after all VIC-II rendering (sprites, priority, collision) is complete.
6. Screenshots are accessible via `IMediaCapture.CaptureScreenshot()` which returns the image data or writes to a file path.
7. Integer scaling options (1x, 2x, 3x, 4x) are available.
8. Palette selection (VICE default, Pepto, CCS64, Community Colors) is configurable.

### Traceability

- **Interfaces:** `IMediaCapture`
- **Test Suite:** `ScreenshotCaptureTests`, `PngFormatTests`, `PaletteTests`

---

## FR-MED-002: Video Recording

**ID:** FR-MED-002
**Title:** Video Recording (MP4 via FFmpeg)
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The emulator shall record video output to MP4 (H.264) format using FFmpeg libraries via P/Invoke. Recording captures consecutive frames at the native frame rate (PAL: 50fps, NTSC: approximately 59.94fps) and encodes them in real-time or offline.

### Acceptance Criteria

1. Video is encoded to H.264 in an MP4 container.
2. Frame rate matches the emulated system (50fps PAL, 59.94fps NTSC).
3. Resolution options include native (403x284 PAL) and integer-scaled variants.
4. Recording can be started, paused, resumed, and stopped via `IMediaCapture`.
5. The encoding does not drop frames during real-time recording on the reference hardware (a modern desktop CPU).
6. Quality presets (low/medium/high) are configurable, mapping to CRF values.
7. The recording pipeline uses zero managed allocations per frame on the hot path.

### Traceability

- **Interfaces:** `IMediaCapture`, `IVideoEncoder`
- **Test Suite:** `VideoRecordingTests`, `H264EncoderTests`, `FrameRateAccuracyTests`

---

## FR-MED-003: Audio Recording

**ID:** FR-MED-003
**Title:** Audio Recording (WAV/FLAC)
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The emulator shall record audio output to WAV (uncompressed PCM) or FLAC (lossless compressed) format. The recording captures the SID audio output at the configured sample rate.

### Acceptance Criteria

1. WAV format: 16-bit PCM, mono or stereo, at configurable sample rates (44100, 48000, 96000 Hz).
2. FLAC format: lossless compression of the same PCM data.
3. Recording captures the mixed SID output including all voices, filter, and volume.
4. For dual-SID configurations, stereo recording captures left/right channels independently.
5. Recording can be started, paused, resumed, and stopped independently of video recording.
6. Audio samples are buffered to prevent gaps during recording.
7. The recording includes digi playback ($D418) output at full fidelity.

### Traceability

- **Interfaces:** `IMediaCapture`, `IAudioEncoder`
- **Test Suite:** `AudioRecordingTests`, `WavFormatTests`, `FlacFormatTests`, `StereoRecordingTests`

---

## FR-MED-004: Synchronized A/V Capture

**ID:** FR-MED-004
**Title:** Synchronized Audio/Video Capture
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The emulator shall support recording audio and video simultaneously with correct synchronization. The audio and video streams are muxed into a single container file (MP4) with timestamps that maintain A/V sync throughout the recording.

### Acceptance Criteria

1. Audio and video streams are written to a single MP4 container with correct timestamps.
2. A/V sync drift does not exceed 1 frame duration (20ms PAL, 16.7ms NTSC) over any recording length.
3. The muxer correctly interleaves audio and video packets.
4. Starting A/V recording captures both streams from the same frame/sample boundary.
5. Pausing and resuming A/V recording maintains sync after the gap.
6. The `IMuxer` interface handles timestamp generation and stream interleaving.
7. Audio sample count per video frame is correctly calculated to avoid drift (PAL: 882.17 samples/frame at 44100Hz).

### Traceability

- **Interfaces:** `IMediaCapture`, `IMuxer`
- **Test Suite:** `AvSyncTests`, `MuxerTests`, `LongRecordingSyncTests`

---

## FR-MED-005: Format Selection

**ID:** FR-MED-005
**Title:** Output Format Selection and Configuration
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The media capture system shall support multiple output formats with configurable encoding parameters. Format selection is exposed through the `IMediaCapture` interface.

### Acceptance Criteria

1. Video formats: MP4 (H.264), AVI (uncompressed or MJPEG), GIF (animated, for short clips).
2. Audio formats: WAV (PCM 16-bit), FLAC (lossless), MP3 (lossy, for smaller files).
3. Image formats: PNG (lossless), BMP (uncompressed), JPEG (lossy).
4. Each format exposes its available quality/compression parameters.
5. Format availability is reported by `IMediaCapture.GetSupportedFormats()`.
6. Unavailable formats (e.g., if FFmpeg libraries are not present) are gracefully reported as unsupported without crashing.
7. The default format for each capture type is configurable via `IMediaCapture.SetDefaultFormat()`.

### Traceability

- **Interfaces:** `IMediaCapture`
- **Test Suite:** `FormatSelectionTests`, `FormatAvailabilityTests`, `GracefulDegradationTests`
