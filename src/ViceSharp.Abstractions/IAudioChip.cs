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
}