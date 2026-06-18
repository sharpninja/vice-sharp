namespace ViceSharp.Abstractions;

/// <summary>
/// Common interface for all audio sound generator chips.
/// </summary>
public interface IAudioChip : IClockedDevice
{
    /// <summary>Master volume level 0-15</summary>
    byte MasterVolume { get; set; }

    /// <summary>Number of audio channels</summary>
    int ChannelCount { get; }

    /// <summary>Generates next audio sample</summary>
    float GenerateSample();

    /// <summary>
    /// Samples submitted to the audio device but not yet played, i.e. the device's
    /// pending-playback queue depth. The pacing gate's sound back-pressure regulator
    /// reads this to throttle the emulation worker to the rate the device drains it.
    /// Defaults to 0 for chips with no live audio backend.
    /// </summary>
    int QueuedSampleCount => 0;

    /// <summary>
    /// True when this chip is actively streaming samples to an audio device (a backend is
    /// attached and the audio clock is configured), so it can act as the emulation timing
    /// source. Defaults to false (silent / parity mode).
    /// </summary>
    bool IsAudioTimingSource => false;
}