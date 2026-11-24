namespace ViceSharp.Abstractions;

/// <summary>
/// Platform-specific audio output backend. Receives PCM samples from the
/// emulation engine's ring buffer and delivers them to the host audio system.
/// </summary>
public interface IAudioBackend
{
    /// <summary>
    /// Whether a usable output device is attached.
    /// This does not indicate whether recording or sample submission remains supported.
    /// Existing backends default to available; device-aware backends and wrappers report
    /// false when no playback device is present or it has failed or been disposed.
    /// </summary>
    bool IsAvailable => true;

    /// <summary>
    /// Whether the backend is available and its playback back-pressure can regulate emulation.
    /// 2026-11-23, FR-SID-013 / FR-SNDREG-001: a silent sink can be available without
    /// providing a timing source, as in VICE's dummy driver.
    /// </summary>
    bool IsTimingSource => IsAvailable;

    /// <summary>Submits a buffer of PCM samples for playback.</summary>
    void SubmitSamples(ReadOnlySpan<float> samples);

    /// <summary>Number of samples currently queued for playback.</summary>
    int QueuedSampleCount { get; }

    /// <summary>
    /// Number of additional samples the playback device can accept without blocking.
    /// Backends that do not expose finite device space report a large value.
    /// </summary>
    int AvailableSampleCount => int.MaxValue;

    /// <summary>Pauses audio playback without discarding buffered data.</summary>
    void Pause();

    /// <summary>Resumes audio playback.</summary>
    void Resume();

    /// <summary>Stops playback and discards all buffered samples.</summary>
    void Stop();
}
