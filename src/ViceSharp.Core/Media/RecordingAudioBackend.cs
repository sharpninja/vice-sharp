namespace ViceSharp.Core.Media;

using System;
using ViceSharp.Abstractions;

/// <summary>
/// FR/TR: FR-MED-003 (audio backend tee to WAV recorder).
/// Use case: Bridge the SID/IAudioBackend pipeline (float PCM in [-1, 1])
/// to the WAV recorder (int16 LE PCM) without disturbing the playback
/// path. The tee performs the canonical clamp + scale (s * 32767) and
/// writes each batch into the recorder. An optional downstream backend
/// receives the original unmodified float span so real audio output
/// continues to work alongside recording.
/// Acceptance: SubmitSamples forwards the exact float buffer to the
/// downstream (when present) and writes one int16 sample per input
/// float to the recorder; out-of-range floats are clamped symmetrically
/// to [-1, 1] before scaling so -1.0 -> -32767 and 1.0 -> 32767.
/// </summary>
public sealed class RecordingAudioBackend : IAudioBackend
{
    private readonly WavAudioRecorder _recorder;
    private readonly IAudioBackend? _downstream;
    private readonly short[] _scratch;

    /// <summary>
    /// Constructs a tee that writes int16 PCM into <paramref name="recorder"/>
    /// and optionally forwards float samples to <paramref name="downstream"/>.
    /// </summary>
    /// <param name="recorder">Required WAV recorder. Tee does not own its lifetime.</param>
    /// <param name="downstream">Optional inner backend for live playback.</param>
    /// <param name="scratchSize">Internal short[] buffer size for batched int16 conversion.</param>
    public RecordingAudioBackend(WavAudioRecorder recorder, IAudioBackend? downstream = null, int scratchSize = 4096)
    {
        ArgumentNullException.ThrowIfNull(recorder);
        if (scratchSize <= 0) throw new ArgumentOutOfRangeException(nameof(scratchSize));
        _recorder = recorder;
        _downstream = downstream;
        _scratch = new short[scratchSize];
    }

    /// <inheritdoc/>
    public int QueuedSampleCount => _downstream?.QueuedSampleCount ?? 0;

    /// <inheritdoc/>
    public void SubmitSamples(ReadOnlySpan<float> samples)
    {
        int processed = 0;
        while (processed < samples.Length)
        {
            int chunk = Math.Min(_scratch.Length, samples.Length - processed);
            for (int i = 0; i < chunk; i++)
            {
                float s = samples[processed + i];
                if (s > 1f) s = 1f;
                else if (s < -1f) s = -1f;
                _scratch[i] = (short)(s * 32767f);
            }
            _recorder.WriteSamples(new ReadOnlySpan<short>(_scratch, 0, chunk));
            processed += chunk;
        }

        _downstream?.SubmitSamples(samples);
    }

    /// <inheritdoc/>
    public void Pause() => _downstream?.Pause();

    /// <inheritdoc/>
    public void Resume() => _downstream?.Resume();

    /// <inheritdoc/>
    public void Stop() => _downstream?.Stop();
}
