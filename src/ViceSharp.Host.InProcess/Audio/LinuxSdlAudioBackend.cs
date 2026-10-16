using System.Diagnostics;
using ViceSharp.Abstractions;

namespace ViceSharp.Host.Audio;

/// <summary>
/// FR-SID-013 / TR-PLAT-001 / TEST-SID-001: Linux live output through SDL2 queued audio.
/// PCM16 conversion uses the shared master gain exactly once. A bounded software
/// queue supplies backpressure; waits release the lock so pause/stop/dispose can
/// cancel them. Missing devices return null at creation, and a failed/stalled
/// device ceases to be an audio timing source instead of hanging the emulator.
/// </summary>
public sealed class LinuxSdlAudioBackend : IAudioBackend, IDisposable
{
    internal const int QueueCapacitySamples = 2048;
    private const int SampleRate = 44100;
    private const int FragmentSamples = 256;
    private const int BytesPerSample = 2;
    private readonly object _lock = new();
    private readonly ILinuxAudioDevice _device;
    private readonly byte[] _scratch = new byte[FragmentSamples * BytesPerSample];
    private readonly TimeSpan _stallTimeout;
    private bool _available = true;
    private bool _paused;
    private bool _needsStart = true;
    private int _generation;

    private LinuxSdlAudioBackend(ILinuxAudioDevice device, TimeSpan stallTimeout)
    {
        _device = device;
        _stallTimeout = stallTimeout;
    }

    internal static LinuxSdlAudioBackend? TryCreate() => OperatingSystem.IsLinux()
        ? TryCreate(static () => new SdlQueuedAudioDevice()) : null;

    internal static LinuxSdlAudioBackend? TryCreate(Func<ILinuxAudioDevice> deviceFactory, TimeSpan? stallTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(deviceFactory);
        ILinuxAudioDevice? device = null;
        try
        {
            device = deviceFactory();
            if (device.Open(SampleRate, 1, FragmentSamples))
                return new(device, stallTimeout ?? TimeSpan.FromSeconds(2));
            ReportFailure("No usable SDL2 output device was opened.");
        }
        catch (Exception error)
        {
            ReportFailure(error.Message);
        }
        try { device?.Dispose(); }
        catch (Exception error) { ReportFailure(error.Message); }
        return null;
    }

    /// <summary>False after a device error, a bounded queue stall, or disposal.</summary>
    public bool IsAvailable { get { lock (_lock) return _available; } }

    /// <inheritdoc/>
    public void SubmitSamples(ReadOnlySpan<float> samples)
    {
        int generation;
        lock (_lock)
        {
            if (!_available || _paused || samples.IsEmpty)
                return;
            generation = _generation;
        }
        while (!samples.IsEmpty)
        {
            int count = Math.Min(FragmentSamples, samples.Length);
            if (!WaitAndSubmit(samples[..count], generation))
                return;
            samples = samples[count..];
        }
    }

    private bool WaitAndSubmit(ReadOnlySpan<float> samples, int generation)
    {
        long started = Stopwatch.GetTimestamp();
        while (true)
        {
            lock (_lock)
            {
                if (!_available || _paused || generation != _generation)
                    return false;
                try
                {
                    int queued = ReadQueuedSamples();
                    if (QueueCapacitySamples - queued >= samples.Length)
                    {
                        int bytes = samples.Length * BytesPerSample;
                        AudioSampleConverter.ConvertToPcm16(samples, _scratch.AsSpan(0, bytes), MasterAudioControl.EffectiveGain);
                        if (!_device.Queue(_scratch.AsSpan(0, bytes)))
                        {
                            Disable("SDL2 could not queue audio.");
                            return false;
                        }
                        if (_needsStart)
                        {
                            _device.SetPaused(false);
                            _needsStart = false;
                        }
                        return true;
                    }
                    if (Stopwatch.GetElapsedTime(started) >= _stallTimeout)
                    {
                        Disable("The SDL2 output queue stopped draining.");
                        return false;
                    }
                }
                catch (Exception error)
                {
                    Disable(error.Message);
                    return false;
                }
            }
            Thread.Sleep(1);
        }
    }

    /// <inheritdoc/>
    public int QueuedSampleCount
    {
        get
        {
            lock (_lock)
            {
                if (!_available) return 0;
                try { return ReadQueuedSamples(); }
                catch (Exception error) { Disable(error.Message); return 0; }
            }
        }
    }

    /// <inheritdoc/>
    public int AvailableSampleCount
    {
        get
        {
            lock (_lock)
            {
                if (!_available) return int.MaxValue;
                if (_paused) return 0;
                try { return Math.Max(0, QueueCapacitySamples - ReadQueuedSamples()); }
                catch (Exception error) { Disable(error.Message); return int.MaxValue; }
            }
        }
    }

    private int ReadQueuedSamples() => Math.Max(0, _device.QueuedBytes / BytesPerSample);

    /// <inheritdoc/>
    public void Pause()
    {
        lock (_lock)
        {
            if (!_available || _paused) return;
            _paused = true;
            _generation++;
            try { _device.SetPaused(true); }
            catch (Exception error) { Disable(error.Message); }
        }
    }

    /// <inheritdoc/>
    public void Resume()
    {
        lock (_lock)
        {
            if (!_available || !_paused) return;
            _paused = false;
            try { _device.SetPaused(false); _needsStart = false; }
            catch (Exception error) { Disable(error.Message); }
        }
    }

    /// <inheritdoc/>
    public void Stop()
    {
        lock (_lock)
        {
            if (!_available) return;
            _generation++;
            _paused = false;
            _needsStart = true;
            try { _device.SetPaused(true); _device.Clear(); }
            catch (Exception error) { Disable(error.Message); }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_lock) Disable(null);
    }

    private void Disable(string? reason)
    {
        if (!_available) return;
        _available = false;
        _generation++;
        if (reason is not null) ReportFailure(reason);
        try { _device.Dispose(); }
        catch (Exception error) { ReportFailure(error.Message); }
    }

    private static void ReportFailure(string reason) =>
        Console.Error.WriteLine($"ViceSharp Linux audio unavailable: {reason}");
}

/// <summary>Device seam keeps Linux backend tests silent and independent of a real audio server.</summary>
internal interface ILinuxAudioDevice : IDisposable
{
    bool Open(int sampleRate, int channels, int fragmentSamples);
    int QueuedBytes { get; }
    bool Queue(ReadOnlySpan<byte> pcm);
    void SetPaused(bool paused);
    void Clear();
}
