using System;
using ViceSharp.Abstractions;

namespace ViceSharp.Host.Audio;

/// <summary>
/// The AppContainer-safe console (UWP/Xbox) real-time audio backend for live SID
/// playback (PLAN-XBOXUWP S18, IMPL-XBOXUWP-018; FR-XAUDIO-003, TR-XAUDIO-002).
///
/// <para>Unlike the desktop <see cref="WinMmAudioBackend"/>, which applies device
/// back-pressure (a full <c>waveOut</c> queue parks the emulation worker in
/// <c>WaitAndSubmitBuffer</c> until Windows plays enough samples), this backend is a
/// DECOUPLED, NON-blocking ring sink: <see cref="SubmitSamples"/> writes each SID
/// fragment into the <see cref="XAudio2AudioMath"/> ring and returns immediately,
/// dropping the oldest fragment when the ring is full
/// (<see cref="XAudio2AudioMath.DropsOldestWhenRingFull"/>). The deterministic
/// emulation worker is therefore never parked on the audio device - there is no
/// blocking primitive on the submit path, so <see cref="Pause"/> can never fail to
/// unpark a worker (there is no waiter to release) and can never deadlock a submitting
/// worker.</para>
///
/// <para>The native XAudio2 calls sit behind the <see cref="ISourceVoiceDevice"/> seam.
/// The real device (<see cref="XAudio2SourceVoiceDevice"/>) uses only
/// <c>[LibraryImport("xaudio2_9.dll")]</c> blittable vtable interop (no winmm, no
/// kernel32), and this backend declares no P/Invoke of its own. When the device cannot
/// be opened (CI / headless / non-Windows / driver failure) the backend degrades to a
/// silent no-op so nothing crashes and no audio device is opened.</para>
///
/// <para>Master volume / mute is applied exactly once, downstream in
/// <see cref="AudioSampleConverter"/> via <see cref="MasterAudioControl.EffectiveGain"/>,
/// as the float fragments are converted to little-endian PCM16 before they reach the
/// ring - matching the desktop WinMm path so the two heads never diverge on gain.</para>
/// </summary>
public sealed class XAudio2SourceVoiceBackend : IAudioBackend, IDisposable
{
    private const int SampleRate = 44100;
    private const int Channels = 1;
    private const int BytesPerSample = 2; // 16-bit mono PCM.
    private const int FragmentSampleCount = XAudio2AudioMath.FragmentSampleCount;
    private const int BufferFragmentCount = XAudio2AudioMath.BufferFragmentCount;
    private const int FragmentBytes = FragmentSampleCount * BytesPerSample;

    private readonly object _lock = new();
    private readonly ISourceVoiceDevice _device;
    private readonly byte[] _scratch = new byte[FragmentBytes];

    private int _head;
    private int _queued;
    private bool _open;
    private bool _paused;
    private bool _disposed;

    /// <summary>
    /// Creates the console audio backend over the real XAudio2 source-voice device,
    /// opening it immediately. If the device cannot be opened the backend is a silent
    /// no-op.
    /// </summary>
    public XAudio2SourceVoiceBackend()
        : this(CreateRealDevice)
    {
    }

    /// <summary>
    /// Test / composition seam: creates the backend over a supplied device factory and
    /// opens the device immediately. Production uses the parameterless constructor
    /// (the real <see cref="XAudio2SourceVoiceDevice"/>); tests inject a fake to exercise
    /// the ring / submit / pause / degrade logic off-console.
    /// </summary>
    /// <param name="deviceFactory">Factory for the device to open.</param>
    internal XAudio2SourceVoiceBackend(Func<ISourceVoiceDevice> deviceFactory)
    {
        ArgumentNullException.ThrowIfNull(deviceFactory);
        _device = deviceFactory();

        try
        {
            _open = _device.Open(SampleRate, Channels, FragmentBytes, BufferFragmentCount);
        }
        catch
        {
            // No device / driver: stay silent rather than failing the run.
            _open = false;
        }

        if (_open)
        {
            try
            {
                _device.Start();
            }
            catch
            {
                _open = false;
            }
        }
    }

