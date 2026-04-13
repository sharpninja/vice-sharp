# Video and Audio Decoupling

Emulation engines run at variable speeds depending on host system load, emulated machine complexity, and user-requested fast-forward or slow-motion. The display and audio subsystems of the host, however, operate on their own fixed schedules: the monitor refreshes at its native rate (typically 60 Hz), and the audio device expects a continuous stream of samples at its configured sample rate (typically 44100 or 48000 Hz). Decoupling the emulation engine from these output subsystems is essential for smooth, artifact-free presentation.

## Why Decoupling Is Needed

Without decoupling, the emulation loop would be rigidly locked to either the display or audio refresh rate. This causes several problems:

1. **Speed variation:** The emulated machine runs at a fixed cycle rate (e.g., 985,248 Hz for C64 PAL), but the host may not be able to sustain exactly that rate. During complex scenes (e.g., many sprites with DMA stealing), the emulation may briefly slow down; during simple scenes it may run faster than real-time.

2. **Mismatched rates:** C64 PAL produces 50.125 frames per second, but most monitors run at 60 Hz. NTSC machines produce ~59.826 fps, close to 60 Hz but not exact. Even small rate mismatches cause periodic frame judder without buffering.

3. **Fast-forward and slow-motion:** Users expect to run the emulator at 2x, 4x, or fractional speeds for debugging or speedrunning. The output subsystems must handle arbitrary input rates gracefully.

