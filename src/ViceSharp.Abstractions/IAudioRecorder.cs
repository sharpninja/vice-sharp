namespace ViceSharp.Abstractions;

using System;

/// <summary>
/// FR-MED-003: persistent audio sink that captures the emulator's audio
/// output stream to a file (WAV, FLAC, etc.). Implementations write a
/// container-specific header on construction, append samples on
/// <see cref="WriteSamples"/>, and finalise on <see cref="Stop"/>.
/// Disposing the recorder must be equivalent to calling Stop.
/// </summary>
public interface IAudioRecorder : IDisposable
{
    /// <summary>Sample rate in Hz (44100, 48000, 96000, etc.).</summary>
    int SampleRate { get; }

    /// <summary>Channel count (1 = mono, 2 = stereo).</summary>
    int Channels { get; }

    /// <summary>
    /// Append a buffer of signed 16-bit PCM samples. For stereo
    /// recorders the caller must interleave channels as
    /// [L0, R0, L1, R1, ...].
    /// </summary>
    void WriteSamples(ReadOnlySpan<short> samples);

    /// <summary>
    /// Finalise the recording (patch header sizes, flush). Further
    /// WriteSamples calls after Stop are silently ignored.
    /// </summary>
    void Stop();
}
