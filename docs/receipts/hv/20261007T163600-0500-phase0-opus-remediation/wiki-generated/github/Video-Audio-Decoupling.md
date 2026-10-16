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

### Presentation Timing

Presentation timing (vsync against the host display, compositor pacing) is owned by the host render surface, not by `IFrameSink` or the emulation loop. The desktop UI's in-process renderer reads the committed frame buffer on the UI's own render schedule; remote UIs consume the gRPC `VideoService` frame stream at whatever cadence they choose. Emulation-side pacing is a separate concern handled by the emulation gate (see Emulation Pacing below).

### Frame Skip

When the emulation falls behind real time (or during warp/fast-forward), display frames are effectively skipped without perturbing emulation:

- **Latest-wins overwrite:** every frame is fully computed (all cycle-accurate side effects occur, preserving determinism), but if a new frame completes before the previous one was consumed, the committed buffer is simply overwritten. Presentation never stalls emulation and emulation never stalls presentation.
- **Skip visibility:** the sink's `FrameCount` counts presented frames, so hosts can derive an effective presentation rate and surface a speed/fps indicator (as the desktop status bar does).

## Audio Decoupling

### Ring Buffer

Audio samples produced by the emulation engine (SID output for C64) are submitted to the `IAudioBackend`, which writes them into its device ring buffer. The host audio system drains that buffer on its own schedule.

```
Emulation thread:    [ SubmitSamples ] --> | Device Ring | --> [ Playback ] Host audio device
                                           |  (N slots)  |
```

Ring buffer parameters (desktop `WinMmAudioBackend`):
- **Capacity:** 8 fragments of 256 samples (2048 samples, roughly 46 ms at 44100 Hz). Free space is calculated from `waveOutGetPosition` against the write cursor, matching VICE's WMM driver, so back-pressure reflects samples Windows has actually played.
- **Write policy:** live audio waits for a free fragment instead of dropping PCM when the device queue is full; that blocking write is exactly the sound back-pressure the VICE pacing gate relies on. During warp or fast-forward the live output leaf is paused and fragments are discarded without blocking.
- **Underrun:** if the emulation falls behind, the device plays through already-submitted data and the gap is audible; the pacing gate's fine advance granularity (roughly 2 ms slices) keeps submissions ahead of the roughly 5.8 ms fragment drain in normal operation.

### Sample Rate Conversion

The emulated machine's audio output rate is derived from its master clock and may not match the host audio device's sample rate:

| Machine | Native Audio Rate | Typical Host Rate | Ratio |
|---------|-------------------|-------------------|-------|
| C64 PAL | 985,248 / cycle divisor ~= variable | 48,000 Hz | Requires resampling |
| C64 NTSC | 1,022,727 / cycle divisor ~= variable | 48,000 Hz | Requires resampling |

The SID chip produces one sample per CPU cycle in the simplest model, but practical implementations downsample to a target rate. ViceSharp handles rate conversion in the SID output path:

1. **Cycle-to-sample cadence:** the SID emits one output sample every `clock / sampleRate` cycles and submits them to the `IAudioBackend` in small fixed batches (256 samples).
2. **Limiter-rate cadence tracking:** when the pace limiter is set away from 100%, the session pushes the requested rate into the audio chip (`IAudioChip.SetRelativeSpeed`), stretching or compressing the sample cadence so the fixed-rate audio device paces emulation at the requested speed. This mirrors VICE's `sound_set_relative_speed` (`sound.c`).

### Latency Management

Audio latency is the delay between the emulation producing a sample and the user hearing it. Lower latency improves responsiveness (important for music programs) but requires tighter scheduling.

Latency components:
- **Device queue latency:** `QueuedSampleCount / SampleRate` seconds of already-submitted audio waiting to play.
- **Audio backend latency:** The host audio API's own buffering (e.g., the WinMM buffer chain in the desktop `WinMmAudioBackend`).
- **Total latency:** Sum of the two.

There is no latency setpoint on the contract. Instead, the backend exposes its queue state (`QueuedSampleCount`, and `AvailableSampleCount` for how much more the device can accept without blocking), and latency is bounded structurally: the backend's fixed buffer chain caps how much audio can ever be queued, and the VICE pacing gate's sound back-pressure (below) stops the emulator from running ahead of the device, so the queue hovers near full without growing.

## IFrameSink Contract

The `IFrameSink` interface (`src/ViceSharp.Abstractions/IFrameSink.cs`) is the boundary between the emulation engine and the display subsystem. It has three members: `PresentFrame(ReadOnlySpan<byte> pixelData)`, `IsReady`, and `FrameCount`.

1. **Thread safety:** `PresentFrame` is called from the emulation thread. The sink implementation must be safe for cross-thread access (typically by copying the pixel data into its own buffer or using an atomic swap).

