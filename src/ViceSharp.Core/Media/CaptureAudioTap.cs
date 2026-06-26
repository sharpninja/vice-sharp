namespace ViceSharp.Core.Media;

using System;
using ViceSharp.Abstractions;

/// <summary>
/// FR-MED-003 (runtime sound recording tap): a live-installed
/// <see cref="IAudioBackend"/> that sits permanently in the SID -> output
/// path and exposes a swappable recorder slot. Unlike
/// <see cref="RecordingAudioBackend"/> (whose recorder is fixed at
/// construction), the tap is built once at machine-build time and lets the
/// host attach/detach an <see cref="IAudioRecorder"/> at runtime so a
/// StartCapture/StopCapture pair can begin and finalise a WAV recording
/// without rebuilding the machine.
/// Use case: install the tap as the SID's audio backend (wrapping the real
/// platform backend, or null when headless). While no recorder is attached
/// the tap is a transparent pass-through with zero conversion work; once a
/// recorder is attached every submitted float batch is clamped to [-1, 1],
/// scaled to int16 (s * 32767), and written to the recorder before the
/// original float span is forwarded to the live output.
/// Acceptance: with no recorder, SubmitSamples forwards the exact float span
/// to the downstream and writes nothing; with a recorder attached, one int16
/// sample is written per input float (clamped) and the float span still
/// reaches the downstream so live playback is undisturbed; DetachRecorder
/// returns the previously-attached recorder (or null) and restores
/// pass-through.
/// </summary>
public sealed class CaptureAudioTap : IAudioBackend
{
    private readonly IAudioBackend? _downstream;
    private readonly object _sync = new();
    private readonly short[] _scratch;
    private IAudioRecorder? _recorder;

    /// <summary>
    /// Constructs a tap forwarding to <paramref name="downstream"/> (the real
    /// output backend, or null when headless / silent).
    /// </summary>
    /// <param name="downstream">Optional inner backend for live playback.</param>
    /// <param name="scratchSize">Internal short[] batch buffer size for float->int16 conversion.</param>
    public CaptureAudioTap(IAudioBackend? downstream = null, int scratchSize = 4096)
    {
        if (scratchSize <= 0) throw new ArgumentOutOfRangeException(nameof(scratchSize));
        _downstream = downstream;
        _scratch = new short[scratchSize];
    }

    /// <inheritdoc/>
    public int QueuedSampleCount => _downstream?.QueuedSampleCount ?? 0;

    /// <summary>True while a recorder is attached (a recording is in progress).</summary>
    public bool IsRecording
    {
        get { lock (_sync) { return _recorder is not null; } }
    }

    /// <summary>
    /// Attaches a recorder so subsequent submitted samples are also written to
    /// it. Replaces any previously-attached recorder (the caller owns the old
    /// recorder's lifetime).
    /// </summary>
    public void AttachRecorder(IAudioRecorder recorder)
    {
        ArgumentNullException.ThrowIfNull(recorder);
        lock (_sync) { _recorder = recorder; }
    }

    /// <summary>
    /// Detaches the current recorder (if any) and returns it so the caller can
    /// finalise it. Restores transparent pass-through.
    /// </summary>
    public IAudioRecorder? DetachRecorder()
    {
        lock (_sync)
        {
            var previous = _recorder;
            _recorder = null;
            return previous;
        }
    }

    /// <inheritdoc/>
    public void SubmitSamples(ReadOnlySpan<float> samples)
    {
        // Snapshot the recorder under the lock, then do the clamp/scale/write
        // OUTSIDE it so the recorder's work never gates Attach/Detach (or, with a
        // background-queue recorder, a transient back-pressure block). The recorder
        // itself is safe against a concurrent Detach/Stop.
        IAudioRecorder? recorder;
        lock (_sync) { recorder = _recorder; }

        if (recorder is not null && !samples.IsEmpty)
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
                recorder.WriteSamples(new ReadOnlySpan<short>(_scratch, 0, chunk));
                processed += chunk;
            }
        }

        // Forward outside the lock so live playback latency is never gated on
        // the recorder's file I/O when a recording is in progress.
        _downstream?.SubmitSamples(samples);
    }

    /// <inheritdoc/>
    public void Pause() => _downstream?.Pause();

    /// <inheritdoc/>
    public void Resume() => _downstream?.Resume();

    /// <inheritdoc/>
    public void Stop() => _downstream?.Stop();
}