    private static ISourceVoiceDevice CreateRealDevice() => new XAudio2SourceVoiceDevice();

    /// <inheritdoc/>
    public void SubmitSamples(ReadOnlySpan<float> samples)
    {
        if (_disposed || !_open || samples.IsEmpty)
            return;

        // The lock only guards fast in-memory accounting + a non-blocking device submit;
        // it is never held across a wait, so a worker can never park here and Pause()
        // can always acquire it promptly.
        lock (_lock)
        {
            if (_disposed || !_open || _paused)
                return;

            ReconcileDrainedLocked();

            var offset = 0;
            while (offset < samples.Length)
            {
                var take = Math.Min(FragmentSampleCount, samples.Length - offset);
                var pcmBytes = take * BytesPerSample;

                AudioSampleConverter.ConvertToPcm16(
                    samples.Slice(offset, take),
                    _scratch.AsSpan(0, pcmBytes),
                    MasterAudioControl.EffectiveGain);

                // Drop-oldest when the ring is full; the producer never blocks and the
                // queued count is held at the ring capacity, never above.
                var write = XAudio2AudioMath.Enqueue(_head, _queued, BufferFragmentCount);
                _head = write.Head;
                _queued = write.QueuedFragmentCount;

                _device.SubmitBuffer(_scratch.AsSpan(0, pcmBytes));
                offset += take;
            }
        }
    }

    /// <inheritdoc/>
    public int QueuedSampleCount
    {
        get
        {
            if (_disposed || !_open)
                return 0;

            lock (_lock)
            {
                if (_disposed || !_open)
                    return 0;

                ReconcileDrainedLocked();
                return _queued * FragmentSampleCount;
            }
        }
    }

    /// <inheritdoc/>
    public int AvailableSampleCount
    {
        get
        {
            if (_disposed || !_open)
                return int.MaxValue;

            lock (_lock)
            {
                if (_disposed || !_open)
                    return int.MaxValue;

                ReconcileDrainedLocked();
                return (BufferFragmentCount - _queued) * FragmentSampleCount;
            }
        }
    }

    /// <inheritdoc/>
    public void Pause()
    {
        if (_disposed || !_open)
            return;

        lock (_lock)
        {
            if (_disposed || !_open || _paused)
                return;

            _paused = true;
            _device.Stop();
        }
    }

    /// <inheritdoc/>
    public void Resume()
    {
        if (_disposed || !_open)
            return;

        lock (_lock)
        {
            if (_disposed || !_open || !_paused)
                return;

            _paused = false;
            _device.Start();
        }
    }

    /// <inheritdoc/>
    public void Stop()
    {
        if (_disposed || !_open)
            return;

        lock (_lock)
        {
            if (_disposed || !_open)
                return;

            _device.Stop();
            _head = 0;
            _queued = 0;
            _paused = false;
        }
    }

    /// <summary>
    /// Reconciles the ring's queued count down to what the device still holds, so the
    /// reported queue falls as the device drains. Never raises the count; combined with
    /// the drop-oldest <see cref="XAudio2AudioMath.Enqueue"/> cap this keeps
    /// <c>_queued</c> in <c>[0, BufferFragmentCount]</c>.
    /// </summary>
    private void ReconcileDrainedLocked()
    {
        var live = _device.BuffersQueued;
        if (live < 0)
            live = 0;
        else if (live > BufferFragmentCount)
            live = BufferFragmentCount;

        if (live < _queued)
        {
            _head = (_head + (_queued - live)) % BufferFragmentCount;
            _queued = live;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
                return;

            _disposed = true;

            if (_open)
            {
                try
                {
                    _device.Stop();
                }
                catch
                {
                    // Best-effort teardown.
                }
            }

            _open = false;
            _device.Dispose();
        }
    }
}