2. **Pixel format:** Frames are delivered as raw BGRA8888 bytes (`width * height * 4`). There is no per-sink format negotiation; the frame path, the capture recorders, and the gRPC `VideoService` all share this format.

3. **Backpressure:** If `IsReady` returns false, the emulation engine skips delivery of the current frame (the frame is still computed for correctness). The sink signals readiness when it has consumed the previous frame.

4. **Diagnostics:** `FrameCount` reports total frames presented since the last reset, which hosts use for effective-rate display.

## IAudioBackend Contract

The `IAudioBackend` interface (`src/ViceSharp.Abstractions/IAudioBackend.cs`) is the boundary between the emulation engine and the audio subsystem. Its members are `SubmitSamples(ReadOnlySpan<float>)`, `QueuedSampleCount`, `AvailableSampleCount`, `Pause`, `Resume`, and `Stop`.

1. **Thread model:** `SubmitSamples` is called from the emulation thread. The backend's playback runs on the host audio system's own thread; the backend's internal buffering mediates between the two.

2. **Sample format:** Samples are 32-bit floating-point PCM. The SID produces a mono stream; the desktop `WinMmAudioBackend` converts it to clamped 16-bit mono PCM at 44100 Hz (`AudioSampleConverter`) and writes it into a looping WaveOut ring of 8 fragments of 256 samples.

3. **Lifecycle:** Backends arrive pre-configured from their factory (`AudioBackendFactory`); there is no separate initialize step. `Pause`/`Resume` control playback without discarding buffered data (used for warp and fast-forward suspension). `Stop` discards all buffered samples and releases audio resources.

4. **Queue monitoring and back-pressure:** `QueuedSampleCount` reports how many samples are buffered but not yet played; `AvailableSampleCount` reports how many more the device can accept without blocking (backends without finite device space report a large value). The VICE pacing gate uses the blocking fragment write as its sound back-pressure regulator.

## Emulation Pacing: Gate Strategies

Pacing the emulation worker to real time is a pluggable strategy, the `IEmulationGate` (`src/ViceSharp.Host/Services/IEmulationGate.cs`). The `EmulationPumpService` owns the worker thread and clean-instruction cycle advancement; the gate decides, per worker iteration, how many cycles each running session advances and how the worker blocks or sleeps. Gates must never busy-spin.

Two strategies are selectable (`src/ViceSharp.Host/Services/EmulationGateStrategies.cs`, stored ids `"semaphore"` and `"vice"`); the user picks one via the limiter settings and can switch live. The default is **VICE**.

### VICE (default)

Faithful to VICE's Layer-3 outer throttle (`vsync.c` / `sound.c`). Two regulators, in precedence order:

1. **Sound-buffer back-pressure** (`sound.c` `sound_flush`): when the audio device is the timing source (the SID has a live backend and a configured audio clock), the emulator advances freely and the audio backend blocks only when a completed sound fragment is written to a full device queue. Produce sound first, then write a whole fragment when one fits, otherwise retry after a short sleep. When sound is the timing source, vsync is skipped for that session.
2. **vsync** (`vsync.c` `set_timer_speed` / `vsync_do_vsync`): compute the emulated-cycles-per-second target (master clock x speed), convert the emulated-cycle delta since an anchor into how many host ticks should have passed, and sleep the remainder on an OS waitable timer. It targets a wall-clock time derived from cycle progress, not a fixed frame rate, and self-corrects: cycles a tick could not advance persist as a deficit and are caught up on following ticks.

### Semaphore (fallback)

A high-resolution OS waitable timer fires at 500 Hz and releases a `SemaphoreSlim`; the worker blocks on it (yielding the CPU) and advances the real-time cycle deficit since its anchor each tick, so the emulated clock tracks real time regardless of timer jitter.

### Warp and the Pace Limiter

Both gates honor each session's limiter/warp state (`EmulatorRuntimeSession`):

- **Limiter rate:** `LimiterRatePercent` (default 100) scales the pacing target; the UI exposes it as a live slider and a speed-cycle button, and `EmulatorHost.SetLimiterRate` sets it remotely. With the limiter on, the session also pushes the rate into the SID (`IAudioChip.SetRelativeSpeed`) so the fixed-rate audio device paces the requested speed, mirroring VICE's `sound_set_relative_speed`.
- **Warp** (limiter off): highest precedence. The gate applies no pacing and runs large cycle bursts flat out. Live audio output is suspended so SID fragment writes are discarded without blocking, mirroring VICE's `sound_flush` warp behavior; SID sample calculation itself continues, and an attached sound recorder keeps receiving samples through the capture tap (TR-AUDIO-WARP-001).
- **Fast-forward ceiling:** live audio is also suspended when the limiter rate exceeds 200% (`LiveAudioMaxRatePercent`): a fixed 44100 Hz device cannot be fed faster than double speed, so beyond it the vsync regulator paces the requested rate while the live audio leaf discards.