4. **Background operation:** When the emulator window is minimized or occluded, video output should be suppressed to save GPU resources, but audio should continue playing (or be paused, at the user's preference).

## Video Decoupling

### Frame Buffer Double-Buffering

The video chip (VIC-II, VIC, TED, etc.) renders into a **back buffer** during emulation. When a complete frame is finished (at the vertical blank boundary), the back buffer is atomically swapped with the **front buffer**. The `IFrameSink` always reads from the front buffer.

```
Emulation thread:        [  Render frame N into back buffer  ] -- swap -- [  Render N+1  ]
                                                                   |
IFrameSink reads:        [  Display frame N-1 from front     ] ---------> [  Display N   ]
```

Buffer management rules:
- The swap is a pointer exchange, not a memory copy. Cost: single atomic operation.
- If the emulation completes a frame before the sink has consumed the previous one, the new frame overwrites the front buffer (latest-wins policy). This prevents the emulation from stalling on slow display paths.
- If the sink requests a frame before the emulation has produced one, it re-presents the current front buffer (frame repeat).

### VSync Strategies

ViceSharp supports multiple synchronization strategies, configured via the `IFrameSink`:

| Strategy | Behavior | Use Case |
|----------|----------|----------|
| **VSync-locked** | Presents frames synchronized to the monitor's vertical refresh. Uses compositor hints where available. | Standard desktop use. Eliminates tearing but may introduce up to one frame of input latency. |
| **Adaptive VSync** | VSync when the emulation keeps up; tears when it falls behind. | Reduces stutter during brief slowdowns compared to hard VSync. |
| **Unlocked** | Presents frames as fast as the emulation produces them, ignoring monitor refresh. | Benchmarking, fast-forward mode. May cause visible tearing. |
| **Frame pacing** | Inserts variable sleep between frames to match the emulated machine's native rate. | Authentic experience: C64 PAL runs at exactly 50.125 fps even on a 60 Hz display. |

### Frame Skip

When the emulation falls behind real-time (or during fast-forward), frames may be skipped to catch up:

- **Automatic skip:** If the emulation is more than one frame behind, intermediate frames are computed (all cycle-accurate side effects occur) but not rendered to the `IFrameSink`. The video chip still executes all cycles, maintaining determinism, but the pixel output is discarded.
- **Skip limit:** A configurable maximum skip count (default: 4) prevents the display from freezing entirely during sustained slowdowns.
- **Skip reporting:** `FrameMetadata.WasSkipped` indicates whether a frame was skipped, allowing the UI to display a frame-skip indicator.

## Audio Decoupling

### Ring Buffer

Audio samples produced by the emulation engine (SID output for C64, VIC audio for VIC-20, TED audio for Plus/4) are written into a lock-free ring buffer. The `IAudioBackend` reads from this ring buffer on its own thread, typically driven by the host audio system's callback.

```
Emulation thread:    [ Write samples ] --> | Ring Buffer | --> [ Read samples ] Audio callback thread
                                           |  (N slots)  |
```

Ring buffer parameters:
- **Capacity:** Configurable, typically 4096-8192 samples. Larger buffers increase latency but tolerate more scheduling jitter.
- **Write policy:** If the buffer is full (emulation running faster than real-time), excess samples are dropped from the oldest end. This prevents unbounded latency growth during fast-forward.
- **Read policy:** If the buffer underruns (emulation running slower than real-time), the audio backend inserts silence or repeats the last sample block. Both approaches are audible but preferable to undefined behavior.

### Sample Rate Conversion

The emulated machine's audio output rate is derived from its master clock and may not match the host audio device's sample rate:

| Machine | Native Audio Rate | Typical Host Rate | Ratio |
|---------|-------------------|-------------------|-------|
| C64 PAL | 985,248 / cycle divisor ~= variable | 48,000 Hz | Requires resampling |
| C64 NTSC | 1,022,727 / cycle divisor ~= variable | 48,000 Hz | Requires resampling |

The SID chip produces one sample per CPU cycle in the simplest model, but practical implementations downsample to a target rate. ViceSharp performs sample rate conversion using:

1. **Accumulate-and-average:** During each emulation step, SID output values are accumulated. At the output sample boundary, the accumulated values are averaged. This is a simple box filter suitable for the hot path.
2. **Optional high-quality resampling:** When `AudioParameters.HighQualityResample` is true, a polyphase FIR filter is used. This produces higher-fidelity output at the cost of additional CPU. Applied as a post-processing step outside the cycle-accurate hot path.

### Latency Management

Audio latency is the delay between the emulation producing a sample and the user hearing it. Lower latency improves responsiveness (important for music programs) but requires tighter scheduling.

Latency components:
- **Ring buffer latency:** `BufferSizeSamples / SampleRate` seconds. A 2048-sample buffer at 48 kHz = ~42 ms.
- **Audio backend latency:** The host audio API's own buffering. Typically 5-20 ms for modern APIs (WASAPI exclusive, PulseAudio low-latency, CoreAudio).
- **Total latency:** Sum of ring buffer and backend latency.

`IAudioBackend.TargetLatencyMs` sets the desired total latency. The implementation adjusts the ring buffer size and requests an appropriate backend buffer size. `ActualLatencyMs` reports the measured end-to-end latency.

Latency adaptation:
- If `ActualLatencyMs` exceeds `TargetLatencyMs` by more than 50%, the ring buffer is drained faster (slight pitch increase, inaudible at small adjustments) to reduce the backlog.
- If the ring buffer is consistently less than 25% full, the drain rate is slowed slightly to prevent underruns.

## IFrameSink Contract

The `IFrameSink` interface is the boundary between the emulation engine and the display subsystem:

1. **Thread safety:** `PresentFrame` is called from the emulation thread. The sink implementation must be safe for cross-thread access (typically by copying the pixel data into its own buffer or using an atomic swap).

2. **Pixel format:** The emulation engine produces frames in the format specified by `PreferredFormat`. The sink declares its preference; the engine performs any necessary conversion before delivery. `RGBA8888` is the default.

3. **Resolution:** `DisplayResolution` may change at runtime if the emulated machine switches video modes (e.g., VIC-II multicolor vs. hires, VDC 80-column mode). The sink must handle resolution changes gracefully.

4. **Backpressure:** If `IsReady` returns false, the emulation engine skips delivery of the current frame (the frame is still computed for correctness). The sink signals readiness when it has consumed the previous frame.

5. **Metadata:** `FrameMetadata` carries the frame number, cycle stamp, video standard, and whether the frame was skipped. The sink can use this for frame-pacing calculations and diagnostic display.

## IAudioBackend Contract

The `IAudioBackend` interface is the boundary between the emulation engine and the audio subsystem:

1. **Thread model:** `SubmitSamples` is called from the emulation thread. The backend's playback callback runs on a separate audio thread. The ring buffer mediates between the two.

2. **Sample format:** Samples are 32-bit floating-point PCM, mono or stereo as specified by `AudioParameters.Channels`. For the SID (mono output), the backend may apply stereo expansion or panning as a post-processing effect.

3. **Lifecycle:** `Initialize` must be called before any samples are submitted. `Pause`/`Resume` control playback without discarding buffered data (used when the emulator window loses focus). `Stop` discards all buffered data and releases audio resources.

4. **Queue monitoring:** `QueuedSampleCount` reports how many samples are buffered but not yet played. The emulation engine uses this to detect overrun (too many queued samples = emulation running too fast) and underrun (too few = emulation running too slow).

## Refresh Strategies

ViceSharp supports three high-level synchronization strategies that coordinate video and audio decoupling:

### Sync to Audio

The emulation loop paces itself to maintain a steady audio buffer fill level. The audio ring buffer drives timing:

- If the buffer is more than 75% full, the emulation pauses briefly.
- If the buffer is less than 25% full, the emulation runs uncapped until the buffer recovers.
- Video frames are delivered whenever available, with frame skip or repeat as needed.

This strategy produces the smoothest audio at the potential cost of occasional video judder. Recommended for music playback and general use.

### Sync to Video

The emulation loop paces itself to the host display's refresh rate (via VSync):

- One frame of emulation is computed per display refresh.
- Audio samples are produced at whatever rate results from the emulation, and the ring buffer absorbs rate differences.
- Sample rate drift is corrected by micro-adjusting the playback rate (pitch stretching within inaudible limits).

This strategy produces the smoothest video at the potential cost of occasional audio artifacts. Recommended for games and visual demonstrations.

### Free-Run

The emulation loop runs as fast as possible with no synchronization:

- Video frames are delivered at the emulation's natural rate. The sink discards any it cannot display.
- Audio samples are produced at the emulation's natural rate. Overflow is dropped from the ring buffer.
- Useful for fast-forward, benchmarking, and automated testing.

The user selects the strategy via a system-level setting. The default is **Sync to Audio**.
