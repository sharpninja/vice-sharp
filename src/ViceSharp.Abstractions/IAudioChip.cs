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
    /// pending-playback queue depth. This is retained as diagnostics; sound pacing
    /// uses <see cref="AvailableSampleCount"/> so it follows VICE's buffer-space gate.
    /// Defaults to 0 for chips with no live audio backend.
    /// </summary>
    int QueuedSampleCount => 0;

    /// <summary>
    /// Samples the live audio backend can accept without blocking. VICE uses
    /// sound-driver buffer space, rounded to whole fragments, as the timing gate.
    /// Defaults to a large value for chips with no finite live backend.
    /// </summary>
    int AvailableSampleCount => int.MaxValue;

    /// <summary>
    /// Native sound fragment size in samples. VICE only flushes whole fragments
    /// and waits until the device has room for at least one fragment.
    /// </summary>
    int AudioFragmentSampleCount => 256;

    /// <summary>
    /// True when this chip is actively streaming samples to an audio device (a backend is
    /// attached and the audio clock is configured), so it can act as the emulation timing
    /// source. Defaults to false (silent / parity mode).
    /// </summary>
    bool IsAudioTimingSource => false;

    /// <summary>
    /// Relative emulation speed for live audio, in percent (VICE
    /// sound_set_relative_speed, sound.c:1799): the chip scales its
    /// tick-to-sample cadence by speed/100 (clkstep, sound.c:1067) so the
    /// fixed-rate device drains one emulated-second of audio in 100/speed
    /// wall seconds - audio back-pressure then paces emulation to the
    /// requested rate, pitch shifting with speed exactly like VICE
    /// fast-forward. Non-positive values are ignored. Default: no-op for
    /// chips without live audio.
    /// </summary>
    void SetRelativeSpeed(double speedPercent)
    {
    }
}
