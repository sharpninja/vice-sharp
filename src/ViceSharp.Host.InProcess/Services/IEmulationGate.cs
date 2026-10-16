using ViceSharp.Host.Runtime;

namespace ViceSharp.Host.Services;

/// <summary>
/// Strategy for gating/pacing the emulation worker to real time (BUG-THROTTLE-001).
/// The <see cref="EmulationPumpService"/> owns the worker thread, the clean-instruction
/// cycle advancement, and per-frame bookkeeping; an <see cref="IEmulationGate"/> decides,
/// for each worker iteration, how many cycles a Running session advances and how it is
/// throttled (block/sleep) to real time. Implementations must never busy-spin.
///
/// Selectable strategies:
///  - "Semaphore": a high-resolution OS timer releases a SemaphoreSlim; the worker blocks
///    on it and advances the real-time cycle deficit each tick (warp passes non-blocking).
///  - "VICE": faithful to VICE's Layer-3 outer throttle - sound-buffer back-pressure first
///    (block while the audio device's buffer is full), then vsync (sleep so wall-clock
///    tracks emulated-cycle progress, not a fixed frame rate); warp skips both.
/// </summary>
public interface IEmulationGate : IDisposable
{
    /// <summary>Strategy name (e.g. "Semaphore", "VICE").</summary>
    string Name { get; }

    /// <summary>Start any auxiliary threads/timers. Called once when the pump starts.</summary>
    void Start();

    /// <summary>Stop auxiliary threads/timers and release resources. Called when the pump stops.</summary>
    void Stop();

    /// <summary>
    /// Run one worker iteration: for each Running session in <paramref name="registry"/>,
    /// advance it via <paramref name="advance"/> (which runs that many master cycles in
    /// clean instruction groups and returns the count advanced) and throttle to real time
    /// per this strategy, honoring each session's limiter/warp state. Returns true when at
    /// least one session was Running, so the worker can idle when it returns false.
    /// </summary>
    bool Tick(EmulatorRuntimeRegistry registry, Func<EmulatorRuntimeSession, long, long> advance);
}
