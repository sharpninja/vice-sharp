namespace ViceSharp.Abstractions;

/// <summary>
/// Platform-specific audio output backend. Receives PCM samples from the
/// emulation engine's ring buffer and delivers them to the host audio system.
/// </summary>
public interface IAudioBackend
{
    /// <summary>Submits a buffer of PCM samples for playback.</summary>
    void SubmitSamples(ReadOnlySpan<float> samples);

    /// <summary>Number of samples currently queued for playback.</summary>
    int QueuedSampleCount { get; }

    /// <summary>Pauses audio playback without discarding buffered data.</summary>
    void Pause();

    /// <summary>Resumes audio playback.</summary>
    void Resume();

    /// <summary>Stops playback and discards all buffered samples.</summary>
    void Stop();
}
